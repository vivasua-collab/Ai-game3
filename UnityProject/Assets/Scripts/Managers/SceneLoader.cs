// ============================================================================
// SceneLoader.cs — Управление загрузкой сцен
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-04-01 13:03:39 UTC
// ============================================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using CultivationGame.Core;

namespace CultivationGame.Managers
{
    /// <summary>
    /// Менеджер загрузки сцен с поддержкой:
    /// - Асинхронной загрузки
    /// - Индикатора загрузки
    /// - Переходов между сценами
    /// - Паузы при загрузке
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        #region Configuration
        
        [Header("Scene Names")]
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string gameWorldScene = "GameWorld";
        [SerializeField] private string characterCreationScene = "CharacterCreation";
        [SerializeField] private string loadingScene = "Loading";
        
        [Header("Settings")]
        [SerializeField] private float minimumLoadingTime = 1f;
        [SerializeField] private bool pauseDuringLoading = true;
        
        #endregion
        
        #region Runtime
        
        private static SceneLoader _instance;
        private bool isLoading = false;
        private string currentScene;
        private string targetScene;
        
        #endregion
        
        #region Properties
        
        /// <summary>
        /// Синглтон инстанс.
        /// </summary>
        public static SceneLoader Instance => _instance;
        
        /// <summary>
        /// Текущая загружаемая сцена.
        /// </summary>
        public string TargetScene => targetScene;
        
        /// <summary>
        /// Идёт ли загрузка.
        /// </summary>
        public bool IsLoading => isLoading;
        
        /// <summary>
        /// Прогресс загрузки (0-1).
        /// </summary>
        public float LoadProgress { get; private set; }
        
        #endregion
        
        #region Events
        
        /// <summary>
        /// Начало загрузки сцены. (sceneName)
        /// </summary>
        public event Action<string> OnLoadStart;
        
        /// <summary>
        /// Прогресс загрузки. (progress 0-1)
        /// </summary>
        public event Action<float> OnLoadProgress;
        
        /// <summary>
        /// Загрузка завершена. (sceneName)
        /// </summary>
        public event Action<string> OnLoadComplete;
        
        #endregion
        
        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            currentScene = SceneManager.GetActiveScene().name;
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        
        #endregion
        
        #region Public API
        
        /// <summary>
        /// Загрузить главное меню.
        /// </summary>
        public void LoadMainMenu()
        {
            LoadScene(mainMenuScene);
        }
        
        /// <summary>
        /// Загрузить игровой мир.
        /// </summary>
        public void LoadGameWorld()
        {
            LoadScene(gameWorldScene);
        }
        
        /// <summary>
        /// Загрузить создание персонажа.
        /// </summary>
        public void LoadCharacterCreation()
        {
            LoadScene(characterCreationScene);
        }
        
        /// <summary>
        /// Загрузить сцену по имени.
        /// </summary>
        public void LoadScene(string sceneName)
        {
            if (isLoading)
            {
                Debug.LogWarning($"[SceneLoader] Already loading {targetScene}, cannot load {sceneName}");
                return;
            }
            
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[SceneLoader] Scene name is null or empty");
                return;
            }
            
            // FIX MGR-C02: Validate scene exists in Build Settings before loading (2026-04-11)
            if (!IsSceneInBuildSettings(sceneName))
            {
                Debug.LogError($"[SceneLoader] Scene '{sceneName}' not found in Build Settings!");
                return;
            }
            
            StartCoroutine(LoadSceneAsync(sceneName));
        }
        
        /// <summary>
        /// Перезагрузить текущую сцену.
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(currentScene);
        }
        
        /// <summary>
        /// Загрузить следующую сцену (по build index).
        /// </summary>
        public void LoadNextScene()
        {
            int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
            
            if (nextIndex < SceneManager.sceneCountInBuildSettings)
            {
                string scenePath = SceneUtility.GetScenePathByBuildIndex(nextIndex);
                string sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
                LoadScene(sceneName);
            }
            else
            {
                Debug.LogWarning("[SceneLoader] No next scene available");
            }
        }
        
        /// <summary>
        /// Выйти из игры.
        /// </summary>
        public void QuitGame()
        {
            GameEvents.TriggerGameQuit();
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        
        #endregion
        
        #region Private Methods
        
        // FIX MGR-H04: Save previous timeScale instead of unconditionally restoring to 1f (2026-04-11)
        private float previousTimeScale = 1f;
        
        private IEnumerator LoadSceneAsync(string sceneName)
        {
            isLoading = true;
            targetScene = sceneName;
            LoadProgress = 0f;
            
            // Уведомляем о начале загрузки
            GameEvents.TriggerSceneUnloading(currentScene);
            
            // FIX CORE-H02: Очистка событий при смене сцены для предотвращения утечек
            GameEvents.ClearAllEvents();
            
            OnLoadStart?.Invoke(sceneName);
            
            // FIX MGR-H04: Save previous timeScale so we don't unconditionally restore to 1f (2026-04-11)
            // If the game was paused (timeScale=0) before loading, it should stay paused after.
            previousTimeScale = Time.timeScale;
            
            // Пауза игры при загрузке
            if (pauseDuringLoading)
            {
                Time.timeScale = 0f;
            }
            
            // Сначала загружаем loading screen (если есть)
            bool useLoadingScene = !string.IsNullOrEmpty(loadingScene) && 
                                   SceneManager.GetSceneByName(loadingScene).IsValid() == false;
            
            if (useLoadingScene)
            {
                yield return SceneManager.LoadSceneAsync(loadingScene, LoadSceneMode.Additive);
            }
            
            // Запускаем загрузку целевой сцены
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(sceneName);
            
            if (loadOperation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to load scene: {sceneName}");
                isLoading = false;
                yield break;
            }
            
            // Не позволяем сцене активироваться сразу
            loadOperation.allowSceneActivation = false;
            
            // Отслеживаем прогресс
            float startTime = Time.realtimeSinceStartup;
            
            while (!loadOperation.isDone)
            {
                // Unity возвращает 0-0.9 для прогресса, 0.9-1.0 = ready
                float progress = Mathf.Clamp01(loadOperation.progress / 0.9f);
                LoadProgress = progress;
                OnLoadProgress?.Invoke(progress);
                
                // Когда достигли 90%, ждём минимальное время
                if (loadOperation.progress >= 0.9f)
                {
                    // Ждём минимальное время загрузки
                    float elapsed = Time.realtimeSinceStartup - startTime;
                    if (elapsed < minimumLoadingTime)
                    {
                        yield return new WaitForSecondsRealtime(minimumLoadingTime - elapsed);
                    }
                    
                    // Разрешаем активацию
                    loadOperation.allowSceneActivation = true;
                }
                
                yield return null;
            }
            
            // FIX MGR-H03: Only unload loading scene if it's still loaded (2026-04-11)
            // When LoadSceneAsync was called WITHOUT LoadSceneMode.Additive, it replaces
            // the current scene, which unloads the loading scene automatically.
            if (useLoadingScene && SceneManager.GetSceneByName(loadingScene).IsValid())
            {
                yield return SceneManager.UnloadSceneAsync(loadingScene);
            }
            
            // Обновляем состояние
            currentScene = sceneName;
            isLoading = false;
            LoadProgress = 1f;
            
            // FIX MGR-C03: Always restore timeScale (safety for exceptions during load) (2026-04-11)
            // FIX MGR-H04: Restore to previous value, not unconditionally 1f (2026-04-11)
            if (pauseDuringLoading)
            {
                Time.timeScale = previousTimeScale;
            }
            
            // Уведомляем о завершении
            OnLoadProgress?.Invoke(1f);
            OnLoadComplete?.Invoke(sceneName);
            GameEvents.TriggerSceneLoaded(sceneName);
            
            Debug.Log($"[SceneLoader] Scene loaded: {sceneName}");
        }
        
        #endregion
        
        #region Scene Validation
        
        /// <summary>
        /// Проверить, добавлена ли сцена в Build Settings.
        /// </summary>
        public static bool IsSceneInBuildSettings(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (name == sceneName)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Получить все сцены из Build Settings.
        /// </summary>
        public static string[] GetAllScenesInBuild()
        {
            string[] scenes = new string[SceneManager.sceneCountInBuildSettings];
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                scenes[i] = System.IO.Path.GetFileNameWithoutExtension(path);
            }
            return scenes;
        }
        
        #endregion
    }
}
