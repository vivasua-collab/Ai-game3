// ============================================================================
// Phase12TMPEssentials.cs — Фаза 12: Импорт TMP Essentials
// Cultivation World Simulator
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase12TMPEssentials : IScenePhase
    {
        public string Name => "TMP Essentials";
        public string MenuPath => "Phase 12: Import TMP Essentials";
        public int Order => 12;

        public bool IsNeeded()
        {
            var tmpSettings = TMP_Settings.instance;
            return tmpSettings == null;
        }

        public void Execute()
        {
            Debug.Log("[Phase12] Checking TMP Essentials...");

            try
            {
                var tmpSettings = TMP_Settings.instance;
                if (tmpSettings == null)
                {
                    Debug.Log("[Phase12] TMP Essentials not found. Importing...");

                    var importerType = System.Type.GetType(
                        "TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro");
                    if (importerType != null)
                    {
                        var window = EditorWindow.GetWindow(importerType);
                        if (window != null)
                        {
                            Debug.Log("[Phase12] TMP Import window opened. Please click 'Import TMP Essentials'.");
                            return;
                        }
                    }

                    Debug.LogWarning("[Phase12] Cannot auto-import TMP. " +
                        "Please: Window → TextMeshPro → Import TMP Essentials");
                }
                else
                {
                    Debug.Log("[Phase12] TMP Essentials already imported");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Phase12] TMP check failed: {ex.Message}. " +
                    "Please import manually: Window → TextMeshPro → Import TMP Essentials");
            }
        }
    }
}
#endif
