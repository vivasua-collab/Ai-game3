// ============================================================================
// GameTile.cs — Пользовательский тайл для Tilemap
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-11 14:44:18 UTC — FIX CS0115: ITilemap (по API Unity 6000.3) + полная квалификация TileData
// ============================================================================
//
// ИСТОРИЯ ИСПРАВЛЕНИЙ GetTileData (ВАЖНО!):
//
// КОРНЕВАЯ ПРИЧИНА CS0115: ДВЕ проблемы одновременно:
//   1. Конфликт имён TileData: внутри namespace CultivationGame.TileSystem
//      имя TileData резолвится в наш класс, а НЕ в UnityEngine.Tilemaps.TileData.
//   2. Оба параметра (ITilemap и Tilemap) тестировались, но каждая попытка
//      меняла только ОДНУ проблему, оставляя вторую.
//
// ПРАВИЛЬНОЕ РЕШЕНИЕ (обе проблемы сразу):
//   - ITilemap — правильный тип 2-го параметра (по API Unity 6000.3)
//   - UnityEngine.Tilemaps.TileData — полная квалификация 3-го параметра
//   - UnityEngine.Tilemaps.TileFlags — полная квалификация для флагов
//
// Источник: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.TileBase.GetTileData.html
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

        // FIX CS0115: ITilemap — правильный тип 2-го параметра по API Unity 6000.3.
        // Предыдущая попытка с ITilemap не удалась из-за конфликта TileData
        // (наш CultivationGame.TileSystem.TileData вместо UnityEngine.Tilemaps.TileData).
        // Теперь TileData полностью квалифицирован — ITilemap работает.
        // Редактировано: 2026-04-11 14:44:18 UTC
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
