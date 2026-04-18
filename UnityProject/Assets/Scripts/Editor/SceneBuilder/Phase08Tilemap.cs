// ============================================================================
// Phase08Tilemap.cs — Фаза 08: Tilemap система
// Cultivation World Simulator
// ============================================================================
// Объединяет: Phase 08 (FullSceneBuilder) + PATCH-002, PATCH-005, PATCH-006, PATCH-007
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using CultivationGame.Core;
using CultivationGame.World;
using CultivationGame.TileSystem;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase08Tilemap : IScenePhase
    {
        public string Name => "Tilemap";
        public string MenuPath => "Phase 08: Tilemap System";
        public int Order => 8;

        public bool IsNeeded()
        {
            SceneBuilderUtils.EnsureSceneOpen();
            return UnityEngine.Object.FindFirstObjectByType<Grid>() == null;
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            if (UnityEngine.Object.FindFirstObjectByType<Grid>() != null)
            {
                Debug.Log("[Phase08] Grid already exists — применяем патчи к существующему");
                ApplyPatchesToExisting();
                return;
            }

            // Grid
            GameObject gridObj = new GameObject("Grid");
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(2f, 2f, 1f); // PATCH-006
            grid.cellGap = Vector3.zero;

            // Terrain Tilemap
            GameObject terrainObj = new GameObject("Terrain");
            terrainObj.transform.SetParent(gridObj.transform);
            var terrainTilemap = terrainObj.AddComponent<Tilemap>();
            var terrainRenderer = terrainObj.AddComponent<TilemapRenderer>();
            terrainRenderer.sortingLayerName = "Terrain"; // PATCH-002
            terrainRenderer.sortingOrder = 0;
            terrainRenderer.mode = TilemapRenderer.Mode.Chunk;
            // PATCH-007: НЕ добавляем TilemapCollider2D на Terrain

            // Objects Tilemap
            GameObject objectsObj = new GameObject("Objects");
            objectsObj.transform.SetParent(gridObj.transform);
            var objectTilemap = objectsObj.AddComponent<Tilemap>();
            var objectRenderer = objectsObj.AddComponent<TilemapRenderer>();
            objectRenderer.sortingLayerName = "Objects"; // PATCH-002
            objectRenderer.sortingOrder = 0;
            objectRenderer.mode = TilemapRenderer.Mode.Chunk;
            objectsObj.AddComponent<TilemapCollider2D>();

            // TileMapController
            GameObject controllerObj = new GameObject("TileMapController");
            var controller = controllerObj.AddComponent<TileMapController>();
            var so = new SerializedObject(controller);
            so.FindProperty("terrainTilemap").objectReferenceValue = terrainTilemap;
            so.FindProperty("objectTilemap").objectReferenceValue = objectTilemap;
            so.FindProperty("defaultWidth").intValue = 100;
            so.FindProperty("defaultHeight").intValue = 80;
            so.ApplyModifiedProperties();

            // TestLocationGameController
            var gameControllerObj = new GameObject("GameController");
            var gameController = gameControllerObj.AddComponent<TestLocationGameController>();
            var gso = new SerializedObject(gameController);
            gso.FindProperty("tileMapController").objectReferenceValue = controller;
            gso.ApplyModifiedProperties();

            // DestructibleObjectController
            var destructibleController = gameControllerObj.AddComponent<DestructibleObjectController>();
            var dso = new SerializedObject(destructibleController);
            dso.FindProperty("tileMapController").objectReferenceValue = controller;
            dso.ApplyModifiedProperties();

            // PATCH-005: HarvestableSpawner
            var harvestableSpawner = gameControllerObj.AddComponent<HarvestableSpawner>();
            var hso = new SerializedObject(harvestableSpawner);
            hso.FindProperty("tileMapController").objectReferenceValue = controller;
            hso.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(gridObj, "Create Tilemap System");
            Undo.RegisterCreatedObjectUndo(controllerObj, "Create TileMapController");
            Undo.RegisterCreatedObjectUndo(gameControllerObj, "Create GameController");

            RenderPipelineLogger.LogTilemapState();
            Debug.Log("[Phase08] Tilemap system created");
        }

        /// <summary>
        /// Применить PATCH-002, PATCH-005, PATCH-006, PATCH-007 к существующей системе.
        /// </summary>
        private void ApplyPatchesToExisting()
        {
            // PATCH-002: TilemapRenderer на правильных слоях
            var renderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                string name = r.gameObject.name;
                string targetLayer;
                if (name.Contains("Terrain") || name.Contains("terrain"))
                    targetLayer = "Terrain";
                else if (name.Contains("Object") || name.Contains("object") || name.Contains("Overlay"))
                    targetLayer = "Objects";
                else
                    targetLayer = "Objects";

                if (r.sortingLayerName != targetLayer)
                {
                    r.sortingLayerName = targetLayer;
                    r.sortingOrder = (targetLayer == "Objects" && name.Contains("Overlay")) ? 10 : 0;
                    Debug.Log($"[Phase08] PATCH-002: \"{name}\" → \"{targetLayer}\"");
                }
            }

            // PATCH-006: Grid cellSize
            var grid = Object.FindFirstObjectByType<Grid>();
            if (grid != null && (grid.cellSize != new Vector3(2f, 2f, 1f) || grid.cellGap != Vector3.zero))
            {
                grid.cellSize = new Vector3(2f, 2f, 1f);
                grid.cellGap = Vector3.zero;
                Debug.Log("[Phase08] PATCH-006: Grid cellSize=(2,2,1)");
            }

            // PATCH-007: Нет TilemapCollider2D на Terrain
            if (grid != null)
            {
                var terrainT = grid.transform.Find("Terrain");
                if (terrainT != null)
                {
                    var terrainCollider = terrainT.GetComponent<TilemapCollider2D>();
                    if (terrainCollider != null)
                    {
                        Object.DestroyImmediate(terrainCollider);
                        Debug.Log("[Phase08] PATCH-007: TilemapCollider2D удалён с Terrain");
                    }
                }
            }

            // PATCH-005: HarvestableSpawner
            var gameController = Object.FindFirstObjectByType<TestLocationGameController>();
            if (gameController != null && gameController.GetComponent<HarvestableSpawner>() == null)
            {
                var tileController = Object.FindFirstObjectByType<TileMapController>();
                var hs = gameController.gameObject.AddComponent<HarvestableSpawner>();
                var hso = new SerializedObject(hs);
                if (tileController != null)
                    hso.FindProperty("tileMapController").objectReferenceValue = tileController;
                hso.ApplyModifiedProperties();
                Debug.Log("[Phase08] PATCH-005: HarvestableSpawner добавлен");
            }
        }
    }
}
#endif
