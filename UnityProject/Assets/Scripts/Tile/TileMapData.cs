// ============================================================================
// TileMapData.cs — Данные карты тайлов
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-12 00:00:00 UTC — Fix-12: DateTime→long generatedAtTicks for JsonUtility
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Данные карты тайлов для одной локации.
    /// Размер локации: динамический (100м - 10км).
    /// </summary>
    [Serializable]
    public class TileMapData
    {
        // === Идентификация ===
        public string mapId;
        public string mapName;
        
        // === Размеры ===
        public int width;   // В тайлах
        public int height;  // В тайлах
        public int tileSize = 2; // Размер тайла в метрах (2×2 м)
        
        // === Данные ===
        public TileData[] tiles;
        
        // === Метаданные ===
        public int seed;
        public string biome;
        
        // FIX TIL-C02: DateTime→long ticks for JsonUtility compatibility (2026-04-12)
        public long generatedAtTicks;
        
        /// <summary>
        /// Helper property to convert ticks to/from DateTime.
        /// JsonUtility cannot serialize DateTime directly.
        /// </summary>
        public DateTime GeneratedAt
        {
            get => new DateTime(generatedAtTicks, DateTimeKind.Utc);
            set => generatedAtTicks = value.Ticks;
        }

        // === Конструкторы ===
        public TileMapData() { }

        public TileMapData(int width, int height, string name = "Location")
        {
            this.mapId = Guid.NewGuid().ToString();
            this.mapName = name;
            this.width = width;
            this.height = height;
            this.seed = UnityEngine.Random.Range(0, int.MaxValue);
            this.generatedAtTicks = DateTime.UtcNow.Ticks; // FIX TIL-C02 (2026-04-12)
            
            // Создать массив тайлов
            tiles = new TileData[width * height];
        }

        // === Индексация ===

        /// <summary>
        /// Получить индекс тайла в массиве.
        /// </summary>
        private int GetIndex(int x, int y)
        {
            return y * width + x;
        }

        /// <summary>
        /// Проверить, находятся ли координаты в пределах карты.
        /// </summary>
        public bool InBounds(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        /// <summary>
        /// Получить тайл по координатам.
        /// </summary>
        public TileData GetTile(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return tiles[GetIndex(x, y)];
        }

        /// <summary>
        /// Установить тайл по координатам.
        /// </summary>
        public void SetTile(int x, int y, TileData tile)
        {
            if (!InBounds(x, y)) return;
            tiles[GetIndex(x, y)] = tile;
        }

        /// <summary>
        /// Создать тайл по координатам.
        /// </summary>
        public TileData CreateTile(int x, int y, TerrainType terrain = TerrainType.Grass)
        {
            var tile = new TileData(x, y, terrain);
            SetTile(x, y, tile);
            return tile;
        }

        // === Поиск ===

        /// <summary>
        /// Найти все тайлы определённого типа.
        /// </summary>
        public List<TileData> FindTiles(TerrainType terrain)
        {
            var result = new List<TileData>();
            foreach (var tile in tiles)
            {
                if (tile != null && tile.terrain == terrain)
                    result.Add(tile);
            }
            return result;
        }

        /// <summary>
        /// Найти все объекты определённого типа.
        /// </summary>
        public List<(TileData tile, TileObjectData obj)> FindObjects(TileObjectType objType)
        {
            var result = new List<(TileData, TileObjectData)>();
            foreach (var tile in tiles)
            {
                if (tile == null) continue;
                foreach (var obj in tile.objects)
                {
                    if (obj.objectType == objType)
                        result.Add((tile, obj));
                }
            }
            return result;
        }

        /// <summary>
        /// Найти проходимый тайл рядом с указанными координатами.
        /// </summary>
        public TileData FindPassableNearby(int centerX, int centerY, int radius = 5)
        {
            for (int r = 0; r <= radius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Mathf.Abs(dx) != r && Mathf.Abs(dy) != r) continue;
                        
                        var tile = GetTile(centerX + dx, centerY + dy);
                        if (tile != null && tile.IsPassable())
                            return tile;
                    }
                }
            }
            return null;
        }

        // === Преобразование координат ===

        /// <summary>
        /// Преобразовать мировые координаты в тайловые.
        /// </summary>
        public Vector2Int WorldToTile(Vector2 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / tileSize),
                Mathf.FloorToInt(worldPos.y / tileSize)
            );
        }

        /// <summary>
        /// Преобразовать тайловые координаты в мировые (центр тайла).
        /// </summary>
        public Vector2 TileToWorld(int x, int y)
        {
            return new Vector2(
                x * tileSize + tileSize * 0.5f,
                y * tileSize + tileSize * 0.5f
            );
        }

        /// <summary>
        /// Получить размер карты в метрах.
        /// </summary>
        public Vector2 GetWorldSize()
        {
            return new Vector2(width * tileSize, height * tileSize);
        }

        // === Сохранение/Загрузка ===

        /// <summary>
        /// Сериализовать карту в JSON.
        /// </summary>
        public string ToJson()
        {
            return JsonUtility.ToJson(this, true);
        }

        /// <summary>
        /// Десериализовать карту из JSON.
        /// </summary>
        public static TileMapData FromJson(string json)
        {
            return JsonUtility.FromJson<TileMapData>(json);
        }
    }
}
