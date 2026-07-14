using System.Collections.Generic;

namespace SenseOfDirection.Ui.Localization.Config
{
    /// <summary>Localized names/descriptions for every "Campfire" section config entry.</summary>
    internal static class CampfireConfigLocalization
    {
        internal static void Register(ConfigLocalizationTable.Registry registry)
        {
            void Add(string key, Dictionary<LocalizedText.Language, ConfigLocalizationEntry> table) =>
                registry.Add("Campfire", key, table);

            Add("enable-campfire-indicator", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ENABLE CAMPFIRE INDICATOR", "Show an always-on edge-of-screen indicator pointing at the current segment's campfire (the one you're trying to reach next), so you always know which way to go. On by default, since this is the most direct answer to the question the mod is named after. Turn it off if you'd rather find your own way up and keep the rest of the mod."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ACTIVER L'INDICATEUR DE FEU DE CAMP", "Affiche un indicateur permanent en bord d'écran pointant vers le feu de camp du segment actuel (celui que vous essayez d'atteindre), pour toujours savoir où aller. Activé par défaut, car c'est la réponse la plus directe à la question qui donne son nom au mod. Désactivez-le si vous préférez trouver votre propre chemin tout en gardant le reste du mod."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("ATTIVA INDICATORE FALÒ", "Mostra un indicatore sempre attivo sul bordo dello schermo che punta verso il falò del segmento attuale (quello che stai cercando di raggiungere), così saprai sempre da che parte andare. Attivo di default, perché è la risposta più diretta alla domanda da cui prende il nome il mod. Disattivalo se preferisci trovare la tua strada da solo mantenendo il resto del mod."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("LAGERFEUER-ANZEIGE AKTIVIEREN", "Zeigt eine dauerhafte Bildschirmrand-Anzeige, die auf das Lagerfeuer des aktuellen Abschnitts zeigt (das, das du gerade erreichen willst), damit du immer weißt, wohin es geht. Standardmäßig aktiviert, da dies die direkteste Antwort auf die Frage ist, nach der der Mod benannt ist. Schalte es aus, wenn du deinen eigenen Weg finden und trotzdem den Rest des Mods behalten möchtest."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ACTIVAR INDICADOR DE HOGUERA", "Muestra un indicador permanente en el borde de la pantalla que señala hacia la hoguera del segmento actual (la que intentas alcanzar), para que siempre sepas hacia dónde ir. Activado por defecto, ya que es la respuesta más directa a la pregunta que da nombre al mod. Desactívalo si prefieres encontrar tu propio camino y conservar el resto del mod."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ACTIVAR INDICADOR DE FOGATA", "Muestra un indicador permanente en el borde de la pantalla que señala hacia la fogata del segmento actual (la que intentas alcanzar), para que siempre sepas hacia dónde ir. Activado por defecto, ya que es la respuesta más directa a la pregunta que le da nombre al mod. Desactívalo si prefieres encontrar tu propio camino y conservar el resto del mod."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ATIVAR INDICADOR DE FOGUEIRA", "Mostra um indicador permanente na borda da tela apontando para a fogueira do segmento atual (aquela que você está tentando alcançar), para que você sempre saiba para onde ir. Ativado por padrão, já que essa é a resposta mais direta à pergunta que dá nome ao mod. Desative se preferir encontrar seu próprio caminho e manter o resto do mod."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ВКЛЮЧИТЬ ИНДИКАТОР КОСТРА", "Показывает постоянный индикатор на краю экрана, указывающий на костёр текущего участка (тот, к которому вы стремитесь), чтобы вы всегда знали, куда идти. Включено по умолчанию, так как это самый прямой ответ на вопрос, в честь которого назван мод. Отключите, если предпочитаете искать путь сами, сохранив остальной функционал мода."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("УВІМКНУТИ ІНДИКАТОР БАГАТТЯ", "Показує постійний індикатор на краю екрана, що вказує на багаття поточної ділянки (те, до якого ви прямуєте), щоб ви завжди знали, куди йти. Увімкнено за замовчуванням, адже це найпряміша відповідь на питання, на честь якого названо мод. Вимкніть, якщо волієте шукати шлях самостійно, зберігаючи решту функціоналу мода."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("启用篝火指示器", "始终显示一个屏幕边缘指示器，指向当前段落的篝火（你正试图到达的那个），这样你就能一直知道该往哪走。默认开启，因为这是对本模组名字所提问题最直接的回答。如果你更想自己摸索路线，同时保留模组其余功能，可以关闭此项。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("啟用營火指示器", "始終顯示一個螢幕邊緣指示器，指向目前段落的營火（你正試圖到達的那個），讓你隨時知道該往哪走。預設開啟，因為這是對本模組名稱所提問題最直接的回答。若你想自己摸索路線並保留模組其餘功能，可以關閉此項。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("焚き火インジケーターを有効化", "現在の区間の焚き火（次に目指す焚き火）を指す、常時表示の画面端インジケーターを表示し、常にどちらへ進めばよいか分かるようにします。このMODの名前の由来である問いに最も直接的に答える機能なので、デフォルトで有効です。自分で道を見つけたい場合は無効にしても、MODの他の機能はそのまま使えます。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("모닥불 표시기 활성화", "현재 구간의 모닥불(다음에 도달하려는 목표)을 가리키는 화면 가장자리 표시기를 항상 표시하여 어느 방향으로 가야 할지 항상 알 수 있게 합니다. 이 모드의 이름이 답하려는 질문에 가장 직접적으로 답하는 기능이므로 기본적으로 켜져 있습니다. 직접 길을 찾고 싶다면 이 기능만 끄고 모드의 나머지 기능은 그대로 사용할 수 있습니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("WŁĄCZ WSKAŹNIK OGNISKA", "Pokazuje stały wskaźnik na krawędzi ekranu wskazujący na ognisko bieżącego odcinka (to, do którego zmierzasz), dzięki czemu zawsze wiesz, w którą stronę iść. Domyślnie włączone, ponieważ to najbardziej bezpośrednia odpowiedź na pytanie, od którego wziął się tytuł moda. Wyłącz, jeśli wolisz sam znajdować drogę, zachowując resztę funkcji moda."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("KAMP ATEŞİ GÖSTERGESİNİ ETKİNLEŞTİR", "Mevcut bölümün kamp ateşine (ulaşmaya çalıştığınız ateşe) işaret eden, her zaman açık bir ekran kenarı göstergesi gösterir; böylece her zaman hangi yöne gideceğinizi bilirsiniz. Modun adını aldığı soruya en doğrudan cevap olduğu için varsayılan olarak açıktır. Kendi yolunuzu bulmayı tercih ediyorsanız ve modun geri kalanını korumak istiyorsanız kapatabilirsiniz."),
            });

            Add("show-distance", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("SHOW DISTANCE", "Show the distance sub-line under the campfire indicator."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("AFFICHER LA DISTANCE", "Affiche la sous-ligne de distance sous l'indicateur de feu de camp."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("MOSTRA DISTANZA", "Mostra la sottolinea della distanza sotto l'indicatore del falò."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("ENTFERNUNG ANZEIGEN", "Zeigt die Entfernungszeile unter der Lagerfeuer-Anzeige."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("MOSTRAR DISTANCIA", "Muestra la línea de distancia debajo del indicador de la hoguera."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("MOSTRAR DISTANCIA", "Muestra la línea de distancia debajo del indicador de la fogata."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("MOSTRAR DISTÂNCIA", "Mostra a linha de distância abaixo do indicador da fogueira."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ПОКАЗЫВАТЬ РАССТОЯНИЕ", "Показывает строку расстояния под индикатором костра."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ПОКАЗУВАТИ ВІДСТАНЬ", "Показує рядок відстані під індикатором багаття."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("显示距离", "在篝火指示器下方显示距离子行。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("顯示距離", "在營火指示器下方顯示距離子行。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("距離を表示", "焚き火インジケーターの下に距離のサブ行を表示します。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("거리 표시", "모닥불 표시기 아래에 거리 보조 줄을 표시합니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("POKAŻ ODLEGŁOŚĆ", "Pokazuje linię odległości pod wskaźnikiem ogniska."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("MESAFEYİ GÖSTER", "Kamp ateşi göstergesinin altında mesafe alt satırını gösterir."),
            });
        }
    }
}
