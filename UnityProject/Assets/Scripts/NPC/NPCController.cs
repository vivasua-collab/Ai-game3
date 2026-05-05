// ============================================================================
// NPCController.cs — Главный контроллер NPC
// Cultivation World Simulator
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-11 06:46:00 UTC — Fix-07, NPC-M05: generated.age вместо деривации
// Редактировано: 2026-04-30 07:48:00 UTC — GAP-4: авторегистрация в WorldController + OnDestroy
// Редактировано: 2026-04-30 09:45:00 UTC — ICombatant: реализация интерфейса, TakeDamage через пайплайн
// Редактировано: 2026-05-04 07:20:00 UTC — ФАЗА 5: CombatAI + CombatTrigger + ITechniqueUser
// Редактировано: 2026-05-07 10:00:00 UTC — ФАЗА 1: EquipmentController → ICombatant связь
// Редактировано: 2026-05-05 09:55:00 UTC — К-12: preset.baseAttitude вместо ValueToAttitude
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
using CultivationGame.Generators;  // Редактировано: 2026-05-01 — NPCRole для save/load
using CultivationGame.Inventory;    // ФАЗА 1: EquipmentController

namespace CultivationGame.NPC
{
    /// <summary>
    /// Главный контроллер NPC — объединяет все системы NPC.
    /// Реализует ICombatant и ITechniqueUser для участия в боевом пайплайне.
    /// </summary>
    public class NPCController : MonoBehaviour, ICombatant, ITechniqueUser
    {
        [Header("Preset")]
        [SerializeField] private Data.ScriptableObjects.NPCPresetData preset;
        
        [Header("Systems")]
        [SerializeField] private BodyController bodyController;
        [SerializeField] private QiController qiController;
        [SerializeField] private TechniqueController techniqueController;
        [SerializeField] private NPCAI aiController;
        [SerializeField] private EquipmentController equipmentController; // ФАЗА 1: связь экипировки с боем
        
        // ФАЗА 5: CombatAI и CombatTrigger для боевых решений
        private Combat.CombatAI combatAI;
        private Combat.CombatTrigger combatTrigger;
        
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
#pragma warning disable CS0067
        public event Action<NPCController, CultivationLevel> OnBreakthrough;
#pragma warning restore CS0067
        public event Action<NPCController, string, int> OnRelationshipChanged;
        public event Action<NPCController, NPCAIState> OnAIStateChanged;
        
        // === ICombatant Events ===
        public event Action OnDeath;
        public event Action<float> OnDamageTaken;
        public event Action<long, long> OnQiChanged;
        
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
        
        // === ICombatant Explicit Implementation ===
        // ФАЗА 1: делегирование боевых бонусов в EquipmentController (2026-05-07)
        
        string ICombatant.Name => NpcName;
        GameObject ICombatant.GameObject => gameObject;
        int ICombatant.CultivationLevel => state != null ? (int)state.CultivationLevel : 0;
        int ICombatant.CultivationSubLevel => state?.SubLevel ?? 0;
        int ICombatant.Strength => (int)(state?.BodyStrength ?? 10);
        int ICombatant.Agility => (int)(state?.Agility ?? 10);
        int ICombatant.Intelligence => (int)(state?.Intelligence ?? 10);
        int ICombatant.Vitality => (int)(state?.Constitution ?? 10);
        long ICombatant.CurrentQi => state?.CurrentQi ?? 0;
        long ICombatant.MaxQi => state?.MaxQi ?? 0;
        float ICombatant.QiDensity => qiController?.QiDensity ?? 1f;
        QiDefenseType ICombatant.QiDefense => techniqueController != null && techniqueController.HasActiveShield() ? QiDefenseType.Shield : (qiController != null ? qiController.QiDefense : QiDefenseType.RawQi);
        bool ICombatant.HasShieldTechnique => techniqueController != null && techniqueController.HasDefensiveTechnique();
        BodyMaterial ICombatant.BodyMaterial => bodyController?.BodyMaterial ?? BodyMaterial.Organic;
        float ICombatant.HealthPercent => state != null && state.MaxHealth > 0
            ? (float)state.CurrentHealth / state.MaxHealth
            : 0f;
        bool ICombatant.IsAlive => IsAlive;
        int ICombatant.Penetration => 0;
        // ФАЗА 1: бонусы из EquipmentController (исправлен баг: Agility вместо BodyStrength для Dodge/Parry)
        float ICombatant.DodgeChance => DefenseProcessor.CalculateDodgeChance(
            (int)(state?.Agility ?? 10), equipmentController?.GetDodgePenalty() ?? 0f);
        float ICombatant.ParryChance => DefenseProcessor.CalculateParryChance(
            (int)(state?.Agility ?? 10), equipmentController?.GetParryBonus() ?? 0f);
        float ICombatant.BlockChance => DefenseProcessor.CalculateBlockChance(
            (int)(state?.BodyStrength ?? 10), equipmentController?.GetBlockBonus() ?? 0f);
        float ICombatant.ArmorCoverage => equipmentController?.GetArmorCoverage() ?? 0f;
        float ICombatant.DamageReduction => equipmentController?.GetDamageReduction() ?? 0f;
        int ICombatant.ArmorValue => equipmentController?.GetArmorValue() ?? 0;
        
        // === ICombatant Method Implementations ===
        
        /// <summary>
        /// Нанести урон указанной части тела через пайплайн боевой системы.
        /// </summary>
        void ICombatant.TakeDamage(BodyPartType part, float damage)
        {
            if (!IsAlive) return;
            
            if (bodyController != null)
            {
                bodyController.TakeDamage(part, damage);
            }
            else
            {
                // Fallback: прямой урон по HP (без BodyController)
                state.CurrentHealth -= (int)damage;
            }
            
            OnDamageTaken?.Invoke(damage);
            
            // Обновляем HealthPercent в state
            SyncHealthFromBody();
            
            // Проверяем смерть
            if (!CheckAlive())
            {
                Die("combat");
            }
        }
        
        /// <summary>
        /// Нанести урон в случайную часть тела.
        /// </summary>
        void ICombatant.TakeDamageRandom(float damage)
        {
            if (!IsAlive) return;
            
            if (bodyController != null)
            {
                bodyController.TakeDamageRandom(damage);
            }
            else
            {
                state.CurrentHealth -= (int)damage;
            }
            
            OnDamageTaken?.Invoke(damage);
            SyncHealthFromBody();
            
            if (!CheckAlive())
            {
                Die("combat");
            }
        }
        
        bool ICombatant.SpendQi(long amount)
        {
            bool result = qiController?.SpendQi(amount) ?? false;
            if (result && state != null)
            {
                state.CurrentQi = qiController.CurrentQi;
                OnQiChanged?.Invoke(state.CurrentQi, state.MaxQi);
            }
            return result;
        }
        
        void ICombatant.AddQi(long amount)
        {
            qiController?.AddQi(amount);
            if (state != null && qiController != null)
            {
                state.CurrentQi = qiController.CurrentQi;
                OnQiChanged?.Invoke(state.CurrentQi, state.MaxQi);
            }
        }
        
        AttackerParams ICombatant.GetAttackerParams(Element attackElement)
        {
            return new AttackerParams
            {
                CultivationLevel = (int)(state?.CultivationLevel ?? Core.CultivationLevel.None),
                Strength = (int)(state?.BodyStrength ?? 10),
                Agility = (int)(state?.Agility ?? 10),
                Intelligence = (int)(state?.Intelligence ?? 10),
                Penetration = 0,
                AttackElement = attackElement,
                // FIX ИСП-БЛ-02: MeleeWeapon если есть оружие, иначе MeleeStrike
                CombatSubtype = equipmentController?.GetMainWeapon() != null
                    ? CombatSubtype.MeleeWeapon : CombatSubtype.MeleeStrike,
                TechniqueLevel = 1,
                TechniqueGrade = TechniqueGrade.Common,
                IsUltimate = false,
                IsQiTechnique = false,
                WeaponBonusDamage = equipmentController?.GetWeaponBonusDamage() ?? 0f,  // ФАЗА 1: из EquipmentController
                // FIX С-02: Поля для полной формулы урона оружия (EQUIPMENT_SYSTEM.md §7.3)
                WeaponDamage = equipmentController?.GetWeaponDamage() ?? 0f,
                StrBonusRatio = 0.5f,
                AgiBonusRatio = 0.3f
            };
        }
        
        DefenderParams ICombatant.GetDefenderParams()
        {
            return new DefenderParams
            {
                CultivationLevel = (int)(state?.CultivationLevel ?? Core.CultivationLevel.None),
                CurrentQi = state?.CurrentQi ?? 0,
                QiDefense = qiController != null ? qiController.QiDefense : QiDefenseType.RawQi,
                Agility = (int)(state?.Agility ?? 10),
                Strength = (int)(state?.BodyStrength ?? 10),
                ArmorCoverage = equipmentController?.GetArmorCoverage() ?? 0f,
                DamageReduction = equipmentController?.GetDamageReduction() ?? 0f,
                ArmorValue = equipmentController?.GetArmorValue() ?? 0,
                DodgePenalty = equipmentController?.GetDodgePenalty() ?? 0f,
                ParryBonus = equipmentController?.GetParryBonus() ?? 0f,
                BlockBonus = equipmentController?.GetBlockBonus() ?? 0f,
                // В-01: Эффективность из экипировки (было захардкожено 0.5 / 0.7)
                BlockEffectiveness = equipmentController?.GetBlockEffectiveness() ?? 0.5f,
                ShieldEffectiveness = equipmentController?.GetShieldEffectiveness() ?? 0.7f,
                BodyMaterial = bodyController?.BodyMaterial ?? BodyMaterial.Organic,
                DefenderElement = Element.Neutral,
                FormationBuffMultiplier = 1.0f  // TODO: из FormationSystem
            };
        }
        
        // === ITechniqueUser Implementation (ФАЗА 5) ===
        
        /// <summary>TechniqueController для ITechniqueUser</summary>
        TechniqueController ITechniqueUser.TechniqueController => techniqueController;
        
        /// <summary>Можно ли использовать технику?</summary>
        bool ITechniqueUser.CanUseTechnique(LearnedTechnique technique)
        {
            return techniqueController != null && techniqueController.CanUseTechnique(technique);
        }
        
        /// <summary>Использовать технику через TechniqueController</summary>
        TechniqueUseResult ITechniqueUser.UseTechnique(LearnedTechnique technique)
        {
            if (techniqueController == null)
                return new TechniqueUseResult { Success = false, FailReason = "No TechniqueController" };
            return techniqueController.UseTechnique(technique);
        }
        
        /// <summary>
        /// CombatAI для принятия боевых решений (ФАЗА 5).
        /// Инициализируется в InitializeCombatComponents().
        /// </summary>
        public Combat.CombatAI CombatAI => combatAI;
        
        /// <summary>
        /// CombatTrigger для автоматического агра (ФАЗА 5).
        /// </summary>
        public Combat.CombatTrigger CombatTrigger => combatTrigger;
        
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
            
            // Подписка на BodyController.OnDeath для автоматической смерти NPC
            if (bodyController != null)
            {
                bodyController.OnDeath += OnBodyDeath;
            }
            
            // GAP-4: Авторегистрация в WorldController
            // ServiceLocator.Request<WorldController> может не сработать (регистрация закомментирована),
            // поэтому используем FindFirstObjectByType как fallback
            var wc = FindFirstObjectByType<WorldController>();
            if (wc != null)
                wc.RegisterNPC(this);
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
            // ФАЗА 1: EquipmentController — может отсутствовать (опциональная система)
            if (equipmentController == null)
                equipmentController = GetComponent<EquipmentController>();
            
            // ФАЗА 5: Инициализация боевых компонентов
            InitializeCombatComponents();
        }
        
        /// <summary>
        /// Инициализация боевых компонентов: CombatAI + CombatTrigger (ФАЗА 5).
        /// CombatAI — для принятия боевых решений (какую технику накачать).
        /// CombatTrigger — для автоматического агра при контакте.
        /// </summary>
        private void InitializeCombatComponents()
        {
            // CombatAI — если нет, добавляем автоматически
            combatAI = GetComponent<Combat.CombatAI>();
            if (combatAI == null && techniqueController != null)
            {
                combatAI = gameObject.AddComponent<Combat.CombatAI>();
                combatAI.Init(this, techniqueController);
                Debug.Log($"[NPCController] CombatAI добавлен для {NpcName}");
            }
            else if (combatAI != null && techniqueController != null)
            {
                combatAI.Init(this, techniqueController);
            }
            
            // CombatTrigger — если нет, добавляем автоматически (только для враждебных NPC)
            combatTrigger = GetComponent<Combat.CombatTrigger>();
            if (combatTrigger == null && Attitude <= Attitude.Unfriendly)
            {
                combatTrigger = gameObject.AddComponent<Combat.CombatTrigger>();
                combatTrigger.SetOwner(this);
                Debug.Log($"[NPCController] CombatTrigger добавлен для {NpcName} (Attitude={Attitude})");
            }
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
            
            // К-12: Используем baseAttitude напрямую из пресета вместо конвертации baseDisposition
            state.Attitude = preset.baseAttitude;
            // Обратно совместимая конвертация если baseAttitude = Neutral (default) и baseDisposition != 0
#pragma warning disable CS0618 // Disposition/baseDisposition obsolete
            if (preset.baseAttitude == Attitude.Neutral && preset.baseDisposition != 0)
            {
                state.Attitude = NPCState.ValueToAttitude(preset.baseDisposition);
            }
            // Keep legacy Disposition in sync for any remaining references
            state.Disposition = (Disposition)preset.baseDisposition;
#pragma warning restore CS0618
            
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
            OnDeath?.Invoke(); // ICombatant event
            
            // FIX ИСП-БЛ-04: Удаление мёртвых NPC из сцены
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false; // Перестать блокировать
            // Отключить визуал если есть
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                // Делаем полупрозрачным на 3 секунды, потом уничтожаем
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.4f);
            }
            Destroy(gameObject, 3f);
            
            Debug.Log($"NPC {state.Name} died from {cause}");
        }
        
        // FIX NPC-H03: TakeDamage notifies AI of attacker (2026-04-11)
        // Редактировано: 2026-04-30 — Помечен Obsolete, используйте ICombatant.TakeDamage()
        /// <summary>
        /// Нанести урон NPC (устаревший метод).
        /// Для боевого пайплайна используйте ICombatant.TakeDamage(BodyPartType, float).
        /// Этот метод оставлен для обратной совместимости.
        /// </summary>
        [Obsolete("Use ICombatant.TakeDamage(BodyPartType, float) through CombatManager.")]
        public void TakeDamage(int damage, string attackerId = "")
        {
            if (!state.IsAlive) return;
            
            // Направляем через пайплайн, если BodyController доступен
            if (bodyController != null)
            {
                bodyController.TakeDamageRandom(damage);
                SyncHealthFromBody();
            }
            else
            {
                state.CurrentHealth -= damage;
            }
            
            // FIX NPC-H03: Notify AI controller of attacker
            if (!string.IsNullOrEmpty(attackerId) && aiController != null)
            {
                float threatLevel = damage * 0.5f;
                aiController.AddThreat(attackerId, threatLevel);
            }
            
            OnDamageTaken?.Invoke(damage);
            
            if (!CheckAlive())
            {
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
            // Редактировано: 2026-05-01 — Инициализация Agility из генератора
            state.Agility = generated.agility;

            // Mental (генерируем на основе intelligence)
            state.Intelligence = generated.intelligence;
            state.Willpower = generated.intelligence * 0.8f;
            state.Perception = generated.agility * 0.5f + generated.intelligence * 0.5f;
            state.Wisdom = generated.intelligence * 0.6f;

            // FIX NPC-ATT-01: Replace Disposition with Attitude + PersonalityTrait (2026-04-11)
            // Convert GeneratedNPC.baseDisposition (Disposition enum) to Attitude + PersonalityTrait
#pragma warning disable CS0618 // Disposition obsolete — обратная совместимость
            state.Disposition = generated.baseDisposition;
            // FIX: Используем новые поля Attitude+PersonalityTrait из генератора (2026-04-11)
            state.Attitude = generated.baseAttitude != default ? generated.baseAttitude : ConvertDispositionToAttitude(generated.baseDisposition);
            state.Personality = generated.basePersonality != PersonalityTrait.None ? generated.basePersonality : ConvertDispositionToPersonality(generated.baseDisposition);
#pragma warning restore CS0618

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

            // ФАЗА 6: Инициализация экипировки и техник из пресета
            InitializeEquipmentFromPreset();
            InitializeTechniquesFromPreset();

            Debug.Log($"NPC initialized from generator: {state.Name} (L{generated.cultivationLevel}.{generated.cultivationSubLevel})");
        }

        // FIX NPC-ATT-01: Conversion helpers from legacy Disposition to Attitude + PersonalityTrait (2026-04-11)
        /// <summary>
        /// Convert legacy Disposition enum to Attitude enum.

        // === ФАЗА 6: Инициализация экипировки и техник NPC ===

        /// <summary>
        /// Инициализировать экипировку NPC из NPCPresetData.equipment.
        /// NPCPresetData уже имеет List<EquippedItem> с slot, itemId, grade, durabilityPercent.
        /// </summary>
        private void InitializeEquipmentFromPreset()
        {
            if (preset == null || equipmentController == null) return;
            if (preset.equipment == null || preset.equipment.Count == 0) return;

            foreach (var item in preset.equipment)
            {
                if (string.IsNullOrEmpty(item.itemId)) continue;

                // Загружаем EquipmentData из Resources
                var equipmentData = Resources.Load<CultivationGame.Data.ScriptableObjects.EquipmentData>(
                    $"Equipment/{item.itemId}");

                if (equipmentData != null)
                {
                    int durability = item.durabilityPercent > 0
                        ? Mathf.RoundToInt(equipmentData.maxDurability * item.durabilityPercent / 100f)
                        : -1;

                    equipmentController.Equip(equipmentData, item.grade, durability);
                    Debug.Log($"[NPCController] Экипировано: {equipmentData.nameRu} → {item.slot} ({item.grade})");
                }
                else
                {
                    Debug.LogWarning($"[NPCController] EquipmentData не найдена: Equipment/{item.itemId}");
                }
            }
        }

        /// <summary>
        /// Инициализировать техники NPC из NPCPresetData.knownTechniques.
        /// NPCPresetData уже имеет List<KnownTechnique> с techniqueId, mastery, quickSlot.
        /// </summary>
        private void InitializeTechniquesFromPreset()
        {
            if (preset == null || techniqueController == null) return;
            if (preset.knownTechniques == null || preset.knownTechniques.Count == 0) return;

            foreach (var known in preset.knownTechniques)
            {
                if (string.IsNullOrEmpty(known.techniqueId)) continue;

                // Загружаем TechniqueData из Resources
                var techData = Resources.Load<CultivationGame.Data.ScriptableObjects.TechniqueData>(
                    $"Techniques/{known.techniqueId}");

                if (techData != null)
                {
                    techniqueController.LearnTechnique(techData, known.mastery);
                    if (known.quickSlot >= 0 && known.quickSlot < 9)
                    {
                        var learned = techniqueController.GetTechnique(known.techniqueId);
                        if (learned != null)
                            techniqueController.AssignToQuickSlot(learned, known.quickSlot);
                    }
                    Debug.Log($"[NPCController] Изучена техника: {techData.nameRu} (mastery={known.mastery:F0}%, slot={known.quickSlot})");
                }
                else
                {
                    Debug.LogWarning($"[NPCController] TechniqueData не найдена: Techniques/{known.techniqueId}");
                }
            }
        }

        /// <summary>
        /// Convert legacy Disposition enum to Attitude enum.
        /// </summary>
#pragma warning disable CS0618 // Disposition obsolete
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
#pragma warning restore CS0618

        /// <summary>
        /// Convert legacy Disposition enum to PersonalityTrait flags.
        /// </summary>
#pragma warning disable CS0618 // Disposition obsolete
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
#pragma warning restore CS0618

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

        // === Health Sync ===
        
        /// <summary>
        /// Синхронизировать HP из BodyController в NPCState.
        /// BodyController — источник истины для HP, но NPCState хранит CurrentHealth.
        /// </summary>
        private void SyncHealthFromBody()
        {
            if (bodyController != null && state != null)
            {
                state.CurrentHealth = (int)(bodyController.HealthPercent * state.MaxHealth);
            }
        }
        
        /// <summary>
        /// Проверить, жив ли NPC (по BodyController если есть, иначе по state).
        /// </summary>
        private bool CheckAlive()
        {
            if (bodyController != null)
                return bodyController.IsAlive;
            return state.CurrentHealth > 0;
        }
        
        // GAP-4: Отписка от WorldController при уничтожении
        private void OnDestroy()
        {
            var wc = FindFirstObjectByType<WorldController>();
            if (wc != null)
                wc.UnregisterNPC(NpcId);
            
            // Отписка от BodyController events
            if (bodyController != null)
            {
                bodyController.OnDeath -= OnBodyDeath;
            }
        }
        
        /// <summary>
        /// Обработчик смерти тела — вызывает NPC Die().
        /// </summary>
        private void OnBodyDeath()
        {
            if (state.IsAlive)
                Die("combat");
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
                Agility = state.Agility,  // Редактировано: 2026-05-01 — Agility save
                Lifespan = state.Lifespan,
                Willpower = state.Willpower,
                Perception = state.Perception,
                Intelligence = state.Intelligence,
                Wisdom = state.Wisdom,
                // FIX NPC-ATT-04: Save Attitude + PersonalityTrait (2026-04-11)
#pragma warning disable CS0618 // Disposition obsolete
                Disposition = (int)state.Disposition,
#pragma warning restore CS0618
                AttitudeValue = (int)state.Attitude,
                PersonalityFlags = (int)state.Personality,
                ElementAffinities = state.ElementAffinities,
                // FIX NPC-C02: Serialize skill levels as array (2026-04-11)
                SkillLevels = state.GetSkillLevelData().entries,
                IsAlive = state.IsAlive,
                SectId = state.SectId ?? "",
                CurrentLocation = state.CurrentLocation ?? "",
                CurrentAIState = (int)state.CurrentAIState,
                TargetId = state.TargetId ?? "",
                RoleValue = (int)state.Role,  // Редактировано: 2026-05-01 — NPCRole save
                // ФАЗА 7: Сохранение экипировки и техник NPC
                EquipmentSlots = GetEquipmentSaveData(),
                TechniqueSlots = GetTechniqueSaveData()
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
            state.Agility = data.Agility;  // Редактировано: 2026-05-01 — Agility load
            state.Lifespan = data.Lifespan;
            state.Willpower = data.Willpower;
            state.Perception = data.Perception;
            state.Intelligence = data.Intelligence;
            state.Wisdom = data.Wisdom;
            // FIX NPC-ATT-04: Load Attitude + PersonalityTrait (2026-04-11)
#pragma warning disable CS0618 // Disposition obsolete
            state.Disposition = (Disposition)data.Disposition;
#pragma warning restore CS0618
            state.Attitude = (Attitude)data.AttitudeValue;
            state.Personality = (PersonalityTrait)data.PersonalityFlags;
            state.ElementAffinities = data.ElementAffinities;
            // FIX NPC-C02: Deserialize skill levels from array (2026-04-11)
#pragma warning disable CS0618 // SkillLevels устарел, но нужен для обратной совместимости
            if (data.SkillLevels != null)
            {
                var skillData = new SkillLevelData { entries = data.SkillLevels };
                state.SkillLevels = skillData.ToDictionary();
            }
            else
            {
                state.SkillLevels = new Dictionary<string, float>();
            }
#pragma warning restore CS0618
            state.IsAlive = data.IsAlive;
            state.SectId = data.SectId;
            state.CurrentLocation = data.CurrentLocation;
            state.CurrentAIState = (NPCAIState)data.CurrentAIState;
            state.TargetId = data.TargetId;
            state.Role = (NPCRole)data.RoleValue;  // Редактировано: 2026-05-01 — NPCRole load

            // ФАЗА 7: Загрузка экипировки и техник NPC
            LoadEquipmentSaveData(data.EquipmentSlots);
            LoadTechniqueSaveData(data.TechniqueSlots);
        }

        // === ФАЗА 7: Save/Load helpers для экипировки и техник ===

        /// <summary>
        /// Получить сериализуемые данные экипировки NPC.
        /// </summary>
        private NPCEquipmentSaveData[] GetEquipmentSaveData()
        {
            if (equipmentController == null) return Array.Empty<NPCEquipmentSaveData>();

            var items = equipmentController.GetAllEquipped();
            var result = new NPCEquipmentSaveData[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                result[i] = new NPCEquipmentSaveData
                {
                    Slot = (int)items[i].Slot,
                    ItemId = items[i].ItemId,
                    Grade = (int)items[i].grade,
                    Durability = items[i].durability
                };
            }
            return result;
        }

        /// <summary>
        /// Получить сериализуемые данные техник NPC.
        /// </summary>
        private NPCTechniqueSlotSaveData[] GetTechniqueSaveData()
        {
            if (techniqueController == null) return Array.Empty<NPCTechniqueSlotSaveData>();

            var techs = techniqueController.Techniques;
            var result = new NPCTechniqueSlotSaveData[techs.Count];
            for (int i = 0; i < techs.Count; i++)
            {
                result[i] = new NPCTechniqueSlotSaveData
                {
                    TechniqueId = techs[i].Data?.techniqueId ?? "",
                    Mastery = techs[i].Mastery,
                    QuickSlot = techs[i].QuickSlot
                };
            }
            return result;
        }

        /// <summary>
        /// Загрузить экипировку NPC из сохранения.
        /// </summary>
        private void LoadEquipmentSaveData(NPCEquipmentSaveData[] data)
        {
            if (data == null || equipmentController == null) return;

            equipmentController.UnequipAll();
            foreach (var entry in data)
            {
                if (string.IsNullOrEmpty(entry.ItemId)) continue;

                var eqData = Resources.Load<CultivationGame.Data.ScriptableObjects.EquipmentData>(
                    $"Equipment/{entry.ItemId}");
                if (eqData != null)
                {
                    equipmentController.Equip(eqData, (CultivationGame.Core.EquipmentGrade)entry.Grade, entry.Durability);
                }
            }
        }

        /// <summary>
        /// Загрузить техники NPC из сохранения.
        /// </summary>
        private void LoadTechniqueSaveData(NPCTechniqueSlotSaveData[] data)
        {
            if (data == null || techniqueController == null) return;

            foreach (var entry in data)
            {
                if (string.IsNullOrEmpty(entry.TechniqueId)) continue;

                var techData = Resources.Load<CultivationGame.Data.ScriptableObjects.TechniqueData>(
                    $"Techniques/{entry.TechniqueId}");
                if (techData != null)
                {
                    techniqueController.LearnTechnique(techData, entry.Mastery);
                    if (entry.QuickSlot >= 0 && entry.QuickSlot < 9)
                    {
                        var learned = techniqueController.GetTechnique(entry.TechniqueId);
                        if (learned != null)
                            techniqueController.AssignToQuickSlot(learned, entry.QuickSlot);
                    }
                }
            }
        }
    }
}
