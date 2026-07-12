using System;
using SenseOfDirection.Compass;
using UnityEngine;

namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// A single world-space thing tracked by <see cref="IndicatorManager"/>:
    /// a world-position source (a live player, a ping, a campfire, or a fixed
    /// dummy test point) plus the UI widget that represents it on screen.
    ///
    /// Generic on purpose - Mechanic 1 (player labels), Mechanic 2 (pings),
    /// and the campfire indicator all register their own widget hierarchy and
    /// let the shared manager handle the screen-space placement math.
    ///
    /// The Compass-prefixed fields below (Phase 7) are optional metadata read
    /// by <see cref="Compass.CompassManager"/> so it can render its own
    /// top-of-screen marker for this same anchor without each mechanic having
    /// to register twice - defaults mean "not shown on the compass" (CompassKind
    /// stays None) until a mechanic opts in.
    /// </summary>
    public class IndicatorAnchor
    {
        public readonly Func<Vector3> GetWorldPosition;

        /// <summary>Widget hidden entirely (position not updated) when this returns false.</summary>
        public Func<bool> IsActive = () => true;

        public float EdgeMarginPixels = 48f;

        /// <summary>Root widget, repositioned every frame to the on-screen or clamped-edge point.</summary>
        public readonly RectTransform Widget;

        /// <summary>
        /// Optional child shown only while off-screen and rotated to point
        /// toward the tracked world position. Null if this anchor has no
        /// off-screen arrow (e.g. it's only ever meaningful on-screen).
        /// </summary>
        public readonly RectTransform ArrowWidget;

        /// <summary>
        /// Optional child shown only while on-screen (the exact opposite of
        /// <see cref="ArrowWidget"/>) - e.g. the item-ping crosshair, which
        /// only makes sense overlaid on the actually-visible object, not
        /// while the off-screen arrow is pointing toward it instead. Null if
        /// this anchor has no such widget.
        /// </summary>
        public readonly RectTransform OnScreenOnlyWidget;

        /// <summary>None means this anchor never shows up on the compass tape.</summary>
        public CompassMarkerKind CompassKind = CompassMarkerKind.None;

        /// <summary>Governs whether <see cref="Widget"/>/<see cref="ArrowWidget"/> vs. the compass marker (or both) are shown for this anchor.</summary>
        public Func<IndicatorDisplayMode> GetDisplayMode = () => IndicatorDisplayMode.OffScreenOnly;

        /// <summary>Extra compass-only visibility gate (e.g. player labels' own toggle-key/distance-gate state) on top of <see cref="IsActive"/>.</summary>
        public Func<bool> IsCompassVisible = () => true;

        public Func<Color> GetCompassColor = () => Color.white;

        /// <summary>Null/empty means no name text is available for this anchor (e.g. a generic point ping).</summary>
        public Func<string> GetCompassLabel = () => null;

        public Func<bool> GetIsDead = () => false;
        public Func<bool> GetIsUnconscious = () => false;

        public IndicatorAnchor(Func<Vector3> getWorldPosition, RectTransform widget, RectTransform arrowWidget = null, RectTransform onScreenOnlyWidget = null)
        {
            GetWorldPosition = getWorldPosition;
            Widget = widget;
            ArrowWidget = arrowWidget;
            OnScreenOnlyWidget = onScreenOnlyWidget;
        }
    }
}
