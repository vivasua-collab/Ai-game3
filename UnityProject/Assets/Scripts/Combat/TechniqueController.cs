// ============================================================================
// TechniqueController.cs — Контроллер техник
// Cultivation World Simulator
// Версия: 1.1 — Интеграция с системой накачки (TechniqueChargeSystem)
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-05-04 04:30:00 UTC — Добавлен UseTechniqueFromCharge
// Редактировано: 2026-05-05 09:55:00 UTC — С-11: IncreaseMastery baseGain 0.01→1.0, 0.02→2.0
// Редактировано: 2026-05-07 10:30:00 UTC — ФАЗА 2: qiCostReduction из EquipmentController
// Редактировано: 2026-05-05 08:25:00 UTC — HasDefensiveTechnique() + HasActiveShield()
// Редактировано: 2026-05-05 10:05:00 UTC

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Qi;
using CultivationGame.Combat;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Изученная техника.
    /// </summary>
    [Serializable]
    public class LearnedTechnique
    {
        public Data.ScriptableObjects.TechniqueData Data;
        public float Mastery;           // 0-100%
        public int QuickSlot;           // -1 = не назначен
        public float CooldownRemaining; // В секундах
        
        public LearnedTechnique(Data.ScriptableObjects.TechniqueData data, float mastery = 0f)
        {
            Data = data;
            Mastery = mastery;
            QuickSlot = -1;
            CooldownRemaining = 0f;
        }
    }
    
    /// <summary>
    /// Контроллер техник — управление изученными техниками и их использованием.
    /// 
    /// Источники:
    /// - TECHNIQUE_SYSTEM.md — Система техник
    /// - ALGORITHMS.md §3-4 — Ёмкость и расчёт техник
    /// 
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  СИСТЕМА МАСТЕРСТВА                                                        ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║                                                                            ║
    /// ║  Прирост:                                                                  ║
    /// ║  masteryGained = max(0.1, baseGain × (1 - currentMastery / 100))           ║
    /// ║                                                                            ║
    /// ║  Влияние:                                                                  ║
    /// ║  | Мастерство | Бонус ёмкости |                                            ║
    /// ║  |------------|---------------|                                            ║
    /// ║  | 0%         | +0%           |                                            ║
    /// ║  | 50%        | +25%          |                                            ║
    /// ║  | 100%       | +50%          |                                            ║
    /// ║                                                                            ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public class TechniqueController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private QiController qiController;

        // ФАЗА 2: EquipmentController для qiCostReduction (опционально)
        private CultivationGame.Inventory.EquipmentController equipmentController;
        
        [Header("Config")]
        [SerializeField] private int maxQuickSlots = 10;
        [SerializeField] private int maxUltimates = 1;
        
        // === Runtime ===
        
        private List<LearnedTechnique> learnedTechniques = new List<LearnedTechnique>();
        private LearnedTechnique[] quickSlots;
        private int ultimateCount = 0;
        
        // === Система накачки ===
        private TechniqueChargeSystem chargeSystem;
        
        // === Events ===
        
        public event Action<LearnedTechnique> OnTechniqueLearned;
        public event Action<LearnedTechnique, float> OnTechniqueUsed;
        public event Action<LearnedTechnique> OnTechniqueMastered;
        public event Action<int> OnCooldownUpdated;
        
        // === Properties ===
        
        public List<LearnedTechnique> Techniques => learnedTechniques;
        public int TechniqueCount => learnedTechniques.Count;

        /// <summary>
        /// Есть ли у практика защитная техника (Shield/Block/Dodge)?
        /// Используется ICombatant.HasShieldTechnique для определения QiDefense типа.
        /// </summary>
        public bool HasDefensiveTechnique()
        {
            foreach (var tech in learnedTechniques)
            {
                if (tech.Data != null && tech.Data.techniqueType == TechniqueType.Defense)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Есть ли активная техника-щит (не на кулдауне)?
        /// Используется для QiDefenseType.Shield vs RawQi.
        /// FIX В-09: Проверяет defenseSubtype == Shield вместо любого Defense.
        /// Раньше любая Defense-техника активировала Shield-режим Qi Buffer — неверно.
        /// </summary>
        public bool HasActiveShield()
        {
            foreach (var tech in learnedTechniques)
            {
                if (tech.Data != null &&
                    tech.Data.techniqueType == TechniqueType.Defense &&
                    tech.Data.defenseSubtype == DefenseSubtype.Shield &&
                    tech.CooldownRemaining <= 0f)
                    return true;
            }
            return false;
        }
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            quickSlots = new LearnedTechnique[maxQuickSlots];
            
            if (qiController == null)
                qiController = GetComponent<QiController>();

            // Получаем/создаём систему накачки
            chargeSystem = GetComponent<TechniqueChargeSystem>();

            // ФАЗА 2: EquipmentController — может отсутствовать (опционально)
            equipmentController = GetComponent<CultivationGame.Inventory.EquipmentController>();
        }
        
        private void Update()
        {
            ProcessCooldowns();
        }
        
        // === Learning ===
        
        /// <summary>
        /// Изучить технику.
        /// 
        /// Источник: TECHNIQUE_SYSTEM.md §"Ограничения по уровню"
        /// 
        /// Ограничения:
        /// - Нельзя изучить одну технику дважды
        /// - Максимум 1 Ultimate-техника
        /// </summary>
        public bool LearnTechnique(Data.ScriptableObjects.TechniqueData technique, float initialMastery = 0f)
        {
            if (technique == null) return false;
            
            // Проверяем, не изучена ли уже
            if (HasTechnique(technique.techniqueId))
                return false;
            
            // Проверяем ограничения Ultimate
            if (technique.isUltimate && ultimateCount >= maxUltimates)
                return false;
            
            var learned = new LearnedTechnique(technique, initialMastery);
            learnedTechniques.Add(learned);
            
            if (technique.isUltimate)
                ultimateCount++;
            
            OnTechniqueLearned?.Invoke(learned);
            return true;
        }
        
        /// <summary>
        /// Проверить, изучена ли техника.
        /// </summary>
        public bool HasTechnique(string techniqueId)
        {
            return learnedTechniques.Exists(t => t.Data != null && t.Data.techniqueId == techniqueId);
        }
        
        /// <summary>
        /// Получить изученную технику.
        /// </summary>
        public LearnedTechnique GetTechnique(string techniqueId)
        {
            return learnedTechniques.Find(t => t.Data != null && t.Data.techniqueId == techniqueId);
        }
        
        // === Quick Slots ===
        
        /// <summary>
        /// Назначить технику в слот быстрого доступа.
        /// </summary>
        public bool AssignToQuickSlot(LearnedTechnique technique, int slot)
        {
            if (technique == null || slot < 0 || slot >= maxQuickSlots)
                return false;
            
            // Убираем из старого слота
            if (technique.QuickSlot >= 0 && technique.QuickSlot < maxQuickSlots)
            {
                quickSlots[technique.QuickSlot] = null;
            }
            
            // Убираем технику, которая была в новом слоте
            if (quickSlots[slot] != null)
            {
                quickSlots[slot].QuickSlot = -1;
            }
            
            quickSlots[slot] = technique;
            technique.QuickSlot = slot;
            
            return true;
        }
        
        /// <summary>
        /// Получить технику из слота.
        /// </summary>
        public LearnedTechnique GetQuickSlotTechnique(int slot)
        {
            if (slot < 0 || slot >= maxQuickSlots)
                return null;
            
            return quickSlots[slot];
        }
        
        // === Usage ===
        
        /// <summary>
        /// Проверить, можно ли использовать технику.
        /// 
        /// Условия:
        /// 1. Кулдаун = 0
        /// 2. Уровень культивации ≥ minCultivationLevel
        /// 3. Текущее Ци ≥ qiCost
        /// </summary>
        public bool CanUseTechnique(LearnedTechnique technique)
        {
            if (technique == null || technique.Data == null)
                return false;
            
            // Проверка кулдауна
            if (technique.CooldownRemaining > 0)
                return false;
            
            // Проверка уровня культивации
            if (qiController.CultivationLevel < technique.Data.minCultivationLevel)
                return false;

            // ФАЗА 3: MeleeWeapon техника требует оружие в основной руке
            if (technique.Data.combatSubtype == CombatSubtype.MeleeWeapon)
            {
                if (equipmentController == null || equipmentController.GetMainWeapon() == null)
                {
                    return false; // Нет оружия — нельзя использовать MeleeWeapon технику
                }
            }
            
            // Рассчитываем стоимость Ци
            long qiCost = CalculateQiCost(technique);
            
            // Проверка Ци
            if (qiController.CurrentQi < qiCost)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Использовать технику.
        /// 
        /// Источник: TECHNIQUE_SYSTEM.md §"Принципы"
        /// 
        /// Пайплайн:
        /// 1. Проверка условий
        /// 2. Расчёт параметров (qiCost, capacity, damage)
        /// 3. Трата Ци
        /// 4. Установка кулдауна
        /// 5. Повышение мастерства
        /// </summary>
        public TechniqueUseResult UseTechnique(LearnedTechnique technique)
        {
            TechniqueUseResult result = new TechniqueUseResult();
            
            if (!CanUseTechnique(technique))
            {
                result.Success = false;
                result.FailReason = "Cannot use technique";
                return result;
            }
            
            // Рассчитываем параметры
            long qiCost = CalculateQiCost(technique);
            int capacity = CalculateCapacity(technique);
            int damage = CalculateDamage(technique);
            
            // Тратим Ци
            if (!qiController.SpendQi(qiCost))
            {
                result.Success = false;
                result.FailReason = "Not enough Qi";
                return result;
            }
            
            // Устанавливаем кулдаун
            // FIX CMB-H02: technique.Data.cooldown уже в СЕКУНДАХ (из TechniqueData)
            // CooldownRemaining тоже в секундах — множитель ×60 убран
            technique.CooldownRemaining = technique.Data.cooldown;
            
            // Повышаем мастерство (+1.0 за использование)
            // С-11: было 0.01 — слишком мало, TECHNIQUE_SYSTEM.md: прирост = max(0.1, baseGain × (1 - mastery/100))
            IncreaseMastery(technique, 1.0f);
            
            // Заполняем результат
            result.Success = true;
            result.QiCost = qiCost;
            result.Capacity = capacity;
            result.Damage = damage;
            result.CastTime = CalculateCastTime(technique);
            result.Element = technique.Data.element;
            result.Type = technique.Data.techniqueType;
            result.IsUltimate = technique.Data.isUltimate;
            
            OnTechniqueUsed?.Invoke(technique, result.CastTime);
            
            return result;
        }
        
        /// <summary>
        /// Использовать технику из слота быстрого доступа.
        /// С системой накачки: начинается накачка, а не мгновенное использование.
        /// </summary>
        public TechniqueUseResult UseQuickSlot(int slot)
        {
            var technique = GetQuickSlotTechnique(slot);
            if (technique == null)
                return new TechniqueUseResult { Success = false, FailReason = "Empty slot" };
            
            // Если есть система накачки — начинаем накачку
            if (chargeSystem != null)
            {
                if (chargeSystem.IsCharging)
                {
                    // Уже накачиваем
                    if (chargeSystem.ActiveCharge.Technique == technique)
                    {
                        // Та же техника → отмена накачки
                        chargeSystem.CancelCharge();
                        return new TechniqueUseResult { Success = false, FailReason = "Charge cancelled" };
                    }
                    // Другая техника → игнорируем (правило: только одна)
                    return new TechniqueUseResult { Success = false, FailReason = "Already charging" };
                }
                
                // Начинаем накачку
                if (chargeSystem.BeginCharge(technique))
                {
                    return new TechniqueUseResult { Success = false, FailReason = "Charging started" };
                }
                return new TechniqueUseResult { Success = false, FailReason = "Cannot start charge" };
            }
            
            // Fallback: без системы накачки — мгновенное использование
            return UseTechnique(technique);
        }
        
        /// <summary>
        /// Использовать технику ПОСЛЕ завершения накачки.
        /// Вызывается из TechniqueChargeSystem.FireChargedTechnique().
        /// НЕ тратит Ци (уже потрачено во время накачки), только:
        /// - Устанавливает кулдаун
        /// - Повышает мастерство
        /// - Формирует TechniqueUseResult для CombatManager
        /// </summary>
        public TechniqueUseResult UseTechniqueFromCharge(LearnedTechnique technique)
        {
            TechniqueUseResult result = new TechniqueUseResult();
            
            if (technique == null || technique.Data == null)
            {
                result.Success = false;
                result.FailReason = "Invalid technique";
                return result;
            }
            
            // Ци уже потрачено во время накачки — НЕ списываем повторно!
            
            // Устанавливаем кулдаун
            technique.CooldownRemaining = technique.Data.cooldown;
            
            // Повышаем мастерство (+2.0 за использование через накачку — больше чем мгновенное)
            // С-11: было 0.02 — слишком мало
            IncreaseMastery(technique, 2.0f);
            
            // Рассчитываем параметры
            int capacity = CalculateCapacity(technique);
            int damage = CalculateDamage(technique);
            long qiCost = CalculateQiCost(technique);
            
            // Заполняем результат
            result.Success = true;
            result.QiCost = qiCost;
            result.Capacity = capacity;
            result.Damage = damage;
            result.CastTime = CalculateCastTime(technique);
            result.Element = technique.Data.element;
            result.Type = technique.Data.techniqueType;
            result.IsUltimate = technique.Data.isUltimate;
            
            OnTechniqueUsed?.Invoke(technique, result.CastTime);
            
            return result;
        }
        
        /// <summary>
        /// Система накачки (для внешнего доступа).
        /// </summary>
        public TechniqueChargeSystem ChargeSystem => chargeSystem;
        
        /// <summary>
        /// Идёт ли накачка техники?
        /// </summary>
        public bool IsCharging => chargeSystem != null && chargeSystem.IsCharging;
        
        // === Calculations ===
        
        private long CalculateQiCost(LearnedTechnique technique)
        {
            // Базовая стоимость из TechniqueCapacity
            long baseCost = TechniqueCapacity.CalculateQiCost(
                technique.Data.baseQiCost,
                technique.Data.techniqueLevel
            );

            // ФАЗА 2: Снижение стоимости Ци от оружия
            float reduction = equipmentController?.GetQiCostReduction() ?? 0f;
            if (reduction > 0f)
            {
                baseCost = (long)(baseCost * (1f - reduction));
            }

            return baseCost;
        }
        
        private int CalculateCapacity(LearnedTechnique technique)
        {
            return TechniqueCapacity.CalculateCapacity(
                technique.Data.techniqueType,
                technique.Data.combatSubtype,
                technique.Data.techniqueLevel,
                technique.Mastery
            );
        }
        
        private int CalculateDamage(LearnedTechnique technique)
        {
            int capacity = CalculateCapacity(technique);
            return TechniqueCapacity.CalculateDamage(
                capacity,
                technique.Data.grade,
                technique.Data.isUltimate
            );
        }
        
        private float CalculateCastTime(LearnedTechnique technique)
        {
            long qiCost = CalculateQiCost(technique);
            return TechniqueCapacity.CalculateCastTime(
                qiCost,
                qiController.Conductivity,
                qiController.CultivationLevel,
                technique.Mastery
            );
        }
        
        // === Mastery ===
        
        /// <summary>
        /// Повысить мастерство техники.
        /// 
        /// Источник: TECHNIQUE_SYSTEM.md §"Система мастерства"
        /// 
        /// Формула:
        /// masteryGained = max(0.1, baseGain × (1 - currentMastery / 100))
        /// 
        /// FIX CMB-H01: Реализована формула с убывающим приростом.
        /// </summary>
        private void IncreaseMastery(LearnedTechnique technique, float baseGain)
        {
            if (technique.Mastery >= 100f) return;
            
            float oldMastery = technique.Mastery;
            
            // FIX CMB-H01: Формула из документации — прирост замедляется с ростом мастерства
            float masteryGained = Mathf.Max(0.1f, baseGain * (1f - technique.Mastery / 100f));
            technique.Mastery = Mathf.Min(100f, technique.Mastery + masteryGained);
            
            if (oldMastery < 100f && technique.Mastery >= 100f)
            {
                OnTechniqueMastered?.Invoke(technique);
            }
        }
        
        // === Cooldowns ===
        
        private void ProcessCooldowns()
        {
            for (int i = 0; i < learnedTechniques.Count; i++)
            {
                if (learnedTechniques[i].CooldownRemaining > 0)
                {
                    learnedTechniques[i].CooldownRemaining -= Time.deltaTime;
                    
                    if (learnedTechniques[i].CooldownRemaining <= 0)
                    {
                        learnedTechniques[i].CooldownRemaining = 0;
                        OnCooldownUpdated?.Invoke(i);
                    }
                }
            }
        }
        
        /// <summary>
        /// Сбросить все кулдауны.
        /// </summary>
        public void ResetAllCooldowns()
        {
            foreach (var technique in learnedTechniques)
            {
                technique.CooldownRemaining = 0;
            }
        }
        
        // === Data ===
        
        /// <summary>
        /// Получить данные для сохранения.
        /// </summary>
        public List<TechniqueSaveData> GetSaveData()
        {
            List<TechniqueSaveData> data = new List<TechniqueSaveData>();
            
            foreach (var tech in learnedTechniques)
            {
                if (tech.Data != null)
                {
                    data.Add(new TechniqueSaveData
                    {
                        TechniqueId = tech.Data.techniqueId,
                        Mastery = tech.Mastery,
                        QuickSlot = tech.QuickSlot
                    });
                }
            }
            
            return data;
        }
        
        /// <summary>
        /// Загрузить данные.
        /// </summary>
        public void LoadSaveData(List<TechniqueSaveData> data)
        {
            // Реализуется при интеграции с SaveSystem
        }
    }
    
    /// <summary>
    /// Результат использования техники.
    /// </summary>
    public struct TechniqueUseResult
    {
        public bool Success;
        public string FailReason;
        public long QiCost;
        public int Capacity;
        public int Damage;
        public float CastTime;
        public Element Element;
        public TechniqueType Type;
        public bool IsUltimate;
    }
    
    /// <summary>
    /// Данные техники для сохранения.
    /// </summary>
    [Serializable]
    public struct TechniqueSaveData
    {
        public string TechniqueId;
        public float Mastery;
        public int QuickSlot;
    }
}
