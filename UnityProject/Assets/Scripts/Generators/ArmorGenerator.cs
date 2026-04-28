// ============================================================================
// ArmorGenerator.cs — Генератор брони
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
// Подтипы брони (§1.4):
// | Подтип      | Защищаемые части тела           | Покрытие |
// |-------------|--------------------------------|----------|
// | armor_head  | head                           | 70-95%   |
// | armor_torso | torso, heart                   | 60-90%   |
// | armor_arms  | left_arm, right_arm            | 50-85%   |
// | armor_hands | left_hand, right_hand          | 40-80%   |
// | armor_legs  | left_leg, right_leg            | 50-85%   |
// | armor_feet  | left_foot, right_foot          | 30-70%   |
// | armor_full  | Все части                      | 80-100%  |
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Подтип брони
    /// Источник: EQUIPMENT_SYSTEM.md §1.4 "Подтипы брони"
    /// </summary>
    public enum ArmorSubtype
    {
        Head,           // Шлем (head)
        Torso,          // Нагрудник (torso, heart)
        Arms,           // Наручи (left_arm, right_arm)
        Hands,          // Перчатки (left_hand, right_hand)
        Legs,           // Поножи (left_leg, right_leg)
        Feet,           // Сабатоны (left_foot, right_foot)
        Full            // Полная броня (все части)
    }

    /// <summary>
    /// Тип брони (весовая категория)
    /// </summary>
    public enum ArmorWeightClass
    {
        Light,          // Лёгкая (ткань, кожа)
        Medium,         // Средняя (кольчуга, стёганая)
        Heavy           // Тяжёлая (латы)
    }

    /// <summary>
    /// Параметры генерации брони
    /// </summary>
    [Serializable]
    public class ArmorGenerationParams
    {
        public ArmorSubtype subtype = ArmorSubtype.Torso;
        public ArmorWeightClass weightClass = ArmorWeightClass.Medium;
        public int itemLevel = 1;
        public EquipmentGrade grade = EquipmentGrade.Common;
        public int materialTier = 1;
        public MaterialCategory materialCategory = MaterialCategory.Metal;
        public int count = 1;
        public int? seed = null;
    }

    /// <summary>
    /// Результат генерации брони
    /// </summary>
    [Serializable]
    public class GeneratedArmor
    {
        // Identity
        public string id;
        public string nameRu;
        public string nameEn;

        // Classification
        public ArmorSubtype subtype;
        public ArmorWeightClass weightClass;
        public EquipmentGrade grade;

        // Material
        public string materialId;
        public int materialTier;
        public MaterialCategory materialCategory;

        // Level
        public int itemLevel;

        // Stats
        public int armor;                       // Базовая броня
        public float damageReduction;           // Снижение урона % (0-80%)
        public float coverage;                  // Покрытие части тела %
        public float dodgePenalty;              // Штраф уклонения
        public float moveSpeedPenalty;          // Штраф скорости

        // Qi properties
        public float qiFlowPenalty;             // Штраф проводимости Ци

        // Durability
        public int maxDurability;
        public int currentDurability;

        // Requirements
        public int requiredStrength;
        public int requiredCultivationLevel;

        // Inventory (строчная модель v2.0)
        public float weight;                         // Вес (кг)
        public float volume;                         // Объём (литры) = clamp(weight, 1, 4)
        public bool stackable = false;               // Броня не стакается
        public int maxStack = 1;
        public NestingFlag allowNesting = NestingFlag.Any;
        public ItemCategory category = ItemCategory.Armor;

        // Bonuses
        public List<ArmorBonus> bonuses = new List<ArmorBonus>();

        // Protected body parts
        public List<BodyPartType> protectedParts = new List<BodyPartType>();

        // Elemental resistances
        public Dictionary<Element, float> elementalResistances = new Dictionary<Element, float>();

        public string description;
    }

    [Serializable]
    public class ArmorBonus
    {
        public string statName;
        public float value;
        public bool isPercentage;
    }

    /// <summary>
    /// Генератор брони
    /// Соответствует документации: docs/EQUIPMENT_SYSTEM.md
    /// </summary>
    public static class ArmorGenerator
    {
        // === ГРЕЙД МНОЖИТЕЛИ (источник: EQUIPMENT_SYSTEM.md §2.1) ===
        private static readonly float[] GradeDurabilityMult = { 0.5f, 1.0f, 1.5f, 2.5f, 4.0f };
        private static readonly float[] GradeEffectivenessMult = { 0.5f, 1.0f, 1.3f, 1.7f, 2.5f };

        // === БОНУСЫ ОТ GRADE (источник: EQUIPMENT_SYSTEM.md §2.1) ===
        private static readonly int[] MinBonusesByGrade = { 0, 0, 1, 2, 4 };
        private static readonly int[] MaxBonusesByGrade = { 0, 1, 2, 4, 6 };

        // === БАЗОВЫЙ ВЕС БРОНИ ПО (подтип, весовой класс) в кг ===
        // Источник: EQUIPMENT_SYSTEM.md §8 — вес брони зависит от типа и материала
        private static readonly Dictionary<(ArmorSubtype, ArmorWeightClass), float> BaseWeightBySubtypeAndClass = new Dictionary<(ArmorSubtype, ArmorWeightClass), float>
        {
            // Light
            { (ArmorSubtype.Head,  ArmorWeightClass.Light), 0.3f },
            { (ArmorSubtype.Torso, ArmorWeightClass.Light), 0.5f },
            { (ArmorSubtype.Arms,  ArmorWeightClass.Light), 0.2f },
            { (ArmorSubtype.Hands, ArmorWeightClass.Light), 0.1f },
            { (ArmorSubtype.Legs,  ArmorWeightClass.Light), 0.3f },
            { (ArmorSubtype.Feet,  ArmorWeightClass.Light), 0.2f },
            { (ArmorSubtype.Full,  ArmorWeightClass.Light), 1.5f },
            // Medium
            { (ArmorSubtype.Head,  ArmorWeightClass.Medium), 2.5f },
            { (ArmorSubtype.Torso, ArmorWeightClass.Medium), 5.0f },
            { (ArmorSubtype.Arms,  ArmorWeightClass.Medium), 2.0f },
            { (ArmorSubtype.Hands, ArmorWeightClass.Medium), 0.8f },
            { (ArmorSubtype.Legs,  ArmorWeightClass.Medium), 3.0f },
            { (ArmorSubtype.Feet,  ArmorWeightClass.Medium), 1.5f },
            { (ArmorSubtype.Full,  ArmorWeightClass.Medium), 8.0f },
            // Heavy
            { (ArmorSubtype.Head,  ArmorWeightClass.Heavy), 4.0f },
            { (ArmorSubtype.Torso, ArmorWeightClass.Heavy), 8.0f },
            { (ArmorSubtype.Arms,  ArmorWeightClass.Heavy), 3.5f },
            { (ArmorSubtype.Hands, ArmorWeightClass.Heavy), 1.5f },
            { (ArmorSubtype.Legs,  ArmorWeightClass.Heavy), 5.0f },
            { (ArmorSubtype.Feet,  ArmorWeightClass.Heavy), 3.0f },
            { (ArmorSubtype.Full,  ArmorWeightClass.Heavy), 12.0f }
        };

        // === МНОЖИТЕЛЬ ВЕСА МАТЕРИАЛА (источник: EQUIPMENT_SYSTEM.md §3.1) ===
        // T1 (Iron): 1.0, T2 (Steel): 1.0, T3 (Spirit Iron): 0.8,
        // T4 (Star Metal): 0.7, T5 (Void Matter): 0.5
        private static readonly float[] MaterialWeightMult = { 1.0f, 1.0f, 0.8f, 0.7f, 0.5f };

        // === ПОКРЫТИЕ ПО ПОДТИПАМ (источник: EQUIPMENT_SYSTEM.md §1.4) ===
        private static readonly Dictionary<ArmorSubtype, (float min, float max)> CoverageBySubtype = new Dictionary<ArmorSubtype, (float, float)>
        {
            { ArmorSubtype.Head, (70f, 95f) },
            { ArmorSubtype.Torso, (60f, 90f) },
            { ArmorSubtype.Arms, (50f, 85f) },
            { ArmorSubtype.Hands, (40f, 80f) },
            { ArmorSubtype.Legs, (50f, 85f) },
            { ArmorSubtype.Feet, (30f, 70f) },
            { ArmorSubtype.Full, (80f, 100f) }
        };

        // === ЗАЩИЩАЕМЫЕ ЧАСТИ ТЕЛА (источник: EQUIPMENT_SYSTEM.md §1.4) ===
        private static readonly Dictionary<ArmorSubtype, BodyPartType[]> ProtectedPartsBySubtype = new Dictionary<ArmorSubtype, BodyPartType[]>
        {
            { ArmorSubtype.Head, new[] { BodyPartType.Head } },
            { ArmorSubtype.Torso, new[] { BodyPartType.Torso, BodyPartType.Heart } },
            { ArmorSubtype.Arms, new[] { BodyPartType.LeftArm, BodyPartType.RightArm } },
            { ArmorSubtype.Hands, new[] { BodyPartType.LeftHand, BodyPartType.RightHand } },
            { ArmorSubtype.Legs, new[] { BodyPartType.LeftLeg, BodyPartType.RightLeg } },
            { ArmorSubtype.Feet, new[] { BodyPartType.LeftFoot, BodyPartType.RightFoot } },
            { ArmorSubtype.Full, new[] {
                BodyPartType.Head, BodyPartType.Torso, BodyPartType.Heart,
                BodyPartType.LeftArm, BodyPartType.RightArm,
                BodyPartType.LeftHand, BodyPartType.RightHand,
                BodyPartType.LeftLeg, BodyPartType.RightLeg,
                BodyPartType.LeftFoot, BodyPartType.RightFoot
            }}
        };

        // === БАЗОВАЯ БРОНЯ ПО ВЕСОВОМУ КЛАССУ ===
        // Лёгкая: меньше защиты, меньше штрафов
        // Средняя: баланс
        // Тяжёлая: много защиты, большие штрафы
        private static readonly Dictionary<ArmorWeightClass, (int baseArmor, float dodgePen, float speedPen, float qiPen)> StatsByWeightClass = new Dictionary<ArmorWeightClass, (int, float, float, float)>
        {
            { ArmorWeightClass.Light, (5, 0f, 0f, 0f) },
            { ArmorWeightClass.Medium, (15, -5f, -5f, -5f) },
            { ArmorWeightClass.Heavy, (30, -15f, -15f, -10f) }
        };

        // === ТИРЫ МАТЕРИАЛОВ (источник: EQUIPMENT_SYSTEM.md §3.1) ===
        private static readonly (int minDur, int maxDur, float minCond, float maxCond)[] MaterialTierStats =
        {
            (20, 50, 0.3f, 0.8f),   // T1
            (50, 80, 0.4f, 1.2f),   // T2
            (80, 150, 1.0f, 2.0f),  // T3
            (150, 400, 2.0f, 3.5f), // T4
            (400, 600, 4.0f, 5.0f)  // T5
        };

        // Названия материалов по категориям для брони
        private static readonly Dictionary<MaterialCategory, string[]> ArmorMaterialNames = new Dictionary<MaterialCategory, string[]>
        {
            { MaterialCategory.Metal, new[] { "Железо", "Сталь", "Духовное железо", "Звёздный металл", "Пустотная материя" } },
            { MaterialCategory.Leather, new[] { "Кожа", "Дублёная кожа", "Духовная кожа", "Драконья кожа", "Изначальная кожа" } },
            { MaterialCategory.Cloth, new[] { "Ткань", "Шёлк", "Духовный шёлк", "Небесный шёлк", "Изначальная ткань" } },
            { MaterialCategory.Crystal, new[] { "Хрусталь", "Горный хрусталь", "Духовный кристалл", "Звёздный кристалл", "Изначальный кристалл" } }
        };

        // Названия брони по подтипам
        private static readonly Dictionary<ArmorSubtype, string[]> ArmorNamesBySubtype = new Dictionary<ArmorSubtype, string[]>
        {
            { ArmorSubtype.Head, new[] { "Шлем", "Корона", "Капюшон", "Диадема", "Маска" } },
            { ArmorSubtype.Torso, new[] { "Нагрудник", "Кираса", "Кольчуга", "Мантия", "Доспех" } },
            { ArmorSubtype.Arms, new[] { "Наручи", "Наплечники", "Рукава", "Браслеты", "Поножи рук" } },
            { ArmorSubtype.Hands, new[] { "Перчатки", "Рукавицы", "Наручи кистей", "Кастеты", "Латные перчатки" } },
            { ArmorSubtype.Legs, new[] { "Поножи", "Наголенники", "Штаны", "Брюки", "Латные поножи" } },
            { ArmorSubtype.Feet, new[] { "Сабатоны", "Сапоги", "Ботинки", "Туфли", "Латные сапоги" } },
            { ArmorSubtype.Full, new[] { "Полный доспех", "Латы", "Броня", "Доспех тела", "Панцирь" } }
        };

        // Проводимость Ци материалов (источник: EQUIPMENT_SYSTEM.md §8.3)
        // | Материал        | Штраф к qi_efficiency |
        // |-----------------|----------------------|
        // | Ткань (cloth)   | 0%                   |
        // | Кожа (leather)  | -5%                  |
        // | Железо (iron)   | -10%                 |
        // | Сталь (steel)   | -15%                 |
        // | Духовное железо | +5%                  |
        // | Нефрит (jade)   | +10%                 |
        private static readonly Dictionary<MaterialCategory, float> QiFlowByCategory = new Dictionary<MaterialCategory, float>
        {
            { MaterialCategory.Cloth, 0f },
            { MaterialCategory.Leather, -5f },
            { MaterialCategory.Metal, -10f },  // Базовое железо
            { MaterialCategory.Crystal, 5f },  // Нефрит
            { MaterialCategory.Spirit, 10f },
            { MaterialCategory.Void, 20f }
        };

        /// <summary>
        /// Сгенерировать одну броню
        /// </summary>
        public static GeneratedArmor Generate(ArmorGenerationParams parameters, SeededRandom rng = null)
        {
            if (rng == null)
                rng = new SeededRandom(parameters.seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));

            var armor = new GeneratedArmor();

            // Classification
            armor.subtype = parameters.subtype;
            armor.weightClass = parameters.weightClass;
            armor.grade = parameters.grade;
            armor.itemLevel = Mathf.Clamp(parameters.itemLevel, 1, 9);
            armor.materialTier = Mathf.Clamp(parameters.materialTier, 1, 5);
            armor.materialCategory = parameters.materialCategory;

            // Вес (строчная модель инвентаря)
            var weightKey = (armor.subtype, armor.weightClass);
            float baseWeight = BaseWeightBySubtypeAndClass.ContainsKey(weightKey)
                ? BaseWeightBySubtypeAndClass[weightKey] : 3.0f;
            float matWMult = MaterialWeightMult[armor.materialTier - 1];
            armor.weight = baseWeight * matWMult * (1f + (armor.itemLevel - 1) * 0.05f);
            armor.volume = Mathf.Clamp(armor.weight, 1f, 4f);
            armor.stackable = false;
            armor.maxStack = 1;
            armor.allowNesting = NestingFlag.Any;
            armor.category = ItemCategory.Armor;

            // Material
            armor.materialId = GetMaterialName(armor.materialCategory, armor.materialTier, rng);

            // Protected parts
            armor.protectedParts = new List<BodyPartType>(GetProtectedParts(armor.subtype));

            // Base stats from weight class
            var (baseArmor, dodgePen, speedPen, qiPen) = StatsByWeightClass[armor.weightClass];

            // Apply level scaling
            float levelMult = 1f + (armor.itemLevel - 1) * 0.4f;
            baseArmor = Mathf.RoundToInt(baseArmor * levelMult);

            // Apply material tier bonus
            float materialMult = 1f + (armor.materialTier - 1) * 0.3f;
            baseArmor = Mathf.RoundToInt(baseArmor * materialMult);

            // Apply grade effectiveness
            float gradeEffMult = GetGradeEffectiveness(armor.grade);
            armor.armor = Mathf.RoundToInt(baseArmor * gradeEffMult);

            // Damage reduction (0-80% максимум)
            armor.damageReduction = Mathf.Min(80f, armor.armor * 0.5f + (int)armor.grade * 5f);

            // Coverage
            var (minCov, maxCov) = GetCoverage(armor.subtype);
            armor.coverage = rng.NextFloat(minCov, maxCov);

            // Penalties
            armor.dodgePenalty = dodgePen + (armor.weightClass == ArmorWeightClass.Heavy ? -5f : 0f);
            armor.moveSpeedPenalty = speedPen + (armor.weightClass == ArmorWeightClass.Heavy ? -5f : 0f);

            // Qi flow penalty (источник: EQUIPMENT_SYSTEM.md §8.3)
            float baseQiPen = GetQiFlowPenalty(armor.materialCategory, armor.materialTier);
            armor.qiFlowPenalty = baseQiPen + qiPen;

            // Durability
            var (minDur, maxDur, _, _) = MaterialTierStats[armor.materialTier - 1];
            int baseDurability = rng.Next(minDur, maxDur + 1);
            float gradeDurMult = GetGradeDurability(armor.grade);
            armor.maxDurability = Mathf.RoundToInt(baseDurability * gradeDurMult);
            armor.currentDurability = armor.maxDurability;

            // Requirements
            CalculateRequirements(armor, rng);

            // Bonuses
            GenerateBonuses(armor, rng);

            // Elemental resistances (для Perfect+ грейдов)
            if (armor.grade >= EquipmentGrade.Perfect)
                GenerateElementalResistances(armor, rng);

            // ID
            armor.id = $"armor_{armor.subtype}_{armor.materialTier}_{armor.itemLevel}_{rng.Next(1000):D3}";

            // Name
            armor.nameRu = GenerateName(armor, rng);
            armor.nameEn = armor.nameRu;

            // Description
            armor.description = GenerateDescription(armor);

            return armor;
        }

        /// <summary>
        /// Сгенерировать несколько броней
        /// </summary>
        public static List<GeneratedArmor> GenerateMultiple(ArmorGenerationParams parameters)
        {
            var rng = new SeededRandom(parameters.seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            var results = new List<GeneratedArmor>();

            for (int i = 0; i < parameters.count; i++)
            {
                var armor = Generate(parameters, rng);
                results.Add(armor);
            }

            return results;
        }

        /// <summary>
        /// Сгенерировать броню для уровня культивации
        /// </summary>
        public static GeneratedArmor GenerateForLevel(int cultivationLevel, SeededRandom rng = null)
        {
            if (rng == null)
                rng = new SeededRandom();

            int itemLevel = Mathf.Clamp(cultivationLevel, 1, 9);
            int materialTier = Mathf.Clamp((cultivationLevel + 1) / 2, 1, 5);

            // Случайный подтип
            var subtypes = (ArmorSubtype[])Enum.GetValues(typeof(ArmorSubtype));
            var subtype = rng.NextElement(subtypes);

            // Случайный весовой класс
            var weightClasses = (ArmorWeightClass[])Enum.GetValues(typeof(ArmorWeightClass));
            var weightClass = rng.NextElement(weightClasses);

            // Материал зависит от весового класса
            MaterialCategory materialCat = weightClass switch
            {
                ArmorWeightClass.Light => rng.NextElement(new[] { MaterialCategory.Cloth, MaterialCategory.Leather }),
                ArmorWeightClass.Medium => rng.NextElement(new[] { MaterialCategory.Leather, MaterialCategory.Metal }),
                ArmorWeightClass.Heavy => MaterialCategory.Metal,
                _ => MaterialCategory.Metal
            };

            var grade = GenerateGrade(rng, cultivationLevel); // FIX GEN-H02: Use local GenerateGrade instead of WeaponGenerator (2026-04-11)

            var parameters = new ArmorGenerationParams
            {
                subtype = subtype,
                weightClass = weightClass,
                itemLevel = itemLevel,
                grade = grade,
                materialTier = materialTier,
                materialCategory = materialCat,
                seed = rng.Next()
            };

            return Generate(parameters, new SeededRandom(parameters.seed.Value));
        }

        // === Helpers ===

        private static (float min, float max) GetCoverage(ArmorSubtype subtype)
        {
            if (CoverageBySubtype.ContainsKey(subtype))
                return CoverageBySubtype[subtype];
            return (50f, 80f);
        }

        private static BodyPartType[] GetProtectedParts(ArmorSubtype subtype)
        {
            if (ProtectedPartsBySubtype.ContainsKey(subtype))
                return ProtectedPartsBySubtype[subtype];
            return new BodyPartType[0];
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

        private static float GetQiFlowPenalty(MaterialCategory category, int tier)
        {
            // Базовый штраф по категории
            float basePen = QiFlowByCategory.ContainsKey(category) ? QiFlowByCategory[category] : 0f;

            // Улучшение с тирами
            // T1-T2: базовый штраф
            // T3+: улучшение на 5% за каждый тир
            if (tier >= 3)
                basePen += (tier - 2) * 5f;

            return basePen;
        }

        private static string GetMaterialName(MaterialCategory category, int tier, SeededRandom rng)
        {
            if (ArmorMaterialNames.ContainsKey(category))
            {
                var names = ArmorMaterialNames[category];
                int index = Mathf.Clamp(tier - 1, 0, names.Length - 1);
                return names[index];
            }
            return $"Материал T{tier}";
        }

        private static void CalculateRequirements(GeneratedArmor armor, SeededRandom rng)
        {
            // Сила зависит от весового класса
            armor.requiredStrength = armor.weightClass switch
            {
                ArmorWeightClass.Light => 3 + armor.itemLevel,
                ArmorWeightClass.Medium => 8 + armor.itemLevel * 2,
                ArmorWeightClass.Heavy => 15 + armor.itemLevel * 3,
                _ => 5
            };

            // Уровень культивации зависит от тира материала
            armor.requiredCultivationLevel = Mathf.Max(1, (armor.materialTier - 1) * 2);
        }

        private static void GenerateBonuses(GeneratedArmor armor, SeededRandom rng)
        {
            int minBonus = MinBonusesByGrade[(int)armor.grade];
            int maxBonus = MaxBonusesByGrade[(int)armor.grade];
            int bonusCount = rng.Next(minBonus, maxBonus + 1);

            string[] possibleBonuses = { "healthMax", "staminaRegen", "qiRegen", "resistance", "dodgeChance", "blockChance" };

            for (int i = 0; i < bonusCount; i++)
            {
                string bonusType = rng.NextElement(possibleBonuses);
                float value = rng.NextFloat(0.03f, 0.1f) * ((int)armor.grade + 1);

                armor.bonuses.Add(new ArmorBonus
                {
                    statName = bonusType,
                    value = value,
                    isPercentage = true
                });
            }
        }

        private static void GenerateElementalResistances(GeneratedArmor armor, SeededRandom rng)
        {
            // Perfect: 1-2 сопротивления
            // Transcendent: 2-4 сопротивления
            int count = armor.grade == EquipmentGrade.Perfect ? rng.Next(1, 3) : rng.Next(2, 5);

            var elements = new[] { Element.Fire, Element.Water, Element.Earth, Element.Air, Element.Lightning, Element.Void };

            for (int i = 0; i < count && i < elements.Length; i++)
            {
                var element = rng.NextElement(elements);
                if (!armor.elementalResistances.ContainsKey(element))
                {
                    armor.elementalResistances[element] = rng.NextFloat(5f, 25f);
                }
            }
        }

        private static string GenerateName(GeneratedArmor armor, SeededRandom rng)
        {
            string materialName = armor.materialId;
            string baseName = "";

            if (ArmorNamesBySubtype.ContainsKey(armor.subtype))
                baseName = rng.NextElement(ArmorNamesBySubtype[armor.subtype]);
            else
                baseName = armor.subtype.ToString();

            // Добавляем префикс грейда
            string gradePrefix = armor.grade switch
            {
                EquipmentGrade.Damaged => "Повреждённый ",
                EquipmentGrade.Refined => "Улучшенный ",
                EquipmentGrade.Perfect => "Совершенный ",
                EquipmentGrade.Transcendent => "Трансцендентный ",
                _ => ""
            };

            // Добавляем весовой класс
            string weightPrefix = armor.weightClass switch
            {
                ArmorWeightClass.Light => "Лёгкий ",
                ArmorWeightClass.Heavy => "Тяжёлый ",
                _ => ""
            };

            return $"{gradePrefix}{weightPrefix}{materialName} {baseName}";
        }

        private static string GenerateDescription(GeneratedArmor armor)
        {
            var parts = new List<string>();
            parts.Add($"{armor.nameRu} уровня {armor.itemLevel}");
            parts.Add($"Броня: {armor.armor}, Снижение урона: {armor.damageReduction:F0}%");
            parts.Add($"Покрытие: {armor.coverage:F0}%");

            if (armor.dodgePenalty != 0)
                parts.Add($"Штраф уклонения: {armor.dodgePenalty:F0}%");
            if (armor.moveSpeedPenalty != 0)
                parts.Add($"Штраф скорости: {armor.moveSpeedPenalty:F0}%");
            if (armor.qiFlowPenalty != 0)
                parts.Add($"Проводимость Ци: {armor.qiFlowPenalty:+0;-0;0}%");

            parts.Add($"Прочность: {armor.currentDurability}/{armor.maxDurability}");

            return string.Join(". ", parts) + ".";
        }

        /// <summary>
        /// Сгенерировать Grade по распределению (локальная копия).
        /// FIX GEN-H02: Extracted from WeaponGenerator to remove cross-dependency (2026-04-11)
        /// </summary>
        private static EquipmentGrade GenerateGrade(SeededRandom rng, int level = 1)
        {
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

        /// <summary>
        /// Вывести примеры сгенерированной брони
        /// </summary>
        public static string GenerateExamples()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== ПРИМЕРЫ СГЕНЕРИРОВАННОЙ БРОНИ ===\n");

            var testCases = new[]
            {
                (ArmorSubtype.Torso, ArmorWeightClass.Light, 1, EquipmentGrade.Common, 1, MaterialCategory.Cloth),
                (ArmorSubtype.Head, ArmorWeightClass.Medium, 3, EquipmentGrade.Refined, 2, MaterialCategory.Leather),
                (ArmorSubtype.Torso, ArmorWeightClass.Heavy, 5, EquipmentGrade.Perfect, 3, MaterialCategory.Metal),
                (ArmorSubtype.Full, ArmorWeightClass.Heavy, 7, EquipmentGrade.Perfect, 4, MaterialCategory.Metal),
                (ArmorSubtype.Torso, ArmorWeightClass.Light, 9, EquipmentGrade.Transcendent, 5, MaterialCategory.Crystal),
            };

            var rng = new SeededRandom(12345);

            foreach (var (subtype, weight, level, grade, tier, matCat) in testCases)
            {
                var parameters = new ArmorGenerationParams
                {
                    subtype = subtype,
                    weightClass = weight,
                    itemLevel = level,
                    grade = grade,
                    materialTier = tier,
                    materialCategory = matCat,
                    seed = rng.Next()
                };

                var armor = Generate(parameters, new SeededRandom(parameters.seed.Value));

                sb.AppendLine($"[{armor.grade}] {armor.nameRu}");
                sb.AppendLine($"  ID: {armor.id}");
                sb.AppendLine($"  Весовой класс: {armor.weightClass}");
                sb.AppendLine($"  Броня: {armor.armor}, Снижение урона: {armor.damageReduction:F0}%");
                sb.AppendLine($"  Покрытие: {armor.coverage:F0}%");
                sb.AppendLine($"  Защищаемые части: {string.Join(", ", armor.protectedParts)}");
                sb.AppendLine($"  Прочность: {armor.currentDurability}/{armor.maxDurability}");
                sb.AppendLine($"  Вес: {armor.weight:F2}кг, Объём: {armor.volume:F1}л");
                sb.AppendLine($"  Проводимость Ци: {armor.qiFlowPenalty:+0;-0;0}%");
                sb.AppendLine($"  Требования: СИЛ {armor.requiredStrength}, Ур.{armor.requiredCultivationLevel}+");
                if (armor.elementalResistances.Count > 0)
                    sb.AppendLine($"  Сопротивления: {string.Join(", ", armor.elementalResistances)}");
                if (armor.bonuses.Count > 0)
                    sb.AppendLine($"  Бонусы: {string.Join(", ", armor.bonuses.ConvertAll(b => $"{b.statName} +{b.value * 100:F0}%"))}");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
