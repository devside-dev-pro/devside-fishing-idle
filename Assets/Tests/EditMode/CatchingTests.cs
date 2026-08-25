using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    public class CatchingTests
    {
        /// <summary>3 espèces : 2 en surface (commune w3, rare w1), 1 en profondeur 1 (w100).</summary>
        static BalanceConfig ConfigWithSpecies()
        {
            var config = TestConfigs.Simple();
            config.species.Add(new SpeciesDef { id = "common", minDepthLevel = 0, weight = 3, valueMultiplier = 1, discoveryBonus = 1.5 });
            config.species.Add(new SpeciesDef { id = "rare", minDepthLevel = 0, weight = 1, valueMultiplier = 10, discoveryBonus = 2 });
            config.species.Add(new SpeciesDef { id = "deep", minDepthLevel = 1, weight = 100, valueMultiplier = 5, discoveryBonus = 1.1 });
            config.upgrades.Add(new UpgradeDef
            {
                id = "hull", effect = UpgradeEffect.DepthLevel,
                maxLevel = 3, baseCost = 1000, costGrowth = 2,
            });
            return config;
        }

        [Test]
        public void PickSpecies_NeverPicksSpeciesBeyondCurrentDepth()
        {
            var config = ConfigWithSpecies();
            var state = new GameState(); // profondeur 0

            for (double roll = 0; roll < 1; roll += 0.05)
                Assert.That(Catching.PickSpecies(config, state, roll).id, Is.Not.EqualTo("deep"),
                    $"roll {roll} : une espèce profonde est sortie en surface");
        }

        [Test]
        public void PickSpecies_IsWeightedAndDeterministic()
        {
            var config = ConfigWithSpecies();
            var state = new GameState();

            // Poids cumulés : common 0–3, rare 3–4 (total 4).
            Assert.That(Catching.PickSpecies(config, state, 0.5).id, Is.EqualTo("common"));
            Assert.That(Catching.PickSpecies(config, state, 0.8).id, Is.EqualTo("rare"));
            Assert.That(Catching.PickSpecies(config, state, 0.999).id, Is.EqualTo("rare"));
        }

        [Test]
        public void PickSpecies_HullUpgradeUnlocksDeepSpecies()
        {
            var config = ConfigWithSpecies();
            var state = new GameState();
            state.GetOrCreateUpgrade("hull").level = 1;

            Assert.That(Catching.DepthLevel(config, state), Is.EqualTo(1));
            // Total 104, deep pèse 100 : roll 0.5 → cible 52 → deep.
            Assert.That(Catching.PickSpecies(config, state, 0.5).id, Is.EqualTo("deep"));
        }

        [Test]
        public void RegisterCatch_IsADiscoveryOnlyOnce()
        {
            var state = new GameState();

            Assert.That(Catching.RegisterCatch(state, "common"), Is.True);
            Assert.That(Catching.RegisterCatch(state, "common"), Is.False);
            Assert.That(state.discoveredSpecies.Count, Is.EqualTo(1));
        }

        [Test]
        public void CollectionBonus_MultipliesProduction()
        {
            var config = ConfigWithSpecies();
            var state = new GameState();
            state.discoveredSpecies.Add("common"); // bonus ×1.5
            state.GetOrCreateProducer("fisher").count = 1;

            Simulation.Tick(config, state, 10);

            Assert.That(state.rawFish, Is.EqualTo(15).Within(1e-9),
                "1/s × 10 s × 1.5 de bonus de collection");
        }

        [Test]
        public void CastLine_AppliesSpeciesValueAndRegistersDiscovery()
        {
            var config = ConfigWithSpecies();
            var state = new GameState();

            var result = Simulation.CastLine(config, state, 0.8); // → rare, ×10

            Assert.That(result.speciesId, Is.EqualTo("rare"));
            Assert.That(result.newDiscovery, Is.True);
            Assert.That(result.amount, Is.EqualTo(10).Within(1e-9));
            Assert.That(state.rawFish, Is.EqualTo(10).Within(1e-9));

            var again = Simulation.CastLine(config, state, 0.8);
            Assert.That(again.newDiscovery, Is.False);
        }

        [Test]
        public void CastLine_WithoutSpeciesTable_StillCatches()
        {
            var config = TestConfigs.Simple(); // aucune espèce définie
            var state = new GameState();

            var result = Simulation.CastLine(config, state, 0.5);

            Assert.That(result.speciesId, Is.Null);
            Assert.That(result.amount, Is.EqualTo(1).Within(1e-9));
        }
    }
}
