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
        /// <summary>Multiplie la capacité de la cale (plafond de stock, donc de gains hors-ligne).</summary>
        HoldCapacityMultiplier,
        /// <summary>Débloque la vente automatique (niveau max 1).</summary>
        UnlockAutoSell,
        /// <summary>+1 palier de profondeur par niveau — donne accès aux espèces profondes.</summary>
        DepthLevel,
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

        /// <summary>
        /// Capacité de base de la cale, en unités de poisson (avant multiplicateurs).
        /// C'est elle qui plafonne l'accumulation hors-ligne : cale pleine = production stoppée.
        /// </summary>
        public double baseHoldCapacity = 200;

        /// <summary>
        /// Garde-fou de calcul pour la simulation hors-ligne, en secondes. La vraie limite
        /// de gameplay est la cale ; ce plafond évite seulement de simuler des semaines.
        /// </summary>
        public double offlineCapSeconds = 72 * 3600;

        /// <summary>Richesse cumulée donnant droit au premier point de prestige.</summary>
        public double prestigeBase = 25_000_000;

        /// <summary>Bonus de production par point de prestige (0.04 = +4 %).</summary>
        public double prestigeBonusPerPoint = 0.04;

        public Dictionary<ResourceId, double> sellPrices = new Dictionary<ResourceId, double>();
        public List<ProducerDef> producers = new List<ProducerDef>();
        public List<UpgradeDef> upgrades = new List<UpgradeDef>();
        public List<SpeciesDef> species = new List<SpeciesDef>();

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
                baseRate = 3, baseCost = 600, costGrowth = 1.15,
            });
            config.producers.Add(new ProducerDef
            {
                id = "fisherman_t3", output = ResourceId.RawFish,
                baseRate = 18, baseCost = 25_000, costGrowth = 1.15,
            });
            config.producers.Add(new ProducerDef
            {
                id = "cutting_station", output = ResourceId.CutFish,
                input = ResourceId.RawFish, inputPerOutput = 1,
                baseRate = 1, baseCost = 400, costGrowth = 1.22,
            });
            config.producers.Add(new ProducerDef
            {
                id = "fillet_station", output = ResourceId.Fillet,
                input = ResourceId.CutFish, inputPerOutput = 2,
                baseRate = 0.5, baseCost = 3_500, costGrowth = 1.22,
            });

            // v2 : la canne progresse moins vite que son coût (×1.5 de pêche pour ×2.2 de
            // prix) — le clic reste utile mais ne peut plus porter l'économie à lui seul.
            config.upgrades.Add(new UpgradeDef
            {
                id = "rod", effect = UpgradeEffect.ManualCatchMultiplier,
                multiplierPerLevel = 1.5, maxLevel = 40,
                baseCost = 10, costGrowth = 2.2,
            });
            config.upgrades.Add(new UpgradeDef
            {
                id = "crew_training", effect = UpgradeEffect.ProducerRateMultiplier,
                targetProducerId = "*", multiplierPerLevel = 1.25,
                baseCost = 800, costGrowth = 3.5,
            });
            config.upgrades.Add(new UpgradeDef
            {
                id = "market_deals", effect = UpgradeEffect.SellPriceMultiplier,
                multiplierPerLevel = 1.2,
                baseCost = 2_000, costGrowth = 4,
            });
            config.upgrades.Add(new UpgradeDef
            {
                id = "auto_sell", effect = UpgradeEffect.UnlockAutoSell,
                maxLevel = 1, baseCost = 15_000, costGrowth = 1,
            });
            config.upgrades.Add(new UpgradeDef
            {
                id = "cargo_hold", effect = UpgradeEffect.HoldCapacityMultiplier,
                multiplierPerLevel = 2, maxLevel = 30,
                baseCost = 250, costGrowth = 2.8,
            });
            // La profondeur est le contenu mid/long terme : palier 1 à l'échelle de la
            // première grosse session, palier 3 à l'échelle de plusieurs jours.
            config.upgrades.Add(new UpgradeDef
            {
                id = "boat_hull", effect = UpgradeEffect.DepthLevel,
                maxLevel = 3, baseCost = 50_000, costGrowth = 12,
            });

            // Poissodex v1. Palier 0 accessible dès le départ, paliers 1-3 gated par boat_hull.
            // Les poids définissent la rareté au sein d'un même palier ; valueMultiplier ce que
            // vaut la prise ; discoveryBonus le bonus permanent de collection.
            // v2 : les valeurs d'espèces sont compressées (×1 à ×60 au lieu de ×1 à ×5000).
            // La rareté fait le frisson de la prise et alimente le Poissodex ; elle ne doit
            // jamais transformer le clic en jackpot qui écrase le reste de l'économie.
            config.species.Add(new SpeciesDef { id = "sardine", minDepthLevel = 0, weight = 50, valueMultiplier = 1, discoveryBonus = 1.01 });
            config.species.Add(new SpeciesDef { id = "mackerel", minDepthLevel = 0, weight = 30, valueMultiplier = 1.3, discoveryBonus = 1.01 });
            config.species.Add(new SpeciesDef { id = "sea_bass", minDepthLevel = 0, weight = 15, valueMultiplier = 1.8, discoveryBonus = 1.02 });
            config.species.Add(new SpeciesDef { id = "sunfish", minDepthLevel = 0, weight = 5, valueMultiplier = 3, discoveryBonus = 1.03 });
            config.species.Add(new SpeciesDef { id = "tuna", minDepthLevel = 1, weight = 40, valueMultiplier = 2.5, discoveryBonus = 1.02 });
            config.species.Add(new SpeciesDef { id = "swordfish", minDepthLevel = 1, weight = 25, valueMultiplier = 4, discoveryBonus = 1.03 });
            config.species.Add(new SpeciesDef { id = "moonfish", minDepthLevel = 1, weight = 10, valueMultiplier = 6, discoveryBonus = 1.04 });
            config.species.Add(new SpeciesDef { id = "ghost_eel", minDepthLevel = 1, weight = 3, valueMultiplier = 10, discoveryBonus = 1.05 });
            config.species.Add(new SpeciesDef { id = "anglerfish", minDepthLevel = 2, weight = 30, valueMultiplier = 6, discoveryBonus = 1.03 });
            config.species.Add(new SpeciesDef { id = "giant_squid", minDepthLevel = 2, weight = 12, valueMultiplier = 12, discoveryBonus = 1.05 });
            config.species.Add(new SpeciesDef { id = "abyssal_shark", minDepthLevel = 2, weight = 4, valueMultiplier = 20, discoveryBonus = 1.06 });
            config.species.Add(new SpeciesDef { id = "kraken_spawn", minDepthLevel = 3, weight = 6, valueMultiplier = 30, discoveryBonus = 1.08 });
            config.species.Add(new SpeciesDef { id = "leviathan", minDepthLevel = 3, weight = 1, valueMultiplier = 60, discoveryBonus = 1.12 });

            return config;
        }
    }
}
