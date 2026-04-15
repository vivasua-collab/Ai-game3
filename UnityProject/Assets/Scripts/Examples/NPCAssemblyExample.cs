// ============================================================================
// NPCAssemblyExample.cs — Пример сборки NPC
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-31 10:51:29 UTC
// Редактировано: 2026-04-11 08:28:36 UTC — FIX NPC-M02: #pragma warning для устаревшего Disposition
// ============================================================================
//
// Демонстрация полной сборки NPC 6-го уровня:
// - Генерация базового NPC
// - Генерация техник
// - Генерация оружия
// - Генерация брони
// - Генерация расходников
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Generators;

namespace CultivationGame.Examples
{
    /// <summary>
    /// Класс для демонстрации сборки NPC
    /// </summary>
    public static class NPCAssemblyExample
    {
        /// <summary>
        /// Полностью собранный NPC
        /// </summary>
        [Serializable]
        public class AssembledNPC
        {
            // === Identity ===
            public string id;
            public string name;
            public int cultivationLevel;
            public int cultivationSubLevel;
            public CoreQuality coreQuality;
            public NPCCategory category;

            // === Stats ===
            public int vitality;
            public int strength;
            public int agility;
            public int constitution;
            public int intelligence;

            // === Qi ===
            public long maxQi;
            public long currentQi;
            public float conductivity;
            public int qiDensity; // = 2^(level-1) = 32 для L6

            // === Body ===
            public int maxHealth;
            public int currentHealth;
            public List<BodyPartData> bodyParts = new List<BodyPartData>();

            // === Combat ===
            public int baseDamage;
            public int baseDefense;
#pragma warning disable CS0618 // Disposition устарел, используется для обратной совместимости
            public Disposition disposition;
#pragma warning restore CS0618
            public float aggressionLevel;

            // === Equipment ===
            public GeneratedWeapon mainWeapon;
            public GeneratedArmor headArmor;
            public GeneratedArmor torsoArmor;
            public GeneratedArmor armsArmor;
            public GeneratedArmor legsArmor;
            public List<GeneratedArmor> allArmor = new List<GeneratedArmor>();

            // === Techniques ===
            public List<GeneratedTechnique> techniques = new List<GeneratedTechnique>();

            // === Consumables ===
            public List<GeneratedConsumable> consumables = new List<GeneratedConsumable>();

            // === Calculated Stats ===
            public int totalDamage;
            public int totalDefense;
            public long totalQiForBreakthrough;
        }

        [Serializable]
        public class BodyPartData
        {
            public BodyPartType type;
            public int maxRedHP;
            public int maxBlackHP;
            public bool isVital;
        }

        /// <summary>
        /// Сборка полного NPC 6-го уровня
        /// </summary>
        public static AssembledNPC AssembleLevel6Cultivator(int? seed = null)
        {
            int useSeed = seed ?? 12345;
            var rng = new SeededRandom(useSeed);
            var npc = new AssembledNPC();

            // ========================================
            // 1. ГЕНЕРАЦИЯ БАЗОВОГО NPC
            // ========================================
            var npcParams = new NPCGenerationParams
            {
                cultivationLevel = 6,
                role = NPCRole.Cultivator,
                count = 1,
                seed = rng.Next()
            };

            var generatedNPC = NPCGenerator.Generate(npcParams, new SeededRandom(npcParams.seed.Value));

            // Копируем данные
            npc.id = generatedNPC.id;
            npc.name = generatedNPC.nameRu;
            npc.cultivationLevel = generatedNPC.cultivationLevel;
            npc.cultivationSubLevel = generatedNPC.cultivationSubLevel;
            npc.coreQuality = generatedNPC.coreQuality;
            npc.category = generatedNPC.category;
            npc.vitality = generatedNPC.vitality;
            npc.strength = generatedNPC.strength;
            npc.agility = generatedNPC.agility;
            npc.constitution = generatedNPC.constitution;
            npc.intelligence = generatedNPC.intelligence;
            npc.maxQi = generatedNPC.maxQi;
            npc.currentQi = generatedNPC.currentQi;
            npc.conductivity = generatedNPC.conductivity;
            npc.baseDamage = generatedNPC.baseDamage;
            npc.baseDefense = generatedNPC.baseDefense;
#pragma warning disable CS0618 // Disposition устарел
            npc.disposition = generatedNPC.baseDisposition;
#pragma warning restore CS0618
            npc.aggressionLevel = generatedNPC.aggressionLevel;

            // Qi Density = 2^(level-1) = 2^5 = 32
            npc.qiDensity = (int)Math.Pow(2, npc.cultivationLevel - 1);

            // HP
            npc.maxHealth = 100 + npc.cultivationLevel * 500 + npc.cultivationSubLevel * 50;
            npc.currentHealth = npc.maxHealth;

            // Qi для прорыва (×10 от capacity)
            npc.totalQiForBreakthrough = npc.maxQi * 10;

            // Body Parts
            foreach (var bp in generatedNPC.bodyParts)
            {
                npc.bodyParts.Add(new BodyPartData
                {
                    type = bp.partType,
                    maxRedHP = bp.maxRedHP,
                    maxBlackHP = bp.maxBlackHP,
                    isVital = bp.isVital
                });
            }

            // ========================================
            // 2. ГЕНЕРАЦИЯ ТЕХНИК (2-4 техники для L6)
            // ========================================
            int techniqueCount = 3;
            var techniqueTypes = new[] { TechniqueType.Combat, TechniqueType.Defense, TechniqueType.Support };

            for (int i = 0; i < techniqueCount; i++)
            {
                var techParams = new TechniqueGenerationParams
                {
                    type = techniqueTypes[i % techniqueTypes.Length],
                    level = Mathf.Max(2, npc.cultivationLevel - rng.Next(0, 3)), // L2-L6
                    grade = TechniqueGenerator.GenerateGrade(rng),
                    mastery = rng.NextFloat(0, 80),
                    seed = rng.Next()
                };

                var technique = TechniqueGenerator.Generate(techParams, new SeededRandom(techParams.seed.Value));
                npc.techniques.Add(technique);
            }

            // ========================================
            // 3. ГЕНЕРАЦИЯ ОРУЖИЯ
            // ========================================
            // Основное оружие (меч для культиватора)
            var weaponParams = new WeaponGenerationParams
            {
                subtype = WeaponSubtype.Sword,
                itemLevel = npc.cultivationLevel,
                grade = WeaponGenerator.GenerateGrade(rng, npc.cultivationLevel),
                materialTier = Mathf.Clamp((npc.cultivationLevel + 1) / 2, 1, 5), // T3-T4 для L6
                materialCategory = MaterialCategory.Metal,
                seed = rng.Next()
            };
            npc.mainWeapon = WeaponGenerator.Generate(weaponParams, new SeededRandom(weaponParams.seed.Value));

            // ========================================
            // 4. ГЕНЕРАЦИЯ БРОНИ
            // ========================================
            // Набор брони
            var armorTypes = new[] { ArmorSubtype.Head, ArmorSubtype.Torso, ArmorSubtype.Arms, ArmorSubtype.Legs };

            foreach (var armorType in armorTypes)
            {
                var armorParams = new ArmorGenerationParams
                {
                    subtype = armorType,
                    weightClass = rng.NextBool(0.6f) ? ArmorWeightClass.Medium : ArmorWeightClass.Light,
                    itemLevel = npc.cultivationLevel,
                    grade = WeaponGenerator.GenerateGrade(rng, npc.cultivationLevel),
                    materialTier = Mathf.Clamp((npc.cultivationLevel + 1) / 2, 1, 5),
                    materialCategory = rng.NextBool(0.7f) ? MaterialCategory.Metal : MaterialCategory.Leather,
                    seed = rng.Next()
                };

                var armor = ArmorGenerator.Generate(armorParams, new SeededRandom(armorParams.seed.Value));
                npc.allArmor.Add(armor);

                // Распределяем по слотам
                switch (armorType)
                {
                    case ArmorSubtype.Head: npc.headArmor = armor; break;
                    case ArmorSubtype.Torso: npc.torsoArmor = armor; break;
                    case ArmorSubtype.Arms: npc.armsArmor = armor; break;
                    case ArmorSubtype.Legs: npc.legsArmor = armor; break;
                }
            }

            // ========================================
            // 5. ГЕНЕРАЦИЯ РАСХОДНИКОВ
            // ========================================
            // Таблетки лечения
            var pillParams = new ConsumableGenerationParams
            {
                type = ConsumableType.Pill,
                effectCategory = ConsumableEffectCategory.Healing,
                itemLevel = npc.cultivationLevel,
                rarity = ItemRarity.Rare,
                count = 3,
                seed = rng.Next()
            };
            for (int i = 0; i < 3; i++)
            {
                pillParams.seed = rng.Next();
                npc.consumables.Add(ConsumableGenerator.Generate(pillParams, new SeededRandom(pillParams.seed.Value)));
            }

            // Эликсиры Qi
            var elixirParams = new ConsumableGenerationParams
            {
                type = ConsumableType.Elixir,
                effectCategory = ConsumableEffectCategory.QiRestoration,
                itemLevel = npc.cultivationLevel,
                rarity = ItemRarity.Uncommon,
                count = 2,
                seed = rng.Next()
            };
            for (int i = 0; i < 2; i++)
            {
                elixirParams.seed = rng.Next();
                npc.consumables.Add(ConsumableGenerator.Generate(elixirParams, new SeededRandom(elixirParams.seed.Value)));
            }

            // ========================================
            // 6. РАСЧЁТ ИТОГОВЫХ ХАРАКТЕРИСТИК
            // ========================================
            // Урон = базовый + оружие
            npc.totalDamage = npc.baseDamage + (npc.mainWeapon?.baseDamage ?? 0);

            // Защита = базовая + вся броня
            npc.totalDefense = npc.baseDefense;
            foreach (var armor in npc.allArmor)
            {
                npc.totalDefense += armor.armor;
            }

            return npc;
        }

        /// <summary>
        /// Вывести информацию о собранном NPC
        /// </summary>
        public static string PrintNPCInfo(AssembledNPC npc)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("╔══════════════════════════════════════════════════════════════════════════════╗");
            sb.AppendLine($"║  NPC: {npc.name} — Уровень культивации L{npc.cultivationLevel}.{npc.cultivationSubLevel}");
            sb.AppendLine("╠══════════════════════════════════════════════════════════════════════════════╣");

            // Identity
            sb.AppendLine("║ === ИДЕНТИФИКАЦИЯ ===");
            sb.AppendLine($"║ ID: {npc.id}");
            sb.AppendLine($"║ Качество ядра: {npc.coreQuality}");
            sb.AppendLine($"║ Категория: {npc.category}");
#pragma warning disable CS0618 // Disposition устарел
            sb.AppendLine($"║ Характер: {npc.disposition}");
#pragma warning restore CS0618
            sb.AppendLine($"║ Агрессивность: {npc.aggressionLevel:P0}");

            // Stats
            sb.AppendLine("║");
            sb.AppendLine("║ === ХАРАКТЕРИСТИКИ ===");
            sb.AppendLine($"║ Сила: {npc.strength} | Ловкость: {npc.agility} | Телосложение: {npc.constitution}");
            sb.AppendLine($"║ Интеллект: {npc.intelligence} | Живучесть: {npc.vitality}");

            // Qi
            sb.AppendLine("║");
            sb.AppendLine("║ === ЦИ (Qi) ===");
            sb.AppendLine($"║ Макс. Ци: {npc.maxQi:N0}");
            sb.AppendLine($"║ Текущее Ци: {npc.currentQi:N0}");
            sb.AppendLine($"║ Плотность Ци: {npc.qiDensity} (2^{npc.cultivationLevel - 1})");
            sb.AppendLine($"║ Проводимость: {npc.conductivity:F2} ед/сек");
            sb.AppendLine($"║ Для прорыва нужно: {npc.totalQiForBreakthrough:N0} Ци");

            // Body
            sb.AppendLine("║");
            sb.AppendLine("║ === ТЕЛО ===");
            sb.AppendLine($"║ HP: {npc.currentHealth}/{npc.maxHealth}");
            sb.AppendLine($"║ Части тела: {npc.bodyParts.Count}");
            foreach (var bp in npc.bodyParts)
            {
                sb.AppendLine($"║   • {bp.type}: {bp.maxRedHP} HP (vital: {bp.isVital})");
            }

            // Combat
            sb.AppendLine("║");
            sb.AppendLine("║ === БОЕВЫЕ ХАРАКТЕРИСТИКИ ===");
            sb.AppendLine($"║ Итого урон: {npc.totalDamage}");
            sb.AppendLine($"║ Итого защита: {npc.totalDefense}");

            // Weapon
            sb.AppendLine("║");
            sb.AppendLine("║ === ОРУЖИЕ ===");
            if (npc.mainWeapon != null)
            {
                sb.AppendLine($"║ [{npc.mainWeapon.grade}] {npc.mainWeapon.nameRu}");
                sb.AppendLine($"║   Урон: {npc.mainWeapon.baseDamage} ({npc.mainWeapon.damageType})");
                sb.AppendLine($"║   Скорость: {npc.mainWeapon.attackSpeed:F2} | Дальность: {npc.mainWeapon.range:F1}м");
                sb.AppendLine($"║   Крит: {npc.mainWeapon.critChance}% / ×{npc.mainWeapon.critDamage:F1}");
                sb.AppendLine($"║   Прочность: {npc.mainWeapon.currentDurability}/{npc.mainWeapon.maxDurability}");
                sb.AppendLine($"║   Проводимость Ци: {npc.mainWeapon.qiConductivity:F2}");
                if (npc.mainWeapon.bonuses.Count > 0)
                {
                    sb.AppendLine($"║   Бонусы: {string.Join(", ", npc.mainWeapon.bonuses.ConvertAll(b => $"+{b.value * 100:F0}% {b.statName}"))}");
                }
            }

            // Armor
            sb.AppendLine("║");
            sb.AppendLine("║ === БРОНЯ ===");
            foreach (var armor in npc.allArmor)
            {
                sb.AppendLine($"║ [{armor.grade}] {armor.nameRu}");
                sb.AppendLine($"║   Защита: {armor.armor} | Снижение урона: {armor.damageReduction:F0}%");
                sb.AppendLine($"║   Покрытие: {armor.coverage:F0}% | Прочность: {armor.currentDurability}/{armor.maxDurability}");
                if (armor.elementalResistances.Count > 0)
                {
                    sb.AppendLine($"║   Сопротивления: {string.Join(", ", armor.elementalResistances)}");
                }
            }

            // Techniques
            sb.AppendLine("║");
            sb.AppendLine("║ === ТЕХНИКИ ===");
            foreach (var tech in npc.techniques)
            {
                sb.AppendLine($"║ [{tech.grade}] {tech.nameRu} (L{tech.level})");
                sb.AppendLine($"║   Ёмкость: {tech.capacity} | Урон: {tech.baseDamage}");
                sb.AppendLine($"║   Стоимость Ци: {tech.qiCost} | Кулдаун: {tech.cooldown} тиков");
                sb.AppendLine($"║   Элемент: {tech.element} | Эффекты: {string.Join(", ", tech.effects.ConvertAll(e => e.name))}");
                if (tech.isUltimate) sb.AppendLine($"║   ⚡ ULTIMATE-ТЕХНИКА!");
            }

            // Consumables
            sb.AppendLine("║");
            sb.AppendLine("║ === РАСХОДНИКИ ===");
            foreach (var consumable in npc.consumables)
            {
                sb.AppendLine($"║ {consumable.icon} [{consumable.rarity}] {consumable.nameRu}");
                sb.AppendLine($"║   Эффекты: {string.Join(", ", consumable.effects.ConvertAll(e => $"{e.effectType}:{(e.isPercentage ? (e.valueFloat * 100).ToString("F0") + "%" : e.isLongValue ? e.valueLong.ToString() : e.valueFloat.ToString())}"))}");
                if (consumable.sideEffects.Count > 0)
                {
                    sb.AppendLine($"║   ⚠️ Побочные эффекты: {string.Join(", ", consumable.sideEffects.ConvertAll(s => s.effectType))}");
                }
            }

            sb.AppendLine("╚══════════════════════════════════════════════════════════════════════════════╝");

            return sb.ToString();
        }

        /// <summary>
        /// Запуск демонстрации
        /// </summary>
        public static string RunDemo()
        {
            var npc = AssembleLevel6Cultivator(12345);
            return PrintNPCInfo(npc);
        }
    }
}
