// Создано: 2026-04-03
// Источник: docs/examples/TechniqueEffectsSystem.md

using UnityEngine;
using System.Collections;
using CultivationGame.Core;

namespace CultivationGame.Combat.Effects // FIX: CultivationWorld→CultivationGame (2026-04-12)
{
    /// <summary>
    /// Базовый класс для эффектов техник.
    /// 
    /// Типы эффектов:
    /// 1. Направленные (Directional) — движутся в направлении
    /// 2. Расширяющиеся (Expanding) — разрастаются от центра
    /// 3. Статические (Static) — остаются на месте
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class TechniqueEffect : MonoBehaviour
    {
        #region Configuration

        [Header("Base Settings")]
        [SerializeField] protected Element element = Element.Neutral;
        [SerializeField] protected float duration = 1f;
        [SerializeField] protected AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);
        [SerializeField] protected AnimationCurve alphaCurve = AnimationCurve.Linear(0, 1, 1, 0);

        [Header("Visual")]
        [SerializeField] protected SpriteRenderer spriteRenderer;
        [SerializeField] protected bool autoDestroy = true;

        #endregion

        #region State

        protected float _elapsedTime;
        protected Vector3 _originalScale;
        protected Color _originalColor;
        protected bool _isPlaying;

        #endregion

        #region Properties

        public Element Element => element;
        public bool IsPlaying => _isPlaying;
        public float Progress => duration > 0 ? _elapsedTime / duration : 1f;

        #endregion

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            _originalScale = transform.localScale;

            if (spriteRenderer != null)
            {
                _originalColor = spriteRenderer.color;
            }
        }

        protected virtual void Update()
        {
            if (!_isPlaying) return;

            if (_elapsedTime < duration)
            {
                _elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(_elapsedTime / duration);

                ApplyScaleAnimation(t);
                ApplyAlphaAnimation(t);
                OnUpdateEffect(t);

                if (_elapsedTime >= duration)
                {
                    OnEffectComplete();
                }
            }
        }

        #endregion

        #region Animation

        /// <summary>
        /// Применяет анимацию масштаба.
        /// </summary>
        protected virtual void ApplyScaleAnimation(float t)
        {
            if (scaleCurve != null)
            {
                float scale = scaleCurve.Evaluate(t);
                transform.localScale = _originalScale * scale;
            }
        }

        /// <summary>
        /// Применяет анимацию прозрачности.
        /// </summary>
        protected virtual void ApplyAlphaAnimation(float t)
        {
            if (alphaCurve != null && spriteRenderer != null)
            {
                Color color = spriteRenderer.color;
                color.a = _originalColor.a * alphaCurve.Evaluate(t);
                spriteRenderer.color = color;
            }
        }

        /// <summary>
        /// Вызывается каждый кадр во время эффекта.
        /// Переопределите для специфической логики.
        /// </summary>
        protected virtual void OnUpdateEffect(float t) { }

        #endregion

        #region Control

        /// <summary>
        /// Запускает эффект.
        /// </summary>
        /// <param name="origin">Точка появления</param>
        /// <param name="direction">Направление (для направленных эффектов)</param>
        public virtual void Play(Vector2 origin, Vector2 direction = default)
        {
            transform.position = origin;
            _elapsedTime = 0f;
            _isPlaying = true;
            gameObject.SetActive(true);

            // Сбрасываем визуальное состояние
            transform.localScale = _originalScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = _originalColor;
            }
        }

        /// <summary>
        /// Останавливает эффект.
        /// </summary>
        public virtual void Stop()
        {
            _isPlaying = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Пауза эффекта.
        /// </summary>
        public virtual void Pause()
        {
            _isPlaying = false;
        }

        /// <summary>
        /// Возобновление эффекта.
        /// </summary>
        public virtual void Resume()
        {
            _isPlaying = true;
        }

        /// <summary>
        /// Вызывается при завершении эффекта.
        /// </summary>
        protected virtual void OnEffectComplete()
        {
            _isPlaying = false;

            if (autoDestroy)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Устанавливает длительность эффекта.
        /// </summary>
        public void SetDuration(float newDuration)
        {
            duration = Mathf.Max(0.1f, newDuration);
        }

        /// <summary>
        /// Устанавливает элемент эффекта.
        /// </summary>
        public void SetElement(Element newElement)
        {
            element = newElement;
        }

        /// <summary>
        /// Устанавливает кривые анимации.
        /// </summary>
        public void SetCurves(AnimationCurve scale, AnimationCurve alpha)
        {
            scaleCurve = scale;
            alphaCurve = alpha;
        }

        #endregion
    }
}
