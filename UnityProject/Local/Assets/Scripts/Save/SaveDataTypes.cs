// ============================================================================
// SaveDataTypes.cs — Типы данных для сохранения
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.Save
{
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
        public DateTime CreatedAt;
        public DateTime ModifiedAt;
        public int LoadCount;
        public bool IsAutoSave;
        public bool IsQuickSave;
        public List<string> Tags = new List<string>();
        
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
        
        // Controls
        public Dictionary<string, string> KeyBindings = new Dictionary<string, string>();
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
        public DateTime UnlockTime;
        public float Progress;
        public int CurrentStage;
        public int MaxStage;
        
        public bool IsCompleted => Progress >= 1f;
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
        public Dictionary<string, int> Objectives = new Dictionary<string, int>();
        public List<string> Flags = new List<string>();
        public DateTime StartTime;
        public DateTime? CompletionTime;
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
        public DateTime GeneratedAt;
        
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
        public DateTime BackupTime;
        public string Reason; // "auto", "manual", "before_update"
        public SaveHeader Header;
        
        /// <summary>
        /// Максимальное время хранения резервной копии (в днях).
        /// </summary>
        public static readonly int MaxAgeDays = 30;
        
        /// <summary>
        /// Проверить, устарела ли копия.
        /// </summary>
        public bool IsExpired()
        {
            return (DateTime.Now - BackupTime).TotalDays > MaxAgeDays;
        }
    }
}
