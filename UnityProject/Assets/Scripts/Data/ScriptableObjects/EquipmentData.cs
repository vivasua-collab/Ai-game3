// ============================================================================
// EquipmentData.cs — Данные экипировки (оружие, броня, аксессуары)
// Cultivation World Simulator
// Создано: 2026-04-13 14:03:25 UTC
// Редактировано: 2026-04-18 18:43:19 UTC — +handType (WeaponHandType)
// ============================================================================
//
// ВЫНЕСЕН ИЗ ItemData.cs — Unity требует совпадение имени файла и класса
// для ScriptableObject с [CreateAssetMenu]. До этого EquipmentData был
// определён внутри ItemData.cs, что вызывало "No script asset for EquipmentData".
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные экипировки (оружие, броня, аксессуары)
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment", menuName = "Cultivation/Equipment")]
    public class EquipmentData : ItemData
    {
        [Header("Equipment")]
        [Tooltip("Слот экипировки")]
        public EquipmentSlot slot;
        
        [Tooltip("Тип хвата (одноручное/двуручное)")]
        public WeaponHandType handType = WeaponHandType.OneHand;
        
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
}
