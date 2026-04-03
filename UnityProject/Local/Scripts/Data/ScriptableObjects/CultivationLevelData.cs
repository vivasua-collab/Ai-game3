// ============================================================================
// CultivationLevelData.cs — Данные уровня культивации
// Cultivation World Simulator
// Версия: 1.1 — Добавлен динамический расчёт прорывов
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:05:48 UTC
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные уровня культивации.
    /// Создаётся как ScriptableObject для каждого из 10 уровней.
    /// </summary>
    [CreateAssetMenu(fileName = "CultivationLevel", menuName = "Cultivation/Cultivation Level")]
    public class CultivationLevelData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Уровень культивации (1-10)")]
        [Range(1, 10)]
        public int level;
        
        [Tooltip("Название на русском")]
        public string nameRu;
        
        [Tooltip("Название на английском")]
        public string nameEn;
        
        [TextArea(3, 5)]
        [Tooltip("Описание уровня")]
        public string description;
        
        [Header("Qi Parameters")]
        [Tooltip("Плотность Ци (множитель)")]
        public int qiDensity = 1;
        
        [Tooltip("Множитель роста ёмкости ядра")]
        public float coreCapacityMultiplier = 1.0f;
        
        [Tooltip("Базовая ёмкость ядра на этом уровне")]
        public long baseCoreCapacity = 1000;
        
        [Header("Body Effects")]
        [Tooltip("Множитель старения (1.0 = норма, 0.0 = остановлено)")]
        [Range(0f, 1f)]
        public float agingMultiplier = 1.0f;
        
        [Tooltip("Множитель регенерации")]
        public float regenerationMultiplier = 1.0f;
        
        [Tooltip("Множитель проводимости")]
        public float conductivityMultiplier = 1.0f;
        
        [Header("Abilities")]
        [Tooltip("Способности, доступные на этом уровне")]
        [TextArea(5, 10)]
        public string abilitiesDescription;
        
        [Tooltip("Может жить без еды")]
        public bool noFoodRequired = false;
        
        [Tooltip("Может жить без воды")]
        public bool noWaterRequired = false;
        
        [Tooltip("Может летать/парить")]
        public bool canFly = false;
        
        [Tooltip("Регенерация конечностей")]
        public bool canRegenerateLimbs = false;
        
        [Header("Breakthrough")]
        [Tooltip("Использовать динамический расчёт (coreCapacity × множитель)")]
        public bool useDynamicBreakthroughCalculation = true;
        
        [Tooltip("Множитель Ци для под-уровня (по умолчанию 10 = coreCapacity × 10)")]
        public int subLevelMultiplier = 10;
        
        [Tooltip("Множитель Ци для уровня (по умолчанию 100 = coreCapacity × 100)")]
        public int levelMultiplier = 100;
        
        [Tooltip("Ци для прорыва на следующий под-уровень (если dynamic = false)")]
        public long qiForSubLevelBreakthrough = 10000;
        
        [Tooltip("Ци для прорыва на следующий основной уровень (если dynamic = false)")]
        public long qiForLevelBreakthrough = 100000;
        
        [Tooltip("Шанс неудачи прорыва (%)")]
        [Range(0f, 100f)]
        public float breakthroughFailureChance = 10f;
        
        [Tooltip("Урон при неудаче прорыва (%)")]
        [Range(0f, 100f)]
        public float breakthroughFailureDamage = 20f;
        
        // === Runtime Methods ===
        
        /// <summary>
        /// Получить Ци для прорыва под-уровня.
        /// По документации: coreCapacity × 10
        /// </summary>
        public long GetQiForSubLevelBreakthrough(long currentCoreCapacity)
        {
            if (useDynamicBreakthroughCalculation)
            {
                return currentCoreCapacity * subLevelMultiplier;
            }
            return qiForSubLevelBreakthrough;
        }
        
        /// <summary>
        /// Получить Ци для прорыва уровня.
        /// По документации: coreCapacity × 100
        /// </summary>
        public long GetQiForLevelBreakthrough(long currentCoreCapacity)
        {
            if (useDynamicBreakthroughCalculation)
            {
                return currentCoreCapacity * levelMultiplier;
            }
            return qiForLevelBreakthrough;
        }
    }
}
