using SenseOfDirection.Common;
using UnityEngine;
using UnityEngine.UI;

namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// Builds the off-screen "dart" arrow child GameObject shared by every
    /// widget that opts into an arrow (currently <see cref="Pings.PingWidget"/>
    /// and <see cref="ItemPings.ItemPingWidget"/>). Centralized so the sprite/
    /// shadow setup can't drift out of sync between widget types the way the
    /// old copy-pasted plain-rectangle Image code did.
    /// </summary>
    public static class OffScreenArrow
    {
        /// <summary>
        /// The dart art's own footprint. Exposed because
        /// <see cref="ItemPings.ItemPingWidget"/> swaps a (square, larger)
        /// item icon into this same Image while
        /// <c>use-native-item-ping-icons</c> is on, and needs to be able to put
        /// the dart's own size back when it swaps out again.
        /// </summary>
        public static readonly Vector2 DartSize = new Vector2(19f, 21f);

        public static RectTransform Create(RectTransform parent, Color color)
        {
            var arrowGo = new GameObject("Arrow", typeof(RectTransform), typeof(Image), typeof(Shadow));
            var arrowRect = (RectTransform)arrowGo.transform;
            arrowRect.SetParent(parent, false);
            arrowRect.sizeDelta = DartSize;

            // Pivot at the shape's own geometric center, not its tail: this
            // rotates in place like a compass needle in a fixed bezel, so it
            // stays visually centered above the distance label at every
            // angle. Pivoting near the tail instead (rotation point = where
            // the "pointer" originates) looks right for angles near
            // straight-up/down, but swings the whole silhouette noticeably
            // sideways once the target is off to the side.
            arrowRect.pivot = new Vector2(0.5f, 0.5f);
            // Sits in the gap between ItemPingWidget's name label above
            // (anchoredPosition.y +18) and the distance label below (-22) -
            // shared with PingWidget, which has no name label, so this also
            // just needs to clear its own distance label.
            arrowRect.anchoredPosition = new Vector2(0f, -2f);

            var arrowImage = arrowGo.GetComponent<Image>();
            arrowImage.sprite = IconAssets.PingArrow;
            arrowImage.color = color;
            arrowImage.raycastTarget = false;
            // Only matters for the item-ping swap above (the dart's own art is
            // authored at this exact aspect) - keeps a non-square item icon
            // from being stretched into whatever box it lands in.
            arrowImage.preserveAspect = true;

            var arrowShadow = arrowGo.GetComponent<Shadow>();
            arrowShadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            arrowShadow.effectDistance = new Vector2(1.5f, -2f);
            arrowShadow.useGraphicAlpha = true;

            return arrowRect;
        }
    }
}
