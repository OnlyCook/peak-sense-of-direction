using SenseOfDirection.Common;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
using SenseOfDirection.ItemPings;
using UnityEngine;

namespace SenseOfDirection.BelltowerIndicator
{
    /// <summary>
    /// Points a screen-space/compass indicator at the nearest not-yet-lit
    /// Belltower (a <c>GhostFire</c> - the pyre-with-a-bell Gloom safe zone;
    /// see <see cref="ItemPings.PingableProps"/>' own <c>GhostFire</c> case),
    /// same pattern as <see cref="ScoutStatueIndicator.ScoutStatueIndicatorController"/>.
    /// Belltowers only exist in the Gloom biome (added in PEAK 2.0's update),
    /// so no biome filtering is needed - a <c>GhostFire</c> instance simply
    /// never exists anywhere else.
    ///
    /// Reuses <see cref="ItemPingDetector"/>'s own registry
    /// (<see cref="PingableRegistry.Props"/>) rather than a scene sweep of its
    /// own, exactly like the scout statue indicator - <see cref="PingableRegistryPatches"/>
    /// already keeps it current, and every <c>GhostFire</c> in the level
    /// already lands in it via <see cref="PingableProps.TryResolve"/>, display
    /// name included (the pyre's own localized name, per that class' own
    /// <c>displayNameIndex</c> - not something this mod maintains a
    /// translation table for, so it's already correctly localized with no
    /// extra work here).
    ///
    /// "Nearest", not "next": a run's Gloom layout can place several
    /// Belltowers, with nothing tracking which one is "first" - same
    /// reasoning as the scout statue indicator's own nearest-amulet search.
    ///
    /// Reuses <see cref="ItemPingWidget"/> wholesale, same as the scout statue
    /// indicator, but with no native icon override (there's no bundled/native
    /// art for a Belltower) - it shows the mod's own generic item-ping
    /// diamond instead, and has no hide-name setting of its own (unlike the
    /// scout statue indicator - the maintainer's explicit ask): the name
    /// always shows whenever the indicator does. Compass placement follows
    /// <c>Campfire/campfire-placement</c> automatically (no separate setting),
    /// same as the campfire and scout statue indicators.
    /// </summary>
    public class BelltowerIndicatorController : MonoBehaviour
    {
        private static BelltowerIndicatorController _instance;

        public static BelltowerIndicatorController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.BelltowerIndicatorController");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<BelltowerIndicatorController>();
                }
                return _instance;
            }
        }

        private GhostFire _trackedBelltower;
        private System.Func<string> _trackedDisplayName;
        private ItemPingWidget _widget;
        private bool _shouldShow;

        /// <summary>
        /// Drives the "pinged" color feedback - see <see cref="NotifyPinged"/>
        /// and <see cref="Common.PingFlashState"/>'s own doc comment. Simpler
        /// than the campfire/scout statue indicators' own use of this: the
        /// name here is never normally hidden (no hide-name setting), so the
        /// rest color passed to <see cref="PingFlashState.Evaluate"/> is
        /// always opaque white - there's no "stay invisible outside a flash"
        /// case to fade towards transparent for.
        /// </summary>
        private readonly PingFlashState _pingFlash = new PingFlashState();

        private static readonly Safe.Context _ctxUpdateImpl =
            new Safe.Context("BelltowerIndicatorController.Update", failureLimit: 300);

        private void Update()
        {
            if (_ctxUpdateImpl.Disabled) return;
            try { UpdateImpl(); _ctxUpdateImpl.Succeeded(); }
            catch (System.Exception e) { _ctxUpdateImpl.Failed(e); }
        }

        private void UpdateImpl()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;

            if (!cfg.EnableBelltowerIndicator.Value)
            {
                Teardown();
                return;
            }

            (GhostFire nearest, System.Func<string> displayName) = Character.localCharacter != null
                ? FindNearestUnlitBelltower()
                : (null, null);

            _shouldShow = nearest != null;
            if (nearest != null)
            {
                _trackedBelltower = nearest;
                _trackedDisplayName = displayName;
            }

            EnsureWidget();

            if (_trackedBelltower == null)
            {
                return;
            }

            string name = _trackedDisplayName != null ? _trackedDisplayName() : "Belltower";
            float distanceMeters = Vector3.Distance(
                CharacterPositions.LocalViewpoint(), _trackedBelltower.transform.position) * CharacterStats.unitsToMeters;
            _widget.Refresh(name, distanceMeters, showName: true, showDistance: true);
            _widget.SetNameColor(_pingFlash.Evaluate(Color.white));
        }

        /// <summary>
        /// Called from <see cref="ItemPingSpawner"/> when a ping lands on the
        /// currently-tracked Belltower while
        /// <c>Campfire/enable-belltower-indicator</c> is on, in place of
        /// spawning a redundant <see cref="ItemPingHighlight"/> for the same
        /// thing this indicator is already showing.
        /// </summary>
        internal void NotifyPinged(Color color) => _pingFlash.Trigger(color);

        /// <summary>Built once, the first time there's something to show; stays registered for the rest of the session - see <see cref="ScoutStatueIndicator.ScoutStatueIndicatorController.EnsureWidget"/> for the same pattern's own rationale.</summary>
        private void EnsureWidget()
        {
            if (_widget != null)
            {
                return;
            }

            _widget = ItemPingWidget.Rent(
                () => _trackedBelltower != null ? _trackedBelltower.transform.position : Vector3.zero,
                Color.white, enableArrow: true);
            _widget.Anchor.IsActive = () => _shouldShow && _trackedBelltower != null
                && !_trackedBelltower.isLit && _trackedBelltower.gameObject.activeInHierarchy;

            _widget.Anchor.CompassKind = CompassMarkerKind.ItemPing;

            // Same discrete-retarget reasoning as the scout statue indicator's
            // own CompassInstantHide: "nearest" can hand off to a different
            // Belltower in one frame (one gets lit, or a segment deactivates).
            _widget.Anchor.CompassInstantHide = true;
            _widget.Anchor.GetPlacement = () => Plugin.Instance.Cfg.CampfirePlacement.Value;
            _widget.Anchor.GetCompassColor = () => _pingFlash.Evaluate(Color.white);

            // The compass marker's ItemPing kind is already unconditionally
            // text-tinted (see CompassMarkerWidget) - real item pings want
            // both name and distance colored, but this is this indicator's
            // own persistent anchor, not a real ping's, so its distance stays
            // white always, matching the off-screen widget's own SetNameColor
            // (name-only) split above.
            _widget.Anchor.SuppressCompassDistanceTint = () => true;
            _widget.Anchor.GetCompassLabel = () => _trackedDisplayName != null ? _trackedDisplayName() : null;

            IndicatorManager.Instance.RegisterAnchor(_widget.Anchor);
        }

        /// <summary>
        /// Nearest not-yet-lit Belltower, or null. No distance cap - a Gloom
        /// run has only a handful of these, so an unconditional scan of
        /// <see cref="PingableRegistry.Props"/> is cheap (already built and
        /// kept current for pinging - see this class's own doc comment).
        /// </summary>
        private static (GhostFire, System.Func<string>) FindNearestUnlitBelltower()
        {
            Vector3 origin = CharacterPositions.LocalViewpoint();
            GhostFire nearest = null;
            System.Func<string> nearestName = null;
            float nearestSqDistance = float.MaxValue;

            var props = PingableRegistry.Instance.Props;
            for (int i = 0; i < props.Count; i++)
            {
                PingableRegistry.PropTarget prop = props[i];
                if (!(prop.Behaviour is GhostFire ghostFire) || ghostFire == null)
                {
                    continue;
                }
                if (ghostFire.isLit || !ghostFire.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float sqDistance = (ghostFire.transform.position - origin).sqrMagnitude;
                if (sqDistance < nearestSqDistance)
                {
                    nearestSqDistance = sqDistance;
                    nearest = ghostFire;
                    nearestName = prop.DisplayName;
                }
            }

            return (nearest, nearestName);
        }

        /// <summary>Only ever hit when the master switch itself is turned off - the ordinary "lit"/no-Belltower-left case is handled via <see cref="_shouldShow"/> instead, see this class's own doc comment.</summary>
        private void Teardown()
        {
            if (_widget != null)
            {
                IndicatorManager.Instance.UnregisterAnchor(_widget.Anchor);
                _widget = null;
            }
            _trackedBelltower = null;
            _trackedDisplayName = null;
            _shouldShow = false;
        }
    }
}
