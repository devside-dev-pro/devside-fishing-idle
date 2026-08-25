using System.Collections;
using System.Collections.Generic;
using Devside.FishingIdle.Core;
using UnityEngine;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Le diorama 3D : le bateau vu quasi du dessus en portrait (référence : Fishing
    /// Frenzy / Hooked Inc), qui MATÉRIALISE l'état du jeu — chaque pêcheur acheté apparaît
    /// sur le pont et lance sa ligne à l'eau, les caisses s'empilent avec la cale, le bateau
    /// grossit, l'eau fonce avec la profondeur, des poissons nagent autour. Tout est
    /// construit par code en primitives flat (placeholder assumé : sera remplacé par un
    /// pack low-poly sans toucher à la logique). Ajouté automatiquement par GameUi.
    /// </summary>
    public class BoatView : MonoBehaviour
    {
        public static BoatView Instance { get; private set; }

        static readonly Color SkyColor = new Color(0.53f, 0.78f, 0.92f);
        static readonly Color WaterShallow = new Color(0.16f, 0.55f, 0.62f);
        static readonly Color WaterDeep = new Color(0.05f, 0.15f, 0.35f);
        static readonly Color HullColor = new Color(0.4f, 0.25f, 0.15f);
        static readonly Color WaterlineColor = new Color(0.2f, 0.12f, 0.08f);
        static readonly Color DeckColor = new Color(0.72f, 0.5f, 0.3f);
        static readonly Color CabinColor = new Color(0.9f, 0.87f, 0.78f);
        static readonly Color CrateColor = new Color(0.72f, 0.55f, 0.28f);
        static readonly Color RodColor = new Color(0.25f, 0.18f, 0.12f);
        static readonly Color LineColor = new Color(0.9f, 0.95f, 1f);
        static readonly Color RippleColor = new Color(0.85f, 0.95f, 1f);
        static readonly Color FishColor = new Color(0.75f, 0.82f, 0.88f);
        static readonly Color AmbientFishColor = new Color(0.16f, 0.32f, 0.42f);
        static readonly Color[] CrewColors =
        {
            new Color(0.9f, 0.55f, 0.2f),   // fisherman_t1
            new Color(0.25f, 0.55f, 0.85f), // fisherman_t2
            new Color(0.8f, 0.3f, 0.35f),   // fisherman_t3
        };

        // Emplacements de pêche sur le pont (positions locales au bateau), par tier.
        // La longueur du bateau court le long de l'axe x = la verticale de l'écran.
        static readonly Vector3[] T1Slots =
        {
            new Vector3(-1.4f, 0.75f, 0.85f), new Vector3(-0.5f, 0.75f, 0.85f),
            new Vector3(0.4f, 0.75f, 0.85f), new Vector3(1.2f, 0.75f, 0.85f),
            new Vector3(-1.4f, 0.75f, -0.85f), new Vector3(-0.5f, 0.75f, -0.85f),
        };
        static readonly Vector3[] T2Slots =
        {
            new Vector3(0.4f, 0.75f, -0.85f), new Vector3(1.2f, 0.75f, -0.85f),
            new Vector3(2.1f, 0.75f, 0f),
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
            public Transform body;
            public float radius;
            public float speed;
            public float phase;
        }

        Camera _camera;
        Transform _boat;
        Renderer _water;
        readonly List<Transform> _crates = new List<Transform>();
        readonly List<CrewVisual> _t1Crew = new List<CrewVisual>();
        readonly List<CrewVisual> _t2Crew = new List<CrewVisual>();
        readonly List<AmbientFish> _ambientFish = new List<AmbientFish>();
        GameObject _trawlerRig;
        GameObject _cuttingStation;
        GameObject _filletStation;
        Transform _cuttingKnife;

        readonly Dictionary<Color, Material> _materials = new Dictionary<Color, Material>();

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            SetupCameraAndLight();
            BuildSea();
            BuildBoat();
            BuildAmbientFish();
        }

        void Update()
        {
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.State == null || _boat == null) return;
            var config = boot.Config;
            var state = boot.State;
            float t = Time.time;

            // Roulis léger : le diorama respire.
            _boat.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 1.2f, 0f, Mathf.Sin(t * 0.6f) * 1.8f);
            _boat.localPosition = new Vector3(0f, 0.05f * Mathf.Sin(t * 0.8f), 0f);

            // L'eau fonce avec la profondeur.
            int depth = Catching.DepthLevel(config, state);
            _water.material.color = Color.Lerp(WaterShallow, WaterDeep, Mathf.Clamp01(depth / 3f));

            // Le bateau grossit avec les extensions de cale.
            int holdLevel = state.UpgradeLevel("cargo_hold");
            float scale = Mathf.Min(1.55f, 1f + 0.05f * holdLevel);
            _boat.localScale = new Vector3(scale, 1f + (scale - 1f) * 0.4f, scale);

            SyncCrew(_t1Crew, T1Slots, state.ProducerCount("fisherman_t1"), CrewColors[0]);
            SyncCrew(_t2Crew, T2Slots, state.ProducerCount("fisherman_t2"), CrewColors[1]);
            if (_trawlerRig != null) _trawlerRig.SetActive(state.ProducerCount("fisherman_t3") > 0);
            if (_cuttingStation != null) _cuttingStation.SetActive(state.ProducerCount("cutting_station") > 0);
            if (_filletStation != null) _filletStation.SetActive(state.ProducerCount("fillet_station") > 0);
            if (_cuttingKnife != null && _cuttingKnife.gameObject.activeInHierarchy)
                _cuttingKnife.localPosition = new Vector3(0f, 0.28f + Mathf.Abs(Mathf.Sin(t * 6f)) * 0.14f, 0f);

            // Les caisses matérialisent le remplissage de la cale.
            double capacity = Multipliers.HoldCapacity(config, state);
            float fill = capacity <= 0 ? 0f : Mathf.Clamp01((float)(state.TotalFishStock / capacity));
            int visibleCrates = Mathf.CeilToInt(fill * _crates.Count);
            for (int i = 0; i < _crates.Count; i++)
                _crates[i].gameObject.SetActive(i < visibleCrates);

            AnimateCrew(_t1Crew, t);
            AnimateCrew(_t2Crew, t + 3f);
            AnimateAmbientFish(t);
        }

        /// <summary>Poisson qui jaillit + chiffre qui vole, à l'endroit tapé (coordonnées écran).</summary>
        public void PlayCatchEffect(Vector2 screenPosition, CatchResult result)
        {
            if (_camera == null || result == null || result.amount <= 0) return;

            Vector3 splash = RaycastWater(screenPosition);
            StartCoroutine(FishJump(splash));
            string text = "+" + Numbers.Format(result.amount);
            StartCoroutine(FloatingText(splash + Vector3.up * 0.6f, text, Color.white, 42));
            if (result.newDiscovery)
                StartCoroutine(FloatingText(splash + Vector3.up * 1.3f,
                    $"{GameTheme.Species(result.speciesId)} — {GameTheme.NewDiscovery}",
                    new Color(1f, 0.85f, 0.3f), 52));
        }

        Vector3 RaycastWater(Vector2 screenPosition)
        {
            var ray = _camera.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float distance))
            {
                var point = ray.GetPoint(distance);
                // Reste près du bateau pour que l'effet soit toujours à l'écran.
                point.x = Mathf.Clamp(point.x, -4f, 4f);
                point.z = Mathf.Clamp(point.z, -3.5f, 3.5f);
                return point;
            }
            return new Vector3(1.5f, 0f, 2.2f);
        }

        void SetupCameraAndLight()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                _camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                _camera.tag = "MainCamera";
            }
            // Quasi top-down : la longueur du bateau (axe x) court le long de la verticale
            // de l'écran, avec juste assez d'inclinaison pour garder du volume.
            _camera.orthographic = true;
            _camera.orthographicSize = 5.4f;
            _camera.transform.rotation = Quaternion.Euler(70f, 90f, 0f);
            _camera.transform.position = -_camera.transform.forward * 22f + _camera.transform.up * 0.7f;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = SkyColor;

            // Réutilise la Directional Light déjà présente dans la scène (celle du template
            // par défaut) : en ajouter une deuxième surexpose tout le rendu.
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
            _water = water.GetComponent<Renderer>();
            _water.material = Mat(WaterShallow);
        }

        void BuildBoat()
        {
            _boat = new GameObject("Boat").transform;

            // Silhouette lisible du dessus : liseré sombre à la flottaison, coque qui
            // déborde du pont (cadre foncé tout autour), proue en pointe.
            Block("Waterline", _boat, new Vector3(0f, 0.12f, 0f), new Vector3(4.7f, 0.24f, 2.4f), WaterlineColor);
            Block("Hull", _boat, new Vector3(0f, 0.4f, 0f), new Vector3(4.5f, 0.56f, 2.2f), HullColor);
            Block("Deck", _boat, new Vector3(-0.1f, 0.72f, 0f), new Vector3(4.1f, 0.08f, 1.8f), DeckColor);
            var bowHull = Block("Bow", _boat, new Vector3(2.25f, 0.4f, 0f), new Vector3(1.56f, 0.56f, 1.56f), HullColor);
            bowHull.localRotation = Quaternion.Euler(0f, 45f, 0f);
            var bowDeck = Block("BowDeck", _boat, new Vector3(2.2f, 0.72f, 0f), new Vector3(1.3f, 0.08f, 1.3f), DeckColor);
            bowDeck.localRotation = Quaternion.Euler(0f, 45f, 0f);
            Block("Cabin", _boat, new Vector3(-1.6f, 1.12f, 0f), new Vector3(1.1f, 0.8f, 1.2f), CabinColor);
            Block("Roof", _boat, new Vector3(-1.6f, 1.57f, 0f), new Vector3(1.3f, 0.1f, 1.4f), HullColor);

            // Zone des caisses (arrière) : matérialise le remplissage de la cale.
            var crateStack = new GameObject("Crates").transform;
            crateStack.SetParent(_boat, false);
            crateStack.localPosition = new Vector3(-2.4f, 0.75f, 0f);
            for (int i = 0; i < 6; i++)
            {
                var crate = Block("Crate", crateStack,
                    new Vector3(i % 2 * 0.42f - 0.2f, 0.19f + i / 2 * 0.4f, (i % 3 - 1) * 0.14f),
                    new Vector3(0.38f, 0.38f, 0.38f), CrateColor);
                crate.gameObject.SetActive(false);
                _crates.Add(crate);
            }

            // Structure de chalutage (fisherman_t3) : mât + potence, cachée tant que rien n'est acheté.
            _trawlerRig = new GameObject("TrawlerRig");
            _trawlerRig.transform.SetParent(_boat, false);
            Block("Mast", _trawlerRig.transform, new Vector3(0.6f, 1.8f, 0f), new Vector3(0.12f, 2.2f, 0.12f), RodColor);
            Block("Boom", _trawlerRig.transform, new Vector3(0.6f, 2.7f, 0.7f), new Vector3(0.1f, 0.1f, 1.6f), RodColor);
            _trawlerRig.SetActive(false);

            // Ateliers de transformation, visibles une fois achetés.
            _cuttingStation = new GameObject("CuttingStation");
            _cuttingStation.transform.SetParent(_boat, false);
            _cuttingStation.transform.localPosition = new Vector3(-0.3f, 0.77f, 0f);
            Block("Table", _cuttingStation.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.8f, 0.36f, 0.5f), CabinColor);
            _cuttingKnife = Block("Knife", _cuttingStation.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.08f, 0.3f, 0.08f), Color.gray);
            _cuttingStation.SetActive(false);

            _filletStation = new GameObject("FilletStation");
            _filletStation.transform.SetParent(_boat, false);
            _filletStation.transform.localPosition = new Vector3(-1.1f, 0.77f, 0f);
            Block("Table", _filletStation.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.8f, 0.36f, 0.5f), new Color(0.6f, 0.75f, 0.8f));
            _filletStation.SetActive(false);
        }

        void BuildAmbientFish()
        {
            for (int i = 0; i < 7; i++)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
                body.name = "AmbientFish";
                body.localScale = new Vector3(0.14f, 0.05f, 0.3f);
                body.GetComponent<Renderer>().material = Mat(AmbientFishColor);
                Object.Destroy(body.GetComponent<Collider>());
                _ambientFish.Add(new AmbientFish
                {
                    body = body,
                    radius = 2.6f + i * 0.45f,
                    speed = 0.25f + (i % 3) * 0.12f,
                    phase = i * 1.9f,
                });
            }
        }

        void SyncCrew(List<CrewVisual> crew, Vector3[] slots, int owned, Color color)
        {
            int wanted = Mathf.Min(owned, slots.Length);
            while (crew.Count < wanted)
                crew.Add(BuildCrewMember(slots[crew.Count], color));
            for (int i = 0; i < crew.Count; i++)
            {
                bool active = i < wanted;
                crew[i].root.gameObject.SetActive(active);
                crew[i].ripple.gameObject.SetActive(active);
            }
        }

        CrewVisual BuildCrewMember(Vector3 localPosition, Color color)
        {
            var root = new GameObject("Crew").transform;
            root.SetParent(_boat, false);
            root.localPosition = localPosition;
            // Face à l'eau : les emplacements de chaque bord regardent vers l'extérieur,
            // celui de la proue vers l'avant.
            float yaw = Mathf.Abs(localPosition.z) < 0.1f ? 90f : (localPosition.z >= 0f ? 0f : 180f);
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule).transform;
            body.SetParent(root, false);
            body.localScale = new Vector3(0.26f, 0.3f, 0.26f);
            body.localPosition = new Vector3(0f, 0.28f, 0f);
            body.GetComponent<Renderer>().material = Mat(color);

            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            head.SetParent(root, false);
            head.localScale = Vector3.one * 0.2f;
            head.localPosition = new Vector3(0f, 0.66f, 0f);
            head.GetComponent<Renderer>().material = Mat(new Color(0.95f, 0.8f, 0.65f));

            var rod = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            rod.SetParent(root, false);
            rod.localScale = new Vector3(0.035f, 0.5f, 0.035f);
            rod.localPosition = new Vector3(0.12f, 0.55f, 0.3f);
            rod.localRotation = Quaternion.Euler(55f, 0f, 0f);
            rod.GetComponent<Renderer>().material = Mat(RodColor);

            // Ligne de pêche du bout de la canne jusqu'à l'eau, avec un rond d'impact.
            var line = rod.gameObject.AddComponent<LineRenderer>();
            line.material = Mat(LineColor);
            line.widthMultiplier = 0.025f;
            line.positionCount = 2;
            line.useWorldSpace = true;

            var ripple = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            ripple.name = "Ripple";
            ripple.localScale = new Vector3(0.3f, 0.006f, 0.3f);
            ripple.GetComponent<Renderer>().material = Mat(RippleColor);
            Object.Destroy(ripple.GetComponent<Collider>());

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

                // La ligne part du bout de la canne et plonge dans l'eau devant le pêcheur.
                Vector3 rodTip = visual.rod.position + visual.rod.up * 0.5f;
                Vector3 waterPoint = visual.root.position + visual.root.forward * 1.35f;
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
            for (int i = 0; i < _ambientFish.Count; i++)
            {
                var fish = _ambientFish[i];
                float angle = fish.phase + t * fish.speed;
                var pos = new Vector3(Mathf.Cos(angle) * fish.radius * 1.3f, 0.04f, Mathf.Sin(angle) * fish.radius);
                fish.body.position = pos;
                var tangent = new Vector3(-Mathf.Sin(angle) * 1.3f, 0f, Mathf.Cos(angle));
                fish.body.rotation = Quaternion.LookRotation(tangent);
            }
        }

        IEnumerator FishJump(Vector3 from)
        {
            var fish = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            fish.localScale = new Vector3(0.16f, 0.12f, 0.34f);
            fish.GetComponent<Renderer>().material = Mat(FishColor);
            Object.Destroy(fish.GetComponent<Collider>());

            Vector3 to = _boat.position + new Vector3(0f, 1f, 0f);
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
            Object.Destroy(fish.gameObject);
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
            Object.Destroy(go);
        }

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
            var material = new Material(Shader.Find("Standard")) { color = color };
            material.SetFloat("_Glossiness", 0.1f);
            _materials[color] = material;
            return material;
        }
    }
}
