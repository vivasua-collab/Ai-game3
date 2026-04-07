// ============================================================================
// TestLocationSetup.cs — Настройка тестовой локации
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

namespace CultivationGame.TileSystem.Editor
{
    /// <summary>
    /// Автоматическая настройка тестовой локации.
    /// </summary>
    public static class TestLocationSetup
    {
        private const string SCENE_PATH = "Assets/Scenes/TestLocation.unity";

        [MenuItem("Tools/Setup Test Location Scene")]
        public static void SetupTestLocation()
        {
            // Создать сцену
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            // Создать Grid
            var gridObj = new GameObject("Grid");
            var grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(2f, 2f, 1f); // Тайл = 2×2 м

            // Создать Tilemap для поверхности
            var terrainObj = new GameObject("Terrain");
            terrainObj.transform.SetParent(gridObj.transform);
            var terrainTilemap = terrainObj.AddComponent<Tilemap>();
            var terrainRenderer = terrainObj.AddComponent<TilemapRenderer>();
            terrainRenderer.sortOrder = 0;
            terrainObj.AddComponent<TilemapCollider2D>();

            // Создать Tilemap для объектов
            var objectsObj = new GameObject("Objects");
            objectsObj.transform.SetParent(gridObj.transform);
            var objectTilemap = objectsObj.AddComponent<Tilemap>();
            var objectRenderer = objectsObj.AddComponent<TilemapRenderer>();
            objectRenderer.sortOrder = 1;

            // Создать контроллер карты
            var controllerObj = new GameObject("TileMapController");
            var controller = controllerObj.AddComponent<TileMapController>();
            
            // Назначить ссылки через SerializedObject
            var so = new SerializedObject(controller);
            so.FindProperty("terrainTilemap").objectReferenceValue = terrainTilemap;
            so.FindProperty("objectTilemap").objectReferenceValue = objectTilemap;
            so.FindProperty("defaultWidth").intValue = 30;
            so.FindProperty("defaultHeight").intValue = 20;
            so.ApplyModifiedProperties();

            // Создать камеру
            var cameraObj = Camera.main?.gameObject;
            if (cameraObj == null)
            {
                cameraObj = new GameObject("Main Camera");
                cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }
            cameraObj.transform.position = new Vector3(30, 20, -10);
            cameraObj.GetComponent<Camera>().orthographic = true;
            cameraObj.GetComponent<Camera>().orthographicSize = 20;

            // Создать источник света
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;

            // Сохранить сцену
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            
            Debug.Log($"Created test location scene at {SCENE_PATH}");
            Debug.Log("Run 'Tools > Generate Tile Sprites' to create tile sprites");
            Debug.Log("Then assign sprites to TileMapController in the Inspector");
        }
    }
}
#endif
