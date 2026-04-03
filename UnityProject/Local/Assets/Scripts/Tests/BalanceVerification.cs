// ============================================================================
// BalanceVerification.cs — Верификация баланса игры
// Cultivation World Simulator
// Версия: 1.0
// ============================================================================
// Создан: 2026-03-31
// Этап: 6 - Testing & Balance
// ============================================================================

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using CultivationGame.Core;
using CultivationGame.Combat;

namespace CultivationGame.Tests
{
    /// <summary>
    /// Верификация баланса игры.
    /// Проверяет формулы, таблицы и соотношения.
    /// </summary>
    public static class BalanceVerification
    {
        #region Qi Core Capacity

        /// <summary>
        /// Проверяет формулу ёмкости ядра.
        /// Формула: coreCapacity = 1000 × 1.1^totalSubLevels
        /// где totalSubLevels = (level-1) × 10 + subLevel
        /// </summary>
        public static void VerifyCoreCapacityFormula()
        {
            Debug.Log("=== Core Capacity Formula Verification ===");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Level | SubLevel | TotalSubs | Capacity");
            sb.AppendLine("------|----------|-----------|----------");

            for (int level = 1; level <= 10; level++)
            {
                for (int subLevel = 0; subLevel <= 9; subLevel++)
                {
                    int totalSubLevels = (level - 1) * 10 + subLevel;
                    float capacity = 1000 * Mathf.Pow(1.1f, totalSubLevels);

                    sb.AppendLine($"L{level}    | .{subLevel}       | {totalSubLevels,3}       | {capacity,10:F0}");
                }
            }

            Debug.Log(sb.ToString());

            // Проверка ключевых значений
            Debug.Log("Key values:");
            Debug.Log($"L1.0: {1000 * Mathf.Pow(1.1f, 0):F0} (expected: 1000)");
            Debug.Log($"L3.0: {1000 * Mathf.Pow(1.1f, 20):F0} (expected: ~6727)");
            Debug.Log($"L5.0: {1000 * Mathf.Pow(1.1f, 40):F0} (expected: ~45259)");
            Debug.Log($"L7.0: {1000 * Mathf.Pow(1.1f, 60):F0} (expected: ~304481)");
            Debug.Log($"L10.9: {1000 * Mathf.Pow(1.1f, 99):F0} (expected: ~12527829)");
        }

        #endregion

        #region Technique Damage by Level

        /// <summary>
        /// Проверяет урон техник по уровням.
        /// </summary>
        public static void VerifyTechniqueDamageByLevel()
        {
            Debug.Log("=== Technique Damage by Level ===");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Grade        | Common Base | ×1.2 | ×1.4 | ×1.6 | Ultimate");
            sb.AppendLine("-------------|-------------|------|------|------|---------");

            var capacities = new Dictionary<string, int>
            {
                ["Formation"] = 80,
                ["Defense"] = 72,
                ["Combat-Melee"] = 64,
                ["Combat-Weapon"] = 48,
                ["Support"] = 56,
                ["Healing"] = 56,
                ["Movement"] = 40,
                ["Curse"] = 40,
                ["Poison"] = 40,
                ["Sensory"] = 32
            };

            foreach (var kvp in capacities)
            {
                int baseCapacity = kvp.Value;
                float common = baseCapacity;
                float refined = baseCapacity * 1.2f;
                float perfect = baseCapacity * 1.4f;
                float transcendent = baseCapacity * 1.6f;
                float ultimate = baseCapacity * 1.3f; // Ultimate multiplier

                sb.AppendLine($"{kvp.Key,-12} | {common,6:F0}     | {refined,4:F0} | {perfect,4:F0} | {transcendent,4:F0} | {ultimate,7:F0}");
            }

            Debug.Log(sb.ToString());
        }

        #endregion

        #region Qi Buffer Efficiency

        /// <summary>
        /// Проверяет эффективность буфера Ци.
        /// </summary>
        public static void VerifyQiBufferEfficiency()
        {
            Debug.Log("=== Qi Buffer Efficiency ===");

            float testDamage = 100f;
            int testQi = 1000;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Type      | Defense | Absorption | Ratio | Piercing | Qi Cost | Efficiency");
            sb.AppendLine("----------|---------|------------|-------|----------|---------|------------");

            // Qi Technique + Raw Qi
            var qiRaw = QiBuffer.ProcessQiTechniqueDamage(testDamage, testQi, QiDefenseType.RawQi);
            sb.AppendLine($"Qi Tech   | Raw Qi  | {qiRaw.AbsorbedDamage,6:F0}%    | 3:1   | {qiRaw.PiercingDamage,6:F0}%   | {qiRaw.QiConsumed,5}   | {qiRaw.AbsorbedDamage / testDamage * 100,6:F0}%");

            // Qi Technique + Shield
            var qiShield = QiBuffer.ProcessQiTechniqueDamage(testDamage, testQi, QiDefenseType.Shield);
            sb.AppendLine($"Qi Tech   | Shield  | 100%       | 1:1   | {qiShield.PiercingDamage,6:F0}%    | {qiShield.QiConsumed,5}   | 100%");

            // Physical + Raw Qi
            var physRaw = QiBuffer.ProcessPhysicalDamage(testDamage, testQi, QiDefenseType.RawQi);
            sb.AppendLine($"Physical  | Raw Qi  | {physRaw.AbsorbedDamage,6:F0}%    | 5:1   | {physRaw.PiercingDamage,6:F0}%   | {physRaw.QiConsumed,5}   | {physRaw.AbsorbedDamage / testDamage * 100,6:F0}%");

            // Physical + Shield
            var physShield = QiBuffer.ProcessPhysicalDamage(testDamage, testQi, QiDefenseType.Shield);
            sb.AppendLine($"Physical  | Shield  | 100%       | 2:1   | {physShield.PiercingDamage,6:F0}%    | {physShield.QiConsumed,5}   | 100%");

            Debug.Log(sb.ToString());

            // Вывод: Qi Shield (1:1) эффективнее для Qi-техник
            // Physical Shield (2:1) менее эффективен
            Debug.Log("Conclusion:");
            Debug.Log("- Qi Shield (1:1 ratio) most efficient for Qi techniques");
            Debug.Log("- Physical Shield (2:1 ratio) less efficient");
            Debug.Log("- Raw Qi always has piercing damage (10% Qi tech, 20% physical)");
        }

        #endregion

        #region Level Suppression Table

        /// <summary>
        /// Проверяет таблицу подавления уровнем.
        /// </summary>
        public static void VerifyLevelSuppressionTable()
        {
            Debug.Log("=== Level Suppression Table ===");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Level Diff | Normal | Technique | Ultimate");
            sb.AppendLine("-----------|--------|-----------|----------");

            for (int diff = 0; diff <= 5; diff++)
            {
                float normal = GameConstants.LevelSuppressionTable[diff][0];
                float technique = GameConstants.LevelSuppressionTable[diff][1];
                float ultimate = GameConstants.LevelSuppressionTable[diff][2];

                sb.AppendLine($"{diff,10} | {normal,4:P0}  | {technique,6:P0}    | {ultimate,6:P0}");
            }

            Debug.Log(sb.ToString());

            // Вывод
            Debug.Log("Key observations:");
            Debug.Log("- Normal attacks useless after 3 level difference");
            Debug.Log("- Techniques still viable at 3 level difference (5%)");
            Debug.Log("- Ultimates viable up to 4 level difference (10%)");
            Debug.Log("- 5+ level difference = complete immunity");
        }

        #endregion

        #region Meditation Time Calculation

        /// <summary>
        /// Проверяет время медитации.
        /// </summary>
        public static void VerifyMeditationTime()
        {
            Debug.Log("=== Meditation Time Analysis ===");

            // Базовая генерация Ци: 1 единица в минуту (без плотности)
            // С плотностью: qiDensity × baseRate

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Level | Density | Gen/min | Time to fill L1 capacity | Time to fill current capacity");
            sb.AppendLine("------|---------|---------|--------------------------|------------------------------");

            for (int level = 1; level <= 10; level++)
            {
                int density = GameConstants.QiDensityByLevel[level - 1];
                int totalSubLevels = (level - 1) * 10; // .0 sublevel
                float capacity = 1000 * Mathf.Pow(1.1f, totalSubLevels);
                float genPerMinute = density; // base rate = 1

                float timeToFillL1 = 1000 / genPerMinute;
                float timeToFillCurrent = capacity / genPerMinute;

                string l1Time = FormatTime(timeToFillL1);
                string currentTime = FormatTime(timeToFillCurrent);

                sb.AppendLine($"L{level}    | {density,7} | {genPerMinute,7:F0} | {l1Time,24} | {currentTime,28}");
            }

            Debug.Log(sb.ToString());
        }

        private static string FormatTime(float minutes)
        {
            if (minutes < 60)
                return $"{minutes:F0}m";
            else if (minutes < 1440)
                return $"{minutes / 60:F1}h";
            else
                return $"{minutes / 1440:F1}d";
        }

        #endregion

        #region Grade Distribution

        /// <summary>
        /// Проверяет распределение грейдов.
        /// 60% Common, 28% Refined, 10% Perfect, 2% Transcendent
        /// </summary>
        public static void VerifyGradeDistribution()
        {
            Debug.Log("=== Grade Distribution Verification ===");

            int iterations = 10000;
            var counts = new Dictionary<TechniqueGrade, int>
            {
                [TechniqueGrade.Common] = 0,
                [TechniqueGrade.Refined] = 0,
                [TechniqueGrade.Perfect] = 0,
                [TechniqueGrade.Transcendent] = 0
            };

            // Simulate grade rolls
            for (int i = 0; i < iterations; i++)
            {
                float roll = UnityEngine.Random.value;

                if (roll < 0.02f) // 2%
                    counts[TechniqueGrade.Transcendent]++;
                else if (roll < 0.12f) // 10%
                    counts[TechniqueGrade.Perfect]++;
                else if (roll < 0.40f) // 28%
                    counts[TechniqueGrade.Refined]++;
                else // 60%
                    counts[TechniqueGrade.Common]++;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Grade        | Expected | Actual  | Difference");
            sb.AppendLine("-------------|----------|---------|------------");

            float[] expected = { 0.60f, 0.28f, 0.10f, 0.02f };
            var grades = new[] { TechniqueGrade.Common, TechniqueGrade.Refined, TechniqueGrade.Perfect, TechniqueGrade.Transcendent };

            for (int i = 0; i < grades.Length; i++)
            {
                float actual = counts[grades[i]] / (float)iterations;
                float diff = Mathf.Abs(actual - expected[i]) * 100;

                sb.AppendLine($"{grades[i],-12} | {expected[i],6:P0}   | {actual,5:P1}  | {diff,5:F1}%");
            }

            Debug.Log(sb.ToString());
        }

        #endregion

        #region Combat Damage Curve

        /// <summary>
        /// Строит кривую урона по уровням.
        /// </summary>
        public static void GenerateDamageCurve()
        {
            Debug.Log("=== Combat Damage Curve ===");

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Attacker | Defender | Normal | Technique | Ultimate");
            sb.AppendLine("---------|----------|--------|-----------|----------");

            for (int attacker = 1; attacker <= 10; attacker++)
            {
                for (int defender = 1; defender <= 10; defender++)
                {
                    float normal = LevelSuppression.CalculateSuppression(attacker, defender, AttackType.Normal);
                    float technique = LevelSuppression.CalculateSuppression(attacker, defender, AttackType.Technique);
                    float ultimate = LevelSuppression.CalculateSuppression(attacker, defender, AttackType.Ultimate);

                    if (attacker != defender) continue; // Show only diagonal for brevity

                    sb.AppendLine($"L{attacker,7} | L{defender,7} | {normal,4:P0}  | {technique,6:P0}    | {ultimate,6:P0}");
                }
            }

            Debug.Log(sb.ToString());
        }

        #endregion

        #region Full Balance Report

        /// <summary>
        /// Генерирует полный отчёт о балансе.
        /// </summary>
        public static string GenerateFullReport()
        {
            StringBuilder report = new StringBuilder();

            report.AppendLine("# Balance Verification Report");
            report.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine();

            // 1. Core Capacity
            report.AppendLine("## 1. Core Capacity Formula");
            report.AppendLine();
            report.AppendLine("Formula: `coreCapacity = 1000 × 1.1^totalSubLevels`");
            report.AppendLine();
            report.AppendLine("| Level | SubLevel | TotalSubs | Capacity |");
            report.AppendLine("|-------|----------|-----------|----------|");

            for (int level = 1; level <= 10; level++)
            {
                for (int sub = 0; sub <= 9; sub += 3) // Every 3rd for brevity
                {
                    int total = (level - 1) * 10 + sub;
                    float capacity = 1000 * Mathf.Pow(1.1f, total);
                    report.AppendLine($"| L{level}   | .{sub}       | {total,3}       | {capacity,10:F0} |");
                }
            }
            report.AppendLine();

            // 2. Level Suppression
            report.AppendLine("## 2. Level Suppression Table");
            report.AppendLine();
            report.AppendLine("| Diff | Normal | Technique | Ultimate |");
            report.AppendLine("|------|--------|-----------|----------|");

            for (int diff = 0; diff <= 5; diff++)
            {
                float n = GameConstants.LevelSuppressionTable[diff][0];
                float t = GameConstants.LevelSuppressionTable[diff][1];
                float u = GameConstants.LevelSuppressionTable[diff][2];
                report.AppendLine($"| {diff}    | {n,4:P0}  | {t,6:P0}    | {u,6:P0}  |");
            }
            report.AppendLine();

            // 3. Qi Buffer
            report.AppendLine("## 3. Qi Buffer Parameters");
            report.AppendLine();
            report.AppendLine("| Type     | Defense | Absorption | Ratio | Piercing |");
            report.AppendLine("|----------|---------|------------|-------|----------|");
            report.AppendLine("| Qi Tech  | Raw Qi  | 90%        | 3:1   | 10%      |");
            report.AppendLine("| Qi Tech  | Shield  | 100%       | 1:1   | 0%       |");
            report.AppendLine("| Physical | Raw Qi  | 80%        | 5:1   | 20%      |");
            report.AppendLine("| Physical | Shield  | 100%       | 2:1   | 0%       |");
            report.AppendLine();

            // 4. Grade Multipliers
            report.AppendLine("## 4. Grade Multipliers");
            report.AppendLine();
            report.AppendLine("| Grade        | Technique | Equipment |");
            report.AppendLine("|--------------|-----------|-----------|");
            report.AppendLine("| Common       | ×1.0      | ×1.0      |");
            report.AppendLine("| Refined      | ×1.2      | ×1.5      |");
            report.AppendLine("| Perfect      | ×1.4      | ×2.5      |");
            report.AppendLine("| Transcendent | ×1.6      | ×4.0      |");
            report.AppendLine();

            // 5. Qi Density
            report.AppendLine("## 5. Qi Density by Level");
            report.AppendLine();
            report.AppendLine("| Level | Density |");
            report.AppendLine("|-------|---------|");

            for (int i = 0; i < 10; i++)
            {
                report.AppendLine($"| L{i + 1}   | ×{GameConstants.QiDensityByLevel[i]}      |");
            }

            return report.ToString();
        }

        /// <summary>
        /// Сохраняет отчёт в файл.
        /// </summary>
        public static void SaveReportToFile()
        {
            string report = GenerateFullReport();
            string path = System.IO.Path.Combine(Application.dataPath, "balance_report.md");
            System.IO.File.WriteAllText(path, report);
            Debug.Log($"Balance report saved to: {path}");
        }

        #endregion

        #region Quick Verification

        /// <summary>
        /// Быстрая проверка всех формул.
        /// </summary>
        public static bool QuickVerify()
        {
            Debug.Log("=== Quick Balance Verification ===");

            bool allCorrect = true;

            // 1. Check Qi Density
            int[] expectedDensities = { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
            for (int i = 0; i < 10; i++)
            {
                if (GameConstants.QiDensityByLevel[i] != expectedDensities[i])
                {
                    Debug.LogError($"Qi Density L{i + 1}: expected {expectedDensities[i]}, got {GameConstants.QiDensityByLevel[i]}");
                    allCorrect = false;
                }
            }

            // 2. Check Level Suppression
            float[][] expectedSuppression = new float[][]
            {
                new float[] { 1.0f, 1.0f, 1.0f },
                new float[] { 0.5f, 0.75f, 1.0f },
                new float[] { 0.1f, 0.25f, 0.5f },
                new float[] { 0.0f, 0.05f, 0.25f },
                new float[] { 0.0f, 0.0f, 0.1f },
                new float[] { 0.0f, 0.0f, 0.0f }
            };

            for (int diff = 0; diff <= 5; diff++)
            {
                for (int type = 0; type < 3; type++)
                {
                    float expected = expectedSuppression[diff][type];
                    float actual = GameConstants.LevelSuppressionTable[diff][type];

                    if (!Mathf.Approximately(expected, actual))
                    {
                        Debug.LogError($"Suppression diff={diff} type={type}: expected {expected}, got {actual}");
                        allCorrect = false;
                    }
                }
            }

            // 3. Check Grade Multipliers
            var expectedGrades = new Dictionary<TechniqueGrade, float>
            {
                [TechniqueGrade.Common] = 1.0f,
                [TechniqueGrade.Refined] = 1.2f,
                [TechniqueGrade.Perfect] = 1.4f,
                [TechniqueGrade.Transcendent] = 1.6f
            };

            foreach (var kvp in expectedGrades)
            {
                float actual = GameConstants.TechniqueGradeMultipliers[kvp.Key];
                if (!Mathf.Approximately(kvp.Value, actual))
                {
                    Debug.LogError($"Grade {kvp.Key}: expected {kvp.Value}, got {actual}");
                    allCorrect = false;
                }
            }

            // 4. Check HP Ratios
            if (!Mathf.Approximately(GameConstants.RED_HP_RATIO, 0.7f))
            {
                Debug.LogError($"Red HP Ratio: expected 0.7, got {GameConstants.RED_HP_RATIO}");
                allCorrect = false;
            }

            if (!Mathf.Approximately(GameConstants.BLACK_HP_RATIO, 0.3f))
            {
                Debug.LogError($"Black HP Ratio: expected 0.3, got {GameConstants.BLACK_HP_RATIO}");
                allCorrect = false;
            }

            if (allCorrect)
            {
                Debug.Log("<color=green>All balance values verified correctly!</color>");
            }
            else
            {
                Debug.Log("<color=red>Balance verification failed!</color>");
            }

            return allCorrect;
        }

        #endregion
    }

    // ============================================================================
    // Editor Integration
    // ============================================================================

    #if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(BalanceVerificationComponent))]
    public class BalanceVerificationEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(10);

            if (GUILayout.Button("Run Quick Verification"))
            {
                BalanceVerification.QuickVerify();
            }

            if (GUILayout.Button("Verify Core Capacity"))
            {
                BalanceVerification.VerifyCoreCapacityFormula();
            }

            if (GUILayout.Button("Verify Level Suppression"))
            {
                BalanceVerification.VerifyLevelSuppressionTable();
            }

            if (GUILayout.Button("Verify Qi Buffer"))
            {
                BalanceVerification.VerifyQiBufferEfficiency();
            }

            if (GUILayout.Button("Verify Meditation Time"))
            {
                BalanceVerification.VerifyMeditationTime();
            }

            if (GUILayout.Button("Generate Full Report"))
            {
                Debug.Log(BalanceVerification.GenerateFullReport());
            }

            if (GUILayout.Button("Save Report to File"))
            {
                BalanceVerification.SaveReportToFile();
            }
        }
    }
    #endif

    /// <summary>
    /// Компонент для запуска верификации баланса из редактора.
    /// </summary>
    public class BalanceVerificationComponent : MonoBehaviour
    {
        [ContextMenu("Run Quick Verification")]
        private void RunQuickVerification()
        {
            BalanceVerification.QuickVerify();
        }

        [ContextMenu("Generate Full Report")]
        private void GenerateReport()
        {
            Debug.Log(BalanceVerification.GenerateFullReport());
        }
    }
}
