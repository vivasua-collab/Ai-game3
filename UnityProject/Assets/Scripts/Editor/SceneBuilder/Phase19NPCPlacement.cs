// ============================================================================
// Phase19NPCPlacement.cs — Фаза 19: Размещение NPC на тестовой поляне
// Cultivation World Simulator
// Создано: 2026-04-30 07:58:00 UTC
// ============================================================================
//
// IScenePhase: размещение NPC на тестовой поляне.
// 1 Merchant, 2 Guard, 1 Elder, 1 Cultivator, 2 Monster.
// Вызывает NPCSceneSpawner.SpawnNPCInScene() для каждого NPC.
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Generators;
using CultivationGame.NPC;

namespace CultivationGame.Editor.SceneBuilder
{
    /// <summary>
    /// Фаза 19: Размещение NPC на тестовой поляне.
    /// </summary>
    public class Phase19NPCPlacement : IScenePhase
    {
        // === IScenePhase ===

        public string Name => "NPC Placement";
        public string MenuPath => "Phase 19: NPC Placement";
        public int Order => 19;

        // === Конфигурация спавна ===
        // Центр карты = позиция Player (100, 80). Смещения относительно центра.

        private const float CENTER_X = 100f;
        private const float CENTER_Y = 80f;

        private static readonly (NPCRole role, int level, float offsetX, float offsetY)[] NPC_PLACEMENTS =
        {
            (NPCRole.Merchant,   2,   3f,  -2f),  // Центр деревни
            (NPCRole.Guard,      3,   8f,   0f),  // Вход
            (NPCRole.Guard,      2,  -5f,   3f),  // Патруль
            (NPCRole.Elder,      5,  -3f,  -4f),  // Дом старейшины
            (NPCRole.Cultivator, 4,   5f,   5f),  // Площадка культивации
            (NPCRole.Monster,    1, -10f,  -8f),  // Окраина
            (NPCRole.Monster,    2,  12f, -10f),  // Окраина
        };

        /// <summary>
        /// Проверяет, нужно ли выполнение фазы.
        /// Если на сцене НЕТ ни одного объекта с тегом "NPC" → return true.
        /// </summary>
        public bool IsNeeded()
        {
            // Проверяем, есть ли уже NPC в сцене
            var existingNPCs = GameObject.FindGameObjectsWithTag("NPC");
            return existingNPCs == null || existingNPCs.Length == 0;
        }

        /// <summary>
        /// Выполняет фазу: спавнит NPC на тестовой поляне.
        /// </summary>
        public void Execute()
        {
            Debug.Log("[Phase19] Начало размещения NPC на тестовой поляне");

            int spawned = 0;

            foreach (var (role, level, offsetX, offsetY) in NPC_PLACEMENTS)
            {
                Vector3 position = new Vector3(CENTER_X + offsetX, CENTER_Y + offsetY, 0f);

                var controller = NPCSceneSpawner.SpawnNPCInScene(role, level, position);
                if (controller != null)
                {
                    spawned++;

                    // Дополнительная настройка для Guard: точки патруля
                    if (role == NPCRole.Guard)
                    {
                        SetupGuardPatrol(controller, position);
                    }

                    Debug.Log($"[Phase19] Спавн: {controller.NpcName} ({role} L{level}) поз.({position.x},{position.y})");
                }
                else
                {
                    Debug.LogWarning($"[Phase19] Не удалось создать NPC {role} L{level}");
                }
            }

            Debug.Log($"[Phase19] ✅ Размещение завершено: {spawned}/{NPC_PLACEMENTS.Length} NPC");
        }

        /// <summary>
        /// Настроить точки патруля для Guard.
        /// Guard патрулирует между своей точкой и соседней.
        /// </summary>
        private void SetupGuardPatrol(NPCController controller, Vector3 basePosition)
        {
            var ai = controller.GetComponent<NPCAI>();
            if (ai == null) return;

            // Точки патруля: basePosition ± (3, 0) и (0, 3)
            Vector3[] patrolPoints = new Vector3[]
            {
                basePosition,
                basePosition + new Vector3(3f, 0f, 0f),
                basePosition + new Vector3(3f, 3f, 0f),
                basePosition + new Vector3(0f, 3f, 0f)
            };

            ai.SetPatrolPoints(patrolPoints);
        }
    }
}
#endif
