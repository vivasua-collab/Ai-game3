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
                // Fallback: ServiceLocator
                inventory = ServiceLocator.Get<InventoryController>();
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
                Debug.LogWarning($"[ResourcePickup] ItemData не найден для '{resourceId}'. Предмет подобран, но не добавлен в слоты инвентаря. Назначьте ItemData в Inspector или создайте ItemData для этого ресурса.");
                return true; // Предмет всё равно подбирается (для игрового процесса)
            }
            
            // Нет инвентаря вообще — всё равно подбираем
            Debug.Log($"[ResourcePickup] Нет InventoryController у {picker.name}. Предмет подобран: {resourceId} x{amount}");
            return true;
        }
        
        /// <summary>
        /// FIX TIL-H01: Поиск ItemData по resourceId через Resources. (2026-04-11)
        /// Загружает все ItemData из папки Resources/Items.
        /// </summary>
        private ItemData FindItemDataById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            
            // Попробовать загрузить из Resources по имени
            var items = Resources.LoadAll<ItemData>("Items");
            foreach (var item in items)
            {
                if (item != null && item.itemId == id)
                    return item;
            }
            
            return null;
        }
        
        // === Gizmos ===
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
