// ============================================================================
// MaterialSystem.cs — Система материалов
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Inventory
{
    /// <summary>
    /// Система управления материалами.
    /// 5 тиров материалов с различными свойствами.
    /// </summary>
    public class MaterialSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Material Settings")]
        [Tooltip("База данных материалов")]
        public List<Data.ScriptableObjects.MaterialData> materialDatabase;

        [Header("Tier Multipliers")]
        [Tooltip("Множитель характеристик по тирам")]
        public float[] tierMultipliers = { 1f, 2f, 4f, 8f, 16f };

        [Header("Material Bonuses")]
        [Tooltip("Бонусы по категориям материалов")]
        public MaterialCategoryConfig[] categoryConfigs;

        #endregion

        #region Runtime Data

        // Кэш материалов по ID
        private Dictionary<string, Data.ScriptableObjects.MaterialData> materialCache;

        // Кэш материалов по тиру
        private Dictionary<int, List<Data.ScriptableObjects.MaterialData>> materialsByTier;

        // Кэш материалов по категории
        private Dictionary<MaterialCategory, List<Data.ScriptableObjects.MaterialData>> materialsByCategory;

        #endregion

        #region Events

#pragma warning disable CS0067
        public event Action<Data.ScriptableObjects.MaterialData> OnMaterialDiscovered;
#pragma warning restore CS0067

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeCache();
        }

        #endregion

        #region Initialization

        private void InitializeCache()
        {
            materialCache = new Dictionary<string, Data.ScriptableObjects.MaterialData>();
            materialsByTier = new Dictionary<int, List<Data.ScriptableObjects.MaterialData>>();
            materialsByCategory = new Dictionary<MaterialCategory, List<Data.ScriptableObjects.MaterialData>>();

            // Инициализируем словари для тиров
            for (int i = 1; i <= 5; i++)
            {
                materialsByTier[i] = new List<Data.ScriptableObjects.MaterialData>();
            }

            // Инициализируем словари для категорий
            foreach (MaterialCategory category in Enum.GetValues(typeof(MaterialCategory)))
            {
                materialsByCategory[category] = new List<Data.ScriptableObjects.MaterialData>();
            }

            // Заполняем кэши
            if (materialDatabase != null)
            {
                foreach (var material in materialDatabase)
                {
                    if (material != null)
                    {
                        AddToCache(material);
                    }
                }
            }
        }

        private void AddToCache(Data.ScriptableObjects.MaterialData material)
        {
            // Кэш по ID
            materialCache[material.itemId] = material;

            // Кэш по тиру
            int tier = Mathf.Clamp(material.tier, 1, 5);
            materialsByTier[tier].Add(material);

            // Кэш по категории
            materialsByCategory[material.materialCategory].Add(material);
        }

        #endregion

        #region Query

        /// <summary>
        /// Получает материал по ID
        /// </summary>
        public Data.ScriptableObjects.MaterialData GetMaterial(string materialId)
        {
            materialCache.TryGetValue(materialId, out var material);
            return material;
        }

        /// <summary>
        /// Получает все материалы тира
        /// </summary>
        public List<Data.ScriptableObjects.MaterialData> GetMaterialsByTier(int tier)
        {
            tier = Mathf.Clamp(tier, 1, 5);
            return new List<Data.ScriptableObjects.MaterialData>(materialsByTier[tier]);
        }

        /// <summary>
        /// Получает все материалы категории
        /// </summary>
        public List<Data.ScriptableObjects.MaterialData> GetMaterialsByCategory(MaterialCategory category)
        {
            return new List<Data.ScriptableObjects.MaterialData>(materialsByCategory[category]);
        }

        /// <summary>
        /// Получает материалы по критериям
        /// </summary>
        public List<Data.ScriptableObjects.MaterialData> GetMaterials(MaterialCategory? category = null, int? tier = null)
        {
            var result = new List<Data.ScriptableObjects.MaterialData>();

            foreach (var material in materialCache.Values)
            {
                bool matches = true;

                if (category.HasValue && material.materialCategory != category.Value)
                    matches = false;

                if (tier.HasValue && material.tier != tier.Value)
                    matches = false;

                if (matches)
                    result.Add(material);
            }

            return result;
        }

        /// <summary>
        /// Проверяет существование материала
        /// </summary>
        public bool MaterialExists(string materialId)
        {
            return materialCache.ContainsKey(materialId);
        }

        /// <summary>
        /// Получает количество материалов в базе
        /// </summary>
        public int GetTotalMaterialCount()
        {
            return materialCache.Count;
        }

        #endregion

        #region Material Properties

        /// <summary>
        /// Получает множитель тира
        /// </summary>
        public float GetTierMultiplier(int tier)
        {
            tier = Mathf.Clamp(tier, 1, 5);
            return tierMultipliers[tier - 1];
        }

        /// <summary>
        /// Вычисляет итоговые характеристики материала
        /// </summary>
        public MaterialProperties CalculateProperties(Data.ScriptableObjects.MaterialData material)
        {
            if (material == null)
                return new MaterialProperties();

            float tierMult = GetTierMultiplier(material.tier);

            return new MaterialProperties
            {
                materialId = material.itemId,
                tier = material.tier,
                category = material.materialCategory,

                // Базовые характеристики с множителем тира
                hardness = Mathf.RoundToInt(material.hardness * tierMult),
                durability = Mathf.RoundToInt(material.durability * tierMult),
                conductivity = material.conductivity * tierMult,

                // Бонусы
                damageBonus = material.damageBonus * tierMult,
                defenseBonus = material.defenseBonus * tierMult,
                qiConductivityBonus = material.qiConductivityBonus * tierMult,

                // Экономика
                value = Mathf.RoundToInt(material.value * tierMult),
                weight = material.weight,

                // Редкость
                dropChance = material.dropChance / tierMult,
                requiredLevel = material.requiredLevel
            };
        }

        /// <summary>
        /// Сравнивает два материала
        /// </summary>
        public int CompareMaterials(Data.ScriptableObjects.MaterialData material1, Data.ScriptableObjects.MaterialData material2)
        {
            if (material1 == null && material2 == null) return 0;
            if (material1 == null) return -1;
            if (material2 == null) return 1;

            // Сначала сравниваем тир
            int tierCompare = material1.tier.CompareTo(material2.tier);
            if (tierCompare != 0) return tierCompare;

            // Затем редкость
            int rarityCompare = material1.rarity.CompareTo(material2.rarity);
            if (rarityCompare != 0) return rarityCompare;

            // Затем проводимость Ци
            return material1.conductivity.CompareTo(material2.conductivity);
        }

        #endregion

        #region Material Generation

        /// <summary>
        /// Генерирует случайный материал указанного тира
        /// </summary>
        public Data.ScriptableObjects.MaterialData GenerateRandomMaterial(int tier)
        {
            var materials = GetMaterialsByTier(tier);
            if (materials.Count == 0)
                return null;

            // Взвешенный случайный выбор по редкости
            float totalWeight = 0f;
            var weights = new List<float>();

            foreach (var material in materials)
            {
                // Чем реже, тем меньше вес
                float weight = GetRarityWeight(material.rarity);
                weights.Add(weight);
                totalWeight += weight;
            }

            float random = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;

            for (int i = 0; i < materials.Count; i++)
            {
                cumulative += weights[i];
                if (random <= cumulative)
                {
                    return materials[i];
                }
            }

            return materials[0];
        }

        /// <summary>
        /// Генерирует материал для добычи
        /// </summary>
        public Data.ScriptableObjects.MaterialData GenerateDropMaterial(int playerLevel, float luckModifier = 1f)
        {
            // Определяем тир на основе уровня игрока
            int maxTier = Mathf.Clamp(playerLevel / 2 + 1, 1, 5);
            int minTier = Mathf.Max(1, maxTier - 2);

            // Шанс выпадения с учётом удачи
            float dropChance = UnityEngine.Random.value * luckModifier;

            if (dropChance < 0.1f) // 10% шанс материала максимального тира
            {
                return GenerateRandomMaterial(maxTier);
            }
            else if (dropChance < 0.4f) // 30% шанс материала среднего тира
            {
                int midTier = (maxTier + minTier) / 2;
                return GenerateRandomMaterial(midTier);
            }
            else // 60% шанс материала минимального тира
            {
                return GenerateRandomMaterial(minTier);
            }
        }

        private float GetRarityWeight(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => 50f,
                ItemRarity.Uncommon => 30f,
                ItemRarity.Rare => 15f,
                ItemRarity.Epic => 4f,
                ItemRarity.Legendary => 0.9f,
                ItemRarity.Mythic => 0.1f,
                _ => 1f
            };
        }

        #endregion

        #region Material Compatibility

        /// <summary>
        /// Проверяет совместимость материалов для крафта
        /// </summary>
        public bool AreMaterialsCompatible(Data.ScriptableObjects.MaterialData material1, Data.ScriptableObjects.MaterialData material2)
        {
            if (material1 == null || material2 == null)
                return false;

            // Материалы одного тира всегда совместимы
            if (material1.tier == material2.tier)
                return true;

            // Разница в тирах не более 1
            if (Mathf.Abs(material1.tier - material2.tier) <= 1)
                return true;

            // Некоторые категории совместимы
            if (AreCategoriesCompatible(material1.materialCategory, material2.materialCategory))
                return true;

            return false;
        }

        private bool AreCategoriesCompatible(MaterialCategory cat1, MaterialCategory cat2)
        {
            // Металл + Кристалл = сплавы
            if ((cat1 == MaterialCategory.Metal && cat2 == MaterialCategory.Crystal) ||
                (cat1 == MaterialCategory.Crystal && cat2 == MaterialCategory.Metal))
                return true;

            // Кожа + Ткань = броня
            if ((cat1 == MaterialCategory.Leather && cat2 == MaterialCategory.Cloth) ||
                (cat1 == MaterialCategory.Cloth && cat2 == MaterialCategory.Leather))
                return true;

            // Кость + Дерево = оружие
            if ((cat1 == MaterialCategory.Bone && cat2 == MaterialCategory.Wood) ||
                (cat1 == MaterialCategory.Wood && cat2 == MaterialCategory.Bone))
                return true;

            // Духовные материалы совместимы со всеми
            if (cat1 == MaterialCategory.Spirit || cat2 == MaterialCategory.Spirit)
                return true;

            return false;
        }

        #endregion

        #region Database Management

        /// <summary>
        /// Добавляет материал в базу
        /// </summary>
        public void RegisterMaterial(Data.ScriptableObjects.MaterialData material)
        {
            if (material == null || string.IsNullOrEmpty(material.itemId))
                return;

            if (!materialCache.ContainsKey(material.itemId))
            {
                materialDatabase ??= new List<Data.ScriptableObjects.MaterialData>();
                materialDatabase.Add(material);
                AddToCache(material);
            }
        }

        /// <summary>
        /// Удаляет материал из базы
        /// </summary>
        public bool UnregisterMaterial(string materialId)
        {
            if (!materialCache.TryGetValue(materialId, out var material))
                return false;

            materialCache.Remove(materialId);
            materialsByTier[material.tier].Remove(material);
            materialsByCategory[material.materialCategory].Remove(material);
            materialDatabase?.Remove(material);

            return true;
        }

        /// <summary>
        /// Загружает материалы из списка
        /// </summary>
        public void LoadMaterials(List<Data.ScriptableObjects.MaterialData> materials)
        {
            if (materials == null)
                return;

            foreach (var material in materials)
            {
                RegisterMaterial(material);
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [ContextMenu("Refresh Cache")]
        private void RefreshCache()
        {
            InitializeCache();
            Debug.Log($"MaterialSystem: Cached {materialCache.Count} materials");
        }

        [ContextMenu("List All Materials")]
        private void ListAllMaterials()
        {
            foreach (var kvp in materialCache)
            {
                Debug.Log($"Material: {kvp.Value.nameRu} (Tier {kvp.Value.tier}, {kvp.Value.materialCategory})");
            }
        }
#endif

        #endregion
    }

    // ============================================================================
    // MaterialProperties — Вычисленные свойства материала
    // ============================================================================

    [Serializable]
    public struct MaterialProperties
    {
        public string materialId;
        public int tier;
        public MaterialCategory category;

        // Physical
        public int hardness;
        public int durability;
        public float conductivity;

        // Combat bonuses
        public float damageBonus;
        public float defenseBonus;
        public float qiConductivityBonus;

        // Economy
        public int value;
        public float weight;

        // Drop
        public float dropChance;
        public int requiredLevel;

        /// <summary>
        /// Оценка качества материала
        /// </summary>
        public float QualityScore => (hardness + durability) * 0.5f + conductivity * 10f;

        /// <summary>
        /// Подходит ли для оружия
        /// </summary>
        public bool IsWeaponMaterial => category is MaterialCategory.Metal or MaterialCategory.Bone or MaterialCategory.Crystal;

        /// <summary>
        /// Подходит ли для брони
        /// </summary>
        public bool IsArmorMaterial => category is MaterialCategory.Metal or MaterialCategory.Leather or MaterialCategory.Cloth;
    }

    // ============================================================================
    // MaterialCategoryConfig — Конфигурация категории
    // ============================================================================

    [Serializable]
    public class MaterialCategoryConfig
    {
        public MaterialCategory category;
        public float baseHardness;
        public float baseConductivity;
        public Color displayColor;
        public string[] compatibleCategories;
    }

    // ============================================================================
    // Material Instance — Экземпляр материала в инвентаре
    // ============================================================================

    [Serializable]
    public class MaterialInstance
    {
        public string materialId;
        public int count;
        public float quality; // 0.8 - 1.2 множитель качества
        public int purity; // 0-100%

        public MaterialInstance(string id, int amount, float qual = 1f, int pur = 100)
        {
            materialId = id;
            count = amount;
            quality = Mathf.Clamp(qual, 0.5f, 1.5f);
            purity = Mathf.Clamp(pur, 0, 100);
        }

        /// <summary>
        /// Очищенность материала (влияет на крафт)
        /// </summary>
        public float PurityMultiplier => purity / 100f;

        /// <summary>
        /// Итоговый множитель для крафта
        /// </summary>
        public float CraftingMultiplier => quality * PurityMultiplier;
    }
}
