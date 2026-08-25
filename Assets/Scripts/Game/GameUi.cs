using System.Collections.Generic;
using Devside.FishingIdle.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Surcouche UI du diorama (BoatView) — portrait mobile, référence 1080×1920, bâtie par
    /// code (UiKit). Le centre de l'écran appartient à la scène 3D : bandeau de stats en
    /// haut, barre d'onglets en bas (Équipage / Pêcher / Améliorations) qui ouvre des
    /// panneaux, et on pêche aussi en tapant directement sur l'eau. Tous les libellés
    /// viennent de GameTheme. Ajoute BoatView automatiquement.
    /// </summary>
    [RequireComponent(typeof(GameBootstrap))]
    public class GameUi : MonoBehaviour
    {
        const float HeaderHeight = 330f;
        const float BottomBarHeight = 170f;
        const float PrestigeBandHeight = 92f;
        const float PanelHeight = 900f;

        static readonly Color HeaderBg = new Color(0.05f, 0.11f, 0.18f, 0.96f);
        static readonly Color PanelBg = new Color(0.04f, 0.09f, 0.15f, 0.97f);
        static readonly Color RowBg = new Color(1f, 1f, 1f, 0.07f);
        static readonly Color Accent = new Color(0.13f, 0.55f, 0.6f);
        static readonly Color AccentWarm = new Color(0.85f, 0.5f, 0.12f);
        static readonly Color TabColor = new Color(0.1f, 0.2f, 0.3f);
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
        Text _holdText;
        Text _offlineText;
        RectTransform _holdFill;
        Button _sellButton;

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

            if (_catchBanner.gameObject.activeSelf && Time.time > _catchBannerUntil)
                _catchBanner.gameObject.SetActive(false);

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
            _catchBanner.gameObject.SetActive(true);
            _catchBannerUntil = Time.time + 2.2f;
        }

        void TogglePanel(GameObject panel, GameObject other)
        {
            bool show = !panel.activeSelf;
            panel.SetActive(show);
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
            var header = UiKit.CreatePanel("Header", canvas, HeaderBg);
            UiKit.AnchorTop(header.rectTransform, 0, HeaderHeight);

            _moneyText = UiKit.CreateText("Money", header.transform, 80, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.AnchorTop(_moneyText.rectTransform, 22, 92, 30);

            _stocksText = UiKit.CreateText("Stocks", header.transform, 36, TextDim, TextAnchor.MiddleCenter);
            UiKit.AnchorTop(_stocksText.rectTransform, 118, 46, 30);

            _metaText = UiKit.CreateText("Meta", header.transform, 32, TextDim, TextAnchor.MiddleCenter);
            UiKit.AnchorTop(_metaText.rectTransform, 166, 42, 30);

            var holdBar = UiKit.CreatePanel("HoldBar", header.transform, new Color(1, 1, 1, 0.12f));
            UiKit.AnchorTop(holdBar.rectTransform, 222, 26, 60);
            var fill = UiKit.CreatePanel("Fill", holdBar.transform, HoldBarColor);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = new Vector2(0, 1);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;
            _holdFill = fill.rectTransform;

            _holdText = UiKit.CreateText("HoldText", header.transform, 30, TextDim, TextAnchor.MiddleLeft);
            var holdRt = _holdText.rectTransform;
            holdRt.anchorMin = new Vector2(0f, 1f);
            holdRt.anchorMax = new Vector2(0.6f, 1f);
            holdRt.offsetMin = new Vector2(60, -300);
            holdRt.offsetMax = new Vector2(-6, -254);

            var (sellButton, sellLabel) = UiKit.CreateButton("SellAll", header.transform, new Color(0.3f, 0.35f, 0.45f), 30);
            sellLabel.text = GameTheme.SellAllAction;
            var sellRt = (RectTransform)sellButton.transform;
            sellRt.anchorMin = new Vector2(0.6f, 1f);
            sellRt.anchorMax = new Vector2(1f, 1f);
            sellRt.offsetMin = new Vector2(6, -308);
            sellRt.offsetMax = new Vector2(-60, -250);
            sellButton.onClick.AddListener(() =>
            {
                var boot = GameBootstrap.Instance;
                Economy.SellAll(boot.Config, boot.State);
            });
            _sellButton = sellButton;

            _offlineText = UiKit.CreateText("Offline", canvas, 30, new Color(1f, 0.85f, 0.4f), TextAnchor.MiddleCenter);
            UiKit.AnchorTop(_offlineText.rectTransform, HeaderHeight + 8, 44, 30);
            _offlineText.gameObject.SetActive(false);
        }

        void BuildCatchBanner(Transform canvas)
        {
            _catchBanner = UiKit.CreateText("CatchBanner", canvas, 40, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.AnchorTop(_catchBanner.rectTransform, HeaderHeight + 56, 54, 30);
            _catchBanner.gameObject.SetActive(false);
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
            var panel = UiKit.CreatePanel("Panel", canvas, PanelBg);
            UiKit.AnchorBottom(panel.rectTransform, BottomBarHeight + PrestigeBandHeight, PanelHeight, 16);

            var titleText = UiKit.CreateText("Title", panel.transform, 40, TextMain, TextAnchor.MiddleCenter, FontStyle.Bold);
            titleText.text = title;
            UiKit.AnchorTop(titleText.rectTransform, 14, 60, 20);

            content = UiKit.CreateScrollList("List", panel.transform, new Color(0, 0, 0, 0.2f));
            UiKit.AnchorVerticalSpan((RectTransform)content.parent, 86, 16, 16);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        void BuildPrestigeBand(Transform canvas)
        {
            var (button, label) = UiKit.CreateButton("Prestige", canvas, AccentWarm, 40);
            label.fontStyle = FontStyle.Bold;
            UiKit.AnchorBottom(((RectTransform)button.transform), BottomBarHeight + 8, PrestigeBandHeight - 16, 40);
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
            var bar = UiKit.CreatePanel("BottomBar", canvas, HeaderBg);
            UiKit.AnchorBottom(bar.rectTransform, 0, BottomBarHeight);

            var (crewTab, crewLabel) = UiKit.CreateButton("CrewTab", bar.transform, TabColor, 34);
            crewLabel.text = GameTheme.CrewTab;
            SetBarSlot((RectTransform)crewTab.transform, 0f, 0.31f);
            crewTab.onClick.AddListener(() => TogglePanel(_producersPanel, _upgradesPanel));

            var (castButton, castLabel) = UiKit.CreateButton("Cast", bar.transform, Accent, 46);
            castLabel.fontStyle = FontStyle.Bold;
            castLabel.text = GameTheme.CastAction;
            SetBarSlot((RectTransform)castButton.transform, 0.33f, 0.67f);
            castButton.onClick.AddListener(() => Cast(new Vector2(Screen.width * 0.62f, Screen.height * 0.45f)));

            var (upgradesTab, upgradesLabel) = UiKit.CreateButton("UpgradesTab", bar.transform, TabColor, 34);
            upgradesLabel.text = GameTheme.UpgradesTab;
            SetBarSlot((RectTransform)upgradesTab.transform, 0.69f, 1f);
            upgradesTab.onClick.AddListener(() => TogglePanel(_upgradesPanel, _producersPanel));
        }

        static void SetBarSlot(RectTransform rt, float xMin, float xMax)
        {
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = new Vector2(xMin <= 0f ? 24 : 0, 24);
            rt.offsetMax = new Vector2(xMax >= 1f ? -24 : 0, -24);
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
