// ============================================================================
// NPCController.cs — Главный контроллер NPC
// Cultivation World Simulator
// Версия: 1.1
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-03-31 10:38:00 UTC
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
            
            // Базовое отношение преобразуем в Disposition
            if (preset.baseDisposition <= -50)
                state.Disposition = Disposition.Hostile;
            else if (preset.baseDisposition <= -10)
                state.Disposition = Disposition.Unfriendly;
            else if (preset.baseDisposition < 10)
                state.Disposition = Disposition.Neutral;
            else if (preset.baseDisposition < 50)
                state.Disposition = Disposition.Friendly;
            else
                state.Disposition = Disposition.Allied;
            
            // Инициализируем аффинности элементов (по умолчанию нейтральные)
            state.ElementAffinities = new float[(int)Element.Count];
            
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
        private int CalculateMaxQi()
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
            
            return (int)Math.Round(coreCapacity);
        }
        
        private int CalculateMaxLifespan()
        {
            int baseLifespan = 80;
            int levelBonus = (int)state.CultivationLevel * 100;
            return baseLifespan + levelBonus;
        }
        
        // === Lifespan ===
        
        private void UpdateLifespan()
        {
            // Уменьшаем жизнь каждый игровой день
            // Реальная реализация зависит от игровой системы времени
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
        
        /// <summary>
        /// Нанести урон NPC.
        /// </summary>
        public void TakeDamage(int damage, string attackerId = "")
        {
            if (!state.IsAlive) return;
            
            // Уменьшаем здоровье
            state.CurrentHealth -= damage;
            
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
            state.Age = 18 + UnityEngine.Random.Range(0, 50); // Возраст по умолчанию

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

            // Personality
            state.Disposition = generated.baseDisposition;

            // Элементальные аффинности
            state.ElementAffinities = new float[(int)Element.Count];

            // Ресурсы
            CalculateResourcesFromLevel();

            // Qi (используем формулу из документации)
            state.MaxQi = (int)generated.maxQi;
            state.CurrentQi = (int)generated.currentQi;

            // AI параметры
            if (aiController != null)
            {
                aiController.SetPersonality(
                    generated.aggressionLevel,
                    1f - generated.aggressionLevel,
                    0.5f,
                    0.5f
                );
                aiController.ApplyDispositionModifiers(generated.baseDisposition);
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
                BodyStrength = state.BodyStrength,
                BodyDefense = state.BodyDefense,
                Constitution = state.Constitution,
                Lifespan = state.Lifespan,
                Willpower = state.Willpower,
                Perception = state.Perception,
                Intelligence = state.Intelligence,
                Wisdom = state.Wisdom,
                Disposition = (int)state.Disposition,
                ElementAffinities = state.ElementAffinities,
                SkillLevels = state.SkillLevels,
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
            state.BodyStrength = data.BodyStrength;
            state.BodyDefense = data.BodyDefense;
            state.Constitution = data.Constitution;
            state.Lifespan = data.Lifespan;
            state.Willpower = data.Willpower;
            state.Perception = data.Perception;
            state.Intelligence = data.Intelligence;
            state.Wisdom = data.Wisdom;
            state.Disposition = (Disposition)data.Disposition;
            state.ElementAffinities = data.ElementAffinities;
            state.SkillLevels = data.SkillLevels;
            state.IsAlive = data.IsAlive;
            state.SectId = data.SectId;
            state.CurrentLocation = data.CurrentLocation;
            state.CurrentAIState = (NPCAIState)data.CurrentAIState;
            state.TargetId = data.TargetId;
        }
    }
}
