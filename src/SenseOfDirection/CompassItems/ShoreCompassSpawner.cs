using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace SenseOfDirection.CompassItems
{
    /// <summary>
    /// Backs <c>Compass-Items/shore-compass-count</c> and
    /// <c>shore-pirate-compass-count</c>: how many regular / Pirate's Compasses
    /// lie on the shore when a run starts.
    ///
    /// <b>Where vanilla's shore compass comes from.</b> The host's
    /// <c>MapHandler.Update</c> fires every segment-0 and global
    /// <c>ISpawner</c> exactly once, the first frame after the start routine
    /// finishes (guarded by its private <c>hasSpawnedInitialSpawners</c>).
    /// The starting compass is one of those spawns, but the scene object that
    /// places it isn't visible in decompiled code, so rather than guess which
    /// spawner it is, this wraps that one frame: a prefix snapshots every
    /// compass already in the scene, and a postfix, once the flag has flipped,
    /// diffs against it - whatever regular Compass is new and lying on the
    /// ground is vanilla's shore compass.
    ///
    /// That compass is then the anchor: extras are laid in a small ring around
    /// it, and a count below vanilla's removes the surplus. If vanilla placed
    /// no regular compass at all this run, the regular count is left alone
    /// (whatever suppressed it, the game meant it), and any Pirate's Compasses
    /// fall back to the players' spawn point instead.
    ///
    /// Spawned views are added to <c>MapHandler</c>'s own
    /// <c>viewsToDestoryIfNotAlreadyWhenSwitchingSegments</c>, the same list
    /// vanilla's shore compass lands in, so leftovers nobody picked up get
    /// cleaned up on the same terms as vanilla's.
    ///
    /// Host-only for the same reason as the other spawners in this folder: the
    /// whole block only runs on the master client in the first place.
    /// </summary>
    public static class ShoreCompassSpawner
    {
        /// <summary>Distance from the anchor compass to each extra one.</summary>
        private const float RingRadius = 0.6f;

        private const float GroundProbeUp = 1.5f;
        private const float GroundProbeDistance = 4f;

        private static AccessTools.FieldRef<MapHandler, bool> _hasSpawnedInitialSpawners;
        private static AccessTools.FieldRef<MapHandler, List<PhotonView>> _viewsToDestroy;
        private static MethodInfo _forceSyncForFrames;

        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                _hasSpawnedInitialSpawners = AccessTools.FieldRefAccess<MapHandler, bool>("hasSpawnedInitialSpawners");
                _viewsToDestroy = AccessTools.FieldRefAccess<MapHandler, List<PhotonView>>("viewsToDestoryIfNotAlreadyWhenSwitchingSegments");
                _forceSyncForFrames = AccessTools.Method(typeof(Item), "ForceSyncForFrames");

                var update = AccessTools.Method(typeof(MapHandler), "Update");
                harmony.Patch(update,
                    prefix: new HarmonyMethod(typeof(ShoreCompassSpawner), nameof(UpdatePrefix)),
                    postfix: new HarmonyMethod(typeof(ShoreCompassSpawner), nameof(UpdatePostfix)));

                log.LogInfo("ShoreCompassSpawner: patched MapHandler.Update.");
            }
            catch (Exception e)
            {
                log.LogError($"ShoreCompassSpawner.Apply failed (non-fatal, the shore compass count settings won't work): {e}");
            }
        }

        /// <summary>
        /// Null on every frame except the one the initial spawners are about to
        /// fire on - then it holds the compasses that existed beforehand. The
        /// per-frame cost otherwise is two bool reads.
        /// </summary>
        private static void UpdatePrefix(MapHandler __instance, out HashSet<Item> __state)
        {
            __state = null;
            try
            {
                if (!PhotonNetwork.IsMasterClient || _hasSpawnedInitialSpawners(__instance))
                {
                    return;
                }

                __state = new HashSet<Item>();
                foreach (Item item in FindCompasses(CompassPointer.CompassType.Normal))
                {
                    __state.Add(item);
                }
            }
            catch (Exception e)
            {
                __state = null;
                Plugin.Instance?.Log.LogError($"ShoreCompassSpawner: pre-spawn snapshot failed (non-fatal): {e}");
            }
        }

        private static void UpdatePostfix(MapHandler __instance, HashSet<Item> __state)
        {
            if (__state == null)
            {
                return;
            }

            try
            {
                // Still waiting on the start routine - nothing spawned this frame.
                if (!_hasSpawnedInitialSpawners(__instance) || !PhotonNetwork.IsMasterClient)
                {
                    return;
                }

                Adjust(__instance, __state);
            }
            catch (Exception e)
            {
                Plugin.Instance?.Log.LogError($"ShoreCompassSpawner: adjusting the shore compasses failed (non-fatal): {e}");
            }
        }

        private static void Adjust(MapHandler mapHandler, HashSet<Item> before)
        {
            PluginConfig cfg = Plugin.Instance?.Cfg;
            if (cfg == null)
            {
                return;
            }

            int wantedNormal = Mathf.Max(0, cfg.ShoreCompassCount.Value);
            int wantedPirate = Mathf.Max(0, cfg.ShorePirateCompassCount.Value);

            var vanilla = new List<Item>();
            foreach (Item item in FindCompasses(CompassPointer.CompassType.Normal))
            {
                if (!before.Contains(item) && item.itemState == ItemState.Ground)
                {
                    vanilla.Add(item);
                }
            }

            CompassItemAssets.Log($"shore: vanilla placed {vanilla.Count} compass(es); wanted {wantedNormal} regular, {wantedPirate} pirate.");

            Vector3 anchor;
            if (vanilla.Count > 0)
            {
                anchor = vanilla[0].transform.position;
            }
            else
            {
                // Nothing to anchor to and nothing to adjust - only the Pirate's
                // Compasses (if any) still need a place.
                if (wantedPirate == 0 || !TryGetSpawnPointAnchor(out anchor))
                {
                    return;
                }
                wantedNormal = 0;
            }

            // Fewer than vanilla: remove the surplus.
            for (int i = vanilla.Count - 1; i >= wantedNormal && i >= 0; i--)
            {
                var view = vanilla[i].GetComponent<PhotonView>();
                if (view != null)
                {
                    PhotonNetwork.Destroy(view);
                }
            }

            int extraNormal = vanilla.Count > 0 ? Mathf.Max(0, wantedNormal - vanilla.Count) : 0;
            int total = extraNormal + wantedPirate;
            if (total == 0)
            {
                return;
            }

            List<PhotonView> cleanupList = _viewsToDestroy(mapHandler);
            int slot = 0;

            if (extraNormal > 0)
            {
                Item prefab = CompassItemAssets.GetPrefab(pirate: false);
                for (int i = 0; i < extraNormal; i++)
                {
                    Spawn(prefab, RingPosition(anchor, slot++, total), cleanupList);
                }
            }

            if (wantedPirate > 0)
            {
                Item prefab = CompassItemAssets.GetPrefab(pirate: true);
                for (int i = 0; i < wantedPirate; i++)
                {
                    Spawn(prefab, RingPosition(anchor, slot++, total), cleanupList);
                }
            }
        }

        private static IEnumerable<Item> FindCompasses(CompassPointer.CompassType type)
        {
            foreach (CompassPointer pointer in UnityEngine.Object.FindObjectsByType<CompassPointer>(FindObjectsSortMode.None))
            {
                if (pointer == null || pointer.compassType != type)
                {
                    continue;
                }

                Item item = pointer.GetComponentInParent<Item>();
                if (item != null)
                {
                    yield return item;
                }
            }
        }

        private static bool TryGetSpawnPointAnchor(out Vector3 anchor)
        {
            anchor = default;
            if (SpawnPoint.allSpawnPoints == null)
            {
                return false;
            }

            foreach (SpawnPoint point in SpawnPoint.allSpawnPoints)
            {
                if (point != null)
                {
                    anchor = point.transform.position;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Evenly spread around <paramref name="anchor"/> on the horizontal
        /// plane, then dropped onto whatever is underneath - the same ground
        /// probe <see cref="CampfireCompassSpawner"/> uses, so nothing ends up
        /// floating or buried on a sloped beach.
        /// </summary>
        private static Vector3 RingPosition(Vector3 anchor, int index, int count)
        {
            float angle = (index + 0.5f) * (360f / Mathf.Max(1, count)) * Mathf.Deg2Rad;
            Vector3 spot = anchor + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * RingRadius;

            if (Physics.Raycast(spot + Vector3.up * GroundProbeUp, Vector3.down, out RaycastHit hit,
                    GroundProbeDistance, HelperFunctions.AllPhysicalExceptCharacter, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * 0.05f;
            }

            return spot;
        }

        private static void Spawn(Item prefab, Vector3 position, List<PhotonView> cleanupList)
        {
            if (prefab == null)
            {
                Plugin.Instance?.Log.LogWarning("ShoreCompassSpawner: compass item not found in ItemDatabase - nothing spawned.");
                return;
            }

            if (!prefab.IsValidToSpawn())
            {
                CompassItemAssets.Log($"{prefab.name} isn't valid to spawn in this run's settings - skipping.");
                return;
            }

            GameObject spawnedObject = PhotonNetwork.InstantiateItemRoom(prefab.name, position, Quaternion.identity);
            var item = spawnedObject != null ? spawnedObject.GetComponent<Item>() : null;
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
                cleanupList?.Add(view);
            }

            CompassItemAssets.Log($"shore: spawned {prefab.name} at {position}.");
        }
    }
}
