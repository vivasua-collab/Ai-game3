// ============================================================================
// CombatManager.cs — Центральный менеджер боя
// Cultivation World Simulator
// Версия: 1.1 — Исправлены проблемы code review
// ============================================================================
// Создан: 2026-03-31 14:12:00 UTC
// Редактировано: 2026-04-02 13:51:19 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Состояние боя.
    /// </summary>
    public enum CombatState
    {
        None,           // Боя нет
        Initiating,     // Инициализация
        Active,         // Бой активен
        Paused,         // Пауза
        Ending          // Завершение
    }

    /// <summary>
    /// Результат боя.
    /// </summary>
    public struct CombatResult
    {
        public bool Victory;
        public ICombatant Winner;
        public ICombatant Loser;
        public int TotalDamageDealt;
        public int TotalDamageTaken;
        public float CombatDuration;
        public List<CombatEventData> CombatLog;
    }

    /// <summary>
    /// Результат атаки.
    /// </summary>
    public struct AttackResult
    {
        public bool Success;
        public DamageResult Damage;
        public ICombatant Attacker;
        public ICombatant Defender;
        public string FailReason;

        public static AttackResult Failed(string reason)
        {
            return new AttackResult { Success = false, FailReason = reason };
        }
    }

    /// <summary>
    /// Центральный менеджер боя.
    /// Координирует все боевые взаимодействия между ICombatant.
    ///
    /// Источник: COMBAT_SYSTEM.md
    ///
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  ПОРЯДОК ПРОХОЖДЕНИЯ УРОНА (10 СЛОЁВ)                                      ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║  СЛОЙ 1:  Исходный урон (TechniqueCapacity)                                ║
    /// ║  СЛОЙ 2:  Level Suppression (подавление уровнем)                           ║
    /// ║  СЛОЙ 3:  Определение части тела (DefenseProcessor.RollBodyPart)          ║
    /// ║  СЛОЙ 4:  Активная защита (dodge/parry/block)                              ║
    /// ║  СЛОЙ 5:  Qi Buffer (поглощение Ци)                                        ║
    /// ║  СЛОЙ 6:  Покрытие брони                                                   ║
    /// ║  СЛОЙ 7:  Снижение бронёй                                                  ║
    /// ║  СЛОЙ 8:  Материал тела                                                    ║
    /// ║  СЛОЙ 9:  Распределение по HP (70% красная, 30% чёрная)                    ║
    /// ║  СЛОЙ 10: Последствия (кровотечение, шок, смерть)                          ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public class CombatManager : MonoBehaviour
    {
        #region Singleton

        public static CombatManager Instance { get; private set; }

        #endregion

        #region Fields

        [Header("Settings")]
        [SerializeField] private bool autoEndCombat = true;
        [SerializeField] private float combatTimeout = 300f; // 5 минут

        // === Runtime ===
        private CombatState state = CombatState.None;
        private List<ICombatant> combatants = new List<ICombatant>();
        private ICombatant currentAttacker;
        private ICombatant currentDefender;
        private float combatStartTime;
        private int totalDamageDealt;
        private int totalDamageTaken;

        // === Events ===

        public event Action OnCombatStart;
        public event Action<CombatResult> OnCombatEnd;
        public event Action<ICombatant, ICombatant, DamageResult> OnAttackExecuted;
        public event Action<CombatState> OnStateChanged;

        #endregion

        #region Properties

        public CombatState State => state;
        public bool IsInCombat => state == CombatState.Active;
        public IReadOnlyList<ICombatant> Combatants => combatants;
        public ICombatant CurrentAttacker => currentAttacker;
        public ICombatant CurrentDefender => currentDefender;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (state == CombatState.Active)
            {
                CheckCombatEnd();
                CheckTimeout();
            }
        }

        #endregion

        #region Combat Flow

        /// <summary>
        /// Начать бой между двумя combatants.
        /// </summary>
        public void InitiateCombat(ICombatant attacker, ICombatant defender)
        {
            if (attacker == null || defender == null)
            {
                Debug.LogError("[CombatManager] Cannot initiate combat: null combatant");
                return;
            }

            if (state != CombatState.None)
            {
                Debug.LogWarning("[CombatManager] Combat already in progress");
                return;
            }

            SetState(CombatState.Initiating);

            combatants.Clear();
            combatants.Add(attacker);
            combatants.Add(defender);

            currentAttacker = attacker;
            currentDefender = defender;

            combatStartTime = Time.time;
            totalDamageDealt = 0;
            totalDamageTaken = 0;

            // Подписываемся на события смерти
            attacker.OnDeath += HandleCombatantDeath;
            defender.OnDeath += HandleCombatantDeath;

            SetState(CombatState.Active);

            // Отправляем событие начала боя
            CombatEvents.Dispatch(CombatEventType.CombatStart, attacker, defender);
            OnCombatStart?.Invoke();

            Debug.Log($"[CombatManager] Combat started: {attacker.Name} vs {defender.Name}");
        }

        /// <summary>
        /// Выполнить атаку.
        /// </summary>
        public AttackResult ExecuteAttack(
            ICombatant attacker,
            ICombatant defender,
            int techniqueCapacity,
            AttackerParams attackerParams)
        {
            if (state != CombatState.Active)
            {
                return AttackResult.Failed("Combat not active");
            }

            if (!attacker.IsAlive || !defender.IsAlive)
            {
                return AttackResult.Failed("Combatant is dead");
            }

            // Получаем параметры защищающегося
            DefenderParams defenderParams = defender.GetDefenderParams();

            // Рассчитываем урон по полному пайплайну 10 слоёв
            DamageResult damageResult = DamageCalculator.CalculateDamage(
                techniqueCapacity,
                attackerParams,
                defenderParams
            );

            // Создаём результат атаки
            AttackResult result = new AttackResult
            {
                Success = true,
                Damage = damageResult,
                Attacker = attacker,
                Defender = defender
            };

            // Применяем урон к защищающемуся
            if (damageResult.FinalDamage > 0)
            {
                ApplyDamageToTarget(defender, damageResult);

                totalDamageDealt += (int)damageResult.FinalDamage;
                totalDamageTaken += (int)damageResult.FinalDamage;
            }

            // Отправляем события
            CombatEvents.DispatchDamage(attacker, defender, damageResult);
            OnAttackExecuted?.Invoke(attacker, defender, damageResult);

            // Проверяем смерть
            if (!defender.IsAlive)
            {
                CombatEvents.DispatchDeath(attacker, defender);
                EndCombat(attacker, defender);
            }

            return result;
        }

        /// <summary>
        /// Выполнить атаку техникой.
        /// </summary>
        public AttackResult ExecuteTechniqueAttack(
            ITechniqueUser attacker,
            ICombatant defender,
            LearnedTechnique technique)
        {
            if (technique == null || technique.Data == null)
            {
                return AttackResult.Failed("Invalid technique");
            }

            // Проверяем возможность использования
            if (!attacker.CanUseTechnique(technique))
            {
                return AttackResult.Failed("Cannot use technique");
            }

            // Используем технику
            TechniqueUseResult techResult = attacker.UseTechnique(technique);

            if (!techResult.Success)
            {
                return AttackResult.Failed(techResult.FailReason);
            }

            // Формируем параметры атакующего
            AttackerParams attackerParams = attacker.GetAttackerParams(techResult.Element);
            attackerParams.TechniqueLevel = technique.Data.techniqueLevel;
            attackerParams.TechniqueGrade = technique.Data.grade;
            attackerParams.IsUltimate = technique.Data.isUltimate;
            attackerParams.IsQiTechnique = techResult.Type != TechniqueType.Cultivation;
            attackerParams.CombatSubtype = technique.Data.combatSubtype;

            // Выполняем атаку
            AttackResult result = ExecuteAttack(attacker, defender, techResult.Capacity, attackerParams);

            // Отправляем событие использования техники
            if (result.Success)
            {
                CombatEvents.DispatchTechniqueUsed(attacker, techResult);
            }

            return result;
        }

        /// <summary>
        /// Выполнить базовую атаку (без техники).
        /// </summary>
        public AttackResult ExecuteBasicAttack(ICombatant attacker, ICombatant defender)
        {
            // Базовая атака - множитель 64 (melee_strike)
            int baseCapacity = TechniqueCapacity.GetBaseCapacity(TechniqueType.Combat, CombatSubtype.MeleeStrike);

            AttackerParams attackerParams = attacker.GetAttackerParams();

            return ExecuteAttack(attacker, defender, baseCapacity, attackerParams);
        }

        /// <summary>
        /// Завершить бой.
        /// </summary>
        public void EndCombat(ICombatant winner, ICombatant loser)
        {
            if (state == CombatState.None || state == CombatState.Ending)
                return;

            SetState(CombatState.Ending);

            // Отписываемся от событий смерти
            foreach (var combatant in combatants)
            {
                combatant.OnDeath -= HandleCombatantDeath;
            }

            CombatResult result = new CombatResult
            {
                Victory = winner != null,
                Winner = winner,
                Loser = loser,
                TotalDamageDealt = totalDamageDealt,
                TotalDamageTaken = totalDamageTaken,
                CombatDuration = Time.time - combatStartTime,
                CombatLog = CombatLog.GetEntries(100)
            };

            SetState(CombatState.None);

            // Отправляем событие окончания боя
            CombatEvents.Dispatch(CombatEventType.CombatEnd, winner, null, winner != null ? 1f : 0f);
            OnCombatEnd?.Invoke(result);

            Debug.Log($"[CombatManager] Combat ended. Winner: {winner?.Name ?? "None"}");
        }

        /// <summary>
        /// Принудительно завершить бой.
        /// </summary>
        public void ForceEndCombat()
        {
            if (state == CombatState.None)
                return;

            EndCombat(null, null);
        }

        #endregion

        #region Damage Application

        /// <summary>
        /// Применить урон к цели.
        /// Проходит через Qi Buffer и BodyController.
        /// </summary>
        private void ApplyDamageToTarget(ICombatant target, DamageResult damage)
        {
            // Урон уже прошёл через Qi Buffer в DamageCalculator
            // Теперь применяем к телу

            // Если урон был поглощён Ци - тратим Ци
            if (damage.QiConsumed > 0)
            {
                target.SpendQi(damage.QiConsumed);
            }

            // Применяем урон к части тела
            if (damage.FinalDamage > 0)
            {
                target.TakeDamage(damage.HitPart, damage.FinalDamage);
            }
        }

        #endregion

        #region Event Handlers

        private void HandleCombatantDeath()
        {
            // FIX: Проверяем состояние боя, чтобы избежать повторного вызова EndCombat
            if (state != CombatState.Active)
            {
                Debug.LogWarning("[CombatManager] HandleCombatantDeath called but combat not active");
                return;
            }
            
            // Находим кто умер
            ICombatant dead = null;
            ICombatant alive = null;

            foreach (var c in combatants)
            {
                if (!c.IsAlive)
                    dead = c;
                else
                    alive = c;
            }

            if (dead != null && alive != null)
            {
                EndCombat(alive, dead);
            }
            else if (dead != null)
            {
                // Все умерли или никто не выжил
                EndCombat(null, dead);
            }
        }

        #endregion

        #region State Management

        private void SetState(CombatState newState)
        {
            if (state == newState)
                return;

            state = newState;
            OnStateChanged?.Invoke(state);
        }

        private void CheckCombatEnd()
        {
            if (!autoEndCombat)
                return;

            // Проверяем живы ли combatants
            foreach (var c in combatants)
            {
                if (!c.IsAlive)
                {
                    ICombatant winner = combatants.Find(x => x != c && x.IsAlive);
                    EndCombat(winner, c);
                    return;
                }
            }
        }

        private void CheckTimeout()
        {
            if (Time.time - combatStartTime > combatTimeout)
            {
                Debug.LogWarning("[CombatManager] Combat timeout reached");
                ForceEndCombat();
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Проверить, находится ли combatant в бою.
        /// </summary>
        public bool IsCombatantInCombat(ICombatant combatant)
        {
            return combatants.Contains(combatant);
        }

        /// <summary>
        /// Получить противника для combatant.
        /// </summary>
        public ICombatant GetOpponent(ICombatant combatant)
        {
            foreach (var c in combatants)
            {
                if (c != combatant)
                    return c;
            }
            return null;
        }

        #endregion
    }
}
