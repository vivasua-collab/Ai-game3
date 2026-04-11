// ============================================================================
// PlayerController.cs — Главный контроллер игрока
// Cultivation World Simulator
// Версия: 1.2 — Заменён FindFirstObjectByType на ServiceLocator
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-09 10:35:00 UTC
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 1.2:
// - FIX: FindFirstObjectByType заменён на ServiceLocator.GetOrFind (аудит Unity 6.3)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;
using CultivationGame.Qi;
using CultivationGame.Body;
using CultivationGame.NPC;
using CultivationGame.Interaction;
using CultivationGame.World;
using CultivationGame.Save;
using UnityEngine.InputSystem;

namespace CultivationGame.Player
{
    /// <summary>
    /// Главный контроллер игрока — объединяет все системы персонажа.
    /// </summary>
    // FIX PLR-H01: Implement ICombatant interface by delegating to QiController/BodyController (2026-04-11)
    public class PlayerController : MonoBehaviour, ICombatant
    {
        [Header("Identity")]
        [SerializeField] private string playerId = "player";
        [SerializeField] private string playerName = "Игрок";
        
        [Header("Systems")]
        [SerializeField] private BodyController bodyController;
        [SerializeField] private QiController qiController;
        [SerializeField] private TechniqueController techniqueController;
        [SerializeField] private InteractionController interactionController;
        [SerializeField] private StatDevelopment statDevelopment;
        [SerializeField] private SleepSystem sleepSystem;
        
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runSpeedMultiplier = 1.5f;
        [SerializeField] private float staminaCostPerSecond = 1f;
        
        [Header("References")]
        [SerializeField] private WorldController worldController;
        [SerializeField] private TimeController timeController;
        
        // === State ===
        private PlayerState state = new PlayerState();
        private bool isInitialized = false;
        
        // === Movement ===
        private Vector2 moveInput;
        private bool isRunning = false;
        private Rigidbody2D rb;
        
        // === Events ===
        public event Action<int, int> OnHealthChanged;
        public event Action<long, long> OnQiChanged;
        public event Action<CultivationLevel> OnCultivationLevelChanged;
        public event Action<string> OnLocationChanged;
        public event Action OnPlayerDeath;
        public event Action OnPlayerRevive;
        
        // FIX PLR-H01: ICombatant events (2026-04-11)
        event Action ICombatant.OnDeath { add { OnPlayerDeath += value; } remove { OnPlayerDeath -= value; } }
        event Action<float> ICombatant.OnDamageTaken { add { } remove { } } // Delegated through BodyController
        event Action<long, long> ICombatant.OnQiChanged { add { OnQiChanged += value; } remove { OnQiChanged -= value; } }
        
        // === Properties ===
        public string PlayerId => playerId;
        public string PlayerName => playerName;
        public PlayerState State => state;
        public StatDevelopment StatDevelopment => statDevelopment;
        public CultivationLevel CultivationLevel => qiController != null ? (CultivationLevel)qiController.CultivationLevel : CultivationLevel.None;
        public bool IsAlive => state.IsAlive;
        public string CurrentLocation => state.CurrentLocation;
        
        // FIX PLR-H01: ICombatant explicit implementations (2026-04-11)
        string ICombatant.Name => playerName;
        GameObject ICombatant.GameObject => gameObject;
        int ICombatant.CultivationLevel => qiController?.CultivationLevel ?? 1;
        int ICombatant.CultivationSubLevel => 0;
        int ICombatant.Strength => 10; // TODO: from StatDevelopment
        int ICombatant.Agility => 10;
        int ICombatant.Intelligence => 10;
        int ICombatant.Vitality => 10;
        long ICombatant.CurrentQi => qiController?.CurrentQi ?? 0;
        long ICombatant.MaxQi => qiController?.MaxQi ?? 0;
        float ICombatant.QiDensity => qiController?.QiDensity ?? 1f;
        QiDefenseType ICombatant.QiDefense => QiDefenseType.RawQi; // TODO: check shield technique
        bool ICombatant.HasShieldTechnique => false; // TODO: check techniqueController
        BodyMaterial ICombatant.BodyMaterial => bodyController?.BodyMaterial ?? BodyMaterial.Organic;
        float ICombatant.HealthPercent => bodyController?.HealthPercent ?? 0f;
        bool ICombatant.IsAlive => state.IsAlive;
        int ICombatant.Penetration => 0;
        float ICombatant.DodgeChance => DefenseProcessor.CalculateDodgeChance(10, 0f);
        float ICombatant.ParryChance => DefenseProcessor.CalculateParryChance(10, 0f);
        float ICombatant.BlockChance => DefenseProcessor.CalculateBlockChance(10, 0f);
        float ICombatant.ArmorCoverage => 0f;
        float ICombatant.DamageReduction => 0f;
        int ICombatant.ArmorValue => 0;
        
        // FIX PLR-H01: ICombatant method implementations (2026-04-11)
        void ICombatant.TakeDamage(BodyPartType part, float damage)
        {
            if (bodyController != null)
            {
                bodyController.TakeDamage(part, damage);
            }
        }
        
        void ICombatant.TakeDamageRandom(float damage)
        {
            if (bodyController != null)
            {
                bodyController.TakeDamageRandom(damage);
            }
        }
        
        bool ICombatant.SpendQi(long amount)
        {
            return qiController?.SpendQi(amount) ?? false;
        }
        
        void ICombatant.AddQi(long amount)
        {
            qiController?.AddQi(amount);
        }
        
        AttackerParams ICombatant.GetAttackerParams(Element attackElement)
        {
            return new AttackerParams
            {
                CultivationLevel = qiController?.CultivationLevel ?? 1,
                Strength = 10, Agility = 10, Intelligence = 10, Penetration = 0,
                AttackElement = attackElement, CombatSubtype = CombatSubtype.MeleeStrike,
                TechniqueLevel = 1, TechniqueGrade = TechniqueGrade.Common,
                IsUltimate = false, IsQiTechnique = false
            };
        }
        
        DefenderParams ICombatant.GetDefenderParams()
        {
            return new DefenderParams
            {
                CultivationLevel = qiController?.CultivationLevel ?? 1,
                CurrentQi = qiController?.CurrentQi ?? 0,
                QiDefense = QiDefenseType.RawQi,
                Agility = 10, Strength = 10,
                ArmorCoverage = 0f, DamageReduction = 0f, ArmorValue = 0,
                DodgePenalty = 0f, ParryBonus = 0f, BlockBonus = 0f,
                BodyMaterial = bodyController?.BodyMaterial ?? BodyMaterial.Organic,
                DefenderElement = Element.Neutral
            };
        }
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            InitializeComponents();
        }
        
        private void Start()
        {
            InitializePlayer();
        }
        
        private void Update()
        {
            if (!IsAlive) return;
            
            ProcessInput();
            UpdateState();
        }
        
        private void FixedUpdate()
        {
            if (!IsAlive) return;
            
            ProcessMovement();
        }
        
        private void OnDestroy()
        {
            // Отписываемся от событий (fix: утечка памяти)
            if (bodyController != null)
            {
                bodyController.OnDeath -= OnBodyDeath;
            }
            
            if (qiController != null)
            {
                qiController.OnQiChanged -= OnQiChangedHandler;
                qiController.OnCultivationLevelChanged -= OnCultivationLevelChangedHandler;
            }
        }
        
        // === Initialization ===
        
        private void InitializeComponents()
        {
            rb = GetComponent<Rigidbody2D>();
            
            if (bodyController == null)
                bodyController = GetComponent<BodyController>();
            if (qiController == null)
                qiController = GetComponent<QiController>();
            if (techniqueController == null)
                techniqueController = GetComponent<TechniqueController>();
            if (interactionController == null)
                interactionController = GetComponent<InteractionController>();
            if (statDevelopment == null)
                statDevelopment = GetComponent<StatDevelopment>();
            if (sleepSystem == null)
                sleepSystem = GetComponent<SleepSystem>();
            
            // FIX: Используем ServiceLocator вместо FindFirstObjectByType
            // GetOrFind найдёт через ServiceLocator O(1) или fallback на FindFirstObjectByType
            if (worldController == null)
                worldController = ServiceLocator.GetOrFind<WorldController>();
            if (timeController == null)
                timeController = ServiceLocator.GetOrFind<TimeController>();
        }
        
        private void InitializePlayer()
        {
            state.PlayerId = playerId;
            state.Name = playerName;
            state.IsAlive = true;
            
            // Подписываемся на события систем
            if (bodyController != null)
            {
                bodyController.OnDeath += OnBodyDeath;
            }
            
            if (qiController != null)
            {
                qiController.OnQiChanged += OnQiChangedHandler;
                qiController.OnCultivationLevelChanged += OnCultivationLevelChangedHandler;
            }
            
            isInitialized = true;
            Debug.Log($"Player initialized: {playerName}");
        }
        
        // === Input Processing ===
        
        private void ProcessInput()
        {
            // Движение - New Input System
            if (Keyboard.current != null)
            {
                moveInput.x = 0f;
                moveInput.y = 0f;
                
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                    moveInput.x = -1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                    moveInput.x = 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                    moveInput.y = 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    moveInput.y = -1f;
                    
                moveInput.Normalize();
                
                isRunning = Keyboard.current.leftShiftKey.isPressed && state.CurrentStamina > 0;
                
                // Медитация (F5)
                if (Keyboard.current.f5Key.wasPressedThisFrame)
                {
                    StartMeditation();
                }
            }
        }
        
        private void ProcessMovement()
        {
            if (rb == null) return;
            
            float speed = moveSpeed;
            
            // Ускорение бега
            if (isRunning && moveInput.magnitude > 0.1f)
            {
                speed *= runSpeedMultiplier;
                
                // Тратим выносливость
                state.CurrentStamina -= staminaCostPerSecond * Time.fixedDeltaTime;
                state.CurrentStamina = Mathf.Max(0, state.CurrentStamina);
            }
            else
            {
                // Восстанавливаем выносливость
                state.CurrentStamina += staminaCostPerSecond * 0.5f * Time.fixedDeltaTime;
                state.CurrentStamina = Mathf.Min(state.MaxStamina, state.CurrentStamina);
            }
            
            // Применяем движение
            rb.linearVelocity = moveInput * speed; // FIX: Unity 6 — velocity→linearVelocity
        }
        
        private void UpdateState()
        {
            // Обновляем состояние на основе систем
            if (qiController != null)
            {
                state.CurrentQi = qiController.CurrentQi; // FIX: long — без truncation
                state.MaxQi = qiController.MaxQi; // FIX: long — без truncation
                state.CultivationLevel = (CultivationLevel)qiController.CultivationLevel;
            }
            
            if (bodyController != null)
            {
                state.HealthPercent = bodyController.HealthPercent;
            }
        }
        
        // === Cultivation ===
        
        /// <summary>
        /// Начать медитацию.
        /// </summary>
        public void StartMeditation()
        {
            if (qiController == null) return;
            
            // Медитируем 1 игровой час
            long gained = qiController.Meditate(60);
            
            Debug.Log($"Meditation gained: {gained} Qi");
        }
        
        /// <summary>
        /// Попытка прорыва.
        /// </summary>
        public bool AttemptBreakthrough(bool isMajor)
        {
            if (qiController == null) return false;
            
            bool success = qiController.PerformBreakthrough(isMajor);
            
            if (success)
            {
                Debug.Log($"Breakthrough successful! New level: {qiController.CultivationLevel}");
            }
            else
            {
                Debug.Log("Breakthrough failed - not enough Qi");
            }
            
            return success;
        }
        
        // === Combat ===
        
        /// <summary>
        /// Нанести урон игроку.
        /// </summary>
        public void TakeDamage(int damage, string attackerId = "")
        {
            if (!IsAlive) return;
            
            if (bodyController != null)
            {
                bodyController.TakeDamageRandom(damage);
            }
        }
        
        /// <summary>
        /// Вылечить игрока.
        /// </summary>
        public void Heal(int amount)
        {
            if (bodyController != null)
            {
                bodyController.HealAll(amount, amount * 0.5f);
            }
        }
        
        /// <summary>
        /// Использовать технику из слота.
        /// </summary>
        public void UseQuickSlot(int slot)
        {
            if (techniqueController != null)
            {
                techniqueController.UseQuickSlot(slot);
            }
        }
        
        // === Location ===
        
        /// <summary>
        /// Переместиться в локацию.
        /// </summary>
        public void SetLocation(string locationId)
        {
            state.CurrentLocation = locationId;
            OnLocationChanged?.Invoke(locationId);
        }
        
        // === Death & Revival ===
        
        private void OnBodyDeath()
        {
            Die();
        }
        
        /// <summary>
        /// Смерть игрока.
        /// </summary>
        public void Die()
        {
            if (!state.IsAlive) return;
            
            state.IsAlive = false;
            state.DeathCount++;
            
            OnPlayerDeath?.Invoke();
            
            Debug.Log("Player died!");
        }
        
        /// <summary>
        /// Воскресить игрока.
        /// </summary>
        public void Revive(float healthPercent = 0.5f)
        {
            if (state.IsAlive) return;
            
            state.IsAlive = true;
            
            // FIX PLR-H03: Restore to healthPercent, not FullRestore (2026-04-11)
            if (bodyController != null)
            {
                float recoveryPercent = Mathf.Clamp01(healthPercent) * 100f;
                bodyController.HealAll(recoveryPercent, recoveryPercent * 0.3f);
            }
            
            if (qiController != null)
            {
                qiController.RestoreFull(); // Qi always fully restores on revive
            }
            
            state.CurrentStamina = state.MaxStamina * healthPercent;
            
            OnPlayerRevive?.Invoke();
            
            Debug.Log("Player revived!");
        }
        
        // === Event Handlers ===
        
        private void OnQiChangedHandler(long current, long max)
        {
            OnQiChanged?.Invoke(current, max);
        }
        
        private void OnCultivationLevelChangedHandler(int level)
        {
            OnCultivationLevelChanged?.Invoke((CultivationLevel)level);
        }
        
        // === Stat Development ===

        /// <summary>
        /// Добавить опыт к характеристике.
        /// </summary>
        public void AddStatExperience(StatType stat, float amount)
        {
            statDevelopment?.AddDelta(stat, amount);
        }

        /// <summary>
        /// Добавить опыт от боевого действия.
        /// </summary>
        public void AddCombatExperience(CombatActionType action)
        {
            statDevelopment?.AddCombatDelta(action);
        }

        /// <summary>
        /// Начать сон.
        /// </summary>
        public void StartSleep(float hours = 0f)
        {
            if (sleepSystem != null)
            {
                sleepSystem.StartSleep(hours);
                state.IsSleeping = true;
            }
        }

        /// <summary>
        /// Проснуться.
        /// </summary>
        public void EndSleep()
        {
            if (sleepSystem != null)
            {
                sleepSystem.EndSleep();
                state.IsSleeping = false;
            }
        }

        /// <summary>
        /// Получить значение характеристики.
        /// </summary>
        public float GetStat(StatType stat)
        {
            return statDevelopment?.GetStat(stat) ?? StatDevelopment.BASE_STAT_VALUE;
        }

        /// <summary>
        /// Получить прогресс развития характеристики.
        /// </summary>
        public float GetStatProgress(StatType stat)
        {
            return statDevelopment?.GetProgress(stat) ?? 0f;
        }

        // === Techniques ===

        /// <summary>
        /// Изучить технику.
        /// </summary>
        public bool LearnTechnique(Data.ScriptableObjects.TechniqueData technique, float initialMastery = 0f)
        {
            if (techniqueController == null) return false;
            return techniqueController.LearnTechnique(technique, initialMastery);
        }
        
        /// <summary>
        /// Назначить технику в слот.
        /// </summary>
        public bool AssignTechniqueToSlot(int slot, LearnedTechnique technique)
        {
            if (techniqueController == null) return false;
            return techniqueController.AssignToQuickSlot(technique, slot);
        }
        
        // === Save/Load ===
        
        public PlayerSaveData GetSaveData()
        {
            return new PlayerSaveData
            {
                PlayerId = playerId,
                Name = playerName,
                CultivationLevel = (int)CultivationLevel,
                CurrentQi = state.CurrentQi,
                CurrentLocationId = state.CurrentLocation
            };
        }
        
        public void LoadSaveData(PlayerSaveData data)
        {
            playerId = data.PlayerId;
            playerName = data.Name;
            state.CurrentQi = data.CurrentQi;
            state.CurrentLocation = data.CurrentLocationId;
            
            if (qiController != null)
            {
                qiController.SetCultivationLevel(data.CultivationLevel);
                qiController.SetQi(data.CurrentQi);
            }
        }
    }
    
    /// <summary>
    /// Runtime состояние игрока.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        public string PlayerId;
        public string Name;
        
        // Cultivation
        public CultivationLevel CultivationLevel;
        public long CurrentQi; // FIX: int→long для Qi > 2.1B
        public long MaxQi; // FIX: int→long для Qi > 2.1B
        
        // Health
        public float HealthPercent;
        public float CurrentStamina = 100f;
        public float MaxStamina = 100f;
        
        // Location
        public string CurrentLocation;
        
        // Stats
        public int DeathCount;
        
        // Status
        public bool IsAlive = true;
        public bool IsInCombat;
        public bool IsMeditating;
        public bool IsSleeping;
        public bool IsTraveling;
    }
}
