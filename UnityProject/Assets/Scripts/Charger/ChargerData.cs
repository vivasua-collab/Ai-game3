// ============================================================================
// ChargerData.cs — ScriptableObject данных зарядника Ци
// Cultivation World Simulator
// Создано: 2026-04-03 08:15:00 UTC
// Редактировано: 2026-04-11 06:38:02 UTC — Qi int→long миграция (capacity, currentQi в ChargerBufferData)
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Charger
{
    /// <summary>
    /// Форм-фактор зарядника.
    /// Источник: CHARGER_SYSTEM.md §1.1 "По форм-фактору"
    /// </summary>
    public enum ChargerFormFactor
    {
        Belt,       // Пояс-накопитель (3-8 слотов, буфер 500)
        Bracelet,   // Браслет-накопитель (2-4 слота, буфер 200)
        Necklace,   // Ожерелье-накопитель (1-3 слота, буфер 1000)
        Ring,       // Кольцо-накопитель (1 слот, буфер 50)
        Backpack    // Ранец-накопитель (6-15 слотов, буфер 2000)
    }
    
    /// <summary>
    /// Назначение зарядника.
    /// Источник: CHARGER_SYSTEM.md §1.2 "По назначению"
    /// </summary>
    public enum ChargerPurpose
    {
        Accumulation,   // Медитационный (×0.8 скорость, ×1.5 буфер)
        Combat,         // Боевой (×1.5 скорость, ×0.7 буфер)
        Hybrid          // Универсальный (×1.0 скорость, ×1.0 буфер)
    }
    
    /// <summary>
    /// Материал зарядника.
    /// Источник: CHARGER_SYSTEM.md §6.1 "Материалы зарядников"
    /// </summary>
    public enum ChargerMaterial
    {
        Iron,           // Тир 1: Проводимость 5, Прочность 100
        Copper,         // Тир 1: Проводимость 8, Прочность 80
        Silver,         // Тир 2: Проводимость 15, Прочность 90
        SpiritIron,     // Тир 3: Проводимость 25, Прочность 200
        Jade,           // Тир 3: Проводимость 20, Прочность 150
        SpiritJade,     // Тир 3: Проводимость 35, Прочность 300
        DragonBone,     // Тир 4: Проводимость 50, Прочность 1000
        VoidMatter      // Тир 5: Проводимость 100, Прочность 2000
    }
    
    /// <summary>
    /// Режим работы зарядника.
    /// Источник: CHARGER_SYSTEM.md §3.2 "Режимы работы"
    /// Упрощено до двух режимов: вкл/выкл
    /// </summary>
    public enum ChargerMode
    {
        Off,    // Выключен (скорость 0, потери 0%)
        On      // Включен (скорость ×1.0, потери 10%)
    }
    
    /// <summary>
    /// Данные слота для камня Ци.
    /// </summary>
    [Serializable]
    public struct ChargerSlotData
    {
        public int index;
        public string minQiStoneQuality;    // Минимальное качество камня
        public string maxQiStoneSize;       // Максимальный размер камня
        public bool isActive;               // Активен ли слот
        public bool isSealed;               // Запечатан ли слот
        public float absorptionBonus;       // Бонус поглощения
        public float qiRetention;           // Сохранение Ци (%)
    }
    
    /// <summary>
    /// Данные буфера Ци зарядника.
    /// </summary>
    [Serializable]
    public struct ChargerBufferData
    {
        [Tooltip("Ёмкость буфера (50-2000)")]
        public long capacity; // FIX: int→long Qi migration (2026-04-12)
        
        [Tooltip("Текущее Ци в буфере")]
        public long currentQi; // FIX: int→long Qi migration (2026-04-12)
        
        [Tooltip("Проводимость зарядника (5-100 ед/сек)")]
        public float conductivity;
        
        [Tooltip("Скорость входящего потока")]
        public float inputRate;
        
        [Tooltip("Скорость исходящего потока")]
        public float outputRate;
    }
    
    /// <summary>
    /// ScriptableObject для данных зарядника Ци.
    /// Источник: CHARGER_SYSTEM.md
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharger", menuName = "Cultivation/Equipment/Charger")]
    public class ChargerData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string chargerId;
        [SerializeField] private string chargerName;
        [SerializeField][TextArea] private string description;
        
        [Header("Type")]
        [SerializeField] private ChargerFormFactor formFactor = ChargerFormFactor.Belt;
        [SerializeField] private ChargerPurpose purpose = ChargerPurpose.Hybrid;
        [SerializeField] private EquipmentGrade grade = EquipmentGrade.Common;
        [SerializeField] private ChargerMaterial material = ChargerMaterial.Iron;
        
        [Header("Slots")]
        [SerializeField] private List<ChargerSlotData> slots = new List<ChargerSlotData>();
        [SerializeField] private int baseSlotCount = 3;
        
        [Header("Buffer")]
        [SerializeField] private ChargerBufferData buffer;
        
        [Header("Heat")]
        [SerializeField] private float currentHeat = 0f;        // 0-100%
        [SerializeField] private float heatDissipationRate = 1f; // %/сек
        [SerializeField] private float overheatThreshold = 100f;
        [SerializeField] private float overheatCooldown = 30f;   // секунд
        
        [Header("Bonuses")]
        [SerializeField] private int strengthBonus;
        [SerializeField] private int agilityBonus;
        [SerializeField] private int intelligenceBonus;
        [SerializeField] private int vitalityBonus;
        
        [Header("Requirements")]
        [SerializeField] private int minCultivationLevel = 1;
        [SerializeField] private int minStrength;
        [SerializeField] private int minIntelligence;
        
        [Header("Value")]
        [SerializeField] private int baseValue = 100;
        [SerializeField] private int currentDurability = 100;
        [SerializeField] private int maxDurability = 100;
        
        // === Properties ===
        
        public string ChargerId => chargerId;
        public string ChargerName => chargerName;
        public string Description => description;
        public ChargerFormFactor FormFactor => formFactor;
        public ChargerPurpose Purpose => purpose;
        public EquipmentGrade Grade => grade;
        public ChargerMaterial Material => material;
        public List<ChargerSlotData> Slots => slots;
        public ChargerBufferData Buffer => buffer;
        public float CurrentHeat => currentHeat;
        public float HeatDissipationRate => heatDissipationRate;
        public float OverheatThreshold => overheatThreshold;
        public float OverheatCooldown => overheatCooldown;
        public int BaseValue => baseValue;
        public int CurrentDurability => currentDurability;
        public int MaxDurability => maxDurability;
        
        // === Form Factor Configs ===
        
        /// <summary>
        /// Получить базовое количество слотов по форм-фактору.
        /// </summary>
        public static (int minSlots, int maxSlots, int baseBuffer) GetFormFactorConfig(ChargerFormFactor ff)
        {
            return ff switch
            {
                ChargerFormFactor.Belt => (3, 8, 500),
                ChargerFormFactor.Bracelet => (2, 4, 200),
                ChargerFormFactor.Necklace => (1, 3, 1000),
                ChargerFormFactor.Ring => (1, 1, 50),
                ChargerFormFactor.Backpack => (6, 15, 2000),
                _ => (1, 3, 100)
            };
        }
        
        /// <summary>
        /// Получить проводимость материала.
        /// </summary>
        public static float GetMaterialConductivity(ChargerMaterial mat)
        {
            return mat switch
            {
                ChargerMaterial.Iron => 5f,
                ChargerMaterial.Copper => 8f,
                ChargerMaterial.Silver => 15f,
                ChargerMaterial.SpiritIron => 25f,
                ChargerMaterial.Jade => 20f,
                ChargerMaterial.SpiritJade => 35f,
                ChargerMaterial.DragonBone => 50f,
                ChargerMaterial.VoidMatter => 100f,
                _ => 5f
            };
        }
        
        /// <summary>
        /// Получить прочность материала.
        /// </summary>
        public static int GetMaterialDurability(ChargerMaterial mat)
        {
            return mat switch
            {
                ChargerMaterial.Iron => 100,
                ChargerMaterial.Copper => 80,
                ChargerMaterial.Silver => 90,
                ChargerMaterial.SpiritIron => 200,
                ChargerMaterial.Jade => 150,
                ChargerMaterial.SpiritJade => 300,
                ChargerMaterial.DragonBone => 1000,
                ChargerMaterial.VoidMatter => 2000,
                _ => 100
            };
        }
        
        /// <summary>
        /// Получить сохранение Ци материала (%).
        /// </summary>
        public static float GetMaterialQiRetention(ChargerMaterial mat)
        {
            return mat switch
            {
                ChargerMaterial.Iron => 0.95f,
                ChargerMaterial.Copper => 0.90f,
                ChargerMaterial.Silver => 0.92f,
                ChargerMaterial.SpiritIron => 0.98f,
                ChargerMaterial.Jade => 0.97f,
                ChargerMaterial.SpiritJade => 0.99f,
                ChargerMaterial.DragonBone => 0.995f,
                ChargerMaterial.VoidMatter => 1.0f,
                _ => 0.95f
            };
        }
        
        /// <summary>
        /// Получить множитель назначения для скорости.
        /// </summary>
        public static float GetPurposeSpeedMultiplier(ChargerPurpose purpose)
        {
            return purpose switch
            {
                ChargerPurpose.Accumulation => 0.8f,
                ChargerPurpose.Combat => 1.5f,
                ChargerPurpose.Hybrid => 1.0f,
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// Получить множитель назначения для буфера.
        /// </summary>
        public static float GetPurposeBufferMultiplier(ChargerPurpose purpose)
        {
            return purpose switch
            {
                ChargerPurpose.Accumulation => 1.5f,
                ChargerPurpose.Combat => 0.7f,
                ChargerPurpose.Hybrid => 1.0f,
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// Получить эффективность в бою по назначению (%).
        /// </summary>
        public static float GetPurposeCombatEfficiency(ChargerPurpose purpose)
        {
            return purpose switch
            {
                ChargerPurpose.Accumulation => 0.5f,
                ChargerPurpose.Combat => 1.0f,
                ChargerPurpose.Hybrid => 0.75f,
                _ => 0.75f
            };
        }
        
        // === Runtime Initialization ===
        
        /// <summary>
        /// Инициализировать данные зарядника при создании.
        /// </summary>
        public void Initialize()
        {
            // Генерация ID если пустой
            if (string.IsNullOrEmpty(chargerId))
            {
                chargerId = Guid.NewGuid().ToString().Substring(0, 8);
            }
            
            // Настройка слотов по форм-фактору
            var (minSlots, maxSlots, baseBuffer) = GetFormFactorConfig(formFactor);
            
            if (slots.Count == 0)
            {
                int slotCount = Mathf.Clamp(baseSlotCount, minSlots, maxSlots);
                for (int i = 0; i < slotCount; i++)
                {
                    slots.Add(new ChargerSlotData
                    {
                        index = i,
                        minQiStoneQuality = "raw",
                        maxQiStoneSize = "medium",
                        isActive = true,
                        isSealed = false,
                        absorptionBonus = 0f,
                        qiRetention = GetMaterialQiRetention(material)
                    });
                }
            }
            
            // Настройка буфера — FIX: (long) для Qi (2026-04-11)
            buffer.capacity = (long)Mathf.Round(baseBuffer * GetPurposeBufferMultiplier(purpose));
            buffer.currentQi = 0;
            buffer.conductivity = GetMaterialConductivity(material) * GetPurposeSpeedMultiplier(purpose);
            buffer.inputRate = buffer.conductivity;
            buffer.outputRate = buffer.conductivity;
            
            // Настройка прочности
            maxDurability = GetMaterialDurability(material);
            currentDurability = maxDurability;
        }
        
        // === Editor Validation ===
        
        private void OnValidate()
        {
            // Автоматическое имя если пустое
            if (string.IsNullOrEmpty(chargerName))
            {
                chargerName = $"{grade} {formFactor} Charger";
            }
        }
    }
}
