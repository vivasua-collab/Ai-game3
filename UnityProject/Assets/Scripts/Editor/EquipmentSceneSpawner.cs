// ============================================================================
// EquipmentSceneSpawner.cs — Генерация эквипа в активной сцене (hotkey + menu)
// Cultivation World Simulator
// Создано: 2026-04-29
// ============================================================================
//
// Горячие клавиши:
//   Ctrl+G     — 3 случайных предмета рядом с игроком
//   Ctrl+Shift+G — 10 случайных предметов рядом с игроком
//   Ctrl+F1    — 1 оружие T1 в инвентарь игрока
//   Ctrl+F2    — 1 броня T1 в инвентарь игрока
//
// Пункты меню: Tools/Equipment/Spawn In Scene/
//
// Использует LootGenerator (runtime) — не создаёт .asset файлов.
// Спавнит ResourcePickup с EquipmentData возле позиции Player.
// ============================================================================

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using CultivationGame.Core;
using CultivationGame.Generators;
using CultivationGame.TileSystem;
using CultivationGame.Inventory;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Editor
{
    /// <summary>
    /// Editor-утилита: генерация экипировки прямо в активной сцене.
    /// Спавнит ResourcePickup с EquipmentData или добавляет в инвентарь.
    /// </summary>
    public static class EquipmentSceneSpawner
    {
        // ================================================================
        //  КОНСТАНТЫ
        // ================================================================

        private const float SPREAD_RADIUS = 3f;   // Разброс вокруг игрока
        private const float PICKUP_RADIUS = 1f;    // Радиус подбора
        private const float LOOT_LIFETIME = 600f;  // 10 минут

        // ================================================================
        //  МЕНЮ: СПАВН В СЦЕНУ (ResourcePickup)
        // ================================================================

        /// Спавн 3 случайных предметов рядом с игроком [Ctrl+G]
        [MenuItem("Tools/Equipment/Spawn In Scene/Random Loot x3 _%g", false, 30)]
        public static void SpawnRandomLoot3() => SpawnLootNearPlayer(3, 1);

        /// Спавн 10 случайных предметов рядом с игроком [Ctrl+Shift+G]
        [MenuItem("Tools/Equipment/Spawn In Scene/Random Loot x10 _%#g", false, 31)]
        public static void SpawnRandomLoot10() => SpawnLootNearPlayer(10, 1);

        /// Спавн 5 предметов уровня 3 [Ctrl+Alt+G]
        [MenuItem("Tools/Equipment/Spawn In Scene/Random Loot L3 x5 _%&g", false, 32)]
        public static void SpawnRandomLootL3() => SpawnLootNearPlayer(5, 3);

        /// Спавн 1 оружия рядом с игроком
        [MenuItem("Tools/Equipment/Spawn In Scene/Weapon (Random)", false, 33)]
        public static void SpawnWeapon() => SpawnSingleNearPlayer(() =>
            LootGenerator.GenerateRandomWeapon(1));

        /// Спавн 1 брони рядом с игроком
        [MenuItem("Tools/Equipment/Spawn In Scene/Armor (Random)", false, 34)]
        public static void SpawnArmor() => SpawnSingleNearPlayer(() =>
            LootGenerator.GenerateRandomArmor(1));

        // ================================================================
        //  МЕНЮ: ПРЯМО В ИНВЕНТАРЬ
        // ================================================================

        /// Добавить 1 оружие в инвентарь игрока [Ctrl+F1]
        [MenuItem("Tools/Equipment/Add to Inventory/Weapon _%F1", false, 40)]
        public static void AddWeaponToInventory()
        {
            var equip = LootGenerator.GenerateRandomWeapon(1);
            AddToPlayerInventory(equip);
        }

        /// Добавить 1 броню в инвентарь игрока [Ctrl+F2]
        [MenuItem("Tools/Equipment/Add to Inventory/Armor _%F2", false, 41)]
        public static void AddArmorToInventory()
        {
            var equip = LootGenerator.GenerateRandomArmor(1);
            AddToPlayerInventory(equip);
        }

        /// Добавить 3 случайных предмета в инвентарь [Ctrl+F3]
        [MenuItem("Tools/Equipment/Add to Inventory/Random Loot x3 _%F3", false, 42)]
        public static void AddRandomLootToInventory()
        {
            var loot = LootGenerator.GenerateLoot(1, 3);
            int added = 0;
            foreach (var eq in loot)
            {
                if (AddToPlayerInventory(eq))
                    added++;
            }
            Debug.Log($"[EquipmentSpawner] Добавлено {added}/{loot.Count} предметов в инвентарь");
        }

        // ================================================================
        //  РЕАЛИЗАЦИЯ: СПАВН В СЦЕНУ
        // ================================================================

        /// <summary>
        /// Спавнит несколько ResourcePickup с EquipmentData рядом с Player.
        /// </summary>
        private static void SpawnLootNearPlayer(int count, int level)
        {
            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogWarning("[EquipmentSpawner] Player не найден в сцене! Спавн в точке (0,0,0)");
            }

            Vector3 center = player != null ? player.transform.position : Vector3.zero;
            var rng = new SeededRandom();
            var loot = LootGenerator.GenerateLoot(level, count, rng);
            int spawned = 0;

            foreach (var equipment in loot)
            {
                Vector3 offset = Random.insideUnitSphere * SPREAD_RADIUS;
                offset.z = 0; // 2D — Z всегда 0
                Vector3 pos = center + offset;

                SpawnEquipmentPickup(equipment, pos);
                spawned++;
            }

            Debug.Log($"[EquipmentSpawner] Спавн: {spawned} предметов уровня {level} возле Player");
        }

        /// <summary>
        /// Спавнит один предмет рядом с игроком.
        /// </summary>
        private static void SpawnSingleNearPlayer(System.Func<EquipmentData> generator)
        {
            var player = GameObject.Find("Player");
            Vector3 center = player != null ? player.transform.position : Vector3.zero;
            Vector3 offset = Random.insideUnitSphere * 1.5f;
            offset.z = 0;

            var equipment = generator();
            SpawnEquipmentPickup(equipment, center + offset);

            Debug.Log($"[EquipmentSpawner] Спавн: {equipment.nameRu} ({equipment.rarity}) возле Player");
        }

        /// <summary>
        /// Создать GameObject с ResourcePickup + EquipmentData.
        /// </summary>
        private static void SpawnEquipmentPickup(EquipmentData equipment, Vector3 position)
        {
            // GameObject
            var go = new GameObject($"Loot_{equipment.nameRu}");
            go.transform.position = position;
            go.layer = LayerMask.NameToLayer("Items");
            if (go.layer < 0)
                go.layer = 0; // Default если слой Items не существует

            // SpriteRenderer — цветной кружок по редкости
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite();
            sr.color = RarityToColor(equipment.rarity);
            sr.sortingLayerName = "Objects";
            sr.sortingOrder = 50;

            // Collider для подбора
            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = PICKUP_RADIUS;

            // ResourcePickup
            var pickup = go.AddComponent<ResourcePickup>();
            pickup.Initialize(equipment.itemId, 1, equipment);
            pickup.AutoPickup = false;
            pickup.PickupRadius = PICKUP_RADIUS;

            // Undo
            Undo.RegisterCreatedObjectUndo(go, "Spawn Equipment Loot");
        }

        // ================================================================
        //  РЕАЛИЗАЦИЯ: ДОБАВЛЕНИЕ В ИНВЕНТАРЬ
        // ================================================================

        /// <summary>
        /// Добавить EquipmentData в инвентарь Player.
        /// Возвращает true если успешно.
        /// </summary>
        private static bool AddToPlayerInventory(EquipmentData equipment)
        {
            if (equipment == null)
            {
                Debug.LogWarning("[EquipmentSpawner] EquipmentData is null!");
                return false;
            }

            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogWarning("[EquipmentSpawner] Player не найден в сцене!");
                return false;
            }

            var inventory = player.GetComponent<InventoryController>();
            if (inventory == null)
            {
                Debug.LogWarning("[EquipmentSpawner] InventoryController не найден у Player!");
                return false;
            }

            var slot = inventory.AddItem(equipment, 1);
            if (slot != null)
            {
                Debug.Log($"[EquipmentSpawner] ✅ {equipment.nameRu} ({equipment.rarity}, " +
                          $"T{equipment.materialTier} {equipment.grade}) → инвентарь, слот {slot.SlotId}");
                return true;
            }
            else
            {
                Debug.LogWarning($"[EquipmentSpawner] ❌ Инвентарь полон! Не добавлено: {equipment.nameRu}");
                return false;
            }
        }

        // ================================================================
        //  УТИЛИТЫ
        // ================================================================

        /// <summary>
        /// Цвет по редкости (3-axis: Rarity → border / marker color).
        /// </summary>
        private static Color RarityToColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common    => new Color(0.7f, 0.7f, 0.7f),   // Серый
            ItemRarity.Uncommon  => new Color(0.3f, 0.9f, 0.3f),   // Зелёный
            ItemRarity.Rare      => new Color(0.2f, 0.5f, 1f),     // Синий
            ItemRarity.Epic      => new Color(0.7f, 0.2f, 1f),     // Фиолетовый
            ItemRarity.Legendary => new Color(1f, 0.6f, 0.1f),     // Оранжевый
            ItemRarity.Mythic    => new Color(1f, 0.15f, 0.15f),   // Красный
            _                    => Color.white
        };

        /// <summary>
        /// Создать простой круглый спрайт (16x16 белая точка).
        /// Runtime-спрайт — не требует ассетов.
        /// </summary>
        private static Sprite CreateCircleSprite()
        {
            int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;

            float center = size / 2f;
            float radius = size / 2f - 1f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist <= radius ? Color.white : Color.clear);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
#endif
