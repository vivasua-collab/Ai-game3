// ============================================================================
// SpeciesData.cs — Данные вида существа
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:11:36 UTC
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Данные вида существа.
    /// Определяет базовые характеристики, способности и анатомию.
    /// </summary>
    [CreateAssetMenu(fileName = "Species", menuName = "Cultivation/Species")]
    public class SpeciesData : ScriptableObject
    {
        // FIX DAT-M04: OnValidate to check weaknesses/resistances overlap (2026-04-11)
        private void OnValidate()
        {
            if (weaknesses != null && resistances != null)
            {
                foreach (var w in weaknesses)
                {
                    if (resistances.Contains(w))
                    {
                        Debug.LogWarning($"[SpeciesData] Element {w} is in both weaknesses and resistances — this is invalid.");
                    }
                }
            }
        }
        [Header("Basic Info")]
        [Tooltip("Уникальный ID вида")]
        public string speciesId;
        
        [Tooltip("Название на русском")]
        public string nameRu;
        
        [Tooltip("Название на английском")]
        public string nameEn;
        
        [TextArea(2, 4)]
        [Tooltip("Описание вида")]
        public string description;
        
        [Header("Classification")]
        [Tooltip("Тип души (первичная классификация)")]
        public SoulType soulType;
        
        [Tooltip("Морфология тела")]
        public Morphology morphology;
        
        [Tooltip("Материал тела")]
        public BodyMaterial bodyMaterial;
        
        [Tooltip("Класс размера")]
        public SizeClass sizeClass;
        
        [Header("Stats Range")]
        [Tooltip("Диапазон силы")]
        public MinMaxRange strength;
        
        [Tooltip("Диапазон ловкости")]
        public MinMaxRange agility;
        
        [Tooltip("Диапазон интеллекта")]
        public MinMaxRange intelligence;
        
        [Tooltip("Диапазон жизнеспособности")]
        public MinMaxRange vitality;
        
        [Header("Cultivation")]
        [Tooltip("Может культивировать")]
        public bool canCultivate = false;
        
        [Tooltip("Врождённая генерация Ци")]
        public bool innateQiGeneration = false;
        
        [Tooltip("Максимальный уровень культивации")]
        [Range(0, 10)]
        public int maxCultivationLevel = 0;
        
        [Tooltip("Базовая ёмкость ядра")]
        public LongMinMaxRange coreCapacityBase; // FIX DAT-H02: MinMaxRange→LongMinMaxRange for Qi Model B (2026-04-11)
        
        [Tooltip("Базовая проводимость")]
        [Range(0.1f, 5f)]
        public float conductivityBase = 1.0f;
        
        [Header("Abilities")]
        [Tooltip("Может говорить")]
        public bool speechCapable = false;
        
        [Tooltip("Использует инструменты")]
        public bool toolUse = false;
        
        [Tooltip("Скорость обучения")]
        [Range(0.1f, 2f)]
        public float learningRate = 1.0f;
        
        [Tooltip("Врождённые техники")]
        public List<string> innateTechniques = new List<string>();
        
        [Header("Resistances")]
        [Tooltip("Слабости")]
        public List<Element> weaknesses = new List<Element>();
        
        [Tooltip("Сопротивления")]
        public List<Element> resistances = new List<Element>();
        
        [Header("Lifespan")]
        [Tooltip("Продолжительность жизни (годы)")]
        public int lifespan = 100;
        
        [Tooltip("Возраст зрелости")]
        public int maturityAge = 18;
        
        [Header("Body Parts")]
        [Tooltip("Конфигурация частей тела")]
        public List<BodyPartConfig> bodyParts = new List<BodyPartConfig>();
    }
    
    /// <summary>
    /// Класс размера существа.
    /// Источник: docs/BODY_SYSTEM.md §"Классы размера"
    /// </summary>
    public enum SizeClass
    {
        Tiny,       // < 30 см (Мышь, птенец) — 0.1× сила, 0.3× HP
        Small,      // 30-60 см (Кошка, заяц) — 0.3× сила, 0.5× HP
        Medium,     // 60-180 см (Человек, волк) — 1.0× сила, 1.0× HP
        Large,      // 1.8-3 м (Тигр, медведь) — 2.0× сила, 1.5× HP
        Huge,       // 3-10 м (Великан, дракон) — 5.0× сила, 2.0× HP
        Gargantuan, // 10-30 м (Титан) — 15.0× сила, 3.0× HP
        Colossal    // 30+ м (Космический дракон) — 50.0× сила, 5.0× HP
    }
    
    /// <summary>
    /// Диапазон значений
    /// </summary>
    [System.Serializable]
    public class MinMaxRange
    {
        public float min;
        public float max;
        
        public float GetRandom()
        {
            return UnityEngine.Random.Range(min, max);
        }
    }

    /// <summary>
    /// Диапазон значений (long) — для Qi Model B, где ёмкость > 2.1B
    /// FIX DAT-H02: Added for coreCapacityBase (2026-04-11)
    /// </summary>
    [System.Serializable]
    public class LongMinMaxRange
    {
        public long min;
        public long max;

        public long GetRandom()
        {
            return (long)UnityEngine.Random.Range((float)min, (float)max);
        }
    }
    
    /// <summary>
    /// Конфигурация части тела
    /// </summary>
    [System.Serializable]
    public class BodyPartConfig
    {
        public BodyPartType partType;
        public string customName;
        public int baseHp;
        public float hitChanceModifier;
        public bool isVital = false;
    }
}
