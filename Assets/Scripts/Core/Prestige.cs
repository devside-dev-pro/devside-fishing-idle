using System;

namespace Devside.FishingIdle.Core
{
    /// <summary>
    /// Prestige : reset volontaire contre des points permanents. Courbe en racine carrée de
    /// la richesse cumulée — doubler ses points demande de quadrupler sa richesse.
    /// </summary>
    public static class Prestige
    {
        /// <summary>Points totaux auxquels donne droit une richesse cumulée.</summary>
        public static int PointsFor(BalanceConfig config, double lifetimeMoney)
            => lifetimeMoney <= 0 ? 0 : (int)Math.Floor(Math.Sqrt(lifetimeMoney / config.prestigeBase));

        /// <summary>Points qui seraient gagnés en prestigeant maintenant.</summary>
        public static int PendingPoints(BalanceConfig config, GameState state)
            => Math.Max(0, PointsFor(config, state.lifetimeMoney) - state.prestigePoints);

        /// <summary>Bonus global de production appliqué à la pêche manuelle et aux producteurs.</summary>
        public static double ProductionMultiplier(BalanceConfig config, GameState state)
            => 1 + state.prestigePoints * config.prestigeBonusPerPoint;

        /// <summary>
        /// Encaisse les points en attente et réinitialise la partie (reset dur : seuls les
        /// acquis permanents survivent). Renvoie les points gagnés, 0 si rien à encaisser.
        /// </summary>
        public static int Execute(BalanceConfig config, GameState state)
        {
            int gained = PendingPoints(config, state);
            if (gained <= 0) return 0;

            var fresh = new GameState
            {
                prestigePoints = state.prestigePoints + gained,
                lifetimeMoney = state.lifetimeMoney,
                lastSeenUnixSeconds = state.lastSeenUnixSeconds,
                // Le Poissodex est permanent : les découvertes survivent au reset.
                discoveredSpecies = state.discoveredSpecies,
                // L'équipement aussi : ce qu'on a trouvé et porté ne se rejoue pas.
                equipment = state.equipment,
                equippedBySlot = state.equippedBySlot,
            };
            state.CopyFrom(fresh);
            return gained;
        }
    }
}
