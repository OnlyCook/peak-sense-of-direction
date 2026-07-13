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
            nameText.fontSize = 16f;
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
            distanceText.fontSize = 14f;
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
            bool isDead, bool isUnconscious)
        {
            if (NativeAssets.Font != null)
            {
                if (_nameText.font != NativeAssets.Font) _nameText.font = NativeAssets.Font;
                if (_distanceText.font != NativeAssets.Font) _distanceText.font = NativeAssets.Font;
            }
            // Font asset/material assignment (native vs. tinted-instanced) is
            // handled below, once whether this marker's text is tinted is
            // known.

            var iconRect = (RectTransform)_iconImage.transform;
            iconRect.sizeDelta = new Vector2(iconSizePixels, iconSizePixels);

            // Campfire uses its own real HUD-icon colors - only the
            // procedurally-generated placeholder shapes get tinted per-anchor.
            _iconImage.color = _kind == CompassMarkerKind.Campfire ? Color.white : color;
            if (_kind == CompassMarkerKind.Campfire && NativeAssets.CampfireIconSprite != null && _iconImage.sprite != NativeAssets.CampfireIconSprite)
            {
                _iconImage.sprite = NativeAssets.CampfireIconSprite;
            }

            // Campfire has no owning-player color to darken (it's not tied to
            // any anchor's own color, same reasoning as the fill above), so
            // its outline stays pure black; every other kind's outline is a
            // darkened version of its own tint instead of a flat black line.
            Color outlineColor = _kind == CompassMarkerKind.Campfire ? Color.black : ColorUtil.Darken(color);
            for (int i = 0; i < _iconOutlines.Length; i++)
            {
                Image outline = _iconOutlines[i];
                var outlineRect = (RectTransform)outline.transform;
                outlineRect.sizeDelta = new Vector2(iconSizePixels, iconSizePixels);
                outlineRect.anchoredPosition = OutlineOffsets[i] * 1.5f;
                outline.color = outlineColor;
                if (_kind == CompassMarkerKind.Campfire && NativeAssets.CampfireIconSprite != null && outline.sprite != NativeAssets.CampfireIconSprite)
                {
                    outline.sprite = NativeAssets.CampfireIconSprite;
                }
            }

            // Player marker's face swaps between normal/unconscious/dead art
            // as the anchor's own state changes (icon + all outline copies
            // need to stay in sync, same as the campfire swap above).
            if (_kind == CompassMarkerKind.Player)
            {
                Sprite faceSprite = ResolvePlayerFaceSprite(isDead, isUnconscious);
                if (faceSprite != null && _iconImage.sprite != faceSprite)
                {
                    _iconImage.sprite = faceSprite;
                    foreach (Image outline in _iconOutlines)
                    {
                        outline.sprite = faceSprite;
                    }
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
