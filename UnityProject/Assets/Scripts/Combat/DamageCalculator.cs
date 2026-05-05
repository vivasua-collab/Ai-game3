// ============================================================================
// DamageCalculator.cs — Главный калькулятор урона
// Cultivation World Simulator
// Версия: 1.1 — Fix-02: Elemental interaction Variant A, AttackType.Normal, defenderElement
// Создан: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-10 14:43:00 UTC
// Редактировано: 2026-05-04 07:25:00 UTC — ФАЗА 7: Слой 1b (оружие), Слой 3b (формация)
// Редактировано: 2026-05-07 10:30:00 UTC — ФАЗА 2: Слой 1c (TechniqueDamageBonus)
// Редактировано: 2026-05-05 10:05:00 UTC
// Редактировано: 2026-05-05 10:10:00 UTC
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

        // FIX В-06: Слот оружия атакующего для износа
        public EquipmentSlot AttackerWeaponSlot;

        // FIX В-04: Последствия урона (Слой 10)
        public float BleedDamage;      // Кровотечение: урон за тик
        public int BleedDuration;      // Длительность кровотечения в тиках
        public bool IsInShock;         // Шок
        public float ShockPenalty;     // Штраф шока (0-1)
        public bool IsStunned;         // Оглушение
        public float StunDuration;     // Длительность оглушения в секундах

        // FIX С-04: Стихийный эффект (COMBAT_SYSTEM.md §«Эффекты стихий»)
        public ElementalEffect ElementalEffect;
    }
    
    /// <summary>
    /// Стихийный эффект.
    /// FIX С-04: Добавлен для реализации стихийных эффектов (COMBAT_SYSTEM.md §«Эффекты стихий»)
    /// </summary>
    [Serializable]
    public struct ElementalEffect
    {
        public ElementalEffectType type;
        public float damagePerTick;
        public float speedPenalty;
        public int duration;  // в тиках
    }

    /// <summary>
    /// Параметры атакующего.
    /// ФАЗА 7: Добавлено WeaponBonusDamage для слоя 1b.
    /// ФАЗА 2: Добавлено TechniqueDamageBonus для слоя 1c.
    /// FIX С-02: Добавлены WeaponDamage, StrBonusRatio, AgiBonusRatio для формулы урона оружия.
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

        // ФАЗА 7: Слой 1b — бонус урона оружия
        /// <summary>Бонусный урон от оружия (для CombatSubtype.MeleeWeapon)</summary>
        public float WeaponBonusDamage;

        // ФАЗА 2: Слой 1c — бонус урона техник от оружия
        /// <summary>Бонус к урону Ци-техник от оружия (%)</summary>
        public float TechniqueDamageBonus;

        // FIX С-02: Поля для полной формулы урона оружия (EQUIPMENT_SYSTEM.md §7.3)
        /// <summary>Урон оружия (базовый, без множителей грейда)</summary>
        public float WeaponDamage;
        /// <summary>Коэффициент вклада Силы в урон оружия (default 0.5)</summary>
        public float StrBonusRatio;
        /// <summary>Коэффициент вклада Ловкости в урон оружия (default 0.3)</summary>
        public float AgiBonusRatio;
    }
    
    /// <summary>
    /// Параметры защищающегося.
    /// FIX CMB-H05: добавлено DefenderElement для стихийных взаимодействий
    /// ФАЗА 7: Добавлено FormationBuffMultiplier для слоя 3b.
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
        
        // В-01: Эффективность парирования и блокирования из экипировки (0-1)
        /// <summary>Эффективность парирования (0-1). При успехе парирования: damage ×= (1 - BlockEffectiveness)</summary>
        public float BlockEffectiveness;
        /// <summary>Эффективность блокирования щитом (0-1). При успехе блока: damage ×= (1 - ShieldEffectiveness)</summary>
        public float ShieldEffectiveness;
        
        public BodyMaterial BodyMaterial;
        
        // FIX CMB-H05: Элемент защитника для стихийных взаимодействий
        public Element DefenderElement;

        // ФАЗА 7: Слой 3b — бафф формации
        /// <summary>
        /// Множитель баффа формации (1.0 = нет формации, > 1.0 = усиление).
        /// Источник: FormationSystem / FormationArrayEffect.
        /// Применяется как: finalDamage *= formationBuffMultiplier
        /// </summary>
        public float FormationBuffMultiplier;
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
            // СЛОЙ 1b: Бонус урона оружия (ФАЗА 7)
            // FIX С-02: Полная формула урона оружия по EQUIPMENT_SYSTEM.md §7.3
            // Для CombatSubtype.MeleeWeapon рассчитываем урон с учётом статов.
            // ========================================

            // FIX С-02: Полная формула урона оружия по EQUIPMENT_SYSTEM.md §7.3
            if (attacker.CombatSubtype == CombatSubtype.MeleeWeapon && attacker.WeaponDamage > 0)
            {
                float handDamage = 3f + (attacker.Strength - 10f) * 0.3f;
                float weaponDmg = attacker.WeaponDamage;
                float baseDamage = UnityEngine.Mathf.Max(handDamage, weaponDmg * 0.5f);

                float strBonus = attacker.Strength * attacker.StrBonusRatio;
                float agiBonus = attacker.Agility * attacker.AgiBonusRatio;
                float statScaling = (strBonus + agiBonus) / 100f;
                float bonusDamage = weaponDmg * statScaling;

                // Заменить плоский бонус на формульный расчёт
                damage = damage - attacker.WeaponBonusDamage + baseDamage + bonusDamage;
            }
            else if (attacker.CombatSubtype == CombatSubtype.MeleeWeapon && attacker.WeaponBonusDamage > 0)
            {
                // Fallback: если WeaponDamage не задан, используем старый плоский бонус
                damage += attacker.WeaponBonusDamage;
            }
            
            // ========================================
            // СЛОЙ 1c: Бонус урона техник от оружия (ФАЗА 2)
            // Для Ци-техник добавляем бонус от оружия (%).
            // Формула: damage *= (1 + techniqueDamageBonus)
            // Источник: EquipmentController / WeaponData.techniqueDamageBonus
            // ========================================
            
            if (attacker.IsQiTechnique && attacker.TechniqueDamageBonus > 0)
            {
                damage *= (1f + attacker.TechniqueDamageBonus);
            }
            
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

            // FIX С-04: Применить стихийный эффект при преимуществе элемента
            if (attacker.AttackElement != Element.Neutral && result.ElementMultiplier > 1.0f)
            {
                ApplyElementalEffect(attacker.AttackElement, damage, ref result);
            }

            // ========================================
            // СЛОЙ 3b: Бафф формации (ФАЗА 7)
            // Если защитник в формации, входящий урон умножается на formationBuffMultiplier.
            // Значение > 1.0 = формация усиливает защиту (снижает урон обратно).
            // Формула: damage /= formationBuffMultiplier
            // ========================================

            if (defender.FormationBuffMultiplier > 1.0f)
            {
                damage /= defender.FormationBuffMultiplier;
            }
            
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
                Penetration = attacker.Penetration,
                // В-01: Эффективность из экипировки (было захардкожено 0.5 / 0.7)
                BlockEffectiveness = defender.BlockEffectiveness,
                ShieldEffectiveness = defender.ShieldEffectiveness
            };
            
            // Проверяем уклонение
            if (RollChance(defenseData.DodgeChance))
            {
                result.WasDodged = true;
                result.FinalDamage = 0f;
                return result;
            }
            
            // Проверяем парирование
            // В-01: Множитель из экипировки (было захардкожено 0.5)
            if (RollChance(defenseData.ParryChance))
            {
                result.WasParried = true;
                // Парирование: успех → damage ×= (1 - blockEffectiveness)
                damage *= (1f - defenseData.BlockEffectiveness);
            }
            
            // Проверяем блок
            // В-01: Множитель из экипировки (было захардкожено 0.3)
            if (RollChance(defenseData.BlockChance))
            {
                result.WasBlocked = true;
                // Блок щитом: успех → damage ×= (1 - shieldEffectiveness)
                damage *= (1f - defenseData.ShieldEffectiveness);
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
            
            // FIX К-05: Poison — НЕ стихия атаки, не имеет стихийных взаимодействий
            // Источник: ALGORITHMS.md §10.1 «Poison — НЕ стихия, а состояние Ци»
            if (attacker == Element.Poison)
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
            
            // FIX К-04: Light ↔ Void: ×1.5 (двусторонняя противоположность)
            // Void→Light уже обрабатывается через OppositeElements.
            // Light→Void тоже, но Light→Poison нужно добавить отдельно:
            if (attacker == Element.Light && defender == Element.Poison)
                return GameConstants.FIRE_TO_POISON_MULTIPLIER; // ×1.2 (очищение)
            
            // Fire → Poison: ×1.2 (выжигание токсинов, одностороннее)
            if (attacker == Element.Fire && defender == Element.Poison)
                return GameConstants.FIRE_TO_POISON_MULTIPLIER;
            
            // Void → All (кроме противоположного Lightning и Light): ×1.2
            if (attacker == Element.Void && defender != Element.Lightning && defender != Element.Light)
                return GameConstants.VOID_ELEMENT_MULTIPLIER;
            
            return 1.0f;
        }
        
        /// <summary>
        /// Применить стихийный эффект на основе элемента атаки.
        /// FIX С-04: Реализация стихийных эффектов (COMBAT_SYSTEM.md §«Эффекты стихий»)
        /// </summary>
        private static void ApplyElementalEffect(Element element, float damage, ref DamageResult result)
        {
            switch (element)
            {
                case Element.Fire:
                    // Горение: DoT 5%/тик, 3 тика
                    result.ElementalEffect = new ElementalEffect
                    {
                        type = ElementalEffectType.Burn,
                        damagePerTick = damage * 0.05f,
                        duration = 3
                    };
                    break;
                case Element.Water:
                    // Замедление: -20% скорости, 2 тика
                    result.ElementalEffect = new ElementalEffect
                    {
                        type = ElementalEffectType.Slow,
                        speedPenalty = 0.2f,
                        duration = 2
                    };
                    break;
                case Element.Earth:
                    // Оглушение: 15% шанс
                    if (RollChance(0.15f))
                    {
                        result.IsStunned = true;
                        result.StunDuration = 1.0f;
                    }
                    break;
                case Element.Air:
                    // Отталкивание
                    result.ElementalEffect = new ElementalEffect
                    {
                        type = ElementalEffectType.Knockback,
                        duration = 1
                    };
                    break;
                case Element.Lightning:
                    // Цепной урон (пока заглушка)
                    result.ElementalEffect = new ElementalEffect
                    {
                        type = ElementalEffectType.Chain,
                        damagePerTick = damage * 0.3f,
                        duration = 1
                    };
                    break;
                case Element.Void:
                    // Пробитие: игнорирование части защиты
                    result.ElementalEffect = new ElementalEffect
                    {
                        type = ElementalEffectType.Pierce,
                        duration = 1
                    };
                    break;
                case Element.Light:
                    // Очищение: снимает дебаффы (пока заглушка)
                    result.ElementalEffect = new ElementalEffect
                    {
                        type = ElementalEffectType.Purify,
                        duration = 1
                    };
                    break;
                case Element.Poison:
                    // Отравление: DoT 2%/тик, 5 тиков
                    result.ElementalEffect = new ElementalEffect
                    {
                        type = ElementalEffectType.PoisonDot,
                        damagePerTick = damage * 0.02f,
                        duration = 5
                    };
                    break;
            }
        }
        
        private static bool RollChance(float chance)
        {
            return UnityEngine.Random.value < chance;
        }
    }
}
