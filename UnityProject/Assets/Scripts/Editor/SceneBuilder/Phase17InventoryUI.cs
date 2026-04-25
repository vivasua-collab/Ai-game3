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
// Зависимости: Phase07UI (Canvas + UIManager), Phase16InventoryData (ассеты)
//
// Редактировано: 2026-04-25 10:30:00 UTC — FIX: Подключение InventoryScreen
//   к UIManager (inventoryPanel + inventoryScreen ссылки). Без этого клавиша I
//   не отображает инвентарь. Также подключены внутренние ссылки InventoryScreen.
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using CultivationGame.UI;
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
            var canvas = GameObject.Find("GameUI");
            if (canvas == null) return false;
            return canvas.transform.Find("InventoryScreen") == null;
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            var canvas = GameObject.Find("GameUI");
            if (canvas == null)
            {
                Debug.LogError("[Phase17] Canvas 'GameUI' не найден! Сначала выполните Phase 07.");
                return;
            }

            if (canvas.transform.Find("InventoryScreen") != null)
            {
                Debug.Log("[Phase17] InventoryScreen уже существует — пропускаем");
                return;
            }

            // Создать InventoryScreen (скрыт по умолчанию)
            GameObject inventoryScreenGO = new GameObject("InventoryScreen");
            inventoryScreenGO.transform.SetParent(canvas.transform, false);

            var screenRect = inventoryScreenGO.AddComponent<RectTransform>();
            screenRect.anchorMin = Vector2.zero;
            screenRect.anchorMax = Vector2.one;
            screenRect.offsetMin = Vector2.zero;
            screenRect.offsetMax = Vector2.zero;

            // InventoryScreen component
            var inventoryScreenComp = inventoryScreenGO.AddComponent<InventoryScreen>();

            // Background overlay
            CreateBackgroundOverlay(inventoryScreenGO);

            // Main panel
            GameObject mainPanel = CreateMainPanel(inventoryScreenGO);

            // Header — получаем ссылки на closeButton, sortButton, titleText
            CreateHeader(mainPanel, out var closeButton, out var sortButton, out var titleText);

            // Content area — BodyDoll + Backpack (+ StorageRingPanel)
            CreateContentArea(mainPanel, out var bodyDollPanel, out var backpackPanel, out var storageRingPanel);

            // SpiritStorage panel (скрыта)
            CreateSpiritStoragePanel(mainPanel);

            // Belt panel
            CreateBeltPanel(mainPanel, out var beltContainer);

            // Tab bar — получаем ссылки на кнопки вкладок
            CreateTabBar(mainPanel, out var backpackTab, out var spiritStorageTab, out var storageRingTab);

            // Tooltip panel
            CreateTooltipPanel(inventoryScreenGO, out var tooltipPanel);

            // DragDrop layer
            CreateDragDropLayer(inventoryScreenGO, out var dragDropHandler);

            // Context menu
            CreateContextMenu(inventoryScreenGO);

            // Скрыть по умолчанию
            inventoryScreenGO.SetActive(false);

            // === Подключение внутренних ссылок InventoryScreen ===
            WireInventoryScreenReferences(inventoryScreenComp, backpackPanel, bodyDollPanel,
                dragDropHandler, tooltipPanel, storageRingPanel, beltContainer,
                closeButton, sortButton, titleText,
                backpackTab, spiritStorageTab, storageRingTab);

            // === Подключение InventoryScreen к UIManager ===
            WireUIManagerInventoryReferences(canvas, inventoryScreenGO, inventoryScreenComp);

            Undo.RegisterCreatedObjectUndo(inventoryScreenGO, "Create InventoryScreen");
            Debug.Log("[Phase17] ✅ InventoryScreen создан и подключён к UIManager");
        }

        // ====================================================================
        //  Wiring
        // ====================================================================

        private void WireInventoryScreenReferences(
            InventoryScreen screen,
            BackpackPanel backpackPanel, BodyDollPanel bodyDollPanel,
            DragDropHandler dragDropHandler, TooltipPanel tooltipPanel,
            StorageRingPanel storageRingPanel, Transform beltContainer,
            UnityEngine.UI.Button closeButton, UnityEngine.UI.Button sortButton,
            TMPro.TMP_Text titleText,
            UnityEngine.UI.Button backpackTab, UnityEngine.UI.Button spiritStorageTab,
            UnityEngine.UI.Button storageRingTab)
        {
            SerializedObject so = new SerializedObject(screen);

            so.FindProperty("backpackPanel").objectReferenceValue = backpackPanel;
            so.FindProperty("bodyDollPanel").objectReferenceValue = bodyDollPanel;
            so.FindProperty("dragDropHandler").objectReferenceValue = dragDropHandler;
            so.FindProperty("tooltipPanel").objectReferenceValue = tooltipPanel;

            if (storageRingPanel != null)
                so.FindProperty("storageRingPanel").objectReferenceValue = storageRingPanel;

            if (beltContainer != null)
                so.FindProperty("beltContainer").objectReferenceValue = beltContainer;

            if (closeButton != null)
                so.FindProperty("closeButton").objectReferenceValue = closeButton;

            if (sortButton != null)
                so.FindProperty("sortButton").objectReferenceValue = sortButton;

            if (titleText != null)
                so.FindProperty("titleText").objectReferenceValue = titleText;

            if (backpackTab != null)
                so.FindProperty("backpackTab").objectReferenceValue = backpackTab;

            if (spiritStorageTab != null)
                so.FindProperty("spiritStorageTab").objectReferenceValue = spiritStorageTab;

            if (storageRingTab != null)
                so.FindProperty("storageRingTab").objectReferenceValue = storageRingTab;

            so.ApplyModifiedProperties();
            Debug.Log("[Phase17] InventoryScreen внутренние ссылки подключены");
        }

        private void WireUIManagerInventoryReferences(GameObject canvas,
            GameObject inventoryScreenGO, InventoryScreen inventoryScreenComp)
        {
            var uiManager = canvas.GetComponent<UIManager>();
            if (uiManager == null)
            {
                Debug.LogWarning("[Phase17] UIManager не найден на Canvas! Выполните Phase 07.");
                return;
            }

            SerializedObject so = new SerializedObject(uiManager);
            so.FindProperty("inventoryPanel").objectReferenceValue = inventoryScreenGO;
            so.FindProperty("inventoryScreen").objectReferenceValue = inventoryScreenComp;
            so.ApplyModifiedProperties();

            Debug.Log("[Phase17] UIManager.inventoryPanel + inventoryScreen подключены");
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

        private void CreateHeader(GameObject parent,
            out UnityEngine.UI.Button closeButtonOut,
            out UnityEngine.UI.Button sortButtonOut,
            out TMPro.TMP_Text titleTextOut)
        {
            closeButtonOut = null;
            sortButtonOut = null;
            titleTextOut = null;

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

            // Title text (TMP)
            titleTextOut = SceneBuilderUtils.CreateTMPText(header, "Title", "ИНВЕНТАРЬ",
                new Vector2(10, 0), 18, TMPro.FontStyles.Bold, new Color(0.9f, 0.85f, 0.7f));

            // Sort button
            GameObject sortBtn = new GameObject("SortButton");
            sortBtn.transform.SetParent(header.transform, false);
            var sortRect = sortBtn.AddComponent<RectTransform>();
            sortRect.anchorMin = new Vector2(1f, 0f);
            sortRect.anchorMax = new Vector2(1f, 1f);
            sortRect.pivot = new Vector2(1f, 0.5f);
            sortRect.sizeDelta = new Vector2(80, 30);
            sortRect.anchoredPosition = new Vector2(-50, 0);

            var sortImage = sortBtn.AddComponent<Image>();
            sortImage.color = new Color(0.2f, 0.3f, 0.2f);
            sortButtonOut = sortBtn.AddComponent<Button>();

            var sortLabel = new GameObject("Label");
            sortLabel.transform.SetParent(sortBtn.transform, false);
            var slRect = sortLabel.AddComponent<RectTransform>();
            slRect.anchorMin = Vector2.zero;
            slRect.anchorMax = Vector2.one;
            slRect.offsetMin = Vector2.zero;
            slRect.offsetMax = Vector2.zero;
            var slText = sortLabel.AddComponent<Text>();
            slText.text = "Сорт.";
            slText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            slText.fontSize = 12;
            slText.color = Color.white;
            slText.alignment = TextAnchor.MiddleCenter;

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
            closeButtonOut = closeBtn.AddComponent<Button>();

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
        }

        private void CreateContentArea(GameObject parent,
            out BodyDollPanel bodyDollPanelOut,
            out BackpackPanel backpackPanelOut,
            out StorageRingPanel storageRingPanelOut)
        {
            bodyDollPanelOut = null;
            backpackPanelOut = null;
            storageRingPanelOut = null;

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

            bodyDollPanelOut = dollPanel.AddComponent<BodyDollPanel>();

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

            backpackPanelOut = backpackPanel.AddComponent<BackpackPanel>();

            // StorageRingPanel (скрыта по умолчанию, показывается при выборе вкладки)
            GameObject ringPanel = new GameObject("StorageRingPanel");
            ringPanel.transform.SetParent(content.transform, false);
            var rpRect = ringPanel.AddComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(0.37f, 0f);
            rpRect.anchorMax = new Vector2(1f, 1f);
            rpRect.offsetMin = Vector2.zero;
            rpRect.offsetMax = Vector2.zero;

            var rpImage = ringPanel.AddComponent<Image>();
            rpImage.color = new Color(0.1f, 0.1f, 0.12f, 0.6f);

            storageRingPanelOut = ringPanel.AddComponent<StorageRingPanel>();
            ringPanel.SetActive(false);
        }

        private void CreateSpiritStoragePanel(GameObject parent)
        {
            GameObject spiritPanel = new GameObject("SpiritStoragePanel");
            spiritPanel.transform.SetParent(parent.transform, false);

            var rect = spiritPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.37f, 0.05f);
            rect.anchorMax = new Vector2(0.9f, 0.95f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = spiritPanel.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.12f, 0.6f);

            spiritPanel.SetActive(false);
        }

        private void CreateBeltPanel(GameObject parent, out Transform beltContainerOut)
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

            beltContainerOut = belt.transform;
        }

        private void CreateTabBar(GameObject parent,
            out UnityEngine.UI.Button backpackTabOut,
            out UnityEngine.UI.Button spiritStorageTabOut,
            out UnityEngine.UI.Button storageRingTabOut)
        {
            backpackTabOut = null;
            spiritStorageTabOut = null;
            storageRingTabOut = null;

            GameObject tabBar = new GameObject("TabBar");
            tabBar.transform.SetParent(parent.transform, false);

            var rect = tabBar.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(10, 40);
            rect.offsetMax = new Vector2(-10, 65);

            CreateTabButton(tabBar, "BackpackTab", "Рюкзак", 0, out backpackTabOut);
            CreateTabButton(tabBar, "SpiritStorageTab", "Дух. хранилище", 1, out spiritStorageTabOut);
            CreateTabButton(tabBar, "StorageRingTab", "Кольцо", 2, out storageRingTabOut);
        }

        private void CreateTabButton(GameObject parent, string name, string label, int index,
            out UnityEngine.UI.Button buttonOut)
        {
            buttonOut = null;

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

            buttonOut = tab.AddComponent<Button>();

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

        private void CreateTooltipPanel(GameObject parent, out TooltipPanel tooltipPanelOut)
        {
            tooltipPanelOut = null;

            GameObject tooltip = new GameObject("TooltipPanel");
            tooltip.transform.SetParent(parent.transform, false);

            var rect = tooltip.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(250, 300);

            var image = tooltip.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            tooltipPanelOut = tooltip.AddComponent<TooltipPanel>();
            tooltip.SetActive(false);
        }

        private void CreateDragDropLayer(GameObject parent, out DragDropHandler dragDropOut)
        {
            dragDropOut = null;

            GameObject dragLayer = new GameObject("DragDropLayer");
            dragLayer.transform.SetParent(parent.transform, false);

            var rect = dragLayer.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            dragDropOut = dragLayer.AddComponent<DragDropHandler>();
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
