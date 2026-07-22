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

        /// <summary>
        /// 0 = name above / distance below straddling the crosshair at full
        /// spacing; 1 = the two lines pulled together, for a label that's been
        /// nudged off its crosshair (the gap the crosshair sat in is then empty).
        /// Driven by <see cref="IndicatorManager"/> via
        /// <see cref="IndicatorAnchor.SetLabelCompaction"/>. See <see cref="ApplyLabelLayout"/>.
        /// </summary>
        private float _labelCompaction;

        /// <summary>How much further down the distance line sits when a native icon (bigger than the diamond) is shown - folded into the spread layout only (a compacted label has left the icon behind). Cached from <see cref="Refresh"/> so <see cref="ApplyLabelLayout"/> can re-run on a compaction change without it.</summary>
        private float _distanceExtraDrop;

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

        /// <summary>Name/distance line offsets straddling the crosshair (compaction 0). The crosshair sits in the gap between them.</summary>
        private const float SpreadNameY = 24f;
        private const float SpreadDistanceY = -18f;

        /// <summary>Name/distance line offsets closed up (compaction 1), for a label nudged off its crosshair - just enough for the two lines (~20 and ~16 tall) not to touch, with no icon between them any more.</summary>
        private const float CompactNameY = 12f;
        private const float CompactDistanceY = -12f;

        /// <summary>Approximate half-height of the rendered distance line, for sizing how far below the icon it must sit to clear it.</summary>
        private const float DistanceHalfHeight = 8f;

        /// <summary>Gap left between the icon's bottom edge and the distance line's top edge in the spread layout, so the icon never obscures the distance digits.</summary>
        private const float IconDistanceClearance = 3f;

        /// <summary>The crosshair/icon sits exactly on the tracked point by construction, but that reads as hugging the name label above it with a comparatively large gap to the distance line below - nudging it down a few pixels (name/distance lines themselves untouched, both anchored under <see cref="_labelGroup"/> rather than <see cref="_root"/>) balances the two gaps visually.</summary>
        private const float CrosshairYOffset = -4f;

        /// <summary>Nudges the off-screen arrow/native-icon down a few pixels off the label group's exact centre point, to read as visually centered between the name and distance lines the same way <see cref="CrosshairYOffset"/> balances the on-screen crosshair.</summary>
        private const float ArrowYOffset = -3f;

        /// <summary>Fallback dart size shown off-screen when the item has no native icon, a touch larger than <see cref="OffScreenArrow.DartSize"/> so it reads clearly at a glance - only applied here (not the shared constant) so it doesn't also grow <see cref="Pings.PingWidget"/>'s arrow.</summary>
        private static readonly Vector2 ItemArrowSize = new Vector2(22f, 24f);

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
            crosshairRect.anchoredPosition = new Vector2(0f, CrosshairYOffset);

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
            _labelCompaction = 0f;
            _distanceExtraDrop = 0f;
            ApplyLabelLayout();
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
                SetLabelCompaction = SetCompaction,
                ReleaseWidget = Release,
            };
        }

        /// <summary>
        /// Driven 0..1 by <see cref="IndicatorManager"/> while this label is
        /// nudged off/onto its crosshair (see <see cref="IndicatorAnchor.SetLabelCompaction"/>).
        /// </summary>
        private void SetCompaction(float compaction)
        {
            compaction = Mathf.Clamp01(compaction);
            if (Mathf.Approximately(compaction, _labelCompaction))
            {
                return;
            }
            _labelCompaction = compaction;
            ApplyLabelLayout();
        }

        /// <summary>
        /// Positions the name above / distance below their shared label group,
        /// interpolated between the crosshair-straddling layout (compaction 0) and
        /// the closed-up one (compaction 1). Called every <see cref="Refresh"/> and
        /// whenever the compaction changes, so the visible text and the overlap box
        /// computed alongside it can't disagree about where the lines are.
        /// </summary>
        private void ApplyLabelLayout()
        {
            // The native-icon drop only applies to the spread layout - a compacted
            // label has slid off the icon, so there's nothing below it to clear.
            float spreadDistY = SpreadDistanceY - _distanceExtraDrop;
            float nameY = Mathf.Lerp(SpreadNameY, CompactNameY, _labelCompaction);
            float distY = Mathf.Lerp(spreadDistY, CompactDistanceY, _labelCompaction);
            _nameText.rectTransform.anchoredPosition = new Vector2(0f, nameY);
            _distanceText.rectTransform.anchoredPosition = new Vector2(0f, distY);
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

        /// <summary>
        /// Every letter (both cases), digit and punctuation mark that shows up
        /// in an actual item/luggage/creature/hazard display name - unlike
        /// "WARMUP", which was only ever exercising 6 distinct letters. TMP's
        /// dynamic font atlas only rasterizes a glyph the first time it's
        /// actually rendered, and doing that mid-frame (a texture repack +
        /// GPU reupload) is exactly the kind of hitch this class exists to
        /// front-load - a real item's name (whichever glyphs it happens to
        /// need) was otherwise still paying that cost on the first ping that
        /// ever highlighted one, prewarmed widget pool or not.
        /// </summary>
        private const string FullGlyphSample = "ABCDEFGHIJKLMNOPQRSTUVWXYZ abcdefghijklmnopqrstuvwxyz 0123456789 x()-'";

        /// <summary>Drives one throwaway layout/mesh build per text so a prewarmed widget's first real Refresh isn't also TMP's first.</summary>
        private void WarmText()
        {
            _nameText.text = FullGlyphSample;
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
            // Live config value (PluginConfig.IndicatorIconSizeMultiplier), so
            // re-applied every frame rather than baked in - everything below
            // that derives from crosshairSize (the distance line's drop, the
            // overlap box) already reads it live too, so scaling it here is
            // enough to cascade through the rest of this widget's layout.
            float iconSizeMultiplier = Plugin.Instance.Cfg.IndicatorIconSizeMultiplier.Value;
            float crosshairSize = (nativeIcon != null ? NativeIconSizePixels : CrosshairSizePixels) * iconSizeMultiplier;
            _crosshair.sizeDelta = new Vector2(crosshairSize, crosshairSize);
            _crosshairImage.color = nativeIcon != null ? Color.white : _color;

            // The crosshair/icon stays pinned to the tracked point (y=0) while the
            // spread distance line sits just below it, so a big enough icon reaches
            // straight over that line and hides it entirely - and unlike the name
            // (a long word an icon can only clip a letter or two of, which the eye
            // still fills in), a couple of obscured digits leave the distance
            // unreadable. So drop the distance line just far enough that the icon's
            // bottom clears it, sized from the icon actually shown (bigger native
            // icons drop it further). The name is deliberately left where it is: if
            // anything has to sit under the icon it's the name's bottom edge, which
            // reads through far better. Only affects the spread layout - a compacted
            // label has slid off the icon, so it needs no drop at all.
            float iconHalf = crosshairSize * 0.5f;
            float distanceClearY = -(iconHalf + DistanceHalfHeight + IconDistanceClearance);
            float distanceExtraDrop = Mathf.Max(0f, SpreadDistanceY - distanceClearY);

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
            _arrow.sizeDelta = (nativeIcon != null
                ? new Vector2(NativeIconSizePixels, NativeIconSizePixels)
                : ItemArrowSize) * iconSizeMultiplier;
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

            // Name rides above the tracked point, distance below it, with the
            // icon (on-screen crosshair / off-screen arrow) in the gap between.
            // A native icon reaches further down than the diamond, so its bottom
            // would poke into the distance line - drop that line by the size
            // difference to keep the same clearance. ApplyLabelLayout folds this
            // into the spread layout and blends toward the compacted one per the
            // current _labelCompaction (driven by IndicatorManager).
            _distanceExtraDrop = distanceExtraDrop;
            ApplyLabelLayout();
            float spreadDistY = SpreadDistanceY - distanceExtraDrop;

            // Off-screen, the arrow shows the item's icon in place of the
            // on-screen crosshair - and, unlike the crosshair, it has no visible
            // object under it to stay pinned to. The overlap resolver nudges only
            // the label group to destack neighbours; left to itself the arrow
            // would stay parked at the clamped edge while its label slid away,
            // opening the empty gap the icon used to fill and orphaning that icon
            // where a neighbour's label then lands on it (icons obstructing
            // labels in-game). Ride the arrow along with the label group instead,
            // so off-screen the icon+name+distance stay one solid unit: the icon
            // is always centred between the two lines, and the resolver keeps the
            // whole unit - icon included - clear of its neighbours. On-screen the
            // arrow is hidden (the pinned crosshair shows instead), so this is a
            // no-op there.
            _arrow.anchoredPosition = _labelGroup.anchoredPosition + new Vector2(0f, ArrowYOffset);

            // Off-screen, several labels can pile onto the same clamped edge
            // point. This cap is how far one may travel along that edge before the
            // resolver decides it belongs on the inset overflow line instead - set
            // to roughly one column-width, the distance it would move to reach that
            // second line, so it hops over exactly when staying put would carry it
            // further from where it points than the hop costs. That keeps the
            // primary line the fuller one for a genuinely tight cluster (4-2, not
            // 2-4) while a merely crowded stack spills to the second line sooner
            // than smearing itself across a third of the edge. On-screen, where
            // crowding is mild and the label sits on a visible crosshair, the
            // tighter default is kept so it never drifts far. Scaled by the blend.
            if (Anchor != null)
            {
                Anchor.MaxOverlapOffset = Mathf.Lerp(LabelOverlapResolver.MaxOffsetMagnitude, 130f, offScreenBlend);

                // No longer force a downward-only nudge on-screen: the label now
                // compacts (name/distance close up) as it's pushed clear of the
                // crosshair, so an upward nudge no longer risks the distance line
                // landing on a native icon - the whole label has left the icon
                // behind either way. Keeping it off lets an on-screen stack split
                // symmetrically (both labels share the move) instead of piling the
                // entire offset onto the lower one.
                Anchor.OverlapOffsetDownwardOnly = false;
            }

            // Widest-visible-text measurement drives the overlap box's width, and
            // the name/distance line positions drive its height and centre: the
            // name rides above the tracked point (28 tall) and the distance line
            // below it (24 tall), so the box is not centred on the crosshair/arrow.
            if (!showName && !showDistance)
            {
                Anchor.OverlapSize = Vector2.zero;
                Anchor.OverlapPlacementSize = Vector2.zero;
                Anchor.OverlapCenterOffset = Vector2.zero;
                return;
            }

            // Detection box: top/bottom hug the actual rendered lines at their
            // spread positions (name ~20px tall centred at SpreadNameY, distance
            // ~16px tall centred at spreadDistY) rather than padding out to the
            // whole widget. Width is the real half-width doubled, plus a little
            // air. This is the footprint used to decide whether two labels collide.
            float width = widestHalf * 2f + 12f;
            float top = showName ? SpreadNameY + 10f : -6f;
            float bottom = showDistance ? spreadDistY - 8f : 10f;
            Anchor.OverlapSize = new Vector2(width, top - bottom);
            Anchor.OverlapCenterOffset = new Vector2(0f, (top + bottom) * 0.5f);

            // Placement box: the same footprint measured against the compacted
            // layout (name/distance closed up over the crosshair gap). The
            // on-screen resolver spaces stacked labels by this tighter box - which
            // is where they end up once nudged clear and compacted - so they pack
            // as close as the text really needs, while still being detected on the
            // taller spread box above (so they stay reliably clustered rather than
            // oscillating in and out of overlap). Only when both lines show; a
            // single line has no gap to close, so it falls back to the spread box.
            if (showName && showDistance)
            {
                float compTop = CompactNameY + 10f;
                float compBottom = CompactDistanceY - 8f;
                Anchor.OverlapPlacementSize = new Vector2(width, compTop - compBottom);
            }
            else
            {
                Anchor.OverlapPlacementSize = Vector2.zero;
            }
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
