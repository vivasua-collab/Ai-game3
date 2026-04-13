// Создано: 2026-04-03
// Редактировано: 2026-04-13 12:08:04 UTC — замена Input.mousePosition на Mouse.current.position
// Источник: docs/examples/CharacterSpriteMirroring.md

using UnityEngine;
using UnityEngine.InputSystem;

namespace CultivationGame.Character // FIX CHR-H01: CultivationWorld→CultivationGame (2026-04-12)
{
    /// <summary>
    /// Контроллер спрайта персонажа.
    /// Управляет направлением взгляда через зеркалирование.
    /// 
    /// Принцип:
    /// - Один спрайт смотрит вправо
    /// - Зеркалирование scaleX = -1 даёт взгляд влево
    /// - Экономия: 1 спрайт вместо 2+
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterSpriteController : MonoBehaviour
    {
        #region Configuration

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Settings")]
        [SerializeField] private bool faceRightByDefault = true;
        [SerializeField] private float flipThreshold = 0.1f;

        #endregion

        #region State

        private int _facingDirection = 1;
        private Vector3 _originalScale;
        private bool _isInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// Текущее направление: 1 = вправо, -1 = влево.
        /// </summary>
        public int FacingDirection => _facingDirection;

        /// <summary>
        /// Быстро проверить, смотрит ли персонаж вправо.
        /// </summary>
        public bool IsFacingRight => _facingDirection > 0;

        /// <summary>
        /// Быстро проверить, смотрит ли персонаж влево.
        /// </summary>
        public bool IsFacingLeft => _facingDirection < 0;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_isInitialized) return;

            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            _originalScale = transform.localScale;
            _facingDirection = faceRightByDefault ? 1 : -1;
            ApplyFacingDirection();

            _isInitialized = true;
        }

        #endregion

        #region Direction Control

        /// <summary>
        /// Устанавливает направление взгляда.
        /// </summary>
        /// <param name="direction">1 = вправо, -1 = влево</param>
        public void SetFacingDirection(int direction)
        {
            if (direction == 0) return;

            int newDirection = direction > 0 ? 1 : -1;

            if (_facingDirection != newDirection)
            {
                _facingDirection = newDirection;
                ApplyFacingDirection();
            }
        }

        /// <summary>
        /// Устанавливает направление взгляда через булево значение.
        /// </summary>
        /// <param name="faceRight">true = вправо, false = влево</param>
        public void SetFacingDirection(bool faceRight)
        {
            SetFacingDirection(faceRight ? 1 : -1);
        }

        /// <summary>
        /// Поворачивает персонажа к указанной точке.
        /// </summary>
        /// <param name="target">Точка в мире</param>
        public void FaceTowards(Vector2 target)
        {
            Vector2 position = transform.position;
            float direction = target.x - position.x;

            if (Mathf.Abs(direction) > flipThreshold)
            {
                SetFacingDirection(direction > 0 ? 1 : -1);
            }
        }

        /// <summary>
        /// Поворачивает персонажа в сторону движения.
        /// </summary>
        /// <param name="velocity">Вектор скорости</param>
        public void FaceMovementDirection(Vector2 velocity)
        {
            if (Mathf.Abs(velocity.x) > flipThreshold)
            {
                SetFacingDirection(velocity.x > 0 ? 1 : -1);
            }
        }

        /// <summary>
        /// Поворачивает персонажа к другому объекту.
        /// </summary>
        /// <param name="targetTransform">Трансформ цели</param>
        public void FaceTowards(Transform targetTransform)
        {
            if (targetTransform != null)
            {
                FaceTowards(targetTransform.position);
            }
        }

        /// <summary>
        /// Инвертирует текущее направление.
        /// </summary>
        public void Flip()
        {
            SetFacingDirection(-_facingDirection);
        }

        /// <summary>
        /// Сбрасывает направление к начальному.
        /// </summary>
        public void ResetDirection()
        {
            SetFacingDirection(faceRightByDefault ? 1 : -1);
        }

        #endregion

        #region Internal

        /// <summary>
        /// Применяет направление к трансформу.
        /// </summary>
        private void ApplyFacingDirection()
        {
            // Зеркалируем спрайт через scale
            Vector3 scale = _originalScale;
            scale.x = Mathf.Abs(scale.x) * _facingDirection;
            transform.localScale = scale;
        }

        #endregion

        #region Static Helpers

        /// <summary>
        /// Быстро зеркалирует спрайт через SpriteRenderer.flipX.
        /// Альтернативный метод для простых случаев.
        /// </summary>
        /// <param name="renderer">Спрайт-рендерер</param>
        /// <param name="faceRight">Смотреть вправо?</param>
        public static void FlipSprite(SpriteRenderer renderer, bool faceRight)
        {
            if (renderer != null)
            {
                renderer.flipX = !faceRight;
            }
        }

        /// <summary>
        /// Определяет направление по позиции мыши.
        /// </summary>
        /// <param name="characterPosition">Позиция персонажа</param>
        /// <returns>1 = вправо, -1 = влево</returns>
        public static int GetDirectionFromMouse(Vector2 characterPosition)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(
                Mouse.current != null ? (Vector3)Mouse.current.position.value : Vector3.zero);
            return mousePos.x > characterPosition.x ? 1 : -1;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Test Face Left")]
        private void TestFaceLeft() => SetFacingDirection(-1);

        [ContextMenu("Test Face Right")]
        private void TestFaceRight() => SetFacingDirection(1);

        [ContextMenu("Test Flip")]
        private void TestFlip() => Flip();

        private void OnValidate()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }
#endif

        #endregion
    }
}
