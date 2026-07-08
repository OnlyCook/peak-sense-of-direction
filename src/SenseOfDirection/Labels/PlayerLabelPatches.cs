using System;
using BepInEx.Logging;
using HarmonyLib;

namespace SenseOfDirection.Labels
{
    /// <summary>
    /// Registers/unregisters a <see cref="PlayerLabel"/> per <c>Character</c>
    /// as they spawn/despawn. <c>Character.Awake</c>/<c>OnDestroy</c> are
    /// private, so patched via <see cref="AccessTools"/> rather than the
    /// <c>[HarmonyPatch]</c> attribute form (matches this mod's own
    /// screen-space framework needing no reflection for public members, but
    /// these two do). Confirmed as the right lifecycle hook by
    /// `AiAeT-BetterPlayerDistance` doing the same (RESEARCH.md Q11).
    /// </summary>
    public static class PlayerLabelPatches
    {
        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                var awake = AccessTools.Method(typeof(Character), "Awake");
                harmony.Patch(awake, postfix: new HarmonyMethod(typeof(PlayerLabelPatches), nameof(AwakePostfix)));

                var onDestroy = AccessTools.Method(typeof(Character), "OnDestroy");
                harmony.Patch(onDestroy, postfix: new HarmonyMethod(typeof(PlayerLabelPatches), nameof(OnDestroyPostfix)));

                log.LogInfo("PlayerLabelPatches: patched Character.Awake/OnDestroy.");
            }
            catch (Exception e)
            {
                log.LogError($"PlayerLabelPatches.Apply failed (non-fatal, player labels won't work): {e}");
            }
        }

        private static void AwakePostfix(Character __instance)
        {
            PlayerLabelController.Instance.RegisterCharacter(__instance);
        }

        private static void OnDestroyPostfix(Character __instance)
        {
            PlayerLabelController.Instance.UnregisterCharacter(__instance);
        }
    }
}
