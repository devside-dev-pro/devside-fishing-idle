using System;

namespace Devside.FishingIdle.Core
{
    /// <summary>Coûts, achats et ventes. Toutes les courbes de coût sont géométriques.</summary>
    public static class Economy
    {
        /// <summary>Coût des <paramref name="count"/> prochains exemplaires quand on en possède <paramref name="owned"/>.</summary>
        public static double ProducerCost(ProducerDef def, int owned, int count = 1)
            => GeometricCost(def.baseCost, def.costGrowth, owned, count);

        public static double UpgradeCost(UpgradeDef def, int currentLevel, int count = 1)
            => GeometricCost(def.baseCost, def.costGrowth, currentLevel, count);

        /// <summary>Somme base×g^owned + … + base×g^(owned+count-1).</summary>
        public static double GeometricCost(double baseCost, double growth, int owned, int count)
        {
            if (count <= 0) return 0;
            if (Math.Abs(growth - 1) < 1e-9) return baseCost * count;
            double first = baseCost * Math.Pow(growth, owned);
            return first * (Math.Pow(growth, count) - 1) / (growth - 1);
        }

        /// <summary>Nombre maximal d'exemplaires payables avec <paramref name="funds"/>.</summary>
        public static int MaxAffordable(double baseCost, double growth, int owned, double funds)
        {
            if (funds <= 0 || baseCost <= 0) return 0;
            if (Math.Abs(growth - 1) < 1e-9) return (int)Math.Floor(funds / baseCost);

            double first = baseCost * Math.Pow(growth, owned);
            if (funds < first) return 0;

            int n = (int)Math.Floor(Math.Log(funds * (growth - 1) / first + 1, growth));
            // Corrige les erreurs d'arrondi flottant aux frontières.
            while (n > 0 && GeometricCost(baseCost, growth, owned, n) > funds) n--;
            while (GeometricCost(baseCost, growth, owned, n + 1) <= funds) n++;
            return n;
        }

        public static bool TryBuyProducer(BalanceConfig config, GameState state, string producerId, int count = 1)
        {
            var def = config.Producer(producerId);
            var owned = state.GetOrCreateProducer(producerId);
            double cost = ProducerCost(def, owned.count, count);
            if (state.money < cost) return false;

            state.AddResource(ResourceId.Money, -cost);
            owned.count += count;
            return true;
        }

        public static bool TryBuyUpgrade(BalanceConfig config, GameState state, string upgradeId)
        {
            var def = config.Upgrade(upgradeId);
            var owned = state.GetOrCreateUpgrade(upgradeId);
            if (owned.level >= def.maxLevel) return false;

            double cost = UpgradeCost(def, owned.level);
            if (state.money < cost) return false;

            state.AddResource(ResourceId.Money, -cost);
            owned.level++;
            if (def.effect == UpgradeEffect.UnlockAutoSell) state.autoSellUnlocked = true;
            return true;
        }

        /// <summary>Vend jusqu'à <paramref name="amount"/> unités ; renvoie l'argent encaissé.</summary>
        public static double Sell(BalanceConfig config, GameState state, ResourceId resource, double amount)
        {
            double stock = state.GetResource(resource);
            double sold = Math.Min(Math.Max(amount, 0), stock);
            if (sold <= 0) return 0;

            double gained = sold * config.SellPrice(resource) * Multipliers.SellPrice(config, state);
            state.AddResource(resource, -sold);
            state.AddResource(ResourceId.Money, gained);
            return gained;
        }

        /// <summary>Vend tout le stock de poisson (brut, découpé, filets) ; renvoie l'argent encaissé.</summary>
        public static double SellAll(BalanceConfig config, GameState state)
        {
            double gained = 0;
            gained += Sell(config, state, ResourceId.RawFish, state.rawFish);
            gained += Sell(config, state, ResourceId.CutFish, state.cutFish);
            gained += Sell(config, state, ResourceId.Fillet, state.fillet);
            return gained;
        }
    }
}
