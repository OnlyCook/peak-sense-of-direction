using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// Phase 2 debug tool: spawns a handful of fixed dummy world points around
    /// the camera the first time one is found, so the edge-of-screen indicator
    /// framework can be visually verified in-game before Mechanic 1/2 wire in
    /// real players/pings. Gated behind PluginConfig.EnableIndicatorTestHarness
    /// - never on by default.
    /// </summary>
    public class IndicatorTestHarness : MonoBehaviour
    {
        private readonly List<IndicatorAnchor> _anchors = new List<IndicatorAnchor>();
        private bool _spawned;

        private void Update()
        {
            if (_spawned)
            {
                return;
            }
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }
            SpawnDummyAnchors(camera.transform.position);
            _spawned = true;
        }

        private void SpawnDummyAnchors(Vector3 origin)
        {
            var points = new (string label, Vector3 offset, Color color)[]
            {
                ("N", new Vector3(0f, 0f, 25f), Color.red),
                ("E", new Vector3(25f, 0f, 0f), Color.green),
                ("S", new Vector3(0f, 0f, -25f), Color.cyan),
                ("W", new Vector3(-25f, 0f, 0f), Color.yellow),
                ("Up", new Vector3(0f, 15f, 8f), Color.white),
                ("Far", new Vector3(0f, 2f, 400f), Color.magenta),
            };

            foreach (var (label, offset, color) in points)
            {
                Vector3 worldPos = origin + offset;
                RectTransform widget = BuildWidget(label, color, out RectTransform arrow);
                var anchor = new IndicatorAnchor(() => worldPos, widget, arrow);
                IndicatorManager.Instance.RegisterAnchor(anchor);
                _anchors.Add(anchor);
            }
        }

        private RectTransform BuildWidget(string label, Color color, out RectTransform arrow)
        {
            RectTransform canvasTransform = IndicatorManager.Instance.CanvasTransform;

            var root = new GameObject($"SoD.TestAnchor.{label}", typeof(RectTransform));
            var rootRect = (RectTransform)root.transform;
            rootRect.SetParent(canvasTransform, false);
            rootRect.sizeDelta = new Vector2(24f, 24f);

            var dot = new GameObject("Dot", typeof(RectTransform), typeof(Image));
            var dotRect = (RectTransform)dot.transform;
            dotRect.SetParent(rootRect, false);
            dotRect.sizeDelta = new Vector2(20f, 20f);
            dot.GetComponent<Image>().color = color;

            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            var arrowRect = (RectTransform)arrowGo.transform;
            arrowRect.SetParent(rootRect, false);
            arrowRect.sizeDelta = new Vector2(14f, 26f);
            arrowRect.pivot = new Vector2(0.5f, 0.15f);
            arrowRect.anchoredPosition = Vector2.zero;
            arrowGo.GetComponent<Image>().color = color;
            arrow = arrowRect;

            return rootRect;
        }
    }
}
