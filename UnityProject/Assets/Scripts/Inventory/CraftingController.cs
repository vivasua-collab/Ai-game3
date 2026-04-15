// ============================================================================
// CraftingController.cs — Система крафта
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:08:52 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Save;

namespace CultivationGame.Inventory
{
    /// <summary>
    /// Система крафта предметов.
    /// Создание оружия, брони, пилюль и артефактов.
    /// </summary>
    public class CraftingController : MonoBehaviour
    {
        #region Configuration

        [Header("Crafting Settings")]
        [Tooltip("Рецепты крафта")]
        public List<CraftingRecipe> recipes;

        [Tooltip("Бонус качества от навыка")]
        public float skillQualityBonus = 0.1f;

        [Tooltip("Шанс критического крафта")]
        public float criticalCraftChance = 0.05f;

        [Header("References")]
        [Tooltip("Инвентарь игрока")]
        public InventoryController playerInventory;

        [Tooltip("Система материалов")]
        public MaterialSystem materialSystem;

        #endregion

        #region Runtime Data

        // Кэш рецептов по ID результата
        private Dictionary<string, List<CraftingRecipe>> recipesByResult;

        // Кэш рецептов по категории
        private Dictionary<ItemCategory, List<CraftingRecipe>> recipesByCategory;

        // Текущий навык крафта
        private Dictionary<CraftingType, int> craftingSkills = new Dictionary<CraftingType, int>();

        // FIX INV-H02: Real XP system for crafting (2026-04-11)
        private Dictionary<CraftingType, int> craftingExperience = new Dictionary<CraftingType, int>();

        #endregion

        #region Events

        public event Action<CraftingResult> OnCraftSuccess;
        public event Action<CraftingResult> OnCraftFailure;
        public event Action<CraftingRecipe> OnRecipeLearned;
#pragma warning disable CS0067
        public event Action<float> OnCraftProgress;
#pragma warning restore CS0067

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeRecipeCache();
            InitializeSkills();
        }

        #endregion

        #region Initialization

        private void InitializeRecipeCache()
        {
            recipesByResult = new Dictionary<string, List<CraftingRecipe>>();
            recipesByCategory = new Dictionary<ItemCategory, List<CraftingRecipe>>();

            foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
            {
                recipesByCategory[category] = new List<CraftingRecipe>();
            }

            if (recipes != null)
            {
                foreach (var recipe in recipes)
                {
                    AddRecipeToCache(recipe);
                }
            }
        }

        private void AddRecipeToCache(CraftingRecipe recipe)
        {
            if (recipe == null || recipe.resultItem == null)
                return;

            string resultId = recipe.resultItem.itemId;

            if (!recipesByResult.ContainsKey(resultId))
            {
                recipesByResult[resultId] = new List<CraftingRecipe>();
            }

            recipesByResult[resultId].Add(recipe);
            recipesByCategory[recipe.resultItem.category].Add(recipe);
        }

        private void InitializeSkills()
        {
            foreach (CraftingType type in Enum.GetValues(typeof(CraftingType)))
            {
                craftingSkills[type] = 1;
            }
        }

        #endregion

        #region Crafting

        /// <summary>
        /// Создаёт предмет по рецепту
        /// </summary>
        public CraftingResult Craft(CraftingRecipe recipe, int count = 1)
        {
            var result = new CraftingResult
            {
                recipe = recipe,
                success = false,
                itemsCreated = 0,
                quality = 0f
            };

            if (recipe == null)
            {
                result.failReason = "Invalid recipe";
                OnCraftFailure?.Invoke(result);
                return result;
            }

            // Проверяем требования
            var validation = ValidateRecipe(recipe, count);
            if (!validation.canCraft)
            {
                result.failReason = validation.failReason;
                OnCraftFailure?.Invoke(result);
                return result;
            }

            // Проверяем навык
            int skillLevel = GetCraftingSkill(recipe.craftingType);
            if (skillLevel < recipe.requiredSkillLevel)
            {
                result.failReason = $"Insufficient skill level ({skillLevel}/{recipe.requiredSkillLevel})";
                OnCraftFailure?.Invoke(result);
                return result;
            }

            // Расходуем материалы
            foreach (var ingredient in recipe.ingredients)
            {
                playerInventory.RemoveItemById(ingredient.materialId, ingredient.amount * count);
            }

            // Вычисляем качество
            float baseQuality = CalculateBaseQuality(recipe, skillLevel);
            bool isCritical = UnityEngine.Random.value < criticalCraftChance;

            if (isCritical)
            {
                baseQuality *= 1.5f;
                result.isCritical = true;
            }

            // Успех или провал
            float successChance = CalculateSuccessChance(recipe, skillLevel);
            result.success = UnityEngine.Random.value < successChance;

            if (result.success)
            {
                // Определяем грейд результата
                EquipmentGrade grade = DetermineGrade(baseQuality);

                // Создаём предметы
                int itemsToCreate = count * recipe.resultAmount;
                result.itemsCreated = itemsToCreate;
                result.quality = baseQuality;
                result.grade = grade;

                // Добавляем в инвентарь
                for (int i = 0; i < itemsToCreate; i++)
                {
                    var slot = playerInventory.AddItem(recipe.resultItem, 1);
                    // FIX INV-H01: Применить грейд к созданному предмету (2026-04-11)
                    if (slot != null)
                    {
                        slot.grade = grade;
                    }
                }

                // Увеличиваем навык
                AddSkillExperience(recipe.craftingType, recipe.experienceGain * count);

                OnCraftSuccess?.Invoke(result);
            }
            else
            {
                result.failReason = "Crafting failed";
                AddSkillExperience(recipe.craftingType, recipe.experienceGain * count / 2);
                OnCraftFailure?.Invoke(result);
            }

            return result;
        }

        /// <summary>
        /// Создаёт предмет с указанными материалами (custom crafting)
        /// </summary>
        public CraftingResult CraftCustom(
            Data.ScriptableObjects.EquipmentData baseItem,
            List<MaterialInstance> materials,
            CraftingType craftingType)
        {
            var result = new CraftingResult
            {
                success = false,
                itemsCreated = 0
            };

            if (baseItem == null || materials == null || materials.Count == 0)
            {
                result.failReason = "Invalid parameters";
                OnCraftFailure?.Invoke(result);
                return result;
            }

            // Проверяем наличие материалов
            foreach (var mat in materials)
            {
                if (!playerInventory.HasItem(mat.materialId, mat.count))
                {
                    result.failReason = $"Missing material: {mat.materialId}";
                    OnCraftFailure?.Invoke(result);
                    return result;
                }
            }

            // Вычисляем качество на основе материалов
            float materialQuality = CalculateMaterialQuality(materials);
            int skillLevel = GetCraftingSkill(craftingType);

            float finalQuality = materialQuality * (1f + skillLevel * skillQualityBonus);

            // Расходуем материалы
            foreach (var mat in materials)
            {
                playerInventory.RemoveItemById(mat.materialId, mat.count);
            }

            // Создаём предмет
            result.success = true;
            result.itemsCreated = 1;
            result.quality = finalQuality;
            result.grade = DetermineGrade(finalQuality);

            var slot = playerInventory.AddItem(baseItem, 1);
            // FIX INV-H01: Применить грейд к созданному предмету (2026-04-11)
            if (slot != null)
            {
                slot.grade = result.grade;
            }

            OnCraftSuccess?.Invoke(result);
            return result;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Проверяет возможность крафта
        /// </summary>
        public RecipeValidation ValidateRecipe(CraftingRecipe recipe, int count = 1)
        {
            var validation = new RecipeValidation
            {
                canCraft = true,
                failReason = ""
            };

            if (recipe == null)
            {
                validation.canCraft = false;
                validation.failReason = "No recipe";
                return validation;
            }

            // Проверяем материалы
            validation.missingMaterials = new List<MaterialRequirement>();

            foreach (var ingredient in recipe.ingredients)
            {
                int have = playerInventory.CountItem(ingredient.materialId);
                int need = ingredient.amount * count;

                if (have < need)
                {
                    validation.canCraft = false;
                    validation.missingMaterials.Add(new MaterialRequirement
                    {
                        materialId = ingredient.materialId,
                        amount = need - have
                    });
                }
            }

            if (!validation.canCraft)
            {
                validation.failReason = "Missing materials";
                return validation;
            }

            // Проверяем место в инвентаре
            int freeSlots = playerInventory.FreeSlots;
            if (freeSlots < recipe.resultItem.sizeWidth * recipe.resultItem.sizeHeight)
            {
                validation.canCraft = false;
                validation.failReason = "Inventory full";
                return validation;
            }

            // Проверяем навык
            int skillLevel = GetCraftingSkill(recipe.craftingType);
            if (skillLevel < recipe.requiredSkillLevel)
            {
                validation.canCraft = false;
                validation.failReason = "Skill too low";
                return validation;
            }

            return validation;
        }

        /// <summary>
        /// Проверяет, можно ли создать предмет
        /// </summary>
        public bool CanCraft(CraftingRecipe recipe, int count = 1)
        {
            return ValidateRecipe(recipe, count).canCraft;
        }

        #endregion

        #region Calculations

        private float CalculateBaseQuality(CraftingRecipe recipe, int skillLevel)
        {
            // Базовое качество от рецепта
            float quality = recipe.baseQuality;

            // Бонус от навыка
            quality += (skillLevel - recipe.requiredSkillLevel) * skillQualityBonus;

            // Шанс случайного бонуса
            if (UnityEngine.Random.value < 0.1f)
            {
                quality += 0.1f;
            }

            return Mathf.Clamp01(quality);
        }

        private float CalculateSuccessChance(CraftingRecipe recipe, int skillLevel)
        {
            // Базовый шанс
            float chance = recipe.baseSuccessChance;

            // Бонус от навыка
            int skillDiff = skillLevel - recipe.requiredSkillLevel;
            chance += skillDiff * 0.05f;

            // Кап на успех
            return Mathf.Clamp(chance, 0.1f, 0.99f);
        }

        private float CalculateMaterialQuality(List<MaterialInstance> materials)
        {
            if (materials == null || materials.Count == 0)
                return 0.5f;

            float totalQuality = 0f;
            float totalWeight = 0f;

            foreach (var mat in materials)
            {
                var matData = materialSystem?.GetMaterial(mat.materialId);
                if (matData == null) continue;

                float tierWeight = matData.tier;
                totalQuality += mat.CraftingMultiplier * tierWeight;
                totalWeight += tierWeight;
            }

            return totalWeight > 0 ? totalQuality / totalWeight : 0.5f;
        }

        private EquipmentGrade DetermineGrade(float quality)
        {
            if (quality >= 0.95f) return EquipmentGrade.Transcendent;
            if (quality >= 0.8f) return EquipmentGrade.Perfect;
            if (quality >= 0.6f) return EquipmentGrade.Refined;
            if (quality >= 0.4f) return EquipmentGrade.Common;
            return EquipmentGrade.Damaged;
        }

        #endregion

        #region Skills

        /// <summary>
        /// Получает уровень навыка крафта
        /// </summary>
        public int GetCraftingSkill(CraftingType type)
        {
            return craftingSkills.TryGetValue(type, out int level) ? level : 1;
        }

        /// <summary>
        /// Добавляет опыт к навыку.
        /// FIX INV-H02: Реальная XP система вместо random 20% level-up (2026-04-11)
        /// XP threshold per level = level * 100
        /// </summary>
        public void AddSkillExperience(CraftingType type, int experience)
        {
            if (!craftingExperience.ContainsKey(type))
                craftingExperience[type] = 0;

            craftingExperience[type] += experience;

            int currentLevel = GetCraftingSkill(type);
            int expNeeded = currentLevel * 100;

            while (craftingExperience[type] >= expNeeded && currentLevel < 10)
            {
                craftingExperience[type] -= expNeeded;
                currentLevel++;
                craftingSkills[type] = currentLevel;
                expNeeded = currentLevel * 100;
            }
        }

        /// <summary>
        /// Получает текущий опыт навыка крафта.
        /// FIX INV-H02: Добавлено для доступа к XP (2026-04-11)
        /// </summary>
        public int GetCraftingExperience(CraftingType type)
        {
            return craftingExperience.TryGetValue(type, out int exp) ? exp : 0;
        }

        /// <summary>
        /// Устанавливает уровень навыка
        /// </summary>
        public void SetCraftingSkill(CraftingType type, int level)
        {
            craftingSkills[type] = Mathf.Clamp(level, 1, 10);
        }

        #endregion

        #region Recipes

        /// <summary>
        /// Получает рецепты по категории результата
        /// </summary>
        public List<CraftingRecipe> GetRecipesByCategory(ItemCategory category)
        {
            return recipesByCategory.TryGetValue(category, out var list)
                ? new List<CraftingRecipe>(list)
                : new List<CraftingRecipe>();
        }

        /// <summary>
        /// Получает рецепты для создания указанного предмета
        /// </summary>
        public List<CraftingRecipe> GetRecipesForItem(string itemId)
        {
            return recipesByResult.TryGetValue(itemId, out var list)
                ? new List<CraftingRecipe>(list)
                : new List<CraftingRecipe>();
        }

        /// <summary>
        /// Получает все доступные рецепты
        /// </summary>
        public List<CraftingRecipe> GetAvailableRecipes()
        {
            var available = new List<CraftingRecipe>();

            foreach (var recipe in recipes)
            {
                if (CanCraft(recipe))
                {
                    available.Add(recipe);
                }
            }

            return available;
        }

        /// <summary>
        /// Добавляет новый рецепт
        /// </summary>
        public void LearnRecipe(CraftingRecipe recipe)
        {
            if (recipe == null || recipes.Contains(recipe))
                return;

            recipes.Add(recipe);
            AddRecipeToCache(recipe);
            OnRecipeLearned?.Invoke(recipe);
        }

        #endregion

        #region Save/Load

        public CraftingSaveData GetSaveData()
        {
            // FIX SAV-H01: Use serializable CraftingSkillEntry array instead of Dictionary (2026-04-11)
            var skillEntries = new List<CraftingSkillEntry>();
            foreach (var kvp in craftingSkills)
            {
                skillEntries.Add(new CraftingSkillEntry((int)kvp.Key, kvp.Value));
            }
            
            return new CraftingSaveData
            {
                skills = skillEntries.ToArray(),
                knownRecipeIds = recipes.ConvertAll(r => r.recipeId)
            };
        }

        public void LoadSaveData(CraftingSaveData data)
        {
            // FIX SAV-H01: Deserialize from CraftingSkillEntry array (2026-04-11)
            if (data?.skills != null)
            {
                foreach (var entry in data.skills)
                {
                    CraftingType type = (CraftingType)entry.craftingType;
                    craftingSkills[type] = entry.level;
                }
            }
        }

        #endregion
    }

    // ============================================================================
    // CraftingRecipe — Рецепт крафта
    // ============================================================================

    [CreateAssetMenu(fileName = "CraftingRecipe", menuName = "Cultivation/Crafting Recipe")]
    [Serializable]
    public class CraftingRecipe : ScriptableObject
    {
        [Header("Recipe Info")]
        public string recipeId;
        public string recipeName;
        [TextArea(2, 4)]
        public string description;

        [Header("Result")]
        public Data.ScriptableObjects.ItemData resultItem;
        public int resultAmount = 1;

        [Header("Ingredients")]
        public List<CraftingIngredient> ingredients;

        [Header("Requirements")]
        public CraftingType craftingType = CraftingType.General;
        public int requiredSkillLevel = 1;

        [Header("Stats")]
        [Range(0f, 1f)]
        public float baseQuality = 0.5f;
        [Range(0.1f, 1f)]
        public float baseSuccessChance = 0.8f;
        public int experienceGain = 10;

        [Header("Time")]
        public float craftTime = 1f;

        [Header("Unlock")]
        public bool isDefault = false;
        public string unlockCondition;
    }

    // ============================================================================
    // CraftingIngredient — Ингредиент рецепта
    // ============================================================================

    [Serializable]
    public class CraftingIngredient
    {
        public string materialId;
        public int amount = 1;
        public bool isOptional = false;

        // Альтернативные материалы
        public List<string> alternativeIds;
    }

    // ============================================================================
    // CraftingResult — Результат крафта
    // ============================================================================

    [Serializable]
    public class CraftingResult
    {
        public CraftingRecipe recipe;
        public bool success;
        public int itemsCreated;
        public float quality;
        public EquipmentGrade grade;
        public bool isCritical;
        public string failReason;

        public int TotalValue => itemsCreated * (recipe?.resultItem?.value ?? 0);
    }

    // ============================================================================
    // RecipeValidation — Результат проверки рецепта
    // ============================================================================

    [Serializable]
    public class RecipeValidation
    {
        public bool canCraft;
        public string failReason;
        public List<MaterialRequirement> missingMaterials;
    }

    // ============================================================================
    // MaterialRequirement — Требование материала
    // ============================================================================

    [Serializable]
    public class MaterialRequirement
    {
        public string materialId;
        public int amount;
    }

    // ============================================================================
    // CraftingType — Тип крафта
    // ============================================================================

    public enum CraftingType
    {
        General,        // Общий
        Smithing,       // Кузнечное дело
        Alchemy,        // Алхимия
        Tailoring,      // Портняжное дело
        Inscription,    // Начертание
        Cooking,        // Кулинария
        Engineering,    // Инженерия
        ArtifactCrafting // Создание артефактов
    }

    // ============================================================================
    // Save Data
    // ============================================================================

    [Serializable]
    public class CraftingSaveData
    {
        // FIX SAV-H01: Use serializable array instead of Dictionary (2026-04-11)
        public CraftingSkillEntry[] skills;
        public List<string> knownRecipeIds;
    }
}
