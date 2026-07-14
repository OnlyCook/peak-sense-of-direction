using System.Collections.Generic;
using BepInEx.Configuration;
using SenseOfDirection.Ui.Localization.Config;

namespace SenseOfDirection.Ui.Localization
{
    /// <summary>
    /// Every config entry's localized display name/description, keyed by
    /// (BepInEx section, key) - which is always unique together even though a
    /// key alone repeats across sections (<c>show-distance</c> exists in four
    /// of them).
    ///
    /// Deliberately modular: one file per config section under
    /// <see cref="Config"/>, each registering only its own keys. Adding a new
    /// config entry's translations means adding one call in that section's own
    /// file - never touching this one - and adding a whole new section means
    /// one new file plus one new line in <see cref="Build"/>.
    /// </summary>
    internal static class ConfigLocalizationTable
    {
        /// <summary>Handed to every section's <c>Register</c> method - the only thing those files are allowed to touch.</summary>
        internal sealed class Registry
        {
            internal readonly Dictionary<(string Section, string Key), Dictionary<LocalizedText.Language, ConfigLocalizationEntry>> Entries =
                new Dictionary<(string, string), Dictionary<LocalizedText.Language, ConfigLocalizationEntry>>();

            /// <summary>Registers one config key's full per-language table.</summary>
            internal void Add(string section, string key, Dictionary<LocalizedText.Language, ConfigLocalizationEntry> perLanguage)
            {
                Entries[(section, key)] = perLanguage;
            }
        }

        private static readonly Registry _registry = Build();

        private static Registry Build()
        {
            var registry = new Registry();

            GeneralConfigLocalization.Register(registry);
            FontsConfigLocalization.Register(registry);
            PlayerLabelsConfigLocalization.Register(registry);
            CampfireConfigLocalization.Register(registry);
            PingsConfigLocalization.Register(registry);
            PingAudioConfigLocalization.Register(registry);
            PingAntiSpamConfigLocalization.Register(registry);
            ItemPingsConfigLocalization.Register(registry);
            ItemPingDetectionConfigLocalization.Register(registry);
            CompassConfigLocalization.Register(registry);
            GhostFreeCamConfigLocalization.Register(registry);
            DebugConfigLocalization.Register(registry);

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
