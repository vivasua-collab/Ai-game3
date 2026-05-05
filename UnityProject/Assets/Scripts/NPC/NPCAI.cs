// ============================================================================
// NPCAI.cs — AI принятие решений
// Cultivation World Simulator
// Версия: 1.1
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-11 00:00:00 UTC — Fix-07
// Редактировано: 2026-04-30 07:55:00 UTC — NPCVisual feedback при смене AI-состояния
// Редактировано: 2026-04-30 09:55:00 UTC — ExecuteXxx через NPCMovement + CombatManager, AggroRadius, TargetResolution
// Редактировано: 2026-05-01 13:03:00 UTC — fix: detectionRange CS0414 warning suppressed
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;
using CultivationGame.Qi;

namespace CultivationGame.NPC
{
    /// <summary>
    /// AI контроллер для NPC — принятие решений и поведение.
    /// </summary>
    public class NPCAI : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float decisionInterval = 1f;
        [SerializeField] private float aggroRange = 10f;
        [SerializeField] private float fleeHealthThreshold = 0.2f;
#pragma warning disable CS0414  // TODO: использовать для не-боевого обнаружения
        [SerializeField] private float detectionRange = 15f;
#pragma warning restore CS0414
        
        [Header("Combat")]
        [SerializeField] private float attackRange = 1.5f;
        [SerializeField] private float attackCooldown = 1.5f;
        [SerializeField] private float chaseSpeedMultiplier = 1.2f;
        [SerializeField] private LayerMask playerLayerMask = -1;
        
        [Header("Personality Modifiers")]
        [SerializeField] private float aggressiveness = 0.5f;    // 0-1
        [SerializeField] private float cautiousness = 0.5f;      // 0-1
        [SerializeField] private float socialness = 0.5f;        // 0-1
        [SerializeField] private float ambition = 0.5f;          // 0-1
        
        // === Runtime ===
        private NPCController npcController;
        private NPCVisual npcVisual;
        private NPCMovement npcMovement;
        private NPCState state;
        private float decisionTimer;
        private List<string> knownTargets = new List<string>();
        private Dictionary<string, float> threatLevels = new Dictionary<string, float>();
        
        // Combat Runtime
        private float lastAttackTime = -999f;
        private Transform cachedTargetTransform;
        private float aggroCheckTimer = 0f;
        private float aggroCheckInterval = 0.5f;
        
        // FIX NPC-M01: Threat decay tracking
        private float threatDecayTimer = 0f;
        [SerializeField] private float threatDecayInterval = 5f;   // seconds between decay ticks
        [SerializeField] private float threatDecayRate = 2f;       // threat points lost per tick
        
        // === Patrol ===
        private Vector3[] patrolPoints;
        private int currentPatrolIndex;
        
        // FIX NPC-ATT-02: Store personality traits for behavior weights
        private PersonalityTrait personalityFlags = PersonalityTrait.None;
        
        // === Events ===
        public event Action<NPCAIState, NPCAIState> OnStateChanged;
        public event Action<string> OnTargetAcquired;
        public event Action<string> OnTargetLost;
        
        // === Properties ===
        public NPCAIState CurrentState => state?.CurrentAIState ?? NPCAIState.Idle;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            npcController = GetComponent<NPCController>();
            npcVisual = GetComponent<NPCVisual>();
            npcMovement = GetComponent<NPCMovement>();
        }
        
        private void Start()
        {
            if (npcController != null)
                state = npcController.State;

            // Редактировано: 2026-05-04 — Refresh ссылок, которые были null в Awake()
            // (компоненты добавлялись после NPCAI в NPCSceneSpawner)
            if (npcVisual == null)
                npcVisual = GetComponent<NPCVisual>();
            if (npcMovement == null)
                npcMovement = GetComponent<NPCMovement>();
        }
        
        private void Update()
        {
            if (state == null || !state.IsAlive) return;
            
            decisionTimer += Time.deltaTime;
            
            if (decisionTimer >= decisionInterval)
            {
                decisionTimer = 0f;
                MakeDecision();
            }
            
            // FIX NPC-M01: Decay threat levels over time
            DecayThreats();
            
            // Aggro Radius: обнаружение игрока
            CheckAggroRadius();
            
            ExecuteCurrentState();
        }
        
        // === Decision Making ===
        
        /// <summary>
        /// Принять решение о следующем действии.
        /// </summary>
        private void MakeDecision()
        {
            // Проверяем здоровье
            if (ShouldFlee())
            {
                ChangeState(NPCAIState.Fleeing);
                return;
            }
            
            // Проверяем угрозы
            string threat = GetHighestThreat();
            if (!string.IsNullOrEmpty(threat))
            {
                if (aggressiveness > cautiousness)
                {
                    state.TargetId = threat;
                    ChangeState(NPCAIState.Attacking);
                }
                else
                {
                    ChangeState(NPCAIState.Fleeing);
                }
                return;
            }
            
            // Если в бою, но нет угроз — возвращаемся к обычному поведению
            if (CurrentState == NPCAIState.Attacking || CurrentState == NPCAIState.Fleeing)
            {
                ChangeState(NPCAIState.Idle);
                return;
            }
            
            // Обычное поведение на основе личности и времени
            DecideNormalBehavior();
        }
        
        private bool ShouldFlee()
        {
            if (state == null) return false;
            
            float healthPercent = (float)state.CurrentHealth / Mathf.Max(1, state.MaxHealth);
            
            // FIX NPC-ATT-02: Pacifist flees more readily
            bool isPacifist = (personalityFlags & PersonalityTrait.Pacifist) != 0;
            float effectiveFleeThreshold = isPacifist ? fleeHealthThreshold * 2f : fleeHealthThreshold;
            
            return healthPercent <= effectiveFleeThreshold && cautiousness > aggressiveness;
        }
        
        private string GetHighestThreat()
        {
            string highestThreat = null;
            float highestLevel = 0f;
            
            foreach (var kvp in threatLevels)
            {
                if (kvp.Value > highestLevel)
                {
                    highestLevel = kvp.Value;
                    highestThreat = kvp.Key;
                }
            }
            
            return highestThreat;
        }
        
        // FIX NPC-H01 + NPC-ATT-02: Revised probabilities with PersonalityTrait weights (2026-04-11)
        /// <summary>
        /// Decide normal behavior based on personality traits and weighted probabilities.
        /// 
        /// PersonalityTrait effects:
        /// - Aggressive: +50% attack, −30% defense
        /// - Cautious: +50% defense, −30% attack
        /// - Ambitious: +30% attack, +30% leadership (patrol/guard)
        /// - Treacherous: when Attitude < Neutral, chance of betrayal
        /// - Loyal: never betrays
        /// - Pacifist: −50% attack, +30% flee/rest
        /// </summary>
        private void DecideNormalBehavior()
        {
            // Base weights for each behavior
            float idleWeight = 0.10f;
            float wanderWeight = 0.15f;
            float talkWeight = socialness * 0.20f;
            float cultivateWeight = ambition * 0.25f;
            float restWeight = 0.15f;
            float workWeight = 0.10f;
            float patrolWeight = 0.05f;
            
            // FIX NPC-ATT-02: Apply PersonalityTrait modifiers
            bool isAggressive = (personalityFlags & PersonalityTrait.Aggressive) != 0;
            bool isCautious = (personalityFlags & PersonalityTrait.Cautious) != 0;
            bool isAmbitious = (personalityFlags & PersonalityTrait.Ambitious) != 0;
            bool isTreacherous = (personalityFlags & PersonalityTrait.Treacherous) != 0;
            bool isLoyal = (personalityFlags & PersonalityTrait.Loyal) != 0;
            bool isPacifist = (personalityFlags & PersonalityTrait.Pacifist) != 0;
            bool isVengeful = (personalityFlags & PersonalityTrait.Vengeful) != 0;
            bool isCurious = (personalityFlags & PersonalityTrait.Curious) != 0;
            
            // Aggressive: +50% attack (patrol as proxy), −30% rest
            if (isAggressive)
            {
                patrolWeight *= 1.5f;
                restWeight *= 0.7f;
            }
            
            // Cautious: +50% defense (rest/patrol), −30% attack (patrol aggressive)
            if (isCautious)
            {
                restWeight *= 1.5f;
                patrolWeight *= 0.7f;
            }
            
            // Ambitious: +30% cultivate, +30% leadership (patrol/guard)
            if (isAmbitious)
            {
                cultivateWeight *= 1.3f;
                patrolWeight *= 1.3f;
                workWeight *= 1.2f;
            }
            
            // Pacifist: −50% attack, +30% rest/flee
            if (isPacifist)
            {
                patrolWeight *= 0.5f;
                restWeight *= 1.3f;
                cultivateWeight *= 1.2f;
            }
            
            // Curious: more wandering and talking
            if (isCurious)
            {
                wanderWeight *= 1.4f;
                talkWeight *= 1.3f;
            }
            
            // Vengeful: more patrol (seeking enemies)
            if (isVengeful)
            {
                patrolWeight *= 1.3f;
            }
            
            // Treacherous: when Attitude < Neutral, increase chance of betrayal actions
            // (represented as more working/patrol and less talking)
            if (isTreacherous && state != null && (int)state.Attitude < (int)Attitude.Neutral)
            {
                talkWeight *= 0.5f;
                patrolWeight *= 1.4f;
                // Note: actual betrayal mechanics would be implemented in combat/interaction
            }
            
            // Loyal: never abandons — increase work/talk, decrease idle
            if (isLoyal)
            {
                idleWeight *= 0.5f;
                workWeight *= 1.3f;
                talkWeight *= 1.2f;
            }
            
            // Calculate total weight for normalization
            float totalWeight = idleWeight + wanderWeight + talkWeight + cultivateWeight + 
                                restWeight + workWeight + patrolWeight;
            
            // Roll and pick behavior
            float rand = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;
            
            cumulative += idleWeight;
            if (rand < cumulative) { ChangeState(NPCAIState.Idle); return; }
            
            cumulative += wanderWeight;
            if (rand < cumulative) { ChangeState(NPCAIState.Wandering); return; }
            
            cumulative += talkWeight;
            if (rand < cumulative) { ChangeState(NPCAIState.Talking); return; }
            
            cumulative += cultivateWeight;
            if (rand < cumulative) { ChangeState(NPCAIState.Cultivating); return; }
            
            cumulative += restWeight;
            if (rand < cumulative) { ChangeState(NPCAIState.Resting); return; }
            
            cumulative += workWeight;
            if (rand < cumulative) { ChangeState(NPCAIState.Working); return; }
            
            cumulative += patrolWeight;
            if (rand < cumulative) { ChangeState(NPCAIState.Patrolling); return; }
            
            // Fallback
            ChangeState(NPCAIState.Idle);
        }
        
        // === State Execution ===
        
        private void ExecuteCurrentState()
        {
            state.StateTimer += Time.deltaTime;
            
            switch (CurrentState)
            {
                case NPCAIState.Idle:
                    ExecuteIdle();
                    break;
                case NPCAIState.Wandering:
                    ExecuteWandering();
                    break;
                case NPCAIState.Patrolling:
                    ExecutePatrolling();
                    break;
                case NPCAIState.Following:
                    ExecuteFollowing();
                    break;
                case NPCAIState.Fleeing:
                    ExecuteFleeing();
                    break;
                case NPCAIState.Attacking:
                    ExecuteAttacking();
                    break;
                case NPCAIState.Meditating:
                case NPCAIState.Cultivating:
                    ExecuteCultivating();
                    break;
                case NPCAIState.Resting:
                    ExecuteResting();
                    break;
                default:
                    break;
            }
        }
        
        private void ExecuteIdle()
        {
            // Останавливаем движение
            if (npcMovement != null && npcMovement.IsMoving)
                npcMovement.Stop();
            
            // Ничего не делаем, ждём
            if (state.StateTimer > 5f)
            {
                ChangeState(NPCAIState.Wandering);
            }
        }
        
        private void ExecuteWandering()
        {
            if (npcMovement == null) return;
            
            // Блуждание вокруг домашней позиции
            npcMovement.WanderAround(npcMovement.HomePosition);
        }
        
        private void ExecutePatrolling()
        {
            if (npcMovement == null) return;
            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                // Нет точек патруля — блуждаем
                ExecuteWandering();
                return;
            }
            
            // Двигаемся к текущей точке патруля
            Vector3 target = patrolPoints[currentPatrolIndex];
            npcMovement.MoveTo(target);
            
            // Если достигли — переходим к следующей
            if (npcMovement.HasReachedTarget(target))
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            }
        }
        
        private void ExecuteFollowing()
        {
            if (npcMovement == null) return;
            if (string.IsNullOrEmpty(state.TargetId)) return;
            
            Transform target = FindTargetTransform(state.TargetId);
            if (target != null)
            {
                npcMovement.FollowTarget(target, stopDistance: 1.5f);
            }
        }
        
        private void ExecuteFleeing()
        {
            if (npcMovement == null) return;
            
            // Определяем, от чего убегаем
            string threatId = GetHighestThreat();
            Transform threat = FindTargetTransform(threatId);
            
            if (threat != null)
            {
                npcMovement.FleeFrom(threat.position);
            }
            else
            {
                // Угроза не найдена — убегаем от последней известной позиции
                npcMovement.FleeFrom(state.LastKnownPosition);
            }
            
            // Через 10с без урона — пытаемся спрятаться (Idle)
            if (state.StateTimer > 10f)
            {
                npcMovement.Stop();
                ChangeState(NPCAIState.Idle);
            }
        }
        
        private void ExecuteAttacking()
        {
            if (string.IsNullOrEmpty(state.TargetId))
            {
                ChangeState(NPCAIState.Idle);
                return;
            }
            
            Transform target = FindTargetTransform(state.TargetId);
            if (target == null)
            {
                // Цель потеряна
                ChangeState(NPCAIState.Idle);
                return;
            }
            
            float dist = Vector3.Distance(transform.position, target.position);
            
            if (dist > attackRange)
            {
                // Подходим к цели
                if (npcMovement != null)
                {
                    float chaseSpeed = npcMovement.BaseSpeed * chaseSpeedMultiplier;
                    npcMovement.MoveTo(target.position, chaseSpeed);
                }
            }
            else
            {
                // В зоне атаки — останавливаемся и атакуем
                if (npcMovement != null)
                    npcMovement.Stop();
                
                // Проверяем кулдаун атаки
                if (Time.time - lastAttackTime >= attackCooldown)
                {
                    lastAttackTime = Time.time;
                    PerformAttack(target);
                }
            }
        }
        
        // FIX NPC-M02: Qi restoration proportional to conductivity (2026-04-11)
        /// <summary>
        /// Cultivating — Qi restoration proportional to conductivity.
        /// Base: 10 Qi, scaled by conductivity (from QiController).
        /// </summary>
        private void ExecuteCultivating()
        {
            // Останавливаем движение при культивации
            if (npcMovement != null && npcMovement.IsMoving)
                npcMovement.Stop();
            
            if (state.StateTimer > 30f)
            {
                // Base Qi recovery, scaled by conductivity
                float conductivity = 1.0f; // default
                if (npcController != null)
                {
                    QiController qiCtrl = npcController.GetComponent<QiController>();
                    if (qiCtrl != null)
                    {
                        conductivity = qiCtrl.Conductivity;
                    }
                }
                
                long qiGain = (long)(10f * conductivity);
                state.CurrentQi = Math.Min(state.MaxQi, state.CurrentQi + qiGain);
                ChangeState(NPCAIState.Idle);
            }
        }
        
        private void ExecuteResting()
        {
            // Останавливаем движение при отдыхе
            if (npcMovement != null && npcMovement.IsMoving)
                npcMovement.Stop();
            
            // Отдых — восстановление здоровья и выносливости
            if (state.StateTimer > 20f)
            {
                state.CurrentHealth = Mathf.Min(state.MaxHealth, state.CurrentHealth + 5);
                state.CurrentStamina = Mathf.Min(state.MaxStamina, state.CurrentStamina + 20f);
                ChangeState(NPCAIState.Idle);
            }
        }
        
        // === State Management ===
        
        private void ChangeState(NPCAIState newState)
        {
            if (CurrentState == newState) return;
            
            NPCAIState oldState = CurrentState;
            state.CurrentAIState = newState;
            state.StateTimer = 0f;
            
            OnStateChanged?.Invoke(oldState, newState);
            
            if (npcController != null)
                npcController.SetAIState(newState);
            
            // Визуальная обратная связь через NPCVisual
            if (npcVisual != null)
                npcVisual.SetAIState(newState);
        }
        
        // === Threat System ===
        
        /// <summary>
        /// Добавить угрозу.
        /// </summary>
        public void AddThreat(string sourceId, float threatLevel)
        {
            if (!threatLevels.ContainsKey(sourceId))
            {
                threatLevels[sourceId] = 0f;
            }
            
            threatLevels[sourceId] += threatLevel;
            
            // Автоматически реагируем на высокую угрозу
            if (threatLevels[sourceId] > 50f && !knownTargets.Contains(sourceId))
            {
                knownTargets.Add(sourceId);
                OnTargetAcquired?.Invoke(sourceId);
            }
        }
        
        /// <summary>
        /// Удалить угрозу.
        /// </summary>
        public void RemoveThreat(string sourceId)
        {
            if (threatLevels.ContainsKey(sourceId))
            {
                threatLevels.Remove(sourceId);
                knownTargets.Remove(sourceId);
                OnTargetLost?.Invoke(sourceId);
            }
        }
        
        /// <summary>
        /// Очистить все угрозы.
        /// </summary>
        public void ClearAllThreats()
        {
            threatLevels.Clear();
            knownTargets.Clear();
        }
        
        // FIX NPC-M01: Threat decay over time (2026-04-11)
        // FIX: 2026-05-05 — Collection was modified during enumeration (Dictionary value update invalidates enumerator)
        /// <summary>
        /// Decay threat levels over time. Threats that are not reinforced gradually fade.
        /// </summary>
        private void DecayThreats()
        {
            threatDecayTimer += Time.deltaTime;
            
            if (threatDecayTimer < threatDecayInterval)
                return;
            
            threatDecayTimer = 0f;
            
            // Собираем изменения в отдельные списки — нельзя модифицировать Dictionary внутри foreach
            List<string> toRemove = null;
            List<KeyValuePair<string, float>> toUpdate = null;
            
            foreach (var kvp in threatLevels)
            {
                float newThreat = kvp.Value - threatDecayRate;
                if (newThreat <= 0f)
                {
                    if (toRemove == null) toRemove = new List<string>();
                    toRemove.Add(kvp.Key);
                }
                else
                {
                    if (toUpdate == null) toUpdate = new List<KeyValuePair<string, float>>();
                    toUpdate.Add(new KeyValuePair<string, float>(kvp.Key, newThreat));
                }
            }
            
            // Применяем обновления значений
            if (toUpdate != null)
            {
                foreach (var update in toUpdate)
                {
                    threatLevels[update.Key] = update.Value;
                }
            }
            
            // Remove expired threats
            if (toRemove != null)
            {
                foreach (string sourceId in toRemove)
                {
                    threatLevels.Remove(sourceId);
                    knownTargets.Remove(sourceId);
                    OnTargetLost?.Invoke(sourceId);
                }
            }
        }
        
        // === Patrol Setup ===
        
        /// <summary>
        /// Установить точки патруля.
        /// </summary>
        public void SetPatrolPoints(Vector3[] points)
        {
            patrolPoints = points;
            currentPatrolIndex = 0;
        }
        
        // === Personality ===
        
        /// <summary>
        /// Установить параметры личности.
        /// </summary>
        public void SetPersonality(float aggressive, float cautious, float social, float ambit)
        {
            aggressiveness = Mathf.Clamp01(aggressive);
            cautiousness = Mathf.Clamp01(cautious);
            socialness = Mathf.Clamp01(social);
            ambition = Mathf.Clamp01(ambit);
        }
        
        // FIX NPC-ATT-02: Replace ApplyDispositionModifiers with ApplyPersonalityModifiers (2026-04-11)
        /// <summary>
        /// Apply PersonalityTrait flags to modify personality weights.
        /// Each trait adjusts the base personality parameters.
        /// </summary>
        public void ApplyPersonalityModifiers(PersonalityTrait traits)
        {
            personalityFlags = traits;
            
            if ((traits & PersonalityTrait.Aggressive) != 0)
            {
                aggressiveness = Mathf.Clamp01(aggressiveness + 0.3f);
                cautiousness = Mathf.Clamp01(cautiousness - 0.1f);
            }
            if ((traits & PersonalityTrait.Cautious) != 0)
            {
                cautiousness = Mathf.Clamp01(cautiousness + 0.3f);
                aggressiveness = Mathf.Clamp01(aggressiveness - 0.1f);
            }
            if ((traits & PersonalityTrait.Ambitious) != 0)
            {
                ambition = Mathf.Clamp01(ambition + 0.3f);
            }
            if ((traits & PersonalityTrait.Treacherous) != 0)
            {
                ambition = Mathf.Clamp01(ambition + 0.2f);
                socialness = Mathf.Clamp01(socialness - 0.1f);
            }
            if ((traits & PersonalityTrait.Loyal) != 0)
            {
                socialness = Mathf.Clamp01(socialness + 0.2f);
            }
            if ((traits & PersonalityTrait.Pacifist) != 0)
            {
                aggressiveness = Mathf.Clamp01(aggressiveness - 0.3f);
                cautiousness = Mathf.Clamp01(cautiousness + 0.2f);
            }
            if ((traits & PersonalityTrait.Vengeful) != 0)
            {
                aggressiveness = Mathf.Clamp01(aggressiveness + 0.2f);
            }
            if ((traits & PersonalityTrait.Curious) != 0)
            {
                socialness = Mathf.Clamp01(socialness + 0.2f);
            }
        }
        
        /// <summary>
        /// Legacy: Apply disposition modifiers.
        /// FIX NPC-ATT-02: Migrated to ApplyPersonalityModifiers. (2026-04-11)
        /// </summary>
        [Obsolete("Use ApplyPersonalityModifiers(PersonalityTrait) instead.")]
#pragma warning disable CS0618 // Disposition obsolete
        public void ApplyDispositionModifiers(Disposition disposition)
        {
            switch (disposition)
            {
                case Disposition.Aggressive:
                    aggressiveness += 0.3f;
                    cautiousness -= 0.1f;
                    break;
                case Disposition.Friendly:
                    socialness += 0.3f;
                    aggressiveness -= 0.1f;
                    break;
                case Disposition.Cautious:
                    cautiousness += 0.3f;
                    aggressiveness -= 0.1f;
                    break;
                case Disposition.Treacherous:
                    ambition += 0.2f;
                    socialness -= 0.1f;
                    break;
                case Disposition.Ambitious:
                    ambition += 0.3f;
                    break;
                default:
                    break;
            }
            
            // Нормализуем значения
            aggressiveness = Mathf.Clamp01(aggressiveness);
            cautiousness = Mathf.Clamp01(cautiousness);
            socialness = Mathf.Clamp01(socialness);
            ambition = Mathf.Clamp01(ambition);
        }
#pragma warning restore CS0618 // Disposition obsolete
        
        // === Combat System ===
        
        /// <summary>
        /// Выполнить атаку по цели через CombatManager.
        /// FIX: 2026-05-05 — CombatManager not found: добавлен fallback на FindObjectOfType
        ///   и прямой путь урона при отсутствии CombatManager.
        /// </summary>
        private void PerformAttack(Transform target)
        {
            if (npcController == null) return;
            
            ICombatant npcCombatant = npcController as ICombatant;
            ICombatant targetCombatant = target.GetComponent<ICombatant>();
            
            if (npcCombatant == null || targetCombatant == null)
            {
                Debug.LogWarning($"[NPCAI] Cannot attack: NPC or target is not ICombatant");
                return;
            }
            
            // Пытаемся получить CombatManager — singleton с auto-create fallback
            CombatManager cm = CombatManager.GetOrCreate();
            
            if (cm == null)
            {
                // CombatManager отсутствует — наносим урон напрямую через ICombatant.TakeDamage
                PerformDirectAttack(npcCombatant, targetCombatant);
                return;
            }
            
            // Если бой ещё не начат — инициируем
            if (!cm.IsInCombat || !cm.IsCombatantInCombat(npcCombatant))
            {
                cm.InitiateCombat(npcCombatant, targetCombatant);
            }
            
            // Выполняем базовую атаку через полный пайплайн
            AttackResult result = cm.ExecuteBasicAttack(npcCombatant, targetCombatant);
            
            if (result.Success)
            {
                Debug.Log($"[NPCAI] {npcController.NpcName} атакует {targetCombatant.Name}: " +
                          $"урон={result.Damage.FinalDamage:F1}");
            }
        }
        
        /// <summary>
        /// Прямая атака без CombatManager — упрощённый путь урона.
        /// Используется как fallback когда CombatManager не найден.
        /// </summary>
        private void PerformDirectAttack(ICombatant attacker, ICombatant defender)
        {
            if (attacker == null || defender == null || !defender.IsAlive) return;
            
            // Базовый урон от силы атакующего
            int baseDamage = Mathf.Max(1, attacker.Strength / 2);
            
            // Выбираем случайную часть тела
            BodyPartType hitPart = BodyPartType.Torso; // дефолт
            var bodyCtrl = defender.GameObject?.GetComponent<CultivationGame.Body.BodyController>();
            if (bodyCtrl != null)
            {
                hitPart = CultivationGame.Combat.DefenseProcessor.RollBodyPart();
            }
            
            defender.TakeDamage(hitPart, baseDamage);
            
            Debug.Log($"[NPCAI] {npcController?.NpcName ?? "NPC"} атакует {defender.Name} (direct): " +
                      $"урон={baseDamage}, часть={hitPart}");
        }
        
        /// <summary>
        /// Найти Transform цели по ID.
        /// Поддерживает: "player" → GameObject.Find("Player")
        /// Для NPC целей — поиск по NPCController.NpcId
        /// </summary>
        public Transform FindTargetTransform(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return null;
            
            // Проверяем кэш
            if (cachedTargetTransform != null && cachedTargetTransform.gameObject.activeInHierarchy)
            {
                // Проверяем, что кэшированная цель совпадает
                var npcCtrl = cachedTargetTransform.GetComponent<NPCController>();
                if (npcCtrl != null && npcCtrl.NpcId == targetId)
                    return cachedTargetTransform;
                
                var playerCtrl = cachedTargetTransform.GetComponent<Player.PlayerController>();
                if (playerCtrl != null && targetId == "player")
                    return cachedTargetTransform;
            }
            
            Transform found = null;
            
            // Специальный случай: игрок
            if (targetId == "player")
            {
                var playerObj = GameObject.Find("Player");
                if (playerObj != null)
                    found = playerObj.transform;
            }
            else
            {
                // Поиск по NPCController.NpcId
                var allNPCs = FindObjectsByType<NPCController>(FindObjectsSortMode.None);
                foreach (var npc in allNPCs)
                {
                    if (npc.NpcId == targetId && npc.IsAlive)
                    {
                        found = npc.transform;
                        break;
                    }
                }
            }
            
            // Кэшируем результат
            if (found != null)
                cachedTargetTransform = found;
            
            return found;
        }
        
        // === Aggro Radius ===
        
        /// <summary>
        /// Проверить aggro radius — обнаружение игрока в зоне агрессии.
        /// Monster/Enemy атакуют при Attitude.Hostile/Hatred.
        /// Guard/Elder атакуют только при провокации (через Threat).
        /// </summary>
        private void CheckAggroRadius()
        {
            if (state == null) return;
            
            // Не проверяем, если уже в бою или уже атакуем/убегаем
            if (CurrentState == NPCAIState.Attacking || CurrentState == NPCAIState.Fleeing)
                return;
            
            aggroCheckTimer += Time.deltaTime;
            if (aggroCheckTimer < aggroCheckInterval)
                return;
            aggroCheckTimer = 0f;
            
            // Проверяем агрессию только для враждебных NPC
            if (state.Attitude != Attitude.Hostile && state.Attitude != Attitude.Hatred)
                return;
            
            // Поиск игрока через Physics2D.OverlapCircle
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, aggroRange, playerLayerMask);
            
            foreach (var hit in hits)
            {
                var playerCombatant = hit.GetComponent<Player.PlayerController>();
                if (playerCombatant != null && playerCombatant.IsAlive)
                {
                    // Игрок в зоне агрессии!
                    state.TargetId = "player";
                    state.LastKnownPosition = playerCombatant.transform.position;
                    AddThreat("player", 30f); // Базовая угроза при обнаружении
                    
                    // Решаем: атаковать или бежать
                    if (aggressiveness > cautiousness)
                    {
                        ChangeState(NPCAIState.Attacking);
                    }
                    else
                    {
                        ChangeState(NPCAIState.Fleeing);
                    }
                    
                    return;
                }
            }
        }
        
        /// <summary>
        /// Установить слой для обнаружения игрока.
        /// </summary>
        public void SetPlayerLayerMask(LayerMask mask)
        {
            playerLayerMask = mask;
        }
    }
}
