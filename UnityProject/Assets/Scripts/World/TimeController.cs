// ============================================================================
// TimeController.cs — Система игрового времени (ТИКОВАЯ СИСТЕМА)
// Cultivation World Simulator
// Версия: 3.0 — Dual Tick Model: OnTick (константа) + OnWorldTick (масштабируется)
// ============================================================================
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-05-04 07:10:00 UTC — Dual Tick Model v3.0
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 3.0:
// - DUAL TICK MODEL: два типа тиков
//   • OnTick — КОНСТАНТА (tickInterval реальных секунд), для боя/накачки
//   • OnWorldTick — МАСШТАБИРУЕТСЯ скоростью, для мира (формации, Ци, NPC)
// - При Normal speed: OnWorldTick = OnTick (1 тик/сек)
// - При Fast speed: OnWorldTick ×5 (5 тиков/сек)
// - При VeryFast speed: OnWorldTick ×15 (15 тиков/сек)
// - OnTick срабатывает в ОБЕИХ режимах (детерминированный и нет)
// - Добавлено публичное свойство TickInterval
// - Добавлено событие OnTickDelta (float) — для систем с покадровым обновлением
// - Добавлен Instance (синглтон) для доступа из боевой системы
// - Добавлен TickDeltaTime — реальное время одного тика в секундах
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.World
{
    /// <summary>
    /// Контроллер игрового времени.
    /// Управляет течением времени, датами, сезонами.
    ///
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  DUAL TICK MODEL — ТИКОВАЯ СИСТЕМА v3.0                                     ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║                                                                            ║
    /// ║  OnTick (БОЕВОЙ ТИК):                                                      ║
    /// ║  • Интервал: tickInterval реальных секунд (КОНСТАНТА)                      ║
    /// ║  • НЕ масштабируется скоростью времени                                     ║
    /// ║  • Используется: бой, накачка, кулдауны                                   ║
    /// ║                                                                            ║
    /// ║  OnWorldTick (МИРОВОЙ ТИК):                                                ║
    /// ║  • Интервал: tickInterval / speedMultiplier реальных секунд               ║
    /// ║  • МАСШТАБИРУЕТСЯ скоростью времени (регулятор игрока)                     ║
    /// ║  • Normal (1×): 1 тик/сек | Fast (5×): 5 тик/сек | VeryFast (15×): 15     ║
    /// ║  • Используется: формации, Ци-пулы, NPC, баффы                            ║
    /// ║                                                                            ║
    /// ║  Правило tick/10: minChargeTime = tickInterval / 10                        ║
    /// ║                                                                            ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public class TimeController : MonoBehaviour
    {
        #region Singleton

        public static TimeController Instance { get; private set; }

        #endregion

        [Header("Тиковая система — ОСНОВНОЙ ТАЙМЕР")]
        [Tooltip("Интервал тика в РЕАЛЬНЫХ секундах. 1 тик = константа для мира.")]
        [SerializeField] private float tickInterval = 1f;

        [Header("Time Settings")]
        [SerializeField] private TimeSpeed currentTimeSpeed = TimeSpeed.Normal;
        [SerializeField] private bool autoAdvance = true;
        [SerializeField] private bool useDeterministicTime = true; // Использовать FixedUpdate для детерминизма
        
        [Header("Time Ratios")]
        [SerializeField] private float normalSpeedRatio = 60f;      // 1 сек = 1 минута
        [SerializeField] private float fastSpeedRatio = 300f;       // 1 сек = 5 минут
        [SerializeField] private float veryFastSpeedRatio = 900f;   // 1 сек = 15 минут
        
        [Header("Calendar")]
        [SerializeField] private int daysPerMonth = 30;
        [SerializeField] private int monthsPerYear = 12;
        [SerializeField] private int hoursPerDay = 24;
        [SerializeField] private int minutesPerHour = 60;
        
        // === Runtime State ===
        
        private int currentYear = 1;
        private int currentMonth = 1;
        private int currentDay = 1;
        private int currentHour = 6;    // Начинаем с 6:00
        private int currentMinute = 0;
        
        private float timeAccumulator = 0f;
        private double totalGameSeconds = 0;
        private float tickAccumulator = 0f;  // Аккумулятор боевых тиков (константа)
        private int currentTick = 0;
        private float lastTickRealTime = 0f; // Реальное время предыдущего тика
        
        // === World Tick (масштабируемый) ===
        private float worldTickAccumulator = 0f; // Аккумулятор мировых тиков (масштабируется скоростью)
        private int currentWorldTick = 0;
        private float lastWorldTickRealTime = 0f;
        
        // FIX WLD-M02: Guard against cascading event re-entrancy (2026-04-11)
        private bool isAdvancing = false;
        
        // === Events ===
        
        public event Action<int> OnMinutePassed;
        public event Action<int> OnHourPassed;
        public event Action<int> OnDayPassed;
        public event Action<int> OnMonthPassed;
        public event Action<int> OnYearPassed;
        public event Action<TimeOfDay> OnTimeOfDayChanged;
        public event Action<Season> OnSeasonChanged;
        public event Action<TimeSpeed> OnTimeSpeedChanged;
        
        /// <summary>
        /// Боевой тик — КОНСТАНТА, не зависит от скорости времени.
        /// Интервал = tickInterval реальных секунд.
        /// Используется: бой, накачка техник, кулдауны.
        /// Параметр: номер тика (int, монотонно возрастающий).
        /// </summary>
        public event Action<int> OnTick;
        
        /// <summary>
        /// Мировой тик — МАСШТАБИРУЕТСЯ скоростью времени.
        /// При Normal: 1 тик/сек. При Fast: 5 тик/сек. При VeryFast: 15 тик/сек.
        /// Используется: формации, Ци-пулы, NPC жизненный цикл, баффы.
        /// Параметр: номер мирового тика (int, монотонно возрастающий).
        /// </summary>
        public event Action<int> OnWorldTick;
        
        /// <summary>
        /// Покадровое событие тика — для систем с плавным обновлением (UI, накачка).
        /// Параметр: deltaTime в реальных секундах с прошлого кадра.
        /// </summary>
        public event Action<float> OnTickDelta;
        
        // === Properties ===
        
        /// <summary>Интервал боевого тика в реальных секундах — КОНСТАНТА.</summary>
        public float TickInterval => tickInterval;
        
        /// <summary>Минимальное время накачки техники = tick / 10.</summary>
        public float MinChargeTime => tickInterval / 10f;
        
        /// <summary>Текущий номер боевого тика (монотонно возрастающий).</summary>
        public int CurrentTick => currentTick;
        
        /// <summary>Текущий номер мирового тика (монотонно возрастающий).</summary>
        public int CurrentWorldTick => currentWorldTick;
        
        /// <summary>Множитель скорости тиков (1 = Normal, 5 = Fast, 15 = VeryFast).</summary>
        public float TickSpeedMultiplier => currentTimeSpeed == TimeSpeed.Paused ? 0f : GetSpeedRatio() / normalSpeedRatio;
        
        /// <summary>Реальное время между боевыми тиками.</summary>
        public float TickDeltaTime { get; private set; } = 0f;
        
        /// <summary>Реальное время между мировыми тиками (уменьшается при увеличении скорости).</summary>
        public float WorldTickDeltaTime { get; private set; } = 0f;
        
        public int Year => currentYear;
        public int Month => currentMonth;
        public int Day => currentDay;
        public int Hour => currentHour;
        public int Minute => currentMinute;
        public TimeSpeed CurrentSpeed => currentTimeSpeed;
        public TimeOfDay CurrentTimeOfDay => CalculateTimeOfDay();
        public Season CurrentSeason => CalculateSeason();
        public double TotalGameSeconds => totalGameSeconds;
        
        public string FormattedDate => $"{currentDay:D2}.{currentMonth:D2}.{currentYear}";
        public string FormattedTime => $"{currentHour:D2}:{currentMinute:D2}";
        public string FormattedDateTime => $"{FormattedDate} {FormattedTime}";
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            // Синглтон для доступа из боевой системы и других компонентов
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            
            lastTickRealTime = Time.time;
            lastWorldTickRealTime = Time.time;
        }
        
        private void Update()
        {
            // Покадровое событие — для плавного обновления UI и накачки
            float deltaTime = Time.deltaTime;
            OnTickDelta?.Invoke(deltaTime);
            
            // Недетерминированный режим (зависит от FPS)
            if (!useDeterministicTime && autoAdvance && currentTimeSpeed != TimeSpeed.Paused)
            {
                ProcessTimeUpdate(deltaTime);
            }
        }
        
        private void FixedUpdate()
        {
            // Детерминированный режим (фиксированный шаг, защита от FPS просадок)
            if (useDeterministicTime && autoAdvance && currentTimeSpeed != TimeSpeed.Paused)
            {
                ProcessTimeFixed();
            }
        }
        
        private void OnDestroy()
        {
            ServiceLocator.Unregister<TimeController>();
            
            if (Instance == this)
            {
                Instance = null;
            }
        }
        
        // === Time Processing ===
        
        /// <summary>
        /// Обработка времени через Update (недетерминированная).
        /// </summary>
        private void ProcessTimeUpdate(float deltaTime)
        {
            float ratio = GetSpeedRatio();
            timeAccumulator += deltaTime * ratio;
            
            // Боевой тик — КОНСТАНТА (не масштабируется скоростью)
            tickAccumulator += deltaTime;
            ProcessTick();
            
            // Мировой тик — МАСШТАБИРУЕТСЯ скоростью
            float speedMultiplier = ratio / normalSpeedRatio;
            worldTickAccumulator += deltaTime * speedMultiplier;
            ProcessWorldTick();
            
            while (timeAccumulator >= 60f)
            {
                timeAccumulator -= 60f;
                AdvanceMinute();
            }
        }
        
        /// <summary>
        /// Обработка времени через FixedUpdate (детерминированная).
        /// Защищена от просадок FPS, использует Time.fixedDeltaTime.
        /// </summary>
        private void ProcessTimeFixed()
        {
            float ratio = GetSpeedRatio();
            float gameTimeDelta = Time.fixedDeltaTime * ratio;
            
            timeAccumulator += gameTimeDelta;
            
            // Боевой тик — КОНСТАНТА
            tickAccumulator += Time.fixedDeltaTime;
            ProcessTick();
            
            // Мировой тик — МАСШТАБИРУЕТСЯ скоростью
            float speedMultiplier = ratio / normalSpeedRatio;
            worldTickAccumulator += Time.fixedDeltaTime * speedMultiplier;
            ProcessWorldTick();
            
            while (timeAccumulator >= 60f)
            {
                timeAccumulator -= 60f;
                AdvanceMinute();
            }
        }
        
        /// <summary>
        /// Обработка боевого тика — КОНСТАНТА, не зависит от скорости.
        /// Используется боевой системой, накачкой техник, кулдаунами.
        /// </summary>
        private void ProcessTick()
        {
            while (tickAccumulator >= tickInterval)
            {
                tickAccumulator -= tickInterval;
                currentTick++;
                
                float now = Time.time;
                TickDeltaTime = now - lastTickRealTime;
                lastTickRealTime = now;
                
                OnTick?.Invoke(currentTick);
            }
        }
        
        /// <summary>
        /// Обработка мирового тика — МАСШТАБИРУЕТСЯ скоростью времени.
        /// Normal: 1 тик/сек, Fast: 5 тик/сек, VeryFast: 15 тик/сек.
        /// Используется формациями, Ци-пулами, NPC, баффами.
        /// </summary>
        private void ProcessWorldTick()
        {
            while (worldTickAccumulator >= tickInterval)
            {
                worldTickAccumulator -= tickInterval;
                currentWorldTick++;
                
                float now = Time.time;
                WorldTickDeltaTime = now - lastWorldTickRealTime;
                lastWorldTickRealTime = now;
                
                OnWorldTick?.Invoke(currentWorldTick);
            }
        }
        
        private float GetSpeedRatio()
        {
            return currentTimeSpeed switch
            {
                TimeSpeed.Normal => normalSpeedRatio,
                TimeSpeed.Fast => fastSpeedRatio,
                TimeSpeed.VeryFast => veryFastSpeedRatio,
                _ => 0f
            };
        }
        
        /// <summary>
        /// Продвинуть время на 1 минуту.
        /// </summary>
        public void AdvanceMinute()
        {
            // FIX WLD-M02: Guard against external re-entrancy during cascade (2026-04-11)
            if (isAdvancing) return;
            isAdvancing = true;
            try
            {
                AdvanceMinuteInternal();
            }
            finally
            {
                isAdvancing = false;
            }
        }
        
        /// <summary>
        /// Internal minute advance (allows cascading to hour/day/month/year).
        /// </summary>
        private void AdvanceMinuteInternal()
        {
            currentMinute++;
            totalGameSeconds += 60;  // FIX: минута = 60 секунд, не 1
            
            OnMinutePassed?.Invoke(currentMinute);
            
            if (currentMinute >= minutesPerHour)
            {
                currentMinute = 0;
                AdvanceHourInternal();
            }
        }
        
        /// <summary>
        /// Продвинуть время на 1 час (public entry point with re-entrancy guard).
        /// </summary>
        public void AdvanceHour()
        {
            // FIX WLD-M02: If already advancing (called from AdvanceMinute cascade), use internal (2026-04-11)
            if (isAdvancing)
            {
                AdvanceHourInternal();
                return;
            }
            isAdvancing = true;
            try
            {
                AdvanceHourInternal();
            }
            finally
            {
                isAdvancing = false;
            }
        }
        
        /// <summary>
        /// Internal hour advance logic.
        /// FIX WLD-H01: OnHourPassed fires with the NEW hour value after increment (2026-04-11).
        /// This is intentional — "HourPassed" means the transition to this hour has occurred.
        /// </summary>
        private void AdvanceHourInternal()
        {
            // Вычисляем oldTimeOfDay ДО мутации hour
            TimeOfDay oldTimeOfDay = CalculateTimeOfDay();
            
            currentHour++;
            
            // FIX WLD-H01: Fire with post-increment value — subscribers receive the new hour (2026-04-11)
            OnHourPassed?.Invoke(currentHour);
            
            if (currentHour >= hoursPerDay)
            {
                currentHour = 0;
                AdvanceDayInternal();
            }
            
            // Проверяем смену времени суток
            TimeOfDay newTimeOfDay = CalculateTimeOfDay();
            if (oldTimeOfDay != newTimeOfDay)
            {
                OnTimeOfDayChanged?.Invoke(newTimeOfDay);
            }
        }
        
        /// <summary>
        /// Продвинуть время на 1 день (public entry point with re-entrancy guard).
        /// </summary>
        public void AdvanceDay()
        {
            if (isAdvancing)
            {
                AdvanceDayInternal();
                return;
            }
            isAdvancing = true;
            try
            {
                AdvanceDayInternal();
            }
            finally
            {
                isAdvancing = false;
            }
        }
        
        /// <summary>
        /// Internal day advance logic.
        /// </summary>
        private void AdvanceDayInternal()
        {
            currentDay++;
            Season oldSeason = CalculateSeason();
            
            OnDayPassed?.Invoke(currentDay);
            
            if (currentDay > daysPerMonth)
            {
                currentDay = 1;
                AdvanceMonthInternal();
            }
            
            // Проверяем смену сезона
            Season newSeason = CalculateSeason();
            if (oldSeason != newSeason)
            {
                OnSeasonChanged?.Invoke(newSeason);
            }
        }
        
        /// <summary>
        /// Продвинуть время на 1 месяц (public entry point with re-entrancy guard).
        /// </summary>
        public void AdvanceMonth()
        {
            if (isAdvancing)
            {
                AdvanceMonthInternal();
                return;
            }
            isAdvancing = true;
            try
            {
                AdvanceMonthInternal();
            }
            finally
            {
                isAdvancing = false;
            }
        }
        
        /// <summary>
        /// Internal month advance logic.
        /// </summary>
        private void AdvanceMonthInternal()
        {
            currentMonth++;
            
            OnMonthPassed?.Invoke(currentMonth);
            
            if (currentMonth > monthsPerYear)
            {
                currentMonth = 1;
                AdvanceYearInternal();
            }
        }
        
        /// <summary>
        /// Продвинуть время на 1 год (public entry point with re-entrancy guard).
        /// </summary>
        public void AdvanceYear()
        {
            if (isAdvancing)
            {
                AdvanceYearInternal();
                return;
            }
            isAdvancing = true;
            try
            {
                AdvanceYearInternal();
            }
            finally
            {
                isAdvancing = false;
            }
        }
        
        /// <summary>
        /// Internal year advance logic.
        /// </summary>
        private void AdvanceYearInternal()
        {
            currentYear++;
            OnYearPassed?.Invoke(currentYear);
        }
        
        // === Time Control ===
        
        /// <summary>
        /// Установить скорость времени.
        /// </summary>
        public void SetTimeSpeed(TimeSpeed speed)
        {
            if (currentTimeSpeed != speed)
            {
                currentTimeSpeed = speed;
                OnTimeSpeedChanged?.Invoke(speed);
            }
        }
        
        /// <summary>
        /// Пауза времени.
        /// </summary>
        public void Pause()
        {
            SetTimeSpeed(TimeSpeed.Paused);
        }
        
        /// <summary>
        /// Возобновить нормальную скорость.
        /// </summary>
        public void Resume()
        {
            SetTimeSpeed(TimeSpeed.Normal);
        }
        
        /// <summary>
        /// Установить конкретное время.
        /// </summary>
        public void SetTime(int hour, int minute = 0)
        {
            // FIX WLD-M01: Fire transition events when time-of-day changes (2026-04-11)
            TimeOfDay oldTimeOfDay = CalculateTimeOfDay();
            
            currentHour = Mathf.Clamp(hour, 0, hoursPerDay - 1);
            currentMinute = Mathf.Clamp(minute, 0, minutesPerHour - 1);
            
            TimeOfDay newTimeOfDay = CalculateTimeOfDay();
            if (oldTimeOfDay != newTimeOfDay)
            {
                OnTimeOfDayChanged?.Invoke(newTimeOfDay);
            }
        }
        
        /// <summary>
        /// Установить конкретную дату.
        /// </summary>
        public void SetDate(int day, int month, int year)
        {
            // FIX WLD-M01: Fire transition events when season changes (2026-04-11)
            Season oldSeason = CalculateSeason();
            
            currentDay = Mathf.Clamp(day, 1, daysPerMonth);
            currentMonth = Mathf.Clamp(month, 1, monthsPerYear);
            currentYear = Mathf.Max(1, year);
            
            Season newSeason = CalculateSeason();
            if (oldSeason != newSeason)
            {
                OnSeasonChanged?.Invoke(newSeason);
            }
        }
        
        /// <summary>
        /// Продвинуть время на указанное количество часов.
        /// </summary>
        public void AdvanceHours(int hours)
        {
            for (int i = 0; i < hours; i++)
            {
                AdvanceHour();
            }
        }
        
        /// <summary>
        /// Продвинуть время на указанное количество дней.
        /// </summary>
        public void AdvanceDays(int days)
        {
            for (int i = 0; i < days; i++)
            {
                AdvanceDay();
            }
        }
        
        // === Calculations ===
        
        private TimeOfDay CalculateTimeOfDay()
        {
            if (currentHour == 0) return TimeOfDay.Midnight;
            if (currentHour < 5) return TimeOfDay.Night;
            if (currentHour < 7) return TimeOfDay.Dawn;
            if (currentHour < 12) return TimeOfDay.Morning;
            if (currentHour < 14) return TimeOfDay.Noon;
            if (currentHour < 18) return TimeOfDay.Afternoon;
            if (currentHour < 21) return TimeOfDay.Evening;
            return TimeOfDay.Night;
        }
        
        private Season CalculateSeason()
        {
            // Простой расчёт: 3 месяца на сезон
            int seasonIndex = ((currentMonth - 1) / 3) % 4;
            return (Season)seasonIndex;
        }
        
        /// <summary>
        /// Получить общее количество дней с начала игры.
        /// FIX WLD-M06: Use totalGameSeconds for accuracy instead of calendar calculation (2026-04-11)
        /// </summary>
        public int GetTotalDays()
        {
            return (int)(totalGameSeconds / (hoursPerDay * minutesPerHour * 60));
        }
        
        /// <summary>
        /// Получить общее количество часов с начала игры.
        /// </summary>
        public long GetTotalHours()
        {
            return (long)GetTotalDays() * hoursPerDay + currentHour;
        }
        
        // === Save/Load ===
        
        public TimeSaveData GetSaveData()
        {
            return new TimeSaveData
            {
                Year = currentYear,
                Month = currentMonth,
                Day = currentDay,
                Hour = currentHour,
                Minute = currentMinute,
                TotalGameSeconds = totalGameSeconds,
                TimeSpeed = (int)currentTimeSpeed
            };
        }
        
        public void LoadSaveData(TimeSaveData data)
        {
            // FIX WLD-H06: Validate and clamp loaded values (2026-04-11)
            currentYear = Mathf.Max(1, data.Year);
            currentMonth = Mathf.Clamp(data.Month, 1, monthsPerYear);
            currentDay = Mathf.Clamp(data.Day, 1, daysPerMonth);
            currentHour = Mathf.Clamp(data.Hour, 0, hoursPerDay - 1);
            currentMinute = Mathf.Clamp(data.Minute, 0, minutesPerHour - 1);
            totalGameSeconds = Math.Max(0, data.TotalGameSeconds); // FIX CS1503: Math.Max для double (2026-04-11)
            currentTimeSpeed = (TimeSpeed)Mathf.Clamp(data.TimeSpeed, 0, 3);
        }
    }
    
    /// <summary>
    /// Время года.
    /// </summary>
    public enum Season
    {
        Spring,     // Весна
        Summer,     // Лето
        Autumn,     // Осень
        Winter      // Зима
    }
    
    /// <summary>
    /// Данные времени для сохранения.
    /// </summary>
    [Serializable]
    public class TimeSaveData
    {
        public int Year;
        public int Month;
        public int Day;
        public int Hour;
        public int Minute;
        public double TotalGameSeconds;
        public int TimeSpeed;
    }
}
