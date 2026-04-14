// ============================================================================
// WorldController.cs — Главный контроллер мира
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:17:18 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.NPC;
using CultivationGame.Generators;

namespace CultivationGame.World
{
    /// <summary>
    /// Данные мира.
    /// </summary>
    [Serializable]
    public class WorldData
    {
        public string WorldId;
        public string WorldName;
        public int WorldAge;            // В годах
        public long WorldSeed;
        
        // Глобальные параметры
        public float GlobalQiDensity = 1.0f;
        public int TotalPopulation = 0;
        public int CultivatorCount = 0;
        
        // История
        public List<WorldEvent> RecentEvents = new List<WorldEvent>();
        public List<string> MajorEvents = new List<string>();
    }
    
    /// <summary>
    /// Мировое событие.
    /// </summary>
    [Serializable]
    public class WorldEvent
    {
        public string EventId;
        public string EventName;
        public string Description;
        public WorldEventType Type;
        public int YearOccurred;
        public int MonthOccurred;
        public int DayOccurred;
        public string LocationId;
        public List<string> InvolvedNpcIds = new List<string>();
        
        // FIX WLD-H03: Replace Dictionary<string,object> with serializable structure (2026-04-11)
        // JsonUtility cannot serialize Dictionary<string,object>. Using List<WorldEventDataEntry> instead.
        public List<WorldEventDataEntry> EventData = new List<WorldEventDataEntry>();
        
        /// <summary>
        /// Helper: get event data value by key.
        /// </summary>
        public WorldEventData GetEventData(string key)
        {
            foreach (var entry in EventData)
            {
                if (entry.key == key)
                    return entry.value;
            }
            return null;
        }
        
        /// <summary>
        /// Helper: set event data value by key.
        /// </summary>
        public void SetEventData(string key, WorldEventData value)
        {
            for (int i = 0; i < EventData.Count; i++)
            {
                if (EventData[i].key == key)
                {
                    EventData[i] = new WorldEventDataEntry { key = key, value = value };
                    return;
                }
            }
            EventData.Add(new WorldEventDataEntry { key = key, value = value });
        }
    }
    
    /// <summary>
    /// FIX WLD-H03: Serializable key-value entry for WorldEvent.EventData (2026-04-11)
    /// Replaces Dictionary<string, object> which JsonUtility cannot serialize.
    /// </summary>
    [Serializable]
    public class WorldEventDataEntry
    {
        public string key;
        public WorldEventData value;
    }
    
    /// <summary>
    /// FIX WLD-H03: Serializable data value for WorldEvent.EventData (2026-04-11)
    /// Provides typed fields instead of object boxing which JsonUtility cannot handle.
    /// </summary>
    [Serializable]
    public class WorldEventData
    {
        public string StringValue = "";
        public int IntValue;
        public float FloatValue;
        public long LongValue;
        public bool BoolValue;
        
        // Static factory helpers
        public static WorldEventData FromString(string v) => new WorldEventData { StringValue = v };
        public static WorldEventData FromInt(int v) => new WorldEventData { IntValue = v };
        public static WorldEventData FromFloat(float v) => new WorldEventData { FloatValue = v };
        public static WorldEventData FromLong(long v) => new WorldEventData { LongValue = v };
        public static WorldEventData FromBool(bool v) => new WorldEventData { BoolValue = v };
    }
    
    /// <summary>
    /// Тип мирового события.
    /// </summary>
    public enum WorldEventType
    {
        Natural,        // Природное (землетрясение, засуха)
        Political,      // Политическое (война, союз)
        Cultivation,    // Культивация (прорыв, падение)
        Mystical,       // Мистическое (артефакт, явление)
        Disaster,       // Катастрофа
        Discovery,      // Открытие (новая земля, техника)
        Death,          // Смерть важного NPC
        Birth,          // Рождение
        Sect,           // Событие секты
        Tournament      // Турнир
    }
    
    /// <summary>
    /// Главный контроллер мира — координирует все системы.
    /// </summary>
    public class WorldController : MonoBehaviour
    {
        [Header("World Settings")]
        [SerializeField] private string worldName = "Cultivation World";
        [SerializeField] private long worldSeed = 12345;
        
        [Header("References")]
        [SerializeField] private TimeController timeController;
        [SerializeField] private LocationController locationController;
        [SerializeField] private FactionController factionController;
        [SerializeField] private EventController eventController;
        [SerializeField] private GeneratorRegistry generatorRegistry;
        
        // === Runtime ===
        private WorldData worldData;
        private Dictionary<string, NPCController> npcs = new Dictionary<string, NPCController>();
        private List<string> importantNpcIds = new List<string>();
        
        // === State ===
        private bool isInitialized = false;
        
        // === Events ===
        public event Action OnWorldInitialized;
        public event Action<int> OnYearPassed;
        public event Action<WorldEvent> OnWorldEvent;
        
        // === Properties ===
        public WorldData Data => worldData;
        public TimeController Time => timeController;
        public LocationController Locations => locationController;
        public FactionController Factions => factionController;
        public EventController Events => eventController;
        public GeneratorRegistry Generators => generatorRegistry;
        public bool IsInitialized => isInitialized;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            // FIX: Регистрация в ServiceLocator для O(1) доступа
            // Редактировано: 2026-04-14 06:10:00 UTC
            ServiceLocator.Register(this);
            InitializeControllers();
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Unregister<WorldController>();
        }
        
        private void Start()
        {
            InitializeWorld();
        }
        
        // === Initialization ===
        
        private void InitializeControllers()
        {
            if (timeController == null)
                timeController = GetComponent<TimeController>();
            if (locationController == null)
                locationController = GetComponent<LocationController>();
            if (factionController == null)
                factionController = GetComponent<FactionController>();
            if (eventController == null)
                eventController = GetComponent<EventController>();
            if (generatorRegistry == null)
                generatorRegistry = GetComponent<GeneratorRegistry>();
        }
        
        private void InitializeWorld()
        {
            worldData = new WorldData
            {
                WorldId = Guid.NewGuid().ToString(),
                WorldName = worldName,
                WorldSeed = worldSeed,
                WorldAge = 0
            };
            
            // Инициализируем генераторы с сидом мира
            if (generatorRegistry != null)
            {
                generatorRegistry.Initialize(worldSeed);
            }
            else
            {
                // Пробуем найти GeneratorRegistry
                generatorRegistry = GeneratorRegistry.Instance;
                if (generatorRegistry != null)
                {
                    generatorRegistry.Initialize(worldSeed);
                }
            }
            
            // Подписываемся на события времени
            if (timeController != null)
            {
                timeController.OnYearPassed += HandleYearPassed;
            }
            
            isInitialized = true;
            OnWorldInitialized?.Invoke();
            
            Debug.Log($"World initialized: {worldName} (Seed: {worldSeed})");
        }
        
        // === Time Events ===
        
        private void HandleYearPassed(int year)
        {
            worldData.WorldAge++;
            
            // Обновляем статистику мира
            UpdateWorldStatistics();
            
            // Проверяем случайные события
            if (eventController != null)
            {
                eventController.CheckYearlyEvents();
            }
            
            // Старение NPC
            ProcessNPCAging();
            
            OnYearPassed?.Invoke(year);
        }
        
        private void UpdateWorldStatistics()
        {
            worldData.TotalPopulation = npcs.Count;
            worldData.CultivatorCount = 0;
            
            foreach (var kvp in npcs)
            {
                if (kvp.Value.CultivationLevel != CultivationLevel.None)
                {
                    worldData.CultivatorCount++;
                }
            }
        }
        
        private void ProcessNPCAging()
        {
            foreach (var kvp in npcs)
            {
                NPCController npc = kvp.Value;
                if (npc != null && npc.IsAlive)
                {
                    npc.CheckDeathFromAge();
                }
            }
        }
        
        // === NPC Management ===
        
        /// <summary>
        /// Зарегистрировать NPC в мире.
        /// </summary>
        public void RegisterNPC(NPCController npc)
        {
            if (npc == null) return;
            
            npcs[npc.NpcId] = npc;
        }
        
        /// <summary>
        /// Удалить NPC из мира.
        /// </summary>
        public void UnregisterNPC(string npcId)
        {
            npcs.Remove(npcId);
        }
        
        /// <summary>
        /// Получить NPC по ID.
        /// </summary>
        public NPCController GetNPC(string npcId)
        {
            if (npcs.TryGetValue(npcId, out NPCController npc))
            {
                return npc;
            }
            return null;
        }
        
        /// <summary>
        /// Получить всех NPC в локации.
        /// </summary>
        public List<NPCController> GetNPCsInLocation(string locationId)
        {
            List<NPCController> result = new List<NPCController>();
            
            foreach (var kvp in npcs)
            {
                if (kvp.Value.State.CurrentLocation == locationId)
                {
                    result.Add(kvp.Value);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Получить всех NPC с уровнем культивации.
        /// </summary>
        public List<NPCController> GetCultivators(CultivationLevel minLevel = CultivationLevel.AwakenedCore)
        {
            List<NPCController> result = new List<NPCController>();
            
            foreach (var kvp in npcs)
            {
                if (kvp.Value.CultivationLevel >= minLevel)
                {
                    result.Add(kvp.Value);
                }
            }
            
            return result;
        }
        
        // === World Events ===
        
        /// <summary>
        /// Записать мировое событие.
        /// </summary>
        public void RecordEvent(WorldEvent worldEvent)
        {
            if (worldEvent == null) return;
            
            worldData.RecentEvents.Add(worldEvent);
            
            // Ограничиваем историю
            if (worldData.RecentEvents.Count > 100)
            {
                worldData.RecentEvents.RemoveAt(0);
            }
            
            // Важные события сохраняем отдельно
            if (worldEvent.Type == WorldEventType.Death ||
                worldEvent.Type == WorldEventType.Cultivation ||
                worldEvent.Type == WorldEventType.Sect)
            {
                worldData.MajorEvents.Add(worldEvent.EventId);
            }
            
            OnWorldEvent?.Invoke(worldEvent);
        }
        
        /// <summary>
        /// Создать событие.
        /// </summary>
        public WorldEvent CreateEvent(string name, WorldEventType type, string description = "")
        {
            return new WorldEvent
            {
                EventId = Guid.NewGuid().ToString(),
                EventName = name,
                Type = type,
                Description = description,
                YearOccurred = timeController?.Year ?? 1,
                MonthOccurred = timeController?.Month ?? 1,
                DayOccurred = timeController?.Day ?? 1
            };
        }
        
        // === Queries ===
        
        /// <summary>
        /// Получить статистику мира.
        /// </summary>
        public WorldStatistics GetStatistics()
        {
            WorldStatistics stats = new WorldStatistics();
            
            stats.TotalNPCs = npcs.Count;
            stats.Year = worldData.WorldAge;
            
            foreach (var kvp in npcs)
            {
                if (kvp.Value.IsAlive)
                {
                    stats.AliveNPCs++;
                    if (kvp.Value.CultivationLevel != CultivationLevel.None)
                        stats.Cultivators++;
                }
            }
            
            return stats;
        }
        
        // === Save/Load ===
        
        public WorldSaveData GetSaveData()
        {
            return new WorldSaveData
            {
                WorldId = worldData.WorldId,
                WorldName = worldData.WorldName,
                WorldAge = worldData.WorldAge,
                WorldSeed = worldData.WorldSeed,
                GlobalQiDensity = worldData.GlobalQiDensity,
                ImportantNpcIds = importantNpcIds.ToArray()
            };
        }
        
        public void LoadSaveData(WorldSaveData data)
        {
            worldData = new WorldData
            {
                WorldId = data.WorldId,
                WorldName = data.WorldName,
                WorldAge = data.WorldAge,
                WorldSeed = data.WorldSeed,
                GlobalQiDensity = data.GlobalQiDensity
            };
            
            importantNpcIds = new List<string>(data.ImportantNpcIds);
        }
    }
    
    /// <summary>
    /// Статистика мира.
    /// </summary>
    public struct WorldStatistics
    {
        public int TotalNPCs;
        public int AliveNPCs;
        public int Cultivators;
        public int Year;
    }
    
    /// <summary>
    /// Данные мира для сохранения.
    /// </summary>
    [Serializable]
    public class WorldSaveData
    {
        public string WorldId;
        public string WorldName;
        public int WorldAge;
        public long WorldSeed;
        public float GlobalQiDensity;
        public string[] ImportantNpcIds;
    }
}
