// ============================================================================
// TestLocationSetup.cs — Настройка тестовой локации
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-09 07:14:00 UTC — добавлен using TMPro
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;

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

            // === Создать UI ===
            CreateUI(scene);
            
            // === Создать TestLocationGameController ===
            CreateGameController(controllerObj);
            
            // === Камера ===
            var cameraObj = Camera.main?.gameObject;
            if (cameraObj == null)
            {
                cameraObj = new GameObject("Main Camera");
                cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }
            cameraObj.transform.position = new Vector3(30, 20, -10);
            cameraObj.GetComponent<Camera>().orthographic = true;
            cameraObj.GetComponent<Camera>().orthographicSize = 15;

            // Создать источник света
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;

            // Сохранить сцену
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            
            Debug.Log($"Created test location scene at {SCENE_PATH}");
            Debug.Log("NEXT STEPS:");
            Debug.Log("1. Tools > Generate Tile Sprites");
            Debug.Log("2. Assign sprites to TileMapController in Inspector");
            Debug.Log("3. Assign UI elements to TestLocationGameController");
        }
        
        private static void CreateUI(UnityEngine.SceneManagement.Scene scene)
        {
            // Canvas
            GameObject canvasGO = new GameObject("GameUI");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            // EventSystem
            var eventSystem = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var inputModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    eventSystemGO.AddComponent(inputModuleType);
                }
            }
            
            // HUD Panel
            GameObject hud = new GameObject("HUD");
            hud.transform.SetParent(canvasGO.transform, false);
            
            RectTransform hudRect = hud.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0, 1);
            hudRect.anchorMax = new Vector2(0, 1);
            hudRect.pivot = new Vector2(0, 1);
            hudRect.anchoredPosition = new Vector2(20, -20);
            hudRect.sizeDelta = new Vector2(320, 200);
            
            Image hudBg = hud.AddComponent<Image>();
            hudBg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            
            // Location Text
            var locationText = CreateText(hud, "LocationText", "Test Location", new Vector2(10, -10), 22, TextAnchor.UpperLeft);
            locationText.fontStyle = FontStyles.Bold;
            
            // Position Text
            var posText = CreateText(hud, "PositionText", "Position: (0, 0)", new Vector2(10, -38), 16, TextAnchor.UpperLeft);
            posText.color = new Color(0.7f, 0.7f, 0.8f);
            
            // HP Bar
            CreateSlider(hud, "HealthBar", new Vector2(10, -70), 280, 20, new Color(0.8f, 0.2f, 0.2f));
            var hpText = CreateText(hud, "HealthText", "HP: 100/100", new Vector2(10, -95), 16, TextAnchor.UpperLeft);
            
            // Qi Bar
            CreateSlider(hud, "QiBar", new Vector2(10, -120), 280, 20, new Color(0.2f, 0.5f, 0.9f));
            var qiText = CreateText(hud, "QiText", "Ци: 1000/1000", new Vector2(10, -145), 16, TextAnchor.UpperLeft);
            
            // Stamina Bar
            CreateSlider(hud, "StaminaBar", new Vector2(10, -170), 280, 16, new Color(0.2f, 0.8f, 0.3f));
            
            // Instructions
            GameObject instructions = new GameObject("Instructions");
            instructions.transform.SetParent(canvasGO.transform, false);
            
            RectTransform instrRect = instructions.AddComponent<RectTransform>();
            instrRect.anchorMin = new Vector2(1, 0);
            instrRect.anchorMax = new Vector2(1, 0);
            instrRect.pivot = new Vector2(1, 0);
            instrRect.anchoredPosition = new Vector2(-20, 20);
            instrRect.sizeDelta = new Vector2(300, 120);
            
            Image instrBg = instructions.AddComponent<Image>();
            instrBg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);
            
            var instrText = CreateText(instructions, "Text", 
                "Управление:\n" +
                "WASD - движение\n" +
                "Shift - бег\n" +
                "F5 - медитация\n" +
                "G - регенерировать карту", 
                new Vector2(15, -10), 16, TextAnchor.UpperLeft);
            
            Debug.Log("[TestLocationSetup] UI created");
        }
        
        private static void CreateGameController(GameObject tileMapController)
        {
            // Найти или создать TestLocationGameController
            var gameControllerObj = new GameObject("GameController");
            var gameController = gameControllerObj.AddComponent<TestLocationGameController>();
            
            // Назначить ссылку на TileMapController
            var so = new SerializedObject(gameController);
            so.FindProperty("tileMapController").objectReferenceValue = tileMapController.GetComponent<TileMapController>();
            so.ApplyModifiedProperties();
            
            // Добавить DestructibleObjectController
            var destructibleController = gameControllerObj.AddComponent<DestructibleObjectController>();
            var destructibleSo = new SerializedObject(destructibleController);
            destructibleSo.FindProperty("tileMapController").objectReferenceValue = tileMapController.GetComponent<TileMapController>();
            destructibleSo.ApplyModifiedProperties();
            
            Debug.Log("[TestLocationSetup] GameController created with DestructibleObjectController");
        }
        
        private static TextMeshProUGUI CreateText(GameObject parent, string name, string text, Vector2 position, int fontSize, TextAnchor alignment)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent.transform, false);
            
            RectTransform rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280, fontSize + 10);
            
            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = alignment == TextAnchor.UpperLeft ? TextAlignmentOptions.TopLeft :
                           alignment == TextAnchor.UpperCenter ? TextAlignmentOptions.Top :
                           TextAlignmentOptions.TopLeft;
            
            return tmp;
        }
        
        private static Slider CreateSlider(GameObject parent, string name, Vector2 position, float width, float height, Color fillColor)
        {
            GameObject sliderGO = new GameObject(name);
            sliderGO.transform.SetParent(parent.transform, false);
            
            RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 1);
            sliderRect.anchorMax = new Vector2(0, 1);
            sliderRect.pivot = new Vector2(0, 1);
            sliderRect.anchoredPosition = position;
            sliderRect.sizeDelta = new Vector2(width, height);
            
            Slider slider = sliderGO.AddComponent<Slider>();
            
            // Background
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f);
            
            // Fill Area
            GameObject fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;
            
            // Fill
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            RectTransform fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fillGO.AddComponent<Image>();
            fillImage.color = fillColor;
            
            // Assign to slider
            slider.targetGraphic = fillImage;
            slider.fillRect = fillRect;
            slider.handleRect = null;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 100;
            
            return slider;
        }
    }
}
#endif
