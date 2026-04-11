// Создано: 2026-04-03
// Источник: docs/examples/CharacterSpriteMirroring.md

using UnityEngine;

namespace CultivationGame.Character // FIX CHR-H01: CultivationWorld→CultivationGame (2026-04-12)
{
    /// <summary>
    /// Контроллер для объектов, которые НЕ должны зеркалиться вместе с персонажем.
    /// 
    /// Проблема:
    /// При зеркалировании родителя через transform.localScale.x = -1,
    /// все дочерние объекты тоже зеркалируются.
    /// 
    /// Решение:
    /// Компенсируем зеркалирование, сохраняя оригинальный масштаб.
    /// 
    /// Примеры использования:
    /// - Орбитальное оружие
    /// - Эффекты частиц
    /// - UI элементы над персонажем
    /// </summary>
    public class IndependentScale : MonoBehaviour
    {
        #region Configuration

        [Header("Settings")]
        [SerializeField] private bool preserveX = true;
        [SerializeField] private bool preserveY = false;
        [SerializeField] private bool preserveZ = false;
        [SerializeField] private bool checkParentChain = true;

        #endregion

        #region State

        private Vector3 _originalScale;
        private Transform _parentTransform;
        private Transform _originalParent;
        private bool _isInitialized;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            Initialize();
        }

        private void LateUpdate()
        {
            ApplyIndependentScale();
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (_isInitialized) return;

            _originalScale = transform.localScale;
            _originalParent = transform.parent;
            _parentTransform = transform.parent;

            _isInitialized = true;
        }

        #endregion

        #region Scale Compensation

        /// <summary>
        /// Применяет независимый масштаб.
        /// </summary>
        private void ApplyIndependentScale()
        {
            if (!_isInitialized) return;

            // Проверяем родителей на зеркалирование
            Vector3 scaleMultiplier = GetParentScaleMultiplier();

            // Компенсируем масштаб
            Vector3 newScale = _originalScale;

            if (preserveX && scaleMultiplier.x < 0)
            {
                newScale.x *= -1;
            }

            if (preserveY && scaleMultiplier.y < 0)
            {
                newScale.y *= -1;
            }

            if (preserveZ && scaleMultiplier.z < 0)
            {
                newScale.z *= -1;
            }

            transform.localScale = newScale;
        }

        /// <summary>
        /// Получает мультипликатор масштаба от родителей.
        /// </summary>
        private Vector3 GetParentScaleMultiplier()
        {
            Vector3 multiplier = Vector3.one;

            if (!checkParentChain)
            {
                if (_parentTransform != null)
                {
                    multiplier = _parentTransform.localScale;
                }
                return multiplier;
            }

            // Проходим по всей цепочке родителей
            Transform current = transform.parent;

            while (current != null)
            {
                multiplier.x *= current.localScale.x > 0 ? 1 : -1;
                multiplier.y *= current.localScale.y > 0 ? 1 : -1;
                multiplier.z *= current.localScale.z > 0 ? 1 : -1;

                current = current.parent;
            }

            return multiplier;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Обновляет оригинальный масштаб.
        /// </summary>
        public void UpdateOriginalScale()
        {
            _originalScale = transform.localScale;
        }

        /// <summary>
        /// Устанавливает оригинальный масштаб.
        /// </summary>
        public void SetOriginalScale(Vector3 scale)
        {
            _originalScale = scale;
        }

        /// <summary>
        /// Сбрасывает к оригинальному масштабу.
        /// </summary>
        public void ResetToOriginal()
        {
            transform.localScale = _originalScale;
        }

        /// <summary>
        /// Включает/выключает сохранение по осям.
        /// </summary>
        public void SetPreserveAxes(bool x, bool y, bool z)
        {
            preserveX = x;
            preserveY = y;
            preserveZ = z;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && _isInitialized)
            {
                ApplyIndependentScale();
            }
        }

        [ContextMenu("Update Original Scale")]
        private void UpdateScaleInEditor()
        {
            _originalScale = transform.localScale;
            Debug.Log($"[IndependentScale] Original scale updated to: {_originalScale}");
        }

        [ContextMenu("Reset to Original")]
        private void ResetInEditor()
        {
            ResetToOriginal();
        }
#endif

        #endregion
    }
}
