// ============================================================================
// QiBuffer.cs — Буфер Ци (защита от урона)
// Cultivation World Simulator
// Версия: 1.1 — Fix-01: int→long для Qi > 2.1B
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-04-10 13:49:00 UTC
// ============================================================================

using System;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Результат обработки урона через буфер Ци.
    /// FIX: QiConsumed/QiRemaining → long для поддержки Qi > 2.1B на L5+
    /// </summary>
    public struct QiBufferResult
    {
        public float AbsorbedDamage;     // Поглощённый урон
        public float PiercingDamage;     // Пробивший урон (к HP)
        public long QiConsumed;          // Потраченное Ци (FIX: int→long)
        public long QiRemaining;         // Оставшееся Ци (FIX: int→long)
        public bool WasShieldActive;     // Был ли активен щит
        public bool WasQiDepleted;       // Закончилось ли Ци
    }
    
    /// <summary>
    /// Тип защиты Ци.
    /// </summary>
    public enum QiDefenseType
    {
        None,       // Нет защиты
        RawQi,      // Сырая Ци (естественная защита)
        Shield      // Щитовая техника (активная защита)
    }
    
    /// <summary>
    /// Тип урона для расчёта буфера Ци.
    /// Источник: ALGORITHMS.md §2 "Буфер Ци"
    /// </summary>
    public enum DamageSourceType
    {
        QiTechnique,    // Техники Ци (есть резонанс)
        Physical        // Физический урон (нет резонанса)
    }
    
    /// <summary>
    /// Статический класс для расчёта поглощения урона через Ци.
    /// 
    /// Источник: ALGORITHMS.md §2 "Буфер Ци"
    /// 
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  КРИТИЧЕСКОЕ РАЗЛИЧЕНИЕ: ТЕХНИКИ ЦИ vs ФИЗИЧЕСКИЙ УРОН                     ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║                                                                            ║
    /// ║  ТЕХНИКИ ЦИ (с резонансом):                                                ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐   ║
    /// ║  │ Сырая Ци:  90% поглощение, 3:1 соотношение, 10% ВСЕГДА пробивает    │   ║
    /// ║  │ Щит:       100% поглощение, 1:1 соотношение, 0% пробитие            │   ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘   ║
    /// ║                                                                            ║
    /// ║  ФИЗИЧЕСКИЙ УРОН (без резонанса):                                          ║
    /// ║  ┌─────────────────────────────────────────────────────────────────────┐   ║
    /// ║  │ Сырая Ци:  80% поглощение, 5:1 соотношение, 20% ВСЕГДА пробивает    │   ║
    /// ║  │ Щит:       100% поглощение, 2:1 соотношение, 0% пробитие            │   ║
    /// ║  └─────────────────────────────────────────────────────────────────────┘   ║
    /// ║                                                                            ║
    /// ║  Почему различие?                                                          ║
    /// ║  - Нет резонанса — Ци "гасит" физику грубо, без точности                  ║
    /// ║  - Труднее блокировать меч, чем сгусток Ци                                 ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public static class QiBuffer
    {
        // === Константы для техник Ци (из ALGORITHMS.md) ===
        private const float QI_TECHNIQUE_RAW_ABSORPTION = 0.9f;     // 90%
        private const float QI_TECHNIQUE_RAW_PIERCING = 0.1f;       // 10%
        private const float QI_TECHNIQUE_RAW_RATIO = 3.0f;          // 3:1
        private const float QI_TECHNIQUE_SHIELD_RATIO = 1.0f;       // 1:1
        
        // === Константы для физического урона (из ALGORITHMS.md) ===
        private const float PHYSICAL_RAW_ABSORPTION = 0.8f;         // 80%
        private const float PHYSICAL_RAW_PIERCING = 0.2f;           // 20%
        private const float PHYSICAL_RAW_RATIO = 5.0f;              // 5:1
        private const float PHYSICAL_SHIELD_RATIO = 2.0f;           // 2:1
        
        /// <summary>
        /// Обработать урон от ТЕХНИКИ ЦИ через буфер (alias).
        /// FIX: currentQi long
        /// </summary>
        public static QiBufferResult ProcessDamage(
            float incomingDamage,
            long currentQi,
            QiDefenseType defenseType)
        {
            return ProcessQiTechniqueDamage(incomingDamage, currentQi, defenseType);
        }
        
        /// <summary>
        /// Обработать урон от ТЕХНИКИ ЦИ через буфер.
        /// Источник: ALGORITHMS.md §2.3 "Техники Ци"
        /// FIX: currentQi long для Qi > 2.1B
        /// </summary>
        public static QiBufferResult ProcessQiTechniqueDamage(
            float incomingDamage,
            long currentQi,
            QiDefenseType defenseType)
        {
            QiBufferResult result = new QiBufferResult
            {
                WasShieldActive = defenseType == QiDefenseType.Shield,
                WasQiDepleted = false
            };
            
            if (defenseType == QiDefenseType.None || currentQi < GameConstants.MIN_QI_FOR_BUFFER)
            {
                result.AbsorbedDamage = 0f;
                result.PiercingDamage = incomingDamage;
                result.QiConsumed = 0;
                result.QiRemaining = currentQi;
                return result;
            }
            
            if (defenseType == QiDefenseType.RawQi)
            {
                float absorbableDamage = incomingDamage * QI_TECHNIQUE_RAW_ABSORPTION;
                float guaranteedPiercing = incomingDamage * QI_TECHNIQUE_RAW_PIERCING;
                long requiredQi = (long)(absorbableDamage * QI_TECHNIQUE_RAW_RATIO);
                
                if (currentQi >= requiredQi)
                {
                    result.AbsorbedDamage = absorbableDamage;
                    result.PiercingDamage = guaranteedPiercing;
                    result.QiConsumed = requiredQi;
                    result.QiRemaining = currentQi - requiredQi;
                }
                else
                {
                    double absorbRatio = (double)currentQi / requiredQi;
                    result.AbsorbedDamage = absorbableDamage * (float)absorbRatio;
                    result.PiercingDamage = incomingDamage - result.AbsorbedDamage;
                    result.QiConsumed = currentQi;
                    result.QiRemaining = 0;
                    result.WasQiDepleted = true;
                }
            }
            else if (defenseType == QiDefenseType.Shield)
            {
                long requiredQi = (long)(incomingDamage * QI_TECHNIQUE_SHIELD_RATIO);
                
                if (currentQi >= requiredQi)
                {
                    result.AbsorbedDamage = incomingDamage;
                    result.PiercingDamage = 0f;
                    result.QiConsumed = requiredQi;
                    result.QiRemaining = currentQi - requiredQi;
                }
                else
                {
                    double absorbedRatio = (double)currentQi / (incomingDamage * QI_TECHNIQUE_SHIELD_RATIO);
                    result.AbsorbedDamage = incomingDamage * (float)absorbedRatio;
                    result.PiercingDamage = incomingDamage - result.AbsorbedDamage;
                    result.QiConsumed = currentQi;
                    result.QiRemaining = 0;
                    result.WasQiDepleted = true;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Обработать ФИЗИЧЕСКИЙ урон через буфер Ци.
        /// Источник: ALGORITHMS.md §2.3 "Физический урон"
        /// FIX: currentQi long для Qi > 2.1B
        /// </summary>
        public static QiBufferResult ProcessPhysicalDamage(
            float incomingDamage,
            long currentQi,
            QiDefenseType defenseType)
        {
            QiBufferResult result = new QiBufferResult
            {
                WasShieldActive = defenseType == QiDefenseType.Shield,
                WasQiDepleted = false
            };
            
            if (defenseType == QiDefenseType.None || currentQi < GameConstants.MIN_QI_FOR_BUFFER)
            {
                result.AbsorbedDamage = 0f;
                result.PiercingDamage = incomingDamage;
                result.QiConsumed = 0;
                result.QiRemaining = currentQi;
                return result;
            }
            
            if (defenseType == QiDefenseType.RawQi)
            {
                float absorbableDamage = incomingDamage * PHYSICAL_RAW_ABSORPTION;
                float guaranteedPiercing = incomingDamage * PHYSICAL_RAW_PIERCING;
                long requiredQi = (long)(absorbableDamage * PHYSICAL_RAW_RATIO);
                
                if (currentQi >= requiredQi)
                {
                    result.AbsorbedDamage = absorbableDamage;
                    result.PiercingDamage = guaranteedPiercing;
                    result.QiConsumed = requiredQi;
                    result.QiRemaining = currentQi - requiredQi;
                }
                else
                {
                    double absorbRatio = (double)currentQi / requiredQi;
                    result.AbsorbedDamage = absorbableDamage * (float)absorbRatio;
                    result.PiercingDamage = incomingDamage - result.AbsorbedDamage;
                    result.QiConsumed = currentQi;
                    result.QiRemaining = 0;
                    result.WasQiDepleted = true;
                }
            }
            else if (defenseType == QiDefenseType.Shield)
            {
                long requiredQi = (long)(incomingDamage * PHYSICAL_SHIELD_RATIO);
                
                if (currentQi >= requiredQi)
                {
                    result.AbsorbedDamage = incomingDamage;
                    result.PiercingDamage = 0f;
                    result.QiConsumed = requiredQi;
                    result.QiRemaining = currentQi - requiredQi;
                }
                else
                {
                    double absorbedRatio = (double)currentQi / (incomingDamage * PHYSICAL_SHIELD_RATIO);
                    result.AbsorbedDamage = incomingDamage * (float)absorbedRatio;
                    result.PiercingDamage = incomingDamage - result.AbsorbedDamage;
                    result.QiConsumed = currentQi;
                    result.QiRemaining = 0;
                    result.WasQiDepleted = true;
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Рассчитать необходимое Ци для поглощения урона.
        /// FIX: long return для Qi > 2.1B
        /// </summary>
        public static long CalculateRequiredQi(float damage, QiDefenseType defenseType, DamageSourceType damageSource = DamageSourceType.QiTechnique)
        {
            if (defenseType == QiDefenseType.None) return 0;
            
            bool isPhysical = damageSource == DamageSourceType.Physical;
            
            float absorbable = defenseType == QiDefenseType.RawQi 
                ? damage * (isPhysical ? PHYSICAL_RAW_ABSORPTION : QI_TECHNIQUE_RAW_ABSORPTION) 
                : damage;
            
            float ratio = defenseType == QiDefenseType.RawQi 
                ? (isPhysical ? PHYSICAL_RAW_RATIO : QI_TECHNIQUE_RAW_RATIO)
                : (isPhysical ? PHYSICAL_SHIELD_RATIO : QI_TECHNIQUE_SHIELD_RATIO);
            
            return (long)(absorbable * ratio);
        }
        
        /// <summary>
        /// Проверить, достаточно ли Ци для защиты.
        /// FIX: currentQi long
        /// </summary>
        public static bool CanAbsorbDamage(float damage, long currentQi, QiDefenseType defenseType, DamageSourceType damageSource = DamageSourceType.QiTechnique)
        {
            if (defenseType == QiDefenseType.None) return false;
            long required = CalculateRequiredQi(damage, defenseType, damageSource);
            return currentQi >= required;
        }
        
        /// <summary>
        /// Получить эффективность защиты Ци.
        /// </summary>
        public static float GetDefenseEfficiency(QiDefenseType defenseType, DamageSourceType damageSource = DamageSourceType.QiTechnique)
        {
            return defenseType switch
            {
                QiDefenseType.None => 0f,
                QiDefenseType.RawQi => damageSource == DamageSourceType.Physical 
                    ? PHYSICAL_RAW_ABSORPTION 
                    : QI_TECHNIQUE_RAW_ABSORPTION,
                QiDefenseType.Shield => 1.0f,
                _ => 0f
            };
        }
    }
}
