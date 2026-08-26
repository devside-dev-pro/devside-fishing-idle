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
        public const string Cliff = "Art/PirateQuaternius/Environment_Cliff1";
        public const string Palm = "Art/PirateQuaternius/Environment_PalmTree_1";

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

        /// <summary>Instancie un modèle de Resources ; null s'il est introuvable (fallback à l'appelant).</summary>
        public static GameObject Spawn(string resourcePath, Transform parent = null)
        {
            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogWarning($"ArtLibrary : modèle introuvable « {resourcePath} »");
                return null;
            }
            var instance = Object.Instantiate(prefab, parent);
            FixMaterials(instance);
            return instance;
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
        public static void FixMaterials(GameObject root)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    var replacement = GetOrCreateUrpMaterial(materials[i], lit);
                    if (replacement != materials[i])
                    {
                        materials[i] = replacement;
                        changed = true;
                    }
                }
                if (changed) renderer.sharedMaterials = materials;
            }
        }

        static Material GetOrCreateUrpMaterial(Material source, Shader lit)
        {
            if (source == null) return null;
            if (source.shader != null && source.shader.name.Contains("Universal")) return source;
            if (FixedMaterials.TryGetValue(source, out var cached)) return cached;

            var material = new Material(lit) { name = source.name + " (URP)" };
            if (source.HasProperty("_Color")) material.color = source.color;
            if (source.HasProperty("_MainTex") && source.mainTexture != null)
                material.mainTexture = source.mainTexture;
            material.SetFloat("_Smoothness", 0.08f);

            FixedMaterials[source] = material;
            return material;
        }
    }
}
