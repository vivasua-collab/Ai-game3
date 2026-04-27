// ============================================================================
// Phase14CreateTileAssets.cs — Фаза 14: Создание тайловых ассетов
// Cultivation World Simulator
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using CultivationGame.World;
using CultivationGame.TileSystem;
using CultivationGame.TileSystem.Editor;
using System.IO;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase14CreateTileAssets : IScenePhase
    {
        public string Name => "Create & Assign Tile Assets";
        public string MenuPath => "Phase 14: Create Tile Assets";
        public int Order => 14;

        public bool IsNeeded()
        {
            bool terrainAssetsExist = SceneBuilderUtils.HasAssetsInFolder("Assets/Tiles/Terrain");
            bool objectAssetsExist = SceneBuilderUtils.HasAssetsInFolder("Assets/Tiles/Objects");
            if (!terrainAssetsExist || !objectAssetsExist) return true;

            SceneBuilderUtils.EnsureSceneOpen();
            var controller = UnityEngine.Object.FindFirstObjectByType<TileMapController>();
            if (controller == null) return true;

            var so = new SerializedObject(controller);
            var grassProp = so.FindProperty("grassTile");
            if (grassProp == null || grassProp.objectReferenceValue == null) return true;

            return false;
        }

        public void Execute()
        {
            // Шаг 1: Убедиться что спрайты существуют
            if (!Directory.Exists("Assets/Sprites/Tiles") ||
                !File.Exists("Assets/Sprites/Tiles/terrain_grass.png"))
            {
                Debug.Log("[Phase14] Спрайты не найдены, генерируем...");
                TileSpriteGenerator.GenerateAllSprites();
            }

            // Шаг 1.5: Реимпорт спрайтов
            SceneBuilderUtils.ReimportTileSprites();

            // Шаг 2: Создать папки
            SceneBuilderUtils.EnsureDirectory("Assets/Tiles/Terrain");
            SceneBuilderUtils.EnsureDirectory("Assets/Tiles/Objects");

            // Шаг 3: Terrain Tile assets
            CreateTerrainTileAsset("Tile_Grass", "terrain_grass", TerrainType.Grass, 1.0f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_Dirt", "terrain_dirt", TerrainType.Dirt, 1.0f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_Stone", "terrain_stone", TerrainType.Stone, 1.0f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_WaterShallow", "terrain_water_shallow", TerrainType.Water_Shallow, 2.0f, true, GameTileFlags.Passable | GameTileFlags.Swimable);
            CreateTerrainTileAsset("Tile_WaterDeep", "terrain_water_deep", TerrainType.Water_Deep, 0.0f, false, GameTileFlags.Swimable | GameTileFlags.Flyable);
            CreateTerrainTileAsset("Tile_Sand", "terrain_sand", TerrainType.Sand, 1.2f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_Void", "terrain_void", TerrainType.Void, 0.0f, false, GameTileFlags.None);
            CreateTerrainTileAsset("Tile_Snow", "terrain_snow", TerrainType.Snow, 1.3f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_Ice", "terrain_ice", TerrainType.Ice, 1.5f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_Lava", "terrain_lava", TerrainType.Lava, 0.0f, false, GameTileFlags.Flyable);

            // Шаг 4: Object Tile assets
            CreateObjectTileAsset("Tile_Tree", "obj_tree", TileObjectType.Tree_Oak, 200, true, true, true);
            CreateObjectTileAsset("Tile_RockSmall", "obj_rock_small", TileObjectType.Rock_Small, 100, false, false, true);
            CreateObjectTileAsset("Tile_RockMedium", "obj_rock_medium", TileObjectType.Rock_Medium, 300, true, true, true);
            CreateObjectTileAsset("Tile_Bush", "obj_bush", TileObjectType.Bush, 50, false, false, false);
            CreateObjectTileAsset("Tile_Chest", "obj_chest", TileObjectType.Chest, 50, false, false, false);
            CreateObjectTileAsset("Tile_OreVein", "obj_ore_vein", TileObjectType.OreVein, 250, true, true, true);
            CreateObjectTileAsset("Tile_Herb", "obj_herb", TileObjectType.Herb, 30, false, false, false);
            CreateObjectTileAsset("Tile_TreeOak", "obj_tree_oak", TileObjectType.Tree_Oak, 200, true, true, true);
            CreateObjectTileAsset("Tile_TreePine", "obj_tree_pine", TileObjectType.Tree_Pine, 200, true, true, true);
            CreateObjectTileAsset("Tile_TreeBirch", "obj_tree_birch", TileObjectType.Tree_Birch, 180, true, true, true);
            CreateObjectTileAsset("Tile_BushBerry", "obj_bush_berry", TileObjectType.Bush_Berry, 40, false, false, true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Шаг 5: Назначить TileBase
            AssignTileBasesToController();

            Debug.Log("[Phase14] Tile assets созданы и назначены");
        }

        private void CreateTerrainTileAsset(string assetName, string spriteName, TerrainType terrainType, float moveCost, bool isPassable, GameTileFlags flags)
        {
            string assetPath = $"Assets/Tiles/Terrain/{assetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<TerrainTile>(assetPath) != null) return;

            var tile = ScriptableObject.CreateInstance<TerrainTile>();
            tile.terrainType = terrainType;
            tile.moveCost = moveCost;
            tile.isPassable = isPassable;
            tile.flags = flags;
            tile.color = Color.white;

            string spritePath = $"Assets/Sprites/Tiles/{spriteName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null) tile.sprite = sprite;
            else Debug.LogWarning($"[Phase14] Спрайт не найден: {spritePath}");

            AssetDatabase.CreateAsset(tile, assetPath);
            Debug.Log($"[Phase14] Создан TerrainTile: {assetPath}");
        }

        private void CreateObjectTileAsset(string assetName, string spriteName, TileObjectType objectType, int durability, bool blocksVision, bool providesCover, bool isHarvestable)
        {
            string assetPath = $"Assets/Tiles/Objects/{assetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<ObjectTile>(assetPath) != null) return;

            var tile = ScriptableObject.CreateInstance<ObjectTile>();
            tile.objectType = objectType;
            tile.durability = durability;
            tile.blocksVision = blocksVision;
            tile.providesCover = providesCover;
            tile.isHarvestable = isHarvestable;
            tile.isPassable = false;
            tile.flags = GameTileFlags.None;
            tile.color = Color.white;

            string spritePath = $"Assets/Sprites/Tiles/{spriteName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null) tile.sprite = sprite;
            else Debug.LogWarning($"[Phase14] Спрайт не найден: {spritePath}");

            AssetDatabase.CreateAsset(tile, assetPath);
            Debug.Log($"[Phase14] Создан ObjectTile: {assetPath}");
        }

        private void AssignTileBasesToController()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            var controller = UnityEngine.Object.FindFirstObjectByType<TileMapController>();
            if (controller == null)
            {
                Debug.LogWarning("[Phase14] TileMapController не найден. Пропускаем назначение TileBase.");
                return;
            }

            var so = new SerializedObject(controller);

            // Terrain Tiles
            SceneBuilderUtils.AssignTileProperty(so, "grassTile", "Assets/Tiles/Terrain/Tile_Grass.asset");
            SceneBuilderUtils.AssignTileProperty(so, "dirtTile", "Assets/Tiles/Terrain/Tile_Dirt.asset");
            SceneBuilderUtils.AssignTileProperty(so, "stoneTile", "Assets/Tiles/Terrain/Tile_Stone.asset");
            SceneBuilderUtils.AssignTileProperty(so, "waterShallowTile", "Assets/Tiles/Terrain/Tile_WaterShallow.asset");
            SceneBuilderUtils.AssignTileProperty(so, "waterDeepTile", "Assets/Tiles/Terrain/Tile_WaterDeep.asset");
            SceneBuilderUtils.AssignTileProperty(so, "sandTile", "Assets/Tiles/Terrain/Tile_Sand.asset");
            SceneBuilderUtils.AssignTileProperty(so, "voidTile", "Assets/Tiles/Terrain/Tile_Void.asset");
            SceneBuilderUtils.AssignTileProperty(so, "snowTile", "Assets/Tiles/Terrain/Tile_Snow.asset");
            SceneBuilderUtils.AssignTileProperty(so, "iceTile", "Assets/Tiles/Terrain/Tile_Ice.asset");
            SceneBuilderUtils.AssignTileProperty(so, "lavaTile", "Assets/Tiles/Terrain/Tile_Lava.asset");

            // Object Tiles
            SceneBuilderUtils.AssignTileProperty(so, "treeTile", "Assets/Tiles/Objects/Tile_Tree.asset");
            SceneBuilderUtils.AssignTileProperty(so, "rockSmallTile", "Assets/Tiles/Objects/Tile_RockSmall.asset");
            SceneBuilderUtils.AssignTileProperty(so, "rockMediumTile", "Assets/Tiles/Objects/Tile_RockMedium.asset");
            SceneBuilderUtils.AssignTileProperty(so, "bushTile", "Assets/Tiles/Objects/Tile_Bush.asset");
            SceneBuilderUtils.AssignTileProperty(so, "chestTile", "Assets/Tiles/Objects/Tile_Chest.asset");
            SceneBuilderUtils.AssignTileProperty(so, "oreVeinTile", "Assets/Tiles/Objects/Tile_OreVein.asset");
            SceneBuilderUtils.AssignTileProperty(so, "herbTile", "Assets/Tiles/Objects/Tile_Herb.asset");
            SceneBuilderUtils.AssignTileProperty(so, "treeOakTile", "Assets/Tiles/Objects/Tile_TreeOak.asset");
            SceneBuilderUtils.AssignTileProperty(so, "treePineTile", "Assets/Tiles/Objects/Tile_TreePine.asset");
            SceneBuilderUtils.AssignTileProperty(so, "treeBirchTile", "Assets/Tiles/Objects/Tile_TreeBirch.asset");
            SceneBuilderUtils.AssignTileProperty(so, "bushBerryTile", "Assets/Tiles/Objects/Tile_BushBerry.asset");

            so.ApplyModifiedProperties();
            Debug.Log("[Phase14] TileBase ссылки назначены в TileMapController");
        }
    }
}
#endif
