// ============================================================================
// BodySystemTests.cs — Unit тесты системы тела
// Cultivation World Simulator
// Создано: 2026-04-16 12:43 UTC
// ============================================================================
// Тестирует: BodyDamage (CalculateDamagePenalty, IsAlive, GetOverallHealthPercent),
// BodyPart, BodyController
// ============================================================================

using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using CultivationGame.Core;
using CultivationGame.Body;

namespace CultivationGame.Tests
{
    [TestFixture]
    public class BodySystemTests
    {
        private GameObject testObj;
        private BodyController body;

        [SetUp]
        public void Setup()
        {
            testObj = new GameObject("TestBody");
            testObj.SetActive(false);
            body = testObj.AddComponent<BodyController>();
        }

        [TearDown]
        public void TearDown()
        {
            if (testObj != null)
            {
                Object.DestroyImmediate(testObj);
            }
        }

        // ====================================================================
        //  BodyDamage — статические методы
        // ====================================================================

        [Test]
        public void IsAlive_AllPartsHealthy_ReturnsTrue()
        {
            // Arrange
            var parts = CreateHealthyParts();

            // Act
            bool alive = BodyDamage.IsAlive(parts);

            // Assert
            Assert.IsTrue(alive, "Все части здоровы — IsAlive должен быть true");
        }

        [Test]
        public void IsAlive_AllPartsDestroyed_ReturnsFalse()
        {
            // Arrange
            var parts = CreateDestroyedParts();

            // Act
            bool alive = BodyDamage.IsAlive(parts);

            // Assert
            Assert.IsFalse(alive, "Все части уничтожены — IsAlive должен быть false");
        }

        [Test]
        public void GetOverallHealthPercent_FullHealth_ReturnsOne()
        {
            // Arrange
            var parts = CreateHealthyParts();

            // Act
            float health = BodyDamage.GetOverallHealthPercent(parts);

            // Assert
            Assert.AreEqual(1.0f, health, 0.01f, "Полное здоровье = 1.0");
        }

        [Test]
        public void GetOverallHealthPercent_NoHealth_ReturnsZero()
        {
            // Arrange
            var parts = CreateDestroyedParts();

            // Act
            float health = BodyDamage.GetOverallHealthPercent(parts);

            // Assert
            Assert.AreEqual(0.0f, health, 0.01f, "Нет здоровья = 0.0");
        }

        [Test]
        public void CalculateDamagePenalty_NoDamage_ReturnsZero()
        {
            // Arrange
            var parts = CreateHealthyParts();

            // Act
            float penalty = BodyDamage.CalculateDamagePenalty(parts);

            // Assert
            Assert.AreEqual(0.0f, penalty, 0.01f, "Без повреждений штраф = 0");
        }

        // ====================================================================
        //  Вспомогательные методы
        // ====================================================================

        private List<BodyPart> CreateHealthyParts()
        {
            var parts = new List<BodyPart>
            {
                new BodyPart { name = "Head", maxHealth = 100, currentHealth = 100 },
                new BodyPart { name = "Torso", maxHealth = 200, currentHealth = 200 },
                new BodyPart { name = "LeftArm", maxHealth = 80, currentHealth = 80 },
                new BodyPart { name = "RightArm", maxHealth = 80, currentHealth = 80 },
                new BodyPart { name = "LeftLeg", maxHealth = 100, currentHealth = 100 },
                new BodyPart { name = "RightLeg", maxHealth = 100, currentHealth = 100 },
            };
            return parts;
        }

        private List<BodyPart> CreateDestroyedParts()
        {
            var parts = new List<BodyPart>
            {
                new BodyPart { name = "Head", maxHealth = 100, currentHealth = 0 },
                new BodyPart { name = "Torso", maxHealth = 200, currentHealth = 0 },
                new BodyPart { name = "LeftArm", maxHealth = 80, currentHealth = 0 },
                new BodyPart { name = "RightArm", maxHealth = 80, currentHealth = 0 },
                new BodyPart { name = "LeftLeg", maxHealth = 100, currentHealth = 0 },
                new BodyPart { name = "RightLeg", maxHealth = 100, currentHealth = 0 },
            };
            return parts;
        }
    }
}
