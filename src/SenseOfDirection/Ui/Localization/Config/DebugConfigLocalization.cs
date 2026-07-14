using System.Collections.Generic;

namespace SenseOfDirection.Ui.Localization.Config
{
    /// <summary>Localized names/descriptions for every "Debug" section config entry.</summary>
    internal static class DebugConfigLocalization
    {
        internal static void Register(ConfigLocalizationTable.Registry registry)
        {
            void Add(string key, Dictionary<LocalizedText.Language, ConfigLocalizationEntry> table) =>
                registry.Add("Debug", key, table);

            Add("enable-debug-logging", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ENABLE DEBUG LOGGING", "Log extra diagnostic detail to the BepInEx console/log file."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ACTIVER LA JOURNALISATION DE DÉBOGAGE", "Enregistre des détails de diagnostic supplémentaires dans la console/le fichier journal BepInEx."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("ATTIVA REGISTRAZIONE DI DEBUG", "Registra dettagli diagnostici extra nella console/file di log di BepInEx."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("DEBUG-PROTOKOLLIERUNG AKTIVIEREN", "Protokolliert zusätzliche Diagnosedetails in der BepInEx-Konsole/Protokolldatei."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ACTIVAR REGISTRO DE DEPURACIÓN", "Registra detalles de diagnóstico adicionales en la consola/archivo de registro de BepInEx."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ACTIVAR REGISTRO DE DEPURACIÓN", "Registra detalles de diagnóstico adicionales en la consola/archivo de registro de BepInEx."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ATIVAR REGISTRO DE DEPURAÇÃO", "Registra detalhes diagnósticos extras no console/arquivo de log do BepInEx."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ВКЛЮЧИТЬ ОТЛАДОЧНОЕ ЛОГИРОВАНИЕ", "Записывает дополнительные диагностические сведения в консоль/файл журнала BepInEx."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("УВІМКНУТИ ЗНЕВАДЖУВАЛЬНЕ ЛОГУВАННЯ", "Записує додаткові діагностичні відомості в консоль/файл журналу BepInEx."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("启用调试日志", "将额外的诊断细节记录到 BepInEx 控制台/日志文件中。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("啟用偵錯記錄", "將額外的診斷細節記錄到 BepInEx 主控台/記錄檔案中。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("デバッグログを有効化", "追加の診断詳細をBepInExコンソール/ログファイルに記録します。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("디버그 로깅 활성화", "추가 진단 정보를 BepInEx 콘솔/로그 파일에 기록합니다."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("WŁĄCZ LOGOWANIE DEBUGOWANIA", "Zapisuje dodatkowe szczegóły diagnostyczne do konsoli/pliku dziennika BepInEx."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("HATA AYIKLAMA GÜNLÜĞÜNÜ ETKİNLEŞTİR", "Ek tanılama ayrıntılarını BepInEx konsoluna/günlük dosyasına kaydeder."),
            });

            Add("enable-indicator-test-harness", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ENABLE INDICATOR TEST HARNESS", "Spawn a handful of fixed dummy world points around the camera to visually verify the edge-of-screen indicator framework. Dev/QA tool only; leave off for normal play."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ACTIVER LE BANC DE TEST D'INDICATEURS", "Fait apparaître quelques points fictifs fixes dans le monde autour de la caméra pour vérifier visuellement le système d'indicateurs de bord d'écran. Outil dev/QA uniquement ; à laisser désactivé en jeu normal."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("ATTIVA BANCO DI TEST INDICATORI", "Genera alcuni punti fittizi fissi nel mondo attorno alla telecamera per verificare visivamente il framework degli indicatori a bordo schermo. Solo strumento dev/QA; lasciare disattivato per il gioco normale."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("INDIKATOR-TESTUMGEBUNG AKTIVIEREN", "Erzeugt eine Handvoll fester Dummy-Weltpunkte um die Kamera, um das Bildschirmrand-Indikator-Framework visuell zu überprüfen. Nur Dev/QA-Werkzeug; für normales Spielen deaktiviert lassen."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ACTIVAR BANCO DE PRUEBAS DE INDICADORES", "Genera un puñado de puntos ficticios fijos en el mundo alrededor de la cámara para verificar visualmente el sistema de indicadores de borde de pantalla. Solo herramienta dev/QA; dejar desactivado para el juego normal."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ACTIVAR BANCO DE PRUEBAS DE INDICADORES", "Genera un puñado de puntos ficticios fijos en el mundo alrededor de la cámara para verificar visualmente el sistema de indicadores de borde de pantalla. Solo herramienta dev/QA; dejar desactivado para el juego normal."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ATIVAR BANCADA DE TESTE DE INDICADORES", "Gera alguns pontos fictícios fixos no mundo ao redor da câmera para verificar visualmente o framework de indicadores de borda de tela. Apenas ferramenta dev/QA; deixe desativado para jogo normal."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ВКЛЮЧИТЬ ТЕСТОВЫЙ СТЕНД ИНДИКАТОРОВ", "Создаёт несколько фиктивных фиксированных точек мира вокруг камеры для визуальной проверки системы индикаторов у края экрана. Только инструмент для разработки/QA; оставьте выключенным для обычной игры."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("УВІМКНУТИ ТЕСТОВИЙ СТЕНД ІНДИКАТОРІВ", "Створює декілька фіктивних фіксованих точок світу навколо камери для візуальної перевірки системи індикаторів біля краю екрана. Лише інструмент для розробки/QA; залиште вимкненим для звичайної гри."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("启用指示器测试台", "在摄像机周围生成若干固定的虚拟世界坐标点，用于直观验证屏幕边缘指示器框架。仅供开发/QA 使用；正常游戏中请保持关闭。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("啟用指示器測試台", "在攝影機周圍產生若干固定的虛擬世界座標點，用於直觀驗證螢幕邊緣指示器框架。僅供開發/QA 使用；正常遊戲中請保持關閉。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("インジケーターテストハーネスを有効化", "画面端インジケーターフレームワークを視覚的に検証するため、カメラ周辺に固定のダミーワールドポイントをいくつか生成します。開発/QAツール専用です。通常プレイではオフのままにしてください。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("인디케이터 테스트 하네스 활성화", "화면 가장자리 인디케이터 프레임워크를 시각적으로 검증하기 위해 카메라 주변에 몇 개의 고정된 더미 월드 포인트를 생성합니다. 개발/QA 도구 전용입니다. 일반 플레이에서는 꺼두세요."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("WŁĄCZ TESTOWY ZESTAW WSKAŹNIKÓW", "Tworzy kilka stałych, sztucznych punktów świata wokół kamery, aby wizualnie zweryfikować system wskaźników krawędzi ekranu. Wyłącznie narzędzie dev/QA; pozostaw wyłączone podczas normalnej gry."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("GÖSTERGE TEST DÜZENEĞİNİ ETKİNLEŞTİR", "Ekran kenarı gösterge çerçevesini görsel olarak doğrulamak için kamera etrafında birkaç sabit sahte dünya noktası oluşturur. Yalnızca geliştirici/QA aracıdır; normal oynanışta kapalı bırakın."),
            });

            Add("enable-zombie-debug-esp", new Dictionary<LocalizedText.Language, ConfigLocalizationEntry>
            {
                [LocalizedText.Language.English] = new ConfigLocalizationEntry("ENABLE ZOMBIE DEBUG ESP", "Dev/QA aid: always-visible edge-of-screen label for every naturally-spawned zombie in the level, through walls, to speed up testing zombie-ping detection without hunting a whole level for a rare spawn. Not a real feature; leave off for normal play."),
                [LocalizedText.Language.French] = new ConfigLocalizationEntry("ACTIVER L'ESP DE DÉBOGAGE ZOMBIE", "Aide dev/QA : étiquette de bord d'écran toujours visible pour chaque zombie apparu naturellement dans le niveau, à travers les murs, pour accélérer les tests de détection de ping de zombie sans devoir fouiller tout un niveau pour une apparition rare. Pas une vraie fonctionnalité ; à laisser désactivé en jeu normal."),
                [LocalizedText.Language.Italian] = new ConfigLocalizationEntry("ATTIVA ESP DI DEBUG ZOMBIE", "Aiuto dev/QA: etichetta a bordo schermo sempre visibile per ogni zombie generato naturalmente nel livello, attraverso i muri, per velocizzare il test del rilevamento ping zombie senza dover cercare in tutto il livello una rara apparizione. Non è una vera funzionalità; lasciare disattivato per il gioco normale."),
                [LocalizedText.Language.German] = new ConfigLocalizationEntry("ZOMBIE-DEBUG-ESP AKTIVIEREN", "Dev/QA-Hilfe: immer sichtbares Bildschirmrand-Label für jeden natürlich gespawnten Zombie im Level, durch Wände hindurch, um das Testen der Zombie-Ping-Erkennung zu beschleunigen, ohne ein ganzes Level nach einem seltenen Spawn abzusuchen. Kein echtes Feature; für normales Spielen deaktiviert lassen."),
                [LocalizedText.Language.SpanishSpain] = new ConfigLocalizationEntry("ACTIVAR ESP DE DEPURACIÓN DE ZOMBIS", "Ayuda dev/QA: etiqueta de borde de pantalla siempre visible para cada zombi generado naturalmente en el nivel, a través de las paredes, para acelerar las pruebas de detección de ping de zombis sin tener que rastrear todo un nivel en busca de una aparición rara. No es una función real; dejar desactivado para el juego normal."),
                [LocalizedText.Language.SpanishLatam] = new ConfigLocalizationEntry("ACTIVAR ESP DE DEPURACIÓN DE ZOMBIS", "Ayuda dev/QA: etiqueta de borde de pantalla siempre visible para cada zombi generado naturalmente en el nivel, a través de las paredes, para acelerar las pruebas de detección de ping de zombis sin tener que rastrear todo un nivel en busca de una aparición rara. No es una función real; dejar desactivado para el juego normal."),
                [LocalizedText.Language.BRPortuguese] = new ConfigLocalizationEntry("ATIVAR ESP DE DEPURAÇÃO DE ZUMBI", "Auxílio dev/QA: rótulo de borda de tela sempre visível para cada zumbi gerado naturalmente no nível, através de paredes, para acelerar o teste de detecção de ping de zumbi sem precisar vasculhar um nível inteiro atrás de um spawn raro. Não é um recurso real; deixe desativado para jogo normal."),
                [LocalizedText.Language.Russian] = new ConfigLocalizationEntry("ВКЛЮЧИТЬ ОТЛАДОЧНЫЙ ESP ЗОМБИ", "Инструмент для разработки/QA: всегда видимая подпись у края экрана для каждого естественно заспавнившегося зомби на уровне, сквозь стены, чтобы ускорить тестирование обнаружения пинга зомби без обыскивания всего уровня в поисках редкого спавна. Не настоящая функция; оставьте выключенным для обычной игры."),
                [LocalizedText.Language.Ukrainian] = new ConfigLocalizationEntry("УВІМКНУТИ ЗНЕВАДЖУВАЛЬНИЙ ESP ЗОМБІ", "Інструмент для розробки/QA: завжди видимий підпис біля краю екрана для кожного природно заспавненого зомбі на рівні, крізь стіни, щоб пришвидшити тестування виявлення пінгу зомбі без обшукування цілого рівня в пошуках рідкісного спавну. Не справжня функція; залиште вимкненим для звичайної гри."),
                [LocalizedText.Language.SimplifiedChinese] = new ConfigLocalizationEntry("启用僵尸调试透视", "开发/QA 辅助功能：为关卡中每个自然生成的僵尸显示始终可见的屏幕边缘标签（可透墙），以加快僵尸呼喊检测测试，无需在整个关卡中搜寻稀有生成点。并非正式功能；正常游戏中请保持关闭。"),
                [LocalizedText.Language.TraditionalChinese] = new ConfigLocalizationEntry("啟用殭屍偵錯透視", "開發/QA 輔助功能：為關卡中每個自然生成的殭屍顯示始終可見的螢幕邊緣標籤（可透牆），以加快殭屍呼喊偵測測試，無需在整個關卡中搜尋稀有生成點。並非正式功能；正常遊戲中請保持關閉。"),
                [LocalizedText.Language.Japanese] = new ConfigLocalizationEntry("ゾンビデバッグESPを有効化", "開発/QA支援：レベル内で自然発生するすべてのゾンビに対し、壁越しでも常に表示される画面端ラベルを表示し、レアなスポーンをレベル全体で探し回ることなくゾンビピン検出のテストを高速化します。実際の機能ではありません。通常プレイではオフのままにしてください。"),
                [LocalizedText.Language.Korean] = new ConfigLocalizationEntry("좀비 디버그 ESP 활성화", "개발/QA 보조 기능: 레벨 내 자연 스폰된 모든 좀비에 대해 벽을 통과해서도 항상 보이는 화면 가장자리 라벨을 표시하여, 희귀한 스폰을 찾기 위해 레벨 전체를 뒤지지 않고도 좀비 핑 감지 테스트를 빠르게 할 수 있게 합니다. 실제 기능이 아닙니다. 일반 플레이에서는 꺼두세요."),
                [LocalizedText.Language.Polish] = new ConfigLocalizationEntry("WŁĄCZ ESP DEBUGOWANIA ZOMBIE", "Pomoc dev/QA: zawsze widoczna etykieta krawędzi ekranu dla każdego naturalnie zespawnowanego zombie na poziomie, przez ściany, aby przyspieszyć testowanie wykrywania pingów zombie bez przeszukiwania całego poziomu w poszukiwaniu rzadkiego spawnu. To nie jest prawdziwa funkcja; pozostaw wyłączone podczas normalnej gry."),
                [LocalizedText.Language.Turkish] = new ConfigLocalizationEntry("ZOMBİ HATA AYIKLAMA ESP'SİNİ ETKİNLEŞTİR", "Geliştirici/QA yardımcısı: seviyede doğal olarak beliren her zombi için, nadir bir spawn'u tüm seviyede aramak zorunda kalmadan zombi ping algılamasını test etmeyi hızlandırmak amacıyla, duvarların içinden bile her zaman görünen bir ekran kenarı etiketi gösterir. Gerçek bir özellik değildir; normal oynanışta kapalı bırakın."),
            });
        }
    }
}
