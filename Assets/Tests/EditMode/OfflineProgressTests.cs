using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    public class OfflineProgressTests
    {
        [Test]
        public void Apply_CapsSimulatedTime()
        {
            var config = TestConfigs.Simple();
            config.offlineCapSeconds = 2 * 3600;
            var state = new GameState();
            state.GetOrCreateProducer("fisher").count = 1;

            var result = OfflineProgress.Apply(config, state, 10 * 3600);

            Assert.That(result.simulatedSeconds, Is.EqualTo(2 * 3600));
            Assert.That(state.rawFish, Is.EqualTo(2 * 3600).Within(1e-6), "1/s pendant 2 h plafonnées");
        }

        [Test]
        public void Apply_MatchesChunkedSimulationStepByStep()
        {
            var config = TestConfigs.Simple();

            var offline = new GameState();
            offline.GetOrCreateProducer("fisher").count = 2;
            offline.GetOrCreateProducer("cutter").count = 1;

            var chunked = new GameState();
            chunked.GetOrCreateProducer("fisher").count = 2;
            chunked.GetOrCreateProducer("cutter").count = 1;

            OfflineProgress.Apply(config, offline, 600);
            for (int i = 0; i < 600 / (int)OfflineProgress.StepSeconds; i++)
                Simulation.Tick(config, chunked, OfflineProgress.StepSeconds, allowAutoSell: false);

            Assert.That(offline.rawFish, Is.EqualTo(chunked.rawFish).Within(1e-6));
            Assert.That(offline.cutFish, Is.EqualTo(chunked.cutFish).Within(1e-6));
        }

        [Test]
        public void Apply_DoesNotAutoSellAtSea()
        {
            // Même avec la vente auto débloquée, hors-ligne le poisson s'accumule dans la
            // cale au lieu d'être vendu : c'est elle qui plafonne le gain hors-ligne.
            var config = TestConfigs.Simple();
            config.baseHoldCapacity = 100;
            var state = new GameState { autoSellUnlocked = true };
            state.GetOrCreateProducer("fisher").count = 1;

            var result = OfflineProgress.Apply(config, state, 3600);

            Assert.That(result.moneyGained, Is.EqualTo(0).Within(1e-9), "pas de comptoir en mer");
            Assert.That(state.rawFish, Is.EqualTo(100).Within(1e-6), "production stoppée cale pleine");
            Assert.That(result.holdFull, Is.True);
        }

        [Test]
        public void Apply_NegativeElapsed_DoesNothing()
        {
            var config = TestConfigs.Simple();
            var state = new GameState();
            state.GetOrCreateProducer("fisher").count = 1;

            var result = OfflineProgress.Apply(config, state, -100);

            Assert.That(result.simulatedSeconds, Is.EqualTo(0));
            Assert.That(state.rawFish, Is.EqualTo(0));
        }

        [Test]
        public void Apply_ReportsStockGains()
        {
            var config = TestConfigs.Simple();
            var state = new GameState();
            state.GetOrCreateProducer("fisher").count = 1;

            var result = OfflineProgress.Apply(config, state, 120);

            Assert.That(result.stockGained, Is.EqualTo(120).Within(1e-6));
            Assert.That(result.holdFull, Is.False, "cale quasi illimitée dans la config de test");
        }
    }
}
