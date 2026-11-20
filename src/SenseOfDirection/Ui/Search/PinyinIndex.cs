using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace SenseOfDirection.Ui.Search
{
    /// <summary>
    /// Toneless hanzi -> pinyin lookup, so Chinese players can search setting
    /// names by pinyin (most type pinyin, not hanzi). Same data and approach as
    /// peak-item-spawner's own PinyinIndex: a gzipped embedded blob of
    /// "&lt;hanzi&gt;&lt;ascii-pinyin&gt;\n" lines, one primary reading per
    /// character (Unihan kMandarin, BMP CJK only).
    ///
    /// Only ever loaded when the game language is Chinese
    /// (<see cref="IsChineseLanguage"/>) - nobody else needs the ~20k-entry
    /// table in memory.
    /// </summary>
    internal static class PinyinIndex
    {
        private static Dictionary<char, string> _map;

        internal static bool IsChineseLanguage =>
            LocalizedText.CURRENT_LANGUAGE == LocalizedText.Language.SimplifiedChinese ||
            LocalizedText.CURRENT_LANGUAGE == LocalizedText.Language.TraditionalChinese;

        private static void EnsureLoaded()
        {
            if (_map != null)
            {
                return;
            }

            _map = new Dictionary<char, string>();
            try
            {
                using Stream raw = Assembly.GetExecutingAssembly().GetManifestResourceStream("SenseOfDirection.pinyin.bin");
                if (raw == null)
                {
                    return;
                }

                using var gz = new GZipStream(raw, CompressionMode.Decompress);
                using var reader = new StreamReader(gz, Encoding.UTF8);
                var map = new Dictionary<char, string>(21000);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length >= 2)
                    {
                        map[line[0]] = line.Substring(1);
                    }
                }
                _map = map;
            }
            catch (Exception e)
            {
                Plugin.Instance?.Log.LogWarning($"PinyinIndex: failed to load the pinyin table ({e.Message}); pinyin search disabled.");
            }
        }

        private static bool HasHan(string s)
        {
            foreach (char c in s)
            {
                if (c >= 0x4E00 && c <= 0x9FFF)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Appends the full pinyin, a v->u variant and the syllable initials of
        /// <paramref name="name"/> to <paramref name="blob"/>, each on its own
        /// line so a query only ever matches within one form.
        /// </summary>
        internal static void Append(string name, StringBuilder blob)
        {
            if (string.IsNullOrEmpty(name) || !IsChineseLanguage || !HasHan(name))
            {
                return;
            }

            EnsureLoaded();
            if (_map.Count == 0)
            {
                return;
            }

            var full = new StringBuilder(name.Length * 4);
            var initials = new StringBuilder(name.Length);
            foreach (char raw in name)
            {
                char c = char.ToLowerInvariant(raw);
                if (_map.TryGetValue(c, out string py))
                {
                    full.Append(py);
                    initials.Append(py[0]);
                }
                else if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
                {
                    full.Append(c);
                    initials.Append(c);
                }
            }

            if (full.Length == 0)
            {
                return;
            }

            string f = full.ToString();
            blob.Append('\n').Append(f);
            if (f.IndexOf('v') >= 0)
            {
                blob.Append('\n').Append(f.Replace('v', 'u'));
            }
            blob.Append('\n').Append(initials);
        }
    }
}
