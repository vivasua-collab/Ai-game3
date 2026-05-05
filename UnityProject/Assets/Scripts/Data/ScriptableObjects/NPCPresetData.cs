// ============================================================================
// NPCPresetData.cs — Пресет NPC
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:05:48 UTC
// Редактировано: 2026-05-05 09:55:00 UTC — К-12: baseDisposition→baseAttitude; В-11: personalityFlags; С-13: [Obsolete] Alignment и personalityTraits
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Пресет NPC — шаблон для генерации персонажей.
    /// </summary>
    [CreateAssetMenu(fileName = "NPCPreset", menuName = "Cultivation/NPC Preset")]
    public class NPCPresetData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Уникальный ID пресета")]
        public string presetId;
        
        [Tooltip("Имя (или шаблон имени)")]
        public string nameTemplate;
        
        [Tooltip("Титул")]
        public string title;
        
        [TextArea(2, 4)]
        [Tooltip("Предыстория")]
        public string backstory;
        
        [Header("Category")]
        [Tooltip("Категория NPC")]
        public NPCCategory category;

        [Tooltip("ID вида существа (для поиска SpeciesData)")]
        public string speciesId;

        [Tooltip("Вид существа (назначается вручную или по speciesId)")]
        public SpeciesData species;
        
        [Header("Cultivation")]
        [Tooltip("Уровень культивации")]
        [Range(1, 10)]
        public int cultivationLevel = 1;
        
        [Tooltip("Под-уровень культивации")]
        [Range(0, 9)]
        public int cultivationSubLevel = 0;
        
        [Tooltip("Ёмкость ядра")]
        public long coreCapacity = 1000;
        
        [Tooltip("Текущее Ци (в % от максимума)")]
        [Range(0f, 100f)]
        public float qiPercentage = 100f;
        
        [Header("Stats")]
        [Tooltip("Сила")]
        [Range(1, 100)]
        public int strength = 10;
        
        [Tooltip("Ловкость")]
        [Range(1, 100)]
        public int agility = 10;
        
        [Tooltip("Интеллект")]
        [Range(1, 100)]
        public int intelligence = 10;
        
        [Tooltip("Жизнеспособность")]
        [Range(1, 100)]
        public int vitality = 10;
        
        [Tooltip("Проводимость")]
        [Range(0.1f, 10f)]
        public float conductivity = 1.0f;
        
        [Header("Personality")]
        [Tooltip("Флаги характера (комбинируемые, [Flags])")]
        public PersonalityTrait personalityFlags = PersonalityTrait.None; // В-11: заменяет List<PersonalityTraitEntry>
        
        [Obsolete("Используйте personalityFlags (PersonalityTrait enum с [Flags])")]
        [Tooltip("Черты характера (устарело — используйте personalityFlags)")]
        public List<PersonalityTraitEntry> personalityTraits = new List<PersonalityTraitEntry>(); // FIX: PersonalityTrait→PersonalityTraitEntry (2026-04-12)
        
        [Tooltip("Мотивация")]
        [TextArea(1, 2)]
        public string motivation;
        
        [Obsolete("Мировоззрение устарело — используйте personalityFlags для характера и baseAttitude для отношения")]
        [Tooltip("Мировоззрение (устарело)")]
        public Alignment alignment;
        
        [Header("Relations")]
        [Tooltip("Базовое отношение к игроку (enum Attitude)")]
        public Attitude baseAttitude = Attitude.Neutral; // К-12: заменяет baseDisposition (int)
        
        [Obsolete("Используйте baseAttitude (Attitude enum) вместо baseDisposition (int)")]
        [Tooltip("Базовое расположение (устарело — используйте baseAttitude)")]
        [Range(-100, 100)]
        public int baseDisposition = 0;
        
        [Tooltip("Фракция")]
        public string factionId;
        
        [Tooltip("Роль во фракции")]
        public string factionRole;
        
        [Header("Techniques")]
        [Tooltip("Известные техники")]
        public List<KnownTechnique> knownTechniques = new List<KnownTechnique>();
        
        [Header("Equipment")]
        [Tooltip("Экипировка")]
        public List<EquippedItem> equipment = new List<EquippedItem>();
        
        [Tooltip("Инвентарь")]
        public List<InventoryItem> inventory = new List<InventoryItem>();
        
        [Header("AI")]
        [Tooltip("Тип поведения")]
        public BehaviorType behaviorType;
        
        [Tooltip("Агрессивность")]
        [Range(0f, 100f)]
        public float aggressiveness = 50f;
        
        [Tooltip("Смелость")]
        [Range(0f, 100f)]
        public float courage = 50f;
    }
    
    /// <summary>
    /// Черта характера (запись пресета)
    /// FIX: Renamed from PersonalityTrait to PersonalityTraitEntry to avoid CS0104 collision
    /// with CultivationGame.Core.PersonalityTrait enum (2026-04-12)
    /// </summary>
    [System.Serializable]
    public class PersonalityTraitEntry
    {
        public string traitName;
        public int intensity; // -10 до 10
    }
    
    /// <summary>
    /// Мировоззрение
    /// </summary>
    public enum Alignment
    {
        LawfulGood,
        NeutralGood,
        ChaoticGood,
        LawfulNeutral,
        TrueNeutral,
        ChaoticNeutral,
        LawfulEvil,
        NeutralEvil,
        ChaoticEvil
    }
    
    /// <summary>
    /// Известная техника
    /// </summary>
    [System.Serializable]
    public class KnownTechnique
    {
        public string techniqueId;
        [Range(0f, 100f)]
        public float mastery = 0f;
        public int quickSlot = -1;
    }
    
    /// <summary>
    /// Экипированный предмет
    /// </summary>
    [System.Serializable]
    public class EquippedItem
    {
        public EquipmentSlot slot;
        public string itemId;
        public EquipmentGrade grade;
        public int durabilityPercent = 100;
    }
    
    /// <summary>
    /// Предмет в инвентаре
    /// </summary>
    [System.Serializable]
    public class InventoryItem
    {
        public string itemId;
        public int quantity = 1;
    }
    
    /// <summary>
    /// Тип поведения AI
    /// </summary>
    public enum BehaviorType
    {
        Passive,        // Пассивный (не атакует)
        Defensive,      // Оборонительный (атакует при угрозе)
        Neutral,        // Нейтральный (реагирует на действия)
        Aggressive,     // Агрессивный (атакует врагов)
        Hostile,        // Враждебный (атакует игрока)
        Friendly        // Дружелюбный (помогает)
    }
}
