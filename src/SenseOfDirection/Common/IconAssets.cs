using System.IO;
using System.Reflection;
using UnityEngine;

namespace SenseOfDirection.Common
{
    /// <summary>
    /// Loads the mod's bundled icon PNGs (embedded resources - see the
    /// .csproj's "Icons/*.png" EmbeddedResource glob) into cached Sprites.
    ///
    /// The compass face/ping/item-ping icons are drawn with a pure-white fill
    /// and a pure-black outline on transparent, specifically so they can be
    /// recolored per-anchor via Image.color: Unity's sprite tint multiplies
    /// each pixel's RGB by the tint color, so white * tint = tint exactly
    /// (the fill takes on the anchor's color losslessly) while black * tint
    /// stays black regardless of tint (the outline is untouched). The two
    /// player-label status badges are the one exception - they're pre-colored
    /// (fixed tan fill) and are never tinted, since they're meant to read as
    /// a fixed status color rather than the pinging/pinged player's own.
    /// </summary>
    public static class IconAssets
    {
        private static Sprite _playerFace;
        private static Sprite _playerUnconsciousFace;
        private static Sprite _playerDeadFace;
        private static Sprite _pingRing;
        private static Sprite _itemPingDiamond;
        private static Sprite _deadBadge;
        private static Sprite _unconsciousBadge;

        public static Sprite PlayerFace => _playerFace ??= Load("player-icon-reference-white");
        public static Sprite PlayerUnconsciousFace => _playerUnconsciousFace ??= Load("player-unconscious-icon-reference-white");
        public static Sprite PlayerDeadFace => _playerDeadFace ??= Load("player-dead-icon-reference-white");
        public static Sprite PingRing => _pingRing ??= Load("ping-icon-reference-white");
        public static Sprite ItemPingDiamond => _itemPingDiamond ??= Load("item-ping-icon-reference-white");
        public static Sprite DeadBadge => _deadBadge ??= Load("player-dead-icon-reference-badge");
        public static Sprite UnconsciousBadge => _unconsciousBadge ??= Load("player-unconscious-icon-reference-badge");

        private static Sprite Load(string iconName)
        {
            string resourcePath = $"SenseOfDirection.Icons.{iconName}.png";
            using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourcePath);
            if (stream == null)
            {
                return null;
            }

            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            if (!texture.LoadImage(memoryStream.ToArray()))
            {
                return null;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), texture.width);
        }
    }
}
