// ============================================================================
// LocationController.cs — Система локаций
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
    /// Данные локации.
    /// </summary>
    [Serializable]
    public class LocationData
    {
        public string LocationId;
        public string LocationName;
        public string ParentLocationId;    // Для вложенных локаций
        public LocationType Type;
        public TerrainType Terrain;
        public BuildingType? BuildingType;
        
        [TextArea(2, 4)]
        public string Description;
        
        // Координаты на карте мира
        public Vector2 WorldPosition;
        
        // Характеристики
        public float QiDensity = 1.0f;           // Плотность Ци
        public float DangerLevel = 0f;           // Уровень опасности (0-100)
        public int RecommendedLevel = 1;         // Рекомендуемый уровень культивации
        
        // Связи
        public List<string> ConnectedLocations = new List<string>();
        public List<string> NPCIds = new List<string>();
        public List<string> ResourceNodes = new List<string>();
        
        // Флаги
        public bool IsDiscovered = false;
        public bool IsSafeZone = false;
        public bool HasSect = false;
        public string SectId;
    }
    
    /// <summary>
    /// Переход между локациями.
    /// </summary>
    [Serializable]
    public class LocationTransition
    {
        public string FromLocationId;
        public string ToLocationId;
        public float TravelTimeMinutes = 60f;
        public float DangerChance = 0f;
        public int RequiredLevel = 0;
        public string RequiredItem;
        public bool IsDiscovered = false;
    }
    
    /// <summary>
    /// Контроллер локаций — управление всеми локациями в мире.
    /// </summary>
    public class LocationController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float baseTravelSpeed = 1f;
        [SerializeField] private float travelDangerBaseChance = 0.1f;
        
        // === Storage ===
        private Dictionary<string, LocationData> locations = new Dictionary<string, LocationData>();
        private Dictionary<string, LocationTransition> transitions = new Dictionary<string, LocationTransition>();
        private string currentLocationId;
        private List<string> discoveredLocations = new List<string>();
        
        // === Travel State ===
        private bool isTraveling = false;
        private string travelDestinationId;
        private float travelProgress = 0f;
        private float travelDuration = 0f;
        
        // === References ===
        private TimeController timeController;
        
        // === Events ===
        public event Action<string> OnLocationEntered;
        public event Action<string> OnLocationExited;
        public event Action<string, string> OnTravelStarted;    // from, to
        public event Action<string> OnTravelCompleted;
        public event Action<string> OnTravelInterrupted;
        public event Action<string> OnLocationDiscovered;
        public event Action<float, float> OnTravelProgress;     // progress, duration
        
        // === Properties ===
        public LocationData CurrentLocation => GetLocation(currentLocationId);
        public string CurrentLocationId => currentLocationId;
        public bool IsTraveling => isTraveling;
        public float TravelProgress => travelProgress;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            timeController = GetComponent<TimeController>();
            if (timeController == null)
                timeController = FindFirstObjectByType<TimeController>();
        }
        
        private void Update()
        {
            if (isTraveling)
            {
                ProcessTravel();
            }
        }
        
        // === Location Management ===
        
        /// <summary>
        /// Создать новую локацию.
        /// </summary>
        public LocationData CreateLocation(string name, LocationType type, Vector2 position)
        {
            LocationData location = new LocationData
            {
                LocationId = Guid.NewGuid().ToString(),
                LocationName = name,
                Type = type,
                WorldPosition = position
            };
            
            locations[location.LocationId] = location;
            return location;
        }
        
        /// <summary>
        /// Получить локацию по ID.
        /// </summary>
        public LocationData GetLocation(string locationId)
        {
            if (!string.IsNullOrEmpty(locationId) && locations.TryGetValue(locationId, out LocationData location))
            {
                return location;
            }
            return null;
        }
        
        /// <summary>
        /// Получить все локации.
        /// </summary>
        public List<LocationData> GetAllLocations()
        {
            return new List<LocationData>(locations.Values);
        }
        
        /// <summary>
        /// Получить соседние локации.
        /// </summary>
        public List<LocationData> GetConnectedLocations(string locationId)
        {
            List<LocationData> result = new List<LocationData>();
            LocationData location = GetLocation(locationId);
            
            if (location != null && location.ConnectedLocations != null)
            {
                foreach (string connectedId in location.ConnectedLocations)
                {
                    LocationData connected = GetLocation(connectedId);
                    if (connected != null)
                    {
                        result.Add(connected);
                    }
                }
            }
            
            return result;
        }
        
        // === Location State ===
        
        /// <summary>
        /// Войти в локацию.
        /// </summary>
        public bool EnterLocation(string locationId)
        {
            if (isTraveling) return false;
            if (!locations.ContainsKey(locationId)) return false;
            
            string oldLocationId = currentLocationId;
            
            if (!string.IsNullOrEmpty(oldLocationId))
            {
                OnLocationExited?.Invoke(oldLocationId);
            }
            
            currentLocationId = locationId;
            
            // Отмечаем как открытую
            if (!discoveredLocations.Contains(locationId))
            {
                discoveredLocations.Add(locationId);
                LocationData location = GetLocation(locationId);
                if (location != null)
                {
                    location.IsDiscovered = true;
                }
                OnLocationDiscovered?.Invoke(locationId);
            }
            
            OnLocationEntered?.Invoke(locationId);
            return true;
        }
        
        /// <summary>
        /// Проверить, открыта ли локация.
        /// </summary>
        public bool IsLocationDiscovered(string locationId)
        {
            return discoveredLocations.Contains(locationId);
        }
        
        // === Travel ===
        
        /// <summary>
        /// Начать путешествие в указанную локацию.
        /// </summary>
        public bool StartTravel(string destinationId)
        {
            if (isTraveling) return false;
            if (string.IsNullOrEmpty(currentLocationId)) return false;
            if (!locations.ContainsKey(destinationId)) return false;
            
            LocationData current = GetLocation(currentLocationId);
            LocationData destination = GetLocation(destinationId);
            
            if (current == null || destination == null) return false;
            
            // Проверяем, есть ли прямое соединение
            if (!current.ConnectedLocations.Contains(destinationId))
            {
                // Нет прямого пути
                return false;
            }
            
            // Рассчитываем время путешествия
            float distance = Vector2.Distance(current.WorldPosition, destination.WorldPosition);
            travelDuration = CalculateTravelTime(distance, current, destination);
            
            // Начинаем путешествие
            isTraveling = true;
            travelDestinationId = destinationId;
            travelProgress = 0f;
            
            OnTravelStarted?.Invoke(currentLocationId, destinationId);
            
            return true;
        }
        
        /// <summary>
        /// Рассчитать время путешествия.
        /// </summary>
        private float CalculateTravelTime(float distance, LocationData from, LocationData to)
        {
            // Базовое время: расстояние / скорость
            float baseTime = distance / baseTravelSpeed;
            
            // Модификаторы
            float terrainMod = GetTerrainModifier(from.Terrain) + GetTerrainModifier(to.Terrain);
            baseTime *= (1f + terrainMod * 0.5f);
            
            // Минимум 30 минут
            return Mathf.Max(30f, baseTime);
        }
        
        private float GetTerrainModifier(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Plains => 0f,
                TerrainType.Forest => 0.2f,
                TerrainType.Mountains => 0.5f,
                TerrainType.Swamp => 0.4f,
                TerrainType.Desert => 0.3f,
                TerrainType.Jungle => 0.3f,
                TerrainType.Tundra => 0.3f,
                TerrainType.Volcanic => 0.6f,
                TerrainType.Sea => 0.8f,
                TerrainType.Spiritual => 0.1f,
                _ => 0f
            };
        }
        
        private void ProcessTravel()
        {
            // Прогресс зависит от скорости времени
            float timeRatio = timeController != null ? GetTimeRatio() : 60f;
            travelProgress += Time.deltaTime * timeRatio;
            
            OnTravelProgress?.Invoke(travelProgress, travelDuration);
            
            if (travelProgress >= travelDuration)
            {
                CompleteTravel();
            }
        }
        
        private float GetTimeRatio()
        {
            if (timeController == null) return 60f;
            
            return timeController.CurrentSpeed switch
            {
                TimeSpeed.Normal => 60f,
                TimeSpeed.Fast => 300f,
                TimeSpeed.VeryFast => 900f,
                _ => 0f
            };
        }
        
        private void CompleteTravel()
        {
            // FIX WLD-C01: Save destinationId and destination data BEFORE clearing travel state (2026-04-11)
            // Previously travelDestinationId was set to null before ShouldTriggerTravelEvent read it,
            // causing the destination lookup to always return null.
            string destinationId = travelDestinationId;
            LocationData destinationData = GetLocation(travelDestinationId);
            
            isTraveling = false;
            travelDestinationId = null;
            travelProgress = 0f;
            
            // Проверяем на случайное событие — передаём сохранённые данные
            if (ShouldTriggerTravelEvent(destinationData))
            {
                OnTravelInterrupted?.Invoke(destinationId);
                return;
            }
            
            // Входим в новую локацию
            EnterLocation(destinationId);
            OnTravelCompleted?.Invoke(destinationId);
        }
        
        private bool ShouldTriggerTravelEvent(LocationData destination = null)
        {
            // Базовый шанс события
            float chance = travelDangerBaseChance;
            
            // Учитываем опасность локаций
            LocationData current = GetLocation(currentLocationId);
            
            if (current != null) chance += current.DangerLevel * 0.001f;
            if (destination != null) chance += destination.DangerLevel * 0.001f;
            
            return UnityEngine.Random.value < chance;
        }
        
        /// <summary>
        /// Отменить текущее путешествие.
        /// </summary>
        public void CancelTravel()
        {
            if (!isTraveling) return;
            
            isTraveling = false;
            string destinationId = travelDestinationId;
            travelDestinationId = null;
            travelProgress = 0f;
            
            OnTravelInterrupted?.Invoke(destinationId);
        }
        
        // === Connections ===
        
        /// <summary>
        /// Создать связь между локациями.
        /// </summary>
        public void ConnectLocations(string location1Id, string location2Id, float travelTime = 0f)
        {
            LocationData loc1 = GetLocation(location1Id);
            LocationData loc2 = GetLocation(location2Id);
            
            if (loc1 == null || loc2 == null) return;
            
            if (!loc1.ConnectedLocations.Contains(location2Id))
            {
                loc1.ConnectedLocations.Add(location2Id);
            }
            
            if (!loc2.ConnectedLocations.Contains(location1Id))
            {
                loc2.ConnectedLocations.Add(location1Id);
            }
            
            // Создаём переход
            string transitionKey = $"{location1Id}_{location2Id}";
            transitions[transitionKey] = new LocationTransition
            {
                FromLocationId = location1Id,
                ToLocationId = location2Id,
                TravelTimeMinutes = travelTime
            };
        }
        
        // === Queries ===
        
        /// <summary>
        /// Найти ближайшую локацию.
        /// </summary>
        public LocationData FindNearestLocation(Vector2 position, LocationType? typeFilter = null)
        {
            LocationData nearest = null;
            float nearestDist = float.MaxValue;
            
            foreach (var kvp in locations)
            {
                LocationData loc = kvp.Value;
                
                if (typeFilter.HasValue && loc.Type != typeFilter.Value)
                    continue;
                
                float dist = Vector2.Distance(position, loc.WorldPosition);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = loc;
                }
            }
            
            return nearest;
        }
        
        /// <summary>
        /// Получить локации в радиусе.
        /// </summary>
        public List<LocationData> GetLocationsInRange(Vector2 position, float range)
        {
            List<LocationData> result = new List<LocationData>();
            
            foreach (var kvp in locations)
            {
                float dist = Vector2.Distance(position, kvp.Value.WorldPosition);
                if (dist <= range)
                {
                    result.Add(kvp.Value);
                }
            }
            
            return result;
        }
        
        // === Save/Load ===
        
        public LocationSaveData GetSaveData()
        {
            return new LocationSaveData
            {
                CurrentLocationId = currentLocationId,
                DiscoveredLocations = discoveredLocations.ToArray(),
                IsTraveling = isTraveling,
                TravelDestinationId = travelDestinationId,
                TravelProgress = travelProgress
            };
        }
        
        public void LoadSaveData(LocationSaveData data)
        {
            currentLocationId = data.CurrentLocationId;
            discoveredLocations = new List<string>(data.DiscoveredLocations);
            isTraveling = data.IsTraveling;
            travelDestinationId = data.TravelDestinationId;
            travelProgress = data.TravelProgress;
        }
    }
    
    /// <summary>
    /// Данные локаций для сохранения.
    /// </summary>
    [Serializable]
    public class LocationSaveData
    {
        public string CurrentLocationId;
        public string[] DiscoveredLocations;
        public bool IsTraveling;
        public string TravelDestinationId;
        public float TravelProgress;
    }
}
