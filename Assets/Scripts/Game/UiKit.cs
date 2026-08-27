using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Devside.FishingIdle.Game
{
    /// <summary>
    /// Petits constructeurs uGUI pour bâtir toute l'UI par code : aucun câblage de scène,
    /// aucun prefab — le repo reste 100 % texte et l'UI se recrée à chaque Play.
    /// Le look « jeu mobile » vient de trois briques : sprite à coins arrondis généré par
    /// code (9-slice), ombres portées, et textes gras à contour sombre.
    /// </summary>
    public static class UiKit
    {
        static Font _font;
        static Sprite _rounded;
        static readonly Dictionary<string, Sprite> IconCache = new Dictionary<string, Sprite>();

        public static Font DefaultFont
            => _font != null ? _font : _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Sprite blanc à coins arrondis (9-slice), généré par code — la base de toute l'UI.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (_rounded != null) return _rounded;
                const int size = 64;
                const float radius = 22f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "RoundedRect" };
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = Mathf.Max(0f, Mathf.Max(radius - x, x - (size - 1 - radius)));
                        float dy = Mathf.Max(0f, Mathf.Max(radius - y, y - (size - 1 - radius)));
                        float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                tex.Apply();
                _rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
                    100f, 0, SpriteMeshType.FullRect, new Vector4(radius + 4, radius + 4, radius + 4, radius + 4));
                return _rounded;
            }
        }

        /// <summary>
        /// Icône de Resources/UI/Icons (générées par IA) ; null si absente — l'UI reste
        /// correcte sans (les emplacements d'icônes se masquent).
        /// </summary>
        public static Sprite Icon(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (IconCache.TryGetValue(name, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>("UI/Icons/" + name);
            Sprite sprite = null;
            if (tex != null)
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            IconCache[name] = sprite;
            return sprite;
        }

        static Sprite _circle;

        /// <summary>Sprite disque blanc antialiasé, généré par code (joystick, pastilles).</summary>
        public static Sprite Circle
        {
            get
            {
                if (_circle != null) return _circle;
                const int size = 64;
                const float radius = size / 2f - 1.5f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Circle" };
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - size / 2f + 0.5f;
                        float dy = y - size / 2f + 0.5f;
                        float alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                tex.Apply();
                _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return _circle;
            }
        }

        static Sprite _ring;

        /// <summary>Anneau blanc antialiasé, généré par code (contours de zones sur la carte).</summary>
        public static Sprite Ring
        {
            get
            {
                if (_ring != null) return _ring;
                const int size = 256;
                const float outer = size / 2f - 1.5f;
                const float inner = outer - 3f;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Ring" };
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x - size / 2f + 0.5f;
                        float dy = y - size / 2f + 0.5f;
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        float alpha = Mathf.Clamp01(outer - d + 0.5f) * Mathf.Clamp01(d - inner + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
                tex.Apply();
                _ring = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
                return _ring;
            }
        }

        /// <summary>Carte à coins arrondis avec ombre portée optionnelle.</summary>
        public static Image CreateCard(string name, Transform parent, Color fill, bool shadow = true)
        {
            var rt = CreateRect(name, parent);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = fill;
            if (shadow)
            {
                var effect = rt.gameObject.AddComponent<Shadow>();
                effect.effectColor = new Color(0f, 0f, 0f, 0.35f);
                effect.effectDistance = new Vector2(0f, -5f);
            }
            return image;
        }

        /// <summary>Ombre douce sous une carte : le relief qui décolle une ligne du fond.</summary>
        public static void AddSoftShadow(Graphic graphic, float distance = 3f, float alpha = 0.18f)
        {
            var shadow = graphic.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.04f, 0.12f, 0.18f, alpha);
            shadow.effectDistance = new Vector2(0f, -distance);
        }

        /// <summary>Contour sombre « cartoon » sur un texte.</summary>
        public static void AddOutline(Text text, float thickness = 1.6f)
        {
            var outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.02f, 0.07f, 0.12f, 0.9f);
            outline.effectDistance = new Vector2(thickness, -thickness);
        }

        /// <summary>Hauteur de la tranche d'un bouton : c'est elle qui donne le volume.</summary>
        const float ButtonDepth = 10f;

        /// <summary>
        /// Bouton de jeu mobile, en trois couches : une TRANCHE sombre en bas (le volume),
        /// une FACE colorée posée dessus, et un REFLET clair sur sa moitié haute (la
        /// lumière). Sous le doigt, la face descend jusqu'à recouvrir sa tranche —
        /// le bouton s'enfonce vraiment, c'est ce retour tactile qui fait la différence
        /// entre un jeu et une liste de liens.
        /// </summary>
        public static (Button button, Text label, RectTransform rect) CreateFancyButton(
            string name, Transform parent, Color fill, int fontSize, Sprite icon = null)
        {
            // Tranche : la même teinte, nettement assombrie et désaturée vers le sombre.
            var edge = CreateCard(name, parent, new Color(fill.r * 0.52f, fill.g * 0.52f, fill.b * 0.52f, fill.a));
            var face = CreateCard("Face", edge.transform, fill, shadow: false);
            Stretch(face.rectTransform, 0, 0, 0, ButtonDepth);

            var button = edge.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            // Pas de teinte au clic : c'est le DÉPLACEMENT qui signale l'appui (PressEffect),
            // un assombrissement en plus rendrait le bouton terne.
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.62f, 0.66f, 0.70f, 0.9f);
            colors.fadeDuration = 0.05f;
            button.colors = colors;

            // Reflet : une bande claire sur la moitié haute de la face. C'est ce qui
            // donne l'aspect « bonbon » des références, pour un seul Image de plus.
            var gloss = CreateCard("Gloss", face.transform, new Color(1f, 1f, 1f, 0.22f), shadow: false);
            gloss.raycastTarget = false;
            var glossRt = gloss.rectTransform;
            glossRt.anchorMin = new Vector2(0f, 0.5f);
            glossRt.anchorMax = new Vector2(1f, 1f);
            glossRt.offsetMin = new Vector2(8f, 0f);
            glossRt.offsetMax = new Vector2(-8f, -7f);

            var label = CreateText("Label", face.transform, fontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            AddOutline(label);
            Stretch(label.rectTransform);

            if (icon != null)
            {
                var iconImage = CreateRect("Icon", face.transform).gameObject.AddComponent<Image>();
                iconImage.sprite = icon;
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                var iconRt = iconImage.rectTransform;
                iconRt.anchorMin = new Vector2(0.5f, 1f);
                iconRt.anchorMax = new Vector2(0.5f, 1f);
                iconRt.pivot = new Vector2(0.5f, 1f);
                iconRt.anchoredPosition = new Vector2(0f, -10f);
                iconRt.sizeDelta = new Vector2(56f, 56f);
                label.alignment = TextAnchor.LowerCenter;
                label.rectTransform.offsetMin = new Vector2(0f, 10f);
            }

            var press = edge.gameObject.AddComponent<PressEffect>();
            press.face = face.rectTransform;
            press.button = button;
            press.depth = ButtonDepth - 2f;

            return (button, label, edge.rectTransform);
        }

        /// <summary>
        /// Pastille ronde colorée portant une icône — la vignette des jeux mobiles, qui
        /// donne du poids à une ligne de liste là où une icône nue se perd.
        /// </summary>
        public static Image CreateIconBadge(Transform parent, Sprite icon, Color tint, float size)
        {
            var badge = CreateRect("Badge", parent).gameObject.AddComponent<Image>();
            badge.sprite = Circle;
            badge.color = tint;
            badge.raycastTarget = false;
            badge.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            badge.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            badge.rectTransform.sizeDelta = new Vector2(size, size);

            var shine = CreateRect("Shine", badge.transform).gameObject.AddComponent<Image>();
            shine.sprite = Circle;
            shine.color = new Color(1f, 1f, 1f, 0.20f);
            shine.raycastTarget = false;
            shine.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            shine.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            shine.rectTransform.pivot = new Vector2(0.5f, 1f);
            shine.rectTransform.anchoredPosition = new Vector2(0f, -size * 0.08f);
            shine.rectTransform.sizeDelta = new Vector2(size * 0.66f, size * 0.34f);

            if (icon != null)
            {
                var image = CreateRect("Icon", badge.transform).gameObject.AddComponent<Image>();
                image.sprite = icon;
                image.preserveAspect = true;
                image.raycastTarget = false;
                Stretch(image.rectTransform, size * 0.16f, size * 0.16f, size * 0.16f, size * 0.16f);
            }
            return badge;
        }

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
