using System.Collections.Generic;
using UnityEngine;

namespace SenseOfDirection.Labels
{
    /// <summary>
    /// Maps a raw <see cref="KeyCode"/> to its index within vanilla's own
    /// <see cref="NativeAssets.KeyboardSprites"/> atlas, for keys that were
    /// never bound to any of the game's own actions - the only lookup
    /// <c>InputSpriteData.GetSpriteTag</c> itself exposes (decompiled
    /// Assembly-CSharp) resolves a bound Input System action's live binding
    /// path, so it can't be called for an arbitrary rebindable key directly.
    ///
    /// This table is a straight port of that same decompiled class's private
    /// <c>inputPathToSpriteTagKeyboard</c>/<c>...Mouse</c> dictionaries
    /// (Input System control-path leaf name -&gt; sprite index), just keyed by
    /// <see cref="KeyCode"/> instead of by control-path string, so it stays
    /// in lockstep with vanilla's own glyphs if that atlas ever changes.
    /// </summary>
    public static class NativeKeySpriteIndex
    {
        public static bool TryGetIndex(KeyCode key, out int index) => SpriteIndexByKeyCode.TryGetValue(key, out index);

        private static readonly Dictionary<KeyCode, int> SpriteIndexByKeyCode = BuildMap();

        private static Dictionary<KeyCode, int> BuildMap()
        {
            var map = new Dictionary<KeyCode, int>();

            for (int i = 0; i <= 9; i++)
            {
                map[KeyCode.Alpha0 + i] = i;
            }
            for (int i = 0; i < 26; i++)
            {
                map[KeyCode.A + i] = 10 + i;
            }
            for (int i = 0; i < 12; i++)
            {
                map[KeyCode.F1 + i] = 36 + i;
            }
            for (int i = 0; i <= 9; i++)
            {
                map[KeyCode.Keypad0 + i] = 127 + i;
            }

            map[KeyCode.Minus] = 78;
            map[KeyCode.Equals] = 80;
            map[KeyCode.LeftBracket] = 82;
            map[KeyCode.RightBracket] = 83;
            map[KeyCode.BackQuote] = 81;
            map[KeyCode.Tab] = 53;
            map[KeyCode.LeftShift] = 51;
            map[KeyCode.RightShift] = 51;
            map[KeyCode.LeftControl] = 49;
            map[KeyCode.RightControl] = 49;
            map[KeyCode.LeftAlt] = 50;
            map[KeyCode.RightAlt] = 50;
            map[KeyCode.Space] = 69;
            map[KeyCode.Semicolon] = 85;
            map[KeyCode.Quote] = 100;
            map[KeyCode.Comma] = 87;
            map[KeyCode.Period] = 88;
            map[KeyCode.Slash] = 76;
            map[KeyCode.Backslash] = 84;
            map[KeyCode.Insert] = 70;
            map[KeyCode.Delete] = 71;
            map[KeyCode.Home] = 72;
            map[KeyCode.End] = 73;
            map[KeyCode.PageUp] = 74;
            map[KeyCode.PageDown] = 75;
            map[KeyCode.UpArrow] = 56;
            map[KeyCode.DownArrow] = 58;
            map[KeyCode.LeftArrow] = 59;
            map[KeyCode.RightArrow] = 57;
            map[KeyCode.KeypadPlus] = 119;
            map[KeyCode.KeypadMinus] = 118;
            map[KeyCode.KeypadDivide] = 120;
            map[KeyCode.KeypadMultiply] = 121;
            map[KeyCode.KeypadEnter] = 122;
            map[KeyCode.KeypadPeriod] = 123;
            map[KeyCode.CapsLock] = 52;
            map[KeyCode.Backspace] = 67;
            map[KeyCode.Return] = 68;
            map[KeyCode.Escape] = 54;

            // Mouse buttons - inputPathToSpriteTagMouse in the same
            // decompiled class, included here since KeyCode.MouseN is a
            // legal (if unlikely) toggle-key binding too.
            map[KeyCode.Mouse0] = 109;
            map[KeyCode.Mouse1] = 110;
            map[KeyCode.Mouse2] = 111;

            return map;
        }
    }
}
