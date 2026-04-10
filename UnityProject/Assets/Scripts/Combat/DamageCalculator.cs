// ============================================================================
// DamageCalculator.cs — Главный калькулятор урона
// Cultivation World Simulator
// Версия: 1.1 — Fix-02: Elemental interaction Variant A, AttackType.Normal, defenderElement
// Создан: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-10 14:43:00 UTC
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
        public long QiConsumed;             // FIX: long — Потраченное Ци
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
    /// FIX CMB-H05: добавлено DefenderElement для стихийных взаимодействий
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
        
        // FIX CMB-H05: Элемент защитника для стихийных взаимодействий
        public Element DefenderElement;
    }
    
    /// <summary>
    /// Главный калькулятор урона.
    /// Объединяет все системы: LevelSuppression, QiBuffer, DefenseProcessor.
    /// 
    /// Источник: ALGORITHMS.md §5 "Пайплайн урона (10 слоёв)"
    /// 
    /// FIX CMB-C01: CalculateElementalInteraction вызывается с defenderElement
    /// FIX CMB-C02: AttackType учитывает IsQiTechnique (Normal для физ. атак)
    /// FIX CMB-H05: defenderElement в DefenderParams
    /// </summary>
    public static class DamageCalculator
    {
        /// <summary>
        /// Полный расчёт урона по 10-слойному пайплайну.
        /// </summary>
        public static DamageResult CalculateDamage(
            int techniqueCapacity,
            AttackerParams attacker,
            DefenderParams defender)
        {
            DamageResult result = new DamageResult
            {
                // FIX CMB-C02: AttackType учитывает IsQiTechnique
                AttackType = attacker.IsUltimate ? AttackType.Ultimate
                    : attacker.IsQiTechnique ? AttackType.Technique
                    : AttackType.Normal,
                HitPart = BodyPartType.Torso
            };
            
            // ========================================
            // СЛОЙ 1: Исходный урон
            // ========================================
            
            result.RawDamage = TechniqueCapacity.CalculateDamage(
                techniqueCapacity,
                attacker.TechniqueGrade,
                attacker.IsUltimate
            );
            
            float damage = result.RawDamage;
            
            // ========================================
            // СЛОЙ 2: Level Suppression
            // ⚠️ Применяется ДО Qi Buffer!
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
                return result;
            }
            
            // ========================================
            // Элементальный множитель
            // FIX CMB-C01: Вызываем CalculateElementalInteraction с defenderElement
            // ========================================
            
            result.ElementMultiplier = CalculateElementalInteraction(attacker.AttackElement, defender.DefenderElement);
            damage *= result.ElementMultiplier;
            
            // ========================================
            // СЛОЙ 3: Определение части тела
            // ========================================
            
            result.HitPart = DefenseProcessor.RollBodyPart();
            
            // ========================================
            // СЛОЙ 4: Активная защита
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
                damage *= 0.5f;
            }
            
            // Проверяем блок
            if (RollChance(defenseData.BlockChance))
            {
                result.WasBlocked = true;
                damage *= 0.3f;
            }
            
            // ========================================
            // СЛОЙ 5: Qi Buffer
            // ========================================
            
            if (defender.QiDefense != QiDefenseType.None && defender.CurrentQi >= GameConstants.MIN_QI_FOR_BUFFER)
            {
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
            // ========================================
            
            if (RollChance(defender.ArmorCoverage))
            {
                result.HitArmor = true;
                
                float dr = Math.Min(GameConstants.MAX_DAMAGE_REDUCTION, defender.DamageReduction);
                damage *= (1f - dr);
                
                int effectiveArmor = Math.Max(0, defender.ArmorValue - attacker.Penetration);
                damage = Math.Max(1f, damage - effectiveArmor * 0.5f);
            }
            
            // ========================================
            // СЛОЙ 8: Материал тела
            // ========================================
            
            if (GameConstants.BodyMaterialReduction.TryGetValue(defender.BodyMaterial, out float matReduction))
            {
                damage *= (1f - matReduction);
            }
            
            // ========================================
            // СЛОЙ 9: Распределение по HP
            // ========================================
            
            result.FinalDamage = Math.Max(0f, damage);
            result.RedHPDamage = result.FinalDamage * GameConstants.RED_HP_RATIO;
            result.BlackHPDamage = result.FinalDamage * GameConstants.BLACK_HP_RATIO;
            
            // ========================================
            // СЛОЙ 10: Последствия
            // ========================================
            
            result.IsFatal = (result.HitPart == BodyPartType.Heart || result.HitPart == BodyPartType.Head) 
                          && result.FinalDamage > 50f;
            
            return result;
        }
        
        /// <summary>
        /// Рассчитать множитель для атаки элементом по элементу.
        /// 
        /// FIX CMB-C01: Полная реализация Variant A (решение пользователя)
        /// 
        /// Схема противоположностей (Variant A):
        /// - Fire ↔ Water: ×1.5 opposite, ×0.8 affinity
        /// - Earth ↔ Air: ×1.5 opposite, ×0.8 affinity
        /// - Lightning ↔ Void: ×1.5 opposite, ×0.8 affinity
        /// - Fire → Poison: ×1.2 (выжигание токсинов, одностороннее)
        /// - Void → All: ×1.2 (поглощение)
        /// - Neutral → All: ×1.0
        /// </summary>
        public static float CalculateElementalInteraction(Element attacker, Element defender)
        {
            // Neutral — без бонусов
            if (attacker == Element.Neutral)
                return 1.0f;
            
            // Проверяем противоположные элементы
            if (GameConstants.OppositeElements.TryGetValue(attacker, out Element opposite))
            {
                if (defender == opposite)
                    return GameConstants.OPPOSITE_ELEMENT_MULTIPLIER; // ×1.5
            }
            
            // Проверяем сродство (атакующий бьёт по своему элементу → слабее)
            if (GameConstants.OppositeElements.TryGetValue(defender, out Element defenderOpposite))
            {
                if (attacker == defenderOpposite)
                    return GameConstants.AFFINITY_ELEMENT_MULTIPLIER; // ×0.8
            }
            
            // Fire → Poison: ×1.2 (выжигание токсинов, одностороннее)
            if (attacker == Element.Fire && defender == Element.Poison)
                return GameConstants.FIRE_TO_POISON_MULTIPLIER;
            
            // Void → All (кроме противоположного Lightning): ×1.2
            if (attacker == Element.Void && defender != Element.Lightning)
                return GameConstants.VOID_ELEMENT_MULTIPLIER;
            
            return 1.0f;
        }
        
        private static bool RollChance(float chance)
        {
            return UnityEngine.Random.value < chance;
        }
    }
}
