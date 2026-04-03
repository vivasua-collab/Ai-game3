// ============================================================================
// LevelSuppression.cs — Подавление уровнем культивации
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-03-31 09:24:43 UTC
// ============================================================================

using System;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Статический класс для расчёта подавления уровнем.
    /// Решает проблему: практик L1 не должен наносить урон практике L8.
    /// 
    /// Источник: ALGORITHMS.md §1 "Система подавления уровнем"
    /// 
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  ПРОБЛЕМА                                                                  ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║  Qi Buffer с поглощением 90% позволяет практике L1 наносить урон          ║
    /// ║  практике L8. Это нарушает лор о разнице сил между уровнями.              ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// 
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  РЕШЕНИЕ                                                                   ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║  Множитель подавления на основе разницы уровней культивации.              ║
    /// ║  Применяется ПЕРЕД Qi Buffer.                                             ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public static class LevelSuppression
    {
        /// <summary>
        /// Рассчитать множитель подавления.
        /// 
        /// Источник: ALGORITHMS.md §1.3-1.4 "Таблица подавления" и "Формула"
        /// 
        /// Формула:
        /// effectiveAttackerLevel = max(attackerLevel, techniqueLevel)  // для техник
        /// levelDiff = max(0, defenderLevel - effectiveAttackerLevel)
        /// suppression = SUPPRESSION_TABLE[min(5, levelDiff)][attackType]
        /// finalDamage = rawDamage × suppression
        /// 
        /// Таблица подавления:
        /// | Разница уровней | Normal | Technique | Ultimate |
        /// |-----------------|--------|-----------|----------|
        /// | 0               | ×1.0   | ×1.0      | ×1.0     |
        /// | 1               | ×0.5   | ×0.75     | ×1.0     |
        /// | 2               | ×0.1   | ×0.25     | ×0.5     |
        /// | 3               | ×0.0   | ×0.05     | ×0.25    |
        /// | 4               | ×0.0   | ×0.0      | ×0.1     |
        /// | 5+              | ×0.0   | ×0.0      | ×0.0     |
        /// </summary>
        /// <param name="attackerLevel">Уровень культивации атакующего</param>
        /// <param name="defenderLevel">Уровень культивации защищающегося</param>
        /// <param name="attackType">Тип атаки (Normal, Technique, Ultimate)</param>
        /// <param name="techniqueLevel">Уровень техники (если применимо)</param>
        /// <returns>Множитель урона (0.0 - 1.0)</returns>
        public static float CalculateSuppression(
            int attackerLevel, 
            int defenderLevel, 
            AttackType attackType,
            int techniqueLevel = 0)
        {
            // Эффективный уровень атакующего
            // Для техник: берём максимум из уровня практика и уровня техники
            // Источник: ALGORITHMS.md §1.4 "effectiveAttackerLevel = max(attackerLevel, techniqueLevel)"
            int effectiveAttackerLevel = attackType == AttackType.Normal 
                ? attackerLevel 
                : Math.Max(attackerLevel, techniqueLevel);
            
            // Разница уровней (только положительная — защитник сильнее)
            // Подавление работает ТОЛЬКО когда защитник сильнее!
            int levelDiff = Math.Max(0, defenderLevel - effectiveAttackerLevel);
            
            // Ограничение таблицы (макс 5 уровней разницы)
            int tableIndex = Math.Min(levelDiff, GameConstants.MAX_LEVEL_DIFF);
            
            // Индекс типа атаки (0=Normal, 1=Technique, 2=Ultimate)
            int attackIndex = (int)attackType;
            
            // ✅ Используем константы из GameConstants
            return GameConstants.LevelSuppressionTable[tableIndex][attackIndex];
        }
        
        /// <summary>
        /// Проверить, может ли атака нанести урон.
        /// </summary>
        public static bool CanDealDamage(
            int attackerLevel,
            int defenderLevel,
            AttackType attackType,
            int techniqueLevel = 0)
        {
            return CalculateSuppression(attackerLevel, defenderLevel, attackType, techniqueLevel) > 0f;
        }
        
        /// <summary>
        /// Получить строковое описание подавления.
        /// </summary>
        public static string GetSuppressionDescription(float suppression)
        {
            if (suppression >= 1.0f) return "Полный урон";
            if (suppression >= 0.75f) return "Слабое подавление";
            if (suppression >= 0.5f) return "Умеренное подавление";
            if (suppression >= 0.25f) return "Сильное подавление";
            if (suppression > 0f) return "Критическое подавление";
            return "Урон невозможен";
        }
        
        /// <summary>
        /// Получить множитель подавления по разнице уровней.
        /// </summary>
        public static float GetSuppressionByDiff(int levelDiff, AttackType attackType)
        {
            int tableIndex = Math.Min(Math.Max(0, levelDiff), GameConstants.MAX_LEVEL_DIFF);
            return GameConstants.LevelSuppressionTable[tableIndex][(int)attackType];
        }
    }
}
