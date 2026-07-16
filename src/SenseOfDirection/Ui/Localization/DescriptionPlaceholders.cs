using System;
using System.Text.RegularExpressions;
using UnityEngine.InputSystem;

namespace SenseOfDirection.Ui.Localization
{
    /// <summary>
    /// Resolves the placeholder forms a translated description can embed to
    /// reference another piece of this menu's own UI (or the game's own
    /// input bindings), rather than spelling out its English identifier
    /// literally:
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
    /// <item><c>{pingkey}</c> - the vanilla <c>Ping</c> input action's own
    /// current binding, read live off <c>InputSystem.actions</c> so a
    /// rebind in PEAK's own controls menu is reflected here too, with no
    /// language-specific text of its own to keep in sync (button/key names
    /// are rendered by Unity's Input System, not by this mod).</item>
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
        private static readonly Regex PingKeyPattern = new Regex(@"\{pingkey\}", RegexOptions.Compiled);

        private static InputAction _pingAction;

        internal static string Resolve(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            text = KeyPattern.Replace(text, ResolveKeyMatch);
            text = EnumPattern.Replace(text, ResolveEnumMatch);
            text = PingKeyPattern.Replace(text, ResolvePingKeyMatch);
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

        /// <summary>
        /// Not cached beyond the found <see cref="InputAction"/> itself - the
        /// action's *binding* can change at any time (PEAK's own controls
        /// menu), so the display string has to be read fresh every call, same
        /// as <see cref="Ui.PreviewPingMarker.PingKeyWasPressed"/> reads the
        /// action's controls fresh every frame rather than caching a result.
        /// </summary>
        private static string ResolvePingKeyMatch(Match match)
        {
            _pingAction ??= InputSystem.actions?.FindAction("Ping");
            string display = _pingAction?.GetBindingDisplayString();
            return string.IsNullOrEmpty(display) ? "PING" : display.ToUpperInvariant();
        }
    }
}
