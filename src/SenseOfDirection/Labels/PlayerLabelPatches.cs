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

        /// <summary>
        /// The single most important guard in the mod, and the reason
        /// <see cref="Common.Safe"/> exists at all.
        ///
        /// This postfix does real work - it builds a label's whole UI
        /// hierarchy, which pulls in the overlay canvas, the native font/icon
        /// assets and the config - and it does it from inside vanilla's
        /// <c>Character.Awake</c>. A Harmony postfix that throws does not fail
        /// quietly: the exception propagates out of <c>Character.Awake</c>
        /// itself, aborting whatever vanilla had left to do in that call chain
        /// and leaving a half-constructed Character in the scene. That is a
        /// cosmetic feature holding a core gameplay object hostage, and it is
        /// the shape of failure most likely to be reported as "the mod broke
        /// my game" (a Character that never finished waking up can be missing
        /// anything set up after our hook - voice handling included) rather
        /// than as "a label is missing".
        ///
        /// Auto-disables after 10 consecutive failures: if label creation is
        /// broken outright (a game update moved something we read), it will
        /// fail for every character forever, and continuing to throw once per
        /// spawn helps nobody.
        /// </summary>
        private static void AwakePostfix(Character __instance)
        {
            Common.Safe.Run(
                "PlayerLabelPatches.AwakePostfix (registering a player label)",
                () => PlayerLabelController.Instance.RegisterCharacter(__instance),
                failureLimit: 10);
        }

        /// <summary>
        /// Guarded for the same reason as <see cref="AwakePostfix"/>, but
        /// deliberately never auto-disabled: this is the cleanup half, and
        /// skipping it permanently would leak a label per character forever.
        /// A transient failure here costs one stale label, which
        /// <see cref="Common.SceneResetCoordinator"/> clears on the next scene
        /// load anyway.
        /// </summary>
        private static void OnDestroyPostfix(Character __instance)
        {
            Common.Safe.Run(
                "PlayerLabelPatches.OnDestroyPostfix (unregistering a player label)",
                () => PlayerLabelController.Instance.UnregisterCharacter(__instance));
        }
    }
}
