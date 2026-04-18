// ============================================================================
// Phase07UI.cs — Фаза 07: UI (Canvas, HUD, EventSystem)
// Cultivation World Simulator
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase07UI : IScenePhase
    {
        public string Name => "UI";
        public string MenuPath => "Phase 07: UI (Canvas, HUD, EventSystem)";
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
            CreateHUDPanel(canvasGO);

            Undo.RegisterCreatedObjectUndo(canvasGO, "Create GameUI");
            Debug.Log("[Phase07] UI created");
        }

        private void CreateHUDPanel(GameObject canvas)
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
        }
    }
}
#endif
