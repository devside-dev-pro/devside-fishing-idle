using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    /// <summary>
    /// Harnais d'équilibrage : un bot glouton joue les premières heures en accéléré sur la
    /// vraie table (BalanceConfig.Default). Les assertions protègent le RYTHME dans les
    /// deux sens : bornes basses (le jeu ne doit pas être trop lent) ET bornes hautes
    /// (le contenu ne doit pas se consommer en minutes — leçon du premier playtest, où
    /// tout était acheté en 3 minutes). Les valeurs exactes de BalanceConfig peuvent
    /// bouger librement tant que ces enveloppes tiennent.
    /// NB : le bot est glouton (il dépense en continu, n'épargne jamais) — il sous-estime
    /// donc un joueur qui économise pour les gros achats. Les bornes en tiennent compte.
    /// </summary>
    public class PacingTests
    {
        /// <summary>Bot : 3 lancers/s, vend toutes les 10 s, achète tout ce qu'il peut (le moins cher d'abord).</summary>
        static GameState PlayFor(BalanceConfig config, int seconds)
        {
            var state = new GameState();
            var rng = new System.Random(20260825); // graine fixe : le bot est rejouable
            for (int t = 0; t < seconds; t++)
            {
                // La profondeur est de la géographie : le bot joue la couche hôte et
                // navigue toujours aussi profond que sa coque l'autorise.
                state.currentZone = Catching.MaxNavigableZone(config, state);
                for (int c = 0; c < 3; c++) Simulation.CastLine(config, state, rng.NextDouble());
                Simulation.Tick(config, state, 1);
                if (!state.autoSellUnlocked && t % 10 == 0) Economy.SellAll(config, state);

                bool bought = true;
                while (bought)
                {
                    bought = false;
                    foreach (var up in config.upgrades)
                        if (state.UpgradeLevel(up.id) < up.maxLevel
                            && Economy.TryBuyUpgrade(config, state, up.id))
                            bought = true;

                    ProducerDef cheapest = null;
                    double cheapestCost = double.MaxValue;
                    foreach (var p in config.producers)
                    {
                        double cost = Economy.ProducerCost(p, state.ProducerCount(p.id));
                        if (cost < cheapestCost)
                        {
                            cheapest = p;
                            cheapestCost = cost;
                        }
                    }
                    if (cheapest != null && Economy.TryBuyProducer(config, state, cheapest.id))
                        bought = true;
                }
            }
            return state;
        }

        static int TotalProducers(GameState state)
        {
            int total = 0;
            foreach (var p in state.producers) total += p.count;
            return total;
        }

        // ---------- Bornes basses : le jeu ne doit pas être trop lent ----------

        [Test]
        public void FirstAutomation_ArrivesWithinTwoMinutes()
        {
            var state = PlayFor(BalanceConfig.Default(), 120);
            Assert.That(TotalProducers(state), Is.GreaterThanOrEqualTo(1),
                "le joueur doit pouvoir s'offrir son premier pêcheur en moins de 2 minutes");
        }

        [Test]
        public void FirstHour_BuildsMeaningfulProgress()
        {
            var state = PlayFor(BalanceConfig.Default(), 3600);

            Assert.That(TotalProducers(state), Is.GreaterThanOrEqualTo(12),
                "après 1 h, l'automatisation doit être bien installée");
            Assert.That(state.lifetimeMoney, Is.GreaterThan(5_000),
                "l'économie de la première heure est trop lente");
        }

        // ---------- Bornes hautes : le contenu ne doit pas se consommer en minutes ----------

        [Test]
        public void AutoSell_IsNotAffordableInFifteenMinutes()
        {
            var state = PlayFor(BalanceConfig.Default(), 900);
            Assert.That(state.autoSellUnlocked, Is.False,
                "la vente auto est la fin d'une corvée : elle doit se mériter, pas tomber en 15 minutes");
        }

        [Test]
        public void RodUpgrades_CannotBeMaxedInTheFirstHour()
        {
            var state = PlayFor(BalanceConfig.Default(), 3600);
            Assert.That(state.UpgradeLevel("rod"), Is.LessThan(20),
                "la canne ne doit jamais être proche du max en 1 h (leçon du speedrun du premier playtest)");
        }

        [Test]
        public void Collection_IsNotCompleteAfterTwoHours()
        {
            var state = PlayFor(BalanceConfig.Default(), 7200);
            Assert.That(state.discoveredSpecies.Count, Is.LessThan(13),
                "le Poissodex complet est un objectif long terme, pas un trophée de la première session");
        }

        [Test]
        public void Prestige_IsNotReachableInTheFirstHour()
        {
            var state = PlayFor(BalanceConfig.Default(), 3600);
            Assert.That(Prestige.PendingPoints(BalanceConfig.Default(), state), Is.EqualTo(0),
                "le premier prestige se joue en heures, pas en minutes");
        }

        // ---------- Forme de la courbe ----------

        [Test]
        public void Progression_AcceleratesOverTime()
        {
            double firstHalf = PlayFor(BalanceConfig.Default(), 1800).lifetimeMoney;
            double full = PlayFor(BalanceConfig.Default(), 3600).lifetimeMoney;

            Assert.That(full, Is.GreaterThan(1.6 * firstHalf),
                "la seconde demi-heure doit rapporter nettement plus que la première (courbe composée)");
        }
    }
}
