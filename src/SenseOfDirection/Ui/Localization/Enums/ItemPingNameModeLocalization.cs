using System.Collections.Generic;
using SenseOfDirection.ItemPings;

namespace SenseOfDirection.Ui.Localization.Enums
{
    /// <summary>Localized dropdown text for <see cref="ItemPingNameMode"/>.</summary>
    internal static class ItemPingNameModeLocalization
    {
        internal static void Register(EnumLocalizationTable.Registry registry)
        {
            void Add(string value, Dictionary<LocalizedText.Language, string> table) =>
                registry.Add(typeof(ItemPingNameMode), value, table);

            Add("Never", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "NEVER",
                [LocalizedText.Language.French] = "JAMAIS",
                [LocalizedText.Language.Italian] = "MAI",
                [LocalizedText.Language.German] = "NIE",
                [LocalizedText.Language.SpanishSpain] = "NUNCA",
                [LocalizedText.Language.SpanishLatam] = "NUNCA",
                [LocalizedText.Language.BRPortuguese] = "NUNCA",
                [LocalizedText.Language.Russian] = "НИКОГДА",
                [LocalizedText.Language.Ukrainian] = "НІКОЛИ",
                [LocalizedText.Language.SimplifiedChinese] = "从不",
                [LocalizedText.Language.TraditionalChinese] = "從不",
                [LocalizedText.Language.Japanese] = "表示しない",
                [LocalizedText.Language.Korean] = "표시 안 함",
                [LocalizedText.Language.Polish] = "NIGDY",
                [LocalizedText.Language.Turkish] = "ASLA",
            });

            Add("HideWhenIconShown", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "HIDE WHEN ICON SHOWN",
                [LocalizedText.Language.French] = "MASQUER SI ICÔNE AFFICHÉE",
                [LocalizedText.Language.Italian] = "NASCONDI SE ICONA VISIBILE",
                [LocalizedText.Language.German] = "AUSBLENDEN BEI SYMBOL",
                [LocalizedText.Language.SpanishSpain] = "OCULTAR SI HAY ICONO",
                [LocalizedText.Language.SpanishLatam] = "OCULTAR SI HAY ÍCONO",
                [LocalizedText.Language.BRPortuguese] = "OCULTAR QUANDO ÍCONE VISÍVEL",
                [LocalizedText.Language.Russian] = "СКРЫВАТЬ ПРИ ЗНАЧКЕ",
                [LocalizedText.Language.Ukrainian] = "ХОВАТИ ПРИ ЗНАЧКУ",
                [LocalizedText.Language.SimplifiedChinese] = "显示图标时隐藏",
                [LocalizedText.Language.TraditionalChinese] = "顯示圖示時隱藏",
                [LocalizedText.Language.Japanese] = "アイコン表示時は非表示",
                [LocalizedText.Language.Korean] = "아이콘 표시 시 숨김",
                [LocalizedText.Language.Polish] = "UKRYJ, GDY IKONA WIDOCZNA",
                [LocalizedText.Language.Turkish] = "SİMGE GÖSTERİLİNCE GİZLE",
            });

            Add("Always", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "ALWAYS",
                [LocalizedText.Language.French] = "TOUJOURS",
                [LocalizedText.Language.Italian] = "SEMPRE",
                [LocalizedText.Language.German] = "IMMER",
                [LocalizedText.Language.SpanishSpain] = "SIEMPRE",
                [LocalizedText.Language.SpanishLatam] = "SIEMPRE",
                [LocalizedText.Language.BRPortuguese] = "SEMPRE",
                [LocalizedText.Language.Russian] = "ВСЕГДА",
                [LocalizedText.Language.Ukrainian] = "ЗАВЖДИ",
                [LocalizedText.Language.SimplifiedChinese] = "始终",
                [LocalizedText.Language.TraditionalChinese] = "始終",
                [LocalizedText.Language.Japanese] = "常に表示",
                [LocalizedText.Language.Korean] = "항상 표시",
                [LocalizedText.Language.Polish] = "ZAWSZE",
                [LocalizedText.Language.Turkish] = "HER ZAMAN",
            });
        }
    }
}
