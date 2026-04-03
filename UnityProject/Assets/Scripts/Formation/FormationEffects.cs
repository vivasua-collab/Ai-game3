// ============================================================================
// FormationEffects.cs — Применение эффектов формации
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 13:40:00 UTC
// ============================================================================
//
// Источник: docs/FORMATION_SYSTEM.md
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;

namespace CultivationGame.Formation
{
    /// <summary>
    /// Статический класс для применения эффектов формации.
    /// 
    /// ┌─────────────────────────────────────────────────────────────────────────┐
    /// │                     ТИПЫ ЭФФЕКТОВ ФОРМАЦИИ                              │
    /// ├─────────────────────────────────────────────────────────────────────────┤
    /// │                                                                          │
    /// │   Buff        → Усиление характеристик союзников                        │
    /// │   Debuff      → Ослабление врагов                                        │
    │ │   Damage      → Периодический урон врагам                               │
    /// │   Heal        → Исцеление союзников                                      │
    /// │   Control     → Контроль (заморозка, замедление, стан)                   │
    /// │   Shield      → Защитный щит союзникам                                   │
    /// │   Summon      → Призыв существ                                           │
    /// │                                                                          │
    /// │   Применение:                                                             │
    /// │   - На входе в зону (applyOnEnter)                                       │
    /// │   - Периодически (tickInterval)                                          │
    /// │   - Снятие при выходе (removeOnExit)                                     │
    /// │                                                                          │
    /// └─────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public static class FormationEffects
    {
        #region Target Detection

        /// <summary>
        /// Найти цели в радиусе.
        /// </summary>
        /// <param name="center">Центр поиска</param>
        /// <param name="radius">Радиус</param>
        /// <param name="layerMask">Слой для поиска</param>
        /// <returns>Массив коллайдеров</returns>
        public static Collider2D[] FindTargets(Vector2 center, float radius, LayerMask layerMask)
        {
            Collider2D[] results = new Collider2D[100];
            int count = Physics2D.OverlapCircleNonAlloc(center, radius, results, layerMask);

            Collider2D[] validResults = new Collider2D[count];
            Array.Copy(results, validResults, count);

            return validResults;
        }

        /// <summary>
        /// Определить, является ли цель союзником.
        /// </summary>
        /// <param name="target">Цель</param>
        /// <param name="owner">Владелец формации</param>
        /// <returns>True если союзник</returns>
        public static bool IsAlly(GameObject target, GameObject owner)
        {
            if (target == null || owner == null) return false;

            // Сам владелец — союзник
            if (target == owner) return true;

            // Проверяем фракцию/команду
            var targetFaction = target.GetComponent<World.FactionController>();
            var ownerFaction = owner.GetComponent<World.FactionController>();

            if (targetFaction != null && ownerFaction != null)
            {
                return targetFaction.IsAlly(ownerFaction.CurrentFactionId);
            }

            // Проверяем тег
            if (target.CompareTag("Player") || target.CompareTag("Ally"))
                return true;

            if (target.CompareTag("Enemy"))
                return false;

            // По умолчанию — нейтрал
            return false;
        }

        #endregion

        #region Effect Application

        /// <summary>
        /// Применить список эффектов к цели.
        /// </summary>
        /// <param name="target">Цель</param>
        /// <param name="effects">Список эффектов</param>
        /// <param name="source">Источник (формация)</param>
        public static void ApplyEffects(GameObject target, List<FormationEffect> effects, GameObject source)
        {
            if (target == null || effects == null) return;

            foreach (var effect in effects)
            {
                ApplyEffect(target, effect, source);
            }
        }

        /// <summary>
        /// Применить один эффект к цели.
        /// </summary>
        public static void ApplyEffect(GameObject target, FormationEffect effect, GameObject source)
        {
            if (target == null || effect == null) return;

            switch (effect.effectType)
            {
                case FormationEffectType.Buff:
                    ApplyBuff(target, effect);
                    break;

                case FormationEffectType.Debuff:
                    ApplyDebuff(target, effect);
                    break;

                case FormationEffectType.Damage:
                    ApplyDamage(target, effect, source);
                    break;

                case FormationEffectType.Heal:
                    ApplyHeal(target, effect);
                    break;

                case FormationEffectType.Control:
                    ApplyControl(target, effect);
                    break;

                case FormationEffectType.Shield:
                    ApplyShield(target, effect);
                    break;

                case FormationEffectType.Summon:
                    ApplySummon(target, effect, source);
                    break;
            }

            // Визуальный эффект
            if (effect.vfxPrefab != null)
            {
                var vfx = UnityEngine.Object.Instantiate(effect.vfxPrefab, target.transform.position, Quaternion.identity);
                UnityEngine.Object.Destroy(vfx, 2f);
            }

            // Звуковой эффект
            if (effect.soundEffect != null)
            {
                AudioSource.PlayClipAtPoint(effect.soundEffect, target.transform.position);
            }
        }

        #endregion

        #region Buff Application

        /// <summary>
        /// Применить бафф к цели.
        /// </summary>
        public static void ApplyBuff(GameObject target, FormationEffect effect)
        {
            if (effect.buffType == BuffType.None) return;

            // Проверяем наличие системы баффов
            var buffManager = target.GetComponent<BuffManager>();
            if (buffManager != null)
            {
                buffManager.AddBuff(effect.buffType, effect.value, effect.isPercentage, effect.tickInterval);
                return;
            }

            // Fallback: прямое применение
            ApplyBuffFallback(target, effect);
        }

        /// <summary>
        /// Fallback применение баффа (без BuffManager).
        /// </summary>
        private static void ApplyBuffFallback(GameObject target, FormationEffect effect)
        {
            // Временное решение для тестирования
            // Реальная система должна использовать BuffManager
            Debug.Log($"[FormationEffects] Buff {effect.buffType} +{effect.value}{(effect.isPercentage ? "%" : "")} applied to {target.name}");
        }

        #endregion

        #region Debuff Application

        /// <summary>
        /// Применить дебафф к цели.
        /// </summary>
        public static void ApplyDebuff(GameObject target, FormationEffect effect)
        {
            if (effect.buffType == BuffType.None) return;

            var buffManager = target.GetComponent<BuffManager>();
            if (buffManager != null)
            {
                // Дебафф = отрицательный бафф
                buffManager.AddDebuff(effect.buffType, effect.value, effect.isPercentage, effect.tickInterval);
                return;
            }

            Debug.Log($"[FormationEffects] Debuff {effect.buffType} -{effect.value}{(effect.isPercentage ? "%" : "")} applied to {target.name}");
        }

        #endregion

        #region Damage Application

        /// <summary>
        /// Нанести урон цели.
        /// </summary>
        public static void ApplyDamage(GameObject target, FormationEffect effect, GameObject source)
        {
            if (effect.tickValue <= 0) return;

            // Проверяем наличие боевой системы
            var combatant = target.GetComponent<Combatant>();
            if (combatant != null)
            {
                combatant.TakeDamage(effect.tickValue, DamageType.Elemental, source);
                return;
            }

            // Fallback: прямое нанесение урона
            ApplyDamageFallback(target, effect, source);
        }

        /// <summary>
        /// Fallback нанесение урона.
        /// </summary>
        private static void ApplyDamageFallback(GameObject target, FormationEffect effect, GameObject source)
        {
            // Проверяем BodyController
            var body = target.GetComponent<Body.BodyController>();
            if (body != null)
            {
                body.ApplyDamage(effect.tickValue);
                return;
            }

            // Проверяем QiController (урон по Ци)
            var qi = target.GetComponent<Qi.QiController>();
            if (qi != null)
            {
                qi.SpendQi(effect.tickValue);
            }

            Debug.Log($"[FormationEffects] Damage {effect.tickValue} applied to {target.name}");
        }

        #endregion

        #region Heal Application

        /// <summary>
        /// Исцелить цель.
        /// </summary>
        public static void ApplyHeal(GameObject target, FormationEffect effect)
        {
            if (effect.tickValue <= 0) return;

            // Исцеление Ци
            var qi = target.GetComponent<Qi.QiController>();
            if (qi != null)
            {
                qi.AddQi(effect.tickValue);
            }

            // Исцеление тела
            var body = target.GetComponent<Body.BodyController>();
            if (body != null)
            {
                body.Heal(effect.tickValue);
            }

            Debug.Log($"[FormationEffects] Heal {effect.tickValue} applied to {target.name}");
        }

        #endregion

        #region Control Application

        /// <summary>
        /// Применить контроль к цели.
        /// </summary>
        public static void ApplyControl(GameObject target, FormationEffect effect)
        {
            if (effect.controlType == ControlType.None) return;

            // Проверяем наличие системы контроля
            var controlReceiver = target.GetComponent<IControlReceiver>();
            if (controlReceiver != null)
            {
                controlReceiver.ApplyControl(effect.controlType, effect.controlDuration);
                return;
            }

            // Fallback: прямое применение
            ApplyControlFallback(target, effect);
        }

        /// <summary>
        /// Fallback применение контроля.
        /// </summary>
        private static void ApplyControlFallback(GameObject target, FormationEffect effect)
        {
            switch (effect.controlType)
            {
                case ControlType.Freeze:
                    // Останавливаем движение
                    var mover = target.GetComponent<UnityEngine.AI.NavMeshAgent>();
                    if (mover != null) mover.isStopped = true;

                    var rb = target.GetComponent<Rigidbody2D>();
                    if (rb != null) rb.simulated = false;
                    break;

                case ControlType.Slow:
                    // Замедляем движение
                    var rbSlow = target.GetComponent<Rigidbody2D>();
                    if (rbSlow != null) rbSlow.linearVelocity *= 0.5f;
                    break;

                case ControlType.Root:
                    // Обездвиживаем
                    var rbRoot = target.GetComponent<Rigidbody2D>();
                    if (rbRoot != null) rbRoot.linearVelocity = Vector2.zero;
                    break;

                case ControlType.Stun:
                    // Оглушаем
                    var stunnable = target.GetComponent<IStunnable>();
                    if (stunnable != null) stunnable.Stun(effect.controlDuration);
                    break;
            }

            Debug.Log($"[FormationEffects] Control {effect.controlType} for {effect.controlDuration}s applied to {target.name}");
        }

        #endregion

        #region Shield Application

        /// <summary>
        /// Применить щит к цели.
        /// </summary>
        public static void ApplyShield(GameObject target, FormationEffect effect)
        {
            if (effect.value <= 0) return;

            // Проверяем QiController
            var qi = target.GetComponent<Qi.QiController>();
            if (qi != null)
            {
                // Добавляем временный щит
                // Реальная реализация зависит от системы щитов
                Debug.Log($"[FormationEffects] Shield {effect.value} applied to {target.name}");
            }
        }

        #endregion

        #region Summon Application

        /// <summary>
        /// Призвать существо.
        /// </summary>
        public static void ApplySummon(GameObject target, FormationEffect effect, GameObject source)
        {
            // Призыв существа рядом с целью или источником
            Vector3 spawnPosition = source != null ? source.transform.position : target.transform.position;

            // Случайное смещение
            spawnPosition += new Vector3(
                UnityEngine.Random.Range(-2f, 2f),
                UnityEngine.Random.Range(-2f, 2f),
                0
            );

            // Реальный призыв зависит от системы призыва
            Debug.Log($"[FormationEffects] Summon at {spawnPosition}");
        }

        #endregion

        #region Effect Removal

        /// <summary>
        /// Снять эффекты с цели.
        /// </summary>
        public static void RemoveEffects(GameObject target, List<FormationEffect> effects, GameObject source)
        {
            if (target == null || effects == null) return;

            foreach (var effect in effects)
            {
                RemoveEffect(target, effect, source);
            }
        }

        /// <summary>
        /// Снять один эффект с цели.
        /// </summary>
        public static void RemoveEffect(GameObject target, FormationEffect effect, GameObject source)
        {
            if (target == null || effect == null) return;

            switch (effect.effectType)
            {
                case FormationEffectType.Buff:
                case FormationEffectType.Debuff:
                    var buffManager = target.GetComponent<BuffManager>();
                    if (buffManager != null)
                    {
                        buffManager.RemoveBuff(effect.buffType);
                    }
                    break;

                case FormationEffectType.Control:
                    var controlReceiver = target.GetComponent<IControlReceiver>();
                    if (controlReceiver != null)
                    {
                        controlReceiver.RemoveControl(effect.controlType);
                    }
                    break;

                // Damage, Heal, Shield, Summon не требуют снятия
            }
        }

        #endregion
    }

    #region Interfaces

    /// <summary>
    /// Интерфейс для приёмника эффектов контроля.
    /// </summary>
    public interface IControlReceiver
    {
        void ApplyControl(ControlType controlType, float duration);
        void RemoveControl(ControlType controlType);
        bool IsControlled { get; }
        ControlType CurrentControl { get; }
    }

    /// <summary>
    /// Интерфейс для оглушаемых объектов.
    /// </summary>
    public interface IStunnable
    {
        void Stun(float duration);
        void Unstun();
        bool IsStunned { get; }
    }

    /// <summary>
    /// Менеджер баффов (заглушка для интеграции).
    /// </summary>
    public class BuffManager : MonoBehaviour
    {
        public void AddBuff(BuffType buffType, float value, bool isPercentage, float duration)
        {
            Debug.Log($"[BuffManager] AddBuff: {buffType} +{value}{(isPercentage ? "%" : "")} for {duration}s");
        }

        public void AddDebuff(BuffType buffType, float value, bool isPercentage, float duration)
        {
            Debug.Log($"[BuffManager] AddDebuff: {buffType} -{value}{(isPercentage ? "%" : "")} for {duration}s");
        }

        public void RemoveBuff(BuffType buffType)
        {
            Debug.Log($"[BuffManager] RemoveBuff: {buffType}");
        }
    }

    /// <summary>
    /// Типы баффов.
    /// </summary>
    public enum BuffType
    {
        None,
        Damage,         // Урон
        Defense,        // Защита
        Speed,          // Скорость
        CriticalChance, // Шанс крита
        CriticalDamage, // Урон крита
        QiRegen,        // Регенерация Ци
        MaxQi,          // Максимальное Ци
        Conductivity,   // Проводимость
        Health,         // Здоровье
        Stamina,        // Выносливость
        Resistance,     // Сопротивление
        Evasion         // Уклонение
    }

    #endregion
}
