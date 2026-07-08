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

        private static bool Prefix(UIPlayerNames __instance, int index)
        {
            if (!Plugin.Instance.Cfg.ReplaceVanillaLabels.Value)
            {
                return true;
            }
            if (index >= 0 && index < __instance.playerNameText.Length)
            {
                __instance.playerNameText[index].gameObject.SetActive(false);
            }
            return false;
        }
    }
}
