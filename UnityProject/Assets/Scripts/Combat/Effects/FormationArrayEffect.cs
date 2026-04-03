// Создано: 2026-04-03
// Источник: docs/examples/TechniqueEffectsSystem.md

using UnityEngine;
using CultivationWorld.Combat.OrbitalSystem;

namespace CultivationWorld.Combat.Effects
{
    /// <summary>
    /// Формационный массив — статический эффект на земле.
    /// 
    /// Особенности:
    /// - Остается на месте
    /// - Применяет эффекты к тем, кто стоит внутри
    /// - Может усиливать союзников или ослаблять врагов
    /// </summary>
    public class FormationArrayEffect : TechniqueEffect
    {
        #region Configuration

        [Header("Formation Settings")]
        [SerializeField] private float radius = 2f;
        [SerializeField] private LayerMask affectedLayers;

        [Header("Effect Application")]
        [SerializeField] private float effectInterval = 1f;
        [SerializeField] private bool buffAllies = true;
        [SerializeField] private bool debuffEnemies = true;

        [Header("Buffs (для союзников)")]
        [SerializeField] private float defenseBonus = 0.2f; // +20% защиты
        [SerializeField] private float attackBonus = 0.1f;  // +10% атаки
        [SerializeField] private int healingPerTick = 2;

        [Header("Debuffs (для врагов)")]
        [SerializeField] private float defenseMalus = 0.1f; // -10% защиты
        [SerializeField] private float speedMalus = 0.2f;   // -20% скорости
        [SerializeField] private int damagePerTick = 3;

        [Header("Visual")]
        [SerializeField] private ParticleSystem activationParticles;
        [SerializeField] private GameObject runeCirclePrefab;

        #endregion

        #region State

        private float _lastEffectTime;
        private Collider2D[] _affectedBuffer = new Collider2D[30];
        private GameObject _runeCircleInstance;

        #endregion

        #region Properties

        public float Radius => radius;

        #endregion

        #region Play

        public override void Play(Vector2 origin, Vector2 direction = default)
        {
            base.Play(origin, direction);

            _lastEffectTime = 0f;

            // Создаём визуальный рунный круг
            if (runeCirclePrefab != null)
            {
                _runeCircleInstance = Instantiate(runeCirclePrefab, origin, Quaternion.identity);
                _runeCircleInstance.transform.localScale = Vector3.one * radius * 2;
            }

            // Эффект активации
            if (activationParticles != null)
            {
                activationParticles.Play();
            }
        }

        #endregion

        #region Effect Application

        protected override void OnUpdateEffect(float t)
        {
            base.OnUpdateEffect(t);

            // Применяем эффект с интервалом
            if (Time.time - _lastEffectTime >= effectInterval)
            {
                ApplyFormationEffects();
                _lastEffectTime = Time.time;
            }
        }

        /// <summary>
        /// Применяет эффекты формации.
        /// Редактировано: 2026-04-03 - Обновлено для Unity 6 (OverlapCircle вместо OverlapCircleNonAlloc)
        /// </summary>
        private void ApplyFormationEffects()
        {
            Collider2D[] hits = Physics2D.OverlapCircle(
                transform.position,
                radius,
                affectedLayers
            );

            foreach (var hit in hits)
            {
                var target = hit.GetComponent<ICombatTarget>();
                if (target == null) continue;

                if (!target.IsHostile && buffAllies)
                {
                    ApplyBuff(target);
                }
                else if (target.IsHostile && debuffEnemies)
                {
                    ApplyDebuff(target);
                }
            }
        }

        /// <summary>
        /// Применяет бафф к союзнику.
        /// </summary>
        private void ApplyBuff(ICombatTarget target)
        {
            // TODO: Реализовать через систему баффов
            // BuffSystem.ApplyBuff(target, BuffType.Defense, defenseBonus, effectInterval + 0.1f);
            // BuffSystem.ApplyBuff(target, BuffType.Attack, attackBonus, effectInterval + 0.1f);
            // target.Heal(healingPerTick);

            // Временная реализация - логирование
            Debug.Log($"[Formation] Buff applied to {target.Position}");
        }

        /// <summary>
        /// Применяет дебафф к врагу.
        /// </summary>
        private void ApplyDebuff(ICombatTarget target)
        {
            // Наносим периодический урон
            var damage = new DamageInfo
            {
                Amount = damagePerTick,
                Element = element,
                Source = null,
                HitPoint = target.Position
            };

            target.TakeDamage(damage);

            // TODO: Добавить систему дебаффов
            // DebuffSystem.ApplyDebuff(target, DebuffType.DefenseReduction, defenseMalus, effectInterval + 0.1f);
            // DebuffSystem.ApplyDebuff(target, DebuffType.Slow, speedMalus, effectInterval + 0.1f);
        }

        #endregion

        #region Cleanup

        protected override void OnEffectComplete()
        {
            // Уничтожаем рунный круг
            if (_runeCircleInstance != null)
            {
                Destroy(_runeCircleInstance);
                _runeCircleInstance = null;
            }

            base.OnEffectComplete();
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Устанавливает радиус формации.
        /// </summary>
        public void SetRadius(float newRadius)
        {
            radius = Mathf.Max(0.5f, newRadius);

            if (_runeCircleInstance != null)
            {
                _runeCircleInstance.transform.localScale = Vector3.one * radius * 2;
            }
        }

        /// <summary>
        /// Устанавливает баффы.
        /// </summary>
        public void SetBuffs(float defense, float attack, int healing)
        {
            defenseBonus = Mathf.Clamp01(defense);
            attackBonus = Mathf.Clamp01(attack);
            healingPerTick = Mathf.Max(0, healing);
        }

        /// <summary>
        /// Устанавливает дебаффы.
        /// </summary>
        public void SetDebuffs(float defenseReduction, float slow, int damage)
        {
            defenseMalus = Mathf.Clamp01(defenseReduction);
            speedMalus = Mathf.Clamp01(slow);
            damagePerTick = Mathf.Max(0, damage);
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Рисуем область формации
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, radius);

            // Рисуем заполненную область
            UnityEditor.Handles.color = new Color(1f, 0.8f, 0f, 0.2f);
            UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.forward, radius);
        }
#endif

        #endregion
    }
}
