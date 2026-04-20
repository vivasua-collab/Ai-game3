// ============================================================================
// TileMapController.cs — Контроллер карты тайлов
// Cultivation World Simulator
// Создано: 2026-04-07 14:24:05 UTC
// Редактировано: 2026-04-17 12:38 UTC — FIX-SORT: FixAllTilemapRenderers() ищет ВСЕ TilemapRenderer по типу + EnsureSortingLayerOrder() проверяет ПОРЯДОК слоёв.
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CultivationGame.Core;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Контроллер карты тайлов.
    /// Управляет отображением, обновлением и взаимодействием с тайлами.
    /// </summary>
    public class TileMapController : MonoBehaviour
    {
        [Header("Tilemap References")]
        [SerializeField] private Tilemap terrainTilemap;
        [SerializeField] private Tilemap objectTilemap;
        [SerializeField] private Tilemap overlayTilemap;

        [Header("Terrain Tiles")]
        [SerializeField] private TileBase grassTile;
        [SerializeField] private TileBase dirtTile;
        [SerializeField] private TileBase stoneTile;
        [SerializeField] private TileBase waterShallowTile;
        [SerializeField] private TileBase waterDeepTile;
        [SerializeField] private TileBase sandTile;
        [SerializeField] private TileBase voidTile;
        // FIX: Добавлены недостающие типы террейна
        // Редактировано: 2026-04-14 13:45:00 UTC
        [SerializeField] private TileBase snowTile;
        [SerializeField] private TileBase iceTile;
        [SerializeField] private TileBase lavaTile;

        [Header("Object Tiles")]
        [SerializeField] private TileBase treeTile;
        // FIX-R2: Добавлены подтипы деревьев для корректного отображения каждого вида.
        // Ранее: все деревья → один treeTile. Теперь: каждое дерево → свой tile.
        // Редактировано: 2026-04-16 08:03 UTC
        [SerializeField] private TileBase treeOakTile;
        [SerializeField] private TileBase treePineTile;
        [SerializeField] private TileBase treeBirchTile;
        [SerializeField] private TileBase rockSmallTile;
        [SerializeField] private TileBase rockMediumTile;
        [SerializeField] private TileBase bushTile;
        [SerializeField] private TileBase bushBerryTile;
        [SerializeField] private TileBase chestTile;
        // FIX: Добавлены недостающие типы объектов
        [SerializeField] private TileBase oreVeinTile;
        [SerializeField] private TileBase herbTile;

        [Header("Settings")]
        // FIX: Размер карты 100×80 тайлов = 200×160 метров (было 80×60)
        // Редактировано: 2026-04-14 13:45:00 UTC
        [SerializeField] private int defaultWidth = 100;
        [SerializeField] private int defaultHeight = 80;
        [SerializeField] private bool generateOnStart = true;

        // === State ===
        private TileMapData mapData;
        private Dictionary<string, GameObject> spawnedObjects = new Dictionary<string, GameObject>();
        private Transform objectsParent;

        // === Events ===
        public event Action<TileData> OnTileChanged;
        public event Action<TileMapData> OnMapGenerated;

        // === Properties ===
        public TileMapData MapData => mapData;
        public int Width => mapData?.width ?? 0;
        public int Height => mapData?.height ?? 0;

        // === Unity Lifecycle ===

        private void Awake()
        {
            // Создать родительский объект для спавна
            objectsParent = new GameObject("SpawnedObjects").transform;
            objectsParent.SetParent(transform);

            // FIX: cellGap = 0. Pixel bleed через увеличенные terrain-спрайты (68×68 PPU=32 → 2.125u)
            // устраняет белую сетку надёжнее, чем отрицательный cellGap.
            // Редактировано: 2026-04-15 UTC
            var grid = GetComponentInParent<Grid>();
            if (grid != null)
            {
                grid.cellGap = Vector3.zero;
            }

            // FIX-V2-7: Runtime автофикс Sorting Layers для TilemapRenderer.
            // TestLocationSetup НЕ назначал sortingLayerName → оба рендерера на "Default" слое.
            // Это приводило к тому, что тайлы рендерились поверх игрока и ресурсов.
            // Теперь: terrain → "Terrain", objects → "Objects" (всегда, независимо от создателя сцены).
            // Редактировано: 2026-04-17 UTC
            EnsureSortingLayersExist();
            FixTilemapSortingLayers();

            // FIX-SORT: Проверить ПОРЯДОК Sorting Layers — если Player ниже Terrain,
            // игрок рендерится ПОЗАДИ тайлов → невидим. EnsureSortingLayersExist()
            // только создаёт недостающие, но НЕ проверяет порядок.
            // Редактировано: 2026-04-17 12:38 UTC
            EnsureSortingLayerOrder();
        }

        private void Start()
        {
            // FIX: Автосоздание GameTile из спрайтов, если [SerializeField] поля не назначены
            // Редактировано: 2026-04-15 11:15:00 UTC
            EnsureTileAssets();

            // FIX-V2-7: Runtime диагностика Sorting Layers и TilemapRenderer.
            // L1/L2 логи НЕ вызывались в рантайме — только из Editor (FullSceneBuilder).
            // Без них невозможно понять, на каком слое реально рендерится тайловая карта.
            // Редактировано: 2026-04-17 UTC
            RenderPipelineLogger.LogSortingLayers();
            RenderPipelineLogger.LogTilemapState();

            // FIX-SORT: Поиск ВСЕХ TilemapRenderer на сцене по типу.
            // FixTilemapSortingLayers() фиксит только 3 конкретных [SerializeField] ссылки.
            // Если FullSceneBuilder создал дополнительные TilemapRenderer'ы,
            // они остаются на "Default" слое → рендерятся поверх игрока.
            // Редактировано: 2026-04-17 12:38 UTC
            FixAllTilemapRenderers();
            RenderPipelineLogger.LogAllRendererState();

            if (generateOnStart)
            {
                GenerateTestMap();
            }
        }

        /// <summary>
        /// FIX-V2-7: Убедиться, что Sorting Layers "Terrain" и "Objects" существуют.
        /// Если их нет — создать через TagManager (Editor) или предупредить (Build).
        /// Без этих слоёв TilemapRenderer'ы попадают на "Default" → рендер поверх всего.
        /// Редактировано: 2026-04-17 UTC
        /// </summary>
        private void EnsureSortingLayersExist()
        {
            var layers = SortingLayer.layers;
            bool hasTerrain = false;
            bool hasObjects = false;
            bool hasPlayer = false;

            foreach (var layer in layers)
            {
                if (layer.name == "Terrain") hasTerrain = true;
                if (layer.name == "Objects") hasObjects = true;
                if (layer.name == "Player") hasPlayer = true;
            }

            if (hasTerrain && hasObjects && hasPlayer)
                return; // Всё ОК

            Debug.LogWarning($"[TileMapController] FIX-V2-7: Отсутствуют Sorting Layers! " +
                $"Terrain={hasTerrain}, Objects={hasObjects}, Player={hasPlayer}. " +
                $"Попытка создать...");

#if UNITY_EDITOR
            // Создаём недостающие Sorting Layers через TagManager
            var tagManager = new UnityEditor.SerializedObject(
                UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");

            if (sortingLayersProp != null)
            {
                string[] requiredLayers = { "Background", "Terrain", "Objects", "Player", "UI" };

                foreach (var layerName in requiredLayers)
                {
                    bool exists = false;
                    for (int i = 0; i < sortingLayersProp.arraySize; i++)
                    {
                        var nameProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                        if (nameProp != null && nameProp.stringValue == layerName)
                        {
                            exists = true;
                            break;
                        }
                    }

                    if (!exists)
                    {
                        sortingLayersProp.InsertArrayElementAtIndex(sortingLayersProp.arraySize);
                        var newLayer = sortingLayersProp.GetArrayElementAtIndex(sortingLayersProp.arraySize - 1);
                        newLayer.FindPropertyRelative("name").stringValue = layerName;
                        // uniqueID будет переназначен детерминированно в EnsureSortingLayerOrder()
                        newLayer.FindPropertyRelative("uniqueID").intValue = sortingLayersProp.arraySize - 1;
                        newLayer.FindPropertyRelative("locked").boolValue = false;
                        Debug.Log($"[TileMapController] FIX-V2-7: Создан Sorting Layer \"{layerName}\"");
                    }
                }

                tagManager.ApplyModifiedProperties();
                UnityEditor.AssetDatabase.SaveAssets();
                Debug.Log("[TileMapController] FIX-V2-7: Sorting Layers созданы и сохранены");
            }
#else
            Debug.LogError("[TileMapController] Sorting Layers отсутствуют! В build версии их нужно создать заранее в Editor.");
#endif
        }

        /// <summary>
        /// FIX-SORT: Проверить ПОРЯДОК Sorting Layers и исправить если неправильный.
        /// EnsureSortingLayersExist() только создаёт недостающие слои, но НЕ проверяет
        /// их порядок. Если слои были созданы ранее вручную или другим скриптом
        /// в неправильном порядке (например, Player перед Terrain), то Player
        /// рендерится ПОЗАДИ Terrain → спрайт поверхности поверх игрока.
        /// Правильный порядок: Default(0) < Background(1) < Terrain(2) < Objects(3) < Player(4) < UI(5)
        /// Редактировано: 2026-04-17 12:38 UTC
        /// </summary>
        private void EnsureSortingLayerOrder()
        {
            // Правильный порядок слоёв (индекс = порядок рендеринга, меньше = позади)
            string[] correctOrder = { "Default", "Background", "Terrain", "Objects", "Player", "UI" };

            var layers = SortingLayer.layers;

            // Проверяем порядок: каждый требуемый слой должен быть на правильной позиции
            bool orderIsCorrect = true;
            for (int i = 0; i < layers.Length && i < correctOrder.Length; i++)
            {
                if (layers[i].name != correctOrder[i])
                {
                    orderIsCorrect = false;
                    break;
                }
            }

            // Также проверяем, что нет лишних слоёв между нашими
            if (layers.Length < correctOrder.Length) orderIsCorrect = false;

            if (orderIsCorrect)
            {
                Debug.Log("[TileMapController] FIX-SORT: Порядок Sorting Layers корректный");
                return;
            }

            // Порядок неправильный — логируем проблему
            Debug.LogWarning("[TileMapController] FIX-SORT: Порядок Sorting Layers НЕПРАВИЛЬНЫЙ! " +
                "Это может приводить к тому, что terrain рендерится поверх игрока.");
            for (int i = 0; i < layers.Length; i++)
            {
                Debug.LogWarning($"  [{i}] \"{layers[i].name}\" (id={layers[i].id})");
            }
            Debug.LogWarning($"Ожидаемый порядок: {string.Join(" < ", correctOrder)}");

#if UNITY_EDITOR
            // Перестраиваем порядок слоёв через TagManager
            var tagManager = new UnityEditor.SerializedObject(
                UnityEditor.AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");

            if (sortingLayersProp == null)
            {
                Debug.LogError("[TileMapController] FIX-SORT: m_SortingLayers не найден!");
                return;
            }

            // Собираем существующие слои в словарь (имя → свойства)
            var existingLayers = new Dictionary<string, UnityEditor.SerializedProperty>();
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var elem = sortingLayersProp.GetArrayElementAtIndex(i);
                var nameProp = elem.FindPropertyRelative("name");
                if (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
                    existingLayers[nameProp.stringValue] = elem;
            }

            // Создаём недостающие слои перед перестановкой
            foreach (var layerName in correctOrder)
            {
                if (!existingLayers.ContainsKey(layerName) && layerName != "Default")
                {
                    sortingLayersProp.InsertArrayElementAtIndex(sortingLayersProp.arraySize);
                    var newLayer = sortingLayersProp.GetArrayElementAtIndex(sortingLayersProp.arraySize - 1);
                    newLayer.FindPropertyRelative("name").stringValue = layerName;
                    // uniqueID будет переназначен детерминированно ниже
                    newLayer.FindPropertyRelative("uniqueID").intValue = sortingLayersProp.arraySize - 1;
                    newLayer.FindPropertyRelative("locked").boolValue = false;
                    Debug.Log($"[TileMapController] FIX-SORT: Создан недостающий слой \"{layerName}\"");
                }
            }

            // Перестраиваем массив в правильном порядке.
            // Способ: читаем все элементы, очищаем массив, вставляем в правильном порядке.
            // Сначала сохраняем данные всех слоёв
            var layerData = new List<Dictionary<string, object>>();
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var elem = sortingLayersProp.GetArrayElementAtIndex(i);
                var data = new Dictionary<string, object>();
                var nameProp = elem.FindPropertyRelative("name");
                var idProp = elem.FindPropertyRelative("uniqueID");
                var lockedProp = elem.FindPropertyRelative("locked");
                if (nameProp != null) data["name"] = nameProp.stringValue;
                if (idProp != null) data["uniqueID"] = idProp.intValue;
                if (lockedProp != null) data["locked"] = lockedProp.boolValue;
                layerData.Add(data);
            }

            // Строим новый порядок: сначала correctOrder, потом остальные
            var newOrder = new List<Dictionary<string, object>>();
            var usedNames = new HashSet<string>();

            // Добавляем в правильном порядке
            foreach (var expectedName in correctOrder)
            {
                var found = layerData.Find(d => d.ContainsKey("name") && (string)d["name"] == expectedName);
                if (found != null)
                {
                    newOrder.Add(found);
                    usedNames.Add(expectedName);
                }
            }

            // Добавляем остальные слои (не из correctOrder) в конец
            foreach (var data in layerData)
            {
                if (data.ContainsKey("name") && !usedNames.Contains((string)data["name"]))
                    newOrder.Add(data);
            }

            // Очищаем массив и заполняем в новом порядке с ДЕТЕРМИНИРОВАННЫМИ uniqueID
            // uniqueID = индекс слоя (0, 1, 2, ...) — гарантирует одинаковый результат на любом ПК
            sortingLayersProp.ClearArray();
            for (int idx = 0; idx < newOrder.Count; idx++)
            {
                var data = newOrder[idx];
                sortingLayersProp.InsertArrayElementAtIndex(idx);
                var elem = sortingLayersProp.GetArrayElementAtIndex(idx);
                if (data.ContainsKey("name"))
                    elem.FindPropertyRelative("name").stringValue = (string)data["name"];
                elem.FindPropertyRelative("uniqueID").intValue = idx;  // ДЕТЕРМИНИРОВАННЫЙ ID
                elem.FindPropertyRelative("locked").boolValue = false;
            }

            tagManager.ApplyModifiedProperties();
            UnityEditor.AssetDatabase.SaveAssets();

            Debug.Log("[TileMapController] FIX-SORT: Порядок Sorting Layers исправлен:");
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var elem = sortingLayersProp.GetArrayElementAtIndex(i);
                var n = elem.FindPropertyRelative("name");
                Debug.Log($"  [{i}] \"{n?.stringValue ?? "?"}\"");
            }
#else
            Debug.LogError("[TileMapController] FIX-SORT: Порядок Sorting Layers неправильный! " +
                "В build версии порядок должен быть настроен заранее в Editor.");
#endif
        }

        /// <summary>
        /// FIX-SORT: Найти ВСЕ TilemapRenderer на сцене и установить правильные Sorting Layers.
        /// FixTilemapSortingLayers() работает только с [SerializeField] ссылками.
        /// Если на сцене есть TilemapRenderer'ы, не привязанные к этому контроллеру
        /// (например, созданные FullSceneBuilder или вручную), они остаются на "Default"
        /// слое → рендерятся поверх Player и Objects.
        /// Этот метод ищет ВСЕ TilemapRenderer по типу и назначает правильные слои
        /// по имени GameObject (terrain → "Terrain", остальные → "Objects").
        /// Редактировано: 2026-04-17 12:38 UTC
        /// </summary>
        private void FixAllTilemapRenderers()
        {
            var allRenderers = FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            int fixedCount = 0;

            foreach (var renderer in allRenderers)
            {
                string currentLayer = renderer.sortingLayerName;
                string targetLayer = DetermineSortingLayerForRenderer(renderer);

                if (currentLayer != targetLayer)
                {
                    Debug.LogWarning($"[TileMapController] FIX-SORT: TilemapRenderer \"{renderer.name}\" " +
                        $"был на слое \"{currentLayer}\" → исправлено на \"{targetLayer}\"");
                    renderer.sortingLayerName = targetLayer;

                    // Terrain tilemap — order=0, Objects tilemap — order=0, overlay — order=10
                    renderer.sortingOrder = (targetLayer == "Objects" && renderer.name.Contains("Overlay")) ? 10 : 0;
                    fixedCount++;
                }
            }

            if (fixedCount > 0)
                Debug.Log($"[TileMapController] FIX-SORT: Исправлено {fixedCount} TilemapRenderer'ов");
            else
                Debug.Log("[TileMapController] FIX-SORT: Все TilemapRenderer'ы на правильных слоях");
        }

        /// <summary>
        /// FIX-SORT: Определить правильный Sorting Layer для TilemapRenderer по его имени.
        /// Имя GameObject содержит подстроку "Terrain" → слой "Terrain".
        /// Имя содержит "Object" или другое → слой "Objects".
        /// Редактировано: 2026-04-17 12:38 UTC
        /// </summary>
        private string DetermineSortingLayerForRenderer(TilemapRenderer renderer)
        {
            string name = renderer.gameObject.name;

            // По имени GameObject определяем тип
            if (name.Contains("Terrain") || name.Contains("terrain"))
                return "Terrain";
            if (name.Contains("Object") || name.Contains("object"))
                return "Objects";
            if (name.Contains("Overlay") || name.Contains("overlay"))
                return "Objects";

            // Fallback: проверяем, является ли этот рендерер одним из наших
            if (terrainTilemap != null && renderer.GetComponent<Tilemap>() == terrainTilemap)
                return "Terrain";
            if (objectTilemap != null && renderer.GetComponent<Tilemap>() == objectTilemap)
                return "Objects";
            if (overlayTilemap != null && renderer.GetComponent<Tilemap>() == overlayTilemap)
                return "Objects";

            // Если не можем определить — ставим на Objects (безопаснее, чем Default)
            Debug.LogWarning($"[TileMapController] FIX-SORT: Неизвестный TilemapRenderer \"{name}\" " +
                $"→ fallback на \"Objects\"");
            return "Objects";
        }

        /// <summary>
        /// FIX-V2-7: Принудительно назначить правильные Sorting Layers для TilemapRenderer'ов.
        /// TestLocationSetup НЕ назначал sortingLayerName → "Default" → рендер поверх игрока.
        /// Редактировано: 2026-04-17 UTC
        /// </summary>
        private void FixTilemapSortingLayers()
        {
            // Фиксим terrain tilemap
            if (terrainTilemap != null)
            {
                var renderer = terrainTilemap.GetComponent<TilemapRenderer>();
                if (renderer != null && renderer.sortingLayerName != "Terrain")
                {
                    Debug.LogWarning($"[TileMapController] FIX-V2-7: Terrain TilemapRenderer был на слое " +
                        $"\"{renderer.sortingLayerName}\" → исправлено на \"Terrain\"");
                    renderer.sortingLayerName = "Terrain";
                    renderer.sortingOrder = 0;
                }
            }

            // Фиксим objects tilemap
            if (objectTilemap != null)
            {
                var renderer = objectTilemap.GetComponent<TilemapRenderer>();
                if (renderer != null && renderer.sortingLayerName != "Objects")
                {
                    Debug.LogWarning($"[TileMapController] FIX-V2-7: Objects TilemapRenderer был на слое " +
                        $"\"{renderer.sortingLayerName}\" → исправлено на \"Objects\"");
                    renderer.sortingLayerName = "Objects";
                    renderer.sortingOrder = 0;
                }
            }

            // Фиксим overlay tilemap (если есть)
            if (overlayTilemap != null)
            {
                var renderer = overlayTilemap.GetComponent<TilemapRenderer>();
                if (renderer != null && renderer.sortingLayerName != "Objects")
                {
                    renderer.sortingLayerName = "Objects";
                    renderer.sortingOrder = 10;
                }
            }
        }

        // === Tile Asset Auto-Creation ===

        /// <summary>
        /// FIX: Автоматически создать GameTile из спрайтов, если [SerializeField] поля не назначены.
        /// Это устраняет проблему "цветных точек" — когда тайлы не назначены в Inspector,
        /// Tilemap показывает пустые клетки. Метод загружает спрайты из Assets/Sprites/
        /// и создаёт GameTile экземпляры runtime.
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private void EnsureTileAssets()
        {
            // ==================================================================
            // TERRAIN: ВСЕГДА процедурные спрайты (Sprite.Create в рантайме).
            // PNG-спрайты через Unity Import Pipeline вызывают белую сетку
            // между тайлами (субпиксельные зазоры). Процедурные спрайты,
            // созданные через Sprite.Create(), не проходят Import Pipeline
            // и не имеют этой проблемы. Это рабочий подход из 14 апреля.
            // Графическая полировка terrain — на следующем этапе.
            // Редактировано: 2026-04-17 11:44 UTC
            // ==================================================================
            grassTile = ForceProceduralTerrainTile("terrain_grass", true, TerrainType.Grass);
            dirtTile = ForceProceduralTerrainTile("terrain_dirt", true, TerrainType.Dirt);
            stoneTile = ForceProceduralTerrainTile("terrain_stone", true, TerrainType.Stone);
            waterShallowTile = ForceProceduralTerrainTile("terrain_water_shallow", false, TerrainType.Water_Shallow);
            waterDeepTile = ForceProceduralTerrainTile("terrain_water_deep", false, TerrainType.Water_Deep);
            sandTile = ForceProceduralTerrainTile("terrain_sand", true, TerrainType.Sand);
            voidTile = ForceProceduralTerrainTile("terrain_void", false, TerrainType.Void);
            snowTile = ForceProceduralTerrainTile("terrain_snow", true, TerrainType.Snow);
            iceTile = ForceProceduralTerrainTile("terrain_ice", false, TerrainType.Ice);
            lavaTile = ForceProceduralTerrainTile("terrain_lava", false, TerrainType.Lava);

            // ==================================================================
            // OBJECTS: Загрузка из PNG (объекты не имеют проблемы на стыках).
            // Fallback на процедурные спрайты если PNG не найден.
            // Редактировано: 2026-04-16 08:03 UTC
            // ==================================================================
            treeTile = EnsureTile(treeTile, "obj_tree", false, TerrainType.Grass);
            treeOakTile = EnsureTile(treeOakTile, "obj_tree_oak", false, TerrainType.Grass);
            treePineTile = EnsureTile(treePineTile, "obj_tree_pine", false, TerrainType.Grass);
            treeBirchTile = EnsureTile(treeBirchTile, "obj_tree_birch", false, TerrainType.Grass);
            rockSmallTile = EnsureTile(rockSmallTile, "obj_rock_small", false, TerrainType.Grass);
            rockMediumTile = EnsureTile(rockMediumTile, "obj_rock_medium", false, TerrainType.Grass);
            bushTile = EnsureTile(bushTile, "obj_bush", true, TerrainType.Grass);
            bushBerryTile = EnsureTile(bushBerryTile, "obj_bush_berry", true, TerrainType.Grass);
            chestTile = EnsureTile(chestTile, "obj_chest", true, TerrainType.Grass);
            oreVeinTile = EnsureTile(oreVeinTile, "obj_ore_vein", false, TerrainType.Stone);
            herbTile = EnsureTile(herbTile, "obj_herb", true, TerrainType.Grass);

            // FIX: cellGap = 0. Pixel bleed через terrain-спрайты устраняет зазоры.
            // Редактировано: 2026-04-15 UTC
            var grid = GetComponentInParent<Grid>();
            if (grid != null)
            {
                grid.cellGap = Vector3.zero;
            }
        }

        /// <summary>
        /// ВСЕГДА создаёт terrain GameTile с процедурным спрайтом (Sprite.Create).
        /// PNG-спрайты через Unity Import Pipeline вызывают белую сетку между
        /// terrain-тайлами (субпиксельные зазоры на стыках). Sprite.Create()
        /// обходит Import Pipeline → нет зазоров. Это рабочий подход 14 апреля.
        /// Вызывается для terrain-тайлов в EnsureTileAssets() вместо EnsureTile().
        /// Редактировано: 2026-04-17 11:44 UTC
        /// </summary>
        private TileBase ForceProceduralTerrainTile(string spriteName, bool passable, TerrainType terrain)
        {
            Sprite sprite = CreateProceduralTileSprite(spriteName, terrain);
            if (sprite == null)
            {
                Debug.LogWarning($"[TileMapController] Не удалось создать процедурный terrain-тайл: {spriteName}");
                return null;
            }

            var tile = ScriptableObject.CreateInstance<GameTile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.terrainType = terrain;
            tile.isPassable = passable;
            tile.moveCost = passable ? 1f : 0f;
            tile.flags = passable ? GameTileFlags.Passable : GameTileFlags.None;
            Debug.Log($"[TileMapController] Процедурный terrain-тайл: {spriteName} (Sprite.Create, passable={passable})");
            return tile;
        }

        /// <summary>
        /// Создать GameTile из спрайта, если поле не назначено.
        /// Загружает спрайт из Assets/Sprites/Tiles/.
        /// Fallback: создаёт процедурный спрайт.
        /// Используется ТОЛЬКО для object-тайлов (terrain — через ForceProceduralTerrainTile).
        /// Редактировано: 2026-04-17 11:44 UTC
        /// </summary>
        private TileBase EnsureTile(TileBase currentTile, string spriteName, bool passable, TerrainType terrain)
        {
            if (currentTile != null) return currentTile;

            Sprite loadedSprite = LoadTileSprite(spriteName);
            if (loadedSprite == null)
            {
                // Fallback: создать процедурный спрайт
                loadedSprite = CreateProceduralTileSprite(spriteName, terrain);
            }

            if (loadedSprite != null)
            {
                var tile = ScriptableObject.CreateInstance<GameTile>();
                tile.sprite = loadedSprite;
                tile.color = Color.white;
                tile.terrainType = terrain;
                tile.isPassable = passable;
                tile.moveCost = passable ? 1f : 0f;
                tile.flags = passable ? GameTileFlags.Passable : GameTileFlags.None;
                Debug.Log($"[TileMapController] Автосоздан GameTile: {spriteName} (passable={passable})");
                return tile;
            }

            Debug.LogWarning($"[TileMapController] Не удалось создать тайл: {spriteName}");
            return null;
        }

        /// <summary>
        /// Загрузить спрайт из Assets/Sprites/.
        /// СРЕД-1 FIX: Перед загрузкой — принудительный реимпорт с правильными настройками.
        /// Редактировано: 2026-04-16
        /// </summary>
        private Sprite LoadTileSprite(string spriteName)
        {
#if UNITY_EDITOR
            // FIX: Tiles_AI/ убран из поиска — AI-спрайты 1024×1024 RGB без альфа-канала.
            // Обработанные AI-спрайты лежат в Tiles/ (64×64 RGBA с прозрачностью).
            // Редактировано: 2026-04-15 17:14:21 UTC
            string[] searchPaths = new string[]
            {
                $"Assets/Sprites/Tiles/{spriteName}.png"
            };
            foreach (var path in searchPaths)
            {
                // СРЕД-1 FIX: Убедиться, что спрайт импортирован корректно перед загрузкой
                // Редактировано: 2026-04-16
                bool isObject = spriteName.StartsWith("obj_");
                EnsureTileSpriteImportSettings(path, isObject);
                var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null) return sprite;
            }
#else
            // В build версии — загрузка из Resources
            var resSprite = UnityEngine.Resources.Load<Sprite>($"Sprites/{spriteName}");
            if (resSprite != null) return resSprite;
#endif
            return null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// PPU=32 для ВСЕХ спрайтов — единое рабочее значение из 14 апреля.
        /// 64/32 = 2.0 юнита = ТОЧНО в ячейку Grid(2,2,1) → нет зазоров.
        /// Редактировано: 2026-04-17 10:53 UTC
        /// </summary>
        private void EnsureTileSpriteImportSettings(string assetPath, bool isObject)
        {
            var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
            if (importer == null) return;

            // PPU=32 для terrain и objects — единое рабочее значение
            // Редактировано: 2026-04-17 10:53 UTC
            int targetPPU = 32;
            bool needsReimport = importer.textureType != UnityEditor.TextureImporterType.Sprite
                || importer.spritePixelsPerUnit != targetPPU
                || importer.alphaIsTransparency != true;

            if (needsReimport)
            {
                importer.textureType = UnityEditor.TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = targetPPU;
                importer.alphaIsTransparency = true;
                importer.spriteImportMode = UnityEditor.SpriteImportMode.Single;
                importer.textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point; // Point — чёткие края
                UnityEditor.AssetDatabase.ImportAsset(assetPath, UnityEditor.ImportAssetOptions.ForceUpdate);
                UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceUpdate);
                Debug.Log($"[TileMapController] Спрайт реимпортирован: {assetPath} → PPU={targetPPU}");
            }
        }
#endif

        /// <summary>
        /// Создать процедурный спрайт тайла (Sprite.Create — без PNG-файла).
        /// ИСПОЛЬЗУЕТСЯ КАК ОСНОВНОЙ МЕТОД для terrain-тайлов.
        /// PNG-спрайты через Import Pipeline вызывают белую сетку —
        /// Sprite.Create() обходит pipeline → нет зазоров.
        /// Для terrain: 64×64 PPU=32 Point → 2.0 юнита = точно в ячейку.
        /// Редактировано: 2026-04-17 11:44 UTC — terrain-спрайты ВСЕГДА процедурные.
        /// </summary>
        private Sprite CreateProceduralTileSprite(string spriteName, TerrainType terrain)
        {
            bool isObject = spriteName.StartsWith("obj_");

            // PPU=32 для terrain и objects — единое рабочее значение из 14 апреля.
            // 64×64 при PPU=32 → 64/32 = 2.0 юнита = ТОЧНО в ячейку Grid(2,2,1).
            // При PPU=32 нет зазоров между тайлами (проверено 14 апреля).
            // Редактировано: 2026-04-17 10:53 UTC
            int texSize = 64;
            int ppu = 32;

            Texture2D texture = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point; // Point — чёткие края
            texture.wrapMode = TextureWrapMode.Clamp;

            Color color = GetTerrainColor(terrain);

            for (int x = 0; x < texSize; x++)
            {
                for (int y = 0; y < texSize; y++)
                {
                    if (isObject)
                    {
                        // Объектные спрайты — прозрачный фон
                        texture.SetPixel(x, y, Color.clear);
                    }
                    else
                    {
                        // Тайловые спрайты — сплошная заливка (pixel bleed)
                        float variation = UnityEngine.Random.Range(0.95f, 1.05f);
                        Color pixelColor = color * variation;
                        pixelColor.a = color.a;
                        texture.SetPixel(x, y, pixelColor);
                    }
                }
            }

            // Для объектных спрайтов — нарисовать простую форму (уменьшенную)
            if (isObject)
            {
                int cx = texSize / 2;
                int cy = texSize / 2;
                if (spriteName.Contains("tree"))
                {
                    DrawRectOnTexture(texture, cx - 3, 8, 6, 24, new Color(0.4f, 0.25f, 0.15f));
                    for (int y = 24; y < 56; y++)
                    {
                        int width = (56 - y) / 3;
                        DrawRectOnTexture(texture, cx - width, y, width * 2, 1, color);
                    }
                }
                else if (spriteName.Contains("rock"))
                {
                    DrawEllipseOnTexture(texture, cx, cy - 2, 8, 6, color);
                    DrawEllipseOnTexture(texture, cx, cy - 2, 6, 4, color * 1.1f);
                }
                else if (spriteName.Contains("ore"))
                {
                    DrawEllipseOnTexture(texture, cx, cy - 3, 10, 8, new Color(0.45f, 0.4f, 0.35f));
                    DrawEllipseOnTexture(texture, cx - 3, cy - 2, 4, 3, new Color(0.8f, 0.6f, 0.2f));
                }
                else if (spriteName.Contains("bush"))
                {
                    DrawEllipseOnTexture(texture, cx - 5, cy, 7, 8, color);
                    DrawEllipseOnTexture(texture, cx + 5, cy, 7, 8, color * 0.9f);
                    DrawEllipseOnTexture(texture, cx, cy + 3, 10, 7, color * 1.1f);
                }
                else if (spriteName.Contains("chest"))
                {
                    DrawRectOnTexture(texture, cx - 8, cy - 6, 16, 12, color);
                    DrawRectOnTexture(texture, cx - 9, cy + 4, 18, 4, color * 1.2f);
                }
                else if (spriteName.Contains("herb"))
                {
                    DrawRectOnTexture(texture, cx - 1, 10, 2, 20, new Color(0.2f, 0.4f, 0.15f));
                    DrawEllipseOnTexture(texture, cx, cy + 5, 6, 6, color);
                }
            }

            texture.Apply();

            // Все спрайты: rect (0,0,64,64) при PPU=32 → 2.0u = точно в ячейку Grid(2,2,1)
            // Редактировано: 2026-04-17 10:53 UTC
            return Sprite.Create(texture, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f), ppu);
        }

        /// <summary>
        /// Получить цвет для типа террейна (для процедурных спрайтов).
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private Color GetTerrainColor(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Grass => new Color(0.4f, 0.7f, 0.3f),
                TerrainType.Dirt => new Color(0.6f, 0.4f, 0.2f),
                TerrainType.Stone => new Color(0.5f, 0.5f, 0.55f),
                TerrainType.Water_Shallow => new Color(0.3f, 0.5f, 0.8f, 0.8f),
                TerrainType.Water_Deep => new Color(0.2f, 0.3f, 0.7f, 0.9f),
                TerrainType.Sand => new Color(0.9f, 0.85f, 0.6f),
                TerrainType.Void => new Color(0.1f, 0.1f, 0.1f),
                TerrainType.Snow => new Color(0.95f, 0.95f, 1f),
                TerrainType.Ice => new Color(0.7f, 0.85f, 0.95f),
                TerrainType.Lava => new Color(0.9f, 0.3f, 0.05f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }

        /// <summary>
        /// Нарисовать прямоугольник на текстуре (для процедурных спрайтов).
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private void DrawRectOnTexture(Texture2D texture, int x, int y, int w, int h, Color color)
        {
            for (int px = x; px < x + w && px < texture.width; px++)
            {
                for (int py = y; py < y + h && py < texture.height; py++)
                {
                    if (px >= 0 && py >= 0) texture.SetPixel(px, py, color);
                }
            }
        }

        /// <summary>
        /// Нарисовать эллипс на текстуре (для процедурных спрайтов).
        /// Редактировано: 2026-04-15 11:15:00 UTC
        /// </summary>
        private void DrawEllipseOnTexture(Texture2D texture, int cx, int cy, int rx, int ry, Color color)
        {
            for (int x = cx - rx; x <= cx + rx; x++)
            {
                for (int y = cy - ry; y <= cy + ry; y++)
                {
                    float dx = (x - cx) / (float)rx;
                    float dy = (y - cy) / (float)ry;
                    if (dx * dx + dy * dy <= 1f && x >= 0 && y >= 0 && x < texture.width && y < texture.height)
                    {
                        texture.SetPixel(x, y, color);
                    }
                }
            }
        }

        // === Map Generation ===

        /// <summary>
        /// Создать тестовую карту.
        /// </summary>
        [ContextMenu("Generate Test Map")]
        public void GenerateTestMap()
        {
            GenerateMap(defaultWidth, defaultHeight, "Test Location");
        }

        /// <summary>
        /// Сгенерировать карту.
        /// </summary>
        public void GenerateMap(int width, int height, string name = "Location")
        {
            mapData = new TileMapData(width, height, name);
            
            // Заполнить базовой поверхностью
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    mapData.CreateTile(x, y, TerrainType.Grass);
                }
            }

            // Добавить тестовые объекты
            AddTestFeatures();

            // Отрисовать карту
            RenderMap();

            OnMapGenerated?.Invoke(mapData);
            Debug.Log($"Generated map: {width}x{height} tiles ({width * 2}x{height * 2} meters)");
        }

        /// <summary>
        /// Добавить тестовые элементы на карту.
        /// Процедурная генерация с биомами, реками, зонами Ци.
        /// Редактировано: 2026-04-14 08:00:00 UTC — полная переработка для карты 100×80
        /// </summary>
        private void AddTestFeatures()
        {
            var random = new System.Random(mapData.seed);
            int width = mapData.width;
            int height = mapData.height;
            int centerX = width / 2;
            int centerY = height / 2;

            // === БИОМ 1: Песчаный берег (левый нижний угол, 15×10) ===
            GenerateBiomeRect(0, 0, 15, 10, TerrainType.Sand, random);

            // === БИОМ 2: Пустыня (правый нижний угол, 20×12) ===
            GenerateBiomeRect(width - 20, 0, 20, 12, TerrainType.Sand, random);
            // Песчаные дюны — вариация высот через случайные камни
            for (int i = 0; i < 15; i++)
            {
                int x = random.Next(width - 18, width - 2);
                int y = random.Next(2, 10);
                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.terrain == TerrainType.Sand && tile.objects.Count == 0)
                {
                    tile.AddObject(new TileObjectData(TileObjectType.Rock_Small));
                }
            }

            // === БИОМ 3: Каменные холмы (верхний левый угол, 18×15) ===
            GenerateBiomeEllipse(9, height - 8, 9, 8, TerrainType.Stone, random);

            // === БИОМ 4: Горный хребет (верхний правый угол) ===
            GenerateBiomeEllipse(width - 12, height - 10, 10, 10, TerrainType.Stone, random);
            // Снежные пики — снежный биом вокруг гор (шире чем ледяные вершины)
            // Редактировано: 2026-04-14 14:14:00 UTC
            GenerateBiomeEllipse(width - 12, height - 8, 7, 6, TerrainType.Snow, random);
            // Ледяные вершины (самый центр горного хребта)
            GenerateBiomeEllipse(width - 12, height - 6, 3, 3, TerrainType.Ice, random);

            // === БИОМ 5: Снежная тундра (верхний левый угол, рядом с каменными холмами) ===
            // Редактировано: 2026-04-14 14:14:00 UTC
            GenerateBiomeEllipse(15, height - 12, 8, 7, TerrainType.Snow, random);
            // Ледяное озеро в тундре
            GenerateBiomeEllipse(15, height - 14, 3, 2, TerrainType.Ice, random);

            // === ОЗЕРО 1: Левый нижний (рядом с песком) ===
            GenerateLake(10, 10, 5, 4, random);

            // === ОЗЕРО 2: Правый верхний ===
            GenerateLake(width - 15, height - 10, 5, 4, random);

            // === ОЗЕРО 3: Центральное (малое) ===
            GenerateLake(centerX + 15, centerY - 5, 3, 3, random);

            // === РЕКА: Вертикальная от верхнего озера вниз ===
            GenerateRiver(width - 15, height - 14, width - 15, height / 2, 2, random);

            // === РЕКА 2: Горизонтальная от левого озера ===
            GenerateRiver(15, 10, width / 3, 10, 2, random);

            // === ЛАВОВОЕ ОЗЕРО (нижний центр) ===
            GenerateLavaLake(centerX, 7, 6, 4, random);

            // === ЗОНА 5: Земляные дороги ===
            // Главная дорога: горизонталь через центр
            for (int x = 3; x < width - 3; x++)
            {
                for (int offset = 0; offset < 2; offset++)
                {
                    var tile = mapData.GetTile(x, centerY + offset);
                    if (tile != null && tile.terrain == TerrainType.Grass)
                    {
                        tile.terrain = TerrainType.Dirt;
                        tile.UpdateTerrainProperties();
                    }
                }
            }

            // Вертикальная дорога через центр
            for (int y = 3; y < height - 3; y++)
            {
                for (int offset = 0; offset < 2; offset++)
                {
                    var tile = mapData.GetTile(centerX + offset, y);
                    if (tile != null && tile.terrain == TerrainType.Grass)
                    {
                        tile.terrain = TerrainType.Dirt;
                        tile.UpdateTerrainProperties();
                    }
                }
            }

            // === ЗОНА 6: Каменная площадка в центре (алтарь) ===
            for (int x = centerX - 5; x <= centerX + 5; x++)
            {
                for (int y = centerY - 5; y <= centerY + 5; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile != null)
                    {
                        tile.terrain = TerrainType.Stone;
                        tile.UpdateTerrainProperties();
                    }
                }
            }

            // Алтарь — высокая плотность Ци
            var altarTile = mapData.GetTile(centerX, centerY);
            if (altarTile != null)
            {
                altarTile.baseQiDensity = 500;
                altarTile.currentQiDensity = 500;
            }

            // Кольцо алтаря — средняя плотность Ци
            for (int x = centerX - 4; x <= centerX + 4; x++)
            {
                for (int y = centerY - 4; y <= centerY + 4; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile != null && !(x == centerX && y == centerY))
                    {
                        tile.baseQiDensity = 200;
                        tile.currentQiDensity = 200;
                    }
                }
            }

            // === ЗОНА 7: Зона медитации (восток от центра) ===
            GenerateQiZone(centerX + 20, centerY + 5, 4, 150, random);

            // === ЗОНА 8: Зона медитации (запад от центра) ===
            GenerateQiZone(centerX - 25, centerY - 3, 3, 120, random);

            // === РАСТИТЕЛЬНОСТЬ: Густой лес (левая треть карты) ===
            for (int i = 0; i < 150; i++)
            {
                int x = random.Next(3, width / 3);
                int y = random.Next(3, height - 3);

                if (IsInSpecialZone(x, y, centerX, centerY)) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var treeObj = new TileObjectData(
                        random.Next(3) == 0 ? TileObjectType.Tree_Pine :
                        random.Next(3) == 0 ? TileObjectType.Tree_Birch :
                        TileObjectType.Tree_Oak
                    );
                    tile.AddObject(treeObj);
                }
            }

            // Средний лес (центральная треть)
            for (int i = 0; i < 80; i++)
            {
                int x = random.Next(width / 3, 2 * width / 3);
                int y = random.Next(3, height - 3);

                if (IsInSpecialZone(x, y, centerX, centerY)) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var treeObj = new TileObjectData(
                        random.Next(4) == 0 ? TileObjectType.Tree_Pine : TileObjectType.Tree_Oak
                    );
                    tile.AddObject(treeObj);
                }
            }

            // Редкий лес (правая треть — степь)
            for (int i = 0; i < 40; i++)
            {
                int x = random.Next(2 * width / 3, width - 3);
                int y = random.Next(3, height - 3);

                if (IsInSpecialZone(x, y, centerX, centerY)) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var treeObj = new TileObjectData(TileObjectType.Tree_Oak);
                    tile.AddObject(treeObj);
                }
            }

            // === КУСТЫ (разбросаны по карте) ===
            for (int i = 0; i < 70; i++)
            {
                int x = random.Next(3, width - 3);
                int y = random.Next(3, height - 3);

                if (IsInSpecialZone(x, y, centerX, centerY)) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.objects.Count == 0 && tile.IsPassable())
                {
                    var bushObj = new TileObjectData(
                        random.Next(4) == 0 ? TileObjectType.Bush_Berry : TileObjectType.Bush
                    );
                    tile.AddObject(bushObj);
                }
            }

            // === КАМНИ (разбросаны по карте, больше в каменных зонах) ===
            for (int i = 0; i < 45; i++)
            {
                int x = random.Next(3, width - 3);
                int y = random.Next(3, height - 3);

                if (IsInSpecialZone(x, y, centerX, centerY)) continue;

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var rockObj = new TileObjectData(
                        random.Next(3) == 0 ? TileObjectType.Rock_Medium : TileObjectType.Rock_Small
                    );
                    tile.AddObject(rockObj);
                }
            }

            // === РУДНЫЕ ЖИЛЫ (рядом с каменными зонами) ===
            for (int i = 0; i < 20; i++)
            {
                int x = random.Next(0, width);
                int y = random.Next(0, height);

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.terrain == TerrainType.Stone && tile.objects.Count == 0)
                {
                    var oreObj = new TileObjectData(TileObjectType.OreVein);
                    tile.AddObject(oreObj);
                }
            }

            // === СУНДУКИ (редкие, на проходимых тайлах) ===
            for (int i = 0; i < 10; i++)
            {
                int x = random.Next(5, width - 5);
                int y = random.Next(5, height - 5);

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.IsPassable() && tile.objects.Count == 0)
                {
                    var chestObj = new TileObjectData(TileObjectType.Chest);
                    tile.AddObject(chestObj);
                }
            }

            // === ТРАВЫ И ЦВЕТЫ (по всей карте) ===
            for (int i = 0; i < 60; i++)
            {
                int x = random.Next(3, width - 3);
                int y = random.Next(3, height - 3);

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.objects.Count == 0 && tile.terrain == TerrainType.Grass)
                {
                    var herbObj = new TileObjectData(
                        random.Next(3) == 0 ? TileObjectType.Herb :
                        random.Next(2) == 0 ? TileObjectType.Flower :
                        TileObjectType.Grass_Tall
                    );
                    tile.AddObject(herbObj);
                }
            }

            // === ЗИМНИЕ ТРАВЫ (в снежных биомах) ===
            // Редактировано: 2026-04-14 14:14:00 UTC
            for (int i = 0; i < 25; i++)
            {
                int x = random.Next(3, width - 3);
                int y = random.Next(height / 2, height - 3);

                var tile = mapData.GetTile(x, y);
                if (tile != null && tile.objects.Count == 0 && tile.terrain == TerrainType.Snow)
                {
                    var herbObj = new TileObjectData(TileObjectType.Herb);
                    tile.AddObject(herbObj);
                }
            }

            // === ГРАНИЦА КАРТЫ — Void обрывы ===
            for (int x = 0; x < width; x++)
            {
                SetVoidIfGrass(x, 0);
                SetVoidIfGrass(x, 1);
                SetVoidIfGrass(x, height - 1);
                SetVoidIfGrass(x, height - 2);
            }
            for (int y = 0; y < height; y++)
            {
                SetVoidIfGrass(0, y);
                SetVoidIfGrass(1, y);
                SetVoidIfGrass(width - 1, y);
                SetVoidIfGrass(width - 2, y);
            }
        }

        /// <summary>
        /// Сгенерировать биом прямоугольником.
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateBiomeRect(int startX, int startY, int w, int h, TerrainType terrain, System.Random random)
        {
            for (int x = startX; x < startX + w && x < mapData.width; x++)
            {
                for (int y = startY; y < startY + h && y < mapData.height; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile != null)
                    {
                        tile.terrain = terrain;
                        tile.UpdateTerrainProperties();
                    }
                }
            }
        }

        /// <summary>
        /// Сгенерировать биом эллипсом.
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateBiomeEllipse(float cx, float cy, float rx, float ry, TerrainType terrain, System.Random random)
        {
            for (int x = (int)(cx - rx); x <= (int)(cx + rx); x++)
            {
                for (int y = (int)(cy - ry); y <= (int)(cy + ry); y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile != null)
                    {
                        float dx = (x - cx) / rx;
                        float dy = (y - cy) / ry;
                        if (dx * dx + dy * dy <= 1f)
                        {
                            tile.terrain = terrain;
                            tile.UpdateTerrainProperties();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Сгенерировать озеро (мелкая + глубокая вода).
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateLake(float cx, float cy, float rx, float ry, System.Random random)
        {
            for (int x = (int)(cx - rx - 1); x <= (int)(cx + rx + 1); x++)
            {
                for (int y = (int)(cy - ry - 1); y <= (int)(cy + ry + 1); y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile == null) continue;

                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    float dist = dx * dx + dy * dy;

                    if (dist <= 0.55f)
                    {
                        tile.terrain = TerrainType.Water_Deep;
                        tile.UpdateTerrainProperties();
                    }
                    else if (dist <= 1f)
                    {
                        tile.terrain = TerrainType.Water_Shallow;
                        tile.UpdateTerrainProperties();
                    }
                }
            }
        }

        /// <summary>
        /// Сгенерировать лавовое озеро.
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateLavaLake(float cx, float cy, float rx, float ry, System.Random random)
        {
            for (int x = (int)(cx - rx - 1); x <= (int)(cx + rx + 1); x++)
            {
                for (int y = (int)(cy - ry - 1); y <= (int)(cy + ry + 1); y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile == null) continue;

                    float dx = (x - cx) / rx;
                    float dy = (y - cy) / ry;
                    float dist = dx * dx + dy * dy;

                    if (dist <= 0.35f)
                    {
                        tile.terrain = TerrainType.Lava;
                        tile.UpdateTerrainProperties();
                    }
                    else if (dist <= 0.7f)
                    {
                        tile.terrain = TerrainType.Stone;
                        tile.UpdateTerrainProperties();
                    }
                }
            }
        }

        /// <summary>
        /// Сгенерировать реку от (x1,y1) до (x2,y2) шириной width тайлов.
        /// Простая прямая река.
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateRiver(int x1, int y1, int x2, int y2, int riverWidth, System.Random random)
        {
            // Bresenham-подобная линия
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int steps = Math.Max(dx, dy);

            if (steps == 0) return;

            float stepX = (float)(x2 - x1) / steps;
            float stepY = (float)(y2 - y1) / steps;

            for (int i = 0; i <= steps; i++)
            {
                int cx = (int)(x1 + stepX * i);
                int cy = (int)(y1 + stepY * i);

                for (int w = 0; w < riverWidth; w++)
                {
                    // Перпендикуляр к направлению реки
                    int px = -((int)stepY) * w / Math.Max(1, (int)(Math.Abs(stepX) + Math.Abs(stepY)));
                    int py = ((int)stepX) * w / Math.Max(1, (int)(Math.Abs(stepX) + Math.Abs(stepY)));

                    var tile = mapData.GetTile(cx + px, cy + py);
                    if (tile != null && tile.terrain != TerrainType.Stone && tile.terrain != TerrainType.Lava)
                    {
                        tile.terrain = w == 0 ? TerrainType.Water_Shallow : TerrainType.Water_Shallow;
                        tile.UpdateTerrainProperties();
                    }
                }
            }
        }

        /// <summary>
        /// Сгенерировать зону высокой плотности Ци.
        /// Редактировано: 2026-04-14 08:00:00 UTC
        /// </summary>
        private void GenerateQiZone(int cx, int cy, int radius, int qiDensity, System.Random random)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                for (int y = cy - radius; y <= cy + radius; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile == null) continue;

                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (dist <= radius)
                    {
                        // Плотность убывает к краю зоны
                        float falloff = 1f - (dist / radius);
                        tile.baseQiDensity = (int)(qiDensity * falloff);
                        tile.currentQiDensity = tile.baseQiDensity;
                    }
                }
            }
        }

        /// <summary>
        /// Установить Void если текущий тип — Grass (для границ карты).
        /// </summary>
        private void SetVoidIfGrass(int x, int y)
        {
            var tile = mapData?.GetTile(x, y);
            if (tile != null && tile.terrain == TerrainType.Grass)
            {
                tile.terrain = TerrainType.Void;
                tile.UpdateTerrainProperties();
            }
        }

        /// <summary>
        /// Проверить, находится ли тайл в специальной зоне (озёра, алтарь, лавовое озеро).
        /// Редактировано: 2026-04-14 08:00:00 UTC — обновлено для карты 100×80
        /// </summary>
        private bool IsInSpecialZone(int x, int y, int centerX, int centerY)
        {
            // Центральная каменная площадка (алтарь)
            if (Mathf.Abs(x - centerX) <= 6 && Mathf.Abs(y - centerY) <= 6)
                return true;

            // Озеро 1: левый нижний
            if (x >= 5 && x < 16 && y >= 5 && y < 15)
            {
                float dx = (x - 10f) / 5f;
                float dy = (y - 10f) / 4f;
                if (dx * dx + dy * dy <= 1.2f)
                    return true;
            }

            // Озеро 2: правый верхний
            if (x >= mapData.width - 21 && x < mapData.width - 9 && y >= mapData.height - 15 && y < mapData.height - 5)
            {
                float dx = (x - (mapData.width - 15f)) / 5f;
                float dy = (y - (mapData.height - 10f)) / 4f;
                if (dx * dx + dy * dy <= 1.2f)
                    return true;
            }

            // Озеро 3: центральное
            if (Mathf.Abs(x - (centerX + 15)) <= 4 && Mathf.Abs(y - (centerY - 5)) <= 4)
            {
                float dx = (x - (centerX + 15f)) / 3f;
                float dy = (y - (centerY - 5f)) / 3f;
                if (dx * dx + dy * dy <= 1.2f)
                    return true;
            }

            // Лавовое озеро
            if (Mathf.Abs(x - centerX) <= 7 && y >= 3 && y < 12)
                return true;

            // Песчаный берег (левый нижний)
            if (x < 15 && y < 10)
                return true;

            // Пустыня (правый нижний)
            if (x >= mapData.width - 20 && y < 12)
                return true;

            return false;
        }

        // === Rendering ===

        /// <summary>
        /// Отрисовать карту на Tilemap.
        /// </summary>
        public void RenderMap()
        {
            if (mapData == null || terrainTilemap == null) return;

            // Очистить
            terrainTilemap.ClearAllTiles();
            if (objectTilemap != null) objectTilemap.ClearAllTiles();

            // Отрисовать поверхность
            for (int x = 0; x < mapData.width; x++)
            {
                for (int y = 0; y < mapData.height; y++)
                {
                    var tile = mapData.GetTile(x, y);
                    if (tile == null) continue;

                    // Поверхность
                    TileBase terrainTile = GetTerrainTile(tile.terrain);
                    if (terrainTile != null)
                    {
                        terrainTilemap.SetTile(new Vector3Int(x, y, 0), terrainTile);
                    }

                    // Объекты
                    if (objectTilemap != null && tile.objects.Count > 0)
                    {
                        // Пропускать harvestable-объекты — они спавнятся как GameObject через HarvestableSpawner
                        // Чекпоинт: 04_15_harvest_system_plan.md §6.2
                        // Редактировано: 2026-04-16
                        var obj = tile.objects[0];
                        if (obj.isHarvestable)
                            continue;

                        TileBase objTile = GetObjectTile(obj.objectType);
                        if (objTile != null)
                        {
                            objectTilemap.SetTile(new Vector3Int(x, y, 0), objTile);
                        }
                    }
                }
            }

            // Обновить коллайдеры
            terrainTilemap.RefreshAllTiles();
            if (objectTilemap != null) objectTilemap.RefreshAllTiles();

            // FIX-V2-6: Диагностика отрендеренных тайлов
            // Редактировано: 2026-04-16 11:37 UTC
            int terrainCount = 0, objectCount = 0;
            BoundsInt bounds = terrainTilemap.cellBounds;
            foreach (var pos in bounds.allPositionsWithin)
            {
                if (terrainTilemap.GetTile(pos) != null) terrainCount++;
            }
            if (objectTilemap != null)
            {
                bounds = objectTilemap.cellBounds;
                foreach (var pos in bounds.allPositionsWithin)
                {
                    if (objectTilemap.GetTile(pos) != null) objectCount++;
                }
            }
            CultivationGame.Core.RenderPipelineLogger.LogTileRendered(terrainCount, objectCount);
        }

        /// <summary>
        /// Получить Tile для типа поверхности.
        /// </summary>
        private TileBase GetTerrainTile(TerrainType type)
        {
            return type switch
            {
                TerrainType.Grass => grassTile,
                TerrainType.Dirt => dirtTile,
                TerrainType.Stone => stoneTile,
                TerrainType.Water_Shallow => waterShallowTile,
                TerrainType.Water_Deep => waterDeepTile,
                TerrainType.Sand => sandTile,
                TerrainType.Void => voidTile,
                // FIX: Добавлены Snow, Ice и Lava
                // Редактировано: 2026-04-14 13:45:00 UTC
                TerrainType.Snow => snowTile,
                TerrainType.Ice => iceTile,
                TerrainType.Lava => lavaTile,
                _ => null
            };
        }

        /// <summary>
        /// Получить Tile для типа объекта.
        /// Редактировано: 2026-04-13 13:35:27 UTC — добавлены OreVein, Herb, Bush_Berry, Grass_Tall
        /// </summary>
        // FIX-R2: Подтипы деревьев возвращают свои tile, с fallback на универсальный.
        // Ранее: все Tree_Oak/Pine/Birch → treeTile (одинаковые).
        // Теперь: Tree_Oak → treeOakTile ?? treeTile (уникальный с fallback).
        // Редактировано: 2026-04-16 08:03 UTC
        private TileBase GetObjectTile(TileObjectType type)
        {
            return type switch
            {
                TileObjectType.Tree_Oak => treeOakTile ?? treeTile,
                TileObjectType.Tree_Pine => treePineTile ?? treeTile,
                TileObjectType.Tree_Birch => treeBirchTile ?? treeTile,
                TileObjectType.Rock_Small => rockSmallTile,
                TileObjectType.Rock_Medium => rockMediumTile,
                TileObjectType.Rock_Large => rockMediumTile,
                TileObjectType.Bush => bushTile,
                TileObjectType.Bush_Berry => bushBerryTile ?? bushTile,
                TileObjectType.Chest => chestTile,
                // FIX: OreVein и Herb теперь используют собственные tile поля
                // Редактировано: 2026-04-14 06:38:00 UTC
                TileObjectType.OreVein => oreVeinTile ?? rockMediumTile,
                TileObjectType.Herb => herbTile ?? bushTile,
                TileObjectType.Grass_Tall => herbTile ?? bushTile,
                TileObjectType.Flower => herbTile ?? bushTile,
                _ => null
            };
        }

        // === Tile Access ===

        /// <summary>
        /// Получить тайл по мировым координатам.
        /// </summary>
        public TileData GetTileAtWorld(Vector2 worldPos)
        {
            if (mapData == null) return null;
            var tilePos = mapData.WorldToTile(worldPos);
            return mapData.GetTile(tilePos.x, tilePos.y);
        }

        /// <summary>
        /// Получить тайл по координатам тайла.
        /// </summary>
        public TileData GetTile(int x, int y)
        {
            return mapData?.GetTile(x, y);
        }

        /// <summary>
        /// Изменить тип поверхности тайла.
        /// </summary>
        public void SetTerrain(int x, int y, TerrainType terrain)
        {
            var tile = mapData?.GetTile(x, y);
            if (tile == null) return;

            tile.terrain = terrain;
            tile.UpdateTerrainProperties();

            // Обновить отображение
            TileBase terrainTile = GetTerrainTile(terrain);
            if (terrainTile != null && terrainTilemap != null)
            {
                terrainTilemap.SetTile(new Vector3Int(x, y, 0), terrainTile);
            }

            OnTileChanged?.Invoke(tile);
        }

        /// <summary>
        /// Добавить объект на тайл.
        /// </summary>
        public void AddObject(int x, int y, TileObjectType objType)
        {
            var tile = mapData?.GetTile(x, y);
            if (tile == null) return;

            var obj = new TileObjectData(objType);
            tile.AddObject(obj);

            // Обновить отображение
            TileBase objTile = GetObjectTile(objType);
            if (objTile != null && objectTilemap != null)
            {
                objectTilemap.SetTile(new Vector3Int(x, y, 0), objTile);
            }

            OnTileChanged?.Invoke(tile);
        }

        /// <summary>
        /// Удалить объект с тайла.
        /// </summary>
        public void RemoveObject(int x, int y, TileObjectData obj)
        {
            var tile = mapData?.GetTile(x, y);
            if (tile == null) return;

            tile.RemoveObject(obj);

            // Обновить отображение
            if (objectTilemap != null)
            {
                objectTilemap.SetTile(new Vector3Int(x, y, 0), null);
            }

            OnTileChanged?.Invoke(tile);
        }

        // === Pathfinding Helpers ===

        /// <summary>
        /// Получить соседние тайлы.
        /// </summary>
        public List<TileData> GetNeighbors(int x, int y, bool includeDiagonals = false)
        {
            var neighbors = new List<TileData>();
            
            // 4-directional
            var dirs = new (int dx, int dy)[]
            {
                (0, 1), (0, -1), (1, 0), (-1, 0)
            };

            // Add diagonals if requested
            if (includeDiagonals)
            {
                dirs = new (int dx, int dy)[]
                {
                    (0, 1), (0, -1), (1, 0), (-1, 0),
                    (1, 1), (1, -1), (-1, 1), (-1, -1)
                };
            }

            foreach (var (dx, dy) in dirs)
            {
                var tile = mapData?.GetTile(x + dx, y + dy);
                if (tile != null)
                    neighbors.Add(tile);
            }

            return neighbors;
        }

        // === Gizmos ===

        private void OnDrawGizmosSelected()
        {
            if (mapData == null) return;

            // Рисовать границы карты
            Gizmos.color = Color.yellow;
            Vector3 size = new Vector3(mapData.width * 2, mapData.height * 2, 0);
            Vector3 center = size / 2;
            Gizmos.DrawWireCube(center, size);
        }
    }
}
