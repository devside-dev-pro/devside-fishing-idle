using System.Collections;
using System.Collections.Generic;
using Devside.FishingIdle.Core;
using UnityEngine;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Le diorama 3D : le bateau pirate (modèles Quaternius) vu quasi du dessus en portrait,
    /// qui MATÉRIALISE l'état du jeu — chaque pêcheur acheté apparaît sur le pont et lance
    /// sa ligne, les barils s'empilent avec la cale, le navire est remplacé par un plus
    /// grand au niveau 5 d'extension de cale, l'eau (shader stylisé) fonce avec la
    /// profondeur, des poissons nagent autour. Chaque modèle a un fallback primitive si
    /// l'asset manque (codage défensif). Ajouté automatiquement par GameUi.
    /// La coque est accrochée sous BoatController.Root (position + cap dans l'archipel,
    /// îles gérées par WorldMap) ; caméra, océan et écume suivent le bateau (FollowBoat).
    /// </summary>
    public class BoatView : MonoBehaviour
    {
        public static BoatView Instance { get; private set; }

        static readonly Color SkyColor = new Color(0.53f, 0.78f, 0.92f);
        static readonly Color WaterShallow = new Color(0.16f, 0.55f, 0.62f);
        static readonly Color WaterDeep = new Color(0.05f, 0.15f, 0.35f);
        static readonly Color RodColor = new Color(0.25f, 0.18f, 0.12f);
        static readonly Color LineColor = new Color(0.9f, 0.95f, 1f);
        static readonly Color BobberRed = new Color(0.85f, 0.2f, 0.15f);
        static readonly Color FallbackHull = new Color(0.4f, 0.25f, 0.15f);
        static readonly Color FallbackDeck = new Color(0.72f, 0.5f, 0.3f);
        static readonly Color FallbackProp = new Color(0.72f, 0.55f, 0.28f);
        static readonly Color FallbackFish = new Color(0.16f, 0.32f, 0.42f);

        // Paliers de navire : on COMMENCE sur une petite barque (retour playtest),
        // navire moyen au niveau 3 de cale, grand navire au niveau 8 — l'évolution
        // se fait petit à petit. Modèles custom (Art/Custom/Ships) prioritaires,
        // packs Quaternius en repli.
        static readonly int[] ShipTierHoldLevels = { 0, 3, 8 };
        static readonly string[] ShipTierCustom = { "barque", "chalutier_moyen", "chalutier_grand" };
        static readonly string[] ShipTierFallback = { ArtLibrary.ShipSmall, ArtLibrary.ShipSmall, ArtLibrary.ShipLarge };
        static readonly float[] ShipTierLength = { 3.1f, 4.6f, 6f };

        // Enfoncement dans l'eau par palier : une petite barque basse s'enfonce à
        // peine, sinon l'eau passe à travers son plancher (retour playtest).
        static readonly float[] ShipTierSink = { 0.08f, 0.22f, 0.28f };

        // Emplacements d'équipage en coordonnées normalisées du navire
        // (x : fraction de la longueur — la verticale de l'écran ; z : fraction de la largeur).
        static readonly Vector2[] T1Slots =
        {
            new Vector2(-0.3f, 0.4f), new Vector2(-0.12f, 0.4f), new Vector2(0.06f, 0.4f),
            new Vector2(0.24f, 0.4f), new Vector2(-0.3f, -0.4f), new Vector2(-0.12f, -0.4f),
        };
        static readonly Vector2[] T2Slots =
        {
            new Vector2(0.06f, -0.4f), new Vector2(0.24f, -0.4f), new Vector2(0.4f, 0f),
        };
        static readonly Vector2[] T3Slots =
        {
            new Vector2(-0.44f, 0.28f), new Vector2(-0.44f, -0.28f),
        };
        static readonly string[] TierProducerIds = { "fisherman_t1", "fisherman_t2", "fisherman_t3" };
        static readonly string[] TierModelPaths = { ArtLibrary.CrewT1, ArtLibrary.CrewT2, ArtLibrary.CrewT3 };

        // Fragments de noms des personnages custom par tier (fichiers de
        // Art/Custom/Characters nommés librement, ex. char_marin_pecheur_v2).
        static readonly string[][] TierCustomFragments =
        {
            new[] { "mousse", "marin" },
            new[] { "pecheur_pro" },
            new[] { "vieux" },
        };

        class CrewVisual
        {
            public Transform root;
            public Transform rod;
            public LineRenderer line;
            public Transform bobber;
        }

        class AmbientFish
        {
            public Transform root;
            public float speed;

            /// <summary>Cap en radians dans le plan XZ (le poisson va où il regarde).</summary>
            public float heading;

            public float wanderPhase;
            public float divePhase;
            public float baseDepth;
        }

        Camera _camera;
        Transform _boat;
        Renderer _water;
        Material _waterMaterial;
        bool _stylizedWater;

        GameObject _ship;
        int _shipTier = -1;
        float _shipLength = 4.5f;
        float _shipWidth = 2f;

        /// <summary>Hauteur du pont au centre du navire — référence de la « bande de pont ».</summary>
        float _deckLevel = 0.74f;

        readonly List<CrewVisual>[] _crew =
        {
            new List<CrewVisual>(), new List<CrewVisual>(), new List<CrewVisual>(),
        };
        Vector3[][] _slots;
        readonly List<GameObject> _crates = new List<GameObject>();
        GameObject _cuttingStation;
        GameObject _filletStation;
        readonly List<AmbientFish> _ambientFish = new List<AmbientFish>();

        readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();
        static Shader _litShader;

        static Shader LitShader
        {
            get
            {
                if (_litShader == null)
                {
                    _litShader = Shader.Find("Universal Render Pipeline/Lit");
                    if (_litShader == null) _litShader = Shader.Find("Standard");
                }
                return _litShader;
            }
        }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            SetupCameraAndLight();
            BuildSea();
            BuildAmbientFish();
        }

        void Update()
        {
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.State == null) return;
            var config = boot.Config;
            var state = boot.State;
            float t = Time.time;

            EnsureShip(state);

            // Ceinture-bretelles : la coque doit vivre sous le BoatRoot — si l'attache
            // a raté (ordre d'initialisation), le bateau resterait à l'origine pendant
            // que la caméra, l'écume et la zone naviguent sans lui.
            if (BoatController.Instance != null && _boat != null
                && _boat.parent != BoatController.Instance.Root)
                _boat.SetParent(BoatController.Instance.Root, false);

            for (int tier = 0; tier < 3; tier++)
                SyncCrew(_crew[tier], _slots[tier], state.ProducerCount(TierProducerIds[tier]), tier);

            if (_cuttingStation != null) _cuttingStation.SetActive(state.ProducerCount("cutting_station") > 0);
            if (_filletStation != null) _filletStation.SetActive(state.ProducerCount("fillet_station") > 0);

            // Les barils matérialisent le remplissage de la cale.
            double capacity = Multipliers.HoldCapacity(config, state);
            float fill = capacity <= 0 ? 0f : Mathf.Clamp01((float)(state.TotalFishStock / capacity));
            int visible = Mathf.CeilToInt(fill * _crates.Count);
            for (int i = 0; i < _crates.Count; i++)
                if (_crates[i] != null && _crates[i].activeSelf != (i < visible))
                    _crates[i].SetActive(i < visible);

            // Roulis léger, et croissance douce avec les extensions de cale
            // (le vrai saut visuel est le changement de navire au niveau 5).
            int holdLevel = state.UpgradeLevel("cargo_hold");
            float scale = Mathf.Min(1.2f, 1f + 0.02f * holdLevel);
            _boat.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 1.2f, 0f, Mathf.Sin(t * 0.6f) * 1.8f);
            _boat.localPosition = new Vector3(0f, 0.04f * Mathf.Sin(t * 0.8f), 0f);
            _boat.localScale = Vector3.one * scale;

            // L'eau fonce avec la profondeur ; l'anneau d'écume suit la taille du navire.
            float depth01 = Mathf.Clamp01(Catching.DepthLevel(config, state) / 3f);
            if (_stylizedWater)
            {
                _waterMaterial.SetFloat("_DepthBlend", depth01);
                _waterMaterial.SetFloat("_FoamRadius", (_shipLength * 0.52f + 0.25f) * scale);
            }
            else
            {
                _waterMaterial.color = Color.Lerp(WaterShallow, WaterDeep, depth01);
            }

            for (int tier = 0; tier < 3; tier++)
                AnimateCrew(_crew[tier], t + tier * 2.7f);
            AnimateAmbientFish(t);
        }

        /// <summary>Cadre au mouillage : le bateau n'occupe qu'un gros quart de l'écran.</summary>
        const float IdleOrthoSize = 7.4f;

        /// <summary>Dézoom supplémentaire en navigation, pour voir où l'on va.</summary>
        const float SailOrthoBonus = 2.4f;

        const float ZoomSpeed = 3.5f;

        /// <summary>
        /// Suivi du bateau, appelé par BoatController après le déplacement : la caméra
        /// reste cadrée sur la coque et dézoome pendant la navigation, le plan d'eau
        /// glisse sous elle (le bruit du shader est en coordonnées monde, donc l'eau
        /// « défile » vraiment), et l'anneau d'écume reçoit position + cap.
        /// </summary>
        public void FollowBoat(Transform root)
        {
            if (_camera != null)
            {
                _camera.transform.position = root.position - _camera.transform.forward * 22f + _camera.transform.up * 0.7f;
                float throttle = Mathf.Min(1f, BoatController.SteerInput.magnitude);
                _camera.orthographicSize = Mathf.MoveTowards(
                    _camera.orthographicSize, IdleOrthoSize + SailOrthoBonus * throttle, ZoomSpeed * Time.deltaTime);
            }
            if (_water != null)
                _water.transform.position = new Vector3(root.position.x, 0f, root.position.z);
            if (_stylizedWater && _waterMaterial != null)
            {
                var forward = new Vector2(root.right.x, root.right.z);
                forward = forward.sqrMagnitude < 0.001f ? Vector2.right : forward.normalized;
                _waterMaterial.SetVector("_BoatPos",
                    new Vector4(root.position.x, root.position.z, forward.x, forward.y));
            }
        }

        /// <summary>Poisson qui jaillit + chiffre qui vole, à l'endroit tapé (coordonnées écran).</summary>
        public void PlayCatchEffect(Vector2 screenPosition, CatchResult result)
        {
            if (_camera == null || result == null || result.amount <= 0) return;

            Vector3 splash = RaycastWater(screenPosition);
            StartCoroutine(FishJump(splash, result.speciesId));
            StartCoroutine(FloatingText(splash + Vector3.up * 0.6f, "+" + Numbers.Format(result.amount), Color.white, 42));
            if (result.newDiscovery)
                StartCoroutine(FloatingText(splash + Vector3.up * 1.3f,
                    $"{GameTheme.Species(result.speciesId)} — {GameTheme.NewDiscovery}",
                    new Color(1f, 0.85f, 0.3f), 52));
        }

        // ---------- Construction du monde ----------

        void SetupCameraAndLight()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                _camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                _camera.tag = "MainCamera";
            }
            _camera.orthographic = true;
            _camera.orthographicSize = IdleOrthoSize;
            _camera.transform.rotation = Quaternion.Euler(70f, 90f, 0f);
            _camera.transform.position = -_camera.transform.forward * 22f + _camera.transform.up * 0.7f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = SkyColor;

            // Réutilise la Directional Light de la scène : en ajouter une deuxième surexpose.
            Light sun = null;
            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type != LightType.Directional) continue;
                if (sun == null) sun = light;
                else light.gameObject.SetActive(false);
            }
            if (sun == null) sun = new GameObject("Sun", typeof(Light)).GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.0f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.42f, 0.47f, 0.54f);
        }

        void BuildSea()
        {
            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            water.transform.localScale = new Vector3(8f, 1f, 8f);
            // Le plan suit le bateau chaque frame : pas de collider (les raycasts de
            // pont ne doivent voir que la coque, et un collider mobile coûte cher).
            Destroy(water.GetComponent<Collider>());
            _water = water.GetComponent<Renderer>();

            var stylized = Shader.Find("Devside/StylizedWater");
            _stylizedWater = stylized != null;
            _waterMaterial = _stylizedWater
                ? new Material(stylized)
                : new Material(LitShader) { color = WaterShallow };
            if (_stylizedWater)
            {
                _waterMaterial.SetColor("_ColorShallow", WaterShallow);
                _waterMaterial.SetColor("_ColorDeep", WaterDeep);
            }
            _water.material = _waterMaterial;
        }

        void EnsureShip(GameState state)
        {
            int holdLevel = state.UpgradeLevel("cargo_hold");
            int tier = holdLevel >= ShipTierHoldLevels[2] ? 2 : holdLevel >= ShipTierHoldLevels[1] ? 1 : 0;
            if (_boat != null && tier == _shipTier) return;
            _shipTier = tier;
            RebuildShip(tier);
        }

        void RebuildShip(int tier)
        {
            if (_boat == null) _boat = new GameObject("Boat").transform;
            // Mesures de bounds et raycasts de pont supposent une coque à l'origine en
            // rotation identité : on la détache du BoatRoot (qui peut être loin et
            // orienté) le temps de la reconstruction, puis on la raccroche à la fin.
            _boat.SetParent(null, false);
            _boat.localRotation = Quaternion.identity;
            _boat.localScale = Vector3.one;
            _boat.localPosition = Vector3.zero;

            for (int i = _boat.childCount - 1; i >= 0; i--) Destroy(_boat.GetChild(i).gameObject);
            // Les bouchons vivent HORS de la hiérarchie du bateau (positionnés en
            // monde) : sans destruction explicite ils restent figés sur l'eau au
            // changement de navire (bug vécu).
            foreach (var list in _crew)
            {
                foreach (var visual in list)
                    if (visual.bobber != null)
                        Destroy(visual.bobber.gameObject);
                list.Clear();
            }
            _crates.Clear();

            float targetLength = ShipTierLength[tier];
            _ship = ArtLibrary.SpawnFirst(_boat,
                ArtLibrary.CustomShip(ShipTierCustom[tier]), ShipTierFallback[tier]);
            if (_ship != null)
            {
                var bounds = ArtLibrary.MeasureBounds(_ship);
                if (bounds.size.z > bounds.size.x)
                    _ship.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                ArtLibrary.NormalizeToSize(_ship, targetLength, ShipTierSink[tier]);
                ArtLibrary.AddColliders(_ship);
                Physics.SyncTransforms();
                bounds = ArtLibrary.MeasureBounds(_ship);
                _shipLength = bounds.size.x;
                _shipWidth = Mathf.Max(1.2f, bounds.size.z);
            }
            else
            {
                BuildFallbackShip();
                _shipLength = 4.5f;
                _shipWidth = 2.2f;
            }

            // Référence de hauteur du pont : le PLUS BAS de plusieurs échantillons —
            // au centre seul, un navire à cabine donnait le TOIT comme référence et
            // tout (table, équipage) se posait dans le ciel (bug vécu). Les surfaces
            // trop au-dessus (toits) ou trop en dessous (ancre) sont hors bande.
            _deckLevel = float.MaxValue;
            float[] sampleX = { -0.32f, -0.15f, 0f, 0.22f };
            foreach (float fx in sampleX)
            {
                float? y = RawDeckHeight(fx * _shipLength, 0f);
                if (y.HasValue) _deckLevel = Mathf.Min(_deckLevel, y.Value);
            }
            float? side = RawDeckHeight(0f, 0.28f * _shipWidth);
            if (side.HasValue) _deckLevel = Mathf.Min(_deckLevel, side.Value);
            if (_deckLevel >= float.MaxValue) _deckLevel = 0.74f;

            _slots = new[] { ResolveSlots(T1Slots), ResolveSlots(T2Slots), ResolveSlots(T3Slots) };
            BuildDeckProps();

            if (BoatController.Instance != null)
                _boat.SetParent(BoatController.Instance.Root, false);
        }

        void BuildFallbackShip()
        {
            Block("Waterline", _boat, new Vector3(0f, 0.12f, 0f), new Vector3(4.7f, 0.24f, 2.4f), new Color(0.2f, 0.12f, 0.08f));
            Block("Hull", _boat, new Vector3(0f, 0.4f, 0f), new Vector3(4.5f, 0.56f, 2.2f), FallbackHull);
            Block("Deck", _boat, new Vector3(-0.1f, 0.72f, 0f), new Vector3(4.1f, 0.08f, 1.8f), FallbackDeck);
            var bow = Block("Bow", _boat, new Vector3(2.25f, 0.4f, 0f), new Vector3(1.56f, 0.56f, 1.56f), FallbackHull);
            bow.localRotation = Quaternion.Euler(0f, 45f, 0f);
            _ship = null;
        }

        Vector3[] ResolveSlots(Vector2[] fractions)
        {
            var result = new Vector3[fractions.Length];
            for (int i = 0; i < fractions.Length; i++)
                result[i] = ResolveDeckPoint(fractions[i].x * _shipLength, fractions[i].y * _shipWidth);
            return result;
        }

        /// <summary>
        /// Point de pont SÛR : les bounds du navire dépassent le pont réel (ancre sur
        /// la coque, beaupré...), donc un point calculé en fractions des bounds peut
        /// tomber dans l'eau OU sur une protubérance (bug vécu deux fois : l'équipage
        /// à côté du bateau, puis debout sur l'ancre « dans le vide »). On ramène le
        /// point vers le centre par paliers jusqu'à toucher une surface DANS LA BANDE
        /// DU PONT (± 0.45 autour de la hauteur du pont au centre).
        /// </summary>
        Vector3 ResolveDeckPoint(float x, float z)
        {
            for (int step = 0; step < 8; step++)
            {
                float k = 1f - step * 0.11f;
                float? y = RawDeckHeight(x * k, z * k);
                if (y.HasValue && Mathf.Abs(y.Value - _deckLevel) <= 0.3f)
                    return new Vector3(x * k, y.Value, z * k);
            }
            return new Vector3(x * 0.3f, _deckLevel, z * 0.3f);
        }

        /// <summary>
        /// Hauteur de la surface la plus basse au-dessus de la flottaison à (x, z)
        /// local (raycast sur les colliders du navire ; voiles et mâts, plus hauts,
        /// ignorés). Null si rien sous le point. Sans filtre de bande : c'est
        /// ResolveDeckPoint qui juge si c'est vraiment du pont.
        /// </summary>
        float? RawDeckHeight(float x, float z)
        {
            if (_ship == null) return 0.74f; // navire de secours : pont plat connu
            var hits = Physics.RaycastAll(new Vector3(x, 8f, z), Vector3.down, 16f);
            float best = float.MaxValue;
            foreach (var hit in hits)
            {
                if (!hit.transform.IsChildOf(_ship.transform)) continue;
                if (hit.point.y < 0.05f) continue;
                if (hit.point.y < best) best = hit.point.y;
            }
            if (best >= float.MaxValue) return null;
            return best + 0.02f;
        }

        void BuildDeckProps()
        {
            // Le capitaine (le joueur !) est toujours à bord, près de la barre.
            SpawnOnDeck(new[] { ArtLibrary.Captain }, -0.28f, 0.05f, 0.72f, normalizeHeight: true,
                characterFragments: new[] { "capitaine" });

            // Postes de transformation : version custom générée si déposée, pack sinon.
            _cuttingStation = SpawnOnDeck(
                new[] { ArtLibrary.CustomProp("cutting_station"), ArtLibrary.CuttingStation }, -0.06f, 0.16f, 0.9f);
            _filletStation = SpawnOnDeck(
                new[] { ArtLibrary.CustomProp("fillet_station"), ArtLibrary.FilletStation }, -0.06f, -0.22f, 0.5f);

            // Deux rangées de barils à l'arrière : la jauge physique de la cale
            // (le baril custom déborde de poissons — parfait pour une jauge de stock).
            for (int i = 0; i < 6; i++)
            {
                float fx = -0.42f + i % 2 * 0.055f;
                float fz = -0.2f + i % 3 * 0.2f;
                var barrel = SpawnOnDeck(
                    new[] { ArtLibrary.CustomProp("fish_barrel"), ArtLibrary.Barrel }, fx, fz, 0.34f);
                if (barrel != null && i >= 3)
                    barrel.transform.localPosition += Vector3.up * 0.3f;
                _crates.Add(barrel);
            }
        }

        GameObject SpawnOnDeck(string[] paths, float fx, float fz, float targetSize,
            bool normalizeHeight = false, string[] characterFragments = null)
        {
            var holder = new GameObject("Prop").transform;
            holder.SetParent(_boat, false);
            holder.localPosition = ResolveDeckPoint(fx * _shipLength, fz * _shipWidth);

            var model = characterFragments != null
                ? ArtLibrary.SpawnCustomCharacter(holder, characterFragments)
                : null;
            if (model == null) model = ArtLibrary.SpawnFirst(holder, paths);
            if (model == null)
            {
                Block("Fallback", holder, Vector3.up * (targetSize * 0.5f), Vector3.one * targetSize, FallbackProp);
            }
            else if (normalizeHeight)
            {
                ArtLibrary.NormalizeToHeight(model, targetSize);
            }
            else
            {
                ArtLibrary.NormalizeToSize(model, targetSize);
            }
            return holder.gameObject;
        }

        static readonly string[] AmbientSmallSpecies = { "sardine", "mackerel", "sea_bass" };

        /// <summary>Distance au bateau au-delà de laquelle un poisson est relâché ailleurs.</summary>
        const float FishRecycleRadius = 17f;

        /// <summary>Distance d'apparition : hors cadre, pour que le tour de passe-passe ne se voie pas.</summary>
        const float FishSpawnRadius = 13f;

        void BuildAmbientFish()
        {
            for (int i = 0; i < 6; i++)
                AddAmbientFish(AmbientSmallSpecies[i % AmbientSmallSpecies.Length],
                    0.5f, 1.15f + i * 0.12f, -0.2f - (i % 3) * 0.06f);
            AddAmbientFish("abyssal_shark", 1.1f, 1.6f, -0.3f);
            AddAmbientFish("moonfish", 0.95f, 0.95f, -0.26f);
            AddAmbientFish("leviathan", 2.3f, 0.75f, -0.42f);
        }

        void AddAmbientFish(string speciesId, float size, float speed, float baseDepth)
        {
            var root = new GameObject("AmbientFish").transform;
            var model = SpawnFishModel(speciesId, size, root);
            if (model == null)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                body.SetParent(root, false);
                body.localScale = new Vector3(size * 0.4f, size * 0.16f, size);
                body.GetComponent<Renderer>().material = Mat(FallbackFish);
                Destroy(body.GetComponent<Collider>());
            }
            var fish = new AmbientFish
            {
                root = root,
                speed = speed,
                baseDepth = baseDepth,
                wanderPhase = Random.value * 12f,
                divePhase = Random.value * 12f,
            };
            ReleaseAmbientFish(fish, _boat != null ? _boat.position : Vector3.zero, spread: true);
            _ambientFish.Add(fish);
        }

        /// <summary>
        /// (Re)lâche un poisson autour du joueur : au premier peuplement il peut naître
        /// n'importe où dans le champ, ensuite toujours au bord et cap tourné vers la
        /// zone de jeu — il la traverse au lieu de s'en éloigner aussitôt.
        /// </summary>
        void ReleaseAmbientFish(AmbientFish fish, Vector3 center, bool spread = false)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float distance = spread ? Random.Range(3f, FishSpawnRadius) : FishSpawnRadius;
            fish.root.position = new Vector3(
                center.x + Mathf.Cos(angle) * distance,
                fish.baseDepth,
                center.z + Mathf.Sin(angle) * distance);
            fish.heading = angle + Mathf.PI + Random.Range(-0.9f, 0.9f);
        }

        /// <summary>
        /// Modèle d'un poisson : l'espèce custom générée (Meshy — créée de profil, nez
        /// en +x, on la tourne vers +z, la convention du jeu) depuis
        /// Resources/Art/Custom/Fish ; null si elle n'est pas déposée (fallback
        /// primitive à l'appelant — le pack poissons Quaternius a été retiré du repo).
        /// </summary>
        GameObject SpawnFishModel(string speciesId, float size, Transform parent)
        {
            if (string.IsNullOrEmpty(speciesId)) return null;
            var model = ArtLibrary.SpawnQuiet(ArtLibrary.CustomFish(speciesId));
            if (model == null) return null;
            model.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            ArtLibrary.NormalizeToSize(model, size);
            model.transform.SetParent(parent, false);
            return model;
        }

        // ---------- Équipage ----------

        void SyncCrew(List<CrewVisual> crew, Vector3[] slots, int owned, int tier)
        {
            int wanted = Mathf.Min(owned, slots.Length);
            while (crew.Count < wanted)
                crew.Add(BuildCrewMember(slots[crew.Count], tier));
            for (int i = 0; i < crew.Count; i++)
            {
                bool active = i < wanted;
                if (crew[i].root.gameObject.activeSelf != active)
                {
                    crew[i].root.gameObject.SetActive(active);
                    crew[i].bobber.gameObject.SetActive(active);
                }
            }
        }

        CrewVisual BuildCrewMember(Vector3 localPosition, int tier)
        {
            var root = new GameObject("Crew").transform;
            root.SetParent(_boat, false);
            root.localPosition = localPosition;
            float yaw = Mathf.Abs(localPosition.z) < 0.15f * _shipWidth
                ? 90f
                : (localPosition.z >= 0f ? 0f : 180f);
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);

            // Normalisé hors hiérarchie (le bateau peut être en plein roulis), puis re-parenté.
            // Personnage custom du tier si déposé, modèle Quaternius sinon.
            var model = ArtLibrary.SpawnCustomCharacter(null, TierCustomFragments[tier]);
            if (model == null) model = ArtLibrary.SpawnQuiet(TierModelPaths[tier]);
            if (model != null)
            {
                ArtLibrary.NormalizeToHeight(model, 0.6f);
                model.transform.SetParent(root, false);
            }
            else
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
                body.SetParent(root, false);
                body.localScale = new Vector3(0.24f, 0.28f, 0.24f);
                body.localPosition = new Vector3(0f, 0.26f, 0f);
                body.GetComponent<Renderer>().material = Mat(FallbackProp);
            }

            var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            rod.SetParent(root, false);
            rod.localScale = new Vector3(0.03f, 0.42f, 0.03f);
            rod.localPosition = new Vector3(0.1f, 0.48f, 0.24f);
            rod.localRotation = Quaternion.Euler(55f, 0f, 0f);
            rod.GetComponent<Renderer>().material = Mat(RodColor);

            var line = rod.gameObject.AddComponent<LineRenderer>();
            line.material = Mat(LineColor);
            line.widthMultiplier = 0.022f;
            line.positionCount = 2;
            line.useWorldSpace = true;

            // Petit bouchon de pêche rouge et blanc au bout de la ligne — fini les
            // gros ronds blancs qui pulsaient sur l'eau.
            var bobber = new GameObject("Bobber").transform;
            var bottom = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            bottom.SetParent(bobber, false);
            bottom.localScale = Vector3.one * 0.09f;
            bottom.GetComponent<Renderer>().material = Mat(Color.white);
            Destroy(bottom.GetComponent<Collider>());
            var top = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            top.SetParent(bobber, false);
            top.localScale = Vector3.one * 0.075f;
            top.localPosition = Vector3.up * 0.05f;
            top.GetComponent<Renderer>().material = Mat(BobberRed);
            Destroy(top.GetComponent<Collider>());

            return new CrewVisual { root = root, rod = rod, line = line, bobber = bobber };
        }

        void AnimateCrew(List<CrewVisual> crew, float t)
        {
            for (int i = 0; i < crew.Count; i++)
            {
                var visual = crew[i];
                if (!visual.root.gameObject.activeSelf) continue;

                visual.root.localRotation = Quaternion.Euler(
                    Mathf.Sin(t * 2.1f + i * 1.7f) * 4f,
                    visual.root.localEulerAngles.y,
                    0f);

                Vector3 rodTip = visual.rod.position + visual.rod.up * 0.42f;
                Vector3 waterPoint = visual.root.position + visual.root.forward * 1.3f;
                waterPoint.y = 0.02f;
                visual.line.SetPosition(0, rodTip);
                visual.line.SetPosition(1, waterPoint);

                // Le bouchon flotte et tressaille doucement au bout de la ligne.
                float bob = Mathf.Sin(t * 2.5f + i * 2.3f) * 0.035f;
                visual.bobber.position = waterPoint + Vector3.up * (0.02f + bob);
            }
        }

        void AnimateAmbientFish(float t)
        {
            // La vie marine vit dans l'EAU, pas autour de la coque : chaque poisson
            // suit son cap en coordonnées monde (il défile donc vraiment quand on
            // navigue), serpente un peu, et plonge sous la surface — le plan d'eau
            // étant opaque, c'est lui qui fait disparaître le poisson. Quand il sort
            // du champ, on le relâche discrètement de l'autre côté : il y a toujours
            // de la vie à voir, sans jamais de banc collé au bateau.
            Vector3 center = _boat != null ? _boat.position : Vector3.zero;
            float dt = Time.deltaTime;
            for (int i = 0; i < _ambientFish.Count; i++)
            {
                var fish = _ambientFish[i];
                fish.heading += Mathf.Sin(t * 0.5f + fish.wanderPhase) * 0.55f * dt;
                var direction = new Vector3(Mathf.Cos(fish.heading), 0f, Mathf.Sin(fish.heading));

                var position = fish.root.position + direction * (fish.speed * dt);
                position.y = fish.baseDepth + Mathf.Sin(t * 0.45f + fish.divePhase) * 0.3f;
                fish.root.position = position;
                fish.root.rotation = Quaternion.LookRotation(direction);

                float dx = position.x - center.x;
                float dz = position.z - center.z;
                if (dx * dx + dz * dz > FishRecycleRadius * FishRecycleRadius)
                    ReleaseAmbientFish(fish, center);
            }
        }

        // ---------- Effets ----------

        Vector3 RaycastWater(Vector2 screenPosition)
        {
            Vector3 center = _boat != null ? _boat.position : Vector3.zero;
            center.y = 0f;
            var ray = _camera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                var point = ray.GetPoint(distance);
                point.x = Mathf.Clamp(point.x, center.x - 4f, center.x + 4f);
                point.z = Mathf.Clamp(point.z, center.z - 3.5f, center.z + 3.5f);
                return point;
            }
            return center + new Vector3(1.5f, 0f, 2.2f);
        }

        IEnumerator FishJump(Vector3 from, string speciesId)
        {
            // Le poisson qui jaillit est la VRAIE espèce attrapée quand son modèle
            // custom est déposé — la capture d'un léviathan doit se voir !
            var fish = new GameObject("CaughtFish").transform;
            var model = SpawnFishModel(speciesId, 0.42f, fish);
            if (model == null)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                body.SetParent(fish, false);
                body.localScale = new Vector3(0.16f, 0.12f, 0.34f);
                body.GetComponent<Renderer>().material = Mat(new Color(0.75f, 0.82f, 0.88f));
                Destroy(body.GetComponent<Collider>());
            }

            Vector3 to = (_boat != null ? _boat.position : Vector3.zero) + Vector3.up;
            const float duration = 0.65f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                var pos = Vector3.Lerp(from, to, k);
                pos.y += Mathf.Sin(k * Mathf.PI) * 1.6f;
                fish.position = pos;
                fish.rotation = Quaternion.LookRotation((to - from).normalized) * Quaternion.Euler(k * 360f, 0f, 0f);
                yield return null;
            }
            Destroy(fish.gameObject);
        }

        IEnumerator FloatingText(Vector3 worldPosition, string text, Color color, int fontSize)
        {
            var go = new GameObject("FloatingText", typeof(TextMesh));
            var mesh = go.GetComponent<TextMesh>();
            mesh.text = text;
            mesh.font = UiKit.DefaultFont;
            mesh.fontSize = fontSize;
            mesh.characterSize = 0.035f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.color = color;
            go.GetComponent<MeshRenderer>().material = UiKit.DefaultFont.material;

            const float duration = 1.1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float k = elapsed / duration;
                go.transform.position = worldPosition + Vector3.up * (k * 1.1f);
                go.transform.rotation = _camera.transform.rotation;
                mesh.color = new Color(color.r, color.g, color.b, 1f - k * k);
                yield return null;
            }
            Destroy(go);
        }

        // ---------- Primitives de secours ----------

        Transform Block(string name, Transform parent, Vector3 localPosition, Vector3 size, Color color)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            block.name = name;
            block.SetParent(parent, false);
            block.localPosition = localPosition;
            block.localScale = size;
            block.GetComponent<Renderer>().material = Mat(color);
            return block;
        }

        Material Mat(Color color)
        {
            if (_materials.TryGetValue(color, out var cached)) return cached;
            var material = new Material(LitShader) { color = color };
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
            _materials[color] = material;
            return material;
        }
    }
}
