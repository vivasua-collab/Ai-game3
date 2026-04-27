// ============================================================================
// Phase11GenerateUIPrefabs.cs — Фаза 11: Генерация UI префабов формаций
// Cultivation World Simulator
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Generators;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase11GenerateUIPrefabs : IScenePhase
    {
        public string Name => "Generate Formation UI Prefabs";
        public string MenuPath => "Phase 11: Generate Formation UI Prefabs";
        public int Order => 11;

        public bool IsNeeded()
        {
            return !System.IO.File.Exists("Assets/Prefabs/UI/Formation/FormationListItem.prefab");
        }

        public void Execute()
        {
            Debug.Log("[Phase11] Generating formation UI prefabs...");
            FormationUIPrefabsGenerator.GenerateAllPrefabs();
            AssetDatabase.Refresh();
        }
    }
}
#endif
