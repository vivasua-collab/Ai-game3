// ============================================================================
// QiController.cs — Контроллер Ци
// Cultivation World Simulator
// Версия: 1.1 — Исправлены критические ошибки аудита
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:17:18 UTC
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;

namespace CultivationGame.Qi
{
    /// <summary>
    /// Контроллер Ци — управляет накоплением, расходом и регенерацией Ци.
    /// </summary>
    public class QiController : MonoBehaviour
    {
        [Header("Cultivation")]
        [SerializeField] private int cultivationLevel = 1;
        [SerializeField] private int cultivationSubLevel = 0;
        [SerializeField] private CoreQuality coreQuality = CoreQuality.Normal;
        
        [Header("Qi Stats")]
        [SerializeField] private long coreCapacity = 1000;
        [SerializeField] private long currentQi = 1000;
        [SerializeField] private float conductivity = 1.0f;
        
        [Header("Regeneration")]
        [SerializeField] private bool enablePassiveRegen = true;
        [SerializeField] private float regenMultiplier = 1f;
        
        // === Runtime ===
        
        private long maxQiCapacity;
        private float qiDensity;
        private float dailyAccumulator = 0f;
        
        // === Events ===
        
        public event Action<long, long> OnQiChanged; // (current, max)
        public event Action OnQiDepleted;
        public event Action OnQiFull;
        public event Action<int> OnCultivationLevelChanged;
        
        // === Properties ===
        
        public long CurrentQi => currentQi;
        public long MaxQi => maxQiCapacity;
        public long CoreCapacity => coreCapacity;
        public float Conductivity => conductivity;
        public int CultivationLevel => cultivationLevel;
        public float QiPercent => maxQiCapacity > 0 ? (float)currentQi / maxQiCapacity : 0f;
        public float QiDensity => qiDensity;
        public bool IsFull => currentQi >= maxQiCapacity;
        public bool IsEmpty => currentQi <= 0;
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            RecalculateStats();
        }
        
        private void Update()
        {
            if (enablePassiveRegen)
            {
                ProcessPassiveRegeneration();
            }
        }
        
        // === Initialization ===
        
        /// <summary>
        /// Пересчитать характеристики на основе уровня культивации.
        /// </summary>
        public void RecalculateStats()
        {
            // Плотность Ци = 2^(level-1)
            qiDensity = Mathf.Pow(2, cultivationLevel - 1);
            
            // Максимальная ёмкость
            maxQiCapacity = CalculateMaxCapacity();
            
            // Ограничиваем текущее Ци
            currentQi = Math.Min(currentQi, maxQiCapacity);
            
            // Проводимость = coreCapacity / 360 секунд (по документации QI_SYSTEM.md)
            // Это определяет скорость вывода Ци и медитации
            // Базовая проводимость при L1.0: 1000 / 360 ≈ 2.78
            conductivity = maxQiCapacity / 360f;
            
            OnQiChanged?.Invoke(currentQi, maxQiCapacity);
        }
        
        /// <summary>
        /// Рассчитать максимальную ёмкость ядра.
        /// </summary>
        private long CalculateMaxCapacity()
        {
            // Базовая ёмкость × качество ядра × уровень
            long baseCapacity = GameConstants.BASE_CORE_CAPACITY;
            
            // Множитель качества
            float qualityMult = GetQualityMultiplier();
            
            // Рост по под-уровням
            float subLevelGrowth = Mathf.Pow(GameConstants.CORE_CAPACITY_GROWTH, 
                (cultivationLevel - 1) * 10 + cultivationSubLevel);
            
            return (long)(baseCapacity * qualityMult * subLevelGrowth);
        }
        
        private float GetQualityMultiplier()
        {
            return coreQuality switch
            {
                CoreQuality.Fragmented => 0.5f,
                CoreQuality.Cracked => 0.7f,
                CoreQuality.Flawed => 0.85f,
                CoreQuality.Normal => 1.0f,
                CoreQuality.Refined => 1.2f,
                CoreQuality.Perfect => 1.5f,
                CoreQuality.Transcendent => 2.0f,
                _ => 1.0f
            };
        }
        
        // === Qi Management ===
        
        /// <summary>
        /// Потратить Ци.
        /// </summary>
        /// <returns>True если хватило Ци</returns>
        public bool SpendQi(int amount)
        {
            if (currentQi >= amount)
            {
                currentQi -= amount;
                OnQiChanged?.Invoke(currentQi, maxQiCapacity);
                
                if (currentQi <= 0)
                {
                    OnQiDepleted?.Invoke();
                }
                
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Добавить Ци.
        /// </summary>
        public void AddQi(long amount)
        {
            currentQi = Math.Min(maxQiCapacity, currentQi + amount);
            OnQiChanged?.Invoke(currentQi, maxQiCapacity);
            
            if (IsFull)
            {
                OnQiFull?.Invoke();
            }
        }
        
        /// <summary>
        /// Установить текущее Ци.
        /// </summary>
        public void SetQi(long amount)
        {
            currentQi = Math.Max(0, Math.Min(maxQiCapacity, amount));
            OnQiChanged?.Invoke(currentQi, maxQiCapacity);
        }
        
        /// <summary>
        /// Полное восстановление Ци.
        /// </summary>
        public void RestoreFull()
        {
            currentQi = maxQiCapacity;
            OnQiChanged?.Invoke(currentQi, maxQiCapacity);
            OnQiFull?.Invoke();
        }
        
        // === Regeneration ===
        
        private void ProcessPassiveRegeneration()
        {
            // Генерация микроядром: 10% от ёмкости в сутки
            // В секундах: ёмкость * 0.1 / (24 * 60 * 60)
            float dailyGen = maxQiCapacity * GameConstants.MICROCORE_GENERATION_RATE;
            float perSecond = dailyGen / 86400f;
            
            // Применяем множители (БЕЗ qiDensity! - она влияет только на урон техник)
            // Регенерация: 10% от ёмкости в сутки, умноженная на regenMultiplier уровня
            float actualRegen = perSecond * regenMultiplier * Time.deltaTime;
            
            if (actualRegen >= 1f)
            {
                AddQi((long)actualRegen);
            }
            else
            {
                // Накапливаем мелкие доли
                dailyAccumulator += actualRegen;
                if (dailyAccumulator >= 1f)
                {
                    AddQi((long)dailyAccumulator);
                    dailyAccumulator -= (long)dailyAccumulator;
                }
            }
        }
        
        // === Meditation ===
        
        /// <summary>
        /// Медитация — ускоренное накопление Ци.
        /// </summary>
        /// <param name="durationTicks">Длительность в тиках</param>
        /// <returns>Накопленное Ци</returns>
        public long Meditate(int durationTicks)
        {
            // Базовая скорость: проводимость × плотность × тики
            float baseGain = conductivity * qiDensity * durationTicks;
            
            // Множитель медитации (зависит от уровня)
            float meditationMult = 1f + cultivationLevel * 0.1f;
            
            long gained = (long)(baseGain * meditationMult);
            AddQi(gained);
            
            return gained;
        }
        
        // === Cultivation ===
        
        /// <summary>
        /// Установить уровень культивации.
        /// </summary>
        public void SetCultivationLevel(int level, int subLevel = 0)
        {
            cultivationLevel = Mathf.Clamp(level, 1, 10);
            cultivationSubLevel = Mathf.Clamp(subLevel, 0, 9);
            RecalculateStats();
            OnCultivationLevelChanged?.Invoke(cultivationLevel);
        }
        
        /// <summary>
        /// Проверить возможность прорыва.
        /// </summary>
        public bool CanBreakthrough(bool isMajorLevel)
        {
            long required = isMajorLevel 
                ? (long)(coreCapacity * GameConstants.BIG_BREAKTHROUGH_MULTIPLIER)
                : (long)(coreCapacity * GameConstants.SMALL_BREAKTHROUGH_MULTIPLIER);
            
            return currentQi >= required;
        }
        
        /// <summary>
        /// Выполнить прорыв.
        /// </summary>
        public bool PerformBreakthrough(bool isMajorLevel)
        {
            if (!CanBreakthrough(isMajorLevel)) return false;
            
            long required = isMajorLevel 
                ? (long)(coreCapacity * GameConstants.BIG_BREAKTHROUGH_MULTIPLIER)
                : (long)(coreCapacity * GameConstants.SMALL_BREAKTHROUGH_MULTIPLIER);
            
            SpendQi((int)required);
            
            if (isMajorLevel)
            {
                cultivationLevel++;
                cultivationSubLevel = 0;
                coreCapacity = maxQiCapacity;
            }
            else
            {
                cultivationSubLevel++;
                if (cultivationSubLevel > 9)
                {
                    cultivationSubLevel = 0;
                    cultivationLevel++;
                }
            }
            
            RecalculateStats();
            OnCultivationLevelChanged?.Invoke(cultivationLevel);
            
            return true;
        }
        
        // === Combat Integration ===
        
        [Header("Combat")]
        [SerializeField] private bool hasShieldTechnique = false;
        
        /// <summary>
        /// Тип защиты Ци.
        /// </summary>
        public QiDefenseType QiDefense => hasShieldTechnique ? QiDefenseType.Shield : QiDefenseType.RawQi;
        
        /// <summary>
        /// Поглотить урон через буфер Ци.
        /// 
        /// Источник: ALGORITHMS.md §2 "Буфер Ци"
        /// 
        /// Различает:
        /// - Техники Ци (90%/3:1/10% или 100%/1:1/0% для щита)
        /// - Физический урон (80%/5:1/20% или 100%/2:1/0% для щита)
        /// </summary>
        /// <param name="damage">Входящий урон</param>
        /// <param name="isQiTechnique">True = техника Ци, False = физический урон</param>
        /// <returns>Результат поглощения</returns>
        public QiBufferResult AbsorbDamage(float damage, bool isQiTechnique = true)
        {
            if (currentQi < GameConstants.MIN_QI_FOR_BUFFER)
            {
                return new QiBufferResult
                {
                    AbsorbedDamage = 0f,
                    PiercingDamage = damage,
                    QiConsumed = 0,
                    QiRemaining = (int)currentQi,
                    WasShieldActive = false,
                    WasQiDepleted = false
                };
            }
            
            QiBufferResult result = isQiTechnique
                ? QiBuffer.ProcessQiTechniqueDamage(damage, (int)currentQi, QiDefense)
                : QiBuffer.ProcessPhysicalDamage(damage, (int)currentQi, QiDefense);
            
            // Тратим Ци
            if (result.QiConsumed > 0)
            {
                SpendQi(result.QiConsumed);
                result.QiRemaining = (int)currentQi;
            }
            
            return result;
        }
        
        /// <summary>
        /// Проверить, может ли Ци поглотить урон.
        /// </summary>
        public bool CanAbsorbDamage(float damage, bool isQiTechnique = true)
        {
            if (currentQi < GameConstants.MIN_QI_FOR_BUFFER)
                return false;
            
            DamageSourceType sourceType = isQiTechnique ? DamageSourceType.QiTechnique : DamageSourceType.Physical;
            return QiBuffer.CanAbsorbDamage(damage, (int)currentQi, QiDefense, sourceType);
        }
        
        /// <summary>
        /// Рассчитать необходимое Ци для поглощения урона.
        /// </summary>
        public int CalculateRequiredQiForDamage(float damage, bool isQiTechnique = true)
        {
            DamageSourceType sourceType = isQiTechnique ? DamageSourceType.QiTechnique : DamageSourceType.Physical;
            return QiBuffer.CalculateRequiredQi(damage, QiDefense, sourceType);
        }
        
        // === Utility ===
        
        /// <summary>
        /// Получить информацию о Ци в строковом формате.
        /// </summary>
        public string GetQiInfo()
        {
            return $"Ци: {currentQi:N0} / {maxQiCapacity:N0} ({QiPercent:P0})";
        }
        
        /// <summary>
        /// Получить информацию о культивации.
        /// </summary>
        public string GetCultivationInfo()
        {
            return $"Уровень: L{cultivationLevel}.{cultivationSubLevel} | Проводимость: {conductivity:F1}";
        }
    }
}
