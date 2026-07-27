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
            // RespawnChest is the revive chest, not lootable luggage.
            // LuggageCursed is the Ancient Luggage: it holds a single mystic
            // item, and stuffing a compass in alongside it would both break that
            // "one cursed item" premise and hand out a free reward for taking
            // the curse. Both are Luggage subclasses, so both need filtering out
            // explicitly.
            if (!(spawner is Luggage luggage) || spawner is RespawnChest || spawner is LuggageCursed)
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

            // Both config values are absolute "per luggage opened" percentages,
            // but we only get here on the fraction of opens that left a free
            // slot - so each is divided back out by that fraction to recover the
            // conditional chance to actually roll with. See FreeSlotProbability.
            float freeSlotChance = FreeSlotProbability(luggage);
            if (freeSlotChance <= 0f)
            {
                return;
            }

            float pirateChance = pirateEnabled
                ? Mathf.Clamp01(cfg.PirateCompassFromLuggageChancePercent.Value / 100f / freeSlotChance)
                : 0f;

            // The regular compass is only rolled after the Pirate's roll missed,
            // so its own conditional chance is divided by the odds of getting
            // that far - which keeps its absolute percentage exact rather than
            // silently shrinking as the Pirate's chance goes up. If the two
            // together ask for more than the free-slot rate can supply, Clamp01
            // caps it and the Pirate's compass keeps priority.
            float normalChance = normalEnabled && pirateChance < 1f
                ? Mathf.Clamp01(cfg.CompassFromLuggageChancePercent.Value / 100f / freeSlotChance / (1f - pirateChance))
                : 0f;

            bool pirate;
            if (pirateChance > 0f && Roll(pirateChance))
            {
                pirate = true;
            }
            else if (normalChance > 0f && Roll(normalChance))
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
        /// <item>A spot from the luggage's <em>capacity list</em> - the largest
        /// spot list this luggage could actually have rolled (see
        /// <see cref="FindCapacityList"/>) - that this open didn't use. This is
        /// the real "a slot is empty" case: <c>Spawner.spawnPointMode</c>'s
        /// weighted lists are how a luggage varies its item <em>count</em> (note
        /// the field's own <c>[FormerlySerializedAs("spawnCountMode")]</c>), so
        /// a 2-slot suitcase that opened with one item did so by rolling a
        /// 1-spot list, leaving the second physical slot unused rather than
        /// "null" anywhere in the data. Skipped if it sits within
        /// <see cref="SlotSeparation"/> of a slot that did get an item, so this
        /// can never stack a compass on top of existing loot.</item>
        /// </list>
        ///
        /// Restricting source 2 to the capacity list is what keeps a luggage's
        /// real capacity honest. Scanning <em>every</em> transform the prefab
        /// carries instead (its legacy <c>spawnSpots</c> field left populated
        /// while the spawner runs in <c>WeightedLists</c> mode, or an entry with
        /// <c>weight = 0</c> that <c>RandomSelection</c> can never pick) finds a
        /// "free" slot the game itself could never have filled, on every single
        /// luggage - which made the roll effectively unconditional.
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

            List<Transform> capacity = FindCapacityList(luggage);
            if (capacity == null)
            {
                return null;
            }

            foreach (Transform candidate in capacity)
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
        /// This luggage's capacity list: the largest spot list <em>this open
        /// could have rolled instead</em>, which is what defines how many items
        /// the luggage physically holds (2 for a regular Luggage, 3 for a big
        /// one) - or null if it has no free capacity to speak of.
        ///
        /// Mirrors <c>Spawner.GetSpawnSpots</c>'s own branch exactly, so only
        /// lists the game could genuinely have picked are considered:
        ///
        /// <list type="bullet">
        /// <item><c>SingleList</c> mode reads <c>spawnSpots</c> and nothing
        /// else, and that same list is what this open already used - there is no
        /// larger alternative, so there's never a free slot.</item>
        /// <item><c>WeightedLists</c> mode reads <em>only</em>
        /// <c>weightedSpawnSpots</c>; the <c>spawnSpots</c> field is dead data
        /// in this mode and is deliberately ignored. Entries with
        /// <c>weight &lt;= 0</c> are skipped too: <c>HelperFunctions.
        /// RandomSelection</c>'s weighted overload can never return one (its
        /// <c>random.Next(num + 0) &gt;= num</c> test is always false once any
        /// weight has accumulated), so their spots aren't real capacity.</item>
        /// </list>
        /// </summary>
        private static List<Transform> FindCapacityList(Luggage luggage)
        {
            if (luggage.spawnPointMode != Spawner.SpawnPointMode.WeightedLists || luggage.weightedSpawnSpots == null)
            {
                return null;
            }

            List<Transform> largest = null;
            foreach (Spawner.WeightedSpawnPointEntry entry in luggage.weightedSpawnSpots)
            {
                if (entry?.spawnSpots == null || entry.weight <= 0)
                {
                    continue;
                }

                if (largest == null || entry.spawnSpots.Count > largest.Count)
                {
                    largest = entry.spawnSpots;
                }
            }

            return largest;
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

            List<Transform> capacity = FindCapacityList(luggage);

            // Per-entry odds, straight out of HelperFunctions.RandomSelection's
            // weighted overload: P(entry) = weight / sum(weights). Printing them
            // is the whole point of this dump - a luggage type's free-slot rate
            // is exactly the share of its weight that sits on entries smaller
            // than the capacity list, so one open of each prefab pins the rate
            // down analytically instead of needing a large sample.
            int totalWeight = 0;
            if (luggage.weightedSpawnSpots != null)
            {
                foreach (Spawner.WeightedSpawnPointEntry entry in luggage.weightedSpawnSpots)
                {
                    if (entry != null && entry.weight > 0)
                    {
                        totalWeight += entry.weight;
                    }
                }
            }

            var weighted = new List<string>();
            if (luggage.weightedSpawnSpots != null)
            {
                for (int i = 0; i < luggage.weightedSpawnSpots.Count; i++)
                {
                    Spawner.WeightedSpawnPointEntry entry = luggage.weightedSpawnSpots[i];
                    int weight = entry?.weight ?? 0;
                    int spots = entry?.spawnSpots?.Count ?? 0;
                    string odds = totalWeight > 0 && weight > 0
                        ? $"{weight * 100f / totalWeight:0.##}%"
                        : "unreachable";
                    string chosen = entry?.spawnSpots == usedSpots ? " <-CHOSEN" : string.Empty;
                    weighted.Add($"#{i}: {spots} spots, weight {weight} ({odds}){chosen}");
                }
            }

            DebugLog(
                $"opened '{luggage.name}': spot mode {luggage.spawnPointMode}, " +
                $"legacy spawnSpots={luggage.spawnSpots?.Count ?? 0} (dead data in WeightedLists mode), " +
                $"capacity={capacity?.Count ?? 0}, " +
                $"weightedSpawnSpots={{{string.Join(" | ", weighted.ToArray())}}}, " +
                $"this open used {usedSpots.Count} spot(s) and spawned {spawned.Count} item(s) -> " +
                $"free slot: {(freeSlot != null ? freeSlot.name : "<none>")}");

            DumpTally(luggage, usedSpots, capacity, freeSlot);
        }

        /// <summary>
        /// Running per-prefab tally of opens vs. opens that left a free slot,
        /// logged after every open. The empirical counterpart to the per-entry
        /// odds above: it's what confirms the analytic rate is the rate actually
        /// observed in a run, and it's the number the config's chance-percent
        /// range is scaled against. Debug-logging only; reset per session (the
        /// BepInEx log is rewritten every launch anyway).
        /// </summary>
        private static readonly Dictionary<string, int[]> _tally = new Dictionary<string, int[]>();

        private static void DumpTally(Luggage luggage, List<Transform> usedSpots, List<Transform> capacity, Transform freeSlot)
        {
            // "(Clone)"-suffixed instance names all collapse onto their prefab.
            string prefab = luggage.name.Replace("(Clone)", string.Empty).Trim();

            if (!_tally.TryGetValue(prefab, out int[] counts))
            {
                counts = new int[2];
                _tally[prefab] = counts;
            }

            counts[0]++;
            if (freeSlot != null)
            {
                counts[1]++;
            }

            var lines = new List<string>();
            foreach (KeyValuePair<string, int[]> pair in _tally)
            {
                int opens = pair.Value[0];
                int free = pair.Value[1];
                lines.Add($"  {pair.Key}: {free}/{opens} opens left a free slot ({free * 100f / opens:0.#}%)");
            }

            DebugLog(
                $"free-slot tally so far (this open: {usedSpots.Count} of {capacity?.Count ?? 0} slots used)\n" +
                string.Join("\n", lines.ToArray()));
        }

        private static bool Roll(float chance) => _rng.NextDouble() < chance;

        /// <summary>
        /// The odds (0-1) that opening this luggage leaves a free slot at all -
        /// i.e. that it rolls one of the spot lists smaller than its capacity.
        ///
        /// Exact, not estimated: item count is purely <c>GetSpawnSpots().Count</c>
        /// (<c>Spawner.SpawnItems</c> fills every spot it's handed, and
        /// <c>LootData.GetRandomItems</c> always returns as many items as asked
        /// - it recycles its working pool rather than coming up short), and
        /// <c>HelperFunctions.RandomSelection</c>'s weighted overload picks
        /// entry <c>i</c> with probability <c>weight_i / sum(weights)</c>. So
        /// this is just the weight share sitting on the below-capacity entries.
        ///
        /// Both lootable luggage types come out at 2/3 as shipped - LuggageSmall
        /// holds 1-2 items (weights 100/50) and LuggageBig holds 2-3 (weights
        /// 66/33), each favouring the smaller roll 2:1 - but it's computed from
        /// the live weights per luggage rather than baked in as that constant,
        /// so a game patch or another mod's own luggage stays correct for free.
        /// Explorer's Luggage (LuggageEpic) has a single 100% full-capacity
        /// entry and so returns 0 here, which is what excludes it.
        /// </summary>
        private static float FreeSlotProbability(Luggage luggage)
        {
            List<Transform> capacity = FindCapacityList(luggage);
            if (capacity == null || luggage.weightedSpawnSpots == null)
            {
                return 0f;
            }

            int totalWeight = 0;
            int belowCapacityWeight = 0;

            foreach (Spawner.WeightedSpawnPointEntry entry in luggage.weightedSpawnSpots)
            {
                if (entry?.spawnSpots == null || entry.weight <= 0)
                {
                    continue;
                }

                totalWeight += entry.weight;
                if (entry.spawnSpots.Count < capacity.Count)
                {
                    belowCapacityWeight += entry.weight;
                }
            }

            return totalWeight > 0 ? (float)belowCapacityWeight / totalWeight : 0f;
        }

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
