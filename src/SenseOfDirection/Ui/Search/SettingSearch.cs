using System.Globalization;
using System.Text;
using BepInEx.Configuration;
using SenseOfDirection.Ui.Localization;

namespace SenseOfDirection.Ui.Search
{
    /// <summary>
    /// Matching for the quick setup panel's search tab, normalized the same way
    /// peak-item-spawner's search box is: case, whitespace, hyphens/dashes,
    /// apostrophes and diacritics are all ignored, so "luggage ping",
    /// "luggage-ping" and "luggageping" are one query, and "a" finds "ä".
    /// </summary>
    internal static class SettingSearch
    {
        /// <summary>
        /// Everything a setting can be found by, each form on its own line: its
        /// current-language name, its English name (so English works in every
        /// language) and its config key, plus pinyin forms of the name when the
        /// game is in Chinese.
        /// </summary>
        internal static string Blob(ConfigEntryBase entry)
        {
            string name = ConfigSettingNaming.DisplayName(entry);
            var sb = new StringBuilder(128);
            sb.Append(Normalize(name));

            if (ConfigLocalizationTable.TryGetEnglishName(entry.Definition.Section, entry.Definition.Key, out string english))
            {
                sb.Append('\n').Append(Normalize(english));
            }

            sb.Append('\n').Append(Normalize(entry.Definition.Key));
            PinyinIndex.Append(name, sb);
            return sb.ToString();
        }

        internal static bool Matches(string blob, string normalizedQuery) =>
            normalizedQuery.Length == 0 || blob.IndexOf(normalizedQuery, System.StringComparison.Ordinal) >= 0;

        /// <summary>
        /// Lowercased first, so a capital's own decomposition (Turkish "İ" ->
        /// "i" + combining dot) loses its mark in the same pass as every other
        /// diacritic.
        /// </summary>
        internal static string Normalize(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            s = s.ToLowerInvariant()
                .Replace("ß", "ss").Replace("ø", "o").Replace("æ", "ae").Replace("œ", "oe")
                .Replace("đ", "d").Replace("ł", "l").Replace("ı", "i");

            string nfd;
            try
            {
                nfd = s.Normalize(NormalizationForm.FormD);
            }
            catch
            {
                nfd = s;
            }

            var sb = new StringBuilder(nfd.Length);
            foreach (char c in nfd)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark ||
                    char.IsWhiteSpace(c) ||
                    c == '-' || c == '‐' || c == '–' || c == '—' || c == '_' ||
                    c == '\'' || c == '’' || c == '‘' || c == '`' || c == '´')
                {
                    continue;
                }
                sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
