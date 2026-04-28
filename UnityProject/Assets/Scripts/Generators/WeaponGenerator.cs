// ============================================================================
// WeaponGenerator.cs — Генератор оружия
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-31 10:22:36 UTC
// Редактировано: 2026-03-31 10:22:36 UTC
// ============================================================================
//
// Источник: docs/EQUIPMENT_SYSTEM.md
//
// Архитектура "Матрёшка":
// 1. БАЗОВЫЙ КЛАСС (Base Class) — тип, подтип, уровень
// 2. МАТЕРИАЛ (Material) — тир, свойства
// 3. ГРЕЙД (Grade) — качество, множители
//
// Формула урона оружия:
// baseDamage = max(handDamage, weaponDamage × 0.5)
// bonusDamage = weaponDamage × statScaling
// totalDamage = baseDamage + bonusDamage
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Подтип оружия
    /// Источник: EQUIPMENT_SYSTEM.md §1.3 "Подтипы оружия"
    /// </summary>
    public enum WeaponSubtype
    {
        Unarmed,        // Кастеты, когти
        Dagger,         // Кинжал
        Sword,          // Меч
        Greatsword,     // Двуручный меч
        Axe,            // Топор
        Spear,          // Копьё
        Bow,            // Лук
        Staff,          // Посох
        Hammer,         // Молот
        Mace,           // Булава
        Crossbow,       // Арбалет
        Wand            // Жезл
    }

    /// <summary>
    /// Класс оружия
    /// Источник: EQUIPMENT_SYSTEM.md §7.1 "Классификация оружия"
    /// </summary>
    public enum WeaponClass
    {
        Unarmed,        // Без оружия
        Light,          // Лёгкое
        Medium,         // Среднее
        Heavy,          // Тяжёлое
        Ranged,         // Дальнобойное
        Magic           // Магическое
    }

    /// <summary>
    /// Тип урона оружия
    /// Источник: EQUIPMENT_SYSTEM.md §7.2 "Типы урона оружия"
    /// </summary>
    public enum WeaponDamageType
    {
        Slashing,       // Рубящий
        Piercing,       // Колющий
        Blunt,          // Дробящий
        Elemental       // Стихийный
    }

    /// <summary>
    /// Параметры генерации оружия
    /// </summary>
    [Serializable]
    public class WeaponGenerationParams
    {
        public WeaponSubtype subtype = WeaponSubtype.Sword;
        public int itemLevel = 1;               // Уровень предмета (1-9)
        public EquipmentGrade grade = EquipmentGrade.Common;
        public int materialTier = 1;            // Тир материала (1-5)
        public MaterialCategory materialCategory = MaterialCategory.Metal;
        public int count = 1;
        public int? seed = null;
    }

    /// <summary>
    /// Результат генерации оружия
    /// </summary>
    [Serializable]
    public class GeneratedWeapon
    {
        // Identity
        public string id;
        public string nameRu;
        public string nameEn;

        // Classification
        public WeaponSubtype subtype;
        public WeaponClass weaponClass;
        public WeaponDamageType damageType;
        public EquipmentGrade grade;

        // Material
        public string materialId;
        public int materialTier;
        public MaterialCategory materialCategory;

        // Level
        public int itemLevel;

        // Stats
        public int baseDamage;
        public float attackSpeed;
        public float range;
        public int critChance;
        public float critDamage;

        // Requirements
        public int requiredStrength;
        public int requiredAgility;
        public int requiredCultivationLevel;

        // Durability
        public int maxDurability;
        public int currentDurability;

        // Inventory (строчная модель v2.0)
        public float weight;                         // Вес (кг)
        public float volume;                         // Объём (литры) = clamp(weight, 1, 4)
        public bool stackable = false;               // Оружие не стакается
        public int maxStack = 1;
        public NestingFlag allowNesting = NestingFlag.Any;
        public ItemCategory category = ItemCategory.Weapon;

        // Bonuses
        public List<StatBonus> bonuses = new List<StatBonus>();

        // Qi properties
        public float qiConductivity;
        public float qiCostReduction;

        // Special
        public List<string> specialEffects = new List<string>();

        public string description;
    }

    [Serializable]
    public class StatBonus
    {
        public string statName;
        public float value;
        public bool isPercentage;
    }

    /// <summary>
    /// Генератор оружия
    /// Соответствует документации: docs/EQUIPMENT_SYSTEM.md
    /// </summary>
    public static class WeaponGenerator
    {
        // === ГРЕЙД МНОЖИТЕЛИ (источник: EQUIPMENT_SYSTEM.md §2.1) ===
        // | Грейд       | Прочность | Эффективность |
        // |-------------|-----------|---------------|
        // | Damaged     | ×0.5      | ×0.5          |
        // | Common      | ×1.0      | ×1.0          |
        // | Refined     | ×1.5      | ×1.3          |
        // | Perfect     | ×2.5      | ×1.7          |
        // | Transcendent| ×4.0      | ×2.5          |
        private static readonly float[] GradeDurabilityMult = { 0.5f, 1.0f, 1.5f, 2.5f, 4.0f };
        private static readonly float[] GradeEffectivenessMult = { 0.5f, 1.0f, 1.3f, 1.7f, 2.5f };

        // === БОНУСЫ ОТ GRADE (источник: EQUIPMENT_SYSTEM.md §2.1) ===
        // | Grade       | Бонусов |
        // |-------------|---------|
        // | Damaged     | 0       |
        // | Common      | 0-1     |
        // | Refined     | 1-2     |
        // | Perfect     | 2-4     |
        // | Transcendent| 4-6     |
        private static readonly int[] MinBonusesByGrade = { 0, 0, 1, 2, 4 };
        private static readonly int[] MaxBonusesByGrade = { 0, 1, 2, 4, 6 };

        // === БАЗОВЫЙ ВЕС ПО ПОДТИПУ (источник: EQUIPMENT_SYSTEM.md §7) ===
        private static readonly Dictionary<WeaponSubtype, float> BaseWeightBySubtype = new Dictionary<WeaponSubtype, float>
        {
            { WeaponSubtype.Unarmed,     0.3f },   // Кастеты
            { WeaponSubtype.Dagger,      0.5f },   // Кинжал
            { WeaponSubtype.Sword,       2.5f },   // Меч
            { WeaponSubtype.Greatsword,  6.0f },   // Двуручник
            { WeaponSubtype.Axe,         3.0f },   // Топор
            { WeaponSubtype.Spear,       2.8f },   // Копьё
            { WeaponSubtype.Bow,         1.2f },   // Лук
            { WeaponSubtype.Staff,       2.0f },   // Посох
            { WeaponSubtype.Hammer,      5.0f },   // Молот
            { WeaponSubtype.Mace,        3.5f },   // Булава
            { WeaponSubtype.Crossbow,    3.0f },   // Арбалет
            { WeaponSubtype.Wand,        0.2f }    // Жезл
        };

        // === МНОЖИТЕЛЬ ВЕСА МАТЕРИАЛА (источник: EQUIPMENT_SYSTEM.md §3.1) ===
        // T1 (Iron): 1.0, T2 (Steel): 1.0, T3 (Spirit Iron): 0.8,
        // T4 (Star Metal): 0.7, T5 (Void Matter): 0.5
        private static readonly float[] MaterialWeightMult = { 1.0f, 1.0f, 0.8f, 0.7f, 0.5f };

        // === ХАРАКТЕРИСТИКИ ПОДТИПОВ (источник: EQUIPMENT_SYSTEM.md §1.3) ===
        private static readonly Dictionary<WeaponSubtype, WeaponClass> WeaponClassBySubtype = new Dictionary<WeaponSubtype, WeaponClass>
        {
            { WeaponSubtype.Unarmed, WeaponClass.Unarmed },
            { WeaponSubtype.Dagger, WeaponClass.Light },
            { WeaponSubtype.Sword, WeaponClass.Medium },
            { WeaponSubtype.Greatsword, WeaponClass.Heavy },
            { WeaponSubtype.Axe, WeaponClass.Medium },
            { WeaponSubtype.Spear, WeaponClass.Heavy },
            { WeaponSubtype.Bow, WeaponClass.Ranged },
            { WeaponSubtype.Staff, WeaponClass.Magic },
            { WeaponSubtype.Hammer, WeaponClass.Heavy },
            { WeaponSubtype.Mace, WeaponClass.Medium },
            { WeaponSubtype.Crossbow, WeaponClass.Ranged },
            { WeaponSubtype.Wand, WeaponClass.Magic }
        };

        private static readonly Dictionary<WeaponSubtype, (int baseDmg, float speed, float range)> WeaponStatsBySubtype = new Dictionary<WeaponSubtype, (int, float, float)>
        {
            // (baseDamage, attackSpeed, range в метрах)
            { WeaponSubtype.Unarmed, (3, 1.5f, 0.5f) },
            { WeaponSubtype.Dagger, (8, 1.3f, 0.5f) },
            { WeaponSubtype.Sword, (15, 1.0f, 1.0f) },
            { WeaponSubtype.Greatsword, (25, 0.7f, 1.5f) },
            { WeaponSubtype.Axe, (18, 0.9f, 1.0f) },
            { WeaponSubtype.Spear, (20, 0.8f, 2.0f) },
            { WeaponSubtype.Bow, (12, 1.0f, 30f) },
            { WeaponSubtype.Staff, (8, 0.8f, 1.5f) },
            { WeaponSubtype.Hammer, (28, 0.6f, 1.2f) },
            { WeaponSubtype.Mace, (16, 0.9f, 1.0f) },
            { WeaponSubtype.Crossbow, (18, 0.7f, 40f) },
            { WeaponSubtype.Wand, (5, 1.2f, 20f) }
        };

        private static readonly Dictionary<WeaponSubtype, WeaponDamageType> DamageTypeBySubtype = new Dictionary<WeaponSubtype, WeaponDamageType>
        {
            { WeaponSubtype.Unarmed, WeaponDamageType.Blunt },
            { WeaponSubtype.Dagger, WeaponDamageType.Piercing },
            { WeaponSubtype.Sword, WeaponDamageType.Slashing },
            { WeaponSubtype.Greatsword, WeaponDamageType.Slashing },
            { WeaponSubtype.Axe, WeaponDamageType.Slashing },
            { WeaponSubtype.Spear, WeaponDamageType.Piercing },
            { WeaponSubtype.Bow, WeaponDamageType.Piercing },
            { WeaponSubtype.Staff, WeaponDamageType.Blunt },
            { WeaponSubtype.Hammer, WeaponDamageType.Blunt },
            { WeaponSubtype.Mace, WeaponDamageType.Blunt },
            { WeaponSubtype.Crossbow, WeaponDamageType.Piercing },
            { WeaponSubtype.Wand, WeaponDamageType.Elemental }
        };

        // === ТИРЫ МАТЕРИАЛОВ (источник: EQUIPMENT_SYSTEM.md §3.1) ===
        // | Тир | Прочность | Проводимость Ци |
        // |-----|-----------|-----------------|
        // | T1  | 20-50     | 0.3-0.8         |
        // | T2  | 50-80     | 0.4-1.2         |
        // | T3  | 80-150    | 1.0-2.0         |
        // | T4  | 150-400   | 2.0-3.5         |
        // | T5  | 400-600   | 4.0-5.0         |
        private static readonly (int minDur, int maxDur, float minCond, float maxCond)[] MaterialTierStats =
        {
            (20, 50, 0.3f, 0.8f),   // T1
            (50, 80, 0.4f, 1.2f),   // T2
            (80, 150, 1.0f, 2.0f),  // T3
            (150, 400, 2.0f, 3.5f), // T4
            (400, 600, 4.0f, 5.0f)  // T5
        };

        // Названия материалов по тирам
        private static readonly Dictionary<MaterialCategory, string[]> MaterialNamesByCategory = new Dictionary<MaterialCategory, string[]>
        {
            { MaterialCategory.Metal, new[] { "Железо", "Сталь", "Духовное железо", "Звёздный металл", "Пустотная материя" } },
            { MaterialCategory.Bone, new[] { "Кость", "Закалённая кость", "Духовная кость", "Кость дракона", "Изначальная кость" } },
            { MaterialCategory.Wood, new[] { "Дерево", "Железное дерево", "Духовное дерево", "Древесина древних", "Мировое древо" } },
            { MaterialCategory.Crystal, new[] { "Хрусталь", "Горный хрусталь", "Духовный кристалл", "Звёздный кристалл", "Изначальный кристалл" } }
        };

        // Названия оружия по подтипам
        private static readonly Dictionary<WeaponSubtype, string[]> WeaponNamesBySubtype = new Dictionary<WeaponSubtype, string[]>
        {
            { WeaponSubtype.Unarmed, new[] { "Кастеты", "Когти", "Перчатки" } },
            { WeaponSubtype.Dagger, new[] { "Кинжал", "Короткий меч", "Стилет" } },
            { WeaponSubtype.Sword, new[] { "Меч", "Клинок", "Катана" } },
            { WeaponSubtype.Greatsword, new[] { "Двуручный меч", "Палаш", "Клеймор" } },
            { WeaponSubtype.Axe, new[] { "Топор", "Секира", "Боевой топор" } },
            { WeaponSubtype.Spear, new[] { "Копьё", "Алебарда", "Глефа" } },
            { WeaponSubtype.Bow, new[] { "Лук", "Длинный лук", "Короткий лук" } },
            { WeaponSubtype.Staff, new[] { "Посох", "Жезл", "Скипетр" } },
            { WeaponSubtype.Hammer, new[] { "Молот", "Боевой молот", "Кувалда" } },
            { WeaponSubtype.Mace, new[] { "Булава", "Палица", "Моргенштерн" } },
            { WeaponSubtype.Crossbow, new[] { "Арбалет", "Тяжёлый арбалет" } },
            { WeaponSubtype.Wand, new[] { "Жезл", "Волшебная палочка", "Скипетр" } }
        };

        /// <summary>
        /// Сгенерировать одно оружие
        /// </summary>
        public static GeneratedWeapon Generate(WeaponGenerationParams parameters, SeededRandom rng = null)
        {
            if (rng == null)
                rng = new SeededRandom(parameters.seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));

            var weapon = new GeneratedWeapon();

            // Classification
            weapon.subtype = parameters.subtype;
            weapon.weaponClass = GetWeaponClass(weapon.subtype);
            weapon.damageType = GetDamageType(weapon.subtype);
            weapon.grade = parameters.grade;
            weapon.itemLevel = Mathf.Clamp(parameters.itemLevel, 1, 9);
            weapon.materialTier = Mathf.Clamp(parameters.materialTier, 1, 5);
            weapon.materialCategory = parameters.materialCategory;

            // Вес (строчная модель инвентаря)
            float baseWeight = BaseWeightBySubtype.ContainsKey(weapon.subtype)
                ? BaseWeightBySubtype[weapon.subtype] : 2.0f;
            float matWMult = MaterialWeightMult[weapon.materialTier - 1];
            weapon.weight = baseWeight * matWMult * (1f + (weapon.itemLevel - 1) * 0.05f);
            weapon.volume = Mathf.Clamp(weapon.weight, 1f, 4f);
            weapon.stackable = false;
            weapon.maxStack = 1;
            weapon.allowNesting = NestingFlag.Any;
            weapon.category = ItemCategory.Weapon;

            // Material
            weapon.materialId = GetMaterialName(weapon.materialCategory, weapon.materialTier, rng);

            // Base stats from subtype
            var (baseDmg, speed, range) = GetWeaponBaseStats(weapon.subtype);

            // Apply level scaling
            float levelMult = 1f + (weapon.itemLevel - 1) * 0.3f;
            baseDmg = Mathf.RoundToInt(baseDmg * levelMult);

            // Apply material tier bonus
            float materialMult = 1f + (weapon.materialTier - 1) * 0.25f;
            baseDmg = Mathf.RoundToInt(baseDmg * materialMult);

            // Apply grade effectiveness
            float gradeEffMult = GetGradeEffectiveness(weapon.grade);
            weapon.baseDamage = Mathf.RoundToInt(baseDmg * gradeEffMult);

            weapon.attackSpeed = speed * (1f + (int)weapon.grade * 0.05f);
            weapon.range = range;

            // Critical
            weapon.critChance = 5 + (int)weapon.grade * 3 + rng.Next(-2, 3);
            weapon.critDamage = 1.5f + (int)weapon.grade * 0.1f;

            // Durability (источник: EQUIPMENT_SYSTEM.md §3.1)
            var (minDur, maxDur, minCond, maxCond) = MaterialTierStats[weapon.materialTier - 1];
            int baseDurability = rng.Next(minDur, maxDur + 1);
            float gradeDurMult = GetGradeDurability(weapon.grade);
            weapon.maxDurability = Mathf.RoundToInt(baseDurability * gradeDurMult);
            weapon.currentDurability = weapon.maxDurability;

            // Qi conductivity
            weapon.qiConductivity = rng.NextFloat(minCond, maxCond) * gradeEffMult;
            weapon.qiCostReduction = weapon.grade >= EquipmentGrade.Refined ? (int)weapon.grade * 0.05f : 0f;

            // Requirements
            CalculateRequirements(weapon, rng);

            // Bonuses
            GenerateBonuses(weapon, rng);

            // ID
            weapon.id = $"weapon_{weapon.subtype}_{weapon.materialTier}_{weapon.itemLevel}_{rng.Next(1000):D3}";

            // Name
            weapon.nameRu = GenerateName(weapon, rng);
            weapon.nameEn = weapon.nameRu;

            // Description
            weapon.description = GenerateDescription(weapon);

            return weapon;
        }

        /// <summary>
        /// Сгенерировать несколько оружий
        /// </summary>
        public static List<GeneratedWeapon> GenerateMultiple(WeaponGenerationParams parameters)
        {
            var rng = new SeededRandom(parameters.seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            var results = new List<GeneratedWeapon>();

            for (int i = 0; i < parameters.count; i++)
            {
                var weapon = Generate(parameters, rng);
                results.Add(weapon);
            }

            return results;
        }

        /// <summary>
        /// Сгенерировать оружие для уровня культивации
        /// </summary>
        public static GeneratedWeapon GenerateForLevel(int cultivationLevel, SeededRandom rng = null)
        {
            if (rng == null)
                rng = new SeededRandom();

            // Определяем уровень предмета и тир материала
            int itemLevel = Mathf.Clamp(cultivationLevel, 1, 9);
            int materialTier = Mathf.Clamp((cultivationLevel + 1) / 2, 1, 5);

            // Случайный подтип
            var subtypes = (WeaponSubtype[])Enum.GetValues(typeof(WeaponSubtype));
            var subtype = rng.NextElement(subtypes);

            // Grade по распределению
            var grade = GenerateGrade(rng, cultivationLevel);

            var parameters = new WeaponGenerationParams
            {
                subtype = subtype,
                itemLevel = itemLevel,
                grade = grade,
                materialTier = materialTier,
                materialCategory = rng.NextElement(new[] { MaterialCategory.Metal, MaterialCategory.Bone, MaterialCategory.Wood, MaterialCategory.Crystal }),
                seed = rng.Next()
            };

            return Generate(parameters, new SeededRandom(parameters.seed.Value));
        }

        // === Helpers ===

        private static WeaponClass GetWeaponClass(WeaponSubtype subtype)
        {
            if (WeaponClassBySubtype.ContainsKey(subtype))
                return WeaponClassBySubtype[subtype];
            return WeaponClass.Medium;
        }

        private static WeaponDamageType GetDamageType(WeaponSubtype subtype)
        {
            if (DamageTypeBySubtype.ContainsKey(subtype))
                return DamageTypeBySubtype[subtype];
            return WeaponDamageType.Slashing;
        }

        private static (int, float, float) GetWeaponBaseStats(WeaponSubtype subtype)
        {
            if (WeaponStatsBySubtype.ContainsKey(subtype))
                return WeaponStatsBySubtype[subtype];
            return (10, 1.0f, 1.0f);
        }

        private static float GetGradeDurability(EquipmentGrade grade)
        {
            int index = (int)grade;
            if (index >= 0 && index < GradeDurabilityMult.Length)
                return GradeDurabilityMult[index];
            return 1.0f;
        }

        private static float GetGradeEffectiveness(EquipmentGrade grade)
        {
            int index = (int)grade;
            if (index >= 0 && index < GradeEffectivenessMult.Length)
                return GradeEffectivenessMult[index];
            return 1.0f;
        }

        /// <summary>
        /// Генерация Grade по распределению
        /// Источник: EQUIPMENT_SYSTEM.md §2.2 "Распределение грейдов"
        /// </summary>
        public static EquipmentGrade GenerateGrade(SeededRandom rng, int level = 1)
        {
            // Распределение зависит от уровня
            // | Уровень | Damaged | Common | Refined | Perfect | Transcendent |
            // |---------|---------|--------|---------|---------|--------------|
            // | L1      | 30%     | 60%    | 10%     | 0%      | 0%           |
            // | L3      | 10%     | 50%    | 35%     | 5%      | 0%           |
            // | L5      | 5%      | 30%    | 45%     | 20%     | 0%           |
            // | L7      | 0%      | 20%    | 40%     | 35%     | 5%           |
            // | L9      | 0%      | 10%    | 30%     | 40%     | 20%          |

            float[] distribution;
            if (level <= 1)
                distribution = new[] { 0.30f, 0.60f, 0.10f, 0.00f, 0.00f };
            else if (level <= 3)
                distribution = new[] { 0.10f, 0.50f, 0.35f, 0.05f, 0.00f };
            else if (level <= 5)
                distribution = new[] { 0.05f, 0.30f, 0.45f, 0.20f, 0.00f };
            else if (level <= 7)
                distribution = new[] { 0.00f, 0.20f, 0.40f, 0.35f, 0.05f };
            else
                distribution = new[] { 0.00f, 0.10f, 0.30f, 0.40f, 0.20f };

            float roll = rng.NextFloat();
            float cumulative = 0f;

            for (int i = 0; i < distribution.Length; i++)
            {
                cumulative += distribution[i];
                if (roll < cumulative)
                    return (EquipmentGrade)i;
            }

            return EquipmentGrade.Common;
        }

        private static string GetMaterialName(MaterialCategory category, int tier, SeededRandom rng)
        {
            if (MaterialNamesByCategory.ContainsKey(category))
            {
                var names = MaterialNamesByCategory[category];
                int index = Mathf.Clamp(tier - 1, 0, names.Length - 1);
                return names[index];
            }
            return $"Материал T{tier}";
        }

        private static void CalculateRequirements(GeneratedWeapon weapon, SeededRandom rng)
        {
            // Базовые требования по классу оружия
            switch (weapon.weaponClass)
            {
                case WeaponClass.Unarmed:
                    weapon.requiredStrength = 0;
                    weapon.requiredAgility = 5 + weapon.itemLevel;
                    break;
                case WeaponClass.Light:
                    weapon.requiredStrength = 5 + weapon.itemLevel;
                    weapon.requiredAgility = 8 + weapon.itemLevel;
                    break;
                case WeaponClass.Medium:
                    weapon.requiredStrength = 10 + weapon.itemLevel * 2;
                    weapon.requiredAgility = 5 + weapon.itemLevel;
                    break;
                case WeaponClass.Heavy:
                    weapon.requiredStrength = 15 + weapon.itemLevel * 3;
                    weapon.requiredAgility = 3 + weapon.itemLevel;
                    break;
                case WeaponClass.Ranged:
                    weapon.requiredStrength = 5 + weapon.itemLevel;
                    weapon.requiredAgility = 10 + weapon.itemLevel * 2;
                    break;
                case WeaponClass.Magic:
                    weapon.requiredStrength = 3 + weapon.itemLevel;
                    weapon.requiredAgility = 5 + weapon.itemLevel;
                    break;
            }

            // Уровень культивации зависит от тира материала
            weapon.requiredCultivationLevel = Mathf.Max(1, (weapon.materialTier - 1) * 2);
        }

        private static void GenerateBonuses(GeneratedWeapon weapon, SeededRandom rng)
        {
            int minBonus = MinBonusesByGrade[(int)weapon.grade];
            int maxBonus = MaxBonusesByGrade[(int)weapon.grade];
            int bonusCount = rng.Next(minBonus, maxBonus + 1);

            string[] possibleBonuses = { "damage", "attackSpeed", "critChance", "critDamage", "armorPenetration", "qiDamage", "lifesteal" };

            for (int i = 0; i < bonusCount; i++)
            {
                string bonusType = rng.NextElement(possibleBonuses);
                float value = rng.NextFloat(0.05f, 0.2f) * ((int)weapon.grade + 1);

                weapon.bonuses.Add(new StatBonus
                {
                    statName = bonusType,
                    value = value,
                    isPercentage = true
                });
            }
        }

        private static string GenerateName(GeneratedWeapon weapon, SeededRandom rng)
        {
            string materialName = weapon.materialId;
            string baseName = "";

            if (WeaponNamesBySubtype.ContainsKey(weapon.subtype))
                baseName = rng.NextElement(WeaponNamesBySubtype[weapon.subtype]);
            else
                baseName = weapon.subtype.ToString();

            // Добавляем префикс грейда
            string gradePrefix = weapon.grade switch
            {
                EquipmentGrade.Damaged => "Сломанный ",
                EquipmentGrade.Refined => "Улучшенный ",
                EquipmentGrade.Perfect => "Совершенный ",
                EquipmentGrade.Transcendent => "Трансцендентный ",
                _ => ""
            };

            return $"{gradePrefix}{materialName} {baseName}";
        }

        private static string GenerateDescription(GeneratedWeapon weapon)
        {
            return $"{weapon.nameRu} уровня {weapon.itemLevel}. " +
                   $"Урон: {weapon.baseDamage}, Скорость: {weapon.attackSpeed:F1}. " +
                   $"Тип урона: {weapon.damageType}. " +
                   $"Прочность: {weapon.currentDurability}/{weapon.maxDurability}.";
        }

        /// <summary>
        /// Вывести примеры сгенерированного оружия
        /// </summary>
        public static string GenerateExamples()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== ПРИМЕРЫ СГЕНЕРИРОВАННОГО ОРУЖИЯ ===\n");

            var testCases = new[]
            {
                (WeaponSubtype.Sword, 1, EquipmentGrade.Common, 1),
                (WeaponSubtype.Dagger, 3, EquipmentGrade.Refined, 2),
                (WeaponSubtype.Greatsword, 5, EquipmentGrade.Perfect, 3),
                (WeaponSubtype.Bow, 7, EquipmentGrade.Perfect, 4),
                (WeaponSubtype.Staff, 9, EquipmentGrade.Transcendent, 5),
            };

            var rng = new SeededRandom(12345);

            foreach (var (subtype, level, grade, tier) in testCases)
            {
                var parameters = new WeaponGenerationParams
                {
                    subtype = subtype,
                    itemLevel = level,
                    grade = grade,
                    materialTier = tier,
                    materialCategory = tier <= 2 ? MaterialCategory.Metal : rng.NextElement(new[] { MaterialCategory.Metal, MaterialCategory.Crystal }),
                    seed = rng.Next()
                };

                var weapon = Generate(parameters, new SeededRandom(parameters.seed.Value));

                sb.AppendLine($"[{weapon.grade}] {weapon.nameRu}");
                sb.AppendLine($"  ID: {weapon.id}");
                sb.AppendLine($"  Урон: {weapon.baseDamage} ({weapon.damageType})");
                sb.AppendLine($"  Скорость атаки: {weapon.attackSpeed:F2}");
                sb.AppendLine($"  Дальность: {weapon.range:F1}м");
                sb.AppendLine($"  Крит: {weapon.critChance}% / {weapon.critDamage:F1}x");
                sb.AppendLine($"  Прочность: {weapon.currentDurability}/{weapon.maxDurability}");
                sb.AppendLine($"  Вес: {weapon.weight:F2}кг, Объём: {weapon.volume:F1}л");
                sb.AppendLine($"  Требования: СИЛ {weapon.requiredStrength}, ЛОВ {weapon.requiredAgility}, Ур.{weapon.requiredCultivationLevel}+");
                sb.AppendLine($"  Проводимость Ци: {weapon.qiConductivity:F2}");
                if (weapon.bonuses.Count > 0)
                    sb.AppendLine($"  Бонусы: {string.Join(", ", weapon.bonuses.ConvertAll(b => $"{b.statName} +{b.value * 100:F0}%"))}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
