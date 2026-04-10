// ============================================================================
// QiController.cs — Контроллер Ци
// Cultivation World Simulator
// Версия: 1.4 — Fix-03: CanBreakthrough Модель В, Meditate + время, overflow check
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-10 16:20:00 UTC — Fix-03: QI-MDL-B01/02, QI-H01/02, QI-M01
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
        
        [Header("Perk Bonuses")]
        [SerializeField] [Range(0f, 2f)] private float conductivityBonus = 0f; // +0% to +200%
        
        [Header("Regeneration")]
        [SerializeField] private bool enablePassiveRegen = true;
        [SerializeField] private float regenMultiplier = 1f;
        
        // === Runtime ===
        
        private long maxQiCapacity;
        private float qiDensity;
        private float baseConductivity; // Базовая проводимость (без бонусов)
        private double dailyAccumulator = 0.0; // FIX: Используем double для точности
        
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
        public float BaseConductivity => baseConductivity;
        public float ConductivityBonus => conductivityBonus;
        public int CultivationLevel => cultivationLevel;
        public float QiPercent => maxQiCapacity > 0 ? (float)currentQi / maxQiCapacity : 0f;
        public float QiDensity => qiDensity;
        public bool IsFull => currentQi >= maxQiCapacity;
        public bool IsEmpty => currentQi <= 0;
        
        /// <summary>
        /// Эффективное Ци = текущее Ци × плотность.
        /// Используется для расчёта мощности техник и UI.
        /// Модель В: effectiveQi = coreCapacity × qiDensity
        /// </summary>
        public long EffectiveQi => (long)(currentQi * qiDensity);
        
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
            
            // Базовая проводимость = coreCapacity / 360 секунд (по документации QI_SYSTEM.md)
            // Это определяет скорость вывода Ци и медитации
            // Базовая проводимость при L1.0: 1000 / 360 ≈ 2.78
            baseConductivity = maxQiCapacity / 360f;
            
            // Итоговая проводимость с бонусом от перков
            // Формула: finalConductivity = baseConductivity × (1 + bonus)
            // Защита от умножения на 0: (1 + 0) = 1, результат = baseConductivity
            conductivity = baseConductivity * (1f + conductivityBonus);
            
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
            
            // FIX: Защита от переполнения long (max ~9.22e18)
            // Используем double для промежуточных вычислений
            double rawCapacity = (double)baseCapacity * qualityMult * subLevelGrowth;
            
            // Ограничиваем максимальным значением long
            const long MAX_SAFE_CAPACITY = long.MaxValue / 2; // ~4.6e18 — безопасный предел
            
            if (rawCapacity > MAX_SAFE_CAPACITY)
            {
                Debug.LogWarning($"[QiController] Capacity overflow detected! Clamping to {MAX_SAFE_CAPACITY}");
                return MAX_SAFE_CAPACITY;
            }
            
            return (long)rawCapacity;
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
        /// FIX: int→long для поддержки Qi > 2.1B на L5+
        /// FIX: Проверка amount > 0 предотвращает эксплойт (negative = добавление Qi)
        /// </summary>
        /// <returns>True если хватило Ци</returns>
        public bool SpendQi(long amount)
        {
            if (amount <= 0) return false; // FIX: QI-H03 — отрицательное/нулевое значение
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
            double dailyGen = maxQiCapacity * GameConstants.MICROCORE_GENERATION_RATE;
            double perSecond = dailyGen / 86400.0;
            
            // Применяем множители (БЕЗ qiDensity! - она влияет только на урон техник)
            // Регенерация: 10% от ёмкости в сутки, умноженная на regenMultiplier уровня
            double actualRegen = perSecond * regenMultiplier * Time.deltaTime;
            
            if (actualRegen >= 1.0)
            {
                AddQi((long)actualRegen);
            }
            else
            {
                // Накапливаем мелкие доли с повышенной точностью
                dailyAccumulator += actualRegen;
                if (dailyAccumulator >= 1.0)
                {
                    AddQi((long)dailyAccumulator);
                    dailyAccumulator -= (long)dailyAccumulator;
                }
            }
        }
        
        // === Meditation ===
        
        /// <summary>
        /// Медитация — ускоренное накопление Ци.
        /// FIX QI-H01: Добавлено продвижение игрового времени.
        /// FIX QI-M01: Проверка overflow для durationTicks * conductivity.
        /// </summary>
        /// <param name="durationTicks">Длительность в тиках (1 тик = 1 минута)</param>
        /// <returns>Накопленное Ци</returns>
        public long Meditate(int durationTicks)
        {
            if (durationTicks <= 0) return 0;
            
            // FIX QI-M01: Overflow check
            double baseGain = (double)conductivity * qiDensity * durationTicks;
            
            // Множитель медитации (зависит от уровня)
            float meditationMult = 1f + cultivationLevel * 0.1f;
            
            // FIX QI-M01: Clamp to avoid overflow
            double totalGain = baseGain * meditationMult;
            const long MAX_SAFE_GAIN = long.MaxValue / 2;
            long gained = totalGain > MAX_SAFE_GAIN ? MAX_SAFE_GAIN : (long)totalGain;
            
            AddQi(gained);
            
            // FIX QI-H01: Продвижение игрового времени
            // 1 тик = 1 минута, преобразуем в часы
            float hoursAdvanced = durationTicks / 60f;
            var timeController = FindFirstObjectByType<CultivationGame.World.TimeController>();
            if (timeController != null && hoursAdvanced > 0f)
            {
                timeController.AdvanceHours(Mathf.RoundToInt(hoursAdvanced));
            }
            
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
        /// FIX QI-MDL-B01: Модель В — требование = capacity(next) × density(next)
        /// 
        /// Большой прорыв (majorLevel): capacity(nextLevel) × density(nextLevel)
        /// Малый прорыв (!majorLevel): capacity(nextSubLevel) × currentDensity
        /// </summary>
        public bool CanBreakthrough(bool isMajorLevel)
        {
            long required = CalculateBreakthroughRequirement(isMajorLevel);
            return currentQi >= required;
        }
        
        /// <summary>
        /// Рассчитать требование прорыва (Модель В).
        /// FIX QI-MDL-B01
        /// </summary>
        public long CalculateBreakthroughRequirement(bool isMajorLevel)
        {
            if (isMajorLevel)
            {
                int nextLevel = cultivationLevel + 1;
                float nextDensity = Mathf.Pow(2, nextLevel - 1);
                long nextCapacity = EstimateCapacityAtLevel(nextLevel);
                return (long)((double)nextCapacity * nextDensity);
            }
            else
            {
                // Малый прорыв: текущая плотность, следующая подёмкость
                float currentDensity = qiDensity;
                long nextSubCapacity = EstimateCapacityAtSubLevel(cultivationLevel, cultivationSubLevel + 1);
                return (long)((double)nextSubCapacity * currentDensity);
            }
        }
        
        /// <summary>
        /// Выполнить прорыв.
        /// ПОСЛЕ ПРОРЫВА Ци = 0 (ядро пустое).
        /// FIX QI-MDL-B02: RecalculateStats() пересчитает coreCapacity правильно.
        /// Порядок: level++ → RecalculateStats() → coreCapacity обновляется через maxQiCapacity
        /// </summary>
        public bool PerformBreakthrough(bool isMajorLevel)
        {
            if (!CanBreakthrough(isMajorLevel)) return false;
            
            // ПОСЛЕ ПРОРЫВА ЦИ = 0
            currentQi = 0;
            
            if (isMajorLevel)
            {
                cultivationLevel++;
                cultivationSubLevel = 0;
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
            
            // FIX QI-MDL-B02: RecalculateStats пересчитает maxQiCapacity,
            // затем обновляем coreCapacity
            RecalculateStats();
            coreCapacity = maxQiCapacity;
            
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
                    QiRemaining = currentQi, // FIX: long вместо int
                    WasShieldActive = false,
                    WasQiDepleted = false
                };
            }
            
            QiBufferResult result = isQiTechnique
                ? QiBuffer.ProcessQiTechniqueDamage(damage, currentQi, QiDefense) // FIX: long
                : QiBuffer.ProcessPhysicalDamage(damage, currentQi, QiDefense);   // FIX: long
            
            // Тратим Ци
            if (result.QiConsumed > 0)
            {
                SpendQi(result.QiConsumed);
                result.QiRemaining = currentQi; // FIX: long
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
            return QiBuffer.CanAbsorbDamage(damage, currentQi, QiDefense, sourceType); // FIX: long
        }
        
        /// <summary>
        /// Рассчитать необходимое Ци для поглощения урона.
        /// </summary>
        public long CalculateRequiredQiForDamage(float damage, bool isQiTechnique = true)
        {
            DamageSourceType sourceType = isQiTechnique ? DamageSourceType.QiTechnique : DamageSourceType.Physical;
            return QiBuffer.CalculateRequiredQi(damage, QiDefense, sourceType); // FIX: long return
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
            string bonusStr = conductivityBonus > 0 ? $" (+{conductivityBonus:P0})" : "";
            return $"Уровень: L{cultivationLevel}.{cultivationSubLevel} | Проводимость: {conductivity:F1}{bonusStr}";
        }
        
        // === Perk Integration ===
        
        /// <summary>
        /// Установить бонус проводимости от перков.
        /// 
        /// Формула: finalConductivity = baseConductivity × (1 + bonus)
        /// 
        /// Примеры:
        /// - bonus = 0.00 → проводимость без изменений (защита от умножения на 0)
        /// - bonus = 0.15 → +15% проводимости (перк "Закалка меридиан")
        /// - bonus = 0.30 → +30% проводимости (врождённый перк "Золотое качество тела")
        /// </summary>
        /// <param name="bonus">Бонус в долях (0.15 = 15%, 0.30 = 30%)</param>
        public void SetConductivityBonus(float bonus)
        {
            // Ограничиваем бонус диапазоном [0, 2] (0% - 200%)
            conductivityBonus = Mathf.Clamp(bonus, 0f, 2f);
            
            // Пересчитываем итоговую проводимость
            // Защита от умножения на 0: (1 + 0) = 1
            conductivity = baseConductivity * (1f + conductivityBonus);
            
            Debug.Log($"[QiController] Conductivity bonus set to {conductivityBonus:P0}. " +
                      $"Final conductivity: {conductivity:F1} (base: {baseConductivity:F1})");
        }
        
        /// <summary>
        /// Добавить бонус проводимости (суммируется с существующим).
        /// </summary>
        /// <param name="additionalBonus">Дополнительный бонус в долях</param>
        public void AddConductivityBonus(float additionalBonus)
        {
            SetConductivityBonus(conductivityBonus + additionalBonus);
        }

        // === Formation Integration ===

        /// <summary>
        /// Передать Ци в формацию.
        /// FIX: long для Qi > 2.1B
        /// Источник: FORMATION_SYSTEM.md
        /// </summary>
        public long TransferToFormation(Formation.FormationCore formation, long maxAmount)
        {
            if (formation == null || currentQi <= 0) return 0;

            long amount = Math.Min(currentQi, maxAmount);

            if (amount > 0)
            {
                // Скорость передачи = проводимость × плотность
                float transferRate = conductivity * qiDensity;

                // Формация принимает с учётом своей проводимости
                long accepted = formation.ContributeQi(gameObject, amount, transferRate);

                if (accepted > 0)
                {
                    SpendQi(accepted);
                }

                return accepted;
            }

            return 0;
        }

        /// <summary>
        /// Получить скорость передачи Ци (проводимость × плотность).
        /// </summary>
        public float GetTransferRate()
        {
            return conductivity * qiDensity;
        }
        
        // === Model B Helpers ===
        
        /// <summary>
        /// Оценить ёмкость ядра на указанном уровне.
        /// Используется для расчёта требований прорыва (Модель В).
        /// </summary>
        public long EstimateCapacityAtLevel(int level)
        {
            long baseCapacity = GameConstants.BASE_CORE_CAPACITY;
            float qualityMult = GetQualityMultiplier();
            float subLevelGrowth = Mathf.Pow(GameConstants.CORE_CAPACITY_GROWTH, (level - 1) * 10);
            double rawCapacity = (double)baseCapacity * qualityMult * subLevelGrowth;
            const long MAX_SAFE = long.MaxValue / 2;
            return rawCapacity > MAX_SAFE ? MAX_SAFE : (long)rawCapacity;
        }
        
        /// <summary>
        /// Оценить ёмкость ядра на указанном подуровне.
        /// </summary>
        public long EstimateCapacityAtSubLevel(int level, int subLevel)
        {
            long baseCapacity = GameConstants.BASE_CORE_CAPACITY;
            float qualityMult = GetQualityMultiplier();
            float subLevelGrowth = Mathf.Pow(GameConstants.CORE_CAPACITY_GROWTH, (level - 1) * 10 + subLevel);
            double rawCapacity = (double)baseCapacity * qualityMult * subLevelGrowth;
            const long MAX_SAFE = long.MaxValue / 2;
            return rawCapacity > MAX_SAFE ? MAX_SAFE : (long)rawCapacity;
        }
    }
}
