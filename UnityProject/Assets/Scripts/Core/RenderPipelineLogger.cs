// ============================================================================
// RenderPipelineLogger.cs — Диагностика рендер-пайплайна
// Cultivation World Simulator
// Создано: 2026-04-16 11:37 UTC
// Редактировано: 2026-04-17 12:50 UTC — FIX-SORT: Добавлен LogAllRendererState() + диагностика порядка слоёв
// ============================================================================
// FIX-V2-6: Статический класс для логирования состояния каждого этапа
// рендер-пайплайна. Выводит в Unity Console, видно пользователю.
// Позволяет быстро найти причину визуальных багов.
// ============================================================================

using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

namespace CultivationGame.Core
{
    /// <summary>
    /// Диагностическая система для рендер-пайплайна.
    /// Логирует состояние Sorting Layers, Tilemap, SpriteRenderer, Camera, Light2D.
    /// Вызывается из ключевых точек: FullSceneBuilder, TileMapController, PlayerVisual и т.д.
    /// Создано: 2026-04-16 11:37 UTC
    /// Редактировано: 2026-04-17 12:50 UTC
    /// </summary>
    public static class RenderPipelineLogger
    {
        private const string PREFIX = "[RPL]";

        // ====================================================================
        //  L1: SORTING LAYERS
        // ====================================================================

        /// <summary>
        /// Логирует все Sorting Layers: имя, ID, порядок.
        /// Вызывается из FullSceneBuilder.ExecuteTagsLayers() после создания слоёв.
        /// </summary>
        public static void LogSortingLayers()
        {
            Debug.Log($"{PREFIX}-L1 ====== SORTING LAYERS ======");

            SortingLayer[] layers = SortingLayer.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                Debug.Log($"{PREFIX}-L1   [{i}] \"{layers[i].name}\" (id={layers[i].id})");
            }

            // Проверка критических слоёв
            bool hasObjects = SortingLayer.NameToID("Objects") != 0 || LayerExists("Objects", layers);
            bool hasPlayer = SortingLayer.NameToID("Player") != 0 || LayerExists("Player", layers);
            bool hasTerrain = SortingLayer.NameToID("Terrain") != 0 || LayerExists("Terrain", layers);

            Debug.Log($"{PREFIX}-L1 {(hasObjects ? "✅" : "❌")} Sorting layer \"Objects\" {(hasObjects ? "существует" : "НЕ СУЩЕСТВУЕТ — спрайты невидимы!")}");
            Debug.Log($"{PREFIX}-L1 {(hasPlayer ? "✅" : "⚠️")} Sorting layer \"Player\" {(hasPlayer ? "существует" : "не найден")}");
            Debug.Log($"{PREFIX}-L1 {(hasTerrain ? "✅" : "⚠️")} Sorting layer \"Terrain\" {(hasTerrain ? "существует" : "не найден")}");
        }

        private static bool LayerExists(string name, SortingLayer[] layers)
        {
            foreach (var layer in layers)
            {
                if (layer.name == name) return true;
            }
            return false;
        }

        // ====================================================================
        //  L2: TILEMAP STATE
        // ====================================================================

        /// <summary>
        /// Логирует состояние TilemapRenderer'ов: sortingLayer, sortingOrder, cellSize.
        /// Вызывается из FullSceneBuilder.ExecuteTilemap() после создания.
        /// </summary>
        public static void LogTilemapState()
        {
            Debug.Log($"{PREFIX}-L2 ====== TILEMAP STATE ======");

            Grid grid = Object.FindFirstObjectByType<Grid>();
            if (grid != null)
            {
                Debug.Log($"{PREFIX}-L2 Grid: cellSize={grid.cellSize}, cellGap={grid.cellGap}");
            }
            else
            {
                Debug.LogWarning($"{PREFIX}-L2 ❌ Grid НЕ НАЙДЕН!");
                return;
            }

            TilemapRenderer[] renderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            foreach (var renderer in renderers)
            {
                Tilemap tilemap = renderer.GetComponent<Tilemap>();
                int tileCount = 0;
                if (tilemap != null)
                {
                    BoundsInt bounds = tilemap.cellBounds;
                    foreach (var pos in bounds.allPositionsWithin)
                    {
                        if (tilemap.GetTile(pos) != null) tileCount++;
                    }
                }

                Debug.Log($"{PREFIX}-L2 {renderer.name}: " +
                    $"sortingLayer=\"{renderer.sortingLayerName}\"(id={renderer.sortingLayerID}), " +
                    $"sortingOrder={renderer.sortingOrder}, " +
                    $"mode={renderer.mode}, " +
                    $"tiles={tileCount}");
            }
        }

        // ====================================================================
        //  L3: SPRITE IMPORT STATE
        // ====================================================================

        /// <summary>
        /// Логирует состояние импорта спрайта: PPU, alphaIsTransparency, filterMode.
        /// Вызывается из TileMapController.EnsureTileAssets() после загрузки спрайта.
        /// </summary>
        public static void LogSpriteImportState(string assetPath, Sprite sprite)
        {
            if (sprite != null)
            {
                Debug.Log($"{PREFIX}-L3 Sprite \"{sprite.name}\": " +
                    $"loaded=true, PPU={sprite.pixelsPerUnit}, " +
                    $"bounds=({sprite.bounds.size.x:F3}, {sprite.bounds.size.y:F3}), " +
                    $"rect=({sprite.rect.width}×{sprite.rect.height}), " +
                    $"path={assetPath}");
            }
            else
            {
                Debug.LogWarning($"{PREFIX}-L3 ❌ Sprite=null для path={assetPath}");
            }
        }

        // ====================================================================
        //  L4: TILE RENDERED
        // ====================================================================

        /// <summary>
        /// Логирует кол-во отрендеренных тайлов.
        /// Вызывается из TileMapController.RenderMap() после рендеринга.
        /// </summary>
        public static void LogTileRendered(int terrainCount, int objectCount)
        {
            Debug.Log($"{PREFIX}-L4 ====== TILES RENDERED ======");
            Debug.Log($"{PREFIX}-L4 Terrain: {terrainCount} тайлов");
            Debug.Log($"{PREFIX}-L4 Objects: {objectCount} тайлов");
            Debug.Log($"{PREFIX}-L4 {(terrainCount > 0 ? "✅" : "❌")} Terrain имеет тайлы");
            Debug.Log($"{PREFIX}-L4 {(objectCount > 0 ? "✅" : "⚠️")} Objects имеет тайлы");
        }

        // ====================================================================
        //  L5: PLAYER VISUAL
        // ====================================================================

        /// <summary>
        /// Логирует состояние PlayerVisual: sortingLayer, shader, sprite bounds.
        /// Вызывается из PlayerVisual.CreateVisual() после создания.
        /// </summary>
        public static void LogPlayerVisualState(SpriteRenderer mainSr, SpriteRenderer shadowSr = null)
        {
            Debug.Log($"{PREFIX}-L5 ====== PLAYER VISUAL ======");

            if (mainSr != null)
            {
                string spriteInfo = mainSr.sprite != null
                    ? $"loaded=true, PPU={mainSr.sprite.pixelsPerUnit}, bounds=({mainSr.sprite.bounds.size.x:F3}, {mainSr.sprite.bounds.size.y:F3}), name=\"{mainSr.sprite.name}\""
                    : "❌ NULL";

                Debug.Log($"{PREFIX}-L5 MainSprite: {spriteInfo}");
                Debug.Log($"{PREFIX}-L5 MainSorting: layer=\"{mainSr.sortingLayerName}\"(id={mainSr.sortingLayerID}), order={mainSr.sortingOrder}");
                Debug.Log($"{PREFIX}-L5 MainMaterial: shader=\"{(mainSr.material != null ? mainSr.material.shader.name : "NULL")}\"");
                Debug.Log($"{PREFIX}-L5 MainColor: {mainSr.color}");
                Debug.Log($"{PREFIX}-L5 {(mainSr.sprite != null ? "✅" : "❌")} Player sprite загружен");
            }
            else
            {
                Debug.LogWarning($"{PREFIX}-L5 ❌ MainSpriteRenderer = NULL!");
            }

            if (shadowSr != null)
            {
                Debug.Log($"{PREFIX}-L5 ShadowSorting: layer=\"{shadowSr.sortingLayerName}\"(id={shadowSr.sortingLayerID}), order={shadowSr.sortingOrder}");
            }
        }

        // ====================================================================
        //  L6: HARVESTABLE SPAWN
        // ====================================================================

        /// <summary>
        /// Логирует состояние созданных harvestable-объектов.
        /// Вызывается из HarvestableSpawner.SpawnHarvestables() после спавна.
        /// </summary>
        public static void LogHarvestableState(List<GameObject> spawned, int skipped)
        {
            Debug.Log($"{PREFIX}-L6 ====== HARVESTABLE SPAWN ======");
            Debug.Log($"{PREFIX}-L6 Spawned: {spawned.Count} объектов, {skipped} пропущено");

            // Логируем до 3 примеров
            int sampleCount = Mathf.Min(spawned.Count, 3);
            for (int i = 0; i < sampleCount; i++)
            {
                var sr = spawned[i].GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    string spriteInfo = sr.sprite != null ? "loaded" : "❌ NULL";
                    Debug.Log($"{PREFIX}-L6 Sample[{i}]: {spawned[i].name} → " +
                        $"layer=\"{sr.sortingLayerName}\"(id={sr.sortingLayerID}), " +
                        $"order={sr.sortingOrder}, " +
                        $"shader=\"{(sr.material != null ? sr.material.shader.name : "NULL")}\", " +
                        $"sprite={spriteInfo}");
                }
                else
                {
                    Debug.LogWarning($"{PREFIX}-L6 Sample[{i}]: {spawned[i].name} → ❌ НЕТ SpriteRenderer!");
                }
            }

            Debug.Log($"{PREFIX}-L6 {(spawned.Count > 0 ? "✅" : "❌")} Harvestable объекты созданы");
        }

        // ====================================================================
        //  L7: CAMERA STATE
        // ====================================================================

        /// <summary>
        /// Логирует состояние камеры: orthographic, position, size.
        /// Вызывается из PlayerVisual.Start() или FullSceneBuilder.ExecuteCameraLight().
        /// </summary>
        public static void LogCameraState()
        {
            Debug.Log($"{PREFIX}-L7 ====== CAMERA STATE ======");

            Camera cam = Camera.main;
            if (cam != null)
            {
                Debug.Log($"{PREFIX}-L7 Camera: orthographic={cam.orthographic}, " +
                    $"position={cam.transform.position}, " +
                    $"orthographicSize={cam.orthographicSize}, " +
                    $"backgroundColor={cam.backgroundColor}, " +
                    $"clearFlags={cam.clearFlags}");
                Debug.Log($"{PREFIX}-L7 ✅ Camera найдена");
            }
            else
            {
                Debug.LogWarning($"{PREFIX}-L7 ❌ Camera.main = NULL!");
            }
        }

        // ====================================================================
        //  L8: LIGHT STATE
        // ====================================================================

        /// <summary>
        /// Логирует состояние Light2D.
        /// Вызывается из FullSceneBuilder.ExecuteCameraLight() после создания.
        /// Редактировано: 2026-04-25 — Multi-assembly поиск Light2D типа (как в Phase04).
        /// </summary>
        public static void LogLightState()
        {
            Debug.Log($"{PREFIX}-L8 ====== LIGHT STATE ======");

            var light2DObj = GameObject.Find("GlobalLight2D");
            if (light2DObj != null)
            {
                // Multi-assembly поиск Light2D типа (как в Phase04)
                var light2DType = ResolveLight2DType();
                if (light2DType != null)
                {
                    var component = light2DObj.GetComponent(light2DType);
                    if (component != null)
                    {
                        var intensityProp = light2DType.GetProperty("intensity");
                        var colorProp = light2DType.GetProperty("color");
                        var lightTypeProp = light2DType.GetProperty("lightType");

                        float intensity = intensityProp != null ? (float)intensityProp.GetValue(component) : 0f;
                        Color color = colorProp != null ? (Color)colorProp.GetValue(component) : Color.white;
                        int lightType = lightTypeProp != null ? (int)lightTypeProp.GetValue(component) : -1;

                        Debug.Log($"{PREFIX}-L8 GlobalLight2D: type={lightType} (1=Global), " +
                            $"intensity={intensity}, color={color}, " +
                            $"сборка={light2DType.Assembly.GetName().Name}");
                        Debug.Log($"{PREFIX}-L8 ✅ Light2D найден");
                    }
                    else
                    {
                        Debug.LogWarning($"{PREFIX}-L8 ⚠️ GlobalLight2D объект есть, " +
                            $"тип найден ({light2DType.Assembly.GetName().Name}), " +
                            $"но компонент Light2D не найден на объекте!");
                    }
                }
                else
                {
                    Debug.LogWarning($"{PREFIX}-L8 ⚠️ Тип Light2D не найден ни в одной сборке! " +
                        "Сборки Unity.2D.RenderPipeline.Runtime, Unity.RenderPipeline.Universal.2D.Runtime, " +
                        "Unity.RenderPipeline.Universal.Runtime — ни одна не содержит Light2D.");
                    LogLight2DDiagnostics();
                }
            }
            else
            {
                Debug.LogWarning($"{PREFIX}-L8 ❌ GlobalLight2D НЕ НАЙДЕН — спрайты будут чёрными (Sprite-Lit-Default)!");
            }

            // Дополнительная диагностика: какие материалы используют рендереры
            LogRendererMaterialState();
        }

        /// <summary>
        /// Поиск типа Light2D через несколько имён сборок.
        /// Версия для runtime (без Editor-зависимостей).
        /// </summary>
        private static System.Type ResolveLight2DType()
        {
            string[] assemblyNames = new string[]
            {
                "Unity.2D.RenderPipeline.Runtime",
                "Unity.RenderPipeline.Universal.2D.Runtime",
                "Unity.RenderPipeline.Universal.Runtime",
            };

            string fullTypeName = "UnityEngine.Rendering.Universal.Light2D";

            foreach (var asmName in assemblyNames)
            {
                var type = System.Type.GetType($"{fullTypeName}, {asmName}");
                if (type != null) return type;
            }

            // Fallback: поиск по fullName во всех загруженных сборках
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(fullTypeName);
                    if (type != null) return type;
                }
                catch (System.Exception) { }
            }

            // Fallback: поиск по короткому имени "Light2D"
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (type.Name == "Light2D" && type.IsSubclassOf(typeof(Component)))
                            return type;
                    }
                }
                catch (System.Reflection.ReflectionTypeLoadException) { }
                catch (System.Exception) { }
            }

            return null;
        }

        /// <summary>
        /// Диагностика: логировать сборки, связанные с рендерингом.
        /// </summary>
        private static void LogLight2DDiagnostics()
        {
            Debug.Log($"{PREFIX}-L8 === ДИАГНОСТИКА Light2D ===");
            int renderRelated = 0;
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                string name = assembly.GetName().Name;
                if (name.Contains("Render") || name.Contains("2D") || name.Contains("2d") ||
                    name.Contains("Universal") || name.Contains("Pipeline"))
                {
                    Debug.Log($"{PREFIX}-L8   Сборка: {name}");
                    renderRelated++;
                }
            }
            Debug.Log($"{PREFIX}-L8 Render-related сборок: {renderRelated}");
            Debug.Log($"{PREFIX}-L8 === КОНЕЦ ДИАГНОСТИКИ ===");
        }

        /// <summary>
        /// Логирует информацию о материалах SpriteRenderer и TilemapRenderer.
        /// Помогает определить, какие рендереры используют Sprite-Lit-Default
        /// (требует Light2D) vs Sprite-Unlit-Default (не требует Light2D).
        /// </summary>
        private static void LogRendererMaterialState()
        {
            // TilemapRenderer'ы
            var tilemapRenderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            foreach (var r in tilemapRenderers)
            {
                string shaderName = r.sharedMaterial != null ? r.sharedMaterial.shader.name : "NULL";
                bool isLit = shaderName.Contains("Lit");
                Debug.Log($"{PREFIX}-L8 TilemapRenderer \"{r.name}\": shader=\"{shaderName}\" " +
                    $"{(isLit ? "⚠️ LIT (требует Light2D)" : "✅ UNLIT")}");
            }

            // SpriteRenderer'ы — подсчёт по шейдеру
            var spriteRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            int litCount = 0, unlitCount = 0, otherCount = 0;
            foreach (var sr in spriteRenderers)
            {
                if (sr.sharedMaterial == null) { otherCount++; continue; }
                string shaderName = sr.sharedMaterial.shader.name;
                if (shaderName.Contains("Lit") && !shaderName.Contains("Unlit")) litCount++;
                else if (shaderName.Contains("Unlit")) unlitCount++;
                else otherCount++;
            }
            Debug.Log($"{PREFIX}-L8 SpriteRenderer: Lit={litCount} (⚠️ требует Light2D), " +
                $"Unlit={unlitCount} (✅), Other={otherCount}");
        }

        // ====================================================================
        //  FULL DIAGNOSTICS
        // ====================================================================

        /// <summary>
        /// Полная диагностика рендер-пайплайна. Логирует все этапы разом.
        /// Вызывается вручную через ContextMenu или из кода.
        /// </summary>
        public static void LogFullDiagnostics()
        {
            Debug.Log($"{PREFIX} ========================================");
            Debug.Log($"{PREFIX} === ПОЛНАЯ ДИАГНОСТИКА РЕНДЕР-ПАЙПЛАЙНА ===");
            Debug.Log($"{PREFIX} ========================================");

            LogSortingLayers();
            LogTilemapState();
            LogCameraState();
            LogLightState();

            // Логируем все SpriteRenderer на сцене
            var allSR = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            Debug.Log($"{PREFIX} Всего SpriteRenderer на сцене: {allSR.Length}");
            int visibleCount = 0;
            int nullSpriteCount = 0;
            foreach (var sr in allSR)
            {
                if (sr.sprite == null)
                {
                    nullSpriteCount++;
                    Debug.LogWarning($"{PREFIX} ❌ SpriteRenderer \"{sr.gameObject.name}\": sprite=NULL, layer=\"{sr.sortingLayerName}\"");
                }
                else
                {
                    visibleCount++;
                }
            }
            Debug.Log($"{PREFIX} SpriteRenderer со спрайтом: {visibleCount}, без спрайта: {nullSpriteCount}");

            Debug.Log($"{PREFIX} ========================================");
            Debug.Log($"{PREFIX} === ДИАГНОСТИКА ЗАВЕРШЕНА ===");
            Debug.Log($"{PREFIX} ========================================");
        }

        // ====================================================================
        //  L9: ALL RENDERER STATE (FIX-SORT)
        // ====================================================================

        /// <summary>
        /// FIX-SORT: Логирует ВСЕ рендереры на сцене (TilemapRenderer + SpriteRenderer)
        /// с их sorting-параметрами. Позволяет быстро найти рендереры на "Default" слое
        /// или с неправильным порядком.
        /// Вызывается из TileMapController.Start() после FixAllTilemapRenderers().
        /// Редактировано: 2026-04-17 12:50 UTC
        /// </summary>
        public static void LogAllRendererState()
        {
            Debug.Log($"{PREFIX}-L9 ====== ALL RENDERER STATE ======");

            // Проверяем порядок Sorting Layers
            var layers = SortingLayer.layers;
            int terrainIdx = -1, objectsIdx = -1, playerIdx = -1;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == "Terrain") terrainIdx = i;
                if (layers[i].name == "Objects") objectsIdx = i;
                if (layers[i].name == "Player") playerIdx = i;
            }

            bool orderCorrect = terrainIdx >= 0 && objectsIdx >= 0 && playerIdx >= 0 &&
                terrainIdx < objectsIdx && objectsIdx < playerIdx;

            Debug.Log($"{PREFIX}-L9 Порядок слоёв: Terrain={terrainIdx}, Objects={objectsIdx}, Player={playerIdx}");
            Debug.Log($"{PREFIX}-L9 {(orderCorrect ? "✅" : "❌")} Порядок {(!orderCorrect ? "НЕПРАВИЛЬНЫЙ — terrain будет поверх player!" : "корректный")}");

            // TilemapRenderer'ы
            var tilemapRenderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            Debug.Log($"{PREFIX}-L9 TilemapRenderer'ы: {tilemapRenderers.Length}");
            foreach (var r in tilemapRenderers)
            {
                bool onDefault = r.sortingLayerName == "Default";
                Debug.Log($"{PREFIX}-L9   {(onDefault ? "❌" : "✅")} {r.name}: " +
                    $"layer=\"{r.sortingLayerName}\"(id={r.sortingLayerID}), " +
                    $"order={r.sortingOrder}" +
                    (onDefault ? " — НА DEFAULT СЛОЕ! Будет поверх Player!" : ""));
            }

            // SpriteRenderer'ы — подсчёт по слоям
            var spriteRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            int onDefaultCount = 0;
            int onPlayerCount = 0;
            int onObjectsCount = 0;
            int onTerrainCount = 0;
            int onOtherCount = 0;

            foreach (var sr in spriteRenderers)
            {
                if (sr.sortingLayerName == "Default") onDefaultCount++;
                else if (sr.sortingLayerName == "Player") onPlayerCount++;
                else if (sr.sortingLayerName == "Objects") onObjectsCount++;
                else if (sr.sortingLayerName == "Terrain") onTerrainCount++;
                else onOtherCount++;
            }

            Debug.Log($"{PREFIX}-L9 SpriteRenderer'ы: {spriteRenderers.Length} всего");
            Debug.Log($"{PREFIX}-L9   Default={onDefaultCount}, Terrain={onTerrainCount}, " +
                $"Objects={onObjectsCount}, Player={onPlayerCount}, Прочие={onOtherCount}");

            if (onDefaultCount > 0)
            {
                Debug.LogWarning($"{PREFIX}-L9 ❌ {onDefaultCount} SpriteRenderer'ов на DEFAULT слое! " +
                    "Они будут рендериться поверх Player и Objects!");
                // Логируем первые 5 проблемных
                int logged = 0;
                foreach (var sr in spriteRenderers)
                {
                    if (sr.sortingLayerName == "Default" && logged < 5)
                    {
                        Debug.LogWarning($"{PREFIX}-L9   ❌ \"{sr.gameObject.name}\": order={sr.sortingOrder}");
                        logged++;
                    }
                }
            }

            // Логируем Player sprite отдельно
            var playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                var playerSR = playerObj.GetComponentInChildren<SpriteRenderer>();
                if (playerSR != null)
                {
                    Debug.Log($"{PREFIX}-L9 Player: layer=\"{playerSR.sortingLayerName}\"(id={playerSR.sortingLayerID}), " +
                        $"order={playerSR.sortingOrder}, visible={playerSR.isVisible}");
                }
            }
            else
            {
                Debug.LogWarning($"{PREFIX}-L9 ❌ GameObject \"Player\" НЕ НАЙДЕН на сцене!");
            }
        }
    }
}
