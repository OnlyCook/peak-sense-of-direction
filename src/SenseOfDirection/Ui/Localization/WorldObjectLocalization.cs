using System.Collections.Generic;

namespace SenseOfDirection.Ui.Localization
{
    /// <summary>
    /// Names for the stray world objects this mod labels on its own: creatures,
    /// hazards, traps and the other level props that <see cref="ItemPings.ItemPingDetector"/>
    /// and <see cref="ItemPings.PingableProps"/> detect.
    ///
    /// These need their own table because - unlike an <c>Item</c>, a
    /// <c>Luggage</c>, a <c>FakeItem</c> or a <c>GhostFire</c>, which all carry
    /// a localized name of their own that this mod simply reads - none of them
    /// has any name string in the game at all. A jellyfish, a geyser, a saw
    /// blade: the game never writes them down anywhere, so the mod has to.
    /// Every one of them was hardcoded English until now, which was the last
    /// thing in the mod that wasn't translated.
    ///
    /// Authored as <c>Localization/world.tsv</c> (<c>Key\tLanguage\tText</c>,
    /// one row per language) and read back through
    /// <see cref="LocalizationResource"/> exactly like the config/enum/chrome
    /// tables - the .csproj globs <c>Localization/*.tsv</c>, so the file needed
    /// no build change to be compressed and embedded.
    ///
    /// The campfire is deliberately absent: it already has
    /// <see cref="CampfireIndicator.CampfireLocalization"/>, the table the
    /// edge indicator has always used, and one name should not live in two
    /// tables that can drift apart.
    ///
    /// Keys are the kebab-case ids in that file; <see cref="Keys"/> holds them
    /// as constants so a typo is a compile error rather than a label silently
    /// falling back to English at runtime.
    /// </summary>
    internal static class WorldObjectLocalization
    {
        internal static class Keys
        {
            internal const string Pyre = "pyre";
            internal const string Jellyfish = "jellyfish";
            internal const string Spider = "spider";
            internal const string Capybara = "capybara";
            internal const string Zombie = "zombie";
            internal const string Antlion = "antlion";
            internal const string Pickaxe = "pickaxe";
            internal const string Piton = "piton";
            internal const string GiantUrchin = "giant-urchin";
            internal const string SporeBomb = "spore-bomb";
            internal const string PoisonSporeBomb = "poison-spore-bomb";
            internal const string ExplosiveSporeBomb = "explosive-spore-bomb";
            internal const string Icicle = "icicle";
            internal const string SnowPile = "snow-pile";
            internal const string Tumbleweed = "tumbleweed";
            internal const string PoisonIvy = "poison-ivy";
            internal const string Monstera = "monstera";
            internal const string Geyser = "geyser";
            internal const string FlashBulb = "flash-bulb";
            internal const string Flytrap = "flytrap";
            internal const string GhostBall = "ghost-ball";
            internal const string ArrowTrap = "arrow-trap";
            internal const string SpikeTrap = "spike-trap";
            internal const string SawBlade = "saw-blade";
            internal const string SwingingMace = "swinging-mace";
            internal const string SpikeRoller = "spike-roller";
        }

        private static readonly Dictionary<string, Dictionary<LocalizedText.Language, string>> Table = Load();

        private static Dictionary<string, Dictionary<LocalizedText.Language, string>> Load()
        {
            var table = new Dictionary<string, Dictionary<LocalizedText.Language, string>>();

            foreach (string[] row in LocalizationResource.ReadRows("world"))
            {
                // Key, Language, Text
                if (row.Length != 3 || !System.Enum.TryParse(row[1], out LocalizedText.Language language))
                {
                    continue;
                }

                if (!table.TryGetValue(row[0], out var perLanguage))
                {
                    perLanguage = new Dictionary<LocalizedText.Language, string>();
                    table[row[0]] = perLanguage;
                }
                perLanguage[language] = row[2];
            }

            return table;
        }

        /// <summary>
        /// The name for <paramref name="key"/> in the player's current language,
        /// falling back to English and finally to the key itself - a visible but
        /// harmless label rather than an empty one, and recognizable enough in a
        /// bug report to point straight at the missing row.
        ///
        /// Resolved per call rather than cached at detection time, so a player
        /// switching language mid-session sees it take effect on the next ping
        /// instead of keeping whatever was current when the level loaded.
        /// </summary>
        internal static string Get(string key)
        {
            if (!Table.TryGetValue(key, out var perLanguage))
            {
                return key;
            }
            if (perLanguage.TryGetValue(LocalizedText.CURRENT_LANGUAGE, out string name))
            {
                return name;
            }
            return perLanguage.TryGetValue(LocalizedText.Language.English, out string english) ? english : key;
        }
    }
}
