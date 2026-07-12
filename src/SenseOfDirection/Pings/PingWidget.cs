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

        private PingWidget(RectTransform root, CanvasGroup canvasGroup, RectTransform arrow, TMP_Text distanceText, System.Func<Vector3> getWorldPosition)
        {
            CanvasGroup = canvasGroup;
            _distanceText = distanceText;
            Anchor = new IndicatorAnchor(getWorldPosition, root, arrow);
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

            var textGo = new GameObject("Distance", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(root, false);
            textRect.sizeDelta = new Vector2(120f, 24f);
            textRect.anchoredPosition = new Vector2(0f, -22f);

            var distanceText = textGo.GetComponent<TextMeshProUGUI>();
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.color = color;
            distanceText.fontSize = 16f;
            distanceText.enableWordWrapping = false;

            return new PingWidget(root, canvasGroup, arrowRect, distanceText, getWorldPosition);
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
            }
        }
    }
}
