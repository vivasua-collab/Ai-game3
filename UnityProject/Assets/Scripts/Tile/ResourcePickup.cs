// ============================================================================
// ResourcePickup.cs — Подбираемый ресурс
// Cultivation World Simulator
// Создано: 2026-04-08
// Версия: 1.0 — Unity 6.3 совместимость
// ============================================================================

using System;
using UnityEngine;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Подбираемый ресурс на карте.
    /// </summary>
    public class ResourcePickup : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private string resourceId;
        [SerializeField] private int amount = 1;
        
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
        /// Инициализироватьpickup.
        /// </summary>
        public void Initialize(string resourceId, int amount)
        {
            this.resourceId = resourceId;
            this.amount = amount;
            
            // Обновить имя объекта
            gameObject.name = $"Drop_{resourceId}_x{amount}";
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
        
        private bool TryAddToInventory(GameObject picker)
        {
            // TODO: Интеграция с системой инвентаря
            // var inventory = picker.GetComponent<InventoryController>();
            // if (inventory != null)
            // {
            //     return inventory.AddItem(resourceId, amount);
            // }
            
            // Временная реализация - всегда успешно
            Debug.Log($"[ResourcePickup] Would add to inventory: {resourceId} x{amount} (inventory integration pending)");
            return true;
        }
        
        // === Gizmos ===
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, pickupRadius);
        }
    }
}
