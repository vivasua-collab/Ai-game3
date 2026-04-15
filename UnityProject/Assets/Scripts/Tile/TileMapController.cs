// ============================================================================
// TileMapController.cs — Контроллер карты тайлов
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-15 12:00:00 UTC — FIX: terrain Rect(0,0,66,66) для pixel bleed overlap, объектные спрайты с прозрачностью
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Контроллер карты тайлов.
    /// Управляет отображением, обновлением и взаимодействием с тайлами.
    /// </summary>
    public class TileMapController : MonoBehaviour
    {
        [Header("Tilemap References")]
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private Tilemap objectTilemap;
        [SerializeField] private Tilemap overlayTilemap;

        [Header("Terrain Tiles")]
        [SerializeField] private TileBase grassTile;
        [SerializeField] private TileBase dirtTile;
        [SerializeField] private TileBase stoneTile;
        [SerializeField] private TileBase waterShallowTile;
        [SerializeField] private TileBase waterDeepTile;
        [SerializeField] private TileBase sandTile;
        [SerializeField] private TileBase voidTile;
        // FIX: Добавлены недостающие типы террейна
        // Редактировано: 2026-04-14 13:45:00 UTC
        [SerializeField] private TileBase snowTile;
        [SerializeField] private TileBase iceTile;
        [SerializeField] private TileBase lavaTile;

        [Header("Object Tiles")]
        [SerializeField] private TileBase treeTile;
        [SerializeField] private TileBase rockSmallTile;
        [SerializeField] private TileBase rockMediumTile;
        [SerializeField] private TileBase bushTile;
        [SerializeField] private TileBase chestTile;
        // FIX: Добавлены недостающие типы объектов
        [SerializeField] private TileBase oreVeinTile;
        [SerializeField] private TileBase herbTile;

        [Header("Settings")]
        // FIX: Размер карты 100×80 тайлов = 200×160 метров (было 80×60)
        // Редактировано: 2026-04-14 13:45:00 UTC
        [SerializeField] private int defaultWidth = 100;
        [SerializeField] private int defaultHeight = 80;
        [SerializeField] private bool generateOnStart = true;

        // === State ===
        private TileMapData mapData;
        private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();
        private Transform objectsParent;

        // === Events ===
        public event Action<TileData> OnTileChanged;
        public event Action<TileMapData> OnMapGenerated;

        // === Properties ===
        public TileMapData MapData => mapData;
        public int Width => mapData?.width ?? 0;
        public int Height => mapData?.height ?? 0;

        // === Unity Lifecycle ===

        private void Awake()
        {
            // Создать родительский объект для спавна
            objectsParent = new GameObject("SpawnedObjects").transform;
            objectsParent.SetParent(transform);

            // FIX: cellGap = 0. Pixel bleed через увеличенные terrain-спрайты (68×68 PPU=32 → 2.125u)
            // устраняет белую сетку надёжнее, чем отрицательный cellGap.
            // Редактировано: 2026-04-16 UTC
            var grid = GetComponentInParent<Grid>();
            if (grid != null)
            {
                grid.cellGap = Vector3.zero;
            }
        }

        private void Start()
        {
            // FIX: Автосоздание GameTile из спрайтов, если [SerializeField] поля не назначены
            // Редактировано: 2026-04-15 11:15:00 UTC
            EnsureTileAssets();

            if (generateOnStart)
            {
                GenerateTestMap();
            }
        }

        // === Tile Asset Auto-Creation ===

        /// <summary>
        /// FIX: Автоматически создать GameTile из спрайтов, если [SerializeField] поля не назначены.
        /// Это устраняет проблему "цветных точек" — когда тайлы не назначены в Inspector,
        /// Tilemap показывает пустые клетки. Метод загружает спрайты из Assets/Sprites/
        /// и создаёт GameTile экземпляры runtime.
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private void EnsureTileAssets()
        {
            // Terrain tile mappings: field → sprite name → passable flag
            var terrainMappings = new (TileBase field, string spriteName, bool passable, TerrainType terrain)[]
            {
                (grassTile, "terrain_grass", true, TerrainType.Grass),
                (dirtTile, "terrain_dirt", true, TerrainType.Dirt),
                (stoneTile, "terrain_stone", true, TerrainType.Stone),
                (waterShallowTile, "terrain_water_shallow", false, TerrainType.Water_Shallow),
                (waterDeepTile, "terrain_water_deep", false, TerrainType.Water_Deep),
                (sandTile, "terrain_sand", true, TerrainType.Sand),
                (voidTile, "terrain_void", false, TerrainType.Void),
                (snowTile, "terrain_snow", true, TerrainType.Snow),
                (iceTile, "terrain_ice", false, TerrainType.Ice),
                (lavaTile, "terrain_lava", false, TerrainType.Lava),
            };

            // Object tile mappings
            var objectMappings = new (TileBase field, string spriteName, bool passable)[]
            {
                (treeTile, "obj_tree", false),
                (rockSmallTile, "obj_rock_small", false),
                (rockMediumTile, "obj_rock_medium", false),
                (bushTile, "obj_bush", true),
                (chestTile, "obj_chest", true),
                (oreVeinTile, "obj_ore_vein", false),
                (herbTile, "obj_herb", true),
            };

            // Обработка terrain тайлов
            grassTile = EnsureTile(grassTile, "terrain_grass", true, TerrainType.Grass);
            dirtTile = EnsureTile(dirtTile, "terrain_dirt", true, TerrainType.Dirt);
            stoneTile = EnsureTile(stoneTile, "terrain_stone", true, TerrainType.Stone);
            waterShallowTile = EnsureTile(waterShallowTile, "terrain_water_shallow", false, TerrainType.Water_Shallow);
            waterDeepTile = EnsureTile(waterDeepTile, "terrain_water_deep", false, TerrainType.Water_Deep);
            sandTile = EnsureTile(sandTile, "terrain_sand", true, TerrainType.Sand);
            voidTile = EnsureTile(voidTile, "terrain_void", false, TerrainType.Void);
            snowTile = EnsureTile(snowTile, "terrain_snow", true, TerrainType.Snow);
            iceTile = EnsureTile(iceTile, "terrain_ice", false, TerrainType.Ice);
            lavaTile = EnsureTile(lavaTile, "terrain_lava", false, TerrainType.Lava);

            // Обработка object тайлов
            treeTile = EnsureTile(treeTile, "obj_tree", false, TerrainType.Grass);
            rockSmallTile = EnsureTile(rockSmallTile, "obj_rock_small", false, TerrainType.Grass);
            rockMediumTile = EnsureTile(rockMediumTile, "obj_rock_medium", false, TerrainType.Grass);
            bushTile = EnsureTile(bushTile, "obj_bush", true, TerrainType.Grass);
            chestTile = EnsureTile(chestTile, "obj_chest", true, TerrainType.Grass);
            oreVeinTile = EnsureTile(oreVeinTile, "obj_ore_vein", false, TerrainType.Stone);
            herbTile = EnsureTile(herbTile, "obj_herb", true, TerrainType.Grass);

            // FIX: cellGap = 0. Pixel bleed через terrain-спрайты устраняет зазоры.
            // Редактировано: 2026-04-16 UTC
            var grid = GetComponentInParent<Grid>();
            if (grid != null)
            {
                grid.cellGap = Vector3.zero;
            }
        }

        /// <summary>
        /// Создать GameTile из спрайта, если поле не назначено.
        /// Загружает спрайт из Assets/Sprites/Tiles_AI/ или Assets/Sprites/Tiles/.
        /// Fallback: создаёт процедурный спрайт.
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private TileBase EnsureTile(TileBase currentTile, string spriteName, bool passable, TerrainType terrain)
        {
            if (currentTile != null) return currentTile;

            Sprite loadedSprite = LoadTileSprite(spriteName);
            if (loadedSprite == null)
            {
                // Fallback: создать процедурный спрайт
                loadedSprite = CreateProceduralTileSprite(spriteName, terrain);
            }

            if (loadedSprite != null)
            {
                var tile = ScriptableObject.CreateInstance<GameTile>();
                tile.sprite = loadedSprite;
                tile.color = Color.white;
                tile.terrainType = terrain;
                tile.isPassable = passable;
                tile.moveCost = passable ? 1f : 0f;
                tile.flags = passable ? GameTileFlags.Passable : GameTileFlags.None;
                Debug.Log($"[TileMapController] Автосоздан GameTile: {spriteName} (passable={passable})");
                return tile;
            }

            Debug.LogWarning($"[TileMapController] Не удалось создать тайл: {spriteName}");
            return null;
        }

        /// <summary>
        /// Загрузить спрайт из Assets/Sprites/.
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private Sprite LoadTileSprite(string spriteName)
        {
#if UNITY_EDITOR
            string[] searchPaths = new string[]
            {
                $"Assets/Sprites/Tiles_AI/{spriteName}.png",
                $"Assets/Sprites/Tiles/{spriteName}.png"
            };
            foreach (var path in searchPaths)
            {
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) return sprite;
            }
#else
            // В build версии — загрузка из Resources
            var resSprite = UnityEngine.Resources.Load<Sprite>($"Sprites/{spriteName}");
            if (resSprite != null) return resSprite;
#endif
            return null;
        }

        /// <summary>
        /// Создать процедурный спрайт тайла (fallback при отсутствии файла).
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private Sprite CreateProceduralTileSprite(string spriteName, TerrainType terrain)
        {
            bool isObject = spriteName.StartsWith("obj_");

            // Terrain: 68×68, PPU=32 → 2.125 юнита (pixel bleed устраняет белую сетку)
            // Objects: 64×64, PPU=160 → 0.4 юнита (в 5 раз меньше ячейки)
            // Редактировано: 2026-04-16 UTC
            int texSize = isObject ? 64 : 68;
            int ppu = isObject ? 160 : 32;

            Texture2D texture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Color color = GetTerrainColor(terrain);

            for (int x = 0; x < texSize; x++)
            {
                for (int y = 0; y < texSize; y++)
                {
                    if (isObject)
                    {
                        // Объектные спрайты — прозрачный фон
                        texture.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        // Тайловые спрайты — сплошная заливка (pixel bleed)
                        float variation = UnityEngine.Random.Range(0.95f, 1.05f);
                        Color pixelColor = color * variation;
                        pixelColor.a = color.a;
                        texture.SetPixel(x, y, pixelColor);
                    }
                }
            }

            // Для объектных спрайтов — нарисовать простую форму (уменьшенную)
            if (isObject)
            {
                int cx = texSize / 2;
                int cy = texSize / 2;
                if (spriteName.Contains("tree"))
                {
                    DrawRectOnTexture(texture, cx - 3, 8, 6, 24, new Color(0.4f, 0.25f, 0.15f));
                    for (int y = 24; y < 56; y++)
                    {
                        int width = (56 - y) / 3;
                        DrawRectOnTexture(texture, cx - width, y, width * 2, 1, color);
                    }
                }
                else if (spriteName.Contains("rock"))
                {
                    DrawEllipseOnTexture(texture, cx, cy - 2, 8, 6, color);
                    DrawEllipseOnTexture(texture, cx, cy - 2, 6, 4, color * 1.1f);
                }
                else if (spriteName.Contains("ore"))
                {
                    DrawEllipseOnTexture(texture, cx, cy - 3, 10, 8, new Color(0.45f, 0.4f, 0.35f));
                    DrawEllipseOnTexture(texture, cx - 3, cy - 2, 4, 3, new Color(0.8f, 0.6f, 0.2f));
                }
                else if (spriteName.Contains("bush"))
                {
                    DrawEllipseOnTexture(texture, cx - 5, cy, 7, 8, color);
                    DrawEllipseOnTexture(texture, cx + 5, cy, 7, 8, color * 0.9f);
                    DrawEllipseOnTexture(texture, cx, cy + 3, 10, 7, color * 1.1f);
                }
                else if (spriteName.Contains("chest"))
                {
                    DrawRectOnTexture(texture, cx - 8, cy - 6, 16, 12, color);
                    DrawRectOnTexture(texture, cx - 9, cy + 4, 18, 4, color * 1.2f);
                }
                else if (spriteName.Contains("herb"))
                {
                    DrawRectOnTexture(texture, cx - 1, 10, 2, 20, new Color(0.2f, 0.4f, 0.15f));
                    DrawEllipseOnTexture(texture, cx, cy + 5, 6, 6, color);
                }
            }

            texture.Apply();

            // Terrain: полный rect (0,0,68,68) при PPU=32 → 2.125u (перекрытие ячейки 2.0u)
            // Objects: полный rect (0,0,64,64) при PPU=160 → 0.4u (в 5 раз меньше ячейки)
            return Sprite.Create(texture, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), ppu);
        }

        /// <summary>
        /// Получить цвет для типа террейна (для процедурных спрайтов).
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private Color GetTerrainColor(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Grass => new Color(0.4f, 0.7f, 0.3f),
                TerrainType.Dirt => new Color(0.6f, 0.4f, 0.2f),
                TerrainType.Stone => new Color(0.5f, 0.5f, 0.55f),
                TerrainType.Water_Shallow => new Color(0.3f, 0.5f, 0.8f, 0.8f),
                TerrainType.Water_Deep => new Color(0.2f, 0.3f, 0.7f, 0.9f),
                TerrainType.Sand => new Color(0.9f, 0.85f, 0.6f),
                TerrainType.Void => new Color(0.1f, 0.1f, 0.1f),
                TerrainType.Snow => new Color(0.95f, 0.95f, 1f),
                TerrainType.Ice => new Color(0.7f, 0.85f, 0.95f),
                TerrainType.Lava => new Color(0.9f, 0.3f, 0.05f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }

        /// <summary>
        /// Нарисовать прямоугольник на текстуре (для процедурных спрайтов).
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private void DrawRectOnTexture(Texture2D texture, int x, int y, int w, int h, Color color)
        {
            for (int px = x; px < x + w && px < texture.width; px++)
            {
                for (int py = y; py < y + h && py < texture.height; py++)
                {
                    if (px >= 0 && py >= 0) texture.SetPixel(px, py, color);
                }
            }
        }

        /// <summary>
        /// Нарисовать эллипс на текстуре (для процедурных спрайтов).
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private void DrawEllipseOnTexture(Texture2D texture, int cx, int cy, int rx, int ry, Color color)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                for (int y = cy - ry; y <= cy + ry; y++)
                {
                    float dx = (x - cx) / (float)rx;
                    float dy = (y - cy) / (float)ry;
                    if (dx * dx + dy * dy <= 1f && x >= 0 && y >= 0 && x < texture.width && y < texture.height)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        // === Map Generation ===

        /// <summary>
        /// Создать тестовую карту.
        /// </summary>
        [ContextMenu("Generate Test Map")]
        public void GenerateTestMap()
        {
            GenerateMap(defaultWidth, defaultHeight, "Test Location");
        }

        /// <summary>
        /// Сгенерировать карту.
        /// </summary>
        public void GenerateMap(int width, int height, string name = "Location")
        {
            mapData = new TileMapData(width, height, name);
            
            // Заполнить базовой поверхностью
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    mapData.CreateTile(x, y, TerrainType.Grass);
                }
            }

            // Добавить тестовые объекты
            AddTestFeatures();

            // Отрисовать карту
            RenderMap();

            OnMapGenerated?.Invoke(mapData);
            Debug.Log($"Generated map: {width}x{height} tiles ({width * 2}x{height * 2} meters)");
        }

        /// <summary>
        /// Добавить тестовые элементы на карту.
        /// Процедурная генерация с биомами, реками, зонами Ци.
        /// Редактировано: 2026-04-14 08:00:00 UTC — полная переработка для карты 100×80
        /// </summary>
        private void AddTestFeatures()
        {
            var random = new System.Random(mapData.seed);
            int width = mapData.width;
            int height = mapData.height;
            int centerX = width / 2;
            int centerY = height / 2;

            // === БИОМ 1: Песчаный берег (левый нижний угол, 15×10) ===
            GenerateBiomeRect(0, 0, 15, 10, TerrainType.Sand, random);

            // === БИОМ 2: Пустыня (правый нижний угол, 20×12) ===
            GenerateBiomeRect(width - 20, 0, 20, 12, TerrainType.Sand, random);
            // Песчаные дюны — вариация высот через случайные камни
            for (int i = 0; i < 15; i++)
            {
                int x = random.Next(width - 18, width - 2);
                int y = random.Next(2, 10);
                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.terrain == TerrainType.Sand && tile.objects.Count == 0)
                {
                    tile.AddObject(new TileObjectData(TileObjectType.Rock_Small));
                }
            }

            // === БИОМ 3: Каменные холмы (верхний левый угол, 18×15) ===
            GenerateBiomeEllipse(9, height - 8, 9, 8, TerrainType.Stone, random);

            // === БИОМ 4: Горный хребет (верхний правый угол) ===
            GenerateBiomeEllipse(width - 12, height - 10, 10, 10, TerrainType.Stone, random);
            // Снежные пики — снежный биом вокруг гор (шире чем ледяные вершины)
            // Редактировано: 2026-04-14 14:14:00 UTC
            GenerateBiomeEllipse(width - 12, height - 8, 7, 6, TerrainType.Snow, random);
            // Ледяные вершины (самый центр горного хребта)
            GenerateBiomeEllipse(width - 12, height - 6, 3, 3, TerrainType.Ice, random);

            // === БИОМ 5: Снежная тундра (верхний левый угол, рядом с каменными холмами) ===
            // Редактировано: 2026-04-14 14:14:00 UTC
            GenerateBiomeEllipse(15, height - 12, 8, 7, TerrainType.Snow, random);
            // Ледяное озеро в тундре
            GenerateBiomeEllipse(15, height - 14, 3, 2, TerrainType.Ice, random);

            // === ОЗЕРО 1: Левый нижний (рядом с песком) ===
            GenerateLake(10, 10, 5, 4, random);

            // === ОЗЕРО 2: Правый верхний ===
            GenerateLake(width - 15, height - 10, 5, 4, random);

            // === ОЗЕРО 3: Центральное (малое) ===
            GenerateLake(centerX + 15, centerY - 5, 3, 3, random);

            // === РЕКА: Вертикальная от верхнего озера вниз ===
            GenerateRiver(width - 15, height - 14, width - 15, height / 2, 2, random);

            // === РЕКА 2: Горизонтальная от левого озера ===
            GenerateRiver(15, 10, width / 3, 10, 2, random);

            // === ЛАВОВОЕ ОЗЕРО (нижний центр) ===
            GenerateLavaLake(centerX, 7, 6, 4, random);

            // === ЗОНА 5: Земляные дороги ===
            // Главная дорога: горизонталь через центр
            for (int x = 3; x < width - 3; x++)
            {
                for (int offset = 0; offset < 2; offset++)
                {
                    var tile = mapData.GetTile(x, centerY + offset);
                    if (tile != null && tile.terrain == TerrainType.Grass)
                    {
                        tile.terrain = TerrainType.Dirt;
                        tile.UpdateTerrainProperties();
                    }
                }
            }

            // Вертикальная дорога через центр
            for (int y = 3; y < height - 3; y++)
            {
                for (int offset = 0; offset < 2; offset++)
                {
                    var tile = mapData.GetTile(centerX + offset, y);
                    if (tile != null && tile.terrain == TerrainType.Grass)
                    {
                        tile.terrain = TerrainType.Dirt;
                        tile.UpdateTerrainProperties();
                    }
                }
            }

            // === ЗОНА 6: Каменная площадка в центре (алтарь) ===
            for (int x = centerX - 5; x <= centerX + 5; x++)
            {
                for (int y = centerY - 5; y <= centerY + 5; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile != null)
                    {
                        tile.terrain = TerrainType.Stone;
                        tile.UpdateTerrainProperties();
                    }
                }
            }

            // Алтарь — высокая плотность Ци
            var altarTile = mapData.GetTile(centerX, centerY);
            if (altarTile != null)
            {
                altarTile.baseQiDensity = 500;
                altarTile.currentQiDensity = 500;
            }

            // Кольцо алтаря — средняя плотность Ци
            for (int x = centerX - 4; x <= centerX + 4; x++)
            {
                for (int y = centerY - 4; y <= centerY + 4; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile != null && !(x == centerX && y == centerY))
                    {
                        tile.baseQiDensity = 200;
                        tile.currentQiDensity = 200;
                    }
                }
            }

            // === ЗОНА 7: Зона медитации (восток от центра) ===
            GenerateQiZone(centerX + 20, centerY + 5, 4, 150, random);

            // === ЗОНА 8: Зона медитации (запад от центра) ===
            GenerateQiZone(centerX - 25, centerY - 3, 3, 120, random);

            // === РАСТИТЕЛЬНОСТЬ: Густой лес (левая треть карты) ===
            for (int i = 0; i < 150; i++)
            {
                int x = random.Next(3, width / 3);
                int y = random.Next(3, height - 3);

                if (IsInSpecialZone(x, y, centerX, centerY)) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var treeObj = new TileObjectData(
                        random.Next(3) == 0 ? TileObjectType.Tree_Pine :
                        random.Next(3) == 0 ? TileObjectType.Tree_Birch :
                        TileObjectType.Tree_Oak
                    );
                    tile.AddObject(treeObj);
                }
            }

            // Средний лес (центральная треть)
            for (int i = 0; i < 80; i++)
            {
                int x = random.Next(width / 3, 2 * width / 3);
                int y = random.Next(3, height - 3);

                if (IsInSpecialZone(x, y, centerX, centerY)) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var treeObj = new TileObjectData(
                        random.Next(4) == 0 ? TileObjectType.Tree_Pine : TileObjectType.Tree_Oak
                    );
                    tile.AddObject(treeObj);
                }
            }

            // Редкий лес (правая треть — степь)
            for (int i = 0; i < 40; i++)
            {
                int x = random.Next(2 * width / 3, width - 3);
                int y = random.Next(3, height - 3);

                if (IsInSpecialZone(x, y, centerX, centerY)) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var treeObj = new TileObjectData(TileObjectType.Tree_Oak);
                    tile.AddObject(treeObj);
                }
            }

            // === КУСТЫ (разбросаны по карте) ===
            for (int i = 0; i < 70; i++)
            {
                int x = random.Next(3, width - 3);
                int y = random.Next(3, height - 3);

                if (IsInSpecialZone(x, y, centerX, centerY)) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.objects.Count == 0 && tile.IsPassable())
                {
                    var bushObj = new TileObjectData(
                        random.Next(4) == 0 ? TileObjectType.Bush_Berry : TileObjectType.Bush
                    );
                    tile.AddObject(bushObj);
                }
            }

            // === КАМНИ (разбросаны по карте, больше в каменных зонах) ===
            for (int i = 0; i < 45; i++)
            {
                int x = random.Next(3, width - 3);
                int y = random.Next(3, height - 3);

                if (IsInSpecialZone(x, y, centerX, centerY)) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var rockObj = new TileObjectData(
                        random.Next(3) == 0 ? TileObjectType.Rock_Medium : TileObjectType.Rock_Small
                    );
                    tile.AddObject(rockObj);
                }
            }

            // === РУДНЫЕ ЖИЛЫ (рядом с каменными зонами) ===
            for (int i = 0; i < 20; i++)
            {
                int x = random.Next(0, width);
                int y = random.Next(0, height);

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.terrain == TerrainType.Stone && tile.objects.Count == 0)
                {
                    var oreObj = new TileObjectData(TileObjectType.OreVein);
                    tile.AddObject(oreObj);
                }
            }

            // === СУНДУКИ (редкие, на проходимых тайлах) ===
            for (int i = 0; i < 10; i++)
            {
                int x = random.Next(5, width - 5);
                int y = random.Next(5, height - 5);

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var chestObj = new TileObjectData(TileObjectType.Chest);
                    tile.AddObject(chestObj);
                }
            }

            // === ТРАВЫ И ЦВЕТЫ (по всей карте) ===
            for (int i = 0; i < 60; i++)
            {
                int x = random.Next(3, width - 3);
                int y = random.Next(3, height - 3);

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.objects.Count == 0 && tile.terrain == TerrainType.Grass)
                {
                    var herbObj = new TileObjectData(
                        random.Next(3) == 0 ? TileObjectType.Herb :
                        random.Next(2) == 0 ? TileObjectType.Flower :
                        TileObjectType.Grass_Tall
                    );
                    tile.AddObject(herbObj);
                }
            }

            // === ЗИМНИЕ ТРАВЫ (в снежных биомах) ===
            // Редактировано: 2026-04-14 14:14:00 UTC
            for (int i = 0; i < 25; i++)
            {
                int x = random.Next(3, width - 3);
                int y = random.Next(height / 2, height - 3);

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.objects.Count == 0 && tile.terrain == TerrainType.Snow)
                {
                    var herbObj = new TileObjectData(TileObjectType.Herb);
                    tile.AddObject(herbObj);
                }
            }

            // === ГРАНИЦА КАРТЫ — Void обрывы ===
            for (int x = 0; x < width; x++)
            {
                SetVoidIfGrass(x, 0);
                SetVoidIfGrass(x, 1);
                SetVoidIfGrass(x, height - 1);
                SetVoidIfGrass(x, height - 2);
            }
            for (int y = 0; y < height; y++)
            {
                SetVoidIfGrass(0, y);
                SetVoidIfGrass(1, y);
                SetVoidIfGrass(width - 1, y);
                SetVoidIfGrass(width - 2, y);
            }
        }

        /// <summary>
        /// Сгенерировать биом прямоугольником.
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateBiomeRect(int startX, int startY, int w, int h, TerrainType terrain, System.Random random)
        {
            for (int x = startX; x < startX + w && x < mapData.width; x++)
            {
                for (int y = startY; y < startY + h && y < mapData.height; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile != null)
                    {
                        tile.terrain = terrain;
                        tile.UpdateTerrainProperties();
                    }
                }
            }
        }

        /// <summary>
        /// Сгенерировать биом эллипсом.
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateBiomeEllipse(float cx, float cy, float rx, float ry, TerrainType terrain, System.Random random)
        {
            for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
            {
                for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile != null)
                    {
                        float dx = (x - cx) / rx;
                        float dy = (y - cy) / ry;
                        if (dx * dx + dy * dy <= 1f)
                        {
                            tile.terrain = terrain;
                            tile.UpdateTerrainProperties();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Сгенерировать озеро (мелкая + глубокая вода).
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateLake(float cx, float cy, float rx, float ry, System.Random random)
        {
            for (int x = (int)(cx - rx - 1); x <= (int)(cx + rx + 1); x++)
            {
                for (int y = (int)(cy - ry - 1); y <= (int)(cy + ry + 1); y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile == null) continue;

                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    float dist = dx * dx + dy * dy;

                    if (dist <= 0.55f)
                    {
                        tile.terrain = TerrainType.Water_Deep;
                        tile.UpdateTerrainProperties();
                    }
                    else if (dist <= 1f)
                    {
                        tile.terrain = TerrainType.Water_Shallow;
                        tile.UpdateTerrainProperties();
                    }
                }
            }
        }

        /// <summary>
        /// Сгенерировать лавовое озеро.
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateLavaLake(float cx, float cy, float rx, float ry, System.Random random)
        {
            for (int x = (int)(cx - rx - 1); x <= (int)(cx + rx + 1); x++)
            {
                for (int y = (int)(cy - ry - 1); y <= (int)(cy + ry + 1); y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile == null) continue;

                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    float dist = dx * dx + dy * dy;

                    if (dist <= 0.35f)
                    {
                        tile.terrain = TerrainType.Lava;
                        tile.UpdateTerrainProperties();
                    }
                    else if (dist <= 0.7f)
                    {
                        tile.terrain = TerrainType.Stone;
                        tile.UpdateTerrainProperties();
                    }
                }
            }
        }

        /// <summary>
        /// Сгенерировать реку от (x1,y1) до (x2,y2) шириной width тайлов.
        /// Простая прямая река.
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateRiver(int x1, int y1, int x2, int y2, int riverWidth, System.Random random)
        {
            // Bresenham-подобная линия
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int steps = Math.Max(dx, dy);

            if (steps == 0) return;

            float stepX = (float)(x2 - x1) / steps;
            float stepY = (float)(y2 - y1) / steps;

            for (int i = 0; i <= steps; i++)
            {
                int cx = (int)(x1 + stepX * i);
                int cy = (int)(y1 + stepY * i);

                for (int w = 0; w < riverWidth; w++)
                {
                    // Перпендикуляр к направлению реки
                    int px = -((int)stepY) * w / Math.Max(1, (int)(Math.Abs(stepX) + Math.Abs(stepY)));
                    int py = ((int)stepX) * w / Math.Max(1, (int)(Math.Abs(stepX) + Math.Abs(stepY)));

                    var tile = mapData.GetTile(cx + px, cy + py);
                    if (tile != null && tile.terrain != TerrainType.Stone && tile.terrain != TerrainType.Lava)
                    {
                        tile.terrain = w == 0 ? TerrainType.Water_Shallow : TerrainType.Water_Shallow;
                        tile.UpdateTerrainProperties();
                    }
                }
            }
        }

        /// <summary>
        /// Сгенерировать зону высокой плотности Ци.
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateQiZone(int cx, int cy, int radius, int qiDensity, System.Random random)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile == null) continue;

                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist <= radius)
                    {
                        // Плотность убывает к краю зоны
                        float falloff = 1f - (dist / radius);
                        tile.baseQiDensity = (int)(qiDensity * falloff);
                        tile.currentQiDensity = tile.baseQiDensity;
                    }
                }
            }
        }

        /// <summary>
        /// Установить Void если текущий тип — Grass (для границ карты).
        /// </summary>
        private void SetVoidIfGrass(int x, int y)
        {
            var tile = mapData?.GetTile(x, y);
            if (tile != null && tile.terrain == TerrainType.Grass)
            {
                tile.terrain = TerrainType.Void;
                tile.UpdateTerrainProperties();
            }
        }

        /// <summary>
        /// Проверить, находится ли тайл в специальной зоне (озёра, алтарь, лавовое озеро).
        /// Редактировано: 2026-04-14 08:00:00 UTC — обновлено для карты 100×80
        /// </summary>
        private bool IsInSpecialZone(int x, int y, int centerX, int centerY)
        {
            // Центральная каменная площадка (алтарь)
            if (Mathf.Abs(x - centerX) <= 6 && Mathf.Abs(y - centerY) <= 6)
                return true;

            // Озеро 1: левый нижний
            if (x >= 5 && x < 16 && y >= 5 && y < 15)
            {
                float dx = (x - 10f) / 5f;
                float dy = (y - 10f) / 4f;
                if (dx * dx + dy * dy <= 1.2f)
                    return true;
            }

            // Озеро 2: правый верхний
            if (x >= mapData.width - 21 && x < mapData.width - 9 && y >= mapData.height - 15 && y < mapData.height - 5)
            {
                float dx = (x - (mapData.width - 15f)) / 5f;
                float dy = (y - (mapData.height - 10f)) / 4f;
                if (dx * dx + dy * dy <= 1.2f)
                    return true;
            }

            // Озеро 3: центральное
            if (Mathf.Abs(x - (centerX + 15)) <= 4 && Mathf.Abs(y - (centerY - 5)) <= 4)
            {
                float dx = (x - (centerX + 15f)) / 3f;
                float dy = (y - (centerY - 5f)) / 3f;
                if (dx * dx + dy * dy <= 1.2f)
                    return true;
            }

            // Лавовое озеро
            if (Mathf.Abs(x - centerX) <= 7 && y >= 3 && y < 12)
                return true;

            // Песчаный берег (левый нижний)
            if (x < 15 && y < 10)
                return true;

            // Пустыня (правый нижний)
            if (x >= mapData.width - 20 && y < 12)
                return true;

            return false;
        }

        // === Rendering ===

        /// <summary>
        /// Отрисовать карту на Tilemap.
        /// </summary>
        public void RenderMap()
        {
            if (mapData == null || terrainTilemap == null) return;

            // Очистить
            terrainTilemap.ClearAllTiles();
            if (objectTilemap != null) objectTilemap.ClearAllTiles();

            // Отрисовать поверхность
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile == null) continue;

                    // Поверхность
                    TileBase terrainTile = GetTerrainTile(tile.terrain);
                    if (terrainTile != null)
                    {
                        terrainTilemap.SetTile(new Vector3Int(x, y, 0), terrainTile);
                    }

                    // Объекты
                    if (objectTilemap != null && tile.objects.Count > 0)
                    {
                        TileBase objTile = GetObjectTile(tile.objects[0].objectType);
                        if (objTile != null)
                        {
                            objectTilemap.SetTile(new Vector3Int(x, y, 0), objTile);
                        }
                    }
                }
            }

            // Обновить коллайдеры
            terrainTilemap.RefreshAllTiles();
            if (objectTilemap != null) objectTilemap.RefreshAllTiles();
        }

        /// <summary>
        /// Получить Tile для типа поверхности.
        /// </summary>
        private TileBase GetTerrainTile(TerrainType type)
        {
            return type switch
            {
                TerrainType.Grass => grassTile,
                TerrainType.Dirt => dirtTile,
                TerrainType.Stone => stoneTile,
                TerrainType.Water_Shallow => waterShallowTile,
                TerrainType.Water_Deep => waterDeepTile,
                TerrainType.Sand => sandTile,
                TerrainType.Void => voidTile,
                // FIX: Добавлены Snow, Ice и Lava
                // Редактировано: 2026-04-14 13:45:00 UTC
                TerrainType.Snow => snowTile,
                TerrainType.Ice => iceTile,
                TerrainType.Lava => lavaTile,
                _ => null
            };
        }

        /// <summary>
        /// Получить Tile для типа объекта.
        /// Редактировано: 2026-04-13 13:35:27 UTC — добавлены OreVein, Herb, Bush_Berry, Grass_Tall
        /// </summary>
        private TileBase GetObjectTile(TileObjectType type)
        {
            return type switch
            {
                TileObjectType.Tree_Oak => treeTile,
                TileObjectType.Tree_Pine => treeTile,
                TileObjectType.Tree_Birch => treeTile,
                TileObjectType.Rock_Small => rockSmallTile,
                TileObjectType.Rock_Medium => rockMediumTile,
                TileObjectType.Rock_Large => rockMediumTile,
                TileObjectType.Bush => bushTile,
                TileObjectType.Bush_Berry => bushTile,
                TileObjectType.Chest => chestTile,
                // FIX: OreVein и Herb теперь используют собственные tile поля
                // Редактировано: 2026-04-14 06:38:00 UTC
                TileObjectType.OreVein => oreVeinTile ?? rockMediumTile,
                TileObjectType.Herb => herbTile ?? bushTile,
                TileObjectType.Grass_Tall => herbTile ?? bushTile,
                TileObjectType.Flower => herbTile ?? bushTile,
                _ => null
            };
        }

        // === Tile Access ===

        /// <summary>
        /// Получить тайл по мировым координатам.
        /// </summary>
        public TileData GetTileAtWorld(Vector2 worldPos)
        {
            if (mapData == null) return null;
            var tilePos = mapData.WorldToTile(worldPos);
            return mapData.GetTile(tilePos.x, tilePos.y);
        }

        /// <summary>
        /// Получить тайл по координатам тайла.
        /// </summary>
        public TileData GetTile(int x, int y)
        {
            return mapData?.GetTile(x, y);
        }

        /// <summary>
        /// Изменить тип поверхности тайла.
        /// </summary>
        public void SetTerrain(int x, int y, TerrainType terrain)
        {
            var tile = mapData?.GetTile(x, y);
            if (tile == null) return;

            tile.terrain = terrain;
            tile.UpdateTerrainProperties();

            // Обновить отображение
            TileBase terrainTile = GetTerrainTile(terrain);
            if (terrainTile != null && terrainTilemap != null)
            {
                terrainTilemap.SetTile(new Vector3Int(x, y, 0), terrainTile);
            }

            OnTileChanged?.Invoke(tile);
        }

        /// <summary>
        /// Добавить объект на тайл.
        /// </summary>
        public void AddObject(int x, int y, TileObjectType objType)
        {
            var tile = mapData?.GetTile(x, y);
            if (tile == null) return;

            var obj = new TileObjectData(objType);
            tile.AddObject(obj);

            // Обновить отображение
            TileBase objTile = GetObjectTile(objType);
            if (objTile != null && objectTilemap != null)
            {
                objectTilemap.SetTile(new Vector3Int(x, y, 0), objTile);
            }

            OnTileChanged?.Invoke(tile);
        }

        /// <summary>
        /// Удалить объект с тайла.
        /// </summary>
        public void RemoveObject(int x, int y, TileObjectData obj)
        {
            var tile = mapData?.GetTile(x, y);
            if (tile == null) return;

            tile.RemoveObject(obj);

            // Обновить отображение
            if (objectTilemap != null)
            {
                objectTilemap.SetTile(new Vector3Int(x, y, 0), null);
            }

            OnTileChanged?.Invoke(tile);
        }

        // === Pathfinding Helpers ===

        /// <summary>
        /// Получить соседние тайлы.
        /// </summary>
        public List<TileData> GetNeighbors(int x, int y, bool includeDiagonals = false)
        {
            var neighbors = new List<TileData>();
            
            // 4-directional
            var dirs = new (int dx, int dy)[]
            {
                (0, 1), (0, -1), (1, 0), (-1, 0)
            };

            // Add diagonals if requested
            if (includeDiagonals)
            {
                dirs = new (int dx, int dy)[]
                {
                    (0, 1), (0, -1), (1, 0), (-1, 0),
                    (1, 1), (1, -1), (-1, 1), (-1, -1)
                };
            }

            foreach (var (dx, dy) in dirs)
            {
                var tile = mapData?.GetTile(x + dx, y + dy);
                if (tile != null)
                    neighbors.Add(tile);
            }

            return neighbors;
        }

        // === Gizmos ===

        private void OnDrawGizmosSelected()
        {
            if (mapData == null) return;

            // Рисовать границы карты
            Gizmos.color = Color.yellow;
            Vector3 size = new Vector3(mapData.width * 2, mapData.height * 2, 0);
            Vector3 center = size / 2;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
