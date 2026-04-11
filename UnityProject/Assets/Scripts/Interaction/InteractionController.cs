// ============================================================================
// InteractionController.cs — Контроллер взаимодействий игрока
// Cultivation World Simulator
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-11 07:26:00 UTC — Disposition→Attitude+PersonalityTrait миграция
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.NPC;
using CultivationGame.Combat;
using CultivationGame.Qi;
using CultivationGame.Body;

namespace CultivationGame.Interaction
{
    /// <summary>
    /// Типы взаимодействия.
    /// </summary>
    public enum InteractionType
    {
        Talk,           // Разговор
        Trade,          // Торговля
        Attack,         // Атака
        Challenge,      // Вызов на дуэль
        Recruit,        // Вербовка в секту
        Teach,          // Обучение технике
        Learn,          // Изучение техники
        Spar,           // Спарринг
        Give,           // Передача предмета
        Ask,            // Вопрос
        Flatter,        // Лесть
        Threaten,       // Угроза
        Gift,           // Подарок
        Insult,         // Оскорбление
        Rescue,         // Спасение
        Heal,           // Лечение
        Cultivate,      // Совместная культивация
        Meditate        // Медитация
    }
    
    /// <summary>
    /// Результат взаимодействия.
    /// </summary>
    [Serializable]
    public class InteractionResult
    {
        public bool Success;
        public InteractionType Type;
        public string Message;
        public int RelationshipChange;
        public long QiChange; // FIX: int→long Qi migration (2026-04-12)
        public int HealthChange;
        public List<string> UnlockedInteractions;
        public Dictionary<string, object> CustomData;
        
        public InteractionResult()
        {
            Success = false;
            Message = "";
            UnlockedInteractions = new List<string>();
            CustomData = new Dictionary<string, object>();
        }
    }
    
    /// <summary>
    /// Контроллер взаимодействий — управление взаимодействиями игрока с миром.
    /// </summary>
    public class InteractionController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactableLayers;
        
        [Header("References")]
        [SerializeField] private QiController playerQi;
        [SerializeField] private BodyController playerBody;
        [SerializeField] private TechniqueController playerTechniques;
        
        // === Runtime ===
        private List<Interactable> nearbyInteractables = new List<Interactable>();
        private Interactable currentTarget;
        private Dictionary<string, float> cooldowns = new Dictionary<string, float>();
        
        // === Events ===
        public event Action<Interactable> OnTargetAcquired;
        public event Action<Interactable> OnTargetLost;
        public event Action<Interactable, InteractionType, InteractionResult> OnInteractionComplete;
        public event Action<List<InteractionType>> OnAvailableInteractionsUpdated;
        
        // === Properties ===
        public Interactable CurrentTarget => currentTarget;
        public List<Interactable> NearbyInteractables => nearbyInteractables;
        
        // === Unity Lifecycle ===
        
        private void Update()
        {
            UpdateCooldowns();
            ScanForInteractables();
        }
        
        // === Scanning ===
        
        private void ScanForInteractables()
        {
            // Находим все интерактивные объекты в радиусе
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRange, interactableLayers);
            
            List<Interactable> newNearby = new List<Interactable>();
            
            foreach (var hit in hits)
            {
                Interactable interactable = hit.GetComponent<Interactable>();
                if (interactable != null && interactable.CanInteract(this))
                {
                    newNearby.Add(interactable);
                }
            }
            
            // Проверяем изменения
            if (newNearby.Count != nearbyInteractables.Count)
            {
                nearbyInteractables = newNearby;
                
                // Автовыбор ближайшей цели
                if (currentTarget == null && nearbyInteractables.Count > 0)
                {
                    SelectTarget(nearbyInteractables[0]);
                }
                else if (currentTarget != null && !nearbyInteractables.Contains(currentTarget))
                {
                    ClearTarget();
                }
            }
        }
        
        // === Target Management ===
        
        /// <summary>
        /// Выбрать цель для взаимодействия.
        /// </summary>
        public void SelectTarget(Interactable target)
        {
            if (target == null || !nearbyInteractables.Contains(target)) return;
            
            if (currentTarget != null && currentTarget != target)
            {
                OnTargetLost?.Invoke(currentTarget);
            }
            
            currentTarget = target;
            OnTargetAcquired?.Invoke(target);
            
            // Обновляем доступные взаимодействия
            UpdateAvailableInteractions();
        }
        
        /// <summary>
        /// Очистить текущую цель.
        /// </summary>
        public void ClearTarget()
        {
            if (currentTarget != null)
            {
                OnTargetLost?.Invoke(currentTarget);
            }
            currentTarget = null;
        }
        
        private void UpdateAvailableInteractions()
        {
            if (currentTarget == null) return;
            
            List<InteractionType> available = currentTarget.GetAvailableInteractions(this);
            OnAvailableInteractionsUpdated?.Invoke(available);
        }
        
        // === Interaction Execution ===
        
        /// <summary>
        /// Выполнить взаимодействие.
        /// </summary>
        public InteractionResult Interact(InteractionType type)
        {
            if (currentTarget == null)
            {
                return new InteractionResult
                {
                    Success = false,
                    Message = "No target selected"
                };
            }
            
            // Проверка кулдауна
            string cooldownKey = $"{currentTarget.Id}_{type}";
            if (cooldowns.TryGetValue(cooldownKey, out float remaining) && remaining > 0)
            {
                return new InteractionResult
                {
                    Success = false,
                    Message = $"Cooldown: {remaining:F1}s remaining"
                };
            }
            
            // Выполняем взаимодействие
            InteractionResult result = currentTarget.Interact(this, type);
            
            // Устанавливаем кулдаун
            if (result.Success)
            {
                cooldowns[cooldownKey] = GetCooldownForType(type);
            }
            
            OnInteractionComplete?.Invoke(currentTarget, type, result);
            
            return result;
        }
        
        // === Interaction Helpers ===
        
        /// <summary>
        /// Получить кулдаун для типа взаимодействия.
        /// </summary>
        private float GetCooldownForType(InteractionType type)
        {
            switch (type)
            {
                case InteractionType.Attack:
                    return 1f;
                case InteractionType.Talk:
                    return 0.5f;
                case InteractionType.Trade:
                    return 0.5f;
                case InteractionType.Gift:
                    return 60f;
                case InteractionType.Flatter:
                    return 30f;
                case InteractionType.Threaten:
                    return 60f;
                case InteractionType.Insult:
                    return 60f;
                default:
                    return 1f;
            }
        }
        
        private void UpdateCooldowns()
        {
            List<string> expiredKeys = new List<string>();
            
            foreach (var kvp in cooldowns)
            {
                if (kvp.Value > 0)
                {
                    cooldowns[kvp.Key] = kvp.Value - Time.deltaTime;
                    if (cooldowns[kvp.Key] <= 0)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }
            }
            
            foreach (var key in expiredKeys)
            {
                cooldowns.Remove(key);
            }
        }
        
        // === Relationship-Based Interactions ===
        
        /// <summary>
        /// Рассчитать изменение отношений на основе взаимодействия.
        /// </summary>
        public int CalculateRelationshipChange(InteractionType type, NPCController npc)
        {
            if (npc == null) return 0;
            
            int baseChange = GetBaseRelationshipChange(type);
            
            // Модификатор на основе характера NPC (Attitude + PersonalityTrait)
            // FIX: Disposition→Attitude+PersonalityTrait (2026-04-11)
            float dispositionMod = GetPersonalityModifier(npc.State.Personality, type);
            float attitudeMod = GetAttitudeModifier(npc.State.Attitude, type);
            
            return Mathf.RoundToInt(baseChange * dispositionMod * attitudeMod);
        }
        
        private int GetBaseRelationshipChange(InteractionType type)
        {
            switch (type)
            {
                case InteractionType.Talk:
                    return 1;
                case InteractionType.Flatter:
                    return 5;
                case InteractionType.Gift:
                    return 10;
                case InteractionType.Threaten:
                    return -15;
                case InteractionType.Insult:
                    return -20;
                case InteractionType.Attack:
                    return -50;
                case InteractionType.Heal:
                    return 15;
                case InteractionType.Rescue:
                    return 30;
                case InteractionType.Teach:
                    return 20;
                case InteractionType.Learn:
                    return 5;
                default:
                    return 0;
            }
        }
        
        /// <summary>
        /// Модификатор на основе отношения NPC к игроку (Attitude).
        /// FIX: Замена GetDispositionModifier для Attitude (2026-04-11)
        /// </summary>
        private float GetAttitudeModifier(Attitude attitude, InteractionType type)
        {
            return attitude switch
            {
                Attitude.Hatred => type == InteractionType.Attack ? 1.5f : 0.8f,
                Attitude.Hostile => type == InteractionType.Threaten ? 1.3f : 0.9f,
                Attitude.Unfriendly => 0.9f,
                Attitude.Friendly => type == InteractionType.Talk || type == InteractionType.Flatter ? 1.5f : 1f,
                Attitude.Allied => 1.3f,
                Attitude.SwornAlly => 1.5f,
                _ => 1f // Neutral
            };
        }
        
        /// <summary>
        /// Модификатор на основе характера NPC (PersonalityTrait [Flags]).
        /// FIX: Замена GetDispositionModifier для PersonalityTrait (2026-04-11)
        /// </summary>
        private float GetPersonalityModifier(PersonalityTrait personality, InteractionType type)
        {
            float mod = 1f;
            
            if ((personality & PersonalityTrait.Aggressive) != 0)
                mod *= type == InteractionType.Threaten ? 0.5f : 1f;
            if ((personality & PersonalityTrait.Cautious) != 0)
                mod *= type == InteractionType.Gift ? 1.3f : 1f;
            if ((personality & PersonalityTrait.Treacherous) != 0)
                mod *= type == InteractionType.Flatter ? 0.5f : 1f;
            if ((personality & PersonalityTrait.Ambitious) != 0)
                mod *= (type == InteractionType.Teach || type == InteractionType.Learn) ? 1.5f : 1f;
            if ((personality & PersonalityTrait.Loyal) != 0)
                mod *= type == InteractionType.Gift ? 1.2f : 1f;
            if ((personality & PersonalityTrait.Pacifist) != 0)
                mod *= type == InteractionType.Attack ? 0.3f : 1f;
            if ((personality & PersonalityTrait.Curious) != 0)
                mod *= (type == InteractionType.Talk || type == InteractionType.Learn) ? 1.3f : 1f;
            if ((personality & PersonalityTrait.Vengeful) != 0)
                mod *= type == InteractionType.Insult ? 2f : 1f;
                
            return mod;
        }
        
        // === Gizmos ===
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
    
    /// <summary>
    /// Интерактивный объект — базовый класс для всех интерактивных объектов.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        [Header("Interactable")]
        [SerializeField] protected string interactableId;
        [SerializeField] protected string interactableName;
        [SerializeField] protected bool isInteractable = true;
        
        public string Id => interactableId;
        public string Name => interactableName;
        
        /// <summary>
        /// Можно ли взаимодействовать.
        /// </summary>
        public virtual bool CanInteract(InteractionController player)
        {
            return isInteractable;
        }
        
        /// <summary>
        /// Получить доступные типы взаимодействия.
        /// </summary>
        public abstract List<InteractionType> GetAvailableInteractions(InteractionController player);
        
        /// <summary>
        /// Выполнить взаимодействие.
        /// </summary>
        public abstract InteractionResult Interact(InteractionController player, InteractionType type);
    }
}
