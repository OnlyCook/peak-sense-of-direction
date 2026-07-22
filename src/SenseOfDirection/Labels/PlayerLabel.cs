using SenseOfDirection.Common;
using SenseOfDirection.Indicators;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Labels
{
    /// <summary>
    /// One player's on-screen label: name + distance sub-line + host/status
    /// icons. Built as a small UI hierarchy under <see cref="IndicatorManager"/>'s
    /// shared canvas and registered there as an <see cref="IndicatorAnchor"/>
    /// so Phase 2's edge-clamping applies to it automatically (no off-screen
    /// arrow though - that's reserved for pings, per maintainer direction;
    /// labels just clamp quietly to the edge).
    ///
    /// Badges are stacked vertically (crown above the name, status badge
    /// below the distance line) rather than offset left/right - a
    /// horizontally-offset badge can get pushed past the actual screen edge
    /// and clipped when the label itself is already edge-clamped near that
    /// side, which a vertical stack avoids.
    ///
    /// <see cref="PlayerLabelController"/> owns the per-frame content refresh
    /// (text/color/icon visibility, fade) and drives the crossfade with
    /// vanilla's own name label; the indicator manager owns positioning.
    /// </summary>
    public class PlayerLabel
    {
        public readonly IndicatorAnchor Anchor;

        private readonly RectTransform _root;
        private readonly CanvasGroup _canvasGroup;
        private readonly TMP_Text _nameText;
        private readonly TMP_Text _distanceText;
        private readonly GameObject _hostIcon;
        private readonly RectTransform _hostIconRect;
        private readonly Image _hostIconImage;
        private readonly GameObject _deadIcon;
        private readonly RectTransform _deadIconRect;
        private readonly GameObject _unconsciousIcon;
        private readonly RectTransform _unconsciousIconRect;

        /// <summary>Vanilla's own fade rate (`Time.deltaTime * 5f`), matched here (UIPlayerNames.UpdateName).</summary>
        private const float FadeSpeedPerSecond = 5f;

        /// <summary>
        /// Four player labels standing on the same spot have to stack ~93px
        /// apart to clear each other, i.e. ~140px from the middle of that stack
        /// to either end - a 56px cap simply cannot separate a full party, so
        /// they stayed piled up. This is the label's own tracked position moving
        /// (name, distance and badges together), not text drifting away from an
        /// icon left behind, so a shift of this size still reads as belonging to
        /// its player.
        /// </summary>
        private const float MaxOverlapOffsetPixels = 140f;

        /// <summary>Horizontal breathing room added to the measured text width.</summary>
        private const float OverlapPaddingPixels = 12f;

        /// <summary>
        /// The host crown badge's own inner (bottom) edge Y, and the status
        /// badge's own inner (top) edge Y with/without the distance line -
        /// i.e. the edge facing the name/distance text, which stays put as
        /// <c>badge-size-pixels</c> changes so a bigger badge grows outward,
        /// away from the text, rather than over it. Each badge's actual
        /// anchored Y is this inner edge plus/minus half its current size -
        /// see <see cref="Refresh"/>. Derived from the original fixed 26px
        /// badge layout (host Y=29, status Y=-35/-20, half=13): 29-13=16,
        /// -35+13=-22, -20+13=-7.
        /// </summary>
        private const float HostBadgeInnerEdgeY = 16f;
        private const float StatusBadgeInnerEdgeYWithDistance = -22f;
        private const float StatusBadgeInnerEdgeYWithoutDistance = -7f;

        private PlayerLabel(
            RectTransform root, CanvasGroup canvasGroup,
            TMP_Text nameText, TMP_Text distanceText,
            GameObject hostIcon, Image hostIconImage,
            GameObject deadIcon, GameObject unconsciousIcon,
            System.Func<Vector3> getWorldPosition)
        {
            _root = root;
            _canvasGroup = canvasGroup;
            _nameText = nameText;
            _distanceText = distanceText;
            _hostIcon = hostIcon;
            _hostIconRect = (RectTransform)hostIcon.transform;
            _hostIconImage = hostIconImage;
            _deadIcon = deadIcon;
            _deadIconRect = (RectTransform)deadIcon.transform;
            _unconsciousIcon = unconsciousIcon;
            _unconsciousIconRect = (RectTransform)unconsciousIcon.transform;

            // OverlapSize/OverlapCenterOffset are refined every Refresh() call
            // below to what's actually rendered - a fixed 220x90 claimed the
            // width of the longest name imaginable no matter what this label
            // says, which made two comfortably-separated labels "collide" and
            // shove each other for no reason. The whole widget moves (no
            // LabelWidget child), so it can afford a larger cap than a label
            // sliding away from an arrow that stays put.
            Anchor = new IndicatorAnchor(getWorldPosition, root)
            {
                OverlapSize = new Vector2(220f, 90f),
                MaxOverlapOffset = MaxOverlapOffsetPixels,
            };
        }

        /// <param name="parent">Where the widget is built. Null (the live game) means the shared overlay canvas; the config preview menu passes its own stage instead.</param>
        public static PlayerLabel Create(System.Func<Vector3> getWorldPosition, RectTransform parent = null)
        {
            RectTransform canvasTransform = parent != null ? parent : IndicatorManager.Instance.CanvasTransform;

            var rootGo = new GameObject("SoD.PlayerLabel", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(canvasTransform, false);
            root.sizeDelta = new Vector2(220f, 90f);

            var canvasGroup = rootGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Badge size bumped from the original 20x20 - barely distinguishable
            // at that size in practice. Offsets shifted outward by the same
            // +3px the half-size grew by (10 -> 13), so the gap to the name/
            // distance text stays exactly what it was before, just with a
            // bigger badge.
            GameObject hostIcon = CreateIcon(root, "HostIcon", Color.white, new Vector2(26f, 26f), new Vector2(0f, 29f));
            Image hostIconImage = hostIcon.GetComponent<Image>();
            hostIcon.SetActive(false);

            TMP_Text nameText = CreateText(root, "Name", new Vector2(0f, 10f));
            TMP_Text distanceText = CreateText(root, "Distance", new Vector2(0f, -12f));

            GameObject deadIcon = CreateIcon(root, "DeadIcon", Color.white, new Vector2(26f, 26f), new Vector2(0f, -35f), IconAssets.DeadBadge);
            deadIcon.SetActive(false);

            GameObject unconsciousIcon = CreateIcon(root, "UnconsciousIcon", Color.white, new Vector2(26f, 26f), new Vector2(0f, -35f), IconAssets.UnconsciousBadge);
            unconsciousIcon.SetActive(false);

            return new PlayerLabel(root, canvasGroup, nameText, distanceText, hostIcon, hostIconImage, deadIcon, unconsciousIcon, getWorldPosition);
        }

        private static TMP_Text CreateText(RectTransform parent, string goName, Vector2 anchoredPosition)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = new Vector2(220f, 30f);
            rect.anchoredPosition = anchoredPosition;

            var text = go.GetComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.enableWordWrapping = false;
            return text;
        }

        private static GameObject CreateIcon(RectTransform parent, string goName, Color color, Vector2 size, Vector2 anchoredPosition, Sprite sprite = null)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            var image = go.GetComponent<Image>();
            image.color = color;
            image.preserveAspect = true;
            if (sprite != null)
            {
                image.sprite = sprite;
            }
            return go;
        }

        public void Destroy()
        {
            if (_root != null)
            {
                Object.Destroy(_root.gameObject);
            }
        }

        /// <summary>
        /// Direct read/write of the label's own fade alpha, bypassing the
        /// targetAlpha <see cref="Refresh"/> would otherwise drive it towards.
        /// Only <see cref="PlayerLabelController.ResetAll"/> uses the setter,
        /// to ease a label out on a scene load rather than destroying it
        /// outright while still visible.
        /// </summary>
        public float Alpha
        {
            get => _canvasGroup.alpha;
            set => _canvasGroup.alpha = value;
        }

        /// <summary>
        /// Updates text/colors/icon visibility/fade for the current frame.
        /// Font size, font asset/material, and the show-distance/show-badges
        /// toggles are all re-applied every call (cheap) rather than baked
        /// in at creation, so config changes take effect immediately without
        /// a restart, and so a label created before <see cref="NativeAssets"/>
        /// finished discovering the native font still picks it up as soon as
        /// it's found. Positioning is IndicatorManager's job.
        /// </summary>
        public void Refresh(
            string name, float distanceMeters, bool isHost, bool isDead, bool isUnconscious,
            Color nameColor, float nameFontSize, float distanceFontSize, float targetAlpha,
            bool showDistance, bool showBadges, float badgeSizePixels)
        {
            if (NativeAssets.Font != null && _nameText.font != NativeAssets.Font)
            {
                _nameText.font = NativeAssets.Font;
                _distanceText.font = NativeAssets.Font;
            }
            if (NativeAssets.OutlineMaterial != null && _nameText.fontSharedMaterial != NativeAssets.OutlineMaterial)
            {
                _nameText.fontSharedMaterial = NativeAssets.OutlineMaterial;
                _distanceText.fontSharedMaterial = NativeAssets.OutlineMaterial;
            }

            _nameText.fontSize = HudFontScale.Name(nameFontSize, Anchor.OffScreenBlend);
            _distanceText.fontSize = HudFontScale.Distance(distanceFontSize, Anchor.OffScreenBlend);

            _nameText.text = name;
            _nameText.color = nameColor;

            _distanceText.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                _distanceText.text = $"{Mathf.RoundToInt(distanceMeters)}m";
            }

            _hostIcon.SetActive(showBadges && isHost);
            if (NativeAssets.HostStarSprite != null && _hostIconImage.sprite != NativeAssets.HostStarSprite)
            {
                _hostIconImage.sprite = NativeAssets.HostStarSprite;
            }

            _deadIcon.SetActive(showBadges && isDead);
            _unconsciousIcon.SetActive(showBadges && !isDead && isUnconscious);

            // Live config value, so re-applied every frame rather than baked in
            // at creation - see PluginConfig.PlayerLabelBadgeSizePixels. Grows
            // outward from its own inner (text-facing) edge rather than from
            // its centre, so a bigger badge pushes further from the name/
            // distance text instead of drawing over it - see the *InnerEdgeY
            // constants' own doc comment.
            float badgeHalf = badgeSizePixels * 0.5f;
            var badgeSize = new Vector2(badgeSizePixels, badgeSizePixels);
            _hostIconRect.sizeDelta = badgeSize;
            _deadIconRect.sizeDelta = badgeSize;
            _unconsciousIconRect.sizeDelta = badgeSize;
            _hostIconRect.anchoredPosition = new Vector2(0f, HostBadgeInnerEdgeY + badgeHalf);

            // Move the status badge up to sit directly under the name when the
            // distance line isn't there to sit under, so it doesn't hang in the
            // gap the hidden line would have filled (both visually and in the
            // overlap-avoidance box below).
            float statusBadgeY = (showDistance ? StatusBadgeInnerEdgeYWithDistance : StatusBadgeInnerEdgeYWithoutDistance) - badgeHalf;
            _deadIconRect.anchoredPosition = new Vector2(0f, statusBadgeY);
            _unconsciousIconRect.anchoredPosition = new Vector2(0f, statusBadgeY);

            // Unscaled: the preview menu freezes the game while it's open, and a
            // scaled delta is zero there - the labels would snap between shown and
            // hidden instead of fading, which is the one thing the preview is meant
            // to show honestly. In play the two are the same thing.
            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * FadeSpeedPerSecond);

            RefreshOverlapBox(showDistance, showBadges && isHost, showBadges && (isDead || isUnconscious), statusBadgeY, badgeHalf);
        }

        /// <summary>
        /// The overlap box (see <see cref="IndicatorAnchor.OverlapSize"/>/
        /// <see cref="IndicatorAnchor.OverlapCenterOffset"/>) as this label is
        /// currently rendered, rather than a fixed worst-case guess: as wide as
        /// its widest visible line, and spanning only the elements actually
        /// shown. Vertical extents come straight from the layout above - the
        /// crown badge tops out at +42 (anchored +29, 26 tall), the name caps at
        /// +25, and the lower edge follows whatever is actually the bottom-most
        /// visible element: the status badge (which itself moves up when there's
        /// no distance line), else the distance line at -24, else the bare name
        /// at -5 - which means the box is <em>not</em> centred on the tracked
        /// point, hence the centre offset.
        /// </summary>
        private void RefreshOverlapBox(bool showDistance, bool showHostBadge, bool showStatusBadge, float statusBadgeY, float badgeHalf)
        {
            float width = _nameText.GetPreferredValues().x;
            if (showDistance)
            {
                width = Mathf.Max(width, _distanceText.GetPreferredValues().x);
            }

            float top = showHostBadge ? HostBadgeInnerEdgeY + badgeHalf * 2f : 25f;
            float bottom;
            if (showStatusBadge)
            {
                bottom = statusBadgeY - badgeHalf;
            }
            else if (showDistance)
            {
                bottom = -24f;
            }
            else
            {
                bottom = -5f;
            }

            Anchor.OverlapSize = new Vector2(width + OverlapPaddingPixels, top - bottom);
            Anchor.OverlapCenterOffset = new Vector2(0f, (top + bottom) * 0.5f);
        }
    }
}
