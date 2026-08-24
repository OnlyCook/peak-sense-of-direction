using System.Collections.Generic;
using UnityEngine;

namespace SenseOfDirection.Indicators
{
    // Owns the single full-screen overlay canvas used for every edge-of-screen
    // indicator (player labels, pings, campfire), and drives each registered
    // IndicatorAnchor's widget to the right on-screen or clamped-edge
    // position every frame via ScreenSpaceTracker.
    //
    // Lazily created on first use and kept alive for the process lifetime
    // (DontDestroyOnLoad) - mechanics register/unregister anchors as their
    // own tracked objects (players, pings, ...) come and go.
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

        // A second, non-singleton manager driving the exact same anchor/widget
        // machinery into somewhere other than the real screen - used by the
        // config preview menu, which renders the mod's real widgets against a
        // fake camera inside a panel. Everything below (edge clamping,
        // on/off-screen transition, overlap resolution) is already
        // resolution-independent, so the preview gets the real behaviour
        // instead of a lookalike that could drift out of sync.
        // surface: widgets are parented here, and its rect size stands in for the screen.
        // camera: projected against instead of Camera.main.
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

        // Parent every registered anchor's widget under this.
        public RectTransform CanvasTransform { get; private set; }

        // Set on a CreateDetached instance: owns no canvas of its own and never touches the game's HUD sorting order.
        private bool _detached;

        // Null on the live instance, which tracks Camera.main.
        private Camera _cameraOverride;

        // Read-only view for CompassManager, which drives its own top-of-screen markers off these same anchors.
        public IReadOnlyList<IndicatorAnchor> Anchors => _anchors;

        private readonly List<IndicatorAnchor> _anchors = new List<IndicatorAnchor>();
        private Canvas _canvas;

        // Pixels/second the resolved overlap offset is smoothed towards its target at.
        private const float OverlapOffsetSpeedPixelsPerSecond = 240f;

        // A crowded edge stack fans out into at most this many lines - the
        // primary line along the edge plus one overflow line inset toward
        // the centre, so it never reaches far enough in to touch the crosshair.
        private const int EdgeLabelMaxLines = 2;

        // How long an anchor's widget takes to slide between its on-screen
        // and off-screen forms when the tracked point crosses that boundary.
        // Short on purpose - it should read as morphing, not as a trip.
        private const float TransitionDurationSeconds = 0.18f;

        // Per-anchor on/off-screen transition. The widget normally sits on
        // its exact tracked target every frame; only a real IsOffScreen flip
        // starts a transition, easing from the frozen position at flip-time
        // to the live target over TransitionDurationSeconds. Lerping to the
        // live target (rather than chasing at a fixed speed) means it lands
        // exactly on target with nothing to overshoot or correct afterwards.
        private struct TransitionState
        {
            public bool WasOffScreen;
            public Vector2 StartPosition;
            public float Elapsed;

            // Last position actually applied to the widget.
            public Vector2 CurrentPosition;

            // OffScreenBlend at the moment the current transition started, and where it currently sits.
            public float StartBlend;
            public float CurrentBlend;
        }

        private readonly Dictionary<IndicatorAnchor, TransitionState> _transitions = new Dictionary<IndicatorAnchor, TransitionState>();

        // Camera yaw/pitch speed (deg/s) above which an on/off-screen flip counts as a fast snap-pan rather than a deliberate turn.
        private const float FastPanAngularSpeedThresholdDegreesPerSecond = 130f;

        // Camera forward as of the previous LateUpdate. Null on the first frame (or after the camera changes) so that frame isn't mistaken for a fast pan.
        private Vector3? _lastCameraForward;

        // True for the current frame when the camera turned faster than the
        // threshold above. A widget whose on/off-screen state flips that frame
        // skips the eased transition and snaps straight to target - the ease
        // is for a label "morphing" during ordinary looking-around; on a fast
        // snap-pan the jump already reads as instant, so easing it would only
        // add a visible slide with nothing earned.
        private bool _isFastPan;

        private readonly List<IndicatorAnchor> _overlapCandidates = new List<IndicatorAnchor>();

        // Overlap candidates split by how they're anchored this frame - each group spreads along a different axis (see ResolveLabelOverlaps).
        private readonly List<IndicatorAnchor> _groupOnScreen = new List<IndicatorAnchor>();
        private readonly List<IndicatorAnchor> _groupLeftRightEdge = new List<IndicatorAnchor>();
        private readonly List<IndicatorAnchor> _groupTopBottomEdge = new List<IndicatorAnchor>();

        private readonly List<Vector2> _overlapBasePositionsScratch = new List<Vector2>();
        private readonly List<Vector2> _overlapSizesScratch = new List<Vector2>();
        private readonly List<Vector2> _overlapPlacementSizesScratch = new List<Vector2>();
        private readonly List<float> _overlapCapsScratch = new List<float>();
        private readonly Dictionary<IndicatorAnchor, Vector2> _overlapBasePosition = new Dictionary<IndicatorAnchor, Vector2>();

        // Per-anchor 0..1 label compaction, eased towards 1 while an on-screen
        // label is nudged clear of its crosshair and back to 0 once it settles.
        private readonly Dictionary<IndicatorAnchor, float> _overlapCompaction = new Dictionary<IndicatorAnchor, float>();

        // Resolved-offset magnitude past which an on-screen label is considered nudged off its crosshair and starts compacting.
        private const float CompactionMoveThresholdPixels = 8f;

        // Per-second rate label compaction eases at.
        private const float OverlapCompactionSpeedPerSecond = OverlapOffsetSpeedPixelsPerSecond / 30f;

        // Each candidate's overlap box centre (tracked position + OverlapCenterOffset) - what the resolver reasons about.
        private readonly Dictionary<IndicatorAnchor, Vector2> _overlapBoxPosition = new Dictionary<IndicatorAnchor, Vector2>();
        private readonly Dictionary<IndicatorAnchor, Vector2> _overlapOffset = new Dictionary<IndicatorAnchor, Vector2>();

        // Per-anchor pacing state for ApplyResolvedOffset's own offset motion.
        private readonly Dictionary<IndicatorAnchor, OverlapAnimationPacing.State> _overlapPacing = new Dictionary<IndicatorAnchor, OverlapAnimationPacing.State>();

        // Built from the Instance getter rather than Awake, so a detached instance never builds a canvas it wouldn't use.
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
            _anchorFailures.Remove(anchor);

            // Caller-supplied teardown - guarded so a throw here can't strand
            // the rest of an unregister loop (e.g. LateUpdateImpl's retire path).
            Common.Safe.Run("IndicatorManager.UnregisterAnchor (widget teardown)", () =>
            {
                if (anchor.ReleaseWidget != null)
                {
                    anchor.ReleaseWidget();
                }
                else if (anchor.Widget != null)
                {
                    Destroy(anchor.Widget.gameObject);
                }
            });
        }

        private static readonly Common.Safe.Context _ctxLateUpdateImpl =
            new Common.Safe.Context("IndicatorManager.LateUpdate", failureLimit: 300);

        private void LateUpdate()
        {
            if (_ctxLateUpdateImpl.Disabled) return;
            try { LateUpdateImpl(); _ctxLateUpdateImpl.Succeeded(); }
            catch (System.Exception e) { _ctxLateUpdateImpl.Failed(e); }
        }

        private void LateUpdateImpl()
        {
            // Sit just behind the game's own HUD canvas so this overlay never
            // draws over the vanilla UI. A detached instance draws inside
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

                // Widget destroyed out from under us - drop the anchor.
                if (anchor.Widget == null)
                {
                    _anchors.RemoveAt(i);
                    continue;
                }

                // Every anchor is driven behind its own guard: its getters
                // close over live game objects that can be destroyed or
                // half-torn-down at an awkward moment, and one throwing must
                // not abort the whole loop and freeze every indicator on screen.
                if (!TryUpdateAnchor(anchor, camera, canvasSize))
                {
                    // Persistently broken: retire it rather than throwing every frame forever.
                    if (BumpAnchorFailure(anchor) >= AnchorFailuresToRetire)
                    {
                        Plugin.Instance?.Log?.LogWarning(
                            $"IndicatorManager: retiring an anchor that failed {AnchorFailuresToRetire}x in a row.");
                        UnregisterAnchor(anchor);
                    }
                    continue;
                }
                if (_anchorFailures.Count > 0)
                {
                    _anchorFailures.Remove(anchor);
                }
            }

            ResolveLabelOverlaps(canvasSize);
        }

        // Consecutive per-anchor failures before LateUpdateImpl gives up on one.
        private const int AnchorFailuresToRetire = 60;

        private readonly Dictionary<IndicatorAnchor, int> _anchorFailures = new Dictionary<IndicatorAnchor, int>();

        private int BumpAnchorFailure(IndicatorAnchor anchor)
        {
            _anchorFailures.TryGetValue(anchor, out int count);
            count++;
            _anchorFailures[anchor] = count;
            return count;
        }

        private static readonly Common.Safe.Context _ctxAnchor =
            new Common.Safe.Context("IndicatorManager anchor update");

        // Hand-rolled try/catch rather than a Safe.Run lambda: this runs once
        // per anchor per frame, and a capturing lambda would allocate a closure every call.
        private bool TryUpdateAnchor(IndicatorAnchor anchor, Camera camera, Vector2 canvasSize)
        {
            try
            {
                UpdateAnchor(anchor, camera, canvasSize);
                _ctxAnchor.Succeeded();
                return true;
            }
            catch (System.Exception e)
            {
                _ctxAnchor.Failed(e);
                return false;
            }
        }

        private void UpdateAnchor(IndicatorAnchor anchor, Camera camera, Vector2 canvasSize)
        {
            // CompassOnly mode hides this widget/arrow entirely - the anchor
            // stays registered since CompassManager reads the same list.
            bool showOffScreenWidget = anchor.GetPlacement() != IndicatorPlacement.CompassOnly;
            bool active = camera != null && anchor.IsActive() && showOffScreenWidget;
            anchor.Widget.gameObject.SetActive(active);
            if (!active)
            {
                // Dropped so a later reappearance snaps to its fresh target instead of sliding in from a stale one.
                _transitions.Remove(anchor);
                return;
            }

            bool allowOffScreen = anchor.AllowOffScreen();

            // Off-screen indicator disabled (player labels' own
            // enable-offscreen-indicator): track the label's true, unclamped
            // screen position instead of snapping it to the canvas edge, and
            // only hide it once its whole footprint - not just its tracked
            // point - has left the canvas, so it keeps showing whatever part
            // (e.g. its name) is still in-bounds while sliding past the edge.
            if (!allowOffScreen)
            {
                var rawState = ScreenSpaceTracker.Compute(
                    camera, canvasSize, anchor.GetWorldPosition(), anchor.EdgeMarginPixels, clampToEdge: false);

                if (IsEntirelyOffCanvas(rawState.CanvasPosition, canvasSize, anchor.OverlapSize, anchor.OverlapCenterOffset))
                {
                    anchor.Widget.gameObject.SetActive(false);
                    _transitions.Remove(anchor);
                    return;
                }

                anchor.Widget.anchoredPosition = rawState.CanvasPosition;
                anchor.OffScreenBlend = 0f;
                _transitions.Remove(anchor);

                if (anchor.OverlapSize.x > 0f && anchor.OverlapSize.y > 0f)
                {
                    _overlapCandidates.Add(anchor);
                    _overlapBasePosition[anchor] = rawState.CanvasPosition;
                    _overlapBoxPosition[anchor] = rawState.CanvasPosition + anchor.OverlapCenterOffset;
                }

                if (anchor.ArrowWidget != null)
                {
                    anchor.ArrowWidget.gameObject.SetActive(false);
                }
                if (anchor.OnScreenOnlyWidget != null)
                {
                    anchor.OnScreenOnlyWidget.gameObject.SetActive(true);
                }
                return;
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
                    // Arrow art points "up" (+Y) at rotation 0; confirmed in-game that -90 is
                    // correct here, not +90 - don't "simplify" this back.
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

        // True once an anchor's footprint (OverlapSize box, offset by
        // OverlapCenterOffset) has entirely left the canvas, rather than just
        // its tracked point - used by the off-screen-indicator-disabled path
        // so a label keeps showing whatever part is still in view. A zero
        // footprint degrades to treating the tracked point as the whole box.
        private static bool IsEntirelyOffCanvas(Vector2 position, Vector2 canvasSize, Vector2 footprint, Vector2 centerOffset)
        {
            Vector2 half = footprint * 0.5f;
            Vector2 boxCenter = position + centerOffset;
            float halfCanvasX = canvasSize.x * 0.5f;
            float halfCanvasY = canvasSize.y * 0.5f;

            bool outsideX = boxCenter.x + half.x < -halfCanvasX || boxCenter.x - half.x > halfCanvasX;
            bool outsideY = boxCenter.y + half.y < -halfCanvasY || boxCenter.y - half.y > halfCanvasY;
            return outsideX || outsideY;
        }

        // Refreshes _isFastPan from how far the camera turned since the previous frame.
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

        // Where this anchor's widget goes this frame: its exact tracked
        // target, except during the brief ease an on/off-screen flip kicks
        // off. A flip mid-transition restarts the ease from the widget's
        // current position, so swinging in and out of view reverses smoothly.
        private Vector2 ResolveTransitionedPosition(IndicatorAnchor anchor, IndicatorState state)
        {
            Vector2 target = state.CanvasPosition;
            float targetBlend = state.IsOffScreen ? 1f : 0f;

            if (!_transitions.TryGetValue(anchor, out TransitionState transition))
            {
                // First frame for this anchor - no previous position to come from.
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
                // A flip during a fast snap-pan is marked already-finished so it snaps to target instead of easing.
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

        // Second pass, run after every anchor's own tracked position is
        // already set: nudges apart any labels (opted in via a nonzero
        // OverlapSize) whose boxes overlap. Each cluster splits apart around
        // its own middle by the least total movement that clears it - purely
        // geometric, so it can't misplace itself as unrelated anchors come
        // and go. Offsets are smoothed towards target rather than applied
        // directly, so labels sliding into/out of overlap don't snap.
        private void ResolveLabelOverlaps(Vector2 canvasSize)
        {
            if (_overlapCandidates.Count == 0)
            {
                return;
            }

            // Avoidance off: every label sits at its exact tracked position;
            // offsets still ease to zero rather than snapping.
            if (!Plugin.Instance.Cfg.EnableLabelOverlapAvoidance.Value)
            {
                foreach (IndicatorAnchor anchor in _overlapCandidates)
                {
                    ApplyResolvedOffset(anchor, Vector2.zero, canvasSize);
                }
                return;
            }

            // Which way a label spreads depends on how it's anchored this
            // frame: on-screen spreads vertically (single line); clamped to
            // left/right spreads vertically along that edge; clamped to
            // top/bottom spreads horizontally along that edge - each with one
            // overflow line inset toward centre. Resolving edges separately
            // (rather than one global vertical pass) keeps top/bottom stacks
            // from jittering with nowhere to go along their own edge.
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

                // Which edge it's clamped to: whichever axis is closer to its limit.
                Vector2 p = _overlapBasePosition[anchor];
                bool leftRight = Mathf.Abs(p.x) / halfW >= Mathf.Abs(p.y) / halfH;
                (leftRight ? _groupLeftRightEdge : _groupTopBottomEdge).Add(anchor);
            }

            ResolveOnScreenGroup(_groupOnScreen, canvasSize);
            ResolveGroup(_groupLeftRightEdge, LabelOverlapResolver.Axis.Vertical, EdgeLabelMaxLines, true, canvasSize);
            ResolveGroup(_groupTopBottomEdge, LabelOverlapResolver.Axis.Horizontal, EdgeLabelMaxLines, true, canvasSize);
        }

        // On-screen labels sit on scattered visible points rather than one
        // shared edge, so they separate in 2D - splitting vertically and
        // fanning slightly sideways rather than a rigid column. Detection
        // uses each label's full OverlapSize; spacing uses the tighter
        // OverlapPlacementSize when set, so compacted stacks pack closer.
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

        // Resolves one edge/on-screen group's overlaps on the given axis and
        // applies the result. The resolver's buffer is only valid until its
        // next call, so each group is fully consumed before the next runs.
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

        // Clamps, smooths and applies one anchor's resolved overlap offset to
        // its label (or whole widget). Shared by every resolution group and
        // by the "avoidance off" path (zero target so labels ease back).
        private void ApplyResolvedOffset(IndicatorAnchor anchor, Vector2 target, Vector2 canvasSize)
        {
            if (anchor.OverlapOffsetDownwardOnly && target.y > 0f)
            {
                target.y = 0f;
            }

            // Compaction leads off the resolver's raw target (before the
            // edge clamp below), so name/distance lines close up as the label
            // travels rather than chasing it. Only on-screen labels compact -
            // off-screen the icon rides with the label, so the gap is real.
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

            // Keep the resolved box fully on-screen so a spread stack can't
            // push a label (or part of an icon) right off the edge - a
            // residual overlap at the very edge beats an invisible entry.
            // Skipped when AllowOffScreen is false: that anchor deliberately
            // tracks its raw, unclamped position so it can slide past the
            // canvas edge (see UpdateAnchor) - clamping here would undo that.
            if (anchor.AllowOffScreen())
            {
                Vector2 boxBase = _overlapBoxPosition[anchor];
                Vector2 half = anchor.OverlapSize * 0.5f;
                float limitX = Mathf.Max(0f, canvasSize.x * 0.5f - half.x);
                float limitY = Mathf.Max(0f, canvasSize.y * 0.5f - half.y);
                target.x = Mathf.Clamp(boxBase.x + target.x, -limitX, limitX) - boxBase.x;
                target.y = Mathf.Clamp(boxBase.y + target.y, -limitY, limitY) - boxBase.y;
            }

            Vector2 currentOffset = _overlapOffset.TryGetValue(anchor, out Vector2 existing) ? existing : Vector2.zero;
            if (!_overlapPacing.TryGetValue(anchor, out OverlapAnimationPacing.State pacing))
            {
                pacing = new OverlapAnimationPacing.State();
                _overlapPacing[anchor] = pacing;
            }
            Vector2 smoothedOffset = OverlapAnimationPacing.Advance(currentOffset, target, OverlapOffsetSpeedPixelsPerSecond, pacing);
            _overlapOffset[anchor] = smoothedOffset;

            // LabelWidget (when set) holds just the anchor's text, nudged
            // instead of Widget itself so an arrow/crosshair that must stay
            // exactly on the tracked position never moves.
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
