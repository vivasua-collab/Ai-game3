// ============================================================================
// StatBonus.cs — Единый класс бонуса к характеристике
// Cultivation World Simulator
// Создано: 2026-05-05 09:55:00 UTC — С-07: объединение дублирующихся StatBonus
// ============================================================================
//
// С-07: Раньше StatBonus существовал в двух местах:
//   1. CultivationGame.Data.ScriptableObjects.StatBonus (ItemData.cs) — поле bonus
//   2. CultivationGame.Generators.StatBonus (WeaponGenerator.cs) — поле value
// Теперь используется единый класс в CultivationGame.Data с полем value.
//
// ItemData.cs — старое поле bonus переименовано в value
// WeaponGenerator.cs — локальный StatBonus удалён, используется этот
// EquipmentSOFactory — обновлён маппинг bonus → value
// ============================================================================

using System;

namespace CultivationGame.Data
{
    /// <summary>
    /// Единый класс бонуса к характеристике.
    /// Используется в ItemData, EquipmentData, WeaponGenerator и т.д.
    /// С-07: Объединяет дублирующиеся определения из ItemData.cs и WeaponGenerator.cs.
    /// </summary>
    [Serializable]
    public class StatBonus
    {
        /// <summary>Название характеристики</summary>
        public string statName;

        /// <summary>Значение бонуса (абсолютное или процентное)</summary>
        public float value;

        /// <summary>Является ли бонус процентным (true = +X%, false = +X)</summary>
        public bool isPercentage;
    }
}
