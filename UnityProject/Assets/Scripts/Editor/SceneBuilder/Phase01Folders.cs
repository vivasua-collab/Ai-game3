// ============================================================================
// Phase01Folders.cs — Фаза 01: Создание папок
// Cultivation World Simulator
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase01Folders : IScenePhase
    {
        public string Name => "Folders";
        public string MenuPath => "Phase 01: Folders";
        public int Order => 1;

        public bool IsNeeded()
        {
            foreach (var folder in SceneBuilderConstants.REQUIRED_FOLDERS)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    return true;
            }
            return false;
        }

        public void Execute()
        {
            int created = 0;
            foreach (var folder in SceneBuilderConstants.REQUIRED_FOLDERS)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    SceneBuilderUtils.EnsureDirectory(folder);
                    created++;
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[Phase01] Created {created} folders");
        }
    }
}
#endif
