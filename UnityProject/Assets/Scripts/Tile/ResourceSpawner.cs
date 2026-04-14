// ============================================================================
// ResourceSpawner.cs — Спавнер ресурсных объектов на локации
// Cultivation World Simulator
// Создано: 2026-04-14 07:35:00 UTC
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Описание типа ресурса для спавна.
    /// </summary>
    [System.Serializable]
    public class ResourceSpawnEntry
    {
        [Tooltip("Идентификатор ресурса")]
        public string resourceId;
        [Tooltip("Отображаемое имя")]
        public string displayName;
        [Tooltip("Цвет спрайта ресурса")]
        public Color spriteColor = Color.yellow;
        [Tooltip("Количество при подборе")]
        public int amount = 1;
        [Tooltip("Минимальное количество на карте")]
        public int minCount = 3;
        [Tooltip("Максимальное количество на карте")]
        public int maxCount = 8;
        [Tooltip("Типы поверхности, на которых спавнится")]
        public TerrainType[] spawnTerrain = { TerrainType.Grass, TerrainType.Dirt };
        [Tooltip("Радиус подбора")]
        public float pickupRadius = 0.8f;
        [Tooltip("Автоподбор при приближении")]
        public bool autoPickup = true;
    }

    /// <summary>
    /// Спавнер ресурсных объектов.
    /// Размещает ResourcePickup на карте при старте и периодически респавнит.
    /// </summary>
    public class ResourceSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TileMapController tileMapController;

        [Header("Resource Types")]
        [SerializeField] private List<ResourceSpawnEntry> resourceTypes = new List<ResourceSpawnEntry>();

        [Header("Spawn Settings")]
        [SerializeField] private bool spawnOnStart = true;
        [SerializeField] private float respawnInterval = 60f; // секунды между респавнами
        [SerializeField] private int maxResourcesTotal = 100;
        [SerializeField] private int spawnMargin = 2; // отступ от краёв карты

        [Header("Visuals")]
        [SerializeField] private int spriteSize = 24;
        [SerializeField] private float spriteScale = 0.6f;

        // === Runtime ===
        private List<GameObject> spawnedResources = new List<GameObject>();
        private float respawnTimer;
        private System.Random rng;
        private Transform resourcesParent;

        // === Unity Lifecycle ===

        private void Awake()
        {
            if (tileMapController == null)
                tileMapController = ServiceLocator.GetOrFind<TileMapController>();

            resourcesParent = new GameObject("Resources").transform;
            resourcesParent.SetParent(transform);
        }

        private void Start()
        {
            if (tileMapController != null)
            {
                tileMapController.OnMapGenerated += OnMapGenerated;

                // Если карта уже есть
                if (tileMapController.MapData != null && spawnOnStart)
                {
                    SpawnAllResources();
                }
            }
        }

        private void OnDestroy()
        {
            if (tileMapController != null)
                tileMapController.OnMapGenerated -= OnMapGenerated;
        }

        private void Update()
        {
            // Респавн ресурсов
            respawnTimer += Time.deltaTime;
            if (respawnTimer >= respawnInterval)
            {
                respawnTimer = 0f;
                RespawnMissingResources();
            }

            // Удалить подобранные ресурсы из списка
            CleanupPickedUp();
        }

        // === Public API ===

        /// <summary>
        /// Заспавнить все ресурсы согласно конфигурации.
        /// </summary>
        public void SpawnAllResources()
        {
            if (tileMapController?.MapData == null) return;

            rng = new System.Random(tileMapController.MapData.seed ^ 0xBEEF);

            foreach (var entry in resourceTypes)
            {
                int count = rng.Next(entry.minCount, entry.maxCount + 1);
                SpawnResourceType(entry, count);
            }

            Debug.Log($"[ResourceSpawner] Заспавнено {spawnedResources.Count} ресурсов");
        }

        /// <summary>
        /// Респавнить недостающие ресурсы.
        /// </summary>
        public void RespawnMissingResources()
        {
            if (tileMapController?.MapData == null) return;

            int totalActive = CountActiveByType();
            if (totalActive >= maxResourcesTotal) return;

            foreach (var entry in resourceTypes)
            {
                int current = CountActiveByType(entry.resourceId);
                int needed = entry.minCount - current;
                if (needed > 0)
                {
                    SpawnResourceType(entry, needed);
                }
            }
        }

        /// <summary>
        /// Очистить все ресурсы с карты.
        /// </summary>
        public void ClearAllResources()
        {
            foreach (var go in spawnedResources)
            {
                if (go != null) Destroy(go);
            }
            spawnedResources.Clear();
        }

        // === Private ===

        private void OnMapGenerated(TileMapData mapData)
        {
            ClearAllResources();
            if (spawnOnStart)
            {
                SpawnAllResources();
            }
        }

        private void SpawnResourceType(ResourceSpawnEntry entry, int count)
        {
            var mapData = tileMapController.MapData;
            if (mapData == null) return;

            if (rng == null) rng = new System.Random();

            int spawned = 0;
            int attempts = 0;
            int maxAttempts = count * 20; // ограничение попыток

            while (spawned < count && attempts < maxAttempts)
            {
                attempts++;

                int x = rng.Next(spawnMargin, mapData.width - spawnMargin);
                int y = rng.Next(spawnMargin, mapData.height - spawnMargin);

                var tile = mapData.GetTile(x, y);
                if (tile == null) continue;

                // Проверить тип поверхности
                if (!IsAllowedTerrain(tile.terrain, entry.spawnTerrain)) continue;

                // Проверить проходимость
                if (!tile.IsPassable()) continue;

                // Не спавнить на тайле с объектами
                if (tile.objects.Count > 0) continue;

                // Не спавнить слишком близко к другим ресурсам
                Vector2 worldPos = tile.GetWorldPosition();
                if (IsTooCloseToOther(worldPos, 3f)) continue;

                // Спавним!
                SpawnSingleResource(entry, worldPos);
                spawned++;
            }

            if (spawned > 0)
                Debug.Log($"[ResourceSpawner] {entry.displayName}: заспавнено {spawned}/{count}");
        }

        private GameObject SpawnSingleResource(ResourceSpawnEntry entry, Vector2 worldPos)
        {
            GameObject go = new GameObject($"Res_{entry.resourceId}");
            go.transform.SetParent(resourcesParent);
            go.transform.position = worldPos;
            go.tag = "Resource";

            // Спрайт
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateResourceSprite(entry);
            sr.sortingOrder = 5;
            sr.color = entry.spriteColor;

            // Масштаб
            go.transform.localScale = Vector3.one * spriteScale;

            // Коллайдер для подбора
            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = entry.pickupRadius;

            // ResourcePickup компонент
            var pickup = go.AddComponent<ResourcePickup>();
            pickup.Initialize(entry.resourceId, entry.amount);
            // Установить автоподбор и радиус
            pickup.AutoPickup = entry.autoPickup;
            pickup.PickupRadius = entry.pickupRadius;

            spawnedResources.Add(go);
            return go;
        }

        private Sprite CreateResourceSprite(ResourceSpawnEntry entry)
        {
            int size = spriteSize;
            Texture2D texture = new Texture2D(size, size);
            Color[] pixels = new Color[size * size];

            Color main = entry.spriteColor;
            Color dark = new Color(main.r * 0.6f, main.g * 0.6f, main.b * 0.6f);
            Color light = new Color(
                Mathf.Min(1f, main.r * 1.3f),
                Mathf.Min(1f, main.g * 1.3f),
                Mathf.Min(1f, main.b * 1.3f)
            );

            float center = size * 0.5f;
            float radius = size * 0.4f;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist < radius * 0.5f)
                    {
                        // Яркое ядро
                        pixels[y * size + x] = light;
                    }
                    else if (dist < radius)
                    {
                        // Основной цвет
                        pixels[y * size + x] = main;
                    }
                    else if (dist < radius + 1.5f)
                    {
                        // Контур
                        pixels[y * size + x] = dark;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32);
        }

        private bool IsAllowedTerrain(TerrainType terrain, TerrainType[] allowed)
        {
            if (allowed == null || allowed.Length == 0) return true;
            foreach (var t in allowed)
            {
                if (t == terrain) return true;
            }
            return false;
        }

        private bool IsTooCloseToOther(Vector2 pos, float minDist)
        {
            float minDistSq = minDist * minDist;
            foreach (var go in spawnedResources)
            {
                if (go == null) continue;
                float dx = go.transform.position.x - pos.x;
                float dy = go.transform.position.y - pos.y;
                if (dx * dx + dy * dy < minDistSq) return true;
            }
            return false;
        }

        private int CountActiveByType(string resourceId)
        {
            int count = 0;
            foreach (var go in spawnedResources)
            {
                if (go == null) continue;
                var pickup = go.GetComponent<ResourcePickup>();
                if (pickup != null && pickup.ResourceId == resourceId)
                    count++;
            }
            return count;
        }

        private int CountActiveByType()
        {
            int count = 0;
            foreach (var go in spawnedResources)
            {
                if (go != null) count++;
            }
            return count;
        }

        private void CleanupPickedUp()
        {
            spawnedResources.RemoveAll(go => go == null);
        }

        // === Default Config ===

        /// <summary>
        /// Установить стандартную конфигурацию ресурсов для тестовой локации.
        /// </summary>
        public void SetDefaultResourceTypes()
        {
            resourceTypes.Clear();

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "herb",
                displayName = "Целебная трава",
                spriteColor = new Color(0.3f, 0.8f, 0.3f),
                amount = 1,
                minCount = 8,
                maxCount = 15,
                spawnTerrain = new[] { TerrainType.Grass },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "berries",
                displayName = "Ягоды",
                spriteColor = new Color(0.8f, 0.2f, 0.3f),
                amount = 3,
                minCount = 5,
                maxCount = 10,
                spawnTerrain = new[] { TerrainType.Grass },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "stone",
                displayName = "Камень",
                spriteColor = new Color(0.6f, 0.6f, 0.65f),
                amount = 2,
                minCount = 5,
                maxCount = 10,
                spawnTerrain = new[] { TerrainType.Stone, TerrainType.Dirt },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "ore",
                displayName = "Руда",
                spriteColor = new Color(0.8f, 0.5f, 0.2f),
                amount = 1,
                minCount = 3,
                maxCount = 6,
                spawnTerrain = new[] { TerrainType.Stone },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "wood",
                displayName = "Древесина",
                spriteColor = new Color(0.6f, 0.4f, 0.2f),
                amount = 3,
                minCount = 5,
                maxCount = 10,
                spawnTerrain = new[] { TerrainType.Grass, TerrainType.Dirt },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "qi_crystal",
                displayName = "Ци-кристалл",
                spriteColor = new Color(0.4f, 0.7f, 1f),
                amount = 1,
                minCount = 2,
                maxCount = 5,
                spawnTerrain = new[] { TerrainType.Stone },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "sand_pearl",
                displayName = "Песчаная жемчужина",
                spriteColor = new Color(0.9f, 0.85f, 0.5f),
                amount = 1,
                minCount = 2,
                maxCount = 4,
                spawnTerrain = new[] { TerrainType.Sand },
                autoPickup = true
            });
        }
    }
}
