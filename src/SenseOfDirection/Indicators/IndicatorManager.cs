using System.Collections.Generic;
using UnityEngine;

namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// Owns the single full-screen overlay canvas used for every edge-of-screen
    /// indicator (player labels, pings, campfire), and drives each registered
    /// <see cref="IndicatorAnchor"/>'s widget to the right on-screen or
    /// clamped-edge position every frame via <see cref="ScreenSpaceTracker"/>.
    ///
    /// Lazily created on first use and kept alive for the process lifetime
    /// (DontDestroyOnLoad) - individual mechanics register/unregister anchors
    /// as their own tracked objects (players, pings, ...) come and go.
    /// </summary>
    public class IndicatorManager : MonoBehaviour
    {
        private static IndicatorManager _instance;

        public static IndicatorManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.IndicatorManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<IndicatorManager>();
                    _instance.BuildLiveCanvas();
                }
                return _instance;
            }
        }

        /// <summary>
        /// A second, non-singleton manager driving the exact same anchor/widget
        /// machinery into somewhere other than the real screen: used by the
        /// config preview menu (<c>Ui.PreviewScene</c>), which renders the mod's
        /// real widgets against a fake camera inside a panel.
        ///
        /// Everything below - edge clamping, the on/off-screen transition,
        /// overlap resolution - is resolution-independent already
        /// (<see cref="ScreenSpaceTracker"/> works in viewport space, and the
        /// canvas size is a parameter, not <c>Screen.width/height</c>), so the
        /// preview gets the real behaviour rather than a lookalike
        /// reimplementation that could drift out of sync with it.
        /// </summary>
        /// <param name="surface">Widgets are parented here, and its rect size stands in for the screen.</param>
        /// <param name="camera">Projected against instead of <see cref="Camera.main"/>.</param>
        public static IndicatorManager CreateDetached(RectTransform surface, Camera camera)
        {
            var go = new GameObject("SenseOfDirection.IndicatorManager.Detached");
            go.transform.SetParent(surface, false);

            var manager = go.AddComponent<IndicatorManager>();
            manager._detached = true;
            manager._cameraOverride = camera;
            manager.CanvasTransform = surface;
            return manager;
        }

        /// <summary>Parent every registered anchor's widget under this.</summary>
        public RectTransform CanvasTransform { get; private set; }

        /// <summary>Set on a <see cref="CreateDetached"/> instance: it owns no canvas of its own and never touches the game's HUD sorting order.</summary>
        private bool _detached;

        /// <summary>Null on the live instance, which tracks <see cref="Camera.main"/>.</summary>
        private Camera _cameraOverride;

        /// <summary>Read-only view for <see cref="Compass.CompassManager"/>, which drives its own top-of-screen markers off the same registered anchors instead of requiring a second registration call per mechanic.</summary>
        public IReadOnlyList<IndicatorAnchor> Anchors => _anchors;

        private readonly List<IndicatorAnchor> _anchors = new List<IndicatorAnchor>();
        private Canvas _canvas;

        /// <summary>Pixels/second the resolved overlap offset (see <see cref="LabelOverlapResolver"/>) is smoothed towards its target at - keeps labels sliding apart/back together instead of snapping as overlap starts/stops.</summary>
        private const float OverlapOffsetSpeedPixelsPerSecond = 240f;

        /// <summary>
        /// A crowded edge stack may fan out into at most this many lines - the
        /// primary line along the edge plus one overflow line inset toward the
        /// screen centre, so it never reaches far enough in to touch the crosshair.
        /// The overflow line's inward step is sized to the labels themselves inside
        /// <see cref="LabelOverlapResolver"/>, not fixed here.
        /// </summary>
        private const int EdgeLabelMaxLines = 2;

        /// <summary>
        /// How long an anchor's widget takes to slide between its on-screen
        /// form (sitting on the projected point) and its off-screen form
        /// (clamped to the canvas edge) when the tracked point crosses that
        /// boundary - the one moment where the target position genuinely jumps.
        /// Short on purpose: it should read as the label morphing, not as the
        /// label taking a trip.
        /// </summary>
        private const float TransitionDurationSeconds = 0.18f;

        /// <summary>
        /// Per-anchor on/off-screen transition. Every frame the widget sits on
        /// its exact tracked target - no continuous smoothing, because that's
        /// what made an off-screen label lag behind the edge it's panning along
        /// and made a mid-air jump from one edge to the opposite one (which
        /// happens near the behind-camera transition, while the point stays
        /// off-screen throughout) render as a slow crawl across the screen.
        /// Instead, only a real <see cref="IndicatorState.IsOffScreen"/> flip
        /// starts a transition: the widget's current position is frozen as the
        /// start point and, for <see cref="TransitionDurationSeconds"/>, the
        /// widget eases from it to the <em>live</em> target. Lerping towards the
        /// live target (rather than chasing it at a fixed speed) means the
        /// widget lands exactly on it when the transition ends, with nothing to
        /// overshoot and nothing to counteract afterwards.
        /// </summary>
        private struct TransitionState
        {
            public bool WasOffScreen;
            public Vector2 StartPosition;
            public float Elapsed;

            /// <summary>Last position actually applied to the widget - the start point if a flip happens next frame.</summary>
            public Vector2 CurrentPosition;

            /// <summary>
            /// <see cref="IndicatorAnchor.OffScreenBlend"/> at the moment the
            /// current transition started, and where it currently sits. Tracked
            /// alongside the position for the same reason it is: a flip that
            /// lands mid-transition starts from wherever the blend actually got
            /// to, not from a clean 0/1, so reversing part-way eases back
            /// instead of jumping.
            /// </summary>
            public float StartBlend;
            public float CurrentBlend;
        }

        private readonly Dictionary<IndicatorAnchor, TransitionState> _transitions = new Dictionary<IndicatorAnchor, TransitionState>();

        /// <summary>
        /// Camera yaw/pitch speed, in degrees/second, above which an on/off-
        /// screen flip is considered a rapid snap-pan rather than a deliberate
        /// slow turn - see <see cref="_isFastPan"/>.
        /// </summary>
        private const float FastPanAngularSpeedThresholdDegreesPerSecond = 130f;

        /// <summary>Camera forward direction as of the previous <see cref="LateUpdate"/>, used to derive pan speed. Null on the first frame (or right after the tracked camera changes/disappears) so that frame can't be mistaken for a fast pan.</summary>
        private Vector3? _lastCameraForward;

        /// <summary>
        /// True for the current frame's <see cref="LateUpdate"/> when the
        /// camera turned faster than <see cref="FastPanAngularSpeedThresholdDegreesPerSecond"/>
        /// since the previous frame. A widget whose on/off-screen state flips
        /// on such a frame skips the eased transition entirely and snaps
        /// straight to its target - the ease exists so a label "morphs"
        /// between its on-/off-screen forms during ordinary looking-around,
        /// but on a fast snap-pan the jump itself is already instant from the
        /// player's perspective (the eye can't track a target crossing the
        /// screen that fast), so easing it only adds a visible half-screen
        /// slide with nothing earned in return.
        /// </summary>
        private bool _isFastPan;

        private readonly List<IndicatorAnchor> _overlapCandidates = new List<IndicatorAnchor>();

        /// <summary>The overlap candidates split by how they're anchored this frame - each group spreads along a different axis (see <see cref="ResolveLabelOverlaps"/>).</summary>
        private readonly List<IndicatorAnchor> _groupOnScreen = new List<IndicatorAnchor>();
        private readonly List<IndicatorAnchor> _groupLeftRightEdge = new List<IndicatorAnchor>();
        private readonly List<IndicatorAnchor> _groupTopBottomEdge = new List<IndicatorAnchor>();

        private readonly List<Vector2> _overlapBasePositionsScratch = new List<Vector2>();
        private readonly List<Vector2> _overlapSizesScratch = new List<Vector2>();
        private readonly List<Vector2> _overlapPlacementSizesScratch = new List<Vector2>();
        private readonly List<float> _overlapCapsScratch = new List<float>();
        private readonly Dictionary<IndicatorAnchor, Vector2> _overlapBasePosition = new Dictionary<IndicatorAnchor, Vector2>();

        /// <summary>
        /// Per-anchor 0..1 label compaction (see <see cref="IndicatorAnchor.SetLabelCompaction"/>),
        /// eased towards 1 while an on-screen label is nudged clear of its
        /// crosshair and back to 0 when it settles onto it. Mirrors the compass's
        /// own <c>_markerLabelCompaction</c>.
        /// </summary>
        private readonly Dictionary<IndicatorAnchor, float> _overlapCompaction = new Dictionary<IndicatorAnchor, float>();

        /// <summary>Resolved-offset magnitude past which an on-screen label is considered nudged off its crosshair and starts compacting its name/distance lines together.</summary>
        private const float CompactionMoveThresholdPixels = 8f;

        /// <summary>Per-second rate the label compaction eases at - the whole 0..1 travelled over roughly this many pixels of offset, so the lines close up over the same short slide the label makes rather than snapping.</summary>
        private const float OverlapCompactionSpeedPerSecond = OverlapOffsetSpeedPixelsPerSecond / 30f;

        /// <summary>Each candidate's overlap <em>box</em> centre (tracked position + <see cref="IndicatorAnchor.OverlapCenterOffset"/>) - what the resolver reasons about, as opposed to the widget position it gets applied to.</summary>
        private readonly Dictionary<IndicatorAnchor, Vector2> _overlapBoxPosition = new Dictionary<IndicatorAnchor, Vector2>();
        private readonly Dictionary<IndicatorAnchor, Vector2> _overlapOffset = new Dictionary<IndicatorAnchor, Vector2>();

        /// <summary>Per-anchor delay/speed pacing state for <see cref="ApplyResolvedOffset"/>'s own offset motion - see <see cref="OverlapAnimationPacing"/>.</summary>
        private readonly Dictionary<IndicatorAnchor, OverlapAnimationPacing.State> _overlapPacing = new Dictionary<IndicatorAnchor, OverlapAnimationPacing.State>();

        /// <summary>
        /// The live instance's own full-screen overlay canvas. Called from the
        /// <see cref="Instance"/> getter rather than <c>Awake</c>, so a
        /// <see cref="CreateDetached"/> instance (which renders into a canvas
        /// someone else owns) doesn't build one it would never use.
        /// </summary>
        private void BuildLiveCanvas()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>().enabled = false;

            CanvasTransform = (RectTransform)canvasGo.transform;
            CanvasTransform.anchorMin = new Vector2(0.5f, 0.5f);
            CanvasTransform.anchorMax = new Vector2(0.5f, 0.5f);
            CanvasTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        public IndicatorAnchor RegisterAnchor(IndicatorAnchor anchor)
        {
            _anchors.Add(anchor);
            return anchor;
        }

        public void UnregisterAnchor(IndicatorAnchor anchor)
        {
            _anchors.Remove(anchor);
            _overlapBasePosition.Remove(anchor);
            _overlapBoxPosition.Remove(anchor);
            _overlapOffset.Remove(anchor);
            _overlapCompaction.Remove(anchor);
            _overlapPacing.Remove(anchor);
            _transitions.Remove(anchor);

            if (anchor.ReleaseWidget != null)
            {
                anchor.ReleaseWidget();
            }
            else if (anchor.Widget != null)
            {
                Destroy(anchor.Widget.gameObject);
            }
        }

        private void LateUpdate()
        {
            // Sit just behind the game's own HUD canvas rather than a fixed
            // sky-high sorting order, so this overlay never draws over the
            // vanilla UI (pause menu, inventory bar, etc.) - only under it.
            // A detached instance has no canvas of its own; it draws inside
            // whatever the preview menu already put it in.
            if (!_detached && GUIManager.instance != null && GUIManager.instance.hudCanvas != null)
            {
                _canvas.sortingOrder = GUIManager.instance.hudCanvas.sortingOrder - 1;
            }

            Camera camera = _cameraOverride != null ? _cameraOverride : Camera.main;
            Vector2 canvasSize = CanvasTransform.rect.size;

            UpdatePanSpeed(camera);

            _overlapCandidates.Clear();

            for (int i = _anchors.Count - 1; i >= 0; i--)
            {
                var anchor = _anchors[i];

                // Widget destroyed out from under us (e.g. its owning system
                // tore down the GameObject directly) - drop the anchor.
                if (anchor.Widget == null)
                {
                    _anchors.RemoveAt(i);
                    continue;
                }

                // CompassOnly mode hides this off-screen widget/arrow entirely -
                // the anchor still stays registered (Compass.CompassManager reads
                // the same anchor list for its own top-of-screen marker), it just
                // doesn't get positioned or shown here.
                bool showOffScreenWidget = anchor.GetPlacement() != IndicatorPlacement.CompassOnly;
                bool active = camera != null && anchor.IsActive() && showOffScreenWidget;
                anchor.Widget.gameObject.SetActive(active);
                if (!active)
                {
                    // Dropped so a later reappearance snaps straight to its
                    // fresh target instead of sliding in from a stale
                    // position last seen possibly a while ago.
                    _transitions.Remove(anchor);
                    continue;
                }

                var state = ScreenSpaceTracker.Compute(camera, canvasSize, anchor.GetWorldPosition(), anchor.EdgeMarginPixels);
                Vector2 position = ResolveTransitionedPosition(anchor, state);
                anchor.Widget.anchoredPosition = position;

                if (anchor.OverlapSize.x > 0f && anchor.OverlapSize.y > 0f)
                {
                    _overlapCandidates.Add(anchor);
                    _overlapBasePosition[anchor] = position;
                    _overlapBoxPosition[anchor] = position + anchor.OverlapCenterOffset;
                }

                if (anchor.ArrowWidget != null)
                {
                    anchor.ArrowWidget.gameObject.SetActive(state.IsOffScreen);
                    if (state.IsOffScreen)
                    {
                        // Sprite convention: arrow art points "up" (+Y) at rotation 0.
                        // Confirmed in-game with the actual directional arrow art
                        // (the old placeholder was a symmetric rectangle, so this
                        // couldn't be verified visually until the real sprite
                        // shipped): the +90 offset renders backwards, -90 is
                        // correct - do not "simplify" this back to +90.
                        anchor.ArrowWidget.localEulerAngles = anchor.RotateArrowWidget
                            ? new Vector3(0f, 0f, state.ArrowAngleDegrees - 90f)
                            : Vector3.zero;
                    }
                }

                if (anchor.OnScreenOnlyWidget != null)
                {
                    anchor.OnScreenOnlyWidget.gameObject.SetActive(!state.IsOffScreen);
                }
            }

            ResolveLabelOverlaps(canvasSize);
        }

        /// <summary>
        /// Refreshes <see cref="_isFastPan"/> from how far the camera turned
        /// since the previous frame.
        /// </summary>
        private void UpdatePanSpeed(Camera camera)
        {
            if (camera == null)
            {
                _lastCameraForward = null;
                _isFastPan = false;
                return;
            }

            Vector3 forward = camera.transform.forward;
            if (!_lastCameraForward.HasValue || Time.deltaTime <= 0f)
            {
                _isFastPan = false;
            }
            else
            {
                float angularSpeed = Vector3.Angle(_lastCameraForward.Value, forward) / Time.deltaTime;
                _isFastPan = angularSpeed >= FastPanAngularSpeedThresholdDegreesPerSecond;
            }
            _lastCameraForward = forward;
        }

        /// <summary>
        /// Where this anchor's widget goes this frame: its exact tracked target,
        /// except during the brief ease that an on/off-screen flip kicks off (see
        /// <see cref="TransitionState"/>). A flip mid-transition just re-starts
        /// the ease from wherever the widget currently is, so a player swinging
        /// in and out of view reverses smoothly instead of finishing a trip it
        /// no longer wants to make.
        /// </summary>
        private Vector2 ResolveTransitionedPosition(IndicatorAnchor anchor, IndicatorState state)
        {
            Vector2 target = state.CanvasPosition;
            float targetBlend = state.IsOffScreen ? 1f : 0f;

            if (!_transitions.TryGetValue(anchor, out TransitionState transition))
            {
                // First frame for this anchor: it has no previous position to
                // come from, so it belongs on its target right away.
                _transitions[anchor] = new TransitionState
                {
                    WasOffScreen = state.IsOffScreen,
                    Elapsed = TransitionDurationSeconds,
                    CurrentPosition = target,
                    CurrentBlend = targetBlend,
                };
                anchor.OffScreenBlend = targetBlend;
                return target;
            }

            if (transition.WasOffScreen != state.IsOffScreen)
            {
                transition.WasOffScreen = state.IsOffScreen;
                transition.StartPosition = transition.CurrentPosition;
                transition.StartBlend = transition.CurrentBlend;
                // A flip during a fast snap-pan is marked already-finished
                // rather than started, so it snaps straight to the live
                // target below instead of easing across the screen - see
                // _isFastPan.
                transition.Elapsed = _isFastPan ? TransitionDurationSeconds : 0f;
            }

            Vector2 position = target;
            float blend = targetBlend;
            if (transition.Elapsed < TransitionDurationSeconds)
            {
                transition.Elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(transition.Elapsed / TransitionDurationSeconds));
                position = Vector2.Lerp(transition.StartPosition, target, t);
                blend = Mathf.Lerp(transition.StartBlend, targetBlend, t);
            }

            transition.CurrentPosition = position;
            transition.CurrentBlend = blend;
            _transitions[anchor] = transition;
            anchor.OffScreenBlend = blend;
            return position;
        }

        /// <summary>
        /// Second pass, run after every anchor's own "natural" tracked
        /// position is already set above: nudges apart any labels (opted in
        /// via a nonzero <see cref="IndicatorAnchor.OverlapSize"/>) whose
        /// boxes overlap, per <see cref="LabelOverlapResolver"/> - each cluster
        /// of colliding labels splits apart around its own middle, by the least
        /// total movement that clears it. Purely geometric: it depends on no
        /// list/registration order and on no particular conflicting neighbour,
        /// so it can't misplace itself over time as unrelated anchors elsewhere
        /// come and go. The resulting offset is smoothed towards its target
        /// rather than applied directly, so a label sliding into/out of overlap
        /// doesn't snap.
        /// </summary>
        private void ResolveLabelOverlaps(Vector2 canvasSize)
        {
            if (_overlapCandidates.Count == 0)
            {
                return;
            }

            // Off (Plugin.Instance.Cfg.EnableLabelOverlapAvoidance) means every
            // label just sits at its exact tracked position, same as before
            // this feature existed - target offsets all stay zero (still
            // smoothed towards, so toggling this off mid-overlap eases labels
            // back instead of snapping them).
            if (!Plugin.Instance.Cfg.EnableLabelOverlapAvoidance.Value)
            {
                foreach (IndicatorAnchor anchor in _overlapCandidates)
                {
                    ApplyResolvedOffset(anchor, Vector2.zero, canvasSize);
                }
                return;
            }

            // Which way a label should spread depends on how it's anchored this
            // frame, so the candidates are split and each group resolved on its
            // own axis:
            //  - on-screen (sitting on a visible point, not an edge): spread
            //    vertically, single line - the original behaviour, left untouched.
            //  - clamped to the left/right edge: spread vertically along that edge,
            //    with one overflow column inset toward the centre.
            //  - clamped to the top/bottom edge: spread horizontally along that
            //    edge, with one overflow row inset toward the centre.
            // Spreading a top/bottom stack vertically (as one global pass did) is
            // what made those edges a jittering pile - the labels had nowhere to
            // go along their actual edge. Groups sit on different edges, far apart,
            // so resolving them separately loses no real cross-group collision.
            _groupOnScreen.Clear();
            _groupLeftRightEdge.Clear();
            _groupTopBottomEdge.Clear();
            float halfW = Mathf.Max(1f, canvasSize.x * 0.5f);
            float halfH = Mathf.Max(1f, canvasSize.y * 0.5f);
            foreach (IndicatorAnchor anchor in _overlapCandidates)
            {
                if (anchor.OffScreenBlend < 0.5f)
                {
                    _groupOnScreen.Add(anchor);
                    continue;
                }

                // Which edge it's clamped to: whichever axis its tracked point is
                // pinned closest to the limit on.
                Vector2 p = _overlapBasePosition[anchor];
                bool leftRight = Mathf.Abs(p.x) / halfW >= Mathf.Abs(p.y) / halfH;
                (leftRight ? _groupLeftRightEdge : _groupTopBottomEdge).Add(anchor);
            }

            ResolveOnScreenGroup(_groupOnScreen, canvasSize);
            ResolveGroup(_groupLeftRightEdge, LabelOverlapResolver.Axis.Vertical, EdgeLabelMaxLines, true, canvasSize);
            ResolveGroup(_groupTopBottomEdge, LabelOverlapResolver.Axis.Horizontal, EdgeLabelMaxLines, true, canvasSize);
        }

        /// <summary>
        /// The on-screen group's own resolver: unlike the two edge groups (which
        /// spread along one shared axis), on-screen labels sit on scattered
        /// visible points, so they separate in 2D - splitting apart vertically and
        /// fanning slightly sideways into a diagonal rather than a rigid column
        /// (<see cref="LabelOverlapResolver.ComputeOffsetsOnScreen"/>). Detection
        /// uses each label's full footprint (<see cref="IndicatorAnchor.OverlapSize"/>);
        /// spacing uses its tighter compacted footprint
        /// (<see cref="IndicatorAnchor.OverlapPlacementSize"/>) when it has one, so
        /// stacked labels pack as close as their name/distance lines really need
        /// once compacted.
        /// </summary>
        private void ResolveOnScreenGroup(List<IndicatorAnchor> group, Vector2 canvasSize)
        {
            if (group.Count == 0)
            {
                return;
            }

            _overlapBasePositionsScratch.Clear();
            _overlapSizesScratch.Clear();
            _overlapPlacementSizesScratch.Clear();
            _overlapCapsScratch.Clear();
            foreach (IndicatorAnchor anchor in group)
            {
                _overlapBasePositionsScratch.Add(_overlapBoxPosition[anchor]);
                _overlapSizesScratch.Add(anchor.OverlapSize);
                _overlapPlacementSizesScratch.Add(anchor.OverlapPlacementSize);
                _overlapCapsScratch.Add(anchor.MaxOverlapOffset);
            }

            Vector2[] targetOffsets = LabelOverlapResolver.ComputeOffsetsOnScreen(
                _overlapBasePositionsScratch, _overlapSizesScratch,
                _overlapPlacementSizesScratch, _overlapCapsScratch);

            for (int i = 0; i < group.Count; i++)
            {
                ApplyResolvedOffset(group[i], targetOffsets[i], canvasSize);
            }
        }

        /// <summary>
        /// Resolves one edge/on-screen group's overlaps on the given axis and
        /// applies the result. The resolver hands back a shared buffer valid only
        /// until its next call, so each group is fully consumed here before the
        /// next <see cref="ResolveGroup"/> runs.
        /// </summary>
        private void ResolveGroup(List<IndicatorAnchor> group, LabelOverlapResolver.Axis axis, int maxLines, bool densePack, Vector2 canvasSize)
        {
            if (group.Count == 0)
            {
                return;
            }

            _overlapBasePositionsScratch.Clear();
            _overlapSizesScratch.Clear();
            _overlapCapsScratch.Clear();
            foreach (IndicatorAnchor anchor in group)
            {
                _overlapBasePositionsScratch.Add(_overlapBoxPosition[anchor]);
                _overlapSizesScratch.Add(anchor.OverlapSize);
                _overlapCapsScratch.Add(anchor.MaxOverlapOffset);
            }

            Vector2[] targetOffsets = LabelOverlapResolver.ComputeOffsets(
                _overlapBasePositionsScratch, _overlapSizesScratch, axis, _overlapCapsScratch,
                maxRows: maxLines, densePack: densePack);

            for (int i = 0; i < group.Count; i++)
            {
                ApplyResolvedOffset(group[i], targetOffsets[i], canvasSize);
            }
        }

        /// <summary>
        /// Clamps, smooths and applies one anchor's resolved overlap offset to its
        /// label (or whole widget). Shared by every resolution group and by the
        /// "avoidance off" path (which passes a zero target so labels ease back).
        /// </summary>
        private void ApplyResolvedOffset(IndicatorAnchor anchor, Vector2 target, Vector2 canvasSize)
        {
            if (anchor.OverlapOffsetDownwardOnly && target.y > 0f)
            {
                target.y = 0f;
            }

            // Compaction leads off the resolver's raw target (before the screen-
            // edge clamp below), so the name/distance lines close up as the label
            // travels rather than chasing it: a label pushed clear of its own
            // crosshair no longer wants the empty gap that crosshair sat in. Only
            // on-screen labels compact - off-screen the icon rides with the label,
            // so the gap is still real - and only widgets that offer the hook.
            if (anchor.SetLabelCompaction != null)
            {
                float targetCompaction = anchor.OffScreenBlend < 0.5f && target.magnitude > CompactionMoveThresholdPixels
                    ? 1f
                    : 0f;
                float currentCompaction = _overlapCompaction.TryGetValue(anchor, out float existingCompaction) ? existingCompaction : 0f;
                float smoothedCompaction = Mathf.MoveTowards(currentCompaction, targetCompaction, Time.deltaTime * OverlapCompactionSpeedPerSecond * OverlapAnimationPacing.Multiplier);
                _overlapCompaction[anchor] = smoothedCompaction;
                anchor.SetLabelCompaction(smoothedCompaction);
            }

            // Keep the resolved box fully on-screen: a stack clamped to an edge
            // could otherwise be spread (or an overflow line stepped) right off it,
            // hiding a label or part of an icon entirely. Clamp the box centre so
            // its whole footprint stays inside the canvas - better a residual
            // overlap at the very edge than an invisible entry. The box is the
            // label's footprint (OverlapCenterOffset + OverlapSize) around the
            // tracked point.
            Vector2 boxBase = _overlapBoxPosition[anchor];
            Vector2 half = anchor.OverlapSize * 0.5f;
            float limitX = Mathf.Max(0f, canvasSize.x * 0.5f - half.x);
            float limitY = Mathf.Max(0f, canvasSize.y * 0.5f - half.y);
            target.x = Mathf.Clamp(boxBase.x + target.x, -limitX, limitX) - boxBase.x;
            target.y = Mathf.Clamp(boxBase.y + target.y, -limitY, limitY) - boxBase.y;

            Vector2 currentOffset = _overlapOffset.TryGetValue(anchor, out Vector2 existing) ? existing : Vector2.zero;
            if (!_overlapPacing.TryGetValue(anchor, out OverlapAnimationPacing.State pacing))
            {
                pacing = new OverlapAnimationPacing.State();
                _overlapPacing[anchor] = pacing;
            }
            Vector2 smoothedOffset = OverlapAnimationPacing.Advance(currentOffset, target, OverlapOffsetSpeedPixelsPerSecond, pacing);
            _overlapOffset[anchor] = smoothedOffset;

            // LabelWidget (when the anchor has one) is a local (0,0)-homed child of
            // Widget holding just the text - nudge that instead of Widget itself,
            // so an arrow/crosshair that needs to stay exactly on the tracked
            // position never moves.
            if (anchor.LabelWidget != null)
            {
                anchor.LabelWidget.anchoredPosition = smoothedOffset;
            }
            else
            {
                anchor.Widget.anchoredPosition = _overlapBasePosition[anchor] + smoothedOffset;
            }
        }
    }
}
