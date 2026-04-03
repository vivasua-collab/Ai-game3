// ============================================================================
// FormationAssetGenerator.cs — Генератор FormationData и FormationCoreData
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 14:45:00 UTC
// ============================================================================
//
// Меню: Tools → Generate Assets → Formation Assets
// Создаёт FormationData и FormationCoreData assets автоматически
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using CultivationGame.Core;
using CultivationGame.Formation;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Генератор ассетов для системы формаций.
    /// </summary>
    public static class FormationAssetGenerator
    {
        #region Paths

        private const string FORMATIONS_PATH = "Assets/Data/Formations";
        private const string CORES_PATH = "Assets/Data/FormationCores";

        #endregion

        #region Menu Items

        [MenuItem("Tools/Generate Assets/Formation Assets (All)", false, 20)]
        public static void GenerateAllFormationAssets()
        {
            int total = 0;
            int errors = 0;

            total += GenerateFormationData();
            total += GenerateFormationCoreData();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            errors = ValidateFormationAssets();

            if (errors > 0)
            {
                Debug.LogWarning($"[FormationAssetGenerator] Generated {total} assets with {errors} validation errors!");
            }
            else
            {
                Debug.Log($"[FormationAssetGenerator] Generated {total} assets - all valid!");
            }
        }

        [MenuItem("Tools/Generate Assets/Formation Data (24)", false, 21)]
        public static int GenerateFormationData()
        {
            EnsureDirectory(FORMATIONS_PATH);

            int count = 0;

            // Генерируем по 3 формации каждого типа (L1-L3, Small-Medium)
            foreach (FormationType type in System.Enum.GetValues(typeof(FormationType)))
            {
                string typePath = Path.Combine(FORMATIONS_PATH, type.ToString());
                EnsureDirectory(typePath);

                // Малая L1
                count += CreateFormationAsset(type, 1, FormationSize.Small, typePath);
                
                // Средняя L2
                count += CreateFormationAsset(type, 2, FormationSize.Medium, typePath);
                
                // Малая L3
                count += CreateFormationAsset(type, 3, FormationSize.Small, typePath);
            }

            Debug.Log($"[FormationAssetGenerator] Generated {count} FormationData assets");
            return count;
        }

        [MenuItem("Tools/Generate Assets/Formation Core Data (30)", false, 22)]
        public static int GenerateFormationCoreData()
        {
            EnsureDirectory(CORES_PATH);

            int count = 0;

            // Disk: L1-L6
            string diskPath = Path.Combine(CORES_PATH, "Disk");
            EnsureDirectory(diskPath);
            for (int level = 1; level <= 6; level++)
            {
                FormationCoreVariant variant = GetVariantForLevel(level);
                count += CreateCoreAsset(FormationCoreType.Disk, level, variant, diskPath);
            }

            // Altar: L5-L9
            string altarPath = Path.Combine(CORES_PATH, "Altar");
            EnsureDirectory(altarPath);
            for (int level = 5; level <= 9; level++)
            {
                FormationCoreVariant variant = GetVariantForLevel(level);
                count += CreateCoreAsset(FormationCoreType.Altar, level, variant, altarPath);
            }

            // Array: L3, L5, L7
            string arrayPath = Path.Combine(CORES_PATH, "Array");
            EnsureDirectory(arrayPath);
            foreach (int level in new[] { 3, 5, 7 })
            {
                count += CreateCoreAsset(FormationCoreType.Array, level, GetVariantForLevel(level), arrayPath);
            }

            // Totem: L2, L4, L6
            string totemPath = Path.Combine(CORES_PATH, "Totem");
            EnsureDirectory(totemPath);
            foreach (int level in new[] { 2, 4, 6 })
            {
                count += CreateCoreAsset(FormationCoreType.Totem, level, GetVariantForLevel(level), totemPath);
            }

            // Seal: L4, L6, L8
            string sealPath = Path.Combine(CORES_PATH, "Seal");
            EnsureDirectory(sealPath);
            foreach (int level in new[] { 4, 6, 8 })
            {
                count += CreateCoreAsset(FormationCoreType.Seal, level, GetVariantForLevel(level), sealPath);
            }

            Debug.Log($"[FormationAssetGenerator] Generated {count} FormationCoreData assets");
            return count;
        }

        [MenuItem("Tools/Generate Assets/Validate Formation Assets", false, 50)]
        public static int ValidateFormationAssets()
        {
            int totalErrors = 0;

            totalErrors += ValidateFormations();
            totalErrors += ValidateCores();

            if (totalErrors == 0)
            {
                Debug.Log("[FormationAssetGenerator] ✅ All formation assets validated successfully!");
            }
            else
            {
                Debug.LogError($"[FormationAssetGenerator] ❌ Validation failed with {totalErrors} errors!");
            }

            return totalErrors;
        }

        [MenuItem("Tools/Generate Assets/Clear Formation Assets", false, 100)]
        public static void ClearFormationAssets()
        {
            ClearDirectory(FORMATIONS_PATH);
            ClearDirectory(CORES_PATH);

            AssetDatabase.Refresh();
            Debug.Log("[FormationAssetGenerator] Cleared all formation assets");
        }

        #endregion

        #region Formation Data Creation

        private static int CreateFormationAsset(FormationType type, int level, FormationSize size, string path)
        {
            string fileName = $"F_{type}_L{level}_{size}.asset";
            string fullPath = Path.Combine(path, fileName);

            // Проверяем существование
            if (AssetDatabase.LoadAssetAtPath<FormationData>(fullPath) != null)
            {
                return 0;
            }

            var asset = ScriptableObject.CreateInstance<FormationData>();

            // Identity
            asset.formationId = $"{type.ToString().ToLower()}_l{level}_{size.ToString().ToLower()}";
            asset.displayName = GetFormationDisplayName(type, level, size);
            asset.nameEn = $"{type} L{level} {size}";
            asset.description = GetFormationDescription(type, level, size);

            // Classification
            asset.formationType = type;
            asset.size = size;
            asset.level = level;
            asset.element = Element.Neutral;

            // Qi Costs (автоматический расчёт)
            asset.contourQiOverride = 0; // Используем формулу: 80 × 2^(level-1)
            asset.drawTime = 5f + level * 2f;
            asset.cooldown = 60f + level * 30f;

            // Area
            asset.creationRadius = 10f + level * 5f;
            asset.effectRadius = GetBaseEffectRadius(size);
            asset.height = 5f;

            // Duration
            asset.requiresCore = level >= 5;
            asset.isPermanent = false;
            asset.baseDuration = GetBaseDuration(type);

            // Effects
            ConfigureEffects(asset, type, level, size);

            // Requirements
            asset.requirements = new FormationRequirement
            {
                minCultivationLevel = Mathf.Max(1, level - 1),
                minFormationKnowledge = (level - 1) * 10
            };

            // Core compatibility
            asset.minCoreLevel = Mathf.Max(1, level - 2);
            asset.maxCoreLevel = level + 2;

            // Visual
            asset.formationColor = GetFormationColor(type);

            AssetDatabase.CreateAsset(asset, fullPath);
            return 1;
        }

        private static void ConfigureEffects(FormationData asset, FormationType type, int level, FormationSize size)
        {
            float powerMultiplier = Mathf.Pow(1.5f, level - 1);
            
            switch (type)
            {
                case FormationType.Barrier:
                    // Бафф защиты союзникам
                    asset.allyEffects.Add(new FormationEffect
                    {
                        effectType = FormationEffectType.Buff,
                        buffType = BuffType.Defense,
                        value = 10f * powerMultiplier,
                        isPercentage = true
                    });
                    asset.allyEffects.Add(new FormationEffect
                    {
                        effectType = FormationEffectType.Shield,
                        value = 100f * powerMultiplier,
                        isPercentage = false
                    });
                    break;

                case FormationType.Trap:
                    // Урон и замедление врагам
                    asset.enemyEffects.Add(new FormationEffect
                    {
                        effectType = FormationEffectType.Damage,
                        tickValue = Mathf.RoundToInt(20f * powerMultiplier),
                        tickInterval = 2f
                    });
                    asset.enemyEffects.Add(new FormationEffect
                    {
                        effectType = FormationEffectType.Control,
                        controlType = ControlType.Slow,
                        controlDuration = 3f + level * 0.5f
                    });
                    break;

                case FormationType.Amplification:
                    // Бафф урона и скорости союзникам
                    asset.allyEffects.Add(new FormationEffect
                    {
                        effectType = FormationEffectType.Buff,
                        buffType = BuffType.Damage,
                        value = 15f * powerMultiplier,
                        isPercentage = true
                    });
                    asset.allyEffects.Add(new FormationEffect
                    {
                        effectType = FormationEffectType.Buff,
                        buffType = BuffType.Speed,
                        value = 10f * powerMultiplier,
                        isPercentage = true
                    });
                    break;

                case FormationType.Suppression:
                    // Дебафф врагам
                    asset.enemyEffects.Add(new FormationEffect
                    {
                        effectType = FormationEffectType.Debuff,
                        buffType = BuffType.Damage,
                        value = -20f * powerMultiplier,
                        isPercentage = true
                    });
                    asset.enemyEffects.Add(new FormationEffect
                    {
                        effectType = FormationEffectType.Debuff,
                        buffType = BuffType.Speed,
                        value = -15f * powerMultiplier,
                        isPercentage = true
                    });
                    break;

                case FormationType.Gathering:
                    // Исцеление союзников
                    asset.allyEffects.Add(new FormationEffect
                    {
                        effectType = FormationEffectType.Heal,
                        tickValue = Mathf.RoundToInt(10f * powerMultiplier),
                        tickInterval = 5f
                    });
                    break;

                case FormationType.Detection:
                    // Не имеет прямых эффектов — функциональная формация
                    break;

                case FormationType.Teleportation:
                    // Функциональная формация
                    break;

                case FormationType.Summoning:
                    // Призыв существ
                    asset.allyEffects.Add(new FormationEffect
                    {
                        effectType = FormationEffectType.Summon,
                        value = 1 + level / 3
                    });
                    break;
            }

            asset.effectTickInterval = 2f;
            asset.applyOnEnter = true;
            asset.removeOnExit = true;
        }

        #endregion

        #region Core Data Creation

        private static int CreateCoreAsset(FormationCoreType coreType, int level, FormationCoreVariant variant, string path)
        {
            string fileName = $"Core_{coreType}_L{level}_{variant}.asset";
            string fullPath = Path.Combine(path, fileName);

            // Проверяем существование
            if (AssetDatabase.LoadAssetAtPath<FormationCoreData>(fullPath) != null)
            {
                return 0;
            }

            var asset = ScriptableObject.CreateInstance<FormationCoreData>();

            // Basic Info
            asset.coreId = $"{coreType.ToString().ToLower()}_l{level}_{variant.ToString().ToLower()}";
            asset.nameRu = $"{GetVariantNameRu(variant)} {GetCoreTypeNameRu(coreType)} {level}-го уровня";
            asset.nameEn = $"{variant} {coreType} L{level}";
            asset.description = GetCoreDescription(coreType, level, variant);

            // Classification
            asset.coreType = coreType;
            asset.variant = variant;
            asset.rarity = GetRarityForLevel(level);

            // Capacity
            asset.levelMin = GetMinLevelForCore(coreType);
            asset.levelMax = GetMaxLevelForCore(coreType);
            asset.maxSlots = GetMaxSlotsForCore(coreType);
            asset.baseConductivity = GetBaseConductivity(variant, level);
            asset.maxCapacity = GetMaxCapacity(coreType, level, variant);

            // Formation Types
            asset.recommendedType = GetRecommendedType(coreType);

            // Size
            asset.maxSize = GetMaxSizeForCore(coreType);

            // Requirements
            asset.minCultivationLevel = level;
            asset.minConductivity = 0.5f + level * 0.3f;
            asset.minFormationKnowledge = (level - 1) * 10;

            // Durability
            asset.maxDurability = GetMaxDurability(variant);
            asset.durabilityPerActivation = 1;
            asset.durabilityPerHour = coreType == FormationCoreType.Altar ? 1 : 0;

            // Visual
            asset.glowColor = GetCoreGlowColor(variant);

            AssetDatabase.CreateAsset(asset, fullPath);
            return 1;
        }

        #endregion

        #region Validation

        private static int ValidateFormations()
        {
            int errors = 0;
            var guids = AssetDatabase.FindAssets("t:FormationData", new[] { FORMATIONS_PATH });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<FormationData>(path);

                if (asset == null) continue;

                if (string.IsNullOrEmpty(asset.formationId))
                {
                    Debug.LogError($"[Validation] {path}: Missing formationId");
                    errors++;
                }

                if (string.IsNullOrEmpty(asset.displayName))
                {
                    Debug.LogError($"[Validation] {path}: Missing displayName");
                    errors++;
                }

                if (asset.level < 1 || asset.level > 9)
                {
                    Debug.LogWarning($"[Validation] {path}: Invalid level ({asset.level})");
                }

                if (asset.effectRadius <= 0)
                {
                    Debug.LogWarning($"[Validation] {path}: Invalid effectRadius ({asset.effectRadius})");
                }
            }

            return errors;
        }

        private static int ValidateCores()
        {
            int errors = 0;
            var guids = AssetDatabase.FindAssets("t:FormationCoreData", new[] { CORES_PATH });

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<FormationCoreData>(path);

                if (asset == null) continue;

                if (string.IsNullOrEmpty(asset.coreId))
                {
                    Debug.LogError($"[Validation] {path}: Missing coreId");
                    errors++;
                }

                if (asset.levelMin > asset.levelMax)
                {
                    Debug.LogError($"[Validation] {path}: levelMin > levelMax");
                    errors++;
                }

                if (asset.maxCapacity <= 0)
                {
                    Debug.LogWarning($"[Validation] {path}: Invalid maxCapacity ({asset.maxCapacity})");
                }
            }

            return errors;
        }

        #endregion

        #region Helpers

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folder = Path.GetFileName(path);

                if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                {
                    EnsureDirectory(parent);
                }

                AssetDatabase.CreateFolder(parent, folder);
            }
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

        private static FormationCoreVariant GetVariantForLevel(int level)
        {
            return level switch
            {
                1 or 2 => FormationCoreVariant.Stone,
                3 or 4 => FormationCoreVariant.Jade,
                5 or 6 => FormationCoreVariant.SpiritIron,
                7 or 8 => FormationCoreVariant.Crystal,
                9 => FormationCoreVariant.StarMetal,
                _ => FormationCoreVariant.Stone
            };
        }

        private static ItemRarity GetRarityForLevel(int level)
        {
            return level switch
            {
                1 or 2 => ItemRarity.Common,
                3 or 4 => ItemRarity.Uncommon,
                5 or 6 => ItemRarity.Rare,
                7 or 8 => ItemRarity.Epic,
                9 => ItemRarity.Legendary,
                _ => ItemRarity.Common
            };
        }

        private static string GetFormationDisplayName(FormationType type, int level, FormationSize size)
        {
            string typeName = type switch
            {
                FormationType.Barrier => "Барьер",
                FormationType.Trap => "Ловушка",
                FormationType.Amplification => "Усиление",
                FormationType.Suppression => "Подавление",
                FormationType.Gathering => "Сбор",
                FormationType.Detection => "Обнаружение",
                FormationType.Teleportation => "Телепортация",
                FormationType.Summoning => "Призыв",
                _ => type.ToString()
            };

            string sizeName = size switch
            {
                FormationSize.Small => "Малый",
                FormationSize.Medium => "Средний",
                FormationSize.Large => "Большой",
                FormationSize.Great => "Великий",
                FormationSize.Heavy => "Тяжёлый",
                _ => ""
            };

            return $"{sizeName} {typeName.ToLower()} {level}-го уровня";
        }

        private static string GetFormationDescription(FormationType type, int level, FormationSize size)
        {
            return type switch
            {
                FormationType.Barrier => $"Защитный барьер уровня {level}. Поглощает урон и защищает союзников.",
                FormationType.Trap => $"Скрытая ловушка уровня {level}. Наносит урон и замедляет врагов.",
                FormationType.Amplification => $"Формация усиления уровня {level}. Увеличивает боевые характеристики союзников.",
                FormationType.Suppression => $"Формация подавления уровня {level}. Ослабляет врагов в зоне действия.",
                FormationType.Gathering => $"Формация сбора уровня {level}. Ускоряет восстановление сил.",
                FormationType.Detection => $"Формация обнаружения уровня {level}. Раскрывает скрытых врагов.",
                FormationType.Teleportation => $"Телепортационная формация уровня {level}. Позволяет мгновенно перемещаться.",
                FormationType.Summoning => $"Формация призыва уровня {level}. Призывает существ на помощь.",
                _ => $"Формация {type} уровня {level}"
            };
        }

        private static float GetBaseEffectRadius(FormationSize size)
        {
            return size switch
            {
                FormationSize.Small => 20f,
                FormationSize.Medium => 50f,
                FormationSize.Large => 100f,
                FormationSize.Great => 200f,
                FormationSize.Heavy => 500f,
                _ => 20f
            };
        }

        private static float GetBaseDuration(FormationType type)
        {
            return type switch
            {
                FormationType.Barrier => 300f,
                FormationType.Trap => 600f,
                FormationType.Amplification => 180f,
                FormationType.Suppression => 180f,
                FormationType.Gathering => 600f,
                FormationType.Detection => 1200f,
                FormationType.Teleportation => 60f,
                FormationType.Summoning => 120f,
                _ => 300f
            };
        }

        private static Color GetFormationColor(FormationType type)
        {
            return type switch
            {
                FormationType.Barrier => new Color(0.3f, 0.5f, 1f),    // Синий
                FormationType.Trap => new Color(1f, 0.3f, 0.3f),       // Красный
                FormationType.Amplification => new Color(0.3f, 1f, 0.5f), // Зелёный
                FormationType.Suppression => new Color(0.7f, 0.3f, 0.7f), // Фиолетовый
                FormationType.Gathering => new Color(1f, 1f, 0.3f),    // Жёлтый
                FormationType.Detection => new Color(0.5f, 1f, 1f),    // Голубой
                FormationType.Teleportation => new Color(0.7f, 0.5f, 1f), // Лиловый
                FormationType.Summoning => new Color(1f, 0.7f, 0.3f),  // Оранжевый
                _ => Color.cyan
            };
        }

        private static string GetVariantNameRu(FormationCoreVariant variant)
        {
            return variant switch
            {
                FormationCoreVariant.Stone => "Каменный",
                FormationCoreVariant.Jade => "Нефритовый",
                FormationCoreVariant.Iron => "Железный",
                FormationCoreVariant.SpiritIron => "Духожелезный",
                FormationCoreVariant.Crystal => "Кристаллический",
                FormationCoreVariant.StarMetal => "Звёзднометаллический",
                FormationCoreVariant.VoidMatter => "Пустотный",
                _ => variant.ToString()
            };
        }

        private static string GetCoreTypeNameRu(FormationCoreType coreType)
        {
            return coreType switch
            {
                FormationCoreType.Disk => "диск",
                FormationCoreType.Altar => "алтарь",
                FormationCoreType.Array => "массив",
                FormationCoreType.Totem => "тотем",
                FormationCoreType.Seal => "отпечаток",
                _ => coreType.ToString()
            };
        }

        private static string GetCoreDescription(FormationCoreType coreType, int level, FormationCoreVariant variant)
        {
            string portable = coreType == FormationCoreType.Disk || coreType == FormationCoreType.Totem || coreType == FormationCoreType.Seal
                ? "портативное" : "стационарное";
            
            return $"{GetVariantNameRu(variant).ToLower()} {GetCoreTypeNameRu(coreType)} {level}-го уровня. {portable.Capitalize()} ядро для формаций.";
        }

        private static int GetMinLevelForCore(FormationCoreType coreType)
        {
            return coreType switch
            {
                FormationCoreType.Disk => 1,
                FormationCoreType.Totem => 1,
                FormationCoreType.Array => 2,
                FormationCoreType.Seal => 3,
                FormationCoreType.Altar => 4,
                _ => 1
            };
        }

        private static int GetMaxLevelForCore(FormationCoreType coreType)
        {
            return coreType switch
            {
                FormationCoreType.Disk => 6,
                FormationCoreType.Totem => 7,
                FormationCoreType.Array => 8,
                FormationCoreType.Seal => 9,
                FormationCoreType.Altar => 9,
                _ => 9
            };
        }

        private static int GetMaxSlotsForCore(FormationCoreType coreType)
        {
            return coreType switch
            {
                FormationCoreType.Disk => 3,
                FormationCoreType.Totem => 4,
                FormationCoreType.Array => 6,
                FormationCoreType.Seal => 2,
                FormationCoreType.Altar => 12,
                _ => 3
            };
        }

        private static int GetBaseConductivity(FormationCoreVariant variant, int level)
        {
            float variantMult = variant switch
            {
                FormationCoreVariant.Stone => 1f,
                FormationCoreVariant.Jade => 1.5f,
                FormationCoreVariant.Iron => 1.2f,
                FormationCoreVariant.SpiritIron => 2f,
                FormationCoreVariant.Crystal => 3f,
                FormationCoreVariant.StarMetal => 4f,
                FormationCoreVariant.VoidMatter => 5f,
                _ => 1f
            };

            return Mathf.RoundToInt(10 * variantMult * (1 + (level - 1) * 0.2f));
        }

        private static long GetMaxCapacity(FormationCoreType coreType, int level, FormationCoreVariant variant)
        {
            float baseCapacity = coreType switch
            {
                FormationCoreType.Disk => 10000,
                FormationCoreType.Totem => 20000,
                FormationCoreType.Array => 50000,
                FormationCoreType.Seal => 30000,
                FormationCoreType.Altar => 100000,
                _ => 10000
            };

            float variantMult = variant switch
            {
                FormationCoreVariant.Stone => 1f,
                FormationCoreVariant.Jade => 1.2f,
                FormationCoreVariant.Iron => 1.3f,
                FormationCoreVariant.SpiritIron => 1.5f,
                FormationCoreVariant.Crystal => 2f,
                FormationCoreVariant.StarMetal => 2.5f,
                FormationCoreVariant.VoidMatter => 3f,
                _ => 1f
            };

            return (long)(baseCapacity * variantMult * Mathf.Pow(2, level - 1));
        }

        private static FormationType GetRecommendedType(FormationCoreType coreType)
        {
            return coreType switch
            {
                FormationCoreType.Disk => FormationType.Barrier,
                FormationCoreType.Altar => FormationType.Amplification,
                FormationCoreType.Array => FormationType.Suppression,
                FormationCoreType.Totem => FormationType.Summoning,
                FormationCoreType.Seal => FormationType.Trap,
                _ => FormationType.Barrier
            };
        }

        private static FormationSize GetMaxSizeForCore(FormationCoreType coreType)
        {
            return coreType switch
            {
                FormationCoreType.Disk => FormationSize.Medium,
                FormationCoreType.Totem => FormationSize.Medium,
                FormationCoreType.Array => FormationSize.Large,
                FormationCoreType.Seal => FormationSize.Small,
                FormationCoreType.Altar => FormationSize.Heavy,
                _ => FormationSize.Medium
            };
        }

        private static int GetMaxDurability(FormationCoreVariant variant)
        {
            return variant switch
            {
                FormationCoreVariant.Stone => 100,
                FormationCoreVariant.Jade => 80,
                FormationCoreVariant.Iron => 150,
                FormationCoreVariant.SpiritIron => 120,
                FormationCoreVariant.Crystal => 50,
                FormationCoreVariant.StarMetal => 200,
                FormationCoreVariant.VoidMatter => 300,
                _ => 100
            };
        }

        private static Color GetCoreGlowColor(FormationCoreVariant variant)
        {
            return variant switch
            {
                FormationCoreVariant.Stone => new Color(0.5f, 0.5f, 0.5f),
                FormationCoreVariant.Jade => new Color(0.3f, 0.8f, 0.5f),
                FormationCoreVariant.Iron => new Color(0.6f, 0.6f, 0.7f),
                FormationCoreVariant.SpiritIron => new Color(0.5f, 0.7f, 1f),
                FormationCoreVariant.Crystal => new Color(0.8f, 0.9f, 1f),
                FormationCoreVariant.StarMetal => new Color(1f, 0.9f, 0.5f),
                FormationCoreVariant.VoidMatter => new Color(0.5f, 0.2f, 0.8f),
                _ => Color.white
            };
        }

        #endregion
    }

    internal static class StringExtensions
    {
        public static string Capitalize(this string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return char.ToUpper(s[0]) + s.Substring(1);
        }
    }
}
#endif
