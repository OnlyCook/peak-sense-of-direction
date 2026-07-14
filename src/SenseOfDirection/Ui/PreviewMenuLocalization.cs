using System.Collections.Generic;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// Translated chrome copy for <see cref="PreviewMenu"/> and
    /// <see cref="NativeSettingCells"/> - keyed off the game's own
    /// <c>LocalizedText.CURRENT_LANGUAGE</c>, same pattern as
    /// <see cref="GhostFreeCam.GhostFreeCamLocalization"/>. Community-sourced,
    /// not professionally reviewed - good enough for short UI chrome, easy to
    /// correct/extend per-language later.
    ///
    /// Deliberately scoped to the menu's own chrome only (title, tabs, footer,
    /// placeholder, loading/rebind copy) - not the 82 config entries' own
    /// descriptions/display names, which stay English for now (same as the
    /// config file itself, which has never been localized).
    /// </summary>
    internal static class PreviewMenuLocalization
    {
        internal struct Strings
        {
            public readonly string QuickSetup;
            public readonly string TabPlayerLabels;
            public readonly string TabPings;
            public readonly string TabItemPings;
            public readonly string TabCampfire;
            public readonly string TabCompass;
            public readonly string TabGeneral;
            public readonly string DescriptionPlaceholder;
            public readonly string Loading;
            public readonly string FooterClose;
            public readonly string FooterChangesSaveInstantly;
            public readonly string PressAKey;
            public readonly string DefaultValuePrefix;

            /// <summary>
            /// The preview scene's luggage item-ping name. Unlike COCONUT/BACKPACK
            /// (which resolve off a loaded <c>Item</c> prefab's own
            /// <c>GetItemName()</c> - always available, prefabs stay loaded in
            /// memory regardless of the current run), luggage has no such prefab
            /// to fall back on: <see cref="PreviewScene.FindLuggageDisplayName"/>
            /// needs an actual <c>Luggage</c> instance placed in the currently
            /// loaded level, and a run with none left unopened (or none spawned
            /// yet) has nothing to ask. This is the fallback for exactly that
            /// case, so the preview always shows a translated name rather than
            /// silently reverting to English whenever a run happens to have no
            /// luggage sitting around.
            /// </summary>
            public readonly string LuggageName;

            public Strings(
                string quickSetup, string tabPlayerLabels, string tabPings, string tabItemPings,
                string tabCampfire, string tabCompass, string tabGeneral, string descriptionPlaceholder,
                string loading, string footerClose, string footerChangesSaveInstantly, string pressAKey,
                string defaultValuePrefix, string luggageName)
            {
                QuickSetup = quickSetup;
                TabPlayerLabels = tabPlayerLabels;
                TabPings = tabPings;
                TabItemPings = tabItemPings;
                TabCampfire = tabCampfire;
                TabCompass = tabCompass;
                TabGeneral = tabGeneral;
                DescriptionPlaceholder = descriptionPlaceholder;
                Loading = loading;
                FooterClose = footerClose;
                FooterChangesSaveInstantly = footerChangesSaveInstantly;
                PressAKey = pressAKey;
                DefaultValuePrefix = defaultValuePrefix;
                LuggageName = luggageName;
            }
        }

        private static readonly Dictionary<LocalizedText.Language, Strings> Table =
            new Dictionary<LocalizedText.Language, Strings>
            {
                [LocalizedText.Language.English] = new Strings(
                    "QUICK SETUP", "PLAYER LABELS", "PINGS", "ITEM PINGS", "CAMPFIRE", "COMPASS", "GENERAL",
                    "HOVER A SETTING FOR ITS DESCRIPTION", "LOADING...", "CLOSE", "CHANGES SAVE INSTANTLY",
                    "PRESS A KEY...", "DEFAULT:", "Luggage"),

                [LocalizedText.Language.French] = new Strings(
                    "CONFIGURATION RAPIDE", "LABELS JOUEURS", "PINGS", "PINGS D'OBJETS", "FEU DE CAMP", "BOUSSOLE",
                    "GÉNÉRAL", "SURVOLEZ UN PARAMÈTRE POUR VOIR SA DESCRIPTION", "CHARGEMENT...", "FERMER",
                    "LES MODIFICATIONS SONT ENREGISTRÉES INSTANTANÉMENT", "APPUYEZ SUR UNE TOUCHE...",
                    "PAR DÉFAUT :", "Bagage"),

                [LocalizedText.Language.Italian] = new Strings(
                    "CONFIGURAZIONE RAPIDA", "ETICHETTE GIOCATORI", "PING", "PING OGGETTI", "FALÒ", "BUSSOLA",
                    "GENERALE", "PASSA IL MOUSE SU UN'IMPOSTAZIONE PER LA SUA DESCRIZIONE", "CARICAMENTO...",
                    "CHIUDI", "LE MODIFICHE VENGONO SALVATE ISTANTANEAMENTE", "PREMI UN TASTO...", "PREDEFINITO:",
                    "Bagaglio"),

                [LocalizedText.Language.German] = new Strings(
                    "SCHNELLEINRICHTUNG", "SPIELERNAMEN", "PINGS", "GEGENSTANDS-PINGS", "LAGERFEUER", "KOMPASS",
                    "ALLGEMEIN", "BEWEGE DEN MAUSZEIGER ÜBER EINE EINSTELLUNG FÜR IHRE BESCHREIBUNG",
                    "WIRD GELADEN...", "SCHLIESSEN", "ÄNDERUNGEN WERDEN SOFORT GESPEICHERT",
                    "DRÜCKE EINE TASTE...", "STANDARD:", "Gepäck"),

                [LocalizedText.Language.SpanishSpain] = new Strings(
                    "CONFIGURACIÓN RÁPIDA", "ETIQUETAS DE JUGADOR", "PINGS", "PINGS DE OBJETOS", "HOGUERA",
                    "BRÚJULA", "GENERAL", "PASA EL CURSOR SOBRE UN AJUSTE PARA VER SU DESCRIPCIÓN",
                    "CARGANDO...", "CERRAR", "LOS CAMBIOS SE GUARDAN AL INSTANTE", "PULSA UNA TECLA...",
                    "PREDETERMINADO:", "Equipaje"),

                [LocalizedText.Language.SpanishLatam] = new Strings(
                    "CONFIGURACIÓN RÁPIDA", "ETIQUETAS DE JUGADOR", "PINGS", "PINGS DE OBJETOS", "HOGUERA",
                    "BRÚJULA", "GENERAL", "PASA EL CURSOR SOBRE UN AJUSTE PARA VER SU DESCRIPCIÓN",
                    "CARGANDO...", "CERRAR", "LOS CAMBIOS SE GUARDAN AL INSTANTE", "PULSA UNA TECLA...",
                    "PREDETERMINADO:", "Equipaje"),

                [LocalizedText.Language.BRPortuguese] = new Strings(
                    "CONFIGURAÇÃO RÁPIDA", "RÓTULOS DE JOGADOR", "PINGS", "PINGS DE ITENS", "FOGUEIRA",
                    "BÚSSOLA", "GERAL", "PASSE O MOUSE SOBRE UMA CONFIGURAÇÃO PARA VER SUA DESCRIÇÃO",
                    "CARREGANDO...", "FECHAR", "AS ALTERAÇÕES SÃO SALVAS INSTANTANEAMENTE",
                    "PRESSIONE UMA TECLA...", "PADRÃO:", "Bagagem"),

                [LocalizedText.Language.Russian] = new Strings(
                    "БЫСТРАЯ НАСТРОЙКА", "МЕТКИ ИГРОКОВ", "ПИНГИ", "ПИНГИ ПРЕДМЕТОВ", "КОСТЁР", "КОМПАС",
                    "ОБЩИЕ", "НАВЕДИТЕ КУРСОР НА НАСТРОЙКУ, ЧТОБЫ УВИДЕТЬ ОПИСАНИЕ", "ЗАГРУЗКА...", "ЗАКРЫТЬ",
                    "ИЗМЕНЕНИЯ СОХРАНЯЮТСЯ МГНОВЕННО", "НАЖМИТЕ КЛАВИШУ...", "ПО УМОЛЧАНИЮ:", "Багаж"),

                [LocalizedText.Language.Ukrainian] = new Strings(
                    "ШВИДКЕ НАЛАШТУВАННЯ", "МІТКИ ГРАВЦІВ", "ПІНГИ", "ПІНГИ ПРЕДМЕТІВ", "БАГАТТЯ", "КОМПАС",
                    "ЗАГАЛЬНІ", "НАВЕДІТЬ КУРСОР НА НАЛАШТУВАННЯ, ЩОБ ПОБАЧИТИ ОПИС", "ЗАВАНТАЖЕННЯ...",
                    "ЗАКРИТИ", "ЗМІНИ ЗБЕРІГАЮТЬСЯ МИТТЄВО", "НАТИСНІТЬ КЛАВІШУ...", "ЗА ЗАМОВЧУВАННЯМ:", "Багаж"),

                [LocalizedText.Language.SimplifiedChinese] = new Strings(
                    "快速设置", "玩家标签", "呼喊", "物品呼喊", "篝火", "指南针", "通用", "将鼠标悬停在设置上以查看说明",
                    "加载中...", "关闭", "更改会立即保存", "按下一个按键...", "默认值：", "行李"),

                [LocalizedText.Language.TraditionalChinese] = new Strings(
                    "快速設定", "玩家標籤", "呼喊", "物品呼喊", "營火", "指南針", "通用", "將滑鼠懸停在設定上以查看說明",
                    "載入中...", "關閉", "變更會立即儲存", "按下一個按鍵...", "預設值：", "行李"),

                [LocalizedText.Language.Japanese] = new Strings(
                    "クイックセットアップ", "プレイヤーラベル", "ピン", "アイテムピン", "焚き火", "コンパス", "全般",
                    "設定にカーソルを合わせると説明が表示されます", "読み込み中...", "閉じる",
                    "変更は即座に保存されます", "キーを押してください...", "デフォルト：", "荷物"),

                [LocalizedText.Language.Korean] = new Strings(
                    "빠른 설정", "플레이어 라벨", "핑", "아이템 핑", "모닥불", "나침반", "일반",
                    "설정에 마우스를 올리면 설명이 표시됩니다", "로딩 중...", "닫기", "변경 사항은 즉시 저장됩니다",
                    "키를 누르세요...", "기본값:", "짐"),

                [LocalizedText.Language.Polish] = new Strings(
                    "SZYBKA KONFIGURACJA", "ETYKIETY GRACZY", "PINGI", "PINGI PRZEDMIOTÓW", "OGNISKO", "KOMPAS",
                    "OGÓLNE", "NAJEDŹ NA USTAWIENIE, ABY ZOBACZYĆ JEGO OPIS", "WCZYTYWANIE...", "ZAMKNIJ",
                    "ZMIANY ZAPISYWANE SĄ NATYCHMIAST", "NACIŚNIJ KLAWISZ...", "DOMYŚLNIE:", "Bagaż"),

                [LocalizedText.Language.Turkish] = new Strings(
                    "HIZLI KURULUM", "OYUNCU ETİKETLERİ", "PİNGLER", "EŞYA PİNGLERİ", "KAMP ATEŞİ", "PUSULA",
                    "GENEL", "AÇIKLAMASINI GÖRMEK İÇİN BİR AYARIN ÜZERİNE GELİN", "YÜKLENİYOR...", "KAPAT",
                    "DEĞİŞİKLİKLER ANINDA KAYDEDİLİR", "BİR TUŞA BASIN...", "VARSAYILAN:", "Bagaj"),
            };

        internal static Strings Current
        {
            get
            {
                if (!Table.TryGetValue(LocalizedText.CURRENT_LANGUAGE, out Strings strings))
                {
                    strings = Table[LocalizedText.Language.English];
                }
                return strings;
            }
        }
    }
}
