// ============================================================================
// Phase09GenerateAssets.cs — Фаза 09: Генерация ассетов из JSON
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-09
// Редактировано: 2026-04-29 12:13:35 UTC — аудит: IsNeeded() расширена для всех типов ассетов
// Редактировано: 2026-04-29 12:30:00 UTC — повторный аудит: +FormationCores в IsNeeded()
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Generators;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase09GenerateAssets : IScenePhase
    {
        public string Name => "Generate Assets (SO from JSON)";
        public string MenuPath => "Phase 09: Generate Assets from JSON";
        public int Order => 9;

        public bool IsNeeded()
        {
            // Базовые ассеты
            if (!SceneBuilderUtils.HasAssetsInFolder("Assets/Data/CultivationLevels")) return true;
            if (!SceneBuilderUtils.HasAssetsInFolder("Assets/Data/Elements")) return true;
            if (!SceneBuilderUtils.HasAssetsInFolder("Assets/Data/MortalStages")) return true;
            // Расширенные ассеты
            if (!SceneBuilderUtils.HasAssetsInFolder("Assets/Data/Techniques")) return true;
            if (!SceneBuilderUtils.HasAssetsInFolder("Assets/Data/NPCPresets")) return true;
            if (!SceneBuilderUtils.HasAssetsInFolder("Assets/Data/Equipment")) return true;
            if (!SceneBuilderUtils.HasAssetsInFolder("Assets/Data/Items")) return true;
            if (!SceneBuilderUtils.HasAssetsInFolder("Assets/Data/Materials")) return true;
            // Формации
            if (!SceneBuilderUtils.HasAssetsInFolder("Assets/Data/Formations")) return true;
            if (!SceneBuilderUtils.HasAssetsInFolder("Assets/Data/FormationCores")) return true;
            return false;
        }

        public void Execute()
        {
            int total = 0;

            Debug.Log("[Phase09] Generating base assets from JSON...");
            total += AssetGenerator.GenerateCultivationLevels();
            total += AssetGenerator.GenerateElements();
            total += AssetGenerator.GenerateMortalStages();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase09] Generating extended assets from JSON...");
            total += AssetGeneratorExtended.GenerateTechniques();
            total += AssetGeneratorExtended.GenerateNPCPresets();
            total += AssetGeneratorExtended.GenerateEquipment();
            total += AssetGeneratorExtended.GenerateItems();
            total += AssetGeneratorExtended.GenerateMaterials();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Phase09] Generating formation assets...");
            total += FormationAssetGenerator.GenerateFormationData();
            total += FormationAssetGenerator.GenerateFormationCoreData();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            int errors = AssetGeneratorExtended.ValidateAllAssets();
            errors += FormationAssetGenerator.ValidateFormationAssets();

            if (errors > 0)
                Debug.LogWarning($"[Phase09] Asset generation complete with {errors} validation warnings");
            else
                Debug.Log($"[Phase09] Generated {total} assets — all valid!");
        }
    }
}
#endif
