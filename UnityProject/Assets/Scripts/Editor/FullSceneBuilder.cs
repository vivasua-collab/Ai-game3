// ============================================================================
// FullSceneBuilder.cs — Оркестратор сборки сцены (v2.0)
// Cultivation World Simulator
// ============================================================================
// Версия: 2.0 — Рефакторинг: монолит → оркестратор + отдельные файлы фаз
//
// АРХИТЕКТУРА:
//   20 фаз, каждая в отдельном файле (Assets/Scripts/Editor/SceneBuilder/).
//   Каждая фаза реализует IScenePhase — IsNeeded() + Execute().
//   Оркестратор регистрирует фазы и управляет их запуском.
//
//   Все 12 патчей из ScenePatchBuilder.cs объединены в соответствующие фазы:
//     PATCH-001, PATCH-011 → Phase02TagsLayers
//     PATCH-002, PATCH-005, PATCH-006, PATCH-007 → Phase08Tilemap
//     PATCH-003 → Phase06Player
//     PATCH-004, PATCH-009, PATCH-010 → Phase04CameraLight
//     PATCH-008 → SceneBuilderUtils.CleanMissingPrefabs
//     PATCH-012 → Phase02TagsLayers
//
// ФАЙЛЫ:
//   SceneBuilder/IScenePhase.cs           — Интерфейс фазы
//   SceneBuilder/SceneBuilderConstants.cs  — Общие константы
//   SceneBuilder/SceneBuilderUtils.cs      — Общие утилиты
//   SceneBuilder/Phase00URPSetup.cs           — Фаза 00 (NEW)
//   SceneBuilder/Phase01Folders.cs         — Фаза 01
//   SceneBuilder/Phase02TagsLayers.cs      — Фаза 02
//   ... и т.д. для Phase03-Phase15
//   SceneBuilder/Phase16InventoryData.cs    — Фаза 16
//   SceneBuilder/Phase17InventoryUI.cs       — Фаза 17
//   SceneBuilder/Phase18InventoryComponents.cs — Фаза 18
//   SceneBuilder/Phase19NPCPlacement.cs        — Фаза 19 (NEW)
//   FullSceneBuilder.cs                    — Оркестратор (этот файл)
//
// СОВМЕСТИМОСТЬ: Unity 6.3+ (6000.3)
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Editor.SceneBuilder;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Оркестратор сборки сцены.
    /// Регистрирует все фазы и управляет их запуском.
    /// Каждая фаза находится в отдельном файле.
    /// </summary>
    public static class FullSceneBuilder
    {
        #region Phase Registry

        private static readonly IScenePhase[] PHASES = new IScenePhase[]
        {
            new Phase00URPSetup(),
            new Phase01Folders(),
            new Phase02TagsLayers(),
            new Phase03SceneCreation(),
            new Phase04CameraLight(),
            new Phase05GameManager(),
            new Phase06Player(),
            new Phase07UI(),
            new Phase08Tilemap(),
            new Phase09GenerateAssets(),
            new Phase10GenerateSprites(),
            new Phase11GenerateUIPrefabs(),
            new Phase12TMPEssentials(),
            new Phase13SaveScene(),
            new Phase14CreateTileAssets(),
            new Phase15ConfigureTestLocation(),
            new Phase16InventoryData(),
            new Phase17InventoryUI(),
            new Phase18InventoryComponents(),
            new Phase19NPCPlacement(),
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
                string phaseLabel = $"[{i + 1}/{PHASES.Length}] {phase.Name}";

                try
                {
                    if (phase.IsNeeded())
                    {
                        Debug.Log($"{phaseLabel}: Executing...");
                        phase.Execute();
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

                    if (!EditorUtility.DisplayDialog("Phase Failed",
                        $"Фаза «{phase.Name}» упала:\n{ex.Message}\n\nПродолжить?", "Да", "Стоп"))
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

        [MenuItem("Tools/Full Scene Builder/Phase 00: URP Asset Setup", false, 100)]
        public static void RunPhase00() { RunSinglePhase(0); }

        [MenuItem("Tools/Full Scene Builder/Phase 01: Folders", false, 101)]
        public static void RunPhase01() { RunSinglePhase(1); }

        [MenuItem("Tools/Full Scene Builder/Phase 02: Tags and Layers", false, 102)]
        public static void RunPhase02() { RunSinglePhase(2); }

        [MenuItem("Tools/Full Scene Builder/Phase 03: Create Scene", false, 103)]
        public static void RunPhase03() { RunSinglePhase(3); }

        [MenuItem("Tools/Full Scene Builder/Phase 04: Camera and Light", false, 104)]
        public static void RunPhase04() { RunSinglePhase(4); }

        [MenuItem("Tools/Full Scene Builder/Phase 05: GameManager and Systems", false, 105)]
        public static void RunPhase05() { RunSinglePhase(5); }

        [MenuItem("Tools/Full Scene Builder/Phase 06: Player", false, 106)]
        public static void RunPhase06() { RunSinglePhase(6); }

        [MenuItem("Tools/Full Scene Builder/Phase 07: UI (Canvas, HUD, EventSystem)", false, 107)]
        public static void RunPhase07() { RunSinglePhase(7); }

        [MenuItem("Tools/Full Scene Builder/Phase 08: Tilemap System", false, 108)]
        public static void RunPhase08() { RunSinglePhase(8); }

        [MenuItem("Tools/Full Scene Builder/Phase 09: Generate Assets from JSON", false, 109)]
        public static void RunPhase09() { RunSinglePhase(9); }

        [MenuItem("Tools/Full Scene Builder/Phase 10: Generate Tile Sprites", false, 110)]
        public static void RunPhase10() { RunSinglePhase(10); }

        [MenuItem("Tools/Full Scene Builder/Phase 11: Generate Formation UI Prefabs", false, 111)]
        public static void RunPhase11() { RunSinglePhase(11); }

        [MenuItem("Tools/Full Scene Builder/Phase 12: Import TMP Essentials", false, 112)]
        public static void RunPhase12() { RunSinglePhase(12); }

        [MenuItem("Tools/Full Scene Builder/Phase 13: Save Scene", false, 113)]
        public static void RunPhase13() { RunSinglePhase(13); }

        [MenuItem("Tools/Full Scene Builder/Phase 14: Create Tile Assets", false, 114)]
        public static void RunPhase14() { RunSinglePhase(14); }

        [MenuItem("Tools/Full Scene Builder/Phase 15: Configure Test Location", false, 115)]
        public static void RunPhase15() { RunSinglePhase(15); }

        [MenuItem("Tools/Full Scene Builder/Phase 16: Inventory Data", false, 116)]
        public static void RunPhase16() { RunSinglePhase(16); }

        [MenuItem("Tools/Full Scene Builder/Phase 17: Inventory UI", false, 117)]
        public static void RunPhase17() { RunSinglePhase(17); }

        [MenuItem("Tools/Full Scene Builder/Phase 18: Inventory Components", false, 118)]
        public static void RunPhase18() { RunSinglePhase(18); }

        [MenuItem("Tools/Full Scene Builder/Phase 19: NPC Placement", false, 119)]
        public static void RunPhase19() { RunSinglePhase(19); }

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

            if (phase.IsNeeded())
            {
                try
                {
                    Debug.Log($"[FullSceneBuilder] Running phase: {phase.Name}");
                    phase.Execute();
                    Debug.Log($"[FullSceneBuilder] ✅ Phase '{phase.Name}' complete");
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[FullSceneBuilder] ❌ Phase '{phase.Name}' failed: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"[FullSceneBuilder] ⏭ Phase '{phase.Name}' skipped (already done)");
            }
        }
    }
}
#endif
