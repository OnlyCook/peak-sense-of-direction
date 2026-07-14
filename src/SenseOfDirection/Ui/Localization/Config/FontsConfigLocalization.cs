using System.Collections.Generic;

namespace SenseOfDirection.Ui.Localization.Config
{
    /// <summary>Localized names/descriptions for every "Fonts" section config entry.</summary>
    internal static class FontsConfigLocalization
    {
        internal static void Register(ConfigLocalizationTable.Registry registry)
        {
            void Add(string key, Dictionary<LocalizedText.Language, ConfigLocalizationEntry> table) =>
                registry.Add("Fonts", key, table);

            Add("on-screen-name-scale", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ON-SCREEN NAME SCALE", "Scales every name label drawn on a thing you can actually see (player labels, item/creature pings). 1 keeps the shipped sizes."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ÉCHELLE DES NOMS À L'ÉCRAN", "Redimensionne tous les labels de nom affichés sur quelque chose que vous voyez réellement (labels de joueurs, pings d'objets/créatures). 1 conserve les tailles d'origine."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SCALA NOME A SCHERMO", "Ridimensiona ogni etichetta con il nome disegnata su qualcosa che puoi effettivamente vedere (etichette giocatori, ping di oggetti/creature). 1 mantiene le dimensioni originali."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("NAMENSGRÖSSE AUF BILDSCHIRM", "Skaliert jedes Namens-Label, das auf etwas gezeichnet wird, das du tatsächlich sehen kannst (Spielernamen, Gegenstands-/Kreaturen-Pings). 1 behält die Standardgrößen bei."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ESCALA DE NOMBRE EN PANTALLA", "Escala cada etiqueta de nombre dibujada sobre algo que puedes ver realmente (etiquetas de jugador, pings de objetos/criaturas). 1 mantiene los tamaños originales."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ESCALA DE NOMBRE EN PANTALLA", "Escala cada etiqueta de nombre dibujada sobre algo que puedes ver realmente (etiquetas de jugador, pings de objetos/criaturas). 1 mantiene los tamaños originales."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ESCALA DE NOME NA TELA", "Redimensiona cada rótulo de nome desenhado sobre algo que você realmente pode ver (rótulos de jogador, pings de itens/criaturas). 1 mantém os tamanhos originais."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("МАСШТАБ ИМЕНИ НА ЭКРАНЕ", "Масштабирует каждую метку с именем, отображаемую на том, что вы реально видите (метки игроков, пинги предметов/существ). 1 сохраняет исходные размеры."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("МАСШТАБ ІМЕНІ НА ЕКРАНІ", "Масштабує кожну мітку з іменем, що відображається на тому, що ви реально бачите (мітки гравців, пінги предметів/істот). 1 зберігає початкові розміри."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("屏幕内名称缩放", "缩放绘制在你实际可见事物上的所有名称标签（玩家标签、物品/生物呼喊）。1 保持出厂尺寸。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("螢幕內名稱縮放", "縮放繪製在你實際可見事物上的所有名稱標籤（玩家標籤、物品/生物呼喊）。1 保持出廠尺寸。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("画面内の名前スケール", "実際に見えているものに表示される名前ラベル（プレイヤーラベル、アイテム/クリーチャーピン）をすべて拡大縮小します。1 は出荷時のサイズのままです。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("화면 내 이름 크기", "실제로 보이는 대상에 표시되는 모든 이름 라벨(플레이어 라벨, 아이템/생물체 핑)의 크기를 조정합니다. 1은 기본 크기를 유지합니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("SKALA NAZWY NA EKRANIE", "Skaluje każdą etykietę z nazwą rysowaną na czymś, co faktycznie widzisz (etykiety graczy, pingi przedmiotów/stworzeń). 1 zachowuje domyślne rozmiary."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("EKRAN İÇİ İSİM ÖLÇEĞİ", "Gerçekten görebildiğiniz bir şeyin üzerine çizilen her isim etiketini (oyuncu etiketleri, eşya/yaratık pingleri) ölçeklendirir. 1 varsayılan boyutları korur."),
            });

            Add("on-screen-distance-scale", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ON-SCREEN DISTANCE SCALE", "Same, for the distance sub-line under an on-screen label."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ÉCHELLE DE LA DISTANCE À L'ÉCRAN", "Pareil, pour la ligne de distance sous un label à l'écran."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SCALA DISTANZA A SCHERMO", "Uguale, per la riga della distanza sotto un'etichetta a schermo."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("ENTFERNUNGSGRÖSSE AUF BILDSCHIRM", "Dasselbe, für die Entfernungszeile unter einem Label auf dem Bildschirm."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ESCALA DE DISTANCIA EN PANTALLA", "Lo mismo, para la línea de distancia bajo una etiqueta en pantalla."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ESCALA DE DISTANCIA EN PANTALLA", "Lo mismo, para la línea de distancia bajo una etiqueta en pantalla."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ESCALA DE DISTÂNCIA NA TELA", "O mesmo, para a linha de distância abaixo de um rótulo na tela."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("МАСШТАБ РАССТОЯНИЯ НА ЭКРАНЕ", "То же самое, для строки расстояния под меткой на экране."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("МАСШТАБ ВІДСТАНІ НА ЕКРАНІ", "Те саме, для рядка відстані під міткою на екрані."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("屏幕内距离缩放", "同上，用于屏幕内标签下方的距离子行。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("螢幕內距離縮放", "同上，用於螢幕內標籤下方的距離子行。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("画面内の距離スケール", "同様に、画面内ラベルの下にある距離のサブラインに適用されます。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("화면 내 거리 크기", "동일하게, 화면 내 라벨 아래 거리 보조 줄에 적용됩니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("SKALA ODLEGŁOŚCI NA EKRANIE", "To samo, dla linii odległości pod etykietą na ekranie."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("EKRAN İÇİ MESAFE ÖLÇEĞİ", "Aynısı, ekran içi etiketin altındaki mesafe alt satırı için."),
            });

            Add("off-screen-name-scale", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("OFF-SCREEN NAME SCALE", "Scales every name label on a thing that's currently off-screen, i.e. clamped to the edge with an arrow. Set this below {key:Fonts/on-screen-name-scale} to keep a crowded screen edge quieter without shrinking the labels on things you're actually looking at."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ÉCHELLE DES NOMS HORS ÉCRAN", "Redimensionne chaque label de nom sur quelque chose actuellement hors écran, c'est-à-dire fixé au bord avec une flèche. Réglez cette valeur en dessous de {key:Fonts/on-screen-name-scale} pour garder un bord d'écran chargé plus discret sans réduire les labels des choses que vous regardez réellement."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SCALA NOME FUORI SCHERMO", "Ridimensiona ogni etichetta con il nome su qualcosa che al momento è fuori schermo, cioè bloccato al bordo con una freccia. Imposta questo valore sotto {key:Fonts/on-screen-name-scale} per rendere un bordo schermo affollato più discreto senza rimpicciolire le etichette delle cose che stai effettivamente guardando."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("NAMENSGRÖSSE AUSSERHALB DES BILDSCHIRMS", "Skaliert jedes Namens-Label auf etwas, das sich gerade außerhalb des Bildschirms befindet, also mit einem Pfeil an den Rand geklemmt ist. Setze diesen Wert unter {key:Fonts/on-screen-name-scale}, um einen überfüllten Bildschirmrand ruhiger wirken zu lassen, ohne die Labels der Dinge zu verkleinern, die du gerade tatsächlich betrachtest."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ESCALA DE NOMBRE FUERA DE PANTALLA", "Escala cada etiqueta de nombre de algo que está actualmente fuera de pantalla, es decir, fijado al borde con una flecha. Ajusta este valor por debajo de {key:Fonts/on-screen-name-scale} para que un borde de pantalla saturado se vea más tranquilo sin reducir las etiquetas de lo que realmente estás mirando."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ESCALA DE NOMBRE FUERA DE PANTALLA", "Escala cada etiqueta de nombre de algo que está actualmente fuera de pantalla, es decir, fijado al borde con una flecha. Ajusta este valor por debajo de {key:Fonts/on-screen-name-scale} para que un borde de pantalla saturado se vea más tranquilo sin reducir las etiquetas de lo que realmente estás mirando."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ESCALA DE NOME FORA DA TELA", "Redimensiona cada rótulo de nome de algo que está atualmente fora da tela, ou seja, fixado na borda com uma seta. Defina este valor abaixo de {key:Fonts/on-screen-name-scale} para deixar uma borda de tela lotada mais discreta sem diminuir os rótulos das coisas que você realmente está olhando."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("МАСШТАБ ИМЕНИ ЗА ЭКРАНОМ", "Масштабирует каждую метку с именем на объекте, находящемся сейчас за пределами экрана, то есть закреплённом у края со стрелкой. Установите это значение ниже {key:Fonts/on-screen-name-scale}, чтобы переполненный край экрана выглядел спокойнее, не уменьшая метки того, на что вы реально смотрите."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("МАСШТАБ ІМЕНІ ЗА ЕКРАНОМ", "Масштабує кожну мітку з іменем на об'єкті, який зараз перебуває за межами екрана, тобто закріпленому біля краю зі стрілкою. Встановіть це значення нижче {key:Fonts/on-screen-name-scale}, щоб перевантажений край екрана виглядав спокійніше, не зменшуючи мітки того, на що ви реально дивитеся."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("屏幕外名称缩放", "缩放当前处于屏幕外（即带箭头固定在边缘）事物的名称标签。将此值设为低于 {key:Fonts/on-screen-name-scale}，可让拥挤的屏幕边缘更安静，同时不缩小你实际正在看的事物的标签。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("螢幕外名稱縮放", "縮放目前處於螢幕外（即帶箭頭固定在邊緣）事物的名稱標籤。將此值設為低於 {key:Fonts/on-screen-name-scale}，可讓擁擠的螢幕邊緣更安靜，同時不縮小你實際正在看的事物的標籤。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("画面外の名前スケール", "現在画面外にある、つまり矢印付きで端に固定されているものの名前ラベルをすべて拡大縮小します。この値を {key:Fonts/on-screen-name-scale} より小さくすると、実際に見ているものへのラベルを縮小せずに、混雑した画面端を控えめにできます。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("화면 밖 이름 크기", "현재 화면 밖에 있는, 즉 화살표와 함께 가장자리에 고정된 대상의 모든 이름 라벨 크기를 조정합니다. 이 값을 {key:Fonts/on-screen-name-scale}보다 낮게 설정하면 실제로 보고 있는 대상의 라벨을 줄이지 않고도 붐비는 화면 가장자리를 더 조용하게 만들 수 있습니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("SKALA NAZWY POZA EKRANEM", "Skaluje każdą etykietę z nazwą na czymś, co obecnie znajduje się poza ekranem, czyli jest przypięte do krawędzi ze strzałką. Ustaw tę wartość poniżej {key:Fonts/on-screen-name-scale}, aby zatłoczona krawędź ekranu była spokojniejsza bez pomniejszania etykiet rzeczy, na które faktycznie patrzysz."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("EKRAN DIŞI İSİM ÖLÇEĞİ", "Şu anda ekran dışında olan, yani bir okla kenara sabitlenmiş bir şeyin her isim etiketini ölçeklendirir. Gerçekten baktığınız şeylerin etiketlerini küçültmeden kalabalık bir ekran kenarını daha sakin tutmak için bunu {key:Fonts/on-screen-name-scale} altında ayarlayın."),
            });

            Add("off-screen-distance-scale", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("OFF-SCREEN DISTANCE SCALE", "Same, for the distance sub-line under an off-screen (edge-clamped) label."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ÉCHELLE DE LA DISTANCE HORS ÉCRAN", "Pareil, pour la ligne de distance sous un label hors écran (fixé au bord)."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SCALA DISTANZA FUORI SCHERMO", "Uguale, per la riga della distanza sotto un'etichetta fuori schermo (bloccata al bordo)."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("ENTFERNUNGSGRÖSSE AUSSERHALB DES BILDSCHIRMS", "Dasselbe, für die Entfernungszeile unter einem Label außerhalb des Bildschirms (am Rand fixiert)."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ESCALA DE DISTANCIA FUERA DE PANTALLA", "Lo mismo, para la línea de distancia bajo una etiqueta fuera de pantalla (fijada al borde)."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ESCALA DE DISTANCIA FUERA DE PANTALLA", "Lo mismo, para la línea de distancia bajo una etiqueta fuera de pantalla (fijada al borde)."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ESCALA DE DISTÂNCIA FORA DA TELA", "O mesmo, para a linha de distância abaixo de um rótulo fora da tela (fixado na borda)."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("МАСШТАБ РАССТОЯНИЯ ЗА ЭКРАНОМ", "То же самое, для строки расстояния под меткой за экраном (закреплённой у края)."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("МАСШТАБ ВІДСТАНІ ЗА ЕКРАНОМ", "Те саме, для рядка відстані під міткою за екраном (закріпленою біля краю)."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("屏幕外距离缩放", "同上，用于屏幕外（边缘固定）标签下方的距离子行。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("螢幕外距離縮放", "同上，用於螢幕外（邊緣固定）標籤下方的距離子行。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("画面外の距離スケール", "同様に、画面外（端に固定された）ラベルの下にある距離のサブラインに適用されます。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("화면 밖 거리 크기", "동일하게, 화면 밖(가장자리 고정) 라벨 아래 거리 보조 줄에 적용됩니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("SKALA ODLEGŁOŚCI POZA EKRANEM", "To samo, dla linii odległości pod etykietą poza ekranem (przypiętą do krawędzi)."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("EKRAN DIŞI MESAFE ÖLÇEĞİ", "Aynısı, ekran dışı (kenara sabitlenmiş) etiketin altındaki mesafe alt satırı için."),
            });

            Add("compass-name-scale", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("COMPASS NAME SCALE", "Scales the name label above each compass-tape marker (only shown at all when Compass/{key:Compass/show-names} is on)."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ÉCHELLE DES NOMS SUR LA BOUSSOLE", "Redimensionne le label de nom au-dessus de chaque repère de la boussole (affiché uniquement si Compass/{key:Compass/show-names} est activé)."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SCALA NOME BUSSOLA", "Ridimensiona l'etichetta con il nome sopra ogni indicatore della bussola (visibile solo se Compass/{key:Compass/show-names} è attivo)."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("NAMENSGRÖSSE AUF DEM KOMPASS", "Skaliert das Namens-Label über jeder Kompassband-Markierung (nur sichtbar, wenn Compass/{key:Compass/show-names} aktiviert ist)."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ESCALA DE NOMBRE EN LA BRÚJULA", "Escala la etiqueta de nombre sobre cada marcador de la brújula (solo se muestra si Compass/{key:Compass/show-names} está activado)."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ESCALA DE NOMBRE EN LA BRÚJULA", "Escala la etiqueta de nombre sobre cada marcador de la brújula (solo se muestra si Compass/{key:Compass/show-names} está activado)."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ESCALA DE NOME NA BÚSSOLA", "Redimensiona o rótulo de nome acima de cada marcador da bússola (só aparece quando Compass/{key:Compass/show-names} está ativado)."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("МАСШТАБ ИМЕНИ НА КОМПАСЕ", "Масштабирует метку с именем над каждой меткой на компасе (отображается только при включённом Compass/{key:Compass/show-names})."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("МАСШТАБ ІМЕНІ НА КОМПАСІ", "Масштабує мітку з іменем над кожною міткою на компасі (відображається лише при увімкненому Compass/{key:Compass/show-names})."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("指南针名称缩放", "缩放每个指南针标记上方的名称标签（仅在 Compass/{key:Compass/show-names} 开启时显示）。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("指南針名稱縮放", "縮放每個指南針標記上方的名稱標籤（僅在 Compass/{key:Compass/show-names} 開啟時顯示）。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("コンパスの名前スケール", "各コンパスマーカーの上にある名前ラベルを拡大縮小します（Compass/{key:Compass/show-names} がオンのときのみ表示されます）。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("나침반 이름 크기", "각 나침반 표시 위의 이름 라벨 크기를 조정합니다(Compass/{key:Compass/show-names}가 켜져 있을 때만 표시됨)."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("SKALA NAZWY NA KOMPASIE", "Skaluje etykietę z nazwą nad każdym znacznikiem kompasu (widoczna tylko, gdy Compass/{key:Compass/show-names} jest włączone)."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("PUSULA İSİM ÖLÇEĞİ", "Her pusula işaretçisinin üzerindeki isim etiketini ölçeklendirir (yalnızca Compass/{key:Compass/show-names} açıkken gösterilir)."),
            });

            Add("compass-distance-scale", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("COMPASS DISTANCE SCALE", "Scales the distance sub-label under each compass-tape marker."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ÉCHELLE DE LA DISTANCE SUR LA BOUSSOLE", "Redimensionne le sous-label de distance sous chaque repère de la boussole."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SCALA DISTANZA BUSSOLA", "Ridimensiona la sotto-etichetta della distanza sotto ogni indicatore della bussola."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("ENTFERNUNGSGRÖSSE AUF DEM KOMPASS", "Skaliert das Entfernungs-Unterlabel unter jeder Kompassband-Markierung."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ESCALA DE DISTANCIA EN LA BRÚJULA", "Escala la subetiqueta de distancia bajo cada marcador de la brújula."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ESCALA DE DISTANCIA EN LA BRÚJULA", "Escala la subetiqueta de distancia bajo cada marcador de la brújula."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ESCALA DE DISTÂNCIA NA BÚSSOLA", "Redimensiona o sub-rótulo de distância abaixo de cada marcador da bússola."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("МАСШТАБ РАССТОЯНИЯ НА КОМПАСЕ", "Масштабирует подпись расстояния под каждой меткой на компасе."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("МАСШТАБ ВІДСТАНІ НА КОМПАСІ", "Масштабує підпис відстані під кожною міткою на компасі."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("指南针距离缩放", "缩放每个指南针标记下方的距离子标签。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("指南針距離縮放", "縮放每個指南針標記下方的距離子標籤。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("コンパスの距離スケール", "各コンパスマーカーの下にある距離のサブラベルを拡大縮小します。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("나침반 거리 크기", "각 나침반 표시 아래의 거리 보조 라벨 크기를 조정합니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("SKALA ODLEGŁOŚCI NA KOMPASIE", "Skaluje podetykietę odległości pod każdym znacznikiem kompasu."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("PUSULA MESAFE ÖLÇEĞİ", "Her pusula işaretçisinin altındaki mesafe alt etiketini ölçeklendirir."),
            });
        }
    }
}
