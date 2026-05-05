// ============================================================================
// NPCData.cs — Runtime данные NPC
// Cultivation World Simulator
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-11 06:38:02 UTC — NPC-C01/C02, NPC-ATT-01/04: Disposition→Attitude+PersonalityTrait, SkillLevelData сериализация
// Редактировано: 2026-05-07 11:30:00 UTC — ФАЗА 7: NPCEquipmentSaveData, NPCTechniqueSlotSaveData
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;
using CultivationGame.Qi;
using CultivationGame.Generators;  // Редактировано: 2026-05-01 — NPCRole enum

namespace CultivationGame.NPC
{
    // FIX NPC-C02: Serializable wrapper for SkillLevels dictionary
    // JsonUtility cannot serialize Dictionary<,>, so we use an array of entries.
    [Serializable]
    public struct SkillLevelEntry
    {
        public string skillId;
        public float level;

        public SkillLevelEntry(string id, float lvl)
        {
            skillId = id;
            level = lvl;
        }
    }

    /// <summary>
    /// FIX NPC-C02: Serializable container for skill levels.
    /// Converts to/from Dictionary for runtime, serializes as array for save.
    /// </summary>
    [Serializable]
    public class SkillLevelData
    {
        public SkillLevelEntry[] entries = Array.Empty<SkillLevelEntry>();

        public SkillLevelData() { }

        public SkillLevelData(Dictionary<string, float> dict)
        {
            FromDictionary(dict);
        }

        public void FromDictionary(Dictionary<string, float> dict)
        {
            if (dict == null)
            {
                entries = Array.Empty<SkillLevelEntry>();
                return;
            }
            entries = new SkillLevelEntry[dict.Count];
            int i = 0;
            foreach (var kvp in dict)
            {
                entries[i++] = new SkillLevelEntry(kvp.Key, kvp.Value);
            }
        }

        public Dictionary<string, float> ToDictionary()
        {
            var dict = new Dictionary<string, float>();
            if (entries != null)
            {
                foreach (var entry in entries)
                {
                    dict[entry.skillId] = entry.level;
                }
            }
            return dict;
        }
    }

    /// <summary>
    /// Runtime состояние NPC.
    /// </summary>
    [Serializable]
    public class NPCState
    {
        // === Identity ===
        public string NpcId;
        public string Name;
        public int Age;
        // Редактировано: 2026-05-01 — NPCRole для save/load (раньше терялась)
        public NPCRole Role;
        
        // === Cultivation ===
        public CultivationLevel CultivationLevel;
        public int SubLevel;              // 1-9 внутри уровня
        public float CultivationProgress; // 0-100%
        public MortalStage MortalStage;   // Для смертных
        public float DormantCoreProgress; // 0-100% для пробуждения ядра
        
        // === Resources ===
        public long CurrentQi;
        public long MaxQi;
        public int CurrentHealth;
        public int MaxHealth;
        public float CurrentStamina;
        public float MaxStamina;
        
        // === Body ===
        public float BodyStrength;
        public float BodyDefense;
        public float Constitution;
        // Редактировано: 2026-05-01 — Добавлено отдельное поле Agility (было BodyStrength по ошибке)
        public float Agility;
        public int Lifespan;
        public int MaxLifespan;
        
        // === Mental ===
        public float Willpower;
        public float Perception;
        public float Intelligence;
        public float Wisdom;
        
        // === Personality ===
        // FIX NPC-ATT-01: Replace Disposition with Attitude + PersonalityTrait (2026-04-11)
        [Obsolete("Use Attitude + PersonalityTrait instead. Migrated in Fix-07.")]
        public Disposition Disposition;
        public Attitude Attitude = Attitude.Neutral;
        public PersonalityTrait Personality = PersonalityTrait.None;
        public float[] ElementAffinities; // Индекс = Element enum
        
        // FIX NPC-C02: SkillLevels is now serializable via SkillLevelData
        [Obsolete("Use GetSkillLevels()/SetSkillLevels() for serialization support.")]
        public Dictionary<string, float> SkillLevels;
        
        // === Status ===
        public bool IsAlive;
        public bool IsInCombat;
        public bool IsInSect;
        public string SectId;
        public string CurrentLocation;
        
        // === AI State ===
        public NPCAIState CurrentAIState;
        public string TargetId;
        public Vector3 LastKnownPosition;
        public float StateTimer;
        
        public NPCState()
        {
            NpcId = Guid.NewGuid().ToString();
            Name = "Unnamed";
            Age = 18;
            CultivationLevel = CultivationLevel.None;
            SubLevel = 1;
            MortalStage = MortalStage.Adult;
#pragma warning disable CS0618 // Disposition obsolete
            Disposition = Disposition.Neutral;
#pragma warning restore CS0618
            Attitude = Attitude.Neutral;
            Personality = PersonalityTrait.None;
            IsAlive = true;
            ElementAffinities = new float[Enum.GetValues(typeof(Element)).Length];
#pragma warning disable CS0618 // SkillLevels obsolete
            SkillLevels = new Dictionary<string, float>();
#pragma warning restore CS0618
            CurrentAIState = NPCAIState.Idle;
            Lifespan = 80;
            MaxLifespan = 80;
        }
        
        // FIX NPC-C02: Helpers for serializable skill levels
        /// <summary>
        /// Get skill levels as serializable SkillLevelData.
        /// </summary>
        public SkillLevelData GetSkillLevelData()
        {
#pragma warning disable CS0618 // SkillLevels obsolete
            return new SkillLevelData(SkillLevels);
#pragma warning restore CS0618
        }
        
        /// <summary>
        /// Set skill levels from serializable SkillLevelData.
        /// </summary>
        public void SetSkillLevelData(SkillLevelData data)
        {
#pragma warning disable CS0618 // SkillLevels obsolete
            SkillLevels = data?.ToDictionary() ?? new Dictionary<string, float>();
#pragma warning restore CS0618
        }
        
        // FIX NPC-ATT-01: Helper to convert numeric value to Attitude enum
        /// <summary>
        /// Convert a numeric disposition value (-100..100) to Attitude enum.
        /// </summary>
        public static Attitude ValueToAttitude(int value)
        {
            if (value <= -51) return Attitude.Hatred;
            if (value <= -21) return Attitude.Hostile;
            if (value <= -10) return Attitude.Unfriendly;
            if (value <= 9) return Attitude.Neutral;
            if (value <= 49) return Attitude.Friendly;
            if (value <= 79) return Attitude.Allied;
            return Attitude.SwornAlly;
        }
    }
    
    /// <summary>
    /// Состояния AI для NPC.
    /// </summary>
    public enum NPCAIState
    {
        Idle,           // Бездействие
        Wandering,      // Блуждание
        Patrolling,     // Патрулирование
        Following,      // Следование за целью
        Fleeing,        // Бегство
        Attacking,      // Атака
        Defending,      // Защита
        Meditating,     // Медитация
        Cultivating,    // Культивация
        Resting,        // Отдых
        Trading,        // Торговля
        Talking,        // Разговор
        Working,        // Работа
        Searching,      // Поиск
        Guarding        // Охрана
    }
    
    /// <summary>
    /// Результаты взаимодействия с NPC.
    /// </summary>
    [Serializable]
    public class NPCInteractionResult
    {
        public bool Success;
        public string Message;
        public int RelationshipChange;
        public List<string> UnlockedOptions;
        public List<DialogueOption> AvailableDialogues;
        
        public NPCInteractionResult()
        {
            Success = false;
            Message = "";
            RelationshipChange = 0;
            UnlockedOptions = new List<string>();
            AvailableDialogues = new List<DialogueOption>();
        }
    }
    
    /// <summary>
    /// Опция диалога.
    /// </summary>
    [Serializable]
    public class DialogueOption
    {
        public string Id;
        public string Text;
        public string Response;
        public int RelationshipChange;
        public string RequiredFlag;
        public string SetFlag;
        public List<string> NextOptions;
        public bool IsAvailable;
        
        public DialogueOption()
        {
            Id = Guid.NewGuid().ToString();
            NextOptions = new List<string>();
            IsAvailable = true;
        }
    }
    
    /// <summary>
    /// Данные для сохранения NPC.
    /// FIX NPC-C01: All max resource fields present with correct types.
    /// FIX NPC-ATT-04: Attitude + PersonalityTrait fields added.
    /// </summary>
    [Serializable]
    public class NPCSaveData
    {
        public string NpcId;
        public string Name;
        public int Age;
        
        // Cultivation
        public int CultivationLevel;
        public int SubLevel;
        public float CultivationProgress;
        public int MortalStage;
        public float DormantCoreProgress;
        
        // Resources
        public long CurrentQi;
        public int CurrentHealth;
        public float CurrentStamina;
        
        // FIX NPC-C01: Max Resources with correct types (2026-04-11)
        public long MaxQi;
        public int MaxHealth;          // was float, fixed to int (matches NPCState.MaxHealth)
        public float MaxStamina;
        public int MaxLifespan;        // was float, fixed to int (matches NPCState.MaxLifespan)
        
        // Body
        public float BodyStrength;
        public float BodyDefense;
        public float Constitution;
        public float Agility;       // Редактировано: 2026-05-01 — Добавлено для save/load
        public int Lifespan;
        
        // Mental
        public float Willpower;
        public float Perception;
        public float Intelligence;
        public float Wisdom;
        
        // Personality
        // FIX NPC-ATT-04: Replaced Disposition with Attitude + PersonalityTrait (2026-04-11)
        [Obsolete("Use AttitudeValue + PersonalityFlags instead.")]
        public int Disposition;
        public int AttitudeValue;              // FIX NPC-ATT-04: Attitude enum as int
        public int PersonalityFlags;           // FIX NPC-ATT-04: PersonalityTrait [Flags] as int
        public float[] ElementAffinities;
        
        // FIX NPC-C02: SkillLevels as serializable array (2026-04-11)
        public SkillLevelEntry[] SkillLevels;  // was Dictionary<string,float>
        
        // Status
        public bool IsAlive;
        public string SectId;
        public string CurrentLocation;
        
        // AI
        public int CurrentAIState;
        public string TargetId;

        // Редактировано: 2026-05-01 — NPCRole для save/load
        public int RoleValue;  // NPCRole enum as int

        // ФАЗА 7: Экипировка и техники NPC
        public NPCEquipmentSaveData[] EquipmentSlots;
        public NPCTechniqueSlotSaveData[] TechniqueSlots;
    }

    // ============================================================================
    // ФАЗА 7: Save data для экипировки и техник NPC
    // ============================================================================

    /// <summary>
    /// Сериализуемые данные экипировки NPC.
    /// Сохраняет itemId, slot, grade, durability.
    /// </summary>
    [Serializable]
    public class NPCEquipmentSaveData
    {
        public int Slot;          // EquipmentSlot enum as int
        public string ItemId;
        public int Grade;        // EquipmentGrade enum as int
        public int Durability;
    }

    /// <summary>
    /// Сериализуемые данные слота техники NPC.
    /// Сохраняет techniqueId, mastery, quickSlot.
    /// </summary>
    [Serializable]
    public class NPCTechniqueSlotSaveData
    {
        public string TechniqueId;
        public float Mastery;
        public int QuickSlot;
    }
}
