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
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;
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
        //  Test Equipment Set — базовый набор шмота для проверки куклы
        // ====================================================================
        // Редактировано: 2026-04-25 19:00:00 MSK

        private const string TEST_EQUIP_FOLDER = "Assets/Data/Equipment/TestSet";

        /// <summary>
        /// Создаёт базовый набор экипировки для тестирования инвентаря и куклы.
        /// По одному предмету на каждый видимый слот (7 шт.) + 2 расходника + 2 материала.
        /// Каждый предмет получает процедурно сгенерированную иконку (цветной квадрат с буквой).
        /// </summary>
        private void AddTestEquipmentSet()
        {
            SceneBuilderUtils.EnsureDirectory(TEST_EQUIP_FOLDER);

            // Проверяем — если хоть один тестовый предмет существует, пропускаем
            if (AssetDatabase.LoadAssetAtPath<EquipmentData>($"{TEST_EQUIP_FOLDER}/Test_IronHelmet.asset") != null)
            {
                Debug.Log("[Phase16] Test Equipment Set уже существует — пропускаем");
                return;
            }

            // === ОДЕЖДА (левая колонка куклы) ===

            // Иконки теперь используют GradeColors (Д9, Д10, Д11)
            CreateTestEquipment("Test_IronHelmet", "Железный шлем", "Простой железный шлем.",
                EquipmentSlot.Head, WeaponHandType.OneHand,
                damage: 0, defense: 12, coverage: 75f, damageReduction: 8f, dodgeBonus: -5f,
                weight: 2.5f, value: 60,
                ItemRarity.Common, EquipmentGrade.Common, "iron", 1,
                iconLetter: "Ш");

            CreateTestEquipment("Test_ClothRobe", "Тканевая роба", "Простая роба из ткани.",
                EquipmentSlot.Torso, WeaponHandType.OneHand,
                damage: 0, defense: 3, coverage: 60f, damageReduction: 0f, dodgeBonus: 0f,
                weight: 0.5f, value: 20,
                ItemRarity.Common, EquipmentGrade.Common, "cloth", 1,
                iconLetter: "Р");

            CreateTestEquipment("Test_LeatherBelt", "Кожаный ремень", "Прочный ремень из кожи.",
                EquipmentSlot.Belt, WeaponHandType.OneHand,
                damage: 0, defense: 2, coverage: 30f, damageReduction: 0f, dodgeBonus: 0f,
                weight: 0.3f, value: 15,
                ItemRarity.Common, EquipmentGrade.Common, "leather", 1,
                iconLetter: "П");

            CreateTestEquipment("Test_ClothPants", "Тканевые штаны", "Простые тканевые штаны.",
                EquipmentSlot.Legs, WeaponHandType.OneHand,
                damage: 0, defense: 2, coverage: 50f, damageReduction: 0f, dodgeBonus: 0f,
                weight: 0.4f, value: 15,
                ItemRarity.Common, EquipmentGrade.Common, "cloth", 1,
                iconLetter: "Н");

            CreateTestEquipment("Test_ClothShoes", "Тканевые туфли", "Лёгкие тканевые туфли.",
                EquipmentSlot.Feet, WeaponHandType.OneHand,
                damage: 0, defense: 1, coverage: 30f, damageReduction: 0f, dodgeBonus: 0f,
                weight: 0.2f, value: 10,
                ItemRarity.Common, EquipmentGrade.Common, "cloth", 1,
                iconLetter: "О");

            // === ОРУЖИЕ (правая колонка куклы) ===

            CreateTestEquipment("Test_IronSword", "Железный меч", "Надёжный железный меч.",
                EquipmentSlot.WeaponMain, WeaponHandType.OneHand,
                damage: 12, defense: 0, coverage: 0f, damageReduction: 0f, dodgeBonus: 0f,
                weight: 2.5f, value: 50,
                ItemRarity.Common, EquipmentGrade.Common, "iron", 1,
                iconLetter: "М");

            CreateTestEquipment("Test_IronDagger", "Железный кинжал", "Лёгкий железный кинжал.",
                EquipmentSlot.WeaponOff, WeaponHandType.OneHand,
                damage: 6, defense: 0, coverage: 0f, damageReduction: 0f, dodgeBonus: 0f,
                weight: 0.5f, value: 35,
                ItemRarity.Common, EquipmentGrade.Common, "iron", 1,
                iconLetter: "К");

            // === Двуручное (для теста блокировки WeaponOff) ===

            CreateTestEquipment("Test_IronGreatsword", "Железный двуручник", "Тяжёлый двуручный меч.",
                EquipmentSlot.WeaponMain, WeaponHandType.TwoHand,
                damage: 24, defense: 0, coverage: 0f, damageReduction: 0f, dodgeBonus: -5f,
                weight: 6.0f, value: 120,
                ItemRarity.Uncommon, EquipmentGrade.Common, "iron", 1,
                iconLetter: "Д");

            // === Редкие предметы для разнообразия ===

            CreateTestEquipment("Test_SpiritRobe", "Духовная роба", "Роба из духовного шёлка.",
                EquipmentSlot.Torso, WeaponHandType.OneHand,
                damage: 0, defense: 15, coverage: 75f, damageReduction: 8f, dodgeBonus: 0f,
                weight: 0.4f, value: 650,
                ItemRarity.Rare, EquipmentGrade.Refined, "spirit_silk", 3,
                iconLetter: "Д");

            CreateTestEquipment("Test_SteelSword", "Стальной меч", "Качественный стальной меч.",
                EquipmentSlot.WeaponMain, WeaponHandType.OneHand,
                damage: 18, defense: 0, coverage: 0f, damageReduction: 0f, dodgeBonus: 0f,
                weight: 2.3f, value: 180,
                ItemRarity.Uncommon, EquipmentGrade.Refined, "steel", 2,
                iconLetter: "С");

            Debug.Log("[Phase16] ✅ Test Equipment Set создан: 10 предметов (7 слотов + 2 редких + 1 двуручник)");
        }

        /// <summary>
        /// Создаёт один тестовый предмет экипировки с процедурной иконкой.
        /// Иконка использует GradeColors для фона (Д9) и Tier-индикатор (Д10, Д11).
        /// </summary>
        private void CreateTestEquipment(
            string fileName, string nameRu, string description,
            EquipmentSlot slot, WeaponHandType handType,
            int damage, int defense, float coverage, float damageReduction, float dodgeBonus,
            float weight, int value,
            ItemRarity rarity, EquipmentGrade grade,
            string materialId, int materialTier,
            string iconLetter)
        {
            string assetPath = $"{TEST_EQUIP_FOLDER}/{fileName}.asset";

            var data = ScriptableObject.CreateInstance<EquipmentData>();
            data.itemId = fileName;
            data.nameRu = nameRu;
            data.nameEn = fileName.Replace("Test_", "");
            data.description = description;
            data.category = (slot == EquipmentSlot.WeaponMain || slot == EquipmentSlot.WeaponOff)
                ? ItemCategory.Weapon : ItemCategory.Armor;
            data.itemType = slot.ToString();
            data.rarity = rarity;
            data.stackable = false;
            data.maxStack = 1;
            data.weight = weight;
            data.value = value;
            data.hasDurability = true;
            data.maxDurability = 100;
            // Объём по формуле строчной модели
            data.volume = Mathf.Clamp(weight, 1f, 4f);
            data.allowNesting = NestingFlag.Any;
            // Иконка через GradeColors (Д9, Д10, Д11)
            Color iconBg = GradeColors.GetIconBgColor(grade, materialTier);
            Color iconBorder = GetRarityBorderColor(rarity);
            Color tierIndicator = GradeColors.GetTierColor(materialTier);
            data.icon = GenerateTestIcon(fileName, iconBg, iconBorder, tierIndicator, iconLetter);

            // EquipmentData fields
            data.slot = slot;
            data.handType = handType;
            data.damage = damage;
            data.defense = defense;
            data.coverage = coverage;
            data.damageReduction = damageReduction;
            data.dodgeBonus = dodgeBonus;
            data.materialId = materialId;
            data.materialTier = materialTier;
            data.grade = grade;
            data.itemLevel = 1;

            AssetDatabase.CreateAsset(data, assetPath);
        }

        /// <summary>
        /// Генерирует тестовую иконку предмета — цветной квадрат с буквой.
        /// Размер 32×32px. Фон = GradeColors (Д9, Д11), рамка = Rarity, Tier-индикатор 4×4 (Д10).
        /// </summary>
        private Sprite GenerateTestIcon(string fileName, Color bgColor, Color borderColor, Color tierColor, string letter)
        {
            const string iconDir = "Assets/Sprites/UI/ItemIcons";
            string iconPath = $"{iconDir}/{fileName}.png";

            SceneBuilderUtils.EnsureDirectory(iconDir);

            // Если уже существует — загружаем
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (existing != null) return existing;

            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];

            // Фон — Grade-цвет (затемнённый по Tier через GradeColors.GetIconBgColor)
            Color32 bg = bgColor;
            Color32 border = borderColor;
            Color32 letterColor = new Color32(255, 255, 255, 220);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Рамка 2px — Rarity-цвет
                    if (x < 2 || x >= size - 2 || y < 2 || y >= size - 2)
                        pixels[y * size + x] = border;
                    else
                        pixels[y * size + x] = bg;
                }
            }

            // Tier-индикатор 4×4 в правом нижнем углу (Д10)
            for (int y = 2; y < 6; y++)
                for (int x = size - 6; x < size - 2; x++)
                    pixels[y * size + x] = tierColor;

            // Буква — простая 5×7 пиксельная сетка
            DrawPixelLetter(pixels, size, letter, 13, 12, letterColor);

            tex.SetPixels32(pixels);
            tex.Apply();

            var png = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(iconPath, png);
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(iconPath);
            var importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 32;
                importer.filterMode = FilterMode.Point;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        }

        /// <summary>Цвет рамки по редкости</summary>
        private Color32 GetRarityBorderColor(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return new Color32(107, 114, 128, 255);    // Серый
                case ItemRarity.Uncommon: return new Color32(34, 197, 94, 255);     // Зелёный
                case ItemRarity.Rare: return new Color32(59, 130, 246, 255);        // Синий
                case ItemRarity.Epic: return new Color32(168, 85, 247, 255);        // Фиолетовый
                case ItemRarity.Legendary: return new Color32(251, 191, 36, 255);   // Золотой
                case ItemRarity.Mythic: return new Color32(239, 68, 68, 255);       // Красный
                default: return new Color32(107, 114, 128, 255);
            }
        }

        /// <summary>Рисует пиксельную букву 5×7 в массив</summary>
        private void DrawPixelLetter(Color32[] pixels, int texSize, string letter, int ox, int oy, Color32 color)
        {
            // Упрощённая пиксельная сетка для русских букв
            bool[,] grid = GetLetterGrid(letter);
            if (grid == null) return;

            for (int y = 0; y < 7; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    if (!grid[y, x]) continue;
                    int px = ox + x;
                    int py = oy + (6 - y); // Инверсия Y
                    if (px >= 0 && px < texSize && py >= 0 && py < texSize)
                        pixels[py * texSize + px] = color;
                }
            }
        }

        /// <summary>Пиксельная сетка 5×7 для русских букв</summary>
        private bool[,] GetLetterGrid(string letter)
        {
            switch (letter)
            {
                // Ш — шлем
                case "Ш": return new bool[,] {
                    { true, false, true, false, true },
                    { true, false, true, false, true },
                    { true, false, true, false, true },
                    { true, false, true, false, true },
                    { true, true, true, true, true },
                    { false, false, false, false, false },
                    { false, false, false, false, false } };
                // Р — роба
                case "Р": return new bool[,] {
                    { true, true, true, false, false },
                    { true, false, false, true, false },
                    { true, false, false, true, false },
                    { true, true, true, false, false },
                    { true, false, false, false, false },
                    { true, false, false, false, false },
                    { true, false, false, false, false } };
                // П — пояс
                case "П": return new bool[,] {
                    { true, true, true, true, true },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { false, false, false, false, false },
                    { false, false, false, false, false } };
                // Н — ноги
                case "Н": return new bool[,] {
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { true, true, true, true, true },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { false, false, false, false, false },
                    { false, false, false, false, false } };
                // О — обувь
                case "О": return new bool[,] {
                    { false, true, true, true, false },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { false, true, true, true, false },
                    { false, false, false, false, false },
                    { false, false, false, false, false } };
                // М — меч
                case "М": return new bool[,] {
                    { true, false, false, false, true },
                    { true, true, false, true, true },
                    { true, false, true, false, true },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { false, false, false, false, false },
                    { false, false, false, false, false } };
                // К — кинжал
                case "К": return new bool[,] {
                    { true, false, false, true, false },
                    { true, false, true, false, false },
                    { true, true, false, false, false },
                    { true, false, true, false, false },
                    { true, false, false, true, false },
                    { false, false, false, false, false },
                    { false, false, false, false, false } };
                // Д — двуручник
                case "Д": return new bool[,] {
                    { false, true, true, true, false },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { true, true, true, true, true },
                    { true, false, false, false, false },
                    { false, true, true, true, false } };
                // С — стальной
                case "С": return new bool[,] {
                    { false, true, true, true, false },
                    { true, false, false, false, false },
                    { true, false, false, false, false },
                    { false, false, false, false, true },
                    { false, true, true, true, false },
                    { false, false, false, false, false },
                    { false, false, false, false, false } };
                default: return new bool[,] {
                    { true, true, true, true, true },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { true, false, false, false, true },
                    { true, true, true, true, true },
                    { false, false, false, false, false },
                    { false, false, false, false, false } };
            }
        }
    }
}
#endif
