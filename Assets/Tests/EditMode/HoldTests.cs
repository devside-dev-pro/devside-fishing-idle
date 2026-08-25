using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    /// <summary>
    /// La cale : plafonne le stock (donc l'accumulation hors-ligne), jamais le flux vendu
    /// au fil de l'eau. Voir docs/GAME-DESIGN.md, section économie.
    /// </summary>
    public class HoldTests
    {
        static BalanceConfig SmallHoldConfig(double capacity)
        {
            var config = TestConfigs.Simple();
            config.baseHoldCapacity = capacity;
            return config;
        }

        [Test]
        public void PrimaryProduction_StopsWhenHoldIsFull()
        {
            var config = SmallHoldConfig(10);
            var state = new GameState();
            state.GetOrCreateProducer("fisher").count = 1;

            Simulation.Tick(config, state, 30, allowAutoSell: false);

            Assert.That(state.rawFish, Is.EqualTo(10).Within(1e-9), "10 de capacité, pas un de plus");
        }

        [Test]
        public void AutoSell_MakesTheHoldIrrelevantOnline()
        {
            var config = SmallHoldConfig(10);
            var state = new GameState { autoSellUnlocked = true };
            state.GetOrCreateProducer("fisher").count = 1;

            Simulation.Tick(config, state, 30);

            Assert.That(state.money, Is.EqualTo(30).Within(1e-9),
                "le poisson vendu au fil de l'eau ne passe pas par la limite de la cale");
            Assert.That(state.TotalFishStock, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void Transformations_CompressTheStockAndFreeSpace()
        {
            var config = SmallHoldConfig(10);
            config.producers.Add(new ProducerDef
            {
                id = "presser", output = ResourceId.Fillet,
                input = ResourceId.CutFish, inputPerOutput = 2,
                baseRate = 5, baseCost = 1, costGrowth = 1.5,
            });
            var state = new GameState { cutFish = 10 }; // cale pleine
            state.GetOrCreateProducer("presser").count = 1;

            Simulation.Tick(config, state, 1, allowAutoSell: false);

            Assert.That(state.fillet, Is.EqualTo(5).Within(1e-9));
            Assert.That(state.TotalFishStock, Is.EqualTo(5).Within(1e-9),
                "2 découpés → 1 filet : la transformation libère de la place");
        }

        [Test]
        public void CargoHoldUpgrade_MultipliesCapacity()
        {
            var config = SmallHoldConfig(10);
            config.upgrades.Add(new UpgradeDef
            {
                id = "hold", effect = UpgradeEffect.HoldCapacityMultiplier,
                multiplierPerLevel = 2, baseCost = 1, costGrowth = 2,
            });
            var state = new GameState();
            state.GetOrCreateUpgrade("hold").level = 2; // capacité ×4 → 40
            state.GetOrCreateProducer("fisher").count = 1;

            Simulation.Tick(config, state, 100, allowAutoSell: false);

            Assert.That(state.rawFish, Is.EqualTo(40).Within(1e-9));
        }

        [Test]
        public void CastLine_CatchesNothingWhenHoldIsFull()
        {
            var config = SmallHoldConfig(5);
            var state = new GameState { rawFish = 5 };

            var result = Simulation.CastLine(config, state, 0.5);

            Assert.That(result.amount, Is.EqualTo(0));
            Assert.That(result.speciesId, Is.Null);
            Assert.That(state.rawFish, Is.EqualTo(5));
        }
    }
}
