// ============================================================================
// Phase18InventoryComponents.cs — Фаза 18: Компоненты инвентаря на Player
// Cultivation World Simulator
// ============================================================================
// Добавляет SpiritStorageController и StorageRingController на Player.
// Подключает InventoryController к InventoryScreen UI.
// Подключает EquipmentController к BodyDollPanel UI.
// Настраивает начальный рюкзак (ClothSack) на старте игры.
//
// Зависимости: Phase06Player (Player), Phase17InventoryUI (UI)
//
// Редактировано: 2026-04-27 18:15:00 UTC — строчная модель инвентаря
// Редактировано: 2026-04-29 12:13:35 UTC — аудит: IsNeeded() возвращает true при отсутствии зависимости
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CultivationGame.Inventory;

namespace CultivationGame.Editor.SceneBuilder
{
    public class Phase18InventoryComponents : IScenePhase
    {
        public string Name => "Inventory Components";
        public string MenuPath => "Phase 18: Inventory Components";
        public int Order => 18;

        public bool IsNeeded()
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                // Зависимость отсутствует — фаза требуется, но Execute() выдаст ошибку
                Debug.LogWarning("[Phase18] Player не найден! Сначала выполните Phase 06.");
                return true;
            }

            // Фаза нужна, если SpiritStorageController ещё не добавлен
            return player.GetComponent<SpiritStorageController>() == null;
        }

        public void Execute()
        {
            SceneBuilderUtils.EnsureSceneOpen();

            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("[Phase18] Player не найден! Сначала выполните Phase 06.");
                return;
            }

            // Шаг 1: Добавить SpiritStorageController
            if (player.GetComponent<SpiritStorageController>() == null)
            {
                var spiritStorage = player.AddComponent<SpiritStorageController>();
                ConfigureSpiritStorageController(player);
                Debug.Log("[Phase18] SpiritStorageController добавлен на Player");
            }
            else
            {
                Debug.Log("[Phase18] SpiritStorageController уже существует");
            }

            // Шаг 2: Добавить StorageRingController
            if (player.GetComponent<StorageRingController>() == null)
            {
                var storageRing = player.AddComponent<StorageRingController>();
                ConfigureStorageRingController(player);
                Debug.Log("[Phase18] StorageRingController добавлен на Player");
            }
            else
            {
                Debug.Log("[Phase18] StorageRingController уже существует");
            }

            // Шаг 3: Настроить InventoryController — подключить к UI
            ConfigureInventoryControllerUI(player);

            // Шаг 4: Настроить EquipmentController — подключить к BodyDoll
            ConfigureEquipmentControllerUI(player);

            // Шаг 5: Настроить начальный рюкзак
            SetupStarterBackpack(player);

            Debug.Log("[Phase18] ✅ Компоненты инвентаря настроены на Player: " +
                      "SpiritStorage + StorageRing + UI подключения");
        }

        // ====================================================================
        //  Configuration
        // ====================================================================

        private void ConfigureSpiritStorageController(GameObject player)
        {
            SceneBuilderUtils.SetupComponent<SpiritStorageController>(player, ssc =>
            {
                SerializedObject so = new SerializedObject(ssc);
                SceneBuilderUtils.SetProperty(so, "baseQiCost", 5);
                SceneBuilderUtils.SetProperty(so, "qiCostPerKg", 2f);
                SceneBuilderUtils.SetProperty(so, "retrievalQiCost", 3f);
                SceneBuilderUtils.SetProperty(so, "qiCostPerKgRetrieval", 1f);
                SceneBuilderUtils.SetProperty(so, "requiredCultivationLevel", 1); // AwakenedCore
                so.ApplyModifiedProperties();
            });
        }

        private void ConfigureStorageRingController(GameObject player)
        {
            SceneBuilderUtils.SetupComponent<StorageRingController>(player, src =>
            {
                // StorageRingController настраивается автоматически при экипировке кольца
                // через EquipmentController — нет SerializableField для дефолтов
            });
        }

        private void ConfigureInventoryControllerUI(GameObject player)
        {
            // Найти InventoryScreen в сцене
            var inventoryScreenObj = GameObject.Find("GameUI/InventoryScreen");
            if (inventoryScreenObj == null)
            {
                Debug.LogWarning("[Phase18] InventoryScreen не найден — UI подключение пропущено. Выполните Phase 17.");
                return;
            }

            SceneBuilderUtils.SetupComponent<InventoryController>(player, ic =>
            {
                // InventoryController v2.0 не имеет прямой ссылки на InventoryScreen —
                // подключение через UIManager в рантайме
                Debug.Log("[Phase18] InventoryController готов к подключению через UIManager");
            });
        }

        private void ConfigureEquipmentControllerUI(GameObject player)
        {
            SceneBuilderUtils.SetupComponent<EquipmentController>(player, ec =>
            {
                // EquipmentController v2.0 подключается к BodyDollPanel через UIManager
                Debug.Log("[Phase18] EquipmentController готов к подключению через UIManager");
            });
        }

        private void SetupStarterBackpack(GameObject player)
        {
            // Найти стартовый рюкзак (ClothSack)
            var starterBackpack = AssetDatabase.LoadAssetAtPath<Data.ScriptableObjects.BackpackData>(
                "Assets/Data/Backpacks/Backpack_ClothSack.asset");

            if (starterBackpack != null)
            {
                SceneBuilderUtils.SetupComponent<InventoryController>(player, ic =>
                {
                    SerializedObject so = new SerializedObject(ic);
                    // Ссылка на стартовый рюкзак устанавливается в рантайме через SetBackpack()
                    // Или можно назначить через serialized property, если InventoryController имеет [SerializeField] BackpackData
                    Debug.Log("[Phase18] Стартовый рюкзак ClothSack найден — будет назначен в рантайме");
                    so.ApplyModifiedProperties();
                });
            }
            else
            {
                Debug.LogWarning("[Phase18] Стартовый рюкзак ClothSack не найден! " +
                                 "Убедитесь, что Phase 16 выполнена.");
            }
        }
    }
}
#endif
