using UnityEngine;

namespace SenseOfDirection.Compass
{
    /// <summary>Small shared color constants for the compass tape.</summary>
    public static class CompassTheme
    {
        /// <summary>True north's tick/letter gets this dark red instead of plain white - common real-world compass convention, and the one splash of color on an otherwise minimal/monochrome tape. Not affected by <c>compass-line-color</c> - it's the one deliberate splash of color regardless of base tape color.</summary>
        public static readonly Color NorthAccent = new Color(0.65f, 0.1f, 0.1f, 1f);

        /// <summary>Resolves <c>compass-line-color</c> to the base RGB every tick line/label and the baseline stripe are tinted against (alpha is applied separately per-element, same as the old hardcoded <see cref="Color.white"/>).</summary>
        public static Color LineColor(CompassLineColor choice)
        {
            switch (choice)
            {
                case CompassLineColor.White: return Color.white;
                case CompassLineColor.LightGray: return new Color(0.78f, 0.78f, 0.78f, 1f);
                case CompassLineColor.Gray: return new Color(0.55f, 0.55f, 0.55f, 1f);
                case CompassLineColor.DarkGray: return new Color(0.32f, 0.32f, 0.32f, 1f);
                case CompassLineColor.Black: return Color.black;
                default: return Color.white;
            }
        }
    }
}
