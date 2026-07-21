using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;

namespace SenseOfDirection.Compatibility
{
    /// <summary>
    /// PEAKSleepTalk (com.github.lokno.PEAKSleepTalk - last published 2024,
    /// no longer maintained) patches three vanilla methods:
    /// <c>CharacterVoiceHandler.Update</c> (the actual audio/volume mechanism
    /// behind its "let passed-out players talk" feature - harmless, and left
    /// alone here), <c>AnimatedMouth.ProcessMicData</c> (the matching mouth-
    /// flap animation), and <c>MainCameraMovement.HandleSpecSelection</c> (an
    /// <c>AllowSpectate</c>-gated tweak to who a passed-out player can
    /// spectate).
    ///
    /// <c>HandleSpecSelection</c>: confirmed via a real bug report + repro
    /// (see conversation/PR history) using diagnostic logging on both sides
    /// of <c>MainCameraMovement.LateUpdate</c> - with PEAKSleepTalk installed,
    /// a Harmony *prefix* on that method keeps firing every frame once the
    /// local player is fully passed out/dead, but the matching *postfix*
    /// (this mod's own ghost free-cam hook) silently stops, which only
    /// happens if the original method itself throws (plain postfixes never
    /// run when the method they postfix threw). <c>HandleSpecSelection</c> is
    /// called from <c>Spectate()</c>, itself only ever called once
    /// <c>fullyPassedOut</c> is true - i.e. exactly the method PEAKSleepTalk
    /// patches sits on the exact call path that starts failing at exactly the
    /// right moment. Its own <c>AllowSpectate</c> guard was
    /// <see langword="false"/> (i.e. a no-op) in the repro's actual config,
    /// but merely having *any* Harmony patch attached to the method changes
    /// its compiled form, which was enough to break it regardless of whether
    /// the patch's own logic ever ran.
    ///
    /// <c>ProcessMicData</c>: a follow-up bug report found voice-chat mouth
    /// animation broken entirely (for every talking player, not just passed-
    /// out ones) whenever PEAKSleepTalk's patch on this method was left in
    /// place, and working again once it was removed - the same "any patch
    /// here breaks the method outright" pattern as <c>HandleSpecSelection</c>,
    /// not a logic bug in the patch's own passed-out-specific branch (which
    /// would only ever affect passed-out characters, not everyone). Removing
    /// it costs only the mouth-flap animation specifically for a passed-out
    /// player using PEAKSleepTalk's own feature to talk - already a niche
    /// combination, and one vanilla itself never accounted for either
    /// (<c>ProcessMicData</c>'s own gate is <c>!dead &amp;&amp; !passedOut</c>,
    /// with no notion of "loud enough to animate but still passed out").
    ///
    /// <c>CharacterVoiceHandler.Update</c> is the one patch left alone - it's
    /// the actual audio/volume mechanism PEAKSleepTalk's feature depends on,
    /// and never caused either reported issue.
    ///
    /// Only patches owned by PEAKSleepTalk's own Harmony ID are touched, so
    /// any other mod's patches on the same methods are left alone; and
    /// <see cref="GhostFreeCamPatches"/>'s own <c>LateUpdate</c> finalizer is
    /// a general safety net (not PEAKSleepTalk-specific) against any *other*
    /// mod breaking that particular call chain the same way in the future.
    /// </summary>
    internal static class SleepTalkCompat
    {
        private const string SleepTalkHarmonyId = "com.github.lokno.PEAKSleepTalk";

        internal static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                bool removedSpecSelection = TryRemovePatches(harmony, AccessTools.Method(typeof(MainCameraMovement), "HandleSpecSelection"), log);
                bool removedMicData = TryRemovePatches(harmony, AccessTools.Method(typeof(AnimatedMouth), "ProcessMicData"), log);

                if (removedSpecSelection)
                {
                    log.LogInfo("SleepTalkCompat: removed PEAKSleepTalk's MainCameraMovement.HandleSpecSelection patch - it breaks vanilla spectate/ghost free-cam after death.");
                }
                if (removedMicData)
                {
                    log.LogInfo("SleepTalkCompat: removed PEAKSleepTalk's AnimatedMouth.ProcessMicData patch - it breaks voice-chat mouth animation for everyone. Its talk-while-passed-out audio (CharacterVoiceHandler.Update) is unaffected and keeps working.");
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
