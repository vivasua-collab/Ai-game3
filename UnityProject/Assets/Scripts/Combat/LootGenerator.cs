// ============================================================================
// LootGenerator.cs — Генерация лута при смерти (СЛОЙ 10b)
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-05-04 07:28:00 UTC
// ============================================================================
//
// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  СЛОЙ 10b: LOOT GENERATION                                                  ║
// ╠═══════════════════════════════════════════════════════════════════════════╣
// ║  Генерирует дроп при смерти ICombatant.                                     ║
// ║  Факторы: уровень культивации, стихия, вид существа.                        ║
// ║  Результат: список LootEntry (itemId + amount).                              ║
// ╚═══════════════════════════════════════════════════════════════════════════╝
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Запись лута (один предмет + количество).
    /// </summary>
    [Serializable]
    public struct LootEntry
    {
        /// <summary>ID предмета</summary>
        public string ItemId;
        /// <summary>Количество</summary>
        public int Amount;
        /// <summary>Редкость предмета</summary>
        public ItemRarity Rarity;
        /// <summary>Источник (кто дропнул)</summary>
        public string SourceName;

        public LootEntry(string itemId, int amount, ItemRarity rarity = ItemRarity.Common, string source = "")
        {
            ItemId = itemId;
            Amount = amount;
            Rarity = rarity;
            SourceName = source;
        }
    }

    /// <summary>
    /// Результат генерации лута.
    /// </summary>
    [Serializable]
    public class LootResult
    {
        public List<LootEntry> Items = new List<LootEntry>();
        public long QiAbsorbed;          // Ци, поглощённое из поверженного
        public int CultivationExp;       // Опыт культивации за победу

        /// <summary>Общее количество предметов</summary>
        public int TotalItemCount => Items.Count;

        /// <summary>Есть ли лут?</summary>
        public bool HasLoot => Items.Count > 0 || QiAbsorbed > 0;
    }

    /// <summary>
    /// Генератор лута при смерти ICombatant.
    /// СЛОЙ 10b пайплайна урона — вызывается при смерти.
    ///
    /// Факторы генерации:
    /// - Уровень культивации поверженного → выше уровень = лучше лут
    /// - Стихия → элементальные ресурсы
    /// - Вид существа → разные типы дропа
    ///
    /// Формулы:
    /// - qiAbsorbed = maxQi × absorptionRate (10-30% в зависимости от разницы уровней)
    /// - cultivationExp = cultivationLevel × 10 + random(0, cultivationLevel × 5)
    /// - itemChance = baseDropRate × (1 + (level - 1) × 0.1)
    /// </summary>
    public static class LootGenerator
    {
        #region Константы

        /// <summary>Базовый шанс выпадения предмета (0.0-1.0)</summary>
        private const float BASE_DROP_RATE = 0.4f;
        /// <summary>Бонус к шансу за уровень культивации</summary>
        private const float DROP_RATE_PER_LEVEL = 0.05f;
        /// <summary>Максимальный шанс выпадения</summary>
        private const float MAX_DROP_RATE = 0.95f;
        /// <summary>Базовый % поглощения Ци</summary>
        private const float BASE_QI_ABSORPTION_RATE = 0.1f;
        /// <summary>Бонус поглощения Ци за разницу уровней (атакующий выше)</summary>
        private const float QI_ABSORPTION_PER_LEVEL_ABOVE = 0.02f;
        /// <summary>Максимальный % поглощения Ци</summary>
        private const float MAX_QI_ABSORPTION_RATE = 0.3f;
        /// <summary>Множитель опыта за уровень</summary>
        private const int EXP_PER_LEVEL = 10;
        /// <summary>Рандомная добавка опыта за уровень</summary>
        private const int EXP_RANDOM_PER_LEVEL = 5;

        #endregion

        #region Публичные методы

        /// <summary>
        /// Сгенерировать лут при смерти ICombatant.
        /// Основной метод — вызывается из CombatManager при завершении боя.
        /// </summary>
        /// <param name="defeated">Поверженный combatant</param>
        /// <param name="victorLevel">Уровень культивации победителя</param>
        /// <returns>Результат генерации лута</returns>
        public static LootResult GenerateLoot(ICombatant defeated, int victorLevel = 1)
        {
            if (defeated == null)
                return new LootResult();

            LootResult result = new LootResult();

            int defeatedLevel = defeated.CultivationLevel;
            long defeatedMaxQi = defeated.MaxQi;
            Element defeatedElement = defeated.GetDefenderParams().DefenderElement;

            // === Ци поглощение ===
            result.QiAbsorbed = CalculateQiAbsorption(defeatedMaxQi, defeatedLevel, victorLevel);

            // === Опыт культивации ===
            result.CultivationExp = CalculateCultivationExp(defeatedLevel);

            // === Предметы ===
            GenerateItemDrops(result, defeatedLevel, defeatedElement, defeated.BodyMaterial, defeated.Name);

            // ФАЗА 6: Шанс выпадения экипировки убитого NPC
            GenerateEquipmentDrops(result, defeated);

            Debug.Log($"[LootGenerator] Лут из {defeated.Name}: " +
                      $"{result.TotalItemCount} предметов, " +
                      $"Ци={result.QiAbsorbed}, Опыт={result.CultivationExp}");

            return result;
        }

        /// <summary>
        /// Рассчитать количество поглощённого Ци.
        /// Формула: maxQi × absorptionRate
        /// absorptionRate = BASE + (victorLevel - defeatedLevel) × BONUS
        /// </summary>
        public static long CalculateQiAbsorption(long defeatedMaxQi, int defeatedLevel, int victorLevel)
        {
            float rate = BASE_QI_ABSORPTION_RATE;

            // Бонус за разницу уровней (победитель сильнее → больше поглощает)
            int levelDiff = victorLevel - defeatedLevel;
            if (levelDiff > 0)
            {
                rate += levelDiff * QI_ABSORPTION_PER_LEVEL_ABOVE;
            }

            rate = Mathf.Min(rate, MAX_QI_ABSORPTION_RATE);
            rate = Mathf.Max(rate, BASE_QI_ABSORPTION_RATE);

            return (long)(defeatedMaxQi * rate);
        }

        /// <summary>
        /// Рассчитать опыт культивации за победу.
        /// Формула: level × EXP_PER_LEVEL + random(0, level × EXP_RANDOM_PER_LEVEL)
        /// </summary>
        public static int CalculateCultivationExp(int defeatedLevel)
        {
            int baseExp = defeatedLevel * EXP_PER_LEVEL;
            int randomBonus = UnityEngine.Random.Range(0, defeatedLevel * EXP_RANDOM_PER_LEVEL + 1);
            return baseExp + randomBonus;
        }

        #endregion

        #region Генерация предметов

        /// <summary>
        /// Генерация предметов на основе уровня и стихии.
        /// </summary>
        private static void GenerateItemDrops(
            LootResult result,
            int defeatedLevel,
            Element element,
            BodyMaterial bodyMaterial,
            string sourceName)
        {
            // Рассчитываем шанс выпадения
            float dropRate = BASE_DROP_RATE + (defeatedLevel - 1) * DROP_RATE_PER_LEVEL;
            dropRate = Mathf.Min(dropRate, MAX_DROP_RATE);

            // 1. Обязательный дроп — Ци-ядро (всегда)
            if (defeatedLevel >= 1)
            {
                result.Items.Add(new LootEntry(
                    "qi_core",
                    UnityEngine.Random.Range(1, 3),
                    defeatedLevel >= 5 ? ItemRarity.Uncommon : ItemRarity.Common,
                    sourceName
                ));
            }

            // 2. Элементальный ресурс (по стихии)
            if (element != Element.Neutral && RollChance(dropRate))
            {
                string elementItem = GetElementalItemId(element);
                if (!string.IsNullOrEmpty(elementItem))
                {
                    result.Items.Add(new LootEntry(
                        elementItem,
                        UnityEngine.Random.Range(1, 2 + defeatedLevel / 3),
                        defeatedLevel >= 7 ? ItemRarity.Uncommon : ItemRarity.Common,
                        sourceName
                    ));
                }
            }

            // 3. Материал тела
            if (RollChance(dropRate * 0.5f))
            {
                string bodyItem = GetBodyMaterialItemId(bodyMaterial);
                if (!string.IsNullOrEmpty(bodyItem))
                {
                    result.Items.Add(new LootEntry(
                        bodyItem,
                        UnityEngine.Random.Range(1, 3),
                        ItemRarity.Common,
                        sourceName
                    ));
                }
            }

            // 4. Редкий дроп (уровень 3+)
            if (defeatedLevel >= 3 && RollChance(dropRate * 0.3f))
            {
                string rareItem = GetRareItemId(defeatedLevel, element);
                if (!string.IsNullOrEmpty(rareItem))
                {
                    result.Items.Add(new LootEntry(
                        rareItem,
                        1,
                        defeatedLevel >= 8 ? ItemRarity.Rare : ItemRarity.Uncommon,
                        sourceName
                    ));
                }
            }

            // 5. Эпический дроп (уровень 7+)
            if (defeatedLevel >= 7 && RollChance(dropRate * 0.1f))
            {
                result.Items.Add(new LootEntry(
                    GetEpicItemId(defeatedLevel, element),
                    1,
                    ItemRarity.Rare,
                    sourceName
                ));
            }
        }

        /// <summary>
        /// Получить ID элементального ресурса.
        /// </summary>
        private static string GetElementalItemId(Element element)
        {
            return element switch
            {
                Element.Fire => "fire_essence",
                Element.Water => "water_essence",
                Element.Earth => "earth_essence",
                Element.Air => "wind_essence",
                Element.Lightning => "lightning_essence",
                Element.Void => "void_shard",
                Element.Poison => "poison_gland",
                _ => ""
            };
        }

        /// <summary>
        /// Получить ID материала из тела.
        /// </summary>
        private static string GetBodyMaterialItemId(BodyMaterial material)
        {
            return material switch
            {
                BodyMaterial.Organic => "organic_material",
                BodyMaterial.Scaled => "scale_fragment",
                BodyMaterial.Chitin => "chitin_plate",
                BodyMaterial.Mineral => "mineral_shard",
                BodyMaterial.Ethereal => "ethereal_essence",
                BodyMaterial.Construct => "construct_scrap",
                BodyMaterial.Chaos => "chaos_fragment",
                _ => ""
            };
        }

        /// <summary>
        /// Получить ID редкого предмета.
        /// </summary>
        private static string GetRareItemId(int level, Element element)
        {
            // Выбираем из пула по стихии
            string[] fireItems = { "flame_core", "ember_stone", "phoenix_feather" };
            string[] waterItems = { "frost_crystal", "sea_pearl", "ice_shard" };
            string[] earthItems = { "earth_crystal", "mountain_heart", "clay_tablet" };
            string[] genericItems = { "cultivation_pill", "spirit_herb", "qi_crystal", "medallion" };

            string[] pool = element switch
            {
                Element.Fire => fireItems,
                Element.Water => waterItems,
                Element.Earth => earthItems,
                _ => genericItems
            };

            int index = UnityEngine.Random.Range(0, pool.Length);
            return pool[index];
        }

        /// <summary>
        /// Получить ID эпического предмета.
        /// </summary>
        private static string GetEpicItemId(int level, Element element)
        {
            string[] epics = { "ancient_scroll", "spirit_vein_map", "breakthrough_pill", "formation_disk" };
            int index = UnityEngine.Random.Range(0, epics.Length);
            return epics[index];
        }

        private static bool RollChance(float chance)
        {
            return UnityEngine.Random.value < chance;
        }

        // ФАЗА 6: Шанс выпадения экипировки убитого NPC
        private const float EQUIPMENT_DROP_RATE = 0.30f; // 30% шанс выпадения каждого предмета

        /// <summary>
        /// ФАЗА 6: Сгенерировать выпадение экипировки убитого NPC.
        /// Каждый экипированный предмет имеет 30% шанс выпасть.
        /// </summary>
        private static void GenerateEquipmentDrops(LootResult result, ICombatant defeated)
        {
            if (defeated?.GameObject == null) return;

            var eqCtrl = defeated.GameObject.GetComponent<CultivationGame.Inventory.EquipmentController>();
            if (eqCtrl == null) return;

            foreach (var slot in CultivationGame.Inventory.EquipmentController.VisibleSlots)
            {
                var instance = eqCtrl.GetEquipment(slot);
                if (instance == null) continue;

                if (RollChance(EQUIPMENT_DROP_RATE))
                {
                    string itemId = instance.equipmentData?.itemId ?? "";
                    if (!string.IsNullOrEmpty(itemId))
                    {
                        // Добавляем предмет в лут (как EquipmentEntry)
                        result.Items.Add(new LootEntry
                        {
                            ItemId = itemId,
                            Amount = 1,
                            Rarity = instance.equipmentData?.rarity ?? CultivationGame.Core.ItemRarity.Common
                        });
                        Debug.Log($"[LootGenerator] Экипировка выпала: {instance.Name} ({slot})");
                    }
                }
            }
        }

        #endregion
    }
}
