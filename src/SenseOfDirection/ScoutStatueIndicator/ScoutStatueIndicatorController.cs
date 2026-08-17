using SenseOfDirection.Common;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
using SenseOfDirection.ItemPings;
using UnityEngine;

namespace SenseOfDirection.ScoutStatueIndicator
{
    /// <summary>
    /// Points a screen-space/compass indicator at the nearest still-unclaimed
    /// "Scout Statue" - one of 4 statues randomly placed across the first 4
    /// biomes, each holding its own Scout's Amulet (healing, infinite stamina,
    /// double jump, or clone - see <see cref="ItemPings.PingableProps"/>'
    /// <c>FakeItem</c> case). Picking up all 4 begins the secret ending.
    ///
    /// Not to be confused with <c>MapHandler.CurrentScoutStatue</c> (a
    /// <c>RespawnChest</c>, the revive chest at base camp) or the in-game
    /// <c>ScoutStatue</c> class (the single altar you carry amulets *to* -
    /// see the decompile's <c>Peak.ScoutStatue</c>) - this indicator is about
    /// the 4 statues you find the amulets *on*, which the game only exposes as
    /// a <c>FakeItem</c> stand-in (not a real <c>Item</c>, hence not otherwise
    /// pingable) until picked up.
    ///
    /// Identified by <c>Item.ItemTags.ScoutAmulet</c> on the FakeItem's
    /// <c>realItemPrefab</c> - the one tag the game itself uses to gate
    /// interacting with the altar (<c>Peak.ScoutStatue.IsConstantlyInteractable</c>),
    /// so it's guaranteed to single out exactly these 4 amulets and nothing
    /// else <c>FakeItem</c> stands in for (e.g. BingBong's medallion).
    ///
    /// Reuses <see cref="ItemPingDetector"/>'s own registry
    /// (<see cref="PingableRegistry.Props"/>) rather than a scene sweep of its
    /// own - <see cref="PingableRegistryPatches"/> already keeps it current for
    /// free, and every FakeItem in the level (amulets included) already lands
    /// in it via <see cref="PingableProps.TryResolve"/>. "Still holding an
    /// item" is <c>!FakeItem.pickedUp</c>; "exists in the current scene" is
    /// <c>gameObject.activeInHierarchy</c> - a statue in a segment that's been
    /// deactivated (already passed) reads as inactive the same way a dead
    /// player's body does elsewhere in this mod.
    ///
    /// "Nearest", not "next" in run order: nothing tracks which of the 4
    /// biomes-worth of statues is earliest in this particular run's layout, so
    /// nearest-by-distance is what actually gets a player to one - same
    /// reasoning (and the same persistent-widget/<see cref="IndicatorAnchor.IsActive"/>
    /// pattern, to avoid a frozen-marker flash on retarget) as
    /// <see cref="PirateCompass.PirateCompassLuggageIndicatorController"/>'s
    /// own "nearest unopened luggage" search.
    ///
    /// Reuses <see cref="ItemPingWidget"/> wholesale (name + distance + native
    /// item icon + off-screen arrow/compass marker) rather than a new widget
    /// type - the icon is the amulet's own real inventory art
    /// (<see cref="PingableProps.TryGetIcon"/>, already resolves a
    /// <c>FakeItem</c>'s <c>realItemPrefab</c> icon), per the maintainer's ask
    /// not to bother with a bespoke statue icon. Compass placement follows
    /// <c>Campfire/campfire-placement</c> (no separate setting), same as the
    /// campfire indicator.
    /// </summary>
    public class ScoutStatueIndicatorController : MonoBehaviour
    {
        private static ScoutStatueIndicatorController _instance;

        public static ScoutStatueIndicatorController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.ScoutStatueIndicatorController");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<ScoutStatueIndicatorController>();
                }
                return _instance;
            }
        }

        private FakeItem _trackedAmulet;
        private System.Func<string> _trackedDisplayName;
        private ItemPingWidget _widget;
        private bool _shouldShow;

        /// <summary>
        /// Drives the "pinged" color feedback - see <see cref="NotifyPinged"/>
        /// and <see cref="Common.PingFlashState"/>'s own doc comment.
        /// </summary>
        private readonly PingFlashState _pingFlash = new PingFlashState();

        private static readonly Safe.Context _ctxUpdateImpl =
            new Safe.Context("ScoutStatueIndicatorController.Update", failureLimit: 300);

        private void Update()
        {
            if (_ctxUpdateImpl.Disabled) return;
            try { UpdateImpl(); _ctxUpdateImpl.Succeeded(); }
            catch (System.Exception e) { _ctxUpdateImpl.Failed(e); }
        }

        private void UpdateImpl()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;

            if (!cfg.EnableScoutStatueIndicator.Value)
            {
                Teardown();
                return;
            }

            (FakeItem nearest, System.Func<string> displayName) = Character.localCharacter != null
                ? FindNearestUnclaimedAmulet()
                : (null, null);

            _shouldShow = nearest != null;
            if (nearest != null)
            {
                _trackedAmulet = nearest;
                _trackedDisplayName = displayName;
            }

            EnsureWidget();

            if (_trackedAmulet == null)
            {
                return;
            }

            string name = _trackedDisplayName != null ? _trackedDisplayName() : "Scout's Amulet";
            Sprite icon = PingableProps.TryGetIcon(_trackedAmulet);
            float distanceMeters = Vector3.Distance(
                CharacterPositions.LocalViewpoint(), _trackedAmulet.transform.position) * CharacterStats.unitsToMeters;

            _widget.Refresh(name, distanceMeters, ShouldShowName(), showDistance: true, icon);
            _widget.SetNameColor(_pingFlash.Evaluate(NameRestColor()));
        }

        /// <summary>
        /// Called from <see cref="ItemPingSpawner"/> when a ping lands on the
        /// currently-tracked amulet while <c>Campfire/enable-scout-statue-indicator</c>
        /// is on, in place of spawning a redundant <see cref="ItemPingHighlight"/>
        /// for the same thing this indicator is already showing.
        /// </summary>
        internal void NotifyPinged(Color color) => _pingFlash.Trigger(color);

        /// <summary>
        /// Whether the name label shows right now, on both the off-screen
        /// widget and the compass marker. When
        /// <c>hide-scout-statue-indicator-name</c> is off, always. When it's
        /// on (the default), the name is normally hidden entirely and only
        /// forced on for <see cref="PingFlashState.Active"/>'s whole window -
        /// see <see cref="NameRestColor"/> for how it still fades out smoothly
        /// rather than popping away the instant that window ends.
        /// </summary>
        private bool ShouldShowName()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            if (!cfg.HideScoutStatueIndicatorName.Value)
            {
                return true;
            }
            return _pingFlash.Active && cfg.ItemPingNameMode.Value == ItemPingNameMode.Always;
        }

        /// <summary>
        /// The color <see cref="PingFlashState.Evaluate"/> settles the name
        /// text at once a flash finishes. When the name is normally shown
        /// (<c>hide-scout-statue-indicator-name</c> off) that's opaque white -
        /// it stays visible, just un-tinted. When it's normally hidden
        /// (default), a fully transparent white instead - see that method's
        /// own doc comment: this is what makes the forced-visible window
        /// above fade out smoothly instead of disappearing the instant it ends.
        /// </summary>
        private static Color NameRestColor() =>
            Plugin.Instance.Cfg.HideScoutStatueIndicatorName.Value ? new Color(1f, 1f, 1f, 0f) : Color.white;

        /// <summary>Built once, the first time there's something to show; stays registered for the rest of the session - see this class's own doc comment for why.</summary>
        private void EnsureWidget()
        {
            if (_widget != null)
            {
                return;
            }

            _widget = ItemPingWidget.Rent(
                () => _trackedAmulet != null ? _trackedAmulet.transform.position : Vector3.zero,
                Color.white, enableArrow: true);
            _widget.Anchor.IsActive = () => _shouldShow && _trackedAmulet != null
                && !_trackedAmulet.pickedUp && _trackedAmulet.gameObject.activeInHierarchy;

            _widget.Anchor.CompassKind = CompassMarkerKind.ItemPing;

            // Same discrete-retarget reasoning as PirateCompassLuggageIndicatorController's
            // own CompassInstantHide: "nearest" can hand off to a different
            // statue in one frame (finishing one, or a segment deactivating),
            // and this avoids a stale frozen marker easing out at the old bearing.
            _widget.Anchor.CompassInstantHide = true;
            _widget.Anchor.GetPlacement = () => Plugin.Instance.Cfg.CampfirePlacement.Value;
            _widget.Anchor.GetCompassColor = () => _pingFlash.Evaluate(NameRestColor());

            // The compass marker's ItemPing kind is already unconditionally
            // text-tinted (see CompassMarkerWidget) - real item pings want
            // both name and distance colored, but this is this indicator's
            // own persistent anchor, not a real ping's, so its distance stays
            // white always, matching the off-screen widget's own SetNameColor
            // (name-only) split above.
            _widget.Anchor.SuppressCompassDistanceTint = () => true;
            _widget.Anchor.GetCompassLabel = () => ShouldShowName() && _trackedDisplayName != null ? _trackedDisplayName() : null;
            _widget.Anchor.GetCompassIcon = () => _trackedAmulet != null ? PingableProps.TryGetIcon(_trackedAmulet) : null;

            IndicatorManager.Instance.RegisterAnchor(_widget.Anchor);
        }

        /// <summary>
        /// Nearest still-on-its-statue Scout's Amulet, or null. No distance
        /// cap - there are only ever 4 of these in a whole run, so an
        /// unconditional scan of <see cref="PingableRegistry.Props"/> is cheap
        /// (that list holds every level prop, not just amulets, but it's
        /// already built and kept current for pinging - see this class's own
        /// doc comment).
        /// </summary>
        private static (FakeItem, System.Func<string>) FindNearestUnclaimedAmulet()
        {
            Vector3 origin = CharacterPositions.LocalViewpoint();
            FakeItem nearest = null;
            System.Func<string> nearestName = null;
            float nearestSqDistance = float.MaxValue;

            var props = PingableRegistry.Instance.Props;
            for (int i = 0; i < props.Count; i++)
            {
                PingableRegistry.PropTarget prop = props[i];
                if (!(prop.Behaviour is FakeItem fakeItem) || fakeItem == null)
                {
                    continue;
                }
                if (fakeItem.pickedUp || !fakeItem.gameObject.activeInHierarchy)
                {
                    continue;
                }
                if (fakeItem.realItemPrefab == null || !fakeItem.realItemPrefab.itemTags.HasFlag(Item.ItemTags.ScoutAmulet))
                {
                    continue;
                }

                float sqDistance = (fakeItem.transform.position - origin).sqrMagnitude;
                if (sqDistance < nearestSqDistance)
                {
                    nearestSqDistance = sqDistance;
                    nearest = fakeItem;
                    nearestName = prop.DisplayName;
                }
            }

            return (nearest, nearestName);
        }

        /// <summary>Only ever hit when the master switch itself is turned off - the ordinary "picked up"/no-amulet-left case is handled via <see cref="_shouldShow"/> instead, see this class's own doc comment.</summary>
        private void Teardown()
        {
            if (_widget != null)
            {
                IndicatorManager.Instance.UnregisterAnchor(_widget.Anchor);
                _widget = null;
            }
            _trackedAmulet = null;
            _trackedDisplayName = null;
            _shouldShow = false;
        }
    }
}
