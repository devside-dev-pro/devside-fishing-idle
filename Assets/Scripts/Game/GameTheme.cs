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
            ["boat_hull"] = "Coque renforcée (profondeur +1)",
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

        public const string CastAction = "Pêcher !";
        public const string SellAllAction = "Tout vendre";
        public const string PrestigeAction = "PRESTIGE";
        public const string ProducersSection = "— Producteurs —";
        public const string UpgradesSection = "— Améliorations —";
        public const string CrewTab = "Équipage";
        public const string UpgradesTab = "Améliorer";
        public const string MoneySuffix = "pièces";
        public const string RawLabel = "Brut";
        public const string CutLabel = "Découpé";
        public const string FilletLabel = "Filets";
        public const string HoldLabel = "Cale";
        public const string DepthLabel = "Profondeur";
        public const string CollectionLabel = "Poissodex";
        public const string LevelAbbrev = "niv.";
        public const string NewDiscovery = "★ Découverte !";
        public const string HoldFullMessage = "Cale pleine — vendez votre stock !";
        public const string OfflinePrefix = "Pendant votre absence";
        public const string OfflineHoldFull = "cale pleine !";
        public const string FishUnit = "poissons";

        public static string Producer(string id) => ProducerNames.TryGetValue(id, out var name) ? name : id;
        public static string Upgrade(string id) => UpgradeNames.TryGetValue(id, out var name) ? name : id;
        public static string Species(string id) => SpeciesNames.TryGetValue(id, out var name) ? name : id;
    }
}
