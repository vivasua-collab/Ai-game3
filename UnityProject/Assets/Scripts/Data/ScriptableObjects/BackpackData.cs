// ============================================================================
// BackpackData.cs — Данные рюкзака (строчная модель v3.0)
// Cultivation World Simulator
// Создано: 2026-04-18 18:43:19 UTC
// Редактировано: 2026-04-27 18:06:00 UTC — строчная модель: grid→weight/volume
// ============================================================================
//
// ВЕРСИЯ 3.0: Строчная модель инвентаря.
// Убраны: gridWidth, gridHeight, TotalSlots (сеточная модель).
// Добавлены: maxWeight, maxVolume, ownWeight (строчная модель).
// Ограничители: масса (кг) + объём (литры), а не ячейки сетки.
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные рюкзака. Определяет лимиты массы и объёма инвентаря.
    /// Строчная модель: вместо сетки ячеек — список предметов с ограничителями.
    /// </summary>
    [CreateAssetMenu(fileName = "Backpack", menuName = "Cultivation/Backpack")]
    public class BackpackData : ItemData
    {
        [Header("Capacity (Line Model)")]
        [Tooltip("Максимальная масса содержимого (кг)")]
        public float maxWeight = 30f;

        [Tooltip("Максимальный объём содержимого (литры)")]
        public float maxVolume = 50f;

        [Tooltip("Собственный вес рюкзака (кг)")]
        public float ownWeight = 0.5f;

        [Header("Weight Bonuses")]
        [Tooltip("Снижение веса содержимого (%)")]
        [Range(0f, 50f)]
        public float weightReduction = 0f;

        [Tooltip("Бонус к базовому лимиту массы персонажа (кг)")]
        public float maxWeightBonus = 0f;

        [Header("Belt")]
        [Tooltip("Дополнительные слоты пояса (0-4)")]
        [Range(0, 4)]
        public int beltSlots = 0;
    }
}
