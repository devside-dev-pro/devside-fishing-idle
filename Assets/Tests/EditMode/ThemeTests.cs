using Devside.FishingIdle.Core;
using Devside.FishingIdle.Game;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    /// <summary>
    /// La couche thème doit couvrir toute la table d'équilibrage : un id sans libellé
    /// s'afficherait brut à l'écran (GameTheme renvoie l'id en secours).
    /// </summary>
    public class ThemeTests
    {
        [Test]
        public void EveryDefaultBalanceId_HasAThemeLabel()
        {
            var config = BalanceConfig.Default();

            foreach (var producer in config.producers)
                Assert.That(GameTheme.Producer(producer.id), Is.Not.EqualTo(producer.id),
                    $"libellé manquant pour le producteur {producer.id}");

            foreach (var upgrade in config.upgrades)
                Assert.That(GameTheme.Upgrade(upgrade.id), Is.Not.EqualTo(upgrade.id),
                    $"libellé manquant pour l'amélioration {upgrade.id}");

            foreach (var species in config.species)
                Assert.That(GameTheme.Species(species.id), Is.Not.EqualTo(species.id),
                    $"libellé manquant pour l'espèce {species.id}");
        }
    }
}
