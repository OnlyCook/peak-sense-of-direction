using System;
using BepInEx.Logging;
using HarmonyLib;

namespace SenseOfDirection.Ui
{
    /// <summary>
    /// Stops the vanilla pause menu from opening as a side effect of the same
    /// Escape press that just closed the preview menu (<see cref="PreviewMenu"/>).
    ///
    /// The obvious fix - clearing <c>Character.localCharacter.input.pauseWasPressed</c>
    /// right after closing - does not work, and was tried here first:
    /// <c>CharacterInput</c> re-derives that field from the Input System's
    /// <c>WasPressedThisFrame()</c> every frame, so whatever we write gets
    /// overwritten the next time that runs, whatever the ordering. Chasing the
    /// flag is a dead end.
    ///
    /// Instead the method that actually opens the menu
    /// (<c>GUIManager.UpdatePaused</c>) is prefixed and skipped exactly once,
    /// right after the preview menu closes on Escape. That doesn't depend on which
    /// MonoBehaviour's Update happens to run first this frame.
    ///
    /// Same approach (and same reasoning) as the maintainer's other PEAK mod,
    /// `peak-checkpoint-save`, whose F7 picker hit this exact problem - see its
    /// own PauseSuppressPatch.
    /// </summary>
    internal static class PauseSuppressPatch
    {
        private static bool _suppressOnce;

        internal static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                var target = AccessTools.Method(typeof(GUIManager), "UpdatePaused");
                if (target == null)
                {
                    log.LogWarning("PauseSuppressPatch: GUIManager.UpdatePaused not found; "
                        + "closing the preview menu with Escape may also open the pause menu.");
                    return;
                }

                harmony.Patch(target, prefix: new HarmonyMethod(typeof(PauseSuppressPatch), nameof(Prefix)));
            }
            catch (Exception e)
            {
                log.LogError($"PauseSuppressPatch.Apply failed (non-fatal): {e}");
            }
        }

        /// <summary>
        /// Called the moment Escape closes the preview menu: the game's very next
        /// <c>UpdatePaused()</c> call (later in this same frame) is skipped, so it
        /// can't open the pause menu off that press. Self-resetting - it never
        /// lingers into a later frame even if nothing consumes it.
        /// </summary>
        internal static void SuppressNextOpen() => _suppressOnce = true;

        private static bool Prefix()
        {
            if (!_suppressOnce)
            {
                return true;
            }

            _suppressOnce = false;
            return false; // skip UpdatePaused's body entirely for this one call
        }
    }
}
