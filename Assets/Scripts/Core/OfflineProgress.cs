using System;

namespace Devside.FishingIdle.Core
{
    /// <summary>Résumé d'une progression hors-ligne, destiné à l'écran « pendant votre absence ».</summary>
    public class OfflineResult
    {
        public double simulatedSeconds;
        public double moneyGained;
        public double stockGained;
    }

    public static class OfflineProgress
    {
        /// <summary>
        /// Pas de simulation. Assez court pour que la chaîne de transformation avance comme
        /// en ligne, assez long pour que 8 h d'absence restent bon marché à calculer.
        /// </summary>
        public const double StepSeconds = 60;

        /// <summary>
        /// Applique la progression hors-ligne, plafonnée par la config, et renvoie un résumé.
        /// </summary>
        public static OfflineResult Apply(BalanceConfig config, GameState state, double elapsedSeconds)
        {
            double cap = Multipliers.OfflineCapSeconds(config, state);
            double simulated = Math.Max(0, Math.Min(elapsedSeconds, cap));

            double moneyBefore = state.money;
            double stockBefore = state.rawFish + state.cutFish + state.fillet;

            double remaining = simulated;
            while (remaining > 0)
            {
                double dt = Math.Min(StepSeconds, remaining);
                Simulation.Tick(config, state, dt);
                remaining -= dt;
            }

            return new OfflineResult
            {
                simulatedSeconds = simulated,
                moneyGained = state.money - moneyBefore,
                stockGained = state.rawFish + state.cutFish + state.fillet - stockBefore,
            };
        }
    }
}
