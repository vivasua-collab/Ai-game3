// ============================================================================
// ScenePatchBuilder.cs — УСТАРЕВШИЙ файл (DEPRECATED)
// Cultivation World Simulator
// ============================================================================
// ВНИМАНИЕ: Этот файл УСТАРЕЛ с версии 2.0!
// Все патчи объединены в соответствующие фазы FullSceneBuilder (v2.0):
//
//   PATCH-001 (Sorting Layers порядок)   → Phase02TagsLayers
//   PATCH-002 (TilemapRenderer слои)     → Phase08Tilemap
//   PATCH-003 (Player sorting layer)     → Phase06Player
//   PATCH-004 (GlobalLight2D)            → Phase04CameraLight
//   PATCH-005 (HarvestableSpawner)       → Phase08Tilemap
//   PATCH-006 (Grid настройки)           → Phase08Tilemap
//   PATCH-007 (Terrain без коллайдера)   → Phase08Tilemap
//   PATCH-008 (Missing Scripts/Prefabs)  → SceneBuilderUtils.CleanMissingPrefabs
//   PATCH-009 (Camera boundsMin)         → Phase04CameraLight
//   PATCH-010 (Camera Undo)             → Phase04CameraLight
//   PATCH-011 (6 Sorting Layers)         → Phase02TagsLayers
//   PATCH-012 (UI→GameUI layer)          → Phase02TagsLayers
//
// Используйте: Tools → Full Scene Builder → Build All (One Click)
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace CultivationGame.Editor
{
    /// <summary>
    /// УСТАРЕВШИЙ патчер. Все патчи объединены в FullSceneBuilder v2.0.
    /// Этот класс оставлен только для обратной совместимости меню.
    /// </summary>
    [System.Obsolete("ScenePatchBuilder устарел. Используйте FullSceneBuilder v2.0 (Tools → Full Scene Builder → Build All)")]
    public static class ScenePatchBuilder
    {
        [MenuItem("Tools/Scene Patch Builder/Apply All Pending Patches (DEPRECATED — use Full Scene Builder)", false, 0)]
        public static void ApplyAllPending()
        {
            Debug.LogWarning("[ScenePatchBuilder] УСТАРЕЛО! Все патчи теперь в FullSceneBuilder v2.0. " +
                "Используйте: Tools → Full Scene Builder → Build All (One Click)");
            EditorUtility.DisplayDialog("Устаревший инструмент",
                "ScenePatchBuilder устарел!\n\nВсе патчи объединены в FullSceneBuilder v2.0.\n" +
                "Используйте: Tools → Full Scene Builder → Build All (One Click)",
                "OK");
        }

        [MenuItem("Tools/Scene Patch Builder/Validate Current Scene (DEPRECATED — use Full Scene Builder)", false, 100)]
        public static void ValidateScene()
        {
            Debug.LogWarning("[ScenePatchBuilder] УСТАРЕЛО! Используйте FullSceneBuilder Build All для валидации.");
        }

        [MenuItem("Tools/Scene Patch Builder/Show Applied Patches (DEPRECATED)", false, 200)]
        public static void ShowAppliedPatches()
        {
            Debug.LogWarning("[ScenePatchBuilder] УСТАРЕЛО!");
        }

        [MenuItem("Tools/Scene Patch Builder/Reset Patch History (Dangerous!) (DEPRECATED)", false, 300)]
        public static void ResetPatchHistory()
        {
            Debug.LogWarning("[ScenePatchBuilder] УСТАРЕЛО!");
        }
    }
}
#endif
