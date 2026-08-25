using System;

namespace Devside.FishingIdle.Core
{
    /// <summary>Résumé d'une progression hors-ligne, destiné à l'écran « pendant votre absence ».</summary>
    public class OfflineResult
    {
        public double simulatedSeconds;
        public double moneyGained;
        public double stockGained;

        /// <summary>La cale a saturé pendant l'absence — base de la future notification « cale pleine ».</summary>
        public bool holdFull;
    }

    public static class OfflineProgress
    {
        /// <summary>
        /// Pas de simulation. Assez court pour que la chaîne de transformation avance comme
        /// en ligne, assez long pour que 8 h d'absence restent bon marché à calculer.
        /// </summary>
        public const double StepSeconds = 60;

        /// <summary>
        /// Applique la progression hors-ligne et renvoie un résumé. La vente auto ne tourne
        /// pas en mer : le poisson s'accumule dans la cale et c'est sa capacité qui plafonne
        /// réellement le gain (offlineCapSeconds n'est qu'un garde-fou de calcul).
        /// </summary>
        public static OfflineResult Apply(BalanceConfig config, GameState state, double elapsedSeconds)
        {
            double simulated = Math.Max(0, Math.Min(elapsedSeconds, config.offlineCapSeconds));

            double moneyBefore = state.money;
            double stockBefore = state.TotalFishStock;

            double remaining = simulated;
            while (remaining > 0)
            {
                double dt = Math.Min(StepSeconds, remaining);
                Simulation.Tick(config, state, dt, allowAutoSell: false);
                remaining -= dt;
            }

            return new OfflineResult
            {
                simulatedSeconds = simulated,
                moneyGained = state.money - moneyBefore,
                stockGained = state.TotalFishStock - stockBefore,
                holdFull = state.TotalFishStock + 1e-6 >= Multipliers.HoldCapacity(config, state),
            };
        }
    }
}
