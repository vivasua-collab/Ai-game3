// ============================================================================
// ResourceLogger.cs — Система логирования для ресурсной системы
// Cultivation World Simulator
// Создано: 2026-04-15 17:31:49 UTC
// ============================================================================

using UnityEngine;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Статический класс для детального логирования ресурсной системы.
    /// Позволяет точно диагностировать проблемы со спавном и видимостью ресурсов.
    /// Создано: 2026-04-15 17:31:49 UTC
    /// </summary>
    public static class ResourceLogger
    {
        // Уровень логирования: 0=нет, 1=ошибки, 2=предупреждения, 3=инфо, 4=детально
        public static int LogLevel = 3;

        /// <summary>
        /// Логировать информацию о спавне ресурса.
        /// Создано: 2026-04-15 17:31:49 UTC
        /// </summary>
        public static void LogSpawn(string resourceId, Vector3 position, Sprite sprite, float scale, string shaderName)
        {
            if (LogLevel < 3) return;

            string spriteInfo = sprite != null
                ? $"sprite='{sprite.name}', size=({sprite.bounds.size.x:F2}, {sprite.bounds.size.y:F2}), PPU={sprite.pixelsPerUnit}, rect=({sprite.rect.width}x{sprite.rect.height})"
                : "sprite=NULL!";
            string worldSize = sprite != null
                ? $"worldSize=({sprite.bounds.size.x * scale:F2}, {sprite.bounds.size.y * scale:F2})"
                : "worldSize=N/A";

            Debug.Log($"[ResourceLogger] SPAWN: id='{resourceId}', pos=({position.x:F1}, {position.y:F1}, {position.z:F1}), " +
                $"{spriteInfo}, scale={scale:F2}, {worldSize}, shader='{shaderName}'");
        }

        /// <summary>
        /// Логировать информацию о спрайте ресурса.
        /// Создано: 2026-04-15 17:31:49 UTC
        /// </summary>
        public static void LogSpriteLoad(string resourceId, string source, bool success, string path = "")
        {
            if (LogLevel < 3) return;

            if (success)
                Debug.Log($"[ResourceLogger] SPRITE LOAD: id='{resourceId}', source={source}, path='{path}' — OK");
            else
                Debug.LogWarning($"[ResourceLogger] SPRITE LOAD: id='{resourceId}', source={source}, path='{path}' — НЕ НАЙДЕН, fallback на программный");
        }

        /// <summary>
        /// Логировать предупреждение.
        /// Создано: 2026-04-15 17:31:49 UTC
        /// </summary>
        public static void LogWarning(string message)
        {
            if (LogLevel < 2) return;
            Debug.LogWarning($"[ResourceLogger] {message}");
        }

        /// <summary>
        /// Логировать ошибку.
        /// Создано: 2026-04-15 17:31:49 UTC
        /// </summary>
        public static void LogError(string message)
        {
            if (LogLevel < 1) return;
            Debug.LogError($"[ResourceLogger] {message}");
        }

        /// <summary>
        /// Логировать детальную информацию (только при LogLevel=4).
        /// Создано: 2026-04-15 17:31:49 UTC
        /// </summary>
        public static void LogDetail(string message)
        {
            if (LogLevel < 4) return;
            Debug.Log($"[ResourceLogger] DETAIL: {message}");
        }

        /// <summary>
        /// Логировать итоги спавна всех ресурсов.
        /// Создано: 2026-04-15 17:31:49 UTC
        /// </summary>
        public static void LogSpawnSummary(int totalSpawned, int totalAttempted, float elapsedSeconds)
        {
            if (LogLevel < 2) return;
            Debug.Log($"[ResourceLogger] ИТОГ СПАВНА: {totalSpawned}/{totalAttempted} ресурсов за {elapsedSeconds:F1}с");
        }
    }
}
