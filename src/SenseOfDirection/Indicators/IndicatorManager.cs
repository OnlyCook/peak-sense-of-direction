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
                }
                return _instance;
            }
        }

        /// <summary>Parent every registered anchor's widget under this.</summary>
        public RectTransform CanvasTransform { get; private set; }

        /// <summary>Read-only view for <see cref="Compass.CompassManager"/>, which drives its own top-of-screen markers off the same registered anchors instead of requiring a second registration call per mechanic.</summary>
        public IReadOnlyList<IndicatorAnchor> Anchors => _anchors;

        private readonly List<IndicatorAnchor> _anchors = new List<IndicatorAnchor>();
        private Canvas _canvas;

        /// <summary>Pixels/second the resolved overlap offset (see <see cref="LabelOverlapResolver"/>) is smoothed towards its target at - keeps labels sliding apart/back together instead of snapping as overlap starts/stops.</summary>
        private const float OverlapOffsetSpeedPixelsPerSecond = 240f;

        private readonly List<IndicatorAnchor> _overlapCandidates = new List<IndicatorAnchor>();
        private readonly List<Vector2> _overlapBasePositionsScratch = new List<Vector2>();
        private readonly List<Vector2> _overlapSizesScratch = new List<Vector2>();
        private readonly List<float> _overlapCapsScratch = new List<float>();
        private readonly Dictionary<IndicatorAnchor, Vector2> _overlapBasePosition = new Dictionary<IndicatorAnchor, Vector2>();

        /// <summary>Each candidate's overlap <em>box</em> centre (tracked position + <see cref="IndicatorAnchor.OverlapCenterOffset"/>) - what the resolver reasons about, as opposed to the widget position it gets applied to.</summary>
        private readonly Dictionary<IndicatorAnchor, Vector2> _overlapBoxPosition = new Dictionary<IndicatorAnchor, Vector2>();
        private readonly Dictionary<IndicatorAnchor, Vector2> _overlapOffset = new Dictionary<IndicatorAnchor, Vector2>();

        private void Awake()
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
            if (anchor.Widget != null)
            {
                Destroy(anchor.Widget.gameObject);
            }
        }

        private void LateUpdate()
        {
            // Sit just behind the game's own HUD canvas rather than a fixed
            // sky-high sorting order, so this overlay never draws over the
            // vanilla UI (pause menu, inventory bar, etc.) - only under it.
            if (GUIManager.instance != null && GUIManager.instance.hudCanvas != null)
            {
                _canvas.sortingOrder = GUIManager.instance.hudCanvas.sortingOrder - 1;
            }

            var camera = Camera.main;
            Vector2 canvasSize = CanvasTransform.rect.size;

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
                bool showOffScreenWidget = anchor.GetDisplayMode() != IndicatorDisplayMode.CompassOnly;
                bool active = camera != null && anchor.IsActive() && showOffScreenWidget;
                anchor.Widget.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var state = ScreenSpaceTracker.Compute(camera, canvasSize, anchor.GetWorldPosition(), anchor.EdgeMarginPixels);
                anchor.Widget.anchoredPosition = state.CanvasPosition;

                if (anchor.OverlapSize.x > 0f && anchor.OverlapSize.y > 0f)
                {
                    _overlapCandidates.Add(anchor);
                    _overlapBasePosition[anchor] = state.CanvasPosition;
                    _overlapBoxPosition[anchor] = state.CanvasPosition + anchor.OverlapCenterOffset;
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
                        anchor.ArrowWidget.localEulerAngles = new Vector3(0f, 0f, state.ArrowAngleDegrees - 90f);
                    }
                }

                if (anchor.OnScreenOnlyWidget != null)
                {
                    anchor.OnScreenOnlyWidget.gameObject.SetActive(!state.IsOffScreen);
                }
            }

            ResolveLabelOverlaps();
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
        private void ResolveLabelOverlaps()
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
            bool enabled = Plugin.Instance.Cfg.EnableLabelOverlapAvoidance.Value;

            Vector2[] targetOffsets;
            if (enabled)
            {
                _overlapBasePositionsScratch.Clear();
                _overlapSizesScratch.Clear();
                _overlapCapsScratch.Clear();
                foreach (IndicatorAnchor anchor in _overlapCandidates)
                {
                    _overlapBasePositionsScratch.Add(_overlapBoxPosition[anchor]);
                    _overlapSizesScratch.Add(anchor.OverlapSize);
                    _overlapCapsScratch.Add(anchor.MaxOverlapOffset);
                }

                // Vertical only, no row staggering: labels here have the whole
                // screen height to spread into, and pushing them sideways instead
                // (which is what a second "row" would mean on this axis) reads as
                // a diagonal jumble rather than a stack.
                targetOffsets = LabelOverlapResolver.ComputeOffsets(_overlapBasePositionsScratch, _overlapSizesScratch, LabelOverlapResolver.Axis.Vertical, _overlapCapsScratch);
            }
            else
            {
                targetOffsets = new Vector2[_overlapCandidates.Count];
            }

            for (int i = 0; i < _overlapCandidates.Count; i++)
            {
                IndicatorAnchor anchor = _overlapCandidates[i];
                Vector2 currentOffset = _overlapOffset.TryGetValue(anchor, out Vector2 existing) ? existing : Vector2.zero;
                Vector2 smoothedOffset = Vector2.MoveTowards(currentOffset, targetOffsets[i], Time.deltaTime * OverlapOffsetSpeedPixelsPerSecond);
                _overlapOffset[anchor] = smoothedOffset;

                // LabelWidget (when the anchor has one) is a local (0,0)-
                // homed child of Widget holding just the text - nudge that
                // instead of Widget itself, so an arrow/crosshair that needs
                // to stay exactly on the tracked position never moves.
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
}
