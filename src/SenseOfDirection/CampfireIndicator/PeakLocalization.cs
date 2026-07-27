using System.Collections.Generic;

namespace SenseOfDirection.CampfireIndicator
{
    /// <summary>
    /// Translated "Peak" text for the indicator's own name label once
    /// <see cref="CampfireIndicatorController"/> has switched off the campfire
    /// and onto the summit. Same situation (and same community-sourced caveat)
    /// as <see cref="CampfireLocalization"/>: on-screen marker text, not a config
    /// entry, so it has no row in <c>Ui.Localization.ConfigLocalizationTable</c>.
    ///
    /// Deliberately not read off the game's own <c>PEAK</c> progress-point title
    /// (<c>MountainProgressHandler.ProgressPoint.localizedTitle</c>) even though
    /// one exists: that string is authored for the game's full-screen hero-title
    /// card and is styled/cased for it, which doesn't match the sentence-case
    /// one-word labels every other marker in this mod uses.
    /// </summary>
    internal static class PeakLocalization
    {
        private static readonly Dictionary<LocalizedText.Language, string> Table =
            new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "Peak",
                [LocalizedText.Language.French] = "Sommet",
                [LocalizedText.Language.Italian] = "Vetta",
                [LocalizedText.Language.German] = "Gipfel",
                [LocalizedText.Language.SpanishSpain] = "Cima",
                [LocalizedText.Language.SpanishLatam] = "Cima",
                [LocalizedText.Language.BRPortuguese] = "Cume",
                [LocalizedText.Language.Russian] = "Вершина",
                [LocalizedText.Language.Ukrainian] = "Вершина",
                [LocalizedText.Language.SimplifiedChinese] = "山顶",
                [LocalizedText.Language.TraditionalChinese] = "山頂",
                [LocalizedText.Language.Japanese] = "山頂",
                [LocalizedText.Language.Korean] = "정상",
                [LocalizedText.Language.Polish] = "Szczyt",
                [LocalizedText.Language.Turkish] = "Zirve",
            };

        internal static string Name =>
            Table.TryGetValue(LocalizedText.CURRENT_LANGUAGE, out string name) ? name : Table[LocalizedText.Language.English];
    }
}
