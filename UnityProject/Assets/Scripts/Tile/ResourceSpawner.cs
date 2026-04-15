// ============================================================================
// ResourceSpawner.cs — Спавнер ресурсных объектов на локации
// Cultivation World Simulator
// Создано: 2026-04-14 07:35:00 UTC
// Редактировано: 2026-04-15 17:14:21 UTC — FIX 3A/3B: убран Tiles_AI/, spriteScale=1.0, Unlit шейдер
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
        [SerializeField] private int maxResourcesTotal = 250; // Редактировано: 2026-04-14 — увеличено для карты 100×80
        [SerializeField] private int spawnMargin = 3; // отступ от краёв карты (увеличен)

        [Header("Visuals")]
        [SerializeField] private int spriteSize = 64; // Размер текстуры (совпадает с объектами тайлов)
        [SerializeField] private float spriteScale = 1.0f; // FIX: 1.0 — ресурсы нормально видны (было 0.16 — микроскопические)
        // При PPU=160: 64/160=0.4 юнита × scale=1.0 = 0.4 юнита (разумный размер)
        // Редактировано: 2026-04-15 17:14:21 UTC

        // === Runtime ===
        private List<GameObject> spawnedResources = new List<GameObject>();
        private float respawnTimer;
        private System.Random rng;
        private Transform resourcesParent;
        // FIX: Флаг для отслеживания загрузки AI-спрайтов (не тонировать их)
        // Редактировано: 2026-04-15 UTC
        private bool _lastSpriteWasAI;

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
            // FIX: Тег "Resource" теперь добавлен в TagManager через FullSceneBuilder Phase 02
            // Редактировано: 2026-04-15 12:00:00 UTC
            go.tag = "Resource";

            // Спрайт
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateResourceSprite(entry);
            sr.sortingOrder = 5;
            // FIX: AI-спрайты не тонируем (используем оригинальные цвета),
            // программные fallback-спрайты — тонируем entry.spriteColor
            // Редактировано: 2026-04-15 UTC
            sr.color = _lastSpriteWasAI ? Color.white : entry.spriteColor;
            
            // FIX: Используем Unlit шейдер — рендерит БЕЗ Light2D (гарантированно виден).
            // Sprite-Lit-Default без Light2D = чёрный (невидимый).
            // Редактировано: 2026-04-15 17:14:21 UTC
            Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (spriteShader == null)
                spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (spriteShader == null)
                spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader != null)
                sr.material = new Material(spriteShader);

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
            // FIX: Пытаемся загрузить AI-спрайт из Assets/Sprites/Tiles_AI/ или Assets/Sprites/Tiles/
            // Если не найден — fallback на программный круг
            // Редактировано: 2026-04-15 UTC
            Sprite loadedSprite = LoadResourceSprite(entry.resourceId);
            if (loadedSprite != null)
            {
                _lastSpriteWasAI = true;
                return loadedSprite;
            }
            
            // Fallback: программный спрайт
            _lastSpriteWasAI = false;
            int size = spriteSize;
            // FIX: RGBA32 формат для правильной прозрачности
            // Редактировано: 2026-04-15 11:25:00 UTC
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            Color[] pixels = new Color[size * size];

            // Заполнить прозрачным
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            Color main = entry.spriteColor;
            Color dark = new Color(main.r * 0.6f, main.g * 0.6f, main.b * 0.6f);
            Color light = new Color(
                Mathf.Min(1f, main.r * 1.3f),
                Mathf.Min(1f, main.g * 1.3f),
                Mathf.Min(1f, main.b * 1.3f)
            );

            float center = size * 0.5f;
            float radius = size * 0.35f;

            // FIX: Рисуем кристалл/гем вместо простого круга — более узнаваемый
            // Редактировано: 2026-04-15 12:00:00 UTC
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dx = x - center;
                    float dy = y - center;

                    // Ромбовидная форма кристалла
                    float diamondDist = Mathf.Abs(dx) / radius + Mathf.Abs(dy) / (radius * 1.2f);

                    if (diamondDist < 0.4f)
                    {
                        // Яркое ядро — блик
                        pixels[y * size + x] = light;
                    }
                    else if (diamondDist < 0.85f)
                    {
                        // Основной цвет
                        pixels[y * size + x] = main;
                    }
                    else if (diamondDist < 1.0f)
                    {
                        // Контур
                        pixels[y * size + x] = dark;
                    }
                    // else: прозрачный (уже заполнен Color.clear)
                }
            }

            // Маленький блик сверху-слева
            int hlSize = size / 6;
            int hlX = (int)(center - radius * 0.25f);
            int hlY = (int)(center + radius * 0.3f);
            for (int x = hlX; x < hlX + hlSize && x < size; x++)
            {
                for (int y = hlY; y < hlY + hlSize && y < size; y++)
                {
                    if (x >= 0 && y >= 0)
                    {
                        pixels[y * size + x] = Color.white;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            // PPU=160 → 64/160 = 0.4 юнита (в 5 раз меньше ячейки 2.0)
            // Редактировано: 2026-04-15 — PPU увеличен с 32 до 160
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 160);
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
        /// Редактировано: 2026-04-14 07:55:00 UTC — увеличены лимиты для карты 100×80, добавлены новые типы
        /// </summary>
        public void SetDefaultResourceTypes()
        {
            resourceTypes.Clear();

            // === Растительные ресурсы (лесная зона) ===
            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "herb",
                displayName = "Целебная трава",
                spriteColor = new Color(0.3f, 0.8f, 0.3f),
                amount = 1,
                minCount = 15,
                maxCount = 30,
                spawnTerrain = new[] { TerrainType.Grass },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "berries",
                displayName = "Ягоды",
                spriteColor = new Color(0.8f, 0.2f, 0.3f),
                amount = 3,
                minCount = 10,
                maxCount = 20,
                spawnTerrain = new[] { TerrainType.Grass },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "mushroom",
                displayName = "Гриб Ци",
                spriteColor = new Color(0.7f, 0.5f, 0.9f),
                amount = 2,
                minCount = 8,
                maxCount = 15,
                spawnTerrain = new[] { TerrainType.Grass, TerrainType.Dirt },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "rare_herb",
                displayName = "Редкая трава",
                spriteColor = new Color(0.2f, 0.9f, 0.6f),
                amount = 1,
                minCount = 3,
                maxCount = 8,
                spawnTerrain = new[] { TerrainType.Grass },
                autoPickup = true
            });

            // === Минеральные ресурсы (каменные зоны) ===
            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "stone",
                displayName = "Камень",
                spriteColor = new Color(0.6f, 0.6f, 0.65f),
                amount = 2,
                minCount = 10,
                maxCount = 20,
                spawnTerrain = new[] { TerrainType.Stone, TerrainType.Dirt },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "ore",
                displayName = "Руда",
                spriteColor = new Color(0.8f, 0.5f, 0.2f),
                amount = 1,
                minCount = 6,
                maxCount = 12,
                spawnTerrain = new[] { TerrainType.Stone },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "iron_ore",
                displayName = "Железная руда",
                spriteColor = new Color(0.5f, 0.5f, 0.55f),
                amount = 2,
                minCount = 4,
                maxCount = 10,
                spawnTerrain = new[] { TerrainType.Stone },
                autoPickup = true
            });

            // === Ци-ресурсы (редкие) ===
            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "qi_crystal",
                displayName = "Ци-кристалл",
                spriteColor = new Color(0.4f, 0.7f, 1f),
                amount = 1,
                minCount = 4,
                maxCount = 10,
                spawnTerrain = new[] { TerrainType.Stone },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "spirit_stone",
                displayName = "Духовный камень",
                spriteColor = new Color(0.9f, 0.7f, 1f),
                amount = 1,
                minCount = 2,
                maxCount = 5,
                spawnTerrain = new[] { TerrainType.Stone },
                autoPickup = true
            });

            // === Древесина ===
            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "wood",
                displayName = "Древесина",
                spriteColor = new Color(0.6f, 0.4f, 0.2f),
                amount = 3,
                minCount = 12,
                maxCount = 25,
                spawnTerrain = new[] { TerrainType.Grass, TerrainType.Dirt },
                autoPickup = true
            });

            // === Пустынные ресурсы ===
            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "sand_pearl",
                displayName = "Песчаная жемчужина",
                spriteColor = new Color(0.9f, 0.85f, 0.5f),
                amount = 1,
                minCount = 4,
                maxCount = 8,
                spawnTerrain = new[] { TerrainType.Sand },
                autoPickup = true
            });

            resourceTypes.Add(new ResourceSpawnEntry
            {
                resourceId = "desert_crystal",
                displayName = "Пустынный кристалл",
                spriteColor = new Color(1f, 0.8f, 0.3f),
                amount = 1,
                minCount = 2,
                maxCount = 5,
                spawnTerrain = new[] { TerrainType.Sand },
                autoPickup = true
            });
        }

        /// <summary>
        /// Загрузить AI-спрайт ресурса.
        /// Маппинг resourceId → файл спрайта в Assets/Sprites/Tiles/
        /// (обработанные AI-спрайты с прозрачностью лежат в Tiles/).
        /// FIX 3A: Убран Tiles_AI/ из поиска — AI-спрайты там RGB без альфа-канала,
        /// что приводит к белому фону. Обработанные RGBA-спрайты лежат в Tiles/.
        /// Редактировано: 2026-04-15 16:53:48 UTC
        /// </summary>
        private Sprite LoadResourceSprite(string resourceId)
        {
            // Маппинг resourceId → имя файла спрайта
            // FIX: Добавлены маппинги для ВСЕХ типов ресурсов
            // Редактировано: 2026-04-15 12:00:00 UTC
            string spriteName = resourceId switch
            {
                "ore" => "obj_ore_vein",
                "iron_ore" => "obj_ore_vein",
                "stone" => "obj_rock_medium",
                "wood" => "obj_tree",
                "herb" => "obj_herb",
                "rare_herb" => "obj_herb",
                "berries" => "obj_bush",
                "mushroom" => "obj_herb",
                "qi_crystal" => "obj_ore_vein",
                "spirit_stone" => "obj_rock_medium",
                "sand_pearl" => "obj_rock_small",
                "desert_crystal" => "obj_ore_vein",
                _ => null
            };

            if (spriteName == null) return null;

            #if UNITY_EDITOR
            // Editor: загрузка из Assets/Sprites/Tiles/ (обработанные AI-спрайты с прозрачностью)
            // FIX 3A: Tiles_AI/ убран из поиска — там RGB спрайты без alpha → белый фон
            // Редактировано: 2026-04-15 16:53:48 UTC
            string[] searchPaths = new string[]
            {
                $"Assets/Sprites/Tiles/{spriteName}.png"  // Только обработанные спрайты с прозрачностью
            };
            foreach (var path in searchPaths)
            {
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    return sprite;
                }
            }
            #else
            // Runtime build: загрузка из Resources
            var resSprite = Resources.Load<Sprite>($"Sprites/{spriteName}");
            if (resSprite != null) return resSprite;
            #endif

            return null;
        }
    }
}
