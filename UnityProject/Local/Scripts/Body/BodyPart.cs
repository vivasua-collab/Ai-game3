// ============================================================================
// BodyPart.cs — Часть тела
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-03-31 08:46:09 UTC
// ============================================================================

using System;
using CultivationGame.Core;

namespace CultivationGame.Body
{
    /// <summary>
    /// Часть тела с Kenshi-style двойной HP.
    /// 
    /// Источник: BODY_SYSTEM.md "Система двойной HP (Kenshi-style)"
    /// 
    /// Концепция:
    /// - Функциональная HP (красная) — работоспособность части
    /// - Структурная HP (чёрная) — физическая целостность
    /// 
    /// Соотношение: Структурная HP = Функциональная HP × 2
    /// Исключение: Сердце имеет только функциональную HP
    /// </summary>
    [Serializable]
    public class BodyPart
    {
        // === Идентификация ===
        public BodyPartType PartType;
        public string CustomName;
        public bool IsVital;           // Жизненно важная (смерть при уничтожении)
        
        // === HP (Kenshi-style) ===
        public float MaxRedHP;         // Максимальная функциональная HP
        public float CurrentRedHP;     // Текущая функциональная HP
        
        public float MaxBlackHP;       // Максимальная структурная HP
        public float CurrentBlackHP;   // Текущая структурная HP
        
        // === Состояние ===
        public BodyPartState State;
        
        // === Модификаторы ===
        public float BaseHitChance;    // Базовый шанс попадания
        
        // === Конструктор ===
        
        /// <summary>
        /// Создать часть тела.
        /// </summary>
        /// <param name="partType">Тип части тела</param>
        /// <param name="maxRedHP">Максимальная функциональная HP</param>
        /// <param name="isVital">Жизненно важная часть (head, heart)</param>
        public BodyPart(BodyPartType partType, float maxRedHP, bool isVital = false)
        {
            PartType = partType;
            CustomName = partType.ToString();
            IsVital = isVital;
            
            MaxRedHP = maxRedHP;
            CurrentRedHP = maxRedHP;
            
            // Структурная HP = функциональная × 2
            // Источник: BODY_SYSTEM.md "Соотношение HP"
            // ⚠️ ВНИМАНИЕ: Для сердца должна быть только красная HP (blackHP = 0)
            // Текущая реализация устанавливает blackHP всегда, что не соответствует документации
            MaxBlackHP = maxRedHP * GameConstants.STRUCTURAL_HP_MULTIPLIER;
            CurrentBlackHP = MaxBlackHP;
            
            State = BodyPartState.Healthy;
            
            // Базовый шанс попадания из констант
            // Источник: ALGORITHMS.md §8 "Шансы попадания по частям тела"
            if (GameConstants.BodyPartHitChances.TryGetValue(partType, out float chance))
            {
                BaseHitChance = chance;
            }
            else
            {
                BaseHitChance = 0.1f; // 10% по умолчанию
            }
        }
        
        // === Методы ===
        
        /// <summary>
        /// Нанести урон.
        /// Урон распределяется: 70% красная HP, 30% чёрная HP.
        /// Источник: ALGORITHMS.md §9 "Расчёт телесного урона"
        /// </summary>
        public void TakeDamage(float redDamage, float blackDamage)
        {
            CurrentRedHP -= redDamage;
            CurrentBlackHP -= blackDamage;
            
            UpdateState();
        }
        
        /// <summary>
        /// Восстановить HP.
        /// Источник: BODY_SYSTEM.md "Регенерация"
        /// Порядок восстановления:
        /// 1. Сначала структурная HP
        /// 2. Затем функциональная HP
        /// </summary>
        public void Heal(float redHeal, float blackHeal = 0f)
        {
            CurrentRedHP = Math.Min(MaxRedHP, CurrentRedHP + redHeal);
            if (blackHeal > 0)
            {
                CurrentBlackHP = Math.Min(MaxBlackHP, CurrentBlackHP + blackHeal);
            }
            
            UpdateState();
        }
        
        /// <summary>
        /// Обновить состояние части тела.
        /// Источник: BODY_SYSTEM.md "Механика повреждения"
        /// 
        /// Состояния:
        /// - Healthy: оба типа HP полны
        /// - Bruised: функциональная HP снижена (аналог Damaged)
        /// - Wounded: функциональная HP < 30%
        /// - Disabled: функциональная HP = 0, часть парализована (аналог Crippled)
        /// - Severed: структурная HP = 0, часть отрублена
        /// </summary>
        public void UpdateState()
        {
            if (CurrentBlackHP <= 0)
            {
                State = BodyPartState.Severed;
                CurrentRedHP = 0;
                CurrentBlackHP = 0;
            }
            else if (CurrentRedHP <= 0)
            {
                State = BodyPartState.Disabled;
                CurrentRedHP = 0;
            }
            else if (CurrentRedHP < MaxRedHP * 0.3f)
            {
                State = BodyPartState.Wounded;
            }
            else if (CurrentRedHP < MaxRedHP * 0.7f)
            {
                State = BodyPartState.Bruised;
            }
            else
            {
                State = BodyPartState.Healthy;
            }
        }
        
        /// <summary>
        /// Проверить, работает ли часть.
        /// Работоспособные состояния: Healthy, Bruised, Wounded
        /// </summary>
        public bool IsFunctional()
        {
            return State == BodyPartState.Healthy 
                || State == BodyPartState.Bruised 
                || State == BodyPartState.Wounded;
        }
        
        /// <summary>
        /// Проверить, отрублена ли часть.
        /// </summary>
        public bool IsSevered()
        {
            return State == BodyPartState.Severed;
        }
        
        /// <summary>
        /// Получить процент функционального HP.
        /// </summary>
        public float GetRedHPPercent()
        {
            return MaxRedHP > 0 ? CurrentRedHP / MaxRedHP : 0f;
        }
        
        /// <summary>
        /// Получить процент структурного HP.
        /// </summary>
        public float GetBlackHPPercent()
        {
            return MaxBlackHP > 0 ? CurrentBlackHP / MaxBlackHP : 0f;
        }
        
        /// <summary>
        /// Создать копию части тела.
        /// </summary>
        public BodyPart Clone()
        {
            return new BodyPart(PartType, MaxRedHP, IsVital)
            {
                CustomName = CustomName,
                CurrentRedHP = CurrentRedHP,
                CurrentBlackHP = CurrentBlackHP,
                State = State,
                BaseHitChance = BaseHitChance
            };
        }
    }
}
