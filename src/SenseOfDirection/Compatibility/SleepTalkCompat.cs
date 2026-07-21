using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace SenseOfDirection.Compatibility
{
    /// <summary>
    /// PEAKSleepTalk (com.github.lokno.PEAKSleepTalk - last published 2024,
    /// no longer maintained) patches three vanilla methods:
    /// <c>CharacterVoiceHandler.Update</c>/<c>AnimatedMouth.ProcessMicData</c>
    /// (its actual "let passed-out players talk" feature - harmless, and left
    /// alone here) and <c>MainCameraMovement.HandleSpecSelection</c> (an
    /// <c>AllowSpectate</c>-gated tweak to who a passed-out player can
    /// spectate). Confirmed via a real bug report + repro (see conversation/
    /// PR history) using diagnostic logging on both sides of
    /// <c>MainCameraMovement.LateUpdate</c>: with PEAKSleepTalk installed, a
    /// Harmony *prefix* on that method keeps firing every frame once the
    /// local player is fully passed out/dead, but the matching *postfix*
    /// (this mod's own ghost free-cam hook) silently stops - which only
    /// happens if the original method itself throws, since postfixes never
    /// run when the method they postfix threw (they're plain postfixes, not
    /// finalizers). <c>HandleSpecSelection</c> is called from <c>Spectate()</c>,
    /// itself only ever called once <c>fullyPassedOut</c> is true - i.e.
    /// exactly the method PEAKSleepTalk patches sits on the exact call path
    /// that starts failing at exactly the right moment. Its own
    /// <c>AllowSpectate</c> guard was <see langword="false"/> (i.e. a no-op)
    /// in the repro's actual config, but merely having *any* Harmony patch
    /// attached to a method changes its compiled form, which was enough to
    /// break it regardless of whether the patch's own logic ever ran.
    ///
    /// Only that one patch is removed - <see cref="GhostFreeCamPatches"/>'s
    /// own <c>LateUpdate</c> finalizer is what actually protects us from any
    /// exception in this call chain now (belt-and-suspenders for future/other
    /// mods), so there's no need to touch the mic-related patches at all;
    /// they never caused the reported issue, and disabling them would break
    /// PEAKSleepTalk's actual feature for no benefit. Only patches owned by
    /// PEAKSleepTalk's own Harmony ID are touched, so any other mod's patches
    /// on the same method are left alone.
    /// </summary>
    internal static class SleepTalkCompat
    {
        private const string SleepTalkHarmonyId = "com.github.lokno.PEAKSleepTalk";

        internal static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                // PassedOutSpectatePatch: sits directly on the one vanilla
                // method (MainCameraMovement.HandleSpecSelection, called from
                // Spectate(), itself only ever called once fullyPassedOut is
                // true) in the exact call path that starts failing once a
                // player is fully passed out/dead - even though its own
                // AllowSpectate=false guard is normally false (a no-op) in the
                // reported repro's config, merely having any Harmony patch
                // attached to this specific method is enough to break it for
                // everyone downstream (including our own postfix on
                // LateUpdate, which never gets a chance to run if the
                // original throws - see GhostFreeCamPatches' own diagnostics).
                // CharacterVoiceHandler.Update/AnimatedMouth.ProcessMicData
                // are deliberately left untouched - they're PEAKSleepTalk's
                // actual "talk while passed out" feature and were never
                // responsible for the ghost-cam breakage.
                bool removedAny = TryRemovePatches(harmony, AccessTools.Method(typeof(MainCameraMovement), "HandleSpecSelection"), log);

                if (removedAny)
                {
                    log.LogInfo("SleepTalkCompat: removed PEAKSleepTalk's MainCameraMovement.HandleSpecSelection patch - it breaks vanilla spectate/ghost free-cam after death. Its talk-while-passed-out feature is unaffected and keeps working.");
                }
            }
            catch (System.Exception e)
            {
                log.LogWarning($"SleepTalkCompat.Apply failed (non-fatal, no compatibility patch applied): {e}");
            }
        }

        private static bool TryRemovePatches(Harmony harmony, MethodBase method, ManualLogSource log)
        {
            if (method == null)
            {
                return false;
            }

            Patches info = Harmony.GetPatchInfo(method);
            if (info == null)
            {
                return false;
            }

            bool owned = false;
            foreach (Patch p in info.Prefixes) if (p.owner == SleepTalkHarmonyId) { owned = true; break; }
            if (!owned) foreach (Patch p in info.Postfixes) if (p.owner == SleepTalkHarmonyId) { owned = true; break; }
            if (!owned)
            {
                return false;
            }

            harmony.Unpatch(method, HarmonyPatchType.All, SleepTalkHarmonyId);
            log.LogInfo($"SleepTalkCompat: removed PEAKSleepTalk patches from {method.DeclaringType?.Name}.{method.Name}.");
            return true;
        }
    }
}
