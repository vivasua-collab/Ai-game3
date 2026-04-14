// ============================================================================
// ObjectTile.cs — Тайл объекта
// Cultivation World Simulator
// Создано: 2026-04-13 14:03:25 UTC
// ============================================================================
//
// ВЫНЕСЕН ИЗ GameTile.cs — Unity требует совпадение имени файла и класса
// для ScriptableObject с [CreateAssetMenu]. До этого ObjectTile был
// определён внутри GameTile.cs, что вызывало "No script asset for ObjectTile".
// ============================================================================

using UnityEngine;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Тайл объекта.
    /// Используется для деревьев, камней, кустов, сундуков и т.д.
    /// Объекты непроходимы по умолчанию.
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
