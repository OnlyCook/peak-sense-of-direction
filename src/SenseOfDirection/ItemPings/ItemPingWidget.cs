using System;
using System.Collections.Generic;
using SenseOfDirection.Common;
using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// Screen-space widget for one item/luggage ping highlight: a name label
    /// (item name, or "Nx Item Name" when grouped) above an optional distance
    /// sub-line, plus the same off-screen arrow mechanism <see
    /// cref="Pings.PingWidget"/> uses - unlike player labels/the campfire
    /// indicator, which deliberately clamp quietly with no arrow (see those
    /// classes' own doc comments), the arrow makes sense here since this is
    /// pointing at a specific pinged object, same reasoning as the ping
    /// indicator itself. Tinted to the pinging player's own character color,
    /// same as the ping/ripple. A small crosshair (the same diamond icon the
    /// compass uses for item pings) sits between the name and distance line so
    /// the widget reads as a crosshair on the pinged object rather than just
    /// floating text.
    ///
    /// Pooled (<see cref="Rent"/>/<see cref="Release"/>), because one of these
    /// is built per pinged item group and thrown away a few seconds later:
    /// building the hierarchy means five GameObjects carrying two TMP texts and
    /// two Images, each of which allocates a mesh and pulls a material, and
    /// pinging a pile of loot builds several of them in the same frame. Rented
    /// widgets are re-bound (<see cref="Bind"/>) to a new target/color instead,
    /// and <see cref="Prewarm"/> fills the pool while nothing is happening, so
    /// even the very first ping of a session doesn't pay for the first one.
    /// </summary>
    public class ItemPingWidget
    {
        private static readonly Stack<ItemPingWidget> Pool = new Stack<ItemPingWidget>();

        public IndicatorAnchor Anchor { get; private set; }
        public CanvasGroup CanvasGroup { get; }

        private readonly RectTransform _root;
        private readonly RectTransform _arrow;
        private readonly RectTransform _crosshair;
        private readonly RectTransform _labelGroup;
        private readonly Image _arrowImage;
        private readonly Image _crosshairImage;
        private readonly TMP_Text _nameText;
        private readonly TMP_Text _distanceText;

        /// <summary>
        /// Last text each label was measured at, and the half-width that came
        /// out. <c>TMP_Text.GetPreferredValues()</c> runs a full text layout
        /// pass, and <see cref="Refresh"/> is called every frame by
        /// <see cref="ItemPingHighlight"/> - but the text only changes when the
        /// item name or the rounded distance does, so measuring on every frame
        /// was re-deriving a value that had not moved. Null means "not measured
        /// yet", which no real label text can be.
        /// </summary>
        private string _measuredName;
        private float _measuredNameHalfWidth;
        private string _measuredDistance;
        private float _measuredDistanceHalfWidth;

        private int _lastDistanceMeters = int.MinValue;

        /// <summary>The pinging player's color, kept so the crosshair can go back to being tinted with it if it stops showing a native icon (see <see cref="Refresh"/>).</summary>
        private Color _color = Color.white;

        /// <summary>
        /// Crosshair box size for the mod's own diamond, and for a native item
        /// icon (<c>use-native-item-ping-icons</c>) respectively. The icons are
        /// the game's own inventory art - busier, and drawn with padding inside
        /// their own texture - so at the diamond's size they read as a smudge
        /// rather than as the item. preserveAspect keeps non-square ones from
        /// stretching into the wider box.
        /// </summary>
        private const float CrosshairSizePixels = 30f;
        private const float NativeIconSizePixels = 44f;

        /// <summary>Sizes the name/distance lines are tuned at; the `Fonts` section scales these rather than replacing them (see <see cref="HudFontScale"/>).</summary>
        private const float NameFontSizeBase = 20f;
        private const float DistanceFontSizeBase = 16f;

        private ItemPingWidget(
            RectTransform root, CanvasGroup canvasGroup, RectTransform arrow, Image arrowImage,
            RectTransform crosshair, Image crosshairImage, RectTransform labelGroup,
            TMP_Text nameText, TMP_Text distanceText)
        {
            _root = root;
            CanvasGroup = canvasGroup;
            _arrow = arrow;
            _arrowImage = arrowImage;
            _crosshair = crosshair;
            _crosshairImage = crosshairImage;
            _labelGroup = labelGroup;
            _nameText = nameText;
            _distanceText = distanceText;
        }

        /// <summary>Takes a widget from the pool (building one only if it's empty) and binds it to a fresh target/color.</summary>
        public static ItemPingWidget Rent(Func<Vector3> getWorldPosition, Color color, bool enableArrow)
        {
            ItemPingWidget widget = Pool.Count > 0 ? Pool.Pop() : Build();
            widget.Bind(getWorldPosition, color, enableArrow);
            return widget;
        }

        /// <summary>
        /// Builds <paramref name="count"/> widgets up front and parks them in
        /// the pool - see <see cref="Common.PingPrewarm"/>, which also drives a
        /// throwaway text through each one so TMP has already built its mesh
        /// and font atlas entries by the time a real ping needs them.
        /// </summary>
        public static void Prewarm(int count)
        {
            while (Pool.Count < count)
            {
                ItemPingWidget widget = Build();
                widget.WarmText();
                Pool.Push(widget);
            }
        }

        /// <summary>Built into the config preview menu's stage instead of the live canvas, and outside the pool - see <see cref="Pings.PingWidget.CreateDetached"/> for why.</summary>
        internal static ItemPingWidget CreateDetached(RectTransform parent, Func<Vector3> getWorldPosition, Color color, bool enableArrow)
        {
            ItemPingWidget widget = Build(parent);
            widget.Bind(getWorldPosition, color, enableArrow);

            // Never hand a preview widget back to the shared pool - see
            // Pings.PingWidget.CreateDetached for the bug that would cause.
            widget.Anchor.ReleaseWidget = () => UnityEngine.Object.Destroy(widget._root.gameObject);
            return widget;
        }

        private static ItemPingWidget Build(RectTransform parent = null)
        {
            RectTransform canvasTransform = parent != null ? parent : IndicatorManager.Instance.CanvasTransform;

            var rootGo = new GameObject("SoD.ItemPingIndicator", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(canvasTransform, false);
            root.sizeDelta = new Vector2(20f, 20f);
            rootGo.SetActive(false);

            var canvasGroup = rootGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Always built, shown or hidden per binding (enable-item-ping-off-
            // screen-indicator is a live config toggle, and a pooled widget can
            // be rented next under the opposite setting).
            RectTransform arrowRect = OffScreenArrow.Create(root, Color.white);
            Image arrowImage = arrowRect.GetComponent<Image>();

            // Home position (0,0) relative to root - overlap resolution
            // nudges this transform, not root/arrow/crosshair (see the
            // LabelWidget assignment in Bind).
            var labelGroupGo = new GameObject("LabelGroup", typeof(RectTransform));
            var labelGroupRect = (RectTransform)labelGroupGo.transform;
            labelGroupRect.SetParent(root, false);

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.SetParent(labelGroupRect, false);
            nameRect.sizeDelta = new Vector2(320f, 28f);
            nameRect.anchoredPosition = new Vector2(0f, 24f);

            var nameText = nameGo.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontSize = NameFontSizeBase;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Overflow;

            var distGo = new GameObject("Distance", typeof(RectTransform), typeof(TextMeshProUGUI));
            var distRect = (RectTransform)distGo.transform;
            distRect.SetParent(labelGroupRect, false);
            distRect.sizeDelta = new Vector2(120f, 24f);
            distRect.anchoredPosition = new Vector2(0f, -18f);

            var distanceText = distGo.GetComponent<TextMeshProUGUI>();
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.fontSize = DistanceFontSizeBase;
            distanceText.enableWordWrapping = false;

            // Sits between the name and distance line (name bottom ~10px, distance
            // top ~-16px) so it reads as a crosshair on the pinged object itself,
            // same diamond icon the compass already uses for item pings. Only
            // shown while the target is actually on-screen - see
            // IndicatorAnchor.OnScreenOnlyWidget - since it makes no sense
            // overlaid on nothing while the off-screen arrow is showing instead.
            var crosshairGo = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
            var crosshairRect = (RectTransform)crosshairGo.transform;
            crosshairRect.SetParent(root, false);
            crosshairRect.sizeDelta = new Vector2(CrosshairSizePixels, CrosshairSizePixels);
            crosshairRect.anchoredPosition = Vector2.zero;

            var crosshairIcon = crosshairGo.GetComponent<Image>();
            crosshairIcon.sprite = IconAssets.ItemPingDiamond;
            crosshairIcon.raycastTarget = false;
            crosshairIcon.preserveAspect = true;

            return new ItemPingWidget(root, canvasGroup, arrowRect, arrowImage, crosshairRect, crosshairIcon, labelGroupRect, nameText, distanceText);
        }

        private void Bind(Func<Vector3> getWorldPosition, Color color, bool enableArrow)
        {
            _color = color;
            _nameText.color = color;
            _distanceText.color = color;
            _crosshairImage.color = color;
            _arrowImage.color = color;
            _arrow.gameObject.SetActive(enableArrow);

            _labelGroup.anchoredPosition = Vector2.zero;
            CanvasGroup.alpha = 1f;
            _root.gameObject.SetActive(true);

            // Root is a tiny 20x20 anchor point; the real footprint spans
            // the name label above it down through the distance sub-line
            // below. Refined every Refresh() call to the actual rendered
            // text width instead of a generous static guess - an
            // over-wide box here made overlap resolution trigger (and push
            // labels away) far more than actually needed. LabelWidget =
            // labelGroup (not root) so overlap resolution only ever nudges
            // the name/distance text, never the arrow or the on-screen
            // crosshair - both need to stay exactly on the tracked position.
            Anchor = new IndicatorAnchor(getWorldPosition, _root, enableArrow ? _arrow : null, _crosshair)
            {
                OverlapSize = new Vector2(120f, 60f),
                LabelWidget = _labelGroup,
                ReleaseWidget = Release,
            };
        }

        /// <summary>
        /// Handed back to the pool by <see cref="IndicatorManager.UnregisterAnchor"/>
        /// (via <see cref="IndicatorAnchor.ReleaseWidget"/>) once this widget's
        /// highlight has finished fading out.
        /// </summary>
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

        /// <summary>Drives one throwaway layout/mesh build per text so a prewarmed widget's first real Refresh isn't also TMP's first.</summary>
        private void WarmText()
        {
            _nameText.text = "WARMUP";
            _distanceText.text = "0m";
            _nameText.ForceMeshUpdate();
            _distanceText.ForceMeshUpdate();
            _nameText.text = string.Empty;
            _distanceText.text = string.Empty;
            _measuredName = null;
            _measuredDistance = null;
            _lastDistanceMeters = int.MinValue;
        }

        public void Refresh(string displayName, float distanceMeters, bool showName, bool showDistance, Sprite nativeIcon = null)
        {
            // The mod's own diamond is authored white-fill/black-outline so it
            // can be tinted to the pinging player's color (Common.IconAssets);
            // the game's own item icon is finished, colored art, so it's shown
            // untinted and a size up instead - the same reasoning the compass
            // marker uses (Compass.CompassMarkerWidget.Refresh). Resolved every
            // frame rather than at bind time because the widget is pooled and
            // use-native-item-ping-icons is a live toggle - a widget rented next
            // by a luggage/creature (no icon of its own) has to be able to get
            // back to the diamond.
            Sprite crosshairSprite = nativeIcon != null ? nativeIcon : IconAssets.ItemPingDiamond;
            if (crosshairSprite != null && _crosshairImage.sprite != crosshairSprite)
            {
                _crosshairImage.sprite = crosshairSprite;
            }
            float crosshairSize = nativeIcon != null ? NativeIconSizePixels : CrosshairSizePixels;
            _crosshair.sizeDelta = new Vector2(crosshairSize, crosshairSize);
            _crosshairImage.color = nativeIcon != null ? Color.white : _color;

            // Off-screen, the same icon takes the place of the dart entirely
            // (rather than the dart pointing at an unseen item): the item is
            // what's worth recognizing at a glance, and its clamped edge
            // position already says which way it is - so the arrow is left
            // upright (RotateArrowWidget) instead of being spun, since an item
            // icon has no "point" to aim and a rotated one just looks
            // knocked over.
            Sprite arrowSprite = nativeIcon != null ? nativeIcon : IconAssets.PingArrow;
            if (arrowSprite != null && _arrowImage.sprite != arrowSprite)
            {
                _arrowImage.sprite = arrowSprite;
            }
            _arrow.sizeDelta = nativeIcon != null
                ? new Vector2(NativeIconSizePixels, NativeIconSizePixels)
                : OffScreenArrow.DartSize;
            _arrowImage.color = nativeIcon != null ? Color.white : _color;
            if (Anchor != null)
            {
                Anchor.RotateArrowWidget = nativeIcon == null;
            }

            if (NativeAssets.Font != null)
            {
                // Swapping the font changes how wide the same string renders,
                // so any cached measurement taken under the old one is void
                // (see MeasureHalfWidth) - this happens at most once per widget,
                // the first time the game's own font is found.
                if (_nameText.font != NativeAssets.Font)
                {
                    _nameText.font = NativeAssets.Font;
                    _measuredName = null;
                }
                if (_distanceText.font != NativeAssets.Font)
                {
                    _distanceText.font = NativeAssets.Font;
                    _measuredDistance = null;
                }
            }
            if (NativeAssets.OutlineMaterial != null)
            {
                if (_nameText.fontSharedMaterial != NativeAssets.OutlineMaterial) _nameText.fontSharedMaterial = NativeAssets.OutlineMaterial;
                if (_distanceText.fontSharedMaterial != NativeAssets.OutlineMaterial) _distanceText.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            // Live config values, so re-applied every frame rather than baked in
            // at creation. Both cached measurements below are keyed on their
            // string alone, so a size change voids them for the same reason a
            // font swap does - the same string renders wider at a bigger size.
            float offScreenBlend = Anchor != null ? Anchor.OffScreenBlend : 0f;
            float nameFontSize = HudFontScale.Name(NameFontSizeBase, offScreenBlend);
            if (!Mathf.Approximately(_nameText.fontSize, nameFontSize))
            {
                _nameText.fontSize = nameFontSize;
                _measuredName = null;
            }
            float distanceFontSize = HudFontScale.Distance(DistanceFontSizeBase, offScreenBlend);
            if (!Mathf.Approximately(_distanceText.fontSize, distanceFontSize))
            {
                _distanceText.fontSize = distanceFontSize;
                _measuredDistance = null;
            }

            _nameText.gameObject.SetActive(showName);
            if (showName && !string.Equals(_nameText.text, displayName, StringComparison.Ordinal))
            {
                _nameText.text = displayName;
            }

            _distanceText.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                // Only ever rebuilt when the whole-metre reading actually
                // changes: the string interpolation allocates, and assigning
                // TMP_Text.text schedules a mesh rebuild, both of which used to
                // happen every frame for every live item ping even while
                // standing still.
                int rounded = Mathf.RoundToInt(distanceMeters);
                if (rounded != _lastDistanceMeters)
                {
                    _lastDistanceMeters = rounded;
                    _distanceText.text = $"{rounded}m";
                }
            }

            // The widget is centered on its anchor point, but IndicatorManager
            // only clamps that anchor point itself to within EdgeMarginPixels
            // of the screen edge - it has no idea how wide the name label
            // actually renders. A fixed default margin (48px) is nowhere near
            // half the width of a long/grouped name (e.g. "2x COCONUT"), so
            // the label's own left/right half was clipping past the physical
            // screen edge even though its anchor point was safely on-screen.
            // Widen the margin to always cover half the widest currently-shown
            // label text - re-measured only when that text changes (see
            // _measuredName), not every frame.
            float widestHalf = 0f;
            if (showName)
            {
                widestHalf = Mathf.Max(widestHalf, MeasureHalfWidth(_nameText, ref _measuredName, ref _measuredNameHalfWidth));
            }
            if (showDistance)
            {
                widestHalf = Mathf.Max(widestHalf, MeasureHalfWidth(_distanceText, ref _measuredDistance, ref _measuredDistanceHalfWidth));
            }
            Anchor.EdgeMarginPixels = Mathf.Max(48f, widestHalf + 12f);

            // Same widest-visible-text measurement, reused for the overlap
            // box (Indicators.LabelOverlapResolver) instead of a separate
            // fixed guess - keeps overlap detection matched to what's
            // actually on screen (e.g. a short "KING" vs. a wider
            // "2x COCONUT").
            // Same widest-visible-text measurement drives the box's height and
            // centre too: the name rides above the tracked point (anchored +24,
            // 28 tall -> tops out at +38) and the distance line below it
            // (anchored -18, 24 tall -> bottoms out at -30), so the box is not
            // centred on the crosshair/arrow, which stay exactly on the tracked
            // position regardless.
            if (!showName && !showDistance)
            {
                Anchor.OverlapSize = Vector2.zero;
                Anchor.OverlapCenterOffset = Vector2.zero;
                return;
            }

            float top = showName ? 38f : -6f;
            float bottom = showDistance ? -30f : 10f;
            Anchor.OverlapSize = new Vector2(widestHalf * 2f + 12f, top - bottom);
            Anchor.OverlapCenterOffset = new Vector2(0f, (top + bottom) * 0.5f);
        }

        /// <summary>Half the rendered width of <paramref name="text"/>, re-measured only when its string has changed since the last call.</summary>
        private static float MeasureHalfWidth(TMP_Text text, ref string measuredText, ref float measuredHalfWidth)
        {
            if (!string.Equals(measuredText, text.text, StringComparison.Ordinal))
            {
                measuredText = text.text;
                measuredHalfWidth = text.GetPreferredValues().x * 0.5f;
            }
            return measuredHalfWidth;
        }
    }
}
