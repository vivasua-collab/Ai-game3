// ============================================================================
// SceneBuilderConstants.cs — Общие константы для всех фаз сборки
// Cultivation World Simulator
// Версия: 2.0
// ============================================================================
// Вынесено из FullSceneBuilder.cs для использования в отдельных фазах.
// ============================================================================

#if UNITY_EDITOR
using System.Collections.Generic;

namespace CultivationGame.Editor.SceneBuilder
{
    /// <summary>
    /// Общие константы для всех фаз сборки сцены.
    /// Раньше находились внутри FullSceneBuilder — теперь доступны всем фазам.
    /// </summary>
    public static class SceneBuilderConstants
    {
        public const string SCENE_PATH = "Assets/Scenes/Main.unity";
        public const string SCENE_NAME = "Main";

        // Папки, которые должны существовать
        public static readonly string[] REQUIRED_FOLDERS = new string[]
        {
            "Assets/Scenes",
            "Assets/Prefabs/Player",
            "Assets/Prefabs/NPC",
            "Assets/Prefabs/UI",
            "Assets/Prefabs/Items",
            "Assets/Data/JSON",
            "Assets/Data/CultivationLevels",
            "Assets/Data/Elements",
            "Assets/Data/MortalStages",
            "Assets/Data/Techniques",
            "Assets/Data/NPCPresets",
            "Assets/Data/Equipment",
            "Assets/Data/Items",
            "Assets/Data/Materials",
            "Assets/Data/Formations",
            "Assets/Data/FormationCores",
            "Assets/Data/Species",
            "Assets/Sprites/Tiles",
            "Assets/Sprites/Characters/Player",
            "Assets/Sprites/Characters/NPC",
            "Assets/Sprites/Elements",
            "Assets/Sprites/Equipment",
            "Assets/Sprites/Items",
            "Assets/Sprites/Techniques",
            "Assets/Sprites/Combat",
            "Assets/Sprites/UI",
            "Assets/Sprites/Cultivation",
            "Assets/Tiles/Terrain",
            "Assets/Tiles/Objects",
            "Assets/Audio/Music",
            "Assets/Audio/SFX",
            "Assets/Art/Characters",
            "Assets/Art/Effects",
            "Assets/Art/Items",
            "Assets/Art/Sprites",
            "Assets/Art/UI",
        };

        // Теги
        public static readonly string[] REQUIRED_TAGS = new string[]
        {
            "Player",
            "NPC",
            "Interactable",
            "Item",
            "Enemy",
            "Resource",
            "Harvestable",
        };

        // Слои (имя → номер)
        public static readonly KeyValuePair<string, int>[] REQUIRED_LAYERS = new KeyValuePair<string, int>[]
        {
            new KeyValuePair<string, int>("Player", 6),
            new KeyValuePair<string, int>("NPC", 7),
            new KeyValuePair<string, int>("Interactable", 8),
            new KeyValuePair<string, int>("Item", 9),
            new KeyValuePair<string, int>("Enemy", 10),
            new KeyValuePair<string, int>("GameUI", 11),   // PATCH-012: Переименован из UI в GameUI
            new KeyValuePair<string, int>("Background", 12),
            new KeyValuePair<string, int>("Harvestable", 13),
        };

        // Sorting Layers (порядок важен — снизу вверх)
        public static readonly string[] REQUIRED_SORTING_LAYERS = new string[]
        {
            "Default",     // ID=0, всегда существует
            "Background",  // ID=1
            "Terrain",     // ID=2
            "Objects",     // ID=3 — КРИТИЧЕСКИЙ! Используется PlayerVisual, HarvestableSpawner
            "Player",      // ID=4 — Используется PlayerVisual
            "UI"           // ID=5
        };
    }
}
#endif
