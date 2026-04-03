// ============================================================================
// FactionController.cs — Система фракций
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-03-30 14:00:00 UTC
// Редактировано: 2026-03-31 10:17:18 UTC
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.World
{
    /// <summary>
    /// Тип фракции.
    /// </summary>
    public enum FactionType
    {
        Sect,           // Секта культиваторов
        Clan,           // Клан (семейная организация)
        Guild,          // Гильдия (торговая/ремесленная)
        Empire,         // Империя
        Kingdom,        // Королевство
        Academy,        // Академия
        Alliance,       // Альянс
        Bandit,         // Бандитская группа
        Demon,          // Демоническая секта
        Orthodox,       // Праведная секта
        Unorthodox      // Нейтральная секта
    }
    
    /// <summary>
    /// Ранг во фракции.
    /// </summary>
    public enum FactionRank
    {
        None,           // Не член фракции
        Recruit,        // Новичок
        Outer,          // Внешний ученик
        Inner,          // Внутренний ученик
        Core,           // Основной ученик
        Elder,          // Старейшина
        ViceLeader,     // Заместитель лидера
        Leader,         // Лидер
        Patriarch,      // Патриарх (для кланов)
        Ancestor        // Предок-основатель (почётный титул)
    }
    
    /// <summary>
    /// Данные фракции.
    /// </summary>
    [Serializable]
    public class FactionData
    {
        public string FactionId;
        public string FactionName;
        public FactionType Type;
        public FactionRank DefaultRank;
        
        [TextArea(2, 5)]
        public string Description;
        
        // Основная информация
        public string HeadquartersLocation;
        public string LeaderId;
        public int MemberCount;
        public int MaxMembers;
        
        // Репутация
        public int GlobalReputation;    // -100 to 100
        public int Influence;            // Политическое влияние
        
        // Ресурсы
        public long Wealth;
        public int Territory;
        
        // Отношения с другими фракциями
        public Dictionary<string, int> FactionRelations;
        
        // Требования для вступления
        public CultivationLevel MinCultivationLevel;
        public int MinAge;
        public int MaxAge;
        
        // Флаги
        public bool IsRecruiting;
        public bool IsAtWar;
        public List<string> Allies;
        public List<string> Enemies;
        
        public FactionData()
        {
            FactionRelations = new Dictionary<string, int>();
            Allies = new List<string>();
            Enemies = new List<string>();
        }
    }
    
    /// <summary>
    /// Членство во фракции.
    /// </summary>
    [Serializable]
    public class FactionMembership
    {
        public string FactionId;
        public FactionRank Rank;
        public float JoinTime;
        public float ContributionPoints;
        public int CompletedMissions;
        public List<string> UnlockedPrivileges;
        
        public FactionMembership()
        {
            UnlockedPrivileges = new List<string>();
        }
    }
    
    /// <summary>
    /// Глобальный контроллер фракций — управляет всеми фракциями в мире.
    /// </summary>
    public class FactionController : MonoBehaviour
    {
        [Header("Config")]
        [SerializeField] private float relationDecayRate = 0.1f;
        [SerializeField] private float warThreshold = -50;
        [SerializeField] private float allianceThreshold = 50;
        
        // === Storage ===
        private Dictionary<string, FactionData> factions = new Dictionary<string, FactionData>();
        private Dictionary<string, FactionMembership> playerMemberships = new Dictionary<string, FactionMembership>();
        
        // === Events ===
        public event Action<FactionData> OnFactionCreated;
        public event Action<FactionData> OnFactionDestroyed;
        public event Action<string, string, int> OnFactionRelationChanged; // faction1, faction2, value
        public event Action<string, string, FactionRank> OnMemberJoined;
        public event Action<string, string, FactionRank> OnMemberLeft;
        public event Action<string, string, FactionRank> OnMemberRankChanged;
        public event Action<string, string> OnWarDeclared;
        public event Action<string, string> OnAllianceFormed;
        
        // === Faction Management ===
        
        /// <summary>
        /// Создать новую фракцию.
        /// </summary>
        public FactionData CreateFaction(string name, FactionType type, string leaderId)
        {
            FactionData faction = new FactionData
            {
                FactionId = Guid.NewGuid().ToString(),
                FactionName = name,
                Type = type,
                LeaderId = leaderId,
                DefaultRank = FactionRank.Recruit,
                MinCultivationLevel = CultivationLevel.None,
                IsRecruiting = true
            };
            
            factions[faction.FactionId] = faction;
            OnFactionCreated?.Invoke(faction);
            
            // Лидер автоматически вступает с высшим рангом
            AddMember(faction.FactionId, leaderId, FactionRank.Leader);
            
            return faction;
        }
        
        /// <summary>
        /// Удалить фракцию.
        /// </summary>
        public void DestroyFaction(string factionId)
        {
            if (!factions.TryGetValue(factionId, out FactionData faction))
                return;
            
            factions.Remove(factionId);
            OnFactionDestroyed?.Invoke(faction);
        }
        
        /// <summary>
        /// Получить данные фракции.
        /// </summary>
        public FactionData GetFaction(string factionId)
        {
            if (factions.TryGetValue(factionId, out FactionData faction))
                return faction;
            return null;
        }
        
        /// <summary>
        /// Получить все фракции.
        /// </summary>
        public List<FactionData> GetAllFactions()
        {
            return new List<FactionData>(factions.Values);
        }
        
        // === Membership ===
        
        /// <summary>
        /// Добавить члена во фракцию.
        /// </summary>
        public bool AddMember(string factionId, string memberId, FactionRank rank = FactionRank.Recruit)
        {
            if (!factions.TryGetValue(factionId, out FactionData faction))
                return false;
            
            if (faction.MemberCount >= faction.MaxMembers && faction.MaxMembers > 0)
                return false;
            
            FactionMembership membership = new FactionMembership
            {
                FactionId = factionId,
                Rank = rank,
                JoinTime = Time.time
            };
            
            playerMemberships[memberId] = membership;
            faction.MemberCount++;
            
            OnMemberJoined?.Invoke(factionId, memberId, rank);
            
            return true;
        }
        
        /// <summary>
        /// Удалить члена из фракции.
        /// </summary>
        public void RemoveMember(string factionId, string memberId)
        {
            if (!playerMemberships.TryGetValue(memberId, out FactionMembership membership))
                return;
            
            if (membership.FactionId != factionId)
                return;
            
            FactionRank oldRank = membership.Rank;
            playerMemberships.Remove(memberId);
            
            if (factions.TryGetValue(factionId, out FactionData faction))
            {
                faction.MemberCount--;
            }
            
            OnMemberLeft?.Invoke(factionId, memberId, oldRank);
        }
        
        /// <summary>
        /// Изменить ранг члена.
        /// </summary>
        public void SetMemberRank(string factionId, string memberId, FactionRank newRank)
        {
            if (!playerMemberships.TryGetValue(memberId, out FactionMembership membership))
                return;
            
            if (membership.FactionId != factionId)
                return;
            
            FactionRank oldRank = membership.Rank;
            membership.Rank = newRank;
            
            OnMemberRankChanged?.Invoke(factionId, memberId, newRank);
        }
        
        /// <summary>
        /// Получить членство персонажа.
        /// </summary>
        public FactionMembership GetMembership(string memberId)
        {
            if (playerMemberships.TryGetValue(memberId, out FactionMembership membership))
                return membership;
            return null;
        }
        
        /// <summary>
        /// Получить ранг персонажа во фракции.
        /// </summary>
        public FactionRank GetMemberRank(string memberId, string factionId)
        {
            var membership = GetMembership(memberId);
            if (membership != null && membership.FactionId == factionId)
                return membership.Rank;
            return FactionRank.None;
        }
        
        // === Relations ===
        
        /// <summary>
        /// Получить отношение между фракциями.
        /// </summary>
        public int GetFactionRelation(string faction1Id, string faction2Id)
        {
            if (!factions.TryGetValue(faction1Id, out FactionData faction1))
                return 0;
            
            if (faction1.FactionRelations.TryGetValue(faction2Id, out int relation))
                return relation;
            
            return 0;
        }
        
        /// <summary>
        /// Изменить отношение между фракциями.
        /// </summary>
        public void ModifyFactionRelation(string faction1Id, string faction2Id, int change)
        {
            if (!factions.TryGetValue(faction1Id, out FactionData faction1))
                return;
            
            if (!factions.TryGetValue(faction2Id, out FactionData faction2))
                return;
            
            int oldValue = GetFactionRelation(faction1Id, faction2Id);
            int newValue = Mathf.Clamp(oldValue + change, -100, 100);
            
            faction1.FactionRelations[faction2Id] = newValue;
            faction2.FactionRelations[faction1Id] = newValue;
            
            OnFactionRelationChanged?.Invoke(faction1Id, faction2Id, newValue);
            
            // Проверяем на войну/альянс
            CheckWarAndAlliance(faction1Id, faction2Id, newValue);
        }
        
        private void CheckWarAndAlliance(string faction1Id, string faction2Id, int relation)
        {
            var faction1 = GetFaction(faction1Id);
            var faction2 = GetFaction(faction2Id);
            
            if (faction1 == null || faction2 == null) return;
            
            // Проверяем войну
            if (relation <= warThreshold)
            {
                if (!faction1.Enemies.Contains(faction2Id))
                {
                    faction1.Enemies.Add(faction2Id);
                    faction2.Enemies.Add(faction1Id);
                    faction1.Allies.Remove(faction2Id);
                    faction2.Allies.Remove(faction1Id);
                    faction1.IsAtWar = true;
                    faction2.IsAtWar = true;
                    
                    OnWarDeclared?.Invoke(faction1Id, faction2Id);
                }
            }
            // Проверяем альянс
            else if (relation >= allianceThreshold)
            {
                if (!faction1.Allies.Contains(faction2Id))
                {
                    faction1.Allies.Add(faction2Id);
                    faction2.Allies.Add(faction1Id);
                    faction1.Enemies.Remove(faction2Id);
                    faction2.Enemies.Remove(faction1Id);
                    
                    OnAllianceFormed?.Invoke(faction1Id, faction2Id);
                }
            }
        }
        
        // === Contribution ===
        
        /// <summary>
        /// Добавить очки вклада члену.
        /// </summary>
        public void AddContribution(string memberId, float points)
        {
            if (!playerMemberships.TryGetValue(memberId, out FactionMembership membership))
                return;
            
            membership.ContributionPoints += points;
        }
        
        /// <summary>
        /// Потратить очки вклада.
        /// </summary>
        public bool SpendContribution(string memberId, float points)
        {
            if (!playerMemberships.TryGetValue(memberId, out FactionMembership membership))
                return false;
            
            if (membership.ContributionPoints < points)
                return false;
            
            membership.ContributionPoints -= points;
            return true;
        }
        
        // === Queries ===
        
        /// <summary>
        /// Получить все фракции типа.
        /// </summary>
        public List<FactionData> GetFactionsByType(FactionType type)
        {
            List<FactionData> result = new List<FactionData>();
            
            foreach (var kvp in factions)
            {
                if (kvp.Value.Type == type)
                {
                    result.Add(kvp.Value);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Получить врагов фракции.
        /// </summary>
        public List<FactionData> GetEnemies(string factionId)
        {
            List<FactionData> result = new List<FactionData>();
            
            var faction = GetFaction(factionId);
            if (faction == null) return result;
            
            foreach (string enemyId in faction.Enemies)
            {
                var enemy = GetFaction(enemyId);
                if (enemy != null)
                {
                    result.Add(enemy);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Получить союзников фракции.
        /// </summary>
        public List<FactionData> GetAllies(string factionId)
        {
            List<FactionData> result = new List<FactionData>();
            
            var faction = GetFaction(factionId);
            if (faction == null) return result;
            
            foreach (string allyId in faction.Allies)
            {
                var ally = GetFaction(allyId);
                if (ally != null)
                {
                    result.Add(ally);
                }
            }
            
            return result;
        }
        
        // === Save/Load ===
        
        public FactionSystemSaveData GetSaveData()
        {
            return new FactionSystemSaveData
            {
                Factions = new List<FactionData>(factions.Values),
                Memberships = new List<FactionMembershipSaveData>()
            };
        }
        
        public void LoadSaveData(FactionSystemSaveData data)
        {
            factions.Clear();
            playerMemberships.Clear();
            
            foreach (var faction in data.Factions)
            {
                factions[faction.FactionId] = faction;
            }
        }
    }
    
    /// <summary>
    /// Данные для сохранения системы фракций.
    /// </summary>
    [Serializable]
    public class FactionSystemSaveData
    {
        public List<FactionData> Factions;
        public List<FactionMembershipSaveData> Memberships;
    }
    
    /// <summary>
    /// Данные членства для сохранения.
    /// </summary>
    [Serializable]
    public class FactionMembershipSaveData
    {
        public string MemberId;
        public string FactionId;
        public int Rank;
        public float ContributionPoints;
    }
}
