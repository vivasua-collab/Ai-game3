// ============================================================================
// HarvestableSpawner.cs — Спавнер harvestable-объектов как GameObject
// Cultivation World Simulator
// Версия: 1.1
// Создано: 2026-04-16
// Редактировано: 2026-04-16 — КРИТ-1 FIX: Unlit шейдер, СРЕД-1 FIX: EnsureSpriteImportSettings
// Чекпоинт: 04_15_harvest_system_plan.md v3 §6
// ============================================================================
//
// Подписывается на TileMapController.OnMapGenerated,
// читает TileMapData, создаёт GameObject для isHarvestable объектов.
//
// Вариант C: все harvestable объекты спавнятся как отдельные GameObject.
// objectTilemap используется только для не-harvestable объектов.
//
// Physics-слой "Harvestable" — для LayerMask фильтрации в Physics2D.
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Спавнер harvestable-объектов как отдельных GameObject.
    /// Подписывается на TileMapController.OnMapGenerated и создаёт
    /// объекты с коллайдерами для обнаружения через Physics2D.
    /// </summary>
    public class HarvestableSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Контроллер карты (если null — ищется через ServiceLocator)")]
        [SerializeField] private TileMapController tileMapController;

        [Header("Settings")]
        [Tooltip("Слои для Physics2D поиска (слой \"Harvestable\")")]
        [SerializeField] private LayerMask harvestableLayerMask = -1;

        [Tooltip("Имя слоя для harvestable-объектов")]
        [SerializeField] private string harvestableLayerName = "Harvestable";

        [Tooltip("Родительский объект для спавна (если null — создаётся автоматически)")]
        [SerializeField] private Transform spawnParent;

        [Header("Sprite Settings")]
        [Tooltip("Размер спрайта в пикселях")]
        [SerializeField] private int spriteSize = 64;

        [Tooltip("Целевой размер в мировых координатах")]
        [SerializeField] private float targetWorldSize = 1.8f;

        // === Runtime State ===
        private List<GameObject> spawnedHarvestables = new List<GameObject>();
        private int harvestableLayerIndex = -1;
        private bool isSubscribed = false;

        // === Properties ===
        public IReadOnlyList<GameObject> SpawnedHarvestables => spawnedHarvestables;
        public int SpawnedCount => spawnedHarvestables.Count;

        // === Unity Lifecycle ===

        private void Awake()
        {
            // Определить индекс слоя "Harvestable"
            harvestableLayerIndex = LayerMask.NameToLayer(harvestableLayerName);

            if (harvestableLayerIndex < 0)
            {
                Debug.LogWarning($"[HarvestableSpawner] Слой \"{harvestableLayerName}\" не найден в Tag Manager. " +
                    "Добавьте слой в Edit → Project Settings → Tags and Layers. " +
                    "Используется слой Default как fallback.");
                harvestableLayerIndex = 0; // Default layer
            }

            // Создать родительский объект
            if (spawnParent == null)
            {
                var parentObj = new GameObject("HarvestableObjects");
                parentObj.transform.SetParent(transform);
                spawnParent = parentObj.transform;
            }

            // FIX Race Condition: подписываемся в Awake (до Start других объектов),
            // чтобы не пропустить OnMapGenerated от TileMapController.Start().
            // Если TileMapController уже сгенерировал карту — подписка всё равно нужна
            // для повторных генераций.
            // Редактировано: 2026-04-16
            if (tileMapController == null)
            {
                tileMapController = CultivationGame.Core.ServiceLocator.GetOrFind<TileMapController>();
            }
            SubscribeToMapEvents();
        }

        private void Start()
        {
            // FIX Race Condition: проверяем, была ли карта уже сгенерирована
            // до нашей подписки (TileMapController.Start мог выполниться раньше).
            // Если MapData уже есть — спавним harvestable-объекты немедленно.
            // Редактировано: 2026-04-16
            if (tileMapController != null && tileMapController.MapData != null && spawnedHarvestables.Count == 0)
            {
                SpawnHarvestables(tileMapController.MapData);
                Debug.Log("[HarvestableSpawner] Карта уже сгенерирована — спавн выполнен в Start()");
            }
            else if (tileMapController == null)
            {
                // Пытаемся найти ещё раз (ServiceLocator мог ещё не быть инициализирован в Awake)
                tileMapController = CultivationGame.Core.ServiceLocator.GetOrFind<TileMapController>();
                if (tileMapController != null)
                {
                    SubscribeToMapEvents();
                    if (tileMapController.MapData != null && spawnedHarvestables.Count == 0)
                    {
                        SpawnHarvestables(tileMapController.MapData);
                        Debug.Log("[HarvestableSpawner] TileMapController найден в Start() — спавн выполнен");
                    }
                }
                else
                {
                    Debug.LogError("[HarvestableSpawner] TileMapController не найден! Спавн невозможен.");
                }
            }
        }

        private void OnDestroy()
        {
            UnsubscribeFromMapEvents();
        }

        // === Публичные методы ===

        /// <summary>
        /// Спавнить все harvestable-объекты из TileMapData.
        /// Вызывается автоматически при OnMapGenerated.
        /// </summary>
        public void SpawnHarvestables(TileMapData mapData)
        {
            if (mapData == null)
            {
                Debug.LogError("[HarvestableSpawner] mapData is null!");
                return;
            }

            // Очистить предыдущие объекты
            ClearSpawnedObjects();

            int spawnedCount = 0;
            int skippedCount = 0;

            // Обойти все тайлы и найти harvestable-объекты
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile == null || tile.objects.Count == 0) continue;

                    foreach (var obj in tile.objects)
                    {
                        if (!obj.isHarvestable) continue;

                        // Создать GameObject для harvestable-объекта
                        GameObject harvestableGO = CreateHarvestableGameObject(obj, x, y, mapData);

                        if (harvestableGO != null)
                        {
                            spawnedHarvestables.Add(harvestableGO);
                            spawnedCount++;
                        }
                        else
                        {
                            skippedCount++;
                        }
                    }
                }
            }

            Debug.Log($"[HarvestableSpawner] Спавн завершён: {spawnedCount} объектов, {skippedCount} пропущено");
        }

        /// <summary>
        /// Удалить все спавненные объекты.
        /// </summary>
        public void ClearSpawnedObjects()
        {
            foreach (var go in spawnedHarvestables)
            {
                if (go != null)
                    Destroy(go);
            }
            spawnedHarvestables.Clear();
        }

        // === Приватные методы ===

        /// <summary>
        /// Создать GameObject для harvestable-объекта.
        /// Спецификация: §4.4 чекпоинта.
        /// </summary>
        private GameObject CreateHarvestableGameObject(TileObjectData objData, int tileX, int tileY, TileMapData mapData)
        {
            // Мировые координаты центра тайла
            Vector2 worldPos = mapData.TileToWorld(tileX, tileY);

            // Создать GameObject
            string goName = $"Harvestable_{objData.objectType}_{tileX}_{tileY}";
            GameObject go = new GameObject(goName);

            // Установить позицию
            go.transform.position = new Vector3(worldPos.x, worldPos.y, 0);
            go.transform.SetParent(spawnParent);

            // Установить слой
            go.layer = harvestableLayerIndex;

            // Установить тег
            go.tag = "Harvestable";

            // 1. SpriteRenderer — визуал объекта
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            Sprite sprite = LoadHarvestableSprite(objData.objectType);
            sr.sprite = sprite;

            // Настроить рендер
            if (sprite != null)
            {
                float scale = CalculateSpriteScale(sprite);
                go.transform.localScale = new Vector3(scale, scale, 1f);
            }

            // Сортировка — объекты поверх террейна
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = GetSortingOrder(objData.objectType);

            // КРИТ-1 FIX: Unlit шейдер — рендерит БЕЗ Light2D (как в PlayerVisual и ResourceSpawner).
            // Sprite-Lit-Default без Light2D → чёрные/невидимые спрайты.
            // Sprite-Unlit-Default рендерит без освещения — всегда виден.
            // Редактировано: 2026-04-16
            Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (spriteShader == null) spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (spriteShader == null) spriteShader = Shader.Find("Sprites/Default");
            sr.material = new Material(spriteShader);

            // 2. Rigidbody2D (Static) — для корректной работы физики
            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            // 3. BoxCollider2D — физический коллайдер (блокировка прохода), НЕ trigger
            BoxCollider2D solidCollider = go.AddComponent<BoxCollider2D>();
            solidCollider.isTrigger = false;
            if (sprite != null)
            {
                // Размер коллайдера = 80% от размера спрайта (чтобы можно было подойти близко)
                Vector2 spriteSize = sprite.bounds.size;
                solidCollider.size = new Vector2(spriteSize.x * 0.6f, spriteSize.y * 0.7f);
                solidCollider.offset = new Vector2(0, -spriteSize.y * 0.1f);
            }

            // 4. CircleCollider2D (isTrigger) — зона обнаружения для Physics2D
            CircleCollider2D detectionCollider = go.AddComponent<CircleCollider2D>();
            detectionCollider.isTrigger = true;
            detectionCollider.radius = 1.2f; // Больше физического коллайдера

            // 5. Harvestable — логика добычи
            Harvestable harvestable = go.AddComponent<Harvestable>();

            // FIX-R4: Передаём depletedSprite в Initialize, чтобы Harvestable
            // мог сменить спрайт при исчерпании (Deplete()).
            // Ранее: depletedSprite загружался, но НЕ передавался — мёртвый код.
            // Редактировано: 2026-04-16 07:55 UTC
            Sprite depletedSprite = LoadDepletedSprite(objData.objectType);
            harvestable.Initialize(objData, depletedSprite);

            return go;
        }

        /// <summary>
        /// Загрузить спрайт для harvestable-объекта.
        /// Пытается загрузить AI-спрайт, при неудаче — создаёт программный.
        /// </summary>
        private Sprite LoadHarvestableSprite(TileObjectType objectType)
        {
            // Попытка загрузить AI-спрайт
            string spritePath = GetSpritePath(objectType);
            Sprite sprite = LoadSpriteFromAssets(spritePath);

            if (sprite != null)
                return sprite;

            // Fallback: программный спрайт
            return CreateProceduralSprite(objectType);
        }

        /// <summary>
        /// Загрузить спрайт для исчерпанного состояния.
        /// </summary>
        private Sprite LoadDepletedSprite(TileObjectType objectType)
        {
            string depletedPath = GetDepletedSpritePath(objectType);
            return LoadSpriteFromAssets(depletedPath);
        }

        /// <summary>
        /// Получить путь к AI-спрайту для типа объекта.
        /// </summary>
        // FIX-R1: Исправлены пути спрайтов — добавлен префикс obj_ и реальные имена файлов.
        // Ранее: "tree_oak" → файла нет → процедурный fallback → уродливые спрайты.
        // Теперь: "obj_tree_oak" → Assets/Sprites/Tiles/obj_tree_oak.png → AI-спрайт.
        // Редактировано: 2026-04-16 07:55 UTC
        private string GetSpritePath(TileObjectType objectType)
        {
            return objectType switch
            {
                TileObjectType.Tree_Oak => "Sprites/Tiles/obj_tree_oak",
                TileObjectType.Tree_Pine => "Sprites/Tiles/obj_tree_pine",
                TileObjectType.Tree_Birch => "Sprites/Tiles/obj_tree_birch",
                TileObjectType.Rock_Small => "Sprites/Tiles/obj_rock_small",
                TileObjectType.Rock_Medium => "Sprites/Tiles/obj_rock_medium",
                TileObjectType.OreVein => "Sprites/Tiles/obj_ore_vein",
                TileObjectType.Bush => "Sprites/Tiles/obj_bush",
                TileObjectType.Bush_Berry => "Sprites/Tiles/obj_bush_berry",
                TileObjectType.Herb => "Sprites/Tiles/obj_herb",
                _ => null
            };
        }

        /// <summary>
        /// Получить путь к спрайту для исчерпанного состояния.
        /// </summary>
        // FIX-R4: Исправлены пути depleted-спрайтов — добавлен префикс obj_.
        // Ранее: "stump" → файла нет → depleted не отображался.
        // Теперь: "obj_stump" → Assets/Sprites/Tiles/obj_stump.png → AI-спрайт пня.
        // Редактировано: 2026-04-16 07:55 UTC
        private string GetDepletedSpritePath(TileObjectType objectType)
        {
            return objectType switch
            {
                TileObjectType.Tree_Oak => "Sprites/Tiles/obj_stump",
                TileObjectType.Tree_Pine => "Sprites/Tiles/obj_stump",
                TileObjectType.Tree_Birch => "Sprites/Tiles/obj_stump",
                TileObjectType.Rock_Small => "Sprites/Tiles/obj_rock_depleted",
                TileObjectType.Rock_Medium => "Sprites/Tiles/obj_rock_depleted",
                TileObjectType.OreVein => "Sprites/Tiles/obj_ore_depleted",
                TileObjectType.Bush => "Sprites/Tiles/obj_bush_depleted",
                TileObjectType.Bush_Berry => "Sprites/Tiles/obj_bush_depleted",
                TileObjectType.Herb => "Sprites/Tiles/obj_herb",
                _ => null
            };
        }

        /// <summary>
        /// Загрузить спрайт из Assets/Sprites/ или Resources/.
        /// СРЕД-1 FIX: Перед загрузкой — принудительный реимпорт с правильными настройками.
        /// Без .meta в Git Unity использует дефолтные: TextureType=Default (НЕ Sprite), PPU=100.
        /// LoadAssetAtPath<Sprite>() возвращает null при TextureType≠Sprite.
        /// Редактировано: 2026-04-16
        /// </summary>
        private Sprite LoadSpriteFromAssets(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

#if UNITY_EDITOR
            // В Editor — загрузить через AssetDatabase
            string fullPath = $"Assets/{relativePath}.png";

            // СРЕД-1 FIX: Убедиться, что спрайт импортирован корректно
            // Редактировано: 2026-04-16
            bool isObject = relativePath.Contains("obj_");
            EnsureSpriteImportSettings(fullPath, isObject);
            // Перезагружаем после возможного реимпорта
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
            if (sprite != null) return sprite;
#endif

            // В Build — загрузить через Resources
            string resourcesPath = relativePath;
            if (resourcesPath.StartsWith("Sprites/"))
                resourcesPath = resourcesPath.Substring("Sprites/".Length);

            return Resources.Load<Sprite>(resourcesPath);
        }

#if UNITY_EDITOR
        /// <summary>
        /// СРЕД-1 FIX: Убедиться, что спрайт импортирован с правильными настройками.
        /// Без .meta в Git Unity использует дефолтные: TextureType=Default (НЕ Sprite), PPU=100.
        /// LoadAssetAtPath<Sprite>() возвращает null при TextureType≠Sprite.
        /// КРИТ-3 FIX: Terrain PPU=31 (64/31=2.065u — pixel bleed). Objects PPU=160.
        /// Редактировано: 2026-04-16
        /// </summary>
        private void EnsureSpriteImportSettings(string assetPath, bool isObject)
        {
            var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
            if (importer == null) return;

            // КРИТ-3 FIX: Terrain PPU=31 (64/31=2.065u — pixel bleed). Objects PPU=160.
            int targetPPU = isObject ? 160 : 31;
            bool needsReimport = importer.textureType != UnityEditor.TextureImporterType.Sprite
                || importer.spritePixelsPerUnit != targetPPU
                || importer.alphaIsTransparency != true;

            if (needsReimport)
            {
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = targetPPU;
                importer.alphaIsTransparency = true;
                importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                importer.filterMode = isObject ? FilterMode.Point : FilterMode.Bilinear;
                UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
                Debug.Log($"[HarvestableSpawner] Спрайт реимпортирован: {assetPath} → PPU={targetPPU}, alphaIsTransparency=true");
            }
        }
#endif

        /// <summary>
        /// Создать программный (procedural) спрайт для типа объекта.
        /// Fallback при отсутствии AI-спрайта.
        /// </summary>
        private Sprite CreateProceduralSprite(TileObjectType objectType)
        {
            int size = spriteSize; // 64px
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;

            Color mainColor = GetObjectColor(objectType);
            Color darkColor = mainColor * 0.7f;
            Color lightColor = mainColor * 1.3f;

            // Залить прозрачным
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.clear;
            texture.SetPixels(pixels);

            // Нарисовать фигуру в зависимости от типа
            switch (objectType)
            {
                case TileObjectType.Tree_Oak:
                case TileObjectType.Tree_Pine:
                case TileObjectType.Tree_Birch:
                    DrawTreeShape(texture, size, mainColor, darkColor, lightColor);
                    break;

                case TileObjectType.Rock_Small:
                case TileObjectType.Rock_Medium:
                    DrawRockShape(texture, size, mainColor, darkColor, lightColor);
                    break;

                case TileObjectType.OreVein:
                    DrawOreShape(texture, size, mainColor, darkColor, lightColor);
                    break;

                case TileObjectType.Bush:
                case TileObjectType.Bush_Berry:
                    DrawBushShape(texture, size, mainColor, darkColor, lightColor);
                    break;

                case TileObjectType.Herb:
                    DrawHerbShape(texture, size, mainColor, darkColor, lightColor);
                    break;

                default:
                    DrawDefaultShape(texture, size, mainColor);
                    break;
            }

            texture.Apply();

            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                32 // PPU
            );
            sprite.name = $"Procedural_{objectType}";

            return sprite;
        }

        /// <summary>
        /// Получить цвет объекта по типу (для программных спрайтов).
        /// </summary>
        private Color GetObjectColor(TileObjectType objectType)
        {
            return objectType switch
            {
                TileObjectType.Tree_Oak => new Color(0.4f, 0.6f, 0.2f),    // Тёмно-зелёный
                TileObjectType.Tree_Pine => new Color(0.2f, 0.5f, 0.2f),    // Хвойный зелёный
                TileObjectType.Tree_Birch => new Color(0.5f, 0.7f, 0.3f),   // Светло-зелёный
                TileObjectType.Rock_Small => new Color(0.6f, 0.6f, 0.6f),   // Серый
                TileObjectType.Rock_Medium => new Color(0.5f, 0.5f, 0.5f),  // Тёмно-серый
                TileObjectType.OreVein => new Color(0.7f, 0.4f, 0.2f),      // Коричнево-оранжевый
                TileObjectType.Bush => new Color(0.3f, 0.6f, 0.2f),         // Кустарник
                TileObjectType.Bush_Berry => new Color(0.4f, 0.3f, 0.6f),   // Фиолетовый (ягоды)
                TileObjectType.Herb => new Color(0.3f, 0.8f, 0.3f),         // Ярко-зелёный
                _ => Color.white
            };
        }

        /// <summary>
        /// Рассчитать масштаб спрайта для целевого мирового размера.
        /// </summary>
        private float CalculateSpriteScale(Sprite sprite)
        {
            if (sprite == null) return 1f;
            float spriteWorldWidth = sprite.bounds.size.x;
            if (spriteWorldWidth <= 0) return 1f;
            float scale = targetWorldSize / spriteWorldWidth;
            return Mathf.Clamp(scale, 0.1f, 20f);
        }

        /// <summary>
        /// Получить порядок сортировки для типа объекта.
        /// </summary>
        private int GetSortingOrder(TileObjectType objectType)
        {
            return objectType switch
            {
                TileObjectType.Herb => 1,
                TileObjectType.Bush => 2,
                TileObjectType.Bush_Berry => 2,
                TileObjectType.Rock_Small => 3,
                TileObjectType.Rock_Medium => 3,
                TileObjectType.OreVein => 4,
                TileObjectType.Tree_Oak => 5,
                TileObjectType.Tree_Pine => 5,
                TileObjectType.Tree_Birch => 5,
                _ => 0
            };
        }

        // === Подписка на события карты ===

        private void SubscribeToMapEvents()
        {
            if (tileMapController == null || isSubscribed) return;

            tileMapController.OnMapGenerated += OnMapGenerated;
            isSubscribed = true;
        }

        private void UnsubscribeFromMapEvents()
        {
            if (tileMapController != null && isSubscribed)
            {
                tileMapController.OnMapGenerated -= OnMapGenerated;
            }
            isSubscribed = false;
        }

        private void OnMapGenerated(TileMapData mapData)
        {
            SpawnHarvestables(mapData);
        }

        // === Рисование программных спрайтов ===

        private void DrawTreeShape(Texture2D tex, int size, Color main, Color dark, Color light)
        {
            int half = size / 2;

            // Ствол (коричневый)
            Color trunk = new Color(0.4f, 0.25f, 0.1f);
            FillRect(tex, half - 4, 0, 8, half - 4, trunk);

            // Крона (треугольная)
            for (int row = 0; row < half + 8; row++)
            {
                int width = (int)((half + 8 - row) * 0.8f);
                int cx = half;
                for (int col = -width; col <= width; col++)
                {
                    int x = cx + col;
                    int y = half - 4 + row;
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        Color c = (col + row) % 4 == 0 ? light : (col % 3 == 0 ? dark : main);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
        }

        private void DrawRockShape(Texture2D tex, int size, Color main, Color dark, Color light)
        {
            int half = size / 2;

            // Камень — округлая форма
            for (int y = 4; y < size - 4; y++)
            {
                for (int x = 4; x < size - 4; x++)
                {
                    float dx = (x - half) / (float)(half - 6);
                    float dy = (y - half) / (float)(half - 6);
                    float dist = dx * dx + dy * dy;

                    if (dist < 0.8f)
                    {
                        Color c = dist < 0.3f ? light : (dist > 0.6f ? dark : main);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
        }

        private void DrawOreShape(Texture2D tex, int size, Color main, Color dark, Color light)
        {
            // Основа — камень
            DrawRockShape(tex, size, new Color(0.5f, 0.5f, 0.5f), new Color(0.35f, 0.35f, 0.35f), new Color(0.65f, 0.65f, 0.65f));

            // Вкрапления руды
            Color oreColor = new Color(0.9f, 0.6f, 0.2f);
            int half = size / 2;
            var orePositions = new[] { (half - 8, half - 4), (half + 6, half + 2), (half - 2, half + 8), (half + 10, half - 8) };
            foreach (var (ox, oy) in orePositions)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        if (dx * dx + dy * dy <= 4)
                        {
                            int px = ox + dx;
                            int py = oy + dy;
                            if (px >= 0 && px < size && py >= 0 && py < size)
                                tex.SetPixel(px, py, oreColor);
                        }
                    }
                }
            }
        }

        private void DrawBushShape(Texture2D tex, int size, Color main, Color dark, Color light)
        {
            int half = size / 2;

            // Куст — округлая форма в нижней половине
            for (int y = 4; y < size - 8; y++)
            {
                for (int x = 8; x < size - 8; x++)
                {
                    float dx = (x - half) / (float)(half - 10);
                    float dy = (y - half + 4) / (float)(half - 10);
                    float dist = dx * dx + dy * dy;

                    if (dist < 0.7f)
                    {
                        Color c = dist < 0.2f ? light : (dist > 0.5f ? dark : main);
                        tex.SetPixel(x, y, c);
                    }
                }
            }

            // Ягоды (для Bush_Berry)
            if (main.b > 0.4f)
            {
                Color berryColor = new Color(0.8f, 0.2f, 0.3f);
                var berryPos = new[] { (half - 6, half - 2), (half + 4, half), (half, half + 6), (half + 8, half - 6) };
                foreach (var (bx, by) in berryPos)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            int px = bx + dx;
                            int py = by + dy;
                            if (px >= 0 && px < size && py >= 0 && py < size)
                                tex.SetPixel(px, py, berryColor);
                        }
                    }
                }
            }
        }

        private void DrawHerbShape(Texture2D tex, int size, Color main, Color dark, Color light)
        {
            int half = size / 2;

            // Стебель
            Color stemColor = new Color(0.2f, 0.5f, 0.1f);
            for (int y = 4; y < size - 16; y++)
            {
                tex.SetPixel(half, y, stemColor);
                if (y % 3 == 0) tex.SetPixel(half + 1, y, stemColor);
            }

            // Листья сверху
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -6; dx <= 6; dx++)
                {
                    int x = half + dx;
                    int y = size - 16 + dy;
                    if (x >= 0 && x < size && y >= 0 && y < size)
                    {
                        float dist = (dx * dx) / 36f + (dy * dy) / 16f;
                        if (dist < 1f)
                        {
                            Color c = dist < 0.3f ? light : main;
                            tex.SetPixel(x, y, c);
                        }
                    }
                }
            }
        }

        private void DrawDefaultShape(Texture2D tex, int size, Color main)
        {
            int half = size / 2;
            for (int y = 8; y < size - 8; y++)
            {
                for (int x = 8; x < size - 8; x++)
                {
                    float dx = (x - half) / (float)(half - 10);
                    float dy = (y - half) / (float)(half - 10);
                    if (dx * dx + dy * dy < 1f)
                        tex.SetPixel(x, y, main);
                }
            }
        }

        /// <summary>
        /// Залить прямоугольную область цветом.
        /// </summary>
        private void FillRect(Texture2D tex, int x0, int y0, int width, int height, Color color)
        {
            for (int y = y0; y < y0 + height && y < tex.height; y++)
            {
                for (int x = x0; x < x0 + width && x < tex.width; x++)
                {
                    if (x >= 0 && y >= 0)
                        tex.SetPixel(x, y, color);
                }
            }
        }
    }
}
