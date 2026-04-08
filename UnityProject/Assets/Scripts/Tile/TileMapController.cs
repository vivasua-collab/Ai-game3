// ============================================================================
// TileMapController.cs — Контроллер карты тайлов
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-08 06:17:46 UTC
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

        [Header("Object Tiles")]
        [SerializeField] private TileBase treeTile;
        [SerializeField] private TileBase rockSmallTile;
        [SerializeField] private TileBase rockMediumTile;
        [SerializeField] private TileBase bushTile;
        [SerializeField] private TileBase chestTile;

        [Header("Settings")]
        [SerializeField] private int defaultWidth = 50;
        [SerializeField] private int defaultHeight = 50;
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
        }

        private void Start()
        {
            if (generateOnStart)
            {
                GenerateTestMap();
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
        /// </summary>
        private void AddTestFeatures()
        {
            var random = new System.Random(mapData.seed);
            int width = mapData.width;
            int height = mapData.height;

            // Пруд в углу
            for (int x = 5; x < 12; x++)
            {
                for (int y = 5; y < 10; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile != null)
                    {
                        tile.terrain = (x == 5 || x == 11 || y == 5 || y == 9) 
                            ? TerrainType.Water_Shallow 
                            : TerrainType.Water_Deep;
                        tile.UpdateTerrainProperties();
                    }
                }
            }

            // Каменная площадка в центре
            int centerX = width / 2;
            int centerY = height / 2;
            for (int x = centerX - 3; x <= centerX + 3; x++)
            {
                for (int y = centerY - 3; y <= centerY + 3; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile != null)
                    {
                        tile.terrain = TerrainType.Stone;
                        tile.UpdateTerrainProperties();
                    }
                }
            }

            // Деревья по периметру (кроме площадки)
            for (int i = 0; i < 30; i++)
            {
                int x = random.Next(0, width);
                int y = random.Next(0, height);
                
                // Избегать центра и воды
                if (Mathf.Abs(x - centerX) <= 4 && Mathf.Abs(y - centerY) <= 4) continue;
                if (x >= 5 && x < 12 && y >= 5 && y < 10) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var treeObj = new TileObjectData(TileObjectType.Tree_Oak);
                    tile.AddObject(treeObj);
                }
            }

            // Камни
            for (int i = 0; i < 15; i++)
            {
                int x = random.Next(0, width);
                int y = random.Next(0, height);
                
                if (x >= 5 && x < 12 && y >= 5 && y < 10) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var rockObj = new TileObjectData(
                        random.Next(2) == 0 ? TileObjectType.Rock_Small : TileObjectType.Rock_Medium
                    );
                    tile.AddObject(rockObj);
                }
            }

            // Кусты
            for (int i = 0; i < 10; i++)
            {
                int x = random.Next(0, width);
                int y = random.Next(0, height);

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.objects.Count == 0)
                {
                    var bushObj = new TileObjectData(TileObjectType.Bush);
                    tile.AddObject(bushObj);
                }
            }
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
                _ => null
            };
        }

        /// <summary>
        /// Получить Tile для типа объекта.
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
                TileObjectType.Bush => bushTile,
                TileObjectType.Chest => chestTile,
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
