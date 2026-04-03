// ============================================================================
// GameInitializer.cs — Инициализация игры при старте
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-04-01 13:03:39 UTC
// ============================================================================

using System;
using System.Collections;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Player;
using CultivationGame.Qi;
using CultivationGame.Body;
using CultivationGame.World;
using CultivationGame.Save;

namespace CultivationGame.Managers
{
    /// <summary>
    /// Инициализатор игры — отвечает за стартовую настройку всех систем.
    /// 
    /// Запускается автоматически при старте сцены.
    /// Координирует порядок инициализации систем.
    /// </summary>
    public class GameInitializer : MonoBehaviour
    {
        #region Configuration
        
        [Header("Initialization Order")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private float initializationDelay = 0.1f;
        
        [Header("Debug")]
        [SerializeField] private bool debugMode = false;
        [SerializeField] private bool logInitializationSteps = true;
        
        #endregion
        
        #region Runtime
        
        private bool isInitialized = false;
        private int systemsInitialized = 0;
        private int totalSystems = 8;
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Инициализация началась.
        /// </summary>
        public event Action OnInitializationStart;
        
        /// <summary>
        /// Система инициализирована. (systemName)
        /// </summary>
        public event Action<string> OnSystemInitialized;
        
        /// <summary>
        /// Инициализация завершена.
        /// </summary>
        public event Action OnInitializationComplete;
        
        /// <summary>
        /// Ошибка инициализации. (systemName, error)
        /// </summary>
        public event Action<string, string> OnInitializationError;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Игра инициализирована.
        /// </summary>
        public bool IsInitialized => isInitialized;
        
        /// <summary>
        /// Прогресс инициализации (0-1).
        /// </summary>
        public float Progress => (float)systemsInitialized / totalSystems;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Start()
        {
            if (initializeOnStart)
            {
                StartCoroutine(InitializeGameAsync());
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Запустить инициализацию вручную.
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[GameInitializer] Already initialized");
                return;
            }
            
            StartCoroutine(InitializeGameAsync());
        }
        
        /// <summary>
        /// Переинициализировать игру (например, после загрузки).
        /// </summary>
        public void Reinitialize()
        {
            isInitialized = false;
            systemsInitialized = 0;
            StartCoroutine(InitializeGameAsync());
        }
        
        #endregion
        
        #region Initialization Sequence
        
        private IEnumerator InitializeGameAsync()
        {
            Log("[GameInitializer] Starting initialization...");
            OnInitializationStart?.Invoke();
            
            yield return new WaitForSeconds(initializationDelay);
            
            // 1. GameEvents
            yield return InitializeSystem("GameEvents", InitializeGameEvents);
            
            // 2. GameManager
            yield return InitializeSystem("GameManager", InitializeGameManager);
            
            // 3. Time System
            yield return InitializeSystem("TimeSystem", InitializeTimeSystem);
            
            // 4. World System
            yield return InitializeSystem("WorldSystem", InitializeWorldSystem);
            
            // 5. Save System
            yield return InitializeSystem("SaveSystem", InitializeSaveSystem);
            
            // 6. Player Systems
            yield return InitializeSystem("PlayerSystems", InitializePlayerSystems);
            
            // 7. UI System
            yield return InitializeSystem("UISystem", InitializeUISystem);
            
            // 8. Final Setup
            yield return InitializeSystem("FinalSetup", FinalSetup);
            
            isInitialized = true;
            
            Log("[GameInitializer] ✅ Initialization complete!");
            OnInitializationComplete?.Invoke();
            
            // Триггерим событие старта игры
            GameEvents.TriggerGameStart();
            
            // Устанавливаем состояние
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }
        }
        
        private IEnumerator InitializeSystem(string systemName, Func<IEnumerator> initMethod)
        {
            try
            {
                yield return initMethod();
                systemsInitialized++;
                OnSystemInitialized?.Invoke(systemName);
                Log($"[GameInitializer] ✓ {systemName} initialized ({systemsInitialized}/{totalSystems})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameInitializer] ✗ {systemName} failed: {ex.Message}");
                OnInitializationError?.Invoke(systemName, ex.Message);
            }
        }
        
        #endregion
        
        #region System Initialization Methods
        
        private IEnumerator InitializeGameEvents()
        {
            // Включаем логирование событий в debug режиме
            GameEvents.DebugLogging = debugMode;
            yield return null;
        }
        
        private IEnumerator InitializeGameManager()
        {
            var gameManager = GameManager.Instance;
            
            if (gameManager == null)
            {
                // Пытаемся найти
                gameManager = FindFirstObjectByType<GameManager>();
                
                if (gameManager == null)
                {
                    Debug.LogWarning("[GameInitializer] GameManager not found, some features may not work");
                }
            }
            
            yield return null;
        }
        
        private IEnumerator InitializeTimeSystem()
        {
            var timeController = FindFirstObjectByType<TimeController>();
            
            if (timeController != null)
            {
                // Подписываемся на события времени
                GameEvents.OnTimeSpeedChanged += (speed) =>
                {
                    if (debugMode) Debug.Log($"[GameInitializer] Time speed changed: {speed}");
                };
            }
            else
            {
                Debug.LogWarning("[GameInitializer] TimeController not found");
            }
            
            yield return null;
        }
        
        private IEnumerator InitializeWorldSystem()
        {
            var worldController = FindFirstObjectByType<WorldController>();
            
            if (worldController != null)
            {
                // Мир инициализируется автоматически
            }
            else
            {
                Debug.LogWarning("[GameInitializer] WorldController not found");
            }
            
            yield return null;
        }
        
        private IEnumerator InitializeSaveSystem()
        {
            var saveManager = FindFirstObjectByType<SaveManager>();
            
            if (saveManager != null)
            {
                // Подписываемся на события сохранения
                GameEvents.OnGameSaving += (slot) =>
                {
                    if (debugMode) Debug.Log($"[GameInitializer] Saving to slot {slot}");
                };
                
                GameEvents.OnGameLoaded += (slot) =>
                {
                    if (debugMode) Debug.Log($"[GameInitializer] Loaded from slot {slot}");
                    // Переинициализируем после загрузки
                    Reinitialize();
                };
            }
            else
            {
                Debug.LogWarning("[GameInitializer] SaveManager not found");
            }
            
            yield return null;
        }
        
        private IEnumerator InitializePlayerSystems()
        {
            var player = FindFirstObjectByType<PlayerController>();
            
            if (player != null)
            {
                var bodyController = player.GetComponent<BodyController>();
                var qiController = player.GetComponent<QiController>();
                
                // Подписываемся на события игрока
                if (bodyController != null)
                {
                    bodyController.OnDamageTaken += (part, result) =>
                    {
                        GameEvents.TriggerDamageTaken((int)result.DamageDealt, part.PartType.ToString());
                    };
                    
                    bodyController.OnDeath += () =>
                    {
                        GameEvents.TriggerPlayerDeath();
                    };
                }
                
                if (qiController != null)
                {
                    qiController.OnQiChanged += (current, max) =>
                    {
                        GameEvents.TriggerPlayerQiChanged(current, max);
                    };
                    
                    qiController.OnCultivationLevelChanged += (level) =>
                    {
                        GameEvents.TriggerPlayerCultivationLevelChanged(level);
                    };
                }
                
                Log($"[GameInitializer] Player initialized: {player.PlayerName}");
            }
            else
            {
                Debug.LogWarning("[GameInitializer] PlayerController not found");
            }
            
            yield return null;
        }
        
        private IEnumerator InitializeUISystem()
        {
            // UI система инициализируется автоматически через HUDController
            yield return null;
        }
        
        private IEnumerator FinalSetup()
        {
            // Финальная настройка после всех систем
            
            // Проверяем критические системы
            bool hasPlayer = FindFirstObjectByType<PlayerController>() != null;
            bool hasGameManager = GameManager.Instance != null;
            bool hasTimeSystem = FindFirstObjectByType<TimeController>() != null;
            
            if (!hasPlayer || !hasGameManager || !hasTimeSystem)
            {
                Debug.LogWarning($"[GameInitializer] Missing critical systems: " +
                    $"Player={hasPlayer}, GameManager={hasGameManager}, Time={hasTimeSystem}");
            }
            
            yield return null;
        }
        
        #endregion
        
        #region Helpers
        
        private void Log(string message)
        {
            if (logInitializationSteps)
            {
                Debug.Log(message);
            }
        }
        
        #endregion
    }
}
