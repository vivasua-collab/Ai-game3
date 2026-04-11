// ============================================================================
// AssetGenerator.cs — Генератор ScriptableObject ассетов из JSON
// Cultivation World Simulator
// Версия: 1.1
// ============================================================================
// Создан: 2026-04-01
// Редактирован: 2025-04-01 — Добавлена генерация MortalStageData
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
    /// Генератор ScriptableObject ассетов из JSON файлов.
    ///
    /// Меню: Tools → Generate Assets → ...
    /// Источник JSON: Assets/Data/JSON/
    /// Результат: Assets/Data/[CultivationLevels|Elements|MortalStages]/
    /// </summary>
    public static class AssetGenerator
    {
        #region Paths

        private const string JSON_PATH = "Assets/Data/JSON";
        private const string OUTPUT_CULTIVATION = "Assets/Data/CultivationLevels";
        private const string OUTPUT_ELEMENTS = "Assets/Data/Elements";
        private const string OUTPUT_MORTAL = "Assets/Data/MortalStages";

        #endregion

        #region Menu Items

        [MenuItem("Tools/Generate Assets/All Assets from JSON", false, 0)]
        public static void GenerateAllAssets()
        {
            int total = 0;

            total += GenerateCultivationLevels();
            total += GenerateElements();
            total += GenerateMortalStages();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[AssetGenerator] Generated {total} assets total");
        }

        [MenuItem("Tools/Generate Assets/Cultivation Levels (10)", false, 1)]
        public static int GenerateCultivationLevels()
        {
            EnsureDirectory(OUTPUT_CULTIVATION);

            string jsonPath = Path.Combine(JSON_PATH, "cultivation_levels.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[AssetGenerator] JSON not found: {jsonPath}");
                return 0;
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<CultivationLevelsJson>(json);

            if (data == null || data.levels == null)
            {
                Debug.LogError($"[AssetGenerator] Failed to parse: {jsonPath}");
                return 0;
            }

            int count = 0;

            foreach (var levelData in data.levels)
            {
                var asset = ScriptableObject.CreateInstance<CultivationLevelData>();
                ApplyLevelData(asset, levelData);

                string fileName = $"Level{levelData.level}_{levelData.nameEn.Replace(" ", "")}.asset";
                string outputPath = Path.Combine(OUTPUT_CULTIVATION, fileName);

                AssetDatabase.CreateAsset(asset, outputPath);
                count++;
            }

            Debug.Log($"[AssetGenerator] Generated {count} CultivationLevelData assets");
            return count;
        }

        [MenuItem("Tools/Generate Assets/Elements (7)", false, 2)]
        public static int GenerateElements()
        {
            EnsureDirectory(OUTPUT_ELEMENTS);

            string jsonPath = Path.Combine(JSON_PATH, "elements.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogError($"[AssetGenerator] JSON not found: {jsonPath}");
                return 0;
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<ElementsJson>(json);

            if (data == null || data.elements == null)
            {
                Debug.LogError($"[AssetGenerator] Failed to parse: {jsonPath}");
                return 0;
            }

            int count = 0;

            foreach (var elementData in data.elements)
            {
                var asset = ScriptableObject.CreateInstance<ElementData>();
                ApplyElementData(asset, elementData);

                string fileName = $"Element_{elementData.nameEn}.asset";
                string outputPath = Path.Combine(OUTPUT_ELEMENTS, fileName);

                AssetDatabase.CreateAsset(asset, outputPath);
                count++;
            }

            Debug.Log($"[AssetGenerator] Generated {count} ElementData assets");
            return count;
        }

        [MenuItem("Tools/Generate Assets/Mortal Stages (6)", false, 3)]
        public static int GenerateMortalStages()
        {
            EnsureDirectory(OUTPUT_MORTAL);

            string jsonPath = Path.Combine(JSON_PATH, "mortal_stages.json");

            if (!File.Exists(jsonPath))
            {
                Debug.LogWarning($"[AssetGenerator] JSON not found: {jsonPath}. Generating from defaults...");
                return GenerateMortalStagesFromDefaults();
            }

            string json = File.ReadAllText(jsonPath);
            var data = JsonUtility.FromJson<MortalStagesJson>(json);

            if (data == null || data.stages == null)
            {
                Debug.LogError($"[AssetGenerator] Failed to parse: {jsonPath}");
                return 0;
            }

            int count = 0;

            foreach (var stageData in data.stages)
            {
                var asset = ScriptableObject.CreateInstance<MortalStageData>();
                ApplyMortalStageData(asset, stageData);

                string fileName = $"Stage{stageData.stage}_{stageData.nameEn.Replace(" ", "")}.asset";
                string outputPath = Path.Combine(OUTPUT_MORTAL, fileName);

                AssetDatabase.CreateAsset(asset, outputPath);
                count++;
            }

            Debug.Log($"[AssetGenerator] Generated {count} MortalStageData assets");
            return count;
        }

        /// <summary>
        /// Генерирует MortalStageData из значений по умолчанию (без JSON)
        /// </summary>
        private static int GenerateMortalStagesFromDefaults()
        {
            var defaultStages = new List<MortalStageJson>
            {
                new MortalStageJson
                {
                    stage = "Newborn",
                    nameRu = "Новорождённый",
                    nameEn = "Newborn",
                    description = "Только что рождённый ребёнок. Ядро только начинает формироваться.",
                    minAge = 0, maxAge = 7,
                    minCoreFormation = 0f, maxCoreFormation = 30f, coreFormationRate = 4f,
                    minQiCapacity = 1, maxQiCapacity = 30, qiAbsorptionRate = 0.01f,
                    canRegenerateQi = false,
                    minStrength = 0.1f, maxStrength = 2f,
                    minAgility = 0.1f, maxAgility = 2f,
                    minConstitution = 0.1f, maxConstitution = 2f,
                    baseAwakeningChance = 0f, highDensityAwakeningChance = 0f, criticalAwakeningChance = 0f,
                    canAwaken = false,
                    requiresFood = true, requiresWater = true, requiresSleep = true,
                    canLearnMartialArts = false, canMeditate = false,
                    abilitiesDescription = "Полная зависимость от родителей. Естественное поглощение Ци из среды.",
                    nextStage = "Child",
                    isAwakeningPoint = false
                },
                new MortalStageJson
                {
                    stage = "Child",
                    nameRu = "Ребёнок",
                    nameEn = "Child",
                    description = "Ребёнок в возрасте 7-16 лет. Ядро формируется активнее.",
                    minAge = 7, maxAge = 16,
                    minCoreFormation = 30f, maxCoreFormation = 60f, coreFormationRate = 3f,
                    minQiCapacity = 30, maxQiCapacity = 100, qiAbsorptionRate = 0.02f,
                    canRegenerateQi = false,
                    minStrength = 2f, maxStrength = 8f,
                    minAgility = 2f, maxAgility = 8f,
                    minConstitution = 2f, maxConstitution = 8f,
                    baseAwakeningChance = 0.001f, highDensityAwakeningChance = 0.01f, criticalAwakeningChance = 0f,
                    canAwaken = false,
                    requiresFood = true, requiresWater = true, requiresSleep = true,
                    canLearnMartialArts = true, canMeditate = false,
                    abilitiesDescription = "Может начать изучать боевые искусства. Начало осознания Ци.",
                    nextStage = "Adult",
                    isAwakeningPoint = false
                },
                new MortalStageJson
                {
                    stage = "Adult",
                    nameRu = "Взрослый",
                    nameEn = "Adult",
                    description = "Взрослый человек 16-30 лет. Пик физической формы и формирования ядра.",
                    minAge = 16, maxAge = 30,
                    minCoreFormation = 60f, maxCoreFormation = 90f, coreFormationRate = 2f,
                    minQiCapacity = 100, maxQiCapacity = 200, qiAbsorptionRate = 0.03f,
                    canRegenerateQi = false,
                    minStrength = 8f, maxStrength = 15f,
                    minAgility = 8f, maxAgility = 15f,
                    minConstitution = 8f, maxConstitution = 15f,
                    baseAwakeningChance = 0.01f, highDensityAwakeningChance = 0.1f, criticalAwakeningChance = 0.01f,
                    canAwaken = true,
                    requiresFood = true, requiresWater = true, requiresSleep = true,
                    canLearnMartialArts = true, canMeditate = true,
                    abilitiesDescription = "Оптимальный возраст для пробуждения. Может медитировать для ускорения формирования ядра.",
                    nextStage = "Mature",
                    isAwakeningPoint = false
                },
                new MortalStageJson
                {
                    stage = "Mature",
                    nameRu = "Зрелый",
                    nameEn = "Mature",
                    description = "Зрелый человек 30-50 лет. Ядро полностью сформировано, но начинает угасать.",
                    minAge = 30, maxAge = 50,
                    minCoreFormation = 90f, maxCoreFormation = 100f, coreFormationRate = 1f,
                    minQiCapacity = 80, maxQiCapacity = 150, qiAbsorptionRate = 0.02f,
                    canRegenerateQi = false,
                    minStrength = 10f, maxStrength = 15f,
                    minAgility = 8f, maxAgility = 12f,
                    minConstitution = 10f, maxConstitution = 15f,
                    baseAwakeningChance = 0.05f, highDensityAwakeningChance = 0.3f, criticalAwakeningChance = 0.1f,
                    canAwaken = true,
                    requiresFood = true, requiresWater = true, requiresSleep = true,
                    canLearnMartialArts = true, canMeditate = true,
                    abilitiesDescription = "Последний оптимальный этап для пробуждения. После 50 лет шанс резко падает.",
                    nextStage = "Elder",
                    isAwakeningPoint = false
                },
                new MortalStageJson
                {
                    stage = "Elder",
                    nameRu = "Старец",
                    nameEn = "Elder",
                    description = "Пожилой человек 50+ лет. Ядро угасает, шанс пробуждения минимален.",
                    minAge = 50, maxAge = 100,
                    minCoreFormation = 30f, maxCoreFormation = 80f, coreFormationRate = -1f,
                    minQiCapacity = 30, maxQiCapacity = 80, qiAbsorptionRate = 0.01f,
                    canRegenerateQi = false,
                    minStrength = 5f, maxStrength = 10f,
                    minAgility = 3f, maxAgility = 8f,
                    minConstitution = 5f, maxConstitution = 10f,
                    baseAwakeningChance = 0.001f, highDensityAwakeningChance = 0.01f, criticalAwakeningChance = 0.005f,
                    canAwaken = true,
                    requiresFood = true, requiresWater = true, requiresSleep = true,
                    canLearnMartialArts = false, canMeditate = true,
                    abilitiesDescription = "Ядро угасает. Пробуждение возможно, но с высокими рисками.",
                    nextStage = "None",
                    isAwakeningPoint = false
                },
                new MortalStageJson
                {
                    stage = "Awakening",
                    nameRu = "Пробуждение",
                    nameEn = "Awakening",
                    description = "Точка перехода от смертного к практику. Пробуждение дремлющего ядра.",
                    minAge = 16, maxAge = 50,
                    minCoreFormation = 80f, maxCoreFormation = 100f, coreFormationRate = 0f,
                    minQiCapacity = 100, maxQiCapacity = 200, qiAbsorptionRate = 0.05f,
                    canRegenerateQi = true,
                    minStrength = 10f, maxStrength = 20f,
                    minAgility = 10f, maxAgility = 20f,
                    minConstitution = 10f, maxConstitution = 20f,
                    baseAwakeningChance = 1f, highDensityAwakeningChance = 5f, criticalAwakeningChance = 1f,
                    canAwaken = true,
                    requiresFood = true, requiresWater = true, requiresSleep = true,
                    canLearnMartialArts = true, canMeditate = true,
                    abilitiesDescription = "Точка пробуждения. После успешного пробуждения становится практиком 1-го уровня.",
                    nextStage = "None",
                    isAwakeningPoint = true
                }
            };

            int count = 0;

            foreach (var stageData in defaultStages)
            {
                var asset = ScriptableObject.CreateInstance<MortalStageData>();
                ApplyMortalStageData(asset, stageData);

                string fileName = $"Stage_{stageData.nameEn}.asset";
                string outputPath = Path.Combine(OUTPUT_MORTAL, fileName);

                AssetDatabase.CreateAsset(asset, outputPath);
                count++;
            }

            Debug.Log($"[AssetGenerator] Generated {count} MortalStageData assets from defaults");
            return count;
        }

        [MenuItem("Tools/Generate Assets/Clear All Generated", false, 100)]
        public static void ClearAllGenerated()
        {
            ClearDirectory(OUTPUT_CULTIVATION);
            ClearDirectory(OUTPUT_ELEMENTS);
            ClearDirectory(OUTPUT_MORTAL);

            AssetDatabase.Refresh();
            Debug.Log("[AssetGenerator] Cleared all generated assets");
        }

        #endregion

        #region Apply Data

        private static void ApplyLevelData(CultivationLevelData asset, CultivationLevelJson data)
        {
            // Basic Info
            asset.level = data.level;
            asset.nameRu = data.nameRu;
            asset.nameEn = data.nameEn;
            asset.description = data.description;

            // Qi Parameters
            asset.qiDensity = data.qiDensity;
            asset.coreCapacityMultiplier = data.coreCapacityMultiplier;
            asset.baseCoreCapacity = data.baseCoreCapacity;

            // Body Effects
            asset.agingMultiplier = data.agingMultiplier;
            asset.conductivityMultiplier = data.conductivityMultiplier;

            // Abilities
            asset.abilitiesDescription = data.abilitiesDescription;
            asset.noFoodRequired = data.noFoodRequired;
            asset.noWaterRequired = data.noWaterRequired;
            asset.canFly = data.canFly;
            asset.canRegenerateLimbs = data.canRegenerateLimbs;

            // Breakthrough
            asset.useDynamicBreakthroughCalculation = data.useDynamicBreakthroughCalculation;
            asset.subLevelMultiplier = data.subLevelMultiplier;
            asset.levelMultiplier = data.levelMultiplier;
            asset.qiForSubLevelBreakthrough = data.qiForSubLevelBreakthrough;
            asset.qiForLevelBreakthrough = data.qiForLevelBreakthrough;
            asset.breakthroughFailureChance = data.breakthroughFailureChance;
            asset.breakthroughFailureDamage = data.breakthroughFailureDamage;
        }

        private static void ApplyElementData(ElementData asset, ElementJson data)
        {
            // Basic Info
            asset.elementType = ParseElement(data.element);
            asset.nameRu = data.nameRu;
            asset.nameEn = data.nameEn;
            asset.description = data.description;
            asset.color = ParseColor(data.color);

            // Relationships
            asset.oppositeElement = ParseElement(data.opposite);
            asset.affinityElements = ParseElementList(data.affinities);
            asset.weakToElements = ParseElementList(data.weakTo);

            // Damage Multipliers
            asset.oppositeMultiplier = data.oppositeMultiplier;
            asset.affinityMultiplier = data.affinityMultiplier;
            asset.voidMultiplier = data.voidMultiplier;

            // Effects
            if (data.effects != null)
            {
                foreach (var effectData in data.effects)
                {
                    var effect = new ElementEffect
                    {
                        effectName = effectData.effectName,
                        description = effectData.description,
                        baseDuration = effectData.baseDuration,
                        damageMultiplier = effectData.damageMultiplier,
                        applyChance = effectData.applyChance
                    };
                    asset.possibleEffects.Add(effect);
                }
            }

            // Environment
            asset.affectsEnvironment = data.affectsEnvironment;
        }

        private static void ApplyMortalStageData(MortalStageData asset, MortalStageJson data)
        {
            // Basic Info
            asset.stage = ParseMortalStage(data.stage);
            asset.nameRu = data.nameRu;
            asset.nameEn = data.nameEn;
            asset.description = data.description;

            // Age
            asset.minAge = data.minAge;
            asset.maxAge = data.maxAge;

            // Core Formation
            asset.minCoreFormation = data.minCoreFormation;
            asset.maxCoreFormation = data.maxCoreFormation;
            asset.coreFormationRate = data.coreFormationRate;

            // Qi Capacity
            asset.minQiCapacity = data.minQiCapacity;
            asset.maxQiCapacity = data.maxQiCapacity;
            asset.qiAbsorptionRate = data.qiAbsorptionRate;
            asset.canRegenerateQi = data.canRegenerateQi;

            // Stats
            asset.minStrength = data.minStrength;
            asset.maxStrength = data.maxStrength;
            asset.minAgility = data.minAgility;
            asset.maxAgility = data.maxAgility;
            asset.minConstitution = data.minConstitution;
            asset.maxConstitution = data.maxConstitution;

            // Awakening
            asset.baseAwakeningChance = data.baseAwakeningChance;
            asset.highDensityAwakeningChance = data.highDensityAwakeningChance;
            asset.criticalAwakeningChance = data.criticalAwakeningChance;
            asset.canAwaken = data.canAwaken;

            // Abilities
            asset.requiresFood = data.requiresFood;
            asset.requiresWater = data.requiresWater;
            asset.requiresSleep = data.requiresSleep;
            asset.canLearnMartialArts = data.canLearnMartialArts;
            asset.canMeditate = data.canMeditate;
            asset.abilitiesDescription = data.abilitiesDescription;

            // Transition
            asset.nextStage = ParseMortalStage(data.nextStage);
            asset.isAwakeningPoint = data.isAwakeningPoint;
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

        private static MortalStage ParseMortalStage(string value)
        {
            if (string.IsNullOrEmpty(value)) return MortalStage.None;

            switch (value.ToLower())
            {
                case "none": return MortalStage.None;
                case "newborn": return MortalStage.Newborn;
                case "child": return MortalStage.Child;
                case "adult": return MortalStage.Adult;
                case "mature": return MortalStage.Mature;
                case "elder": return MortalStage.Elder;
                case "awakening": return MortalStage.Awakening;
                default: return MortalStage.None;
            }
        }

        private static List<Element> ParseElementList(List<string> values)
        {
            var result = new List<Element>();
            if (values == null) return result;

            foreach (var v in values)
            {
                result.Add(ParseElement(v));
            }
            return result;
        }

        private static Color ParseColor(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return Color.white;

            // Remove # if present
            if (hex.StartsWith("#")) hex = hex.Substring(1);

            if (hex.Length != 6) return Color.white;

            int r = int.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            int g = int.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
            int b = int.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);

            return new Color(r / 255f, g / 255f, b / 255f);
        }

        #endregion

        #region Utility

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folder = Path.GetFileName(path);

                // Create parent directories recursively
                if (!AssetDatabase.IsValidFolder(parent))
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

        #endregion

        #region JSON Classes

        [System.Serializable]
        private class CultivationLevelsJson
        {
            public List<CultivationLevelJson> levels;
        }

        [System.Serializable]
        private class CultivationLevelJson
        {
            public int level;
            public string nameRu;
            public string nameEn;
            public string description;
            public int qiDensity;
            public float coreCapacityMultiplier;
            public long baseCoreCapacity;
            public float agingMultiplier;
            public float conductivityMultiplier;
            public string abilitiesDescription;
            public bool noFoodRequired;
            public bool noWaterRequired;
            public bool canFly;
            public bool canRegenerateLimbs;
            public bool useDynamicBreakthroughCalculation;
            public int subLevelMultiplier;
            public int levelMultiplier;
            public long qiForSubLevelBreakthrough;
            public long qiForLevelBreakthrough;
            public float breakthroughFailureChance;
            public float breakthroughFailureDamage;
        }

        [System.Serializable]
        private class ElementsJson
        {
            public List<ElementJson> elements;
        }

        [System.Serializable]
        private class ElementJson
        {
            public string element;
            public string nameRu;
            public string nameEn;
            public string description;
            public string color;
            public string opposite;
            public List<string> affinities;
            public List<string> weakTo;
            public float oppositeMultiplier;
            public float affinityMultiplier;
            public float voidMultiplier;
            public List<ElementEffectJson> effects;
            public bool affectsEnvironment;
        }

        [System.Serializable]
        private class ElementEffectJson
        {
            public string effectName;
            public string description;
            public int baseDuration;
            public float damageMultiplier;
            public float applyChance;
        }

        [System.Serializable]
        private class MortalStagesJson
        {
            public List<MortalStageJson> stages;
        }

        [System.Serializable]
        private class MortalStageJson
        {
            // Basic Info
            public string stage;
            public string nameRu;
            public string nameEn;
            public string description;

            // Age
            public int minAge;
            public int maxAge;

            // Core Formation
            public float minCoreFormation;
            public float maxCoreFormation;
            public float coreFormationRate;

            // Qi Capacity — FIX: int→long (2026-04-11)
            public long minQiCapacity;
            public long maxQiCapacity;
            public float qiAbsorptionRate;
            public bool canRegenerateQi;

            // Stats
            public float minStrength;
            public float maxStrength;
            public float minAgility;
            public float maxAgility;
            public float minConstitution;
            public float maxConstitution;

            // Awakening
            public float baseAwakeningChance;
            public float highDensityAwakeningChance;
            public float criticalAwakeningChance;
            public bool canAwaken;

            // Abilities
            public bool requiresFood;
            public bool requiresWater;
            public bool requiresSleep;
            public bool canLearnMartialArts;
            public bool canMeditate;
            public string abilitiesDescription;

            // Transition
            public string nextStage;
            public bool isAwakeningPoint;
        }

        #endregion
    }
}
#endif
