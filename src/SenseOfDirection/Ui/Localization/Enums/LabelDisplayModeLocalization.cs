using System.Collections.Generic;
using SenseOfDirection.Labels;

namespace SenseOfDirection.Ui.Localization.Enums
{
    /// <summary>Localized dropdown text for <see cref="LabelDisplayMode"/>.</summary>
    internal static class LabelDisplayModeLocalization
    {
        internal static void Register(EnumLocalizationTable.Registry registry)
        {
            void Add(string value, Dictionary<LocalizedText.Language, string> table) =>
                registry.Add(typeof(LabelDisplayMode), value, table);

            Add("Toggle", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "TOGGLE",
                [LocalizedText.Language.French] = "BASCULE",
                [LocalizedText.Language.Italian] = "ATTIVA/DISATTIVA",
                [LocalizedText.Language.German] = "UMSCHALTEN",
                [LocalizedText.Language.SpanishSpain] = "ALTERNAR",
                [LocalizedText.Language.SpanishLatam] = "ALTERNAR",
                [LocalizedText.Language.BRPortuguese] = "ALTERNAR",
                [LocalizedText.Language.Russian] = "ПЕРЕКЛЮЧЕНИЕ",
                [LocalizedText.Language.Ukrainian] = "ПЕРЕМИКАННЯ",
                [LocalizedText.Language.SimplifiedChinese] = "切换",
                [LocalizedText.Language.TraditionalChinese] = "切換",
                [LocalizedText.Language.Japanese] = "トグル",
                [LocalizedText.Language.Korean] = "전환",
                [LocalizedText.Language.Polish] = "PRZEŁĄCZANIE",
                [LocalizedText.Language.Turkish] = "AÇMA/KAPAMA",
            });

            Add("AlwaysOn", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "ALWAYS ON",
                [LocalizedText.Language.French] = "TOUJOURS ACTIVÉ",
                [LocalizedText.Language.Italian] = "SEMPRE ATTIVO",
                [LocalizedText.Language.German] = "IMMER AN",
                [LocalizedText.Language.SpanishSpain] = "SIEMPRE ACTIVO",
                [LocalizedText.Language.SpanishLatam] = "SIEMPRE ACTIVO",
                [LocalizedText.Language.BRPortuguese] = "SEMPRE ATIVO",
                [LocalizedText.Language.Russian] = "ВСЕГДА ВКЛ.",
                [LocalizedText.Language.Ukrainian] = "ЗАВЖДИ УВІМК.",
                [LocalizedText.Language.SimplifiedChinese] = "始终开启",
                [LocalizedText.Language.TraditionalChinese] = "始終開啟",
                [LocalizedText.Language.Japanese] = "常にオン",
                [LocalizedText.Language.Korean] = "항상 켜짐",
                [LocalizedText.Language.Polish] = "ZAWSZE WŁĄCZONE",
                [LocalizedText.Language.Turkish] = "HER ZAMAN AÇIK",
            });

            Add("Hold", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "HOLD",
                [LocalizedText.Language.French] = "MAINTENIR",
                [LocalizedText.Language.Italian] = "TIENI PREMUTO",
                [LocalizedText.Language.German] = "HALTEN",
                [LocalizedText.Language.SpanishSpain] = "MANTENER PULSADO",
                [LocalizedText.Language.SpanishLatam] = "MANTENER PRESIONADO",
                [LocalizedText.Language.BRPortuguese] = "SEGURAR",
                [LocalizedText.Language.Russian] = "УДЕРЖАНИЕ",
                [LocalizedText.Language.Ukrainian] = "УТРИМАННЯ",
                [LocalizedText.Language.SimplifiedChinese] = "按住",
                [LocalizedText.Language.TraditionalChinese] = "按住",
                [LocalizedText.Language.Japanese] = "長押し",
                [LocalizedText.Language.Korean] = "누르고 있기",
                [LocalizedText.Language.Polish] = "PRZYTRZYMANIE",
                [LocalizedText.Language.Turkish] = "BASILI TUTMA",
            });
        }
    }
}
