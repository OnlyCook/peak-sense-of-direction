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
    /// - Off the ping path entirely. The sweep runs on a slow background
    ///   cadence (<see cref="RebuildIntervalSeconds"/>) and immediately after a
    ///   scene load, never in response to a ping. A ping then only does cheap
    ///   distance math against the already-built lists.
    ///
    /// Freshness: the only thing a stale bucket can miss is an object that
    /// came into existence since the last sweep (a destroyed one is caught by
    /// the null checks at match time, and a moved one is fine - positions are
    /// read live, never cached). The one case where that gap is actually
    /// reachable in play is loot appearing mid-run (a luggage opening right in
    /// front of you, then someone pinging it a second later), so
    /// <see cref="ItemPingDetector"/> additionally unions in the game's own
    /// <c>Item.ALL_ACTIVE_ITEMS</c> - a short, live list that a freshly spawned
    /// (non-kinematic) item always lands in - at match time. That's cheap
    /// because it's small, and it covers precisely the objects a periodic sweep
    /// can be behind on.
    /// </summary>
    public class PingableRegistry : MonoBehaviour
    {
        /// <summary>
        /// Slow on purpose: the sweep is the expensive thing this class exists
        /// to keep away from the ping path, so re-running it often would just
        /// move the same cost onto a random frame instead of removing it. What
        /// a level's worth of static loot/creatures/handholds looks like barely
        /// changes second to second, and the one genuinely dynamic case
        /// (freshly spawned items) is covered live at match time - see the
        /// class doc.
        /// </summary>
        private const float RebuildIntervalSeconds = 5f;

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

        public IReadOnlyList<Item> Items => _items;
        public IReadOnlyList<SlipperyJellyfish> Jellyfish => _jellyfish;
        public IReadOnlyList<Mob> Mobs => _mobs;
        public IReadOnlyList<Spider> Spiders => _spiders;
        public IReadOnlyList<Capybara> Capybaras => _capybaras;
        public IReadOnlyList<MushroomZombie> Zombies => _zombies;
        public IReadOnlyList<Antlion> Antlions => _antlions;
        public IReadOnlyList<ClimbHandle> ClimbHandles => _climbHandles;

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
            StartCoroutine(RebuildLoop());
        }

        private IEnumerator RebuildLoop()
        {
            var wait = new WaitForSeconds(RebuildIntervalSeconds);
            while (true)
            {
                yield return wait;

                // Nothing to sweep (and nothing that could ping) outside an
                // actual level - main menu, loading, etc.
                if (Character.localCharacter != null)
                {
                    Rebuild();
                }
            }
        }

        /// <summary>
        /// One <c>FindObjectsByType&lt;MonoBehaviour&gt;</c> sweep, bucketed by
        /// type. Inactive objects are excluded by the query itself (that's
        /// <c>FindObjectsByType</c>'s default) - an item still inside an
        /// unopened luggage isn't pingable anyway, and one that gets
        /// deactivated later is caught by the liveness checks at match time.
        /// </summary>
        public void Rebuild()
        {
            Stopwatch stopwatch = Plugin.Instance.Cfg.EnableDebugLogging.Value ? Stopwatch.StartNew() : null;

            _items.Clear();
            _jellyfish.Clear();
            _mobs.Clear();
            _spiders.Clear();
            _capybaras.Clear();
            _zombies.Clear();
            _antlions.Clear();
            _climbHandles.Clear();
            _urchins.Clear();

            foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour is Item item)
                {
                    _items.Add(item);
                }
                if (behaviour is SlipperyJellyfish jellyfish)
                {
                    _jellyfish.Add(jellyfish);
                }
                if (behaviour is Mob mob)
                {
                    _mobs.Add(mob);
                }
                if (behaviour is Spider spider)
                {
                    _spiders.Add(spider);
                }
                if (behaviour is Capybara capybara)
                {
                    _capybaras.Add(capybara);
                }
                if (behaviour is MushroomZombie zombie)
                {
                    _zombies.Add(zombie);
                }
                if (behaviour is Antlion antlion)
                {
                    _antlions.Add(antlion);
                }
                if (behaviour is ClimbHandle climbHandle)
                {
                    _climbHandles.Add(climbHandle);
                }
                if (behaviour is CollisionModifier modifier && IsUrchin(modifier))
                {
                    _urchins.Add(modifier);
                }
            }

            if (stopwatch != null)
            {
                Plugin.Instance.Log.LogInfo(
                    $"PingableRegistry: swept in {stopwatch.Elapsed.TotalMilliseconds:F1}ms - {_items.Count} items, "
                    + $"{_mobs.Count} mobs, {_zombies.Count} zombies, {_spiders.Count} spiders, {_capybaras.Count} capybaras, "
                    + $"{_jellyfish.Count} jellyfish, {_antlions.Count} antlions, {_climbHandles.Count} handles, {_urchins.Count} urchins.");
            }
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
