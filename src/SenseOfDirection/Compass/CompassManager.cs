using System.Collections.Generic;
using System.Linq;
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
                }
                return _instance;
            }
        }

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

        private readonly List<IndicatorAnchor> _overlapCandidates = new List<IndicatorAnchor>();
        private readonly List<Vector2> _overlapBasePositionsScratch = new List<Vector2>();
        private readonly List<Vector2> _overlapSizesScratch = new List<Vector2>();
        private readonly Dictionary<IndicatorAnchor, float> _markerBaseX = new Dictionary<IndicatorAnchor, float>();
        private readonly Dictionary<IndicatorAnchor, Vector2> _markerSize = new Dictionary<IndicatorAnchor, Vector2>();
        private readonly Dictionary<IndicatorAnchor, Vector2> _markerOverlapOffset = new Dictionary<IndicatorAnchor, Vector2>();

        /// <summary>Approximate vertical span of a marker's name-above/distance-below text, for overlap detection - see the horizontal-only note on <see cref="ResolveMarkerOverlaps"/>'s own use of it.</summary>
        private const float MarkerLabelHeight = 70f;

        private void Awake()
        {
            BuildUi();
            BuildTicks();
        }

        private void BuildUi()
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

            var rootGo = new GameObject("Root", typeof(RectTransform));
            _root = (RectTransform)rootGo.transform;
            _root.SetParent(canvasGo.transform, false);
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

        /// <summary>Whether the local player is currently holding an in-game compass item (identified by its <c>CompassPointer</c> child component - PEAK has no dedicated "Compass" item class, it's a data-driven <c>Item</c> like any other).</summary>
        private static bool IsHoldingCompassItem()
        {
            Item current = Character.localCharacter?.data?.currentItem;
            return current != null && current.GetComponentInChildren<CompassPointer>() != null;
        }

        private void Update()
        {
            NativeAssets.TryFindAll();
            PluginConfig cfg = Plugin.Instance.Cfg;
            Camera camera = Camera.main;

            bool requiresHeldItem = cfg.CompassRequiresHoldingItem.Value;
            if (!cfg.EnableCompass.Value || camera == null || Character.localCharacter == null
                || (requiresHeldItem && !IsHoldingCompassItem()))
            {
                _root.gameObject.SetActive(false);
                return;
            }
            _root.gameObject.SetActive(true);

            float tickExtraHeight = TickExtraHeight(cfg.CompassHeightPixels.Value);
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
            var seen = new HashSet<IndicatorAnchor>();
            _overlapCandidates.Clear();

            foreach (IndicatorAnchor anchor in IndicatorManager.Instance.Anchors)
            {
                if (anchor.CompassKind == CompassMarkerKind.None)
                {
                    continue;
                }
                seen.Add(anchor);

                bool wantsCompass = anchor.GetDisplayMode() != IndicatorDisplayMode.OffScreenOnly;
                bool structurallyOk = anchor.IsActive() && anchor.IsCompassVisible();

                if (!_markers.TryGetValue(anchor, out CompassMarkerWidget widget))
                {
                    if (!wantsCompass || !structurallyOk)
                    {
                        continue;
                    }
                    widget = CompassMarkerWidget.Create(_root, anchor.CompassKind);
                    widget.CanvasGroup.alpha = 0f; // fades in below instead of popping in at full alpha
                    _markers[anchor] = widget;
                }

                if (!wantsCompass || !structurallyOk)
                {
                    FadeMarkerAlpha(widget, 0f);
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

                // Overlap resolution (see ResolveMarkerOverlaps) only ever
                // needs to push markers apart along the tape's one axis (all
                // markers already share the same baseline Y) - box width is
                // whichever of icon/name/distance is currently the widest
                // visible element, since a wide name label can overlap a
                // neighbor even when the icons themselves don't.
                float markerWidth = cfg.CompassIconSizePixels.Value;
                if (cfg.CompassShowNames.Value && !string.IsNullOrEmpty(anchor.GetCompassLabel()))
                {
                    markerWidth = Mathf.Max(markerWidth, 160f);
                }
                if (cfg.CompassShowDistances.Value)
                {
                    markerWidth = Mathf.Max(markerWidth, 120f);
                }
                _overlapCandidates.Add(anchor);
                _markerBaseX[anchor] = x;
                _markerSize[anchor] = new Vector2(markerWidth, MarkerLabelHeight);

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

                float distanceMeters = Vector3.Distance(CharacterPositions.LocalViewpoint(), worldPos) * CharacterStats.unitsToMeters;

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
                    anchor.GetIsUnconscious());
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
            foreach (IndicatorAnchor stale in _markers.Keys.Where(a => !seen.Contains(a)).ToList())
            {
                CompassMarkerWidget widget = _markers[stale];
                FadeMarkerAlpha(widget, 0f);
                if (widget.CanvasGroup.alpha <= 0.01f)
                {
                    widget.Destroy();
                    _markers.Remove(stale);
                    _markerBaseX.Remove(stale);
                    _markerSize.Remove(stale);
                    _markerOverlapOffset.Remove(stale);
                }
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

                targetOffsets = Indicators.LabelOverlapResolver.ComputeOffsets(_overlapBasePositionsScratch, _overlapSizesScratch, Indicators.LabelOverlapResolver.Axis.Horizontal);
            }
            else
            {
                targetOffsets = new Vector2[_overlapCandidates.Count];
            }

            for (int i = 0; i < _overlapCandidates.Count; i++)
            {
                IndicatorAnchor anchor = _overlapCandidates[i];
                Vector2 currentOffset = _markerOverlapOffset.TryGetValue(anchor, out Vector2 existing) ? existing : Vector2.zero;
                Vector2 smoothedOffset = Vector2.MoveTowards(currentOffset, targetOffsets[i], Time.deltaTime * OverlapOffsetSpeedPixelsPerSecond);
                _markerOverlapOffset[anchor] = smoothedOffset;

                _markers[anchor].LabelGroup.anchoredPosition = smoothedOffset;
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
