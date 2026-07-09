using UnityEngine;

namespace SenseOfDirection.Compass
{
    /// <summary>Small shared color constants for the compass tape.</summary>
    public static class CompassTheme
    {
        /// <summary>True north's tick/letter gets this dark red instead of plain white - common real-world compass convention, and the one splash of color on an otherwise minimal/monochrome tape.</summary>
        public static readonly Color NorthAccent = new Color(0.65f, 0.1f, 0.1f, 1f);
    }
}
