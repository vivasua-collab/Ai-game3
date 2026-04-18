// ============================================================================
// ItemData.cs — Данные предмета (базовый класс)
// Cultivation World Simulator
// Версия: 1.1
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-18 18:43:19 UTC — +volume, +allowNesting, fix sizeHeight Range(1,2)
// ============================================================================
//
// РЕДАКТИРОВАНИЕ 2026-04-13: EquipmentData и MaterialData вынесены в отдельные файлы
//   (EquipmentData.cs и MaterialData.cs). Unity требует совпадение имени файла и
//   класса для ScriptableObject с [CreateAssetMenu], иначе возникает ошибка
//   "No script asset for EquipmentData/MaterialData".
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Базовые данные предмета.
    /// Создаётся как ScriptableObject для каждого типа предмета.
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
        
        [Header("Size")]
        [Tooltip("Ширина в сетке инвентаря")]
        [Range(1, 2)]
        public int sizeWidth = 1;
        
        [Tooltip("Высота в сетке инвентаря")]
        [Range(1, 2)]
        public int sizeHeight = 1;
        
        [Header("Physical")]
        [Tooltip("Вес (кг)")]
        public float weight = 0.1f;
        
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
        [Tooltip("Объём предмета (для колец хранения)")]
        public float volume = 1.0f;
        
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
    
    [System.Serializable]
    public class EquipmentLayer
    {
        public string layerName;
        public bool isOuterLayer = true;
    }
    
    [System.Serializable]
    public class StatBonus
    {
        public string statName;
        public float bonus;
        public bool isPercentage = false;
    }
    
    [System.Serializable]
    public class SpecialEffect
    {
        public string effectName;
        public string description;
        public float triggerChance;
    }
}
