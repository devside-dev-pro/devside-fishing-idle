using System;
using System.Collections.Generic;

namespace Devside.FishingIdle.Core
{
    /// <summary>
    /// Les quatre emplacements d'équipement du capitaine. Sérialisés en int dans les
    /// sauvegardes : ne jamais réordonner, seulement ajouter à la fin.
    /// </summary>
    public enum EquipmentSlot
    {
        Rod = 0,
        Reel = 1,
        Bait = 2,
        Outfit = 3,
    }

    /// <summary>Rareté d'une pièce d'équipement : elle décide de sa puissance et de sa fréquence en coffre.</summary>
    public enum Rarity
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3,
    }

    /// <summary>Ce qu'une pièce d'équipement améliore une fois portée.</summary>
    public enum EquipmentEffect
    {
        ManualCatch = 0,
        ProducerRate = 1,
        SellPrice = 2,
        HoldCapacity = 3,
    }

    public class EquipmentDef
    {
        public string id;
        public EquipmentSlot slot;
        public Rarity rarity;
        public EquipmentEffect effect;

        /// <summary>Bonus additif par niveau (0.04 = +4 % par niveau, cumulé aux autres pièces portées).</summary>
        public double bonusPerLevel;

        public int maxLevel = 10;
    }

    /// <summary>
    /// Un coffre : ce qu'il coûte, et la chance de chaque rareté. Les poids ne sont pas
    /// normalisés — Equipment.OpenChest s'en charge.
    /// </summary>
    public class ChestDef
    {
        public string id;
        public double cost;

        /// <summary>Prix en perles du même coffre à la boutique (0 = pas vendu en perles).</summary>
        public double pearlCost;

        public double[] rarityWeights = new double[4];
    }

    /// <summary>Ce qu'un coffre a donné, pour l'affichage.</summary>
    public class ChestResult
    {
        public string equipmentId;

        /// <summary>Vrai si la pièce vient d'entrer dans la collection (niveau 1).</summary>
        public bool isNew;

        /// <summary>Niveau atteint après ouverture (les doublons font monter la pièce).</summary>
        public int level;
    }

    /// <summary>
    /// Équipement du capitaine : collection de pièces qui montent de niveau par doublons
    /// (fusion), quatre portées à la fois. Comme le reste du Core, tout est déterministe :
    /// le hasard des coffres est un roll ∈ [0,1[ injecté par la couche hôte.
    ///
    /// Choix de conception : on ne possède pas des objets distincts mais des PIÈCES, dont
    /// le niveau croît en accumulant des exemplaires. La sauvegarde reste minuscule, la
    /// progression se lit d'un coup d'œil, et un doublon n'est jamais un déchet.
    /// </summary>
    public static class Equipment
    {
        /// <summary>Exemplaires à réunir pour passer du niveau donné au suivant.</summary>
        public static int CopiesToUpgrade(int level) => level <= 0 ? 1 : 2 * level;

        public static EquipmentDef Def(BalanceConfig config, string equipmentId)
        {
            for (int i = 0; i < config.equipment.Count; i++)
                if (config.equipment[i].id == equipmentId)
                    return config.equipment[i];
            return null;
        }

        /// <summary>Les pièces d'un emplacement, dans l'ordre de la table (rareté croissante).</summary>
        public static List<EquipmentDef> ForSlot(BalanceConfig config, EquipmentSlot slot)
        {
            var list = new List<EquipmentDef>();
            for (int i = 0; i < config.equipment.Count; i++)
                if (config.equipment[i].slot == slot)
                    list.Add(config.equipment[i]);
            return list;
        }

        public static int Level(GameState state, string equipmentId)
            => state.EquipmentPiece(equipmentId, false)?.level ?? 0;

        public static bool Owns(GameState state, string equipmentId) => Level(state, equipmentId) > 0;

        /// <summary>Exemplaires en réserve pour la prochaine fusion.</summary>
        public static int Copies(GameState state, string equipmentId)
            => state.EquipmentPiece(equipmentId, false)?.copies ?? 0;

        public static bool CanUpgrade(BalanceConfig config, GameState state, string equipmentId)
        {
            var def = Def(config, equipmentId);
            if (def == null) return false;
            int level = Level(state, equipmentId);
            return level > 0 && level < def.maxLevel && Copies(state, equipmentId) >= CopiesToUpgrade(level);
        }

        /// <summary>Fusionne les doublons en un niveau. Faux si la réserve ne suffit pas.</summary>
        public static bool Upgrade(BalanceConfig config, GameState state, string equipmentId)
        {
            if (!CanUpgrade(config, state, equipmentId)) return false;
            var piece = state.EquipmentPiece(equipmentId, true);
            piece.copies -= CopiesToUpgrade(piece.level);
            piece.level++;
            return true;
        }

        /// <summary>
        /// Porte une pièce possédée (elle remplace celle de son emplacement).
        /// Faux si la pièce est inconnue ou pas encore trouvée.
        /// </summary>
        public static bool Equip(BalanceConfig config, GameState state, string equipmentId)
        {
            var def = Def(config, equipmentId);
            if (def == null || !Owns(state, equipmentId)) return false;
            state.SetEquipped(def.slot, equipmentId);
            return true;
        }

        public static void Unequip(GameState state, EquipmentSlot slot) => state.SetEquipped(slot, null);

        /// <summary>La pièce portée à cet emplacement, ou null.</summary>
        public static EquipmentDef Equipped(BalanceConfig config, GameState state, EquipmentSlot slot)
        {
            string id = state.EquippedId(slot);
            return string.IsNullOrEmpty(id) ? null : Def(config, id);
        }

        /// <summary>
        /// Multiplicateur apporté par les pièces PORTÉES pour cet effet (1 = aucun bonus).
        /// Les bonus s'additionnent entre pièces : quatre pièces à +10 % font ×1.4, pas ×1.46 —
        /// c'est plus lisible pour le joueur et plus facile à équilibrer.
        /// </summary>
        public static double Bonus(BalanceConfig config, GameState state, EquipmentEffect effect)
        {
            double bonus = 0;
            for (int slot = 0; slot < 4; slot++)
            {
                var def = Equipped(config, state, (EquipmentSlot)slot);
                if (def == null || def.effect != effect) continue;
                bonus += def.bonusPerLevel * Level(state, def.id);
            }
            return 1 + bonus;
        }

        public static ChestDef Chest(BalanceConfig config, string chestId)
        {
            for (int i = 0; i < config.chests.Count; i++)
                if (config.chests[i].id == chestId)
                    return config.chests[i];
            return null;
        }

        public static bool CanAfford(BalanceConfig config, GameState state, string chestId)
        {
            var chest = Chest(config, chestId);
            return chest != null && state.money >= chest.cost;
        }

        /// <summary>
        /// Ouvre un coffre : débite son prix, tire une rareté selon les poids puis une
        /// pièce parmi celles de cette rareté, et l'ajoute à la collection. Le roll
        /// ∈ [0,1[ vient de l'hôte. Null si le coffre est inconnu ou pas payable.
        /// </summary>
        public static ChestResult OpenChest(BalanceConfig config, GameState state, string chestId, double roll)
        {
            var chest = Chest(config, chestId);
            if (chest == null || state.money < chest.cost) return null;

            var pool = PickPool(config, chest, roll);
            if (pool.Count == 0) return null;

            state.money -= chest.cost;

            // Le même roll sert à choisir la pièce dans la rareté tirée : un seul roll
            // par ouverture garde les tests lisibles et la sauvegarde rejouable.
            int index = (int)(Clamp01(roll) * pool.Count);
            if (index >= pool.Count) index = pool.Count - 1;
            var def = pool[index];

            var piece = state.EquipmentPiece(def.id, true);
            bool isNew = piece.level == 0;
            if (isNew) piece.level = 1;
            else piece.copies++;

            // Première pièce trouvée pour un emplacement vide : on la porte d'office,
            // sinon le joueur repart en mer sans avoir profité de sa trouvaille.
            if (string.IsNullOrEmpty(state.EquippedId(def.slot))) state.SetEquipped(def.slot, def.id);

            return new ChestResult { equipmentId = def.id, isNew = isNew, level = piece.level };
        }

        /// <summary>
        /// Rareté tirée par les poids du coffre, repliée sur la rareté juste en dessous
        /// tant que la table n'a rien à offrir (un coffre ne rend jamais rien).
        /// </summary>
        static List<EquipmentDef> PickPool(BalanceConfig config, ChestDef chest, double roll)
        {
            double total = 0;
            for (int i = 0; i < chest.rarityWeights.Length; i++) total += chest.rarityWeights[i];
            if (total <= 0) return new List<EquipmentDef>();

            double target = Clamp01(roll) * total;
            int rarity = 0;
            double sum = 0;
            for (int i = 0; i < chest.rarityWeights.Length; i++)
            {
                sum += chest.rarityWeights[i];
                if (target < sum || i == chest.rarityWeights.Length - 1)
                {
                    rarity = i;
                    break;
                }
            }

            for (int r = rarity; r >= 0; r--)
            {
                var pool = OfRarity(config, (Rarity)r);
                if (pool.Count > 0) return pool;
            }
            return new List<EquipmentDef>();
        }

        static List<EquipmentDef> OfRarity(BalanceConfig config, Rarity rarity)
        {
            var list = new List<EquipmentDef>();
            for (int i = 0; i < config.equipment.Count; i++)
                if (config.equipment[i].rarity == rarity)
                    list.Add(config.equipment[i]);
            return list;
        }

        static double Clamp01(double value) => value < 0 ? 0 : value >= 1 ? 0.999999 : value;
    }
}
