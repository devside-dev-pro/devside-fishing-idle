using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Joystick virtuel de navigation (coin bas-gauche, au-dessus de la barre d'onglets).
    /// Deux disques semi-transparents générés par code (UiKit.Circle) — aucun asset.
    /// BoatController lit la direction via la propriété statique ; (0,0) au repos.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        /// <summary>Direction écran normalisée (magnitude ≤ 1) ; Vector2.zero au repos.</summary>
        public static Vector2 Direction { get; private set; }

        const float Radius = 150f;

        RectTransform _knob;

        public static VirtualJoystick Create(Transform canvas)
        {
            var baseImage = UiKit.CreateRect("Joystick", canvas).gameObject.AddComponent<Image>();
            baseImage.sprite = UiKit.Circle;
            baseImage.color = new Color(1f, 1f, 1f, 0.13f);
            // Seul le disque capte les taps : les coins transparents laissent passer
            // (pêche au tap et boutons voisins restent accessibles).
            baseImage.alphaHitTestMinimumThreshold = 0.5f;
            var rt = baseImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(240f, 490f);
            rt.sizeDelta = Vector2.one * ((Radius + 40f) * 2f);

            var knob = UiKit.CreateRect("Knob", baseImage.transform).gameObject.AddComponent<Image>();
            knob.sprite = UiKit.Circle;
            knob.color = new Color(1f, 1f, 1f, 0.38f);
            knob.raycastTarget = false;
            knob.rectTransform.sizeDelta = new Vector2(150f, 150f);

            var joystick = baseImage.gameObject.AddComponent<VirtualJoystick>();
            joystick._knob = knob.rectTransform;
            return joystick;
        }

        public void OnPointerDown(PointerEventData eventData) => OnDrag(eventData);

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)transform, eventData.position, eventData.pressEventCamera, out var local);
            var clamped = Vector2.ClampMagnitude(local, Radius);
            _knob.anchoredPosition = clamped;
            Direction = clamped / Radius;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        void OnDisable() => Release();

        void Release()
        {
            Direction = Vector2.zero;
            if (_knob != null) _knob.anchoredPosition = Vector2.zero;
        }
    }
}
