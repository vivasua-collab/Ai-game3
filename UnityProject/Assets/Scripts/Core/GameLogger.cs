// ============================================================================
// GameLogger.cs — Централизованная система логирования
// Cultivation World Simulator
// Создано: 2026-04-16 12:43 UTC
// ============================================================================
// Архитектура:
//   - LogCategory: enum категорий логов (вкл/выкл по категории)
//   - LogLevel: уровни (Debug, Info, Warning, Error)
//   - GameLogger: статический API — Log(), LogWarning(), LogError()
//   - LogConfig: ScriptableObject — настройки (вкл/выкл категории, файловые логи)
//   - FileLogHandler: запись логов в файл (опционально)
//
// ИСПОЛЬЗОВАНИЕ:
//   GameLogger.Log(LogCategory.Combat, "Удар нанесён: {0} dmg", damage);
//   GameLogger.LogWarning(LogCategory.Qi, "Ци ниже минимума: {0}", qi);
//   GameLogger.LogError(LogCategory.Save, "Файл не найден: {0}", path);
//
// НАСТРОЙКА:
//   Assets/Data/LogConfig.asset — ScriptableObject с флагами
//   Или через код: GameLogger.SetCategoryEnabled(LogCategory.Combat, false);
// ============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CultivationGame.Core
{
    // ========================================================================
    //  Категории логов
    // ========================================================================

    /// <summary>
    /// Категории логирования. Каждую категорию можно включить/выключить.
    /// Создано: 2026-04-16 12:43 UTC
    /// </summary>
    public enum LogCategory
    {
        System,     // Общие системные сообщения
        Combat,     // Боевая система
        Qi,         // Система Ци
        Body,       // Система тела
        Tile,       // Тайловая система
        Player,     // Игрок
        NPC,        // NPC
        Render,     // Рендер-пайплайн
        Save,       // Сохранение/загрузка
        Formation,  // Формации
        Inventory,  // Инвентарь
        Interaction,// Взаимодействие
        UI,         // Пользовательский интерфейс
        Harvest,    // Система сбора (harvest)
        Generator,  // Генераторы контента
        Network     // Сетевое (на будущее)
    }

    // ========================================================================
    //  Уровни логов
    // ========================================================================

    /// <summary>
    /// Уровни логирования. Фильтр: логируются только сообщения >= минимального уровня.
    /// Создано: 2026-04-16 12:43 UTC
    /// </summary>
    public enum LogLevel
    {
        Debug   = 0,  // Детальная отладка — только в Development builds
        Info    = 1,  // Информационные сообщения
        Warning = 2,  // Предупреждения
        Error   = 3   // Ошибки
    }

    // ========================================================================
    //  Конфигурация логирования (ScriptableObject)
    // ========================================================================

    /// <summary>
    /// Настройки логирования. Создаётся как ScriptableObject в Assets/Data/.
    /// Доступно через Inspector — можно менять флаги без перекомпиляции.
    /// Создано: 2026-04-16 12:43 UTC
    /// </summary>
    [CreateAssetMenu(fileName = "LogConfig", menuName = "Cultivation/Log Config")]
    public class LogConfig : ScriptableObject
    {
        [Header("Общие настройки")]
        [Tooltip("Минимальный уровень логов. Логи ниже уровня не выводятся.")]
        public LogLevel minLogLevel = LogLevel.Debug;

        [Tooltip("Включить запись логов в файл")]
        public bool enableFileLogging = false;

        [Tooltip("Путь к файлу логов (относительно Application.persistentDataPath)")]
        public string logFilePath = "Logs/game.log";

        [Tooltip("Максимальный размер файла логов (МБ). При превышении — ротация.")]
        public int maxFileSizeMB = 10;

        [Tooltip("Включить временные метки в логах")]
        public bool includeTimestamp = true;

        [Tooltip("Включить имя категории в логах")]
        public bool includeCategory = true;

        [Tooltip("Цветной формат в Unity Console (теги rich text)")]
        public bool richTextInConsole = true;

        [Header("Категории — вкл/выкл")]
        [Tooltip("Включить/выключить логирование по категориям")]
        public CategoryToggle[] categories = new CategoryToggle[]
        {
            new CategoryToggle(LogCategory.System, true),
            new CategoryToggle(LogCategory.Combat, true),
            new CategoryToggle(LogCategory.Qi, true),
            new CategoryToggle(LogCategory.Body, true),
            new CategoryToggle(LogCategory.Tile, true),
            new CategoryToggle(LogCategory.Player, true),
            new CategoryToggle(LogCategory.NPC, true),
            new CategoryToggle(LogCategory.Render, true),
            new CategoryToggle(LogCategory.Save, true),
            new CategoryToggle(LogCategory.Formation, true),
            new CategoryToggle(LogCategory.Inventory, true),
            new CategoryToggle(LogCategory.Interaction, true),
            new CategoryToggle(LogCategory.UI, true),
            new CategoryToggle(LogCategory.Harvest, true),
            new CategoryToggle(LogCategory.Generator, true),
            new CategoryToggle(LogCategory.Network, true),
        };

        /// <summary>
        /// Сериализуемая пара категория-флаг для Inspector
        /// </summary>
        [Serializable]
        public class CategoryToggle
        {
            public LogCategory category;
            public bool enabled = true;

            public CategoryToggle(LogCategory cat, bool en)
            {
                category = cat;
                enabled = en;
            }
        }
    }

    // ========================================================================
    //  Основной класс GameLogger
    // ========================================================================

    /// <summary>
    /// Централизованная система логирования.
    /// - Фильтрация по категориям (вкл/выкл)
    /// - Фильтрация по уровню (Debug, Info, Warning, Error)
    /// - Вывод в Unity Console (Debug.Log)
    /// - Опциональная запись в файл (FileLogHandler)
    /// - Цветовое кодирование по категориям в Console
    ///
    /// Создано: 2026-04-16 12:43 UTC
    /// </summary>
    public static class GameLogger
    {
        // ---- Состояние ----
        private static Dictionary<LogCategory, bool> _categoryEnabled = new Dictionary<LogCategory, bool>();
        private static LogLevel _minLogLevel = LogLevel.Debug;
        private static bool _fileLoggingEnabled = false;
        private static FileLogHandler _fileHandler;
        private static bool _initialized = false;
        private static LogConfig _config;

        // ---- Цвета категорий для Console ----
        private static readonly Dictionary<LogCategory, string> CategoryColors = new Dictionary<LogCategory, string>
        {
            { LogCategory.System,      "#AAAAAA" },  // Серый
            { LogCategory.Combat,      "#FF6B6B" },  // Красный
            { LogCategory.Qi,          "#6BCB77" },  // Зелёный
            { LogCategory.Body,        "#FFD93D" },  // Жёлтый
            { LogCategory.Tile,        "#4D96FF" },  // Синий
            { LogCategory.Player,      "#9B59B6" },  // Фиолетовый
            { LogCategory.NPC,         "#E67E22" },  // Оранжевый
            { LogCategory.Render,      "#1ABC9C" },  // Бирюзовый
            { LogCategory.Save,        "#95A5A6" },  // Серебро
            { LogCategory.Formation,   "#E91E63" },  // Розовый
            { LogCategory.Inventory,   "#8BC34A" },  // Лаймовый
            { LogCategory.Interaction, "#00BCD4" },  // Циановый
            { LogCategory.UI,          "#FF9800" },  // Янтарный
            { LogCategory.Harvest,     "#795548" },  // Коричневый
            { LogCategory.Generator,   "#607D8B" },  // Стальной
            { LogCategory.Network,     "#3F51B5" },  // Индиго
        };

        // ---- Короткие имена категорий для логов ----
        private static readonly Dictionary<LogCategory, string> CategoryShortNames = new Dictionary<LogCategory, string>
        {
            { LogCategory.System,      "SYS" },
            { LogCategory.Combat,      "CMB" },
            { LogCategory.Qi,          "QI" },
            { LogCategory.Body,        "BDY" },
            { LogCategory.Tile,        "TIL" },
            { LogCategory.Player,      "PLR" },
            { LogCategory.NPC,         "NPC" },
            { LogCategory.Render,      "RND" },
            { LogCategory.Save,        "SAV" },
            { LogCategory.Formation,   "FRM" },
            { LogCategory.Inventory,   "INV" },
            { LogCategory.Interaction, "INT" },
            { LogCategory.UI,          "UI" },
            { LogCategory.Harvest,     "HRV" },
            { LogCategory.Generator,   "GEN" },
            { LogCategory.Network,     "NET" },
        };

        // ====================================================================
        //  Инициализация
        // ====================================================================

        /// <summary>
        /// Инициализация логгера. Вызывается автоматически при первом логе,
        /// или явно из GameInitializer.
        /// Создано: 2026-04-16 12:43 UTC
        /// </summary>
        public static void Initialize(LogConfig config = null)
        {
            if (_initialized) return;

            _config = config;

            // Инициализируем категории — по умолчанию все включены
            _categoryEnabled.Clear();
            foreach (LogCategory cat in Enum.GetValues(typeof(LogCategory)))
            {
                _categoryEnabled[cat] = true;
            }

            // Если есть конфиг — применяем
            if (config != null)
            {
                _minLogLevel = config.minLogLevel;
                _fileLoggingEnabled = config.enableFileLogging;

                // Применяем флаги категорий из конфига
                if (config.categories != null)
                {
                    foreach (var toggle in config.categories)
                    {
                        _categoryEnabled[toggle.category] = toggle.enabled;
                    }
                }

                // Инициализируем файловый логгер если нужно
                if (_fileLoggingEnabled)
                {
                    _fileHandler = new FileLogHandler(config.logFilePath, config.maxFileSizeMB);
                }
            }

            _initialized = true;

            // Логируем инициализацию (используем внутренний метод чтобы не зациклиться)
            Debug.Log("[GameLogger] Инициализация завершена. " +
                $"Уровень: {_minLogLevel}, Файл: {_fileLoggingEnabled}, " +
                $"Категорий: {_categoryEnabled.Count}");
        }

        /// <summary>
        /// Убедиться что логгер инициализирован (ленивая инициализация)
        /// </summary>
        private static void EnsureInitialized()
        {
            if (!_initialized)
            {
                // Пробуем загрузить конфиг из ресурсов
                var config = Resources.Load<LogConfig>("LogConfig");
                Initialize(config);
            }
        }

        // ====================================================================
        //  Публичный API — Логирование
        // ====================================================================

        /// <summary>
        /// Информационный лог. Уровень: Info.
        /// </summary>
        public static void Log(LogCategory category, string message)
        {
            LogInternal(LogLevel.Info, category, message);
        }

        /// <summary>
        /// Информационный лог с форматированием. Уровень: Info.
        /// </summary>
        public static void Log(LogCategory category, string format, params object[] args)
        {
            LogInternal(LogLevel.Info, category, string.Format(format, args));
        }

        /// <summary>
        /// Отладочный лог. Уровень: Debug. Выводится только если minLogLevel <= Debug.
        /// </summary>
        public static void LogDebug(LogCategory category, string message)
        {
            LogInternal(LogLevel.Debug, category, message);
        }

        /// <summary>
        /// Отладочный лог с форматированием. Уровень: Debug.
        /// </summary>
        public static void LogDebug(LogCategory category, string format, params object[] args)
        {
            LogInternal(LogLevel.Debug, category, string.Format(format, args));
        }

        /// <summary>
        /// Предупреждение. Уровень: Warning.
        /// </summary>
        public static void LogWarning(LogCategory category, string message)
        {
            LogInternal(LogLevel.Warning, category, message);
        }

        /// <summary>
        /// Предупреждение с форматированием. Уровень: Warning.
        /// </summary>
        public static void LogWarning(LogCategory category, string format, params object[] args)
        {
            LogInternal(LogLevel.Warning, category, string.Format(format, args));
        }

        /// <summary>
        /// Ошибка. Уровень: Error.
        /// </summary>
        public static void LogError(LogCategory category, string message)
        {
            LogInternal(LogLevel.Error, category, message);
        }

        /// <summary>
        /// Ошибка с форматированием. Уровень: Error.
        /// </summary>
        public static void LogError(LogCategory category, string format, params object[] args)
        {
            LogInternal(LogLevel.Error, category, string.Format(format, args));
        }

        // ====================================================================
        //  Публичный API — Управление
        // ====================================================================

        /// <summary>
        /// Включить/выключить категорию логов.
        /// </summary>
        public static void SetCategoryEnabled(LogCategory category, bool enabled)
        {
            EnsureInitialized();
            _categoryEnabled[category] = enabled;
        }

        /// <summary>
        /// Проверить, включена ли категория.
        /// </summary>
        public static bool IsCategoryEnabled(LogCategory category)
        {
            EnsureInitialized();
            return _categoryEnabled.ContainsKey(category) && _categoryEnabled[category];
        }

        /// <summary>
        /// Установить минимальный уровень логов.
        /// </summary>
        public static void SetMinLogLevel(LogLevel level)
        {
            EnsureInitialized();
            _minLogLevel = level;
        }

        /// <summary>
        /// Включить/выключить запись в файл.
        /// </summary>
        public static void SetFileLogging(bool enabled)
        {
            EnsureInitialized();
            _fileLoggingEnabled = enabled;
            if (enabled && _fileHandler == null)
            {
                _fileHandler = new FileLogHandler("Logs/game.log", 10);
            }
        }

        /// <summary>
        /// Включить ВСЕ категории.
        /// </summary>
        public static void EnableAllCategories()
        {
            EnsureInitialized();
            foreach (LogCategory cat in Enum.GetValues(typeof(LogCategory)))
            {
                _categoryEnabled[cat] = true;
            }
        }

        /// <summary>
        /// Выключить ВСЕ категории кроме System.
        /// </summary>
        public static void DisableAllCategories()
        {
            EnsureInitialized();
            foreach (LogCategory cat in Enum.GetValues(typeof(LogCategory)))
            {
                _categoryEnabled[cat] = cat == LogCategory.System;
            }
        }

        /// <summary>
        /// Вывести текущую конфигурацию логирования в Console.
        /// </summary>
        public static void LogConfigState()
        {
            EnsureInitialized();
            var sb = new StringBuilder();
            sb.AppendLine("[GameLogger] Конфигурация:");
            sb.AppendLine($"  Минимальный уровень: {_minLogLevel}");
            sb.AppendLine($"  Файловый лог: {_fileLoggingEnabled}");
            sb.AppendLine("  Категории:");
            foreach (LogCategory cat in Enum.GetValues(typeof(LogCategory)))
            {
                bool enabled = _categoryEnabled.ContainsKey(cat) && _categoryEnabled[cat];
                sb.AppendLine($"    {cat}: {(enabled ? "✅ ВКЛ" : "❌ ВЫКЛ")}");
            }
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// Закрыть файловый лог и освободить ресурсы.
        /// Вызывается при выходе из приложения.
        /// </summary>
        public static void Shutdown()
        {
            if (_fileHandler != null)
            {
                _fileHandler.Close();
                _fileHandler = null;
            }
            _initialized = false;
        }

        // ====================================================================
        //  Внутренняя реализация
        // ====================================================================

        private static void LogInternal(LogLevel level, LogCategory category, string message)
        {
            EnsureInitialized();

            // Фильтр по уровню
            if (level < _minLogLevel) return;

            // Фильтр по категории
            if (!IsCategoryEnabled(category)) return;

            // Формируем префикс
            string shortName = CategoryShortNames.ContainsKey(category) ? CategoryShortNames[category] : category.ToString();
            string timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
            string levelStr = level.ToString().ToUpper();
            string prefix = $"[{timestamp}][{shortName}][{levelStr}]";

            // Полное сообщение
            string fullMessage = $"{prefix} {message}";

            // Вывод в Unity Console с цветом
            string color = CategoryColors.ContainsKey(category) ? CategoryColors[category] : "#FFFFFF";
            string consoleMessage = $"<color={color}>{prefix}</color> {message}";

            // Выводим через соответствующий метод Debug
            switch (level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    Debug.Log(consoleMessage);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(consoleMessage);
                    break;
                case LogLevel.Error:
                    Debug.LogError(consoleMessage);
                    break;
            }

            // Запись в файл если включена
            if (_fileLoggingEnabled && _fileHandler != null)
            {
                _fileHandler.WriteLine(fullMessage);
            }
        }
    }

    // ========================================================================
    //  Файловый обработчик логов
    // ========================================================================

    /// <summary>
    /// Запись логов в файл с ротацией по размеру.
    /// Файл: Application.persistentDataPath/logFilePath
    /// При превышении maxFileSizeMB — старый файл переименовывается в .bak,
    /// новый файл создаётся с нуля.
    /// Создано: 2026-04-16 12:43 UTC
    /// </summary>
    public class FileLogHandler
    {
        private readonly string _logPath;
        private readonly string _bakPath;
        private readonly int _maxSizeBytes;
        private StreamWriter _writer;
        private readonly object _lock = new object();

        public FileLogHandler(string relativePath, int maxSizeMB)
        {
            string basePath = Application.persistentDataPath;
            string fullDir = Path.Combine(basePath, Path.GetDirectoryName(relativePath));

            if (!Directory.Exists(fullDir))
            {
                Directory.CreateDirectory(fullDir);
            }

            _logPath = Path.Combine(basePath, relativePath);
            _bakPath = _logPath + ".bak";
            _maxSizeBytes = maxSizeMB * 1024 * 1024;

            // Ротация: если файл слишком большой — переименовать в .bak
            if (File.Exists(_logPath))
            {
                var info = new FileInfo(_logPath);
                if (info.Length > _maxSizeBytes)
                {
                    try
                    {
                        if (File.Exists(_bakPath)) File.Delete(_bakPath);
                        File.Move(_logPath, _bakPath);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[FileLogHandler] Ошибка ротации: {ex.Message}");
                    }
                }
            }

            // Открываем файл для дописывания
            try
            {
                _writer = new StreamWriter(_logPath, true, Encoding.UTF8) { AutoFlush = true };
                _writer.WriteLine($"=== Лог-сессия начата: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ===");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FileLogHandler] Не удалось открыть файл лога: {ex.Message}");
                _writer = null;
            }
        }

        /// <summary>
        /// Записать строку в файл лога.
        /// </summary>
        public void WriteLine(string line)
        {
            if (_writer == null) return;

            lock (_lock)
            {
                try
                {
                    _writer.WriteLine(line);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[FileLogHandler] Ошибка записи: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Закрыть файл лога.
        /// </summary>
        public void Close()
        {
            if (_writer != null)
            {
                try
                {
                    _writer.WriteLine($"=== Лог-сессия завершена: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC ===");
                    _writer.Close();
                }
                catch { }
                _writer = null;
            }
        }
    }

    // ========================================================================
    //  MonoBehaviour-обёртка для инициализации логгера на сцене
    // ========================================================================

    /// <summary>
    /// Компонент для инициализации GameLogger на сцене.
    /// Добавить к GameManager. При старте инициализирует логгер с конфигом.
    /// При выходе — корректно закрывает файл лога.
    /// Создано: 2026-04-16 12:43 UTC
    /// </summary>
    public class GameLoggerInit : MonoBehaviour
    {
        [Tooltip("Конфигурация логирования (ScriptableObject)")]
        public LogConfig logConfig;

        [Tooltip("Включить файловый лог в Editor (для отладки)")]
        public bool enableFileLogInEditor = false;

        private void Awake()
        {
            // Инициализация логгера
            GameLogger.Initialize(logConfig);

            // В Editor можно включить файловый лог для отладки
#if UNITY_EDITOR
            if (enableFileLogInEditor && logConfig != null && !logConfig.enableFileLogging)
            {
                GameLogger.SetFileLogging(true);
            }
#endif

            GameLogger.Log(LogCategory.System, "GameLogger инициализирован");
        }

        private void OnApplicationQuit()
        {
            GameLogger.Log(LogCategory.System, "Приложение завершается");
            GameLogger.Shutdown();
        }

#if UNITY_EDITOR
        [ContextMenu("Логгер: Показать конфигурацию")]
        private void ShowConfig()
        {
            GameLogger.LogConfigState();
        }

        [ContextMenu("Логгер: Включить все категории")]
        private void EnableAll()
        {
            GameLogger.EnableAllCategories();
            GameLogger.Log(LogCategory.System, "Все категории логов включены");
        }

        [ContextMenu("Логгер: Выключить все категории (кроме System)")]
        private void DisableAll()
        {
            GameLogger.DisableAllCategories();
            GameLogger.Log(LogCategory.System, "Все категории логов выключены (кроме System)");
        }

        [ContextMenu("Логгер: Открыть файл лога")]
        private void OpenLogFile()
        {
            string path = System.IO.Path.Combine(
                UnityEngine.Application.persistentDataPath,
                logConfig != null ? logConfig.logFilePath : "Logs/game.log");
            if (System.IO.File.Exists(path))
            {
                UnityEditor.EditorUtility.RevealInFinder(path);
            }
            else
            {
                Debug.LogWarning($"[GameLogger] Файл лога не найден: {path}");
            }
        }
#endif
    }
}
