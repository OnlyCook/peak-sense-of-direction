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

        public readonly IndicatorAnchor Anchor;

        private readonly RectTransform _root;
        private readonly Image _iconImage;
        private readonly Image[] _outlineImages;
        private readonly TMP_Text _distanceText;

        private CampfireWidget(
            RectTransform root, Image iconImage, Image[] outlineImages,
            TMP_Text distanceText, System.Func<Vector3> getWorldPosition)
        {
            _root = root;
            _iconImage = iconImage;
            _outlineImages = outlineImages;
            _distanceText = distanceText;
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

        public static CampfireWidget Create(System.Func<Vector3> getWorldPosition)
        {
            RectTransform canvasTransform = IndicatorManager.Instance.CanvasTransform;

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
            distanceText.fontSize = 18f;
            distanceText.enableWordWrapping = false;

            return new CampfireWidget(root, iconImage, outlineImages, distanceText, getWorldPosition);
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
        public void Refresh(float distanceMeters, bool showDistance)
        {
            if (NativeAssets.CampfireIconSprite != null && _iconImage.sprite != NativeAssets.CampfireIconSprite)
            {
                _iconImage.sprite = NativeAssets.CampfireIconSprite;
                _iconImage.color = Color.white;
                foreach (Image outline in _outlineImages)
                {
                    outline.sprite = NativeAssets.CampfireIconSprite;
                    outline.color = Color.black;
                }
            }

            if (NativeAssets.Font != null && _distanceText.font != NativeAssets.Font)
            {
                _distanceText.font = NativeAssets.Font;
            }
            if (NativeAssets.OutlineMaterial != null && _distanceText.fontSharedMaterial != NativeAssets.OutlineMaterial)
            {
                _distanceText.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            _distanceText.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                _distanceText.text = $"{Mathf.RoundToInt(distanceMeters)}m";
            }

            // Box measured from what's actually drawn (28px icon, plus the
            // distance line hanging below it at -22) rather than a fixed guess,
            // so it neither invents collisions with a neighbour it's clear of nor
            // misses one it isn't. Icon top is +14, distance line bottom -34,
            // so the box doesn't sit centred on the tracked point.
            float top = 14f;
            float bottom = showDistance ? -34f : -14f;
            float width = showDistance
                ? Mathf.Max(28f, _distanceText.GetPreferredValues().x + 12f)
                : 28f;

            Anchor.OverlapSize = new Vector2(width, top - bottom);
            Anchor.OverlapCenterOffset = new Vector2(0f, (top + bottom) * 0.5f);
        }
    }
}
