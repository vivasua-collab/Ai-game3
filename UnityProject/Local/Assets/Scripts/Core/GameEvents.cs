// ============================================================================
// GameEvents.cs — Централизованная система событий
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-04-01 13:03:39 UTC
// ============================================================================

using System;
using CultivationGame.Core;

namespace CultivationGame.Core
{
    /// <summary>
    /// Централизованная система событий для связи между системами.
    /// 
    /// Использование:
    /// - Подписка: GameEvents.OnPlayerDeath += HandleDeath;
    /// - Вызов: GameEvents.TriggerPlayerDeath();
    /// 
    /// Преимущества:
    /// - Слабая связанность между системами
    /// - Централизованное управление событиями
    /// - Удобная отладка через логирование
    /// </summary>
    public static class GameEvents
    {
        #region Debug
        
        /// <summary>
        /// Включить логирование всех событий в Console.
        /// </summary>
        public static bool DebugLogging { get; set; } = false;
        
        #endregion
        
        #region Game State Events
        
        /// <summary>
        /// Игра запущена.
        /// </summary>
        public static event Action OnGameStart;
        
        /// <summary>
        /// Игра поставлена на паузу.
        /// </summary>
        public static event Action OnGamePause;
        
        /// <summary>
        /// Игра продолжена после паузы.
        /// </summary>
        public static event Action OnGameResume;
        
        /// <summary>
        /// Игра завершается.
        /// </summary>
        public static event Action OnGameQuit;
        
        /// <summary>
        /// Изменение состояния игры.
        /// </summary>
        public static event Action<GameState, GameState> OnGameStateChanged;
        
        /// <summary>
        /// Сцена загружена.
        /// </summary>
        public static event Action<string> OnSceneLoaded;
        
        /// <summary>
        /// Сцена выгружается.
        /// </summary>
        public static event Action<string> OnSceneUnloading;
        
        #endregion
        
        #region Player Events
        
        /// <summary>
        /// Изменение HP игрока. (currentHP, maxHP)
        /// </summary>
        public static event Action<int, int> OnPlayerHealthChanged;
        
        /// <summary>
        /// Изменение Ци игрока. (currentQi, maxQi)
        /// </summary>
        public static event Action<long, long> OnPlayerQiChanged;
        
        /// <summary>
        /// Изменение уровня культивации.
        /// </summary>
        public static event Action<int> OnPlayerCultivationLevelChanged;
        
        /// <summary>
        /// Игрок умер.
        /// </summary>
        public static event Action OnPlayerDeath;
        
        /// <summary>
        /// Игрок воскрес.
        /// </summary>
        public static event Action OnPlayerRevive;
        
        /// <summary>
        /// Изменение локации игрока.
        /// </summary>
        public static event Action<string> OnPlayerLocationChanged;
        
        /// <summary>
        /// Игрок начал медитацию.
        /// </summary>
        public static event Action OnPlayerMeditationStart;
        
        /// <summary>
        /// Игрок закончил медитацию. (gainedQi)
        /// </summary>
        public static event Action<long> OnPlayerMeditationEnd;
        
        /// <summary>
        /// Игрок заснул.
        /// </summary>
        public static event Action<float> OnPlayerSleepStart; // hours
        
        /// <summary>
        /// Игрок проснулся.
        /// </summary>
        public static event Action<float> OnPlayerSleepEnd; // hours slept
        
        /// <summary>
        /// Прорыв уровня культивации.
        /// </summary>
        public static event Action<int, bool> OnPlayerBreakthrough; // (level, success)
        
        #endregion
        
        #region Combat Events
        
        /// <summary>
        /// Бой начался. (enemyId)
        /// </summary>
        public static event Action<string> OnCombatStart;
        
        /// <summary>
        /// Бой закончился. (victory)
        /// </summary>
        public static event Action<bool> OnCombatEnd;
        
        /// <summary>
        /// Игрок нанёс урон. (damage)
        /// </summary>
        public static event Action<int> OnDamageDealt;
        
        /// <summary>
        /// Игрок получил урон. (damage, partType)
        /// </summary>
        public static event Action<int, string> OnDamageTaken;
        
        /// <summary>
        /// Техника использована. (techniqueId, success)
        /// </summary>
        public static event Action<string, bool> OnTechniqueUsed;
        
        /// <summary>
        /// Техника изучена. (techniqueId)
        /// </summary>
        public static event Action<string> OnTechniqueLearned;
        
        /// <summary>
        /// Техника достигла мастерства. (techniqueId)
        /// </summary>
        public static event Action<string> OnTechniqueMastered;
        
        /// <summary>
        /// Враг убит. (enemyId)
        /// </summary>
        public static event Action<string> OnEnemyKilled;
        
        #endregion
        
        #region NPC Events
        
        /// <summary>
        /// Взаимодействие с NPC. (npcId)
        /// </summary>
        public static event Action<string> OnNPCInteract;
        
        /// <summary>
        /// Диалог начался. (npcId, dialogueId)
        /// </summary>
        public static event Action<string, string> OnDialogueStart;
        
        /// <summary>
        /// Диалог закончился. (npcId)
        /// </summary>
        public static event Action<string> OnDialogueEnd;
        
        /// <summary>
        /// Изменение отношений. (npcId, newValue)
        /// </summary>
        public static event Action<string, int> OnRelationChanged;
        
        /// <summary>
        /// NPC присоединился к группе. (npcId)
        /// </summary>
        public static event Action<string> OnNPCJoined;
        
        /// <summary>
        /// NPC покинул группу. (npcId)
        /// </summary>
        public static event Action<string> OnNPCLeft;
        
        #endregion
        
        #region World Events
        
        /// <summary>
        /// Изменение времени суток. (hour)
        /// </summary>
        public static event Action<int> OnTimeHourChanged;
        
        /// <summary>
        /// Новый день. (day)
        /// </summary>
        public static event Action<int> OnDayChanged;
        
        /// <summary>
        /// Изменение месяца. (month)
        /// </summary>
        public static event Action<int> OnMonthChanged;
        
        /// <summary>
        /// Изменение года. (year)
        /// </summary>
        public static event Action<int> OnYearChanged;
        
        /// <summary>
        /// Скорость времени изменилась. (speed)
        /// </summary>
        public static event Action<TimeSpeed> OnTimeSpeedChanged;
        
        /// <summary>
        /// Мировое событие触发ировано. (eventId)
        /// </summary>
        public static event Action<string> OnWorldEventTriggered;
        
        /// <summary>
        /// Мировое событие завершено. (eventId, success)
        /// </summary>
        public static event Action<string, bool> OnWorldEventEnded;
        
        #endregion
        
        #region Inventory Events
        
        /// <summary>
        /// Предмет добавлен в инвентарь. (itemId, count)
        /// </summary>
        public static event Action<string, int> OnItemAdded;
        
        /// <summary>
        /// Предмет удалён из инвентаря. (itemId, count)
        /// </summary>
        public static event Action<string, int> OnItemRemoved;
        
        /// <summary>
        /// Экипировка надета. (itemId, slot)
        /// </summary>
        public static event Action<string, string> OnItemEquipped;
        
        /// <summary>
        /// Экипировка снята. (itemId, slot)
        /// </summary>
        public static event Action<string, string> OnItemUnequipped;
        
        /// <summary>
        /// Предмет создан (крафт). (itemId)
        /// </summary>
        public static event Action<string> OnItemCrafted;
        
        #endregion
        
        #region Quest Events
        
        /// <summary>
        /// Квест начат. (questId)
        /// </summary>
        public static event Action<string> OnQuestStarted;
        
        /// <summary>
        /// Цель квеста обновлена. (questId, objectiveIndex, current, target)
        /// </summary>
        public static event Action<string, int, int, int> OnQuestObjectiveUpdated;
        
        /// <summary>
        /// Квест завершён. (questId)
        /// </summary>
        public static event Action<string> OnQuestCompleted;
        
        /// <summary>
        /// Квест провален. (questId)
        /// </summary>
        public static event Action<string> OnQuestFailed;
        
        #endregion
        
        #region Save Events
        
        /// <summary>
        /// Игра сохраняется.
        /// </summary>
        public static event Action<int> OnGameSaving; // slot
        
        /// <summary>
        /// Игра сохранена.
        /// </summary>
        public static event Action<int> OnGameSaved; // slot
        
        /// <summary>
        /// Игра загружается.
        /// </summary>
        public static event Action<int> OnGameLoading; // slot
        
        /// <summary>
        /// Игра загружена.
        /// </summary>
        public static event Action<int> OnGameLoaded; // slot
        
        #endregion
        
        #region Trigger Methods
        
        // === Game State ===
        
        public static void TriggerGameStart()
        {
            if (DebugLogging) UnityEngine.Debug.Log("[GameEvents] Game Start");
            OnGameStart?.Invoke();
        }
        
        public static void TriggerGamePause()
        {
            if (DebugLogging) UnityEngine.Debug.Log("[GameEvents] Game Pause");
            OnGamePause?.Invoke();
        }
        
        public static void TriggerGameResume()
        {
            if (DebugLogging) UnityEngine.Debug.Log("[GameEvents] Game Resume");
            OnGameResume?.Invoke();
        }
        
        public static void TriggerGameQuit()
        {
            if (DebugLogging) UnityEngine.Debug.Log("[GameEvents] Game Quit");
            OnGameQuit?.Invoke();
        }
        
        public static void TriggerGameStateChanged(GameState oldState, GameState newState)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] State: {oldState} → {newState}");
            OnGameStateChanged?.Invoke(oldState, newState);
        }
        
        public static void TriggerSceneLoaded(string sceneName)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Scene Loaded: {sceneName}");
            OnSceneLoaded?.Invoke(sceneName);
        }
        
        public static void TriggerSceneUnloading(string sceneName)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Scene Unloading: {sceneName}");
            OnSceneUnloading?.Invoke(sceneName);
        }
        
        // === Player ===
        
        public static void TriggerPlayerHealthChanged(int current, int max)
        {
            OnPlayerHealthChanged?.Invoke(current, max);
        }
        
        public static void TriggerPlayerQiChanged(long current, long max)
        {
            OnPlayerQiChanged?.Invoke(current, max);
        }
        
        public static void TriggerPlayerCultivationLevelChanged(int level)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Cultivation Level: {level}");
            OnPlayerCultivationLevelChanged?.Invoke(level);
        }
        
        public static void TriggerPlayerDeath()
        {
            if (DebugLogging) UnityEngine.Debug.Log("[GameEvents] Player Death");
            OnPlayerDeath?.Invoke();
        }
        
        public static void TriggerPlayerRevive()
        {
            if (DebugLogging) UnityEngine.Debug.Log("[GameEvents] Player Revive");
            OnPlayerRevive?.Invoke();
        }
        
        public static void TriggerPlayerLocationChanged(string locationId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Location: {locationId}");
            OnPlayerLocationChanged?.Invoke(locationId);
        }
        
        public static void TriggerPlayerMeditationStart()
        {
            if (DebugLogging) UnityEngine.Debug.Log("[GameEvents] Meditation Start");
            OnPlayerMeditationStart?.Invoke();
        }
        
        public static void TriggerPlayerMeditationEnd(long gainedQi)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Meditation End: +{gainedQi} Qi");
            OnPlayerMeditationEnd?.Invoke(gainedQi);
        }
        
        public static void TriggerPlayerSleepStart(float hours)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Sleep Start: {hours}h");
            OnPlayerSleepStart?.Invoke(hours);
        }
        
        public static void TriggerPlayerSleepEnd(float hours)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Sleep End: {hours}h");
            OnPlayerSleepEnd?.Invoke(hours);
        }
        
        public static void TriggerPlayerBreakthrough(int level, bool success)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Breakthrough L{level}: {(success ? "Success" : "Failed")}");
            OnPlayerBreakthrough?.Invoke(level, success);
        }
        
        // === Combat ===
        
        public static void TriggerCombatStart(string enemyId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Combat Start: {enemyId}");
            OnCombatStart?.Invoke(enemyId);
        }
        
        public static void TriggerCombatEnd(bool victory)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Combat End: {(victory ? "Victory" : "Defeat")}");
            OnCombatEnd?.Invoke(victory);
        }
        
        public static void TriggerDamageDealt(int damage)
        {
            OnDamageDealt?.Invoke(damage);
        }
        
        public static void TriggerDamageTaken(int damage, string partType)
        {
            OnDamageTaken?.Invoke(damage, partType);
        }
        
        public static void TriggerTechniqueUsed(string techniqueId, bool success)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Technique Used: {techniqueId} ({success})");
            OnTechniqueUsed?.Invoke(techniqueId, success);
        }
        
        public static void TriggerTechniqueLearned(string techniqueId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Technique Learned: {techniqueId}");
            OnTechniqueLearned?.Invoke(techniqueId);
        }
        
        public static void TriggerTechniqueMastered(string techniqueId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Technique Mastered: {techniqueId}");
            OnTechniqueMastered?.Invoke(techniqueId);
        }
        
        public static void TriggerEnemyKilled(string enemyId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Enemy Killed: {enemyId}");
            OnEnemyKilled?.Invoke(enemyId);
        }
        
        // === NPC ===
        
        public static void TriggerNPCInteract(string npcId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] NPC Interact: {npcId}");
            OnNPCInteract?.Invoke(npcId);
        }
        
        public static void TriggerDialogueStart(string npcId, string dialogueId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Dialogue Start: {npcId} / {dialogueId}");
            OnDialogueStart?.Invoke(npcId, dialogueId);
        }
        
        public static void TriggerDialogueEnd(string npcId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Dialogue End: {npcId}");
            OnDialogueEnd?.Invoke(npcId);
        }
        
        public static void TriggerRelationChanged(string npcId, int newValue)
        {
            OnRelationChanged?.Invoke(npcId, newValue);
        }
        
        public static void TriggerNPCJoined(string npcId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] NPC Joined: {npcId}");
            OnNPCJoined?.Invoke(npcId);
        }
        
        public static void TriggerNPCLeft(string npcId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] NPC Left: {npcId}");
            OnNPCLeft?.Invoke(npcId);
        }
        
        // === World ===
        
        public static void TriggerTimeHourChanged(int hour)
        {
            OnTimeHourChanged?.Invoke(hour);
        }
        
        public static void TriggerDayChanged(int day)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Day: {day}");
            OnDayChanged?.Invoke(day);
        }
        
        public static void TriggerMonthChanged(int month)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Month: {month}");
            OnMonthChanged?.Invoke(month);
        }
        
        public static void TriggerYearChanged(int year)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Year: {year}");
            OnYearChanged?.Invoke(year);
        }
        
        public static void TriggerTimeSpeedChanged(TimeSpeed speed)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Time Speed: {speed}");
            OnTimeSpeedChanged?.Invoke(speed);
        }
        
        public static void TriggerWorldEventTriggered(string eventId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Event: {eventId}");
            OnWorldEventTriggered?.Invoke(eventId);
        }
        
        public static void TriggerWorldEventEnded(string eventId, bool success)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Event End: {eventId} ({success})");
            OnWorldEventEnded?.Invoke(eventId, success);
        }
        
        // === Inventory ===
        
        public static void TriggerItemAdded(string itemId, int count)
        {
            OnItemAdded?.Invoke(itemId, count);
        }
        
        public static void TriggerItemRemoved(string itemId, int count)
        {
            OnItemRemoved?.Invoke(itemId, count);
        }
        
        public static void TriggerItemEquipped(string itemId, string slot)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Equipped: {itemId} → {slot}");
            OnItemEquipped?.Invoke(itemId, slot);
        }
        
        public static void TriggerItemUnequipped(string itemId, string slot)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Unequipped: {itemId} ← {slot}");
            OnItemUnequipped?.Invoke(itemId, slot);
        }
        
        public static void TriggerItemCrafted(string itemId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Crafted: {itemId}");
            OnItemCrafted?.Invoke(itemId);
        }
        
        // === Quest ===
        
        public static void TriggerQuestStarted(string questId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Quest Started: {questId}");
            OnQuestStarted?.Invoke(questId);
        }
        
        public static void TriggerQuestObjectiveUpdated(string questId, int index, int current, int target)
        {
            OnQuestObjectiveUpdated?.Invoke(questId, index, current, target);
        }
        
        public static void TriggerQuestCompleted(string questId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Quest Completed: {questId}");
            OnQuestCompleted?.Invoke(questId);
        }
        
        public static void TriggerQuestFailed(string questId)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Quest Failed: {questId}");
            OnQuestFailed?.Invoke(questId);
        }
        
        // === Save ===
        
        public static void TriggerGameSaving(int slot)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Saving to slot {slot}...");
            OnGameSaving?.Invoke(slot);
        }
        
        public static void TriggerGameSaved(int slot)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Saved to slot {slot}");
            OnGameSaved?.Invoke(slot);
        }
        
        public static void TriggerGameLoading(int slot)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Loading from slot {slot}...");
            OnGameLoading?.Invoke(slot);
        }
        
        public static void TriggerGameLoaded(int slot)
        {
            if (DebugLogging) UnityEngine.Debug.Log($"[GameEvents] Loaded from slot {slot}");
            OnGameLoaded?.Invoke(slot);
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Отписаться от всех событий (для очистки при смене сцены).
        /// </summary>
        public static void ClearAllEvents()
        {
            OnGameStart = null;
            OnGamePause = null;
            OnGameResume = null;
            OnGameQuit = null;
            OnGameStateChanged = null;
            OnSceneLoaded = null;
            OnSceneUnloading = null;
            
            OnPlayerHealthChanged = null;
            OnPlayerQiChanged = null;
            OnPlayerCultivationLevelChanged = null;
            OnPlayerDeath = null;
            OnPlayerRevive = null;
            OnPlayerLocationChanged = null;
            OnPlayerMeditationStart = null;
            OnPlayerMeditationEnd = null;
            OnPlayerSleepStart = null;
            OnPlayerSleepEnd = null;
            OnPlayerBreakthrough = null;
            
            OnCombatStart = null;
            OnCombatEnd = null;
            OnDamageDealt = null;
            OnDamageTaken = null;
            OnTechniqueUsed = null;
            OnTechniqueLearned = null;
            OnTechniqueMastered = null;
            OnEnemyKilled = null;
            
            OnNPCInteract = null;
            OnDialogueStart = null;
            OnDialogueEnd = null;
            OnRelationChanged = null;
            OnNPCJoined = null;
            OnNPCLeft = null;
            
            OnTimeHourChanged = null;
            OnDayChanged = null;
            OnMonthChanged = null;
            OnYearChanged = null;
            OnTimeSpeedChanged = null;
            OnWorldEventTriggered = null;
            OnWorldEventEnded = null;
            
            OnItemAdded = null;
            OnItemRemoved = null;
            OnItemEquipped = null;
            OnItemUnequipped = null;
            OnItemCrafted = null;
            
            OnQuestStarted = null;
            OnQuestObjectiveUpdated = null;
            OnQuestCompleted = null;
            OnQuestFailed = null;
            
            OnGameSaving = null;
            OnGameSaved = null;
            OnGameLoading = null;
            OnGameLoaded = null;
            
            if (DebugLogging) UnityEngine.Debug.Log("[GameEvents] All events cleared");
        }
        
        #endregion
    }
}
