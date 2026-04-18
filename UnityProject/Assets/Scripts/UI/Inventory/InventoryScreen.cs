// ============================================================================
// InventoryScreen.cs — Главный экран инвентаря (v2.0)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-18 20:00:00 UTC
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

        [Header("Belt (future)")]
        [SerializeField] private Transform beltContainer;
        [SerializeField] private int maxBeltSlots = 4;

        [Header("UI Elements")]
        [SerializeField] private UnityEngine.UI.Button closeButton;
        [SerializeField] private UnityEngine.UI.Button sortButton;
        [SerializeField] private TMPro.TMP_Text titleText;

        [Header("Animation")]
        [SerializeField] private float openCloseDuration = 0.15f;
        [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #endregion

        #region Runtime Data

        private InventoryController inventoryController;
        private EquipmentController equipmentController;
        private bool isInitialized = false;
        private bool isOpen = false;

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

            // Инициализируем панели
            if (backpackPanel != null && inventoryController != null)
            {
                backpackPanel.Initialize(inventoryController, dragDropHandler);
            }

            if (bodyDollPanel != null && equipmentController != null && inventoryController != null)
            {
                bodyDollPanel.Initialize(equipmentController, inventoryController, dragDropHandler);
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

            // Заголовок
            if (titleText != null)
                titleText.text = "ИНВЕНТАРЬ";

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
                // Уведомляем UIManager
                var uiManager = FindObjectOfType<CultivationGame.UI.UIManager>();
                if (uiManager != null)
                    uiManager.ReturnToPrevious();
            }

            // I — закрыть инвентарь
            if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                Close();
                var uiManager = FindObjectOfType<CultivationGame.UI.UIManager>();
                if (uiManager != null)
                    uiManager.ToggleInventory();
            }
        }

        #endregion
    }
}
