using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Compass
{
    /// <summary>
    /// One fixed heading mark on the compass tape (every 15°): a short
    /// vertical line always shown, plus a label - N/E/S/W at the four
    /// cardinal points (always lettered), a plain numeric heading at every
    /// other mark when <c>compass-show-degree-numbers</c> is on, or nothing
    /// (just the line) otherwise. <see cref="CompassManager"/> creates the
    /// fixed set of these once and repositions/refades them every frame as
    /// the camera turns - none of these are ever created/destroyed at
    /// runtime, unlike <see cref="CompassMarkerWidget"/>.
    /// </summary>
    public class CompassTick
    {
        public readonly float Degrees;
        public readonly RectTransform Rect;
        public readonly CanvasGroup CanvasGroup;
        public readonly TMP_Text Label;

        private CompassTick(float degrees, RectTransform rect, CanvasGroup canvasGroup, TMP_Text label)
        {
            Degrees = degrees;
            Rect = rect;
            CanvasGroup = canvasGroup;
            Label = label;
        }

        public static CompassTick Create(RectTransform parent, float degrees)
        {
            var rootGo = new GameObject($"SoD.CompassTick.{degrees:F0}", typeof(RectTransform));
            var rect = (RectTransform)rootGo.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = Vector2.zero; // see CompassManager's own Root for why this matters

            var canvasGroup = rootGo.AddComponent<CanvasGroup>();

            bool isCardinal = Mathf.Approximately(degrees % 90f, 0f);
            bool isNorth = Mathf.Approximately(degrees % 360f, 0f);

            // Cardinal (N/E/S/W) ticks are taller/thicker than the plain
            // degree ticks between them, so the two read as clearly distinct
            // at a glance even before the label text is legible. Both heights
            // kept even (not odd), so centering both on the same point splits
            // symmetrically either side with no rounding-induced drift.
            float lineHeight = isCardinal ? 16f : 8f;

            var lineGo = new GameObject("Line", typeof(RectTransform), typeof(Image));
            var lineRect = (RectTransform)lineGo.transform;
            lineRect.SetParent(rect, false);
            // Centered (not top-pivoted) on the tick's own origin, which
            // CompassManager sets to the same Y as the marker baseline -
            // straddles the baseline line so the two form a "+" cross,
            // rather than sitting entirely above it.
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(isCardinal ? 2.5f : 1f, lineHeight);
            // Minor ticks still read as sitting slightly too high even at an
            // even height - lazy fix, just nudge them down 1px rather than
            // chasing the exact sub-pixel cause further.
            lineRect.anchoredPosition = isCardinal ? Vector2.zero : new Vector2(0f, -1f);
            lineGo.GetComponent<Image>().color = isNorth ? CompassTheme.NorthAccent : new Color(1f, 1f, 1f, isCardinal ? 0.9f : 0.45f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var labelRect = (RectTransform)labelGo.transform;
            labelRect.SetParent(rect, false);
            // Above the line's own top edge (half its height, since it's
            // center-pivoted), plus a small gap - bottom-pivoted so it grows
            // upward from that point rather than downward through it.
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.sizeDelta = new Vector2(60f, 20f);
            labelRect.anchoredPosition = new Vector2(0f, lineHeight * 0.5f + 6f);

            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.fontSize = isCardinal ? 20f : 14f;
            label.color = isNorth ? CompassTheme.NorthAccent : (isCardinal ? Color.white : new Color(1f, 1f, 1f, 0.75f));

            return new CompassTick(degrees, rect, canvasGroup, label);
        }
    }
}
