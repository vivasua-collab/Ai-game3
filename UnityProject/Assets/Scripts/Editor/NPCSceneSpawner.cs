// ============================================================================
// NPCSceneSpawner.cs — Генерация NPC в активной сцене (hotkey + menu)
// Cultivation World Simulator
// Создано: 2026-04-30 07:50:00 UTC
// ============================================================================
//
// Горячие клавиши:
//   Ctrl+N          — 1 случайный NPC рядом с Player
//   Ctrl+Shift+N    — 5 NPC разных ролей рядом с Player
//   Ctrl+F5         — 1 Merchant рядом с Player
//   Ctrl+F6         — 1 Monster/Enemy рядом с Player
//
// Пункты меню: Tools/NPC/Spawn In Scene/
//
// Аналог EquipmentSceneSpawner — создаёт GameObject напрямую,
// без .prefab файлов. Вызывает NPCController.InitializeFromGenerated().
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
// UnityEngine.SceneManagement — не используется в данном файле
using CultivationGame.Core;
using CultivationGame.Generators;
using CultivationGame.NPC;
using CultivationGame.Body;
using CultivationGame.Qi;

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

        private const float SPREAD_RADIUS = 3f;    // Разброс вокруг игрока
        private const float NPC_COLLIDER_RADIUS = 0.5f; // Физический коллайдер
        private const float INTERACTION_RADIUS = 1.5f;  // Радиус взаимодействия

        // ================================================================
        //  МЕНЮ: СПАВН В СЦЕНУ
        // ================================================================

        /// Спавн 1 случайного NPC рядом с Player [Ctrl+N]
        [MenuItem("Tools/NPC/Spawn In Scene/Random NPC _%n", false, 30)]
        public static void SpawnRandomNPC()
        {
            SpawnNPCNearPlayer(NPCRole.Passerby, 0);
        }

        /// Спавн 5 NPC разных ролей рядом с Player [Ctrl+Shift+N]
        [MenuItem("Tools/NPC/Spawn In Scene/5 Random NPCs _%#n", false, 31)]
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

        /// Спавн 1 Merchant рядом с Player [Ctrl+F5]
        [MenuItem("Tools/NPC/Spawn In Scene/Merchant _%F5", false, 32)]
        public static void SpawnMerchant()
        {
            SpawnNPCNearPlayer(NPCRole.Merchant, 2);
        }

        /// Спавн 1 Monster/Enemy рядом с Player [Ctrl+F6]
        [MenuItem("Tools/NPC/Spawn In Scene/Monster _%F6", false, 33)]
        public static void SpawnMonster()
        {
            SpawnNPCNearPlayer(NPCRole.Monster, 1);
        }

        /// Спавн 1 Guard рядом с Player
        [MenuItem("Tools/NPC/Spawn In Scene/Guard", false, 34)]
        public static void SpawnGuard()
        {
            SpawnNPCNearPlayer(NPCRole.Guard, 2);
        }

        /// Спавн 1 Elder рядом с Player
        [MenuItem("Tools/NPC/Spawn In Scene/Elder", false, 35)]
        public static void SpawnElder()
        {
            SpawnNPCNearPlayer(NPCRole.Elder, 5);
        }

        /// Спавн 1 Enemy рядом с Player
        [MenuItem("Tools/NPC/Spawn In Scene/Enemy", false, 36)]
        public static void SpawnEnemy()
        {
            SpawnNPCNearPlayer(NPCRole.Enemy, 1);
        }

        /// Спавн 1 Cultivator рядом с Player
        [MenuItem("Tools/NPC/Spawn In Scene/Cultivator", false, 37)]
        public static void SpawnCultivator()
        {
            SpawnNPCNearPlayer(NPCRole.Cultivator, 3);
        }

        /// Спавн 1 Disciple рядом с Player
        [MenuItem("Tools/NPC/Spawn In Scene/Disciple", false, 38)]
        public static void SpawnDisciple()
        {
            SpawnNPCNearPlayer(NPCRole.Disciple, 1);
        }

        /// Удалить все NPC из сцены
        [MenuItem("Tools/NPC/Clear All NPCs", false, 50)]
        public static void ClearAllNPCs()
        {
            var npcs = Object.FindObjectsByType<NPCController>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var npc in npcs)
            {
                Undo.DestroyObjectImmediate(npc.gameObject);
                count++;
            }
            Debug.Log($"[NPCSpawner] Удалено {count} NPC из сцены");
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
        /// </summary>
        public static NPCController SpawnNPCInScene(NPCRole role, int cultivationLevel, Vector3 position)
        {
            // 1. Генерируем данные NPC
            GeneratedNPC generated = GenerateNPCData(role, cultivationLevel);
            if (generated == null)
            {
                Debug.LogError("[NPCSpawner] Не удалось сгенерировать NPC!");
                return null;
            }

            // 2. Создаём GameObject
            var go = new GameObject($"NPC_{generated.nameRu}");
            position.z = 0;
            go.transform.position = position;

            // Слой NPC (7) — если существует, иначе Default
            int npcLayer = LayerMask.NameToLayer("NPC");
            go.layer = npcLayer >= 0 ? npcLayer : 0;

            // Тег NPC
            go.tag = "NPC";

            // 3. Добавляем компоненты
            var controller = go.AddComponent<NPCController>();
            var ai = go.AddComponent<NPCAI>();
            var body = go.AddComponent<BodyController>();
            var qi = go.AddComponent<QiController>();
            var technique = go.AddComponent<TechniqueController>();
            var visual = go.AddComponent<NPCVisual>();
            var interactable = go.AddComponent<NPCInteractable>();
            var movement = go.AddComponent<NPCMovement>();

            // 4. Физика: Rigidbody2D
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.linearDamping = 5f; // Трение для остановки

            // 5. Физический коллайдер (solid)
            var solidCollider = go.AddComponent<CircleCollider2D>();
            solidCollider.isTrigger = false;
            solidCollider.radius = NPC_COLLIDER_RADIUS;

            // 6. Настройка NPCAI через SerializedObject
            var soAI = new SerializedObject(ai);
            soAI.FindProperty("decisionInterval").floatValue = 1f;
            soAI.FindProperty("aggroRange").floatValue = 10f;
            soAI.FindProperty("fleeHealthThreshold").floatValue = 0.2f;
            soAI.FindProperty("attackRange").floatValue = 1.5f;
            soAI.FindProperty("attackCooldown").floatValue = 1.5f;
            soAI.ApplyModifiedProperties();

            // 6a. Настройка NPCMovement — домашняя позиция и скорость по роли
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

            // 7. Инициализируем NPCController из GeneratedNPC
            controller.InitializeFromGenerated(generated);

            // 8. Настраиваем NPCVisual — спрайт по роли
            visual.SetSpriteByRole(role);

            // 9. Обновляем визуал
            visual.UpdateVisualFromState();

            // 10. Настраиваем NPCInteractable — роль
            interactable.SetNPCRole(role);

            // 11. Устанавливаем начальный AI state
            SetInitialAIState(ai, role);

            // 12. Undo
            Undo.RegisterCreatedObjectUndo(go, "Spawn NPC");

            Debug.Log($"[NPCSpawner] Спавн: {generated.nameRu} ({role} L{cultivationLevel}) " +
                      $"поз.{position} Att={generated.baseAttitude}");

            return controller;
        }

        /// <summary>
        /// Генерация данных NPC через NPCGenerator или GeneratorRegistry.
        /// </summary>
        private static GeneratedNPC GenerateNPCData(NPCRole role, int cultivationLevel)
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

            // Устанавливаем через SerializedObject, т.к. NPCAI.state приватное
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
