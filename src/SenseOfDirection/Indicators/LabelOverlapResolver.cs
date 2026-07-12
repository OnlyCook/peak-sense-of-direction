using System.Collections.Generic;
using UnityEngine;

namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// Shared overlap-resolution core for both <see cref="IndicatorManager"/>'s
    /// edge-of-screen labels and <see cref="Compass.CompassManager"/>'s tape
    /// markers. Given a priority-ordered list of label boxes at their
    /// "natural" (world-tracked) position, returns a per-label offset that
    /// nudges anything overlapping an earlier (higher-priority) label out of
    /// the way - earlier entries never move.
    ///
    /// Deliberately the simplest version that reads well without breaking:
    /// single axis per call, one fixed, predictable push direction per axis
    /// (always down for vertical, always away from screen/tape center for
    /// horizontal) - not alternating, not per-line, not based on which
    /// specific neighbor is conflicting. Earlier, more "clever" versions
    /// (vertical-then-horizontal fallback, per-line name/distance splitting,
    /// conflict-relative direction) each fixed one visual complaint while
    /// introducing a worse one - a diagonal jumble, a huge gap between an
    /// icon and its distance line, or the whole thing misplacing itself over
    /// a few seconds as unrelated anchors came and went. A single fixed
    /// direction can never do that: it doesn't depend on list order, doesn't
    /// depend on which other label happens to be conflicting, and always
    /// converges instead of potentially pushing towards a neighbor it should
    /// be moving away from.
    ///
    /// Deliberately no snapping: callers are expected to smooth the returned
    /// offset towards its previous value themselves (see
    /// <see cref="IndicatorManager"/>/<see cref="Compass.CompassManager"/>'s own
    /// <c>Vector2.MoveTowards</c> usage) rather than applying it directly, so
    /// a label popping in/out of overlap doesn't jump.
    /// </summary>
    public static class LabelOverlapResolver
    {
        public enum Axis
        {
            Vertical,
            Horizontal,
        }

        /// <summary>
        /// Hard cap on how far any single label ever gets nudged from its
        /// real tracked position. If clearing every overlap would need more
        /// than this, the remaining overlap is simply left as-is rather than
        /// shoving the label far enough that it stops reading as belonging
        /// to its own icon.
        /// </summary>
        public const float MaxOffsetMagnitude = 56f;

        /// <summary>
        /// Loop-iteration safety cap, independent of <see cref="MaxOffsetMagnitude"/>
        /// (which already bounds total displacement) - just prevents a
        /// pathological case (many same-position labels with a tiny
        /// remaining budget) from looping many times over near-zero pushes.
        /// </summary>
        public const int MaxConcurrentOverlaps = 5;

        /// <summary>Extra breathing room between two resolved boxes, beyond just touching edges.</summary>
        private const float Padding = 3f;

        /// <summary>
        /// <paramref name="basePositions"/>/<paramref name="sizes"/> must be
        /// ordered by descending priority (index 0 keeps its position
        /// always). Each label only moves by the minimum distance actually
        /// needed to clear whatever it's overlapping (not a fixed full-box
        /// jump), never by more than <paramref name="maxOffsetMagnitude"/> in
        /// total, always in the same fixed direction for that axis (down for
        /// vertical; away from 0 - screen/tape center - for horizontal).
        /// </summary>
        public static Vector2[] ComputeOffsets(IReadOnlyList<Vector2> basePositions, IReadOnlyList<Vector2> sizes, Axis axis, float maxOffsetMagnitude = MaxOffsetMagnitude)
        {
            int count = basePositions.Count;
            var resolvedPositions = new Vector2[count];
            var offsets = new Vector2[count];
            bool vertical = axis == Axis.Vertical;

            for (int i = 0; i < count; i++)
            {
                Vector2 pos = basePositions[i];
                Vector2 size = sizes[i];
                float budget = maxOffsetMagnitude;
                // Vertical always pushes down; horizontal always pushes away
                // from center (0) - a fixed, position-derived direction that
                // never depends on which neighbor happens to be conflicting
                // or on this entry's index/registration order, so it can't
                // flip and make things worse as unrelated anchors elsewhere
                // come and go.
                float direction = vertical ? -1f : (pos.x >= 0f ? 1f : -1f);

                int pushes = 0;
                bool movedAgain = true;
                while (movedAgain && pushes < MaxConcurrentOverlaps && budget > 0f)
                {
                    movedAgain = false;
                    for (int j = 0; j < i; j++)
                    {
                        Vector2 other = resolvedPositions[j];
                        Vector2 otherSize = sizes[j];
                        if (!Overlaps(pos, size, other, otherSize))
                        {
                            continue;
                        }

                        pushes++;
                        if (vertical)
                        {
                            float halfHeightSum = size.y * 0.5f + otherSize.y * 0.5f;
                            float penetration = halfHeightSum - Mathf.Abs(pos.y - other.y);
                            float step = Mathf.Min(penetration + Padding, budget);
                            pos.y += direction * step;
                            budget -= step;
                        }
                        else
                        {
                            float halfWidthSum = size.x * 0.5f + otherSize.x * 0.5f;
                            float penetration = halfWidthSum - Mathf.Abs(pos.x - other.x);
                            float step = Mathf.Min(penetration + Padding, budget);
                            pos.x += direction * step;
                            budget -= step;
                        }

                        movedAgain = true;
                        break;
                    }
                }

                resolvedPositions[i] = pos;
                offsets[i] = pos - basePositions[i];
            }

            return offsets;
        }

        private static bool Overlaps(Vector2 posA, Vector2 sizeA, Vector2 posB, Vector2 sizeB)
        {
            if (sizeA.x <= 0f || sizeA.y <= 0f || sizeB.x <= 0f || sizeB.y <= 0f)
            {
                return false;
            }

            float halfWidthSum = sizeA.x * 0.5f + sizeB.x * 0.5f;
            float halfHeightSum = sizeA.y * 0.5f + sizeB.y * 0.5f;
            return Mathf.Abs(posA.x - posB.x) < halfWidthSum && Mathf.Abs(posA.y - posB.y) < halfHeightSum;
        }
    }
}
