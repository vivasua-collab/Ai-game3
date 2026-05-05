// ============================================================================
// GameSettings.cs — Настройки игры
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-03-31 09:54:21 UTC
// ============================================================================

using UnityEngine;

namespace CultivationGame.Core
{
    /// <summary>
    /// Глобальные настройки игры.
    /// Создаётся как ScriptableObject: Assets/ScriptableObjects/Config/GameSettings.asset
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Cultivation/Config/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Game Info")]
        [Tooltip("Название игры")]
        public string gameName = "Cultivation World Simulator";
        
        [Tooltip("Версия игры")]
        public string version = "0.1.0";
        
        [Header("Time Settings")]
        [Tooltip("Начальный год")]
        public int startYear = 1;
        
        [Tooltip("Начальный месяц (1-12)")]
        [Range(1, 12)]
        public int startMonth = 1;
        
        [Tooltip("Начальный день (1-30)")]
        [Range(1, 30)]
        public int startDay = 1;
        
        [Tooltip("Начальный час (0-23)")]
        [Range(0, 23)]
        public int startHour = 8;
        
        [Tooltip("Скорость времени по умолчанию")]
        public TimeSpeed defaultTimeSpeed = TimeSpeed.Normal;
        
        [Header("Player Settings")]
        [Tooltip("Базовое здоровье игрока")]
        public int basePlayerHealth = 100;
        
        [Tooltip("Базовое Ци игрока")]
        public long basePlayerQi = 1000; // FIX Н-11: 100→1000 (соответствует Constants.BASE_CORE_CAPACITY, GLOSSARY.md)
        
        [Tooltip("Начальный уровень культивации")]
        [Range(1, 10)]
        public int startCultivationLevel = 1;
        
        [Tooltip("Начальный под-уровень")]
        [Range(0, 9)]
        public int startCultivationSubLevel = 0;
        
        [Header("Combat Settings")]
        [Tooltip("Использовать подавление уровнем")]
        public bool useLevelSuppression = true;
        
        [Tooltip("Максимальная разница уровней для атаки")]
        [Range(1, 10)]
        public int maxLevelDifferenceForAttack = 5;
        
        [Tooltip("Использовать Qi Buffer")]
        public bool useQiBuffer = true;
        
        [Tooltip("Использовать Kenshi-style повреждения")]
        public bool useKenshiDamage = true;
        
        [Header("NPC Settings")]
        [Tooltip("Максимальное количество NPC в сцене")]
        [Range(10, 200)]
        public int maxNPCsInScene = 50;
        
        [Tooltip("Радиус деактивации NPC")]
        public float npcDeactivationDistance = 50f;
        
        [Tooltip("Интервал обновления AI (сек)")]
        [Range(0.1f, 2f)]
        public float aiUpdateInterval = 0.5f;
        
        [Header("Save Settings")]
        [Tooltip("Автосохранение")]
        public bool autoSave = true;
        
        [Tooltip("Интервал автосохранения (минуты игрового времени)")]
        [Range(1, 60)]
        public int autoSaveIntervalMinutes = 60;
        
        [Tooltip("Количество слотов сохранений")]
        [Range(1, 10)]
        public int maxSaveSlots = 5;
        
        [Header("UI Settings")]
        [Tooltip("Показывать HUD")]
        public bool showHUD = true;
        
        [Tooltip("Показывать миникарту")]
        public bool showMinimap = false;
        
        [Tooltip("Размер миникарты")]
        [Range(100, 300)]
        public int minimapSize = 200;
        
        [Header("Debug Settings")]
        [Tooltip("Режим отладки")]
        public bool debugMode = false;
        
        [Tooltip("Показывать Gizmos")]
        public bool showGizmos = true;
        
        [Tooltip("Логировать боевые действия")]
        public bool logCombat = false;
        
        [Tooltip("Бессмертие игрока (для тестов)")]
        public bool godMode = false;
    }
    
    /// <summary>
    /// Настройки звука.
    /// Создаётся как ScriptableObject: Assets/ScriptableObjects/Config/AudioSettings.asset
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "Cultivation/Config/Audio Settings")]
    public class AudioSettings : ScriptableObject
    {
        [Header("Volume")]
        [Range(0f, 1f)]
        public float masterVolume = 1f;
        
        [Range(0f, 1f)]
        public float musicVolume = 0.7f;
        
        [Range(0f, 1f)]
        public float sfxVolume = 0.8f;
        
        [Range(0f, 1f)]
        public float ambientVolume = 0.5f;
        
        [Header("Music")]
        public bool loopMusic = true;
        
        [Range(0f, 5f)]
        public float musicFadeTime = 2f;
    }
    
    /// <summary>
    /// Настройки графики.
    /// Создаётся как ScriptableObject: Assets/ScriptableObjects/Config/GraphicsSettings.asset
    /// </summary>
    [CreateAssetMenu(fileName = "GraphicsSettings", menuName = "Cultivation/Config/Graphics Settings")]
    public class GraphicsSettings : ScriptableObject
    {
        [Header("Resolution")]
        public int defaultWidth = 1920;
        public int defaultHeight = 1080;
        public bool fullscreen = false;
        public bool vsync = true;
        
        [Header("Quality")]
        public int qualityLevel = 2;
        public int targetFrameRate = 60;
        
        [Header("Effects")]
        public bool postProcessing = true;
        public bool particles = true;
        public int maxParticleCount = 1000;
    }
}
