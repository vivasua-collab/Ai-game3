// ============================================================================
// Phase17InventoryUI.cs — Фаза 17: UI инвентаря
// Cultivation World Simulator
// ============================================================================
// Создаёт GameObject InventoryScreen в GameUI Canvas со всеми панелями:
// - BodyDollPanel (7 видимых слотов экипировки)
// - BackpackPanel (динамическая Diablo-style сетка)
// - TooltipPanel (карточка предмета с volume/allowNesting)
// - DragDropHandler (перетаскивание между зонами)
// - StorageRingPanel (каталогизатор кольца)
// - TabBar (переключение Рюкзак / Дух. хранилище / Кольцо)
//
// Зависимости: Phase07UI (Canvas), Phase16InventoryData (ассеты)
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using CultivationGame.UI.Inventory;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase17InventoryUI : IScenePhase
    {
        public string Name => "Inventory UI";
        public string MenuPath => "Phase 17: Inventory UI";
        public int Order => 17;

        public bool IsNeeded()
        {
            SceneBuilderUtils.EnsureSceneOpen();
            // Проверяем наличие InventoryScreen в сцене
            var canvas = GameObject.Find("GameUI");
            if (canvas == null) return false;
            return canvas.transform.Find("InventoryScreen") == null;
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            // Найти Canvas
            var canvas = GameObject.Find("GameUI");
            if (canvas == null)
            {
                Debug.LogError("[Phase17] Canvas 'GameUI' не найден! Сначала выполните Phase 07.");
                return;
            }

            // Проверить, не существует ли уже
            if (canvas.transform.Find("InventoryScreen") != null)
            {
                Debug.Log("[Phase17] InventoryScreen уже существует — пропускаем");
                return;
            }

            // Создать InventoryScreen (скрыт по умолчанию)
            GameObject inventoryScreen = new GameObject("InventoryScreen");
            inventoryScreen.transform.SetParent(canvas.transform, false);

            var screenRect = inventoryScreen.AddComponent<RectTransform>();
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;

            // InventoryScreen component
            var inventoryScreenComp = inventoryScreen.AddComponent<InventoryScreen>();

            // Background overlay (затемнение фона)
            CreateBackgroundOverlay(inventoryScreen);

            // Main panel (центральная панель)
            GameObject mainPanel = CreateMainPanel(inventoryScreen);

            // Header
            CreateHeader(mainPanel);

            // Content area — BodyDoll + Backpack
            CreateContentArea(mainPanel);

            // Belt panel
            CreateBeltPanel(mainPanel);

            // Tab bar
            CreateTabBar(mainPanel);

            // Tooltip panel
            CreateTooltipPanel(inventoryScreen);

            // DragDrop layer
            CreateDragDropLayer(inventoryScreen);

            // Context menu
            CreateContextMenu(inventoryScreen);

            // Скрыть по умолчанию
            inventoryScreen.SetActive(false);

            Undo.RegisterCreatedObjectUndo(inventoryScreen, "Create InventoryScreen");
            Debug.Log("[Phase17] ✅ InventoryScreen создан с панелями: BodyDoll, Backpack, Tooltip, DragDrop, ContextMenu, TabBar");
        }

        // ====================================================================
        //  UI Element Creation Helpers
        // ====================================================================

        private void CreateBackgroundOverlay(GameObject parent)
        {
            GameObject bg = new GameObject("BackgroundOverlay");
            bg.transform.SetParent(parent.transform, false);

            var rect = bg.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = bg.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.5f);
        }

        private GameObject CreateMainPanel(GameObject parent)
        {
            GameObject panel = new GameObject("MainPanel");
            panel.transform.SetParent(parent.transform, false);

            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.1f, 0.05f);
            rect.anchorMax = new Vector2(0.9f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = panel.AddComponent<Image>();
            image.color = new Color(0.12f, 0.12f, 0.15f, 0.95f);

            return panel;
        }

        private void CreateHeader(GameObject parent)
        {
            GameObject header = new GameObject("Header");
            header.transform.SetParent(parent.transform, false);

            var rect = header.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(0, -40);
            rect.offsetMax = Vector2.zero;

            var image = header.AddComponent<Image>();
            image.color = new Color(0.15f, 0.12f, 0.1f, 1f);

            // Title text
            GameObject title = new GameObject("Title");
            title.transform.SetParent(header.transform, false);
            var titleRect = title.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 0f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(10, 0);
            titleRect.offsetMax = new Vector2(-60, 0);

            var titleText = title.AddComponent<Text>();
            titleText.text = "ИНВЕНТАРЬ";
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 18;
            titleText.color = new Color(0.9f, 0.85f, 0.7f);
            titleText.alignment = TextAnchor.MiddleLeft;

            // Close button
            GameObject closeBtn = new GameObject("CloseButton");
            closeBtn.transform.SetParent(header.transform, false);
            var btnRect = closeBtn.AddComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(1f, 0f);
            btnRect.anchorMax = new Vector2(1f, 1f);
            btnRect.pivot = new Vector2(1f, 0.5f);
            btnRect.sizeDelta = new Vector2(40, 40);
            btnRect.anchoredPosition = new Vector2(-5, 0);

            var btnImage = closeBtn.AddComponent<Image>();
            btnImage.color = new Color(0.6f, 0.2f, 0.2f);

            var btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(closeBtn.transform, false);
            var btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
            var btnText = btnTextObj.AddComponent<Text>();
            btnText.text = "✕";
            btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            btnText.fontSize = 16;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;

            closeBtn.AddComponent<Button>();
        }

        private void CreateContentArea(GameObject parent)
        {
            GameObject content = new GameObject("ContentArea");
            content.transform.SetParent(parent.transform, false);

            var rect = content.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.offsetMin = new Vector2(10, 50);
            rect.offsetMax = new Vector2(-10, -50);

            // BodyDollPanel (левая часть)
            GameObject dollPanel = new GameObject("BodyDollPanel");
            dollPanel.transform.SetParent(content.transform, false);
            var dollRect = dollPanel.AddComponent<RectTransform>();
            dollRect.anchorMin = new Vector2(0f, 0f);
            dollRect.anchorMax = new Vector2(0.35f, 1f);
            dollRect.offsetMin = Vector2.zero;
            dollRect.offsetMax = Vector2.zero;

            var dollImage = dollPanel.AddComponent<Image>();
            dollImage.color = new Color(0.1f, 0.1f, 0.12f, 0.8f);

            // BodyDollPanel component
            dollPanel.AddComponent<BodyDollPanel>();

            // BackpackPanel (правая часть)
            GameObject backpackPanel = new GameObject("BackpackPanel");
            backpackPanel.transform.SetParent(content.transform, false);
            var bpRect = backpackPanel.AddComponent<RectTransform>();
            bpRect.anchorMin = new Vector2(0.37f, 0f);
            bpRect.anchorMax = new Vector2(1f, 1f);
            bpRect.offsetMin = Vector2.zero;
            bpRect.offsetMax = Vector2.zero;

            var bpImage = backpackPanel.AddComponent<Image>();
            bpImage.color = new Color(0.1f, 0.1f, 0.12f, 0.6f);

            // BackpackPanel component
            backpackPanel.AddComponent<BackpackPanel>();
        }

        private void CreateBeltPanel(GameObject parent)
        {
            GameObject belt = new GameObject("BeltPanel");
            belt.transform.SetParent(parent.transform, false);

            var rect = belt.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(10, 5);
            rect.offsetMax = new Vector2(-10, 40);

            var image = belt.AddComponent<Image>();
            image.color = new Color(0.08f, 0.08f, 0.1f, 0.9f);
        }

        private void CreateTabBar(GameObject parent)
        {
            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(parent.transform, false);

            var rect = tabBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(10, 40);
            rect.offsetMax = new Vector2(-10, 65);

            // Tab: Backpack
            CreateTabButton(tabBar, "BackpackTab", "Рюкзак", 0);
            // Tab: SpiritStorage
            CreateTabButton(tabBar, "SpiritStorageTab", "Дух. хранилище", 1);
            // Tab: StorageRing
            CreateTabButton(tabBar, "StorageRingTab", "Кольцо", 2);
        }

        private void CreateTabButton(GameObject parent, string name, string label, int index)
        {
            GameObject tab = new GameObject(name);
            tab.transform.SetParent(parent.transform, false);

            float width = 1f / 3f;
            var rect = tab.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(index * width, 0f);
            rect.anchorMax = new Vector2((index + 1) * width, 1f);
            rect.offsetMin = new Vector2(2, 0);
            rect.offsetMax = new Vector2(-2, 0);

            var image = tab.AddComponent<Image>();
            image.color = index == 0
                ? new Color(0.2f, 0.18f, 0.15f, 1f)
                : new Color(0.12f, 0.12f, 0.14f, 1f);

            var btn = tab.AddComponent<Button>();

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(tab.transform, false);
            var labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var labelText = labelObj.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.color = new Color(0.85f, 0.8f, 0.7f);
            labelText.alignment = TextAnchor.MiddleCenter;
        }

        private void CreateTooltipPanel(GameObject parent)
        {
            GameObject tooltip = new GameObject("TooltipPanel");
            tooltip.transform.SetParent(parent.transform, false);

            var rect = tooltip.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(250, 300);

            var image = tooltip.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            // TooltipPanel component
            tooltip.AddComponent<TooltipPanel>();

            tooltip.SetActive(false);
        }

        private void CreateDragDropLayer(GameObject parent)
        {
            GameObject dragLayer = new GameObject("DragDropLayer");
            dragLayer.transform.SetParent(parent.transform, false);

            var rect = dragLayer.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // DragDropHandler component
            dragLayer.AddComponent<DragDropHandler>();
        }

        private void CreateContextMenu(GameObject parent)
        {
            GameObject contextMenu = new GameObject("ContextMenu");
            contextMenu.transform.SetParent(parent.transform, false);

            var rect = contextMenu.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(160, 120);

            var image = contextMenu.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            contextMenu.SetActive(false);
        }
    }
}
#endif
