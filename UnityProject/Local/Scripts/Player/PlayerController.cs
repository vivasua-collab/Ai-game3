// ============================================================================
// PlayerController.cs — Главный контроллер игрока
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:17:18 UTC
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
    public class PlayerController : MonoBehaviour
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
        
        // === Properties ===
        public string PlayerId => playerId;
        public string PlayerName => playerName;
        public PlayerState State => state;
        public CultivationLevel CultivationLevel => qiController != null ? (CultivationLevel)qiController.CultivationLevel : CultivationLevel.None;
        public bool IsAlive => state.IsAlive;
        public string CurrentLocation => state.CurrentLocation;
        
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
            
            if (worldController == null)
                worldController = FindFirstObjectByType<WorldController>();
            if (timeController == null)
                timeController = FindFirstObjectByType<TimeController>();
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
            rb.velocity = moveInput * speed;
        }
        
        private void UpdateState()
        {
            // Обновляем состояние на основе систем
            if (qiController != null)
            {
                state.CurrentQi = (int)qiController.CurrentQi;
                state.MaxQi = (int)qiController.MaxQi;
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
            
            if (bodyController != null)
            {
                bodyController.FullRestore();
            }
            
            if (qiController != null)
            {
                qiController.RestoreFull();
            }
            
            state.CurrentStamina = state.MaxStamina;
            
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
        public int CurrentQi;
        public int MaxQi;
        
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
