using Devside.FishingIdle.Core;
using NUnit.Framework;

namespace Devside.FishingIdle.Core.Tests
{
    /// <summary>
    /// L'équipement : collection de pièces qui montent par doublons, quatre portées à la
    /// fois. Comme partout dans le Core, le hasard des coffres est injecté par l'hôte.
    /// </summary>
    public class EquipmentTests
    {
        static BalanceConfig Config() => BalanceConfig.Default();

        [Test]
        public void OpenChest_FirstFind_GivesLevelOneAndEquipsIt()
        {
            var config = Config();
            var state = new GameState { money = 10_000 };

            var result = Equipment.OpenChest(config, state, "chest_wood", 0.1);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.isNew, Is.True);
            Assert.That(result.level, Is.EqualTo(1));
            var def = Equipment.Def(config, result.equipmentId);
            Assert.That(state.EquippedId(def.slot), Is.EqualTo(def.id),
                "un emplacement vide se remplit tout seul : la trouvaille doit servir tout de suite");
            Assert.That(state.money, Is.EqualTo(5_000).Within(1e-9), "le coffre est payé");
        }

        [Test]
        public void OpenChest_WithoutMoney_DoesNothing()
        {
            var config = Config();
            var state = new GameState { money = 100 };

            Assert.That(Equipment.OpenChest(config, state, "chest_wood", 0.5), Is.Null);
            Assert.That(state.money, Is.EqualTo(100).Within(1e-9));
            Assert.That(state.equipment, Is.Empty);
        }

        [Test]
        public void Duplicates_FeedTheNextLevel()
        {
            var config = Config();
            var state = new GameState();
            var piece = state.EquipmentPiece("rod_bamboo", true);
            piece.level = 1;

            Assert.That(Equipment.CanUpgrade(config, state, "rod_bamboo"), Is.False, "sans doublon, rien à fusionner");

            piece.copies = Equipment.CopiesToUpgrade(1);
            Assert.That(Equipment.Upgrade(config, state, "rod_bamboo"), Is.True);
            Assert.That(Equipment.Level(state, "rod_bamboo"), Is.EqualTo(2));
            Assert.That(Equipment.Copies(state, "rod_bamboo"), Is.EqualTo(0), "les doublons sont consommés");
        }

        [Test]
        public void Upgrade_StopsAtMaxLevel()
        {
            var config = Config();
            var state = new GameState();
            var def = Equipment.Def(config, "rod_bamboo");
            var piece = state.EquipmentPiece("rod_bamboo", true);
            piece.level = def.maxLevel;
            piece.copies = 999;

            Assert.That(Equipment.CanUpgrade(config, state, "rod_bamboo"), Is.False);
            Assert.That(Equipment.Upgrade(config, state, "rod_bamboo"), Is.False);
        }

        [Test]
        public void Equip_NeedsToOwnThePiece()
        {
            var config = Config();
            var state = new GameState();

            Assert.That(Equipment.Equip(config, state, "rod_carbon"), Is.False);
            Assert.That(state.EquippedId(EquipmentSlot.Rod), Is.Null);

            state.EquipmentPiece("rod_carbon", true).level = 1;
            Assert.That(Equipment.Equip(config, state, "rod_carbon"), Is.True);
            Assert.That(state.EquippedId(EquipmentSlot.Rod), Is.EqualTo("rod_carbon"));
        }

        [Test]
        public void Bonus_CountsOnlyWornPieces_AndAddsUp()
        {
            var config = Config();
            var state = new GameState();
            state.EquipmentPiece("rod_bamboo", true).level = 3;   // +2 % × 3 = +6 %
            state.EquipmentPiece("rod_carbon", true).level = 5;   // rangée, pas portée

            Assert.That(Equipment.Bonus(config, state, EquipmentEffect.ManualCatch), Is.EqualTo(1).Within(1e-9),
                "rien de porté, aucun bonus");

            Equipment.Equip(config, state, "rod_bamboo");
            Assert.That(Equipment.Bonus(config, state, EquipmentEffect.ManualCatch), Is.EqualTo(1.06).Within(1e-9));

            // Un seul objet par emplacement : porter la canne carbone remplace le bambou.
            Equipment.Equip(config, state, "rod_carbon");
            Assert.That(Equipment.Bonus(config, state, EquipmentEffect.ManualCatch), Is.EqualTo(1.2).Within(1e-9));
        }

        [Test]
        public void WornEquipment_ReachesTheSimulation()
        {
            var config = Config();
            var state = new GameState();
            double before = Multipliers.ManualCatch(config, state);

            state.EquipmentPiece("rod_carbon", true).level = 5;   // +4 % × 5 = +20 %
            Equipment.Equip(config, state, "rod_carbon");

            Assert.That(Multipliers.ManualCatch(config, state), Is.EqualTo(before * 1.2).Within(1e-9));
        }

        [Test]
        public void HoldEquipment_WidensTheHold()
        {
            var config = Config();
            var state = new GameState();
            double before = Multipliers.HoldCapacity(config, state);

            state.EquipmentPiece("outfit_hat", true).level = 4;   // +2 % × 4 = +8 %
            Equipment.Equip(config, state, "outfit_hat");

            Assert.That(Multipliers.HoldCapacity(config, state), Is.EqualTo(before * 1.08).Within(1e-9));
        }

        [Test]
        public void Chests_FavourTheirOwnRarities()
        {
            var config = Config();
            // Un roll très bas tombe dans la première tranche (commune) pour tous les
            // coffres ; un roll très haut atteint la meilleure rareté du coffre d'or.
            var wood = new GameState { money = 1_000_000 };
            var gold = new GameState { money = 1_000_000 };

            var common = Equipment.OpenChest(config, wood, "chest_wood", 0.01);
            var best = Equipment.OpenChest(config, gold, "chest_gold", 0.999);

            Assert.That(Equipment.Def(config, common.equipmentId).rarity, Is.EqualTo(Rarity.Common));
            Assert.That(Equipment.Def(config, best.equipmentId).rarity, Is.EqualTo(Rarity.Legendary));
        }

        [Test]
        public void Prestige_KeepsTheCollection()
        {
            var config = Config();
            var state = new GameState { lifetimeMoney = 100_000_000, money = 500 };
            state.EquipmentPiece("rod_carbon", true).level = 3;
            Equipment.Equip(config, state, "rod_carbon");

            Prestige.Execute(config, state);

            Assert.That(Equipment.Level(state, "rod_carbon"), Is.EqualTo(3), "l'équipement est permanent");
            Assert.That(state.EquippedId(EquipmentSlot.Rod), Is.EqualTo("rod_carbon"), "et reste porté");
            Assert.That(state.money, Is.EqualTo(0).Within(1e-9), "le reste est bien remis à zéro");
        }
    }
}
