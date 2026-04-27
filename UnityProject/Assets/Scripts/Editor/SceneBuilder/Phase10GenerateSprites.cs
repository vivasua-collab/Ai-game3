// ============================================================================
// Phase10GenerateSprites.cs — Фаза 10: Генерация тайловых спрайтов
// Cultivation World Simulator
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.TileSystem.Editor;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase10GenerateSprites : IScenePhase
    {
        public string Name => "Generate Tile Sprites";
        public string MenuPath => "Phase 10: Generate Tile Sprites";
        public int Order => 10;

        public bool IsNeeded()
        {
            return !SceneBuilderUtils.HasAssetsInFolder("Assets/Sprites/Tiles");
        }

        public void Execute()
        {
            Debug.Log("[Phase10] Generating tile sprites...");
            TileSpriteGenerator.GenerateAllSprites();
            AssetDatabase.Refresh();
        }
    }
}
#endif
