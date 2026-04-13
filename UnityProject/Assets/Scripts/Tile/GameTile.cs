// ============================================================================
// GameTile.cs — Пользовательский тайл для Tilemap (базовый класс)
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-15 12:00:00 UTC — FIX: colliderType на основе isPassable
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
// РЕДАКТИРОВАНИЕ 2026-04-13: TerrainTile и ObjectTile вынесены в отдельные файлы
//   (TerrainTile.cs и ObjectTile.cs). Unity требует совпадение имени файла и
//   класса для ScriptableObject с [CreateAssetMenu], иначе возникает ошибка
//   "No script asset for TerrainTile/ObjectTile".
//
// Источник: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Tilemaps.TileBase.GetTileData.html
// ============================================================================

using UnityEngine;
using UnityEngine.Tilemaps;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Пользовательский тайл с дополнительными данными.
    /// Базовый класс для TerrainTile и ObjectTile.
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
        // FIX: Устанавливаем colliderType на основе isPassable.
        // Passable тайлы (трава, грязь, песок) НЕ создают физический коллайдер,
        // непроходимые (вода, void, объекты) — создают.
        // Без этого-fix TilemapCollider2D создаёт коллайдеры для ВСЕХ тайлов,
        // полностью блокируя движение игрока.
        // Редактировано: 2026-04-15 12:00:00 UTC
        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref UnityEngine.Tilemaps.TileData tileData)
        {
            tileData.sprite = sprite;
            tileData.color = color;
            tileData.flags = UnityEngine.Tilemaps.TileFlags.None;

            // Ключевой FIX: проходимые тайлы не создают коллайдер
            tileData.colliderType = isPassable
                ? UnityEngine.Tilemaps.Tile.ColliderType.None
                : UnityEngine.Tilemaps.Tile.ColliderType.Sprite;
        }
    }
}
