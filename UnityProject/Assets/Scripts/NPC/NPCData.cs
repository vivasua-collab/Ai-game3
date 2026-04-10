// ============================================================================
// NPCData.cs — Runtime данные NPC
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-03-31 10:38:00 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;
using CultivationGame.Qi;

namespace CultivationGame.NPC
{
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
        public int Lifespan;
        public int MaxLifespan;
        
        // === Mental ===
        public float Willpower;
        public float Perception;
        public float Intelligence;
        public float Wisdom;
        
        // === Personality ===
        public Disposition Disposition;
        public float[] ElementAffinities; // Индекс = Element enum
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
            Disposition = Disposition.Neutral;
            IsAlive = true;
            ElementAffinities = new float[(int)Element.Count];
            SkillLevels = new Dictionary<string, float>();
            CurrentAIState = NPCAIState.Idle;
            Lifespan = 80;
            MaxLifespan = 80;
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
        
        // Max Resources
        public long MaxQi;
        public float MaxHealth;
        public float MaxStamina;
        public float MaxLifespan;
        
        // Body
        public float BodyStrength;
        public float BodyDefense;
        public float Constitution;
        public int Lifespan;
        
        // Mental
        public float Willpower;
        public float Perception;
        public float Intelligence;
        public float Wisdom;
        
        // Personality
        public int Disposition;
        public float[] ElementAffinities;
        public Dictionary<string, float> SkillLevels;
        
        // Status
        public bool IsAlive;
        public string SectId;
        public string CurrentLocation;
        
        // AI
        public int CurrentAIState;
        public string TargetId;
    }
}
