// ============================================================================
// GameLoggerTests.cs — Unit тесты системы логирования
// Cultivation World Simulator
// Создано: 2026-04-16 12:43 UTC
// ============================================================================
// Тестирует: GameLogger — категории, уровни, фильтрация, вкл/выкл
// ============================================================================

using NUnit.Framework;
using CultivationGame.Core;

namespace CultivationGame.Tests
{
    [TestFixture]
    public class GameLoggerTests
    {
        [SetUp]
        public void Setup()
        {
            // Сбрасываем логгер перед каждым тестом
            GameLogger.Shutdown();
            GameLogger.Initialize(null);
        }

        [TearDown]
        public void TearDown()
        {
            GameLogger.EnableAllCategories();
            GameLogger.SetMinLogLevel(LogLevel.Debug);
            GameLogger.Shutdown();
        }

        // ====================================================================
        //  Категории — вкл/выкл
        // ====================================================================

        [Test]
        public void SetCategoryEnabled_DisableCategory_CategoryIsDisabled()
        {
            // Act
            GameLogger.SetCategoryEnabled(LogCategory.Combat, false);

            // Assert
            Assert.IsFalse(GameLogger.IsCategoryEnabled(LogCategory.Combat),
                "Категория Combat должна быть выключена");
        }

        [Test]
        public void SetCategoryEnabled_EnableCategory_CategoryIsEnabled()
        {
            // Arrange
            GameLogger.SetCategoryEnabled(LogCategory.Qi, false);

            // Act
            GameLogger.SetCategoryEnabled(LogCategory.Qi, true);

            // Assert
            Assert.IsTrue(GameLogger.IsCategoryEnabled(LogCategory.Qi),
                "Категория Qi должна быть включена");
        }

        [Test]
        public void DisableAllCategories_OnlySystemEnabled()
        {
            // Act
            GameLogger.DisableAllCategories();

            // Assert
            Assert.IsTrue(GameLogger.IsCategoryEnabled(LogCategory.System),
                "System должна остаться включённой");
            Assert.IsFalse(GameLogger.IsCategoryEnabled(LogCategory.Combat),
                "Combat должна быть выключена");
            Assert.IsFalse(GameLogger.IsCategoryEnabled(LogCategory.Qi),
                "Qi должна быть выключена");
        }

        [Test]
        public void EnableAllCategories_AllEnabled()
        {
            // Arrange
            GameLogger.DisableAllCategories();

            // Act
            GameLogger.EnableAllCategories();

            // Assert
            foreach (LogCategory cat in System.Enum.GetValues(typeof(LogCategory)))
            {
                Assert.IsTrue(GameLogger.IsCategoryEnabled(cat),
                    $"Категория {cat} должна быть включена");
            }
        }

        // ====================================================================
        //  Уровни логов
        // ====================================================================

        [Test]
        public void SetMinLogLevel_Warning_WarningAndErrorPass()
        {
            // Act
            GameLogger.SetMinLogLevel(LogLevel.Warning);

            // Assert — фильтр не генерирует исключений
            // (проверяем что метод выполняется без ошибок)
            GameLogger.LogDebug(LogCategory.System, "Это не должно логироваться");
            GameLogger.Log(LogCategory.System, "Это тоже не должно логироваться");
            GameLogger.LogWarning(LogCategory.System, "Это должно логироваться");
            GameLogger.LogError(LogCategory.System, "Это точно должно логироваться");
        }

        [Test]
        public void SetMinLogLevel_Error_OnlyErrorPass()
        {
            // Act
            GameLogger.SetMinLogLevel(LogLevel.Error);

            // Проверяем что метод не падает
            GameLogger.Log(LogCategory.System, "Инфо — не должно пройти");
            GameLogger.LogWarning(LogCategory.System, "Предупреждение — не должно пройти");
            GameLogger.LogError(LogCategory.System, "Ошибка — должна пройти");
        }

        // ====================================================================
        //  Логирование — основные методы не падают
        // ====================================================================

        [Test]
        public void Log_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameLogger.Log(LogCategory.System, "Тестовое сообщение");
            }, "Log не должен вызывать исключений");
        }

        [Test]
        public void Log_WithFormat_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameLogger.Log(LogCategory.Combat, "Урон: {0}, Тип: {1}", 50, "Melee");
            }, "Log с форматированием не должен вызывать исключений");
        }

        [Test]
        public void LogDebug_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameLogger.LogDebug(LogCategory.Qi, "Отладочное сообщение");
            }, "LogDebug не должен вызывать исключений");
        }

        [Test]
        public void LogWarning_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameLogger.LogWarning(LogCategory.Save, "Предупреждение");
            }, "LogWarning не должен вызывать исключений");
        }

        [Test]
        public void LogError_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameLogger.LogError(LogCategory.Render, "Ошибка рендера");
            }, "LogError не должен вызывать исключений");
        }

        // ====================================================================
        //  Фильтрация — выключенная категория не логирует
        // ====================================================================

        [Test]
        public void Log_DisabledCategory_DoesNotThrow()
        {
            // Arrange
            GameLogger.SetCategoryEnabled(LogCategory.Network, false);

            // Act & Assert — не падает, но и не логирует
            Assert.DoesNotThrow(() =>
            {
                GameLogger.Log(LogCategory.Network, "Это сообщение не должно выводиться");
            }, "Лог в выключенную категорию не должен падать");
        }

        // ====================================================================
        //  LogConfigState — не падает
        // ====================================================================

        [Test]
        public void LogConfigState_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameLogger.LogConfigState();
            }, "LogConfigState не должен падать");
        }

        // ====================================================================
        //  Shutdown — корректное завершение
        // ====================================================================

        [Test]
        public void Shutdown_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                GameLogger.Shutdown();
            }, "Shutdown не должен падать");
        }

        [Test]
        public void Shutdown_CanReinitialize()
        {
            // Act
            GameLogger.Shutdown();
            GameLogger.Initialize(null);

            // Assert — логгер работает после реинициализации
            Assert.DoesNotThrow(() =>
            {
                GameLogger.Log(LogCategory.System, "После реинициализации");
            }, "Логгер должен работать после реинициализации");
        }
    }
}
