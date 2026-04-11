// ============================================================================
// CombatTests.cs — Unit тесты боевой системы
// Cultivation World Simulator
// Создано: 2026-03-31
// Редактировано: 2026-04-11 06:38:02 UTC — Qi int→long миграция в QiBuffer тестах
// Этап: 6 - Testing & Balance
// ============================================================================

using System;
using UnityEngine;
using NUnit.Framework;
using CultivationGame.Core;
using CultivationGame.Combat;

namespace CultivationGame.Tests
{
    /// <summary>
    /// Unit тесты для боевой системы.
    /// Тестируют DamageCalculator, QiBuffer, LevelSuppression.
    /// </summary>
    [TestFixture]
    public class CombatTests
    {
        #region Setup

        [SetUp]
        public void Setup()
        {
            // Инициализация перед каждым тестом
        }

        [TearDown]
        public void TearDown()
        {
            // Очистка после каждого теста
        }

        #endregion

        #region LevelSuppression Tests

        [Test]
        public void LevelSuppression_SameLevel_ReturnsFullDamage()
        {
            // Arrange
            int attackerLevel = 3;
            int defenderLevel = 3;

            // Act
            float normal = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Normal);
            float technique = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Technique);
            float ultimate = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Ultimate);

            // Assert
            Assert.AreEqual(1.0f, normal, "Normal attack should deal full damage at same level");
            Assert.AreEqual(1.0f, technique, "Technique should deal full damage at same level");
            Assert.AreEqual(1.0f, ultimate, "Ultimate should deal full damage at same level");
        }

        [Test]
        public void LevelSuppression_OneLevelDifference_AppliesCorrectPenalty()
        {
            // Arrange
            int attackerLevel = 3;
            int defenderLevel = 4;

            // Act
            float normal = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Normal);
            float technique = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Technique);
            float ultimate = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Ultimate);

            // Assert - Based on ALGORITHMS.md table
            Assert.AreEqual(0.5f, normal, "Normal attack should be 50% against +1 level");
            Assert.AreEqual(0.75f, technique, "Technique should be 75% against +1 level");
            Assert.AreEqual(1.0f, ultimate, "Ultimate should be 100% against +1 level");
        }

        [Test]
        public void LevelSuppression_TwoLevelDifference_AppliesCorrectPenalty()
        {
            // Arrange
            int attackerLevel = 3;
            int defenderLevel = 5;

            // Act
            float normal = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Normal);
            float technique = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Technique);
            float ultimate = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Ultimate);

            // Assert
            Assert.AreEqual(0.1f, normal, "Normal attack should be 10% against +2 levels");
            Assert.AreEqual(0.25f, technique, "Technique should be 25% against +2 levels");
            Assert.AreEqual(0.5f, ultimate, "Ultimate should be 50% against +2 levels");
        }

        [Test]
        public void LevelSuppression_ThreeLevelDifference_AppliesCorrectPenalty()
        {
            // Arrange
            int attackerLevel = 3;
            int defenderLevel = 6;

            // Act
            float normal = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Normal);
            float technique = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Technique);
            float ultimate = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Ultimate);

            // Assert
            Assert.AreEqual(0.0f, normal, "Normal attack should be 0% against +3 levels");
            Assert.AreEqual(0.05f, technique, "Technique should be 5% against +3 levels");
            Assert.AreEqual(0.25f, ultimate, "Ultimate should be 25% against +3 levels");
        }

        [Test]
        public void LevelSuppression_FivePlusLevelDifference_NoDamage()
        {
            // Arrange
            int attackerLevel = 1;
            int defenderLevel = 6;

            // Act
            float normal = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Normal);
            float technique = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Technique);
            float ultimate = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Ultimate);

            // Assert
            Assert.AreEqual(0.0f, normal, "Normal attack should be 0% against +5+ levels");
            Assert.AreEqual(0.0f, technique, "Technique should be 0% against +5+ levels");
            Assert.AreEqual(0.0f, ultimate, "Ultimate should be 0% against +5+ levels");
        }

        [Test]
        public void LevelSuppression_AttackerHigherLevel_FullDamage()
        {
            // Arrange
            int attackerLevel = 5;
            int defenderLevel = 3;

            // Act
            float suppression = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Normal);

            // Assert - Attacker stronger means NO suppression
            Assert.AreEqual(1.0f, suppression, "Attacker higher level should deal full damage");
        }

        [Test]
        public void LevelSuppression_TechniqueLevelAffectsEffectiveLevel()
        {
            // Arrange
            int attackerLevel = 1;
            int defenderLevel = 3;
            int techniqueLevel = 3;

            // Act
            float withoutTechnique = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Technique, 0);
            float withTechnique = LevelSuppression.CalculateSuppression(attackerLevel, defenderLevel, AttackType.Technique, techniqueLevel);

            // Assert
            Assert.AreNotEqual(withoutTechnique, withTechnique, "Technique level should affect suppression");
            Assert.Greater(withTechnique, withoutTechnique, "Technique level should reduce suppression");
        }

        #endregion

        #region QiBuffer Tests

        [Test]
        public void QiBuffer_QiTechnique_RawQiAbsorbsCorrectly()
        {
            // Arrange
            float incomingDamage = 100f;
            long currentQi = 1000;
            QiDefenseType defenseType = QiDefenseType.RawQi;

            // Act
            var result = QiBuffer.ProcessQiTechniqueDamage(incomingDamage, currentQi, defenseType);

            // Assert
            // 90% absorbable = 90 damage
            // 3:1 ratio = 270 Qi needed
            Assert.AreEqual(90f, result.AbsorbedDamage, 0.1f, "Should absorb 90% of damage");
            Assert.AreEqual(10f, result.PiercingDamage, 0.1f, "10% should always pierce");
            Assert.AreEqual(270, result.QiConsumed, "Should consume 270 Qi (3:1 ratio)");
        }

        [Test]
        public void QiBuffer_QiTechnique_ShieldAbsorbsFully()
        {
            // Arrange
            float incomingDamage = 100f;
            long currentQi = 1000;
            QiDefenseType defenseType = QiDefenseType.Shield;

            // Act
            var result = QiBuffer.ProcessQiTechniqueDamage(incomingDamage, currentQi, defenseType);

            // Assert
            // 100% absorbable
            // 1:1 ratio = 100 Qi needed
            Assert.AreEqual(100f, result.AbsorbedDamage, 0.1f, "Shield should absorb 100%");
            Assert.AreEqual(0f, result.PiercingDamage, 0.1f, "No piercing with shield");
            Assert.AreEqual(100, result.QiConsumed, "Should consume 100 Qi (1:1 ratio)");
        }

        [Test]
        public void QiBuffer_PhysicalDamage_RawQiAbsorbsLess()
        {
            // Arrange
            float incomingDamage = 100f;
            long currentQi = 1000;
            QiDefenseType defenseType = QiDefenseType.RawQi;

            // Act
            var result = QiBuffer.ProcessPhysicalDamage(incomingDamage, currentQi, defenseType);

            // Assert
            // 80% absorbable = 80 damage
            // 5:1 ratio = 400 Qi needed
            // 20% always pierces
            Assert.AreEqual(80f, result.AbsorbedDamage, 0.1f, "Should absorb 80% of physical damage");
            Assert.AreEqual(20f, result.PiercingDamage, 0.1f, "20% should always pierce for physical");
            Assert.AreEqual(400, result.QiConsumed, "Should consume 400 Qi (5:1 ratio)");
        }

        [Test]
        public void QiBuffer_PhysicalDamage_ShieldUsesHigherRatio()
        {
            // Arrange
            float incomingDamage = 100f;
            long currentQi = 1000;
            QiDefenseType defenseType = QiDefenseType.Shield;

            // Act
            var result = QiBuffer.ProcessPhysicalDamage(incomingDamage, currentQi, defenseType);

            // Assert
            // 100% absorbable
            // 2:1 ratio = 200 Qi needed
            Assert.AreEqual(100f, result.AbsorbedDamage, 0.1f, "Shield should absorb 100%");
            Assert.AreEqual(0f, result.PiercingDamage, 0.1f, "No piercing with shield");
            Assert.AreEqual(200, result.QiConsumed, "Should consume 200 Qi (2:1 ratio for physical)");
        }

        [Test]
        public void QiBuffer_InsufficientQi_PartialAbsorption()
        {
            // Arrange
            float incomingDamage = 100f;
            long currentQi = 100; // Not enough for full absorption
            QiDefenseType defenseType = QiDefenseType.RawQi;

            // Act
            var result = QiBuffer.ProcessQiTechniqueDamage(incomingDamage, currentQi, defenseType);

            // Assert
            Assert.IsTrue(result.WasQiDepleted, "Qi should be depleted");
            Assert.AreEqual(0, result.QiRemaining, "Qi should be 0 after depletion");
            Assert.Greater(result.PiercingDamage, 10f, "More should pierce when Qi depleted");
        }

        [Test]
        public void QiBuffer_NoDefense_NoAbsorption()
        {
            // Arrange
            float incomingDamage = 100f;
            long currentQi = 1000;
            QiDefenseType defenseType = QiDefenseType.None;

            // Act
            var result = QiBuffer.ProcessQiTechniqueDamage(incomingDamage, currentQi, defenseType);

            // Assert
            Assert.AreEqual(0f, result.AbsorbedDamage, "No absorption without defense");
            Assert.AreEqual(incomingDamage, result.PiercingDamage, "Full damage should pierce");
            Assert.AreEqual(0, result.QiConsumed, "No Qi should be consumed");
        }

        [Test]
        public void QiBuffer_MinimumQiRequired_ActivatesDefense()
        {
            // Arrange
            float incomingDamage = 100f;
            int enoughQi = 100;
            int notEnoughQi = 5; // Below MIN_QI_FOR_BUFFER
            QiDefenseType defenseType = QiDefenseType.RawQi;

            // Act
            var resultEnough = QiBuffer.ProcessQiTechniqueDamage(incomingDamage, enoughQi, defenseType);
            var resultNotEnough = QiBuffer.ProcessQiTechniqueDamage(incomingDamage, notEnoughQi, defenseType);

            // Assert
            Assert.Greater(resultEnough.AbsorbedDamage, 0f, "Should absorb with enough Qi");
            Assert.AreEqual(0f, resultNotEnough.AbsorbedDamage, "Should not absorb below minimum Qi");
        }

        #endregion

        #region TechniqueCapacity Tests

        [Test]
        public void TechniqueCapacity_CalculatesCorrectDamage()
        {
            // Arrange
            int capacity = 64; // Base combat technique

            // Act
            float commonDamage = TechniqueCapacity.CalculateDamage(capacity, TechniqueGrade.Common, false);
            float refinedDamage = TechniqueCapacity.CalculateDamage(capacity, TechniqueGrade.Refined, false);
            float perfectDamage = TechniqueCapacity.CalculateDamage(capacity, TechniqueGrade.Perfect, false);
            float transcendentDamage = TechniqueCapacity.CalculateDamage(capacity, TechniqueGrade.Transcendent, false);

            // Assert
            // Based on TECHNIQUE_SYSTEM.md
            // Common: ×1.0, Refined: ×1.2, Perfect: ×1.4, Transcendent: ×1.6
            Assert.AreEqual(capacity, commonDamage, 0.1f, "Common should be base damage");

            float expectedRefined = capacity * 1.2f;
            Assert.AreEqual(expectedRefined, refinedDamage, 0.1f, "Refined should be 1.2x");

            float expectedPerfect = capacity * 1.4f;
            Assert.AreEqual(expectedPerfect, perfectDamage, 0.1f, "Perfect should be 1.4x");

            float expectedTranscendent = capacity * 1.6f;
            Assert.AreEqual(expectedTranscendent, transcendentDamage, 0.1f, "Transcendent should be 1.6x");
        }

        [Test]
        public void TechniqueCapacity_Ultimate_HasMultiplier()
        {
            // Arrange
            int capacity = 64;

            // Act
            float normalDamage = TechniqueCapacity.CalculateDamage(capacity, TechniqueGrade.Common, false);
            float ultimateDamage = TechniqueCapacity.CalculateDamage(capacity, TechniqueGrade.Common, true);

            // Assert
            // Ultimate has ×1.3 multiplier
            float expected = normalDamage * GameConstants.ULTIMATE_DAMAGE_MULTIPLIER;
            Assert.AreEqual(expected, ultimateDamage, 0.1f, "Ultimate should have 1.3x multiplier");
        }

        #endregion

        #region DamageCalculator Integration Tests

        [Test]
        public void DamageCalculator_FullPipeline_ProducesValidResult()
        {
            // Arrange
            int techniqueCapacity = 64;

            var attacker = new AttackerParams
            {
                CultivationLevel = 3,
                Strength = 15,
                Agility = 12,
                Intelligence = 10,
                Penetration = 0,
                AttackElement = Element.Fire,
                CombatSubtype = CombatSubtype.MeleeStrike,
                TechniqueLevel = 3,
                TechniqueGrade = TechniqueGrade.Common,
                IsUltimate = false,
                IsQiTechnique = true
            };

            var defender = new DefenderParams
            {
                CultivationLevel = 3,
                CurrentQi = 1000,
                QiDefense = QiDefenseType.RawQi,
                Agility = 10,
                Strength = 10,
                ArmorCoverage = 0.5f,
                DamageReduction = 0.2f,
                ArmorValue = 10,
                DodgePenalty = 0,
                ParryBonus = 0,
                BlockBonus = 0,
                BodyMaterial = BodyMaterial.Organic
            };

            // Act
            var result = DamageCalculator.CalculateDamage(techniqueCapacity, attacker, defender);

            // Assert
            Assert.GreaterOrEqual(result.FinalDamage, 0f, "Final damage should be non-negative");
            Assert.GreaterOrEqual(result.RedHPDamage, 0f, "Red HP damage should be non-negative");
            Assert.GreaterOrEqual(result.BlackHPDamage, 0f, "Black HP damage should be non-negative");
            Assert.AreEqual(result.RedHPDamage, result.FinalDamage * GameConstants.RED_HP_RATIO, 0.1f, "Red HP ratio should be 70%");
            Assert.AreEqual(result.BlackHPDamage, result.FinalDamage * GameConstants.BLACK_HP_RATIO, 0.1f, "Black HP ratio should be 30%");
        }

        [Test]
        public void DamageCalculator_LevelSuppression_ReducesDamage()
        {
            // Arrange
            int techniqueCapacity = 64;

            var attacker = new AttackerParams
            {
                CultivationLevel = 1,
                Strength = 10,
                Agility = 10,
                Intelligence = 10,
                Penetration = 0,
                AttackElement = Element.Neutral,
                CombatSubtype = CombatSubtype.MeleeStrike,
                TechniqueLevel = 1,
                TechniqueGrade = TechniqueGrade.Common,
                IsUltimate = false,
                IsQiTechnique = true
            };

            var defender = new DefenderParams
            {
                CultivationLevel = 4, // 3 levels higher
                CurrentQi = 0,
                QiDefense = QiDefenseType.None,
                Agility = 10,
                Strength = 10,
                ArmorCoverage = 0f,
                DamageReduction = 0f,
                ArmorValue = 0,
                DodgePenalty = 0,
                ParryBonus = 0,
                BlockBonus = 0,
                BodyMaterial = BodyMaterial.Organic
            };

            // Act
            var result = DamageCalculator.CalculateDamage(techniqueCapacity, attacker, defender);

            // Assert - With 3 level difference, technique should be at 5%
            Assert.Less(result.FinalDamage, result.RawDamage, "Level suppression should reduce damage");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void QiBuffer_ZeroDamage_NoConsumption()
        {
            // Arrange
            float incomingDamage = 0f;
            long currentQi = 1000;
            QiDefenseType defenseType = QiDefenseType.RawQi;

            // Act
            var result = QiBuffer.ProcessQiTechniqueDamage(incomingDamage, currentQi, defenseType);

            // Assert
            Assert.AreEqual(0f, result.AbsorbedDamage, "Zero damage should absorb nothing");
            Assert.AreEqual(0, result.QiConsumed, "Zero damage should consume no Qi");
        }

        [Test]
        public void LevelSuppression_NegativeLevelDiff_FullDamage()
        {
            // Edge case: negative level difference should still work
            float suppression = LevelSuppression.CalculateSuppression(5, 3, AttackType.Normal);
            Assert.AreEqual(1.0f, suppression, "Higher level attacker should deal full damage");
        }

        #endregion
    }

    // ============================================================================
    // Test Runner Helper (для запуска в Unity)
    // ============================================================================

    /// <summary>
    /// Помощник для запуска тестов в редакторе Unity.
    /// </summary>
    public static class TestRunnerHelper
    {
        public static void RunAllCombatTests()
        {
            Debug.Log("=== Running Combat Tests ===");

            var tests = new CombatTests();
            tests.Setup();

            // Получаем все методы тестов через рефлексию
            var testMethods = typeof(CombatTests).GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            int passed = 0;
            int failed = 0;

            foreach (var method in testMethods)
            {
                if (method.GetCustomAttributes(typeof(TestAttribute), false).Length > 0)
                {
                    try
                    {
                        method.Invoke(tests, null);
                        Debug.Log($"[PASS] {method.Name}");
                        passed++;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[FAIL] {method.Name}: {ex.InnerException?.Message ?? ex.Message}");
                        failed++;
                    }

                    tests.TearDown();
                    tests.Setup();
                }
            }

            tests.TearDown();

            Debug.Log($"=== Combat Tests Complete: {passed} passed, {failed} failed ===");
        }
    }
}
