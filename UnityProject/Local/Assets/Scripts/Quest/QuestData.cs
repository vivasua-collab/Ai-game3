// ============================================================================
// QuestData.cs — ScriptableObject данных квеста
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-04-03 09:00:00 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Quest
{
    /// <summary>
    /// Тип квеста.
    /// Источник: quests.json
    /// </summary>
    public enum QuestType
    {
        Main,           // Основной сюжет
        Side,           // Побочный квест
        Daily,          // Ежедневный
        Cultivation,    // Квест культивации
        Faction,        // Квест фракции
        Hidden,         // Скрытый квест
        Chain           // Цепочка квестов
    }
    
    /// <summary>
    /// Состояние квеста.
    /// </summary>
    public enum QuestState
    {
        Locked,         // Недоступен
        Available,      // Доступен для взятия
        Active,         // Выполняется
        Completed,      // Выполнен
        Failed,         // Провален
        Abandoned       // Брошен
    }
    
    /// <summary>
    /// Награда за квест.
    /// </summary>
    [Serializable]
    public class QuestReward
    {
        public int experience;
        public int gold;
        public int spiritStones;
        public List<string> items = new List<string>();
        public string techniqueId;
        public int factionReputation;
        public string factionId;
        
        /// <summary>
        /// Получить текст награды для UI.
        /// </summary>
        public string GetRewardText()
        {
            List<string> parts = new List<string>();
            
            if (experience > 0) parts.Add($"{experience} опыта");
            if (gold > 0) parts.Add($"{gold} золота");
            if (spiritStones > 0) parts.Add($"{spiritStones} камней духа");
            if (items.Count > 0) parts.Add($"{items.Count} предмет(ов)");
            if (!string.IsNullOrEmpty(techniqueId)) parts.Add($"техника");
            
            return string.Join(", ", parts);
        }
    }
    
    /// <summary>
    /// Требования для начала квеста.
    /// </summary>
    [Serializable]
    public class QuestRequirements
    {
        public int minLevel = 1;
        public CultivationLevel minCultivation = CultivationLevel.None;
        public List<string> prerequisiteQuests = new List<string>();
        public List<string> requiredItems = new List<string>();
        public string requiredFaction;
        public int minFactionReputation;
        public int minStrength;
        public int minIntelligence;
        
        /// <summary>
        /// Проверить, выполнены ли требования.
        /// </summary>
        public bool CheckRequirements(
            int playerLevel,
            CultivationLevel cultivation,
            HashSet<string> completedQuests,
            Dictionary<string, int> inventoryItems,
            Dictionary<string, int> factionReputation,
            int strength,
            int intelligence)
        {
            // Уровень
            if (playerLevel < minLevel) return false;
            
            // Культивация
            if (cultivation < minCultivation) return false;
            
            // Пререквизиты
            foreach (var questId in prerequisiteQuests)
            {
                if (!completedQuests.Contains(questId)) return false;
            }
            
            // Предметы
            foreach (var itemId in requiredItems)
            {
                if (!inventoryItems.ContainsKey(itemId) || inventoryItems[itemId] <= 0)
                    return false;
            }
            
            // Фракция
            if (!string.IsNullOrEmpty(requiredFaction))
            {
                if (!factionReputation.TryGetValue(requiredFaction, out int rep))
                    return false;
                if (rep < minFactionReputation) return false;
            }
            
            // Характеристики
            if (strength < minStrength) return false;
            if (intelligence < minIntelligence) return false;
            
            return true;
        }
    }
    
    /// <summary>
    /// ScriptableObject для данных квеста.
    /// </summary>
    [CreateAssetMenu(fileName = "NewQuest", menuName = "Cultivation/Quest/QuestData")]
    public class QuestData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string questId;
        [SerializeField] private string questName;
        [SerializeField][TextArea] private string description;
        [SerializeField][TextArea] private string summary; // Краткое описание для списка
        
        [Header("Type")]
        [SerializeField] private QuestType type = QuestType.Side;
        [SerializeField] private int chapter = 1;          // Глава сюжета
        [SerializeField] private int order = 0;            // Порядок в главе
        
        [Header("Quest Giver")]
        [SerializeField] private string giverNpcId;        // Кто даёт квест
        [SerializeField] private string giverLocationId;   // Где даётся квест
        
        [Header("Objectives")]
        [SerializeField] private List<QuestObjective> objectives = new List<QuestObjective>();
        [SerializeField] private bool sequentialObjectives = true; // Цели последовательно
        
        [Header("Rewards")]
        [SerializeField] private QuestReward rewards = new QuestReward();
        
        [Header("Requirements")]
        [SerializeField] private QuestRequirements requirements = new QuestRequirements();
        
        [Header("Time Limit")]
        [SerializeField] private bool hasTimeLimit = false;
        [SerializeField] private int timeLimitTicks = 0;   // В тиках
        
        [Header("Fail Conditions")]
        [SerializeField] private bool canFail = false;
        [SerializeField] private string failCondition;     // Описание условия провала
        
        [Header("Follow-up")]
        [SerializeField] private string nextQuestId;       // Следующий квест в цепочке
        
        [Header("UI")]
        [SerializeField] private Sprite icon;
        [SerializeField] private Color questColor = Color.white;
        
        // === Properties ===
        
        public string QuestId => questId;
        public string QuestName => questName;
        public string Description => description;
        public string Summary => summary;
        public QuestType Type => type;
        public int Chapter => chapter;
        public int Order => order;
        public string GiverNpcId => giverNpcId;
        public string GiverLocationId => giverLocationId;
        public List<QuestObjective> Objectives => objectives;
        public bool SequentialObjectives => sequentialObjectives;
        public QuestReward Rewards => rewards;
        public QuestRequirements Requirements => requirements;
        public bool HasTimeLimit => hasTimeLimit;
        public int TimeLimitTicks => timeLimitTicks;
        public bool CanFail => canFail;
        public string FailCondition => failCondition;
        public string NextQuestId => nextQuestId;
        public Sprite Icon => icon;
        public Color QuestColor => questColor;
        
        // === Editor Validation ===
        
        private void OnValidate()
        {
            // Автоматический ID если пустой
            if (string.IsNullOrEmpty(questId))
            {
                questId = Guid.NewGuid().ToString().Substring(0, 8);
            }
            
            // Автоматическое имя
            if (string.IsNullOrEmpty(questName))
            {
                questName = name;
            }
        }
        
        // === Factory Methods ===
        
        /// <summary>
        /// Создать данные квеста (для генератора).
        /// </summary>
        public static QuestData CreateQuest(
            string id,
            string name,
            QuestType type,
            string description,
            List<QuestObjective> objectives,
            QuestReward rewards)
        {
            QuestData data = CreateInstance<QuestData>();
            data.questId = id;
            data.questName = name;
            data.type = type;
            data.description = description;
            data.objectives = objectives;
            data.rewards = rewards;
            
            return data;
        }
        
        // === Utility ===
        
        /// <summary>
        /// Получить общее количество целей.
        /// </summary>
        public int TotalObjectives => objectives.Count;
        
        /// <summary>
        /// Проверить, можно ли выполнить квест параллельно.
        /// </summary>
        public bool IsParallel => !sequentialObjectives;
        
        /// <summary>
        /// Получить тип квеста для UI.
        /// </summary>
        public string GetTypeDisplay()
        {
            return type switch
            {
                QuestType.Main => "Основной",
                QuestType.Side => "Побочный",
                QuestType.Daily => "Ежедневный",
                QuestType.Cultivation => "Культивация",
                QuestType.Faction => "Фракция",
                QuestType.Hidden => "Скрытый",
                QuestType.Chain => "Цепочка",
                _ => type.ToString()
            };
        }
    }
}
