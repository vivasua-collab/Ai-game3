// ============================================================================
// ItemDatabase.cs — Глобальная база данных предметов (itemId → ItemData)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-05-05 16:37:16 MSK
// Чекпоинт: ИСП-БЛ-06 — Единый резолвер ItemId → ItemData
// ============================================================================
//
// Заменяет Resources.LoadAll<ItemData>("Items") на каждом подборe.
// Кэширует ВСЕ ItemData из Resources + FindObjectsOfTypeAll.
// Используется CombatLootHandler, ResourcePickup, QuestController и др.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Core
{
    /// <summary>
    /// Глобальная база данных предметов.
    /// Кэширует все ItemData (включая EquipmentData) для быстрого доступа по itemId.
    /// 
    /// Использование:
    ///   var itemData = ItemDatabase.GetById("sword_iron");
    ///   var equipData = ItemDatabase.GetByEquipmentId("sword_iron");
    /// </summary>
    public static class ItemDatabase
    {
        private static Dictionary<string, ItemData> cache;
        private static Dictionary<string, EquipmentData> equipmentCache;
        private static bool isBuilt = false;

        /// <summary>Количество предметов в кэше</summary>
        public static int Count => cache?.Count ?? 0;

        /// <summary>Кэш построен?</summary>
        public static bool IsBuilt => isBuilt;

        /// <summary>
        /// Получить ItemData по itemId. Автоматически строит кэш при первом вызове.
        /// </summary>
        public static ItemData GetById(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            if (!isBuilt) BuildCache();

            cache.TryGetValue(itemId, out var data);
            return data;
        }

        /// <summary>
        /// Получить EquipmentData по itemId. Быстрее чем GetById + cast.
        /// </summary>
        public static EquipmentData GetByEquipmentId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            if (!isBuilt) BuildCache();

            equipmentCache.TryGetValue(itemId, out var data);
            return data;
        }

        /// <summary>
        /// Зарегистрировать предмет вручную (для runtime ScriptableObjects).
        /// </summary>
        public static void Register(ItemData itemData)
        {
            if (itemData == null || string.IsNullOrEmpty(itemData.itemId)) return;

            if (cache == null)
            {
                cache = new Dictionary<string, ItemData>();
                equipmentCache = new Dictionary<string, EquipmentData>();
            }

            cache[itemData.itemId] = itemData;

            if (itemData is EquipmentData equipData)
                equipmentCache[equipData.itemId] = equipData;
        }

        /// <summary>
        /// Принудительно перестроить кэш (вызвать при загрузке новой сцены).
        /// </summary>
        public static void RebuildCache()
        {
            isBuilt = false;
            BuildCache();
        }

        /// <summary>
        /// Очистить кэш (при смене сцены или выходе).
        /// </summary>
        public static void Clear()
        {
            cache?.Clear();
            equipmentCache?.Clear();
            isBuilt = false;
        }

        /// <summary>
        /// Построить кэш из всех доступных ItemData.
        /// Источники: Resources.LoadAll + FindObjectsOfTypeAll.
        /// </summary>
        private static void BuildCache()
        {
            cache = new Dictionary<string, ItemData>();
            equipmentCache = new Dictionary<string, EquipmentData>();

            // 1. Из Resources/Items — загруженные .asset файлы
            var resourcesItems = Resources.LoadAll<ItemData>("Items");
            if (resourcesItems != null)
            {
                foreach (var item in resourcesItems)
                {
                    if (item != null && !string.IsNullOrEmpty(item.itemId))
                    {
                        cache[item.itemId] = item;
                        if (item is EquipmentData eq) equipmentCache[item.itemId] = eq;
                    }
                }
            }

            // 2. Из Resources/Equipment — экипировка
            var resourcesEquipment = Resources.LoadAll<EquipmentData>("Equipment");
            if (resourcesEquipment != null)
            {
                foreach (var item in resourcesEquipment)
                {
                    if (item != null && !string.IsNullOrEmpty(item.itemId))
                    {
                        cache[item.itemId] = item;
                        equipmentCache[item.itemId] = item;
                    }
                }
            }

            // 3. Runtime ScriptableObjects — FindObjectsOfTypeAll
            var allItems = Resources.FindObjectsOfTypeAll<ItemData>();
            if (allItems != null)
            {
                foreach (var item in allItems)
                {
                    if (item != null && !string.IsNullOrEmpty(item.itemId))
                    {
                        cache[item.itemId] = item;
                        if (item is EquipmentData eq) equipmentCache[item.itemId] = eq;
                    }
                }
            }

            isBuilt = true;
            Debug.Log($"[ItemDatabase] Кэш построен: {cache.Count} предметов, {equipmentCache.Count} экипировок");
        }
    }
}
