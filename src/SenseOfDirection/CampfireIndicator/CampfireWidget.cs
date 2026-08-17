using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.CampfireIndicator
{
    /// <summary>
    /// The on-screen campfire widget: the game's own HUD campfire icon
    /// (<see cref="NativeAssets.CampfireIconSprite"/>), with a black outline
    /// to match the look of the host crown badge on player labels, plus an
    /// optional distance sub-line in the native font. No off-screen arrow -
    /// that's reserved for Mechanic 2's ping indicator per maintainer
    /// direction (same reservation <see cref="Labels.PlayerLabel"/> already
    /// follows); this widget just clamps quietly to the edge like a player
    /// label does. Built under <see cref="IndicatorManager"/>'s shared
    /// canvas, registered as its own <see cref="IndicatorAnchor"/> so Phase
    /// 2's edge-clamping applies automatically.
    /// </summary>
    public class CampfireWidget
    {
        /// <summary>
        /// The HUD campfire sprite has no outline baked into its art (unlike
        /// the host crown, which is already styled that way natively), so
        /// the border is faked the classic UI way: eight copies of the same
        /// sprite, tinted solid black, drawn behind the real icon and offset
        /// by one pixel in every direction - the sprite's own alpha shape
        /// does the rest, giving a stroke that follows its silhouette
        /// instead of a plain offset rectangle (which is all Unity's built-in
        /// `UI.Outline` component would give for a `Simple`-mode `Image`,
        /// since that component duplicates the quad's four vertices, not the
        /// sprite's shape).
        /// </summary>
        private static readonly Vector2[] OutlineOffsets =
        {
            new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f),
            new Vector2(-1f, 0f),                         new Vector2(1f, 0f),
            new Vector2(-1f, 1f),  new Vector2(0f, 1f),  new Vector2(1f, 1f),
        };

        /// <summary>Size the distance line is tuned at; the `Fonts` section scales this rather than replacing it (see <see cref="Common.HudFontScale"/>).</summary>
        private const float DistanceFontSizeBase = 18f;

        /// <summary>Size the (normally-hidden) name row is tuned at - see <see cref="SetFlashName"/>.</summary>
        private const float NameFontSizeBase = 18f;

        public readonly IndicatorAnchor Anchor;

        private readonly RectTransform _root;
        private readonly Image _iconImage;
        private readonly Image[] _outlineImages;
        private readonly TMP_Text _distanceText;
        private readonly TMP_Text _nameText;

        private CampfireWidget(
            RectTransform root, Image iconImage, Image[] outlineImages,
            TMP_Text distanceText, TMP_Text nameText, System.Func<Vector3> getWorldPosition)
        {
            _root = root;
            _iconImage = iconImage;
            _outlineImages = outlineImages;
            _distanceText = distanceText;
            _nameText = nameText;
            // Icon is always visible, so the box is never fully zero (unlike
            // Pings.PingWidget) - just shrunk to icon-only when the distance
            // sub-line is hidden, refined every Refresh() call below. The whole
            // widget (icon and text together) moves, so like Labels.PlayerLabel
            // it can afford a larger cap than a label sliding away from an arrow
            // left standing at the tracked position.
            Anchor = new IndicatorAnchor(getWorldPosition, root)
            {
                OverlapSize = new Vector2(28f, 28f),
                MaxOverlapOffset = 110f,
            };
        }

        /// <param name="parent">Where the widget is built. Null (the live game) means the shared overlay canvas; the config preview menu passes its own stage instead.</param>
        public static CampfireWidget Create(System.Func<Vector3> getWorldPosition, RectTransform parent = null)
        {
            RectTransform canvasTransform = parent != null ? parent : IndicatorManager.Instance.CanvasTransform;

            var rootGo = new GameObject("SoD.CampfireIndicator", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(canvasTransform, false);
            root.sizeDelta = new Vector2(28f, 28f);

            // Outline copies first (rendered behind, per UI sibling order),
            // real icon last (rendered on top).
            var outlineImages = new Image[OutlineOffsets.Length];
            for (int i = 0; i < OutlineOffsets.Length; i++)
            {
                outlineImages[i] = CreateIconImage(root, $"Outline{i}", OutlineOffsets[i], Color.black);
            }
            Image iconImage = CreateIconImage(root, "Icon", Vector2.zero, new Color(1f, 1f, 1f, 0f));

            var textGo = new GameObject("Distance", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(root, false);
            textRect.sizeDelta = new Vector2(120f, 24f);
            textRect.anchoredPosition = new Vector2(0f, -22f);

            var distanceText = textGo.GetComponent<TextMeshProUGUI>();
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.color = Color.white;
            distanceText.fontSize = DistanceFontSizeBase;
            distanceText.enableWordWrapping = false;

            // Hidden by default - this widget deliberately shows no name at
            // all under normal operation (see this class's own doc comment),
            // only while SetFlashName is holding it visible for a ping-flash
            // (Common.PingFlashState).
            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.SetParent(root, false);
            nameRect.sizeDelta = new Vector2(320f, 28f);
            nameRect.anchoredPosition = new Vector2(0f, 22f);

            var nameText = nameGo.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = Color.white;
            nameText.fontSize = NameFontSizeBase;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Overflow;
            nameGo.SetActive(false);

            return new CampfireWidget(root, iconImage, outlineImages, distanceText, nameText, getWorldPosition);
        }

        private static Image CreateIconImage(RectTransform parent, string goName, Vector2 anchoredPosition, Color color)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(28f, 28f);
            rect.anchoredPosition = anchoredPosition;

            var image = go.GetComponent<Image>();
            image.preserveAspect = true;
            // No sprite yet (NativeAssets hasn't found it) - stay invisible
            // rather than rendering Unity's default solid-white placeholder
            // rect until Refresh() assigns the real campfire icon.
            image.color = color;
            return image;
        }

        public void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        /// <summary>
        /// Icon sprite (and its outline copies') and the distance text's
        /// font/material are re-applied every call rather than baked in at
        /// creation (cheap, matches <see cref="Labels.PlayerLabel.Refresh"/>'s
        /// own reasoning) so a widget created before <see cref="NativeAssets"/>
        /// finishes discovering these still picks them up as soon as they're
        /// found.
        /// </summary>
        /// <param name="iconSprite">
        /// Icon to draw. Null (the usual case) means the game's own HUD campfire
        /// sprite as soon as <see cref="NativeAssets"/> has found it;
        /// <see cref="CampfireIndicatorController"/> passes
        /// <see cref="Common.IconAssets.Peak"/> instead once it has switched from
        /// the campfire to the summit. Both are finished black-and-white art, so
        /// either gets the same untinted treatment (white <c>Image.color</c>, i.e.
        /// the sprite's own colors, plus the black outline copies).
        /// </param>
        public void Refresh(float distanceMeters, bool showDistance, Sprite iconSprite = null)
        {
            Sprite icon = iconSprite != null ? iconSprite : NativeAssets.CampfireIconSprite;
            if (icon != null && _iconImage.sprite != icon)
            {
                _iconImage.sprite = icon;
                _iconImage.color = Color.white;
                foreach (Image outline in _outlineImages)
                {
                    outline.sprite = icon;
                    outline.color = Color.black;
                }
            }

            // Live config value (PluginConfig.IndicatorIconSizeMultiplier), so
            // re-applied every frame rather than baked in at creation.
            float iconSize = 28f * Plugin.Instance.Cfg.IndicatorIconSizeMultiplier.Value;
            _iconImage.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            foreach (Image outline in _outlineImages)
            {
                outline.rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
            }

            if (NativeAssets.Font != null && _distanceText.font != NativeAssets.Font)
            {
                _distanceText.font = NativeAssets.Font;
            }
            if (NativeAssets.OutlineMaterial != null && _distanceText.fontSharedMaterial != NativeAssets.OutlineMaterial)
            {
                _distanceText.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            // Live config value, so re-applied every frame rather than baked in
            // at creation.
            _distanceText.fontSize = Common.HudFontScale.Distance(DistanceFontSizeBase, Anchor.OffScreenBlend);

            _distanceText.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                _distanceText.text = $"{Mathf.RoundToInt(distanceMeters)}m";
            }

            // Box measured from what's actually drawn (the icon, plus the
            // distance line hanging below it at -22, plus the name row above
            // it at +22 while a ping flash is holding it visible) rather than
            // a fixed guess, so it neither invents collisions with a
            // neighbour it's clear of nor misses one it isn't. Icon top/
            // bottom scale with iconSize above, so a bigger icon
            // (indicator-icon-size-multiplier) still gets a correctly-sized
            // overlap footprint instead of clipping a neighbour.
            bool showName = _nameText.gameObject.activeSelf;
            float iconHalf = iconSize * 0.5f;
            float top = showName ? iconHalf + 20f : iconHalf;
            float bottom = showDistance ? -(iconHalf + 20f) : -iconHalf;
            float width = iconSize;
            if (showDistance)
            {
                width = Mathf.Max(width, _distanceText.GetPreferredValues().x + 12f);
            }
            if (showName)
            {
                width = Mathf.Max(width, _nameText.GetPreferredValues().x + 12f);
            }

            Anchor.OverlapSize = new Vector2(width, top - bottom);
            Anchor.OverlapCenterOffset = new Vector2(0f, (top + bottom) * 0.5f);
        }

        /// <summary>
        /// Temporarily shows (or hides again) a name row above the icon -
        /// this widget normally has no name label at all (see this class's
        /// own doc comment), only while a ping flash
        /// (<see cref="Common.PingFlashState"/>) is holding it visible, per
        /// the maintainer's ask that a ping on the campfire show its name the
        /// same way a real item ping would (<c>Item-Pings/name-mode</c>),
        /// regardless of <c>Campfire/hide-name</c> (which only governs the
        /// compass marker's own label, a separate rendering surface).
        /// </summary>
        public void SetFlashName(bool show, string text)
        {
            _nameText.gameObject.SetActive(show);
            if (show)
            {
                if (NativeAssets.Font != null && _nameText.font != NativeAssets.Font)
                {
                    _nameText.font = NativeAssets.Font;
                }
                if (NativeAssets.OutlineMaterial != null && _nameText.fontSharedMaterial != NativeAssets.OutlineMaterial)
                {
                    _nameText.fontSharedMaterial = NativeAssets.OutlineMaterial;
                }
                _nameText.fontSize = Common.HudFontScale.Name(NameFontSizeBase, Anchor.OffScreenBlend);
                _nameText.text = text;
            }
        }

        /// <summary>
        /// Overrides the name text color - used by
        /// <see cref="CampfireIndicatorController"/>'s ping-flash feedback.
        /// Leaves the distance text (always stays white regardless of a ping
        /// flash, maintainer's ask) and the icon/outline (those stay the
        /// campfire's/summit's/portal's own untinted native art either way,
        /// see <see cref="Refresh"/>'s own icon-color handling) alone.
        /// </summary>
        public void SetNameColor(Color color)
        {
            _nameText.color = color;
        }
    }
}
