// ============================================================================
// NPCMovement.cs — Компонент движения NPC
// Cultivation World Simulator
// Создано: 2026-04-30 09:40:00 UTC
// ============================================================================
//
// Управляет перемещением NPC через Rigidbody2D.linearVelocity.
// Используется NPCAI для реализации состояний движения.
//
// Источник: checkpoints/04_30_npc_movement_combat_plan.md §Checkpoint 1
// ============================================================================

using UnityEngine;

namespace CultivationGame.NPC
{
    /// <summary>
    /// Компонент движения NPC — управляет перемещением через Rigidbody2D.
    /// Предоставляет методы для разных типов движения (блуждание, патруль,
    /// следование, бегство). Вызывается из NPCAI.ExecuteXxx().
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class NPCMovement : MonoBehaviour
    {
        [Header("Speed Settings")]
        [SerializeField] private float baseSpeed = 3f;
        [SerializeField] private float fleeSpeedMultiplier = 1.3f;
        [SerializeField] private float chaseSpeedMultiplier = 1.2f;

        [Header("Wander Settings")]
        [SerializeField] private float wanderRadius = 5f;
        [SerializeField] private float wanderPauseMin = 1f;
        [SerializeField] private float wanderPauseMax = 4f;
        [SerializeField] private float reachThreshold = 0.15f;

        // === Runtime ===
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Vector3 homePosition;
        private Vector3 currentTarget;
        private bool isMoving = false;
        private bool isWandering = false;
        private bool isWanderPaused = false;
        private float wanderPauseTimer = 0f;
        private float wanderPauseDuration = 2f;

        // === Properties ===

        /// <summary>Базовая скорость движения NPC.</summary>
        public float BaseSpeed => baseSpeed;

        /// <summary>Движется ли NPC в данный момент.</summary>
        public bool IsMoving => isMoving;

        /// <summary>Домашняя позиция NPC (центр блуждания).</summary>
        public Vector3 HomePosition
        {
            get => homePosition;
            set => homePosition = value;
        }

        // === Unity Lifecycle ===

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Запоминаем начальную позицию как домашнюю
            homePosition = transform.position;
        }

        // === Public Movement Methods ===

        /// <summary>
        /// Двигаться к указанной точке.
        /// </summary>
        /// <param name="target">Целевая позиция</param>
        /// <param name="speed">Скорость движения (если 0 — используется baseSpeed)</param>
        public void MoveTo(Vector3 target, float speed = 0f)
        {
            if (speed <= 0f) speed = baseSpeed;

            Vector2 direction = (target - transform.position);
            direction.z = 0f;

            // Если достаточно близко — останавливаемся
            if (direction.magnitude <= reachThreshold)
            {
                Stop();
                return;
            }

            direction.Normalize();
            rb.linearVelocity = direction * speed;
            isMoving = true;
            isWandering = false;

            UpdateFacing(direction);
        }

        /// <summary>
        /// Случайное блуждание вокруг центра.
        /// NPC выбирает случайную точку в радиусе, идёт к ней,
        /// делает паузу, затем выбирает следующую.
        /// </summary>
        /// <param name="center">Центр области блуждания</param>
        /// <param name="radius">Радиус блуждания (если 0 — используется wanderRadius)</param>
        /// <param name="speed">Скорость (если 0 — baseSpeed)</param>
        public void WanderAround(Vector3 center, float radius = 0f, float speed = 0f)
        {
            if (radius <= 0f) radius = wanderRadius;
            if (speed <= 0f) speed = baseSpeed;

            // Инициализация блуждания
            if (!isWandering)
            {
                isWandering = true;
                isWanderPaused = false;
                PickNewWanderTarget(center, radius);
            }

            // Пауза между блужданиями
            if (isWanderPaused)
            {
                wanderPauseTimer -= Time.deltaTime;
                if (wanderPauseTimer <= 0f)
                {
                    isWanderPaused = false;
                    PickNewWanderTarget(center, radius);
                }
                return;
            }

            // Двигаемся к текущей цели
            Vector2 direction = (currentTarget - transform.position);
            direction.z = 0f;

            if (direction.magnitude <= reachThreshold)
            {
                // Достигли цели — пауза
                Stop();
                isWanderPaused = true;
                wanderPauseDuration = Random.Range(wanderPauseMin, wanderPauseMax);
                wanderPauseTimer = wanderPauseDuration;
                return;
            }

            direction.Normalize();
            rb.linearVelocity = direction * speed;
            isMoving = true;

            UpdateFacing(direction);
        }

        /// <summary>
        /// Бегство от указанной точки.
        /// NPC движется в противоположном направлении от источника.
        /// </summary>
        /// <param name="source">Точка, от которой убегаем</param>
        /// <param name="speed">Скорость бегства (если 0 — baseSpeed * fleeSpeedMultiplier)</param>
        public void FleeFrom(Vector3 source, float speed = 0f)
        {
            if (speed <= 0f) speed = baseSpeed * fleeSpeedMultiplier;

            Vector2 direction = (transform.position - source);
            direction.z = 0f;

            if (direction.magnitude < 0.01f)
            {
                // Мы на одной точке с угрозой — бежим в случайном направлении
                direction = Random.insideUnitCircle.normalized;
            }
            else
            {
                direction.Normalize();
            }

            rb.linearVelocity = direction * speed;
            isMoving = true;
            isWandering = false;

            UpdateFacing(direction);
        }

        /// <summary>
        /// Следование за целью.
        /// </summary>
        /// <param name="target">Transform цели</param>
        /// <param name="stopDistance">Дистанция остановки</param>
        /// <param name="speed">Скорость (если 0 — baseSpeed * chaseSpeedMultiplier)</param>
        public void FollowTarget(Transform target, float stopDistance = 1.5f, float speed = 0f)
        {
            if (target == null)
            {
                Stop();
                return;
            }

            if (speed <= 0f) speed = baseSpeed * chaseSpeedMultiplier;

            Vector2 direction = (target.position - transform.position);
            direction.z = 0f;

            if (direction.magnitude <= stopDistance)
            {
                Stop();
                return;
            }

            direction.Normalize();
            rb.linearVelocity = direction * speed;
            isMoving = true;
            isWandering = false;

            UpdateFacing(direction);
        }

        /// <summary>
        /// Остановить движение NPC.
        /// </summary>
        public void Stop()
        {
            rb.linearVelocity = Vector2.zero;
            isMoving = false;
            isWandering = false;
            isWanderPaused = false;
        }

        // === Utility ===

        /// <summary>
        /// Расстояние от NPC до указанной точки.
        /// </summary>
        public float DistanceTo(Vector3 target)
        {
            Vector2 diff = target - transform.position;
            return diff.magnitude;
        }

        /// <summary>
        /// Достиг ли NPC указанной точки.
        /// </summary>
        public bool HasReachedTarget(Vector3 target, float threshold = 0f)
        {
            if (threshold <= 0f) threshold = reachThreshold;
            return DistanceTo(target) <= threshold;
        }

        /// <summary>
        /// Установить домашнюю позицию (центр блуждания).
        /// </summary>
        public void SetHomePosition(Vector3 position)
        {
            homePosition = position;
        }

        /// <summary>
        /// Установить базовую скорость.
        /// </summary>
        public void SetBaseSpeed(float speed)
        {
            baseSpeed = Mathf.Max(0.5f, speed);
        }

        /// <summary>
        /// Установить радиус блуждания.
        /// </summary>
        public void SetWanderRadius(float radius)
        {
            wanderRadius = Mathf.Max(1f, radius);
        }

        // === Private Methods ===

        /// <summary>
        /// Разворот спрайта по направлению движения.
        /// SpriteRenderer.flipX = true если двигается влево.
        /// </summary>
        private void UpdateFacing(Vector2 direction)
        {
            if (spriteRenderer == null) return;

            if (direction.x < -0.01f)
                spriteRenderer.flipX = true;
            else if (direction.x > 0.01f)
                spriteRenderer.flipX = false;
        }

        /// <summary>
        /// Выбрать новую случайную цель для блуждания.
        /// </summary>
        private void PickNewWanderTarget(Vector3 center, float radius)
        {
            currentTarget = GetRandomPointAround(center, radius);
            currentTarget.z = 0f;
        }

        /// <summary>
        /// Получить случайную точку вокруг центра.
        /// </summary>
        private Vector3 GetRandomPointAround(Vector3 center, float radius)
        {
            Vector2 randomOffset = Random.insideUnitCircle * radius;
            return new Vector3(center.x + randomOffset.x, center.y + randomOffset.y, 0f);
        }
    }
}
