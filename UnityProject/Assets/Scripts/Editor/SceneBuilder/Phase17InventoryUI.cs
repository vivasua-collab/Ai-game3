// ============================================================================
// Phase17InventoryUI.cs — Фаза 17: UI инвентаря
// Cultivation World Simulator
// ============================================================================
// Создаёт GameObject InventoryScreen в GameUI Canvas со всеми панелями:
// - BodyDollPanel (двухколоночная кукла: одежда слева, оружие справа)
// - BackpackPanel (динамическая Diablo-style сетка)
// - TooltipPanel (карточка предмета с volume/allowNesting)
// - DragDropHandler (перетаскивание между зонами)
// - StorageRingPanel (каталогизатор кольца)
// - TabBar (переключение Рюкзак / Дух. хранилище / Кольцо)
//
// Зависимости: Phase07UI (Canvas + UIManager), Phase16InventoryData (ассеты)
//
// Редактировано: 2026-04-25 14:35:00 MSK — BackpackPanel, InventorySlotUI,
//   DragDropHandler, TooltipPanel wiring.
// Редактировано: 2026-04-25 16:00:00 MSK — BodyDollPanel internals.
// Редактировано: 2026-04-25 18:30:00 MSK — РЕДИЗАЙН куклы: двухколоночный
//   layout, квадратные 50×50 слоты, процедурный силуэт тела, ширина 300px.
// Редактировано: 2026-04-27 18:40 UTC — фикс: MiddleRight→MidlineRight, scrollbarVisibility→verticalScrollbarVisibility
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using CultivationGame.UI;
using CultivationGame.UI.Inventory;
using CultivationGame.Core;

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

            // Tooltip panel — создаём внутренние элементы и подключаем ссылки
            CreateTooltipPanel(inventoryScreenGO, out var tooltipPanel);

            // DragDrop layer — создаём внутренние элементы и подключаем ссылки
            CreateDragDropLayer(inventoryScreenGO, out var dragDropHandler, tooltipPanel);

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
        //  Wiring — InventoryScreen
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
        //  Wiring — BackpackPanel
        // ====================================================================

        /// <summary>
        /// Подключает все SerializeField ссылки BackpackPanel через SerializedObject.
        /// </summary>
        private void WireBackpackPanelReferences(
            BackpackPanel panel,
            GameObject slotRowPrefab,
            Transform listContainer,
            TMP_Text backpackNameText,
            TMP_Text weightText,
            Slider weightBar,
            TMP_Text volumeText,
            Slider volumeBar,
            TMP_Text countText)
        {
            SerializedObject so = new SerializedObject(panel);

            so.FindProperty("slotRowPrefab").objectReferenceValue = slotRowPrefab;
            so.FindProperty("listContainer").objectReferenceValue = listContainer;
            so.FindProperty("backpackNameText").objectReferenceValue = backpackNameText;
            so.FindProperty("weightText").objectReferenceValue = weightText;
            so.FindProperty("weightBar").objectReferenceValue = weightBar;
            so.FindProperty("volumeText").objectReferenceValue = volumeText;
            so.FindProperty("volumeBar").objectReferenceValue = volumeBar;
            so.FindProperty("countText").objectReferenceValue = countText;

            so.ApplyModifiedProperties();
            Debug.Log("[Phase17] BackpackPanel SerializeField ссылки подключены (8 полей: строчная модель)");
        }

        // ====================================================================
        //  Wiring — DragDropHandler
        // ====================================================================

        /// <summary>
        /// Подключает все SerializeField ссылки DragDropHandler через SerializedObject.
        /// </summary>
        private void WireDragDropHandlerReferences(
            DragDropHandler handler,
            Image dragIcon,
            RectTransform dragTransform,
            GameObject contextMenuPrefab,
            Transform contextMenuContainer,
            TooltipPanel tooltipPanel)
        {
            SerializedObject so = new SerializedObject(handler);

            so.FindProperty("dragIcon").objectReferenceValue = dragIcon;
            so.FindProperty("dragTransform").objectReferenceValue = dragTransform;
            so.FindProperty("contextMenuPrefab").objectReferenceValue = contextMenuPrefab;
            so.FindProperty("contextMenuContainer").objectReferenceValue = contextMenuContainer;
            so.FindProperty("tooltipPanel").objectReferenceValue = tooltipPanel;

            so.ApplyModifiedProperties();
            Debug.Log("[Phase17] DragDropHandler SerializeField ссылки подключены (5 полей)");
        }

        // ====================================================================
        //  Wiring — TooltipPanel
        // ====================================================================

        /// <summary>
        /// Подключает все SerializeField ссылки TooltipPanel через SerializedObject.
        /// </summary>
        private void WireTooltipPanelReferences(
            TooltipPanel panel,
            Image rarityBorder,
            TMP_Text nameText,
            TMP_Text rarityText,
            TMP_Text typeText,
            GameObject combatSection,
            TMP_Text damageText,
            TMP_Text defenseText,
            TMP_Text coverageText,
            TMP_Text damageReductionText,
            TMP_Text dodgeBonusText,
            GameObject physicalSection,
            TMP_Text weightText,
            TMP_Text volumeText,
            TMP_Text nestingText,
            TMP_Text durabilityText,
            GameObject bonusesSection,
            TMP_Text bonusesText,
            GameObject materialSection,
            TMP_Text materialText,
            TMP_Text gradeText,
            GameObject requirementsSection,
            TMP_Text requirementsText,
            TMP_Text descriptionText,
            TMP_Text valueText)
        {
            SerializedObject so = new SerializedObject(panel);

            // Заголовок
            so.FindProperty("rarityBorder").objectReferenceValue = rarityBorder;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("rarityText").objectReferenceValue = rarityText;
            so.FindProperty("typeText").objectReferenceValue = typeText;

            // Боевой раздел
            so.FindProperty("combatSection").objectReferenceValue = combatSection;
            so.FindProperty("damageText").objectReferenceValue = damageText;
            so.FindProperty("defenseText").objectReferenceValue = defenseText;
            so.FindProperty("coverageText").objectReferenceValue = coverageText;
            so.FindProperty("damageReductionText").objectReferenceValue = damageReductionText;
            so.FindProperty("dodgeBonusText").objectReferenceValue = dodgeBonusText;

            // Физический раздел
            so.FindProperty("physicalSection").objectReferenceValue = physicalSection;
            so.FindProperty("weightText").objectReferenceValue = weightText;
            so.FindProperty("volumeText").objectReferenceValue = volumeText;
            so.FindProperty("nestingText").objectReferenceValue = nestingText;
            so.FindProperty("durabilityText").objectReferenceValue = durabilityText;

            // Бонусы
            so.FindProperty("bonusesSection").objectReferenceValue = bonusesSection;
            so.FindProperty("bonusesText").objectReferenceValue = bonusesText;

            // Материал / Грейд
            so.FindProperty("materialSection").objectReferenceValue = materialSection;
            so.FindProperty("materialText").objectReferenceValue = materialText;
            so.FindProperty("gradeText").objectReferenceValue = gradeText;

            // Требования
            so.FindProperty("requirementsSection").objectReferenceValue = requirementsSection;
            so.FindProperty("requirementsText").objectReferenceValue = requirementsText;

            // Описание
            so.FindProperty("descriptionText").objectReferenceValue = descriptionText;

            // Стоимость
            so.FindProperty("valueText").objectReferenceValue = valueText;

            so.ApplyModifiedProperties();
            Debug.Log("[Phase17] TooltipPanel SerializeField ссылки подключены (24 поля)");
        }

        // ====================================================================
        //  SlotUI Prefab Creation
        // ====================================================================

        /// <summary>
        /// Создаёт префаб InventorySlotUI — горизонтальная строка (строчная модель v3.0).
        /// Строка: иконка + название + количество + вес + объём.
        /// Префаб НЕАКТИВЕН (шаблон для Instantiate).
        /// </summary>
        private GameObject CreateSlotUIPrefab()
        {
            // Корневой объект — SlotRowPrefab
            GameObject slotGO = new GameObject("SlotRowPrefab");
            var slotRect = slotGO.AddComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.sizeDelta = new Vector2(0f, 28f);

            // Фон (background) — тёмно-синий
            var bgImage = slotGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.18f, 0.8f);
            bgImage.raycastTarget = true;

            // HorizontalLayoutGroup для строки
            var hLayout = slotGO.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 4f;
            hLayout.padding = new RectOffset(2, 2, 2, 2);
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            // Компонент InventorySlotUI
            var slotUI = slotGO.AddComponent<InventorySlotUI>();

            // --- Дочерние объекты для каждого SerializeField ---

            // Icon (иконка предмета) → iconImage
            GameObject iconGO = new GameObject("Icon");
            iconGO.transform.SetParent(slotGO.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(24f, 24f);
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;

            // NameText → nameText
            var nameTMP = SceneBuilderUtils.CreateTMPText(slotGO, "NameText", "",
                new Vector2(0, 0), 11, FontStyles.Normal, Color.white);
            var nameRect = nameTMP.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(100f, 20f);
            nameTMP.alignment = TextAlignmentOptions.Left;

            // CountText → countText
            var countTMP = SceneBuilderUtils.CreateTMPText(slotGO, "CountText", "",
                new Vector2(0, 0), 11, FontStyles.Bold, Color.white);
            var countRect = countTMP.GetComponent<RectTransform>();
            countRect.sizeDelta = new Vector2(40f, 20f);
            countTMP.alignment = TextAlignmentOptions.MidlineRight;

            // WeightText → weightText
            var weightTMP = SceneBuilderUtils.CreateTMPText(slotGO, "WeightText", "",
                new Vector2(0, 0), 10, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));
            var weightTMPRect = weightTMP.GetComponent<RectTransform>();
            weightTMPRect.sizeDelta = new Vector2(50f, 18f);
            weightTMP.alignment = TextAlignmentOptions.MidlineRight;

            // VolumeText → volumeText
            var volumeTMP = SceneBuilderUtils.CreateTMPText(slotGO, "VolumeText", "",
                new Vector2(0, 0), 10, FontStyles.Normal, new Color(0.5f, 0.7f, 0.9f));
            var volumeTMPRect = volumeTMP.GetComponent<RectTransform>();
            volumeTMPRect.sizeDelta = new Vector2(50f, 18f);
            volumeTMP.alignment = TextAlignmentOptions.MidlineRight;

            // --- Подключение SerializeField ссылок InventorySlotUI ---
            SerializedObject so = new SerializedObject(slotUI);

            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            so.FindProperty("nameText").objectReferenceValue = nameTMP;
            so.FindProperty("countText").objectReferenceValue = countTMP;
            so.FindProperty("weightText").objectReferenceValue = weightTMP;
            so.FindProperty("volumeText").objectReferenceValue = volumeTMP;

            so.ApplyModifiedProperties();

            // Префаб НЕАКТИВЕН — шаблон для Instantiate
            slotGO.SetActive(false);

            Debug.Log("[Phase17] SlotRowPrefab создан (InventorySlotUI v3.0: строка с 5 полями)");
            return slotGO;
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

            // ================================================================
            // BodyDollPanel (левая часть) — ДВУХКОЛОНОЧНЫЙ LAYOUT v2
            // Редактировано: 2026-04-25 18:30:00 MSK
            // ================================================================
            // Layout: Силуэт тела по центру, квадратные слоты 50×50 по бокам
            // Левая колонка: одежда (Head, Torso, Belt, Legs, Feet)
            // Правая колонка: оружие (WeaponMain, WeaponOff), кольца (скрыты)

            GameObject dollPanel = new GameObject("BodyDollPanel");
            dollPanel.transform.SetParent(content.transform, false);
            var dollRect = dollPanel.AddComponent<RectTransform>();
            dollRect.anchorMin = new Vector2(0f, 0f);
            dollRect.anchorMax = new Vector2(0.40f, 1f); // Было 0.35 → 0.40
            dollRect.offsetMin = Vector2.zero;
            dollRect.offsetMax = Vector2.zero;

            var dollImage = dollPanel.AddComponent<Image>();
            dollImage.color = new Color(34f/255f, 34f/255f, 51f/255f, 1f);

            bodyDollPanelOut = dollPanel.AddComponent<BodyDollPanel>();

            // Заголовок «КУКЛА»
            var titleTMP = SceneBuilderUtils.CreateTMPText(dollPanel, "TitleText", "КУКЛА",
                new Vector2(0, 0), 14, FontStyles.Bold, new Color(0.85f, 0.8f, 0.7f));
            var titleRect = titleTMP.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -4f);
            titleRect.sizeDelta = new Vector2(0f, 20f);
            titleTMP.alignment = TextAlignmentOptions.Center;

            // --- Силуэт тела (центр панели) ---
            // Процедурно сгенерированный спрайт 80×220
            GameObject silhouetteGO = new GameObject("BodySilhouette");
            silhouetteGO.transform.SetParent(dollPanel.transform, false);
            var silhouetteRect = silhouetteGO.AddComponent<RectTransform>();
            silhouetteRect.anchorMin = new Vector2(0.5f, 1f);
            silhouetteRect.anchorMax = new Vector2(0.5f, 1f);
            silhouetteRect.pivot = new Vector2(0.5f, 1f);
            silhouetteRect.anchoredPosition = new Vector2(0f, -28f);
            silhouetteRect.sizeDelta = new Vector2(80f, 220f);
            var bodySilhouette = silhouetteGO.AddComponent<Image>();
            bodySilhouette.color = new Color(0.18f, 0.18f, 0.25f, 1f);
            bodySilhouette.raycastTarget = false;
            // Попытка загрузить процедурный спрайт
            var bodySprite = GenerateBodySilhouetteSprite();
            if (bodySprite != null)
            {
                bodySilhouette.sprite = bodySprite;
                bodySilhouette.color = new Color(0.35f, 0.35f, 0.5f, 1f);
            }

            // === ЛЕВАЯ КОЛОНКА — Экипировка одежды ===
            // Слоты 50×50, x anchored к левому краю
            // Позиции Y: anatomically aligned with body silhouette
            // Head: y=-30, Torso: y=-85, Belt: y=-140, Legs: y=-195, Feet: y=-250

            var headSlot = CreateDollSlot(dollPanel, "Head", EquipmentSlot.Head,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(4f, -30f), new Vector2(50f, 50f));
            var torsoSlot = CreateDollSlot(dollPanel, "Torso", EquipmentSlot.Torso,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(4f, -85f), new Vector2(50f, 50f));
            var beltSlot = CreateDollSlot(dollPanel, "Belt", EquipmentSlot.Belt,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(4f, -140f), new Vector2(50f, 50f));
            var legsSlot = CreateDollSlot(dollPanel, "Legs", EquipmentSlot.Legs,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(4f, -195f), new Vector2(50f, 50f));
            var feetSlot = CreateDollSlot(dollPanel, "Feet", EquipmentSlot.Feet,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(4f, -250f), new Vector2(50f, 50f));

            // === ПРАВАЯ КОЛОНКА — Экипировка рук + кольца ===
            // WeaponMain: y=-85 (на уровне торса), WeaponOff: y=-140 (на уровне пояса)
            var weaponMainSlot = CreateDollSlot(dollPanel, "WeaponMain", EquipmentSlot.WeaponMain,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-54f, -85f), new Vector2(50f, 50f));
            var weaponOffSlot = CreateDollSlot(dollPanel, "WeaponOff", EquipmentSlot.WeaponOff,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-54f, -140f), new Vector2(50f, 50f));

            // 4 скрытых слота колец (правая колонка, ниже оружия)
            var ringLeft1Slot = CreateDollSlot(dollPanel, "RingLeft1", EquipmentSlot.RingLeft1,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-54f, -195f), new Vector2(50f, 50f));
            var ringLeft2Slot = CreateDollSlot(dollPanel, "RingLeft2", EquipmentSlot.RingLeft2,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-54f, -250f), new Vector2(50f, 50f));
            var ringRight1Slot = CreateDollSlot(dollPanel, "RingRight1", EquipmentSlot.RingRight1,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-54f, -305f), new Vector2(50f, 50f));
            var ringRight2Slot = CreateDollSlot(dollPanel, "RingRight2", EquipmentSlot.RingRight2,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-54f, -360f), new Vector2(50f, 50f));
            ringLeft1Slot.gameObject.SetActive(false);
            ringLeft2Slot.gameObject.SetActive(false);
            ringRight1Slot.gameObject.SetActive(false);
            ringRight2Slot.gameObject.SetActive(false);

            // --- Панель статистики (внизу куклы) ---
            GameObject statsPanelGO = new GameObject("StatsPanel");
            statsPanelGO.transform.SetParent(dollPanel.transform, false);
            var statsPanelRect = statsPanelGO.AddComponent<RectTransform>();
            statsPanelRect.anchorMin = new Vector2(0f, 0f);
            statsPanelRect.anchorMax = new Vector2(1f, 0f);
            statsPanelRect.pivot = new Vector2(0.5f, 0f);
            statsPanelRect.anchoredPosition = new Vector2(0f, 4f);
            statsPanelRect.sizeDelta = new Vector2(0f, 70f);

            var damageText = SceneBuilderUtils.CreateTMPText(statsPanelGO, "DamageText", "⚔ 0",
                new Vector2(4f, -2f), 11, FontStyles.Normal, new Color(0.9f, 0.5f, 0.3f));
            var damageRect = damageText.GetComponent<RectTransform>();
            damageRect.anchorMin = new Vector2(0f, 1f);
            damageRect.anchorMax = new Vector2(1f, 1f);
            damageRect.pivot = new Vector2(0f, 1f);
            damageRect.anchoredPosition = new Vector2(4f, -2f);
            damageRect.sizeDelta = new Vector2(-8f, 16f);

            var defenseText = SceneBuilderUtils.CreateTMPText(statsPanelGO, "DefenseText", "🛡 0",
                new Vector2(4f, -20f), 11, FontStyles.Normal, new Color(0.4f, 0.7f, 0.9f));
            var defenseRect = defenseText.GetComponent<RectTransform>();
            defenseRect.anchorMin = new Vector2(0f, 1f);
            defenseRect.anchorMax = new Vector2(1f, 1f);
            defenseRect.pivot = new Vector2(0f, 1f);
            defenseRect.anchoredPosition = new Vector2(4f, -20f);
            defenseRect.sizeDelta = new Vector2(-8f, 16f);

            var statsSummaryText = SceneBuilderUtils.CreateTMPText(statsPanelGO, "StatsSummaryText", "",
                new Vector2(4f, -38f), 10, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));
            var statsSummaryRect = statsSummaryText.GetComponent<RectTransform>();
            statsSummaryRect.anchorMin = new Vector2(0f, 1f);
            statsSummaryRect.anchorMax = new Vector2(1f, 1f);
            statsSummaryRect.pivot = new Vector2(0f, 1f);
            statsSummaryRect.anchoredPosition = new Vector2(4f, -38f);
            statsSummaryRect.sizeDelta = new Vector2(-8f, 28f);

            // Индикатор хранилища кольца (скрыт)
            GameObject ringStorageIndicatorGO = new GameObject("RingStorageIndicator");
            ringStorageIndicatorGO.transform.SetParent(dollPanel.transform, false);
            var ringIndRect = ringStorageIndicatorGO.AddComponent<RectTransform>();
            ringIndRect.anchorMin = new Vector2(0f, 0f);
            ringIndRect.anchorMax = new Vector2(1f, 0f);
            ringIndRect.pivot = new Vector2(0.5f, 0f);
            ringIndRect.anchoredPosition = new Vector2(0f, -24f);
            ringIndRect.sizeDelta = new Vector2(0f, 24f);
            var ringIndBg = ringStorageIndicatorGO.AddComponent<Image>();
            ringIndBg.color = new Color(0.15f, 0.12f, 0.2f, 0.8f);
            ringIndBg.raycastTarget = false;

            var ringVolumeText = SceneBuilderUtils.CreateTMPText(ringStorageIndicatorGO,
                "RingVolumeText", "Объём: 0/0",
                new Vector2(0f, 0f), 10, FontStyles.Normal, new Color(0.6f, 0.5f, 0.8f));
            var ringVolRect = ringVolumeText.GetComponent<RectTransform>();
            ringVolRect.anchorMin = new Vector2(0f, 0f);
            ringVolRect.anchorMax = new Vector2(1f, 1f);
            ringVolRect.pivot = new Vector2(0.5f, 0.5f);
            ringVolRect.anchoredPosition = Vector2.zero;
            ringVolRect.sizeDelta = Vector2.zero;
            ringVolumeText.alignment = TextAlignmentOptions.Center;

            ringStorageIndicatorGO.SetActive(false);

            // --- Подключение SerializeField ссылок BodyDollPanel ---
            WireBodyDollPanelReferences(
                bodyDollPanelOut,
                headSlot, torsoSlot, beltSlot, legsSlot, feetSlot,
                weaponMainSlot, weaponOffSlot,
                ringLeft1Slot, ringLeft2Slot, ringRight1Slot, ringRight2Slot,
                ringStorageIndicatorGO, ringVolumeText,
                damageText, defenseText, statsSummaryText,
                bodySilhouette);

            Debug.Log("[Phase17] BodyDollPanel двухколоночный layout создан (5 лево + 2 право + 4 скрыто)");

            // BackpackPanel (правая часть)
            GameObject backpackPanel = new GameObject("BackpackPanel");
            backpackPanel.transform.SetParent(content.transform, false);
            var bpRect = backpackPanel.AddComponent<RectTransform>();
            bpRect.anchorMin = new Vector2(0.42f, 0f); // Было 0.37 → 0.42
            bpRect.anchorMax = new Vector2(1f, 1f);
            bpRect.offsetMin = Vector2.zero;
            bpRect.offsetMax = Vector2.zero;

            var bpImage = backpackPanel.AddComponent<Image>();
            bpImage.color = new Color(0.1f, 0.1f, 0.12f, 0.6f);

            backpackPanelOut = backpackPanel.AddComponent<BackpackPanel>();

            // --- Внутренние элементы BackpackPanel (строчная модель v3.0) ---

            // ScrollRect — контейнер с прокруткой для списка предметов
            GameObject scrollGO = new GameObject("ScrollRect");
            scrollGO.transform.SetParent(backpackPanel.transform, false);
            var scrollRect = scrollGO.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(8, 35);
            scrollRect.offsetMax = new Vector2(-8, -30);
            var scrollImage = scrollGO.AddComponent<Image>();
            scrollImage.color = new Color(0.08f, 0.08f, 0.1f, 0.5f);
            var scrollRectComp = scrollGO.AddComponent<ScrollRect>();
            scrollRectComp.horizontal = false;
            scrollRectComp.vertical = true;
            scrollRectComp.movementType = ScrollRect.MovementType.Clamped;
            scrollRectComp.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // ListContainer — VerticalLayoutGroup для строк-предметов
            GameObject listContGO = new GameObject("ListContainer");
            listContGO.transform.SetParent(scrollGO.transform, false);
            var listContRect = listContGO.AddComponent<RectTransform>();
            listContRect.anchorMin = new Vector2(0f, 1f);
            listContRect.anchorMax = new Vector2(1f, 1f);
            listContRect.pivot = new Vector2(0f, 1f);
            listContRect.offsetMin = new Vector2(0, 0);
            listContRect.offsetMax = new Vector2(0, 0);
            var vertLayout = listContGO.AddComponent<VerticalLayoutGroup>();
            vertLayout.spacing = 2f;
            vertLayout.padding = new RectOffset(4, 4, 4, 4);
            vertLayout.childAlignment = TextAnchor.UpperCenter;
            vertLayout.childControlWidth = true;
            vertLayout.childControlHeight = false;
            vertLayout.childForceExpandWidth = true;
            vertLayout.childForceExpandHeight = false;
            var contentSizeFitter = listContGO.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Подключаем ScrollRect → ListContainer
            scrollRectComp.content = listContRect;

            // BackpackNameText — название рюкзака
            var bpNameText = SceneBuilderUtils.CreateTMPText(backpackPanel, "BackpackNameText",
                "Тканевая сумка",
                new Vector2(10, -5), 14, FontStyles.Bold,
                new Color(0.85f, 0.8f, 0.7f));
            var bpNameRect = bpNameText.GetComponent<RectTransform>();
            bpNameRect.anchorMin = new Vector2(0f, 1f);
            bpNameRect.anchorMax = new Vector2(0.6f, 1f);
            bpNameRect.pivot = new Vector2(0f, 1f);
            bpNameRect.anchoredPosition = new Vector2(10, -5);
            bpNameRect.sizeDelta = new Vector2(200f, 20f);

            // WeightText — текущий / максимальный вес
            var weightTMP = SceneBuilderUtils.CreateTMPText(backpackPanel, "WeightText",
                "0.0 / 10.0 кг",
                new Vector2(0, -5), 12, FontStyles.Normal,
                Color.white);
            var weightTMPRect = weightTMP.GetComponent<RectTransform>();
            weightTMPRect.anchorMin = new Vector2(0.6f, 1f);
            weightTMPRect.anchorMax = new Vector2(1f, 1f);
            weightTMPRect.pivot = new Vector2(1f, 1f);
            weightTMPRect.anchoredPosition = new Vector2(-10, -5);
            weightTMPRect.sizeDelta = new Vector2(150f, 18f);
            weightTMP.alignment = TextAlignmentOptions.Right;

            // WeightBar — полоска веса
            var weightBarSlider = SceneBuilderUtils.CreateBar(backpackPanel, "WeightBar",
                new Vector2(10, -25), 200f, 8f,
                new Color(0.3f, 0.7f, 0.3f, 0.9f));
            var weightBarRect = weightBarSlider.GetComponent<RectTransform>();
            weightBarRect.anchorMin = new Vector2(0f, 1f);
            weightBarRect.anchorMax = new Vector2(1f, 1f);
            weightBarRect.pivot = new Vector2(0f, 1f);
            weightBarRect.anchoredPosition = new Vector2(10, -25);
            weightBarRect.sizeDelta = new Vector2(-20, 8f);
            weightBarSlider.maxValue = 10f;
            weightBarSlider.value = 0f;

            // VolumeBar — полоска объёма
            var volumeBarSlider = SceneBuilderUtils.CreateBar(backpackPanel, "VolumeBar",
                new Vector2(10, -34), 200f, 6f,
                new Color(0.3f, 0.5f, 0.8f, 0.9f));
            var volumeBarRect = volumeBarSlider.GetComponent<RectTransform>();
            volumeBarRect.anchorMin = new Vector2(0f, 1f);
            volumeBarRect.anchorMax = new Vector2(1f, 1f);
            volumeBarRect.pivot = new Vector2(0f, 1f);
            volumeBarRect.anchoredPosition = new Vector2(10, -34);
            volumeBarRect.sizeDelta = new Vector2(-20, 6f);
            volumeBarSlider.maxValue = 50f;
            volumeBarSlider.value = 0f;

            // VolumeText — текущий / максимальный объём
            var volumeTMP = SceneBuilderUtils.CreateTMPText(backpackPanel, "VolumeText",
                "0.0 / 50.0 л",
                new Vector2(0, -34), 10, FontStyles.Normal,
                new Color(0.5f, 0.7f, 0.9f));
            var volumeTMPRect = volumeTMP.GetComponent<RectTransform>();
            volumeTMPRect.anchorMin = new Vector2(1f, 1f);
            volumeTMPRect.anchorMax = new Vector2(1f, 1f);
            volumeTMPRect.pivot = new Vector2(1f, 1f);
            volumeTMPRect.anchoredPosition = new Vector2(-10, -34);
            volumeTMPRect.sizeDelta = new Vector2(80f, 14f);
            volumeTMP.alignment = TextAlignmentOptions.Right;

            // CountText — количество предметов
            var countTMP = SceneBuilderUtils.CreateTMPText(backpackPanel, "CountText",
                "0 предм.",
                new Vector2(0, -44), 10, FontStyles.Normal,
                new Color(0.6f, 0.6f, 0.6f));
            var countTMPRect = countTMP.GetComponent<RectTransform>();
            countTMPRect.anchorMin = new Vector2(0f, 1f);
            countTMPRect.anchorMax = new Vector2(1f, 1f);
            countTMPRect.pivot = new Vector2(0f, 1f);
            countTMPRect.anchoredPosition = new Vector2(10, -44);
            countTMPRect.sizeDelta = new Vector2(-20, 14f);

            // SlotRowPrefab — шаблон для строки предмета
            GameObject slotPrefab = CreateSlotUIPrefab();
            slotPrefab.transform.SetParent(backpackPanel.transform, false);

            // --- Подключение SerializeField ссылок BackpackPanel ---
            WireBackpackPanelReferences(
                backpackPanelOut,
                slotPrefab,
                listContGO.transform,
                bpNameText,
                weightTMP,
                weightBarSlider,
                volumeTMP,
                volumeBarSlider,
                countTMP);

            Debug.Log("[Phase17] BackpackPanel внутренние элементы созданы и подключены");

            // StorageRingPanel (скрыта по умолчанию, показывается при выборе вкладки)
            GameObject ringPanel = new GameObject("StorageRingPanel");
            ringPanel.transform.SetParent(content.transform, false);
            var rpRect = ringPanel.AddComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(0.42f, 0f); // Было 0.37 → 0.42
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
            rect.anchorMin = new Vector2(0.42f, 0.05f);
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
            rect.anchorMin = new Vector2(0.40f, 0f);
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

        // ====================================================================
        //  TooltipPanel — создание внутренних элементов + подключение
        // ====================================================================

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

            // --- Внутренние элементы TooltipPanel ---

            // Рамка редкости — rarityBorder
            GameObject rarityBorderGO = new GameObject("RarityBorder");
            rarityBorderGO.transform.SetParent(tooltip.transform, false);
            var rarityBorderRect = rarityBorderGO.AddComponent<RectTransform>();
            rarityBorderRect.anchorMin = Vector2.zero;
            rarityBorderRect.anchorMax = Vector2.one;
            rarityBorderRect.offsetMin = Vector2.zero;
            rarityBorderRect.offsetMax = Vector2.zero;
            var rarityBorderImage = rarityBorderGO.AddComponent<Image>();
            rarityBorderImage.color = Color.gray;
            rarityBorderImage.raycastTarget = false;

            // Внутренний контент с отступом от рамки
            GameObject contentGO = new GameObject("Content");
            contentGO.transform.SetParent(tooltip.transform, false);
            var contentRect = contentGO.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(6, 6);
            contentRect.offsetMax = new Vector2(-6, -6);

            float yPos = 0f; // Текущая позиция по Y (от верхнего края)

            // Название предмета — nameText
            var nameTMP = SceneBuilderUtils.CreateTMPText(contentGO, "NameText", "",
                new Vector2(0, yPos), 16, FontStyles.Bold, Color.white);
            var nameR = nameTMP.GetComponent<RectTransform>();
            nameR.anchorMin = new Vector2(0f, 1f);
            nameR.anchorMax = new Vector2(1f, 1f);
            nameR.pivot = new Vector2(0f, 1f);
            nameR.anchoredPosition = new Vector2(0, yPos);
            nameR.sizeDelta = new Vector2(0f, 22f);
            yPos -= 22f;

            // Редкость — rarityText
            var rarityTMP = SceneBuilderUtils.CreateTMPText(contentGO, "RarityText", "",
                new Vector2(0, yPos), 12, FontStyles.Normal, Color.gray);
            var rarityR = rarityTMP.GetComponent<RectTransform>();
            rarityR.anchorMin = new Vector2(0f, 1f);
            rarityR.anchorMax = new Vector2(0.5f, 1f);
            rarityR.pivot = new Vector2(0f, 1f);
            rarityR.anchoredPosition = new Vector2(0, yPos);
            rarityR.sizeDelta = new Vector2(0f, 16f);
            yPos -= 16f;

            // Тип — typeText
            var typeTMP = SceneBuilderUtils.CreateTMPText(contentGO, "TypeText", "",
                new Vector2(0, yPos), 12, FontStyles.Normal, new Color(0.7f, 0.7f, 0.7f));
            var typeR = typeTMP.GetComponent<RectTransform>();
            typeR.anchorMin = new Vector2(0.5f, 1f);
            typeR.anchorMax = new Vector2(1f, 1f);
            typeR.pivot = new Vector2(1f, 1f);
            typeR.anchoredPosition = new Vector2(0, yPos);
            typeR.sizeDelta = new Vector2(0f, 16f);
            typeTMP.alignment = TextAlignmentOptions.Right;
            yPos -= 20f;

            // --- Боевой раздел (CombatSection) ---
            GameObject combatSectionGO = new GameObject("CombatSection");
            combatSectionGO.transform.SetParent(contentGO.transform, false);
            var combatSectionRect = combatSectionGO.AddComponent<RectTransform>();
            combatSectionRect.anchorMin = new Vector2(0f, 1f);
            combatSectionRect.anchorMax = new Vector2(1f, 1f);
            combatSectionRect.pivot = new Vector2(0f, 1f);
            combatSectionRect.anchoredPosition = new Vector2(0, yPos);
            combatSectionRect.sizeDelta = new Vector2(0f, 50f);
            yPos -= 50f;

            var damageTMP = SceneBuilderUtils.CreateTMPText(combatSectionGO, "DamageText", "",
                new Vector2(5, 0), 12, FontStyles.Normal, new Color(1f, 0.5f, 0.3f));
            damageTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 16f);

            var defenseTMP = SceneBuilderUtils.CreateTMPText(combatSectionGO, "DefenseText", "",
                new Vector2(5, -16), 12, FontStyles.Normal, new Color(0.3f, 0.6f, 1f));
            defenseTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 16f);

            var coverageTMP = SceneBuilderUtils.CreateTMPText(combatSectionGO, "CoverageText", "",
                new Vector2(5, -32), 12, FontStyles.Normal, new Color(0.6f, 0.6f, 0.8f));
            coverageTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 16f);

            var dmgRedTMP = SceneBuilderUtils.CreateTMPText(combatSectionGO, "DamageReductionText", "",
                new Vector2(120, 0), 11, FontStyles.Normal, new Color(0.7f, 0.5f, 0.8f));
            dmgRedTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 14f);

            var dodgeTMP = SceneBuilderUtils.CreateTMPText(combatSectionGO, "DodgeBonusText", "",
                new Vector2(120, -16), 11, FontStyles.Normal, new Color(0.5f, 0.8f, 0.5f));
            dodgeTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(110f, 14f);

            combatSectionGO.SetActive(false);

            // --- Физический раздел (PhysicalSection) ---
            GameObject physicalSectionGO = new GameObject("PhysicalSection");
            physicalSectionGO.transform.SetParent(contentGO.transform, false);
            var physicalSectionRect = physicalSectionGO.AddComponent<RectTransform>();
            physicalSectionRect.anchorMin = new Vector2(0f, 1f);
            physicalSectionRect.anchorMax = new Vector2(1f, 1f);
            physicalSectionRect.pivot = new Vector2(0f, 1f);
            physicalSectionRect.anchoredPosition = new Vector2(0, yPos);
            physicalSectionRect.sizeDelta = new Vector2(0f, 60f);
            yPos -= 60f;

            var physWeightTMP = SceneBuilderUtils.CreateTMPText(physicalSectionGO, "WeightText", "",
                new Vector2(5, 0), 12, FontStyles.Normal, Color.white);
            physWeightTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 16f);

            var volumeTMP = SceneBuilderUtils.CreateTMPText(physicalSectionGO, "VolumeText", "",
                new Vector2(5, -16), 12, FontStyles.Normal, Color.white);
            volumeTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 16f);

            var nestingTMP = SceneBuilderUtils.CreateTMPText(physicalSectionGO, "NestingText", "",
                new Vector2(5, -32), 12, FontStyles.Normal, Color.white);
            nestingTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 16f);

            var durTMP = SceneBuilderUtils.CreateTMPText(physicalSectionGO, "DurabilityText", "",
                new Vector2(5, -48), 12, FontStyles.Normal, Color.white);
            durTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 16f);

            // --- Раздел бонусов (BonusesSection) ---
            GameObject bonusesSectionGO = new GameObject("BonusesSection");
            bonusesSectionGO.transform.SetParent(contentGO.transform, false);
            var bonusesSectionRect = bonusesSectionGO.AddComponent<RectTransform>();
            bonusesSectionRect.anchorMin = new Vector2(0f, 1f);
            bonusesSectionRect.anchorMax = new Vector2(1f, 1f);
            bonusesSectionRect.pivot = new Vector2(0f, 1f);
            bonusesSectionRect.anchoredPosition = new Vector2(0, yPos);
            bonusesSectionRect.sizeDelta = new Vector2(0f, 40f);
            yPos -= 40f;

            var bonusesTMP = SceneBuilderUtils.CreateTMPText(bonusesSectionGO, "BonusesText", "",
                new Vector2(5, 0), 11, FontStyles.Normal, new Color(0.9f, 0.85f, 0.5f));
            bonusesTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 36f);

            bonusesSectionGO.SetActive(false);

            // --- Раздел материала (MaterialSection) ---
            GameObject materialSectionGO = new GameObject("MaterialSection");
            materialSectionGO.transform.SetParent(contentGO.transform, false);
            var materialSectionRect = materialSectionGO.AddComponent<RectTransform>();
            materialSectionRect.anchorMin = new Vector2(0f, 1f);
            materialSectionRect.anchorMax = new Vector2(1f, 1f);
            materialSectionRect.pivot = new Vector2(0f, 1f);
            materialSectionRect.anchoredPosition = new Vector2(0, yPos);
            materialSectionRect.sizeDelta = new Vector2(0f, 30f);
            yPos -= 30f;

            var materialTMP = SceneBuilderUtils.CreateTMPText(materialSectionGO, "MaterialText", "",
                new Vector2(5, 0), 12, FontStyles.Normal, new Color(0.7f, 0.65f, 0.5f));
            materialTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 16f);

            var gradeTMP = SceneBuilderUtils.CreateTMPText(materialSectionGO, "GradeText", "",
                new Vector2(130, 0), 12, FontStyles.Normal, new Color(0.8f, 0.7f, 0.4f));
            gradeTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 16f);

            materialSectionGO.SetActive(false);

            // --- Раздел требований (RequirementsSection) ---
            GameObject requirementsSectionGO = new GameObject("RequirementsSection");
            requirementsSectionGO.transform.SetParent(contentGO.transform, false);
            var reqSectionRect = requirementsSectionGO.AddComponent<RectTransform>();
            reqSectionRect.anchorMin = new Vector2(0f, 1f);
            reqSectionRect.anchorMax = new Vector2(1f, 1f);
            reqSectionRect.pivot = new Vector2(0f, 1f);
            reqSectionRect.anchoredPosition = new Vector2(0, yPos);
            reqSectionRect.sizeDelta = new Vector2(0f, 30f);
            yPos -= 30f;

            var requirementsTMP = SceneBuilderUtils.CreateTMPText(requirementsSectionGO, "RequirementsText", "",
                new Vector2(5, 0), 11, FontStyles.Normal, new Color(0.9f, 0.5f, 0.3f));
            requirementsTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 26f);

            requirementsSectionGO.SetActive(false);

            // --- Описание (DescriptionText) ---
            var descTMP = SceneBuilderUtils.CreateTMPText(contentGO, "DescriptionText", "",
                new Vector2(5, yPos), 11, FontStyles.Italic, new Color(0.65f, 0.65f, 0.65f));
            var descR = descTMP.GetComponent<RectTransform>();
            descR.anchorMin = new Vector2(0f, 1f);
            descR.anchorMax = new Vector2(1f, 1f);
            descR.pivot = new Vector2(0f, 1f);
            descR.anchoredPosition = new Vector2(5, yPos);
            descR.sizeDelta = new Vector2(-10f, 40f);
            yPos -= 40f;

            // --- Стоимость (ValueText) ---
            var valueTMP = SceneBuilderUtils.CreateTMPText(contentGO, "ValueText", "",
                new Vector2(5, yPos), 12, FontStyles.Bold, new Color(0.9f, 0.8f, 0.3f));
            var valueR = valueTMP.GetComponent<RectTransform>();
            valueR.anchorMin = new Vector2(0f, 1f);
            valueR.anchorMax = new Vector2(1f, 1f);
            valueR.pivot = new Vector2(0f, 1f);
            valueR.anchoredPosition = new Vector2(5, yPos);
            valueR.sizeDelta = new Vector2(-10f, 18f);

            // --- Подключение SerializeField ссылок TooltipPanel ---
            WireTooltipPanelReferences(
                tooltipPanelOut,
                rarityBorderImage,
                nameTMP,
                rarityTMP,
                typeTMP,
                combatSectionGO,
                damageTMP,
                defenseTMP,
                coverageTMP,
                dmgRedTMP,
                dodgeTMP,
                physicalSectionGO,
                physWeightTMP,
                volumeTMP,
                nestingTMP,
                durTMP,
                bonusesSectionGO,
                bonusesTMP,
                materialSectionGO,
                materialTMP,
                gradeTMP,
                requirementsSectionGO,
                requirementsTMP,
                descTMP,
                valueTMP);

            tooltip.SetActive(false);
            Debug.Log("[Phase17] TooltipPanel внутренние элементы созданы и подключены");
        }

        // ====================================================================
        //  DragDropLayer — создание внутренних элементов + подключение
        // ====================================================================

        private void CreateDragDropLayer(GameObject parent,
            out DragDropHandler dragDropOut,
            TooltipPanel tooltipPanel)
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

            // --- DragIcon — иконка перетаскиваемого предмета ---
            GameObject dragIconGO = new GameObject("DragIcon");
            dragIconGO.transform.SetParent(dragLayer.transform, false);
            var dragIconRect = dragIconGO.AddComponent<RectTransform>();
            dragIconRect.anchorMin = new Vector2(0.5f, 0.5f);
            dragIconRect.anchorMax = new Vector2(0.5f, 0.5f);
            dragIconRect.pivot = new Vector2(0.5f, 0.5f);
            dragIconRect.sizeDelta = new Vector2(50f, 50f);
            var dragIconImage = dragIconGO.AddComponent<Image>();
            dragIconImage.color = Color.white;
            dragIconImage.raycastTarget = false;
            dragIconGO.SetActive(false); // Скрыта по умолчанию

            // --- ContextMenuPrefab — шаблон контекстного меню ---
            GameObject contextMenuPrefab = CreateContextMenuPrefab();
            contextMenuPrefab.transform.SetParent(dragLayer.transform, false);

            // --- ContextMenuContainer — контейнер для экземпляров контекстного меню ---
            GameObject contextMenuContGO = new GameObject("ContextMenuContainer");
            contextMenuContGO.transform.SetParent(dragLayer.transform, false);
            var cmContRect = contextMenuContGO.AddComponent<RectTransform>();
            cmContRect.anchorMin = Vector2.zero;
            cmContRect.anchorMax = Vector2.one;
            cmContRect.offsetMin = Vector2.zero;
            cmContRect.offsetMax = Vector2.zero;

            // --- Подключение SerializeField ссылок DragDropHandler ---
            WireDragDropHandlerReferences(
                dragDropOut,
                dragIconImage,
                dragIconRect,
                contextMenuPrefab,
                contextMenuContGO.transform,
                tooltipPanel);

            Debug.Log("[Phase17] DragDropHandler внутренние элементы созданы и подключены");
        }

        /// <summary>
        /// Создаёт шаблон контекстного меню для DragDropHandler.contextMenuPrefab.
        /// </summary>
        private GameObject CreateContextMenuPrefab()
        {
            // Корневой объект — ContextMenuPrefab
            GameObject menuGO = new GameObject("ContextMenuPrefab");
            var menuRect = menuGO.AddComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0.5f, 0.5f);
            menuRect.anchorMax = new Vector2(0.5f, 0.5f);
            menuRect.pivot = new Vector2(0f, 1f);
            menuRect.sizeDelta = new Vector2(160f, 120f);

            // Фон
            var bgImage = menuGO.AddComponent<Image>();
            bgImage.color = new Color(0.1f, 0.1f, 0.12f, 0.95f);

            // Контейнер для пунктов меню
            GameObject itemsContainer = new GameObject("ItemsContainer");
            itemsContainer.transform.SetParent(menuGO.transform, false);
            var itemsRect = itemsContainer.AddComponent<RectTransform>();
            itemsRect.anchorMin = Vector2.zero;
            itemsRect.anchorMax = Vector2.one;
            itemsRect.offsetMin = new Vector2(4, 4);
            itemsRect.offsetMax = new Vector2(-4, -4);

            // Префаб НЕАКТИВЕН
            menuGO.SetActive(false);

            Debug.Log("[Phase17] ContextMenuPrefab создан");
            return menuGO;
        }

        // ====================================================================
        //  DollSlotUI — создание и подключение
        // ====================================================================
        // Редактировано: 2026-04-25 16:00:00 MSK

        /// <summary>
        /// Создаёт один DollSlotUI — квадратный слот экипировки 50×50.
        /// Редактировано: 2026-04-25 18:30:00 MSK — редизайн: 180×40 → 50×50
        /// 
        /// Слот квадратный, якоря задаются параметрами.
        /// Дочерние: IconImage, SlotBorder, SlotLabel, ItemNameText, DurabilityBar,
        /// BlockedOverlay, EmptyIcon — 7 дочерних, 8 SerializeField.
        /// </summary>
        private DollSlotUI CreateDollSlot(GameObject parent, string slotName,
            EquipmentSlot equipSlot,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 anchoredPos, Vector2 sizeDelta)
        {
            // Корневой объект слота
            GameObject slotGO = new GameObject(slotName);
            slotGO.transform.SetParent(parent.transform, false);
            var slotRect = slotGO.AddComponent<RectTransform>();
            slotRect.anchorMin = anchorMin;
            slotRect.anchorMax = anchorMax;
            slotRect.pivot = pivot;
            slotRect.anchoredPosition = anchoredPos;
            slotRect.sizeDelta = sizeDelta; // 50×50 для квадратных слотов

            // Фон слота — rgba(51, 51, 68, 255)
            var bgImage = slotGO.AddComponent<Image>();
            bgImage.color = new Color(51f / 255f, 51f / 255f, 68f / 255f, 1f);
            bgImage.raycastTarget = true;

            // Компонент DollSlotUI
            var slotUI = slotGO.AddComponent<DollSlotUI>();

            // --- 7 дочерних объектов (квадратный layout) ---

            // 1. IconImage (центр, с отступом) → iconImage
            GameObject iconGO = new GameObject("IconImage");
            iconGO.transform.SetParent(slotGO.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(4f, 6f);   // 4px отступ сверху/снизу
            iconRect.offsetMax = new Vector2(-4f, -10f); // 10px снизу для label
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.raycastTarget = false;
            iconGO.SetActive(false); // Скрыт по умолчанию

            // 2. SlotBorder (полный размер) → slotBorder
            GameObject borderGO = new GameObject("SlotBorder");
            borderGO.transform.SetParent(slotGO.transform, false);
            var borderRect = borderGO.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = Vector2.zero;
            borderRect.offsetMax = Vector2.zero;
            var slotBorder = borderGO.AddComponent<Image>();
            slotBorder.color = new Color(68f / 255f, 68f / 255f, 68f / 255f, 1f);
            slotBorder.raycastTarget = false;

            // 3. SlotLabel (нижняя часть слота, мелкий текст) → slotLabel
            var slotLabel = SceneBuilderUtils.CreateTMPText(slotGO, "SlotLabel", slotName,
                new Vector2(0f, 0f), 8, FontStyles.Bold, new Color(0.6f, 0.6f, 0.6f));
            var slotLabelRect = slotLabel.GetComponent<RectTransform>();
            slotLabelRect.anchorMin = new Vector2(0f, 0f);
            slotLabelRect.anchorMax = new Vector2(1f, 0f);
            slotLabelRect.pivot = new Vector2(0.5f, 0f);
            slotLabelRect.anchoredPosition = new Vector2(0f, 1f);
            slotLabelRect.sizeDelta = new Vector2(0f, 10f);
            slotLabel.alignment = TextAlignmentOptions.Center;

            // 4. ItemNameText (скрыт в квадратном слоте — показывается в tooltip) → itemNameText
            var itemNameText = SceneBuilderUtils.CreateTMPText(slotGO, "ItemNameText", "",
                new Vector2(0f, 0f), 1, FontStyles.Normal, Color.clear);
            var itemNameRect = itemNameText.GetComponent<RectTransform>();
            itemNameRect.anchorMin = new Vector2(0f, 0f);
            itemNameRect.anchorMax = new Vector2(1f, 0f);
            itemNameRect.pivot = new Vector2(0.5f, 0f);
            itemNameRect.anchoredPosition = Vector2.zero;
            itemNameRect.sizeDelta = Vector2.zero;
            itemNameText.gameObject.SetActive(false); // Скрыт — tooltip показывает

            // 5. DurabilityBar (тонкая полоска внизу, скрыта) → durabilityBar
            GameObject durBarGO = new GameObject("DurabilityBar");
            durBarGO.transform.SetParent(slotGO.transform, false);
            var durBarRect = durBarGO.AddComponent<RectTransform>();
            durBarRect.anchorMin = new Vector2(0f, 0f);
            durBarRect.anchorMax = new Vector2(1f, 0f);
            durBarRect.pivot = new Vector2(0f, 0f);
            durBarRect.offsetMin = new Vector2(2f, 0f);
            durBarRect.offsetMax = new Vector2(-2f, 2f);
            var durabilityBar = durBarGO.AddComponent<Image>();
            durabilityBar.color = new Color(0.3f, 0.9f, 0.3f, 0.9f);
            durabilityBar.raycastTarget = false;
            durBarGO.SetActive(false); // Скрыт по умолчанию

            // 6. BlockedOverlay (полный размер, скрыт) → blockedOverlay
            GameObject blockedGO = new GameObject("BlockedOverlay");
            blockedGO.transform.SetParent(slotGO.transform, false);
            var blockedRect = blockedGO.AddComponent<RectTransform>();
            blockedRect.anchorMin = Vector2.zero;
            blockedRect.anchorMax = Vector2.one;
            blockedRect.offsetMin = Vector2.zero;
            blockedRect.offsetMax = Vector2.zero;
            var blockedImage = blockedGO.AddComponent<Image>();
            blockedImage.color = new Color(0.3f, 0.3f, 0.3f, 0.6f);
            blockedImage.raycastTarget = false;
            blockedGO.SetActive(false); // Скрыт по умолчанию

            // 7. EmptyIcon (центр, видимый) → emptyIcon
            GameObject emptyIconGO = new GameObject("EmptyIcon");
            emptyIconGO.transform.SetParent(slotGO.transform, false);
            var emptyIconRect = emptyIconGO.AddComponent<RectTransform>();
            emptyIconRect.anchorMin = new Vector2(0.5f, 0.6f);
            emptyIconRect.anchorMax = new Vector2(0.5f, 0.6f);
            emptyIconRect.pivot = new Vector2(0.5f, 0.5f);
            emptyIconRect.sizeDelta = new Vector2(24f, 24f);
            emptyIconRect.anchoredPosition = Vector2.zero;
            var emptyIconImage = emptyIconGO.AddComponent<Image>();
            emptyIconImage.color = new Color(0.3f, 0.3f, 0.4f, 0.5f); // Тусклая иконка-плейсхолдер
            emptyIconImage.raycastTarget = false;

            // --- Подключение 8 SerializeField ссылок ---
            WireDollSlotReferences(slotUI, iconImage, slotBorder, slotLabel,
                itemNameText, durabilityBar, blockedGO, emptyIconGO, equipSlot);

            Debug.Log($"[Phase17] DollSlotUI (50×50) создан: {slotName} ({equipSlot})");
            return slotUI;
        }

        /// <summary>
        /// Подключает все 8 SerializeField ссылок DollSlotUI через SerializedObject.
        /// </summary>
        private void WireDollSlotReferences(DollSlotUI slotUI,
            Image iconImage, Image slotBorder, TMP_Text slotLabel,
            TMP_Text itemNameText, Image durabilityBar,
            GameObject blockedOverlay, GameObject emptyIcon,
            EquipmentSlot slotType)
        {
            // Редактировано: 2026-04-25 16:00:00 MSK
            SerializedObject so = new SerializedObject(slotUI);

            so.FindProperty("iconImage").objectReferenceValue = iconImage;
            so.FindProperty("slotBorder").objectReferenceValue = slotBorder;
            so.FindProperty("slotLabel").objectReferenceValue = slotLabel;
            so.FindProperty("itemNameText").objectReferenceValue = itemNameText;
            so.FindProperty("durabilityBar").objectReferenceValue = durabilityBar;
            so.FindProperty("blockedOverlay").objectReferenceValue = blockedOverlay;
            so.FindProperty("emptyIcon").objectReferenceValue = emptyIcon;
            so.FindProperty("slotType").intValue = (int)slotType;

            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Подключает все 17 SerializeField ссылок BodyDollPanel через SerializedObject.
        /// Поля: 11 слотов DollSlotUI, ringStorageIndicator, ringVolumeText,
        /// damageText, defenseText, statsSummaryText, bodySilhouette.
        /// </summary>
        private void WireBodyDollPanelReferences(
            BodyDollPanel panel,
            DollSlotUI headSlot, DollSlotUI torsoSlot, DollSlotUI beltSlot,
            DollSlotUI legsSlot, DollSlotUI feetSlot,
            DollSlotUI weaponMainSlot, DollSlotUI weaponOffSlot,
            DollSlotUI ringLeft1Slot, DollSlotUI ringLeft2Slot,
            DollSlotUI ringRight1Slot, DollSlotUI ringRight2Slot,
            GameObject ringStorageIndicator, TMP_Text ringVolumeText,
            TMP_Text damageText, TMP_Text defenseText, TMP_Text statsSummaryText,
            Image bodySilhouette)
        {
            // Редактировано: 2026-04-25 16:00:00 MSK
            SerializedObject so = new SerializedObject(panel);

            // 1-5: видимые слоты экипировки
            so.FindProperty("headSlot").objectReferenceValue = headSlot;
            so.FindProperty("torsoSlot").objectReferenceValue = torsoSlot;
            so.FindProperty("beltSlot").objectReferenceValue = beltSlot;
            so.FindProperty("legsSlot").objectReferenceValue = legsSlot;
            so.FindProperty("feetSlot").objectReferenceValue = feetSlot;

            // 6-7: оружейные слоты
            so.FindProperty("weaponMainSlot").objectReferenceValue = weaponMainSlot;
            so.FindProperty("weaponOffSlot").objectReferenceValue = weaponOffSlot;

            // 8-11: скрытые слоты колец
            so.FindProperty("ringLeft1Slot").objectReferenceValue = ringLeft1Slot;
            so.FindProperty("ringLeft2Slot").objectReferenceValue = ringLeft2Slot;
            so.FindProperty("ringRight1Slot").objectReferenceValue = ringRight1Slot;
            so.FindProperty("ringRight2Slot").objectReferenceValue = ringRight2Slot;

            // 12-13: индикатор хранилища кольца
            so.FindProperty("ringStorageIndicator").objectReferenceValue = ringStorageIndicator;
            so.FindProperty("ringVolumeText").objectReferenceValue = ringVolumeText;

            // 14-16: тексты статистики
            so.FindProperty("damageText").objectReferenceValue = damageText;
            so.FindProperty("defenseText").objectReferenceValue = defenseText;
            so.FindProperty("statsSummaryText").objectReferenceValue = statsSummaryText;

            // 17: силуэт тела
            so.FindProperty("bodySilhouette").objectReferenceValue = bodySilhouette;

            so.ApplyModifiedProperties();
            Debug.Log("[Phase17] BodyDollPanel SerializeField ссылки подключены (17 полей)");
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

        // ====================================================================
        //  Body Silhouette Generation
        // ====================================================================

        /// <summary>
        /// Генерирует процедурный силуэт тела персонажа (80×220px).
        /// Сохраняет как PNG в Assets/Sprites/UI/BodySilhouette.png и импортирует как Sprite.
        /// Редактировано: 2026-04-25 18:30:00 MSK
        /// </summary>
        private Sprite GenerateBodySilhouetteSprite()
        {
            const string dir = "Assets/Sprites/UI";
            const string path = dir + "/BodySilhouette.png";

            // Проверяем, существует ли уже спрайт
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (existing != null)
            {
                Debug.Log("[Phase17] BodySilhouette sprite уже существует");
                return existing;
            }

            // Создаём директорию
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            int w = 80, h = 220;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];

            // Заполняем прозрачным
            var clear = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // Цвет тела — тёмно-синий силуэт
            var body = new Color32(60, 60, 90, 255);
            var outline = new Color32(90, 90, 130, 255);

            // === Рисуем фигуру (от ног к голове, y=0 = низ) ===

            // Ступни (y: 8-18)
            FillRect(pixels, w, h, 18, 8, 16, 10, body);
            FillRect(pixels, w, h, 46, 8, 16, 10, body);

            // Ноги (y: 18-90)
            FillRect(pixels, w, h, 22, 18, 14, 72, body);
            FillRect(pixels, w, h, 44, 18, 14, 72, body);

            // Таз / пояс (y: 90-105)
            FillRect(pixels, w, h, 18, 90, 44, 15, body);

            // Торс (y: 105-165)
            FillRect(pixels, w, h, 16, 105, 48, 60, body);

            // Плечи (y: 155-170)
            FillRect(pixels, w, h, 8, 155, 64, 15, body);

            // Левая рука (y: 120-170)
            FillRect(pixels, w, h, 6, 120, 10, 50, body);

            // Правая рука (y: 120-170)
            FillRect(pixels, w, h, 64, 120, 10, 50, body);

            // Левая кисть (y: 108-122)
            FillEllipse(pixels, w, h, 11, 115, 7, 7, body);

            // Правая кисть (y: 108-122)
            FillEllipse(pixels, w, h, 69, 115, 7, 7, body);

            // Шея (y: 165-175)
            FillRect(pixels, w, h, 32, 165, 16, 10, body);

            // Голова (y: 175-210)
            FillEllipse(pixels, w, h, 40, 192, 16, 18, body);

            // === Контур (1px вокруг каждого элемента) ===
            DrawOutline(pixels, w, h, body, outline);

            tex.SetPixels32(pixels);
            tex.Apply();

            // Сохраняем PNG
            var png = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, png);
            Object.DestroyImmediate(tex);

            // Импортируем как Sprite
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            Debug.Log($"[Phase17] BodySilhouette sprite сгенерирован: {path}");
            return sprite;
        }

        /// <summary>Заполняет прямоугольник цветом</summary>
        private void FillRect(Color32[] pixels, int w, int h,
            int x, int y, int rw, int rh, Color32 color)
        {
            for (int dy = 0; dy < rh; dy++)
            {
                for (int dx = 0; dx < rw; dx++)
                {
                    int px = x + dx;
                    int py = y + dy;
                    if (px >= 0 && px < w && py >= 0 && py < h)
                        pixels[py * w + px] = color;
                }
            }
        }

        /// <summary>Заполняет эллипс цветом</summary>
        private void FillEllipse(Color32[] pixels, int w, int h,
            int cx, int cy, int rx, int ry, Color32 color)
        {
            for (int dy = -ry; dy <= ry; dy++)
            {
                for (int dx = -rx; dx <= rx; dx++)
                {
                    if ((dx * dx * ry * ry + dy * dy * rx * rx) <= (rx * rx * ry * ry))
                    {
                        int px = cx + dx;
                        int py = cy + dy;
                        if (px >= 0 && px < w && py >= 0 && py < h)
                            pixels[py * w + px] = color;
                    }
                }
            }
        }

        /// <summary>Рисует 1px контур вокруг непрозрачных пикселей</summary>
        private void DrawOutline(Color32[] pixels, int w, int h,
            Color32 bodyColor, Color32 outlineColor)
        {
            // Проходим по всем пикселям; если прозрачный сосед рядом с body → outline
            var temp = (Color32[])pixels.Clone();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (temp[y * w + x].a > 0) continue; // Уже закрашен

                    // Проверяем 4 соседа
                    bool hasNeighbor = false;
                    if (x > 0 && temp[y * w + (x - 1)].a > 0) hasNeighbor = true;
                    if (x < w - 1 && temp[y * w + (x + 1)].a > 0) hasNeighbor = true;
                    if (y > 0 && temp[(y - 1) * w + x].a > 0) hasNeighbor = true;
                    if (y < h - 1 && temp[(y + 1) * w + x].a > 0) hasNeighbor = true;

                    if (hasNeighbor)
                        pixels[y * w + x] = outlineColor;
                }
            }
        }
    }
}
#endif
