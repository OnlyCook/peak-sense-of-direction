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

        private RectTransform _root;
        private RectTransform _baseline;

        private readonly List<CompassTick> _ticks = new List<CompassTick>();
        private readonly Dictionary<IndicatorAnchor, CompassMarkerWidget> _markers = new Dictionary<IndicatorAnchor, CompassMarkerWidget>();

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
            var baselineImage = baselineGo.GetComponent<Image>();
            baselineImage.sprite = CompassIcons.HorizontalFadeLine;
            baselineImage.type = Image.Type.Simple;
            baselineImage.color = new Color(1f, 1f, 1f, 0.55f);
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

            _baseline.sizeDelta = new Vector2(width, BaselineThicknessPixels);
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

            foreach (CompassTick tick in _ticks)
            {
                tick.ApplyHeight(tickExtraHeight);

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
                    _markers[anchor] = widget;
                }

                if (!wantsCompass || !structurallyOk)
                {
                    widget.CanvasGroup.alpha = 0f;
                    continue;
                }

                Vector3 worldPos = anchor.GetWorldPosition();
                Vector3 toTarget = worldPos - camPos;
                Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
                if (flat.sqrMagnitude < 0.0001f)
                {
                    widget.CanvasGroup.alpha = 0f;
                    continue;
                }

                float targetYaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
                float relative = Mathf.DeltaAngle(cameraYaw, targetYaw);
                float absRelative = Mathf.Abs(relative);
                if (absRelative > halfFov)
                {
                    widget.CanvasGroup.alpha = 0f;
                    continue;
                }

                float x = (relative / halfFov) * halfWidth;
                widget.Root.anchoredPosition = new Vector2(x, -baselineY);
                widget.CanvasGroup.alpha = ComputeEdgeFade(absRelative, halfFov);

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

            if (_markers.Count > seen.Count)
            {
                foreach (IndicatorAnchor stale in _markers.Keys.Where(a => !seen.Contains(a)).ToList())
                {
                    _markers[stale].Destroy();
                    _markers.Remove(stale);
                }
            }
        }

        /// <summary>Smoothly fades a mark out over the last quarter of the visible half-FOV, rather than a hard pop at the exact cutoff.</summary>
        private static float ComputeEdgeFade(float absRelativeDegrees, float halfFov)
        {
            float fadeStart = halfFov * 0.75f;
            if (absRelativeDegrees <= fadeStart)
            {
                return 1f;
            }
            return Mathf.Clamp01(1f - (absRelativeDegrees - fadeStart) / (halfFov - fadeStart));
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
