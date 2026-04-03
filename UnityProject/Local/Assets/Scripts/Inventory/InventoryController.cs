// ============================================================================
// InventoryController.cs — Система инвентаря
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Inventory
{
    /// <summary>
    /// Контроллер инвентаря (Diablo-style сетка).
    /// Управляет слотами, стаками и операциями с предметами.
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        #region Configuration

        [Header("Inventory Settings")]
        [Tooltip("Ширина сетки инвентаря")]
        [Range(4, 16)]
        public int gridWidth = 8;

        [Tooltip("Высота сетки инвентаря")]
        [Range(4, 12)]
        public int gridHeight = 6;

        [Tooltip("Максимальный вес")]
        public float maxWeight = 100f;

        [Tooltip("Включить ограничение веса")]
        public bool useWeightLimit = true;

        [Header("References")]
        [Tooltip("Владелец инвентаря")]
        public MonoBehaviour owner;

        #endregion

        #region Runtime Data

        // Сетка занятости (true = занято)
        private bool[,] gridOccupancy;

        // Список всех слотов
        private List<InventorySlot> slots = new List<InventorySlot>();

        // Текущий вес
        private float currentWeight = 0f;

        // Счётчик ID для слотов
        private int nextSlotId = 0;

        #endregion

        #region Events

        public event Action<InventorySlot> OnItemAdded;
        public event Action<InventorySlot> OnItemRemoved;
        public event Action<InventorySlot, int> OnItemStackChanged;
        public event Action<float, float> OnWeightChanged;
        public event Action OnInventoryFull;

        #endregion

        #region Properties

        public int TotalSlots => gridWidth * gridHeight;
        public int UsedSlots => slots.Count;
        public int FreeSlots => TotalSlots - UsedSlots;
        public float CurrentWeight => currentWeight;
        public float WeightPercent => maxWeight > 0 ? currentWeight / maxWeight : 0f;
        public bool IsOverencumbered => useWeightLimit && currentWeight > maxWeight;
        public IReadOnlyList<InventorySlot> Slots => slots.AsReadOnly();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeGrid();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализирует сетку инвентаря
        /// </summary>
        public void InitializeGrid()
        {
            gridOccupancy = new bool[gridWidth, gridHeight];
            slots.Clear();
            currentWeight = 0f;
            nextSlotId = 0;
        }

        /// <summary>
        /// Изменяет размер инвентаря
        /// </summary>
        public bool Resize(int newWidth, int newHeight)
        {
            if (newWidth < gridWidth || newHeight < gridHeight)
            {
                // Проверяем, не выходят ли предметы за новые границы
                foreach (var slot in slots)
                {
                    if (slot.GridX + slot.ItemWidth > newWidth ||
                        slot.GridY + slot.ItemHeight > newHeight)
                    {
                        return false; // Нельзя уменьшить — предметы не влезут
                    }
            }
            }

            // Создаём новую сетку
            var newGrid = new bool[newWidth, newHeight];

            // Копируем занятые ячейки
            for (int x = 0; x < Mathf.Min(gridWidth, newWidth); x++)
            {
                for (int y = 0; y < Mathf.Min(gridHeight, newHeight); y++)
                {
                    newGrid[x, y] = gridOccupancy[x, y];
                }
            }

            gridOccupancy = newGrid;
            gridWidth = newWidth;
            gridHeight = newHeight;

            return true;
        }

        #endregion

        #region Add Item

        /// <summary>
        /// Добавляет предмет в инвентарь
        /// </summary>
        public InventorySlot AddItem(Data.ScriptableObjects.ItemData itemData, int count = 1)
        {
            if (itemData == null || count <= 0)
                return null;

            // Проверяем вес
            if (useWeightLimit)
            {
                float addedWeight = itemData.weight * count;
                if (currentWeight + addedWeight > maxWeight)
                {
                    OnInventoryFull?.Invoke();
                    return null;
                }
            }

            // Если предмет стакается, ищем существующий слот
            if (itemData.stackable)
            {
                var existingSlot = FindSlotWithItem(itemData.itemId);
                if (existingSlot != null)
                {
                    int spaceLeft = itemData.maxStack - existingSlot.Count;
                    int toAdd = Mathf.Min(count, spaceLeft);

                    existingSlot.AddCount(toAdd);
                    UpdateWeight(itemData.weight * toAdd);

                    if (toAdd < count)
                    {
                        // Остаток добавляем в новый слот
                        AddItem(itemData, count - toAdd);
                    }

                    OnItemStackChanged?.Invoke(existingSlot, existingSlot.Count);
                    return existingSlot;
                }
            }

            // Ищем свободное место для нового слота
            var position = FindFreePosition(itemData.sizeWidth, itemData.sizeHeight);
            if (!position.HasValue)
            {
                OnInventoryFull?.Invoke();
                return null;
            }

            // Создаём новый слот
            var newSlot = CreateSlot(itemData, position.Value.x, position.Value.y, count);
            slots.Add(newSlot);

            // Занимаем ячейки
            OccupyGrid(position.Value.x, position.Value.y, itemData.sizeWidth, itemData.sizeHeight);

            // Обновляем вес
            UpdateWeight(itemData.weight * count);

            OnItemAdded?.Invoke(newSlot);
            return newSlot;
        }

        /// <summary>
        /// Добавляет предмет в конкретную позицию
        /// </summary>
        public InventorySlot AddItemAt(Data.ScriptableObjects.ItemData itemData, int gridX, int gridY, int count = 1)
        {
            if (itemData == null || count <= 0)
                return null;

            // Проверяем границы
            if (!IsValidPosition(gridX, gridY, itemData.sizeWidth, itemData.sizeHeight))
                return null;

            // Проверяем, свободно ли место
            if (!IsAreaFree(gridX, gridY, itemData.sizeWidth, itemData.sizeHeight))
                return null;

            // Проверяем вес
            if (useWeightLimit)
            {
                float addedWeight = itemData.weight * count;
                if (currentWeight + addedWeight > maxWeight)
                {
                    OnInventoryFull?.Invoke();
                    return null;
                }
            }

            // Создаём слот
            var newSlot = CreateSlot(itemData, gridX, gridY, count);
            slots.Add(newSlot);

            // Занимаем ячейки
            OccupyGrid(gridX, gridY, itemData.sizeWidth, itemData.sizeHeight);

            // Обновляем вес
            UpdateWeight(itemData.weight * count);

            OnItemAdded?.Invoke(newSlot);
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
            InitializeGrid();
            currentWeight = 0f;
            OnWeightChanged?.Invoke(0f, maxWeight);
        }

        private bool RemoveFromSlot(InventorySlot slot, int count)
        {
            if (count > slot.Count)
                count = slot.Count;

            slot.RemoveCount(count);
            UpdateWeight(-slot.ItemData.weight * count);

            // Если слот пуст, удаляем его
            if (slot.Count <= 0)
            {
                FreeGrid(slot.GridX, slot.GridY, slot.ItemWidth, slot.ItemHeight);
                slots.Remove(slot);
                OnItemRemoved?.Invoke(slot);
            }
            else
            {
                OnItemStackChanged?.Invoke(slot, slot.Count);
            }

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

            // Проверяем границы
            if (!IsValidPosition(newGridX, newGridY, slot.ItemWidth, slot.ItemHeight))
                return false;

            // Освобождаем старое место
            FreeGrid(slot.GridX, slot.GridY, slot.ItemWidth, slot.ItemHeight);

            // Проверяем, свободно ли новое место
            if (!IsAreaFree(newGridX, newGridY, slot.ItemWidth, slot.ItemHeight))
            {
                // Возвращаем старое место
                OccupyGrid(slot.GridX, slot.GridY, slot.ItemWidth, slot.ItemHeight);
                return false;
            }

            // Занимаем новое место
            OccupyGrid(newGridX, newGridY, slot.ItemWidth, slot.ItemHeight);

            // Обновляем позицию
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
            FreeGrid(slot1.GridX, slot1.GridY, slot1.ItemWidth, slot1.ItemHeight);
            FreeGrid(slot2.GridX, slot2.GridY, slot2.ItemWidth, slot2.ItemHeight);

            // Меняем позиции
            int tempX = slot1.GridX;
            int tempY = slot1.GridY;

            slot1.SetPosition(slot2.GridX, slot2.GridY);
            slot2.SetPosition(tempX, tempY);

            // Занимаем новые места
            OccupyGrid(slot1.GridX, slot1.GridY, slot1.ItemWidth, slot1.ItemHeight);
            OccupyGrid(slot2.GridX, slot2.GridY, slot2.ItemWidth, slot2.ItemHeight);

            return true;
        }

        #endregion

        #region Query

        /// <summary>
        /// Находит слот по ID
        /// </summary>
        public InventorySlot FindSlotById(int slotId)
        {
            return slots.Find(s => s.SlotId == slotId);
        }

        /// <summary>
        /// Находит слот с указанным предметом
        /// </summary>
        public InventorySlot FindSlotWithItem(string itemId)
        {
            return slots.Find(s => s.ItemData.itemId == itemId && s.Count < s.ItemData.maxStack);
        }

        /// <summary>
        /// Находит все слоты с указанным предметом
        /// </summary>
        public List<InventorySlot> FindAllSlotsWithItem(string itemId)
        {
            return slots.FindAll(s => s.ItemData.itemId == itemId);
        }

        /// <summary>
        /// Подсчитывает количество предмета
        /// </summary>
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

        /// <summary>
        /// Проверяет наличие предмета
        /// </summary>
        public bool HasItem(string itemId, int count = 1)
        {
            return CountItem(itemId) >= count;
        }

        /// <summary>
        /// Проверяет наличие свободного места
        /// </summary>
        public bool HasFreeSpace(int width = 1, int height = 1)
        {
            return FindFreePosition(width, height).HasValue;
        }

        /// <summary>
        /// Получает слот по позиции в сетке
        /// </summary>
        public InventorySlot GetSlotAtPosition(int gridX, int gridY)
        {
            foreach (var slot in slots)
            {
                if (gridX >= slot.GridX && gridX < slot.GridX + slot.ItemWidth &&
                    gridY >= slot.GridY && gridY < slot.GridY + slot.ItemHeight)
                {
                    return slot;
                }
            }
            return null;
        }

        #endregion

        #region Grid Operations

        /// <summary>
        /// Проверяет валидность позиции
        /// </summary>
        private bool IsValidPosition(int gridX, int gridY, int width, int height)
        {
            return gridX >= 0 && gridY >= 0 &&
                   gridX + width <= gridWidth &&
                   gridY + height <= gridHeight;
        }

        /// <summary>
        /// Проверяет, свободна ли область
        /// </summary>
        private bool IsAreaFree(int gridX, int gridY, int width, int height)
        {
            for (int x = gridX; x < gridX + width; x++)
            {
                for (int y = gridY; y < gridY + height; y++)
                {
                    if (gridOccupancy[x, y])
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Занимает область сетки
        /// </summary>
        private void OccupyGrid(int gridX, int gridY, int width, int height)
        {
            for (int x = gridX; x < gridX + width; x++)
            {
                for (int y = gridY; y < gridY + height; y++)
                {
                    gridOccupancy[x, y] = true;
                }
            }
        }

        /// <summary>
        /// Освобождает область сетки
        /// </summary>
        private void FreeGrid(int gridX, int gridY, int width, int height)
        {
            for (int x = gridX; x < gridX + width; x++)
            {
                for (int y = gridY; y < gridY + height; y++)
                {
                    gridOccupancy[x, y] = false;
                }
            }
        }

        /// <summary>
        /// Находит свободную позицию для предмета
        /// </summary>
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

        #endregion

        #region Helpers

        private InventorySlot CreateSlot(Data.ScriptableObjects.ItemData itemData, int gridX, int gridY, int count)
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

        private void UpdateWeight(float delta)
        {
            currentWeight += delta;
            currentWeight = Mathf.Max(0f, currentWeight);
            OnWeightChanged?.Invoke(currentWeight, maxWeight);
        }

        #endregion

        #region Save/Load Support

        /// <summary>
        /// Получает данные для сохранения
        /// </summary>
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
                    durability = slot.Durability
                });
            }
            return data;
        }

        /// <summary>
        /// Загружает данные
        /// </summary>
        public void LoadSaveData(List<InventorySlotSaveData> data, Dictionary<string, Data.ScriptableObjects.ItemData> itemDatabase)
        {
            Clear();

            if (data == null) return;

            foreach (var slotData in data)
            {
                if (itemDatabase.TryGetValue(slotData.itemId, out var itemData))
                {
                    var slot = AddItemAt(itemData, slotData.gridX, slotData.gridY, slotData.count);
                    if (slot != null && slotData.durability > 0)
                    {
                        slot.durability = slotData.durability;
                    }
                }
            }
        }

        #endregion
    }

    // ============================================================================
    // InventorySlot — Слот инвентаря
    // ============================================================================

    [Serializable]
    public class InventorySlot
    {
        public int SlotId;
        public Data.ScriptableObjects.ItemData ItemData;
        public int Count;
        public int GridX;
        public int GridY;

        [SerializeField]
        internal int durability;

        // Properties
        public int ItemWidth => ItemData?.sizeWidth ?? 1;
        public int ItemHeight => ItemData?.sizeHeight ?? 1;
        public int Durability => durability;
        public int MaxDurability => ItemData?.maxDurability ?? 100;
        public float DurabilityPercent => MaxDurability > 0 ? (float)durability / MaxDurability : 1f;
        public bool HasDurability => durability > 0;
        public DurabilityCondition Condition => GetCondition();

        public void AddCount(int amount) => Count += amount;
        public void RemoveCount(int amount) => Count = Mathf.Max(0, Count - amount);
        public void SetPosition(int x, int y) { GridX = x; GridY = y; }

        public void Damage(int amount)
        {
            if (durability > 0)
            {
                durability = Mathf.Max(0, durability - amount);
            }
        }

        public void Repair(int amount)
        {
            if (durability > 0)
            {
                durability = Mathf.Min(MaxDurability, durability + amount);
            }
        }

        private DurabilityCondition GetCondition()
        {
            if (durability <= 0) return DurabilityCondition.Pristine;

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
    // Save Data
    // ============================================================================

    [Serializable]
    public class InventorySlotSaveData
    {
        public string itemId;
        public int count;
        public int gridX;
        public int gridY;
        public int durability;
    }
}
