using System;
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
    /// 3. <b>Overflow onto a second line</b> (opt-in, see
    ///    <paramref name="rowStaggerPixels"/>). A cluster too crowded to fit
    ///    along its one axis at any cap spills onto another line offset along the
    ///    other axis. Two shapes: the compass tape (short markers) interleaves
    ///    alternate members onto stacked rows straight below it; edge labels (too
    ///    tall/wide to interleave without gaps) fill one line densely and peel
    ///    just the overflow onto a second line stepped toward the screen centre
    ///    (<paramref name="densePack"/>).
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

        /// <summary>Gap left between a crowded edge stack's primary and overflow lines, on top of the widest label's cross-axis size (see <see cref="PackOverflowLines"/>).</summary>
        private const float OverflowLineGap = 10f;

        /// <summary>
        /// Every working buffer below - including the one <see cref="ComputeOffsets"/>
        /// hands back - is reused between calls rather than freshly allocated.
        /// This runs every frame, twice (once for the screen labels, once for
        /// the compass tape), over however many labels are live, and pings are
        /// what put labels on screen: the garbage it produced therefore scaled
        /// with exactly the thing ISSUES.md wants never to cost anything (the
        /// more you ping, the more it allocated, the more often the GC ran).
        ///
        /// The returned array is shared and only valid until the next call -
        /// both callers read it immediately, inside the same loop that asked
        /// for it, which is what makes this safe.
        /// </summary>
        private static Vector2[] _offsets = new Vector2[16];
        private static float[] _caps = new float[16];
        private static readonly List<int> _live = new List<int>();
        private static readonly Dictionary<int, int> _parent = new Dictionary<int, int>();
        private static readonly Dictionary<int, List<int>> _clusters = new Dictionary<int, List<int>>();
        private static readonly List<List<int>> _clusterValues = new List<List<int>>();
        private static readonly List<List<int>> _intListPool = new List<List<int>>();
        private static readonly List<int> _rowMembers = new List<int>();
        private static float[] _cumulative = new float[16];
        private static float[] _sums = new float[16];
        private static int[] _counts = new int[16];
        private static float[] _means = new float[16];
        private static float[] _resolved = new float[16];

        /// <summary>Which cluster members stay on the primary line vs. spill to the overflow line, split by <see cref="PackOverflowLines"/>. Reused between calls like every other buffer here.</summary>
        private static readonly List<int> _keepMembers = new List<int>();
        private static readonly List<int> _overflowMembers = new List<int>();

        /// <summary>Sort state for <see cref="ClusterComparison"/> - a static delegate over static state, so ordering a cluster doesn't allocate a closure per cluster per frame.</summary>
        private static IReadOnlyList<Vector2> _sortPositions;
        private static bool _sortVertical;

        private static readonly Comparison<int> ClusterComparison = (i, j) =>
        {
            int byMain = Main(_sortPositions[j], _sortVertical).CompareTo(Main(_sortPositions[i], _sortVertical));
            return byMain != 0
                ? byMain
                : Cross(_sortPositions[i], _sortVertical).CompareTo(Cross(_sortPositions[j], _sortVertical));
        };

        public static Vector2[] ComputeOffsets(
            IReadOnlyList<Vector2> basePositions,
            IReadOnlyList<Vector2> sizes,
            Axis axis,
            float maxOffsetMagnitude = MaxOffsetMagnitude,
            float rowStaggerPixels = 0f,
            int maxRows = 1,
            bool densePack = false)
        {
            EnsureCapacity(ref _caps, basePositions.Count);
            for (int i = 0; i < basePositions.Count; i++)
            {
                _caps[i] = maxOffsetMagnitude;
            }
            return ComputeOffsets(basePositions, sizes, axis, _caps, rowStaggerPixels, maxRows, densePack);
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
        /// <paramref name="densePack"/> switches the overflow strategy from
        /// interleaving (the compass tape's short markers) to filling one line
        /// densely and spilling only what won't fit onto a second line stepped
        /// toward the screen centre (edge labels, which are too tall/wide to
        /// interleave without leaving a label-sized hole beside each entry).
        /// </summary>
        public static Vector2[] ComputeOffsets(
            IReadOnlyList<Vector2> basePositions,
            IReadOnlyList<Vector2> sizes,
            Axis axis,
            IReadOnlyList<float> maxOffsets,
            float rowStaggerPixels = 0f,
            int maxRows = 1,
            bool densePack = false)
        {
            int count = basePositions.Count;
            EnsureCapacity(ref _offsets, count);
            for (int i = 0; i < count; i++)
            {
                _offsets[i] = Vector2.zero;
            }

            bool vertical = axis == Axis.Vertical;
            // Interleaving needs a caller-supplied row pitch to stack rows by;
            // dense packing derives its own overflow-line offset from the labels'
            // widths, so it only needs maxRows > 1 to be allowed a second line.
            if (!densePack && rowStaggerPixels <= 0f)
            {
                maxRows = 1;
            }

            _live.Clear();
            for (int i = 0; i < count; i++)
            {
                if (sizes[i].x > 0f && sizes[i].y > 0f)
                {
                    _live.Add(i);
                }
            }

            FindClusters(_live, basePositions, sizes, maxOffsets, vertical);

            _sortPositions = basePositions;
            _sortVertical = vertical;

            foreach (List<int> cluster in _clusterValues)
            {
                if (cluster.Count < 2)
                {
                    continue;
                }

                // Descending along the movement axis, so the first entry is the
                // topmost label (vertical) / the rightmost marker (horizontal).
                // Ties broken by the other axis, so the order is fully determined
                // by geometry rather than by registration order.
                cluster.Sort(ClusterComparison);

                int rows = maxRows > 1
                    ? RowsNeeded(cluster, basePositions, sizes, maxOffsets, vertical, maxRows)
                    : 1;

                // A crowded edge stack fills one line densely and spills only the
                // overflow onto a second line stepped toward the screen centre,
                // rather than interleaving. Interleaving - which the compass tape
                // does with its short markers - leaves a label-sized hole beside
                // every entry when the labels are this big, because the slot
                // between two of a line's entries belongs to the other line and is
                // offset off to the side. See PackOverflowLines.
                if (densePack && rows > 1)
                {
                    PackOverflowLines(cluster, basePositions, sizes, maxOffsets, vertical);
                    continue;
                }

                // Single column (vertical, rows == 1) or the compass tape
                // (horizontal): interleave onto `rows` rows and spread each row on
                // its own. Adjacent members land on different rows, so each row's
                // members start out roughly `rows` times further apart than the
                // cluster as a whole - which is what makes them fit.
                for (int row = 0; row < rows; row++)
                {
                    _rowMembers.Clear();
                    for (int k = row; k < cluster.Count; k += rows)
                    {
                        _rowMembers.Add(cluster[k]);
                    }

                    if (_rowMembers.Count > 1)
                    {
                        IsotonicPlace(_rowMembers, basePositions, sizes, vertical);
                    }
                    else
                    {
                        EnsureCapacity(ref _resolved, 1);
                        _resolved[0] = Main(basePositions[_rowMembers[0]], vertical);
                    }

                    for (int k = 0; k < _rowMembers.Count; k++)
                    {
                        int index = _rowMembers[k];
                        float cap = maxOffsets[index];
                        float delta = Mathf.Clamp(_resolved[k] - Main(basePositions[index], vertical), -cap, cap);
                        _offsets[index] = vertical
                            ? new Vector2(0f, delta)
                            : new Vector2(delta, -row * rowStaggerPixels);
                    }
                }
            }

            _sortPositions = null;
            return _offsets;
        }

        /// <summary>
        /// All-zero offsets ("nobody moves"), for callers whose overlap
        /// avoidance is switched off - they still smooth towards the returned
        /// target every frame, so they need a real array, just not a fresh one
        /// each frame. Same shared-buffer contract as <see cref="ComputeOffsets"/>:
        /// valid until the next call.
        /// </summary>
        public static Vector2[] ZeroOffsets(int count)
        {
            EnsureCapacity(ref _offsets, count);
            for (int i = 0; i < count; i++)
            {
                _offsets[i] = Vector2.zero;
            }
            return _offsets;
        }

        private static void EnsureCapacity<T>(ref T[] buffer, int count)
        {
            if (buffer.Length < count)
            {
                buffer = new T[Mathf.NextPowerOfTwo(count)];
            }
        }

        /// <summary>
        /// Minimal-displacement, order-preserving placement of one cluster along
        /// its movement axis. Adding each label's required cumulative separation
        /// turns "keep them in this order, at least this far apart" into "this
        /// sequence must be non-increasing", which pool-adjacent-violators solves
        /// exactly by merging every violating run into its own mean.
        /// </summary>
        /// <summary>Writes its result into the shared <see cref="_resolved"/> buffer (indices 0..members.Count), read by the caller straight away.</summary>
        private static void IsotonicPlace(List<int> members, IReadOnlyList<Vector2> basePositions, IReadOnlyList<Vector2> sizes, bool vertical)
        {
            int n = members.Count;
            EnsureCapacity(ref _cumulative, n);
            EnsureCapacity(ref _sums, n);
            EnsureCapacity(ref _counts, n);
            EnsureCapacity(ref _means, n);
            EnsureCapacity(ref _resolved, n);

            _cumulative[0] = 0f;
            for (int k = 1; k < n; k++)
            {
                _cumulative[k] = _cumulative[k - 1] + Separation(sizes[members[k - 1]], sizes[members[k]], vertical);
            }

            int blocks = 0;

            for (int k = 0; k < n; k++)
            {
                float sum = Main(basePositions[members[k]], vertical) + _cumulative[k];
                int size = 1;

                while (blocks > 0 && _means[blocks - 1] < sum / size)
                {
                    blocks--;
                    sum += _sums[blocks];
                    size += _counts[blocks];
                }

                _sums[blocks] = sum;
                _counts[blocks] = size;
                _means[blocks] = sum / size;
                blocks++;
            }

            int at = 0;
            for (int b = 0; b < blocks; b++)
            {
                for (int k = 0; k < _counts[b]; k++, at++)
                {
                    _resolved[at] = _means[b] - _cumulative[at];
                }
            }
        }

        /// <summary>
        /// Places one crowded edge cluster on up to two lines. The primary line
        /// spreads by the usual minimal-displacement isotonic pass; any label it
        /// couldn't fit within its cap is peeled onto a second line, spread among
        /// its fellow overflows on its own and stepped toward the screen centre
        /// along the cross axis (so it never crosses the crosshair or runs off the
        /// near edge). Both lines still use isotonic, so this stays as stable
        /// frame-to-frame as the single-line case - only which side of its cap a
        /// borderline label sits on can flip it between lines.
        ///
        /// The primary line stays the fuller one as long as the per-label cap is
        /// generous enough to hold most of the stack near the edge (which it is by
        /// design - the second line is the fallback for the few that would have to
        /// travel too far from their true position). The overflow line's inward
        /// step is sized from the widest label actually present rather than a fixed
        /// pitch, so narrow labels sit close and wide ones ("2x PAWN") never let a
        /// primary label's text collide with an overflow label's.
        /// </summary>
        private static void PackOverflowLines(List<int> cluster, IReadOnlyList<Vector2> basePositions, IReadOnlyList<Vector2> sizes, IReadOnlyList<float> maxOffsets, bool vertical)
        {
            // Trial the whole cluster on one line, then split off whoever the cap
            // wouldn't let fit there. Also collect the widest cross-axis size (the
            // overflow line's inward step) and where the cluster sits (which way
            // that step and the overflow go - toward the centre).
            IsotonicPlace(cluster, basePositions, sizes, vertical);
            _keepMembers.Clear();
            _overflowMembers.Clear();
            float maxCross = 0f;
            float crossSum = 0f;
            for (int k = 0; k < cluster.Count; k++)
            {
                int idx = cluster[k];
                maxCross = Mathf.Max(maxCross, Cross(sizes[idx], vertical));
                crossSum += Cross(basePositions[idx], vertical);
                float delta = _resolved[k] - Main(basePositions[idx], vertical);
                if (Mathf.Abs(delta) > maxOffsets[idx])
                {
                    _overflowMembers.Add(idx);
                }
                else
                {
                    _keepMembers.Add(idx);
                }
            }

            // Everyone fit on one line - apply that trial straight off.
            if (_overflowMembers.Count == 0)
            {
                ApplyLine(cluster, basePositions, sizes, maxOffsets, vertical, 0f);
                return;
            }

            // A full widest-label width apart (plus a little air) clears any pairing
            // of primary and overflow labels, since that's >= the sum of any two
            // half-widths; stepped toward the canvas centre from whichever edge the
            // stack sits on.
            float crossOffset = (crossSum > 0f ? -1f : 1f) * (maxCross + OverflowLineGap);

            // Re-spread the primary line without the overflow (it has more room
            // now), then the overflow line on its own, inset toward centre.
            ApplyLine(_keepMembers, basePositions, sizes, maxOffsets, vertical, 0f);
            ApplyLine(_overflowMembers, basePositions, sizes, maxOffsets, vertical, crossOffset);
        }

        /// <summary>
        /// Isotonic-spreads one line's members along the main axis and writes each
        /// one's clamped offset (with <paramref name="crossOffset"/> applied on the
        /// cross axis) into <see cref="_offsets"/>.
        /// </summary>
        private static void ApplyLine(List<int> members, IReadOnlyList<Vector2> basePositions, IReadOnlyList<Vector2> sizes, IReadOnlyList<float> maxOffsets, bool vertical, float crossOffset)
        {
            if (members.Count == 0)
            {
                return;
            }

            IsotonicPlace(members, basePositions, sizes, vertical);
            for (int k = 0; k < members.Count; k++)
            {
                int idx = members[k];
                float cap = maxOffsets[idx];
                float delta = Mathf.Clamp(_resolved[k] - Main(basePositions[idx], vertical), -cap, cap);
                _offsets[idx] = vertical
                    ? new Vector2(crossOffset, delta)
                    : new Vector2(delta, crossOffset);
            }
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

        /// <summary>
        /// Connected components over "these two labels could collide" (union-find).
        /// Fills the shared <see cref="_clusterValues"/> with the clusters found;
        /// the member lists themselves come from - and go back to -
        /// <see cref="_intListPool"/>, so a frame's worth of clustering allocates
        /// nothing once the pool has warmed up.
        ///
        /// The overlap test widens each box along the movement axis by that
        /// label's own offset cap (<paramref name="maxOffsets"/>), so two labels
        /// end up in the same cluster whenever <em>resolving</em> one could push
        /// it into the other - not only when they already overlap where they sit.
        /// Clustering on the bare (unwidened) boxes was unsound: a label pushed
        /// aside to clear its own cluster-mate could land on a neighbour that was
        /// never in the cluster and so never got a chance to move out of the way
        /// (the "Rook shoved into 2x QUEEN" case). Placement/spacing still use the
        /// real box sizes; only which labels get solved together is widened here,
        /// and the isotonic pass leaves any label it doesn't actually need to move
        /// exactly where it was, so over-grouping costs nothing.
        /// </summary>
        private static void FindClusters(List<int> live, IReadOnlyList<Vector2> basePositions, IReadOnlyList<Vector2> sizes, IReadOnlyList<float> maxOffsets, bool vertical)
        {
            foreach (List<int> spent in _clusterValues)
            {
                spent.Clear();
                _intListPool.Add(spent);
            }
            _clusterValues.Clear();
            _clusters.Clear();
            _parent.Clear();

            foreach (int i in live)
            {
                _parent[i] = i;
            }

            for (int a = 0; a < live.Count; a++)
            {
                for (int b = a + 1; b < live.Count; b++)
                {
                    int i = live[a], j = live[b];
                    if (!OverlapsWithReach(basePositions[i], sizes[i], maxOffsets[i], basePositions[j], sizes[j], maxOffsets[j], vertical))
                    {
                        continue;
                    }

                    int rootA = Find(_parent, i);
                    int rootB = Find(_parent, j);
                    if (rootA != rootB)
                    {
                        _parent[rootA] = rootB;
                    }
                }
            }

            foreach (int i in live)
            {
                int root = Find(_parent, i);
                if (!_clusters.TryGetValue(root, out List<int> members))
                {
                    members = RentIntList();
                    _clusters[root] = members;
                    _clusterValues.Add(members);
                }
                members.Add(i);
            }
        }

        private static List<int> RentIntList()
        {
            if (_intListPool.Count == 0)
            {
                return new List<int>();
            }
            List<int> list = _intListPool[_intListPool.Count - 1];
            _intListPool.RemoveAt(_intListPool.Count - 1);
            return list;
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

        /// <summary>
        /// Like <see cref="Overlaps"/>, but each box is grown along the movement
        /// axis by its own offset cap first, so it reports true whenever the two
        /// labels <em>could</em> be pushed into each other, not just when they
        /// already touch. Cross axis is compared at true size (a label never
        /// moves that way). See <see cref="FindClusters"/> for why.
        /// </summary>
        private static bool OverlapsWithReach(Vector2 posA, Vector2 sizeA, float capA, Vector2 posB, Vector2 sizeB, float capB, bool vertical)
        {
            if (sizeA.x <= 0f || sizeA.y <= 0f || sizeB.x <= 0f || sizeB.y <= 0f)
            {
                return false;
            }

            float mainReach = (Main(sizeA, vertical) + Main(sizeB, vertical)) * 0.5f + capA + capB;
            float crossReach = (Cross(sizeA, vertical) + Cross(sizeB, vertical)) * 0.5f;
            float mainDelta = Mathf.Abs(Main(posA, vertical) - Main(posB, vertical));
            float crossDelta = Mathf.Abs(Cross(posA, vertical) - Cross(posB, vertical));
            return mainDelta < mainReach && crossDelta < crossReach;
        }

    }
}
