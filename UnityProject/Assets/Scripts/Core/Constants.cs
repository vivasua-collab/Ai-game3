// ============================================================================
// Constants.cs — Константы игры
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-03-31 09:49:01 UTC
// ============================================================================

using System;
using System.Collections.Generic;

namespace CultivationGame.Core
{
    /// <summary>
    /// Статический класс с основными константами игры.
    /// Все числовые значения, таблицы и параметры.
    /// 
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  ИСТОЧНИКИ ИСТИНЫ                                                          ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║  • ALGORITHMS.md — формулы и алгоритмы                                     ║
    /// ║  • TECHNIQUE_SYSTEM.md — система техник                                    ║
    /// ║  • EQUIPMENT_SYSTEM.md — система экипировки                                ║
    /// ║  • ENTITY_TYPES.md — типы сущностей и материалы                            ║
    /// ║  • QI_SYSTEM.md — система Ци                                               ║
    /// ║  • BODY_SYSTEM.md — система тела                                           ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public static class GameConstants
    {
        #region Version
        
        public const string VERSION = "0.1.0";
        public const int SAVE_VERSION = 1;
        
        #endregion
        
        #region Character Base Stats
        
        /// <summary>Базовое значение характеристик</summary>
        public const int BASE_STAT_VALUE = 10;
        
        /// <summary>Минимальное значение характеристики</summary>
        public const int MIN_STAT_VALUE = 1;
        
        /// <summary>Максимальное значение характеристики. FIX CORE-C07: 100→1000 по решению пользователя.</summary>
        public const int MAX_STAT_VALUE = 1000;
        
        /// <summary>
        /// Базовая ёмкость ядра (L1.0)
        /// Источник: QI_SYSTEM.md
        /// Формула: coreCapacity = 1000 × 1.1^totalSubLevels
        /// </summary>
        public const int BASE_CORE_CAPACITY = 1000;
        
        /// <summary>
        /// Множитель роста ёмкости ядра
        /// Источник: QI_SYSTEM.md
        /// Формула: coreCapacity = 1000 × 1.1^totalSubLevels
        /// </summary>
        public const float CORE_CAPACITY_GROWTH = 1.1f;
        
        /// <summary>Базовая проводимость</summary>
        public const float BASE_CONDUCTIVITY = 1.0f;
        
        /// <summary>Базовый вес переносимого груза</summary>
        public const float BASE_CARRY_WEIGHT = 50f;
        
        #endregion
        
        #region Mortal Stages (до культивации)
        
        /// <summary>
        /// Формирование дремлющего ядра по этапам смертного (%)
        /// </summary>
        public static readonly Dictionary<MortalStage, (float min, float max)> DormantCoreFormation = new Dictionary<MortalStage, (float min, float max)>
        {
            { MortalStage.None, (0f, 0f) },
            { MortalStage.Newborn, (0f, 0.3f) },      // 0-30%
            { MortalStage.Child, (0.3f, 0.6f) },      // 30-60%
            { MortalStage.Adult, (0.6f, 0.9f) },      // 60-90%
            { MortalStage.Mature, (0.9f, 1.0f) },     // 90-100%
            { MortalStage.Elder, (0.5f, 0.8f) },      // Угасание
            { MortalStage.Awakening, (0.8f, 1.0f) }   // Точка пробуждения
        };
        
        /// <summary>
        /// Максимальная естественная Ци для смертных
        /// </summary>
        public static readonly Dictionary<MortalStage, int> MaxMortalQi = new Dictionary<MortalStage, int>
        {
            { MortalStage.None, 0 },
            { MortalStage.Newborn, 30 },
            { MortalStage.Child, 100 },
            { MortalStage.Adult, 200 },
            { MortalStage.Mature, 150 },
            { MortalStage.Elder, 80 },
            { MortalStage.Awakening, 250 }
        };
        
        /// <summary>
        /// Шанс естественного пробуждения (%)
        /// </summary>
        public static readonly Dictionary<MortalStage, float> AwakeningChance = new Dictionary<MortalStage, float>
        {
            { MortalStage.None, 0f },
            { MortalStage.Newborn, 0f },
            { MortalStage.Child, 0.001f },    // 0.001%
            { MortalStage.Adult, 0.1f },      // 0.1%
            { MortalStage.Mature, 1f },       // 1%
            { MortalStage.Elder, 0.5f },      // 0.5%
            { MortalStage.Awakening, 5f }     // 5%
        };
        
        /// <summary>
        /// Возрастные диапазоны для этапов смертного
        /// </summary>
        public static readonly Dictionary<MortalStage, (int min, int max)> AgeRanges = new Dictionary<MortalStage, (int min, int max)>
        {
            { MortalStage.Newborn, (0, 7) },
            { MortalStage.Child, (7, 16) },
            { MortalStage.Adult, (16, 30) },
            { MortalStage.Mature, (30, 50) },
            { MortalStage.Elder, (50, 100) }
        };
        
        /// <summary>
        /// Множители шанса пробуждения по типу
        /// </summary>
        public static readonly Dictionary<AwakeningType, float> AwakeningTypeMultipliers = new Dictionary<AwakeningType, float>
        {
            { AwakeningType.None, 0f },
            { AwakeningType.Natural, 0.01f },      // Базовый 0.01%
            { AwakeningType.Guided, 0.6f },        // 60% с учителем
            { AwakeningType.Artifact, 0.3f },      // 30% с артефактом
            { AwakeningType.Forced, 0.4f }         // 40% насильственно (с рисками)
        };
        
        /// <summary>Минимальная сформированность ядра для пробуждения</summary>
        public const float MIN_DORMANT_CORE_FOR_AWAKENING = 0.8f; // 80%
        
        /// <summary>Оптимальный возраст для пробуждения (начало)</summary>
        public const int OPTIMAL_AWAKENING_AGE_MIN = 16;
        
        /// <summary>Оптимальный возраст для пробуждения (конец)</summary>
        public const int OPTIMAL_AWAKENING_AGE_MAX = 40;
        
        #endregion
        
        #region Cultivation Levels
        
        /// <summary>Максимальный уровень культивации</summary>
        public const int MAX_CULTIVATION_LEVEL = 10;
        
        /// <summary>Максимум под-уровней</summary>
        public const int MAX_SUB_LEVEL = 9;
        
        /// <summary>Ци для малого прорыва (под-уровень)</summary>
        public const float SMALL_BREAKTHROUGH_MULTIPLIER = 10f;
        
        /// <summary>Ци для большого прорыва (основной уровень)</summary>
        public const float BIG_BREAKTHROUGH_MULTIPLIER = 100f;
        
        /// <summary>Генерация микроядром (% от ёмкости в сутки)</summary>
        public const float MICROCORE_GENERATION_RATE = 0.1f;
        
        /// <summary>
        /// Плотность Ци по уровням (Qi Density = 2^(level-1))
        /// Источник: ALGORITHMS.md §3.3 "Плотность Ци"
        /// </summary>
        public static readonly int[] QiDensityByLevel = new int[]
        {
            1,      // L1
            2,      // L2
            4,      // L3
            8,      // L4
            16,     // L5
            32,     // L6
            64,     // L7
            128,    // L8
            256,    // L9
            512     // L10
        };
        
        /// <summary>
        /// Множители старения по уровням
        /// </summary>
        public static readonly float[] AgingMultipliers = new float[]
        {
            1.0f,   // L1
            1.0f,   // L2
            0.9f,   // L3
            0.4f,   // L4
            0.3f,   // L5
            0.1f,   // L6
            0.0f,   // L7 — старение остановлено
            0.0f,   // L8
            0.0f,   // L9
            0.0f    // L10
        };
        
        /// <summary>
        /// Множители регенерации по уровням
        /// </summary>
        public static readonly float[] RegenerationMultipliers = new float[]
        {
            1.1f,   // L1
            2.0f,   // L2
            3.0f,   // L3
            5.0f,   // L4
            8.0f,   // L5
            15.0f,  // L6
            30.0f,  // L7
            100.0f, // L8
            1000.0f,// L9
            float.MaxValue    // L10 — FIX CORE-C06: PositiveInfinity→MaxValue. Обработка: мгновенное восстановление = MaxValue
        };
        
        #endregion
        
        #region Combat - Level Suppression
        
        /// <summary>
        /// Таблица подавления уровнем.
        /// Источник: ALGORITHMS.md §1.3 "Таблица подавления"
        /// [разница уровней][тип атаки: 0=normal, 1=technique, 2=ultimate]
        /// </summary>
        public static readonly float[][] LevelSuppressionTable = new float[][]
        {
            new float[] { 1.0f, 1.0f, 1.0f },    // Разница 0
            new float[] { 0.5f, 0.75f, 1.0f },   // Разница 1
            new float[] { 0.1f, 0.25f, 0.5f },   // Разница 2
            new float[] { 0.0f, 0.05f, 0.25f },  // Разница 3
            new float[] { 0.0f, 0.0f, 0.1f },    // Разница 4
            new float[] { 0.0f, 0.0f, 0.0f }     // Разница 5+
        };
        
        /// <summary>Максимальная разница уровней для таблицы</summary>
        public const int MAX_LEVEL_DIFF = 5;
        
        #endregion
        
        #region Combat - Qi Buffer
        
        /// <summary>
        /// Поглощение сырой Ци для техник Ци (%)
        /// Источник: ALGORITHMS.md §2.3 "Техники Ци"
        /// </summary>
        public const float RAW_QI_ABSORPTION = 0.9f;
        
        /// <summary>
        /// Пробивающий урон сырой Ци для техник Ци (%)
        /// Источник: ALGORITHMS.md §2.3 "Техники Ци"
        /// </summary>
        public const float RAW_QI_PIERCING = 0.1f;
        
        /// <summary>
        /// Соотношение Ци:Урон для сырой Ци (техники Ци)
        /// Источник: ALGORITHMS.md §2.3 "Техники Ци"
        /// </summary>
        public const float RAW_QI_RATIO = 3.0f;
        
        /// <summary>
        /// Соотношение Ци:Урон для щитовой техники
        /// Источник: ALGORITHMS.md §2.3 "Техники Ци"
        /// </summary>
        public const float SHIELD_QI_RATIO = 1.0f;
        
        /// <summary>Минимальное Ци для активации буфера</summary>
        public const int MIN_QI_FOR_BUFFER = 10;
        
        #endregion
        
        #region Combat - Technique Capacity
        
        /// <summary>
        /// Базовая ёмкость техник по типу
        /// Источник: ALGORITHMS.md §3.1 "Базовая ёмкость по типу"
        /// </summary>
        public static readonly Dictionary<TechniqueType, int> BaseCapacityByType = new Dictionary<TechniqueType, int>
        {
            { TechniqueType.Formation, 80 },
            { TechniqueType.Defense, 72 },
            { TechniqueType.Combat, 64 },     // Переопределяется подтипом
            { TechniqueType.Support, 56 },
            { TechniqueType.Healing, 56 },
            { TechniqueType.Movement, 40 },
            { TechniqueType.Curse, 40 },
            { TechniqueType.Poison, 40 },
            { TechniqueType.Sensory, 32 },
            { TechniqueType.Cultivation, 0 }  // Пассивная, не используется
        };
        
        /// <summary>
        /// Базовая ёмкость по подтипу атаки
        /// Источник: ALGORITHMS.md §3.1
        /// </summary>
        public static readonly Dictionary<CombatSubtype, int> BaseCapacityBySubtype = new Dictionary<CombatSubtype, int>
        {
            { CombatSubtype.MeleeStrike, 64 },
            { CombatSubtype.MeleeWeapon, 48 },
            { CombatSubtype.RangedProjectile, 32 },
            { CombatSubtype.RangedBeam, 32 },
            { CombatSubtype.RangedAoe, 32 },
            { CombatSubtype.DefenseBlock, 72 },
            { CombatSubtype.DefenseShield, 72 },
            { CombatSubtype.DefenseDodge, 72 }
        };
        
        #endregion
        
        #region Combat - Grade Multipliers
        
        /// <summary>
        /// Множители урона по грейду техники.
        /// 
        /// Источник: TECHNIQUE_SYSTEM.md §"Система Grade (Качество)"
        /// 
        /// ╔═══════════════════════════════════════════════════════════════════════════╗
        /// ║  ВАЖНО: Стоимость Ци всегда ×1.0 — не зависит от Grade!                    ║
        /// ║  Источник: TECHNIQUE_SYSTEM.md                                             ║
        /// ╚═══════════════════════════════════════════════════════════════════════════╝
        /// 
        /// | Grade        | Урон |
        /// |--------------|------|
        /// | Common       | ×1.0 |
        /// | Refined      | ×1.2 |
        /// | Perfect      | ×1.4 |
        /// | Transcendent | ×1.6 |
        /// </summary>
        public static readonly Dictionary<TechniqueGrade, float> TechniqueGradeMultipliers = new Dictionary<TechniqueGrade, float>
        {
            { TechniqueGrade.Common, 1.0f },
            { TechniqueGrade.Refined, 1.2f },
            { TechniqueGrade.Perfect, 1.4f },
            { TechniqueGrade.Transcendent, 1.6f }
        };
        
        /// <summary>
        /// Множители параметров по грейду экипировки
        /// Источник: EQUIPMENT_SYSTEM.md §2.1 "Уровни качества"
        /// </summary>
        public static readonly Dictionary<EquipmentGrade, float> EquipmentGradeMultipliers = new Dictionary<EquipmentGrade, float>
        {
            { EquipmentGrade.Damaged, 0.5f },
            { EquipmentGrade.Common, 1.0f },
            { EquipmentGrade.Refined, 1.5f },
            { EquipmentGrade.Perfect, 2.5f },
            { EquipmentGrade.Transcendent, 4.0f }
        };
        
        /// <summary>
        /// Множитель урона Ultimate-техники.
        /// Источник: TECHNIQUE_SYSTEM.md §"Ultimate-техники"
        /// "Множитель урона: ×1.3"
        /// </summary>
        public const float ULTIMATE_DAMAGE_MULTIPLIER = 1.3f;
        
        /// <summary>
        /// Множитель стоимости Ци Ultimate-техники.
        /// Источник: TECHNIQUE_SYSTEM.md §"Ultimate-техники"
        /// "Множитель стоимости Ци: ×1.5"
        /// </summary>
        public const float ULTIMATE_QI_COST_MULTIPLIER = 1.5f;
        
        #endregion
        
        #region Combat - Defense Pipeline
        
        /// <summary>
        /// Максимальное снижение урона бронёй (%)
        /// Источник: ALGORITHMS.md §5.2 "Слой 6-7"
        /// </summary>
        public const float MAX_DAMAGE_REDUCTION = 0.8f;
        
        /// <summary>
        /// Снижение урона по материалу тела
        /// Источник: ENTITY_TYPES.md §5 "Материалы тела"
        /// </summary>
        public static readonly Dictionary<BodyMaterial, float> BodyMaterialReduction = new Dictionary<BodyMaterial, float>
        {
            { BodyMaterial.Organic, 0.0f },
            { BodyMaterial.Scaled, 0.3f },
            { BodyMaterial.Chitin, 0.2f },
            { BodyMaterial.Mineral, 0.5f },
            { BodyMaterial.Ethereal, 0.7f },
            { BodyMaterial.Construct, 0.4f },    // FIX CORE-C05: Construct добавлен (30-50% → 0.4 среднее)
            { BodyMaterial.Chaos, 0.4f }
        };
        
        /// <summary>
        /// Твёрдость материалов тела
        /// Источник: ENTITY_TYPES.md §5 "Материалы тела"
        /// </summary>
        public static readonly Dictionary<BodyMaterial, int> BodyMaterialHardness = new Dictionary<BodyMaterial, int>
        {
            { BodyMaterial.Organic, 3 },
            { BodyMaterial.Scaled, 6 },
            { BodyMaterial.Chitin, 5 },
            { BodyMaterial.Mineral, 8 },
            { BodyMaterial.Ethereal, 1 },
            { BodyMaterial.Construct, 7 },    // FIX CORE-C05: Construct добавлен (5-8 → 7 среднее)
            { BodyMaterial.Chaos, 5 }
        };
        
        #endregion
        
        #region Combat - Body Part Hit Chances
        
        /// <summary>
        /// Базовые шансы попадания по частям тела (гуманоид)
        /// Источник: ALGORITHMS.md §8.1 "Базовые шансы (гуманоид)"
        /// 
        /// | Часть тела | Шанс |
        /// |------------|------|
        /// | head       | 5%   |
        /// | torso      | 40%  |
        /// | heart      | 2%   |
        /// | left_arm   | 10%  |
        /// | right_arm  | 10%  |
        /// | left_leg   | 12%  |
        /// | right_leg  | 12%  |
        /// | left_hand  | 4%   |
        /// | right_hand | 4%   |
        /// | left_foot  | 0.5% |
        /// | right_foot | 0.5% |
        /// | ИТОГО      | 100% |
        /// </summary>
        public static readonly Dictionary<BodyPartType, float> BodyPartHitChances = new Dictionary<BodyPartType, float>
        {
            { BodyPartType.Head, 0.05f },
            { BodyPartType.Torso, 0.40f },
            { BodyPartType.Heart, 0.02f },
            { BodyPartType.LeftArm, 0.10f },
            { BodyPartType.RightArm, 0.10f },
            { BodyPartType.LeftLeg, 0.12f },
            { BodyPartType.RightLeg, 0.12f },
            { BodyPartType.LeftHand, 0.04f },
            { BodyPartType.RightHand, 0.04f },
            { BodyPartType.LeftFoot, 0.005f },
            { BodyPartType.RightFoot, 0.005f }
        };
        
        #endregion
        
        #region Combat - Damage Distribution
        
        /// <summary>
        /// Доля урона на красную HP (функциональная)
        /// Источник: ALGORITHMS.md §9.1 "Формула"
        /// </summary>
        public const float RED_HP_RATIO = 0.7f;
        
        /// <summary>
        /// Доля урона на чёрную HP (структурная)
        /// Источник: ALGORITHMS.md §9.1 "Формула"
        /// </summary>
        public const float BLACK_HP_RATIO = 0.3f;
        
        /// <summary>
        /// Множитель структурной HP от функциональной
        /// Источник: BODY_SYSTEM.md "Соотношение HP"
        /// "Структурная HP = Функциональная HP × 2"
        /// </summary>
        public const float STRUCTURAL_HP_MULTIPLIER = 2.0f;
        
        #endregion
        
        #region Soft Caps
        
        /// <summary>
        /// Конфигурация мягких капов
        /// Источник: ALGORITHMS.md §6 "Мягкие капы (Soft Caps)"
        /// </summary>
        public static class SoftCaps
        {
            // Скорость
            public const float SPEED_CAP = 0.5f;          // ±50%
            public const float SPEED_DECAY = 1.5f;
            
            // Скорость атаки
            public const float ATTACK_SPEED_CAP = 0.75f;  // +75%
            public const float ATTACK_SPEED_DECAY = 1.2f;
            
            // Урон
            public const float DAMAGE_CAP = 1.0f;         // +100%
            public const float DAMAGE_DECAY = 1.0f;
            
            // Критический шанс
            public const float CRIT_CHANCE_CAP = 0.5f;    // +50%
            public const float CRIT_CHANCE_DECAY = 0.8f;
            
            // Критический урон
            public const float CRIT_DAMAGE_CAP = 1.5f;    // +150%
            public const float CRIT_DAMAGE_DECAY = 1.0f;
            
            // Защита
            public const float DEFENSE_CAP = 0.8f;        // +80%
            public const float DEFENSE_DECAY = 1.2f;
            
            // Броня
            public const float ARMOR_CAP = 200f;          // +200
            public const float ARMOR_DECAY = 1.5f;
            
            // Стоимость Ци (отрицательный кап)
            public const float QI_COST_CAP = -0.5f;       // -50%
            public const float QI_COST_DECAY = 1.0f;
            
            // Эффективность Ци
            public const float QI_EFFICIENCY_CAP = 0.5f;  // +50%
            public const float QI_EFFICIENCY_DECAY = 1.0f;
            
            // Кулдаун (отрицательный кап)
            public const float COOLDOWN_CAP = -0.6f;      // -60%
            public const float COOLDOWN_DECAY = 1.2f;
            
            // Вампиризм
            public const float LIFESTEAL_CAP = 0.3f;      // +30%
            public const float LIFESTEAL_DECAY = 0.8f;
        }
        
        #endregion
        
        #region Time System
        
        /// <summary>Минут в часе</summary>
        public const int MINUTES_PER_HOUR = 60;
        
        /// <summary>Часов в сутках</summary>
        public const int HOURS_PER_DAY = 24;
        
        /// <summary>Дней в месяце</summary>
        public const int DAYS_PER_MONTH = 30;
        
        /// <summary>Месяцев в году</summary>
        public const int MONTHS_PER_YEAR = 12;
        
        /// <summary>Тиков в минуте игрового времени</summary>
        public const int TICKS_PER_MINUTE = 1;
        
        /// <summary>
        /// Множители скорости времени
        /// </summary>
        public static readonly Dictionary<TimeSpeed, float> TimeSpeedMultipliers = new Dictionary<TimeSpeed, float>
        {
            { TimeSpeed.Paused, 0f },
            { TimeSpeed.Normal, 1f },
            { TimeSpeed.Fast, 5f },
            { TimeSpeed.VeryFast, 15f }
        };
        
        #endregion
        
        #region Durability
        
        /// <summary>
        /// Состояния прочности по диапазону
        /// Источник: EQUIPMENT_SYSTEM.md §4.1 "Состояния"
        /// </summary>
        public static readonly Dictionary<DurabilityCondition, (float min, float max)> DurabilityRanges = new Dictionary<DurabilityCondition, (float min, float max)>
        {
            { DurabilityCondition.Pristine, (1.0f, 1.0f) },
            { DurabilityCondition.Excellent, (0.8f, 0.99f) },
            { DurabilityCondition.Good, (0.6f, 0.79f) },
            { DurabilityCondition.Worn, (0.4f, 0.59f) },
            { DurabilityCondition.Damaged, (0.2f, 0.39f) },
            { DurabilityCondition.Broken, (0.0f, 0.19f) }
        };
        
        /// <summary>
        /// Эффективность по состоянию прочности
        /// Источник: EQUIPMENT_SYSTEM.md §4.1 "Состояния"
        /// </summary>
        public static readonly Dictionary<DurabilityCondition, float> DurabilityEfficiency = new Dictionary<DurabilityCondition, float>
        {
            { DurabilityCondition.Pristine, 1.0f },
            { DurabilityCondition.Excellent, 0.95f },
            { DurabilityCondition.Good, 0.85f },
            { DurabilityCondition.Worn, 0.70f },
            { DurabilityCondition.Damaged, 0.50f },
            { DurabilityCondition.Broken, 0.20f }
        };
        
        #endregion
        
        #region Elements
        
        /// <summary>
        /// Противоположные элементы (Variant A — решение пользователя)
        /// Fire ↔ Water, Earth ↔ Air, Lightning ↔ Void
        /// </summary>
        public static readonly Dictionary<Element, Element> OppositeElements = new Dictionary<Element, Element>
        {
            { Element.Fire, Element.Water },
            { Element.Water, Element.Fire },
            { Element.Earth, Element.Air },
            { Element.Air, Element.Earth },
            { Element.Lightning, Element.Void },      // FIX CMB-C01: Variant A
            { Element.Void, Element.Lightning }        // FIX CMB-C01: Variant A
        };
        
        /// <summary>
        /// Множитель урона при атаке противоположного элемента
        /// Источник: ALGORITHMS.md §10.2 "Множители эффективности атаки"
        /// </summary>
        public const float OPPOSITE_ELEMENT_MULTIPLIER = 1.5f;
        
        /// <summary>
        /// Множитель урона при сродстве элементов
        /// Источник: ALGORITHMS.md §10.2 "Множители эффективности атаки"
        /// </summary>
        public const float AFFINITY_ELEMENT_MULTIPLIER = 0.8f;
        
        /// <summary>
        /// Множитель урона Void по всем элементам
        /// Источник: ALGORITHMS.md §10.2 "Множители эффективности атаки"
        /// </summary>
        public const float VOID_ELEMENT_MULTIPLIER = 1.2f;
        
        /// <summary>
        /// Множитель урона Fire по Poison (выжигание токсинов, одностороннее)
        /// FIX CMB-C01: Variant A — Fire → Poison ×1.2
        /// </summary>
        public const float FIRE_TO_POISON_MULTIPLIER = 1.2f;
        
        #endregion
        
        #region Item Rarity
        
        /// <summary>
        /// Шанс выпадения по редкости
        /// </summary>
        public static readonly Dictionary<ItemRarity, float> RarityDropChances = new Dictionary<ItemRarity, float>
        {
            { ItemRarity.Common, 0.50f },
            { ItemRarity.Uncommon, 0.30f },
            { ItemRarity.Rare, 0.15f },
            { ItemRarity.Epic, 0.04f },
            { ItemRarity.Legendary, 0.01f },
            { ItemRarity.Mythic, 0.001f }
        };
        
        #endregion
        
        #region Inventory
        
        /// <summary>Ширина сетки инвентаря</summary>
        public const int INVENTORY_WIDTH = 7;
        
        /// <summary>Высота сетки инвентаря</summary>
        public const int INVENTORY_HEIGHT = 7;
        
        /// <summary>Максимальный размер предмета по ширине</summary>
        public const int MAX_ITEM_WIDTH = 2;
        
        /// <summary>Максимальный размер предмета по высоте</summary>
        public const int MAX_ITEM_HEIGHT = 3;
        
        #endregion
        
        #region NPC
        
        /// <summary>Максимальное расстояние обнаружения NPC</summary>
        public const float NPC_DETECTION_RANGE = 20f;
        
        /// <summary>Частота обновления AI (секунды)</summary>
        public const float AI_UPDATE_INTERVAL = 0.5f;
        
        /// <summary>Расстояние для взаимодействия</summary>
        public const float INTERACTION_DISTANCE = 2f;
        
        #endregion
        
        #region Save System
        
        /// <summary>Интервал автосохранения (в тиках)</summary>
        public const int AUTO_SAVE_INTERVAL = 60;
        
        /// <summary>Максимум слотов сохранений</summary>
        public const int MAX_SAVE_SLOTS = 5;
        
        /// <summary>Расширение файлов сохранения</summary>
        public const string SAVE_FILE_EXTENSION = ".sav";
        
        #endregion
    }
}
