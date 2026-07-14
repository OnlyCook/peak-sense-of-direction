using System;
using System.Collections.Generic;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
using SenseOfDirection.ItemPings;
using SenseOfDirection.Labels;
using SenseOfDirection.Ui.Localization.Enums;
using Zorro.Settings;

namespace SenseOfDirection.Ui.Localization
{
    /// <summary>
    /// Every config enum's per-value display text, keyed by (enum type, value
    /// name) - the enum-value equivalent of <see cref="ConfigLocalizationTable"/>,
    /// same one-file-per-enum modularity under <see cref="Enums"/>.
    ///
    /// Covers every enum a dropdown in the preview menu can show, including
    /// <c>OffOnMode</c> (Zorro's own OFF/ON enum, borrowed for every
    /// <c>ConfigEntry&lt;bool&gt;</c> - see <see cref="ConfigBoolSetting"/>) so
    /// a boolean setting's dropdown is exactly as localized as everything else.
    /// </summary>
    internal static class EnumLocalizationTable
    {
        internal sealed class Registry
        {
            internal readonly Dictionary<(Type EnumType, string ValueName), Dictionary<LocalizedText.Language, string>> Entries =
                new Dictionary<(Type, string), Dictionary<LocalizedText.Language, string>>();

            internal void Add(Type enumType, string valueName, Dictionary<LocalizedText.Language, string> perLanguage)
            {
                Entries[(enumType, valueName)] = perLanguage;
            }
        }

        private static readonly Registry _registry = Build();

        private static Registry Build()
        {
            var registry = new Registry();

            IndicatorPlacementLocalization.Register(registry);
            LabelDisplayModeLocalization.Register(registry);
            ItemPingNameModeLocalization.Register(registry);
            CompassLineColorLocalization.Register(registry);
            OffOnModeLocalization.Register(registry);

            return registry;
        }

        /// <summary>The current-language text for one enum value, or <paramref name="fallback"/> if no translation is registered for it at all.</summary>
        internal static string Get(Type enumType, string valueName, string fallback)
        {
            if (_registry.Entries.TryGetValue((enumType, valueName), out var perLanguage))
            {
                if (perLanguage.TryGetValue(LocalizedText.CURRENT_LANGUAGE, out string value))
                {
                    return value;
                }
                if (perLanguage.TryGetValue(LocalizedText.Language.English, out value))
                {
                    return value;
                }
            }

            return fallback;
        }

        /// <summary>Every enum type a description can reference via a <c>{enumval:TypeName.Value}</c> placeholder - see <see cref="DescriptionPlaceholders"/>.</summary>
        private static readonly Dictionary<string, Type> TypesByName = new Dictionary<string, Type>
        {
            ["IndicatorPlacement"] = typeof(IndicatorPlacement),
            ["LabelDisplayMode"] = typeof(LabelDisplayMode),
            ["ItemPingNameMode"] = typeof(ItemPingNameMode),
            ["CompassLineColor"] = typeof(CompassLineColor),
            ["OffOnMode"] = typeof(OffOnMode),
        };

        internal static bool TryGetType(string shortName, out Type enumType) => TypesByName.TryGetValue(shortName, out enumType);
    }
}
