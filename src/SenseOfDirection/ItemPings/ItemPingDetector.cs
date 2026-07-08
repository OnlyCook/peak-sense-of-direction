using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// One nearby pingable thing found near a ping point (an <c>Item</c>,
    /// <c>Luggage</c>, or a creature). <c>GetCenter</c>/<c>GetDisplayName</c>
    /// stay live (re-read the source component every call) rather than
    /// snapshotting once, so a highlight's label/position stays correct if
    /// the target moves or its name changes (e.g. an item's cooked-state
    /// name) while the highlight is still showing.
    /// </summary>
    public readonly struct PingableTarget
    {
        public readonly GameObject GameObject;
        public readonly Func<Vector3> GetCenter;
        public readonly Func<string> GetDisplayName;

        public PingableTarget(GameObject gameObject, Func<Vector3> getCenter, Func<string> getDisplayName)
        {
            GameObject = gameObject;
            GetCenter = getCenter;
            GetDisplayName = getDisplayName;
        }
    }

    /// <summary>
    /// Native replacement for <c>memiczny-PingItems</c>' item/luggage
    /// detection (its <c>PingableFactory</c>/<c>OptimizedPingableFactory</c>),
    /// reimplemented from scratch per RESEARCH.md Q8/ROADMAP.md Phase 5b (that
    /// mod ships no LICENSE, so its approach is a reference only, not a source
    /// of copied code) - and against current game APIs, since the reference
    /// mod's own <c>Item.ALL_ITEMS</c> read no longer exists at all (renamed/
    /// restructured by a game update, part of why that mod is broken
    /// outright) - and its apparent replacement, <c>Item.ALL_ACTIVE_ITEMS</c>,
    /// turned out to be the wrong list too (see the scene-wide-query comment
    /// below): a "recently relevant" optimization cache, not a master list of
    /// every item. Deliberately narrower in scope than the reference mod: no
    /// spatial-grid/pooling optimization (this runs once per ping, not
    /// per-frame, against a level's worth of objects - not worth the
    /// complexity), and no celestial/spore-shroom adapters. <c>MirageLuggage</c>
    /// is intentionally left out: it has no display name of its own (just a
    /// decoy visual effect component), so there's nothing meaningful to label
    /// it with. Giant urchins and spore bombs are *not* supported yet either,
    /// despite being requested - no distinct component/class for either was
    /// found in the decompile (unlike jellyfish/mobs/spider/capybara), so
    /// detecting them needs the actual in-scene GameObject name identified
    /// first - see <see cref="LogNearbyUnmatched"/>.
    ///
    /// A target counts as found if either: it's within the configured radius
    /// of the ping's landed point (<c>point</c>), or it lies close enough to
    /// the pinging player's approximate aim line (<c>rayOrigin</c>/
    /// <c>rayDirection</c>, reconstructed as head-to-point since the RPC only
    /// carries the final point/normal, not the original camera ray) to count
    /// as "aimed at," regardless of distance to <c>point</c>. The ray check
    /// is pure math, deliberately independent of <c>Physics</c>/colliders -
    /// per the decompile (`Item.SetState`/`SetColliders`), an item still
    /// attached to its spawn point (an unpicked coconut on a tree, berries on
    /// a bush, a just-spawned item from opened luggage) has its own collider
    /// *disabled* until first picked up (the same reason it can't be pushed
    /// either), so no raycast/spherecast against real physics - including
    /// `Pings/PointPingerPatches.TryGetPingHitPrefix`'s own hitbox-assisted
    /// ping raycast - can ever land directly on one. The ray-distance test
    /// here sidesteps that entirely by never touching `Physics` for the
    /// item/luggage/creature side of the check.
    /// </summary>
    public static class ItemPingDetector
    {
        public static List<PingableTarget> FindNear(
            Vector3 point, float itemRadiusUnits, float luggageRadiusUnits,
            Vector3 rayOrigin, Vector3 rayDirection, float rayMaxDistanceUnits, float rayHitboxRadiusUnits,
            bool includeCreatures)
        {
            var results = new List<PingableTarget>();

            float itemRadiusSq = itemRadiusUnits * itemRadiusUnits;
            float luggageRadiusSq = luggageRadiusUnits * luggageRadiusUnits;
            float rayHitboxRadiusSq = rayHitboxRadiusUnits * rayHitboxRadiusUnits;

            bool Matches(Vector3 center, float radiusSq)
            {
                return (center - point).sqrMagnitude <= radiusSq
                    || MatchesRay(center, rayOrigin, rayDirection, rayMaxDistanceUnits, rayHitboxRadiusSq);
            }

            // Creatures (Mob/Spider/Capybara/MushroomZombie/Antlion) also get
            // a wider ray-alignment tolerance, not just a wider point-radius:
            // the ping's own landed point can't be trusted to sit anywhere
            // near a hostile creature in the first place (its collider is
            // almost certainly on the "Character" physics layer, deliberately
            // excluded from the ping-hitbox-assist raycast to avoid snapping
            // pings onto teammates - so the marker visually passes through it
            // to whatever's behind, same as vanilla always did), which means
            // rayDirection (reconstructed as head-to-point, not the true
            // camera ray) can diverge further from the real aim line than it
            // does for a stationary target the raycast actually stopped on.
            // Reported necessary after a live test: 15 pings landing visibly
            // on/near a MushroomZombie still failed to highlight it even
            // after the point-radius widening alone.
            float creatureRayHitboxRadiusSq = (rayHitboxRadiusUnits * 2f) * (rayHitboxRadiusUnits * 2f);

            bool MatchesCreature(Vector3 center)
            {
                return (center - point).sqrMagnitude <= luggageRadiusSq
                    || MatchesRay(center, rayOrigin, rayDirection, rayMaxDistanceUnits, creatureRayHitboxRadiusSq);
            }

            // Deliberately not Item.ALL_ACTIVE_ITEMS: per the decompile
            // (Item.OnEnable/Start + ItemOptimizationManager.Update), that
            // list is a "recently relevant" optimization cache, not "every
            // item" - WasActive() (which adds an item to it) is only called
            // when the item's own Rigidbody isn't kinematic, so an item still
            // attached to its spawn point (tree/bush, still kinematic - see
            // the ray-assist doc comment above) is never added at all, and
            // ItemOptimizationManager expires *any* item from the list after
            // 30 seconds without a fresh WasActive() call (e.g. simply
            // sitting on the ground, untouched, while the player is far away
            // - exactly the "walked off and came back" case). A scene-wide
            // query sidesteps both: same pattern used for every other type
            // below, cheap enough since this runs once per accepted ping, not
            // per-frame.
            foreach (Item item in UnityEngine.Object.FindObjectsByType<Item>(FindObjectsSortMode.None))
            {
                if (item == null || !item.gameObject.activeInHierarchy || !Matches(item.Center(), itemRadiusSq))
                {
                    continue;
                }
                Item capturedItem = item;
                results.Add(new PingableTarget(capturedItem.gameObject, () => capturedItem.Center(), () => capturedItem.GetItemName()));
            }

            foreach (Luggage luggage in Luggage.ALL_LUGGAGE)
            {
                if (luggage == null || !luggage.gameObject.activeInHierarchy || !Matches(luggage.transform.position, luggageRadiusSq))
                {
                    continue;
                }
                Luggage capturedLuggage = luggage;
                results.Add(new PingableTarget(capturedLuggage.gameObject, () => capturedLuggage.transform.position, () => capturedLuggage.GetName()));
            }

            foreach (SlipperyJellyfish jellyfish in UnityEngine.Object.FindObjectsByType<SlipperyJellyfish>(FindObjectsSortMode.None))
            {
                if (jellyfish == null || !jellyfish.gameObject.activeInHierarchy || !Matches(jellyfish.transform.position, itemRadiusSq))
                {
                    continue;
                }
                SlipperyJellyfish capturedJellyfish = jellyfish;
                results.Add(new PingableTarget(capturedJellyfish.gameObject, () => capturedJellyfish.transform.position, () => "Jellyfish"));
            }

            if (includeCreatures)
            {
                // Mob is the base class for most creatures (confirmed via
                // decompile: Beetle : Mob) - detecting the base type picks up
                // every Mob-derived species for free rather than needing a
                // hardcoded per-species list. Spider and Capybara don't
                // inherit Mob, so they're handled explicitly. None of these
                // classes carry a display name field (unlike Item/Luggage),
                // so Mob falls back to its GameObject name (stripped of the
                // usual runtime "(Clone)" suffix) - approximate, but the best
                // available without a hardcoded species→name table. Matched
                // against luggageRadiusSq (the larger of the two configured
                // radii), not itemRadiusSq - creatures move, unlike static
                // loot, and are physically bigger than most items, so they
                // warrant the same "bigger target" forgiveness Luggage
                // already gets (confirmed necessary via a live MushroomZombie
                // test that landed 1.4-2.1m from the ping point, right at/
                // past the tighter item radius).
                foreach (Mob mob in UnityEngine.Object.FindObjectsByType<Mob>(FindObjectsSortMode.None))
                {
                    if (mob == null || !mob.gameObject.activeInHierarchy || !MatchesCreature(mob.transform.position))
                    {
                        continue;
                    }
                    Mob capturedMob = mob;
                    string name = capturedMob.gameObject.name.Replace("(Clone)", string.Empty).Trim();
                    results.Add(new PingableTarget(capturedMob.gameObject, () => capturedMob.transform.position, () => name));
                }

                foreach (Spider spider in UnityEngine.Object.FindObjectsByType<Spider>(FindObjectsSortMode.None))
                {
                    if (spider == null || !spider.gameObject.activeInHierarchy || !MatchesCreature(spider.transform.position))
                    {
                        continue;
                    }
                    Spider capturedSpider = spider;
                    results.Add(new PingableTarget(capturedSpider.gameObject, () => capturedSpider.transform.position, () => "Spider"));
                }

                // Capybara deliberately kept on the tighter, plain item
                // radius/ray tolerance (Matches, not MatchesCreature) per
                // maintainer correction - unlike the other creatures here,
                // capybaras are static decoration (they don't move or do
                // anything), and fruit items are commonly placed right next
                // to them - widening their catch radius the same way moving
                // creatures need would make it too easy to end up
                // highlighting the capybara when a nearby fruit item was
                // the thing actually meant to be pinged.
                foreach (Capybara capybara in UnityEngine.Object.FindObjectsByType<Capybara>(FindObjectsSortMode.None))
                {
                    if (capybara == null || !capybara.gameObject.activeInHierarchy || !Matches(capybara.transform.position, itemRadiusSq))
                    {
                        continue;
                    }
                    Capybara capturedCapybara = capybara;
                    results.Add(new PingableTarget(capturedCapybara.gameObject, () => capturedCapybara.transform.position, () => "Capybara"));
                }

                // Confirmed via a dedicated diagnostic pass (not
                // Character-based at all - Character.isZombie was 0 every
                // time; a live test found a real MushroomZombie 1.4-2.1m from
                // the ping, i.e. right at/just past the old (item-sized, 2m
                // default) detection radius, consistent with "distance was
                // just off" rather than a wrong-component problem).
                // Reported to only ever match the very first ping, and its
                // highlight then stayed fixed at that spot even as the
                // (moving) zombie visibly walked away - strongly suggests
                // MushroomZombie's own root transform.position doesn't
                // actually track its visible body (root-motion-less
                // animation, or the root sitting at a fixed logic anchor
                // while a child rig does the actual moving). Same class of
                // bug Item.Center() already guards against for items (it
                // reads mainRenderer.bounds.center, not transform.position,
                // for exactly this reason) - applied the same fix here via a
                // child Renderer's live bounds center instead.
                foreach (MushroomZombie zombie in UnityEngine.Object.FindObjectsByType<MushroomZombie>(FindObjectsSortMode.None))
                {
                    if (zombie == null || !zombie.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    Vector3 zombieCenter = GetLiveCenter(zombie.gameObject);
                    if (!MatchesCreature(zombieCenter))
                    {
                        continue;
                    }
                    GameObject capturedZombieGo = zombie.gameObject;
                    results.Add(new PingableTarget(capturedZombieGo, () => GetLiveCenter(capturedZombieGo), () => "Zombie"));
                }

                foreach (Antlion antlion in UnityEngine.Object.FindObjectsByType<Antlion>(FindObjectsSortMode.None))
                {
                    if (antlion == null || !antlion.gameObject.activeInHierarchy || !MatchesCreature(antlion.transform.position))
                    {
                        continue;
                    }
                    Antlion capturedAntlion = antlion;
                    results.Add(new PingableTarget(capturedAntlion.gameObject, () => capturedAntlion.transform.position, () => "Antlion"));
                }

                // Requested for completeness even though the campfire already
                // has its own always-visible edge indicator (Phase 4,
                // CampfireIndicatorController) - only the *current* segment's
                // campfire, matching what that indicator already points at.
                // MapHandler.ExistsAndInitialized guard is mandatory here,
                // not optional: MapHandler.CurrentCampfire's getter throws a
                // NullReferenceException outside an actual run (e.g. at the
                // Airport) - CampfireIndicatorController already knows to
                // check this first; this loop didn't, and the resulting
                // exception was being silently swallowed by
                // ReceivePointRpcPrefix's outer try/catch (falls back to
                // vanilla on any exception) - meaning *every* ping while at
                // the Airport (or any other not-yet-initialized state)
                // silently skipped ALL item-ping detection, not just the
                // campfire check, mimicking a total detection failure that
                // had nothing to do with radius/matching logic at all.
                if (MapHandler.ExistsAndInitialized)
                {
                    Campfire campfire = MapHandler.CurrentCampfire;
                    if (campfire != null && campfire.gameObject.activeInHierarchy && Matches(campfire.transform.position, itemRadiusSq))
                    {
                        results.Add(new PingableTarget(campfire.gameObject, () => campfire.transform.position, () => "Campfire"));
                    }
                }

                // Pickaxe and (Rusty) Piton are the same underlying component
                // - ClimbHandle, a climbable handhold anchor - distinguished
                // only by its own isPickaxe flag (confirmed via decompile;
                // ClimbHandle.GetName() itself returns "PICKAXE" or the
                // less clean "PITONPROMPT" localization key, so a hardcoded
                // name per flag is used instead for a cleaner label).
                foreach (ClimbHandle handle in UnityEngine.Object.FindObjectsByType<ClimbHandle>(FindObjectsSortMode.None))
                {
                    if (handle == null || !handle.gameObject.activeInHierarchy || !Matches(handle.Center(), itemRadiusSq))
                    {
                        continue;
                    }
                    ClimbHandle capturedHandle = handle;
                    string name = capturedHandle.isPickaxe ? "Pickaxe" : "Piton";
                    results.Add(new PingableTarget(capturedHandle.gameObject, () => capturedHandle.Center(), () => name));
                }

                // Giant Urchin: no distinctive name anywhere in its own
                // hierarchy (its hitbox GameObject is plainly named
                // "Collider", parented directly under the level's generic
                // "Map" root) - confirmed via LogNearbyUnmatched's component
                // dump instead: the hitbox carries a CollisionModifier
                // (shared with Antlion, not distinctive alone) whose parent
                // carries DisableBasedOnRunSettings with
                // disableIfSettingDisabled == Hazard_Urchins (that field is
                // exactly how the game itself gates hazards on/off per-run -
                // Antlion.Start() reads the same RunSettings.SETTINGTYPE enum
                // directly) - that specific combination is what actually
                // identifies it, not a name.
                // Reported by the maintainer as only pingable while noclipped
                // inside one - the CollisionModifier's own transform.position
                // (used as the match center) is apparently well inside the
                // visible shell, not on its surface, so the normal item
                // radius (2m default) only ever reached it from point-blank
                // range. Matched against luggageRadiusUnits doubled instead -
                // deliberately more generous than the plain creature radius,
                // since this offset is structural (a fixed gap between
                // collider-root and visible surface), not just "creatures are
                // bigger/moving" like the other luggageRadiusSq uses above.
                float urchinRadiusSq = (luggageRadiusUnits * 2f) * (luggageRadiusUnits * 2f);
                foreach (CollisionModifier modifier in UnityEngine.Object.FindObjectsByType<CollisionModifier>(FindObjectsSortMode.None))
                {
                    if (modifier == null || !modifier.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    Transform parent = modifier.transform.parent;
                    if (parent == null)
                    {
                        continue;
                    }
                    DisableBasedOnRunSettings disabler = parent.GetComponent<DisableBasedOnRunSettings>();
                    if (disabler == null || disabler.disableIfSettingDisabled != RunSettings.SETTINGTYPE.Hazard_Urchins
                        || !Matches(modifier.transform.position, urchinRadiusSq))
                    {
                        continue;
                    }
                    CollisionModifier capturedModifier = modifier;
                    results.Add(new PingableTarget(capturedModifier.gameObject, () => capturedModifier.transform.position, () => "Giant Urchin"));
                }

                // Spore bombs / explosive spore bombs have no dedicated
                // component at all in the decompile (same situation the
                // reference mod hit for its own "SporeShroom" - it matched by
                // plain GameObject name too, no class to key off). Names
                // identified via LogNearbyUnmatched against a real
                // maintainer playthrough: "Forest_SporeFungus" (regular) and
                // "Jungle_SporeMushroomExplo" (explosive) - matched by
                // substring, not exact equality, so other biomes' prefab
                // name variants (if the biome prefix differs) still match.
                // A bounded OverlapSphere rather than a full scene query,
                // since (unlike tree-coconuts) these sit on the ground where
                // you'd ping directly at them - no ray-assist reach needed.
                var seenHazards = new HashSet<GameObject>();
                foreach (Collider collider in Physics.OverlapSphere(point, itemRadiusUnits, ~0, QueryTriggerInteraction.Collide))
                {
                    GameObject go = collider.gameObject;
                    if (!seenHazards.Add(go))
                    {
                        continue;
                    }
                    string hazardName = NamedHazardDisplayName(go.name);
                    if (hazardName == null)
                    {
                        continue;
                    }
                    GameObject capturedGo = go;
                    results.Add(new PingableTarget(capturedGo, () => capturedGo.transform.position, () => hazardName));
                }
            }

            return results;
        }

        /// <summary>
        /// Objects with no dedicated component in the decompile at all - same
        /// situation the reference mod hit for its own "SporeShroom", which
        /// it matched by plain GameObject name too, no class to key off.
        /// Checked in order, most-specific substring first (e.g.
        /// "SporeMushroomExplo" before the shorter "SporeMushroom", which
        /// would otherwise also match it). Names identified via
        /// <see cref="LogNearbyUnmatched"/> against real maintainer
        /// playthroughs - matched by substring, not exact equality, so other
        /// biomes' differently-prefixed prefab variants (if any) still match.
        /// </summary>
        private static readonly (string NameContains, string DisplayName)[] NamedHazards =
        {
            ("SporeMushroomExplo", "Explosive Spore Bomb"),
            ("SporeMushroom", "Poison Spore Bomb"),
            ("SporeFungus", "Spore Bomb"),
            ("ShakyIcicle", "Icicle"),
            ("Snow Mount", "Snow Pile"),
            ("tumbleweed", "Tumbleweed"),
            ("PoisonIvy", "Poison Ivy"),
            ("Monstera", "Monstera"),
            ("Geyser", "Geyser"),
            ("FlashPlant", "Flash Bulb"),
            // Deliberately no "Cactus base" entry (removed) - that's the big
            // decorative StickyCactus structure's own ground collider, not
            // the small pickup-able cactus the maintainer actually meant
            // (confirmed via decompile: that one is a CactusBall
            // ItemComponent on a regular Item, already covered by the Item
            // loop above) - matching it here was actively wrong, producing a
            // highlight labeled "Cactus" attached to the big structure
            // instead of the real pickup.
        };

        /// <summary>
        /// Live position for a GameObject whose own root transform might not
        /// track its actual visible body (see the MushroomZombie loop above)
        /// - prefers a child Renderer's current bounds center, falling back
        /// to transform.position if it has none. Re-fetches each call rather
        /// than caching, since it's cheap (component lookups on a single
        /// known GameObject, not a scene search) and safe against the
        /// renderer being swapped/destroyed.
        ///
        /// Skips <c>ParticleSystemRenderer</c>/<c>TrailRenderer</c>/
        /// <c>LineRenderer</c> and prefers a <c>SkinnedMeshRenderer</c> (the
        /// actual animated body mesh) first - a plain
        /// <c>GetComponentInChildren&lt;Renderer&gt;()</c> is unordered
        /// across sibling children and can just as easily return an inert
        /// attack-effect particle system instead of the body (confirmed via
        /// a live debug-ESP test: a spawned MushroomZombie's own "VFX_Kick"
        /// child - a kick-attack effect that had never played, so its bounds
        /// were the Unity default `center (0,0,0), size zero` - was being
        /// returned ahead of any actual body mesh, placing the highlight at
        /// the world origin instead of the zombie).
        /// </summary>
        internal static Vector3 GetLiveCenter(GameObject go)
        {
            SkinnedMeshRenderer skinned = go.GetComponentInChildren<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                return skinned.bounds.center;
            }
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
            {
                if (renderer is ParticleSystemRenderer || renderer is TrailRenderer || renderer is LineRenderer)
                {
                    continue;
                }
                return renderer.bounds.center;
            }
            return go.transform.position;
        }

        private static string NamedHazardDisplayName(string gameObjectName)
        {
            foreach ((string nameContains, string displayName) in NamedHazards)
            {
                if (gameObjectName.Contains(nameContains))
                {
                    return displayName;
                }
            }
            return null;
        }

        /// <summary>Closest-point-on-ray distance test; ignores anything behind the ray origin or past <c>maxDistance</c> along it.</summary>
        private static bool MatchesRay(Vector3 center, Vector3 rayOrigin, Vector3 rayDirection, float maxDistance, float hitboxRadiusSq)
        {
            if (rayDirection == Vector3.zero)
            {
                return false;
            }
            float alongRay = Vector3.Dot(center - rayOrigin, rayDirection);
            if (alongRay < 0f || alongRay > maxDistance)
            {
                return false;
            }
            Vector3 closestPointOnRay = rayOrigin + rayDirection * alongRay;
            return (center - closestPointOnRay).sqrMagnitude <= hitboxRadiusSq;
        }

        /// <summary>
        /// Throttles the expensive full-scene <see cref="Renderer"/> sweep in
        /// <see cref="LogNearbyUnmatched"/> below - reported by the maintainer
        /// as a consistent stutter while spam-pinging with debug logging on.
        /// `FindObjectsByType&lt;Renderer&gt;()` walks *every* renderer in the
        /// level on every single ping when debug logging is left on (which it
        /// was, throughout this whole investigation) - fine occasionally, not
        /// fine every ping during rapid-fire testing. This is a one-off debug
        /// aid, not something that needs to re-run more than once every few
        /// seconds even during a spam-ping session.
        /// </summary>
        private static float _lastRendererScanTime = float.NegativeInfinity;
        private const float RendererScanCooldownSeconds = 3f;

        /// <summary>
        /// Debug-only aid (only runs when enable-debug-logging is on): logs
        /// the name of every collider *and* renderer within radiusUnits of a
        /// ping point that isn't already one of the recognized types, so the
        /// maintainer can identify the actual in-scene GameObject name for
        /// still-unsupported pingables without needing to re-decompile
        /// anything - once a name shows up here consistently, it can be added
        /// the same way SlipperyJellyfish/Spider/Capybara/Antlion were.
        /// Renderers are included, not just colliders, because some requested
        /// pingables (e.g. decorative foliage like Poison Ivy/Monstera) may
        /// have no Collider at all - a pure-visual mesh would otherwise never
        /// show up in a Collider-only sweep (the renderer sweep is throttled,
        /// see <see cref="RendererScanCooldownSeconds"/>, so it doesn't run on
        /// every single ping). Also filters out this mod's own spawned
        /// objects (`SoD.` prefix - ping ripples/widgets) and the local
        /// player's own first-person hand/arm model, which otherwise show up
        /// as log noise on every ping regardless of what was actually pinged.
        /// Each entry also includes `transform.root.name` - a hazard's own
        /// collider/renderer is often on a generically-named child (e.g.
        /// plain "Collider"), while the actually-identifying name usually
        /// lives on a parent further up (this is exactly what happened with
        /// a Giant Urchin ping that only logged "Collider" with no root name
        /// captured, since this wasn't yet in place - motivated the change).
        /// </summary>
        public static void LogNearbyUnmatched(Vector3 point, float radiusUnits, BepInEx.Logging.ManualLogSource log)
        {
            bool IsRecognized(GameObject go)
            {
                if (go.name.StartsWith("SoD.") || go.name == "Hand"
                    || go.GetComponentInParent<Item>() != null || go.GetComponentInParent<Luggage>() != null
                    || go.GetComponentInParent<SlipperyJellyfish>() != null || go.GetComponentInParent<Mob>() != null
                    || go.GetComponentInParent<Spider>() != null || go.GetComponentInParent<Capybara>() != null
                    || go.GetComponentInParent<MushroomZombie>() != null || go.GetComponentInParent<Antlion>() != null
                    || go.GetComponentInParent<ClimbHandle>() != null || go.GetComponentInParent<Campfire>() != null
                    || go.GetComponentInParent<Character>() != null
                    || NamedHazardDisplayName(go.name) != null)
                {
                    return true;
                }
                // Giant Urchin - see the dedicated CollisionModifier +
                // DisableBasedOnRunSettings loop above for why this specific
                // combination (not just the component type alone, which
                // Antlion also uses) identifies it.
                CollisionModifier modifier = go.GetComponent<CollisionModifier>();
                if (modifier != null && go.transform.parent != null)
                {
                    DisableBasedOnRunSettings disabler = go.transform.parent.GetComponent<DisableBasedOnRunSettings>();
                    if (disabler != null && disabler.disableIfSettingDisabled == RunSettings.SETTINGTYPE.Hazard_Urchins)
                    {
                        return true;
                    }
                }
                return false;
            }

            var seen = new HashSet<GameObject>();
            var sb = new StringBuilder();

            void Describe(GameObject go)
            {
                string rootName = go.transform.root.name;
                string label = rootName == go.name ? go.name : $"{go.name} (root: {rootName})";

                // Component types, not just names - a generic GameObject name
                // ("Collider" under a generic "Map" root, e.g. a Giant
                // Urchin's own hitbox with no distinguishing name anywhere in
                // its hierarchy) can still carry a distinctive attached
                // script/component even when its name doesn't say anything.
                // Checks the object itself and one level up (the interaction/
                // hazard script is often one level above the bare collider).
                var componentNames = new List<string>();
                foreach (Component c in go.GetComponents<Component>())
                {
                    if (c != null && !(c is Transform) && !(c is Collider))
                    {
                        componentNames.Add(DescribeComponent(c));
                    }
                }
                Transform parent = go.transform.parent;
                if (parent != null)
                {
                    foreach (Component c in parent.GetComponents<Component>())
                    {
                        if (c != null && !(c is Transform) && !(c is Collider))
                        {
                            componentNames.Add($"parent:{DescribeComponent(c)}");
                        }
                    }
                }
                if (componentNames.Count > 0)
                {
                    label += $" [{string.Join("/", componentNames)}]";
                }

                sb.Append(label).Append(", ");
            }

            // DisableBasedOnRunSettings carries a public disableIfSettingDisabled
            // field naming the exact RunSettings.SETTINGTYPE (e.g. Hazard_Urchins,
            // Hazard_Geysers, Hazard_Antlion - the same per-hazard settings enum
            // Antlion's own Start() reads) it's gated on - reading it directly
            // tells us conclusively which hazard a generically-named GameObject
            // (e.g. plain "Collider") actually is, no name-matching required.
            string DescribeComponent(Component c)
            {
                if (c is DisableBasedOnRunSettings disabler)
                {
                    return $"DisableBasedOnRunSettings(setting={disabler.disableIfSettingDisabled})";
                }
                return c.GetType().Name;
            }

            foreach (Collider collider in Physics.OverlapSphere(point, radiusUnits, ~0, QueryTriggerInteraction.Collide))
            {
                GameObject go = collider.gameObject;
                if (!seen.Add(go) || IsRecognized(go))
                {
                    continue;
                }
                Describe(go);
            }

            if (Time.time - _lastRendererScanTime >= RendererScanCooldownSeconds)
            {
                _lastRendererScanTime = Time.time;
                float radiusSq = radiusUnits * radiusUnits;
                foreach (Renderer renderer in UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
                {
                    GameObject go = renderer.gameObject;
                    if ((renderer.bounds.center - point).sqrMagnitude > radiusSq || !seen.Add(go) || IsRecognized(go))
                    {
                        continue;
                    }
                    Describe(go);
                }
            }

            if (sb.Length > 0)
            {
                log.LogInfo($"ItemPingDetector: unmatched nearby objects at {point}: {sb}");
            }
        }
    }
}
