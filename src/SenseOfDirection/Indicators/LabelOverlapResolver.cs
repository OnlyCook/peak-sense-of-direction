using System.Collections.Generic;
using UnityEngine;

namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// Shared overlap-resolution core for both <see cref="IndicatorManager"/>'s
    /// edge-of-screen labels and <see cref="Compass.CompassManager"/>'s tape
    /// markers. Given label boxes at their "natural" (world-tracked) position,
    /// returns a per-label offset that clears any overlap.
    ///
    /// Works in three steps:
    ///
    /// 1. <b>Cluster.</b> Only labels that actually collide are considered
    ///    together; everything else keeps its exact tracked position.
    /// 2. <b>Place.</b> Each cluster is spread along the one axis it's allowed
    ///    to move on by an isotonic regression (pool-adjacent-violators), which
    ///    is the exact minimiser of total squared displacement subject to
    ///    keeping the labels in the order they already appear in. Labels split
    ///    apart around the middle of their own cluster instead of all sliding
    ///    the same way, so each moves about half as far and a stack of three or
    ///    more still clears within its offset cap.
    /// 3. <b>Stagger</b> (compass only, see <paramref name="rowStaggerPixels"/>).
    ///    Markers on the tape can only spread sideways, and a crowded 640px tape
    ///    physically cannot fit four named markers on one row at any cap. A
    ///    cluster that can't fit alternates its members onto a second (and
    ///    third) row below the tape, and each row is then spread on its own.
    ///
    /// The result depends only on the labels' positions and sizes - never on
    /// registration order, and never on which particular neighbour happens to be
    /// conflicting. That's what keeps it from drifting or misplacing itself over
    /// a few seconds as unrelated anchors come and go, which is what sank the
    /// earlier greedy version (one fixed push direction per axis, priority by
    /// list order): that one also spent each label's whole offset budget pushing
    /// a single way, so three stacked labels ran out of budget before they
    /// cleared and just stayed piled up.
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
        /// Default cap on how far a label ever gets nudged from its real tracked
        /// position. If clearing every overlap would need more than this, the
        /// remaining overlap is simply left as-is rather than shoving the label
        /// far enough that it stops reading as belonging to its own icon.
        /// Callers can pass a per-label cap instead (see the <c>maxOffsets</c>
        /// overload): a label that moves as a whole (a player label, the
        /// campfire) can afford to travel further than one whose text is sliding
        /// away from an arrow/crosshair that stays behind.
        /// </summary>
        public const float MaxOffsetMagnitude = 56f;

        /// <summary>Extra breathing room between two resolved boxes, beyond just touching edges.</summary>
        private const float Padding = 3f;

        public static Vector2[] ComputeOffsets(
            IReadOnlyList<Vector2> basePositions,
            IReadOnlyList<Vector2> sizes,
            Axis axis,
            float maxOffsetMagnitude = MaxOffsetMagnitude,
            float rowStaggerPixels = 0f,
            int maxRows = 1)
        {
            var caps = new float[basePositions.Count];
            for (int i = 0; i < caps.Length; i++)
            {
                caps[i] = maxOffsetMagnitude;
            }
            return ComputeOffsets(basePositions, sizes, axis, caps, rowStaggerPixels, maxRows);
        }

        /// <summary>
        /// <paramref name="basePositions"/> are the label <em>boxes'</em>
        /// centres. An anchor whose box doesn't sit centred on its tracked point
        /// (a player label runs from its crown badge down to its status badge)
        /// passes the box centre here; what comes back is a plain delta either
        /// way, so it applies to the widget unchanged.
        /// <paramref name="maxOffsets"/> is the per-label cap.
        /// <paramref name="rowStaggerPixels"/> above zero lets a cluster that
        /// can't fit along <paramref name="axis"/> alternate onto up to
        /// <paramref name="maxRows"/> rows, offset along the other axis.
        /// </summary>
        public static Vector2[] ComputeOffsets(
            IReadOnlyList<Vector2> basePositions,
            IReadOnlyList<Vector2> sizes,
            Axis axis,
            IReadOnlyList<float> maxOffsets,
            float rowStaggerPixels = 0f,
            int maxRows = 1)
        {
            int count = basePositions.Count;
            var offsets = new Vector2[count];
            bool vertical = axis == Axis.Vertical;
            if (rowStaggerPixels <= 0f)
            {
                maxRows = 1;
            }

            var live = new List<int>();
            for (int i = 0; i < count; i++)
            {
                if (sizes[i].x > 0f && sizes[i].y > 0f)
                {
                    live.Add(i);
                }
            }

            foreach (List<int> cluster in FindClusters(live, basePositions, sizes))
            {
                if (cluster.Count < 2)
                {
                    continue;
                }

                // Descending along the movement axis, so the first entry is the
                // topmost label (vertical) / the rightmost marker (horizontal).
                // Ties broken by the other axis, so the order is fully determined
                // by geometry rather than by registration order.
                cluster.Sort((i, j) =>
                {
                    int byMain = Main(basePositions[j], vertical).CompareTo(Main(basePositions[i], vertical));
                    return byMain != 0
                        ? byMain
                        : Cross(basePositions[i], vertical).CompareTo(Cross(basePositions[j], vertical));
                });

                int rows = maxRows > 1
                    ? RowsNeeded(cluster, basePositions, sizes, maxOffsets, vertical, maxRows)
                    : 1;

                // Adjacent members land on different rows, so each row's own
                // members start out roughly `rows` times further apart than the
                // cluster as a whole - which is what makes them fit.
                for (int row = 0; row < rows; row++)
                {
                    var rowMembers = new List<int>();
                    for (int k = row; k < cluster.Count; k += rows)
                    {
                        rowMembers.Add(cluster[k]);
                    }

                    float[] resolved = rowMembers.Count > 1
                        ? IsotonicPlace(rowMembers, basePositions, sizes, vertical)
                        : new[] { Main(basePositions[rowMembers[0]], vertical) };

                    for (int k = 0; k < rowMembers.Count; k++)
                    {
                        int index = rowMembers[k];
                        float cap = maxOffsets[index];
                        float delta = Mathf.Clamp(resolved[k] - Main(basePositions[index], vertical), -cap, cap);
                        offsets[index] = vertical
                            ? new Vector2(-row * rowStaggerPixels, delta)
                            : new Vector2(delta, -row * rowStaggerPixels);
                    }
                }
            }

            return offsets;
        }

        /// <summary>
        /// Minimal-displacement, order-preserving placement of one cluster along
        /// its movement axis. Adding each label's required cumulative separation
        /// turns "keep them in this order, at least this far apart" into "this
        /// sequence must be non-increasing", which pool-adjacent-violators solves
        /// exactly by merging every violating run into its own mean.
        /// </summary>
        private static float[] IsotonicPlace(List<int> members, IReadOnlyList<Vector2> basePositions, IReadOnlyList<Vector2> sizes, bool vertical)
        {
            int n = members.Count;
            var cumulative = new float[n];
            for (int k = 1; k < n; k++)
            {
                cumulative[k] = cumulative[k - 1] + Separation(sizes[members[k - 1]], sizes[members[k]], vertical);
            }

            var sums = new float[n];
            var counts = new int[n];
            var means = new float[n];
            int blocks = 0;

            for (int k = 0; k < n; k++)
            {
                float sum = Main(basePositions[members[k]], vertical) + cumulative[k];
                int size = 1;

                while (blocks > 0 && means[blocks - 1] < sum / size)
                {
                    blocks--;
                    sum += sums[blocks];
                    size += counts[blocks];
                }

                sums[blocks] = sum;
                counts[blocks] = size;
                means[blocks] = sum / size;
                blocks++;
            }

            var resolved = new float[n];
            int at = 0;
            for (int b = 0; b < blocks; b++)
            {
                for (int k = 0; k < counts[b]; k++, at++)
                {
                    resolved[at] = means[b] - cumulative[at];
                }
            }
            return resolved;
        }

        /// <summary>
        /// How many rows this cluster needs: the span it would have to occupy on
        /// a single row, against the span it's actually allowed to use (its own
        /// extent, widened by the caps of the labels at either end).
        /// </summary>
        private static int RowsNeeded(List<int> cluster, IReadOnlyList<Vector2> basePositions, IReadOnlyList<Vector2> sizes, IReadOnlyList<float> maxOffsets, bool vertical, int maxRows)
        {
            float required = 0f;
            for (int k = 1; k < cluster.Count; k++)
            {
                required += Separation(sizes[cluster[k - 1]], sizes[cluster[k]], vertical);
            }

            int first = cluster[0];
            int last = cluster[cluster.Count - 1];
            float extent = Mathf.Abs(Main(basePositions[first], vertical) - Main(basePositions[last], vertical));
            float available = Mathf.Max(extent + maxOffsets[first] + maxOffsets[last], 1f);

            return Mathf.Clamp(Mathf.CeilToInt(required / available), 1, maxRows);
        }

        /// <summary>Connected components over "these two labels actually collide" (union-find).</summary>
        private static IEnumerable<List<int>> FindClusters(List<int> live, IReadOnlyList<Vector2> basePositions, IReadOnlyList<Vector2> sizes)
        {
            var parent = new Dictionary<int, int>();
            foreach (int i in live)
            {
                parent[i] = i;
            }

            for (int a = 0; a < live.Count; a++)
            {
                for (int b = a + 1; b < live.Count; b++)
                {
                    int i = live[a], j = live[b];
                    if (!Overlaps(basePositions[i], sizes[i], basePositions[j], sizes[j]))
                    {
                        continue;
                    }

                    int rootA = Find(parent, i);
                    int rootB = Find(parent, j);
                    if (rootA != rootB)
                    {
                        parent[rootA] = rootB;
                    }
                }
            }

            var clusters = new Dictionary<int, List<int>>();
            foreach (int i in live)
            {
                int root = Find(parent, i);
                if (!clusters.TryGetValue(root, out List<int> members))
                {
                    members = new List<int>();
                    clusters[root] = members;
                }
                members.Add(i);
            }
            return clusters.Values;
        }

        private static int Find(Dictionary<int, int> parent, int i)
        {
            while (parent[i] != i)
            {
                parent[i] = parent[parent[i]];
                i = parent[i];
            }
            return i;
        }

        private static float Main(Vector2 v, bool vertical) => vertical ? v.y : v.x;

        private static float Cross(Vector2 v, bool vertical) => vertical ? v.x : v.y;

        private static float Separation(Vector2 sizeA, Vector2 sizeB, bool vertical) =>
            (Main(sizeA, vertical) + Main(sizeB, vertical)) * 0.5f + Padding;

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
