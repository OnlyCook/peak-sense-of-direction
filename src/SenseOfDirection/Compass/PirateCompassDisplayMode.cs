namespace SenseOfDirection.Compass
{
    /// <summary>
    /// The Pirate's Compass half of the compass tape's display-mode gate
    /// (<c>pirate-display-mode</c>), evaluated against Pirate's Compasses the
    /// same way <see cref="CompassDisplayMode"/> is evaluated against regular
    /// ones, with the two OR'd together (see
    /// <see cref="CompassManager.IsDisplayModeSatisfied(PluginConfig)"/>).
    ///
    /// Same levels as <see cref="CompassDisplayMode"/> except for the first:
    /// an unconditional "always on" would be meaningless here (it would show
    /// the tape whether or not a Pirate's Compass exists anywhere on you, which
    /// <see cref="CompassDisplayMode.AlwaysOn"/> on the regular setting already
    /// does), so that slot is <see cref="MatchDisplayMode"/> instead.
    /// </summary>
    public enum PirateCompassDisplayMode
    {
        /// <summary>No separate rule: a Pirate's Compass counts under whatever level <c>display-mode</c> itself is set to.</summary>
        MatchDisplayMode,

        /// <inheritdoc cref="CompassDisplayMode.Carried"/>
        Carried,

        /// <inheritdoc cref="CompassDisplayMode.MainInventory"/>
        MainInventory,

        /// <inheritdoc cref="CompassDisplayMode.RequireHolding"/>
        RequireHolding,
    }
}
