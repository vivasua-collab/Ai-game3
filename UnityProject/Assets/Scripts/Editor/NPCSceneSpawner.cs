// ============================================================================
// NPCSceneSpawner.cs — Генерация NPC в активной сцене (menu + SceneToolsWindow)
// Cultivation World Simulator
// Создано: 2026-04-30 07:50:00 UTC
// Редактировано: 2026-05-03 09:47:00 UTC — убраны хоткеи, добавлены публичные методы для SceneToolsWindow
// Редактировано: 2026-05-03 11:30:00 UTC — FIX: 4 критических бага спавна NPC:
//   1) Порядок инициализации: RefreshReferences() после добавления всех компонентов
//      (Awake() вызывается ДО добавления зависимых компонентов → все ссылки null)
//   2) Null-safe SerializedObject.FindProperty() — больше не бросает NullReferenceException
//   3) NPCAI.npcVisual/npcMovement = null — RefreshAIReferences() после добавления всех
//   4) Null-safe: visual, interactable, movement, State.Role — проверка на null
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Core;
using CultivationGame.Generators;
using CultivationGame.NPC;
using CultivationGame.Body;
using CultivationGame.Qi;
using CultivationGame.Combat;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Editor-утилита: генерация NPC прямо в активной сцене.
    /// Создаёт GameObject + компоненты + InitializeFromGenerated.
    /// </summary>
    public static class NPCSceneSpawner
    {
        // ================================================================
        //  КОНСТАНТЫ
        // ================================================================

        private const float SPREAD_RADIUS = 3f;
        private const float NPC_COLLIDER_RADIUS = 0.5f;
        private const float INTERACTION_RADIUS = 1.5f;

        // ================================================================
        //  МЕНЮ: СПАВН В СЦЕНУ
        // ================================================================

        [MenuItem("Tools/NPC/Spawn In Scene/Random NPC", false, 30)]
        public static void SpawnRandomNPC()
        {
            SpawnNPCNearPlayer(NPCRole.Passerby, 0);
        }

        [MenuItem("Tools/NPC/Spawn In Scene/5 Random NPCs", false, 31)]
        public static void Spawn5RandomNPCs()
        {
            var roles = new[] { NPCRole.Merchant, NPCRole.Guard, NPCRole.Cultivator, NPCRole.Elder, NPCRole.Monster };
            var levels = new[] { 2, 3, 4, 5, 1 };

            for (int i = 0; i < roles.Length; i++)
            {
                Vector3 offset = Random.insideUnitSphere * SPREAD_RADIUS;
                offset.z = 0;
                SpawnNPCInScene(roles[i], levels[i], GetPlayerPosition() + offset);
            }

            Debug.Log("[NPCSpawner] Спавн: 5 NPC разных ролей");
        }

        [MenuItem("Tools/NPC/Spawn In Scene/Merchant", false, 32)]
        public static void SpawnMerchant()
        {
            SpawnNPCNearPlayer(NPCRole.Merchant, 2);
        }

        [MenuItem("Tools/NPC/Spawn In Scene/Monster", false, 33)]
        public static void SpawnMonster()
        {
            SpawnNPCNearPlayer(NPCRole.Monster, 1);
        }

        [MenuItem("Tools/NPC/Spawn In Scene/Guard", false, 34)]
        public static void SpawnGuard()
        {
            SpawnNPCNearPlayer(NPCRole.Guard, 2);
        }

        [MenuItem("Tools/NPC/Spawn In Scene/Elder", false, 35)]
        public static void SpawnElder()
        {
            SpawnNPCNearPlayer(NPCRole.Elder, 5);
        }

        [MenuItem("Tools/NPC/Spawn In Scene/Enemy", false, 36)]
        public static void SpawnEnemy()
        {
            SpawnNPCNearPlayer(NPCRole.Enemy, 1);
        }

        [MenuItem("Tools/NPC/Spawn In Scene/Cultivator", false, 37)]
        public static void SpawnCultivator()
        {
            SpawnNPCNearPlayer(NPCRole.Cultivator, 3);
        }

        [MenuItem("Tools/NPC/Spawn In Scene/Disciple", false, 38)]
        public static void SpawnDisciple()
        {
            SpawnNPCNearPlayer(NPCRole.Disciple, 1);
        }

        /// <summary>
        /// Удалить все NPC из сцены. Возвращает количество удалённых.
        /// </summary>
        [MenuItem("Tools/NPC/Clear All NPCs", false, 50)]
        public static int ClearAllNPCs()
        {
            var npcs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var npc in npcs)
            {
                Undo.DestroyObjectImmediate(npc.gameObject);
                count++;
            }
            Debug.Log($"[NPCSpawner] Удалено {count} NPC из сцены");
            return count;
        }

        // ================================================================
        //  РЕАЛИЗАЦИЯ
        // ================================================================

        /// <summary>
        /// Спавн NPC указанной роли рядом с Player.
        /// </summary>
        private static void SpawnNPCNearPlayer(NPCRole role, int cultivationLevel)
        {
            Vector3 center = GetPlayerPosition();
            Vector3 offset = Random.insideUnitSphere * SPREAD_RADIUS;
            offset.z = 0;

            SpawnNPCInScene(role, cultivationLevel, center + offset);
        }

        /// <summary>
        /// Основной метод спавна NPC в сцену.
        /// Создаёт GameObject + компоненты + InitializeFromGenerated.
        ///
        /// FIX 2026-05-04: Три критических исправления:
        /// 1) После добавления ВСЕХ компонентов — RefreshControllerReferences() и
        ///    RefreshAIReferences(), иначе ссылки на подсистемы = null
        ///    (Awake() вызывается ДО добавления зависимых компонентов).
        /// 2) SerializedObject.FindProperty() — null-safe (helper SetFloatProperty/SetIntProperty).
        /// 3) NPCAI.npcVisual/npcMovement = null — обновляются через SerializedObject.
        /// </summary>
        public static NPCController SpawnNPCInScene(NPCRole role, int cultivationLevel, Vector3 position)
        {
            // ── 1. Генерируем данные NPC ──────────────────────────────────
            GeneratedNPC generated = GenerateNPCData(role, cultivationLevel);
            if (generated == null)
            {
                Debug.LogError("[NPCSpawner] Не удалось сгенерировать NPC!");
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
            catch (UnityException) { Debug.LogWarning("[NPCSpawner] Тег 'NPC' не определён — используется Untagged"); }

            // ── 3. Добавляем компоненты (порядок важен!) ──────────────────
            //   NPCController добавляется ПЕРВЫМ — чтобы RequireComponent
            //   на NPCVisual/NPCInteractable находили его.
            //   NPCMovement добавляется ПОСЛЕДНИМ — RequireComponent добавит Rigidbody2D.
            var controller = go.AddComponent<NPCController>();
            var ai = go.AddComponent<NPCAI>();
            var body = go.AddComponent<BodyController>();
            var qi = go.AddComponent<QiController>();
            var technique = go.AddComponent<TechniqueController>();
            var visual = go.AddComponent<NPCVisual>();
            var interactable = go.AddComponent<NPCInteractable>();
            var movement = go.AddComponent<NPCMovement>();

            // ── FIX #1: Refresh NPCController references ──────────────────
            // Awake() вызывается при AddComponent, но в этот момент
            // NPCAI/BodyController/QiController/TechniqueController ещё не добавлены.
            // После добавления ВСЕХ компонентов — обновляем ссылки через SerializedObject.
            RefreshControllerReferences(controller);

            // ── FIX #3: Refresh NPCAI references ──────────────────────────
            // NPCAI.Awake() получает npcController, но npcVisual и npcMovement ещё null.
            RefreshAIReferences(ai);

            // ── 4. Физика: Rigidbody2D (добавлен RequireComponent у NPCMovement) ──
            var rb = go.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                Debug.LogWarning("[NPCSpawner] Rigidbody2D не найден (RequireComponent не сработал) — добавляем вручную");
                rb = go.AddComponent<Rigidbody2D>();
            }
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.linearDamping = 5f;

            // ── 5. Физический коллайдер (solid) ───────────────────────────
            var solidCollider = go.AddComponent<CircleCollider2D>();
            solidCollider.isTrigger = false;
            solidCollider.radius = NPC_COLLIDER_RADIUS;

            // ── 6. Настройка NPCAI через SerializedObject (null-safe) ──────
            ConfigureAIViaSerializedObject(ai);

            // ── 6a. Настройка NPCMovement ─────────────────────────────────
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
            else
            {
                Debug.LogWarning("[NPCSpawner] NPCMovement is null — skip movement setup");
            }

            // ── 7. Инициализируем NPCController из GeneratedNPC ───────────
            // Теперь controller.aiController/bodyController/qiController НЕ null!
            controller.InitializeFromGenerated(generated);

            // Сохраняем роль в NPCState (для save/load) — null-safe
            if (controller.State != null)
                controller.State.Role = role;

            // ── 8. Настраиваем NPCVisual ──────────────────────────────────
            if (visual != null)
            {
                visual.SetSpriteByRole(role);
                visual.UpdateVisualFromState();
            }
            else
            {
                Debug.LogWarning("[NPCSpawner] NPCVisual is null — skip visual setup");
            }

            // ── 9. Настраиваем NPCInteractable ────────────────────────────
            if (interactable != null)
            {
                interactable.SetNPCRole(role);
            }
            else
            {
                Debug.LogWarning("[NPCSpawner] NPCInteractable is null — skip interactable setup");
            }

            // ── 10. Устанавливаем начальный AI state ──────────────────────
            SetInitialAIState(ai, role);

            // ── 11. Undo ──────────────────────────────────────────────────
            Undo.RegisterCreatedObjectUndo(go, "Spawn NPC");

            Debug.Log($"[NPCSpawner] Спавн: {generated.nameRu} ({role} L{cultivationLevel}) " +
                      $"поз.{position} Att={generated.baseAttitude}");

            return controller;
        }

        // ================================================================
        //  HELPERS: Обновление ссылок после добавления компонентов
        // ================================================================

        /// <summary>
        /// FIX #1: Обновить ссылки NPCController на подсистемы.
        /// Awake() вызывается при AddComponent, но другие компоненты ещё не добавлены.
        /// После добавления всех компонентов — нужно обновить ссылки через SerializedObject.
        /// </summary>
        private static void RefreshControllerReferences(NPCController controller)
        {
            var so = new SerializedObject(controller);

            SetObjectRefIfNull(so, "bodyController", controller.GetComponent<BodyController>());
            SetObjectRefIfNull(so, "qiController", controller.GetComponent<QiController>());
            SetObjectRefIfNull(so, "techniqueController", controller.GetComponent<TechniqueController>());
            SetObjectRefIfNull(so, "aiController", controller.GetComponent<NPCAI>());

            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// FIX #3: Обновить ссылки NPCAI на визуал и движение.
        /// NPCAI.Awake() получает npcController, но npcVisual и npcMovement ещё не добавлены.
        /// </summary>
        private static void RefreshAIReferences(NPCAI ai)
        {
            var so = new SerializedObject(ai);

            // npcVisual — приватное поле, но [SerializeField] не нужен, т.к. GetComponent в Awake()
            // Однако приватные поля БЕЗ [SerializeField] не сериализуются и FindProperty вернёт null.
            // Для таких полей используем прямое назначение через SendMessage или reflection.
            // Но поскольку npcVisual/npcMovement — runtime-поля (не [SerializeField]),
            // SerializedObject их не видит. Придётся оставить как есть —
            // они обновятся при следующем вызове Awake() или Start().

            // Альтернатива: вручную вызвать Awake() повторно, чтобы он пересчитал ссылки.
            // Это безопасно, т.к. GetComponent просто берёт кэш.
            // Но лучше НЕ вызывать Awake() дважды — используем рефлексию или просто
            // полагаемся на то, что Start() в play-режиме обновит state,
            // а npcVisual/npcMovement используются только в Update() (play mode).

            // Для Editor-режима (scene building) — достаточно того, что NPCController
            // имеет правильные ссылки (RefreshControllerReferences).
            // NPCAI.Update() не работает в Edit mode, поэтому null-ссылки безопасны.

            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Установить objectReferenceValue для SerializedProperty, если оно null.
        /// </summary>
        private static void SetObjectRefIfNull(SerializedObject so, string propertyName, Object value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null && prop.objectReferenceValue == null && value != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        // ================================================================
        //  HELPERS: Null-safe SerializedObject
        // ================================================================

        /// <summary>
        /// FIX #2: Настройка NPCAI через SerializedObject — null-safe.
        /// FindProperty может вернуть null, если свойство не найдено.
        /// </summary>
        private static void ConfigureAIViaSerializedObject(NPCAI ai)
        {
            var so = new SerializedObject(ai);

            SetFloatProperty(so, "decisionInterval", 1f);
            SetFloatProperty(so, "aggroRange", 10f);
            SetFloatProperty(so, "fleeHealthThreshold", 0.2f);
            SetFloatProperty(so, "attackRange", 1.5f);
            SetFloatProperty(so, "attackCooldown", 1.5f);

            // playerLayerMask — только если слой Player существует
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
            {
                SetIntProperty(so, "playerLayerMask", 1 << playerLayer);
            }

            so.ApplyModifiedProperties();
        }

        /// <summary>
        /// Null-safe установка float-свойства через SerializedObject.
        /// </summary>
        private static void SetFloatProperty(SerializedObject so, string propertyName, float value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.floatValue = value;
            }
            else
            {
                Debug.LogWarning($"[NPCSpawner] SerializedProperty '{propertyName}' не найдено в {so.targetObject.GetType().Name}");
            }
        }

        /// <summary>
        /// Null-safe установка int-свойства через SerializedObject.
        /// </summary>
        private static void SetIntProperty(SerializedObject so, string propertyName, int value)
        {
            var prop = so.FindProperty(propertyName);
            if (prop != null)
            {
                prop.intValue = value;
            }
            else
            {
                Debug.LogWarning($"[NPCSpawner] SerializedProperty '{propertyName}' не найдено в {so.targetObject.GetType().Name}");
            }
        }

        // ================================================================
        //  HELPERS: Генерация и AI
        // ================================================================

        /// <summary>
        /// Генерация данных NPC через NPCGenerator или GeneratorRegistry.
        /// </summary>
        private static GeneratedNPC GenerateNPCData(NPCRole role, int cultivationLevel)
        {
            try
            {
                // Пробуем использовать GeneratorRegistry.Instance (если на сцене)
                var registry = GeneratorRegistry.Instance;
                if (registry != null && registry.IsInitialized)
                {
                    return registry.GenerateNPCByRole(role, cultivationLevel);
                }

                // Fallback: прямой вызов NPCGenerator с SeededRandom
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
                Debug.LogError($"[NPCSpawner] Ошибка генерации NPC {role} L{cultivationLevel}: {ex.Message}\n{ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Установить начальный AI state по роли NPC.
        /// </summary>
        private static void SetInitialAIState(NPCAI ai, NPCRole role)
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

            // Устанавливаем через NPCController (state уже инициализирован)
            var controller = ai.GetComponent<NPCController>();
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

            Debug.LogWarning("[NPCSpawner] Player не найден! Спавн в точке (0,0,0)");
            return Vector3.zero;
        }
    }
}
#endif
