// ============================================================================
// TimeController.cs — Система игрового времени
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:17:18 UTC
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.World
{
    /// <summary>
    /// Контроллер игрового времени.
    /// Управляет течением времени, датами, сезонами.
    /// </summary>
    public class TimeController : MonoBehaviour
    {
        [Header("Time Settings")]
        [SerializeField] private TimeSpeed currentTimeSpeed = TimeSpeed.Normal;
        [SerializeField] private bool autoAdvance = true;
        
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
        
        // === Events ===
        
        public event Action<int> OnMinutePassed;
        public event Action<int> OnHourPassed;
        public event Action<int> OnDayPassed;
        public event Action<int> OnMonthPassed;
        public event Action<int> OnYearPassed;
        public event Action<TimeOfDay> OnTimeOfDayChanged;
        public event Action<Season> OnSeasonChanged;
        public event Action<TimeSpeed> OnTimeSpeedChanged;
        
        // === Properties ===
        
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
        
        private void Update()
        {
            if (autoAdvance && currentTimeSpeed != TimeSpeed.Paused)
            {
                ProcessTime();
            }
        }
        
        // === Time Processing ===
        
        private void ProcessTime()
        {
            float ratio = GetSpeedRatio();
            timeAccumulator += Time.deltaTime * ratio;
            
            while (timeAccumulator >= 60f)
            {
                timeAccumulator -= 60f;
                AdvanceMinute();
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
            currentMinute++;
            totalGameSeconds++;
            
            OnMinutePassed?.Invoke(currentMinute);
            
            if (currentMinute >= minutesPerHour)
            {
                currentMinute = 0;
                AdvanceHour();
            }
        }
        
        /// <summary>
        /// Продвинуть время на 1 час.
        /// </summary>
        public void AdvanceHour()
        {
            currentHour++;
            TimeOfDay oldTimeOfDay = CalculateTimeOfDay();
            
            OnHourPassed?.Invoke(currentHour);
            
            if (currentHour >= hoursPerDay)
            {
                currentHour = 0;
                AdvanceDay();
            }
            
            // Проверяем смену времени суток
            TimeOfDay newTimeOfDay = CalculateTimeOfDay();
            if (oldTimeOfDay != newTimeOfDay)
            {
                OnTimeOfDayChanged?.Invoke(newTimeOfDay);
            }
        }
        
        /// <summary>
        /// Продвинуть время на 1 день.
        /// </summary>
        public void AdvanceDay()
        {
            currentDay++;
            Season oldSeason = CalculateSeason();
            
            OnDayPassed?.Invoke(currentDay);
            
            if (currentDay > daysPerMonth)
            {
                currentDay = 1;
                AdvanceMonth();
            }
            
            // Проверяем смену сезона
            Season newSeason = CalculateSeason();
            if (oldSeason != newSeason)
            {
                OnSeasonChanged?.Invoke(newSeason);
            }
        }
        
        /// <summary>
        /// Продвинуть время на 1 месяц.
        /// </summary>
        public void AdvanceMonth()
        {
            currentMonth++;
            
            OnMonthPassed?.Invoke(currentMonth);
            
            if (currentMonth > monthsPerYear)
            {
                currentMonth = 1;
                AdvanceYear();
            }
        }
        
        /// <summary>
        /// Продвинуть время на 1 год.
        /// </summary>
        public void AdvanceYear()
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
            currentHour = Mathf.Clamp(hour, 0, hoursPerDay - 1);
            currentMinute = Mathf.Clamp(minute, 0, minutesPerHour - 1);
        }
        
        /// <summary>
        /// Установить конкретную дату.
        /// </summary>
        public void SetDate(int day, int month, int year)
        {
            currentDay = Mathf.Clamp(day, 1, daysPerMonth);
            currentMonth = Mathf.Clamp(month, 1, monthsPerYear);
            currentYear = Mathf.Max(1, year);
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
        /// </summary>
        public int GetTotalDays()
        {
            return (currentYear - 1) * monthsPerYear * daysPerMonth +
                   (currentMonth - 1) * daysPerMonth +
                   currentDay;
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
            currentYear = data.Year;
            currentMonth = data.Month;
            currentDay = data.Day;
            currentHour = data.Hour;
            currentMinute = data.Minute;
            totalGameSeconds = data.TotalGameSeconds;
            currentTimeSpeed = (TimeSpeed)data.TimeSpeed;
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
