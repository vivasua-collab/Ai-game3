// ============================================================================
// InventoryController.cs — Система инвентаря (v3.0 — строчная модель)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-03
// Редактировано: 2026-04-27 18:07:00 UTC — ПОЛНАЯ ПЕРЕПИСЬ: сетка → строчная модель
// ============================================================================
// Изменения v3.0 (строчная модель):
// - УБРАНА сетка: gridSlotIds[,], posX/posY, sizeWidth/sizeHeight
// - Ограничители: масса (weight) + объём (volume) вместо ячеек
// - InventorySlot: rowIndex вместо GridX/GridY
// - FindSlotById: Dictionary O(1) вместо List.Find O(N)
// - CanFitItem: проверка по массе + объёму
// - SwapRows: перестановка строк вместо SwapSlots по координатам
// - Save/Load: без gridX/gridY
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Inventory
{
    /// <summary>
    /// Контроллер инвентаря (v3.0 — строчная модель).
    ///
    /// Вместо сетки (gridPosX/PosY) — строчный список предметов.
    /// Ограничители вместимости: масса (weight) и объём (volume).
    /// Размеры определяются надетым рюкзаком (BackpackData.maxWeight/maxVolume).
    ///
    /// Механики:
    /// - Эффективный вес: effectiveWeight = totalWeight × (1 − backpack.weightReduction)
    /// - Максимальный вес: baseMaxWeight + backpack.maxWeightBonus
    /// - Максимальный объём: backpack.maxVolume (или defaultMaxVolume)
    /// - Стак стекируемых предметов (maxStack)
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        #region Configuration

        [Header("Base Settings (overridden by backpack)")]
        [Tooltip("Базовый максимальный вес (без рюкзака, кг)")]
        public float baseMaxWeight = 30f;

        [Tooltip("Базовый максимальный объём (без рюкзака, литры)")]
        public float defaultMaxVolume = 50f;

        [Tooltip("Включить ограничение веса")]
        public bool useWeightLimit = true;

        [Tooltip("Включить ограничение объёма")]
        public bool useVolumeLimit = true;

        [Header("References")]
        [Tooltip("Контроллер экипировки (для моста)")]
        public EquipmentController equipmentController;

        #endregion

        #region Runtime Data

        /// <summary>Текущий рюкзак (null = стартовая сумка)</summary>
        private BackpackData currentBackpack;

        /// <summary>Список всех предметов (строчная модель)</summary>
        private List<InventorySlot> slots = new List<InventorySlot>();

        /// <summary>Быстрый доступ к слотам по SlotId (O(1) вместо O(N))</summary>
        private Dictionary<int, InventorySlot> slotById = new Dictionary<int, InventorySlot>();

        /// <summary>Сырой вес (без снижения от рюкзака)</summary>
        private float rawWeight = 0f;

        /// <summary>Текущий объём</summary>
        private float rawVolume = 0f;

        /// <summary>Счётчик ID для слотов</summary>
        private int nextSlotId = 0;

        #endregion

        #region Events

        /// <summary>Предмет добавлен (новый слот)</summary>
        public event Action<InventorySlot> OnItemAdded;
        /// <summary>Предмет удалён (слот очищен)</summary>
        public event Action<InventorySlot> OnItemRemoved;
        /// <summary>Изменилось количество в стаке</summary>
        public event Action<InventorySlot, int> OnItemStackChanged;
        /// <summary>Изменился вес или объём</summary>
        public event Action<float, float, float, float> OnWeightVolumeChanged;
        /// <summary>Инвентарь полон</summary>
        public event Action OnInventoryFull;
        /// <summary>Рюкзак изменён (сменился или снялся)</summary>
        public event Action<BackpackData> OnBackpackChanged;
        /// <summary>Список перестроен (сортировка, загрузка)</summary>
        public event Action OnInventoryRebuilt;

        #endregion

        #region Properties

        /// <summary>Количество предметов (строк в списке)</summary>
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

        /// <summary>Текущий объём</summary>
        public float TotalVolume => rawVolume;

        /// <summary>Максимальный вес с учётом бонуса рюкзака</summary>
        public float MaxWeight
        {
            get
            {
                float bonus = currentBackpack != null ? currentBackpack.maxWeightBonus : 0f;
                return baseMaxWeight + bonus;
            }
        }

        /// <summary>Максимальный объём (определяется рюкзаком)</summary>
        public float MaxVolume
        {
            get
            {
                return currentBackpack != null ? currentBackpack.maxVolume : defaultMaxVolume;
            }
        }

        /// <summary>Процент веса</summary>
        public float WeightPercent => MaxWeight > 0 ? EffectiveWeight / MaxWeight : 0f;

        /// <summary>Процент объёма</summary>
        public float VolumePercent => MaxVolume > 0 ? rawVolume / MaxVolume : 0f;

        /// <summary>Перегруз по весу</summary>
        public bool IsOverencumbered => useWeightLimit && EffectiveWeight > MaxWeight;

        /// <summary>Переполнение по объёму</summary>
        public bool IsOverVolume => useVolumeLimit && rawVolume > MaxVolume;

        /// <summary>Текущий рюкзак</summary>
        public BackpackData CurrentBackpack => currentBackpack;

        /// <summary>Доступ к слотам (только чтение)</summary>
        public IReadOnlyList<InventorySlot> Slots => slots.AsReadOnly();

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Инициализация — пустой инвентарь
            RecalculateTotals();
        }

        #endregion

        #region Backpack Management

        /// <summary>
        /// Устанавливает рюкзак. Изменяет лимиты массы/объёма.
        /// Если текущие предметы превышают новые лимиты — рюкзак НЕ ставится.
        /// </summary>
        /// <returns>true если рюкзак успешно установлен</returns>
        public bool SetBackpack(BackpackData backpackData)
        {
            // Проверяем, помещаются ли предметы в новые лимиты
            float newMaxWeight = baseMaxWeight + (backpackData != null ? backpackData.maxWeightBonus : 0f);
            float newMaxVolume = backpackData != null ? backpackData.maxVolume : defaultMaxVolume;
            float newReduction = backpackData != null ? backpackData.weightReduction / 100f : 0f;

            float newEffectiveWeight = rawWeight * (1f - newReduction);

            if (useWeightLimit && newEffectiveWeight > newMaxWeight)
            {
                Debug.LogWarning("[InventoryController] Нельзя сменить рюкзак: превышен лимит веса");
                return false;
            }

            if (useVolumeLimit && rawVolume > newMaxVolume)
            {
                Debug.LogWarning("[InventoryController] Нельзя сменить рюкзак: превышен лимит объёма");
                return false;
            }

            currentBackpack = backpackData;

            OnBackpackChanged?.Invoke(currentBackpack);
            NotifyWeightVolumeChanged();

            return true;
        }

        /// <summary>
        /// Снимает рюкзак. Возвращает false если без рюкзака не помещаются предметы.
        /// </summary>
        public bool RemoveBackpack()
        {
            return SetBackpack(null);
        }

        #endregion

        #region Capacity Check

        /// <summary>
        /// Проверяет, помещается ли предмет в инвентарь (по массе и объёму).
        /// </summary>
        public bool CanFitItem(ItemData itemData, int count = 1)
        {
            if (itemData == null || count <= 0)
                return false;

            // Проверка веса
            if (useWeightLimit)
            {
                float addedWeight = itemData.weight * count;
                // Учитываем снижение веса от рюкзака для нового предмета
                float reduction = currentBackpack != null ? currentBackpack.weightReduction / 100f : 0f;
                float effectiveAdded = addedWeight * (1f - reduction);
                if (EffectiveWeight + effectiveAdded > MaxWeight)
                    return false;
            }

            // Проверка объёма
            if (useVolumeLimit)
            {
                float addedVolume = itemData.volume * count;
                if (rawVolume + addedVolume > MaxVolume)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Сколько единиц предмета можно добавить (по массе и объёму).
        /// </summary>
        public int HowManyCanFit(ItemData itemData, int requested = int.MaxValue)
        {
            if (itemData == null)
                return 0;

            int byWeight = requested;
            int byVolume = requested;

            if (useWeightLimit && itemData.weight > 0)
            {
                float reduction = currentBackpack != null ? currentBackpack.weightReduction / 100f : 0f;
                float effectivePerItem = itemData.weight * (1f - reduction);
                float available = MaxWeight - EffectiveWeight;
                byWeight = effectivePerItem > 0 ? Mathf.FloorToInt(available / effectivePerItem) : requested;
            }

            if (useVolumeLimit && itemData.volume > 0)
            {
                float available = MaxVolume - rawVolume;
                byVolume = Mathf.FloorToInt(available / itemData.volume);
            }

            return Mathf.Max(0, Mathf.Min(requested, byWeight, byVolume));
        }

        /// <summary>
        /// Совместимость: HasFreeSpace для SpiritStorage/StorageRing.
        /// В строчной модели всегда true — проверка по CanFitItem.
        /// </summary>
        public bool HasFreeSpace(int width = 1, int height = 1)
        {
            // Легаси-совместимость: в строчной модели нет сетки
            // Реальная проверка — CanFitItem
            return true;
        }

        #endregion

        #region Add Item

        /// <summary>
        /// Добавляет предмет в инвентарь (итеративно).
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

                    // Проверяем лимиты
                    toAdd = Mathf.Min(toAdd, HowManyCanFit(itemData, toAdd));
                    if (toAdd <= 0)
                    {
                        OnInventoryFull?.Invoke();
                        return lastSlot;
                    }

                    slot.AddCount(toAdd);
                    rawWeight += itemData.weight * toAdd;
                    rawVolume += itemData.volume * toAdd;
                    remaining -= toAdd;

                    OnItemStackChanged?.Invoke(slot, slot.Count);
                    lastSlot = slot;
                }
            }

            // Шаг 2: Создаём новые слоты для оставшихся предметов
            while (remaining > 0)
            {
                int toPlace = Mathf.Min(remaining, itemData.maxStack);

                // Проверяем лимиты
                toPlace = Mathf.Min(toPlace, HowManyCanFit(itemData, toPlace));
                if (toPlace <= 0)
                {
                    OnInventoryFull?.Invoke();
                    return lastSlot;
                }

                var newSlot = CreateSlot(itemData, toPlace);
                slots.Add(newSlot);
                slotById[newSlot.SlotId] = newSlot;
                UpdateRowIndices();

                rawWeight += itemData.weight * toPlace;
                rawVolume += itemData.volume * toPlace;
                remaining -= toPlace;

                OnItemAdded?.Invoke(newSlot);
                lastSlot = newSlot;
            }

            NotifyWeightVolumeChanged();
            return lastSlot;
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
            slotById.Clear();
            nextSlotId = 0;
            rawWeight = 0f;
            rawVolume = 0f;
            NotifyWeightVolumeChanged();
            OnInventoryRebuilt?.Invoke();
        }

        /// <summary>
        /// Удаляет предметы из слота.
        /// Вес и объём вычисляются ДО модификации слота.
        /// </summary>
        private bool RemoveFromSlot(InventorySlot slot, int count)
        {
            if (count <= 0) return false;
            if (count > slot.Count)
                count = slot.Count;

            // Вычисляем дельту ДО модификации слота
            float weightDelta = -slot.ItemData.weight * count;
            float volumeDelta = -slot.ItemData.volume * count;

            // Обновляем слот
            slot.RemoveCount(count);

            // Обновляем тоталы
            rawWeight = Mathf.Max(0f, rawWeight + weightDelta);
            rawVolume = Mathf.Max(0f, rawVolume + volumeDelta);

            // Если слот пуст, удаляем его
            if (slot.Count <= 0)
            {
                int index = slots.IndexOf(slot);
                slots.Remove(slot);
                slotById.Remove(slot.SlotId);
                UpdateRowIndices();
                OnItemRemoved?.Invoke(slot);
            }
            else
            {
                OnItemStackChanged?.Invoke(slot, slot.Count);
            }

            NotifyWeightVolumeChanged();
            return true;
        }

        #endregion

        #region Move / Swap

        /// <summary>
        /// Меняет местами две строки в списке.
        /// </summary>
        public bool SwapRows(int slotId1, int slotId2)
        {
            var slot1 = FindSlotById(slotId1);
            var slot2 = FindSlotById(slotId2);

            if (slot1 == null || slot2 == null)
                return false;

            if (slot1 == slot2)
                return false;

            // Находим индексы в списке
            int index1 = slots.IndexOf(slot1);
            int index2 = slots.IndexOf(slot2);

            if (index1 < 0 || index2 < 0)
                return false;

            // Обмениваем позиции в списке
            slots[index1] = slot2;
            slots[index2] = slot1;

            // Обновляем rowIndex
            slot1.rowIndex = index2;
            slot2.rowIndex = index1;

            return true;
        }

        /// <summary>
        /// Перемещает строку в указанную позицию в списке.
        /// </summary>
        public bool MoveRowTo(int slotId, int targetIndex)
        {
            var slot = FindSlotById(slotId);
            if (slot == null)
                return false;

            int currentIndex = slots.IndexOf(slot);
            if (currentIndex < 0 || currentIndex == targetIndex)
                return false;

            if (targetIndex < 0 || targetIndex >= slots.Count)
                return false;

            // Убираем из текущей позиции
            slots.RemoveAt(currentIndex);
            // Вставляем в новую
            slots.Insert(targetIndex, slot);
            // Пересчитываем индексы
            UpdateRowIndices();

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

            // Проверяем, помещается ли в инвентарь
            if (!CanFitItem(instance.equipmentData, 1))
            {
                // Нет места — возвращаем обратно
                equipmentController.Equip(instance.equipmentData, instance.grade, instance.durability);
                return null;
            }

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

        /// <summary>Поиск слота по ID (O(1) через Dictionary)</summary>
        public InventorySlot FindSlotById(int slotId)
        {
            slotById.TryGetValue(slotId, out var slot);
            return slot;
        }

        public InventorySlot FindSlotWithItem(string itemId)
        {
            foreach (var slot in slots)
            {
                if (slot.ItemData.itemId == itemId && slot.Count < slot.ItemData.maxStack)
                    return slot;
            }
            return null;
        }

        public List<InventorySlot> FindAllSlotsWithItem(string itemId)
        {
            var result = new List<InventorySlot>();
            foreach (var slot in slots)
            {
                if (slot.ItemData.itemId == itemId)
                    result.Add(slot);
            }
            return result;
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

        /// <summary>
        /// Получить слот по индексу строки
        /// </summary>
        public InventorySlot GetSlotByIndex(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= slots.Count)
                return null;
            return slots[rowIndex];
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

            UpdateRowIndices();
            OnInventoryRebuilt?.Invoke();
        }

        #endregion

        #region Helpers

        private InventorySlot CreateSlot(ItemData itemData, int count)
        {
            var slot = new InventorySlot
            {
                SlotId = nextSlotId++,
                ItemData = itemData,
                Count = Mathf.Min(count, itemData.maxStack),
                durability = itemData.hasDurability ? itemData.maxDurability : -1
            };
            return slot;
        }

        /// <summary>
        /// Обновляет rowIndex всех слотов по текущему порядку в списке
        /// </summary>
        private void UpdateRowIndices()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].rowIndex = i;
            }
        }

        /// <summary>
        /// Пересчитывает rawWeight и rawVolume из слотов (на случай расхождения)
        /// </summary>
        private void RecalculateTotals()
        {
            rawWeight = 0f;
            rawVolume = 0f;
            foreach (var slot in slots)
            {
                rawWeight += slot.ItemData.weight * slot.Count;
                rawVolume += slot.ItemData.volume * slot.Count;
            }
        }

        private void NotifyWeightVolumeChanged()
        {
            OnWeightVolumeChanged?.Invoke(EffectiveWeight, MaxWeight, rawVolume, MaxVolume);
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
                if (itemDatabase != null && itemDatabase.TryGetValue(slotData.itemId, out var itemData))
                {
                    var slot = AddItem(itemData, slotData.count);
                    if (slot != null)
                    {
                        // Прочность: durability >= 0 = система прочности используется
                        if (slotData.durability >= 0)
                        {
                            slot.durability = slotData.durability;
                        }
                        slot.grade = slotData.grade;
                    }
                }
            }

            OnInventoryRebuilt?.Invoke();
        }

        #endregion
    }

    // ============================================================================
    // InventorySlot — Слот инвентаря (v3.0 — строчная модель)
    // ============================================================================

    [Serializable]
    public class InventorySlot
    {
        public int SlotId;
        public ItemData ItemData;
        public int Count;

        /// <summary>Позиция в строчном списке (вычисляемое)</summary>
        [NonSerialized]
        public int rowIndex;

        [SerializeField]
        internal int durability;

        // Грейд для крафтовых предметов
        public EquipmentGrade grade = EquipmentGrade.Common;

        // Properties
        public int Durability => durability;
        public int MaxDurability => ItemData?.maxDurability ?? 100;
        public float DurabilityPercent => MaxDurability > 0 ? (float)durability / MaxDurability : 1f;

        /// <summary>
        /// Предмет использует систему прочности.
        /// durability >= 0 = система прочности используется
        /// durability < 0 (обычно -1) = система прочности НЕ используется
        /// </summary>
        public bool HasDurability => durability >= 0;

        public DurabilityCondition Condition => GetCondition();

        // Вычисляемые свойства для отображения
        public float TotalWeight => ItemData != null ? ItemData.weight * Count : 0f;
        public float TotalVolume => ItemData != null ? ItemData.volume * Count : 0f;

        public void AddCount(int amount) => Count += amount;
        public void RemoveCount(int amount) => Count = Mathf.Max(0, Count - amount);

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
            // FIX С-01: Убрано Excellent, приведено к 5 состояниям (EQUIPMENT_SYSTEM.md §4.1)
            if (percent >= 100f) return DurabilityCondition.Pristine;
            if (percent >= 80f) return DurabilityCondition.Good;
            if (percent >= 60f) return DurabilityCondition.Worn;
            if (percent >= 20f) return DurabilityCondition.Damaged;
            return DurabilityCondition.Broken;
        }
    }

    // ============================================================================
    // Save Data (v3.0 — без gridX/gridY)
    // ============================================================================

    [Serializable]
    public class InventorySlotSaveData
    {
        public string itemId;
        public int count;
        public int durability;
        public EquipmentGrade grade;
    }
}
