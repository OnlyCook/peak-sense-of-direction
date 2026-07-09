namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// Per-indicator-type choice between the original edge-of-screen widget
    /// (label/arrow clamped to the screen edge) and Phase 7's top-of-screen
    /// compass tape, or both at once. Each mechanic (player labels, campfire,
    /// pings, item pings) has its own independent config entry of this type -
    /// see <c>PluginConfig</c> - rather than one global switch, so e.g. pings
    /// can stay off-screen-arrow-only while players show on the compass.
    /// </summary>
    public enum IndicatorDisplayMode
    {
        OffScreenOnly,
        CompassOnly,
        Both,
    }
}
