using System.Collections.Generic;
using SenseOfDirection.Indicators;

namespace SenseOfDirection.Ui.Localization.Enums
{
    /// <summary>Localized dropdown text for <see cref="IndicatorPlacement"/> - shared by every <c>*-placement</c> setting.</summary>
    internal static class IndicatorPlacementLocalization
    {
        internal static void Register(EnumLocalizationTable.Registry registry)
        {
            void Add(string value, Dictionary<LocalizedText.Language, string> table) =>
                registry.Add(typeof(IndicatorPlacement), value, table);

            Add("OffScreenOnly", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "OFF-SCREEN ONLY",
                [LocalizedText.Language.French] = "BORD D'ÉCRAN UNIQUEMENT",
                [LocalizedText.Language.Italian] = "SOLO BORDO SCHERMO",
                [LocalizedText.Language.German] = "NUR BILDSCHIRMRAND",
                [LocalizedText.Language.SpanishSpain] = "SOLO BORDE DE PANTALLA",
                [LocalizedText.Language.SpanishLatam] = "SOLO BORDE DE PANTALLA",
                [LocalizedText.Language.BRPortuguese] = "SOMENTE BORDA DA TELA",
                [LocalizedText.Language.Russian] = "ТОЛЬКО КРАЙ ЭКРАНА",
                [LocalizedText.Language.Ukrainian] = "ТІЛЬКИ КРАЙ ЕКРАНА",
                [LocalizedText.Language.SimplifiedChinese] = "仅屏幕边缘",
                [LocalizedText.Language.TraditionalChinese] = "僅螢幕邊緣",
                [LocalizedText.Language.Japanese] = "画面端のみ",
                [LocalizedText.Language.Korean] = "화면 가장자리만",
                [LocalizedText.Language.Polish] = "TYLKO KRAWĘDŹ EKRANU",
                [LocalizedText.Language.Turkish] = "SADECE EKRAN KENARI",
            });

            Add("CompassOnly", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "COMPASS ONLY",
                [LocalizedText.Language.French] = "BOUSSOLE UNIQUEMENT",
                [LocalizedText.Language.Italian] = "SOLO BUSSOLA",
                [LocalizedText.Language.German] = "NUR KOMPASS",
                [LocalizedText.Language.SpanishSpain] = "SOLO BRÚJULA",
                [LocalizedText.Language.SpanishLatam] = "SOLO BRÚJULA",
                [LocalizedText.Language.BRPortuguese] = "SOMENTE BÚSSOLA",
                [LocalizedText.Language.Russian] = "ТОЛЬКО КОМПАС",
                [LocalizedText.Language.Ukrainian] = "ТІЛЬКИ КОМПАС",
                [LocalizedText.Language.SimplifiedChinese] = "仅指南针",
                [LocalizedText.Language.TraditionalChinese] = "僅指南針",
                [LocalizedText.Language.Japanese] = "コンパスのみ",
                [LocalizedText.Language.Korean] = "나침반만",
                [LocalizedText.Language.Polish] = "TYLKO KOMPAS",
                [LocalizedText.Language.Turkish] = "SADECE PUSULA",
            });

            Add("Both", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "BOTH",
                [LocalizedText.Language.French] = "LES DEUX",
                [LocalizedText.Language.Italian] = "ENTRAMBI",
                [LocalizedText.Language.German] = "BEIDE",
                [LocalizedText.Language.SpanishSpain] = "AMBOS",
                [LocalizedText.Language.SpanishLatam] = "AMBOS",
                [LocalizedText.Language.BRPortuguese] = "AMBOS",
                [LocalizedText.Language.Russian] = "ОБА",
                [LocalizedText.Language.Ukrainian] = "ОБИДВА",
                [LocalizedText.Language.SimplifiedChinese] = "两者都",
                [LocalizedText.Language.TraditionalChinese] = "兩者皆",
                [LocalizedText.Language.Japanese] = "両方",
                [LocalizedText.Language.Korean] = "둘 다",
                [LocalizedText.Language.Polish] = "OBA",
                [LocalizedText.Language.Turkish] = "İKİSİ DE",
            });
        }
    }
}
