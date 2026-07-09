namespace SenseOfDirection.Compass
{
    /// <summary>
    /// Which placeholder-icon shape/behavior <see cref="CompassMarkerWidget"/>
    /// builds for an <see cref="Indicators.IndicatorAnchor"/>. None means the
    /// anchor never gets a compass marker at all (default for anything that
    /// hasn't opted in).
    /// </summary>
    public enum CompassMarkerKind
    {
        None,
        Player,
        Campfire,
        Ping,
        ItemPing,
    }
}
