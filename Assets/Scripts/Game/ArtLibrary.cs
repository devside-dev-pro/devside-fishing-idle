using System.Collections.Generic;
using UnityEngine;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Accès aux modèles 3D des packs (dossier Resources/Art) : chemins connus,
    /// instanciation défensive (null si le modèle manque → l'appelant garde un fallback),
    /// conversion automatique des matériaux importés vers URP (anti-magenta), mise à
    /// l'échelle par mesure de bounds, et colliders pour les raycasts de pont.
    /// </summary>
    public static class ArtLibrary
    {
        // Pack pirate Quaternius (style low-poly cartoon, atlas commun).
        public const string ShipSmall = "Art/PirateQuaternius/Ship_Small";
        public const string ShipLarge = "Art/PirateQuaternius/Ship_Large";
        public const string CrewT1 = "Art/PirateQuaternius/Characters_Henry";
        public const string CrewT2 = "Art/PirateQuaternius/Characters_Anne";
        public const string CrewT3 = "Art/PirateQuaternius/Characters_Mako";
        public const string Captain = "Art/PirateQuaternius/Characters_Captain_Barbarossa";
        public const string Barrel = "Art/PirateQuaternius/Prop_Barrel";
        public const string CuttingStation = "Art/PirateQuaternius/Environment_Sawmill";
        public const string FilletStation = "Art/PirateQuaternius/Prop_Bucket_Fishes";
        public const string Dock = "Art/PirateQuaternius/Environment_Dock";
        public const string House = "Art/PirateQuaternius/Environment_House1";
        public const string Merchant = "Art/PirateQuaternius/Characters_Sharky";

        // Décor d'îles : plusieurs variantes pour que chaque île ait sa silhouette.
        public static readonly string[] Cliffs =
        {
            "Art/PirateQuaternius/Environment_Cliff1",
            "Art/PirateQuaternius/Environment_Cliff2",
            "Art/PirateQuaternius/Environment_Cliff3",
            "Art/PirateQuaternius/Environment_Cliff4",
        };
        public static readonly string[] Palms =
        {
            "Art/PirateQuaternius/Environment_PalmTree_1",
            "Art/PirateQuaternius/Environment_PalmTree_2",
            "Art/PirateQuaternius/Environment_PalmTree_3",
        };
        public static readonly string[] Rocks =
        {
            "Art/PirateQuaternius/Environment_Rock_1",
            "Art/PirateQuaternius/Environment_Rock_2",
            "Art/PirateQuaternius/Environment_Rock_3",
            "Art/PirateQuaternius/Environment_Rock_4",
            "Art/PirateQuaternius/Environment_Rock_5",
        };

        // Assets custom générés par IA (recette : docs/ASSET-PIPELINE.md), déposés dans
        // Resources/Art/Custom/. Prioritaires quand présents, repli sur les packs sinon.
        public static string CustomFish(string speciesId) => "Art/Custom/Fish/" + speciesId;
        public static string CustomProp(string name) => "Art/Custom/Props/" + name;

        // Pack poissons Quaternius.
        public static readonly string[] SmallFish =
        {
            "Art/FishQuaternius/Fish1",
            "Art/FishQuaternius/Fish2",
            "Art/FishQuaternius/Fish3",
        };
        public const string Shark = "Art/FishQuaternius/Shark";
        public const string Manta = "Art/FishQuaternius/Manta ray";
        public const string Whale = "Art/FishQuaternius/Whale";

        static readonly Dictionary<Material, Material> FixedMaterials = new Dictionary<Material, Material>();
        static Texture2D _pirateAtlas;
        static bool _pirateAtlasLoaded;

        /// <summary>
        /// L'atlas du pack pirate : les FBX Quaternius ne lient pas leur texture à
        /// l'import, on la force sur les matériaux sans texture de ce pack (sinon tout
        /// le navire et l'équipage sortent blancs).
        /// </summary>
        static Texture2D PirateAtlas
        {
            get
            {
                if (!_pirateAtlasLoaded)
                {
                    _pirateAtlasLoaded = true;
                    _pirateAtlas = Resources.Load<Texture2D>("Art/PirateQuaternius/Atlas_Pirate");
                }
                return _pirateAtlas;
            }
        }

        /// <summary>Instancie un modèle de Resources ; null s'il est introuvable (fallback à l'appelant).</summary>
        public static GameObject Spawn(string resourcePath, Transform parent = null)
        {
            var instance = SpawnQuiet(resourcePath, parent);
            if (instance == null)
                Debug.LogWarning($"ArtLibrary : modèle introuvable « {resourcePath} »");
            return instance;
        }

        /// <summary>
        /// Comme Spawn, sans avertissement : pour les chemins optionnels (assets custom
        /// pas encore déposés) où l'absence est un cas normal, pas une erreur.
        /// </summary>
        public static GameObject SpawnQuiet(string resourcePath, Transform parent = null)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return null;
            var instance = Object.Instantiate(prefab, parent);
            var fallbackTexture = resourcePath.StartsWith("Art/PirateQuaternius/") ? PirateAtlas : null;
            FixMaterials(instance, fallbackTexture);
            return instance;
        }

        /// <summary>Premier modèle disponible parmi plusieurs chemins (custom d'abord, pack ensuite).</summary>
        public static GameObject SpawnFirst(Transform parent, params string[] paths)
        {
            for (int i = 0; i < paths.Length; i++)
            {
                var instance = SpawnQuiet(paths[i], parent);
                if (instance != null) return instance;
            }
            return null;
        }

        static GameObject[] _customCharacters;

        /// <summary>Tous les personnages custom déposés (cache du LoadAll).</summary>
        static GameObject[] CustomCharacters
            => _customCharacters ?? (_customCharacters = Resources.LoadAll<GameObject>("Art/Custom/Characters"));

        /// <summary>
        /// Personnage custom retrouvé par fragments de nom (essayés dans l'ordre), car
        /// les fichiers de Art/Custom/Characters sont nommés librement (ex.
        /// char_capitaine_x.glb). Quand un personnage existe en plusieurs versions
        /// (une T-pose brute et une animée), la version ANIMÉE gagne : composant
        /// d'animation présent sur le prefab, indice « anim » dans le nom, malus
        /// « tpose ». Null si aucun ne correspond.
        /// </summary>
        public static GameObject SpawnCustomCharacter(Transform parent, params string[] nameFragments)
        {
            for (int f = 0; f < nameFragments.Length; f++)
            {
                GameObject best = null;
                int bestScore = int.MinValue;
                foreach (var prefab in CustomCharacters)
                {
                    string name = prefab.name.ToLowerInvariant();
                    if (!name.Contains(nameFragments[f])) continue;
                    int score = CharacterScore(prefab, name);
                    if (best == null || score > bestScore
                        || (score == bestScore && string.CompareOrdinal(prefab.name, best.name) > 0))
                    {
                        best = prefab;
                        bestScore = score;
                    }
                }
                if (best == null) continue;
                var instance = Object.Instantiate(best, parent);
                FixMaterials(instance);
                return instance;
            }
            return null;
        }

        /// <summary>Le pont doit vivre : une version animée bat une T-pose brute.</summary>
        static int CharacterScore(GameObject prefab, string lowerName)
        {
            int score = 0;
            if (prefab.GetComponentInChildren<Animation>(true) != null
                || prefab.GetComponentInChildren<Animator>(true) != null) score += 4;
            if (lowerName.Contains("anim")) score += 2;
            if (lowerName.Contains("tpose") || lowerName.Contains("t_pose") || lowerName.Contains("t-pose")) score -= 3;
            return score;
        }

        /// <summary>Bounds monde combinées de tous les renderers (zéro si aucun).</summary>
        public static Bounds MeasureBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        /// <summary>
        /// Met le modèle à l'échelle pour que sa plus grande dimension horizontale fasse
        /// <paramref name="targetSize"/>, puis pose ses pieds (min.y) sur son parent.
        /// À appeler quand la hiérarchie parente est encore en transform identité.
        /// </summary>
        public static void NormalizeToSize(GameObject model, float targetSize, float sinkDepth = 0f)
        {
            var bounds = MeasureBounds(model);
            float current = Mathf.Max(bounds.size.x, bounds.size.z);
            if (current < 0.001f) return;
            float scale = targetSize / current;
            model.transform.localScale = model.transform.localScale * scale;

            bounds = MeasureBounds(model);
            var parentPos = model.transform.parent != null ? model.transform.parent.position : Vector3.zero;
            model.transform.position += new Vector3(
                parentPos.x - bounds.center.x,
                parentPos.y - bounds.min.y - sinkDepth,
                parentPos.z - bounds.center.z);
        }

        /// <summary>
        /// Met le modèle à l'échelle pour une hauteur cible, puis pose ses pieds (min.y)
        /// sur son parent. Même contrat qu'au-dessus : parent en transform identité.
        /// </summary>
        public static void NormalizeToHeight(GameObject model, float targetHeight)
        {
            var bounds = MeasureBounds(model);
            if (bounds.size.y < 0.001f) return;
            float scale = targetHeight / bounds.size.y;
            model.transform.localScale = model.transform.localScale * scale;

            bounds = MeasureBounds(model);
            var parentPos = model.transform.parent != null ? model.transform.parent.position : Vector3.zero;
            model.transform.position += new Vector3(
                parentPos.x - bounds.center.x,
                parentPos.y - bounds.min.y,
                parentPos.z - bounds.center.z);
        }

        /// <summary>MeshColliders sur tous les meshes (pour les raycasts de hauteur de pont).</summary>
        public static void AddColliders(GameObject root)
        {
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
                if (filter.GetComponent<Collider>() == null && filter.sharedMesh != null)
                    filter.gameObject.AddComponent<MeshCollider>();
        }

        /// <summary>
        /// Remplace les matériaux importés hors-URP (shader Standard → magenta sous URP)
        /// par des équivalents URP/Lit, en conservant couleur et texture. Mise en cache :
        /// toutes les instances d'un même matériau source partagent le même remplacement.
        /// </summary>
        public static void FixMaterials(GameObject root, Texture2D fallbackTexture = null)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var replacement = GetOrCreateUrpMaterial(materials[i], lit, fallbackTexture);
                    if (replacement != materials[i])
                    {
                        materials[i] = replacement;
                        changed = true;
                    }
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        static Material GetOrCreateUrpMaterial(Material source, Shader lit, Texture2D fallbackTexture)
        {
            if (source == null) return null;
            if (source.shader != null && source.shader.name.Contains("Universal") && source.mainTexture != null)
                return source;
            if (FixedMaterials.TryGetValue(source, out var cached)) return cached;

            var material = new Material(lit) { name = source.name + " (URP)" };
            if (source.HasProperty("_Color")) material.color = source.color;
            var texture = source.HasProperty("_MainTex") ? source.mainTexture : null;
            if (texture == null) texture = fallbackTexture;
            if (texture != null) material.mainTexture = texture;
            material.SetFloat("_Smoothness", 0.08f);

            FixedMaterials[source] = material;
            return material;
        }
    }
}
