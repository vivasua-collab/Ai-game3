// ============================================================================
// Phase07UI.cs — Фаза 07: UI (Canvas, HUD, EventSystem, UIManager)
// Cultivation World Simulator
// ============================================================================
// Редактировано: 2026-04-25 10:25:00 UTC — FIX: Добавлен UIManager, MenuUI,
//   подключение SerializedField ссылок. Без UIManager клавиша I не работает.
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using CultivationGame.UI;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase07UI : IScenePhase
    {
        public string Name => "UI";
        public string MenuPath => "Phase 07: UI (Canvas, HUD, EventSystem, UIManager)";
        public int Order => 7;

        public bool IsNeeded()
        {
            SceneBuilderUtils.EnsureSceneOpen();
            return GameObject.Find("GameUI") == null;
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            if (GameObject.Find("GameUI") != null)
            {
                Debug.Log("[Phase07] UI already exists");
                return;
            }

            // Canvas
            GameObject canvasGO = new GameObject("GameUI");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // UIManager — главный менеджер UI (singleton, обрабатывает клавишу I)
            var uiManager = canvasGO.AddComponent<UIManager>();

            // EventSystem
            var eventSystem = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventSystemGO = new GameObject("EventSystem");
                eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();

                var inputModuleType = System.Type.GetType(
                    "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                    eventSystemGO.AddComponent(inputModuleType);

                Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");
            }

            // HUD Panel
            GameObject hud = CreateHUDPanel(canvasGO);

            // MainMenu Panel (скрыта по умолчанию)
            GameObject mainMenu = CreateMainMenuPanel(canvasGO);

            // PauseMenu Panel (скрыта по умолчанию)
            GameObject pauseMenu = CreatePauseMenuPanel(canvasGO);

            // Подключаем SerializedField ссылки UIManager
            WireUIManagerReferences(uiManager, canvas, hud, mainMenu, pauseMenu);

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create GameUI");
            Debug.Log("[Phase07] ✅ UI создан: Canvas + UIManager + HUD + MainMenu + PauseMenu");
        }

        // ====================================================================
        //  HUD Panel
        // ====================================================================

        private GameObject CreateHUDPanel(GameObject canvas)
        {
            GameObject hud = new GameObject("HUD");
            hud.transform.SetParent(canvas.transform, false);

            RectTransform hudRect = hud.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0, 1);
            hudRect.anchorMax = new Vector2(0, 1);
            hudRect.pivot = new Vector2(0, 1);
            hudRect.anchoredPosition = new Vector2(10, -10);
            hudRect.sizeDelta = new Vector2(320, 200);

            Image hudImage = hud.AddComponent<Image>();
            hudImage.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            // HUDController component
            hud.AddComponent<HUDController>();

            SceneBuilderUtils.CreateTMPText(hud, "LocationText", "Cultivation World", new Vector2(10, -10), 22,
                TMPro.FontStyles.Bold, Color.white);
            SceneBuilderUtils.CreateTMPText(hud, "TimeText", "День 1 — 06:00", new Vector2(10, -38), 18,
                TMPro.FontStyles.Normal, new Color(0.8f, 0.9f, 1f));

            SceneBuilderUtils.CreateBar(hud, "HealthBar", new Vector2(10, -70), 280, 20, new Color(0.8f, 0.2f, 0.2f));
            SceneBuilderUtils.CreateTMPText(hud, "HealthText", "HP: 100/100", new Vector2(10, -95), 16,
                TMPro.FontStyles.Normal, Color.white);

            SceneBuilderUtils.CreateBar(hud, "QiBar", new Vector2(10, -120), 280, 20, new Color(0.2f, 0.5f, 0.9f));
            SceneBuilderUtils.CreateTMPText(hud, "QiText", "Ци: 100/100", new Vector2(10, -145), 16,
                TMPro.FontStyles.Normal, Color.white);

            SceneBuilderUtils.CreateBar(hud, "StaminaBar", new Vector2(10, -170), 280, 16, new Color(0.2f, 0.8f, 0.3f));

            return hud;
        }

        // ====================================================================
        //  Main Menu Panel (скрыта)
        // ====================================================================

        private GameObject CreateMainMenuPanel(GameObject canvas)
        {
            GameObject mainMenu = new GameObject("MainMenu");
            mainMenu.transform.SetParent(canvas.transform, false);

            var rect = mainMenu.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = mainMenu.AddComponent<Image>();
            image.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

            // MenuUI component
            var menuUI = mainMenu.AddComponent<MenuUI>();

            // Центральный контейнер кнопок
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(mainMenu.transform, false);
            var containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.35f, 0.3f);
            containerRect.anchorMax = new Vector2(0.65f, 0.7f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            // Заголовок
            SceneBuilderUtils.CreateTMPText(buttonContainer, "TitleText", " Cultivation World", new Vector2(0, -10), 36,
                TMPro.FontStyles.Bold, new Color(0.9f, 0.85f, 0.7f));

            // Кнопки
            CreateMenuButton(buttonContainer, "NewGameButton", "Новая игра", -80, out var newGameBtn);
            CreateMenuButton(buttonContainer, "ContinueButton", "Продолжить", -120, out var continueBtn);
            CreateMenuButton(buttonContainer, "LoadGameButton", "Загрузить", -160, out var loadGameBtn);
            CreateMenuButton(buttonContainer, "SettingsButton", "Настройки", -200, out var settingsBtn);
            CreateMenuButton(buttonContainer, "QuitButton", "Выход", -240, out var quitBtn);

            // Подключаем MenuUI ссылки
            SerializedObject menuSo = new SerializedObject(menuUI);
            menuSo.FindProperty("uiManager").objectReferenceValue = canvas.GetComponent<UIManager>();
            menuSo.FindProperty("newGameButton").objectReferenceValue = newGameBtn;
            menuSo.FindProperty("continueButton").objectReferenceValue = continueBtn;
            menuSo.FindProperty("loadGameButton").objectReferenceValue = loadGameBtn;
            menuSo.FindProperty("settingsButton").objectReferenceValue = settingsBtn;
            menuSo.FindProperty("quitButton").objectReferenceValue = quitBtn;
            menuSo.ApplyModifiedProperties();

            // Скрыта по умолчанию (UIManager.ForceInitialSync включает MainMenu)
            mainMenu.SetActive(true);

            return mainMenu;
        }

        // ====================================================================
        //  Pause Menu Panel (скрыта)
        // ====================================================================

        private GameObject CreatePauseMenuPanel(GameObject canvas)
        {
            GameObject pauseMenu = new GameObject("PauseMenu");
            pauseMenu.transform.SetParent(canvas.transform, false);

            var rect = pauseMenu.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = pauseMenu.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.7f);

            // Центральный контейнер
            GameObject buttonContainer = new GameObject("PauseButtonContainer");
            buttonContainer.transform.SetParent(pauseMenu.transform, false);
            var containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.35f, 0.3f);
            containerRect.anchorMax = new Vector2(0.65f, 0.7f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;

            SceneBuilderUtils.CreateTMPText(buttonContainer, "PauseTitle", "⏸ ПАУЗА", new Vector2(0, -10), 28,
                TMPro.FontStyles.Bold, new Color(0.9f, 0.85f, 0.7f));

            CreateMenuButton(buttonContainer, "ResumeButton", "Продолжить", -70, out var resumeBtn);
            CreateMenuButton(buttonContainer, "SaveButton", "Сохранить", -110, out var saveBtn);
            CreateMenuButton(buttonContainer, "LoadButton", "Загрузить", -150, out var loadBtn);
            CreateMenuButton(buttonContainer, "PauseMainMenuButton", "Главное меню", -190, out var mainMenuBtn);

            // Скрыта по умолчанию
            pauseMenu.SetActive(false);

            return pauseMenu;
        }

        // ====================================================================
        //  UI Wiring
        // ====================================================================

        private void WireUIManagerReferences(UIManager uiManager, Canvas canvas, GameObject hud,
            GameObject mainMenu, GameObject pauseMenu)
        {
            SerializedObject so = new SerializedObject(uiManager);
            so.FindProperty("mainCanvas").objectReferenceValue = canvas;
            so.FindProperty("hudPanel").objectReferenceValue = hud;
            so.FindProperty("mainMenu").objectReferenceValue = mainMenu;
            so.FindProperty("pauseMenu").objectReferenceValue = pauseMenu;
            // inventoryPanel, inventoryScreen, dialogPanel, characterPanel, mapPanel —
            // подключаются в Phase17 (InventoryScreen)
            so.ApplyModifiedProperties();

            Debug.Log("[Phase07] UIManager ссылки подключены: mainCanvas, hudPanel, mainMenu, pauseMenu");
        }

        // ====================================================================
        //  Helpers
        // ====================================================================

        private void CreateMenuButton(GameObject parent, string name, string label, float yOffset, out Button outButton)
        {
            GameObject btn = new GameObject(name);
            btn.transform.SetParent(parent.transform, false);

            var btnRect = btn.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.1f, 1f);
            btnRect.anchorMax = new Vector2(0.9f, 1f);
            btnRect.pivot = new Vector2(0.5f, 1f);
            btnRect.offsetMin = new Vector2(0, yOffset - 35);
            btnRect.offsetMax = new Vector2(0, yOffset);

            var btnImage = btn.AddComponent<Image>();
            btnImage.color = new Color(0.15f, 0.12f, 0.1f, 1f);

            var button = btn.AddComponent<Button>();

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btn.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelText = labelObj.AddComponent<TMPro.TMP_Text>();
            // TMP_Text создаётся через AddComponent — нужен TMP_DefaultControls или ручная настройка
            // Используем обычный Text как фоллбэк
            Object.DestroyImmediate(labelText);
            var fallbackText = labelObj.AddComponent<Text>();
            fallbackText.text = label;
            fallbackText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            fallbackText.fontSize = 18;
            fallbackText.color = new Color(0.85f, 0.8f, 0.7f);
            fallbackText.alignment = TextAnchor.MiddleCenter;

            outButton = button;
        }
    }
}
#endif
