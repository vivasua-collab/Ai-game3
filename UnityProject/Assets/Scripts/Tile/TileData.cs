// ============================================================================
// TileData.cs — Данные тайла
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-09 07:12:00 UTC — TileFlags → GameTileFlags
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Данные одного тайла (2×2 м).
    /// Содержит все слои: поверхность, объекты, субъекты.
    /// </summary>
    [Serializable]
    public class TileData
    {
        // === Координаты ===
        public int x;
        public int y;
        public int z; // Уровень высоты (-5..+5)

        // === Поверхность (Слой 2) ===
        public TerrainType terrain = TerrainType.Grass;
        public float moveCost = 1.0f; // Множитель стоимости движения

        // === Объекты (Слой 3) ===
        public List<TileObjectData> objects = new List<TileObjectData>();
        
        // === Параметры окружения ===
        public int baseQiDensity = 100;     // Базовая плотность Ци
        public int currentQiDensity = 100;  // Текущая плотность (с модификаторами)
        public float temperature = 20f;     // Температура в °C
        
        // === Вода ===
        public bool hasWater;
        public float waterDepth;            // Глубина в метрах
        
        // === Флаги ===
        public GameTileFlags flags = GameTileFlags.Passable;

        // === Runtime ссылки ===
        [NonSerialized] public List<int> entityIds = new List<int>(); // ID субъектов на тайле

        // === Конструкторы ===
        public TileData() { }

        public TileData(int x, int y, TerrainType terrain = TerrainType.Grass)
        {
            this.x = x;
            this.y = y;
            this.z = 0;
            this.terrain = terrain;
            UpdateTerrainProperties();
        }

        // === Методы ===

        /// <summary>
        /// Обновить свойства на основе типа поверхности.
        /// </summary>
        public void UpdateTerrainProperties()
        {
            switch (terrain)
            {
                case TerrainType.None:
                case TerrainType.Void:
                    moveCost = 0f;
                    flags = GameTileFlags.None;
                    break;
                case TerrainType.Grass:
                case TerrainType.Dirt:
                case TerrainType.Stone:
                    moveCost = 1.0f;
                    flags = GameTileFlags.Passable;
                    break;
                case TerrainType.Water_Shallow:
                    moveCost = 2.0f;
                    flags = GameTileFlags.Passable | GameTileFlags.Swimable;
                    hasWater = true;
                    waterDepth = 0.5f;
                    break;
                case TerrainType.Water_Deep:
                    moveCost = 0f;
                    flags = GameTileFlags.Swimable | GameTileFlags.Flyable;
                    hasWater = true;
                    waterDepth = 3f;
                    break;
                case TerrainType.Sand:
                    moveCost = 1.2f;
                    flags = GameTileFlags.Passable;
                    break;
                case TerrainType.Snow:
                    moveCost = 1.5f;
                    flags = GameTileFlags.Passable;
                    break;
                case TerrainType.Ice:
                    moveCost = 1.5f;
                    flags = GameTileFlags.Passable | GameTileFlags.Dangerous;
                    break;
                case TerrainType.Lava:
                    moveCost = 0f;
                    flags = GameTileFlags.Dangerous | GameTileFlags.Flyable;
                    break;
            }
        }

        /// <summary>
        /// Проверить, можно ли пройти через тайл.
        /// </summary>
        public bool IsPassable(bool canSwim = false, bool canFly = false)
        {
            if ((flags & GameTileFlags.Passable) != 0) return true;
            if (canSwim && (flags & GameTileFlags.Swimable) != 0) return true;
            if (canFly && (flags & GameTileFlags.Flyable) != 0) return true;
            return false;
        }

        /// <summary>
        /// Проверить, блокирует ли тайл видимость.
        /// </summary>
        public bool BlocksVision()
        {
            return (flags & GameTileFlags.BlocksVision) != 0;
        }

        /// <summary>
        /// Добавить объект на тайл.
        /// </summary>
        public void AddObject(TileObjectData obj)
        {
            objects.Add(obj);
            UpdateObjectFlags(obj);
        }

        /// <summary>
        /// Удалить объект с тайла.
        /// </summary>
        public void RemoveObject(TileObjectData obj)
        {
            objects.Remove(obj);
            RecalculateFlags();
        }

        /// <summary>
        /// Обновить флаги на основе объекта.
        /// </summary>
        private void UpdateObjectFlags(TileObjectData obj)
        {
            if (!obj.isPassable)
            {
                flags &= ~GameTileFlags.Passable;
            }
            if (obj.blocksVision)
            {
                flags |= GameTileFlags.BlocksVision;
            }
            if (obj.providesCover)
            {
                flags |= GameTileFlags.ProvidesCover;
            }
            if (obj.isInteractable)
            {
                flags |= GameTileFlags.Interactable;
            }
        }

        /// <summary>
        /// Пересчитать все флаги.
        /// </summary>
        private void RecalculateFlags()
        {
            // Сбросить флаги объектов
            flags &= ~(GameTileFlags.BlocksVision | GameTileFlags.ProvidesCover | GameTileFlags.Interactable);
            
            // Применить заново
            foreach (var obj in objects)
            {
                UpdateObjectFlags(obj);
            }
        }

        /// <summary>
        /// Получить позицию в мировых координатах.
        /// Тайл = 2×2 м, центр тайла.
        /// </summary>
        public Vector2 GetWorldPosition()
        {
            return new Vector2(x * 2f + 1f, y * 2f + 1f);
        }

        /// <summary>
        /// Получить тайл из мировых координат.
        /// </summary>
        public static Vector2Int WorldToTile(Vector2 worldPos)
        {
            return new Vector2Int(Mathf.FloorToInt(worldPos.x / 2f), Mathf.FloorToInt(worldPos.y / 2f));
        }
    }

    /// <summary>
    /// Данные объекта на тайле.
    /// </summary>
    [Serializable]
    public class TileObjectData
    {
        public string objectId;
        public TileObjectType objectType;
        public TileObjectCategory category;
        
        // Размеры в тайлах
        public int width = 1;
        public int height = 1;
        
        // Свойства
        public bool isPassable = false;
        public bool blocksVision = true;
        public bool providesCover = true;
        public bool isInteractable = false;
        public bool isHarvestable = false;
        
        // Прочность
        public int maxDurability = 100;
        public int currentDurability = 100;
        
        // Ресурсы (если harvestable)
        public string resourceId;
        public int resourceCount;

        // === Конструкторы ===
        public TileObjectData() { }

        public TileObjectData(TileObjectType type)
        {
            objectType = type;
            objectId = System.Guid.NewGuid().ToString();
            SetDefaultProperties();
        }

        /// <summary>
        /// Установить свойства по умолчанию для типа объекта.
        /// </summary>
        private void SetDefaultProperties()
        {
            switch (objectType)
            {
                case TileObjectType.Tree_Oak:
                case TileObjectType.Tree_Pine:
                case TileObjectType.Tree_Birch:
                    category = TileObjectCategory.Vegetation;
                    isPassable = false;
                    blocksVision = true;
                    providesCover = true;
                    isHarvestable = true;
                    resourceId = "wood";
                    resourceCount = 10;
                    maxDurability = 200;
                    break;

                case TileObjectType.Bush:
                case TileObjectType.Bush_Berry:
                    category = TileObjectCategory.Vegetation;
                    isPassable = false;
                    blocksVision = false;
                    providesCover = true;
                    isHarvestable = objectType == TileObjectType.Bush_Berry;
                    resourceId = "berries";
                    resourceCount = 5;
                    maxDurability = 50;
                    break;

                case TileObjectType.Rock_Small:
                    category = TileObjectCategory.Rock;
                    isPassable = false;
                    blocksVision = false;
                    providesCover = true;
                    isHarvestable = true;
                    resourceId = "stone";
                    resourceCount = 3;
                    maxDurability = 100;
                    break;

                case TileObjectType.Rock_Medium:
                case TileObjectType.Rock_Large:
                    category = TileObjectCategory.Rock;
                    isPassable = false;
                    blocksVision = true;
                    providesCover = true;
                    isHarvestable = true;
                    resourceId = "stone";
                    resourceCount = 10;
                    maxDurability = 300;
                    width = objectType == TileObjectType.Rock_Large ? 2 : 1;
                    height = objectType == TileObjectType.Rock_Large ? 2 : 1;
                    break;

                case TileObjectType.Wall_Wood:
                case TileObjectType.Wall_Stone:
                    category = TileObjectCategory.Building;
                    isPassable = false;
                    blocksVision = true;
                    providesCover = true;
                    maxDurability = objectType == TileObjectType.Wall_Stone ? 500 : 200;
                    break;

                case TileObjectType.Door:
                    category = TileObjectCategory.Building;
                    isPassable = true;
                    blocksVision = false;
                    providesCover = false;
                    isInteractable = true;
                    maxDurability = 100;
                    break;

                case TileObjectType.Chest:
                    category = TileObjectCategory.Interactive;
                    isPassable = false;
                    blocksVision = false;
                    providesCover = false;
                    isInteractable = true;
                    maxDurability = 50;
                    break;

                case TileObjectType.Shrine:
                case TileObjectType.Altar:
                    category = TileObjectCategory.Interactive;
                    isPassable = true;
                    blocksVision = false;
                    providesCover = false;
                    isInteractable = true;
                    maxDurability = 1000;
                    break;

                case TileObjectType.OreVein:
                    category = TileObjectCategory.Rock;
                    isPassable = false;
                    blocksVision = false;
                    providesCover = true;
                    isHarvestable = true;
                    isInteractable = true;
                    resourceId = "ore";
                    resourceCount = 15;
                    maxDurability = 400;
                    break;

                case TileObjectType.Herb:
                    category = TileObjectCategory.Vegetation;
                    isPassable = true;
                    blocksVision = false;
                    providesCover = false;
                    isHarvestable = true;
                    resourceId = "herb";
                    resourceCount = 1;
                    maxDurability = 10;
                    break;

                default:
                    category = TileObjectCategory.Decoration;
                    isPassable = true;
                    break;
            }

            currentDurability = maxDurability;
        }
    }
}
