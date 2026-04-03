// ============================================================================
// BodyController.cs — Контроллер тела
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-30 10:00:00 UTC
// Редактирован: 2026-03-31 08:46:09 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Body
{
    /// <summary>
    /// Контроллер тела существа.
    /// Управляет частями тела, повреждениями и регенерацией.
    /// 
    /// Архитектура соответствует ENTITY_TYPES.md:
    /// - SoulType (L1) → Morphology (L2) → Species (L3)
    /// - BodyMaterial определяет снижение урона
    /// </summary>
    public class BodyController : MonoBehaviour
    {
        [Header("Species")]
        [SerializeField] private Data.ScriptableObjects.SpeciesData speciesData;
        
        [Header("Body Material")]
        [SerializeField] private BodyMaterial bodyMaterial = BodyMaterial.Organic;
        
        [Header("Stats")]
        [SerializeField] private int vitality = 10;
        [SerializeField] private int cultivationLevel = 1;
        
        [Header("Regeneration")]
        [SerializeField] private bool enableRegeneration = true;
        [SerializeField] private float regenRate = 1f; // HP/сек
        
        // === Runtime Data ===
        
        private List<BodyPart> bodyParts = new List<BodyPart>();
        private Morphology morphology = Morphology.Humanoid;
        private SoulType soulType = SoulType.Character;
        
        // === Events ===
        
        public event Action<BodyPart, BodyDamageResult> OnDamageTaken;
        public event Action<BodyPart> OnPartSevered;
        public event Action OnDeath;
        
        // === Properties ===
        
        public List<BodyPart> BodyParts => bodyParts;
        public BodyMaterial BodyMaterial => bodyMaterial;
        public Morphology Morphology => morphology;
        public SoulType SoulType => soulType;
        
        public bool IsAlive => BodyDamage.IsAlive(bodyParts);
        public float HealthPercent => BodyDamage.GetOverallHealthPercent(bodyParts);
        public float DamagePenalty => BodyDamage.CalculateDamagePenalty(bodyParts);
        
        // === Unity Lifecycle ===
        
        private void Awake()
        {
            InitializeBody();
        }
        
        private void Update()
        {
            if (enableRegeneration && IsAlive)
            {
                ProcessRegeneration();
            }
        }
        
        // === Initialization ===
        
        /// <summary>
        /// Инициализировать тело на основе данных вида.
        /// Источник: ENTITY_TYPES.md §6 "SoulEntity"
        /// </summary>
        public void InitializeBody()
        {
            if (speciesData != null)
            {
                InitializeFromSpecies(speciesData);
            }
            else
            {
                InitializeDefaultHumanoid();
            }
        }
        
        /// <summary>
        /// Инициализировать из данных вида.
        /// Использует трёхуровневую классификацию:
        /// - SoulType (L1) → природа сущности
        /// - Morphology (L2) → форма тела
        /// - Species (L3) → конкретный вид
        /// </summary>
        public void InitializeFromSpecies(Data.ScriptableObjects.SpeciesData data)
        {
            soulType = data.soulType;
            morphology = data.morphology;
            bodyMaterial = data.bodyMaterial;
            vitality = (int)data.vitality.max;
            
            // Создаём части тела на основе конфигурации
            bodyParts.Clear();
            
            if (data.bodyParts != null && data.bodyParts.Count > 0)
            {
                foreach (var config in data.bodyParts)
                {
                    var part = new BodyPart(config.partType, config.baseHp, config.isVital);
                    if (!string.IsNullOrEmpty(config.customName))
                        part.CustomName = config.customName;
                    part.BaseHitChance += config.hitChanceModifier;
                    bodyParts.Add(part);
                }
            }
            else
            {
                // По умолчанию — гуманоид
                bodyParts = BodyDamage.CreateHumanoidBody(vitality);
            }
        }
        
        /// <summary>
        /// Инициализировать стандартное гуманоидное тело.
        /// Источник: BODY_SYSTEM.md "Части тела гуманоида"
        /// </summary>
        public void InitializeDefaultHumanoid()
        {
            bodyParts = BodyDamage.CreateHumanoidBody(vitality);
            morphology = Morphology.Humanoid;
            soulType = SoulType.Character;
        }
        
        // === Damage ===
        
        /// <summary>
        /// Нанести урон указанной части тела.
        /// Источник: ALGORITHMS.md §5 "Пайплайн урона"
        /// </summary>
        public BodyDamageResult TakeDamage(BodyPartType partType, float damage)
        {
            BodyPart part = GetPart(partType);
            if (part == null)
            {
                // Если часть не найдена, перенаправляем в торс
                part = GetPart(BodyPartType.Torso);
            }
            
            return TakeDamage(part, damage);
        }
        
        /// <summary>
        /// Нанести урон конкретной части тела.
        /// </summary>
        public BodyDamageResult TakeDamage(BodyPart part, float damage)
        {
            if (part == null)
                return new BodyDamageResult();
            
            var result = BodyDamage.ApplyDamage(part, damage);
            
            // События
            OnDamageTaken?.Invoke(part, result);
            
            if (result.WasSevered)
            {
                OnPartSevered?.Invoke(part);
            }
            
            if (result.IsFatal)
            {
                OnDeath?.Invoke();
            }
            
            return result;
        }
        
        /// <summary>
        /// Нанести урон в случайную часть (по таблице шансов).
        /// Источник: ALGORITHMS.md §8 "Шансы попадания по частям тела"
        /// </summary>
        public BodyDamageResult TakeDamageRandom(float damage)
        {
            // Бросаем часть тела через DefenseProcessor
            BodyPartType hitPart = Combat.DefenseProcessor.RollBodyPart();
            return TakeDamage(hitPart, damage);
        }
        
        // === Healing ===
        
        /// <summary>
        /// Вылечить указанную часть тела.
        /// </summary>
        public void HealPart(BodyPartType partType, float redHeal, float blackHeal = 0f)
        {
            BodyPart part = GetPart(partType);
            if (part != null && !part.IsSevered())
            {
                part.Heal(redHeal, blackHeal);
            }
        }
        
        /// <summary>
        /// Вылечить всё тело.
        /// </summary>
        public void HealAll(float redHeal, float blackHeal = 0f)
        {
            foreach (var part in bodyParts)
            {
                if (!part.IsSevered())
                {
                    part.Heal(redHeal, blackHeal);
                }
            }
        }
        
        /// <summary>
        /// Полное восстановление.
        /// </summary>
        public void FullRestore()
        {
            foreach (var part in bodyParts)
            {
                part.CurrentRedHP = part.MaxRedHP;
                part.CurrentBlackHP = part.MaxBlackHP;
                part.UpdateState();
            }
        }
        
        // === Regeneration ===
        
        /// <summary>
        /// Обработка регенерации.
        /// Источник: BODY_SYSTEM.md "Кровотечение и регенерация"
        /// Множители регенерации: Constants.RegenerationMultipliers
        /// </summary>
        private void ProcessRegeneration()
        {
            if (!IsAlive) return;
            
            // Множитель регенерации от уровня культивации
            float regenMultiplier = GetRegenMultiplier();
            float regenAmount = regenRate * regenMultiplier * Time.deltaTime;
            
            foreach (var part in bodyParts)
            {
                if (!part.IsSevered() && part.CurrentRedHP < part.MaxRedHP)
                {
                    // Восстановление: красная HP и 30% от этого в чёрную
                    part.Heal(regenAmount, regenAmount * 0.3f);
                }
            }
        }
        
        /// <summary>
        /// Получить множитель регенерации по уровню культивации.
        /// Источник: Constants.RegenerationMultipliers
        /// L1: 1.1x, L2: 2.0x, L3: 3.0x, L4: 5.0x, L5: 8.0x,
        /// L6: 15.0x, L7: 30.0x, L8: 100.0x, L9: 1000.0x, L10: ∞
        /// </summary>
        private float GetRegenMultiplier()
        {
            if (cultivationLevel >= 1 && cultivationLevel <= GameConstants.RegenerationMultipliers.Length)
            {
                return GameConstants.RegenerationMultipliers[cultivationLevel - 1];
            }
            return 1f;
        }
        
        // === Utility ===
        
        /// <summary>
        /// Получить часть тела по типу.
        /// </summary>
        public BodyPart GetPart(BodyPartType partType)
        {
            return bodyParts.Find(p => p.PartType == partType);
        }
        
        /// <summary>
        /// Получить все жизненно важные части.
        /// </summary>
        public List<BodyPart> GetVitalParts()
        {
            return bodyParts.FindAll(p => p.IsVital);
        }
        
        /// <summary>
        /// Получить все парализованные части.
        /// </summary>
        public List<BodyPart> GetDisabledParts()
        {
            return bodyParts.FindAll(p => p.State == BodyPartState.Disabled);
        }
        
        /// <summary>
        /// Получить все отрубленные части.
        /// </summary>
        public List<BodyPart> GetSeveredParts()
        {
            return bodyParts.FindAll(p => p.IsSevered());
        }
        
        /// <summary>
        /// Установить уровень культивации (влияет на регенерацию).
        /// </summary>
        public void SetCultivationLevel(int level)
        {
            cultivationLevel = Mathf.Clamp(level, 1, 10);
        }
    }
}
