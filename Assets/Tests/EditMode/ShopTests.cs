using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    /// <summary>
    /// La boutique : perles, boosts temporaires et pubs récompensées. Le Core ne connaît
    /// ni SDK ni facturation — il applique des récompenses que l'hôte déclenche.
    /// </summary>
    public class ShopTests
    {
        static BalanceConfig Config() => BalanceConfig.Default();

        [Test]
        public void Discovery_PaysPearls()
        {
            var config = Config();
            var state = new GameState();

            var first = Simulation.CastLine(config, state, 0.0);
            Assert.That(first.newDiscovery, Is.True);
            Assert.That(state.pearls, Is.EqualTo(config.pearlsPerDiscovery).Within(1e-9));

            // La même espèce une deuxième fois ne paie plus : c'est la DÉCOUVERTE qui paie.
            Simulation.CastLine(config, state, 0.0);
            Assert.That(state.pearls, Is.EqualTo(config.pearlsPerDiscovery).Within(1e-9));
        }

        [Test]
        public void BuyBoost_SpendsPearls_AndRunsDown()
        {
            var config = Config();
            var state = new GameState { pearls = 100 };
            var def = Shop.Boost(config, "boost_net");

            Assert.That(Shop.BuyBoost(config, state, "boost_net"), Is.True);
            Assert.That(state.pearls, Is.EqualTo(100 - def.pearlCost).Within(1e-9));
            Assert.That(Shop.BoostSecondsLeft(state, "boost_net"), Is.EqualTo(def.durationSeconds).Within(1e-9));

            Simulation.Tick(config, state, 600);
            Assert.That(Shop.BoostSecondsLeft(state, "boost_net"),
                Is.EqualTo(def.durationSeconds - 600).Within(1e-6), "le temps de jeu consomme le boost");
        }

        [Test]
        public void BuyBoost_WithoutPearls_DoesNothing()
        {
            var config = Config();
            var state = new GameState { pearls = 3 };

            Assert.That(Shop.BuyBoost(config, state, "boost_net"), Is.False);
            Assert.That(Shop.IsBoostActive(state, "boost_net"), Is.False);
            Assert.That(state.pearls, Is.EqualTo(3).Within(1e-9));
        }

        [Test]
        public void Boost_StacksUpToItsCap()
        {
            var config = Config();
            var state = new GameState();
            var def = Shop.Boost(config, "boost_net");

            for (int i = 0; i < 5; i++) Shop.GrantBoost(config, state, "boost_net");

            Assert.That(Shop.BoostSecondsLeft(state, "boost_net"), Is.EqualTo(def.maxStackSeconds).Within(1e-9),
                "relancer prolonge, mais jamais au-delà du plafond");
        }

        [Test]
        public void FishingBoost_DoublesProduction()
        {
            var config = Config();
            var state = new GameState();
            double before = Multipliers.ProducerRate(config, state, "fisherman_t1");

            Shop.GrantBoost(config, state, "boost_net");

            Assert.That(Multipliers.ProducerRate(config, state, "fisherman_t1"),
                Is.EqualTo(before * 2).Within(1e-9));
            Assert.That(Multipliers.ManualCatch(config, state),
                Is.EqualTo(Multipliers.ManualCatch(config, new GameState()) * 2).Within(1e-9));
        }

        [Test]
        public void SailBoost_LeavesFishingAlone()
        {
            var config = Config();
            var state = new GameState();
            double before = Multipliers.ProducerRate(config, state, "fisherman_t1");

            Shop.GrantBoost(config, state, "boost_wind");

            Assert.That(Multipliers.ProducerRate(config, state, "fisherman_t1"), Is.EqualTo(before).Within(1e-9));
            Assert.That(Shop.BoostMultiplier(config, state, BoostKind.SailSpeed), Is.EqualTo(1.5).Within(1e-9));
        }

        [Test]
        public void RewardedAd_PaysOnce_ThenNeedsToRecharge()
        {
            var config = Config();
            var state = new GameState();
            var def = Shop.Ad(config, "ad_pearl");

            Assert.That(Shop.IsAdReady(config, state, "ad_pearl"), Is.True, "une pub est prête au premier lancement");
            Assert.That(Shop.ClaimAdReward(config, state, "ad_pearl"), Is.True);
            Assert.That(state.pearls, Is.EqualTo(def.pearls).Within(1e-9));

            Assert.That(Shop.ClaimAdReward(config, state, "ad_pearl"), Is.False, "pas deux fois de suite");
            Assert.That(state.pearls, Is.EqualTo(def.pearls).Within(1e-9));

            Simulation.Tick(config, state, def.cooldownSeconds);
            Assert.That(Shop.IsAdReady(config, state, "ad_pearl"), Is.True, "le délai écoulé, elle revient");
        }

        [Test]
        public void RewardedAd_CanPayInBoostTime()
        {
            var config = Config();
            var state = new GameState();

            Assert.That(Shop.ClaimAdReward(config, state, "ad_net"), Is.True);

            Assert.That(Shop.IsBoostActive(state, "boost_net"), Is.True);
            Assert.That(state.pearls, Is.EqualTo(0).Within(1e-9), "cette pub paie en temps, pas en perles");
        }

        [Test]
        public void PearlPack_IsCreditedByTheHost()
        {
            var config = Config();
            var state = new GameState();
            var pack = Shop.Pack(config, "pack_m");

            double granted = Shop.GrantPack(config, state, "pack_m");

            Assert.That(granted, Is.EqualTo(pack.pearls + pack.bonusPearls).Within(1e-9));
            Assert.That(state.pearls, Is.EqualTo(granted).Within(1e-9));
        }

        [Test]
        public void PearlChest_SpendsPearls_NotCoins()
        {
            var config = Config();
            var chest = Equipment.Chest(config, "chest_wood");
            var state = new GameState { pearls = chest.pearlCost, money = 42 };

            var result = Shop.OpenChestWithPearls(config, state, "chest_wood", 0.2);

            Assert.That(result, Is.Not.Null);
            Assert.That(state.pearls, Is.EqualTo(0).Within(1e-9));
            Assert.That(state.money, Is.EqualTo(42).Within(1e-9), "les pièces ne bougent pas");
            Assert.That(Equipment.Owns(state, result.equipmentId), Is.True);
        }

        [Test]
        public void PearlChest_WithoutPearls_DoesNothing()
        {
            var config = Config();
            var state = new GameState { pearls = 1, money = 1_000_000 };

            Assert.That(Shop.OpenChestWithPearls(config, state, "chest_gold", 0.5), Is.Null,
                "les pièces ne remplacent pas les perles");
            Assert.That(state.equipment, Is.Empty);
        }

        [Test]
        public void Prestige_KeepsPearls()
        {
            var config = Config();
            var state = new GameState { lifetimeMoney = 100_000_000, money = 500, pearls = 120 };

            Prestige.Execute(config, state);

            Assert.That(state.pearls, Is.EqualTo(120).Within(1e-9));
            Assert.That(state.money, Is.EqualTo(0).Within(1e-9));
        }
    }
}
