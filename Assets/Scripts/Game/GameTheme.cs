using System.Collections.Generic;
using Devside.FishingIdle.Core;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Couche thème : seule source des textes affichables du jeu (règle AGENTS.md — aucun
    /// libellé en dur dans la vraie UI). Le Core ne manipule que des ids stables ; c'est ici
    /// qu'ils deviennent des mots. À terme : ScriptableObjects + localisation.
    /// </summary>
    public static class GameTheme
    {
        static readonly Dictionary<string, string> ProducerNames = new Dictionary<string, string>
        {
            ["fisherman_t1"] = "Moussaillon",
            ["fisherman_t2"] = "Pêcheur pro",
            ["fisherman_t3"] = "Chalutier",
            ["cutting_station"] = "Atelier de découpe",
            ["fillet_station"] = "Atelier de filetage",
        };

        static readonly Dictionary<string, string> UpgradeNames = new Dictionary<string, string>
        {
            ["rod"] = "Canne améliorée",
            ["crew_training"] = "Formation d'équipage",
            ["market_deals"] = "Contacts au marché",
            ["auto_sell"] = "Comptoir de vente auto",
            ["cargo_hold"] = "Extension de cale",
            ["boat_hull"] = "Coque renforcée",
        };

        static readonly Dictionary<string, string> SpeciesNames = new Dictionary<string, string>
        {
            ["sardine"] = "Sardine",
            ["mackerel"] = "Maquereau",
            ["sea_bass"] = "Bar",
            ["sunfish"] = "Poisson-lune",
            ["tuna"] = "Thon",
            ["swordfish"] = "Espadon",
            ["moonfish"] = "Opah",
            ["ghost_eel"] = "Anguille fantôme",
            ["anglerfish"] = "Baudroie",
            ["giant_squid"] = "Calmar géant",
            ["abyssal_shark"] = "Requin des abysses",
            ["kraken_spawn"] = "Rejeton de kraken",
            ["leviathan"] = "Léviathan",
        };

        static readonly Dictionary<string, string> IslandNames = new Dictionary<string, string>
        {
            ["island_port"] = "Le Vieux Ponton",
            ["island_lagoon"] = "Lagon Turquoise",
            ["island_mist"] = "Île des Brumes",
            ["island_abyss"] = "Porte des Abysses",
        };

        public const string CastAction = "Pêcher !";
        public const string SellAllAction = "Tout vendre";
        public const string PrestigeAction = "PRESTIGE";
        public const string ProducersSection = "— Producteurs —";
        public const string UpgradesSection = "— Améliorations —";

        // Les 5 onglets de la barre du bas (v0.4).
        public const string BoatTab = "Bateau";
        public const string MapTab = "Carte";
        public const string ProfileTab = "Profil";
        public const string ShopTab = "Boutique";

        public const string MapTitle = "L'archipel";
        public const string MerchantWelcomeFormat = "{0} — vendez votre pêche au comptoir !";
        public const string ZoneLockedFormat = "Coque niv. {0}";
        public const string ProfileTitle = "Capitaine";
        public const string CollectionSection = "— Poissodex —";
        public const string StatsSection = "— Statistiques —";
        public const string UndiscoveredSpecies = "???";
        public const string StatLifetime = "Gagné depuis le début";
        public const string StatDiscovered = "Espèces découvertes";
        public const string StatPrestige = "Points de prestige";
        public const string StatZone = "Zone actuelle";
        public const string ShopComingSoon =
            "Le comptoir ouvre bientôt : perles, coffres, boosts et cosmétiques.\n" +
            "Les pièces se gagnent en jouant — les perles arriveront avec la boutique.";
        public const string MoneySuffix = "pièces";
        public const string RawLabel = "Brut";
        public const string CutLabel = "Découpé";
        public const string FilletLabel = "Filets";
        public const string HoldLabel = "Cale";
        public const string DepthLabel = "Profondeur";
        public const string CollectionLabel = "Poissodex";
        public const string LevelAbbrev = "niv.";
        public const string NewDiscovery = "★ Découverte !";
        public const string HoldFullMessage = "Cale pleine — retournez vendre au comptoir !";
        public const string HullTooWeak = "Ta coque ne supporte pas ces eaux — renforce-la !";
        public const string ZoneReachedPrefix = "Nouvelles eaux";
        public const string OfflinePrefix = "Pendant votre absence";
        public const string OfflineHoldFull = "cale pleine !";
        public const string FishUnit = "poissons";
        public const string PerUnitSuffix = "l'unité";

        public static string Producer(string id) => ProducerNames.TryGetValue(id, out var name) ? name : id;
        public static string Upgrade(string id) => UpgradeNames.TryGetValue(id, out var name) ? name : id;
        public static string Species(string id) => SpeciesNames.TryGetValue(id, out var name) ? name : id;
        public static string Island(string id) => IslandNames.TryGetValue(id, out var name) ? name : id;
    }
}
