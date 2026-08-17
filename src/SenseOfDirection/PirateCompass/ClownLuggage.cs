using UnityEngine;

namespace SenseOfDirection.PirateCompass
{
    /// <summary>
    /// Identifies Clown Luggage - confirmed via decompile
    /// (<c>AchievementManager.TestLuggageOpened</c> gates its own
    /// <c>ClownLuggageOpened</c> counter on exactly this same
    /// <c>gameObject.CompareTag("ClownLuggage")</c> check, so it's the game's
    /// own way of telling regular luggage apart from clown luggage at
    /// runtime - there's no dedicated <c>LuggageClown</c> subclass or
    /// per-instance field; <c>SpawnPool.LuggageClown</c> is a different
    /// thing entirely, a spawn-table flag consulted only while loot is being
    /// rolled, not something a runtime <c>Luggage</c> instance carries).
    /// Shared by <see cref="PirateCompassLuggageIndicatorController"/> (this
    /// mod's own indicator) and <see cref="PirateCompassNeedlePatch"/> (the
    /// real in-game needle), so <c>Pirate-Compass/clown-luggage-only</c>
    /// can't have the two disagree on what counts.
    /// </summary>
    internal static class ClownLuggage
    {
        private const string Tag = "ClownLuggage";

        internal static bool Is(Luggage luggage) =>
            luggage != null && luggage.gameObject.CompareTag(Tag);
    }
}
