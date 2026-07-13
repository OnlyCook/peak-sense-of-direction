using UnityEngine;

namespace SenseOfDirection.Common
{
    /// <summary>
    /// Resolves the `Fonts` section's per-area scales into an actual font size.
    ///
    /// The settings are multipliers rather than absolute sizes so that each
    /// widget keeps the size it was tuned to relative to its neighbours - a
    /// player's name deliberately reads larger than an item ping's, which reads
    /// larger than a compass marker's. One flat pixel size per area would
    /// collapse that hierarchy; a multiplier scales the whole thing while
    /// preserving it. At the default 1 every call here is a no-op, i.e. the mod
    /// renders exactly the sizes it always did.
    /// </summary>
    public static class HudFontScale
    {
        /// <summary>
        /// A name label's size, given the widget's own base size and how
        /// off-screen its anchor currently is (<see cref="Indicators.IndicatorAnchor.OffScreenBlend"/>).
        /// Lerped rather than picked, because a label crossing the screen edge
        /// is one widget changing state, not two widgets swapping - snapping the
        /// font size mid-transition would undo the very smoothing the position
        /// transition exists to provide.
        /// </summary>
        public static float Name(float baseSize, float offScreenBlend)
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            return baseSize * Mathf.Lerp(cfg.OnScreenNameFontScale.Value, cfg.OffScreenNameFontScale.Value, offScreenBlend);
        }

        /// <summary>Same as <see cref="Name"/>, for a distance sub-line.</summary>
        public static float Distance(float baseSize, float offScreenBlend)
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            return baseSize * Mathf.Lerp(cfg.OnScreenDistanceFontScale.Value, cfg.OffScreenDistanceFontScale.Value, offScreenBlend);
        }

        /// <summary>
        /// A compass marker's name label. The compass tape has no on/off-screen
        /// state of its own - a marker is either on the tape or not shown at all
        /// - so it gets its own flat pair of scales instead of a blend.
        /// </summary>
        public static float CompassName(float baseSize)
        {
            return baseSize * Plugin.Instance.Cfg.CompassNameFontScale.Value;
        }

        /// <summary>Same as <see cref="CompassName"/>, for a marker's distance sub-label.</summary>
        public static float CompassDistance(float baseSize)
        {
            return baseSize * Plugin.Instance.Cfg.CompassDistanceFontScale.Value;
        }
    }
}
