// ============================================================================
// DefenseProcessor.cs — Обработка защиты (10-слойный пайплайн)
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-03-31 09:24:43 UTC
// ============================================================================

using System;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Результат обработки защиты.
    /// </summary>
    public struct DefenseResult
    {
        public bool WasDodged;          // Уклонение
        public bool WasParried;         // Парирование
        public bool WasBlocked;         // Блок
        public bool HitArmor;           // Попал в броню
        public float FinalDamage;       // Итоговый урон
        public BodyPartType HitPart;    // Попавшая часть тела
    }
    
    /// <summary>
    /// Данные о защитных параметрах.
    /// </summary>
    public struct DefenseData
    {
        public float DodgeChance;       // Шанс уклонения
        public float ParryChance;       // Шанс парирования
        public float BlockChance;       // Шанс блока
        
        public float ArmorCoverage;     // Покрытие брони (%)
        public float DamageReduction;   // Снижение урона (%)
        public int ArmorValue;          // Значение брони
        
        public BodyMaterial BodyMaterial; // Материал тела
        public int Penetration;         // Пробитие атакующего
    }
    
    /// <summary>
    /// Статический класс для обработки защиты по 10-слойному пайплайну.
    /// 
    /// Источник: ALGORITHMS.md §5 "Пайплайн урона (10 слоёв)"
    /// 
    /// Обрабатывает слои:
    /// - СЛОЙ 3: Определение части тела
    /// - СЛОЙ 4: Активная защита (dodge/parry/block)
    /// - СЛОЙ 6: Покрытие брони
    /// - СЛОЙ 7: Снижение бронёй
    /// - СЛОЙ 8: Материал тела
    /// 
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  ФОРМУЛЫ АКТИВНОЙ ЗАЩИТЫ (СЛОЙ 4)                                          ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║                                                                            ║
    /// ║  Уклонение:                                                                ║
    /// ║  dodgeChance = 5% + (AGI-10) × 0.5% - armorDodgePenalty                    ║
    /// ║  → Успех: damage = 0, END                                                  ║
    /// ║                                                                            ║
    /// ║  Парирование:                                                              ║
    /// ║  parryChance = weaponParryBonus + (AGI-10) × 0.3%                          ║
    /// ║  → Успех: damage ×= 0.5 (50% урона)                                        ║
    /// ║                                                                            ║
    /// ║  Блок щитом:                                                               ║
    /// ║  blockChance = shieldBlock + (STR-10) × 0.2%                               ║
    /// ║  → Успех: damage ×= 0.3 (30% урона)                                        ║
    /// ║                                                                            ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// </summary>
    public static class DefenseProcessor
    {
        private static readonly Random random = new Random();
        
        /// <summary>
        /// Обработать урон через все слои защиты.
        /// 
        /// Порядок слоёв:
        /// 1. СЛОЙ 4: Активная защита (dodge → parry → block)
        /// 2. СЛОЙ 6: Покрытие брони
        /// 3. СЛОЙ 7: Снижение бронёй
        /// 4. СЛОЙ 8: Материал тела
        /// </summary>
        public static DefenseResult ProcessDefense(float rawDamage, DefenseData defense)
        {
            DefenseResult result = new DefenseResult
            {
                HitPart = RollBodyPart()
            };
            
            float damage = rawDamage;
            
            // === СЛОЙ 4: Активная защита ===
            // Источник: ALGORITHMS.md §5.2 "Слой 4"
            
            // Уклонение (полный промах)
            if (RollChance(defense.DodgeChance))
            {
                result.WasDodged = true;
                result.FinalDamage = 0f;
                return result;
            }
            
            // Парирование (50% урона)
            if (RollChance(defense.ParryChance))
            {
                result.WasParried = true;
                damage *= 0.5f;
            }
            
            // Блок (30% урона)
            if (RollChance(defense.BlockChance))
            {
                result.WasBlocked = true;
                damage *= 0.3f;
            }
            
            // === СЛОЙ 6: Покрытие брони ===
            // Источник: ALGORITHMS.md §5.2 "Слой 6"
            
            if (RollChance(defense.ArmorCoverage))
            {
                result.HitArmor = true;
                
                // === СЛОЙ 7: Снижение бронёй ===
                // Источник: ALGORITHMS.md §5.2 "Слой 7"
                
                // Процентное снижение (кап 80%)
                float dr = Math.Min(GameConstants.MAX_DAMAGE_REDUCTION, defense.DamageReduction);
                damage *= (1f - dr);
                
                // Плоское вычитание (с учётом пробития)
                int effectiveArmor = Math.Max(0, defense.ArmorValue - defense.Penetration);
                damage = Math.Max(1f, damage - effectiveArmor * 0.5f);
            }
            
            // === СЛОЙ 8: Материал тела ===
            // Источник: ALGORITHMS.md §5.2 "Слой 8"
            // Источник: ENTITY_TYPES.md §5 "Материалы тела"
            
            if (GameConstants.BodyMaterialReduction.TryGetValue(defense.BodyMaterial, out float materialReduction))
            {
                damage *= (1f - materialReduction);
            }
            
            result.FinalDamage = Math.Max(0f, damage);
            return result;
        }
        
        /// <summary>
        /// Рассчитать шанс уклонения.
        /// 
        /// Источник: ALGORITHMS.md §5.2 "Слой 4"
        /// 
        /// Формула:
        /// dodgeChance = 5% + (AGI-10) × 0.5% - armorDodgePenalty
        /// 
        /// Примеры:
        /// - AGI=10, no armor: 5%
        /// - AGI=20, no armor: 5% + 5% = 10%
        /// - AGI=10, heavy armor (-10%): 5% - 10% = -5% → 0%
        /// </summary>
        public static float CalculateDodgeChance(int agility, float armorDodgePenalty = 0f)
        {
            // Базовый 5% + (AGI-10) × 0.5% - штраф брони
            return Math.Max(0f, 0.05f + (agility - 10) * 0.005f - armorDodgePenalty);
        }
        
        /// <summary>
        /// Рассчитать шанс парирования.
        /// 
        /// Источник: ALGORITHMS.md §5.2 "Слой 4"
        /// 
        /// Формула:
        /// parryChance = weaponParryBonus + (AGI-10) × 0.3%
        /// 
        /// Примеры:
        /// - AGI=10, weapon +5%: 5%
        /// - AGI=20, weapon +5%: 5% + 3% = 8%
        /// </summary>
        public static float CalculateParryChance(int agility, float weaponParryBonus = 0f)
        {
            // Бонус оружия + (AGI-10) × 0.3%
            return Math.Max(0f, weaponParryBonus + (agility - 10) * 0.003f);
        }
        
        /// <summary>
        /// Рассчитать шанс блока.
        /// 
        /// Источник: ALGORITHMS.md §5.2 "Слой 4"
        /// 
        /// Формула:
        /// blockChance = shieldBlock + (STR-10) × 0.2%
        /// 
        /// Примеры:
        /// - STR=10, shield +15%: 15%
        /// - STR=20, shield +15%: 15% + 2% = 17%
        /// </summary>
        public static float CalculateBlockChance(int strength, float shieldBlockBonus = 0f)
        {
            // Бонус щита + (STR-10) × 0.2%
            return Math.Max(0f, shieldBlockBonus + (strength - 10) * 0.002f);
        }
        
        /// <summary>
        /// Бросить часть тела (гуманоид).
        /// 
        /// Источник: ALGORITHMS.md §8 "Шансы попадания по частям тела"
        /// 
        /// Базовые шансы (гуманоид):
        /// | Часть тела | Шанс |
        /// |------------|------|
        /// | head       | 5%   |
        /// | torso      | 40%  |
        /// | heart      | 2%   |
        /// | left_arm   | 10%  |
        /// | right_arm  | 10%  |
        /// | left_leg   | 12%  |
        /// | right_leg  | 12%  |
        /// | left_hand  | 4%   |
        /// | right_hand | 4%   |
        /// | left_foot  | 0.5% |
        /// | right_foot | 0.5% |
        /// | ИТОГО      | 100% |
        /// </summary>
        public static BodyPartType RollBodyPart()
        {
            float roll = (float)random.NextDouble();
            float cumulative = 0f;
            
            foreach (var kvp in GameConstants.BodyPartHitChances)
            {
                cumulative += kvp.Value;
                if (roll < cumulative)
                {
                    return kvp.Key;
                }
            }
            
            return BodyPartType.Torso; // По умолчанию
        }
        
        /// <summary>
        /// Применить мягкий кап (diminishing returns).
        /// 
        /// Источник: ALGORITHMS.md §6 "Мягкие капы (Soft Caps)"
        /// 
        /// Формула:
        /// effectiveBonus = cap × (1 - e^(-bonus / (cap × decayRate)))
        /// 
        /// Пример (damage cap +100%, decay 1.0):
        /// - Бонус +50%:  effective = 100 × (1 - e^(-0.5)) = 39.3%
        /// - Бонус +100%: effective = 100 × (1 - e^(-1.0)) = 63.2%
        /// - Бонус +200%: effective = 100 × (1 - e^(-2.0)) = 86.5%
        /// </summary>
        public static float ApplySoftCap(float bonus, float cap, float decayRate)
        {
            if (cap == 0) return 0f;
            return cap * (1f - (float)Math.Exp(-bonus / (cap * decayRate)));
        }
        
        /// <summary>
        /// Проверить шанс.
        /// </summary>
        private static bool RollChance(float chance)
        {
            return random.NextDouble() < chance;
        }
    }
}
