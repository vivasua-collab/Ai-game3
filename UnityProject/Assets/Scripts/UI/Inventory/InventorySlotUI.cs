// ============================================================================
// InventorySlotUI.cs — Визуальная строка предмета (v3.0 — строчная модель)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-18 20:00:00 UTC
// Редактировано: 2026-04-27 18:10:00 UTC — ПЕРЕПИСЬ: ячейка сетки → строка списка
// Редактировано: 2026-04-29 08:55:00 UTC — подсветка фона по Grade через GradeColors (UI.1)
// ============================================================================
// Визуальная строка предмета в строчном инвентаре.
// Формат: [иконка] [название] [кол-во] [вес] [объём]
// Поддерживает: рамка по редкости, drag, контекстное меню, tooltip.
// ============================================================================

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
    /// Визуальная строка предмета в строчном инвентаре (v3.0).
    /// Формат: иконка + название + кол-во + вес + объём.
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
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text countText;
        [SerializeField] private TMP_Text weightText;
        [SerializeField] private TMP_Text volumeText;
        [SerializeField] private Image durabilityBar;

        [Header("Colors")]
        [SerializeField] private Color normalColor = new Color(0.1f, 0.1f, 0.18f, 0.8f);
        [SerializeField] private Color hoverColor = new Color(0.2f, 0.2f, 0.35f, 0.9f);

        #endregion

        #region Runtime Data

        private InventorySlot slot;
        private BackpackPanel owningPanel;
        private bool isDragging;
        private CanvasGroup canvasGroup;
        private Vector2 originalPosition;
        private Transform originalParent;

        #endregion

        #region Properties

        /// <summary>Слот инвентаря</summary>
        public InventorySlot Slot => slot;

        /// <summary>Индекс строки в списке</summary>
        public int RowIndex => slot != null ? slot.rowIndex : -1;

        /// <summary>Есть ли предмет</summary>
        public bool HasItem => slot != null && slot.ItemData != null;

        #endregion

        #region Events

        /// <summary>Клик по строке (левый)</summary>
        public event System.Action<InventorySlotUI> OnSlotClicked;

        /// <summary>Правый клик (контекстное меню)</summary>
        public event System.Action<InventorySlotUI> OnSlotRightClicked;

        /// <summary>Наведение мыши (tooltip)</summary>
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
        /// Устанавливает данные предмета в строку.
        /// </summary>
        public void SetSlot(InventorySlot inventorySlot, BackpackPanel panel)
        {
            slot = inventorySlot;
            owningPanel = panel;

            if (inventorySlot != null)
                UpdateItemDisplay();
        }

        /// <summary>
        /// Обновляет отображение из данных слота.
        /// </summary>
        public void RefreshDisplay()
        {
            if (slot != null)
                UpdateItemDisplay();
        }

        #endregion

        #region Display Update

        private void UpdateItemDisplay()
        {
            if (slot == null || slot.ItemData == null)
                return;

            var itemData = slot.ItemData;

            // Иконка
            if (iconImage != null)
            {
                iconImage.sprite = itemData.icon;
                iconImage.enabled = itemData.icon != null;
                iconImage.color = Color.white;
            }

            // Название
            if (nameText != null)
            {
                nameText.text = itemData.nameRu ?? itemData.itemId;
                nameText.color = GetRarityColor(itemData.rarity);
            }

            // Количество
            if (countText != null)
            {
                countText.text = slot.Count > 1 ? $"×{slot.Count}" : "";
            }

            // Вес
            if (weightText != null)
            {
                weightText.text = $"{slot.TotalWeight:F1}кг";
            }

            // Объём
            if (volumeText != null)
            {
                volumeText.text = $"{slot.TotalVolume:F1}л";
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

            // Фон — подсветка по Grade (UI.1, Д9)
            if (background != null)
            {
                if (itemData is EquipmentData eqData)
                {
                    Color gradeTint = GradeColors.GetGradeColor(eqData.grade);
                    gradeTint.a = 0.15f;
                    background.color = gradeTint;
                }
                else
                {
                    background.color = normalColor;
                }
            }
        }

        /// <summary>
        /// Обновляет отображение количества.
        /// </summary>
        public void UpdateCount(int newCount)
        {
            if (countText != null)
            {
                countText.text = newCount > 1 ? $"×{newCount}" : "";
            }
            if (weightText != null && slot != null)
            {
                weightText.text = $"{slot.TotalWeight:F1}кг";
            }
            if (volumeText != null && slot != null)
            {
                volumeText.text = $"{slot.TotalVolume:F1}л";
            }
        }

        /// <summary>
        /// Устанавливает подсветку при наведении.
        /// </summary>
        public void SetHoverHighlight(bool hovered)
        {
            if (background != null && !isDragging)
            {
                background.color = hovered ? hoverColor : normalColor;
            }
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

        public bool IsDragging => isDragging;

        #endregion

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHoverHighlight(true);
            OnSlotHover?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHoverHighlight(false);
            OnSlotExit?.Invoke(this);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (slot == null) return;

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
