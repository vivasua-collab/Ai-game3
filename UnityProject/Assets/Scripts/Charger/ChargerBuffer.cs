// ============================================================================
// ChargerBuffer.cs — Буфер Ци зарядника
// Cultivation World Simulator
// Версия: 1.1 — Fix-01: long для practitionerCurrentQi, [Header] убраны
// Создано: 2026-04-03 08:25:00 UTC
// Редактировано: 2026-04-10 — Fix-01: Qi int→long migration
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Charger
{
    /// <summary>
    /// Результат использования буфера зарядника.
    /// FIX: QiFromCore → long (из ядра практика, может быть > 2.1B)
    /// </summary>
    public struct ChargerBufferResult
    {
        public long QiFromCore;          // Ци из ядра практика (FIX: long)
        public int QiFromBuffer;         // Ци из буфера зарядника (буфер малый)
        public int QiRemaining;          // Остаток в буфере
        public int QiLost;               // Потери (10%)
        public bool WasBufferUsed;       // Использован ли буфер
        public bool WasBufferDepleted;   // Опустошён ли буфер
    }
    
    /// <summary>
    /// Буфер Ци зарядника.
    /// Управляет накоплением и отдачей Ци.
    /// Источник: CHARGER_SYSTEM.md §2.2 "Буфер Ци"
    /// 
    /// FIX CHR-C01: Убраны [Header] (не работают в [Serializable] non-MonoBehaviour),
    /// оставлены [SerializeField] (нужны для Unity serialization).
    /// </summary>
    [Serializable]
    public class ChargerBuffer
    {
        // Capacity — [Header] убран, [SerializeField] оставлен для сериализации
        [SerializeField] private int capacity = 500;
        [SerializeField] private int currentQi;
        
        // Rates
        [SerializeField] private float conductivity = 10f;      // Проводимость (5-100 Ци/сек)
        [SerializeField] private float inputRate = 10f;         // Скорость входящего потока
        [SerializeField] private float outputRate = 10f;        // Скорость исходящего потока
        
        // Efficiency
        [SerializeField] private float efficiencyLoss = 0.1f;   // Потери 10%
        
        // === Runtime ===
        private float accumulationAccumulator = 0f;
        
        // === Properties ===
        
        public int Capacity => capacity;
        public int CurrentQi => currentQi;
        public float Conductivity => conductivity;
        public float InputRate => inputRate;
        public float OutputRate => outputRate;
        public float EfficiencyLoss => efficiencyLoss;
        public bool IsFull => currentQi >= capacity;
        public bool IsEmpty => currentQi <= 0;
        public float QiPercent => capacity > 0 ? (float)currentQi / capacity : 0f;
        public float AvailablePercent => capacity > 0 ? (float)currentQi / capacity : 0f;
        
        // === Events ===
        
        public event Action<int, int> OnBufferChanged;  // (current, capacity)
        public event Action OnBufferFull;
        public event Action OnBufferDepleted;
        
        // === Constructor ===
        
        public ChargerBuffer() { }
        
        public ChargerBuffer(int capacity, float conductivity)
        {
            this.capacity = capacity;
            this.conductivity = conductivity;
            this.inputRate = conductivity;
            this.outputRate = conductivity;
            this.currentQi = 0;
        }
        
        // === Initialization ===
        
        /// <summary>
        /// Настроить буфер.
        /// </summary>
        public void Configure(int bufferCapacity, float bufferConductivity, float lossPercent = 0.1f)
        {
            capacity = bufferCapacity;
            conductivity = bufferConductivity;
            inputRate = bufferConductivity;
            outputRate = bufferConductivity;
            efficiencyLoss = lossPercent;
            currentQi = 0;
        }
        
        // === Qi Management ===
        
        /// <summary>
        /// Добавить Ци в буфер (от камней).
        /// </summary>
        /// <param name="amount">Количество Ци</param>
        /// <returns>Фактически добавленное количество</returns>
        public int AddQi(int amount)
        {
            if (amount <= 0) return 0;
            
            int spaceAvailable = capacity - currentQi;
            int added = Math.Min(amount, spaceAvailable);
            
            currentQi += added;
            OnBufferChanged?.Invoke(currentQi, capacity);
            
            if (IsFull)
            {
                OnBufferFull?.Invoke();
            }
            
            return added;
        }
        
        /// <summary>
        /// Извлечь Ци из буфера.
        /// </summary>
        /// <param name="amount">Запрашиваемое количество</param>
        /// <returns>Фактически извлечённое (с учётом потерь)</returns>
        public int ExtractQi(int amount)
        {
            if (amount <= 0 || currentQi <= 0) return 0;
            
            int extracted = Math.Min(amount, currentQi);
            currentQi -= extracted;
            
            OnBufferChanged?.Invoke(currentQi, capacity);
            
            if (IsEmpty)
            {
                OnBufferDepleted?.Invoke();
            }
            
            return extracted;
        }
        
        /// <summary>
        /// Извлечь Ци с учётом потерь (10%).
        /// </summary>
        /// <param name="amount">Запрашиваемое количество (чистое)</param>
        /// <returns>Извлечённое Ци с учётом потерь</returns>
        public int ExtractQiWithLoss(int amount)
        {
            // Нужно извлечь больше, чтобы получить требуемое
            int required = Mathf.CeilToInt(amount / (1f - efficiencyLoss));
            return ExtractQi(required);
        }
        
        // === Combat Integration ===
        
        /// <summary>
        /// Попробовать использовать Ци для техники.
        /// Порядок: сначала ядро, потом буфер.
        /// 
        /// Источник: CHARGER_SYSTEM.md §4.2 "Механика использования"
        /// FIX: practitionerCurrentQi → long для Qi > 2.1B на L5+
        /// </summary>
        /// <param name="qiCost">Стоимость техники</param>
        /// <param name="practitionerCurrentQi">Текущее Ци практика</param>
        /// <returns>Результат использования</returns>
        public ChargerBufferResult UseQiForTechnique(long qiCost, long practitionerCurrentQi)
        {
            ChargerBufferResult result = new ChargerBufferResult
            {
                QiFromCore = 0,
                QiFromBuffer = 0,
                QiRemaining = currentQi,
                QiLost = 0,
                WasBufferUsed = false,
                WasBufferDepleted = false
            };
            
            // 1. Сначала используем Ци из ядра практика
            if (practitionerCurrentQi >= qiCost)
            {
                // Достаточно Ци в ядре — буфер не нужен
                result.QiFromCore = qiCost;
                return result;
            }
            
            // 2. Используем всё из ядра
            result.QiFromCore = practitionerCurrentQi;
            long remaining = qiCost - practitionerCurrentQi;
            
            // 3. Добираем из буфера (с потерями)
            if (currentQi > 0 && remaining > 0)
            {
                // С учётом 10% потерь нужно больше Ци
                int requiredFromBuffer = Mathf.CeilToInt(remaining / (1f - efficiencyLoss));
                int availableFromBuffer = Math.Min(requiredFromBuffer, currentQi);
                
                // Извлекаем
                currentQi -= availableFromBuffer;
                result.QiFromBuffer = availableFromBuffer;
                result.QiRemaining = currentQi;
                result.QiLost = Mathf.FloorToInt(availableFromBuffer * efficiencyLoss);
                result.WasBufferUsed = true;
                result.WasBufferDepleted = currentQi <= 0;
                
                OnBufferChanged?.Invoke(currentQi, capacity);
                
                if (IsEmpty)
                {
                    OnBufferDepleted?.Invoke();
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Проверить, можно ли использовать технику.
        /// FIX: practitionerCurrentQi → long
        /// </summary>
        public bool CanUseTechnique(long qiCost, long practitionerCurrentQi)
        {
            // С учётом потерь в буфере
            long effectiveAvailable = practitionerCurrentQi + (long)(currentQi * (1f - efficiencyLoss));
            
            return effectiveAvailable >= qiCost;
        }
        
        /// <summary>
        /// Рассчитать доступное Ци с учётом потерь.
        /// FIX: practitionerCurrentQi → long, return → long
        /// </summary>
        public long GetEffectiveQiAvailable(long practitionerCurrentQi)
        {
            return practitionerCurrentQi + (long)(currentQi * (1f - efficiencyLoss));
        }
        
        // === Accumulation (per-frame) ===
        
        /// <summary>
        /// Накопить Ци от камней за кадр.
        /// </summary>
        /// <param name="totalStoneRate">Суммарная скорость камней</param>
        /// <param name="deltaTime">Время кадра</param>
        /// <returns>Фактически накопленное Ци</returns>
        public int AccumulateFromStones(float totalStoneRate, float deltaTime)
        {
            if (IsFull) return 0;
            
            // Ограничено проводимостью зарядника
            float effectiveRate = Math.Min(totalStoneRate, conductivity);
            float qiThisFrame = effectiveRate * deltaTime;
            
            // Накапливаем мелкие доли
            accumulationAccumulator += qiThisFrame;
            
            if (accumulationAccumulator >= 1f)
            {
                int toAdd = Mathf.FloorToInt(accumulationAccumulator);
                accumulationAccumulator -= toAdd;
                return AddQi(toAdd);
            }
            
            return 0;
        }
        
        /// <summary>
        /// Передать Ци практику (медитация).
        /// </summary>
        /// <param name="practitionerConductivity">Проводимость практика</param>
        /// <param name="deltaTime">Время кадра</param>
        /// <returns>Переданное Ци</returns>
        public int TransferToPractitioner(float practitionerConductivity, float deltaTime)
        {
            if (IsEmpty) return 0;
            
            // Скорость ограничена: выход буфера, проводимость практика
            float effectiveRate = Math.Min(outputRate, practitionerConductivity);
            float qiThisFrame = effectiveRate * deltaTime;
            
            // Потери 10%
            qiThisFrame *= (1f - efficiencyLoss);
            
            if (qiThisFrame >= 1f)
            {
                return ExtractQi(Mathf.FloorToInt(qiThisFrame));
            }
            
            return 0;
        }
        
        // === Utility ===
        
        /// <summary>
        /// Полная разрядка буфера.
        /// </summary>
        public void Discharge()
        {
            currentQi = 0;
            OnBufferChanged?.Invoke(currentQi, capacity);
            OnBufferDepleted?.Invoke();
        }
        
        /// <summary>
        /// Полная зарядка буфера (для тестов).
        /// </summary>
        public void ChargeFull()
        {
            currentQi = capacity;
            OnBufferChanged?.Invoke(currentQi, capacity);
            OnBufferFull?.Invoke();
        }
        
        /// <summary>
        /// Получить информацию о буфере.
        /// </summary>
        public string GetBufferInfo()
        {
            return $"Буфер: {currentQi:N0}/{capacity:N0} ({QiPercent:P0}) | Проводимость: {conductivity:F1}/сек";
        }
        
        /// <summary>
        /// Получить состояние буфера.
        /// </summary>
        public string GetBufferState()
        {
            if (IsEmpty) return "Пуст";
            if (QiPercent < 0.3f) return "Низкий";
            if (QiPercent < 0.7f) return "Средний";
            if (IsFull) return "Полный";
            return "Высокий";
        }
    }
}
