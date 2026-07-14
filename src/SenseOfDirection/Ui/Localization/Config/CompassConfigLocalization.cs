using System.Collections.Generic;

namespace SenseOfDirection.Ui.Localization.Config
{
    /// <summary>Localized names/descriptions for every "Compass" section config entry.</summary>
    internal static class CompassConfigLocalization
    {
        internal static void Register(ConfigLocalizationTable.Registry registry)
        {
            void Add(string key, Dictionary<LocalizedText.Language, ConfigLocalizationEntry> table) =>
                registry.Add("Compass", key, table);

            Add("enable-compass", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ENABLE COMPASS", "Master switch for the top-of-screen compass tape. Off hides it entirely regardless of any individual mechanic's placement setting."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ACTIVER LA BOUSSOLE", "Interrupteur principal pour la bande de boussole en haut de l'écran. Désactivé la masque entièrement, quel que soit le réglage de placement propre à chaque mécanique."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("ATTIVA BUSSOLA", "Interruttore principale per la barra della bussola in alto sullo schermo. Disattivato la nasconde completamente, indipendentemente dall'impostazione di posizionamento propria di ogni meccanica."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("KOMPASS AKTIVIEREN", "Hauptschalter für das Kompassband am oberen Bildschirmrand. Deaktiviert blendet es komplett aus, unabhängig von der eigenen Platzierungseinstellung jedes Mechanismus."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ACTIVAR BRÚJULA", "Interruptor principal de la cinta de brújula en la parte superior de la pantalla. Desactivado la oculta por completo, sin importar el ajuste de ubicación propio de cada mecánica."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ACTIVAR BRÚJULA", "Interruptor principal de la cinta de brújula en la parte superior de la pantalla. Desactivado la oculta por completo, sin importar el ajuste de ubicación propio de cada mecánica."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ATIVAR BÚSSOLA", "Interruptor principal da fita de bússola no topo da tela. Desativado a oculta completamente, independentemente da própria configuração de posição de cada mecânica."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ВКЛЮЧИТЬ КОМПАС", "Главный переключатель ленты компаса в верхней части экрана. Выключено полностью скрывает её, независимо от собственной настройки размещения каждого механизма."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("УВІМКНУТИ КОМПАС", "Головний перемикач стрічки компаса вгорі екрана. Вимкнено повністю приховує її, незалежно від власного налаштування розміщення кожного механізму."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("启用指南针", "屏幕顶部指南针条带的主开关。关闭后无论各机制自己的位置设置如何，都会完全隐藏它。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("啟用指南針", "螢幕頂部指南針條帶的主開關。關閉後無論各機制自己的位置設定為何，都會完全隱藏它。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("コンパスを有効化", "画面上部のコンパスバーのマスタースイッチ。オフにすると、各機能自体の配置設定に関係なく完全に非表示になります。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("나침반 활성화", "화면 상단 나침반 띠의 마스터 스위치입니다. 끄면 각 기능 자체의 배치 설정과 관계없이 완전히 숨겨집니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("WŁĄCZ KOMPAS", "Główny przełącznik paska kompasu u góry ekranu. Wyłączenie ukrywa go całkowicie, niezależnie od własnego ustawienia umiejscowienia danego mechanizmu."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("PUSULAYI ETKİNLEŞTİR", "Ekranın üstündeki pusula şeridi için ana anahtar. Kapalı olduğunda, her mekaniğin kendi konum ayarından bağımsız olarak tamamen gizlenir."),
            });

            Add("width-pixels", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("WIDTH PIXELS", "Width of the compass tape, in pixels at the 1920-wide reference resolution (scales with actual resolution same as everything else). Wider shows more of the horizon at once."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("LARGEUR EN PIXELS", "Largeur de la bande de boussole, en pixels à la résolution de référence de 1920 (s'adapte à la résolution réelle comme tout le reste). Plus large affiche davantage de l'horizon à la fois."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("LARGHEZZA IN PIXEL", "Larghezza della barra della bussola, in pixel alla risoluzione di riferimento di 1920 (si adatta alla risoluzione reale come tutto il resto). Più larga mostra più orizzonte alla volta."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("BREITE IN PIXELN", "Breite des Kompassbands in Pixeln bei der Referenzauflösung von 1920 (skaliert wie alles andere mit der tatsächlichen Auflösung). Breiter zeigt mehr vom Horizont auf einmal."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ANCHO EN PÍXELES", "Ancho de la cinta de brújula, en píxeles a la resolución de referencia de 1920 (se escala con la resolución real como todo lo demás). Más ancho muestra más horizonte a la vez."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ANCHO EN PÍXELES", "Ancho de la cinta de brújula, en píxeles a la resolución de referencia de 1920 (se escala con la resolución real como todo lo demás). Más ancho muestra más horizonte a la vez."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("LARGURA EM PIXELS", "Largura da fita de bússola, em pixels na resolução de referência de 1920 (escala com a resolução real como tudo o mais). Mais larga mostra mais do horizonte de uma vez."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ШИРИНА В ПИКСЕЛЯХ", "Ширина ленты компаса в пикселях при базовом разрешении 1920 (масштабируется вместе с реальным разрешением, как и всё остальное). Чем шире, тем больше горизонта видно одновременно."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ШИРИНА В ПІКСЕЛЯХ", "Ширина стрічки компаса в пікселях за базової роздільної здатності 1920 (масштабується разом із реальною роздільною здатністю, як і все інше). Що ширше, то більше горизонту видно одночасно."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("宽度（像素）", "指南针条带的宽度，以 1920 宽参考分辨率下的像素为单位（与其他一切一样会随实际分辨率缩放）。更宽可一次显示更多地平线范围。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("寬度（像素）", "指南針條帶的寬度，以 1920 寬參考解析度下的像素為單位（與其他一切一樣會隨實際解析度縮放）。更寬可一次顯示更多地平線範圍。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("幅（ピクセル）", "コンパスバーの幅（基準解像度1920幅でのピクセル数、他のすべてと同様に実際の解像度に合わせて拡大縮小されます）。広くすると一度に見える地平線の範囲が広がります。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("너비(픽셀)", "나침반 띠의 너비로, 1920 너비 기준 해상도에서의 픽셀 단위입니다(다른 모든 것과 마찬가지로 실제 해상도에 맞춰 크기가 조정됩니다). 넓게 설정하면 한 번에 더 많은 지평선이 보입니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("SZEROKOŚĆ W PIKSELACH", "Szerokość paska kompasu w pikselach przy referencyjnej rozdzielczości 1920 (skaluje się wraz z rzeczywistą rozdzielczością, tak jak wszystko inne). Większa szerokość pokazuje więcej horyzontu naraz."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("GENİŞLİK (PİKSEL)", "Pusula şeridinin, 1920 genişlik referans çözünürlüğündeki piksel cinsinden genişliği (her şey gibi gerçek çözünürlüğe göre ölçeklenir). Daha geniş olması, ufkun aynı anda daha fazla kısmını gösterir."),
            });

            Add("marker-gap-pixels", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("MARKER GAP PIXELS", "Vertical gap between the tick row and the marker baseline below it, on top of a small fixed minimum. The default keeps everything tight together; raise this for more breathing room (e.g. after turning on {key:Compass/show-names})."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ESPACE MARQUEURS EN PIXELS", "Espace vertical entre la rangée de graduations et la ligne de base des marqueurs en dessous, en plus d'un petit minimum fixe. La valeur par défaut garde tout compact ; augmentez-la pour plus d'espace (par ex. après avoir activé {key:Compass/show-names})."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SPAZIO MARCATORI IN PIXEL", "Spazio verticale tra la riga delle tacche e la linea di base dei marcatori sottostante, oltre a un piccolo minimo fisso. Il valore predefinito mantiene tutto compatto; aumentalo per più spazio (ad es. dopo aver attivato {key:Compass/show-names})."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("MARKIERUNGSABSTAND IN PIXELN", "Vertikaler Abstand zwischen der Strichreihe und der darunterliegenden Markierungsgrundlinie, zusätzlich zu einem kleinen festen Minimum. Der Standardwert hält alles eng beieinander; erhöhe ihn für mehr Platz (z. B. nach dem Aktivieren von {key:Compass/show-names})."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ESPACIO DE MARCADORES EN PÍXELES", "Espacio vertical entre la fila de marcas y la línea base de marcadores debajo de ella, además de un pequeño mínimo fijo. El valor predeterminado mantiene todo compacto; auméntalo para más espacio (por ejemplo, tras activar {key:Compass/show-names})."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ESPACIO DE MARCADORES EN PÍXELES", "Espacio vertical entre la fila de marcas y la línea base de marcadores debajo de ella, además de un pequeño mínimo fijo. El valor predeterminado mantiene todo compacto; auméntalo para más espacio (por ejemplo, tras activar {key:Compass/show-names})."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ESPAÇO DE MARCADORES EM PIXELS", "Espaço vertical entre a linha de marcas e a linha de base dos marcadores abaixo dela, além de um pequeno mínimo fixo. O padrão mantém tudo compacto; aumente para mais espaço (ex.: após ativar {key:Compass/show-names})."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ОТСТУП МЕТОК В ПИКСЕЛЯХ", "Вертикальный отступ между рядом делений и базовой линией меток под ним, поверх небольшого фиксированного минимума. Значение по умолчанию держит всё компактно; увеличьте для большего простора (например, после включения {key:Compass/show-names})."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ВІДСТУП МІТОК У ПІКСЕЛЯХ", "Вертикальний відступ між рядом поділок і базовою лінією міток під ним, поверх невеликого фіксованого мінімуму. Значення за замовчуванням тримає все компактно; збільште для більшого простору (наприклад, після увімкнення {key:Compass/show-names})."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("标记间距（像素）", "刻度行与其下方标记基线之间的垂直间距，叠加在一个较小的固定最小值之上。默认值让所有内容紧凑排列；如需更多空间（例如开启 {key:Compass/show-names} 后）可调高此值。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("標記間距（像素）", "刻度列與其下方標記基線之間的垂直間距，疊加在一個較小的固定最小值之上。預設值讓所有內容緊湊排列；如需更多空間（例如開啟 {key:Compass/show-names} 後）可調高此值。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("マーカー間隔（ピクセル）", "目盛り行とその下のマーカー基準線との間の垂直方向の間隔（小さな固定最小値に上乗せされます）。デフォルトではすべてが密集して表示されます。余裕を持たせたい場合（例：{key:Compass/show-names} を有効にした後）は値を上げてください。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("마커 간격(픽셀)", "눈금 행과 그 아래 마커 기준선 사이의 수직 간격으로, 작은 고정 최솟값에 더해집니다. 기본값은 모든 것을 촘촘하게 유지합니다. 여유 공간을 늘리려면(예: {key:Compass/show-names}를 켠 후) 값을 높이세요."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("ODSTĘP ZNACZNIKÓW W PIKSELACH", "Pionowy odstęp między rzędem kresek a leżącą pod nim linią bazową znaczników, ponad niewielkim stałym minimum. Wartość domyślna trzyma wszystko blisko siebie; zwiększ ją, aby uzyskać więcej przestrzeni (np. po włączeniu {key:Compass/show-names})."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("İŞARETÇİ BOŞLUĞU (PİKSEL)", "Çentik sırası ile altındaki işaretçi taban çizgisi arasındaki dikey boşluk, küçük sabit bir minimuma ek olarak. Varsayılan değer her şeyi sıkı tutar; daha fazla boşluk için (örneğin {key:Compass/show-names} açıldıktan sonra) bu değeri artırın."),
            });

            Add("vertical-offset-pixels", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("VERTICAL OFFSET PIXELS", "Gap between the top of the screen and the compass tape."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("DÉCALAGE VERTICAL EN PIXELS", "Espace entre le haut de l'écran et la bande de boussole."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SCOSTAMENTO VERTICALE IN PIXEL", "Spazio tra la parte superiore dello schermo e la barra della bussola."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("VERTIKALER VERSATZ IN PIXELN", "Abstand zwischen dem oberen Bildschirmrand und dem Kompassband."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("DESPLAZAMIENTO VERTICAL EN PÍXELES", "Espacio entre la parte superior de la pantalla y la cinta de brújula."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("DESPLAZAMIENTO VERTICAL EN PÍXELES", "Espacio entre la parte superior de la pantalla y la cinta de brújula."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("DESLOCAMENTO VERTICAL EM PIXELS", "Espaço entre o topo da tela e a fita de bússola."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ВЕРТИКАЛЬНОЕ СМЕЩЕНИЕ В ПИКСЕЛЯХ", "Отступ между верхним краем экрана и лентой компаса."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ВЕРТИКАЛЬНЕ ЗМІЩЕННЯ В ПІКСЕЛЯХ", "Відступ між верхнім краєм екрана та стрічкою компаса."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("垂直偏移（像素）", "屏幕顶部与指南针条带之间的间距。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("垂直偏移（像素）", "螢幕頂部與指南針條帶之間的間距。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("垂直オフセット（ピクセル）", "画面上端とコンパスバーの間の間隔。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("수직 오프셋(픽셀)", "화면 상단과 나침반 띠 사이의 간격입니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("PRZESUNIĘCIE PIONOWE W PIKSELACH", "Odstęp między górną krawędzią ekranu a paskiem kompasu."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("DİKEY KAYDIRMA (PİKSEL)", "Ekranın üst kısmı ile pusula şeridi arasındaki boşluk."),
            });

            Add("horizontal-offset-pixels", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("HORIZONTAL OFFSET PIXELS", "Horizontal offset from top-center. 0 keeps it centered; positive shifts right, negative shifts left (e.g. to dodge another HUD mod's own top-of-screen element)."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("DÉCALAGE HORIZONTAL EN PIXELS", "Décalage horizontal par rapport au centre supérieur. 0 le garde centré ; positif décale vers la droite, négatif vers la gauche (par ex. pour éviter l'élément d'un autre mod HUD en haut de l'écran)."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SCOSTAMENTO ORIZZONTALE IN PIXEL", "Scostamento orizzontale dal centro superiore. 0 lo mantiene centrato; positivo sposta a destra, negativo a sinistra (ad es. per evitare l'elemento in alto di un'altra mod HUD)."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("HORIZONTALER VERSATZ IN PIXELN", "Horizontaler Versatz von der oberen Mitte. 0 hält es zentriert; positiv verschiebt nach rechts, negativ nach links (z. B. um einem anderen HUD-Mod-Element am oberen Bildschirmrand auszuweichen)."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("DESPLAZAMIENTO HORIZONTAL EN PÍXELES", "Desplazamiento horizontal desde el centro superior. 0 lo mantiene centrado; positivo desplaza a la derecha, negativo a la izquierda (por ejemplo, para evitar el elemento superior de otro mod de HUD)."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("DESPLAZAMIENTO HORIZONTAL EN PÍXELES", "Desplazamiento horizontal desde el centro superior. 0 lo mantiene centrado; positivo desplaza a la derecha, negativo a la izquierda (por ejemplo, para evitar el elemento superior de otro mod de HUD)."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("DESLOCAMENTO HORIZONTAL EM PIXELS", "Deslocamento horizontal a partir do centro superior. 0 mantém centralizado; positivo desloca para a direita, negativo para a esquerda (ex.: para evitar o elemento superior de outro mod de HUD)."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ГОРИЗОНТАЛЬНОЕ СМЕЩЕНИЕ В ПИКСЕЛЯХ", "Горизонтальное смещение от верхнего центра. 0 оставляет по центру; положительное смещает вправо, отрицательное — влево (например, чтобы избежать элемента другого HUD-мода вверху экрана)."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ГОРИЗОНТАЛЬНЕ ЗМІЩЕННЯ В ПІКСЕЛЯХ", "Горизонтальне зміщення від верхнього центру. 0 залишає по центру; додатне зміщує праворуч, від'ємне — ліворуч (наприклад, щоб уникнути елемента іншого HUD-мода вгорі екрана)."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("水平偏移（像素）", "相对于顶部中心的水平偏移。0 保持居中；正值向右偏移，负值向左偏移（例如用于避开另一个 HUD 模组在屏幕顶部的元素）。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("水平偏移（像素）", "相對於頂部中心的水平偏移。0 保持置中；正值向右偏移，負值向左偏移（例如用於避開另一個 HUD 模組在螢幕頂部的元素）。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("水平オフセット（ピクセル）", "画面上部中央からの水平オフセット。0で中央に配置。正の値で右へ、負の値で左へ移動します（例：他のHUD MODの画面上部要素を避けるため）。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("수평 오프셋(픽셀)", "상단 중앙 기준 수평 오프셋입니다. 0이면 중앙에 유지됩니다. 양수는 오른쪽으로, 음수는 왼쪽으로 이동합니다(예: 다른 HUD 모드의 화면 상단 요소를 피하기 위해)."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("PRZESUNIĘCIE POZIOME W PIKSELACH", "Przesunięcie poziome od górnego środka. 0 utrzymuje wyśrodkowanie; wartość dodatnia przesuwa w prawo, ujemna w lewo (np. aby uniknąć elementu innego moda HUD u góry ekranu)."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("YATAY KAYDIRMA (PİKSEL)", "Üst-orta noktadan yatay kayma. 0 ortalanmış tutar; pozitif sağa, negatif sola kaydırır (örneğin başka bir HUD modunun ekran üstü öğesinden kaçınmak için)."),
            });

            Add("fov-degrees", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("FOV DEGREES", "How much of the horizon (in degrees) is visible on the tape at once before a heading/marker slides off the edge. Lower feels closer to your actual view frustum; higher gives more lead time for things approaching from the side."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("CHAMP DE VISION EN DEGRÉS", "Quelle portion de l'horizon (en degrés) est visible sur la bande à la fois avant qu'un cap/marqueur ne glisse hors du bord. Plus bas se rapproche de votre frustum de vue réel ; plus haut donne plus de temps d'anticipation pour ce qui approche sur le côté."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("CAMPO VISIVO IN GRADI", "Quanta parte dell'orizzonte (in gradi) è visibile sulla barra alla volta prima che una direzione/marcatore scivoli fuori dal bordo. Più basso è più vicino al tuo frustum visivo reale; più alto dà più anticipo per le cose che si avvicinano di lato."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("SICHTFELD IN GRAD", "Wie viel vom Horizont (in Grad) gleichzeitig auf dem Band sichtbar ist, bevor eine Richtung/Markierung über den Rand hinausgleitet. Niedriger fühlt sich näher an deinem tatsächlichen Sichtfeld an; höher gibt mehr Vorlaufzeit für Dinge, die von der Seite kommen."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("CAMPO DE VISIÓN EN GRADOS", "Cuánto del horizonte (en grados) es visible en la cinta a la vez antes de que un rumbo/marcador se deslice fuera del borde. Más bajo se siente más cercano a tu frustum de visión real; más alto da más tiempo de anticipación para lo que se acerca de lado."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("CAMPO DE VISIÓN EN GRADOS", "Cuánto del horizonte (en grados) es visible en la cinta a la vez antes de que un rumbo/marcador se deslice fuera del borde. Más bajo se siente más cercano a tu frustum de visión real; más alto da más tiempo de anticipación para lo que se acerca de lado."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("CAMPO DE VISÃO EM GRAUS", "Quanto do horizonte (em graus) é visível na fita de uma vez antes de um rumo/marcador deslizar para fora da borda. Mais baixo parece mais próximo do seu frustum de visão real; mais alto dá mais tempo de antecedência para o que se aproxima pela lateral."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("УГОЛ ОБЗОРА В ГРАДУСАХ", "Сколько горизонта (в градусах) видно на ленте одновременно, прежде чем курс/метка соскользнёт за край. Меньше — ближе к реальному углу обзора камеры; больше — больше времени на реакцию для приближающегося сбоку."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("КУТ ОГЛЯДУ В ГРАДУСАХ", "Скільки горизонту (у градусах) видно на стрічці одночасно, перш ніж курс/мітка зісковзне за край. Менше — ближче до реального кута огляду камери; більше — більше часу на реакцію для того, що наближається збоку."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("视野角度", "条带上一次可见多少地平线范围（以度为单位），超出后方位/标记会滑出边缘。数值越低越接近你实际的视锥；数值越高，对从侧面靠近的目标提供的提前预警时间越多。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("視野角度", "條帶上一次可見多少地平線範圍（以度為單位），超出後方位/標記會滑出邊緣。數值越低越接近你實際的視錐；數值越高，對從側面靠近的目標提供的提前預警時間越多。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("視野角（度）", "方位やマーカーが端にスライドして消える前に、バー上に一度に表示される地平線の範囲（度数）。低くすると実際の視野に近くなり、高くすると横から近づくものへの猶予が増えます。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("시야각(도)", "방위/마커가 가장자리로 사라지기 전까지 띠에 한 번에 표시되는 지평선 범위(도 단위)입니다. 낮을수록 실제 시야각에 가깝게 느껴지고, 높을수록 옆에서 다가오는 대상에 대한 여유 시간이 늘어납니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("POLE WIDZENIA W STOPNIACH", "Ile horyzontu (w stopniach) jest widoczne na pasku naraz, zanim kierunek/znacznik zsunie się poza krawędź. Niższa wartość jest bliższa rzeczywistemu polu widzenia; wyższa daje więcej czasu na reakcję na coś zbliżającego się z boku."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("GÖRÜŞ ALANI (DERECE)", "Bir yön/işaretçi kenardan kayıp gitmeden önce şeritte aynı anda ne kadar ufkun (derece cinsinden) görünür olduğu. Düşük değer gerçek görüş alanınıza daha yakın hissettirir; yüksek değer, yandan yaklaşan şeyler için daha fazla önceden haber verir."),
            });

            Add("icon-size-pixels", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ICON SIZE PIXELS", "Size of each marker's icon on the compass."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("TAILLE D'ICÔNE EN PIXELS", "Taille de l'icône de chaque marqueur sur la boussole."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("DIMENSIONE ICONA IN PIXEL", "Dimensione dell'icona di ciascun marcatore sulla bussola."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("SYMBOLGRÖSSE IN PIXELN", "Größe des Symbols jeder Markierung auf dem Kompass."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("TAMAÑO DE ICONO EN PÍXELES", "Tamaño del icono de cada marcador en la brújula."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("TAMAÑO DE ÍCONO EN PÍXELES", "Tamaño del ícono de cada marcador en la brújula."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("TAMANHO DO ÍCONE EM PIXELS", "Tamanho do ícone de cada marcador na bússola."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("РАЗМЕР ЗНАЧКА В ПИКСЕЛЯХ", "Размер значка каждой метки на компасе."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("РОЗМІР ЗНАЧКА В ПІКСЕЛЯХ", "Розмір значка кожної мітки на компасі."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("图标大小（像素）", "指南针上每个标记图标的大小。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("圖示大小（像素）", "指南針上每個標記圖示的大小。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("アイコンサイズ（ピクセル）", "コンパス上の各マーカーアイコンのサイズ。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("아이콘 크기(픽셀)", "나침반에 표시되는 각 마커 아이콘의 크기입니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("ROZMIAR IKONY W PIKSELACH", "Rozmiar ikony każdego znacznika na kompasie."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("SİMGE BOYUTU (PİKSEL)", "Pusuladaki her işaretçinin simge boyutu."),
            });

            Add("elevation-threshold-meters", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ELEVATION THRESHOLD METERS", "A marker only gets an up/down elevation arrow once its target is at least this many meters above/below you, which avoids a flickering arrow for things that are roughly level with you."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("SEUIL D'ÉLÉVATION EN MÈTRES", "Un marqueur n'obtient une flèche d'élévation haut/bas que lorsque sa cible est au moins à cette distance en mètres au-dessus/en dessous de vous, ce qui évite une flèche clignotante pour ce qui est à peu près à votre niveau."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SOGLIA DI ELEVAZIONE IN METRI", "Un marcatore riceve una freccia di elevazione su/giù solo quando il suo bersaglio è almeno a questa distanza in metri sopra/sotto di te, il che evita una freccia sfarfallante per cose più o meno al tuo livello."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("HÖHENSCHWELLE IN METERN", "Eine Markierung erhält nur dann einen Auf-/Ab-Höhenpfeil, wenn ihr Ziel mindestens so viele Meter über/unter dir ist, was einen flackernden Pfeil für etwa gleich hohe Dinge vermeidet."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("UMBRAL DE ELEVACIÓN EN METROS", "Un marcador solo obtiene una flecha de elevación arriba/abajo cuando su objetivo está al menos a esta distancia en metros por encima/debajo de ti, lo que evita una flecha parpadeante para algo a tu mismo nivel."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("UMBRAL DE ELEVACIÓN EN METROS", "Un marcador solo obtiene una flecha de elevación arriba/abajo cuando su objetivo está al menos a esta distancia en metros por encima/debajo de ti, lo que evita una flecha parpadeante para algo a tu mismo nivel."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("LIMITE DE ELEVAÇÃO EM METROS", "Um marcador só recebe uma seta de elevação para cima/baixo quando seu alvo está a pelo menos essa quantidade de metros acima/abaixo de você, o que evita uma seta piscando para algo praticamente no seu nível."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ПОРОГ ВЫСОТЫ В МЕТРАХ", "Метка получает стрелку высоты вверх/вниз только когда её цель находится как минимум на столько метров выше/ниже вас, что предотвращает мерцание стрелки для объектов примерно на вашем уровне."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ПОРІГ ВИСОТИ В МЕТРАХ", "Мітка отримує стрілку висоти вгору/вниз лише коли її ціль перебуває щонайменше на стільки метрів вище/нижче за вас, що запобігає миготінню стрілки для об'єктів приблизно на вашому рівні."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("高度阈值（米）", "只有当目标高于/低于你至少这么多米时，标记才会显示上/下高度箭头，这样可以避免与你大致同高的目标出现箭头闪烁。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("高度閾值（公尺）", "只有當目標高於/低於你至少這麼多公尺時，標記才會顯示上/下高度箭頭，這樣可以避免與你大致同高的目標出現箭頭閃爍。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("高度しきい値（メートル）", "対象がこのメートル数以上あなたより上/下にある場合にのみ、マーカーに上下の高度矢印が表示されます。ほぼ同じ高さのものに対して矢印がちらつくのを防ぎます。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("고도 임계값(미터)", "대상이 당신보다 최소 이만큼 미터 위/아래에 있을 때만 마커에 상/하 고도 화살표가 표시되며, 이는 대략 같은 높이에 있는 대상에 대해 화살표가 깜박이는 것을 방지합니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("PRÓG WYSOKOŚCI W METRACH", "Znacznik otrzymuje strzałkę wysokości w górę/dół tylko wtedy, gdy jego cel znajduje się co najmniej tyle metrów nad/pod tobą, co zapobiega migotaniu strzałki dla obiektów na mniej więcej twoim poziomie."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("YÜKSEKLİK EŞİĞİ (METRE)", "Bir işaretçi, yalnızca hedefi sizden en az bu kadar metre yukarıda/aşağıda olduğunda yukarı/aşağı yükseklik oku alır; bu, sizinle yaklaşık aynı seviyedeki şeyler için okun titremesini önler."),
            });

            Add("show-degree-numbers", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("SHOW DEGREE NUMBERS", "Show a numeric heading (e.g. \"105\") at every non-cardinal tick instead of leaving it as a plain unlabeled line. N/E/S/W are always lettered either way."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("AFFICHER LES DEGRÉS", "Affiche un cap numérique (par ex. « 105 ») à chaque graduation non cardinale au lieu de la laisser comme une simple ligne sans étiquette. N/E/S/O sont toujours lettrés de toute façon."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("MOSTRA NUMERI DI GRADI", "Mostra una direzione numerica (ad es. \"105\") su ogni tacca non cardinale invece di lasciarla come una semplice linea senza etichetta. N/E/S/O sono sempre lettere comunque."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("GRADZAHLEN ANZEIGEN", "Zeigt eine numerische Richtung (z. B. „105“) an jedem nicht-kardinalen Strich an, statt sie als einfache unbeschriftete Linie zu belassen. N/O/S/W werden ohnehin immer mit Buchstaben angezeigt."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("MOSTRAR NÚMEROS DE GRADOS", "Muestra un rumbo numérico (por ejemplo, «105») en cada marca no cardinal en lugar de dejarla como una simple línea sin etiqueta. N/E/S/O siempre llevan letras de todos modos."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("MOSTRAR NÚMEROS DE GRADOS", "Muestra un rumbo numérico (por ejemplo, «105») en cada marca no cardinal en lugar de dejarla como una simple línea sin etiqueta. N/E/S/O siempre llevan letras de todos modos."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("MOSTRAR NÚMEROS DE GRAUS", "Mostra um rumo numérico (ex.: \"105\") em cada marca não cardinal em vez de deixá-la como uma linha simples sem rótulo. N/L/S/O sempre têm letras de qualquer forma."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ПОКАЗЫВАТЬ ГРАДУСЫ ЧИСЛАМИ", "Показывает числовой курс (например, «105») на каждой некардинальной отметке вместо простой линии без подписи. С/В/Ю/З всегда обозначены буквами в любом случае."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ПОКАЗУВАТИ ГРАДУСИ ЧИСЛАМИ", "Показує числовий курс (наприклад, «105») на кожній некардинальній позначці замість простої лінії без підпису. Пн/Сх/Пд/Зх завжди позначені літерами в будь-якому разі."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("显示度数数字", "在每个非基本方位刻度处显示数字方位（例如“105”），而不是留一条无标签的普通线。N/E/S/W 无论如何都始终以字母标注。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("顯示度數數字", "在每個非基本方位刻度處顯示數字方位（例如「105」），而不是留一條無標籤的普通線。N/E/S/W 無論如何都始終以字母標註。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("度数を表示", "基本方位以外の目盛りに、ラベルなしの線のままにせず数値方位（例：「105」）を表示します。N/E/S/Wはいずれにせよ常に文字表記です。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("도수 표시", "기본 방위가 아닌 눈금마다 라벨 없는 단순한 선으로 두는 대신 숫자 방위(예: \"105\")를 표시합니다. N/E/S/W는 어떤 경우든 항상 문자로 표시됩니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("POKAZUJ LICZBY STOPNI", "Pokazuje numeryczny kierunek (np. „105”) przy każdej niekardynalnej kresce zamiast pozostawiać ją jako zwykłą linię bez etykiety. N/E/S/W zawsze mają litery niezależnie od tego."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("DERECE NUMARALARINI GÖSTER", "Ana yön olmayan her çentikte, etiketsiz düz bir çizgi bırakmak yerine sayısal bir yön (örn. \"105\") gösterir. K/D/G/B her durumda her zaman harflerle gösterilir."),
            });

            Add("show-names", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("SHOW NAMES", "Show a name label above each compass marker that has one (players, item/creature pings, the campfire). Off by default to keep the tape simple; distances still show independently of this setting."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("AFFICHER LES NOMS", "Affiche une étiquette de nom au-dessus de chaque marqueur de boussole qui en a un (joueurs, pings d'objets/créatures, le feu de camp). Désactivé par défaut pour garder la bande simple ; les distances s'affichent toujours indépendamment de ce réglage."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("MOSTRA NOMI", "Mostra un'etichetta con il nome sopra ogni marcatore della bussola che ne ha uno (giocatori, ping di oggetti/creature, il falò). Disattivato per impostazione predefinita per mantenere la barra semplice; le distanze vengono comunque mostrate indipendentemente da questa impostazione."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("NAMEN ANZEIGEN", "Zeigt ein Namensschild über jeder Kompassmarkierung an, die eines hat (Spieler, Gegenstands-/Kreaturen-Pings, das Lagerfeuer). Standardmäßig deaktiviert, um das Band einfach zu halten; Entfernungen werden unabhängig von dieser Einstellung weiterhin angezeigt."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("MOSTRAR NOMBRES", "Muestra una etiqueta de nombre sobre cada marcador de brújula que tenga uno (jugadores, pings de objetos/criaturas, la hoguera). Desactivado por defecto para mantener la cinta simple; las distancias se siguen mostrando independientemente de este ajuste."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("MOSTRAR NOMBRES", "Muestra una etiqueta de nombre sobre cada marcador de brújula que tenga uno (jugadores, pings de objetos/criaturas, la hoguera). Desactivado por defecto para mantener la cinta simple; las distancias se siguen mostrando independientemente de este ajuste."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("MOSTRAR NOMES", "Mostra um rótulo de nome acima de cada marcador de bússola que tenha um (jogadores, pings de itens/criaturas, a fogueira). Desativado por padrão para manter a fita simples; as distâncias continuam sendo exibidas independentemente dessa configuração."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ПОКАЗЫВАТЬ ИМЕНА", "Показывает подпись с именем над каждой меткой компаса, у которой она есть (игроки, пинги предметов/существ, костёр). Выключено по умолчанию, чтобы лента оставалась простой; расстояния всё равно показываются независимо от этой настройки."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ПОКАЗУВАТИ ІМЕНА", "Показує підпис з іменем над кожною міткою компаса, яка його має (гравці, пінги предметів/істот, багаття). Вимкнено за замовчуванням, щоб стрічка залишалася простою; відстані все одно показуються незалежно від цього налаштування."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("显示名称", "在每个具有名称的指南针标记上方显示名称标签（玩家、物品/生物呼喊、篝火）。默认关闭以保持条带简洁；距离仍会独立于此设置显示。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("顯示名稱", "在每個具有名稱的指南針標記上方顯示名稱標籤（玩家、物品/生物呼喊、篝火）。預設關閉以保持條帶簡潔；距離仍會獨立於此設定顯示。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("名前を表示", "名前を持つ各コンパスマーカー（プレイヤー、アイテム/クリーチャーピン、焚き火）の上に名前ラベルを表示します。バーをシンプルに保つためデフォルトはオフです。距離はこの設定とは無関係に表示されます。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("이름 표시", "이름이 있는 각 나침반 마커(플레이어, 아이템/생물 핑, 모닥불) 위에 이름 라벨을 표시합니다. 띠를 단순하게 유지하기 위해 기본적으로 꺼져 있습니다. 거리는 이 설정과 무관하게 계속 표시됩니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("POKAZUJ NAZWY", "Pokazuje etykietę z nazwą nad każdym znacznikiem kompasu, który ją posiada (gracze, pingi przedmiotów/stworzeń, ognisko). Domyślnie wyłączone, aby pasek pozostał prosty; odległości nadal są pokazywane niezależnie od tego ustawienia."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("İSİMLERİ GÖSTER", "İsmi olan her pusula işaretçisinin (oyuncular, eşya/yaratık pingleri, kamp ateşi) üzerinde bir isim etiketi gösterir. Şeridi sade tutmak için varsayılan olarak kapalıdır; mesafeler bu ayardan bağımsız olarak gösterilmeye devam eder."),
            });

            Add("show-distances", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("SHOW DISTANCES", "Show a distance sub-label under each compass marker."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("AFFICHER LES DISTANCES", "Affiche une sous-étiquette de distance sous chaque marqueur de boussole."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("MOSTRA DISTANZE", "Mostra una sotto-etichetta di distanza sotto ogni marcatore della bussola."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("ENTFERNUNGEN ANZEIGEN", "Zeigt eine Entfernungs-Unterbeschriftung unter jeder Kompassmarkierung an."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("MOSTRAR DISTANCIAS", "Muestra una subetiqueta de distancia bajo cada marcador de brújula."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("MOSTRAR DISTANCIAS", "Muestra una subetiqueta de distancia bajo cada marcador de brújula."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("MOSTRAR DISTÂNCIAS", "Mostra um sub-rótulo de distância abaixo de cada marcador de bússola."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ПОКАЗЫВАТЬ РАССТОЯНИЯ", "Показывает подпись расстояния под каждой меткой компаса."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ПОКАЗУВАТИ ВІДСТАНІ", "Показує підпис відстані під кожною міткою компаса."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("显示距离", "在每个指南针标记下方显示距离子标签。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("顯示距離", "在每個指南針標記下方顯示距離子標籤。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("距離を表示", "各コンパスマーカーの下に距離のサブラベルを表示します。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("거리 표시", "각 나침반 마커 아래에 거리 하위 라벨을 표시합니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("POKAZUJ ODLEGŁOŚCI", "Pokazuje podetykietę odległości pod każdym znacznikiem kompasu."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("MESAFELERİ GÖSTER", "Her pusula işaretçisinin altında bir mesafe alt etiketi gösterir."),
            });

            Add("requires-holding-item", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("REQUIRES HOLDING ITEM", "Only show the compass tape while the local player is actually holding an in-game Compass item, instead of it always being visible. Off by default."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("NÉCESSITE DE TENIR L'OBJET", "N'affiche la bande de boussole que lorsque le joueur local tient réellement un objet Boussole du jeu, au lieu d'être toujours visible. Désactivé par défaut."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("RICHIEDE DI TENERE L'OGGETTO", "Mostra la barra della bussola solo quando il giocatore locale sta effettivamente tenendo un oggetto Bussola di gioco, invece di essere sempre visibile. Disattivato per impostazione predefinita."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("ERFORDERT HALTEN DES GEGENSTANDS", "Zeigt das Kompassband nur an, wenn der lokale Spieler tatsächlich einen Kompass-Gegenstand aus dem Spiel hält, statt immer sichtbar zu sein. Standardmäßig deaktiviert."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("REQUIERE SOSTENER EL OBJETO", "Solo muestra la cinta de brújula mientras el jugador local sostiene realmente un objeto Brújula del juego, en lugar de estar siempre visible. Desactivado por defecto."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("REQUIERE SOSTENER EL OBJETO", "Solo muestra la cinta de brújula mientras el jugador local sostiene realmente un objeto Brújula del juego, en lugar de estar siempre visible. Desactivado por defecto."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("REQUER SEGURAR O ITEM", "Só mostra a fita de bússola enquanto o jogador local está realmente segurando um item Bússola do jogo, em vez de estar sempre visível. Desativado por padrão."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ТРЕБУЕТ ДЕРЖАТЬ ПРЕДМЕТ", "Показывает ленту компаса только пока локальный игрок действительно держит игровой предмет «Компас», вместо постоянной видимости. Выключено по умолчанию."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ПОТРЕБУЄ ТРИМАННЯ ПРЕДМЕТА", "Показує стрічку компаса лише поки локальний гравець дійсно тримає ігровий предмет «Компас», замість постійної видимості. Вимкнено за замовчуванням."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("需要手持物品", "仅当本地玩家实际手持游戏内的指南针物品时才显示指南针条带，而不是始终可见。默认关闭。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("需要手持物品", "僅當本地玩家實際手持遊戲內的指南針物品時才顯示指南針條帶，而不是始終可見。預設關閉。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("アイテム所持が必要", "常に表示するのではなく、ローカルプレイヤーがゲーム内のコンパスアイテムを実際に持っている間だけコンパスバーを表示します。デフォルトはオフです。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("아이템 소지 필요", "항상 표시되는 대신, 로컬 플레이어가 실제로 게임 내 나침반 아이템을 들고 있을 때만 나침반 띠를 표시합니다. 기본적으로 꺼져 있습니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("WYMAGA TRZYMANIA PRZEDMIOTU", "Pokazuje pasek kompasu tylko wtedy, gdy lokalny gracz faktycznie trzyma przedmiot Kompas z gry, zamiast być zawsze widocznym. Domyślnie wyłączone."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("EŞYA TUTMAYI GEREKTİRİR", "Her zaman görünür olmak yerine, pusula şeridini yalnızca yerel oyuncu gerçekten oyun içi bir Pusula eşyasını tutarken gösterir. Varsayılan olarak kapalıdır."),
            });

            Add("line-color", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("LINE COLOR", "Base color of the compass tape's heading ticks/labels and baseline stripe (true north keeps its own dark red accent regardless)."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("COULEUR DES LIGNES", "Couleur de base des graduations/étiquettes de cap et de la bande de référence de la boussole (le nord vrai garde son propre accent rouge foncé dans tous les cas)."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("COLORE DELLE LINEE", "Colore di base delle tacche/etichette di direzione e della striscia di base della bussola (il nord vero mantiene comunque il proprio accento rosso scuro)."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("LINIENFARBE", "Grundfarbe der Richtungsstriche/-beschriftungen und des Grundlinienstreifens des Kompassbands (echter Norden behält in jedem Fall seinen eigenen dunkelroten Akzent)."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("COLOR DE LÍNEA", "Color base de las marcas/etiquetas de rumbo y la franja de línea base de la cinta de brújula (el norte verdadero conserva su propio acento rojo oscuro de todos modos)."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("COLOR DE LÍNEA", "Color base de las marcas/etiquetas de rumbo y la franja de línea base de la cinta de brújula (el norte verdadero conserva su propio acento rojo oscuro de todos modos)."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("COR DA LINHA", "Cor base das marcas/rótulos de rumo e da faixa de linha de base da fita de bússola (o norte verdadeiro mantém seu próprio destaque vermelho escuro de qualquer forma)."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ЦВЕТ ЛИНИЙ", "Базовый цвет делений/подписей курса и базовой полосы ленты компаса (истинный север в любом случае сохраняет свой тёмно-красный акцент)."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("КОЛІР ЛІНІЙ", "Базовий колір поділок/підписів курсу та базової смуги стрічки компаса (справжня північ у будь-якому разі зберігає свій темно-червоний акцент)."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("线条颜色", "指南针条带方位刻度/标签及基线条纹的基础颜色（正北无论如何都保留自己的暗红色强调色）。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("線條顏色", "指南針條帶方位刻度/標籤及基線條紋的基礎顏色（正北無論如何都保留自己的暗紅色強調色）。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("線の色", "コンパスバーの方位目盛り/ラベルと基準線の基本色（真北はいずれにせよ独自の濃い赤のアクセントを保持します）。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("선 색상", "나침반 띠의 방위 눈금/라벨과 기준선 줄무늬의 기본 색상입니다(진북은 어떤 경우든 자체 짙은 빨간색 강조색을 유지합니다)."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("KOLOR LINII", "Podstawowy kolor kresek kierunkowych/etykiet i paska bazowego kompasu (prawdziwa północ zawsze zachowuje swój ciemnoczerwony akcent)."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("ÇİZGİ RENGİ", "Pusula şeridinin yön çentiklerinin/etiketlerinin ve taban çizgisi şeridinin temel rengi (gerçek kuzey her durumda kendi koyu kırmızı vurgusunu korur)."),
            });

            Add("line-thickness-multiplier", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("LINE THICKNESS MULTIPLIER", "Scales the thickness of the compass tape's tick lines (both cardinal and minor) and its baseline stripe. 1 keeps the default thickness; higher values make the lines bolder."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("MULTIPLICATEUR D'ÉPAISSEUR DE LIGNE", "Ajuste l'épaisseur des graduations de la bande de boussole (cardinales et mineures) et de sa bande de référence. 1 garde l'épaisseur par défaut ; des valeurs plus élevées rendent les lignes plus épaisses."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("MOLTIPLICATORE SPESSORE LINEA", "Scala lo spessore delle tacche della barra della bussola (sia cardinali che minori) e della sua striscia di base. 1 mantiene lo spessore predefinito; valori più alti rendono le linee più marcate."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("LINIENSTÄRKE-MULTIPLIKATOR", "Skaliert die Dicke der Strichlinien des Kompassbands (Haupt- und Nebenrichtungen) sowie seines Grundlinienstreifens. 1 behält die Standarddicke bei; höhere Werte machen die Linien fetter."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("MULTIPLICADOR DE GROSOR DE LÍNEA", "Escala el grosor de las marcas de la cinta de brújula (tanto cardinales como menores) y su franja de línea base. 1 mantiene el grosor predeterminado; valores más altos hacen las líneas más gruesas."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("MULTIPLICADOR DE GROSOR DE LÍNEA", "Escala el grosor de las marcas de la cinta de brújula (tanto cardinales como menores) y su franja de línea base. 1 mantiene el grosor predeterminado; valores más altos hacen las líneas más gruesas."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("MULTIPLICADOR DE ESPESSURA DE LINHA", "Escala a espessura das marcas da fita de bússola (tanto cardeais quanto menores) e de sua faixa de linha de base. 1 mantém a espessura padrão; valores mais altos deixam as linhas mais grossas."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("МНОЖИТЕЛЬ ТОЛЩИНЫ ЛИНИЙ", "Масштабирует толщину делений ленты компаса (главных и второстепенных) и её базовой полосы. 1 сохраняет толщину по умолчанию; более высокие значения делают линии жирнее."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("МНОЖНИК ТОВЩИНИ ЛІНІЙ", "Масштабує товщину поділок стрічки компаса (головних і другорядних) та її базової смуги. 1 зберігає товщину за замовчуванням; вищі значення роблять лінії жирнішими."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("线条粗细倍数", "缩放指南针条带刻度线（主要和次要方位）及其基线条纹的粗细。1 保持默认粗细；数值越高线条越粗。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("線條粗細倍數", "縮放指南針條帶刻度線（主要和次要方位）及其基線條紋的粗細。1 保持預設粗細；數值越高線條越粗。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("線の太さ倍率", "コンパスバーの目盛り線（主要・副次方位とも）と基準線の太さを拡大縮小します。1でデフォルトの太さを維持し、値を高くすると線が太くなります。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("선 굵기 배율", "나침반 띠의 눈금선(주요 및 보조 방위 모두)과 기준선 줄무늬의 굵기를 조정합니다. 1은 기본 굵기를 유지하며, 값이 클수록 선이 더 굵어집니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("MNOŻNIK GRUBOŚCI LINII", "Skaluje grubość kresek kompasu (głównych i pomniejszych) oraz paska bazowego. 1 zachowuje domyślną grubość; wyższe wartości pogrubiają linie."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("ÇİZGİ KALINLIĞI ÇARPANI", "Pusula şeridinin çentik çizgilerinin (hem ana hem ikincil) ve taban çizgisi şeridinin kalınlığını ölçekler. 1 varsayılan kalınlığı korur; daha yüksek değerler çizgileri daha kalın yapar."),
            });

            Add("clamp-icons-to-edge", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("CLAMP ICONS TO EDGE", "Markers that would otherwise not be visible (outside the compass FOV window) are instead clamped to the nearest left/right edge of the tape and shown dimmed, like a mini radar, instead of not appearing at all. Off by default."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("FIXER LES ICÔNES AU BORD", "Les marqueurs qui ne seraient sinon pas visibles (hors de la fenêtre de champ de vision de la boussole) sont plutôt fixés au bord gauche/droit le plus proche de la bande et affichés atténués, comme un mini radar, au lieu de ne pas apparaître du tout. Désactivé par défaut."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("BLOCCA ICONE AL BORDO", "I marcatori che altrimenti non sarebbero visibili (fuori dalla finestra del campo visivo della bussola) vengono invece bloccati al bordo sinistro/destro più vicino della barra e mostrati attenuati, come un mini radar, invece di non apparire affatto. Disattivato per impostazione predefinita."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("SYMBOLE AM RAND FIXIEREN", "Markierungen, die sonst nicht sichtbar wären (außerhalb des Kompass-Sichtfelds), werden stattdessen am nächstgelegenen linken/rechten Rand des Bands fixiert und abgedunkelt angezeigt, wie ein Mini-Radar, anstatt gar nicht zu erscheinen. Standardmäßig deaktiviert."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("FIJAR ICONOS AL BORDE", "Los marcadores que de otro modo no serían visibles (fuera de la ventana de campo de visión de la brújula) se fijan en su lugar al borde izquierdo/derecho más cercano de la cinta y se muestran atenuados, como un mini radar, en lugar de no aparecer en absoluto. Desactivado por defecto."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("FIJAR ÍCONOS AL BORDE", "Los marcadores que de otro modo no serían visibles (fuera de la ventana de campo de visión de la brújula) se fijan en su lugar al borde izquierdo/derecho más cercano de la cinta y se muestran atenuados, como un mini radar, en lugar de no aparecer en absoluto. Desactivado por defecto."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("FIXAR ÍCONES NA BORDA", "Marcadores que de outra forma não seriam visíveis (fora da janela de campo de visão da bússola) são fixados na borda esquerda/direita mais próxima da fita e exibidos escurecidos, como um mini radar, em vez de não aparecerem de forma alguma. Desativado por padrão."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ПРИЖИМАТЬ ЗНАЧКИ К КРАЮ", "Метки, которые иначе были бы невидимы (вне окна обзора компаса), вместо этого прижимаются к ближайшему левому/правому краю ленты и показываются затемнёнными, как мини-радар, а не пропадают полностью. Выключено по умолчанию."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ПРИТИСКАТИ ЗНАЧКИ ДО КРАЮ", "Мітки, які інакше були б невидимі (поза вікном огляду компаса), натомість притискаються до найближчого лівого/правого краю стрічки й показуються затемненими, як міні-радар, замість того щоб зникати повністю. Вимкнено за замовчуванням."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("将图标固定到边缘", "原本不可见的标记（超出指南针视野窗口）会被固定到条带最近的左/右边缘并以变暗方式显示，如同一个迷你雷达，而不是完全不显示。默认关闭。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("將圖示固定到邊緣", "原本不可見的標記（超出指南針視野視窗）會被固定到條帶最近的左/右邊緣並以變暗方式顯示，如同一個迷你雷達，而不是完全不顯示。預設關閉。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("アイコンを端に固定", "本来なら表示されないマーカー（コンパスの視野範囲外）を、まったく表示しない代わりに、バーの最も近い左右の端に固定してミニレーダーのように減光表示します。デフォルトはオフです。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("아이콘을 가장자리에 고정", "그렇지 않으면 보이지 않을 마커(나침반 시야 범위 밖)는 전혀 표시되지 않는 대신, 미니 레이더처럼 어둡게 표시된 채 띠의 가장 가까운 좌/우 가장자리에 고정됩니다. 기본적으로 꺼져 있습니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("PRZYPNIJ IKONY DO KRAWĘDZI", "Znaczniki, które w innym przypadku byłyby niewidoczne (poza oknem pola widzenia kompasu), są zamiast tego przypinane do najbliższej lewej/prawej krawędzi paska i wyświetlane przyciemnione, jak mini radar, zamiast w ogóle się nie pojawiać. Domyślnie wyłączone."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("SİMGELERİ KENARA SABİTLE", "Aksi takdirde görünmeyecek işaretçiler (pusula görüş alanı penceresinin dışında) hiç görünmemek yerine, şeridin en yakın sol/sağ kenarına sabitlenir ve mini bir radar gibi soluk gösterilir. Varsayılan olarak kapalıdır."),
            });
        }
    }
}
