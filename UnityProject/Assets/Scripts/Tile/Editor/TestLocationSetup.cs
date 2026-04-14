// ============================================================================
// TestLocationSetup.cs — Настройка тестовой локации
// Cultivation World Simulator
// Версия: 1.2 — исправлены ошибки компиляции Unity 6.3
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-14 08:05:00 UTC — размер карты 100×80, Camera2DSetup bounds
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 1.2:
// - FIX: NEXT_STEPS изменён с const на static readonly (CS0133)
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 1.1:
// - FIX: UI строки вынесены в константы (аудит Unity 6.3)
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using CultivationGame.World;
using CultivationGame.Core;

namespace CultivationGame.TileSystem.Editor
{
    /// <summary>
    /// Автоматическая настройка тестовой локации.
    /// </summary>
    public static class TestLocationSetup
    {
        private const string SCENE_PATH = "Assets/Scenes/TestLocation.unity";

        #region UI Strings (вынесены из magic strings)

        // Локализация UI
        private const string UI_LOCATION_NAME = "Test Location";
        private const string UI_POSITION_FORMAT = "Position: ({0}, {1})";
        private const string UI_HP_FORMAT = "HP: {0}/{1}";
        private const string UI_QI_FORMAT = "Ци: {0}/{1}";

        // Инструкции управления
        private const string UI_INSTRUCTIONS = 
            "Управление:\n" +
            "WASD - движение\n" +
            "Shift - бег\n" +
            "F5 - медитация\n" +
            "G - регенерировать карту";

        // Сообщения консоли
        private const string MSG_SCENE_CREATED = "Created test location scene at {0}";
        private const string MSG_UI_CREATED = "[TestLocationSetup] UI created";
        private const string MSG_CONTROLLER_CREATED = "[TestLocationSetup] GameController created with DestructibleObjectController";

        // Инструкции для разработчика
        private static readonly string[] NEXT_STEPS = new string[]
        {
            "1. Tools > Generate Tile Sprites",
            "2. Assign sprites to TileMapController in Inspector",
            "3. Assign UI elements to TestLocationGameController"
        };

        #endregion

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
            terrainRenderer.sortOrder = (TilemapRenderer.SortOrder)0; // FIX CS0266 (2026-04-11)
            terrainObj.AddComponent<TilemapCollider2D>();

            // Создать Tilemap для объектов
            var objectsObj = new GameObject("Objects");
            objectsObj.transform.SetParent(gridObj.transform);
            var objectTilemap = objectsObj.AddComponent<Tilemap>();
            var objectRenderer = objectsObj.AddComponent<TilemapRenderer>();
            objectRenderer.sortOrder = (TilemapRenderer.SortOrder)1; // FIX CS0266 (2026-04-11)

            // Создать контроллер карты
            var controllerObj = new GameObject("TileMapController");
            var controller = controllerObj.AddComponent<TileMapController>();
            
            // Назначить ссылки через SerializedObject
            var so = new SerializedObject(controller);
            so.FindProperty("terrainTilemap").objectReferenceValue = terrainTilemap;
            so.FindProperty("objectTilemap").objectReferenceValue = objectTilemap;
            so.FindProperty("defaultWidth").intValue = 100;
            so.FindProperty("defaultHeight").intValue = 80;
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
            cameraObj.transform.position = new Vector3(100, 80, -10);
            var cam = cameraObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 10;
            
            // Добавить Camera2DSetup с настройками слежения
            var camera2D = cameraObj.GetComponent<Camera2DSetup>();
            if (camera2D == null)
                camera2D = cameraObj.AddComponent<Camera2DSetup>();
            var camSo = new SerializedObject(camera2D);
            camSo.FindProperty("orthographicSize").floatValue = 10f;
            camSo.FindProperty("followEnabled").boolValue = true;
            camSo.FindProperty("useBounds").boolValue = true;
            camSo.FindProperty("boundsMax").vector2Value = new Vector2(200f, 160f);
            camSo.ApplyModifiedProperties();

            // Создать источник света
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1f;

            // Сохранить сцену
            EditorSceneManager.SaveScene(scene, SCENE_PATH);
            
            Debug.Log(string.Format(MSG_SCENE_CREATED, SCENE_PATH));
            Debug.Log("NEXT STEPS:");
            foreach (var step in NEXT_STEPS)
            {
                Debug.Log(step);
            }
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
            var locationText = CreateText(hud, "LocationText", UI_LOCATION_NAME, new Vector2(10, -10), 22, TextAnchor.UpperLeft);
            locationText.fontStyle = FontStyles.Bold;
            
            // Position Text
            var posText = CreateText(hud, "PositionText", string.Format(UI_POSITION_FORMAT, 0, 0), new Vector2(10, -38), 16, TextAnchor.UpperLeft);
            posText.color = new Color(0.7f, 0.7f, 0.8f);
            
            // HP Bar
            CreateSlider(hud, "HealthBar", new Vector2(10, -70), 280, 20, new Color(0.8f, 0.2f, 0.2f));
            var hpText = CreateText(hud, "HealthText", string.Format(UI_HP_FORMAT, 100, 100), new Vector2(10, -95), 16, TextAnchor.UpperLeft);
            
            // Qi Bar
            CreateSlider(hud, "QiBar", new Vector2(10, -120), 280, 20, new Color(0.2f, 0.5f, 0.9f));
            var qiText = CreateText(hud, "QiText", string.Format(UI_QI_FORMAT, 1000, 1000), new Vector2(10, -145), 16, TextAnchor.UpperLeft);
            
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
            
            var instrText = CreateText(instructions, "Text", UI_INSTRUCTIONS, new Vector2(15, -10), 16, TextAnchor.UpperLeft);
            
            Debug.Log(MSG_UI_CREATED);
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
            
            Debug.Log(MSG_CONTROLLER_CREATED);
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
