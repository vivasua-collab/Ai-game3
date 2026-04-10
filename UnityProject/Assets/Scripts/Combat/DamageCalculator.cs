// ============================================================================
// DamageCalculator.cs — Главный калькулятор урона
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
    /// Результат полного расчёта урона.
    /// </summary>
    public struct DamageResult
    {
        // Входные данные
        public float RawDamage;             // Исходный урон (СЛОЙ 1)
        public AttackType AttackType;       // Тип атаки
        
        // Множители
        public float SuppressionMultiplier; // Подавление уровнем (СЛОЙ 2)
        public float ElementMultiplier;     // Элементальный множитель
        
        // Защита
        public bool WasDodged;              // СЛОЙ 4: Уклонение
        public bool WasParried;             // СЛОЙ 4: Парирование
        public bool WasBlocked;             // СЛОЙ 4: Блок
        public bool QiAbsorbed;             // СЛОЙ 5: Qi Buffer
        public float QiAbsorbedAmount;
        public bool HitArmor;               // СЛОЙ 6: Попадание в броню
        
        // Итог
        public float FinalDamage;           // Итоговый урон
        public float RedHPDamage;           // СЛОЙ 9: Урон по красной HP
        public float BlackHPDamage;         // СЛОЙ 9: Урон по чёрной HP
        public BodyPartType HitPart;        // СЛОЙ 3: Поражённая часть
        
        // Дополнительно
        public int QiConsumed;              // Потраченное Ци
        public bool IsFatal;                // СЛОЙ 10: Смертельный удар?
    }
    
    /// <summary>
    /// Параметры атакующего.
    /// </summary>
    public struct AttackerParams
    {
        public int CultivationLevel;
        public int Strength;
        public int Agility;
        public int Intelligence;
        public int Penetration;
        public Element AttackElement;
        public CombatSubtype CombatSubtype;
        public int TechniqueLevel;
        public TechniqueGrade TechniqueGrade;
        public bool IsUltimate;
        public bool IsQiTechnique;          // TRUE = техника Ци, FALSE = физическая атака
    }
    
    /// <summary>
    /// Параметры защищающегося.
    /// </summary>
    public struct DefenderParams
    {
        public int CultivationLevel;
        public long CurrentQi; // FIX: int→long для Qi > 2.1B на L5+
        public QiDefenseType QiDefense;
        
        public int Agility;
        public int Strength;
        
        public float ArmorCoverage;
        public float DamageReduction;
        public int ArmorValue;
        public float DodgePenalty;
        public float ParryBonus;
        public float BlockBonus;
        
        public BodyMaterial BodyMaterial;
    }
    
    /// <summary>
    /// Главный калькулятор урона.
    /// Объединяет все системы: LevelSuppression, QiBuffer, DefenseProcessor.
    /// 
    /// Источник: ALGORITHMS.md §5 "Пайплайн урона (10 слоёв)"
    /// 
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  ПОРЯДОК ПРОХОЖДЕНИЯ УРОНА (10 СЛОЁВ)                                      ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║  СЛОЙ 1:  Исходный урон                                                    ║
    /// ║  СЛОЙ 2:  Level Suppression (подавление уровнем)                           ║
    /// ║  СЛОЙ 3:  Определение части тела                                           ║
    /// ║  СЛОЙ 4:  Активная защита (dodge/parry/block)                              ║
    /// ║  СЛОЙ 5:  Qi Buffer (поглощение Ци)                                        ║
    /// ║  СЛОЙ 6:  Покрытие брони                                                   ║
    /// ║  СЛОЙ 7:  Снижение бронёй                                                  ║
    /// ║  СЛОЙ 8:  Материал тела                                                    ║
    /// ║  СЛОЙ 9:  Распределение по HP (70% красная, 30% чёрная)                    ║
    /// ║  СЛОЙ 10: Последствия (кровотечение, шок, смерть)                          ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    /// 
    /// ⚠️ ВАЖНО: Level Suppression (СЛОЙ 2) должен применяться ДО Qi Buffer!
    /// 
    /// ⚠️ ВАЖНО: Qi Buffer различает:
    /// - Техники Ци (isQiTechnique=true): 90%/3:1/10%
    /// - Физический урон (isQiTechnique=false): 80%/5:1/20%
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Полный расчёт урона по 10-слойному пайплайну.
        /// 
        /// Источник: ALGORITHMS.md §5 "Пайплайн урона (10 слоёв)"
        /// </summary>
        public static DamageResult CalculateDamage(
            int techniqueCapacity,
            AttackerParams attacker,
            DefenderParams defender)
        {
            DamageResult result = new DamageResult
            {
                AttackType = attacker.IsUltimate ? AttackType.Ultimate : AttackType.Technique,
                HitPart = BodyPartType.Torso
            };
            
            // ========================================
            // СЛОЙ 1: Исходный урон
            // Источник: ALGORITHMS.md §5.2 "Слой 1"
            // ========================================
            
            result.RawDamage = TechniqueCapacity.CalculateDamage(
                techniqueCapacity,
                attacker.TechniqueGrade,
                attacker.IsUltimate
            );
            
            float damage = result.RawDamage;
            
            // ========================================
            // СЛОЙ 2: Level Suppression
            // Источник: ALGORITHMS.md §5.2 "Слой 2"
            // ⚠️ ВАЖНО: Применяется ДО Qi Buffer!
            // ========================================
            
            result.SuppressionMultiplier = LevelSuppression.CalculateSuppression(
                attacker.CultivationLevel,
                defender.CultivationLevel,
                result.AttackType,
                attacker.TechniqueLevel
            );
            
            damage *= result.SuppressionMultiplier;
            
            if (damage <= 0f)
            {
                result.FinalDamage = 0f;
                return result; // Урон невозможен из-за подавления
            }
            
            // ========================================
            // Элементальный множитель
            // (Не является отдельным слоем, применяется после подавления)
            // ========================================
            
            result.ElementMultiplier = CalculateElementMultiplier(attacker.AttackElement);
            damage *= result.ElementMultiplier;
            
            // ========================================
            // СЛОЙ 3: Определение части тела
            // Источник: ALGORITHMS.md §5.2 "Слой 3"
            // ========================================
            
            result.HitPart = DefenseProcessor.RollBodyPart();
            
            // ========================================
            // СЛОЙ 4: Активная защита
            // Источник: ALGORITHMS.md §5.2 "Слой 4"
            // Формулы:
            // - dodgeChance = 5% + (AGI-10) × 0.5% - armorDodgePenalty
            // - parryChance = weaponParryBonus + (AGI-10) × 0.3%
            // - blockChance = shieldBlock + (STR-10) × 0.2%
            // ========================================
            
            DefenseData defenseData = new DefenseData
            {
                DodgeChance = DefenseProcessor.CalculateDodgeChance(defender.Agility, defender.DodgePenalty),
                ParryChance = DefenseProcessor.CalculateParryChance(defender.Agility, defender.ParryBonus),
                BlockChance = DefenseProcessor.CalculateBlockChance(defender.Strength, defender.BlockBonus),
                ArmorCoverage = defender.ArmorCoverage,
                DamageReduction = defender.DamageReduction,
                ArmorValue = defender.ArmorValue,
                BodyMaterial = defender.BodyMaterial,
                Penetration = attacker.Penetration
            };
            
            // Проверяем уклонение
            if (RollChance(defenseData.DodgeChance))
            {
                result.WasDodged = true;
                result.FinalDamage = 0f;
                return result;
            }
            
            // Проверяем парирование
            if (RollChance(defenseData.ParryChance))
            {
                result.WasParried = true;
                damage *= 0.5f; // 50% урона при парировании
            }
            
            // Проверяем блок
            if (RollChance(defenseData.BlockChance))
            {
                result.WasBlocked = true;
                damage *= 0.3f; // 30% урона при блоке
            }
            
            // ========================================
            // СЛОЙ 5: Qi Buffer
            // Источник: ALGORITHMS.md §5.2 "Слой 5"
            // 
            // ⚠️ КРИТИЧНО: Различаем техники Ци и физический урон!
            // - Техники Ци: 90%/3:1/10% или 100%/1:1/0% (щит)
            // - Физический: 80%/5:1/20% или 100%/2:1/0% (щит)
            // ========================================
            
            if (defender.QiDefense != QiDefenseType.None && defender.CurrentQi >= GameConstants.MIN_QI_FOR_BUFFER)
            {
                // Выбираем правильный метод в зависимости от типа атаки
                QiBufferResult qiResult = attacker.IsQiTechnique
                    ? QiBuffer.ProcessQiTechniqueDamage(damage, defender.CurrentQi, defender.QiDefense)
                    : QiBuffer.ProcessPhysicalDamage(damage, defender.CurrentQi, defender.QiDefense);
                
                result.QiAbsorbed = true;
                result.QiAbsorbedAmount = qiResult.AbsorbedDamage;
                result.QiConsumed = qiResult.QiConsumed;
                damage = qiResult.PiercingDamage;
            }
            
            // ========================================
            // СЛОЙ 6-7: Броня (покрытие + снижение)
            // Источник: ALGORITHMS.md §5.2 "Слои 6-7"
            // ========================================
            
            if (RollChance(defender.ArmorCoverage))
            {
                result.HitArmor = true;
                
                // Снижение урона (кап 80%)
                float dr = Math.Min(GameConstants.MAX_DAMAGE_REDUCTION, defender.DamageReduction);
                damage *= (1f - dr);
                
                // Плоское вычитание (с учётом пробития)
                int effectiveArmor = Math.Max(0, defender.ArmorValue - attacker.Penetration);
                damage = Math.Max(1f, damage - effectiveArmor * 0.5f);
            }
            
            // ========================================
            // СЛОЙ 8: Материал тела
            // Источник: ALGORITHMS.md §5.2 "Слой 8"
            // Источник: ENTITY_TYPES.md §5 "Материалы тела"
            // ========================================
            
            if (GameConstants.BodyMaterialReduction.TryGetValue(defender.BodyMaterial, out float matReduction))
            {
                damage *= (1f - matReduction);
            }
            
            // ========================================
            // СЛОЙ 9: Распределение по HP
            // Источник: ALGORITHMS.md §5.2 "Слой 9"
            // Источник: ALGORITHMS.md §9 "Расчёт телесного урона"
            // 
            // redHP -= damage × 0.7  (функциональная)
            // blackHP -= damage × 0.3 (структурная)
            // ========================================
            
            result.FinalDamage = Math.Max(0f, damage);
            result.RedHPDamage = result.FinalDamage * GameConstants.RED_HP_RATIO;
            result.BlackHPDamage = result.FinalDamage * GameConstants.BLACK_HP_RATIO;
            
            // ========================================
            // СЛОЙ 10: Последствия
            // Источник: ALGORITHMS.md §5.2 "Слой 10"
            // - Кровотечение
            // - Шок
            // - Оглушение
            // - Смерть (heart HP ≤ 0 или head HP ≤ 0)
            // ========================================
            
            result.IsFatal = (result.HitPart == BodyPartType.Heart || result.HitPart == BodyPartType.Head) 
                          && result.FinalDamage > 50f;
            
            return result;
        }
        
        /// <summary>
        /// Рассчитать элементальный множитель.
        /// Источник: ALGORITHMS.md §10 "Система элементов"
        /// </summary>
        private static float CalculateElementMultiplier(Element element)
        {
            if (element == Element.Void)
                return GameConstants.VOID_ELEMENT_MULTIPLIER;
            if (element == Element.Neutral)
                return 1.0f;
            return 1.0f;
        }
        
        /// <summary>
        /// Рассчитать множитель для атаки элементом по элементу.
        /// Источник: ALGORITHMS.md §10.2 "Множители эффективности атаки"
        /// 
        /// - Противоположные элементы: ×1.5 урона
        /// - Сродство: ×0.8 урона
        /// - Void: ×1.2 по любому
        /// - Neutral: ×1.0
        /// </summary>
        public static float CalculateElementalInteraction(Element attacker, Element defender)
        {
            // Противоположные элементы
            if (GameConstants.OppositeElements.TryGetValue(attacker, out Element opposite))
            {
                if (defender == opposite)
                    return GameConstants.OPPOSITE_ELEMENT_MULTIPLIER;
            }
            
            // Void особый случай
            if (attacker == Element.Void)
                return GameConstants.VOID_ELEMENT_MULTIPLIER;
            
            return 1.0f;
        }
        
        private static bool RollChance(float chance)
        {
            return UnityEngine.Random.value < chance;
        }
    }
}
