// ============================================================================
// BodySystemTests.cs — Unit тесты системы тела
// Cultivation World Simulator
// Создано: 2026-04-16 12:43 UTC
// Редактировано: 2026-04-17 20:00:00 UTC — FIX: BodyPart API (конструктор + методы)
// ============================================================================
// Тестирует: BodyDamage (CalculateDamagePenalty, IsAlive, GetOverallHealthPercent),
// BodyPart (конструктор, TakeDamage, ApplyDamage, Heal, UpdateState, IsFunctional,
// IsSevered, GetRedHPPercent), BodyController
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
        //  BodyDamage.IsAlive
        // ====================================================================

        [Test]
        public void IsAlive_AllPartsHealthy_ReturnsTrue()
        {
            // Arrange — CreateHumanoidBody создаёт здоровые части
            var parts = BodyDamage.CreateHumanoidBody(10);

            // Act
            bool alive = BodyDamage.IsAlive(parts);

            // Assert
            Assert.IsTrue(alive, "Все части здоровы — IsAlive должен быть true");
        }

        [Test]
        public void IsAlive_VitalPartDestroyed_ReturnsFalse()
        {
            // Arrange — голова жизненно важная, уничтожаем красную HP
            var parts = BodyDamage.CreateHumanoidBody(10);
            var head = parts.Find(p => p.PartType == BodyPartType.Head);

            // Наносим урон, убивающий голову (redHP = 0)
            head.ApplyDamage(head.MaxRedHP * 2f);

            // Act
            bool alive = BodyDamage.IsAlive(parts);

            // Assert
            Assert.IsFalse(alive, "Голова уничтожена — IsAlive должен быть false");
        }

        [Test]
        public void IsAlive_NonVitalPartDestroyed_StillAlive()
        {
            // Arrange — торс не жизненно важный (isVital=false)
            var parts = BodyDamage.CreateHumanoidBody(10);
            var torso = parts.Find(p => p.PartType == BodyPartType.Torso);

            // Наносим урон, убивающий торс
            torso.ApplyDamage(torso.MaxRedHP * 3f);

            // Act
            bool alive = BodyDamage.IsAlive(parts);

            // Assert — торс не жизненно важный, организм жив
            Assert.IsTrue(alive, "Торс не жизненно важный — IsAlive должен быть true");
        }

        [Test]
        public void IsAlive_HeartDestroyed_ReturnsFalse()
        {
            // Arrange — сердце жизненно важное
            var parts = BodyDamage.CreateHumanoidBody(10);
            var heart = parts.Find(p => p.PartType == BodyPartType.Heart);

            // Наносим урон, убивающий сердце
            heart.ApplyDamage(heart.MaxRedHP * 2f);

            // Act
            bool alive = BodyDamage.IsAlive(parts);

            // Assert
            Assert.IsFalse(alive, "Сердце уничтожено — IsAlive должен быть false");
        }

        // ====================================================================
        //  BodyDamage.GetOverallHealthPercent
        // ====================================================================

        [Test]
        public void GetOverallHealthPercent_FullHealth_ReturnsOne()
        {
            // Arrange
            var parts = BodyDamage.CreateHumanoidBody(10);

            // Act
            float health = BodyDamage.GetOverallHealthPercent(parts);

            // Assert
            Assert.AreEqual(1.0f, health, 0.01f, "Полное здоровье = 1.0");
        }

        [Test]
        public void GetOverallHealthPercent_AfterDamage_LessThanOne()
        {
            // Arrange
            var parts = BodyDamage.CreateHumanoidBody(10);
            var head = parts.Find(p => p.PartType == BodyPartType.Head);

            // Наносим урон в голову
            head.ApplyDamage(head.MaxRedHP * 0.5f);

            // Act
            float health = BodyDamage.GetOverallHealthPercent(parts);

            // Assert
            Assert.Less(health, 1.0f, "После урона здоровье < 1.0");
            Assert.Greater(health, 0.0f, "После частичного урона здоровье > 0.0");
        }

        // ====================================================================
        //  BodyDamage.CalculateDamagePenalty
        // ====================================================================

        [Test]
        public void CalculateDamagePenalty_NoDamage_ReturnsZero()
        {
            // Arrange
            var parts = BodyDamage.CreateHumanoidBody(10);

            // Act
            float penalty = BodyDamage.CalculateDamagePenalty(parts);

            // Assert
            Assert.AreEqual(0.0f, penalty, 0.01f, "Без повреждений штраф = 0");
        }

        [Test]
        public void CalculateDamagePenalty_SeveredPart_HasPenalty()
        {
            // Arrange — отрубаем руку (повреждение структурной HP до 0)
            var parts = BodyDamage.CreateHumanoidBody(10);
            var leftArm = parts.Find(p => p.PartType == BodyPartType.LeftArm);

            // Наносим огромный урон чтобы отрубить
            leftArm.ApplyDamage(leftArm.MaxRedHP * 10f);

            // Act
            float penalty = BodyDamage.CalculateDamagePenalty(parts);

            // Assert — отрубленная часть даёт штраф 0.3
            Assert.Greater(penalty, 0.0f, "Отрубленная часть даёт штраф");
        }

        [Test]
        public void CalculateDamagePenalty_MaxPenalty_CappedAt09()
        {
            // Arrange — отрубаем все части
            var parts = BodyDamage.CreateHumanoidBody(10);
            foreach (var part in parts)
            {
                part.ApplyDamage(part.MaxRedHP * 100f);
            }

            // Act
            float penalty = BodyDamage.CalculateDamagePenalty(parts);

            // Assert — максимум 90%
            Assert.LessOrEqual(penalty, 0.9f, "Штраф ограничен 0.9");
        }

        // ====================================================================
        //  BodyPart — конструктор и свойства
        // ====================================================================

        [Test]
        public void BodyPart_Constructor_SetsPartType()
        {
            // Act
            var part = new BodyPart(BodyPartType.Head, 50f, isVital: true);

            // Assert
            Assert.AreEqual(BodyPartType.Head, part.PartType, "PartType = Head");
            Assert.IsTrue(part.IsVital, "Голова — жизненно важная");
        }

        [Test]
        public void BodyPart_Constructor_SetsMaxRedHP()
        {
            // Act
            var part = new BodyPart(BodyPartType.Torso, 100f);

            // Assert
            Assert.AreEqual(100f, part.MaxRedHP, 0.01f, "MaxRedHP = 100");
            Assert.AreEqual(100f, part.CurrentRedHP, 0.01f, "CurrentRedHP = MaxRedHP при создании");
        }

        [Test]
        public void BodyPart_Constructor_Heart_HasZeroBlackHP()
        {
            // Act — сердце имеет ТОЛЬКО функциональную HP
            var heart = new BodyPart(BodyPartType.Heart, 80f, isVital: true);

            // Assert
            Assert.AreEqual(0f, heart.MaxBlackHP, 0.01f, "Сердце: MaxBlackHP = 0 (CORE-C01)");
            Assert.AreEqual(0f, heart.CurrentBlackHP, 0.01f, "Сердце: CurrentBlackHP = 0");
        }

        [Test]
        public void BodyPart_Constructor_NonHeart_HasBlackHP()
        {
            // Act — структурная HP = функциональная × STRUCTURAL_HP_MULTIPLIER
            var torso = new BodyPart(BodyPartType.Torso, 100f);

            // Assert
            Assert.Greater(torso.MaxBlackHP, 0f, "Торс должен иметь структурную HP");
            // MaxBlackHP = 100 * STRUCTURAL_HP_MULTIPLIER (обычно ×2)
            float expectedBlack = 100f * GameConstants.STRUCTURAL_HP_MULTIPLIER;
            Assert.AreEqual(expectedBlack, torso.MaxBlackHP, 0.01f,
                "Структурная HP = функциональная × STRUCTURAL_HP_MULTIPLIER");
        }

        [Test]
        public void BodyPart_Constructor_StartsHealthy()
        {
            // Act
            var part = new BodyPart(BodyPartType.LeftArm, 40f);

            // Assert
            Assert.AreEqual(BodyPartState.Healthy, part.State, "Новая часть = Healthy");
            Assert.IsTrue(part.IsFunctional(), "Новая часть функциональна");
            Assert.IsFalse(part.IsSevered(), "Новая часть не отрублена");
        }

        // ====================================================================
        //  BodyPart — ApplyDamage
        // ====================================================================

        [Test]
        public void BodyPart_ApplyDamage_ReducesRedHP()
        {
            // Arrange
            var part = new BodyPart(BodyPartType.Torso, 100f);
            float beforeHP = part.CurrentRedHP;

            // Act
            part.ApplyDamage(30f);

            // Assert — 70% от 30 = 21 урона в красную HP
            float expectedRedDamage = 30f * GameConstants.RED_HP_RATIO;
            Assert.AreEqual(beforeHP - expectedRedDamage, part.CurrentRedHP, 0.1f,
                "ApplyDamage уменьшает красную HP на 70% от урона");
        }

        [Test]
        public void BodyPart_ApplyDamage_ReducesBlackHP()
        {
            // Arrange
            var part = new BodyPart(BodyPartType.Torso, 100f);
            float beforeBlackHP = part.CurrentBlackHP;

            // Act
            part.ApplyDamage(30f);

            // Assert — 30% от 30 = 9 урона в чёрную HP
            float expectedBlackDamage = 30f * GameConstants.BLACK_HP_RATIO;
            Assert.AreEqual(beforeBlackHP - expectedBlackDamage, part.CurrentBlackHP, 0.1f,
                "ApplyDamage уменьшает чёрную HP на 30% от урона");
        }

        [Test]
        public void BodyPart_ApplyDamage_UpdatesState()
        {
            // Arrange
            var part = new BodyPart(BodyPartType.LeftArm, 40f);

            // Act — наносим урон < 70% красной HP (bruised)
            part.ApplyDamage(10f);

            // Assert
            Assert.AreEqual(BodyPartState.Bruised, part.State,
                "Урон < 70% MaxRedHP → Bruised");
        }

        [Test]
        public void BodyPart_ApplyDamage_WoundedState()
        {
            // Arrange
            var part = new BodyPart(BodyPartType.LeftArm, 100f);

            // Act — наносим урон > 70% но < 100% красной HP
            part.ApplyDamage(75f);

            // Assert — красная HP < 30% от макса → Wounded
            Assert.AreEqual(BodyPartState.Wounded, part.State,
                "Красная HP < 30% MaxRedHP → Wounded");
        }

        [Test]
        public void BodyPart_ApplyDamage_DisabledState()
        {
            // Arrange
            var part = new BodyPart(BodyPartType.LeftArm, 40f);

            // Act — убиваем красную HP (структурная ещё есть)
            part.ApplyDamage(200f);

            // Assert — красная HP ≤ 0, чёрная > 0 → Disabled
            Assert.AreEqual(BodyPartState.Disabled, part.State,
                "Красная HP = 0, чёрная > 0 → Disabled");
            Assert.IsFalse(part.IsFunctional(), "Disabled часть не функциональна");
        }

        [Test]
        public void BodyPart_ApplyDamage_SeveredState()
        {
            // Arrange
            var part = new BodyPart(BodyPartType.LeftArm, 40f);

            // Act — уничтожаем полностью (и красную, и чёрную)
            part.ApplyDamage(10000f);

            // Assert — чёрная HP ≤ 0 → Severed
            Assert.AreEqual(BodyPartState.Severed, part.State,
                "Чёрная HP = 0 → Severed");
            Assert.IsTrue(part.IsSevered(), "Severed = отрублена");
        }

        // ====================================================================
        //  BodyPart — Heal
        // ====================================================================

        [Test]
        public void BodyPart_Heal_IncreasesCurrentRedHP()
        {
            // Arrange
            var part = new BodyPart(BodyPartType.Torso, 100f);
            part.ApplyDamage(50f);
            float afterDamageHP = part.CurrentRedHP;

            // Act
            part.Heal(20f);

            // Assert
            Assert.Greater(part.CurrentRedHP, afterDamageHP,
                "Лечение увеличивает красную HP");
        }

        [Test]
        public void BodyPart_Heal_CappedAtMax()
        {
            // Arrange
            var part = new BodyPart(BodyPartType.Torso, 100f);
            part.ApplyDamage(10f);

            // Act — лечим больше чем потеряли
            part.Heal(1000f);

            // Assert
            Assert.LessOrEqual(part.CurrentRedHP, part.MaxRedHP,
                "Лечение не превышает MaxRedHP");
        }

        [Test]
        public void BodyPart_Heal_SeveredPart_ReturnsFalse()
        {
            // Arrange — отрубаем часть
            var part = new BodyPart(BodyPartType.LeftArm, 40f);
            part.ApplyDamage(10000f);
            Assert.IsTrue(part.IsSevered(), "Предусловие: часть отрублена");

            // Act
            bool result = part.Heal(100f);

            // Assert — отрубленную часть нельзя вылечить
            Assert.IsFalse(result, "Нельзя вылечить отрубленную часть");
        }

        // ====================================================================
        //  BodyPart — GetRedHPPercent / GetBlackHPPercent
        // ====================================================================

        [Test]
        public void BodyPart_GetRedHPPercent_Full_ReturnsOne()
        {
            // Arrange
            var part = new BodyPart(BodyPartType.Torso, 100f);

            // Assert
            Assert.AreEqual(1.0f, part.GetRedHPPercent(), 0.01f,
                "Полная красная HP = 1.0");
        }

        [Test]
        public void BodyPart_GetRedHPPercent_AfterDamage_LessThanOne()
        {
            // Arrange
            var part = new BodyPart(BodyPartType.Torso, 100f);
            part.ApplyDamage(70f); // 70% урона → красная HP снижена

            // Assert
            Assert.Less(part.GetRedHPPercent(), 1.0f,
                "После урона красная HP < 100%");
        }

        // ====================================================================
        //  BodyPart — Clone
        // ====================================================================

        [Test]
        public void BodyPart_Clone_CreatesIndependentCopy()
        {
            // Arrange
            var original = new BodyPart(BodyPartType.Head, 50f, isVital: true);
            original.ApplyDamage(20f);

            // Act
            var clone = original.Clone();

            // Assert — клон имеет то же состояние
            Assert.AreEqual(original.PartType, clone.PartType, "PartType совпадает");
            Assert.AreEqual(original.CurrentRedHP, clone.CurrentRedHP, 0.01f,
                "CurrentRedHP совпадает");
            Assert.AreEqual(original.State, clone.State, "State совпадает");

            // Наносим урон клону — оригинал не меняется
            clone.ApplyDamage(10f);
            Assert.AreNotEqual(original.CurrentRedHP, clone.CurrentRedHP,
                "Клон независим от оригинала");
        }

        // ====================================================================
        //  BodyDamage.CreateHumanoidBody
        // ====================================================================

        [Test]
        public void CreateHumanoidBody_CreatesCorrectPartCount()
        {
            // Act — гуманоид: Head, Torso, Heart, 2 Arms, 2 Hands, 2 Legs, 2 Feet = 11
            var parts = BodyDamage.CreateHumanoidBody(10);

            // Assert
            Assert.AreEqual(11, parts.Count,
                "Гуманоид = 11 частей тела (Head, Torso, Heart, 2 Arms, 2 Hands, 2 Legs, 2 Feet)");
        }

        [Test]
        public void CreateHumanoidBody_HeadIsVital()
        {
            // Act
            var parts = BodyDamage.CreateHumanoidBody(10);
            var head = parts.Find(p => p.PartType == BodyPartType.Head);

            // Assert
            Assert.IsNotNull(head, "Голова должна быть создана");
            Assert.IsTrue(head.IsVital, "Голова — жизненно важная");
        }

        [Test]
        public void CreateHumanoidBody_VitalityAffectsHP()
        {
            // Arrange
            var partsLow = BodyDamage.CreateHumanoidBody(5);
            var partsHigh = BodyDamage.CreateHumanoidBody(20);

            var headLow = partsLow.Find(p => p.PartType == BodyPartType.Head);
            var headHigh = partsHigh.Find(p => p.PartType == BodyPartType.Head);

            // Assert — больше живучести = больше HP
            Assert.Greater(headHigh.MaxRedHP, headLow.MaxRedHP,
                "Выше Vitality → больше HP");
        }
    }
}
