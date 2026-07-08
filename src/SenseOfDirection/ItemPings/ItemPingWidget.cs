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
    /// same as the ping/ripple.
    /// </summary>
    public class ItemPingWidget
    {
        public readonly IndicatorAnchor Anchor;
        public CanvasGroup CanvasGroup { get; }

        private readonly TMP_Text _nameText;
        private readonly TMP_Text _distanceText;

        private ItemPingWidget(RectTransform root, CanvasGroup canvasGroup, RectTransform arrow, TMP_Text nameText, TMP_Text distanceText, System.Func<Vector3> getWorldPosition)
        {
            CanvasGroup = canvasGroup;
            _nameText = nameText;
            _distanceText = distanceText;
            Anchor = new IndicatorAnchor(getWorldPosition, root, arrow);
        }

        public static ItemPingWidget Create(System.Func<Vector3> getWorldPosition, Color color, bool enableArrow)
        {
            RectTransform canvasTransform = IndicatorManager.Instance.CanvasTransform;

            var rootGo = new GameObject("SoD.ItemPingIndicator", typeof(RectTransform));
            var root = (RectTransform)rootGo.transform;
            root.SetParent(canvasTransform, false);
            root.sizeDelta = new Vector2(20f, 20f);

            var canvasGroup = rootGo.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            RectTransform arrowRect = null;
            if (enableArrow)
            {
                var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
                arrowRect = (RectTransform)arrowGo.transform;
                arrowRect.SetParent(root, false);
                arrowRect.sizeDelta = new Vector2(14f, 26f);
                arrowRect.pivot = new Vector2(0.5f, 0.15f);
                arrowGo.GetComponent<Image>().color = color;
            }

            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            var nameRect = (RectTransform)nameGo.transform;
            nameRect.SetParent(root, false);
            nameRect.sizeDelta = new Vector2(320f, 28f);
            nameRect.anchoredPosition = new Vector2(0f, 18f);

            var nameText = nameGo.GetComponent<TextMeshProUGUI>();
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.color = color;
            nameText.fontSize = 20f;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Overflow;

            var distGo = new GameObject("Distance", typeof(RectTransform), typeof(TextMeshProUGUI));
            var distRect = (RectTransform)distGo.transform;
            distRect.SetParent(root, false);
            distRect.sizeDelta = new Vector2(120f, 24f);
            distRect.anchoredPosition = new Vector2(0f, -22f);

            var distanceText = distGo.GetComponent<TextMeshProUGUI>();
            distanceText.alignment = TextAlignmentOptions.Center;
            distanceText.color = color;
            distanceText.fontSize = 16f;
            distanceText.enableWordWrapping = false;

            return new ItemPingWidget(root, canvasGroup, arrowRect, nameText, distanceText, getWorldPosition);
        }

        public void Refresh(string displayName, float distanceMeters, bool showName, bool showDistance)
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

            _nameText.gameObject.SetActive(showName);
            if (showName)
            {
                _nameText.text = displayName;
            }

            _distanceText.gameObject.SetActive(showDistance);
            if (showDistance)
            {
                _distanceText.text = $"{Mathf.RoundToInt(distanceMeters)}m";
            }
        }
    }
}
