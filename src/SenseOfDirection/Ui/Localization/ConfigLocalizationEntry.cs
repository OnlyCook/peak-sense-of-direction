namespace SenseOfDirection.Ui.Localization
{
    /// <summary>One config entry's localized display name + description, for one language - see <see cref="ConfigLocalizationTable"/>.</summary>
    internal readonly struct ConfigLocalizationEntry
    {
        internal readonly string Name;
        internal readonly string Description;

        internal ConfigLocalizationEntry(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
