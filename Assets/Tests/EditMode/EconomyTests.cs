using System;
using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    public class EconomyTests
    {
        [Test]
        public void GeometricCost_SingleItem_MatchesPower()
        {
            double cost = Economy.GeometricCost(10, 1.15, 3, 1);
            Assert.That(cost, Is.EqualTo(10 * Math.Pow(1.15, 3)).Within(1e-9));
        }

        [Test]
        public void GeometricCost_Series_EqualsSumOfSingles()
        {
            double series = Economy.GeometricCost(10, 1.15, 2, 5);
            double sum = 0;
            for (int i = 0; i < 5; i++) sum += Economy.GeometricCost(10, 1.15, 2 + i, 1);
            Assert.That(series, Is.EqualTo(sum).Within(1e-9));
        }

        [Test]
        public void GeometricCost_GrowthOne_IsLinear()
        {
            Assert.That(Economy.GeometricCost(10, 1, 7, 3), Is.EqualTo(30).Within(1e-9));
        }

        [TestCase(0.0)]
        [TestCase(9.99)]
        [TestCase(10.0)]
        [TestCase(157.0)]
        [TestCase(12345.0)]
        [TestCase(1e9)]
        public void MaxAffordable_IsConsistentWithCost(double funds)
        {
            const double baseCost = 10;
            const double growth = 1.15;
            const int owned = 4;

            int n = Economy.MaxAffordable(baseCost, growth, owned, funds);

            Assert.That(n, Is.GreaterThanOrEqualTo(0));
            Assert.That(Economy.GeometricCost(baseCost, growth, owned, n), Is.LessThanOrEqualTo(funds),
                "le lot acheté doit être payable");
            Assert.That(Economy.GeometricCost(baseCost, growth, owned, n + 1), Is.GreaterThan(funds),
                "un exemplaire de plus ne doit pas être payable");
        }

        [Test]
        public void TryBuyProducer_DeductsMoneyAndIncrements()
        {
            var config = TestConfigs.Simple();
            var state = TestConfigs.StateWith(money: 100);

            bool bought = Economy.TryBuyProducer(config, state, "fisher");

            Assert.That(bought, Is.True);
            Assert.That(state.money, Is.EqualTo(90).Within(1e-9));
            Assert.That(state.ProducerCount("fisher"), Is.EqualTo(1));
        }

        [Test]
        public void TryBuyProducer_RefusesWhenBroke()
        {
            var config = TestConfigs.Simple();
            var state = TestConfigs.StateWith(money: 5);

            Assert.That(Economy.TryBuyProducer(config, state, "fisher"), Is.False);
            Assert.That(state.money, Is.EqualTo(5));
            Assert.That(state.ProducerCount("fisher"), Is.EqualTo(0));
        }

        [Test]
        public void TryBuyUpgrade_RespectsMaxLevel()
        {
            var config = TestConfigs.Simple();
            var state = TestConfigs.StateWith(money: 10_000);

            Assert.That(Economy.TryBuyUpgrade(config, state, "auto"), Is.True);
            Assert.That(state.autoSellUnlocked, Is.True);
            Assert.That(Economy.TryBuyUpgrade(config, state, "auto"), Is.False, "niveau max atteint");
        }

        [Test]
        public void Buying_DoesNotReduceLifetimeMoney()
        {
            var config = TestConfigs.Simple();
            var state = new GameState();
            state.AddResource(ResourceId.Money, 100); // passe par AddResource pour créditer lifetime

            Economy.TryBuyProducer(config, state, "fisher");

            Assert.That(state.lifetimeMoney, Is.EqualTo(100).Within(1e-9));
        }

        [Test]
        public void Sell_CreditsPriceAndClampsToStock()
        {
            var config = TestConfigs.Simple();
            var state = TestConfigs.StateWith(rawFish: 3);

            double gained = Economy.Sell(config, state, ResourceId.RawFish, 10);

            Assert.That(gained, Is.EqualTo(3).Within(1e-9), "3 poissons à 1 pièce");
            Assert.That(state.rawFish, Is.EqualTo(0).Within(1e-9));
            Assert.That(state.money, Is.EqualTo(3).Within(1e-9));
        }

        [Test]
        public void SellAll_EmptiesEveryStock()
        {
            var config = TestConfigs.Simple();
            var state = new GameState { rawFish = 2, cutFish = 3, fillet = 1 };

            double gained = Economy.SellAll(config, state);

            Assert.That(gained, Is.EqualTo(2 * 1 + 3 * 5 + 1 * 20).Within(1e-9));
            Assert.That(state.rawFish + state.cutFish + state.fillet, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void SellAll_AppliesHostPriceMultiplier()
        {
            var config = TestConfigs.Simple();
            var state = new GameState { rawFish = 4 };

            // Bonus contextuel de la couche hôte (comptoir du marchand : bateau à quai).
            double gained = Economy.SellAll(config, state, priceMultiplier: 1.25);

            Assert.That(gained, Is.EqualTo(4 * 1 * 1.25).Within(1e-9));
            Assert.That(state.money, Is.EqualTo(5).Within(1e-9));
        }
    }
}
