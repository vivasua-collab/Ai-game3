// ============================================================================
// ElementData.cs — Данные элемента (стихии)
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:05:48 UTC
// Редактировано: 2026-05-05 09:55:00 UTC — В-12: oppositeElement→oppositeElements (Void имеет 2 противоположности)
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные элемента (стихии).
    /// </summary>
    [CreateAssetMenu(fileName = "Element", menuName = "Cultivation/Element")]
    public class ElementData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Тип элемента")]
        public Element elementType;
        
        [Tooltip("Название на русском")]
        public string nameRu;
        
        [Tooltip("Название на английском")]
        public string nameEn;
        
        [Tooltip("Символ/иконка")]
        public Sprite icon;
        
        [Tooltip("Цвет элемента")]
        public Color color = Color.white;
        
        [TextArea(2, 3)]
        [Tooltip("Описание элемента")]
        public string description;
        
        [Header("Relationships")]
        [Tooltip("Противоположные элементы (список: Void имеет 2 — Lightning и Light)")]
        public List<Element> oppositeElements = new List<Element>();
        
        [Obsolete("Используйте oppositeElements (List<Element>) вместо oppositeElement. Void имеет 2 противоположности.")]
        [Tooltip("Противоположный элемент (устарело — используйте oppositeElements)")]
        public Element oppositeElement;
        
        [Tooltip("Элементы сродства (ослабляют)")]
        public List<Element> affinityElements = new List<Element>();
        
        [Tooltip("Элементы, к которым уязвим")]
        public List<Element> weakToElements = new List<Element>();
        
        [Header("Damage Multipliers")]
        [Tooltip("Множитель урона по противоположному элементу")]
        public float oppositeMultiplier = 1.5f;
        
        [Tooltip("Множитель урона по элементам сродства")]
        public float affinityMultiplier = 0.8f;
        
        [Tooltip("Множитель урона от Void")]
        public float voidMultiplier = 1.2f;
        
        [Header("Effects")]
        [Tooltip("Возможные эффекты элемента")]
        public List<ElementEffect> possibleEffects = new List<ElementEffect>();
        
        [Header("Environment")]
        [Tooltip("Влияет на окружение")]
        public bool affectsEnvironment = false;
        
        [Tooltip("Эффекты на местности")]
        public List<EnvironmentEffect> environmentEffects = new List<EnvironmentEffect>();
    }
    
    /// <summary>
    /// Эффект элемента
    /// </summary>
    [System.Serializable]
    public class ElementEffect
    {
        [Tooltip("Название эффекта")]
        public string effectName;
        
        [Tooltip("Описание")]
        [TextArea(1, 2)]
        public string description;
        
        [Tooltip("Длительность (тики)")]
        public int baseDuration = 10;
        
        [Tooltip("Множитель урона от эффекта")]
        public float damageMultiplier = 0.1f;
        
        [Tooltip("Шанс наложения (%)")]
        [Range(0f, 100f)]
        public float applyChance = 50f;
    }
    
    /// <summary>
    /// Эффект на окружение
    /// </summary>
    [System.Serializable]
    public class EnvironmentEffect
    {
        public string effectName;
        public string description;
        public int radius;
        public int duration;
    }
}
