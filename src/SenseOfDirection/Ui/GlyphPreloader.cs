using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SenseOfDirection.Labels;
using TMPro;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// Adds every character the menu can ever show to the game's dynamic font atlases in one batch, before any
    /// of its text exists.
    ///
    /// Left to itself, TextMeshPro adds glyphs to a dynamic atlas one at a time as text first needs them, and
    /// each addition re-uploads the whole atlas texture. The fallback font used for CJK text has a 4096x4096
    /// atlas (~16 MB), so a few hundred new characters in one frame are a few GB of uploads, which DX12 on some
    /// NVIDIA setups keeps alive until the frame finishes - enough to run a machine out of memory and crash.
    /// One batch call uploads the atlas once instead.
    /// </summary>
    internal static class GlyphPreloader
    {
        internal static void Preload(IEnumerable<string> texts)
        {
            NativeAssets.TryFindAll();
            TMP_FontAsset font = NativeAssets.Font;
            if (font == null)
            {
                return;
            }

            var seen = new HashSet<int>();
            var characters = new StringBuilder();
            foreach (string text in texts)
            {
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                for (int i = 0; i < text.Length; i++)
                {
                    int codePoint = char.IsHighSurrogate(text[i]) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])
                        ? char.ConvertToUtf32(text[i], text[++i])
                        : text[i];

                    if (codePoint > ' ' && !char.IsControl((char)(codePoint & 0xFFFF)) && seen.Add(codePoint))
                    {
                        characters.Append(char.ConvertFromUtf32(codePoint));
                    }
                }
            }

            if (characters.Length == 0)
            {
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            string missing = characters.ToString();
            var visited = new HashSet<TMP_FontAsset>();

            // The order TMP itself resolves a glyph in: the menu font, then its fallbacks (depth first), then the global fallbacks.
            AddTo(font, ref missing, visited);
            if (TMP_Settings.fallbackFontAssets != null)
            {
                foreach (TMP_FontAsset fallback in TMP_Settings.fallbackFontAssets)
                {
                    AddTo(fallback, ref missing, visited);
                }
            }

            if (Plugin.Instance.Cfg.EnableDebugLogging.Value)
            {
                Plugin.Instance.Log.LogInfo(
                    $"GlyphPreloader: preloaded {characters.Length - missing.Length} of {characters.Length} distinct characters in {stopwatch.ElapsedMilliseconds}ms.");
            }
        }

        private static void AddTo(TMP_FontAsset font, ref string missing, HashSet<TMP_FontAsset> visited)
        {
            if (font == null || missing.Length == 0 || !visited.Add(font))
            {
                return;
            }

            font.TryAddCharacters(missing, out missing);

            if (font.fallbackFontAssetTable == null)
            {
                return;
            }

            foreach (TMP_FontAsset fallback in font.fallbackFontAssetTable)
            {
                AddTo(fallback, ref missing, visited);
            }
        }
    }
}
