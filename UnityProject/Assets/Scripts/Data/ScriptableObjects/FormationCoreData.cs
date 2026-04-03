// ============================================================================
// FormationCoreData.cs — Данные ядра формации
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 09:20:39 UTC
// Редактировано: 2026-04-03 09:20:39 UTC
// ============================================================================
//
// Источник: docs/DATA_MODELS.md §10 "FormationCore"
// Источник: docs/FORMATIONS_SYSTEM.md
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Тип ядра формации
    /// </summary>
    public enum FormationCoreType
    {
        Disk,           // Диск (портативный)
        Altar,          // Алтарь (стационарный)
        Array,          // Массив (напольный)
        Totem,          // Тотем
        Seal            // Печать
    }

    /// <summary>
    /// Материал ядра формации
    /// </summary>
    public enum FormationCoreVariant
    {
        Stone,          // Камень
        Jade,           // Нефрит
        Iron,           // Железо
        SpiritIron,     // Духовное железо
        Crystal,        // Кристалл
        StarMetal,      // Звёздный металл
        VoidMatter      // Пустотная материя
    }

    /// <summary>
    /// Тип формации
    /// </summary>
    public enum FormationType
    {
        Barrier,        // Барьер (защита)
        Trap,           // Ловушка (атака)
        Amplification,  // Усиление (баффы)
        Suppression,    // Подавление (дебаффы)
        Gathering,      // Сбор (ресурсы)
        Detection,      // Обнаружение (сенсор)
        Teleportation,  // Телепортация
        Summoning       // Призыв
    }

    /// <summary>
    /// Размер формации
    /// </summary>
    public enum FormationSize
    {
        Small,          // 3x3 метра
        Medium,         // 10x10 метров
        Large,          // 30x30 метров
        Great,          // 100x100 метров
        Heavy           // 300x300 метров
    }

    /// <summary>
    /// Данные ядра формации.
    /// Создаётся как ScriptableObject для каждого типа ядра.
    /// </summary>
    [CreateAssetMenu(fileName = "FormationCore", menuName = "Cultivation/Formation Core")]
    public class FormationCoreData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Уникальный ID ядра")]
        public string coreId;
        
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
        [Tooltip("Тип ядра")]
        public FormationCoreType coreType = FormationCoreType.Disk;
        
        [Tooltip("Материал")]
        public FormationCoreVariant variant = FormationCoreVariant.Stone;
        
        [Tooltip("Редкость")]
        public ItemRarity rarity = ItemRarity.Common;
        
        [Header("Capacity")]
        [Tooltip("Минимальный уровень формации")]
        [Range(1, 9)]
        public int levelMin = 1;
        
        [Tooltip("Максимальный уровень формации")]
        [Range(1, 9)]
        public int levelMax = 9;
        
        [Tooltip("Максимальное количество слотов для камней Ци")]
        [Range(1, 12)]
        public int maxSlots = 3;
        
        [Tooltip("Базовая проводимость (ед/сек)")]
        [Range(1, 100)]
        public int baseConductivity = 10;
        
        [Tooltip("Максимальная ёмкость")]
        public long maxCapacity = 1000;
        
        [Header("Formation Types")]
        [Tooltip("Поддерживаемые типы формаций")]
        public List<FormationType> supportedTypes = new List<FormationType>();
        
        [Tooltip("Рекомендуемый тип")]
        public FormationType recommendedType = FormationType.Barrier;
        
        [Header("Size")]
        [Tooltip("Поддерживаемые размеры")]
        public List<FormationSize> supportedSizes = new List<FormationSize>();
        
        [Tooltip("Максимальный размер")]
        public FormationSize maxSize = FormationSize.Medium;
        
        [Header("Qi Stones")]
        [Tooltip("Требуемые камни Ци")]
        public List<QiStoneSlot> qiStoneSlots = new List<QiStoneSlot>();
        
        [Header("Effects")]
        [Tooltip("Элемент баффа (если применимо)")]
        public Element element = Element.Neutral;
        
        [Tooltip("Множитель эффекта от уровня")]
        public AnimationCurve levelEffectCurve = AnimationCurve.Linear(1, 1, 9, 3);
        
        [Header("Requirements")]
        [Tooltip("Минимальный уровень культивации")]
        [Range(0, 10)]
        public int minCultivationLevel = 0;
        
        [Tooltip("Минимальная проводимость для использования")]
        [Range(0.1f, 10f)]
        public float minConductivity = 0.5f;
        
        [Tooltip("Требуемые знания формаций")]
        [Range(0, 100)]
        public int minFormationKnowledge = 0;
        
        [Header("Durability")]
        [Tooltip("Максимальная прочность")]
        public int maxDurability = 100;
        
        [Tooltip("Расход прочности при активации")]
        public int durabilityPerActivation = 1;
        
        [Tooltip("Расход при поддержании (в час)")]
        public int durabilityPerHour = 0;
        
        [Header("Visual")]
        [Tooltip("Модель для 3D")]
        public GameObject modelPrefab;
        
        [Tooltip("VFX при активации")]
        public GameObject activationVfx;
        
        [Tooltip("VFX при действии")]
        public GameObject activeVfx;
        
        [Tooltip("Цвет свечения")]
        public Color glowColor = Color.cyan;
        
        // === Runtime Methods ===
        
        /// <summary>
        /// Проверить поддержку типа формации
        /// </summary>
        public bool SupportsType(FormationType type)
        {
            return supportedTypes.Count == 0 || supportedTypes.Contains(type);
        }
        
        /// <summary>
        /// Проверить поддержку размера
        /// </summary>
        public bool SupportsSize(FormationSize size)
        {
            int sizeIndex = (int)size;
            int maxIndex = (int)maxSize;
            return sizeIndex <= maxIndex;
        }
        
        /// <summary>
        /// Получить множитель эффекта для уровня
        /// </summary>
        public float GetEffectMultiplier(int level)
        {
            return levelEffectCurve.Evaluate(level);
        }
        
        /// <summary>
        /// Рассчитать стоимость создания формации
        /// </summary>
        public long CalculateQiCost(FormationType type, FormationSize size, int level)
        {
            // Базовая стоимость зависит от типа и размера
            float baseCost = type switch
            {
                FormationType.Barrier => 100,
                FormationType.Trap => 80,
                FormationType.Amplification => 120,
                FormationType.Suppression => 150,
                FormationType.Gathering => 200,
                FormationType.Detection => 60,
                FormationType.Teleportation => 500,
                FormationType.Summoning => 300,
                _ => 100
            };
            
            // Множитель размера
            float sizeMult = size switch
            {
                FormationSize.Small => 1,
                FormationSize.Medium => 3,
                FormationSize.Large => 10,
                FormationSize.Great => 30,
                FormationSize.Heavy => 100,
                _ => 1
            };
            
            // Множитель уровня
            float levelMult = Mathf.Pow(2, level - 1);
            
            return (long)(baseCost * sizeMult * levelMult);
        }
    }
    
    /// <summary>
    /// Слот для камня Ци
    /// </summary>
    [System.Serializable]
    public class QiStoneSlot
    {
        [Tooltip("Номер слота")]
        public int slotNumber;
        
        [Tooltip("Требуемый тип камня")]
        public QiStoneType requiredType = QiStoneType.Any;
        
        [Tooltip("Минимальный тир камня")]
        [Range(1, 5)]
        public int minTier = 1;
        
        [Tooltip("Опциональный слот")]
        public bool optional = false;
        
        [Tooltip("Бонус от камня в этом слоте")]
        public float slotBonus = 1.0f;
    }
    
    /// <summary>
    /// Тип камня Ци
    /// </summary>
    public enum QiStoneType
    {
        Any,            // Любой
        Neutral,        // Нейтральный
        Fire,           // Огненный
        Water,          // Водный
        Earth,          // Земной
        Air,            // Воздушный
        Lightning,      // Грозовой
        Void            // Пустотный
    }
}
