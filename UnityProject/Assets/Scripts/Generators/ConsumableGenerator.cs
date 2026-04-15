// ============================================================================
// ConsumableGenerator.cs — Генератор расходников
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-31 10:22:36 UTC
// Редактировано: 2026-03-31 10:22:36 UTC
// ============================================================================
//
// Источник: docs/INVENTORY_SYSTEM.md, docs/QI_SYSTEM.md
//
// Типы расходников:
// - Таблетки (Pill) — мгновенный эффект, возможны побочные эффекты
// - Эликсиры (Elixir) — длительный эффект, безопаснее таблеток
// - Еда (Food) — восстановление HP/выносливости
// - Напитки (Drink) — восстановление Ци
// - Яды (Poison) — негативные эффекты
// - Свитки (Scroll) — одноразовые техники
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Тип расходника
    /// </summary>
    public enum ConsumableType
    {
        Pill,           // Таблетка
        Elixir,         // Эликсир
        Food,           // Еда
        Drink,          // Напиток
        Poison,         // Яд
        Scroll,         // Свиток техники
        Talisman        // Талисман (одноразовый)
    }

    /// <summary>
    /// Категория эффекта расходника
    /// </summary>
    public enum ConsumableEffectCategory
    {
        Healing,        // Лечение HP
        QiRestoration,  // Восстановление Ци
        Buff,           // Временное усиление
        Debuff,         // Дебафф (для ядов)
        Cultivation,    // Помощь в культивации
        Permanent       // Постоянный эффект (редкие таблетки)
    }

    /// <summary>
    /// Параметры генерации расходника
    /// </summary>
    [Serializable]
    public class ConsumableGenerationParams
    {
        public ConsumableType type = ConsumableType.Pill;
        public ConsumableEffectCategory effectCategory = ConsumableEffectCategory.Healing;
        public int itemLevel = 1;               // Уровень предмета (1-9)
        public ItemRarity rarity = ItemRarity.Common;
        public int count = 1;
        public int? seed = null;
    }

    /// <summary>
    /// Результат генерации расходника
    /// </summary>
    [Serializable]
    public class GeneratedConsumable
    {
        // Identity
        public string id;
        public string nameRu;
        public string nameEn;

        // Classification
        public ConsumableType type;
        public ConsumableEffectCategory effectCategory;
        public ItemRarity rarity;

        // Level
        public int itemLevel;

        // Stack
        public int maxStack;
        public bool stackable;

        // Size
        public int sizeWidth;
        public int sizeHeight;

        // Value
        public int value;                       // Стоимость в духовных камнях
        public float weight;

        // Effects
        public List<ConsumableEffect> effects = new List<ConsumableEffect>();

        // Requirements
        public int requiredCultivationLevel;
        public int maxCultivationLevel;         // Не эффективно выше этого уровня

        // Side effects (для таблеток)
        public List<ConsumableEffect> sideEffects = new List<ConsumableEffect>();
        public float sideEffectChance;          // Шанс побочного эффекта

        // Duration
        public int duration;                    // Длительность в тиках (0 = мгновенно)

        // Cooldown
        public int cooldown;                    // Кулдаун между использованиями (тики)

        // Description
        public string description;

        // Icon (emoji for display)
        public string icon;
    }

    [Serializable]
    public class ConsumableEffect
    {
        public string effectType;
        public long valueLong;  // FIX GEN-H04: Qi values as long (2026-04-11)
        public float valueFloat; // Kept for non-Qi effects (percentages, etc.)
        public int duration;            // 0 = мгновенно
        public bool isPercentage;
        public bool isLongValue;         // FIX GEN-H04: Flag to distinguish long vs float (2026-04-11)

        // Backward-compatible property
        [System.Obsolete("Use valueLong or valueFloat depending on isLongValue")]
        public float value { get => isLongValue ? (float)valueLong : valueFloat; set { if (isLongValue) valueLong = (long)value; else valueFloat = value; } }
    }

    /// <summary>
    /// Генератор расходников
    /// </summary>
    public static class ConsumableGenerator
    {
        // === РЕДКОСТЬ И СТОИМОСТЬ ===
        // | Редкость    | Множитель стоимости |
        // |-------------|---------------------|
        // | Common      | ×1                  |
        // | Uncommon    | ×2                  |
        // | Rare        | ×5                  |
        // | Epic        | ×15                 |
        // | Legendary   | ×50                 |
        // | Mythic      | ×200                |
        private static readonly int[] RarityValueMult = { 1, 2, 5, 15, 50, 200 };

        // === НАЗВАНИЯ ПО ТИПАМ ===
        private static readonly Dictionary<ConsumableType, string[]> NamesByType = new Dictionary<ConsumableType, string[]>
        {
            { ConsumableType.Pill, new[] { "Таблетка", "Пилюля", "Таблетка", "Шарик", "Капсула" } },
            { ConsumableType.Elixir, new[] { "Эликсир", "Зелье", "Настойка", "Отвар", "Экстракт" } },
            { ConsumableType.Food, new[] { "Булочка", "Пирожное", "Лапша", "Рисовый шарик", "Мясо" } },
            { ConsumableType.Drink, new[] { "Чай", "Напиток", "Сок", "Вино", "Вода" } },
            { ConsumableType.Poison, new[] { "Яд", "Токсин", "Отрава", "Смертельная смесь", "Проклятие" } },
            { ConsumableType.Scroll, new[] { "Свиток", "Свиток техники", "Формула", "Рукопись", "Гримуар" } },
            { ConsumableType.Talisman, new[] { "Талисман", "Амулет", "Печать", "Знак", "Руна" } }
        };

        // Иконки по типам
        private static readonly Dictionary<ConsumableType, string> IconsByType = new Dictionary<ConsumableType, string>
        {
            { ConsumableType.Pill, "💊" },
            { ConsumableType.Elixir, "🧪" },
            { ConsumableType.Food, "🍱" },
            { ConsumableType.Drink, "🍵" },
            { ConsumableType.Poison, "☠️" },
            { ConsumableType.Scroll, "📜" },
            { ConsumableType.Talisman, "🔯" }
        };

        // Префиксы по эффектам
        private static readonly Dictionary<ConsumableEffectCategory, string[]> PrefixesByEffect = new Dictionary<ConsumableEffectCategory, string[]>
        {
            { ConsumableEffectCategory.Healing, new[] { "Исцеляющая", "Лечебная", "Восстанавливающая", "Живительная" } },
            { ConsumableEffectCategory.QiRestoration, new[] { "Духовная", "Ци-восстанавливающая", "Энергетическая", "Силовая" } },
            { ConsumableEffectCategory.Buff, new[] { "Усиливающая", "Боевая", "Мощная", "Героическая" } },
            { ConsumableEffectCategory.Debuff, new[] { "Ослабляющая", "Проклятая", "Тёмная", "Зловещая" } },
            { ConsumableEffectCategory.Cultivation, new[] { "Культигенная", "Пробуждающая", "Прозревающая", "Истинная" } },
            { ConsumableEffectCategory.Permanent, new[] { "Вечная", "Божественная", "Изначальная", "Небесная" } }
        };

        // Базовая стоимость по типу
        private static readonly Dictionary<ConsumableType, int> BaseValueByType = new Dictionary<ConsumableType, int>
        {
            { ConsumableType.Pill, 50 },
            { ConsumableType.Elixir, 100 },
            { ConsumableType.Food, 10 },
            { ConsumableType.Drink, 20 },
            { ConsumableType.Poison, 80 },
            { ConsumableType.Scroll, 200 },
            { ConsumableType.Talisman, 150 }
        };

        // Размеры по типу
        private static readonly Dictionary<ConsumableType, (int w, int h)> SizeByType = new Dictionary<ConsumableType, (int, int)>
        {
            { ConsumableType.Pill, (1, 1) },
            { ConsumableType.Elixir, (1, 1) },
            { ConsumableType.Food, (1, 1) },
            { ConsumableType.Drink, (1, 2) },
            { ConsumableType.Poison, (1, 1) },
            { ConsumableType.Scroll, (1, 2) },
            { ConsumableType.Talisman, (1, 1) }
        };

        // Максимальный стек по типу
        private static readonly Dictionary<ConsumableType, int> MaxStackByType = new Dictionary<ConsumableType, int>
        {
            { ConsumableType.Pill, 20 },
            { ConsumableType.Elixir, 10 },
            { ConsumableType.Food, 50 },
            { ConsumableType.Drink, 20 },
            { ConsumableType.Poison, 10 },
            { ConsumableType.Scroll, 5 },
            { ConsumableType.Talisman, 10 }
        };

        /// <summary>
        /// Сгенерировать один расходник
        /// </summary>
        public static GeneratedConsumable Generate(ConsumableGenerationParams parameters, SeededRandom rng = null)
        {
            if (rng == null)
                rng = new SeededRandom(parameters.seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));

            var consumable = new GeneratedConsumable();

            // Classification
            consumable.type = parameters.type;
            consumable.effectCategory = parameters.effectCategory;
            consumable.rarity = parameters.rarity;
            consumable.itemLevel = Mathf.Clamp(parameters.itemLevel, 1, 9);

            // Stack properties
            consumable.stackable = true;
            consumable.maxStack = MaxStackByType.ContainsKey(consumable.type) ? MaxStackByType[consumable.type] : 10;

            // Size
            var (w, h) = SizeByType.ContainsKey(consumable.type) ? SizeByType[consumable.type] : (1, 1);
            consumable.sizeWidth = w;
            consumable.sizeHeight = h;

            // Weight
            consumable.weight = consumable.type switch
            {
                ConsumableType.Pill => 0.01f,
                ConsumableType.Elixir => 0.2f,
                ConsumableType.Food => 0.3f,
                ConsumableType.Drink => 0.3f,
                ConsumableType.Scroll => 0.05f,
                ConsumableType.Talisman => 0.05f,
                _ => 0.1f
            };

            // Value
            int baseValue = BaseValueByType.ContainsKey(consumable.type) ? BaseValueByType[consumable.type] : 50;
            float levelMult = 1f + (consumable.itemLevel - 1) * 0.5f;
            int rarityMult = RarityValueMult[(int)consumable.rarity];
            consumable.value = Mathf.RoundToInt(baseValue * levelMult * rarityMult);

            // Generate effects
            GenerateEffects(consumable, rng);

            // Side effects (для таблеток с редкостью ниже Legendary)
            if (consumable.type == ConsumableType.Pill && consumable.rarity < ItemRarity.Legendary)
            {
                GenerateSideEffects(consumable, rng);
            }

            // Duration
            consumable.duration = consumable.type switch
            {
                ConsumableType.Pill => 0,  // Мгновенно
                ConsumableType.Elixir => 60 + consumable.itemLevel * 10, // 1-2.5 часа
                ConsumableType.Food => 30,
                ConsumableType.Drink => 30,
                ConsumableType.Poison => 60 + consumable.itemLevel * 20,
                ConsumableType.Talisman => 0,
                _ => 0
            };

            // Cooldown
            consumable.cooldown = consumable.type switch
            {
                ConsumableType.Pill => 10, // 10 минут
                ConsumableType.Elixir => 30,
                ConsumableType.Food => 5,
                ConsumableType.Drink => 5,
                _ => 0
            };

            // Requirements
            consumable.requiredCultivationLevel = Mathf.Max(0, consumable.itemLevel - 2);
            consumable.maxCultivationLevel = consumable.itemLevel + 4; // Не эффективно выше

            // Icon
            consumable.icon = IconsByType.ContainsKey(consumable.type) ? IconsByType[consumable.type] : "📦";

            // ID
            consumable.id = $"consumable_{consumable.type}_{consumable.effectCategory}_{consumable.itemLevel}_{rng.Next(1000):D3}";

            // Name
            consumable.nameRu = GenerateName(consumable, rng);
            consumable.nameEn = consumable.nameRu;

            // Description
            consumable.description = GenerateDescription(consumable);

            return consumable;
        }

        /// <summary>
        /// Сгенерировать несколько расходников
        /// </summary>
        public static List<GeneratedConsumable> GenerateMultiple(ConsumableGenerationParams parameters)
        {
            var rng = new SeededRandom(parameters.seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            var results = new List<GeneratedConsumable>();

            for (int i = 0; i < parameters.count; i++)
            {
                var consumable = Generate(parameters, rng);
                results.Add(consumable);
            }

            return results;
        }

        /// <summary>
        /// Сгенерировать расходник для уровня культивации
        /// </summary>
        public static GeneratedConsumable GenerateForLevel(int cultivationLevel, SeededRandom rng = null)
        {
            if (rng == null)
                rng = new SeededRandom();

            int itemLevel = Mathf.Clamp(cultivationLevel, 1, 9);

            // Случайный тип
            var types = (ConsumableType[])Enum.GetValues(typeof(ConsumableType));
            var type = rng.NextElement(types);

            // Эффект зависит от типа
            var effectCategories = type switch
            {
                ConsumableType.Pill => new[] { ConsumableEffectCategory.Healing, ConsumableEffectCategory.QiRestoration, ConsumableEffectCategory.Cultivation, ConsumableEffectCategory.Permanent },
                ConsumableType.Elixir => new[] { ConsumableEffectCategory.Buff, ConsumableEffectCategory.Healing, ConsumableEffectCategory.QiRestoration },
                ConsumableType.Food => new[] { ConsumableEffectCategory.Healing },
                ConsumableType.Drink => new[] { ConsumableEffectCategory.QiRestoration },
                ConsumableType.Poison => new[] { ConsumableEffectCategory.Debuff },
                ConsumableType.Scroll => new[] { ConsumableEffectCategory.Buff },
                ConsumableType.Talisman => new[] { ConsumableEffectCategory.Buff, ConsumableEffectCategory.Healing },
                _ => new[] { ConsumableEffectCategory.Healing }
            };

            var effectCategory = rng.NextElement(effectCategories);

            // Редкость по распределению
            var rarity = GenerateRarity(rng, cultivationLevel);

            var parameters = new ConsumableGenerationParams
            {
                type = type,
                effectCategory = effectCategory,
                itemLevel = itemLevel,
                rarity = rarity,
                seed = rng.Next()
            };

            return Generate(parameters, new SeededRandom(parameters.seed.Value));
        }

        // === Helpers ===

        private static void GenerateEffects(GeneratedConsumable consumable, SeededRandom rng)
        {
            float baseValue = consumable.itemLevel * 10 * ((int)consumable.rarity + 1);

            switch (consumable.effectCategory)
            {
                case ConsumableEffectCategory.Healing:
                    consumable.effects.Add(new ConsumableEffect
                    {
                        effectType = "healHP",
                        valueFloat = baseValue * 2,
                        isLongValue = false,
                        duration = consumable.duration,
                        isPercentage = false
                    });
                    break;

                case ConsumableEffectCategory.QiRestoration:
                    consumable.effects.Add(new ConsumableEffect
                    {
                        effectType = "restoreQi",
                        valueLong = (long)(baseValue * 100), // FIX GEN-H04: Qi as long (2026-04-11)
                        isLongValue = true,
                        duration = consumable.duration,
                        isPercentage = false
                    });
                    break;

                case ConsumableEffectCategory.Buff:
                    // Случайный бафф
                    string[] buffs = { "damage", "defense", "speed", "critChance", "qiRegen" };
                    string buff = rng.NextElement(buffs);
                    consumable.effects.Add(new ConsumableEffect
                    {
                        effectType = buff,
                        valueFloat = rng.NextFloat(0.1f, 0.3f) * ((int)consumable.rarity + 1),
                        isLongValue = false,
                        duration = consumable.duration,
                        isPercentage = true
                    });
                    break;

                case ConsumableEffectCategory.Debuff:
                    string[] debuffs = { "poison", "slow", "weakness", "silence" };
                    string debuff = rng.NextElement(debuffs);
                    consumable.effects.Add(new ConsumableEffect
                    {
                        effectType = debuff,
                        valueFloat = baseValue * 0.5f,
                        isLongValue = false,
                        duration = consumable.duration,
                        isPercentage = false
                    });
                    break;

                case ConsumableEffectCategory.Cultivation:
                    // Помощь в культивации
                    consumable.effects.Add(new ConsumableEffect
                    {
                        effectType = "cultivationExp",
                        valueFloat = baseValue,
                        isLongValue = false,
                        duration = 0,
                        isPercentage = false
                    });
                    consumable.effects.Add(new ConsumableEffect
                    {
                        effectType = "qiRegen",
                        valueFloat = 0.2f,
                        isLongValue = false,
                        duration = 120, // 2 часа
                        isPercentage = true
                    });
                    break;

                case ConsumableEffectCategory.Permanent:
                    // Постоянный эффект (очень редкий)
                    string[] permanentEffects = { "maxHP", "maxQi", "strength", "agility", "intelligence" };
                    string permEffect = rng.NextElement(permanentEffects);
                    bool isQiEffect = permEffect == "maxQi";
                    consumable.effects.Add(new ConsumableEffect
                    {
                        effectType = permEffect,
                        valueLong = isQiEffect ? (long)(1 + (int)consumable.rarity) : 0, // FIX GEN-H04: Qi as long (2026-04-11)
                        valueFloat = isQiEffect ? 0f : 1 + (int)consumable.rarity,
                        isLongValue = isQiEffect,
                        duration = 0,
                        isPercentage = false
                    });
                    break;
            }
        }

        private static void GenerateSideEffects(GeneratedConsumable consumable, SeededRandom rng)
        {
            // Шанс побочного эффекта зависит от редкости
            // Common: 50%, Uncommon: 30%, Rare: 15%, Epic: 5%
            consumable.sideEffectChance = consumable.rarity switch
            {
                ItemRarity.Common => 0.50f,
                ItemRarity.Uncommon => 0.30f,
                ItemRarity.Rare => 0.15f,
                ItemRarity.Epic => 0.05f,
                _ => 0f
            };

            if (rng.NextBool(consumable.sideEffectChance))
            {
                string[] sideEffectTypes = { "nausea", "dizziness", "weakness", "qiInstability" };
                string sideEffect = rng.NextElement(sideEffectTypes);

                consumable.sideEffects.Add(new ConsumableEffect
                {
                    effectType = sideEffect,
                    valueFloat = rng.NextFloat(0.1f, 0.3f),
                    isLongValue = false,
                    duration = 30 + rng.Next(30), // 30-60 тиков
                    isPercentage = true
                });
            }
        }

        public static ItemRarity GenerateRarity(SeededRandom rng, int level = 1)
        {
            // Распределение редкости
            // | Common    | 50% |
            // | Uncommon  | 30% |
            // | Rare      | 15% |
            // | Epic      | 4%  |
            // | Legendary | 1%  |
            // | Mythic    | 0.1%|

            float roll = rng.NextFloat();

            if (roll < 0.001f) return ItemRarity.Mythic;
            if (roll < 0.01f) return ItemRarity.Legendary;
            if (roll < 0.05f) return ItemRarity.Epic;
            if (roll < 0.20f) return ItemRarity.Rare;
            if (roll < 0.50f) return ItemRarity.Uncommon;
            return ItemRarity.Common;
        }

        private static string GenerateName(GeneratedConsumable consumable, SeededRandom rng)
        {
            string prefix = "";
            if (PrefixesByEffect.ContainsKey(consumable.effectCategory))
                prefix = rng.NextElement(PrefixesByEffect[consumable.effectCategory]);

            string baseName = "";
            if (NamesByType.ContainsKey(consumable.type))
                baseName = rng.NextElement(NamesByType[consumable.type]);

            // Добавляем уровень для редких+
            string levelSuffix = consumable.rarity >= ItemRarity.Rare ? $" ур.{consumable.itemLevel}" : "";

            return $"{prefix} {baseName}{levelSuffix}".Trim();
        }

        private static string GenerateDescription(GeneratedConsumable consumable)
        {
            var parts = new List<string>();

            parts.Add($"{consumable.nameRu}");

            // Эффекты
            foreach (var effect in consumable.effects)
            {
                string valueStr = effect.isPercentage ? $"{effect.valueFloat * 100:F0}%" : 
                    effect.isLongValue ? effect.valueLong.ToString() : effect.valueFloat.ToString(); // FIX GEN-H04 (2026-04-11)
                string durationStr = effect.duration > 0 ? $" на {effect.duration} тиков" : "";
                parts.Add($"  • {effect.effectType}: {valueStr}{durationStr}");
            }

            // Побочные эффекты
            if (consumable.sideEffects.Count > 0)
            {
                parts.Add($"  ⚠️ Побочные эффекты ({consumable.sideEffectChance * 100:F0}% шанс):");
                foreach (var side in consumable.sideEffects)
                {
                    parts.Add($"    • {side.effectType}");
                }
            }

            // Требования
            if (consumable.requiredCultivationLevel > 0)
                parts.Add($"Требуется уровень культивации: {consumable.requiredCultivationLevel}+");

            if (consumable.maxCultivationLevel < 10)
                parts.Add($"Не эффективен выше уровня {consumable.maxCultivationLevel}");

            parts.Add($"Стоимость: {consumable.value} духовных камней");

            return string.Join("\n", parts);
        }

        /// <summary>
        /// Вывести примеры сгенерированных расходников
        /// </summary>
        public static string GenerateExamples()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== ПРИМЕРЫ СГЕНЕРИРОВАННЫХ РАСХОДНИКОВ ===\n");

            var testCases = new[]
            {
                (ConsumableType.Pill, ConsumableEffectCategory.Healing, 1, ItemRarity.Common),
                (ConsumableType.Pill, ConsumableEffectCategory.Cultivation, 3, ItemRarity.Rare),
                (ConsumableType.Elixir, ConsumableEffectCategory.Buff, 5, ItemRarity.Epic),
                (ConsumableType.Pill, ConsumableEffectCategory.Permanent, 7, ItemRarity.Legendary),
                (ConsumableType.Drink, ConsumableEffectCategory.QiRestoration, 4, ItemRarity.Uncommon),
                (ConsumableType.Poison, ConsumableEffectCategory.Debuff, 6, ItemRarity.Rare),
            };

            var rng = new SeededRandom(12345);

            foreach (var (type, effect, level, rarity) in testCases)
            {
                var parameters = new ConsumableGenerationParams
                {
                    type = type,
                    effectCategory = effect,
                    itemLevel = level,
                    rarity = rarity,
                    seed = rng.Next()
                };

                var consumable = Generate(parameters, new SeededRandom(parameters.seed.Value));

                sb.AppendLine($"{consumable.icon} [{consumable.rarity}] {consumable.nameRu}");
                sb.AppendLine($"  ID: {consumable.id}");
                sb.AppendLine($"  Уровень: {consumable.itemLevel}");
                sb.AppendLine($"  Стоимость: {consumable.value} духовных камней");
                sb.AppendLine($"  Размер: {consumable.sizeWidth}x{consumable.sizeHeight}, Стек: {consumable.maxStack}");

                foreach (var e in consumable.effects)
                {
                    string val = e.isPercentage ? $"{e.valueFloat * 100:F0}%" : e.isLongValue ? e.valueLong.ToString() : e.valueFloat.ToString(); // FIX GEN-H04
                    string dur = e.duration > 0 ? $" ({e.duration} тиков)" : "";
                    sb.AppendLine($"  Эффект: {e.effectType} = {val}{dur}");
                }

                if (consumable.sideEffects.Count > 0)
                {
                    sb.AppendLine($"  ⚠️ Побочные эффекты ({consumable.sideEffectChance * 100:F0}%):");
                    foreach (var s in consumable.sideEffects)
                        sb.AppendLine($"    - {s.effectType}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
