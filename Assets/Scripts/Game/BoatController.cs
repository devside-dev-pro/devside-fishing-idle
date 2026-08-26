using Devside.FishingIdle.Core;
using UnityEngine;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Pilotage du bateau au joystick. Porte le transform racine (position + cap) sous
    /// lequel BoatView accroche la coque et ses animations locales (roulis, tangage).
    /// Écrit state.currentZone d'après la position dans le monde (la profondeur est de
    /// la géographie) et borne le rayon navigable au niveau de coque
    /// (Catching.MaxNavigableZone) — au-delà, message « coque trop faible ».
    /// </summary>
    public class BoatController : MonoBehaviour
    {
        public static BoatController Instance { get; private set; }

        /// <summary>Racine monde du bateau : position + cap. La proue est son +x local.</summary>
        public Transform Root { get; private set; }

        const float BaseSpeed = 4f;
        const float SpeedPerZone = 1.1f;
        const float TurnDegreesPerSecond = 200f;

        float _lastBlockedMessage = -99f;

        void Awake()
        {
            Instance = this;
            Root = new GameObject("BoatRoot").transform;
        }

        void Update()
        {
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.State == null) return;
            var state = boot.State;
            int maxZone = Catching.MaxNavigableZone(boot.Config, state);

            var input = VirtualJoystick.Direction;
            if (input.sqrMagnitude > 0.002f)
                Sail(input, maxZone);

            int zone = WorldMap.ZoneAt(Root.position);
            if (zone != state.currentZone)
            {
                bool deeper = zone > state.currentZone;
                state.currentZone = zone;
                if (deeper && GameUi.Instance != null)
                    GameUi.Instance.ShowBanner($"{GameTheme.ZoneReachedPrefix} — {GameTheme.DepthLabel} {zone}");
            }

            if (BoatView.Instance != null) BoatView.Instance.FollowBoat(Root);
        }

        void Sail(Vector2 input, int maxZone)
        {
            // Écran → monde : la caméra (yaw 90°) fait du haut d'écran +x et de la droite -z.
            var direction = new Vector3(input.y, 0f, -input.x);
            float throttle = Mathf.Min(1f, direction.magnitude);
            direction.Normalize();

            float speed = (BaseSpeed + SpeedPerZone * maxZone) * throttle;
            var next = Root.position + direction * (speed * Time.deltaTime);

            // La coque borne le rayon navigable — au-delà, on bute sur la frontière.
            float allowed = WorldMap.AllowedRadius(maxZone);
            var flat = new Vector2(next.x, next.z);
            if (flat.magnitude > allowed)
            {
                flat = flat.normalized * allowed;
                next = new Vector3(flat.x, 0f, flat.y);
                if (Time.time - _lastBlockedMessage > 3f && GameUi.Instance != null)
                {
                    GameUi.Instance.ShowBanner(GameTheme.HullTooWeak);
                    _lastBlockedMessage = Time.time;
                }
            }

            next = WorldMap.PushOutOfIslands(next);
            next.y = 0f;
            Root.position = next;

            // Cap : +x tourné de θ autour de Y donne (cos θ, 0, -sin θ) → θ = atan2(-z, x).
            float targetYaw = Mathf.Atan2(-direction.z, direction.x) * Mathf.Rad2Deg;
            Root.rotation = Quaternion.RotateTowards(
                Root.rotation, Quaternion.Euler(0f, targetYaw, 0f), TurnDegreesPerSecond * Time.deltaTime);
        }
    }
}
