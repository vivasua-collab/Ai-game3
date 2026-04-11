// ============================================================================
// FormationQiPool.cs — Ёмкость и утечка Ци формации
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 13:20:00 UTC
// ============================================================================
//
// Источник: docs/FORMATION_SYSTEM.md
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Data.ScriptableObjects;

namespace CultivationGame.Formation
{
    /// <summary>
    /// Статические константы утечки Ци.
    /// Источник: FORMATION_SYSTEM.md "Естественная утечка Ци"
    /// </summary>
    public static class FormationDrainConstants
    {
        /// <summary>
        /// Интервал утечки в тиках по уровню формации.
        /// 1 тик = 1 минута игрового времени.
        /// </summary>
        public static readonly int[] INTERVAL_BY_LEVEL = { 60, 60, 40, 40, 20, 20, 10, 10, 5 };

        /// <summary>
        /// Количество Ци за раз по размеру формации.
        /// </summary>
        public static readonly int[] AMOUNT_BY_SIZE = { 1, 3, 10, 30, 100 };

        /// <summary>
        /// Получить интервал утечки для уровня.
        /// </summary>
        public static int GetDrainInterval(int level)
        {
            int index = Mathf.Clamp(level - 1, 0, INTERVAL_BY_LEVEL.Length - 1);
            return INTERVAL_BY_LEVEL[index];
        }

        /// <summary>
        /// Получить количество утечки для размера.
        /// </summary>
        public static int GetDrainAmount(int sizeIndex)
        {
            int index = Mathf.Clamp(sizeIndex, 0, AMOUNT_BY_SIZE.Length - 1);
            return AMOUNT_BY_SIZE[index];
        }
    }

    /// <summary>
    /// Результат изменения Ци в пуле.
    /// </summary>
    public struct QiPoolResult
    {
        public long amountChanged;
        public long currentQi;
        public long capacity;
        public float fillPercent;
        public bool wasFilled;
        public bool wasDepleted;
    }

    /// <summary>
    /// Пул Ци формации.
    /// Управляет ёмкостью, наполнением, потреблением и естественной утечкой.
    /// 
    /// ┌─────────────────────────────────────────────────────────────────────────┐
    /// │                     ПРИНЦИП РАБОТЫ ПУЛА Ци                              │
    /// ├─────────────────────────────────────────────────────────────────────────┤
    /// │                                                                          │
    /// │   НАПОЛНЕНИЕ                          УТЕЧКА                             │
    /// │   ────────────                        ──────                             │
    /// │   Практики вносят Ци                 Каждый N тиков                      │
    /// │   Скорость = проводимость            -M Ци за раз                        │
    /// │   ↓                                   ↓                                   │
    /// │   ┌─────────────────┐               ┌─────────────────┐                  │
    /// │   │   currentQi     │ ───────────→  │   currentQi     │                  │
    /// │   │   += amount     │               │   -= drain      │                  │
    /// │   └─────────────────┘               └─────────────────┘                  │
    /// │         ↓                                   ↓                            │
    /// │   currentQi >= capacity?              currentQi <= 0?                     │
    /// │         ↓                                   ↓                            │
    /// │   OnFilled → Activate                OnDepleted → Deactivate              │
    /// │                                                                          │
    /// └─────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    [Serializable]
    public class FormationQiPool
    {
        #region Configuration

        /// <summary>
        /// Максимальная ёмкость.
        /// </summary>
        public long capacity;

        /// <summary>
        /// Интервал утечки в тиках (1 тик = 1 минута).
        /// </summary>
        public int drainInterval = 60;

        /// <summary>
        /// Количество Ци утекающего за раз.
        /// </summary>
        public int drainAmount = 1;

        /// <summary>
        /// Проводимость пула (ед/сек).
        /// </summary>
        public float conductivity = 10f;

        #endregion

        #region State

        /// <summary>
        /// Текущее количество Ци.
        /// </summary>
        public long currentQi;

        /// <summary>
        /// Последний тик утечки.
        /// </summary>
        public int lastDrainTick;

        /// <summary>
        /// Накопленная утечка (для точности).
        /// </summary>
        private float accumulatedDrain;

        #endregion

        #region Events

        /// <summary>
        /// Событие изменения Ци.
        /// </summary>
        public event Action<long, long> OnQiChanged; // (current, capacity)

        /// <summary>
        /// Событие заполнения пула.
        /// </summary>
        public event Action OnFilled;

        /// <summary>
        /// Событие истощения пула.
        /// </summary>
        public event Action OnDepleted;

        #endregion

        #region Properties

        /// <summary>
        /// Процент заполнения.
        /// </summary>
        public float FillPercent => capacity > 0 ? (float)currentQi / capacity : 0f;

        /// <summary>
        /// Готов к активации (100% заполнение).
        /// </summary>
        public bool IsReadyForActivation => currentQi >= capacity;

        /// <summary>
        /// Пул пуст.
        /// </summary>
        public bool IsEmpty => currentQi <= 0;

        /// <summary>
        /// Пул полон.
        /// </summary>
        public bool IsFull => currentQi >= capacity;

        #endregion

        #region Initialization

        /// <summary>
        /// Конструктор по умолчанию.
        /// </summary>
        public FormationQiPool()
        {
            capacity = 1000;
            currentQi = 0;
            drainInterval = 60;
            drainAmount = 1;
            conductivity = 10f;
            lastDrainTick = 0;
        }

        /// <summary>
        /// Конструктор с параметрами.
        /// </summary>
        public FormationQiPool(long capacity, int drainInterval, int drainAmount, float conductivity = 10f)
        {
            this.capacity = capacity;
            this.currentQi = 0;
            this.drainInterval = drainInterval;
            this.drainAmount = drainAmount;
            this.conductivity = conductivity;
            this.lastDrainTick = 0;
        }

        /// <summary>
        /// Настроить пул из данных формации.
        /// </summary>
        public void Configure(FormationData data, FormationCoreData core = null)
        {
            // Расчёт ёмкости
            bool isHeavy = data.size == FormationSize.Heavy && data.level >= 6;
            capacity = data.CalculateCapacity(isHeavy);

            // Параметры утечки
            drainInterval = data.DrainIntervalTicks;
            drainAmount = data.DrainAmount;

            // Проводимость (из ядра если есть)
            if (core != null)
            {
                conductivity = core.baseConductivity;
            }
            else
            {
                // Базовая проводимость без ядра
                conductivity = 5f + data.level * 2;
            }

            // Сброс состояния
            currentQi = 0;
            lastDrainTick = 0;
        }

        #endregion

        #region Qi Management

        /// <summary>
        /// Добавить Ци при наполнении.
        /// </summary>
        /// <param name="amount">Количество Ци</param>
        /// <param name="rate">Скорость передачи (проводимость практика)</param>
        /// <returns>Результат операции</returns>
        public QiPoolResult AddQi(long amount, float rate = 0f)
        {
            long previousQi = currentQi;

            // Ограничение по скорости проводимости
            if (rate > 0 && conductivity > 0)
            {
                // Максимум что может принять пул за эту операцию
                long maxAccept = (long)(conductivity * 60f); // За минуту
                amount = Math.Min(amount, maxAccept);
            }

            // Добавляем Ци
            currentQi = Math.Min(capacity, currentQi + amount);

            // Результат
            QiPoolResult result = new QiPoolResult
            {
                amountChanged = currentQi - previousQi,
                currentQi = currentQi,
                capacity = capacity,
                fillPercent = FillPercent,
                wasFilled = previousQi < capacity && currentQi >= capacity,
                wasDepleted = false
            };

            // События
            OnQiChanged?.Invoke(currentQi, capacity);

            if (result.wasFilled)
            {
                OnFilled?.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Использовать Ци (для эффектов формации).
        /// </summary>
        /// <param name="amount">Количество Ци</param>
        /// <returns>Результат операции</returns>
        public QiPoolResult ConsumeQi(long amount)
        {
            long previousQi = currentQi;

            currentQi = Math.Max(0, currentQi - amount);

            QiPoolResult result = new QiPoolResult
            {
                amountChanged = previousQi - currentQi,
                currentQi = currentQi,
                capacity = capacity,
                fillPercent = FillPercent,
                wasFilled = false,
                wasDepleted = previousQi > 0 && currentQi <= 0
            };

            OnQiChanged?.Invoke(currentQi, capacity);

            if (result.wasDepleted)
            {
                OnDepleted?.Invoke();
            }

            return result;
        }

        /// <summary>
        /// Принять Ци от практика.
        /// FIX FRM-M03: amount → long для Qi > 2.1B на L5+
        /// </summary>
        /// <param name="amount">Предлагаемое количество</param>
        /// <param name="transferRate">Скорость передачи (проводимость × плотность)</param>
        /// <returns>Принятое количество</returns>
        public long AcceptQi(long amount, float transferRate)
        {
            if (amount <= 0 || IsFull) return 0;

            // Ограничение по проводимости формации
            long maxAccept = (long)(conductivity * 60f); // За минуту
            long accepted = Math.Min(amount, maxAccept);

            // Не больше чем осталось до полного
            long space = capacity - currentQi;
            accepted = Math.Min(accepted, space);

            if (accepted > 0)
            {
                currentQi += accepted;

                OnQiChanged?.Invoke(currentQi, capacity);

                if (IsFull)
                {
                    OnFilled?.Invoke();
                }
            }

            return accepted;
        }

        #endregion

        #region Drain Processing

        /// <summary>
        /// Обработать утечку.
        /// Вызывается каждый игровой тик.
        /// </summary>
        /// <param name="currentTick">Текущий игровой тик</param>
        /// <returns>Количество утекшего Ци</returns>
        public int ProcessDrain(int currentTick)
        {
            if (IsEmpty || drainInterval <= 0) return 0;

            // Проверяем интервал
            int ticksSinceLastDrain = currentTick - lastDrainTick;

            if (ticksSinceLastDrain < drainInterval) return 0;

            // Рассчитываем количество утечек
            int drainCount = ticksSinceLastDrain / drainInterval;

            // Утечка (целое число Ци)
            int totalDrain = drainCount * drainAmount;

            // Применяем утечку
            long previousQi = currentQi;
            currentQi = Math.Max(0, currentQi - totalDrain);

            // Обновляем последний тик
            lastDrainTick = currentTick;

            // События
            long actualDrain = previousQi - currentQi;

            if (actualDrain > 0)
            {
                OnQiChanged?.Invoke(currentQi, capacity);
            }

            if (IsEmpty)
            {
                OnDepleted?.Invoke();
            }

            return actualDrain;
        }

        /// <summary>
        /// Рассчитать время до истощения.
        /// </summary>
        /// <param name="ticksPerMinute">Тиков в минуту (обычно 1)</param>
        /// <returns>Время в минутах</returns>
        public long GetTimeUntilDepleted(int ticksPerMinute = 1)
        {
            if (IsEmpty || drainInterval <= 0 || drainAmount <= 0)
                return long.MaxValue;

            // Количество утечек до полного истощения
            long drainCount = currentQi / drainAmount;

            // Время в тиках
            long ticksUntilDepleted = drainCount * drainInterval;

            // Конвертируем в минуты
            return ticksUntilDepleted / ticksPerMinute;
        }

        /// <summary>
        /// Получить время до истощения в читаемом формате.
        /// </summary>
        public string GetTimeUntilDepletedFormatted()
        {
            long minutes = GetTimeUntilDepleted();

            if (minutes == long.MaxValue) return "∞";

            long days = minutes / 1440;
            long hours = (minutes % 1440) / 60;
            long mins = minutes % 60;

            if (days > 0)
                return $"{days}д {hours}ч {mins}м";
            else if (hours > 0)
                return $"{hours}ч {mins}м";
            else
                return $"{mins}м";
        }

        #endregion

        #region Utility

        /// <summary>
        /// Сбросить пул.
        /// </summary>
        public void Reset()
        {
            currentQi = 0;
            lastDrainTick = 0;
            accumulatedDrain = 0;
            OnQiChanged?.Invoke(currentQi, capacity);
        }

        /// <summary>
        /// Заполнить полностью.
        /// </summary>
        public void Fill()
        {
            if (currentQi < capacity)
            {
                currentQi = capacity;
                OnQiChanged?.Invoke(currentQi, capacity);
                OnFilled?.Invoke();
            }
        }

        /// <summary>
        /// Получить информацию о пуле.
        /// </summary>
        public string GetInfo()
        {
            return $"Ци: {currentQi:N0} / {capacity:N0} ({FillPercent:P1})\n" +
                   $"Утечка: {drainAmount} каждые {drainInterval} тиков\n" +
                   $"До истощения: {GetTimeUntilDepletedFormatted()}";
        }

        /// <summary>
        /// Данные для сохранения.
        /// </summary>
        public FormationQiPoolSaveData GetSaveData()
        {
            return new FormationQiPoolSaveData
            {
                capacity = capacity,
                currentQi = currentQi,
                drainInterval = drainInterval,
                drainAmount = drainAmount,
                conductivity = conductivity,
                lastDrainTick = lastDrainTick
            };
        }

        /// <summary>
        /// Загрузить из сохранения.
        /// </summary>
        public void LoadSaveData(FormationQiPoolSaveData data)
        {
            capacity = data.capacity;
            currentQi = data.currentQi;
            drainInterval = data.drainInterval;
            drainAmount = data.drainAmount;
            conductivity = data.conductivity;
            lastDrainTick = data.lastDrainTick;
        }

        #endregion
    }

    /// <summary>
    /// Данные пула Ци для сохранения.
    /// </summary>
    [Serializable]
    public struct FormationQiPoolSaveData
    {
        public long capacity;
        public long currentQi;
        public int drainInterval;
        public int drainAmount;
        public float conductivity;
        public int lastDrainTick;
    }
}
