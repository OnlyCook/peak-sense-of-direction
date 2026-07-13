namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// <em>Where</em> a tracked thing gets drawn: the edge-of-screen widget
    /// (label/arrow clamped to the screen edge), the top-of-screen compass tape,
    /// or both at once.
    ///
    /// Deliberately not to be confused with <see cref="Labels.LabelDisplayMode"/>,
    /// which answers a different question - <em>when</em> player labels are shown
    /// (Toggle/AlwaysOn/Hold). Placement is about surface, display mode is about
    /// timing.
    ///
    /// Each mechanic (player labels, campfire, pings, item pings) has its own
    /// independent entry of this type rather than one global switch, so e.g.
    /// pings can stay off-screen-arrow-only while players show on the compass.
    /// All four live together in the `General` config section: they're routing
    /// between two shared rendering surfaces rather than a property of any one
    /// mechanic, and "show everything on the compass" is a single intent that
    /// shouldn't cost four trips through the settings menu.
    /// </summary>
    public enum IndicatorPlacement
    {
        OffScreenOnly,
        CompassOnly,
        Both,
    }
}
