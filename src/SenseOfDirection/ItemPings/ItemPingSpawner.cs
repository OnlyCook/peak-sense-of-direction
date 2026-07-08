using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// Entry point called from <see cref="Pings.PointPingerPatches"/>'
    /// <c>ReceivePoint_Rpc</c> prefix once per accepted ping: converts the
    /// configured meter radii to world units, detects nearby items/luggage
    /// (<see cref="ItemPingDetector"/>), groups them by display name when
    /// <c>enable-item-ping-grouping</c> is on (deliberately simpler than the
    /// reference mod's iterative same-type cluster search - everything found
    /// here is already within one ping's detection radius of the same point,
    /// so a flat group-by-name is enough), and spawns one <see
    /// cref="ItemPingHighlight"/> per (group of) target(s) - or, if a
    /// detected target is already covered by a still-active (non-fading)
    /// highlight from an earlier ping, refreshes that highlight instead of
    /// stacking a second one on top of it.
    /// </summary>
    public static class ItemPingSpawner
    {
        /// <summary>
        /// Which highlight currently "owns" a given target GameObject, so a
        /// re-ping of the same item merges into it (resets its timer) rather
        /// than spawning an overlapping duplicate. Entries are removed as
        /// soon as their owning highlight starts fading out (see
        /// <see cref="ItemPingHighlight.OnFadeStart"/>), not only once it's
        /// finally destroyed - a re-ping during that brief fade window is
        /// free to start a fresh highlight instead of trying to revive a
        /// dying one.
        /// </summary>
        private static readonly Dictionary<GameObject, ItemPingHighlight> ActiveByTarget = new Dictionary<GameObject, ItemPingHighlight>();

        /// <summary>
        /// Small buffer added past the ping's landed point when computing how
        /// far along the aim ray to still count as "aimed at" - the item
        /// itself is normally between the pinging player and wherever the
        /// point actually landed (e.g. a coconut in front of the tree trunk
        /// the ping's own raycast hit), so this mostly just absorbs
        /// measurement noise in the head-to-point direction approximation,
        /// not meant to reach meaningfully further than the ping itself.
        /// </summary>
        private const float RayOvershootMeters = 2f;

        /// <returns>How many item/luggage targets were highlighted (new or merged), for the caller to decide whether to suppress its own generic ping distance label.</returns>
        public static int SpawnFor(Vector3 point, Color color, Character pingingCharacter)
        {
            PluginConfig cfg = Plugin.Instance.Cfg;

            float itemRadiusUnits = cfg.ItemPingDetectionRadiusMeters.Value / CharacterStats.unitsToMeters;
            float luggageRadiusUnits = cfg.LuggagePingDetectionRadiusMeters.Value / CharacterStats.unitsToMeters;

            Vector3 rayOrigin = pingingCharacter.Head;
            Vector3 toPoint = point - rayOrigin;
            float distanceToPoint = toPoint.magnitude;
            Vector3 rayDirection = distanceToPoint > 0.0001f ? toPoint / distanceToPoint : Vector3.zero;

            float rayHitboxRadiusUnits = 0f;
            float rayMaxDistanceUnits = 0f;
            if (cfg.EnableItemPingRayAssist.Value)
            {
                rayHitboxRadiusUnits = cfg.ItemPingRayAssistRadiusMeters.Value / CharacterStats.unitsToMeters;
                rayMaxDistanceUnits = distanceToPoint + RayOvershootMeters / CharacterStats.unitsToMeters;
            }

            List<PingableTarget> found = ItemPingDetector.FindNear(
                point, itemRadiusUnits, luggageRadiusUnits,
                rayOrigin, rayDirection, rayMaxDistanceUnits, rayHitboxRadiusUnits,
                cfg.EnableCreaturePings.Value);

            if (cfg.EnableDebugLogging.Value)
            {
                ItemPingDetector.LogNearbyUnmatched(point, itemRadiusUnits, Plugin.Instance.Log);
            }

            if (found.Count == 0)
            {
                return 0;
            }

            bool enableArrow = cfg.EnableItemPingOffScreenIndicator.Value;
            float duration = cfg.ItemPingDurationSeconds.Value;

            IEnumerable<List<PingableTarget>> clusters = cfg.EnableItemPingGrouping.Value
                ? found.GroupBy(t => t.GetDisplayName()).Select(g => g.ToList())
                : found.Select(t => new List<PingableTarget> { t });

            foreach (List<PingableTarget> cluster in clusters)
            {
                SpawnOrMerge(cluster, color, duration, enableArrow);
            }

            return found.Count;
        }

        private static void SpawnOrMerge(List<PingableTarget> cluster, Color color, float duration, bool enableArrow)
        {
            ItemPingHighlight existing = null;
            foreach (PingableTarget target in cluster)
            {
                if (ActiveByTarget.TryGetValue(target.GameObject, out ItemPingHighlight highlight))
                {
                    existing = highlight;
                    break;
                }
            }

            if (existing != null)
            {
                existing.Refresh(cluster, duration);
                foreach (PingableTarget target in cluster)
                {
                    ActiveByTarget[target.GameObject] = existing;
                }
                return;
            }

            ItemPingHighlight created = ItemPingHighlight.Spawn(cluster, color, duration, enableArrow);
            created.OnFadeStart = () =>
            {
                foreach (PingableTarget target in created.Targets)
                {
                    if (ActiveByTarget.TryGetValue(target.GameObject, out ItemPingHighlight owner) && owner == created)
                    {
                        ActiveByTarget.Remove(target.GameObject);
                    }
                }
            };
            foreach (PingableTarget target in cluster)
            {
                ActiveByTarget[target.GameObject] = created;
            }
        }
    }
}
