using System;
using System.Collections.Generic;

namespace Devside.FishingIdle.Core
{
    /// <summary>
    /// Un producteur : génère une ressource en continu, éventuellement en consommant une
    /// autre (poste de transformation). Les pêcheurs et les ateliers partagent ces maths.
    /// </summary>
    public class ProducerDef
    {
        public string id;
        public ResourceId output;

        /// <summary>Unités produites par seconde et par exemplaire (avant multiplicateurs).</summary>
        public double baseRate;

        /// <summary>Ressource consommée, ou null pour un producteur primaire (pêcheur).</summary>
        public ResourceId? input;

        /// <summary>Unités d'entrée consommées pour produire 1 unité de sortie.</summary>
        public double inputPerOutput = 1;

        public double baseCost;
        public double costGrowth;
    }

    public enum UpgradeEffect
    {
        /// <summary>Multiplie la pêche manuelle (canne).</summary>
        ManualCatchMultiplier,
        /// <summary>Multiplie le débit d'un producteur (targetProducerId, "*" = tous).</summary>
        ProducerRateMultiplier,
        /// <summary>Multiplie tous les prix de vente.</summary>
        SellPriceMultiplier,
        /// <summary>Multiplie le plafond de gains hors-ligne.</summary>
        OfflineCapMultiplier,
        /// <summary>Débloque la vente automatique (niveau max 1).</summary>
        UnlockAutoSell,
    }

    public class UpgradeDef
    {
        public string id;
        public UpgradeEffect effect;
        public string targetProducerId = "*";

        /// <summary>Multiplicateur appliqué par niveau (2 = production doublée à chaque niveau).</summary>
        public double multiplierPerLevel = 1;

        public int maxLevel = int.MaxValue;
        public double baseCost;
        public double costGrowth;
    }

    /// <summary>
    /// Toute la table d'équilibrage du jeu. Ids stables uniquement, aucun libellé affichable
    /// (les noms/icônes vivront dans la couche thème côté Unity). Voir docs/GAME-DESIGN.md.
    /// </summary>
    public class BalanceConfig
    {
        /// <summary>Poissons attrapés par lancer manuel, avant multiplicateurs.</summary>
        public double baseManualCatch = 1;

        /// <summary>Plafond de simulation hors-ligne, en secondes (avant multiplicateurs).</summary>
        public double offlineCapSeconds = 2 * 3600;

        /// <summary>Richesse cumulée donnant droit au premier point de prestige.</summary>
        public double prestigeBase = 1_000_000;

        /// <summary>Bonus de production par point de prestige (0.02 = +2 %).</summary>
        public double prestigeBonusPerPoint = 0.02;

        public Dictionary<ResourceId, double> sellPrices = new Dictionary<ResourceId, double>();
        public List<ProducerDef> producers = new List<ProducerDef>();
        public List<UpgradeDef> upgrades = new List<UpgradeDef>();

        public ProducerDef Producer(string id)
        {
            for (int i = 0; i < producers.Count; i++)
                if (producers[i].id == id)
                    return producers[i];
            throw new ArgumentException($"Producteur inconnu : {id}");
        }

        public UpgradeDef Upgrade(string id)
        {
            for (int i = 0; i < upgrades.Count; i++)
                if (upgrades[i].id == id)
                    return upgrades[i];
            throw new ArgumentException($"Amélioration inconnue : {id}");
        }

        public double SellPrice(ResourceId resource)
            => sellPrices.TryGetValue(resource, out double price) ? price : 0;

        /// <summary>
        /// Table v1. Contrainte : les producteurs primaires doivent être listés avant les
        /// postes qui consomment leur sortie (la simulation traite la liste dans l'ordre).
        /// </summary>
        public static BalanceConfig Default()
        {
            var config = new BalanceConfig();

            config.sellPrices[ResourceId.RawFish] = 1;
            config.sellPrices[ResourceId.CutFish] = 4;
            config.sellPrices[ResourceId.Fillet] = 12;

            config.producers.Add(new ProducerDef
            {
                id = "fisherman_t1", output = ResourceId.RawFish,
                baseRate = 0.5, baseCost = 15, costGrowth = 1.15,
            });
            config.producers.Add(new ProducerDef
            {
                id = "fisherman_t2", output = ResourceId.RawFish,
                baseRate = 4, baseCost = 300, costGrowth = 1.15,
            });
            config.producers.Add(new ProducerDef
            {
                id = "fisherman_t3", output = ResourceId.RawFish,
                baseRate = 25, baseCost = 8_000, costGrowth = 1.15,
            });
            config.producers.Add(new ProducerDef
            {
                id = "cutting_station", output = ResourceId.CutFish,
                input = ResourceId.RawFish, inputPerOutput = 1,
                baseRate = 1, baseCost = 120, costGrowth = 1.18,
            });
            config.producers.Add(new ProducerDef
            {
                id = "fillet_station", output = ResourceId.Fillet,
                input = ResourceId.CutFish, inputPerOutput = 2,
                baseRate = 0.5, baseCost = 900, costGrowth = 1.18,
            });

            config.upgrades.Add(new UpgradeDef
            {
                id = "rod", effect = UpgradeEffect.ManualCatchMultiplier,
                multiplierPerLevel = 2, maxLevel = 25,
                baseCost = 10, costGrowth = 4,
            });
            config.upgrades.Add(new UpgradeDef
            {
                id = "crew_training", effect = UpgradeEffect.ProducerRateMultiplier,
                targetProducerId = "*", multiplierPerLevel = 1.25,
                baseCost = 200, costGrowth = 3,
            });
            config.upgrades.Add(new UpgradeDef
            {
                id = "market_deals", effect = UpgradeEffect.SellPriceMultiplier,
                multiplierPerLevel = 1.2,
                baseCost = 500, costGrowth = 3.5,
            });
            config.upgrades.Add(new UpgradeDef
            {
                id = "auto_sell", effect = UpgradeEffect.UnlockAutoSell,
                maxLevel = 1, baseCost = 2_500, costGrowth = 1,
            });
            config.upgrades.Add(new UpgradeDef
            {
                id = "offline_logbook", effect = UpgradeEffect.OfflineCapMultiplier,
                multiplierPerLevel = 1.5, maxLevel = 8,
                baseCost = 1_000, costGrowth = 5,
            });

            return config;
        }
    }
}
