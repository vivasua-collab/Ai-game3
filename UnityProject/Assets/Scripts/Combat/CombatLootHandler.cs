// ============================================================================
// CombatLootHandler.cs — Обработчик боевого лута (подключает лут к инвентарю)
// Cultivation World Simulator
// ============================================================================
// Создано: 2026-05-05 16:37:16 MSK
// Чекпоинт: ИСП-БЛ-01 — Подключить боевой лут к инвентарю игрока
// ============================================================================
//
// Подписывается на CombatManager.OnLootGenerated.
// Конвертирует LootEntry.ItemId → ItemData через ItemDatabase.
// Добавляет предметы в InventoryController, Ци — в QiController.
//
// Вешается на Player GO как компонент.
// ============================================================================

using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;
using CultivationGame.Inventory;
using CultivationGame.Qi;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Обработчик боевого лута.
    /// Подключает LootResult от CombatManager к инвентарю игрока.
    /// 
    /// Подписывается на CombatManager.OnLootGenerated в Start().
    /// При получении лута:
    /// 1. Конвертирует LootEntry.ItemId → ItemData через ItemDatabase
    /// 2. Вызывает InventoryController.AddItem() для каждого предмета
    /// 3. Добавляет QiAbsorbed через QiController.AddQi()
    /// </summary>
    [RequireComponent(typeof(InventoryController))]
    public class CombatLootHandler : MonoBehaviour
    {
        private InventoryController inventoryController;
        private QiController qiController;

        private void Start()
        {
            inventoryController = GetComponent<InventoryController>();
            qiController = GetComponent<QiController>();

            // Подписываемся на событие генерации лута
            var combatManager = CombatManager.GetOrCreate();
            if (combatManager != null)
            {
                combatManager.OnLootGenerated += OnLootGenerated;
            }
            else
            {
                // CombatManager может появиться позже — подписываемся через задержку
                Debug.LogWarning("[CombatLootHandler] CombatManager не найден при Start — подписка при первом Update");
            }
        }

        private void Update()
        {
            // Fallback: подписываемся когда CombatManager появится
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnLootGenerated -= OnLootGenerated;
                CombatManager.Instance.OnLootGenerated += OnLootGenerated;
                enabled = false; // Отключаем Update — подписка выполнена
            }
        }

        private void OnDestroy()
        {
            // Отписываемся
            if (CombatManager.Instance != null)
            {
                CombatManager.Instance.OnLootGenerated -= OnLootGenerated;
            }
        }

        /// <summary>
        /// Обработчик события генерации лута.
        /// Вызывается из CombatManager.EndCombat() при смерти combatant.
        /// </summary>
        private void OnLootGenerated(ICombatant defeated, LootResult loot)
        {
            if (loot == null || !loot.HasLoot) return;

            Debug.Log($"[CombatLootHandler] Получен лут из {defeated?.Name ?? "Unknown"}: " +
                      $"{loot.TotalItemCount} предметов, Ци={loot.QiAbsorbed}");

            // 1. Добавляем Ци
            if (loot.QiAbsorbed > 0 && qiController != null)
            {
                qiController.AddQi(loot.QiAbsorbed);
                Debug.Log($"[CombatLootHandler] Поглощено Ци: +{loot.QiAbsorbed}");
            }

            // 2. Добавляем предметы в инвентарь
            if (inventoryController != null)
            {
                foreach (var entry in loot.Items)
                {
                    AddLootEntryToInventory(entry);
                }
            }
            else
            {
                Debug.LogWarning("[CombatLootHandler] InventoryController не найден — лут потерян!");
            }

            // 3. Отправляем GameEvents
            GameEvents.TriggerCombatEnd(true);
            if (defeated != null)
            {
                GameEvents.TriggerEnemyKilled(defeated.Name);
            }
        }

        /// <summary>
        /// Добавить одну запись лута в инвентарь.
        /// Пытается найти ItemData через ItemDatabase, при отсутствии создаёт временный.
        /// </summary>
        private void AddLootEntryToInventory(LootEntry entry)
        {
            if (string.IsNullOrEmpty(entry.ItemId) || entry.Amount <= 0) return;

            // Ищем ItemData через ItemDatabase
            ItemData itemData = ItemDatabase.GetById(entry.ItemId);

            if (itemData != null)
            {
                var slot = inventoryController.AddItem(itemData, entry.Amount);
                if (slot != null)
                {
                    Debug.Log($"[CombatLootHandler] Добавлено: {itemData.nameRu} x{entry.Amount}");
                }
                else
                {
                    Debug.LogWarning($"[CombatLootHandler] Инвентарь полон! Предмет потерян: {entry.ItemId} x{entry.Amount}");
                }
            }
            else
            {
                // ItemData не найден — создаём временный runtime SO
                itemData = CreateFallbackItemData(entry);
                if (itemData != null)
                {
                    ItemDatabase.Register(itemData);
                    var slot = inventoryController.AddItem(itemData, entry.Amount);
                    if (slot != null)
                    {
                        Debug.Log($"[CombatLootHandler] Добавлено (fallback): {itemData.nameRu} x{entry.Amount}");
                    }
                    else
                    {
                        Debug.LogWarning($"[CombatLootHandler] Инвентарь полон! Предмет потерян: {entry.ItemId} x{entry.Amount}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[CombatLootHandler] Не удалось создать ItemData для: {entry.ItemId}");
                }
            }
        }

        /// <summary>
        /// Создать временный ItemData для предмета, не найденного в базе.
        /// </summary>
        private ItemData CreateFallbackItemData(LootEntry entry)
        {
            var data = ScriptableObject.CreateInstance<ItemData>();
            data.itemId = entry.ItemId;
            data.nameRu = entry.ItemId; // Будет заменено при локализации
            data.nameEn = entry.ItemId;
            data.category = ItemCategory.Material;
            data.rarity = entry.Rarity;
            data.stackable = true;
            data.maxStack = 99;
            data.weight = 0.1f;
            data.volume = 0.5f;
            data.value = (int)entry.Rarity * 5 + 1;

            // Маппинг известных предметов
            switch (entry.ItemId)
            {
                case "qi_core":
                    data.nameRu = "Ци-ядро";
                    data.category = ItemCategory.Material;
                    data.rarity = ItemRarity.Uncommon;
                    break;
                case "fire_essence":
                    data.nameRu = "Огненная эссенция";
                    data.category = ItemCategory.Material;
                    break;
                case "water_essence":
                    data.nameRu = "Водная эссенция";
                    data.category = ItemCategory.Material;
                    break;
                case "earth_essence":
                    data.nameRu = "Земляная эссенция";
                    data.category = ItemCategory.Material;
                    break;
                case "wind_essence":
                    data.nameRu = "Ветряная эссенция";
                    data.category = ItemCategory.Material;
                    break;
                case "lightning_essence":
                    data.nameRu = "Грозовая эссенция";
                    data.category = ItemCategory.Material;
                    break;
                case "void_shard":
                    data.nameRu = "Осколок Пустоты";
                    data.category = ItemCategory.Material;
                    data.rarity = ItemRarity.Rare;
                    break;
                case "poison_gland":
                    data.nameRu = "Ядовитая железа";
                    data.category = ItemCategory.Material;
                    break;
                case "spirit_herb":
                    data.nameRu = "Духовная трава";
                    data.category = ItemCategory.Consumable;
                    break;
                case "cultivation_pill":
                    data.nameRu = "Пилюля культивации";
                    data.category = ItemCategory.Consumable;
                    data.rarity = ItemRarity.Uncommon;
                    break;
                case "qi_crystal":
                    data.nameRu = "Ци-кристалл";
                    data.category = ItemCategory.Material;
                    data.rarity = ItemRarity.Rare;
                    break;
                default:
                    // Оставляем ID как имя
                    break;
            }

            return data;
        }
    }
}
