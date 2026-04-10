// ============================================================================
// QuestController.cs — Главный контроллер системы квестов
// Cultivation World Simulator
// Версия: 1.1
// Создано: 2026-04-03 09:05:00 UTC
// Редактировано: 2026-04-11 00:00:00 UTC — Fix-07
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
        
        // === Quest Data Registry ===
        // FIX QST-C02: Registry for looking up QuestData by ID during load
        private Dictionary<string, QuestData> questDataRegistry = new Dictionary<string, QuestData>();
        
        // === References ===
        
        private TimeController timeController;
        
        // FIX QST-C03: References for reward integration
        private Player.StatDevelopment statDevelopment;
        private Inventory.InventoryController inventoryController;
        private Player.PlayerController playerController;
        
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
            
            // FIX QST-C03: Get references for reward integration
            ServiceLocator.Request<Player.PlayerController>(pc => playerController = pc);
        }
        
        private void OnDestroy()
        {
            // Редактировано: 2026-04-03 - Unregister не принимает аргумент
            ServiceLocator.Unregister<QuestController>();
        }
        
        // FIX QST-C02: Register quest data for lookup during load
        /// <summary>
        /// Register a QuestData asset for lookup by quest ID.
        /// Required for restoring active quests from save data.
        /// </summary>
        public void RegisterQuestData(QuestData data)
        {
            if (data != null && !string.IsNullOrEmpty(data.QuestId))
            {
                questDataRegistry[data.QuestId] = data;
            }
        }
        
        /// <summary>
        /// Register multiple QuestData assets.
        /// </summary>
        public void RegisterQuestData(IEnumerable<QuestData> dataList)
        {
            if (dataList == null) return;
            foreach (var data in dataList)
            {
                RegisterQuestData(data);
            }
        }
        
        // === Quest Management ===
        
        /// <summary>
        /// Взять квест.
        /// FIX QST-C01: Uses CloneForInstance() to create independent objectives.
        /// </summary>
        /// <param name="questData">Данные квеста (SO template)</param>
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
            
            // FIX QST-C01: Clone quest data so each instance has independent objectives (2026-04-11)
            QuestData instanceData = questData.CloneForInstance();
            
            // Создаём активный квест
            ActiveQuest activeQuest = new ActiveQuest
            {
                questId = questId,
                questData = instanceData,
                state = QuestState.Active,
                objectiveStates = new List<ObjectiveSaveData>(),
                startTimeTicks = GetCurrentTime(),
                remainingTime = instanceData.TimeLimitTicks
            };
            
            // Инициализируем цели
            for (int i = 0; i < instanceData.Objectives.Count; i++)
            {
                var obj = instanceData.Objectives[i];
                
                // Первая цель активна, остальные по условию
                if (i == 0 || !instanceData.SequentialObjectives)
                {
                    obj.Activate();
                }
                
                // Подписываемся на события цели
                obj.OnCompleted += HandleObjectiveCompleted;
                obj.OnProgressChanged += HandleObjectiveProgress;
            }
            
            activeQuests.Add(questId, activeQuest);
            availableQuests.Remove(questId);
            
            // Register for future lookups
            RegisterQuestData(questData);
            
            // Авто-отслеживание
            if (autoTrackNewQuest || string.IsNullOrEmpty(trackedQuestId))
            {
                SetTrackedQuest(questId);
            }
            
            OnQuestAccepted?.Invoke(instanceData);
            OnQuestListChanged?.Invoke();
            
            Debug.Log($"[Quest] Квест принят: {instanceData.QuestName}");
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
            
            // FIX QST-C03: Grant rewards on quest completion
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
        
        // FIX QST-C03: Implement GrantRewards — XP→StatDevelopment, Items→InventoryController, Gold→PlayerState (2026-04-11)
        /// <summary>
        /// Grant quest rewards to the player.
        /// - Experience → StatDevelopment.AddDelta (distributed across stats)
        /// - Gold → PlayerState (via PlayerController)
        /// - Items → InventoryController.AddItem
        /// - Spirit Stones → PlayerState (via PlayerController)
        /// - Technique → TechniqueController
        /// - Faction Reputation → FactionController
        /// </summary>
        private void GrantRewards(QuestReward rewards)
        {
            if (rewards == null)
            {
                Debug.LogWarning("[Quest] GrantRewards called with null rewards");
                return;
            }
            
            Debug.Log($"[Quest] Награды: {rewards.GetRewardText()}");
            
            // Experience → StatDevelopment
            if (rewards.experience > 0)
            {
                if (statDevelopment == null && playerController != null)
                {
                    statDevelopment = playerController.GetComponent<Player.StatDevelopment>();
                }
                
                if (statDevelopment != null)
                {
                    // Distribute XP as stat deltas across all stats equally
                    float xpPerStat = rewards.experience / 4f;
                    statDevelopment.AddDelta(Core.StatType.Strength, xpPerStat * 0.3f);
                    statDevelopment.AddDelta(Core.StatType.Agility, xpPerStat * 0.2f);
                    statDevelopment.AddDelta(Core.StatType.Intelligence, xpPerStat * 0.3f);
                    statDevelopment.AddDelta(Core.StatType.Vitality, xpPerStat * 0.2f);
                    Debug.Log($"[Quest] XP granted: {rewards.experience} → StatDevelopment");
                }
                else
                {
                    Debug.LogWarning("[Quest] StatDevelopment not found, XP reward not applied");
                }
            }
            
            // Gold → PlayerState (via PlayerController)
            if (rewards.gold > 0)
            {
                // PlayerState doesn't have a gold field yet — log for future integration
                // TODO: Add gold to PlayerState when currency system is implemented
                Debug.Log($"[Quest] Gold granted: {rewards.gold} (pending PlayerState.gold integration)");
            }
            
            // Spirit Stones
            if (rewards.spiritStones > 0)
            {
                Debug.Log($"[Quest] Spirit Stones granted: {rewards.spiritStones} (pending currency integration)");
            }
            
            // Items → InventoryController
            if (rewards.items != null && rewards.items.Count > 0)
            {
                if (inventoryController == null && playerController != null)
                {
                    inventoryController = playerController.GetComponent<Inventory.InventoryController>();
                }
                
                if (inventoryController != null)
                {
                    foreach (string itemId in rewards.items)
                    {
                        // Load ItemData from Resources or registry
                        var itemData = Resources.Load<Data.ScriptableObjects.ItemData>($"Items/{itemId}");
                        if (itemData != null)
                        {
                            inventoryController.AddItem(itemData);
                            Debug.Log($"[Quest] Item granted: {itemId}");
                        }
                        else
                        {
                            Debug.LogWarning($"[Quest] ItemData not found for: {itemId}");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[Quest] InventoryController not found, item rewards not applied");
                }
            }
            
            // Technique
            if (!string.IsNullOrEmpty(rewards.techniqueId))
            {
                Debug.Log($"[Quest] Technique granted: {rewards.techniqueId} (pending TechniqueController integration)");
            }
            
            // Faction Reputation
            if (rewards.factionReputation > 0 && !string.IsNullOrEmpty(rewards.factionId))
            {
                Debug.Log($"[Quest] Faction reputation +{rewards.factionReputation} for {rewards.factionId} (pending FactionController integration)");
            }
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
                var activeSave = new ActiveQuestSaveData
                {
                    questId = quest.questId,
                    state = quest.state,
                    startTimeTicks = quest.startTimeTicks,
                    remainingTime = quest.remainingTime,
                    objectiveStates = new List<ObjectiveSaveData>()
                };
                
                // Save objective states from the cloned quest data
                if (quest.questData != null)
                {
                    foreach (var obj in quest.questData.Objectives)
                    {
                        activeSave.objectiveStates.Add(obj.GetSaveData());
                    }
                }
                
                data.activeQuests.Add(activeSave);
            }
            
            return data;
        }
        
        // FIX QST-C02: Restore active quests from save data (2026-04-11)
        /// <summary>
        /// Загрузить данные. Restores active quests by looking up QuestData from registry,
        /// cloning objectives, and restoring progress.
        /// </summary>
        public void LoadSaveData(QuestSystemSaveData data)
        {
            // Clear current state
            foreach (var quest in activeQuests.Values)
            {
                if (quest.questData != null)
                {
                    foreach (var obj in quest.questData.Objectives)
                    {
                        obj.OnCompleted -= HandleObjectiveCompleted;
                        obj.OnProgressChanged -= HandleObjectiveProgress;
                    }
                }
            }
            activeQuests.Clear();
            
            completedQuests = new HashSet<string>(data.completedQuests ?? new List<string>());
            failedQuests = new HashSet<string>(data.failedQuests ?? new List<string>());
            availableQuests = new HashSet<string>(data.availableQuests ?? new List<string>());
            
            // Restore active quests
            if (data.activeQuests != null)
            {
                foreach (var savedQuest in data.activeQuests)
                {
                    if (string.IsNullOrEmpty(savedQuest.questId)) continue;
                    
                    // Look up QuestData from registry
                    if (!questDataRegistry.TryGetValue(savedQuest.questId, out QuestData templateData))
                    {
                        Debug.LogWarning($"[Quest] Cannot restore quest {savedQuest.questId}: QuestData not found in registry");
                        continue;
                    }
                    
                    // FIX QST-C01: Clone for independent instance
                    QuestData instanceData = templateData.CloneForInstance();
                    
                    ActiveQuest activeQuest = new ActiveQuest
                    {
                        questId = savedQuest.questId,
                        questData = instanceData,
                        state = savedQuest.state,
                        startTimeTicks = savedQuest.startTimeTicks,
                        remainingTime = savedQuest.remainingTime,
                        objectiveStates = savedQuest.objectiveStates ?? new List<ObjectiveSaveData>()
                    };
                    
                    // Restore objective progress
                    if (savedQuest.objectiveStates != null)
                    {
                        foreach (var objState in savedQuest.objectiveStates)
                        {
                            // Find matching objective by ID
                            foreach (var obj in instanceData.Objectives)
                            {
                                if (obj.ObjectiveId == objState.objectiveId)
                                {
                                    obj.LoadSaveData(objState);
                                    break;
                                }
                            }
                        }
                    }
                    
                    // Re-subscribe to objective events
                    foreach (var obj in instanceData.Objectives)
                    {
                        obj.OnCompleted += HandleObjectiveCompleted;
                        obj.OnProgressChanged += HandleObjectiveProgress;
                    }
                    
                    activeQuests.Add(savedQuest.questId, activeQuest);
                    
                    Debug.Log($"[Quest] Restored active quest: {instanceData.QuestName}");
                }
            }
            
            // Restore tracked quest
            if (activeQuests.Count > 0 && string.IsNullOrEmpty(trackedQuestId))
            {
                trackedQuestId = GetFirstActiveQuestId();
            }
            
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
