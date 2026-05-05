// ============================================================================
// TechniqueData.cs — Данные техники культивации
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:05:48 UTC
// Редактировано: 2026-05-05 10:05:00 UTC
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные техники.
    /// Создаётся как ScriptableObject для каждой техники.
    /// </summary>
    [CreateAssetMenu(fileName = "Technique", menuName = "Cultivation/Technique")]
    public class TechniqueData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Уникальный ID техники")]
        public string techniqueId;
        
        [Tooltip("Название на русском")]
        public string nameRu;
        
        [Tooltip("Название на английском")]
        public string nameEn;
        
        [TextArea(3, 5)]
        [Tooltip("Описание техники")]
        public string description;
        
        [Tooltip("Иконка техники")]
        public Sprite icon;
        
        [Header("Classification")]
        [Tooltip("Тип техники")]
        public TechniqueType techniqueType;
        
        [Tooltip("Подтип (для боевых техник)")]
        public CombatSubtype combatSubtype;
        
        // В-09: Подтип защитной техники для HasActiveShield()
        [Tooltip("Подтип защитной техники (Shield активирует Shield-режим Qi Buffer)")]
        public DefenseSubtype defenseSubtype = DefenseSubtype.None;
        
        [Tooltip("Элемент техники")]
        public Element element;
        
        [Tooltip("Грейд техники (качество)")]
        public TechniqueGrade grade;
        
        [Tooltip("Уровень техники (1-9)")]
        [Range(1, 9)]
        public int techniqueLevel = 1;
        
        [Header("Leveling")]
        [Tooltip("Минимальный уровень развития")]
        [Range(1, 9)]
        public int minLevel = 1;
        
        [Tooltip("Максимальный уровень развития")]
        [Range(1, 9)]
        public int maxLevel = 9;
        
        [Tooltip("Можно развивать")]
        public bool canEvolve = true;
        
        [Header("Costs")]
        [Tooltip("Базовая стоимость Ци")]
        public long baseQiCost = 10; // FIX DAT-H01: int→long для Qi > 2.1B на L5+
        
        [Tooltip("Физическая усталость (%)")]
        [Range(0f, 100f)]
        public float physicalFatigueCost = 0f;
        
        [Tooltip("Ментальная усталость (%)")]
        [Range(0f, 100f)]
        public float mentalFatigueCost = 0f;
        
        [Tooltip("Кулдаун (тики)")]
        public int cooldown = 0;
        
        [Header("Capacity")]
        [Tooltip("Базовая ёмкость техники")]
        public int baseCapacity = 64;
        
        [Tooltip("Ultimate-техника")]
        public bool isUltimate = false;
        
        [Header("Scaling")]
        [Tooltip("Масштабирование от силы")]
        [Range(0f, 1f)]
        public float strengthScaling = 0f;
        
        [Tooltip("Масштабирование от ловкости")]
        [Range(0f, 1f)]
        public float agilityScaling = 0f;
        
        [Tooltip("Масштабирование от интеллекта")]
        [Range(0f, 1f)]
        public float intelligenceScaling = 0f;
        
        [Tooltip("Масштабирование от проводимости")]
        [Range(0f, 1f)]
        public float conductivityScaling = 0.5f;
        
        [Header("Requirements")]
        [Tooltip("Минимальный уровень культивации")]
        [Range(1, 10)]
        public int minCultivationLevel = 1;
        
        [Tooltip("Минимальная сила")]
        [Range(1, 100)]
        public int minStrength = 1;
        
        [Tooltip("Минимальная ловкость")]
        [Range(1, 100)]
        public int minAgility = 1;
        
        [Tooltip("Минимальный интеллект")]
        [Range(1, 100)]
        public int minIntelligence = 1;
        
        [Tooltip("Минимальная проводимость")]
        public float minConductivity = 1f;
        
        [Header("Effects")]
        [Tooltip("Эффекты техники")]
        public List<TechniqueEffect> effects = new List<TechniqueEffect>();
        
        [Header("Acquisition")]
        [Tooltip("Источники получения")]
        public List<string> sources = new List<string>();
        
        [Tooltip("Можно выучить из свитка")]
        public bool learnableFromScroll = true;
        
        [Tooltip("Можно получить от NPC")]
        public bool learnableFromNPC = true;
    }
    
    /// <summary>
    /// Эффект техники
    /// </summary>
    [System.Serializable]
    public class TechniqueEffect
    {
        [Tooltip("Тип эффекта")]
        public EffectType effectType;
        
        [Tooltip("Значение эффекта")]
        public float value;
        
        [Tooltip("Длительность (тики, 0 = мгновенный)")]
        public int duration = 0;
        
        [Tooltip("Шанс срабатывания (%)")]
        [Range(0f, 100f)]
        public float chance = 100f;
    }
    
    /// <summary>
    /// Тип эффекта техники
    /// </summary>
    public enum EffectType
    {
        Damage,             // Урон
        Heal,               // Лечение
        Buff,               // Бафф
        Debuff,             // Дебафф
        Shield,             // Щит
        Movement,           // Перемещение
        StatBoost,          // Усиление характеристики
        StatReduction,      // Снижение характеристики
        Elemental,          // Элементальный эффект
        Special             // Специальный эффект
    }
}
