using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    public class PrestigeTests
    {
        static BalanceConfig Config()
        {
            var config = TestConfigs.Simple();
            config.prestigeBase = 1_000_000;
            config.prestigeBonusPerPoint = 0.02;
            return config;
        }

        [TestCase(0, 0)]
        [TestCase(999_999, 0)]
        [TestCase(1_000_000, 1)]
        [TestCase(3_999_999, 1)]
        [TestCase(4_000_000, 2)]
        [TestCase(100_000_000, 10)]
        public void PointsFor_FollowsSquareRootCurve(double lifetime, int expected)
        {
            Assert.That(Prestige.PointsFor(Config(), lifetime), Is.EqualTo(expected));
        }

        [Test]
        public void PendingPoints_SubtractsAlreadyOwnedPoints()
        {
            var state = new GameState { lifetimeMoney = 4_000_000, prestigePoints = 1 };
            Assert.That(Prestige.PendingPoints(Config(), state), Is.EqualTo(1));
        }

        [Test]
        public void Execute_ResetsRunButKeepsPermanents()
        {
            var config = Config();
            var state = new GameState
            {
                money = 123, rawFish = 45, lifetimeMoney = 4_000_000, autoSellUnlocked = true,
            };
            state.GetOrCreateProducer("fisher").count = 10;
            state.discoveredSpecies.Add("sardine");

            int gained = Prestige.Execute(config, state);

            Assert.That(gained, Is.EqualTo(2));
            Assert.That(state.discoveredSpecies, Does.Contain("sardine"),
                "le Poissodex survit au prestige");
            Assert.That(state.prestigePoints, Is.EqualTo(2));
            Assert.That(state.lifetimeMoney, Is.EqualTo(4_000_000), "la richesse cumulée survit au reset");
            Assert.That(state.money, Is.EqualTo(0));
            Assert.That(state.rawFish, Is.EqualTo(0));
            Assert.That(state.autoSellUnlocked, Is.False);
            Assert.That(state.ProducerCount("fisher"), Is.EqualTo(0));
        }

        [Test]
        public void Execute_WithNothingPending_IsANoOp()
        {
            var config = Config();
            var state = new GameState { money = 50, lifetimeMoney = 10 };

            Assert.That(Prestige.Execute(config, state), Is.EqualTo(0));
            Assert.That(state.money, Is.EqualTo(50), "pas de reset sans points à encaisser");
        }

        [Test]
        public void ProductionMultiplier_BoostsTickAndCastLine()
        {
            var config = Config();
            var state = new GameState { prestigePoints = 50 }; // 1 + 50×0.02 = ×2
            state.GetOrCreateProducer("fisher").count = 1;

            Simulation.Tick(config, state, 10);
            double caught = Simulation.CastLine(config, state, 0).amount;

            Assert.That(state.rawFish, Is.EqualTo(20 + caught).Within(1e-9));
            Assert.That(caught, Is.EqualTo(2).Within(1e-9));
        }
    }
}
