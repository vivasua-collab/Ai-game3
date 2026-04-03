// ============================================================================
// BuffData.cs — Данные баффа/дебаффа
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 09:20:39 UTC
// Редактировано: 2026-04-03 09:20:39 UTC
// ============================================================================
//
// Баффы и дебаффы для техник, предметов, окружения
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Тип баффа
    /// </summary>
    public enum BuffType
    {
        Buff,           // Положительный эффект
        Debuff,         // Отрицательный эффект
        Neutral         // Нейтральный эффект
    }

    /// <summary>
    /// Способ удаления баффа
    /// </summary>
    public enum BuffRemovalType
    {
        Time,           // По истечении времени
        Action,         // При действии
        Combat,         // При выходе из боя
        Rest,           // При отдыхе
        Manual          // Только вручную
    }

    /// <summary>
    /// Данные баффа/дебаффа.
    /// Создаётся как ScriptableObject для каждого типа эффекта.
    /// </summary>
    [CreateAssetMenu(fileName = "Buff", menuName = "Cultivation/Buff")]
    public class BuffData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Уникальный ID баффа")]
        public string buffId;
        
        [Tooltip("Название на русском")]
        public string nameRu;
        
        [Tooltip("Название на английском")]
        public string nameEn;
        
        [TextArea(2, 4)]
        [Tooltip("Описание")]
        public string description;
        
        [Tooltip("Иконка")]
        public Sprite icon;
        
        [Header("Classification")]
        [Tooltip("Тип баффа")]
        public BuffType buffType = BuffType.Buff;
        
        [Tooltip("Категория")]
        public BuffCategory category = BuffCategory.General;
        
        [Tooltip("Элемент (если элементальный)")]
        public Element element = Element.Neutral;
        
        [Tooltip("Можно сложить (stack)")]
        public bool stackable = false;
        
        [Tooltip("Максимум стаков")]
        [Range(1, 10)]
        public int maxStacks = 1;
        
        [Tooltip("Тип сложения")]
        public StackType stackType = StackType.Refresh;
        
        [Header("Duration")]
        [Tooltip("Длительность (тики, 0 = постоянный)")]
        public int durationTicks = 100;
        
        [Tooltip("Тип удаления")]
        public BuffRemovalType removalType = BuffRemovalType.Time;
        
        [Tooltip("Действует в бою")]
        public bool activeInCombat = true;
        
        [Tooltip("Действует вне боя")]
        public bool activeOutsideCombat = true;
        
        [Header("Visual")]
        [Tooltip("Цвет иконки")]
        public Color buffColor = Color.white;
        
        [Tooltip("VFX при наложении")]
        public GameObject applyVfx;
        
        [Tooltip("VFX при действии")]
        public GameObject activeVfx;
        
        [Tooltip("VFX при снятии")]
        public GameObject removeVfx;
        
        [Header("Effects")]
        [Tooltip("Модификаторы характеристик")]
        public List<StatModifier> statModifiers = new List<StatModifier>();
        
        [Tooltip("Периодические эффекты")]
        public List<PeriodicEffect> periodicEffects = new List<PeriodicEffect>();
        
        [Tooltip("Особые эффекты")]
        public List<SpecialBuffEffect> specialEffects = new List<SpecialBuffEffect>();
        
        [Header("Interactions")]
        [Tooltip("Баффы, которые отменяет")]
        public List<BuffData> dispels = new List<BuffData>();
        
        [Tooltip("Баффы, с которыми несовместим")]
        public List<BuffData> incompatibleWith = new List<BuffData>();
        
        [Tooltip("Баффы, которые усиливают")]
        public List<BuffData> enhancedBy = new List<BuffData>();
        
        // === Runtime Methods ===
        
        /// <summary>
        /// Получить модификатор характеристики
        /// </summary>
        public float GetStatModifier(string statName)
        {
            float total = 0f;
            foreach (var mod in statModifiers)
            {
                if (mod.statName == statName)
                {
                    total += mod.isPercentage ? mod.value / 100f : mod.value;
                }
            }
            return total;
        }
        
        /// <summary>
        /// Является ли бафф положительным
        /// </summary>
        public bool IsPositive => buffType == BuffType.Buff;
        
        /// <summary>
        /// Является ли бафф постоянным
        /// </summary>
        public bool IsPermanent => durationTicks == 0;
    }
    
    /// <summary>
    /// Категория баффа
    /// </summary>
    public enum BuffCategory
    {
        General,        // Общий
        Combat,         // Боевой
        Cultivation,    // Культивация
        Elemental,      // Элементальный
        Poison,         // Яд
        Curse,          // Проклятие
        Blessing,       // Благословение
        Transformation, // Трансформация
        Environment     // Окружение
    }
    
    /// <summary>
    /// Тип сложения стаков
    /// </summary>
    public enum StackType
    {
        Refresh,        // Обновлять длительность
        Add,            // Добавлять длительность
        Independent     // Независимые баффы
    }
    
    /// <summary>
    /// Модификатор характеристики
    /// </summary>
    [System.Serializable]
    public class StatModifier
    {
        [Tooltip("Имя характеристики")]
        public string statName;
        
        [Tooltip("Значение")]
        public float value;
        
        [Tooltip("В процентах")]
        public bool isPercentage = false;
        
        [Tooltip("Множитель за стак")]
        public float stackMultiplier = 1.0f;
    }
    
    /// <summary>
    /// Периодический эффект
    /// </summary>
    [System.Serializable]
    public class PeriodicEffect
    {
        [Tooltip("Тип эффекта")]
        public PeriodicType effectType;
        
        [Tooltip("Интервал (тики)")]
        public int intervalTicks = 10;
        
        [Tooltip("Значение")]
        public float value;
        
        [Tooltip("Шанс срабатывания (%)")]
        [Range(0f, 100f)]
        public float triggerChance = 100f;  // Редактировано: 2026-04-03 - добавлено поле
        
        [Tooltip("Масштабируется от характеристики")]
        public string scalingStat;
        
        [Tooltip("Коэффициент масштабирования")]
        public float scalingCoefficient = 0f;
    }
    
    /// <summary>
    /// Тип периодического эффекта
    /// </summary>
    public enum PeriodicType
    {
        Damage,         // Урон
        Heal,           // Лечение
        QiRestore,      // Восстановление Ци
        QiDrain,        // Поглощение Ци
        StatChange      // Изменение характеристики
    }
    
    /// <summary>
    /// Особый эффект баффа
    /// </summary>
    [System.Serializable]
    public class SpecialBuffEffect
    {
        [Tooltip("Тип эффекта")]
        public SpecialEffectType effectType;
        
        [Tooltip("Шанс срабатывания (%)")]
        [Range(0f, 100f)]
        public float triggerChance = 100f;
        
        [Tooltip("Параметры эффекта")]
        public string parameters;
    }
    
    /// <summary>
    /// Тип особого эффекта
    /// </summary>
    public enum SpecialEffectType
    {
        Stun,           // Оглушение
        Slow,           // Замедление
        Root,           // Обездвиживание
        Silence,        // Немота
        Blind,          // Слепота
        Immunity,       // Иммунитет
        Reflect,        // Отражение
        Absorb,         // Поглощение
        Shield,         // Щит
        Regeneration,   // Регенерация
        Lifesteal,      // Вампиризм
        Thorns          // Шипы
    }
}
