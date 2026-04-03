// Создано: 2026-04-03
// Редактировано: 2026-04-03
// Источник: docs/examples/TechniqueEffectsSystem.md

using UnityEngine;
using CultivationGame.Core;

namespace CultivationWorld.Combat.Effects
{
    /// <summary>
    /// Направленный эффект техники.
    /// Движется в определённом направлении.
    /// 
    /// Примеры:
    /// - Огненный удар (fire slash)
    /// - Водяная волна (water wave)
    /// - Воздушный клинок (air blade)
    /// - Молния (lightning bolt)
    /// </summary>
    public class DirectionalEffect : TechniqueEffect
    {
        #region Configuration

        [Header("Movement")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float maxDistance = 5f;
        [SerializeField] private bool penetrateEnemies = false;
        [SerializeField] private int maxPenetrations = 3;

        [Header("Combat")]
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private float hitRadius = 0.5f;
        [SerializeField] private LayerMask hitLayers;

        [Header("Visual")]
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem hitParticles;
        [SerializeField] private AudioClip hitSound;

        #endregion

        #region State

        private Vector2 _direction;
        private Vector2 _startPosition;
        private int _penetrationCount;
        private Collider2D[] _hitBuffer = new Collider2D[20];

        #endregion

        #region Play

        /// <summary>
        /// Запускает эффект в направлении.
        /// </summary>
        public override void Play(Vector2 origin, Vector2 direction = default)
        {
            base.Play(origin, direction);

            _direction = direction.normalized;
            _startPosition = origin;
            _penetrationCount = 0;

            // Поворачиваем спрайт в направлении движения
            float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);

            // Настраиваем цвет trail по элементу
            SetupTrailColor();
        }

        /// <summary>
        /// Настраивает цвет следа по элементу.
        /// </summary>
        private void SetupTrailColor()
        {
            if (trailRenderer == null) return;

            // Редактировано: 2026-04-03
            Color trailColor = element switch
            {
                Element.Fire => new Color(1f, 0.3f, 0.1f, 0.8f),
                Element.Water => new Color(0.2f, 0.5f, 1f, 0.8f),
                Element.Lightning => new Color(0.8f, 0.8f, 1f, 0.9f),
                Element.Earth => new Color(0.6f, 0.4f, 0.2f, 0.8f),
                Element.Air => new Color(0.7f, 1f, 0.7f, 0.6f),
                Element.Poison => new Color(0.5f, 0f, 0.8f, 0.8f),
                Element.Void => new Color(0.3f, 0f, 0.5f, 0.8f),
                _ => new Color(1f, 1f, 1f, 0.5f)
            };

            trailRenderer.startColor = trailColor;
            trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        }

        #endregion

        #region Update

        protected override void OnUpdateEffect(float t)
        {
            base.OnUpdateEffect(t);

            // Движение в направлении
            transform.position += (Vector3)(_direction * speed * Time.deltaTime);

            // Проверка максимальной дистанции
            float distance = Vector2.Distance(_startPosition, transform.position);
            if (distance >= maxDistance)
            {
                OnEffectComplete();
                return;
            }

            // Проверка попаданий
            CheckHits();
        }

        /// <summary>
        /// Проверяет попадания по целям.
        /// Редактировано: 2026-04-03 - Обновлено для Unity 6 (OverlapCircle вместо OverlapCircleNonAlloc)
        /// </summary>
        private void CheckHits()
        {
            Collider2D[] hits = Physics2D.OverlapCircle(
                transform.position,
                hitRadius,
                hitLayers
            );

            foreach (var hit in hits)
            {
                ProcessHit(hit);
            }
        }

        /// <summary>
        /// Обрабатывает попадание.
        /// </summary>
        private void ProcessHit(Collider2D hitCollider)
        {
            var combatTarget = hitCollider.GetComponent<OrbitalSystem.ICombatTarget>();
            if (combatTarget != null && combatTarget.IsHostile)
            {
                ApplyDamage(combatTarget, hitCollider.transform.position);
                OnHitEffects(hitCollider.transform.position);

                if (!penetrateEnemies)
                {
                    OnEffectComplete();
                    return;
                }

                _penetrationCount++;
                if (_penetrationCount >= maxPenetrations)
                {
                    OnEffectComplete();
                }
            }
        }

        /// <summary>
        /// Наносит урон.
        /// </summary>
        private void ApplyDamage(OrbitalSystem.ICombatTarget target, Vector2 hitPoint)
        {
            var damage = new OrbitalSystem.DamageInfo
            {
                Amount = CalculateDamage(),
                Element = element,
                Source = null,
                HitPoint = hitPoint
            };

            target.TakeDamage(damage);
        }

        /// <summary>
        /// Вычисляет урон.
        /// </summary>
        protected virtual int CalculateDamage()
        {
            return baseDamage;
        }

        /// <summary>
        /// Эффекты при попадании.
        /// </summary>
        private void OnHitEffects(Vector2 position)
        {
            if (hitParticles != null)
            {
                var particles = Instantiate(hitParticles, position, Quaternion.identity);
                particles.Play();
                Destroy(particles.gameObject, particles.main.duration);
            }

            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, position);
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Устанавливает скорость эффекта.
        /// </summary>
        public void SetSpeed(float newSpeed)
        {
            speed = Mathf.Max(1f, newSpeed);
        }

        /// <summary>
        /// Устанавливает максимальную дистанцию.
        /// </summary>
        public void SetMaxDistance(float distance)
        {
            maxDistance = Mathf.Max(1f, distance);
        }

        /// <summary>
        /// Устанавливает базовый урон.
        /// </summary>
        public void SetBaseDamage(int damage)
        {
            baseDamage = Mathf.Max(1, damage);
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hitRadius);

            // Рисуем направление
            if (Application.isPlaying && _isPlaying)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, _direction * 2f);
            }
        }
#endif

        #endregion
    }
}
