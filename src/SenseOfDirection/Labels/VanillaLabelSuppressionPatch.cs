using System;
using BepInEx.Logging;
using HarmonyLib;

namespace SenseOfDirection.Labels
{
    /// <summary>
    /// Backs <c>PluginConfig.ReplaceVanillaLabels</c> (off by default): when
    /// enabled, forces the game's own close-range player name labels off
    /// entirely so Sense of Direction's labels are the only ones shown,
    /// instead of the two systems handing off to each other.
    ///
    /// Prefixes `UIPlayerNames.UpdateName` (RESEARCH.md Q1) - the single
    /// method that shows/hides/positions every native name label - and skips
    /// it (forcing the slot inactive) when the setting is on.
    /// </summary>
    public static class VanillaLabelSuppressionPatch
    {
        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                var updateName = AccessTools.Method(typeof(UIPlayerNames), nameof(UIPlayerNames.UpdateName));
                harmony.Patch(updateName, prefix: new HarmonyMethod(typeof(VanillaLabelSuppressionPatch), nameof(Prefix)));

                log.LogInfo("VanillaLabelSuppressionPatch: patched UIPlayerNames.UpdateName.");
            }
            catch (Exception e)
            {
                log.LogError($"VanillaLabelSuppressionPatch.Apply failed (non-fatal, replace-vanilla-labels won't work): {e}");
            }
        }

        /// <summary>
        /// A *replacing* prefix (returns false), so it is doubly worth
        /// guarding: an exception here doesn't just skip our suppression, it
        /// propagates out of vanilla's own name-label update and can take the
        /// surrounding HUD refresh down with it, every frame. On any failure
        /// we fall back to <see langword="true"/> - i.e. let vanilla draw its
        /// own label. A duplicated name on screen is a far better outcome than
        /// a broken HUD, and it's self-correcting the moment the underlying
        /// problem clears.
        /// </summary>
        private static readonly Common.Safe.Context Guard =
            new Common.Safe.Context("VanillaLabelSuppressionPatch.Prefix (hiding a vanilla name label)", failureLimit: 300);

        private static bool Prefix(UIPlayerNames __instance, int index)
        {
            // Allocation-free guard: this runs once per player per frame.
            if (Guard.Disabled)
            {
                return true;
            }
            try
            {
                bool result = PrefixImpl(__instance, index);
                Guard.Succeeded();
                return result;
            }
            catch (Exception e)
            {
                Guard.Failed(e);
                return true;
            }
        }

        private static bool PrefixImpl(UIPlayerNames __instance, int index)
        {
            if (!Plugin.Instance.Cfg.ReplaceVanillaLabels.Value)
            {
                return true;
            }
            // playerNameText is read straight off a vanilla component, so it
            // can legitimately be null before the HUD has finished setting
            // itself up (and would NRE on .Length below). Nothing to suppress
            // yet in that case - hand the call back to vanilla.
            if (__instance == null || __instance.playerNameText == null)
            {
                return true;
            }
            if (index >= 0 && index < __instance.playerNameText.Length
                && __instance.playerNameText[index] != null)
            {
                __instance.playerNameText[index].gameObject.SetActive(false);
            }
            return false;
        }
    }
}
