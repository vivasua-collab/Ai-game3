// ============================================================================
// NPCAI.cs — AI принятие решений
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-03-31 10:38:00 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

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
        [SerializeField] private float detectionRange = 15f;
        
        [Header("Personality Modifiers")]
        [SerializeField] private float aggressiveness = 0.5f;    // 0-1
        [SerializeField] private float cautiousness = 0.5f;      // 0-1
        [SerializeField] private float socialness = 0.5f;        // 0-1
        [SerializeField] private float ambition = 0.5f;          // 0-1
        
        // === Runtime ===
        private NPCController npcController;
        private NPCState state;
        private float decisionTimer;
        private List<string> knownTargets = new List<string>();
        private Dictionary<string, float> threatLevels = new Dictionary<string, float>();
        
        // === Patrol ===
        private Vector3[] patrolPoints;
        private int currentPatrolIndex;
        
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
        }
        
        private void Start()
        {
            if (npcController != null)
                state = npcController.State;
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
            
            float healthPercent = (float)state.CurrentHealth / state.MaxHealth;
            return healthPercent <= fleeHealthThreshold && cautiousness > aggressiveness;
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
        
        private void DecideNormalBehavior()
        {
            // На основе личности выбираем действие
            float rand = UnityEngine.Random.value;
            
            if (rand < socialness * 0.3f)
            {
                ChangeState(NPCAIState.Wandering);
            }
            else if (rand < socialness * 0.5f)
            {
                ChangeState(NPCAIState.Talking);
            }
            else if (rand < ambition * 0.4f)
            {
                ChangeState(NPCAIState.Cultivating);
            }
            else if (rand < 0.6f)
            {
                ChangeState(NPCAIState.Resting);
            }
            else if (rand < 0.8f)
            {
                ChangeState(NPCAIState.Working);
            }
            else
            {
                ChangeState(NPCAIState.Idle);
            }
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
            // Ничего не делаем, ждём
            if (state.StateTimer > 5f)
            {
                ChangeState(NPCAIState.Wandering);
            }
        }
        
        private void ExecuteWandering()
        {
            // Случайное блуждание
            // Реальное движение будет реализовано с NavMesh
        }
        
        private void ExecutePatrolling()
        {
            if (patrolPoints == null || patrolPoints.Length == 0) return;
            
            // Переход к следующей точке патруля
            // Реальное движение с NavMesh
        }
        
        private void ExecuteFollowing()
        {
            if (string.IsNullOrEmpty(state.TargetId)) return;
            
            // Следование за целью
        }
        
        private void ExecuteFleeing()
        {
            // Бегство от угрозы
            if (state.StateTimer > 10f)
            {
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
            
            // Атака цели через CombatSystem
        }
        
        private void ExecuteCultivating()
        {
            // Культивация — восстановление Ци
            if (state.StateTimer > 30f)
            {
                // Восстанавливаем Ци
                state.CurrentQi = Mathf.Min(state.MaxQi, state.CurrentQi + 10);
                ChangeState(NPCAIState.Idle);
            }
        }
        
        private void ExecuteResting()
        {
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
        
        /// <summary>
        /// Модифицировать личность на основе характера.
        /// </summary>
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
    }
}
