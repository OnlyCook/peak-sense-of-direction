using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;

namespace SenseOfDirection.Pings
{
    /// <summary>
    /// Screen-space indicator for one active ping: an optional distance
    /// sub-line, and - unlike <see cref="Labels.PlayerLabel"/> and the
    /// campfire indicator, which clamp quietly to the edge - an off-screen
    /// arrow child, since the arrow is reserved for pings per maintainer
    /// direction (see those classes' own doc comments). Deliberately no
    /// on-screen marker/dot: the real 3D ping is already visible on-screen,
    /// so a 2D UI element drawn on top of it just obstructs the view; the
    /// arrow (via <see cref="IndicatorManager"/>'s own off-screen gating)
    /// only ever appears once the ping has actually left the screen. Tinted
    /// to the pinging player's own character color - both the off-screen
    /// arrow and the distance sub-line.
    /// </summary>
    public class PingWidget
    {
        public readonly IndicatorAnchor Anchor;
        public CanvasGroup CanvasGroup { get; }

        private readonly TMP_Text _distanceText;

        private PingWidget(RectTransform root, CanvasGroup canvasGroup, RectTransform arrow, RectTransform labelGroup, TMP_Text distanceText, System.Func<Vector3> getWorldPosition)
        {
            CanvasGroup = canvasGroup;
            _distanceText = distanceText;
            // LabelWidget = labelGroup (not root) so overlap resolution only
            // ever nudges the distance text, never the off-screen arrow -
            // the arrow needs to stay exactly on the tracked position to
            // still point the right way. OverlapSize itself is left at
            // (0,0) here and only ever set in Refresh() while the distance
            // text is actually visible (see there) - a ping's distance line
            // is often suppressed entirely (e.g. an item ping already
            // showing distance for the same event, per PointPingerPatches),
            // and a stale nonzero box on an invisible widget was pushing
            // other, real labels away from a target they'd otherwise have
            // no reason to avoid.
            Anchor = new IndicatorAnchor(getWorldPosition, root, arrow) { LabelWidget = labelGroup };
        }

        public static PingWidget Create(System.Func<Vector3> getWorldPosition, Color color, bool enableArrow)
        {
            RectTransform canvasTransform = IndicatorManager.Instance.CanvasTransform;

            var rootGo = new GameObject("SoD.PingIndicator", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(canvasTransform, false);
            root.sizeDelta = new Vector2(20f, 20f);

            var canvasGroup = rootGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            RectTransform arrowRect = enableArrow ? OffScreenArrow.Create(root, color) : null;

            // Home position (0,0) relative to root - overlap resolution
            // nudges this transform, not root/arrow (see LabelWidget above).
            var labelGroupGo = new GameObject("LabelGroup", typeof(RectTransform));
            var labelGroupRect = (RectTransform)labelGroupGo.transform;
            labelGroupRect.SetParent(root, false);

            var textGo = new GameObject("Distance", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(labelGroupRect, false);
            textRect.sizeDelta = new Vector2(120f, 24f);
            textRect.anchoredPosition = new Vector2(0f, -22f);

            var distanceText = textGo.GetComponent<TextMeshProUGUI>();
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.color = color;
            distanceText.fontSize = 16f;
            distanceText.enableWordWrapping = false;

            return new PingWidget(root, canvasGroup, arrowRect, labelGroupRect, distanceText, getWorldPosition);
        }

        public void Refresh(float distanceMeters, bool showDistance)
        {
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
                // Just the distance line: 24 tall, anchored 22px below the
                // tracked point (hence the centre offset - the arrow itself
                // stays put and isn't part of the box).
                Anchor.OverlapSize = new Vector2(_distanceText.GetPreferredValues().x + 12f, 28f);
                Anchor.OverlapCenterOffset = new Vector2(0f, -22f);
            }
            else
            {
                Anchor.OverlapSize = Vector2.zero;
                Anchor.OverlapCenterOffset = Vector2.zero;
            }
        }
    }
}
