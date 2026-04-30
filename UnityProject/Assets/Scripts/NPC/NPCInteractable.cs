// ============================================================================
// NPCInteractable.cs — Интерактивный компонент NPC
// Cultivation World Simulator
// Создано: 2026-04-30 07:45:00 UTC
// ============================================================================
//
// Расширяет Interactable для NPC.
// InteractionController сканирует Physics2D на Interactable.
// NPCInteractable добавляется к NPC и позволяет взаимодействовать.
//
// Требует: CircleCollider2D (trigger) на том же GameObject.
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;
using CultivationGame.Generators;
using CultivationGame.Interaction;

namespace CultivationGame.NPC
{
    /// <summary>
    /// Интерактивный компонент NPC — позволяет InteractionController
    /// обнаруживать и взаимодействовать с NPC.
    /// </summary>
    [RequireComponent(typeof(NPCController))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class NPCInteractable : Interactable
    {
        [Header("NPC Reference")]
        [SerializeField] private NPCController npcController;

        [Header("Interaction Settings")]
        [SerializeField] private float interactionRadius = 1.5f;
        [SerializeField] private bool isInDialogue;

        // === Unity Lifecycle ===

        private void Awake()
        {
            if (npcController == null)
                npcController = GetComponent<NPCController>();

            // Настраиваем интерактивный коллайдер
            var colliders = GetComponents<CircleCollider2D>();
            bool hasTriggerCollider = false;

            foreach (var col in colliders)
            {
                if (col.isTrigger)
                {
                    hasTriggerCollider = true;
                    col.radius = interactionRadius;
                    break;
                }
            }

            // Если нет trigger-коллайдера, добавляем
            if (!hasTriggerCollider)
            {
                var triggerCol = gameObject.AddComponent<CircleCollider2D>();
                triggerCol.isTrigger = true;
                triggerCol.radius = interactionRadius;
            }

            // Устанавливаем ID интерактивного объекта
            interactableId = $"npc_interact_{System.Guid.NewGuid().ToString("N")[..8]}";
            interactableName = "NPC";
        }

        private void Start()
        {
            if (npcController != null)
            {
                interactableName = npcController.NpcName;
            }
        }

        // === Interactable Implementation ===

        /// <summary>
        /// Можно ли взаимодействовать с NPC.
        /// </summary>
        public override bool CanInteract(InteractionController player)
        {
            if (!isInteractable) return false;
            if (npcController == null) return false;
            if (!npcController.IsAlive) return false;
            if (isInDialogue) return false;

            return true;
        }

        /// <summary>
        /// Получить доступные типы взаимодействия.
        /// </summary>
        public override List<InteractionType> GetAvailableInteractions(InteractionController player)
        {
            var interactions = new List<InteractionType>();

            if (npcController == null || !npcController.IsAlive)
                return interactions;

            // Разговор — всегда доступен
            interactions.Add(InteractionType.Talk);

            // Торговец — торговля
            if (npcController.State != null)
            {
                var role = GetNPCRole();
                var attitude = npcController.Attitude;

                // Торговля — для торговцев
                if (role == NPCRole.Merchant)
                    interactions.Add(InteractionType.Trade);

                // Атака — если отношение хуже Neutral
                if ((int)attitude < (int)Attitude.Neutral)
                    interactions.Add(InteractionType.Attack);

                // Подарок/Лесть — если отношение Friendly+
                if ((int)attitude >= (int)Attitude.Friendly)
                {
                    interactions.Add(InteractionType.Gift);
                    interactions.Add(InteractionType.Flatter);
                }

                // Обучение — для культиваторов и старейшин
                if (role == NPCRole.Cultivator || role == NPCRole.Elder)
                {
                    interactions.Add(InteractionType.Learn);
                    interactions.Add(InteractionType.Cultivate);
                }

                // Преподавание — для старейшин
                if (role == NPCRole.Elder)
                    interactions.Add(InteractionType.Teach);

                // Спарринг — для охранников и культиваторов
                if (role == NPCRole.Guard || role == NPCRole.Cultivator || role == NPCRole.Disciple)
                    interactions.Add(InteractionType.Spar);

                // Угроза и оскорбление — всегда доступны (но с последствиями)
                interactions.Add(InteractionType.Threaten);
                interactions.Add(InteractionType.Insult);

                // Вопрос — для всех
                interactions.Add(InteractionType.Ask);
            }

            return interactions;
        }

        /// <summary>
        /// Выполнить взаимодействие.
        /// </summary>
        public override InteractionResult Interact(InteractionController player, InteractionType type)
        {
            var result = new InteractionResult
            {
                Type = type,
                Success = false,
                Message = ""
            };

            if (npcController == null || !npcController.IsAlive)
            {
                result.Message = "NPC недоступен";
                return result;
            }

            switch (type)
            {
                case InteractionType.Talk:
                    result = HandleTalk(player);
                    break;

                case InteractionType.Trade:
                    result = HandleTrade(player);
                    break;

                case InteractionType.Attack:
                    result = HandleAttack(player);
                    break;

                case InteractionType.Gift:
                    result = HandleGift(player);
                    break;

                case InteractionType.Flatter:
                    result = HandleFlatter(player);
                    break;

                case InteractionType.Learn:
                    result = HandleLearn(player);
                    break;

                case InteractionType.Teach:
                    result = HandleTeach(player);
                    break;

                case InteractionType.Cultivate:
                    result = HandleCultivate(player);
                    break;

                case InteractionType.Spar:
                    result = HandleSpar(player);
                    break;

                case InteractionType.Threaten:
                    result = HandleThreaten(player);
                    break;

                case InteractionType.Insult:
                    result = HandleInsult(player);
                    break;

                case InteractionType.Ask:
                    result = HandleAsk(player);
                    break;

                default:
                    result.Message = $"Взаимодействие {type} не реализовано";
                    break;
            }

            return result;
        }

        // === Обработчики взаимодействий ===

        private InteractionResult HandleTalk(InteractionController player)
        {
            isInDialogue = true;

            var result = new InteractionResult
            {
                Type = InteractionType.Talk,
                Success = true,
                Message = $"{npcController.NpcName} внимательно слушает вас.",
                RelationshipChange = 1
            };

            // Изменяем отношения
            npcController.ModifyRelationship("player", result.RelationshipChange);

            // Заглушка: через 5 секунд «диалог» заканчивается
            // Реальная интеграция с DialogueSystem будет позже
            isInDialogue = false;

            return result;
        }

        private InteractionResult HandleTrade(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Trade,
                Success = true,
                Message = $"{npcController.NpcName} открывает торговое меню.",
                RelationshipChange = 0
            };

            // Заглушка: торговля не реализована
            Debug.Log($"[NPCInteractable] Trade с {npcController.NpcName} — заглушка");

            return result;
        }

        private InteractionResult HandleAttack(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Attack,
                Success = true,
                Message = $"Вы атакуете {npcController.NpcName}!",
                RelationshipChange = -50
            };

            // Получаем ICombatant для NPC и Player
            ICombatant npcCombatant = npcController as ICombatant;
            var playerObj = player?.GetComponent<CultivationGame.Player.PlayerController>();
            ICombatant playerCombatant = playerObj as ICombatant;
            
            if (npcCombatant != null && playerCombatant != null)
            {
                CombatManager cm = CombatManager.Instance;
                if (cm != null)
                {
                    // Инициируем бой и наносим базовую атаку через полный пайплайн
                    cm.InitiateCombat(playerCombatant, npcCombatant);
                    AttackResult attackResult = cm.ExecuteBasicAttack(playerCombatant, npcCombatant);
                    
                    if (attackResult.Success)
                    {
                        result.Message = $"Вы атакуете {npcController.NpcName}! Урон: {attackResult.Damage.FinalDamage:F1}";
                    }
                }
                else
                {
                    // Fallback: CombatManager не найден — используем старый метод
#pragma warning disable CS0618
                    npcController.TakeDamage(10, "player");
#pragma warning restore CS0618
                }
            }
            else
            {
                // Fallback: нет ICombatant — старый метод
#pragma warning disable CS0618
                npcController.TakeDamage(10, "player");
#pragma warning restore CS0618
            }

            npcController.ModifyRelationship("player", result.RelationshipChange);

            return result;
        }

        private InteractionResult HandleGift(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Gift,
                Success = true,
                Message = $"{npcController.NpcName} благодарно принимает подарок.",
                RelationshipChange = 10
            };

            npcController.ModifyRelationship("player", result.RelationshipChange);

            return result;
        }

        private InteractionResult HandleFlatter(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Flatter,
                Success = true,
                Message = $"{npcController.NpcName} приятно удивлён вашей лестью.",
                RelationshipChange = 5
            };

            npcController.ModifyRelationship("player", result.RelationshipChange);

            return result;
        }

        private InteractionResult HandleLearn(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Learn,
                Success = true,
                Message = $"{npcController.NpcName} делится знаниями о культивации.",
                RelationshipChange = 5
            };

            npcController.ModifyRelationship("player", result.RelationshipChange);

            return result;
        }

        private InteractionResult HandleTeach(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Teach,
                Success = true,
                Message = $"{npcController.NpcName} предлагает обучить вас технике.",
                RelationshipChange = 20
            };

            npcController.ModifyRelationship("player", result.RelationshipChange);

            return result;
        }

        private InteractionResult HandleCultivate(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Cultivate,
                Success = true,
                Message = $"Вы начинаете совместную культивацию с {npcController.NpcName}.",
                RelationshipChange = 5
            };

            npcController.ModifyRelationship("player", result.RelationshipChange);

            return result;
        }

        private InteractionResult HandleSpar(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Spar,
                Success = true,
                Message = $"{npcController.NpcName} соглашается на спарринг.",
                RelationshipChange = 3
            };

            npcController.ModifyRelationship("player", result.RelationshipChange);

            return result;
        }

        private InteractionResult HandleThreaten(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Threaten,
                Success = true,
                Message = $"{npcController.NpcName} с опаской смотрит на вас.",
                RelationshipChange = -15
            };

            npcController.ModifyRelationship("player", result.RelationshipChange);

            return result;
        }

        private InteractionResult HandleInsult(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Insult,
                Success = true,
                Message = $"{npcController.NpcName} оскорблён!",
                RelationshipChange = -20
            };

            npcController.ModifyRelationship("player", result.RelationshipChange);

            return result;
        }

        private InteractionResult HandleAsk(InteractionController player)
        {
            var result = new InteractionResult
            {
                Type = InteractionType.Ask,
                Success = true,
                Message = $"{npcController.NpcName}: «Могу ли я чем-то помочь?»",
                RelationshipChange = 1
            };

            npcController.ModifyRelationship("player", result.RelationshipChange);

            return result;
        }

        // === Утилиты ===

        /// <summary>
        /// Получить роль NPC из NPCController.
        /// </summary>
        private NPCRole GetNPCRole()
        {
            // NPCController не хранит роль напрямую, но можно определить по Attitude и Personality
            // Для упрощения — используем Attitude как индикатор
            // TODO: Добавить NPCRole в NPCState для точного определения
            if (npcController == null) return NPCRole.Passerby;

            var attitude = npcController.Attitude;
            var personality = npcController.Personality;

            // Эвристика по Attitude + PersonalityTrait
            if (attitude == Attitude.Hostile || attitude == Attitude.Hatred)
            {
                if ((personality & PersonalityTrait.Aggressive) != 0)
                    return NPCRole.Monster;
                return NPCRole.Enemy;
            }

            if ((personality & PersonalityTrait.Loyal) != 0 && (personality & PersonalityTrait.Cautious) != 0)
                return NPCRole.Guard;

            if ((personality & PersonalityTrait.Curious) != 0 && (personality & PersonalityTrait.Ambitious) != 0)
                return NPCRole.Cultivator;

            if ((personality & PersonalityTrait.Loyal) != 0)
                return NPCRole.Elder;

            return NPCRole.Passerby;
        }

        /// <summary>
        /// Установить роль NPC напрямую (используется из NPCSceneSpawner).
        /// </summary>
        public void SetNPCRole(NPCRole role)
        {
            _cachedRole = role;
            interactableName = npcController?.NpcName ?? role.ToString();
        }

        private NPCRole? _cachedRole;

        /// <summary>
        /// Получить кэшированную роль (если установлена через SetNPCRole).
        /// </summary>
        public NPCRole? CachedRole => _cachedRole;
    }
}
