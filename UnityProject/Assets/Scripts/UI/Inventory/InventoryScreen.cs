// ============================================================================
// InventoryScreen.cs — Главный экран инвентаря (v2.0)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-18 20:00:00 UTC
// Редактировано: 2026-04-25 10:38:00 UTC — FIX: Убрана двойная обработка I, UIManager.Instance
// ============================================================================
// Объединяет все панели инвентаря в один экран:
// - BodyDollPanel (7 видимых слотов экипировки)
// - BackpackPanel (динамическая Diablo-style сетка)
// - BeltPanel (слоты быстрого доступа — заглушка)
// - TooltipPanel (карточка предмета)
// - DragDropHandler (перетаскивание)
// Открывается по клавише I через UIManager.
// ============================================================================

using System;
using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;
using CultivationGame.Inventory;

namespace CultivationGame.UI.Inventory
{
    /// <summary>
    /// Главный экран инвентаря. Объединяет куклу, рюкзак и обработчик перетаскивания.
    /// Управляется UIManager через GameState.Inventory.
    /// </summary>
    public class InventoryScreen : MonoBehaviour
    {
        #region Configuration

        [Header("Panels")]
        [SerializeField] private BackpackPanel backpackPanel;
        [SerializeField] private BodyDollPanel bodyDollPanel;
        [SerializeField] private DragDropHandler dragDropHandler;
        [SerializeField] private TooltipPanel tooltipPanel;
        [SerializeField] private StorageRingPanel storageRingPanel;
        [SerializeField] private GameObject spiritStoragePanel;

        [Header("Storage Tabs")]
        [SerializeField] private UnityEngine.UI.Button backpackTab;
        [SerializeField] private UnityEngine.UI.Button spiritStorageTab;
        [SerializeField] private UnityEngine.UI.Button storageRingTab;

        [Header("Belt (future)")]
        [SerializeField] private Transform beltContainer;
#pragma warning disable CS0414 // Поле используется через Unity Inspector (будущее расширение)
        [SerializeField] private int maxBeltSlots = 4;
#pragma warning restore CS0414

        [Header("UI Elements")]
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private UnityEngine.UI.Button sortButton;
        [SerializeField] private TMPro.TMP_Text titleText;

        [Header("Animation")]
#pragma warning disable CS0414 // Поле используется через Unity Inspector (будущее расширение)
        [SerializeField] private float openCloseDuration = 0.15f;
#pragma warning restore CS0414
        [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #endregion

        #region Runtime Data

        private InventoryController inventoryController;
        private EquipmentController equipmentController;
        private StorageRingController storageRingController;
        private bool isInitialized = false;
        private bool isOpen = false;

        private enum StorageTab { Backpack, SpiritStorage, StorageRing }
        private StorageTab activeTab = StorageTab.Backpack;

        #endregion

        #region Properties

        public bool IsOpen => isOpen;
        public BackpackPanel Backpack => backpackPanel;
        public BodyDollPanel Doll => bodyDollPanel;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void OnEnable()
        {
            SubscribeToInput();
            Refresh();

            // Анимация открытия
            isOpen = true;

            // Показываем панель кольца хранения
            if (storageRingPanel != null)
                storageRingPanel.Show();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
            isOpen = false;

            // Скрываем tooltip и контекстное меню
            if (tooltipPanel != null)
                tooltipPanel.Hide();
            if (dragDropHandler != null)
                dragDropHandler.HideContextMenu();

            // Скрываем панель кольца хранения
            if (storageRingPanel != null)
                storageRingPanel.Hide();
        }

        private void Update()
        {
            HandleInput();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // Находим контроллеры через ServiceLocator
            inventoryController = ServiceLocator.GetOrFind<InventoryController>();
            equipmentController = ServiceLocator.GetOrFind<EquipmentController>();
            storageRingController = ServiceLocator.GetOrFind<StorageRingController>();

            // Инициализируем панели
            if (backpackPanel != null && inventoryController != null)
            {
                backpackPanel.Initialize(inventoryController, dragDropHandler);
            }

            if (bodyDollPanel != null && equipmentController != null && inventoryController != null)
            {
                bodyDollPanel.Initialize(equipmentController, inventoryController, dragDropHandler);
            }

            if (storageRingPanel != null && storageRingController != null)
            {
                storageRingPanel.Initialize(storageRingController, inventoryController);
            }

            if (dragDropHandler != null)
            {
                dragDropHandler.Initialize(inventoryController, equipmentController, backpackPanel, bodyDollPanel);
            }

            // Кнопки
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);

            if (sortButton != null)
                sortButton.onClick.AddListener(SortInventory);

            // Кнопки вкладок хранилища
            if (backpackTab != null)
                backpackTab.onClick.AddListener(() => SwitchTab(StorageTab.Backpack));
            if (storageRingTab != null)
                storageRingTab.onClick.AddListener(() => SwitchTab(StorageTab.StorageRing));

            // Заголовок
            if (titleText != null)
                titleText.text = "ИНВЕНТАРЬ";

            // Начальная вкладка — рюкзак
            SwitchTab(StorageTab.Backpack);

            isInitialized = true;
        }

        #endregion

        #region Open / Close

        /// <summary>
        /// Открывает экран инвентаря.
        /// </summary>
        public void Open()
        {
            if (isOpen) return;

            gameObject.SetActive(true);

            // Разблокируем курсор
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Обновляем данные
            Refresh();
        }

        /// <summary>
        /// Закрывает экран инвентаря.
        /// </summary>
        public void Close()
        {
            if (!isOpen) return;

            // Возвращаем курсор
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            gameObject.SetActive(false);
        }

        /// <summary>
        /// Переключает экран инвентаря.
        /// </summary>
        public void Toggle()
        {
            if (isOpen)
                Close();
            else
                Open();
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Обновляет все панели инвентаря.
        /// </summary>
        public void Refresh()
        {
            if (!isInitialized) return;

            if (backpackPanel != null)
                backpackPanel.RebuildGrid();

            if (bodyDollPanel != null)
                bodyDollPanel.RefreshAllSlots();

            if (storageRingPanel != null)
                storageRingPanel.RefreshContents();
        }

        #endregion

        #region Storage Tabs

        /// <summary>
        /// Переключает вкладку хранилища (рюкзак / духовное хранилище / кольцо хранения).
        /// </summary>
        private void SwitchTab(StorageTab tab)
        {
            activeTab = tab;

            // Показываем/скрываем панели
            if (backpackPanel != null)
                backpackPanel.gameObject.SetActive(tab == StorageTab.Backpack);

            if (spiritStoragePanel != null)
                spiritStoragePanel.SetActive(tab == StorageTab.SpiritStorage);

            if (storageRingPanel != null)
                storageRingPanel.gameObject.SetActive(tab == StorageTab.StorageRing);

            // Обновляем цвета кнопок вкладок
            UpdateTabButtonColors();
        }

        /// <summary>
        /// Обновляет визуальное состояние кнопок вкладок.
        /// </summary>
        private void UpdateTabButtonColors()
        {
            SetTabColor(backpackTab, activeTab == StorageTab.Backpack);
            SetTabColor(spiritStorageTab, activeTab == StorageTab.SpiritStorage);
            SetTabColor(storageRingTab, activeTab == StorageTab.StorageRing);
        }

        private void SetTabColor(UnityEngine.UI.Button button, bool active)
        {
            if (button == null) return;
            var colors = button.colors;
            colors.normalColor = active ? new Color(0.3f, 0.6f, 0.9f) : Color.white;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }

        #endregion

        #region Sort

        private void SortInventory()
        {
            if (inventoryController == null) return;

            inventoryController.SortInventory();

            if (backpackPanel != null)
                backpackPanel.RebuildGrid();
        }

        #endregion

        #region Input

        private void SubscribeToInput()
        {
            // Подписка через UIManager — клавиша I обрабатывается там
        }

        private void UnsubscribeFromInput()
        {
            // Отписка
        }

        private void HandleInput()
        {
            if (Keyboard.current == null) return;

            // ESC закрывает инвентарь
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Close();
                // Уведомляем UIManager через singleton (FIX: был FindFirstObjectByType)
                if (UIManager.Instance != null)
                    UIManager.Instance.ReturnToPrevious();
            }

            // Редактировано: 2026-04-25 — FIX: Убрана обработка клавиши I из InventoryScreen.
            // UIManager — единственный обработчик переключения инвентаря.
            // Раньше InventoryScreen тоже обрабатывал I → Close() + ToggleInventory(),
            // что вызывало двойной toggle и конфликт SetActive.
        }

        #endregion
    }
}
