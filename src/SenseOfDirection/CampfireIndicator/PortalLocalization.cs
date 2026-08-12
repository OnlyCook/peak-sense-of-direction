using System.Collections.Generic;

namespace SenseOfDirection.CampfireIndicator
{
    /// <summary>
    /// Translated "Portal" text for the indicator's own name label while the run
    /// is down in the Nadir and <see cref="CampfireIndicatorController"/> is
    /// pointing at the way back out. Same situation (and same community-sourced
    /// caveat) as <see cref="CampfireLocalization"/>/<see cref="PeakLocalization"/>:
    /// on-screen marker text, not a config entry, so it has no row in
    /// <c>Ui.Localization.ConfigLocalizationTable</c>.
    ///
    /// Deliberately not read off the portal's own vanilla name
    /// (<c>PeakGatePortal.GetName()</c> -> <c>LocalizedText.GetText("NAME_PEAKPORTAL")</c>),
    /// for the same reason <see cref="PeakLocalization"/> passes on the game's
    /// progress-point title: that string is authored for vanilla's interaction
    /// prompt, not for the sentence-case one-word labels every other marker in
    /// this mod uses - and <c>GetText</c> answers a missing key with the literal
    /// placeholder <c>"LOC: NAME_PEAKPORTAL"</c> rather than anything a player
    /// should ever see on their HUD.
    /// </summary>
    internal static class PortalLocalization
    {
        private static readonly Dictionary<LocalizedText.Language, string> Table =
            new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "Portal",
                [LocalizedText.Language.French] = "Portail",
                [LocalizedText.Language.Italian] = "Portale",
                [LocalizedText.Language.German] = "Portal",
                [LocalizedText.Language.SpanishSpain] = "Portal",
                [LocalizedText.Language.SpanishLatam] = "Portal",
                [LocalizedText.Language.BRPortuguese] = "Portal",
                [LocalizedText.Language.Russian] = "Портал",
                [LocalizedText.Language.Ukrainian] = "Портал",
                [LocalizedText.Language.SimplifiedChinese] = "传送门",
                [LocalizedText.Language.TraditionalChinese] = "傳送門",
                [LocalizedText.Language.Japanese] = "ポータル",
                [LocalizedText.Language.Korean] = "포털",
                [LocalizedText.Language.Polish] = "Portal",
                [LocalizedText.Language.Turkish] = "Portal",
            };

        internal static string Name =>
            Table.TryGetValue(LocalizedText.CURRENT_LANGUAGE, out string name) ? name : Table[LocalizedText.Language.English];
    }
}
