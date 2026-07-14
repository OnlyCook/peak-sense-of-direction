using System;
using System.Text.RegularExpressions;

namespace SenseOfDirection.Ui.Localization
{
    /// <summary>
    /// Resolves the two placeholder forms a translated description can embed to
    /// reference another piece of this menu's own UI, rather than spelling out
    /// its English identifier literally:
    ///
    /// <list type="bullet">
    /// <item><c>{key:Section/key-name}</c> - that config entry's own current-
    /// language display name, e.g. <c>{key:Player-Labels/toggle-key}</c>
    /// becomes "UMSCHALTTASTE" in German, exactly what that row's own label
    /// reads as. See <see cref="ConfigLocalizationTable"/>.</item>
    /// <item><c>{enumval:EnumTypeName.Value}</c> - that enum value's own
    /// current-language dropdown text, e.g. <c>{enumval:LabelDisplayMode.Toggle}</c>
    /// becomes "UMSCHALTEN" in German, exactly what that dropdown option reads
    /// as. See <see cref="EnumLocalizationTable"/>.</item>
    /// </list>
    ///
    /// Why this exists at all: a description that names another setting or enum
    /// value has to name the thing the player actually sees on screen. A
    /// translated sentence that keeps the literal English identifier ("press
    /// toggle-key" or "AlwaysOn: labels are always visible") reads as a foreign
    /// word dropped into an otherwise fully-translated sentence, because that
    /// identifier never actually appears anywhere in a non-English UI - the
    /// setting's own row is translated, the dropdown's own option is
    /// translated, just not the *reference* to either from inside prose. A
    /// placeholder resolved from the exact same tables that drive those rows/
    /// dropdowns can't drift out of sync with them, which a hand-translated
    /// literal reference reliably would the next time either side changed.
    ///
    /// Deliberately only used inside <see cref="ConfigLocalizationTable"/>
    /// content, resolved by <see cref="ConfigSettingNaming.Tooltip"/> - the
    /// canonical English descriptions in <c>PluginConfig.cs</c> (which also
    /// serve the raw <c>.cfg</c> file, where the literal kebab-case key/enum
    /// identifier actually is the correct and necessary thing to show, since
    /// that's exactly the text someone editing the file by hand would type)
    /// are never run through this and keep their literal references as-is.
    /// </summary>
    internal static class DescriptionPlaceholders
    {
        private static readonly Regex KeyPattern = new Regex(@"\{key:([^/}]+)/([^}]+)\}", RegexOptions.Compiled);
        private static readonly Regex EnumPattern = new Regex(@"\{enumval:([^.}]+)\.([^}]+)\}", RegexOptions.Compiled);

        internal static string Resolve(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            text = KeyPattern.Replace(text, ResolveKeyMatch);
            text = EnumPattern.Replace(text, ResolveEnumMatch);
            return text;
        }

        private static string ResolveKeyMatch(Match match)
        {
            string section = match.Groups[1].Value;
            string key = match.Groups[2].Value;

            return ConfigLocalizationTable.TryGet(section, key, out ConfigLocalizationEntry entry)
                ? entry.Name
                : key.Replace('-', ' ').ToUpperInvariant();
        }

        private static string ResolveEnumMatch(Match match)
        {
            string typeName = match.Groups[1].Value;
            string valueName = match.Groups[2].Value;

            string mechanicalFallback = Regex.Replace(valueName, "(?<!^)([A-Z])", " $1").ToUpperInvariant();
            return EnumLocalizationTable.TryGetType(typeName, out Type enumType)
                ? EnumLocalizationTable.Get(enumType, valueName, mechanicalFallback)
                : mechanicalFallback;
        }
    }
}
