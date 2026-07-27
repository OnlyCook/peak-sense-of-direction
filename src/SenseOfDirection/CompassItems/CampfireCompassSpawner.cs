using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace SenseOfDirection.CompassItems
{
    /// <summary>
    /// Backs the two <c>Compass-Items</c> campfire settings: an extra regular
    /// Compass laid on the ground next to each campfire area's backpack.
    ///
    /// Vanilla only ever hands out one compass per run (at the very start),
    /// which is fine solo but leaves everyone else in a co-op run without one -
    /// hence a per-campfire top-up, dropped right beside the backpack that's
    /// already the "grab your gear here" spot of every campfire area.
    ///
    /// <b>Polls rather than hooks.</b> The first version patched
    /// <c>Campfire.Light_Rpc</c> and spawned when a fire was lit; that was the
    /// wrong moment entirely (maintainer feedback) - the compass has to be
    /// waiting at a campfire when you *arrive*, not appear once you leave. But
    /// there's no single "a campfire loaded" event either: the first campfire
    /// already exists when the run starts, and later ones appear as their biome
    /// streams in. So this walks the scene on a slow timer instead and tops up
    /// any campfire it hasn't already handled, which covers both cases with one
    /// rule - the exact same re-resolve-every-frame reasoning
    /// <c>CampfireIndicator/CampfireIndicatorController.cs</c> already uses for
    /// <c>MapHandler.CurrentCampfire</c>, just throttled since this one has to
    /// sweep the scene rather than read a property.
    ///
    /// A campfire is only ever handled once it actually gets its compass, so a
    /// biome whose backpack loads a moment after its campfire simply gets
    /// picked up by a later sweep. The Kiln's final campfire falls out of the
    /// same rule for free: no backpack there means no spawn, ever.
    ///
    /// Host-only and host-authoritative for the same structural reason
    /// <see cref="LuggageCompassSpawner"/> is: item spawning is the master
    /// client's job, so this no-ops for everyone else and only the host's own
    /// config is ever read. No sync needed.
    /// </summary>
    public class CampfireCompassSpawner : MonoBehaviour
    {
        /// <summary>How far from a campfire its backpack can be. Maintainer-measured: every campfire in the game has one well within 15m.</summary>
        private const float BackpackSearchRadius = 15f;

        /// <summary>How far to the backpack's side the compass is laid down.</summary>
        private const float BackpackSideOffset = 1f;

        /// <summary>Seconds between scene sweeps - a campfire appearing a second or two before its compass does is imperceptible, and this keeps the two scene-wide queries off the per-frame path.</summary>
        private const float SweepIntervalSeconds = 2f;

        /// <summary>How far above/below the intended spot to look for the ground the compass rests on.</summary>
        private const float GroundProbeUp = 2f;
        private const float GroundProbeDistance = 8f;

        private static CampfireCompassSpawner _instance;

        public static CampfireCompassSpawner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.CampfireCompassSpawner");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CampfireCompassSpawner>();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Campfires already topped up, so repeated sweeps (and a fire being
        /// lit, re-lit, or re-broadcast to a joining player) can't stack
        /// compasses. Entries from an unloaded scene can never match a freshly
        /// created campfire object, so this needs no explicit reset.
        /// </summary>
        private readonly HashSet<Campfire> _handled = new HashSet<Campfire>();

        /// <summary><c>Item.ForceSyncForFrames</c> is internal - invoked reflectively so a spawned compass gets the same physics handoff <c>Spawner.InitializePhysics</c> gives every spawner-placed item.</summary>
        private static MethodInfo _forceSyncForFrames;

        private float _nextSweepTime;

        private void Awake()
        {
            _forceSyncForFrames = AccessTools.Method(typeof(Item), "ForceSyncForFrames");
        }

        private void Update()
        {
            if (Time.time < _nextSweepTime)
            {
                return;
            }
            _nextSweepTime = Time.time + SweepIntervalSeconds;

            try
            {
                Sweep();
            }
            catch (Exception e)
            {
                Plugin.Instance?.Log.LogError($"CampfireCompassSpawner: campfire sweep failed (non-fatal): {e}");
            }
        }

        private void Sweep()
        {
            PluginConfig cfg = Plugin.Instance?.Cfg;
            if (cfg == null || !cfg.EnableCompassAtCampfires.Value)
            {
                return;
            }

            // Item spawning is the master client's job, and there's nothing to
            // do outside an actual run.
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom || !MapHandler.ExistsAndInitialized)
            {
                return;
            }

            if (!IsCompassNeeded(cfg))
            {
                return;
            }

            Campfire[] campfires = UnityEngine.Object.FindObjectsByType<Campfire>(FindObjectsSortMode.None);
            if (campfires.Length == 0)
            {
                return;
            }

            Backpack[] backpacks = null;

            foreach (Campfire campfire in campfires)
            {
                if (campfire == null || _handled.Contains(campfire))
                {
                    continue;
                }

                // Deferred until a campfire actually needs it, so a sweep that
                // finds nothing new costs one query instead of two.
                backpacks = backpacks ?? UnityEngine.Object.FindObjectsByType<Backpack>(FindObjectsSortMode.None);

                Backpack backpack = FindCampfireBackpack(backpacks, campfire.transform.position);
                if (backpack == null)
                {
                    // Either its biome hasn't finished loading (a later sweep
                    // gets it) or it's the Kiln's final campfire, which has no
                    // backpack at all - and so never gets a compass.
                    continue;
                }

                Item prefab = CompassItemAssets.GetPrefab(pirate: false);
                if (prefab == null)
                {
                    Plugin.Instance?.Log.LogWarning("CampfireCompassSpawner: no regular Compass item found in ItemDatabase - nothing spawned.");
                    return;
                }

                if (!prefab.IsValidToSpawn())
                {
                    CompassItemAssets.Log($"{prefab.name} isn't valid to spawn in this run's settings - skipping.");
                    return;
                }

                _handled.Add(campfire);
                Spawn(prefab, ResolveSpawnPosition(backpack), campfire, backpack);
            }
        }

        /// <summary>
        /// Whether the run actually needs the top-up, per
        /// <c>campfire-compass-only-when-needed</c> (on by default): solo runs
        /// don't (vanilla's single starting compass is already yours), and
        /// neither does a run whose host has the compass tape on
        /// <see cref="Compass.CompassDisplayMode.AlwaysOn"/>, since then nobody
        /// has to hold a compass item to get their bearings in the first place.
        /// Turning the setting off spawns one at every campfire unconditionally.
        ///
        /// Read off the host's own config by construction - this only ever runs
        /// on the master client.
        /// </summary>
        private static bool IsCompassNeeded(PluginConfig cfg)
        {
            if (!cfg.CampfireCompassOnlyWhenNeeded.Value)
            {
                return true;
            }

            if (PhotonNetwork.OfflineMode || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom.PlayerCount <= 1)
            {
                return false;
            }

            return cfg.CompassDisplayMode.Value != Compass.CompassDisplayMode.AlwaysOn;
        }

        /// <summary>
        /// Where the compass goes: a step to the backpack's own right (flattened
        /// onto the horizontal plane, so the offset stays level no matter how
        /// the backpack itself came to rest), then dropped straight down onto
        /// whatever it lands on. The ground probe is what makes the result
        /// actually lie *on* the ground rather than at the backpack's own pivot
        /// height, which a maintainer-pinged reference spot showed sitting
        /// noticeably lower than the backpack's own body.
        /// </summary>
        private static Vector3 ResolveSpawnPosition(Backpack backpack)
        {
            Vector3 side = Vector3.ProjectOnPlane(backpack.transform.right, Vector3.up);
            if (side.sqrMagnitude < 0.0001f)
            {
                // Backpack is lying with its right axis straight up/down - any
                // level direction will do, so borrow its forward instead.
                side = Vector3.ProjectOnPlane(backpack.transform.forward, Vector3.up);
            }

            if (side.sqrMagnitude < 0.0001f)
            {
                side = Vector3.right;
            }

            Vector3 spot = backpack.transform.position + side.normalized * BackpackSideOffset;

            if (Physics.Raycast(spot + Vector3.up * GroundProbeUp, Vector3.down, out RaycastHit hit,
                    GroundProbeDistance, HelperFunctions.AllPhysicalExceptCharacter, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * 0.05f;
            }

            return spot;
        }

        /// <summary>
        /// The nearest free-standing <c>Backpack</c> to the campfire. Anything
        /// parented under a player is skipped: a worn backpack's own
        /// <c>itemState</c> never leaves <c>Ground</c> while equipped (that
        /// field only drives which mesh is shown), so a player standing at the
        /// fire would otherwise be picked as "the campfire's backpack" - the
        /// same false positive <c>peak-checkpoint-save</c>'s own
        /// <c>CampfireAreaHelpers.IsFreeWorldItem</c> guards against.
        /// </summary>
        private static Backpack FindCampfireBackpack(Backpack[] backpacks, Vector3 campfirePosition)
        {
            Backpack nearest = null;
            float bestDistanceSq = BackpackSearchRadius * BackpackSearchRadius;

            foreach (Backpack backpack in backpacks)
            {
                if (backpack == null ||
                    backpack.GetComponentInParent<Character>(true) != null ||
                    backpack.GetComponentInParent<Player>(true) != null)
                {
                    continue;
                }

                float distanceSq = (backpack.transform.position - campfirePosition).sqrMagnitude;
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    nearest = backpack;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Spawns the compass face-up at <paramref name="position"/> and hands
        /// it off to physics exactly the way <c>Spawner.InitializePhysics</c>
        /// does for every spawner-placed item (force-sync, then the buffered
        /// <c>SetKinematicRPC</c>), so it rests where it's put instead of
        /// rolling off down the mountain.
        /// </summary>
        private static void Spawn(Item prefab, Vector3 position, Campfire campfire, Backpack backpack)
        {
            GameObject spawnedObject = PhotonNetwork.InstantiateItemRoom(prefab.name, position, Quaternion.identity);
            if (spawnedObject == null)
            {
                Plugin.Instance?.Log.LogWarning($"CampfireCompassSpawner: InstantiateItemRoom returned null for {prefab.name}.");
                return;
            }

            var item = spawnedObject.GetComponent<Item>();
            if (item == null)
            {
                return;
            }

            CompassItemAssets.FaceUp(item);

            _forceSyncForFrames?.Invoke(item, new object[] { 10 });

            var view = item.GetComponent<PhotonView>();
            if (view != null)
            {
                view.RPC("SetKinematicRPC", RpcTarget.AllBuffered, true, item.transform.position, item.transform.rotation);
            }

            CompassItemAssets.Log(
                $"spawned {prefab.name} at {position} for campfire '{campfire.name}' {Vector3.Distance(campfire.transform.position, position):0.0}m away " +
                $"(backpack '{backpack.name}' at {backpack.transform.position}).");
        }
    }
}
