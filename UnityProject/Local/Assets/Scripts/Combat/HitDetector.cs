// ============================================================================
// HitDetector.cs — Определение попаданий
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-31 14:14:00 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Результат определения попадания.
    /// </summary>
    public struct HitDetectionResult
    {
        public bool HasLineOfSight;     // Есть прямая видимость
        public bool IsInRange;          // В пределах дальности
        public float Distance;          // Расстояние до цели
        public ICombatant Target;       // Найденная цель
        public BodyPartType SuggestedPart; // Предлагаемая часть тела
        public Vector3 HitPoint;        // Точка попадания
        public string FailReason;       // Причина неудачи
    }

    /// <summary>
    /// Тип цели.
    /// </summary>
    public enum TargetType
    {
        None,
        Self,
        Ally,
        Enemy,
        Neutral,
        Any
    }

    /// <summary>
    /// Определение попаданий и целей.
    ///
    /// Источник: ALGORITHMS.md §8 "Шансы попадания"
    ///
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  ШАНСЫ ПОПАДАНИЯ                                                           ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║                                                                            ║
    /// ║  Базовый шанс: зависит от типа атаки                                       ║
    /// ║  - Ближний бой: 95%                                                        ║
    /// ║  - Дальнобойная атака: зависит от расстояния                               ║
    /// ║                                                                            ║
    /// ║  Модификаторы:                                                             ║
    /// ║  - Размер цели: ±10-30%                                                    ║
    /// ║  - Позиция: ±10-25%                                                        ║
    /// ║  - Оружие: зависит от типа                                                 ║
    /// ║                                                                            ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public static class HitDetector
    {
        // === Constants ===

        private const float DEFAULT_ATTACK_RANGE = 2f;
        private const float RANGED_ATTACK_RANGE = 20f;
        private const float LINE_OF_SIGHT_DISTANCE = 100f;
        private const float MELEE_BASE_HIT_CHANCE = 0.95f;
        private const float RANGED_BASE_HIT_CHANCE = 0.80f;

        // === Layer Masks ===

        private static LayerMask obstacleLayerMask = -1; // Default: everything except triggers

        /// <summary>
        /// Инициализировать маски слоёв.
        /// </summary>
        static HitDetector()
        {
            // Исключаем триггеры из проверки препятствий
            obstacleLayerMask = ~0;
        }

        #region Target Detection

        /// <summary>
        /// Найти ближайшую цель.
        /// </summary>
        public static ICombatant FindNearestTarget(
            ICombatant attacker,
            float maxRange = DEFAULT_ATTACK_RANGE,
            TargetType targetType = TargetType.Enemy)
        {
            if (attacker == null)
                return null;

            Vector3 attackerPos = attacker.GameObject.transform.position;

            // Ищем все ICombatant в радиусе
            Collider[] colliders = Physics.OverlapSphere(attackerPos, maxRange);

            ICombatant nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                ICombatant target = collider.GetComponent<ICombatant>();

                if (target == null || target == attacker)
                    continue;

                if (!target.IsAlive)
                    continue;

                // Проверяем тип цели
                if (!IsValidTargetType(attacker, target, targetType))
                    continue;

                // Проверяем расстояние
                float distance = Vector3.Distance(
                    attackerPos,
                    target.GameObject.transform.position
                );

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = target;
                }
            }

            return nearestTarget;
        }

        /// <summary>
        /// Найти все цели в радиусе.
        /// </summary>
        public static List<ICombatant> FindTargetsInRange(
            ICombatant attacker,
            float range,
            TargetType targetType = TargetType.Enemy)
        {
            List<ICombatant> targets = new List<ICombatant>();

            if (attacker == null)
                return targets;

            Vector3 attackerPos = attacker.GameObject.transform.position;
            Collider[] colliders = Physics.OverlapSphere(attackerPos, range);

            foreach (var collider in colliders)
            {
                ICombatant target = collider.GetComponent<ICombatant>();

                if (target == null || target == attacker)
                    continue;

                if (!target.IsAlive)
                    continue;

                if (IsValidTargetType(attacker, target, targetType))
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        /// <summary>
        /// Проверить тип цели.
        /// </summary>
        private static bool IsValidTargetType(ICombatant attacker, ICombatant target, TargetType targetType)
        {
            // TODO: Реализовать систему фракций для определения Ally/Enemy

            return targetType switch
            {
                TargetType.None => false,
                TargetType.Self => target == attacker,
                TargetType.Any => true,
                TargetType.Enemy => target != attacker, // Упрощённо
                TargetType.Ally => false, // TODO: Проверка фракции
                TargetType.Neutral => false, // TODO: Проверка фракции
                _ => false
            };
        }

        #endregion

        #region Line of Sight

        /// <summary>
        /// Проверить прямую видимость.
        /// </summary>
        public static bool HasLineOfSight(ICombatant attacker, ICombatant target)
        {
            if (attacker == null || target == null)
                return false;

            Vector3 attackerPos = attacker.GameObject.transform.position;
            Vector3 targetPos = target.GameObject.transform.position;

            Vector3 direction = (targetPos - attackerPos).normalized;
            float distance = Vector3.Distance(attackerPos, targetPos);

            // Raycast от атакующего к цели
            if (Physics.Raycast(attackerPos, direction, out RaycastHit hit, distance, obstacleLayerMask))
            {
                // Если raycast попал в что-то до цели
                return hit.collider.gameObject == target.GameObject ||
                       hit.collider.transform.IsChildOf(target.GameObject.transform);
            }

            // Raycast не попал ни во что - цель не закрыта препятствием
            return true;
        }

        /// <summary>
        /// Проверить прямую видимость к точке.
        /// </summary>
        public static bool HasLineOfSightToPoint(ICombatant attacker, Vector3 point)
        {
            if (attacker == null)
                return false;

            Vector3 attackerPos = attacker.GameObject.transform.position;
            Vector3 direction = (point - attackerPos).normalized;
            float distance = Vector3.Distance(attackerPos, point);

            return !Physics.Raycast(attackerPos, direction, distance, obstacleLayerMask);
        }

        #endregion

        #region Hit Detection

        /// <summary>
        /// Проверить возможность атаки.
        /// </summary>
        public static HitDetectionResult CheckAttackFeasibility(
            ICombatant attacker,
            ICombatant target,
            CombatSubtype attackType = CombatSubtype.MeleeStrike)
        {
            HitDetectionResult result = new HitDetectionResult
            {
                Target = target,
                FailReason = ""
            };

            if (attacker == null || target == null)
            {
                result.FailReason = "Invalid combatant";
                return result;
            }

            // Проверка расстояния
            float range = GetAttackRange(attackType);
            result.Distance = Vector3.Distance(
                attacker.GameObject.transform.position,
                target.GameObject.transform.position
            );
            result.IsInRange = result.Distance <= range;

            if (!result.IsInRange)
            {
                result.FailReason = $"Target out of range ({result.Distance:F1} > {range:F1})";
                return result;
            }

            // Проверка прямой видимости
            result.HasLineOfSight = HasLineOfSight(attacker, target);

            if (!result.HasLineOfSight)
            {
                result.FailReason = "No line of sight";
                return result;
            }

            // Вычисляем точку попадания
            result.HitPoint = target.GameObject.transform.position;

            // Предлагаем часть тела
            result.SuggestedPart = DefenseProcessor.RollBodyPart();

            return result;
        }

        /// <summary>
        /// Получить дальность атаки по типу.
        /// </summary>
        public static float GetAttackRange(CombatSubtype attackType)
        {
            return attackType switch
            {
                CombatSubtype.MeleeStrike => DEFAULT_ATTACK_RANGE,
                CombatSubtype.MeleeWeapon => DEFAULT_ATTACK_RANGE + 1f,
                CombatSubtype.RangedProjectile => RANGED_ATTACK_RANGE,
                CombatSubtype.RangedBeam => RANGED_ATTACK_RANGE * 1.5f,
                CombatSubtype.RangedAoe => RANGED_ATTACK_RANGE,
                _ => DEFAULT_ATTACK_RANGE
            };
        }

        #endregion

        #region Hit Chance Calculation

        /// <summary>
        /// Рассчитать шанс попадания.
        ///
        /// Источник: ALGORITHMS.md §8 "Шансы попадания"
        /// </summary>
        public static float CalculateHitChance(
            ICombatant attacker,
            ICombatant target,
            CombatSubtype attackType)
        {
            float baseChance = IsRangedAttack(attackType)
                ? RANGED_BASE_HIT_CHANCE
                : MELEE_BASE_HIT_CHANCE;

            // Модификатор от ловкости атакующего
            float agilityMod = (attacker.Agility - 10) * 0.01f;
            baseChance += agilityMod;

            // Модификатор размера цели (если есть)
            // TODO: Добавить SizeClass после интеграции

            // Модификатор расстояния для дальнобойных атак
            if (IsRangedAttack(attackType))
            {
                float distance = Vector3.Distance(
                    attacker.GameObject.transform.position,
                    target.GameObject.transform.position
                );
                float maxRange = GetAttackRange(attackType);
                float distanceRatio = distance / maxRange;

                // Шанс падает с расстоянием
                baseChance *= (1f - distanceRatio * 0.3f);
            }

            return Mathf.Clamp01(baseChance);
        }

        /// <summary>
        /// Проверить попадание.
        /// </summary>
        public static bool RollForHit(float hitChance)
        {
            return UnityEngine.Random.value < hitChance;
        }

        /// <summary>
        /// Проверить, является ли атака дальнобойной.
        /// </summary>
        public static bool IsRangedAttack(CombatSubtype attackType)
        {
            return attackType == CombatSubtype.RangedProjectile ||
                   attackType == CombatSubtype.RangedBeam ||
                   attackType == CombatSubtype.RangedAoe;
        }

        #endregion

        #region Area of Effect

        /// <summary>
        /// Найти все цели в области.
        /// </summary>
        public static List<ICombatant> FindTargetsInArea(
            Vector3 center,
            float radius,
            ICombatant exclude = null)
        {
            List<ICombatant> targets = new List<ICombatant>();
            Collider[] colliders = Physics.OverlapSphere(center, radius);

            foreach (var collider in colliders)
            {
                ICombatant target = collider.GetComponent<ICombatant>();

                if (target == null)
                    continue;

                if (exclude != null && target == exclude)
                    continue;

                if (!target.IsAlive)
                    continue;

                targets.Add(target);
            }

            return targets;
        }

        /// <summary>
        /// Найти цели в конусе.
        /// </summary>
        public static List<ICombatant> FindTargetsInCone(
            ICombatant attacker,
            Vector3 direction,
            float range,
            float angleDegrees)
        {
            List<ICombatant> targets = new List<ICombatant>();

            if (attacker == null)
                return targets;

            Vector3 attackerPos = attacker.GameObject.transform.position;
            List<ICombatant> potentialTargets = FindTargetsInRange(attacker, range, TargetType.Enemy);

            float halfAngle = angleDegrees / 2f;

            foreach (var target in potentialTargets)
            {
                Vector3 toTarget = (target.GameObject.transform.position - attackerPos).normalized;
                float angle = Vector3.Angle(direction, toTarget);

                if (angle <= halfAngle)
                {
                    targets.Add(target);
                }
            }

            return targets;
        }

        #endregion
    }
}
