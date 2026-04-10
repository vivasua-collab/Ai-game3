// ============================================================================
// FactionData.cs — Данные фракции
// Cultivation World Simulator
// Версия: 1.0
// Создано: 2026-04-03 09:20:39 UTC
// Редактировано: 2026-04-03 09:20:39 UTC
// ============================================================================
//
// Источник: docs/DATA_MODELS.md §14 "Faction"
// Источник: docs/FACTION_SYSTEM.md
// ============================================================================

using UnityEngine;
using System.Collections.Generic;
using CultivationGame.Core;

namespace CultivationGame.Data.ScriptableObjects
{
    /// <summary>
    /// Тип фракции
    /// </summary>
    public enum FactionType
    {
        Sect,           // Секта культивации
        Clan,           // Клан (семейный)
        Guild,          // Гильдия торговцев/ремесленников
        Empire,         // Империя/государство
        Alliance,       // Альянс нескольких фракций
        Independent,    // Независимая организация
        Criminal,       // Преступная организация
        Religious       // Религиозный орден
    }

    // FIX DAT-C01: FactionRelationType moved to Enums.cs (2026-04-11)

    /// <summary>
    /// Данные фракции.
    /// Создаётся как ScriptableObject для каждой фракции в мире.
    /// </summary>
    [CreateAssetMenu(fileName = "Faction", menuName = "Cultivation/Faction")]
    public class FactionData : ScriptableObject
    {
        [Header("Basic Info")]
        [Tooltip("Уникальный ID фракции")]
        public string factionId;
        
        [Tooltip("Название на русском")]
        public string nameRu;
        
        [Tooltip("Название на английском")]
        public string nameEn;
        
        [TextArea(2, 4)]
        [Tooltip("Описание фракции")]
        public string description;
        
        [Tooltip("Эмблема/герб")]
        public Sprite emblem;
        
        [Tooltip("Цвет фракции")]
        public Color factionColor = Color.white;
        
        [Header("Classification")]
        [Tooltip("Тип фракции")]
        public FactionType factionType;
        
        [Tooltip("ID нации (если есть)")]
        public string nationId;
        
        [Tooltip("Главная локация")]
        public LocationAsset headquarters; // FIX DAT-H03: LocationData→LocationAsset (2026-04-11)
        
        [Header("Power")]
        [Tooltip("Уровень силы (1-10)")]
        [Range(1, 10)]
        public int powerLevel = 1;
        
        [Tooltip("Средний уровень культивации членов")]
        [Range(1, 10)]
        public int avgMemberCultivationLevel = 1;
        
        [Tooltip("Количество членов")]
        public int memberCount = 100;
        
        [Tooltip("Ресурсы фракции")]
        public FactionResources resources = new FactionResources();
        
        [Header("Relations")]
        [Tooltip("Начальные отношения с игроком")]
        [Range(-100, 100)]
        public int basePlayerRelation = 0;
        
        [Tooltip("Отношения с другими фракциями")]
        public List<FactionRelation> relations = new List<FactionRelation>();
        
        [Header("Requirements")]
        [Tooltip("Минимальный уровень для вступления")]
        [Range(0, 10)]
        public int minJoinLevel = 0;
        
        [Tooltip("Требования для вступления")]
        public List<JoinRequirement> joinRequirements = new List<JoinRequirement>();
        
        [Header("Benefits")]
        [Tooltip("Бонусы члена фракции")]
        public List<FactionBenefit> benefits = new List<FactionBenefit>();
        
        [Tooltip("Доступные техники")]
        public List<TechniqueData> availableTechniques = new List<TechniqueData>();
        
        [Header("Ranks")]
        [Tooltip("Ранги фракции")]
        public List<FactionRank> ranks = new List<FactionRank>();
        
        [Header("NPCs")]
        [Tooltip("Лидер фракции")]
        public string leaderNpcId;
        
        [Tooltip("Важные NPC")]
        public List<string> importantNpcs = new List<string>();
        
        // === Runtime Methods ===
        
        /// <summary>
        /// Получить отношение к другой фракции
        /// </summary>
        public FactionRelationType GetRelationTo(FactionData other)
        {
            foreach (var relation in relations)
            {
                if (relation.targetFaction == other)
                    return relation.relationType;
            }
            return FactionRelationType.Neutral;
        }
        
        /// <summary>
        /// Получить силу отношения к другой фракции
        /// </summary>
        public int GetRelationStrength(FactionData other)
        {
            foreach (var relation in relations)
            {
                if (relation.targetFaction == other)
                    return relation.strength;
            }
            return 0;
        }
        
        /// <summary>
        /// Проверить, может ли персонаж вступить
        /// </summary>
        public bool CanJoin(int cultivationLevel, Dictionary<string, int> stats)
        {
            if (cultivationLevel < minJoinLevel)
                return false;
            
            foreach (var req in joinRequirements)
            {
                if (!req.CheckRequirement(stats))
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Получить ранг по репутации
        /// FIX DAT-M01: Sort ranks before binary search (2026-04-11)
        /// </summary>
        public FactionRank GetRankByReputation(int reputation)
        {
            if (ranks == null || ranks.Count == 0) return null;
            
            // FIX DAT-M01: Sort by minReputation descending to find highest qualifying rank (2026-04-11)
            var sortedRanks = new List<FactionRank>(ranks);
            sortedRanks.Sort((a, b) => b.minReputation.CompareTo(a.minReputation));
            
            foreach (var rank in sortedRanks)
            {
                if (reputation >= rank.minReputation)
                    return rank;
            }
            return sortedRanks[sortedRanks.Count - 1]; // Return lowest rank as fallback
        }
    }
    
    /// <summary>
    /// Отношение между фракциями
    /// </summary>
    [System.Serializable]
    public class FactionRelation
    {
        [Tooltip("Целевая фракция")]
        public FactionData targetFaction;
        
        [Tooltip("Тип отношения")]
        public FactionRelationType relationType;
        
        [Tooltip("Сила отношения (-100 до 100)")]
        [Range(-100, 100)]
        public int strength = 0;
        
        [Tooltip("Скорость ухудшения")]
        [Range(0f, 10f)]
        public float decayRate = 1.0f;
    }
    
    /// <summary>
    /// Ресурсы фракции
    /// </summary>
    [System.Serializable]
    public class FactionResources
    {
        [Tooltip("Духовные камни")]
        public long spiritStones = 10000;
        
        [Tooltip("Очки вклада (всего)")]
        public long contributionPoints = 0;
        
        [Tooltip("Репутация в мире")]
        [Range(-100, 100)]
        public int worldReputation = 0;
        
        [Tooltip("Территории")]
        public List<string> territories = new List<string>();
    }
    
    /// <summary>
    /// Требование для вступления
    /// </summary>
    [System.Serializable]
    public class JoinRequirement
    {
        [Tooltip("Тип требования")]
        public RequirementType type;
        
        [Tooltip("Имя характеристики")]
        public string statName;
        
        [Tooltip("Минимальное значение")]
        public int minValue;
        
        public bool CheckRequirement(Dictionary<string, int> stats)
        {
            if (type == RequirementType.Stat && !string.IsNullOrEmpty(statName))
            {
                if (stats.TryGetValue(statName, out int value))
                    return value >= minValue;
                return false;
            }
            return true;
        }
    }
    
    /// <summary>
    /// Тип требования
    /// </summary>
    public enum RequirementType
    {
        Stat,           // Характеристика
        Quest,          // Квест
        Item,           // Предмет
        Reputation,     // Репутация
        Recommendation  // Рекомендация
    }
    
    /// <summary>
    /// Бонус члена фракции
    /// </summary>
    [System.Serializable]
    public class FactionBenefit
    {
        [Tooltip("Название бонуса")]
        public string name;
        
        [Tooltip("Описание")]
        [TextArea(1, 2)]
        public string description;
        
        [Tooltip("Минимальный ранг")]
        public int minRank = 0;
        
        [Tooltip("Тип бонуса")]
        public BenefitType benefitType;
        
        [Tooltip("Значение бонуса")]
        public float value;
    }
    
    /// <summary>
    /// Тип бонуса
    /// </summary>
    public enum BenefitType
    {
        StatBonus,          // Бонус к характеристике
        Discount,           // Скидка в магазинах
        TechniqueAccess,    // Доступ к техникам
        ResourceAccess,     // Доступ к ресурсам
        QuestReward,        // Бонус к наградам за квесты
        TrainingBonus       // Бонус к обучению
    }
    
    /// <summary>
    /// Ранг во фракции
    /// </summary>
    [System.Serializable]
    public class FactionRank
    {
        [Tooltip("Название ранга")]
        public string name;
        
        [Tooltip("Название на английском")]
        public string nameEn;
        
        [Tooltip("Минимальная репутация")]
        public int minReputation = 0;
        
        [Tooltip("Уровень в иерархии (0 = низший)")]
        public int hierarchyLevel = 0;
        
        [Tooltip("Привилегии")]
        public List<string> privileges = new List<string>();
    }
}
