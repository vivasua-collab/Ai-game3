// ============================================================================
// GameInitializer.cs — Инициализация игры при старте
// Cultivation World Simulator
// Версия: 1.1 — Исправлены проблемы с event subscriptions
// ============================================================================
// Создан: 2026-04-01 13:03:39 UTC
// Редактировано: 2026-04-13 12:05:15 UTC — FIX: WaitForSeconds→WaitForSecondsRealtime (timeScale deadlock)
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 1.1:
// - FIX: Lambda handlers заменены на named methods
// - FIX: Добавлена отписка от событий в OnDestroy
// - FIX: Добавлен флаг isSubscribed для защиты от дублирования подписок
// - FIX: Убран Reinitialize() из OnGameLoaded (cascading reinitialization bug)
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
    /// 
    /// ВАЖНО: Подписки на события используют named methods для возможности отписки.
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
        private bool isInitializing = false; // FIX MGR-C01: Guard against parallel InitializeGameAsync (2026-04-11)
        private bool isSubscribed = false;  // FIX: Флаг для защиты от дублирования подписок
        private int systemsInitialized = 0;
        private int totalSystems = 8;
        
        // Cached references for event handlers
        private BodyController cachedBodyController;
        private QiController cachedQiController;
        
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
        
        private void OnDestroy()
        {
            // FIX: Отписываемся от всех событий
            UnsubscribeFromEvents();
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
            
            // FIX MGR-C01: Also check isInitializing to prevent parallel init (2026-04-11)
            if (isInitializing)
            {
                Debug.LogWarning("[GameInitializer] Already initializing");
                return;
            }
            
            StartCoroutine(InitializeGameAsync());
        }
        
        /// <summary>
        /// Переинициализировать игру (например, после загрузки).
        /// ВНИМАНИЕ: Не вызывать из подписок на GameEvents!
        /// </summary>
        public void Reinitialize()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("[GameInitializer] Not initialized, nothing to reinitialize");
                return;
            }
            
            // FIX MGR-C01: Guard against parallel reinitialize (2026-04-11)
            if (isInitializing)
            {
                Debug.LogWarning("[GameInitializer] Already initializing, cannot reinitialize");
                return;
            }
            
            // Сначала отписываемся от старых событий
            UnsubscribeFromEvents();
            
            isInitialized = false;
            systemsInitialized = 0;
            StartCoroutine(InitializeGameAsync());
        }
        
        #endregion
        
        #region Initialization Sequence
        
        private IEnumerator InitializeGameAsync()
        {
            // FIX MGR-C01: Prevent parallel initialization (2026-04-11)
            if (isInitializing)
            {
                Debug.LogWarning("[GameInitializer] Already initializing!");
                yield break;
            }
            isInitializing = true;
            
            Log("[GameInitializer] Starting initialization...");
            OnInitializationStart?.Invoke();
            
            // FIX: WaitForSecondsRealtime — не зависит от timeScale.
            // UIManager.Start() ставит timeScale=0 (MainMenu), из-за чего
            // WaitForSeconds зависает навсегда → игра не инициализируется.
            yield return new WaitForSecondsRealtime(initializationDelay);
            
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
            isInitializing = false; // FIX MGR-C01: Clear flag on completion (2026-04-11)
            
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
            IEnumerator coroutine = null;
            try
            {
                coroutine = initMethod();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameInitializer] ✗ {systemName} failed to start: {ex.Message}");
                OnInitializationError?.Invoke(systemName, ex.Message);
                yield break;
            }
            
            yield return coroutine;
            
            systemsInitialized++;
            OnSystemInitialized?.Invoke(systemName);
            Log($"[GameInitializer] ✓ {systemName} initialized ({systemsInitialized}/{totalSystems})");
        }
        
        #endregion
        
        #region System Initialization Methods
        
        private IEnumerator InitializeGameEvents()
        {
            GameEvents.DebugLogging = debugMode;
            yield return null;
        }
        
        private IEnumerator InitializeGameManager()
        {
            var gameManager = GameManager.Instance;
            
            if (gameManager == null)
            {
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
            // FIX: Проверяем, уже ли зарегистрирован в ServiceLocator (Awake регистрирует раньше)
            // Редактировано: 2026-04-16 14:35:00 UTC
            if (!ServiceLocator.IsRegistered<TimeController>())
            {
                var timeController = FindFirstObjectByType<TimeController>();
                
                if (timeController != null)
                {
                    ServiceLocator.Register(timeController);
                }
                else
                {
                    Debug.LogWarning("[GameInitializer] TimeController not found");
                }
            }
            
            yield return null;
        }
        
        private IEnumerator InitializeWorldSystem()
        {
            // FIX: Проверяем, уже ли зарегистрирован в ServiceLocator (Awake регистрирует раньше)
            // Редактировано: 2026-04-16 14:35:00 UTC
            if (!ServiceLocator.IsRegistered<WorldController>())
            {
                var worldController = FindFirstObjectByType<WorldController>();
                
                if (worldController != null)
                {
                    ServiceLocator.Register(worldController);
                }
                else
                {
                    Debug.LogWarning("[GameInitializer] WorldController not found");
                }
            }
            
            yield return null;
        }
        
        private IEnumerator InitializeSaveSystem()
        {
            var saveManager = FindFirstObjectByType<SaveManager>();
            
            if (saveManager != null)
            {
                // FIX MGR-H02: SubscribeToSaveEvents handled by SubscribeToEvents() below (2026-04-11)
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
                cachedBodyController = player.GetComponent<BodyController>();
                cachedQiController = player.GetComponent<QiController>();
                
                // FIX MGR-H02: SubscribeToPlayerEvents handled by SubscribeToEvents() below (2026-04-11)
                
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
            // FIX MGR-H02: Subscribe to ALL events via unified method (2026-04-11)
            // This replaces separate SubscribeToSaveEvents/SubscribeToPlayerEvents calls
            SubscribeToEvents();
            
            // Финальная настройка после всех систем
            
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
        
        #region Event Subscription Management
        
        /// <summary>
        /// Подписаться на все события (с защитой от дублирования).
        /// </summary>
        private void SubscribeToEvents()
        {
            if (isSubscribed) return;
            
            SubscribeToSaveEvents();
            SubscribeToPlayerEvents();
            SubscribeToTimeEvents();
            
            isSubscribed = true;
        }
        
        /// <summary>
        /// Отписаться от всех событий.
        /// </summary>
        private void UnsubscribeFromEvents()
        {
            if (!isSubscribed) return;
            
            // Отписываемся от GameEvents
            GameEvents.OnGameSaving -= HandleGameSaving;
            GameEvents.OnGameLoaded -= HandleGameLoaded;
            GameEvents.OnTimeSpeedChanged -= HandleTimeSpeedChanged;
            
            // Отписываемся от Player events
            if (cachedBodyController != null)
            {
                cachedBodyController.OnDamageTaken -= HandleBodyDamageTaken;
                cachedBodyController.OnDeath -= HandlePlayerDeath;
            }
            
            if (cachedQiController != null)
            {
                cachedQiController.OnQiChanged -= HandleQiChanged;
                cachedQiController.OnCultivationLevelChanged -= HandleCultivationLevelChanged;
            }
            
            isSubscribed = false;
        }
        
        // NOTE MGR-H05: Individual Subscribe methods (SubscribeToSaveEvents, SubscribeToPlayerEvents,
        // SubscribeToTimeEvents) are called from SubscribeToEvents() which already checks isSubscribed.
        // No additional isSubscribed checks needed in these individual methods. (2026-04-11)
        
        private void SubscribeToSaveEvents()
        {
            GameEvents.OnGameSaving += HandleGameSaving;
            GameEvents.OnGameLoaded += HandleGameLoaded;
        }
        
        private void SubscribeToPlayerEvents()
        {
            if (cachedBodyController != null)
            {
                cachedBodyController.OnDamageTaken += HandleBodyDamageTaken;
                cachedBodyController.OnDeath += HandlePlayerDeath;
            }
            
            if (cachedQiController != null)
            {
                cachedQiController.OnQiChanged += HandleQiChanged;
                cachedQiController.OnCultivationLevelChanged += HandleCultivationLevelChanged;
            }
        }
        
        private void SubscribeToTimeEvents()
        {
            GameEvents.OnTimeSpeedChanged += HandleTimeSpeedChanged;
        }
        
        #endregion
        
        #region Named Event Handlers
        
        private void HandleGameSaving(int slot)
        {
            if (debugMode) Debug.Log($"[GameInitializer] Saving to slot {slot}");
        }
        
        private void HandleGameLoaded(int slot)
        {
            if (debugMode) Debug.Log($"[GameInitializer] Loaded from slot {slot}");
            
            // FIX: НЕ вызываем Reinitialize() здесь!
            // Это вызывает cascading reinitialization bug.
            // Вместо этого обновляем только то, что нужно.
            // Reinitialize() должен вызываться явно из SaveManager после применения данных.
        }
        
        private void HandleTimeSpeedChanged(TimeSpeed speed)
        {
            if (debugMode) Debug.Log($"[GameInitializer] Time speed changed: {speed}");
        }
        
        private void HandleBodyDamageTaken(BodyPart part, BodyDamageResult result)
        {
            int totalDamage = (int)(result.RedHPDamage + result.BlackHPDamage);
            GameEvents.TriggerDamageTaken(totalDamage, part.PartType.ToString());
        }
        
        private void HandlePlayerDeath()
        {
            GameEvents.TriggerPlayerDeath();
        }
        
        private void HandleQiChanged(long current, long max)
        {
            GameEvents.TriggerPlayerQiChanged(current, max);
        }
        
        private void HandleCultivationLevelChanged(int level)
        {
            GameEvents.TriggerPlayerCultivationLevelChanged(level);
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
