// Создано: 2026-04-03
// Редактировано: 2026-04-03
// Источник: docs/examples/TechniqueEffectsSystem.md

using UnityEngine;
using CultivationGame.Core;
// FIX: Add explicit using for OrbitalSystem types (ICombatTarget, DamageInfo) (2026-04-11)
using CultivationWorld.Combat.OrbitalSystem;

namespace CultivationWorld.Combat.Effects
{
    /// <summary>
    /// Расширяющийся эффект техники.
    /// Разрастается от центра.
    /// 
    /// Примеры:
    /// - Расширяющийся туман (mist)
    /// - Ядовитое облако (poison cloud)
    /// - Аура исцеления (healing aura)
    /// - Взрыв ци (qi explosion)
    /// </summary>
    public class ExpandingEffect : TechniqueEffect
    {
        #region Configuration

        [Header("Expansion")]
        [SerializeField] private float startRadius = 0.5f;
        [SerializeField] private float maxRadius = 3f;

        [Header("Effect Application")]
        [SerializeField] private LayerMask affectedLayers;
        [SerializeField] private bool applyEffectContinuously = true;
        [SerializeField] private float effectInterval = 0.5f;
        [SerializeField] private bool affectHostile = true;
        [SerializeField] private bool affectFriendly = false;

        [Header("Combat")]
        [SerializeField] private int baseDamage = 5;
        [SerializeField] private int baseHealing = 5;

        #endregion

        #region State

        private float _currentRadius;
        private float _lastEffectTime;
        private Collider2D[] _affectedBuffer = new Collider2D[30];

        #endregion

        #region Properties

        /// <summary>
        /// Текущий радиус эффекта.
        /// </summary>
        public float CurrentRadius => _currentRadius;

        /// <summary>
        /// Максимальный радиус эффекта.
        /// </summary>
        public float MaxRadius => maxRadius;

        #endregion

        #region Play

        public override void Play(Vector2 origin, Vector2 direction = default)
        {
            base.Play(origin, direction);

            _currentRadius = startRadius;
            _lastEffectTime = 0f;
        }

        #endregion

        #region Animation

        protected override void ApplyScaleAnimation(float t)
        {
            // Интерполируем радиус
            _currentRadius = Mathf.Lerp(startRadius, maxRadius, t);

            // Масштабируем спрайт
            float scale = _currentRadius / startRadius;
            transform.localScale = _originalScale * scale;
        }

        protected override void OnUpdateEffect(float t)
        {
            base.OnUpdateEffect(t);

            // Применяем эффект с интервалом
            if (applyEffectContinuously && Time.time - _lastEffectTime >= effectInterval)
            {
                ApplyEffectToTargets();
                _lastEffectTime = Time.time;
            }
        }

        #endregion

        #region Effect Application

        /// <summary>
        /// Применяет эффект к целям в радиусе.
        /// Редактировано: 2026-04-03 - Обновлено для Unity 6 (OverlapCircleAll вместо OverlapCircleNonAlloc)
        /// </summary>
        private void ApplyEffectToTargets()
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(
                transform.position,
                _currentRadius,
                affectedLayers
            );

            foreach (var hit in hits)
            {
                ProcessTarget(hit);
            }
        }

        /// <summary>
        /// Обрабатывает цель в зоне эффекта.
        /// </summary>
        protected virtual void ProcessTarget(Collider2D target)
        {
            // FIX: Use fully qualified ICombatTarget from OrbitalSystem namespace (2026-04-11)
            var combatTarget = target.GetComponent<ICombatTarget>();
            if (combatTarget == null) return;

            // Проверяем дружественность/враждебность
            bool shouldAffect = (combatTarget.IsHostile && affectHostile) ||
                               (!combatTarget.IsHostile && affectFriendly);

            if (shouldAffect)
            {
                ApplyEffect(combatTarget);
            }
        }

        /// <summary>
        /// Применяет эффект к цели.
        /// FIX: Use ICombatTarget from imported OrbitalSystem namespace (2026-04-11)
        /// </summary>
        protected virtual void ApplyEffect(ICombatTarget target)
        {
            // Редактировано: 2026-04-03
            switch (element)
            {
                case Element.Poison:
                    ApplyPoison(target);
                    break;

                case Element.Fire:
                    ApplyFireDamage(target);
                    break;

                case Element.Water:
                    ApplyWaterEffect(target);
                    break;

                case Element.Neutral:
                    if (!target.IsHostile && affectFriendly)
                    {
                        ApplyHealing(target);
                    }
                    break;

                default:
                    ApplyGenericDamage(target);
                    break;
            }
        }

        /// <summary>
        /// Применяет эффект яда.
        /// FIX: Use ICombatTarget from imported OrbitalSystem namespace (2026-04-11)
        /// </summary>
        protected virtual void ApplyPoison(ICombatTarget target)
        {
            var damage = new DamageInfo
            {
                Amount = baseDamage,
                Element = Element.Poison,
                Source = null,
                HitPoint = target.Position
            };

            target.TakeDamage(damage);
        }

        /// <summary>
        /// Применяет огненный урон.
        /// FIX: Use ICombatTarget from imported OrbitalSystem namespace (2026-04-11)
        /// </summary>
        protected virtual void ApplyFireDamage(ICombatTarget target)
        {
            var damage = new DamageInfo
            {
                Amount = baseDamage,
                Element = Element.Fire,
                Source = null,
                HitPoint = target.Position
            };

            target.TakeDamage(damage);
        }

        /// <summary>
        /// Применяет водный эффект.
        /// FIX: Use ICombatTarget from imported OrbitalSystem namespace (2026-04-11)
        /// </summary>
        protected virtual void ApplyWaterEffect(ICombatTarget target)
        {
            var damage = new DamageInfo
            {
                Amount = baseDamage / 2,
                Element = Element.Water,
                Source = null,
                HitPoint = target.Position
            };

            target.TakeDamage(damage);
        }

        /// <summary>
        /// Применяет исцеление.
        /// FIX: Use ICombatTarget from imported OrbitalSystem namespace (2026-04-11)
        /// </summary>
        protected virtual void ApplyHealing(ICombatTarget target)
        {
            // TODO: Добавить систему исцеления через интерфейс
            // target.Heal(baseHealing);
        }

        /// <summary>
        /// Применяет базовый урон.
        /// FIX: Use ICombatTarget from imported OrbitalSystem namespace (2026-04-11)
        /// </summary>
        protected virtual void ApplyGenericDamage(ICombatTarget target)
        {
            var damage = new DamageInfo
            {
                Amount = baseDamage,
                Element = element,
                Source = null,
                HitPoint = target.Position
            };

            target.TakeDamage(damage);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Устанавливает радиусы эффекта.
        /// </summary>
        public void SetRadius(float start, float max)
        {
            startRadius = Mathf.Max(0.1f, start);
            maxRadius = Mathf.Max(startRadius, max);
        }

        /// <summary>
        /// Устанавливает урон.
        /// </summary>
        public void SetDamage(int damage)
        {
            baseDamage = Mathf.Max(0, damage);
        }

        /// <summary>
        /// Устанавливает исцеление.
        /// </summary>
        public void SetHealing(int healing)
        {
            baseHealing = Mathf.Max(0, healing);
        }

        /// <summary>
        /// Устанавливает интервал применения.
        /// </summary>
        public void SetEffectInterval(float interval)
        {
            effectInterval = Mathf.Max(0.1f, interval);
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, _currentRadius);

            UnityEditor.Handles.color = new Color(0, 1, 1, 0.3f);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.forward, _currentRadius);
        }
#endif

        #endregion
    }
}
