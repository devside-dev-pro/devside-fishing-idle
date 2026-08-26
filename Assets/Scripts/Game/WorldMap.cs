using System.Collections.Generic;
using UnityEngine;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// L'archipel : des îles à positions fixes et des anneaux de zones concentriques
    /// autour du point de départ. La zone où se trouve le bateau devient
    /// state.currentZone (la profondeur est de la géographie — voir Core/Catching) ;
    /// la coque borne le rayon navigable (AllowedRadius). Décor bâti par code via
    /// ArtLibrary, avec fallback primitive si un modèle manque.
    /// </summary>
    public class WorldMap : MonoBehaviour
    {
        public class Island
        {
            public string id;
            public Vector3 position;

            /// <summary>Rayon interdit au bateau (la plage) — le décor reste plus petit.</summary>
            public float radius;
            public int zone;
            public bool hasMerchant;
        }

        /// <summary>Frontières des zones (rayons) ; au-delà de la dernière = zone 3.</summary>
        static readonly float[] ZoneRadii = { 35f, 85f, 145f };

        static readonly Island[] Islands =
        {
            new Island { id = "island_port", position = new Vector3(7f, 0f, -2.5f), radius = 3.6f, zone = 0, hasMerchant = true },
            new Island { id = "island_lagoon", position = new Vector3(55f, 0f, -18f), radius = 5f, zone = 1 },
            new Island { id = "island_mist", position = new Vector3(115f, 0f, 25f), radius = 6f, zone = 2 },
            new Island { id = "island_abyss", position = new Vector3(175f, 0f, -35f), radius = 7f, zone = 3 },
        };

        static readonly Color FallbackSand = new Color(0.83f, 0.72f, 0.5f);

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

        /// <summary>Repousse une position hors des plages : le bateau contourne les îles.</summary>
        public static Vector3 PushOutOfIslands(Vector3 position)
        {
            for (int i = 0; i < Islands.Length; i++)
            {
                var island = Islands[i];
                var offset = new Vector2(position.x - island.position.x, position.z - island.position.z);
                float distance = offset.magnitude;
                if (distance >= island.radius) continue;
                offset = distance < 0.001f ? Vector2.right : offset / distance;
                position.x = island.position.x + offset.x * island.radius;
                position.z = island.position.z + offset.y * island.radius;
            }
            return position;
        }

        void BuildIsland(Island island, int index)
        {
            var root = new GameObject(island.id).transform;
            root.SetParent(transform, false);
            root.position = island.position;

            var cliff = ArtLibrary.Spawn(ArtLibrary.Cliffs[index % ArtLibrary.Cliffs.Length], root);
            if (cliff != null)
            {
                ArtLibrary.NormalizeToSize(cliff, island.radius * 1.1f, 0.35f);
            }
            else
            {
                var mound = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
                mound.SetParent(root, false);
                mound.localScale = new Vector3(island.radius * 1.1f, 0.35f, island.radius * 1.1f);
                var renderer = mound.GetComponent<Renderer>();
                var lit = Shader.Find("Universal Render Pipeline/Lit");
                renderer.material = new Material(lit != null ? lit : Shader.Find("Standard")) { color = FallbackSand };
            }

            // Palmiers et rochers, placés en cercle déterministe (varie d'île en île).
            int palmCount = 1 + index % ArtLibrary.Palms.Length;
            for (int p = 0; p < palmCount; p++)
            {
                var palm = ArtLibrary.Spawn(ArtLibrary.Palms[(index + p) % ArtLibrary.Palms.Length], root);
                if (palm == null) continue;
                ArtLibrary.NormalizeToHeight(palm, 1.5f + 0.3f * p);
                float angle = index * 2.1f + p * 2.4f;
                palm.transform.position +=
                    new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (island.radius * 0.3f) + Vector3.up * 0.3f;
            }

            for (int r = 0; r < 3; r++)
            {
                var rock = ArtLibrary.Spawn(ArtLibrary.Rocks[(index * 2 + r) % ArtLibrary.Rocks.Length], root);
                if (rock == null) continue;
                ArtLibrary.NormalizeToSize(rock, 0.7f, 0.15f);
                float angle = index * 1.7f + r * 2.1f + 0.9f;
                rock.transform.position +=
                    new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * (island.radius * 0.75f);
            }

            if (island.hasMerchant) BuildMerchantOutpost(island, root);
        }

        /// <summary>Comptoir du marchand : ponton tourné vers le large, cabane, et le marchand qui attend.</summary>
        static void BuildMerchantOutpost(Island island, Transform root)
        {
            // Direction de l'île vers le point de départ : le ponton accueille le joueur.
            var toSea = -island.position;
            toSea.y = 0f;
            toSea = toSea.sqrMagnitude < 0.01f ? Vector3.right : toSea.normalized;

            var dock = ArtLibrary.Spawn(ArtLibrary.Dock, root);
            if (dock != null)
            {
                // Même convention que le navire : l'axe long du modèle est ramené sur +x,
                // puis pointé vers le large (yaw monde : +x tourné de θ donne (cos θ, 0, -sin θ)).
                var bounds = ArtLibrary.MeasureBounds(dock);
                float baseYaw = bounds.size.z > bounds.size.x ? 90f : 0f;
                float seaYaw = Mathf.Atan2(-toSea.z, toSea.x) * Mathf.Rad2Deg;
                dock.transform.rotation = Quaternion.Euler(0f, seaYaw + baseYaw, 0f);
                ArtLibrary.NormalizeToSize(dock, 2.6f);
                dock.transform.position += toSea * (island.radius * 0.8f) + Vector3.up * 0.05f;
            }

            var house = ArtLibrary.Spawn(ArtLibrary.House, root);
            if (house != null)
            {
                house.transform.rotation = Quaternion.LookRotation(toSea);
                ArtLibrary.NormalizeToSize(house, 1.9f);
                house.transform.position += -toSea * (island.radius * 0.3f) + Vector3.up * 0.35f;
            }

            var merchant = ArtLibrary.Spawn(ArtLibrary.Merchant, root);
            if (merchant != null)
            {
                merchant.transform.rotation = Quaternion.LookRotation(toSea);
                ArtLibrary.NormalizeToHeight(merchant, 0.85f);
                merchant.transform.position += toSea * (island.radius * 0.5f) + Vector3.up * 0.25f;
            }
        }
    }
}
