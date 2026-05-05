// ============================================================================
// MaterialData.cs — Данные материала для крафта
// Cultivation World Simulator
// Создано: 2026-04-13 14:03:25 UTC
// Редактировано: 2026-05-05 09:55:00 UTC — В-15: добавлены flexibility и qiRetention
// ============================================================================
//
// ВЫНЕСЕН ИЗ ItemData.cs — Unity требует совпадение имени файла и класса
// для ScriptableObject с [CreateAssetMenu]. До этого MaterialData был
// определён внутри ItemData.cs, что вызывало "No script asset for MaterialData".
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
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
        
        [Header("Дополнительные свойства")]
        [Tooltip("Гибкость (EQUIPMENT_SYSTEM.md §3.2)")]
        [Range(0f, 1f)]
        public float flexibility = 0.5f;    // В-15: Гибкость материала
        
        [Tooltip("Удержание Ци % (EQUIPMENT_SYSTEM.md §3.2)")]
        [Range(75f, 100f)]
        public float qiRetention = 90f;     // В-15: Удержание Ци %
        
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
}
