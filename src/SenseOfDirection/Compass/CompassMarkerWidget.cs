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
    /// that anchor's own color), an optional dead/unconscious status badge
    /// (players only, same red/yellow convention <see cref="PlayerLabel"/>
    /// already uses), an optional elevation arrow, and optional name/distance
    /// sub-text. <see cref="CompassManager"/> owns creation/positioning/
    /// per-frame refresh - this class is just the widget hierarchy.
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

        private readonly CompassMarkerKind _kind;
        private readonly Image _iconImage;
        private readonly Image[] _iconOutlines;
        private readonly Image _faceOverlay;
        private readonly Image _statusBadge;
        private readonly TMP_Text _elevationArrow;
        private readonly TMP_Text _nameText;
        private readonly TMP_Text _distanceText;

        private CompassMarkerWidget(
            CompassMarkerKind kind, RectTransform root, CanvasGroup canvasGroup,
            Image iconImage, Image[] iconOutlines, Image faceOverlay, Image statusBadge, TMP_Text elevationArrow,
            TMP_Text nameText, TMP_Text distanceText)
        {
            _kind = kind;
            Root = root;
            CanvasGroup = canvasGroup;
            _iconImage = iconImage;
            _iconOutlines = iconOutlines;
            _faceOverlay = faceOverlay;
            _statusBadge = statusBadge;
            _elevationArrow = elevationArrow;
            _nameText = nameText;
            _distanceText = distanceText;
        }

        private static Sprite ResolveIconSprite(CompassMarkerKind kind) => kind switch
        {
            CompassMarkerKind.Player => CompassIcons.FilledCircle,
            CompassMarkerKind.Ping => CompassIcons.RingCircle,
            CompassMarkerKind.ItemPing => CompassIcons.Diamond,
            CompassMarkerKind.Campfire => NativeAssets.CampfireIconSprite,
            _ => null,
        };

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

            Image faceOverlay = null;
            if (kind == CompassMarkerKind.Player)
            {
                var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
                var faceRect = (RectTransform)faceGo.transform;
                faceRect.SetParent(iconRect, false);
                faceRect.anchorMin = Vector2.zero;
                faceRect.anchorMax = Vector2.one;
                faceRect.offsetMin = Vector2.zero;
                faceRect.offsetMax = Vector2.zero;
                faceOverlay = faceGo.GetComponent<Image>();
                faceOverlay.sprite = CompassIcons.SmileyFace;
                faceOverlay.preserveAspect = true;
                faceOverlay.color = Color.black;
            }

            Image statusBadge = null;
            if (kind == CompassMarkerKind.Player)
            {
                var badgeGo = new GameObject("StatusBadge", typeof(RectTransform), typeof(Image));
                var badgeRect = (RectTransform)badgeGo.transform;
                badgeRect.SetParent(root, false);
                badgeRect.sizeDelta = new Vector2(10f, 10f);
                badgeRect.anchorMin = new Vector2(1f, 1f);
                badgeRect.anchorMax = new Vector2(1f, 1f);
                badgeRect.anchoredPosition = new Vector2(-2f, -2f);
                statusBadge = badgeGo.GetComponent<Image>();
                statusBadge.gameObject.SetActive(false);
            }

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
            // this default font uses.
            elevationArrow.fontMaterial.SetColor("_OutlineColor", Color.black);
            elevationArrow.fontMaterial.SetFloat("_OutlineWidth", 0.3f);
            elevationArrow.gameObject.SetActive(false);

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.SetParent(root, false);
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
            distRect.SetParent(root, false);
            distRect.anchorMin = new Vector2(0.5f, 0.5f);
            distRect.anchorMax = new Vector2(0.5f, 0.5f);
            distRect.pivot = new Vector2(0.5f, 0.5f);
            distRect.sizeDelta = new Vector2(120f, 20f);
            var distanceText = distGo.GetComponent<TextMeshProUGUI>();
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.enableWordWrapping = false;
            distanceText.fontSize = 14f;
            distanceText.color = new Color(1f, 1f, 1f, 0.9f);

            return new CompassMarkerWidget(kind, root, canvasGroup, iconImage, iconOutlines, faceOverlay, statusBadge, elevationArrow, nameText, distanceText);
        }

        public void Destroy()
        {
            if (Root != null)
            {
                Object.Destroy(Root.gameObject);
            }
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
            if (NativeAssets.OutlineMaterial != null)
            {
                if (_nameText.fontSharedMaterial != NativeAssets.OutlineMaterial) _nameText.fontSharedMaterial = NativeAssets.OutlineMaterial;
                if (_distanceText.fontSharedMaterial != NativeAssets.OutlineMaterial) _distanceText.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            var iconRect = (RectTransform)_iconImage.transform;
            iconRect.sizeDelta = new Vector2(iconSizePixels, iconSizePixels);

            // Campfire uses its own real HUD-icon colors - only the
            // procedurally-generated placeholder shapes get tinted per-anchor.
            _iconImage.color = _kind == CompassMarkerKind.Campfire ? Color.white : color;
            if (_kind == CompassMarkerKind.Campfire && NativeAssets.CampfireIconSprite != null && _iconImage.sprite != NativeAssets.CampfireIconSprite)
            {
                _iconImage.sprite = NativeAssets.CampfireIconSprite;
            }

            for (int i = 0; i < _iconOutlines.Length; i++)
            {
                Image outline = _iconOutlines[i];
                var outlineRect = (RectTransform)outline.transform;
                outlineRect.sizeDelta = new Vector2(iconSizePixels, iconSizePixels);
                outlineRect.anchoredPosition = OutlineOffsets[i] * 1.5f;
                if (_kind == CompassMarkerKind.Campfire && NativeAssets.CampfireIconSprite != null && outline.sprite != NativeAssets.CampfireIconSprite)
                {
                    outline.sprite = NativeAssets.CampfireIconSprite;
                }
            }

            // Elevation arrow sits beside the icon (see below), so the name
            // label can sit directly above it without the two colliding.
            float y = iconSizePixels * 0.62f;
            _nameText.gameObject.SetActive(showName && !string.IsNullOrEmpty(name));
            if (showName && !string.IsNullOrEmpty(name))
            {
                _nameText.text = name;
                ((RectTransform)_nameText.transform).anchoredPosition = new Vector2(0f, y + 12f);
            }

            float distY = -(iconSizePixels * 0.62f);
            _distanceText.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                _distanceText.text = $"{Mathf.RoundToInt(distanceMeters)}m";
                ((RectTransform)_distanceText.transform).anchoredPosition = new Vector2(0f, distY - 4f);
            }

            if (_statusBadge != null)
            {
                bool showBadge = isDead || isUnconscious;
                _statusBadge.gameObject.SetActive(showBadge);
                if (showBadge)
                {
                    _statusBadge.color = isDead ? new Color(0.8f, 0.15f, 0.15f) : new Color(0.85f, 0.75f, 0.1f);
                }
            }

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
            }
        }
    }
}
