// ============================================================================
// LocationData.cs — Данные локации
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 09:20:39 UTC
// Редактировано: 2026-04-03 09:20:39 UTC
// ============================================================================
//
// Источник: docs/DATA_MODELS.md §4 "Location"
//
// Иерархия локаций:
// Region → Area → Building → Room
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Тип локации
    /// </summary>
    public enum LocationType
    {
        Region,         // Регион (большая область)
        Area,           // Область (часть региона)
        Building,       // Здание
        Room,           // Комната
        Dungeon,        // Подземелье
        Secret          // Секретная область
    }

    /// <summary>
    /// Данные локации.
    /// Создаётся как ScriptableObject для каждой локации в мире.
    /// </summary>
    [CreateAssetMenu(fileName = "Location", menuName = "Cultivation/Location")]
    public class LocationData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Уникальный ID локации")]
        public string locationId;
        
        [Tooltip("Название на русском")]
        public string nameRu;
        
        [Tooltip("Название на английском")]
        public string nameEn;
        
        [TextArea(2, 4)]
        [Tooltip("Описание локации")]
        public string description;
        
        [Tooltip("Иконка локации")]
        public Sprite icon;
        
        [Header("Classification")]
        [Tooltip("Тип локации")]
        public LocationType locationType;
        
        [Tooltip("Тип местности")]
        public TerrainType terrainType;
        
        [Tooltip("Родительская локация (для Area/Building/Room)")]
        public LocationData parentLocation;
        
        [Tooltip("Дочерние локации")]
        public List<LocationData> childLocations = new List<LocationData>();
        
        [Header("Coordinates")]
        [Tooltip("Восток(+)/Запад(-)")]
        public int coordX;
        
        [Tooltip("Север(+)/Юг(-)")]
        public int coordY;
        
        [Tooltip("Высота(+)/Глубина(-)")]
        public int coordZ;
        
        [Tooltip("Расстояние от центра мира")]
        public int distanceFromCenter;
        
        [Header("Dimensions")]
        [Tooltip("Ширина (метры)")]
        public int width = 100;
        
        [Tooltip("Длина (метры)")]
        public int length = 100;
        
        [Tooltip("Высота (метры)")]
        public int height = 10;
        
        [Header("Qi Properties")]
        [Tooltip("Плотность Ци (ед/м³)")]
        [Range(0.1f, 100f)]
        public float qiDensity = 1.0f;
        
        [Tooltip("Поток Ци (ед/сек)")]
        [Range(0f, 10f)]
        public float qiFlowRate = 1.0f;
        
        [Tooltip("Бонус к медитации (%)")]
        [Range(-50f, 100f)]
        public float meditationBonus = 0f;
        
        [Header("Environment")]
        [Tooltip("Температура (°C)")]
        [Range(-50f, 50f)]
        public float temperature = 20f;
        
        [Tooltip("Влажность (%)")]
        [Range(0f, 100f)]
        public float humidity = 50f;
        
        [Tooltip("Опасность (%)")]
        [Range(0f, 100f)]
        public float dangerLevel = 0f;
        
        [Header("Resources")]
        [Tooltip("Доступные ресурсы")]
        public List<LocationResource> resources = new List<LocationResource>();
        
        [Tooltip("Шанс встречи врага (%)")]
        [Range(0f, 100f)]
        public float enemyEncounterChance = 0f;
        
        [Tooltip("Возможные враги")]
        public List<string> possibleEnemies = new List<string>();
        
        [Header("Access")]
        [Tooltip("Минимальный уровень культивации для входа")]
        [Range(0, 10)]
        public int minCultivationLevel = 0;
        
        [Tooltip("Требуется разрешение фракции")]
        public bool requiresFactionPermission = false;
        
        [Tooltip("ID фракции")]
        public string requiredFactionId;
        
        [Tooltip("Минимальная репутация с фракцией")]
        [Range(-100, 100)]
        public int minFactionReputation = 0;
        
        [Header("Connections")]
        [Tooltip("Связанные локации (куда можно перейти)")]
        public List<LocationConnection> connections = new List<LocationConnection>();
        
        [Header("NPCs")]
        [Tooltip("NPC в локации")]
        public List<string> residentNPCs = new List<string>();
        
        [Tooltip("Торговцы в локации")]
        public List<string> merchants = new List<string>();
        
        // === Runtime Methods ===
        
        /// <summary>
        /// Получить мировые координаты
        /// </summary>
        public Vector3 GetWorldPosition()
        {
            return new Vector3(coordX, coordZ, coordY);
        }
        
        /// <summary>
        /// Получить расстояние до другой локации
        /// </summary>
        public float GetDistanceTo(LocationData other)
        {
            float dx = coordX - other.coordX;
            float dy = coordY - other.coordY;
            float dz = coordZ - other.coordZ;
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }
        
        /// <summary>
        /// Проверить доступность для персонажа
        /// </summary>
        public bool IsAccessible(int cultivationLevel, Dictionary<string, int> factionReputation)
        {
            if (cultivationLevel < minCultivationLevel)
                return false;
            
            if (requiresFactionPermission && !string.IsNullOrEmpty(requiredFactionId))
            {
                if (!factionReputation.TryGetValue(requiredFactionId, out int rep))
                    return false;
                if (rep < minFactionReputation)
                    return false;
            }
            
            return true;
        }
    }
    
    /// <summary>
    /// Ресурс в локации
    /// </summary>
    [System.Serializable]
    public class LocationResource
    {
        [Tooltip("ID ресурса")]
        public string resourceId;
        
        [Tooltip("Тип ресурса")]
        public ResourceType resourceType;
        
        [Tooltip("Количество")]
        [Range(1, 1000)]
        public int amount = 10;
        
        [Tooltip("Время восстановления (часы)")]
        [Range(0, 168)]
        public int respawnHours = 24;
        
        [Tooltip("Требуемый уровень для сбора")]
        [Range(0, 10)]
        public int requiredLevel = 0;
        
        [Tooltip("Требуемый инструмент")]
        public string requiredTool;
    }
    
    /// <summary>
    /// Тип ресурса
    /// </summary>
    public enum ResourceType
    {
        Herb,           // Трава
        Ore,            // Руда
        Wood,           // Дерево
        Water,          // Вода
        SpiritStone,    // Духовный камень
        Crystal,        // Кристалл
        Special         // Специальный
    }
    
    /// <summary>
    /// Связь между локациями
    /// </summary>
    [System.Serializable]
    public class LocationConnection
    {
        [Tooltip("Целевая локация")]
        public LocationData targetLocation;
        
        [Tooltip("Расстояние (метры)")]
        public int distance = 100;
        
        [Tooltip("Время пути (минуты)")]
        public int travelTimeMinutes = 30;
        
        [Tooltip("Базовый шанс опасности (%)")]
        [Range(0f, 100f)]
        public float dangerChance = 0f;
        
        [Tooltip("Требования для прохода")]
        public string requirement;
    }
}
