// ============================================================================
// Enums.cs — Все перечисления проекта
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-03-31 09:54:21 UTC
// ============================================================================

using System;

namespace CultivationGame.Core
{
    #region Cultivation
    
    /// <summary>
    /// Этапы развития смертного (до культивации)
    /// </summary>
    public enum MortalStage
    {
        None = 0,           // Не применимо (практик)
        Newborn = 1,        // Новорождённый (0-7 лет)
        Child = 2,          // Ребёнок (7-16 лет)
        Adult = 3,          // Взрослый (16-30 лет)
        Mature = 4,         // Зрелый (30-50 лет)
        Elder = 5,          // Старец (50+ лет)
        Awakening = 9       // Точка пробуждения
    }
    
    /// <summary>
    /// Уровни культивации (1-10)
    /// </summary>
    public enum CultivationLevel
    {
        None = 0,               // Смертный (без ядра)
        AwakenedCore = 1,       // Пробуждённое Ядро
        LifeFlow = 2,           // Течение Жизни
        InternalFire = 3,       // Пламя Внутреннего Огня
        BodySpiritUnion = 4,    // Объединение Тела и Духа
        HeartOfHeaven = 5,      // Сердце Небес
        VeilBreaker = 6,        // Разрыв Пелены
        EternalRing = 7,        // Вечное Кольцо
        VoiceOfHeaven = 8,      // Глас Небес
        ImmortalCore = 9,       // Бессмертное Ядро
        Ascension = 10          // Вознесение
    }
    
    /// <summary>
    /// Тип пробуждения ядра
    /// </summary>
    public enum AwakeningType
    {
        None,               // Не пробуждён
        Natural,            // Естественное (спонтанное)
        Guided,             // Направленное (с учителем)
        Artifact,           // Артефактное (пилюля/камень)
        Forced              // Насильственное (рискованное)
    }
    
    /// <summary>
    /// Качество ядра культивации
    /// </summary>
    public enum CoreQuality
    {
        Fragmented = 1,     // Осколочное
        Cracked = 2,        // Треснутое
        Flawed = 3,         // С изъяном
        Normal = 4,         // Нормальное
        Refined = 5,        // Очищенное
        Perfect = 6,        // Совершенное
        Transcendent = 7    // Трансцендентное
    }
    
    #endregion
    
    #region Elements
    
    /// <summary>
    /// Элементы (стихии).
    /// Источник: ELEMENTS_SYSTEM.md
    /// 
    /// Стихии (8 элементов):
    /// - neutral (Нейтральный) — чистый Ци
    /// - fire (Огонь) — горение, DoT
    /// - water (Вода) — замедление, контроль
    /// - earth (Земля) — оглушение, стан
    /// - air (Воздух) — отталкивание
    /// - lightning (Молния) — цепной урон
    /// - void (Пустота) — пробитие, антимагия
    /// - poison (Яд) — DoT, дебаффы (особая стихия)
    /// </summary>
    public enum Element
    {
        Neutral,    // Нейтральный
        Fire,       // Огонь
        Water,      // Вода
        Earth,      // Земля
        Air,        // Воздух
        Lightning,  // Молния
        Void,       // Пустота
        Poison,     // Яд (особая стихия)
        Count       // Количество элементов
    }
    
    /// <summary>
    /// Тип урона
    /// </summary>
    public enum DamageType
    {
        Physical,       // Физический
        Qi,             // Ци
        Elemental,      // Элементальный
        Pure,           // Чистый (игнорирует защиту)
        Void            // Пустотный
    }
    
    #endregion
    
    #region Techniques
    
    /// <summary>
    /// Тип техники
    /// Источник: TECHNIQUE_SYSTEM.md §"Типы техник"
    /// 
    /// | Тип | Описание |
    /// |-----|----------|
    /// | Combat | Боевые |
    /// | Cultivation | Культивация (пассивная) |
    /// | Defense | Защитные |
    /// | Support | Поддержка |
    /// | Healing | Исцеление (element=neutral ТОЛЬКО) |
    /// | Movement | Перемещение |
    /// | Sensory | Восприятие |
    /// | Curse | Проклятия |
    /// | Poison | Яды (element=poison ТОЛЬКО) |
    /// | Formation | Формации |
    /// </summary>
    public enum TechniqueType
    {
        Combat,         // Боевая
        Cultivation,    // Культивация
        Defense,        // Защитная
        Support,        // Поддержка
        Healing,        // Исцеление
        Movement,       // Перемещение
        Sensory,        // Восприятие
        Curse,          // Проклятие
        Poison,         // Яд
        Formation       // Формация
    }
    
    /// <summary>
    /// Подтип боевой техники
    /// Источник: TECHNIQUE_SYSTEM.md §"Combat (Боевые)"
    /// </summary>
    public enum CombatSubtype
    {
        None,
        MeleeStrike,        // Удар телом
        MeleeWeapon,        // Удар с оружием
        RangedProjectile,   // Снаряд
        RangedBeam,         // Луч
        RangedAoe,          // Область
        DefenseBlock,       // Блок
        DefenseShield,      // Щит
        DefenseDodge        // Уклонение
    }
    
    /// <summary>
    /// Грейд техники (качество)
    /// Источник: TECHNIQUE_SYSTEM.md §"Система Grade (Качество)"
    /// 
    /// | Grade | Урон | Бонусов | Шанс эффекта |
    /// |-------|------|---------|--------------|
    /// | Common | ×1.0 | 0 | 0% |
    /// | Refined | ×1.2 | 1 | 20% |
    /// | Perfect | ×1.4 | 2 | 50% |
    /// | Transcendent | ×1.6 | 3 | 80% |
    /// 
    /// ⚠️ ВАЖНО: Стоимость Ци всегда ×1.0 — не зависит от Grade!
    /// </summary>
    public enum TechniqueGrade
    {
        Common,         // Обычная (×1.0)
        Refined,        // Очищенная (×1.2)
        Perfect,        // Совершенная (×1.4)
        Transcendent    // Трансцендентная (×1.6)
    }
    
    #endregion
    
    #region Body
    
    /// <summary>
    /// Тип души (первичная классификация существ).
    /// Источник: ENTITY_TYPES.md §2 "Уровень 1: SoulType"
    /// 
    /// Иерархия: SoulType (L1) → Morphology (L2) → Species (L3)
    /// 
    /// | SoulType | Body | Qi | Mind | Описание |
    /// |----------|------|-----|------|----------|
    /// | character | ✅ organic | ✅ core | ✅ full | Разумные существа |
    /// | creature | ✅ organic/scaled | ✅ core | ⚠️ instinct | Животные, звери |
    /// | spirit | ❌ / ethereal | ✅ reservoir | ✅ full | Бесплотные сущности |
    /// | artifact | ✅ mineral | ✅ reservoir | ⚠️ simple | Разумные предметы |
    /// | construct | ✅ construct/mineral | ✅ reservoir | ⚠️ simple | Искусственные создания |
    /// </summary>
    public enum SoulType
    {
        Character,      // Персонаж (органика + полное сознание)
        Creature,       // Существо (органика + инстинкты)
        Spirit,         // Дух (эфирное тело + сознание)
        Artifact,       // Артефакт (минерал + простое сознание)
        Construct       // Конструкт (искусственное тело)
    }
    
    /// <summary>
    /// Морфология тела (внешняя форма).
    /// Источник: ENTITY_TYPES.md §3 "Уровень 2: Morphology"
    /// 
    /// | Morphology | Описание | Части тела | Примеры |
    /// |------------|----------|------------|---------|
    /// | humanoid | Двурукое двуногое | 11 + сердце | Человек, Эльф |
    /// | quadruped | Четвероногое | 8 + сердце | Волк, Тигр |
    /// | bird | Крылатое | 6-7 + сердце | Орёл, Феникс |
    /// | serpentine | Змееподобное | 6 + сегменты | Змея, Ламия |
    /// | arthropod | Членистоногое | Экзоскелет | Паук, Скорпион |
    /// | amorphous | Бесформенное | 2 (core + essence) | Призрак |
    /// | hybrid_centaur | Кентавр | 12 + сердце | Кентавр |
    /// | hybrid_mermaid | Русалка | 8 + сердце | Русалка |
    /// | hybrid_harpy | Гарпия | 9 + сердце | Гарпия |
    /// | hybrid_lamia | Ламия | 8 + сегменты | Ламия |
    /// </summary>
    public enum Morphology
    {
        Humanoid,       // Гуманоид (2 руки, 2 ноги)
        Quadruped,      // Четвероногое
        Bird,           // Крылатое
        Serpentine,     // Змееподобное
        Arthropod,      // Членистоногое
        Amorphous,      // Бесформенное
        HybridCentaur,  // Кентавр
        HybridMermaid,  // Русалка
        HybridHarpy,    // Гарпия
        HybridLamia     // Ламия
    }
    
    /// <summary>
    /// Материал тела.
    /// Источник: ENTITY_TYPES.md §5 "Материалы тела"
    /// 
    /// | Материал | Твёрдость | Снижение урона |
    /// |----------|-----------|----------------|
    /// | organic | 3 | 0% |
    /// | scaled | 6 | 30% |
    /// | chitin | 5 | 20% |
    /// | ethereal | 1 | 70% физики |
    /// | mineral | 8 | 50% |
    /// | construct | 5-8 | 30-50% |
    /// | chaos | 5 | Переменно |
    /// </summary>
    public enum BodyMaterial
    {
        Organic,        // Органика (снижение 0%)
        Scaled,         // Чешуя (снижение 30%)
        Chitin,         // Хитин (снижение 20%)
        Mineral,        // Минерал (снижение 50%)
        Ethereal,       // Эфир (снижение 70% физики)
        Construct,      // Конструкт (снижение 30-50%)
        Chaos           // Хаос (переменное)
    }
    
    /// <summary>
    /// Часть тела (гуманоид).
    /// Источник: ALGORITHMS.md §8.1 "Базовые шансы (гуманоид)"
    /// </summary>
    public enum BodyPartType
    {
        Head,           // Голова
        Torso,          // Торс
        Heart,          // Сердце
        LeftArm,        // Левая рука
        RightArm,       // Правая рука
        LeftLeg,        // Левая нога
        RightLeg,       // Правая нога
        LeftHand,       // Левая кисть
        RightHand,      // Правая кисть
        LeftFoot,       // Левая стопа
        RightFoot       // Правая стопа
    }
    
    /// <summary>
    /// Состояние части тела.
    /// Источник: BODY_SYSTEM.md "Механика повреждения"
    /// 
    /// - Healthy: оба типа HP полны
    /// - Bruised: функциональная HP снижена (<70%)
    /// - Wounded: функциональная HP < 30%
    /// - Disabled: функциональная HP = 0 (паралич)
    /// - Severed: структурная HP = 0 (отрублена)
    /// - Destroyed: полностью уничтожена
    /// </summary>
    public enum BodyPartState
    {
        Healthy,        // Здорова
        Bruised,        // Ушиблена
        Wounded,        // Ранена
        Disabled,       // Парализована (красная HP = 0)
        Severed,        // Отрублена (чёрная HP = 0)
        Destroyed       // Уничтожена
    }
    
    #endregion
    
    #region Equipment
    
    /// <summary>
    /// Слот экипировки
    /// </summary>
    public enum EquipmentSlot
    {
        Head,           // Голова (шлем)
        Torso,          // Торс (броня)
        LeftHand,       // Левая рука
        RightHand,      // Правая рука
        Legs,           // Ноги (поножи)
        Feet,           // Ступни (обувь)
        Accessory1,     // Аксессуар 1
        Accessory2,     // Аксессуар 2
        Accessory3,     // Аксессуар 3
        Back,           // Спина (плащ)
        Backpack        // Рюкзак
    }
    
    /// <summary>
    /// Категория предмета
    /// </summary>
    public enum ItemCategory
    {
        Weapon,         // Оружие
        Armor,          // Броня
        Accessory,      // Аксессуар
        Consumable,     // Расходник
        Material,       // Материал
        Technique,      // Свиток техники
        Quest,          // Квестовый предмет
        Misc            // Разное
    }
    
    /// <summary>
    /// Редкость предмета
    /// </summary>
    public enum ItemRarity
    {
        Common,         // Обычный (50%)
        Uncommon,       // Необычный (30%)
        Rare,           // Редкий (15%)
        Epic,           // Эпический (4%)
        Legendary,      // Легендарный (1%)
        Mythic          // Мифический (0.1%)
    }
    
    /// <summary>
    /// Грейд экипировки (качество).
    /// Источник: EQUIPMENT_SYSTEM.md §2.1 "Уровни качества"
    /// 
    /// | Грейд | Прочность | Эффективность |
    /// |-------|-----------|---------------|
    /// | Damaged | ×0.5 | ×0.5 |
    /// | Common | ×1.0 | ×1.0 |
    /// | Refined | ×1.5 | ×1.3-1.5 |
    /// | Perfect | ×2.5 | ×1.7-2.5 |
    /// | Transcendent | ×4.0 | ×2.5-4.0 |
    /// </summary>
    public enum EquipmentGrade
    {
        Damaged,        // Повреждённый (×0.5)
        Common,         // Обычный (×1.0)
        Refined,        // Очищенный (×1.5)
        Perfect,        // Совершенный (×2.5)
        Transcendent    // Трансцендентный (×4.0)
    }
    
    /// <summary>
    /// Состояние прочности.
    /// Источник: EQUIPMENT_SYSTEM.md §4.1 "Состояния"
    /// </summary>
    public enum DurabilityCondition
    {
        Pristine,       // 100% — Идеальное
        Excellent,      // 80-99% — Отличное
        Good,           // 60-79% — Хорошее
        Worn,           // 40-59% — Изношенное
        Damaged,        // 20-39% — Повреждённое
        Broken          // <20% — Сломанное
    }
    
    #endregion
    
    #region Materials
    
    /// <summary>
    /// Тир материала (1-5).
    /// Источник: EQUIPMENT_SYSTEM.md §3.1 "Тиры материалов"
    /// </summary>
    public enum MaterialTier
    {
        Tier1 = 1,      // Обычные материалы (Iron, Leather, Cloth)
        Tier2 = 2,      // Качественные материалы (Steel, Silk)
        Tier3 = 3,      // Духовные материалы (Spirit Iron, Jade)
        Tier4 = 4,      // Небесные материалы (Star Metal, Dragon Bone)
        Tier5 = 5       // Первородные материалы (Void Matter)
    }
    
    /// <summary>
    /// Категория материала
    /// </summary>
    public enum MaterialCategory
    {
        Metal,          // Металл
        Leather,        // Кожа
        Cloth,          // Ткань
        Wood,           // Дерево
        Bone,           // Кость
        Crystal,        // Кристалл
        Gem,            // Драгоценный камень
        Organic,        // Органический
        Spirit,         // Духовный
        Void            // Пустотный
    }
    
    #endregion
    
    #region NPC
    
    /// <summary>
    /// Категория NPC (определяет сохранение)
    /// </summary>
    public enum NPCCategory
    {
        Temp,           // Временный (только в памяти)
        Plot,           // Сюжетный (сохраняется в файл)
        Unique          // Уникальный (полная история)
    }
    
    /// <summary>
    /// Отношение NPC к игроку / Характер NPC
    /// </summary>
    public enum Disposition
    {
        Hostile,        // Враждебный (-100 до -50)
        Unfriendly,     // Недружелюбный (-49 до -10)
        Neutral,        // Нейтральный (-9 до 9)
        Friendly,       // Дружелюбный (10 до 49)
        Allied,         // Союзник (50 до 100)
        Aggressive,     // Агрессивный характер
        Cautious,       // Осторожный характер
        Treacherous,    // Коварный характер
        Ambitious       // Честолюбивый характер
    }
    
    /// <summary>
    /// Тип отношения между фракциями
    /// </summary>
    public enum FactionRelationType
    {
        Ally,           // Союзник
        Enemy,          // Враг
        Neutral,        // Нейтрал
        Vassal          // Вассал
    }
    
    #endregion
    
    #region World
    
    /// <summary>
    /// Тип локации
    /// </summary>
    public enum LocationType
    {
        Region,         // Регион (большая область)
        Area,           // Область (часть региона)
        Building,       // Здание
        Room            // Комната
    }
    
    /// <summary>
    /// Тип местности
    /// </summary>
    public enum TerrainType
    {
        Mountains,      // Горы
        Plains,         // Равнины
        Forest,         // Лес
        Sea,            // Море
        Desert,         // Пустыня
        Swamp,          // Болото
        Tundra,         // Тундра
        Jungle,         // Джунгли
        Volcanic,       // Вулканическая
        Spiritual       // Духовная область
    }
    
    /// <summary>
    /// Тип здания
    /// </summary>
    public enum BuildingType
    {
        House,          // Дом
        Shop,           // Лавка
        Temple,         // Храм
        Cave,           // Пещера
        Tower,          // Башня
        SectHQ,         // Штаб-квартала секты
        Dojo,           // Додзё
        Forge,          // Кузница
        AlchemyLab,     // Алхимическая лаборатория
        Library         // Библиотека
    }
    
    #endregion
    
    #region Time
    
    /// <summary>
    /// Скорость игрового времени
    /// </summary>
    public enum TimeSpeed
    {
        Paused,         // Пауза
        Normal,         // 1 сек = 1 минута
        Fast,           // 1 сек = 5 минут
        VeryFast        // 1 сек = 15 минут
    }
    
    /// <summary>
    /// Время суток
    /// </summary>
    public enum TimeOfDay
    {
        Dawn,           // Рассвет (5-7)
        Morning,        // Утро (7-12)
        Noon,           // Полдень (12-14)
        Afternoon,      // День (14-18)
        Evening,        // Вечер (18-21)
        Night,          // Ночь (21-5)
        Midnight        // Полночь (0)
    }
    
    #endregion
    
    #region Combat
    
    /// <summary>
    /// Тип атаки (для подавления уровнем).
    /// Источник: ALGORITHMS.md §1.5 "Типы атак"
    /// 
    /// | Тип | Описание |
    /// |-----|----------|
    /// | normal | Обычная атака без техники |
    /// | technique | Атака техникой |
    /// | ultimate | Ultimate-техника (×1.3 урона) |
    /// </summary>
    public enum AttackType
    {
        Normal,         // Обычная атака
        Technique,      // Техника
        Ultimate        // Ultimate-техника
    }
    
    /// <summary>
    /// Результат атаки
    /// </summary>
    public enum AttackResult
    {
        Miss,           // Промах
        Dodge,          // Уклонение
        Parry,          // Парирование
        Block,          // Блок
        Hit,            // Попадание
        CriticalHit,    // Критическое попадание
        Kill            // Убийство
    }
    
    /// <summary>
    /// Стадия боя
    /// </summary>
    public enum CombatStage
    {
        None,           // Не в бою
        Initiative,     // Определение инициативы
        PlayerTurn,     // Ход игрока
        EnemyTurn,      // Ход врага
        Resolution,     // Разрешение действий
        Victory,        // Победа
        Defeat          // Поражение
    }
    
    #endregion
    
    #region Save
    
    /// <summary>
    /// Слот сохранения
    /// </summary>
    public enum SaveSlot
    {
        Slot1,
        Slot2,
        Slot3,
        AutoSave,
        QuickSave
    }
    
    /// <summary>
    /// Тип сохранения
    /// </summary>
    public enum SaveType
    {
        Manual,         // Ручное
        Auto,           // Автосохранение
        Quick,          // Быстрое
        Checkpoint      // Контрольная точка
    }
    
    #endregion
    
    #region UI
    
    /// <summary>
    /// Состояние игры (для UI)
    /// </summary>
    public enum GameState
    {
        None,           // Редактировано: 2026-04-03 - Sentinel state для инициализации
        MainMenu,       // Главное меню
        Loading,        // Загрузка
        Playing,        // Игра
        Paused,         // Пауза
        Inventory,      // Инвентарь
        Combat,         // Бой
        Dialog,         // Диалог
        Cutscene,       // Катсцена
        Settings        // Настройки
    }
    
    #endregion
}
