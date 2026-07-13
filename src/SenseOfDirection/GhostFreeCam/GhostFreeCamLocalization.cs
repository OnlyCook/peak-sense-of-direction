namespace SenseOfDirection.GhostFreeCam
{
    /// <summary>
    /// Translated "to go into/leave free-cam mode" copy for
    /// <see cref="GhostFreeCamKeyHint"/>, keyed off the game's own
    /// <c>LocalizedText.CURRENT_LANGUAGE</c> (set by the player's in-game
    /// language setting) rather than always showing English - this text has
    /// no vanilla localization key of its own to piggyback on (it's a hint
    /// for this mod's own key, not native UI copy), so translations are
    /// maintained here directly. Community-sourced, not professionally
    /// reviewed - good enough for a short UI hint, easy to correct/extend
    /// per-language later without touching <see cref="GhostFreeCamKeyHint"/>
    /// itself.
    /// </summary>
    internal static class GhostFreeCamLocalization
    {
        private struct Strings
        {
            public readonly string Enter;
            public readonly string Leave;

            public Strings(string enter, string leave)
            {
                Enter = enter;
                Leave = leave;
            }
        }

        private static readonly System.Collections.Generic.Dictionary<LocalizedText.Language, Strings> Table =
            new System.Collections.Generic.Dictionary<LocalizedText.Language, Strings>
            {
                [LocalizedText.Language.English] = new Strings("to go into free-cam mode", "to leave free-cam mode"),
                [LocalizedText.Language.French] = new Strings("pour passer en mode caméra libre", "pour quitter le mode caméra libre"),
                [LocalizedText.Language.Italian] = new Strings("per entrare in modalità telecamera libera", "per uscire dalla modalità telecamera libera"),
                [LocalizedText.Language.German] = new Strings("um in den Freikamera-Modus zu wechseln", "um den Freikamera-Modus zu verlassen"),
                [LocalizedText.Language.SpanishSpain] = new Strings("para entrar en modo cámara libre", "para salir del modo cámara libre"),
                [LocalizedText.Language.SpanishLatam] = new Strings("para entrar en modo cámara libre", "para salir del modo cámara libre"),
                [LocalizedText.Language.BRPortuguese] = new Strings("para entrar no modo câmera livre", "para sair do modo câmera livre"),
                [LocalizedText.Language.Russian] = new Strings("чтобы войти в режим свободной камеры", "чтобы выйти из режима свободной камеры"),
                [LocalizedText.Language.Ukrainian] = new Strings("щоб увійти в режим вільної камери", "щоб вийти з режиму вільної камери"),
                [LocalizedText.Language.SimplifiedChinese] = new Strings("进入自由视角模式", "退出自由视角模式"),
                [LocalizedText.Language.TraditionalChinese] = new Strings("進入自由視角模式", "退出自由視角模式"),
                [LocalizedText.Language.Japanese] = new Strings("フリーカメラモードに入る", "フリーカメラモードを終了する"),
                [LocalizedText.Language.Korean] = new Strings("자유 카메라 모드로 전환", "자유 카메라 모드 종료"),
                [LocalizedText.Language.Polish] = new Strings("aby wejść w tryb wolnej kamery", "aby wyjść z trybu wolnej kamery"),
                [LocalizedText.Language.Turkish] = new Strings("serbest kamera moduna girmek için", "serbest kamera modundan çıkmak için"),
            };

        public static string GetLabel(bool freeCamActive)
        {
            if (!Table.TryGetValue(LocalizedText.CURRENT_LANGUAGE, out Strings strings))
            {
                strings = Table[LocalizedText.Language.English];
            }

            return freeCamActive ? strings.Leave : strings.Enter;
        }
    }
}
