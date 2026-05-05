// ============================================================================
// ItemData.cs — Данные предмета (базовый класс)
// Cultivation World Simulator
// Версия: 2.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-27 18:06:00 UTC — строчная модель: убраны sizeWidth/sizeHeight
// Редактировано: 2026-05-05 09:55:00 UTC — С-07: StatBonus удалён (используйте CultivationGame.Data.StatBonus)
// ============================================================================
//
// ВЕРСИЯ 2.0: Убраны sizeWidth/sizeHeight — строчная модель инвентаря.
// Каждый предмет = 1 строка. Ограничители: масса + объём.
// Объём (volume) и флаг вложения (allowNesting) уже были добавлены ранее.
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Базовые данные предмета.
    /// Создаётся как ScriptableObject для каждого типа предмета.
    /// Версия 2.0: строчная модель инвентаря (нет сетки, масса + объём).
    /// </summary>
    [CreateAssetMenu(fileName = "Item", menuName = "Cultivation/Item")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Уникальный ID предмета")]
        public string itemId;

        [Tooltip("Название на русском")]
        public string nameRu;

        [Tooltip("Название на английском")]
        public string nameEn;

        [TextArea(2, 4)]
        [Tooltip("Описание предмета")]
        public string description;

        [Header("Classification")]
        [Tooltip("Категория предмета")]
        public ItemCategory category;

        [Tooltip("Тип предмета (детальная классификация)")]
        public string itemType;

        [Tooltip("Редкость")]
        public ItemRarity rarity;

        [Tooltip("Иконка")]
        public Sprite icon;

        [Header("Stacking")]
        [Tooltip("Можно стакать")]
        public bool stackable = true;

        [Tooltip("Максимум в стаке")]
        [Range(1, 999)]
        public int maxStack = 99;

        [Header("Physical")]
        [Tooltip("Вес (кг)")]
        public float weight = 0.1f;

        [Tooltip("Объём (литры) — определяет вместимость в рюкзак/хранилище")]
        public float volume = 1.0f;

        [Tooltip("Стоимость (духовные камни)")]
        public int value = 1;

        [Header("Durability")]
        [Tooltip("Имеет прочность")]
        public bool hasDurability = false;

        [Tooltip("Максимальная прочность")]
        public int maxDurability = 100;

        [Header("Effects")]
        [Tooltip("Эффекты при использовании")]
        public List<ItemEffect> effects = new List<ItemEffect>();

        [Header("Requirements")]
        [Tooltip("Минимальный уровень культивации")]
        [Range(0, 10)]
        public int requiredCultivationLevel = 0;

        [Tooltip("Требования к характеристикам")]
        public List<StatRequirement> statRequirements = new List<StatRequirement>();

        [Header("Storage")]
        [Tooltip("Куда можно поместить предмет (флаг вложения)")]
        public NestingFlag allowNesting = NestingFlag.Any;
    }

    // === Helper Classes ===

    [System.Serializable]
    public class ItemEffect
    {
        public string effectType;
        public float value;
        public int duration;
    }

    [System.Serializable]
    public class StatRequirement
    {
        public string statName;
        public int minValue;
    }

    // С-07: StatBonus перенесён в CultivationGame.Data.StatBonus (Scripts/Data/StatBonus.cs)
    // Старый StatBonus здесь удалён — используйте using CultivationGame.Data;
    // Если в старых SO-ассетах было поле bonus, оно переименовано в value

    [System.Serializable]
    public class SpecialEffect
    {
        public string effectName;
        public string description;
        public float triggerChance;
    }
}
