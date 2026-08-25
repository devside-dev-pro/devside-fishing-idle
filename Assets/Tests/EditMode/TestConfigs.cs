using Devside.FishingIdle.Core;

namespace Devside.FishingIdle.Core.Tests
{
    /// <summary>
    /// Configs minimales construites à la main : les tests unitaires ne dépendent jamais de
    /// BalanceConfig.Default(), pour que l'équilibrage puisse bouger sans casser les tests.
    /// Seul PacingTests utilise la table réelle (c'est son rôle).
    /// </summary>
    public static class TestConfigs
    {
        /// <summary>Un pêcheur (1 brut/s, coût 10, ×1.5) + un atelier de découpe (1/s, entrée 1:1).</summary>
        public static BalanceConfig Simple()
        {
            var config = new BalanceConfig();
            config.sellPrices[ResourceId.RawFish] = 1;
            config.sellPrices[ResourceId.CutFish] = 5;
            config.sellPrices[ResourceId.Fillet] = 20;

            config.producers.Add(new ProducerDef
            {
                id = "fisher", output = ResourceId.RawFish,
                baseRate = 1, baseCost = 10, costGrowth = 1.5,
            });
            config.producers.Add(new ProducerDef
            {
                id = "cutter", output = ResourceId.CutFish,
                input = ResourceId.RawFish, inputPerOutput = 1,
                baseRate = 1, baseCost = 50, costGrowth = 1.5,
            });

            config.upgrades.Add(new UpgradeDef
            {
                id = "rod", effect = UpgradeEffect.ManualCatchMultiplier,
                multiplierPerLevel = 2, baseCost = 10, costGrowth = 2,
            });
            config.upgrades.Add(new UpgradeDef
            {
                id = "training", effect = UpgradeEffect.ProducerRateMultiplier,
                targetProducerId = "*", multiplierPerLevel = 2,
                baseCost = 100, costGrowth = 2,
            });
            config.upgrades.Add(new UpgradeDef
            {
                id = "auto", effect = UpgradeEffect.UnlockAutoSell,
                maxLevel = 1, baseCost = 100, costGrowth = 1,
            });

            return config;
        }

        public static GameState StateWith(double money = 0, double rawFish = 0)
            => new GameState { money = money, rawFish = rawFish };
    }
}
