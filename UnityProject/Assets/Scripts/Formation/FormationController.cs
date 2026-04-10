// ============================================================================
// FormationController.cs — Главный контроллер системы формаций
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 13:30:00 UTC
// ============================================================================
//
// Источник: docs/FORMATION_SYSTEM.md
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;
using CultivationGame.Qi;
using CultivationGame.Charger;

namespace CultivationGame.Formation
{
    /// <summary>
    /// Известная формация практика.
    /// </summary>
    [Serializable]
    public class KnownFormation
    {
        public string formationId;
        public FormationData data;
        public int masteryLevel;
        public long timesUsed;

        public KnownFormation(FormationData data)
        {
            this.formationId = data.formationId;
            this.data = data;
            this.masteryLevel = 1;
            this.timesUsed = 0;
        }
    }

    /// <summary>
    /// Ядро с внедрённой формацией.
    /// </summary>
    [Serializable]
    public class ImbuedCore
    {
        public string coreId;
        public FormationCoreData coreData;
        public FormationData formationData;
        public bool isMounted;
        public Vector2 mountedPosition;

        public ImbuedCore(FormationCoreData core, FormationData formation)
        {
            this.coreId = core.coreId;
            this.coreData = core;
            this.formationData = formation;
            this.isMounted = false;
            this.mountedPosition = Vector2.zero;
        }
    }

    /// <summary>
    /// Главный контроллер системы формаций.
    /// Управляет созданием, активацией, удалением и сохранением формаций.
    /// 
    /// ┌─────────────────────────────────────────────────────────────────────────┐
    /// │                     АРХИТЕКТУРА КОНТРОЛЛЕРА                             │
    /// ├─────────────────────────────────────────────────────────────────────────┤
    /// │                                                                          │
    /// │   ┌─────────────────┐     ┌─────────────────┐     ┌─────────────────┐   │
    /// │   │ KnownFormations │     │  ActiveCores    │     │  ImbuedCores    │   │
    /// │   │ (изученные)     │     │  (в мире)       │     │  (в инвентаре)  │   │
    /// │   └─────────────────┘     └─────────────────┘     └─────────────────┘   │
    /// │           ↓                       ↓                       ↓              │
    /// │   ┌─────────────────────────────────────────────────────────────────┐   │
    /// │   │                    FormationController                         │   │
    /// │   │   - CreateWithoutCore()   - ImbueCore()   - MountAltar()       │   │
    /// │   │   - StartFilling()        - ContributeQi() - Activate()        │   │
    /// │   │   - OnTimeTick()          - ProcessDrain() - Deactivate()      │   │
    /// │   └─────────────────────────────────────────────────────────────────┘   │
    /// │                                   ↓                                      │
    /// │   ┌─────────────────────────────────────────────────────────────────┐   │
    /// │   │                    Интеграция                                   │   │
    /// │   │   QiController ←──→ ChargerController ←──→ TimeController       │   │
    /// │   └─────────────────────────────────────────────────────────────────┘   │
    /// │                                                                          │
    /// └─────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public class FormationController : MonoBehaviour
    {
        #region Singleton

        private static FormationController _instance;
        public static FormationController Instance => _instance;

        #endregion

        #region Configuration

        [Header("Settings")]
        [SerializeField] private int maxActiveFormations = 10;
        [SerializeField] private LayerMask targetLayerMask = -1;

        #endregion

        #region State

        [Header("Known Formations")]
        [SerializeField] private List<KnownFormation> knownFormations = new List<KnownFormation>();

        [Header("Imbued Cores (Inventory)")]
        [SerializeField] private List<ImbuedCore> imbuedCores = new List<ImbuedCore>();

        [Header("Active Formations")]
        [SerializeField] private List<FormationCore> activeCores = new List<FormationCore>();

        [Header("Selection")]
        [SerializeField] private FormationData selectedFormation;
        [SerializeField] private FormationCore selectedActiveCore;

        #endregion

        #region References

        private QiController playerQi;
        private ChargerController playerCharger;
        private World.TimeController timeController;

        #endregion

        #region Events

        /// <summary>
        /// Событие создания формации.
        /// </summary>
        public event Action<FormationCore> OnFormationCreated;

        /// <summary>
        /// Событие активации формации.
        /// </summary>
        public event Action<FormationCore> OnFormationActivated;

        /// <summary>
        /// Событие истощения формации.
        /// </summary>
        public event Action<FormationCore> OnFormationDepleted;

        /// <summary>
        /// Событие внедрения формации в ядро.
        /// </summary>
        public event Action<ImbuedCore> OnCoreImbued;

        /// <summary>
        /// Событие изучения новой формации.
        /// </summary>
        public event Action<KnownFormation> OnFormationLearned;

        /// <summary>
        /// Событие изменения выбора.
        /// </summary>
        public event Action<FormationData> OnSelectionChanged;

        #endregion

        #region Properties

        public List<KnownFormation> KnownFormations => knownFormations;
        public List<ImbuedCore> ImbuedCores => imbuedCores;
        public List<FormationCore> ActiveCores => activeCores;
        public FormationData SelectedFormation => selectedFormation;
        public FormationCore SelectedActiveCore => selectedActiveCore;
        public int ActiveCount => activeCores.Count;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void Start()
        {
            InitializeReferences();
        }

        private void OnDestroy()
        {
            _instance = null;
            UnsubscribeFromEvents();
        }

        #endregion

        #region Initialization

        private void InitializeReferences()
        {
            // Находим компоненты игрока
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerQi = player.GetComponent<QiController>();
                playerCharger = player.GetComponent<ChargerController>();
            }

            // Находим TimeController
            // Редактировано: 2026-04-03 - Обновлено для Unity 6 (FindFirstObjectByType вместо FindObjectOfType)
            timeController = FindFirstObjectByType<World.TimeController>();

            // Подписываемся на события
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (timeController != null)
            {
                timeController.OnTick += HandleTimeTick;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (timeController != null)
            {
                timeController.OnTick -= HandleTimeTick;
            }

            // Отписываемся от всех активных формаций
            foreach (var core in activeCores)
            {
                if (core != null)
                {
                    core.OnDepleted -= HandleFormationDepleted;
                }
            }
        }

        #endregion

        #region Formation Creation

        /// <summary>
        /// Создать формацию без физического ядра (одноразовая).
        /// </summary>
        /// <param name="data">Данные формации</param>
        /// <param name="position">Позиция</param>
        /// <param name="creator">Создатель</param>
        /// <returns>Созданная формация или null</returns>
        public FormationCore CreateWithoutCore(FormationData data, Vector2 position, GameObject creator = null)
        {
            if (data == null)
            {
                Debug.LogError("[FormationController] Data is null!");
                return null;
            }

            if (data.requiresCore)
            {
                Debug.LogWarning($"[FormationController] Формация {data.displayName} требует ядро!");
                return null;
            }

            // Проверяем лимит
            if (activeCores.Count >= maxActiveFormations)
            {
                Debug.LogWarning("[FormationController] Достигнут максимум активных формаций!");
                return null;
            }

            // Проверяем знания
            var known = knownFormations.Find(k => k.formationId == data.formationId);
            if (known == null)
            {
                Debug.LogWarning($"[FormationController] Формация {data.displayName} не изучена!");
                return null;
            }

            // Тратим Ци на контур
            if (creator != null)
            {
                var qi = creator.GetComponent<QiController>();
                if (qi != null)
                {
                    if (!qi.SpendQi(data.ContourQi))
                    {
                        Debug.LogWarning($"[FormationController] Недостаточно Ци для контура: {data.ContourQi}");
                        return null;
                    }
                }
            }

            // Создаём объект
            GameObject formationObj = new GameObject($"Formation_{data.formationId}");
            formationObj.transform.position = position;

            FormationCore core = formationObj.AddComponent<FormationCore>();
            core.Initialize(data, position, creator ?? gameObject, null);

            // Подписываемся на события
            core.OnDepleted += HandleFormationDepleted;

            // Добавляем в список
            activeCores.Add(core);
            known.timesUsed++;

            OnFormationCreated?.Invoke(core);

            Debug.Log($"[FormationController] Создана формация: {data.displayName} в {position}");

            return core;
        }

        /// <summary>
        /// Внедрить формацию в ядро.
        /// </summary>
        /// <param name="core">Ядро формации</param>
        /// <param name="data">Данные формации</param>
        /// <param name="creator">Создатель</param>
        /// <returns>ImbuedCore или null</returns>
        public ImbuedCore ImbueCore(FormationCoreData core, FormationData data, GameObject creator = null)
        {
            if (core == null || data == null)
            {
                Debug.LogError("[FormationController] Core or Data is null!");
                return null;
            }

            // Проверяем совместимость
            if (!data.IsCompatibleWithCore(core))
            {
                Debug.LogWarning($"[FormationController] Формация {data.displayName} несовместима с ядром {core.nameRu}!");
                return null;
            }

            // Проверяем знания
            var known = knownFormations.Find(k => k.formationId == data.formationId);
            if (known == null)
            {
                Debug.LogWarning($"[FormationController] Формация {data.displayName} не изучена!");
                return null;
            }

            // Тратим Ци на внедрение
            if (creator != null)
            {
                var qi = creator.GetComponent<QiController>();
                if (qi != null)
                {
                    if (!qi.SpendQi(data.ContourQi))
                    {
                        Debug.LogWarning($"[FormationController] Недостаточно Ци для внедрения: {data.ContourQi}");
                        return null;
                    }
                }
            }

            // Создаём внедрённое ядро
            ImbuedCore imbued = new ImbuedCore(core, data);
            imbuedCores.Add(imbued);

            OnCoreImbued?.Invoke(imbued);
            known.timesUsed++;

            Debug.Log($"[FormationController] Формация {data.displayName} внедрена в {core.nameRu}");

            return imbued;
        }

        #endregion

        #region Mounting & Activation

        /// <summary>
        /// Смонтировать ядро-алтарь.
        /// </summary>
        /// <param name="imbued">Внедрённое ядро</param>
        /// <param name="position">Позиция монтажа</param>
        /// <returns>Активная формация или null</returns>
        public FormationCore MountAltar(ImbuedCore imbued, Vector2 position)
        {
            if (imbued == null)
            {
                Debug.LogError("[FormationController] Imbued core is null!");
                return null;
            }

            if (imbued.coreData.coreType != FormationCoreType.Altar)
            {
                Debug.LogWarning("[FormationController] Только алтари требуют монтажа!");
                return null;
            }

            if (imbued.isMounted)
            {
                Debug.LogWarning("[FormationController] Ядро уже смонтировано!");
                return null;
            }

            // Проверяем лимит
            if (activeCores.Count >= maxActiveFormations)
            {
                Debug.LogWarning("[FormationController] Достигнут максимум активных формаций!");
                return null;
            }

            // Создаём объект
            GameObject formationObj = new GameObject($"Formation_{imbued.formationData.formationId}");
            formationObj.transform.position = position;

            FormationCore core = formationObj.AddComponent<FormationCore>();
            core.Initialize(imbued.formationData, position, gameObject, imbued.coreData);

            // Подписываемся
            core.OnDepleted += HandleFormationDepleted;

            // Обновляем состояние
            imbued.isMounted = true;
            imbued.mountedPosition = position;

            // Добавляем в списки
            activeCores.Add(core);

            OnFormationCreated?.Invoke(core);

            Debug.Log($"[FormationController] Алтарь смонтирован: {imbued.coreData.nameRu} в {position}");

            return core;
        }

        /// <summary>
        /// Разместить дисковую формацию.
        /// </summary>
        /// <param name="imbued">Внедрённое ядро</param>
        /// <param name="position">Позиция</param>
        /// <returns>Активная формация или null</returns>
        public FormationCore PlaceDisk(ImbuedCore imbued, Vector2 position)
        {
            if (imbued == null)
            {
                Debug.LogError("[FormationController] Imbued core is null!");
                return null;
            }

            if (imbued.coreData.coreType != FormationCoreType.Disk)
            {
                Debug.LogWarning("[FormationController] Только диски можно размещать!");
                return null;
            }

            // Проверяем лимит
            if (activeCores.Count >= maxActiveFormations)
            {
                Debug.LogWarning("[FormationController] Достигнут максимум активных формаций!");
                return null;
            }

            // Создаём объект
            GameObject formationObj = new GameObject($"Formation_{imbued.formationData.formationId}");
            formationObj.transform.position = position;

            FormationCore core = formationObj.AddComponent<FormationCore>();
            core.Initialize(imbued.formationData, position, gameObject, imbued.coreData);

            // Подписываемся
            core.OnDepleted += HandleFormationDepleted;

            // Добавляем в списки
            activeCores.Add(core);

            OnFormationCreated?.Invoke(core);

            Debug.Log($"[FormationController] Диск размещён: {imbued.coreData.nameRu} в {position}");

            return core;
        }

        /// <summary>
        /// Начать наполнение формации.
        /// </summary>
        public void StartFilling(FormationCore core)
        {
            if (core == null || core.Stage != FormationStage.Filling) return;

            // Добавляем владельца как первого участника
            if (playerQi != null)
            {
                float rate = playerQi.Conductivity * playerQi.QiDensity;
                core.AddParticipant(playerQi.gameObject, rate);
            }
        }

        /// <summary>
        /// Внести Ци в формацию.
        /// FIX: long для Qi > 2.1B
        /// </summary>
        public long ContributeQi(FormationCore core, long maxAmount)
        {
            if (core == null || playerQi == null) return 0;

            long amount = Math.Min(maxAmount, playerQi.CurrentQi);
            float transferRate = playerQi.Conductivity * playerQi.QiDensity;

            long accepted = core.ContributeQi(playerQi.gameObject, amount, transferRate);

            if (accepted > 0)
            {
                playerQi.SpendQi(accepted);
            }

            return accepted;
        }

        #endregion

        #region Learning

        /// <summary>
        /// Изучить формацию.
        /// </summary>
        public bool LearnFormation(FormationData data)
        {
            if (data == null) return false;

            // Проверяем, не изучена ли уже
            if (knownFormations.Exists(k => k.formationId == data.formationId))
            {
                Debug.LogWarning($"[FormationController] Формация {data.displayName} уже изучена!");
                return false;
            }

            // Проверяем требования
            if (playerQi != null)
            {
                if (playerQi.CultivationLevel < data.requirements.minCultivationLevel)
                {
                    Debug.LogWarning($"[FormationController] Требуется уровень {data.requirements.minCultivationLevel}!");
                    return false;
                }
            }

            // Добавляем в список
            KnownFormation known = new KnownFormation(data);
            knownFormations.Add(known);

            OnFormationLearned?.Invoke(known);

            Debug.Log($"[FormationController] Изучена формация: {data.displayName}");

            return true;
        }

        /// <summary>
        /// Проверить, изучена ли формация.
        /// </summary>
        public bool IsFormationKnown(string formationId)
        {
            return knownFormations.Exists(k => k.formationId == formationId);
        }

        #endregion

        #region Time Processing

        private void HandleTimeTick(int tick)
        {
            // Обрабатываем утечку для всех активных формаций
            for (int i = activeCores.Count - 1; i >= 0; i--)
            {
                var core = activeCores[i];
                if (core != null && core.IsActive)
                {
                    core.ProcessTimeTick(tick);
                }
            }
        }

        private void HandleFormationDepleted(FormationCore core)
        {
            OnFormationDepleted?.Invoke(core);

            // Удаляем из списка (с задержкой для визуальных эффектов)
            StartCoroutine(RemoveDepletedFormation(core));
        }

        private System.Collections.IEnumerator RemoveDepletedFormation(FormationCore core)
        {
            yield return new WaitForSeconds(2f);

            if (activeCores.Contains(core))
            {
                activeCores.Remove(core);
            }

            if (core != null)
            {
                Destroy(core.gameObject);
            }
        }

        #endregion

        #region Selection

        /// <summary>
        /// Выбрать формацию для размещения.
        /// </summary>
        public void SelectFormation(FormationData data)
        {
            selectedFormation = data;
            OnSelectionChanged?.Invoke(data);
        }

        /// <summary>
        /// Выбрать активную формацию.
        /// </summary>
        public void SelectActiveFormation(FormationCore core)
        {
            selectedActiveCore = core;
        }

        #endregion

        #region Charger Integration

        /// <summary>
        /// Подпитать формацию от зарядника.
        /// FIX: long для Qi > 2.1B
        /// </summary>
        public long ChargeFormationFromCharger(FormationCore core)
        {
            if (core == null || playerCharger == null) return 0;
            if (!playerCharger.IsOperational || !playerCharger.HasStones) return 0;

            // Ограничено проводимостью зарядника
            long maxTransfer = (long)(playerCharger.Buffer.Conductivity * Time.deltaTime * 60);

            // Используем Ци из буфера
            var result = playerCharger.Buffer.UseQiForTechnique(maxTransfer, 0);

            if (result.QiFromBuffer > 0)
            {
                core.ContributeQi(playerQi?.gameObject, result.QiFromBuffer, playerCharger.Buffer.Conductivity);
            }

            return result.QiFromBuffer;
        }

        #endregion

        #region Save/Load

        /// <summary>
        /// Получить данные для сохранения.
        /// </summary>
        public FormationSystemSaveData GetSaveData()
        {
            var saveData = new FormationSystemSaveData();

            // Изученные формации
            foreach (var known in knownFormations)
            {
                saveData.knownFormations.Add(new KnownFormationSaveData
                {
                    formationId = known.formationId,
                    masteryLevel = known.masteryLevel,
                    timesUsed = known.timesUsed
                });
            }

            // Внедрённые ядра
            foreach (var imbued in imbuedCores)
            {
                saveData.imbuedCores.Add(new ImbuedCoreSaveData
                {
                    coreId = imbued.coreId,
                    formationId = imbued.formationData.formationId,
                    isMounted = imbued.isMounted,
                    mountedPositionX = imbued.mountedPosition.x,
                    mountedPositionY = imbued.mountedPosition.y
                });
            }

            // Активные формации
            foreach (var core in activeCores)
            {
                if (core != null)
                {
                    saveData.activeFormations.Add(core.GetSaveData());
                }
            }

            return saveData;
        }

        /// <summary>
        /// Загрузить из сохранения.
        /// </summary>
        public void LoadSaveData(FormationSystemSaveData saveData, Dictionary<string, FormationData> formationsLookup, Dictionary<string, FormationCoreData> coresLookup)
        {
            // Очищаем текущее состояние
            foreach (var core in activeCores)
            {
                if (core != null) Destroy(core.gameObject);
            }
            activeCores.Clear();
            knownFormations.Clear();
            imbuedCores.Clear();

            // Загружаем изученные формации
            foreach (var knownSave in saveData.knownFormations)
            {
                if (formationsLookup.TryGetValue(knownSave.formationId, out var data))
                {
                    var known = new KnownFormation(data)
                    {
                        masteryLevel = knownSave.masteryLevel,
                        timesUsed = knownSave.timesUsed
                    };
                    knownFormations.Add(known);
                }
            }

            // Загружаем внедрённые ядра
            foreach (var imbuedSave in saveData.imbuedCores)
            {
                if (coresLookup.TryGetValue(imbuedSave.coreId, out var core) &&
                    formationsLookup.TryGetValue(imbuedSave.formationId, out var formation))
                {
                    var imbued = new ImbuedCore(core, formation)
                    {
                        isMounted = imbuedSave.isMounted,
                        mountedPosition = new Vector2(imbuedSave.mountedPositionX, imbuedSave.mountedPositionY)
                    };
                    imbuedCores.Add(imbued);
                }
            }

            // Загружаем активные формации
            foreach (var formationSave in saveData.activeFormations)
            {
                if (formationsLookup.TryGetValue(formationSave.formationId, out var data))
                {
                    coresLookup.TryGetValue(formationSave.coreId, out var core);

                    GameObject formationObj = new GameObject($"Formation_{data.formationId}");
                    FormationCore formationCore = formationObj.AddComponent<FormationCore>();
                    formationCore.InitializeFromSave(formationSave, data, core);

                    formationCore.OnDepleted += HandleFormationDepleted;
                    activeCores.Add(formationCore);
                }
            }

            Debug.Log($"[FormationController] Загружено: {knownFormations.Count} изученных, {imbuedCores.Count} ядер, {activeCores.Count} активных");
        }

        #endregion

        #region Utility

        /// <summary>
        /// Найти формацию в точке.
        /// </summary>
        public FormationCore GetFormationAtPoint(Vector2 point)
        {
            foreach (var core in activeCores)
            {
                if (core != null && core.IsInEffectArea(point))
                {
                    return core;
                }
            }
            return null;
        }

        /// <summary>
        /// Получить все формации в радиусе.
        /// </summary>
        public List<FormationCore> GetFormationsInRadius(Vector2 center, float radius)
        {
            List<FormationCore> result = new List<FormationCore>();

            foreach (var core in activeCores)
            {
                if (core != null && Vector2.Distance(core.Center, center) <= radius)
                {
                    result.Add(core);
                }
            }

            return result;
        }

        /// <summary>
        /// Получить информацию о системе.
        /// </summary>
        public string GetSystemInfo()
        {
            return $"Формации: {activeCores.Count}/{maxActiveFormations}\n" +
                   $"Изучено: {knownFormations.Count}\n" +
                   $"Ядер: {imbuedCores.Count}";
        }

        #endregion
    }

    #region Save Data Structures

    /// <summary>
    /// Данные системы формаций для сохранения.
    /// </summary>
    [Serializable]
    public class FormationSystemSaveData
    {
        public List<KnownFormationSaveData> knownFormations = new List<KnownFormationSaveData>();
        public List<ImbuedCoreSaveData> imbuedCores = new List<ImbuedCoreSaveData>();
        public List<FormationSaveData> activeFormations = new List<FormationSaveData>();
    }

    /// <summary>
    /// Данные изученной формации для сохранения.
    /// </summary>
    [Serializable]
    public class KnownFormationSaveData
    {
        public string formationId;
        public int masteryLevel;
        public long timesUsed;
    }

    /// <summary>
    /// Данные внедрённого ядра для сохранения.
    /// </summary>
    [Serializable]
    public class ImbuedCoreSaveData
    {
        public string coreId;
        public string formationId;
        public bool isMounted;
        public float mountedPositionX;
        public float mountedPositionY;
    }

    /// <summary>
    /// Данные активной формации для сохранения.
    /// </summary>
    [Serializable]
    public class FormationSaveData
    {
        public string formationId;
        public string coreId;
        public string ownerId;
        public float positionX;
        public float positionY;
        public float radius;
        public FormationStage stage;
        public float remainingDuration;
        public FormationQiPoolSaveData qiPoolData;
    }

    #endregion
}
