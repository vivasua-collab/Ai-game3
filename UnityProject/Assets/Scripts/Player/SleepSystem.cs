// ============================================================================
// SleepSystem.cs — Система сна и консолидации
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-31 14:28:00 UTC
// ============================================================================
//
// Источник: docs/STAT_THRESHOLD_SYSTEM.md, docs/ARCHITECTURE.md
//
// Функции:
// 1. Управление сном персонажа
// 2. Консолидация виртуальных дельт
// 3. Восстановление HP и Ци
// 4. Обновление времени
//
// ╔═══════════════════════════════════════════════════════════════════════════╗
// ║  ПРАВИЛА СНА                                                              ║
// ╠═══════════════════════════════════════════════════════════════════════════╣
// ║                                                                            ║
// ║  Минимум для закрепления: 4 часа                                           ║
// ║  Максимум за сон: +0.20 к дельте                                           ║
// ║  Восстановление HP: 100% за 8 часов                                        ║
// ║  Восстановление Ци: зависит от проводимости                                ║
// ║                                                                            ║
// ║  Формула закрепления: consolidated = min(delta, hours * 0.025)             ║
// ║                                                                            ║
// ╚═══════════════════════════════════════════════════════════════════════════╝
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Qi;
using CultivationGame.Body;

namespace CultivationGame.Player
{
    /// <summary>
    /// Состояние сна.
    /// </summary>
    public enum SleepState
    {
        Awake,              // Бодрствует
        FallingAsleep,      // Засыпает
        Sleeping,           // Спит
        WakingUp,           // Просыпается
        Interrupted         // Прерван
    }

    /// <summary>
    /// Результат сна.
    /// </summary>
    public struct SleepResult
    {
        public float HoursSlept;
        public SleepState EndState;
        public SleepConsolidationResult StatConsolidation;
        public int QiRecovered;
        public float HPRecovered;
        public bool WasInterrupted;
        public string InterruptionReason;
    }

    /// <summary>
    /// Система сна — управляет сном и консолидацией характеристик.
    /// 
    /// Функции:
    /// - Начало/конец сна
    /// - Консолидация дельт через StatDevelopment
    /// - Восстановление HP и Ци
    /// - Взаимодействие с TimeController
    /// </summary>
    public class SleepSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Sleep Settings")]
        [SerializeField] private float minSleepHours = 4f;
        [SerializeField] private float maxSleepHours = 12f;
        [SerializeField] private float optimalSleepHours = 8f;

        [Header("Recovery")]
        [SerializeField] private float hpRecoveryRate = 0.125f;      // 12.5% в час (100% за 8ч)
        [SerializeField] private float staminaRecoveryRate = 1.0f;    // 100% за час

        [Header("References")]
        [SerializeField] private StatDevelopment statDevelopment;
        [SerializeField] private QiController qiController;
        [SerializeField] private BodyController bodyController;
        [SerializeField] private World.TimeController timeController;

        #endregion

        #region Runtime

        private SleepState currentState = SleepState.Awake;
        private float sleepStartTime = 0f;
        private float sleepDuration = 0f;
        private bool isInitialized = false;

        #endregion

        #region Events

        public event Action<SleepState, SleepState> OnSleepStateChanged;  // (oldState, newState)
        public event Action<float> OnSleepStarted;                        // (hours)
        public event Action<SleepResult> OnSleepEnded;
        public event Action<float> OnSleepProgress;                      // (progress 0-1)

        #endregion

        #region Properties

        public SleepState CurrentState => currentState;
        public bool IsAwake => currentState == SleepState.Awake;
        public bool IsSleeping => currentState == SleepState.Sleeping;
        public float SleepProgress => sleepDuration > 0 ? Mathf.Clamp01(sleepDuration / maxSleepHours) : 0f;
        public float HoursSlept => sleepDuration;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            if (statDevelopment == null)
                statDevelopment = GetComponent<StatDevelopment>();
            if (qiController == null)
                qiController = GetComponent<QiController>();
            if (bodyController == null)
                bodyController = GetComponent<BodyController>();
            if (timeController == null)
                timeController = FindFirstObjectByType<World.TimeController>();

            isInitialized = true;
        }

        #endregion

        #region Sleep Control

        /// <summary>
        /// Начать сон.
        /// </summary>
        /// <param name="hours">Желаемая длительность (0 = до полного восстановления)</param>
        public void StartSleep(float hours = 0f)
        {
            if (!IsAwake)
            {
                Debug.LogWarning("[SleepSystem] Already sleeping or in transition");
                return;
            }

            // Определяем длительность сна
            if (hours <= 0f)
            {
                // Авто-определение: до полного восстановления HP/Ци
                hours = CalculateOptimalSleepTime();
            }

            hours = Mathf.Clamp(hours, minSleepHours, maxSleepHours);

            SetState(SleepState.FallingAsleep);
            sleepStartTime = Time.time;
            sleepDuration = 0f;

            // Переходим к сну
            SetState(SleepState.Sleeping);

            OnSleepStarted?.Invoke(hours);

            Debug.Log($"[SleepSystem] Started sleeping for {hours:F1} hours");
        }

        /// <summary>
        /// Завершить сон.
        /// </summary>
        public void EndSleep()
        {
            if (!IsSleeping)
            {
                Debug.LogWarning("[SleepSystem] Not sleeping");
                return;
            }

            var result = ProcessSleepEnd(false, "");

            SetState(SleepState.WakingUp);
            SetState(SleepState.Awake);

            OnSleepEnded?.Invoke(result);

            Debug.Log($"[SleepSystem] Sleep ended: {result.HoursSlept:F1}h, Stats gained: {result.StatConsolidation.StatResults.Count}");
        }

        /// <summary>
        /// Прервать сон.
        /// </summary>
        public void InterruptSleep(string reason = "")
        {
            if (!IsSleeping)
                return;

            var result = ProcessSleepEnd(true, reason);

            SetState(SleepState.Interrupted);
            SetState(SleepState.Awake);

            OnSleepEnded?.Invoke(result);

            Debug.Log($"[SleepSystem] Sleep interrupted: {reason}");
        }

        /// <summary>
        /// Обновить сон (вызывать каждый игровой час).
        /// </summary>
        /// <param name="gameHours">Прошедшие игровые часы</param>
        public void UpdateSleep(float gameHours)
        {
            if (!IsSleeping) return;

            sleepDuration += gameHours;

            // Восстановление
            ProcessRecovery(gameHours);

            // Проверяем достижение максимума
            if (sleepDuration >= maxSleepHours)
            {
                EndSleep();
            }

            OnSleepProgress?.Invoke(SleepProgress);
        }

        /// <summary>
        /// Быстрый сон (мгновенный, для тестов и отдыха).
        /// </summary>
        public SleepResult QuickSleep(float hours)
        {
            hours = Mathf.Clamp(hours, minSleepHours, maxSleepHours);
            sleepDuration = hours;

            return ProcessSleepEnd(false, "");
        }

        #endregion

        #region Processing

        private SleepResult ProcessSleepEnd(bool interrupted, string reason)
        {
            var result = new SleepResult
            {
                HoursSlept = sleepDuration,
                EndState = interrupted ? SleepState.Interrupted : SleepState.Awake,
                WasInterrupted = interrupted,
                InterruptionReason = reason
            };

            // 1. Консолидация характеристик
            if (statDevelopment != null)
            {
                result.StatConsolidation = statDevelopment.ConsolidateSleep(sleepDuration);
            }

            // 2. Финальное восстановление HP/Ци
            result.HPRecovered = ProcessFinalHPRecovery();
            result.QiRecovered = ProcessFinalQiRecovery();

            // Сброс
            sleepDuration = 0f;

            return result;
        }

        private void ProcessRecovery(float hours)
        {
            // Восстановление HP
            if (bodyController != null)
            {
                float hpAmount = hours * hpRecoveryRate * 100; // Процент от макс HP
                bodyController.HealAll(hpAmount, hpAmount * 0.3f);
            }

            // Восстановление выносливости (мгновенно при сне)
            // ...

            // Ци восстанавливается естественным путём через QiController
        }

        private float ProcessFinalHPRecovery()
        {
            if (bodyController == null) return 0f;

            float beforeHP = bodyController.HealthPercent * 100;
            bodyController.FullRestore();
            float afterHP = 100f;

            return afterHP - beforeHP;
        }

        private int ProcessFinalQiRecovery()
        {
            if (qiController == null) return 0;

            long beforeQi = qiController.CurrentQi;
            qiController.RestoreFull();
            long afterQi = qiController.MaxQi;

            return (int)(afterQi - beforeQi);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Рассчитать оптимальное время сна.
        /// </summary>
        private float CalculateOptimalSleepTime()
        {
            float hours = minSleepHours;

            // Добавляем время для восстановления HP
            if (bodyController != null)
            {
                float hpMissing = 1f - bodyController.HealthPercent;
                hours += hpMissing / hpRecoveryRate;
            }

            // Добавляем время для восстановления Ци
            if (qiController != null)
            {
                float qiMissing = 1f - qiController.QiPercent;
                // Ци восстанавливается медленнее
                hours += qiMissing * 2f;
            }

            return Mathf.Clamp(hours, minSleepHours, optimalSleepHours);
        }

        private void SetState(SleepState newState)
        {
            if (currentState == newState) return;

            SleepState oldState = currentState;
            currentState = newState;

            OnSleepStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        /// Получить статус сна в текстовом формате.
        /// </summary>
        public string GetStatusText()
        {
            return currentState switch
            {
                SleepState.Awake => "Бодрствует",
                SleepState.FallingAsleep => "Засыпает...",
                SleepState.Sleeping => $"Спит ({sleepDuration:F1}ч)",
                SleepState.WakingUp => "Просыпается...",
                SleepState.Interrupted => "Сон прерван",
                _ => "Неизвестно"
            };
        }

        #endregion
    }
}
