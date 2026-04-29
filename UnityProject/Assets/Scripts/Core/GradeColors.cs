// ============================================================================
// GradeColors.cs — Цветовая палитра грейдов и тиров (единая точка доступа)
// Cultivation World Simulator
// Создано: 2026-04-29 08:55:00 UTC
// ============================================================================
//
// ТРИ ОСИ ЦВЕТА:
//   1. Grade  → фон иконки, текст грейда   (Д9)
//   2. Tier   → индикатор материала, яркость (Д10)
//   3. Rarity → рамка слота (существующая система InventorySlotUI.GetRarityColor)
//
// Grade ≠ Rarity. Grade = качество предмета, Rarity = редкость.
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Core
{
    /// <summary>
    /// Цветовая палитра грейдов и тиров.
    /// Единая точка доступа для всех UI-компонентов.
    /// </summary>
    public static class GradeColors
    {
        // === ЦВЕТА ГРЕЙДА (Д9) ===
        // Коричневый → Серый → Зелёный → Синий → Золотой

        /// Цвет фона иконки по грейду
        public static Color GetGradeColor(EquipmentGrade grade) => grade switch
        {
            EquipmentGrade.Damaged      => new Color(0.545f, 0.271f, 0.075f), // Коричнево-красный
            EquipmentGrade.Common       => new Color(0.612f, 0.639f, 0.686f), // Серый
            EquipmentGrade.Refined      => new Color(0.133f, 0.773f, 0.369f), // Зелёный
            EquipmentGrade.Perfect      => new Color(0.231f, 0.510f, 0.965f), // Синий
            EquipmentGrade.Transcendent => new Color(0.961f, 0.620f, 0.043f), // Золотой
            _ => Color.gray
        };

        /// Hex-строка для грейда (для отладки и логов)
        public static string GetGradeHex(EquipmentGrade grade) => grade switch
        {
            EquipmentGrade.Damaged      => "#8B4513",
            EquipmentGrade.Common       => "#9CA3AF",
            EquipmentGrade.Refined      => "#22C55E",
            EquipmentGrade.Perfect      => "#3B82F6",
            EquipmentGrade.Transcendent => "#F59E0B",
            _ => "#808080"
        };

        /// Название грейда на русском
        public static string GetGradeNameRu(EquipmentGrade grade) => grade switch
        {
            EquipmentGrade.Damaged      => "Повреждённый",
            EquipmentGrade.Common       => "Обычный",
            EquipmentGrade.Refined      => "Улучшенный",
            EquipmentGrade.Perfect      => "Совершенный",
            EquipmentGrade.Transcendent => "Превосходящий",
            _ => "???"
        };

        // === ЦВЕТА ТИРА (Д10) ===
        // Стальной → Зелёный → Голубой → Фиолетовый → Розовый

        /// Цвет индикатора тира материала
        public static Color GetTierColor(int tier) => tier switch
        {
            1 => new Color(0.545f, 0.616f, 0.686f), // Стальной серый (T1)
            2 => new Color(0.290f, 0.871f, 0.502f), // Светло-зелёный (T2)
            3 => new Color(0.376f, 0.647f, 0.980f), // Голубой (T3)
            4 => new Color(0.655f, 0.545f, 0.980f), // Фиолетовый (T4)
            5 => new Color(0.957f, 0.447f, 0.714f), // Розовый (T5)
            _ => Color.gray
        };

        /// Hex-строка для тира
        public static string GetTierHex(int tier) => tier switch
        {
            1 => "#8B9DAF",
            2 => "#4ADE80",
            3 => "#60A5FA",
            4 => "#A78BFA",
            5 => "#F472B6",
            _ => "#808080"
        };

        /// Название тира на русском
        public static string GetTierNameRu(int tier) => tier switch
        {
            1 => "Обычный",
            2 => "Качественный",
            3 => "Духовный",
            4 => "Небесный",
            5 => "Первородный",
            _ => "???"
        };

        // === КОМБИНИРОВАННАЯ ФОРМУЛА (Д11) ===

        /// Цвет фона иконки = Grade × яркость по Tier
        /// iconBg = GradeColor × (0.7 + 0.3 × tier/5)
        /// Чем выше тир, тем ярче фон иконки
        public static Color GetIconBgColor(EquipmentGrade grade, int tier)
        {
            Color gradeColor = GetGradeColor(grade);
            float brightness = 0.7f + 0.3f * (Mathf.Clamp(tier, 1, 5) / 5f);
            return gradeColor * brightness;
        }
    }
}
