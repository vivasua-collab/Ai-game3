// ============================================================================
// InventoryController.cs — Система инвентаря (v2.0 — рюкзак + багфиксы)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-03
// Редактировано: 2026-04-18 19:15:00 UTC — ПОЛНАЯ ПЕРЕРАБОТКА по INVENTORY_UI_DRAFT.md v2.0
// ============================================================================
// Изменения v2.0:
// - Динамическая сетка от BackpackData (не фиксированная 8×6)
// - SetBackpack() + effectiveWeight с учётом weightReduction
// - Мост EquipFromInventory / UnequipToInventory
// - FIX INV-BUG-01: Рекурсивный AddItem → итеративный (вес корректен)
// - FIX INV-BUG-02: RemoveFromSlot — вес до удаления из списка
// - FIX INV-BUG-03: Resize — полная перестройка occupancy
// - FIX INV-BUG-04: FreeGrid — перестройка вместо побитового сброса
// - FIX INV-BUG-05: HasDurability при durability=0 → true
// - FIX INV-BUG-06: LoadSaveData durability=0 → загружается
// - FIX INV-BUG-07: FreeSlots считает ячейки, а не предметы
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Inventory
{
    /// <summary>
    /// Контроллер инвентаря (v2.0 — Diablo-style сетка + рюкзак).
    /// 
    /// Размер сетки определяется надетым рюкзаком (BackpackData).
    /// Стартовый: 3×4 (12 ячеек). При смене рюкзака — пересоздаётся.
    /// 
    /// Механики:
    /// - Вес: effectiveWeight = totalWeight × (1 − backpack.weightReduction)
    /// - Максимальный вес: maxWeight + backpack.maxWeightBonus
    /// - Предметы 1×1, 2×1, 1×2, 2×2
    /// - Стак стекируемых предметов (maxStack)
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        #region Configuration

        [Header("Base Settings (overridden by backpack)")]
        [Tooltip("Базовый максимальный вес (без рюкзака)")]
        public float baseMaxWeight = 30f;

        [Tooltip("Включить ограничение веса")]
        public bool useWeightLimit = true;

        [Header("Default Grid (no backpack)")]
        [Tooltip("Ширина сетки по умолчанию (без рюкзака)")]
        [Range(3, 10)]
        public int defaultGridWidth = 3;

        [Tooltip("Высота сетки по умолчанию (без рюкзака)")]
        [Range(3, 8)]
        public int defaultGridHeight = 4;

        [Header("References")]
        [Tooltip("Контроллер экипировки (для моста)")]
        public EquipmentController equipmentController;

        #endregion

        #region Runtime Data

        /// <summary>Текущий рюкзак (null = стартовая сумка)</summary>
        private BackpackData currentBackpack;

        /// <summary>Ширина сетки (определяется рюкзаком)</summary>
        private int gridWidth;

        /// <summary>Высота сетки (определяется рюкзаком)</summary>
        private int gridHeight;

        /// <summary>Сетка занятости: slotId → true (ячейка занята)</summary>
        private int[,] gridSlotIds;

        /// <summary>Список всех слотов</summary>
        private List<InventorySlot> slots = new List<InventorySlot>();

        /// <summary>Сырой вес (без снижения от рюкзака)</summary>
        private float rawWeight = 0f;

        /// <summary>Счётчик ID для слотов</summary>
        private int nextSlotId = 0;

        #endregion

        #region Events

        public event Action<InventorySlot> OnItemAdded;
        public event Action<InventorySlot> OnItemRemoved;
        public event Action<InventorySlot, int> OnItemStackChanged;
        public event Action<float, float> OnWeightChanged;
        public event Action OnInventoryFull;
        /// <summary>Рюкзак изменён (сменился или снялся)</summary>
        public event Action<BackpackData> OnBackpackChanged;

        #endregion

        #region Properties

        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public int TotalSlots => gridWidth * gridHeight;

        /// <summary>Количество занятых ячеек (FIX INV-BUG-07)</summary>
        public int UsedCells
        {
            get
            {
                int count = 0;
                for (int x = 0; x < gridWidth; x++)
                    for (int y = 0; y < gridHeight; y++)
                        if (gridSlotIds[x, y] >= 0)
                            count++;
                return count;
            }
        }

        /// <summary>Количество свободных ячеек (FIX INV-BUG-07)</summary>
        public int FreeCells => TotalSlots - UsedCells;

        /// <summary>Количество предметов (для совместимости)</summary>
        public int UsedSlots => slots.Count;

        /// <summary>Эффективный вес с учётом снижения от рюкзака</summary>
        public float EffectiveWeight
        {
            get
            {
                float reduction = currentBackpack != null ? currentBackpack.weightReduction / 100f : 0f;
                return rawWeight * (1f - reduction);
            }
        }

        /// <summary>Сырой вес (без снижения)</summary>
        public float RawWeight => rawWeight;

        /// <summary>Максимальный вес с учётом бонуса рюкзака</summary>
        public float MaxWeight
        {
            get
            {
                float bonus = currentBackpack != null ? currentBackpack.maxWeightBonus : 0f;
                return baseMaxWeight + bonus;
            }
        }

        /// <summary>Процент веса</summary>
        public float WeightPercent => MaxWeight > 0 ? EffectiveWeight / MaxWeight : 0f;

        /// <summary>Перегруз</summary>
        public bool IsOverencumbered => useWeightLimit && EffectiveWeight > MaxWeight;

        /// <summary>Текущий рюкзак</summary>
        public BackpackData CurrentBackpack => currentBackpack;

        /// <summary>Доступ к слотам (только чтение)</summary>
        public IReadOnlyList<InventorySlot> Slots => slots.AsReadOnly();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            ApplyGridSize(defaultGridWidth, defaultGridHeight);
        }

        #endregion

        #region Backpack Management

        /// <summary>
        /// Устанавливает рюкзак. Изменяет размер сетки.
        /// Если старый рюкзак не пуст — предметы сохраняются (если помещаются).
        /// </summary>
        /// <returns>true если рюкзак успешно установлен</returns>
        public bool SetBackpack(BackpackData backpackData)
        {
            int newWidth = backpackData != null ? backpackData.gridWidth : defaultGridWidth;
            int newHeight = backpackData != null ? backpackData.gridHeight : defaultGridHeight;

            // Проверяем, помещаются ли предметы в новую сетку
            foreach (var slot in slots)
            {
                if (slot.GridX + slot.ItemWidth > newWidth ||
                    slot.GridY + slot.ItemHeight > newHeight)
                {
                    // Предмет не помещается — нельзя сменить рюкзак
                    Debug.LogWarning($"[InventoryController] Нельзя сменить рюкзак: предмет {slot.ItemData?.nameRu} не помещается");
                    return false;
                }
            }

            currentBackpack = backpackData;

            // Пересоздаём сетку, сохраняя предметы
            ApplyGridSize(newWidth, newHeight);

            OnBackpackChanged?.Invoke(currentBackpack);
            OnWeightChanged?.Invoke(EffectiveWeight, MaxWeight);

            return true;
        }

        /// <summary>
        /// Снимает рюкзак. Возвращает false если в новой сетке не помещаются предметы.
        /// </summary>
        public bool RemoveBackpack()
        {
            return SetBackpack(null);
        }

        #endregion

        #region Grid Initialization

        private void ApplyGridSize(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;

            // Создаём пустую сетку (-1 = свободно)
            gridSlotIds = new int[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    gridSlotIds[x, y] = -1;

            // Восстанавливаем занятые ячейки из существующих слотов
            foreach (var slot in slots)
            {
                if (slot.GridX + slot.ItemWidth <= width &&
                    slot.GridY + slot.ItemHeight <= height)
                {
                    OccupyGrid(slot.SlotId, slot.GridX, slot.GridY, slot.ItemWidth, slot.ItemHeight);
                }
            }

            nextSlotId = slots.Count > 0 ? Mathf.Max(nextSlotId, slots[slots.Count - 1].SlotId + 1) : 0;
        }

        /// <summary>
        /// Перестраивает сетку занятости из слотов (FIX INV-BUG-04).
        /// Используется вместо побитового FreeGrid для предотвращения пересечений.
        /// </summary>
        private void RebuildGridFromSlots()
        {
            for (int x = 0; x < gridWidth; x++)
                for (int y = 0; y < gridHeight; y++)
                    gridSlotIds[x, y] = -1;

            foreach (var slot in slots)
            {
                OccupyGrid(slot.SlotId, slot.GridX, slot.GridY, slot.ItemWidth, slot.ItemHeight);
            }
        }

        #endregion

        #region Add Item

        /// <summary>
        /// Добавляет предмет в инвентарь (итеративно, FIX INV-BUG-01).
        /// Сначала заполняет существующие стакы, затем — новые слоты.
        /// </summary>
        public InventorySlot AddItem(ItemData itemData, int count = 1)
        {
            if (itemData == null || count <= 0)
                return null;

            int remaining = count;
            InventorySlot lastSlot = null;

            // Шаг 1: Заполняем существующие стаки (если предмет стакается)
            if (itemData.stackable)
            {
                foreach (var slot in slots)
                {
                    if (remaining <= 0) break;
                    if (slot.ItemData.itemId != itemData.itemId) continue;
                    if (slot.Count >= itemData.maxStack) continue;

                    int spaceLeft = itemData.maxStack - slot.Count;
                    int toAdd = Mathf.Min(remaining, spaceLeft);

                    // Проверяем вес
                    if (useWeightLimit && EffectiveWeight + itemData.weight * toAdd > MaxWeight)
                    {
                        // Пробуем добавить сколько помещается по весу
                        float availableWeight = MaxWeight - EffectiveWeight;
                        int canAfford = availableWeight > 0 ? Mathf.FloorToInt(availableWeight / itemData.weight) : 0;
                        toAdd = Mathf.Min(toAdd, canAfford);
                        if (toAdd <= 0)
                        {
                            OnInventoryFull?.Invoke();
                            return lastSlot;
                        }
                    }

                    slot.AddCount(toAdd);
                    rawWeight += itemData.weight * toAdd;
                    remaining -= toAdd;

                    OnItemStackChanged?.Invoke(slot, slot.Count);
                    lastSlot = slot;
                }
            }

            // Шаг 2: Создаём новые слоты для оставшихся предметов
            while (remaining > 0)
            {
                var position = FindFreePosition(itemData.sizeWidth, itemData.sizeHeight);
                if (!position.HasValue)
                {
                    OnInventoryFull?.Invoke();
                    return lastSlot;
                }

                int toPlace = Mathf.Min(remaining, itemData.maxStack);

                // Проверяем вес
                if (useWeightLimit && EffectiveWeight + itemData.weight * toPlace > MaxWeight)
                {
                    float availableWeight = MaxWeight - EffectiveWeight;
                    int canAfford = availableWeight > 0 ? Mathf.FloorToInt(availableWeight / itemData.weight) : 0;
                    toPlace = Mathf.Min(toPlace, canAfford);
                    if (toPlace <= 0)
                    {
                        OnInventoryFull?.Invoke();
                        return lastSlot;
                    }
                }

                var newSlot = CreateSlot(itemData, position.Value.x, position.Value.y, toPlace);
                slots.Add(newSlot);
                OccupyGrid(newSlot.SlotId, position.Value.x, position.Value.y, itemData.sizeWidth, itemData.sizeHeight);

                rawWeight += itemData.weight * toPlace;
                remaining -= toPlace;

                OnItemAdded?.Invoke(newSlot);
                lastSlot = newSlot;
            }

            OnWeightChanged?.Invoke(EffectiveWeight, MaxWeight);
            return lastSlot;
        }

        /// <summary>
        /// Добавляет предмет в конкретную позицию
        /// </summary>
        public InventorySlot AddItemAt(ItemData itemData, int gridX, int gridY, int count = 1)
        {
            if (itemData == null || count <= 0)
                return null;

            if (!IsValidPosition(gridX, gridY, itemData.sizeWidth, itemData.sizeHeight))
                return null;

            if (!IsAreaFree(gridX, gridY, itemData.sizeWidth, itemData.sizeHeight))
                return null;

            if (useWeightLimit)
            {
                float addedWeight = itemData.weight * count;
                if (EffectiveWeight + addedWeight > MaxWeight)
                {
                    OnInventoryFull?.Invoke();
                    return null;
                }
            }

            var newSlot = CreateSlot(itemData, gridX, gridY, count);
            slots.Add(newSlot);
            OccupyGrid(newSlot.SlotId, gridX, gridY, itemData.sizeWidth, itemData.sizeHeight);

            rawWeight += itemData.weight * count;

            OnItemAdded?.Invoke(newSlot);
            OnWeightChanged?.Invoke(EffectiveWeight, MaxWeight);
            return newSlot;
        }

        #endregion

        #region Remove Item

        /// <summary>
        /// Удаляет предмет из слота
        /// </summary>
        public bool RemoveItem(int slotId, int count = 1)
        {
            var slot = FindSlotById(slotId);
            if (slot == null)
                return false;

            return RemoveFromSlot(slot, count);
        }

        /// <summary>
        /// Удаляет предмет по ID
        /// </summary>
        public int RemoveItemById(string itemId, int count = 1)
        {
            int removed = 0;

            for (int i = slots.Count - 1; i >= 0 && removed < count; i--)
            {
                var slot = slots[i];
                if (slot.ItemData.itemId == itemId)
                {
                    int toRemove = Mathf.Min(count - removed, slot.Count);
                    if (RemoveFromSlot(slot, toRemove))
                    {
                        removed += toRemove;
                    }
                }
            }

            return removed;
        }

        /// <summary>
        /// Удаляет всё содержимое слота
        /// </summary>
        public bool RemoveSlot(int slotId)
        {
            var slot = FindSlotById(slotId);
            if (slot == null)
                return false;

            return RemoveFromSlot(slot, slot.Count);
        }

        /// <summary>
        /// Очищает весь инвентарь
        /// </summary>
        public void Clear()
        {
            slots.Clear();
            ApplyGridSize(gridWidth, gridHeight);
            rawWeight = 0f;
            OnWeightChanged?.Invoke(0f, MaxWeight);
        }

        /// <summary>
        /// Удаляет предметы из слота (FIX INV-BUG-02: вес вычисляется до модификации).
        /// </summary>
        private bool RemoveFromSlot(InventorySlot slot, int count)
        {
            if (count <= 0) return false;
            if (count > slot.Count)
                count = slot.Count;

            // Вычисляем вес ДО модификации слота
            float weightDelta = -slot.ItemData.weight * count;

            // Обновляем слот
            slot.RemoveCount(count);

            // Обновляем вес
            rawWeight += weightDelta;
            rawWeight = Mathf.Max(0f, rawWeight);

            // Если слот пуст, удаляем его
            if (slot.Count <= 0)
            {
                // FIX INV-BUG-04: перестраиваем сетку вместо побитового FreeGrid
                slots.Remove(slot);
                RebuildGridFromSlots();
                OnItemRemoved?.Invoke(slot);
            }
            else
            {
                OnItemStackChanged?.Invoke(slot, slot.Count);
            }

            OnWeightChanged?.Invoke(EffectiveWeight, MaxWeight);
            return true;
        }

        #endregion

        #region Move Item

        /// <summary>
        /// Перемещает предмет в новую позицию
        /// </summary>
        public bool MoveItem(int slotId, int newGridX, int newGridY)
        {
            var slot = FindSlotById(slotId);
            if (slot == null)
                return false;

            if (!IsValidPosition(newGridX, newGridY, slot.ItemWidth, slot.ItemHeight))
                return false;

            // Освобождаем старое место и проверяем новое
            ClearSlotFromGrid(slot.SlotId);

            if (!IsAreaFree(newGridX, newGridY, slot.ItemWidth, slot.ItemHeight))
            {
                // Возвращаем старое место
                OccupyGrid(slot.SlotId, slot.GridX, slot.GridY, slot.ItemWidth, slot.ItemHeight);
                return false;
            }

            // Занимаем новое место
            OccupyGrid(slot.SlotId, newGridX, newGridY, slot.ItemWidth, slot.ItemHeight);
            slot.SetPosition(newGridX, newGridY);

            return true;
        }

        /// <summary>
        /// Меняет местами два слота
        /// </summary>
        public bool SwapSlots(int slotId1, int slotId2)
        {
            var slot1 = FindSlotById(slotId1);
            var slot2 = FindSlotById(slotId2);

            if (slot1 == null || slot2 == null)
                return false;

            // Проверяем, влезают ли предметы на новые места
            if (!IsValidPosition(slot2.GridX, slot2.GridY, slot1.ItemWidth, slot1.ItemHeight))
                return false;
            if (!IsValidPosition(slot1.GridX, slot1.GridY, slot2.ItemWidth, slot2.ItemHeight))
                return false;

            // Освобождаем оба места
            ClearSlotFromGrid(slot1.SlotId);
            ClearSlotFromGrid(slot2.SlotId);

            // Меняем позиции
            int tempX = slot1.GridX;
            int tempY = slot1.GridY;

            slot1.SetPosition(slot2.GridX, slot2.GridY);
            slot2.SetPosition(tempX, tempY);

            // Занимаем новые места
            OccupyGrid(slot1.SlotId, slot1.GridX, slot1.GridY, slot1.ItemWidth, slot1.ItemHeight);
            OccupyGrid(slot2.SlotId, slot2.GridX, slot2.GridY, slot2.ItemWidth, slot2.ItemHeight);

            return true;
        }

        #endregion

        #region Equipment Bridge

        /// <summary>
        /// Экипирует предмет из инвентаря на куклу.
        /// Удаляет из инвентаря и надевает на EquipmentController.
        /// </summary>
        /// <returns>Экземпляр экипировки или null</returns>
        public EquipmentInstance EquipFromInventory(int slotId)
        {
            var slot = FindSlotById(slotId);
            if (slot == null)
                return null;

            if (!(slot.ItemData is EquipmentData equipData))
                return null;

            if (equipmentController == null)
                return null;

            // Пробуем экипировать
            var instance = equipmentController.Equip(equipData, slot.grade, slot.Durability);
            if (instance != null)
            {
                // Удаляем из инвентаря
                RemoveFromSlot(slot, 1);
            }

            return instance;
        }

        /// <summary>
        /// Снимает предмет с куклы и помещает в инвентарь.
        /// </summary>
        /// <returns>Слот инвентаря или null (нет места)</returns>
        public InventorySlot UnequipToInventory(EquipmentSlot equipSlot)
        {
            if (equipmentController == null)
                return null;

            var instance = equipmentController.Unequip(equipSlot);
            if (instance == null)
                return null;

            // Добавляем в инвентарь
            var inventorySlot = AddItem(instance.equipmentData, 1);
            if (inventorySlot != null)
            {
                // Переносим прочность и грейд
                inventorySlot.durability = instance.durability;
                inventorySlot.grade = instance.grade;
            }
            else
            {
                // Нет места — возвращаем обратно
                equipmentController.Equip(instance.equipmentData, instance.grade, instance.durability);
            }

            return inventorySlot;
        }

        #endregion

        #region Query

        public InventorySlot FindSlotById(int slotId)
        {
            return slots.Find(s => s.SlotId == slotId);
        }

        public InventorySlot FindSlotWithItem(string itemId)
        {
            return slots.Find(s => s.ItemData.itemId == itemId && s.Count < s.ItemData.maxStack);
        }

        public List<InventorySlot> FindAllSlotsWithItem(string itemId)
        {
            return slots.FindAll(s => s.ItemData.itemId == itemId);
        }

        public int CountItem(string itemId)
        {
            int total = 0;
            foreach (var slot in slots)
            {
                if (slot.ItemData.itemId == itemId)
                    total += slot.Count;
            }
            return total;
        }

        public bool HasItem(string itemId, int count = 1)
        {
            return CountItem(itemId) >= count;
        }

        public bool HasFreeSpace(int width = 1, int height = 1)
        {
            return FindFreePosition(width, height).HasValue;
        }

        public InventorySlot GetSlotAtPosition(int gridX, int gridY)
        {
            if (gridX < 0 || gridX >= gridWidth || gridY < 0 || gridY >= gridHeight)
                return null;

            int slotId = gridSlotIds[gridX, gridY];
            if (slotId < 0) return null;

            return FindSlotById(slotId);
        }

        /// <summary>
        /// Сортирует инвентарь по категории → имени → редкости
        /// </summary>
        public void SortInventory()
        {
            slots.Sort((a, b) =>
            {
                int catCompare = ((int)a.ItemData.category).CompareTo((int)b.ItemData.category);
                if (catCompare != 0) return catCompare;

                int nameCompare = string.Compare(a.ItemData.nameRu, b.ItemData.nameRu, StringComparison.Ordinal);
                if (nameCompare != 0) return nameCompare;

                return ((int)a.ItemData.rarity).CompareTo((int)b.ItemData.rarity);
            });

            // Пересчитываем позиции
            int x = 0, y = 0;
            foreach (var slot in slots)
            {
                // Пробуем разместить начиная с текущей позиции
                var pos = FindFreePositionFrom(x, y, slot.ItemWidth, slot.ItemHeight);
                if (pos.HasValue)
                {
                    slot.SetPosition(pos.Value.x, pos.Value.y);
                }
                else
                {
                    var fallback = FindFreePosition(slot.ItemWidth, slot.ItemHeight);
                    if (fallback.HasValue)
                        slot.SetPosition(fallback.Value.x, fallback.Value.y);
                }
            }

            RebuildGridFromSlots();
        }

        #endregion

        #region Grid Operations

        private bool IsValidPosition(int gridX, int gridY, int width, int height)
        {
            return gridX >= 0 && gridY >= 0 &&
                   gridX + width <= gridWidth &&
                   gridY + height <= gridHeight;
        }

        private bool IsAreaFree(int gridX, int gridY, int width, int height)
        {
            for (int x = gridX; x < gridX + width; x++)
            {
                for (int y = gridY; y < gridY + height; y++)
                {
                    if (gridSlotIds[x, y] >= 0)
                        return false;
                }
            }
            return true;
        }

        private void OccupyGrid(int slotId, int gridX, int gridY, int width, int height)
        {
            for (int x = gridX; x < gridX + width; x++)
            {
                for (int y = gridY; y < gridY + height; y++)
                {
                    gridSlotIds[x, y] = slotId;
                }
            }
        }

        /// <summary>
        /// Освобождает ячейки, занятые указанным slotId
        /// </summary>
        private void ClearSlotFromGrid(int slotId)
        {
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (gridSlotIds[x, y] == slotId)
                        gridSlotIds[x, y] = -1;
                }
            }
        }

        private Vector2Int? FindFreePosition(int width, int height)
        {
            for (int y = 0; y <= gridHeight - height; y++)
            {
                for (int x = 0; x <= gridWidth - width; x++)
                {
                    if (IsAreaFree(x, y, width, height))
                        return new Vector2Int(x, y);
                }
            }
            return null;
        }

        /// <summary>
        /// Ищет свободную позицию начиная с указанной точки
        /// </summary>
        private Vector2Int? FindFreePositionFrom(int startX, int startY, int width, int height)
        {
            for (int y = startY; y <= gridHeight - height; y++)
            {
                int xStart = (y == startY) ? startX : 0;
                for (int x = xStart; x <= gridWidth - width; x++)
                {
                    if (IsAreaFree(x, y, width, height))
                        return new Vector2Int(x, y);
                }
            }
            return null;
        }

        #endregion

        #region Helpers

        private InventorySlot CreateSlot(ItemData itemData, int gridX, int gridY, int count)
        {
            return new InventorySlot
            {
                SlotId = nextSlotId++,
                ItemData = itemData,
                Count = Mathf.Min(count, itemData.maxStack),
                GridX = gridX,
                GridY = gridY,
                durability = itemData.hasDurability ? itemData.maxDurability : -1
            };
        }

        #endregion

        #region Resize (for backpack change with items already placed)

        /// <summary>
        /// Изменяет размер инвентаря (FIX INV-BUG-03: полная перестройка).
        /// Возвращает false если предметы не помещаются.
        /// </summary>
        public bool Resize(int newWidth, int newHeight)
        {
            // Проверяем, помещаются ли все предметы
            foreach (var slot in slots)
            {
                if (slot.GridX + slot.ItemWidth > newWidth ||
                    slot.GridY + slot.ItemHeight > newHeight)
                {
                    return false;
                }
            }

            ApplyGridSize(newWidth, newHeight);
            return true;
        }

        #endregion

        #region Save/Load Support

        public List<InventorySlotSaveData> GetSaveData()
        {
            var data = new List<InventorySlotSaveData>();
            foreach (var slot in slots)
            {
                data.Add(new InventorySlotSaveData
                {
                    itemId = slot.ItemData.itemId,
                    count = slot.Count,
                    gridX = slot.GridX,
                    gridY = slot.GridY,
                    durability = slot.Durability,
                    grade = slot.grade
                });
            }
            return data;
        }

        public void LoadSaveData(List<InventorySlotSaveData> data, Dictionary<string, ItemData> itemDatabase)
        {
            Clear();

            if (data == null) return;

            foreach (var slotData in data)
            {
                if (itemDatabase.TryGetValue(slotData.itemId, out var itemData))
                {
                    var slot = AddItemAt(itemData, slotData.gridX, slotData.gridY, slotData.count);
                    if (slot != null)
                    {
                        // FIX INV-BUG-06: durability >= 0 (0 = сломанный предмет тоже загружается)
                        if (slotData.durability >= 0)
                        {
                            slot.durability = slotData.durability;
                        }
                        slot.grade = slotData.grade;
                    }
                }
            }
        }

        #endregion
    }

    // ============================================================================
    // InventorySlot — Слот инвентаря (v2.0)
    // ============================================================================

    [Serializable]
    public class InventorySlot
    {
        public int SlotId;
        public ItemData ItemData;
        public int Count;
        public int GridX;
        public int GridY;

        [SerializeField]
        internal int durability;

        // Грейд для крафтовых предметов
        public EquipmentGrade grade = EquipmentGrade.Common;

        // Properties
        public int ItemWidth => ItemData?.sizeWidth ?? 1;
        public int ItemHeight => ItemData?.sizeHeight ?? 1;
        public int Durability => durability;
        public int MaxDurability => ItemData?.maxDurability ?? 100;
        public float DurabilityPercent => MaxDurability > 0 ? (float)durability / MaxDurability : 1f;

        /// <summary>
        /// Предмет использует систему прочности (FIX INV-BUG-05: durability=0 → true)
        /// durability >= 0 = система прочности используется
        /// durability < 0 (обычно -1) = система прочности НЕ используется
        /// </summary>
        public bool HasDurability => durability >= 0;

        public DurabilityCondition Condition => GetCondition();

        public void AddCount(int amount) => Count += amount;
        public void RemoveCount(int amount) => Count = Mathf.Max(0, Count - amount);
        public void SetPosition(int x, int y) { GridX = x; GridY = y; }

        public void Damage(int amount)
        {
            if (durability >= 0)
            {
                durability = Mathf.Max(0, durability - amount);
            }
        }

        public void Repair(int amount)
        {
            if (durability >= 0)
            {
                durability = Mathf.Min(MaxDurability, durability + amount);
            }
        }

        private DurabilityCondition GetCondition()
        {
            if (durability < 0) return DurabilityCondition.Pristine;
            if (durability == 0) return DurabilityCondition.Broken;

            float percent = DurabilityPercent * 100f;
            if (percent >= 100f) return DurabilityCondition.Pristine;
            if (percent >= 80f) return DurabilityCondition.Excellent;
            if (percent >= 60f) return DurabilityCondition.Good;
            if (percent >= 40f) return DurabilityCondition.Worn;
            if (percent >= 20f) return DurabilityCondition.Damaged;
            return DurabilityCondition.Broken;
        }
    }

    // ============================================================================
    // ItemStack — Стак предметов
    // ============================================================================

    [Serializable]
    public class ItemStack
    {
        public string itemId;
        public int count;
        public int durability;

        public ItemStack(string id, int amount, int dur = -1)
        {
            itemId = id;
            count = amount;
            durability = dur;
        }

        public bool IsStackableWith(ItemStack other)
        {
            return itemId == other.itemId && durability == other.durability;
        }

        public void Merge(ItemStack other, int maxStack)
        {
            if (!IsStackableWith(other)) return;

            int space = maxStack - count;
            int toAdd = Mathf.Min(space, other.count);

            count += toAdd;
            other.count -= toAdd;
        }

        public ItemStack Split(int amount)
        {
            if (amount >= count) return null;

            var newStack = new ItemStack(itemId, amount, durability);
            count -= amount;
            return newStack;
        }
    }

    // ============================================================================
    // Save Data (v2.0 — +grade field)
    // ============================================================================

    [Serializable]
    public class InventorySlotSaveData
    {
        public string itemId;
        public int count;
        public int gridX;
        public int gridY;
        public int durability;
        public EquipmentGrade grade;
    }
}
