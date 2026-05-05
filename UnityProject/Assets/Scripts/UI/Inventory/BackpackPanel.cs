// ============================================================================
// BackpackPanel.cs — Панель рюкзака (v3.0 — строчная модель)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-18 20:00:00 UTC
// Редактировано: 2026-04-27 18:10:00 UTC — ПЕРЕПИСЬ: сетка → строчный список
// ============================================================================
// Строчная модель: VerticalLayoutGroup + ScrollRect.
// Каждая строка = 1 предмет (иконка + название + кол-во + вес + объём).
// Ограничители: масса (кг) + объём (литры).
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CultivationGame.Core;
using CultivationGame.Inventory;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.UI.Inventory
{
    /// <summary>
    /// Панель рюкзака — строчный список предметов (v3.0).
    /// Вместо сетки ячеек — список с ограничителями массы и объёма.
    /// </summary>
    public class BackpackPanel : MonoBehaviour
    {
        #region Configuration

        [Header("Prefabs")]
        [SerializeField] private GameObject slotRowPrefab;

        [Header("UI References")]
        [SerializeField] private Transform listContainer;
        [SerializeField] private TMP_Text backpackNameText;
        [SerializeField] private TMP_Text weightText;
        [SerializeField] private Slider weightBar;
        [SerializeField] private TMP_Text volumeText;
        [SerializeField] private Slider volumeBar;
        [SerializeField] private TMP_Text countText;

        #endregion

        #region Runtime Data

        private InventoryController inventoryController;
        private DragDropHandler dragDropHandler;

        /// <summary>Визуальные строки: slotId → InventorySlotUI</summary>
        private Dictionary<int, InventorySlotUI> rowUIs = new Dictionary<int, InventorySlotUI>();

        #endregion

        #region Properties

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
            RefreshList();
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
            inventoryController.OnWeightVolumeChanged += OnWeightVolumeChanged;
            inventoryController.OnBackpackChanged += OnBackpackChanged;
            inventoryController.OnInventoryRebuilt += OnInventoryRebuilt;
        }

        private void UnsubscribeFromEvents()
        {
            if (inventoryController == null) return;

            inventoryController.OnItemAdded -= OnItemAdded;
            inventoryController.OnItemRemoved -= OnItemRemoved;
            inventoryController.OnItemStackChanged -= OnItemStackChanged;
            inventoryController.OnWeightVolumeChanged -= OnWeightVolumeChanged;
            inventoryController.OnBackpackChanged -= OnBackpackChanged;
            inventoryController.OnInventoryRebuilt -= OnInventoryRebuilt;
        }

        #endregion

        #region List Management

        /// <summary>
        /// Полностью перестраивает список. Вызывается при инициализации, загрузке, смене рюкзака.
        /// </summary>
        public void RefreshList()
        {
            if (inventoryController == null) return;

            ClearList();
            CreateAllRows();
            UpdateIndicators();
            UpdateBackpackInfo();
        }

        private void ClearList()
        {
            if (listContainer != null)
            {
                for (int i = listContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(listContainer.GetChild(i).gameObject);
                }
            }

            rowUIs.Clear();
        }

        private void CreateAllRows()
        {
            if (listContainer == null || slotRowPrefab == null) return;

            foreach (var slot in inventoryController.Slots)
            {
                CreateRowUI(slot);
            }
        }

        /// <summary>
        /// Создаёт визуальную строку для одного предмета.
        /// </summary>
        private InventorySlotUI CreateRowUI(InventorySlot slot)
        {
            if (slotRowPrefab == null || listContainer == null) return null;

            var rowGO = Instantiate(slotRowPrefab, listContainer);
            // FIX ИСП-ИНВ-01: Активировать клон после Instantiate — префаб может быть деактивирован
            rowGO.SetActive(true);
            var rowUI = rowGO.GetComponent<InventorySlotUI>();

            if (rowUI == null)
                rowUI = rowGO.AddComponent<InventorySlotUI>();

            rowUI.SetSlot(slot, this);

            // Подписка на drag & контекстное меню
            rowUI.OnDragBegin += OnRowDragBegin;
            rowUI.OnDragging += OnRowDragging;
            rowUI.OnDragEnd += OnRowDragEnd;
            rowUI.OnSlotRightClicked += OnRowRightClicked;
            rowUI.OnSlotHover += OnRowHover;
            rowUI.OnSlotExit += OnRowExit;

            rowUIs[slot.SlotId] = rowUI;
            return rowUI;
        }

        /// <summary>
        /// Удаляет визуальную строку предмета.
        /// </summary>
        private void RemoveRowUI(int slotId)
        {
            if (rowUIs.TryGetValue(slotId, out var rowUI))
            {
                Destroy(rowUI.gameObject);
                rowUIs.Remove(slotId);
            }
        }

        #endregion

        #region Event Handlers (from InventoryController)

        private void OnItemAdded(InventorySlot slot)
        {
            CreateRowUI(slot);
            UpdateIndicators();
        }

        private void OnItemRemoved(InventorySlot slot)
        {
            RemoveRowUI(slot.SlotId);
            UpdateIndicators();
        }

        private void OnItemStackChanged(InventorySlot slot, int newCount)
        {
            if (rowUIs.TryGetValue(slot.SlotId, out var rowUI))
            {
                rowUI.UpdateCount(newCount);
            }
            UpdateIndicators();
        }

        private void OnWeightVolumeChanged(float effWeight, float maxWeight, float totalVolume, float maxVolume)
        {
            UpdateIndicators();
        }

        private void OnBackpackChanged(BackpackData newBackpack)
        {
            RefreshList();
        }

        private void OnInventoryRebuilt()
        {
            RefreshList();
        }

        #endregion

        #region UI Event Handlers (from RowUI)

        private void OnRowDragBegin(InventorySlotUI rowUI, UnityEngine.EventSystems.PointerEventData eventData)
        {
            dragDropHandler?.BeginDrag(rowUI, eventData);
        }

        private void OnRowDragging(InventorySlotUI rowUI, UnityEngine.EventSystems.PointerEventData eventData)
        {
            dragDropHandler?.UpdateDrag(rowUI, eventData);
        }

        private void OnRowDragEnd(InventorySlotUI rowUI, UnityEngine.EventSystems.PointerEventData eventData)
        {
            dragDropHandler?.EndDrag(rowUI, eventData);
        }

        private void OnRowRightClicked(InventorySlotUI rowUI)
        {
            dragDropHandler?.ShowContextMenu(rowUI);
        }

        private void OnRowHover(InventorySlotUI rowUI)
        {
            dragDropHandler?.ShowTooltip(rowUI);
        }

        private void OnRowExit(InventorySlotUI rowUI)
        {
            dragDropHandler?.HideTooltip();
        }

        #endregion

        #region Visual Updates

        private void UpdateIndicators()
        {
            if (inventoryController == null) return;

            // Вес
            if (weightText != null)
            {
                weightText.text = $"{inventoryController.EffectiveWeight:F1} / {inventoryController.MaxWeight:F1} кг";
                weightText.color = inventoryController.IsOverencumbered
                    ? new Color(1f, 0.3f, 0.3f)
                    : Color.white;
            }

            if (weightBar != null)
            {
                weightBar.maxValue = inventoryController.MaxWeight > 0 ? inventoryController.MaxWeight : 1f;
                weightBar.value = inventoryController.EffectiveWeight;
            }

            // Объём
            if (volumeText != null)
            {
                volumeText.text = $"{inventoryController.TotalVolume:F1} / {inventoryController.MaxVolume:F1} л";
                volumeText.color = inventoryController.IsOverVolume
                    ? new Color(1f, 0.3f, 0.3f)
                    : Color.white;
            }

            if (volumeBar != null)
            {
                volumeBar.maxValue = inventoryController.MaxVolume > 0 ? inventoryController.MaxVolume : 1f;
                volumeBar.value = inventoryController.TotalVolume;
            }

            // Количество предметов
            if (countText != null)
            {
                countText.text = $"{inventoryController.UsedSlots} предм.";
            }
        }

        private void UpdateBackpackInfo()
        {
            if (inventoryController == null) return;

            var backpack = inventoryController.CurrentBackpack;

            if (backpackNameText != null)
                backpackNameText.text = backpack != null ? backpack.nameRu : "Тканевая сумка";
        }

        #endregion
    }
}
