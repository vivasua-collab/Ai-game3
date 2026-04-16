// ============================================================================
// FormationQiPoolTests.cs — Unit тесты пула Ци формации
// Cultivation World Simulator
// Создано: 2026-04-16 12:43 UTC
// ============================================================================
// Тестирует: FormationQiPool — AddQi, ConsumeQi, AcceptQi, ProcessDrain,
// FillPercent, IsReadyForActivation, IsEmpty, IsFull
// ============================================================================

using NUnit.Framework;
using CultivationGame.Formation;

namespace CultivationGame.Tests
{
    [TestFixture]
    public class FormationQiPoolTests
    {
        private FormationQiPool pool;

        [SetUp]
        public void Setup()
        {
            // Создаём пул: capacity=1000, drainInterval=60, drainAmount=1, conductivity=10
            pool = new FormationQiPool(1000, 60, 1, 10f);
        }

        [TearDown]
        public void TearDown()
        {
            pool = null;
        }

        // ====================================================================
        //  Инициализация
        // ====================================================================

        [Test]
        public void Constructor_SetsCorrectCapacity()
        {
            // Assert
            Assert.AreEqual(1000, pool.capacity, "Ёмкость должна быть 1000");
        }

        [Test]
        public void Constructor_StartsEmpty()
        {
            // Assert
            Assert.AreEqual(0, pool.currentQi, "Пул должен начинаться пустым");
            Assert.IsTrue(pool.IsEmpty, "IsEmpty должен быть true");
            Assert.IsFalse(pool.IsFull, "IsFull должен быть false");
        }

        // ====================================================================
        //  AddQi
        // ====================================================================

        [Test]
        public void AddQi_PositiveAmount_IncreasesCurrentQi()
        {
            // Act
            var result = pool.AddQi(500);

            // Assert
            Assert.Greater(result.currentQi, 0, "Ци в пуле должно увеличиться");
            Assert.AreEqual(500, pool.currentQi, "Ци должно быть 500");
        }

        [Test]
        public void AddQi_ExceedsCapacity_FillsToCapacity()
        {
            // Act
            pool.AddQi(2000);

            // Assert
            Assert.LessOrEqual(pool.currentQi, pool.capacity, "Ци не должно превышать ёмкость");
            Assert.IsTrue(pool.IsFull, "Пул должен быть полным");
        }

        [Test]
        public void AddQi_FillsToCapacity_SetsWasFilled()
        {
            // Act
            var result = pool.AddQi(1000);

            // Assert
            Assert.IsTrue(result.wasFilled, "wasFilled должен быть true при заполнении");
            Assert.IsTrue(pool.IsReadyForActivation, "Пул должен быть готов к активации");
        }

        // ====================================================================
        //  ConsumeQi
        // ====================================================================

        [Test]
        public void ConsumeQi_SufficientQi_ReturnsSuccess()
        {
            // Arrange
            pool.AddQi(500);

            // Act
            var result = pool.ConsumeQi(300);

            // Assert
            Assert.AreEqual(200, pool.currentQi, "После расхода должно остаться 200");
        }

        [Test]
        public void ConsumeQi_InsufficientQi_ReturnsFailure()
        {
            // Arrange
            pool.AddQi(100);

            // Act
            var result = pool.ConsumeQi(500);

            // Assert
            Assert.IsTrue(result.wasDepleted, "Пул должен быть истощён");
            Assert.IsTrue(pool.IsEmpty, "Пул должен быть пустым");
        }

        // ====================================================================
        //  FillPercent
        // ====================================================================

        [Test]
        public void FillPercent_Empty_ReturnsZero()
        {
            // Assert
            Assert.AreEqual(0.0f, pool.FillPercent, 0.01f, "Пустой пул = 0%");
        }

        [Test]
        public void FillPercent_Half_ReturnsHalf()
        {
            // Arrange
            pool.AddQi(500);

            // Assert
            Assert.AreEqual(0.5f, pool.FillPercent, 0.05f, "Половина = 50%");
        }

        [Test]
        public void FillPercent_Full_ReturnsOne()
        {
            // Arrange
            pool.AddQi(1000);

            // Assert
            Assert.AreEqual(1.0f, pool.FillPercent, 0.01f, "Полный = 100%");
        }

        // ====================================================================
        //  Drain (интервалы)
        // ====================================================================

        [Test]
        public void GetDrainInterval_Level1_Returns60()
        {
            // Act
            int interval = FormationQiPool.GetDrainInterval(1);

            // Assert
            Assert.AreEqual(60, interval, "Интервал стока L1 = 60 тиков");
        }

        [Test]
        public void GetDrainInterval_Level5_Returns20()
        {
            // Act
            int interval = FormationQiPool.GetDrainInterval(5);

            // Assert
            Assert.AreEqual(20, interval, "Интервал стока L5 = 20 тиков");
        }

        [Test]
        public void GetDrainAmount_Size0_Returns1()
        {
            // Act
            int amount = FormationQiPool.GetDrainAmount(0);

            // Assert
            Assert.AreEqual(1, amount, "Размер 0 = 1 единица стока");
        }

        [Test]
        public void GetDrainAmount_Size4_Returns100()
        {
            // Act
            int amount = FormationQiPool.GetDrainAmount(4);

            // Assert
            Assert.AreEqual(100, amount, "Размер 4 = 100 единиц стока");
        }

        // ====================================================================
        //  ProcessDrain
        // ====================================================================

        [Test]
        public void ProcessDrain_BeforeInterval_NoDrain()
        {
            // Arrange
            pool.AddQi(500);
            int beforeQi = (int)pool.currentQi;

            // Act — первый тик (интервал = 60)
            int drained = pool.ProcessDrain(1);

            // Assert — ещё рано для стока
            Assert.AreEqual(0, drained, "Сток не должен произойти до истечения интервала");
            Assert.AreEqual(beforeQi, pool.currentQi, "Ци не должно измениться");
        }

        [Test]
        public void ProcessDrain_AfterInterval_DrainsCorrectly()
        {
            // Arrange
            pool.AddQi(500);

            // Act — тик = 60 (интервал истёк)
            int drained = pool.ProcessDrain(60);

            // Assert
            Assert.AreEqual(1, drained, "Должно стечь 1 единица");
            Assert.AreEqual(499, pool.currentQi, "Остаток = 499");
        }

        // ====================================================================
        //  Reset / Fill
        // ====================================================================

        [Test]
        public void Reset_ClearsQi()
        {
            // Arrange
            pool.AddQi(500);

            // Act
            pool.Reset();

            // Assert
            Assert.IsTrue(pool.IsEmpty, "Пул должен быть пустым после Reset");
        }

        [Test]
        public void Fill_FillsToCapacity()
        {
            // Act
            pool.Fill();

            // Assert
            Assert.IsTrue(pool.IsFull, "Пул должен быть полным после Fill");
            Assert.AreEqual(pool.capacity, pool.currentQi, "Ци = ёмкость");
        }

        // ====================================================================
        //  Edge Cases
        // ====================================================================

        [Test]
        public void AddQi_ZeroAmount_NoChange()
        {
            // Arrange
            pool.AddQi(100);
            long before = pool.currentQi;

            // Act
            pool.AddQi(0);

            // Assert
            Assert.AreEqual(before, pool.currentQi, "AddQi(0) не должно менять Ци");
        }

        [Test]
        public void ConsumeQi_ZeroAmount_NoChange()
        {
            // Arrange
            pool.AddQi(100);

            // Act
            pool.ConsumeQi(0);

            // Assert
            Assert.AreEqual(100, pool.currentQi, "ConsumeQi(0) не должно менять Ци");
        }

        [Test]
        public void ProcessDrain_EmptyPool_NoNegativeQi()
        {
            // Act — сток из пустого пула
            pool.ProcessDrain(60);

            // Assert
            Assert.GreaterOrEqual(pool.currentQi, 0, "Ци не должно стать отрицательным");
        }
    }
}
