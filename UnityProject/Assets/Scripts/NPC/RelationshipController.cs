// ============================================================================
// RelationshipController.cs — Система отношений
// Cultivation World Simulator
// Версия: 1.1
// Создано: 2026-03-30 10:00:00 UTC
// Редактировано: 2026-04-11 00:00:00 UTC — Fix-07
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.World;

namespace CultivationGame.NPC
{
    /// <summary>
    /// Тип отношения между персонажами.
    /// FIX NPC-M06: Added Stranger and SwornAlly to match Attitude enum ranges.
    /// </summary>
    public enum RelationshipType
    {
        Stranger,        // Незнакомец (no interaction yet)
        Hatred,          // Ненависть (-100..-51)
        Hostile,         // Враждебный (-50..-21)
        Unfriendly,      // Недружелюбный (-20..-10)
        Neutral,         // Нейтральный (-9..9)
        Friendly,        // Дружелюбный (10..49)
        Allied,          // Союзник (50..79)
        SwornAlly,       // Побратим (80..100)
        // Special role-based types
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
#pragma warning disable CS0414
        [SerializeField] private int maxHistorySize = 50;
        [SerializeField] private float relationshipDecayRate = 0.01f; // В день
#pragma warning restore CS0414
        [SerializeField] private float decayStartDelay = 7f;         // Дней до начала затухания
        
        // === Storage ===
        private Dictionary<string, RelationshipRecord> relationships = new Dictionary<string, RelationshipRecord>();
        private string ownerId;
        
        // FIX NPC-H05: TimeController reference for game time
        private TimeController timeController;
        
        // === Events ===
        public event Action<string, int, int> OnRelationshipChanged;  // targetId, oldValue, newValue
        public event Action<string, RelationshipType, RelationshipType> OnRelationshipTypeChanged;
        public event Action<string> OnNewRelationshipEstablished;
        
        // === Initialization ===
        
        private void Start()
        {
            // FIX NPC-H05: Get TimeController via ServiceLocator
            ServiceLocator.Request<TimeController>(tc => timeController = tc);
        }
        
        public void Initialize(string ownerNpcId)
        {
            // FIX NPC-M03: Validate ownerId (2026-04-11)
            if (string.IsNullOrEmpty(ownerNpcId))
            {
                Debug.LogWarning("[RelationshipController] Initialize called with null/empty ownerId");
                return;
            }
            ownerId = ownerNpcId;
        }
        
        // === Core Methods ===
        
        /// <summary>
        /// Получить значение отношений к цели.
        /// </summary>
        public int GetRelationship(string targetId)
        {
            // FIX NPC-M03: Validate targetId
            if (string.IsNullOrEmpty(targetId)) return 0;
            
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
                if (record.Flags.Contains("sworn")) return RelationshipType.SwornAlly;
                if (record.Flags.Contains("family")) return RelationshipType.Allied;
            }
            
            // Если нет записи — Stranger
            if (!relationships.ContainsKey(key))
            {
                return RelationshipType.Stranger;
            }
            
            // Определяем по значению
            return CalculateRelationshipType(value);
        }
        
        // FIX NPC-M06: CalculateRelationshipType aligned with Attitude enum ranges (2026-04-11)
        /// <summary>
        /// Calculate relationship type from numeric value.
        /// Ranges now match the Attitude enum:
        ///   Hatred:     -100..-51
        ///   Hostile:    -50..-21
        ///   Unfriendly: -20..-10
        ///   Neutral:    -9..9
        ///   Friendly:   10..49
        ///   Allied:     50..79
        ///   SwornAlly:  80..100
        /// </summary>
        private RelationshipType CalculateRelationshipType(int value)
        {
            if (value <= -51) return RelationshipType.Hatred;
            if (value <= -21) return RelationshipType.Hostile;
            if (value <= -10) return RelationshipType.Unfriendly;
            if (value <= 9) return RelationshipType.Neutral;
            if (value <= 49) return RelationshipType.Friendly;
            if (value <= 79) return RelationshipType.Allied;
            return RelationshipType.SwornAlly;
        }
        
        // FIX NPC-ATT-03: CalculateAttitude from numeric value (2026-04-11)
        /// <summary>
        /// Calculate Attitude enum from a numeric relationship value.
        /// This replaces the old GetDisposition() pattern.
        /// </summary>
        public Attitude CalculateAttitude(string targetId)
        {
            int value = GetRelationship(targetId);
            return ValueToAttitude(value);
        }
        
        /// <summary>
        /// Convert numeric value to Attitude enum.
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
        
        /// <summary>
        /// Изменить отношение к цели.
        /// </summary>
        public void ModifyRelationship(string targetId, int change, string reason = "")
        {
            // FIX NPC-M03: Validate ownerId and targetId
            if (string.IsNullOrEmpty(ownerId))
            {
                Debug.LogWarning("[RelationshipController] ownerId not set, cannot modify relationship");
                return;
            }
            if (string.IsNullOrEmpty(targetId))
            {
                Debug.LogWarning("[RelationshipController] targetId is null/empty, cannot modify relationship");
                return;
            }
            
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
            
            // FIX NPC-H05: Use game time for LastInteractionTime
            record.LastInteractionTime = GetGameTime();
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
            List<string> result = new List<string>();
            result.AddRange(GetTargetsByType(RelationshipType.Hatred));
            result.AddRange(GetTargetsByType(RelationshipType.Hostile));
            return result;
        }
        
        /// <summary>
        /// Получить всех друзей.
        /// </summary>
        public List<string> GetFriends()
        {
            List<string> result = new List<string>();
            result.AddRange(GetTargetsByType(RelationshipType.Friendly));
            result.AddRange(GetTargetsByType(RelationshipType.Allied));
            result.AddRange(GetTargetsByType(RelationshipType.SwornAlly));
            return result;
        }
        
        // === Decay ===
        
        // FIX NPC-H05: ProcessDecay uses game time (TimeController) instead of Time.time (2026-04-11)
        /// <summary>
        /// Обновить затухание отношений (вызывать раз в игровой день).
        /// Uses game time from TimeController for accurate decay calculation.
        /// </summary>
        /// <param name="currentGameTime">Current game time in seconds (from TimeController.TotalGameSeconds)</param>
        public void ProcessDecay(float currentGameTime)
        {
            // FIX NPC-M03: Validate ownerId before processing
            if (string.IsNullOrEmpty(ownerId)) return;
            
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
        
        /// <summary>
        /// FIX NPC-H05: Process decay using TimeController (auto-resolves game time).
        /// </summary>
        public void ProcessDecay()
        {
            float gameTime = GetGameTime();
            ProcessDecay(gameTime);
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
        
        // FIX NPC-H05: Get game time from TimeController, fallback to Time.time
        /// <summary>
        /// Get current game time. Uses TimeController if available, otherwise falls back to Time.time.
        /// </summary>
        private float GetGameTime()
        {
            if (timeController != null)
            {
                return (float)timeController.TotalGameSeconds;
            }
            return Time.time;
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
