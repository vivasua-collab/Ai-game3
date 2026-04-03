// Создано: 2026-04-03
// Источник: docs/examples/OrbitalWeaponSystem.md

using UnityEngine;
using CultivationGame.Core;

namespace CultivationWorld.Combat.OrbitalSystem
{
    /// <summary>
    /// Оружие на орбите.
    /// 
    /// Особенности:
    /// - Вращается вокруг персонажа
    /// - Наносит урон при контакте во время атаки
    /// - Имеет стихийный элемент (огонь, вода, и т.д.)
    /// - Визуальные эффекты зависят от элемента
    /// </summary>
    public class OrbitalWeapon : MonoBehaviour
    {
        #region Configuration

        [Header("Weapon Settings")]
        [SerializeField] private string weaponName = "Orbital Weapon";
        [SerializeField] private Element element = Element.Neutral;
        [SerializeField] private int baseDamage = 10;
        [SerializeField] private float hitRadius = 0.5f;
        [SerializeField] private LayerMask hitLayers;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private ParticleSystem hitParticles;

        [Header("Audio")]
        [SerializeField] private AudioClip hitSound;
        [SerializeField] [Range(0f, 1f)] private float hitSoundVolume = 0.5f;

        #endregion

        #region State

        private OrbitalWeaponController _controller;
        private Collider2D[] _hitResults = new Collider2D[10];
        private bool _isActive = true;
        private bool _isInitialized;

        #endregion

        #region Properties

        public bool IsActive => _isActive;
        public Element Element => element;
        public string WeaponName => weaponName;
        public int BaseDamage => baseDamage;

        #endregion

        #region Lifecycle

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            if (trailRenderer == null)
                trailRenderer = GetComponent<TrailRenderer>();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Инициализация оружия контроллером.
        /// </summary>
        public void Initialize(OrbitalWeaponController controller)
        {
            _controller = controller;
            _isInitialized = true;

            // Настраиваем визуальные эффекты на основе элемента
            SetupElementVisuals();
        }

        /// <summary>
        /// Деинициализация.
        /// </summary>
        public void Deinitialize()
        {
            _controller = null;
            _isInitialized = false;
        }

        /// <summary>
        /// Настраивает визуальные эффекты в зависимости от элемента.
        /// </summary>
        private void SetupElementVisuals()
        {
            if (trailRenderer == null) return;

            // Цвет следа в зависимости от элемента
            Color trailColor = GetElementColor();
            trailRenderer.startColor = trailColor;
            trailRenderer.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        }

        /// <summary>
        /// Получает цвет для элемента.
        /// </summary>
        private Color GetElementColor()
        {
            return element switch
            {
                Element.Fire => new Color(1f, 0.3f, 0.1f, 0.8f),      // Красно-оранжевый
                Element.Water => new Color(0.2f, 0.5f, 1f, 0.8f),     // Синий
                Element.Lightning => new Color(0.8f, 0.8f, 1f, 0.9f), // Голубовато-белый
                Element.Earth => new Color(0.6f, 0.4f, 0.2f, 0.8f),   // Коричневый
                Element.Air => new Color(0.7f, 1f, 0.7f, 0.6f),       // Светло-зелёный
                Element.Poison => new Color(0.5f, 0f, 0.8f, 0.8f),    // Фиолетовый
                Element.Void => new Color(0.3f, 0f, 0.5f, 0.8f),      // Тёмно-фиолетовый
                Element.Neutral => new Color(1f, 1f, 1f, 0.5f),       // Белый
                _ => new Color(1f, 1f, 1f, 0.5f)
            };
        }

        #endregion

        #region Combat

        /// <summary>
        /// Вызывается при попадании атаки.
        /// </summary>
        public void OnAttackHit()
        {
            // Проверяем попадания
            int hitCount = Physics2D.OverlapCircleNonAlloc(
                transform.position,
                hitRadius,
                _hitResults,
                hitLayers
            );

            for (int i = 0; i < hitCount; i++)
            {
                ProcessHit(_hitResults[i]);
            }
        }

        /// <summary>
        /// Обрабатывает попадание по цели.
        /// </summary>
        private void ProcessHit(Collider2D hitCollider)
        {
            // Проверяем интерфейс combat target
            var combatTarget = hitCollider.GetComponent<ICombatTarget>();
            if (combatTarget != null && combatTarget.IsHostile)
            {
                ApplyDamage(combatTarget, hitCollider.transform.position);
                OnHitEffects(hitCollider.transform.position);
                return;
            }

            // Проверяем компонент Health (альтернативный подход)
            var health = hitCollider.GetComponent<IHealth>();
            if (health != null)
            {
                ApplyDamageToHealth(health, hitCollider.transform.position);
                OnHitEffects(hitCollider.transform.position);
            }
        }

        /// <summary>
        /// Наносит урон цели через интерфейс ICombatTarget.
        /// </summary>
        private void ApplyDamage(ICombatTarget target, Vector2 hitPoint)
        {
            DamageInfo damage = new DamageInfo
            {
                Amount = CalculateDamage(),
                Element = element,
                Source = _controller != null ? _controller.gameObject : null,
                HitPoint = hitPoint
            };

            target.TakeDamage(damage);
        }

        /// <summary>
        /// Наносит урон через интерфейс IHealth.
        /// </summary>
        private void ApplyDamageToHealth(IHealth health, Vector2 hitPoint)
        {
            int finalDamage = CalculateDamage();
            health.TakeDamage(finalDamage);
        }

        /// <summary>
        /// Вычисляет итоговый урон.
        /// </summary>
        protected virtual int CalculateDamage()
        {
            // Базовый урон + возможные модификаторы
            return baseDamage;
        }

        #endregion

        #region Visual Effects

        /// <summary>
        /// Эффекты при попадании.
        /// </summary>
        private void OnHitEffects(Vector2 position)
        {
            // Частицы
            if (hitParticles != null)
            {
                var particles = Instantiate(hitParticles, position, Quaternion.identity);
                particles.Play();
                Destroy(particles.gameObject, particles.main.duration);
            }

            // Звук
            if (hitSound != null)
            {
                AudioSource.PlayClipAtPoint(hitSound, position, hitSoundVolume);
            }
        }

        #endregion

        #region Activation

        /// <summary>
        /// Активирует/деактивирует оружие.
        /// </summary>
        public void SetActive(bool active)
        {
            _isActive = active;

            if (spriteRenderer != null)
                spriteRenderer.enabled = active;

            if (trailRenderer != null)
                trailRenderer.enabled = active;
        }

        /// <summary>
        /// Устанавливает базовый урон.
        /// </summary>
        public void SetBaseDamage(int damage)
        {
            baseDamage = Mathf.Max(1, damage);
        }

        /// <summary>
        /// Устанавливает элемент оружия.
        /// </summary>
        public void SetElement(Element newElement)
        {
            element = newElement;
            SetupElementVisuals();
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, hitRadius);
        }

        private void OnValidate()
        {
            if (baseDamage < 1) baseDamage = 1;
            if (hitRadius < 0.1f) hitRadius = 0.1f;
        }
#endif

        #endregion
    }

    #region Interfaces

    /// <summary>
    /// Интерфейс для целей, которые могут получать урон.
    /// </summary>
    public interface ICombatTarget
    {
        bool IsHostile { get; }
        Vector2 Position { get; }
        void TakeDamage(DamageInfo damage);
    }

    /// <summary>
    /// Интерфейс для системы здоровья.
    /// </summary>
    public interface IHealth
    {
        int CurrentHealth { get; }
        int MaxHealth { get; }
        void TakeDamage(int amount);
        void Heal(int amount);
    }

    /// <summary>
    /// Информация о наносимом уроне.
    /// </summary>
    [System.Serializable]
    public struct DamageInfo
    {
        public int Amount;
        public Element Element;
        public GameObject Source;
        public Vector2 HitPoint;
    }

    #endregion
}
