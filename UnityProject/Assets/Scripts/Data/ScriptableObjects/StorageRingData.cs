// ============================================================================
// StorageRingData.cs — Данные кольца хранения
// Cultivation World Simulator
// Создано: 2026-04-18 18:43:19 UTC
// ============================================================================
//
// Кольцо хранения — объём-ограниченное хранилище, экипируется на слот
// кольца на кукле. Стоимость доступа = Qi × volume.
// allowNesting для самого кольца = None (пространственная нестабильность).
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные кольца хранения. Объём-ограниченное хранилище,
    /// экипируется на слот кольца на кукле.
    /// </summary>
    [CreateAssetMenu(fileName = "StorageRing", menuName = "Cultivation/Storage Ring")]
    public class StorageRingData : ItemData
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
