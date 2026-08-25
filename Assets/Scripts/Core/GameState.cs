using System;
using System.Collections.Generic;

namespace Devside.FishingIdle.Core
{
    /// <summary>
    /// Ressources de la simulation. Les valeurs sont sérialisées dans les sauvegardes :
    /// ne jamais réordonner ni supprimer, seulement ajouter à la fin.
    /// </summary>
    public enum ResourceId
    {
        Money = 0,
        RawFish = 1,
        CutFish = 2,
        Fillet = 3,
    }

    [Serializable]
    public class ProducerState
    {
        public string id;
        public int count;
    }

    [Serializable]
    public class UpgradeState
    {
        public string id;
        public int level;
    }

    /// <summary>
    /// État complet d'une partie. POCO sérialisable (champs publics, listes) pour rester
    /// compatible avec JsonUtility côté hôte, sans dépendre d'Unity ici.
    /// </summary>
    [Serializable]
    public class GameState
    {
        public int version = 1;

        public double money;
        public double rawFish;
        public double cutFish;
        public double fillet;

        /// <summary>Argent gagné depuis le tout début (ne baisse jamais) — base du prestige.</summary>
        public double lifetimeMoney;
        public int prestigePoints;
        public bool autoSellUnlocked;

        public List<ProducerState> producers = new List<ProducerState>();
        public List<UpgradeState> upgrades = new List<UpgradeState>();

        /// <summary>Poissodex : ids des espèces découvertes. Permanent — survit au prestige.</summary>
        public List<string> discoveredSpecies = new List<string>();

        /// <summary>Horodatage unix (secondes) fourni par la couche hôte à la sauvegarde.</summary>
        public long lastSeenUnixSeconds;

        public double GetResource(ResourceId id)
        {
            switch (id)
            {
                case ResourceId.Money: return money;
                case ResourceId.RawFish: return rawFish;
                case ResourceId.CutFish: return cutFish;
                case ResourceId.Fillet: return fillet;
                default: return 0;
            }
        }

        public void AddResource(ResourceId id, double amount)
        {
            switch (id)
            {
                case ResourceId.Money:
                    money += amount;
                    if (amount > 0) lifetimeMoney += amount;
                    break;
                case ResourceId.RawFish: rawFish += amount; break;
                case ResourceId.CutFish: cutFish += amount; break;
                case ResourceId.Fillet: fillet += amount; break;
            }
        }

        public int ProducerCount(string producerId)
        {
            for (int i = 0; i < producers.Count; i++)
                if (producers[i].id == producerId)
                    return producers[i].count;
            return 0;
        }

        public ProducerState GetOrCreateProducer(string producerId)
        {
            for (int i = 0; i < producers.Count; i++)
                if (producers[i].id == producerId)
                    return producers[i];
            var created = new ProducerState { id = producerId, count = 0 };
            producers.Add(created);
            return created;
        }

        public int UpgradeLevel(string upgradeId)
        {
            for (int i = 0; i < upgrades.Count; i++)
                if (upgrades[i].id == upgradeId)
                    return upgrades[i].level;
            return 0;
        }

        public UpgradeState GetOrCreateUpgrade(string upgradeId)
        {
            for (int i = 0; i < upgrades.Count; i++)
                if (upgrades[i].id == upgradeId)
                    return upgrades[i];
            var created = new UpgradeState { id = upgradeId, level = 0 };
            upgrades.Add(created);
            return created;
        }

        /// <summary>Remplace tout l'état par celui de <paramref name="other"/> (même référence conservée).</summary>
        public void CopyFrom(GameState other)
        {
            version = other.version;
            money = other.money;
            rawFish = other.rawFish;
            cutFish = other.cutFish;
            fillet = other.fillet;
            lifetimeMoney = other.lifetimeMoney;
            prestigePoints = other.prestigePoints;
            autoSellUnlocked = other.autoSellUnlocked;
            producers = other.producers;
            upgrades = other.upgrades;
            discoveredSpecies = other.discoveredSpecies;
            lastSeenUnixSeconds = other.lastSeenUnixSeconds;
        }
    }
}
