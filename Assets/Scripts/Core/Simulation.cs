namespace Devside.FishingIdle.Core
{
    /// <summary>
    /// Avancée du temps. Déterministe : aucun RNG, aucun accès à l'horloge — le dt vient
    /// de la couche hôte, ce qui rend chaque scénario rejouable en test.
    /// </summary>
    public static class Simulation
    {
        /// <summary>
        /// Fait avancer la simulation de <paramref name="dt"/> secondes. Les producteurs
        /// sont traités dans l'ordre de la config (primaires avant transformations) : un
        /// poste peut donc consommer ce qui vient d'être produit dans le même tick.
        ///
        /// La cale plafonne le STOCK, pas le flux : quand la vente auto opère (en ligne),
        /// le poisson part au marché dans le même tick et la cale ne bride rien ; quand
        /// elle n'opère pas (début de partie, ou hors-ligne où <paramref name="allowAutoSell"/>
        /// est false — pas de comptoir en mer), la production primaire s'arrête cale pleine.
        /// Les transformations, elles, ne gonflent jamais le stock (ratios ≥ 1:1) — les
        /// filets le compressent même, ce qui libère de la place hors-ligne.
        ///
        /// <paramref name="sellPriceMultiplier"/> est le prix du comptoir où le bateau
        /// se trouve (les îles lointaines paient mieux) : le Core ne connaît pas la
        /// géographie, l'hôte injecte le multiplicateur.
        /// </summary>
        public static void Tick(BalanceConfig config, GameState state, double dt,
            bool allowAutoSell = true, double sellPriceMultiplier = 1)
        {
            if (dt <= 0) return;

            // Boosts et délais de pub s'écoulent avec le temps de jeu (et donc aussi
            // pendant la simulation hors-ligne : un boost temporel se consomme).
            Shop.Tick(state, dt);

            bool selling = allowAutoSell && state.autoSellUnlocked;
            double capacity = Multipliers.HoldCapacity(config, state);

            for (int i = 0; i < config.producers.Count; i++)
            {
                var def = config.producers[i];
                int count = state.ProducerCount(def.id);
                if (count <= 0) continue;

                double produced = def.baseRate * count * Multipliers.ProducerRate(config, state, def.id) * dt;

                if (def.input.HasValue)
                {
                    double needed = produced * def.inputPerOutput;
                    double available = state.GetResource(def.input.Value);
                    if (needed > available)
                    {
                        produced = available / def.inputPerOutput;
                        needed = available;
                    }
                    if (produced <= 0) continue;
                    state.AddResource(def.input.Value, -needed);
                }
                else if (!selling)
                {
                    double space = capacity - state.TotalFishStock;
                    if (space <= 0) continue;
                    if (produced > space) produced = space;
                }

                state.AddResource(def.output, produced);
            }

            if (selling) Economy.SellAll(config, state, sellPriceMultiplier);
        }

        /// <summary>
        /// Action manuelle : un lancer de ligne. Le roll ∈ [0,1[ vient de la couche hôte
        /// (UnityEngine.Random.value en jeu, valeur fixée en test) et détermine l'espèce.
        /// La découverte d'espèces ne passe que par ici : le clic garde un rôle à vie.
        /// </summary>
        public static CatchResult CastLine(BalanceConfig config, GameState state, double roll)
        {
            var result = new CatchResult();

            // Cale pleine : rien ne rentre (il faut rentrer vendre). Le seuil n'est pas
            // zéro mais UNE prise entière : sous ce reste, on rendait une fraction de
            // poisson, affichée « +0 » — le joueur clique et croit le jeu cassé (retour
            // playtest). Et ce départ doit précéder le tirage d'espèce, sinon une
            // découverte (et ses perles) serait brûlée sur une prise qui n'entre pas.
            double space = Multipliers.HoldCapacity(config, state) - state.TotalFishStock;
            if (space < System.Math.Min(1, config.baseManualCatch)) return result;

            var species = Catching.PickSpecies(config, state, roll);
            double amount = config.baseManualCatch * Multipliers.ManualCatch(config, state);
            if (species != null)
            {
                amount *= species.valueMultiplier;
                result.speciesId = species.id;
                result.newDiscovery = Catching.RegisterCatch(state, species.id);
                // Le Poissodex est la source premium du joueur gratuit : chaque
                // première prise d'une espèce rapporte quelques perles.
                if (result.newDiscovery) state.pearls += config.pearlsPerDiscovery;
            }
            if (amount > space) amount = space;
            result.amount = amount;
            state.AddResource(ResourceId.RawFish, amount);
            return result;
        }
    }
}
