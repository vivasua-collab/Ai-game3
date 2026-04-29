// ============================================================================
// Phase15ConfigureTestLocation.cs — Фаза 15: Настройка тестовой локации
// Cultivation World Simulator
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
    public class Phase15ConfigureTestLocation : IScenePhase
    {
        public string Name => "Configure Test Location";
        public string MenuPath => "Phase 15: Configure Test Location";
        public int Order => 15;

        public bool IsNeeded()
        {
            // Если сцена не открыта — Camera.main вернёт null → return true
            var cam = Camera.main;
            if (cam == null) return true;

            // Камера ещё не настроена для тайловой карты
            if (cam.transform.position.x < 1f && cam.transform.position.y < 1f)
                return true;

            if (UnityEngine.Object.FindFirstObjectByType<Grid>() == null)
                return true;

            return false;
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            ConfigureCamera();
            ConfigureColliders();
            EnsureGameController();

            Debug.Log("[Phase15] Test Location настроена");
        }

        private void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var controller = UnityEngine.Object.FindFirstObjectByType<TileMapController>();
            if (controller == null) return;

            var so = new SerializedObject(controller);
            int width = so.FindProperty("defaultWidth")?.intValue ?? 30;
            int height = so.FindProperty("defaultHeight")?.intValue ?? 20;

            float centerX = width * 2f / 2f;
            float centerY = height * 2f / 2f;

            cam.transform.position = new Vector3(centerX, centerY, -10f);
            cam.orthographicSize = 8f;
            cam.orthographic = true;

            // Camera2DSetup
            var camera2D = cam.GetComponent<Camera2DSetup>();
            if (camera2D == null)
                camera2D = cam.gameObject.AddComponent<Camera2DSetup>();

            var camSo = new SerializedObject(camera2D);
            camSo.FindProperty("orthographicSize").floatValue = 8f;
            camSo.FindProperty("followEnabled").boolValue = true;
            camSo.FindProperty("followSmoothness").floatValue = 0.08f;
            camSo.FindProperty("useBounds").boolValue = true;
            camSo.FindProperty("boundsMin").vector2Value = Vector2.zero;
            camSo.FindProperty("boundsMax").vector2Value = new Vector2(width * 2f, height * 2f);
            camSo.ApplyModifiedProperties();

            Debug.Log($"[Phase15] Камера: ({centerX}, {centerY}, -10), Camera2DSetup привязан");
        }

        private void ConfigureColliders()
        {
            var grid = UnityEngine.Object.FindFirstObjectByType<Grid>();
            if (grid == null) return;

            // Terrain: УДАЛИТЬ TilemapCollider2D (блокирует движение)
            var terrainTransform = grid.transform.Find("Terrain");
            if (terrainTransform != null)
            {
                var terrainCollider = terrainTransform.GetComponent<TilemapCollider2D>();
                if (terrainCollider != null)
                {
                    Object.DestroyImmediate(terrainCollider);
                    Debug.Log("[Phase15] Удалён TilemapCollider2D с Terrain");
                }
            }

            // Objects: ДОБАВИТЬ TilemapCollider2D если отсутствует
            var objectsTransform = grid.transform.Find("Objects");
            if (objectsTransform != null)
            {
                var objectCollider = objectsTransform.GetComponent<TilemapCollider2D>();
                if (objectCollider == null)
                {
                    objectsTransform.gameObject.AddComponent<TilemapCollider2D>();
                    Debug.Log("[Phase15] TilemapCollider2D добавлен на Objects");
                }
            }
        }

        private void EnsureGameController()
        {
            var gameController = UnityEngine.Object.FindFirstObjectByType<TestLocationGameController>();
            var controller = UnityEngine.Object.FindFirstObjectByType<TileMapController>();

            if (gameController == null)
            {
                if (controller == null) return;

                var gcObj = new GameObject("GameController");
                var gc = gcObj.AddComponent<TestLocationGameController>();
                var gso = new SerializedObject(gc);
                gso.FindProperty("tileMapController").objectReferenceValue = controller;
                gso.ApplyModifiedProperties();

                var dc = gcObj.AddComponent<DestructibleObjectController>();
                var dso = new SerializedObject(dc);
                dso.FindProperty("tileMapController").objectReferenceValue = controller;
                dso.ApplyModifiedProperties();

                var hs = gcObj.AddComponent<HarvestableSpawner>();
                var hso = new SerializedObject(hs);
                hso.FindProperty("tileMapController").objectReferenceValue = controller;
                hso.ApplyModifiedProperties();

                Debug.Log("[Phase15] GameController + DestructibleObjectController + HarvestableSpawner созданы");
            }
            else
            {
                // Проверяем отсутствующие компоненты
                if (gameController.GetComponent<HarvestableSpawner>() == null && controller != null)
                {
                    var hs = gameController.gameObject.AddComponent<HarvestableSpawner>();
                    var hso = new SerializedObject(hs);
                    hso.FindProperty("tileMapController").objectReferenceValue = controller;
                    hso.ApplyModifiedProperties();
                    Debug.Log("[Phase15] HarvestableSpawner добавлен к GameController");
                }

                if (gameController.GetComponent<DestructibleObjectController>() == null && controller != null)
                {
                    var dc = gameController.gameObject.AddComponent<DestructibleObjectController>();
                    var dso = new SerializedObject(dc);
                    dso.FindProperty("tileMapController").objectReferenceValue = controller;
                    dso.ApplyModifiedProperties();
                    Debug.Log("[Phase15] DestructibleObjectController добавлен к GameController");
                }
            }
        }
    }
}
#endif
