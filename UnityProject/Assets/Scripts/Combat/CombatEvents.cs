// ============================================================================
// CombatEvents.cs — Система событий боя
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-31 14:10:00 UTC
// ============================================================================

using System;
using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// Тип события боя.
    /// </summary>
    public enum CombatEventType
    {
        CombatStart,        // Бой начался
        CombatEnd,          // Бой закончился
        TurnStart,          // Начало хода
        TurnEnd,            // Конец хода
        AttackStart,        // Начало атаки
        AttackHit,          // Атака попала
        AttackMiss,         // Атака промахнулась
        AttackDodged,       // Атака уклонена
        AttackParried,      // Атака парирована
        AttackBlocked,      // Атака заблокирована
        DamageDealt,        // Урон нанесён
        DamageTaken,        // Урон получен
        QiAbsorbed,         // Ци поглотила урон
        QiDepleted,         // Ци истощено
        BodyPartHit,        // Попадание в часть тела
        BodyPartSevered,    // Часть тела отрублена
        Death,              // Смерть
        TechniqueUsed,      // Техника использована
        TechniqueLearned,   // Техника изучена
        CooldownReady       // Кулдаун готов
    }

    /// <summary>
    /// Данные события боя.
    /// </summary>
    public struct CombatEventData
    {
        public CombatEventType EventType;
        public float Timestamp;
        public ICombatant Source;
        public ICombatant Target;
        public float Value;
        public string Description;
        public BodyPartType HitPart;
        public Element Element;
        public DamageResult DamageResult;

        public static CombatEventData Create(
            CombatEventType type,
            ICombatant source = null,
            ICombatant target = null,
            float value = 0f,
            string description = "")
        {
            return new CombatEventData
            {
                EventType = type,
                Timestamp = Time.time,
                Source = source,
                Target = target,
                Value = value,
                Description = description
            };
        }
    }

    /// <summary>
    /// Аргументы события боя.
    /// </summary>
    public class CombatEventArgs : EventArgs
    {
        public CombatEventData Data { get; private set; }

        public CombatEventArgs(CombatEventData data)
        {
            Data = data;
        }

        public static CombatEventArgs Create(
            CombatEventType type,
            ICombatant source = null,
            ICombatant target = null,
            float value = 0f,
            string description = "")
        {
            return new CombatEventArgs(CombatEventData.Create(type, source, target, value, description));
        }
    }

    /// <summary>
    /// Статический класс для глобальных событий боя.
    /// Позволяет подписываться на события из любого места кода.
    /// </summary>
    public static class CombatEvents
    {
        // === Events ===

        /// <summary>Общее событие боя</summary>
        public static event Action<CombatEventArgs> OnCombatEvent;

        /// <summary>Бой начался</summary>
        public static event Action<ICombatant, ICombatant> OnCombatStart;

        /// <summary>Бой закончился</summary>
        public static event Action<ICombatant, bool> OnCombatEnd;

        /// <summary>Урон нанесён</summary>
        public static event Action<ICombatant, ICombatant, float> OnDamageDealt;

        /// <summary>Урон получен</summary>
        public static event Action<ICombatant, float> OnDamageTaken;

        /// <summary>Смерть боевой единицы</summary>
        public static event Action<ICombatant> OnCombatantDeath;

        /// <summary>Техника использована</summary>
        public static event Action<ICombatant, TechniqueUseResult> OnTechniqueUsed;

        /// <summary>Ци поглотило урон</summary>
        public static event Action<ICombatant, float, int> OnQiAbsorbedDamage;

        // === Dispatch Methods ===

        /// <summary>
        /// Отправить событие боя.
        /// </summary>
        public static void Dispatch(CombatEventData data)
        {
            OnCombatEvent?.Invoke(new CombatEventArgs(data));
            DispatchSpecific(data);
        }

        /// <summary>
        /// Отправить упрощённое событие.
        /// </summary>
        public static void Dispatch(
            CombatEventType type,
            ICombatant source = null,
            ICombatant target = null,
            float value = 0f,
            string description = "")
        {
            var data = CombatEventData.Create(type, source, target, value, description);
            Dispatch(data);
        }

        private static void DispatchSpecific(CombatEventData data)
        {
            switch (data.EventType)
            {
                case CombatEventType.CombatStart:
                    OnCombatStart?.Invoke(data.Source, data.Target);
                    break;

                case CombatEventType.CombatEnd:
                    OnCombatEnd?.Invoke(data.Source, data.Value > 0);
                    break;

                case CombatEventType.DamageDealt:
                    OnDamageDealt?.Invoke(data.Source, data.Target, data.Value);
                    break;

                case CombatEventType.DamageTaken:
                    OnDamageTaken?.Invoke(data.Target, data.Value);
                    break;

                case CombatEventType.Death:
                    OnCombatantDeath?.Invoke(data.Target);
                    break;

                case CombatEventType.QiAbsorbed:
                    OnQiAbsorbedDamage?.Invoke(data.Target, data.Value, (int)data.DamageResult.QiConsumed);
                    break;

                case CombatEventType.TechniqueUsed:
                    // OnTechniqueUsed вызывается отдельно
                    break;
            }
        }

        /// <summary>
        /// Отправить событие использования техники.
        /// </summary>
        public static void DispatchTechniqueUsed(ICombatant source, TechniqueUseResult result)
        {
            var data = CombatEventData.Create(
                CombatEventType.TechniqueUsed,
                source,
                null,
                result.Damage,
                $"Technique used: {result.Type}"
            );
            Dispatch(data);
            OnTechniqueUsed?.Invoke(source, result);
        }

        /// <summary>
        /// Отправить событие урона.
        /// </summary>
        public static void DispatchDamage(ICombatant source, ICombatant target, DamageResult result)
        {
            // Урон нанесён (для атакующего)
            Dispatch(CombatEventType.DamageDealt, source, target, result.FinalDamage);

            // Урон получен (для защищающегося)
            Dispatch(CombatEventType.DamageTaken, target, null, result.FinalDamage);

            // Попадание в часть тела
            if (result.FinalDamage > 0)
            {
                var hitData = CombatEventData.Create(
                    CombatEventType.BodyPartHit,
                    source,
                    target,
                    result.FinalDamage
                );
                hitData.HitPart = result.HitPart;
                Dispatch(hitData);
            }

            // Ци поглотило урон
            if (result.QiAbsorbed)
            {
                var qiData = CombatEventData.Create(
                    CombatEventType.QiAbsorbed,
                    source,
                    target,
                    result.QiAbsorbedAmount
                );
                qiData.DamageResult = result;
                Dispatch(qiData);
            }

            // Специфичные события
            if (result.WasDodged)
                Dispatch(CombatEventType.AttackDodged, source, target);

            if (result.WasParried)
                Dispatch(CombatEventType.AttackParried, source, target);

            if (result.WasBlocked)
                Dispatch(CombatEventType.AttackBlocked, source, target);
        }

        /// <summary>
        /// Отправить событие смерти.
        /// </summary>
        public static void DispatchDeath(ICombatant killer, ICombatant victim)
        {
            Dispatch(CombatEventType.Death, killer, victim);
        }
    }

    /// <summary>
    /// Лог боя для UI и отладки.
    /// FIX CMB-C07: Убрана подписка из статического конструктора (недетерминированный порядок).
    /// Добавлена явная инициализация и очистка.
    /// </summary>
    public class CombatLog
    {
        private static readonly int MaxEntries = 100;
        private static readonly System.Collections.Generic.List<CombatEventData> entries =
            new System.Collections.Generic.List<CombatEventData>();
        
        private static bool isInitialized = false; // FIX CMB-C07

        public static event Action<CombatEventData> OnEntryAdded;

        // FIX CMB-C07: Убран статический конструктор с подпиской.
        // Инициализация через явный вызов Initialize().

        /// <summary>
        /// Явная инициализация CombatLog.
        /// Вызывать из CombatManager.Awake() или аналогичного места.
        /// FIX CMB-C07: заменяет подписку в статическом конструкторе.
        /// </summary>
        public static void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;
            CombatEvents.OnCombatEvent += AddEntry;
        }
        
        /// <summary>
        /// Очистка при завершении боя.
        /// FIX CMB-C07: отписка + сброс.
        /// </summary>
        public static void Cleanup()
        {
            CombatEvents.OnCombatEvent -= AddEntry;
            entries.Clear();
            isInitialized = false;
        }

        public static void AddEntry(CombatEventArgs args)
        {
            entries.Add(args.Data);

            if (entries.Count > MaxEntries)
            {
                entries.RemoveAt(0);
            }

            OnEntryAdded?.Invoke(args.Data);
        }

        public static System.Collections.Generic.List<CombatEventData> GetEntries(int count = 10)
        {
            var result = new System.Collections.Generic.List<CombatEventData>();
            int start = Mathf.Max(0, entries.Count - count);

            for (int i = start; i < entries.Count; i++)
            {
                result.Add(entries[i]);
            }

            return result;
        }

        public static void Clear()
        {
            entries.Clear();
        }

        public static string GetFormattedEntry(CombatEventData entry)
        {
            string sourceName = entry.Source?.Name ?? "Unknown";
            string targetName = entry.Target?.Name ?? "Unknown";

            return entry.EventType switch
            {
                CombatEventType.CombatStart => $"[БОЙ] {sourceName} vs {targetName}",
                CombatEventType.CombatEnd => $"[БОЙ] Бой окончен",
                CombatEventType.AttackHit => $"[УДАР] {sourceName} → {targetName}",
                CombatEventType.AttackDodged => $"[УКЛОН] {targetName} уклонился от {sourceName}",
                CombatEventType.AttackParried => $"[ПАРИР] {targetName} парировал атаку {sourceName}",
                CombatEventType.AttackBlocked => $"[БЛОК] {targetName} заблокировал атаку {sourceName}",
                CombatEventType.DamageDealt => $"[УРОН] {sourceName} нанёс {entry.Value:F0} урона {targetName}",
                CombatEventType.DamageTaken => $"[УРОН] {targetName} получил {entry.Value:F0} урона",
                CombatEventType.QiAbsorbed => $"[ЦИ] {targetName} поглотил {entry.Value:F0} урона через Ци",
                CombatEventType.BodyPartHit => $"[ПОПАДАНИЕ] {entry.HitPart} поражена ({entry.Value:F0})",
                CombatEventType.Death => $"[СМЕРТЬ] {targetName} погиб от руки {sourceName}",
                CombatEventType.TechniqueUsed => $"[ТЕХНИКА] {sourceName} использовал технику",
                _ => $"[{entry.EventType}] {entry.Description}"
            };
        }
    }
}
