using UnityEngine;

namespace SenseOfDirection.Indicators
{
    // Result of projecting a world position onto a screen-space canvas: either
    // its on-screen position, or a position clamped to the nearest canvas edge
    // plus a direction angle for an off-screen arrow indicator.
    public struct IndicatorState
    {
        public Vector2 CanvasPosition;
        public bool IsOffScreen;

        // Degrees, standard math convention (0 = +X/right, 90 = +Y/up, CCW).
        // Only meaningful when IsOffScreen is true. Rotating a "points up"
        // arrow sprite should subtract 90 when applying this to a Z rotation.
        public float ArrowAngleDegrees;
    }

    // Pure screen-space geometry: given a camera, canvas size, and world
    // position, computes where to place a UI element for it - the real
    // on-screen point, or a point clamped to the canvas edge (inset by a
    // margin) with a direction for an off-screen arrow. No MonoBehaviour/
    // gameplay dependency, shared by player labels and pings.
    public static class ScreenSpaceTracker
    {
        public static IndicatorState Compute(
            Camera camera,
            Vector2 canvasSize,
            Vector3 worldPosition,
            float edgeMarginPixels = 48f,
            bool clampToEdge = true)
        {
            Vector3 viewport = camera.WorldToViewportPoint(worldPosition);
            bool behindCamera = viewport.z < 0f;
            bool withinBounds = viewport.x >= 0f && viewport.x <= 1f
                                 && viewport.y >= 0f && viewport.y <= 1f;

            if (!clampToEdge)
            {
                // No edge clamping wanted (e.g. player labels with their
                // off-screen indicator disabled): report the raw, unclamped
                // position so the caller can judge visibility off the
                // label's own footprint instead of this point alone.
                if (behindCamera)
                {
                    // Behind the camera, Unity's viewport coords mirror to
                    // the opposite side - push far past the canvas in the
                    // mirrored-back direction rather than at some on-screen
                    // placeholder the label would appear frozen at.
                    Vector2 mirroredFromCenter = new Vector2(
                        (0.5f - viewport.x) * canvasSize.x,
                        (0.5f - viewport.y) * canvasSize.y);
                    if (mirroredFromCenter.sqrMagnitude < 0.0001f)
                    {
                        mirroredFromCenter = Vector2.up;
                    }
                    Vector2 farPosition = mirroredFromCenter.normalized * (canvasSize.magnitude + 10000f);
                    return new IndicatorState
                    {
                        CanvasPosition = farPosition,
                        IsOffScreen = true,
                        ArrowAngleDegrees = 0f,
                    };
                }

                return new IndicatorState
                {
                    CanvasPosition = ViewportToCanvas(viewport, canvasSize),
                    IsOffScreen = !withinBounds,
                    ArrowAngleDegrees = 0f,
                };
            }

            if (!behindCamera && withinBounds)
            {
                // Inset by the same margin as the off-screen case below even
                // though this point is on-screen - a raw projection can land
                // right at the pixel edge, clipping a wide label centered on
                // it. Only nudges points already near the border.
                Vector2 position = ViewportToCanvas(viewport, canvasSize);
                float halfWidth = Mathf.Max(canvasSize.x * 0.5f - edgeMarginPixels, 1f);
                float halfHeight = Mathf.Max(canvasSize.y * 0.5f - edgeMarginPixels, 1f);
                position.x = Mathf.Clamp(position.x, -halfWidth, halfWidth);
                position.y = Mathf.Clamp(position.y, -halfHeight, halfHeight);

                return new IndicatorState
                {
                    CanvasPosition = position,
                    IsOffScreen = false,
                    ArrowAngleDegrees = 0f,
                };
            }

            // Direction from canvas center to the (possibly out-of-bounds)
            // viewport point. A point behind the camera projects to the
            // opposite side, so mirror its direction back through center.
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

        // Scales a center-relative direction out to the boundary of a
        // centered rectangle (canvasSize, inset by edgeMarginPixels).
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
