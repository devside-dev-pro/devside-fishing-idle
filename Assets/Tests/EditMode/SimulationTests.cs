using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    public class SimulationTests
    {
        [Test]
        public void Tick_PrimaryProducer_ProducesRateTimesDt()
        {
            var config = TestConfigs.Simple();
            var state = new GameState();
            state.GetOrCreateProducer("fisher").count = 3;

            Simulation.Tick(config, state, 10);

            Assert.That(state.rawFish, Is.EqualTo(30).Within(1e-9), "3 pêcheurs × 1/s × 10 s");
        }

        [Test]
        public void Tick_Station_IsLimitedByAvailableInput()
        {
            var config = TestConfigs.Simple();
            var state = TestConfigs.StateWith(rawFish: 3);
            state.GetOrCreateProducer("cutter").count = 1;

            Simulation.Tick(config, state, 10); // capacité 10, mais seulement 3 en stock

            Assert.That(state.cutFish, Is.EqualTo(3).Within(1e-9));
            Assert.That(state.rawFish, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void Tick_Chain_ConservesMatter()
        {
            var config = TestConfigs.Simple();
            var state = new GameState();
            state.GetOrCreateProducer("fisher").count = 2;  // 2 brut/s
            state.GetOrCreateProducer("cutter").count = 1;  // découpe 1/s (entrée 1:1)

            Simulation.Tick(config, state, 100);

            double produced = 2 * 100;
            Assert.That(state.rawFish + state.cutFish, Is.EqualTo(produced).Within(1e-6),
                "rien ne se perd dans la chaîne (ratio 1:1)");
            Assert.That(state.cutFish, Is.EqualTo(100).Within(1e-6), "l'atelier a tourné à plein régime");
        }

        [Test]
        public void Tick_StationCanConsumeSameTickProduction()
        {
            var config = TestConfigs.Simple();
            var state = new GameState();
            state.GetOrCreateProducer("fisher").count = 1;
            state.GetOrCreateProducer("cutter").count = 1;

            Simulation.Tick(config, state, 1); // stock initial nul : la découpe mange la production du tick

            Assert.That(state.cutFish, Is.EqualTo(1).Within(1e-9));
            Assert.That(state.rawFish, Is.EqualTo(0).Within(1e-9));
        }

        [Test]
        public void Tick_AutoSell_ConvertsLeftoversToMoney()
        {
            var config = TestConfigs.Simple();
            var state = new GameState { autoSellUnlocked = true };
            state.GetOrCreateProducer("fisher").count = 1;

            Simulation.Tick(config, state, 10);

            Assert.That(state.rawFish, Is.EqualTo(0).Within(1e-9));
            Assert.That(state.money, Is.EqualTo(10).Within(1e-9), "10 bruts vendus à 1 pièce");
        }

        [Test]
        public void CastLine_AppliesRodMultiplier()
        {
            var config = TestConfigs.Simple();
            var state = new GameState();
            state.GetOrCreateUpgrade("rod").level = 2; // ×2 par niveau → ×4

            double caught = Simulation.CastLine(config, state, 0).amount;

            Assert.That(caught, Is.EqualTo(4).Within(1e-9));
            Assert.That(state.rawFish, Is.EqualTo(4).Within(1e-9));
        }

        [Test]
        public void Training_MultipliesProducerOutput()
        {
            var config = TestConfigs.Simple();
            var state = new GameState();
            state.GetOrCreateProducer("fisher").count = 1;
            state.GetOrCreateUpgrade("training").level = 1; // ×2

            Simulation.Tick(config, state, 5);

            Assert.That(state.rawFish, Is.EqualTo(10).Within(1e-9));
        }

        [Test]
        public void Tick_NegativeOrZeroDt_DoesNothing()
        {
            var config = TestConfigs.Simple();
            var state = new GameState();
            state.GetOrCreateProducer("fisher").count = 1;

            Simulation.Tick(config, state, 0);
            Simulation.Tick(config, state, -5);

            Assert.That(state.rawFish, Is.EqualTo(0));
        }
    }
}
