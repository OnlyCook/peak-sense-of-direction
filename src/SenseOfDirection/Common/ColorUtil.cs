using UnityEngine;

namespace SenseOfDirection.Common
{
    /// <summary>Small color-math helpers shared across label/icon widgets.</summary>
    public static class ColorUtil
    {
        /// <summary>
        /// Darkens a color for use as an outline/stroke by scaling its HSV
        /// value component down, rather than flattening to pure black - keeps
        /// the stroke visibly tied to its owning anchor's own color (e.g. a
        /// player's character color) instead of a flat black line that reads
        /// the same for every anchor regardless of color.
        /// </summary>
        public static Color Darken(Color color, float amount = 0.55f)
        {
            Color.RGBToHSV(color, out float h, out float s, out float v);
            Color darkened = Color.HSVToRGB(h, s, v * (1f - amount));
            darkened.a = color.a;
            return darkened;
        }
    }
}
