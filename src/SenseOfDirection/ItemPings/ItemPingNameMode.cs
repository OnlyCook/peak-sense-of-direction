namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// When a pinged item/luggage/creature shows its name label. Replaces what
    /// used to be two separate bools (<c>show-item-ping-name</c> plus
    /// <c>only-show-item-ping-name-without-icon</c>), which between them encoded
    /// exactly these three states - and left the second one meaningless unless
    /// both the first and <c>use-native-icons</c> happened to be on.
    /// </summary>
    public enum ItemPingNameMode
    {
        /// <summary>Never show a name - just the highlight (and distance, if enabled).</summary>
        Never,

        /// <summary>
        /// Hide the name of anything already showing its own in-game icon (the
        /// icon says what it is), while things without one - luggage, creatures,
        /// hazards - keep their name so you can still tell what you pinged.
        /// Falls back to <see cref="Always"/> when <c>use-native-icons</c> is
        /// off, since then nothing has an icon to speak for it.
        /// </summary>
        HideWhenIconShown,

        /// <summary>Always show the name, icon or not.</summary>
        Always,
    }
}
