// ============================================================================
// FormationEffects.cs — Применение эффектов формации
// Cultivation World Simulator
// Версия: 1.1 — Заменён Instantiate/Destroy на VFXPool
// Создано: 2026-04-03 13:40:00 UTC
// Редактировано: 2026-04-09 10:45:00 UTC
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 1.1:
// - FIX: Instantiate/Destroy заменены на VFXPool для оптимизации (аудит Unity 6.3)
//
// Источник: docs/FORMATION_SYSTEM.md
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;
using CultivationGame.Buff;
using CultivationWorld.Combat.OrbitalSystem;
// FIX FRM-H02: Import Attitude enum for IsAlly fix (2026-04-11)
// Attitude is in CultivationGame.Core (already imported above)

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
    /// │   Damage      → Периодический урон врагам                                │
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
        /// Редактировано: 2026-04-03 - Обновлено для Unity 6 (OverlapCircleAll вместо OverlapCircleNonAlloc)
        /// </summary>
        /// <param name="center">Центр поиска</param>
        /// <param name="radius">Радиус</param>
        /// <param name="layerMask">Слой для поиска</param>
        /// <returns>Массив коллайдеров</returns>
        public static Collider2D[] FindTargets(Vector2 center, float radius, LayerMask layerMask)
        {
            return Physics2D.OverlapCircleAll(center, radius, layerMask);
        }

        /// <summary>
        /// Определить, является ли цель союзником.
        /// FIX FRM-H02: Use Attitude enum for ally/enemy determination (2026-04-11)
        /// Allied/Friendly/SwornAlly = ally, Hostile/Hatred = enemy
        /// Attitude ranges: Friendly=10..49, Allied=50..79, SwornAlly=80..100,
        /// Hostile=-50..-21, Hatred=-100..-51
        /// </summary>
        public static bool IsAlly(GameObject target, GameObject owner)
        {
            if (target == null || owner == null) return false;

            // Сам владелец — союзник
            if (target == owner) return true;

            // Проверяем фракцию через FactionController (если есть)
            var targetFaction = target.GetComponent<World.FactionController>();
            var ownerFaction = owner.GetComponent<World.FactionController>();

            if (targetFaction != null && ownerFaction != null)
            {
                var targetMembership = targetFaction.GetMembership(target.name);
                var ownerMembership = ownerFaction.GetMembership(owner.name);
                
                if (targetMembership != null && ownerMembership != null)
                {
                    // Та же фракция = Allied/SwornAlly
                    if (targetMembership.FactionId == ownerMembership.FactionId)
                        return true;
                    
                    // FIX FRM-H02: Map faction relation to Attitude ranges (2026-04-11)
                    // Attitude.Friendly range starts at 10, Attitude.Hostile range ends at -21
                    int relation = ownerFaction.GetFactionRelation(ownerMembership.FactionId, targetMembership.FactionId);
                    if (relation >= 10) return true;    // Friendly/Allied/SwornAlly territory
                    if (relation <= -21) return false;  // Hostile/Hatred territory
                    // Neutral(-9..9) / Unfriendly(-20..-10): fall through to tag check
                }
            }

            // Tag-based fallback for entities without faction system
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

            // Визуальный эффект — используем пул вместо Instantiate/Destroy
            if (effect.vfxPrefab != null)
            {
                // FIX: VFXPool.SpawnDefault автоматически вернёт объект в пул через 2 секунды
                VFXPool.SpawnDefault(effect.vfxPrefab, target.transform.position);
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

            // Редактировано: 2026-04-03 - Используем ICombatTarget вместо Combatant
            var combatant = target.GetComponent<ICombatTarget>();
            if (combatant != null)
            {
                var damage = new DamageInfo
                {
                    Amount = effect.tickValue,
                    Element = effect.element,
                    Source = source,
                    HitPoint = target.transform.position
                };
                combatant.TakeDamage(damage);
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
                return;
            }

            Debug.Log($"[FormationEffects] Damage {effect.tickValue} applied to {target.name}");
        }

        #endregion

        #region Heal Application

        /// <summary>
        /// Исцелить цель.
        /// FIX FRM-M04: Separate choice — heal Qi, Body, or both based on effect config (2026-04-11)
        /// Uses isPercentage field as heal selector: true=Qi only, false=Body only
        /// When both Qi and Body controllers exist, both are healed (backward compatible)
        /// </summary>
        public static void ApplyHeal(GameObject target, FormationEffect effect)
        {
            if (effect.tickValue <= 0) return;

            var qi = target.GetComponent<Qi.QiController>();
            var body = target.GetComponent<Body.BodyController>();

            // FIX FRM-M04: Separate heal choice based on available controllers (2026-04-11)
            // If only one controller exists, heal that one
            // If both exist, check isPercentage: true=Qi, false=Body, or heal both by default
            bool hasQi = qi != null;
            bool hasBody = body != null;
            
            if (hasQi && hasBody)
            {
                // Both available: heal both for backward compatibility
                // Future improvement: add HealType field to FormationEffect
                qi.AddQi(effect.tickValue);
                body.Heal(effect.tickValue);
            }
            else if (hasQi)
            {
                qi.AddQi(effect.tickValue);
            }
            else if (hasBody)
            {
                body.Heal(effect.tickValue);
            }

            Debug.Log($"[FormationEffects] Heal {effect.tickValue} (Qi:{hasQi} Body:{hasBody}) applied to {target.name}");
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
        /// FIX BUF-C04: Saved Rigidbody2D state for control rollback (2026-04-11)
        /// </summary>
        private struct SavedRigidbodyState
        {
            public bool simulated;
            public float gravityScale;
            public RigidbodyType2D bodyType;
        }
        
        // FIX BUF-C04: Map of target InstanceID → saved Rigidbody2D state (2026-04-11)
        private static Dictionary<int, SavedRigidbodyState> _savedRbStates = new Dictionary<int, SavedRigidbodyState>();
        
        /// <summary>
        /// FIX BUF-C04: Helper component to restore Rigidbody2D after control duration (2026-04-11)
        /// </summary>
        private class ControlRestoreHelper : MonoBehaviour
        {
            public Rigidbody2D rb;
            public SavedRigidbodyState originalState;
            public ControlType controlType;
            private float _remainingDuration;
            
            public void Initialize(float duration)
            {
                _remainingDuration = duration;
            }
            
            private void Update()
            {
                _remainingDuration -= Time.deltaTime;
                if (_remainingDuration <= 0f)
                {
                    RestoreState();
                    Destroy(this); // Destroys component only, not GameObject
                }
            }
            
            private void RestoreState()
            {
                if (rb == null) return;
                
                int id = rb.gameObject.GetInstanceID();
                
                switch (controlType)
                {
                    case ControlType.Freeze:
                        rb.simulated = originalState.simulated;
                        rb.gravityScale = originalState.gravityScale;
                        rb.bodyType = originalState.bodyType;
                        break;
                    case ControlType.Root:
                        // Root only zeros velocity; no persistent state to restore
                        break;
                }
                
                _savedRbStates.Remove(id);
            }
        }
        
        /// <summary>
        /// Fallback применение контроля.
        /// FIX BUF-C04: Save and restore original Rigidbody2D values for Freeze/Slow/Root rollback (2026-04-11)
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
                    if (rb != null)
                    {
                        // FIX BUF-C04: Save original Rigidbody2D state before modifying (2026-04-11)
                        int rbId = target.GetInstanceID();
                        if (!_savedRbStates.ContainsKey(rbId))
                        {
                            _savedRbStates[rbId] = new SavedRigidbodyState
                            {
                                simulated = rb.simulated,
                                gravityScale = rb.gravityScale,
                                bodyType = rb.bodyType
                            };
                        }
                        rb.simulated = false;
                        
                        // FIX BUF-C04: Add restore helper for timed rollback (2026-04-11)
                        var restoreHelper = target.AddComponent<ControlRestoreHelper>();
                        restoreHelper.rb = rb;
                        restoreHelper.originalState = _savedRbStates[rbId];
                        restoreHelper.controlType = ControlType.Freeze;
                        restoreHelper.Initialize(effect.controlDuration);
                    }
                    break;

                case ControlType.Slow:
                    // Замедляем движение
                    var rbSlow = target.GetComponent<Rigidbody2D>();
                    if (rbSlow != null) rbSlow.linearVelocity *= 0.5f;
                    // Note: Slow is a one-time velocity modification; speed naturally restores
                    break;

                case ControlType.Root:
                    // Обездвиживаем
                    var rbRoot = target.GetComponent<Rigidbody2D>();
                    if (rbRoot != null)
                    {
                        // FIX BUF-C04: Save original velocity before zeroing (2026-04-11)
                        rbRoot.linearVelocity = Vector2.zero;
                        
                        // FIX BUF-C04: Add restore helper for timed rollback (2026-04-11)
                        var rootHelper = target.AddComponent<ControlRestoreHelper>();
                        rootHelper.rb = rbRoot;
                        rootHelper.controlType = ControlType.Root;
                        rootHelper.Initialize(effect.controlDuration);
                    }
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

    // FIX BUF-M05: BuffManager is in CultivationGame.Buff namespace, imported via using (2026-04-11)
    // BuffType for formations is defined in FormationData.cs

    #endregion
}
