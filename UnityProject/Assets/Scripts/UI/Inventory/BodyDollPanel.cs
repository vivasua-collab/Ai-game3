// ============================================================================
// BodyDollPanel.cs — Кукла тела (7 видимых слотов экипировки)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-18 20:00:00 UTC
// ============================================================================
// Визуальная кукла персонажа с 7 видимыми слотами:
// Head, Torso, Belt, Legs, Feet, WeaponMain, WeaponOff
// Поддерживает: двуручное оружие (блокировка WeaponOff), drag & drop, tooltip.
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
    /// Визуальный слот экипировки на кукле.
    /// </summary>
    public class DollSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IPointerClickHandler, IDropHandler
    {
        [Header("Visual")]
        [SerializeField] private EquipmentSlot slotType;
        [SerializeField] private Image iconImage;
        [SerializeField] private Image slotBorder;
        [SerializeField] private TMP_Text slotLabel;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private Image durabilityBar;
        [SerializeField] private GameObject blockedOverlay;
        [SerializeField] private GameObject emptyIcon;

        private EquipmentInstance currentEquipment;
        private bool isBlocked;
        private BodyDollPanel owningPanel;

        /// <summary>Тип слота</summary>
        public EquipmentSlot SlotType => slotType;

        /// <summary>Текущая экипировка</summary>
        public EquipmentInstance Equipment => currentEquipment;

        /// <summary>Заблокирован (двуручное оружие)</summary>
        public bool IsBlocked => isBlocked;

        /// <summary>Есть ли предмет в слоте</summary>
        public bool HasItem => currentEquipment != null;

        /// <summary>Событие: клик по слоту</summary>
        public event Action<DollSlotUI> OnSlotClicked;
        /// <summary>Событие: правый клик</summary>
        public event Action<DollSlotUI> OnSlotRightClicked;
        /// <summary>Событие: наведение</summary>
        public event Action<DollSlotUI> OnSlotHover;
        /// <summary>Событие: уход мыши</summary>
        public event Action<DollSlotUI> OnSlotExit;
        /// <summary>Событие: дроп предмета</summary>
        public event Action<DollSlotUI> OnItemDropped;

        public void Initialize(BodyDollPanel panel)
        {
            owningPanel = panel;

            if (slotLabel != null)
                slotLabel.text = GetSlotDisplayName();

            Clear();
        }

        /// <summary>
        /// Устанавливает экипировку в слот.
        /// </summary>
        public void SetEquipment(EquipmentInstance instance)
        {
            currentEquipment = instance;

            if (instance != null && instance.equipmentData != null)
            {
                if (iconImage != null)
                {
                    iconImage.sprite = instance.equipmentData.icon;
                    iconImage.enabled = instance.equipmentData.icon != null;
                    iconImage.color = Color.white;
                }

                if (itemNameText != null)
                    itemNameText.text = instance.Name;

                if (durabilityBar != null)
                {
                    durabilityBar.fillAmount = instance.DurabilityPercent;
                    durabilityBar.gameObject.SetActive(instance.durability >= 0);
                }

                if (slotBorder != null)
                    slotBorder.color = InventorySlotUI.GetRarityColor(instance.equipmentData.rarity);

                if (emptyIcon != null)
                    emptyIcon.SetActive(false);
            }
            else
            {
                Clear();
            }
        }

        /// <summary>
        /// Устанавливает/снимает блокировку (двуручное оружие блокирует WeaponOff).
        /// </summary>
        public void SetBlocked(bool blocked)
        {
            isBlocked = blocked;

            if (blockedOverlay != null)
                blockedOverlay.SetActive(blocked);

            if (blocked)
                Clear();
        }

        /// <summary>
        /// Очищает слот.
        /// </summary>
        public void Clear()
        {
            currentEquipment = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (itemNameText != null)
                itemNameText.text = "";

            if (durabilityBar != null)
                durabilityBar.gameObject.SetActive(false);

            if (slotBorder != null)
                slotBorder.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);

            if (emptyIcon != null)
                emptyIcon.SetActive(!isBlocked);
        }

        /// <summary>
        /// Устанавливает подсветку для drag & drop.
        /// </summary>
        public void SetDropHighlight(bool valid)
        {
            if (slotBorder != null)
            {
                slotBorder.color = valid
                    ? new Color(0.2f, 0.8f, 0.2f, 0.8f)
                    : new Color(0.8f, 0.2f, 0.2f, 0.8f);
            }
        }

        /// <summary>
        /// Сбрасывает подсветку.
        /// </summary>
        public void ClearDropHighlight()
        {
            if (currentEquipment != null)
            {
                slotBorder.color = InventorySlotUI.GetRarityColor(currentEquipment.equipmentData.rarity);
            }
            else
            {
                slotBorder.color = new Color(0.4f, 0.4f, 0.4f, 0.6f);
            }
        }

        #region Pointer Events

        public void OnPointerEnter(PointerEventData eventData)
        {
            OnSlotHover?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            OnSlotExit?.Invoke(this);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isBlocked) return;

            if (eventData.button == PointerEventData.InputButton.Right)
                OnSlotRightClicked?.Invoke(this);
            else
                OnSlotClicked?.Invoke(this);
        }

        public void OnDrop(PointerEventData eventData)
        {
            OnItemDropped?.Invoke(this);
        }

        #endregion

        private string GetSlotDisplayName()
        {
            return slotType switch
            {
                EquipmentSlot.Head => "Голова",
                EquipmentSlot.Torso => "Торс",
                EquipmentSlot.Belt => "Пояс",
                EquipmentSlot.Legs => "Ноги",
                EquipmentSlot.Feet => "Ступни",
                EquipmentSlot.WeaponMain => "Осн. рука",
                EquipmentSlot.WeaponOff => "Доп. рука",
                _ => slotType.ToString()
            };
        }
    }

    // ============================================================================
    // BodyDollPanel — Панель куклы тела
    // ============================================================================

    /// <summary>
    /// Панель куклы тела — 7 видимых слотов экипировки.
    /// Отображает текущую экипировку и обрабатывает взаимодействия.
    /// </summary>
    public class BodyDollPanel : MonoBehaviour
    {
        #region Configuration

        [Header("Doll Slots (7 visible)")]
        [SerializeField] private DollSlotUI headSlot;
        [SerializeField] private DollSlotUI torsoSlot;
        [SerializeField] private DollSlotUI beltSlot;
        [SerializeField] private DollSlotUI legsSlot;
        [SerializeField] private DollSlotUI feetSlot;
        [SerializeField] private DollSlotUI weaponMainSlot;
        [SerializeField] private DollSlotUI weaponOffSlot;

        [Header("Stats Summary")]
        [SerializeField] private TMP_Text damageText;
        [SerializeField] private TMP_Text defenseText;
        [SerializeField] private TMP_Text statsSummaryText;

        [Header("Visual")]
        [SerializeField] private Image bodySilhouette;
        [SerializeField] private Color equippedColor = new Color(0.13f, 0.77f, 0.37f, 0.2f);

        #endregion

        #region Runtime Data

        private EquipmentController equipmentController;
        private InventoryController inventoryController;
        private DragDropHandler dragDropHandler;
        private Dictionary<EquipmentSlot, DollSlotUI> slotMap = new Dictionary<EquipmentSlot, DollSlotUI>();

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализирует панель куклы.
        /// </summary>
        public void Initialize(EquipmentController equipController, InventoryController invController, DragDropHandler dragHandler)
        {
            equipmentController = equipController;
            inventoryController = invController;
            dragDropHandler = dragHandler;

            BuildSlotMap();
            SubscribeToEvents();
            RefreshAllSlots();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void BuildSlotMap()
        {
            slotMap.Clear();

            if (headSlot != null) { slotMap[EquipmentSlot.Head] = headSlot; headSlot.Initialize(this); }
            if (torsoSlot != null) { slotMap[EquipmentSlot.Torso] = torsoSlot; torsoSlot.Initialize(this); }
            if (beltSlot != null) { slotMap[EquipmentSlot.Belt] = beltSlot; beltSlot.Initialize(this); }
            if (legsSlot != null) { slotMap[EquipmentSlot.Legs] = legsSlot; legsSlot.Initialize(this); }
            if (feetSlot != null) { slotMap[EquipmentSlot.Feet] = feetSlot; feetSlot.Initialize(this); }
            if (weaponMainSlot != null) { slotMap[EquipmentSlot.WeaponMain] = weaponMainSlot; weaponMainSlot.Initialize(this); }
            if (weaponOffSlot != null) { slotMap[EquipmentSlot.WeaponOff] = weaponOffSlot; weaponOffSlot.Initialize(this); }

            // Подписка на события слотов
            foreach (var kvp in slotMap)
            {
                kvp.Value.OnSlotHover += OnDollSlotHover;
                kvp.Value.OnSlotExit += OnDollSlotExit;
                kvp.Value.OnSlotRightClicked += OnDollSlotRightClicked;
                kvp.Value.OnSlotClicked += OnDollSlotClicked;
                kvp.Value.OnItemDropped += OnDollSlotDrop;
            }
        }

        private void SubscribeToEvents()
        {
            if (equipmentController == null) return;

            equipmentController.OnEquipmentEquipped += OnEquipmentEquipped;
            equipmentController.OnEquipmentUnequipped += OnEquipmentUnequipped;
            equipmentController.OnStatsChanged += OnStatsChanged;
            equipmentController.OnWeaponOffBlockChanged += OnWeaponOffBlockChanged;
        }

        private void UnsubscribeFromEvents()
        {
            if (equipmentController == null) return;

            equipmentController.OnEquipmentEquipped -= OnEquipmentEquipped;
            equipmentController.OnEquipmentUnequipped -= OnEquipmentUnequipped;
            equipmentController.OnStatsChanged -= OnStatsChanged;
            equipmentController.OnWeaponOffBlockChanged -= OnWeaponOffBlockChanged;
        }

        #endregion

        #region Refresh

        /// <summary>
        /// Обновляет все слоты куклы.
        /// </summary>
        public void RefreshAllSlots()
        {
            if (equipmentController == null) return;

            foreach (var kvp in slotMap)
            {
                var equipment = equipmentController.GetEquipment(kvp.Key);
                kvp.Value.SetEquipment(equipment);
            }

            // Блокировка WeaponOff
            if (slotMap.TryGetValue(EquipmentSlot.WeaponOff, out var offSlot))
            {
                offSlot.SetBlocked(equipmentController.IsWeaponOffBlocked);
            }

            UpdateStatsDisplay();
        }

        private void UpdateStatsDisplay()
        {
            if (equipmentController == null) return;

            var stats = equipmentController.CurrentStats;

            if (damageText != null)
                damageText.text = stats.totalDamage > 0 ? $"⚔ {stats.totalDamage}" : "";

            if (defenseText != null)
                defenseText.text = stats.totalDefense > 0 ? $"🛡 {stats.totalDefense}" : "";

            if (statsSummaryText != null)
            {
                var sb = new System.Text.StringBuilder();
                if (stats.strength != 0) sb.AppendLine($"Сила: +{stats.strength:F0}");
                if (stats.agility != 0) sb.AppendLine($"Ловкость: +{stats.agility:F0}");
                if (stats.constitution != 0) sb.AppendLine($"Телослож.: +{stats.constitution:F0}");
                if (stats.intelligence != 0) sb.AppendLine($"Интеллект: +{stats.intelligence:F0}");
                if (stats.conductivity != 0) sb.AppendLine($"Проводимость: +{stats.conductivity:F0}");
                if (stats.damageReduction != 0) sb.AppendLine($"Снижение урона: {stats.damageReduction:F0}%");
                if (stats.dodgeBonus != 0) sb.AppendLine($"Уклонение: +{stats.dodgeBonus:F0}%");

                statsSummaryText.text = sb.ToString();
            }
        }

        #endregion

        #region Equipment Event Handlers

        private void OnEquipmentEquipped(EquipmentSlot slot, EquipmentInstance instance)
        {
            if (slotMap.TryGetValue(slot, out var slotUI))
            {
                slotUI.SetEquipment(instance);
            }

            UpdateStatsDisplay();
        }

        private void OnEquipmentUnequipped(EquipmentSlot slot, EquipmentInstance instance)
        {
            if (slotMap.TryGetValue(slot, out var slotUI))
            {
                slotUI.Clear();
            }

            UpdateStatsDisplay();
        }

        private void OnStatsChanged(EquipmentStats stats)
        {
            UpdateStatsDisplay();
        }

        private void OnWeaponOffBlockChanged(bool blocked)
        {
            if (slotMap.TryGetValue(EquipmentSlot.WeaponOff, out var offSlot))
            {
                offSlot.SetBlocked(blocked);
            }
        }

        #endregion

        #region Doll Slot Event Handlers

        private void OnDollSlotHover(DollSlotUI slotUI)
        {
            dragDropHandler?.ShowTooltipForDollSlot(slotUI);
        }

        private void OnDollSlotExit(DollSlotUI slotUI)
        {
            dragDropHandler?.HideTooltip();
        }

        private void OnDollSlotRightClicked(DollSlotUI slotUI)
        {
            dragDropHandler?.ShowContextMenuForDollSlot(slotUI);
        }

        private void OnDollSlotClicked(DollSlotUI slotUI)
        {
            // Левый клик — снять предмет
            UnequipSlot(slotUI.SlotType);
        }

        private void OnDollSlotDrop(DollSlotUI slotUI)
        {
            dragDropHandler?.DropOnDollSlot(slotUI);
        }

        #endregion

        #region Equipment Actions

        /// <summary>
        /// Снимает предмет со слота и помещает в инвентарь.
        /// </summary>
        public void UnequipSlot(EquipmentSlot slot)
        {
            if (equipmentController == null || inventoryController == null) return;

            if (!equipmentController.IsSlotOccupied(slot)) return;

            var result = inventoryController.UnequipToInventory(slot);
            if (result == null)
            {
                Debug.LogWarning($"[BodyDollPanel] Не удалось снять экипировку со слота {slot} — нет места в инвентаре");
            }
        }

        /// <summary>
        /// Экипирует предмет из инвентаря в указанный слот.
        /// </summary>
        public bool EquipFromInventory(int inventorySlotId)
        {
            if (inventoryController == null) return false;

            var result = inventoryController.EquipFromInventory(inventorySlotId);
            return result != null;
        }

        /// <summary>
        /// Проверяет, можно ли экипировать предмет в указанный слот.
        /// </summary>
        public bool CanEquipInSlot(EquipmentData equipmentData)
        {
            if (equipmentController == null) return false;
            return equipmentController.CanEquip(equipmentData);
        }

        /// <summary>
        /// Получает DollSlotUI по типу слота.
        /// </summary>
        public DollSlotUI GetSlotUI(EquipmentSlot slot)
        {
            return slotMap.TryGetValue(slot, out var ui) ? ui : null;
        }

        /// <summary>
        /// Подсвечивает слоты, куда можно надеть предмет.
        /// </summary>
        public void HighlightValidSlots(EquipmentData equipmentData)
        {
            ClearAllHighlights();

            if (equipmentData == null) return;

            // Подсвечиваем целевой слот
            if (slotMap.TryGetValue(equipmentData.slot, out var targetSlot))
            {
                bool canEquip = CanEquipInSlot(equipmentData);
                targetSlot.SetDropHighlight(canEquip);
            }

            // Двуручное — подсвечиваем WeaponOff как заблокированный
            if (equipmentData.handType == WeaponHandType.TwoHand)
            {
                if (slotMap.TryGetValue(EquipmentSlot.WeaponOff, out var offSlot))
                {
                    offSlot.SetDropHighlight(false);
                }
            }
        }

        /// <summary>
        /// Сбрасывает всю подсветку.
        /// </summary>
        public void ClearAllHighlights()
        {
            foreach (var kvp in slotMap)
            {
                kvp.Value.ClearDropHighlight();
            }
        }

        #endregion
    }
}
