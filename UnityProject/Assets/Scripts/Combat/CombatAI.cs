// ============================================================================
// CombatAI.cs — ИИ принятия боевых решений для NPC
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-05-04 04:42:00 UTC
// Редактировано: 2026-05-07 10:45:00 UTC — ФАЗА 3: MeleeWeapon без оружия → пропуск
// ============================================================================
//
// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  СИСТЕМА НАКАЧКИ ДЛЯ NPC                                                    ║
// ╠═══════════════════════════════════════════════════════════════════════════╣
// ║  NPC использует ту же систему накачки, что и игрок:                         ║
// ║  CombatAI → TechniqueChargeSystem.BeginCharge() → накачка → срабатывание    ║
// ║  NPC решает: какую технику накачать, когда отменить, когда защищаться       ║
// ╚═════════════════════════════════════════════════════════════════════════════╝
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Решение ИИ в бою.
    /// </summary>
    public enum AIDecision
    {
        /// <summary>Базовая атака (без техники)</summary>
        BasicAttack,
        /// <summary>Начать накачку техники</summary>
        ChargeTechnique,
        /// <summary>Продолжить накачку (не прерывать)</summary>
        ContinueCharge,
        /// <summary>Защитная техника</summary>
        UseDefensiveTech,
        /// <summary>Побег</summary>
        Flee,
        /// <summary>Ожидание (перегруппировка)</summary>
        Wait
    }

    /// <summary>
    /// ИИ принятия боевых решений для NPC.
    /// Компонент на NPC, который:
    /// - Решает какую технику использовать (и накачать)
    /// - Решает когда атаковать / защищаться / отступать
    /// - Выбирает цель для атаки
    /// - Использует TechniqueChargeSystem для накачки (как и игрок)
    /// </summary>
    public class CombatAI : MonoBehaviour
    {
        [Header("Настройки ИИ")]
        [SerializeField] private AIPersonality personality;
        [SerializeField] private float decisionInterval = 1f; // Интервал принятия решений (сек)

        // === Runtime ===
        private ICombatant ownerCombatant;
        private TechniqueController techniqueController;
        private TechniqueChargeSystem chargeSystem;
        private ICombatant currentTarget;
        private float lastDecisionTime;
        private AIDecision lastDecision;

        // === Свойства ===
        public AIDecision LastDecision => lastDecision;
        public ICombatant CurrentTarget => currentTarget;

        // === Инициализация ===

        /// <summary>
        /// Инициализировать ИИ.
        /// Вызывается из NPCController.
        /// </summary>
        public void Init(ICombatant combatant, TechniqueController techController)
        {
            ownerCombatant = combatant;
            techniqueController = techController;
            chargeSystem = techController?.ChargeSystem;

            // Если личность не задана — создаём сбалансированную
            if (personality == null)
            {
                personality = AIPersonality.CreateBalanced();
            }

            lastDecisionTime = Time.time;
        }

        // === Принятие решений ===

        /// <summary>
        /// Получить следующее действие ИИ.
        /// Вызывается из CombatManager или NPCController.
        ///
        /// Дерево решений:
        /// 1. Если HP < retreatThreshold → Flee
        /// 2. Если идёт накачка → ContinueCharge (если безопасно)
        /// 3. Если противник в ближней зоне → melee техника или BasicAttack
        /// 4. Если противник в дальней зоне → ranged техника или приблизиться
        /// 5. Если получил урон во время накачки → проверить chargeRiskTolerance
        /// </summary>
        public AIDecision GetNextAction(ICombatant target)
        {
            if (ownerCombatant == null || !ownerCombatant.IsAlive)
                return AIDecision.Wait;

            currentTarget = target;

            // Ограничиваем частоту решений
            if (Time.time - lastDecisionTime < decisionInterval)
                return lastDecision;

            lastDecisionTime = Time.time;

            // 1. Проверка HP — отступление
            if (ownerCombatant.HealthPercent < personality.retreatThreshold * 100f)
            {
                lastDecision = AIDecision.Flee;
                return lastDecision;
            }

            // 2. Если идёт накачка — проверить, продолжать ли
            if (chargeSystem != null && chargeSystem.IsCharging)
            {
                // Проверяем безопасность: если HP упало ниже 50% и низкая толерантность к риску
                if (ownerCombatant.HealthPercent < 50f && 
                    UnityEngine.Random.value > personality.chargeRiskTolerance)
                {
                    // Прерываем накачку — слишком опасно
                    chargeSystem.CancelCharge();
                    lastDecision = AIDecision.UseDefensiveTech;
                    return lastDecision;
                }

                lastDecision = AIDecision.ContinueCharge;
                return lastDecision;
            }

            // 3. Выбираем технику или базовую атаку
            if (target == null || !target.IsAlive)
            {
                lastDecision = AIDecision.Wait;
                return lastDecision;
            }

            float distance = Vector2.Distance(
                (Vector2)ownerCombatant.GameObject.transform.position,
                (Vector2)target.GameObject.transform.position
            );

            bool isMeleeRange = distance <= HitDetector.GetAttackRange(CombatSubtype.MeleeStrike);

            // 4. Если предпочитает техники — ищем подходящую
            if (UnityEngine.Random.value < personality.techniquePreference)
            {
                LearnedTechnique bestTech = FindBestTechnique(isMeleeRange);
                if (bestTech != null && techniqueController.CanUseTechnique(bestTech))
                {
                    // Начинаем накачку!
                    if (chargeSystem != null && chargeSystem.BeginCharge(bestTech))
                    {
                        lastDecision = AIDecision.ChargeTechnique;
                        return lastDecision;
                    }
                }
            }

            // 5. Проверяем защиту
            if (personality.defensiveness > 0.5f && UnityEngine.Random.value < personality.defensiveness)
            {
                // Ищем защитную технику
                LearnedTechnique defenseTech = FindDefensiveTechnique();
                if (defenseTech != null && techniqueController.CanUseTechnique(defenseTech))
                {
                    if (chargeSystem != null && chargeSystem.BeginCharge(defenseTech))
                    {
                        lastDecision = AIDecision.UseDefensiveTech;
                        return lastDecision;
                    }
                }
            }

            // 6. Базовая атака
            lastDecision = AIDecision.BasicAttack;
            return lastDecision;
        }

        /// <summary>
        /// Найти лучшую атакующую технику для текущей ситуации.
        /// ФАЗА 3: MeleeWeapon техники пропускаются если нет оружия.
        /// </summary>
        private LearnedTechnique FindBestTechnique(bool isMeleeRange)
        {
            if (techniqueController == null) return null;

            // ФАЗА 3: Проверяем наличие оружия для MeleeWeapon техник
            var eqCtrl = ownerCombatant?.GameObject?.GetComponent<CultivationGame.Inventory.EquipmentController>();
            bool hasWeapon = eqCtrl != null && eqCtrl.GetMainWeapon() != null;

            LearnedTechnique bestTech = null;
            int bestCapacity = 0;

            foreach (var tech in techniqueController.Techniques)
            {
                if (tech == null || tech.Data == null) continue;
                if (!techniqueController.CanUseTechnique(tech)) continue;

                // ФАЗА 3: MeleeWeapon без оружия → fallback (проверка дублирует CanUseTechnique,
                // но позволяет CombatAI логировать причину пропуска)
                if (tech.Data.combatSubtype == CombatSubtype.MeleeWeapon && !hasWeapon)
                {
                    Debug.Log($"[CombatAI] Пропуск {tech.Data.nameRu}: MeleeWeapon без оружия");
                    continue;
                }

                // Фильтруем по дальности
                bool isTechMelee = tech.Data.combatSubtype == CombatSubtype.MeleeStrike ||
                                   tech.Data.combatSubtype == CombatSubtype.MeleeWeapon;
                bool isTechRanged = HitDetector.IsRangedAttack(tech.Data.combatSubtype);

                if (isMeleeRange && !isTechMelee && !isTechRanged) continue;
                if (!isMeleeRange && isTechMelee) continue;

                // Считаем ёмкость (чем больше — тем лучше)
                int capacity = TechniqueCapacity.CalculateCapacity(
                    tech.Data.techniqueType,
                    tech.Data.combatSubtype,
                    tech.Data.techniqueLevel,
                    tech.Mastery
                );

                if (capacity > bestCapacity)
                {
                    bestCapacity = capacity;
                    bestTech = tech;
                }
            }

            return bestTech;
        }

        /// <summary>
        /// Найти защитную технику.
        /// </summary>
        private LearnedTechnique FindDefensiveTechnique()
        {
            if (techniqueController == null) return null;

            foreach (var tech in techniqueController.Techniques)
            {
                if (tech == null || tech.Data == null) continue;
                if (!techniqueController.CanUseTechnique(tech)) continue;

                if (tech.Data.techniqueType == TechniqueType.Defense)
                    return tech;
            }

            return null;
        }
    }
}
