namespace SenseOfDirection.Compass
{
    /// <summary>
    /// Gates when the compass tape is shown at all (<c>compass-display-mode</c>),
    /// replacing the old plain on/off <c>requires-holding-item</c> toggle with
    /// four progressively stricter conditions - see
    /// <see cref="Compass.CompassManager.IsDisplayModeSatisfied"/> for what each
    /// one actually checks.
    /// </summary>
    public enum CompassDisplayMode
    {
        /// <summary>No requirement - the tape is always shown (once enable-compass itself is on).</summary>
        AlwaysOn,

        /// <summary>Anywhere on the player at all - <see cref="MainInventory"/>'s own set, plus a worn backpack's own internal storage.</summary>
        Carried,

        /// <summary>
        /// A compass item sits anywhere in the player's own hand-inventory: one
        /// of the 3 main slots (equipped or not) or the temporary 4th slot used
        /// while picking something up. Includes <see cref="RequireHolding"/>'s
        /// case naturally, since an equipped item is still sitting in its slot.
        /// </summary>
        MainInventory,

        /// <summary>The strictest, and the original behaviour: a compass item must be the one actively equipped/held in hand right now.</summary>
        RequireHolding,
    }
}
