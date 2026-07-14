using System;
using System.Collections.Generic;
using SenseOfDirection.Common;
using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Pings
{
    /// <summary>
    /// Screen-space indicator for one active ping: an optional distance
    /// sub-line, and - unlike <see cref="Labels.PlayerLabel"/> and the
    /// campfire indicator, which clamp quietly to the edge - an off-screen
    /// arrow child, since the arrow is reserved for pings per maintainer
    /// direction (see those classes' own doc comments). Deliberately no
    /// on-screen marker/dot: the real 3D ping is already visible on-screen,
    /// so a 2D UI element drawn on top of it just obstructs the view; the
    /// arrow (via <see cref="IndicatorManager"/>'s own off-screen gating)
    /// only ever appears once the ping has actually left the screen. Tinted
    /// to the pinging player's own character color - both the off-screen
    /// arrow and the distance sub-line.
    ///
    /// Pooled, for the same reason <see cref="ItemPings.ItemPingWidget"/> is:
    /// one is built per ping and thrown away seconds later, so a burst of pings
    /// otherwise means a steady churn of GameObjects and TMP meshes right when
    /// the game is busiest.
    /// </summary>
    public class PingWidget
    {
        private static readonly Stack<PingWidget> Pool = new Stack<PingWidget>();

        /// <summary>Size the distance line is tuned at; the `Fonts` section scales this rather than replacing it (see <see cref="HudFontScale"/>).</summary>
        private const float DistanceFontSizeBase = 16f;

        public IndicatorAnchor Anchor { get; private set; }
        public CanvasGroup CanvasGroup { get; }

        private readonly RectTransform _root;
        private readonly RectTransform _arrow;
        private readonly Image _arrowImage;
        private readonly RectTransform _labelGroup;
        private readonly TMP_Text _distanceText;

        /// <summary>See <see cref="ItemPings.ItemPingWidget"/>'s own measurement cache - <c>GetPreferredValues()</c> is a full text layout pass, and this widget is refreshed every frame for as long as its ping lives.</summary>
        private string _measuredDistance;
        private float _measuredDistanceWidth;

        private int _lastDistanceMeters = int.MinValue;

        private PingWidget(RectTransform root, CanvasGroup canvasGroup, RectTransform arrow, Image arrowImage, RectTransform labelGroup, TMP_Text distanceText)
        {
            _root = root;
            CanvasGroup = canvasGroup;
            _arrow = arrow;
            _arrowImage = arrowImage;
            _labelGroup = labelGroup;
            _distanceText = distanceText;
        }

        public static PingWidget Rent(Func<Vector3> getWorldPosition, Color color, bool enableArrow)
        {
            PingWidget widget = Pool.Count > 0 ? Pool.Pop() : Build();
            widget.Bind(getWorldPosition, color, enableArrow);
            return widget;
        }

        /// <summary>Builds widgets up front and parks them in the pool - see <see cref="Common.PingPrewarm"/>.</summary>
        public static void Prewarm(int count)
        {
            while (Pool.Count < count)
            {
                PingWidget widget = Build();
                widget.WarmText();
                Pool.Push(widget);
            }
        }

        /// <summary>
        /// A widget built into somewhere other than the live overlay canvas (the
        /// config preview menu's own stage), deliberately outside the pool: a
        /// pooled widget is parented to the live canvas and gets recycled by the
        /// next real ping, neither of which is true for one that has to live in
        /// the preview for as long as that menu is open.
        /// </summary>
        internal static PingWidget CreateDetached(RectTransform parent, Func<Vector3> getWorldPosition, Color color, bool enableArrow)
        {
            PingWidget widget = Build(parent);
            widget.Bind(getWorldPosition, color, enableArrow);

            // Bind() points the anchor's release at the shared pool, which would
            // be actively wrong here: this widget is parented to the preview
            // menu's stage, so a real ping renting it back out of the pool would
            // end up with its indicator drawn inside a closed menu instead of on
            // screen. A detached widget is simply destroyed when it goes away.
            widget.Anchor.ReleaseWidget = () => UnityEngine.Object.Destroy(widget._root.gameObject);
            return widget;
        }

        private static PingWidget Build(RectTransform parent = null)
        {
            RectTransform canvasTransform = parent != null ? parent : IndicatorManager.Instance.CanvasTransform;

            var rootGo = new GameObject("SoD.PingIndicator", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(canvasTransform, false);
            root.sizeDelta = new Vector2(20f, 20f);
            rootGo.SetActive(false);

            var canvasGroup = rootGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Always built, shown or hidden per binding: the off-screen arrow
            // is a live config toggle, and a pooled widget can be rented next
            // under the opposite setting.
            RectTransform arrowRect = OffScreenArrow.Create(root, Color.white);
            Image arrowImage = arrowRect.GetComponent<Image>();

            // Home position (0,0) relative to root - overlap resolution
            // nudges this transform, not root/arrow (see the LabelWidget
            // assignment in Bind).
            var labelGroupGo = new GameObject("LabelGroup", typeof(RectTransform));
            var labelGroupRect = (RectTransform)labelGroupGo.transform;
            labelGroupRect.SetParent(root, false);

            var textGo = new GameObject("Distance", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(labelGroupRect, false);
            textRect.sizeDelta = new Vector2(120f, 24f);
            textRect.anchoredPosition = new Vector2(0f, -22f);

            var distanceText = textGo.GetComponent<TextMeshProUGUI>();
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.fontSize = DistanceFontSizeBase;
            distanceText.enableWordWrapping = false;

            return new PingWidget(root, canvasGroup, arrowRect, arrowImage, labelGroupRect, distanceText);
        }

        private void Bind(Func<Vector3> getWorldPosition, Color color, bool enableArrow)
        {
            _distanceText.color = color;
            _arrowImage.color = color;
            _arrow.gameObject.SetActive(enableArrow);

            _labelGroup.anchoredPosition = Vector2.zero;
            CanvasGroup.alpha = 1f;
            _root.gameObject.SetActive(true);

            // LabelWidget = labelGroup (not root) so overlap resolution only
            // ever nudges the distance text, never the off-screen arrow -
            // the arrow needs to stay exactly on the tracked position to
            // still point the right way. OverlapSize itself is left at
            // (0,0) here and only ever set in Refresh() while the distance
            // text is actually visible (see there) - a ping's distance line
            // is often suppressed entirely (e.g. an item ping already
            // showing distance for the same event, per PointPingerPatches),
            // and a stale nonzero box on an invisible widget was pushing
            // other, real labels away from a target they'd otherwise have
            // no reason to avoid.
            Anchor = new IndicatorAnchor(getWorldPosition, _root, enableArrow ? _arrow : null)
            {
                LabelWidget = _labelGroup,
                ReleaseWidget = Release,
            };
        }

        /// <summary>Handed back to the pool by <see cref="IndicatorManager.UnregisterAnchor"/> (via <see cref="IndicatorAnchor.ReleaseWidget"/>) once the ping has finished fading out.</summary>
        private void Release()
        {
            // A destroyed root (scene change, or something else tearing the
            // canvas down) must not go back into the pool - the next renter
            // would get a widget whose GameObjects no longer exist.
            if (_root == null)
            {
                return;
            }

            Anchor = null;
            _root.gameObject.SetActive(false);
            CanvasGroup.alpha = 1f;
            Pool.Push(this);
        }

        /// <summary>Drives one throwaway layout/mesh build so a prewarmed widget's first real Refresh isn't also TMP's first.</summary>
        private void WarmText()
        {
            _distanceText.text = "0m";
            _distanceText.ForceMeshUpdate();
            _distanceText.text = string.Empty;
            _measuredDistance = null;
            _lastDistanceMeters = int.MinValue;
        }

        public void Refresh(float distanceMeters, bool showDistance)
        {
            if (NativeAssets.Font != null && _distanceText.font != NativeAssets.Font)
            {
                // Voids the cached width below - the same string renders at a
                // different size under a different font.
                _distanceText.font = NativeAssets.Font;
                _measuredDistance = null;
            }
            if (NativeAssets.OutlineMaterial != null && _distanceText.fontSharedMaterial != NativeAssets.OutlineMaterial)
            {
                _distanceText.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            // Live config value, so it's re-applied every frame rather than
            // baked in at creation. The measured width cached below is keyed on
            // the string alone, so a size change has to void it too - the same
            // "12m" renders wider at a bigger font.
            float fontSize = HudFontScale.Distance(DistanceFontSizeBase, Anchor.OffScreenBlend);
            if (!Mathf.Approximately(_distanceText.fontSize, fontSize))
            {
                _distanceText.fontSize = fontSize;
                _measuredDistance = null;
            }

            _distanceText.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                // Text is only rebuilt when the whole-metre reading actually
                // changes: the interpolation allocates and the assignment
                // schedules a TMP mesh rebuild, both of which used to happen
                // every frame for every live ping, even a stationary one.
                int rounded = Mathf.RoundToInt(distanceMeters);
                if (rounded != _lastDistanceMeters)
                {
                    _lastDistanceMeters = rounded;
                    _distanceText.text = $"{rounded}m";
                }

                if (!string.Equals(_measuredDistance, _distanceText.text, StringComparison.Ordinal))
                {
                    _measuredDistance = _distanceText.text;
                    _measuredDistanceWidth = _distanceText.GetPreferredValues().x;
                }

                // Just the distance line: 24 tall, anchored 22px below the
                // tracked point (hence the centre offset - the arrow itself
                // stays put and isn't part of the box).
                Anchor.OverlapSize = new Vector2(_measuredDistanceWidth + 12f, 28f);
                Anchor.OverlapCenterOffset = new Vector2(0f, -22f);
            }
            else
            {
                Anchor.OverlapSize = Vector2.zero;
                Anchor.OverlapCenterOffset = Vector2.zero;
            }
        }
    }
}
