using System.Collections.Generic;

namespace SenseOfDirection.CampfireIndicator
{
    /// <summary>
    /// Translated "Campfire" text for the compass marker's own name label
    /// (<see cref="CampfireIndicatorController"/>, <see cref="Ui.PreviewScene"/>'s
    /// mock campfire) - keyed off the game's own <c>LocalizedText.CURRENT_LANGUAGE</c>,
    /// same pattern as <see cref="GhostFreeCam.GhostFreeCamLocalization"/>. This
    /// is the compass marker's own on-screen text, not a config entry, so it has
    /// no row in <see cref="Ui.Localization.ConfigLocalizationTable"/> - maintained
    /// here directly instead. Community-sourced, not professionally reviewed -
    /// good enough for a single-word marker label, easy to correct/extend
    /// per-language later.
    /// </summary>
    internal static class CampfireLocalization
    {
        private static readonly Dictionary<LocalizedText.Language, string> Table =
            new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "Campfire",
                [LocalizedText.Language.French] = "Feu de camp",
                [LocalizedText.Language.Italian] = "Falò",
                [LocalizedText.Language.German] = "Lagerfeuer",
                [LocalizedText.Language.SpanishSpain] = "Hoguera",
                [LocalizedText.Language.SpanishLatam] = "Fogata",
                [LocalizedText.Language.BRPortuguese] = "Fogueira",
                [LocalizedText.Language.Russian] = "Костёр",
                [LocalizedText.Language.Ukrainian] = "Багаття",
                [LocalizedText.Language.SimplifiedChinese] = "篝火",
                [LocalizedText.Language.TraditionalChinese] = "營火",
                [LocalizedText.Language.Japanese] = "焚き火",
                [LocalizedText.Language.Korean] = "모닥불",
                [LocalizedText.Language.Polish] = "Ognisko",
                [LocalizedText.Language.Turkish] = "Kamp Ateşi",
            };

        internal static string Name =>
            Table.TryGetValue(LocalizedText.CURRENT_LANGUAGE, out string name) ? name : Table[LocalizedText.Language.English];
    }
}
