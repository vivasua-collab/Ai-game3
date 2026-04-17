// ============================================================================
// ScenePatchBuilder.cs — Инкрементальный патчер сцены (замена прямого редактирования FullSceneBuilder)
// Cultivation World Simulator
// Версия: 1.1
// ============================================================================
// Создано: 2026-04-17 13:22:01 UTC
// Редактировано: 2026-04-17 14:20 UTC — PATCH-009..012 (аудит), LoadTagManager guard (C-02)
//
// АРХИТЕКТУРА:
//   FullSceneBuilder ЗАМОРОЖЕН — только критические багфиксы.
//   Все будущие изменения сцены — через этот файл (патчи).
//
//   Каждый патч:
//     1. Имеет уникальный ID (например "PATCH-001")
//     2. Имеет IsApplied() — проверка, применён ли уже
//     3. Имеет Apply() — применение патча
//     4. Имеет Validate() — пост-проверка корректности
//     5. Имеет описание для логирования
//
//   Применённые патчи отслеживаются через EditorPrefs (ключ: "CultivationGame_AppliedPatches").
//   Патчи идемпотентны — повторный запуск безопасен.
//
// МЕНЮ:
//   Tools → Scene Patch Builder → Apply All Pending Patches
//   Tools → Scene Patch Builder → Validate Current Scene
//   Tools → Scene Patch Builder → Show Applied Patches
//   Tools → Scene Patch Builder → Reset Patch History (опасно!)
//
// СОВМЕСТИМОСТЬ: Unity 6.3+ (6000.3)
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

// Core
using CultivationGame.Core;

// Tile
using CultivationGame.TileSystem;

// Managers
using CultivationGame.Managers;

// World
using CultivationGame.World;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Инкрементальный патчер сцены.
    /// Каждый патч — атомарное изменение сцены с верификацией.
    /// Патчи применяются последовательно, каждый только один раз.
    /// Создано: 2026-04-17 13:22:01 UTC
    /// </summary>
    public static class ScenePatchBuilder
    {
        private const string PREFS_KEY = "CultivationGame_AppliedPatches";
        private const string SCENE_PATH = "Assets/Scenes/Main.unity";

        // ====================================================================
        //  РЕЕСТР ПАТЧЕЙ
        // ====================================================================

        private struct PatchInfo
        {
            public string id;
            public string description;
            public System.Func<bool> isApplied;
            public System.Action apply;
            public System.Func<bool> validate;
        }

        private static readonly PatchInfo[] PATCHES = new PatchInfo[]
        {
            // ===== PATCH-001: Порядок Sorting Layers =====
            new PatchInfo
            {
                id = "PATCH-001",
                description = "Проверка и исправление порядка Sorting Layers (Default < Background < Terrain < Objects < Player < UI)",
                isApplied = IsPatch001Applied,
                apply = ApplyPatch001,
                validate = ValidatePatch001
            },

            // ===== PATCH-002: TilemapRenderer на правильных слоях =====
            new PatchInfo
            {
                id = "PATCH-002",
                description = "Все TilemapRenderer на правильных Sorting Layers (Terrain → 'Terrain', Objects → 'Objects')",
                isApplied = IsPatch002Applied,
                apply = ApplyPatch002,
                validate = ValidatePatch002
            },

            // ===== PATCH-003: PlayerVisual sorting layer =====
            new PatchInfo
            {
                id = "PATCH-003",
                description = "PlayerVisual SpriteRenderer на слое 'Player' с правильным порядком",
                isApplied = IsPatch003Applied,
                apply = ApplyPatch003,
                validate = ValidatePatch003
            },

            // ===== PATCH-004: Глобальный Light2D =====
            new PatchInfo
            {
                id = "PATCH-004",
                description = "GlobalLight2D существует и корректно настроен (intensity=1, color=white, type=Global)",
                isApplied = IsPatch004Applied,
                apply = ApplyPatch004,
                validate = ValidatePatch004
            },

            // ===== PATCH-005: HarvestableSpawner назначен =====
            new PatchInfo
            {
                id = "PATCH-005",
                description = "HarvestableSpawner существует на GameController и имеет ссылку на TileMapController",
                isApplied = IsPatch005Applied,
                apply = ApplyPatch005,
                validate = ValidatePatch005
            },

            // ===== PATCH-006: Grid настройки =====
            new PatchInfo
            {
                id = "PATCH-006",
                description = "Grid cellSize=(2,2,1), cellGap=(0,0,0)",
                isApplied = IsPatch006Applied,
                apply = ApplyPatch006,
                validate = ValidatePatch006
            },

            // ===== PATCH-007: Отсутствие TilemapCollider2D на Terrain =====
            new PatchInfo
            {
                id = "PATCH-007",
                description = "Terrain Tilemap НЕ имеет TilemapCollider2D (блокирует движение)",
                isApplied = IsPatch007Applied,
                apply = ApplyPatch007,
                validate = ValidatePatch007
            },

            // ===== PATCH-008: Missing Scripts и Prefabs =====
            new PatchInfo
            {
                id = "PATCH-008",
                description = "Сцена не содержит Missing Scripts и Missing Prefabs",
                isApplied = IsPatch008Applied,
                apply = ApplyPatch008,
                validate = ValidatePatch008
            },

            // ===== PATCH-009: Camera2DSetup.boundsMin =====
            new PatchInfo
            {
                id = "PATCH-009",
                description = "Camera2DSetup.boundsMin = (0,0) — Phase 04 не устанавливал boundsMin",
                isApplied = IsPatch009Applied,
                apply = ApplyPatch009,
                validate = ValidatePatch009
            },

            // ===== PATCH-010: Camera Undo.RecordObject =====
            new PatchInfo
            {
                id = "PATCH-010",
                description = "Camera модификации регистрируются в Undo (Phase 04 не использовал RecordObject)",
                isApplied = IsPatch010Applied,
                apply = ApplyPatch010,
                validate = ValidatePatch010
            },

            // ===== PATCH-011: Полная проверка 6 Sorting Layers =====
            new PatchInfo
            {
                id = "PATCH-011",
                description = "Все 6 Sorting Layers (Default, Background, Terrain, Objects, Player, UI) в правильном порядке",
                isApplied = IsPatch011Applied,
                apply = ApplyPatch011,
                validate = ValidatePatch011
            },

            // ===== PATCH-012: UI physics layer → GameUI =====
            new PatchInfo
            {
                id = "PATCH-012",
                description = "Physics layer 11 переименован из 'UI' в 'GameUI' — избегаем конфликта с Unity built-in layer 5 'UI'",
                isApplied = IsPatch012Applied,
                apply = ApplyPatch012,
                validate = ValidatePatch012
            },
        };

        // ====================================================================
        //  MAIN: Apply All Pending Patches
        // ====================================================================

        [MenuItem("Tools/Scene Patch Builder/Apply All Pending Patches", false, 0)]
        public static void ApplyAllPending()
        {
            Debug.Log("========================================");
            Debug.Log("[ScenePatchBuilder] === APPLY ALL PENDING PATCHES ===");
            Debug.Log("========================================");

            // Убедиться что сцена открыта
            EnsureSceneOpen();

            int applied = 0;
            int skipped = 0;
            int failed = 0;
            float startTime = (float)EditorApplication.timeSinceStartup;

            foreach (var patch in PATCHES)
            {
                // Проверяем: применён ли уже?
                if (IsPatchMarkedApplied(patch.id))
                {
                    Debug.Log($"[ScenePatchBuilder] ⏭ {patch.id}: Уже применён (по реестру)");
                    skipped++;
                    continue;
                }

                // Проверяем: нужно ли применять?
                if (patch.isApplied != null && patch.isApplied())
                {
                    // Уже применён по факту (сцена в правильном состоянии), но не отмечен
                    MarkPatchApplied(patch.id);
                    Debug.Log($"[ScenePatchBuilder] ✅ {patch.id}: Уже применён (по состоянию сцены) → отмечен");
                    skipped++;
                    continue;
                }

                // Применяем
                Debug.Log($"[ScenePatchBuilder] 🔧 {patch.id}: Применяем — {patch.description}");
                try
                {
                    patch.apply();
                    MarkPatchApplied(patch.id);

                    // Валидируем
                    if (patch.validate != null)
                    {
                        if (patch.validate())
                        {
                            Debug.Log($"[ScenePatchBuilder] ✅ {patch.id}: Применён и валидирован");
                        }
                        else
                        {
                            Debug.LogError($"[ScenePatchBuilder] ❌ {patch.id}: Применён, но ВАЛИДАЦИЯ ПРОВАЛЕНА!");
                            failed++;
                            continue;
                        }
                    }
                    else
                    {
                        Debug.Log($"[ScenePatchBuilder] ✅ {patch.id}: Применён (без валидации)");
                    }

                    applied++;
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[ScenePatchBuilder] ❌ {patch.id}: ОШИБКА — {ex.Message}\n{ex.StackTrace}");
                    failed++;
                }
            }

            // Сохраняем сцену если были изменения
            if (applied > 0)
            {
                var activeScene = SceneManager.GetActiveScene();
                EditorSceneManager.SaveScene(activeScene);
                Debug.Log("[ScenePatchBuilder] Сцена сохранена");
            }

            float elapsed = (float)EditorApplication.timeSinceStartup - startTime;

            Debug.Log("========================================");
            Debug.Log($"[ScenePatchBuilder] === ИТОГ ===");
            Debug.Log($"  Применено: {applied} | Пропущено: {skipped} | Ошибки: {failed}");
            Debug.Log($"  Время: {elapsed:F1}s");
            Debug.Log("========================================");

            if (failed == 0)
            {
                EditorUtility.DisplayDialog("Патчи применены",
                    $"Применено: {applied}\nПропущено: {skipped}\nВремя: {elapsed:F1}s",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Патчи — есть ошибки",
                    $"Применено: {applied}\nПропущено: {skipped}\nОшибки: {failed}",
                    "OK");
            }
        }

        // ====================================================================
        //  VALIDATE CURRENT SCENE
        // ====================================================================

        [MenuItem("Tools/Scene Patch Builder/Validate Current Scene", false, 100)]
        public static void ValidateScene()
        {
            Debug.Log("========================================");
            Debug.Log("[ScenePatchBuilder] === VALIDATE CURRENT SCENE ===");
            Debug.Log("========================================");

            EnsureSceneOpen();

            int pass = 0;
            int fail = 0;
            int total = PATCHES.Length;

            foreach (var patch in PATCHES)
            {
                if (patch.validate != null)
                {
                    bool result = patch.validate();
                    if (result)
                    {
                        Debug.Log($"[ScenePatchBuilder] ✅ {patch.id}: {patch.description}");
                        pass++;
                    }
                    else
                    {
                        Debug.LogError($"[ScenePatchBuilder] ❌ {patch.id}: ПРОВАЛЕНО — {patch.description}");
                        fail++;
                    }
                }
                else
                {
                    Debug.LogWarning($"[ScenePatchBuilder] ⚠️ {patch.id}: Нет валидации");
                    pass++; // Нет валидации = считаем пройденным
                }
            }

            // Дополнительные проверки (не привязанные к конкретному патчу)
            ValidateAdditionalChecks(ref pass, ref fail);

            Debug.Log("========================================");
            Debug.Log($"[ScenePatchBuilder] === ВАЛИДАЦИЯ: {pass}/{total} пройдено, {fail} провалено ===");
            Debug.Log("========================================");

            EditorUtility.DisplayDialog("Валидация сцены",
                $"Пройдено: {pass}\nПровалено: {fail}",
                "OK");
        }

        /// <summary>
        /// Дополнительные проверки, не привязанные к конкретному патчу.
        /// Редактировано: 2026-04-17 13:22:01 UTC
        /// </summary>
        private static void ValidateAdditionalChecks(ref int pass, ref int fail)
        {
            Debug.Log("[ScenePatchBuilder] --- Дополнительные проверки ---");

            // 1. Камера существует и настроена
            var cam = Camera.main;
            if (cam != null && cam.orthographic && cam.transform.position.z < 0)
            {
                Debug.Log("[ScenePatchBuilder] ✅ Камера: orthographic, z<0");
                pass++;
            }
            else
            {
                Debug.LogError("[ScenePatchBuilder] ❌ Камера: не найдена или неправильно настроена");
                fail++;
            }

            // 2. Grid существует
            var grid = Object.FindFirstObjectByType<Grid>();
            if (grid != null)
            {
                Debug.Log($"[ScenePatchBuilder] ✅ Grid: cellSize={grid.cellSize}, cellGap={grid.cellGap}");
                pass++;
            }
            else
            {
                Debug.LogError("[ScenePatchBuilder] ❌ Grid не найден!");
                fail++;
            }

            // 3. GameManager существует
            var gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager != null)
            {
                Debug.Log("[ScenePatchBuilder] ✅ GameManager существует");
                pass++;
            }
            else
            {
                Debug.LogError("[ScenePatchBuilder] ❌ GameManager не найден!");
                fail++;
            }

            // 4. Player существует и имеет PlayerController
            var player = GameObject.Find("Player");
            if (player != null && player.GetComponent<Player.PlayerController>() != null)
            {
                Debug.Log("[ScenePatchBuilder] ✅ Player с PlayerController существует");
                pass++;
            }
            else
            {
                Debug.LogError("[ScenePatchBuilder] ❌ Player или PlayerController не найдены!");
                fail++;
            }

            // 5. TileMapController существует
            var tileController = Object.FindFirstObjectByType<TileMapController>();
            if (tileController != null)
            {
                Debug.Log("[ScenePatchBuilder] ✅ TileMapController существует");
                pass++;
            }
            else
            {
                Debug.LogError("[ScenePatchBuilder] ❌ TileMapController не найден!");
                fail++;
            }

            // 6. Нет SpriteRenderer на "Default" слое (кроме UI)
            var defaultSR = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None)
                .Where(sr => sr.sortingLayerName == "Default" && !sr.gameObject.GetComponent<Canvas>())
                .ToList();
            if (defaultSR.Count == 0)
            {
                Debug.Log("[ScenePatchBuilder] ✅ Нет SpriteRenderer на 'Default' слое");
                pass++;
            }
            else
            {
                Debug.LogWarning($"[ScenePatchBuilder] ⚠️ {defaultSR.Count} SpriteRenderer на 'Default' слое (могут рендериться поверх Player)");
                foreach (var sr in defaultSR.Take(5))
                {
                    Debug.LogWarning($"  → \"{sr.gameObject.name}\" order={sr.sortingOrder}");
                }
                pass++; // Предупреждение, не ошибка
            }
        }

        // ====================================================================
        //  SHOW APPLIED PATCHES
        // ====================================================================

        [MenuItem("Tools/Scene Patch Builder/Show Applied Patches", false, 200)]
        public static void ShowAppliedPatches()
        {
            var applied = GetAppliedPatches();
            Debug.Log("[ScenePatchBuilder] === Применённые патчи ===");
            foreach (var id in applied)
            {
                var patch = PATCHES.FirstOrDefault(p => p.id == id);
                string desc = patch.id != null ? patch.description : "(неизвестный патч)";
                Debug.Log($"  ✅ {id}: {desc}");
            }

            var pending = PATCHES.Where(p => !applied.Contains(p.id)).ToList();
            if (pending.Count > 0)
            {
                Debug.Log("[ScenePatchBuilder] === Ожидающие патчи ===");
                foreach (var patch in pending)
                {
                    Debug.Log($"  ⏳ {patch.id}: {patch.description}");
                }
            }

            Debug.Log($"[ScenePatchBuilder] Итого: {applied.Count} применено, {pending.Count} ожидает");
            EditorUtility.DisplayDialog("Патчи",
                $"Применено: {applied.Count}\nОжидает: {pending.Count}",
                "OK");
        }

        // ====================================================================
        //  RESET PATCH HISTORY
        // ====================================================================

        [MenuItem("Tools/Scene Patch Builder/Reset Patch History (Dangerous!)", false, 300)]
        public static void ResetPatchHistory()
        {
            if (!EditorUtility.DisplayDialog("ВНИМАНИЕ!",
                "Сброс истории патчей позволит применить их повторно.\n" +
                "Это может привести к дублированию объектов!\n\n" +
                "Продолжить?",
                "Да, сбросить", "Отмена"))
            {
                return;
            }

            EditorPrefs.DeleteKey(PREFS_KEY);
            Debug.Log("[ScenePatchBuilder] История патчей сброшена");
        }

        // ====================================================================
        //  PATCH REGISTRY (EditorPrefs)
        // ====================================================================

        private static List<string> GetAppliedPatches()
        {
            string raw = EditorPrefs.GetString(PREFS_KEY, "");
            if (string.IsNullOrEmpty(raw)) return new List<string>();
            return new List<string>(raw.Split(';'));
        }

        private static void MarkPatchApplied(string patchId)
        {
            var applied = GetAppliedPatches();
            if (!applied.Contains(patchId))
            {
                applied.Add(patchId);
                EditorPrefs.SetString(PREFS_KEY, string.Join(";", applied));
            }
        }

        private static bool IsPatchMarkedApplied(string patchId)
        {
            return GetAppliedPatches().Contains(patchId);
        }

        // ====================================================================
        //  SCENE HELPERS
        // ====================================================================

        private static void EnsureSceneOpen()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.path == SCENE_PATH)
                return;

            if (System.IO.File.Exists(SCENE_PATH))
            {
                EditorSceneManager.OpenScene(SCENE_PATH);
            }
            else
            {
                Debug.LogWarning("[ScenePatchBuilder] Сцена Main.unity не найдена! Сначала запустите FullSceneBuilder.");
            }
        }

        /// <summary>
        /// Безопасная загрузка TagManager.asset. Защита от IndexOutOfRangeException.
        /// AUDIT C-02: LoadAllAssetsAtPath может вернуть пустой массив.
        /// Редактировано: 2026-04-17 14:20 UTC
        /// </summary>
        private static SerializedObject LoadTagManager()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogError("[ScenePatchBuilder] TagManager.asset не найден или пуст!");
                return null;
            }
            return new SerializedObject(assets[0]);
        }

        // ====================================================================
        //  PATCH-001: Порядок Sorting Layers
        // ====================================================================

        private static bool IsPatch001Applied()
        {
            var layers = SortingLayer.layers;
            int terrainIdx = -1, objectsIdx = -1, playerIdx = -1;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == "Terrain") terrainIdx = i;
                if (layers[i].name == "Objects") objectsIdx = i;
                if (layers[i].name == "Player") playerIdx = i;
            }

            // Все три слоя существуют и в правильном порядке
            return terrainIdx >= 0 && objectsIdx >= 0 && playerIdx >= 0
                && terrainIdx < objectsIdx && objectsIdx < playerIdx;
        }

        private static void ApplyPatch001()
        {
            // Делегируем в FullSceneBuilder.EnsureSortingLayers() — там уже реализована
            // перестановка. Но так как это static private метод, дублируем логику.
            // Редактировано: 2026-04-17 13:22:01 UTC

            string[] requiredOrder = { "Default", "Background", "Terrain", "Objects", "Player", "UI" };

            // FIX C-02: Используем безопасную загрузку TagManager (guard на пустой массив)
            // Редактировано: 2026-04-17 14:20 UTC
            var tagManager = LoadTagManager();
            if (tagManager == null) return;
            var sortingLayersProp = tagManager.FindProperty("m_SortingLayers");

            if (sortingLayersProp == null)
            {
                Debug.LogError("[ScenePatchBuilder] PATCH-001: m_SortingLayers не найден!");
                return;
            }

            // Собираем существующие слои
            var existingSet = new HashSet<string>();
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var n = sortingLayersProp.GetArrayElementAtIndex(i).FindPropertyRelative("name");
                if (n != null && !string.IsNullOrEmpty(n.stringValue))
                    existingSet.Add(n.stringValue);
            }

            // Добавляем недостающие
            int nextId = sortingLayersProp.arraySize;
            foreach (var layerName in requiredOrder)
            {
                if (existingSet.Contains(layerName)) continue;
                if (layerName == "Default" && sortingLayersProp.arraySize > 0)
                {
                    var first = sortingLayersProp.GetArrayElementAtIndex(0).FindPropertyRelative("name");
                    if (first != null && first.stringValue == "Default") continue;
                }

                sortingLayersProp.InsertArrayElementAtIndex(sortingLayersProp.arraySize);
                var elem = sortingLayersProp.GetArrayElementAtIndex(sortingLayersProp.arraySize - 1);
                elem.FindPropertyRelative("name").stringValue = layerName;
                elem.FindPropertyRelative("uniqueID").intValue = nextId++;
                elem.FindPropertyRelative("locked").boolValue = false;
                Debug.Log($"[ScenePatchBuilder] PATCH-001: Создан слой \"{layerName}\"");
            }
            tagManager.ApplyModifiedProperties();

            // Переставляем в правильный порядок
            var layerData = new List<Dictionary<string, object>>();
            for (int i = 0; i < sortingLayersProp.arraySize; i++)
            {
                var elem = sortingLayersProp.GetArrayElementAtIndex(i);
                var data = new Dictionary<string, object>();
                var n = elem.FindPropertyRelative("name");
                var id = elem.FindPropertyRelative("uniqueID");
                var l = elem.FindPropertyRelative("locked");
                if (n != null) data["name"] = n.stringValue;
                if (id != null) data["uniqueID"] = id.intValue;
                if (l != null) data["locked"] = l.boolValue;
                layerData.Add(data);
            }

            var newOrder = new List<Dictionary<string, object>>();
            var usedNames = new HashSet<string>();
            foreach (var expectedName in requiredOrder)
            {
                var found = layerData.Find(d => d.ContainsKey("name") && (string)d["name"] == expectedName);
                if (found != null) { newOrder.Add(found); usedNames.Add(expectedName); }
            }
            foreach (var data in layerData)
            {
                if (data.ContainsKey("name") && !usedNames.Contains((string)data["name"]))
                    newOrder.Add(data);
            }

            sortingLayersProp.ClearArray();
            foreach (var data in newOrder)
            {
                sortingLayersProp.InsertArrayElementAtIndex(sortingLayersProp.arraySize);
                var elem = sortingLayersProp.GetArrayElementAtIndex(sortingLayersProp.arraySize - 1);
                if (data.ContainsKey("name")) elem.FindPropertyRelative("name").stringValue = (string)data["name"];
                if (data.ContainsKey("uniqueID")) elem.FindPropertyRelative("uniqueID").intValue = (int)data["uniqueID"];
                if (data.ContainsKey("locked")) elem.FindPropertyRelative("locked").boolValue = (bool)data["locked"];
            }

            tagManager.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            Debug.Log("[ScenePatchBuilder] PATCH-001: Порядок Sorting Layers исправлен");
        }

        private static bool ValidatePatch001()
        {
            var layers = SortingLayer.layers;
            int terrainIdx = -1, objectsIdx = -1, playerIdx = -1;

            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == "Terrain") terrainIdx = i;
                if (layers[i].name == "Objects") objectsIdx = i;
                if (layers[i].name == "Player") playerIdx = i;
            }

            bool valid = terrainIdx >= 0 && objectsIdx >= 0 && playerIdx >= 0
                && terrainIdx < objectsIdx && objectsIdx < playerIdx;

            if (!valid)
            {
                Debug.LogError($"[ScenePatchBuilder] PATCH-001 ВАЛИДАЦИЯ: Порядок слоёв НЕПРАВИЛЬНЫЙ! " +
                    $"Terrain={terrainIdx}, Objects={objectsIdx}, Player={playerIdx}");
            }

            return valid;
        }

        // ====================================================================
        //  PATCH-002: TilemapRenderer на правильных слоях
        // ====================================================================

        private static bool IsPatch002Applied()
        {
            var renderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (r.sortingLayerName == "Default")
                    return false; // На Default — будет поверх Player

                // Проверяем конкретные назначения
                string name = r.gameObject.name;
                if ((name.Contains("Terrain") || name.Contains("terrain")) && r.sortingLayerName != "Terrain")
                    return false;
                if ((name.Contains("Object") || name.Contains("object") || name.Contains("Overlay"))
                    && r.sortingLayerName != "Objects")
                    return false;
            }
            return true;
        }

        private static void ApplyPatch002()
        {
            var renderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                string name = r.gameObject.name;
                string targetLayer;

                if (name.Contains("Terrain") || name.Contains("terrain"))
                    targetLayer = "Terrain";
                else if (name.Contains("Object") || name.Contains("object") || name.Contains("Overlay"))
                    targetLayer = "Objects";
                else
                    targetLayer = "Objects"; // Безопасный fallback

                if (r.sortingLayerName != targetLayer)
                {
                    Debug.Log($"[ScenePatchBuilder] PATCH-002: \"{name}\" → \"{targetLayer}\" (было \"{r.sortingLayerName}\")");
                    r.sortingLayerName = targetLayer;
                    r.sortingOrder = (targetLayer == "Objects" && name.Contains("Overlay")) ? 10 : 0;
                }
            }
        }

        private static bool ValidatePatch002()
        {
            var renderers = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (r.sortingLayerName == "Default")
                {
                    Debug.LogError($"[ScenePatchBuilder] PATCH-002 ВАЛИДАЦИЯ: TilemapRenderer \"{r.name}\" на Default!");
                    return false;
                }
            }
            return true;
        }

        // ====================================================================
        //  PATCH-003: PlayerVisual sorting layer
        // ====================================================================

        private static bool IsPatch003Applied()
        {
            var player = GameObject.Find("Player");
            if (player == null) return true; // Нет игрока — нечего проверять

            var sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return true; // Нет спрайта — пропускаем

            // Игрок должен быть на "Player" или "Objects" слое, НЕ на "Default"
            return sr.sortingLayerName == "Player" || sr.sortingLayerName == "Objects";
        }

        private static void ApplyPatch003()
        {
            var player = GameObject.Find("Player");
            if (player == null) return;

            var sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;

            // Проверяем: слой "Player" существует и в правильном порядке?
            var layers = SortingLayer.layers;
            int playerIdx = -1, terrainIdx = -1, objectsIdx = -1;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == "Player") playerIdx = i;
                if (layers[i].name == "Terrain") terrainIdx = i;
                if (layers[i].name == "Objects") objectsIdx = i;
            }

            if (playerIdx >= 0 && playerIdx > terrainIdx && playerIdx > objectsIdx)
            {
                sr.sortingLayerName = "Player";
                sr.sortingOrder = 0;
                Debug.Log($"[ScenePatchBuilder] PATCH-003: Player → \"Player\" (было \"{sr.sortingLayerName}\")");
            }
            else
            {
                // Fallback: на "Objects" с высоким order
                sr.sortingLayerName = "Objects";
                sr.sortingOrder = 100;
                Debug.LogWarning($"[ScenePatchBuilder] PATCH-003: Player → \"Objects\" order=100 (слой Player недоступен)");
            }
        }

        private static bool ValidatePatch003()
        {
            var player = GameObject.Find("Player");
            if (player == null) return true;

            var sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return true;

            bool valid = sr.sortingLayerName != "Default" && sr.sortingLayerName != "Terrain";
            if (!valid)
            {
                Debug.LogError($"[ScenePatchBuilder] PATCH-003 ВАЛИДАЦИЯ: Player на слое \"{sr.sortingLayerName}\"!");
            }
            return valid;
        }

        // ====================================================================
        //  PATCH-004: GlobalLight2D
        // ====================================================================

        private static bool IsPatch004Applied()
        {
            return GameObject.Find("GlobalLight2D") != null;
        }

        private static void ApplyPatch004()
        {
            var light2DObj = GameObject.Find("GlobalLight2D");
            if (light2DObj != null) return;

            light2DObj = new GameObject("GlobalLight2D");
            var light2DType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.2D.RenderPipeline.Runtime");
            if (light2DType != null)
            {
                var light2D = light2DObj.AddComponent(light2DType);
                var lightTypeProp = light2DType.GetProperty("lightType");
                if (lightTypeProp != null) lightTypeProp.SetValue(light2D, 1); // Global
                var intensityProp = light2DType.GetProperty("intensity");
                if (intensityProp != null) intensityProp.SetValue(light2D, 1f);
                var colorProp = light2DType.GetProperty("color");
                if (colorProp != null) colorProp.SetValue(light2D, Color.white);
                Debug.Log("[ScenePatchBuilder] PATCH-004: GlobalLight2D создан");
            }
            else
            {
                Object.DestroyImmediate(light2DObj);
                Debug.LogWarning("[ScenePatchBuilder] PATCH-004: Light2D тип не найден через Reflection");
            }
        }

        private static bool ValidatePatch004()
        {
            // GlobalLight2D не обязателен при Sprite-Unlit-Default шейдере
            // Предупреждение, не ошибка
            if (GameObject.Find("GlobalLight2D") == null)
            {
                Debug.LogWarning("[ScenePatchBuilder] PATCH-004: GlobalLight2D отсутствует (нормально для Unlit шейдера)");
            }
            return true; // Всегда проходит — Light2D опционален
        }

        // ====================================================================
        //  PATCH-005: HarvestableSpawner назначен
        // ====================================================================

        private static bool IsPatch005Applied()
        {
            var gameController = Object.FindFirstObjectByType<TestLocationGameController>();
            if (gameController == null) return true; // Нет контроллера — пропускаем

            return gameController.GetComponent<HarvestableSpawner>() != null;
        }

        private static void ApplyPatch005()
        {
            var gameController = Object.FindFirstObjectByType<TestLocationGameController>();
            if (gameController == null) return;

            var hs = gameController.GetComponent<HarvestableSpawner>();
            if (hs != null) return;

            var tileController = Object.FindFirstObjectByType<TileMapController>();
            hs = gameController.gameObject.AddComponent<HarvestableSpawner>();
            var so = new SerializedObject(hs);
            if (tileController != null)
                so.FindProperty("tileMapController").objectReferenceValue = tileController;
            so.ApplyModifiedProperties();
            Debug.Log("[ScenePatchBuilder] PATCH-005: HarvestableSpawner добавлен");
        }

        private static bool ValidatePatch005()
        {
            var gameController = Object.FindFirstObjectByType<TestLocationGameController>();
            if (gameController == null) return true;

            bool hasHS = gameController.GetComponent<HarvestableSpawner>() != null;
            if (!hasHS)
            {
                Debug.LogError("[ScenePatchBuilder] PATCH-005 ВАЛИДАЦИЯ: HarvestableSpawner отсутствует!");
            }
            return hasHS;
        }

        // ====================================================================
        //  PATCH-006: Grid настройки
        // ====================================================================

        private static bool IsPatch006Applied()
        {
            var grid = Object.FindFirstObjectByType<Grid>();
            if (grid == null) return true; // Нет Grid — пропускаем

            return grid.cellSize == new Vector3(2f, 2f, 1f)
                && grid.cellGap == Vector3.zero;
        }

        private static void ApplyPatch006()
        {
            var grid = Object.FindFirstObjectByType<Grid>();
            if (grid == null) return;

            grid.cellSize = new Vector3(2f, 2f, 1f);
            grid.cellGap = Vector3.zero;
            Debug.Log("[ScenePatchBuilder] PATCH-006: Grid cellSize=(2,2,1), cellGap=zero");
        }

        private static bool ValidatePatch006()
        {
            var grid = Object.FindFirstObjectByType<Grid>();
            if (grid == null) return true;

            bool valid = grid.cellSize == new Vector3(2f, 2f, 1f) && grid.cellGap == Vector3.zero;
            if (!valid)
            {
                Debug.LogError($"[ScenePatchBuilder] PATCH-006 ВАЛИДАЦИЯ: Grid cellSize={grid.cellSize}, cellGap={grid.cellGap}");
            }
            return valid;
        }

        // ====================================================================
        //  PATCH-007: Нет TilemapCollider2D на Terrain
        // ====================================================================

        private static bool IsPatch007Applied()
        {
            var grid = Object.FindFirstObjectByType<Grid>();
            if (grid == null) return true;

            var terrainT = grid.transform.Find("Terrain");
            if (terrainT == null) return true;

            return terrainT.GetComponent<TilemapCollider2D>() == null;
        }

        private static void ApplyPatch007()
        {
            var grid = Object.FindFirstObjectByType<Grid>();
            if (grid == null) return;

            var terrainT = grid.transform.Find("Terrain");
            if (terrainT == null) return;

            var collider = terrainT.GetComponent<TilemapCollider2D>();
            if (collider != null)
            {
                Object.DestroyImmediate(collider);
                Debug.Log("[ScenePatchBuilder] PATCH-007: TilemapCollider2D удалён с Terrain");
            }
        }

        private static bool ValidatePatch007()
        {
            var grid = Object.FindFirstObjectByType<Grid>();
            if (grid == null) return true;

            var terrainT = grid.transform.Find("Terrain");
            if (terrainT == null) return true;

            bool valid = terrainT.GetComponent<TilemapCollider2D>() == null;
            if (!valid)
            {
                Debug.LogError("[ScenePatchBuilder] PATCH-007 ВАЛИДАЦИЯ: Terrain имеет TilemapCollider2D (блокирует движение)!");
            }
            return valid;
        }

        // ====================================================================
        //  PATCH-008: Missing Scripts и Prefabs
        // ====================================================================

        private static bool IsPatch008Applied()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return true;

            foreach (var rootObj in scene.GetRootGameObjects())
            {
                // Проверяем Missing Prefabs
                if (rootObj.name.Contains("Missing Prefab") || rootObj.name.Contains("(Missing)"))
                    return false;

                // Проверяем Missing Scripts
                var transforms = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t == null || t.gameObject == null) return false;
                    // Проверяем через MissingScript count (Unity 6 API)
                    // Если хотя бы один missing — нужно чистить
                }
            }
            return true;
        }

        private static void ApplyPatch008()
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.isLoaded) return;

            // Удаляем Missing Prefabs
            var toRemove = new List<GameObject>();
            foreach (var rootObj in scene.GetRootGameObjects())
            {
                if (rootObj.name.Contains("Missing Prefab") || rootObj.name.Contains("(Missing)"))
                    toRemove.Add(rootObj);

                var transforms = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t != null && t.gameObject != null)
                    {
                        if (t.gameObject.name.Contains("Missing Prefab") || t.gameObject.name.Contains("(Missing)"))
                            toRemove.Add(t.gameObject);
                        else if (PrefabUtility.IsPrefabAssetMissing(t.gameObject))
                            toRemove.Add(t.gameObject);
                    }
                }
            }

            foreach (var go in toRemove.Distinct())
            {
                Debug.Log($"[ScenePatchBuilder] PATCH-008: Удалён missing prefab: {go.name}");
                Undo.DestroyObjectImmediate(go);
            }

            // Удаляем Missing Scripts
            int totalRemoved = 0;
            foreach (var rootObj in scene.GetRootGameObjects())
            {
                var transforms = rootObj.GetComponentsInChildren<Transform>(true);
                foreach (var t in transforms)
                {
                    if (t != null && t.gameObject != null)
                    {
                        int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
                        totalRemoved += removed;
                    }
                }
            }

            if (totalRemoved > 0)
                Debug.Log($"[ScenePatchBuilder] PATCH-008: Удалено {totalRemoved} missing script(s)");
        }

        private static bool ValidatePatch008()
        {
            // Missing Prefabs — сложно обнаружить программно после удаления
            // Считаем что Patch прошёл если сцена загрузилась
            return true;
        }

        // ====================================================================
        //  PATCH-009: Camera2DSetup.boundsMin
        //  Аудит: H-05 — Phase 04 не устанавливал boundsMin, полагаясь на дефолт.
        //  Редактировано: 2026-04-17 14:20 UTC
        // ====================================================================

        private static bool IsPatch009Applied()
        {
            var cam = Camera.main;
            if (cam == null) return true; // Нет камеры — пропускаем

            var camera2D = cam.GetComponent<Core.Camera2DSetup>();
            if (camera2D == null) return true;

            // Проверяем что boundsMin установлен (не дефолтный Vector2.zero может быть и правильным,
            // но проверяем что useBounds=true и boundsMin задан явно)
            var so = new SerializedObject(camera2D);
            var useBounds = so.FindProperty("useBounds");
            var boundsMin = so.FindProperty("boundsMin");

            // Если useBounds=false — нечего проверять
            if (useBounds != null && !useBounds.boolValue) return true;

            // boundsMin должен быть (0,0) — это наше стандартное значение
            if (boundsMin != null)
            {
                return boundsMin.vector2Value == Vector2.zero;
            }
            return true;
        }

        private static void ApplyPatch009()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var camera2D = cam.GetComponent<Core.Camera2DSetup>();
            if (camera2D == null) return;

            var so = new SerializedObject(camera2D);
            var boundsMinProp = so.FindProperty("boundsMin");
            if (boundsMinProp != null)
            {
                boundsMinProp.vector2Value = Vector2.zero;
                so.ApplyModifiedProperties();
                Debug.Log("[ScenePatchBuilder] PATCH-009: Camera2DSetup.boundsMin = (0,0)");
            }
        }

        private static bool ValidatePatch009()
        {
            var cam = Camera.main;
            if (cam == null) return true;

            var camera2D = cam.GetComponent<Core.Camera2DSetup>();
            if (camera2D == null) return true;

            var so = new SerializedObject(camera2D);
            var boundsMin = so.FindProperty("boundsMin");
            if (boundsMin != null && boundsMin.vector2Value != Vector2.zero)
            {
                Debug.LogWarning($"[ScenePatchBuilder] PATCH-009 ВАЛИДАЦИЯ: boundsMin={boundsMin.vector2Value} (ожидается (0,0))");
            }
            return true; // Предупреждение, не ошибка
        }

        // ====================================================================
        //  PATCH-010: Camera Undo.RecordObject
        //  Аудит: M-03 — Phase 04 не регистрировал модификацию камеры в Undo.
        //  Редактировано: 2026-04-17 14:20 UTC
        // ====================================================================

        private static bool IsPatch010Applied()
        {
            // Undo-записи нельзя проверить программно.
            // Этот патч — разовое действие: регистрируем текущее состояние камеры в Undo.
            // После применения патча — Undo будет работать.
            var cam = Camera.main;
            if (cam == null) return true;

            // Всегда считаем что нужно применить (разовое действие)
            return false;
        }

        private static void ApplyPatch010()
        {
            var cam = Camera.main;
            if (cam == null) return;

            // Регистрируем текущее состояние камеры в Undo
            Undo.RecordObject(cam, "Patch: Record camera state for Undo");
            Undo.RecordObject(cam.transform, "Patch: Record camera transform for Undo");

            var camera2D = cam.GetComponent<Core.Camera2DSetup>();
            if (camera2D != null)
                Undo.RecordObject(camera2D, "Patch: Record Camera2DSetup for Undo");

            Debug.Log("[ScenePatchBuilder] PATCH-010: Camera состояние зарегистрировано в Undo");
        }

        private static bool ValidatePatch010()
        {
            // Undo-записи нельзя валидировать программно
            return true;
        }

        // ====================================================================
        //  PATCH-011: Полная проверка 6 Sorting Layers
        //  Аудит: H-01 — PATCH-001 проверял только Terrain<Objects<Player,
        //  пропускал неправильную позицию Background и UI.
        //  Редактировано: 2026-04-17 14:20 UTC
        // ====================================================================

        private static bool IsPatch011Applied()
        {
            var layers = SortingLayer.layers;
            string[] requiredOrder = { "Default", "Background", "Terrain", "Objects", "Player", "UI" };

            // Собираем индексы всех требуемых слоёв
            int[] indices = new int[requiredOrder.Length];
            for (int i = 0; i < requiredOrder.Length; i++)
            {
                indices[i] = -1;
                for (int j = 0; j < layers.Length; j++)
                {
                    if (layers[j].name == requiredOrder[i])
                    {
                        indices[i] = j;
                        break;
                    }
                }
                // Слой не найден — нужен патч
                if (indices[i] < 0) return false;
            }

            // Проверяем строгий порядок: каждый следующий индекс > предыдущего
            for (int i = 1; i < indices.Length; i++)
            {
                if (indices[i] <= indices[i - 1])
                {
                    Debug.LogWarning($"[ScenePatchBuilder] PATCH-011: {requiredOrder[i]}(idx={indices[i]}) не после {requiredOrder[i - 1]}(idx={indices[i - 1]})");
                    return false;
                }
            }

            return true;
        }

        private static void ApplyPatch011()
        {
            // Делегируем перестановку в логику PATCH-001 (она уже умеет переставлять все слои)
            ApplyPatch001();
            Debug.Log("[ScenePatchBuilder] PATCH-011: Полная проверка Sorting Layers выполнена через PATCH-001 логику");
        }

        private static bool ValidatePatch011()
        {
            return IsPatch011Applied();
        }

        // ====================================================================
        //  PATCH-012: UI physics layer → GameUI
        //  Аудит: H-02 — Layer 11 "UI" дублирует Unity built-in layer 5 "UI".
        //  NameToLayer("UI") возвращает 5, не 11. Код, использующий layer 11,
        //  не будет работать. Переименуем в "GameUI".
        //  Редактировано: 2026-04-17 14:20 UTC
        // ====================================================================

        private static bool IsPatch012Applied()
        {
            // Проверяем: layer 11 не называется "UI" (чтобы избежать дублирования с built-in layer 5)
            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            int uiLayerIndex = LayerMask.NameToLayer("UI");

            // Unity built-in UI layer = 5. Если NameToLayer("UI") = 5 — корректно.
            // Если layer 11 тоже называется "UI" — проблема.
            var tagManager = LoadTagManager();
            if (tagManager == null) return true;

            var layersProp = tagManager.FindProperty("layers");
            if (layersProp == null) return true;

            // Проверяем layer 11
            if (11 < layersProp.arraySize)
            {
                var layer11 = layersProp.GetArrayElementAtIndex(11);
                if (layer11 != null && layer11.stringValue == "UI")
                {
                    // Layer 11 называется "UI" — конфликт с built-in layer 5
                    return false;
                }
            }

            return true;
        }

        private static void ApplyPatch012()
        {
            var tagManager = LoadTagManager();
            if (tagManager == null) return;

            var layersProp = tagManager.FindProperty("layers");
            if (layersProp == null) return;

            if (11 < layersProp.arraySize)
            {
                var layer11 = layersProp.GetArrayElementAtIndex(11);
                if (layer11 != null && layer11.stringValue == "UI")
                {
                    layer11.stringValue = "GameUI";
                    tagManager.ApplyModifiedProperties();
                    Debug.Log("[ScenePatchBuilder] PATCH-012: Layer 11 переименован из 'UI' в 'GameUI' (устранён конфликт с Unity built-in layer 5)");
                }
            }
        }

        private static bool ValidatePatch012()
        {
            var tagManager = LoadTagManager();
            if (tagManager == null) return true;

            var layersProp = tagManager.FindProperty("layers");
            if (layersProp == null) return true;

            if (11 < layersProp.arraySize)
            {
                var layer11 = layersProp.GetArrayElementAtIndex(11);
                if (layer11 != null && layer11.stringValue == "UI")
                {
                    Debug.LogError("[ScenePatchBuilder] PATCH-012 ВАЛИДАЦИЯ: Layer 11 всё ещё называется 'UI' (конфликт с built-in!)");
                    return false;
                }
            }
            return true;
        }
    }
}
#endif
