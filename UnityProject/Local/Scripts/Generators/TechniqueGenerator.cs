// ============================================================================
// TechniqueGenerator.cs — Генератор техник
// Cultivation World Simulator
// Версия: 1.1
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-03-31 11:04:33 UTC
// ============================================================================
//
// Источник: docs/TECHNIQUE_SYSTEM.md
//
// Ключевые формулы:
// - finalDamage = capacity × gradeMult
// - capacity = baseCapacity(type) × 2^(level-1) × masteryBonus
// - qiDensity = 2^(level-1)
//
// Множители Grade (§"Система Grade"):
// | Grade       | Урон  | Бонусов | Шанс эффекта |
// |-------------|-------|---------|--------------|
// | Common      | ×1.0  | 0       | 0%           |
// | Refined     | ×1.2  | 1       | 20%          |
// | Perfect     | ×1.4  | 2       | 50%          |
// | Transcendent| ×1.6  | 3       | 80%          |
//
// Распределение Grade (§"Распределение Grade"):
// | Grade       | Шанс  |
// |-------------|-------|
// | Common      | 60%   |
// | Refined     | 28%   |
// | Perfect     | 10%   |
// | Transcendent| 2%    |
//
// ⚠️ ВАЖНО: Стоимость Ци всегда ×1.0 — не зависит от Grade!
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Параметры генерации техники
    /// </summary>
    [Serializable]
    public class TechniqueGenerationParams
    {
        public TechniqueType type = TechniqueType.Combat;
        public CombatSubtype combatSubtype = CombatSubtype.MeleeStrike; // Для Combat типа
        public int level = 1;                   // Уровень техники (1-9)
        public TechniqueGrade grade = TechniqueGrade.Common;
        public Element? element = null;         // null = случайный
        public float mastery = 0f;              // Мастерство 0-100%
        public int count = 1;                   // Количество
        public int? seed = null;                // null = случайный
    }

    /// <summary>
    /// Результат генерации техники
    /// </summary>
    [Serializable]
    public class GeneratedTechnique
    {
        public string id;
        public string nameRu;
        public string nameEn;
        public TechniqueType type;
        public CombatSubtype combatSubtype;
        public TechniqueGrade grade;
        public Element element;
        public int level;

        // Capacity (ёмкость техники)
        public int capacity;                    // Базовая ёмкость = baseCapacity × 2^(level-1)
        public float masteryBonus;              // Бонус мастерства (0-50%)

        // Stats
        public int qiCost;                      // Стоимость Ци (×1.0 всегда!)
        public int baseDamage;                  // = capacity × gradeMult
        public int cooldown;                    // В тиках
        public float range;
        public float castTime;

        // Requirements
        public int requiredLevel;               // Минимальный уровень культивации
        public int requiredQiCapacity;

        // Effects
        public List<TechniqueEffect> effects = new List<TechniqueEffect>();
        public int bonusCount;                  // Количество бонусов от Grade
        public float effectChance;              // Шанс эффекта от Grade

        public string description;

        // Ultimate
        public bool isUltimate;
    }

    [Serializable]
    public class TechniqueEffect
    {
        public string name;
        public float value;
        public int duration;    // В тиках (1 тик = 1 минута)
    }

    /// <summary>
    /// Генератор техник культивации
    /// Соответствует документации: docs/TECHNIQUE_SYSTEM.md
    /// </summary>
    public static class TechniqueGenerator
    {
        // === МНОЖИТЕЛИ GRADE (источник: TECHNIQUE_SYSTEM.md §"Система Grade") ===
        // | Grade       | Урон  |
        // |-------------|-------|
        // | Common      | ×1.0  |
        // | Refined     | ×1.2  |
        // | Perfect     | ×1.4  |
        // | Transcendent| ×1.6  |
        private static readonly float[] GradeMultipliers = { 1.0f, 1.2f, 1.4f, 1.6f };

        // === БОНУСЫ ОТ GRADE (источник: TECHNIQUE_SYSTEM.md §"Система Grade") ===
        // | Grade       | Бонусов | Шанс эффекта |
        // |-------------|---------|--------------|
        // | Common      | 0       | 0%           |
        // | Refined     | 1       | 20%          |
        // | Perfect     | 2       | 50%          |
        // | Transcendent| 3       | 80%          |
        private static readonly int[] BonusCountByGrade = { 0, 1, 2, 3 };
        private static readonly float[] EffectChanceByGrade = { 0f, 0.20f, 0.50f, 0.80f };

        // === БАЗОВАЯ ЁМКОСТЬ ПО ТИПАМ (источник: TECHNIQUE_SYSTEM.md §"Структурная ёмкость") ===
        // | Тип техники              | baseCapacity |
        // |--------------------------|--------------|
        // | Formation                | 80           |
        // | Defense                  | 72           |
        // | Support                  | 56           |
        // | Healing                  | 56           |
        // | Combat (melee_strike)    | 64           |
        // | Combat (melee_weapon)    | 48           |
        // | Combat (ranged_*)        | 32           |
        // | Movement                 | 40           |
        // | Curse                    | 40           |
        // | Poison                   | 40           |
        // | Sensory                  | 32           |
        // | Cultivation              | null         |
        private static readonly Dictionary<TechniqueType, int> BaseCapacityByType = new Dictionary<TechniqueType, int>
        {
            { TechniqueType.Formation, 80 },
            { TechniqueType.Defense, 72 },
            { TechniqueType.Support, 56 },
            { TechniqueType.Healing, 56 },
            { TechniqueType.Combat, 64 },      // По умолчанию melee_strike
            { TechniqueType.Movement, 40 },
            { TechniqueType.Curse, 40 },
            { TechniqueType.Poison, 40 },
            { TechniqueType.Sensory, 32 }
            // Cultivation = null (пассивная)
        };

        // Базовая ёмкость для подтипов Combat
        private static readonly Dictionary<CombatSubtype, int> BaseCapacityByCombatSubtype = new Dictionary<CombatSubtype, int>
        {
            { CombatSubtype.MeleeStrike, 64 },
            { CombatSubtype.MeleeWeapon, 48 },
            { CombatSubtype.RangedProjectile, 32 },
            { CombatSubtype.RangedBeam, 32 },
            { CombatSubtype.RangedAoe, 32 }
        };

        // === РАСПРЕДЕЛЕНИЕ GRADE (источник: TECHNIQUE_SYSTEM.md §"Распределение Grade") ===
        // | Grade       | Шанс  |
        // |-------------|-------|
        // | Common      | 60%   |
        // | Refined     | 28%   |
        // | Perfect     | 10%   |
        // | Transcendent| 2%    |
        private static readonly float[] GradeDistribution = { 0.60f, 0.28f, 0.10f, 0.02f };

        // Названия техник по типам
        private static readonly Dictionary<TechniqueType, string[]> TechniqueNames = new Dictionary<TechniqueType, string[]>
        {
            { TechniqueType.Combat, new[] { "Удар", "Атака", "Удар", "Атака", "Удар" } },
            { TechniqueType.Defense, new[] { "Защита", "Блок", "Щит", "Стена", "Барьер" } },
            { TechniqueType.Healing, new[] { "Исцеление", "Восстановление", "Лечение", "Регенерация", "Оздоровление" } },
            { TechniqueType.Support, new[] { "Поддержка", "Усиление", "Помощь", "Бонус", "Прирост" } },
            { TechniqueType.Movement, new[] { "Рывок", "Прыжок", "Перемещение", "Телепорт", "Полёт" } },
            { TechniqueType.Curse, new[] { "Проклятие", "Порча", "Сглаз", "Скверна", "Тьма" } },
            { TechniqueType.Poison, new[] { "Яд", "Токсин", "Отрава", "Зелье", "Мор" } },
            { TechniqueType.Sensory, new[] { "Чувство", "Восприятие", "Обнаружение", "Взгляд", "Сенсор" } },
            { TechniqueType.Formation, new[] { "Формация", "Массив", "Печать", "Узор", "Рунный круг" } },
            { TechniqueType.Cultivation, new[] { "Медитация", "Практика", "Путь", "Сосредоточение", "Духовный покой" } }
        };

        // Префиксы элементов
        private static readonly Dictionary<Element, string> ElementPrefixes = new Dictionary<Element, string>
        {
            { Element.Fire, "Огненный" },
            { Element.Water, "Водяной" },
            { Element.Earth, "Земляной" },
            { Element.Air, "Воздушный" },
            { Element.Lightning, "Громовой" },
            { Element.Void, "Пустотный" },
            { Element.Neutral, "" }
        };

        // Ограничения элементов для типов техник (источник: TECHNIQUE_SYSTEM.md §"Стихии")
        // | Тип техники  | Допустимые стихии              |
        // |--------------|--------------------------------|
        // | Healing      | neutral ТОЛЬКО                 |
        // | Cultivation  | neutral ТОЛЬКО                 |
        // | Poison       | poison ТОЛЬКО (но у нас нет элемента poison)|
        private static readonly Dictionary<TechniqueType, Element[]> AllowedElementsByType = new Dictionary<TechniqueType, Element[]>
        {
            { TechniqueType.Healing, new[] { Element.Neutral } },
            { TechniqueType.Cultivation, new[] { Element.Neutral } },
            // Для остальных - все основные элементы
        };

        /// <summary>
        /// Сгенерировать одну технику
        /// </summary>
        public static GeneratedTechnique Generate(TechniqueGenerationParams parameters, SeededRandom rng = null)
        {
            if (rng == null)
                rng = new SeededRandom(parameters.seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));

            var technique = new GeneratedTechnique();

            // Базовые параметры
            technique.type = parameters.type;
            technique.combatSubtype = parameters.combatSubtype;
            technique.level = Mathf.Clamp(parameters.level, 1, 9);
            technique.grade = parameters.grade;

            // Элемент (с ограничениями по типу)
            technique.element = parameters.element ?? GetRandomElementForType(technique.type, rng);

            // Мастерство (0-100%)
            float mastery = Mathf.Clamp(parameters.mastery, 0f, 100f);
            technique.masteryBonus = mastery * 0.5f / 100f; // 0% → 0.0, 100% → 0.5 (§"Система мастерства")

            // === РАСЧЁТ ЁМКОСТИ (источник: TECHNIQUE_SYSTEM.md §"Структурная ёмкость") ===
            // capacity = baseCapacity(type) × 2^(level-1) × masteryBonus
            int baseCapacity = GetBaseCapacity(technique.type, technique.combatSubtype);

            // Cultivation техники имеют capacity = null (пассивные)
            if (technique.type == TechniqueType.Cultivation)
            {
                technique.capacity = 0; // Пассивная техника
                technique.baseDamage = 0;
                technique.qiCost = 0;
            }
            else
            {
                // Формула: capacity = baseCapacity × 2^(level-1) × (1 + masteryBonus)
                technique.capacity = Mathf.RoundToInt(baseCapacity * Mathf.Pow(2f, technique.level - 1) * (1f + technique.masteryBonus));

                // Grade множитель для урона (НЕ для qiCost!)
                float gradeMult = GetGradeMultiplier(technique.grade);
                technique.baseDamage = Mathf.RoundToInt(technique.capacity * gradeMult);

                // qiCost всегда ×1.0 (не зависит от Grade!)
                // Базовая стоимость примерно 10-20% от capacity
                technique.qiCost = Mathf.RoundToInt(baseCapacity * Mathf.Pow(2f, technique.level - 1) * 0.15f);
            }

            // Кулдаун (в тиках, 1 тик = 1 минута)
            technique.cooldown = rng.NextGaussianInt(5 + technique.level * 2, 2, 5, 30);
            technique.range = Mathf.RoundToInt(2f + technique.level * 0.5f + rng.NextFloat(-0.5f, 0.5f));
            technique.castTime = Mathf.Clamp(rng.NextGaussian(1f, 0.3f), 0.5f, 5f);

            // Требования (источник: TECHNIQUE_SYSTEM.md §"Ограничения по уровню")
            // Минимальный уровень техники: max(1, L(практик) - 4)
            technique.requiredLevel = Mathf.Max(1, technique.level - 4);
            technique.requiredQiCapacity = technique.qiCost * 10;

            // Бонусы от Grade
            technique.bonusCount = GetBonusCount(technique.grade);
            technique.effectChance = GetEffectChance(technique.grade);

            // Ultimate-техника (5% шанс для Transcendent)
            technique.isUltimate = technique.grade == TechniqueGrade.Transcendent && rng.NextBool(0.05f);
            if (technique.isUltimate)
            {
                technique.baseDamage = Mathf.RoundToInt(technique.baseDamage * 1.3f);
                technique.qiCost = Mathf.RoundToInt(technique.qiCost * 1.5f);
            }

            // ID
            technique.id = $"tech_{technique.type}_{technique.element}_{technique.level}_{rng.Next(1000):D3}";
            if (technique.isUltimate)
                technique.id = "ult_" + technique.id;

            // Генерация имени
            string prefix = ElementPrefixes.ContainsKey(technique.element) ? ElementPrefixes[technique.element] : "";
            string baseName = GetTechniqueName(technique.type, rng);

            if (!string.IsNullOrEmpty(prefix))
                technique.nameRu = $"{prefix} {baseName}";
            else
                technique.nameRu = baseName;

            if (technique.isUltimate)
                technique.nameRu = "⚡ " + technique.nameRu;

            technique.nameEn = technique.nameRu; // TODO: переводы

            // Описание
            technique.description = GenerateDescription(technique);

            // Эффекты
            GenerateEffects(technique, rng);

            return technique;
        }

        /// <summary>
        /// Сгенерировать несколько техник
        /// </summary>
        public static List<GeneratedTechnique> GenerateMultiple(TechniqueGenerationParams parameters)
        {
            var rng = new SeededRandom(parameters.seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            var results = new List<GeneratedTechnique>();

            for (int i = 0; i < parameters.count; i++)
            {
                var technique = Generate(parameters, rng);
                results.Add(technique);
            }

            return results;
        }

        /// <summary>
        /// Сгенерировать случайную технику для уровня культивации
        /// </summary>
        public static GeneratedTechnique GenerateForLevel(int cultivationLevel, SeededRandom rng = null)
        {
            if (rng == null)
                rng = new SeededRandom();

            var parameters = new TechniqueGenerationParams
            {
                type = rng.NextElement(new[] {
                    TechniqueType.Combat, TechniqueType.Defense,
                    TechniqueType.Healing, TechniqueType.Support,
                    TechniqueType.Movement, TechniqueType.Curse
                }),
                level = Mathf.Clamp(cultivationLevel, 1, 9),
                grade = GenerateGrade(rng),
                element = null,
                mastery = rng.NextFloat(0, 50), // Случайное мастерство 0-50%
                seed = null
            };

            return Generate(parameters, rng);
        }

        // === Helpers ===

        /// <summary>
        /// Получить базовую ёмкость для типа техники
        /// Источник: TECHNIQUE_SYSTEM.md §"Структурная ёмкость"
        /// </summary>
        private static int GetBaseCapacity(TechniqueType type, CombatSubtype combatSubtype)
        {
            // Для Combat с подтипом
            if (type == TechniqueType.Combat && BaseCapacityByCombatSubtype.ContainsKey(combatSubtype))
                return BaseCapacityByCombatSubtype[combatSubtype];

            // Для остальных типов
            if (BaseCapacityByType.ContainsKey(type))
                return BaseCapacityByType[type];

            return 64; // По умолчанию
        }

        /// <summary>
        /// Множитель Grade для урона
        /// Источник: TECHNIQUE_SYSTEM.md §"Система Grade"
        /// </summary>
        private static float GetGradeMultiplier(TechniqueGrade grade)
        {
            int index = (int)grade;
            if (index >= 0 && index < GradeMultipliers.Length)
                return GradeMultipliers[index];
            return 1.0f;
        }

        /// <summary>
        /// Количество бонусов от Grade
        /// Источник: TECHNIQUE_SYSTEM.md §"Система Grade"
        /// </summary>
        private static int GetBonusCount(TechniqueGrade grade)
        {
            int index = (int)grade;
            if (index >= 0 && index < BonusCountByGrade.Length)
                return BonusCountByGrade[index];
            return 0;
        }

        /// <summary>
        /// Шанс эффекта от Grade
        /// Источник: TECHNIQUE_SYSTEM.md §"Система Grade"
        /// </summary>
        private static float GetEffectChance(TechniqueGrade grade)
        {
            int index = (int)grade;
            if (index >= 0 && index < EffectChanceByGrade.Length)
                return EffectChanceByGrade[index];
            return 0f;
        }

        /// <summary>
        /// Сгенерировать Grade по распределению
        /// Источник: TECHNIQUE_SYSTEM.md §"Распределение Grade"
        /// | Common      | 60%   |
        /// | Refined     | 28%   |
        /// | Perfect     | 10%   |
        /// | Transcendent| 2%    |
        /// </summary>
        public static TechniqueGrade GenerateGrade(SeededRandom rng)
        {
            float roll = rng.NextFloat();
            float cumulative = 0f;

            for (int i = 0; i < GradeDistribution.Length; i++)
            {
                cumulative += GradeDistribution[i];
                if (roll < cumulative)
                    return (TechniqueGrade)i;
            }

            return TechniqueGrade.Common;
        }

        /// <summary>
        /// Получить случайный элемент для типа техники
        /// С учётом ограничений (Healing/Cultivation = neutral только)
        /// </summary>
        private static Element GetRandomElementForType(TechniqueType type, SeededRandom rng)
        {
            if (AllowedElementsByType.ContainsKey(type))
                return rng.NextElement(AllowedElementsByType[type]);

            // Основные элементы (без Neutral для боевых техник обычно)
            return rng.NextElement(new[]
            {
                Element.Fire, Element.Water, Element.Earth,
                Element.Air, Element.Lightning, Element.Void
            });
        }

        private static Element GetRandomElement(SeededRandom rng)
        {
            return rng.NextElement(new[]
            {
                Element.Fire, Element.Water, Element.Earth,
                Element.Air, Element.Lightning, Element.Void
            });
        }

        private static string GetTechniqueName(TechniqueType type, SeededRandom rng)
        {
            if (TechniqueNames.TryGetValue(type, out var names))
            {
                return rng.NextElement(names);
            }
            return "Техника";
        }

        private static string GenerateDescription(GeneratedTechnique technique)
        {
            if (technique.type == TechniqueType.Cultivation)
            {
                return $"{technique.nameRu} уровня {technique.level}. " +
                       $"Пассивная техника культивации. Ускоряет восстановление Ци.";
            }

            string desc = $"{technique.nameRu} уровня {technique.level}. ";

            if (technique.baseDamage > 0)
                desc += $"Наносит {technique.baseDamage} урона, ";

            desc += $"требует {technique.qiCost} Ци. ";

            desc += $"Элемент: {technique.element}. ";

            if (technique.isUltimate)
                desc += "⚡ Ultimate-техника!";

            return desc;
        }

        private static void GenerateEffects(GeneratedTechnique technique, SeededRandom rng)
        {
            // Базовый эффект урона
            if (technique.baseDamage > 0)
            {
                technique.effects.Add(new TechniqueEffect
                {
                    name = "damage",
                    value = technique.baseDamage,
                    duration = 0
                });
            }

            // Дополнительные эффекты на основе элемента и Grade
            if (rng.NextBool(technique.effectChance))
            {
                switch (technique.element)
                {
                    case Element.Fire:
                        if (technique.type == TechniqueType.Combat)
                            technique.effects.Add(new TechniqueEffect { name = "burning", value = technique.baseDamage * 0.1f, duration = 5 });
                        break;
                    case Element.Water:
                        technique.effects.Add(new TechniqueEffect { name = "slow", value = 0.3f, duration = 3 });
                        break;
                    case Element.Earth:
                        technique.effects.Add(new TechniqueEffect { name = "stun", value = 1, duration = 1 });
                        break;
                    case Element.Lightning:
                        technique.effects.Add(new TechniqueEffect { name = "chain", value = 0.5f, duration = 0 });
                        break;
                    case Element.Air:
                        technique.effects.Add(new TechniqueEffect { name = "knockback", value = 2f, duration = 0 });
                        break;
                    case Element.Void:
                        technique.effects.Add(new TechniqueEffect { name = "pierce", value = 0.5f, duration = 0 });
                        break;
                }
            }

            // Healing эффекты (neutral только)
            if (technique.type == TechniqueType.Healing)
            {
                technique.effects.Clear();
                technique.effects.Add(new TechniqueEffect { name = "heal", value = technique.baseDamage, duration = 0 });
            }
        }

        /// <summary>
        /// Вывести примеры сгенерированных техник
        /// </summary>
        public static string GenerateExamples()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== ПРИМЕРЫ СГЕНЕРИРОВАННЫХ ТЕХНИК ===\n");

            // Примеры разных уровней и грейдов
            var testCases = new[]
            {
                (TechniqueType.Combat, 1, TechniqueGrade.Common, Element.Fire),
                (TechniqueType.Combat, 3, TechniqueGrade.Refined, Element.Lightning),
                (TechniqueType.Combat, 5, TechniqueGrade.Perfect, Element.Void),
                (TechniqueType.Combat, 9, TechniqueGrade.Transcendent, Element.Fire),
                (TechniqueType.Defense, 5, TechniqueGrade.Refined, Element.Earth),
                (TechniqueType.Healing, 4, TechniqueGrade.Perfect, Element.Neutral),
                (TechniqueType.Support, 6, TechniqueGrade.Refined, Element.Water),
                (TechniqueType.Movement, 3, TechniqueGrade.Common, Element.Air),
            };

            var rng = new SeededRandom(12345);

            foreach (var (type, level, grade, element) in testCases)
            {
                var parameters = new TechniqueGenerationParams
                {
                    type = type,
                    level = level,
                    grade = grade,
                    element = element,
                    mastery = rng.NextFloat(0, 100),
                    seed = rng.Next()
                };

                var tech = Generate(parameters, new SeededRandom(parameters.seed.Value));

                sb.AppendLine($"[{tech.grade}] {tech.nameRu} (L{tech.level})");
                sb.AppendLine($"  ID: {tech.id}");
                sb.AppendLine($"  Элемент: {tech.element}");
                sb.AppendLine($"  Ёмкость: {tech.capacity}");
                sb.AppendLine($"  Урон: {tech.baseDamage}");
                sb.AppendLine($"  Qi стоимость: {tech.qiCost}");
                sb.AppendLine($"  Кулдаун: {tech.cooldown} тиков");
                sb.AppendLine($"  Требования: уровень {tech.requiredLevel}+");
                sb.AppendLine($"  Эффекты: {string.Join(", ", tech.effects.ConvertAll(e => e.name))}");
                if (tech.isUltimate)
                    sb.AppendLine($"  ⚡ ULTIMATE!");
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
