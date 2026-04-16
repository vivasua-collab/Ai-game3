// ============================================================================
// RenderPipelineLogger.cs — Диагностика рендер-пайплайна
// Cultivation World Simulator
// Создано: 2026-04-16 11:37 UTC
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
        /// </summary>
        public static void LogLightState()
        {
            Debug.Log($"{PREFIX}-L8 ====== LIGHT STATE ======");

            var light2DObj = GameObject.Find("GlobalLight2D");
            if (light2DObj != null)
            {
                // Проверяем через reflection (Light2D в другой сборке)
                var light2DType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.2D.RenderPipeline.Runtime");
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

                        Debug.Log($"{PREFIX}-L8 GlobalLight2D: type={lightType} (1=Global), intensity={intensity}, color={color}");
                        Debug.Log($"{PREFIX}-L8 ✅ Light2D найден");
                    }
                    else
                    {
                        Debug.LogWarning($"{PREFIX}-L8 ⚠️ GlobalLight2D объект есть, но компонент Light2D не найден");
                    }
                }
                else
                {
                    Debug.LogWarning($"{PREFIX}-L8 ⚠️ Тип Light2D не найден (сборка Unity.2D.RenderPipeline.Runtime)");
                }
            }
            else
            {
                Debug.LogWarning($"{PREFIX}-L8 ❌ GlobalLight2D НЕ НАЙДЕН — спрайты будут чёрными (Sprite-Lit-Default)!");
            }
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
    }
}
