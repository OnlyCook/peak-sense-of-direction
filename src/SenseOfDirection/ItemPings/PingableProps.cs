using System;
using SenseOfDirection.Ui.Localization;
using UnityEngine;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// The level's fixed props and hazards: things that are neither an
    /// <c>Item</c> nor a creature, have no shared base class, and so had to be
    /// identified one component at a time (each confirmed against a real ping's
    /// <see cref="ItemPingDetector.LogNearbyUnmatched"/> dump, then looked up in
    /// the decompile - the ISSUES.md workflow for "this should be pingable but
    /// isn't").
    ///
    /// One resolver rather than a bucket per type, unlike the creature lists in
    /// <see cref="PingableRegistry"/>: these share every rule (same detection
    /// radius handling, same bounds-based matching, same untranslated English
    /// label as the older <c>NamedHazards</c> entries), so the only thing that
    /// actually differs per type is the name - a switch, not eight parallel
    /// lists and eight near-identical loops.
    ///
    /// <see cref="IsLarge"/> decides which radius a kind is matched against, the
    /// same split creatures/luggage already get over items: a trap or a saw
    /// blade is a big structure you aim at generally, an amulet on a statue is
    /// item-sized and sits right next to other pingables, so it keeps the
    /// tighter item radius rather than being caught by every ping in the
    /// statue's vicinity.
    /// </summary>
    internal static class PingableProps
    {
        /// <summary>Where a prop's label/indicator is placed.</summary>
        internal enum PropAnchor
        {
            /// <summary>Center of everything the prop draws - right for the traps, whose visible mass *is* the thing.</summary>
            BoundsCenter,

            /// <summary>
            /// The prop's own transform. For a prop whose hierarchy carries far
            /// more than the thing you aim at, the bounds center lands nowhere
            /// near it: the Belltower's union measures 23m across (10 renderers
            /// - tower, grave, bell, surrounding dressing), putting its center
            /// ~18m from a ping placed on the tower itself, which is exactly
            /// what was reported. Matching still uses the bounds; only the
            /// anchor changes.
            /// </summary>
            Transform,
        }

        /// <summary>
        /// Whether this behaviour is one of the level props worth pinging, and
        /// under what name/size if so. Called for every behaviour in the level
        /// during a <see cref="PingableRegistry"/> sweep, so it stays a plain
        /// type switch with no allocations or component lookups.
        /// </summary>
        internal static bool TryResolve(MonoBehaviour behaviour, out Func<string> displayName, out bool isLarge, out PropAnchor anchor, out bool trimToBody)
        {
            anchor = PropAnchor.BoundsCenter;
            trimToBody = false;
            switch (behaviour)
            {
                // An item still sitting on its statue/pedestal (the Scout
                // amulets - healing, infinite stamina, double jump, clone - and
                // BingBong's medallion). It isn't an Item yet at all, just a
                // stand-in the game swaps for the real prefab on pickup, which
                // is exactly why it wasn't pingable while the picked-up version
                // always was. Its own GetName() is the game's localized item
                // name, so these get proper per-language labels for free -
                // unlike every other entry here.
                case FakeItem fakeItem:
                    displayName = () => SafeFakeItemName(fakeItem);
                    isLarge = false;
                    return true;

                // The pyre with the bell ("belltower"), lit or not - identified
                // from a ping's ancestor dump ('Pyre' -> GhostFire), since its
                // own colliders are generically named Grave/Bell. Like FakeItem
                // it carries a localized name of its own (displayNameIndex).
                // Deliberately pingable in both states, unlike a campfire (which
                // this mod stops offering once lit, because a lit one is a place
                // you're already at): a pyre is a gloom safe zone either way, so
                // pointing one out stays useful after it's lit.
                case GhostFire ghostFire:
                    displayName = () => SafeLocalizedName(ghostFire.displayNameIndex,
                        WorldObjectLocalization.Get(WorldObjectLocalization.Keys.Pyre));
                    isLarge = true;
                    anchor = PropAnchor.Transform;
                    return true;

                case VenusFlyTrap _:
                    displayName = () => WorldObjectLocalization.Get(WorldObjectLocalization.Keys.Flytrap);
                    isLarge = true;
                    return true;

                // The floating one that chases you down and explodes into a
                // freezing/blinding cloud. Named after its own class, since it
                // has no in-game name string anywhere in the decompile.
                case Peak.GhostBall _:
                    displayName = () => WorldObjectLocalization.Get(WorldObjectLocalization.Keys.GhostBall);
                    isLarge = true;
                    return true;

                case ArrowShooter _:
                    displayName = () => WorldObjectLocalization.Get(WorldObjectLocalization.Keys.ArrowTrap);
                    isLarge = true;
                    // The one kind that needs trimming: it parents its spawned
                    // warning arrows to itself wherever they land, so its
                    // untrimmed box measured 54 x 1.8 x 17.5m - a slab across
                    // half the temple that answered pings aimed 6-14m away at
                    // unrelated things. Nothing else here scatters children
                    // like that, and trimming by default cost the tall Spike
                    // Roller most of its pingable body (see PropAnchor).
                    trimToBody = true;
                    return true;

                case Peak.SpikeTrap _:
                    displayName = () => WorldObjectLocalization.Get(WorldObjectLocalization.Keys.SpikeTrap);
                    isLarge = true;
                    return true;

                case Peak.MovingSawBlade _:
                    displayName = () => WorldObjectLocalization.Get(WorldObjectLocalization.Keys.SawBlade);
                    isLarge = true;
                    return true;

                // Vanilla calls this one SwingingAxe; the label follows what it
                // actually looks like in-game (a spiked mace on a chain).
                case SwingingAxe _:
                    displayName = () => WorldObjectLocalization.Get(WorldObjectLocalization.Keys.SwingingMace);
                    isLarge = true;
                    return true;

                // The spinning spiked log. Its bounds are what make this
                // pingable at all - see the bounds-based matching in
                // ItemPingDetector: the log is long and rotating, so any single
                // point on it (least of all its pivot) is a poor thing to
                // measure against.
                case Peak.SpikeRoller _:
                    displayName = () => WorldObjectLocalization.Get(WorldObjectLocalization.Keys.SpikeRoller);
                    isLarge = true;
                    // Anchored to its pivot, which *is* its axis of rotation:
                    // measured live, a roller's bounding box center wanders
                    // (10.55 -> 11.04 on X between two pings of the same one) as
                    // its arms sweep around, so an indicator riding the box
                    // center twitches constantly while the thing it points at
                    // hasn't gone anywhere. The pivot is stationary by
                    // definition. Confirmed against a second roller in the same
                    // run whose box center sat within 2cm of its pivot.
                    anchor = PropAnchor.Transform;
                    return true;

                default:
                    displayName = null;
                    isLarge = false;
                    return false;
            }
        }

        /// <summary>
        /// The game's own icon for a prop, where the prop stands in for
        /// something that has one. Only <see cref="FakeItem"/> does: it carries
        /// the <c>realItemPrefab</c> it will be swapped for on pickup, and that
        /// prefab's <c>UIData</c> holds the very icon vanilla's inventory shows
        /// - so an amulet on its statue can show the same art the picked-up
        /// amulet does, instead of the mod's generic item-ping marker. Every
        /// other prop here is scenery with no UI representation anywhere in the
        /// game, so there's nothing to borrow.
        /// </summary>
        internal static Sprite TryGetIcon(MonoBehaviour behaviour)
        {
            if (behaviour is FakeItem fakeItem && fakeItem.realItemPrefab != null)
            {
                return Common.NativeIconCache.ForItem(fakeItem.realItemPrefab);
            }
            return null;
        }

        /// <summary>
        /// <c>FakeItem.GetName()</c> goes through <c>LocalizedText</c>, which
        /// answers an unknown key with the literal placeholder
        /// <c>"LOC: SOMETHING"</c> rather than throwing - and this runs inside
        /// ping handling, where an exception costs the whole ping. Falls back to
        /// the raw <c>itemName</c> field, then to a generic label.
        /// </summary>
        private static string SafeFakeItemName(FakeItem fakeItem)
        {
            try
            {
                return SafeLocalizedName(LocalizedText.GetNameIndex(fakeItem.itemName),
                    !string.IsNullOrEmpty(fakeItem.itemName) ? fakeItem.itemName : "Item");
            }
            catch (System.Exception)
            {
                return "Item";
            }
        }

        /// <summary>
        /// A game localization key resolved to text, with the two ways that can
        /// go wrong handled: <c>LocalizedText.GetText</c> answers an unknown key
        /// with the literal placeholder <c>"LOC: SOMETHING"</c> rather than
        /// throwing, and this runs inside ping handling where an exception costs
        /// the whole ping.
        /// </summary>
        private static string SafeLocalizedName(string localizationKey, string fallback)
        {
            try
            {
                if (string.IsNullOrEmpty(localizationKey))
                {
                    return fallback;
                }
                string name = LocalizedText.GetText(localizationKey);
                return !string.IsNullOrEmpty(name) && !name.StartsWith("LOC:") ? name : fallback;
            }
            catch (System.Exception)
            {
                return fallback;
            }
        }

        /// <summary>
        /// World-space bounds of everything this prop actually draws, unioned -
        /// the anchor for both the match test and the label position.
        ///
        /// A single point can't represent these: a saw blade rides a spline
        /// track, a swinging mace hangs off an arm, a spike roller is a long
        /// rotating log, and in each case the component's own transform sits at
        /// a pivot that can be metres from anything visible (the maintainer's
        /// note on the roller - "just ping its center" - is exactly this).
        /// Renderer bounds are already world-space and follow the animation, so
        /// they answer both questions live with no per-frame bookkeeping.
        ///
        /// The renderer array itself is cached per prop by
        /// <see cref="CollectRenderers"/>; only the bounds within it are read
        /// live, so this stays allocation-free on the ping path.
        /// </summary>
        internal static bool TryGetBounds(Renderer[] renderers, GameObject go, bool trimToBody, out Bounds bounds)
        {
            // float.PositiveInfinity = keep every renderer; the trim is opt-in
            // per kind rather than a blanket rule (see the ArrowShooter case).
            float maxDistanceSq = trimToBody ? MaxRendererDistanceFromPivotSq : float.PositiveInfinity;
            Vector3 pivot = go != null ? go.transform.position : Vector3.zero;

            if (TryUnionBounds(renderers, pivot, maxDistanceSq, out bounds))
            {
                return true;
            }

            // Cached array came back empty. A prop registered through its own
            // Start/Awake hook can be measured before the rest of its prefab is
            // in place, and some props (GenericOptimizer users like the flytrap)
            // rebuild their visuals later - so re-collect live rather than
            // treating the prop as unpingable forever. Rare by construction, so
            // the allocation doesn't matter; the cached path stays the norm.
            if (go != null && TryUnionBounds(CollectRenderers(go), pivot, maxDistanceSq, out bounds))
            {
                return true;
            }

            // Nothing drawn at all (or nothing drawn *yet*): fall back to the
            // prop's colliders, which is what the ping raycast actually hit.
            return go != null && TryColliderBounds(go, out bounds);
        }

        /// <summary>
        /// Union of a prop's renderers, optionally trimmed to those within
        /// <see cref="MaxRendererDistanceFromPivot"/> of its pivot.
        ///
        /// The trim is opt-in per kind, not a blanket rule: applied to
        /// everything it cost the vertical Spike Roller most of its body, so
        /// only the top few metres of a ~37m-tall hazard could be pinged at all
        /// (reported, and the reason this is a flag).
        ///
        /// The trim is not cosmetic. An <c>ArrowShooter</c> parents its spawned
        /// *warning arrows* to itself, scattered wherever they landed across the
        /// room - untrimmed, one measured 54m x 1.8m x 17.5m, a flat slab
        /// covering half the temple. Since a prop also matches when the aim line
        /// passes through its box, that slab made "ARROW TRAP" answer pings
        /// aimed at completely unrelated things (seen in the log: matches at
        /// 6.4m, 10.7m, 14.4m from a 2.19m radius). Trimming to the body keeps
        /// the aim test meaning what it says.
        ///
        /// Applied live rather than at collection time because these props move:
        /// a renderer's distance from the pivot is only meaningful right now.
        /// </summary>
        private static bool TryUnionBounds(Renderer[] renderers, Vector3 pivot, float maxDistanceFromPivotSq, out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            if (renderers == null)
            {
                return false;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }
                Bounds rendererBounds = renderer.bounds;
                if ((rendererBounds.center - pivot).sqrMagnitude > maxDistanceFromPivotSq)
                {
                    continue;
                }
                if (!any)
                {
                    bounds = rendererBounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(rendererBounds);
                }
            }

            return any;
        }

        /// <summary>
        /// How far from its pivot a renderer can sit and still count as part of
        /// the prop (world units; ~1.37 units per metre). Generous enough for
        /// the tallest real body measured - a swinging mace's arm at ~6 units -
        /// while excluding an arrow trap's stray warning arrows.
        /// </summary>
        private const float MaxRendererDistanceFromPivot = 8f;
        private const float MaxRendererDistanceFromPivotSq = MaxRendererDistanceFromPivot * MaxRendererDistanceFromPivot;

        /// <summary>Last-resort bounds from a prop's colliders, for one that draws nothing of its own.</summary>
        private static bool TryColliderBounds(GameObject go, out Bounds bounds)
        {
            bounds = default;
            bool any = false;

            foreach (Collider collider in go.GetComponentsInChildren<Collider>(includeInactive: true))
            {
                if (collider == null)
                {
                    continue;
                }
                if (!any)
                {
                    bounds = collider.bounds;
                    any = true;
                }
                else
                {
                    bounds.Encapsulate(collider.bounds);
                }
            }

            return any;
        }

        /// <summary>
        /// The renderers <see cref="TryGetBounds"/> measures, gathered once when
        /// a prop is registered (see <c>PingableRegistry.PropTarget</c>) rather
        /// than per ping.
        ///
        /// Particle/trail/line renderers are filtered out here for the same
        /// reason <see cref="ItemPingDetector.GetLiveCenter"/> skips them: an
        /// effect that has never played reports a zero-size box at the world
        /// origin, which would stretch the union across the entire level.
        /// </summary>
        internal static Renderer[] CollectRenderers(GameObject go)
        {
            var kept = new System.Collections.Generic.List<Renderer>();
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(includeInactive: true))
            {
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                {
                    continue;
                }
                kept.Add(renderer);
            }
            return kept.ToArray();
        }
    }
}
