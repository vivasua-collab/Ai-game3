// ============================================================================
// FullSceneBuilder.cs — Инкрементальный One-Click Builder сцены
// Cultivation World Simulator
// Версия: 1.2
// ============================================================================
// Создано: 2026-04-13 08:00:00 UTC
// Редактировано: 2026-04-17 10:53 UTC — REVERT: PPU=32 для terrain и objects (рабочее значение 14 апреля). Убран AI sprite pipeline.
//
// АРХИТЕКТУРА:
//   15 фаз, каждая идемпотентна (повторный запуск безопасен).
//   Фаза проверяет что уже сделано и пропускает при необходимости.
//   Можно запустить все фазы разом или по отдельности.
//
// МЕНЮ:
//   Tools → Full Scene Builder → Build All (One Click)
//   Tools → Full Scene Builder → Phase 01: Folders
//   Tools → Full Scene Builder → Phase 02: Tags & Layers
//   ... и т.д.
//
// СОВМЕСТИМОСТЬ: Unity 6.3+ (6000.3)
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

// Core
using CultivationGame.Core;

// Managers
using CultivationGame.Managers;

// World
using CultivationGame.World;

// Player
using CultivationGame.Player;

// Qi
using CultivationGame.Qi;

// Body
using CultivationGame.Body;

// Inventory
using CultivationGame.Inventory;

// Combat
using CultivationGame.Combat;

// Save
using CultivationGame.Save;

// Interaction
using CultivationGame.Interaction;

// Tile
using CultivationGame.TileSystem;
using CultivationGame.TileSystem.Editor;

// Generators
using CultivationGame.Generators;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Инкрементальный One-Click Builder.
    ///
    /// Каждая фаза:
    ///   1. Проверяет, нужно ли действие (IsPhaseNeeded)
    ///   2. Выполняет (ExecutePhase)
    ///   3. Логирует результат
    ///
    /// Повторный запуск безопасен — пропускает уже выполненные фазы.
    /// </summary>
    public static class FullSceneBuilder
    {
        #region Constants

        private const string SCENE_PATH = "Assets/Scenes/Main.unity";
        private const string SCENE_NAME = "Main";

        // Папки, которые должны существовать
        private static readonly string[] REQUIRED_FOLDERS = new string[]
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
        private static readonly string[] REQUIRED_TAGS = new string[]
        {
            "Player",
            "NPC",
            "Interactable",
            "Item",
            "Enemy",
            "Resource",  // FIX: Добавлен тег "Resource" для ResourceSpawner. Редактировано: 2026-04-15 12:00:00 UTC
            "Harvestable", // Шаг 7: Тег для harvestable-объектов. Редактировано: 2026-04-16
        };

        // Слои (имя → номер)
        private static readonly KeyValuePair<string, int>[] REQUIRED_LAYERS = new KeyValuePair<string, int>[]
        {
            new KeyValuePair<string, int>("Player", 6),
            new KeyValuePair<string, int>("NPC", 7),
            new KeyValuePair<string, int>("Interactable", 8),
            new KeyValuePair<string, int>("Item", 9),
            new KeyValuePair<string, int>("Enemy", 10),
            new KeyValuePair<string, int>("UI", 11),
            new KeyValuePair<string, int>("Background", 12),
            new KeyValuePair<string, int>("Harvestable", 13), // Шаг 7: Слой для harvestable-объектов (Physics2D). Редактировано: 2026-04-16
        };

        #endregion

        #region Phase Registry

        private delegate bool CheckNeededDelegate();
        private delegate void ExecutePhaseDelegate();

        private struct PhaseInfo
        {
            public string name;
            public string menuPath;
            public CheckNeededDelegate isNeeded;
            public ExecutePhaseDelegate execute;
        }

        private static readonly PhaseInfo[] PHASES = new PhaseInfo[]
        {
            new PhaseInfo
            {
                name = "Folders",
                menuPath = "Phase 01: Folders",
                isNeeded = IsFoldersNeeded,
                execute = ExecuteFolders
            },
            new PhaseInfo
            {
                name = "Tags & Layers",
                menuPath = "Phase 02: Tags and Layers",
                isNeeded = IsTagsLayersNeeded,
                execute = ExecuteTagsLayers
            },
            new PhaseInfo
            {
                name = "Scene Creation",
                menuPath = "Phase 03: Create Scene",
                isNeeded = IsSceneNeeded,
                execute = ExecuteScene
            },
            new PhaseInfo
            {
                name = "Camera & Light",
                menuPath = "Phase 04: Camera and Light",
                isNeeded = IsCameraLightNeeded,
                execute = ExecuteCameraLight
            },
            new PhaseInfo
            {
                name = "GameManager",
                menuPath = "Phase 05: GameManager and Systems",
                isNeeded = IsGameManagerNeeded,
                execute = ExecuteGameManager
            },
            new PhaseInfo
            {
                name = "Player",
                menuPath = "Phase 06: Player",
                isNeeded = IsPlayerNeeded,
                execute = ExecutePlayer
            },
            new PhaseInfo
            {
                name = "UI",
                menuPath = "Phase 07: UI (Canvas, HUD, EventSystem)",
                isNeeded = IsUINeeded,
                execute = ExecuteUI
            },
            new PhaseInfo
            {
                name = "Tilemap",
                menuPath = "Phase 08: Tilemap System",
                isNeeded = IsTilemapNeeded,
                execute = ExecuteTilemap
            },
            new PhaseInfo
            {
                name = "Generate Assets (SO from JSON)",
                menuPath = "Phase 09: Generate Assets from JSON",
                isNeeded = IsGenerateAssetsNeeded,
                execute = ExecuteGenerateAssets
            },
            new PhaseInfo
            {
                name = "Generate Tile Sprites",
                menuPath = "Phase 10: Generate Tile Sprites",
                isNeeded = IsGenerateSpritesNeeded,
                execute = ExecuteGenerateSprites
            },
            new PhaseInfo
            {
                name = "Generate Formation UI Prefabs",
                menuPath = "Phase 11: Generate Formation UI Prefabs",
                isNeeded = IsGenerateUIPrefabsNeeded,
                execute = ExecuteGenerateUIPrefabs
            },
            new PhaseInfo
            {
                name = "TMP Essentials",
                menuPath = "Phase 12: Import TMP Essentials",
                isNeeded = IsTMPEssentialsNeeded,
                execute = ExecuteTMPEssentials
            },
            new PhaseInfo
            {
                name = "Save Scene",
                menuPath = "Phase 13: Save Scene",
                isNeeded = IsSaveSceneNeeded,
                execute = ExecuteSaveScene
            },
            new PhaseInfo
            {
                name = "Create & Assign Tile Assets",
                menuPath = "Phase 14: Create Tile Assets",
                isNeeded = IsCreateTileAssetsNeeded,
                execute = ExecuteCreateTileAssets
            },
            new PhaseInfo
            {
                name = "Configure Test Location",
                menuPath = "Phase 15: Configure Test Location",
                isNeeded = IsConfigureTestLocationNeeded,
                execute = ExecuteConfigureTestLocation
            },
        };

        #endregion

        // ====================================================================
        //  MAIN: Build All (One Click)
        // ====================================================================

        [MenuItem("Tools/Full Scene Builder/Build All (One Click)", false, 0)]
        public static void BuildAll()
        {
            Debug.Log("========================================");
            Debug.Log("[FullSceneBuilder] === BUILD ALL START ===");
            Debug.Log("========================================");

            int executed = 0;
            int skipped = 0;
            int failed = 0;
            float startTime = (float)EditorApplication.timeSinceStartup;

            for (int i = 0; i < PHASES.Length; i++)
            {
                var phase = PHASES[i];
                string phaseLabel = $"[{i + 1}/{PHASES.Length}] {phase.name}";

                try
                {
                    if (phase.isNeeded())
                    {
                        Debug.Log($"{phaseLabel}: Executing...");
                        phase.execute();
                        Debug.Log($"{phaseLabel}: ✅ Done");
                        executed++;
                    }
                    else
                    {
                        Debug.Log($"{phaseLabel}: ⏭ Skipped (already done)");
                        skipped++;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"{phaseLabel}: ❌ FAILED — {ex.Message}\n{ex.StackTrace}");
                    failed++;

                    // Спрашиваем продолжать ли
                    if (!EditorUtility.DisplayDialog("Phase Failed",
                        $"Фаза «{phase.name}» упала:\n{ex.Message}\n\nПродолжить?", "Да", "Стоп"))
                    {
                        break;
                    }
                }
            }

            float elapsed = (float)EditorApplication.timeSinceStartup - startTime;

            Debug.Log("========================================");
            Debug.Log($"[FullSceneBuilder] === BUILD ALL COMPLETE ===");
            Debug.Log($"  Executed: {executed} | Skipped: {skipped} | Failed: {failed}");
            Debug.Log($"  Time: {elapsed:F1}s");
            Debug.Log("========================================");

            if (failed == 0)
            {
                EditorUtility.DisplayDialog("Build Complete",
                    $"Сборка завершена!\n\nВыполнено: {executed}\nПропущено: {skipped}\nВремя: {elapsed:F1}s",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Build Finished with Errors",
                    $"Выполнено: {executed}\nПропущено: {skipped}\nОшибки: {failed}",
                    "OK");
            }
        }

        // ====================================================================
        //  INDIVIDUAL PHASE MENU ITEMS
        // ====================================================================

        [MenuItem("Tools/Full Scene Builder/Phase 01: Folders", false, 101)]
        public static void RunPhase01() { RunSinglePhase(0); }

        [MenuItem("Tools/Full Scene Builder/Phase 02: Tags and Layers", false, 102)]
        public static void RunPhase02() { RunSinglePhase(1); }

        [MenuItem("Tools/Full Scene Builder/Phase 03: Create Scene", false, 103)]
        public static void RunPhase03() { RunSinglePhase(2); }

        [MenuItem("Tools/Full Scene Builder/Phase 04: Camera and Light", false, 104)]
        public static void RunPhase04() { RunSinglePhase(3); }

        [MenuItem("Tools/Full Scene Builder/Phase 05: GameManager and Systems", false, 105)]
        public static void RunPhase05() { RunSinglePhase(4); }

        [MenuItem("Tools/Full Scene Builder/Phase 06: Player", false, 106)]
        public static void RunPhase06() { RunSinglePhase(5); }

        [MenuItem("Tools/Full Scene Builder/Phase 07: UI (Canvas, HUD, EventSystem)", false, 107)]
        public static void RunPhase07() { RunSinglePhase(6); }

        [MenuItem("Tools/Full Scene Builder/Phase 08: Tilemap System", false, 108)]
        public static void RunPhase08() { RunSinglePhase(7); }

        [MenuItem("Tools/Full Scene Builder/Phase 09: Generate Assets from JSON", false, 109)]
        public static void RunPhase09() { RunSinglePhase(8); }

        [MenuItem("Tools/Full Scene Builder/Phase 10: Generate Tile Sprites", false, 110)]
        public static void RunPhase10() { RunSinglePhase(9); }

        [MenuItem("Tools/Full Scene Builder/Phase 11: Generate Formation UI Prefabs", false, 111)]
        public static void RunPhase11() { RunSinglePhase(10); }

        [MenuItem("Tools/Full Scene Builder/Phase 12: Import TMP Essentials", false, 112)]
        public static void RunPhase12() { RunSinglePhase(11); }

        [MenuItem("Tools/Full Scene Builder/Phase 13: Save Scene", false, 113)]
        public static void RunPhase13() { RunSinglePhase(12); }

        [MenuItem("Tools/Full Scene Builder/Phase 14: Create Tile Assets", false, 114)]
        public static void RunPhase14() { RunSinglePhase(13); }

        [MenuItem("Tools/Full Scene Builder/Phase 15: Configure Test Location", false, 115)]
        public static void RunPhase15() { RunSinglePhase(14); }

        // ====================================================================
        //  UTILITY: Run Single Phase
        // ====================================================================

        private static void RunSinglePhase(int index)
        {
            if (index < 0 || index >= PHASES.Length)
            {
                Debug.LogError($"[FullSceneBuilder] Invalid phase index: {index}");
                return;
            }

            var phase = PHASES[index];

            if (phase.isNeeded())
            {
                try
                {
                    Debug.Log($"[FullSceneBuilder] Running phase: {phase.name}");
                    phase.execute();
                    Debug.Log($"[FullSceneBuilder] ✅ Phase '{phase.name}' complete");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[FullSceneBuilder] ❌ Phase '{phase.name}' failed: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"[FullSceneBuilder] ⏭ Phase '{phase.name}' skipped (already done)");
            }
        }

        // ====================================================================
        //  PHASE 01: FOLDERS
        // ====================================================================

        private static bool IsFoldersNeeded()
        {
            foreach (var folder in REQUIRED_FOLDERS)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    return true;
            }
            return false;
        }

        private static void ExecuteFolders()
        {
            int created = 0;
            foreach (var folder in REQUIRED_FOLDERS)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    EnsureDirectory(folder);
                    created++;
                }
            }
            AssetDatabase.Refresh();
            Debug.Log($"[FullSceneBuilder] Created {created} folders");
        }

        // ====================================================================
        //  PHASE 02: TAGS & LAYERS
        // ====================================================================

        private static bool IsTagsLayersNeeded()
        {
            // Проверяем теги
            var tags = UnityEditorInternal.InternalEditorUtility.tags;
            foreach (var requiredTag in REQUIRED_TAGS)
            {
                bool found = false;
                foreach (var existingTag in tags)
                {
                    if (existingTag == requiredTag) { found = true; break; }
                }
                if (!found) return true;
            }

            // Проверяем слои
            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            foreach (var kvp in REQUIRED_LAYERS)
            {
                bool found = false;
                foreach (var existingLayer in layers)
                {
                    if (existingLayer == kvp.Key) { found = true; break; }
                }
                if (!found) return true;
            }

            // FIX-V2-1: Проверяем Sorting Layers
            // Если слой "Objects" не существует — нужна фаза для его создания
            // Редактировано: 2026-04-16 11:37 UTC
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayersProp != null)
            {
                HashSet<string> existingSortingLayers = new HashSet<string>();
                for (int i = 0; i < sortingLayersProp.arraySize; i++)
                {
                    var nameProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                    if (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
                        existingSortingLayers.Add(nameProp.stringValue);
                }
                // "Objects" — критический слой, без него спрайты невидимы
                if (!existingSortingLayers.Contains("Objects"))
                    return true;
            }

            return false;
        }

        private static void ExecuteTagsLayers()
        {
            // --- Теги ---
            var tags = new List<string>(UnityEditorInternal.InternalEditorUtility.tags);
            bool tagsChanged = false;

            foreach (var requiredTag in REQUIRED_TAGS)
            {
                if (!tags.Contains(requiredTag))
                {
                    tags.Add(requiredTag);
                    tagsChanged = true;
                }
            }

            if (tagsChanged)
            {
                SerializedObject tagManager = new SerializedObject(
                    AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

                var tagsProp = tagManager.FindProperty("tags");
                if (tagsProp != null)
                {
                    tagsProp.ClearArray();
                    for (int i = 0; i < tags.Count; i++)
                    {
                        tagsProp.InsertArrayElementAtIndex(i);
                        tagsProp.GetArrayElementAtIndex(i).stringValue = tags[i];
                    }
                    tagManager.ApplyModifiedProperties();
                }

                Debug.Log($"[FullSceneBuilder] Tags updated: {string.Join(", ", REQUIRED_TAGS)}");
            }

            // --- Слои ---
            SerializedObject layerManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var layersProp = layerManager.FindProperty("layers");
            bool layersChanged = false;

            if (layersProp != null)
            {
                foreach (var kvp in REQUIRED_LAYERS)
                {
                    var element = layersProp.GetArrayElementAtIndex(kvp.Value);
                    if (element != null && string.IsNullOrEmpty(element.stringValue))
                    {
                        element.stringValue = kvp.Key;
                        layersChanged = true;
                    }
                }

                if (layersChanged)
                {
                    layerManager.ApplyModifiedProperties();
                    Debug.Log($"[FullSceneBuilder] Layers updated");
                }
            }

            // --- Sorting Layers (FIX-V2-1) ---
            // КОРНЕВАЯ ПРИЧИНА всех визуальных багов: Sorting Layer "Objects" НЕ существовал!
            // Без него SpriteRenderer.sortingLayerName = "Objects" игнорируется в Unity 6+ →
            // спрайты НЕ рендерятся (игрок, harvestable, ресурсы — все невидимы).
            // Sorting Layers настраиваются в TagManager.asset → m_SortingLayers.
            // Редактировано: 2026-04-16 11:37 UTC
            EnsureSortingLayers();

            if (!tagsChanged && !layersChanged)
            {
                Debug.Log("[FullSceneBuilder] Tags & Layers already configured");
            }
        }

        /// <summary>
        /// FIX-V2-1: Создание Sorting Layers для 2D рендеринга.
        /// Unity имеет ДВЕ системы слоёв:
        ///   - Physics Layers (layers[]) — для коллизий (создаются выше)
        ///   - Sorting Layers (m_SortingLayers) — для порядка рендеринга 2D (создаются тут)
        /// Без Sorting Layer "Objects" все SpriteRenderer с sortingLayerName="Objects"
        /// игнорируются в Unity 6+ → спрайты невидимы!
        /// Редактировано: 2026-04-16 11:37 UTC
        /// </summary>
        private static void EnsureSortingLayers()
        {
            // Требуемые Sorting Layers (порядок важен — снизу вверх):
            // 0. "Default"     — уже существует (дефолтный)
            // 1. "Background"  — фоновые элементы
            // 2. "Terrain"     — terrain TilemapRenderer
            // 3. "Objects"     — объекты TilemapRenderer, SpriteRenderer объектов
            // 4. "Player"      — игрок и его тень
            // 5. "UI"          — UI элементы в мировом пространстве
            string[] requiredSortingLayers = new string[]
            {
                "Default",     // ID=0, всегда существует
                "Background",  // ID=1
                "Terrain",     // ID=2
                "Objects",     // ID=3 ← КРИТИЧЕСКИЙ! Используется PlayerVisual, HarvestableSpawner
                "Player",      // ID=4 ← Используется PlayerVisual (FIX-V2-5)
                "UI"           // ID=5
            };

            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");
            if (sortingLayersProp == null)
            {
                Debug.LogError("[FullSceneBuilder] FIX-V2-1: m_SortingLayers не найден в TagManager!");
                return;
            }

            // Собираем существующие слои
            HashSet<string> existingLayers = new HashSet<string>();
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var nameProp = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (nameProp != null && !string.IsNullOrEmpty(nameProp.stringValue))
                    existingLayers.Add(nameProp.stringValue);
            }

            // Добавляем недостающие слои
            bool changed = false;
            int nextId = sortingLayersProp.arraySize; // Unity назначает ID автоматически

            foreach (string layerName in requiredSortingLayers)
            {
                if (existingLayers.Contains(layerName))
                    continue;

                // "Default" — особый случай, он всегда существует, но может не быть в массиве
                if (layerName == "Default" && sortingLayersProp.arraySize > 0)
                {
                    var first = sortingLayersProp.GetArrayElementAtIndex(0).FindPropertyRelative("name");
                    if (first != null && first.stringValue == "Default")
                        continue;
                }

                int insertIndex = sortingLayersProp.arraySize;
                sortingLayersProp.InsertArrayElementAtIndex(insertIndex);

                var newElement = sortingLayersProp.GetArrayElementAtIndex(insertIndex);

                // Устанавливаем имя
                var nameProp = newElement.FindPropertyRelative("name");
                if (nameProp != null)
                    nameProp.stringValue = layerName;

                // Устанавливаем uniqueID (Unity использует инкрементальные ID)
                var idProp = newElement.FindPropertyRelative("uniqueID");
                if (idProp != null)
                    idProp.intValue = nextId;

                // locked = false
                var lockedProp = newElement.FindPropertyRelative("locked");
                if (lockedProp != null)
                    lockedProp.boolValue = false;

                changed = true;
                Debug.Log($"[FullSceneBuilder] FIX-V2-1: Sorting Layer \"{layerName}\" создан (ID={nextId})");
                nextId++;
            }

            if (changed)
            {
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();

                // Логируем итоговое состояние
                Debug.Log("[FullSceneBuilder] FIX-V2-1: Sorting Layers созданы:");
                for (int i = 0; i < sortingLayersProp.arraySize; i++)
                {
                    var elem = sortingLayersProp.GetArrayElementAtIndex(i);
                    var n = elem.FindPropertyRelative("name");
                    var id = elem.FindPropertyRelative("uniqueID");
                    Debug.Log($"  [{i}] \"{n?.stringValue ?? "?"}\" (id={id?.intValue ?? -1})");
                }
                Debug.Log("[FullSceneBuilder] ✅ Sorting layer \"Objects\" существует — спрайты будут видимы!");
            }
            else
            {
                Debug.Log("[FullSceneBuilder] Sorting Layers уже настроены");
            }
        }

        // ====================================================================
        //  PHASE 03: CREATE SCENE
        // ====================================================================

        private static bool IsSceneNeeded()
        {
            // Проверяем: существует ли файл сцены
            if (System.IO.File.Exists(SCENE_PATH))
                return false;

            return true;
        }

        private static void ExecuteScene()
        {
            // Создаём новую сцену
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Удаляем дефолтную камеру — мы создадим свою в Phase 04
            var defaultCamera = Camera.main;
            if (defaultCamera != null)
            {
                Object.DestroyImmediate(defaultCamera.gameObject);
            }

            // Сохраняем сцену
            EnsureDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log($"[FullSceneBuilder] Scene created: {SCENE_PATH}");
        }

        /// <summary>
        /// Убедиться что нужная сцена открыта. Если нет — открыть.
        /// </summary>
        // Редактировано: 2026-04-13 14:03:25 UTC — FIX: очистка Missing Prefabs при открытии сцены
        private static void EnsureSceneOpen()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == SCENE_PATH)
            {
                CleanMissingPrefabs();
                return;
            }

            if (System.IO.File.Exists(SCENE_PATH))
            {
                EditorSceneManager.OpenScene(SCENE_PATH);
                CleanMissingPrefabs();
            }
            else
            {
                ExecuteScene();
            }
        }

        // ====================================================================
        //  PHASE 04: CAMERA & LIGHT
        // ====================================================================

        private static bool IsCameraLightNeeded()
        {
            EnsureSceneOpen();
            var cam = Camera.main;
            // FIX 2A: Также проверяем Global Light2D — без него спрайты невидимы в URP 2D
            // Редактировано: 2026-04-15 16:53:48 UTC
            bool hasLight2D = GameObject.Find("GlobalLight2D") != null;
            return cam == null || !hasLight2D;
        }

        private static void ExecuteCameraLight()
        {
            EnsureSceneOpen();

            // --- Camera ---
            var camObj = Camera.main?.gameObject;
            if (camObj == null)
            {
                camObj = new GameObject("Main Camera");
                camObj.AddComponent<Camera>();
                camObj.tag = "MainCamera";
            }

            camObj.transform.position = new Vector3(0, 0, -10);
            var cam = camObj.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            // FIX: Тёмно-зелёный фон вместо синего — сливается с травой,
            // нет синих промежутков между спрайтами.
            // Редактировано: 2026-04-15 UTC
            cam.backgroundColor = new Color(0.12f, 0.18f, 0.08f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 8f;

            // Camera2DSetup — камера следит за игроком с ограничением границами карты
            // Редактировано: 2026-04-14 07:50:00 UTC
            var camera2D = camObj.GetComponent<Camera2DSetup>();
            if (camera2D == null)
                camera2D = camObj.AddComponent<Camera2DSetup>();

            var camSo = new SerializedObject(camera2D);
            camSo.FindProperty("orthographicSize").floatValue = 8f;
            camSo.FindProperty("followEnabled").boolValue = true;
            camSo.FindProperty("followSmoothness").floatValue = 0.08f;
            camSo.FindProperty("useBounds").boolValue = true;
            camSo.FindProperty("boundsMax").vector2Value = new Vector2(200f, 160f); // 100×80 × 2м/тайл
            camSo.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(camObj, "Create Camera");

            // --- 3D Light (для материалов, не влияющих на 2D спрайты) ---
            var lightObj = GameObject.Find("Directional Light");
            if (lightObj == null)
            {
                lightObj = new GameObject("Directional Light");
                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                light.color = Color.white;
                lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                Undo.RegisterCreatedObjectUndo(lightObj, "Create Light");
            }

            // --- FIX 2A: Global Light2D для URP 2D Renderer ---
            // Renderer2D.asset активен → Sprite-Lit-Default шейдер требует Light2D.
            // Без Light2D ВСЕ спрайты рендерятся как чёрные (невидимые на тёмном фоне).
            // Это КРИТИЧЕСКОЕ исправление — без него ни один спрайт не виден.
            // Редактировано: 2026-04-15 16:53:48 UTC
            var light2DObj = GameObject.Find("GlobalLight2D");
            if (light2DObj == null)
            {
                light2DObj = new GameObject("GlobalLight2D");
                // Используем System.Type.GetType для безопасного добавления Light2D
                // (класс в сборке Unity.2D.RenderPipeline.Runtime)
                var light2DType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.2D.RenderPipeline.Runtime");
                if (light2DType != null)
                {
                    var light2D = light2DObj.AddComponent(light2DType);
                    // Настраиваем через Reflection — Light2D.lightType = Global
                    var lightTypeProp = light2DType.GetProperty("lightType");
                    if (lightTypeProp != null)
                        lightTypeProp.SetValue(light2D, 1); // 1 = Global
                    // Настраиваем интенсивность
                    var intensityProp = light2DType.GetProperty("intensity");
                    if (intensityProp != null)
                        intensityProp.SetValue(light2D, 1f);
                    // Настраиваем цвет
                    var colorProp = light2DType.GetProperty("color");
                    if (colorProp != null)
                        colorProp.SetValue(light2D, Color.white);

                    Undo.RegisterCreatedObjectUndo(light2DObj, "Create Global Light2D");
                    Debug.Log("[FullSceneBuilder] Global Light2D создан — URP 2D спрайты теперь видимы");
                }
                else
                {
                    // Light2D тип не найден — используем альтернативный подход
                    // Добавляем компонент через SerializedObject
                    Debug.LogWarning("[FullSceneBuilder] Light2D тип не найден через Reflection. " +
                        "URP 2D renderer может не освещать спрайты. " +
                        "Добавьте Light2D вручную: GameObject → Light → 2D → Global Light");
                    Object.DestroyImmediate(light2DObj);
                }
            }

            // FIX-V2-6: Диагностика Camera и Light
            // Редактировано: 2026-04-16 11:37 UTC
            RenderPipelineLogger.LogCameraState();
            RenderPipelineLogger.LogLightState();

            Debug.Log("[FullSceneBuilder] Camera & Light configured");
        }

        // ====================================================================
        //  PHASE 05: GAME MANAGER & SYSTEMS
        // ====================================================================

        private static bool IsGameManagerNeeded()
        {
            EnsureSceneOpen();
            return UnityEngine.Object.FindFirstObjectByType<GameManager>() == null;
        }

        private static void ExecuteGameManager()
        {
            EnsureSceneOpen();

            if (UnityEngine.Object.FindFirstObjectByType<GameManager>() != null)
            {
                Debug.Log("[FullSceneBuilder] GameManager already exists");
                return;
            }

            // Создаём GameManager
            GameObject gameManagerObj = new GameObject("GameManager");
            gameManagerObj.AddComponent<GameManager>();
            gameManagerObj.AddComponent<GameInitializer>();

            // Создаём Systems как дочерний объект
            GameObject systems = new GameObject("Systems");
            systems.transform.SetParent(gameManagerObj.transform);

            // Добавляем все контроллеры
            systems.AddComponent<WorldController>();
            systems.AddComponent<TimeController>();
            systems.AddComponent<LocationController>();
            systems.AddComponent<EventController>();
            systems.AddComponent<FactionController>();

            // GeneratorRegistry — прямая ссылка (namespace: CultivationGame.Generators)
            systems.AddComponent<GeneratorRegistry>();

            systems.AddComponent<SaveManager>();

            // Настраиваем TimeController
            var tc = systems.GetComponent<TimeController>();
            if (tc != null)
            {
                SerializedObject so = new SerializedObject(tc);
                SetProperty(so, "currentTimeSpeed", (int)TimeSpeed.Normal);
                SetProperty(so, "autoAdvance", true);
                SetProperty(so, "normalSpeedRatio", 60);
                SetProperty(so, "fastSpeedRatio", 300);
                SetProperty(so, "veryFastSpeedRatio", 900);
                SetProperty(so, "daysPerMonth", 30);
                SetProperty(so, "monthsPerYear", 12);
                so.ApplyModifiedProperties();
            }

            // Настраиваем SaveManager
            var sm = systems.GetComponent<SaveManager>();
            if (sm != null)
            {
                SerializedObject so = new SerializedObject(sm);
                SetProperty(so, "saveFolder", "Saves");
                SetProperty(so, "fileExtension", ".sav");
                SetProperty(so, "useEncryption", false);
                SetProperty(so, "autoSave", true);
                SetProperty(so, "autoSaveInterval", 300);
                SetProperty(so, "maxSlots", 5);
                so.ApplyModifiedProperties();
            }

            Undo.RegisterCreatedObjectUndo(gameManagerObj, "Create GameManager");
            Debug.Log("[FullSceneBuilder] GameManager + Systems created");
        }

        // ====================================================================
        //  PHASE 06: PLAYER
        // ====================================================================

        private static bool IsPlayerNeeded()
        {
            EnsureSceneOpen();
            return GameObject.Find("Player") == null;
        }

        private static void ExecutePlayer()
        {
            EnsureSceneOpen();

            if (GameObject.Find("Player") != null)
            {
                Debug.Log("[FullSceneBuilder] Player already exists");
                return;
            }

            GameObject player = new GameObject("Player");
            // FIX 2B: Позиция на центр карты вместо (0,0) = Void.
            // Карта 100×80 тайлов × 2м/тайл → центр = (100, 80, 0).
            // На центре — каменный алтарь, видимый на любом фоне.
            // Редактировано: 2026-04-15 16:53:48 UTC
            player.transform.position = new Vector3(100f, 80f, 0f);
            player.tag = "Player";

            // Установить слой Player (6)
            player.layer = 6;

            // Rigidbody2D
            Rigidbody2D rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 1f;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            // Collider
            CircleCollider2D col = player.AddComponent<CircleCollider2D>();
            col.isTrigger = false;
            col.radius = 0.5f;

            // Все компоненты Player
            player.AddComponent<PlayerController>();
            player.AddComponent<BodyController>();
            player.AddComponent<QiController>();
            player.AddComponent<InventoryController>();
            player.AddComponent<EquipmentController>();
            player.AddComponent<TechniqueController>();
            player.AddComponent<SleepSystem>();
            player.AddComponent<InteractionController>();

            // FIX: НЕ добавляем SpriteRenderer напрямую на Player!
            // PlayerVisual создаёт дочерний "Visual" объект со SpriteRenderer в Awake().
            // Дублирование SpriteRenderer приводит к конфликту — спрайт не отображается.
            // PlayerVisual загружает AI-спрайт или создаёт процедурный fallback.
            // Редактировано: 2026-04-15 12:00:00 UTC
            player.AddComponent<PlayerVisual>();

            // Настройка PlayerController
            SetupPlayerComponent<PlayerController>(player, pc =>
            {
                SerializedObject so = new SerializedObject(pc);
                SetProperty(so, "playerId", "player");
                SetProperty(so, "playerName", "Игрок");
                SetProperty(so, "moveSpeed", 5f);
                SetProperty(so, "runSpeedMultiplier", 1.5f);
                so.ApplyModifiedProperties();
            });

            // Настройка BodyController
            SetupPlayerComponent<BodyController>(player, bc =>
            {
                SerializedObject so = new SerializedObject(bc);
                SetProperty(so, "bodyMaterial", (int)BodyMaterial.Organic);
                SetProperty(so, "vitality", 10);
                SetProperty(so, "cultivationLevel", 1);
                SetProperty(so, "enableRegeneration", true);
                SetProperty(so, "regenRate", 1f);
                so.ApplyModifiedProperties();
            });

            // Настройка QiController
            SetupPlayerComponent<QiController>(player, qc =>
            {
                SerializedObject so = new SerializedObject(qc);
                SetProperty(so, "cultivationLevel", 1);
                SetProperty(so, "cultivationSubLevel", 0);
                SetProperty(so, "coreQuality", (int)CoreQuality.Normal);
                SetProperty(so, "currentQi", 100L);
                SetProperty(so, "enablePassiveRegen", true);
                so.ApplyModifiedProperties();
            });

            // Настройка InventoryController
            SetupPlayerComponent<InventoryController>(player, ic =>
            {
                SerializedObject so = new SerializedObject(ic);
                SetProperty(so, "gridWidth", 8);
                SetProperty(so, "gridHeight", 6);
                SetProperty(so, "maxWeight", 100f);
                SetProperty(so, "useWeightLimit", true);
                so.ApplyModifiedProperties();
            });

            // Настройка EquipmentController
            SetupPlayerComponent<EquipmentController>(player, ec =>
            {
                SerializedObject so = new SerializedObject(ec);
                SetProperty(so, "useLayerSystem", true);
                SetProperty(so, "maxLayersPerSlot", 2);
                so.ApplyModifiedProperties();
            });

            // Настройка TechniqueController
            SetupPlayerComponent<TechniqueController>(player, tc =>
            {
                SerializedObject so = new SerializedObject(tc);
                SetProperty(so, "maxQuickSlots", 10);
                SetProperty(so, "maxUltimates", 1);
                so.ApplyModifiedProperties();
            });

            // Настройка SleepSystem
            SetupPlayerComponent<SleepSystem>(player, ss =>
            {
                SerializedObject so = new SerializedObject(ss);
                SetProperty(so, "minSleepHours", 4f);
                SetProperty(so, "maxSleepHours", 12f);
                SetProperty(so, "optimalSleepHours", 8f);
                so.ApplyModifiedProperties();
            });

            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            Debug.Log("[FullSceneBuilder] Player created with all components");
        }

        // ====================================================================
        //  PHASE 07: UI
        // ====================================================================

        private static bool IsUINeeded()
        {
            EnsureSceneOpen();
            return GameObject.Find("GameUI") == null;
        }

        private static void ExecuteUI()
        {
            EnsureSceneOpen();

            // --- Canvas ---
            if (GameObject.Find("GameUI") == null)
            {
                GameObject canvasGO = new GameObject("GameUI");
                Canvas canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasGO.AddComponent<GraphicRaycaster>();

                // --- EventSystem ---
                var eventSystem = UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
                if (eventSystem == null)
                {
                    GameObject eventSystemGO = new GameObject("EventSystem");
                    eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();

                    // InputSystemUIInputModule (для Input System Package)
                    var inputModuleType = System.Type.GetType(
                        "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                    if (inputModuleType != null)
                    {
                        eventSystemGO.AddComponent(inputModuleType);
                    }

                    Debug.Log("[FullSceneBuilder] EventSystem created with InputSystemUIInputModule");
                }

                // --- HUD Panel ---
                CreateHUDPanel(canvasGO);

                Undo.RegisterCreatedObjectUndo(canvasGO, "Create GameUI");
            }

            Debug.Log("[FullSceneBuilder] UI created");
        }

        private static void CreateHUDPanel(GameObject canvas)
        {
            GameObject hud = new GameObject("HUD");
            hud.transform.SetParent(canvas.transform, false);

            RectTransform hudRect = hud.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0, 1);
            hudRect.anchorMax = new Vector2(0, 1);
            hudRect.pivot = new Vector2(0, 1);
            hudRect.anchoredPosition = new Vector2(10, -10);
            hudRect.sizeDelta = new Vector2(320, 200);

            Image hudImage = hud.AddComponent<Image>();
            hudImage.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

            // Location Name
            CreateTMPText(hud, "LocationText", "Cultivation World", new Vector2(10, -10), 22,
                FontStyles.Bold, Color.white);

            // Time Text
            CreateTMPText(hud, "TimeText", "День 1 — 06:00", new Vector2(10, -38), 18,
                FontStyles.Normal, new Color(0.8f, 0.9f, 1f));

            // HP Bar
            CreateBar(hud, "HealthBar", new Vector2(10, -70), 280, 20, new Color(0.8f, 0.2f, 0.2f));
            CreateTMPText(hud, "HealthText", "HP: 100/100", new Vector2(10, -95), 16,
                FontStyles.Normal, Color.white);

            // Qi Bar
            CreateBar(hud, "QiBar", new Vector2(10, -120), 280, 20, new Color(0.2f, 0.5f, 0.9f));
            CreateTMPText(hud, "QiText", "Ци: 100/100", new Vector2(10, -145), 16,
                FontStyles.Normal, Color.white);

            // Stamina Bar
            CreateBar(hud, "StaminaBar", new Vector2(10, -170), 280, 16, new Color(0.2f, 0.8f, 0.3f));
        }

        // ====================================================================
        //  PHASE 08: TILEMAP SYSTEM
        // ====================================================================

        private static bool IsTilemapNeeded()
        {
            EnsureSceneOpen();
            return UnityEngine.Object.FindFirstObjectByType<Grid>() == null;
        }

        private static void ExecuteTilemap()
        {
            EnsureSceneOpen();

            if (UnityEngine.Object.FindFirstObjectByType<Grid>() != null)
            {
                Debug.Log("[FullSceneBuilder] Grid already exists");
                return;
            }

            // Grid
            GameObject gridObj = new GameObject("Grid");
            Grid grid = gridObj.AddComponent<Grid>();
            grid.cellSize = new Vector3(2f, 2f, 1f);
            // FIX: cellGap = 0. Pixel bleed через увеличенные terrain-спрайты (68×68 PPU=32 → 2.125 юнита)
            // устраняет белую сетку надёжнее, чем отрицательный cellGap.
            // Редактировано: 2026-04-15 UTC
            grid.cellGap = Vector3.zero;

            // Terrain Tilemap
            GameObject terrainObj = new GameObject("Terrain");
            terrainObj.transform.SetParent(gridObj.transform);
            var terrainTilemap = terrainObj.AddComponent<Tilemap>();
            var terrainRenderer = terrainObj.AddComponent<TilemapRenderer>();
            // FIX-V2-2: sortOrder — это enum НАПРАВЛЕНИЯ сортировки (BottomLeft, TopRight и т.д.),
            // НЕ приоритет рендеринга! Приоритет — это Renderer.sortingOrder.
            // Раньше: terrainRenderer.sortOrder = (TilemapRenderer.SortOrder)0; — это SortOrder.BottomLeft, НЕ sortingOrder=0!
            // Теперь используем sortingLayerName + sortingOrder для правильного порядка:
            terrainRenderer.sortingLayerName = "Terrain";
            terrainRenderer.sortingOrder = 0;
            // Редактировано: 2026-04-16 11:37 UTC
            // FIX 2A: TilemapRenderer.mode = Chunk — рендерит чанк целиком,
            // нет субпиксельных зазоров между тайлами (устраняет белую сетку).
            // Individual режим создавал отдельный draw call для каждого тайла → зазоры.
            // Редактировано: 2026-04-15 17:31:49 UTC
            terrainRenderer.mode = TilemapRenderer.Mode.Chunk;
            // FIX: НЕ добавляем TilemapCollider2D на Terrain!
            // Terrain тайлы проходимые (трава, грязь) — коллайдер блокирует движение.
            // Коллайдеры terrain управляются через GameTile.colliderType (isPassable).
            // TilemapCollider2D на Terrain нужен только для непроходимых тайлов.
            // Редактировано: 2026-04-15 UTC

            // Objects Tilemap
            GameObject objectsObj = new GameObject("Objects");
            objectsObj.transform.SetParent(gridObj.transform);
            var objectTilemap = objectsObj.AddComponent<Tilemap>();
            var objectRenderer = objectsObj.AddComponent<TilemapRenderer>();
            // FIX-V2-2: То же исправление — sortOrder → sortingLayerName + sortingOrder
            objectRenderer.sortingLayerName = "Objects";
            objectRenderer.sortingOrder = 0;
            // Редактировано: 2026-04-16 11:37 UTC
            // FIX 2A: Chunk mode для объектов тоже (консистентность с terrain)
            // Редактировано: 2026-04-15 17:31:49 UTC
            objectRenderer.mode = TilemapRenderer.Mode.Chunk;
            // Коллайдер для объектов (деревья, камни и т.д.)
            objectsObj.AddComponent<TilemapCollider2D>();

            // TileMapController
            GameObject controllerObj = new GameObject("TileMapController");
            var controller = controllerObj.AddComponent<TileMapController>();

            var so = new SerializedObject(controller);
            so.FindProperty("terrainTilemap").objectReferenceValue = terrainTilemap;
            so.FindProperty("objectTilemap").objectReferenceValue = objectTilemap;
            // Размер тестовой локации: 100×80 тайлов = 200×160 метров
            // Редактировано: 2026-04-14 07:50:00 UTC — увеличено с 30×20
            so.FindProperty("defaultWidth").intValue = 100;
            so.FindProperty("defaultHeight").intValue = 80;
            so.ApplyModifiedProperties();

            // TestLocationGameController
            var gameControllerObj = new GameObject("GameController");
            var gameController = gameControllerObj.AddComponent<TestLocationGameController>();

            var gso = new SerializedObject(gameController);
            gso.FindProperty("tileMapController").objectReferenceValue = controller;
            gso.ApplyModifiedProperties();

            // DestructibleObjectController
            var destructibleController = gameControllerObj.AddComponent<DestructibleObjectController>();
            var dso = new SerializedObject(destructibleController);
            dso.FindProperty("tileMapController").objectReferenceValue = controller;
            dso.ApplyModifiedProperties();

            Undo.RegisterCreatedObjectUndo(gridObj, "Create Tilemap System");
            // FIX-V2-6: Диагностика Tilemap
            // Редактировано: 2026-04-16 11:37 UTC
            RenderPipelineLogger.LogTilemapState();
            Debug.Log("[FullSceneBuilder] Tilemap system created");
        }

        // ====================================================================
        //  PHASE 09: GENERATE ASSETS FROM JSON
        // ====================================================================

        private static bool IsGenerateAssetsNeeded()
        {
            // Проверяем: есть ли хоть один .asset в папках данных
            return !HasAssetsInFolder("Assets/Data/CultivationLevels") ||
                   !HasAssetsInFolder("Assets/Data/Elements") ||
                   !HasAssetsInFolder("Assets/Data/MortalStages");
        }

        private static void ExecuteGenerateAssets()
        {
            int total = 0;

            // AssetGenerator (базовые данные)
            Debug.Log("[FullSceneBuilder] Generating base assets from JSON...");
            total += AssetGenerator.GenerateCultivationLevels();
            total += AssetGenerator.GenerateElements();
            total += AssetGenerator.GenerateMortalStages();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // AssetGeneratorExtended (контент)
            Debug.Log("[FullSceneBuilder] Generating extended assets from JSON...");
            total += AssetGeneratorExtended.GenerateTechniques();
            total += AssetGeneratorExtended.GenerateNPCPresets();
            total += AssetGeneratorExtended.GenerateEquipment();
            total += AssetGeneratorExtended.GenerateItems();
            total += AssetGeneratorExtended.GenerateMaterials();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // FormationAssetGenerator (формации)
            Debug.Log("[FullSceneBuilder] Generating formation assets...");
            total += FormationAssetGenerator.GenerateFormationData();
            total += FormationAssetGenerator.GenerateFormationCoreData();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Валидация
            int errors = AssetGeneratorExtended.ValidateAllAssets();
            errors += FormationAssetGenerator.ValidateFormationAssets();

            if (errors > 0)
            {
                Debug.LogWarning($"[FullSceneBuilder] Asset generation complete with {errors} validation warnings");
            }
            else
            {
                Debug.Log($"[FullSceneBuilder] Generated {total} assets — all valid!");
            }
        }

        // ====================================================================
        //  PHASE 10: GENERATE TILE SPRITES
        // ====================================================================

        private static bool IsGenerateSpritesNeeded()
        {
            return !HasAssetsInFolder("Assets/Sprites/Tiles");
        }

        private static void ExecuteGenerateSprites()
        {
            Debug.Log("[FullSceneBuilder] Generating tile sprites...");
            TileSpriteGenerator.GenerateAllSprites();
        }

        // ====================================================================
        //  PHASE 11: GENERATE FORMATION UI PREFABS
        // ====================================================================

        private static bool IsGenerateUIPrefabsNeeded()
        {
            return !System.IO.File.Exists("Assets/Prefabs/UI/Formation/FormationListItem.prefab");
        }

        private static void ExecuteGenerateUIPrefabs()
        {
            Debug.Log("[FullSceneBuilder] Generating formation UI prefabs...");
            FormationUIPrefabsGenerator.GenerateAllPrefabs();
        }

        // ====================================================================
        //  PHASE 12: TMP ESSENTIALS
        // ====================================================================

        private static bool IsTMPEssentialsNeeded()
        {
            // Проверяем: существует ли TMP Settings
            var tmpSettings = TMPro.TMP_Settings.instance;
            return tmpSettings == null;
        }

        private static void ExecuteTMPEssentials()
        {
            Debug.Log("[FullSceneBuilder] Checking TMP Essentials...");

            try
            {
                // Проверяем и импортируем TMP Essentials
                var tmpSettings = TMPro.TMP_Settings.instance;
                if (tmpSettings == null)
                {
                    Debug.Log("[FullSceneBuilder] TMP Essentials not found. Importing...");

                    // PackageImporter — API для импорта TMP Essentials
                    var importerType = System.Type.GetType(
                        "TMPro.TMP_PackageResourceImporter, Unity.TextMeshPro");
                    if (importerType != null)
                    {
                        // Показываем окно импорта
                        var window = EditorWindow.GetWindow(importerType);
                        if (window != null)
                        {
                            Debug.Log("[FullSceneBuilder] TMP Import window opened. Please click 'Import TMP Essentials'.");
                            return;
                        }
                    }

                    // Альтернативный путь — через меню
                    Debug.LogWarning("[FullSceneBuilder] Cannot auto-import TMP. " +
                        "Please: Window → TextMeshPro → Import TMP Essentials");
                }
                else
                {
                    Debug.Log("[FullSceneBuilder] TMP Essentials already imported");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[FullSceneBuilder] TMP check failed: {ex.Message}. " +
                    "Please import manually: Window → TextMeshPro → Import TMP Essentials");
            }
        }

        // ====================================================================
        //  PHASE 13: SAVE SCENE
        // ====================================================================

        private static bool IsSaveSceneNeeded()
        {
            var activeScene = SceneManager.GetActiveScene();
            return activeScene.isDirty;
        }

        private static void ExecuteSaveScene()
        {
            var activeScene = SceneManager.GetActiveScene();

            if (string.IsNullOrEmpty(activeScene.path))
            {
                EnsureDirectory("Assets/Scenes");
                EditorSceneManager.SaveScene(activeScene, SCENE_PATH);
                Debug.Log($"[FullSceneBuilder] Scene saved to: {SCENE_PATH}");
            }
            else
            {
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log($"[FullSceneBuilder] Scene saved: {activeScene.path}");
            }
        }

        // ====================================================================
        //  SHARED HELPERS
        // ====================================================================

        /// <summary>
        /// Проверить объект на broken/missing prefab.
        /// Добавляет в список на удаление если префаб утерян.
        /// Редактировано: 2026-04-14 07:50:00 UTC
        /// </summary>
        private static void CheckForMissingPrefab(GameObject go, List<GameObject> toRemove)
        {
            if (go == null || toRemove.Contains(go)) return;

            // Проверка 1: имя содержит Missing Prefab
            if (go.name.Contains("Missing Prefab") || go.name.Contains("(Missing)"))
            {
                toRemove.Add(go);
                return;
            }

            // Проверка 2: PrefabUtility — объект является prefab instance, но префаб удалён
            if (PrefabUtility.IsPrefabAssetMissing(go))
            {
                toRemove.Add(go);
                return;
            }

            // Проверка 3: Null prefab reference (объект в сцене ссылается на несуществующий префаб)
            try
            {
                var prefabStatus = PrefabUtility.GetPrefabInstanceStatus(go);
                if (prefabStatus == PrefabInstanceStatus.MissingAsset)
                {
                    toRemove.Add(go);
                    return;
                }
            }
            catch { /* Не prefab — пропускаем */ }
        }

        /// <summary>
        /// Рекурсивно создать папку (аналог mkdir -p).
        /// </summary>
        /// <summary>
        /// Удалить объекты с Missing Prefab из активной сцены.
        /// Также удаляет broken prefab instances (объекты, чей префаб был удалён).
        /// Редактировано: 2026-04-14 07:50:00 UTC — добавлена проверка PrefabUtility
        /// </summary>
        private static void CleanMissingPrefabs()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            int removed = 0;
            var rootObjects = scene.GetRootGameObjects();

            // Собираем объекты для удаления
            var toRemove = new List<GameObject>();

            foreach (var rootObj in rootObjects)
            {
                // Проверяем корневой объект
                CheckForMissingPrefab(rootObj, toRemove);

                // Проверяем все дочерние объекты
                var transforms = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t != null && t.gameObject != null)
                    {
                        CheckForMissingPrefab(t.gameObject, toRemove);
                    }
                }
            }

            foreach (var go in toRemove)
            {
                Debug.Log($"[FullSceneBuilder] Removing missing/broken prefab: {go.name}");
                Undo.DestroyObjectImmediate(go);
                removed++;
            }

            if (removed > 0)
            {
                Debug.Log($"[FullSceneBuilder] Cleaned {removed} missing/broken prefabs");
                EditorSceneManager.SaveScene(scene);
            }

            // FIX: Также очищаем Missing Scripts (The referenced script is missing)
            // Это основная причина ошибки "The referenced script (Unknown) on this Behaviour is missing!"
            // Редактировано: 2026-04-15 UTC
            CleanMissingScripts();
        }

        /// <summary>
        /// Удалить компоненты с отсутствующими скриптами (Missing Scripts).
        /// Исправляет ошибку "The referenced script (Unknown) on this Behaviour is missing!"
        /// Редактировано: 2026-04-15 12:00:00 UTC
        /// </summary>
        private static void CleanMissingScripts()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            int totalRemoved = 0;
            var rootObjects = scene.GetRootGameObjects();

            foreach (var rootObj in rootObjects)
            {
                var allTransforms = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t == null || t.gameObject == null) continue;
                    var go = t.gameObject;

                    // GameObjectUtility.RemoveMonoBehavioursWithMissingScript — Unity 6 API
                    int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                    if (removed > 0)
                    {
                        Debug.Log($"[FullSceneBuilder] Removed {removed} missing script(s) from: {go.name}");
                        totalRemoved += removed;
                    }
                }
            }

            if (totalRemoved > 0)
            {
                Debug.Log($"[FullSceneBuilder] Total missing scripts removed: {totalRemoved}");
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static void EnsureDirectory(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path);
            string folder = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureDirectory(parent);
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                // Fallback: используем System.IO
                Directory.CreateDirectory(path);
                return;
            }

            AssetDatabase.CreateFolder(parent, folder);
        }

        /// <summary>
        /// Безопасная установка свойства SerializedObject.
        /// </summary>
        // Редактировано: 2026-04-13 14:03:25 UTC — FIX: enum fields support via enumValueIndex
        private static void SetProperty(SerializedObject so, string propertyName, object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[FullSceneBuilder] Property '{propertyName}' not found");
                return;
            }

            // FIX: Для enum-полей используем enumValueIndex вместо intValue
            // Unity SerializedProperty: enum хранятся как int index, но propertyType == Enum
            // Вызов intValue на enum-поле вызывает "type is not a supported int value"
            if (prop.propertyType == SerializedPropertyType.Enum && value is int enumIndex)
            {
                prop.enumValueIndex = enumIndex;
                return;
            }

            switch (value)
            {
                case int intVal:
                    prop.intValue = intVal;
                    break;
                case float floatVal:
                    prop.floatValue = floatVal;
                    break;
                case bool boolVal:
                    prop.boolValue = boolVal;
                    break;
                case string strVal:
                    prop.stringValue = strVal;
                    break;
                case long longVal:
                    prop.longValue = longVal;
                    break;
                case double doubleVal:
                    prop.doubleValue = doubleVal;
                    break;
            }
        }

        /// <summary>
        /// Настроить компонент на Player через callback.
        /// </summary>
        private static void SetupPlayerComponent<T>(GameObject player, System.Action<T> setup) where T : Component
        {
            var component = player.GetComponent<T>();
            if (component != null)
            {
                setup(component);
            }
        }

        /// <summary>
        /// Создать TMP текстовый элемент.
        /// </summary>
        private static TextMeshProUGUI CreateTMPText(
            GameObject parent, string name, string text,
            Vector2 position, int fontSize,
            FontStyles fontStyle, Color color)
        {
            GameObject textGO = new GameObject(name);
            textGO.transform.SetParent(parent.transform, false);

            RectTransform rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(280, fontSize + 10);

            var tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = fontStyle;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.TopLeft;

            return tmp;
        }

        /// <summary>
        /// Создать прогресс-бар.
        /// </summary>
        private static Slider CreateBar(
            GameObject parent, string name,
            Vector2 position, float width, float height,
            Color fillColor)
        {
            GameObject sliderGO = new GameObject(name);
            sliderGO.transform.SetParent(parent.transform, false);

            RectTransform sliderRect = sliderGO.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 1);
            sliderRect.anchorMax = new Vector2(0, 1);
            sliderRect.pivot = new Vector2(0, 1);
            sliderRect.anchoredPosition = position;
            sliderRect.sizeDelta = new Vector2(width, height);

            Slider slider = sliderGO.AddComponent<Slider>();

            // Background
            GameObject bgGO = new GameObject("Background");
            bgGO.transform.SetParent(sliderGO.transform, false);
            RectTransform bgRect = bgGO.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgGO.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.25f);

            // Fill Area
            GameObject fillAreaGO = new GameObject("Fill Area");
            fillAreaGO.transform.SetParent(sliderGO.transform, false);
            RectTransform fillAreaRect = fillAreaGO.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            // Fill
            GameObject fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(fillAreaGO.transform, false);
            RectTransform fillRect = fillGO.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fillGO.AddComponent<Image>();
            fillImage.color = fillColor;

            slider.targetGraphic = bgImage;
            slider.fillRect = fillRect;
            slider.handleRect = null;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0;
            slider.maxValue = 100;
            slider.value = 100;
            slider.interactable = false;

            return slider;
        }

        /// <summary>
        /// Проверить: есть ли ассеты в папке.
        /// </summary>
        private static bool HasAssetsInFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
                return false;

            var guids = AssetDatabase.FindAssets("", new[] { folderPath });
            return guids.Length > 0;
        }

        // ====================================================================
        //  PLAYER SPRITE GENERATOR
        // ====================================================================

        /// <summary>
        /// Загружает спрайт персонажа из Assets/Sprites/Characters/Player/.
        /// Если не найден — генерирует процедурный (fallback).
        /// Редактировано: 2026-04-15 12:00:00 UTC — исправлены пути (добавлено .png), добавлен PlayerVisual
        /// </summary>
        private static Sprite CreatePlayerSprite()
        {
            // Попробовать загрузить готовый спрайт персонажа
            // FIX: Добавлены расширения .png — AssetDatabase.LoadAssetAtPath требует полный путь
            // Редактировано: 2026-04-15 12:00:00 UTC
            string[] playerSpritePaths = new string[]
            {
                "Assets/Sprites/Characters/Player/player_variant1_cultivator.png",
                "Assets/Sprites/Characters/Player/player_variant3_warrior.png",
                "Assets/Sprites/player_sprite.png",
            };

            foreach (var spritePath in playerSpritePaths)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    // Переимпортировать с правильным PPU=32 (один тайл = 2 юнита)
                    string assetPath = AssetDatabase.GetAssetPath(sprite);
                    var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spritePixelsPerUnit = 32;
                        importer.filterMode = FilterMode.Point;
                        importer.wrapMode = TextureWrapMode.Clamp;
                        AssetDatabase.ImportAsset(assetPath);
                        // Перезагрузить после реимпорта
                        sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                    }

                    if (sprite != null)
                    {
                        Debug.Log($"[FullSceneBuilder] Player sprite loaded from: {spritePath}");
                        return sprite;
                    }
                }
            }

            // Fallback: процедурная генерация
            Debug.LogWarning("[FullSceneBuilder] Player sprite assets not found, using procedural fallback");
            return CreateProceduralPlayerSprite();
        }

        /// <summary>
        /// Процедурная генерация спрайта персонажа (fallback).
        /// Редактировано: 2026-04-15 07:10:00 UTC — PPU=32 вместо 64
        /// </summary>
        private static Sprite CreateProceduralPlayerSprite()
        {
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            Color bodyColor = new Color(0.3f, 0.7f, 0.9f);   // Голубое тело
            Color outlineColor = new Color(0.1f, 0.3f, 0.5f); // Тёмно-синий контур
            Color headColor = new Color(0.95f, 0.85f, 0.7f);   // Телесный цвет головы
            Color eyeColor = new Color(0.1f, 0.1f, 0.1f);      // Глаза
            Color clear = Color.clear;

            // Заполняем прозрачным
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            int cx = size / 2;

            // Голова (круг r=8, центр y=50)
            FillCircle(pixels, size, cx, 50, 8, headColor);
            FillCircle(pixels, size, cx, 50, 8, outlineColor, true);
            // Глаза
            FillCircle(pixels, size, cx - 3, 51, 1, eyeColor);
            FillCircle(pixels, size, cx + 3, 51, 1, eyeColor);

            // Шея
            FillRect(pixels, size, cx - 2, 42, 5, 3, bodyColor);

            // Тело (прямоугольник)
            FillRect(pixels, size, cx - 8, 24, 17, 18, bodyColor);
            FillRect(pixels, size, cx - 8, 24, 17, 18, outlineColor, true);

            // Руки
            FillRect(pixels, size, cx - 12, 26, 4, 14, bodyColor);
            FillRect(pixels, size, cx - 12, 26, 4, 14, outlineColor, true);
            FillRect(pixels, size, cx + 9, 26, 4, 14, bodyColor);
            FillRect(pixels, size, cx + 9, 26, 4, 14, outlineColor, true);

            // Ноги
            FillRect(pixels, size, cx - 6, 8, 5, 16, bodyColor);
            FillRect(pixels, size, cx - 6, 8, 5, 16, outlineColor, true);
            FillRect(pixels, size, cx + 2, 8, 5, 16, bodyColor);
            FillRect(pixels, size, cx + 2, 8, 5, 16, outlineColor, true);

            tex.SetPixels(pixels);
            tex.Apply();

            // FIX: PPU=32 — один тайл = 2 юнита Grid, персонаж занимает 1 тайл
            // Редактировано: 2026-04-15 07:10:00 UTC — было 64, теперь 32
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
        }

        /// <summary>
        /// Заливка круга (или только контура).
        /// </summary>
        private static void FillCircle(Color[] pixels, int size, int cx, int cy, int radius, Color color, bool outlineOnly = false)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    if (outlineOnly)
                    {
                        if (dist >= radius - 1.5f && dist <= radius + 0.5f)
                            pixels[y * size + x] = color;
                    }
                    else
                    {
                        if (dist <= radius)
                            pixels[y * size + x] = color;
                    }
                }
            }
        }

        /// <summary>
        /// Заливка прямоугольника (или только контура).
        /// </summary>
        private static void FillRect(Color[] pixels, int size, int x0, int y0, int w, int h, Color color, bool outlineOnly = false)
        {
            for (int y = y0; y < y0 + h && y < size; y++)
            {
                for (int x = x0; x < x0 + w && x < size; x++)
                {
                    if (y < 0 || x < 0) continue;
                    if (outlineOnly)
                    {
                        bool isBorder = (y == y0 || y == y0 + h - 1 || x == x0 || x == x0 + w - 1);
                        if (isBorder)
                            pixels[y * size + x] = color;
                    }
                    else
                    {
                        pixels[y * size + x] = color;
                    }
                }
            }
        }

        // ====================================================================
        //  PHASE 14: CREATE & ASSIGN TILE ASSETS
        //  Редактировано: 2026-04-13 13:35:27 UTC
        // ====================================================================

        /// <summary>
        /// Реимпорт спрайтов тайлов с правильными PPU и прозрачностью.
        /// Все спрайты: PPU=32 Point (2.0u = точно в ячейку Grid(2,2,1)).
        /// Сканирует ТОЛЬКО Assets/Sprites/Tiles/.
        /// Редактировано: 2026-04-17 10:53 UTC — REVERT: PPU=32 Point для terrain и objects
        /// (рабочее значение 14 апреля). Убран Bilinear и PPU=30/160.
        /// </summary>
        private static void ReimportTileSprites()
        {
            // Сканируем ТОЛЬКО Tiles/ — процедурные спрайты.
            // Редактировано: 2026-04-17 10:53 UTC
            string[] spriteDirs = new string[] { "Assets/Sprites/Tiles" };
            int reimportCount = 0;

            foreach (string dir in spriteDirs)
            {
                if (!Directory.Exists(dir)) continue;

                string[] pngFiles = Directory.GetFiles(dir, "*.png");
                foreach (string pngPath in pngFiles)
                {
                    var importer = AssetImporter.GetAtPath(pngPath) as TextureImporter;
                    if (importer == null) continue;

                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    // PPU=32 для ВСЕХ спрайтов — единое рабочее значение из 14 апреля.
                    // 64/32 = 2.0 юнита = ТОЧНО в ячейку Grid(2,2,1) → нет зазоров.
                    // Должно совпадать с TileSpriteGenerator (PPU=32).
                    // Редактировано: 2026-04-17 10:53 UTC
                    importer.spritePixelsPerUnit = 32;
                    // Point для ВСЕХ спрайтов — чёткие края, нет размытия.
                    // При PPU=32 и размере 64×64 спрайт = 2.0u = точно ячейка → зазоров нет.
                    // Редактировано: 2026-04-17 10:53 UTC
                    importer.filterMode = FilterMode.Point;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.spriteBorder = Vector4.zero;
                    importer.alphaIsTransparency = true;
                    importer.textureCompression = TextureImporterCompression.Uncompressed;

                    AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
                    reimportCount++;
                }
            }

            if (reimportCount > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log($"[FullSceneBuilder] ReimportTileSprites: переимпортировано {reimportCount} спрайтов (PPU=32 Point)");
            }
            else
            {
                Debug.Log("[FullSceneBuilder] ReimportTileSprites: спрайты не найдены");
            }
        }

        private static bool IsCreateTileAssetsNeeded()
        {
            // Проверяем: существуют ли .asset файлы тайлов
            bool terrainAssetsExist = HasAssetsInFolder("Assets/Tiles/Terrain");
            bool objectAssetsExist = HasAssetsInFolder("Assets/Tiles/Objects");

            if (!terrainAssetsExist || !objectAssetsExist)
                return true;

            // Проверяем: назначены ли TileBase в TileMapController
            EnsureSceneOpen();
            var controller = UnityEngine.Object.FindFirstObjectByType<TileMapController>();
            if (controller == null)
                return true;

            var so = new SerializedObject(controller);
            var grassProp = so.FindProperty("grassTile");
            if (grassProp == null || grassProp.objectReferenceValue == null)
                return true;

            return false;
        }

        private static void ExecuteCreateTileAssets()
        {
            // Шаг 1: Убедиться что спрайты существуют
            if (!Directory.Exists("Assets/Sprites/Tiles") ||
                !File.Exists("Assets/Sprites/Tiles/terrain_grass.png"))
            {
                Debug.Log("[FullSceneBuilder] Phase 14: Спрайты не найдены, генерируем...");
                TileSpriteGenerator.GenerateAllSprites();
            }

            // Шаг 1.5: Реимпорт всех спрайтов тайлов с правильными настройками
            // PPU=32 Point для всех — рабочее значение 14 апреля
            // Редактировано: 2026-04-17 10:53 UTC
            ReimportTileSprites();

            // Шаг 2: Создать папки для тайлов
            EnsureDirectory("Assets/Tiles/Terrain");
            EnsureDirectory("Assets/Tiles/Objects");

            // Шаг 3: Создать TerrainTile .asset файлы
            CreateTerrainTileAsset("Tile_Grass", "terrain_grass", TerrainType.Grass, 1.0f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_Dirt", "terrain_dirt", TerrainType.Dirt, 1.0f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_Stone", "terrain_stone", TerrainType.Stone, 1.0f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_WaterShallow", "terrain_water_shallow", TerrainType.Water_Shallow, 2.0f, true, GameTileFlags.Passable | GameTileFlags.Swimable);
            CreateTerrainTileAsset("Tile_WaterDeep", "terrain_water_deep", TerrainType.Water_Deep, 0.0f, false, GameTileFlags.Swimable | GameTileFlags.Flyable);
            CreateTerrainTileAsset("Tile_Sand", "terrain_sand", TerrainType.Sand, 1.2f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_Void", "terrain_void", TerrainType.Void, 0.0f, false, GameTileFlags.None);
            // FIX: Добавлены Snow, Ice и Lava .asset файлы
            // Редактировано: 2026-04-14 13:55:00 UTC
            CreateTerrainTileAsset("Tile_Snow", "terrain_snow", TerrainType.Snow, 1.3f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_Ice", "terrain_ice", TerrainType.Ice, 1.5f, true, GameTileFlags.Passable);
            CreateTerrainTileAsset("Tile_Lava", "terrain_lava", TerrainType.Lava, 0.0f, false, GameTileFlags.Flyable);

            // Шаг 4: Создать ObjectTile .asset файлы
            CreateObjectTileAsset("Tile_Tree", "obj_tree", TileObjectType.Tree_Oak, 200, true, true, true);
            CreateObjectTileAsset("Tile_RockSmall", "obj_rock_small", TileObjectType.Rock_Small, 100, false, false, true);
            CreateObjectTileAsset("Tile_RockMedium", "obj_rock_medium", TileObjectType.Rock_Medium, 300, true, true, true);
            CreateObjectTileAsset("Tile_Bush", "obj_bush", TileObjectType.Bush, 50, false, false, false);
            CreateObjectTileAsset("Tile_Chest", "obj_chest", TileObjectType.Chest, 50, false, false, false);
            // FIX: Добавлены OreVein и Herb .asset файлы
            // Редактировано: 2026-04-14 06:41:00 UTC
            CreateObjectTileAsset("Tile_OreVein", "obj_ore_vein", TileObjectType.OreVein, 250, true, true, true);
            CreateObjectTileAsset("Tile_Herb", "obj_herb", TileObjectType.Herb, 30, false, false, false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Шаг 5: Назначить TileBase в TileMapController
            AssignTileBasesToController();

            Debug.Log("[FullSceneBuilder] Phase 14: Tile assets созданы и назначены");
        }

        /// <summary>
        /// Создать TerrainTile .asset файл.
        /// </summary>
        private static void CreateTerrainTileAsset(string assetName, string spriteName, TerrainType terrainType, float moveCost, bool isPassable, GameTileFlags flags)
        {
            string assetPath = $"Assets/Tiles/Terrain/{assetName}.asset";

            // Пропустить если уже существует
            if (AssetDatabase.LoadAssetAtPath<TerrainTile>(assetPath) != null)
                return;

            // Создать asset
            var tile = ScriptableObject.CreateInstance<TerrainTile>();
            tile.terrainType = terrainType;
            tile.moveCost = moveCost;
            tile.isPassable = isPassable;
            tile.flags = flags;
            tile.color = Color.white;

            // Назначить спрайт
            string spritePath = $"Assets/Sprites/Tiles/{spriteName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                tile.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"[FullSceneBuilder] Спрайт не найден: {spritePath}");
            }

            AssetDatabase.CreateAsset(tile, assetPath);
            Debug.Log($"[FullSceneBuilder] Создан TerrainTile: {assetPath}");
        }

        /// <summary>
        /// Создать ObjectTile .asset файл.
        /// </summary>
        private static void CreateObjectTileAsset(string assetName, string spriteName, TileObjectType objectType, int durability, bool blocksVision, bool providesCover, bool isHarvestable)
        {
            string assetPath = $"Assets/Tiles/Objects/{assetName}.asset";

            // Пропустить если уже существует
            if (AssetDatabase.LoadAssetAtPath<ObjectTile>(assetPath) != null)
                return;

            // Создать asset
            var tile = ScriptableObject.CreateInstance<ObjectTile>();
            tile.objectType = objectType;
            tile.durability = durability;
            tile.blocksVision = blocksVision;
            tile.providesCover = providesCover;
            tile.isHarvestable = isHarvestable;
            tile.isPassable = false; // Объекты по умолчанию непроходимы
            tile.flags = GameTileFlags.None;
            tile.color = Color.white;

            // Назначить спрайт
            string spritePath = $"Assets/Sprites/Tiles/{spriteName}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                tile.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"[FullSceneBuilder] Спрайт не найден: {spritePath}");
            }

            AssetDatabase.CreateAsset(tile, assetPath);
            Debug.Log($"[FullSceneBuilder] Создан ObjectTile: {assetPath}");
        }

        /// <summary>
        /// Назначить TileBase ссылки в TileMapController через SerializedObject.
        /// </summary>
        private static void AssignTileBasesToController()
        {
            EnsureSceneOpen();

            var controller = UnityEngine.Object.FindFirstObjectByType<TileMapController>();
            if (controller == null)
            {
                Debug.LogWarning("[FullSceneBuilder] TileMapController не найден в сцене. Пропускаем назначение TileBase.");
                return;
            }

            var so = new SerializedObject(controller);

            // Terrain Tiles
            AssignTileProperty(so, "grassTile", "Assets/Tiles/Terrain/Tile_Grass.asset");
            AssignTileProperty(so, "dirtTile", "Assets/Tiles/Terrain/Tile_Dirt.asset");
            AssignTileProperty(so, "stoneTile", "Assets/Tiles/Terrain/Tile_Stone.asset");
            AssignTileProperty(so, "waterShallowTile", "Assets/Tiles/Terrain/Tile_WaterShallow.asset");
            AssignTileProperty(so, "waterDeepTile", "Assets/Tiles/Terrain/Tile_WaterDeep.asset");
            AssignTileProperty(so, "sandTile", "Assets/Tiles/Terrain/Tile_Sand.asset");
            AssignTileProperty(so, "voidTile", "Assets/Tiles/Terrain/Tile_Void.asset");
            // FIX: Добавлены Snow, Ice и Lava tile назначения
            // Редактировано: 2026-04-14 13:55:00 UTC
            AssignTileProperty(so, "snowTile", "Assets/Tiles/Terrain/Tile_Snow.asset");
            AssignTileProperty(so, "iceTile", "Assets/Tiles/Terrain/Tile_Ice.asset");
            AssignTileProperty(so, "lavaTile", "Assets/Tiles/Terrain/Tile_Lava.asset");

            // Object Tiles
            AssignTileProperty(so, "treeTile", "Assets/Tiles/Objects/Tile_Tree.asset");
            AssignTileProperty(so, "rockSmallTile", "Assets/Tiles/Objects/Tile_RockSmall.asset");
            AssignTileProperty(so, "rockMediumTile", "Assets/Tiles/Objects/Tile_RockMedium.asset");
            AssignTileProperty(so, "bushTile", "Assets/Tiles/Objects/Tile_Bush.asset");
            AssignTileProperty(so, "chestTile", "Assets/Tiles/Objects/Tile_Chest.asset");
            // FIX: Добавлены OreVein и Herb tile назначения
            AssignTileProperty(so, "oreVeinTile", "Assets/Tiles/Objects/Tile_OreVein.asset");
            AssignTileProperty(so, "herbTile", "Assets/Tiles/Objects/Tile_Herb.asset");

            so.ApplyModifiedProperties();
            Debug.Log("[FullSceneBuilder] TileBase ссылки назначены в TileMapController");
        }

        /// <summary>
        /// Назначить свойство TileBase из asset файла.
        /// </summary>
        private static void AssignTileProperty(SerializedObject so, string propertyName, string assetPath)
        {
            var prop = so.FindProperty(propertyName);
            if (prop == null)
            {
                Debug.LogWarning($"[FullSceneBuilder] Свойство '{propertyName}' не найдено в TileMapController");
                return;
            }

            var tileAsset = AssetDatabase.LoadAssetAtPath<TileBase>(assetPath);
            if (tileAsset != null)
            {
                prop.objectReferenceValue = tileAsset;
            }
            else
            {
                Debug.LogWarning($"[FullSceneBuilder] Asset не найден: {assetPath}");
            }
        }

        // ====================================================================
        //  PHASE 15: CONFIGURE TEST LOCATION
        //  Редактировано: 2026-04-13 13:35:27 UTC
        // ====================================================================

        private static bool IsConfigureTestLocationNeeded()
        {
            EnsureSceneOpen();

            // Проверяем: настроена ли камера для тайловой карты
            var cam = Camera.main;
            if (cam == null)
                return true;

            // Если камера на позиции (0,0,-10) — значит ещё не настроена для тайловой карты
            if (cam.transform.position.x < 1f && cam.transform.position.y < 1f && cam.orthographicSize < 8f)
                return true;

            // Проверяем: есть ли объект Grid в сцене
            if (UnityEngine.Object.FindFirstObjectByType<Grid>() == null)
                return true;

            return false;
        }

        private static void ExecuteConfigureTestLocation()
        {
            EnsureSceneOpen();

            // --- Камера: позиционировать для тайловой карты ---
            var cam = Camera.main;
            if (cam != null)
            {
                // Найти TileMapController для определения центра карты
                var controller = UnityEngine.Object.FindFirstObjectByType<TileMapController>();
                if (controller != null)
                {
                    var so = new SerializedObject(controller);
                    int width = so.FindProperty("defaultWidth")?.intValue ?? 30;
                    int height = so.FindProperty("defaultHeight")?.intValue ?? 20;

                    // Центр карты в мировых координатах (тайл = 2м)
                    float centerX = width * 2f / 2f;
                    float centerY = height * 2f / 2f;

                    cam.transform.position = new Vector3(centerX, centerY, -10f);
                    cam.orthographicSize = 8f;
                    cam.orthographic = true;

                    // Camera2DSetup — привязка к игроку и границы карты
                    // Редактировано: 2026-04-14 07:50:00 UTC
                    var camera2D = cam.GetComponent<Camera2DSetup>();
                    if (camera2D == null)
                        camera2D = cam.gameObject.AddComponent<Camera2DSetup>();

                    var camSo = new SerializedObject(camera2D);
                    camSo.FindProperty("orthographicSize").floatValue = 8f;
                    camSo.FindProperty("followEnabled").boolValue = true;
                    camSo.FindProperty("followSmoothness").floatValue = 0.08f;
                    camSo.FindProperty("useBounds").boolValue = true;
                    camSo.FindProperty("boundsMin").vector2Value = Vector2.zero;
                    camSo.FindProperty("boundsMax").vector2Value = new Vector2(width * 2f, height * 2f);
                    camSo.ApplyModifiedProperties();

                    Debug.Log($"[FullSceneBuilder] Камера настроена: позиция ({centerX}, {centerY}, -10), size=8, Camera2DSetup привязан");
                }
            }

            // --- Terrain & Objects Colliders ---
            // FIX: Terrain НЕ должен иметь TilemapCollider2D — он блокирует движение!
            // Только непроходимые terrain (вода, void) создают коллайдер через GameTile.colliderType.
            // Objects (деревья, камни) ДОЛЖНЫ иметь TilemapCollider2D.
            // Редактировано: 2026-04-15 UTC
            var grid = UnityEngine.Object.FindFirstObjectByType<Grid>();
            if (grid != null)
            {
                // Найти Terrain дочерний объект — УДАЛИТЬ коллайдер если есть
                var terrainTransform = grid.transform.Find("Terrain");
                if (terrainTransform != null)
                {
                    var terrainObj = terrainTransform.gameObject;
                    var terrainCollider = terrainObj.GetComponent<TilemapCollider2D>();
                    if (terrainCollider != null)
                    {
                        Object.DestroyImmediate(terrainCollider);
                        Debug.Log("[FullSceneBuilder] Удалён TilemapCollider2D с Terrain (блокировал движение)");
                    }
                }

                // Найти Objects дочерний объект
                var objectsTransform = grid.transform.Find("Objects");
                if (objectsTransform != null)
                {
                    var objectsObj = objectsTransform.gameObject;

                    // Проверить/добавить TilemapCollider2D на Objects
                    var objectCollider = objectsObj.GetComponent<TilemapCollider2D>();
                    if (objectCollider == null)
                    {
                        objectsObj.AddComponent<TilemapCollider2D>();
                        Debug.Log("[FullSceneBuilder] TilemapCollider2D добавлен на Objects");
                    }
                }
            }

            // --- Убедиться что TestLocationGameController назначен ---
            var gameController = UnityEngine.Object.FindFirstObjectByType<TestLocationGameController>();
            if (gameController == null)
            {
                // Создать если нет
                var controller = UnityEngine.Object.FindFirstObjectByType<TileMapController>();
                if (controller != null)
                {
                    var gcObj = new GameObject("GameController");
                    var gc = gcObj.AddComponent<TestLocationGameController>();

                    var gso = new SerializedObject(gc);
                    gso.FindProperty("tileMapController").objectReferenceValue = controller;
                    gso.ApplyModifiedProperties();

                    // DestructibleObjectController
                    var dc = gcObj.AddComponent<DestructibleObjectController>();
                    var dso = new SerializedObject(dc);
                    dso.FindProperty("tileMapController").objectReferenceValue = controller;
                    dso.ApplyModifiedProperties();

                    // Шаг 8: HarvestableSpawner — спавнер harvestable-объектов как GameObject
                    // Редактировано: 2026-04-16
                    var hs = gcObj.AddComponent<HarvestableSpawner>();
                    var hso = new SerializedObject(hs);
                    hso.FindProperty("tileMapController").objectReferenceValue = controller;
                    hso.ApplyModifiedProperties();

                    Debug.Log("[FullSceneBuilder] GameController + DestructibleObjectController + HarvestableSpawner созданы");
                }
            }

            Debug.Log("[FullSceneBuilder] Phase 15: Test Location настроена");
        }
    }
}
#endif
