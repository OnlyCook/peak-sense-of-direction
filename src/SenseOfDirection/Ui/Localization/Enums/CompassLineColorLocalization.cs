using System.Collections.Generic;
using SenseOfDirection.Compass;

namespace SenseOfDirection.Ui.Localization.Enums
{
    /// <summary>Localized dropdown text for <see cref="CompassLineColor"/>.</summary>
    internal static class CompassLineColorLocalization
    {
        internal static void Register(EnumLocalizationTable.Registry registry)
        {
            void Add(string value, Dictionary<LocalizedText.Language, string> table) =>
                registry.Add(typeof(CompassLineColor), value, table);

            Add("White", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "WHITE",
                [LocalizedText.Language.French] = "BLANC",
                [LocalizedText.Language.Italian] = "BIANCO",
                [LocalizedText.Language.German] = "WEISS",
                [LocalizedText.Language.SpanishSpain] = "BLANCO",
                [LocalizedText.Language.SpanishLatam] = "BLANCO",
                [LocalizedText.Language.BRPortuguese] = "BRANCO",
                [LocalizedText.Language.Russian] = "БЕЛЫЙ",
                [LocalizedText.Language.Ukrainian] = "БІЛИЙ",
                [LocalizedText.Language.SimplifiedChinese] = "白色",
                [LocalizedText.Language.TraditionalChinese] = "白色",
                [LocalizedText.Language.Japanese] = "白",
                [LocalizedText.Language.Korean] = "흰색",
                [LocalizedText.Language.Polish] = "BIAŁY",
                [LocalizedText.Language.Turkish] = "BEYAZ",
            });

            Add("LightGray", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "LIGHT GRAY",
                [LocalizedText.Language.French] = "GRIS CLAIR",
                [LocalizedText.Language.Italian] = "GRIGIO CHIARO",
                [LocalizedText.Language.German] = "HELLGRAU",
                [LocalizedText.Language.SpanishSpain] = "GRIS CLARO",
                [LocalizedText.Language.SpanishLatam] = "GRIS CLARO",
                [LocalizedText.Language.BRPortuguese] = "CINZA CLARO",
                [LocalizedText.Language.Russian] = "СВЕТЛО-СЕРЫЙ",
                [LocalizedText.Language.Ukrainian] = "СВІТЛО-СІРИЙ",
                [LocalizedText.Language.SimplifiedChinese] = "浅灰色",
                [LocalizedText.Language.TraditionalChinese] = "淺灰色",
                [LocalizedText.Language.Japanese] = "明るいグレー",
                [LocalizedText.Language.Korean] = "밝은 회색",
                [LocalizedText.Language.Polish] = "JASNOSZARY",
                [LocalizedText.Language.Turkish] = "AÇIK GRİ",
            });

            Add("Gray", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "GRAY",
                [LocalizedText.Language.French] = "GRIS",
                [LocalizedText.Language.Italian] = "GRIGIO",
                [LocalizedText.Language.German] = "GRAU",
                [LocalizedText.Language.SpanishSpain] = "GRIS",
                [LocalizedText.Language.SpanishLatam] = "GRIS",
                [LocalizedText.Language.BRPortuguese] = "CINZA",
                [LocalizedText.Language.Russian] = "СЕРЫЙ",
                [LocalizedText.Language.Ukrainian] = "СІРИЙ",
                [LocalizedText.Language.SimplifiedChinese] = "灰色",
                [LocalizedText.Language.TraditionalChinese] = "灰色",
                [LocalizedText.Language.Japanese] = "グレー",
                [LocalizedText.Language.Korean] = "회색",
                [LocalizedText.Language.Polish] = "SZARY",
                [LocalizedText.Language.Turkish] = "GRİ",
            });

            Add("DarkGray", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "DARK GRAY",
                [LocalizedText.Language.French] = "GRIS FONCÉ",
                [LocalizedText.Language.Italian] = "GRIGIO SCURO",
                [LocalizedText.Language.German] = "DUNKELGRAU",
                [LocalizedText.Language.SpanishSpain] = "GRIS OSCURO",
                [LocalizedText.Language.SpanishLatam] = "GRIS OSCURO",
                [LocalizedText.Language.BRPortuguese] = "CINZA ESCURO",
                [LocalizedText.Language.Russian] = "ТЁМНО-СЕРЫЙ",
                [LocalizedText.Language.Ukrainian] = "ТЕМНО-СІРИЙ",
                [LocalizedText.Language.SimplifiedChinese] = "深灰色",
                [LocalizedText.Language.TraditionalChinese] = "深灰色",
                [LocalizedText.Language.Japanese] = "濃いグレー",
                [LocalizedText.Language.Korean] = "진한 회색",
                [LocalizedText.Language.Polish] = "CIEMNOSZARY",
                [LocalizedText.Language.Turkish] = "KOYU GRİ",
            });

            Add("Black", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "BLACK",
                [LocalizedText.Language.French] = "NOIR",
                [LocalizedText.Language.Italian] = "NERO",
                [LocalizedText.Language.German] = "SCHWARZ",
                [LocalizedText.Language.SpanishSpain] = "NEGRO",
                [LocalizedText.Language.SpanishLatam] = "NEGRO",
                [LocalizedText.Language.BRPortuguese] = "PRETO",
                [LocalizedText.Language.Russian] = "ЧЁРНЫЙ",
                [LocalizedText.Language.Ukrainian] = "ЧОРНИЙ",
                [LocalizedText.Language.SimplifiedChinese] = "黑色",
                [LocalizedText.Language.TraditionalChinese] = "黑色",
                [LocalizedText.Language.Japanese] = "黒",
                [LocalizedText.Language.Korean] = "검은색",
                [LocalizedText.Language.Polish] = "CZARNY",
                [LocalizedText.Language.Turkish] = "SİYAH",
            });
        }
    }
}
