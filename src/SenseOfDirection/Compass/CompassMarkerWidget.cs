using System.Collections.Generic;
using SenseOfDirection.Common;
using SenseOfDirection.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Compass
{
    /// <summary>Which way (if any) an elevation arrow should point for a marker.</summary>
    public enum CompassElevation
    {
        None,
        Above,
        Below,
    }

    /// <summary>
    /// One marker on the compass tape: an icon (shape/sprite depends on the
    /// owning anchor's <see cref="CompassMarkerKind"/>, background-tinted to
    /// that anchor's own color; player markers swap to a dead/unconscious
    /// face sprite instead of layering a separate status badge - see
    /// <see cref="ResolvePlayerFaceSprite"/> - so the compass never doubles
    /// up dead/unconscious indicators the way <see cref="PlayerLabel"/>'s
    /// on-screen badge does), an optional elevation arrow, and optional
    /// name/distance sub-text. <see cref="CompassManager"/> owns creation/
    /// positioning/per-frame refresh - this class is just the widget
    /// hierarchy.
    /// </summary>
    public class CompassMarkerWidget
    {
        /// <summary>
        /// Same 8-direction outline trick <see cref="CampfireIndicator.CampfireWidget"/>
        /// already uses (same maintainer's own convention, reused directly
        /// rather than reinvented): plain black copies of the icon's own
        /// sprite, offset a pixel or two in every direction and drawn behind
        /// it, so the silhouette gets a stroke that follows its actual shape.
        /// Applied generically here (any <see cref="CompassMarkerKind"/> gets
        /// one) rather than per-kind, so a future new marker kind gets an
        /// outline automatically with no extra work.
        /// </summary>
        private static readonly Vector2[] OutlineOffsets =
        {
            new Vector2(-1f, -1f), new Vector2(0f, -1f), new Vector2(1f, -1f),
            new Vector2(-1f, 0f),                         new Vector2(1f, 0f),
            new Vector2(-1f, 1f),  new Vector2(0f, 1f),  new Vector2(1f, 1f),
        };

        public readonly RectTransform Root;
        public readonly CanvasGroup CanvasGroup;

        /// <summary>
        /// Home position (0,0) child of <see cref="Root"/> holding just the
        /// name/distance text - see <see cref="Compass.CompassManager"/>'s
        /// own overlap-resolution pass, which nudges this horizontally
        /// (the whole label shifts sideways as one object) so the marker's
        /// icon never leaves its real bearing.
        /// </summary>
        public readonly RectTransform LabelGroup;

        /// <summary>TMP shader property name for the outline color, shared by the elevation arrow and (once tinted) the name/distance text materials.</summary>
        private const string OutlineColorProperty = "_OutlineColor";

        /// <summary>Sizes the marker's name/distance lines are tuned at; the `Fonts` section scales these rather than replacing them (see <see cref="Common.HudFontScale"/>).</summary>
        private const float NameFontSizeBase = 16f;
        private const float DistanceFontSizeBase = 14f;

        private readonly CompassMarkerKind _kind;
        private readonly Image _iconImage;
        private readonly Image[] _iconOutlines;
        private readonly TMP_Text _elevationArrow;
        private readonly TMP_Text _nameText;
        private readonly TMP_Text _distanceText;

        /// <summary>Last icon size passed to <see cref="Refresh"/> - a live config value, needed by <see cref="ApplyLabelPositions"/> whenever the compaction changes between refreshes.</summary>
        private float _iconSizePixels = 26f;

        /// <summary>0 = name/distance straddle the icon at full spacing; 1 = pulled together, for a label staggered onto a row of its own. See <see cref="SetLabelCompaction"/>.</summary>
        private float _labelCompaction;

        /// <summary>Last whole-metre distance the label was built for - see <see cref="Refresh"/>.</summary>
        private int _lastDistanceMeters = int.MinValue;

        /// <summary>
        /// Lazily-instanced material for tinted (Ping/ItemPing) name/distance
        /// text so its outline can be darkened per-anchor-color without
        /// touching <see cref="NativeAssets.OutlineMaterial"/>, which every
        /// other (untinted) label on the compass/HUD shares.
        /// </summary>
        private Material _tintedTextMaterial;

        private CompassMarkerWidget(
            CompassMarkerKind kind, RectTransform root, CanvasGroup canvasGroup,
            Image iconImage, Image[] iconOutlines, TMP_Text elevationArrow,
            RectTransform labelGroup, TMP_Text nameText, TMP_Text distanceText)
        {
            _kind = kind;
            Root = root;
            CanvasGroup = canvasGroup;
            _iconImage = iconImage;
            _iconOutlines = iconOutlines;
            _elevationArrow = elevationArrow;
            LabelGroup = labelGroup;
            _nameText = nameText;
            _distanceText = distanceText;
        }

        private static Sprite ResolveIconSprite(CompassMarkerKind kind) => kind switch
        {
            CompassMarkerKind.Player => IconAssets.PlayerFace,
            CompassMarkerKind.Ping => IconAssets.PingRing,
            CompassMarkerKind.ItemPing => IconAssets.ItemPingDiamond,
            CompassMarkerKind.Campfire => NativeAssets.CampfireIconSprite,
            _ => null,
        };

        /// <summary>Player marker only: which face variant to show for the given dead/unconscious state.</summary>
        private static Sprite ResolvePlayerFaceSprite(bool isDead, bool isUnconscious) => isDead
            ? IconAssets.PlayerDeadFace
            : isUnconscious
                ? IconAssets.PlayerUnconsciousFace
                : IconAssets.PlayerFace;

        /// <summary>
        /// Pooled per kind (the icon sprite/outline set is baked in at build
        /// time, so a Ping marker can't be reused as a Player one). A marker is
        /// thirteen GameObjects - eight outline Images, the icon, three TMP
        /// texts - and pings create and destroy one per ping, per pinged item
        /// group: rebuilding that hierarchy from scratch each time is exactly
        /// the churn ISSUES.md' "never stutter when pinging" entry is about.
        /// </summary>
        private static readonly Dictionary<CompassMarkerKind, Stack<CompassMarkerWidget>> Pool =
            new Dictionary<CompassMarkerKind, Stack<CompassMarkerWidget>>();

        /// <summary>Takes a marker of this kind from the pool, building one only if it's empty.</summary>
        public static CompassMarkerWidget Rent(RectTransform parent, CompassMarkerKind kind)
        {
            if (Pool.TryGetValue(kind, out Stack<CompassMarkerWidget> pooled) && pooled.Count > 0)
            {
                CompassMarkerWidget widget = pooled.Pop();
                if (widget.Root != null)
                {
                    widget.Root.SetParent(parent, false);
                    widget.Root.anchoredPosition = Vector2.zero;
                    widget.LabelGroup.anchoredPosition = Vector2.zero;
                    widget._labelCompaction = 0f;
                    widget._lastDistanceMeters = int.MinValue;
                    widget.Root.gameObject.SetActive(true);
                    return widget;
                }
            }
            return Create(parent, kind);
        }

        /// <summary>Parks a no-longer-shown marker back in its kind's pool instead of destroying it.</summary>
        public static void Release(CompassMarkerWidget widget)
        {
            if (widget == null || widget.Root == null)
            {
                return;
            }

            widget.Root.gameObject.SetActive(false);
            widget.CanvasGroup.alpha = 0f;

            if (!Pool.TryGetValue(widget._kind, out Stack<CompassMarkerWidget> pooled))
            {
                pooled = new Stack<CompassMarkerWidget>();
                Pool[widget._kind] = pooled;
            }
            pooled.Push(widget);
        }

        /// <summary>Builds markers of this kind up front and parks them in the pool - see <see cref="Common.PingPrewarm"/>.</summary>
        public static void Prewarm(RectTransform parent, CompassMarkerKind kind, int count)
        {
            if (!Pool.TryGetValue(kind, out Stack<CompassMarkerWidget> pooled))
            {
                pooled = new Stack<CompassMarkerWidget>();
                Pool[kind] = pooled;
            }

            while (pooled.Count < count)
            {
                CompassMarkerWidget widget = Create(parent, kind);
                widget._nameText.text = "WARMUP";
                widget._distanceText.text = "0m";
                widget._nameText.ForceMeshUpdate();
                widget._distanceText.ForceMeshUpdate();
                widget._nameText.text = string.Empty;
                widget._distanceText.text = string.Empty;
                widget.Root.gameObject.SetActive(false);
                pooled.Push(widget);
            }
        }

        public static CompassMarkerWidget Create(RectTransform parent, CompassMarkerKind kind)
        {
            var rootGo = new GameObject($"SoD.CompassMarker.{kind}", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(parent, false);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.anchorMin = new Vector2(0.5f, 0.5f);
            root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = Vector2.zero; // see CompassManager's own Root for why this matters

            var canvasGroup = rootGo.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Outline copies first (rendered behind, per UI sibling order),
            // real icon added after (rendered on top).
            var iconOutlines = new Image[OutlineOffsets.Length];
            for (int i = 0; i < OutlineOffsets.Length; i++)
            {
                var outlineGo = new GameObject($"IconOutline{i}", typeof(RectTransform), typeof(Image));
                var outlineRect = (RectTransform)outlineGo.transform;
                outlineRect.SetParent(root, false);
                outlineRect.anchorMin = new Vector2(0.5f, 0.5f);
                outlineRect.anchorMax = new Vector2(0.5f, 0.5f);
                outlineRect.pivot = new Vector2(0.5f, 0.5f);
                var outlineImage = outlineGo.GetComponent<Image>();
                outlineImage.preserveAspect = true;
                outlineImage.color = Color.black;
                outlineImage.sprite = ResolveIconSprite(kind);
                iconOutlines[i] = outlineImage;
            }

            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            var iconRect = (RectTransform)iconGo.transform;
            iconRect.SetParent(root, false);
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            var iconImage = iconGo.GetComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.sprite = ResolveIconSprite(kind);

            // Plain Unicode arrow glyphs rather than a drawn triangle - deliberately
            // left on TMP's own default font asset (not NativeAssets.Font, the
            // game's stylized display font) since a general-purpose font is far
            // more likely to actually have U+2191/U+2193 glyphs baked into its atlas.
            var arrowGo = new GameObject("ElevationArrow", typeof(RectTransform), typeof(TextMeshProUGUI));
            var arrowRect = (RectTransform)arrowGo.transform;
            arrowRect.SetParent(root, false);
            arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
            arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            arrowRect.sizeDelta = new Vector2(18f, 18f);
            var elevationArrow = arrowGo.GetComponent<TextMeshProUGUI>();
            elevationArrow.alignment = TextAlignmentOptions.Center;
            elevationArrow.fontSize = 18f;
            elevationArrow.fontStyle = FontStyles.Bold;
            elevationArrow.color = new Color(1f, 1f, 1f, 0.95f);
            // Accessing .fontMaterial (not .fontSharedMaterial) makes TMP
            // instance a per-object material automatically, so this doesn't
            // touch the shared default asset every other TMP element on
            // this default font uses. Outline color is re-applied (darkened
            // per anchor color) every Refresh() call below.
            elevationArrow.fontMaterial.SetFloat("_OutlineWidth", 0.3f);
            elevationArrow.gameObject.SetActive(false);

            // Home position (0,0) relative to root - CompassManager's own
            // overlap resolution nudges this transform, not root, so a
            // marker's icon (plus outline/badge/elevation arrow, all direct
            // children of root above) never moves off its real bearing;
            // only the informational name/distance text is free to shift
            // slightly to avoid overlapping a neighboring marker's own text.
            var labelGroupGo = new GameObject("LabelGroup", typeof(RectTransform));
            var labelGroupRect = (RectTransform)labelGroupGo.transform;
            labelGroupRect.SetParent(root, false);
            labelGroupRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelGroupRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelGroupRect.pivot = new Vector2(0.5f, 0.5f);
            labelGroupRect.sizeDelta = Vector2.zero;

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.SetParent(labelGroupRect, false);
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            nameRect.sizeDelta = new Vector2(160f, 22f);
            var nameText = nameGo.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Overflow;
            nameText.fontSize = NameFontSizeBase;
            nameText.color = Color.white;
            nameGo.SetActive(false);

            var distGo = new GameObject("Distance", typeof(RectTransform), typeof(TextMeshProUGUI));
            var distRect = (RectTransform)distGo.transform;
            distRect.SetParent(labelGroupRect, false);
            distRect.anchorMin = new Vector2(0.5f, 0.5f);
            distRect.anchorMax = new Vector2(0.5f, 0.5f);
            distRect.pivot = new Vector2(0.5f, 0.5f);
            distRect.sizeDelta = new Vector2(120f, 20f);
            var distanceText = distGo.GetComponent<TextMeshProUGUI>();
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.enableWordWrapping = false;
            distanceText.fontSize = DistanceFontSizeBase;
            distanceText.color = new Color(1f, 1f, 1f, 0.9f);

            return new CompassMarkerWidget(kind, root, canvasGroup, iconImage, iconOutlines, elevationArrow, labelGroupRect, nameText, distanceText);
        }

        /// <summary>
        /// How tightly this marker's name/distance lines sit together, smoothed
        /// by <see cref="CompassManager"/> between 0 and 1.
        ///
        /// At 0 they straddle the icon, leaving an icon-sized gap between them.
        /// That gap is exactly right on the tape's own row - the icon really is
        /// sitting in it - but a label staggered down onto row 2 or 3 leaves its
        /// icon behind on the tape, so the gap becomes a hole with nothing in it,
        /// and a reader can't tell at a glance which distance line belongs to
        /// which name. At 1 the two lines close up into a single block.
        ///
        /// Only the text moves: the icon, its outline, the status badge and the
        /// elevation arrow are all children of Root, not LabelGroup, and stay on
        /// the marker's real bearing. (An icon can never be staggered onto
        /// another row for the same reason.)
        /// </summary>
        /// <summary>
        /// The marker's real horizontal footprint on the tape, for overlap
        /// resolution: the widest of the icon and whichever of the name/distance
        /// lines is actually shown, measured from the live rendered text
        /// (<see cref="TMP_Text.preferredWidth"/>) rather than a fixed reservation.
        /// A short label ("84m", "SAM") then claims only the room it needs, so it
        /// stops falsely colliding with a neighbour and getting dragged into a
        /// multi-row stagger it doesn't need. Both lines are centred on the icon,
        /// so the box is symmetric around the marker's bearing and its width is
        /// just the widest single element. Call after <see cref="Refresh"/> has
        /// set this frame's text/font size.
        /// </summary>
        public float MeasureOverlapWidth(float iconSizePixels)
        {
            float width = iconSizePixels;
            if (_nameText.gameObject.activeSelf)
            {
                // GetPreferredValues(text), not the preferredWidth property: the
                // property returns a cached value only refreshed on a layout
                // rebuild, so when a grouped ping's name grows a frame earlier
                // ("QUEEN" -> "2x QUEEN") it kept reporting the old, narrower
                // width - the box stayed sized for "QUEEN" and the extra "2x "
                // (and the tail of the name) bled into the neighbour. Forcing a
                // fresh measurement of the exact current string fixes it.
                width = Mathf.Max(width, _nameText.GetPreferredValues(_nameText.text).x + ReadabilityMargin(_nameText.fontSize));
            }
            if (_distanceText.gameObject.activeSelf)
            {
                width = Mathf.Max(width, _distanceText.GetPreferredValues(_distanceText.text).x + ReadabilityMargin(_distanceText.fontSize));
            }
            return width;
        }

        /// <summary>
        /// Extra width folded into a text line's overlap box beyond its raw
        /// <see cref="TMP_Text.preferredWidth"/>. <c>preferredWidth</c> measures
        /// the glyph geometry only, but this font renders a thick SDF outline
        /// that spills a few pixels past it on every side, so two boxes resolved
        /// to "just touching" still have their outlines overlap - two names read
        /// as one run ("KNIGHTPAWN"), a longer one bleeds into its neighbour.
        /// Reserving roughly a font-size's worth here (so it scales with the
        /// compass font-size config, outline and all) keeps a real, readable gap
        /// between adjacent labels and makes the resolver treat near-touching
        /// ones as the overlap they visually are. Half of it lands on each side
        /// of the centred label, so the on-screen gap between two touching boxes
        /// is one whole margin (plus the resolver's own padding).
        /// </summary>
        private static float ReadabilityMargin(float fontSize) => fontSize;

        public void SetLabelCompaction(float compaction)
        {
            compaction = Mathf.Clamp01(compaction);
            if (Mathf.Approximately(compaction, _labelCompaction))
            {
                return;
            }

            _labelCompaction = compaction;
            ApplyLabelPositions();
        }

        /// <summary>
        /// Places the name above / distance below, interpolated between the
        /// icon-straddling layout (compaction 0) and the closed-up one
        /// (compaction 1). Called on every <see cref="Refresh"/> and whenever the
        /// compaction changes, so the two can't disagree about where the text is.
        /// </summary>
        private void ApplyLabelPositions()
        {
            // Spread: clear of the icon (which the elevation arrow sits beside,
            // not above, so the name can sit directly over it).
            float half = _iconSizePixels * 0.62f;
            float spreadNameY = half + 12f;
            float spreadDistY = -half - 4f;

            // Closed up: just enough for the two text lines (22 and 20 tall) not
            // to touch, independent of icon size - there's no icon between them
            // any more.
            const float CompactNameY = 11f;
            const float CompactDistY = -11f;

            float nameY = Mathf.Lerp(spreadNameY, CompactNameY, _labelCompaction);
            float distY = Mathf.Lerp(spreadDistY, CompactDistY, _labelCompaction);

            ((RectTransform)_nameText.transform).anchoredPosition = new Vector2(0f, nameY);
            ((RectTransform)_distanceText.transform).anchoredPosition = new Vector2(0f, distY);
        }

        public void Refresh(
            float iconSizePixels, Color color, string name, float distanceMeters,
            bool showName, bool showDistance, CompassElevation elevation,
            bool isDead, bool isUnconscious, Sprite nativeIcon = null)
        {
            if (NativeAssets.Font != null)
            {
                if (_nameText.font != NativeAssets.Font) _nameText.font = NativeAssets.Font;
                if (_distanceText.font != NativeAssets.Font) _distanceText.font = NativeAssets.Font;
            }
            // Font asset/material assignment (native vs. tinted-instanced) is
            // handled below, once whether this marker's text is tinted is
            // known.

            // Which art this marker shows right now. Three of the four kinds can
            // change sprite while the marker is alive, so this is resolved every
            // frame rather than only at build time: a native icon handed in by
            // the anchor (an item ping showing the pinged item's own inventory
            // icon, use-native-item-ping-icons) wins over everything; a player's
            // face swaps with its own dead/unconscious state; the campfire's HUD
            // icon may only have been discovered by NativeAssets after the marker
            // was built. And because markers are pooled per kind, an item-ping
            // marker rented next by a plain (icon-less) target has to be able to
            // get back to its own kind's icon too.
            Sprite iconSprite = nativeIcon;
            if (iconSprite == null)
            {
                iconSprite = _kind == CompassMarkerKind.Player
                    ? ResolvePlayerFaceSprite(isDead, isUnconscious)
                    : ResolveIconSprite(_kind);
            }

            // The mod's own icons are authored white-fill/black-outline
            // specifically so Image.color can tint them per-anchor (see
            // Common.IconAssets) - the game's own sprites are finished, colored
            // art instead, so tinting them (or shadowing them in a darkened
            // player color) would just muddy them. They get the plain
            // white/black treatment the campfire's HUD icon already had.
            bool nativeArt = nativeIcon != null || _kind == CompassMarkerKind.Campfire;

            var iconRect = (RectTransform)_iconImage.transform;
            iconRect.sizeDelta = new Vector2(iconSizePixels, iconSizePixels);
            _iconImage.color = nativeArt ? Color.white : color;
            if (iconSprite != null && _iconImage.sprite != iconSprite)
            {
                _iconImage.sprite = iconSprite;
            }

            Color outlineColor = nativeArt ? Color.black : ColorUtil.Darken(color);
            for (int i = 0; i < _iconOutlines.Length; i++)
            {
                Image outline = _iconOutlines[i];
                var outlineRect = (RectTransform)outline.transform;
                outlineRect.sizeDelta = new Vector2(iconSizePixels, iconSizePixels);
                outlineRect.anchoredPosition = OutlineOffsets[i] * 1.5f;
                outline.color = outlineColor;
                if (iconSprite != null && outline.sprite != iconSprite)
                {
                    outline.sprite = iconSprite;
                }
            }

            // Ping/item-ping labels are tinted to the pinging player's own
            // color (matching the icon and the off-screen widget's own
            // labels - Pings.PingWidget/ItemPings.ItemPingWidget), same as
            // the ripple; player/campfire markers keep the fixed white/native
            // look everywhere else on the compass already uses.
            bool tintText = _kind == CompassMarkerKind.Ping || _kind == CompassMarkerKind.ItemPing;
            _nameText.color = tintText ? color : Color.white;
            _distanceText.color = tintText ? color : new Color(1f, 1f, 1f, 0.9f);
            if (tintText)
            {
                // Instanced (not shared) material so the outline can be
                // darkened per-anchor-color without touching
                // NativeAssets.OutlineMaterial, which every untinted label
                // on the compass/HUD still shares.
                if (_tintedTextMaterial == null && NativeAssets.OutlineMaterial != null)
                {
                    _tintedTextMaterial = new Material(NativeAssets.OutlineMaterial);
                }
                if (_tintedTextMaterial != null)
                {
                    _tintedTextMaterial.SetColor(OutlineColorProperty, ColorUtil.Darken(color));
                    if (_nameText.fontSharedMaterial != _tintedTextMaterial) _nameText.fontSharedMaterial = _tintedTextMaterial;
                    if (_distanceText.fontSharedMaterial != _tintedTextMaterial) _distanceText.fontSharedMaterial = _tintedTextMaterial;
                }
            }
            else if (NativeAssets.OutlineMaterial != null)
            {
                if (_nameText.fontSharedMaterial != NativeAssets.OutlineMaterial) _nameText.fontSharedMaterial = NativeAssets.OutlineMaterial;
                if (_distanceText.fontSharedMaterial != NativeAssets.OutlineMaterial) _distanceText.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            // Live config values, so re-applied every frame rather than baked in
            // at creation. The compass has no on/off-screen state of its own - a
            // marker is either on the tape or not drawn - so these are flat
            // scales, not a blend like the edge-of-screen widgets use.
            _nameText.fontSize = Common.HudFontScale.CompassName(NameFontSizeBase);
            _distanceText.fontSize = Common.HudFontScale.CompassDistance(DistanceFontSizeBase);

            _nameText.gameObject.SetActive(showName && !string.IsNullOrEmpty(name));
            if (showName && !string.IsNullOrEmpty(name))
            {
                _nameText.text = name;
            }

            _distanceText.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                // Rebuilt only when the whole-metre reading changes - this runs
                // every frame for every marker on the tape, and both the
                // interpolation and the TMP text assignment (which schedules a
                // mesh rebuild) are needless when the number hasn't moved.
                int rounded = Mathf.RoundToInt(distanceMeters);
                if (rounded != _lastDistanceMeters)
                {
                    _lastDistanceMeters = rounded;
                    _distanceText.text = $"{rounded}m";
                }
            }

            // Icon size is only known here (it's a live config value), so the
            // label layout is (re)applied from it every refresh - including
            // whichever compaction CompassManager last handed us.
            _iconSizePixels = iconSizePixels;
            ApplyLabelPositions();

            bool showArrow = elevation != CompassElevation.None;
            _elevationArrow.gameObject.SetActive(showArrow);
            if (showArrow)
            {
                // Scales with the icon size setting (slightly larger than the
                // icon itself, base size nudged up a bit so the outline below
                // actually reads at typical icon sizes instead of vanishing).
                float arrowSize = iconSizePixels * 0.85f;
                var arrowRect = (RectTransform)_elevationArrow.transform;
                arrowRect.sizeDelta = new Vector2(arrowSize, arrowSize);
                arrowRect.anchoredPosition = new Vector2(iconSizePixels * 0.5f + 3f, 0f);
                _elevationArrow.fontSize = arrowSize;
                _elevationArrow.text = elevation == CompassElevation.Above ? "↑" : "↓";
                _elevationArrow.fontMaterial.SetColor(OutlineColorProperty, outlineColor);
            }
        }
    }
}
