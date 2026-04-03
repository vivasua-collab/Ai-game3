// ============================================================================
// QuestObjective.cs — Цели квестов
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-04-03 08:55:00 UTC
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Quest
{
    /// <summary>
    /// Тип цели квеста.
    /// Источник: quests.json
    /// </summary>
    public enum QuestObjectiveType
    {
        Kill,           // Убить N врагов определённого типа
        Collect,        // Собрать N предметов
        Deliver,        // Доставить предмет NPC
        Escort,         // Сопроводить NPC
        Explore,        // Исследовать локацию
        Defeat,         // Победить босса
        Cultivation,    // Достичь уровня культивации
        Talk,           // Поговорить с NPC
        Use,            // Использовать предмет
        Defend,         // Защитить объект/точку
        Survive,        // Выжить N времени
        Reach,          // Достичь точки
        Learn,          // Изучить технику
        Craft,          // Создать предмет
        Meditate        // Медитировать N времени
    }
    
    /// <summary>
    /// Состояние цели квеста.
    /// </summary>
    public enum ObjectiveState
    {
        Locked,         // Заблокирована (требует выполнения предыдущих)
        Active,         // Активна
        InProgress,     // В процессе выполнения
        Completed,      // Выполнена
        Failed          // Провалена
    }
    
    /// <summary>
    /// Базовый класс цели квеста.
    /// </summary>
    [Serializable]
    public class QuestObjective
    {
        [Header("Identity")]
        [SerializeField] private string objectiveId;
        [SerializeField] private string objectiveName;
        [SerializeField][TextArea] private string description;
        
        [Header("Type")]
        [SerializeField] private QuestObjectiveType type = QuestObjectiveType.Kill;
        
        [Header("Target")]
        [SerializeField] private string targetId;           // ID цели (врага, предмета, NPC)
        [SerializeField] private string targetName;         // Имя цели для отображения
        [SerializeField] private int requiredAmount = 1;    // Требуемое количество
        [SerializeField] private int currentAmount;         // Текущий прогресс
        
        [Header("Location")]
        [SerializeField] private string locationId;         // Где выполнять
        [SerializeField] private Vector3 targetPosition;    // Точка назначения
        
        [Header("Conditions")]
        [SerializeField] private int prerequisiteObjective = -1; // Индекс предыдущей цели (-1 = нет)
        [SerializeField] private int minLevel = 1;          // Минимальный уровень
        [SerializeField] private CultivationLevel minCultivation = CultivationLevel.None;
        
        [Header("Rewards (Bonus)")]
        [SerializeField] private int bonusExp;
        [SerializeField] private int bonusGold;
        [SerializeField] private string bonusItemId;
        
        [Header("State")]
        [SerializeField] private ObjectiveState state = ObjectiveState.Locked;
        
        // === Properties ===
        
        public string ObjectiveId => objectiveId;
        public string ObjectiveName => objectiveName;
        public string Description => description;
        public QuestObjectiveType Type => type;
        public string TargetId => targetId;
        public string TargetName => targetName;
        public int RequiredAmount => requiredAmount;
        public int CurrentAmount => currentAmount;
        public string LocationId => locationId;
        public Vector3 TargetPosition => targetPosition;
        public int PrerequisiteObjective => prerequisiteObjective;
        public int MinLevel => minLevel;
        public CultivationLevel MinCultivation => minCultivation;
        public int BonusExp => bonusExp;
        public int BonusGold => bonusGold;
        public string BonusItemId => bonusItemId;
        public ObjectiveState State => state;
        
        public bool IsCompleted => state == ObjectiveState.Completed;
        public bool IsActive => state == ObjectiveState.Active || state == ObjectiveState.InProgress;
        public bool IsLocked => state == ObjectiveState.Locked;
        public bool IsFailed => state == ObjectiveState.Failed;
        public float Progress => requiredAmount > 0 ? (float)currentAmount / requiredAmount : 0f;
        
        // === Events ===
        
        public event Action<QuestObjective> OnProgressChanged;
        public event Action<QuestObjective> OnCompleted;
        public event Action<QuestObjective> OnFailed;
        
        // === Constructor ===
        
        public QuestObjective()
        {
            objectiveId = Guid.NewGuid().ToString().Substring(0, 8);
        }
        
        public QuestObjective(QuestObjectiveType type, string targetId, int requiredAmount, string name = "")
        {
            objectiveId = Guid.NewGuid().ToString().Substring(0, 8);
            this.type = type;
            this.targetId = targetId;
            this.requiredAmount = requiredAmount;
            this.objectiveName = string.IsNullOrEmpty(name) ? $"{type} {targetId}" : name;
        }
        
        // === State Management ===
        
        /// <summary>
        /// Активировать цель.
        /// </summary>
        public void Activate()
        {
            if (state == ObjectiveState.Locked)
            {
                state = ObjectiveState.Active;
            }
        }
        
        /// <summary>
        /// Заблокировать цель.
        /// </summary>
        public void Lock()
        {
            state = ObjectiveState.Locked;
        }
        
        /// <summary>
        /// Выполнить цель (принудительно).
        /// </summary>
        public void Complete()
        {
            if (state == ObjectiveState.Locked) return;
            
            currentAmount = requiredAmount;
            state = ObjectiveState.Completed;
            OnCompleted?.Invoke(this);
        }
        
        /// <summary>
        /// Провалить цель.
        /// </summary>
        public void Fail()
        {
            state = ObjectiveState.Failed;
            OnFailed?.Invoke(this);
        }
        
        // === Progress ===
        
        /// <summary>
        /// Добавить прогресс.
        /// </summary>
        /// <param name="amount">Количество</param>
        /// <returns>True если цель выполнена</returns>
        public bool AddProgress(int amount = 1)
        {
            if (!IsActive || IsCompleted) return false;
            
            currentAmount = Mathf.Min(currentAmount + amount, requiredAmount);
            state = ObjectiveState.InProgress;
            
            OnProgressChanged?.Invoke(this);
            
            if (currentAmount >= requiredAmount)
            {
                state = ObjectiveState.Completed;
                OnCompleted?.Invoke(this);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Установить прогресс.
        /// </summary>
        public void SetProgress(int amount)
        {
            currentAmount = Mathf.Clamp(amount, 0, requiredAmount);
            
            if (currentAmount > 0)
            {
                state = ObjectiveState.InProgress;
            }
            
            OnProgressChanged?.Invoke(this);
            
            if (currentAmount >= requiredAmount)
            {
                state = ObjectiveState.Completed;
                OnCompleted?.Invoke(this);
            }
        }
        
        /// <summary>
        /// Сбросить прогресс.
        /// </summary>
        public void ResetProgress()
        {
            currentAmount = 0;
            state = ObjectiveState.Locked;
        }
        
        // === Serialization ===
        
        /// <summary>
        /// Получить данные для сохранения.
        /// </summary>
        public ObjectiveSaveData GetSaveData()
        {
            return new ObjectiveSaveData
            {
                objectiveId = objectiveId,
                currentAmount = currentAmount,
                state = state
            };
        }
        
        /// <summary>
        /// Загрузить данные.
        /// </summary>
        public void LoadSaveData(ObjectiveSaveData data)
        {
            if (data.objectiveId != objectiveId) return;
            
            currentAmount = data.currentAmount;
            state = data.state;
        }
        
        // === Utility ===
        
        /// <summary>
        /// Получить текст цели для UI.
        /// </summary>
        public string GetObjectiveText()
        {
            return type switch
            {
                QuestObjectiveType.Kill => $"Убить {targetName}: {currentAmount}/{requiredAmount}",
                QuestObjectiveType.Collect => $"Собрать {targetName}: {currentAmount}/{requiredAmount}",
                QuestObjectiveType.Deliver => $"Доставить {targetName}",
                QuestObjectiveType.Escort => $"Сопроводить {targetName}",
                QuestObjectiveType.Explore => $"Исследовать {targetName}",
                QuestObjectiveType.Defeat => $"Победить {targetName}",
                QuestObjectiveType.Cultivation => $"Достичь уровня культивации {requiredAmount}",
                QuestObjectiveType.Talk => $"Поговорить с {targetName}",
                QuestObjectiveType.Use => $"Использовать {targetName}",
                QuestObjectiveType.Defend => $"Защитить {targetName}",
                QuestObjectiveType.Survive => $"Выжить {requiredAmount} секунд",
                QuestObjectiveType.Reach => $"Добраться до {targetName}",
                QuestObjectiveType.Learn => $"Изучить технику: {targetName}",
                QuestObjectiveType.Craft => $"Создать {targetName}: {currentAmount}/{requiredAmount}",
                QuestObjectiveType.Meditate => $"Медитировать {requiredAmount} тиков",
                _ => $"{type}: {currentAmount}/{requiredAmount}"
            };
        }
        
        /// <summary>
        /// Получить статус для UI.
        /// </summary>
        public string GetStatusText()
        {
            return state switch
            {
                ObjectiveState.Locked => "🔒 Заблокировано",
                ObjectiveState.Active => "⏳ Активно",
                ObjectiveState.InProgress => $"🔄 В процессе ({Progress:P0})",
                ObjectiveState.Completed => "✅ Выполнено",
                ObjectiveState.Failed => "❌ Провалено",
                _ => state.ToString()
            };
        }
    }
    
    /// <summary>
    /// Данные сохранения цели квеста.
    /// </summary>
    [Serializable]
    public struct ObjectiveSaveData
    {
        public string objectiveId;
        public int currentAmount;
        public ObjectiveState state;
    }
}
