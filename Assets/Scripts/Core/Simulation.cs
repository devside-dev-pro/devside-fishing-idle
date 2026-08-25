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
        /// </summary>
        public static void Tick(BalanceConfig config, GameState state, double dt)
        {
            if (dt <= 0) return;

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

                state.AddResource(def.output, produced);
            }

            if (state.autoSellUnlocked) Economy.SellAll(config, state);
        }

        /// <summary>Action manuelle : un lancer de ligne. Renvoie le nombre de poissons attrapés.</summary>
        public static double CastLine(BalanceConfig config, GameState state)
        {
            double caught = config.baseManualCatch * Multipliers.ManualCatch(config, state);
            state.AddResource(ResourceId.RawFish, caught);
            return caught;
        }
    }
}
