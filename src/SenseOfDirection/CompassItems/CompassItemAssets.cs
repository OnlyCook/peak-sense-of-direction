using System.Collections.Generic;
using UnityEngine;
using Zorro.Core;

namespace SenseOfDirection.CompassItems
{
    /// <summary>
    /// Shared plumbing for every "spawn a compass item ourselves" mechanic in
    /// this folder (<see cref="LuggageCompassSpawner"/>,
    /// <see cref="CampfireCompassSpawner"/>): which prefab is which, and how to
    /// lay one down so its dial reads face-up.
    /// </summary>
    internal static class CompassItemAssets
    {
        private static Item _normalCompassPrefab;
        private static Item _pirateCompassPrefab;
        private static bool _resolved;

        /// <summary>
        /// The compass item prefab of the requested kind, found by scanning
        /// <c>ItemDatabase</c> for a <c>CompassPointer</c> of the matching
        /// <c>CompassType</c> - PEAK has no dedicated compass item class, so this
        /// is the same identification <c>Compass.CompassManager</c> already uses
        /// for held items. Resolved once and cached (the database is a static
        /// asset that outlives any scene); ties are broken by lowest item ID so
        /// the pick stays stable across runs. Null until the database is loaded,
        /// so callers simply retry on their next spawn opportunity.
        /// </summary>
        internal static Item GetPrefab(bool pirate)
        {
            if (!_resolved)
            {
                Resolve();
            }

            return pirate ? _pirateCompassPrefab : _normalCompassPrefab;
        }

        private static void Resolve()
        {
            ItemDatabase database = SingletonAsset<ItemDatabase>.Instance;
            if (database == null || database.itemLookup == null || database.itemLookup.Count == 0)
            {
                // Not loaded yet - stay unresolved so the next caller retries.
                return;
            }

            ushort normalId = ushort.MaxValue;
            ushort pirateId = ushort.MaxValue;

            foreach (KeyValuePair<ushort, Item> entry in database.itemLookup)
            {
                Item item = entry.Value;
                if (item == null)
                {
                    continue;
                }

                // includeInactive: a database entry is an uninstantiated prefab,
                // so nothing under it counts as active in a hierarchy.
                var pointer = item.GetComponentInChildren<CompassPointer>(true);
                if (pointer == null)
                {
                    continue;
                }

                if (pointer.compassType == CompassPointer.CompassType.Pirate)
                {
                    if (entry.Key < pirateId)
                    {
                        pirateId = entry.Key;
                        _pirateCompassPrefab = item;
                    }
                }
                else if (pointer.compassType == CompassPointer.CompassType.Normal)
                {
                    if (entry.Key < normalId)
                    {
                        normalId = entry.Key;
                        _normalCompassPrefab = item;
                    }
                }
            }

            _resolved = true;
            Log($"resolved compass prefabs: normal='{(_normalCompassPrefab != null ? _normalCompassPrefab.name : "<none>")}', " +
                $"pirate='{(_pirateCompassPrefab != null ? _pirateCompassPrefab.name : "<none>")}'.");
        }

        /// <summary>
        /// Rolls a freshly spawned compass so its face points straight up - i.e.
        /// so you're looking at the dial rather than at its back.
        ///
        /// Vanilla items authored to appear in luggage carry their own
        /// in-luggage placement (<c>Item.offsetLuggageSpawn</c> + the offsets
        /// <c>Luggage.OffsetSpawn</c> applies). A regular Compass isn't luggage
        /// loot in vanilla at all, so it has none of that and lands at whatever
        /// rotation its spawn point happened to have - which is what left it
        /// face-down in the first playtest.
        ///
        /// The dial's normal is the <c>CompassPointer</c> transform's own
        /// forward axis: <c>CompassPointer.UpdateHeading</c> both projects the
        /// heading onto the plane perpendicular to it
        /// (<c>Vector3.ProjectOnPlane(heading, transform.forward)</c>) and uses
        /// <c>transform.up</c> as the needle's roll reference, so forward is the
        /// axis the needle sweeps around - i.e. straight out of the face. So the
        /// correction is the minimal rotation taking that axis to world up
        /// (<c>Quaternion.FromToRotation</c> keeps the remaining spin as close
        /// to the original as possible, so the compass still sits naturally
        /// rather than snapping to a fixed yaw), followed by a half turn about
        /// world up - the dial's normal by then, so that step is exactly "spin
        /// it 180 degrees where it lies", which is what the second playtest
        /// asked for.
        /// </summary>
        internal static void FaceUp(Item item)
        {
            var pointer = item.GetComponentInChildren<CompassPointer>(true);
            if (pointer == null)
            {
                return;
            }

            Quaternion faceUp = Quaternion.FromToRotation(pointer.transform.forward, Vector3.up);
            item.transform.rotation = Quaternion.AngleAxis(180f, Vector3.up) * faceUp * item.transform.rotation;
        }

        /// <summary>Debug-logging-gated log line, shared by this folder's spawners.</summary>
        internal static void Log(string message)
        {
            if (Plugin.Instance != null && Plugin.Instance.Cfg.EnableDebugLogging.Value)
            {
                Plugin.Instance.Log.LogInfo($"CompassItems: {message}");
            }
        }
    }
}
