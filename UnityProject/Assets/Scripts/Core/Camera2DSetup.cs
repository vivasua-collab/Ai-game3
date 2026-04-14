// ============================================================================
// Camera2DSetup.cs — Настройка и слежение камеры для 2D
// Cultivation World Simulator
// Версия: 2.0 — добавлено слежение за целью и ограничение границами карты
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-14 07:30:00 UTC — v2.0: Camera Follow + Bounds
// ============================================================================

using UnityEngine;
using CultivationGame.Player;

namespace CultivationGame.Core
{
    /// <summary>
    /// Настраивает 2D камеру и обеспечивает плавное слежение за целью.
    /// Ограничивает камеру границами карты.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class Camera2DSetup : MonoBehaviour
    {
        [Header("2D Camera Settings")]
        [Tooltip("Z позиция камеры (должна быть отрицательной)")]
        public float cameraZ = -10f;
        
        [Tooltip("Размер ортографической проекции")]
        public float orthographicSize = 8f;
        
        [Tooltip("Цвет фона")]
        public Color backgroundColor = new Color(0.15f, 0.18f, 0.15f, 1f); // Тёмно-зелёный
        
        [Tooltip("Настраивать при старте")]
        public bool setupOnStart = true;

        [Header("Camera Follow")]
        [Tooltip("Цель слежения (если null — ищется игрок автоматически)")]
        public Transform followTarget;
        
        [Tooltip("Включить слежение")]
        public bool followEnabled = true;
        
        [Tooltip("Скорость слежения (0 = мгновенное, 1 = плавное)")]
        [Range(0.01f, 1f)]
        public float followSmoothness = 0.08f;
        
        [Tooltip("Смещение от цели")]
        public Vector2 followOffset = Vector2.zero;

        [Header("Camera Bounds")]
        [Tooltip("Ограничивать камеру границами")]
        public bool useBounds = true;
        
        [Tooltip("Минимальная позиция камеры (левый нижний угол карты)")]
        public Vector2 boundsMin = new Vector2(0f, 0f);
        
        [Tooltip("Максимальная позиция камеры (правый верхний угол карты)")]
        public Vector2 boundsMax = new Vector2(200f, 120f);

        // === Runtime ===
        private Camera cam;
        private Vector3 velocity;
        private bool hasTarget;

        // === Properties ===
        /// <summary>Половина ширины видимой области камеры в мировых единицах.</summary>
        public float HalfWidth => cam != null ? cam.orthographicSize * cam.aspect : 0f;
        /// <summary>Половина высоты видимой области камеры в мировых единицах.</summary>
        public float HalfHeight => cam != null ? cam.orthographicSize : 0f;

        // === Unity Lifecycle ===

        private void Awake()
        {
            cam = GetComponent<Camera>();
            
            if (setupOnStart)
            {
                SetupCamera();
            }
        }

        private void Start()
        {
            // Автоматически найти цель — игрок
            if (followTarget == null && followEnabled)
            {
                FindPlayerTarget();
            }
        }

        private void LateUpdate()
        {
            // Слежение за целью
            if (followEnabled && followTarget != null)
            {
                FollowTarget();
            }
        }

        // === Setup ===

        /// <summary>
        /// Настроить камеру.
        /// </summary>
        [ContextMenu("Setup Camera Now")]
        public void SetupCamera()
        {
            if (cam == null)
                cam = GetComponent<Camera>();
            
            // Ортографическая проекция
            cam.orthographic = true;
            cam.orthographicSize = orthographicSize;
            
            // Z позиция
            Vector3 pos = transform.position;
            pos.z = cameraZ;
            transform.position = pos;
            
            // Цвет фона
            cam.backgroundColor = backgroundColor;
            cam.clearFlags = CameraClearFlags.SolidColor;
            
            // Глубина
            cam.depth = 0;
            
            Debug.Log($"[Camera2DSetup] Камера настроена: size={cam.orthographicSize}, bg={backgroundColor}");
        }

        // === Follow ===

        /// <summary>
        /// Установить цель слежения.
        /// </summary>
        public void SetFollowTarget(Transform target)
        {
            followTarget = target;
            hasTarget = target != null;
            
            if (hasTarget)
            {
                // Мгновенный переход к цели
                Vector3 targetPos = new Vector3(
                    target.position.x + followOffset.x,
                    target.position.y + followOffset.y,
                    cameraZ
                );
                transform.position = ClampToBounds(targetPos);
            }
            
            Debug.Log($"[Camera2DSetup] Цель слежения: {(target != null ? target.name : "null")}");
        }

        /// <summary>
        /// Установить границы карты для ограничения камеры.
        /// </summary>
        public void SetBounds(Vector2 min, Vector2 max)
        {
            boundsMin = min;
            boundsMax = max;
            useBounds = true;
            
            Debug.Log($"[Camera2DSetup] Границы: ({min.x:F0},{min.y:F0}) — ({max.x:F0},{max.y:F0})");
        }

        /// <summary>
        /// Автоматически найти игрока.
        /// Порядок: ServiceLocator → по тегу → по имени.
        /// </summary>
        public void FindPlayerTarget()
        {
            // Попробовать через ServiceLocator
            var playerCtrl = ServiceLocator.GetOrFind<Player.PlayerController>();
            if (playerCtrl != null)
            {
                SetFollowTarget(playerCtrl.transform);
                return;
            }
            
            // Попробовать по тегу
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                SetFollowTarget(playerObj.transform);
                return;
            }
            
            // Попробовать по имени
            var byName = GameObject.Find("Player");
            if (byName != null)
            {
                SetFollowTarget(byName.transform);
            }
        }

        // === Private ===

        private void FollowTarget()
        {
            Vector3 desiredPos = new Vector3(
                followTarget.position.x + followOffset.x,
                followTarget.position.y + followOffset.y,
                cameraZ
            );

            // Плавное следование (SmoothDamp)
            Vector3 smoothed = Vector3.SmoothDamp(
                transform.position,
                desiredPos,
                ref velocity,
                followSmoothness
            );

            // Ограничить границами
            transform.position = ClampToBounds(smoothed);
        }

        /// <summary>
        /// Ограничить позицию камеры границами карты.
        /// Камера не должна показывать пустоту за пределами карты.
        /// </summary>
        private Vector3 ClampToBounds(Vector3 position)
        {
            if (!useBounds || cam == null) return position;

            float halfW = HalfWidth;
            float halfH = HalfHeight;

            // Минимальная позиция — камера не заходит левее/ниже boundsMin
            float minX = boundsMin.x + halfW;
            float minY = boundsMin.y + halfH;

            // Максимальная позиция — камера не заходит правее/выше boundsMax
            float maxX = boundsMax.x - halfW;
            float maxY = boundsMax.y - halfH;

            // Если карта меньше видимой области — центрируем
            if (minX > maxX) { minX = maxX = (boundsMin.x + boundsMax.x) * 0.5f; }
            if (minY > maxY) { minY = maxY = (boundsMin.y + boundsMax.y) * 0.5f; }

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);

            return position;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            // Автоприменение в редакторе при изменении значений
            if (cam != null && !Application.isPlaying)
            {
                SetupCamera();
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!useBounds) return;

            // Рисовать границы камеры
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Vector3 center = new Vector3(
                (boundsMin.x + boundsMax.x) * 0.5f,
                (boundsMin.y + boundsMax.y) * 0.5f,
                0f
            );
            Vector3 size = new Vector3(
                boundsMax.x - boundsMin.x,
                boundsMax.y - boundsMin.y,
                0.1f
            );
            Gizmos.DrawWireCube(center, size);
        }
#endif
    }
}
