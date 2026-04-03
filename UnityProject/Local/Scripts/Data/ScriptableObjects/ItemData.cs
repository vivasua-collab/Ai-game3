// ============================================================================
// ItemData.cs — Данные предмета
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:05:48 UTC
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
        [Range(1, 3)]
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
    }
    
    /// <summary>
    /// Данные экипировки (оружие, броня, аксессуары)
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment", menuName = "Cultivation/Equipment")]
    public class EquipmentData : ItemData
    {
        [Header("Equipment")]
        [Tooltip("Слот экипировки")]
        public EquipmentSlot slot;
        
        [Tooltip("Слои (для принципа матрёшка)")]
        public List<EquipmentLayer> layers = new List<EquipmentLayer>();
        
        [Header("Stats")]
        [Tooltip("Урон (для оружия)")]
        public int damage = 0;
        
        [Tooltip("Защита (для брони)")]
        public int defense = 0;
        
        [Tooltip("Покрытие брони (%)")]
        [Range(0f, 100f)]
        public float coverage = 100f;
        
        [Tooltip("Снижение урона (%)")]
        [Range(0f, 80f)]
        public float damageReduction = 0f;
        
        [Tooltip("Бонус к уклонению (%)")]
        [Range(-50f, 50f)]
        public float dodgeBonus = 0f;
        
        [Header("Material")]
        [Tooltip("ID материала")]
        public string materialId;
        
        [Tooltip("Тир материала")]
        [Range(1, 5)]
        public int materialTier = 1;
        
        [Tooltip("Грейд экипировки")]
        public EquipmentGrade grade = EquipmentGrade.Common;
        
        [Tooltip("Уровень предмета (1-9)")]
        [Range(1, 9)]
        public int itemLevel = 1;
        
        [Header("Bonuses")]
        [Tooltip("Бонусы к характеристикам")]
        public List<StatBonus> statBonuses = new List<StatBonus>();
        
        [Tooltip("Особые эффекты")]
        public List<SpecialEffect> specialEffects = new List<SpecialEffect>();
    }
    
    /// <summary>
    /// Данные материала для крафта
    /// </summary>
    [CreateAssetMenu(fileName = "Material", menuName = "Cultivation/Material")]
    public class MaterialData : ItemData
    {
        [Header("Material")]
        [Tooltip("Тир материала")]
        [Range(1, 5)]
        public int tier = 1;
        
        [Tooltip("Категория материала")]
        public MaterialCategory materialCategory;
        
        [Header("Properties")]
        [Tooltip("Твёрдость")]
        [Range(1, 100)]
        public int hardness = 10;
        
        [Tooltip("Прочность")]
        [Range(1, 100)]
        public int durability = 50;
        
        [Tooltip("Проводимость Ци")]
        [Range(0.1f, 10f)]
        public float conductivity = 0.5f;
        
        [Header("Bonuses")]
        [Tooltip("Бонус к урону при использовании")]
        public float damageBonus = 0f;
        
        [Tooltip("Бонус к защите при использовании")]
        public float defenseBonus = 0f;
        
        [Tooltip("Бонус к проведению Ци")]
        public float qiConductivityBonus = 0f;
        
        [Header("Source")]
        [Tooltip("Где добывается")]
        [TextArea(2, 3)]
        public string source;
        
        [Tooltip("Шанс выпадения (%)")]
        [Range(0.01f, 100f)]
        public float dropChance = 10f;
        
        [Tooltip("Минимальный уровень культивации для добычи")]
        [Range(1, 10)]
        public int requiredLevel = 1;
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
