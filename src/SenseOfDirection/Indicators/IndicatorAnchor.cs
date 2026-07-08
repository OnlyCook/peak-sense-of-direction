using System;
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

        public IndicatorAnchor(Func<Vector3> getWorldPosition, RectTransform widget, RectTransform arrowWidget = null)
        {
            GetWorldPosition = getWorldPosition;
            Widget = widget;
            ArrowWidget = arrowWidget;
        }
    }
}
