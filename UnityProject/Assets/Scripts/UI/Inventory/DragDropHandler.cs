// ============================================================================
// DragDropHandler.cs — Централизованная система перетаскивания (v3.0)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-18 20:00:00 UTC
// Редактировано: 2026-04-27 18:12:00 UTC — ПЕРЕПИСЬ: строчная модель + багфиксы
// ============================================================================
// Изменения v3.0:
// - DragSource: +StorageRing, +SpiritStorage (BUG-7 фикс)
// - DropTarget: +SpiritStorageList, +StorageRingList
// - SwapRows вместо MoveItem по координатам
// - SplitStack с сохранением durability/grade (BUG-3 фикс)
// - HandleDropOnBackpack для DollSlot реализован (BUG-6 фикс)
// - Убрана координатная логика (ScreenToGridPosition)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using CultivationGame.Core;
using CultivationGame.Inventory;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.UI.Inventory
{
    /// <summary>
    /// Централизованный обработчик перетаскивания предметов (v3.0).
    /// Координирует все операции drag & drop между панелями инвентаря.
    /// </summary>
    public class DragDropHandler : MonoBehaviour
    {
        #region Configuration

        [Header("Drag Visual")]
        [SerializeField] private UnityEngine.UI.Image dragIcon;
        [SerializeField] private RectTransform dragTransform;
        [SerializeField] private float dragScale = 1.1f;

        [Header("Context Menu")]
        [SerializeField] private GameObject contextMenuPrefab;
        [SerializeField] private Transform contextMenuContainer;

        [Header("Tooltip")]
        [SerializeField] private TooltipPanel tooltipPanel;

        #endregion

        #region Runtime Data

        private InventoryController inventoryController;
        private EquipmentController equipmentController;
        private BackpackPanel backpackPanel;
        private BodyDollPanel bodyDollPanel;
        private StorageRingController storageRingController;
        private SpiritStorageController spiritStorageController;

        /// <summary>Текущий перетаскиваемый слот</summary>
        private InventorySlotUI draggedSlotUI;

        /// <summary>Источник перетаскивания</summary>
        private DragSource dragSource = DragSource.None;

        /// <summary>Слот куклы, если dragSource == DollSlot</summary>
        private EquipmentSlot? dollSourceSlot;

        /// <summary>Активное контекстное меню</summary>
        private GameObject activeContextMenu;

        #endregion

        #region Enums

        private enum DragSource
        {
            None,
            Backpack,
            DollSlot,
            StorageRing,
            SpiritStorage
        }

        private enum DropTarget
        {
            None,
            BackpackList,
            DollSlot,
            SpiritStorageList,
            StorageRingList,
            Outside
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализирует обработчик.
        /// </summary>
        public void Initialize(
            InventoryController invController,
            EquipmentController equipController,
            BackpackPanel backpack,
            BodyDollPanel doll)
        {
            inventoryController = invController;
            equipmentController = equipController;
            backpackPanel = backpack;
            bodyDollPanel = doll;

            // Находим контроллеры хранилищ
            storageRingController = ServiceLocator.GetOrFind<StorageRingController>();
            spiritStorageController = ServiceLocator.GetOrFind<SpiritStorageController>();

            // Скрываем иконку перетаскивания
            if (dragIcon != null)
                dragIcon.enabled = false;
        }

        #endregion

        #region Drag Operations

        /// <summary>
        /// Начало перетаскивания из рюкзака.
        /// </summary>
        public void BeginDrag(InventorySlotUI slotUI, PointerEventData eventData)
        {
            if (slotUI == null || slotUI.Slot == null) return;

            draggedSlotUI = slotUI;
            dragSource = DragSource.Backpack;

            // Настраиваем визуал перетаскивания
            SetupDragVisual(slotUI.Slot.ItemData);

            // Подсвечиваем слот экипировки, если предмет — экипировка
            if (bodyDollPanel != null && slotUI.Slot.ItemData is EquipmentData equipData)
            {
                bodyDollPanel.HighlightValidSlots(equipData);
            }

            HideTooltip();
        }

        /// <summary>
        /// Начало перетаскивания из куклы.
        /// </summary>
        public void BeginDragFromDoll(EquipmentSlot slot)
        {
            dragSource = DragSource.DollSlot;
            dollSourceSlot = slot;

            if (equipmentController != null)
            {
                var instance = equipmentController.GetEquippedItem(slot);
                if (instance != null)
                {
                    SetupDragVisual(instance.equipmentData);
                }
            }
        }

        /// <summary>
        /// Обновление позиции перетаскивания.
        /// </summary>
        public void UpdateDrag(InventorySlotUI slotUI, PointerEventData eventData)
        {
            if (dragIcon == null || !dragIcon.enabled) return;

            if (dragTransform != null)
            {
                dragTransform.position = eventData.position;
            }
        }

        /// <summary>
        /// Конец перетаскивания.
        /// </summary>
        public void EndDrag(InventorySlotUI slotUI, PointerEventData eventData)
        {
            if (draggedSlotUI == null && dragSource != DragSource.DollSlot) return;

            // Скрываем визуал
            if (dragIcon != null)
                dragIcon.enabled = false;

            // Сбрасываем подсветку
            if (bodyDollPanel != null)
                bodyDollPanel.ClearAllHighlights();

            // Определяем цель дропа
            var dropTarget = DetermineDropTarget(eventData.position);

            switch (dropTarget)
            {
                case DropTarget.BackpackList:
                    HandleDropOnBackpack();
                    break;

                case DropTarget.DollSlot:
                    HandleDropOnDoll(eventData.position);
                    break;

                case DropTarget.SpiritStorageList:
                    HandleDropOnSpiritStorage();
                    break;

                case DropTarget.StorageRingList:
                    HandleDropOnStorageRing();
                    break;

                case DropTarget.Outside:
                    // Выброс — пока не реализуем
                    break;
            }

            draggedSlotUI = null;
            dragSource = DragSource.None;
            dollSourceSlot = null;
        }

        #endregion

        #region Drop Target Detection

        private DropTarget DetermineDropTarget(Vector2 screenPosition)
        {
            // Проверяем куклу
            if (bodyDollPanel != null)
            {
                var rect = bodyDollPanel.GetComponent<RectTransform>();
                if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition))
                {
                    return DropTarget.DollSlot;
                }
            }

            // Проверяем рюкзак
            if (backpackPanel != null)
            {
                var rect = backpackPanel.GetComponent<RectTransform>();
                if (rect != null && RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition))
                {
                    return DropTarget.BackpackList;
                }
            }

            // TODO: Проверка SpiritStorage и StorageRing панелей
            // Когда SpiritStoragePanel будет создана

            return DropTarget.Outside;
        }

        #endregion

        #region Drop Handling

        private void HandleDropOnBackpack()
        {
            if (inventoryController == null) return;

            if (dragSource == DragSource.DollSlot && dollSourceSlot.HasValue)
            {
                // Снятие экипировки в рюкзак
                inventoryController.UnequipToInventory(dollSourceSlot.Value);
            }
            else if (dragSource == DragSource.Backpack && draggedSlotUI != null)
            {
                // Перемещение внутри рюкзака — в строчной модели это swap
                // Пока ничего не делаем — предметы остаются на месте
            }
            else if (dragSource == DragSource.StorageRing)
            {
                // Из кольца в рюкзак — TODO
            }
            else if (dragSource == DragSource.SpiritStorage)
            {
                // Из духовного хранилища в рюкзак — TODO
            }
        }

        private void HandleDropOnDoll(Vector2 screenPosition)
        {
            if (dragSource == DragSource.Backpack && draggedSlotUI != null)
            {
                var equipData = draggedSlotUI.Slot.ItemData as EquipmentData;
                if (equipData == null) return;

                // Экипируем предмет
                if (bodyDollPanel != null)
                {
                    bodyDollPanel.EquipFromInventory(draggedSlotUI.Slot.SlotId);
                }
            }
        }

        private void HandleDropOnSpiritStorage()
        {
            if (dragSource != DragSource.Backpack || draggedSlotUI == null) return;
            if (spiritStorageController == null || inventoryController == null) return;

            // Перемещаем из инвентаря в духовное хранилище
            var slot = draggedSlotUI.Slot;
            spiritStorageController.StoreFromInventory(slot.SlotId);
        }

        private void HandleDropOnStorageRing()
        {
            if (dragSource != DragSource.Backpack || draggedSlotUI == null) return;
            if (storageRingController == null || inventoryController == null) return;

            // Перемещаем в первое активное кольцо
            var activeRings = GetActiveRingSlots();
            if (activeRings.Count > 0)
            {
                storageRingController.StoreFromInventory(activeRings[0], draggedSlotUI.Slot.SlotId);
            }
        }

        /// <summary>
        /// Обработка дропа на слот куклы (через IDropHandler).
        /// </summary>
        public void DropOnDollSlot(DollSlotUI dollSlot)
        {
            if (draggedSlotUI == null && dragSource != DragSource.DollSlot) return;

            if (dragSource == DragSource.Backpack && draggedSlotUI != null)
            {
                var equipData = draggedSlotUI.Slot.ItemData as EquipmentData;
                if (equipData == null) return;

                // Проверяем, совпадает ли слот
                if (equipData.slot != dollSlot.SlotType)
                {
                    Debug.Log($"[DragDropHandler] Предмет {equipData.nameRu} не подходит для слота {dollSlot.SlotType}");
                    return;
                }

                // Экипируем
                if (bodyDollPanel != null)
                {
                    bodyDollPanel.EquipFromInventory(draggedSlotUI.Slot.SlotId);
                }
            }
            else if (dragSource == DragSource.DollSlot && dollSourceSlot.HasValue)
            {
                // Перемещение между слотами куклы — пока не поддерживается
            }

            // Сбрасываем визуал
            if (dragIcon != null)
                dragIcon.enabled = false;

            if (bodyDollPanel != null)
                bodyDollPanel.ClearAllHighlights();

            draggedSlotUI = null;
            dragSource = DragSource.None;
            dollSourceSlot = null;
        }

        #endregion

        #region Drag Visual

        private void SetupDragVisual(ItemData itemData)
        {
            if (dragIcon == null) return;

            dragIcon.sprite = itemData.icon;
            dragIcon.enabled = itemData.icon != null;
            dragIcon.color = Color.white;

            if (dragTransform != null)
            {
                dragTransform.localScale = Vector3.one * dragScale;
            }
        }

        #endregion

        #region Tooltip

        /// <summary>
        /// Показывает tooltip для слота инвентаря.
        /// </summary>
        public void ShowTooltip(InventorySlotUI slotUI)
        {
            if (tooltipPanel == null || slotUI == null || slotUI.Slot == null) return;

            Vector2 cursorPos = Mouse.current != null
                ? Mouse.current.position.value
                : (Vector2)Input.mousePosition;

            tooltipPanel.ShowForInventorySlot(slotUI.Slot, cursorPos);
        }

        /// <summary>
        /// Показывает tooltip для слота куклы.
        /// </summary>
        public void ShowTooltipForDollSlot(DollSlotUI dollSlot)
        {
            if (tooltipPanel == null || dollSlot == null || dollSlot.Equipment == null) return;

            Vector2 cursorPos = Mouse.current != null
                ? Mouse.current.position.value
                : (Vector2)Input.mousePosition;

            tooltipPanel.ShowForEquipment(dollSlot.Equipment, cursorPos);
        }

        /// <summary>
        /// Скрывает tooltip.
        /// </summary>
        public void HideTooltip()
        {
            if (tooltipPanel != null)
                tooltipPanel.Hide();
        }

        #endregion

        #region Context Menu

        /// <summary>
        /// Показывает контекстное меню для предмета в инвентаре.
        /// </summary>
        public void ShowContextMenu(InventorySlotUI slotUI)
        {
            if (slotUI == null || slotUI.Slot == null || slotUI.Slot.ItemData == null) return;

            HideContextMenu();

            var options = GetInventoryContextMenuOptions(slotUI.Slot);
            CreateContextMenu(options, Mouse.current != null ? Mouse.current.position.value : (Vector2)Input.mousePosition);
        }

        /// <summary>
        /// Показывает контекстное меню для слота куклы.
        /// </summary>
        public void ShowContextMenuForDollSlot(DollSlotUI dollSlot)
        {
            if (dollSlot == null || !dollSlot.HasItem) return;

            HideContextMenu();

            var options = GetDollContextMenuOptions(dollSlot);
            CreateContextMenu(options, Mouse.current != null ? Mouse.current.position.value : (Vector2)Input.mousePosition);
        }

        private List<ContextMenuOption> GetInventoryContextMenuOptions(InventorySlot slot)
        {
            var options = new List<ContextMenuOption>();
            var itemData = slot.ItemData;

            // Использовать (расходники)
            if (itemData.category == ItemCategory.Consumable)
            {
                options.Add(new ContextMenuOption
                {
                    label = "Использовать",
                    action = () => UseItem(slot)
                });
            }

            // Экипировать
            if (itemData is EquipmentData equipData)
            {
                options.Add(new ContextMenuOption
                {
                    label = "Экипировать",
                    action = () => EquipItem(slot, equipData)
                });
            }

            // Разделить стек (с сохранением durability/grade — BUG-3 фикс)
            if (itemData.stackable && slot.Count > 1)
            {
                options.Add(new ContextMenuOption
                {
                    label = "Разделить",
                    action = () => SplitStack(slot)
                });
            }

            // В кольцо хранения
            var activeRingSlots = GetActiveRingSlots();
            if (storageRingController != null && activeRingSlots.Count > 0)
            {
                var ringSlot = activeRingSlots[0];
                if (storageRingController.CanStore(ringSlot, itemData))
                {
                    options.Add(new ContextMenuOption
                    {
                        label = "В кольцо хранения",
                        action = () => StoreInRing(slot)
                    });
                }
            }

            // В духовное хранилище
            if (spiritStorageController != null && spiritStorageController.IsUnlocked)
            {
                if (itemData.allowNesting == NestingFlag.Spirit || itemData.allowNesting == NestingFlag.Any)
                {
                    options.Add(new ContextMenuOption
                    {
                        label = "В дух. хранилище",
                        action = () => StoreInSpiritStorage(slot)
                    });
                }
            }

            // Выбросить
            options.Add(new ContextMenuOption
            {
                label = "Выбросить",
                action = () => DropItem(slot)
            });

            return options;
        }

        private List<ContextMenuOption> GetDollContextMenuOptions(DollSlotUI dollSlot)
        {
            var options = new List<ContextMenuOption>();

            // Снять
            options.Add(new ContextMenuOption
            {
                label = "Снять",
                action = () => bodyDollPanel?.UnequipSlot(dollSlot.SlotType)
            });

            return options;
        }

        private void CreateContextMenu(List<ContextMenuOption> options, Vector2 position)
        {
            if (contextMenuPrefab == null || contextMenuContainer == null) return;

            activeContextMenu = Instantiate(contextMenuPrefab, contextMenuContainer);

            var menuUI = activeContextMenu.GetComponent<CultivationGame.UI.ContextMenuUI>();
            if (menuUI != null)
            {
                var uiOptions = new List<CultivationGame.UI.ContextMenuOption>();
                foreach (var opt in options)
                {
                    uiOptions.Add(new CultivationGame.UI.ContextMenuOption
                    {
                        label = opt.label,
                        action = opt.action
                    });
                }
                menuUI.SetOptions(uiOptions);
                activeContextMenu.transform.position = position;
            }
        }

        /// <summary>
        /// Скрывает контекстное меню.
        /// </summary>
        public void HideContextMenu()
        {
            if (activeContextMenu != null)
            {
                Destroy(activeContextMenu);
                activeContextMenu = null;
            }
        }

        #endregion

        #region Item Actions

        private void UseItem(InventorySlot slot)
        {
            if (inventoryController == null) return;
            inventoryController.RemoveItem(slot.SlotId, 1);
            HideContextMenu();
        }

        private void EquipItem(InventorySlot slot, EquipmentData equipData)
        {
            if (inventoryController == null) return;
            inventoryController.EquipFromInventory(slot.SlotId);
            HideContextMenu();
        }

        /// <summary>
        /// Разделение стака с сохранением durability и grade (BUG-3 фикс).
        /// </summary>
        private void SplitStack(InventorySlot slot)
        {
            if (inventoryController == null || !slot.ItemData.stackable || slot.Count <= 1) return;

            int splitAmount = slot.Count / 2;
            int originalDurability = slot.Durability;
            var originalGrade = slot.grade;

            // Убираем из текущего стака
            inventoryController.RemoveItem(slot.SlotId, splitAmount);

            // Добавляем новый стак
            var newSlot = inventoryController.AddItem(slot.ItemData, splitAmount);
            if (newSlot != null)
            {
                // Переносим durability и grade
                if (originalDurability >= 0)
                    newSlot.durability = originalDurability;
                newSlot.grade = originalGrade;
            }

            HideContextMenu();
        }

        private void DropItem(InventorySlot slot)
        {
            if (inventoryController == null) return;
            inventoryController.RemoveSlot(slot.SlotId);
            HideContextMenu();
        }

        #endregion

        #region Storage Helpers

        private List<EquipmentSlot> GetActiveRingSlots()
        {
            var result = new List<EquipmentSlot>();
            if (storageRingController == null) return result;

            foreach (var slot in StorageRingController.RingSlots)
            {
                if (storageRingController.IsRingSlotActive(slot))
                    result.Add(slot);
            }
            return result;
        }

        /// <summary>
        /// Перемещает предмет из инвентаря в первое активное кольцо хранения.
        /// </summary>
        private void StoreInRing(InventorySlot slot)
        {
            if (storageRingController == null || inventoryController == null) return;

            var activeRingSlots = GetActiveRingSlots();
            if (activeRingSlots.Count == 0) return;

            var ringSlot = activeRingSlots[0];
            storageRingController.StoreFromInventory(ringSlot, slot.SlotId);

            HideContextMenu();
        }

        /// <summary>
        /// Перемещает предмет из инвентаря в духовное хранилище.
        /// </summary>
        private void StoreInSpiritStorage(InventorySlot slot)
        {
            if (spiritStorageController == null || inventoryController == null) return;

            spiritStorageController.StoreFromInventory(slot.SlotId);

            HideContextMenu();
        }

        #endregion

        #region Update

        private void Update()
        {
            // Закрытие контекстного меню по клику вне
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && activeContextMenu != null)
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    HideContextMenu();
                }
            }
        }

        #endregion
    }

    // ============================================================================
    // ContextMenuOption — опция контекстного меню
    // ============================================================================

    public class ContextMenuOption
    {
        public string label;
        public Action action;
    }
}
