// ============================================================================
// MortalStageData.cs — Данные этапа развития смертного
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:05:48 UTC
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные этапа развития смертного (до пробуждения ядра).
    /// Отдельная система от CultivationLevelData.
    /// </summary>
    [CreateAssetMenu(fileName = "MortalStage", menuName = "Cultivation/Mortal Stage")]
    public class MortalStageData : ScriptableObject
    {
        #region Basic Info

        [Header("Basic Info")]
        [Tooltip("Этап развития смертного")]
        public MortalStage stage;

        [Tooltip("Название на русском")]
        public string nameRu;

        [Tooltip("Название на английском")]
        public string nameEn;

        [TextArea(3, 5)]
        [Tooltip("Описание этапа")]
        public string description;

        #endregion

        #region Age

        [Header("Age Range")]
        [Tooltip("Минимальный возраст (лет)")]
        [Range(0, 100)]
        public int minAge = 0;

        [Tooltip("Максимальный возраст (лет)")]
        [Range(0, 150)]
        public int maxAge = 7;

        #endregion

        #region Core Formation

        [Header("Dormant Core Formation")]
        [Tooltip("Минимальная сформированность ядра (%)")]
        [Range(0f, 100f)]
        public float minCoreFormation = 0f;

        [Tooltip("Максимальная сформированность ядра (%)")]
        [Range(0f, 100f)]
        public float maxCoreFormation = 30f;

        [Tooltip("Скорость формирования ядра (% в год)")]
        [Range(0f, 10f)]
        public float coreFormationRate = 4f;

        #endregion

        #region Qi Capacity

        [Header("Qi Capacity")]
        [Tooltip("Минимальная естественная ёмкость Ци")]
        [Range(1, 500)]
        public int minQiCapacity = 1;

        [Tooltip("Максимальная естественная ёмкость Ци")]
        [Range(1, 500)]
        public int maxQiCapacity = 30;

        [Tooltip("Множитель поглощения Ци из среды")]
        [Range(0f, 1f)]
        public float qiAbsorptionRate = 0.01f;

        [Tooltip("Может ли регенерировать Ци (только для пробудившихся)")]
        public bool canRegenerateQi = false;

        #endregion

        #region Stats

        [Header("Base Stats Range")]
        [Tooltip("Минимальная сила")]
        [Range(0f, 20f)]
        public float minStrength = 0.1f;

        [Tooltip("Максимальная сила")]
        [Range(0f, 20f)]
        public float maxStrength = 5f;

        [Tooltip("Минимальная ловкость")]
        [Range(0f, 20f)]
        public float minAgility = 0.1f;

        [Tooltip("Максимальная ловкость")]
        [Range(0f, 20f)]
        public float maxAgility = 5f;

        [Tooltip("Минимальная выносливость")]
        [Range(0f, 20f)]
        public float minConstitution = 0.1f;

        [Tooltip("Максимальная выносливость")]
        [Range(0f, 20f)]
        public float maxConstitution = 5f;

        #endregion

        #region Awakening

        [Header("Awakening")]
        [Tooltip("Базовый шанс пробуждения (%)")]
        [Range(0f, 5f)]
        public float baseAwakeningChance = 0f;

        [Tooltip("Шанс пробуждения в зоне высокой плотности Ци (%)")]
        [Range(0f, 5f)]
        public float highDensityAwakeningChance = 0f;

        [Tooltip("Шанс пробуждения при критическом состоянии (%)")]
        [Range(0f, 5f)]
        public float criticalAwakeningChance = 0f;

        [Tooltip("Можно ли пробудиться на этом этапе")]
        public bool canAwaken = false;

        #endregion

        #region Abilities

        [Header("Abilities & Limitations")]
        [Tooltip("Требуется еда")]
        public bool requiresFood = true;

        [Tooltip("Требуется вода")]
        public bool requiresWater = true;

        [Tooltip("Требуется сон")]
        public bool requiresSleep = true;

        [Tooltip("Может обучаться боевым искусствам")]
        public bool canLearnMartialArts = false;

        [Tooltip("Может медитировать")]
        public bool canMeditate = false;

        [Tooltip("Описание способностей")]
        [TextArea(3, 6)]
        public string abilitiesDescription;

        #endregion

        #region Transition

        [Header("Transition")]
        [Tooltip("Следующий этап (None = последний)")]
        public MortalStage nextStage;

        [Tooltip("Является ли точкой пробуждения")]
        public bool isAwakeningPoint = false;

        #endregion

        #region Runtime Methods

        /// <summary>
        /// Получает случайную сформированность ядра для этого этапа
        /// </summary>
        public float GetRandomCoreFormation()
        {
            return UnityEngine.Random.Range(minCoreFormation, maxCoreFormation);
        }

        /// <summary>
        /// Получает случайную ёмкость Ци для этого этапа
        /// </summary>
        public int GetRandomQiCapacity()
        {
            return UnityEngine.Random.Range(minQiCapacity, maxQiCapacity + 1);
        }

        /// <summary>
        /// Вычисляет шанс пробуждения с учётом условий
        /// </summary>
        public float CalculateAwakeningChance(float coreFormation, bool highDensity, bool critical)
        {
            if (!canAwaken || coreFormation < 80f)
                return 0f;

            float chance = baseAwakeningChance;

            if (highDensity)
                chance = Mathf.Max(chance, highDensityAwakeningChance);

            if (critical)
                chance = Mathf.Max(chance, criticalAwakeningChance);

            // Бонус за высокую сформированность ядра
            if (coreFormation >= 90f)
                chance *= 1.5f;

            return chance;
        }

        /// <summary>
        /// Проверяет, подходит ли возраст для этого этапа
        /// </summary>
        public bool IsAgeInRange(int age)
        {
            return age >= minAge && age <= maxAge;
        }

        /// <summary>
        /// Получает прогресс формирования ядра для возраста
        /// </summary>
        public float GetCoreFormationForAge(int age)
        {
            if (age < minAge) return minCoreFormation;
            if (age > maxAge) return maxCoreFormation;

            float progress = (float)(age - minAge) / (maxAge - minAge);
            return Mathf.Lerp(minCoreFormation, maxCoreFormation, progress);
        }

        #endregion
    }
}
