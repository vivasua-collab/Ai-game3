// ============================================================================
// EquipmentGeneratorMenu.cs — Editor-меню генерации экипировки
// Cultivation World Simulator
// Создано: 2026-04-29 09:23:41 UTC
// ============================================================================
//
// Этап 2 чекпоинта 04_29_equipment_generator_integration_plan.md
//
// Пункты меню (Tools/Equipment):
//   1. Generate Weapon Set (T1)         — 12 подтипов × 3 грейда = 36 SO
//   2. Generate Weapon Set (All Tiers)   — ×5 тиров = 180 SO
//   3. Generate Armor Set (T1)           — 7×3 вес.класс × 3 грейда = 63 SO
//   4. Generate Armor Set (All Tiers)    — ×5 тиров = 315 SO
//   5. Generate Full Set (T1)            — оружие + броня T1 = 99 SO
//   6. Generate Random Loot              — 3 случайных предмета уровня 1
//   7. Clear Generated Equipment         — удаление папки Generated/
//
// Структура папок:
//   Assets/Data/Equipment/Generated/{Weapons,Armor}/T{1-5}/
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using CultivationGame.Core;
using CultivationGame.Generators;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Editor-меню для процедурной генерации экипировки.
    /// Использует WeaponGenerator/ArmorGenerator + EquipmentSOFactory.
    /// </summary>
    public static class EquipmentGeneratorMenu
    {
        private const string OUTPUT_BASE = "Assets/Data/Equipment/Generated";

        // ================================================================
        //  ПУНКТЫ МЕНЮ
        // ================================================================

        /// Генерация оружия T1 (12 подтипов × 3 грейда = 36 SO)
        [MenuItem("Tools/Equipment/Generate Weapon Set (T1)", false, 20)]
        public static void GenerateWeaponSetT1() => GenerateWeaponSet(1);

        /// Генерация оружия всех тиров (12×3×5 = 180 SO)
        [MenuItem("Tools/Equipment/Generate Weapon Set (All Tiers)", false, 21)]
        public static void GenerateWeaponSetAll()
        {
            int total = 0;
            for (int t = 1; t <= 5; t++)
                total += GenerateWeaponSet(t);
            Debug.Log($"[EquipmentGenerator] Weapon sets: {total} items across all tiers");
        }

        /// Генерация брони T1 (7 подтипов × 3 вес.класс × 3 грейда = 63 SO)
        [MenuItem("Tools/Equipment/Generate Armor Set (T1)", false, 22)]
        public static void GenerateArmorSetT1() => GenerateArmorSet(1);

        /// Генерация брони всех тиров (7×3×3×5 = 315 SO)
        [MenuItem("Tools/Equipment/Generate Armor Set (All Tiers)", false, 23)]
        public static void GenerateArmorSetAll()
        {
            int total = 0;
            for (int t = 1; t <= 5; t++)
                total += GenerateArmorSet(t);
            Debug.Log($"[EquipmentGenerator] Armor sets: {total} items across all tiers");
        }

        /// Генерация полного набора T1 (оружие + броня = 99 SO)
        [MenuItem("Tools/Equipment/Generate Full Set (T1)", false, 24)]
        public static void GenerateFullSetT1()
        {
            int weapons = GenerateWeaponSet(1);
            int armors = GenerateArmorSet(1);
            Debug.Log($"[EquipmentGenerator] Full set T1: {weapons} weapons + {armors} armors = {weapons + armors} total");
        }

        /// Генерация случайного лута (3 предмета уровня 1)
        [MenuItem("Tools/Equipment/Generate Random Loot", false, 25)]
        public static void GenerateRandomLoot()
        {
            string lootPath = $"{OUTPUT_BASE}/Loot";
            EnsureDirectory(lootPath);

            var rng = new SeededRandom();
            int count = 0;

            for (int i = 0; i < 3; i++)
            {
                if (rng.NextBool(0.5f))
                {
                    // Генерация оружия
                    var dto = WeaponGenerator.GenerateForLevel(1, rng);
                    string fileName = $"loot_weapon_{dto.subtype}_{dto.grade}_{i}.asset";
                    string path = Path.Combine(lootPath, fileName);
                    EquipmentSOFactory.CreateFromWeapon(dto, path);
                    count++;
                }
                else
                {
                    // Генерация брони
                    var dto = ArmorGenerator.GenerateForLevel(1, rng);
                    string fileName = $"loot_armor_{dto.subtype}_{dto.grade}_{i}.asset";
                    string path = Path.Combine(lootPath, fileName);
                    EquipmentSOFactory.CreateFromArmor(dto, path);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[EquipmentGenerator] Random loot: {count} items generated");
        }

        /// Удаление всей сгенерированной экипировки
        [MenuItem("Tools/Equipment/Clear Generated Equipment", false, 100)]
        public static void ClearGenerated()
        {
            if (AssetDatabase.IsValidFolder(OUTPUT_BASE))
            {
                AssetDatabase.DeleteAsset(OUTPUT_BASE);
                AssetDatabase.Refresh();
                Debug.Log("[EquipmentGenerator] Cleared all generated equipment");
            }
            else
            {
                Debug.Log("[EquipmentGenerator] Nothing to clear — folder does not exist");
            }
        }

        // ================================================================
        //  ГЕНЕРАЦИЯ ОРУЖИЯ
        // ================================================================

        /// <summary>
        /// Генерация набора оружия для указанного тира.
        /// Итерирует все подтипы × грейды, создаёт .asset через EquipmentSOFactory.
        /// Возвращает количество созданных предметов.
        /// </summary>
        private static int GenerateWeaponSet(int tier)
        {
            string tierPath = $"{OUTPUT_BASE}/Weapons/T{tier}";
            EnsureDirectory(tierPath);

            var rng = new SeededRandom(12345 + tier);
            var subtypes = (WeaponSubtype[])System.Enum.GetValues(typeof(WeaponSubtype));
            var grades = new[] { EquipmentGrade.Common, EquipmentGrade.Refined, EquipmentGrade.Perfect };
            var materials = GetMaterialForTier(tier);

            int count = 0;
            foreach (var subtype in subtypes)
            {
                foreach (var grade in grades)
                {
                    var parameters = new WeaponGenerationParams
                    {
                        subtype = subtype,
                        itemLevel = Mathf.Clamp(tier * 2 - 1, 1, 9),
                        grade = grade,
                        materialTier = tier,
                        materialCategory = materials,
                        seed = rng.Next()
                    };

                    var dto = WeaponGenerator.Generate(parameters, new SeededRandom(parameters.seed.Value));
                    string fileName = $"weapon_{dto.subtype}_T{dto.materialTier}_{dto.grade}.asset";
                    string path = Path.Combine(tierPath, fileName);

                    EquipmentSOFactory.CreateFromWeapon(dto, path);
                    count++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[EquipmentGenerator] Generated {count} weapons for T{tier}");
            return count;
        }

        // ================================================================
        //  ГЕНЕРАЦИЯ БРОНИ
        // ================================================================

        /// <summary>
        /// Генерация набора брони для указанного тира.
        /// Итерирует все подтипы × весовые классы × грейды.
        /// Возвращает количество созданных предметов.
        /// </summary>
        private static int GenerateArmorSet(int tier)
        {
            string tierPath = $"{OUTPUT_BASE}/Armor/T{tier}";
            EnsureDirectory(tierPath);

            var rng = new SeededRandom(54321 + tier);
            var subtypes = (ArmorSubtype[])System.Enum.GetValues(typeof(ArmorSubtype));
            var weightClasses = (ArmorWeightClass[])System.Enum.GetValues(typeof(ArmorWeightClass));
            var grades = new[] { EquipmentGrade.Common, EquipmentGrade.Refined, EquipmentGrade.Perfect };

            int count = 0;
            foreach (var subtype in subtypes)
            {
                foreach (var weightClass in weightClasses)
                {
                    // Материал зависит от весового класса
                    MaterialCategory materialCat = GetMaterialForWeightClass(weightClass, tier, rng);

                    foreach (var grade in grades)
                    {
                        var parameters = new ArmorGenerationParams
                        {
                            subtype = subtype,
                            weightClass = weightClass,
                            itemLevel = Mathf.Clamp(tier * 2 - 1, 1, 9),
                            grade = grade,
                            materialTier = tier,
                            materialCategory = materialCat,
                            seed = rng.Next()
                        };

                        var dto = ArmorGenerator.Generate(parameters, new SeededRandom(parameters.seed.Value));
                        string fileName = $"armor_{dto.subtype}_{dto.weightClass}_T{dto.materialTier}_{dto.grade}.asset";
                        string path = Path.Combine(tierPath, fileName);

                        EquipmentSOFactory.CreateFromArmor(dto, path);
                        count++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[EquipmentGenerator] Generated {count} armors for T{tier}");
            return count;
        }

        // ================================================================
        //  ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
        // ================================================================

        /// <summary>
        /// Категория материала по тиру (для оружия).
        /// T1-T3: металл, T4: кристалл, T5: пустотный
        /// </summary>
        private static MaterialCategory GetMaterialForTier(int tier) => tier switch
        {
            1 => MaterialCategory.Metal,
            2 => MaterialCategory.Metal,
            3 => MaterialCategory.Metal,     // Духовное железо
            4 => MaterialCategory.Crystal,   // Звёздный кристалл
            5 => MaterialCategory.Void,      // Пустотная материя
            _ => MaterialCategory.Metal
        };

        /// <summary>
        /// Категория материала по весовому классу и тиру (для брони).
        /// Лёгкая → ткань/кожа, Средняя → кожа/металл, Тяжёлая → металл.
        /// Тир 4+ может использовать кристалл/пустотный.
        /// </summary>
        private static MaterialCategory GetMaterialForWeightClass(ArmorWeightClass weightClass, int tier, SeededRandom rng)
        {
            // На высоких тирах появляются редкие материалы
            if (tier >= 5)
                return weightClass == ArmorWeightClass.Heavy
                    ? MaterialCategory.Void
                    : rng.NextBool(0.5f) ? MaterialCategory.Crystal : MaterialCategory.Spirit;

            if (tier >= 4)
                return weightClass == ArmorWeightClass.Heavy
                    ? MaterialCategory.Crystal
                    : MaterialCategory.Metal;

            // T1-T3: материал зависит от весового класса
            return weightClass switch
            {
                ArmorWeightClass.Light => rng.NextBool(0.5f) ? MaterialCategory.Cloth : MaterialCategory.Leather,
                ArmorWeightClass.Medium => rng.NextBool(0.5f) ? MaterialCategory.Leather : MaterialCategory.Metal,
                ArmorWeightClass.Heavy => MaterialCategory.Metal,
                _ => MaterialCategory.Metal
            };
        }

        /// <summary>
        /// Создание вложенных папок для AssetDatabase.
        /// Использует System.IO.Directory.CreateDirectory + AssetDatabase.Refresh.
        /// </summary>
        private static void EnsureDirectory(string assetPath)
        {
            // Конвертация asset-пути в файловый путь
            string fullPath = Path.Combine(System.IO.Directory.GetParent(Application.dataPath).FullName, assetPath);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
                AssetDatabase.Refresh();
            }
        }
    }
}
#endif
