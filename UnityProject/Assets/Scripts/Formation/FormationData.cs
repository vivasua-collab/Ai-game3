// ============================================================================
// FormationData.cs — ScriptableObject формации
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 13:16:34 UTC
// ============================================================================
//
// Источник: docs/FORMATION_SYSTEM.md
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Formation
{
    /// <summary>
    /// Тип эффекта формации
    /// </summary>
    public enum FormationEffectType
    {
        Buff,           // Бафф (усиление)
        Debuff,         // Дебафф (ослабление)
        Damage,         // Урон
        Heal,           // Исцеление
        Control,        // Контроль (заморозка, замедление)
        Shield,         // Щит
        Summon          // Призыв
    }

    /// <summary>
    /// Тип контроля
    /// </summary>
    public enum ControlType
    {
        None,
        Freeze,         // Заморозка (полная остановка)
        Slow,           // Замедление
        Root,           // Обездвиживание
        Stun,           // Оглушение
        Silence,        // Молчание (блок техник)
        Blind           // Слепота
    }

    /// <summary>
    /// Типы баффов для формаций.
    /// Используется в FormationEffect для указания типа модификации.
    /// </summary>
    public enum BuffType
    {
        None,
        Damage,         // Урон
        Defense,        // Защита
        Speed,          // Скорость
        CriticalChance, // Шанс крита
        CriticalDamage, // Урон крита
        QiRegen,        // Регенерация Ци (только для формаций, не для постоянных баффов!)
        MaxQi,          // Максимальное Ци
        Conductivity,   // Проводимость (с payback механизмом)
        Health,         // Здоровье
        Stamina,        // Выносливость
        Resistance,     // Сопротивление
        Evasion         // Уклонение
    }

    /// <summary>
    /// Этап формации
    /// </summary>
    public enum FormationStage
    {
        None,           // Не инициализирована
        Drawing,        // Прорисовка контура
        Filling,        // Наполнение ёмкости
        Active,         // Работает
        Depleted        // Истощена
    }

    /// <summary>
    /// Эффект формации
    /// </summary>
    [Serializable]
    public class FormationEffect
    {
        [Header("Type")]
        public FormationEffectType effectType = FormationEffectType.Buff;

        [Header("Buff/Debuff")]
        public BuffType buffType = BuffType.Damage;
        public float value = 10f;
        public bool isPercentage = true;

        [Header("Damage/Heal")]
        public int tickValue = 0;
        public float tickInterval = 1f; // 0 = постоянный эффект

        [Header("Control")]
        public ControlType controlType = ControlType.Slow;
        public float controlDuration = 3f;

        [Header("Element")]
        public Element element = Element.Neutral;

        [Header("Visual")]
        public GameObject vfxPrefab;
        public AudioClip soundEffect;
    }

    /// <summary>
    /// Требование для изучения формации
    /// </summary>
    [Serializable]
    public class FormationRequirement
    {
        public int minCultivationLevel = 1;
        public int minFormationKnowledge = 0;
        public List<string> requiredTechniques = new List<string>();
        public List<string> prerequisiteFormations = new List<string>();
    }

    /// <summary>
    /// Данные формации.
    /// Создаётся как ScriptableObject для каждого типа формации.
    /// 
    /// Источник: FORMATION_SYSTEM.md
    /// </summary>
    [CreateAssetMenu(fileName = "FormationData", menuName = "Cultivation/Formation Data")]
    public class FormationData : ScriptableObject
    {
        #region Identity

        [Header("Identity")]
        [Tooltip("Уникальный ID формации")]
        public string formationId;

        [Tooltip("Название на русском")]
        public string displayName;

        [Tooltip("Название на английском")]
        public string nameEn;

        [TextArea(3, 6)]
        [Tooltip("Описание")]
        public string description;

        [Tooltip("Иконка")]
        public Sprite icon;

        #endregion

        #region Classification

        [Header("Classification")]
        [Tooltip("Тип формации")]
        public FormationType formationType = FormationType.Barrier;

        [Tooltip("Размер формации")]
        public FormationSize size = FormationSize.Small;

        [Tooltip("Уровень формации (1-9)")]
        [Range(1, 9)]
        public int level = 1;

        [Tooltip("Элемент формации")]
        public Element element = Element.Neutral;

        #endregion

        #region Qi Costs

        [Header("Qi Costs")]
        [Tooltip("Стоимость прорисовки контура (авто: 80 × 2^(level-1))")]
        public int contourQiOverride = 0; // 0 = автоматический расчёт

        [Tooltip("Время прорисовки (секунды)")]
        public float drawTime = 5f;

        [Tooltip("Кулдаун между использованиями")]
        public float cooldown = 60f;

        #endregion

        #region Area

        [Header("Area")]
        [Tooltip("Радиус прорисовки (метры)")]
        public float creationRadius = 10f;

        [Tooltip("Радиус действия (метры)")]
        public float effectRadius = 50f;

        [Tooltip("Высота формации (для 3D)")]
        public float height = 5f;

        #endregion

        #region Duration

        [Header("Duration")]
        [Tooltip("Требует физическое ядро?")]
        public bool requiresCore = false;

        [Tooltip("Постоянная формация (только со ядром)")]
        public bool isPermanent = false;

        [Tooltip("Базовая длительность (секунды, 0 = бесконечно)")]
        public float baseDuration = 300f;

        #endregion

        #region Effects

        [Header("Effects - Allies")]
        [Tooltip("Эффекты на союзников")]
        public List<FormationEffect> allyEffects = new List<FormationEffect>();

        [Header("Effects - Enemies")]
        [Tooltip("Эффекты на врагов")]
        public List<FormationEffect> enemyEffects = new List<FormationEffect>();

        [Header("Effects - General")]
        [Tooltip("Интервал применения эффектов")]
        public float effectTickInterval = 1f;

        [Tooltip("Применять при входе в зону")]
        public bool applyOnEnter = true;

        [Tooltip("Снимать при выходе из зоны")]
        public bool removeOnExit = true;

        #endregion

        #region Requirements

        [Header("Requirements")]
        public FormationRequirement requirements = new FormationRequirement();

        #endregion

        #region Core

        [Header("Core")]
        [Tooltip("Совместимые типы ядер")]
        public List<FormationCoreType> compatibleCores = new List<FormationCoreType>();

        [Tooltip("Требуемый мин. уровень ядра")]
        [Range(1, 9)]
        public int minCoreLevel = 1;

        [Tooltip("Требуемый макс. уровень ядра")]
        [Range(1, 9)]
        public int maxCoreLevel = 9;

        #endregion

        #region Visual

        [Header("Visual")]
        [Tooltip("Префаб визуального эффекта контура")]
        public GameObject contourVfx;

        [Tooltip("Префаб активной формации")]
        public GameObject activeVfx;

        [Tooltip("Цвет формации")]
        public Color formationColor = Color.cyan;

        [Tooltip("Звук активации")]
        public AudioClip activationSound;

        [Tooltip("Звук истощения")]
        public AudioClip depletedSound;

        #endregion

        #region Calculated Properties

        /// <summary>
        /// Стоимость прорисовки контура.
        /// Формула: 80 × 2^(level-1)
        /// </summary>
        public int ContourQi
        {
            get
            {
                if (contourQiOverride > 0) return contourQiOverride;
                return 80 * (int)Mathf.Pow(2, level - 1);
            }
        }

        /// <summary>
        /// Множитель ёмкости по размеру.
        /// </summary>
        public int SizeMultiplier => size switch
        {
            FormationSize.Small => 10,
            FormationSize.Medium => 50,
            FormationSize.Large => 200,
            FormationSize.Great => 1000,
            FormationSize.Heavy => 10000,
            _ => 10
        };

        /// <summary>
        /// Рассчитать ёмкость формации.
        /// Формула: contourQi × sizeMultiplier
        /// </summary>
        public long CalculateCapacity(bool isHeavy = false)
        {
            if (isHeavy && level >= 6)
            {
                return ContourQi * 10000;
            }
            return (long)ContourQi * SizeMultiplier;
        }

        /// <summary>
        /// Интервал утечки в тиках.
        /// Источник: FORMATION_SYSTEM.md "Естественная утечка Ци"
        /// </summary>
        public int DrainIntervalTicks => level switch
        {
            1 or 2 => 60,   // Каждый час
            3 or 4 => 40,   // Каждые 40 минут
            5 or 6 => 20,   // Каждые 20 минут
            7 or 8 => 10,   // Каждые 10 минут
            9 => 5,         // Каждые 5 минут
            _ => 60
        };

        /// <summary>
        /// Количество Ци утекающего за раз.
        /// </summary>
        public int DrainAmount => size switch
        {
            FormationSize.Small => 1,
            FormationSize.Medium => 3,
            FormationSize.Large => 10,
            FormationSize.Great => 30,
            FormationSize.Heavy => 100,
            _ => 1
        };

        /// <summary>
        /// Максимум помощников при наполнении.
        /// </summary>
        public int MaxHelpers => size switch
        {
            FormationSize.Small => 2,
            FormationSize.Medium => 5,
            FormationSize.Large => 10,
            FormationSize.Great => 20,
            FormationSize.Heavy => 50,
            _ => 2
        };

        /// <summary>
        /// Минимальный уровень помощника.
        /// Формула: max(1, formationLevel - 2)
        /// </summary>
        public int MinHelperLevel => Mathf.Max(1, level - 2);

        #endregion

        #region Validation

        /// <summary>
        /// Проверить совместимость с ядром.
        /// </summary>
        public bool IsCompatibleWithCore(FormationCoreData core)
        {
            if (core == null) return false;

            // Проверка типа ядра
            if (compatibleCores.Count > 0 && !compatibleCores.Contains(core.coreType))
                return false;

            // Проверка уровня
            if (level < core.levelMin || level > core.levelMax)
                return false;

            // Проверка типа формации
            if (!core.SupportsType(formationType))
                return false;

            // Проверка размера
            if (!core.SupportsSize(size))
                return false;

            return true;
        }

        /// <summary>
        /// Получить информацию о формации.
        /// </summary>
        public string GetInfo()
        {
            return $"[{displayName}] L{level} {formationType} {size}\n" +
                   $"Contour: {ContourQi:N0} | Capacity: {CalculateCapacity():N0}\n" +
                   $"Radius: {effectRadius}m | Duration: {(isPermanent ? "Permanent" : $"{baseDuration}s")}";
        }

        #endregion
    }
}
