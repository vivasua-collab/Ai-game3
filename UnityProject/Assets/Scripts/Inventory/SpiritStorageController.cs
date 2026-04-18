// ============================================================================
// SpiritStorageController.cs — Духовное хранилище (Межмировая складка)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-19 12:00:00 UTC
// ============================================================================
// Межмировая складка — безлимитное хранилище с каталогизатором.
// Доступно с уровня культивации AwakenedCore (1).
// Стоимость: baseQiCost + weight × qiCostPerKg.
// Нет сетки — вместо неё каталог с фильтрацией и группировкой.
//
// Правила вложения (NestingFlag):
//   Any     → можно поместить ✅
//   Spirit  → можно поместить ✅ (ТОЛЬКО сюда)
//   Ring    → НЕЛЬЗЯ ❌ (только в кольцо хранения)
//   None    → НЕЛЬЗЯ ❌ (квестовые, живые существа)
//
// Кольца хранения → НЕЛЬЗЯ поместить (пространственная нестабильность)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;
using CultivationGame.Qi;

namespace CultivationGame.Inventory
{
    /// <summary>
    /// Контроллер духовного хранилища (Межмировая складка).
    ///
    /// Безлимитное хранилище с каталогизатором.
    /// Стоимость доступа = Qi (зависит от веса предмета).
    /// Разблокируется на уровне культивации AwakenedCore (1).
    /// </summary>
    public class SpiritStorageController : MonoBehaviour
    {
        #region Configuration

        [Header("Unlock")]
        [Tooltip("Минимальный уровень культивации для разблокировки")]
        public int requiredCultivationLevel = 1; // AwakenedCore

        [Header("Qi Cost")]
        [Tooltip("Базовая стоимость Qi за операцию")]
        public long baseQiCost = 10;

        [Tooltip("Стоимость Qi за кг веса предмета")]
        public float qiCostPerKg = 5f;

        [Header("Time")]
        [Tooltip("Время доступа к складке (сек)")]
        public float accessTime = 2f;

        [Header("References")]
        [Tooltip("Контроллер инвентаря (источник предметов)")]
        public InventoryController inventoryController;

        [Tooltip("Контроллер экипировки (для снятия перед помещением)")]
        public EquipmentController equipmentController;

        #endregion

        #region Runtime Data

        /// <summary>Записи в хранилище</summary>
        private List<SpiritStorageEntry> entries = new List<SpiritStorageEntry>();

        /// <summary>Кэш записей по ID</summary>
        private Dictionary<string, SpiritStorageEntry> entryById = new Dictionary<string, SpiritStorageEntry>();

        /// <summary>Кэш записей по itemId</summary>
        private Dictionary<string, List<SpiritStorageEntry>> entriesByItemId = new Dictionary<string, List<SpiritStorageEntry>>();

        /// <summary>Разблокировано ли хранилище</summary>
        private bool isUnlocked = false;

        /// <summary>Счётчик ID для записей</summary>
        private int nextEntryId = 0;

        /// <summary>Контроллер Ци</summary>
        private QiController qiController;

        /// <summary>Суммарный вес в хранилище</summary>
        private float totalWeight = 0f;

        /// <summary>Количество предметов в хранилище</summary>
        private int totalItemCount = 0;

        #endregion

        #region Events

        /// <summary>Предмет помещён в хранилище</summary>
        public event Action<SpiritStorageEntry> OnItemStored;

        /// <summary>Предмет извлечён из хранилища</summary>
        public event Action<SpiritStorageEntry, int> OnItemRetrieved;

        /// <summary>Хранилище разблокировано</summary>
        public event Action OnStorageUnlocked;

        /// <summary>Содержимое изменено (любая операция)</summary>
        public event Action OnContentsChanged;

        /// <summary>Ошибка при операции</summary>
        public event Action<string> OnOperationFailed;

        #endregion

        #region Properties

        /// <summary>Разблокировано ли хранилище</summary>
        public bool IsUnlocked => isUnlocked;

        /// <summary>Все записи в хранилище</summary>
        public IReadOnlyList<SpiritStorageEntry> Entries => entries.AsReadOnly();

        /// <summary>Суммарный вес хранимых предметов</summary>
        public float TotalWeight => totalWeight;

        /// <summary>Количество уникальных записей</summary>
        public int EntryCount => entries.Count;

        /// <summary>Общее количество предметов (с учётом стакания)</summary>
        public int TotalItemCount => totalItemCount;

        /// <summary>Время доступа (сек)</summary>
        public float AccessTime => accessTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Подписываемся на изменение уровня культивации
            var qi = ServiceLocator.GetOrFind<QiController>();
            if (qi != null)
            {
                qiController = qi;
                qi.OnCultivationLevelChanged += OnCultivationLevelChanged;
                CheckUnlock(qi.CultivationLevel);
            }
        }

        private void OnDestroy()
        {
            if (qiController != null)
                qiController.OnCultivationLevelChanged -= OnCultivationLevelChanged;
        }

        #endregion

        #region Unlock

        private void OnCultivationLevelChanged(int level)
        {
            CheckUnlock(level);
        }

        private void CheckUnlock(int level)
        {
            if (!isUnlocked && level >= requiredCultivationLevel)
            {
                isUnlocked = true;
                OnStorageUnlocked?.Invoke();
                Debug.Log("[SpiritStorage] Межмировая складка разблокирована!");
            }
        }

        /// <summary>
        /// Принудительно разблокировать хранилище (для тестов/отладки)
        /// </summary>
        public void ForceUnlock()
        {
            if (!isUnlocked)
            {
                isUnlocked = true;
                OnStorageUnlocked?.Invoke();
            }
        }

        #endregion

        #region Cost Calculation

        /// <summary>
        /// Рассчитать стоимость Qi для помещения предмета в хранилище.
        /// Формула: baseQiCost + weight × qiCostPerKg
        /// </summary>
        /// <param name="weight">Вес предмета (кг)</param>
        /// <returns>Стоимость в Qi</returns>
        public long GetStorageCost(float weight)
        {
            return baseQiCost + Mathf.RoundToInt(weight * qiCostPerKg);
        }

        /// <summary>
        /// Рассчитать стоимость Qi для извлечения предмета из хранилища.
        /// Формула: baseQiCost + weight × qiCostPerKg (аналогичная)
        /// </summary>
        /// <param name="weight">Вес предмета (кг)</param>
        /// <returns>Стоимость в Qi</returns>
        public long GetRetrievalCost(float weight)
        {
            return baseQiCost + Mathf.RoundToInt(weight * qiCostPerKg);
        }

        /// <summary>
        /// Рассчитать стоимость Qi для нескольких предметов одного типа
        /// </summary>
        public long GetStorageCost(ItemData itemData, int count)
        {
            return GetStorageCost(itemData.weight * count);
        }

        /// <summary>
        /// Рассчитать стоимость извлечения нескольких предметов
        /// </summary>
        public long GetRetrievalCost(ItemData itemData, int count)
        {
            return GetRetrievalCost(itemData.weight * count);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Проверяет, можно ли поместить предмет в духовное хранилище.
        ///
        /// Правила:
        /// - NestingFlag.Any    → ✅
        /// - NestingFlag.Spirit → ✅ (ТОЛЬКО сюда)
        /// - NestingFlag.Ring   → ❌ (только в кольцо)
        /// - NestingFlag.None   → ❌ (запрещено)
        /// - StorageRingData    → ❌ (пространственная нестабильность)
        /// </summary>
        public bool CanStore(ItemData itemData)
        {
            if (itemData == null) return false;

            // Кольца хранения нельзя помещать (пространственная нестабильность)
            if (itemData is StorageRingData) return false;

            // Проверяем флаг вложения
            return itemData.allowNesting == NestingFlag.Any ||
                   itemData.allowNesting == NestingFlag.Spirit;
        }

        /// <summary>
        /// Проверяет, можно ли поместить предмет, с учётом Qi
        /// </summary>
        public bool CanStoreWithQi(ItemData itemData, int count = 1)
        {
            if (!CanStore(itemData)) return false;
            if (!isUnlocked) return false;

            long cost = GetStorageCost(itemData, count);
            return qiController != null && qiController.CurrentQi >= cost;
        }

        /// <summary>
        /// Проверяет, можно ли извлечь предмет, с учётом Qi и места в инвентаре
        /// </summary>
        public bool CanRetrieveWithQi(string entryId, int count = 1)
        {
            var entry = FindEntry(entryId);
            if (entry == null) return false;
            if (count > entry.count) return false;

            long cost = GetRetrievalCost(entry.ItemWeight * count);
            if (qiController == null || qiController.CurrentQi < cost) return false;

            // Проверяем место в инвентаре
            if (inventoryController != null)
            {
                if (!inventoryController.HasFreeSpace(entry.ItemData.sizeWidth, entry.ItemData.sizeHeight))
                    return false;
            }

            return true;
        }

        #endregion

        #region Store Item

        /// <summary>
        /// Перемещает предмет из инвентаря в духовное хранилище.
        /// Списывает Qi за операцию.
        /// </summary>
        /// <param name="inventorySlotId">ID слота инвентаря</param>
        /// <param name="count">Количество (если -1 = весь стак)</param>
        /// <returns>Запись в хранилище или null при ошибке</returns>
        public SpiritStorageEntry StoreFromInventory(int inventorySlotId, int count = -1)
        {
            if (!isUnlocked)
            {
                OnOperationFailed?.Invoke("Духовное хранилище заблокировано");
                return null;
            }

            if (inventoryController == null)
            {
                OnOperationFailed?.Invoke("Инвентарь не подключён");
                return null;
            }

            var slot = inventoryController.FindSlotById(inventorySlotId);
            if (slot == null)
            {
                OnOperationFailed?.Invoke("Слот инвентаря не найден");
                return null;
            }

            var itemData = slot.ItemData;
            if (!CanStore(itemData))
            {
                OnOperationFailed?.Invoke($"Предмет \"{itemData?.nameRu}\" нельзя поместить в духовное хранилище");
                return null;
            }

            int actualCount = count < 0 ? slot.Count : Mathf.Min(count, slot.Count);
            if (actualCount <= 0) return null;

            // Проверяем Qi
            long cost = GetStorageCost(itemData, actualCount);
            if (qiController != null && !qiController.SpendQi(cost))
            {
                OnOperationFailed?.Invoke($"Недостаточно Ци (нужно {cost})");
                return null;
            }

            // Удаляем из инвентаря
            inventoryController.RemoveItem(inventorySlotId, actualCount);

            // Добавляем в хранилище
            var entry = AddEntry(itemData, actualCount, slot.Durability, slot.grade);

            Debug.Log($"[SpiritStorage] Помещено: {itemData.nameRu} x{actualCount} (Qi: {cost})");
            return entry;
        }

        /// <summary>
        /// Помещает предмет напрямую в хранилище (без инвентаря).
        /// Используется при загрузке сохранения или специальных событиях.
        /// </summary>
        /// <param name="itemData">Данные предмета</param>
        /// <param name="count">Количество</param>
        /// <param name="durability">Прочность (-1 = не используется)</param>
        /// <param name="grade">Грейд экипировки</param>
        /// <returns>Запись в хранилище</returns>
        public SpiritStorageEntry StoreDirect(ItemData itemData, int count, int durability = -1, EquipmentGrade grade = EquipmentGrade.Common)
        {
            if (itemData == null || count <= 0) return null;
            return AddEntry(itemData, count, durability, grade);
        }

        #endregion

        #region Retrieve Item

        /// <summary>
        /// Извлекает предмет из хранилища в инвентарь.
        /// Списывает Qi за операцию.
        /// </summary>
        /// <param name="entryId">ID записи в хранилище</param>
        /// <param name="count">Количество (если -1 = весь стак)</param>
        /// <returns>Слот инвентаря или null при ошибке</returns>
        public InventorySlot RetrieveToInventory(string entryId, int count = -1)
        {
            if (!isUnlocked)
            {
                OnOperationFailed?.Invoke("Духовное хранилище заблокировано");
                return null;
            }

            var entry = FindEntry(entryId);
            if (entry == null)
            {
                OnOperationFailed?.Invoke("Запись не найдена");
                return null;
            }

            int actualCount = count < 0 ? entry.count : Mathf.Min(count, entry.count);
            if (actualCount <= 0) return null;

            // Проверяем Qi
            long cost = GetRetrievalCost(entry.ItemWeight * actualCount);
            if (qiController != null && !qiController.SpendQi(cost))
            {
                OnOperationFailed?.Invoke($"Недостаточно Ци (нужно {cost})");
                return null;
            }

            // Проверяем место в инвентаре
            if (inventoryController != null)
            {
                if (!inventoryController.HasFreeSpace(entry.ItemData.sizeWidth, entry.ItemData.sizeHeight))
                {
                    // Возвращаем Qi
                    if (qiController != null) qiController.AddQi(cost);
                    OnOperationFailed?.Invoke("Нет места в инвентаре");
                    return null;
                }
            }

            // Добавляем в инвентарь
            InventorySlot invSlot = null;
            if (inventoryController != null)
            {
                invSlot = inventoryController.AddItem(entry.ItemData, actualCount);
                if (invSlot != null)
                {
                    // Переносим прочность и грейд
                    if (entry.durability >= 0)
                        invSlot.durability = entry.durability;
                    invSlot.grade = entry.grade;
                }
            }

            // Удаляем из хранилища
            RemoveFromEntry(entry, actualCount);

            Debug.Log($"[SpiritStorage] Извлечено: {entry.ItemData?.nameRu} x{actualCount} (Qi: {cost})");
            return invSlot;
        }

        /// <summary>
        /// Извлекает всё содержимое записи в инвентарь.
        /// </summary>
        public InventorySlot RetrieveAllToInventory(string entryId)
        {
            return RetrieveToInventory(entryId, -1);
        }

        #endregion

        #region Catalog / Query

        /// <summary>
        /// Получить всё содержимое хранилища
        /// </summary>
        public List<SpiritStorageEntry> GetContents()
        {
            return new List<SpiritStorageEntry>(entries);
        }

        /// <summary>
        /// Фильтр по категории
        /// </summary>
        public List<SpiritStorageEntry> FilterByCategory(ItemCategory category)
        {
            return entries.Where(e => e.ItemData != null && e.ItemData.category == category).ToList();
        }

        /// <summary>
        /// Фильтр по редкости
        /// </summary>
        public List<SpiritStorageEntry> FilterByRarity(ItemRarity rarity)
        {
            return entries.Where(e => e.ItemData != null && e.ItemData.rarity == rarity).ToList();
        }

        /// <summary>
        /// Фильтр по диапазону веса
        /// </summary>
        public List<SpiritStorageEntry> FilterByWeight(float minWeight, float maxWeight)
        {
            return entries.Where(e => e.ItemData != null &&
                e.ItemData.weight >= minWeight && e.ItemData.weight <= maxWeight).ToList();
        }

        /// <summary>
        /// Текстовый поиск по названию (RU/EN) и описанию
        /// </summary>
        public List<SpiritStorageEntry> Search(string query)
        {
            if (string.IsNullOrEmpty(query)) return GetContents();

            string lowerQuery = query.ToLower();
            return entries.Where(e => e.ItemData != null &&
                (e.ItemData.nameRu?.ToLower().Contains(lowerQuery) == true ||
                 e.ItemData.nameEn?.ToLower().Contains(lowerQuery) == true ||
                 e.ItemData.description?.ToLower().Contains(lowerQuery) == true ||
                 e.ItemData.itemId?.ToLower().Contains(lowerQuery) == true)).ToList();
        }

        /// <summary>
        /// Получить записи, сгруппированные по категории
        /// </summary>
        public Dictionary<ItemCategory, List<SpiritStorageEntry>> GetGroupedByCategory()
        {
            var groups = new Dictionary<ItemCategory, List<SpiritStorageEntry>>();

            foreach (var entry in entries)
            {
                if (entry.ItemData == null) continue;

                var category = entry.ItemData.category;
                if (!groups.ContainsKey(category))
                    groups[category] = new List<SpiritStorageEntry>();

                groups[category].Add(entry);
            }

            return groups;
        }

        /// <summary>
        /// Получить записи по itemId (все стаки одного предмета)
        /// </summary>
        public List<SpiritStorageEntry> FindEntriesByItemId(string itemId)
        {
            if (entriesByItemId.TryGetValue(itemId, out var list))
                return new List<SpiritStorageEntry>(list);
            return new List<SpiritStorageEntry>();
        }

        /// <summary>
        /// Найти запись по ID
        /// </summary>
        public SpiritStorageEntry FindEntry(string entryId)
        {
            if (entryById.TryGetValue(entryId, out var entry))
                return entry;
            return null;
        }

        /// <summary>
        /// Подсчитать общее количество предмета в хранилище
        /// </summary>
        public int CountItem(string itemId)
        {
            int total = 0;
            if (entriesByItemId.TryGetValue(itemId, out var list))
            {
                foreach (var entry in list)
                    total += entry.count;
            }
            return total;
        }

        /// <summary>
        /// Проверить наличие предмета
        /// </summary>
        public bool HasItem(string itemId, int count = 1)
        {
            return CountItem(itemId) >= count;
        }

        /// <summary>
        /// Отсортировать записи (по умолчанию: категория → название → редкость)
        /// </summary>
        public void SortEntries()
        {
            entries.Sort((a, b) =>
            {
                if (a.ItemData == null || b.ItemData == null) return 0;

                int catCompare = ((int)a.ItemData.category).CompareTo((int)b.ItemData.category);
                if (catCompare != 0) return catCompare;

                int nameCompare = string.Compare(a.ItemData.nameRu, b.ItemData.nameRu, StringComparison.Ordinal);
                if (nameCompare != 0) return nameCompare;

                return ((int)a.ItemData.rarity).CompareTo((int)b.ItemData.rarity);
            });

            OnContentsChanged?.Invoke();
        }

        #endregion

        #region Internal Operations

        /// <summary>
        /// Добавляет запись в хранилище. Если предмет стакается и есть
        /// подходящая запись — увеличивает количество.
        /// </summary>
        private SpiritStorageEntry AddEntry(ItemData itemData, int count, int durability, EquipmentGrade grade)
        {
            // Пробуем добавить в существующий стак (только для стекируемых с совпадающим durability/grade)
            if (itemData.stackable)
            {
                foreach (var existing in entries)
                {
                    if (existing.ItemData.itemId == itemData.itemId &&
                        existing.durability == durability &&
                        existing.grade == grade)
                    {
                        existing.count += count;
                        existing.totalWeight = existing.ItemWeight * existing.count;
                        existing.totalVolume = existing.ItemVolume * existing.count;

                        totalWeight += itemData.weight * count;
                        totalItemCount += count;

                        OnItemStored?.Invoke(existing);
                        OnContentsChanged?.Invoke();
                        return existing;
                    }
                }
            }

            // Создаём новую запись
            var entry = new SpiritStorageEntry
            {
                entryId = GenerateEntryId(),
                itemId = itemData.itemId,
                _itemData = itemData,
                count = count,
                durability = durability,
                grade = grade,
                totalWeight = itemData.weight * count,
                totalVolume = itemData.volume * count
            };

            entries.Add(entry);
            entryById[entry.entryId] = entry;

            if (!entriesByItemId.ContainsKey(itemData.itemId))
                entriesByItemId[itemData.itemId] = new List<SpiritStorageEntry>();
            entriesByItemId[itemData.itemId].Add(entry);

            totalWeight += entry.totalWeight;
            totalItemCount += count;

            OnItemStored?.Invoke(entry);
            OnContentsChanged?.Invoke();

            return entry;
        }

        /// <summary>
        /// Уменьшает количество в записи или удаляет её
        /// </summary>
        private void RemoveFromEntry(SpiritStorageEntry entry, int count)
        {
            float removedWeight = entry.ItemWeight * count;
            int removedCount = count;

            entry.count -= count;
            entry.totalWeight = entry.ItemWeight * entry.count;
            entry.totalVolume = entry.ItemVolume * entry.count;

            totalWeight -= removedWeight;
            totalItemCount -= removedCount;

            if (entry.count <= 0)
            {
                // Удаляем запись полностью
                entries.Remove(entry);
                entryById.Remove(entry.entryId);

                if (entriesByItemId.ContainsKey(entry.itemId))
                {
                    entriesByItemId[entry.itemId].Remove(entry);
                    if (entriesByItemId[entry.itemId].Count == 0)
                        entriesByItemId.Remove(entry.itemId);
                }
            }

            OnItemRetrieved?.Invoke(entry, count);
            OnContentsChanged?.Invoke();
        }

        /// <summary>
        /// Генерирует уникальный ID записи
        /// </summary>
        private string GenerateEntryId()
        {
            return $"spirit_{nextEntryId++}";
        }

        #endregion

        #region Clear

        /// <summary>
        /// Очищает всё содержимое хранилища
        /// </summary>
        public void Clear()
        {
            entries.Clear();
            entryById.Clear();
            entriesByItemId.Clear();
            totalWeight = 0f;
            totalItemCount = 0;
            nextEntryId = 0;

            OnContentsChanged?.Invoke();
        }

        #endregion

        #region Save / Load

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public SpiritStorageSaveData GetSaveData()
        {
            var entryData = new SpiritStorageEntrySaveData[entries.Count];
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                entryData[i] = new SpiritStorageEntrySaveData
                {
                    entryId = e.entryId,
                    itemId = e.itemId,
                    count = e.count,
                    durability = e.durability,
                    grade = (int)e.grade
                };
            }

            return new SpiritStorageSaveData
            {
                entries = entryData,
                isUnlocked = isUnlocked,
                nextEntryId = nextEntryId
            };
        }

        /// <summary>
        /// Загрузить данные из сохранения
        /// </summary>
        public void LoadSaveData(SpiritStorageSaveData data, Dictionary<string, ItemData> itemDatabase)
        {
            Clear();

            if (data == null) return;

            isUnlocked = data.isUnlocked;
            nextEntryId = data.nextEntryId;

            if (data.entries != null)
            {
                foreach (var entryData in data.entries)
                {
                    if (itemDatabase.TryGetValue(entryData.itemId, out var itemData))
                    {
                        var entry = new SpiritStorageEntry
                        {
                            entryId = entryData.entryId,
                            itemId = entryData.itemId,
                            _itemData = itemData,
                            count = entryData.count,
                            durability = entryData.durability,
                            grade = (EquipmentGrade)entryData.grade,
                            totalWeight = itemData.weight * entryData.count,
                            totalVolume = itemData.volume * entryData.count
                        };

                        entries.Add(entry);
                        entryById[entry.entryId] = entry;

                        if (!entriesByItemId.ContainsKey(entry.itemId))
                            entriesByItemId[entry.itemId] = new List<SpiritStorageEntry>();
                        entriesByItemId[entry.itemId].Add(entry);

                        totalWeight += entry.totalWeight;
                        totalItemCount += entry.count;
                    }
                }
            }

            OnContentsChanged?.Invoke();
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [ContextMenu("Dump Contents")]
        private void DumpContents()
        {
            Debug.Log($"[SpiritStorage] Unlocked: {isUnlocked}, Entries: {entries.Count}, Items: {totalItemCount}, Weight: {totalWeight:F1}kg");
            foreach (var entry in entries)
            {
                Debug.Log($"  {entry.entryId}: {entry.ItemData?.nameRu} x{entry.count} ({entry.totalWeight:F1}kg)");
            }
        }

        [ContextMenu("Force Unlock")]
        private void EditorForceUnlock()
        {
            ForceUnlock();
        }
#endif

        #endregion
    }

    // ============================================================================
    // SpiritStorageEntry — Запись в духовном хранилище
    // ============================================================================
    // В отличие от инвентаря (сетка), хранилище использует каталог —
    // записи без привязки к позиции. Стакающиеся предметы
    // объединяются в одну запись (если совпадает durability/grade).
    // ============================================================================

    [Serializable]
    public class SpiritStorageEntry
    {
        /// <summary>Уникальный ID записи</summary>
        public string entryId;

        /// <summary>ID предмета (ссылка на ItemData.itemId)</summary>
        public string itemId;

        /// <summary>Ссылка на ItemData (runtime, не сериализуется)</summary>
        [NonSerialized]
        internal ItemData _itemData;

        /// <summary>Количество</summary>
        public int count;

        /// <summary>Прочность (-1 = не используется)</summary>
        public int durability = -1;

        /// <summary>Грейд экипировки</summary>
        public EquipmentGrade grade = EquipmentGrade.Common;

        /// <summary>Суммарный вес (кэш)</summary>
        public float totalWeight;

        /// <summary>Суммарный объём (кэш)</summary>
        public float totalVolume;

        // === Вычисляемые свойства ===

        /// <summary>Ссылка на ItemData</summary>
        public ItemData ItemData => _itemData;

        /// <summary>Вес одного предмета</summary>
        public float ItemWeight => _itemData?.weight ?? 0f;

        /// <summary>Объём одного предмета</summary>
        public float ItemVolume => _itemData?.volume ?? 0f;

        /// <summary>Название предмета (RU)</summary>
        public string NameRu => _itemData?.nameRu ?? itemId;

        /// <summary>Редкость</summary>
        public ItemRarity Rarity => _itemData?.rarity ?? ItemRarity.Common;

        /// <summary>Категория</summary>
        public ItemCategory Category => _itemData?.category ?? ItemCategory.Misc;

        /// <summary>Иконка</summary>
        public Sprite Icon => _itemData?.icon;
    }

    // ============================================================================
    // SpiritStorageSaveData — Данные для сохранения
    // ============================================================================

    [Serializable]
    public class SpiritStorageSaveData
    {
        public SpiritStorageEntrySaveData[] entries;
        public bool isUnlocked;
        public int nextEntryId;
    }

    [Serializable]
    public class SpiritStorageEntrySaveData
    {
        public string entryId;
        public string itemId;
        public int count;
        public int durability;
        public int grade; // EquipmentGrade as int
    }
}
