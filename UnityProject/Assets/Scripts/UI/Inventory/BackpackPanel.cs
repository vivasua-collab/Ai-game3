// ============================================================================
// BackpackPanel.cs — Панель рюкзака (динамическая Diablo-style сетка)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-18 20:00:00 UTC
// Редактировано: 2026-04-20 06:27:21 UTC — +using UnityEngine.EventSystems (FIX CS0246)
// ============================================================================
// Отображает сетку инвентаря, размер которой определяется BackpackData.
// Стартовый рюкзак: 3×4 (12 ячеек). При смене рюкзака — пересоздаётся.
// Поддерживает: предметы 1×1, 2×1, 1×2, 2×2, вес, стак, перетаскивание.
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Inventory;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.UI.Inventory
{
    /// <summary>
    /// Панель рюкзака — Diablo-style сетка инвентаря.
    /// Размер определяется текущим BackpackData.
    /// </summary>
    public class BackpackPanel : MonoBehaviour
    {
        #region Configuration

        [Header("Grid Settings")]
        [SerializeField] private int cellSize = 50;
        [SerializeField] private int cellSpacing = 2;
        [SerializeField] private int padding = 8;

        [Header("Prefabs")]
        [SerializeField] private GameObject slotUIPrefab;

        [Header("UI References")]
        [SerializeField] private Transform gridContainer;
        [SerializeField] private RectTransform gridBackground;
        [SerializeField] private TMP_Text backpackNameText;
        [SerializeField] private TMP_Text weightText;
        [SerializeField] private Slider weightBar;
        [SerializeField] private TMP_Text slotsText;

        [Header("Visual")]
        [SerializeField] private Color gridLineColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

        #endregion

        #region Runtime Data

        private InventoryController inventoryController;
        private DragDropHandler dragDropHandler;

        /// <summary>Все ячейки сетки: ключ = "x,y"</summary>
        private Dictionary<string, InventorySlotUI> gridCells = new Dictionary<string, InventorySlotUI>();

        /// <summary>Визуальные слоты с предметами: slotId → InventorySlotUI</summary>
        private Dictionary<int, InventorySlotUI> itemSlotUIs = new Dictionary<int, InventorySlotUI>();

        private int currentWidth;
        private int currentHeight;

        #endregion

        #region Properties

        public int GridWidth => currentWidth;
        public int GridHeight => currentHeight;
        public InventoryController Controller => inventoryController;

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализирует панель рюкзака.
        /// </summary>
        public void Initialize(InventoryController controller, DragDropHandler dragHandler)
        {
            inventoryController = controller;
            dragDropHandler = dragHandler;

            SubscribeToEvents();
            RebuildGrid();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (inventoryController == null) return;

            inventoryController.OnItemAdded += OnItemAdded;
            inventoryController.OnItemRemoved += OnItemRemoved;
            inventoryController.OnItemStackChanged += OnItemStackChanged;
            inventoryController.OnWeightChanged += OnWeightChanged;
            inventoryController.OnBackpackChanged += OnBackpackChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (inventoryController == null) return;

            inventoryController.OnItemAdded -= OnItemAdded;
            inventoryController.OnItemRemoved -= OnItemRemoved;
            inventoryController.OnItemStackChanged -= OnItemStackChanged;
            inventoryController.OnWeightChanged -= OnWeightChanged;
            inventoryController.OnBackpackChanged -= OnBackpackChanged;
        }

        #endregion

        #region Grid Building

        /// <summary>
        /// Полностью перестраивает сетку. Вызывается при инициализации и смене рюкзака.
        /// </summary>
        public void RebuildGrid()
        {
            if (inventoryController == null) return;

            currentWidth = inventoryController.GridWidth;
            currentHeight = inventoryController.GridHeight;

            ClearGrid();
            CreateGridCells();
            PlaceItems();
            UpdateWeightDisplay(inventoryController.EffectiveWeight, inventoryController.MaxWeight);
            UpdateBackpackInfo();
            UpdateGridBackground();
        }

        private void ClearGrid()
        {
            // Удаляем все дочерние объекты
            if (gridContainer != null)
            {
                for (int i = gridContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(gridContainer.GetChild(i).gameObject);
                }
            }

            gridCells.Clear();
            itemSlotUIs.Clear();
        }

        private void CreateGridCells()
        {
            if (gridContainer == null || slotUIPrefab == null) return;

            for (int y = 0; y < currentHeight; y++)
            {
                for (int x = 0; x < currentWidth; x++)
                {
                    var cellGO = Instantiate(slotUIPrefab, gridContainer);
                    var cellUI = cellGO.GetComponent<InventorySlotUI>();

                    if (cellUI == null)
                        cellUI = cellGO.AddComponent<InventorySlotUI>();

                    cellUI.InitializeEmpty(x, y, this);
                    cellUI.SetPosition(CalculateCellPosition(x, y));
                    cellUI.SetSize(new Vector2(cellSize, cellSize));

                    // Подписка на события
                    cellUI.OnSlotHover += OnCellHover;
                    cellUI.OnSlotExit += OnCellExit;
                    cellUI.OnSlotRightClicked += OnCellRightClicked;

                    gridCells[$"{x},{y}"] = cellUI;
                }
            }
        }

        private void PlaceItems()
        {
            if (inventoryController == null) return;

            foreach (var slot in inventoryController.Slots)
            {
                PlaceItemInGrid(slot);
            }
        }

        /// <summary>
        /// Размещает предмет в сетке, перекрывая фоновые ячейки.
        /// </summary>
        private void PlaceItemInGrid(InventorySlot slot)
        {
            if (slot == null || slot.ItemData == null) return;

            // Создаём визуальный слот для предмета
            var itemGO = Instantiate(slotUIPrefab, gridContainer);
            var itemUI = itemGO.GetComponent<InventorySlotUI>();

            if (itemUI == null)
                itemUI = itemGO.AddComponent<InventorySlotUI>();

            itemUI.SetSlot(slot, this);
            itemUI.SetPosition(CalculateCellPosition(slot.GridX, slot.GridY));
            itemUI.SetSize(CalculateItemSize(slot.ItemWidth, slot.ItemHeight));

            // Подписка на drag & контекстное меню
            itemUI.OnDragBegin += OnItemDragBegin;
            itemUI.OnDragging += OnItemDragging;
            itemUI.OnDragEnd += OnItemDragEnd;
            itemUI.OnSlotRightClicked += OnItemRightClicked;
            itemUI.OnSlotHover += OnItemHover;
            itemUI.OnSlotExit += OnItemExit;

            itemSlotUIs[slot.SlotId] = itemUI;

            // Скрываем фоновые ячейки под предметом
            for (int x = slot.GridX; x < slot.GridX + slot.ItemWidth; x++)
            {
                for (int y = slot.GridY; y < slot.GridY + slot.ItemHeight; y++)
                {
                    string key = $"{x},{y}";
                    if (gridCells.TryGetValue(key, out var cell))
                    {
                        cell.gameObject.SetActive(false);
                    }
                }
            }
        }

        /// <summary>
        /// Убирает визуальный слот предмета из сетки.
        /// </summary>
        private void RemoveItemFromGrid(InventorySlot slot)
        {
            if (slot == null) return;

            if (itemSlotUIs.TryGetValue(slot.SlotId, out var itemUI))
            {
                // Показываем фоновые ячейки обратно
                for (int x = slot.GridX; x < slot.GridX + slot.ItemWidth; x++)
                {
                    for (int y = slot.GridY; y < slot.GridY + slot.ItemHeight; y++)
                    {
                        string key = $"{x},{y}";
                        if (gridCells.TryGetValue(key, out var cell))
                        {
                            cell.gameObject.SetActive(true);
                        }
                    }
                }

                Destroy(itemUI.gameObject);
                itemSlotUIs.Remove(slot.SlotId);
            }
        }

        private void UpdateGridBackground()
        {
            if (gridBackground == null) return;

            float totalWidth = padding * 2 + currentWidth * cellSize + (currentWidth - 1) * cellSpacing;
            float totalHeight = padding * 2 + currentHeight * cellSize + (currentHeight - 1) * cellSpacing;

            gridBackground.sizeDelta = new Vector2(totalWidth, totalHeight);
        }

        #endregion

        #region Position Calculations

        private Vector2 CalculateCellPosition(int gridX, int gridY)
        {
            return new Vector2(
                padding + gridX * (cellSize + cellSpacing),
                -(padding + gridY * (cellSize + cellSpacing))
            );
        }

        private Vector2 CalculateItemSize(int width, int height)
        {
            return new Vector2(
                width * cellSize + (width - 1) * cellSpacing,
                height * cellSize + (height - 1) * cellSpacing
            );
        }

        /// <summary>
        /// Определяет позицию в сетке по экранной координате.
        /// </summary>
        public Vector2Int? ScreenToGridPosition(Vector2 screenPosition)
        {
            if (gridContainer == null) return null;

            var rect = gridContainer as RectTransform;
            if (rect == null) return null;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, screenPosition, null, out Vector2 localPoint))
                return null;

            // Инвертируем Y (сетка идёт сверху вниз)
            localPoint.y = -localPoint.y;

            int gridX = Mathf.FloorToInt((localPoint.x - padding) / (cellSize + cellSpacing));
            int gridY = Mathf.FloorToInt((localPoint.y - padding) / (cellSize + cellSpacing));

            if (gridX < 0 || gridX >= currentWidth || gridY < 0 || gridY >= currentHeight)
                return null;

            return new Vector2Int(gridX, gridY);
        }

        /// <summary>
        /// Проверяет, свободна ли область для предмета заданного размера.
        /// </summary>
        public bool IsAreaFree(int gridX, int gridY, int width, int height, int excludeSlotId = -1)
        {
            if (inventoryController == null) return false;

            // Проверяем границы
            if (gridX < 0 || gridY < 0 ||
                gridX + width > currentWidth || gridY + height > currentHeight)
                return false;

            // Проверяем через контроллер — есть ли предмет в этой области
            for (int x = gridX; x < gridX + width; x++)
            {
                for (int y = gridY; y < gridY + height; y++)
                {
                    var slotAtPos = inventoryController.GetSlotAtPosition(x, y);
                    if (slotAtPos != null && slotAtPos.SlotId != excludeSlotId)
                        return false;
                }
            }

            return true;
        }

        #endregion

        #region Event Handlers (from InventoryController)

        private void OnItemAdded(InventorySlot slot)
        {
            PlaceItemInGrid(slot);
        }

        private void OnItemRemoved(InventorySlot slot)
        {
            RemoveItemFromGrid(slot);
        }

        private void OnItemStackChanged(InventorySlot slot, int newCount)
        {
            if (itemSlotUIs.TryGetValue(slot.SlotId, out var itemUI))
            {
                itemUI.UpdateCount(newCount);
            }
        }

        private void OnWeightChanged(float current, float max)
        {
            UpdateWeightDisplay(current, max);
        }

        private void OnBackpackChanged(BackpackData newBackpack)
        {
            RebuildGrid();
        }

        #endregion

        #region UI Event Handlers (from SlotUI)

        // Фоновые ячейки
        private void OnCellHover(InventorySlotUI slotUI)
        {
            dragDropHandler?.OnCellHover(slotUI);
        }

        private void OnCellExit(InventorySlotUI slotUI)
        {
            dragDropHandler?.OnCellExit(slotUI);
        }

        private void OnCellRightClicked(InventorySlotUI slotUI)
        {
            // Пустая ячейка — нет контекстного меню
        }

        // Предметы
        private void OnItemDragBegin(InventorySlotUI slotUI, PointerEventData eventData)
        {
            dragDropHandler?.BeginDrag(slotUI, eventData);
        }

        private void OnItemDragging(InventorySlotUI slotUI, PointerEventData eventData)
        {
            dragDropHandler?.UpdateDrag(slotUI, eventData);
        }

        private void OnItemDragEnd(InventorySlotUI slotUI, PointerEventData eventData)
        {
            dragDropHandler?.EndDrag(slotUI, eventData);
        }

        private void OnItemRightClicked(InventorySlotUI slotUI)
        {
            dragDropHandler?.ShowContextMenu(slotUI);
        }

        private void OnItemHover(InventorySlotUI slotUI)
        {
            dragDropHandler?.ShowTooltip(slotUI);
        }

        private void OnItemExit(InventorySlotUI slotUI)
        {
            dragDropHandler?.HideTooltip();
        }

        #endregion

        #region Visual Updates

        private void UpdateWeightDisplay(float current, float max)
        {
            if (weightText != null)
                weightText.text = $"{current:F1} / {max:F1} кг";

            if (weightBar != null)
            {
                weightBar.maxValue = max > 0 ? max : 1f;
                weightBar.value = current;
            }

            // Подсветка перегруза
            if (inventoryController != null && weightText != null)
            {
                weightText.color = inventoryController.IsOverencumbered
                    ? new Color(1f, 0.3f, 0.3f)
                    : Color.white;
            }
        }

        private void UpdateBackpackInfo()
        {
            if (inventoryController == null) return;

            var backpack = inventoryController.CurrentBackpack;

            if (backpackNameText != null)
                backpackNameText.text = backpack != null ? backpack.nameRu : "Тканевая сумка";

            if (slotsText != null)
            {
                int total = inventoryController.TotalSlots;
                int used = inventoryController.UsedCells;
                slotsText.text = $"{used}/{total}";
            }
        }

        /// <summary>
        /// Подсвечивает ячейки, куда можно положить предмет.
        /// </summary>
        public void HighlightValidDrop(int itemWidth, int itemHeight, int excludeSlotId = -1)
        {
            for (int y = 0; y < currentHeight; y++)
            {
                for (int x = 0; x < currentWidth; x++)
                {
                    string key = $"{x},{y}";
                    if (gridCells.TryGetValue(key, out var cell) && cell.gameObject.activeSelf)
                    {
                        bool free = IsAreaFree(x, y, itemWidth, itemHeight, excludeSlotId);
                        cell.SetDropHighlight(free);
                    }
                }
            }
        }

        /// <summary>
        /// Сбрасывает всю подсветку.
        /// </summary>
        public void ClearAllHighlights()
        {
            foreach (var cell in gridCells.Values)
            {
                if (cell != null && cell.gameObject.activeSelf)
                    cell.ClearDropHighlight();
            }
        }

        #endregion
    }
}
