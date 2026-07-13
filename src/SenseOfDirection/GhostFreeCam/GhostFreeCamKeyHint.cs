using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.GhostFreeCam
{
    /// <summary>
    /// "&lt;key badge&gt; to go into/leave free-cam mode" hint, shown while
    /// the local player is a ghost and the host has ghost free-cam enabled.
    /// Vanilla's own ghost panel ("you are a ghost / get resurrected at the
    /// statue!") has no mention of this mod's toggle key at all, so players
    /// have no in-UI way to discover it.
    ///
    /// Badge background is a small procedurally-drawn rounded square (same
    /// feathered-alpha technique as <see cref="Compass.CompassIcons"/>), and
    /// both the badge digit/letter and the label text use
    /// <see cref="Labels.NativeAssets.Font"/> - the same chunky rounded font
    /// vanilla's own "you are a ghost" text uses, with its own plain
    /// (non-outline) material - rather than TMP's default font, so this
    /// reads as part of that panel.
    ///
    /// Laid out via a <see cref="HorizontalLayoutGroup"/> +
    /// <see cref="ContentSizeFitter"/> on the root (pivot centered
    /// horizontally) rather than manually-placed children, so the whole
    /// group re-centers itself and never clips regardless of how long the
    /// "go into"/"leave" label text is, and never shifts vertically between
    /// the two states.
    ///
    /// Parented under <see cref="Indicators.IndicatorManager"/>'s existing
    /// full-screen overlay canvas and positioned independently, same as
    /// <see cref="GhostFreeCamCrosshair"/>, rather than trying to slot into
    /// vanilla's own <c>GUIManager.spectatingObject</c> hierarchy whose
    /// internal layout/anchoring isn't known from decompiled code alone.
    /// </summary>
    public static class GhostFreeCamKeyHint
    {
        private const float BadgeSize = 44f;

        /// <summary>Badge fill - exact color sampled from the reference key-badge art the maintainer supplied.</summary>
        private static readonly Color32 BadgeFillColor = new Color32(222, 217, 193, 255);

        /// <summary>Badge outline/letter - exact color sampled from the same reference art.</summary>
        private static readonly Color32 BadgeOutlineColor = new Color32(116, 111, 91, 255);

        private static RectTransform _root;
        private static TMP_Text _badgeText;
        private static Image _badgeImage;
        private static LayoutElement _badgeLayoutElement;
        private static TMP_Text _labelText;
        private static TMP_Text _labelShadowText;
        private static LayoutElement _labelLayoutElement;
        private static KeyCode _lastKey = (KeyCode)(-1);

        public static void Hide()
        {
            if (_root == null)
            {
                return;
            }

            _root.gameObject.SetActive(false);
        }

        public static void SetState(bool freeCamActive, KeyCode toggleKey)
        {
            EnsureCreated();
            _root.gameObject.SetActive(true);
            ApplyNativeAssetsIfReady();

            if (toggleKey != _lastKey)
            {
                _lastKey = toggleKey;
                _badgeText.text = KeyCodeToBadgeLabel(toggleKey);

                // Longer labels (e.g. "Alt", "Shift") need a wider badge, not
                // a squeezed-in single-letter-width one - width grows from
                // BadgeSize as a floor, height always stays fixed. Rebakes
                // the background at the new aspect ratio (rather than
                // 9-slicing a fixed texture) so the rounded corners are
                // always exactly right regardless of size - Image.Type
                // .Sliced turned out unreliable here, auto-shrinking the
                // border into a distorted blob at some sizes.
                float padding = BadgeSize * 0.55f;
                float width = Mathf.Max(BadgeSize, _badgeText.GetPreferredValues().x + padding);
                _badgeLayoutElement.preferredWidth = width;
                _badgeImage.sprite = BuildBadgeSprite(width / BadgeSize);
            }

            string text = freeCamActive ? "to leave free-cam mode" : "to go into free-cam mode";
            _labelText.text = text;
            _labelShadowText.text = text;

            // The label's own TMP text is nested inside a plain child
            // GameObject now (see EnsureCreated) rather than living directly
            // on the LayoutElement-bearing object, so HorizontalLayoutGroup
            // can no longer read its preferred width automatically - drive
            // it manually instead.
            _labelLayoutElement.preferredWidth = _labelText.GetPreferredValues().x + 2f;
        }

        private static void EnsureCreated()
        {
            if (_root != null)
            {
                return;
            }

            var go = new GameObject(
                "GhostFreeCamKeyHint", typeof(RectTransform),
                typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            _root = (RectTransform)go.transform;
            _root.SetParent(Indicators.IndicatorManager.Instance.CanvasTransform, false);
            _root.anchorMin = new Vector2(0.5f, 0f);
            _root.anchorMax = new Vector2(0.5f, 0f);
            _root.pivot = new Vector2(0.5f, 0f);
            _root.anchoredPosition = new Vector2(0f, 130f);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.spacing = 8f;

            var fitter = go.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var badgeGo = new GameObject("Badge", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            var badgeRect = (RectTransform)badgeGo.transform;
            badgeRect.SetParent(_root, false);
            _badgeLayoutElement = badgeGo.GetComponent<LayoutElement>();
            _badgeLayoutElement.preferredWidth = BadgeSize;
            _badgeLayoutElement.preferredHeight = BadgeSize;

            _badgeImage = badgeGo.GetComponent<Image>();
            _badgeImage.sprite = BuildBadgeSprite(1f);
            _badgeImage.type = Image.Type.Simple;
            // Colors are baked into the sprite itself (fill/outline/shadow
            // all differ), so no tint here - a single Image.color multiply
            // can't recolor those independently.
            _badgeImage.color = Color.white;
            _badgeImage.raycastTarget = false;

            var badgeTextGo = new GameObject("Text", typeof(RectTransform));
            var badgeTextRect = (RectTransform)badgeTextGo.transform;
            badgeTextRect.SetParent(badgeRect, false);
            badgeTextRect.anchorMin = Vector2.zero;
            badgeTextRect.anchorMax = Vector2.one;
            // Nudged up a couple px - this font's glyphs otherwise sit low
            // within TMP's own vertically-centered line height and visually
            // clip the badge's bottom edge.
            badgeTextRect.offsetMin = new Vector2(0f, 3f);
            badgeTextRect.offsetMax = new Vector2(0f, 3f);
            _badgeText = badgeTextGo.AddComponent<TextMeshProUGUI>();
            _badgeText.fontSize = 26f;
            _badgeText.alignment = TextAlignmentOptions.Center;
            // Same exact color as the badge's own outline, matching the
            // reference art (letter and outline are the same tone there).
            _badgeText.color = BadgeOutlineColor;
            _badgeText.raycastTarget = false;
            _badgeText.enableWordWrapping = false;

            // UnityEngine.UI.Shadow (a BaseMeshEffect) doesn't apply to TMP's
            // own mesh generation, so the drop shadow is a second, plain
            // duplicate text behind the real one, offset a couple px
            // south-east - the same trick used everywhere TMP needs a
            // shadow without a custom SDF material.
            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(LayoutElement));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(_root, false);
            _labelLayoutElement = labelGo.GetComponent<LayoutElement>();
            _labelLayoutElement.minHeight = BadgeSize;

            var labelShadowGo = new GameObject("Shadow", typeof(RectTransform));
            var labelShadowRect = (RectTransform)labelShadowGo.transform;
            labelShadowRect.SetParent(labelRect, false);
            labelShadowRect.anchorMin = Vector2.zero;
            labelShadowRect.anchorMax = Vector2.one;
            labelShadowRect.offsetMin = new Vector2(1.5f, -1.5f);
            labelShadowRect.offsetMax = new Vector2(1.5f, -1.5f);
            _labelShadowText = labelShadowGo.AddComponent<TextMeshProUGUI>();
            _labelShadowText.fontSize = 26f;
            _labelShadowText.alignment = TextAlignmentOptions.MidlineLeft;
            _labelShadowText.color = new Color(0f, 0f, 0f, 0.5f);
            _labelShadowText.raycastTarget = false;
            _labelShadowText.enableWordWrapping = false;

            var labelTextGo = new GameObject("Text", typeof(RectTransform));
            var labelTextRect = (RectTransform)labelTextGo.transform;
            labelTextRect.SetParent(labelRect, false);
            labelTextRect.anchorMin = Vector2.zero;
            labelTextRect.anchorMax = Vector2.one;
            labelTextRect.offsetMin = Vector2.zero;
            labelTextRect.offsetMax = Vector2.zero;
            _labelText = labelTextGo.AddComponent<TextMeshProUGUI>();
            _labelText.fontSize = 26f;
            _labelText.alignment = TextAlignmentOptions.MidlineLeft;
            _labelText.raycastTarget = false;
            _labelText.enableWordWrapping = false;

            ApplyNativeAssetsIfReady();

            go.SetActive(false);
        }

        private static void ApplyNativeAssetsIfReady()
        {
            Labels.NativeAssets.TryFindAll();

            TMP_FontAsset font = Labels.NativeAssets.Font;
            if (font == null)
            {
                return;
            }

            // Deliberately *not* NativeAssets.OutlineMaterial here (unlike
            // PlayerLabel/CompassMarkerWidget) - vanilla's own "you are a
            // ghost" text this hint sits next to has no outline at all, so
            // this just takes the font asset's own plain default material.
            if (_badgeText.font != font)
            {
                _badgeText.font = font;
                _labelText.font = font;
                _labelShadowText.font = font;
            }

            // Same exact foreground color as vanilla's own "you are a ghost"
            // text - resolved lazily same as Font/OutlineMaterial above.
            _labelText.color = Labels.NativeAssets.DefaultTextColor;
        }

        /// <summary>
        /// Bakes the actual final fill/outline/shadow colors directly into
        /// the texture (rather than tinting a single-color mask via
        /// <see cref="Image.color"/>) since this badge needs three distinct
        /// colors composited together - a single tint multiply can't produce
        /// that. Texture row 0 is the bottom of the sprite (Unity's own
        /// <c>SetPixels32</c> convention), so the shadow center is offset
        /// toward smaller x/y to land south-west, per the reference art.
        ///
        /// Rebaked at the exact target <paramref name="aspectWidthOverHeight"/>
        /// (badge display width / <see cref="BadgeSize"/>) rather than 9-sliced
        /// from one fixed texture - <c>Image.Type.Sliced</c> turned out
        /// unreliable here (Unity auto-shrinks/distorts the border at some
        /// sizes), and rebaking only happens on the rare keybind change, not
        /// per-frame, so the extra cost is negligible.
        /// </summary>
        private static Sprite BuildBadgeSprite(float aspectWidthOverHeight)
        {
            const int texHeight = 64;
            int texWidth = Mathf.Max(texHeight, Mathf.RoundToInt(texHeight * aspectWidthOverHeight));
            var tex = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            float centerX = texWidth * 0.5f;
            float centerY = texHeight * 0.5f;
            float halfExtentY = texHeight * 0.30f;
            float halfExtentX = halfExtentY + (texWidth - texHeight) * 0.5f;
            float cornerRadius = halfExtentY * 0.55f;
            const float outlineThickness = 3f;
            const float feather = 1.3f;

            const float shadowAngleDegrees = 30f;
            float shadowOffset = 3f;
            float shadowDx = shadowOffset * Mathf.Cos(shadowAngleDegrees * Mathf.Deg2Rad);
            float shadowDy = shadowOffset * Mathf.Sin(shadowAngleDegrees * Mathf.Deg2Rad);
            const float shadowFeather = 3f;
            const float shadowMaxAlpha = 0.4f;

            var pixels = new Color32[texWidth * texHeight];
            for (int y = 0; y < texHeight; y++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float shadowDist = RoundedBoxSdf(px, py, centerX - shadowDx, centerY - shadowDy, halfExtentX, halfExtentY, cornerRadius);
                    float shadowAlpha = shadowMaxAlpha * Mathf.Clamp01(0.5f - shadowDist / shadowFeather);
                    Color result = new Color(0f, 0f, 0f, shadowAlpha);

                    float outlineDist = RoundedBoxSdf(px, py, centerX, centerY, halfExtentX, halfExtentY, cornerRadius);
                    float outlineAlpha = Mathf.Clamp01(0.5f - outlineDist / feather);
                    result = Over(new Color(BadgeOutlineColor.r / 255f, BadgeOutlineColor.g / 255f, BadgeOutlineColor.b / 255f, outlineAlpha), result);

                    float fillDist = RoundedBoxSdf(px, py, centerX, centerY, halfExtentX - outlineThickness, halfExtentY - outlineThickness, cornerRadius - outlineThickness);
                    float fillAlpha = Mathf.Clamp01(0.5f - fillDist / feather);
                    result = Over(new Color(BadgeFillColor.r / 255f, BadgeFillColor.g / 255f, BadgeFillColor.b / 255f, fillAlpha), result);

                    pixels[y * texWidth + x] = result;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, texWidth, texHeight), new Vector2(0.5f, 0.5f), texHeight);
        }

        /// <summary>Signed distance from (px, py) to a rounded box centered at (cx, cy) - negative inside, positive outside, zero at the boundary.</summary>
        private static float RoundedBoxSdf(float px, float py, float cx, float cy, float halfWidth, float halfHeight, float radius)
        {
            float qx = Mathf.Abs(px - cx) - (halfWidth - radius);
            float qy = Mathf.Abs(py - cy) - (halfHeight - radius);
            float outsideX = Mathf.Max(qx, 0f);
            float outsideY = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(outsideX * outsideX + outsideY * outsideY) + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        /// <summary>Standard "src over dst" alpha compositing.</summary>
        private static Color Over(Color src, Color dst)
        {
            float outAlpha = src.a + dst.a * (1f - src.a);
            if (outAlpha <= 0.0001f)
            {
                return new Color(0f, 0f, 0f, 0f);
            }

            Color result = (src * src.a + dst * dst.a * (1f - src.a)) / outAlpha;
            result.a = outAlpha;
            return result;
        }

        /// <summary>Short, readable label for the badge - single glyph for letters/digits, otherwise a compacted <see cref="KeyCode"/> name (e.g. <c>LeftShift</c> -&gt; "Shift").</summary>
        private static string KeyCodeToBadgeLabel(KeyCode key)
        {
            if (key >= KeyCode.A && key <= KeyCode.Z)
            {
                return key.ToString();
            }
            if (key >= KeyCode.Alpha0 && key <= KeyCode.Alpha9)
            {
                return key.ToString().Substring("Alpha".Length);
            }

            switch (key)
            {
                case KeyCode.LeftShift:
                case KeyCode.RightShift:
                    return "Shift";
                case KeyCode.LeftControl:
                case KeyCode.RightControl:
                    return "Ctrl";
                case KeyCode.LeftAlt:
                case KeyCode.RightAlt:
                    return "Alt";
                case KeyCode.Space:
                    return "Space";
                case KeyCode.Return:
                    return "Enter";
                case KeyCode.Escape:
                    return "Esc";
                case KeyCode.Tab:
                    return "Tab";
                default:
                    return key.ToString();
            }
        }
    }
}
