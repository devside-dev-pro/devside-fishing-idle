using System.Collections.Generic;
using UnityEngine;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// L'archipel : des îles à positions fixes et des anneaux de zones concentriques
    /// autour du point de départ. La zone où se trouve le bateau devient
    /// state.currentZone (la profondeur est de la géographie — voir Core/Catching) ;
    /// la coque borne le rayon navigable (AllowedRadius). Chaque île est un petit
    /// village bâti par code (sol, maisons, habitants, végétation, décor signature)
    /// via ArtLibrary, avec fallback si un modèle manque.
    /// </summary>
    public class WorldMap : MonoBehaviour
    {
        public class Island
        {
            public string id;
            public Vector3 position;

            /// <summary>Rayon de la plage : la référence de tout le décor de l'île.</summary>
            public float radius;

            /// <summary>
            /// Rayon réellement interdit au bateau : un peu au-delà du haut-fond, pour
            /// que la coque ne vienne jamais chevaucher le sable.
            /// </summary>
            public float BlockRadius => radius * 1.14f;
            public int zone;
            public bool hasMerchant;

            /// <summary>Couleur de la plage et de l'intérieur : chaque île a son climat.</summary>
            public Color sand;
            public Color inland;

            // Taille du village : le Vieux Ponton est un hameau, les îles lointaines
            // sont de vrais villages (retour playtest : « les îles doivent être comme
            // des petits villages, la première toute petite mais les autres plus grandes »).
            public int houses;
            public int palms;
            public int rocks;
            public int villagers;
        }

        /// <summary>Frontières des zones (rayons) ; au-delà de la dernière = zone 3.</summary>
        static readonly float[] ZoneRadii = { 35f, 85f, 145f };

        /// <summary>Hauteur de la plage : tout le décor de l'île se pose à ce niveau.</summary>
        const float SurfaceY = 0.34f;

        static readonly Island[] Islands =
        {
            new Island
            {
                id = "island_port", position = new Vector3(7f, 0f, -2.5f), radius = 4.2f,
                zone = 0, hasMerchant = true,
                sand = new Color(0.90f, 0.82f, 0.62f), inland = new Color(0.55f, 0.68f, 0.40f),
                houses = 1, palms = 3, rocks = 3, villagers = 1,
            },
            new Island
            {
                id = "island_lagoon", position = new Vector3(55f, 0f, -18f), radius = 7.5f, zone = 1,
                sand = new Color(0.96f, 0.90f, 0.72f), inland = new Color(0.48f, 0.72f, 0.42f),
                houses = 3, palms = 7, rocks = 4, villagers = 2,
            },
            new Island
            {
                id = "island_mist", position = new Vector3(115f, 0f, 25f), radius = 10f, zone = 2,
                sand = new Color(0.74f, 0.76f, 0.72f), inland = new Color(0.36f, 0.50f, 0.42f),
                houses = 4, palms = 8, rocks = 6, villagers = 3,
            },
            new Island
            {
                id = "island_abyss", position = new Vector3(175f, 0f, -35f), radius = 13f, zone = 3,
                sand = new Color(0.46f, 0.42f, 0.48f), inland = new Color(0.30f, 0.27f, 0.36f),
                houses = 5, palms = 6, rocks = 8, villagers = 3,
            },
        };

        void Start()
        {
            for (int i = 0; i < Islands.Length; i++) BuildIsland(Islands[i], i);
        }

        /// <summary>Les îles, en lecture (pour la carte de l'archipel).</summary>
        public static IReadOnlyList<Island> AllIslands => Islands;

        /// <summary>Frontières de zones, en lecture (pour la carte de l'archipel).</summary>
        public static IReadOnlyList<float> ZoneBoundaries => ZoneRadii;

        /// <summary>Zone de profondeur à une position du monde (distance XZ au point de départ).</summary>
        public static int ZoneAt(Vector3 position)
        {
            float distance = new Vector2(position.x, position.z).magnitude;
            for (int i = 0; i < ZoneRadii.Length; i++)
                if (distance <= ZoneRadii[i])
                    return i;
            return ZoneRadii.Length;
        }

        /// <summary>Rayon navigable maximal pour un niveau de coque donné.</summary>
        public static float AllowedRadius(int maxZone)
            => maxZone >= ZoneRadii.Length ? float.PositiveInfinity : ZoneRadii[maxZone];

        /// <summary>L'île marchande à portée d'accostage de cette position ; null sinon.</summary>
        public static Island MerchantAt(Vector3 position)
        {
            for (int i = 0; i < Islands.Length; i++)
            {
                var island = Islands[i];
                if (!island.hasMerchant) continue;
                var offset = new Vector2(position.x - island.position.x, position.z - island.position.z);
                // Rayon serré : « à quai » veut dire collé à l'île, pas à deux
                // longueurs de bateau (le bouton de vente s'affichait en pleine mer).
                if (offset.magnitude <= island.BlockRadius + 1.6f) return island;
            }
            return null;
        }

        /// <summary>Repousse une position hors des plages : le bateau contourne les îles.</summary>
        public static Vector3 PushOutOfIslands(Vector3 position)
        {
            for (int i = 0; i < Islands.Length; i++)
            {
                var island = Islands[i];
                var offset = new Vector2(position.x - island.position.x, position.z - island.position.z);
                float distance = offset.magnitude;
                float block = island.BlockRadius;
                if (distance >= block) continue;
                offset = distance < 0.001f ? Vector2.right : offset / distance;
                position.x = island.position.x + offset.x * block;
                position.z = island.position.z + offset.y * block;
            }
            return position;
        }

        void BuildIsland(Island island, int index)
        {
            var root = new GameObject(island.id).transform;
            root.SetParent(transform, false);
            root.position = island.position;

            BuildGround(island, root);
            BuildVillage(island, root, index);
            if (island.hasMerchant) BuildMerchantOutpost(island, root);
            BuildSignatureDecor(island, root);
        }

        /// <summary>
        /// Le sol : un haut-fond qui déborde de la plage (l'île ne flotte plus comme un
        /// caillou posé sur l'eau), la plage, et un intérieur végétal légèrement
        /// surélevé. Trois disques suffisent à donner du relief en lecture verticale.
        /// </summary>
        static void BuildGround(Island island, Transform root)
        {
            Disc(root, "Shallows", island.radius * 1.1f, 0.012f, 0.006f,
                Color.Lerp(island.sand, new Color(0.45f, 0.85f, 0.85f), 0.55f));
            Disc(root, "Beach", island.radius * 0.99f, SurfaceY * 0.5f, SurfaceY * 0.5f, island.sand);
            Disc(root, "Inland", island.radius * 0.74f, SurfaceY * 0.62f, SurfaceY * 0.55f, island.inland);
        }

        /// <summary>Disque plat (cylindre aplati), sans collider : le bateau est repoussé par le code.</summary>
        static void Disc(Transform root, string name, float radius, float halfHeight, float centerY, Color color)
        {
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            disc.name = name;
            disc.SetParent(root, false);
            disc.localScale = new Vector3(radius * 2f, halfHeight, radius * 2f);
            disc.localPosition = new Vector3(0f, centerY, 0f);
            Destroy(disc.GetComponent<Collider>());
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var material = new Material(lit != null ? lit : Shader.Find("Standard")) { color = color };
            material.SetFloat("_Smoothness", 0.05f);
            disc.GetComponent<Renderer>().material = material;
        }

        /// <summary>
        /// Le village : des maisons en éventail dos au large avec leurs tonneaux, des
        /// habitants, la végétation et les rochers de bordure. Tout vient des compteurs
        /// de l'île, donc l'archipel raconte une progression : hameau → village.
        /// </summary>
        static void BuildVillage(Island island, Transform root, int index)
        {
            var toSea = ToSea(island);
            float seaAngle = Mathf.Atan2(toSea.z, toSea.x);
            // Le comptoir occupe déjà la façade maritime : le village se décale.
            float villageCenter = seaAngle + Mathf.PI + (island.hasMerchant ? 1.2f : 0f);

            for (int h = 0; h < island.houses; h++)
            {
                float spread = island.houses == 1 ? 0f : (h / (island.houses - 1f) - 0.5f) * 2.6f;
                float angle = villageCenter + spread;
                var house = Place(root, island, ArtLibrary.Houses[(index + h) % ArtLibrary.Houses.Length],
                    1.7f + 0.12f * index, angle, 0.44f, FaceCenterYaw(angle));
                if (house == null) continue;

                // Chaque maison a son tonneau ou sa caisse : le village a l'air habité.
                string prop = (index + h) % 2 == 0 ? ArtLibrary.Barrel : ArtLibrary.Tropical("Barrel_04");
                Place(root, island, prop, 0.42f, angle + 0.22f, 0.62f);
            }

            for (int v = 0; v < island.villagers; v++)
            {
                float angle = villageCenter + (v - (island.villagers - 1) * 0.5f) * 0.9f;
                string path = island.id == "island_abyss"
                    ? ArtLibrary.Skeleton
                    : ArtLibrary.Villagers[(index + v) % ArtLibrary.Villagers.Length];
                Place(root, island, path, 0.8f, angle, 0.24f, FaceCenterYaw(angle) + 180f, byHeight: true);
            }

            // Végétation en spirale (angle d'or) : dense, sans motif visible.
            for (int p = 0; p < island.palms; p++)
            {
                float angle = p * 2.39996f + index;
                float distance = 0.3f + (p % 4) * 0.16f;
                Place(root, island, ArtLibrary.Palms[(index + p) % ArtLibrary.Palms.Length],
                    1.6f + 0.25f * (p % 3), angle, distance, byHeight: true);
            }

            for (int r = 0; r < island.rocks; r++)
            {
                float angle = r * 2.39996f + index * 1.7f + 0.9f;
                Place(root, island, ArtLibrary.Rocks[(index * 2 + r) % ArtLibrary.Rocks.Length],
                    0.6f + 0.18f * (r % 3), angle, 0.88f, up: SurfaceY * 0.55f);
            }

            // Un ponton de village sur les îles sans comptoir : un port de pêche vit.
            if (!island.hasMerchant)
            {
                float dockAngle = seaAngle + 0.7f;
                var outward = new Vector3(Mathf.Cos(dockAngle), 0f, Mathf.Sin(dockAngle));
                PlaceDock(root, island, outward, 2.4f, 0.88f);
                Place(root, island, ArtLibrary.Anchor, 0.5f, dockAngle - 0.35f, 0.72f);
            }
        }

        /// <summary>
        /// La touche propre à chaque île (packs itch.io — voir CREDITS.md) : cordages
        /// au port, corail au lagon, campement dans les brumes, trésor aux abysses.
        /// Chaque prop est optionnel : l'île reste correcte si un modèle manque.
        /// </summary>
        static void BuildSignatureDecor(Island island, Transform root)
        {
            switch (island.id)
            {
                case "island_port":
                    Place(root, island, ArtLibrary.Tropical("Plank_01"), 0.9f, 5.1f, 0.7f);
                    Place(root, island, ArtLibrary.Kay("rope_bundle_A"), 0.3f, 5.9f, 0.55f);
                    Place(root, island, ArtLibrary.Tropical("Plant_01"), 0.45f, 4.4f, 0.5f);
                    break;
                case "island_lagoon":
                    Place(root, island, ArtLibrary.Tropical("CoralReef_01"), 2.4f, 0.8f, 1.22f, up: -0.12f);
                    Place(root, island, ArtLibrary.Tropical("CoralReef_01"), 1.8f, 3.9f, 1.18f, up: -0.14f);
                    Place(root, island, ArtLibrary.Tropical("Plant_01"), 0.5f, 2.4f, 0.55f);
                    Place(root, island, ArtLibrary.Beach("Beach_Umbrella"), 1.1f, 1.4f, 0.78f);
                    Place(root, island, ArtLibrary.Beach("Surfboard"), 0.9f, 1.1f, 0.86f);
                    break;
                case "island_mist":
                    Place(root, island, ArtLibrary.Tropical("Tent_01"), 1.5f, 1.2f, 0.5f);
                    Place(root, island, ArtLibrary.Kay("lantern"), 0.3f, 1.7f, 0.62f);
                    Place(root, island, ArtLibrary.Kay("torch"), 0.5f, 2.6f, 0.66f);
                    Place(root, island, ArtLibrary.Tropical("Plant_01"), 0.5f, 5.2f, 0.6f);
                    break;
                case "island_abyss":
                    Place(root, island, ArtLibrary.Tropical("Chest_01"), 0.7f, 1.1f, 0.5f);
                    Place(root, island, ArtLibrary.Tropical("Skull_01"), 0.4f, 1.6f, 0.6f);
                    Place(root, island, ArtLibrary.Tropical("PirateSword_01"), 0.55f, 2.1f, 0.45f);
                    Place(root, island, "Art/PirateQuaternius/Environment_LargeBones", 1.6f, 4.3f, 0.62f);
                    Place(root, island, "Art/PirateQuaternius/Environment_Skulls", 0.6f, 3.4f, 0.7f);
                    break;
            }
        }

        /// <summary>Comptoir du marchand : ponton tourné vers le large, cabane, et le marchand qui attend.</summary>
        static void BuildMerchantOutpost(Island island, Transform root)
        {
            var toSea = ToSea(island);

            PlaceDock(root, island, toSea, 2.8f, 0.9f);

            var house = ArtLibrary.Spawn(ArtLibrary.House, root);
            if (house != null)
            {
                house.transform.rotation = Quaternion.LookRotation(-toSea);
                ArtLibrary.NormalizeToSize(house, 2f);
                house.transform.position += toSea * (island.radius * 0.28f) + Vector3.up * SurfaceY;
            }

            var merchant = ArtLibrary.Spawn(ArtLibrary.Merchant, root);
            if (merchant != null)
            {
                merchant.transform.rotation = Quaternion.LookRotation(toSea);
                ArtLibrary.NormalizeToHeight(merchant, 0.85f);
                merchant.transform.position += toSea * (island.radius * 0.55f) + Vector3.up * SurfaceY;
            }

            // Une lanterne au bout du ponton : le comptoir se repère de loin.
            var lantern = ArtLibrary.SpawnQuiet(ArtLibrary.Kay("lantern"), root);
            if (lantern != null)
            {
                ArtLibrary.NormalizeToSize(lantern, 0.24f);
                lantern.transform.position += toSea * (island.radius * 0.95f) + Vector3.up * (SurfaceY * 0.55f);
            }
        }

        /// <summary>
        /// Un ponton qui part de la plage vers le large : ponton du pack tropical en
        /// priorité (bien plus « plage » que le dock pirate), repli Quaternius.
        /// </summary>
        static void PlaceDock(Transform root, Island island, Vector3 outward, float size, float distanceFactor)
        {
            var dock = ArtLibrary.SpawnFirst(root, ArtLibrary.Tropical("Pier_02"), ArtLibrary.Dock);
            if (dock == null) return;

            // Même convention que le navire : l'axe long du modèle est ramené sur +x,
            // puis pointé vers le large (yaw monde : +x tourné de θ donne (cos θ, 0, -sin θ)).
            var bounds = ArtLibrary.MeasureBounds(dock);
            float baseYaw = bounds.size.z > bounds.size.x ? 90f : 0f;
            float outwardYaw = Mathf.Atan2(-outward.z, outward.x) * Mathf.Rad2Deg;
            dock.transform.rotation = Quaternion.Euler(0f, outwardYaw + baseYaw, 0f);
            ArtLibrary.NormalizeToSize(dock, size);
            dock.transform.position +=
                outward * (island.radius * distanceFactor) + Vector3.up * (SurfaceY * 0.4f);
        }

        /// <summary>Direction de l'île vers le point de départ : la façade qui accueille le joueur.</summary>
        static Vector3 ToSea(Island island)
        {
            var toSea = -island.position;
            toSea.y = 0f;
            return toSea.sqrMagnitude < 0.01f ? Vector3.right : toSea.normalized;
        }

        /// <summary>Yaw (degrés) d'un objet placé à cet angle pour qu'il regarde le centre de l'île.</summary>
        static float FaceCenterYaw(float angle)
            => Mathf.Atan2(-Mathf.Cos(angle), -Mathf.Sin(angle)) * Mathf.Rad2Deg;

        /// <summary>
        /// Pose un modèle sur l'île : <paramref name="angle"/> (radians) et
        /// <paramref name="distanceFactor"/> (fraction du rayon, au-delà de 1 = dans
        /// l'eau) donnent la position ; sans yaw, l'objet est simplement tourné selon
        /// son angle. Null si le modèle manque — l'île reste correcte.
        /// </summary>
        static GameObject Place(Transform root, Island island, string path, float size,
            float angle, float distanceFactor, float yaw = float.NaN,
            bool byHeight = false, float up = SurfaceY)
        {
            var model = ArtLibrary.SpawnQuiet(path, root);
            if (model == null) return null;

            model.transform.rotation =
                Quaternion.Euler(0f, float.IsNaN(yaw) ? angle * Mathf.Rad2Deg : yaw, 0f);
            if (byHeight) ArtLibrary.NormalizeToHeight(model, size);
            else ArtLibrary.NormalizeToSize(model, size);

            model.transform.position +=
                new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (island.radius * distanceFactor)
                + Vector3.up * up;
            return model;
        }
    }
}
