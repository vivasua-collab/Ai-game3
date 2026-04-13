// ============================================================================
// TerrainTile.cs — Тайл поверхности
// Cultivation World Simulator
// Создано: 2026-04-13 14:03:25 UTC
// ============================================================================
//
// ВЫНЕСЕН ИЗ GameTile.cs — Unity требует совпадение имени файла и класса
// для ScriptableObject с [CreateAssetMenu]. До этого TerrainTile был
// определён внутри GameTile.cs, что вызывало "No script asset for TerrainTile".
// ============================================================================

using UnityEngine;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Тайл поверхности.
    /// Используется для рендеринга земли, воды, песка и т.д.
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
}
