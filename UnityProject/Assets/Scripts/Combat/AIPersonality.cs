// ============================================================================
// AIPersonality.cs — Данные личности ИИ (ScriptableObject)
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создано: 2026-05-04 04:40:00 UTC
// ============================================================================

using UnityEngine;
using CultivationGame.Core;

namespace CultivationGame.Combat
{
    /// <summary>
    /// ScriptableObject личности ИИ.
    /// Определяет поведение NPC в бою.
    ///
    /// ╔═══════════════════════════════════════════════════════════════════════════╗
    /// ║  ПАРАМЕТРЫ ЛИЧНОСТИ                                                        ║
    /// ╠═══════════════════════════════════════════════════════════════════════════╣
    /// ║  aggression (0-1): склонность к атаке, первый удар                          ║
    /// ║  defensiveness (0-1): склонность к защите, блоку                           ║
    /// ║  retreatThreshold (0-1): HP% для отступления                               ║
    /// ║  techniquePreference (0-1): предпочтение техник vs базовые атаки           ║
    /// ║  chargeRiskTolerance (0-1): готовность накачивать под огнём               ║
    /// ╚═══════════════════════════════════════════════════════════════════════════╝
    ///
    /// FIX Н-06: Связь с PersonalityTrait (Enums.cs):
    ///   PersonalityTrait.Aggressive  → высокая aggression, низкая retreatThreshold
    ///   PersonalityTrait.Cautious    → высокая defensiveness, низкая aggression
    ///   PersonalityTrait.Treacherous → низкая defensiveness союзника, высокий retreatThreshold
    ///   PersonalityTrait.Ambitious   → высокая techniquePreference, aggression
    ///   PersonalityTrait.Pacifist    → низкая aggression, высокий retreatThreshold
    ///   PersonalityTrait.Loyal       → низкий retreatThreshold (не отступает)
    ///   PersonalityTrait.Vengeful    → высокая aggression после получения урона
    ///
    /// AIPersonality управляет ПОВЕДЕНИЕМ В БОЮ (как сражается).
    /// PersonalityTrait управляет ХАРАКТЕРОМ NPC (взаимодействие, квесты, диалог).
    /// NPCPresetData.personalityFlags → CombatAI → AIPersonality (маппинг при инициализации).
    /// </summary>
    [CreateAssetMenu(fileName = "AIPersonality", menuName = "Cultivation/AI Personality")]
    public class AIPersonality : ScriptableObject
    {
        [Header("Агрессия")]
        [Tooltip("Склонность к атаке (0 = пассивный, 1 = берсерк)")]
        [Range(0f, 1f)]
        public float aggression = 0.5f;

        [Header("Защита")]
        [Tooltip("Склонность к защите (0 = никогда не защищается, 1 = всегда блок)")]
        [Range(0f, 1f)]
        public float defensiveness = 0.3f;

        [Header("Отступление")]
        [Tooltip("HP% при котором начинается отступление (0 = до смерти, 1 = при малейшем уроне)")]
        [Range(0f, 1f)]
        public float retreatThreshold = 0.2f;

        [Header("Техники")]
        [Tooltip("Предпочтение техник vs базовые атаки (0 = только базовые, 1 = только техники)")]
        [Range(0f, 1f)]
        public float techniquePreference = 0.6f;

        [Header("Накачка")]
        [Tooltip("Готовность накачивать технику под огнём (0 = прервёт при малейшем уроне, 1 = будет накачивать до конца)")]
        [Range(0f, 1f)]
        public float chargeRiskTolerance = 0.4f;

        /// <summary>
        /// Предустановленные личности.
        /// </summary>
        public static AIPersonality CreateBerserker()
        {
            var p = CreateInstance<AIPersonality>();
            p.aggression = 0.9f;
            p.defensiveness = 0.1f;
            p.retreatThreshold = 0.05f;
            p.techniquePreference = 0.7f;
            p.chargeRiskTolerance = 0.8f;
            p.name = "Berserker";
            return p;
        }

        public static AIPersonality CreateCautious()
        {
            var p = CreateInstance<AIPersonality>();
            p.aggression = 0.3f;
            p.defensiveness = 0.7f;
            p.retreatThreshold = 0.4f;
            p.techniquePreference = 0.5f;
            p.chargeRiskTolerance = 0.2f;
            p.name = "Cautious";
            return p;
        }

        public static AIPersonality CreateBalanced()
        {
            var p = CreateInstance<AIPersonality>();
            p.aggression = 0.5f;
            p.defensiveness = 0.4f;
            p.retreatThreshold = 0.2f;
            p.techniquePreference = 0.6f;
            p.chargeRiskTolerance = 0.4f;
            p.name = "Balanced";
            return p;
        }
    }
}
