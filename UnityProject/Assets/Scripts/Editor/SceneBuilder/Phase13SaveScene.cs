// ============================================================================
// Phase13SaveScene.cs — Фаза 13: Сохранение сцены
// Cultivation World Simulator
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase13SaveScene : IScenePhase
    {
        public string Name => "Save Scene";
        public string MenuPath => "Phase 13: Save Scene";
        public int Order => 13;

        public bool IsNeeded()
        {
            var activeScene = SceneManager.GetActiveScene();
            return activeScene.isDirty;
        }

        public void Execute()
        {
            var activeScene = SceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(activeScene.path))
            {
                SceneBuilderUtils.EnsureDirectory("Assets/Scenes");
                EditorSceneManager.SaveScene(activeScene, SceneBuilderConstants.SCENE_PATH);
                Debug.Log($"[Phase13] Scene saved to: {SceneBuilderConstants.SCENE_PATH}");
            }
            else
            {
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"[Phase13] Scene saved: {activeScene.path}");
            }
        }
    }
}
#endif
