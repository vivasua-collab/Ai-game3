// ============================================================================
// BackpackData.cs — Данные рюкзака
// Cultivation World Simulator
// Создано: 2026-04-18 18:43:19 UTC
// ============================================================================
//
// Рюкзак НЕ экипируется на куклу — отдельная система персонажа.
// Определяет размер сетки инвентаря и бонусы веса.
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные рюкзака. Определяет размер сетки инвентаря и бонусы веса.
    /// Рюкзак НЕ экипируется на куклу — отдельная система персонажа.
    /// </summary>
    [CreateAssetMenu(fileName = "Backpack", menuName = "Cultivation/Backpack")]
    public class BackpackData : ItemData
    {
        [Header("Backpack Grid")]
        [Tooltip("Ширина сетки (слотов)")]
        [Range(3, 10)]
        public int gridWidth = 3;

        [Tooltip("Высота сетки (слотов)")]
        [Range(3, 8)]
        public int gridHeight = 4;

        [Header("Weight Bonuses")]
        [Tooltip("Снижение веса содержимого (%)")]
        [Range(0f, 50f)]
        public float weightReduction = 0f;

        [Tooltip("Бонус к максимальному весу (кг)")]
        public float maxWeightBonus = 0f;

        [Header("Belt")]
        [Tooltip("Дополнительные слоты пояса (0-4)")]
        [Range(0, 4)]
        public int beltSlots = 0;

        // Вычисляемые свойства
        /// <summary>Общее количество слотов сетки</summary>
        public int TotalSlots => gridWidth * gridHeight;
    }
}
