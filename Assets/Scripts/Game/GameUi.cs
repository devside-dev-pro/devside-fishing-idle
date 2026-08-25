using System.Collections.Generic;
using Devside.FishingIdle.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// UI mobile portrait du jeu, construite entièrement par code au démarrage (voir UiKit).
    /// Layout de référence 1080×1920 : bandeau de stats en haut, boutique scrollable au
    /// centre, gros bouton de pêche en bas. Tous les libellés viennent de GameTheme.
    /// À poser sur le même GameObject que GameBootstrap (DevHud est désactivé automatiquement).
    /// </summary>
    [RequireComponent(typeof(GameBootstrap))]
    public class GameUi : MonoBehaviour
    {
        const float HeaderHeight = 400f;
        const float FooterHeight = 340f;

        static readonly Color HeaderBg = new Color(0.05f, 0.11f, 0.18f, 0.96f);
        static readonly Color ListBg = new Color(0.03f, 0.07f, 0.12f, 0.85f);
        static readonly Color RowBg = new Color(1f, 1f, 1f, 0.07f);
        static readonly Color Accent = new Color(0.13f, 0.55f, 0.6f);
        static readonly Color AccentWarm = new Color(0.85f, 0.5f, 0.12f);
        static readonly Color TextMain = Color.white;
        static readonly Color TextDim = new Color(1f, 1f, 1f, 0.65f);
        static readonly Color HoldBarColor = new Color(0.35f, 0.75f, 0.95f);

        class ShopRow
        {
            public GameObject root;
            public Text label;
            public Button button;
            public Text buttonLabel;
        }

        Text _moneyText;
        Text _stocksText;
        Text _metaText;
        Text _offlineText;
        Text _holdText;
        RectTransform _holdFill;

        Text _catchFeedback;
        Button _sellButton;
        Button _prestigeButton;
        Text _prestigeLabel;

        readonly Dictionary<string, ShopRow> _producerRows = new Dictionary<string, ShopRow>();
        readonly Dictionary<string, ShopRow> _upgradeRows = new Dictionary<string, ShopRow>();

        void Start()
        {
            var devHud = GetComponent<DevHud>();
            if (devHud != null) devHud.enabled = false;

            EnsureEventSystem();
            var canvas = CreateCanvas();
            BuildHeader(canvas);
            BuildShop(canvas);
            BuildFooter(canvas);
            ShowOfflineSummary();
        }

        void Update()
        {
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.State == null) return;
            var config = boot.Config;
            var state = boot.State;

            _moneyText.text = $"{Numbers.Format(state.money)} {GameTheme.MoneySuffix}";
            _stocksText.text =
                $"{GameTheme.RawLabel} {Numbers.Format(state.rawFish)}   ·   " +
                $"{GameTheme.CutLabel} {Numbers.Format(state.cutFish)}   ·   " +
                $"{GameTheme.FilletLabel} {Numbers.Format(state.fillet)}";
            _metaText.text =
                $"{GameTheme.DepthLabel} {Catching.DepthLevel(config, state)}   ·   " +
                $"{GameTheme.CollectionLabel} {state.discoveredSpecies.Count}/{config.species.Count}   ·   " +
                $"{GameTheme.PrestigeAction} {state.prestigePoints}";

            double capacity = Multipliers.HoldCapacity(config, state);
            double ratio = capacity <= 0 ? 0 : Mathf.Clamp01((float)(state.TotalFishStock / capacity));
            _holdFill.anchorMax = new Vector2((float)ratio, 1f);
            _holdText.text = $"{GameTheme.HoldLabel}  {Numbers.Format(state.TotalFishStock)} / {Numbers.Format(capacity)}";

            foreach (var pair in _producerRows)
            {
                var def = config.Producer(pair.Key);
                int owned = state.ProducerCount(def.id);
                double cost = Economy.ProducerCost(def, owned);
                pair.Value.label.text = $"{GameTheme.Producer(def.id)}   ×{owned}";
                pair.Value.buttonLabel.text = Numbers.Format(cost);
                pair.Value.button.interactable = state.money >= cost;
            }

            foreach (var pair in _upgradeRows)
            {
                var def = config.Upgrade(pair.Key);
                int level = state.UpgradeLevel(def.id);
                bool visible = level < def.maxLevel;
                if (pair.Value.root.activeSelf != visible) pair.Value.root.SetActive(visible);
                if (!visible) continue;
                double cost = Economy.UpgradeCost(def, level);
                pair.Value.label.text = $"{GameTheme.Upgrade(def.id)}   {GameTheme.LevelAbbrev} {level}";
                pair.Value.buttonLabel.text = Numbers.Format(cost);
                pair.Value.button.interactable = state.money >= cost;
            }

            bool sellVisible = !state.autoSellUnlocked;
            if (_sellButton.gameObject.activeSelf != sellVisible) _sellButton.gameObject.SetActive(sellVisible);

            int pending = Prestige.PendingPoints(config, state);
            bool prestigeVisible = pending > 0;
            if (_prestigeButton.gameObject.activeSelf != prestigeVisible) _prestigeButton.gameObject.SetActive(prestigeVisible);
            if (prestigeVisible) _prestigeLabel.text = $"{GameTheme.PrestigeAction}  +{pending}";
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        static Transform CreateCanvas()
        {
            var go = new GameObject("GameUi", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            return go.transform;
        }

        void BuildHeader(Transform canvas)
        {
            var header = UiKit.CreatePanel("Header", canvas, HeaderBg);
            UiKit.AnchorTop(header.rectTransform, 0, HeaderHeight);

            _moneyText = UiKit.CreateText("Money", header.transform, 84, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.AnchorTop(_moneyText.rectTransform, 30, 100, 30);

            _stocksText = UiKit.CreateText("Stocks", header.transform, 38, TextDim, TextAnchor.MiddleCenter);
            UiKit.AnchorTop(_stocksText.rectTransform, 140, 50, 30);

            _metaText = UiKit.CreateText("Meta", header.transform, 34, TextDim, TextAnchor.MiddleCenter);
            UiKit.AnchorTop(_metaText.rectTransform, 195, 46, 30);

            var holdBar = UiKit.CreatePanel("HoldBar", header.transform, new Color(1, 1, 1, 0.12f));
            UiKit.AnchorTop(holdBar.rectTransform, 260, 30, 60);
            var fill = UiKit.CreatePanel("Fill", holdBar.transform, HoldBarColor);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            _holdFill = fill.rectTransform;

            _holdText = UiKit.CreateText("HoldText", header.transform, 32, TextDim, TextAnchor.MiddleCenter);
            UiKit.AnchorTop(_holdText.rectTransform, 295, 44, 60);

            _offlineText = UiKit.CreateText("Offline", header.transform, 30, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
            UiKit.AnchorTop(_offlineText.rectTransform, 345, 44, 30);
            _offlineText.gameObject.SetActive(false);
        }

        void BuildShop(Transform canvas)
        {
            var boot = GameBootstrap.Instance;
            var content = UiKit.CreateScrollList("Shop", canvas, ListBg);
            UiKit.AnchorVerticalSpan((RectTransform)content.parent, HeaderHeight, FooterHeight);

            CreateSectionTitle(content, GameTheme.ProducersSection);
            foreach (var def in boot.Config.producers)
            {
                string id = def.id;
                var row = CreateShopRow(content, () => Economy.TryBuyProducer(boot.Config, boot.State, id));
                _producerRows[id] = row;
            }

            CreateSectionTitle(content, GameTheme.UpgradesSection);
            foreach (var def in boot.Config.upgrades)
            {
                string id = def.id;
                var row = CreateShopRow(content, () => Economy.TryBuyUpgrade(boot.Config, boot.State, id));
                _upgradeRows[id] = row;
            }
        }

        void BuildFooter(Transform canvas)
        {
            var footer = UiKit.CreatePanel("Footer", canvas, HeaderBg);
            UiKit.AnchorBottom(footer.rectTransform, 0, FooterHeight);

            _catchFeedback = UiKit.CreateText("CatchFeedback", footer.transform, 36, TextMain, TextAnchor.MiddleCenter);
            UiKit.AnchorTop(_catchFeedback.rectTransform, 12, 50, 30);
            _catchFeedback.text = "";

            var (castButton, castLabel) = UiKit.CreateButton("Cast", footer.transform, Accent, 60);
            castLabel.fontStyle = FontStyle.Bold;
            castLabel.text = GameTheme.CastAction;
            UiKit.AnchorBottom(((RectTransform)castButton.transform), 40, 170, 60);
            castButton.onClick.AddListener(OnCastClicked);

            // « Tout vendre » à gauche, « PRESTIGE » à droite : jamais de chevauchement
            // même quand les deux sont visibles.
            var (sellButton, sellLabel) = UiKit.CreateButton("SellAll", footer.transform, new Color(0.3f, 0.35f, 0.45f), 36);
            sellLabel.text = GameTheme.SellAllAction;
            var sellRt = (RectTransform)sellButton.transform;
            sellRt.anchorMin = new Vector2(0f, 1f);
            sellRt.anchorMax = new Vector2(0.5f, 1f);
            sellRt.offsetMin = new Vector2(60, -138);
            sellRt.offsetMax = new Vector2(-12, -72);
            sellButton.onClick.AddListener(() =>
            {
                var boot = GameBootstrap.Instance;
                Economy.SellAll(boot.Config, boot.State);
            });
            _sellButton = sellButton;

            var (prestigeButton, prestigeLabel) = UiKit.CreateButton("Prestige", footer.transform, AccentWarm, 36);
            var prestigeRt = (RectTransform)prestigeButton.transform;
            prestigeRt.anchorMin = new Vector2(0.5f, 1f);
            prestigeRt.anchorMax = new Vector2(1f, 1f);
            prestigeRt.offsetMin = new Vector2(12, -138);
            prestigeRt.offsetMax = new Vector2(-60, -72);
            prestigeButton.onClick.AddListener(() =>
            {
                var boot = GameBootstrap.Instance;
                Prestige.Execute(boot.Config, boot.State);
                _catchFeedback.text = "";
            });
            _prestigeButton = prestigeButton;
            _prestigeLabel = prestigeLabel;
            prestigeButton.gameObject.SetActive(false);
        }

        void OnCastClicked()
        {
            var boot = GameBootstrap.Instance;
            var result = Simulation.CastLine(boot.Config, boot.State, Random.value);
            if (result.amount <= 0)
            {
                _catchFeedback.text = GameTheme.HoldFullMessage;
                return;
            }
            string discovery = result.newDiscovery ? $"   {GameTheme.NewDiscovery}" : "";
            _catchFeedback.text = $"{GameTheme.Species(result.speciesId)}  +{Numbers.Format(result.amount)}{discovery}";
        }

        void ShowOfflineSummary()
        {
            var offline = GameBootstrap.Instance.LastOffline;
            if (offline == null || offline.simulatedSeconds < 60 || offline.stockGained <= 0) return;
            string holdNote = offline.holdFull ? $" — {GameTheme.OfflineHoldFull}" : "";
            _offlineText.text =
                $"{GameTheme.OfflinePrefix} : +{Numbers.Format(offline.stockGained)} {GameTheme.FishUnit}{holdNote}";
            _offlineText.gameObject.SetActive(true);
        }

        static void CreateSectionTitle(Transform parent, string title)
        {
            var text = UiKit.CreateText("Section", parent, 36, TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.text = title;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 70;
        }

        ShopRow CreateShopRow(Transform parent, System.Action onBuy)
        {
            var panel = UiKit.CreatePanel("Row", parent, RowBg);
            panel.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;

            var label = UiKit.CreateText("Name", panel.transform, 40, TextMain);
            var labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(28, 0);
            labelRt.offsetMax = new Vector2(-340, 0);

            var (button, buttonLabel) = UiKit.CreateButton("Buy", panel.transform, Accent, 36);
            var buttonRt = (RectTransform)button.transform;
            buttonRt.anchorMin = new Vector2(1, 0.5f);
            buttonRt.anchorMax = new Vector2(1, 0.5f);
            buttonRt.pivot = new Vector2(1, 0.5f);
            buttonRt.anchoredPosition = new Vector2(-20, 0);
            buttonRt.sizeDelta = new Vector2(300, 88);
            button.onClick.AddListener(() => onBuy());

            return new ShopRow { root = panel.gameObject, label = label, button = button, buttonLabel = buttonLabel };
        }
    }
}
