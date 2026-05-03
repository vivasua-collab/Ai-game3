// ============================================================================
// NPCRuntimeSpawner.cs — Runtime спавн NPC (без Editor API)
// Cultivation World Simulator
// Создано: 2026-05-04 12:00:00 UTC
// ============================================================================
//
// Runtime-версия NPCSceneSpawner — без SerializedObject, Undo, MenuItem.
// Используется DebugMenuController и может вызываться в Play Mode.
//
// Ссылки на компоненты обновляются через Start() (NPCAI.Start уже
// обновляет npcVisual/npcMovement). NPCController.InitializeControllers()
// в Awake() делает GetComponent для подсистем.
//
// Ключевое отличие от Editor-версии:
// - Нет SerializedObject (RefreshControllerReferences/ConfigureAIViaSerializedObject)
// - Нет Undo.RegisterCreatedObjectUndo
// - Нет [MenuItem]
// - Ссылки обновляются через повторный вызов InitializeControllers() + прямое назначение
// ============================================================================

using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Generators;
using CultivationGame.NPC;
using CultivationGame.Body;
using CultivationGame.Qi;
using CultivationGame.Combat;

namespace CultivationGame.NPC
{
    /// <summary>
    /// Runtime спавнер NPC — создаёт GameObject + компоненты + инициализация.
    /// Работает в Play Mode без Editor API.
    /// </summary>
    public static class NPCRuntimeSpawner
    {
        // ================================================================
        //  КОНСТАНТЫ
        // ================================================================

        private const float SPREAD_RADIUS = 3f;
        private const float NPC_COLLIDER_RADIUS = 0.5f;

        // ================================================================
        //  ПУБЛИЧНЫЕ МЕТОДЫ
        // ================================================================

        /// <summary>
        /// Спавн NPC указанной роли рядом с Player.
        /// </summary>
        public static NPCController SpawnNearPlayer(NPCRole role, int cultivationLevel)
        {
            Vector3 center = GetPlayerPosition();
            Vector3 offset = Random.insideUnitSphere * SPREAD_RADIUS;
            offset.z = 0;
            return SpawnNPC(role, cultivationLevel, center + offset);
        }

        /// <summary>
        /// Основной метод спавна NPC.
        /// Создаёт GameObject + компоненты + инициализация.
        /// </summary>
        public static NPCController SpawnNPC(NPCRole role, int cultivationLevel, Vector3 position)
        {
            // ── 1. Генерируем данные NPC ──────────────────────────────────
            GeneratedNPC generated = GenerateNPCData(role, cultivationLevel);
            if (generated == null)
            {
                Debug.LogError("[NPCRuntimeSpawner] Не удалось сгенерировать NPC!");
                return null;
            }

            // ── 2. Создаём GameObject ─────────────────────────────────────
            var go = new GameObject($"NPC_{generated.nameRu}");
            position.z = 0;
            go.transform.position = position;

            // Слой NPC — если существует, иначе Default
            int npcLayer = LayerMask.NameToLayer("NPC");
            go.layer = npcLayer >= 0 ? npcLayer : 0;

            // Тег NPC — безопасное назначение
            try { go.tag = "NPC"; }
            catch (UnityException) { Debug.LogWarning("[NPCRuntimeSpawner] Тег 'NPC' не определён — Untagged"); }

            // ── 3. Добавляем компоненты (порядок важен!) ──────────────────
            var controller = go.AddComponent<NPCController>();
            var ai = go.AddComponent<NPCAI>();
            var body = go.AddComponent<BodyController>();
            var qi = go.AddComponent<QiController>();
            var technique = go.AddComponent<TechniqueController>();
            var visual = go.AddComponent<NPCVisual>();
            var interactable = go.AddComponent<NPCInteractable>();
            var movement = go.AddComponent<NPCMovement>();

            // ── 4. Обновляем ссылки контроллера ───────────────────────────
            // NPCController.Awake() уже вызван, но другие компоненты были добавлены после.
            // Вызываем InitializeControllers() повторно через reflection (private method).
            // Альтернатива: прямое назначение через SerializedField недоступно без Editor,
            // но InitializeControllers() использует GetComponent — этого достаточно.
            RefreshControllerReferencesRuntime(controller);

            // ── 5. Физика: Rigidbody2D ────────────────────────────────────
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogWarning("[NPCRuntimeSpawner] Rigidbody2D не найден — добавляем вручную");
                rb = go.AddComponent<Rigidbody2D>();
            }
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.linearDamping = 5f;

            // ── 6. Физический коллайдер (solid) ───────────────────────────
            var solidCollider = go.AddComponent<CircleCollider2D>();
            solidCollider.isTrigger = false;
            solidCollider.radius = NPC_COLLIDER_RADIUS;

            // ── 7. Настройка NPCMovement ──────────────────────────────────
            if (movement != null)
            {
                movement.SetHomePosition(position);
                float roleSpeed = role switch
                {
                    NPCRole.Monster => 4f,
                    NPCRole.Enemy => 3.5f,
                    NPCRole.Guard => 2.5f,
                    NPCRole.Cultivator => 2f,
                    NPCRole.Elder => 1.5f,
                    NPCRole.Merchant => 1.5f,
                    _ => 2.5f
                };
                movement.SetBaseSpeed(roleSpeed);
                movement.SetWanderRadius(role switch
                {
                    NPCRole.Monster => 8f,
                    NPCRole.Guard => 6f,
                    NPCRole.Merchant => 3f,
                    NPCRole.Elder => 2f,
                    _ => 5f
                });
            }

            // ── 8. Инициализируем NPCController из GeneratedNPC ───────────
            controller.InitializeFromGenerated(generated);

            // Сохраняем роль в NPCState
            if (controller.State != null)
                controller.State.Role = role;

            // ── 9. Настраиваем NPCVisual ──────────────────────────────────
            if (visual != null)
            {
                visual.SetSpriteByRole(role);
                visual.UpdateVisualFromState();
            }

            // ── 10. Настраиваем NPCInteractable ───────────────────────────
            if (interactable != null)
            {
                interactable.SetNPCRole(role);
            }

            // ── 11. Устанавливаем начальный AI state ──────────────────────
            SetInitialAIState(ai, role, controller);

            Debug.Log($"[NPCRuntimeSpawner] Спавн: {generated.nameRu} ({role} L{cultivationLevel}) поз.{position}");

            return controller;
        }

        /// <summary>
        /// Спавн полного набора NPC вокруг центра.
        /// </summary>
        public static int SpawnFullSet(Vector3 center)
        {
            var placements = new (NPCRole role, int level, float ox, float oy)[]
            {
                (NPCRole.Merchant,   2,   3f,  -2f),
                (NPCRole.Guard,      3,   8f,   0f),
                (NPCRole.Guard,      2,  -5f,   3f),
                (NPCRole.Elder,      5,  -3f,  -4f),
                (NPCRole.Cultivator, 4,   5f,   5f),
                (NPCRole.Monster,    1, -10f,  -8f),
                (NPCRole.Monster,    2,  12f, -10f),
            };

            int spawned = 0;
            foreach (var (role, level, ox, oy) in placements)
            {
                var ctrl = SpawnNPC(role, level, new Vector3(center.x + ox, center.y + oy, 0f));
                if (ctrl != null) spawned++;
            }
            return spawned;
        }

        /// <summary>
        /// Удалить все NPC из сцены. Возвращает количество удалённых.
        /// </summary>
        public static int ClearAllNPCs()
        {
            var npcs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var npc in npcs)
            {
                Object.Destroy(npc.gameObject);
                count++;
            }
            Debug.Log($"[NPCRuntimeSpawner] Удалено {count} NPC из сцены");
            return count;
        }

        // ================================================================
        //  HELPERS
        // ================================================================

        /// <summary>
        /// Обновить ссылки NPCController на подсистемы.
        /// В runtime используем повторный вызов InitializeControllers (через reflection),
        /// т.к. Awake() вызывается ДО добавления зависимых компонентов.
        /// </summary>
        private static void RefreshControllerReferencesRuntime(NPCController controller)
        {
            // InitializeControllers — private метод, используем reflection
            var method = typeof(NPCController).GetMethod(
                "InitializeControllers",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(controller, null);
            }
            else
            {
                Debug.LogWarning("[NPCRuntimeSpawner] InitializeControllers не найден — ссылки могут быть null");
            }
        }

        /// <summary>
        /// Генерация данных NPC через NPCGenerator или GeneratorRegistry.
        /// </summary>
        private static GeneratedNPC GenerateNPCData(NPCRole role, int cultivationLevel)
        {
            try
            {
                var registry = GeneratorRegistry.Instance;
                if (registry != null && registry.IsInitialized)
                {
                    return registry.GenerateNPCByRole(role, cultivationLevel);
                }

                var rng = new SeededRandom();
                var parameters = new NPCGenerationParams
                {
                    role = role,
                    cultivationLevel = cultivationLevel,
                    seed = rng.Next()
                };

                return NPCGenerator.Generate(parameters, rng);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[NPCRuntimeSpawner] Ошибка генерации NPC {role} L{cultivationLevel}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Установить начальный AI state по роли NPC.
        /// </summary>
        private static void SetInitialAIState(NPCAI ai, NPCRole role, NPCController controller)
        {
            var initialState = role switch
            {
                NPCRole.Merchant => NPCAIState.Trading,
                NPCRole.Guard => NPCAIState.Patrolling,
                NPCRole.Elder => NPCAIState.Idle,
                NPCRole.Cultivator => NPCAIState.Cultivating,
                NPCRole.Disciple => NPCAIState.Cultivating,
                NPCRole.Monster => NPCAIState.Wandering,
                NPCRole.Enemy => NPCAIState.Wandering,
                NPCRole.Passerby => NPCAIState.Idle,
                _ => NPCAIState.Idle
            };

            if (controller != null)
            {
                controller.SetAIState(initialState);
            }
        }

        /// <summary>
        /// Получить позицию Player в сцене.
        /// </summary>
        private static Vector3 GetPlayerPosition()
        {
            var player = GameObject.Find("Player");
            if (player != null)
                return player.transform.position;

            Debug.LogWarning("[NPCRuntimeSpawner] Player не найден! Спавн в (0,0,0)");
            return Vector3.zero;
        }
    }
}
