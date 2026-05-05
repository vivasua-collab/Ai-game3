// ============================================================================
// ResourcePickup.cs — Подбираемый ресурс
// Cultivation World Simulator
// Создано: 2026-04-08
// Редактировано: 2026-04-11 08:27:05 UTC — FIX TIL-H01: Реальная интеграция с InventoryController.AddItem()
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core; // ServiceLocator
using CultivationGame.Inventory; // InventoryController
using CultivationGame.Data.ScriptableObjects; // ItemData

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Подбираемый ресурс на карте.
    /// FIX TIL-H01: Теперь реально добавляет предметы в InventoryController.
    /// </summary>
    public class ResourcePickup : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string resourceId;
        [SerializeField] private int amount = 1;
        [SerializeField] private ItemData itemData; // FIX TIL-H01: Ссылка на ItemData для AddItem (2026-04-11)
        
        [Header("Settings")]
        [SerializeField] private float pickupRadius = 0.5f;
        [SerializeField] private bool autoPickup = false;
        [SerializeField] private float lifetime = 300f; // 5 минут
        
        // === Runtime ===
        private float spawnTime;
        private bool isPickedUp = false;
        
        // === Events ===
        public event Action<string, int> OnPickedUp;
        
        // === Properties ===
        public string ResourceId => resourceId;
        public int Amount => amount;
        public float PickupRadius { get => pickupRadius; set => pickupRadius = value; }
        public bool AutoPickup { get => autoPickup; set => autoPickup = value; }
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            spawnTime = Time.time;
        }
        
        private void Update()
        {
            // Проверить время жизни
            if (Time.time - spawnTime > lifetime)
            {
                Destroy(gameObject);
            }
            
            // Автоподбор (если включён)
            if (autoPickup)
            {
                CheckAutoPickup();
            }
        }
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isPickedUp) return;
            
            // Проверить, что это игрок
            if (other.CompareTag("Player"))
            {
                Pickup(other.gameObject);
            }
        }
        
        // === Public API ===
        
        /// <summary>
        /// Инициализировать pickup.
        /// </summary>
        public void Initialize(string resourceId, int amount)
        {
            this.resourceId = resourceId;
            this.amount = amount;
            
            // Обновить имя объекта
            gameObject.name = $"Drop_{resourceId}_x{amount}";
        }
        
        /// <summary>
        /// Инициализировать pickup с ItemData.
        /// FIX TIL-H01: Перегрузка с ItemData для реального добавления в инвентарь. (2026-04-11)
        /// </summary>
        public void Initialize(string resourceId, int amount, ItemData data)
        {
            Initialize(resourceId, amount);
            this.itemData = data;
        }
        
        /// <summary>
        /// Подобрать ресурс.
        /// </summary>
        public bool Pickup(GameObject picker)
        {
            if (isPickedUp) return false;
            
            isPickedUp = true;
            
            // Попробовать добавить в инвентарь
            bool success = TryAddToInventory(picker);
            
            if (success)
            {
                OnPickedUp?.Invoke(resourceId, amount);
                Debug.Log($"[ResourcePickup] Picked up: {resourceId} x{amount}");
                Destroy(gameObject);
                return true;
            }
            
            isPickedUp = false;
            return false;
        }
        
        // === Private Methods ===
        
        private void CheckAutoPickup()
        {
            var collider = Physics2D.OverlapCircle(transform.position, pickupRadius, LayerMask.GetMask("Player"));
            if (collider != null)
            {
                Pickup(collider.gameObject);
            }
        }
        
        /// <summary>
        /// FIX TIL-H01: Реальное добавление предмета в InventoryController. (2026-04-11)
        /// Приоритет: ItemData из поля → поиск через ServiceLocator → fallback (лог + подбор).
        /// </summary>
        private bool TryAddToInventory(GameObject picker)
        {
            var inventory = picker.GetComponent<InventoryController>();
            if (inventory == null)
            {
                // Fallback: ServiceLocator — FIX ИСП-ИНВ-03: GetOrFind вместо Get
                inventory = ServiceLocator.GetOrFind<InventoryController>();
            }
            
            if (inventory != null && itemData != null)
            {
                // Основной путь: есть ItemData — вызываем AddItem
                var slot = inventory.AddItem(itemData, amount);
                if (slot != null)
                {
                    Debug.Log($"[ResourcePickup] Добавлено в инвентарь: {resourceId} x{amount} (слот {slot.SlotId})");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"[ResourcePickup] Инвентарь полон, нельзя добавить: {resourceId} x{amount}");
                    return false;
                }
            }
            
            if (inventory != null && itemData == null)
            {
                // ItemData не назначен — попробовать найти по resourceId через все ItemData в ресурсах
                var foundItem = FindItemDataById(resourceId);
                if (foundItem != null)
                {
                    itemData = foundItem;
                    var slot = inventory.AddItem(itemData, amount);
                    if (slot != null)
                    {
                        Debug.Log($"[ResourcePickup] Добавлено в инвентарь (найден ItemData): {resourceId} x{amount}");
                        return true;
                    }
                }
                
                // ItemData не найден — ресурсы типа "wood"/"stone" без ScriptableObject
                // FIX: Создаём временный ItemData runtime, чтобы ресурсы подбирались.
                // Ранее возвращали false — предметы накапливались и не уничтожались.
                // Редактировано: 2026-04-15 17:14:21 UTC
                itemData = CreateTemporaryItemData(resourceId);
                if (itemData != null)
                {
                    var slot = inventory.AddItem(itemData, amount);
                    if (slot != null)
                    {
                        Debug.Log($"[ResourcePickup] Добавлено в инвентарь (временный ItemData): {resourceId} x{amount}");
                        return true;
                    }
                }
                Debug.LogWarning($"[ResourcePickup] ItemData не найден для '{resourceId}'. Предмет НЕ подобран — назначьте ItemData в Inspector или создайте ItemData для этого ресурса.");
                return false;
            }
            
            // Нет инвентаря вообще — FIX ИСП-ИНВ-04: НЕ подбираем, предмет остаётся в мире
            Debug.Log($"[ResourcePickup] Нет InventoryController у {picker.name}. Предмет ОСТАЛСЯ в мире: {resourceId} x{amount}");
            return false;
        }
        
        /// <summary>
        /// FIX TIL-H01 / ИСП-ИНВ-11: Поиск ItemData по resourceId через ItemDatabase (кэш).
        /// Заменяет Resources.LoadAll на каждый вызов.
        /// Редактировано: 2026-05-05
        /// </summary>
        private ItemData FindItemDataById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            
            // ИСП-ИНВ-11: Используем кэшированный ItemDatabase вместо Resources.LoadAll
            return ItemDatabase.GetById(id);
        }
        
        /// <summary>
        /// Создать временный ItemData runtime для ресурса без ScriptableObject.
        /// Это позволяет подбирать ресурсы даже если ItemData .asset не создан.
        /// Редактировано: 2026-04-15 17:14:21 UTC
        /// </summary>
        private ItemData CreateTemporaryItemData(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            
            var data = ScriptableObject.CreateInstance<ItemData>();
            data.itemId = id;
            
            // Маппинг resourceId → отображаемое имя и категория
            switch (id)
            {
                case "herb": data.nameRu = "Целебная трава"; data.category = ItemCategory.Consumable; data.rarity = ItemRarity.Common; break;
                case "berries": data.nameRu = "Ягоды"; data.category = ItemCategory.Consumable; data.rarity = ItemRarity.Common; break;
                case "mushroom": data.nameRu = "Гриб Ци"; data.category = ItemCategory.Consumable; data.rarity = ItemRarity.Uncommon; break;
                case "rare_herb": data.nameRu = "Редкая трава"; data.category = ItemCategory.Consumable; data.rarity = ItemRarity.Rare; break;
                case "stone": data.nameRu = "Камень"; data.category = ItemCategory.Material; data.rarity = ItemRarity.Common; break;
                case "ore": data.nameRu = "Руда"; data.category = ItemCategory.Material; data.rarity = ItemRarity.Uncommon; break;
                case "iron_ore": data.nameRu = "Железная руда"; data.category = ItemCategory.Material; data.rarity = ItemRarity.Uncommon; break;
                case "qi_crystal": data.nameRu = "Ци-кристалл"; data.category = ItemCategory.Material; data.rarity = ItemRarity.Rare; break;
                case "spirit_stone": data.nameRu = "Духовный камень"; data.category = ItemCategory.Material; data.rarity = ItemRarity.Epic; break;
                case "wood": data.nameRu = "Древесина"; data.category = ItemCategory.Material; data.rarity = ItemRarity.Common; break;
                case "sand_pearl": data.nameRu = "Песчаная жемчужина"; data.category = ItemCategory.Material; data.rarity = ItemRarity.Rare; break;
                case "desert_crystal": data.nameRu = "Пустынный кристалл"; data.category = ItemCategory.Material; data.rarity = ItemRarity.Rare; break;
                default: data.nameRu = id; data.category = ItemCategory.Misc; data.rarity = ItemRarity.Common; break;
            }
            
            data.nameEn = id;
            data.stackable = true;
            data.maxStack = 99;
            data.weight = 0.1f;
            data.value = 1;
            
            return data;
        }
        
        // === Gizmos ===
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
