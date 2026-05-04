// ============================================================================
// HitDetector.cs — Определение попаданий (2D)
// Cultivation World Simulator
// Версия: 2.0 — Миграция Physics 3D → Physics2D
// ============================================================================
// Создан: 2026-03-31 14:14:00 UTC
// Редактировано: 2026-05-04 04:38:00 UTC — Миграция на Physics2D
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
        public Vector2 HitPoint;        // Точка попадания (2D)
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
    /// Определение попаданий и целей (2D).
    ///
    /// Источник: ALGORITHMS.md §8 "Шансы попадания"
    ///
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  ВЕРСИЯ 2.0 — ПОЛНАЯ МИГРАЦИЯ НА PHYSICS2D                                ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║  Physics.OverlapSphere → Physics2D.OverlapCircleAll                       ║
    /// ║  Physics.Raycast → Physics2D.Raycast                                      ║
    /// ║  Collider → Collider2D                                                    ║
    /// ║  RaycastHit → RaycastHit2D                                                ║
    /// ║  Vector3 → Vector2 (позиции/направления)                                  ║
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

        private static LayerMask obstacleLayerMask = -1;

        /// <summary>
        /// Инициализировать маски слоёв.
        /// </summary>
        static HitDetector()
        {
            obstacleLayerMask = ~0;
        }

        #region Target Detection

        /// <summary>
        /// Найти ближайшую цель (2D).
        /// </summary>
        public static ICombatant FindNearestTarget(
            ICombatant attacker,
            float maxRange = DEFAULT_ATTACK_RANGE,
            TargetType targetType = TargetType.Enemy)
        {
            if (attacker == null)
                return null;

            Vector2 attackerPos = (Vector2)attacker.GameObject.transform.position;

            // Ищем все ICombatant в радиусе (2D)
            Collider2D[] colliders = Physics2D.OverlapCircleAll(attackerPos, maxRange);

            ICombatant nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (var collider in colliders)
            {
                ICombatant target = collider.GetComponent<ICombatant>();

                if (target == null || target == attacker)
                    continue;

                if (!target.IsAlive)
                    continue;

                if (!IsValidTargetType(attacker, target, targetType))
                    continue;

                float distance = Vector2.Distance(
                    attackerPos,
                    (Vector2)target.GameObject.transform.position
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
        /// Найти все цели в радиусе (2D).
        /// </summary>
        public static List<ICombatant> FindTargetsInRange(
            ICombatant attacker,
            float range,
            TargetType targetType = TargetType.Enemy)
        {
            List<ICombatant> targets = new List<ICombatant>();

            if (attacker == null)
                return targets;

            Vector2 attackerPos = (Vector2)attacker.GameObject.transform.position;
            Collider2D[] colliders = Physics2D.OverlapCircleAll(attackerPos, range);

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
            return targetType switch
            {
                TargetType.None => false,
                TargetType.Self => target == attacker,
                TargetType.Any => true,
                TargetType.Enemy => target != attacker,
                TargetType.Ally => IsValidAlly(attacker, target),
                TargetType.Neutral => IsValidNeutral(attacker, target),
                _ => false
            };
        }
        
        private static bool IsValidAlly(ICombatant attacker, ICombatant target)
        {
            return false; // Без FactionController пока нет способа определить
        }
        
        private static bool IsValidNeutral(ICombatant attacker, ICombatant target)
        {
            return false;
        }

        #endregion

        #region Line of Sight

        /// <summary>
        /// Проверить прямую видимость (2D).
        /// </summary>
        public static bool HasLineOfSight(ICombatant attacker, ICombatant target)
        {
            if (attacker == null || target == null)
                return false;

            Vector2 attackerPos = (Vector2)attacker.GameObject.transform.position;
            Vector2 targetPos = (Vector2)target.GameObject.transform.position;

            Vector2 direction = (targetPos - attackerPos).normalized;
            float distance = Vector2.Distance(attackerPos, targetPos);

            // Raycast 2D от атакующего к цели
            RaycastHit2D hit = Physics2D.Raycast(attackerPos, direction, distance, obstacleLayerMask);

            if (hit.collider != null)
            {
                return hit.collider.gameObject == target.GameObject ||
                       hit.collider.transform.IsChildOf(target.GameObject.transform);
            }

            return true;
        }

        /// <summary>
        /// Проверить прямую видимость к точке (2D).
        /// </summary>
        public static bool HasLineOfSightToPoint(ICombatant attacker, Vector2 point)
        {
            if (attacker == null)
                return false;

            Vector2 attackerPos = (Vector2)attacker.GameObject.transform.position;
            Vector2 direction = (point - attackerPos).normalized;
            float distance = Vector2.Distance(attackerPos, point);

            RaycastHit2D hit = Physics2D.Raycast(attackerPos, direction, distance, obstacleLayerMask);
            return hit.collider == null;
        }

        #endregion

        #region Hit Detection

        /// <summary>
        /// Проверить возможность атаки (2D).
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

            // Проверка расстояния (2D)
            float range = GetAttackRange(attackType);
            result.Distance = Vector2.Distance(
                (Vector2)attacker.GameObject.transform.position,
                (Vector2)target.GameObject.transform.position
            );
            result.IsInRange = result.Distance <= range;

            if (!result.IsInRange)
            {
                result.FailReason = $"Target out of range ({result.Distance:F1} > {range:F1})";
                return result;
            }

            // Проверка прямой видимости (2D)
            result.HasLineOfSight = HasLineOfSight(attacker, target);

            if (!result.HasLineOfSight)
            {
                result.FailReason = "No line of sight";
                return result;
            }

            // Вычисляем точку попадания (2D)
            result.HitPoint = (Vector2)target.GameObject.transform.position;

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
        /// Рассчитать шанс попадания (2D).
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

            // Модификатор размера цели (2D коллайдер)
            float sizeModifier = GetSizeModifier(target);
            baseChance += sizeModifier;

            // Модификатор расстояния для дальнобойных атак
            if (IsRangedAttack(attackType))
            {
                float distance = Vector2.Distance(
                    (Vector2)attacker.GameObject.transform.position,
                    (Vector2)target.GameObject.transform.position
                );
                float maxRange = GetAttackRange(attackType);
                float distanceRatio = distance / maxRange;

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
        
        /// <summary>
        /// Получить модификатор размера цели для шанса попадания (2D).
        /// </summary>
        private static float GetSizeModifier(ICombatant target)
        {
            if (target?.GameObject == null) return 0f;
            
            // Пытаемся получить размер из 2D коллайдера
            var collider2D = target.GameObject.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                float size = collider2D.bounds.size.magnitude;
                if (size > 1.5f) return Mathf.Min(0.3f, (size - 1.5f) * 0.1f);
                if (size < 0.7f) return Mathf.Max(-0.3f, (size - 0.7f) * 0.2f);
            }
            
            return 0f;
        }

        #endregion

        #region Area of Effect

        /// <summary>
        /// Найти все цели в области (2D).
        /// </summary>
        public static List<ICombatant> FindTargetsInArea(
            Vector2 center,
            float radius,
            ICombatant exclude = null)
        {
            List<ICombatant> targets = new List<ICombatant>();
            Collider2D[] colliders = Physics2D.OverlapCircleAll(center, radius);

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
        /// Найти цели в конусе (2D).
        /// </summary>
        public static List<ICombatant> FindTargetsInCone(
            ICombatant attacker,
            Vector2 direction,
            float range,
            float angleDegrees)
        {
            List<ICombatant> targets = new List<ICombatant>();

            if (attacker == null)
                return targets;

            Vector2 attackerPos = (Vector2)attacker.GameObject.transform.position;
            List<ICombatant> potentialTargets = FindTargetsInRange(attacker, range, TargetType.Enemy);

            float halfAngle = angleDegrees / 2f;

            foreach (var target in potentialTargets)
            {
                Vector2 toTarget = ((Vector2)target.GameObject.transform.position - attackerPos).normalized;
                float angle = Vector2.Angle(direction, toTarget);

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
