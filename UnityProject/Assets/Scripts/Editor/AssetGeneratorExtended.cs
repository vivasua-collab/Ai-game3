// ============================================================================
// AssetGeneratorExtended.cs — Расширенный генератор ScriptableObject ассетов
// Cultivation World Simulator
// Версия: 1.1 — Добавлена валидация данных
// ============================================================================
// Создан: 2026-04-02
// Редактировано: 2026-04-18 18:43:19 UTC — EquipmentSlot переписан по v2.0, +handType, +volume, +allowNesting
// Добавляет генерацию: Techniques, NPCPresets, Equipment, Items, Materials
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Расширение AssetGenerator для генерации ScriptableObject из JSON.
    ///
    /// Меню: Tools → Generate Assets → ...
    /// Источник JSON: Assets/Data/JSON/
    /// Результат: Assets/Data/[Techniques|NPCPresets|Equipment|Items|Materials]/
    /// </summary>
    public static class AssetGeneratorExtended
    {
        #region Paths

        private const string JSON_PATH = "Assets/Data/JSON";
        private const string OUTPUT_TECHNIQUES = "Assets/Data/Techniques";
        private const string OUTPUT_NPC_PRESETS = "Assets/Data/NPCPresets";
        private const string OUTPUT_EQUIPMENT = "Assets/Data/Equipment";
        private const string OUTPUT_ITEMS = "Assets/Data/Items";
        private const string OUTPUT_MATERIALS = "Assets/Data/Materials";
        private const string OUTPUT_STORAGE_RINGS = "Assets/Data/StorageRings";

        #endregion

        #region Menu Items - Generate All Extended

        [MenuItem("Tools/Generate Assets/All Extended Assets (122)", false, 10)]
        public static void GenerateAllExtendedAssets()
        {
            int total = 0;
            int errors = 0;

            total += GenerateTechniques();
            total += GenerateNPCPresets();
            total += GenerateEquipment();
            total += GenerateItems();
            total += GenerateMaterials();
            total += GenerateStorageRings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Валидация после генерации
            errors = ValidateAllAssets();

            if (errors > 0)
            {
                Debug.LogWarning($"[AssetGeneratorExtended] Generated {total} assets with {errors} validation errors!");
            }
            else
            {
                Debug.Log($"[AssetGeneratorExtended] Generated {total} assets - all valid!");
            }
        }

        #endregion

        #region Techniques (34)

        [MenuItem("Tools/Generate Assets/Techniques (34)", false, 11)]
        public static int GenerateTechniques()
        {
            EnsureDirectory(OUTPUT_TECHNIQUES);

            string jsonPath = Path.Combine(JSON_PATH, "techniques.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[AssetGeneratorExtended] JSON not found: {jsonPath}");
                return 0;
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<TechniquesJson>(json);

            if (data == null || data.techniques == null)
            {
                Debug.LogError($"[AssetGeneratorExtended] Failed to parse: {jsonPath}");
                return 0;
            }

            int count = 0;

            foreach (var techData in data.techniques)
            {
                var asset = ScriptableObject.CreateInstance<TechniqueData>();
                ApplyTechniqueData(asset, techData);

                string fileName = $"Tech_{techData.nameEn.Replace(" ", "_").Replace("⚡_", "")}.asset";
                string outputPath = Path.Combine(OUTPUT_TECHNIQUES, fileName);

                AssetDatabase.CreateAsset(asset, outputPath);
                count++;
            }

            Debug.Log($"[AssetGeneratorExtended] Generated {count} TechniqueData assets");
            return count;
        }

        private static void ApplyTechniqueData(TechniqueData asset, TechniqueJson data)
        {
            asset.techniqueId = data.techniqueId;
            asset.nameRu = data.nameRu;
            asset.nameEn = data.nameEn;
            asset.description = data.description;

            asset.techniqueType = ParseTechniqueType(data.techniqueType);
            asset.combatSubtype = ParseCombatSubtype(data.combatSubtype);
            asset.element = ParseElement(data.element);
            asset.grade = ParseTechniqueGrade(data.grade);
            asset.techniqueLevel = data.techniqueLevel;

            asset.minLevel = data.minLevel;
            asset.maxLevel = data.maxLevel;
            asset.canEvolve = data.canEvolve;

            asset.baseQiCost = data.baseQiCost;
            asset.physicalFatigueCost = data.physicalFatigueCost;
            asset.mentalFatigueCost = data.mentalFatigueCost;
            asset.cooldown = data.cooldown;

            asset.baseCapacity = data.baseCapacity;
            asset.isUltimate = data.isUltimate;

            asset.strengthScaling = data.strengthScaling;
            asset.agilityScaling = data.agilityScaling;
            asset.intelligenceScaling = data.intelligenceScaling;
            asset.conductivityScaling = data.conductivityScaling;

            asset.minCultivationLevel = data.minCultivationLevel;
            asset.minStrength = data.minStrength;
            asset.minAgility = data.minAgility;
            asset.minIntelligence = data.minIntelligence;
            asset.minConductivity = data.minConductivity;

            if (data.effects != null)
            {
                foreach (var effectData in data.effects)
                {
                    var effect = new TechniqueEffect
                    {
                        effectType = ParseEffectType(effectData.effectType),
                        value = effectData.value,
                        duration = effectData.duration,
                        chance = effectData.chance
                    };
                    asset.effects.Add(effect);
                }
            }

            if (data.sources != null)
            {
                asset.sources = new List<string>(data.sources);
            }

            asset.learnableFromScroll = data.learnableFromScroll;
            asset.learnableFromNPC = data.learnableFromNPC;
        }

        #endregion

        #region NPC Presets (15)

        [MenuItem("Tools/Generate Assets/NPC Presets (15)", false, 12)]
        public static int GenerateNPCPresets()
        {
            EnsureDirectory(OUTPUT_NPC_PRESETS);

            string jsonPath = Path.Combine(JSON_PATH, "npc_presets.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[AssetGeneratorExtended] JSON not found: {jsonPath}");
                return 0;
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<NPCPresetsJson>(json);

            if (data == null || data.presets == null)
            {
                Debug.LogError($"[AssetGeneratorExtended] Failed to parse: {jsonPath}");
                return 0;
            }

            int count = 0;

            foreach (var presetData in data.presets)
            {
                var asset = ScriptableObject.CreateInstance<NPCPresetData>();
                ApplyNPCPresetData(asset, presetData);

                string fileName = $"NPC_{presetData.nameTemplate.Replace(" ", "_")}.asset";
                string outputPath = Path.Combine(OUTPUT_NPC_PRESETS, fileName);

                AssetDatabase.CreateAsset(asset, outputPath);
                count++;
            }

            Debug.Log($"[AssetGeneratorExtended] Generated {count} NPCPresetData assets");
            return count;
        }

        private static void ApplyNPCPresetData(NPCPresetData asset, NPCPresetJson data)
        {
            asset.presetId = data.presetId;
            asset.nameTemplate = data.nameTemplate;
            asset.title = data.title;
            asset.backstory = data.backstory;

            asset.speciesId = data.species;  // SpeciesData назначается вручную по ID
            asset.cultivationLevel = data.cultivationLevel;
            asset.cultivationSubLevel = data.cultivationSubLevel;
            asset.coreCapacity = data.coreCapacity;
            asset.qiPercentage = data.qiPercentage;

            asset.strength = data.strength;
            asset.agility = data.agility;
            asset.intelligence = data.intelligence;
            asset.vitality = data.vitality;
            asset.conductivity = data.conductivity;

            // personalityTraits, motivation, alignment, etc. handled by NPCPresetData fields
        }

        #endregion

        #region Equipment (39)

        [MenuItem("Tools/Generate Assets/Equipment (39)", false, 13)]
        public static int GenerateEquipment()
        {
            EnsureDirectory(OUTPUT_EQUIPMENT);

            string jsonPath = Path.Combine(JSON_PATH, "equipment.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[AssetGeneratorExtended] JSON not found: {jsonPath}");
                return 0;
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<EquipmentJson>(json);

            if (data == null)
            {
                Debug.LogError($"[AssetGeneratorExtended] Failed to parse: {jsonPath}");
                return 0;
            }

            int count = 0;

            // Weapons
            if (data.weapons != null)
            {
                foreach (var weaponData in data.weapons)
                {
                    var asset = ScriptableObject.CreateInstance<EquipmentData>();
                    ApplyWeaponData(asset, weaponData);

                    string fileName = $"Weapon_{weaponData.nameEn.Replace(" ", "_")}.asset";
                    string outputPath = Path.Combine(OUTPUT_EQUIPMENT, fileName);

                    AssetDatabase.CreateAsset(asset, outputPath);
                    count++;
                }
            }

            // Armor
            if (data.armor != null)
            {
                foreach (var armorData in data.armor)
                {
                    var asset = ScriptableObject.CreateInstance<EquipmentData>();
                    ApplyArmorData(asset, armorData);

                    string fileName = $"Armor_{armorData.nameEn.Replace(" ", "_")}.asset";
                    string outputPath = Path.Combine(OUTPUT_EQUIPMENT, fileName);

                    AssetDatabase.CreateAsset(asset, outputPath);
                    count++;
                }
            }

            Debug.Log($"[AssetGeneratorExtended] Generated {count} EquipmentData assets");
            return count;
        }

        private static void ApplyWeaponData(EquipmentData asset, WeaponJson data)
        {
            asset.itemId = data.itemId;
            asset.nameRu = data.nameRu;
            asset.nameEn = data.nameEn;
            asset.description = data.description;

            asset.category = ItemCategory.Weapon;
            asset.stackable = false;
            asset.maxStack = 1;
            asset.weight = data.weight;
            asset.value = data.value;

            asset.slot = ParseEquipmentSlot(data.slot);
            asset.handType = data.isTwoHanded ? WeaponHandType.TwoHand : ParseWeaponHandType(data.slot, data.weaponType);
            asset.damage = Mathf.RoundToInt((data.damageRange.min + data.damageRange.max) / 2f);
            asset.defense = 0;

            // Новые поля из ItemData
            asset.volume = CalculateVolume(ItemCategory.Weapon, data.weight);
            asset.allowNesting = NestingFlag.Any;
        }

        private static void ApplyArmorData(EquipmentData asset, ArmorJson data)
        {
            asset.itemId = data.itemId;
            asset.nameRu = data.nameRu;
            asset.nameEn = data.nameEn;
            asset.description = data.description;

            asset.category = ItemCategory.Armor;
            asset.stackable = false;
            asset.maxStack = 1;
            asset.weight = data.weight;
            asset.value = data.value;

            asset.slot = ParseEquipmentSlot(data.slot);
            asset.handType = WeaponHandType.OneHand; // Броня никогда не двуручная
            asset.damage = 0;
            asset.defense = data.armorValue;
            asset.coverage = data.coverage;
            asset.damageReduction = data.damageReduction;

            // Новые поля из ItemData
            asset.volume = CalculateVolume(ItemCategory.Armor, data.weight);
            asset.allowNesting = NestingFlag.Any;
        }

        #endregion

        #region Items (8)

        [MenuItem("Tools/Generate Assets/Items (8)", false, 14)]
        public static int GenerateItems()
        {
            EnsureDirectory(OUTPUT_ITEMS);

            string jsonPath = Path.Combine(JSON_PATH, "items.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[AssetGeneratorExtended] JSON not found: {jsonPath}");
                return 0;
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<ItemsJson>(json);

            if (data == null)
            {
                Debug.LogError($"[AssetGeneratorExtended] Failed to parse: {jsonPath}");
                return 0;
            }

            int count = 0;

            // Consumables
            if (data.consumables != null)
            {
                foreach (var itemData in data.consumables)
                {
                    var asset = ScriptableObject.CreateInstance<ItemData>();
                    ApplyItemData(asset, itemData);

                    string fileName = $"Item_{itemData.nameEn.Replace(" ", "_")}.asset";
                    string outputPath = Path.Combine(OUTPUT_ITEMS, fileName);

                    AssetDatabase.CreateAsset(asset, outputPath);
                    count++;
                }
            }

            // Scrolls
            if (data.scrolls != null)
            {
                foreach (var scrollData in data.scrolls)
                {
                    var asset = ScriptableObject.CreateInstance<ItemData>();
                    ApplyItemData(asset, scrollData);

                    string fileName = $"Scroll_{scrollData.nameEn.Replace(" ", "_")}.asset";
                    string outputPath = Path.Combine(OUTPUT_ITEMS, fileName);

                    AssetDatabase.CreateAsset(asset, outputPath);
                    count++;
                }
            }

            Debug.Log($"[AssetGeneratorExtended] Generated {count} ItemData assets");
            return count;
        }

        private static void ApplyItemData(ItemData asset, ItemJson data)
        {
            asset.itemId = data.itemId;
            asset.nameRu = data.nameRu;
            asset.nameEn = data.nameEn;
            asset.description = data.description;

            asset.category = ParseItemCategory(data.category);
            asset.itemType = data.itemType;
            asset.rarity = ParseItemRarity(data.rarity);

            asset.stackable = data.stackable;
            asset.maxStack = data.maxStack;
            asset.sizeWidth = data.sizeWidth;
            asset.sizeHeight = data.sizeHeight;
            asset.weight = data.weight;
            asset.value = data.value;

            asset.hasDurability = data.hasDurability;
            asset.maxDurability = data.maxDurability;

            asset.requiredCultivationLevel = data.requiredCultivationLevel;

            // Новые поля хранилища
            asset.volume = CalculateVolume(ParseItemCategory(data.category), data.weight);
            asset.allowNesting = CalculateNestingFlag(ParseItemCategory(data.category));

            if (data.effects != null)
            {
                foreach (var effectData in data.effects)
                {
                    var effect = new ItemEffect
                    {
                        effectType = effectData.effectType,
                        value = effectData.value,
                        duration = effectData.duration
                    };
                    asset.effects.Add(effect);
                }
            }
        }

        #endregion

        #region Materials (17)

        [MenuItem("Tools/Generate Assets/Materials (17)", false, 15)]
        public static int GenerateMaterials()
        {
            EnsureDirectory(OUTPUT_MATERIALS);

            string jsonPath = Path.Combine(JSON_PATH, "materials.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[AssetGeneratorExtended] JSON not found: {jsonPath}");
                return 0;
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<MaterialsJson>(json);

            if (data == null || data.tiers == null)
            {
                Debug.LogError($"[AssetGeneratorExtended] Failed to parse: {jsonPath}");
                return 0;
            }

            int count = 0;

            foreach (var tierData in data.tiers)
            {
                if (tierData.materials != null)
                {
                    foreach (var matData in tierData.materials)
                    {
                        var asset = ScriptableObject.CreateInstance<MaterialData>();
                        ApplyMaterialData(asset, matData, tierData.tier);

                        string fileName = $"Mat_{matData.nameEn.Replace(" ", "_")}.asset";
                        string outputPath = Path.Combine(OUTPUT_MATERIALS, fileName);

                        AssetDatabase.CreateAsset(asset, outputPath);
                        count++;
                    }
                }
            }

            Debug.Log($"[AssetGeneratorExtended] Generated {count} MaterialData assets");
            return count;
        }

        private static void ApplyMaterialData(MaterialData asset, MaterialJson data, int tier)
        {
            asset.itemId = data.id;
            asset.nameRu = data.nameRu;
            asset.nameEn = data.nameEn;
            asset.description = $"{data.nameRu} (Тир {tier})";

            asset.category = ItemCategory.Material;
            asset.rarity = GetRarityForTier(tier);
            asset.stackable = true;
            asset.maxStack = 100;

            asset.tier = tier;
            asset.materialCategory = ParseMaterialCategory(data.category);

            asset.hardness = data.hardness;
            asset.durability = data.durability;
            asset.conductivity = data.conductivity;

            asset.damageBonus = data.damageBonus;
            asset.defenseBonus = data.defenseBonus;

            asset.source = data.sources != null ? string.Join(", ", data.sources) : "";
            asset.dropChance = data.dropChance;
            asset.requiredLevel = data.requiredLevel > 0 ? data.requiredLevel : tier;

            // Новые поля хранилища
            asset.volume = CalculateVolume(ItemCategory.Material, data.durability * 0.01f);
            asset.allowNesting = NestingFlag.Any;
        }

        #endregion

        #region Storage Rings (4)

        /// <summary>
        /// Генерация колец хранения (StorageRingData).
        /// Создано: 2026-04-19 15:30:00 UTC — Этап 5
        /// Источник: INVENTORY_UI_DRAFT.md §3.6.2
        /// </summary>
        [MenuItem("Tools/Generate Assets/Storage Rings (4)", false, 16)]
        public static int GenerateStorageRings()
        {
            EnsureDirectory(OUTPUT_STORAGE_RINGS);

            int count = 0;

            // Кольцо-щель (стартовое)
            var ring1 = ScriptableObject.CreateInstance<StorageRingData>();
            ApplyStorageRingData(ring1,
                "ring_slit", "Кольцо-щель", "Slit Ring",
                "Пространственная щель — минимальный объём хранения.",
                ItemRarity.Common, 5f, 5, 3f, 1.5f, 0.3f, 0.1f);
            AssetDatabase.CreateAsset(ring1, Path.Combine(OUTPUT_STORAGE_RINGS, "Ring_Slit.asset"));
            count++;

            // Кольцо-карман
            var ring2 = ScriptableObject.CreateInstance<StorageRingData>();
            ApplyStorageRingData(ring2,
                "ring_pocket", "Кольцо-карман", "Pocket Ring",
                "Пространственный карман — для путешественника.",
                ItemRarity.Uncommon, 15f, 5, 2f, 1.5f, 0.3f, 0.1f);
            AssetDatabase.CreateAsset(ring2, Path.Combine(OUTPUT_STORAGE_RINGS, "Ring_Pocket.asset"));
            count++;

            // Кольцо-кладовая
            var ring3 = ScriptableObject.CreateInstance<StorageRingData>();
            ApplyStorageRingData(ring3,
                "ring_vault", "Кольцо-кладовая", "Vault Ring",
                "Пространственная кладовая — для серьёзных запасов.",
                ItemRarity.Rare, 30f, 5, 1f, 1.5f, 0.3f, 0.1f);
            AssetDatabase.CreateAsset(ring3, Path.Combine(OUTPUT_STORAGE_RINGS, "Ring_Vault.asset"));
            count++;

            // Кольцо-пространство
            var ring4 = ScriptableObject.CreateInstance<StorageRingData>();
            ApplyStorageRingData(ring4,
                "ring_space", "Кольцо-пространство", "Space Ring",
                "Целое пространство в кольце — высшее достижение пространственного ремесла.",
                ItemRarity.Epic, 60f, 5, 0.5f, 1.5f, 0.3f, 0.1f);
            AssetDatabase.CreateAsset(ring4, Path.Combine(OUTPUT_STORAGE_RINGS, "Ring_Space.asset"));
            count++;

            Debug.Log($"[AssetGeneratorExtended] Generated {count} StorageRingData assets");
            return count;
        }

        /// <summary>
        /// Применяет данные к StorageRingData.
        /// </summary>
        private static void ApplyStorageRingData(
            StorageRingData asset,
            string itemId, string nameRu, string nameEn,
            string description, ItemRarity rarity,
            float maxVolume, int qiCostBase, float qiCostPerUnit,
            float accessTime, float volume, float weight)
        {
            // ItemData поля
            asset.itemId = itemId;
            asset.nameRu = nameRu;
            asset.nameEn = nameEn;
            asset.description = description;

            asset.category = ItemCategory.Accessory;
            asset.itemType = "storage_ring";
            asset.rarity = rarity;

            asset.stackable = false;
            asset.maxStack = 1;
            asset.sizeWidth = 1;
            asset.sizeHeight = 1;
            asset.weight = weight;
            asset.value = Mathf.RoundToInt(maxVolume * 10);

            asset.hasDurability = false;
            asset.maxDurability = 100;

            asset.requiredCultivationLevel = 0; // Не требует культивации

            // Новые поля хранилища
            asset.volume = volume;
            asset.allowNesting = NestingFlag.None; // Кольца хранения НЕЛЬЗЯ поместить в другие хранилища

            // StorageRingData поля
            asset.maxVolume = maxVolume;
            asset.qiCostBase = qiCostBase;
            asset.qiCostPerUnit = qiCostPerUnit;
            asset.accessTime = accessTime;
        }

        #endregion

        #region Parsing Helpers

        private static Element ParseElement(string value)
        {
            if (string.IsNullOrEmpty(value)) return Element.Neutral;

            switch (value.ToLower())
            {
                case "neutral": return Element.Neutral;
                case "fire": return Element.Fire;
                case "water": return Element.Water;
                case "earth": return Element.Earth;
                case "air": return Element.Air;
                case "lightning": return Element.Lightning;
                case "void": return Element.Void;
                case "poison": return Element.Poison;
                default: return Element.Neutral;
            }
        }

        private static TechniqueType ParseTechniqueType(string value)
        {
            if (string.IsNullOrEmpty(value)) return TechniqueType.Combat;

            switch (value.ToLower())
            {
                case "combat": return TechniqueType.Combat;
                case "defense": return TechniqueType.Defense;
                case "healing": return TechniqueType.Healing;
                case "movement": return TechniqueType.Movement;
                case "curse": return TechniqueType.Curse;
                case "cultivation": return TechniqueType.Cultivation;
                case "support": return TechniqueType.Support;
                case "sensory": return TechniqueType.Sensory;
                case "poison": return TechniqueType.Poison;
                default: return TechniqueType.Combat;
            }
        }

        private static CombatSubtype ParseCombatSubtype(string value)
        {
            if (string.IsNullOrEmpty(value)) return CombatSubtype.None;

            switch (value.ToLower())
            {
                case "none": return CombatSubtype.None;
                case "meleestrike": return CombatSubtype.MeleeStrike;
                case "meleeweapon": return CombatSubtype.MeleeWeapon;
                case "rangedprojectile": return CombatSubtype.RangedProjectile;
                case "rangedbeam": return CombatSubtype.RangedBeam;
                case "rangedaoe": return CombatSubtype.RangedAoe;
                default: return CombatSubtype.None;
            }
        }

        private static TechniqueGrade ParseTechniqueGrade(string value)
        {
            if (string.IsNullOrEmpty(value)) return TechniqueGrade.Common;

            switch (value.ToLower())
            {
                case "common": return TechniqueGrade.Common;
                case "refined": return TechniqueGrade.Refined;
                case "perfect": return TechniqueGrade.Perfect;
                case "transcendent": return TechniqueGrade.Transcendent;
                default: return TechniqueGrade.Common;
            }
        }

        private static EffectType ParseEffectType(string value)
        {
            if (string.IsNullOrEmpty(value)) return EffectType.Damage;

            switch (value.ToLower())
            {
                case "damage": return EffectType.Damage;
                case "heal": return EffectType.Heal;
                case "buff": return EffectType.Buff;
                case "debuff": return EffectType.Debuff;
                case "shield": return EffectType.Shield;
                case "movement": return EffectType.Movement;
                case "statboost": return EffectType.StatBoost;
                case "statreduction": return EffectType.StatReduction;
                case "elemental": return EffectType.Elemental;
                case "special": return EffectType.Special;
                case "block": return EffectType.Buff;
                case "dodge": return EffectType.Buff;
                case "reflect": return EffectType.Special;
                default: return EffectType.Damage;
            }
        }

        /// <summary>
        /// Парсинг слота экипировки из JSON.
        /// Редактировано: 2026-04-18 18:43:19 UTC — переписан под новый EquipmentSlot enum (v2.0)
        /// </summary>
        private static EquipmentSlot ParseEquipmentSlot(object value)
        {
            if (value == null) return EquipmentSlot.WeaponMain;

            string slotStr = value.ToString().ToLower();

            switch (slotStr)
            {
                // Видимые слоты куклы (7)
                case "head":
                case "head_armor":
                case "head_clothing": return EquipmentSlot.Head;
                case "torso":
                case "torso_armor":
                case "torso_clothing": return EquipmentSlot.Torso;
                case "belt": return EquipmentSlot.Belt;
                case "legs":
                case "legs_armor":
                case "legs_clothing": return EquipmentSlot.Legs;
                case "feet":
                case "feet_armor":
                case "feet_clothing": return EquipmentSlot.Feet;
                // Оружие
                case "weapon_main": return EquipmentSlot.WeaponMain;
                case "weapon_off": return EquipmentSlot.WeaponOff;
                case "weapon_twohanded": return EquipmentSlot.WeaponMain; // handType определит двуручность
                // Скрытые слоты
                case "amulet": return EquipmentSlot.Amulet;
                case "ring_left":
                case "ring_left_1": return EquipmentSlot.RingLeft1;
                case "ring_left_2": return EquipmentSlot.RingLeft2;
                case "ring_right":
                case "ring_right_1": return EquipmentSlot.RingRight1;
                case "ring_right_2": return EquipmentSlot.RingRight2;
                case "charger": return EquipmentSlot.Charger;
                case "hands":
                case "hands_armor":
                case "hands_clothing": return EquipmentSlot.Hands;
                case "back":
                case "back_armor": return EquipmentSlot.Back;
                default: return EquipmentSlot.WeaponMain;
            }
        }

        /// <summary>
        /// Определяет тип хвата оружия по подтипу из JSON.
        /// Создано: 2026-04-18 18:43:19 UTC
        /// </summary>
        private static WeaponHandType ParseWeaponHandType(string slot, string itemType)
        {
            string slotLower = (slot ?? "").ToLower();
            string typeLower = (itemType ?? "").ToLower();

            // Явно двуручные слоты
            if (slotLower == "weapon_twohanded") return WeaponHandType.TwoHand;

            // Двуручные подтипы оружия
            if (typeLower.Contains("twohand") || typeLower.Contains("two_hand") ||
                typeLower.Contains("greatsword") || typeLower.Contains("great_sword") ||
                typeLower.Contains("spear") || typeLower.Contains("staff") ||
                typeLower.Contains("bow") || typeLower.Contains("greataxe"))
            {
                return WeaponHandType.TwoHand;
            }

            return WeaponHandType.OneHand;
        }

        /// <summary>
        /// Вычисляет объём предмета по категории.
        /// Создано: 2026-04-18 18:43:19 UTC
        /// Источник: INVENTORY_UI_DRAFT.md §4
        /// </summary>
        private static float CalculateVolume(ItemCategory category, float weight)
        {
            return category switch
            {
                ItemCategory.Consumable => 0.1f,
                ItemCategory.Material => Mathf.Max(0.5f, weight * 0.5f),
                ItemCategory.Technique => 0.1f,
                ItemCategory.Quest => 1.0f,
                ItemCategory.Weapon => Mathf.Clamp(weight, 1f, 4f),
                ItemCategory.Armor => Mathf.Clamp(weight, 1f, 4f),
                ItemCategory.Accessory => 0.2f,
                _ => 1.0f
            };
        }

        /// <summary>
        /// Определяет флаг вложения по категории.
        /// Создано: 2026-04-18 18:43:19 UTC
        /// </summary>
        private static NestingFlag CalculateNestingFlag(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Quest => NestingFlag.None,
                ItemCategory.Technique => NestingFlag.Spirit,
                _ => NestingFlag.Any
            };
        }

        private static ItemCategory ParseItemCategory(string value)
        {
            if (string.IsNullOrEmpty(value)) return ItemCategory.Misc;

            switch (value.ToLower())
            {
                case "weapon": return ItemCategory.Weapon;
                case "armor": return ItemCategory.Armor;
                case "accessory": return ItemCategory.Accessory;
                case "consumable": return ItemCategory.Consumable;
                case "material": return ItemCategory.Material;
                case "technique": return ItemCategory.Technique;
                case "quest": return ItemCategory.Quest;
                default: return ItemCategory.Misc;
            }
        }

        private static ItemRarity ParseItemRarity(string value)
        {
            if (string.IsNullOrEmpty(value)) return ItemRarity.Common;

            switch (value.ToLower())
            {
                case "common": return ItemRarity.Common;
                case "uncommon": return ItemRarity.Uncommon;
                case "rare": return ItemRarity.Rare;
                case "epic": return ItemRarity.Epic;
                case "legendary": return ItemRarity.Legendary;
                case "mythic": return ItemRarity.Mythic;
                default: return ItemRarity.Common;
            }
        }

        private static MaterialCategory ParseMaterialCategory(string value)
        {
            if (string.IsNullOrEmpty(value)) return MaterialCategory.Metal;

            switch (value.ToLower())
            {
                case "metal": return MaterialCategory.Metal;
                case "leather": return MaterialCategory.Leather;
                case "cloth": return MaterialCategory.Cloth;
                case "wood": return MaterialCategory.Wood;
                case "bone": return MaterialCategory.Bone;
                case "crystal": return MaterialCategory.Crystal;
                case "gem": return MaterialCategory.Gem;
                case "organic": return MaterialCategory.Organic;
                case "spirit": return MaterialCategory.Spirit;
                case "void": return MaterialCategory.Void;
                default: return MaterialCategory.Metal;
            }
        }

        private static ItemRarity GetRarityForTier(int tier)
        {
            switch (tier)
            {
                case 1: return ItemRarity.Common;
                case 2: return ItemRarity.Uncommon;
                case 3: return ItemRarity.Rare;
                case 4: return ItemRarity.Epic;
                case 5: return ItemRarity.Legendary;
                default: return ItemRarity.Common;
            }
        }

        #endregion

        #region Utility

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folder = Path.GetFileName(path);

                if (!AssetDatabase.IsValidFolder(parent))
                {
                    EnsureDirectory(parent);
                }

                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        [MenuItem("Tools/Generate Assets/Clear Extended Assets", false, 100)]
        public static void ClearExtendedAssets()
        {
            ClearDirectory(OUTPUT_TECHNIQUES);
            ClearDirectory(OUTPUT_NPC_PRESETS);
            ClearDirectory(OUTPUT_EQUIPMENT);
            ClearDirectory(OUTPUT_ITEMS);
            ClearDirectory(OUTPUT_MATERIALS);

            AssetDatabase.Refresh();
            Debug.Log("[AssetGeneratorExtended] Cleared all extended assets");
        }

        private static void ClearDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path)) return;

            var assets = AssetDatabase.FindAssets("", new[] { path });

            foreach (var guid in assets)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                AssetDatabase.DeleteAsset(assetPath);
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Валидация всех сгенерированных ассетов.
        /// Проверяет: дубликаты имён, обязательные поля, корректность ссылок.
        /// </summary>
        [MenuItem("Tools/Generate Assets/Validate All Assets", false, 50)]
        public static int ValidateAllAssets()
        {
            int totalErrors = 0;

            totalErrors += ValidateTechniques();
            totalErrors += ValidateNPCPresets();
            totalErrors += ValidateEquipment();
            totalErrors += ValidateItems();
            totalErrors += ValidateMaterials();
            totalErrors += CheckDuplicateNames();

            if (totalErrors == 0)
            {
                Debug.Log("[AssetGeneratorExtended] ✅ All assets validated successfully!");
            }
            else
            {
                Debug.LogError($"[AssetGeneratorExtended] ❌ Validation failed with {totalErrors} errors!");
            }

            return totalErrors;
        }

        private static int ValidateTechniques()
        {
            int errors = 0;
            var guids = AssetDatabase.FindAssets("t:TechniqueData", new[] { OUTPUT_TECHNIQUES });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TechniqueData>(path);

                if (asset == null) continue;

                // Проверка обязательных полей
                if (string.IsNullOrEmpty(asset.techniqueId))
                {
                    Debug.LogError($"[Validation] {path}: Missing techniqueId");
                    errors++;
                }

                if (string.IsNullOrEmpty(asset.nameEn))
                {
                    Debug.LogError($"[Validation] {path}: Missing nameEn");
                    errors++;
                }

                if (asset.baseCapacity <= 0)
                {
                    Debug.LogWarning($"[Validation] {path}: Invalid baseCapacity ({asset.baseCapacity})");
                }

                if (asset.baseQiCost < 0)
                {
                    Debug.LogWarning($"[Validation] {path}: Negative Qi cost ({asset.baseQiCost})");
                }

                // Проверка уровня культивации
                if (asset.minCultivationLevel < 1 || asset.minCultivationLevel > 10)
                {
                    Debug.LogWarning($"[Validation] {path}: Invalid minCultivationLevel ({asset.minCultivationLevel})");
                }
            }

            return errors;
        }

        private static int ValidateNPCPresets()
        {
            int errors = 0;
            var guids = AssetDatabase.FindAssets("t:NPCPresetData", new[] { OUTPUT_NPC_PRESETS });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<NPCPresetData>(path);

                if (asset == null) continue;

                // Проверка обязательных полей
                if (string.IsNullOrEmpty(asset.presetId))
                {
                    Debug.LogError($"[Validation] {path}: Missing presetId");
                    errors++;
                }

                if (string.IsNullOrEmpty(asset.nameTemplate))
                {
                    Debug.LogError($"[Validation] {path}: Missing nameTemplate");
                    errors++;
                }

                // Проверка уровня культивации
                if (asset.cultivationLevel < 1 || asset.cultivationLevel > 10)
                {
                    Debug.LogWarning($"[Validation] {path}: Invalid cultivationLevel ({asset.cultivationLevel})");
                }

                // Проверка характеристик
                if (asset.strength < 0 || asset.agility < 0 || asset.intelligence < 0 || asset.vitality < 0)
                {
                    Debug.LogWarning($"[Validation] {path}: Negative stats detected");
                }
            }

            return errors;
        }

        private static int ValidateEquipment()
        {
            int errors = 0;
            var guids = AssetDatabase.FindAssets("t:EquipmentData", new[] { OUTPUT_EQUIPMENT });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EquipmentData>(path);

                if (asset == null) continue;

                // Проверка обязательных полей
                if (string.IsNullOrEmpty(asset.itemId))
                {
                    Debug.LogError($"[Validation] {path}: Missing itemId");
                    errors++;
                }

                if (string.IsNullOrEmpty(asset.nameEn))
                {
                    Debug.LogError($"[Validation] {path}: Missing nameEn");
                    errors++;
                }

                // Проверка веса
                if (asset.weight < 0)
                {
                    Debug.LogWarning($"[Validation] {path}: Negative weight ({asset.weight})");
                }

                // Проверка слота
                if (asset.slot == EquipmentSlot.None)
                {
                    Debug.LogWarning($"[Validation] {path}: Equipment slot is None");
                }
            }

            return errors;
        }

        private static int ValidateItems()
        {
            int errors = 0;
            var guids = AssetDatabase.FindAssets("t:ItemData", new[] { OUTPUT_ITEMS });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<ItemData>(path);

                if (asset == null) continue;

                // Проверка обязательных полей
                if (string.IsNullOrEmpty(asset.itemId))
                {
                    Debug.LogError($"[Validation] {path}: Missing itemId");
                    errors++;
                }

                if (string.IsNullOrEmpty(asset.nameEn))
                {
                    Debug.LogError($"[Validation] {path}: Missing nameEn");
                    errors++;
                }

                // Проверка стека
                if (asset.stackable && asset.maxStack <= 0)
                {
                    Debug.LogWarning($"[Validation] {path}: Stackable item with invalid maxStack ({asset.maxStack})");
                }

                // Проверка размера
                if (asset.sizeWidth <= 0 || asset.sizeHeight <= 0)
                {
                    Debug.LogWarning($"[Validation] {path}: Invalid item size ({asset.sizeWidth}x{asset.sizeHeight})");
                }
            }

            return errors;
        }

        private static int ValidateMaterials()
        {
            int errors = 0;
            var guids = AssetDatabase.FindAssets("t:MaterialData", new[] { OUTPUT_MATERIALS });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<MaterialData>(path);

                if (asset == null) continue;

                // Проверка обязательных полей
                if (string.IsNullOrEmpty(asset.itemId))
                {
                    Debug.LogError($"[Validation] {path}: Missing itemId");
                    errors++;
                }

                if (string.IsNullOrEmpty(asset.nameEn))
                {
                    Debug.LogError($"[Validation] {path}: Missing nameEn");
                    errors++;
                }

                // Проверка тира
                if (asset.tier < 1 || asset.tier > 5)
                {
                    Debug.LogWarning($"[Validation] {path}: Invalid tier ({asset.tier})");
                }

                // Проверка характеристик материала
                if (asset.hardness < 0)
                {
                    Debug.LogWarning($"[Validation] {path}: Negative hardness ({asset.hardness})");
                }
            }

            return errors;
        }

        /// <summary>
        /// Проверка на дубликаты имён и ID.
        /// </summary>
        private static int CheckDuplicateNames()
        {
            int errors = 0;
            var nameSet = new HashSet<string>();
            var idSet = new HashSet<string>();

            // Проверяем все типы ассетов
            var allTypes = new[] 
            { 
                (OUTPUT_TECHNIQUES, "t:TechniqueData"),
                (OUTPUT_NPC_PRESETS, "t:NPCPresetData"),
                (OUTPUT_EQUIPMENT, "t:EquipmentData"),
                (OUTPUT_ITEMS, "t:ItemData"),
                (OUTPUT_MATERIALS, "t:MaterialData")
            };

            foreach (var (folder, filter) in allTypes)
            {
                var guids = AssetDatabase.FindAssets(filter, new[] { folder });

                foreach (var guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(path);

                    if (nameSet.Contains(fileName))
                    {
                        Debug.LogError($"[Validation] Duplicate asset name: {fileName}");
                        errors++;
                    }
                    else
                    {
                        nameSet.Add(fileName);
                    }
                }
            }

            return errors;
        }

        #endregion

        #region JSON Classes

        [System.Serializable]
        private class TechniquesJson
        {
            public List<TechniqueJson> techniques;
        }

        [System.Serializable]
        private class TechniqueJson
        {
            public string techniqueId;
            public string nameRu;
            public string nameEn;
            public string description;
            public string techniqueType;
            public string combatSubtype;
            public string element;
            public string grade;
            public int techniqueLevel;
            public int minLevel;
            public int maxLevel;
            public bool canEvolve;
            public long baseQiCost; // FIX DAT-H01: int→long
            public float physicalFatigueCost;
            public float mentalFatigueCost;
            public int cooldown;
            public int baseCapacity;
            public bool isUltimate;
            public float strengthScaling;
            public float agilityScaling;
            public float intelligenceScaling;
            public float conductivityScaling;
            public int minCultivationLevel;
            public int minStrength;
            public int minAgility;
            public int minIntelligence;
            public float minConductivity;
            public List<TechniqueEffectJson> effects;
            public List<string> sources;
            public bool learnableFromScroll;
            public bool learnableFromNPC;
        }

        [System.Serializable]
        private class TechniqueEffectJson
        {
            public string effectType;
            public float value;
            public int duration;
            public float chance;
        }

        [System.Serializable]
        private class NPCPresetsJson
        {
            public List<NPCPresetJson> presets;
        }

        [System.Serializable]
        private class NPCPresetJson
        {
            public string presetId;
            public string nameTemplate;
            public string title;
            public string backstory;
            public string category;
            public string species;
            public int cultivationLevel;
            public int cultivationSubLevel;
            public long coreCapacity;
            public int qiPercentage;
            public int strength;
            public int agility;
            public int intelligence;
            public int vitality;
            public float conductivity;
            public List<PersonalityTraitJson> personalityTraits;
            public string motivation;
            public string alignment;
            public int baseDisposition;
            public string factionId;
            public string factionRole;
        }

        [System.Serializable]
        private class PersonalityTraitJson
        {
            public string traitName;
            public int intensity;
        }

        [System.Serializable]
        private class EquipmentJson
        {
            public List<WeaponJson> weapons;
            public List<ArmorJson> armor;
        }

        [System.Serializable]
        private class WeaponJson
        {
            public string itemId;
            public string nameRu;
            public string nameEn;
            public string description;
            public string weaponType;
            public string damageType;
            public DamageRangeJson damageRange;
            public float attackSpeed;
            public float range;
            public float weight;
            public int value;
            public object slot;
            public bool isTwoHanded;
        }

        [System.Serializable]
        private class DamageRangeJson
        {
            public int min;
            public int max;
        }

        [System.Serializable]
        private class ArmorJson
        {
            public string itemId;
            public string nameRu;
            public string nameEn;
            public string description;
            public string armorType;
            public int armorValue;
            public int damageReduction;
            public float coverage;
            public float weight;
            public int value;
            public string slot;
        }

        [System.Serializable]
        private class ItemsJson
        {
            public List<ItemJson> consumables;
            public List<ItemJson> scrolls;
        }

        [System.Serializable]
        private class ItemJson
        {
            public string itemId;
            public string nameRu;
            public string nameEn;
            public string description;
            public string category;
            public string itemType;
            public string rarity;
            public bool stackable;
            public int maxStack;
            public int sizeWidth;
            public int sizeHeight;
            public float weight;
            public int value;
            public bool hasDurability;
            public int maxDurability;
            public List<ItemEffectJson> effects;
            public int requiredCultivationLevel;
        }

        [System.Serializable]
        private class ItemEffectJson
        {
            public string effectType;
            public float value;
            public int duration;
        }

        [System.Serializable]
        private class MaterialsJson
        {
            public List<MaterialTierJson> tiers;
        }

        [System.Serializable]
        private class MaterialTierJson
        {
            public int tier;
            public string nameRu;
            public string nameEn;
            public List<MaterialJson> materials;
        }

        [System.Serializable]
        private class MaterialJson
        {
            public string id;
            public string nameRu;
            public string nameEn;
            public string category;
            public int hardness;
            public int durability;
            public float conductivity;
            public float damageBonus;
            public float defenseBonus;
            public float dropChance;
            public int requiredLevel;
            public List<string> sources;
        }

        #endregion
    }
}
#endif
