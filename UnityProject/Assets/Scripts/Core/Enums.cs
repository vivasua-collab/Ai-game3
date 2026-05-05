// ============================================================================
// Enums.cs — Все перечисления проекта
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-05-05 09:52:00 UTC
// Редактировано: 2026-05-05 10:05:00 UTC
// Редактировано: 2026-05-05 14:06:23 MSK — FIX С-08: убран Destroyed (unreachable)
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
        Light,      // Свет — FIX К-04: добавлен по ALGORITHMS.md §10.1
        Poison      // Яд (особая стихия — НЕ имеет противоположностей, К-05)
        // FIX CORE-H05: Count убран. Используйте Enum.GetValues(typeof(Element)).Length
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
    /// Подтип защитной техники.
    /// FIX В-09: Добавлен для корректной работы HasActiveShield()
    /// </summary>
    public enum DefenseSubtype
    {
        None,       // Не защитная
        Block,      // Блок
        Parry,      // Парирование
        Shield,     // Щит (активирует Shield-режим Qi Buffer)
        Dodge,      // Уклонение
        Reflect     // Отражение
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
    /// | Refined | ×1.3 | 1 | 20% |
    /// | Perfect | ×1.6 | 2 | 50% |
    /// | Transcendent | ×2.0 | 3 | 80% |
    /// 
    /// ⚠️ ВАЖНО: Стоимость Ци всегда ×1.0 — не зависит от Grade!
    /// FIX К-01: Множители обновлены по TECHNIQUE_SYSTEM.md
    /// </summary>
    public enum TechniqueGrade
    {
        Common,         // Обычная (×1.0)
        Refined,        // Очищенная (×1.3)
        Perfect,        // Совершенная (×1.6)
        Transcendent    // Трансцендентная (×2.0)
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
    /// Источник: BODY_SYSTEM.md «Механика повреждения»
    /// 
    /// - Healthy: оба типа HP полны
    /// - Bruised: функциональная HP снижена (<70%)
    /// - Wounded: функциональная HP < 30%
    /// - Disabled: функциональная HP = 0 (паралич)
    /// - Severed: структурная HP = 0 (отрублена)
    /// 
    /// FIX С-08: Убран Destroyed — недостижимое состояние (никогда не устанавливается
    /// в UpdateState()). Если часть тела уничтожена полностью — она Severed.
    /// </summary>
    public enum BodyPartState
    {
        Healthy,        // Здорова
        Bruised,        // Ушиблена
        Wounded,        // Ранена
        Disabled,       // Парализована (красная HP = 0)
        Severed         // Отрублена (чёрная HP = 0)
    }
    
    #endregion
    
    #region Equipment
    
    /// <summary>
    /// Слот экипировки (переработан по INVENTORY_UI_DRAFT.md v2.0)
    /// 
    /// Видимые слоты куклы (7): Head, Torso, Belt, Legs, Feet, WeaponMain, WeaponOff
    /// Скрытые слоты (заглушки на будущее): Amulet, RingLeft1/2, RingRight1/2,
    ///   Charger, Hands, Back
    /// 
    /// Редактировано: 2026-04-18 18:43:19 UTC — полная замена по драфту v2.0
    /// Старые значения Armor/Clothing/Accessory/Backpack/RingLeft/RingRight УДАЛЕНЫ
    /// </summary>
    public enum EquipmentSlot
    {
        None,
        // === Видимые слоты куклы ===
        Head,           // Голова — шлем, шапка, корона
        Torso,          // Торс — нагрудник, рубашка, роба
        Belt,           // Пояс — ремень, пояс зелий, зарядник-пояс
        Legs,           // Ноги — поножи, штаны
        Feet,           // Ступни — сабатоны, сапоги
        WeaponMain,     // Основная рука — одноручное или щит
        WeaponOff,      // Вторичная рука — одноручное, щит или инструмент
        // === Скрытые слоты (заглушки) ===
        Amulet,         // Амулет — будет с ювелирной системой
        RingLeft1,      // Кольцо левое 1 — будет с системой хранения
        RingLeft2,      // Кольцо левое 2
        RingRight1,     // Кольцо правое 1
        RingRight2,     // Кольцо правое 2
        Charger,        // Зарядник Ци — будет с системой зарядников
        Hands,          // Перчатки — будет с расширением экипировки
        Back            // Плащ/спина — будет с расширением экипировки
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
    /// FIX С-01: Убрано Excellent, приведено к 5 состояниям документации
    /// | Pristine | 100%   |
    /// | Good     | 80-99% |
    /// | Worn     | 60-79% |
    /// | Damaged  | 20-59% |
    /// | Broken   | <20%   |
    /// </summary>
    public enum DurabilityCondition
    {
        Pristine,       // 100% — Идеальное
        Good,           // 80-99% — Хорошее
        Worn,           // 60-79% — Изношенное
        Damaged,        // 20-59% — Повреждённое
        Broken          // <20% — Сломанное
    }

    /// <summary>
    /// Флаг вложения — куда можно поместить предмет.
    /// Источник: INVENTORY_UI_DRAFT.md §3.6.3
    /// 
    /// Создано: 2026-04-18 18:43:19 UTC
    /// </summary>
    public enum NestingFlag
    {
        None,       // Нельзя поместить ни в какое хранилище (живые существа, квестовые)
        Spirit,     // Можно ТОЛЬКО в духовное хранилище
        Ring,       // Можно ТОЛЬКО в кольцо хранения
        Any         // Можно в любое хранилище (по умолчанию)
    }

    /// <summary>
    /// Тип хвата оружия — определяет, сколько слотов рук занимает.
    /// Создано: 2026-04-18 18:43:19 UTC
    /// </summary>
    public enum WeaponHandType
    {
        OneHand,        // Одноручное — занимает 1 слот
        TwoHand         // Двуручное — занимает оба слота (WeaponMain + WeaponOff)
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
    /// [Obsolete] — Заменён на Attitude + PersonalityTrait (Fix-04, CORE-M01)
    /// Каскадная замена в Fix-06/07/08
    /// </summary>
    [Obsolete("Используйте Attitude + PersonalityTrait. Каскадная замена в Fix-06/07/08.")]
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
    /// Отношение NPC к игроку (числовое -100..+100).
    /// FIX CORE-M01: Замена Disposition — отношение к игроку.
    /// Источник: Решение пользователя 2026-04-10
    /// </summary>
    public enum Attitude
    {
        Hatred,         // -100..-51 — атака без предупреждения
        Hostile,        // -50..-21  — атака если спровоцирован
        Unfriendly,     // -20..-10  — избегание
        Neutral,        // -9..9     — безразличие
        Friendly,       // 10..49    — помощь, торговля
        Allied,         // 50..79    — лояльность
        SwornAlly       // 80..100   — самопожертвование
    }
    
    /// <summary>
    /// Характер NPC (комбинируемые черты, [Flags]).
    /// FIX CORE-M01: Замена Disposition — характер NPC.
    /// Источник: Решение пользователя 2026-04-10
    /// </summary>
    [Flags]
    public enum PersonalityTrait
    {
        None        = 0,
        Aggressive  = 1 << 0,   // Склонен к атаке, первый удар
        Cautious    = 1 << 1,   // Избегает рисков, защита
        Treacherous = 1 << 2,   // Может предать при возможности
        Ambitious   = 1 << 3,   // Ищет власть, лидерство
        Loyal       = 1 << 4,   // Не предаёт никогда
        Pacifist    = 1 << 5,   // Избегает боя
        Curious     = 1 << 6,   // Исследует, задаёт вопросы
        Vengeful    = 1 << 7    // Помнит обиды, мстит
    }
    
    /// <summary>
    /// Тип отношения между фракциями
    /// FIX DAT-C01: Enhanced with Overlord/Rival (was duplicate in FactionData.cs) (2026-04-11)
    /// </summary>
    public enum FactionRelationType
    {
        Ally,           // Союзник
        Enemy,          // Враг
        Neutral,        // Нейтрал
        Vassal,         // Вассал
        Overlord,       // Сюзерен
        Rival           // Соперник
    }
    
    #endregion
    
    #region World
    
    /// <summary>
    /// Тип локации
    /// FIX DAT-C02: Enhanced with Dungeon/Secret (was duplicate in LocationData.cs) (2026-04-11)
    /// </summary>
    public enum LocationType
    {
        Region,         // Регион (большая область)
        Area,           // Область (часть региона)
        Building,       // Здание
        Room,           // Комната
        Dungeon,        // Подземелье
        Secret          // Секретная область
    }
    
    /// <summary>
    /// Тип биома (мировая местность).
    /// FIX CORE-H01: Переименован из TerrainType в BiomeType для устранения
    /// конфликта с TileSystem.TerrainType (CS0104). (2026-04-11)
    /// </summary>
    public enum BiomeType
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
    /// Результат атаки (enum для UI/лога).
    /// FIX CORE-M03: Переименован из AttackResult для устранения коллизии
    /// с CombatManager.AttackResult (struct).
    /// </summary>
    public enum CombatAttackResult
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

    /// <summary>
    /// Тип стихийного эффекта.
    /// FIX С-04: Добавлен для реализации стихийных эффектов (COMBAT_SYSTEM.md §«Эффекты стихий»)
    /// </summary>
    public enum ElementalEffectType
    {
        None,
        Burn,       // Горение (Fire)
        Slow,       // Замедление (Water)
        Stun,       // Оглушение (Earth)
        Knockback,  // Отталкивание (Air)
        Chain,      // Цепной урон (Lightning)
        Pierce,     // Пробитие (Void)
        Purify,     // Очищение (Light)
        PoisonDot   // Отравление (Poison)
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
