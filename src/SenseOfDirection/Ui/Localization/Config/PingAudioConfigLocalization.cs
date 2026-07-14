using System.Collections.Generic;

namespace SenseOfDirection.Ui.Localization.Config
{
    /// <summary>Localized names/descriptions for every "Ping-Audio" section config entry.</summary>
    internal static class PingAudioConfigLocalization
    {
        internal static void Register(ConfigLocalizationTable.Registry registry)
        {
            void Add(string key, Dictionary<LocalizedText.Language, ConfigLocalizationEntry> table) =>
                registry.Add("Ping-Audio", key, table);

            Add("enable-audio-boost", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ENABLE AUDIO BOOST", "Drastically reduce the ping sound's distance falloff so it's audible from much further away, while sounding unchanged up close. The rest of this section does nothing while this is off."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ACTIVER L'AMPLIFICATION AUDIO", "Réduit fortement l'atténuation du son de ping avec la distance, pour qu'il soit audible de bien plus loin, sans rien changer de près. Le reste de cette section n'a aucun effet tant que ceci est désactivé."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("ATTIVA POTENZIAMENTO AUDIO", "Riduce drasticamente l'attenuazione con la distanza del suono del ping, rendendolo udibile da molto più lontano, senza cambiare nulla da vicino. Il resto di questa sezione non ha effetto finché questa è disattivata."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("AUDIO-VERSTÄRKUNG AKTIVIEREN", "Reduziert die Entfernungsabnahme des Ping-Sounds drastisch, sodass er aus viel größerer Entfernung hörbar ist, während er aus der Nähe unverändert klingt. Der Rest dieses Abschnitts bewirkt nichts, solange dies deaktiviert ist."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ACTIVAR REFUERZO DE AUDIO", "Reduce drásticamente la atenuación por distancia del sonido del ping, para que se oiga desde mucho más lejos, sin cambiar nada de cerca. El resto de esta sección no hace nada mientras esto esté desactivado."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ACTIVAR REFUERZO DE AUDIO", "Reduce drásticamente la atenuación por distancia del sonido del ping, para que se escuche desde mucho más lejos, sin cambiar nada de cerca. El resto de esta sección no hace nada mientras esto esté desactivado."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ATIVAR REFORÇO DE ÁUDIO", "Reduz drasticamente a atenuação por distância do som do ping, para que seja audível de muito mais longe, sem mudar nada de perto. O resto desta seção não faz nada enquanto isso estiver desativado."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ВКЛЮЧИТЬ УСИЛЕНИЕ ЗВУКА", "Резко снижает затухание звука пинга с расстоянием, чтобы он был слышен намного дальше, при этом вблизи звучание не меняется. Остальные настройки этого раздела не действуют, пока это выключено."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("УВІМКНУТИ ПІДСИЛЕННЯ ЗВУКУ", "Різко зменшує затухання звуку пінгу з відстанню, щоб він був чутний набагато далі, при цьому зблизька звучання не змінюється. Решта налаштувань цього розділу не діють, поки це вимкнено."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("启用音效增强", "大幅降低呼喊音效随距离衰减的程度，使其在更远处也能听到，而近处音效保持不变。此项关闭时，本区其余设置均无效。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("啟用音效增強", "大幅降低呼喊音效隨距離衰減的程度，使其在更遠處也能聽到，而近處音效保持不變。此項關閉時，本區其餘設定均無效。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("音声ブーストを有効化", "ピン音の距離による減衰を大幅に抑え、より遠くからでも聞こえるようにします。近くでの聞こえ方は変わりません。これがオフの間、このセクションの他の設定は何も機能しません。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("오디오 부스트 활성화", "핑 소리의 거리 감쇠를 대폭 줄여 훨씬 먼 곳에서도 들리게 합니다. 가까이에서는 소리가 그대로입니다. 이 항목이 꺼져 있으면 이 섹션의 나머지 설정은 아무 효과가 없습니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("WŁĄCZ WZMOCNIENIE DŹWIĘKU", "Drastycznie zmniejsza zanikanie dźwięku pingu wraz z odległością, dzięki czemu jest słyszalny z dużo większej odległości, bez zmian z bliska. Reszta tej sekcji nie działa, dopóki to jest wyłączone."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("SES GÜÇLENDİRMEYİ ETKİNLEŞTİR", "Ping sesinin mesafeyle azalmasını büyük ölçüde azaltarak çok daha uzaktan duyulmasını sağlar, yakınken ses değişmez. Bu kapalıyken bu bölümdeki diğer ayarlar hiçbir işe yaramaz."),
            });

            Add("range-meters", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("RANGE METERS", "Ping sound's max audible range (vanilla is 150)."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("PORTÉE EN MÈTRES", "Portée audible maximale du son de ping (150 dans le jeu de base)."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("RAGGIO IN METRI", "Raggio udibile massimo del suono del ping (150 nel gioco base)."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("REICHWEITE IN METERN", "Maximale Hörreichweite des Ping-Sounds (Vanilla: 150)."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ALCANCE EN METROS", "Alcance audible máximo del sonido de ping (150 en el juego base)."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ALCANCE EN METROS", "Alcance audible máximo del sonido de ping (150 en el juego base)."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ALCANCE EM METROS", "Alcance audível máximo do som do ping (150 no jogo base)."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ДАЛЬНОСТЬ В МЕТРАХ", "Максимальная дальность слышимости звука пинга (150 в ванильной игре)."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("ДАЛЬНІСТЬ У МЕТРАХ", "Максимальна дальність чутності звуку пінгу (150 у ванільній грі)."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("音效距离（米）", "呼喊音效的最大可听距离（原版为150）。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("音效距離（公尺）", "呼喊音效的最大可聽距離（原版為150）。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("音の到達距離（メートル）", "ピン音の最大可聴距離です（バニラは150）。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("음향 범위(미터)", "핑 소리가 들리는 최대 거리입니다(바닐라는 150)."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("ZASIĘG W METRACH", "Maksymalny słyszalny zasięg dźwięku pingu (w wersji podstawowej to 150)."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("METRE CİNSİNDEN MENZİL", "Ping sesinin duyulabileceği maksimum mesafe (vanilla'da 150)."),
            });

            Add("min-distance-meters", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("MIN DISTANCE METERS", "Distance under which the ping sound plays at full volume before it starts falling off toward {key:Ping-Audio/range-meters}."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("DISTANCE MINIMALE EN MÈTRES", "Distance en dessous de laquelle le son du ping est à pleine puissance avant de commencer à s'atténuer vers {key:Ping-Audio/range-meters}."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("DISTANZA MINIMA IN METRI", "Distanza sotto la quale il suono del ping è a piena potenza prima di iniziare ad attenuarsi verso {key:Ping-Audio/range-meters}."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("MINDESTABSTAND IN METERN", "Entfernung, unterhalb derer der Ping-Sound in voller Lautstärke abgespielt wird, bevor er in Richtung {key:Ping-Audio/range-meters} abzunehmen beginnt."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("DISTANCIA MÍNIMA EN METROS", "Distancia por debajo de la cual el sonido de ping suena a volumen completo antes de empezar a atenuarse hacia {key:Ping-Audio/range-meters}."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("DISTANCIA MÍNIMA EN METROS", "Distancia por debajo de la cual el sonido de ping suena a volumen completo antes de empezar a atenuarse hacia {key:Ping-Audio/range-meters}."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("DISTÂNCIA MÍNIMA EM METROS", "Distância abaixo da qual o som do ping toca em volume total antes de começar a atenuar rumo a {key:Ping-Audio/range-meters}."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("МИНИМАЛЬНОЕ РАССТОЯНИЕ В МЕТРАХ", "Расстояние, в пределах которого звук пинга воспроизводится на полной громкости, прежде чем начинает затухать в сторону {key:Ping-Audio/range-meters}."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("МІНІМАЛЬНА ВІДСТАНЬ У МЕТРАХ", "Відстань, у межах якої звук пінгу відтворюється на повній гучності, перш ніж почне затухати у бік {key:Ping-Audio/range-meters}."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("最小距离（米）", "在此距离以内，呼喊音效以最大音量播放，超出后才开始朝 {key:Ping-Audio/range-meters} 衰减。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("最小距離（公尺）", "在此距離以內，呼喊音效以最大音量播放，超出後才開始朝 {key:Ping-Audio/range-meters} 衰減。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("最小距離（メートル）", "この距離未満ではピン音が最大音量で再生され、それを超えると{key:Ping-Audio/range-meters}に向かって減衰し始めます。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("최소 거리(미터)", "이 거리 이내에서는 핑 소리가 최대 음량으로 재생되며, 이후부터 {key:Ping-Audio/range-meters}를 향해 감쇠하기 시작합니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("MINIMALNA ODLEGŁOŚĆ W METRACH", "Odległość, poniżej której dźwięk pingu jest odtwarzany z pełną głośnością, zanim zacznie zanikać w kierunku {key:Ping-Audio/range-meters}."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("METRE CİNSİNDEN MİNİMUM MESAFE", "Ping sesinin {key:Ping-Audio/range-meters}'a doğru azalmaya başlamadan önce tam ses düzeyinde çaldığı mesafe."),
            });

            Add("volume-multiplier", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("VOLUME MULTIPLIER", "Multiplier on the ping sound's own base (close-range) volume. The far-range audibility boost also makes it slightly too loud up close, so this trims that back down."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("MULTIPLICATEUR DE VOLUME", "Multiplicateur du volume de base (courte portée) du son de ping. L'amplification pour la longue portée le rend aussi un peu trop fort de près, donc ceci sert à corriger cela."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("MOLTIPLICATORE DI VOLUME", "Moltiplicatore del volume base (a corto raggio) del suono del ping. Il potenziamento per il lungo raggio lo rende anche leggermente troppo forte da vicino, quindi questo lo riduce di nuovo."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("LAUTSTÄRKEMULTIPLIKATOR", "Multiplikator für die Grundlautstärke (Nahbereich) des Ping-Sounds. Die Verstärkung für die Fernwirkung macht ihn aus der Nähe auch etwas zu laut, weshalb dies das wieder ausgleicht."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("MULTIPLICADOR DE VOLUMEN", "Multiplicador del volumen base (corto alcance) del sonido de ping. El refuerzo de audibilidad a larga distancia también lo hace algo demasiado fuerte de cerca, así que esto lo compensa."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("MULTIPLICADOR DE VOLUMEN", "Multiplicador del volumen base (corto alcance) del sonido de ping. El refuerzo de audibilidad a larga distancia también lo hace algo demasiado fuerte de cerca, así que esto lo compensa."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("MULTIPLICADOR DE VOLUME", "Multiplicador do volume base (curto alcance) do som do ping. O reforço de audibilidade de longo alcance também deixa o som um pouco alto demais de perto, então isso ajusta esse excesso."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("МНОЖИТЕЛЬ ГРОМКОСТИ", "Множитель базовой (ближней) громкости звука пинга. Усиление слышимости на дальних расстояниях также делает его чуть слишком громким вблизи, поэтому это позволяет это компенсировать."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("МНОЖНИК ГУЧНОСТІ", "Множник базової (ближньої) гучності звуку пінгу. Підсилення чутності на дальніх відстанях також робить його трохи занадто гучним зблизька, тому це дозволяє це компенсувати."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("音量倍率", "呼喊音效基础（近距离）音量的倍率。远距离音效增强也会让近距离声音略显过响，此项用于把它调回来。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("音量倍率", "呼喊音效基礎（近距離）音量的倍率。遠距離音效增強也會讓近距離聲音略顯過響，此項用於把它調回來。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("音量倍率", "ピン音の基本（近距離）音量に対する倍率です。遠距離での聞こえやすさを高めると近距離でも少しうるさくなるため、これで調整します。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("음량 배율", "핑 소리의 기본(근거리) 음량에 곱해지는 배율입니다. 원거리 가청성 강화로 근거리에서 소리가 약간 커지는 것을 이 값으로 다시 낮춥니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("MNOŻNIK GŁOŚNOŚCI", "Mnożnik podstawowej (bliskiej) głośności dźwięku pingu. Wzmocnienie słyszalności na dużą odległość sprawia też, że z bliska jest lekko za głośno, więc to to koryguje."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("SES DÜZEYİ ÇARPANI", "Ping sesinin temel (yakın mesafe) ses düzeyi çarpanı. Uzak mesafe duyulabilirlik artışı yakınken de sesi biraz fazla yüksek yapar, bu ayar bunu dengeler."),
            });
        }
    }
}
