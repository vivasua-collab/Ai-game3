// ============================================================================
// StatDevelopment.cs — Система развития характеристик
// Cultivation World Simulator
// Создано: 2026-03-31 14:25:00 UTC
// Редактировано: 2026-04-11 06:38:02 UTC — CORE-C02/C03/C04/C07: пороги, консолидация, MAX_STAT_VALUE→GameConstants
// ============================================================================
//
// Источник: docs/STAT_THRESHOLD_SYSTEM.md
//
// Ключевые принципы:
// 1. "Нет стадии куколки" — развитие всегда постепенное
// 2. Виртуальная дельта — накопленный прогресс
// 3. Пороги растут с характеристикой: threshold = floor(stat / 10)
// 4. Закрепление только при сне (минимум 4 часа)
//
// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  СИСТЕМА ВИРТУАЛЬНОЙ ДЕЛЬТЫ                                                ║
// ╠═══════════════════════════════════════════════════════════════════════════╣
// ║                                                                            ║
// ║  Действие → AddDelta(stat, amount) → VirtualDelta[stat] += amount          ║
// ║      ↓                                                                     ║
// ║  Сон 4+ часов → ConsolidateSleep(hours)                                    ║
// ║      ↓                                                                     ║
// ║  if (delta >= threshold) Stat += 1                                         ║
// ║                                                                            ║
// ║  Порог: threshold = floor(currentStat / 10)                                ║
// ║  Пример: stat=20 → threshold=2.0 (нужно 2.0 дельты для +1)                ║
// ║                                                                            ║
// ╚═══════════════════════════════════════════════════════════════════════════╝
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
// FIX CORE-C07: MAX_STAT_VALUE теперь в GameConstants

namespace CultivationGame.Core
{
    /// <summary>
    /// Тип характеристики.
    /// </summary>
    public enum StatType
    {
        Strength,       // СИЛ — урон, переносимый вес
        Agility,        // ЛОВ — уклонение, скорость атаки
        Intelligence,   // ИНТ — эффективность техник, обучение
        Vitality        // ЖИВ — HP частей тела, регенерация
    }

    /// <summary>
    /// Результат развития характеристики.
    /// </summary>
    public struct StatDevelopmentResult
    {
        public StatType StatType;
        public float OldValue;
        public float NewValue;
        public int LevelsGained;
        public float DeltaConsumed;
        public float DeltaRemaining;
    }

    /// <summary>
    /// Результат консолидации сна.
    /// </summary>
    public struct SleepConsolidationResult
    {
        public float HoursSlept;
        public List<StatDevelopmentResult> StatResults;
        public float TotalConsolidated;
        public bool WasMinimumSleep;    // >= 4 часов
    }

    /// <summary>
    /// Система развития характеристик с виртуальной дельтой.
    /// 
    /// Источник: docs/STAT_THRESHOLD_SYSTEM.md
    /// 
    /// Механика:
    /// - Виртуальная дельта накапливается от действий
    /// - Закрепление происходит только при сне (минимум 4 часа)
    /// - Пороги растут с характеристикой (линейно)
    /// - Мягкие капы на виртуальную дельту
    /// </summary>
    [Serializable]
    public class StatDevelopment
    {
        #region Constants

        // === КАПЫ ВИРТУАЛЬНОЙ ДЕЛЬТЫ (источник: STAT_THRESHOLD_SYSTEM.md) ===
        public const float MAX_STRENGTH_DELTA = 10.0f;
        public const float MAX_AGILITY_DELTA = 10.0f;
        public const float MAX_INTELLIGENCE_DELTA = 15.0f;
        public const float MAX_VITALITY_DELTA = 10.0f;

        // === ПАРАМЕТРЫ СНА (источник: STAT_THRESHOLD_SYSTEM.md) ===
        public const float MIN_SLEEP_HOURS = 4.0f;          // Минимум для закрепления
        public const float MAX_CONSOLIDATION_PER_SLEEP = 0.20f; // Максимум за сон
        public const float CONSOLIDATION_RATE = 0.025f;     // За час сна

        // === ИСТОЧНИКИ ПРИРОСТА (источник: STAT_THRESHOLD_SYSTEM.md) ===
        // Боевые действия
        public const float DELTA_STRIKE = 0.001f;           // Удар в бою → STR
        public const float DELTA_DODGE = 0.001f;            // Уклонение → AGI
        public const float DELTA_BLOCK = 0.0005f;           // Блок → STR
        public const float DELTA_DAMAGE_TAKEN = 0.001f;     // Получение урона → VIT
        public const float DELTA_TECHNIQUE_USE = 0.0005f;   // Использование техники → INT

        // Тренировки (в минуту)
        public const float DELTA_PHYSICAL_TRAINING = 0.002f / 60f;  // Физическая тренировка
        public const float DELTA_SPARRING = 0.003f / 60f;           // Спарринг (STR + AGI)
        public const float DELTA_MEDITATION = 0.01f / 60f;          // Медитация → INT
        public const float DELTA_BODY_HARDENING = 0.005f / 60f;     // Закалка тела → VIT

        // === БАЗОВЫЕ ХАРАКТЕРИСТИКИ ===
        public const float BASE_STAT_VALUE = 10.0f;
        public const float MIN_STAT_VALUE = 1.0f;
        // FIX CORE-C07: MAX_STAT_VALUE → GameConstants.MAX_STAT_VALUE (1000)
        // Локальная константа убрана, используем единую из GameConstants

        #endregion

        #region Fields

        // === РЕАЛЬНЫЕ ХАРАКТЕРИСТИКИ ===
        // FIX CORE-C04: [SerializeField] убран — не работает в non-MonoBehaviour.
        // [Serializable] обеспечивает сериализацию private полей.
        private float strength = BASE_STAT_VALUE;
        private float agility = BASE_STAT_VALUE;
        private float intelligence = BASE_STAT_VALUE;
        private float vitality = BASE_STAT_VALUE;

        // === ВИРТУАЛЬНЫЕ ДЕЛЬТЫ ===
        private float virtualStrengthDelta = 0f;
        private float virtualAgilityDelta = 0f;
        private float virtualIntelligenceDelta = 0f;
        private float virtualVitalityDelta = 0f;

        // === МОДИФИКАТОРЫ ===
        private float trainingModifier = 1.0f;     // От техник/артефактов
        private float ageModifier = 1.0f;          // От возраста

        #endregion

        #region Events

        public event Action<StatType, float, float> OnStatChanged;           // (type, old, new)
        public event Action<StatType, float> OnDeltaAdded;                   // (type, amount)
        public event Action<StatType, int> OnStatIncreased;                  // (type, levelsGained)
        public event Action<SleepConsolidationResult> OnSleepConsolidated;

        #endregion

        #region Properties

        // === РЕАЛЬНЫЕ ХАРАКТЕРИСТИКИ ===
        public float Strength => strength;
        public float Agility => agility;
        public float Intelligence => intelligence;
        public float Vitality => vitality;

        // === ВИРТУАЛЬНЫЕ ДЕЛЬТЫ ===
        public float StrengthDelta => virtualStrengthDelta;
        public float AgilityDelta => virtualAgilityDelta;
        public float IntelligenceDelta => virtualIntelligenceDelta;
        public float VitalityDelta => virtualVitalityDelta;

        // === МОДИФИКАТОРЫ ===
        public float TrainingModifier 
        { 
            get => trainingModifier; 
            set => trainingModifier = Mathf.Max(0.1f, value); 
        }
        
        public float AgeModifier 
        { 
            get => ageModifier; 
            set => ageModifier = Mathf.Max(0.1f, value); 
        }

        // === ПРОГРЕСС ===
        public float GetStat(StatType type) => type switch
        {
            StatType.Strength => strength,
            StatType.Agility => agility,
            StatType.Intelligence => intelligence,
            StatType.Vitality => vitality,
            _ => BASE_STAT_VALUE
        };

        /// <summary>
        /// Получить все статы как Dictionary для проверок требований.
        /// Редактировано: 2026-04-11 UTC
        /// </summary>
        public Dictionary<string, float> GetAllStatsAsDictionary()
        {
            return new Dictionary<string, float>
            {
                { "Strength", strength },
                { "Agility", agility },
                { "Intelligence", intelligence },
                { "Vitality", vitality }
            };
        }

        public float GetDelta(StatType type) => type switch
        {
            StatType.Strength => virtualStrengthDelta,
            StatType.Agility => virtualAgilityDelta,
            StatType.Intelligence => virtualIntelligenceDelta,
            StatType.Vitality => virtualVitalityDelta,
            _ => 0f
        };

        public float GetMaxDelta(StatType type) => type switch
        {
            StatType.Strength => MAX_STRENGTH_DELTA,
            StatType.Agility => MAX_AGILITY_DELTA,
            StatType.Intelligence => MAX_INTELLIGENCE_DELTA,
            StatType.Vitality => MAX_VITALITY_DELTA,
            _ => 10.0f
        };

        #endregion

        #region Constructor

        public StatDevelopment()
        {
            strength = BASE_STAT_VALUE;
            agility = BASE_STAT_VALUE;
            intelligence = BASE_STAT_VALUE;
            vitality = BASE_STAT_VALUE;
        }

        public StatDevelopment(float str, float agi, float intel, float vit)
        {
            // FIX CORE-C07: используем GameConstants.MAX_STAT_VALUE (1000)
            strength = Mathf.Clamp(str, MIN_STAT_VALUE, GameConstants.MAX_STAT_VALUE);
            agility = Mathf.Clamp(agi, MIN_STAT_VALUE, GameConstants.MAX_STAT_VALUE);
            intelligence = Mathf.Clamp(intel, MIN_STAT_VALUE, GameConstants.MAX_STAT_VALUE);
            vitality = Mathf.Clamp(vit, MIN_STAT_VALUE, GameConstants.MAX_STAT_VALUE);
        }

        #endregion

        #region Add Delta Methods

        /// <summary>
        /// Добавить дельту к характеристике.
        /// </summary>
        /// <param name="type">Тип характеристики</param>
        /// <param name="amount">Количество дельты</param>
        /// <param name="applyModifiers">Применять модификаторы</param>
        public void AddDelta(StatType type, float amount, bool applyModifiers = true)
        {
            if (amount <= 0f) return;

            // Применяем модификаторы
            float finalAmount = applyModifiers 
                ? amount * trainingModifier * ageModifier 
                : amount;

            // Получаем текущую дельту и кап
            float currentDelta = GetDelta(type);
            float maxDelta = GetMaxDelta(type);

            // Ограничиваем капом
            float newDelta = Mathf.Min(currentDelta + finalAmount, maxDelta);
            float actualAdded = newDelta - currentDelta;

            // Применяем
            SetDelta(type, newDelta);

            OnDeltaAdded?.Invoke(type, actualAdded);
        }

        /// <summary>
        /// Добавить дельту от боевого действия.
        /// </summary>
        public void AddCombatDelta(CombatActionType action)
        {
            switch (action)
            {
                case CombatActionType.Strike:
                    AddDelta(StatType.Strength, DELTA_STRIKE);
                    break;
                case CombatActionType.Dodge:
                    AddDelta(StatType.Agility, DELTA_DODGE);
                    break;
                case CombatActionType.Block:
                    AddDelta(StatType.Strength, DELTA_BLOCK);
                    break;
                case CombatActionType.TakeDamage:
                    AddDelta(StatType.Vitality, DELTA_DAMAGE_TAKEN);
                    break;
                case CombatActionType.UseTechnique:
                    AddDelta(StatType.Intelligence, DELTA_TECHNIQUE_USE);
                    break;
            }
        }

        /// <summary>
        /// Добавить дельту от тренировки (за минуту).
        /// </summary>
        public void AddTrainingDelta(StatType type, float minutes, TrainingType trainingType = TrainingType.General)
        {
            float rate = trainingType switch
            {
                TrainingType.Physical => DELTA_PHYSICAL_TRAINING,
                TrainingType.Sparring => DELTA_SPARRING,
                TrainingType.Meditation => DELTA_MEDITATION,
                TrainingType.BodyHardening => DELTA_BODY_HARDENING,
                _ => DELTA_PHYSICAL_TRAINING
            };

            // Спарринг даёт и STR и AGI
            if (trainingType == TrainingType.Sparring)
            {
                AddDelta(StatType.Strength, rate * minutes);
                AddDelta(StatType.Agility, rate * minutes);
            }
            else
            {
                AddDelta(type, rate * minutes);
            }
        }

        #endregion

        #region Threshold Methods

        /// <summary>
        /// Получить порог для повышения характеристики.
        /// FIX CORE-C02: Math.Max(1f, ...) — гарантия порога ≥1 даже при stat < 10.
        /// Формула: threshold = max(1, floor(currentStat / 10))
        /// Источник: STAT_THRESHOLD_SYSTEM.md "Формула порога"
        /// </summary>
        public float GetThreshold(StatType type)
        {
            float currentStat = GetStat(type);
            return Math.Max(1f, Mathf.Floor(currentStat / 10f));
        }

        /// <summary>
        /// Проверить, можно ли повысить характеристику.
        /// FIX CORE-C02: threshold всегда ≥1, проверка threshold > 0 избыточна, но оставлена.
        /// </summary>
        public bool CanAdvance(StatType type)
        {
            float delta = GetDelta(type);
            float threshold = GetThreshold(type);
            return delta >= threshold && threshold > 0f;
        }

        /// <summary>
        /// Получить прогресс до следующего повышения (0-1).
        /// </summary>
        public float GetProgress(StatType type)
        {
            float delta = GetDelta(type);
            float threshold = GetThreshold(type);
            if (threshold <= 0f) return 1f;
            return Mathf.Clamp01(delta / threshold);
        }

        #endregion

        #region Consolidation Methods

        /// <summary>
        /// Закрепить дельты при сне.
        /// Источник: STAT_THRESHOLD_SYSTEM.md "Закрепление при сне"
        /// 
        /// Правила:
        /// 1. Минимум 4 часа для начала закрепления
        /// 2. Максимум закрепления за сон: +0.20 (при 8 часах)
        /// 3. Остаток дельты сохраняется
        /// 
        /// Формула: consolidated = min(virtualDelta, sleepHours * 0.025)
        /// </summary>
        public SleepConsolidationResult ConsolidateSleep(float hours)
        {
            var result = new SleepConsolidationResult
            {
                HoursSlept = hours,
                StatResults = new List<StatDevelopmentResult>(),
                WasMinimumSleep = hours >= MIN_SLEEP_HOURS
            };

            // Если сна недостаточно — закрепления нет
            if (hours < MIN_SLEEP_HOURS)
            {
                OnSleepConsolidated?.Invoke(result);
                return result;
            }

            // Максимум закрепления за сон
            float maxConsolidation = Mathf.Min(hours * CONSOLIDATION_RATE, MAX_CONSOLIDATION_PER_SLEEP);
            result.TotalConsolidated = 0f;

            // Закрепляем каждую характеристику
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                var statResult = ConsolidateStat(type, maxConsolidation);
                if (statResult.LevelsGained > 0)
                {
                    result.StatResults.Add(statResult);
                    result.TotalConsolidated += statResult.DeltaConsumed;
                }
            }

            OnSleepConsolidated?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Закрепить одну характеристику.
        /// </summary>
        private StatDevelopmentResult ConsolidateStat(StatType type, float maxConsolidation)
        {
            float oldValue = GetStat(type);
            var result = new StatDevelopmentResult
            {
                StatType = type,
                OldValue = oldValue,
                NewValue = oldValue,
                LevelsGained = 0,
                DeltaConsumed = 0f,
                DeltaRemaining = GetDelta(type)
            };

            float delta = GetDelta(type);
            float threshold = GetThreshold(type);
            // FIX CORE-C03: availableConsolidation используется для ограничения уровней
            float availableConsolidation = Mathf.Min(delta, maxConsolidation);

            // Если порог достигнут — повышаем характеристику
            if (delta >= threshold && threshold > 0f)
            {
                // FIX CORE-C03: уровни ограничиваются availableConsolidation, не maxConsolidation
                // Сколько уровней можно повысить (обычно 1)
                int levelsPossible = Mathf.FloorToInt(availableConsolidation / threshold);

                if (levelsPossible > 0)
                {
                    // Повышаем (oldValue уже объявлен выше)
                    SetStat(type, oldValue + levelsPossible);
                    
                    // Вычитаем использованную дельту
                    float usedDelta = levelsPossible * threshold;
                    SetDelta(type, delta - usedDelta);

                    result.NewValue = GetStat(type);
                    result.LevelsGained = levelsPossible;
                    result.DeltaConsumed = usedDelta;
                    result.DeltaRemaining = GetDelta(type);

                    OnStatIncreased?.Invoke(type, levelsPossible);
                    OnStatChanged?.Invoke(type, oldValue, result.NewValue);
                }
            }

            return result;
        }

        #endregion

        #region Direct Stat Modification

        /// <summary>
        /// Установить значение характеристики напрямую (для загрузки/дебага).
        /// </summary>
        public void SetStat(StatType type, float value)
        {
            // FIX CORE-C07: используем GameConstants.MAX_STAT_VALUE (1000)
            float clampedValue = Mathf.Clamp(value, MIN_STAT_VALUE, GameConstants.MAX_STAT_VALUE);

            switch (type)
            {
                case StatType.Strength:
                    strength = clampedValue;
                    break;
                case StatType.Agility:
                    agility = clampedValue;
                    break;
                case StatType.Intelligence:
                    intelligence = clampedValue;
                    break;
                case StatType.Vitality:
                    vitality = clampedValue;
                    break;
            }
        }

        /// <summary>
        /// Добавить к характеристике напрямую (для артефактов/эффектов).
        /// </summary>
        public void ModifyStat(StatType type, float amount)
        {
            float current = GetStat(type);
            SetStat(type, current + amount);
        }

        #endregion

        #region Utility

        private void SetDelta(StatType type, float value)
        {
            switch (type)
            {
                case StatType.Strength:
                    virtualStrengthDelta = value;
                    break;
                case StatType.Agility:
                    virtualAgilityDelta = value;
                    break;
                case StatType.Intelligence:
                    virtualIntelligenceDelta = value;
                    break;
                case StatType.Vitality:
                    virtualVitalityDelta = value;
                    break;
            }
        }

        /// <summary>
        /// Сбросить все виртуальные дельты.
        /// </summary>
        public void ResetDeltas()
        {
            virtualStrengthDelta = 0f;
            virtualAgilityDelta = 0f;
            virtualIntelligenceDelta = 0f;
            virtualVitalityDelta = 0f;
        }

        /// <summary>
        /// Получить информацию о прогрессе.
        /// </summary>
        public string GetProgressInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== STAT DEVELOPMENT PROGRESS ===");
            
            foreach (StatType type in Enum.GetValues(typeof(StatType)))
            {
                float stat = GetStat(type);
                float delta = GetDelta(type);
                float threshold = GetThreshold(type);
                float progress = GetProgress(type);
                
                sb.AppendLine($"{type}: {stat:F1} (Δ{delta:F3}/{threshold:F1}, {progress:P0})");
            }
            
            return sb.ToString();
        }

        #endregion
    }

    #region Enums

    /// <summary>
    /// Тип боевого действия.
    /// </summary>
    public enum CombatActionType
    {
        Strike,             // Удар
        Dodge,              // Уклонение
        Block,              // Блок
        TakeDamage,         // Получение урона
        UseTechnique        // Использование техники
    }

    /// <summary>
    /// Тип тренировки.
    /// </summary>
    public enum TrainingType
    {
        General,            // Общая
        Physical,           // Физическая
        Sparring,           // Спарринг
        Meditation,         // Медитация
        BodyHardening       // Закалка тела
    }

    #endregion
}
