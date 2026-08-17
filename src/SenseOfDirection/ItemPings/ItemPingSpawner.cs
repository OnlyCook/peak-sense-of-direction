using System.Collections.Generic;
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

        /// <summary>
        /// How close a ping has to land to an item strapped into *another*
        /// player's worn backpack for it to count as pinged - deliberately far
        /// tighter than the configurable item radius (2m by default) and not
        /// user-configurable, since this isn't a "how forgiving should aiming
        /// be" knob but a "you have to actually be aiming at their pack, not
        /// past them" rule. Roughly the size of the pack itself plus a small
        /// buffer, so a direct hit anywhere on it still registers the slot you
        /// aimed at while a ping sailing past the player picks up nothing.
        /// See <see cref="PingIgnoreFilter"/> for the wearer's own pack, which
        /// is excluded outright rather than tightened.
        /// </summary>
        private const float WornBackpackRadiusMeters = 0.6f;

        /// <summary>Reused between pings (see the grouping comment in <see cref="SpawnFor"/>) - a ping is always fully handled before the next one is, so a single scratch pair is safe.</summary>
        private static readonly List<List<PingableTarget>> _clustersScratch = new List<List<PingableTarget>>();
        private static readonly Dictionary<string, List<PingableTarget>> _clusterByNameScratch = new Dictionary<string, List<PingableTarget>>();

        /// <returns>How many item/luggage targets were highlighted (new or merged), for the caller to decide whether to suppress its own generic ping distance label.</returns>
        public static int SpawnFor(Vector3 point, Color color, Character pingingCharacter)
        {
            PluginConfig cfg = Plugin.Instance.Cfg;

            float itemRadiusUnits = cfg.ItemPingDetectionRadiusMeters.Value / CharacterStats.unitsToMeters;
            float crossKindRadiusUnits = cfg.ItemPingCrossKindRadiusMeters.Value / CharacterStats.unitsToMeters;
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
                point, itemRadiusUnits, crossKindRadiusUnits, luggageRadiusUnits,
                rayOrigin, rayDirection, rayMaxDistanceUnits, rayHitboxRadiusUnits,
                cfg.EnableCreaturePings.Value, pingingCharacter,
                WornBackpackRadiusMeters / CharacterStats.unitsToMeters);

            // Whatever the pinging player is currently holding sits right in
            // front of their own aim ray/near their own position, so it's
            // otherwise a near-guaranteed match on both the point-radius and
            // ray-alignment checks above - excluded here regardless of which
            // one matched, since a player pinging past their own held item
            // should never end up (item-)pinging the thing in their own
            // hands (ISSUES.md).
            Item heldItem = pingingCharacter.data.currentItem;
            if (heldItem != null)
            {
                found.RemoveAll(t => t.GameObject == heldItem.gameObject);
            }

            if (cfg.EnableDebugLogging.Value)
            {
                ItemPingDetector.LogNearbyUnmatched(point, itemRadiusUnits, Plugin.Instance.Log);
            }

            // The campfire indicator and the scout statue indicator both
            // already show an always-on indicator for exactly the thing
            // ItemPingDetector just (re-)detected here (the current
            // campfire, an unclaimed scout amulet) - so pinging either one,
            // while its own indicator is on, flashes that existing
            // indicator's color instead of spawning a second, overlapping
            // highlight for the same thing. See PingFlashState's own doc
            // comment. Still counted in the returned total (the caller uses
            // it to suppress the generic ping's own redundant distance
            // label), same as an ordinary highlight would be.
            int flashHandledCount = found.RemoveAll(target => TryHandleFixedIndicatorPing(target, color, cfg));

            if (found.Count == 0)
            {
                return flashHandledCount;
            }

            bool enableArrow = cfg.EnableItemPingOffScreenIndicator.Value;
            float duration = cfg.ItemPingDurationSeconds.Value;

            // Plain dictionary/list grouping rather than LINQ: this runs inside
            // the frame a ping lands in, so it's on the path ISSUES.md's "never
            // stutter when pinging" complaint is about - GroupBy/Select/ToList
            // allocate an enumerator chain, a grouping object and a list per
            // cluster on top of the lists actually handed to the highlights.
            _clustersScratch.Clear();
            if (cfg.EnableItemPingGrouping.Value)
            {
                _clusterByNameScratch.Clear();
                foreach (PingableTarget target in found)
                {
                    string name = target.GetDisplayName();
                    if (!_clusterByNameScratch.TryGetValue(name, out List<PingableTarget> cluster))
                    {
                        cluster = new List<PingableTarget>();
                        _clusterByNameScratch[name] = cluster;
                        _clustersScratch.Add(cluster);
                    }
                    cluster.Add(target);
                }
            }
            else
            {
                foreach (PingableTarget target in found)
                {
                    _clustersScratch.Add(new List<PingableTarget> { target });
                }
            }

            foreach (List<PingableTarget> cluster in _clustersScratch)
            {
                SpawnOrMerge(cluster, color, duration, enableArrow);
            }

            return found.Count + flashHandledCount;
        }

        /// <summary>
        /// The campfire/scout-amulet/Belltower/Pirate's-Compass-luggage
        /// special case described above <see cref="SpawnFor"/>'s own call
        /// site. The first three are matched by component alone (a live
        /// <c>Campfire</c>, a <c>FakeItem</c> tagged
        /// <c>Item.ItemTags.ScoutAmulet</c> - the same tag
        /// <c>Peak.ScoutStatue.IsConstantlyInteractable</c> gates on - or a
        /// <c>GhostFire</c>) rather than by GameObject identity against
        /// whichever instance the indicator controllers currently track,
        /// since a ping can land on *any* of the (up to) 4 scout amulets or
        /// any Belltower in the level, not just the nearest one this
        /// session's single indicator widget happens to be pointing at right
        /// now - the flash still fires either way, per the maintainer's ask
        /// ("the pointing [...] indicator" is the one persistent widget each
        /// of these features ever creates). The luggage case is different:
        /// a level can hold dozens of unopened Luggage at once, so it's
        /// matched by identity against the one specific instance the Pirate's
        /// Compass indicator is actually tracking - see
        /// <see cref="PirateCompass.PirateCompassLuggageIndicatorController.IsTracking"/>.
        /// </summary>
        private static bool TryHandleFixedIndicatorPing(PingableTarget target, Color color, PluginConfig cfg)
        {
            GameObject go = target.GameObject;
            if (go == null)
            {
                return false;
            }

            if (cfg.EnableCampfireIndicator.Value && go.TryGetComponent(out Campfire _))
            {
                CampfireIndicator.CampfireIndicatorController.Instance.NotifyPinged(color);
                return true;
            }

            if (cfg.EnableScoutStatueIndicator.Value && go.TryGetComponent(out FakeItem fakeItem)
                && fakeItem.realItemPrefab != null && fakeItem.realItemPrefab.itemTags.HasFlag(Item.ItemTags.ScoutAmulet))
            {
                ScoutStatueIndicator.ScoutStatueIndicatorController.Instance.NotifyPinged(color);
                return true;
            }

            if (cfg.EnableBelltowerIndicator.Value && go.TryGetComponent(out GhostFire _))
            {
                BelltowerIndicator.BelltowerIndicatorController.Instance.NotifyPinged(color);
                return true;
            }

            // Unlike the three cases above, a level can hold dozens of
            // unopened Luggage at once (a RespawnChest/"Ancient Statue"
            // included - it's a Luggage subclass with no exclusion anywhere
            // in this detection path), so this one has to match by identity
            // against the specific instance PirateCompassLuggageIndicatorController
            // is actually showing right now, not just "any Luggage" - see
            // that class's own IsTracking doc comment.
            if (cfg.EnablePirateCompassLuggageIndicator.Value && go.TryGetComponent(out Luggage luggage)
                && PirateCompass.PirateCompassLuggageIndicatorController.Instance.IsTracking(luggage.gameObject))
            {
                PirateCompass.PirateCompassLuggageIndicatorController.Instance.NotifyPinged(color);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Spawns (or merges into an existing) highlight for a single caller-built
        /// cluster, bypassing <see cref="SpawnFor"/>'s own point-radius detection -
        /// used by <see cref="LuggagePing.LuggagePingController"/>, which finds its
        /// own targets (every unopened luggage within a flat radius of the player,
        /// not a ping point) but still wants a re-ping of an already-highlighted
        /// luggage to merge/refresh rather than stack a duplicate highlight, same
        /// as a normal ping would.
        /// </summary>
        public static void SpawnOrMerge(List<PingableTarget> cluster, Color color, float duration, bool enableArrow, bool compassSpawnPop = false)
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

            ItemPingHighlight created = ItemPingHighlight.Spawn(cluster, color, duration, enableArrow, compassSpawnPop);
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
