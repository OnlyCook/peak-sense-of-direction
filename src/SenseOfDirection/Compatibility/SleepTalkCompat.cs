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

        /// <summary>
        /// How often <see cref="Tick"/> re-checks for PEAKSleepTalk patches
        /// that appeared after the first sweep.
        ///
        /// The original design ran this exactly once, on the first
        /// <c>MainCameraMovement.LateUpdate</c> - late enough that every other
        /// plugin's <c>Awake</c> (and so PEAKSleepTalk's own
        /// <c>harmony.PatchAll()</c>) had certainly run. That covers the
        /// normal case, but it is a one-shot: a mod that patches lazily
        /// instead (on scene load, on joining a lobby, behind its own config
        /// toggle, or from a coroutine) re-applies the exact patch this
        /// removed and we would never notice. Since the failure that motivated
        /// this whole class only manifests *after a death in a run* - long
        /// after startup - a periodic re-check is the difference between
        /// "fixed" and "fixed unless it comes back". 10s is far more often
        /// than any mod realistically re-patches while costing a dictionary
        /// lookup.
        /// </summary>
        private const float RecheckIntervalSeconds = 10f;

        private static float _lastCheckTime = float.NegativeInfinity;

        private static Harmony _harmony;
        private static ManualLogSource _log;

        /// <summary>
        /// Stands up the watchdog that drives <see cref="Tick"/>.
        ///
        /// This used to be driven from <see cref="GhostFreeCamPatches"/>'s
        /// <c>MainCameraMovement.LateUpdate</c> postfix, which quietly made
        /// the compatibility fix conditional on that patch having applied
        /// successfully. That's backwards: the situations where a patch target
        /// fails to resolve (a game update, another mod claiming it first) are
        /// exactly the situations where compatibility handling matters most.
        /// Driving it from a plain component of our own means it runs
        /// regardless of what else in the mod did or didn't come up.
        /// </summary>
        internal static void Initialize(Harmony harmony, ManualLogSource log)
        {
            _harmony = harmony;
            _log = log;

            var go = new UnityEngine.GameObject("SenseOfDirection.CompatWatchdog");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<Watchdog>();
        }

        /// <summary>
        /// Nothing but a per-frame pump for <see cref="Tick"/>, which does its
        /// own rate limiting.
        /// </summary>
        private class Watchdog : UnityEngine.MonoBehaviour
        {
            private static readonly Common.Safe.Context Guard =
                new Common.Safe.Context("SleepTalkCompat.Watchdog", failureLimit: 300);

            private void Update()
            {
                if (Guard.Disabled)
                {
                    return;
                }
                try
                {
                    Tick(_harmony, _log);
                    Guard.Succeeded();
                }
                catch (System.Exception e)
                {
                    Guard.Failed(e);
                }
            }
        }

        /// <summary>
        /// Cheap per-frame entry point: re-runs <see cref="Apply"/> at most
        /// once every <see cref="RecheckIntervalSeconds"/>. Safe to call every
        /// frame, and a no-op when PEAKSleepTalk isn't installed (the whole
        /// check is a <c>GetPatchInfo</c> lookup that comes back empty).
        /// </summary>
        internal static void Tick(Harmony harmony, ManualLogSource log)
        {
            if (harmony == null || log == null)
            {
                return;
            }

            float now = UnityEngine.Time.unscaledTime;
            if (now - _lastCheckTime < RecheckIntervalSeconds)
            {
                return;
            }
            _lastCheckTime = now;

            Apply(harmony, log);
        }

        internal static void Apply(Harmony harmony, ManualLogSource log)
        {
            if (harmony == null)
            {
                return;
            }

            // Each target is resolved and unpatched independently. They used
            // to share one try block, which meant the first one failing to
            // resolve (a renamed method after a game update, say) silently
            // skipped the second - and the second is the voice-related one.
            TryRemoveFrom(harmony, log, () => AccessTools.Method(typeof(MainCameraMovement), "HandleSpecSelection"),
                "it breaks vanilla spectate/ghost free-cam after death.");
            TryRemoveFrom(harmony, log, () => AccessTools.Method(typeof(AnimatedMouth), "ProcessMicData"),
                "it breaks voice-chat mouth animation for everyone. Its talk-while-passed-out audio (CharacterVoiceHandler.Update) is unaffected and keeps working.");
        }

        /// <summary>
        /// <paramref name="resolve"/> is a delegate rather than an already-
        /// resolved <c>MethodBase</c> so that a missing *type* (not just a
        /// missing method) is caught here too - <c>typeof(SomeGameType)</c>
        /// throws a TypeLoadException at the point the enclosing method is
        /// JITted, so resolving inline in the caller would put it outside any
        /// guard this method could offer.
        /// </summary>
        private static void TryRemoveFrom(Harmony harmony, ManualLogSource log, System.Func<MethodBase> resolve, string why)
        {
            Common.Safe.Run("SleepTalkCompat: unpatching PEAKSleepTalk", () =>
            {
                MethodBase method = resolve();
                if (TryRemovePatches(harmony, method, log))
                {
                    log.LogInfo($"SleepTalkCompat: removed PEAKSleepTalk's {method.DeclaringType?.Name}.{method.Name} patch - {why}");
                }
            });
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

            // Transpilers and finalizers are checked alongside prefixes/
            // postfixes: the original only looked at the latter two, so a
            // PEAKSleepTalk build that used either of the others would have
            // been reported as "nothing to remove" while its patch stayed
            // live. Unpatch below is HarmonyPatchType.All either way, so the
            // detection needs to be All too or the two disagree.
            if (!OwnsAny(info.Prefixes) && !OwnsAny(info.Postfixes)
                && !OwnsAny(info.Transpilers) && !OwnsAny(info.Finalizers))
            {
                return false;
            }

            harmony.Unpatch(method, HarmonyPatchType.All, SleepTalkHarmonyId);
            return true;
        }

        private static bool OwnsAny(System.Collections.ObjectModel.ReadOnlyCollection<Patch> patches)
        {
            if (patches == null)
            {
                return false;
            }
            foreach (Patch p in patches)
            {
                if (p != null && p.owner == SleepTalkHarmonyId)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
