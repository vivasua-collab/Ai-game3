// ============================================================================
// DestructibleObjectController.cs — Контроллер разрушаемых объектов
// Cultivation World Simulator
// Создано: 2026-04-08
// Редактировано: 2026-04-15 11:30:00 UTC — FIX: RGBA32 для fallback спрайтов дропа (прозрачность)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CultivationGame.Core; // FIX TIL-H01: ServiceLocator (2026-04-11)

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Контроллер разрушаемых объектов на карте.
    /// Управляет нанесением урона, разрушением и дропом ресурсов.
    /// </summary>
    public class DestructibleObjectController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TileMapController tileMapController;
        [SerializeField] private Tilemap objectTilemap;
        
        [Header("Settings")]
        [SerializeField] private GameObject resourcePickupPrefab;
        [SerializeField] private bool spawnResourcePickups = true;
        [SerializeField] private LayerMask destructibleLayer;
        
        // === Runtime ===
        private Dictionary<string, TileObjectData> objectsByPosition = new();
        private Queue<DestructionInfo> pendingDestructions = new();
        private Queue<ResourceDrop> pendingDrops = new();
        
        // === Events ===
        /// <summary>Событие при нанесении урона объекту.</summary>
        public event Action<Vector2Int, TileObjectData, int, TileDamageType> OnObjectDamaged;
        
        /// <summary>Событие при разрушении объекта.</summary>
        public event Action<DestructionInfo> OnObjectDestroyed;
        
        /// <summary>Событие при появлении дропа ресурсов.</summary>
        public event Action<ResourceDrop> OnResourceDropped;

        /// <summary>Событие при начале добычи ресурса игроком (F-key).</summary>
        // Редактировано: 2026-04-15 08:20:00 UTC — обратная связь при добыче
        public event Action<Vector2Int, TileObjectData> OnHarvestStarted;
        
        // === Properties ===
        public int PendingDestructionCount => pendingDestructions.Count;
        public int PendingDropsCount => pendingDrops.Count;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            if (tileMapController == null)
                tileMapController = ServiceLocator.GetOrFind<TileMapController>(); // FIX UI-H03 (2026-04-11)
        }
        
        private void Start()
        {
            // Подписаться на генерацию карты
            if (tileMapController != null)
            {
                tileMapController.OnMapGenerated += OnMapGenerated;
                
                // Если карта уже есть, проинициализировать
                if (tileMapController.MapData != null)
                {
                    CacheObjects();
                }
            }
        }
        
        private void OnDestroy()
        {
            if (tileMapController != null)
            {
                tileMapController.OnMapGenerated -= OnMapGenerated;
            }
        }
        
        private void Update()
        {
            // Обработать отложенные разрушения
            ProcessPendingDestructions();
            
            // Обработать отложенный дроп
            ProcessPendingDrops();
        }
        
        // === Public API ===
        
        /// <summary>
        /// Нанести урон объекту на тайле.
        /// </summary>
        /// <param name="tileX">Координата X тайла.</param>
        /// <param name="tileY">Координата Y тайла.</param>
        /// <param name="damage">Количество урона.</param>
        /// <param name="damageType">Тип урона.</param>
        /// <returns>Фактически нанесённый урон. -1 если объект не найден.</returns>
        public int DamageObjectAtTile(int tileX, int tileY, int damage, TileDamageType damageType = TileDamageType.Physical)
        {
            var tile = tileMapController?.GetTile(tileX, tileY);
            if (tile == null || tile.objects.Count == 0)
                return -1;
            
            // Получить первый объект (основной)
            var obj = tile.objects[0];
            
            // Проверить, можно ли нанести урон
            if (obj.currentDurability <= 0)
                return -1;
            
            // Нанести урон
            int actualDamage = obj.ApplyDamage(damage, damageType);
            
            // Уведомить о повреждении
            OnObjectDamaged?.Invoke(new Vector2Int(tileX, tileY), obj, actualDamage, damageType);

            // Уведомить о начале добычи (если объект добываемый)
            // Редактировано: 2026-04-15 08:20:00 UTC — обратная связь при добыче
            if (obj.isHarvestable && actualDamage > 0)
            {
                OnHarvestStarted?.Invoke(new Vector2Int(tileX, tileY), obj);
            }
            
            // Проверить на разрушение
            if (obj.IsDestroyed())
            {
                ScheduleDestruction(tile, tileX, tileY, obj, damageType, actualDamage);
            }
            
            return actualDamage;
        }
        
        /// <summary>
        /// Нанести урон объекту по мировым координатам.
        /// </summary>
        public int DamageObjectAtWorld(Vector2 worldPos, int damage, TileDamageType damageType = TileDamageType.Physical)
        {
            if (tileMapController?.MapData == null)
                return -1;
            
            var tilePos = tileMapController.MapData.WorldToTile(worldPos);
            return DamageObjectAtTile(tilePos.x, tilePos.y, damage, damageType);
        }
        
        /// <summary>
        /// Получить объект на тайле.
        /// </summary>
        public TileObjectData GetObjectAtTile(int tileX, int tileY)
        {
            var tile = tileMapController?.GetTile(tileX, tileY);
            return tile?.objects.Count > 0 ? tile.objects[0] : null;
        }
        
        /// <summary>
        /// Получить прочность объекта на тайле.
        /// </summary>
        public (int current, int max) GetObjectDurability(int tileX, int tileY)
        {
            var obj = GetObjectAtTile(tileX, tileY);
            if (obj == null)
                return (0, 0);
            return (obj.currentDurability, obj.maxDurability);
        }
        
        /// <summary>
        /// Проверить, можно ли разрушить объект на тайле.
        /// </summary>
        public bool IsDestructible(int tileX, int tileY)
        {
            var obj = GetObjectAtTile(tileX, tileY);
            return obj != null && obj.maxDurability > 0;
        }
        
        // === Private Methods ===
        
        private void OnMapGenerated(TileMapData mapData)
        {
            CacheObjects();
        }
        
        private void CacheObjects()
        {
            objectsByPosition.Clear();
            
            var mapData = tileMapController?.MapData;
            if (mapData == null) return;
            
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile?.objects.Count > 0)
                    {
                        string key = $"{x}_{y}";
                        objectsByPosition[key] = tile.objects[0];
                    }
                }
            }
            
            Debug.Log($"[DestructibleObjectController] Cached {objectsByPosition.Count} destructible objects");
        }
        
        private void ScheduleDestruction(TileData tile, int tileX, int tileY, TileObjectData obj, TileDamageType damageType, int finalDamage)
        {
            Vector2 worldPos = tile.GetWorldPosition();
            
            var info = new DestructionInfo(obj, new Vector2Int(tileX, tileY), worldPos, damageType, finalDamage);
            
            // Получить дроп ресурсов
            if (obj.isHarvestable)
            {
                info.ResourceDrops = obj.GetResourceDrops(worldPos);
            }
            
            pendingDestructions.Enqueue(info);
        }
        
        private void ProcessPendingDestructions()
        {
            while (pendingDestructions.Count > 0)
            {
                var info = pendingDestructions.Dequeue();
                ProcessDestruction(info);
            }
        }
        
        private void ProcessDestruction(DestructionInfo info)
        {
            // Удалить объект из данных карты
            var tile = tileMapController?.GetTile(info.TilePosition.x, info.TilePosition.y);
            if (tile != null && tile.objects.Count > 0)
            {
                // Найти и удалить объект
                var obj = tile.objects.Find(o => o.objectId == info.ObjectId);
                if (obj != null)
                {
                    tile.RemoveObject(obj);
                    
                    // Обновить отображение
                    if (objectTilemap != null)
                    {
                        objectTilemap.SetTile(new Vector3Int(info.TilePosition.x, info.TilePosition.y, 0), null);
                    }
                    
                    // Удалить из кэша
                    string key = $"{info.TilePosition.x}_{info.TilePosition.y}";
                    objectsByPosition.Remove(key);
                }
            }
            
            // Добавить дроп ресурсов в очередь
            foreach (var drop in info.ResourceDrops)
            {
                pendingDrops.Enqueue(drop);
            }
            
            // Уведомить о разрушении
            OnObjectDestroyed?.Invoke(info);
            
            Debug.Log($"[DestructibleObjectController] Object destroyed: {info.ObjectType} at ({info.TilePosition.x}, {info.TilePosition.y})");
        }
        
        private void ProcessPendingDrops()
        {
            while (pendingDrops.Count > 0)
            {
                var drop = pendingDrops.Dequeue();
                ProcessResourceDrop(drop);
            }
        }
        
        private void ProcessResourceDrop(ResourceDrop drop)
        {
            if (!spawnResourcePickups)
            {
                Debug.Log($"[DestructibleObjectController] Resource dropped: {drop.ResourceId} x{drop.Amount}");
                return;
            }
            
            // Создать pickup объект
            if (resourcePickupPrefab != null)
            {
                var pickup = Instantiate(resourcePickupPrefab, drop.DropPosition, Quaternion.identity);
                // FIX TIL-H01: Настроить pickup через Initialize() (2026-04-11)
                var pickupComponent = pickup.GetComponent<ResourcePickup>();
                if (pickupComponent != null)
                {
                    pickupComponent.Initialize(drop.ResourceId, drop.Amount);
                }
            }
            else
            {
                // Создать временный визуал для дропа
                CreateTemporaryDropVisual(drop);
            }
            
            OnResourceDropped?.Invoke(drop);
        }
        
        private void CreateTemporaryDropVisual(ResourceDrop drop)
        {
            // Создать временный игровой объект для визуализации дропа
            GameObject dropGO = new GameObject($"Drop_{drop.ResourceId}");
            dropGO.transform.position = drop.DropPosition;
            
            // Добавить спрайт
            SpriteRenderer sr = dropGO.AddComponent<SpriteRenderer>();
            sr.sprite = CreateDropSprite(drop.ResourceId);
            sr.sortingOrder = 5;
            sr.sortingLayerName = "Objects";
            sr.color = GetResourceColor(drop.ResourceId);
            
            // КРИТ-1 FIX: Unlit шейдер — рендерит БЕЗ Light2D (как в PlayerVisual, ResourceSpawner).
            // Без этого дроп-объекты будут невидимыми в URP 2D без Light2D.
            // Редактировано: 2026-04-16
            Shader dropShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (dropShader == null) dropShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (dropShader == null) dropShader = Shader.Find("Sprites/Default");
            if (dropShader != null) sr.material = new Material(dropShader);
            
            // Добавить коллайдер для подбора
            CircleCollider2D col = dropGO.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;
            
            // Добавить простой скрипт подбора
            var pickup = dropGO.AddComponent<ResourcePickup>();
            pickup.Initialize(drop.ResourceId, drop.Amount);
            
            Debug.Log($"[DestructibleObjectController] Created drop visual: {drop.ResourceId} x{drop.Amount} at {drop.DropPosition}");
        }
        
        // NOTE TIL-L02: Texture2D created here is not explicitly tracked/destroyed.
        // It's used in Sprite.Create attached to a GameObject that gets destroyed when picked up.
        // Это допустимо для краткосрочных визуалов дропа, но может течь при накоплении. (2026-04-11)
        private Sprite CreateDropSprite(string resourceId)
        {
            // FIX: RGBA32 для правильной прозрачности fallback спрайтов
            // Редактировано: 2026-04-15 11:30:00 UTC
            Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            Color[] colors = new Color[32 * 32];
            Color color = GetResourceColor(resourceId);
            
            // Заполнить прозрачным фоном
            for (int i = 0; i < colors.Length; i++) colors[i] = Color.clear;
            
            // Создать форму (круг — лучше видно чем квадрат)
            for (int x = 0; x < 32; x++)
            {
                for (int y = 0; y < 32; y++)
                {
                    float dx = x - 16;
                    float dy = y - 16;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist < 10f)
                    {
                        // Яркое ядро
                        colors[y * 32 + x] = new Color(
                            Mathf.Min(1f, color.r * 1.3f),
                            Mathf.Min(1f, color.g * 1.3f),
                            Mathf.Min(1f, color.b * 1.3f), 1f);
                    }
                    else if (dist < 13f)
                    {
                        colors[y * 32 + x] = color;
                    }
                    else if (dist < 14f)
                    {
                        // Контур
                        colors[y * 32 + x] = new Color(color.r * 0.6f, color.g * 0.6f, color.b * 0.6f, 1f);
                    }
                }
            }
            
            texture.SetPixels(colors);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        }
        
        private Color GetResourceColor(string resourceId)
        {
            return resourceId switch
            {
                "wood" => new Color(0.6f, 0.4f, 0.2f),
                "stone" => new Color(0.5f, 0.5f, 0.55f),
                "ore" => new Color(0.7f, 0.5f, 0.3f),
                "herb" => new Color(0.3f, 0.7f, 0.3f),
                "berries" => new Color(0.8f, 0.2f, 0.3f),
                _ => new Color(0.8f, 0.8f, 0.2f)
            };
        }
        
        // === Context Menu ===
        
        [ContextMenu("Debug: List All Destructible Objects")]
        private void DebugListDestructibles()
        {
            foreach (var kvp in objectsByPosition)
            {
                Debug.Log($"Object at {kvp.Key}: {kvp.Value.objectType}, Durability: {kvp.Value.currentDurability}/{kvp.Value.maxDurability}");
            }
        }
    }
}
