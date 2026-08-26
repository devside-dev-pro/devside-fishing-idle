using System.Collections.Generic;
using Devside.FishingIdle.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Surcouche UI du diorama (BoatView) — portrait mobile 1080×1920, bâtie par code.
    /// Look « jeu mobile » : cartes flottantes arrondies avec ombres, boutons à liseré et
    /// face colorée, textes gras à contour, icônes générées (Resources/UI/Icons, l'UI
    /// reste correcte si elles manquent). Le centre de l'écran appartient à la scène 3D ;
    /// on pêche en tapant l'eau ou via le bouton central. Libellés : GameTheme uniquement.
    /// </summary>
    [RequireComponent(typeof(GameBootstrap))]
    public class GameUi : MonoBehaviour
    {
        const float HeaderHeight = 332f;
        const float BottomBarHeight = 190f;
        const float PrestigeBandHeight = 92f;
        const float PanelHeight = 880f;

        static readonly Color CardBg = new Color(0.07f, 0.16f, 0.24f, 0.97f);
        static readonly Color PanelBg = new Color(0.06f, 0.13f, 0.2f, 0.98f);
        static readonly Color RowBg = new Color(1f, 1f, 1f, 0.08f);
        static readonly Color MoneyGreen = new Color(0.22f, 0.58f, 0.28f);
        static readonly Color BuyGreen = new Color(0.24f, 0.62f, 0.3f);
        static readonly Color CastTeal = new Color(0.1f, 0.58f, 0.62f);
        static readonly Color TabBlue = new Color(0.12f, 0.3f, 0.45f);
        static readonly Color SellGray = new Color(0.32f, 0.4f, 0.5f);
        static readonly Color PrestigeOrange = new Color(0.9f, 0.55f, 0.14f);
        static readonly Color TextMain = Color.white;
        static readonly Color TextDim = new Color(1f, 1f, 1f, 0.72f);
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
        Text _holdText;
        Text _offlineText;
        RectTransform _holdFill;
        Button _sellButton;

        GameObject _catchBannerCard;
        Text _catchBanner;
        float _catchBannerUntil;

        GameObject _producersPanel;
        GameObject _upgradesPanel;
        Button _prestigeButton;
        Text _prestigeLabel;

        readonly Dictionary<string, ShopRow> _producerRows = new Dictionary<string, ShopRow>();
        readonly Dictionary<string, ShopRow> _upgradeRows = new Dictionary<string, ShopRow>();

        void Start()
        {
            var devHud = GetComponent<DevHud>();
            if (devHud != null) devHud.enabled = false;
            if (GetComponent<BoatView>() == null) gameObject.AddComponent<BoatView>();

            EnsureEventSystem();
            var canvas = CreateCanvas();
            BuildHeader(canvas);
            BuildCatchBanner(canvas);
            BuildPanels(canvas);
            BuildPrestigeBand(canvas);
            BuildBottomBar(canvas);
            ShowOfflineSummary();
        }

        void Update()
        {
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.State == null) return;
            var config = boot.Config;
            var state = boot.State;

            RefreshHeader(config, state);
            RefreshRows(config, state);

            bool sellVisible = !state.autoSellUnlocked;
            if (_sellButton.gameObject.activeSelf != sellVisible) _sellButton.gameObject.SetActive(sellVisible);

            int pending = Prestige.PendingPoints(config, state);
            bool prestigeVisible = pending > 0;
            if (_prestigeButton.gameObject.activeSelf != prestigeVisible) _prestigeButton.gameObject.SetActive(prestigeVisible);
            if (prestigeVisible) _prestigeLabel.text = $"{GameTheme.PrestigeAction}  +{pending}";

            if (_catchBannerCard.activeSelf && Time.time > _catchBannerUntil)
                _catchBannerCard.SetActive(false);

            if (PointerDownOnScene())
                Cast(PointerScreenPosition());
        }

        // ---------- Actions ----------

        void Cast(Vector2 screenPosition)
        {
            var boot = GameBootstrap.Instance;
            var result = Simulation.CastLine(boot.Config, boot.State, Random.value);

            if (result.amount <= 0)
            {
                ShowBanner(GameTheme.HoldFullMessage);
                return;
            }
            string discovery = result.newDiscovery ? $"   {GameTheme.NewDiscovery}" : "";
            ShowBanner($"{GameTheme.Species(result.speciesId)}  +{Numbers.Format(result.amount)}{discovery}");
            if (BoatView.Instance != null) BoatView.Instance.PlayCatchEffect(screenPosition, result);
        }

        void ShowBanner(string text)
        {
            _catchBanner.text = text;
            _catchBannerCard.SetActive(true);
            _catchBannerUntil = Time.time + 2.2f;
        }

        void TogglePanel(GameObject panel, GameObject other)
        {
            panel.SetActive(!panel.activeSelf);
            other.SetActive(false);
        }

        // ---------- Saisie ----------

        static bool PointerDownOnScene()
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase != TouchPhase.Began) return false;
                return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId);
            }
            if (!Input.GetMouseButtonDown(0)) return false;
            return EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject();
        }

        static Vector2 PointerScreenPosition()
            => Input.touchCount > 0 ? Input.GetTouch(0).position : (Vector2)Input.mousePosition;

        // ---------- Rafraîchissement ----------

        void RefreshHeader(BalanceConfig config, GameState state)
        {
            _moneyText.text = Numbers.Format(state.money);
            _stocksText.text =
                $"{GameTheme.RawLabel} {Numbers.Format(state.rawFish)}   ·   " +
                $"{GameTheme.CutLabel} {Numbers.Format(state.cutFish)}   ·   " +
                $"{GameTheme.FilletLabel} {Numbers.Format(state.fillet)}";
            _metaText.text =
                $"{GameTheme.DepthLabel} {Catching.DepthLevel(config, state)}   ·   " +
                $"{GameTheme.CollectionLabel} {state.discoveredSpecies.Count}/{config.species.Count}   ·   " +
                $"{GameTheme.PrestigeAction} {state.prestigePoints}";

            double capacity = Multipliers.HoldCapacity(config, state);
            float ratio = capacity <= 0 ? 0f : Mathf.Clamp01((float)(state.TotalFishStock / capacity));
            _holdFill.anchorMax = new Vector2(ratio, 1f);
            _holdText.text = $"{GameTheme.HoldLabel}  {Numbers.Format(state.TotalFishStock)} / {Numbers.Format(capacity)}";
        }

        void RefreshRows(BalanceConfig config, GameState state)
        {
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
        }

        // ---------- Construction ----------

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
            var header = UiKit.CreateCard("Header", canvas, CardBg);
            UiKit.AnchorTop(header.rectTransform, 14, HeaderHeight, 14);

            // Pilule d'argent, façon compteur de cash des jeux mobiles.
            var moneyPill = UiKit.CreateCard("MoneyPill", header.transform, MoneyGreen, shadow: false);
            var pillRt = moneyPill.rectTransform;
            pillRt.anchorMin = new Vector2(0.5f, 1f);
            pillRt.anchorMax = new Vector2(0.5f, 1f);
            pillRt.pivot = new Vector2(0.5f, 1f);
            pillRt.anchoredPosition = new Vector2(0f, -14f);
            pillRt.sizeDelta = new Vector2(540f, 96f);

            var coinIcon = UiKit.Icon("coin");
            if (coinIcon != null)
            {
                var icon = UiKit.CreateRect("Coin", moneyPill.transform).gameObject.AddComponent<Image>();
                icon.sprite = coinIcon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                var iconRt = icon.rectTransform;
                iconRt.anchorMin = new Vector2(0f, 0.5f);
                iconRt.anchorMax = new Vector2(0f, 0.5f);
                iconRt.pivot = new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = new Vector2(14f, 0f);
                iconRt.sizeDelta = new Vector2(68f, 68f);
            }

            _moneyText = UiKit.CreateText("Money", moneyPill.transform, 60, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.AddOutline(_moneyText, 2f);
            UiKit.Stretch(_moneyText.rectTransform, 90, 24, 0, 0);

            _stocksText = UiKit.CreateText("Stocks", header.transform, 34, TextDim, TextAnchor.MiddleCenter);
            UiKit.AnchorTop(_stocksText.rectTransform, 122, 44, 30);

            _metaText = UiKit.CreateText("Meta", header.transform, 30, TextDim, TextAnchor.MiddleCenter);
            UiKit.AnchorTop(_metaText.rectTransform, 168, 40, 30);

            var barrelIcon = UiKit.Icon("barrel");
            if (barrelIcon != null)
            {
                var icon = UiKit.CreateRect("Barrel", header.transform).gameObject.AddComponent<Image>();
                icon.sprite = barrelIcon;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                icon.rectTransform.anchorMin = new Vector2(0f, 1f);
                icon.rectTransform.anchorMax = new Vector2(0f, 1f);
                icon.rectTransform.pivot = new Vector2(0f, 1f);
                icon.rectTransform.anchoredPosition = new Vector2(36f, -218f);
                icon.rectTransform.sizeDelta = new Vector2(52f, 52f);
            }

            var holdBar = UiKit.CreateCard("HoldBar", header.transform, new Color(0f, 0f, 0f, 0.35f), shadow: false);
            var holdBarRt = holdBar.rectTransform;
            holdBarRt.anchorMin = new Vector2(0f, 1f);
            holdBarRt.anchorMax = new Vector2(0.62f, 1f);
            holdBarRt.offsetMin = new Vector2(104f, -250f);
            holdBarRt.offsetMax = new Vector2(-8f, -224f);
            var fill = UiKit.CreateCard("Fill", holdBar.transform, HoldBarColor, shadow: false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = new Vector2(3f, 3f);
            fill.rectTransform.offsetMax = new Vector2(-3f, -3f);
            _holdFill = fill.rectTransform;

            _holdText = UiKit.CreateText("HoldText", header.transform, 28, TextDim, TextAnchor.MiddleLeft);
            var holdTextRt = _holdText.rectTransform;
            holdTextRt.anchorMin = new Vector2(0f, 1f);
            holdTextRt.anchorMax = new Vector2(0.62f, 1f);
            holdTextRt.offsetMin = new Vector2(106f, -296f);
            holdTextRt.offsetMax = new Vector2(-6f, -254f);

            var (sellButton, sellLabel, sellRect) = UiKit.CreateFancyButton("SellAll", header.transform, SellGray, 30);
            sellLabel.text = GameTheme.SellAllAction;
            sellRect.anchorMin = new Vector2(0.64f, 1f);
            sellRect.anchorMax = new Vector2(1f, 1f);
            sellRect.offsetMin = new Vector2(4f, -304f);
            sellRect.offsetMax = new Vector2(-22f, -222f);
            sellButton.onClick.AddListener(() =>
            {
                var boot = GameBootstrap.Instance;
                Economy.SellAll(boot.Config, boot.State);
            });
            _sellButton = sellButton;

            _offlineText = UiKit.CreateText("Offline", canvas, 30, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
            UiKit.AddOutline(_offlineText, 1.2f);
            UiKit.AnchorTop(_offlineText.rectTransform, HeaderHeight + 26, 44, 30);
            _offlineText.gameObject.SetActive(false);
        }

        void BuildCatchBanner(Transform canvas)
        {
            var card = UiKit.CreateCard("CatchBanner", canvas, new Color(0.04f, 0.1f, 0.16f, 0.9f), shadow: false);
            UiKit.AnchorTop(card.rectTransform, HeaderHeight + 78, 62, 110);
            _catchBanner = UiKit.CreateText("Text", card.transform, 36, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.AddOutline(_catchBanner, 1.4f);
            UiKit.Stretch(_catchBanner.rectTransform, 16, 16, 0, 0);
            _catchBannerCard = card.gameObject;
            _catchBannerCard.SetActive(false);
        }

        void BuildPanels(Transform canvas)
        {
            var boot = GameBootstrap.Instance;

            _producersPanel = BuildPanel(canvas, GameTheme.ProducersSection, out var producersContent);
            foreach (var def in boot.Config.producers)
            {
                string id = def.id;
                _producerRows[id] = CreateShopRow(producersContent, () => Economy.TryBuyProducer(boot.Config, boot.State, id));
            }

            _upgradesPanel = BuildPanel(canvas, GameTheme.UpgradesSection, out var upgradesContent);
            foreach (var def in boot.Config.upgrades)
            {
                string id = def.id;
                _upgradeRows[id] = CreateShopRow(upgradesContent, () => Economy.TryBuyUpgrade(boot.Config, boot.State, id));
            }
        }

        GameObject BuildPanel(Transform canvas, string title, out RectTransform content)
        {
            var panel = UiKit.CreateCard("Panel", canvas, PanelBg);
            UiKit.AnchorBottom(panel.rectTransform, BottomBarHeight + PrestigeBandHeight + 6, PanelHeight, 16);

            var titleText = UiKit.CreateText("Title", panel.transform, 40, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.AddOutline(titleText, 1.6f);
            titleText.text = title;
            UiKit.AnchorTop(titleText.rectTransform, 14, 58, 20);

            content = UiKit.CreateScrollList("List", panel.transform, new Color(0f, 0f, 0f, 0.18f));
            UiKit.AnchorVerticalSpan((RectTransform)content.parent, 84, 16, 14);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        void BuildPrestigeBand(Transform canvas)
        {
            var (button, label, rect) = UiKit.CreateFancyButton("Prestige", canvas, PrestigeOrange, 40);
            label.fontStyle = FontStyle.Bold;
            UiKit.AnchorBottom(rect, BottomBarHeight + 10, PrestigeBandHeight - 14, 40);
            button.onClick.AddListener(() =>
            {
                var boot = GameBootstrap.Instance;
                Prestige.Execute(boot.Config, boot.State);
                _producersPanel.SetActive(false);
                _upgradesPanel.SetActive(false);
            });
            _prestigeButton = button;
            _prestigeLabel = label;
            button.gameObject.SetActive(false);
        }

        void BuildBottomBar(Transform canvas)
        {
            var bar = UiKit.CreateCard("BottomBar", canvas, CardBg);
            UiKit.AnchorBottom(bar.rectTransform, 12, BottomBarHeight, 14);

            var (crewTab, crewLabel, crewRect) = UiKit.CreateFancyButton("CrewTab", bar.transform, TabBlue, 30, UiKit.Icon("crew"));
            crewLabel.text = GameTheme.CrewTab;
            SetBarSlot(crewRect, 0.02f, 0.32f);
            crewTab.onClick.AddListener(() => TogglePanel(_producersPanel, _upgradesPanel));

            var (castButton, castLabel, castRect) = UiKit.CreateFancyButton("Cast", bar.transform, CastTeal, 38, UiKit.Icon("fish"));
            castLabel.text = GameTheme.CastAction;
            SetBarSlot(castRect, 0.34f, 0.66f);
            castButton.onClick.AddListener(() => Cast(new Vector2(Screen.width * 0.62f, Screen.height * 0.45f)));

            var (upgradesTab, upgradesLabel, upgradesRect) = UiKit.CreateFancyButton("UpgradesTab", bar.transform, TabBlue, 30, UiKit.Icon("upgrade"));
            upgradesLabel.text = GameTheme.UpgradesTab;
            SetBarSlot(upgradesRect, 0.68f, 0.98f);
            upgradesTab.onClick.AddListener(() => TogglePanel(_upgradesPanel, _producersPanel));
        }

        static void SetBarSlot(RectTransform rt, float xMin, float xMax)
        {
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = new Vector2(0f, 18f);
            rt.offsetMax = new Vector2(0f, -18f);
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

        ShopRow CreateShopRow(Transform parent, System.Action onBuy)
        {
            var card = UiKit.CreateCard("Row", parent, RowBg, shadow: false);
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 122;

            var label = UiKit.CreateText("Name", card.transform, 38, TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.AddOutline(label, 1.2f);
            var labelRt = label.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(30f, 0f);
            labelRt.offsetMax = new Vector2(-330f, 0f);

            var (button, buttonLabel, buttonRect) = UiKit.CreateFancyButton("Buy", card.transform, BuyGreen, 34);
            buttonRect.anchorMin = new Vector2(1f, 0.5f);
            buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-16f, 0f);
            buttonRect.sizeDelta = new Vector2(300f, 94f);
            button.onClick.AddListener(() => onBuy());

            return new ShopRow { root = card.gameObject, label = label, button = button, buttonLabel = buttonLabel };
        }
    }
}
