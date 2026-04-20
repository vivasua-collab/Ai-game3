// ============================================================================
// Phase16InventoryData.cs — Фаза 16: Данные инвентаря (Backpack, StorageRing)
// Cultivation World Simulator
// ============================================================================
// Создаёт BackpackData и StorageRingData .asset файлы.
// Добавляет папки Assets/Data/Backpacks и Assets/Data/StorageRings.
// Обновляет существующие ItemData .asset файлы: volume и allowNesting.
//
// Редактировано: 2026-04-20 06:45:00 UTC — StorageRingData → EquipmentData, +slot
//
// Зависимости: Phase09GenerateAssets (ItemData уже созданы)
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

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
            // Проверяем наличие папки Backpacks — если нет, фаза нужна
            return !AssetDatabase.IsValidFolder(BACKPACKS_FOLDER);
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            // Шаг 1: Создать папки
            SceneBuilderUtils.EnsureDirectory(BACKPACKS_FOLDER);
            SceneBuilderUtils.EnsureDirectory(STORAGERINGS_FOLDER);
            Debug.Log("[Phase16] Папки Backpacks и StorageRings созданы");

            // Шаг 2: Создать BackpackData .asset файлы (5 пресетов)
            CreateBackpackData("Backpack_ClothSack", "Тканевая сумка",
                gridWidth: 3, gridHeight: 4, weightReduction: 0f, maxWeightBonus: 0f, beltSlots: 0,
                ItemRarity.Common, weight: 0.5f, value: 5);

            CreateBackpackData("Backpack_LeatherPack", "Кожаный ранец",
                gridWidth: 4, gridHeight: 5, weightReduction: 10f, maxWeightBonus: 10f, beltSlots: 1,
                ItemRarity.Uncommon, weight: 1.0f, value: 25);

            CreateBackpackData("Backpack_IronContainer", "Железный контейнер",
                gridWidth: 5, gridHeight: 5, weightReduction: 15f, maxWeightBonus: 20f, beltSlots: 2,
                ItemRarity.Rare, weight: 3.0f, value: 80);

            CreateBackpackData("Backpack_SpiritBag", "Духовный мешок",
                gridWidth: 6, gridHeight: 6, weightReduction: 25f, maxWeightBonus: 30f, beltSlots: 2,
                ItemRarity.Epic, weight: 1.5f, value: 250);

            CreateBackpackData("Backpack_SpatialChest", "Межпространственный сундук",
                gridWidth: 8, gridHeight: 7, weightReduction: 40f, maxWeightBonus: 50f, beltSlots: 4,
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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase16] ✅ Inventory Data созданы: 5 Backpacks + 4 StorageRings + ItemData обновлены");
        }

        // ====================================================================
        //  BackpackData creation
        // ====================================================================

        private void CreateBackpackData(
            string fileName, string nameRu,
            int gridWidth, int gridHeight,
            float weightReduction, float maxWeightBonus, int beltSlots,
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
            data.sizeWidth = 2;
            data.sizeHeight = 2;
            data.weight = weight;
            data.value = value;
            data.hasDurability = false;
            data.volume = weight; // объём рюкзака = его вес
            data.allowNesting = NestingFlag.None; // Рюкзак нельзя вложить в хранилище

            // BackpackData-specific fields
            data.gridWidth = gridWidth;
            data.gridHeight = gridHeight;
            data.weightReduction = weightReduction;
            data.maxWeightBonus = maxWeightBonus;
            data.beltSlots = beltSlots;

            AssetDatabase.CreateAsset(data, assetPath);
            Debug.Log($"[Phase16] Создан {fileName}: grid={gridWidth}×{gridHeight}, " +
                      $"weightReduction={weightReduction}%, bonus={maxWeightBonus}kg, belt={beltSlots}");
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
            data.sizeWidth = 1;
            data.sizeHeight = 1;
            data.weight = weight;
            data.value = value;
            data.hasDurability = false;
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
    }
}
#endif
