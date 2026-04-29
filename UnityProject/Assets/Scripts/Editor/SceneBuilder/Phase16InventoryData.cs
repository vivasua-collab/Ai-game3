// ============================================================================
// Phase16InventoryData.cs — Фаза 16: Данные инвентаря (Backpack, StorageRing)
// Cultivation World Simulator
// ============================================================================
// Создаёт BackpackData и StorageRingData .asset файлы.
// Добавляет папки Assets/Data/Backpacks и Assets/Data/StorageRings.
// Обновляет существующие ItemData .asset файлы: volume и allowNesting.
//
// Редактировано: 2026-04-20 06:45:00 UTC — StorageRingData → EquipmentData, +slot
// Редактировано: 2026-04-25 19:00:00 MSK — +AddTestEquipmentSet() базовый набор шмота
//   для проверки инвентаря и куклы.
// Редактировано: 2026-04-27 18:15:00 UTC — строчная модель инвентаря
// Редактировано: 2026-04-29 08:55:00 UTC — интеграция GradeColors (Д9, Д10, Д11)
// Редактировано: 2026-04-29 09:30:00 UTC — Этап 5: замена CreateTestEquipment на генераторы
//   WeaponGenerator + ArmorGenerator + EquipmentSOFactory вместо хардкода
// Редактировано: 2026-04-29 12:03:16 UTC — исправление некорректных дат (05-01 → 04-29)
// Редактировано: 2026-04-29 12:13:35 UTC — аудит: IsNeeded() расширена, EnsureSceneOpen убран из Execute
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;
using CultivationGame.Generators;
// GradeColors — единая точка доступа к цветам Grade/Tier (Д12)

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase16InventoryData : IScenePhase
    {
        public string Name => "Inventory Data";
        public string MenuPath => "Phase 16: Inventory Data";
        public int Order => 16;

        // Папки для ассетов инвентаря
        private const string BACKPACKS_FOLDER = "Assets/Data/Backpacks";
        private const string STORAGERINGS_FOLDER = "Assets/Data/StorageRings";

        public bool IsNeeded()
        {
            // Проверяем все создаваемые ресурсы
            if (!AssetDatabase.IsValidFolder(BACKPACKS_FOLDER)) return true;
            if (!AssetDatabase.IsValidFolder(STORAGERINGS_FOLDER)) return true;
            // Тестовый набор экипировки
            if (!AssetDatabase.IsValidFolder(BASIC_FOLDER)) return true;
            if (AssetDatabase.LoadAssetAtPath<EquipmentData>($"{BASIC_FOLDER}/weapon_Sword_T1_Common.asset") == null) return true;
            return false;
        }

        public void Execute()
        {
            // Фаза не модифицирует сцену — EnsureSceneOpen не нужен

            // Шаг 1: Создать папки
            SceneBuilderUtils.EnsureDirectory(BACKPACKS_FOLDER);
            SceneBuilderUtils.EnsureDirectory(STORAGERINGS_FOLDER);
            Debug.Log("[Phase16] Папки Backpacks и StorageRings созданы");

            // Шаг 2: Создать BackpackData .asset файлы (5 пресетов)
            CreateBackpackData("Backpack_ClothSack", "Тканевая сумка",
                maxWeight: 30, maxVolume: 50, weightReduction: 0f, beltSlots: 0, ownWeight: 0.5f,
                ItemRarity.Common, weight: 0.5f, value: 5);

            CreateBackpackData("Backpack_LeatherPack", "Кожаный ранец",
                maxWeight: 50, maxVolume: 80, weightReduction: 10f, beltSlots: 1, ownWeight: 2.0f,
                ItemRarity.Uncommon, weight: 1.0f, value: 25);

            CreateBackpackData("Backpack_IronContainer", "Железный контейнер",
                maxWeight: 80, maxVolume: 120, weightReduction: 15f, beltSlots: 2, ownWeight: 5.0f,
                ItemRarity.Rare, weight: 3.0f, value: 80);

            CreateBackpackData("Backpack_SpiritBag", "Духовный мешок",
                maxWeight: 120, maxVolume: 200, weightReduction: 25f, beltSlots: 2, ownWeight: 3.0f,
                ItemRarity.Epic, weight: 1.5f, value: 250);

            CreateBackpackData("Backpack_SpatialChest", "Межпространственный сундук",
                maxWeight: 200, maxVolume: 500, weightReduction: 40f, beltSlots: 4, ownWeight: 1.0f,
                ItemRarity.Legendary, weight: 2.0f, value: 1000);

            // Шаг 3: Создать StorageRingData .asset файлы (4 пресета)
            CreateStorageRingData("StorageRing_Slit", "Кольцо-щель",
                maxVolume: 5f, qiCostBase: 5, qiCostPerUnit: 3f, accessTime: 1.5f,
                ItemRarity.Common, weight: 0.05f, value: 50);

            CreateStorageRingData("StorageRing_Pocket", "Кольцо-карман",
                maxVolume: 15f, qiCostBase: 5, qiCostPerUnit: 2f, accessTime: 1.5f,
                ItemRarity.Uncommon, weight: 0.05f, value: 150);

            CreateStorageRingData("StorageRing_Vault", "Кольцо-кладовая",
                maxVolume: 30f, qiCostBase: 5, qiCostPerUnit: 1f, accessTime: 1.5f,
                ItemRarity.Rare, weight: 0.05f, value: 400);

            CreateStorageRingData("StorageRing_Space", "Кольцо-пространство",
                maxVolume: 60f, qiCostBase: 5, qiCostPerUnit: 0.5f, accessTime: 1.5f,
                ItemRarity.Epic, weight: 0.05f, value: 1200);

            // Шаг 4: Обновить существующие ItemData — volume и allowNesting
            UpdateExistingItemData();

            // Шаг 5: Создать базовый набор тестовой экипировки
            AddTestEquipmentSet();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase16] ✅ Inventory Data созданы: 5 Backpacks + 4 StorageRings + Test Equipment + ItemData обновлены");
        }

        // ====================================================================
        //  BackpackData creation
        // ====================================================================

        private void CreateBackpackData(
            string fileName, string nameRu,
            float maxWeight, float maxVolume,
            float weightReduction, int beltSlots, float ownWeight,
            ItemRarity rarity, float weight, int value)
        {
            string assetPath = $"{BACKPACKS_FOLDER}/{fileName}.asset";

            // Не перезаписывать существующие
            if (AssetDatabase.LoadAssetAtPath<BackpackData>(assetPath) != null)
            {
                Debug.Log($"[Phase16] {fileName} уже существует — пропускаем");
                return;
            }

            var data = ScriptableObject.CreateInstance<BackpackData>();
            data.itemId = fileName;
            data.nameRu = nameRu;
            data.nameEn = fileName.Replace("_", " ");
            data.description = $"Рюкзак: {nameRu}";
            data.category = ItemCategory.Accessory;
            data.itemType = "Backpack";
            data.rarity = rarity;
            data.stackable = false;
            data.maxStack = 1;
            data.weight = weight;
            data.value = value;
            data.hasDurability = false;
            data.volume = weight; // объём рюкзака = его вес
            data.allowNesting = NestingFlag.None; // Рюкзак нельзя вложить в хранилище

            // BackpackData-specific fields (строчная модель)
            data.maxWeight = maxWeight;
            data.maxVolume = maxVolume;
            data.weightReduction = weightReduction;
            data.beltSlots = beltSlots;
            data.ownWeight = ownWeight;

            AssetDatabase.CreateAsset(data, assetPath);
            Debug.Log($"[Phase16] Создан {fileName}: maxWeight={maxWeight}kg, maxVolume={maxVolume}, " +
                      $"weightReduction={weightReduction}%, belt={beltSlots}, ownWeight={ownWeight}kg");
        }

        // ====================================================================
        //  StorageRingData creation
        // ====================================================================

        private void CreateStorageRingData(
            string fileName, string nameRu,
            float maxVolume, int qiCostBase, float qiCostPerUnit, float accessTime,
            ItemRarity rarity, float weight, int value)
        {
            string assetPath = $"{STORAGERINGS_FOLDER}/{fileName}.asset";

            // Не перезаписывать существующие
            if (AssetDatabase.LoadAssetAtPath<StorageRingData>(assetPath) != null)
            {
                Debug.Log($"[Phase16] {fileName} уже существует — пропускаем");
                return;
            }

            var data = ScriptableObject.CreateInstance<StorageRingData>();
            data.itemId = fileName;
            data.nameRu = nameRu;
            data.nameEn = fileName.Replace("_", " ");
            data.description = $"Кольцо хранения: {nameRu} (объём {maxVolume})";
            data.category = ItemCategory.Accessory;
            data.itemType = "StorageRing";
            data.rarity = rarity;
            data.stackable = false;
            data.maxStack = 1;
            data.volume = 0.05f;
            data.allowNesting = NestingFlag.None; // Кольцо нельзя вложить — пространственная нестабильность

            // EquipmentData-поля (StorageRingData наследует от EquipmentData)
            data.slot = EquipmentSlot.RingLeft1; // дефолтный слот — переназначается при экипировке
            data.handType = WeaponHandType.OneHand;

            // StorageRingData-specific fields
            data.maxVolume = maxVolume;
            data.qiCostBase = qiCostBase;
            data.qiCostPerUnit = qiCostPerUnit;
            data.accessTime = accessTime;

            AssetDatabase.CreateAsset(data, assetPath);
            Debug.Log($"[Phase16] Создан {fileName}: volume={maxVolume}, qiCost={qiCostBase}+{qiCostPerUnit}/unit");
        }

        // ====================================================================
        //  Update existing ItemData with volume/allowNesting defaults
        // ====================================================================

        private void UpdateExistingItemData()
        {
            string[] itemFolders = new string[] { "Assets/Data/Items", "Assets/Data/Equipment", "Assets/Data/Materials" };
            int updated = 0;

            foreach (var folder in itemFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;

                var guids = AssetDatabase.FindAssets("t:ItemData", new[] { folder });
                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                    if (item == null) continue;

                    bool changed = false;

                    // Установить volume по категории, если дефолт 1.0
                    if (Mathf.Approximately(item.volume, 1.0f))
                    {
                        float defaultVolume = GetDefaultVolume(item.category);
                        if (!Mathf.Approximately(defaultVolume, 1.0f))
                        {
                            item.volume = defaultVolume;
                            changed = true;
                        }
                    }

                    // Установить allowNesting по категории
                    NestingFlag defaultNesting = GetDefaultNesting(item.category);
                    if (item.allowNesting != defaultNesting)
                    {
                        item.allowNesting = defaultNesting;
                        changed = true;
                    }

                    if (changed)
                    {
                        EditorUtility.SetDirty(item);
                        updated++;
                    }
                }
            }

            if (updated > 0)
                Debug.Log($"[Phase16] Обновлено {updated} ItemData: volume + allowNesting");
        }

        /// <summary>
        /// Дефолтный объём по категории предмета.
        /// Источник: checkpoints/04_18_data_model_rewrite.md §0.3
        /// </summary>
        private float GetDefaultVolume(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Consumable: return 0.1f;
                case ItemCategory.Material: return 0.5f;
                case ItemCategory.Weapon: return 2.0f;
                case ItemCategory.Armor: return 2.0f;
                case ItemCategory.Accessory: return 0.2f;
                case ItemCategory.Quest: return 1.0f;
                case ItemCategory.Technique: return 0.1f;
                case ItemCategory.Misc: return 1.0f;
                default: return 1.0f;
            }
        }

        /// <summary>
        /// Дефолтный флаг вложения по категории.
        /// Источник: checkpoints/04_18_data_model_rewrite.md §0.3
        /// </summary>
        private NestingFlag GetDefaultNesting(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Quest: return NestingFlag.None;
                case ItemCategory.Technique: return NestingFlag.Spirit;
                default: return NestingFlag.Any;
            }
        }

        // ====================================================================
        //  Test Equipment Set — процедурная генерация через Weapon/Armor генераторы
        // ====================================================================
        // Первоначальный хардкод заменён на генераторы (Этап 5)
        // WeaponGenerator + ArmorGenerator + EquipmentSOFactory

        private const string TEST_EQUIP_FOLDER = "Assets/Data/Equipment/TestSet";
        private const string BASIC_FOLDER = "Assets/Data/Equipment/TestSet/Basic";    // T1 Common
        private const string UPGRADED_FOLDER = "Assets/Data/Equipment/TestSet/Upgraded"; // T3 Refined

        /// <summary>
        /// Создаёт набор экипировки через процедурные генераторы.
        /// Базовый набор: 5 оружия + 5 брони (T1, Common, Level 1)
        /// Улучшенный набор: 3 оружия + 3 брони (T3, Refined, Level 3-5)
        /// </summary>
        private void AddTestEquipmentSet()
        {
            SceneBuilderUtils.EnsureDirectory(BASIC_FOLDER);
            SceneBuilderUtils.EnsureDirectory(UPGRADED_FOLDER);

            // Проверяем — если хоть один тестовый предмет существует, пропускаем
            if (AssetDatabase.LoadAssetAtPath<EquipmentData>($"{BASIC_FOLDER}/weapon_Sword_T1_Common.asset") != null)
            {
                Debug.Log("[Phase16] Test Equipment Set уже существует — пропускаем");
                return;
            }

            var rng = new SeededRandom(42);
            int totalCreated = 0;

            // ============================================================
            //  БАЗОВЫЙ НАБОР: 5 оружия + 5 брони (T1, Common, Level 1)
            // ============================================================

            // Оружие — по одному каждого типа для проверки слотов
            var basicWeapons = new (WeaponSubtype subtype, string fileName)[]
            {
                (WeaponSubtype.Sword,       "weapon_Sword_T1_Common"),
                (WeaponSubtype.Dagger,      "weapon_Dagger_T1_Common"),
                (WeaponSubtype.Greatsword,  "weapon_Greatsword_T1_Common"),   // двуручное
                (WeaponSubtype.Axe,         "weapon_Axe_T1_Common"),
                (WeaponSubtype.Mace,        "weapon_Mace_T1_Common"),
            };

            foreach (var (subtype, fileName) in basicWeapons)
            {
                var dto = WeaponGenerator.Generate(new WeaponGenerationParams
                {
                    subtype = subtype,
                    itemLevel = 1,
                    grade = EquipmentGrade.Common,
                    materialTier = 1,
                    materialCategory = MaterialCategory.Metal,
                    seed = rng.Next()
                }, new SeededRandom(rng.Next()));

                string path = $"{BASIC_FOLDER}/{fileName}.asset";
                EquipmentSOFactory.CreateFromWeapon(dto, path);
                totalCreated++;
            }

            // Броня — по одному на каждый слот
            var basicArmors = new (ArmorSubtype subtype, ArmorWeightClass weightClass, MaterialCategory matCat, string fileName)[]
            {
                (ArmorSubtype.Head,  ArmorWeightClass.Medium, MaterialCategory.Metal,   "armor_Head_Medium_T1_Common"),
                (ArmorSubtype.Torso, ArmorWeightClass.Light,  MaterialCategory.Cloth,   "armor_Torso_Light_T1_Common"),
                (ArmorSubtype.Arms,  ArmorWeightClass.Light,  MaterialCategory.Leather, "armor_Arms_Light_T1_Common"),   // → Hands
                (ArmorSubtype.Legs,  ArmorWeightClass.Light,  MaterialCategory.Cloth,   "armor_Legs_Light_T1_Common"),
                (ArmorSubtype.Feet,  ArmorWeightClass.Light,  MaterialCategory.Leather, "armor_Feet_Light_T1_Common"),
            };

            foreach (var (subtype, weightClass, matCat, fileName) in basicArmors)
            {
                var dto = ArmorGenerator.Generate(new ArmorGenerationParams
                {
                    subtype = subtype,
                    weightClass = weightClass,
                    itemLevel = 1,
                    grade = EquipmentGrade.Common,
                    materialTier = 1,
                    materialCategory = matCat,
                    seed = rng.Next()
                }, new SeededRandom(rng.Next()));

                string path = $"{BASIC_FOLDER}/{fileName}.asset";
                EquipmentSOFactory.CreateFromArmor(dto, path);
                totalCreated++;
            }

            // ============================================================
            //  УЛУЧШЕННЫЙ НАБОР: 3 оружия + 3 брони (T3, Refined, Level 3-5)
            // ============================================================

            var upgradedWeapons = new (WeaponSubtype subtype, int itemLevel, string fileName)[]
            {
                (WeaponSubtype.Sword,  3, "weapon_Sword_T3_Refined"),
                (WeaponSubtype.Staff,  4, "weapon_Staff_T3_Refined"),
                (WeaponSubtype.Spear,  5, "weapon_Spear_T3_Refined"),   // двуручное
            };

            foreach (var (subtype, itemLevel, fileName) in upgradedWeapons)
            {
                var dto = WeaponGenerator.Generate(new WeaponGenerationParams
                {
                    subtype = subtype,
                    itemLevel = itemLevel,
                    grade = EquipmentGrade.Refined,
                    materialTier = 3,
                    materialCategory = MaterialCategory.Metal, // Spirit Iron
                    seed = rng.Next()
                }, new SeededRandom(rng.Next()));

                string path = $"{UPGRADED_FOLDER}/{fileName}.asset";
                EquipmentSOFactory.CreateFromWeapon(dto, path);
                totalCreated++;
            }

            var upgradedArmors = new (ArmorSubtype subtype, ArmorWeightClass weightClass, int itemLevel, string fileName)[]
            {
                (ArmorSubtype.Head,  ArmorWeightClass.Heavy,  3, "armor_Head_Heavy_T3_Refined"),
                (ArmorSubtype.Torso, ArmorWeightClass.Medium, 4, "armor_Torso_Medium_T3_Refined"),
                (ArmorSubtype.Legs,  ArmorWeightClass.Light,  5, "armor_Legs_Light_T3_Refined"),
            };

            foreach (var (subtype, weightClass, itemLevel, fileName) in upgradedArmors)
            {
                var dto = ArmorGenerator.Generate(new ArmorGenerationParams
                {
                    subtype = subtype,
                    weightClass = weightClass,
                    itemLevel = itemLevel,
                    grade = EquipmentGrade.Refined,
                    materialTier = 3,
                    materialCategory = MaterialCategory.Metal, // Spirit Iron
                    seed = rng.Next()
                }, new SeededRandom(rng.Next()));

                string path = $"{UPGRADED_FOLDER}/{fileName}.asset";
                EquipmentSOFactory.CreateFromArmor(dto, path);
                totalCreated++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Phase16] ✅ Test Equipment Set создан через генераторы: {totalCreated} предметов " +
                      "(10 Basic T1 + 6 Upgraded T3)");
        }


    }
}
#endif
