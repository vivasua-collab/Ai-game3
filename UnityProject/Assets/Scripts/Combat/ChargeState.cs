// ============================================================================
// ChargeState.cs — Перечисления и структуры для системы накачки техник
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-05-04 04:26:00 UTC
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Состояние накачки техники.
    /// 
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  ПОТОК: None → Charging → Ready → Firing → None                         ║
    /// ║                  ↓                                                      ║
    /// ║            Interrupted → None                                           ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public enum ChargeState
    {
        /// <summary>Техника не накачивается</summary>
        None,
        /// <summary>Идёт накачка (Ци вливается)</summary>
        Charging,
        /// <summary>Накачка завершена, техника готова к срабатыванию</summary>
        Ready,
        /// <summary>Техника срабатывает (анимация/эффект)</summary>
        Firing,
        /// <summary>Накачка прервана (урон, стан, отмена)</summary>
        Interrupted
    }

    /// <summary>
    /// Причина прерывания накачки.
    /// Определяет % возврата Ци.
    /// </summary>
    public enum ChargeInterruptReason
    {
        /// <summary>Игрок отменил нажатием той же клавиши → 70% Ци возврат</summary>
        PlayerCancel,
        /// <summary>Получен урон ≥ threshold → 50% Ци возврат</summary>
        DamageInterrupt,
        /// <summary>Оглушение (stun) → 0% Ци возврат</summary>
        StunInterrupt,
        /// <summary>Смерть практика → 0% Ци возврат</summary>
        DeathInterrupt,
        /// <summary>Недостаточно Ци для продолжения → 70% Ци возврат</summary>
        QiDepleted
    }

    /// <summary>
    /// Данные текущей накачки техники.
    /// Хранит полное состояние активной накачки.
    /// </summary>
    [Serializable]
    public struct TechniqueChargeData
    {
        // === Идентификация ===
        /// <summary>Какая техника накачивается</summary>
        public LearnedTechnique Technique;
        /// <summary>Текущее состояние накачки</summary>
        public ChargeState State;

        // === Прогресс ===
        /// <summary>Прогресс накачки 0.0-1.0 (время)</summary>
        public float ChargeProgress;
        /// <summary>Полное время накачки (сек)</summary>
        public float ChargeTime;
        /// <summary>Время, прошедшее с начала накачки (сек)</summary>
        public float ElapsedTime;

        // === Ци ===
        /// <summary>Сколько Ци уже вложено</summary>
        public long QiCharged;
        /// <summary>Сколько Ци нужно всего</summary>
        public long QiTotalRequired;
        /// <summary>Скорость вливания Ци (Ци/сек)</summary>
        public float QiChargeRate;

        // === Ограничения ===
        /// <summary>Можно ли двигаться во время накачки</summary>
        public bool CanMoveWhileCharging;
        /// <summary>Множитель скорости движения (если можно двигаться)</summary>
        public float MoveSpeedMultiplier;
        /// <summary>Прерывается ли уроном</summary>
        public bool CanBeInterruptedByDamage;
        /// <summary>Минимальный урон для прерывания</summary>
        public float InterruptDamageThreshold;
        /// <summary>Минимальное время накачки (tick/10)</summary>
        public float MinChargeTime;

        // === Результат прерывания ===
        /// <summary>Причина прерывания (если State = Interrupted)</summary>
        public ChargeInterruptReason InterruptReason;
        /// <summary>% возврата Ци при прерывании</summary>
        public float QiReturnPercent;

        /// <summary>
        /// Создать пустую (неактивную) структуру накачки.
        /// </summary>
        public static TechniqueChargeData Empty => new TechniqueChargeData
        {
            Technique = null,
            State = ChargeState.None,
            ChargeProgress = 0f,
            ChargeTime = 0f,
            ElapsedTime = 0f,
            QiCharged = 0,
            QiTotalRequired = 0,
            QiChargeRate = 0f,
            CanMoveWhileCharging = true,
            MoveSpeedMultiplier = 1f,
            CanBeInterruptedByDamage = true,
            InterruptDamageThreshold = 0f,
            MinChargeTime = 0.1f,
            InterruptReason = ChargeInterruptReason.PlayerCancel,
            QiReturnPercent = 0f
        };

        /// <summary>
        /// Активна ли накачка?
        /// </summary>
        public bool IsActive => State == ChargeState.Charging || State == ChargeState.Ready;
    }
}
