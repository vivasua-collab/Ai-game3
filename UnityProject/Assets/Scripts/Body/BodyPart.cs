// ============================================================================
// BodyPart.cs — Часть тела
// Cultivation World Simulator
// Версия: 1.1 — Улучшена инкапсуляция
// ============================================================================
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-02 14:45:00 UTC
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 1.1:
// - Поля сделаны private с public readonly properties
// - Добавлен метод ApplyDamage() для внешнего использования
// - Добавлена проверка на отрубленную часть в TakeDamage
// - Улучшена документация методов
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
    /// 
    /// ИНКАПСУЛЯЦИЯ:
    /// - Все поля private, доступ через properties (read-only)
    /// - Изменение состояния ТОЛЬКО через методы TakeDamage/Heal
    /// - Внешние системы не могут напрямую манипулировать HP
    /// </summary>
    [Serializable]
    public class BodyPart
    {
        // === Идентификация ===
        private BodyPartType partType;
        private string customName;
        private bool isVital;           // Жизненно важная (смерть при уничтожении)
        
        // === HP (Kenshi-style) ===
        private float maxRedHP;         // Максимальная функциональная HP
        private float currentRedHP;     // Текущая функциональная HP
        
        private float maxBlackHP;       // Максимальная структурная HP
        private float currentBlackHP;   // Текущая структурная HP
        
        // === Состояние ===
        private BodyPartState state;
        
        // === Модификаторы ===
        private float baseHitChance;    // Базовый шанс попадания
        
        // === Public Properties (Read-Only) ===
        
        public BodyPartType PartType => partType;
        public string CustomName => customName;
        public bool IsVital => isVital;
        
        public float MaxRedHP => maxRedHP;
        public float CurrentRedHP => currentRedHP;
        public float MaxBlackHP => maxBlackHP;
        public float CurrentBlackHP => currentBlackHP;
        
        public BodyPartState State => state;
        public float BaseHitChance => baseHitChance;
        
        // === Конструктор ===
        
        /// <summary>
        /// Создать часть тела.
        /// </summary>
        /// <param name="partType">Тип части тела</param>
        /// <param name="maxRedHP">Максимальная функциональная HP</param>
        /// <param name="isVital">Жизненно важная часть (head, heart)</param>
        public BodyPart(BodyPartType partType, float maxRedHP, bool isVital = false)
        {
            this.partType = partType;
            this.customName = partType.ToString();
            this.isVital = isVital;
            
            this.maxRedHP = maxRedHP;
            this.currentRedHP = maxRedHP;
            
            // Структурная HP = функциональная × 2
            // Источник: BODY_SYSTEM.md "Соотношение HP"
            // FIX CORE-C01: Сердце имеет ТОЛЬКО функциональную HP (blackHP = 0)
            if (partType == BodyPartType.Heart)
            {
                this.maxBlackHP = 0f;
                this.currentBlackHP = 0f;
            }
            else
            {
                this.maxBlackHP = maxRedHP * GameConstants.STRUCTURAL_HP_MULTIPLIER;
                this.currentBlackHP = this.maxBlackHP;
            }
            
            this.state = BodyPartState.Healthy;
            
            // Базовый шанс попадания из констант
            // Источник: ALGORITHMS.md §8 "Шансы попадания по частям тела"
            if (GameConstants.BodyPartHitChances.TryGetValue(partType, out float chance))
            {
                this.baseHitChance = chance;
            }
            else
            {
                this.baseHitChance = 0.1f; // 10% по умолчанию
            }
        }
        
        // === Методы ===
        
        /// <summary>
        /// Нанести урон части тела.
        /// Урон распределяется: 70% красная HP, 30% чёрная HP.
        /// Источник: ALGORITHMS.md §9 "Расчёт телесного урона"
        /// 
        /// ВАЖНО: Если часть уже отрублена (Severed), урон не применяется.
        /// </summary>
        /// <param name="redDamage">Урон по функциональной HP</param>
        /// <param name="blackDamage">Урон по структурной HP</param>
        /// <returns>True если урон был применён, false если часть отрублена</returns>
        public bool TakeDamage(float redDamage, float blackDamage)
        {
            // Проверка на отрубленную часть
            if (state == BodyPartState.Severed)
            {
                return false;
            }
            
            currentRedHP -= redDamage;
            currentBlackHP -= blackDamage;
            
            UpdateState();
            return true;
        }
        
        /// <summary>
        /// Применить общий урон с автоматическим распределением.
        /// Распределение: 70% в красную HP, 30% в чёрную HP.
        /// </summary>
        /// <param name="totalDamage">Общий урон</param>
        /// <returns>True если урон был применён</returns>
        public bool ApplyDamage(float totalDamage)
        {
            float redDamage = totalDamage * 0.7f;
            float blackDamage = totalDamage * 0.3f;
            return TakeDamage(redDamage, blackDamage);
        }
        
        /// <summary>
        /// Восстановить HP.
        /// Источник: BODY_SYSTEM.md "Регенерация"
        /// Порядок восстановления:
        /// 1. Сначала структурная HP
        /// 2. Затем функциональная HP
        /// 
        /// ВАЖНО: Невозможно вылечить отрубленную часть!
        /// </summary>
        /// <param name="redHeal">Восстановление функциональной HP</param>
        /// <param name="blackHeal">Восстановление структурной HP (опционально)</param>
        /// <returns>True если лечение было применено, false если часть отрублена</returns>
        public bool Heal(float redHeal, float blackHeal = 0f)
        {
            // Нельзя вылечить отрубленную часть
            if (state == BodyPartState.Severed)
            {
                return false;
            }
            
            currentRedHP = Math.Min(maxRedHP, currentRedHP + redHeal);
            if (blackHeal > 0)
            {
                currentBlackHP = Math.Min(maxBlackHP, currentBlackHP + blackHeal);
            }
            
            UpdateState();
            return true;
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
            if (currentBlackHP <= 0)
            {
                state = BodyPartState.Severed;
                currentRedHP = 0;
                currentBlackHP = 0;
            }
            else if (currentRedHP <= 0)
            {
                state = BodyPartState.Disabled;
                currentRedHP = 0;
            }
            else if (currentRedHP < maxRedHP * 0.3f)
            {
                state = BodyPartState.Wounded;
            }
            else if (currentRedHP < maxRedHP * 0.7f)
            {
                state = BodyPartState.Bruised;
            }
            else
            {
                state = BodyPartState.Healthy;
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
            var clone = new BodyPart(partType, maxRedHP, isVital);
            clone.customName = this.customName;
            clone.currentRedHP = this.currentRedHP;
            clone.currentBlackHP = this.currentBlackHP;
            clone.state = this.state;
            clone.baseHitChance = this.baseHitChance;
            return clone;
        }
        
        /// <summary>
        /// Проверить, парализована ли часть.
        /// </summary>
        public bool IsDisabled()
        {
            return state == BodyPartState.Disabled;
        }
        
        // === Internal Setters (для BodyController и SaveSystem) ===
        
        /// <summary>
        /// Установить CustomName. Используется при инициализации.
        /// </summary>
        internal void SetCustomName(string name)
        {
            customName = name;
        }
        
        /// <summary>
        /// Добавить модификатор к BaseHitChance.
        /// </summary>
        internal void AddHitChanceModifier(float modifier)
        {
            baseHitChance += modifier;
        }
        
        /// <summary>
        /// Принудительно установить HP (для загрузки сохранений).
        /// </summary>
        internal void SetHP(float red, float black)
        {
            currentRedHP = red;
            currentBlackHP = black;
            UpdateState();
        }
    }
}
