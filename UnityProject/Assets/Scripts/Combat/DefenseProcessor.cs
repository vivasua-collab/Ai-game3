// ============================================================================
// DefenseProcessor.cs — Обработка защиты (10-слойный пайплайн)
// Cultivation World Simulator
// Версия: 1.1 — Fix-02: System.Random→UnityEngine.Random, Clamp01 для шансов
// Создан: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-10 14:43:00 UTC
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
    /// FIX CMB-C06: System.Random → UnityEngine.Random (потокобезопасность Unity)
    /// FIX CMB-M06: Mathf.Clamp01 для DodgeChance, ParryChance, BlockChance
    /// </summary>
    public static class DefenseProcessor
    {
        // FIX CMB-C06: Убран System.Random — используем UnityEngine.Random.value
        
        /// <summary>
        /// Обработать урон через все слои защиты.
        /// </summary>
        public static DefenseResult ProcessDefense(float rawDamage, DefenseData defense)
        {
            DefenseResult result = new DefenseResult
            {
                HitPart = RollBodyPart()
            };
            
            float damage = rawDamage;
            
            // === СЛОЙ 4: Активная защита ===
            // FIX CMB-M06: Clamp01 для всех шансов
            float dodgeChance = Mathf.Clamp01(defense.DodgeChance);
            float parryChance = Mathf.Clamp01(defense.ParryChance);
            float blockChance = Mathf.Clamp01(defense.BlockChance);
            
            // Уклонение (полный промах)
            if (RollChance(dodgeChance))
            {
                result.WasDodged = true;
                result.FinalDamage = 0f;
                return result;
            }
            
            // Парирование (50% урона)
            if (RollChance(parryChance))
            {
                result.WasParried = true;
                damage *= 0.5f;
            }
            
            // Блок (30% урона)
            if (RollChance(blockChance))
            {
                result.WasBlocked = true;
                damage *= 0.3f;
            }
            
            // === СЛОЙ 6: Покрытие брони ===
            
            if (RollChance(Mathf.Clamp01(defense.ArmorCoverage)))
            {
                result.HitArmor = true;
                
                // === СЛОЙ 7: Снижение бронёй ===
                
                // Процентное снижение (кап 80%)
                float dr = Math.Min(GameConstants.MAX_DAMAGE_REDUCTION, defense.DamageReduction);
                damage *= (1f - dr);
                
                // Плоское вычитание (с учётом пробития)
                int effectiveArmor = Math.Max(0, defense.ArmorValue - defense.Penetration);
                damage = Math.Max(1f, damage - effectiveArmor * 0.5f);
            }
            
            // === СЛОЙ 8: Материал тела ===
            
            if (GameConstants.BodyMaterialReduction.TryGetValue(defense.BodyMaterial, out float materialReduction))
            {
                damage *= (1f - materialReduction);
            }
            
            result.FinalDamage = Math.Max(0f, damage);
            return result;
        }
        
        /// <summary>
        /// Рассчитать шанс уклонения.
        /// FIX CMB-M06: добавлен Clamp01 в результат
        /// </summary>
        public static float CalculateDodgeChance(int agility, float armorDodgePenalty = 0f)
        {
            // Базовый 5% + (AGI-10) × 0.5% - штраф брони
            float chance = 0.05f + (agility - 10) * 0.005f - armorDodgePenalty;
            return Mathf.Clamp01(Math.Max(0f, chance)); // FIX CMB-M06: Clamp01
        }
        
        /// <summary>
        /// Рассчитать шанс парирования.
        /// FIX CMB-M06: добавлен Clamp01 в результат
        /// </summary>
        public static float CalculateParryChance(int agility, float weaponParryBonus = 0f)
        {
            // Бонус оружия + (AGI-10) × 0.3%
            float chance = weaponParryBonus + (agility - 10) * 0.003f;
            return Mathf.Clamp01(Math.Max(0f, chance)); // FIX CMB-M06: Clamp01
        }
        
        /// <summary>
        /// Рассчитать шанс блока.
        /// FIX CMB-M06: добавлен Clamp01 в результат
        /// </summary>
        public static float CalculateBlockChance(int strength, float shieldBlockBonus = 0f)
        {
            // Бонус щита + (STR-10) × 0.2%
            float chance = shieldBlockBonus + (strength - 10) * 0.002f;
            return Mathf.Clamp01(Math.Max(0f, chance)); // FIX CMB-M06: Clamp01
        }
        
        /// <summary>
        /// Бросить часть тела (гуманоид).
        /// FIX CMB-C06: System.Random → UnityEngine.Random
        /// </summary>
        public static BodyPartType RollBodyPart()
        {
            float roll = UnityEngine.Random.value; // FIX CMB-C06: был System.Random
            float cumulative = 0f;
            
            foreach (var kvp in GameConstants.BodyPartHitChances)
            {
                cumulative += kvp.Value;
                if (roll < cumulative)
                {
                    return kvp.Key;
                }
            }
            
            return BodyPartType.Torso;
        }
        
        /// <summary>
        /// Применить мягкий кап (diminishing returns).
        /// </summary>
        public static float ApplySoftCap(float bonus, float cap, float decayRate)
        {
            if (cap == 0) return 0f;
            return cap * (1f - (float)Math.Exp(-bonus / (cap * decayRate)));
        }
        
        /// <summary>
        /// Проверить шанс.
        /// FIX CMB-C06: UnityEngine.Random вместо System.Random
        /// </summary>
        private static bool RollChance(float chance)
        {
            return UnityEngine.Random.value < chance; // FIX CMB-C06
        }
    }
}
