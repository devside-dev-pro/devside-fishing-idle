using System.Collections.Generic;

namespace Devside.FishingIdle.Core
{
    /// <summary>Ce qu'un boost accélère pendant qu'il est actif.</summary>
    public enum BoostKind
    {
        /// <summary>Toute la pêche : canne et producteurs.</summary>
        Fishing = 0,

        /// <summary>Vitesse de navigation (lue par la couche hôte).</summary>
        SailSpeed = 1,
    }

    public class BoostDef
    {
        public string id;
        public BoostKind kind;

        /// <summary>Multiplicateur appliqué tant que le boost court (2 = double).</summary>
        public double multiplier = 2;

        public double durationSeconds;

        /// <summary>Durée cumulée maximale : relancer un boost s'ajoute, jusqu'à ce plafond.</summary>
        public double maxStackSeconds;

        /// <summary>Prix en perles, ou 0 si le boost ne s'achète pas.</summary>
        public double pearlCost;
    }

    /// <summary>
    /// Un emplacement de pub récompensée. Les pubs sont TOUJOURS un choix du joueur
    /// (jamais d'interstitiel) : chaque emplacement a sa récompense et son délai de
    /// rechargement. Voir docs/BUSINESS-PLAN.md.
    /// </summary>
    public class RewardedAdDef
    {
        public string id;
        public double cooldownSeconds;

        /// <summary>Perles données, si l'emplacement paie en perles.</summary>
        public double pearls;

        /// <summary>Boost accordé, si l'emplacement paie en temps de boost.</summary>
        public string boostId;
    }

    /// <summary>Un pack de perles du magasin (achat réel — la validation appartient à l'hôte).</summary>
    public class PearlPackDef
    {
        public string id;
        public double pearls;

        /// <summary>Perles offertes en plus (mise en avant des gros packs).</summary>
        public double bonusPearls;

        /// <summary>Prix affiché tel quel : le vrai prix vient du store à l'exécution.</summary>
        public string priceLabel;
    }

    /// <summary>
    /// La boutique : perles, boosts temporaires, pubs récompensées et packs.
    /// Le Core ne connaît ni SDK de pub ni facturation — il applique des récompenses
    /// que la couche hôte déclenche une fois la pub vue ou l'achat validé.
    /// </summary>
    public static class Shop
    {
        public static BoostDef Boost(BalanceConfig config, string boostId)
        {
            for (int i = 0; i < config.boosts.Count; i++)
                if (config.boosts[i].id == boostId)
                    return config.boosts[i];
            return null;
        }

        public static RewardedAdDef Ad(BalanceConfig config, string adId)
        {
            for (int i = 0; i < config.rewardedAds.Count; i++)
                if (config.rewardedAds[i].id == adId)
                    return config.rewardedAds[i];
            return null;
        }

        public static PearlPackDef Pack(BalanceConfig config, string packId)
        {
            for (int i = 0; i < config.pearlPacks.Count; i++)
                if (config.pearlPacks[i].id == packId)
                    return config.pearlPacks[i];
            return null;
        }

        /// <summary>Secondes restantes d'un boost (0 s'il ne court pas).</summary>
        public static double BoostSecondsLeft(GameState state, string boostId)
            => state.Timer(state.boosts, boostId, false)?.secondsLeft ?? 0;

        public static bool IsBoostActive(GameState state, string boostId) => BoostSecondsLeft(state, boostId) > 0;

        /// <summary>
        /// Multiplicateur des boosts actifs de cette catégorie. Plusieurs boosts d'une
        /// même catégorie se multiplient — il n'y en a qu'un par catégorie en v1.
        /// </summary>
        public static double BoostMultiplier(BalanceConfig config, GameState state, BoostKind kind)
        {
            double multiplier = 1;
            for (int i = 0; i < config.boosts.Count; i++)
            {
                var def = config.boosts[i];
                if (def.kind != kind) continue;
                if (BoostSecondsLeft(state, def.id) > 0) multiplier *= def.multiplier;
            }
            return multiplier;
        }

        /// <summary>
        /// Ajoute une durée de boost, plafonnée au cumul maximal : relancer un boost déjà
        /// actif prolonge sans jamais dépasser le plafond (sinon un joueur pourrait
        /// empiler des journées entières de ×2 en une session).
        /// </summary>
        public static void GrantBoost(BalanceConfig config, GameState state, string boostId)
        {
            var def = Boost(config, boostId);
            if (def == null) return;
            var timer = state.Timer(state.boosts, boostId, true);
            double cap = def.maxStackSeconds > 0 ? def.maxStackSeconds : def.durationSeconds;
            timer.secondsLeft = System.Math.Min(cap, timer.secondsLeft + def.durationSeconds);
        }

        /// <summary>Achète un boost avec des perles. Faux si le boost est inconnu, gratuit ou impayable.</summary>
        public static bool BuyBoost(BalanceConfig config, GameState state, string boostId)
        {
            var def = Boost(config, boostId);
            if (def == null || def.pearlCost <= 0 || state.pearls < def.pearlCost) return false;
            state.pearls -= def.pearlCost;
            GrantBoost(config, state, boostId);
            return true;
        }

        /// <summary>Secondes avant que cette pub soit à nouveau disponible (0 = prête).</summary>
        public static double AdCooldownLeft(GameState state, string adId)
            => state.Timer(state.adCooldowns, adId, false)?.secondsLeft ?? 0;

        public static bool IsAdReady(BalanceConfig config, GameState state, string adId)
            => Ad(config, adId) != null && AdCooldownLeft(state, adId) <= 0;

        /// <summary>
        /// Encaisse la récompense d'une pub que le joueur VIENT DE REGARDER : c'est
        /// l'hôte (SDK) qui garantit ce fait, le Core ne fait qu'appliquer et recharger
        /// le délai. Faux si la pub n'est pas prête.
        /// </summary>
        public static bool ClaimAdReward(BalanceConfig config, GameState state, string adId)
        {
            if (!IsAdReady(config, state, adId)) return false;
            var def = Ad(config, adId);

            if (def.pearls > 0) state.pearls += def.pearls;
            if (!string.IsNullOrEmpty(def.boostId)) GrantBoost(config, state, def.boostId);

            state.Timer(state.adCooldowns, adId, true).secondsLeft = def.cooldownSeconds;
            return true;
        }

        /// <summary>
        /// Crédite un pack de perles APRÈS validation de l'achat par la couche hôte
        /// (store). Le Core ne facture rien : il enregistre une livraison.
        /// </summary>
        public static double GrantPack(BalanceConfig config, GameState state, string packId)
        {
            var pack = Pack(config, packId);
            if (pack == null) return 0;
            double total = pack.pearls + pack.bonusPearls;
            state.pearls += total;
            return total;
        }

        /// <summary>Achète un coffre avec des perles (le même coffre que celui payé en pièces).</summary>
        public static ChestResult OpenChestWithPearls(BalanceConfig config, GameState state, string chestId, double roll)
        {
            var chest = Equipment.Chest(config, chestId);
            if (chest == null || chest.pearlCost <= 0 || state.pearls < chest.pearlCost) return null;

            // On paie en perles, puis on ouvre en neutralisant le prix en pièces :
            // le tirage lui-même vit dans Equipment, une seule table de rareté.
            state.pearls -= chest.pearlCost;
            double money = state.money;
            state.money = chest.cost;
            var result = Equipment.OpenChest(config, state, chestId, roll);
            state.money = money;
            return result;
        }

        /// <summary>Fait couler le temps des boosts et des délais de pub.</summary>
        public static void Tick(GameState state, double dt)
        {
            if (dt <= 0) return;
            Countdown(state.boosts, dt);
            Countdown(state.adCooldowns, dt);
        }

        static void Countdown(List<TimerState> timers, double dt)
        {
            for (int i = 0; i < timers.Count; i++)
            {
                if (timers[i].secondsLeft <= 0) continue;
                timers[i].secondsLeft -= dt;
                if (timers[i].secondsLeft < 0) timers[i].secondsLeft = 0;
            }
        }
    }
}
