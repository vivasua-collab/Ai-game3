// ============================================================================
// GameTile.cs — Пользовательский тайл для Tilemap
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-11 10:06:19 UTC — REVERT: ITilemap обратно (Tilemap вызывает CS0115 в Unity 6000.3)
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

        // FIX: Unity 6000.3 TileBase.GetTileData использует ITilemap, не Tilemap.
        // Полная квалификация UnityEngine.Tilemaps.TileData — конфликт с CultivationGame.TileSystem.TileData
        // Редактировано: 2026-04-11 10:06:19 UTC
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref UnityEngine.Tilemaps.TileData tileData)
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
