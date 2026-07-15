using System.Collections.Generic;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// Translated chrome copy for <see cref="PreviewMenu"/> and
    /// <see cref="NativeSettingCells"/> - keyed off the game's own
    /// <c>LocalizedText.CURRENT_LANGUAGE</c>, same pattern as
    /// <see cref="GhostFreeCam.GhostFreeCamLocalization"/>. Community-sourced,
    /// not professionally reviewed - good enough for short UI chrome, easy to
    /// correct/extend per-language later.
    ///
    /// Scoped to the menu's own chrome (title, tabs, footer, placeholder,
    /// loading/rebind copy, the preview scene's luggage fallback name) - the
    /// 82 config entries' own descriptions/display names are a separate,
    /// much larger table, see <see cref="Localization.ConfigLocalizationTable"/>.
    /// Loaded from <c>Localization/chrome.tsv</c> via
    /// <see cref="Localization.LocalizationResource"/>.
    /// </summary>
    internal static class PreviewMenuLocalization
    {
        internal struct Strings
        {
            public readonly string QuickSetup;
            public readonly string TabPlayerLabels;
            public readonly string TabPings;
            public readonly string TabItemPings;
            public readonly string TabCampfire;
            public readonly string TabCompass;
            public readonly string TabGeneral;
            public readonly string DescriptionPlaceholder;
            public readonly string Loading;
            public readonly string FooterClose;
            public readonly string FooterChangesSaveInstantly;
            public readonly string PressAKey;
            public readonly string DefaultValuePrefix;

            /// <summary>
            /// The preview scene's luggage item-ping name. Unlike COCONUT/BACKPACK
            /// (which resolve off a loaded <c>Item</c> prefab's own
            /// <c>GetItemName()</c> - always available, prefabs stay loaded in
            /// memory regardless of the current run), luggage has no such prefab
            /// to fall back on: <see cref="PreviewScene.FindLuggageDisplayName"/>
            /// needs an actual <c>Luggage</c> instance placed in the currently
            /// loaded level, and a run with none left unopened (or none spawned
            /// yet) has nothing to ask. This is the fallback for exactly that
            /// case, so the preview always shows a translated name rather than
            /// silently reverting to English whenever a run happens to have no
            /// luggage sitting around.
            /// </summary>
            public readonly string LuggageName;

            public Strings(
                string quickSetup, string tabPlayerLabels, string tabPings, string tabItemPings,
                string tabCampfire, string tabCompass, string tabGeneral, string descriptionPlaceholder,
                string loading, string footerClose, string footerChangesSaveInstantly, string pressAKey,
                string defaultValuePrefix, string luggageName)
            {
                QuickSetup = quickSetup;
                TabPlayerLabels = tabPlayerLabels;
                TabPings = tabPings;
                TabItemPings = tabItemPings;
                TabCampfire = tabCampfire;
                TabCompass = tabCompass;
                TabGeneral = tabGeneral;
                DescriptionPlaceholder = descriptionPlaceholder;
                Loading = loading;
                FooterClose = footerClose;
                FooterChangesSaveInstantly = footerChangesSaveInstantly;
                PressAKey = pressAKey;
                DefaultValuePrefix = defaultValuePrefix;
                LuggageName = luggageName;
            }
        }

        private static readonly Dictionary<LocalizedText.Language, Strings> Table = Build();

        private static Dictionary<LocalizedText.Language, Strings> Build()
        {
            var table = new Dictionary<LocalizedText.Language, Strings>();

            foreach (string[] row in Localization.LocalizationResource.ReadRows("chrome"))
            {
                // Language, QuickSetup, TabPlayerLabels, TabPings, TabItemPings, TabCampfire,
                // TabCompass, TabGeneral, DescriptionPlaceholder, Loading, FooterClose,
                // FooterChangesSaveInstantly, PressAKey, DefaultValuePrefix, LuggageName
                if (row.Length != 15 || !System.Enum.TryParse(row[0], out LocalizedText.Language language))
                {
                    continue;
                }

                table[language] = new Strings(
                    row[1], row[2], row[3], row[4], row[5], row[6], row[7], row[8],
                    row[9], row[10], row[11], row[12], row[13], row[14]);
            }

            return table;
        }

        internal static Strings Current
        {
            get
            {
                if (!Table.TryGetValue(LocalizedText.CURRENT_LANGUAGE, out Strings strings))
                {
                    strings = Table[LocalizedText.Language.English];
                }
                return strings;
            }
        }
    }
}
