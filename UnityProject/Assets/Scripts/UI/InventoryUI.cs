// ============================================================================
// InventoryUI.cs — UI инвентаря (Diablo-style)
// Cultivation World Simulator
// Версия: 1.1 — Fix-12: ServiceLocator, UseItem, SortInventory, Input note
// ============================================================================
// Создан: 2026-03-31
// Этап: 5 - UI Enhancement
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Inventory;

namespace CultivationGame.UI
{
    /// <summary>
    /// UI контроллер инвентаря с Diablo-style сеткой.
    /// Поддерживает Drag & Drop, контекстное меню, стаки.
    /// </summary>
    public class InventoryUI : MonoBehaviour
    {
        #region Configuration

        [Header("Grid Settings")]
        [SerializeField] private int cellSize = 50;
        [SerializeField] private int cellSpacing = 2;
        [SerializeField] private int padding = 10;

        [Header("Prefabs")]
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private GameObject contextMenuPrefab;
        [SerializeField] private GameObject tooltipPrefab;

        [Header("References")]
        [SerializeField] private Transform gridContainer;
        [SerializeField] private Transform dragLayer;
        [SerializeField] private InventoryController inventoryController;
        [SerializeField] private EquipmentController equipmentController;

        [Header("UI Elements")]
        [SerializeField] private TMP_Text weightText;
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private Slider weightBar;
        [SerializeField] private Button sortButton;
        [SerializeField] private Button closeButton;

        #endregion

        #region Runtime Data

        private Dictionary<int, ItemSlotUI> slotUIs = new Dictionary<int, ItemSlotUI>();
        private ItemSlotUI draggedSlot = null;
        private ItemSlotUI hoveredSlot = null;
        private GameObject contextMenu = null;
        private GameObject tooltip = null;

        private bool isInitialized = false;

        #endregion

        #region Events

        public event Action<InventorySlot> OnItemEquipped;
        public event Action<InventorySlot> OnItemUsed;
        public event Action<InventorySlot> OnItemDropped;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeUI();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
            RefreshInventory();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
            CleanupUI();
        }

        private void Update()
        {
            HandleInput();
        }

        #endregion

        #region Initialization

        private void InitializeUI()
        {
            if (inventoryController == null)
                inventoryController = ServiceLocator.GetOrFind<InventoryController>(); // FIX UI-H03 (2026-04-12)

            if (sortButton != null)
                sortButton.onClick.AddListener(SortInventory);

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseInventory);

            isInitialized = true;
        }

        private void SubscribeToEvents()
        {
            if (inventoryController != null)
            {
                inventoryController.OnItemAdded += OnItemAdded;
                inventoryController.OnItemRemoved += OnItemRemoved;
                inventoryController.OnItemStackChanged += OnItemStackChanged;
                inventoryController.OnWeightChanged += OnWeightChanged;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (inventoryController != null)
            {
                inventoryController.OnItemAdded -= OnItemAdded;
                inventoryController.OnItemRemoved -= OnItemRemoved;
                inventoryController.OnItemStackChanged -= OnItemStackChanged;
                inventoryController.OnWeightChanged -= OnWeightChanged;
            }
        }

        private void CleanupUI()
        {
            foreach (var slotUI in slotUIs.Values)
            {
                if (slotUI != null && slotUI.gameObject != null)
                    Destroy(slotUI.gameObject);
            }
            slotUIs.Clear();

            if (contextMenu != null)
                Destroy(contextMenu);
            if (tooltip != null)
                Destroy(tooltip);
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Полностью обновить отображение инвентаря.
        /// </summary>
        public void RefreshInventory()
        {
            if (!isInitialized || inventoryController == null) return;

            CleanupUI();

            // Создаём слоты для всех предметов
            foreach (var slot in inventoryController.Slots)
            {
                CreateSlotUI(slot);
            }

            // Обновляем вес
            UpdateWeightDisplay(inventoryController.CurrentWeight, inventoryController.maxWeight);
        }

        private void CreateSlotUI(InventorySlot slot)
        {
            if (itemSlotPrefab == null || gridContainer == null) return;

            var slotGO = Instantiate(itemSlotPrefab, gridContainer);
            var slotUI = slotGO.GetComponent<ItemSlotUI>();

            if (slotUI == null)
                slotUI = slotGO.AddComponent<ItemSlotUI>();

            slotUI.Initialize(slot, this);
            slotUI.SetPosition(CalculateSlotPosition(slot.GridX, slot.GridY));
            slotUI.SetSize(CalculateSlotSize(slot.ItemWidth, slot.ItemHeight));

            slotUIs[slot.SlotId] = slotUI;
        }

        private Vector2 CalculateSlotPosition(int gridX, int gridY)
        {
            return new Vector2(
                padding + gridX * (cellSize + cellSpacing),
                -padding - gridY * (cellSize + cellSpacing)
            );
        }

        private Vector2 CalculateSlotSize(int width, int height)
        {
            return new Vector2(
                width * cellSize + (width - 1) * cellSpacing,
                height * cellSize + (height - 1) * cellSpacing
            );
        }

        #endregion

        #region Event Handlers

        private void OnItemAdded(InventorySlot slot)
        {
            CreateSlotUI(slot);
        }

        private void OnItemRemoved(InventorySlot slot)
        {
            if (slotUIs.TryGetValue(slot.SlotId, out var slotUI))
            {
                Destroy(slotUI.gameObject);
                slotUIs.Remove(slot.SlotId);
            }
        }

        private void OnItemStackChanged(InventorySlot slot, int newCount)
        {
            if (slotUIs.TryGetValue(slot.SlotId, out var slotUI))
            {
                slotUI.UpdateCount(newCount);
            }
        }

        private void OnWeightChanged(float current, float max)
        {
            UpdateWeightDisplay(current, max);
        }

        private void UpdateWeightDisplay(float current, float max)
        {
            if (weightText != null)
                weightText.text = $"{current:F1}/{max:F1}";

            if (weightBar != null)
            {
                weightBar.maxValue = max;
                weightBar.value = current;
            }

            if (capacityText != null)
            {
                float percent = max > 0 ? (current / max) * 100f : 0f;
                capacityText.text = $"{percent:F0}%";
            }
        }

        #endregion

        #region Drag & Drop

        public void StartDrag(ItemSlotUI slotUI)
        {
            if (slotUI == null || slotUI.Slot == null) return;

            draggedSlot = slotUI;
            draggedSlot.transform.SetParent(dragLayer);
            draggedSlot.SetDragging(true);

            HideTooltip();
            HideContextMenu();
        }

        public void EndDrag(ItemSlotUI slotUI)
        {
            if (draggedSlot == null) return;

            // Проверяем, куда дропнули
            if (hoveredSlot != null && hoveredSlot != draggedSlot)
            {
                // Пытаемся поменять местами
                if (inventoryController.SwapSlots(draggedSlot.Slot.SlotId, hoveredSlot.Slot.SlotId))
                {
                    // Успешный обмен
                    RefreshInventory();
                }
            }
            else
            {
                // Возвращаем на место
                draggedSlot.SetDragging(false);
                draggedSlot.transform.SetParent(gridContainer);
                draggedSlot.SetPosition(CalculateSlotPosition(draggedSlot.Slot.GridX, draggedSlot.Slot.GridY));
            }

            draggedSlot = null;
        }

        public void OnSlotHover(ItemSlotUI slotUI)
        {
            hoveredSlot = slotUI;
            ShowTooltip(slotUI.Slot);
        }

        public void OnSlotExit(ItemSlotUI slotUI)
        {
            if (hoveredSlot == slotUI)
                hoveredSlot = null;
            HideTooltip();
        }

        #endregion

        #region Tooltip

        private void ShowTooltip(InventorySlot slot)
        {
            if (tooltipPrefab == null || slot == null || slot.ItemData == null) return;

            if (tooltip != null)
                Destroy(tooltip);

            tooltip = Instantiate(tooltipPrefab, transform);
            var tooltipUI = tooltip.GetComponent<ItemTooltipUI>();

            if (tooltipUI != null)
            {
                tooltipUI.SetItem(slot.ItemData, slot.Count, slot.Durability);
                tooltip.transform.position = Input.mousePosition + new Vector3(15, -15, 0);
            }
        }

        private void HideTooltip()
        {
            if (tooltip != null)
            {
                Destroy(tooltip);
                tooltip = null;
            }
        }

        #endregion

        #region Context Menu

        public void ShowContextMenu(InventorySlot slot, Vector2 position)
        {
            if (contextMenuPrefab == null || slot == null) return;

            HideContextMenu();

            contextMenu = Instantiate(contextMenuPrefab, transform);
            var menuUI = contextMenu.GetComponent<ContextMenuUI>();

            if (menuUI != null)
            {
                var options = GetContextMenuOptions(slot);
                menuUI.SetOptions(options);
                menuUI.transform.position = position;
            }
        }

        private void HideContextMenu()
        {
            if (contextMenu != null)
            {
                Destroy(contextMenu);
                contextMenu = null;
            }
        }

        private List<ContextMenuOption> GetContextMenuOptions(InventorySlot slot)
        {
            var options = new List<ContextMenuOption>();

            if (slot.ItemData == null) return options;

            var itemData = slot.ItemData;

            // Использовать
            if (itemData.category == ItemCategory.Consumable)
            {
                options.Add(new ContextMenuOption
                {
                    label = "Использовать",
                    action = () => UseItem(slot)
                });
            }

            // Экипировать
            if (itemData is Data.ScriptableObjects.EquipmentData equipmentData)
            {
                options.Add(new ContextMenuOption
                {
                    label = "Экипировать",
                    action = () => EquipItem(slot, equipmentData)
                });
            }

            // Разделить стак
            if (itemData.stackable && slot.Count > 1)
            {
                options.Add(new ContextMenuOption
                {
                    label = "Разделить",
                    action = () => SplitStack(slot)
                });
            }

            // Выбросить
            options.Add(new ContextMenuOption
            {
                label = "Выбросить",
                action = () => DropItem(slot)
            });

            // Детали
            options.Add(new ContextMenuOption
            {
                label = "Информация",
                action = () => ShowItemDetails(slot)
            });

            return options;
        }

        #endregion

        #region Item Actions

        private void UseItem(InventorySlot slot)
        {
            if (slot == null || slot.ItemData == null) return;

            // FIX UI-M06: UseItem через InventoryController (2026-04-12)
            if (inventoryController != null)
            {
                bool used = inventoryController.RemoveItem(slot.SlotId, 1);
                if (used)
                {
                    Debug.Log($"[InventoryUI] Used item: {slot.ItemData.nameRu}");
                    OnItemUsed?.Invoke(slot);
                }
                else
                {
                    Debug.LogWarning($"[InventoryUI] Cannot use item: {slot.ItemData.nameRu}");
                }
            }
            else
            {
                Debug.Log($"Using item: {slot.ItemData.nameRu} (no InventoryController)");
                OnItemUsed?.Invoke(slot);
            }

            HideContextMenu();
        }

        private void EquipItem(InventorySlot slot, Data.ScriptableObjects.EquipmentData equipmentData)
        {
            if (slot == null || equipmentData == null) return;

            if (equipmentController != null)
            {
                if (equipmentController.Equip(equipmentData))
                {
                    // Удаляем из инвентаря
                    inventoryController.RemoveItem(slot.SlotId, 1);
                    OnItemEquipped?.Invoke(slot);
                }
            }

            HideContextMenu();
        }

        private void SplitStack(InventorySlot slot)
        {
            if (slot == null || !slot.ItemData.stackable || slot.Count <= 1) return;

            // TODO: Показать UI для выбора количества
            int splitAmount = slot.Count / 2;

            // Создаём новый слот с половиной
            inventoryController.RemoveItem(slot.SlotId, splitAmount);
            inventoryController.AddItem(slot.ItemData, splitAmount);

            HideContextMenu();
        }

        private void DropItem(InventorySlot slot)
        {
            if (slot == null) return;

            OnItemDropped?.Invoke(slot);
            inventoryController.RemoveSlot(slot.SlotId);
            HideContextMenu();
        }

        private void ShowItemDetails(InventorySlot slot)
        {
            if (slot == null || slot.ItemData == null) return;

            // TODO: Показать детальную информацию о предмете
            Debug.Log($"Item details: {slot.ItemData.nameRu}\n{slot.ItemData.description}");

            HideContextMenu();
        }

        #endregion

        #region Utility

        private void HandleInput()
        {
            // NOTE UI-H06: Старый Input System — будущий переход на новый Input System (2026-04-12)
            // Закрыть контекстное меню по клику вне
            if (Input.GetMouseButtonDown(0) && contextMenu != null)
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    HideContextMenu();
                }
            }

            // ESC закрывает инвентарь
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (contextMenu != null)
                {
                    HideContextMenu();
                }
                else
                {
                    CloseInventory();
                }
            }
        }

        private void SortInventory()
        {
            // FIX UI-M06: Сортировка по типу, затем по имени (2026-04-12)
            if (inventoryController == null) return;

            var sortedSlots = new List<InventorySlot>(inventoryController.Slots);
            sortedSlots.Sort((a, b) =>
            {
                // Сначала по категории
                int catCompare = a.ItemData.category.CompareTo(b.ItemData.category);
                if (catCompare != 0) return catCompare;
                // Затем по имени
                int nameCompare = string.Compare(a.ItemData.nameRu, b.ItemData.nameRu, StringComparison.Ordinal);
                if (nameCompare != 0) return nameCompare;
                // Затем по редкости (выше редкость — раньше)
                return b.ItemData.rarity.CompareTo(a.ItemData.rarity);
            });

            // Перемещаем слоты в отсортированном порядке
            for (int i = 0; i < sortedSlots.Count; i++)
            {
                inventoryController.MoveItem(sortedSlots[i].SlotId, i % inventoryController.TotalSlots, i / inventoryController.TotalSlots);
            }

            RefreshInventory();
        }

        private void CloseInventory()
        {
            gameObject.SetActive(false);
        }

        #endregion
    }

    // ============================================================================
    // ItemSlotUI — Визуальный слот предмета
    // ============================================================================

    public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private Image background;
        [SerializeField] private Image border;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image durabilityBar;

        private InventorySlot slot;
        private InventoryUI inventoryUI;
        private bool isDragging = false;

        public InventorySlot Slot => slot;

        public void Initialize(InventorySlot slot, InventoryUI inventoryUI)
        {
            this.slot = slot;
            this.inventoryUI = inventoryUI;

            UpdateDisplay();
        }

        public void UpdateDisplay()
        {
            if (slot == null || slot.ItemData == null) return;

            // Иконка
            if (iconImage != null)
            {
                iconImage.sprite = slot.ItemData.icon;
                iconImage.enabled = slot.ItemData.icon != null;
            }

            // Количество
            if (countText != null)
            {
                countText.text = slot.Count > 1 ? slot.Count.ToString() : "";
            }

            // Прочность
            if (durabilityBar != null)
            {
                durabilityBar.fillAmount = slot.DurabilityPercent;
                durabilityBar.gameObject.SetActive(slot.HasDurability);
            }

            // Цвет по редкости
            if (border != null)
            {
                border.color = GetRarityColor(slot.ItemData.rarity);
            }
        }

        public void UpdateCount(int newCount)
        {
            if (countText != null)
            {
                countText.text = newCount > 1 ? newCount.ToString() : "";
            }
        }

        public void SetPosition(Vector2 position)
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = position;
            }
        }

        public void SetSize(Vector2 size)
        {
            RectTransform rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.sizeDelta = size;
            }
        }

        public void SetDragging(bool dragging)
        {
            isDragging = dragging;

            if (background != null)
            {
                background.color = dragging ? new Color(1, 1, 1, 0.5f) : Color.white;
            }

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = !dragging;
                canvasGroup.alpha = dragging ? 0.8f : 1f;
            }
        }

        private CanvasGroup canvasGroup;
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => new Color(0.7f, 0.7f, 0.7f),
                ItemRarity.Uncommon => new Color(0.3f, 1f, 0.3f),
                ItemRarity.Rare => new Color(0.3f, 0.5f, 1f),
                ItemRarity.Epic => new Color(0.7f, 0.3f, 1f),
                ItemRarity.Legendary => new Color(1f, 0.5f, 0f),
                ItemRarity.Mythic => new Color(1f, 0.2f, 0.2f),
                _ => Color.gray
            };
        }

        // === Pointer Events ===

        public void OnPointerEnter(PointerEventData eventData)
        {
            inventoryUI?.OnSlotHover(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            inventoryUI?.OnSlotExit(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            inventoryUI?.StartDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (isDragging)
            {
                transform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            inventoryUI?.EndDrag(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                inventoryUI?.ShowContextMenu(slot, eventData.position);
            }
        }
    }

    // ============================================================================
    // ItemTooltipUI — Всплывающая подсказка
    // ============================================================================

    public class ItemTooltipUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private TMP_Text statsText;
        [SerializeField] private TMP_Text valueText;

        public void SetItem(Data.ScriptableObjects.ItemData item, int count, int durability)
        {
            if (item == null) return;

            if (nameText != null)
            {
                nameText.text = item.nameRu;
                nameText.color = GetRarityColor(item.rarity);
            }

            if (typeText != null)
            {
                typeText.text = GetItemTypeText(item);
            }

            if (descriptionText != null)
            {
                descriptionText.text = item.description;
            }

            if (statsText != null)
            {
                statsText.text = GetStatsText(item, count, durability);
            }

            if (valueText != null)
            {
                valueText.text = $"Стоимость: {item.value * count}";
            }
        }

        private string GetItemTypeText(Data.ScriptableObjects.ItemData item)
        {
            return item.category switch
            {
                ItemCategory.Weapon => "Оружие",
                ItemCategory.Armor => "Броня",
                ItemCategory.Accessory => "Аксессуар",
                ItemCategory.Consumable => "Расходник",
                ItemCategory.Material => "Материал",
                ItemCategory.Quest => "Квестовый предмет",
                _ => "Предмет"
            };
        }

        private string GetStatsText(Data.ScriptableObjects.ItemData item, int count, int durability)
        {
            var lines = new System.Text.StringBuilder();

            lines.AppendLine($"Количество: {count}");
            lines.AppendLine($"Вес: {item.weight * count:F1}");

            if (item.hasDurability)
            {
                lines.AppendLine($"Прочность: {durability}/{item.maxDurability}");
            }

            if (item.stackable)
            {
                lines.AppendLine($"Максимум в стаке: {item.maxStack}");
            }

            return lines.ToString();
        }

        private Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => Color.white,
                ItemRarity.Uncommon => Color.green,
                ItemRarity.Rare => Color.blue,
                ItemRarity.Epic => new Color(0.7f, 0.3f, 1f),
                ItemRarity.Legendary => new Color(1f, 0.5f, 0f),
                ItemRarity.Mythic => Color.red,
                _ => Color.white
            };
        }
    }

    // ============================================================================
    // ContextMenuUI — Контекстное меню
    // ============================================================================

    public class ContextMenuUI : MonoBehaviour
    {
        [SerializeField] private Transform optionsContainer;
        [SerializeField] private GameObject optionPrefab;

        public void SetOptions(List<ContextMenuOption> options)
        {
            // Очищаем старые опции
            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }

            // Создаём новые
            foreach (var option in options)
            {
                var optionGO = Instantiate(optionPrefab, optionsContainer);
                var button = optionGO.GetComponent<Button>();
                var text = optionGO.GetComponentInChildren<TMP_Text>();

                if (text != null)
                    text.text = option.label;

                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        option.action?.Invoke();
                        Destroy(gameObject);
                    });
                }
            }
        }
    }

    // ============================================================================
    // ContextMenuOption — Опция контекстного меню
    // ============================================================================

    public class ContextMenuOption
    {
        public string label;
        public Action action;
    }
}
