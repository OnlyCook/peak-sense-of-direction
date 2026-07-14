using System.Collections.Generic;

namespace SenseOfDirection.Ui.Localization.Config
{
    /// <summary>Localized names/descriptions for every "Player-Labels" section config entry.</summary>
    internal static class PlayerLabelsConfigLocalization
    {
        internal static void Register(ConfigLocalizationTable.Registry registry)
        {
            void Add(string key, Dictionary<LocalizedText.Language, ConfigLocalizationEntry> table) =>
                registry.Add("Player-Labels", key, table);

            Add("enable-player-labels", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ENABLE PLAYER LABELS", "Master switch for Sense of Direction's player labels. Off hides them entirely (vanilla's own name labels are unaffected either way)."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ACTIVER LES LABELS JOUEURS", "Interrupteur principal des labels de joueurs de Sense of Direction. Désactivé, ils sont entièrement masqués (les labels de nom d'origine du jeu ne sont affectés dans aucun des deux cas)."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("ATTIVA ETICHETTE GIOCATORI", "Interruttore principale per le etichette giocatori di Sense of Direction. Disattivato, le nasconde completamente (le etichette dei nomi originali del gioco non sono comunque interessate)."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("SPIELERNAMEN AKTIVIEREN", "Hauptschalter für die Spielernamen von Sense of Direction. Deaktiviert, blendet sie vollständig aus (die originalen Namens-Labels des Spiels sind so oder so nicht betroffen)."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ACTIVAR ETIQUETAS DE JUGADOR", "Interruptor principal para las etiquetas de jugador de Sense of Direction. Desactivado, las oculta por completo (las etiquetas de nombre originales del juego no se ven afectadas en ningún caso)."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ACTIVAR ETIQUETAS DE JUGADOR", "Interruptor principal para las etiquetas de jugador de Sense of Direction. Desactivado, las oculta por completo (las etiquetas de nombre originales del juego no se ven afectadas en ningún caso)."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ATIVAR RÓTULOS DE JOGADOR", "Interruptor principal dos rótulos de jogador do Sense of Direction. Desativado, oculta-os completamente (os rótulos de nome originais do jogo não são afetados de qualquer forma)."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ВКЛЮЧИТЬ МЕТКИ ИГРОКОВ", "Главный переключатель меток игроков Sense of Direction. При выключении они полностью скрываются (родные метки имён игры при этом не затрагиваются в любом случае)."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("УВІМКНУТИ МІТКИ ГРАВЦІВ", "Головний перемикач міток гравців Sense of Direction. При вимкненні вони повністю приховуються (рідні мітки імен гри при цьому не змінюються в будь-якому разі)."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("启用玩家标签", "Sense of Direction 玩家标签的总开关。关闭后完全隐藏（无论如何都不影响游戏原生的姓名标签）。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("啟用玩家標籤", "Sense of Direction 玩家標籤的總開關。關閉後完全隱藏（無論如何都不影響遊戲原生的姓名標籤）。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("プレイヤーラベルを有効化", "Sense of Direction のプレイヤーラベルのマスタースイッチです。オフにすると完全に非表示になります（いずれにせよバニラ本来の名前ラベルには影響しません）。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("플레이어 라벨 활성화", "Sense of Direction 플레이어 라벨의 마스터 스위치입니다. 끄면 완전히 숨겨집니다(어느 쪽이든 바닐라 자체의 이름 라벨에는 영향을 주지 않습니다)."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("WŁĄCZ ETYKIETY GRACZY", "Główny przełącznik etykiet graczy Sense of Direction. Po wyłączeniu są całkowicie ukryte (oryginalne etykiety nazw gry nie są w żaden sposób naruszone)."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("OYUNCU ETİKETLERİNİ ETKİNLEŞTİR", "Sense of Direction'ın oyuncu etiketleri için ana anahtar. Kapalıyken tamamen gizlenirler (oyunun kendi isim etiketleri her iki durumda da etkilenmez)."),
            });

            Add("toggle-key", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("TOGGLE KEY", "Key that shows/hides player labels, per {key:Player-Labels/display-mode} below. Only a single key can be bound here, not a combination like Ctrl+G."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("TOUCHE DE BASCULE", "Touche qui affiche/masque les labels de joueurs, selon {key:Player-Labels/display-mode} ci-dessous. Seule une touche unique peut être assignée ici, pas une combinaison comme Ctrl+G."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("TASTO ATTIVA/DISATTIVA", "Tasto che mostra/nasconde le etichette dei giocatori, in base a {key:Player-Labels/display-mode} qui sotto. Qui può essere assegnato solo un singolo tasto, non una combinazione come Ctrl+G."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("UMSCHALTTASTE", "Taste, die Spielernamen ein-/ausblendet, gemäß {key:Player-Labels/display-mode} unten. Hier kann nur eine einzelne Taste belegt werden, keine Kombination wie Strg+G."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("TECLA DE ALTERNANCIA", "Tecla que muestra/oculta las etiquetas de jugador, según {key:Player-Labels/display-mode} más abajo. Aquí solo se puede asignar una tecla única, no una combinación como Ctrl+G."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("TECLA DE ALTERNANCIA", "Tecla que muestra/oculta las etiquetas de jugador, según {key:Player-Labels/display-mode} más abajo. Aquí solo se puede asignar una tecla única, no una combinación como Ctrl+G."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("TECLA DE ALTERNÂNCIA", "Tecla que mostra/oculta os rótulos de jogador, conforme {key:Player-Labels/display-mode} abaixo. Aqui só é possível atribuir uma única tecla, não uma combinação como Ctrl+G."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("КЛАВИША ПЕРЕКЛЮЧЕНИЯ", "Клавиша, показывающая/скрывающая метки игроков, согласно {key:Player-Labels/display-mode} ниже. Здесь можно назначить только одну клавишу, а не сочетание вроде Ctrl+G."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("КЛАВІША ПЕРЕМИКАННЯ", "Клавіша, що показує/приховує мітки гравців, згідно з {key:Player-Labels/display-mode} нижче. Тут можна призначити лише одну клавішу, а не поєднання на кшталт Ctrl+G."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("切换按键", "根据下方 {key:Player-Labels/display-mode} 显示/隐藏玩家标签的按键。这里只能绑定单个按键，不能绑定像 Ctrl+G 这样的组合键。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("切換按鍵", "根據下方 {key:Player-Labels/display-mode} 顯示/隱藏玩家標籤的按鍵。這裡只能綁定單一按鍵，不能綁定像 Ctrl+G 這樣的組合鍵。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("切り替えキー", "下記の {key:Player-Labels/display-mode} に従ってプレイヤーラベルを表示/非表示にするキーです。ここでは単一のキーのみ割り当て可能で、Ctrl+G のような組み合わせは使えません。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("전환 키", "아래 {key:Player-Labels/display-mode}에 따라 플레이어 라벨을 표시/숨김하는 키입니다. 여기에는 단일 키만 지정할 수 있으며, Ctrl+G와 같은 조합키는 사용할 수 없습니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("KLAWISZ PRZEŁĄCZANIA", "Klawisz pokazujący/ukrywający etykiety graczy, zgodnie z {key:Player-Labels/display-mode} poniżej. Można tu przypisać tylko pojedynczy klawisz, a nie kombinację taką jak Ctrl+G."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("AÇMA/KAPAMA TUŞU", "Aşağıdaki {key:Player-Labels/display-mode}'a göre oyuncu etiketlerini gösterir/gizler. Buraya yalnızca tek bir tuş atanabilir, Ctrl+G gibi bir kombinasyon atanamaz."),
            });

            Add("display-mode", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("DISPLAY MODE", "{enumval:LabelDisplayMode.Toggle}: press {key:Player-Labels/toggle-key} to show/hide labels. {enumval:LabelDisplayMode.AlwaysOn}: labels are always visible ({key:Player-Labels/toggle-key} does nothing). {enumval:LabelDisplayMode.Hold}: labels show while {key:Player-Labels/toggle-key} is held down."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("MODE D'AFFICHAGE", "{enumval:LabelDisplayMode.Toggle} : appuyez sur {key:Player-Labels/toggle-key} pour afficher/masquer les labels. {enumval:LabelDisplayMode.AlwaysOn} : les labels sont toujours visibles ({key:Player-Labels/toggle-key} ne fait rien). {enumval:LabelDisplayMode.Hold} : les labels s'affichent tant que {key:Player-Labels/toggle-key} est maintenue."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("MODALITÀ VISUALIZZAZIONE", "{enumval:LabelDisplayMode.Toggle}: premi {key:Player-Labels/toggle-key} per mostrare/nascondere le etichette. {enumval:LabelDisplayMode.AlwaysOn}: le etichette sono sempre visibili ({key:Player-Labels/toggle-key} non fa nulla). {enumval:LabelDisplayMode.Hold}: le etichette sono visibili finché {key:Player-Labels/toggle-key} è tenuto premuto."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("ANZEIGEMODUS", "{enumval:LabelDisplayMode.Toggle}: {key:Player-Labels/toggle-key} drücken, um Labels ein-/auszublenden. {enumval:LabelDisplayMode.AlwaysOn}: Labels sind immer sichtbar ({key:Player-Labels/toggle-key} bewirkt nichts). {enumval:LabelDisplayMode.Hold}: Labels werden angezeigt, solange {key:Player-Labels/toggle-key} gedrückt gehalten wird."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("MODO DE VISUALIZACIÓN", "{enumval:LabelDisplayMode.Toggle}: pulsa {key:Player-Labels/toggle-key} para mostrar/ocultar las etiquetas. {enumval:LabelDisplayMode.AlwaysOn}: las etiquetas están siempre visibles ({key:Player-Labels/toggle-key} no hace nada). {enumval:LabelDisplayMode.Hold}: las etiquetas se muestran mientras se mantiene pulsada {key:Player-Labels/toggle-key}."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("MODO DE VISUALIZACIÓN", "{enumval:LabelDisplayMode.Toggle}: presiona {key:Player-Labels/toggle-key} para mostrar/ocultar las etiquetas. {enumval:LabelDisplayMode.AlwaysOn}: las etiquetas están siempre visibles ({key:Player-Labels/toggle-key} no hace nada). {enumval:LabelDisplayMode.Hold}: las etiquetas se muestran mientras se mantiene presionada {key:Player-Labels/toggle-key}."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("MODO DE EXIBIÇÃO", "{enumval:LabelDisplayMode.Toggle}: pressione {key:Player-Labels/toggle-key} para mostrar/ocultar os rótulos. {enumval:LabelDisplayMode.AlwaysOn}: os rótulos ficam sempre visíveis ({key:Player-Labels/toggle-key} não faz nada). {enumval:LabelDisplayMode.Hold}: os rótulos aparecem enquanto {key:Player-Labels/toggle-key} é mantida pressionada."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("РЕЖИМ ОТОБРАЖЕНИЯ", "{enumval:LabelDisplayMode.Toggle}: нажмите {key:Player-Labels/toggle-key}, чтобы показать/скрыть метки. {enumval:LabelDisplayMode.AlwaysOn}: метки всегда видны ({key:Player-Labels/toggle-key} ничего не делает). {enumval:LabelDisplayMode.Hold}: метки показываются, пока зажата {key:Player-Labels/toggle-key}."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("РЕЖИМ ВІДОБРАЖЕННЯ", "{enumval:LabelDisplayMode.Toggle}: натисніть {key:Player-Labels/toggle-key}, щоб показати/приховати мітки. {enumval:LabelDisplayMode.AlwaysOn}: мітки завжди видимі ({key:Player-Labels/toggle-key} нічого не робить). {enumval:LabelDisplayMode.Hold}: мітки показуються, поки утримується {key:Player-Labels/toggle-key}."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("显示模式", "{enumval:LabelDisplayMode.Toggle}：按下 {key:Player-Labels/toggle-key} 以显示/隐藏标签。{enumval:LabelDisplayMode.AlwaysOn}：标签始终可见（{key:Player-Labels/toggle-key} 无效）。{enumval:LabelDisplayMode.Hold}：按住 {key:Player-Labels/toggle-key} 时显示标签。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("顯示模式", "{enumval:LabelDisplayMode.Toggle}：按下 {key:Player-Labels/toggle-key} 以顯示/隱藏標籤。{enumval:LabelDisplayMode.AlwaysOn}：標籤始終可見（{key:Player-Labels/toggle-key} 無效）。{enumval:LabelDisplayMode.Hold}：按住 {key:Player-Labels/toggle-key} 時顯示標籤。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("表示モード", "{enumval:LabelDisplayMode.Toggle}: {key:Player-Labels/toggle-key} を押してラベルの表示/非表示を切り替えます。{enumval:LabelDisplayMode.AlwaysOn}: ラベルは常に表示されます({key:Player-Labels/toggle-key} は何もしません)。{enumval:LabelDisplayMode.Hold}: {key:Player-Labels/toggle-key} を押している間だけラベルが表示されます。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("표시 모드", "{enumval:LabelDisplayMode.Toggle}: {key:Player-Labels/toggle-key}를 눌러 라벨을 표시/숨김합니다. {enumval:LabelDisplayMode.AlwaysOn}: 라벨이 항상 표시됩니다({key:Player-Labels/toggle-key}는 아무 동작도 하지 않음). {enumval:LabelDisplayMode.Hold}: {key:Player-Labels/toggle-key}를 누르고 있는 동안 라벨이 표시됩니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("TRYB WYŚWIETLANIA", "{enumval:LabelDisplayMode.Toggle}: naciśnij {key:Player-Labels/toggle-key}, aby pokazać/ukryć etykiety. {enumval:LabelDisplayMode.AlwaysOn}: etykiety są zawsze widoczne ({key:Player-Labels/toggle-key} nic nie robi). {enumval:LabelDisplayMode.Hold}: etykiety są widoczne, gdy {key:Player-Labels/toggle-key} jest przytrzymywany."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("GÖRÜNTÜLEME MODU", "{enumval:LabelDisplayMode.Toggle}: etiketleri göstermek/gizlemek için {key:Player-Labels/toggle-key}'e basın. {enumval:LabelDisplayMode.AlwaysOn}: etiketler her zaman görünürdür ({key:Player-Labels/toggle-key} hiçbir şey yapmaz). {enumval:LabelDisplayMode.Hold}: {key:Player-Labels/toggle-key} basılı tutulduğu sürece etiketler gösterilir."),
            });

            Add("hold-shown-duration", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("HOLD SHOWN DURATION", "{enumval:LabelDisplayMode.Hold} mode only: how many seconds labels stay visible after the key is released (also covers a quick tap, since this timer is set on press, not on release)."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("DURÉE D'AFFICHAGE EN MODE MAINTIEN", "Uniquement en mode {enumval:LabelDisplayMode.Hold} : combien de secondes les labels restent visibles après le relâchement de la touche (couvre aussi un appui bref, puisque ce minuteur démarre à l'appui, pas au relâchement)."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("DURATA VISUALIZZAZIONE HOLD", "Solo modalità {enumval:LabelDisplayMode.Hold}: per quanti secondi le etichette restano visibili dopo il rilascio del tasto (copre anche una pressione rapida, poiché questo timer parte alla pressione, non al rilascio)."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("ANZEIGEDAUER BEI HALTEN", "Nur im {enumval:LabelDisplayMode.Hold}-Modus: wie viele Sekunden Labels nach dem Loslassen der Taste sichtbar bleiben (deckt auch einen kurzen Tastendruck ab, da dieser Timer beim Drücken gestartet wird, nicht beim Loslassen)."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("DURACIÓN VISIBLE EN MODO MANTENER", "Solo en modo {enumval:LabelDisplayMode.Hold}: cuántos segundos permanecen visibles las etiquetas tras soltar la tecla (también cubre una pulsación rápida, ya que este temporizador se activa al pulsar, no al soltar)."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("DURACIÓN VISIBLE EN MODO MANTENER", "Solo en modo {enumval:LabelDisplayMode.Hold}: cuántos segundos permanecen visibles las etiquetas después de soltar la tecla (también cubre una pulsación rápida, ya que este temporizador se activa al presionar, no al soltar)."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("DURAÇÃO VISÍVEL AO SEGURAR", "Apenas no modo {enumval:LabelDisplayMode.Hold}: por quantos segundos os rótulos permanecem visíveis após soltar a tecla (também cobre um toque rápido, já que esse cronômetro é iniciado ao pressionar, não ao soltar)."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ДЛИТЕЛЬНОСТЬ ПОКАЗА ПРИ УДЕРЖАНИИ", "Только в режиме {enumval:LabelDisplayMode.Hold}: сколько секунд метки остаются видимыми после отпускания клавиши (также покрывает быстрое нажатие, поскольку этот таймер запускается при нажатии, а не при отпускании)."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ТРИВАЛІСТЬ ПОКАЗУ ПРИ УТРИМАННІ", "Лише в режимі {enumval:LabelDisplayMode.Hold}: скільки секунд мітки залишаються видимими після відпускання клавіші (також охоплює швидке натискання, оскільки цей таймер запускається при натисканні, а не при відпусканні)."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("按住后显示时长", "仅限 {enumval:LabelDisplayMode.Hold} 模式：松开按键后标签保持可见的秒数（也涵盖快速轻触的情况，因为计时器在按下时启动，而非松开时）。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("按住後顯示時長", "僅限 {enumval:LabelDisplayMode.Hold} 模式：放開按鍵後標籤保持可見的秒數（也涵蓋快速輕觸的情況，因為計時器在按下時啟動，而非放開時）。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("長押し後の表示時間", "{enumval:LabelDisplayMode.Hold} モード限定: キーを離した後にラベルが表示され続ける秒数です(このタイマーは離したときではなく押したときに設定されるため、素早いタップにも対応します)。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("누르고 있기 표시 지속 시간", "{enumval:LabelDisplayMode.Hold} 모드 전용: 키를 뗀 후 라벨이 표시된 상태로 유지되는 초입니다(이 타이머는 뗄 때가 아니라 누를 때 설정되므로 짧게 탭한 경우도 포함됩니다)."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("CZAS WYŚWIETLANIA PRZY PRZYTRZYMANIU", "Tylko tryb {enumval:LabelDisplayMode.Hold}: ile sekund etykiety pozostają widoczne po zwolnieniu klawisza (obejmuje też szybkie stuknięcie, ponieważ ten timer jest ustawiany przy naciśnięciu, a nie przy zwolnieniu)."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("BASILI TUTMADA GÖSTERİM SÜRESİ", "Yalnızca {enumval:LabelDisplayMode.Hold} modunda: tuş bırakıldıktan sonra etiketlerin kaç saniye görünür kalacağı (bu zamanlayıcı bırakmada değil basmada başladığından hızlı bir dokunuşu da kapsar)."),
            });

            Add("max-distance-meters", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("MAX DISTANCE METERS", "A player's label stops showing beyond this distance. The default is high enough to cover essentially any sightline in a run; lower it if you'd rather only track teammates who are actually nearby."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("DISTANCE MAXIMALE EN MÈTRES", "Le label d'un joueur cesse de s'afficher au-delà de cette distance. La valeur par défaut est assez élevée pour couvrir pratiquement toute ligne de vue en partie ; réduisez-la si vous préférez ne suivre que les coéquipiers réellement proches."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("DISTANZA MASSIMA IN METRI", "L'etichetta di un giocatore smette di essere visibile oltre questa distanza. Il valore predefinito è abbastanza alto da coprire praticamente qualsiasi linea di visuale in una partita; abbassalo se preferisci tracciare solo i compagni effettivamente vicini."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("MAXIMALE ENTFERNUNG IN METERN", "Das Label eines Spielers wird über diese Entfernung hinaus nicht mehr angezeigt. Der Standardwert ist hoch genug, um praktisch jede Sichtlinie in einem Run abzudecken; senke ihn, wenn du lieber nur tatsächlich nahe Teammitglieder verfolgen möchtest."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("DISTANCIA MÁXIMA EN METROS", "La etiqueta de un jugador deja de mostrarse más allá de esta distancia. El valor predeterminado es lo bastante alto como para cubrir prácticamente cualquier línea de visión en una partida; redúcelo si prefieres seguir solo a los compañeros que están realmente cerca."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("DISTANCIA MÁXIMA EN METROS", "La etiqueta de un jugador deja de mostrarse más allá de esta distancia. El valor predeterminado es lo bastante alto como para cubrir prácticamente cualquier línea de visión en una partida; redúcelo si prefieres seguir solo a los compañeros que están realmente cerca."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("DISTÂNCIA MÁXIMA EM METROS", "O rótulo de um jogador para de aparecer além dessa distância. O padrão é alto o suficiente para cobrir praticamente qualquer linha de visão em uma partida; diminua-o se preferir rastrear apenas colegas que estejam realmente por perto."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("МАКСИМАЛЬНОЕ РАССТОЯНИЕ В МЕТРАХ", "Метка игрока перестаёт отображаться за пределами этого расстояния. Значение по умолчанию достаточно велико, чтобы охватить практически любую видимость в забеге; уменьшите его, если хотите отслеживать только действительно близких союзников."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("МАКСИМАЛЬНА ВІДСТАНЬ У МЕТРАХ", "Мітка гравця перестає відображатися за межами цієї відстані. Значення за замовчуванням достатньо велике, щоб охопити практично будь-яку видимість у забігу; зменшіть його, якщо хочете відстежувати лише справді близьких союзників."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("最大距离（米）", "超过此距离后，玩家标签将不再显示。默认值已足够高，可覆盖一局中几乎所有视线范围；如果你只想追踪真正在附近的队友，可以调低它。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("最大距離（公尺）", "超過此距離後，玩家標籤將不再顯示。預設值已足夠高，可涵蓋一局中幾乎所有視線範圍；如果你只想追蹤真正在附近的隊友，可以調低它。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("最大距離（メートル）", "この距離を超えるとプレイヤーのラベルは表示されなくなります。デフォルト値はラン中のほぼすべての見通し線をカバーできるほど高く設定されています。実際に近くにいる仲間だけを追跡したい場合は下げてください。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("최대 거리(미터)", "이 거리를 넘어서면 플레이어 라벨이 표시되지 않습니다. 기본값은 런 중 거의 모든 시야를 포괄할 만큼 충분히 높습니다. 실제로 가까이 있는 팀원만 추적하고 싶다면 낮추세요."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("MAKSYMALNA ODLEGŁOŚĆ W METRACH", "Etykieta gracza przestaje się wyświetlać poza tą odległością. Wartość domyślna jest wystarczająco wysoka, by objąć praktycznie każdą linię widzenia w przebiegu; zmniejsz ją, jeśli wolisz śledzić tylko naprawdę pobliskich sojuszników."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("METRE CİNSİNDEN MAKSİMUM MESAFE", "Bir oyuncunun etiketi bu mesafenin ötesinde gösterilmeyi bırakır. Varsayılan değer bir koşuda hemen hemen her görüş hattını kapsayacak kadar yüksektir; yalnızca gerçekten yakındaki takım arkadaşlarını takip etmek isterseniz düşürün."),
            });

            Add("name-font-size", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("NAME FONT SIZE", "Base font size of each player's name label, before the Fonts section's on-screen/off-screen name scale is applied on top."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("TAILLE DE POLICE DU NOM", "Taille de police de base du label de nom de chaque joueur, avant l'application de l'échelle on-screen/off-screen de la section Fonts."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("DIMENSIONE CARATTERE NOME", "Dimensione di base del carattere dell'etichetta del nome di ogni giocatore, prima che venga applicata la scala on-screen/off-screen della sezione Fonts."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("SCHRIFTGRÖSSE NAME", "Grundschriftgröße des Namens-Labels jedes Spielers, bevor die on-screen/off-screen-Namensgröße aus dem Fonts-Abschnitt zusätzlich angewendet wird."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("TAMAÑO DE FUENTE DEL NOMBRE", "Tamaño de fuente base de la etiqueta de nombre de cada jugador, antes de aplicar la escala on-screen/off-screen de la sección Fonts."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("TAMAÑO DE FUENTE DEL NOMBRE", "Tamaño de fuente base de la etiqueta de nombre de cada jugador, antes de aplicar la escala on-screen/off-screen de la sección Fonts."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("TAMANHO DA FONTE DO NOME", "Tamanho base da fonte do rótulo de nome de cada jogador, antes de aplicar a escala on-screen/off-screen da seção Fonts."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("РАЗМЕР ШРИФТА ИМЕНИ", "Базовый размер шрифта метки имени каждого игрока, до применения масштаба on-screen/off-screen из раздела Fonts."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("РОЗМІР ШРИФТУ ІМЕНІ", "Базовий розмір шрифту мітки імені кожного гравця, до застосування масштабу on-screen/off-screen з розділу Fonts."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("名称字体大小", "每位玩家名称标签的基础字体大小，在应用 Fonts 分区的屏幕内/屏幕外名称缩放之前。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("名稱字型大小", "每位玩家名稱標籤的基礎字型大小，在套用 Fonts 分區的螢幕內/螢幕外名稱縮放之前。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("名前のフォントサイズ", "各プレイヤーの名前ラベルの基本フォントサイズです。Fonts セクションの画面内/画面外の名前スケールが適用される前の値です。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("이름 글꼴 크기", "Fonts 섹션의 화면 내/화면 밖 이름 크기가 적용되기 전, 각 플레이어 이름 라벨의 기본 글꼴 크기입니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("ROZMIAR CZCIONKI NAZWY", "Podstawowy rozmiar czcionki etykiety nazwy każdego gracza, przed zastosowaniem skali on-screen/off-screen z sekcji Fonts."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("İSİM YAZI TİPİ BOYUTU", "Fonts bölümündeki ekran içi/ekran dışı isim ölçeği üzerine uygulanmadan önceki her oyuncunun isim etiketinin temel yazı tipi boyutu."),
            });

            Add("distance-font-size", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("DISTANCE FONT SIZE", "Base font size of the distance sub-line under each name label, before the Fonts section's distance scale is applied on top."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("TAILLE DE POLICE DE LA DISTANCE", "Taille de police de base de la ligne de distance sous chaque label de nom, avant l'application de l'échelle de distance de la section Fonts."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("DIMENSIONE CARATTERE DISTANZA", "Dimensione di base del carattere della riga della distanza sotto ogni etichetta del nome, prima che venga applicata la scala della distanza della sezione Fonts."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("SCHRIFTGRÖSSE ENTFERNUNG", "Grundschriftgröße der Entfernungszeile unter jedem Namens-Label, bevor die Entfernungsgröße aus dem Fonts-Abschnitt zusätzlich angewendet wird."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("TAMAÑO DE FUENTE DE LA DISTANCIA", "Tamaño de fuente base de la línea de distancia bajo cada etiqueta de nombre, antes de aplicar la escala de distancia de la sección Fonts."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("TAMAÑO DE FUENTE DE LA DISTANCIA", "Tamaño de fuente base de la línea de distancia bajo cada etiqueta de nombre, antes de aplicar la escala de distancia de la sección Fonts."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("TAMANHO DA FONTE DA DISTÂNCIA", "Tamanho base da fonte da linha de distância abaixo de cada rótulo de nome, antes de aplicar a escala de distância da seção Fonts."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("РАЗМЕР ШРИФТА РАССТОЯНИЯ", "Базовый размер шрифта строки расстояния под каждой меткой имени, до применения масштаба расстояния из раздела Fonts."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("РОЗМІР ШРИФТУ ВІДСТАНІ", "Базовий розмір шрифту рядка відстані під кожною міткою імені, до застосування масштабу відстані з розділу Fonts."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("距离字体大小", "每个名称标签下方距离子行的基础字体大小，在应用 Fonts 分区的距离缩放之前。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("距離字型大小", "每個名稱標籤下方距離子行的基礎字型大小，在套用 Fonts 分區的距離縮放之前。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("距離のフォントサイズ", "各名前ラベルの下にある距離のサブラインの基本フォントサイズです。Fonts セクションの距離スケールが適用される前の値です。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("거리 글꼴 크기", "Fonts 섹션의 거리 크기가 적용되기 전, 각 이름 라벨 아래 거리 보조 줄의 기본 글꼴 크기입니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("ROZMIAR CZCIONKI ODLEGŁOŚCI", "Podstawowy rozmiar czcionki linii odległości pod każdą etykietą nazwy, przed zastosowaniem skali odległości z sekcji Fonts."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("MESAFE YAZI TİPİ BOYUTU", "Fonts bölümündeki mesafe ölçeği üzerine uygulanmadan önceki her isim etiketinin altındaki mesafe alt satırının temel yazı tipi boyutu."),
            });

            Add("show-distance", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("SHOW DISTANCE", "Show the distance sub-line under each name label."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("AFFICHER LA DISTANCE", "Affiche la ligne de distance sous chaque label de nom."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("MOSTRA DISTANZA", "Mostra la riga della distanza sotto ogni etichetta del nome."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("ENTFERNUNG ANZEIGEN", "Zeigt die Entfernungszeile unter jedem Namens-Label an."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("MOSTRAR DISTANCIA", "Muestra la línea de distancia bajo cada etiqueta de nombre."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("MOSTRAR DISTANCIA", "Muestra la línea de distancia bajo cada etiqueta de nombre."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("MOSTRAR DISTÂNCIA", "Mostra a linha de distância abaixo de cada rótulo de nome."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ПОКАЗЫВАТЬ РАССТОЯНИЕ", "Показывает строку расстояния под каждой меткой имени."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ПОКАЗУВАТИ ВІДСТАНЬ", "Показує рядок відстані під кожною міткою імені."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("显示距离", "在每个名称标签下方显示距离子行。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("顯示距離", "在每個名稱標籤下方顯示距離子行。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("距離を表示", "各名前ラベルの下に距離のサブラインを表示します。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("거리 표시", "각 이름 라벨 아래에 거리 보조 줄을 표시합니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("POKAŻ ODLEGŁOŚĆ", "Pokazuje linię odległości pod każdą etykietą nazwy."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("MESAFEYİ GÖSTER", "Her isim etiketinin altında mesafe alt satırını gösterir."),
            });

            Add("show-status-badges", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("SHOW STATUS BADGES", "Show the host crown / unconscious / dead badges on each label."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("AFFICHER LES BADGES DE STATUT", "Affiche les badges couronne d'hôte / inconscient / mort sur chaque label."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("MOSTRA BADGE DI STATO", "Mostra i badge corona host / svenuto / morto su ogni etichetta."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("STATUSABZEICHEN ANZEIGEN", "Zeigt die Host-Krone / Bewusstlos- / Tot-Abzeichen auf jedem Label an."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("MOSTRAR INSIGNIAS DE ESTADO", "Muestra las insignias de corona de anfitrión / inconsciente / muerto en cada etiqueta."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("MOSTRAR INSIGNIAS DE ESTADO", "Muestra las insignias de corona de anfitrión / inconsciente / muerto en cada etiqueta."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("MOSTRAR EMBLEMAS DE STATUS", "Mostra os emblemas de coroa de anfitrião / inconsciente / morto em cada rótulo."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ПОКАЗЫВАТЬ ЗНАЧКИ СТАТУСА", "Показывает значки короны хоста / без сознания / мёртв на каждой метке."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ПОКАЗУВАТИ ЗНАЧКИ СТАТУСУ", "Показує значки корони хоста / непритомний / мертвий на кожній мітці."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("显示状态徽章", "在每个标签上显示房主皇冠／昏迷／死亡徽章。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("顯示狀態徽章", "在每個標籤上顯示房主皇冠／昏迷／死亡徽章。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("ステータスバッジを表示", "各ラベルにホストの王冠/気絶/死亡のバッジを表示します。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("상태 배지 표시", "각 라벨에 호스트 왕관/기절/사망 배지를 표시합니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("POKAŻ ODZNAKI STATUSU", "Pokazuje odznaki korony hosta / nieprzytomności / śmierci na każdej etykiecie."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("DURUM RÜTBELERİNİ GÖSTER", "Her etikette ev sahibi tacı / baygın / ölü rütbelerini gösterir."),
            });

            Add("use-character-color", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("USE CHARACTER COLOR", "Color each label's name with that player's own character color instead of the vanilla name-label color."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("UTILISER LA COULEUR DU PERSONNAGE", "Colore le nom de chaque label avec la couleur de personnage propre à ce joueur, au lieu de la couleur d'origine du jeu."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("USA COLORE PERSONAGGIO", "Colora il nome di ogni etichetta con il colore del personaggio di quel giocatore, invece del colore originale del gioco."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("CHARAKTERFARBE VERWENDEN", "Färbt den Namen jedes Labels in der eigenen Charakterfarbe dieses Spielers, statt in der originalen Namens-Label-Farbe."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("USAR COLOR DE PERSONAJE", "Colorea el nombre de cada etiqueta con el color de personaje propio de ese jugador, en lugar del color de etiqueta original del juego."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("USAR COLOR DE PERSONAJE", "Colorea el nombre de cada etiqueta con el color de personaje propio de ese jugador, en lugar del color de etiqueta original del juego."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("USAR COR DO PERSONAGEM", "Colore o nome de cada rótulo com a cor do personagem daquele jogador, em vez da cor original do rótulo do jogo."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ИСПОЛЬЗОВАТЬ ЦВЕТ ПЕРСОНАЖА", "Окрашивает имя на каждой метке в собственный цвет персонажа этого игрока вместо стандартного цвета метки имени."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ВИКОРИСТОВУВАТИ КОЛІР ПЕРСОНАЖА", "Забарвлює ім'я на кожній мітці у власний колір персонажа цього гравця замість стандартного кольору мітки імені."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("使用角色颜色", "用该玩家自己的角色颜色为其标签名称上色，而不是原版名称标签的颜色。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("使用角色顏色", "用該玩家自己的角色顏色為其標籤名稱上色，而不是原版名稱標籤的顏色。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("キャラクターカラーを使用", "バニラの名前ラベルの色の代わりに、そのプレイヤー自身のキャラクターカラーで各ラベルの名前を着色します。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("캐릭터 색상 사용", "바닐라 이름 라벨 색상 대신 해당 플레이어 고유의 캐릭터 색상으로 각 라벨의 이름을 표시합니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("UŻYJ KOLORU POSTACI", "Koloruje nazwę na każdej etykiecie własnym kolorem postaci danego gracza zamiast domyślnego koloru etykiety nazwy."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("KARAKTER RENGİNİ KULLAN", "Her etiketin adını, orijinal isim etiketi rengi yerine o oyuncunun kendi karakter rengiyle boyar."),
            });

            Add("replace-vanilla-labels", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("REPLACE VANILLA LABELS", "Hide the game's own close-range player name labels entirely, so Sense of Direction's labels are the only ones ever shown. Off by default; normally the two systems hand off to each other instead."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("REMPLACER LES LABELS D'ORIGINE", "Masque entièrement les labels de nom de joueur à courte portée du jeu, afin que seuls les labels de Sense of Direction soient affichés. Désactivé par défaut ; normalement, les deux systèmes se relaient plutôt l'un l'autre."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("SOSTITUISCI ETICHETTE ORIGINALI", "Nasconde completamente le etichette con il nome del giocatore a corto raggio del gioco, così solo quelle di Sense of Direction vengono mostrate. Disattivato per impostazione predefinita; normalmente i due sistemi si passano il testimone a vicenda."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("VANILLA-LABELS ERSETZEN", "Blendet die originalen Namens-Labels des Spiels für Spieler in der Nähe vollständig aus, sodass ausschließlich die Labels von Sense of Direction angezeigt werden. Standardmäßig deaktiviert; normalerweise übergeben sich die beiden Systeme gegenseitig."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("REEMPLAZAR ETIQUETAS ORIGINALES", "Oculta por completo las etiquetas de nombre de jugador a corta distancia del propio juego, de modo que solo se muestren las etiquetas de Sense of Direction. Desactivado de forma predeterminada; normalmente ambos sistemas se turnan entre sí."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("REEMPLAZAR ETIQUETAS ORIGINALES", "Oculta por completo las etiquetas de nombre de jugador a corta distancia del propio juego, de modo que solo se muestren las etiquetas de Sense of Direction. Desactivado de forma predeterminada; normalmente ambos sistemas se turnan entre sí."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("SUBSTITUIR RÓTULOS ORIGINAIS", "Oculta completamente os rótulos de nome de jogador de curto alcance do próprio jogo, para que apenas os rótulos do Sense of Direction sejam exibidos. Desativado por padrão; normalmente os dois sistemas se revezam entre si."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ЗАМЕНИТЬ ОРИГИНАЛЬНЫЕ МЕТКИ", "Полностью скрывает собственные ближние метки имён игроков игры, так что отображаются только метки Sense of Direction. По умолчанию выключено; обычно эти две системы просто передают друг другу управление."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ЗАМІНИТИ ОРИГІНАЛЬНІ МІТКИ", "Повністю приховує власні ближні мітки імен гравців гри, тож відображаються лише мітки Sense of Direction. За замовчуванням вимкнено; зазвичай ці дві системи просто передають керування одна одній."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("替换原版标签", "完全隐藏游戏自身的近距离玩家姓名标签，使 Sense of Direction 的标签成为唯一显示的标签。默认关闭；通常这两套系统会彼此自然交替显示。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("取代原版標籤", "完全隱藏遊戲自身的近距離玩家姓名標籤，使 Sense of Direction 的標籤成為唯一顯示的標籤。預設關閉；通常這兩套系統會彼此自然交替顯示。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("バニラのラベルを置き換え", "ゲーム本来の近距離プレイヤー名前ラベルを完全に非表示にし、Sense of Direction のラベルのみが表示されるようにします。デフォルトではオフです。通常はこの2つのシステムが互いに引き継ぎ合います。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("바닐라 라벨 대체", "게임 자체의 근거리 플레이어 이름 라벨을 완전히 숨겨 Sense of Direction의 라벨만 표시되도록 합니다. 기본값은 꺼짐이며, 일반적으로 두 시스템은 서로 자연스럽게 넘겨받습니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("ZASTĄP ORYGINALNE ETYKIETY", "Całkowicie ukrywa własne etykiety nazw graczy z bliskiego zasięgu gry, tak że wyświetlane są wyłącznie etykiety Sense of Direction. Domyślnie wyłączone; zwykle oba systemy po prostu przekazują sobie kontrolę."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("VARSAYILAN ETİKETLERİ DEĞİŞTİR", "Oyunun kendi yakın mesafe oyuncu isim etiketlerini tamamen gizler, böylece yalnızca Sense of Direction'ın etiketleri gösterilir. Varsayılan olarak kapalıdır; normalde iki sistem birbirine devreder."),
            });
        }
    }
}
