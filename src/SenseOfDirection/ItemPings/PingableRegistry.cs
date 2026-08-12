using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// Keeps a ready-to-use list of every pingable thing in the level, so an
    /// actual ping never has to go looking for one.
    ///
    /// <see cref="ItemPingDetector.FindNear"/> used to run one scene-wide
    /// <c>FindObjectsByType</c> per supported type - nine of them (Item,
    /// SlipperyJellyfish, Mob, Spider, Capybara, MushroomZombie, Antlion,
    /// ClimbHandle, CollisionModifier) - on every single accepted ping, on the
    /// RPC callback's own thread of execution, i.e. inside the frame the ping
    /// arrives. Each of those is O(every object in the level), so the whole
    /// detection pass scaled with the level's object count times nine, and it
    /// all landed in one frame: exactly the stutter reported in ISSUES.md,
    /// and it got worse the more people pinged at once (every incoming ping
    /// paid it again, independently).
    ///
    /// Two changes fix that:
    /// - One sweep instead of nine. A single <c>FindObjectsByType&lt;MonoBehaviour&gt;</c>
    ///   pass walks the level once and buckets what it finds by type. The
    ///   per-object type checks are plain C# <c>is</c> tests - far cheaper than
    ///   paying Unity's whole object-graph walk again per type. Objects are
    ///   deliberately allowed into more than one bucket (independent <c>if</c>s,
    ///   not <c>else if</c>/<c>switch</c>), exactly as nine independent typed
    ///   queries would have done - e.g. a creature that both derives from
    ///   <c>Mob</c> and has its own dedicated class still lands in both lists,
    ///   and <see cref="ItemPingDetector"/> dedupes by GameObject at match time.
    /// - Off the ping path entirely. The sweep never runs in response to a
    ///   ping; a ping only does cheap distance math against the already-built
    ///   lists.
    ///
    /// The buckets are kept current by <see cref="PingableRegistryPatches"/>,
    /// which registers each pingable as the game itself brings it to life
    /// (<see cref="NotifySpawned"/>). The full sweep is now only a
    /// *reconciliation* pass on top of that - scene load, segment change, and
    /// once every <see cref="ReconcileIntervalSeconds"/> - because measurement
    /// showed it costs 7-14ms of native, unsplittable query per run (the
    /// bucketing loop was 0.2-0.5ms of that) plus up to 632KB of garbage; see
    /// that class for the full numbers and why spreading it across frames was
    /// not the answer.
    ///
    /// Freshness: the only thing a stale bucket can miss is an object that
    /// came into existence without one of those hooks firing (a destroyed one
    /// is caught by the null checks at match time, and a moved one is fine -
    /// positions are read live, never cached). The one case that was already
    /// reachable in play before any of this is loot appearing mid-run (a
    /// luggage opening right in front of you, then someone pinging it a second
    /// later), so
    /// <see cref="ItemPingDetector"/> additionally unions in the game's own
    /// <c>Item.ALL_ACTIVE_ITEMS</c> at match time - though that list turned out
    /// to *not* actually cover luggage loot (confirmed via debug logging,
    /// ISSUES.md): those items spawn (and stay) kinematic until picked up, and
    /// <c>Item.WasActive()</c> - the only thing that adds an item to
    /// <c>ALL_ACTIVE_ITEMS</c> - is gated on <c>!rig.isKinematic</c>, so it's
    /// never called for them at all. <see cref="ItemPingDetector"/> closes
    /// that gap with a third, live source instead (a bounded
    /// <c>OverlapSphere</c> resolved through <c>Item.TryGetItemFromCollider</c>,
    /// unaffected by kinematic state) - this registry's own periodic sweep is
    /// still what eventually settles such an item into <see cref="Items"/>
    /// itself, just not fast enough for a ping landing in the first few
    /// seconds after a luggage opens.
    /// </summary>
    public class PingableRegistry : MonoBehaviour
    {
        /// <summary>
        /// The full sweep is now a *reconciliation* pass, not the way this
        /// registry stays current - <see cref="PingableRegistryPatches"/> feeds
        /// it live, and the sweep only exists to catch anything those hooks
        /// missed (a game update renaming a lifecycle method, an object that
        /// came into being some way the hooks don't see). Hence a minute rather
        /// than the five seconds it used to poll at: measured at 7-14ms of
        /// unsplittable native query per run, plus up to 632KB of garbage, it's
        /// something to do rarely and deliberately.
        /// </summary>
        private const float ReconcileIntervalSeconds = 60f;

        /// <summary>
        /// How often the cheap segment-number check below runs. A segment
        /// advancing is when a level's worth of static scenery (tree/bush loot,
        /// climb handles, hazards) activates all at once, so it's worth a full
        /// sweep on the spot rather than waiting out the reconciliation
        /// interval - the hooks cover objects that *spawn*, but scenery that was
        /// merely deactivated until its segment came up doesn't necessarily run
        /// a fresh <c>Awake</c>/<c>Start</c>. Polled rather than patched onto
        /// <c>MapHandler.GoToSegment</c>: it's a plain field read (see
        /// <c>Common.MapTargets.IsPastLastCampfire</c>), far less surface than
        /// another Harmony patch for the same answer.
        /// </summary>
        private const float SegmentPollIntervalSeconds = 1f;

        private static PingableRegistry _instance;

        public static PingableRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.PingableRegistry");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PingableRegistry>();
                }
                return _instance;
            }
        }

        private readonly List<Item> _items = new List<Item>();
        private readonly List<SlipperyJellyfish> _jellyfish = new List<SlipperyJellyfish>();
        private readonly List<Mob> _mobs = new List<Mob>();
        private readonly List<Spider> _spiders = new List<Spider>();
        private readonly List<Capybara> _capybaras = new List<Capybara>();
        private readonly List<MushroomZombie> _zombies = new List<MushroomZombie>();
        private readonly List<Antlion> _antlions = new List<Antlion>();
        private readonly List<ClimbHandle> _climbHandles = new List<ClimbHandle>();
        private readonly List<CollisionModifier> _urchins = new List<CollisionModifier>();

        /// <summary>
        /// Level props/hazards (traps, the flytrap, amulets still on their
        /// statue, ...) - see <see cref="PingableProps"/> for why these share
        /// one bucket instead of getting a list each. The display name is
        /// resolved at sweep time along with the type test, so a ping never
        /// re-derives it.
        /// </summary>
        private readonly List<PropTarget> _props = new List<PropTarget>();

        /// <summary>One level prop plus everything a ping needs to know about it.</summary>
        internal readonly struct PropTarget
        {
            internal readonly MonoBehaviour Behaviour;
            internal readonly string DisplayName;

            /// <summary>Match against the wider luggage/creature radius rather than the item radius - see <see cref="PingableProps"/>.</summary>
            internal readonly bool IsLarge;

            /// <summary>
            /// This prop's own renderers, resolved once here rather than per
            /// ping. Props are matched against their bounds, and a level can
            /// carry hundreds of them (684 in the Gloom Temple), so walking
            /// every prop's child hierarchy on every ping - allocating an array
            /// each time - would put real work back on the one path this whole
            /// registry exists to keep clear. The array is fixed for the life of
            /// the prop; only the bounds inside it move, and those are read live.
            /// </summary>
            internal readonly Renderer[] Renderers;

            /// <summary>Where this prop's indicator sits - see <see cref="PingableProps.PropAnchor"/>.</summary>
            internal readonly PingableProps.PropAnchor Anchor;

            /// <summary>Whether stray far-from-pivot renderers are excluded when measuring this prop - see <see cref="PingableProps.TryGetBounds"/>.</summary>
            internal readonly bool TrimToBody;

            internal PropTarget(MonoBehaviour behaviour, string displayName, bool isLarge, Renderer[] renderers, PingableProps.PropAnchor anchor, bool trimToBody)
            {
                Behaviour = behaviour;
                DisplayName = displayName;
                IsLarge = isLarge;
                Renderers = renderers;
                Anchor = anchor;
                TrimToBody = trimToBody;
            }
        }

        /// <summary>
        /// Everything already bucketed, so a re-registration is a no-op - see
        /// <see cref="Bucket"/>, which only puts things in here that actually
        /// landed in a bucket, not every behaviour it's handed. Holds destroyed
        /// objects until the next <see cref="Rebuild"/> prunes them, which is
        /// harmless: it's keyed on reference identity, and a destroyed object
        /// can never be handed back to <see cref="NotifySpawned"/> anyway.
        /// </summary>
        private readonly HashSet<MonoBehaviour> _known = new HashSet<MonoBehaviour>();

        public IReadOnlyList<Item> Items => _items;
        public IReadOnlyList<SlipperyJellyfish> Jellyfish => _jellyfish;
        public IReadOnlyList<Mob> Mobs => _mobs;
        public IReadOnlyList<Spider> Spiders => _spiders;
        public IReadOnlyList<Capybara> Capybaras => _capybaras;
        public IReadOnlyList<MushroomZombie> Zombies => _zombies;
        public IReadOnlyList<Antlion> Antlions => _antlions;
        public IReadOnlyList<ClimbHandle> ClimbHandles => _climbHandles;
        internal IReadOnlyList<PropTarget> Props => _props;

        /// <summary>
        /// Giant urchins, already resolved at sweep time: identified not by
        /// name but by a <c>CollisionModifier</c> whose parent carries a
        /// <c>DisableBasedOnRunSettings</c> gated on
        /// <c>Hazard_Urchins</c> (see <see cref="ItemPingDetector"/>). Doing
        /// that <c>GetComponent</c> chain here, once per sweep, keeps it off
        /// the ping path - the alternative was walking every
        /// <c>CollisionModifier</c> in the level (antlions share the component)
        /// and doing a parent <c>GetComponent</c> on each, per ping.
        /// </summary>
        public IReadOnlyList<CollisionModifier> Urchins => _urchins;

        private void Awake()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            // Straight after a load the old buckets refer to objects from the
            // previous scene (all destroyed), so the first ping in a fresh
            // level would otherwise find nothing at all until the periodic
            // sweep next came around.
            Rebuild();

            // ...and a sweep run at the exact moment the scene comes up can
            // still be too early for anything the level spawns in its own
            // first frames, which the next periodic sweep would then be up to
            // RebuildIntervalSeconds late on - i.e. exactly the window a player
            // pings in as they arrive.
            StartCoroutine(RebuildShortlyAfterLoad());
        }

        private IEnumerator RebuildShortlyAfterLoad()
        {
            yield return new WaitForSeconds(2f);
            Rebuild();
        }

        private void Start()
        {
            StartCoroutine(MaintenanceLoop());
        }

        /// <summary>
        /// Both slow cadences in one coroutine: a cheap segment check every
        /// second, and the reconciliation sweep once a minute.
        /// </summary>
        private IEnumerator MaintenanceLoop()
        {
            var wait = new WaitForSeconds(SegmentPollIntervalSeconds);
            float lastReconcileTime = Time.unscaledTime;
            int lastSegment = int.MinValue;

            while (true)
            {
                yield return wait;

                // Nothing to sweep (and nothing that could ping) outside an
                // actual level - main menu, loading, etc.
                if (Character.localCharacter == null)
                {
                    continue;
                }

                int segment = CurrentSegmentOrUnknown();
                if (segment != lastSegment)
                {
                    lastSegment = segment;
                    lastReconcileTime = Time.unscaledTime;
                    Rebuild();
                    continue;
                }

                if (Time.unscaledTime - lastReconcileTime >= ReconcileIntervalSeconds)
                {
                    lastReconcileTime = Time.unscaledTime;
                    Rebuild();
                }
            }
        }

        /// <summary>
        /// <c>MapHandler.CurrentSegmentNumber</c>, or a sentinel when there's no
        /// readable map. Returning a sentinel rather than throwing keeps the
        /// loop above simple; a transition into or out of "no map" reads as a
        /// segment change, which is a sweep worth running anyway.
        /// </summary>
        private static int CurrentSegmentOrUnknown()
        {
            try
            {
                return MapHandler.ExistsAndInitialized ? (int)MapHandler.CurrentSegmentNumber : int.MinValue + 1;
            }
            catch (System.Exception)
            {
                return int.MinValue + 1;
            }
        }

        /// <summary>
        /// Called from <see cref="PingableRegistryPatches"/> as the game brings
        /// each pingable to life. Runs inside the game's own lifecycle methods,
        /// so it does exactly what one iteration of the sweep's bucketing loop
        /// does and nothing more - the loop was measured at 0.2-0.5ms for tens
        /// of thousands of objects, so a single object's share of that is far
        /// below noise.
        /// </summary>
        internal void NotifySpawned(MonoBehaviour behaviour)
        {
            if (behaviour != null && Bucket(behaviour))
            {
                _liveRegistrations++;
            }
        }

        /// <summary>
        /// How many pingables the live hooks have added since the last sweep,
        /// reported in the sweep's own debug line. Purely diagnostic, but the
        /// one number that says whether
        /// <see cref="PingableRegistryPatches"/> is actually working: a run
        /// where this stays at zero while item counts still climb means the
        /// hooks have silently stopped firing and the reconciliation sweep is
        /// carrying everything on its own.
        /// </summary>
        private int _liveRegistrations;

        /// <summary>
        /// One <c>FindObjectsByType&lt;MonoBehaviour&gt;</c> sweep, merged into
        /// the buckets: destroyed entries are compacted away first, then
        /// everything the query returns is bucketed (already-known objects being
        /// a no-op, see <see cref="Bucket"/>).
        ///
        /// Merge rather than clear-and-refill, which is what this used to do.
        /// The query only returns *active* objects, so refilling from it throws
        /// away everything the live hooks registered that happens to be inactive
        /// at that instant - and a segment-change sweep fires exactly when a new
        /// segment's objects haven't all come up yet. Observed in a real run:
        /// a sweep reporting "0 items" moments after the hooks had registered
        /// 1355 pingables. Under the old 5s poll that self-corrected almost
        /// immediately; at a 60s reconciliation interval it would have left
        /// those items unpingable through the registry for a full minute.
        ///
        /// Nothing is lost by keeping an inactive object in a bucket: every
        /// consumer already re-checks <c>activeInHierarchy</c> at match time
        /// (an item inside an unopened luggage isn't pingable), and if it comes
        /// back it's correct again with no work.
        /// </summary>
        public void Rebuild()
        {
            bool measure = Plugin.Instance.Cfg.EnableDebugLogging.Value;
            Stopwatch stopwatch = measure ? Stopwatch.StartNew() : null;
            int liveRegistrations = _liveRegistrations;
            _liveRegistrations = 0;

            // Compaction - the other half of what "reconciliation" means here,
            // next to catching whatever the hooks missed. `== null` is Unity's
            // own overload, so a destroyed object counts as null even while the
            // managed reference is still around.
            int knownBefore = _known.Count;
            _items.RemoveAll(x => x == null);
            _jellyfish.RemoveAll(x => x == null);
            _mobs.RemoveAll(x => x == null);
            _spiders.RemoveAll(x => x == null);
            _capybaras.RemoveAll(x => x == null);
            _zombies.RemoveAll(x => x == null);
            _antlions.RemoveAll(x => x == null);
            _climbHandles.RemoveAll(x => x == null);
            _urchins.RemoveAll(x => x == null);
            _props.RemoveAll(x => x.Behaviour == null);
            _known.RemoveWhere(x => x == null);
            int pruned = knownBefore - _known.Count;

            // The query and the bucketing loop are timed separately (see
            // MeasureNote): the query is one atomic native call that can't be
            // split across frames, the loop can be - so which of the two
            // dominates decides what a fix can even look like.
            long memoryBefore = measure ? System.GC.GetTotalMemory(forceFullCollection: false) : 0L;
            MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            double queryMs = measure ? stopwatch.Elapsed.TotalMilliseconds : 0d;
            long queryBytes = measure ? System.GC.GetTotalMemory(forceFullCollection: false) - memoryBefore : 0L;

            foreach (MonoBehaviour behaviour in behaviours)
            {
                Bucket(behaviour);
            }

            if (stopwatch != null)
            {
                double totalMs = stopwatch.Elapsed.TotalMilliseconds;
                Plugin.Instance.Log.LogInfo(
                    $"PingableRegistry: swept in {totalMs:F1}ms (query {queryMs:F1}ms over {behaviours.Length} behaviours, "
                    + $"{queryBytes / 1024L}KB alloc | bucketing {totalMs - queryMs:F1}ms) - {_items.Count} items, "
                    + $"{_mobs.Count} mobs, {_zombies.Count} zombies, {_spiders.Count} spiders, {_capybaras.Count} capybaras, "
                    + $"{_jellyfish.Count} jellyfish, {_antlions.Count} antlions, {_climbHandles.Count} handles, {_urchins.Count} urchins, {_props.Count} props "
                    + $"({liveRegistrations} added live by the hooks since the last sweep, {pruned} destroyed pruned).");
            }
        }

        /// <summary>
        /// Puts one behaviour into every bucket it belongs in. Shared by the
        /// sweep and by the live hooks, so the two can't drift apart on what
        /// counts as pingable.
        ///
        /// Type tests are independent <c>if</c>s rather than
        /// <c>else if</c>/<c>switch</c>, exactly as the nine separate typed
        /// queries this replaced would have behaved - a creature that both
        /// derives from <c>Mob</c> and has its own class lands in both lists,
        /// and <see cref="ItemPingDetector"/> dedupes by GameObject at match
        /// time.
        ///
        /// <see cref="_known"/> guards against the same object being added
        /// twice, which the hooks make reachable in a way the old
        /// sweep-only design never was: <c>OnEnable</c> fires again every time
        /// an object is re-activated, and a sweep can run between a spawn and
        /// its next re-enable. Without it, one item could sit in
        /// <see cref="Items"/> several times over and get labeled "3x COCONUT"
        /// on its own.
        ///
        /// Only objects that actually land in a bucket are remembered - the
        /// sweep hands this *every* behaviour in the level, and an early
        /// version added all of them, which grew the set to 43,000 entries in a
        /// real run (seen via its own prune count) to track a few hundred
        /// pingables. Hence Contains-then-Add rather than Add's return value:
        /// one hash lookup for the overwhelming majority that match nothing,
        /// and the set stays the size of what's actually pingable.
        /// </summary>
        /// <returns>Whether this behaviour was newly bucketed (false = not pingable, or already known).</returns>
        private bool Bucket(MonoBehaviour behaviour)
        {
            if (_known.Contains(behaviour))
            {
                return false;
            }

            bool matched = false;
            if (behaviour is Item item)
            {
                _items.Add(item);
                matched = true;
            }
            if (behaviour is SlipperyJellyfish jellyfish)
            {
                _jellyfish.Add(jellyfish);
                matched = true;
            }
            if (behaviour is Mob mob)
            {
                _mobs.Add(mob);
                matched = true;
            }
            if (behaviour is Spider spider)
            {
                _spiders.Add(spider);
                matched = true;
            }
            if (behaviour is Capybara capybara)
            {
                _capybaras.Add(capybara);
                matched = true;
            }
            if (behaviour is MushroomZombie zombie)
            {
                _zombies.Add(zombie);
                matched = true;
            }
            if (behaviour is Antlion antlion)
            {
                _antlions.Add(antlion);
                matched = true;
            }
            if (behaviour is ClimbHandle climbHandle)
            {
                _climbHandles.Add(climbHandle);
                matched = true;
            }
            if (behaviour is CollisionModifier modifier && IsUrchin(modifier))
            {
                _urchins.Add(modifier);
                matched = true;
            }
            if (PingableProps.TryResolve(behaviour, out string propName, out bool propIsLarge, out PingableProps.PropAnchor propAnchor, out bool propTrim))
            {
                _props.Add(new PropTarget(behaviour, propName, propIsLarge, PingableProps.CollectRenderers(behaviour.gameObject), propAnchor, propTrim));
                matched = true;
            }

            if (matched)
            {
                _known.Add(behaviour);
            }
            return matched;
        }

        private static bool IsUrchin(CollisionModifier modifier)
        {
            Transform parent = modifier.transform.parent;
            if (parent == null)
            {
                return false;
            }
            DisableBasedOnRunSettings disabler = parent.GetComponent<DisableBasedOnRunSettings>();
            return disabler != null && disabler.disableIfSettingDisabled == RunSettings.SETTINGTYPE.Hazard_Urchins;
        }
    }
}
