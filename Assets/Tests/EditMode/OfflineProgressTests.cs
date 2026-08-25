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
        public void Apply_MatchesOnlineSimulationStepByStep()
        {
            var config = TestConfigs.Simple();

            var offline = new GameState { autoSellUnlocked = true };
            offline.GetOrCreateProducer("fisher").count = 2;
            offline.GetOrCreateProducer("cutter").count = 1;

            var online = new GameState { autoSellUnlocked = true };
            online.GetOrCreateProducer("fisher").count = 2;
            online.GetOrCreateProducer("cutter").count = 1;

            OfflineProgress.Apply(config, offline, 600);
            for (int i = 0; i < 600 / (int)OfflineProgress.StepSeconds; i++)
                Simulation.Tick(config, online, OfflineProgress.StepSeconds);

            Assert.That(offline.money, Is.EqualTo(online.money).Within(1e-6));
            Assert.That(offline.rawFish, Is.EqualTo(online.rawFish).Within(1e-6));
            Assert.That(offline.cutFish, Is.EqualTo(online.cutFish).Within(1e-6));
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
        public void Apply_ReportsGains()
        {
            var config = TestConfigs.Simple();
            var state = new GameState { autoSellUnlocked = true };
            state.GetOrCreateProducer("fisher").count = 1;

            var result = OfflineProgress.Apply(config, state, 120);

            Assert.That(result.moneyGained, Is.EqualTo(120).Within(1e-6));
            Assert.That(state.money, Is.EqualTo(120).Within(1e-6));
        }
    }
}
