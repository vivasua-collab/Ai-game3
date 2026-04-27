// ============================================================================
// StorageRingController.cs — Контроллер кольца хранения
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-04-19 14:00:00 UTC
// Редактировано: 2026-04-27 18:15:00 UTC — строчная модель инвентаря
// Редактировано: 2026-04-20 06:50:00 UTC — StorageRingData теперь наследует от EquipmentData
// ============================================================================
// Кольцо хранения — объём-ограниченное хранилище, экипируется на слот
// кольца (RingLeft1/2, RingRight1/2) на кукле персонажа.
// Стоимость доступа = qiCostBase + volume × qiCostPerUnit (из StorageRingData).
// Время доступа = 1.5 сек (быстрее духового хранилища — 2 сек).
//
// Правила вложения (NestingFlag):
//   Any     → можно поместить ✅
//   Ring    → можно поместить ✅ (ТОЛЬКО сюда и в духовное — нет, ТОЛЬКО в кольцо)
//   Spirit  → НЕЛЬЗЯ ❌ (только в духовное хранилище)
//   None    → НЕЛЬЗЯ ❌ (квестовые, живые существа)
//
// StorageRingData → НЕЛЬЗЯ поместить (пространственная нестабильность)
// Кольцо → Духовное: ЗАПРЕЩЕНО
// Кольцо → Кольцо: ЗАПРЕЩЕНО (пространственная нестабильность)
// Духовное → Кольцо: ЗАПРЕЩЕНО (сначала через рюкзак)
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
    /// Контроллер кольца хранения.
    ///
    /// Объём-ограниченное хранилище, привязанное к слоту кольца на кукле.
    /// Стоимость доступа = Qi (зависит от объёма предмета).
    /// НЕ требует уровня культивации — достаточно экипировать кольцо.
    /// Игрок может носить до 4 колец, но одновременно работать с одним в UI.
    /// </summary>
    public class StorageRingController : MonoBehaviour
    {
        #region Constants

        /// <summary>Слоты колец на кукле</summary>
        public static readonly EquipmentSlot[] RingSlots = new[]
        {
            EquipmentSlot.RingLeft1, EquipmentSlot.RingLeft2,
            EquipmentSlot.RingRight1, EquipmentSlot.RingRight2
        };

        #endregion

        #region Configuration

        [Header("References")]
        [Tooltip("Контроллер инвентаря (источник/приёмник предметов)")]
        public InventoryController inventoryController;

        [Tooltip("Контроллер экипировки (отслеживание слотов колец)")]
        public EquipmentController equipmentController;

        #endregion

        #region Runtime Data

        /// <summary>Записи по каждому слоту кольца</summary>
        private Dictionary<EquipmentSlot, List<StorageRingEntry>> ringEntries =
            new Dictionary<EquipmentSlot, List<StorageRingEntry>>();

        /// <summary>Текущий объём по каждому слоту кольца</summary>
        private Dictionary<EquipmentSlot, float> ringCurrentVolume =
            new Dictionary<EquipmentSlot, float>();

        /// <summary>Активные (экипированные) кольца: слот → данные кольца</summary>
        private Dictionary<EquipmentSlot, StorageRingData> activeRings =
            new Dictionary<EquipmentSlot, StorageRingData>();

        /// <summary>Кэш записей по entryId для каждого слота</summary>
        private Dictionary<EquipmentSlot, Dictionary<string, StorageRingEntry>> ringEntryById =
            new Dictionary<EquipmentSlot, Dictionary<string, StorageRingEntry>>();

        /// <summary>Кэш записей по itemId для каждого слота</summary>
        private Dictionary<EquipmentSlot, Dictionary<string, List<StorageRingEntry>>> ringEntriesByItemId =
            new Dictionary<EquipmentSlot, Dictionary<string, List<StorageRingEntry>>>();

        /// <summary>Счётчик ID для записей</summary>
        private int nextEntryId = 0;

        /// <summary>Контроллер Ци</summary>
        private QiController qiController;

        #endregion

        #region Events

        /// <summary>Предмет помещён в кольцо хранения</summary>
        public event Action<StorageRingEntry> OnItemStored;

        /// <summary>Предмет извлечён из кольца хранения</summary>
        public event Action<StorageRingEntry, int> OnItemRetrieved;

        /// <summary>Кольцо хранения активировано (экипировано)</summary>
        public event Action<EquipmentSlot, StorageRingData> OnRingStorageActivated;

        /// <summary>Кольцо хранения деактивировано (снято)</summary>
        public event Action<EquipmentSlot> OnRingStorageDeactivated;

        /// <summary>Содержимое кольца изменено (любая операция)</summary>
        public event Action<EquipmentSlot> OnContentsChanged;

        /// <summary>Ошибка при операции</summary>
        public event Action<string> OnOperationFailed;

        #endregion

        #region Properties

        /// <summary>Получить данные активного кольца для указанного слота</summary>
        public StorageRingData GetActiveRing(EquipmentSlot slot)
        {
            return activeRings.TryGetValue(slot, out var data) ? data : null;
        }

        /// <summary>Активен ли слот кольца (экипировано ли кольцо хранения)</summary>
        public bool IsRingSlotActive(EquipmentSlot slot)
        {
            return activeRings.ContainsKey(slot) && activeRings[slot] != null;
        }

        /// <summary>Текущий объём заполнения для указанного слота</summary>
        public float GetCurrentVolume(EquipmentSlot slot)
        {
            return ringCurrentVolume.TryGetValue(slot, out var vol) ? vol : 0f;
        }

        /// <summary>Максимальный объём для указанного слота (из StorageRingData)</summary>
        public float GetMaxVolume(EquipmentSlot slot)
        {
            var ring = GetActiveRing(slot);
            return ring != null ? ring.maxVolume : 0f;
        }

        /// <summary>Процент заполнения объёма для указанного слота (0..1)</summary>
        public float GetVolumePercent(EquipmentSlot slot)
        {
            float maxVol = GetMaxVolume(slot);
            if (maxVol <= 0f) return 0f;
            return GetCurrentVolume(slot) / maxVol;
        }

        /// <summary>Получить все записи для указанного слота кольца</summary>
        public IReadOnlyList<StorageRingEntry> GetEntries(EquipmentSlot slot)
        {
            if (ringEntries.TryGetValue(slot, out var list))
                return list.AsReadOnly();
            return new List<StorageRingEntry>().AsReadOnly();
        }

        /// <summary>Время доступа (сек) — из первого активного кольца</summary>
        public float AccessTime
        {
            get
            {
                // Возвращаем время доступа первого активного кольца
                foreach (var kvp in activeRings)
                {
                    if (kvp.Value != null)
                        return kvp.Value.accessTime;
                }
                return 1.5f; // значение по умолчанию
            }
        }

        /// <summary>Время доступа для конкретного слота</summary>
        public float GetAccessTime(EquipmentSlot slot)
        {
            var ring = GetActiveRing(slot);
            return ring != null ? ring.accessTime : 1.5f;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Инициализируем структуры данных для всех слотов колец
            foreach (var slot in RingSlots)
            {
                ringEntries[slot] = new List<StorageRingEntry>();
                ringCurrentVolume[slot] = 0f;
                ringEntryById[slot] = new Dictionary<string, StorageRingEntry>();
                ringEntriesByItemId[slot] = new Dictionary<string, List<StorageRingEntry>>();
            }

            // Получаем контроллер Ци
            qiController = ServiceLocator.GetOrFind<QiController>();
        }

        private void OnEnable()
        {
            // Подписываемся на события экипировки
            if (equipmentController != null)
            {
                equipmentController.OnEquipmentEquipped += OnEquipmentEquipped;
                equipmentController.OnEquipmentUnequipped += OnEquipmentUnequipped;
            }
        }

        private void OnDisable()
        {
            // Отписываемся от событий экипировки
            if (equipmentController != null)
            {
                equipmentController.OnEquipmentEquipped -= OnEquipmentEquipped;
                equipmentController.OnEquipmentUnequipped -= OnEquipmentUnequipped;
            }
        }

        #endregion

        #region Equipment Event Handlers

        /// <summary>
        /// Обработка экипировки предмета — проверяем, не кольцо ли хранения
        /// </summary>
        private void OnEquipmentEquipped(EquipmentSlot slot, EquipmentInstance instance)
        {
            // Проверяем, что это слот кольца
            if (Array.IndexOf(RingSlots, slot) < 0) return;

            // Проверяем, что экипированный предмет — кольцо хранения
            if (instance?.equipmentData is StorageRingData ringData)
            {
                ActivateRing(slot, ringData);
            }
        }

        /// <summary>
        /// Обработка снятия предмета — проверяем, не кольцо ли хранения
        /// </summary>
        private void OnEquipmentUnequipped(EquipmentSlot slot, EquipmentInstance instance)
        {
            // Проверяем, что это слот кольца
            if (Array.IndexOf(RingSlots, slot) < 0) return;

            // Если в этом слоте было активное кольцо — деактивируем
            if (IsRingSlotActive(slot))
            {
                DeactivateRing(slot);
            }
        }

        #endregion

        #region Ring Activation

        /// <summary>
        /// Активировать кольцо хранения на указанном слоте.
        /// Вызывается автоматически при экипировке StorageRingData.
        /// </summary>
        /// <param name="slot">Слот кольца</param>
        /// <param name="ringData">Данные кольца хранения</param>
        public void ActivateRing(EquipmentSlot slot, StorageRingData ringData)
        {
            if (ringData == null)
            {
                Debug.LogWarning("[StorageRing] Попытка активировать null-кольцо");
                return;
            }

            if (Array.IndexOf(RingSlots, slot) < 0)
            {
                Debug.LogWarning($"[StorageRing] Слот {slot} не является слотом кольца");
                return;
            }

            // Если в слоте уже было другое кольцо — сначала деактивируем
            if (IsRingSlotActive(slot))
            {
                DeactivateRing(slot);
            }

            activeRings[slot] = ringData;

            // Гарантируем, что структуры данных для слота инициализированы
            if (!ringEntries.ContainsKey(slot))
                ringEntries[slot] = new List<StorageRingEntry>();
            if (!ringCurrentVolume.ContainsKey(slot))
                ringCurrentVolume[slot] = 0f;
            if (!ringEntryById.ContainsKey(slot))
                ringEntryById[slot] = new Dictionary<string, StorageRingEntry>();
            if (!ringEntriesByItemId.ContainsKey(slot))
                ringEntriesByItemId[slot] = new Dictionary<string, List<StorageRingEntry>>();

            OnRingStorageActivated?.Invoke(slot, ringData);
            Debug.Log($"[StorageRing] Кольцо активировано: {ringData.nameRu} на слоте {slot}");
        }

        /// <summary>
        /// Деактивировать кольцо хранения на указанном слоте.
        /// Предметы остаются в слоте, но становятся недоступны,
        /// пока кольцо не будет экипировано снова.
        /// </summary>
        /// <param name="slot">Слот кольца</param>
        public void DeactivateRing(EquipmentSlot slot)
        {
            if (!IsRingSlotActive(slot))
            {
                Debug.LogWarning($"[StorageRing] Слот {slot} не содержит активного кольца");
                return;
            }

            var ringData = activeRings[slot];
            activeRings.Remove(slot);

            OnRingStorageDeactivated?.Invoke(slot);
            Debug.Log($"[StorageRing] Кольцо деактивировано: {ringData?.nameRu} на слоте {slot}");
        }

        #endregion

        #region Cost Calculation

        /// <summary>
        /// Рассчитать стоимость Qi для помещения предмета в кольцо хранения.
        /// Формула: qiCostBase + volume × qiCostPerUnit (из StorageRingData)
        /// </summary>
        /// <param name="ringSlot">Слот кольца</param>
        /// <param name="volume">Объём предмета(ов)</param>
        /// <returns>Стоимость в Qi</returns>
        public long GetStorageCost(EquipmentSlot ringSlot, float volume)
        {
            var ring = GetActiveRing(ringSlot);
            if (ring == null) return 0;
            return ring.qiCostBase + (long)Math.Ceiling(volume * ring.qiCostPerUnit);
        }

        /// <summary>
        /// Рассчитать стоимость Qi для извлечения предмета из кольца хранения.
        /// Формула: qiCostBase + volume × qiCostPerUnit (аналогичная)
        /// </summary>
        /// <param name="ringSlot">Слот кольца</param>
        /// <param name="volume">Объём предмета(ов)</param>
        /// <returns>Стоимость в Qi</returns>
        public long GetRetrievalCost(EquipmentSlot ringSlot, float volume)
        {
            var ring = GetActiveRing(ringSlot);
            if (ring == null) return 0;
            return ring.qiCostBase + (long)Math.Ceiling(volume * ring.qiCostPerUnit);
        }

        /// <summary>
        /// Рассчитать стоимость Qi для нескольких предметов одного типа
        /// </summary>
        public long GetStorageCost(EquipmentSlot ringSlot, ItemData itemData, int count)
        {
            return GetStorageCost(ringSlot, itemData.volume * count);
        }

        /// <summary>
        /// Рассчитать стоимость извлечения нескольких предметов
        /// </summary>
        public long GetRetrievalCost(EquipmentSlot ringSlot, ItemData itemData, int count)
        {
            return GetRetrievalCost(ringSlot, itemData.volume * count);
        }

        #endregion

        #region Validation

        /// <summary>
        /// Проверяет, можно ли поместить предмет в кольцо хранения.
        ///
        /// Правила:
        /// - NestingFlag.Any    → ✅
        /// - NestingFlag.Ring   → ✅ (ТОЛЬКО сюда)
        /// - NestingFlag.Spirit → ❌ (только в духовное хранилище)
        /// - NestingFlag.None   → ❌ (запрещено)
        /// - StorageRingData    → ❌ (пространственная нестабильность)
        /// </summary>
        /// <param name="ringSlot">Слот кольца</param>
        /// <param name="itemData">Данные предмета</param>
        /// <returns>Можно ли поместить</returns>
        public bool CanStore(EquipmentSlot ringSlot, ItemData itemData)
        {
            if (itemData == null) return false;

            // Кольцо не экипировано — нельзя хранить
            if (!IsRingSlotActive(ringSlot)) return false;

            // Кольца хранения нельзя помещать (пространственная нестабильность)
            if (itemData is StorageRingData) return false;

            // Проверяем флаг вложения: Any и Ring — разрешены
            return itemData.allowNesting == NestingFlag.Any ||
                   itemData.allowNesting == NestingFlag.Ring;
        }

        /// <summary>
        /// Проверяет, можно ли поместить предмет, с учётом Qi и объёма
        /// </summary>
        /// <param name="ringSlot">Слот кольца</param>
        /// <param name="itemData">Данные предмета</param>
        /// <param name="count">Количество</param>
        /// <returns>Можно ли поместить с учётом всех ограничений</returns>
        public bool CanStoreWithQi(EquipmentSlot ringSlot, ItemData itemData, int count)
        {
            if (!CanStore(ringSlot, itemData)) return false;

            // Проверяем объём
            float itemVolume = itemData.volume * count;
            float remainingVolume = GetMaxVolume(ringSlot) - GetCurrentVolume(ringSlot);
            if (itemVolume > remainingVolume) return false;

            // Проверяем Qi
            long cost = GetStorageCost(ringSlot, itemData, count);
            return qiController != null && qiController.CurrentQi >= cost;
        }

        /// <summary>
        /// Проверяет, можно ли извлечь предмет, с учётом Qi и места в инвентаре
        /// </summary>
        public bool CanRetrieveWithQi(EquipmentSlot ringSlot, string entryId, int count = 1)
        {
            var entry = FindEntry(ringSlot, entryId);
            if (entry == null) return false;
            if (count > entry.count) return false;

            // Проверяем Qi
            long cost = GetRetrievalCost(ringSlot, entry.ItemVolume * count);
            if (qiController == null || qiController.CurrentQi < cost) return false;

            // Проверяем место в инвентаре
            if (inventoryController != null)
            {
                if (!inventoryController.CanFitItem(entry.ItemData, count))
                    return false;
            }

            return true;
        }

        #endregion

        #region Store Item

        /// <summary>
        /// Перемещает предмет из инвентаря в кольцо хранения.
        /// Списывает Qi за операцию.
        /// </summary>
        /// <param name="ringSlot">Слот кольца-хранилища</param>
        /// <param name="inventorySlotId">ID слота инвентаря</param>
        /// <param name="count">Количество (если -1 = весь стак)</param>
        /// <returns>Запись в хранилище или null при ошибке</returns>
        public StorageRingEntry StoreFromInventory(EquipmentSlot ringSlot, int inventorySlotId, int count = -1)
        {
            // Проверяем, что кольцо активно
            if (!IsRingSlotActive(ringSlot))
            {
                OnOperationFailed?.Invoke("Кольцо хранения не экипировано");
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
            if (!CanStore(ringSlot, itemData))
            {
                OnOperationFailed?.Invoke($"Предмет \"{itemData?.nameRu}\" нельзя поместить в кольцо хранения");
                return null;
            }

            int actualCount = count < 0 ? slot.Count : Mathf.Min(count, slot.Count);
            if (actualCount <= 0) return null;

            // Проверяем объём
            float itemVolume = itemData.volume * actualCount;
            float remainingVolume = GetMaxVolume(ringSlot) - GetCurrentVolume(ringSlot);
            if (itemVolume > remainingVolume)
            {
                // Пробуем поместить сколько влезет
                int maxFit = Mathf.FloorToInt(remainingVolume / itemData.volume);
                if (maxFit <= 0)
                {
                    OnOperationFailed?.Invoke("Недостаточно объёма в кольце хранения");
                    return null;
                }
                actualCount = Mathf.Min(actualCount, maxFit);
                itemVolume = itemData.volume * actualCount;
            }

            // Проверяем Qi
            long cost = GetStorageCost(ringSlot, itemVolume);
            if (qiController != null && !qiController.SpendQi(cost))
            {
                OnOperationFailed?.Invoke($"Недостаточно Ци (нужно {cost})");
                return null;
            }

            // Удаляем из инвентаря
            inventoryController.RemoveItem(inventorySlotId, actualCount);

            // Добавляем в кольцо хранения
            var entry = AddEntry(ringSlot, itemData, actualCount, slot.Durability, slot.grade);

            Debug.Log($"[StorageRing] Помещено: {itemData.nameRu} x{actualCount} в {ringSlot} (Qi: {cost}, Vol: {itemVolume:F1})");
            return entry;
        }

        /// <summary>
        /// Помещает предмет напрямую в кольцо хранения (без инвентаря).
        /// Используется при загрузке сохранения или специальных событиях.
        /// </summary>
        /// <param name="ringSlot">Слот кольца-хранилища</param>
        /// <param name="itemData">Данные предмета</param>
        /// <param name="count">Количество</param>
        /// <param name="durability">Прочность (-1 = не используется)</param>
        /// <param name="grade">Грейд экипировки</param>
        /// <returns>Запись в хранилище</returns>
        public StorageRingEntry StoreDirect(EquipmentSlot ringSlot, ItemData itemData, int count, int durability = -1, EquipmentGrade grade = EquipmentGrade.Common)
        {
            if (itemData == null || count <= 0) return null;
            return AddEntry(ringSlot, itemData, count, durability, grade);
        }

        #endregion

        #region Retrieve Item

        /// <summary>
        /// Извлекает предмет из кольца хранения в инвентарь.
        /// Списывает Qi за операцию.
        /// </summary>
        /// <param name="ringSlot">Слот кольца-хранилища</param>
        /// <param name="entryId">ID записи в хранилище</param>
        /// <param name="count">Количество (если -1 = весь стак)</param>
        /// <returns>Слот инвентаря или null при ошибке</returns>
        public InventorySlot RetrieveToInventory(EquipmentSlot ringSlot, string entryId, int count = -1)
        {
            // Проверяем, что кольцо активно
            if (!IsRingSlotActive(ringSlot))
            {
                OnOperationFailed?.Invoke("Кольцо хранения не экипировано");
                return null;
            }

            var entry = FindEntry(ringSlot, entryId);
            if (entry == null)
            {
                OnOperationFailed?.Invoke("Запись не найдена");
                return null;
            }

            int actualCount = count < 0 ? entry.count : Mathf.Min(count, entry.count);
            if (actualCount <= 0) return null;

            // Проверяем Qi
            float retrieveVolume = entry.ItemVolume * actualCount;
            long cost = GetRetrievalCost(ringSlot, retrieveVolume);
            if (qiController != null && !qiController.SpendQi(cost))
            {
                OnOperationFailed?.Invoke($"Недостаточно Ци (нужно {cost})");
                return null;
            }

            // Проверяем место в инвентаре
            if (inventoryController != null)
            {
                if (!inventoryController.CanFitItem(entry.ItemData, actualCount))
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

            // Удаляем из кольца хранения
            RemoveFromEntry(ringSlot, entry, actualCount);

            Debug.Log($"[StorageRing] Извлечено: {entry.ItemData?.nameRu} x{actualCount} из {ringSlot} (Qi: {cost})");
            return invSlot;
        }

        #endregion

        #region Catalog / Query

        /// <summary>
        /// Получить всё содержимое кольца для указанного слота
        /// </summary>
        public List<StorageRingEntry> GetContents(EquipmentSlot slot)
        {
            if (ringEntries.TryGetValue(slot, out var list))
                return new List<StorageRingEntry>(list);
            return new List<StorageRingEntry>();
        }

        /// <summary>
        /// Фильтр по категории для указанного слота
        /// </summary>
        public List<StorageRingEntry> FilterByCategory(EquipmentSlot slot, ItemCategory category)
        {
            if (!ringEntries.TryGetValue(slot, out var list))
                return new List<StorageRingEntry>();

            return list.Where(e => e.ItemData != null && e.ItemData.category == category).ToList();
        }

        /// <summary>
        /// Фильтр по редкости для указанного слота
        /// </summary>
        public List<StorageRingEntry> FilterByRarity(EquipmentSlot slot, ItemRarity rarity)
        {
            if (!ringEntries.TryGetValue(slot, out var list))
                return new List<StorageRingEntry>();

            return list.Where(e => e.ItemData != null && e.ItemData.rarity == rarity).ToList();
        }

        /// <summary>
        /// Фильтр по диапазону веса для указанного слота
        /// </summary>
        public List<StorageRingEntry> FilterByWeight(EquipmentSlot slot, float minWeight, float maxWeight)
        {
            if (!ringEntries.TryGetValue(slot, out var list))
                return new List<StorageRingEntry>();

            return list.Where(e => e.ItemData != null &&
                e.ItemData.weight >= minWeight && e.ItemData.weight <= maxWeight).ToList();
        }

        /// <summary>
        /// Текстовый поиск по названию (RU/EN) и описанию для указанного слота
        /// </summary>
        public List<StorageRingEntry> Search(EquipmentSlot slot, string query)
        {
            if (!ringEntries.TryGetValue(slot, out var list))
                return new List<StorageRingEntry>();

            if (string.IsNullOrEmpty(query))
                return new List<StorageRingEntry>(list);

            string lowerQuery = query.ToLower();
            return list.Where(e => e.ItemData != null &&
                (e.ItemData.nameRu?.ToLower().Contains(lowerQuery) == true ||
                 e.ItemData.nameEn?.ToLower().Contains(lowerQuery) == true ||
                 e.ItemData.description?.ToLower().Contains(lowerQuery) == true ||
                 e.ItemData.itemId?.ToLower().Contains(lowerQuery) == true)).ToList();
        }

        /// <summary>
        /// Получить записи, сгруппированные по категории для указанного слота
        /// </summary>
        public Dictionary<ItemCategory, List<StorageRingEntry>> GetGroupedByCategory(EquipmentSlot slot)
        {
            var groups = new Dictionary<ItemCategory, List<StorageRingEntry>>();

            if (!ringEntries.TryGetValue(slot, out var list))
                return groups;

            foreach (var entry in list)
            {
                if (entry.ItemData == null) continue;

                var category = entry.ItemData.category;
                if (!groups.ContainsKey(category))
                    groups[category] = new List<StorageRingEntry>();

                groups[category].Add(entry);
            }

            return groups;
        }

        /// <summary>
        /// Получить записи по itemId (все стаки одного предмета) для указанного слота
        /// </summary>
        public List<StorageRingEntry> FindEntriesByItemId(EquipmentSlot slot, string itemId)
        {
            if (ringEntriesByItemId.TryGetValue(slot, out var byItemId))
            {
                if (byItemId.TryGetValue(itemId, out var list))
                    return new List<StorageRingEntry>(list);
            }
            return new List<StorageRingEntry>();
        }

        /// <summary>
        /// Найти запись по ID для указанного слота
        /// </summary>
        public StorageRingEntry FindEntry(EquipmentSlot slot, string entryId)
        {
            if (ringEntryById.TryGetValue(slot, out var byId))
            {
                if (byId.TryGetValue(entryId, out var entry))
                    return entry;
            }
            return null;
        }

        /// <summary>
        /// Подсчитать общее количество предмета в кольце для указанного слота
        /// </summary>
        public int CountItem(EquipmentSlot slot, string itemId)
        {
            int total = 0;
            if (ringEntriesByItemId.TryGetValue(slot, out var byItemId))
            {
                if (byItemId.TryGetValue(itemId, out var list))
                {
                    foreach (var entry in list)
                        total += entry.count;
                }
            }
            return total;
        }

        /// <summary>
        /// Проверить наличие предмета в кольце для указанного слота
        /// </summary>
        public bool HasItem(EquipmentSlot slot, string itemId, int count = 1)
        {
            return CountItem(slot, itemId) >= count;
        }

        /// <summary>
        /// Отсортировать записи для указанного слота (по умолчанию: категория → название → редкость)
        /// </summary>
        public void SortEntries(EquipmentSlot slot)
        {
            if (!ringEntries.TryGetValue(slot, out var list))
                return;

            list.Sort((a, b) =>
            {
                if (a.ItemData == null || b.ItemData == null) return 0;

                int catCompare = ((int)a.ItemData.category).CompareTo((int)b.ItemData.category);
                if (catCompare != 0) return catCompare;

                int nameCompare = string.Compare(a.ItemData.nameRu, b.ItemData.nameRu, StringComparison.Ordinal);
                if (nameCompare != 0) return nameCompare;

                return ((int)a.ItemData.rarity).CompareTo((int)b.ItemData.rarity);
            });

            OnContentsChanged?.Invoke(slot);
        }

        #endregion

        #region Internal Operations

        /// <summary>
        /// Добавляет запись в кольцо хранения. Если предмет стакается и есть
        /// подходящая запись — увеличивает количество.
        /// </summary>
        private StorageRingEntry AddEntry(EquipmentSlot slot, ItemData itemData, int count, int durability, EquipmentGrade grade)
        {
            var list = ringEntries[slot];
            var byId = ringEntryById[slot];
            var byItemId = ringEntriesByItemId[slot];

            // Пробуем добавить в существующий стак (только для стекируемых с совпадающим durability/grade)
            if (itemData.stackable)
            {
                foreach (var existing in list)
                {
                    if (existing.ItemData.itemId == itemData.itemId &&
                        existing.durability == durability &&
                        existing.grade == grade)
                    {
                        existing.count += count;
                        existing.totalWeight = existing.ItemWeight * existing.count;
                        existing.totalVolume = existing.ItemVolume * existing.count;

                        ringCurrentVolume[slot] += itemData.volume * count;

                        OnItemStored?.Invoke(existing);
                        OnContentsChanged?.Invoke(slot);
                        return existing;
                    }
                }
            }

            // Создаём новую запись
            var entry = new StorageRingEntry
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

            list.Add(entry);
            byId[entry.entryId] = entry;

            if (!byItemId.ContainsKey(itemData.itemId))
                byItemId[itemData.itemId] = new List<StorageRingEntry>();
            byItemId[itemData.itemId].Add(entry);

            ringCurrentVolume[slot] += entry.totalVolume;

            OnItemStored?.Invoke(entry);
            OnContentsChanged?.Invoke(slot);

            return entry;
        }

        /// <summary>
        /// Уменьшает количество в записи или удаляет её
        /// </summary>
        private void RemoveFromEntry(EquipmentSlot slot, StorageRingEntry entry, int count)
        {
            float removedVolume = entry.ItemVolume * count;

            var list = ringEntries[slot];
            var byId = ringEntryById[slot];
            var byItemId = ringEntriesByItemId[slot];

            entry.count -= count;
            entry.totalWeight = entry.ItemWeight * entry.count;
            entry.totalVolume = entry.ItemVolume * entry.count;

            ringCurrentVolume[slot] -= removedVolume;

            // Гарантируем, что объём не уйдёт в минус из-за погрешностей float
            if (ringCurrentVolume[slot] < 0f)
                ringCurrentVolume[slot] = 0f;

            if (entry.count <= 0)
            {
                // Удаляем запись полностью
                list.Remove(entry);
                byId.Remove(entry.entryId);

                if (byItemId.ContainsKey(entry.itemId))
                {
                    byItemId[entry.itemId].Remove(entry);
                    if (byItemId[entry.itemId].Count == 0)
                        byItemId.Remove(entry.itemId);
                }
            }

            OnItemRetrieved?.Invoke(entry, count);
            OnContentsChanged?.Invoke(slot);
        }

        /// <summary>
        /// Генерирует уникальный ID записи
        /// </summary>
        private string GenerateEntryId()
        {
            return $"ring_{nextEntryId++}";
        }

        #endregion

        #region Clear

        /// <summary>
        /// Очищает всё содержимое кольца для указанного слота
        /// </summary>
        public void Clear(EquipmentSlot slot)
        {
            if (ringEntries.TryGetValue(slot, out var list))
                list.Clear();
            if (ringEntryById.TryGetValue(slot, out var byId))
                byId.Clear();
            if (ringEntriesByItemId.TryGetValue(slot, out var byItemId))
                byItemId.Clear();

            ringCurrentVolume[slot] = 0f;

            OnContentsChanged?.Invoke(slot);
        }

        /// <summary>
        /// Очищает все кольца (используется при перезагрузке)
        /// </summary>
        public void ClearAll()
        {
            foreach (var slot in RingSlots)
            {
                Clear(slot);
            }
            nextEntryId = 0;
        }

        #endregion

        #region Save / Load

        /// <summary>
        /// Получить данные для сохранения
        /// </summary>
        public StorageRingSaveData GetSaveData()
        {
            var slotDataList = new List<StorageRingSlotSaveData>();

            foreach (var slot in RingSlots)
            {
                if (!ringEntries.TryGetValue(slot, out var list))
                    continue;

                // Сохраняем данные слота, даже если кольцо не активно
                // (предметы остаются, но недоступны — можно восстановить при повторной экипировке)
                var entryData = new StorageRingEntrySaveData[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    var e = list[i];
                    entryData[i] = new StorageRingEntrySaveData
                    {
                        entryId = e.entryId,
                        itemId = e.itemId,
                        count = e.count,
                        durability = e.durability,
                        grade = (int)e.grade
                    };
                }

                slotDataList.Add(new StorageRingSlotSaveData
                {
                    slot = slot,
                    entries = entryData,
                    currentVolume = ringCurrentVolume[slot],
                    nextEntryId = nextEntryId // сохраняем глобальный счётчик
                });
            }

            return new StorageRingSaveData
            {
                slots = slotDataList.ToArray(),
                nextEntryId = nextEntryId
            };
        }

        /// <summary>
        /// Загрузить данные из сохранения
        /// </summary>
        public void LoadSaveData(StorageRingSaveData data, Dictionary<string, ItemData> itemDatabase)
        {
            ClearAll();

            if (data == null) return;

            nextEntryId = data.nextEntryId;

            if (data.slots != null)
            {
                foreach (var slotData in data.slots)
                {
                    var slot = slotData.slot;

                    // Инициализируем структуры для слота, если ещё не созданы
                    if (!ringEntries.ContainsKey(slot))
                    {
                        ringEntries[slot] = new List<StorageRingEntry>();
                        ringCurrentVolume[slot] = 0f;
                        ringEntryById[slot] = new Dictionary<string, StorageRingEntry>();
                        ringEntriesByItemId[slot] = new Dictionary<string, List<StorageRingEntry>>();
                    }

                    var list = ringEntries[slot];
                    var byId = ringEntryById[slot];
                    var byItemId = ringEntriesByItemId[slot];

                    if (slotData.entries != null)
                    {
                        foreach (var entryData in slotData.entries)
                        {
                            if (itemDatabase.TryGetValue(entryData.itemId, out var itemData))
                            {
                                var entry = new StorageRingEntry
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

                                list.Add(entry);
                                byId[entry.entryId] = entry;

                                if (!byItemId.ContainsKey(entry.itemId))
                                    byItemId[entry.itemId] = new List<StorageRingEntry>();
                                byItemId[entry.itemId].Add(entry);

                                ringCurrentVolume[slot] += entry.totalVolume;
                            }
                        }
                    }

                    // Обновляем nextEntryId на максимальное из слотов
                    if (slotData.nextEntryId > nextEntryId)
                        nextEntryId = slotData.nextEntryId;
                }
            }

            // Уведомляем об изменениях во всех слотах с записями
            foreach (var slot in RingSlots)
            {
                if (ringEntries.TryGetValue(slot, out var list) && list.Count > 0)
                {
                    OnContentsChanged?.Invoke(slot);
                }
            }
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        [ContextMenu("Dump All Rings")]
        private void DumpAllRings()
        {
            Debug.Log("[StorageRing] === Обзор всех колец ===");
            foreach (var slot in RingSlots)
            {
                var ring = GetActiveRing(slot);
                var entries = ringEntries.TryGetValue(slot, out var list) ? list : new List<StorageRingEntry>();
                float vol = GetCurrentVolume(slot);
                float maxVol = GetMaxVolume(slot);

                Debug.Log($"  {slot}: {(ring != null ? ring.nameRu : "—")}, " +
                          $"Записей: {entries.Count}, Объём: {vol:F1}/{maxVol:F1}");

                foreach (var entry in entries)
                {
                    Debug.Log($"    {entry.entryId}: {entry.ItemData?.nameRu} x{entry.count} " +
                              $"(Vol: {entry.totalVolume:F1}, W: {entry.totalWeight:F1})");
                }
            }
        }

        [ContextMenu("Activate Test Ring on RingLeft1")]
        private void ActivateTestRing()
        {
            // Создаём тестовое кольцо хранения через ScriptableObject
            var testRing = ScriptableObject.CreateInstance<StorageRingData>();
            testRing.nameRu = "Тестовое кольцо";
            testRing.nameEn = "Test Ring";
            testRing.itemId = "test_ring";
            testRing.maxVolume = 10f;
            testRing.qiCostBase = 5;
            testRing.qiCostPerUnit = 2f;
            testRing.accessTime = 1.5f;

            ActivateRing(EquipmentSlot.RingLeft1, testRing);
        }
#endif

        #endregion
    }

    // ============================================================================
    // StorageRingEntry — Запись в кольце хранения
    // ============================================================================
    // Аналог SpiritStorageEntry, но привязана к конкретному слоту кольца.
    // Стакающиеся предметы объединяются в одну запись (если совпадает durability/grade).
    // ============================================================================

    [Serializable]
    public class StorageRingEntry
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
    // StorageRingSaveData — Данные для сохранения
    // ============================================================================

    [Serializable]
    public class StorageRingSaveData
    {
        /// <summary>Данные по каждому слоту кольца</summary>
        public StorageRingSlotSaveData[] slots;

        /// <summary>Глобальный счётчик ID записей</summary>
        public int nextEntryId;
    }

    [Serializable]
    public class StorageRingSlotSaveData
    {
        /// <summary>Слот экипировки</summary>
        public EquipmentSlot slot;

        /// <summary>Записи в этом слоте</summary>
        public StorageRingEntrySaveData[] entries;

        /// <summary>Текущий объём заполнения</summary>
        public float currentVolume;

        /// <summary>Счётчик ID на момент сохранения</summary>
        public int nextEntryId;
    }

    [Serializable]
    public class StorageRingEntrySaveData
    {
        /// <summary>Уникальный ID записи</summary>
        public string entryId;

        /// <summary>ID предмета</summary>
        public string itemId;

        /// <summary>Количество</summary>
        public int count;

        /// <summary>Прочность</summary>
        public int durability;

        /// <summary>Грейд экипировки (как int)</summary>
        public int grade;
    }
}
