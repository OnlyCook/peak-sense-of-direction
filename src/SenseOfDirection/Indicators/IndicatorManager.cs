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

        private readonly List<IndicatorAnchor> _anchors = new List<IndicatorAnchor>();
        private Canvas _canvas;

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

                bool active = camera != null && anchor.IsActive();
                anchor.Widget.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var state = ScreenSpaceTracker.Compute(camera, canvasSize, anchor.GetWorldPosition(), anchor.EdgeMarginPixels);
                anchor.Widget.anchoredPosition = state.CanvasPosition;

                if (anchor.ArrowWidget != null)
                {
                    anchor.ArrowWidget.gameObject.SetActive(state.IsOffScreen);
                    if (state.IsOffScreen)
                    {
                        // Sprite convention: arrow art points "up" (+Y) at rotation 0.
                        // Confirmed via in-game test harness that the naive
                        // (angle - 90) offset renders exactly backwards, so this
                        // is +90, not -90 - do not "simplify" this back.
                        anchor.ArrowWidget.localEulerAngles = new Vector3(0f, 0f, state.ArrowAngleDegrees + 90f);
                    }
                }
            }
        }
    }
}
