using System;
using System.Collections.Generic;
using SenseOfDirection.Common;
using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Compass
{
    /// <summary>
    /// Phase 7: a top-of-screen compass tape, native-HUD-styled, showing
    /// heading ticks (N/E/S/W or degree numbers) plus a marker for every
    /// registered <see cref="IndicatorAnchor"/> that opted in via its
    /// <c>CompassKind</c> - reads the same anchor list
    /// <see cref="IndicatorManager"/> already drives (see that class's
    /// <c>Anchors</c> property) rather than requiring a second registration
    /// call per mechanic, so player labels/campfire/pings/item-pings only
    /// ever have one anchor each.
    ///
    /// Bearing math is plain yaw-angle subtraction (<see cref="Mathf.DeltaAngle"/>)
    /// mapped linearly onto the tape width - deliberately not the reference
    /// Coomzy-Compass_UI's own acos/dot-product curve (kept markers evenly
    /// spaced and makes the optional degree-number ticks trivially line up
    /// with them). Always instantiated from <see cref="Plugin.Awake"/>;
    /// internally no-ops (whole UI hidden) when <c>enable-compass</c> is off,
    /// same pattern as every other always-on controller in this mod.
    ///
    /// Layout note: every positioned element (ticks, markers) is parented to
    /// its own zero-sized "anchor" RectTransform (anchorMin == anchorMax ==
    /// pivot, sizeDelta left at (0,0)) whose own <c>anchoredPosition</c> is
    /// set directly in pixels from <see cref="_root"/>'s origin - same
    /// pattern <c>Indicators.IndicatorManager.CanvasTransform</c> already
    /// uses for every other widget in this mod. An earlier version instead
    /// gave the tick/marker row containers a real, band-sized rect (a
    /// fraction of the total compass height) and relied on Unity's normal
    /// anchor-fraction math to place children within that band -
    /// functionally equivalent on paper, but it visually misplaced ticks/
    /// markers in practice, so it's been replaced with this simpler,
    /// already-proven-elsewhere pattern instead of chasing the discrepancy.
    ///
    /// Visual style: deliberately minimal, matching Coomzy-Compass_UI's own
    /// look (read as reference only, no code copied - see CompassIcons' doc
    /// comment) rather than an earlier, heavier bordered-panel version of
    /// this tape - no background box and no "current heading" pointer (your
    /// facing direction is always the tape's own center by construction, so
    /// a separate indicator for it is redundant); just ticks and markers
    /// floating over the world, resting on one continuous baseline line.
    /// </summary>
    public class CompassManager : MonoBehaviour
    {
        private static CompassManager _instance;

        public static CompassManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.CompassManager");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CompassManager>();
                    _instance.Initialize();
                }
                return _instance;
            }
        }

        /// <summary>
        /// A second, non-singleton tape rendered inside someone else's canvas
        /// against a camera and anchor list of their choosing - the config
        /// preview menu (<c>Ui.PreviewScene</c>). Same reasoning as
        /// <see cref="Indicators.IndicatorManager.CreateDetached"/>: the preview
        /// shows the real compass, driven by the real bearing/fade/overlap code,
        /// rather than a mock-up that could drift away from it.
        /// </summary>
        internal static CompassManager CreateDetached(RectTransform surface, Camera camera, Func<IReadOnlyList<IndicatorAnchor>> anchorSource)
        {
            var go = new GameObject("SenseOfDirection.CompassManager.Detached");
            go.transform.SetParent(surface, false);

            var manager = go.AddComponent<CompassManager>();
            manager._detachedSurface = surface;
            manager._cameraOverride = camera;
            manager._anchorSource = anchorSource;
            manager.Initialize();
            return manager;
        }

        /// <summary>Non-null on a <see cref="CreateDetached"/> instance: the rect its tape lives in, instead of a canvas of its own.</summary>
        private RectTransform _detachedSurface;

        /// <summary>Null on the live instance, which tracks <see cref="Camera.main"/>.</summary>
        private Camera _cameraOverride;

        /// <summary>Null on the live instance, which reads <see cref="IndicatorManager.Instance"/>'s anchors.</summary>
        private Func<IReadOnlyList<IndicatorAnchor>> _anchorSource;

        private bool IsDetached => _detachedSurface != null;

        private const int TickCount = 24; // every 15 degrees

        /// <summary>Marker fade-in/out rate, matched to <c>PlayerLabel</c>'s own vanilla-style crossfade (<c>UIPlayerNames.UpdateName</c>'s <c>Time.deltaTime * 5f</c>) so markers appearing/disappearing (new anchor, out of range, anchor removed entirely) don't pop instantly like the raw edge-of-FOV fade already applies to in-view markers.</summary>
        private const float MarkerFadeSpeedPerSecond = 5f;

        /// <summary>Dimmed alpha a marker settles at once clamped to the tape's edge (<c>compass-clamp-icons-to-edge</c>) rather than the full <see cref="ComputeEdgeFade"/> curve, which would fade it to fully invisible past the FOV cutoff it's deliberately no longer following.</summary>
        private const float ClampedEdgeAlpha = 0.35f;

        private RectTransform _root;
        private RectTransform _baseline;
        private Image _baselineImage;

        /// <summary>Pixels/second the resolved horizontal overlap offset is smoothed towards its target at - same reasoning as <see cref="Indicators.IndicatorManager"/>'s own edge-label version, keeps markers sliding apart/back together instead of snapping.</summary>
        private const float OverlapOffsetSpeedPixelsPerSecond = 240f;

        private readonly List<CompassTick> _ticks = new List<CompassTick>();
        private readonly Dictionary<IndicatorAnchor, CompassMarkerWidget> _markers = new Dictionary<IndicatorAnchor, CompassMarkerWidget>();

        /// <summary>Parent every compass marker hangs off - exposed so <see cref="Common.PingPrewarm"/> can build the pooled ping/item-ping markers ahead of the first ping that needs one.</summary>
        internal RectTransform MarkerRoot => _root;

        private readonly HashSet<IndicatorAnchor> _seenScratch = new HashSet<IndicatorAnchor>();
        private readonly List<IndicatorAnchor> _staleScratch = new List<IndicatorAnchor>();

        private readonly List<IndicatorAnchor> _overlapCandidates = new List<IndicatorAnchor>();
        private readonly List<Vector2> _overlapBasePositionsScratch = new List<Vector2>();
        private readonly List<Vector2> _overlapSizesScratch = new List<Vector2>();
        private readonly Dictionary<IndicatorAnchor, float> _markerBaseX = new Dictionary<IndicatorAnchor, float>();
        private readonly Dictionary<IndicatorAnchor, Vector2> _markerSize = new Dictionary<IndicatorAnchor, Vector2>();
        private readonly Dictionary<IndicatorAnchor, Vector2> _markerOverlapOffset = new Dictionary<IndicatorAnchor, Vector2>();

        /// <summary>Per-marker 0..1 label compaction (see <see cref="CompassMarkerWidget.SetLabelCompaction"/>), smoothed towards 1 while the marker is staggered onto a row below the tape and back to 0 when it returns to it.</summary>
        private readonly Dictionary<IndicatorAnchor, float> _markerLabelCompaction = new Dictionary<IndicatorAnchor, float>();

        /// <summary>
        /// Seconds elapsed since a freshly-created marker with <see cref="IndicatorAnchor.CompassSpawnPop"/>
        /// set started its spawn pop (see <see cref="ApplyMarkerPop"/>) - entries
        /// are removed once the pop finishes, so this only ever holds markers
        /// currently mid-animation.
        /// </summary>
        private readonly Dictionary<IndicatorAnchor, float> _markerPopElapsed = new Dictionary<IndicatorAnchor, float>();

        /// <summary>How long the spawn pop (Luggage-Ping's own extra flourish on top of the ordinary alpha fade-in) takes to settle.</summary>
        private const float MarkerPopDurationSeconds = 0.28f;

        /// <summary>Per-second rate the label compaction eases at - matched to how long the row shift itself takes (a full stagger at <see cref="OverlapOffsetSpeedPixelsPerSecond"/>), so the lines close up as the label travels down rather than snapping shut on arrival.</summary>
        private const float LabelCompactionSpeedPerSecond = OverlapOffsetSpeedPixelsPerSecond / MarkerRowStaggerPixels;

        /// <summary>Approximate vertical span of a marker's name-above/distance-below text, for overlap detection - see the horizontal-only note on <see cref="ResolveMarkerOverlaps"/>'s own use of it.</summary>
        private const float MarkerLabelHeight = 70f;

        /// <summary>How far a marker's label may slide along the tape from its own icon. Wider than the shared 56px default: named markers are 160px boxes, so clearing even one neighbour needs more than that.</summary>
        private const float MarkerMaxOverlapOffset = 90f;

        /// <summary>
        /// A crowded tape cannot declutter sideways at any cap - four named
        /// markers need ~650px and the default tape is 640px wide - so a cluster
        /// that doesn't fit drops alternate members onto a row below (and then a
        /// third).
        ///
        /// Sized against what a staggered row actually renders, not against
        /// <see cref="MarkerLabelHeight"/>: a label down here is compacted (see
        /// <see cref="CompassMarkerWidget.SetLabelCompaction"/>), so it spans
        /// only ~44px rather than the ~70px an on-tape label straddling its icon
        /// does. 60 clears the row above with a comfortable margin while keeping
        /// the stack from eating a third of the screen. Rows are placed
        /// independently of each other (see <see cref="Indicators.LabelOverlapResolver"/>,
        /// which spreads each row along the tape on its own), so this value only
        /// decides how far a staggered label travels - it can't reintroduce a
        /// real overlap within a row.
        /// </summary>
        private const float MarkerRowStaggerPixels = 60f;

        private const int MarkerMaxRows = 3;

        /// <summary>
        /// Called explicitly by the <see cref="Instance"/> getter /
        /// <see cref="CreateDetached"/> rather than from <c>Awake</c>, since
        /// which surface the tape is built into is decided by the caller and
        /// <c>Awake</c> would run before that could be set.
        /// </summary>
        private void Initialize()
        {
            BuildUi();
            BuildTicks();
        }

        private void BuildUi()
        {
            // A detached tape hangs directly off the surface it was given (the
            // preview stage, which already lives in the menu's own canvas); only
            // the live one owns a full-screen canvas of its own.
            Transform parent;
            if (IsDetached)
            {
                parent = _detachedSurface;
            }
            else
            {
                var canvasGo = new GameObject("Canvas");
                canvasGo.transform.SetParent(transform, false);

                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>().enabled = false;
                parent = canvasGo.transform;
            }

            var rootGo = new GameObject("Root", typeof(RectTransform));
            _root = (RectTransform)rootGo.transform;
            _root.SetParent(parent, false);
            _root.anchorMin = new Vector2(0.5f, 1f);
            _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            // A fresh RectTransform's sizeDelta is NOT (0,0) by default once
            // anchorMin/anchorMax stop being (0,0)-(1,1) full-stretch - it's
            // whatever Unity's own default happens to be, which turned out to
            // matter a lot here: every child below anchors itself as a
            // fraction of THIS rect, and a center-anchored child (e.g. a
            // marker) vs. a top-anchored one (e.g. the baseline) resolve to
            // different points unless this is forced to true zero. This one
            // missing line is what caused the whole "line and markers don't
            // line up" bug from the last two passes at this file.
            _root.sizeDelta = Vector2.zero;

            // Deliberately no background panel - Coomzy-Compass_UI's own
            // minimal look (read as reference only, see CompassIcons' doc
            // comment) was preferred over an earlier, heavier bordered-panel
            // version of this tape: markers/ticks float directly over the
            // world, with one continuous baseline line as the only backdrop.
            var baselineGo = new GameObject("Baseline", typeof(RectTransform), typeof(Image));
            _baseline = (RectTransform)baselineGo.transform;
            _baseline.SetParent(_root, false);
            _baseline.anchorMin = new Vector2(0.5f, 1f);
            _baseline.anchorMax = new Vector2(0.5f, 1f);
            // Center-pivoted (not top-pivoted) - anchoredPosition.y is thus
            // the stripe's own visual vertical center, matching exactly where
            // every tick's line crosses it (see CompassTick's own "cross
            // point" doc comment). A top pivot used to put the crossing point
            // 1px above the stripe's actual painted center (the stripe hangs
            // straight down from a top pivot) - ticks compensated for that
            // with an ad hoc, inconsistently-applied nudge instead of fixing
            // the actual mismatch, which is what made the baseline look
            // increasingly off-center as tick lines grew taller.
            _baseline.pivot = new Vector2(0.5f, 0.5f);
            _baselineImage = baselineGo.GetComponent<Image>();
            _baselineImage.sprite = CompassIcons.HorizontalFadeLine;
            _baselineImage.type = Image.Type.Simple;
            _baselineImage.color = new Color(1f, 1f, 1f, 0.55f);
        }

        private void BuildTicks()
        {
            for (int i = 0; i < TickCount; i++)
            {
                float degrees = i * (360f / TickCount);
                // Parented directly to _root (not a separate row container) -
                // both X (bearing) and Y (fixed row offset) are set together,
                // every frame, straight from _root's own origin. An earlier
                // version routed ticks/markers through an intermediate
                // "row anchor" RectTransform whose own Y offset was supposed
                // to match the baseline's - it should have lined up on paper,
                // but didn't in practice, so this removes that whole
                // indirection layer instead of chasing the discrepancy further.
                _ticks.Add(CompassTick.Create(_root, degrees));
            }
        }

        private const float BaselineThicknessPixels = 2f;

        /// <summary>
        /// Base Y offset (from _root's own origin) of the baseline at the
        /// default <c>compass-height-pixels</c> (40) - ticks straddle this
        /// same Y (so their vertical line crosses the baseline, forming a
        /// "+") and marker icons rest centered on it.
        /// </summary>
        private const float BaseBaselineOffset = 26f;

        /// <summary>How far past its default (40px) <c>compass-height-pixels</c> is - the extra length added to each tick's vertical line.</summary>
        private static float TickExtraHeight(float height) => Mathf.Max(height - 40f, 0f);

        /// <summary>
        /// The actual Y offset used this frame: the tick line grows by
        /// <see cref="TickExtraHeight"/> symmetrically about its own local
        /// center (see <see cref="CompassTick.ApplyHeight"/>), and this
        /// shared baseline/tick-root Y is pushed down by half of that same
        /// growth - the two shifts compose into a line whose top edge stays
        /// fixed on screen while its bottom edge extends further down, with
        /// the baseline dragged along to stay centered on the line the
        /// whole time, rather than left behind at the original top.
        /// </summary>
        private static float BaselineOffset(float tickExtraHeight) => BaseBaselineOffset + tickExtraHeight * 0.5f;

        private void ApplyLayout(PluginConfig cfg, float baselineY)
        {
            float width = cfg.CompassWidthPixels.Value;

            _root.anchoredPosition = new Vector2(cfg.CompassHorizontalOffsetPixels.Value, -cfg.CompassVerticalOffsetPixels.Value);

            _baseline.sizeDelta = new Vector2(width, BaselineThicknessPixels * cfg.CompassLineThicknessMultiplier.Value);
            _baseline.anchoredPosition = new Vector2(0f, -baselineY);
        }

        /// <summary>
        /// The <c>CompassPointer</c> child component of whatever the local player
        /// is currently holding, or null if that item has none - PEAK has no
        /// dedicated "Compass" item class, it's a data-driven <c>Item</c> like
        /// any other, identified this way instead. Exposed publicly so
        /// <see cref="PirateCompass.PirateCompassLuggageIndicatorController"/>
        /// can tell a held Pirate's Compass apart from a Normal/Warp one without
        /// re-deriving this same lookup.
        /// </summary>
        public static CompassPointer GetHeldCompassPointer()
        {
            Item current = Character.localCharacter?.data?.currentItem;
            return current != null ? current.GetComponentInChildren<CompassPointer>() : null;
        }

        /// <summary>Whether the local player is currently holding an in-game compass item of any <c>CompassPointer.CompassType</c>.</summary>
        private static bool IsHoldingCompassItem() => GetHeldCompassPointer() != null;

        private void Update()
        {
            NativeAssets.TryFindAll();
            PluginConfig cfg = Plugin.Instance.Cfg;
            Camera camera = _cameraOverride != null ? _cameraOverride : Camera.main;

            // The preview's tape isn't gated on the live player at all: it has no
            // local character to speak of, and blanking it out because you happen
            // not to be holding a compass item right now would just look broken to
            // someone who came here to look at the compass. enable-compass still
            // gates it - turning the mechanic off *should* empty the preview.
            bool gatedOnPlayer = !IsDetached;
            bool requiresHeldItem = gatedOnPlayer && cfg.CompassRequiresHoldingItem.Value;
            if (!cfg.EnableCompass.Value || camera == null
                || (gatedOnPlayer && Character.localCharacter == null)
                || (requiresHeldItem && !IsHoldingCompassItem()))
            {
                _root.gameObject.SetActive(false);
                return;
            }
            _root.gameObject.SetActive(true);

            float tickExtraHeight = TickExtraHeight(cfg.CompassMarkerGapPixels.Value);
            float baselineY = BaselineOffset(tickExtraHeight);
            ApplyLayout(cfg, baselineY);

            Transform camTransform = camera.transform;
            Vector3 camPos = camTransform.position;
            Vector3 flatForward = new Vector3(camTransform.forward.x, 0f, camTransform.forward.z);
            if (flatForward.sqrMagnitude < 0.0001f)
            {
                flatForward = Vector3.forward;
            }
            float cameraYaw = Mathf.Atan2(flatForward.x, flatForward.z) * Mathf.Rad2Deg;

            float halfFov = cfg.CompassFovDegrees.Value * 0.5f;
            float halfWidth = cfg.CompassWidthPixels.Value * 0.5f;

            UpdateTicks(cameraYaw, halfFov, halfWidth, baselineY, tickExtraHeight);
            UpdateMarkers(cameraYaw, halfFov, halfWidth, baselineY, camPos, cfg);
        }

        private void UpdateTicks(float cameraYaw, float halfFov, float halfWidth, float baselineY, float tickExtraHeight)
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            Color lineColor = CompassTheme.LineColor(cfg.CompassLineColor.Value);
            _baselineImage.color = new Color(lineColor.r, lineColor.g, lineColor.b, 0.55f);

            foreach (CompassTick tick in _ticks)
            {
                tick.ApplyHeight(tickExtraHeight, cfg.CompassLineThicknessMultiplier.Value);
                tick.ApplyLineColor(lineColor);

                if (NativeAssets.Font != null && tick.Label.font != NativeAssets.Font)
                {
                    tick.Label.font = NativeAssets.Font;
                }
                if (NativeAssets.OutlineMaterial != null && tick.Label.fontSharedMaterial != NativeAssets.OutlineMaterial)
                {
                    tick.Label.fontSharedMaterial = NativeAssets.OutlineMaterial;
                }

                float relative = Mathf.DeltaAngle(cameraYaw, tick.Degrees);
                float absRelative = Mathf.Abs(relative);
                if (absRelative > halfFov)
                {
                    tick.CanvasGroup.alpha = 0f;
                    continue;
                }

                float x = (relative / halfFov) * halfWidth;
                tick.Rect.anchoredPosition = new Vector2(x, -baselineY);
                tick.CanvasGroup.alpha = ComputeEdgeFade(absRelative, halfFov);

                bool isCardinal = Mathf.Approximately(tick.Degrees % 90f, 0f);
                if (isCardinal)
                {
                    tick.Label.text = CardinalLabel(tick.Degrees);
                    tick.Label.gameObject.SetActive(true);
                }
                else if (cfg.CompassShowDegreeNumbers.Value)
                {
                    tick.Label.text = Mathf.RoundToInt(tick.Degrees).ToString();
                    tick.Label.gameObject.SetActive(true);
                }
                else
                {
                    tick.Label.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateMarkers(float cameraYaw, float halfFov, float halfWidth, float baselineY, Vector3 camPos, PluginConfig cfg)
        {
            // Reused rather than reallocated per frame (this runs every frame,
            // for every registered anchor).
            _seenScratch.Clear();
            _staleScratch.Clear();
            _overlapCandidates.Clear();

            IReadOnlyList<IndicatorAnchor> anchors = _anchorSource != null
                ? _anchorSource()
                : IndicatorManager.Instance.Anchors;

            foreach (IndicatorAnchor anchor in anchors)
            {
                if (anchor.CompassKind == CompassMarkerKind.None)
                {
                    continue;
                }
                _seenScratch.Add(anchor);

                bool wantsCompass = anchor.GetPlacement() != IndicatorPlacement.OffScreenOnly;
                bool structurallyOk = anchor.IsActive() && anchor.IsCompassVisible();

                if (!_markers.TryGetValue(anchor, out CompassMarkerWidget widget))
                {
                    if (!wantsCompass || !structurallyOk)
                    {
                        continue;
                    }
                    widget = CompassMarkerWidget.Rent(_root, anchor.CompassKind);
                    widget.CanvasGroup.alpha = 0f; // fades in below instead of popping in at full alpha
                    _markers[anchor] = widget;

                    if (anchor.CompassSpawnPop)
                    {
                        _markerPopElapsed[anchor] = 0f;
                        widget.Root.localScale = Vector3.zero;
                    }
                    else
                    {
                        widget.Root.localScale = Vector3.one;
                    }
                }

                if (_markerPopElapsed.TryGetValue(anchor, out float popElapsed))
                {
                    ApplyMarkerPop(anchor, widget, popElapsed);
                }

                if (!wantsCompass || !structurallyOk)
                {
                    // See IndicatorAnchor.CompassInstantHide's own doc comment:
                    // an anchor that opts into this skips the gradual fade so a
                    // reactivation mid-fade can never show its stale, frozen
                    // position for even a frame.
                    if (anchor.CompassInstantHide)
                    {
                        widget.CanvasGroup.alpha = 0f;
                    }
                    else
                    {
                        FadeMarkerAlpha(widget, 0f);
                    }
                    continue;
                }

                Vector3 worldPos = anchor.GetWorldPosition();
                Vector3 toTarget = worldPos - camPos;
                Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
                if (flat.sqrMagnitude < 0.0001f)
                {
                    FadeMarkerAlpha(widget, 0f);
                    continue;
                }

                float targetYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                float relative = Mathf.DeltaAngle(cameraYaw, targetYaw);
                float absRelative = Mathf.Abs(relative);
                bool outsideFov = absRelative > halfFov;
                if (outsideFov && !cfg.CompassClampIconsToEdge.Value)
                {
                    FadeMarkerAlpha(widget, 0f);
                    continue;
                }

                float x = outsideFov
                    ? halfWidth * Mathf.Sign(relative)
                    : (relative / halfFov) * halfWidth;
                widget.Root.anchoredPosition = new Vector2(x, -baselineY);

                _overlapCandidates.Add(anchor);
                _markerBaseX[anchor] = x;

                // With clamp-to-edge on, the fade floor is ClampedEdgeAlpha
                // instead of 0 (both inside the fade-out quarter and once
                // past the FOV cutoff) - keeps the curve continuous straight
                // through the boundary instead of fading towards 0 right up
                // to the cutoff and then jumping back up to ClampedEdgeAlpha
                // the instant it's crossed, which read as a snap.
                bool clamp = cfg.CompassClampIconsToEdge.Value;
                float targetAlpha = outsideFov
                    ? ClampedEdgeAlpha
                    : ComputeEdgeFade(absRelative, halfFov, clamp ? ClampedEdgeAlpha : 0f);
                FadeMarkerAlpha(widget, targetAlpha);

                // Measured from the local player's own viewpoint in game, but
                // from the preview's fake camera when detached - there's no local
                // character standing anywhere near the preview's world points, so
                // measuring from the real one would report nonsense distances.
                Vector3 viewpoint = IsDetached ? camPos : CharacterPositions.LocalViewpoint();
                float distanceMeters = Vector3.Distance(viewpoint, worldPos) * CharacterStats.unitsToMeters;

                float elevationDelta = worldPos.y - camPos.y;
                float elevationThresholdWorldUnits = cfg.CompassElevationThresholdMeters.Value / CharacterStats.unitsToMeters;
                CompassElevation elevation = CompassElevation.None;
                if (elevationDelta > elevationThresholdWorldUnits)
                {
                    elevation = CompassElevation.Above;
                }
                else if (elevationDelta < -elevationThresholdWorldUnits)
                {
                    elevation = CompassElevation.Below;
                }

                widget.Refresh(
                    cfg.CompassIconSizePixels.Value,
                    anchor.GetCompassColor(),
                    cfg.CompassShowNames.Value ? anchor.GetCompassLabel() : null,
                    distanceMeters,
                    cfg.CompassShowNames.Value,
                    cfg.CompassShowDistances.Value,
                    elevation,
                    anchor.GetIsDead(),
                    anchor.GetIsUnconscious(),
                    anchor.GetCompassIcon());

                // Overlap resolution (see ResolveMarkerOverlaps) only pushes
                // markers apart along the tape's one axis (all share the same
                // baseline Y), so only the box width matters. Measured from the
                // now-current text rather than a fixed reservation, so a short
                // label claims only the room it needs instead of a blanket 160px
                // - that fixed width was what made lone/short-labelled markers
                // falsely collide and get shoved onto a second row (ISSUES.md).
                _markerSize[anchor] = new Vector2(
                    widget.MeasureOverlapWidth(cfg.CompassIconSizePixels.Value),
                    MarkerLabelHeight);
            }

            ResolveMarkerOverlaps();

            // Not gated on _markers.Count > seen.Count: a registered-but-not-yet-
            // materialized anchor (seen this frame but structurally not ok yet, so
            // it has no marker) can coincidentally match the count of a genuinely
            // stale entry below, making the counts equal even though a stale
            // marker is still sitting in _markers - that coincidence used to skip
            // this whole cleanup pass, letting the stale marker linger on the
            // compass indefinitely (root cause of the "entries permanently stay
            // on the compass" bug). Just always check for stale keys directly.
            foreach (IndicatorAnchor candidate in _markers.Keys)
            {
                if (!_seenScratch.Contains(candidate))
                {
                    _staleScratch.Add(candidate);
                }
            }

            foreach (IndicatorAnchor stale in _staleScratch)
            {
                CompassMarkerWidget widget = _markers[stale];
                FadeMarkerAlpha(widget, 0f);
                if (widget.CanvasGroup.alpha <= 0.01f)
                {
                    CompassMarkerWidget.Release(widget);
                    _markers.Remove(stale);
                    _markerBaseX.Remove(stale);
                    _markerSize.Remove(stale);
                    _markerOverlapOffset.Remove(stale);
                    _markerLabelCompaction.Remove(stale);
                    _markerPopElapsed.Remove(stale);
                }
            }
        }

        /// <summary>
        /// Advances one marker's spawn pop and applies it to <see cref="CompassMarkerWidget.Root"/>'s
        /// scale - Luggage-Ping's own extra flourish (<see cref="IndicatorAnchor.CompassSpawnPop"/>)
        /// layered on top of the ordinary alpha fade-in every marker already
        /// gets, since a luggage ping can drop a whole burst of markers onto
        /// the tape at once and a little overshoot helps them read as "new"
        /// rather than just materializing. Ease-out-back: eases toward 1 but
        /// overshoots past it first, same shape as a UI element "popping" into
        /// place, then settles - not a bounce that keeps oscillating.
        /// </summary>
        private void ApplyMarkerPop(IndicatorAnchor anchor, CompassMarkerWidget widget, float elapsed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / MarkerPopDurationSeconds);

            const float overshoot = 1.70158f;
            float shifted = t - 1f;
            float scale = 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
            widget.Root.localScale = Vector3.one * scale;

            if (t >= 1f)
            {
                widget.Root.localScale = Vector3.one;
                _markerPopElapsed.Remove(anchor);
            }
            else
            {
                _markerPopElapsed[anchor] = elapsed;
            }
        }

        /// <summary>
        /// Second pass, run after every marker's own "natural" bearing-driven
        /// X is already set above: nudges apart any markers whose boxes
        /// overlap, per <see cref="Indicators.LabelOverlapResolver"/> - a
        /// single fixed direction (always away from tape center), never
        /// based on list order or which specific neighbor conflicts, so it
        /// can't misplace itself over time as unrelated anchors come and go.
        /// List order (<c>IndicatorManager.Anchors</c>' own registration
        /// order, same priority convention <see cref="Indicators.IndicatorManager"/>
        /// itself uses) doubles as priority. Applied to
        /// <see cref="CompassMarkerWidget.LabelGroup"/>, not
        /// <see cref="CompassMarkerWidget.Root"/>, so a marker's icon always
        /// stays exactly on its real bearing - only its text may shift
        /// slightly to dodge a neighboring marker's own text. The resulting
        /// offset is smoothed towards its target rather than applied
        /// directly, so a marker's label sliding into/out of overlap doesn't
        /// snap.
        /// </summary>
        private void ResolveMarkerOverlaps()
        {
            if (_overlapCandidates.Count == 0)
            {
                return;
            }

            // Off means every marker's label just sits directly above/below
            // its icon, same as before this feature existed - target offsets
            // all stay zero (still smoothed towards, so toggling this off
            // mid-overlap eases labels back instead of snapping them).
            bool enabled = Plugin.Instance.Cfg.EnableLabelOverlapAvoidance.Value;

            Vector2[] targetOffsets;
            if (enabled)
            {
                _overlapBasePositionsScratch.Clear();
                _overlapSizesScratch.Clear();
                foreach (IndicatorAnchor anchor in _overlapCandidates)
                {
                    _overlapBasePositionsScratch.Add(new Vector2(_markerBaseX[anchor], 0f));
                    _overlapSizesScratch.Add(_markerSize[anchor]);
                }

                targetOffsets = Indicators.LabelOverlapResolver.ComputeOffsets(
                    _overlapBasePositionsScratch,
                    _overlapSizesScratch,
                    Indicators.LabelOverlapResolver.Axis.Horizontal,
                    MarkerMaxOverlapOffset,
                    MarkerRowStaggerPixels,
                    MarkerMaxRows);
            }
            else
            {
                targetOffsets = Indicators.LabelOverlapResolver.ZeroOffsets(_overlapCandidates.Count);
            }

            for (int i = 0; i < _overlapCandidates.Count; i++)
            {
                IndicatorAnchor anchor = _overlapCandidates[i];
                Vector2 currentOffset = _markerOverlapOffset.TryGetValue(anchor, out Vector2 existing) ? existing : Vector2.zero;
                Vector2 smoothedOffset = Vector2.MoveTowards(currentOffset, targetOffsets[i], Time.deltaTime * OverlapOffsetSpeedPixelsPerSecond);
                _markerOverlapOffset[anchor] = smoothedOffset;

                CompassMarkerWidget widget = _markers[anchor];
                widget.LabelGroup.anchoredPosition = smoothedOffset;

                // A label staggered onto row 2/3 has left its icon behind on the
                // tape, so the icon-sized gap its name/distance lines straddle is
                // now an empty hole - and which distance line belongs to which
                // name stops being obvious. Close the two lines up while it's off
                // the tape's own row, and open them back up when it returns.
                // Derived from the *target* row, not the smoothed offset, so the
                // compaction leads the move instead of chasing it, and eased at
                // the same rate so both finish together.
                float targetCompaction = targetOffsets[i].y <= -MarkerRowStaggerPixels * 0.5f ? 1f : 0f;
                float currentCompaction = _markerLabelCompaction.TryGetValue(anchor, out float existingCompaction) ? existingCompaction : 0f;
                float smoothedCompaction = Mathf.MoveTowards(currentCompaction, targetCompaction, Time.deltaTime * LabelCompactionSpeedPerSecond);
                _markerLabelCompaction[anchor] = smoothedCompaction;
                widget.SetLabelCompaction(smoothedCompaction);
            }
        }

        /// <summary>Steps a marker's <c>CanvasGroup.alpha</c> towards <paramref name="targetAlpha"/> at <see cref="MarkerFadeSpeedPerSecond"/> instead of snapping directly to it - covers both fade-in (newly created, alpha starts at 0) and fade-out (going out of range/FOV, or the owning anchor disappearing entirely).</summary>
        private static void FadeMarkerAlpha(CompassMarkerWidget widget, float targetAlpha)
        {
            widget.CanvasGroup.alpha = Mathf.MoveTowards(widget.CanvasGroup.alpha, targetAlpha, Time.deltaTime * MarkerFadeSpeedPerSecond);
        }

        /// <summary>Smoothly fades a mark out over the last quarter of the visible half-FOV, rather than a hard pop at the exact cutoff. <paramref name="minAlpha"/> is the floor the fade approaches instead of 0 - used by the compass-clamp-icons-to-edge path so the curve lands exactly on <see cref="ClampedEdgeAlpha"/> at the FOV cutoff instead of continuing towards 0.</summary>
        private static float ComputeEdgeFade(float absRelativeDegrees, float halfFov, float minAlpha = 0f)
        {
            float fadeStart = halfFov * 0.75f;
            if (absRelativeDegrees <= fadeStart)
            {
                return 1f;
            }
            float t = Mathf.Clamp01((absRelativeDegrees - fadeStart) / (halfFov - fadeStart));
            return Mathf.Lerp(1f, minAlpha, t);
        }

        private static string CardinalLabel(float degrees)
        {
            int normalized = ((Mathf.RoundToInt(degrees) % 360) + 360) % 360;
            return normalized switch
            {
                0 => "N",
                90 => "E",
                180 => "S",
                270 => "W",
                _ => "",
            };
        }
    }
}
