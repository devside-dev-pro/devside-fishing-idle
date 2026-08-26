using System;
using System.Collections.Generic;

namespace Devside.FishingIdle.Core
{
    /// <summary>
    /// Une espèce du Poissodex. minDepthLevel la réserve aux paliers de profondeur
    /// supérieurs (améliorations à effet DepthLevel) : les espèces profondes sont
    /// inaccessibles en début de partie.
    /// </summary>
    public class SpeciesDef
    {
        public string id;
        public int minDepthLevel;

        /// <summary>Poids de tirage parmi les espèces accessibles (plus petit = plus rare).</summary>
        public double weight = 1;

        /// <summary>Un exemplaire attrapé vaut autant d'unités de poisson brut.</summary>
        public double valueMultiplier = 1;

        /// <summary>Multiplicateur permanent de production accordé à la découverte (1.02 = +2 %).</summary>
        public double discoveryBonus = 1;
    }

    /// <summary>Résultat d'un lancer manuel, pour l'affichage (espèce, quantité, découverte).</summary>
    public class CatchResult
    {
        /// <summary>Espèce attrapée, ou null si aucune espèce n'est définie à cette profondeur.</summary>
        public string speciesId;
        public double amount;
        public bool newDiscovery;
    }

    /// <summary>
    /// Tirage et découverte d'espèces. Le hasard est injecté (roll ∈ [0,1[) par la couche
    /// hôte : le cœur reste déterministe et rejouable en test.
    /// </summary>
    public static class Catching
    {
        /// <summary>
        /// Palier de profondeur courant = la zone où se trouve le bateau (fixée par la
        /// couche hôte d'après la position dans le monde). Les améliorations DepthLevel
        /// (coque) ne donnent plus les espèces directement : elles autorisent la
        /// NAVIGATION vers les zones lointaines — la profondeur est de la géographie.
        /// </summary>
        public static int DepthLevel(BalanceConfig config, GameState state)
        {
            return state.currentZone < 0 ? 0 : state.currentZone;
        }

        /// <summary>Zone maximale où la coque autorise à naviguer (somme des niveaux DepthLevel).</summary>
        public static int MaxNavigableZone(BalanceConfig config, GameState state)
        {
            int depth = 0;
            for (int i = 0; i < config.upgrades.Count; i++)
                if (config.upgrades[i].effect == UpgradeEffect.DepthLevel)
                    depth += state.UpgradeLevel(config.upgrades[i].id);
            return depth;
        }

        public static List<SpeciesDef> AvailableSpecies(BalanceConfig config, GameState state)
        {
            int depth = DepthLevel(config, state);
            var available = new List<SpeciesDef>();
            for (int i = 0; i < config.species.Count; i++)
                if (config.species[i].minDepthLevel <= depth)
                    available.Add(config.species[i]);
            return available;
        }

        /// <summary>Tirage pondéré déterministe parmi les espèces accessibles ; null si aucune.</summary>
        public static SpeciesDef PickSpecies(BalanceConfig config, GameState state, double roll)
        {
            var available = AvailableSpecies(config, state);
            if (available.Count == 0) return null;

            double total = 0;
            for (int i = 0; i < available.Count; i++) total += available[i].weight;

            double target = Math.Min(Math.Max(roll, 0), 0.999999999) * total;
            double cumulative = 0;
            for (int i = 0; i < available.Count; i++)
            {
                cumulative += available[i].weight;
                if (target < cumulative) return available[i];
            }
            return available[available.Count - 1];
        }

        /// <summary>Consigne la capture au Poissodex ; renvoie true si c'est une découverte.</summary>
        public static bool RegisterCatch(GameState state, string speciesId)
        {
            if (state.discoveredSpecies.Contains(speciesId)) return false;
            state.discoveredSpecies.Add(speciesId);
            return true;
        }

        /// <summary>Produit des bonus de découverte de toutes les espèces consignées au Poissodex.</summary>
        public static double CollectionBonus(BalanceConfig config, GameState state)
        {
            double bonus = 1;
            for (int i = 0; i < config.species.Count; i++)
                if (state.discoveredSpecies.Contains(config.species[i].id))
                    bonus *= config.species[i].discoveryBonus;
            return bonus;
        }
    }
}
