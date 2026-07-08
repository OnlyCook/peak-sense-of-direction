using UnityEngine;

namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// Result of projecting a world position onto a screen-space canvas: either
    /// its on-screen position, or a position clamped to the nearest canvas edge
    /// plus a direction angle for an off-screen arrow indicator.
    /// </summary>
    public struct IndicatorState
    {
        public Vector2 CanvasPosition;
        public bool IsOffScreen;

        /// <summary>
        /// Degrees, standard math convention (0 = +X/right, 90 = +Y/up,
        /// counter-clockwise). Only meaningful when <see cref="IsOffScreen"/>
        /// is true. Consumers rotating a "points up" arrow sprite should
        /// subtract 90 when applying this to a Z rotation.
        /// </summary>
        public float ArrowAngleDegrees;
    }

    /// <summary>
    /// Pure screen-space geometry: given a camera, a canvas size, and a world
    /// position, computes where to place a UI element for it - either the real
    /// on-screen point, or a point clamped to the edge of the canvas (inset by
    /// a margin) with a direction for an off-screen indicator arrow.
    ///
    /// No MonoBehaviour/gameplay dependency - shared by Mechanic 1 (player
    /// labels) and Mechanic 2 (ping indicator), and independently testable
    /// against dummy points before either mechanic exists.
    /// </summary>
    public static class ScreenSpaceTracker
    {
        public static IndicatorState Compute(
            Camera camera,
            Vector2 canvasSize,
            Vector3 worldPosition,
            float edgeMarginPixels = 48f)
        {
            Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
            bool behindCamera = viewport.z < 0f;
            bool withinBounds = viewport.x >= 0f && viewport.x <= 1f
                                 && viewport.y >= 0f && viewport.y <= 1f;

            if (!behindCamera && withinBounds)
            {
                return new IndicatorState
                {
                    CanvasPosition = ViewportToCanvas(viewport, canvasSize),
                    IsOffScreen = false,
                    ArrowAngleDegrees = 0f,
                };
            }

            // Direction from canvas center to the (possibly out-of-bounds)
            // viewport point. A point behind the camera projects to the
            // opposite side of the viewport from where it actually is, so its
            // raw direction is mirrored back through the center first.
            Vector2 fromCenter = new Vector2(
                (viewport.x - 0.5f) * canvasSize.x,
                (viewport.y - 0.5f) * canvasSize.y);
            if (behindCamera)
            {
                fromCenter = -fromCenter;
            }
            if (fromCenter.sqrMagnitude < 0.0001f)
            {
                fromCenter = Vector2.up;
            }
            Vector2 direction = fromCenter.normalized;

            Vector2 edgePosition = ClampToRectEdge(direction, canvasSize, edgeMarginPixels);

            return new IndicatorState
            {
                CanvasPosition = edgePosition,
                IsOffScreen = true,
                ArrowAngleDegrees = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg,
            };
        }

        /// <summary>
        /// Scales a center-relative direction out to the boundary of a
        /// centered rectangle (canvasSize, inset by edgeMarginPixels on every
        /// side). Standard rectangle/ray intersection from the center: the
        /// hit distance is 1 / max(|dx|/halfWidth, |dy|/halfHeight).
        /// </summary>
        private static Vector2 ClampToRectEdge(Vector2 direction, Vector2 canvasSize, float edgeMarginPixels)
        {
            float halfWidth = Mathf.Max(canvasSize.x * 0.5f - edgeMarginPixels, 1f);
            float halfHeight = Mathf.Max(canvasSize.y * 0.5f - edgeMarginPixels, 1f);

            float scale = 1f / Mathf.Max(
                Mathf.Abs(direction.x) / halfWidth,
                Mathf.Abs(direction.y) / halfHeight);

            return direction * scale;
        }

        private static Vector2 ViewportToCanvas(Vector3 viewport, Vector2 canvasSize)
        {
            return new Vector2(
                (viewport.x - 0.5f) * canvasSize.x,
                (viewport.y - 0.5f) * canvasSize.y);
        }
    }
}
