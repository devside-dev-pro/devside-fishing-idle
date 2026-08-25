using System.Collections;
using System.Collections.Generic;
using Devside.FishingIdle.Core;
using UnityEngine;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Le diorama 3D : un bateau vu en isométrique qui MATÉRIALISE l'état du jeu — chaque
    /// pêcheur acheté apparaît sur le pont, les caisses s'empilent avec la cale, le bateau
    /// grossit avec ses extensions, l'eau fonce avec la profondeur. Tout est construit par
    /// code en primitives flat (placeholder assumé : sera remplacé par un pack low-poly
    /// sans toucher à la logique). Ajouté automatiquement par GameUi.
    /// </summary>
    public class BoatView : MonoBehaviour
    {
        public static BoatView Instance { get; private set; }

        static readonly Color SkyColor = new Color(0.53f, 0.78f, 0.92f);
        static readonly Color WaterShallow = new Color(0.22f, 0.6f, 0.65f);
        static readonly Color WaterDeep = new Color(0.05f, 0.15f, 0.35f);
        static readonly Color HullColor = new Color(0.45f, 0.29f, 0.18f);
        static readonly Color DeckColor = new Color(0.66f, 0.47f, 0.3f);
        static readonly Color CabinColor = new Color(0.85f, 0.83f, 0.76f);
        static readonly Color CrateColor = new Color(0.72f, 0.55f, 0.28f);
        static readonly Color RodColor = new Color(0.25f, 0.18f, 0.12f);
        static readonly Color FishColor = new Color(0.75f, 0.82f, 0.88f);
        static readonly Color[] CrewColors =
        {
            new Color(0.9f, 0.55f, 0.2f),   // fisherman_t1
            new Color(0.25f, 0.55f, 0.85f), // fisherman_t2
            new Color(0.8f, 0.3f, 0.35f),   // fisherman_t3
        };

        // Emplacements de pêche sur le pont (positions locales au bateau), par tier.
        static readonly Vector3[] T1Slots =
        {
            new Vector3(-1.4f, 0.75f, 0.85f), new Vector3(-0.7f, 0.75f, 0.85f),
            new Vector3(0f, 0.75f, 0.85f), new Vector3(0.7f, 0.75f, 0.85f),
            new Vector3(-1.4f, 0.75f, -0.85f), new Vector3(-0.7f, 0.75f, -0.85f),
        };
        static readonly Vector3[] T2Slots =
        {
            new Vector3(1.6f, 0.75f, 0.5f), new Vector3(1.8f, 0.75f, 0f), new Vector3(1.6f, 0.75f, -0.5f),
        };

        Camera _camera;
        Transform _boat;
        Renderer _water;
        Transform _crateStack;
        readonly List<Transform> _crates = new List<Transform>();
        readonly List<Transform> _t1Crew = new List<Transform>();
        readonly List<Transform> _t2Crew = new List<Transform>();
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
        }

        void Update()
        {
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.State == null || _boat == null) return;
            var config = boot.Config;
            var state = boot.State;

            // Roulis léger : le diorama respire.
            float t = Time.time;
            _boat.localRotation = Quaternion.Euler(Mathf.Sin(t * 0.9f) * 1.6f, 0f, Mathf.Sin(t * 0.6f) * 2.2f);
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

            // Animation d'inactivité de l'équipage.
            AnimateCrew(_t1Crew, t);
            AnimateCrew(_t2Crew, t + 3f);
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
                point.x = Mathf.Clamp(point.x, -4f, 5f);
                point.z = Mathf.Clamp(point.z, -4f, 4f);
                return point;
            }
            return new Vector3(2.5f, 0f, 1.5f);
        }

        void SetupCameraAndLight()
        {
            _camera = Camera.main;
            if (_camera == null)
            {
                _camera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
                _camera.tag = "MainCamera";
            }
            _camera.orthographic = true;
            _camera.orthographicSize = 5.2f;
            _camera.transform.position = new Vector3(-7f, 8f, -7f);
            _camera.transform.rotation = Quaternion.Euler(32f, 45f, 0f);
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = SkyColor;

            var lightGo = new GameObject("Sun", typeof(Light));
            var sun = lightGo.GetComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            lightGo.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
            RenderSettings.ambientLight = new Color(0.55f, 0.6f, 0.66f);
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

            Block("Hull", _boat, new Vector3(0f, 0.35f, 0f), new Vector3(4.4f, 0.7f, 2.1f), HullColor);
            Block("Bow", _boat, new Vector3(2.5f, 0.35f, 0f), new Vector3(0.8f, 0.7f, 1.4f), HullColor);
            Block("Deck", _boat, new Vector3(0f, 0.73f, 0f), new Vector3(4.4f, 0.08f, 2.1f), DeckColor);
            Block("Cabin", _boat, new Vector3(-1.5f, 1.15f, 0f), new Vector3(1.1f, 0.8f, 1.2f), CabinColor);
            Block("Roof", _boat, new Vector3(-1.5f, 1.6f, 0f), new Vector3(1.3f, 0.1f, 1.4f), HullColor);

            // Zone des caisses (arrière) : matérialise le remplissage de la cale.
            _crateStack = new GameObject("Crates").transform;
            _crateStack.SetParent(_boat, false);
            _crateStack.localPosition = new Vector3(-2.4f, 0.75f, 0f);
            for (int i = 0; i < 6; i++)
            {
                var crate = Block("Crate", _crateStack,
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
            _cuttingStation.transform.localPosition = new Vector3(0.2f, 0.77f, -0.45f);
            Block("Table", _cuttingStation.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.8f, 0.36f, 0.5f), CabinColor);
            _cuttingKnife = Block("Knife", _cuttingStation.transform, new Vector3(0f, 0.42f, 0f), new Vector3(0.08f, 0.3f, 0.08f), Color.gray);
            _cuttingStation.SetActive(false);

            _filletStation = new GameObject("FilletStation");
            _filletStation.transform.SetParent(_boat, false);
            _filletStation.transform.localPosition = new Vector3(-0.6f, 0.77f, -0.45f);
            Block("Table", _filletStation.transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.8f, 0.36f, 0.5f), new Color(0.6f, 0.75f, 0.8f));
            _filletStation.SetActive(false);
        }

        void SyncCrew(List<Transform> crew, Vector3[] slots, int owned, Color color)
        {
            int wanted = Mathf.Min(owned, slots.Length);
            while (crew.Count < wanted)
            {
                var member = BuildCrewMember(slots[crew.Count], color);
                crew.Add(member);
            }
            for (int i = 0; i < crew.Count; i++)
                crew[i].gameObject.SetActive(i < wanted);
        }

        Transform BuildCrewMember(Vector3 localPosition, Color color)
        {
            var root = new GameObject("Crew").transform;
            root.SetParent(_boat, false);
            root.localPosition = localPosition;
            // Face à l'eau : les emplacements bâbord regardent -z, les autres +z.
            root.localRotation = Quaternion.Euler(0f, localPosition.z >= 0f ? 0f : 180f, 0f);

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

            return root;
        }

        static void AnimateCrew(List<Transform> crew, float t)
        {
            for (int i = 0; i < crew.Count; i++)
            {
                if (!crew[i].gameObject.activeSelf) continue;
                crew[i].localRotation = Quaternion.Euler(
                    Mathf.Sin(t * 2.1f + i * 1.7f) * 4f,
                    crew[i].localEulerAngles.y,
                    0f);
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
