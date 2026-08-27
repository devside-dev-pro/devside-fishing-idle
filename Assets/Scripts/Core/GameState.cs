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

    /// <summary>Un compte à rebours nommé (boost en cours, délai de rechargement d'une pub).</summary>
    [Serializable]
    public class TimerState
    {
        public string id;
        public double secondsLeft;
    }

    /// <summary>
    /// Une pièce d'équipement de la collection : son niveau (0 = pas encore trouvée) et
    /// les exemplaires en réserve pour la prochaine fusion. Voir Core/Equipment.
    /// </summary>
    [Serializable]
    public class EquipmentPieceState
    {
        public string id;
        public int level;
        public int copies;
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

        /// <summary>
        /// Perles : la monnaie premium. Elles se gagnent lentement en jouant (découvertes
        /// du Poissodex, pubs récompensées) ou s'achètent, et survivent au prestige.
        /// </summary>
        public double pearls;

        /// <summary>Argent gagné depuis le tout début (ne baisse jamais) — base du prestige.</summary>
        public double lifetimeMoney;
        public int prestigePoints;
        public bool autoSellUnlocked;

        public List<ProducerState> producers = new List<ProducerState>();
        public List<UpgradeState> upgrades = new List<UpgradeState>();

        /// <summary>Poissodex : ids des espèces découvertes. Permanent — survit au prestige.</summary>
        public List<string> discoveredSpecies = new List<string>();

        /// <summary>Collection d'équipement. Permanente : elle survit au prestige.</summary>
        public List<EquipmentPieceState> equipment = new List<EquipmentPieceState>();

        /// <summary>Boosts en cours (id → secondes restantes).</summary>
        public List<TimerState> boosts = new List<TimerState>();

        /// <summary>Délais de rechargement des pubs récompensées (id → secondes restantes).</summary>
        public List<TimerState> adCooldowns = new List<TimerState>();

        /// <summary>
        /// Pièce portée à chaque emplacement (index = EquipmentSlot, chaîne vide = aucune).
        /// Liste plutôt que tableau : JsonUtility sérialise mal les tableaux redimensionnés.
        /// </summary>
        public List<string> equippedBySlot = new List<string>();

        /// <summary>Stock total de poisson à bord — ce que la cale doit contenir.</summary>
        public double TotalFishStock => rawFish + cutFish + fillet;

        /// <summary>Horodatage unix (secondes) fourni par la couche hôte à la sauvegarde.</summary>
        public long lastSeenUnixSeconds;

        /// <summary>
        /// Zone de profondeur où se trouve le bateau (0 = eaux de départ). Fixée par la
        /// couche hôte d'après la position dans le monde — la navigation y est verrouillée
        /// par l'amélioration de coque, le Core ne fait que lire la zone courante.
        /// </summary>
        public int currentZone;

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

        /// <summary>Un compte à rebours de la liste donnée, créé à la volée si <paramref name="create"/>.</summary>
        public TimerState Timer(List<TimerState> timers, string id, bool create)
        {
            for (int i = 0; i < timers.Count; i++)
                if (timers[i].id == id)
                    return timers[i];
            if (!create) return null;
            var created = new TimerState { id = id, secondsLeft = 0 };
            timers.Add(created);
            return created;
        }

        /// <summary>La pièce d'équipement, créée à la volée si <paramref name="create"/>.</summary>
        public EquipmentPieceState EquipmentPiece(string equipmentId, bool create)
        {
            for (int i = 0; i < equipment.Count; i++)
                if (equipment[i].id == equipmentId)
                    return equipment[i];
            if (!create) return null;
            var created = new EquipmentPieceState { id = equipmentId, level = 0, copies = 0 };
            equipment.Add(created);
            return created;
        }

        /// <summary>Id de la pièce portée à cet emplacement, ou null.</summary>
        public string EquippedId(EquipmentSlot slot)
        {
            int index = (int)slot;
            if (index < 0 || index >= equippedBySlot.Count) return null;
            string id = equippedBySlot[index];
            return string.IsNullOrEmpty(id) ? null : id;
        }

        /// <summary>Porte (ou retire, avec null) une pièce à cet emplacement.</summary>
        public void SetEquipped(EquipmentSlot slot, string equipmentId)
        {
            int index = (int)slot;
            while (equippedBySlot.Count <= index) equippedBySlot.Add(string.Empty);
            equippedBySlot[index] = equipmentId ?? string.Empty;
        }

        /// <summary>Remplace tout l'état par celui de <paramref name="other"/> (même référence conservée).</summary>
        public void CopyFrom(GameState other)
        {
            version = other.version;
            money = other.money;
            pearls = other.pearls;
            rawFish = other.rawFish;
            cutFish = other.cutFish;
            fillet = other.fillet;
            lifetimeMoney = other.lifetimeMoney;
            prestigePoints = other.prestigePoints;
            autoSellUnlocked = other.autoSellUnlocked;
            producers = other.producers;
            upgrades = other.upgrades;
            discoveredSpecies = other.discoveredSpecies;
            equipment = other.equipment;
            equippedBySlot = other.equippedBySlot;
            boosts = other.boosts;
            adCooldowns = other.adCooldowns;
            lastSeenUnixSeconds = other.lastSeenUnixSeconds;
            currentZone = other.currentZone;
        }
    }
}
