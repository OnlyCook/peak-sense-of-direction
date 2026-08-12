using UnityEngine;

namespace SenseOfDirection.ItemPings
{
    /// <summary>
    /// Things that are physically attached to a player and therefore have to be
    /// kept out of the pinging player's own way - both as the surface a ping's
    /// raycast lands on (<see cref="Pings.PointPingerPatches"/>'
    /// <c>TryGetPingHit</c> prefix) and as an item-ping target
    /// (<see cref="ItemPingDetector"/>).
    ///
    /// Vanilla's ping raycast only ever hit <c>TerrainMap</c>, so none of this
    /// could happen; widening it to the "Default" layer so items/luggage are
    /// directly pingable (the whole point of the hitbox assist) also put every
    /// player-attached prop in the ping's way, and PEAK 2.0's reworked
    /// character colliders made it far easier to hit them:
    ///
    /// - An <c>ArrowOnMe</c>/thorn sticking out of your own head sits directly
    ///   in front of your camera, so *every* ping landed on it - the ping
    ///   marker stuck to your face, and the item detection then swept its
    ///   radius around your own head, highlighting whatever your worn backpack
    ///   had strapped to it. Both <c>ArrowOnMe</c> (arrows) and plain thorns
    ///   are the same <c>ThornOnMe</c> component (its <c>type</c> field is the
    ///   only difference), so one check covers both. Ticks are a separate
    ///   component (<c>Bugfix</c>, attached to a leg) with the same problem.
    ///   These are ignored no matter whose body they're stuck in - they're
    ///   small props on a player, never something worth pinging - as are items
    ///   stuck into a player (a cactus ball, the <c>StickyItemComponent</c>
    ///   family).
    /// - A worn backpack (<c>BackpackOnBackVisuals</c> plus one real
    ///   <c>Item</c> per filled slot, parented to the wearer) hangs low enough
    ///   behind/below the player that simply pinging at your own feet landed on
    ///   it and highlighted the whole pack's contents.
    ///
    /// The worn-backpack rule is per-player, unlike the props above: only the
    /// *pinging* player's own worn backpack is excluded outright (you can never
    /// ping what you're wearing). Someone else's stays pingable, just no longer
    /// on the generous item radius - <see cref="ItemPingDetector"/> holds it to
    /// a direct-hit radius instead, so a teammate walking through your
    /// crosshair doesn't donate their inventory to a ping aimed past them.
    /// Items in a backpack *lying on the ground* are unaffected either way.
    /// </summary>
    internal static class PingIgnoreFilter
    {
        /// <summary>
        /// True for an <c>Item</c> the given ping should pretend doesn't exist:
        /// one strapped into that same player's own worn backpack, or stuck
        /// into any player at all.
        /// </summary>
        internal static bool IsIgnoredItem(Item item, Character pingingCharacter)
        {
            if (item == null)
            {
                return false;
            }

            if (TryGetBackpackWearer(item, out Character wearer))
            {
                return wearer != null && wearer == pingingCharacter;
            }

            StickyItemComponent sticky = item.GetComponentInChildren<StickyItemComponent>(includeInactive: true);
            return sticky != null && sticky.stuckToCharacter != null;
        }

        /// <summary>
        /// True when this item is strapped into a backpack somebody is
        /// currently wearing (as opposed to one lying on the ground, which is
        /// an ordinary pingable item like any other). <c>wearer</c> is the
        /// player wearing it, or null if that can't be resolved - in which case
        /// callers treat the item as another player's, i.e. still pingable but
        /// only on the direct-hit radius.
        /// </summary>
        internal static bool TryGetBackpackWearer(Item item, out Character wearer)
        {
            wearer = null;
            if (item == null || item.itemState != ItemState.InBackpack || item.backpackReference.IsNone)
            {
                return false;
            }

            BackpackReference reference = item.backpackReference.Value.Item2;
            if (reference.type != BackpackReference.BackpackType.Equipped)
            {
                return false;
            }

            // An Equipped reference's view is the wearer's own PhotonView
            // (BackpackReference.GetFromEquippedBackpack) - that's the same
            // route vanilla itself takes back to the character.
            if (reference.view != null)
            {
                wearer = reference.view.GetComponent<Character>();
            }
            return true;
        }

        /// <summary>
        /// True when a collider belongs to a player-attached prop this ping's
        /// raycast should pass straight through, to whatever's actually behind
        /// it - the same way it already passes through the player wearing it.
        /// Someone *else's* worn backpack deliberately isn't in that set: it
        /// stays a solid, directly-aimable surface, which is exactly what makes
        /// it pingable at all.
        ///
        /// One walk up the hierarchy rather than a <c>GetComponentInParent</c>
        /// per type: this runs for every hit of a ping's spherecast, which is
        /// on the "never stutter when pinging" path. The walk stops as soon as
        /// it reaches an <c>Item</c> root - whether that item is ignorable is
        /// entirely <see cref="IsIgnoredItem"/>'s call, and anything further up
        /// is that item's *current* holder/container, not part of the item
        /// itself (a held item is already handled separately, by
        /// <c>PointPingerPatches.IsLocalHandOrHeldItem</c>).
        /// </summary>
        internal static bool IsCharacterAttachment(Collider collider, Character pingingCharacter)
        {
            if (collider == null)
            {
                return false;
            }

            for (Transform t = collider.transform; t != null; t = t.parent)
            {
                if (t.TryGetComponent(out Item item))
                {
                    return IsIgnoredItem(item, pingingCharacter);
                }
                if (t.TryGetComponent(out ThornOnMe _) || t.TryGetComponent(out Bugfix _))
                {
                    return true;
                }
                if (t.TryGetComponent(out BackpackOnBackVisuals visuals))
                {
                    return visuals.character != null && visuals.character == pingingCharacter;
                }
            }

            return false;
        }
    }
}
