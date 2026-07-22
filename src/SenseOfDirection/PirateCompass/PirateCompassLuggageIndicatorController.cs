using SenseOfDirection.Common;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
using SenseOfDirection.ItemPings;
using UnityEngine;

namespace SenseOfDirection.PirateCompass
{
    /// <summary>
    /// ISSUES.md: the in-game Pirate's Compass (<c>CompassPointer.CompassType.Pirate</c>,
    /// see <c>Compass.CompassManager.GetHeldCompassPointer</c>) already makes
    /// <c>Compass/display-mode</c>'s Holding Item level show the compass tape
    /// while it's held - it's just another <c>CompassPointer</c>-bearing item
    /// as far as that check goes. But the tape itself is a fixed north-relative
    /// heading strip;
    /// it has no way to represent "point at the nearest unopened luggage", which
    /// is the one thing a Pirate's Compass actually does (its own in-game needle
    /// already does this, per the decompile's <c>CompassPointer.UpdateHeadingPirate</c>
    /// - nearest by <c>Vector3.Distance</c> against <c>Luggage.ALL_LUGGAGE</c>,
    /// no distance cap). This reimplements that same "nearest unopened luggage"
    /// query, but surfaces it as a real screen-space/compass indicator (reusing
    /// <see cref="ItemPings.ItemPingWidget"/> wholesale - it already draws a
    /// name+distance label with an off-screen arrow and a compass marker, and
    /// there's nothing luggage-ping-specific it would need to do differently),
    /// so the direction is actually legible instead of only being conveyed by a
    /// wobbling 3D needle on the held item model.
    ///
    /// The widget/anchor is built once and stays registered for the rest of
    /// the session; showing/hiding it (holstering the compass, no unopened
    /// luggage left) is done purely through <see cref="IndicatorAnchor.IsActive"/>,
    /// never by unregistering and re-registering a fresh anchor - the same
    /// pattern <see cref="CampfireIndicator.CampfireIndicatorController"/>
    /// already uses to keep a hidden-then-reshown indicator's widget alive.
    /// <c>Compass.CompassManager.UpdateMarkers</c> only recomputes a marker's
    /// tape position while its anchor is currently active - while inactive it
    /// just fades the marker's alpha towards 0 and otherwise leaves it frozen
    /// wherever it last was. Tearing the anchor down and building a new one on
    /// every holster/re-equip (an earlier version of this controller did
    /// exactly that) made that frozen old marker linger fading out at its
    /// stale bearing while a *second*, freshly-registered one popped in
    /// already correct - read in-game as the indicator briefly appearing at
    /// the wrong spot (wherever the camera happened to be facing when the
    /// compass was last put away) before "catching up" to the right one.
    /// Keeping one persistent anchor removes the *duplicate*-marker half of
    /// that bug, but not the whole thing by itself: this anchor's own
    /// gradual fade-out (still frozen-in-place while easing towards 0) can
    /// still be caught mid-fade by a fast holster/re-equip, showing that
    /// same single marker at its stale bearing for a moment. See
    /// <see cref="IndicatorAnchor.CompassInstantHide"/> (set true below) for
    /// the other half of the fix - it snaps this anchor's marker straight to
    /// alpha 0 the instant it goes inactive instead of easing it out, so
    /// there is no partially-faded frame left to catch mid-transition.
    /// Keeping one persistent anchor still matters on its own: the instant it
    /// reactivates, <c>UpdateMarkers</c> recomputes its position fresh in that
    /// same frame before ever fading its alpha back up, so there is no stale
    /// position to see in the first place.
    /// </summary>
    public class PirateCompassLuggageIndicatorController : MonoBehaviour
    {
        private static PirateCompassLuggageIndicatorController _instance;

        public static PirateCompassLuggageIndicatorController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.PirateCompassLuggageIndicatorController");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<PirateCompassLuggageIndicatorController>();
                }
                return _instance;
            }
        }

        private Luggage _trackedLuggage;
        private ItemPingWidget _widget;
        private bool _shouldShow;

        private void Update()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;

            if (!cfg.EnablePirateCompassLuggageIndicator.Value)
            {
                Teardown();
                return;
            }

            bool holdingPirateCompass = Character.localCharacter != null && IsHoldingPirateCompass();
            Luggage nearest = holdingPirateCompass ? FindNearestUnopenedLuggage() : null;
            _shouldShow = nearest != null;
            if (nearest != null)
            {
                _trackedLuggage = nearest;
            }

            EnsureWidget(cfg);

            if (_trackedLuggage == null)
            {
                return;
            }

            string displayName = _trackedLuggage.GetName().ToUpperInvariant();
            float distanceMeters = Vector3.Distance(CharacterPositions.LocalViewpoint(), _trackedLuggage.transform.position) * CharacterStats.unitsToMeters;
            _widget.Refresh(displayName, distanceMeters, cfg.ShowPirateCompassLuggageName.Value, cfg.ShowPirateCompassLuggageDistance.Value);
        }

        /// <summary>Built once, the first time there's something to show; stays registered for the rest of the session (see this class's own doc comment for why).</summary>
        private void EnsureWidget(PluginConfig cfg)
        {
            if (_widget != null)
            {
                return;
            }

            _widget = ItemPingWidget.Rent(() => _trackedLuggage != null ? _trackedLuggage.transform.position : Vector3.zero,
                Color.white, cfg.EnablePirateCompassLuggageOffScreenIndicator.Value);
            _widget.Anchor.IsActive = () => _shouldShow && _trackedLuggage != null
                && !_trackedLuggage.IsOpen && _trackedLuggage.gameObject.activeInHierarchy;

            _widget.Anchor.CompassKind = CompassMarkerKind.ItemPing;

            // Holstering/re-equipping the compass is a single discrete action,
            // not a gradual "walking out of view" - see IndicatorAnchor.
            // CompassInstantHide's own doc comment for why that specifically
            // makes this mechanic prone to showing a stale frozen position if
            // the ordinary gradual compass fade were used instead.
            _widget.Anchor.CompassInstantHide = true;
            _widget.Anchor.GetPlacement = () => Plugin.Instance.Cfg.PirateCompassLuggagePlacement.Value;
            _widget.Anchor.GetCompassColor = () => Color.white;
            _widget.Anchor.GetCompassLabel = () => _trackedLuggage != null && Plugin.Instance.Cfg.ShowPirateCompassLuggageName.Value
                ? _trackedLuggage.GetName().ToUpperInvariant()
                : null;

            IndicatorManager.Instance.RegisterAnchor(_widget.Anchor);
        }

        private static bool IsHoldingPirateCompass()
        {
            CompassPointer pointer = CompassManager.GetHeldCompassPointer();
            return pointer != null && pointer.compassType == CompassPointer.CompassType.Pirate;
        }

        /// <summary>
        /// Same "not yet opened" check <see cref="ItemPings.ItemPingDetector"/>
        /// already relies on (<c>luggage.IsOpen</c>, not just presence in
        /// <c>Luggage.ALL_LUGGAGE</c>) - a non-host client's own ALL_LUGGAGE list
        /// keeps holding a reference to already-opened luggage forever in the
        /// common "opens with items" case (see that class's own comment), so
        /// filtering on <c>IsOpen</c> is what actually matches reality on
        /// clients. No distance cap, matching the native Pirate Compass needle's
        /// own unlimited-range behavior (<c>CompassPointer.UpdateHeadingPirate</c>).
        /// </summary>
        private static Luggage FindNearestUnopenedLuggage()
        {
            Vector3 origin = CharacterPositions.LocalViewpoint();
            Luggage nearest = null;
            float nearestSqDistance = float.MaxValue;
            foreach (Luggage luggage in Luggage.ALL_LUGGAGE)
            {
                if (luggage == null || luggage.IsOpen || !luggage.gameObject.activeInHierarchy)
                {
                    continue;
                }
                float sqDistance = (luggage.transform.position - origin).sqrMagnitude;
                if (sqDistance < nearestSqDistance)
                {
                    nearestSqDistance = sqDistance;
                    nearest = luggage;
                }
            }
            return nearest;
        }

        /// <summary>Only ever hit when the master switch itself is turned off - the ordinary holster/no-luggage-left case is handled via <see cref="_shouldShow"/> instead, see this class's own doc comment.</summary>
        private void Teardown()
        {
            if (_widget != null)
            {
                IndicatorManager.Instance.UnregisterAnchor(_widget.Anchor);
                _widget = null;
            }
            _trackedLuggage = null;
            _shouldShow = false;
        }
    }
}
