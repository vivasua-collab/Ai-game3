// ============================================================================
// EventController.cs — Система мировых событий
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:17:18 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.World
{
    /// <summary>
    /// Шаблон события.
    /// </summary>
    [Serializable]
    public class EventTemplate
    {
        public string TemplateId;
        public string EventName;
        public WorldEventType Type;
        
        [TextArea(2, 4)]
        public string Description;
        
        // Условия
        public int MinYear = 0;
        public int MaxYear = int.MaxValue;
        public Season? RequiredSeason;
        public TimeOfDay? RequiredTimeOfDay;
        public float BaseChance = 0.1f;
        public int CooldownDays = 30;
        
        // Эффекты
        public EventEffect[] Effects;
        
        // Требования
        public CultivationLevel? MinCultivationLevel;
        public string RequiredLocationType;
    }
    
    /// <summary>
    /// Эффект события.
    /// </summary>
    [Serializable]
    public class EventEffect
    {
        public EventEffectType Type;
        public string StringValue;
        public int IntValue;
        public float FloatValue;
    }
    
    /// <summary>
    /// Тип эффекта события.
    /// </summary>
    public enum EventEffectType
    {
        ModifyQiDensity,
        SpawnNPC,
        KillNPC,
        CreateLocation,
        DestroyLocation,
        ModifyRelation,
        TriggerBreakthrough,
        GiveItem,
        GiveTechnique,
        ModifyResource,
        SendMessage,
        StartQuest
    }
    
    /// <summary>
    /// Активное событие.
    /// </summary>
    [Serializable]
    public class ActiveEvent
    {
        public string InstanceId;
        public string TemplateId;
        public WorldEvent EventData;
        public float StartTime;
        public float Duration;
        public string LocationId;
        public bool IsActive;
        public bool IsProcessed;
    }
    
    /// <summary>
    /// Контроллер событий — управление случайными и сюжетными событиями.
    /// </summary>
    public class EventController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float eventCheckInterval = 60f;    // Проверка каждую минуту
        [SerializeField] private float baseEventChance = 0.05f;    // 5% базовый шанс
        [SerializeField] private int maxActiveEvents = 5;
        [SerializeField] private int eventHistorySize = 50;
        
        // === References ===
        private TimeController timeController;
        private WorldController worldController;
        
        // === Storage ===
        private Dictionary<string, EventTemplate> templates = new Dictionary<string, EventTemplate>();
        private List<ActiveEvent> activeEvents = new List<ActiveEvent>();
        private List<WorldEvent> eventHistory = new List<WorldEvent>();
        private Dictionary<string, float> lastOccurrence = new Dictionary<string, float>();
        
        // === State ===
        private float checkTimer = 0f;
        
        // FIX WLD-M04: Track last game time for delta calculation (2026-04-11)
        private double lastCheckGameSeconds = 0;
        
        // === Events ===
        public event Action<ActiveEvent> OnEventStarted;
        public event Action<ActiveEvent> OnEventEnded;
        public event Action<WorldEvent> OnEventRecorded;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            timeController = GetComponent<TimeController>();
            worldController = GetComponent<WorldController>();
            
            if (timeController == null)
                timeController = FindFirstObjectByType<TimeController>();
            if (worldController == null)
                worldController = FindFirstObjectByType<WorldController>();
        }
        
        private void Update()
        {
            // FIX WLD-M04: Use game time for event check interval instead of real time (2026-04-11)
            double currentGameSeconds = timeController != null ? timeController.TotalGameSeconds : 0;
            double gameDelta = currentGameSeconds - lastCheckGameSeconds;
            lastCheckGameSeconds = currentGameSeconds;
            
            // Fallback to real time if game time is not advancing (paused or no TimeController)
            float delta = gameDelta > 0 ? (float)gameDelta : Time.deltaTime;
            
            checkTimer += delta;
            
            if (checkTimer >= eventCheckInterval)
            {
                checkTimer = 0f;
                CheckForEvents();
            }
            
            // Обрабатываем активные события
            ProcessActiveEvents();
        }
        
        // === Event Checking ===
        
        /// <summary>
        /// Проверить возможность события.
        /// </summary>
        private void CheckForEvents()
        {
            if (activeEvents.Count >= maxActiveEvents) return;
            
            foreach (var kvp in templates)
            {
                EventTemplate template = kvp.Value;
                
                if (CanTriggerEvent(template))
                {
                    if (UnityEngine.Random.value < template.BaseChance)
                    {
                        TriggerEvent(template);
                        break; // Только одно событие за проверку
                    }
                }
            }
        }
        
        /// <summary>
        /// Проверить годовое событие (вызывается из WorldController).
        /// </summary>
        public void CheckYearlyEvents()
        {
            // Проверяем события, которые происходят раз в год
            foreach (var kvp in templates)
            {
                EventTemplate template = kvp.Value;
                
                if (template.Type == WorldEventType.Natural ||
                    template.Type == WorldEventType.Cultivation)
                {
                    // Годовые события имеют больший шанс
                    if (UnityEngine.Random.value < template.BaseChance * 10f)
                    {
                        TriggerEvent(template);
                    }
                }
            }
        }
        
        private bool CanTriggerEvent(EventTemplate template)
        {
            // Проверка времени
            int currentYear = timeController?.Year ?? 1;
            if (currentYear < template.MinYear || currentYear > template.MaxYear)
                return false;
            
            // FIX WLD-H02: Use game time for cooldown instead of real Time.time (2026-04-11)
            if (lastOccurrence.TryGetValue(template.TemplateId, out float lastTime))
            {
                float currentTime = timeController != null ? (float)timeController.TotalGameSeconds : Time.time;
                float cooldownSeconds = template.CooldownDays * 86400f / 60f; // Учитывая ускорение времени
                if (currentTime - lastTime < cooldownSeconds)
                    return false;
            }
            
            // Проверка сезона
            if (template.RequiredSeason.HasValue)
            {
                Season currentSeason = timeController?.CurrentSeason ?? Season.Spring;
                if (currentSeason != template.RequiredSeason.Value)
                    return false;
            }
            
            // Проверка времени суток
            if (template.RequiredTimeOfDay.HasValue)
            {
                TimeOfDay currentTimeOfDay = timeController?.CurrentTimeOfDay ?? TimeOfDay.Morning;
                if (currentTimeOfDay != template.RequiredTimeOfDay.Value)
                    return false;
            }
            
            return true;
        }
        
        // === Event Triggering ===
        
        /// <summary>
        /// Запустить событие.
        /// </summary>
        public ActiveEvent TriggerEvent(EventTemplate template)
        {
            if (activeEvents.Count >= maxActiveEvents) return null;
            
            ActiveEvent activeEvent = new ActiveEvent
            {
                InstanceId = Guid.NewGuid().ToString(),
                TemplateId = template.TemplateId,
                EventData = new WorldEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventName = template.EventName,
                    Type = template.Type,
                    Description = template.Description,
                    YearOccurred = timeController?.Year ?? 1,
                    MonthOccurred = timeController?.Month ?? 1,
                    DayOccurred = timeController?.Day ?? 1
                },
                StartTime = timeController != null ? (float)timeController.TotalGameSeconds : Time.time, // FIX WLD-H02: game time (2026-04-11)
                Duration = 60f, // По умолчанию 1 минута реального времени
                IsActive = true
            };
            
            activeEvents.Add(activeEvent);
            lastOccurrence[template.TemplateId] = timeController != null ? (float)timeController.TotalGameSeconds : Time.time; // FIX WLD-H02: game time (2026-04-11)
            
            // Применяем эффекты
            ApplyEffects(template.Effects, activeEvent);
            
            OnEventStarted?.Invoke(activeEvent);
            
            Debug.Log($"Event started: {template.EventName}");
            
            return activeEvent;
        }
        
        /// <summary>
        /// Принудительно запустить событие по ID шаблона.
        /// </summary>
        public ActiveEvent ForceEvent(string templateId)
        {
            if (templates.TryGetValue(templateId, out EventTemplate template))
            {
                return TriggerEvent(template);
            }
            return null;
        }
        
        private void ApplyEffects(EventEffect[] effects, ActiveEvent activeEvent)
        {
            if (effects == null) return;
            
            foreach (var effect in effects)
            {
                ApplyEffect(effect, activeEvent);
            }
        }
        
        private void ApplyEffect(EventEffect effect, ActiveEvent activeEvent)
        {
            switch (effect.Type)
            {
                case EventEffectType.ModifyQiDensity:
                    // Изменение плотности Ци в локации
                    break;
                    
                case EventEffectType.SendMessage:
                    Debug.Log($"[Event] {effect.StringValue}");
                    break;
                    
                case EventEffectType.ModifyRelation:
                    // Изменение отношений между фракциями
                    break;
                    
                default:
                    // Остальные эффекты реализуются при интеграции
                    break;
            }
        }
        
        // === Active Event Processing ===
        
        private void ProcessActiveEvents()
        {
            for (int i = activeEvents.Count - 1; i >= 0; i--)
            {
                ActiveEvent activeEvent = activeEvents[i];
                
                if (!activeEvent.IsActive)
                {
                    EndEvent(activeEvent);
                    activeEvents.RemoveAt(i);
                }
                // FIX WLD-H02: Use game time for event duration check (2026-04-11)
                else if ((timeController != null ? (float)timeController.TotalGameSeconds : Time.time) - activeEvent.StartTime >= activeEvent.Duration)
                {
                    activeEvent.IsActive = false;
                }
            }
        }
        
        private void EndEvent(ActiveEvent activeEvent)
        {
            // Записываем в историю
            eventHistory.Add(activeEvent.EventData);
            
            if (eventHistory.Count > eventHistorySize)
            {
                eventHistory.RemoveAt(0);
            }
            
            OnEventEnded?.Invoke(activeEvent);
            OnEventRecorded?.Invoke(activeEvent.EventData);
        }
        
        // === Template Management ===
        
        /// <summary>
        /// Добавить шаблон события.
        /// </summary>
        public void AddTemplate(EventTemplate template)
        {
            if (template != null && !string.IsNullOrEmpty(template.TemplateId))
            {
                templates[template.TemplateId] = template;
            }
        }
        
        /// <summary>
        /// Получить шаблон.
        /// </summary>
        public EventTemplate GetTemplate(string templateId)
        {
            if (templates.TryGetValue(templateId, out EventTemplate template))
            {
                return template;
            }
            return null;
        }
        
        // === Queries ===
        
        /// <summary>
        /// Получить активные события.
        /// </summary>
        public List<ActiveEvent> GetActiveEvents()
        {
            return new List<ActiveEvent>(activeEvents);
        }
        
        /// <summary>
        /// Получить историю событий.
        /// </summary>
        public List<WorldEvent> GetEventHistory(int count = 10)
        {
            List<WorldEvent> result = new List<WorldEvent>();
            
            int startIndex = Mathf.Max(0, eventHistory.Count - count);
            for (int i = startIndex; i < eventHistory.Count; i++)
            {
                result.Add(eventHistory[i]);
            }
            
            return result;
        }
        
        // === Save/Load ===
        
        public EventSaveData GetSaveData()
        {
            return new EventSaveData
            {
                ActiveEvents = activeEvents.ConvertAll(e => e.InstanceId).ToArray(),
                EventHistory = eventHistory.ToArray()
            };
        }
        
        public void LoadSaveData(EventSaveData data)
        {
            eventHistory = new List<WorldEvent>(data.EventHistory);
        }
    }
    
    /// <summary>
    /// Данные событий для сохранения.
    /// </summary>
    [Serializable]
    public class EventSaveData
    {
        public string[] ActiveEvents;
        public WorldEvent[] EventHistory;
    }
}
