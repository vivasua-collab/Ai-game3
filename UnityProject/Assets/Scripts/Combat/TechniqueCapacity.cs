// ============================================================================
// TechniqueCapacity.cs — Ёмкость и расчёт техник
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-03-31 09:54:21 UTC
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Статический класс для расчёта ёмкости и урона техник.
    /// 
    /// Источники:
    /// - ALGORITHMS.md §3 "Ёмкость техник"
    /// - TECHNIQUE_SYSTEM.md §"Структурная ёмкость"
    /// 
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  ГЛАВНОЕ ПРАВИЛО                                                          ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║  finalDamage = capacity × gradeMult × ultimateMult                         ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// 
    /// Множители Grade (ТЕПЕРЬ СООТВЕТСТВУЮТ ДОКУМЕНТАЦИИ):
    /// | Grade        | Множитель |
    /// |--------------|-----------|
    /// | Common       | ×1.0      |
    /// | Refined      | ×1.2      |
    /// | Perfect      | ×1.4      |
    /// | Transcendent | ×1.6      |
    /// 
    /// Ultimate множитель: ×1.3 (согласно TECHNIQUE_SYSTEM.md)
    /// Ultimate стоимость Ци: ×1.5
    /// </summary>
    public static class TechniqueCapacity
    {
        /// <summary>
        /// Рассчитать полную ёмкость техники.
        /// 
        /// Источник: ALGORITHMS.md §3.2 "Формула полной ёмкости"
        /// 
        /// Формула:
        /// capacity = baseCapacity × 2^(level-1) × (1 + mastery × 0.5%)
        /// 
        /// Где:
        /// - baseCapacity — базовая ёмкость по типу техники
        /// - level — уровень техники (1-9)
        /// - mastery — мастерство (0-100%)
        /// 
        /// Пример (melee_strike L5, mastery 0%):
        /// capacity = 64 × 2^4 × 1.0 = 1024
        /// </summary>
        public static int CalculateCapacity(
            TechniqueType techniqueType,
            CombatSubtype subtype,
            int level,
            float mastery)
        {
            // Базовая ёмкость
            int baseCapacity = GetBaseCapacity(techniqueType, subtype);
            
            if (baseCapacity <= 0) return 0; // Пассивные техники (Cultivation)
            
            // Множитель уровня: 2^(level-1)
            // Источник: ALGORITHMS.md §3.2 "levelMultiplier = 2^(techniqueLevel - 1)"
            float levelMultiplier = Mathf.Pow(2, level - 1);
            
            // Бонус мастерства: до +50% при 100%
            // Источник: TECHNIQUE_SYSTEM.md "Бонус мастерства: до +50% при 100%"
            float masteryBonus = 1f + (mastery / 100f) * 0.5f;
            
            return (int)(baseCapacity * levelMultiplier * masteryBonus);
        }
        
        /// <summary>
        /// Получить базовую ёмкость по типу/подтипу.
        /// 
        /// Источник: ALGORITHMS.md §3.1 "Базовая ёмкость по типу"
        /// 
        /// | Тип техники          | baseCapacity |
        /// |----------------------|--------------|
        /// | Formation            | 80           |
        /// | Defense              | 72           |
        /// | Combat (melee_strike)| 64           |
        /// | Support              | 56           |
        /// | Healing              | 56           |
        /// | Combat (melee_weapon)| 48           |
        /// | Movement             | 40           |
        /// | Curse                | 40           |
        /// | Poison               | 40           |
        /// | Sensory              | 32           |
        /// | Combat (ranged_*)    | 32           |
        /// | Cultivation          | null         |
        /// </summary>
        public static int GetBaseCapacity(TechniqueType techniqueType, CombatSubtype subtype)
        {
            // Если есть подтип для боевых техник
            if (techniqueType == TechniqueType.Combat && subtype != CombatSubtype.None)
            {
                if (GameConstants.BaseCapacityBySubtype.TryGetValue(subtype, out int subCapacity))
                {
                    return subCapacity;
                }
            }
            
            // Иначе по типу
            if (GameConstants.BaseCapacityByType.TryGetValue(techniqueType, out int capacity))
            {
                return capacity;
            }
            
            return 0;
        }
        
        /// <summary>
        /// Рассчитать стоимость Ци для техники.
        /// 
        /// Источник: ALGORITHMS.md §4.2 "Qi Cost"
        /// 
        /// Формула:
        /// qiCost = floor(baseCapacity × levelMultiplier)
        /// 
        /// ⚠️ ВАЖНО: Стоимость Ци НЕ зависит от Grade!
        /// Источник: TECHNIQUE_SYSTEM.md "Стоимость Ци всегда ×1.0 — не зависит от Grade"
        /// </summary>
        public static long CalculateQiCost(long baseCapacity, int level)
        {
            double levelMultiplier = Mathf.Pow(2, level - 1);
            return (long)(baseCapacity * levelMultiplier);
        }
        
        /// <summary>
        /// Рассчитать итоговый урон техники.
        /// 
        /// Источник: ALGORITHMS.md §4.1 "Основная формула"
        /// 
        /// Формула:
        /// finalDamage = capacity × gradeMultiplier × ultimateMultiplier
        /// 
        /// Множители Grade (TECHNIQUE_SYSTEM.md):
        /// - Common: 1.0
        /// - Refined: 1.2
        /// - Perfect: 1.4
        /// - Transcendent: 1.6
        /// </summary>
        public static int CalculateDamage(int capacity, TechniqueGrade grade, bool isUltimate)
        {
            // Множитель грейда (теперь соответствует документации)
            float gradeMultiplier = GameConstants.TechniqueGradeMultipliers.TryGetValue(grade, out float g) 
                ? g 
                : 1.0f;
            
            // Множитель Ultimate (×1.3 согласно TECHNIQUE_SYSTEM.md)
            float ultimateMultiplier = isUltimate ? GameConstants.ULTIMATE_DAMAGE_MULTIPLIER : 1.0f;
            
            return (int)(capacity * gradeMultiplier * ultimateMultiplier);
        }
        
        /// <summary>
        /// Рассчитать дестабилизацию при перегрузке Ци.
        /// 
        /// Источник: ALGORITHMS.md §4.3 "Дестабилизация"
        /// 
        /// При qiInput > capacity:
        /// excessQi = qiInput - capacity
        /// backlashDamage = floor(excessQi × 0.5)      // Урон практику
        /// targetDamage = isMelee ? floor(qiInput × 0.5) : 0  // Урон по цели
        /// dissipatedQi = excessQi                     // Рассеянное Ци
        /// 
        /// Источник: TECHNIQUE_SYSTEM.md §"Дестабилизация"
        /// "Переполнение ВОЗМОЖНО, но с последствиями.
        ///  Для ranged атак: Ци разлетается во все стороны, урона по цели НЕТ."
        /// </summary>
        public static (int backlashDamage, int targetDamage, int dissipatedQi) CalculateDestabilization(
            int qiInput,
            int capacity,
            bool isMelee)
        {
            if (qiInput <= capacity)
            {
                return (0, 0, 0);
            }
            
            int excessQi = qiInput - capacity;
            
            // Урон практику
            int backlashDamage = (int)(excessQi * 0.5f);
            
            // Урон цели (только для melee!)
            // Для ranged: Ци разлетается, урона нет
            int targetDamage = isMelee ? (int)(qiInput * 0.5f) : 0;
            
            // Рассеянное Ци
            int dissipatedQi = excessQi;
            
            return (backlashDamage, targetDamage, dissipatedQi);
        }
        
        /// <summary>
        /// Рассчитать время каста техники.
        /// 
        /// Источник: ALGORITHMS.md §12 "Время каста техник"
        /// 
        /// Формула:
        /// baseTime = qiCost / conductivity
        /// effectiveSpeed = conductivity × (1 + cultivationBonus) × (1 + masteryBonus)
        /// effectiveTime = max(0.1, qiCost / effectiveSpeed)
        /// 
        /// Бонусы:
        /// - Уровень культивации: +5% за уровень выше 1
        /// - Мастерство техники: +1% за 1% мастерства
        /// 
        /// Пример:
        /// qiCost=50, conductivity=2.0, level=3, mastery=50%
        /// cultivationBonus = (3-1) × 0.05 = 0.10
        /// masteryBonus = 0.50
        /// effectiveSpeed = 2.0 × 1.10 × 1.50 = 3.3
        /// baseTime = 50 / 2.0 = 25 сек
        /// effectiveTime = 50 / 3.3 = 15.15 сек
        /// </summary>
        public static float CalculateCastTime(
            long qiCost,
            float conductivity,
            int cultivationLevel,
            float mastery)
        {
            // Бонус от уровня культивации: +5% за уровень выше 1
            float cultivationBonus = (cultivationLevel - 1) * 0.05f;
            
            // Бонус от мастерства: +1% за 1% мастерства
            float masteryBonus = mastery / 100f;
            
            // Эффективная скорость
            float effectiveSpeed = conductivity * (1f + cultivationBonus) * (1f + masteryBonus);
            
            // Время (минимум 0.1 сек)
            return Math.Max(0.1f, (float)qiCost / effectiveSpeed);
        }
        
        /// <summary>
        /// Проверить, можно ли использовать технику.
        /// </summary>
        public static bool CanUseTechnique(
            long currentQi,
            long qiCost,
            int cultivationLevel,
            int requiredLevel)
        {
            return currentQi >= qiCost && cultivationLevel >= requiredLevel;
        }
    }
}
