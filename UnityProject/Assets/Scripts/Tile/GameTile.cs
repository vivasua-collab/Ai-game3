// ============================================================================
// GameTile.cs — Пользовательский тайл для Tilemap
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-11 14:06:35 UTC — FIX CS0115: ITilemap→Tilemap (каскадная причина 7 ошибок)
// ============================================================================
//
// ВАЖНО: История исправлений GetTileData:
// 1. Оригинал: ITilemap → CS0115 (no suitable method found to override)
// 2. Каскад: CS0115 → 7 ошибок CS0234/CS0246 в ResourcePickup.cs и DestructibleObjectController.cs
// 3. Решение: Tilemap — подтверждено рабочим для Unity 6000.3 + com.unity.2d.tilemap 1.0.0
// 4. ITilemap доступен только в более новых версиях 2D Tilemap пакета
// ============================================================================

using UnityEngine;
using UnityEngine.Tilemaps;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Пользовательский тайл с дополнительными данными.
    /// </summary>
    [CreateAssetMenu(fileName = "NewGameTile", menuName = "Cultivation/GameTile")]
    public class GameTile : TileBase
    {
        [Header("Visuals")]
        public Sprite sprite;
        public Color color = Color.white;

        [Header("Properties")]
        public TerrainType terrainType = TerrainType.Grass;
        public TileObjectCategory objectCategory = TileObjectCategory.None;
        public TileObjectType objectType = TileObjectType.None;

        [Header("Movement")]
        public float moveCost = 1f;
        public bool isPassable = true;

        [Header("Flags")]
        public GameTileFlags flags = GameTileFlags.Passable;

        // FIX CS0115: Tilemap вместо ITilemap для com.unity.2d.tilemap 1.0.0
        // Полная квалификация UnityEngine.Tilemaps.TileData — конфликт с CultivationGame.TileSystem.TileData
        // Редактировано: 2026-04-11 14:06:35 UTC
        public override void GetTileData(Vector3Int position, Tilemap tilemap, ref UnityEngine.Tilemaps.TileData tileData)
        {
            tileData.sprite = sprite;
            tileData.color = color;
            // Используем UnityEngine.Tilemaps.TileFlags для tileData.flags
            tileData.flags = UnityEngine.Tilemaps.TileFlags.None;
        }
    }

    /// <summary>
    /// Тайл поверхности.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTerrainTile", menuName = "Cultivation/TerrainTile")]
    public class TerrainTile : GameTile
    {
        private void OnEnable()
        {
            objectCategory = TileObjectCategory.None;
            objectType = TileObjectType.None;
        }
    }

    /// <summary>
    /// Тайл объекта.
    /// </summary>
    [CreateAssetMenu(fileName = "NewObjectTile", menuName = "Cultivation/ObjectTile")]
    public class ObjectTile : GameTile
    {
        [Header("Object Properties")]
        public int width = 1;
        public int height = 1;
        public int durability = 100;
        public bool blocksVision = true;
        public bool providesCover = true;
        public bool isInteractable = false;
        public bool isHarvestable = false;

        private void OnEnable()
        {
            isPassable = false;
            flags = GameTileFlags.None;
        }
    }
}
