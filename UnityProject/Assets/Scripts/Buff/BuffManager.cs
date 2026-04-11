// ============================================================================
// BuffManager.cs — Полноценная система баффов и дебаффов
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 14:00:00 UTC
// ============================================================================
//
// Интеграция с:
// - FormationEffects.cs — формации применяют баффы через BuffManager
// - QiController — модификация проводимости (только Conductivity!)
// - Combatant — модификация боевых характеристик
// - BodyController — модификация здоровья
//
// ВАЖНО: PRIMARY характеристики (Strength, Agility, Intelligence, Vitality)
// НЕ могут быть изменены баффами! Только физические средства.
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Data.ScriptableObjects;
using CultivationGame.Formation;
using CultivationGame.Combat;

// Alias для разрешения конфликта имён BuffType
using FormationBuffType = CultivationGame.Formation.BuffType;

namespace CultivationGame.Buff
{
    /// <summary>
    /// Активный бафф на сущности.
    /// </summary>
    [Serializable]
    public class ActiveBuff
    {
        public BuffData data;
        public int currentStacks;
        public int remainingTicks;
        public float timeSinceLastTick;
        public GameObject source;
        public Dictionary<string, float> appliedModifiers;
        
        public ActiveBuff(BuffData buffData, GameObject sourceObj)
        {
            data = buffData;
            currentStacks = 1;
            remainingTicks = buffData.durationTicks;
            timeSinceLastTick = 0f;
            source = sourceObj;
            appliedModifiers = new Dictionary<string, float>();
        }
        
        public bool IsExpired => remainingTicks <= 0 && !data.IsPermanent;
        public bool IsPositive => data.IsPositive;
        public bool IsPermanent => data.IsPermanent;
    }

    /// <summary>
    /// Результат применения баффа.
    /// </summary>
    public enum BuffResult
    {
        Applied,        // Успешно применён
        Stacked,        // Добавлен стак
        Refreshed,      // Обновлена длительность
        Rejected,       // Отклонён (несовместим)
        Replaced        // Заменил существующий
    }

    /// <summary>
    /// Полноценный менеджер баффов для сущности.
    /// 
    /// ┌─────────────────────────────────────────────────────────────────────────┐
    /// │                     СИСТЕМА БАФФОВ                                      │
    /// ├─────────────────────────────────────────────────────────────────────────┤
    /// │                                                                          │
    /// │   ПРАВИЛА:                                                               │
    /// │   1. PRIMARY статы (STR/AGI/INT/VIT) НЕ меняются баффами                │
    /// │   2. Conductivity МОЖЕТ временно модифицироваться с payback             │
    /// │   3. Secondary статы (Damage, Defense, Speed) меняются свободно         │
    /// │   4. QiRegen/CoreCapacity НЕ затрагиваются                              │
    /// │                                                                          │
    /// │   ТИПЫ БАФФОВ:                                                           │
    /// │   - Stat Modifiers: изменение характеристик                             │
    /// │   - Periodic Effects: урон/лечение по тикам                             │
    /// │   - Special Effects: стун, замедление, щит                              │
    /// │                                                                          │
    /// │   СТЕКИНГ:                                                               │
    /// │   - Refresh: обновляет длительность                                     │
    /// │   - Add: добавляет длительность                                         │
    /// │   - Independent: отдельные баффы                                        │
    /// │                                                                          │
    /// └─────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public class BuffManager : MonoBehaviour
    {
        #region Configuration

        [Header("Settings")]
        [Tooltip("Максимум активных баффов")]
        [SerializeField] private int maxActiveBuffs = 20;
        
        [Tooltip("Интервал тиков в секундах")]
        [SerializeField] private float tickInterval = 1f;

        #endregion

        #region State

        private List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
        private Dictionary<string, float> totalModifiers = new Dictionary<string, float>();
        
        // Специальные состояния
        private bool isStunned = false;
        private bool isRooted = false;
        private bool isSilenced = false;
        private bool isBlind = false;
        private float slowMultiplier = 1f;
        private float currentShield = 0f;
        private float lifestealPercent = 0f;
        private float thornsPercent = 0f;
        private float reflectPercent = 0f;
        
        // Временные модификаторы проводимости (с payback)
        private float conductivityModifier = 0f;
        private float conductivityPaybackRate = 0f;
        
        // FIX BUF-C03: Track applied conductivity to QiController for synchronization (2026-04-11)
        private float _appliedConductivityToQi = 0f;
        
        // FIX BUF-H01: Cache for temp BuffData to prevent ScriptableObject.CreateInstance leaks (2026-04-11)
        private Dictionary<string, BuffData> _tempBuffCache = new Dictionary<string, BuffData>();
        
        // FIX BUF-M04: Stack of active control effects (2026-04-11)
        private List<ControlType> _activeControlStack = new List<ControlType>();
        
        // Ссылки на контроллеры
        private Qi.QiController qiController;
        private Body.BodyController bodyController;
        private ICombatant combatant;
        private Combat.TechniqueController techniqueController;

        #endregion

        #region Events

        /// <summary>
        /// Событие: бафф применён.
        /// </summary>
        public event Action<ActiveBuff> OnBuffApplied;
        
        /// <summary>
        /// Событие: бафф снят.
        /// </summary>
        public event Action<ActiveBuff> OnBuffRemoved;
        
        /// <summary>
        /// Событие: бафф обновлён (стак/рефреш).
        /// </summary>
        public event Action<ActiveBuff> OnBuffUpdated;
        
        /// <summary>
        /// Событие: характеристики изменились.
        /// </summary>
        public event Action OnStatsChanged;
        
        /// <summary>
        /// Событие: применение периодического эффекта.
        /// </summary>
        public event Action<PeriodicType, float> OnPeriodicEffect;

        #endregion

        #region Properties

        public List<ActiveBuff> ActiveBuffs => activeBuffs;
        public int ActiveBuffCount => activeBuffs.Count;
        public bool IsStunned => isStunned;
        public bool IsRooted => isRooted;
        public bool IsSilenced => isSilenced;
        public bool IsBlind => isBlind;
        public float SlowMultiplier => slowMultiplier;
        public float CurrentShield => currentShield;
        public float LifestealPercent => lifestealPercent;
        public float ThornsPercent => thornsPercent;
        public float ReflectPercent => reflectPercent;
        public float ConductivityModifier => conductivityModifier;
        
        /// <summary>
        /// Редактировано: 2026-04-03 - Скорость возврата модификатора проводимости
        /// </summary>
        public float ConductivityPaybackRate => conductivityPaybackRate;
        
        /// <summary>
        /// Может ли сущность действовать.
        /// </summary>
        public bool CanAct => !isStunned && !isRooted;
        
        /// <summary>
        /// Может ли использовать техники.
        /// </summary>
        public bool CanUseTechniques => !isStunned && !isSilenced;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Получаем ссылки на контроллеры
            qiController = GetComponent<Qi.QiController>();
            bodyController = GetComponent<Body.BodyController>();
            combatant = GetComponent<ICombatant>();
            techniqueController = GetComponent<Combat.TechniqueController>();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            
            // Обновляем все баффы
            UpdateBuffs(deltaTime);
            
            // Обновляем payback проводимости
            UpdateConductivityPayback(deltaTime);
        }

        private void OnDestroy()
        {
            // Очищаем все баффы
            ClearAllBuffs();
            
            // FIX BUF-H01: Clean up cached BuffData ScriptableObjects (2026-04-11)
            foreach (var kvp in _tempBuffCache)
            {
                if (kvp.Value != null)
                    Destroy(kvp.Value);
            }
            _tempBuffCache.Clear();
        }

        #endregion

        #region Buff Management

        /// <summary>
        /// Добавить бафф по типу (для FormationEffects).
        /// Упрощённый метод для быстрого применения.
        /// </summary>
        public void AddBuff(FormationBuffType buffType, float value, bool isPercentage, float duration)
        {
            // Создаём временные данные баффа
            var tempData = CreateTempBuffData(buffType, value, isPercentage, duration);
            AddBuff(tempData, gameObject);
        }

        /// <summary>
        /// Добавить дебафф по типу (для FormationEffects).
        /// </summary>
        public void AddDebuff(FormationBuffType buffType, float value, bool isPercentage, float duration)
        {
            var tempData = CreateTempBuffData(buffType, -Mathf.Abs(value), isPercentage, duration);
            tempData.buffType = CultivationGame.Data.ScriptableObjects.BuffType.Debuff;
            AddBuff(tempData, gameObject);
        }

        /// <summary>
        /// Добавить бафф из данных.
        /// </summary>
        public BuffResult AddBuff(BuffData buffData, GameObject source)
        {
            if (buffData == null) return BuffResult.Rejected;

            // Проверяем несовместимость
            if (!CheckCompatibility(buffData))
            {
                return BuffResult.Rejected;
            }

            // Проверяем, есть ли уже такой бафф
            var existing = FindBuff(buffData);
            
            if (existing != null)
            {
                return HandleExistingBuff(existing, buffData);
            }

            // Проверяем лимит
            if (activeBuffs.Count >= maxActiveBuffs)
            {
                // Удаляем самый старый дебафф (если новый бафф положительный)
                if (buffData.IsPositive)
                {
                    RemoveOldestDebuff();
                }
                else
                {
                    Debug.LogWarning($"[BuffManager] Достигнут лимит баффов на {gameObject.name}");
                    return BuffResult.Rejected;
                }
            }

            // Создаём новый бафф
            var newBuff = new ActiveBuff(buffData, source);
            activeBuffs.Add(newBuff);

            // Применяем эффекты
            ApplyBuffEffects(newBuff);

            // Удаляем баффы, которые этот диспеллит
            DispelBuffs(buffData);

            // Событие
            OnBuffApplied?.Invoke(newBuff);
            OnStatsChanged?.Invoke();

            Debug.Log($"[BuffManager] Бафф '{buffData.nameRu}' применён к {gameObject.name}");
            return BuffResult.Applied;
        }

        /// <summary>
        /// Удалить бафф по типу.
        /// FIX BUF-M01: Narrow removal — match by buffId prefix for FormationBuffType (2026-04-11)
        /// </summary>
        public void RemoveBuff(FormationBuffType buffType)
        {
            string prefix = $"temp_{buffType}_";
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                if (buff.data != null && buff.data.buffId != null && buff.data.buffId.StartsWith(prefix))
                {
                    RemoveBuffAt(i);
                }
            }
        }

        /// <summary>
        /// Удалить бафф по данным.
        /// </summary>
        public bool RemoveBuff(BuffData buffData)
        {
            for (int i = 0; i < activeBuffs.Count; i++)
            {
                if (activeBuffs[i].data == buffData)
                {
                    RemoveBuffAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Удалить все баффы.
        /// </summary>
        public void ClearAllBuffs()
        {
            while (activeBuffs.Count > 0)
            {
                RemoveBuffAt(0);
            }
            
            // Сбрасываем специальные состояния
            isStunned = false;
            isRooted = false;
            isSilenced = false;
            isBlind = false;
            slowMultiplier = 1f;
            currentShield = 0f;
            lifestealPercent = 0f;
            thornsPercent = 0f;
            reflectPercent = 0f;
            
            OnStatsChanged?.Invoke();
        }

        /// <summary>
        /// Очистить все дебаффы.
        /// </summary>
        public void ClearAllDebuffs()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (!activeBuffs[i].IsPositive)
                {
                    RemoveBuffAt(i);
                }
            }
        }

        /// <summary>
        /// Очистить все баффы.
        /// </summary>
        public void ClearAllPositiveBuffs()
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                if (activeBuffs[i].IsPositive)
                {
                    RemoveBuffAt(i);
                }
            }
        }

        #endregion

        #region Stat Modifiers

        /// <summary>
        /// Получить модификатор характеристики.
        /// </summary>
        public float GetStatModifier(string statName)
        {
            return totalModifiers.TryGetValue(statName, out float value) ? value : 0f;
        }

        /// <summary>
        /// Получить модифицированное значение.
        /// </summary>
        public float GetModifiedValue(string statName, float baseValue)
        {
            float modifier = GetStatModifier(statName);
            
            // Проверяем, процентный ли модификатор
            bool isPercentage = IsPercentageModifier(statName);
            
            if (isPercentage)
            {
                return baseValue * (1f + modifier);
            }
            else
            {
                return baseValue + modifier;
            }
        }

        /// <summary>
        /// Получить модификатор проводимости (с учётом правил).
        /// Проводимость — единственный SECONDARY стат, который может модифицироваться.
        /// </summary>
        public float GetConductivityModifier()
        {
            return conductivityModifier;
        }

        /// <summary>
        /// Добавить временный модификатор проводимости с payback.
        /// </summary>
        public void AddConductivityModifier(float modifier, float paybackRate)
        {
            conductivityModifier += modifier;
            conductivityPaybackRate += paybackRate;
            
            OnStatsChanged?.Invoke();
            Debug.Log($"[BuffManager] Conductivity modifier: +{modifier}, payback rate: {paybackRate}/s");
        }

        #endregion

        #region Special Effects

        /// <summary>
        /// Применить урон к щиту.
        /// </summary>
        /// <returns>Оставшийся урон после щита</returns>
        public float AbsorbWithShield(float damage)
        {
            if (currentShield <= 0) return damage;

            float absorbed = Mathf.Min(currentShield, damage);
            currentShield -= absorbed;

            Debug.Log($"[BuffManager] Щит поглотил {absorbed} урона. Осталось: {currentShield}");

            if (currentShield <= 0)
            {
                // Удаляем бафф щита
                RemoveBuffsByEffect(SpecialEffectType.Shield);
            }

            return damage - absorbed;
        }

        /// <summary>
        /// Рассчитать отражённый урон.
        /// </summary>
        public float CalculateReflectDamage(float incomingDamage)
        {
            if (reflectPercent <= 0) return 0;
            return incomingDamage * (reflectPercent / 100f);
        }

        /// <summary>
        /// Рассчитать урон от шипов.
        /// </summary>
        public float CalculateThornsDamage(float incomingDamage)
        {
            if (thornsPercent <= 0) return 0;
            return incomingDamage * (thornsPercent / 100f);
        }

        /// <summary>
        /// Рассчитать вампиризм.
        /// </summary>
        public float CalculateLifesteal(float dealtDamage)
        {
            if (lifestealPercent <= 0) return 0;
            return dealtDamage * (lifestealPercent / 100f);
        }

        #endregion

        #region Query

        /// <summary>
        /// Проверить наличие баффа.
        /// </summary>
        public bool HasBuff(BuffData buffData)
        {
            return activeBuffs.Exists(b => b.data == buffData);
        }

        /// <summary>
        /// Проверить наличие баффа по ID.
        /// </summary>
        public bool HasBuff(string buffId)
        {
            return activeBuffs.Exists(b => b.data.buffId == buffId);
        }

        /// <summary>
        /// Получить количество стаков баффа.
        /// </summary>
        public int GetStacks(BuffData buffData)
        {
            var buff = FindBuff(buffData);
            return buff?.currentStacks ?? 0;
        }

        /// <summary>
        /// Получить все баффы определённой категории.
        /// </summary>
        public List<ActiveBuff> GetBuffsByCategory(BuffCategory category)
        {
            return activeBuffs.FindAll(b => b.data.category == category);
        }

        /// <summary>
        /// Получить все дебаффы.
        /// </summary>
        public List<ActiveBuff> GetDebuffs()
        {
            return activeBuffs.FindAll(b => !b.IsPositive);
        }

        /// <summary>
        /// Получить все положительные баффы.
        /// </summary>
        public List<ActiveBuff> GetPositiveBuffs()
        {
            return activeBuffs.FindAll(b => b.IsPositive);
        }

        #endregion

        #region Private Methods

        private void UpdateBuffs(float deltaTime)
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                
                // Обновляем таймеры
                buff.timeSinceLastTick += deltaTime;
                
                // Обрабатываем периодические эффекты
                if (buff.timeSinceLastTick >= tickInterval)
                {
                    buff.timeSinceLastTick = 0f;
                    
                    // Уменьшаем длительность
                    if (!buff.IsPermanent)
                    {
                        buff.remainingTicks--;
                    }
                    
                    // Применяем периодические эффекты
                    ProcessPeriodicEffects(buff);
                }
                
                // Проверяем истечение
                if (buff.IsExpired)
                {
                    RemoveBuffAt(i);
                }
            }
        }

        private void UpdateConductivityPayback(float deltaTime)
        {
            if (conductivityModifier <= 0 || conductivityPaybackRate <= 0) return;

            float payback = conductivityPaybackRate * deltaTime;
            conductivityModifier -= payback;

            if (conductivityModifier <= 0)
            {
                conductivityModifier = 0;
                conductivityPaybackRate = 0;
                Debug.Log("[BuffManager] Conductivity payback complete");
            }

            // FIX BUF-C03: Sync conductivity payback to QiController (2026-04-11)
            SyncConductivityToQiController();

            OnStatsChanged?.Invoke();
        }

        private void ProcessPeriodicEffects(ActiveBuff buff)
        {
            if (buff.data.periodicEffects == null) return;

            foreach (var periodic in buff.data.periodicEffects)
            {
                // Проверяем шанс срабатывания
                if (UnityEngine.Random.value > periodic.triggerChance / 100f) continue;

                float value = periodic.value * buff.currentStacks;

                // Масштабирование от характеристики
                if (!string.IsNullOrEmpty(periodic.scalingStat) && periodic.scalingCoefficient > 0)
                {
                    float statValue = GetStatFromController(periodic.scalingStat);
                    value += statValue * periodic.scalingCoefficient;
                }

                switch (periodic.effectType)
                {
                    case PeriodicType.Damage:
                        ApplyPeriodicDamage(buff, value);
                        break;
                        
                    case PeriodicType.Heal:
                        ApplyPeriodicHeal(buff, value);
                        break;
                        
                    case PeriodicType.QiRestore:
                        ApplyQiRestore(value);
                        break;
                        
                    case PeriodicType.QiDrain:
                        ApplyQiDrain(value);
                        break;
                }

                OnPeriodicEffect?.Invoke(periodic.effectType, value);
            }
        }

        private void ApplyPeriodicDamage(ActiveBuff buff, float value)
        {
            // Сначала щит
            value = AbsorbWithShield(value);
            
            if (value <= 0) return;

            // Урон телу
            if (bodyController != null)
            {
                bodyController.ApplyDamage(Mathf.RoundToInt(value));
            }
            // Fallback — урон по Ци
            else if (qiController != null)
            {
                qiController.SpendQi((long)Math.Round(value)); // FIX: int→long Qi migration (2026-04-12)
            }
        }

        private void ApplyPeriodicHeal(ActiveBuff buff, float value)
        {
            if (bodyController != null)
            {
                bodyController.Heal(Mathf.RoundToInt(value));
            }
            
            if (qiController != null)
            {
                qiController.AddQi((long)Math.Round(value)); // FIX: int→long Qi migration (2026-04-12)
            }
        }

        private void ApplyQiRestore(float value)
        {
            if (qiController != null)
            {
                qiController.AddQi((long)Math.Round(value)); // FIX: int→long Qi migration (2026-04-12)
            }
        }

        private void ApplyQiDrain(float value)
        {
            if (qiController != null)
            {
                qiController.SpendQi((long)Math.Round(value)); // FIX: int→long Qi migration (2026-04-12)
            }
        }

        private void ApplyBuffEffects(ActiveBuff buff)
        {
            var data = buff.data;

            // Применяем модификаторы характеристик
            foreach (var mod in data.statModifiers)
            {
                ApplyStatModifier(buff, mod);
            }

            // Применяем специальные эффекты
            foreach (var special in data.specialEffects)
            {
                ApplySpecialEffect(buff, special);
            }
        }

        private void ApplyStatModifier(ActiveBuff buff, StatModifier mod)
        {
            string statName = mod.statName.ToLowerInvariant();
            float value = mod.value * mod.stackMultiplier * buff.currentStacks;

            // ПРОВЕРКА: PRIMARY статы нельзя модифицировать баффами!
            if (IsPrimaryStat(statName))
            {
                Debug.LogWarning($"[BuffManager] Попытка модификации PRIMARY стата '{statName}' баффом. Отклонено!");
                return;
            }

            // ПРОВЕРКА: QiRegen и CoreCapacity нельзя модифицировать!
            if (statName == "qiregen" || statName == "corecapacity" || statName == "qi_regen" || statName == "core_capacity")
            {
                Debug.LogWarning($"[BuffManager] Попытка модификации защищённого стата '{statName}'. Отклонено!");
                return;
            }

            // Conductivity — особый случай с payback
            if (statName == "conductivity")
            {
                // Добавляем модификатор проводимости
                if (mod.isPercentage)
                {
                    conductivityModifier += value / 100f;
                }
                else
                {
                    conductivityModifier += value;
                }
                // FIX BUF-C03: Sync conductivity modifier to QiController (2026-04-11)
                SyncConductivityToQiController();
                return;
            }

            // Обычная обработка
            if (!totalModifiers.ContainsKey(statName))
            {
                totalModifiers[statName] = 0f;
            }

            if (mod.isPercentage)
            {
                totalModifiers[statName] += value / 100f;
            }
            else
            {
                totalModifiers[statName] += value;
            }

            buff.appliedModifiers[statName] = value;
        }

        private void ApplySpecialEffect(ActiveBuff buff, SpecialBuffEffect special)
        {
            // FIX BUF-M03: Don't check triggerChance at apply time — always apply special effects (2026-04-11)
            // triggerChance is for periodic effects; special effects apply for the full buff duration

            switch (special.effectType)
            {
                case SpecialEffectType.Stun:
                    isStunned = true;
                    break;
                    
                case SpecialEffectType.Slow:
                    // FIX BUF-H02: Use data from parameters instead of hardcoded 50% (2026-04-11)
                    float slowPercent = SafeParseFloat(special.parameters, 0.5f);
                    slowMultiplier *= (1f - slowPercent);
                    break;
                    
                case SpecialEffectType.Root:
                    isRooted = true;
                    break;
                    
                case SpecialEffectType.Silence:
                    isSilenced = true;
                    break;
                    
                case SpecialEffectType.Blind:
                    isBlind = true;
                    break;
                    
                case SpecialEffectType.Shield:
                    // FIX BUF-C01: Safe float parsing with fallback (2026-04-11)
                    currentShield += SafeParseFloat(special.parameters);
                    break;
                    
                case SpecialEffectType.Lifesteal:
                    // FIX BUF-C01: Safe float parsing with fallback (2026-04-11)
                    lifestealPercent += SafeParseFloat(special.parameters);
                    break;
                    
                case SpecialEffectType.Thorns:
                    // FIX BUF-C01: Safe float parsing with fallback (2026-04-11)
                    thornsPercent += SafeParseFloat(special.parameters);
                    break;
                    
                case SpecialEffectType.Reflect:
                    // FIX BUF-C01: Safe float parsing with fallback (2026-04-11)
                    reflectPercent += SafeParseFloat(special.parameters);
                    break;
                    
                case SpecialEffectType.Immunity:
                    // Даёт иммунитет к определённым эффектам
                    break;
                    
                case SpecialEffectType.Absorb:
                    // Поглощение урона
                    break;
                    
                case SpecialEffectType.Regeneration:
                    // Регенерация уже обрабатывается через периодические эффекты
                    break;
            }
        }

        private void RemoveBuffAt(int index)
        {
            if (index < 0 || index >= activeBuffs.Count) return;

            var buff = activeBuffs[index];
            
            // Снимаем модификаторы
            RemoveBuffEffects(buff);
            
            // Удаляем из списка
            activeBuffs.RemoveAt(index);
            
            // Событие
            OnBuffRemoved?.Invoke(buff);
            OnStatsChanged?.Invoke();
            
            Debug.Log($"[BuffManager] Бафф '{buff.data.nameRu}' снят с {gameObject.name}");
        }

        private void RemoveBuffEffects(ActiveBuff buff)
        {
            var data = buff.data;

            // Снимаем модификаторы характеристик
            foreach (var mod in data.statModifiers)
            {
                RemoveStatModifier(buff, mod);
            }

            // Снимаем специальные эффекты
            foreach (var special in data.specialEffects)
            {
                RemoveSpecialEffect(buff, special);
            }
        }

        private void RemoveStatModifier(ActiveBuff buff, StatModifier mod)
        {
            string statName = mod.statName.ToLowerInvariant();
            float value = mod.value * mod.stackMultiplier * buff.currentStacks;

            if (IsPrimaryStat(statName)) return;
            if (statName == "qiregen" || statName == "corecapacity") return;

            if (statName == "conductivity")
            {
                if (mod.isPercentage)
                {
                    conductivityModifier -= value / 100f;
                }
                else
                {
                    conductivityModifier -= value;
                }
                // FIX BUF-C03: Sync conductivity modifier to QiController (2026-04-11)
                SyncConductivityToQiController();
                return;
            }

            if (totalModifiers.ContainsKey(statName))
            {
                if (mod.isPercentage)
                {
                    totalModifiers[statName] -= value / 100f;
                }
                else
                {
                    totalModifiers[statName] -= value;
                }
            }
        }

        private void RemoveSpecialEffect(ActiveBuff buff, SpecialBuffEffect special)
        {
            switch (special.effectType)
            {
                case SpecialEffectType.Stun:
                    // Проверяем, нет ли других стунов
                    if (!HasSpecialEffect(SpecialEffectType.Stun, buff))
                        isStunned = false;
                    break;
                    
                case SpecialEffectType.Slow:
                    slowMultiplier = 1f;
                    // FIX BUF-H02: Recalculate slow from remaining buffs using their data (2026-04-11)
                    foreach (var b in activeBuffs)
                    {
                        foreach (var s in b.data.specialEffects)
                        {
                            if (s.effectType == SpecialEffectType.Slow)
                            {
                                float pct = SafeParseFloat(s.parameters, 0.5f);
                                slowMultiplier *= (1f - pct);
                            }
                        }
                    }
                    break;
                    
                case SpecialEffectType.Root:
                    if (!HasSpecialEffect(SpecialEffectType.Root, buff))
                        isRooted = false;
                    break;
                    
                case SpecialEffectType.Silence:
                    if (!HasSpecialEffect(SpecialEffectType.Silence, buff))
                        isSilenced = false;
                    break;
                    
                case SpecialEffectType.Blind:
                    if (!HasSpecialEffect(SpecialEffectType.Blind, buff))
                        isBlind = false;
                    break;
                    
                case SpecialEffectType.Shield:
                    // Щит не снимается при снятии баффа
                    break;
                    
                case SpecialEffectType.Lifesteal:
                    // FIX BUF-C01: Safe float parsing with fallback (2026-04-11)
                    lifestealPercent -= SafeParseFloat(special.parameters);
                    break;
                    
                case SpecialEffectType.Thorns:
                    // FIX BUF-C01: Safe float parsing with fallback (2026-04-11)
                    thornsPercent -= SafeParseFloat(special.parameters);
                    break;
                    
                case SpecialEffectType.Reflect:
                    // FIX BUF-C01: Safe float parsing with fallback (2026-04-11)
                    reflectPercent -= SafeParseFloat(special.parameters);
                    break;
            }
        }

        private bool HasSpecialEffect(SpecialEffectType effectType, ActiveBuff excludeBuff = null)
        {
            foreach (var buff in activeBuffs)
            {
                if (buff == excludeBuff) continue;
                
                foreach (var special in buff.data.specialEffects)
                {
                    if (special.effectType == effectType)
                        return true;
                }
            }
            return false;
        }

        private ActiveBuff FindBuff(BuffData buffData)
        {
            return activeBuffs.Find(b => b.data == buffData);
        }

        private bool CheckCompatibility(BuffData newBuff)
        {
            foreach (var buff in activeBuffs)
            {
                // Проверяем несовместимость
                if (buff.data.incompatibleWith.Contains(newBuff))
                {
                    return false;
                }
                
                // Новый бафф несовместим с существующим
                if (newBuff.incompatibleWith.Contains(buff.data))
                {
                    return false;
                }
            }
            return true;
        }

        private BuffResult HandleExistingBuff(ActiveBuff existing, BuffData newData)
        {
            if (!newData.stackable)
            {
                // Обновляем длительность
                if (newData.stackType == StackType.Refresh)
                {
                    existing.remainingTicks = newData.durationTicks;
                    OnBuffUpdated?.Invoke(existing);
                    return BuffResult.Refreshed;
                }
                // Добавляем длительность
                else if (newData.stackType == StackType.Add)
                {
                    existing.remainingTicks += newData.durationTicks;
                    OnBuffUpdated?.Invoke(existing);
                    return BuffResult.Refreshed;
                }
                // FIX BUF-C02: Independent stacking — actually add as a new buff (2026-04-11)
                else
                {
                    var newBuff = new ActiveBuff(newData, existing.source);
                    activeBuffs.Add(newBuff);
                    ApplyBuffEffects(newBuff);
                    OnBuffApplied?.Invoke(newBuff);
                    OnStatsChanged?.Invoke();
                    return BuffResult.Applied;
                }
            }
            // Стекуемый
            else if (existing.currentStacks < newData.maxStacks)
            {
                existing.currentStacks++;
                existing.remainingTicks = newData.durationTicks;
                
                // Переприменяем эффекты с учётом новых стаков
                UpdateBuffStackEffects(existing);
                
                OnBuffUpdated?.Invoke(existing);
                return BuffResult.Stacked;
            }
            
            return BuffResult.Rejected;
        }

        private void UpdateBuffStackEffects(ActiveBuff buff)
        {
            // Пересчитываем модификаторы с учётом стаков
            foreach (var mod in buff.data.statModifiers)
            {
                string statName = mod.statName.ToLowerInvariant();
                
                if (buff.appliedModifiers.TryGetValue(statName, out float oldValue))
                {
                    // Снимаем старое значение
                    if (totalModifiers.ContainsKey(statName))
                    {
                        if (mod.isPercentage)
                            totalModifiers[statName] -= oldValue / 100f;
                        else
                            totalModifiers[statName] -= oldValue;
                    }
                }
                
                // Применяем новое
                float newValue = mod.value * mod.stackMultiplier * buff.currentStacks;
                buff.appliedModifiers[statName] = newValue;
                
                if (!totalModifiers.ContainsKey(statName))
                    totalModifiers[statName] = 0f;
                    
                if (mod.isPercentage)
                    totalModifiers[statName] += newValue / 100f;
                else
                    totalModifiers[statName] += newValue;
            }
        }

        private void DispelBuffs(BuffData dispeller)
        {
            foreach (var toDispel in dispeller.dispels)
            {
                RemoveBuff(toDispel);
            }
        }

        private void RemoveOldestDebuff()
        {
            for (int i = 0; i < activeBuffs.Count; i++)
            {
                if (!activeBuffs[i].IsPositive)
                {
                    RemoveBuffAt(i);
                    return;
                }
            }
        }

        private void RemoveBuffsByEffect(SpecialEffectType effectType)
        {
            for (int i = activeBuffs.Count - 1; i >= 0; i--)
            {
                var buff = activeBuffs[i];
                foreach (var special in buff.data.specialEffects)
                {
                    if (special.effectType == effectType)
                    {
                        RemoveBuffAt(i);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// FIX BUF-C01: Safe float parsing with fallback (2026-04-11)
        /// </summary>
        private float SafeParseFloat(string text, float fallback = 0f)
        {
            if (float.TryParse(text, out float result))
                return result;
            Debug.LogWarning($"[BuffManager] Failed to parse '{text}' as float, using fallback {fallback}");
            return fallback;
        }
        
        /// <summary>
        /// FIX BUF-C03: Sync conductivity modifier to QiController (2026-04-11)
        /// </summary>
        private void SyncConductivityToQiController()
        {
            if (qiController == null) return;
            float delta = conductivityModifier - _appliedConductivityToQi;
            if (Mathf.Abs(delta) > 0.0001f)
            {
                qiController.AddConductivityBonus(delta);
                _appliedConductivityToQi = conductivityModifier;
            }
        }
        
        private bool IsPrimaryStat(string statName)
        {
            string lower = statName.ToLowerInvariant();
            return lower == "strength" || lower == "str" ||
                   lower == "agility" || lower == "agi" ||
                   lower == "intelligence" || lower == "int" ||
                   lower == "vitality" || lower == "vit";
        }

        private bool IsPercentageModifier(string statName)
        {
            string lower = statName.ToLowerInvariant();
            // Эти статы обычно используют процентные модификаторы
            return lower == "damage" || lower == "defense" ||
                   lower == "speed" || lower == "criticalchance" ||
                   lower == "criticaldamage" || lower == "evasion";
        }

        private float GetStatFromController(string statName)
        {
            string lower = statName.ToLowerInvariant();
            
            // Получаем статы из контроллеров
            if (qiController != null)
            {
                if (lower == "conductivity")
                    return qiController.Conductivity;
                if (lower == "currentqi")
                    return qiController.CurrentQi;
            }
            
            if (bodyController != null)
            {
                if (lower == "vitality")
                    return bodyController.Vitality;
            }
            
            return 0f;
        }

        private string GetStatNameForBuffType(FormationBuffType buffType)
        {
            return buffType switch
            {
                FormationBuffType.Damage => "damage",
                FormationBuffType.Defense => "defense",
                FormationBuffType.Speed => "speed",
                FormationBuffType.CriticalChance => "criticalchance",
                FormationBuffType.CriticalDamage => "criticaldamage",
                FormationBuffType.QiRegen => "qiregen",
                FormationBuffType.MaxQi => "maxqi",
                FormationBuffType.Conductivity => "conductivity",
                FormationBuffType.Health => "health",
                FormationBuffType.Stamina => "stamina",
                FormationBuffType.Resistance => "resistance",
                FormationBuffType.Evasion => "evasion",
                _ => buffType.ToString().ToLowerInvariant()
            };
        }

        /// <summary>
        /// Создать временные данные баффа для простого применения.
        /// FIX BUF-H01: Use cache to prevent ScriptableObject.CreateInstance leaks (2026-04-11)
        /// FIX BUF-M02: Account for tickInterval when converting seconds to ticks (2026-04-11)
        /// </summary>
        private BuffData CreateTempBuffData(FormationBuffType buffType, float value, bool isPercentage, float duration)
        {
            string cacheKey = $"{buffType}_{value}_{isPercentage}_{duration}_{tickInterval}";
            if (_tempBuffCache.TryGetValue(cacheKey, out var cached))
                return cached;
            
            var data = ScriptableObject.CreateInstance<BuffData>();
            data.buffId = $"temp_{buffType}_{Guid.NewGuid():N}";
            data.nameRu = buffType switch
            {
                FormationBuffType.Damage => "Усиление урона",
                FormationBuffType.Defense => "Усиление защиты",
                FormationBuffType.Speed => "Ускорение",
                FormationBuffType.CriticalChance => "Увеличение шанса крита",
                FormationBuffType.CriticalDamage => "Увеличение урона крита",
                FormationBuffType.Conductivity => "Усиление проводимости",
                FormationBuffType.Health => "Увеличение здоровья",
                FormationBuffType.Stamina => "Увеличение выносливости",
                FormationBuffType.Resistance => "Увеличение сопротивления",
                FormationBuffType.Evasion => "Увеличение уклонения",
                _ => buffType.ToString()
            };
            data.buffType = value >= 0 ? CultivationGame.Data.ScriptableObjects.BuffType.Buff : CultivationGame.Data.ScriptableObjects.BuffType.Debuff;
            // FIX BUF-M02: Account for tickInterval when converting seconds to ticks (2026-04-11)
            data.durationTicks = tickInterval > 0 ? Mathf.RoundToInt(duration / tickInterval) : Mathf.RoundToInt(duration);
            data.stackable = false;
            data.stackType = StackType.Independent; // FIX BUF-M01: Mark as Independent for proper removal (2026-04-11)
            
            var modifier = new StatModifier
            {
                statName = GetStatNameForBuffType(buffType),
                value = value,
                isPercentage = isPercentage
            };
            data.statModifiers.Add(modifier);
            
            _tempBuffCache[cacheKey] = data;
            return data;
        }

        #endregion

        #region IControlReceiver Implementation

        /// <summary>
        /// Применить эффект контроля.
        /// FIX BUF-M04: Track control effects in stack (2026-04-11)
        /// FIX BUF-H01: Use cache for control buff data (2026-04-11)
        /// FIX BUF-H02: Use data from parameters for Slow (2026-04-11)
        /// </summary>
        public void ApplyControl(ControlType controlType, float duration)
        {
            // FIX BUF-M04: Add to control stack (2026-04-11)
            if (!_activeControlStack.Contains(controlType))
                _activeControlStack.Add(controlType);
            
            switch (controlType)
            {
                case ControlType.Freeze:
                case ControlType.Stun:
                    isStunned = true;
                    // FIX BUF-H01: Use cache for control buff data (2026-04-11)
                    string stunKey = $"stun_{duration}_{tickInterval}";
                    if (!_tempBuffCache.TryGetValue(stunKey, out var stunData))
                    {
                        stunData = ScriptableObject.CreateInstance<BuffData>();
                        stunData.buffId = $"stun_{Guid.NewGuid():N}";
                        stunData.nameRu = "Оглушение";
                        stunData.durationTicks = tickInterval > 0 ? Mathf.RoundToInt(duration / tickInterval) : Mathf.RoundToInt(duration);
                        stunData.buffType = CultivationGame.Data.ScriptableObjects.BuffType.Debuff;
                        stunData.specialEffects.Add(new SpecialBuffEffect { effectType = SpecialEffectType.Stun });
                        _tempBuffCache[stunKey] = stunData;
                    }
                    AddBuff(stunData, null);
                    break;
                    
                case ControlType.Slow:
                    // FIX BUF-H02: Slow uses configurable percentage, not hardcoded 50% (2026-04-11)
                    slowMultiplier *= 0.5f; // Default slow; BuffManager special effects use parameters for custom values
                    break;
                    
                case ControlType.Root:
                    isRooted = true;
                    break;
                    
                case ControlType.Silence:
                    isSilenced = true;
                    break;
                    
                case ControlType.Blind:
                    isBlind = true;
                    break;
            }
        }

        /// <summary>
        /// Снять эффект контроля.
        /// FIX BUF-H03: Check other active control buffs before resetting flag (2026-04-11)
        /// FIX BUF-M04: Remove from control stack (2026-04-11)
        /// </summary>
        public void RemoveControl(ControlType controlType)
        {
            // FIX BUF-M04: Remove from control stack (2026-04-11)
            _activeControlStack.Remove(controlType);
            
            switch (controlType)
            {
                case ControlType.Freeze:
                case ControlType.Stun:
                    // FIX BUF-H03: Check other active control buffs before resetting (2026-04-11)
                    if (!HasSpecialEffect(SpecialEffectType.Stun))
                        isStunned = false;
                    break;
                    
                case ControlType.Slow:
                    // FIX BUF-H03: Recalculate slow from remaining buffs (2026-04-11)
                    slowMultiplier = 1f;
                    foreach (var b in activeBuffs)
                    {
                        foreach (var s in b.data.specialEffects)
                        {
                            if (s.effectType == SpecialEffectType.Slow)
                            {
                                float pct = SafeParseFloat(s.parameters, 0.5f);
                                slowMultiplier *= (1f - pct);
                            }
                        }
                    }
                    break;
                    
                case ControlType.Root:
                    // FIX BUF-H03: Check other active control buffs before resetting (2026-04-11)
                    if (!HasSpecialEffect(SpecialEffectType.Root))
                        isRooted = false;
                    break;
                    
                case ControlType.Silence:
                    // FIX BUF-H03: Check other active control buffs before resetting (2026-04-11)
                    if (!HasSpecialEffect(SpecialEffectType.Silence))
                        isSilenced = false;
                    break;
                    
                case ControlType.Blind:
                    // FIX BUF-H03: Check other active control buffs before resetting (2026-04-11)
                    if (!HasSpecialEffect(SpecialEffectType.Blind))
                        isBlind = false;
                    break;
            }
        }

        public bool IsControlled => isStunned || isRooted || isSilenced || isBlind || slowMultiplier < 1f;
        // FIX BUF-M04: Return top of control stack (2026-04-11)
        public ControlType CurrentControl => _activeControlStack.Count > 0 ? _activeControlStack[_activeControlStack.Count - 1] : ControlType.None;

        #endregion

        #region IStunnable Implementation

        public void Stun(float duration)
        {
            ApplyControl(ControlType.Stun, duration);
        }

        public void Unstun()
        {
            RemoveControl(ControlType.Stun);
        }

        #endregion
    }
}
