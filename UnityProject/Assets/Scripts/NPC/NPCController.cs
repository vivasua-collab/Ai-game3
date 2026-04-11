// ============================================================================
// NPCController.cs — Главный контроллер NPC
// Cultivation World Simulator
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-11 06:46:00 UTC — Fix-07, NPC-M05: generated.age вместо деривации
// ============================================================================
//
// Источник: docs/NPC_AI_SYSTEM.md, docs/QI_SYSTEM.md
//
// Интеграция с NPCGenerator:
// NPCController.InitializeFromGenerated(GeneratedNPC) — создаёт NPC из генератора
//
// Формула MaxQi (источник: QI_SYSTEM.md §"Ёмкость ядра"):
// coreCapacity = 1000 × 1.1^totalSubLevels
// totalSubLevels = (level - 1) × 10 + subLevel
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;
using CultivationGame.Qi;
using CultivationGame.Body;
using CultivationGame.World;

namespace CultivationGame.NPC
{
    /// <summary>
    /// Главный контроллер NPC — объединяет все системы NPC.
    /// </summary>
    public class NPCController : MonoBehaviour
    {
        [Header("Preset")]
        [SerializeField] private Data.ScriptableObjects.NPCPresetData preset;
        
        [Header("Systems")]
        [SerializeField] private BodyController bodyController;
        [SerializeField] private QiController qiController;
        [SerializeField] private TechniqueController techniqueController;
        [SerializeField] private NPCAI aiController;
        
        [Header("Runtime")]
        [SerializeField] private NPCState state;
        
        // === Relationships ===
        private Dictionary<string, int> relationships = new Dictionary<string, int>();
        private Dictionary<string, string> relationshipFlags = new Dictionary<string, string>();
        
        // FIX NPC-H02: Lifespan tracking
        private float lifespanAccumulator = 0f;
        private TimeController timeController;
        
        // === Events ===
        public event Action<NPCController> OnNPCDeath;
        public event Action<NPCController, CultivationLevel> OnBreakthrough;
        public event Action<NPCController, string, int> OnRelationshipChanged;
        public event Action<NPCController, NPCAIState> OnAIStateChanged;
        
        // === Properties ===
        
        public NPCState State => state;
        public string NpcId => state?.NpcId ?? "";
        public string NpcName => state?.Name ?? "Unknown";
        public CultivationLevel CultivationLevel => state?.CultivationLevel ?? CultivationLevel.None;
        public bool IsAlive => state?.IsAlive ?? false;
        public Data.ScriptableObjects.NPCPresetData Preset => preset;
        
        // FIX NPC-ATT-01: Expose Attitude + PersonalityTrait (2026-04-11)
        public Attitude Attitude => state?.Attitude ?? Attitude.Neutral;
        public PersonalityTrait Personality => state?.Personality ?? PersonalityTrait.None;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            if (state == null)
                state = new NPCState();
                
            InitializeControllers();
        }
        
        private void Start()
        {
            if (preset != null)
                ApplyPreset();
            
            // FIX NPC-H02: Get TimeController for lifespan
            ServiceLocator.Request<TimeController>(tc => timeController = tc);
        }
        
        private void Update()
        {
            if (!IsAlive) return;
            
            UpdateLifespan();
        }
        
        // === Initialization ===
        
        private void InitializeControllers()
        {
            if (bodyController == null)
                bodyController = GetComponent<BodyController>();
            if (qiController == null)
                qiController = GetComponent<QiController>();
            if (techniqueController == null)
                techniqueController = GetComponent<TechniqueController>();
            if (aiController == null)
                aiController = GetComponent<NPCAI>();
        }
        
        /// <summary>
        /// Применить пресет к NPC.
        /// </summary>
        public void ApplyPreset()
        {
            if (preset == null) return;
            
            state.Name = preset.nameTemplate;
            state.CultivationLevel = (CultivationLevel)preset.cultivationLevel;
            state.SubLevel = preset.cultivationSubLevel;
            
            // FIX NPC-ATT-01: Convert baseDisposition int → Attitude enum (2026-04-11)
            state.Attitude = NPCState.ValueToAttitude(preset.baseDisposition);
#pragma warning disable CS0612 // Disposition obsolete
            // Keep legacy Disposition in sync for any remaining references
            state.Disposition = (Disposition)preset.baseDisposition;
#pragma warning restore CS0612
            
            // Инициализируем аффинности элементов (по умолчанию нейтральные)
            state.ElementAffinities = new float[Enum.GetValues(typeof(Element)).Length]; // FIX CORE-H05
            
            // Копируем характеристики
            state.BodyStrength = preset.strength;
            state.Constitution = preset.vitality;
            
            // Рассчитываем ресурсы на основе уровня культивации
            CalculateResourcesFromLevel();
            
            // Инициализируем тело через существующий метод
            if (bodyController != null)
            {
                bodyController.SetCultivationLevel(preset.cultivationLevel);
            }
            
            // Инициализируем Ци через существующий метод
            if (qiController != null)
            {
                qiController.SetCultivationLevel(preset.cultivationLevel, preset.cultivationSubLevel);
            }
        }
        
        private void CalculateResourcesFromLevel()
        {
            state.MaxHealth = CalculateMaxHealth();
            state.CurrentHealth = state.MaxHealth;
            state.MaxQi = CalculateMaxQi();
            state.CurrentQi = state.MaxQi;
            state.MaxStamina = 100f;
            state.CurrentStamina = state.MaxStamina;
            
            // Рассчитываем продолжительность жизни
            state.MaxLifespan = CalculateMaxLifespan();
            state.Lifespan = state.MaxLifespan;
        }
        
        private int CalculateMaxHealth()
        {
            int baseHealth = 100;
            int levelBonus = (int)state.CultivationLevel * 500;
            int subLevelBonus = state.SubLevel * 50;
            return baseHealth + levelBonus + subLevelBonus;
        }
        
        /// <summary>
        /// Рассчитать максимальную Ци.
        /// Источник: docs/QI_SYSTEM.md §"Ёмкость ядра"
        /// Формула: coreCapacity = 1000 × 1.1^totalSubLevels
        /// totalSubLevels = (level - 1) × 10 + subLevel
        /// </summary>
        private long CalculateMaxQi()
        {
            if (state.CultivationLevel == CultivationLevel.None)
            {
                // Смертный: 50-200 Ци
                return 100;
            }
            
            // Формула: 1000 × 1.1^totalSubLevels
            int level = (int)state.CultivationLevel;
            int totalSubLevels = (level - 1) * 10 + state.SubLevel;
            double coreCapacity = 1000 * Math.Pow(1.1, totalSubLevels);
            
            return (long)Math.Round(coreCapacity);
        }
        
        private int CalculateMaxLifespan()
        {
            int baseLifespan = 80;
            int levelBonus = (int)state.CultivationLevel * 100;
            return baseLifespan + levelBonus;
        }
        
        // === Lifespan ===
        
        // FIX NPC-H02: Implement UpdateLifespan — decrease remainingLifespan per game day (2026-04-11)
        /// <summary>
        /// Decrease remaining lifespan based on game time.
        /// 1 game day = 86400 game seconds. Each day reduces lifespan by 1.
        /// </summary>
        private void UpdateLifespan()
        {
            if (timeController == null) return;
            
            double currentGameSeconds = timeController.TotalGameSeconds;
            double currentGameDay = currentGameSeconds / 86400.0;
            
            // Track how many game days have passed since last check
            if (lifespanAccumulator <= 0f)
            {
                lifespanAccumulator = (float)currentGameDay;
                return;
            }
            
            float daysPassed = (float)currentGameDay - lifespanAccumulator;
            if (daysPassed >= 1f)
            {
                int daysToDeduct = (int)daysPassed;
                lifespanAccumulator += daysToDeduct;
                
                state.Lifespan = Mathf.Max(0, state.Lifespan - daysToDeduct);
                
                if (state.Lifespan <= 0)
                {
                    CheckDeathFromAge();
                }
            }
        }
        
        /// <summary>
        /// Проверить, умер ли NPC от старости.
        /// </summary>
        public bool CheckDeathFromAge()
        {
            if (state.Lifespan <= 0)
            {
                Die("old_age");
                return true;
            }
            return false;
        }
        
        // === Death ===
        
        /// <summary>
        /// Убить NPC.
        /// </summary>
        public void Die(string cause = "unknown")
        {
            if (!state.IsAlive) return;
            
            state.IsAlive = false;
            state.CurrentHealth = 0;
            state.CurrentQi = 0;
            
            OnNPCDeath?.Invoke(this);
            
            Debug.Log($"NPC {state.Name} died from {cause}");
        }
        
        // FIX NPC-H03: TakeDamage notifies AI of attacker (2026-04-11)
        /// <summary>
        /// Нанести урон NPC.
        /// </summary>
        /// <param name="damage">Amount of damage</param>
        /// <param name="attackerId">ID of the attacker (used to notify AI)</param>
        public void TakeDamage(int damage, string attackerId = "")
        {
            if (!state.IsAlive) return;
            
            // Уменьшаем здоровье
            state.CurrentHealth -= damage;
            
            // FIX NPC-H03: Notify AI controller of attacker
            if (!string.IsNullOrEmpty(attackerId) && aiController != null)
            {
                // Add threat proportional to damage
                float threatLevel = damage * 0.5f;
                aiController.AddThreat(attackerId, threatLevel);
            }
            
            if (state.CurrentHealth <= 0)
            {
                state.CurrentHealth = 0;
                Die("combat");
            }
        }
        
        // === Relationships ===
        
        /// <summary>
        /// Получить отношение NPC к цели.
        /// </summary>
        public int GetRelationship(string targetId)
        {
            if (relationships.TryGetValue(targetId, out int value))
                return value;
            return 0; // Нейтральное отношение по умолчанию
        }
        
        /// <summary>
        /// Изменить отношение NPC к цели.
        /// </summary>
        public void ModifyRelationship(string targetId, int change)
        {
            int current = GetRelationship(targetId);
            int newValue = Mathf.Clamp(current + change, -100, 100);
            relationships[targetId] = newValue;
            
            OnRelationshipChanged?.Invoke(this, targetId, change);
        }
        
        /// <summary>
        /// Установить флаг отношений.
        /// </summary>
        public void SetRelationshipFlag(string targetId, string flag)
        {
            relationshipFlags[targetId] = flag;
        }
        
        /// <summary>
        /// Проверить наличие флага отношений.
        /// </summary>
        public bool HasRelationshipFlag(string targetId, string flag)
        {
            return relationshipFlags.TryGetValue(targetId, out string f) && f == flag;
        }
        
        // === AI Control ===
        
        /// <summary>
        /// Изменить состояние AI.
        /// </summary>
        public void SetAIState(NPCAIState newState)
        {
            if (state.CurrentAIState == newState) return;
            
            state.CurrentAIState = newState;
            state.StateTimer = 0f;
            
            OnAIStateChanged?.Invoke(this, newState);
        }
        
        /// <summary>
        /// Установить цель для AI.
        /// </summary>
        public void SetTarget(string targetId)
        {
            state.TargetId = targetId;
        }
        
        // === Integration with NPCGenerator ===

        /// <summary>
        /// Инициализировать NPC из сгенерированных данных.
        /// Источник: docs/GENERATORS_SYSTEM.md
        /// </summary>
        /// <param name="generated">Сгенерированный NPC из NPCGenerator</param>
        public void InitializeFromGenerated(Generators.GeneratedNPC generated)
        {
            if (generated == null) return;

            // Identity
            state.NpcId = generated.id;
            state.Name = generated.nameRu;
            
            // FIX NPC-M05: Используем generated.age (2026-04-11)
            state.Age = generated.age > 0 ? generated.age : 18 + generated.cultivationLevel * 5 + UnityEngine.Random.Range(0, 10);

            // Cultivation
            state.CultivationLevel = (CultivationLevel)generated.cultivationLevel;
            state.SubLevel = generated.cultivationSubLevel;

            // Stats
            state.BodyStrength = generated.strength;
            state.Constitution = generated.constitution;
            state.BodyDefense = generated.baseDefense;

            // Mental (генерируем на основе intelligence)
            state.Intelligence = generated.intelligence;
            state.Willpower = generated.intelligence * 0.8f;
            state.Perception = generated.agility * 0.5f + generated.intelligence * 0.5f;
            state.Wisdom = generated.intelligence * 0.6f;

            // FIX NPC-ATT-01: Replace Disposition with Attitude + PersonalityTrait (2026-04-11)
            // Convert GeneratedNPC.baseDisposition (Disposition enum) to Attitude + PersonalityTrait
#pragma warning disable CS0612 // Disposition obsolete
            state.Disposition = generated.baseDisposition;
#pragma warning restore CS0612
            state.Attitude = ConvertDispositionToAttitude(generated.baseDisposition);
            state.Personality = ConvertDispositionToPersonality(generated.baseDisposition);

            // Элементальные аффинности
            state.ElementAffinities = new float[Enum.GetValues(typeof(Element)).Length]; // FIX CORE-H05

            // Ресурсы
            CalculateResourcesFromLevel();

            // Qi (используем формулу из документации)
            state.MaxQi = (long)generated.maxQi;
            state.CurrentQi = (long)generated.currentQi;

            // AI параметры
            if (aiController != null)
            {
                aiController.SetPersonality(
                    generated.aggressionLevel,
                    1f - generated.aggressionLevel,
                    0.5f,
                    0.5f
                );
                // FIX NPC-ATT-01: Apply PersonalityTrait instead of Disposition modifiers
                aiController.ApplyPersonalityModifiers(state.Personality);
            }

            // Инициализируем тело
            if (bodyController != null)
            {
                bodyController.SetCultivationLevel(generated.cultivationLevel);
            }

            // Инициализируем Ци
            if (qiController != null)
            {
                qiController.SetCultivationLevel(generated.cultivationLevel, generated.cultivationSubLevel);
            }

            // Техники (IDs)
            // techniqueController будет использовать techniqueIds для загрузки техник

            Debug.Log($"NPC initialized from generator: {state.Name} (L{generated.cultivationLevel}.{generated.cultivationSubLevel})");
        }

        // FIX NPC-ATT-01: Conversion helpers from legacy Disposition to Attitude + PersonalityTrait (2026-04-11)
        /// <summary>
        /// Convert legacy Disposition enum to Attitude enum.
        /// </summary>
        private static Attitude ConvertDispositionToAttitude(Disposition disposition)
        {
            return disposition switch
            {
                Disposition.Hostile => Attitude.Hostile,
                Disposition.Unfriendly => Attitude.Unfriendly,
                Disposition.Neutral => Attitude.Neutral,
                Disposition.Friendly => Attitude.Friendly,
                Disposition.Allied => Attitude.Allied,
                // Personality-only dispositions map to Neutral attitude
                Disposition.Aggressive => Attitude.Neutral,
                Disposition.Cautious => Attitude.Neutral,
                Disposition.Treacherous => Attitude.Unfriendly,
                Disposition.Ambitious => Attitude.Neutral,
                _ => Attitude.Neutral
            };
        }

        /// <summary>
        /// Convert legacy Disposition enum to PersonalityTrait flags.
        /// </summary>
        private static PersonalityTrait ConvertDispositionToPersonality(Disposition disposition)
        {
            PersonalityTrait trait = PersonalityTrait.None;
            switch (disposition)
            {
                case Disposition.Aggressive:
                    trait |= PersonalityTrait.Aggressive;
                    break;
                case Disposition.Cautious:
                    trait |= PersonalityTrait.Cautious;
                    break;
                case Disposition.Treacherous:
                    trait |= PersonalityTrait.Treacherous;
                    break;
                case Disposition.Ambitious:
                    trait |= PersonalityTrait.Ambitious;
                    break;
                case Disposition.Hostile:
                    trait |= PersonalityTrait.Aggressive;
                    break;
                default:
                    break;
            }
            return trait;
        }

        /// <summary>
        /// Создать и инициализировать NPC из генератора.
        /// Статический метод для удобства.
        /// </summary>
        public static NPCController CreateFromGenerated(
            Generators.GeneratedNPC generated,
            GameObject prefab,
            Vector3 position,
            Transform parent = null)
        {
            if (prefab == null || generated == null) return null;

            GameObject go = GameObject.Instantiate(prefab, position, Quaternion.identity, parent);
            NPCController controller = go.GetComponent<NPCController>();

            if (controller == null)
            {
                controller = go.AddComponent<NPCController>();
            }

            controller.InitializeFromGenerated(generated);
            return controller;
        }

        // === Save/Load ===

        public NPCSaveData GetSaveData()
        {
            return new NPCSaveData
            {
                NpcId = state.NpcId,
                Name = state.Name,
                Age = state.Age,
                CultivationLevel = (int)state.CultivationLevel,
                SubLevel = state.SubLevel,
                CultivationProgress = state.CultivationProgress,
                MortalStage = (int)state.MortalStage,
                DormantCoreProgress = state.DormantCoreProgress,
                CurrentQi = state.CurrentQi,
                CurrentHealth = state.CurrentHealth,
                CurrentStamina = state.CurrentStamina,
                MaxQi = state.MaxQi,
                MaxHealth = state.MaxHealth,           // FIX NPC-C01: int type
                MaxStamina = state.MaxStamina,
                MaxLifespan = state.MaxLifespan,        // FIX NPC-C01: int type
                BodyStrength = state.BodyStrength,
                BodyDefense = state.BodyDefense,
                Constitution = state.Constitution,
                Lifespan = state.Lifespan,
                Willpower = state.Willpower,
                Perception = state.Perception,
                Intelligence = state.Intelligence,
                Wisdom = state.Wisdom,
                // FIX NPC-ATT-04: Save Attitude + PersonalityTrait (2026-04-11)
#pragma warning disable CS0612 // Disposition obsolete
                Disposition = (int)state.Disposition,
#pragma warning restore CS0612
                AttitudeValue = (int)state.Attitude,
                PersonalityFlags = (int)state.Personality,
                ElementAffinities = state.ElementAffinities,
                // FIX NPC-C02: Serialize skill levels as array (2026-04-11)
                SkillLevels = state.GetSkillLevelData().entries,
                IsAlive = state.IsAlive,
                SectId = state.SectId ?? "",
                CurrentLocation = state.CurrentLocation ?? "",
                CurrentAIState = (int)state.CurrentAIState,
                TargetId = state.TargetId ?? ""
            };
        }
        
        public void LoadSaveData(NPCSaveData data)
        {
            state.NpcId = data.NpcId;
            state.Name = data.Name;
            state.Age = data.Age;
            state.CultivationLevel = (CultivationLevel)data.CultivationLevel;
            state.SubLevel = data.SubLevel;
            state.CultivationProgress = data.CultivationProgress;
            state.MortalStage = (MortalStage)data.MortalStage;
            state.DormantCoreProgress = data.DormantCoreProgress;
            state.CurrentQi = data.CurrentQi;
            state.CurrentHealth = data.CurrentHealth;
            state.CurrentStamina = data.CurrentStamina;
            state.MaxQi = data.MaxQi;
            state.MaxHealth = data.MaxHealth;           // FIX NPC-C01: int type
            state.MaxStamina = data.MaxStamina;
            state.MaxLifespan = data.MaxLifespan;        // FIX NPC-C01: int type
            state.BodyStrength = data.BodyStrength;
            state.BodyDefense = data.BodyDefense;
            state.Constitution = data.Constitution;
            state.Lifespan = data.Lifespan;
            state.Willpower = data.Willpower;
            state.Perception = data.Perception;
            state.Intelligence = data.Intelligence;
            state.Wisdom = data.Wisdom;
            // FIX NPC-ATT-04: Load Attitude + PersonalityTrait (2026-04-11)
#pragma warning disable CS0612 // Disposition obsolete
            state.Disposition = (Disposition)data.Disposition;
#pragma warning restore CS0612
            state.Attitude = (Attitude)data.AttitudeValue;
            state.Personality = (PersonalityTrait)data.PersonalityFlags;
            state.ElementAffinities = data.ElementAffinities;
            // FIX NPC-C02: Deserialize skill levels from array (2026-04-11)
            if (data.SkillLevels != null)
            {
                var skillData = new SkillLevelData { entries = data.SkillLevels };
                state.SkillLevels = skillData.ToDictionary();
            }
            else
            {
                state.SkillLevels = new Dictionary<string, float>();
            }
            state.IsAlive = data.IsAlive;
            state.SectId = data.SectId;
            state.CurrentLocation = data.CurrentLocation;
            state.CurrentAIState = (NPCAIState)data.CurrentAIState;
            state.TargetId = data.TargetId;
        }
    }
}
