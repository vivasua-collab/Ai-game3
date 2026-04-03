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
            
            // Распределение урона: 70% красная HP, 30% чёрная HP
            // Источник: ALGORITHMS.md §9 "Расчёт телесного урона"
            result.RedHPDamage = totalDamage * GameConstants.RED_HP_RATIO;
            result.BlackHPDamage = totalDamage * GameConstants.BLACK_HP_RATIO;
            
            // Сохраняем старое состояние
            bool wasFunctional = part.IsFunctional();
            bool wasSevered = part.IsSevered();
            
            // Применяем урон
            part.TakeDamage(result.RedHPDamage, result.BlackHPDamage);
            
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
        /// </summary>
        public static List<BodyPart> CreateHumanoidBody(int vitality = 10)
        {
            // HP = базовое (100) × (VIT/10)
            float hpMultiplier = vitality / 10f;
            float baseHP = 100f * hpMultiplier;
            
            // ⚠️ ВНИМАНИЕ: Базовые HP частей тела должны соответствовать BODY_SYSTEM.md:
            // | Часть | Функц. HP | Струк. HP |
            // | Head  | 50        | 100       |
            // | Torso | 100       | 200       |
            // | Heart | 80        | —         |
            // | Arm   | 40        | 80        |
            // | Hand  | 20        | 40        |
            // | Leg   | 50        | 100       |
            // | Foot  | 25        | 50        |
            //
            // Текущая реализация использует множители от baseHP.
            // При VIT=10 (baseHP=100):
            // - Head: 30 (должно быть 50) ⚠️ РАСХОЖДЕНИЕ
            // - Torso: 100 (должно быть 100) ✅
            // - Heart: 15 (должно быть 80) ⚠️ РАСХОЖДЕНИЕ
            // - Arm: 40 (должно быть 40) ✅
            // - Hand: 15 (должно быть 20) ⚠️ РАСХОЖДЕНИЕ
            // - Leg: 50 (должно быть 50) ✅
            // - Foot: 10 (должно быть 25) ⚠️ РАСХОЖДЕНИЕ
            
            List<BodyPart> parts = new List<BodyPart>
            {
                // Голова — жизненно важная
                new BodyPart(BodyPartType.Head, baseHP * 0.3f, isVital: true),
                
                // Торс — не является жизненно важным (смерть только от кровопотери)
                // Источник: BODY_SYSTEM.md — смерть только от head/heart
                new BodyPart(BodyPartType.Torso, baseHP * 1.0f, isVital: false),
                
                // Сердце — жизненно важное
                new BodyPart(BodyPartType.Heart, baseHP * 0.15f, isVital: true),
                
                // Руки
                new BodyPart(BodyPartType.LeftArm, baseHP * 0.4f),
                new BodyPart(BodyPartType.RightArm, baseHP * 0.4f),
                
                // Кисти
                new BodyPart(BodyPartType.LeftHand, baseHP * 0.15f),
                new BodyPart(BodyPartType.RightHand, baseHP * 0.15f),
                
                // Ноги
                new BodyPart(BodyPartType.LeftLeg, baseHP * 0.5f),
                new BodyPart(BodyPartType.RightLeg, baseHP * 0.5f),
                
                // Ступни
                new BodyPart(BodyPartType.LeftFoot, baseHP * 0.1f),
                new BodyPart(BodyPartType.RightFoot, baseHP * 0.1f)
            };
            
            return parts;
        }
        
        /// <summary>
        /// Создать четвероногое тело.
        /// Источник: BODY_SYSTEM.md "Части тела четвероногих (Quadruped)"
        /// </summary>
        public static List<BodyPart> CreateQuadrupedBody(int vitality = 10)
        {
            float hpMultiplier = vitality / 10f;
            float baseHP = 100f * hpMultiplier;
            
            // ⚠️ ВНИМАНИЕ: Должно соответствовать BODY_SYSTEM.md:
            // | Часть | Функц. HP | Струк. HP |
            // | Head  | 50        | 100       |
            // | Torso | 100       | 200       |
            // | Heart | 80        | —         |
            // | Front leg | 50    | 100       |
            // | Back leg  | 50    | 100       |
            // | Tail | 30          | 60        |
            
            List<BodyPart> parts = new List<BodyPart>
            {
                new BodyPart(BodyPartType.Head, baseHP * 0.4f, isVital: true),
                new BodyPart(BodyPartType.Torso, baseHP * 1.5f, isVital: true), // ⚠️ Torso vital=true, но в доке не указано
                new BodyPart(BodyPartType.Heart, baseHP * 0.2f, isVital: true),
                new BodyPart(BodyPartType.LeftArm, baseHP * 0.4f), // Передние ноги
                new BodyPart(BodyPartType.RightArm, baseHP * 0.4f),
                new BodyPart(BodyPartType.LeftLeg, baseHP * 0.5f),  // Задние ноги
                new BodyPart(BodyPartType.RightLeg, baseHP * 0.5f)
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
