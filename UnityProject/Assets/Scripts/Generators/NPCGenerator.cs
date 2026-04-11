// ============================================================================
// NPCGenerator.cs — Генератор NPC
// Cultivation World Simulator
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-11 06:46:00 UTC — NPC-M05: добавлено поле age в GeneratedNPC
// ============================================================================
//
// Источник: docs/GENERATORS_SYSTEM.md, docs/ENTITY_TYPES.md, docs/QI_SYSTEM.md
//
// Генерация NPC включает:
// - Species (вид)
// - Cultivation Level (уровень культивации)
// - Core Quality (качество ядра)
// - Stats (характеристики)
// - Body Parts (части тела)
// - Techniques (техники)
//
// Формула MaxQi (источник: QI_SYSTEM.md §"Ёмкость ядра"):
// coreCapacity = 1000 × 1.1^totalSubLevels
// totalSubLevels = (level - 1) × 10 + subLevel
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Generators
{
    /// <summary>
    /// Параметры генерации NPC
    /// </summary>
    [Serializable]
    public class NPCGenerationParams
    {
        public SpeciesData species;             // Вид (null = человек)
        public int cultivationLevel = 0;        // 0 = смертный, 1-10 = практик
        public NPCRole role = NPCRole.Passerby; // Роль
        public string locationId = "";          // Локация спавна
        public int count = 1;                   // Количество
        public int? seed = null;                // Seed генерации
    }

    /// <summary>
    /// Роль NPC
    /// </summary>
    public enum NPCRole
    {
        Monster,        // Монстр
        Guard,          // Охранник
        Merchant,       // Торговец
        Cultivator,     // Культиcтор
        Passerby,       // Прохожий
        Elder,          // Старейшина
        Disciple,       // Ученик
        Enemy           // Враг
    }

    /// <summary>
    /// Результат генерации NPC
    /// </summary>
    [Serializable]
    public class GeneratedNPC
    {
        // Identity
        public string id;
        public string nameRu;
        public string nameEn;
        public NPCRole role;
        public NPCCategory category;
        public int age; // Возраст NPC — NPC-M05 (2026-04-11)
        
        // Species
        public SpeciesData species;
        public SoulType soulType;
        public Morphology morphology;
        public BodyMaterial bodyMaterial;
        
        // Cultivation
        public int cultivationLevel;
        public int cultivationSubLevel;
        public CoreQuality coreQuality;
        
        // Stats
        public int vitality;
        public int strength;
        public int agility;
        public int constitution;
        public int intelligence;
        
        // Qi
        public long maxQi;
        public long currentQi;
        public float conductivity;
        
        // Body
        public List<GeneratedBodyPart> bodyParts = new List<GeneratedBodyPart>();
        
        // Combat
        public int baseDamage;
        public int baseDefense;
        
        // Techniques
        public List<string> techniqueIds = new List<string>();
        
        // Equipment
        public List<string> equipmentIds = new List<string>();
        
        // AI — FIX: Disposition→Attitude+PersonalityTrait (2026-04-11)
#pragma warning disable CS0612 // Disposition obsolete
        public Disposition baseDisposition; // Устарело — для обратной совместимости
#pragma warning restore CS0612
        public Attitude baseAttitude; // Отношение к игроку
        public PersonalityTrait basePersonality; // Характер [Flags]
        public float aggressionLevel;
    }

    [Serializable]
    public class GeneratedBodyPart
    {
        public BodyPartType partType;
        public int maxRedHP;
        public int maxBlackHP;
        public bool isVital;
    }

    /// <summary>
    /// Генератор NPC
    /// </summary>
    public static class NPCGenerator
    {
        // Имена по ролям
        private static readonly Dictionary<NPCRole, string[]> RoleNames = new Dictionary<NPCRole, string[]>
        {
            { NPCRole.Monster, new[] { "Тварь", "Зверь", "Монстр", "Существо", "Чудовище" } },
            { NPCRole.Guard, new[] { "Страж", "Охранник", "Воин", "Защитник", "Караульный" } },
            { NPCRole.Merchant, new[] { "Торговец", "Купец", "Продавец", "Скупщик", "Лавочник" } },
            { NPCRole.Cultivator, new[] { "Практик", "Культиcтор", "Адепт", "Ученик", "Мастер" } },
            { NPCRole.Passerby, new[] { "Прохожий", "Странник", "Путник", "Гость", "Посетитель" } },
            { NPCRole.Elder, new[] { "Старейшина", "Мудрец", "Наставник", "Учитель", "Гуру" } },
            { NPCRole.Disciple, new[] { "Ученик", "Послушник", "Новичок", "Неофит", "Адепт" } },
            { NPCRole.Enemy, new[] { "Враг", "Противник", "Недоброжелатель", "Соперник", "Оппонент" } }
        };

        // Множители статов по уровню культивации
        private static readonly int[] VitalityByLevel = { 10, 20, 40, 80, 160, 320, 640, 1280, 2560, 5000 };
        private static readonly int[] DamageByLevel = { 5, 15, 35, 75, 150, 300, 600, 1200, 2500, 5000 };

        /// <summary>
        /// Сгенерировать одного NPC
        /// </summary>
        public static GeneratedNPC Generate(NPCGenerationParams parameters, SeededRandom rng = null)
        {
            if (rng == null)
                rng = new SeededRandom(parameters.seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));

            var npc = new GeneratedNPC();
            
            // Identity
            npc.id = $"npc_{parameters.role}_{parameters.cultivationLevel}_{rng.Next(10000):D5}";
            npc.role = parameters.role;
            npc.nameRu = GenerateName(npc.role, rng);
            npc.nameEn = npc.nameRu;
            
            // Категория на основе роли
            npc.category = GetCategoryForRole(npc.role);
            
            // Возраст — NPC-M05 (2026-04-11)
            // Базовый возраст зависит от уровня культивации + случайная вариация
            npc.age = 18 + parameters.cultivationLevel * 5 + rng.Next(0, 10);
            
            // Species
            npc.species = parameters.species;
            if (npc.species != null)
            {
                npc.soulType = npc.species.soulType;
                npc.morphology = npc.species.morphology;
                npc.bodyMaterial = npc.species.bodyMaterial;
            }
            else
            {
                // По умолчанию — человек
                npc.soulType = SoulType.Character;
                npc.morphology = Morphology.Humanoid;
                npc.bodyMaterial = BodyMaterial.Organic;
            }
            
            // Cultivation
            npc.cultivationLevel = parameters.cultivationLevel;
            npc.cultivationSubLevel = rng.Next(0, 10);
            npc.coreQuality = GenerateCoreQuality(rng);
            
            // Stats
            int levelIndex = Mathf.Clamp(parameters.cultivationLevel, 0, 9);
            npc.vitality = VitalityByLevel[levelIndex] + rng.Next(-2, 3);
            npc.strength = 8 + parameters.cultivationLevel * 2 + rng.Next(-1, 2);
            npc.agility = 8 + parameters.cultivationLevel * 2 + rng.Next(-1, 2);
            npc.constitution = 8 + parameters.cultivationLevel * 2 + rng.Next(-1, 2);
            npc.intelligence = 8 + parameters.cultivationLevel * 2 + rng.Next(-1, 2);
            
            // Qi (только для практиков)
            // Источник: QI_SYSTEM.md §"Ёмкость ядра"
            // Формула: coreCapacity = 1000 × 1.1^totalSubLevels
            if (parameters.cultivationLevel > 0)
            {
                int totalSubLevels = (parameters.cultivationLevel - 1) * 10 + npc.cultivationSubLevel;
                npc.maxQi = (long)Math.Round(1000 * Math.Pow(1.1, totalSubLevels));
                npc.currentQi = npc.maxQi;
                npc.conductivity = (float)npc.maxQi / 360f; // Формула из QI_SYSTEM.md
            }
            else
            {
                // Смертный
                npc.maxQi = 50 + rng.Next(50, 150);
                npc.currentQi = npc.maxQi;
                npc.conductivity = 0.1f;
            }
            
            // Body Parts (гуманоид по умолчанию)
            GenerateBodyParts(npc, rng);
            
            // Combat
            npc.baseDamage = DamageByLevel[levelIndex] + npc.strength / 2;
            npc.baseDefense = npc.constitution + parameters.cultivationLevel * 5;
            
            // Disposition → Attitude + PersonalityTrait (2026-04-11)
            npc.baseAttitude = GetAttitudeForRole(npc.role, rng);
            npc.basePersonality = GetPersonalityForRole(npc.role, rng);
#pragma warning disable CS0612 // Disposition obsolete — обратная совместимость
            npc.baseDisposition = GetDispositionForRole(npc.role);
#pragma warning restore CS0612
            npc.aggressionLevel = GetAggressionForRole(npc.role, rng);
            
            // Генерация техник для практиков
            if (parameters.cultivationLevel > 0)
            {
                int techniqueCount = Mathf.Min(3, 1 + parameters.cultivationLevel / 3);
                for (int i = 0; i < techniqueCount; i++)
                {
                    var techParams = new TechniqueGenerationParams
                    {
                        level = Mathf.Max(1, parameters.cultivationLevel - rng.Next(0, 2)),
                        grade = TechniqueGenerator.GenerateGrade(rng),
                        type = rng.NextElement(new[] { TechniqueType.Combat, TechniqueType.Defense, TechniqueType.Support }),
                        seed = rng.Next()
                    };
                    var technique = TechniqueGenerator.Generate(techParams, rng);
                    npc.techniqueIds.Add(technique.id);
                }
            }
            
            return npc;
        }

        /// <summary>
        /// Сгенерировать несколько NPC
        /// </summary>
        public static List<GeneratedNPC> GenerateMultiple(NPCGenerationParams parameters)
        {
            var rng = new SeededRandom(parameters.seed ?? UnityEngine.Random.Range(int.MinValue, int.MaxValue));
            var results = new List<GeneratedNPC>();
            
            for (int i = 0; i < parameters.count; i++)
            {
                var npc = Generate(parameters, rng);
                results.Add(npc);
            }
            
            return results;
        }

        /// <summary>
        /// Сгенерировать врага для игрока
        /// </summary>
        public static GeneratedNPC GenerateEnemyForPlayer(int playerLevel, SeededRandom rng = null)
        {
            if (rng == null)
                rng = new SeededRandom();
            
            // Враг на 0-2 уровня ниже или выше игрока
            int enemyLevel = Mathf.Clamp(playerLevel + rng.Next(-2, 3), 0, 10);
            
            var parameters = new NPCGenerationParams
            {
                cultivationLevel = enemyLevel,
                role = rng.NextBool(0.7f) ? NPCRole.Enemy : NPCRole.Monster,
                seed = null
            };
            
            return Generate(parameters, rng);
        }

        // === Helpers ===

        private static NPCCategory GetCategoryForRole(NPCRole role)
        {
            return role switch
            {
                NPCRole.Monster => NPCCategory.Temp,
                NPCRole.Enemy => NPCCategory.Temp,
                NPCRole.Passerby => NPCCategory.Temp,
                NPCRole.Guard => NPCCategory.Plot,
                NPCRole.Merchant => NPCCategory.Plot,
                NPCRole.Cultivator => NPCCategory.Plot,
                NPCRole.Elder => NPCCategory.Unique,
                NPCRole.Disciple => NPCCategory.Plot,
                _ => NPCCategory.Temp
            };
        }

        private static CoreQuality GenerateCoreQuality(SeededRandom rng)
        {
            float roll = rng.NextFloat();
            
            if (roll < 0.05f) return CoreQuality.Fragmented;
            if (roll < 0.15f) return CoreQuality.Cracked;
            if (roll < 0.35f) return CoreQuality.Flawed;
            if (roll < 0.70f) return CoreQuality.Normal;
            if (roll < 0.90f) return CoreQuality.Refined;
            if (roll < 0.98f) return CoreQuality.Perfect;
            return CoreQuality.Transcendent;
        }

        private static string GenerateName(NPCRole role, SeededRandom rng)
        {
            if (RoleNames.TryGetValue(role, out var names))
            {
                string baseName = rng.NextElement(names);
                int number = rng.Next(1, 1000);
                return $"{baseName} #{number}";
            }
            return $"NPC #{rng.Next(10000)}";
        }

        private static void GenerateBodyParts(GeneratedNPC npc, SeededRandom rng)
        {
            // Стандартный гуманоид
            var partConfigs = new[]
            {
                (BodyPartType.Head, 50, true),
                (BodyPartType.Torso, 100, true),
                (BodyPartType.Heart, 30, true),
                (BodyPartType.LeftArm, 40, false),
                (BodyPartType.RightArm, 40, false),
                (BodyPartType.LeftLeg, 50, false),
                (BodyPartType.RightLeg, 50, false),
                (BodyPartType.LeftHand, 20, false),
                (BodyPartType.RightHand, 20, false),
                (BodyPartType.LeftFoot, 20, false),
                (BodyPartType.RightFoot, 20, false)
            };
            
            foreach (var (type, baseHP, vital) in partConfigs)
            {
                int hp = baseHP + npc.vitality;
                npc.bodyParts.Add(new GeneratedBodyPart
                {
                    partType = type,
                    maxRedHP = hp,
                    maxBlackHP = hp,
                    isVital = vital
                });
            }
        }

        /// <summary>
        /// Получить Attitude для роли NPC.
        /// FIX: Замена GetDispositionForRole (2026-04-11)
        /// </summary>
        private static Attitude GetAttitudeForRole(NPCRole role, SeededRandom rng)
        {
            return role switch
            {
                NPCRole.Monster => Attitude.Hostile,
                NPCRole.Guard => Attitude.Neutral,
                NPCRole.Merchant => Attitude.Friendly,
                NPCRole.Cultivator => (Attitude)rng.Next(3, 6), // Neutral..Allied
                NPCRole.Passerby => Attitude.Neutral,
                NPCRole.Elder => Attitude.Friendly,
                NPCRole.Disciple => Attitude.Neutral,
                NPCRole.Enemy => Attitude.Hatred,
                _ => Attitude.Neutral
            };
        }
        
        /// <summary>
        /// Получить PersonalityTrait для роли NPC.
        /// FIX: Новый метод вместо Disposition (2026-04-11)
        /// </summary>
        private static PersonalityTrait GetPersonalityForRole(NPCRole role, SeededRandom rng)
        {
            PersonalityTrait trait = PersonalityTrait.None;
            
            // Базовые черты по роли
            switch (role)
            {
                case NPCRole.Monster:
                    trait |= PersonalityTrait.Aggressive;
                    break;
                case NPCRole.Guard:
                    trait |= PersonalityTrait.Cautious | PersonalityTrait.Loyal;
                    break;
                case NPCRole.Merchant:
                    trait |= PersonalityTrait.Cautious;
                    if (rng.NextFloat() > 0.5f) trait |= PersonalityTrait.Ambitious;
                    break;
                case NPCRole.Cultivator:
                    trait |= PersonalityTrait.Curious;
                    if (rng.NextFloat() > 0.5f) trait |= PersonalityTrait.Ambitious;
                    break;
                case NPCRole.Elder:
                    trait |= PersonalityTrait.Loyal | PersonalityTrait.Cautious;
                    break;
                case NPCRole.Disciple:
                    trait |= PersonalityTrait.Curious;
                    break;
                case NPCRole.Enemy:
                    trait |= PersonalityTrait.Aggressive;
                    if (rng.NextFloat() > 0.5f) trait |= PersonalityTrait.Vengeful;
                    break;
            }
            
            // Случайная дополнительная черта (30% шанс)
            if (rng.NextFloat() > 0.7f)
            {
                var allTraits = new[] { PersonalityTrait.Aggressive, PersonalityTrait.Cautious, 
                    PersonalityTrait.Treacherous, PersonalityTrait.Ambitious, 
                    PersonalityTrait.Loyal, PersonalityTrait.Pacifist, 
                    PersonalityTrait.Curious, PersonalityTrait.Vengeful };
                trait |= allTraits[rng.Next(allTraits.Length)];
            }
            
            return trait;
        }

#pragma warning disable CS0612 // Disposition obsolete — обратная совместимость
        private static Disposition GetDispositionForRole(NPCRole role)
        {
            return role switch
            {
                NPCRole.Monster => Disposition.Aggressive,
                NPCRole.Guard => Disposition.Cautious,
                NPCRole.Merchant => Disposition.Friendly,
                NPCRole.Cultivator => Disposition.Neutral,
                NPCRole.Passerby => Disposition.Neutral,
                NPCRole.Elder => Disposition.Friendly,
                NPCRole.Disciple => Disposition.Neutral,
                NPCRole.Enemy => Disposition.Hostile,
                _ => Disposition.Neutral
            };
        }
#pragma warning restore CS0612 // Disposition obsolete

        private static float GetAggressionForRole(NPCRole role, SeededRandom rng)
        {
            return role switch
            {
                NPCRole.Monster => 0.7f + rng.NextFloat() * 0.3f,
                NPCRole.Enemy => 0.6f + rng.NextFloat() * 0.3f,
                NPCRole.Guard => 0.2f + rng.NextFloat() * 0.2f,
                NPCRole.Merchant => rng.NextFloat() * 0.1f,
                NPCRole.Elder => rng.NextFloat() * 0.1f,
                _ => rng.NextFloat() * 0.3f
            };
        }
    }
}
