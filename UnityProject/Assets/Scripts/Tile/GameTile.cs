// ============================================================================
// GameTile.cs — Пользовательский тайл для Tilemap
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-11 14:12:33 UTC — FIX CS0115: ITilemap + полная квалификация TileData
// ============================================================================
//
// ИСТОРИЯ ИСПРАВЛЕНИЙ GetTileData (ВАЖНО!):
//
// КОРНЕВАЯ ПРИЧИНА CS0115: конфликт имён TileData.
//   Внутри namespace CultivationGame.TileSystem имя TileData резолвится
//   в CultivationGame.TileSystem.TileData (наш класс), а НЕ в
//   UnityEngine.Tilemaps.TileData (Unity struct). Сигнатура override
//   не совпадает → CS0115 → каскад 7 ошибок CS0234/CS0246.
//
// ПРЕДЫДУЩИЕ (ОШИБОЧНЫЕ) ДИАГНОЗЫ:
//   1. «ITilemap→Tilemap решает CS0115» — НЕВЕРНО. Tilemap НЕ совпадает
//      с базовым методом TileBase.GetTileData(Vector3Int, ITilemap, ref TileData)
//      по документации Unity 6000.3 (6.3).
//   2. «ITilemap вызывает CS0115» — НЕВЕРНО. ITilemap — правильный тип
//      параметра для Unity 6000.3 (подтверждено официальной документацией).
//
// ПРАВИЛЬНОЕ РЕШЕНИЕ:
//   - ITilemap — правильный тип 2-го параметра (Unity 6000.3 Scripting API)
//   - UnityEngine.Tilemaps.TileData — полная квалификация 3-го параметра
//     для устранения конфликта с CultivationGame.TileSystem.TileData
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

        // FIX CS0115: ITilemap — правильный тип по документации Unity 6000.3.
        // Полная квалификация UnityEngine.Tilemaps.TileData — конфликт
        // с CultivationGame.TileSystem.TileData (наш класс данных тайла).
        // Редактировано: 2026-04-11 14:12:33 UTC
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
