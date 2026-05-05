// ============================================================================
// PlayerController.cs — Главный контроллер игрока
// Cultivation World Simulator
// Версия: 1.2 — Заменён FindFirstObjectByType на ServiceLocator
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-05-04 07:15:00 UTC — ФАЗА 5: ProcessCombatInput + ITechniqueUser
// Редактировано: 2026-05-07 10:00:00 UTC — ФАЗА 1: EquipmentController → ICombatant связь
// Редактировано: 2026-05-05 08:30:00 UTC — HasShieldTechnique + QiDefense из TechniqueController
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
using CultivationGame.TileSystem;
using CultivationGame.UI;
using CultivationGame.Inventory;
using UnityEngine.InputSystem;

namespace CultivationGame.Player
{
    /// <summary>
    /// Главный контроллер игрока — объединяет все системы персонажа.
    /// </summary>
    // FIX PLR-H01: Implement ICombatant interface by delegating to QiController/BodyController (2026-04-11)
    // ФАЗА 5: ITechniqueUser для интеграции с TechniqueChargeSystem (2026-05-04)
    public class PlayerController : MonoBehaviour, ICombatant, ITechniqueUser
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
        [SerializeField] private EquipmentController equipmentController; // ФАЗА 1: связь экипировки с боем
        
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float runSpeedMultiplier = 1.5f;
        [SerializeField] private float staminaCostPerSecond = 1f;
        
        [Header("References")]
        [SerializeField] private WorldController worldController;
        [SerializeField] private TimeController timeController;

        [Header("Harvest")]
        [SerializeField] private float harvestRange = 2.5f;
        [SerializeField] private int harvestDamage = 25;
        [SerializeField] private float harvestCooldown = 0.8f;
        [SerializeField] private LayerMask harvestableLayerMask = -1; // Слой "Harvestable" для Physics2D
        [SerializeField] private bool useNewHarvestSystem = true; // Переключатель: новая (Physics2D) / старая (TileMap) система

        // === State ===
        private PlayerState state = new PlayerState();
        private bool isInitialized = false;

        // === Movement ===
        private Vector2 moveInput;
        private bool isRunning = false;
        private Rigidbody2D rb;

        // === Harvest ===
        private DestructibleObjectController destructibleController;
        private HarvestFeedbackUI harvestFeedback;
        private float lastHarvestTime;
        private Vector2Int lastHarvestTile;
        private string lastHarvestResource = "";
        private Harvestable nearestHarvestable; // Ближайший harvestable-объект (для подсказки)
        private InventoryController inventoryController; // Ссылка на инвентарь
        // FIX-H03: Кэшированный GO для позиционирования фидбека (вместо new GameObject на каждый удар).
        // Редактировано: 2026-04-16
        private GameObject cachedFeedbackTarget;
        
        // === Events ===
#pragma warning disable CS0067
        public event Action<int, int> OnHealthChanged;
#pragma warning restore CS0067
        public event Action<long, long> OnQiChanged;
        public event Action<CultivationLevel> OnCultivationLevelChanged;
        public event Action<string> OnLocationChanged;
        public event Action OnPlayerDeath;
        public event Action OnPlayerRevive;
        public event Action<string, int> OnHarvestComplete; // resourceName, amount
        
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
        // ФАЗА 1: делегирование боевых бонусов в EquipmentController (2026-05-07)
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
        QiDefenseType ICombatant.QiDefense => techniqueController != null && techniqueController.HasActiveShield() ? QiDefenseType.Shield : QiDefenseType.RawQi;
        bool ICombatant.HasShieldTechnique => techniqueController != null && techniqueController.HasDefensiveTechnique();
        BodyMaterial ICombatant.BodyMaterial => bodyController?.BodyMaterial ?? BodyMaterial.Organic;
        float ICombatant.HealthPercent => bodyController?.HealthPercent ?? 0f;
        bool ICombatant.IsAlive => state.IsAlive;
        int ICombatant.Penetration => 0;
        // ФАЗА 1: бонусы из EquipmentController
        float ICombatant.DodgeChance => DefenseProcessor.CalculateDodgeChance(
            10, equipmentController?.GetDodgePenalty() ?? 0f);
        float ICombatant.ParryChance => DefenseProcessor.CalculateParryChance(
            10, equipmentController?.GetParryBonus() ?? 0f);
        float ICombatant.BlockChance => DefenseProcessor.CalculateBlockChance(
            10, equipmentController?.GetBlockBonus() ?? 0f);
        float ICombatant.ArmorCoverage => equipmentController?.GetArmorCoverage() ?? 0f;
        float ICombatant.DamageReduction => equipmentController?.GetDamageReduction() ?? 0f;
        int ICombatant.ArmorValue => equipmentController?.GetArmorValue() ?? 0;
        
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
                IsUltimate = false, IsQiTechnique = false,
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
                CultivationLevel = qiController?.CultivationLevel ?? 1,
                CurrentQi = qiController?.CurrentQi ?? 0,
                QiDefense = QiDefenseType.RawQi,
                Agility = 10, Strength = 10,
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
        
        // === Combat Input (ФАЗА 5) ===
        
        /// <summary>
        /// Текущая цель в бою.
        /// </summary>
        private ICombatant currentCombatTarget;
        
        /// <summary>
        /// Обработка боевого ввода.
        /// Клавиши 1-9: начать накачку техники из quickslot
        /// Повторное нажатие: отменить накачку
        /// Пробел: базовая атака
        /// Q/E: цикл по целям
        /// </summary>
        private void ProcessCombatInput()
        {
            if (Keyboard.current == null) return;
            
            // Техники (1-9) — накачка через TechniqueChargeSystem
            for (int i = 0; i < 9; i++)
            {
                // Клавиши 1-9 на основной клавиатуре
                var key = Keyboard.current.digit1Key; // Начнём с 1
                switch (i)
                {
                    case 0: key = Keyboard.current.digit1Key; break;
                    case 1: key = Keyboard.current.digit2Key; break;
                    case 2: key = Keyboard.current.digit3Key; break;
                    case 3: key = Keyboard.current.digit4Key; break;
                    case 4: key = Keyboard.current.digit5Key; break;
                    case 5: key = Keyboard.current.digit6Key; break;
                    case 6: key = Keyboard.current.digit7Key; break;
                    case 7: key = Keyboard.current.digit8Key; break;
                    case 8: key = Keyboard.current.digit9Key; break;
                }
                
                if (key.wasPressedThisFrame)
                {
                    HandleTechniqueInput(i);
                }
            }
            
            // Базовая атака (Пробел)
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ExecuteBasicAttack();
            }
            
            // Цикл по целям: Q — предыдущая, E — следующая
            if (Keyboard.current.qKey.wasPressedThisFrame)
            {
                CycleTarget(-1);
            }
            if (Keyboard.current.eKey.wasPressedThisFrame)
            {
                CycleTarget(1);
            }
        }
        
        /// <summary>
        /// Обработка нажатия клавиши техники.
        /// Если уже накачиваем ту же → отмена.
        /// Если накачиваем другую → игнорируем (правило: одна техника).
        /// Если не накачиваем → начать накачку.
        /// </summary>
        private void HandleTechniqueInput(int slot)
        {
            if (techniqueController == null) return;
            
            var tech = techniqueController.GetQuickSlotTechnique(slot);
            if (tech == null) return;
            
            if (techniqueController.IsCharging)
            {
                // Уже накачиваем
                if (techniqueController.ChargeSystem.ActiveCharge.Technique == tech)
                {
                    // Та же техника → отмена накачки
                    techniqueController.ChargeSystem.CancelCharge();
                }
                // Другая техника → игнорируем (правило: только одна)
                return;
            }
            
            // Начать накачку
            techniqueController.UseQuickSlot(slot);
        }
        
        /// <summary>
        /// Базовая атака (без техники) — пробел.
        /// </summary>
        private void ExecuteBasicAttack()
        {
            if (CombatManager.Instance == null || !CombatManager.Instance.IsInCombat) return;
            if (currentCombatTarget == null || !currentCombatTarget.IsAlive)
            {
                // Пытаемся найти ближайшую цель
                currentCombatTarget = HitDetector.FindNearestTarget(this);
                if (currentCombatTarget == null) return;
            }
            
            CombatManager.Instance.ExecuteBasicAttack(this, currentCombatTarget);
        }
        
        /// <summary>
        /// Цикл по целям в бою.
        /// direction: -1 = предыдущая, +1 = следующая.
        /// </summary>
        private void CycleTarget(int direction)
        {
            if (CombatManager.Instance == null || !CombatManager.Instance.IsInCombat) return;
            
            var combatants = CombatManager.Instance.Combatants;
            if (combatants.Count == 0) return;
            
            // Находим текущий индекс
            int currentIndex = -1;
            for (int i = 0; i < combatants.Count; i++)
            {
                if (combatants[i] == currentCombatTarget)
                {
                    currentIndex = i;
                    break;
                }
            }
            
            // Переключаемся на следующую/предыдущую цель (кроме себя)
            for (int attempt = 0; attempt < combatants.Count; attempt++)
            {
                currentIndex = (currentIndex + direction + combatants.Count) % combatants.Count;
                if (!ReferenceEquals(combatants[currentIndex], this) && combatants[currentIndex].IsAlive)
                {
                    currentCombatTarget = combatants[currentIndex];
                    Debug.Log($"[PlayerController] Цель: {currentCombatTarget.Name}");
                    return;
                }
            }
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
            ProcessCombatInput();
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

            // FIX-H03: Уничтожение кэшированного GO фидбека.
            // Редактировано: 2026-04-16
            if (cachedFeedbackTarget != null)
                Destroy(cachedFeedbackTarget);
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
            // ФАЗА 1: EquipmentController — может отсутствовать (опциональная система)
            if (equipmentController == null)
                equipmentController = GetComponent<EquipmentController>();
            // FIX: StatDevelopment — [Serializable] класс, НЕ MonoBehaviour.
            // GetComponent<StatDevelopment>() вызывает ArgumentException.
            // Создаём экземпляр через new вместо GetComponent.
            // Редактировано: 2026-04-15 UTC
            if (statDevelopment == null)
                statDevelopment = new StatDevelopment();
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
            // FIX: Защита от двойной инициализации (Start + GameInitializer)
            // Редактировано: 2026-04-15 14:40:00 UTC
            if (isInitialized) return;

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

                // Добыча ресурса (F)
                // Редактировано: 2026-04-15 08:15:00 UTC — F-key harvest с обратной связью
                if (Keyboard.current.fKey.wasPressedThisFrame)
                {
                    AttemptHarvest();
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

        // === Harvest (F-key) ===
        // Редактировано: 2026-04-15 08:15:00 UTC — обратная связь при добыче

        /// <summary>
        /// Попытка добычи ресурса ближайшего объекта (F-key).
        /// Версия 2.0 — использует Physics2D для поиска Harvestable-компонентов.
        /// Чекпоинт: 04_15_harvest_system_plan.md v3 §5.
        /// Редактировано: 2026-04-16 — полная перепись на Physics2D.
        /// </summary>
        private void AttemptHarvest()
        {
            // Проверить кулдаун
            if (Time.time - lastHarvestTime < harvestCooldown)
            {
                harvestFeedback?.ShowHarvestFailed("Подождите...");
                return;
            }

            // Выбор системы: новая (Physics2D) или старая (TileMap)
            if (useNewHarvestSystem)
            {
                AttemptHarvestPhysics();
            }
            else
            {
                AttemptHarvestLegacy();
            }
        }

        /// <summary>
        /// Новая система добычи — поиск через Physics2D по слою "Harvestable".
        /// Чекпоинт §5: Поток добычи.
        /// </summary>
        private void AttemptHarvestPhysics()
        {
            Vector2 playerPos = transform.position;

            // Поиск ближайшего Harvestable через Physics2D.OverlapCircleAll
            Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, harvestRange, harvestableLayerMask);

            if (hits.Length == 0)
            {
                harvestFeedback?.ShowHarvestFailed("Рядом нет ресурсов");
                return;
            }

            // Найти ближайший Harvestable
            Harvestable closest = null;
            float closestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var harvestable = hit.GetComponent<Harvestable>();
                if (harvestable == null || !harvestable.CanHarvest()) continue;

                float dist = Vector2.Distance(playerPos, hit.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = harvestable;
                }
            }

            if (closest == null)
            {
                harvestFeedback?.ShowHarvestFailed("Нельзя добыть");
                return;
            }

            // === Тик добычи ===
            // ФАЗА 1: Множитель инструмента из EquipmentController
            float toolMultiplier = equipmentController?.GetToolMultiplier() ?? 1.0f;
            int damage = harvestDamage;

            // Нанести урон и получить лут
            int yield = closest.HarvestHit(damage, toolMultiplier);

            if (yield > 0)
            {
                lastHarvestTime = Time.time;
                lastHarvestResource = closest.DisplayName;

                // Добавить ресурс в инвентарь
                AddResourceToInventory(closest.ResourceId, yield);

                // Показать прогресс-бар
                if (harvestFeedback == null)
                {
                    harvestFeedback = gameObject.AddComponent<HarvestFeedbackUI>();
                }

                harvestFeedback.ShowHarvestStarted(closest.transform, closest.DisplayName, 1f - closest.GetDurabilityProgress());
                harvestFeedback.ShowHarvestComplete(closest.DisplayName, yield);

                // Событие
                OnHarvestComplete?.Invoke(closest.DisplayName, yield);

                // Добавить опыт Strength
                AddStatExperience(StatType.Strength, 0.5f);

                Debug.Log($"[PlayerController] Harvest: {closest.DisplayName} (id={closest.ResourceId}), " +
                    $"yield={yield}, durability={closest.CurrentDurability}/{closest.MaxDurability}, " +
                    $"remaining={closest.GetRemainingResource()}");
            }
            else if (closest.IsDepleted)
            {
                harvestFeedback?.ShowHarvestFailed("Исчерпано");
            }
            else
            {
                harvestFeedback?.ShowHarvestFailed("Не удалось добыть");
            }
        }

        /// <summary>
        /// Добавить ресурс в инвентарь игрока.
        /// </summary>
        private void AddResourceToInventory(string resourceId, int amount)
        {
            if (amount <= 0) return;

            // Получить InventoryController
            if (inventoryController == null)
            {
                inventoryController = GetComponent<InventoryController>();
                if (inventoryController == null)
                    inventoryController = ServiceLocator.GetOrFind<InventoryController>();
            }

            if (inventoryController != null)
            {
                // Создать временный ItemData для ресурса
                var itemData = CreateResourceItemData(resourceId);
                if (itemData != null)
                {
                    inventoryController.AddItem(itemData, amount);
                }
                else
                {
                    Debug.LogWarning($"[PlayerController] Не удалось создать ItemData для ресурса: {resourceId}");
                }
            }
            else
            {
                Debug.LogWarning("[PlayerController] InventoryController не найден — ресурс не добавлен");
            }
        }

        /// <summary>
        /// Создать временный ItemData для ресурса.
        /// TODO: Заменить на загрузку из базы данных / Resources, когда будет реализована система предметов.
        /// </summary>
        private CultivationGame.Data.ScriptableObjects.ItemData CreateResourceItemData(string resourceId)
        {
            var itemData = ScriptableObject.CreateInstance<CultivationGame.Data.ScriptableObjects.ItemData>();

            // Базовые настройки
            itemData.name = $"res_{resourceId}";
            // Используем reflection или прямую установку, т.к. поля могут быть [SerializeField]
            // Для совместимости — устанавливаем через публичные свойства если есть

            // Маппинг resourceId → параметры предмета
            switch (resourceId)
            {
                case "wood":
                    itemData.itemId = "wood";
                    itemData.nameRu = "Древесина";
                    itemData.category = CultivationGame.Core.ItemCategory.Material;
                    itemData.rarity = CultivationGame.Core.ItemRarity.Common;
                    itemData.stackable = true;
                    itemData.maxStack = 99;
                    break;

                case "stone":
                    itemData.itemId = "stone";
                    itemData.nameRu = "Камень";
                    itemData.category = CultivationGame.Core.ItemCategory.Material;
                    itemData.rarity = CultivationGame.Core.ItemRarity.Common;
                    itemData.stackable = true;
                    itemData.maxStack = 99;
                    break;

                case "ore":
                    itemData.itemId = "ore";
                    itemData.nameRu = "Руда";
                    itemData.category = CultivationGame.Core.ItemCategory.Material;
                    itemData.rarity = CultivationGame.Core.ItemRarity.Uncommon;
                    itemData.stackable = true;
                    itemData.maxStack = 50;
                    break;

                case "berries":
                    itemData.itemId = "berries";
                    itemData.nameRu = "Ягоды";
                    itemData.category = CultivationGame.Core.ItemCategory.Consumable;
                    itemData.rarity = CultivationGame.Core.ItemRarity.Common;
                    itemData.stackable = true;
                    itemData.maxStack = 30;
                    break;

                case "herb":
                    itemData.itemId = "herb";
                    itemData.nameRu = "Трава";
                    itemData.category = CultivationGame.Core.ItemCategory.Consumable;
                    itemData.rarity = CultivationGame.Core.ItemRarity.Common;
                    itemData.stackable = true;
                    itemData.maxStack = 50;
                    break;

                default:
                    itemData.itemId = resourceId;
                    itemData.nameRu = resourceId;
                    itemData.category = CultivationGame.Core.ItemCategory.Material;
                    itemData.rarity = CultivationGame.Core.ItemRarity.Common;
                    itemData.stackable = true;
                    itemData.maxStack = 99;
                    break;
            }

            return itemData;
        }

        /// <summary>
        /// Старая система добычи — через TileMapController (для обратной совместимости).
        /// Используется когда useNewHarvestSystem = false.
        /// </summary>
        private void AttemptHarvestLegacy()
        {
            EnsureHarvestComponents();

            // Найти ближайший разрушаемый объект
            var tileMapCtrl = ServiceLocator.GetOrFind<TileMapController>();
            if (tileMapCtrl == null || tileMapCtrl.MapData == null)
            {
                Debug.LogWarning("[PlayerController] TileMapController не найден для добычи");
                harvestFeedback?.ShowHarvestFailed("Нет карты");
                return;
            }

            // Определить тайл перед игроком (по направлению движения)
            Vector2 playerPos = transform.position;
            Vector2 checkPos = playerPos + moveInput.normalized * 1.5f;

            // Если не двигается — проверяем тайл под игроком и соседние
            if (moveInput.magnitude < 0.1f)
            {
                checkPos = playerPos + Vector2.up * 1.5f; // По умолчанию — вверх
            }

            var tilePos = tileMapCtrl.MapData.WorldToTile(checkPos);
            var tile = tileMapCtrl.GetTile(tilePos.x, tilePos.y);

            if (tile == null || tile.objects.Count == 0)
            {
                // Попробовать соседние тайлы
                Vector2Int[] neighbors = new Vector2Int[]
                {
                    new Vector2Int(tilePos.x + 1, tilePos.y),
                    new Vector2Int(tilePos.x - 1, tilePos.y),
                    new Vector2Int(tilePos.x, tilePos.y + 1),
                    new Vector2Int(tilePos.x, tilePos.y - 1),
                };

                bool found = false;
                foreach (var neighbor in neighbors)
                {
                    var neighborTile = tileMapCtrl.GetTile(neighbor.x, neighbor.y);
                    if (neighborTile?.objects.Count > 0)
                    {
                        tilePos = neighbor;
                        tile = neighborTile;
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    harvestFeedback?.ShowHarvestFailed("Рядом нет ресурсов");
                    return;
                }
            }

            // Проверить дистанцию
            float distToTile = Vector2.Distance(playerPos, tileMapCtrl.MapData.TileToWorld(tilePos.x, tilePos.y));
            if (distToTile > harvestRange)
            {
                harvestFeedback?.ShowHarvestFailed("Слишком далеко");
                return;
            }

            var obj = tile.objects[0];
            if (!obj.isHarvestable || obj.maxDurability <= 0)
            {
                harvestFeedback?.ShowHarvestFailed("Нельзя добыть");
                return;
            }

            // Получить название ресурса
            string resourceName = GetResourceName(obj.objectType);

            // Нанести урон
            if (destructibleController == null)
                destructibleController = ServiceLocator.GetOrFind<DestructibleObjectController>();

            int damage = harvestDamage;

            // Подписаться на событие разрушения один раз
            if (destructibleController != null)
            {
                int result = destructibleController.DamageObjectAtTile(tilePos.x, tilePos.y, damage);

                if (result > 0)
                {
                    lastHarvestTime = Time.time;
                    lastHarvestTile = tilePos;
                    lastHarvestResource = resourceName;

                    // Рассчитать прогресс
                    float progress = 1f - ((float)obj.currentDurability / obj.maxDurability);

                    // Создать/обновить обратную связь
                    if (harvestFeedback == null)
                    {
                        harvestFeedback = gameObject.AddComponent<HarvestFeedbackUI>();
                    }

                    // FIX-H03: Переиспользуем кэшированный GO вместо new GameObject на каждый удар.
                    // Ранее: new GameObject("HarvestTarget") на каждый вызов — утечка GO.
                    // Редактировано: 2026-04-16
                    if (cachedFeedbackTarget == null)
                        cachedFeedbackTarget = new GameObject("HarvestTarget");
                    cachedFeedbackTarget.transform.position = tileMapCtrl.MapData.TileToWorld(tilePos.x, tilePos.y);

                    harvestFeedback.ShowHarvestStarted(cachedFeedbackTarget.transform, resourceName, progress);

                    // Проверить, разрушен ли объект
                    if (obj.currentDurability <= 0 || obj.IsDestroyed())
                    {
                        harvestFeedback.ShowHarvestComplete(resourceName, GetResourceAmount(obj.objectType));
                        OnHarvestComplete?.Invoke(resourceName, GetResourceAmount(obj.objectType));

                        // Добавить опыт
                        AddStatExperience(StatType.Strength, 0.5f);
                    }

                    Debug.Log($"[PlayerController] Harvest hit: {resourceName} at ({tilePos.x},{tilePos.y}), damage={result}, durability={obj.currentDurability}/{obj.maxDurability}");
                }
                else
                {
                    harvestFeedback?.ShowHarvestFailed("Не удалось добыть");
                }
            }
        }

        /// <summary>
        /// Получить название ресурса из типа объекта.
        /// </summary>
        private string GetResourceName(TileObjectType objectType)
        {
            return objectType switch
            {
                TileObjectType.Tree_Oak => "Дерево",
                TileObjectType.Tree_Pine => "Дерево",
                TileObjectType.Rock_Small => "Камень",
                TileObjectType.Rock_Medium => "Камень",
                TileObjectType.OreVein => "Руду",
                TileObjectType.Bush => "Ягоды",
                TileObjectType.Herb => "Траву",
                TileObjectType.Chest => "Сундук",
                _ => "Ресурс"
            };
        }

        /// <summary>
        /// Получить количество добываемого ресурса.
        /// </summary>
        private int GetResourceAmount(TileObjectType objectType)
        {
            return objectType switch
            {
                TileObjectType.Tree_Oak => 3,
                TileObjectType.Tree_Pine => 3,
                TileObjectType.Rock_Small => 2,
                TileObjectType.Rock_Medium => 4,
                TileObjectType.OreVein => 2,
                TileObjectType.Bush => 2,
                TileObjectType.Herb => 1,
                TileObjectType.Chest => 1,
                _ => 1
            };
        }

        /// <summary>
        /// Инициализация компонентов для добычи.
        /// </summary>
        private void EnsureHarvestComponents()
        {
            if (destructibleController == null)
                destructibleController = ServiceLocator.GetOrFind<DestructibleObjectController>();
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
