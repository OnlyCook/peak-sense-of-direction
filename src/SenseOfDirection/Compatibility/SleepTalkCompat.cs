using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace SenseOfDirection.Compatibility
{
    /// <summary>
    /// PEAKSleepTalk (com.github.lokno.PEAKSleepTalk - last published 2024,
    /// no longer maintained) patches <c>CharacterVoiceHandler.Update</c>,
    /// <c>AnimatedMouth.ProcessMicData</c> (its actual "let passed-out
    /// players talk" feature) and <c>MainCameraMovement.HandleSpecSelection</c>
    /// (an <c>AllowSpectate</c>-gated tweak to who a passed-out player can
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
    /// Rather than reverse-engineer the exact JIT/IL interaction (this mod is
    /// unmaintained upstream and the repro is clear), this surgically removes
    /// all three patches by Harmony owner ID once PEAKSleepTalk has had a
    /// chance to apply them - <em>only</em> patches owned by PEAKSleepTalk's
    /// own Harmony ID are touched, so any other mod's patches on the same
    /// methods are left alone. This does mean PEAKSleepTalk's actual feature
    /// (letting passed-out players talk, and its optional AllowSpectate
    /// tweak) stops doing anything while this mod is also installed - an
    /// accepted trade-off over a permanently broken ghost cam.
    /// </summary>
    internal static class SleepTalkCompat
    {
        private const string SleepTalkHarmonyId = "com.github.lokno.PEAKSleepTalk";

        internal static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                bool removedAny = false;
                removedAny |= TryRemovePatches(harmony, AccessTools.Method(typeof(CharacterVoiceHandler), "Update", System.Type.EmptyTypes), log);
                removedAny |= TryRemovePatches(harmony, AccessTools.Method(typeof(AnimatedMouth), "ProcessMicData"), log);

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
                removedAny |= TryRemovePatches(harmony, AccessTools.Method(typeof(MainCameraMovement), "HandleSpecSelection"), log);

                if (removedAny)
                {
                    log.LogInfo("SleepTalkCompat: removed PEAKSleepTalk's CharacterVoiceHandler.Update/AnimatedMouth.ProcessMicData patches - they leave CharacterData.fullyPassedOut in a state that breaks vanilla spectate/ghost free-cam after death. PEAKSleepTalk's own talk-while-passed-out feature will no longer do anything.");
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
