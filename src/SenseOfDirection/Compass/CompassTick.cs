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

        private readonly RectTransform _lineRect;
        private readonly float _lineBaseWidth;
        private readonly float _lineBaseHeight;
        private readonly Image _lineImage;
        private readonly bool _isCardinal;
        private readonly bool _isNorth;

        private CompassTick(float degrees, RectTransform rect, CanvasGroup canvasGroup, TMP_Text label, RectTransform lineRect, float lineBaseWidth, float lineBaseHeight, Image lineImage, bool isCardinal, bool isNorth)
        {
            Degrees = degrees;
            Rect = rect;
            CanvasGroup = canvasGroup;
            Label = label;
            _lineRect = lineRect;
            _lineBaseWidth = lineBaseWidth;
            _lineBaseHeight = lineBaseHeight;
            _lineImage = lineImage;
            _isCardinal = isCardinal;
            _isNorth = isNorth;
        }

        /// <summary>
        /// Re-tints the tick's line/label against <paramref name="baseColor"/>
        /// (<c>compass-line-color</c>, resolved by <see cref="CompassTheme.LineColor"/>)
        /// every frame, same live-config-applies-without-restart pattern as
        /// <see cref="ApplyHeight"/> - true north keeps its own fixed
        /// <see cref="CompassTheme.NorthAccent"/> regardless of this setting.
        /// </summary>
        public void ApplyLineColor(Color baseColor)
        {
            if (_isNorth)
            {
                return;
            }

            _lineImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, _isCardinal ? 0.9f : 0.45f);
            Label.color = _isCardinal
                ? new Color(baseColor.r, baseColor.g, baseColor.b, 1f)
                : new Color(baseColor.r, baseColor.g, baseColor.b, 0.75f);
        }

        /// <summary>
        /// Grows the tick's vertical line by <paramref name="extraPixels"/>
        /// (<c>compass-height-pixels</c> past its default), symmetrically
        /// about its own fixed local center - only the sizeDelta changes
        /// here, the local anchoredPosition never does. The actual
        /// downward-only look (top edge fixed on screen, bottom edge
        /// extending further down, baseline dragged down to stay centered
        /// on the growing line) comes entirely from <see cref="CompassManager"/>
        /// moving the shared tick-root/baseline Y by half of this same
        /// growth - two independent, additive shifts that together produce
        /// the requested effect without this class needing to know about
        /// the baseline at all.
        /// </summary>
        public void ApplyHeight(float extraPixels)
        {
            _lineRect.sizeDelta = new Vector2(_lineBaseWidth, _lineBaseHeight + extraPixels);
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
            float topHalf = lineHeight * 0.5f;

            var lineGo = new GameObject("Line", typeof(RectTransform), typeof(Image));
            var lineRect = (RectTransform)lineGo.transform;
            lineRect.SetParent(rect, false);
            // Centered (not top-pivoted) on the tick's own origin, which
            // CompassManager sets to the same Y as the marker baseline's own
            // visual center - straddles the baseline line so the two form a
            // "+" cross. The cross point (the line's top half, above the
            // baseline) is identical for every tick, cardinal or not - an
            // earlier version nudged only minor ticks down 1px to
            // compensate for the baseline being top- rather than
            // center-pivoted at the time, which just traded one 1px
            // misalignment (cardinal ticks) for another (the nudge growing
            // more visible as `compass-height-pixels` stretched the line
            // further); fixing the baseline's own pivot instead makes every
            // tick line up the same way with no per-tick special case.
            // The extra 1px on the bottom half only (not a symmetric +0.5 on
            // both sides) is a deliberate, requested asymmetry - real
            // compass tapes read as resting "on" the baseline, not
            // straddling it dead center.
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            float lineWidth = isCardinal ? 2.5f : 1.5f;
            float lineBaseHeight = lineHeight + 1f;
            float lineBaseY = -0.5f;
            lineRect.sizeDelta = new Vector2(lineWidth, lineBaseHeight);
            lineRect.anchoredPosition = new Vector2(0f, lineBaseY);
            var lineImage = lineGo.GetComponent<Image>();
            lineImage.color = isNorth ? CompassTheme.NorthAccent : new Color(1f, 1f, 1f, isCardinal ? 0.9f : 0.45f);

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
            labelRect.anchoredPosition = new Vector2(0f, topHalf + 6f);

            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.fontSize = isCardinal ? 20f : 14f;
            label.color = isNorth ? CompassTheme.NorthAccent : (isCardinal ? Color.white : new Color(1f, 1f, 1f, 0.75f));

            return new CompassTick(degrees, rect, canvasGroup, label, lineRect, lineWidth, lineBaseHeight, lineImage, isCardinal, isNorth);
        }
    }
}
