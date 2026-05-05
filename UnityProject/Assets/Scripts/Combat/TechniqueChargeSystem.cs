// ============================================================================
// TechniqueChargeSystem.cs — Ядро системы накачки техник (НАКАЧКА)
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-05-04 04:28:00 UTC
// Редактировано: 2026-05-07 10:30:00 UTC — ФАЗА 2: chargeSpeedBonus из EquipmentController
// ============================================================================
//
// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  «ВЫВЕРНУТАЯ» СИСТЕМА ТЕХНИК                                               ║
// ╠═══════════════════════════════════════════════════════════════════════════╣
// ║                                                                            ║
// ║  СНАЧАЛА нажимается клавиша → ПОТОМ накачка → ТОЛЬКО ПОСЛЕ — срабатывает  ║
// ║                                                                            ║
// ║  Правила:                                                                  ║
// ║  • Одномоментно можно накачивать ТОЛЬКО одну технику                       ║
// ║  • Минимальное время накачки = tick / 10                                   ║
// ║  • Прерывание: урон/стан/отмена → частичный возврат Ци                     ║
// ║  • Накачка блокирует другие техники                                        ║
// ║  • Используется И игроком И NPC                                            ║
// ║                                                                            ║
// ╚═══════════════════════════════════════════════════════════════════════════╝
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Qi;
using CultivationGame.World;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Ядро системы накачки техник.
    /// Компонент, который управляет процессом накачки для ICombatant.
    /// Вешается на тот же GameObject, что и TechniqueController.
    ///
    /// Подписывается на TimeController.OnTickDelta для покадрового обновления.
    /// Используется И игроком И NPC.
    /// </summary>
    public class TechniqueChargeSystem : MonoBehaviour
    {
        #region Константы

        /// <summary>Возврат Ци при отмене игроком (%)</summary>
        private const float QI_RETURN_PLAYER_CANCEL = 0.70f;
        /// <summary>Возврат Ци при прерывании уроном (%)</summary>
        private const float QI_RETURN_DAMAGE_INTERRUPT = 0.50f;
        /// <summary>Возврат Ци при оглушении (%)</summary>
        private const float QI_RETURN_STUN = 0f;
        /// <summary>Возврат Ци при смерти (%)</summary>
        private const float QI_RETURN_DEATH = 0f;
        /// <summary>Возврат Ци при нехватке Ци (%)</summary>
        private const float QI_RETURN_QI_DEPLETED = 0.70f;
        /// <summary>Минимальный % Ци для срабатывания (фиаско)</summary>
        private const float MIN_QI_PERCENT_FOR_FIRE = 0.30f;
        /// <summary>Множитель скорости движения для melee накачки</summary>
        private const float MELEE_MOVE_SPEED_MULT = 0.5f;
        /// <summary>Множитель скорости движения для ranged накачки (0 = нельзя)</summary>
        private const float RANGED_MOVE_SPEED_MULT = 0f;

        #endregion

        #region Поля

        [Header("Ссылки")]
        [SerializeField] private TechniqueController techniqueController;
        [SerializeField] private QiController qiController;

        // ФАЗА 2: EquipmentController для chargeSpeedBonus (опционально)
        private CultivationGame.Inventory.EquipmentController equipmentController;

        // === Runtime ===
        private TechniqueChargeData activeCharge = TechniqueChargeData.Empty;
        private bool isSubscribedToTick = false;

        #endregion

        #region События

        /// <summary>Накачка началась</summary>
        public event Action<TechniqueChargeData> OnChargeStarted;
        /// <summary>Прогресс накачки обновлён (каждый кадр)</summary>
        public event Action<TechniqueChargeData> OnChargeProgress;
        /// <summary>Накачка завершена, техника готова к запуску</summary>
        public event Action<TechniqueChargeData> OnChargeCompleted;
        /// <summary>Накачка прервана</summary>
        public event Action<TechniqueChargeData, ChargeInterruptReason> OnChargeInterrupted;
        /// <summary>Техника сработала после накачки</summary>
        public event Action<TechniqueChargeData> OnChargeFired;

        #endregion

        #region Свойства

        /// <summary>Активна ли накачка в данный момент?</summary>
        public bool IsCharging => activeCharge.IsActive;
        /// <summary>Данные текущей накачки</summary>
        public TechniqueChargeData ActiveCharge => activeCharge;
        /// <summary>Прогресс накачки 0.0-1.0</summary>
        public float ChargeProgress => activeCharge.ChargeProgress;
        /// <summary>Текущее состояние накачки</summary>
        public ChargeState ChargeState => activeCharge.State;
        /// <summary>Можно ли двигаться во время накачки?</summary>
        public bool CanMove => activeCharge.IsActive && activeCharge.CanMoveWhileCharging;
        /// <summary>Множитель скорости движения при накачке</summary>
        public float MoveSpeedMultiplier => activeCharge.IsActive ? activeCharge.MoveSpeedMultiplier : 1f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (techniqueController == null)
                techniqueController = GetComponent<TechniqueController>();
            if (qiController == null)
                qiController = GetComponent<QiController>();
            // ФАЗА 2: EquipmentController — может отсутствовать (опционально)
            equipmentController = GetComponent<CultivationGame.Inventory.EquipmentController>();
        }

        private void OnEnable()
        {
            SubscribeToTick();
        }

        private void OnDisable()
        {
            UnsubscribeFromTick();
        }

        private void OnDestroy()
        {
            UnsubscribeFromTick();
        }

        #endregion

        #region Подписка на тиковую систему

        private void SubscribeToTick()
        {
            if (isSubscribedToTick) return;

            var tc = TimeController.Instance;
            if (tc != null)
            {
                tc.OnTickDelta += ProcessChargeFrame;
                isSubscribedToTick = true;
            }
            else
            {
                // Fallback: подписываемся через Update если TimeController ещё не инициализирован
                Debug.LogWarning("[TechniqueChargeSystem] TimeController не найден, используется fallback Update");
            }
        }

        private void UnsubscribeFromTick()
        {
            if (!isSubscribedToTick) return;

            var tc = TimeController.Instance;
            if (tc != null)
            {
                tc.OnTickDelta -= ProcessChargeFrame;
            }
            isSubscribedToTick = false;
        }

        /// <summary>
        /// Fallback: если TimeController не доступен, ProcessChargeFrame
        /// вызывается из Update.
        /// </summary>
        private void Update()
        {
            if (!isSubscribedToTick && activeCharge.IsActive)
            {
                ProcessChargeFrame(Time.deltaTime);
            }
        }

        #endregion

        #region Публичные методы — управление накачкой

        /// <summary>
        /// Начать накачку техники.
        /// Вызывается из PlayerController (по клавише) или из CombatAI (NPC).
        ///
        /// Пайплайн:
        /// 1. Проверка: можно ли использовать технику?
        /// 2. Проверка: не накачивается ли уже другая техника?
        /// 3. Расчёт параметров накачки (chargeTime, qiTotal, qiRate)
        /// 4. Установка activeCharge в Charging
        /// 5. Отправка события OnChargeStarted
        /// </summary>
        /// <param name="technique">Техника для накачки</param>
        /// <returns>true если накачка началась</returns>
        public bool BeginCharge(LearnedTechnique technique)
        {
            if (technique == null || technique.Data == null)
            {
                Debug.LogWarning("[TechniqueChargeSystem] BeginCharge: техника null");
                return false;
            }

            // Правило: только одна техника одновременно
            if (activeCharge.IsActive)
            {
                Debug.LogWarning($"[TechniqueChargeSystem] BeginCharge: уже накачивается {activeCharge.Technique?.Data?.nameRu}");
                return false;
            }

            // Проверка: можно ли использовать технику?
            if (techniqueController != null && !techniqueController.CanUseTechnique(technique))
            {
                Debug.LogWarning($"[TechniqueChargeSystem] BeginCharge: нельзя использовать {technique.Data.nameRu}");
                return false;
            }

            // Получаем minChargeTime из тиковой системы
            float minChargeTime = GetMinChargeTime();

            // Рассчитываем время накачки
            float chargeTime = CalculateChargeTime(technique, minChargeTime);

            // Рассчитываем стоимость Ци
            long qiCost = techniqueController != null
                ? CalculateQiCost(technique)
                : technique.Data.baseQiCost;

            // Рассчитываем скорость вливания Ци (Ци/сек)
            float qiChargeRate = qiCost > 0 ? (float)qiCost / chargeTime : 0f;

            // Определяем можно ли двигаться
            bool isMelee = IsMeleeTechnique(technique);
            bool canMove = isMelee;
            float moveSpeedMult = isMelee ? MELEE_MOVE_SPEED_MULT : RANGED_MOVE_SPEED_MULT;

            // Формируем данные накачки
            activeCharge = new TechniqueChargeData
            {
                Technique = technique,
                State = ChargeState.Charging,
                ChargeProgress = 0f,
                ChargeTime = chargeTime,
                ElapsedTime = 0f,
                QiCharged = 0,
                QiTotalRequired = qiCost,
                QiChargeRate = qiChargeRate,
                CanMoveWhileCharging = canMove,
                MoveSpeedMultiplier = moveSpeedMult,
                CanBeInterruptedByDamage = true,
                InterruptDamageThreshold = CalculateInterruptThreshold(technique),
                MinChargeTime = minChargeTime,
                InterruptReason = ChargeInterruptReason.PlayerCancel,
                QiReturnPercent = 0f
            };

            // Подписываемся на тик если ещё не подписаны
            SubscribeToTick();

            OnChargeStarted?.Invoke(activeCharge);
            Debug.Log($"[TechniqueChargeSystem] Накачка началась: {technique.Data.nameRu}, " +
                      $"chargeTime={chargeTime:F2}с, qiCost={qiCost}, minTime={minChargeTime:F3}с");

            return true;
        }

        /// <summary>
        /// Отменить накачку (по желанию игрока).
        /// Повторное нажатие той же клавиши → отмена.
        /// Возврат Ци: 70%.
        /// </summary>
        public void CancelCharge()
        {
            if (!activeCharge.IsActive) return;

            InterruptCharge(ChargeInterruptReason.PlayerCancel);
        }

        /// <summary>
        /// Прервать накачку из-за полученного урона.
        /// Вызывается из CombatManager при нанесении урона.
        /// </summary>
        /// <param name="damageAmount">Количество полученного урона</param>
        /// <returns>true если накачка была прервана</returns>
        public bool TryInterruptByDamage(float damageAmount)
        {
            if (!activeCharge.IsActive) return false;
            if (!activeCharge.CanBeInterruptedByDamage) return false;

            if (damageAmount >= activeCharge.InterruptDamageThreshold)
            {
                InterruptCharge(ChargeInterruptReason.DamageInterrupt);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Прервать накачку из-за оглушения (stun).
        /// Вызывается из BuffManager при наложении стана.
        /// Возврат Ци: 0%.
        /// </summary>
        public void InterruptByStun()
        {
            if (!activeCharge.IsActive) return;
            InterruptCharge(ChargeInterruptReason.StunInterrupt);
        }

        /// <summary>
        /// Прервать накачку из-за смерти практика.
        /// Возврат Ци: 0%.
        /// </summary>
        public void InterruptByDeath()
        {
            if (!activeCharge.IsActive) return;
            InterruptCharge(ChargeInterruptReason.DeathInterrupt);
        }

        #endregion

        #region Обработка накачки (каждый кадр)

        /// <summary>
        /// Обработка накачки — вызывается каждый кадр через OnTickDelta.
        /// Обновляет прогресс, вливает Ци, проверяет завершение.
        /// </summary>
        /// <param name="deltaTime">Время с прошлого кадра (сек)</param>
        private void ProcessChargeFrame(float deltaTime)
        {
            if (activeCharge.State != ChargeState.Charging) return;

            // Обновляем время
            activeCharge.ElapsedTime += deltaTime;

            // Обновляем прогресс (0.0 → 1.0)
            activeCharge.ChargeProgress = Mathf.Clamp01(activeCharge.ElapsedTime / activeCharge.ChargeTime);

            // Вливаем Ци (постепенно, каждый кадр)
            long qiToAdd = (long)(activeCharge.QiChargeRate * deltaTime);
            if (qiToAdd > 0)
            {
                activeCharge.QiCharged += qiToAdd;

                // Тратим Ци из QiController (реальное списание)
                if (qiController != null)
                {
                    if (!qiController.SpendQi(qiToAdd))
                    {
                        // Не хватает Ци для продолжения!
                        InterruptCharge(ChargeInterruptReason.QiDepleted);
                        return;
                    }
                }
            }

            // Отправляем событие прогресса
            OnChargeProgress?.Invoke(activeCharge);

            // Проверяем завершение накачки
            if (activeCharge.ChargeProgress >= 1.0f && activeCharge.QiCharged >= (long)(activeCharge.QiTotalRequired * MIN_QI_PERCENT_FOR_FIRE))
            {
                CompleteCharge();
            }
        }

        /// <summary>
        /// Завершение накачки — техника готова к срабатыванию.
        /// Переход: Charging → Ready → Firing → None
        /// </summary>
        private void CompleteCharge()
        {
            activeCharge.State = ChargeState.Ready;
            OnChargeCompleted?.Invoke(activeCharge);

            Debug.Log($"[TechniqueChargeSystem] Накачка завершена: {activeCharge.Technique?.Data?.nameRu}, " +
                      $"QiCharged={activeCharge.QiCharged}/{activeCharge.QiTotalRequired}");

            // Автоматический запуск техники!
            FireChargedTechnique();
        }

        /// <summary>
        /// Запуск техники после завершения накачки.
        /// Переход: Ready → Firing → None
        /// </summary>
        private void FireChargedTechnique()
        {
            activeCharge.State = ChargeState.Firing;

            // Используем технику через TechniqueController
            if (techniqueController != null && activeCharge.Technique != null)
            {
                // Техника срабатывает — Ци уже потрачено во время накачки,
                // поэтому UseTechnique не должен тратить Ци повторно.
                // TechniqueController.UseTechnique() устанавливает кулдаун и повышает мастерство.
                TechniqueUseResult result = techniqueController.UseTechniqueFromCharge(activeCharge.Technique);

                if (result.Success)
                {
                    OnChargeFired?.Invoke(activeCharge);
                    Debug.Log($"[TechniqueChargeSystem] Техника сработала: {activeCharge.Technique.Data.nameRu}");
                }
                else
                {
                    Debug.LogWarning($"[TechniqueChargeSystem] Техника не сработала: {result.FailReason}");
                }
            }

            // Сбрасываем состояние накачки
            activeCharge = TechniqueChargeData.Empty;
        }

        #endregion

        #region Прерывание накачки

        /// <summary>
        /// Прервать накачку с возвратом части Ци.
        /// </summary>
        private void InterruptCharge(ChargeInterruptReason reason)
        {
            if (!activeCharge.IsActive) return;

            activeCharge.State = ChargeState.Interrupted;
            activeCharge.InterruptReason = reason;

            // Рассчитываем % возврата Ци
            float returnPercent = reason switch
            {
                ChargeInterruptReason.PlayerCancel => QI_RETURN_PLAYER_CANCEL,
                ChargeInterruptReason.DamageInterrupt => QI_RETURN_DAMAGE_INTERRUPT,
                ChargeInterruptReason.StunInterrupt => QI_RETURN_STUN,
                ChargeInterruptReason.DeathInterrupt => QI_RETURN_DEATH,
                ChargeInterruptReason.QiDepleted => QI_RETURN_QI_DEPLETED,
                _ => 0f
            };
            activeCharge.QiReturnPercent = returnPercent;

            // Возвращаем часть Ци
            long qiToReturn = (long)(activeCharge.QiCharged * returnPercent);
            if (qiToReturn > 0 && qiController != null)
            {
                qiController.AddQi(qiToReturn);
            }

            // Отправляем событие
            OnChargeInterrupted?.Invoke(activeCharge, reason);

            Debug.Log($"[TechniqueChargeSystem] Накачка прервана: {reason}, " +
                      $"QiCharged={activeCharge.QiCharged}, возврат={qiToReturn} ({returnPercent * 100}%)");

            // Сбрасываем состояние
            activeCharge = TechniqueChargeData.Empty;
        }

        #endregion

        #region Расчёты

        /// <summary>
        /// Рассчитать время накачки техники.
        ///
        /// Формула:
        /// chargeTime = max(minChargeTime, baseChargeTime × (1 / (1 + cultivationBonus)) × (1 / (1 + masteryBonus)))
        ///
        /// Где:
        /// - minChargeTime = tickInterval / 10 (правило tick/10)
        /// - baseChargeTime = из TechniqueData по типу
        /// - cultivationBonus = (cultivationLevel - 1) × 0.05
        /// - masteryBonus = mastery / 100
        /// </summary>
        private float CalculateChargeTime(LearnedTechnique technique, float minChargeTime)
        {
            float baseChargeTime = GetBaseChargeTime(technique);

            // Бонус от уровня культивации: +5% за уровень выше 1
            int cultivationLevel = qiController?.CultivationLevel ?? 1;
            float cultivationBonus = (cultivationLevel - 1) * 0.05f;

            // Бонус от мастерства: +1% за 1% мастерства
            float masteryBonus = technique.Mastery / 100f;

            // Рассчитываем эффективное время
            float effectiveTime = baseChargeTime / (1f + cultivationBonus) / (1f + masteryBonus);

            // ФАЗА 2: Бонус скорости накачки от оружия
            float chargeBonus = equipmentController?.GetChargeSpeedBonus() ?? 0f;
            if (chargeBonus > 0f)
            {
                effectiveTime /= (1f + chargeBonus);
            }

            // Применяем ограничение tick/10
            return Mathf.Max(minChargeTime, effectiveTime);
        }

        /// <summary>
        /// Получить базовое время накачки по типу техники.
        /// </summary>
        private float GetBaseChargeTime(LearnedTechnique technique)
        {
            // Ultimate техники всегда 3.0 сек
            if (technique.Data.isUltimate) return 3.0f;

            // По подтипу атаки
            return technique.Data.combatSubtype switch
            {
                CombatSubtype.MeleeStrike => 0.5f,
                CombatSubtype.MeleeWeapon => 0.8f,
                CombatSubtype.RangedProjectile => 1.2f,
                CombatSubtype.RangedBeam => 1.5f,
                CombatSubtype.RangedAoe => 2.0f,
                CombatSubtype.DefenseBlock => 0.3f,
                CombatSubtype.DefenseShield => 0.3f,
                CombatSubtype.DefenseDodge => 0.3f,
                _ => GetBaseChargeTimeByType(technique)
            };
        }

        /// <summary>
        /// Базовое время накачки по типу техники (для не-Combat).
        /// </summary>
        private float GetBaseChargeTimeByType(LearnedTechnique technique)
        {
            return technique.Data.techniqueType switch
            {
                TechniqueType.Defense => 0.3f,
                TechniqueType.Healing => 1.0f,
                TechniqueType.Support => 0.8f,
                TechniqueType.Curse => 1.2f,
                TechniqueType.Poison => 1.0f,
                TechniqueType.Formation => 2.0f,
                TechniqueType.Movement => 0.4f,
                TechniqueType.Sensory => 0.6f,
                TechniqueType.Cultivation => 0f, // Пассивная, не накачивается
                _ => 1.0f
            };
        }

        /// <summary>
        /// Получить минимальное время накачки из тиковой системы.
        /// minChargeTime = tickInterval / 10
        /// Fallback: 0.1 сек если TimeController не доступен.
        /// </summary>
        private float GetMinChargeTime()
        {
            var tc = TimeController.Instance;
            if (tc != null)
            {
                return tc.MinChargeTime; // tickInterval / 10
            }
            return 0.1f; // Fallback
        }

        /// <summary>
        /// Рассчитать стоимость Ци для техники.
        /// </summary>
        private long CalculateQiCost(LearnedTechnique technique)
        {
            return TechniqueCapacity.CalculateQiCost(
                technique.Data.baseQiCost,
                technique.Data.techniqueLevel
            );
        }

        /// <summary>
        /// Является ли техника melee (ближний бой)?
        /// Melee позволяет двигаться (0.5× скорость), ranged — нет.
        /// </summary>
        private bool IsMeleeTechnique(LearnedTechnique technique)
        {
            return technique.Data.combatSubtype == CombatSubtype.MeleeStrike
                || technique.Data.combatSubtype == CombatSubtype.MeleeWeapon
                || technique.Data.techniqueType == TechniqueType.Defense
                || technique.Data.techniqueType == TechniqueType.Healing;
        }

        /// <summary>
        /// Рассчитать порог урона для прерывания накачки.
        /// Базовый порог = 10% от MaxHP практика.
        /// </summary>
        private float CalculateInterruptThreshold(LearnedTechnique technique)
        {
            // Техники защиты прерываются труднее
            if (technique.Data.techniqueType == TechniqueType.Defense)
                return float.MaxValue; // Защитные техники не прерываются уроном

            // Ultimate техники прерываются легче
            if (technique.Data.isUltimate)
                return 1f; // Любой урон прерывает

            // Стандартный порог: 5% от MaxHP (упрощённо — используем Qi как прокси)
            long maxQi = qiController?.MaxQi ?? 100;
            return maxQi * 0.05f;
        }

        #endregion
    }
}
