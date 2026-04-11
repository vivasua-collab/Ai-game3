// Создано: 2026-04-03
// Источник: docs/examples/OrbitalWeaponSystem.md

using UnityEngine;
using System.Collections.Generic;

namespace CultivationGame.Combat.OrbitalSystem // FIX: CultivationWorld→CultivationGame (2026-04-12)
{
    /// <summary>
    /// Контроллер орбитального оружия.
    /// Управляет оружием, летящим по круговой орбите вокруг персонажа.
    /// 
    /// Концепция:
    /// - Оружие вращается вокруг персонажа по круговой орбите
    /// - Указывает направление атаки (куда смотрит оружие)
    /// - При атаке ускоряется и наносит урон в направлении
    /// </summary>
    public class OrbitalWeaponController : MonoBehaviour
    {
        #region Configuration

        [Header("Orbit Settings")]
        [SerializeField] private float orbitRadius = 1.5f;
        [SerializeField] private float orbitSpeed = 90f; // градусов в секунду
        [SerializeField] private bool clockwiseRotation = true;

        [Header("Weapons")]
        [SerializeField] private List<OrbitalWeapon> weapons = new List<OrbitalWeapon>();

        [Header("Attack Settings")]
        [SerializeField] private float attackRotationSpeed = 360f; // Скорость при атаке
        [SerializeField] private float attackDuration = 0.3f;
        [SerializeField] private AnimationCurve attackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        #endregion

        #region State

        private Transform _characterTransform;
        private float _currentAngle;
        private bool _isAttacking;
        private bool _isInitialized;

        #endregion

        #region Properties

        /// <summary>
        /// Текущий угол орбиты в градусах.
        /// </summary>
        public float CurrentAngle => _currentAngle;

        /// <summary>
        /// Количество активного оружия.
        /// </summary>
        public int ActiveWeaponCount
        {
            get
            {
                int count = 0;
                foreach (var weapon in weapons)
                {
                    if (weapon != null && weapon.IsActive) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Находится ли в состоянии атаки.
        /// </summary>
        public bool IsAttacking => _isAttacking;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _characterTransform = transform;
            _currentAngle = 0f;
            _isInitialized = true;
        }

        private void Update()
        {
            if (!_isInitialized) return;
            UpdateWeaponPositions();
        }

        #endregion

        #region Position Update

        /// <summary>
        /// Обновляет позиции оружия на орбите.
        /// </summary>
        private void UpdateWeaponPositions()
        {
            float speed = _isAttacking ? attackRotationSpeed : orbitSpeed;
            int direction = clockwiseRotation ? 1 : -1;

            _currentAngle += speed * direction * Time.deltaTime;
            _currentAngle %= 360f;

            // Распределяем оружие равномерно по орбите
            float angleStep = weapons.Count > 0 ? 360f / weapons.Count : 0f;

            for (int i = 0; i < weapons.Count; i++)
            {
                if (weapons[i] == null || !weapons[i].IsActive) continue;

                float weaponAngle = _currentAngle + (angleStep * i);
                Vector3 offset = CalculateOrbitOffset(weaponAngle);

                weapons[i].transform.position = _characterTransform.position + offset;

                // Поворот оружия в направлении движения по орбите
                float rotationAngle = weaponAngle + (clockwiseRotation ? 90f : -90f);
                weapons[i].transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
            }
        }

        /// <summary>
        /// Вычисляет смещение от центра орбиты.
        /// </summary>
        private Vector3 CalculateOrbitOffset(float angleDegrees)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Cos(radians) * orbitRadius,
                Mathf.Sin(radians) * orbitRadius,
                0f
            );
        }

        #endregion

        #region Attack

        /// <summary>
        /// Атака в указанном направлении.
        /// Оружие ускоряется и наносит урон при контакте.
        /// </summary>
        /// <param name="direction">Направление атаки (нормализованный вектор)</param>
        public void Attack(Vector2 direction)
        {
            if (weapons.Count == 0 || _isAttacking) return;
            StartCoroutine(AttackCoroutine(direction));
        }

        private System.Collections.IEnumerator AttackCoroutine(Vector2 direction)
        {
            _isAttacking = true;

            // Вычисляем целевой угол
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Анимация атаки
            float elapsed = 0f;
            float startAngle = _currentAngle;

            while (elapsed < attackDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / attackDuration;
                float curveValue = attackCurve.Evaluate(t);

                // Интерполируем угол к цели
                _currentAngle = Mathf.LerpAngle(startAngle, targetAngle, curveValue);

                yield return null;
            }

            // Наносим урон
            foreach (var weapon in weapons)
            {
                if (weapon != null && weapon.IsActive)
                {
                    weapon.OnAttackHit();
                }
            }

            _isAttacking = false;
        }

        /// <summary>
        /// Быстрая атака без анимации (мгновенный удар).
        /// </summary>
        public void QuickAttack(Vector2 direction)
        {
            if (weapons.Count == 0) return;

            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _currentAngle = targetAngle;

            foreach (var weapon in weapons)
            {
                if (weapon != null && weapon.IsActive)
                {
                    weapon.OnAttackHit();
                }
            }
        }

        #endregion

        #region Weapon Management

        /// <summary>
        /// Добавляет оружие на орбиту.
        /// </summary>
        public void AddWeapon(OrbitalWeapon weapon)
        {
            if (weapon == null || weapons.Contains(weapon)) return;

            weapons.Add(weapon);
            weapon.Initialize(this);
        }

        /// <summary>
        /// Удаляет оружие с орбиты.
        /// </summary>
        public void RemoveWeapon(OrbitalWeapon weapon)
        {
            if (weapon == null) return;
            weapons.Remove(weapon);
        }

        /// <summary>
        /// Очищает все оружие.
        /// </summary>
        public void ClearWeapons()
        {
            foreach (var weapon in weapons)
            {
                if (weapon != null)
                {
                    weapon.Deinitialize();
                }
            }
            weapons.Clear();
        }

        #endregion

        #region Queries

        /// <summary>
        /// Получает ближайшее оружие к точке.
        /// </summary>
        public OrbitalWeapon GetNearestWeapon(Vector2 point)
        {
            OrbitalWeapon nearest = null;
            float minDistance = float.MaxValue;

            foreach (var weapon in weapons)
            {
                if (weapon == null || !weapon.IsActive) continue;

                float distance = Vector2.Distance(weapon.transform.position, point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = weapon;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Получает направление ближайшего оружия (для UI индикатора).
        /// </summary>
        public Vector2 GetNearestWeaponDirection()
        {
            float radians = _currentAngle * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }

        /// <summary>
        /// Получает позицию оружия по индексу.
        /// </summary>
        public Vector2 GetWeaponPosition(int index)
        {
            if (index < 0 || index >= weapons.Count) return transform.position;

            float angleStep = weapons.Count > 0 ? 360f / weapons.Count : 0f;
            float weaponAngle = _currentAngle + (angleStep * index);

            return transform.position + CalculateOrbitOffset(weaponAngle);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Устанавливает радиус орбиты.
        /// </summary>
        public void SetOrbitRadius(float radius)
        {
            orbitRadius = Mathf.Max(0.5f, radius);
        }

        /// <summary>
        /// Устанавливает скорость вращения.
        /// </summary>
        public void SetOrbitSpeed(float degreesPerSecond)
        {
            orbitSpeed = Mathf.Max(0f, degreesPerSecond);
        }

        /// <summary>
        /// Устанавливает направление вращения.
        /// </summary>
        public void SetClockwise(bool clockwise)
        {
            clockwiseRotation = clockwise;
        }

        #endregion

        #region Editor

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Рисуем орбиту
            UnityEditor.Handles.color = Color.cyan;
            UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, orbitRadius);

            // Рисуем позиции оружия
            if (weapons.Count > 0)
            {
                float angleStep = 360f / weapons.Count;
                for (int i = 0; i < weapons.Count; i++)
                {
                    float angle = (angleStep * i) * Mathf.Deg2Rad;
                    Vector3 pos = transform.position + new Vector3(
                        Mathf.Cos(angle) * orbitRadius,
                        Mathf.Sin(angle) * orbitRadius,
                        0f
                    );

                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(pos, 0.2f);
                }
            }
        }
#endif

        #endregion
    }
}
