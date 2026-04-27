// ============================================================================
// StorageRingData.cs — Данные кольца хранения
// Cultivation World Simulator
// Создано: 2026-04-18 18:43:19 UTC
// Редактировано: 2026-04-20 06:45:00 UTC — наследование от EquipmentData (FIX CS8121/CS0184)
// ============================================================================
//
// Кольцо хранения — объём-ограниченное хранилище, экипируется на слот
// кольца на кукле. Стоимость доступа = Qi × volume.
// allowNesting для самого кольца = None (пространственная нестабильность).
//
// ВАЖНО: StorageRingData наследует от EquipmentData (НЕ ItemData), т.к.
// кольца хранения экипируются на слоты (RingLeft1/2, RingRight1/2) и должны
// проходить через EquipmentController.Equip(). Это исправляет ошибки:
//   CS8121 — pattern match EquipmentData is StorageRingData
//   CS0184 — expression never StorageRingData
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные кольца хранения. Объём-ограниченное хранилище,
    /// экипируется на слот кольца на кукле.
    /// Наследует от EquipmentData, т.к. является экипируемым предметом.
    /// </summary>
    [CreateAssetMenu(fileName = "StorageRing", menuName = "Cultivation/Storage Ring")]
    public class StorageRingData : EquipmentData
    {
        [Header("Storage Ring")]
        [Tooltip("Максимальный объём хранения")]
        public float maxVolume = 5f;

        [Tooltip("Базовая стоимость Qi для доступа")]
        public int qiCostBase = 5;

        [Tooltip("Стоимость Qi за единицу объёма")]
        public float qiCostPerUnit = 2f;

        [Tooltip("Время доступа (сек)")]
        [Range(0.5f, 5f)]
        public float accessTime = 1.5f;
    }
}
