// ============================================================================
// Combatant.cs — Интерфейс боевой сущности
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-31 14:08:00 UTC
// Редактировано: 2026-05-04 07:25:00 UTC — ФАЗА 7: WeaponBonusDamage + FormationBuffMultiplier
// Редактировано: 2026-05-07 10:00:00 UTC — ФАЗА 1: TODO обновлены (реализовано через EquipmentController)
// Редактировано: 2026-05-05 14:30:00 MSK — FIX: #pragma warning disable CS0618 для переходных Obsolete-свойств
// Редактировано: 2026-05-05 10:05:00 UTC
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Интерфейс для любой сущности, участвующей в бою.
    /// Реализуется Player, NPC, Monster и другими боевыми единицами.
    /// </summary>
    public interface ICombatant
    {
        // === Identity ===
        string Name { get; }
        GameObject GameObject { get; }
        
        // === Cultivation ===
        int CultivationLevel { get; }
        int CultivationSubLevel { get; }
        
        // === Stats ===
        int Strength { get; }
        int Agility { get; }
        int Intelligence { get; }
        int Vitality { get; }
        
        // === Qi ===
        long CurrentQi { get; }
        long MaxQi { get; }
        float QiDensity { get; }
        QiDefenseType QiDefense { get; }
        bool HasShieldTechnique { get; }
        
        // === Body ===
        BodyMaterial BodyMaterial { get; }
        float HealthPercent { get; }
        bool IsAlive { get; }
        
        // === Combat Stats ===
        int Penetration { get; }
        float DodgeChance { get; }
        float ParryChance { get; }
        float BlockChance { get; }
        
        // === Armor ===
        float ArmorCoverage { get; }
        float DamageReduction { get; }
        int ArmorValue { get; }
        
        // === Actions ===
        void TakeDamage(BodyPartType part, float damage);
        void TakeDamageRandom(float damage);
        bool SpendQi(long amount); // FIX: int→long для Qi > 2.1B
        void AddQi(long amount);
        
        // === Combat Parameters ===
        AttackerParams GetAttackerParams(Element attackElement = Element.Neutral);
        DefenderParams GetDefenderParams();
        
        // === Events ===
        event Action OnDeath;
        event Action<float> OnDamageTaken;
        event Action<long, long> OnQiChanged;
    }
    
    /// <summary>
    /// Расширенный интерфейс для Combatant с техниками.
    /// </summary>
    public interface ITechniqueUser : ICombatant
    {
        TechniqueController TechniqueController { get; }
        bool CanUseTechnique(LearnedTechnique technique);
        TechniqueUseResult UseTechnique(LearnedTechnique technique);
    }
    
    /// <summary>
    /// Базовый класс для боевых сущностей.
    /// Содержит общую логику для Player и NPC.
    /// </summary>
    public abstract class CombatantBase : MonoBehaviour, ICombatant
    {
        [Header("Identity")]
        [SerializeField] protected string combatantName = "Unknown";
        
        [Header("Stats")]
        [SerializeField] protected int strength = 10;
        [SerializeField] protected int agility = 10;
        [SerializeField] protected int intelligence = 10;
        [SerializeField] protected int vitality = 10;
        
        // === Cached Components ===
        protected Qi.QiController qiController;
        protected Body.BodyController bodyController;
        protected Combat.TechniqueController techniqueController;
        
        // === Events ===
        public event Action OnDeath;
        public event Action<float> OnDamageTaken;
        public event Action<long, long> OnQiChanged;
        
        // === ICombatant Implementation ===
        
        public virtual string Name => combatantName;
        public GameObject GameObject => gameObject;
        
        public virtual int CultivationLevel => qiController?.CultivationLevel ?? 1;
        public virtual int CultivationSubLevel => 0;
        
        public virtual int Strength => strength;
        public virtual int Agility => agility;
        public virtual int Intelligence => intelligence;
        public virtual int Vitality => vitality;
        
        public virtual long CurrentQi => qiController?.CurrentQi ?? 0;
        public virtual long MaxQi => qiController?.MaxQi ?? 0;
        public virtual float QiDensity => qiController?.QiDensity ?? 1f;
        
        public virtual QiDefenseType QiDefense
        {
            get
            {
                if (qiController == null) return QiDefenseType.None;
                return HasShieldTechnique ? QiDefenseType.Shield : QiDefenseType.RawQi;
            }
        }
        
        public virtual bool HasShieldTechnique => false;
        
        public virtual BodyMaterial BodyMaterial => bodyController?.BodyMaterial ?? BodyMaterial.Organic;
        public virtual float HealthPercent => bodyController?.HealthPercent ?? 0f;
        public virtual bool IsAlive => bodyController?.IsAlive ?? false;
        
        public virtual int Penetration => 0;
        
        [Obsolete("Используйте EquipmentController через ICombatant")]
        public virtual float DodgeChance
        {
            get
            {
                float penalty = 0f; // ФАЗА 1: реализовано через EquipmentController в PlayerController/NPCController
                return DefenseProcessor.CalculateDodgeChance(agility, penalty);
            }
        }
        
        [Obsolete("Используйте EquipmentController через ICombatant")]
        public virtual float ParryChance
        {
            get
            {
                float bonus = 0f; // ФАЗА 1: реализовано через EquipmentController в PlayerController/NPCController
                return DefenseProcessor.CalculateParryChance(agility, bonus);
            }
        }
        
        [Obsolete("Используйте EquipmentController через ICombatant")]
        public virtual float BlockChance
        {
            get
            {
                float bonus = 0f; // ФАЗА 1: реализовано через EquipmentController в PlayerController/NPCController
                return DefenseProcessor.CalculateBlockChance(strength, bonus);
            }
        }
        
        [Obsolete("Используйте EquipmentController через ICombatant")]
        public virtual float ArmorCoverage => 0f;
        [Obsolete("Используйте EquipmentController через ICombatant")]
        public virtual float DamageReduction => 0f;
        [Obsolete("Используйте EquipmentController через ICombatant")]
        public virtual int ArmorValue => 0;
        
        // FIX CMB-C03: Сырые бонусы экипировки (не финальные шансы!)
        // Финальные шансы считаются в DamageCalculator через DefenseProcessor
        // ФАЗА 1: реализовано через EquipmentController в PlayerController/NPCController
        [Obsolete("Используйте EquipmentController через ICombatant")]
        public virtual float WeaponParryBonus => 0f;  // EquipmentController.GetParryBonus()
        [Obsolete("Используйте EquipmentController через ICombatant")]
        public virtual float ShieldBlockBonus => 0f;    // EquipmentController.GetBlockBonus()

        // ФАЗА 1: реализовано через EquipmentController.GetWeaponBonusDamage() в PlayerController/NPCController
        /// <summary>Бонусный урон от оружия (из EquipmentController)</summary>
        [Obsolete("Используйте EquipmentController через ICombatant")]
        public virtual float WeaponBonusDamage => 0f;

        // ФАЗА 7: Слой 3b — множитель баффа формации
        /// <summary>Множитель баффа формации (1.0 = нет, > 1.0 = усиление защиты)</summary>
        public virtual float FormationBuffMultiplier => 1.0f;
        
        // FIX CMB-H05: Элемент защитника для стихийных взаимодействий
        public virtual Element DefenderElement => Element.Neutral;
        
        // === Unity Lifecycle ===
        
        protected virtual void Awake()
        {
            CacheComponents();
        }
        
        protected virtual void CacheComponents()
        {
            qiController = GetComponent<Qi.QiController>();
            bodyController = GetComponent<Body.BodyController>();
            techniqueController = GetComponent<TechniqueController>();
            
            // Subscribe to events
            if (bodyController != null)
            {
                bodyController.OnDeath += HandleDeath;
            }
            
            // FIX: Замена lambda на named method — предотвращает утечку памяти (CMB-C08)
            if (qiController != null)
            {
                qiController.OnQiChanged += HandleQiChanged;
            }
        }
        
        protected virtual void OnDestroy()
        {
            if (bodyController != null)
            {
                bodyController.OnDeath -= HandleDeath;
            }
            
            // FIX: Отписка от QiController — предотвращает утечку (CMB-C08)
            if (qiController != null)
            {
                qiController.OnQiChanged -= HandleQiChanged;
            }
        }
        
        // === Actions ===
        
        public virtual void TakeDamage(BodyPartType part, float damage)
        {
            if (bodyController != null)
            {
                bodyController.TakeDamage(part, damage);
                OnDamageTaken?.Invoke(damage);
            }
        }
        
        public virtual void TakeDamageRandom(float damage)
        {
            if (bodyController != null)
            {
                bodyController.TakeDamageRandom(damage);
                OnDamageTaken?.Invoke(damage);
            }
        }
        
        public virtual bool SpendQi(long amount) // FIX: int→long
        {
            return qiController?.SpendQi(amount) ?? false;
        }
        
        public virtual void AddQi(long amount)
        {
            qiController?.AddQi(amount);
        }
        
        // === Event Handlers ===
        
        protected virtual void HandleDeath()
        {
            OnDeath?.Invoke();
        }
        
        /// <summary>
        /// FIX: Named method для QiController.OnQiChanged — позволяет отписаться
        /// </summary>
        private void HandleQiChanged(long current, long max)
        {
            OnQiChanged?.Invoke(current, max);
        }
        
        // === Utility ===
        
        /// <summary>
        /// Получить параметры атакующего для DamageCalculator.
        /// ФАЗА 7: Добавлено WeaponBonusDamage для слоя 1b.
        /// FIX С-02: Добавлены WeaponDamage, StrBonusRatio, AgiBonusRatio.
        /// </summary>
        public AttackerParams GetAttackerParams(Element attackElement = Element.Neutral)
        {
            return new AttackerParams
            {
                CultivationLevel = CultivationLevel,
                Strength = Strength,
                Agility = Agility,
                Intelligence = Intelligence,
                Penetration = Penetration,
                AttackElement = attackElement,
                CombatSubtype = CombatSubtype.MeleeStrike,
                TechniqueLevel = 1,
                TechniqueGrade = TechniqueGrade.Common,
                IsUltimate = false,
                IsQiTechnique = false,
                #pragma warning disable CS0618 // CombatantBase: переходный период до подключения EquipmentController
                WeaponBonusDamage = WeaponBonusDamage,  // ФАЗА 7: слой 1b
                #pragma warning restore CS0618
                // FIX С-02: Поля для формулы урона оружия (по умолчанию 0 — fallback на WeaponBonusDamage)
                WeaponDamage = 0f,
                StrBonusRatio = 0.5f,
                AgiBonusRatio = 0.3f
            };
        }
        
        /// <summary>
        /// Получить параметры защищающегося для DamageCalculator.
        /// FIX CMB-C03: Передаём СЫРЫЕ бонусы экипировки, не финальные шансы.
        /// DamageCalculator сам вызовет CalculateParryChance/CalculateBlockChance.
        /// ФАЗА 7: Добавлено FormationBuffMultiplier для слоя 3b.
        /// </summary>
        public DefenderParams GetDefenderParams()
        {
            return new DefenderParams
            {
                CultivationLevel = CultivationLevel,
                CurrentQi = CurrentQi,
                QiDefense = QiDefense,
                Agility = Agility,
                Strength = Strength,
                #pragma warning disable CS0618 // CombatantBase: переходный период до подключения EquipmentController
                ArmorCoverage = ArmorCoverage,
                DamageReduction = DamageReduction,
                ArmorValue = ArmorValue,
                #pragma warning restore CS0618
                DodgePenalty = 0f, // ФАЗА 1: реализовано через EquipmentController в PlayerController/NPCController
                #pragma warning disable CS0618
                ParryBonus = WeaponParryBonus,   // FIX CMB-C03: сырой бонус оружия
                BlockBonus = ShieldBlockBonus,    // FIX CMB-C03: сырой бонус щита
                #pragma warning restore CS0618
                // В-01: Эффективность парирования/блока (по умолчанию как было захардкожено)
                BlockEffectiveness = 0.5f,
                ShieldEffectiveness = 0.7f,
                BodyMaterial = BodyMaterial,
                DefenderElement = DefenderElement,  // FIX CMB-H05
                FormationBuffMultiplier = FormationBuffMultiplier  // ФАЗА 7: слой 3b
            };
        }
    }
}
