// ============================================================================
// InventorySlotUI.cs — Визуальный слот предмета (v2.0)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-18 20:00:00 UTC
// ============================================================================
// Визуальная ячейка предмета в сетке инвентаря.
// Поддерживает: иконка, количество, рамка по редкости, подсветка при наведении,
//   drag-состояние, контекстное меню по правому клику.
// ============================================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Inventory;

namespace CultivationGame.UI.Inventory
{
    /// <summary>
    /// Визуальный слот предмета. Может быть пустым (фоновая ячейка сетки)
    /// или содержать предмет (InventorySlot).
    /// </summary>
    public class InventorySlotUI : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerClickHandler
    {
        #region Configuration

        [Header("Visual Elements")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image background;
        [SerializeField] private Image border;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private Image durabilityBar;
        [SerializeField] private GameObject blockedOverlay;

        [Header("Colors")]
        [SerializeField] private Color emptyColor = new Color(0.1f, 0.1f, 0.18f, 0.8f);
        [SerializeField] private Color hoverColor = new Color(0.2f, 0.2f, 0.35f, 0.9f);
        [SerializeField] private Color dragValidColor = new Color(0.2f, 0.5f, 0.2f, 0.9f);
        [SerializeField] private Color dragInvalidColor = new Color(0.5f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color blockedColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);

        #endregion

        #region Runtime Data

        private InventorySlot slot;
        private BackpackPanel owningPanel;
        private bool isDragging;
        private bool isBlocked; // Заблокировано двуручным оружием или другим предметом
        private CanvasGroup canvasGroup;
        private Vector2 originalPosition;
        private Transform originalParent;

        #endregion

        #region Properties

        /// <summary>Слот инвентаря (может быть null для пустой ячейки)</summary>
        public InventorySlot Slot => slot;

        /// <summary>Позиция в сетке</summary>
        public int GridX { get; private set; }
        public int GridY { get; private set; }

        /// <summary>Ячейка заблокирована (двуручное оружие и т.д.)</summary>
        public bool IsBlocked => isBlocked;

        /// <summary>Есть ли предмет в ячейке</summary>
        public bool HasItem => slot != null && slot.ItemData != null;

        #endregion

        #region Events

        /// <summary>Клик по слоту (левый клик)</summary>
        public event System.Action<InventorySlotUI> OnSlotClicked;

        /// <summary>Правый клик по слоту (контекстное меню)</summary>
        public event System.Action<InventorySlotUI> OnSlotRightClicked;

        /// <summary>Наведение мыши</summary>
        public event System.Action<InventorySlotUI> OnSlotHover;

        /// <summary>Увод мыши</summary>
        public event System.Action<InventorySlotUI> OnSlotExit;

        /// <summary>Начало перетаскивания</summary>
        public event System.Action<InventorySlotUI, PointerEventData> OnDragBegin;

        /// <summary>Перетаскивание</summary>
        public event System.Action<InventorySlotUI, PointerEventData> OnDragging;

        /// <summary>Конец перетаскивания</summary>
        public event System.Action<InventorySlotUI, PointerEventData> OnDragEnd;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализирует слот как пустую ячейку сетки.
        /// </summary>
        public void InitializeEmpty(int gridX, int gridY, BackpackPanel panel)
        {
            GridX = gridX;
            GridY = gridY;
            owningPanel = panel;
            slot = null;
            isBlocked = false;

            UpdateEmptyDisplay();
        }

        /// <summary>
        /// Устанавливает данные предмета в слот.
        /// </summary>
        public void SetSlot(InventorySlot inventorySlot, BackpackPanel panel)
        {
            slot = inventorySlot;
            owningPanel = panel;
            isBlocked = false;

            if (inventorySlot != null)
            {
                GridX = inventorySlot.GridX;
                GridY = inventorySlot.GridY;
                UpdateItemDisplay();
            }
            else
            {
                UpdateEmptyDisplay();
            }
        }

        /// <summary>
        /// Устанавливает/снимает блокировку слота.
        /// </summary>
        public void SetBlocked(bool blocked)
        {
            isBlocked = blocked;

            if (blockedOverlay != null)
                blockedOverlay.SetActive(blocked);

            if (background != null && blocked)
                background.color = blockedColor;
        }

        #endregion

        #region Display Update

        private void UpdateItemDisplay()
        {
            if (slot == null || slot.ItemData == null)
            {
                UpdateEmptyDisplay();
                return;
            }

            var itemData = slot.ItemData;

            // Иконка
            if (iconImage != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = itemData.icon != null;
                iconImage.color = Color.white;
            }

            // Количество
            if (countText != null)
            {
                countText.text = slot.Count > 1 ? slot.Count.ToString() : "";
                countText.enabled = true;
            }

            // Прочность
            if (durabilityBar != null)
            {
                durabilityBar.fillAmount = slot.DurabilityPercent;
                durabilityBar.gameObject.SetActive(slot.HasDurability);
            }

            // Рамка по редкости
            if (border != null)
            {
                border.color = GetRarityColor(itemData.rarity);
                border.enabled = true;
            }

            // Фон
            if (background != null)
                background.color = emptyColor;

            // Блокировка
            if (blockedOverlay != null)
                blockedOverlay.SetActive(false);
        }

        private void UpdateEmptyDisplay()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (countText != null)
                countText.enabled = false;

            if (durabilityBar != null)
                durabilityBar.gameObject.SetActive(false);

            if (border != null)
            {
                border.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                border.enabled = true;
            }

            if (background != null)
                background.color = emptyColor;

            if (blockedOverlay != null)
                blockedOverlay.SetActive(false);
        }

        /// <summary>
        /// Обновляет отображение количества.
        /// </summary>
        public void UpdateCount(int newCount)
        {
            if (countText != null)
            {
                countText.text = newCount > 1 ? newCount.ToString() : "";
            }
        }

        /// <summary>
        /// Устанавливает подсветку для валидной/невалидной цели дропа.
        /// </summary>
        public void SetDropHighlight(bool valid)
        {
            if (background != null)
            {
                background.color = valid ? dragValidColor : dragInvalidColor;
            }
        }

        /// <summary>
        /// Сбрасывает подсветку.
        /// </summary>
        public void ClearDropHighlight()
        {
            if (background != null)
            {
                background.color = emptyColor;
            }
        }

        #endregion

        #region Position / Size

        public void SetPosition(Vector2 position)
        {
            var rect = GetComponent<RectTransform>();
            if (rect != null)
                rect.anchoredPosition = position;
        }

        public void SetSize(Vector2 size)
        {
            var rect = GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = size;
        }

        #endregion

        #region Drag State

        public void SetDragging(bool dragging)
        {
            isDragging = dragging;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = !dragging;
                canvasGroup.alpha = dragging ? 0.7f : 1f;
            }
        }

        #endregion

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!isBlocked)
            {
                if (background != null && !isDragging)
                    background.color = hoverColor;
            }
            OnSlotHover?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isDragging)
                ClearDropHighlight();
            OnSlotExit?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (slot == null || isBlocked) return;

            originalPosition = GetComponent<RectTransform>().anchoredPosition;
            originalParent = transform.parent;

            SetDragging(true);
            OnDragBegin?.Invoke(this, eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging) return;
            OnDragging?.Invoke(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging) return;

            SetDragging(false);
            OnDragEnd?.Invoke(this, eventData);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isBlocked) return;

            if (eventData.button == PointerEventData.InputButton.Left)
            {
                OnSlotClicked?.Invoke(this);
            }
            else if (eventData.button == PointerEventData.InputButton.Right)
            {
                OnSlotRightClicked?.Invoke(this);
            }
        }

        #endregion

        #region Colors

        public static Color GetRarityColor(ItemRarity rarity)
        {
            return rarity switch
            {
                ItemRarity.Common => new Color(0.42f, 0.45f, 0.50f),    // Серый
                ItemRarity.Uncommon => new Color(0.13f, 0.77f, 0.37f),  // Зелёный
                ItemRarity.Rare => new Color(0.23f, 0.52f, 0.97f),      // Синий
                ItemRarity.Epic => new Color(0.66f, 0.33f, 0.97f),      // Фиолетовый
                ItemRarity.Legendary => new Color(0.98f, 0.75f, 0.14f), // Золотой
                ItemRarity.Mythic => new Color(0.94f, 0.27f, 0.27f),    // Красный
                _ => Color.gray
            };
        }

        #endregion
    }
}
