// ============================================================================
// Phase03SceneCreation.cs — Фаза 03: Создание сцены
// Cultivation World Simulator
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase03SceneCreation : IScenePhase
    {
        public string Name => "Scene Creation";
        public string MenuPath => "Phase 03: Create Scene";
        public int Order => 3;

        public bool IsNeeded()
        {
            return !System.IO.File.Exists(SceneBuilderConstants.SCENE_PATH);
        }

        public void Execute()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var defaultCamera = Camera.main;
            if (defaultCamera != null)
                Object.DestroyImmediate(defaultCamera.gameObject);

            SceneBuilderUtils.EnsureDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, SceneBuilderConstants.SCENE_PATH);
            Debug.Log($"[Phase03] Scene created: {SceneBuilderConstants.SCENE_PATH}");
        }
    }
}
#endif
