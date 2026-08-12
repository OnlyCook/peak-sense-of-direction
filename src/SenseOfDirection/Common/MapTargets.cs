using UnityEngine;
using Zorro.Core;

namespace SenseOfDirection.Common
{
    /// <summary>
    /// Null-safe accessors for the two run-progress landmarks this mod points at:
    /// the current segment's campfire, and - once there is no campfire left to
    /// light - the summit itself.
    ///
    /// Both exist because vanilla's own <c>MapHandler.CurrentCampfire</c> is not
    /// safe to call unconditionally, and <c>MapHandler.ExistsAndInitialized</c>
    /// (the guard this mod used to rely on) is not enough to make it so. That
    /// property only reports <c>hasFinishedStartRoutine</c>; the getter itself is
    /// <c>GetCampfireRoot(currentSegment).GetComponentInChildren&lt;Campfire&gt;()</c>,
    /// and <c>GetCampfireRoot</c> returns whatever <c>segments[currentSegment].segmentCampfire</c>
    /// happens to be - which for <c>Segment.TheKiln</c> is nothing at all, because
    /// The Kiln has no campfire. So from the moment the run enters its final
    /// segment the getter dereferences null and throws, for the rest of the run.
    ///
    /// Note that the summit is *not* a segment of its own: <c>Segment.Peak</c>
    /// exists in the enum, but the only caller of <c>MapHandler.GoToSegment</c>
    /// anywhere is <c>Campfire.Light_Rpc</c> passing its own <c>advanceToSegment</c>,
    /// and with no campfire in The Kiln nothing ever advances the run to it -
    /// <c>currentSegment</c> never reaches 5 in normal play. The Peak is a
    /// progress point *inside* The Kiln, which is also why
    /// <c>MountainProgressHandler.CheckReached</c> has to special-case
    /// <c>point.biome == Biome.BiomeType.Peak</c> past its <c>BiomeIsPresent</c>
    /// test (the Peak is never in <c>MapHandler.biomes</c>), and why
    /// <c>JumpToSegmentLogic</c> maps a debug jump to Peak back onto The Kiln's
    /// own segment entry with <c>if (segment == Segment.Peak) num2--;</c>.
    ///
    /// The throw was happening every frame in the campfire indicator and on
    /// every ping in <c>ItemPings.ItemPingDetector</c> - where the surrounding
    /// try/catch quietly downgraded the whole ping to a vanilla one, taking item
    /// detection, ripple, scaling and distance labels with it.
    /// </summary>
    internal static class MapTargets
    {
        /// <summary>How long <see cref="AnyUnlitCampfireRemains"/>'s answer is reused before re-deriving it.</summary>
        private const float UnlitScanIntervalSeconds = 1f;

        private static float _unlitScanTime = float.NegativeInfinity;
        private static bool _unlitScanResult;

        /// <summary>
        /// <c>MapHandler.CurrentCampfire</c> with every way it can blow up treated
        /// as "there is no current campfire" instead. Callers get null rather than
        /// an exception; see this class' own summary for why the exception is a
        /// normal, expected end-of-run state and not a corrupted one.
        /// </summary>
        internal static Campfire CurrentCampfire()
        {
            if (!MapHandler.ExistsAndInitialized)
            {
                return null;
            }

            try
            {
                return MapHandler.CurrentCampfire;
            }
            catch (System.Exception)
            {
                // Deliberately silent: this is reached once per frame for the
                // whole final stretch of a run, so logging it would flood the
                // log with a state that is entirely normal.
                return null;
            }
        }

        /// <summary>
        /// Whether any campfire anywhere on the map is still unlit - i.e. whether
        /// there is still a "next campfire" for the run to be heading towards at
        /// all. Walks <c>MapHandler.segments</c> (bounded - one entry per biome)
        /// including inactive roots rather than doing a scene-wide search: every
        /// segment's campfire object exists from scene load, just deactivated
        /// until its segment is the current or previous one
        /// (<c>MapHandler.GoToSegment</c>), so a search restricted to active
        /// objects would report the not-yet-reached ones as absent. Throttled all
        /// the same, since callers poll it every frame.
        /// </summary>
        internal static bool AnyUnlitCampfireRemains()
        {
            if (Time.unscaledTime - _unlitScanTime < UnlitScanIntervalSeconds)
            {
                return _unlitScanResult;
            }
            _unlitScanTime = Time.unscaledTime;
            _unlitScanResult = ScanForUnlitCampfire();
            return _unlitScanResult;
        }

        private static bool ScanForUnlitCampfire()
        {
            if (!MapHandler.ExistsAndInitialized)
            {
                return false;
            }

            // "Saw no campfire at all" is not the same answer as "saw campfires,
            // all of them lit" - only the second one means the run is past its
            // last fire. Reporting the first as "none unlit" would put the
            // indicator on the summit during any state where the segment table
            // isn't readable yet, including the opening moments of a run.
            bool sawAny = false;

            try
            {
                MapHandler.MapSegment[] segments = Singleton<MapHandler>.Instance.segments;
                if (segments == null)
                {
                    return false;
                }

                foreach (MapHandler.MapSegment segment in segments)
                {
                    GameObject root = segment?.segmentCampfire;
                    if (root == null)
                    {
                        continue;
                    }

                    foreach (Campfire campfire in root.GetComponentsInChildren<Campfire>(includeInactive: true))
                    {
                        if (campfire == null)
                        {
                            continue;
                        }
                        sawAny = true;
                        if (campfire.state == Campfire.FireState.Off)
                        {
                            return true;
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // Silent for the same reason as CurrentCampfire above. Answers
                // "yes, unlit ones remain" - a segment table this can't read is
                // no evidence the run is past its last fire.
                return true;
            }

            // Nothing readable at all -> report "unlit campfires remain" (the
            // conservative answer: keep the indicator off rather than putting it
            // on the summit).
            return !sawAny;
        }

        /// <summary>
        /// Whether the run has advanced past its last campfire and onto the final
        /// climb. Read straight off <c>MapHandler.CurrentSegmentNumber</c> (a plain
        /// cast of the private <c>currentSegment</c> field - it can't throw, unlike
        /// <c>CurrentCampfire</c>).
        ///
        /// The threshold is <c>TheKiln</c>, not <c>Peak</c>: The Kiln is the final
        /// segment and has no campfire of its own, so lighting the Caldera's fire
        /// (which is what advances the run *into* The Kiln) is already the last one
        /// lit, and the summit is a sub-area of that same segment rather than a
        /// segment the run ever transitions to. A <c>&gt;= Peak</c> threshold would
        /// simply never fire. Comparing rather than equating all the same, so a
        /// debug jump straight to <c>Peak</c> (the one path that does set
        /// <c>currentSegment</c> to 5) still counts. Vanilla
        /// itself is written around that - <c>LastSeenCampfireIsSafe</c> stops
        /// consulting <c>CurrentCampfire</c> at <c>currentSegment &lt; 4</c>,
        /// <c>CurrentScoutStatue</c> returns null past segment 3,
        /// <c>PreviousScoutStatue</c> special-cases <c>TheKiln</c> onto
        /// <c>segmentParent</c> instead of <c>GetCampfireRoot</c>,
        /// <c>PreviousSegmentIsStillBaseCamp</c> hard-returns true at segment 4 so
        /// <c>CurrentBaseCampSpawnPoint</c> uses <c>PreviousCampfire</c>, and
        /// <c>GoToSegment</c> carries an explicit "NO CAMPFIRE SEGMENT" branch.
        /// Nothing in vanilla ever reads <c>CurrentCampfire</c> at segment 4,
        /// which is exactly why nobody noticed the getter throws there.
        ///
        /// <see cref="AnyUnlitCampfireRemains"/> stays as a belt-and-braces second
        /// route into the same state; it's OR'd with this, so it can only ever add
        /// peak tracking, never suppress it. That matters because a run started
        /// mid-mountain (a jump straight to The Kiln, e.g. peak-checkpoint-save's
        /// own resume) leaves every earlier campfire unlit forever, so the scan
        /// alone would answer "campfires remain" for the rest of the run.
        /// </summary>
        internal static bool IsPastLastCampfire()
        {
            if (!MapHandler.ExistsAndInitialized)
            {
                return false;
            }

            try
            {
                return (int)MapHandler.CurrentSegmentNumber >= (int)Segment.TheKiln;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Whether the run is currently down in the Nadir (PEAK 2.0's Void
        /// sub-biome), where none of the above applies: there is no campfire to
        /// light and the summit is not what anyone is heading for - the way out
        /// is the portal (see <see cref="PortalTransform"/>).
        ///
        /// Read off <c>MapHandler.inNadir</c> (<c>GetCurrentBiome() ==
        /// BiomeType.Void</c>) rather than <c>VoidBiome.VoidBiomeActive</c>,
        /// because the two answer subtly different questions: the segment is what
        /// the run is *in*, while <c>VoidBiome.isActive</c> is a one-way flag its
        /// own <c>Activate()</c> sets and only <c>Deactivate()</c> clears.
        /// <c>inNadir</c> can throw, though - it indexes
        /// <c>segments[currentSegment]</c>, and the Void segment is *appended* to
        /// that array by <c>MapHandler.SetUpVoidSegment</c> (it isn't there at
        /// scene load), so the index is briefly out of range while the run is
        /// entering the biome. The static flag is the fallback for exactly that
        /// window.
        /// </summary>
        internal static bool IsInNadir()
        {
            if (!MapHandler.ExistsAndInitialized)
            {
                return false;
            }

            try
            {
                return Singleton<MapHandler>.Instance.inNadir;
            }
            catch (System.Exception)
            {
                // Silent for the same reason as CurrentCampfire above - this is
                // polled every frame.
            }

            try
            {
                return Peak.VoidBiome.VoidBiomeActive;
            }
            catch (System.Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// The Nadir's exit portal - <c>Peak.PeakGatePortal</c>, the interactible
        /// whose own cast ends the run (<c>Interact_CastFinished</c> ->
        /// <c>Character.EndGame()</c>, which is where <c>CharacterStats.wonViaNadir</c>
        /// gets set). It's the one class of its kind in the whole decompile, so no
        /// name matching or ping-log identification was needed here, unlike the
        /// summit flag <see cref="FindPeakFlag"/> has to go hunting for.
        ///
        /// Cached the same way <see cref="PeakTransform"/> is, and for the same
        /// reason: a scene-wide component search is not something to run per
        /// frame. Unity's null-overload handles invalidation for free - a
        /// destroyed portal (scene unload, leaving the biome) compares equal to
        /// null and re-resolves.
        /// </summary>
        internal static Transform PortalTransform()
        {
            if (_cachedPortal != null)
            {
                return _cachedPortal;
            }
            if (Time.unscaledTime - _portalResolveTime < PortalResolveIntervalSeconds)
            {
                return null;
            }
            _portalResolveTime = Time.unscaledTime;
            _cachedPortal = ResolvePortalTransform();
            return _cachedPortal;
        }

        private static Transform _cachedPortal;
        private static float _portalResolveTime = float.NegativeInfinity;
        private const float PortalResolveIntervalSeconds = 2f;

        private static Transform ResolvePortalTransform()
        {
            try
            {
                // Inactive objects included: the Void biome's own root is only
                // activated once the run actually goes there (VoidBiome.Activate),
                // and this resolve can land in that same frame. Only ever called
                // while IsInNadir() already says the run is in the biome, so
                // there's no risk of latching onto some other scene's portal.
                Peak.PeakGatePortal[] portals = Object.FindObjectsByType<Peak.PeakGatePortal>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                if (portals == null || portals.Length == 0)
                {
                    return null;
                }

                // One is the normal case. If a level ever has more, the nearest
                // one is the one worth pointing at - and an active one always
                // beats an inactive one regardless of distance.
                Peak.PeakGatePortal best = null;
                bool bestActive = false;
                float bestDistanceSq = float.PositiveInfinity;
                Vector3 from = Character.localCharacter != null
                    ? Character.localCharacter.Center
                    : Vector3.zero;

                foreach (Peak.PeakGatePortal portal in portals)
                {
                    if (portal == null)
                    {
                        continue;
                    }
                    bool active = portal.gameObject.activeInHierarchy;
                    float distanceSq = (portal.transform.position - from).sqrMagnitude;
                    if (best == null || (active && !bestActive) || (active == bestActive && distanceSq < bestDistanceSq))
                    {
                        best = portal;
                        bestActive = active;
                        bestDistanceSq = distanceSq;
                    }
                }

                return best != null ? best.transform : null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// The summit landmark to point at once <see cref="IsPastLastCampfire"/> /
        /// <see cref="AnyUnlitCampfireRemains"/> say there's no campfire left to
        /// head for. Three candidates, best first:
        ///
        /// 1. The flag planted at the top - the thing that visually *is* the peak,
        ///    and what the maintainer asked the indicator to aim at. It has no
        ///    component of its own anywhere in the decompile (it's plain scene
        ///    dressing, which is also why a ping aimed at it lands on the
        ///    <c>Rock_Platform</c>/<c>Rock_Round</c> underneath it instead), so
        ///    it's found by name within <c>PeakHandler</c>'s own hierarchy - the
        ///    subtree the whole summit area lives under, confirmed by those same
        ///    ping dumps reporting <c>parent:PeakHandler</c>.
        /// 2. <c>MountainProgressHandler</c>'s last progress point - the transform
        ///    the game's own <c>IsAtPeak</c> measures against, so it's the point
        ///    the run is actually won at. Correct in Z by construction; nothing
        ///    guarantees its X/Y sit anywhere meaningful, which is why it's the
        ///    fallback rather than the primary.
        /// 3. <c>MapHandler.respawnThePeak</c>, where the game teleports players
        ///    that jump straight to the Peak segment.
        ///
        /// Returned as the live <c>Transform</c> rather than a snapshot position so
        /// the indicator keeps tracking it if the game ever moves it.
        /// </summary>
        internal static Transform PeakTransform()
        {
            // Cached hard, because resolving is not cheap: the flag search below
            // walks every Transform under PeakHandler, and that subtree is ~7,000
            // objects (the summit's LOD sets dominate it). Running that per frame
            // was a very visible stutter for the whole final climb. Unity's own
            // null-overload does the invalidation for free - a destroyed transform
            // (scene unload, returning to the airport) compares equal to null and
            // re-resolves. The retry throttle only applies while nothing has been
            // found yet; once it has, this is a plain field read forever.
            if (_cachedPeak != null)
            {
                return _cachedPeak;
            }
            if (Time.unscaledTime - _peakResolveTime < PeakResolveIntervalSeconds)
            {
                return null;
            }
            _peakResolveTime = Time.unscaledTime;
            _cachedPeak = ResolvePeakTransform();
            return _cachedPeak;
        }

        private static Transform _cachedPeak;
        private static float _peakResolveTime = float.NegativeInfinity;
        private const float PeakResolveIntervalSeconds = 2f;

        private static Transform ResolvePeakTransform()
        {
            try
            {
                Transform flag = FindPeakFlag();
                if (flag != null)
                {
                    return flag;
                }

                var progress = Singleton<MountainProgressHandler>.Instance;
                if (progress != null && progress.progressPoints != null && progress.progressPoints.Length > 0)
                {
                    Transform peak = progress.progressPoints[progress.progressPoints.Length - 1]?.transform;
                    if (peak != null)
                    {
                        return peak;
                    }
                }

                if (MapHandler.Exists)
                {
                    Transform respawn = Singleton<MapHandler>.Instance.respawnThePeak;
                    if (respawn != null)
                    {
                        return respawn;
                    }
                }
            }
            catch (System.Exception)
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// The flag's own cloth, whose transform is the one worth aiming at. The
        /// observed hierarchy is
        /// <c>Flag_planted_seagull</c> -> <c>Flag Pole</c> -> <c>flag</c>, all three
        /// matching a naive "contains flag" test - and the outer two sit at the
        /// pole's *base*, on the ground, which reads as the indicator pointing at
        /// the dirt next to the flag rather than at the flag. So an exact-name pass
        /// runs first and picks the innermost one out.
        /// </summary>
        private const string FlagExactName = "flag";

        /// <summary>Fallback name fragments a summit flag might carry if the exact match above misses, most specific first.</summary>
        private static readonly string[] FlagNameFragments = { "peakflag", "flagpole", "flag", "banner", "summit" };

        private static Transform FindPeakFlag()
        {
            Transform root = PeakRoot();
            if (root == null)
            {
                return null;
            }

            Transform[] all = root.GetComponentsInChildren<Transform>(includeInactive: true);

            foreach (Transform candidate in all)
            {
                if (candidate != null && string.Equals(candidate.name, FlagExactName, System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }

            foreach (string fragment in FlagNameFragments)
            {
                foreach (Transform candidate in all)
                {
                    // IndexOf over ToLowerInvariant().Contains: this runs across
                    // every transform in a very large subtree, and the lowercase
                    // copy was one string allocation per transform per fragment.
                    if (candidate != null && candidate.name.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private static Transform PeakRoot()
        {
            try
            {
                var peakHandler = Singleton<PeakHandler>.Instance;
                return peakHandler != null ? peakHandler.transform : null;
            }
            catch (System.Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Debug-logging-only: dumps every candidate summit anchor with its world
        /// position, plus <c>PeakHandler</c>'s own hierarchy. None of the summit
        /// dressing has a class of its own in the decompile, so which transform to
        /// aim at is a question only a live scene can answer - this is how that
        /// gets answered without another decompile pass, the same role
        /// <c>ItemPings.ItemPingDetector.LogNearbyUnmatched</c> plays for
        /// still-unsupported pingables.
        /// </summary>
        internal static void LogPeakCandidates()
        {
            var log = Plugin.Instance.Log;
            log.LogInfo($"MapTargets: peak candidates (segment={SafeSegmentName()})");

            Transform flag = FindPeakFlag();
            log.LogInfo($"  flag-by-name: {Describe(flag)}");

            try
            {
                var progress = Singleton<MountainProgressHandler>.Instance;
                if (progress?.progressPoints != null && progress.progressPoints.Length > 0)
                {
                    var last = progress.progressPoints[progress.progressPoints.Length - 1];
                    log.LogInfo($"  progressPoints.Last() '{last?.title}': {Describe(last?.transform)}");
                }
                else
                {
                    log.LogInfo("  progressPoints: unavailable");
                }
            }
            catch (System.Exception e)
            {
                log.LogInfo($"  progressPoints: threw {e.GetType().Name}");
            }

            try
            {
                log.LogInfo($"  respawnThePeak: {Describe(MapHandler.Exists ? Singleton<MapHandler>.Instance.respawnThePeak : null)}");
            }
            catch (System.Exception e)
            {
                log.LogInfo($"  respawnThePeak: threw {e.GetType().Name}");
            }

            Transform root = PeakRoot();
            if (root == null)
            {
                log.LogInfo("  PeakHandler: not found");
                return;
            }

            // Depth 2 and hard-capped. Depth 3 (what actually found the flag the
            // first time) emitted ~7,000 lines in a single frame - the summit's
            // LOD sets are most of that subtree - which is a stutter in its own
            // right and buries the three candidate lines above it. The flag is
            // reported by `flag-by-name` regardless, so the tree is only here for
            // the case where that misses entirely.
            log.LogInfo($"  PeakHandler hierarchy (depth<=2, first {MaxHierarchyDumpLines}) under {Describe(root)}:");
            int budget = MaxHierarchyDumpLines;
            DumpChildren(root, 1, 2, ref budget);
            if (budget <= 0)
            {
                log.LogInfo("    ... (truncated)");
            }
        }

        private const int MaxHierarchyDumpLines = 80;

        private static void DumpChildren(Transform parent, int depth, int maxDepth, ref int budget)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (budget <= 0)
                {
                    return;
                }
                budget--;

                Transform child = parent.GetChild(i);
                Plugin.Instance.Log.LogInfo($"  {new string(' ', depth * 2)}- {Describe(child)}");
                if (depth < maxDepth)
                {
                    DumpChildren(child, depth + 1, maxDepth, ref budget);
                }
            }
        }

        private static string Describe(Transform t) =>
            t == null ? "(none)" : $"'{t.name}' at {t.position}";

        private static string SafeSegmentName()
        {
            try
            {
                return MapHandler.ExistsAndInitialized ? MapHandler.CurrentSegmentNumber.ToString() : "(no map)";
            }
            catch (System.Exception)
            {
                return "(unreadable)";
            }
        }
    }
}
