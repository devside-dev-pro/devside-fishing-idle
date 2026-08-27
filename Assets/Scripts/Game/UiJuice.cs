using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Le « juice » de l'interface : ce qui sépare une liste de boutons d'un jeu mobile.
    /// Trois briques, toutes en uGUI pur (aucun paquet externe) :
    ///
    /// - <see cref="PressEffect"/> : un bouton s'ENFONCE sous le doigt. C'est le retour
    ///   tactile le plus important d'un jeu mobile ; sans lui, tout paraît plat.
    /// - <see cref="UiTween"/> : de petites animations (apparition, rebond, chiffres qui
    ///   montent) lancées à la demande, sans dépendance ni allocation permanente.
    /// - <see cref="PanelAnimator"/> : les panneaux s'ouvrent et se ferment au lieu
    ///   d'apparaître d'un coup.
    ///
    /// Toutes les durées sont courtes (0,08 à 0,25 s) : une animation qu'on remarque est
    /// une animation trop lente — sur mobile elle doit se sentir, pas s'attendre.
    /// </summary>
    public static class UiTween
    {
        static MonoBehaviour _runner;

        /// <summary>Hôte des coroutines : un objet caché créé à la première animation.</summary>
        static MonoBehaviour Runner
        {
            get
            {
                if (_runner == null)
                {
                    var go = new GameObject("UiTweenRunner") { hideFlags = HideFlags.HideInHierarchy };
                    _runner = go.AddComponent<TweenRunner>();
                }
                return _runner;
            }
        }

        class TweenRunner : MonoBehaviour { }

        /// <summary>Adoucissement standard : démarrage franc, arrivée en douceur.</summary>
        public static float EaseOut(float t) => 1f - (1f - t) * (1f - t);

        /// <summary>Adoucissement avec léger dépassement — c'est lui qui donne le « pop ».</summary>
        public static float EaseBack(float t)
        {
            const float overshoot = 1.7f;
            float inv = t - 1f;
            return inv * inv * ((overshoot + 1f) * inv + overshoot) + 1f;
        }

        /// <summary>Fait apparaître un élément en grossissant légèrement (ouverture de panneau, récompense).</summary>
        public static void Pop(RectTransform target, float from = 0.88f, float duration = 0.22f)
        {
            if (target == null) return;
            Runner.StartCoroutine(PopRoutine(target, from, duration));
        }

        static IEnumerator PopRoutine(RectTransform target, float from, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float scale = Mathf.Lerp(from, 1f, EaseBack(Mathf.Clamp01(elapsed / duration)));
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        /// <summary>Coup de projecteur sur un élément qui vient de changer (gain de monnaie).</summary>
        public static void Punch(RectTransform target, float strength = 0.16f, float duration = 0.24f)
        {
            if (target == null) return;
            Runner.StartCoroutine(PunchRoutine(target, strength, duration));
        }

        static IEnumerator PunchRoutine(RectTransform target, float strength, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Une seule oscillation amortie : gonfle puis revient.
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * strength;
                target.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }

        /// <summary>Fondu d'un CanvasGroup (ouverture/fermeture de panneau).</summary>
        public static void Fade(CanvasGroup group, float from, float to, float duration)
        {
            if (group == null) return;
            Runner.StartCoroutine(FadeRoutine(group, from, to, duration));
        }

        static IEnumerator FadeRoutine(CanvasGroup group, float from, float to, float duration)
        {
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < duration)
            {
                if (group == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, EaseOut(Mathf.Clamp01(elapsed / duration)));
                yield return null;
            }
            if (group != null) group.alpha = to;
        }
    }

    /// <summary>
    /// Le bouton s'enfonce sous le doigt et remonte au relâchement : la face descend
    /// jusqu'à masquer sa tranche, exactement comme une touche mécanique. Posé par
    /// UiKit.CreateFancyButton sur la face du bouton ; inerte si le bouton est désactivé.
    /// </summary>
    public class PressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>Face colorée du bouton (celle qui descend), et sa position au repos.</summary>
        public RectTransform face;
        public Button button;

        float _restBottom;
        float _restTop;
        bool _pressed;

        /// <summary>Profondeur d'enfoncement : la hauteur de la tranche, moins un cheveu.</summary>
        public float depth = 6f;

        void Awake()
        {
            if (face == null) face = transform as RectTransform;
            _restBottom = face.offsetMin.y;
            _restTop = face.offsetMax.y;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            _pressed = true;
            Apply(-depth);
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        /// <summary>Le doigt a glissé hors du bouton : il remonte, comme sur un vrai clavier.</summary>
        public void OnPointerExit(PointerEventData eventData) => Release();

        void OnDisable() => Release();

        void Update()
        {
            // Ceinture et bretelles : si le relâchement s'est perdu (panneau ouvert
            // par-dessus, doigt sorti de l'écran), un bouton resterait enfoncé pour
            // toujours. Dès qu'aucun contact n'est actif, il remonte.
            if (_pressed && !Input.GetMouseButton(0) && Input.touchCount == 0) Release();
        }

        void Release()
        {
            if (!_pressed) return;
            _pressed = false;
            Apply(0f);
        }

        void Apply(float offset)
        {
            if (face == null) return;
            face.offsetMin = new Vector2(face.offsetMin.x, _restBottom + offset);
            face.offsetMax = new Vector2(face.offsetMax.x, _restTop + offset);
        }
    }

    /// <summary>
    /// Ouverture et fermeture animées d'un panneau : il monte et grossit légèrement en
    /// apparaissant. À la fermeture on coupe net — attendre pour fermer irrite,
    /// attendre pour ouvrir donne de la matière.
    /// </summary>
    public class PanelAnimator : MonoBehaviour
    {
        CanvasGroup _group;
        RectTransform _rect;

        void Awake()
        {
            _rect = transform as RectTransform;
            _group = gameObject.GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
        }

        void OnEnable()
        {
            if (_rect == null) return;
            UiTween.Pop(_rect, 0.92f, 0.2f);
            UiTween.Fade(_group, 0f, 1f, 0.16f);
        }
    }
}
