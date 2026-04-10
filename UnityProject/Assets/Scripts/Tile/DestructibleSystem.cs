// ============================================================================
// DestructibleSystem.cs — Система разрушаемости объектов
// Cultivation World Simulator
// Создано: 2026-04-08
// Версия: 1.0 — Unity 6.3 совместимость
// ============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;

namespace CultivationGame.TileSystem
{
    /// <summary>
    /// Интерфейс для разрушаемых объектов.
    /// </summary>
    public interface IDestructible
    {
        /// <summary>Текущая прочность.</summary>
        int CurrentDurability { get; }
        
        /// <summary>Максимальная прочность.</summary>
        int MaxDurability { get; }
        
        /// <summary>Является ли объект разрушенным.</summary>
        bool IsDestroyed { get; }
        
        /// <summary>Нанести урон объекту.</summary>
        /// <param name="damage">Количество урона.</param>
        /// <param name="damageType">Тип урона.</param>
        /// <returns>Фактически нанесённый урон.</returns>
        int TakeDamage(int damage, DamageType damageType = DamageType.Physical);
        
        /// <summary>Восстановить прочность.</summary>
        void Repair(int amount);
        
        /// <summary>Событие при получении урона.</summary>
        event Action<int, DamageType> OnDamageTaken;
        
        /// <summary>Событие при разрушении.</summary>
        event Action<DestructionInfo> OnDestroyed;
    }

    /// <summary>
    /// Тип урона для расчёта эффективности разрушения.
    /// </summary>
    public enum DamageType
    {
        Physical,       // Физический урон (кулаки, blunt weapons)
        Slashing,       // Рубящий урон (мечи, топоры) — эффективен против деревьев
        Piercing,       // Колющий урон (кинжалы, копья)
        Blunt,          // Дробящий урон (молоты) — эффективен против камней
        Energy,         // Энергетический урон (Ци техники)
        Fire,           // Огненный урон
        Explosive       // Взрывной урон
    }

    /// <summary>
    /// Информация о разрушении объекта.
    /// </summary>
    [Serializable]
    public struct DestructionInfo
    {
        public string ObjectId;
        public TileObjectType ObjectType;
        public Vector2Int TilePosition;
        public Vector2 WorldPosition;
        public DamageType FinalDamageType;
        public int FinalDamage;
        public List<ResourceDrop> ResourceDrops;

        public DestructionInfo(TileObjectData obj, Vector2Int tilePos, Vector2 worldPos, DamageType damageType, int damage)
        {
            ObjectId = obj.objectId;
            ObjectType = obj.objectType;
            TilePosition = tilePos;
            WorldPosition = worldPos;
            FinalDamageType = damageType;
            FinalDamage = damage;
            ResourceDrops = new List<ResourceDrop>();
        }
    }

    /// <summary>
    /// Дроп ресурса при разрушении.
    /// </summary>
    [Serializable]
    public struct ResourceDrop
    {
        public string ResourceId;
        public int Amount;
        public Vector2 DropPosition;
        
        public ResourceDrop(string resourceId, int amount, Vector2 dropPosition)
        {
            ResourceId = resourceId;
            Amount = amount;
            DropPosition = dropPosition;
        }
    }

    /// <summary>
    /// Множители урона по типам объектов.
    /// </summary>
    public static class DamageTypeMultipliers
    {
        // Эффективность типов урона по категориям объектов
        private static readonly Dictionary<(TileObjectCategory category, DamageType damageType), float> multipliers = new()
        {
            // Vegetation (деревья, кусты) — слабы к рубящему и огню
            { (TileObjectCategory.Vegetation, DamageType.Slashing), 2.0f },
            { (TileObjectCategory.Vegetation, DamageType.Fire), 3.0f },
            { (TileObjectCategory.Vegetation, DamageType.Blunt), 0.5f },
            { (TileObjectCategory.Vegetation, DamageType.Piercing), 0.3f },
            
            // Rock (камни, руда) — слабы к дробящему
            { (TileObjectCategory.Rock, DamageType.Blunt), 2.0f },
            { (TileObjectCategory.Rock, DamageType.Explosive), 2.5f },
            { (TileObjectCategory.Rock, DamageType.Slashing), 0.3f },
            { (TileObjectCategory.Rock, DamageType.Piercing), 0.2f },
            
            // Building (стены, постройки) — слабы к дробящему и взрывному
            { (TileObjectCategory.Building, DamageType.Blunt), 1.5f },
            { (TileObjectCategory.Building, DamageType.Explosive), 2.0f },
            
            // Interactive (сундуки, алтари)
            { (TileObjectCategory.Interactive, DamageType.Physical), 1.0f },
            { (TileObjectCategory.Interactive, DamageType.Energy), 1.5f },
            
            // Furniture (мебель)
            { (TileObjectCategory.Furniture, DamageType.Slashing), 1.5f },
            { (TileObjectCategory.Furniture, DamageType.Fire), 2.0f },
        };

        /// <summary>
        /// Получить множитель урона для типа объекта.
        /// </summary>
        public static float GetMultiplier(TileObjectCategory category, DamageType damageType)
        {
            var key = (category, damageType);
            return multipliers.TryGetValue(key, out float multiplier) ? multiplier : 1.0f;
        }
    }

    /// <summary>
    /// Расширения для TileObjectData для поддержки разрушаемости.
    /// </summary>
    public static class TileObjectDestructibleExtensions
    {
        /// <summary>
        /// Нанести урон объекту.
        /// </summary>
        public static int ApplyDamage(this TileObjectData obj, int baseDamage, DamageType damageType)
        {
            // Получить множитель для типа урона
            float multiplier = DamageTypeMultipliers.GetMultiplier(obj.category, damageType);
            
            // Рассчитать финальный урон
            int actualDamage = Mathf.RoundToInt(baseDamage * multiplier);
            
            // Применить урон
            int previousDurability = obj.currentDurability;
            obj.currentDurability = Mathf.Max(0, obj.currentDurability - actualDamage);
            
            // Вернуть фактический урон
            return previousDurability - obj.currentDurability;
        }

        /// <summary>
        /// Проверить, разрушен ли объект.
        /// </summary>
        public static bool IsDestroyed(this TileObjectData obj)
        {
            return obj.currentDurability <= 0;
        }

        /// <summary>
        /// Получить дроп ресурсов при разрушении.
        /// </summary>
        public static List<ResourceDrop> GetResourceDrops(this TileObjectData obj, Vector2 worldPos, System.Random random = null)
        {
            var drops = new List<ResourceDrop>();
            
            if (string.IsNullOrEmpty(obj.resourceId) || obj.resourceCount <= 0)
                return drops;

            random ??= new System.Random();
            
            // Базовое количество = resourceCount ± 20%
            float variance = 0.2f;
            int baseAmount = obj.resourceCount;
            int amount = Mathf.RoundToInt(baseAmount * (1f + (float)(random.NextDouble() * 2 - 1) * variance));
            amount = Mathf.Max(1, amount);
            
            // Создать дроп с небольшим разбросом позиции
            Vector2 dropPos = worldPos + new Vector2(
                (float)(random.NextDouble() * 1.5f - 0.75f),
                (float)(random.NextDouble() * 1.5f - 0.75f)
            );
            
            drops.Add(new ResourceDrop(obj.resourceId, amount, dropPos));
            
            return drops;
        }
    }
}
