namespace SenseOfDirection.PirateCompass
{
    /// <summary>
    /// Gates when the Pirate's Compass luggage indicator is shown
    /// (<c>Pirate-Compass/luggage-indicator-display-mode</c>), evaluated against
    /// Pirate's Compasses only and entirely independently of the compass tape's
    /// own <c>Compass/display-mode</c> + <c>Compass/pirate-display-mode</c> pair
    /// - the tape and this indicator are different things gated by different
    /// settings, so e.g. tape from a compass merely stashed in your backpack,
    /// but the luggage arrow only once one is in your own inventory slots.
    ///
    /// The same levels as <see cref="Compass.CompassDisplayMode"/> minus its
    /// <see cref="Compass.CompassDisplayMode.AlwaysOn"/>: an unconditional
    /// "always on" here would be a permanent free nearest-luggage arrow with no
    /// Pirate's Compass involved at all, which both removes the point of the
    /// item and undercuts this mod's own Luggage-Ping mechanic (which pays for
    /// the same information with a radius and a cooldown). Same reason
    /// <see cref="Compass.PirateCompassDisplayMode"/> drops that level too.
    /// </summary>
    public enum PirateCompassLuggageDisplayMode
    {
        /// <inheritdoc cref="Compass.CompassDisplayMode.Carried"/>
        Carried,

        /// <inheritdoc cref="Compass.CompassDisplayMode.MainInventory"/>
        MainInventory,

        /// <summary>The strictest, and the original behaviour before this setting existed: a Pirate's Compass must be the one actively equipped/held in hand right now.</summary>
        RequireHolding,
    }
}
