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
    /// Backs the <c>Compass-Items</c> config section: an optional, purely
    /// additive chance for an opened Luggage to also contain a regular or
    /// Pirate's Compass.
    ///
    /// Three hard rules shape the whole implementation:
    ///
    /// <list type="bullet">
    /// <item><b>Host-only / host-authoritative.</b> Luggage contents are spawned
    /// by the host alone (<c>Luggage.OpenLuggageRPC</c>'s spawn coroutine is
    /// gated on <c>NetCode.Session.IsHost</c>, and <c>Spawner.SpawnItems</c>
    /// itself early-returns for anyone who isn't the master client), so this
    /// patch only ever does anything on the host and only ever reads the host's
    /// own config. No sync is needed - unlike Ghost-Free-Cam, a client's values
    /// simply never get consulted.</item>
    /// <item><b>Never override, only fill.</b> A roll only happens for a luggage
    /// that opened with a physical slot left unused (a regular 2-slot Luggage
    /// that produced a single item), and a hit spawns an extra item into that
    /// free slot - see <see cref="FindFreeSlot"/> for how "unused" is
    /// established, which is less obvious than it sounds. Nothing vanilla - or
    /// another mod running its own postfix on the same method - put in a slot is
    /// ever removed or replaced. That also excludes Explorer's Luggage for free:
    /// both of its slots are always filled, so it never has a free one to begin
    /// with.</item>
    /// <item><b>Never disturb the game's own odds.</b> The loot roll itself is
    /// left completely untouched (no patch on <c>GetObjectsToSpawn</c>/
    /// <c>LootData</c>), and our own coin flips use a private
    /// <see cref="System.Random"/> rather than <c>UnityEngine.Random</c>, so
    /// they can't even perturb the shared RNG stream vanilla's later rolls draw
    /// from.</item>
    /// </list>
    ///
    /// Hooked as a postfix on <c>Spawner.SpawnItems</c> (the one method every
    /// luggage's spawn path funnels through - <c>Luggage</c> doesn't override
    /// it), filtered down to <c>Luggage</c> instances that aren't a
    /// <c>RespawnChest</c> (which is a <c>Luggage</c> subclass, but is the
    /// revive chest, not lootable luggage - it calls <c>base.SpawnItems</c>, so
    /// it would otherwise land here too).
    ///
    /// The luggage's re-open-from-history path (<c>SpawnedItemTracker.
    /// SpawnAndTrackFromItemHistory</c>) never calls <c>SpawnItems</c> at all,
    /// so a luggage restored from a saved run doesn't re-roll - it just respawns
    /// whatever it already contained, including a compass we added the first
    /// time round (which is why a spawned compass is appended to the returned
    /// list: that list is exactly what the caller hands to the tracker).
    /// </summary>
    public static class LuggageCompassSpawner
    {
        /// <summary>
        /// Our own RNG stream, deliberately not <c>UnityEngine.Random</c> - see
        /// the "never disturb the game's own odds" rule in the class doc.
        /// </summary>
        private static readonly System.Random _rng = new System.Random();

        /// <summary><c>Spawner.InitializePhysics</c> is protected, so the post-spawn
        /// physics handoff vanilla does for every luggage item is invoked reflectively
        /// rather than reimplemented (it also calls the internal <c>Item.ForceSyncForFrames</c>).</summary>
        private static MethodInfo _initializePhysics;

        public static void Apply(Harmony harmony, ManualLogSource log)
        {
            try
            {
                _initializePhysics = AccessTools.Method(typeof(Spawner), "InitializePhysics");
                var spawnItems = AccessTools.Method(typeof(Spawner), nameof(Spawner.SpawnItems));
                harmony.Patch(spawnItems, postfix: new HarmonyMethod(typeof(LuggageCompassSpawner), nameof(SpawnItemsPostfix)));

                log.LogInfo("LuggageCompassSpawner: patched Spawner.SpawnItems.");
            }
            catch (Exception e)
            {
                log.LogError($"LuggageCompassSpawner.Apply failed (non-fatal, the Compass-Items settings won't work): {e}");
            }
        }

        private static void SpawnItemsPostfix(Spawner __instance, List<Transform> spawnSpots, List<PhotonView> __result)
        {
            try
            {
                TryAddCompass(__instance, spawnSpots, __result);
            }
            catch (Exception e)
            {
                // Never let this bubble into the game's own spawn routine - a
                // throw here would abort the luggage's spawn coroutine.
                Plugin.Instance?.Log.LogError($"LuggageCompassSpawner: adding a compass to opened luggage failed (non-fatal): {e}");
            }
        }

        private static void TryAddCompass(Spawner spawner, List<Transform> spawnSpots, List<PhotonView> spawned)
        {
            if (!(spawner is Luggage luggage) || spawner is RespawnChest)
            {
                return;
            }

            PluginConfig cfg = Plugin.Instance?.Cfg;
            if (cfg == null)
            {
                return;
            }

            bool pirateEnabled = cfg.EnablePirateCompassFromLuggage.Value;
            bool normalEnabled = cfg.EnableCompassFromLuggage.Value;
            if (!pirateEnabled && !normalEnabled)
            {
                return;
            }

            // Belt-and-suspenders host gate: SpawnItems already early-returns
            // for a non-master client, but that returns an *empty* list while
            // spawnSpots is still full - which would read to the free-spot scan
            // below as "every slot is empty" if it ever ran on a client.
            if (!PhotonNetwork.IsMasterClient)
            {
                return;
            }

            if (spawnSpots == null || spawned == null)
            {
                return;
            }

            Transform freeSlot = FindFreeSlot(luggage, spawnSpots, spawned);
            DumpLuggage(luggage, spawnSpots, spawned, freeSlot);
            if (freeSlot == null)
            {
                return;
            }

            bool pirate;
            if (pirateEnabled && Roll(cfg.PirateCompassFromLuggageChancePercent.Value))
            {
                pirate = true;
            }
            else if (normalEnabled && Roll(cfg.CompassFromLuggageChancePercent.Value))
            {
                pirate = false;
            }
            else
            {
                return;
            }

            Item prefab = CompassItemAssets.GetPrefab(pirate);
            if (prefab == null)
            {
                Plugin.Instance?.Log.LogWarning(
                    $"LuggageCompassSpawner: no {(pirate ? "Pirate's" : "regular")} Compass item found in ItemDatabase - nothing spawned.");
                return;
            }

            // Respects a custom run that disabled this item entirely (the same
            // check vanilla's own loot table applies before offering an item).
            if (!prefab.IsValidToSpawn())
            {
                DebugLog($"{prefab.name} isn't valid to spawn in this run's settings - skipping.");
                return;
            }

            Spawn(luggage, freeSlot, prefab, spawned);
        }

        /// <summary>
        /// Minimum distance (world units) a candidate slot has to keep from
        /// every slot that already received an item to still count as free -
        /// slots taken from a *different* weighted entry (see
        /// <see cref="FindFreeSlot"/>) can sit right on top of a used one.
        /// </summary>
        private const float SlotSeparation = 0.2f;

        /// <summary>
        /// The spawn spot an extra compass can go into without touching
        /// anything the game already placed, or null if this luggage opened
        /// completely full.
        ///
        /// Two sources, in priority order:
        ///
        /// <list type="number">
        /// <item>A spot in the list this open actually used that received no
        /// item. Vanilla's loot table (<c>LootData.GetRandomItems</c>) fills
        /// every requested slot, so in practice this only ever happens in the
        /// edge cases where it comes up short - but it's the cleanest answer
        /// when it does.</item>
        /// <item>A spot from one of the luggage's <em>other</em> weighted spot
        /// lists that this open didn't roll. This is the real "a slot is
        /// empty" case: <c>Spawner.spawnPointMode</c>'s weighted lists are how
        /// a luggage varies its item <em>count</em> (note the field's own
        /// <c>[FormerlySerializedAs("spawnCountMode")]</c>), so a 2-slot
        /// suitcase that opened with one item did so by rolling a 1-spot list,
        /// leaving the second physical slot unused rather than "null" anywhere
        /// in the data. Skipped if it sits within <see cref="SlotSeparation"/>
        /// of a slot that did get an item, so this can never stack a compass on
        /// top of existing loot.</item>
        /// </list>
        ///
        /// Which used spots got an item is derived by matching each spawned
        /// item back to its nearest spot rather than by reading vanilla's
        /// internal loot list: that stays correct no matter what filled them
        /// (vanilla, or another mod's own postfix that added to
        /// <paramref name="spawned"/> before ours ran).
        /// </summary>
        private static Transform FindFreeSlot(Luggage luggage, List<Transform> usedSpots, List<PhotonView> spawned)
        {
            bool[] occupied = MatchSpawnedToSpots(usedSpots, spawned);

            for (int i = 0; i < usedSpots.Count; i++)
            {
                if (!occupied[i] && usedSpots[i] != null)
                {
                    return usedSpots[i];
                }
            }

            foreach (Transform candidate in AllSpawnSpots(luggage))
            {
                if (candidate == null || usedSpots.Contains(candidate))
                {
                    continue;
                }

                bool tooClose = false;
                for (int i = 0; i < usedSpots.Count; i++)
                {
                    if (occupied[i] && usedSpots[i] != null &&
                        (usedSpots[i].position - candidate.position).sqrMagnitude < SlotSeparation * SlotSeparation)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (!tooClose)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>Which of <paramref name="spots"/> a spawned item landed on, by nearest-spot matching.</summary>
        private static bool[] MatchSpawnedToSpots(List<Transform> spots, List<PhotonView> spawned)
        {
            var occupied = new bool[spots.Count];

            foreach (PhotonView view in spawned)
            {
                if (view == null)
                {
                    continue;
                }

                Vector3 position = view.transform.position;
                int best = -1;
                float bestDistanceSq = float.MaxValue;

                for (int i = 0; i < spots.Count; i++)
                {
                    if (occupied[i] || spots[i] == null)
                    {
                        continue;
                    }

                    float distanceSq = (spots[i].position - position).sqrMagnitude;
                    if (distanceSq < bestDistanceSq)
                    {
                        bestDistanceSq = distanceSq;
                        best = i;
                    }
                }

                if (best >= 0)
                {
                    occupied[best] = true;
                }
            }

            return occupied;
        }

        /// <summary>
        /// Every spawn spot this luggage knows about across both spot modes -
        /// its plain <c>spawnSpots</c> list plus every weighted entry's own
        /// list - regardless of which one this particular open rolled.
        /// </summary>
        private static IEnumerable<Transform> AllSpawnSpots(Luggage luggage)
        {
            if (luggage.spawnSpots != null)
            {
                foreach (Transform spot in luggage.spawnSpots)
                {
                    yield return spot;
                }
            }

            if (luggage.weightedSpawnSpots == null)
            {
                yield break;
            }

            foreach (Spawner.WeightedSpawnPointEntry entry in luggage.weightedSpawnSpots)
            {
                if (entry?.spawnSpots == null)
                {
                    continue;
                }

                foreach (Transform spot in entry.spawnSpots)
                {
                    yield return spot;
                }
            }
        }

        /// <summary>
        /// Debug-logging-only dump of one opened luggage's spot layout - what
        /// the prefab offers, what this open rolled, what actually spawned, and
        /// which slot (if any) <see cref="FindFreeSlot"/> settled on. This
        /// mechanic depends entirely on data that only exists in the game's
        /// serialized prefabs (invisible in any decompile), so this is how a
        /// "my luggage had a free slot and got nothing" report gets diagnosed.
        /// </summary>
        private static void DumpLuggage(Luggage luggage, List<Transform> usedSpots, List<PhotonView> spawned, Transform freeSlot)
        {
            if (Plugin.Instance == null || !Plugin.Instance.Cfg.EnableDebugLogging.Value)
            {
                return;
            }

            var weighted = new List<string>();
            if (luggage.weightedSpawnSpots != null)
            {
                foreach (Spawner.WeightedSpawnPointEntry entry in luggage.weightedSpawnSpots)
                {
                    weighted.Add($"[{entry?.spawnSpots?.Count ?? 0} spots, weight {entry?.weight ?? 0}]");
                }
            }

            DebugLog(
                $"opened '{luggage.name}': spot mode {luggage.spawnPointMode}, " +
                $"spawnSpots={luggage.spawnSpots?.Count ?? 0}, " +
                $"weightedSpawnSpots={{{string.Join(", ", weighted.ToArray())}}}, " +
                $"this open used {usedSpots.Count} spot(s) and spawned {spawned.Count} item(s) -> " +
                $"free slot: {(freeSlot != null ? freeSlot.name : "<none>")}");
        }

        private static bool Roll(float chancePercent) => _rng.NextDouble() * 100.0 < chancePercent;

        /// <summary>
        /// Spawns <paramref name="prefab"/> at <paramref name="spot"/> the same
        /// way <c>Spawner.SpawnItems</c> spawns a luggage's own items - same
        /// networked instantiate, same up-target/visual-centering handling, same
        /// <c>Luggage.OffsetSpawn</c> per-item offset, same physics handoff - so
        /// the added compass is indistinguishable from one the game itself
        /// rolled, and appends it to <paramref name="spawned"/> so the caller's
        /// spawn tracking (and therefore a later re-open from history) includes it.
        /// </summary>
        private static void Spawn(Luggage luggage, Transform spot, Item prefab, List<PhotonView> spawned)
        {
            GameObject spawnedObject = PhotonNetwork.InstantiateItemRoom(prefab.name, spot.position, spot.rotation);
            if (spawnedObject == null)
            {
                Plugin.Instance?.Log.LogWarning($"LuggageCompassSpawner: InstantiateItemRoom returned null for {prefab.name}.");
                return;
            }

            var item = spawnedObject.GetComponent<Item>();
            if (item == null)
            {
                return;
            }

            if (luggage.spawnUpTowardsTarget != null)
            {
                item.transform.up = (luggage.spawnUpTowardsTarget.position - item.transform.position).normalized;
            }

            if (luggage.centerItemsVisually)
            {
                item.transform.position += spot.position - item.Center();
            }

            // Luggage.OffsetSpawn's own body (protected virtual, and its
            // RespawnChest exception doesn't apply - those are filtered out above).
            if (item.offsetLuggageSpawn)
            {
                item.transform.position += luggage.transform.rotation * item.offsetLuggagePosition;
                item.transform.rotation *= Quaternion.Euler(item.offsetLuggageRotation);
            }
            else
            {
                CompassItemAssets.FaceUp(item);
            }

            _initializePhysics?.Invoke(luggage, new object[] { item });

            var view = item.GetComponent<PhotonView>();
            if (view != null)
            {
                spawned.Add(view);
            }

            DebugLog($"added {prefab.name} to opened luggage '{luggage.name}'.");
        }

        private static void DebugLog(string message) => CompassItemAssets.Log(message);
    }
}
