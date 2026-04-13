// ============================================================================
// UIManager.cs — Главный менеджер UI
// Cultivation World Simulator
// Версия: 1.1 — Исправлен баг начального состояния
// ============================================================================
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-13 10:34:08 UTC — замена UnityEngine.Input на Input System
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 1.1:
// - FIX: currentState инициализируется как None (sentinel) для первого запуска
// - FIX: Добавлен forceInitialSync для принудительной синхронизации панелей
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CultivationGame.Core;

namespace CultivationGame.UI
{
    /// <summary>
    /// Главный менеджер UI — управляет всеми UI панелями и экранами.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private GameObject hudPanel;
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject dialogPanel;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject characterPanel;
        [SerializeField] private GameObject mapPanel;
        
        [Header("Config")]
        [SerializeField] private bool pauseOnMenu = true;
        [SerializeField] private float fadeInDuration = 0.3f;
        [SerializeField] private float fadeOutDuration = 0.2f;
        
        // === State ===
        private GameState currentState = GameState.None;  // FIX: Sentinel state для первого запуска
        private Stack<GameState> stateHistory = new Stack<GameState>();
        private Dictionary<GameState, GameObject> panels = new Dictionary<GameState, GameObject>();
        private bool isTransitioning = false;
        private bool hasInitialized = false;  // FIX: Флаг для отслеживания первой инициализации
        
        // === References ===
        private HUDController hudController;
        private DialogUI dialogUI;
        
        // === Events ===
        public event Action<GameState, GameState> OnStateChanged;   // newState, oldState
        public event Action OnPause;
        public event Action OnResume;
        public event Action OnMenuOpened;
        public event Action OnMenuClosed;
        
        // === Properties ===
        public GameState CurrentState => currentState;
        public bool IsPaused => currentState == GameState.Paused;
        public bool IsInDialog => currentState == GameState.Dialog;
        public bool IsInMenu => currentState != GameState.Playing;
        public HUDController HUD => hudController;
        
        // === Singleton ===
        public static UIManager Instance { get; private set; }
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            InitializePanels();
            GetReferences();
        }
        
        private void Start()
        {
            // FIX: Принудительная начальная синхронизация
            ForceInitialSync();
        }
        
        private void Update()
        {
            HandleInput();
        }
        
        // === Initialization ===
        
        private void InitializePanels()
        {
            panels[GameState.MainMenu] = mainMenu;
            panels[GameState.Playing] = hudPanel;
            panels[GameState.Paused] = pauseMenu;
            panels[GameState.Dialog] = dialogPanel;
            panels[GameState.Inventory] = inventoryPanel;
            panels[GameState.Settings] = null; // Обрабатывается отдельно
            
            // Изначально скрываем все панели
            foreach (var panel in panels.Values)
            {
                if (panel != null)
                {
                    panel.SetActive(false);
                }
            }
        }
        
        private void GetReferences()
        {
            if (hudPanel != null)
            {
                hudController = hudPanel.GetComponent<HUDController>();
            }
            if (dialogPanel != null)
            {
                dialogUI = dialogPanel.GetComponent<DialogUI>();
            }
        }
        
        // === State Management ===
        
        /// <summary>
        /// Установить состояние UI.
        /// </summary>
        public void SetState(GameState newState)
        {
            if (isTransitioning || currentState == newState) return;
            
            GameState oldState = currentState;
            
            // Сохраняем историю для возврата
            if (oldState == GameState.Playing || oldState == GameState.Dialog)
            {
                stateHistory.Push(oldState);
            }
            
            currentState = newState;
            
            // Обновляем панели
            UpdatePanels(oldState, newState);
            
            // Пауза игры
            if (pauseOnMenu)
            {
                if (newState != GameState.Playing && newState != GameState.Dialog)
                {
                    Time.timeScale = 0f;
                    OnPause?.Invoke();
                }
                else if (oldState != GameState.Playing && oldState != GameState.Dialog)
                {
                    Time.timeScale = 1f;
                    OnResume?.Invoke();
                }
            }
            
            OnStateChanged?.Invoke(newState, oldState);
            
            if (newState != GameState.Playing)
            {
                OnMenuOpened?.Invoke();
            }
            else
            {
                OnMenuClosed?.Invoke();
            }
        }
        
        /// <summary>
        /// FIX: Принудительная начальная синхронизация панелей.
        /// Вызывается один раз при старте для отображения MainMenu.
        /// </summary>
        private void ForceInitialSync()
        {
            if (hasInitialized) return;
            
            hasInitialized = true;
            
            // Показываем MainMenu
            if (panels.TryGetValue(GameState.MainMenu, out GameObject mainMenuPanel) && mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(true);
            }
            
            currentState = GameState.MainMenu;
            
            // Пауза на главном меню
            if (pauseOnMenu)
            {
                Time.timeScale = 0f;
            }
            
            OnMenuOpened?.Invoke();
        }
        
        private void UpdatePanels(GameState oldState, GameState newState)
        {
            // Скрываем старую панель
            if (panels.TryGetValue(oldState, out GameObject oldPanel) && oldPanel != null)
            {
                oldPanel.SetActive(false);
            }
            
            // Показываем новую панель
            if (panels.TryGetValue(newState, out GameObject newPanel) && newPanel != null)
            {
                newPanel.SetActive(true);
            }
        }
        
        /// <summary>
        /// Вернуться к предыдущему состоянию.
        /// </summary>
        public void ReturnToPrevious()
        {
            if (stateHistory.Count > 0)
            {
                GameState previousState = stateHistory.Pop();
                SetState(previousState);
            }
            else
            {
                SetState(GameState.Playing);
            }
        }
        
        /// <summary>
        /// Переключить состояние паузы.
        /// </summary>
        public void TogglePause()
        {
            if (currentState == GameState.Playing)
            {
                SetState(GameState.Paused);
            }
            else if (currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }
        
        // === Input Handling ===
        
        private void HandleInput()
        {
            if (Keyboard.current == null) return;

            // ESC — пауза или возврат
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HandleEscape();
            }

            // I — инвентарь
            if (Keyboard.current.iKey.wasPressedThisFrame)
            {
                ToggleInventory();
            }

            // C — персонаж
            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                ToggleCharacter();
            }

            // M — карта
            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                ToggleMap();
            }
        }
        
        private void HandleEscape()
        {
            switch (currentState)
            {
                case GameState.Playing:
                    SetState(GameState.Paused);
                    break;
                case GameState.Paused:
                    SetState(GameState.Playing);
                    break;
                case GameState.Dialog:
                    // Не закрываем диалог через ESC
                    break;
                default:
                    ReturnToPrevious();
                    break;
            }
        }
        
        // === Menu Operations ===
        
        /// <summary>
        /// Открыть главное меню.
        /// </summary>
        public void OpenMainMenu()
        {
            SetState(GameState.MainMenu);
        }
        
        /// <summary>
        /// Начать новую игру.
        /// </summary>
        public void StartNewGame()
        {
            stateHistory.Clear();
            SetState(GameState.Playing);
        }
        
        /// <summary>
        /// Открыть инвентарь.
        /// </summary>
        public void OpenInventory()
        {
            SetState(GameState.Inventory);
        }
        
        /// <summary>
        /// Переключить инвентарь.
        /// </summary>
        public void ToggleInventory()
        {
            if (currentState == GameState.Inventory)
            {
                ReturnToPrevious();
            }
            else if (currentState == GameState.Playing)
            {
                SetState(GameState.Inventory);
            }
        }
        
        /// <summary>
        /// Открыть окно персонажа.
        /// </summary>
        public void OpenCharacter()
        {
            SetState(GameState.Combat); // Используем Combat как экран персонажа
        }
        
        /// <summary>
        /// Переключить окно персонажа.
        /// </summary>
        public void ToggleCharacter()
        {
            if (currentState == GameState.Combat)
            {
                ReturnToPrevious();
            }
            else if (currentState == GameState.Playing)
            {
                SetState(GameState.Combat);
            }
        }
        
        /// <summary>
        /// Переключить карту.
        /// </summary>
        public void ToggleMap()
        {
            if (currentState == GameState.Cutscene) // Используем как карта
            {
                ReturnToPrevious();
            }
            else if (currentState == GameState.Playing)
            {
                SetState(GameState.Cutscene);
            }
        }
        
        // === Dialog ===
        
        /// <summary>
        /// Начать диалог.
        /// </summary>
        public void StartDialog()
        {
            if (currentState == GameState.Playing)
            {
                SetState(GameState.Dialog);
            }
        }
        
        /// <summary>
        /// Завершить диалог.
        /// </summary>
        public void EndDialog()
        {
            if (currentState == GameState.Dialog)
            {
                SetState(GameState.Playing);
            }
        }
        
        // === Settings ===
        
        /// <summary>
        /// Открыть настройки.
        /// </summary>
        public void OpenSettings()
        {
            SetState(GameState.Settings);
        }
        
        // === Game Flow ===
        
        /// <summary>
        /// Продолжить игру.
        /// </summary>
        public void ContinueGame()
        {
            ReturnToPrevious();
        }
        
        /// <summary>
        /// Выйти в главное меню.
        /// </summary>
        public void QuitToMainMenu()
        {
            stateHistory.Clear();
            SetState(GameState.MainMenu);
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
        
        // === Utility ===
        
        /// <summary>
        /// Получить панель по состоянию.
        /// </summary>
        public GameObject GetPanel(GameState state)
        {
            if (panels.TryGetValue(state, out GameObject panel))
            {
                return panel;
            }
            return null;
        }
        
        /// <summary>
        /// Проверить, активно ли состояние.
        /// </summary>
        public bool IsStateActive(GameState state)
        {
            return currentState == state;
        }
    }
}
