// ============================================================================
// QuestController.cs — Главный контроллер системы квестов
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-04-03 09:05:00 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.World;

namespace CultivationGame.Quest
{
    /// <summary>
    /// Активный квест (runtime состояние).
    /// </summary>
    [Serializable]
    public class ActiveQuest
    {
        public string questId;
        public QuestData questData;
        public QuestState state;
        public List<ObjectiveSaveData> objectiveStates;
        public int startTimeTicks;
        public int remainingTime;
        
        public bool IsExpired => questData.HasTimeLimit && remainingTime <= 0;
        public bool IsCompleted => state == QuestState.Completed;
        public bool IsActive => state == QuestState.Active;
        
        /// <summary>
        /// Рассчитать общий прогресс.
        /// </summary>
        public float GetProgress()
        {
            if (questData == null || questData.Objectives.Count == 0) return 0f;
            
            int completed = 0;
            foreach (var obj in questData.Objectives)
            {
                if (obj.IsCompleted) completed++;
            }
            
            return (float)completed / questData.Objectives.Count;
        }
    }
    
    /// <summary>
    /// Главный контроллер системы квестов.
    /// Управляет взятием, выполнением и завершением квестов.
    /// </summary>
    public class QuestController : MonoBehaviour
    {
        // === Configuration ===
        
        [Header("Settings")]
        [SerializeField] private int maxActiveQuests = 10;
        [SerializeField] private bool autoTrackNewQuest = true;
        
        // === Runtime State ===
        
        private Dictionary<string, ActiveQuest> activeQuests = new Dictionary<string, ActiveQuest>();
        private HashSet<string> completedQuests = new HashSet<string>();
        private HashSet<string> availableQuests = new HashSet<string>();
        private HashSet<string> failedQuests = new HashSet<string>();
        
        private string trackedQuestId; // Отслеживаемый квест
        
        // === References ===
        
        private TimeController timeController;
        
        // === Properties ===
        
        public int ActiveQuestsCount => activeQuests.Count;
        public int MaxActiveQuests => maxActiveQuests;
        public string TrackedQuestId => trackedQuestId;
        public IReadOnlyDictionary<string, ActiveQuest> ActiveQuestsDict => activeQuests;
        public HashSet<string> CompletedQuests => completedQuests;
        public HashSet<string> AvailableQuests => availableQuests;
        
        // === Events ===
        
        public event Action<QuestData> OnQuestAccepted;
        public event Action<QuestData> OnQuestCompleted;
        public event Action<QuestData> OnQuestFailed;
        public event Action<QuestData> OnQuestAbandoned;
        public event Action<QuestObjective> OnObjectiveCompleted;
        public event Action<QuestObjective> OnObjectiveProgress;
        public event Action<string> OnTrackedQuestChanged;
        public event Action OnQuestListChanged;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            // Регистрируем в ServiceLocator
            ServiceLocator.Register<QuestController>(this);
        }
        
        private void Start()
        {
            // Получаем TimeController
            ServiceLocator.Request<TimeController>(tc => timeController = tc);
        }
        
        private void OnDestroy()
        {
            // Редактировано: 2026-04-03 - Unregister не принимает аргумент
            ServiceLocator.Unregister<QuestController>();
        }
        
        // === Quest Management ===
        
        /// <summary>
        /// Взять квест.
        /// </summary>
        /// <param name="questData">Данные квеста</param>
        /// <returns>True если успешно</returns>
        public bool AcceptQuest(QuestData questData)
        {
            if (questData == null)
            {
                Debug.LogError("[Quest] Попытка взять null квест");
                return false;
            }
            
            string questId = questData.QuestId;
            
            // Проверки
            if (activeQuests.ContainsKey(questId))
            {
                Debug.LogWarning($"[Quest] Квест {questData.QuestName} уже взят");
                return false;
            }
            
            if (completedQuests.Contains(questId))
            {
                Debug.LogWarning($"[Quest] Квест {questData.QuestName} уже выполнен");
                return false;
            }
            
            if (failedQuests.Contains(questId))
            {
                Debug.LogWarning($"[Quest] Квест {questData.QuestName} был провален");
                return false;
            }
            
            if (activeQuests.Count >= maxActiveQuests)
            {
                Debug.LogWarning($"[Quest] Достигнут лимит активных квестов ({maxActiveQuests})");
                return false;
            }
            
            // Создаём активный квест
            ActiveQuest activeQuest = new ActiveQuest
            {
                questId = questId,
                questData = questData,
                state = QuestState.Active,
                objectiveStates = new List<ObjectiveSaveData>(),
                startTimeTicks = GetCurrentTime(),
                remainingTime = questData.TimeLimitTicks
            };
            
            // Инициализируем цели
            for (int i = 0; i < questData.Objectives.Count; i++)
            {
                var obj = questData.Objectives[i];
                
                // Первая цель активна, остальные по условию
                if (i == 0 || !questData.SequentialObjectives)
                {
                    obj.Activate();
                }
                
                // Подписываемся на события цели
                obj.OnCompleted += HandleObjectiveCompleted;
                obj.OnProgressChanged += HandleObjectiveProgress;
            }
            
            activeQuests.Add(questId, activeQuest);
            availableQuests.Remove(questId);
            
            // Авто-отслеживание
            if (autoTrackNewQuest || string.IsNullOrEmpty(trackedQuestId))
            {
                SetTrackedQuest(questId);
            }
            
            OnQuestAccepted?.Invoke(questData);
            OnQuestListChanged?.Invoke();
            
            Debug.Log($"[Quest] Квест принят: {questData.QuestName}");
            return true;
        }
        
        /// <summary>
        /// Бросить квест.
        /// </summary>
        public bool AbandonQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out ActiveQuest activeQuest))
            {
                return false;
            }
            
            // Отписываемся от событий целей
            foreach (var obj in activeQuest.questData.Objectives)
            {
                obj.OnCompleted -= HandleObjectiveCompleted;
                obj.OnProgressChanged -= HandleObjectiveProgress;
                obj.ResetProgress();
            }
            
            QuestData questData = activeQuest.questData;
            
            activeQuests.Remove(questId);
            
            // Если был отслеживаемым — сбрасываем
            if (trackedQuestId == questId)
            {
                trackedQuestId = activeQuests.Count > 0 
                    ? GetFirstActiveQuestId() 
                    : null;
                OnTrackedQuestChanged?.Invoke(trackedQuestId);
            }
            
            OnQuestAbandoned?.Invoke(questData);
            OnQuestListChanged?.Invoke();
            
            Debug.Log($"[Quest] Квест брошен: {questData.QuestName}");
            return true;
        }
        
        /// <summary>
        /// Завершить квест.
        /// </summary>
        private void CompleteQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out ActiveQuest activeQuest))
            {
                return;
            }
            
            QuestData questData = activeQuest.questData;
            
            // Отписываемся от событий
            foreach (var obj in questData.Objectives)
            {
                obj.OnCompleted -= HandleObjectiveCompleted;
                obj.OnProgressChanged -= HandleObjectiveProgress;
            }
            
            activeQuest.state = QuestState.Completed;
            activeQuests.Remove(questId);
            completedQuests.Add(questId);
            
            // Разблокируем следующий квест
            if (!string.IsNullOrEmpty(questData.NextQuestId))
            {
                availableQuests.Add(questData.NextQuestId);
            }
            
            // Если был отслеживаемым
            if (trackedQuestId == questId)
            {
                trackedQuestId = activeQuests.Count > 0 
                    ? GetFirstActiveQuestId() 
                    : null;
                OnTrackedQuestChanged?.Invoke(trackedQuestId);
            }
            
            OnQuestCompleted?.Invoke(questData);
            OnQuestListChanged?.Invoke();
            
            Debug.Log($"[Quest] Квест выполнен: {questData.QuestName}");
            
            // TODO: Выдать награды
            GrantRewards(questData.Rewards);
        }
        
        /// <summary>
        /// Провалить квест.
        /// </summary>
        private void FailQuest(string questId)
        {
            if (!activeQuests.TryGetValue(questId, out ActiveQuest activeQuest))
            {
                return;
            }
            
            if (!activeQuest.questData.CanFail)
            {
                Debug.LogWarning($"[Quest] Квест {activeQuest.questData.QuestName} не может быть провален");
                return;
            }
            
            QuestData questData = activeQuest.questData;
            
            // Отписываемся от событий
            foreach (var obj in questData.Objectives)
            {
                obj.OnCompleted -= HandleObjectiveCompleted;
                obj.OnProgressChanged -= HandleObjectiveProgress;
                obj.Fail();
            }
            
            activeQuest.state = QuestState.Failed;
            activeQuests.Remove(questId);
            failedQuests.Add(questId);
            
            if (trackedQuestId == questId)
            {
                trackedQuestId = activeQuests.Count > 0 
                    ? GetFirstActiveQuestId() 
                    : null;
                OnTrackedQuestChanged?.Invoke(trackedQuestId);
            }
            
            OnQuestFailed?.Invoke(questData);
            OnQuestListChanged?.Invoke();
            
            Debug.Log($"[Quest] Квест провален: {questData.QuestName}");
        }
        
        // === Objective Handling ===
        
        private void HandleObjectiveCompleted(QuestObjective objective)
        {
            // Находим квест с этой целью
            foreach (var kvp in activeQuests)
            {
                var quest = kvp.Value;
                int objIndex = quest.questData.Objectives.IndexOf(objective);
                
                if (objIndex >= 0)
                {
                    OnObjectiveCompleted?.Invoke(objective);
                    
                    // Активируем следующую цель (если последовательные)
                    if (quest.questData.SequentialObjectives && objIndex + 1 < quest.questData.Objectives.Count)
                    {
                        quest.questData.Objectives[objIndex + 1].Activate();
                    }
                    
                    // Проверяем, все ли цели выполнены
                    CheckQuestCompletion(quest);
                    
                    break;
                }
            }
        }
        
        private void HandleObjectiveProgress(QuestObjective objective)
        {
            OnObjectiveProgress?.Invoke(objective);
        }
        
        private void CheckQuestCompletion(ActiveQuest quest)
        {
            if (quest.questData == null) return;
            
            bool allCompleted = true;
            
            foreach (var obj in quest.questData.Objectives)
            {
                if (!obj.IsCompleted)
                {
                    allCompleted = false;
                    break;
                }
            }
            
            if (allCompleted)
            {
                CompleteQuest(quest.questId);
            }
        }
        
        // === Progress Updates (Called by Game Events) ===
        
        /// <summary>
        /// Сообщить об убийстве врага.
        /// </summary>
        public void NotifyEnemyKilled(string enemyId)
        {
            foreach (var quest in activeQuests.Values)
            {
                foreach (var obj in quest.questData.Objectives)
                {
                    if (obj.Type == QuestObjectiveType.Kill && 
                        obj.TargetId == enemyId && 
                        obj.IsActive)
                    {
                        obj.AddProgress(1);
                    }
                }
            }
        }
        
        /// <summary>
        /// Сообщить о сборе предмета.
        /// </summary>
        public void NotifyItemCollected(string itemId, int amount = 1)
        {
            foreach (var quest in activeQuests.Values)
            {
                foreach (var obj in quest.questData.Objectives)
                {
                    if (obj.Type == QuestObjectiveType.Collect && 
                        obj.TargetId == itemId && 
                        obj.IsActive)
                    {
                        obj.AddProgress(amount);
                    }
                }
            }
        }
        
        /// <summary>
        /// Сообщить о достижении уровня культивации.
        /// </summary>
        public void NotifyCultivationReached(CultivationLevel level)
        {
            foreach (var quest in activeQuests.Values)
            {
                foreach (var obj in quest.questData.Objectives)
                {
                    if (obj.Type == QuestObjectiveType.Cultivation && obj.IsActive)
                    {
                        if ((int)level >= obj.RequiredAmount)
                        {
                            obj.Complete();
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Сообщить о посещении локации.
        /// </summary>
        public void NotifyLocationVisited(string locationId)
        {
            foreach (var quest in activeQuests.Values)
            {
                foreach (var obj in quest.questData.Objectives)
                {
                    if (obj.Type == QuestObjectiveType.Explore && 
                        obj.TargetId == locationId && 
                        obj.IsActive)
                    {
                        obj.Complete();
                    }
                }
            }
        }
        
        /// <summary>
        /// Сообщить о разговоре с NPC.
        /// </summary>
        public void NotifyNpcTalk(string npcId)
        {
            foreach (var quest in activeQuests.Values)
            {
                foreach (var obj in quest.questData.Objectives)
                {
                    if (obj.Type == QuestObjectiveType.Talk && 
                        obj.TargetId == npcId && 
                        obj.IsActive)
                    {
                        obj.Complete();
                    }
                }
            }
        }
        
        // === Tracking ===
        
        /// <summary>
        /// Установить отслеживаемый квест.
        /// </summary>
        public void SetTrackedQuest(string questId)
        {
            if (questId == trackedQuestId) return;
            
            if (string.IsNullOrEmpty(questId) || activeQuests.ContainsKey(questId))
            {
                trackedQuestId = questId;
                OnTrackedQuestChanged?.Invoke(questId);
            }
        }
        
        /// <summary>
        /// Получить отслеживаемый квест.
        /// </summary>
        public ActiveQuest GetTrackedQuest()
        {
            if (string.IsNullOrEmpty(trackedQuestId)) return null;
            
            activeQuests.TryGetValue(trackedQuestId, out ActiveQuest quest);
            return quest;
        }
        
        // === Query ===
        
        /// <summary>
        /// Проверить, выполнен ли квест.
        /// </summary>
        public bool IsQuestCompleted(string questId)
        {
            return completedQuests.Contains(questId);
        }
        
        /// <summary>
        /// Проверить, активен ли квест.
        /// </summary>
        public bool IsQuestActive(string questId)
        {
            return activeQuests.ContainsKey(questId);
        }
        
        /// <summary>
        /// Получить активный квест по ID.
        /// </summary>
        public ActiveQuest GetActiveQuest(string questId)
        {
            activeQuests.TryGetValue(questId, out ActiveQuest quest);
            return quest;
        }
        
        /// <summary>
        /// Получить список активных квестов.
        /// </summary>
        public List<ActiveQuest> GetActiveQuests()
        {
            return new List<ActiveQuest>(activeQuests.Values);
        }
        
        // === Time Updates ===
        
        /// <summary>
        /// Обновить таймеры квестов (вызывать каждый тик).
        /// </summary>
        public void OnTick(int currentTick)
        {
            List<string> expiredQuests = new List<string>();
            
            foreach (var quest in activeQuests.Values)
            {
                if (quest.questData.HasTimeLimit)
                {
                    quest.remainingTime--;
                    
                    if (quest.remainingTime <= 0)
                    {
                        expiredQuests.Add(quest.questId);
                    }
                }
            }
            
            // Проваливаем истёкшие квесты
            foreach (var questId in expiredQuests)
            {
                FailQuest(questId);
            }
        }
        
        // === Rewards ===
        
        private void GrantRewards(QuestReward rewards)
        {
            // TODO: Интеграция с системой опыта, золота, инвентаря
            
            Debug.Log($"[Quest] Награды: {rewards.GetRewardText()}");
        }
        
        // === Utility ===
        
        private int GetCurrentTime()
        {
            return timeController != null ? (int)timeController.TotalGameSeconds : 0;
        }
        
        private string GetFirstActiveQuestId()
        {
            foreach (var id in activeQuests.Keys)
            {
                return id;
            }
            return null;
        }
        
        /// <summary>
        /// Получить информацию о квестах.
        /// </summary>
        public string GetQuestsInfo()
        {
            return $"[Quest] Активных: {activeQuests.Count}/{maxActiveQuests} | " +
                   $"Выполнено: {completedQuests.Count} | " +
                   $"Провалено: {failedQuests.Count}";
        }
        
        // === Serialization ===
        
        /// <summary>
        /// Получить данные для сохранения.
        /// </summary>
        public QuestSystemSaveData GetSaveData()
        {
            QuestSystemSaveData data = new QuestSystemSaveData
            {
                completedQuests = new List<string>(completedQuests),
                failedQuests = new List<string>(failedQuests),
                availableQuests = new List<string>(availableQuests),
                activeQuests = new List<ActiveQuestSaveData>()
            };
            
            foreach (var quest in activeQuests.Values)
            {
                data.activeQuests.Add(new ActiveQuestSaveData
                {
                    questId = quest.questId,
                    state = quest.state,
                    startTimeTicks = quest.startTimeTicks,
                    remainingTime = quest.remainingTime,
                    objectiveStates = new List<ObjectiveSaveData>()
                });
            }
            
            return data;
        }
        
        /// <summary>
        /// Загрузить данные.
        /// </summary>
        public void LoadSaveData(QuestSystemSaveData data)
        {
            completedQuests = new HashSet<string>(data.completedQuests);
            failedQuests = new HashSet<string>(data.failedQuests);
            availableQuests = new HashSet<string>(data.availableQuests);
            
            // TODO: Восстановить активные квесты
            
            OnQuestListChanged?.Invoke();
        }
    }
    
    /// <summary>
    /// Данные сохранения системы квестов.
    /// </summary>
    [Serializable]
    public struct QuestSystemSaveData
    {
        public List<string> completedQuests;
        public List<string> failedQuests;
        public List<string> availableQuests;
        public List<ActiveQuestSaveData> activeQuests;
    }
    
    /// <summary>
    /// Данные сохранения активного квеста.
    /// </summary>
    [Serializable]
    public struct ActiveQuestSaveData
    {
        public string questId;
        public QuestState state;
        public int startTimeTicks;
        public int remainingTime;
        public List<ObjectiveSaveData> objectiveStates;
    }
}
