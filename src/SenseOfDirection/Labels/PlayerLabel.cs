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
        private readonly Image _hostIconImage;
        private readonly GameObject _deadIcon;
        private readonly GameObject _unconsciousIcon;

        /// <summary>Vanilla's own fade rate (`Time.deltaTime * 5f`), matched here (UIPlayerNames.UpdateName).</summary>
        private const float FadeSpeedPerSecond = 5f;

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
            _hostIconImage = hostIconImage;
            _deadIcon = deadIcon;
            _unconsciousIcon = unconsciousIcon;

            Anchor = new IndicatorAnchor(getWorldPosition, root);
        }

        public static PlayerLabel Create(System.Func<Vector3> getWorldPosition)
        {
            RectTransform canvasTransform = IndicatorManager.Instance.CanvasTransform;

            var rootGo = new GameObject("SoD.PlayerLabel", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(canvasTransform, false);
            root.sizeDelta = new Vector2(220f, 90f);

            var canvasGroup = rootGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            GameObject hostIcon = CreateIcon(root, "HostIcon", Color.white, new Vector2(20f, 20f), new Vector2(0f, 26f));
            Image hostIconImage = hostIcon.GetComponent<Image>();
            hostIcon.SetActive(false);

            TMP_Text nameText = CreateText(root, "Name", new Vector2(0f, 10f));
            TMP_Text distanceText = CreateText(root, "Distance", new Vector2(0f, -12f));

            GameObject deadIcon = CreateIcon(root, "DeadIcon", new Color(0.8f, 0.15f, 0.15f), new Vector2(14f, 14f), new Vector2(0f, -32f));
            deadIcon.SetActive(false);

            GameObject unconsciousIcon = CreateIcon(root, "UnconsciousIcon", new Color(0.85f, 0.75f, 0.1f), new Vector2(14f, 14f), new Vector2(0f, -32f));
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

        private static GameObject CreateIcon(RectTransform parent, string goName, Color color, Vector2 size, Vector2 anchoredPosition)
        {
            var go = new GameObject(goName, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.anchoredPosition = anchoredPosition;
            go.GetComponent<Image>().color = color;
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
            bool showDistance, bool showBadges)
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

            _nameText.fontSize = nameFontSize;
            _distanceText.fontSize = distanceFontSize;

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

            _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, targetAlpha, Time.deltaTime * FadeSpeedPerSecond);
        }
    }
}
