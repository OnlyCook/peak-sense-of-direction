using System;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace SenseOfDirection.Ui.Localization
{
    /// <summary>
    /// Every config entry's localized display name/description, keyed by
    /// (BepInEx section, key) - which is always unique together even though a
    /// key alone repeats across sections (<c>show-distance</c> exists in four
    /// of them).
    ///
    /// Loaded from <c>Localization/config.tsv</c> (one row per section/key/
    /// language: <c>Section\tKey\tLanguage\tName\tDescription</c>) via
    /// <see cref="LocalizationResource"/> - see that class for why this isn't
    /// generated C# any more. Adding a new config entry's translations means
    /// adding 15 rows to that file, nothing here.
    /// </summary>
    internal static class ConfigLocalizationTable
    {
        internal sealed class Registry
        {
            internal readonly Dictionary<(string Section, string Key), Dictionary<LocalizedText.Language, ConfigLocalizationEntry>> Entries =
                new Dictionary<(string, string), Dictionary<LocalizedText.Language, ConfigLocalizationEntry>>();
        }

        private static readonly Registry _registry = Build();

        private static Registry Build()
        {
            var registry = new Registry();

            foreach (string[] row in LocalizationResource.ReadRows("config"))
            {
                // Section, Key, Language, Name, Description
                if (row.Length != 5 || !Enum.TryParse(row[2], out LocalizedText.Language language))
                {
                    continue;
                }

                var entryKey = (row[0], row[1]);
                if (!registry.Entries.TryGetValue(entryKey, out var perLanguage))
                {
                    perLanguage = new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>();
                    registry.Entries[entryKey] = perLanguage;
                }

                perLanguage[language] = new ConfigLocalizationEntry(row[3], row[4]);
            }

            return registry;
        }

        /// <summary>
        /// The current-language entry for <paramref name="entry"/>, falling back
        /// to English if the current language has no translation yet, and false
        /// if the key itself was never registered at all (a config entry added
        /// without updating its section's localization file - the caller falls
        /// back further, to the mechanical key-derived name/raw description).
        /// </summary>
        internal static bool TryGet(ConfigEntryBase entry, out ConfigLocalizationEntry result) =>
            TryGet(entry.Definition.Section, entry.Definition.Key, out result);

        /// <summary>
        /// Same lookup as <see cref="TryGet(ConfigEntryBase, out ConfigLocalizationEntry)"/>,
        /// by raw section/key rather than a live <see cref="ConfigEntryBase"/> -
        /// what <see cref="DescriptionPlaceholders"/> uses to resolve a
        /// <c>{key:Section/key}</c> token in another entry's own description
        /// into that key's actual current-language display name.
        /// </summary>
        internal static bool TryGet(string section, string key, out ConfigLocalizationEntry result)
        {
            result = default;
            if (!_registry.Entries.TryGetValue((section, key), out var perLanguage))
            {
                return false;
            }

            if (perLanguage.TryGetValue(LocalizedText.CURRENT_LANGUAGE, out result))
            {
                return true;
            }

            return perLanguage.TryGetValue(LocalizedText.Language.English, out result);
        }
    }
}
