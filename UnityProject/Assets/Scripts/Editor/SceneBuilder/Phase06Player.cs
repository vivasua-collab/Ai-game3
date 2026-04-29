// ============================================================================
// Phase06Player.cs — Фаза 06: Создание игрока
// Cultivation World Simulator
// ============================================================================
// Объединяет: Phase 06 (FullSceneBuilder) + PATCH-003
// Редактировано: 2026-04-28 14:45 UTC — grid→line: удалены defaultGridWidth/Height, добавлены defaultMaxVolume/useVolumeLimit
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Core;
using CultivationGame.Player;
using CultivationGame.Qi;
using CultivationGame.Body;
using CultivationGame.Inventory;
using CultivationGame.Combat;
using CultivationGame.Interaction;
using CultivationGame.DebugTools;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase06Player : IScenePhase
    {
        public string Name => "Player";
        public string MenuPath => "Phase 06: Player";
        public int Order => 6;

        public bool IsNeeded()
        {
            // Если сцена не открыта — Find вернёт null → return true
            return GameObject.Find("Player") == null;
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            if (GameObject.Find("Player") != null)
            {
                Debug.Log("[Phase06] Player already exists");
                return;
            }

            GameObject player = new GameObject("Player");
            player.transform.position = new Vector3(100f, 80f, 0f); // Центр карты 100×80
            player.tag = "Player";

            // Слой Player через NameToLayer
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                player.layer = playerLayer;
            else
                Debug.LogWarning("[Phase06] Слой 'Player' не найден! Сначала выполните Phase 02.");

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

            // PlayerVisual создаёт дочерний "Visual" со SpriteRenderer
            player.AddComponent<PlayerVisual>();

            // Debug: EquipmentDebugPanel (F2 — отладочная панель инвентаря/генератора)
            player.AddComponent<EquipmentDebugPanel>();

            // Настройка компонентов
            ConfigurePlayerController(player);
            ConfigureBodyController(player);
            ConfigureQiController(player);
            ConfigureInventoryController(player);
            ConfigureEquipmentController(player);
            ConfigureTechniqueController(player);
            ConfigureSleepSystem(player);

            // PATCH-003: Убедиться что SpriteRenderer на правильном слое
            EnsurePlayerSortingLayer(player);

            Undo.RegisterCreatedObjectUndo(player, "Create Player");
            Debug.Log("[Phase06] Player created with all components");
        }

        private void ConfigurePlayerController(GameObject player)
        {
            SceneBuilderUtils.SetupComponent<PlayerController>(player, pc =>
            {
                SerializedObject so = new SerializedObject(pc);
                SceneBuilderUtils.SetProperty(so, "playerId", "player");
                SceneBuilderUtils.SetProperty(so, "playerName", "Игрок");
                SceneBuilderUtils.SetProperty(so, "moveSpeed", 5f);
                SceneBuilderUtils.SetProperty(so, "runSpeedMultiplier", 1.5f);
                so.ApplyModifiedProperties();
            });
        }

        private void ConfigureBodyController(GameObject player)
        {
            SceneBuilderUtils.SetupComponent<BodyController>(player, bc =>
            {
                SerializedObject so = new SerializedObject(bc);
                SceneBuilderUtils.SetProperty(so, "bodyMaterial", (int)BodyMaterial.Organic);
                SceneBuilderUtils.SetProperty(so, "vitality", 10);
                SceneBuilderUtils.SetProperty(so, "cultivationLevel", 1);
                SceneBuilderUtils.SetProperty(so, "enableRegeneration", true);
                SceneBuilderUtils.SetProperty(so, "regenRate", 1f);
                so.ApplyModifiedProperties();
            });
        }

        private void ConfigureQiController(GameObject player)
        {
            SceneBuilderUtils.SetupComponent<QiController>(player, qc =>
            {
                SerializedObject so = new SerializedObject(qc);
                SceneBuilderUtils.SetProperty(so, "cultivationLevel", 1);
                SceneBuilderUtils.SetProperty(so, "cultivationSubLevel", 0);
                SceneBuilderUtils.SetProperty(so, "coreQuality", 3); // CoreQuality.Normal (индекс 3)
                SceneBuilderUtils.SetProperty(so, "currentQi", 100L);
                SceneBuilderUtils.SetProperty(so, "enablePassiveRegen", true);
                so.ApplyModifiedProperties();
            });
        }

        private void ConfigureInventoryController(GameObject player)
        {
            SceneBuilderUtils.SetupComponent<InventoryController>(player, ic =>
            {
                // v3.0: строчная модель — weight + volume (gridWidth/gridHeight удалены)
                // размер определяется рюкзаком (BackpackData.maxWeight/maxVolume)
                SerializedObject so = new SerializedObject(ic);
                SceneBuilderUtils.SetProperty(so, "baseMaxWeight", 30f);
                SceneBuilderUtils.SetProperty(so, "defaultMaxVolume", 50f);
                SceneBuilderUtils.SetProperty(so, "useWeightLimit", true);
                SceneBuilderUtils.SetProperty(so, "useVolumeLimit", true);
                so.ApplyModifiedProperties();
            });
        }

        private void ConfigureEquipmentController(GameObject player)
        {
            SceneBuilderUtils.SetupComponent<EquipmentController>(player, ec =>
            {
                // v2.0: useLayerSystem и maxLayersPerSlot убраны (нет слоёв)
                SerializedObject so = new SerializedObject(ec);
                SceneBuilderUtils.SetProperty(so, "enforceRequirements", true);
                so.ApplyModifiedProperties();
            });
        }

        private void ConfigureTechniqueController(GameObject player)
        {
            SceneBuilderUtils.SetupComponent<TechniqueController>(player, tc =>
            {
                SerializedObject so = new SerializedObject(tc);
                SceneBuilderUtils.SetProperty(so, "maxQuickSlots", 10);
                SceneBuilderUtils.SetProperty(so, "maxUltimates", 1);
                so.ApplyModifiedProperties();
            });
        }

        private void ConfigureSleepSystem(GameObject player)
        {
            SceneBuilderUtils.SetupComponent<SleepSystem>(player, ss =>
            {
                SerializedObject so = new SerializedObject(ss);
                SceneBuilderUtils.SetProperty(so, "minSleepHours", 4f);
                SceneBuilderUtils.SetProperty(so, "maxSleepHours", 12f);
                SceneBuilderUtils.SetProperty(so, "optimalSleepHours", 8f);
                so.ApplyModifiedProperties();
            });
        }

        /// <summary>
        /// PATCH-003: Player SpriteRenderer на правильном Sorting Layer.
        /// Игрок должен быть на слое "Player" (выше Terrain и Objects).
        /// </summary>
        private void EnsurePlayerSortingLayer(GameObject player)
        {
            var sr = player.GetComponentInChildren<SpriteRenderer>();
            if (sr == null) return;

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
            }
            else
            {
                sr.sortingLayerName = "Objects";
                sr.sortingOrder = 100;
                Debug.LogWarning("[Phase06] PATCH-003: Слой Player недоступен → fallback 'Objects' order=100");
            }
        }
    }
}
#endif
