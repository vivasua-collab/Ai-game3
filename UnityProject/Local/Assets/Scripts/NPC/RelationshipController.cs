// ============================================================================
// RelationshipController.cs — Система отношений
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-03-31 10:38:00 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.NPC
{
    /// <summary>
    /// Тип отношения между персонажами.
    /// </summary>
    public enum RelationshipType
    {
        Stranger,        // Незнакомец (-∞ to -50)
        Hostile,         // Враждебный (-50 to -20)
        Unfriendly,      // Недружелюбный (-20 to -5)
        Neutral,         // Нейтральный (-5 to 5)
        Friendly,        // Дружелюбный (5 to 20)
        Friend,          // Друг (20 to 50)
        CloseFriend,     // Близкий друг (50 to 75)
        Family,          // Семья (75 to 100)
        SwornSibling,    // Побратим (90+)
        Master,          // Учитель
        Disciple,        // Ученик
        Lover,           // Возлюбленный
        Rival,           // Соперник
        Enemy            // Враг
    }
    
    /// <summary>
    /// Запись об отношениях между двумя персонажами.
    /// </summary>
    [Serializable]
    public class RelationshipRecord
    {
        public string SourceId;
        public string TargetId;
        public int Value;                    // -100 to 100
        public RelationshipType Type;
        public List<string> Flags;
        public List<RelationshipEvent> History;
        public float LastInteractionTime;
        public int InteractionCount;
        
        public RelationshipRecord()
        {
            Flags = new List<string>();
            History = new List<RelationshipEvent>();
        }
        
        public void AddEvent(string description, int change)
        {
            History.Add(new RelationshipEvent
            {
                Description = description,
                Change = change,
                Timestamp = Time.time
            });
            
            // Ограничиваем историю
            if (History.Count > 50)
            {
                History.RemoveAt(0);
            }
        }
    }
    
    /// <summary>
    /// Событие в истории отношений.
    /// </summary>
    [Serializable]
    public class RelationshipEvent
    {
        public string Description;
        public int Change;
        public float Timestamp;
    }
    
    /// <summary>
    /// Контроллер отношений — управление отношениями между персонажами.
    /// </summary>
    public class RelationshipController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private int maxHistorySize = 50;
        [SerializeField] private float relationshipDecayRate = 0.01f; // В день
        [SerializeField] private float decayStartDelay = 7f;         // Дней до начала затухания
        
        // === Storage ===
        private Dictionary<string, RelationshipRecord> relationships = new Dictionary<string, RelationshipRecord>();
        private string ownerId;
        
        // === Events ===
        public event Action<string, int, int> OnRelationshipChanged;  // targetId, oldValue, newValue
        public event Action<string, RelationshipType, RelationshipType> OnRelationshipTypeChanged;
        public event Action<string> OnNewRelationshipEstablished;
        
        // === Initialization ===
        
        public void Initialize(string ownerNpcId)
        {
            ownerId = ownerNpcId;
        }
        
        // === Core Methods ===
        
        /// <summary>
        /// Получить значение отношений к цели.
        /// </summary>
        public int GetRelationship(string targetId)
        {
            string key = GetKey(targetId);
            if (relationships.TryGetValue(key, out RelationshipRecord record))
            {
                return record.Value;
            }
            return 0; // Нейтральное отношение к незнакомцам
        }
        
        /// <summary>
        /// Получить тип отношений к цели.
        /// </summary>
        public RelationshipType GetRelationshipType(string targetId)
        {
            int value = GetRelationship(targetId);
            
            // Проверяем специальные флаги
            string key = GetKey(targetId);
            if (relationships.TryGetValue(key, out RelationshipRecord record))
            {
                if (record.Flags.Contains("master")) return RelationshipType.Master;
                if (record.Flags.Contains("disciple")) return RelationshipType.Disciple;
                if (record.Flags.Contains("lover")) return RelationshipType.Lover;
                if (record.Flags.Contains("rival")) return RelationshipType.Rival;
                if (record.Flags.Contains("enemy")) return RelationshipType.Enemy;
                if (record.Flags.Contains("sworn")) return RelationshipType.SwornSibling;
                if (record.Flags.Contains("family")) return RelationshipType.Family;
            }
            
            // Определяем по значению
            return CalculateRelationshipType(value);
        }
        
        private RelationshipType CalculateRelationshipType(int value)
        {
            if (value <= -50) return RelationshipType.Hostile;
            if (value <= -20) return RelationshipType.Unfriendly;
            if (value <= -5) return RelationshipType.Neutral;
            if (value < 5) return RelationshipType.Neutral;
            if (value < 20) return RelationshipType.Friendly;
            if (value < 50) return RelationshipType.Friend;
            if (value < 75) return RelationshipType.CloseFriend;
            return RelationshipType.Family;
        }
        
        /// <summary>
        /// Изменить отношение к цели.
        /// </summary>
        public void ModifyRelationship(string targetId, int change, string reason = "")
        {
            string key = GetKey(targetId);
            
            int oldValue = GetRelationship(targetId);
            int newValue = Mathf.Clamp(oldValue + change, -100, 100);
            
            if (!relationships.TryGetValue(key, out RelationshipRecord record))
            {
                record = new RelationshipRecord
                {
                    SourceId = ownerId,
                    TargetId = targetId,
                    Value = newValue,
                    Type = CalculateRelationshipType(newValue)
                };
                relationships[key] = record;
                OnNewRelationshipEstablished?.Invoke(targetId);
            }
            else
            {
                record.Value = newValue;
            }
            
            // Добавляем событие в историю
            if (!string.IsNullOrEmpty(reason))
            {
                record.AddEvent(reason, change);
            }
            
            record.LastInteractionTime = Time.time;
            record.InteractionCount++;
            
            // Проверяем изменение типа отношений
            RelationshipType oldType = record.Type;
            RelationshipType newType = CalculateRelationshipType(newValue);
            
            if (oldType != newType)
            {
                record.Type = newType;
                OnRelationshipTypeChanged?.Invoke(targetId, oldType, newType);
            }
            
            OnRelationshipChanged?.Invoke(targetId, oldValue, newValue);
        }
        
        /// <summary>
        /// Установить отношение к цели.
        /// </summary>
        public void SetRelationship(string targetId, int value, string reason = "")
        {
            int current = GetRelationship(targetId);
            int change = value - current;
            ModifyRelationship(targetId, change, reason);
        }
        
        // === Flags ===
        
        /// <summary>
        /// Добавить флаг отношения.
        /// </summary>
        public void AddFlag(string targetId, string flag)
        {
            string key = GetKey(targetId);
            if (relationships.TryGetValue(key, out RelationshipRecord record))
            {
                if (!record.Flags.Contains(flag))
                {
                    record.Flags.Add(flag);
                }
            }
        }
        
        /// <summary>
        /// Удалить флаг отношения.
        /// </summary>
        public void RemoveFlag(string targetId, string flag)
        {
            string key = GetKey(targetId);
            if (relationships.TryGetValue(key, out RelationshipRecord record))
            {
                record.Flags.Remove(flag);
            }
        }
        
        /// <summary>
        /// Проверить наличие флага.
        /// </summary>
        public bool HasFlag(string targetId, string flag)
        {
            string key = GetKey(targetId);
            if (relationships.TryGetValue(key, out RelationshipRecord record))
            {
                return record.Flags.Contains(flag);
            }
            return false;
        }
        
        // === Queries ===
        
        /// <summary>
        /// Получить всех с заданным типом отношений.
        /// </summary>
        public List<string> GetTargetsByType(RelationshipType type)
        {
            List<string> result = new List<string>();
            
            foreach (var kvp in relationships)
            {
                if (kvp.Value.Type == type)
                {
                    result.Add(kvp.Value.TargetId);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Получить всех с отношением выше порога.
        /// </summary>
        public List<string> GetTargetsAboveThreshold(int threshold)
        {
            List<string> result = new List<string>();
            
            foreach (var kvp in relationships)
            {
                if (kvp.Value.Value >= threshold)
                {
                    result.Add(kvp.Value.TargetId);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Получить всех врагов.
        /// </summary>
        public List<string> GetEnemies()
        {
            return GetTargetsByType(RelationshipType.Hostile);
        }
        
        /// <summary>
        /// Получить всех друзей.
        /// </summary>
        public List<string> GetFriends()
        {
            List<string> result = new List<string>();
            result.AddRange(GetTargetsByType(RelationshipType.Friend));
            result.AddRange(GetTargetsByType(RelationshipType.CloseFriend));
            result.AddRange(GetTargetsByType(RelationshipType.Family));
            return result;
        }
        
        // === Decay ===
        
        /// <summary>
        /// Обновить затухание отношений (вызывать раз в игровой день).
        /// </summary>
        public void ProcessDecay(float currentGameTime)
        {
            foreach (var kvp in relationships)
            {
                RelationshipRecord record = kvp.Value;
                
                // Пропускаем отношения с флагами family, sworn, master, disciple
                if (record.Flags.Contains("family") || 
                    record.Flags.Contains("sworn") ||
                    record.Flags.Contains("master") ||
                    record.Flags.Contains("disciple"))
                {
                    continue;
                }
                
                float timeSinceInteraction = currentGameTime - record.LastInteractionTime;
                
                if (timeSinceInteraction > decayStartDelay * 86400f) // В секундах
                {
                    // Затухание к нейтральному значению
                    if (record.Value > 0)
                    {
                        record.Value = Mathf.Max(0, record.Value - 1);
                    }
                    else if (record.Value < 0)
                    {
                        record.Value = Mathf.Min(0, record.Value + 1);
                    }
                    
                    record.Type = CalculateRelationshipType(record.Value);
                }
            }
        }
        
        // === Save/Load ===
        
        public List<RelationshipSaveData> GetSaveData()
        {
            List<RelationshipSaveData> data = new List<RelationshipSaveData>();
            
            foreach (var kvp in relationships)
            {
                data.Add(new RelationshipSaveData
                {
                    TargetId = kvp.Value.TargetId,
                    Value = kvp.Value.Value,
                    Flags = kvp.Value.Flags.ToArray(),
                    InteractionCount = kvp.Value.InteractionCount
                });
            }
            
            return data;
        }
        
        public void LoadSaveData(List<RelationshipSaveData> data)
        {
            relationships.Clear();
            
            foreach (var entry in data)
            {
                string key = GetKey(entry.TargetId);
                RelationshipRecord record = new RelationshipRecord
                {
                    SourceId = ownerId,
                    TargetId = entry.TargetId,
                    Value = entry.Value,
                    Type = CalculateRelationshipType(entry.Value),
                    InteractionCount = entry.InteractionCount
                };
                
                if (entry.Flags != null)
                {
                    record.Flags = new List<string>(entry.Flags);
                }
                
                relationships[key] = record;
            }
        }
        
        // === Helpers ===
        
        private string GetKey(string targetId)
        {
            return $"{ownerId}_{targetId}";
        }
    }
    
    /// <summary>
    /// Данные для сохранения отношений.
    /// </summary>
    [Serializable]
    public class RelationshipSaveData
    {
        public string TargetId;
        public int Value;
        public string[] Flags;
        public int InteractionCount;
    }
}
