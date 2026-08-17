using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SenseOfDirection.PirateCompass
{
    /// <summary>
    /// Prefixes the private <c>CompassPointer.UpdateHeading</c> (the method
    /// that both picks the heading for <em>and</em> rotates the real in-game
    /// compass needle every frame) for any Pirate's Compass, always - not
    /// just while <c>Pirate-Compass/clown-luggage-only</c> is on. Two
    /// independent problems, both real vanilla bugs found while building that
    /// setting:
    ///
    /// <list type="bullet">
    /// <item><b>No <c>IsOpen</c>/<c>activeInHierarchy</c> check at all.</b>
    /// Vanilla's own <c>UpdateHeadingPirate</c> iterates <c>Luggage.ALL_LUGGAGE</c>
    /// with neither check, so the needle can point at luggage that's already
    /// been opened, or - the one the maintainer actually caught - luggage
    /// left behind in a segment PEAK has since deactivated (it never unloads
    /// a passed segment's scene, just deactivates its root; see
    /// <see cref="Common.MapTargets"/>'s own doc comment on the same fact),
    /// which reads as the needle confidently pointing at "some hidden spot"
    /// with nothing reachable there at all. This mod's own indicator
    /// (<see cref="PirateCompassLuggageIndicatorController.FindNearestUnopenedLuggage"/>)
    /// already filters on both, so the needle now matches it exactly instead
    /// of vanilla's own gap.</item>
    /// <item><b>The "no luggage left" fallback is dead code.</b> Vanilla sets
    /// a slowly-rotating <c>heading</c> when <c>Luggage.ALL_LUGGAGE.Count == 0</c>,
    /// but <c>UpdateHeadingPirate</c> unconditionally overwrites <c>heading</c>
    /// again from <c>currentLuggageVector.normalized</c> a few lines later
    /// with no early return in between - so in practice the needle just
    /// freezes wherever it last pointed. Replaced here with an actual
    /// continuously-sweeping heading (not <c>CompassType.Warp</c>'s own
    /// <c>Transform.RotateAround</c> - an earlier version of this patch tried
    /// that verbatim and it tips the needle vertically instead of spinning,
    /// since that rotation is authored for the Warp needle's own mesh/rest
    /// orientation, not the Pirate needle's - see <see cref="Spin"/>) run
    /// through the exact same <c>ProjectOnPlane</c>+<c>Slerp</c> the
    /// found-a-target case already uses, so the needle keeps looking/moving
    /// like an actual compass needle throughout, just chasing a moving target
    /// instead of a fixed one.</item>
    /// </list>
    ///
    /// <c>Pirate-Compass/clown-luggage-only</c> only narrows *which* luggage
    /// counts (see <see cref="ClownLuggage"/>) on top of both fixes above -
    /// it doesn't gate whether this patch runs at all.
    ///
    /// Purely client-sided regardless: <c>CompassPointer.Update()</c> already
    /// recomputes the needle's heading independently on every client with no
    /// networking involved (confirmed via decompile - <c>heading</c>/
    /// <c>currentLuggageVector</c> are plain local fields, never synced over
    /// Photon), so this only ever changes what the local player's own client
    /// sees, for every Pirate's Compass in the level (not just their own held
    /// one) - the same "client-sided means my own client's view of
    /// everything" reasoning <see cref="Compass.CompassManager"/> already
    /// applies by reading <c>Plugin.Instance.Cfg</c> globally rather than
    /// per-item.
    /// </summary>
    public static class PirateCompassNeedlePatch
    {
        /// <summary>
        /// Degrees/second the needle sweeps at while nothing is left to point
        /// at. A fixed constant, not the item's own <c>warpSpeed</c>/
        /// <c>speedMultiplier</c> fields (an earlier version of this patch
        /// used those, matching <c>CompassType.Warp</c>'s own knobs) - a
        /// Pirate's Compass prefab never actually drives anything off them
        /// (vanilla's own dead-code fallback is the only place that ever
        /// reads <c>warpSpeed</c> for a Pirate compass, and nothing reads
        /// <c>speedMultiplier</c> for one at all), so there's no guarantee
        /// they're authored to any sensible value. Tuned to match the real
        /// Warp Compass' own apparent spin speed (maintainer-verified
        /// in-game - the first value tried, 120, spun at roughly 1/4.5 of
        /// it).
        /// </summary>
        private const float SpinDegreesPerSecond = 540f;

        /// <summary>
        /// Which way <see cref="Spin"/> turns, matching the real Warp
        /// Compass' own direction (maintainer-verified in-game - the first
        /// version of this spin turned the opposite way).
        /// </summary>
        private const float SpinDirection = -1f;

        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                var updateHeading = AccessTools.Method(typeof(CompassPointer), "UpdateHeading");
                harmony.Patch(updateHeading, prefix: new HarmonyMethod(typeof(PirateCompassNeedlePatch), nameof(Prefix)));

                log.LogInfo("PirateCompassNeedlePatch: patched CompassPointer.UpdateHeading.");
            }
            catch (Exception e)
            {
                log.LogError($"PirateCompassNeedlePatch.Apply failed (non-fatal, Pirate's Compass needle keeps its vanilla bugs): {e}");
            }
        }

        /// <summary>
        /// A *replacing* prefix (returns false when it takes over) - guarded
        /// for the same reason every other replacing prefix in this mod is:
        /// an unguarded exception here would propagate out of vanilla's own
        /// per-frame needle update instead of just skipping our own filtering.
        /// </summary>
        private static readonly Common.Safe.Context Guard =
            new Common.Safe.Context("PirateCompassNeedlePatch.Prefix (Pirate's Compass needle)", failureLimit: 300);

        private static bool Prefix(CompassPointer __instance, ref Vector3 ___heading, ref Vector3 ___currentLuggageVector, Item ___item)
        {
            if (Guard.Disabled)
            {
                return true;
            }
            try
            {
                bool result = PrefixImpl(__instance, ref ___heading, ref ___currentLuggageVector, ___item);
                Guard.Succeeded();
                return result;
            }
            catch (Exception e)
            {
                Guard.Failed(e);
                return true;
            }
        }

        private static bool PrefixImpl(CompassPointer __instance, ref Vector3 heading, ref Vector3 currentLuggageVector, Item item)
        {
            if (__instance.compassType != CompassPointer.CompassType.Pirate)
            {
                // Not our concern - Normal/Warp compasses are untouched.
                return true;
            }

            // Mirrors vanilla's own early-out in UpdateHeadingPirate: an item
            // not yet in the active list has nothing sensible to point from -
            // hand back to vanilla, which does the exact same no-op here.
            if (item == null || !item.inActiveList)
            {
                return false;
            }

            bool clownOnly = Plugin.Instance.Cfg.PirateCompassClownLuggageOnly.Value;
            Transform origin = __instance.transform;

            float nearestSqDistance = float.MaxValue;
            Vector3 nearestVector = Vector3.zero;
            bool found = false;

            foreach (Luggage luggage in Luggage.ALL_LUGGAGE)
            {
                // gameObject.activeInHierarchy and IsOpen are both checks
                // vanilla's own UpdateHeadingPirate never makes at all - see
                // this class's own doc comment for why both are needed to
                // actually match PirateCompassLuggageIndicatorController's
                // own (already-correct) search instead of vanilla's gap.
                if (luggage == null || luggage.IsOpen || !luggage.gameObject.activeInHierarchy || luggage.transform.position.y <= 0f)
                {
                    continue;
                }
                if (clownOnly && !ClownLuggage.Is(luggage))
                {
                    continue;
                }

                Vector3 toLuggage = luggage.Center() - origin.position;
                float sqDistance = toLuggage.sqrMagnitude;
                if (sqDistance < nearestSqDistance)
                {
                    nearestSqDistance = sqDistance;
                    toLuggage.y = 0f;
                    nearestVector = toLuggage;
                    found = true;
                }
            }

            currentLuggageVector = found ? nearestVector : Vector3.zero;
            Vector3 rawHeading = found ? nearestVector.normalized : Spin(origin);

            // Same post-processing UpdateHeading itself applies for every
            // non-Warp compass type - reproduced here since this prefix
            // replaces the whole method, not just UpdateHeadingPirate. Run
            // for the spin case too (not CompassType.Warp's own raw
            // Transform.RotateAround) so the needle keeps behaving like an
            // actual compass needle - level, LookRotation-oriented - while
            // sweeping, rather than tipping into whatever
            // RotateAround(needle.right) produces for a mesh authored to sit
            // flat rather than spin like Warp's own dial (see this class's
            // own doc comment). A no-op for the spin case specifically -
            // Spin already returns a vector perpendicular to origin.forward
            // by construction - but kept unconditional so both cases share
            // one code path.
            heading = Vector3.ProjectOnPlane(rawHeading, origin.forward).normalized;
            __instance.needle.rotation = Quaternion.LookRotation(
                Vector3.Slerp(__instance.needle.forward, heading, 10f * Time.deltaTime), origin.up);

            return false;
        }

        /// <summary>
        /// A continuously-rotating heading, standing in for a real target's
        /// direction when none is left - sweeps a full circle every
        /// <c>360 / SpinDegreesPerSecond</c> seconds. Rotates <paramref name="origin"/>'s
        /// own <c>up</c> around its own <c>forward</c> - i.e. entirely
        /// *within* the plane <see cref="PrefixImpl"/> already projects onto
        /// and orients the needle within (that plane's normal *is*
        /// <c>origin.forward</c>), rather than sweeping a full 3D circle
        /// around a fixed world axis and letting <c>ProjectOnPlane</c> flatten
        /// it into that plane after the fact. The first version of this
        /// method did exactly that (rotating <c>Vector3.forward</c> around
        /// world Y) - it looked like it was decelerating to a stop because a
        /// vector sweeping a great circle projects unevenly onto an unrelated
        /// plane, slowing to near-zero apparent angular speed as it passes
        /// close to <c>origin.forward</c> itself (where the projected
        /// component's length shrinks toward zero) before speeding back up
        /// on the far side - a very long, very uneven cycle at this method's
        /// deliberately slow base speed, easily mistaken for "it stopped."
        /// Rotating within the plane directly has no such pole to slow near.
        /// </summary>
        private static Vector3 Spin(Transform origin) =>
            Quaternion.AngleAxis(SpinDirection * Time.time * SpinDegreesPerSecond, origin.forward) * origin.up;
    }
}
