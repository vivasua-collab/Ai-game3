// Создано: 2026-04-03
// Источник: docs/examples/OrbitalWeaponSystem.md

using UnityEngine;
using UnityEngine.UI;
using CultivationGame.Core;
using CultivationGame.Combat.OrbitalSystem; // FIX: needed for OrbitalWeaponController (2026-04-12)

namespace CultivationGame.UI // FIX: CultivationWorld→CultivationGame (2026-04-12)
{
    /// <summary>
    /// UI индикатор, показывающий направление ближайшего орбитального оружия.
    /// 
    /// Функции:
    /// - Показывает направление атаки
    /// - Визуальный помощник для игрока
    /// - Может менять цвет по элементу
    /// </summary>
    public class WeaponDirectionIndicator : MonoBehaviour
    {
        #region Configuration

        [Header("References")]
        [SerializeField] private OrbitalWeaponController weaponController;
        [SerializeField] private Image indicatorArrow;
        [SerializeField] private Image indicatorRing;

        [Header("Position")]
        [SerializeField] private float indicatorDistance = 50f;
        [SerializeField] private bool followWeapon = true;

        [Header("Visual")]
        [SerializeField] private Color neutralColor = Color.white;
        [SerializeField] private Color fireColor = new Color(1f, 0.3f, 0.1f);
        [SerializeField] private Color waterColor = new Color(0.2f, 0.5f, 1f);
        [SerializeField] private Color lightningColor = new Color(0.8f, 0.8f, 1f);
        [SerializeField] private Color earthColor = new Color(0.6f, 0.4f, 0.2f);
        [SerializeField] private Color airColor = new Color(0.7f, 1f, 0.7f);
        [SerializeField] private Color poisonColor = new Color(0.5f, 0f, 0.8f);
        [SerializeField] private Color voidColor = new Color(0.3f, 0f, 0.5f);

        [Header("Animation")]
        [SerializeField] private bool animate = true;
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseScale = 1.2f;

        #endregion

        #region State

        private RectTransform _rectTransform;
        private Vector3 _originalScale;
        private float _pulseTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();

            if (indicatorArrow != null)
            {
                _originalScale = indicatorArrow.rectTransform.localScale;
            }
        }

        private void Update()
        {
            if (weaponController == null)
            {
                FindWeaponController();
                return;
            }

            UpdateIndicator();
            UpdateAnimation();
        }

        #endregion

        #region Update

        /// <summary>
        /// Обновляет позицию и вращение индикатора.
        /// </summary>
        private void UpdateIndicator()
        {
            if (indicatorArrow == null) return;

            // Получаем направление ближайшего оружия
            Vector2 direction = weaponController.GetNearestWeaponDirection();

            // Поворачиваем индикатор
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            indicatorArrow.rectTransform.rotation = Quaternion.Euler(0, 0, angle);

            // Позиция индикатора относительно центра
            if (followWeapon)
            {
                Vector2 position = direction * indicatorDistance;
                indicatorArrow.rectTransform.anchoredPosition = position;
            }

            // Обновляем цвет по элементу
            UpdateColor();
        }

        /// <summary>
        /// Обновляет цвет индикатора.
        /// </summary>
        private void UpdateColor()
        {
            // TODO: Получить элемент ближайшего оружия
            // var nearestWeapon = weaponController.GetNearestWeapon(Vector2.zero);
            // if (nearestWeapon != null)
            // {
            //     indicatorArrow.color = GetElementColor(nearestWeapon.Element);
            // }
        }

        /// <summary>
        /// Анимация пульсации.
        /// </summary>
        private void UpdateAnimation()
        {
            if (!animate || indicatorArrow == null) return;

            _pulseTime += Time.deltaTime * pulseSpeed;
            float scale = 1f + (Mathf.Sin(_pulseTime) * 0.5f + 0.5f) * (pulseScale - 1f);
            indicatorArrow.rectTransform.localScale = _originalScale * scale;
        }

        #endregion

        #region Helpers

        private void FindWeaponController()
        {
            // Автоматический поиск контроллера
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                weaponController = player.GetComponent<OrbitalWeaponController>();
            }
        }

        private Color GetElementColor(Element element)
        {
            return element switch
            {
                Element.Fire => fireColor,
                Element.Water => waterColor,
                Element.Lightning => lightningColor,
                Element.Earth => earthColor,
                Element.Air => airColor,
                Element.Poison => poisonColor,
                Element.Void => voidColor,
                _ => neutralColor
            };
        }

        #endregion

        #region Public API

        /// <summary>
        /// Устанавливает контроллер оружия.
        /// </summary>
        public void SetWeaponController(OrbitalWeaponController controller)
        {
            weaponController = controller;
        }

        /// <summary>
        /// Устанавливает дистанцию индикатора.
        /// </summary>
        public void SetDistance(float distance)
        {
            indicatorDistance = distance;
        }

        /// <summary>
        /// Включает/выключает анимацию.
        /// </summary>
        public void SetAnimate(bool enabled)
        {
            animate = enabled;

            if (!animate && indicatorArrow != null)
            {
                indicatorArrow.rectTransform.localScale = _originalScale;
            }
        }

        /// <summary>
        /// Показывает/скрывает индикатор.
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (indicatorArrow != null)
            {
                indicatorArrow.enabled = visible;
            }

            if (indicatorRing != null)
            {
                indicatorRing.enabled = visible;
            }
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        [ContextMenu("Test Direction - Up")]
        private void TestDirectionUp()
        {
            if (indicatorArrow != null)
            {
                indicatorArrow.rectTransform.rotation = Quaternion.Euler(0, 0, 90);
            }
        }

        [ContextMenu("Test Direction - Right")]
        private void TestDirectionRight()
        {
            if (indicatorArrow != null)
            {
                indicatorArrow.rectTransform.rotation = Quaternion.Euler(0, 0, 0);
            }
        }

        [ContextMenu("Test Direction - Down")]
        private void TestDirectionDown()
        {
            if (indicatorArrow != null)
            {
                indicatorArrow.rectTransform.rotation = Quaternion.Euler(0, 0, -90);
            }
        }
#endif

        #endregion
    }
}
