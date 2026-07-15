using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace SenseOfDirection.Ui.Localization
{
    /// <summary>
    /// Reads one of the mod's tab-separated localization tables (authored as
    /// <c>Localization/*.tsv</c>, human-editable, one row per language - see the
    /// .csproj's <c>CompressLocalizationTsv</c> target) back out of its
    /// gzip-compressed embedded resource.
    ///
    /// Why this exists at all: ~1,450 translated rows compiled as literal
    /// <c>Dictionary&lt;Language, ...&gt;</c> entries (a constructor call plus a
    /// dictionary-add per row, times every row) cost far more in the DLL than
    /// the strings themselves ever did - a plain text table, compressed, is a
    /// fraction of the size for the exact same content. See
    /// <see cref="ConfigLocalizationTable"/>, <see cref="EnumLocalizationTable"/>
    /// and <see cref="PreviewMenuLocalization"/>, which all parse their own
    /// table through this at startup instead.
    /// </summary>
    internal static class LocalizationResource
    {
        internal static IEnumerable<string[]> ReadRows(string fileName)
        {
            string resourceName = "SenseOfDirection.Localization." + fileName + ".tsv.gz";
            using Stream compressed = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (compressed == null)
            {
                yield break;
            }

            using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
            using var reader = new StreamReader(gzip, Encoding.UTF8);

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Length == 0)
                {
                    continue;
                }

                yield return line.Split('\t');
            }
        }
    }
}
