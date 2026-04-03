// ============================================================================
// FormationCore.cs — Runtime активная формация
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 13:25:00 UTC
// ============================================================================
//
// Источник: docs/FORMATION_SYSTEM.md
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Formation
{
    /// <summary>
    /// Участник наполнения формации.
    /// </summary>
    [Serializable]
    public class FormationParticipant
    {
        public GameObject practitioner;
        public string practitionerId;
        public float contributionRate; // Проводимость × плотность
        public long totalContributed;
        public bool isActive;

        public FormationParticipant(GameObject practitioner, float rate)
        {
            this.practitioner = practitioner;
            this.practitionerId = practitioner.GetInstanceID().ToString();
            this.contributionRate = rate;
            this.totalContributed = 0;
            this.isActive = true;
        }
    }

    /// <summary>
    /// Runtime компонент активной формации.
    /// Размещается в игровом мире и управляет всеми аспектами формации.
    /// 
    /// ┌─────────────────────────────────────────────────────────────────────────┐
    /// │                     ЖИЗНЕННЫЙ ЦИКЛ ФОРМАЦИИ                             │
    /// ├─────────────────────────────────────────────────────────────────────────┤
    /// │                                                                          │
    /// │   None → Drawing → Filling → Active → Depleted                          │
    /// │     ↓       ↓         ↓         ↓         ↓                              │
    /// │   (иниц) (контур) (Ци)   (эффекты) (конец)                              │
    /// │                                                                          │
    /// │   Transition conditions:                                                 │
    /// │   - Drawing → Filling: contourQi spent                                  │
    /// │   - Filling → Active: currentQi >= capacity                             │
    /// │   - Active → Depleted: currentQi <= 0                                   │
    /// │                                                                          │
    /// └─────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public class FormationCore : MonoBehaviour
    {
        #region Configuration

        [Header("Data")]
        [SerializeField] private FormationData formationData;
        [SerializeField] private FormationCoreData coreData;

        #endregion

        #region State

        [Header("State")]
        [SerializeField] private FormationStage stage = FormationStage.None;
        [SerializeField] private Vector2 center;
        [SerializeField] private float currentRadius;
        [SerializeField] private float remainingDuration;

        #endregion

        #region Ownership

        [Header("Ownership")]
        [SerializeField] private GameObject owner;
        [SerializeField] private string ownerId;

        #endregion

        #region Components

        private FormationQiPool qiPool;
        private List<FormationParticipant> participants;
        private float totalFillRate;

        // Effect processing
        private Collider2D[] affectedBuffer;
        private List<GameObject> currentlyAffected;
        private float lastEffectTick;
        private float lastDrainTick;

        // Visual
        private GameObject activeVfxInstance;
        private GameObject contourVfxInstance;

        #endregion

        #region Events

        /// <summary>
        /// Событие изменения состояния.
        /// </summary>
        public event Action<FormationStage, FormationStage> OnStageChanged;

        /// <summary>
        /// Событие изменения Ци.
        /// </summary>
        public event Action<long, long> OnQiChanged;

        /// <summary>
        /// Событие добавления участника.
        /// </summary>
        public event Action<FormationParticipant> OnParticipantAdded;

        /// <summary>
        /// Событие активации.
        /// </summary>
        public event Action OnActivated;

        /// <summary>
        /// Событие истощения.
        /// </summary>
        public event Action OnDepleted;

        /// <summary>
        /// Событие применения эффектов.
        /// </summary>
        public event Action<int> OnEffectsApplied; // int = количество целей

        #endregion

        #region Properties

        public FormationData Data => formationData;
        public FormationCoreData CoreData => coreData;
        public FormationStage Stage => stage;
        public FormationQiPool QiPool => qiPool;
        public Vector2 Center => center;
        public float Radius => currentRadius;
        public GameObject Owner => owner;
        public List<FormationParticipant> Participants => participants;
        public bool HasCore => coreData != null;
        public bool IsActive => stage == FormationStage.Active;
        public bool IsFilling => stage == FormationStage.Filling;
        public bool IsDepleted => stage == FormationStage.Depleted;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            qiPool = new FormationQiPool();
            participants = new List<FormationParticipant>();
            currentlyAffected = new List<GameObject>();
            affectedBuffer = new Collider2D[100]; // Буфер для поиска целей
        }

        private void Update()
        {
            switch (stage)
            {
                case FormationStage.Drawing:
                    UpdateDrawing(Time.deltaTime);
                    break;

                case FormationStage.Filling:
                    UpdateFilling(Time.deltaTime);
                    break;

                case FormationStage.Active:
                    UpdateActive(Time.deltaTime);
                    break;

                case FormationStage.Depleted:
                    // Ничего не делаем
                    break;
            }
        }

        private void OnDestroy()
        {
            // Очищаем визуальные эффекты
            if (activeVfxInstance != null)
                Destroy(activeVfxInstance);
            if (contourVfxInstance != null)
                Destroy(contourVfxInstance);
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализировать формацию.
        /// </summary>
        /// <param name="data">Данные формации</param>
        /// <param name="position">Позиция центра</param>
        /// <param name="creator">Создатель</param>
        /// <param name="core">Ядро формации (опционально)</param>
        public void Initialize(FormationData data, Vector2 position, GameObject creator, FormationCoreData core = null)
        {
            formationData = data;
            coreData = core;
            center = position;
            owner = creator;
            ownerId = creator != null ? creator.GetInstanceID().ToString() : "";

            // Настройка радиуса
            currentRadius = data.effectRadius;

            // Настройка пула Ци
            qiPool.Configure(data, core);

            // Подписываемся на события пула
            qiPool.OnQiChanged += HandleQiChanged;
            qiPool.OnFilled += HandleQiFilled;
            qiPool.OnDepleted += HandleQiDepleted;

            // Создаём визуальный эффект контура
            CreateContourVfx();

            // Переходим к стадии прорисовки
            SetStage(FormationStage.Drawing);

            Debug.Log($"[Formation] Инициализирована: {data.displayName} в {position}");
        }

        /// <summary>
        /// Инициализировать из сохранения.
        /// </summary>
        public void InitializeFromSave(FormationSaveData saveData, FormationData data, FormationCoreData core = null)
        {
            formationData = data;
            coreData = core;
            center = new Vector2(saveData.positionX, saveData.positionY);
            currentRadius = saveData.radius;
            remainingDuration = saveData.remainingDuration;
            ownerId = saveData.ownerId;

            // Загружаем пул
            qiPool.LoadSaveData(saveData.qiPoolData);

            // Подписываемся на события
            qiPool.OnQiChanged += HandleQiChanged;
            qiPool.OnFilled += HandleQiFilled;
            qiPool.OnDepleted += HandleQiDepleted;

            // Устанавливаем стадию
            SetStage(saveData.stage);

            // Создаём визуальные эффекты
            if (stage == FormationStage.Active)
            {
                CreateActiveVfx();
            }
            else if (stage == FormationStage.Filling)
            {
                CreateContourVfx();
            }

            Debug.Log($"[Formation] Загружена из сохранения: {data.displayName}");
        }

        #endregion

        #region Stage Management

        private void SetStage(FormationStage newStage)
        {
            if (stage == newStage) return;

            FormationStage previousStage = stage;
            stage = newStage;

            // Обработка переходов
            switch (newStage)
            {
                case FormationStage.Drawing:
                    remainingDuration = formationData.drawTime;
                    break;

                case FormationStage.Filling:
                    // Ждём наполнения
                    break;

                case FormationStage.Active:
                    remainingDuration = formationData.isPermanent ? float.MaxValue : formationData.baseDuration;
                    CreateActiveVfx();
                    PlayActivationSound();
                    break;

                case FormationStage.Depleted:
                    CreateDepletedVfx();
                    PlayDepletedSound();
                    break;
            }

            OnStageChanged?.Invoke(previousStage, newStage);
            Debug.Log($"[Formation] Стадия: {previousStage} → {newStage}");
        }

        #endregion

        #region Drawing Phase

        private void UpdateDrawing(float deltaTime)
        {
            remainingDuration -= deltaTime;

            if (remainingDuration <= 0)
            {
                // Прорисовка завершена
                CompleteDrawing();
            }
        }

        /// <summary>
        /// Завершить прорисовку контура.
        /// </summary>
        public void CompleteDrawing()
        {
            if (stage != FormationStage.Drawing) return;

            SetStage(FormationStage.Filling);
            Debug.Log("[Formation] Контур прорисован, ожидание наполнения");
        }

        #endregion

        #region Filling Phase

        private void UpdateFilling(float deltaTime)
        {
            // Наполнение происходит через ContributeQi
            // Здесь можно добавить автоматическое наполнение от владельца
        }

        /// <summary>
        /// Добавить участника наполнения.
        /// </summary>
        /// <param name="practitioner">Практик</param>
        /// <param name="contributionRate">Скорость вклада (проводимость × плотность)</param>
        /// <returns>True если добавлен</returns>
        public bool AddParticipant(GameObject practitioner, float contributionRate)
        {
            if (stage != FormationStage.Filling) return false;

            // Проверяем лимит участников
            if (participants.Count >= formationData.MaxHelpers)
            {
                Debug.LogWarning("[Formation] Достигнут максимум участников");
                return false;
            }

            // Проверяем минимальный уровень
            var qiController = practitioner.GetComponent<Qi.QiController>();
            if (qiController != null && qiController.CultivationLevel < formationData.MinHelperLevel)
            {
                Debug.LogWarning($"[Formation] Уровень практика слишком низкий: {qiController.CultivationLevel} < {formationData.MinHelperLevel}");
                return false;
            }

            // Проверяем дубликаты
            if (participants.Exists(p => p.practitioner == practitioner))
            {
                Debug.LogWarning("[Formation] Практик уже участвует");
                return false;
            }

            var participant = new FormationParticipant(practitioner, contributionRate);
            participants.Add(participant);

            totalFillRate += contributionRate;

            OnParticipantAdded?.Invoke(participant);
            Debug.Log($"[Formation] Участник добавлен. Всего: {participants.Count}, Скорость: {contributionRate:F1}");

            return true;
        }

        /// <summary>
        /// Внести Ци в формацию.
        /// </summary>
        /// <param name="practitioner">Практик</param>
        /// <param name="amount">Количество Ци</param>
        /// <param name="transferRate">Скорость передачи</param>
        /// <returns>Принятое количество</returns>
        public int ContributeQi(GameObject practitioner, int amount, float transferRate)
        {
            if (stage != FormationStage.Filling && stage != FormationStage.Active)
                return 0;

            int accepted = qiPool.AcceptQi(amount, transferRate);

            // Записываем вклад участника
            var participant = participants.Find(p => p.practitioner == practitioner);
            if (participant != null)
            {
                participant.totalContributed += accepted;
            }

            return accepted;
        }

        #endregion

        #region Active Phase

        private void UpdateActive(float deltaTime)
        {
            // Обновляем длительность
            if (remainingDuration < float.MaxValue)
            {
                remainingDuration -= deltaTime;

                if (remainingDuration <= 0)
                {
                    Deactivate();
                    return;
                }
            }

            // Применяем эффекты
            lastEffectTick += deltaTime;
            if (lastEffectTick >= formationData.effectTickInterval)
            {
                lastEffectTick = 0;
                ApplyEffects();
            }

            // Обрабатываем утечку (если интегрировано с TimeController)
            // Обычно вызывается извне через ProcessDrain
        }

        /// <summary>
        /// Применить эффекты формации.
        /// </summary>
        private void ApplyEffects()
        {
            if (formationData == null) return;

            // Находим цели в радиусе
            int count = Physics2D.OverlapCircleNonAlloc(center, currentRadius, affectedBuffer);

            int allyCount = 0;
            int enemyCount = 0;

            for (int i = 0; i < count; i++)
            {
                Collider2D collider = affectedBuffer[i];
                GameObject target = collider.gameObject;

                // Пропускаем себя
                if (target == gameObject) continue;

                // Проверяем союзник/враг
                bool isAlly = FormationEffects.IsAlly(target, owner);

                if (isAlly && formationData.allyEffects.Count > 0)
                {
                    FormationEffects.ApplyEffects(target, formationData.allyEffects, gameObject);
                    allyCount++;
                }
                else if (!isAlly && formationData.enemyEffects.Count > 0)
                {
                    FormationEffects.ApplyEffects(target, formationData.enemyEffects, gameObject);
                    enemyCount++;
                }
            }

            OnEffectsApplied?.Invoke(allyCount + enemyCount);
        }

        /// <summary>
        /// Активировать формацию.
        /// </summary>
        public void Activate()
        {
            if (stage != FormationStage.Filling) return;

            if (!qiPool.IsReadyForActivation)
            {
                Debug.LogWarning($"[Formation] Нельзя активировать: заполнение {qiPool.FillPercent:P0}");
                return;
            }

            SetStage(FormationStage.Active);
            OnActivated?.Invoke();

            Debug.Log($"[Formation] Активирована: {formationData.displayName}");
        }

        /// <summary>
        /// Деактивировать формацию.
        /// </summary>
        public void Deactivate()
        {
            if (stage != FormationStage.Active) return;

            SetStage(FormationStage.Depleted);
            OnDepleted?.Invoke();

            Debug.Log($"[Formation] Деактивирована: {formationData.displayName}");
        }

        /// <summary>
        /// Обработать тик времени (утечка Ци).
        /// </summary>
        public void ProcessTimeTick(int currentTick)
        {
            if (stage != FormationStage.Active) return;

            int drained = qiPool.ProcessDrain(currentTick);

            if (drained > 0)
            {
                Debug.Log($"[Formation] Утечка: -{drained} Ци | Осталось: {qiPool.currentQi:N0}");
            }
        }

        #endregion

        #region Qi Event Handlers

        private void HandleQiChanged(long current, long capacity)
        {
            OnQiChanged?.Invoke(current, capacity);
        }

        private void HandleQiFilled()
        {
            Debug.Log("[Formation] Пул Ци заполнен!");

            if (stage == FormationStage.Filling)
            {
                Activate();
            }
        }

        private void HandleQiDepleted()
        {
            Debug.Log("[Formation] Пул Ци истощён!");

            if (stage == FormationStage.Active)
            {
                Deactivate();
            }
        }

        #endregion

        #region Visual Effects

        private void CreateContourVfx()
        {
            if (formationData.contourVfx != null)
            {
                contourVfxInstance = Instantiate(formationData.contourVfx, center, Quaternion.identity, transform);
            }
        }

        private void CreateActiveVfx()
        {
            // Удаляем контур
            if (contourVfxInstance != null)
            {
                Destroy(contourVfxInstance);
                contourVfxInstance = null;
            }

            // Создаём активный эффект
            if (formationData.activeVfx != null)
            {
                activeVfxInstance = Instantiate(formationData.activeVfx, center, Quaternion.identity, transform);
            }
        }

        private void CreateDepletedVfx()
        {
            if (activeVfxInstance != null)
            {
                // Можно проиграть анимацию исчезновения
                Destroy(activeVfxInstance, 2f);
            }
        }

        private void PlayActivationSound()
        {
            if (formationData.activationSound != null)
            {
                AudioSource.PlayClipAtPoint(formationData.activationSound, center);
            }
        }

        private void PlayDepletedSound()
        {
            if (formationData.depletedSound != null)
            {
                AudioSource.PlayClipAtPoint(formationData.depletedSound, center);
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Проверить, находится ли точка в зоне действия.
        /// </summary>
        public bool IsInEffectArea(Vector2 point)
        {
            return Vector2.Distance(point, center) <= currentRadius;
        }

        /// <summary>
        /// Получить информацию о формации.
        /// </summary>
        public string GetInfo()
        {
            return $"[{formationData?.displayName ?? "Unknown"}]\n" +
                   $"Стадия: {stage}\n" +
                   $"Ци: {qiPool.currentQi:N0} / {qiPool.capacity:N0} ({qiPool.FillPercent:P0})\n" +
                   $"Радиус: {currentRadius}м\n" +
                   $"Участников: {participants.Count}\n" +
                   $"До истощения: {qiPool.GetTimeUntilDepletedFormatted()}";
        }

        /// <summary>
        /// Получить данные для сохранения.
        /// </summary>
        public FormationSaveData GetSaveData()
        {
            return new FormationSaveData
            {
                formationId = formationData?.formationId ?? "",
                coreId = coreData?.coreId ?? "",
                ownerId = ownerId,
                positionX = center.x,
                positionY = center.y,
                radius = currentRadius,
                stage = stage,
                remainingDuration = remainingDuration,
                qiPoolData = qiPool.GetSaveData()
            };
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            // Рисуем радиус действия
            Gizmos.color = Color.cyan.WithAlpha(0.3f);
            Gizmos.DrawWireSphere(center, currentRadius);

            // Рисуем центр
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(center, 0.5f);
        }

        #endregion
    }

    /// <summary>
    /// Расширения для Color.
    /// </summary>
    internal static class ColorExtensions
    {
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }
    }
}
