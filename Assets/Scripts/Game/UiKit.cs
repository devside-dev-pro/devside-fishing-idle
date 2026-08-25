using UnityEngine;
using UnityEngine.UI;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Petits constructeurs uGUI pour bâtir toute l'UI par code : aucun câblage de scène,
    /// aucun prefab — le repo reste 100 % texte et l'UI se recrée à chaque Play.
    /// </summary>
    public static class UiKit
    {
        static Font _font;

        public static Font DefaultFont
            => _font != null ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static Image CreatePanel(string name, Transform parent, Color color)
        {
            var rt = CreateRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text CreateText(string name, Transform parent, int size, Color color,
            TextAnchor alignment = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var rt = CreateRect(name, parent);
            var text = rt.gameObject.AddComponent<Text>();
            text.font = DefaultFont;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.fontStyle = style;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static (Button button, Text label) CreateButton(string name, Transform parent,
            Color background, int fontSize)
        {
            var image = CreatePanel(name, parent, background);
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.disabledColor = new Color(1, 1, 1, 0.35f);
            button.colors = colors;
            var label = CreateText("Label", image.transform, fontSize, Color.white, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            return (button, label);
        }

        /// <summary>Étire le rect sur toute la surface de son parent (avec marges optionnelles).</summary>
        public static void Stretch(RectTransform rt, float left = 0, float right = 0, float top = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        /// <summary>Bande horizontale collée en haut du parent.</summary>
        public static void AnchorTop(RectTransform rt, float top, float height, float sideInset = 0)
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(sideInset, -(top + height));
            rt.offsetMax = new Vector2(-sideInset, -top);
        }

        /// <summary>Bande horizontale collée en bas du parent.</summary>
        public static void AnchorBottom(RectTransform rt, float bottom, float height, float sideInset = 0)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(sideInset, bottom);
            rt.offsetMax = new Vector2(-sideInset, bottom + height);
        }

        /// <summary>Zone qui s'étire verticalement entre une marge haute et une marge basse.</summary>
        public static void AnchorVerticalSpan(RectTransform rt, float top, float bottom, float sideInset = 0)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(sideInset, bottom);
            rt.offsetMax = new Vector2(-sideInset, -top);
        }

        /// <summary>Liste scrollable verticale ; renvoie le conteneur où ajouter les lignes.</summary>
        public static RectTransform CreateScrollList(string name, Transform parent, Color background)
        {
            var viewport = CreatePanel(name, parent, background);
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();

            var content = CreateRect("Content", viewport.transform);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);

            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.spacing = 14;
            layout.padding = new RectOffset(20, 20, 20, 20);

            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = content;
            scroll.viewport = (RectTransform)viewport.transform;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30;
            return content;
        }
    }
}
