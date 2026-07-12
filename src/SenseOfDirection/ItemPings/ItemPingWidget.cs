using SenseOfDirection.Common;
using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// Screen-space widget for one item/luggage ping highlight: a name label
    /// (item name, or "Nx Item Name" when grouped) above an optional distance
    /// sub-line, plus the same off-screen arrow mechanism <see
    /// cref="Pings.PingWidget"/> uses - unlike player labels/the campfire
    /// indicator, which deliberately clamp quietly with no arrow (see those
    /// classes' own doc comments), the arrow makes sense here since this is
    /// pointing at a specific pinged object, same reasoning as the ping
    /// indicator itself. Tinted to the pinging player's own character color,
    /// same as the ping/ripple. A small crosshair (the same diamond icon the
    /// compass uses for item pings) sits between the name and distance line so
    /// the widget reads as a crosshair on the pinged object rather than just
    /// floating text.
    /// </summary>
    public class ItemPingWidget
    {
        public readonly IndicatorAnchor Anchor;
        public CanvasGroup CanvasGroup { get; }

        private readonly TMP_Text _nameText;
        private readonly TMP_Text _distanceText;

        private ItemPingWidget(RectTransform root, CanvasGroup canvasGroup, RectTransform arrow, RectTransform crosshairRect, RectTransform labelGroup, TMP_Text nameText, TMP_Text distanceText, System.Func<Vector3> getWorldPosition)
        {
            CanvasGroup = canvasGroup;
            _nameText = nameText;
            _distanceText = distanceText;
            // Root is a tiny 20x20 anchor point; the real footprint spans
            // the name label above it down through the distance sub-line
            // below. Refined every Refresh() call to the actual rendered
            // text width instead of a generous static guess - an
            // over-wide box here made overlap resolution trigger (and push
            // labels away) far more than actually needed. LabelWidget =
            // labelGroup (not root) so overlap resolution only ever nudges
            // the name/distance text, never the arrow or the on-screen
            // crosshair - both need to stay exactly on the tracked position.
            Anchor = new IndicatorAnchor(getWorldPosition, root, arrow, crosshairRect) { OverlapSize = new Vector2(120f, 60f), LabelWidget = labelGroup };
        }

        public static ItemPingWidget Create(System.Func<Vector3> getWorldPosition, Color color, bool enableArrow)
        {
            RectTransform canvasTransform = IndicatorManager.Instance.CanvasTransform;

            var rootGo = new GameObject("SoD.ItemPingIndicator", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(canvasTransform, false);
            root.sizeDelta = new Vector2(20f, 20f);

            var canvasGroup = rootGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            RectTransform arrowRect = enableArrow ? OffScreenArrow.Create(root, color) : null;

            // Home position (0,0) relative to root - overlap resolution
            // nudges this transform, not root/arrow/crosshair (see
            // LabelWidget above).
            var labelGroupGo = new GameObject("LabelGroup", typeof(RectTransform));
            var labelGroupRect = (RectTransform)labelGroupGo.transform;
            labelGroupRect.SetParent(root, false);

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.SetParent(labelGroupRect, false);
            nameRect.sizeDelta = new Vector2(320f, 28f);
            nameRect.anchoredPosition = new Vector2(0f, 24f);

            var nameText = nameGo.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = color;
            nameText.fontSize = 20f;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Overflow;

            var distGo = new GameObject("Distance", typeof(RectTransform), typeof(TextMeshProUGUI));
            var distRect = (RectTransform)distGo.transform;
            distRect.SetParent(labelGroupRect, false);
            distRect.sizeDelta = new Vector2(120f, 24f);
            distRect.anchoredPosition = new Vector2(0f, -18f);

            var distanceText = distGo.GetComponent<TextMeshProUGUI>();
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.color = color;
            distanceText.fontSize = 16f;
            distanceText.enableWordWrapping = false;

            // Sits between the name and distance line (name bottom ~10px, distance
            // top ~-16px) so it reads as a crosshair on the pinged object itself,
            // same diamond icon the compass already uses for item pings. Only
            // shown while the target is actually on-screen - see
            // IndicatorAnchor.OnScreenOnlyWidget - since it makes no sense
            // overlaid on nothing while the off-screen arrow is showing instead.
            var crosshairGo = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            var crosshairRect = (RectTransform)crosshairGo.transform;
            crosshairRect.SetParent(root, false);
            crosshairRect.sizeDelta = new Vector2(30f, 30f);
            crosshairRect.anchoredPosition = Vector2.zero;

            var crosshairIcon = crosshairGo.GetComponent<Image>();
            crosshairIcon.sprite = IconAssets.ItemPingDiamond;
            crosshairIcon.color = color;
            crosshairIcon.raycastTarget = false;
            crosshairIcon.preserveAspect = true;

            return new ItemPingWidget(root, canvasGroup, arrowRect, crosshairRect, labelGroupRect, nameText, distanceText, getWorldPosition);
        }

        public void Refresh(string displayName, float distanceMeters, bool showName, bool showDistance)
        {
            if (NativeAssets.Font != null)
            {
                if (_nameText.font != NativeAssets.Font) _nameText.font = NativeAssets.Font;
                if (_distanceText.font != NativeAssets.Font) _distanceText.font = NativeAssets.Font;
            }
            if (NativeAssets.OutlineMaterial != null)
            {
                if (_nameText.fontSharedMaterial != NativeAssets.OutlineMaterial) _nameText.fontSharedMaterial = NativeAssets.OutlineMaterial;
                if (_distanceText.fontSharedMaterial != NativeAssets.OutlineMaterial) _distanceText.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            _nameText.gameObject.SetActive(showName);
            if (showName)
            {
                _nameText.text = displayName;
            }

            _distanceText.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                _distanceText.text = $"{Mathf.RoundToInt(distanceMeters)}m";
            }

            // The widget is centered on its anchor point, but IndicatorManager
            // only clamps that anchor point itself to within EdgeMarginPixels
            // of the screen edge - it has no idea how wide the name label
            // actually renders. A fixed default margin (48px) is nowhere near
            // half the width of a long/grouped name (e.g. "2x COCONUT"), so
            // the label's own left/right half was clipping past the physical
            // screen edge even though its anchor point was safely on-screen.
            // Widen the margin to always cover half the widest currently-shown
            // label text, recomputed every refresh since text changes live.
            float widestHalf = 0f;
            if (showName)
            {
                widestHalf = Mathf.Max(widestHalf, _nameText.GetPreferredValues().x * 0.5f);
            }
            if (showDistance)
            {
                widestHalf = Mathf.Max(widestHalf, _distanceText.GetPreferredValues().x * 0.5f);
            }
            Anchor.EdgeMarginPixels = Mathf.Max(48f, widestHalf + 12f);

            // Same widest-visible-text measurement, reused for the overlap
            // box (Indicators.LabelOverlapResolver) instead of a separate
            // fixed guess - keeps overlap detection matched to what's
            // actually on screen (e.g. a short "KING" vs. a wider
            // "2x COCONUT").
            float widestWidth = showName || showDistance ? Mathf.Max(60f, widestHalf * 2f) : 0f;
            Anchor.OverlapSize = new Vector2(widestWidth, 60f);
        }
    }
}
