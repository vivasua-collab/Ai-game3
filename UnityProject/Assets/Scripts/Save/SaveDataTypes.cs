// ============================================================================
// SaveDataTypes.cs — Типы данных для сохранения
// Cultivation World Simulator
// Версия: 1.2 — FIX SAV-H01: Dictionary wrappers для JsonUtility, новые SaveData классы
// ============================================================================
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-04-11 Fix-08
//
// ИЗМЕНЕНИЯ В ВЕРСИИ 1.2:
// - FIX SAV-H01: Dictionary fields заменены на сериализуемые массивы пар
// - Добавлены FormationSaveData, BuffSaveData, TileSaveData, ChargerSaveData
// - Добавлены CraftingSkillEntry, CustomBonusEntry, KeyBindingEntry, ObjectiveEntry
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.Save
{
    // ============================================================================
    // Сериализуемые обёртки для Dictionary (JsonUtility не поддерживает Dictionary)
    // FIX SAV-H01 (2026-04-11)
    // ============================================================================

    /// <summary>
    /// Запись пары ключ-значение для KeyBindings (string→string).
    /// FIX SAV-H01: Замена Dictionary<string,string> для JsonUtility (2026-04-11)
    /// </summary>
    [Serializable]
    public class KeyBindingEntry
    {
        public string key;
        public string value;

        public KeyBindingEntry() { }
        public KeyBindingEntry(string k, string v) { key = k; value = v; }
    }

    /// <summary>
    /// Запись пары для Objectives квеста (string→int).
    /// FIX SAV-H01: Замена Dictionary<string,int> для JsonUtility (2026-04-11)
    /// </summary>
    [Serializable]
    public class ObjectiveEntry
    {
        public string objectiveId;
        public int progress;

        public ObjectiveEntry() { }
        public ObjectiveEntry(string id, int prog) { objectiveId = id; progress = prog; }
    }

    /// <summary>
    /// Запись пары для customBonuses (string→float).
    /// FIX SAV-H01: Замена Dictionary<string,float> для JsonUtility (2026-04-11)
    /// </summary>
    [Serializable]
    public class CustomBonusEntry
    {
        public string statName;
        public float bonus;

        public CustomBonusEntry() { }
        public CustomBonusEntry(string name, float val) { statName = name; bonus = val; }
    }

    /// <summary>
    /// Запись пары для craftingSkills (CraftingType→int).
    /// FIX SAV-H01: Замена Dictionary<CraftingType,int> для JsonUtility (2026-04-11)
    /// </summary>
    [Serializable]
    public class CraftingSkillEntry
    {
        public int craftingType; // CraftType as int for serialization
        public int level;

        public CraftingSkillEntry() { }
        public CraftingSkillEntry(int type, int lvl) { craftingType = type; level = lvl; }
    }
    /// <summary>
    /// Заголовок сохранения для быстрого доступа.
    /// </summary>
    [Serializable]
    public class SaveHeader
    {
        public string Version;
        public int SaveNumber;
        public long Timestamp;
        public float TotalPlayTime;
        public string CharacterName;
        public int CharacterLevel;
        public string LocationName;
        
        /// <summary>
        /// Проверить совместимость версии.
        /// </summary>
        public bool IsCompatible(string currentVersion)
        {
            if (string.IsNullOrEmpty(Version) || string.IsNullOrEmpty(currentVersion))
                return false;
            
            string[] saveParts = Version.Split('.');
            string[] currentParts = currentVersion.Split('.');
            
            // Проверяем только мажорную версию
            if (saveParts.Length > 0 && currentParts.Length > 0)
            {
                return saveParts[0] == currentParts[0];
            }
            
            return false;
        }
    }
    
    /// <summary>
    /// Метаданные сохранения.
    /// </summary>
    [Serializable]
    public class SaveMetadata
    {
        public string SaveId;
        public string SlotName;
        public long CreatedAtUnix;   // FIX: Unix timestamp
        public long ModifiedAtUnix;  // FIX: Unix timestamp
        public int LoadCount;
        public bool IsAutoSave;
        public bool IsQuickSave;
        public List<string> Tags = new List<string>();
        
        /// <summary>
        /// Получить DateTime создания.
        /// </summary>
        public System.DateTime GetCreatedAt()
        {
            return DateTimeOffset.FromUnixTimeSeconds(CreatedAtUnix).LocalDateTime;
        }
        
        /// <summary>
        /// Получить DateTime модификации.
        /// </summary>
        public System.DateTime GetModifiedAt()
        {
            return DateTimeOffset.FromUnixTimeSeconds(ModifiedAtUnix).LocalDateTime;
        }
        
        /// <summary>
        /// Добавить тег.
        /// </summary>
        public void AddTag(string tag)
        {
            if (!Tags.Contains(tag))
            {
                Tags.Add(tag);
            }
        }
        
        /// <summary>
        /// Проверить наличие тега.
        /// </summary>
        public bool HasTag(string tag)
        {
            return Tags.Contains(tag);
        }
    }
    
    /// <summary>
    /// Данные настроек игры.
    /// </summary>
    [Serializable]
    public class SettingsSaveData
    {
        // Audio
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public float VoiceVolume = 1f;
        
        // Graphics
        public int QualityLevel = 2;
        public bool Fullscreen = true;
        public int ResolutionWidth = 1920;
        public int ResolutionHeight = 1080;
        public int RefreshRate = 60;
        public bool VSync = true;
        
        // Gameplay
        public int TimeSpeedDefault = 1;
        public bool AutoSave = true;
        public float AutoSaveInterval = 300f;
        public bool ShowTutorials = true;
        public string Language = "en";
        
        // Controls — FIX SAV-H01: сериализуемый массив вместо Dictionary (2026-04-11)
        public KeyBindingEntry[] KeyBindings = new KeyBindingEntry[0];
        
        /// <summary>
        /// FIX SAV-H01: Convert KeyBindings to Dictionary for runtime use (2026-04-11)
        /// </summary>
        public Dictionary<string, string> KeyBindingsToDictionary()
        {
            var dict = new Dictionary<string, string>();
            if (KeyBindings != null)
            {
                foreach (var entry in KeyBindings)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.key))
                        dict[entry.key] = entry.value;
                }
            }
            return dict;
        }
        
        /// <summary>
        /// FIX SAV-H01: Set KeyBindings from Dictionary for serialization (2026-04-11)
        /// </summary>
        public void KeyBindingsFromDictionary(Dictionary<string, string> dict)
        {
            if (dict == null)
            {
                KeyBindings = new KeyBindingEntry[0];
                return;
            }
            KeyBindings = new KeyBindingEntry[dict.Count];
            int i = 0;
            foreach (var kvp in dict)
            {
                KeyBindings[i++] = new KeyBindingEntry(kvp.Key, kvp.Value);
            }
        }
    }
    
    /// <summary>
    /// Данные статистики игрока.
    /// </summary>
    [Serializable]
    public class PlayerStatsSaveData
    {
        // Общая статистика
        public long TotalQiGained;
        public long TotalQiSpent;
        public int EnemiesKilled;
        public int TechniquesLearned;
        public int ItemsCollected;
        public int DistanceTraveled;
        public int LocationsVisited;
        public int DialoguesCompleted;
        public int QuestsCompleted;
        
        // Боевая статистика
        public int DamageDealt;
        public int DamageTaken;
        public int CriticalHits;
        public int Dodges;
        public int Blocks;
        
        // Культивация
        public int Breakthroughs;
        public int MeditationHours;
        public int TechniquesMastered;
        
        // Социальная
        public int RelationshipsFormed;
        public int FactionsJoined;
        public int SectsCreated;
        
        /// <summary>
        /// Добавить значение к статистике.
        /// </summary>
        public void AddStat(string statName, long value)
        {
            // Используем рефлексию или switch для обновления
            // Реализуется при необходимости
        }
    }
    
    /// <summary>
    /// Данные достижения.
    /// </summary>
    [Serializable]
    public class AchievementSaveData
    {
        public string AchievementId;
        public bool IsUnlocked;
        public long UnlockTimeUnix;  // FIX: Unix timestamp
        public float Progress;
        public int CurrentStage;
        public int MaxStage;
        
        public bool IsCompleted => Progress >= 1f;
        
        public System.DateTime GetUnlockTime()
        {
            return DateTimeOffset.FromUnixTimeSeconds(UnlockTimeUnix).LocalDateTime;
        }
    }
    
    /// <summary>
    /// Данные квеста.
    /// </summary>
    [Serializable]
    public class QuestSaveData
    {
        public string QuestId;
        public int Stage;
        public bool IsStarted;
        public bool IsCompleted;
        public bool IsFailed;
        // FIX SAV-H01: сериализуемый массив вместо Dictionary (2026-04-11)
        public ObjectiveEntry[] Objectives = new ObjectiveEntry[0];
        public List<string> Flags = new List<string>();
        public long StartTimeUnix;  // FIX: Unix timestamp
        public long? CompletionTimeUnix;  // FIX: Unix timestamp (nullable)
        
        /// <summary>
        /// FIX SAV-H01: Convert Objectives to Dictionary for runtime use (2026-04-11)
        /// </summary>
        public Dictionary<string, int> ObjectivesToDictionary()
        {
            var dict = new Dictionary<string, int>();
            if (Objectives != null)
            {
                foreach (var entry in Objectives)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.objectiveId))
                        dict[entry.objectiveId] = entry.progress;
                }
            }
            return dict;
        }
        
        /// <summary>
        /// FIX SAV-H01: Set Objectives from Dictionary for serialization (2026-04-11)
        /// </summary>
        public void ObjectivesFromDictionary(Dictionary<string, int> dict)
        {
            if (dict == null)
            {
                Objectives = new ObjectiveEntry[0];
                return;
            }
            Objectives = new ObjectiveEntry[dict.Count];
            int i = 0;
            foreach (var kvp in dict)
            {
                Objectives[i++] = new ObjectiveEntry(kvp.Key, kvp.Value);
            }
        }
        
        public System.DateTime GetStartTime()
        {
            return DateTimeOffset.FromUnixTimeSeconds(StartTimeUnix).LocalDateTime;
        }
        
        public System.DateTime? GetCompletionTime()
        {
            return CompletionTimeUnix.HasValue 
                ? DateTimeOffset.FromUnixTimeSeconds(CompletionTimeUnix.Value).LocalDateTime 
                : null;
        }
    }
    
    /// <summary>
    /// Данные журнала.
    /// </summary>
    [Serializable]
    public class JournalSaveData
    {
        public List<JournalEntrySaveData> Entries = new List<JournalEntrySaveData>();
        public List<string> UnlockedTopics = new List<string>();
        public List<string> DiscoveredLore = new List<string>();
    }
    
    /// <summary>
    /// Запись в журнале.
    /// </summary>
    [Serializable]
    public class JournalEntrySaveData
    {
        public string EntryId;
        public string Title;
        public string Content;
        public int Year;
        public int Month;
        public int Day;
        public string LocationId;
        public string Category;
    }
    
    /// <summary>
    /// Контрольная сумма для проверки целостности.
    /// </summary>
    [Serializable]
    public class SaveChecksum
    {
        public string DataHash;
        public long DataSize;
        public long GeneratedAtUnix;  // FIX: Unix timestamp
        
        public System.DateTime GetGeneratedAt()
        {
            return DateTimeOffset.FromUnixTimeSeconds(GeneratedAtUnix).LocalDateTime;
        }
        
        /// <summary>
        /// Вычислить хеш данных.
        /// </summary>
        public static string CalculateHash(string data)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(data);
                byte[] hash = sha.ComputeHash(bytes);
                return Convert.ToBase64String(hash);
            }
        }
        
        /// <summary>
        /// Проверить целостность данных.
        /// </summary>
        public bool Verify(string data)
        {
            if (string.IsNullOrEmpty(DataHash)) return false;
            return CalculateHash(data) == DataHash;
        }
    }
    
    /// <summary>
    /// Резервная копия сохранения.
    /// </summary>
    [Serializable]
    public class SaveBackup
    {
        public string BackupId;
        public string OriginalSlotId;
        public long BackupTimeUnix;  // FIX: Unix timestamp
        public string Reason; // "auto", "manual", "before_update"
        public SaveHeader Header;
        
        /// <summary>
        /// Максимальное время хранения резервной копии (в днях).
        /// </summary>
        public static readonly int MaxAgeDays = 30;
        
        public System.DateTime GetBackupTime()
        {
            return DateTimeOffset.FromUnixTimeSeconds(BackupTimeUnix).LocalDateTime;
        }
        
        /// <summary>
        /// Проверить, устарела ли копия.
        /// </summary>
        public bool IsExpired()
        {
            var backupTime = GetBackupTime();
            return (System.DateTime.Now - backupTime).TotalDays > MaxAgeDays;
        }
    }

    // ============================================================================
    // Новые SaveData классы — FIX SAV-H03 (2026-04-11)
    // ============================================================================

    /// <summary>
    /// Данные формации для сохранения.
    /// FIX SAV-H03: Добавлено для полного сохранения (2026-04-11)
    /// Note: Named FormationSaveEntry to avoid conflict with Formation.FormationSaveData
    /// </summary>
    [Serializable]
    public class FormationSaveEntry
    {
        public string formationId;
        public int practitionerCount;
        public long qiPoolAmount;
    }

    /// <summary>
    /// Данные баффа для сохранения.
    /// FIX SAV-H03: Добавлено для полного сохранения (2026-04-11)
    /// </summary>
    [Serializable]
    public class BuffSaveData
    {
        public string buffId;
        public float remainingDuration;
        public int stacks;
    }

    /// <summary>
    /// Данные тайловой карты для сохранения.
    /// FIX SAV-H03: Добавлено для полного сохранения (2026-04-11)
    /// </summary>
    [Serializable]
    public class TileSaveData
    {
        public int width;
        public int height;
        public string serializedTiles; // JSON string of TileMapData
    }

    /// <summary>
    /// Данные зарядника для сохранения.
    /// FIX SAV-H03: Добавлено для полного сохранения (2026-04-11)
    /// </summary>
    [Serializable]
    public class ChargerSaveData
    {
        public int slotCount;
        public float heatLevel;
        public long qiStored;
    }
}
