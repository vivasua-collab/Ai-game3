// ============================================================================
// EquipmentRuntimeSpawner.cs — Runtime спавн экипировки (без Editor API)
// Cultivation World Simulator
// Создано: 2026-05-04 12:00:00 UTC
// ============================================================================
//
// Runtime-версия EquipmentSceneSpawner — без Undo, MenuItem, AssetDatabase.
// Используется DebugMenuController для спавна лута в Play Mode.
//
// Создаёт GameObject с ResourcePickup + EquipmentData возле позиции Player.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Generators;
using CultivationGame.TileSystem;
using CultivationGame.Inventory;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Runtime спавнер экипировки — создаёт лут в сцене или добавляет в инвентарь.
    /// Работает в Play Mode без Editor API.
    /// </summary>
    public static class EquipmentRuntimeSpawner
    {
        // ================================================================
        //  КОНСТАНТЫ
        // ================================================================

        private const float SPREAD_RADIUS = 3f;
        private const float PICKUP_RADIUS = 1f;

        // ================================================================
        //  СПАВН В СЦЕНУ
        // ================================================================

        /// <summary>
        /// Спавн N случайных предметов рядом с Player.
        /// </summary>
        public static int SpawnRandomLootNearPlayer(int count, int level = 1)
        {
            var player = GameObject.Find("Player");
            Vector3 center = player != null ? player.transform.position : Vector3.zero;

            if (player == null)
                Debug.LogWarning("[EquipRuntimeSpawner] Player не найден! Спавн в (0,0,0)");

            var rng = new SeededRandom();
            var loot = LootGenerator.GenerateLoot(level, count, rng);
            int spawned = 0;

            foreach (var equipment in loot)
            {
                Vector3 offset = Random.insideUnitSphere * SPREAD_RADIUS;
                offset.z = 0;
                SpawnEquipmentPickup(equipment, center + offset);
                spawned++;
            }

            Debug.Log($"[EquipRuntimeSpawner] Спавн: {spawned} предметов уровня {level}");
            return spawned;
        }

        /// <summary>
        /// Спавн оружия в сцену рядом с Player.
        /// </summary>
        public static void SpawnWeaponInScene()
        {
            var player = GameObject.Find("Player");
            Vector3 center = player != null ? player.transform.position : Vector3.zero;
            Vector3 offset = Random.insideUnitSphere * 1.5f;
            offset.z = 0;

            var equipment = LootGenerator.GenerateRandomWeapon(1);
            SpawnEquipmentPickup(equipment, center + offset);
            Debug.Log($"[EquipRuntimeSpawner] Спавн оружия: {equipment.nameRu}");
        }

        /// <summary>
        /// Спавн брони в сцену рядом с Player.
        /// </summary>
        public static void SpawnArmorInScene()
        {
            var player = GameObject.Find("Player");
            Vector3 center = player != null ? player.transform.position : Vector3.zero;
            Vector3 offset = Random.insideUnitSphere * 1.5f;
            offset.z = 0;

            var equipment = LootGenerator.GenerateRandomArmor(1);
            SpawnEquipmentPickup(equipment, center + offset);
            Debug.Log($"[EquipRuntimeSpawner] Спавн брони: {equipment.nameRu}");
        }

        // ================================================================
        //  ДОБАВЛЕНИЕ В ИНВЕНТАРЬ
        // ================================================================

        /// <summary>
        /// Добавить оружие в инвентарь Player.
        /// </summary>
        public static bool AddWeaponToInventory()
        {
            var equip = LootGenerator.GenerateRandomWeapon(1);
            return AddToPlayerInventory(equip);
        }

        /// <summary>
        /// Добавить N случайных предметов в инвентарь Player.
        /// </summary>
        public static int AddRandomToInventory(int count)
        {
            var loot = LootGenerator.GenerateLoot(1, count);
            int added = 0;
            foreach (var eq in loot)
            {
                if (AddToPlayerInventory(eq))
                    added++;
            }
            Debug.Log($"[EquipRuntimeSpawner] Добавлено {added}/{loot.Count} в инвентарь");
            return added;
        }

        // ================================================================
        //  ОЧИСТКА
        // ================================================================

        /// <summary>
        /// Удалить весь лут (ResourcePickup) из сцены.
        /// </summary>
        public static int ClearAllLoot()
        {
            var pickups = Object.FindObjectsByType<ResourcePickup>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var pickup in pickups)
            {
                Object.Destroy(pickup.gameObject);
                count++;
            }
            Debug.Log($"[EquipRuntimeSpawner] Удалено {count} предметов");
            return count;
        }

        // ================================================================
        //  РЕАЛИЗАЦИЯ
        // ================================================================

        private static void SpawnEquipmentPickup(EquipmentData equipment, Vector3 position)
        {
            var go = new GameObject($"Loot_{equipment.nameRu}");
            go.transform.position = position;

            int itemsLayer = LayerMask.NameToLayer("Items");
            go.layer = itemsLayer >= 0 ? itemsLayer : 0;

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
        }

        private static bool AddToPlayerInventory(EquipmentData equipment)
        {
            if (equipment == null) return false;

            var player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogWarning("[EquipRuntimeSpawner] Player не найден!");
                return false;
            }

            var inventory = player.GetComponent<InventoryController>();
            if (inventory == null)
            {
                Debug.LogWarning("[EquipRuntimeSpawner] InventoryController не найден!");
                return false;
            }

            var slot = inventory.AddItem(equipment, 1);
            if (slot != null)
            {
                Debug.Log($"[EquipRuntimeSpawner] {equipment.nameRu} → инвентарь, слот {slot.SlotId}");
                return true;
            }

            Debug.LogWarning("[EquipRuntimeSpawner] Инвентарь полон!");
            return false;
        }

        // ================================================================
        //  УТИЛИТЫ
        // ================================================================

        private static Color RarityToColor(ItemRarity rarity) => rarity switch
        {
            ItemRarity.Common    => new Color(0.7f, 0.7f, 0.7f),
            ItemRarity.Uncommon  => new Color(0.3f, 0.9f, 0.3f),
            ItemRarity.Rare      => new Color(0.2f, 0.5f, 1f),
            ItemRarity.Epic      => new Color(0.7f, 0.2f, 1f),
            ItemRarity.Legendary => new Color(1f, 0.6f, 0.1f),
            ItemRarity.Mythic    => new Color(1f, 0.15f, 0.15f),
            _                    => Color.white
        };

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
