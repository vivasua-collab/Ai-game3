// ============================================================================
// GameManager.cs — Главный менеджер игры
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-31 10:00:00 UTC
// Изменён: 2026-04-01 13:03:39 UTC
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.World;
using CultivationGame.Player;
using CultivationGame.UI;

namespace CultivationGame.Managers
{
    /// <summary>
    /// Главный менеджер игры — координирует все системы.
    /// 
    /// Ответственности:
    /// - Управление состоянием игры (Menu, Playing, Paused)
    /// - Инициализация всех систем при старте
    /// - Координация между системами
    /// - Глобальные события игры
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        #region Singleton

        public static GameManager Instance { get; private set; }

        #endregion

        #region Serialized Fields

        [Header("Config")]
        [SerializeField] private bool initializeOnStart = true;
        [SerializeField] private bool debugMode = false;

        [Header("References")]
        [SerializeField] private WorldController worldController;
        [SerializeField] private TimeController timeController;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private UIManager uiManager;

        #endregion

        #region State

        private GameState currentState = GameState.MainMenu;
        private bool isInitialized = false;
        private float gameTime = 0f;

        #endregion

        #region Events

        /// <summary>Игра инициализирована</summary>
        public event Action OnGameInitialized;

        /// <summary>Состояние игры изменилось</summary>
        public event Action<GameState> OnStateChanged;

        /// <summary>Игра началась</summary>
        public event Action OnGameStart;

        /// <summary>Игра поставлена на паузу</summary>
        public event Action OnGamePause;

        /// <summary>Игра resumed</summary>
        public event Action OnGameResume;

        /// <summary>Игра завершена</summary>
        public event Action OnGameEnd;

        #endregion

        #region Properties

        public GameState CurrentState => currentState;
        public bool IsPaused => currentState == GameState.Paused;
        public bool IsPlaying => currentState == GameState.Playing;
        public bool IsInitialized => isInitialized;
        public WorldController World => worldController;
        public TimeController Time => timeController;
        public PlayerController Player => playerController;
        public float GameTime => gameTime;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Find references if not set
            FindReferences();
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                InitializeGame();
            }
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                gameTime += UnityEngine.Time.deltaTime;
                ProcessInput();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Initialization

        private void FindReferences()
        {
            if (worldController == null)
                worldController = FindFirstObjectByType<WorldController>();
            if (timeController == null)
                timeController = FindFirstObjectByType<TimeController>();
            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>();
            if (uiManager == null)
                uiManager = FindFirstObjectByType<UIManager>();
        }

        /// <summary>
        /// Инициализировать игру.
        /// </summary>
        public void InitializeGame()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[GameManager] Game already initialized!");
                return;
            }

            Log("Initializing game...");

            // Validate references
            ValidateReferences();

            // Initialize systems in order
            InitializeSystems();

            isInitialized = true;
            OnGameInitialized?.Invoke();

            Log("Game initialized successfully!");
        }

        private void ValidateReferences()
        {
            if (worldController == null)
                Debug.LogWarning("[GameManager] WorldController not found!");
            if (timeController == null)
                Debug.LogWarning("[GameManager] TimeController not found!");
            if (playerController == null)
                Debug.LogWarning("[GameManager] PlayerController not found!");
        }

        private void InitializeSystems()
        {
            // World is initialized automatically in WorldController.Start()
            // Time is initialized automatically in TimeController

            // Subscribe to events
            if (worldController != null)
            {
                worldController.OnWorldInitialized += OnWorldInitialized;
            }

            if (playerController != null)
            {
                playerController.OnPlayerDeath += OnPlayerDeath;
            }
        }

        #endregion

        #region State Management

        /// <summary>
        /// Установить состояние игры.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (currentState == newState)
                return;

            GameState oldState = currentState;
            currentState = newState;

            Log($"State changed: {oldState} → {newState}");

            // Handle state transitions
            switch (newState)
            {
                case GameState.MainMenu:
                    HandleMainMenu();
                    break;
                case GameState.Loading:
                    HandleLoading();
                    break;
                case GameState.Playing:
                    HandlePlaying();
                    break;
                case GameState.Paused:
                    HandlePaused();
                    break;
                case GameState.Cutscene:
                    HandleCutscene();
                    break;
            }

            OnStateChanged?.Invoke(newState);
        }

        private void HandleMainMenu()
        {
            UnityEngine.Time.timeScale = 0f;
            if (timeController != null)
                timeController.Pause();
        }

        private void HandleLoading()
        {
            UnityEngine.Time.timeScale = 0f;
        }

        private void HandlePlaying()
        {
            UnityEngine.Time.timeScale = 1f;
            if (timeController != null)
                timeController.Resume();

            if (currentState != GameState.Paused)
            {
                OnGameStart?.Invoke();
            }
            else
            {
                OnGameResume?.Invoke();
            }
        }

        private void HandlePaused()
        {
            UnityEngine.Time.timeScale = 0f;
            if (timeController != null)
                timeController.Pause();

            OnGamePause?.Invoke();
        }

        private void HandleCutscene()
        {
            // Cutscene logic
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Начать новую игру.
        /// </summary>
        public void StartNewGame()
        {
            SetState(GameState.Loading);

            // Reset game time
            gameTime = 0f;

            // Initialize world
            if (worldController != null && !worldController.IsInitialized)
            {
                // World will initialize in its Start()
            }

            // Set player start position
            if (playerController != null)
            {
                playerController.transform.position = Vector3.zero;
            }

            SetState(GameState.Playing);
        }

        /// <summary>
        /// Загрузить игру.
        /// </summary>
        public void LoadGame(string saveName)
        {
            SetState(GameState.Loading);

            // Save/Load system will handle this
            // For now just start playing
            SetState(GameState.Playing);
        }

        /// <summary>
        /// Поставить на паузу.
        /// </summary>
        public void Pause()
        {
            if (currentState == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
        }

        /// <summary>
        /// Продолжить игру.
        /// </summary>
        public void Resume()
        {
            if (currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        /// <summary>
        /// Переключить паузу.
        /// </summary>
        public void TogglePause()
        {
            if (currentState == GameState.Playing)
                Pause();
            else if (currentState == GameState.Paused)
                Resume();
        }

        /// <summary>
        /// Завершить игру.
        /// </summary>
        public void EndGame()
        {
            SetState(GameState.MainMenu);
            OnGameEnd?.Invoke();
        }

        /// <summary>
        /// Выйти из игры.
        /// </summary>
        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion

        #region Input Processing

        private void ProcessInput()
        {
            // Escape - Pause
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }

            // F5 - Quick Save
            if (Input.GetKeyDown(KeyCode.F5))
            {
                QuickSave();
            }

            // F9 - Quick Load
            if (Input.GetKeyDown(KeyCode.F9))
            {
                QuickLoad();
            }
        }

        #endregion

        #region Save/Load

        private void QuickSave()
        {
            Log("Quick Save...");
            // SaveManager will handle this
        }

        private void QuickLoad()
        {
            Log("Quick Load...");
            // SaveManager will handle this
        }

        #endregion

        #region Event Handlers

        private void OnWorldInitialized()
        {
            Log("World initialized callback");
        }

        private void OnPlayerDeath()
        {
            Log("Player died!");
            // Handle player death
        }

        #endregion

        #region Utility

        private void Log(string message)
        {
            if (debugMode)
            {
                Debug.Log($"[GameManager] {message}");
            }
        }

        #endregion
    }
}
