// ============================================================================
// BodyDamage.cs — Обработка телесного урона
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-03-31 08:46:09 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Body
{
    /// <summary>
    /// Результат телесного урона.
    /// </summary>
    public struct BodyDamageResult
    {
        public BodyPartType HitPart;
        public float RedHPDamage;
        public float BlackHPDamage;
        public BodyPartState PreviousState;
        public BodyPartState NewState;
        public bool WasSevered;          // Отрублено
        public bool WasDisabled;         // Парализовано
        public bool IsFatal;             // Смертельно
        public bool CausedBleeding;      // Кровотечение
        public bool CausedShock;         // Шок
        public bool WasAbsorbed;         // Редактировано: 2026-04-03 - Урон поглощён (щит/блок)
    }
    
    /// <summary>
    /// Статический класс для обработки телесного урона (Kenshi-style).
    /// </summary>
    public static class BodyDamage
    {
        /// <summary>
        /// Применить урон к части тела.
        /// FIX BOD-M01: Убран двойной 70/30 split. Распределение урона теперь ТОЛЬКО
        /// в BodyPart.ApplyDamage(). BodyDamage.ApplyDamage передаёт totalDamage напрямую.
        /// </summary>
        public static BodyDamageResult ApplyDamage(
            BodyPart part,
            float totalDamage)
        {
            BodyDamageResult result = new BodyDamageResult
            {
                HitPart = part.PartType,
                PreviousState = part.State
            };
            
            // FIX BOD-M01: Убран double split. BodyPart.ApplyDamage() делает 70/30.
            // Просто передаём общий урон в BodyPart.
            
            // Сохраняем старое состояние
            bool wasFunctional = part.IsFunctional();
            bool wasSevered = part.IsSevered();
            
            // Применяем урон через BodyPart.ApplyDamage (70/30 split внутри)
            part.ApplyDamage(totalDamage);
            
            // Вычисляем фактически нанесённый урон для результата
            result.RedHPDamage = totalDamage * GameConstants.RED_HP_RATIO;
            result.BlackHPDamage = totalDamage * GameConstants.BLACK_HP_RATIO;
            
            result.NewState = part.State;
            
            // Проверяем последствия
            result.WasSevered = !wasSevered && part.IsSevered();
            result.WasDisabled = wasFunctional && !part.IsFunctional();
            
            // Смертельные части: голова и сердце
            // Источник: BODY_SYSTEM.md "Части тела гуманоида"
            if (part.IsVital && part.CurrentRedHP <= 0)
            {
                result.IsFatal = true;
            }
            
            // Шанс кровотечения (при ранении+)
            if (part.State >= BodyPartState.Wounded)
            {
                result.CausedBleeding = UnityEngine.Random.value < 0.3f;
            }
            
            // Шанс шока при большом уроне
            if (totalDamage > part.MaxRedHP * 0.5f)
            {
                result.CausedShock = UnityEngine.Random.value < 0.2f;
            }
            
            return result;
        }
        
        /// <summary>
        /// Создать стандартный гуманоидный набор частей тела.
        /// HP базируется на живучести (Vitality).
        /// Источник: BODY_SYSTEM.md "Части тела гуманоида"
        /// FIX BOD-C01: HP значения приведены в соответствие с BODY_SYSTEM.md.
        /// При VIT=10: Head=50, Torso=100, Heart=80, Arm=40, Hand=20, Leg=50, Foot=25
        /// </summary>
        public static List<BodyPart> CreateHumanoidBody(int vitality = 10)
        {
            // HP = базовое × (VIT/10)
            float hpMultiplier = vitality / 10f;
            
            // FIX BOD-C01: Точные множители по BODY_SYSTEM.md
            // Базовое HP для VIT=10: Head=50, Torso=100, Heart=80, Arm=40, Hand=20, Leg=50, Foot=25
            List<BodyPart> parts = new List<BodyPart>
            {
                // Голова — жизненно важная (50 HP при VIT=10)
                new BodyPart(BodyPartType.Head, 50f * hpMultiplier, isVital: true),
                
                // Торс — не жизненно важный (100 HP при VIT=10)
                // Источник: BODY_SYSTEM.md — смерть только от head/heart
                new BodyPart(BodyPartType.Torso, 100f * hpMultiplier, isVital: false),
                
                // Сердце — жизненно важное (80 HP при VIT=10, только красная HP)
                new BodyPart(BodyPartType.Heart, 80f * hpMultiplier, isVital: true),
                
                // Руки (40 HP при VIT=10)
                new BodyPart(BodyPartType.LeftArm, 40f * hpMultiplier),
                new BodyPart(BodyPartType.RightArm, 40f * hpMultiplier),
                
                // Кисти (20 HP при VIT=10)
                new BodyPart(BodyPartType.LeftHand, 20f * hpMultiplier),
                new BodyPart(BodyPartType.RightHand, 20f * hpMultiplier),
                
                // Ноги (50 HP при VIT=10)
                new BodyPart(BodyPartType.LeftLeg, 50f * hpMultiplier),
                new BodyPart(BodyPartType.RightLeg, 50f * hpMultiplier),
                
                // Ступни (25 HP при VIT=10)
                new BodyPart(BodyPartType.LeftFoot, 25f * hpMultiplier),
                new BodyPart(BodyPartType.RightFoot, 25f * hpMultiplier)
            };
            
            return parts;
        }
        
        /// <summary>
        /// Создать четвероногое тело.
        /// Источник: BODY_SYSTEM.md "Части тела четвероногих (Quadruped)"
        /// FIX BOD-C01: HP значения приведены в соответствие с BODY_SYSTEM.md.
        /// FIX BOD-L03: Quadruped Torso isVital=false по документации.
        /// </summary>
        public static List<BodyPart> CreateQuadrupedBody(int vitality = 10)
        {
            float hpMultiplier = vitality / 10f;
            
            List<BodyPart> parts = new List<BodyPart>
            {
                // Голова — жизненно важная (50 HP при VIT=10)
                new BodyPart(BodyPartType.Head, 50f * hpMultiplier, isVital: true),
                // FIX BOD-L03: Torso isVital=false — по документации смерть только от head/heart
                new BodyPart(BodyPartType.Torso, 150f * hpMultiplier, isVital: false),
                // Сердце — жизненно важное (80 HP при VIT=10)
                new BodyPart(BodyPartType.Heart, 80f * hpMultiplier, isVital: true),
                // Передние ноги (50 HP при VIT=10)
                new BodyPart(BodyPartType.LeftArm, 50f * hpMultiplier),
                new BodyPart(BodyPartType.RightArm, 50f * hpMultiplier),
                // Задние ноги (50 HP при VIT=10)
                new BodyPart(BodyPartType.LeftLeg, 50f * hpMultiplier),
                new BodyPart(BodyPartType.RightLeg, 50f * hpMultiplier)
            };
            
            return parts;
        }
        
        /// <summary>
        /// Рассчитать штрафы от повреждений.
        /// Источник: ALGORITHMS.md §5 "Пайплайн урона"
        /// </summary>
        public static float CalculateDamagePenalty(List<BodyPart> parts)
        {
            float penalty = 0f;
            
            foreach (var part in parts)
            {
                if (part.IsSevered())
                {
                    penalty += 0.3f; // 30% штраф за отрубленную часть
                }
                else if (!part.IsFunctional())
                {
                    penalty += 0.15f; // 15% штраф за парализованную
                }
                else if (part.State == BodyPartState.Wounded)
                {
                    penalty += 0.05f; // 5% штраф за раненую
                }
            }
            
            return Math.Min(0.9f, penalty); // Максимум 90% штраф
        }
        
        /// <summary>
        /// Проверить, жив ли организм.
        /// Смерть при HP≤0 жизненно важной части (head, heart).
        /// </summary>
        public static bool IsAlive(List<BodyPart> parts)
        {
            foreach (var part in parts)
            {
                if (part.IsVital && part.CurrentRedHP <= 0)
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// Получить общее состояние организма (% здоровья).
        /// </summary>
        public static float GetOverallHealthPercent(List<BodyPart> parts)
        {
            float totalMax = 0f;
            float totalCurrent = 0f;
            
            foreach (var part in parts)
            {
                totalMax += part.MaxRedHP;
                totalCurrent += part.CurrentRedHP;
            }
            
            return totalMax > 0 ? totalCurrent / totalMax : 0f;
        }
    }
}
