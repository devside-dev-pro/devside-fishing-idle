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
        public static GameUi Instance { get; private set; }

        const float HeaderHeight = 332f;
        const float BottomBarHeight = 190f;
        const float PrestigeBandHeight = 92f;
        const float PanelHeight = 880f;

        // Palette « candy » (référence playtest : UI mobiles modernes) — cartes
        // claires, texte encre foncée, boutons saturés à ombre dure.
        static readonly Color CardBg = new Color(1f, 1f, 1f, 0.96f);
        static readonly Color PanelBg = new Color(0.96f, 0.94f, 0.89f, 0.98f);
        static readonly Color RowBg = new Color(1f, 1f, 1f, 0.92f);
        static readonly Color MoneyGreen = new Color(0.33f, 0.72f, 0.32f);
        static readonly Color BuyOrange = new Color(0.96f, 0.62f, 0.16f);
        static readonly Color CastGreen = new Color(0.35f, 0.76f, 0.31f);
        static readonly Color TabBlue = new Color(0.25f, 0.6f, 0.86f);
        static readonly Color SellGreen = new Color(0.33f, 0.72f, 0.32f);
        static readonly Color PrestigeOrange = new Color(0.95f, 0.5f, 0.12f);
        static readonly Color TextMain = new Color(0.13f, 0.26f, 0.39f);
        static readonly Color TextDim = new Color(0.13f, 0.26f, 0.39f, 0.62f);
        static readonly Color HoldBarColor = new Color(0.3f, 0.7f, 0.95f);

        class ShopRow
        {
            public GameObject root;
            public Text label;
            public Text subLabel;
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

        /// <summary>Île marchande où le bateau est à quai (null en mer) — bonus au comptoir.</summary>
        WorldMap.Island _merchantHere;

        GameObject _catchBannerCard;
        Text _catchBanner;
        float _catchBannerUntil;

        GameObject _boatPanel;
        GameObject _mapPanel;
        GameObject _profilePanel;
        GameObject _shopPanel;
        Button _prestigeButton;
        Text _prestigeLabel;

        readonly Dictionary<string, ShopRow> _producerRows = new Dictionary<string, ShopRow>();
        readonly Dictionary<string, ShopRow> _upgradeRows = new Dictionary<string, ShopRow>();

        // Carte de l'archipel.
        class MapIslandMarker
        {
            public WorldMap.Island island;
            public Text label;
        }

        const float MapWorldRange = 200f;
        readonly List<MapIslandMarker> _mapIslands = new List<MapIslandMarker>();
        RectTransform _boatMarker;
        float _mapScale;

        // Profil (Poissodex + statistiques).
        class DexRow
        {
            public string id;
            public SpeciesDef def;
            public Text name;
            public Text bonus;
        }

        readonly List<DexRow> _dexRows = new List<DexRow>();
        Text _statLifetime;
        Text _statDiscovered;
        Text _statPrestige;
        Text _statZone;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            var devHud = GetComponent<DevHud>();
            if (devHud != null) devHud.enabled = false;
            // L'archipel et le pilotage d'abord : BoatView accroche la coque sous
            // BoatController.Root dès sa première frame.
            if (GetComponent<WorldMap>() == null) gameObject.AddComponent<WorldMap>();
            if (GetComponent<BoatController>() == null) gameObject.AddComponent<BoatController>();
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
            RefreshMerchant(config);

            // La vente manuelle N'EXISTE qu'au comptoir du marchand (retour playtest :
            // vendre depuis la mer enlevait tout l'intérêt de l'île). En mer, la cale
            // se remplit — il faut rentrer au port pour encaisser.
            bool sellVisible = _merchantHere != null;
            if (_sellButton.gameObject.activeSelf != sellVisible) _sellButton.gameObject.SetActive(sellVisible);

            int pending = Prestige.PendingPoints(config, state);
            bool prestigeVisible = pending > 0;
            if (_prestigeButton.gameObject.activeSelf != prestigeVisible) _prestigeButton.gameObject.SetActive(prestigeVisible);
            if (prestigeVisible) _prestigeLabel.text = $"{GameTheme.PrestigeAction}  +{pending}";

            if (_catchBannerCard.activeSelf && Time.time > _catchBannerUntil)
                _catchBannerCard.SetActive(false);
            if (_offlineText.gameObject.activeSelf && Time.time > _offlineTextUntil)
                _offlineText.gameObject.SetActive(false);

            if (_mapPanel.activeSelf) RefreshMap(config, state);
            if (_profilePanel.activeSelf) RefreshProfile(config, state);

            HandleScenePointer();
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

        /// <summary>
        /// À quai chez le marchand, la vente paie mieux : bannière d'accueil à
        /// l'arrivée et bouton « Tout vendre » qui devient le comptoir bonifié.
        /// </summary>
        void RefreshMerchant(BalanceConfig config)
        {
            var merchant = BoatController.Instance != null
                ? WorldMap.MerchantAt(BoatController.Instance.Root.position)
                : null;
            if (merchant == _merchantHere) return;

            _merchantHere = merchant;
            if (merchant != null)
                ShowBanner(string.Format(GameTheme.MerchantWelcomeFormat, GameTheme.Island(merchant.id)));
        }

        double CurrentSellMultiplier(BalanceConfig config)
            => _merchantHere != null ? config.merchantSellBonus : 1;

        /// <summary>Bandeau d'information sous le header (captures, zone atteinte, blocages).</summary>
        public void ShowBanner(string text)
        {
            _catchBanner.text = text;
            _catchBannerCard.SetActive(true);
            _catchBannerUntil = Time.time + 2.2f;
        }

        void TogglePanel(GameObject panel)
        {
            bool opening = !panel.activeSelf;
            CloseAllPanels();
            panel.SetActive(opening);
        }

        void CloseAllPanels()
        {
            _boatPanel.SetActive(false);
            _mapPanel.SetActive(false);
            _profilePanel.SetActive(false);
            _shopPanel.SetActive(false);
        }

        // ---------- Saisie : posé-glissé pour naviguer, tap pour pêcher ----------

        Vector2 _pointerStart;
        float _pointerStartTime;
        bool _pointerOnScene;
        bool _steering;

        /// <summary>Au-delà de ce déplacement (px à 1080 de large), le toucher devient pilotage.</summary>
        const float DragDeadZone = 40f;

        /// <summary>Déplacement (px de référence) donnant la pleine vitesse.</summary>
        const float DragFullSpeed = 190f;

        const float TapMaxDuration = 0.35f;

        /// <summary>
        /// Un doigt posé sur la scène et glissé dans n'importe quelle direction pilote le
        /// bateau (plus de joystick — retour playtest) ; un tap bref reste un lancer.
        /// </summary>
        void HandleScenePointer()
        {
            bool pressed, released;
            Vector2 position;
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                position = touch.position;
                pressed = touch.phase == TouchPhase.Began;
                released = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
            }
            else
            {
                position = Input.mousePosition;
                pressed = Input.GetMouseButtonDown(0);
                released = Input.GetMouseButtonUp(0);
            }
            bool held = !pressed && !released && (Input.touchCount > 0 || Input.GetMouseButton(0));

            if (pressed)
            {
                _pointerOnScene = !PointerOverUi();
                _pointerStart = position;
                _pointerStartTime = Time.unscaledTime;
                _steering = false;
            }

            if (!_pointerOnScene)
            {
                BoatController.SteerInput = Vector2.zero;
                return;
            }

            float toReference = 1080f / Screen.width;
            var delta = (position - _pointerStart) * toReference;
            if (!_steering && delta.magnitude > DragDeadZone) _steering = true;

            if (released)
            {
                BoatController.SteerInput = Vector2.zero;
                _pointerOnScene = false;
                if (!_steering && Time.unscaledTime - _pointerStartTime <= TapMaxDuration)
                    Cast(position);
                return;
            }

            BoatController.SteerInput = _steering && (held || pressed)
                ? Vector2.ClampMagnitude(delta.normalized * ((delta.magnitude - DragDeadZone) / DragFullSpeed), 1f)
                : Vector2.zero;
        }

        static bool PointerOverUi()
        {
            if (EventSystem.current == null) return false;
            return Input.touchCount > 0
                ? EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)
                : EventSystem.current.IsPointerOverGameObject();
        }

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
                if (pair.Value.subLabel != null)
                {
                    double perUnit = def.baseRate * Multipliers.ProducerRate(config, state, def.id);
                    pair.Value.subLabel.text = owned > 0
                        ? $"{Numbers.Format(perUnit * owned)}/s"
                        : $"{Numbers.Format(perUnit)}/s {GameTheme.PerUnitSuffix}";
                }
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

            _moneyText = UiKit.CreateText("Money", moneyPill.transform, 60, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
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

            var holdBar = UiKit.CreateCard("HoldBar", header.transform, new Color(0f, 0f, 0f, 0.12f), shadow: false);
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

            var (sellButton, sellLabel, sellRect) = UiKit.CreateFancyButton("SellAll", header.transform, SellGreen, 30);
            sellLabel.text = GameTheme.SellAllAction;
            sellRect.anchorMin = new Vector2(0.64f, 1f);
            sellRect.anchorMax = new Vector2(1f, 1f);
            sellRect.offsetMin = new Vector2(4f, -304f);
            sellRect.offsetMax = new Vector2(-22f, -222f);
            sellButton.onClick.AddListener(() =>
            {
                var boot = GameBootstrap.Instance;
                Economy.SellAll(boot.Config, boot.State, CurrentSellMultiplier(boot.Config));
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
            _catchBanner = UiKit.CreateText("Text", card.transform, 36, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.AddOutline(_catchBanner, 1.4f);
            UiKit.Stretch(_catchBanner.rectTransform, 16, 16, 0, 0);
            _catchBannerCard = card.gameObject;
            _catchBannerCard.SetActive(false);
        }

        void BuildPanels(Transform canvas)
        {
            var boot = GameBootstrap.Instance;

            // Onglet Bateau : producteurs et améliorations réunis (l'ancien double panneau).
            _boatPanel = BuildPanel(canvas, GameTheme.BoatTab, out var boatContent);
            AddSectionHeader(boatContent, GameTheme.ProducersSection);
            int rowIndex = 0;
            foreach (var def in boot.Config.producers)
            {
                string id = def.id;
                _producerRows[id] = CreateShopRow(boatContent,
                    () => Economy.TryBuyProducer(boot.Config, boot.State, id),
                    alternate: rowIndex++ % 2 == 1, withSubLabel: true);
            }
            AddSectionHeader(boatContent, GameTheme.UpgradesSection);
            rowIndex = 0;
            foreach (var def in boot.Config.upgrades)
            {
                string id = def.id;
                _upgradeRows[id] = CreateShopRow(boatContent,
                    () => Economy.TryBuyUpgrade(boot.Config, boot.State, id),
                    alternate: rowIndex++ % 2 == 1);
            }

            BuildMapPanel(canvas);
            BuildProfilePanel(canvas);
            BuildShopPanel(canvas);
        }

        static void AddSectionHeader(Transform parent, string title)
        {
            var text = UiKit.CreateText("Section", parent, 30, new Color(0.05f, 0.45f, 0.52f), TextAnchor.MiddleCenter, FontStyle.Bold);
            text.text = title;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
        }

        /// <summary>
        /// La carte : un disque par zone (du large sombre vers les eaux claires du
        /// départ), les îles nommées (verrouillées tant que la coque ne suit pas),
        /// et le bateau en direct. Monde → carte : haut = +x, droite = -z.
        /// </summary>
        void BuildMapPanel(Transform canvas)
        {
            var panel = UiKit.CreateCard("MapPanel", canvas, PanelBg);
            UiKit.AnchorBottom(panel.rectTransform, BottomBarHeight + PrestigeBandHeight + 6, PanelHeight, 16);

            AddPanelTitle(panel.transform, GameTheme.MapTitle);

            var sea = UiKit.CreateCard("Sea", panel.transform, new Color(0.03f, 0.09f, 0.17f), shadow: false);
            UiKit.AnchorVerticalSpan(sea.rectTransform, 90, 16, 14);

            _mapScale = 336f / MapWorldRange;

            var zoneColors = new[]
            {
                new Color(0.16f, 0.55f, 0.62f),
                new Color(0.1f, 0.38f, 0.5f),
                new Color(0.06f, 0.24f, 0.4f),
            };
            var boundaries = WorldMap.ZoneBoundaries;
            for (int i = boundaries.Count - 1; i >= 0; i--)
            {
                var circle = UiKit.CreateRect("Zone" + i, sea.transform).gameObject.AddComponent<Image>();
                circle.sprite = UiKit.Circle;
                circle.color = zoneColors[Mathf.Min(i, zoneColors.Length - 1)];
                circle.raycastTarget = false;
                circle.rectTransform.sizeDelta = Vector2.one * (boundaries[i] * _mapScale * 2f);
            }

            // Contours fins des frontières de zones : la carte se lit comme une carte marine.
            for (int i = 0; i < boundaries.Count; i++)
            {
                var ring = UiKit.CreateRect("Ring" + i, sea.transform).gameObject.AddComponent<Image>();
                ring.sprite = UiKit.Ring;
                ring.color = new Color(1f, 1f, 1f, 0.18f);
                ring.raycastTarget = false;
                ring.rectTransform.sizeDelta = Vector2.one * (boundaries[i] * _mapScale * 2f);
            }

            foreach (var island in WorldMap.AllIslands)
            {
                // Taille réelle de l'île, pour que la carte dise aussi « celle-ci est
                // plus grande » (plancher : une pastille trop petite serait illisible).
                float dotSize = Mathf.Max(24f, island.radius * _mapScale * 2f);

                var halo = UiKit.CreateRect("Halo", sea.transform).gameObject.AddComponent<Image>();
                halo.sprite = UiKit.Circle;
                halo.color = new Color(0.03f, 0.08f, 0.13f, 0.8f);
                halo.raycastTarget = false;
                halo.rectTransform.sizeDelta = Vector2.one * (dotSize + 12f);
                halo.rectTransform.anchoredPosition = MapPoint(island.position);

                var dot = UiKit.CreateRect("Island", sea.transform).gameObject.AddComponent<Image>();
                dot.sprite = UiKit.Circle;
                dot.color = new Color(0.87f, 0.75f, 0.52f);
                dot.raycastTarget = false;
                dot.rectTransform.sizeDelta = Vector2.one * dotSize;
                dot.rectTransform.anchoredPosition = MapPoint(island.position);

                // Le nom dans une pastille sombre — lisible sur n'importe quel fond.
                var pill = UiKit.CreateCard("Pill", sea.transform, new Color(0.03f, 0.08f, 0.13f, 0.85f), shadow: false);
                pill.raycastTarget = false;
                pill.rectTransform.sizeDelta = new Vector2(232f, 74f);
                pill.rectTransform.anchoredPosition = MapPoint(island.position) + new Vector2(0f, -64f);
                var label = UiKit.CreateText("Name", pill.transform, 24, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                UiKit.Stretch(label.rectTransform, 8, 8, 4, 4);
                _mapIslands.Add(new MapIslandMarker { island = island, label = label });
            }

            var markerBorder = UiKit.CreateRect("Boat", sea.transform).gameObject.AddComponent<Image>();
            markerBorder.sprite = UiKit.Circle;
            markerBorder.color = new Color(0.03f, 0.08f, 0.13f);
            markerBorder.raycastTarget = false;
            markerBorder.rectTransform.sizeDelta = new Vector2(32f, 32f);
            var markerFill = UiKit.CreateRect("Fill", markerBorder.transform).gameObject.AddComponent<Image>();
            markerFill.sprite = UiKit.Circle;
            markerFill.color = Color.white;
            markerFill.raycastTarget = false;
            markerFill.rectTransform.sizeDelta = new Vector2(20f, 20f);
            // Petit bec de cap : la carte montre où le bateau POINTE, pas juste où il est.
            var tick = UiKit.CreateRect("Tick", markerBorder.transform).gameObject.AddComponent<Image>();
            tick.sprite = UiKit.Rounded;
            tick.type = Image.Type.Sliced;
            tick.color = Color.white;
            tick.raycastTarget = false;
            tick.rectTransform.anchoredPosition = new Vector2(0f, 20f);
            tick.rectTransform.sizeDelta = new Vector2(9f, 18f);
            _boatMarker = markerBorder.rectTransform;

            panel.gameObject.SetActive(false);
            _mapPanel = panel.gameObject;
        }

        Vector2 MapPoint(Vector3 world) => new Vector2(-world.z * _mapScale, world.x * _mapScale);

        void RefreshMap(BalanceConfig config, GameState state)
        {
            if (BoatController.Instance != null)
            {
                _boatMarker.anchoredPosition = MapPoint(BoatController.Instance.Root.position);
                _boatMarker.localEulerAngles = new Vector3(0f, 0f, -BoatController.Instance.Root.eulerAngles.y);
            }

            int maxZone = Catching.MaxNavigableZone(config, state);
            foreach (var marker in _mapIslands)
            {
                bool reachable = marker.island.zone <= maxZone;
                string name = GameTheme.Island(marker.island.id);
                marker.label.text = reachable
                    ? name
                    : $"{name}\n{string.Format(GameTheme.ZoneLockedFormat, marker.island.zone)}";
                marker.label.color = reachable ? Color.white : new Color(1f, 1f, 1f, 0.55f);
            }
        }

        void BuildProfilePanel(Transform canvas)
        {
            var boot = GameBootstrap.Instance;
            _profilePanel = BuildPanel(canvas, GameTheme.ProfileTitle, out var content);

            AddSectionHeader(content, GameTheme.StatsSection);
            _statLifetime = AddStatRow(content, GameTheme.StatLifetime);
            _statDiscovered = AddStatRow(content, GameTheme.StatDiscovered);
            _statPrestige = AddStatRow(content, GameTheme.StatPrestige);
            _statZone = AddStatRow(content, GameTheme.StatZone);

            AddSectionHeader(content, GameTheme.CollectionSection);
            foreach (var species in boot.Config.species)
            {
                var card = UiKit.CreateCard("Dex", content, RowBg, shadow: false);
                card.gameObject.AddComponent<LayoutElement>().preferredHeight = 84;
                var name = UiKit.CreateText("Name", card.transform, 34, TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
                UiKit.Stretch(name.rectTransform, 30, 260, 0, 0);
                var bonus = UiKit.CreateText("Bonus", card.transform, 30, TextDim, TextAnchor.MiddleRight);
                UiKit.Stretch(bonus.rectTransform, 30, 30, 0, 0);
                _dexRows.Add(new DexRow { id = species.id, def = species, name = name, bonus = bonus });
            }
        }

        /// <summary>Ligne de statistique : libellé à gauche, valeur à droite — jamais tronquée.</summary>
        Text AddStatRow(Transform parent, string title)
        {
            var card = UiKit.CreateCard("Stat", parent, RowBg, shadow: false);
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
            var label = UiKit.CreateText("Label", card.transform, 30, TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.text = title;
            UiKit.Stretch(label.rectTransform, 30, 280, 0, 0);
            var value = UiKit.CreateText("Value", card.transform, 30, TextDim, TextAnchor.MiddleRight);
            UiKit.Stretch(value.rectTransform, 30, 30, 0, 0);
            return value;
        }

        void RefreshProfile(BalanceConfig config, GameState state)
        {
            _statLifetime.text = $"{Numbers.Format(state.lifetimeMoney)} {GameTheme.MoneySuffix}";
            _statDiscovered.text = $"{state.discoveredSpecies.Count}/{config.species.Count}";
            _statPrestige.text = state.prestigePoints.ToString();
            _statZone.text = Catching.DepthLevel(config, state).ToString();

            foreach (var row in _dexRows)
            {
                bool known = state.discoveredSpecies.Contains(row.id);
                row.name.text = known ? GameTheme.Species(row.id) : GameTheme.UndiscoveredSpecies;
                row.name.color = known ? TextMain : TextDim;
                row.bonus.text = known ? $"+{(row.def.discoveryBonus - 1) * 100:0.#} %" : "";
            }
        }

        void BuildShopPanel(Transform canvas)
        {
            var panel = UiKit.CreateCard("ShopPanel", canvas, PanelBg);
            UiKit.AnchorBottom(panel.rectTransform, BottomBarHeight + PrestigeBandHeight + 6, PanelHeight, 16);

            AddPanelTitle(panel.transform, GameTheme.ShopTab);

            var message = UiKit.CreateText("Soon", panel.transform, 34, TextDim, TextAnchor.MiddleCenter);
            message.text = GameTheme.ShopComingSoon;
            UiKit.AnchorVerticalSpan(message.rectTransform, 90, 16, 40);

            panel.gameObject.SetActive(false);
            _shopPanel = panel.gameObject;
        }

        GameObject BuildPanel(Transform canvas, string title, out RectTransform content)
        {
            var panel = UiKit.CreateCard("Panel", canvas, PanelBg);
            UiKit.AnchorBottom(panel.rectTransform, BottomBarHeight + PrestigeBandHeight + 6, PanelHeight, 16);

            AddPanelTitle(panel.transform, title);

            content = UiKit.CreateScrollList("List", panel.transform, new Color(0f, 0f, 0f, 0.06f));
            UiKit.AnchorVerticalSpan((RectTransform)content.parent, 90, 16, 14);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        /// <summary>Bandeau de titre commun à tous les panneaux : pilule bleue, texte blanc.</summary>
        static void AddPanelTitle(Transform panel, string title)
        {
            var band = UiKit.CreateCard("TitleBand", panel, TabBlue, shadow: false);
            UiKit.AnchorTop(band.rectTransform, 10, 68, 220);
            var titleText = UiKit.CreateText("Title", band.transform, 38, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.AddOutline(titleText, 1.4f);
            titleText.text = title;
            UiKit.Stretch(titleText.rectTransform);
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
                CloseAllPanels();
            });
            _prestigeButton = button;
            _prestigeLabel = label;
            button.gameObject.SetActive(false);
        }

        void BuildBottomBar(Transform canvas)
        {
            var bar = UiKit.CreateCard("BottomBar", canvas, CardBg);
            UiKit.AnchorBottom(bar.rectTransform, 12, BottomBarHeight, 14);

            // 5 onglets : Bateau · Carte · PÊCHER · Profil · Boutique.
            AddTab(bar.transform, "BoatTab", GameTheme.BoatTab, UiKit.Icon("crew"), 0.010f, 0.196f, () => TogglePanel(_boatPanel));
            AddTab(bar.transform, "MapTab", GameTheme.MapTab, null, 0.206f, 0.392f, () => TogglePanel(_mapPanel));

            var (castButton, castLabel, castRect) = UiKit.CreateFancyButton("Cast", bar.transform, CastGreen, 32, UiKit.Icon("fish"));
            castLabel.text = GameTheme.CastAction;
            SetBarSlot(castRect, 0.402f, 0.598f);
            castButton.onClick.AddListener(() => Cast(new Vector2(Screen.width * 0.62f, Screen.height * 0.45f)));

            AddTab(bar.transform, "ProfileTab", GameTheme.ProfileTab, UiKit.Icon("star"), 0.608f, 0.794f, () => TogglePanel(_profilePanel));
            AddTab(bar.transform, "ShopTab", GameTheme.ShopTab, UiKit.Icon("coin"), 0.804f, 0.990f, () => TogglePanel(_shopPanel));
        }

        void AddTab(Transform bar, string name, string label, Sprite icon, float xMin, float xMax, System.Action onClick)
        {
            var (button, text, rect) = UiKit.CreateFancyButton(name, bar, TabBlue, 26, icon);
            text.text = label;
            SetBarSlot(rect, xMin, xMax);
            button.onClick.AddListener(() => onClick());
        }

        static void SetBarSlot(RectTransform rt, float xMin, float xMax)
        {
            rt.anchorMin = new Vector2(xMin, 0f);
            rt.anchorMax = new Vector2(xMax, 1f);
            rt.offsetMin = new Vector2(0f, 18f);
            rt.offsetMax = new Vector2(0f, -18f);
        }

        float _offlineTextUntil;

        void ShowOfflineSummary()
        {
            var offline = GameBootstrap.Instance.LastOffline;
            if (offline == null || offline.simulatedSeconds < 60 || offline.stockGained <= 0) return;
            string holdNote = offline.holdFull ? $" — {GameTheme.OfflineHoldFull}" : "";
            _offlineText.text =
                $"{GameTheme.OfflinePrefix} : +{Numbers.Format(offline.stockGained)} {GameTheme.FishUnit}{holdNote}";
            _offlineText.gameObject.SetActive(true);
            _offlineTextUntil = Time.time + 8f; // il restait affiché pour toujours (retour playtest)
        }

        ShopRow CreateShopRow(Transform parent, System.Action onBuy, bool alternate = false, bool withSubLabel = false)
        {
            var card = UiKit.CreateCard("Row", parent,
                alternate ? new Color(1f, 1f, 1f, 0.045f) : RowBg, shadow: false);
            card.gameObject.AddComponent<LayoutElement>().preferredHeight = 122;

            var label = UiKit.CreateText("Name", card.transform, 36, TextMain, TextAnchor.MiddleLeft, FontStyle.Bold);
            var labelRt = label.rectTransform;
            labelRt.anchorMin = new Vector2(0f, withSubLabel ? 0.42f : 0f);
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = new Vector2(30f, 0f);
            labelRt.offsetMax = new Vector2(-330f, withSubLabel ? -6f : 0f);

            Text subLabel = null;
            if (withSubLabel)
            {
                subLabel = UiKit.CreateText("Sub", card.transform, 26, TextDim, TextAnchor.MiddleLeft);
                var subRt = subLabel.rectTransform;
                subRt.anchorMin = Vector2.zero;
                subRt.anchorMax = new Vector2(1f, 0.42f);
                subRt.offsetMin = new Vector2(30f, 8f);
                subRt.offsetMax = new Vector2(-330f, 0f);
            }

            var (button, buttonLabel, buttonRect) = UiKit.CreateFancyButton("Buy", card.transform, BuyOrange, 34);
            buttonRect.anchorMin = new Vector2(1f, 0.5f);
            buttonRect.anchorMax = new Vector2(1f, 0.5f);
            buttonRect.pivot = new Vector2(1f, 0.5f);
            buttonRect.anchoredPosition = new Vector2(-16f, 0f);
            buttonRect.sizeDelta = new Vector2(300f, 94f);
            button.onClick.AddListener(() => onBuy());

            return new ShopRow { root = card.gameObject, label = label, subLabel = subLabel, button = button, buttonLabel = buttonLabel };
        }
    }
}
