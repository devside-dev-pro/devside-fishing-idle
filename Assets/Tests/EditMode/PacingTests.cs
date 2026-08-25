using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    /// <summary>
    /// Harnais d'équilibrage : un bot glouton joue les premières heures en accéléré sur la
    /// vraie table (BalanceConfig.Default). Les assertions sont volontairement larges — elles
    /// protègent le rythme (docs/GAME-DESIGN.md), pas les valeurs exactes. Si un réglage
    /// d'équilibrage les casse, c'est que le rythme du début de partie a réellement changé.
    /// </summary>
    public class PacingTests
    {
        /// <summary>Bot : 3 lancers/s, vend toutes les 10 s, achète tout ce qu'il peut (le moins cher d'abord).</summary>
        static GameState PlayFor(BalanceConfig config, int seconds)
        {
            var state = new GameState();
            for (int t = 0; t < seconds; t++)
            {
                for (int c = 0; c < 3; c++) Simulation.CastLine(config, state);
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

        [Test]
        public void FirstAutomation_ArrivesWithinTwoMinutes()
        {
            var state = PlayFor(BalanceConfig.Default(), 120);
            Assert.That(TotalProducers(state), Is.GreaterThanOrEqualTo(1),
                "le joueur doit pouvoir s'offrir son premier pêcheur en moins de 2 minutes");
        }

        [Test]
        public void FirstHour_BuildsAnAutomationEmpire()
        {
            var state = PlayFor(BalanceConfig.Default(), 3600);

            Assert.That(TotalProducers(state), Is.GreaterThanOrEqualTo(15),
                "après 1 h, l'automatisation doit être largement installée");
            Assert.That(state.lifetimeMoney, Is.GreaterThan(10_000),
                "l'économie de la première heure est trop lente");
            Assert.That(state.autoSellUnlocked, Is.True,
                "la vente auto (fin de la corvée de vente) doit tomber dans la première heure");
        }

        [Test]
        public void Progression_AcceleratesOverTime()
        {
            double firstHalf = PlayFor(BalanceConfig.Default(), 1800).lifetimeMoney;
            double full = PlayFor(BalanceConfig.Default(), 3600).lifetimeMoney;

            Assert.That(full, Is.GreaterThan(2 * firstHalf),
                "la seconde demi-heure doit rapporter plus que la première (courbe exponentielle)");
        }
    }
}
