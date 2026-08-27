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

            foreach (var piece in config.equipment)
            {
                Assert.That(GameTheme.EquipmentName(piece.id), Is.Not.EqualTo(piece.id),
                    $"libellé manquant pour l'équipement {piece.id}");
                Assert.That(GameTheme.EquipmentIcon(piece.id), Is.Not.Null,
                    $"icône manquante pour l'équipement {piece.id}");
            }

            foreach (var chest in config.chests)
            {
                Assert.That(GameTheme.ChestName(chest.id), Is.Not.EqualTo(chest.id),
                    $"libellé manquant pour le coffre {chest.id}");
                Assert.That(GameTheme.EquipmentIcon(chest.id), Is.Not.Null,
                    $"icône manquante pour le coffre {chest.id}");
            }
        }
    }
}
