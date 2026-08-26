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
        static readonly Color RippleColor = new Color(0.85f, 0.95f, 1f);
        static readonly Color FallbackHull = new Color(0.4f, 0.25f, 0.15f);
        static readonly Color FallbackDeck = new Color(0.72f, 0.5f, 0.3f);
        static readonly Color FallbackProp = new Color(0.72f, 0.55f, 0.28f);
        static readonly Color FallbackFish = new Color(0.16f, 0.32f, 0.42f);

        /// <summary>Bascule vers le grand navire à ce niveau d'extension de cale.</summary>
        const int LargeShipHoldLevel = 5;

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
            public Transform ripple;
        }

        class AmbientFish
        {
            public Transform root;
            public float radius;
            public float speed;
            public float phase;
            public float depth;
        }

        Camera _camera;
        Transform _boat;
        Renderer _water;
        Material _waterMaterial;
        bool _stylizedWater;

        GameObject _ship;
        string _shipPath;
        float _shipLength = 4.5f;
        float _shipWidth = 2f;

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

        /// <summary>
        /// Suivi du bateau, appelé par BoatController après le déplacement : la caméra
        /// reste cadrée sur la coque, le plan d'eau glisse sous elle (le bruit du shader
        /// est en coordonnées monde, donc l'eau « défile » vraiment), et l'anneau
        /// d'écume reçoit position + cap.
        /// </summary>
        public void FollowBoat(Transform root)
        {
            if (_camera != null)
                _camera.transform.position = root.position - _camera.transform.forward * 22f + _camera.transform.up * 0.7f;
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
            _camera.orthographicSize = 5.4f;
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
            string wanted = state.UpgradeLevel("cargo_hold") >= LargeShipHoldLevel
                ? ArtLibrary.ShipLarge
                : ArtLibrary.ShipSmall;
            if (_boat != null && wanted == _shipPath) return;
            _shipPath = wanted;
            RebuildShip(wanted);
        }

        void RebuildShip(string path)
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
            foreach (var list in _crew) list.Clear();
            _crates.Clear();

            float targetLength = path == ArtLibrary.ShipLarge ? 5.7f : 4.4f;
            _ship = ArtLibrary.Spawn(path, _boat);
            if (_ship != null)
            {
                var bounds = ArtLibrary.MeasureBounds(_ship);
                if (bounds.size.z > bounds.size.x)
                    _ship.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                ArtLibrary.NormalizeToSize(_ship, targetLength, 0.25f);
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
            {
                float x = fractions[i].x * _shipLength;
                float z = fractions[i].y * _shipWidth;
                result[i] = new Vector3(x, DeckHeightAt(x, z), z);
            }
            return result;
        }

        /// <summary>
        /// Hauteur du pont à un point (x, z) local : raycast vertical sur les colliders du
        /// navire, en gardant la surface la plus basse au-dessus de la flottaison (les
        /// voiles et mâts, plus hauts, sont ignorés).
        /// </summary>
        float DeckHeightAt(float x, float z)
        {
            if (_ship == null) return 0.74f;
            var hits = Physics.RaycastAll(new Vector3(x, 8f, z), Vector3.down, 16f);
            float best = float.MaxValue;
            foreach (var hit in hits)
            {
                if (!hit.transform.IsChildOf(_ship.transform)) continue;
                if (hit.point.y < 0.05f) continue;
                if (hit.point.y < best) best = hit.point.y;
            }
            return best < float.MaxValue ? best + 0.02f : 0.74f;
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
            float x = fx * _shipLength;
            float z = fz * _shipWidth;
            var holder = new GameObject("Prop").transform;
            holder.SetParent(_boat, false);
            holder.localPosition = new Vector3(x, DeckHeightAt(x, z), z);

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

        void BuildAmbientFish()
        {
            for (int i = 0; i < 5; i++)
                AddAmbientFish(AmbientSmallSpecies[i % AmbientSmallSpecies.Length],
                    ArtLibrary.SmallFish[i % ArtLibrary.SmallFish.Length], 0.5f,
                    2.7f + i * 0.5f, 0.28f + (i % 3) * 0.1f, i * 1.9f, -0.03f);
            AddAmbientFish("abyssal_shark", ArtLibrary.Shark, 1.1f, 5.4f, 0.22f, 1.2f, -0.05f);
            AddAmbientFish("moonfish", ArtLibrary.Manta, 0.95f, 5.9f, 0.16f, 3.8f, -0.08f);
            AddAmbientFish("leviathan", ArtLibrary.Whale, 2.3f, 7.2f, 0.08f, 5.5f, -0.18f);
        }

        void AddAmbientFish(string speciesId, string packPath, float size, float radius, float speed, float phase, float depth)
        {
            var root = new GameObject("AmbientFish").transform;
            var model = SpawnFishModel(speciesId, packPath, size, root);
            if (model == null)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                body.SetParent(root, false);
                body.localScale = new Vector3(size * 0.4f, size * 0.16f, size);
                body.GetComponent<Renderer>().material = Mat(FallbackFish);
                Destroy(body.GetComponent<Collider>());
            }
            _ambientFish.Add(new AmbientFish { root = root, radius = radius, speed = speed, phase = phase, depth = depth });
        }

        /// <summary>
        /// Modèle d'un poisson : l'espèce custom générée (Meshy — créée de profil, nez
        /// en +x, on la tourne vers +z comme les modèles du pack) si elle est déposée
        /// dans Resources/Art/Custom/Fish, sinon le modèle de pack ; null si rien.
        /// </summary>
        GameObject SpawnFishModel(string speciesId, string packPath, float size, Transform parent)
        {
            GameObject model = null;
            if (!string.IsNullOrEmpty(speciesId))
            {
                model = ArtLibrary.SpawnQuiet(ArtLibrary.CustomFish(speciesId));
                if (model != null)
                    model.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            }
            if (model == null) model = ArtLibrary.SpawnQuiet(packPath);
            if (model == null) return null;
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
                    crew[i].ripple.gameObject.SetActive(active);
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

            var ripple = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            ripple.name = "Ripple";
            ripple.localScale = new Vector3(0.3f, 0.006f, 0.3f);
            ripple.GetComponent<Renderer>().material = Mat(RippleColor);
            Destroy(ripple.GetComponent<Collider>());

            return new CrewVisual { root = root, rod = rod, line = line, ripple = ripple };
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

                float pulse = 0.24f + 0.1f * Mathf.Sin(t * 2.5f + i * 2.3f);
                visual.ripple.position = waterPoint;
                visual.ripple.localScale = new Vector3(pulse, 0.006f, pulse);
            }
        }

        void AnimateAmbientFish(float t)
        {
            // Les poissons tournent autour du bateau, où qu'il soit : la vie suit le joueur.
            Vector3 center = _boat != null ? _boat.position : Vector3.zero;
            center.y = 0f;
            for (int i = 0; i < _ambientFish.Count; i++)
            {
                var fish = _ambientFish[i];
                float angle = fish.phase + t * fish.speed;
                fish.root.position = center + new Vector3(
                    Mathf.Cos(angle) * fish.radius * 1.3f,
                    fish.depth,
                    Mathf.Sin(angle) * fish.radius);
                var tangent = new Vector3(-Mathf.Sin(angle) * 1.3f, 0f, Mathf.Cos(angle));
                fish.root.rotation = Quaternion.LookRotation(tangent);
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
            var model = SpawnFishModel(speciesId,
                ArtLibrary.SmallFish[Random.Range(0, ArtLibrary.SmallFish.Length)], 0.42f, fish);
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
