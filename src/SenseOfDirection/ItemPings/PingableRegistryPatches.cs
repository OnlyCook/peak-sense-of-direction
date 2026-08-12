using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// Feeds <see cref="PingableRegistry"/> incrementally, so it no longer has
    /// to poll the whole scene graph to stay fresh.
    ///
    /// Why this exists (measured, not assumed - a full playthrough's worth of
    /// timings through every biome): the registry's periodic
    /// <c>FindObjectsByType&lt;MonoBehaviour&gt;</c> sweep cost 7-14ms in one
    /// frame, of which the bucketing loop was 0.2-0.5ms - the rest was the
    /// single native query, which can't be split across frames. Typed queries
    /// are no cheaper (<c>FindObjectsByType&lt;Capybara&gt;</c> took 3.35ms to
    /// return *zero* results): the price is the scene-graph traversal itself,
    /// not the type filter, so nine of them one-per-frame would have been ~4x
    /// the total CPU for a still-too-large per-frame cost. The query also
    /// allocated 120-632KB every 5 seconds (up to ~40,000 behaviours in the
    /// densest biome), i.e. a gen0 collection every few sweeps landing on some
    /// later frame - a stutter of its own that spreading the sweep would never
    /// have fixed.
    ///
    /// So the sweep had to become rare rather than spread, which is only safe
    /// if freshness stops depending on it: these postfixes register each
    /// pingable the moment the game itself brings it to life.
    /// <see cref="PingableRegistry"/> keeps a slow reconciliation sweep as the
    /// safety net, so a hook that a future game update quietly breaks degrades
    /// to "up to a minute late" rather than "never".
    ///
    /// The hook per type is whichever lifecycle method that type actually
    /// declares (confirmed against the decompile - they differ, and patching a
    /// method a class doesn't declare would silently no-op):
    /// <c>OnEnable</c> where one exists (<c>Item</c>, <c>Capybara</c>), since
    /// that also catches an object being re-activated rather than only
    /// constructed - which matters because the registry deliberately holds only
    /// active objects (an item inside an unopened luggage isn't pingable) -
    /// otherwise <c>Awake</c>, otherwise <c>Start</c>.
    ///
    /// Each patch is applied independently and its failure logged but
    /// swallowed: one renamed method should cost that one type its live
    /// updates, not take the whole registry (or the mod's startup) down with
    /// it.
    /// </summary>
    internal static class PingableRegistryPatches
    {
        internal static void Apply(Harmony harmony, ManualLogSource log)
        {
            PatchOne(harmony, log, typeof(Item), "OnEnable");
            PatchOne(harmony, log, typeof(Capybara), "OnEnable");
            PatchOne(harmony, log, typeof(Mob), "Awake");
            PatchOne(harmony, log, typeof(Spider), "Awake");
            PatchOne(harmony, log, typeof(MushroomZombie), "Awake");
            PatchOne(harmony, log, typeof(CollisionModifier), "Awake");
            PatchOne(harmony, log, typeof(SlipperyJellyfish), "Start");
            PatchOne(harmony, log, typeof(Antlion), "Start");
            PatchOne(harmony, log, typeof(ClimbHandle), "Start");
        }

        private static void PatchOne(Harmony harmony, ManualLogSource log, Type type, string methodName)
        {
            try
            {
                // DeclaredMethod, not Method: Item's own OnEnable overrides
                // MonoBehaviourPunCallbacks', and several of these types inherit
                // lifecycle methods they don't declare. Patching the *base*
                // would either fire for every unrelated component in the game or
                // not fire at all, depending on the type - neither is what's
                // wanted here.
                var target = AccessTools.DeclaredMethod(type, methodName);
                if (target == null)
                {
                    log.LogWarning(
                        $"PingableRegistryPatches: {type.Name}.{methodName} not found - that type will only be picked up "
                        + "by the reconciliation sweep (up to a minute late).");
                    return;
                }

                harmony.Patch(target, postfix: new HarmonyMethod(typeof(PingableRegistryPatches), nameof(RegisterPostfix)));
            }
            catch (Exception e)
            {
                log.LogError($"PingableRegistryPatches: failed to patch {type.Name}.{methodName} (non-fatal): {e}");
            }
        }

        /// <summary>
        /// Runs inside the game's own <c>Awake</c>/<c>Start</c>/<c>OnEnable</c>,
        /// so it has to be cheap and it must not throw - an exception here
        /// propagates into a game object's construction. It's one interface-free
        /// type test chain plus a <c>HashSet.Add</c>; see
        /// <see cref="PingableRegistry.NotifySpawned"/>.
        /// </summary>
        private static void RegisterPostfix(MonoBehaviour __instance)
        {
            try
            {
                PingableRegistry.Instance.NotifySpawned(__instance);
            }
            catch (Exception)
            {
                // Deliberately silent: this fires per spawned object, so a
                // logged failure would flood. A miss costs at most one
                // reconciliation interval of freshness.
            }
        }
    }
}
