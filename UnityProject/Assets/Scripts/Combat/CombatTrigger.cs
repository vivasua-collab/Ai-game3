// ============================================================================
// CombatTrigger.cs — Триггер боя (2D коллайдер)
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-05-04 04:35:00 UTC
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Компонент-триггер боя. Вешается на NPC/врага вместе с Collider2D (isTrigger=true).
    /// При контакте Player/NPC с враждебным CombatTrigger — автоматически инициируется бой.
    ///
    /// Логика:
    /// 1. OnTriggerEnter2D → проверка отношения (Attitude)
    /// 2. Если Hostile/Hatred → CombatManager.InitiateCombat()
    /// 3. Если Neutral/Friendly → игнорируется
    /// 4. Радиус триггера = attackRange + buffer
    /// </summary>
    [RequireComponent(typeof(CircleCollider2D))]
    public class CombatTrigger : MonoBehaviour
    {
        [Header("Настройки триггера")]
        [Tooltip("Радиус триггера агра (перекрывает радиус коллайдера)")]
        [SerializeField] private float triggerRadius = 3f;
        [Tooltip("Автоматически вступать в бой при контакте")]
        [SerializeField] private bool autoEngage = true;
        [Tooltip("Кулдаун между попытками агра (сек)")]
        [SerializeField] private float aggroCooldown = 5f;
        [Tooltip("Минимальное отношение для агра")]
        [SerializeField] private Attitude minAttitudeToEngage = Attitude.Hostile;
        [Tooltip("Радиус атаки (для ближнего боя)")]
        [SerializeField] private float attackRange = 1.5f;

        // === Runtime ===
        private ICombatant ownerCombatant;
        private float lastAggroTime;
        private CircleCollider2D triggerCollider;

        // === Свойства ===
        public ICombatant Owner => ownerCombatant;
        public float TriggerRadius => triggerRadius;

        // === Unity Lifecycle ===

        private void Awake()
        {
            triggerCollider = GetComponent<CircleCollider2D>();
            if (triggerCollider != null)
            {
                triggerCollider.isTrigger = true;
                triggerCollider.radius = triggerRadius;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!autoEngage) return;
            if (Time.time - lastAggroTime < aggroCooldown) return;

            // Проверяем, что вошедший — ICombatant
            var targetCombatant = other.GetComponent<ICombatant>() as MonoBehaviour;
            if (targetCombatant == null) return;

            // Не атакуем самого себя
            if (targetCombatant.gameObject == gameObject) return;

            // Проверяем отношение
            // TODO: Интеграция с RelationshipController для реальной проверки Attitude
            // Пока: любой CombatTrigger агрит на Player/NPC с другим тегом
            if (!ShouldEngage(targetCombatant)) return;

            // Получаем ICombatant владельца
            if (ownerCombatant == null)
            {
                ownerCombatant = GetComponent<ICombatant>() as ICombatant;
                if (ownerCombatant == null)
                {
                    // Пробуем через родителя
                    ownerCombatant = GetComponentInParent<ICombatant>() as ICombatant;
                }
            }

            if (ownerCombatant == null) return;

            // Инициируем бой
            var target = (ICombatant)targetCombatant;
            if (CombatManager.Instance != null && !CombatManager.Instance.IsInCombat)
            {
                lastAggroTime = Time.time;
                CombatManager.Instance.InitiateCombat(ownerCombatant, target);
                Debug.Log($"[CombatTrigger] Бой инициирован: {ownerCombatant.Name} → {target.Name}");
            }
        }

        /// <summary>
        /// Проверить, нужно ли атаковать цель.
        /// TODO: Интеграция с RelationshipController.
        /// </summary>
        private bool ShouldEngage(MonoBehaviour target)
        {
            // Временная логика: атакуем по тегу "Player" если мы "Enemy"
            if (gameObject.CompareTag("Enemy") && target.CompareTag("Player"))
                return true;
            if (gameObject.CompareTag("Player") && target.CompareTag("Enemy"))
                return true;

            // По умолчанию — проверка отношения (когда будет RelationshipController)
            return false;
        }

        /// <summary>
        /// Установить владельца триггера (вызывается из NPCController).
        /// </summary>
        public void SetOwner(ICombatant combatant)
        {
            ownerCombatant = combatant;
        }

        /// <summary>
        /// Обновить радиус триггера.
        /// </summary>
        public void SetTriggerRadius(float radius)
        {
            triggerRadius = radius;
            if (triggerCollider != null)
            {
                triggerCollider.radius = radius;
            }
        }

        // === Отрисовка Gizmo (редактор) ===

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, triggerRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }
    }
}
