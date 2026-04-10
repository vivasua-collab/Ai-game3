// ============================================================================
// TerrainConfig.cs — Конфигурация типов поверхности
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-09 10:50:00 UTC
// ============================================================================
//
// РЕШЕНИЕ ПРОБЛЕМЫ: Magic numbers в TileData.UpdateTerrainProperties()
// Конфигурация вынесена в ScriptableObject для удобного редактирования
//
// ИСПОЛЬЗОВАНИЕ:
// 1. Создать TerrainConfig asset в Editor: Create > Data > Terrain Config
// 2. Настроить параметры для каждого типа поверхности
// 3. TileData.UpdateTerrainProperties() использует конфигурацию
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Конфигурация одного типа поверхности.
    /// </summary>
    [Serializable]
    public class TerrainTypeConfig
    {
        public TerrainType type;
        
        [Header("Movement")]
        [Tooltip("Множитель стоимости движения (0 = непроходимо)")]
        [Range(0f, 5f)] public float moveCost = 1.0f;
        
        [Header("Flags")]
        public GameTileFlags flags = GameTileFlags.Passable;
        
        [Header("Water")]
        public bool hasWater;
        [Range(0f, 10f)] public float waterDepth;
        
        [Header("Environment")]
        [Tooltip("Базовая плотность Ци")]
        [Range(0, 500)] public int baseQiDensity = 100;
        
        [Tooltip("Множитель температуры (0 = норма)")]
        [Range(-200f, 200f)] public float temperatureModifier = 0f; // FIX DAT-C04: Expanded from -50..50 to -200..200 for Lava (2026-04-11)

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public TerrainTypeConfig() { }

        /// <summary>
        /// Конструктор с типом.
        /// </summary>
        public TerrainTypeConfig(TerrainType terrainType)
        {
            type = terrainType;
            SetDefaults(terrainType);
        }

        /// <summary>
        /// Установить значения по умолчанию для типа.
        /// </summary>
        private void SetDefaults(TerrainType terrainType)
        {
            switch (terrainType)
            {
                case TerrainType.None:
                case TerrainType.Void:
                    moveCost = 0f;
                    flags = GameTileFlags.None;
                    break;
                    
                case TerrainType.Grass:
                case TerrainType.Dirt:
                case TerrainType.Stone:
                    moveCost = 1.0f;
                    flags = GameTileFlags.Passable;
                    baseQiDensity = 100;
                    break;
                    
                case TerrainType.Water_Shallow:
                    moveCost = 2.0f;
                    flags = GameTileFlags.Passable | GameTileFlags.Swimable;
                    hasWater = true;
                    waterDepth = 0.5f;
                    baseQiDensity = 150; // Вода накапливает Ци
                    break;
                    
                case TerrainType.Water_Deep:
                    moveCost = 0f;
                    flags = GameTileFlags.Swimable | GameTileFlags.Flyable;
                    hasWater = true;
                    waterDepth = 3f;
                    baseQiDensity = 200;
                    break;
                    
                case TerrainType.Sand:
                    moveCost = 1.2f;
                    flags = GameTileFlags.Passable;
                    baseQiDensity = 80;
                    temperatureModifier = 5f; // Теплее
                    break;
                    
                case TerrainType.Snow:
                    moveCost = 1.5f;
                    flags = GameTileFlags.Passable;
                    baseQiDensity = 50;
                    temperatureModifier = -20f; // Холоднее
                    break;
                    
                case TerrainType.Ice:
                    moveCost = 1.5f;
                    flags = GameTileFlags.Passable | GameTileFlags.Dangerous;
                    baseQiDensity = 60;
                    temperatureModifier = -15f;
                    break;
                    
                case TerrainType.Lava:
                    moveCost = 0f;
                    flags = GameTileFlags.Dangerous | GameTileFlags.Flyable;
                    baseQiDensity = 500; // Огромная плотность Ци
                    temperatureModifier = 1000f;
                    break;
                    
                default:
                    moveCost = 1.0f;
                    flags = GameTileFlags.Passable;
                    break;
            }
        }
    }

    /// <summary>
    /// ScriptableObject конфигурация всех типов поверхности.
    /// Создаётся один раз и используется TileData для определения свойств.
    /// </summary>
    [CreateAssetMenu(fileName = "TerrainConfig", menuName = "Cultivation/Data/Terrain Config")]
    public class TerrainConfig : ScriptableObject
    {
        [Header("Terrain Types Configuration")]
        [SerializeField] private List<TerrainTypeConfig> terrainTypes = new List<TerrainTypeConfig>();

        // Кэш для быстрого доступа
        private Dictionary<TerrainType, TerrainTypeConfig> configCache;

        /// <summary>
        /// Получить конфигурацию для типа поверхности.
        /// </summary>
        public TerrainTypeConfig GetConfig(TerrainType type)
        {
            // Инициализируем кэш если нужно
            if (configCache == null)
            {
                BuildCache();
            }

            if (configCache.TryGetValue(type, out var config))
            {
                return config;
            }

            // Возвращаем конфигурацию по умолчанию
            return GetDefaultConfig(type);
        }

        /// <summary>
        /// Построить кэш из списка.
        /// </summary>
        private void BuildCache()
        {
            configCache = new Dictionary<TerrainType, TerrainTypeConfig>();
            
            foreach (var config in terrainTypes)
            {
                if (!configCache.ContainsKey(config.type))
                {
                    configCache[config.type] = config;
                }
            }
        }

        /// <summary>
        /// Получить конфигурацию по умолчанию для типа.
        /// </summary>
        private TerrainTypeConfig GetDefaultConfig(TerrainType type)
        {
            var config = new TerrainTypeConfig(type);
            
            // Добавляем в кэш
            configCache[type] = config;
            
            return config;
        }

        /// <summary>
        /// Инициализировать все типы поверхности.
        /// </summary>
        [ContextMenu("Initialize All Terrain Types")]
        public void InitializeAllTypes()
        {
            terrainTypes.Clear();
            
            foreach (TerrainType type in Enum.GetValues(typeof(TerrainType)))
            {
                terrainTypes.Add(new TerrainTypeConfig(type));
            }
            
            configCache = null; // Сбросить кэш
        }

        #region Static Access

        private static TerrainConfig _instance;

        // FIX DAT-M03: Reset singleton on domain reload for clean play mode (2026-04-11)
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            _instance = null;
        }

        /// <summary>
        /// Экземпляр конфигурации по умолчанию.
        /// Если не назначен через Resources, создаётся программно.
        /// </summary>
        public static TerrainConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Пробуем загрузить из Resources
                    _instance = Resources.Load<TerrainConfig>("TerrainConfig");
                    
                    // Если нет — создаём временный
                    if (_instance == null)
                    {
                        _instance = CreateInstance<TerrainConfig>();
                        _instance.InitializeAllTypes();
                        Debug.Log("[TerrainConfig] Created runtime instance with defaults");
                    }
                }
                
                return _instance;
            }
        }

        /// <summary>
        /// Статический метод для получения конфигурации типа.
        /// </summary>
        public static TerrainTypeConfig GetTerrainConfig(TerrainType type)
        {
            return Instance.GetConfig(type);
        }

        #endregion
    }
}
