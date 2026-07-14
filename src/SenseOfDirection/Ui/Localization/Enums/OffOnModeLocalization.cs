using System.Collections.Generic;
using Zorro.Settings;

namespace SenseOfDirection.Ui.Localization.Enums
{
    /// <summary>
    /// Localized dropdown text for Zorro's own <see cref="OffOnMode"/> - every
    /// <c>ConfigEntry&lt;bool&gt;</c> in this mod is backed by one (see
    /// <see cref="ConfigBoolSetting"/>), so this is what makes every boolean
    /// setting's dropdown read ON/OFF in the player's own language instead of
    /// always English.
    /// </summary>
    internal static class OffOnModeLocalization
    {
        internal static void Register(EnumLocalizationTable.Registry registry)
        {
            void Add(string value, Dictionary<LocalizedText.Language, string> table) =>
                registry.Add(typeof(OffOnMode), value, table);

            Add("OFF", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "OFF",
                [LocalizedText.Language.French] = "DÉSACTIVÉ",
                [LocalizedText.Language.Italian] = "DISATTIVATO",
                [LocalizedText.Language.German] = "AUS",
                [LocalizedText.Language.SpanishSpain] = "DESACTIVADO",
                [LocalizedText.Language.SpanishLatam] = "DESACTIVADO",
                [LocalizedText.Language.BRPortuguese] = "DESATIVADO",
                [LocalizedText.Language.Russian] = "ВЫКЛ",
                [LocalizedText.Language.Ukrainian] = "ВИМК",
                [LocalizedText.Language.SimplifiedChinese] = "关",
                [LocalizedText.Language.TraditionalChinese] = "關",
                [LocalizedText.Language.Japanese] = "オフ",
                [LocalizedText.Language.Korean] = "꺼짐",
                [LocalizedText.Language.Polish] = "WYŁ.",
                [LocalizedText.Language.Turkish] = "KAPALI",
            });

            Add("ON", new Dictionary<LocalizedText.Language, string>
            {
                [LocalizedText.Language.English] = "ON",
                [LocalizedText.Language.French] = "ACTIVÉ",
                [LocalizedText.Language.Italian] = "ATTIVATO",
                [LocalizedText.Language.German] = "AN",
                [LocalizedText.Language.SpanishSpain] = "ACTIVADO",
                [LocalizedText.Language.SpanishLatam] = "ACTIVADO",
                [LocalizedText.Language.BRPortuguese] = "ATIVADO",
                [LocalizedText.Language.Russian] = "ВКЛ",
                [LocalizedText.Language.Ukrainian] = "УВІМК",
                [LocalizedText.Language.SimplifiedChinese] = "开",
                [LocalizedText.Language.TraditionalChinese] = "開",
                [LocalizedText.Language.Japanese] = "オン",
                [LocalizedText.Language.Korean] = "켜짐",
                [LocalizedText.Language.Polish] = "WŁ.",
                [LocalizedText.Language.Turkish] = "AÇIK",
            });
        }
    }
}
