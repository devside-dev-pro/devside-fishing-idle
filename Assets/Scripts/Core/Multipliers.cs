using System;

namespace Devside.FishingIdle.Core
{
    /// <summary>Agrège les multiplicateurs issus des améliorations et du prestige.</summary>
    public static class Multipliers
    {
        public static double ManualCatch(BalanceConfig config, GameState state)
            => Prestige.ProductionMultiplier(config, state)
               * Catching.CollectionBonus(config, state)
               * FromUpgrades(config, state, UpgradeEffect.ManualCatchMultiplier, null);

        public static double ProducerRate(BalanceConfig config, GameState state, string producerId)
            => Prestige.ProductionMultiplier(config, state)
               * Catching.CollectionBonus(config, state)
               * FromUpgrades(config, state, UpgradeEffect.ProducerRateMultiplier, producerId);

        public static double SellPrice(BalanceConfig config, GameState state)
            => FromUpgrades(config, state, UpgradeEffect.SellPriceMultiplier, null);

        public static double HoldCapacity(BalanceConfig config, GameState state)
            => config.baseHoldCapacity
               * FromUpgrades(config, state, UpgradeEffect.HoldCapacityMultiplier, null);

        /// <summary>
        /// Produit des multiplicateurs de toutes les améliorations possédées ayant cet effet.
        /// <paramref name="producerId"/> ne filtre que les effets ciblant un producteur.
        /// </summary>
        static double FromUpgrades(BalanceConfig config, GameState state, UpgradeEffect effect, string producerId)
        {
            double multiplier = 1;
            for (int i = 0; i < config.upgrades.Count; i++)
            {
                var def = config.upgrades[i];
                if (def.effect != effect) continue;
                if (effect == UpgradeEffect.ProducerRateMultiplier
                    && def.targetProducerId != "*"
                    && def.targetProducerId != producerId) continue;

                int level = state.UpgradeLevel(def.id);
                if (level > 0) multiplier *= Math.Pow(def.multiplierPerLevel, level);
            }
            return multiplier;
        }
    }
}
