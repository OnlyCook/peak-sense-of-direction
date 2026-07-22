using System.Collections.Generic;
using SenseOfDirection.Common;
using SenseOfDirection.ItemPings;
using SenseOfDirection.Ui;
using UnityEngine;

namespace SenseOfDirection.LuggagePing
{
    /// <summary>
    /// Luggage-Ping/key (default T): highlights every unopened luggage within
    /// Luggage-Ping/radius-meters of the local player, using the same
    /// <see cref="ItemPingHighlight"/>/<see cref="ItemPingWidget"/> a real ping's
    /// item-ping detection already spawns - name/distance label, off-screen
    /// arrow, compass marker, all routed through Item-Pings/item-ping-placement
    /// same as any other item-ping highlight. Idea and default feel (a flat
    /// radius around the player, not a ping point) taken from the "Compass UI"
    /// mod's own suitcase-ping key, for players coming from that mod - see this
    /// mod's README credits. Deliberately never goes through
    /// <see cref="Pings.PointPingerPatches"/>/the game's own ping RPC: nothing
    /// here is sent to other players, so only the local player ever sees it.
    /// </summary>
    public class LuggagePingController : MonoBehaviour
    {
        private static LuggagePingController _instance;

        public static LuggagePingController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("SenseOfDirection.LuggagePingController");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<LuggagePingController>();
                }
                return _instance;
            }
        }

        /// <summary>Reused every trigger rather than allocated fresh - each call spawns/merges one single-target cluster at a time, see the loop in <see cref="TriggerPing"/>.</summary>
        private static readonly List<PingableTarget> ClusterScratch = new List<PingableTarget>();

        private bool _keyWasDown;

        /// <summary>
        /// Set the instant a rebind starts capturing input and held until the
        /// (possibly newly-assigned) key is next seen physically up. Needed
        /// because <see cref="KeyRebindControl"/> assigns the new
        /// <see cref="KeyCode"/> from the same keypress it captured - that key
        /// is still physically held down for at least the rest of that press,
        /// so without this, the frame capturing ends would otherwise see a
        /// fresh "just pressed" edge on the very key that was just bound and
        /// fire a ping immediately. Requiring one real release first (rather
        /// than just masking the trigger while <c>IsCapturing</c> is true)
        /// also covers the key having changed identity mid-rebind, which a
        /// same-key mask alone can't - <see cref="_keyWasDown"/> was tracking
        /// the *old* key's state, so a switch to a different key that happens
        /// to already be held would otherwise still read as a fresh edge too.
        /// </summary>
        private bool _suppressUntilKeyUp;

        /// <summary><see cref="Time.time"/> of the last successful ping, or negative infinity before the first one - so the very first press of a session is never itself blocked by the cooldown.</summary>
        private float _lastPingTime = float.NegativeInfinity;

        private void Update()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            Character local = Character.localCharacter;

            if (!cfg.EnableLuggagePing.Value || local == null)
            {
                _keyWasDown = false;
                _suppressUntilKeyUp = false;
                return;
            }

            // Deliberately not Input.GetKeyDown - see PlayerLabelController.
            // ComputeLabelsVisible's own doc comment for why: Unity's legacy
            // Input Manager can silently miss a key-down edge when another key
            // (e.g. a WASD movement key) is already held that same frame.
            KeyCode key = cfg.LuggagePingKey.Value;
            bool physicallyDown = key != KeyCode.None && Input.GetKey(key);

            if (KeyRebindControl.IsCapturing)
            {
                _keyWasDown = physicallyDown;
                _suppressUntilKeyUp = true;
                return;
            }

            if (_suppressUntilKeyUp)
            {
                _keyWasDown = physicallyDown;
                if (!physicallyDown)
                {
                    _suppressUntilKeyUp = false;
                }
                return;
            }

            if (physicallyDown && !_keyWasDown)
            {
                float cooldown = cfg.LuggagePingCooldownSeconds.Value;
                float remaining = cooldown - (Time.time - _lastPingTime);
                if (cooldown > 0f && remaining > 0f)
                {
                    // 0 disables the cooldown entirely (see the config
                    // description) - remaining is only checked once cooldown
                    // itself is positive, so a 0 setting can never show this.
                    LuggagePingCooldownIndicator.Instance.Show(remaining);
                }
                else
                {
                    _lastPingTime = Time.time;
                    TriggerPing(cfg, local);
                }
            }
            _keyWasDown = physicallyDown;
        }

        private static void TriggerPing(PluginConfig cfg, Character local)
        {
            Vector3 origin = CharacterPositions.LocalViewpoint();
            float radiusUnits = cfg.LuggagePingRadiusMeters.Value / CharacterStats.unitsToMeters;
            float radiusSq = radiusUnits * radiusUnits;
            Color color = local.refs.customization.PlayerColor;
            float duration = cfg.LuggagePingDurationSeconds.Value;
            bool enableArrow = cfg.EnableItemPingOffScreenIndicator.Value;

            foreach (Luggage luggage in Luggage.ALL_LUGGAGE)
            {
                // Same "not yet opened" filter ItemPingDetector/the Pirate's
                // Compass indicator both already rely on - a non-host client's
                // own ALL_LUGGAGE list can keep holding a reference to
                // already-opened luggage forever (see ItemPingDetector.cs's own
                // comment), so luggage.IsOpen is what actually matches reality.
                if (luggage == null || luggage.IsOpen || !luggage.gameObject.activeInHierarchy)
                {
                    continue;
                }
                if ((luggage.transform.position - origin).sqrMagnitude > radiusSq)
                {
                    continue;
                }

                Luggage capturedLuggage = luggage;
                ClusterScratch.Clear();
                ClusterScratch.Add(new PingableTarget(
                    capturedLuggage.gameObject,
                    () => capturedLuggage.transform.position,
                    () => capturedLuggage.GetName()));
                // Its own spawn pop (on top of the compass' usual alpha fade-in) -
                // a luggage ping can highlight a whole burst of luggage at once,
                // so the extra flourish helps each one read as newly-appeared
                // rather than just fading in identically to every other marker.
                ItemPingSpawner.SpawnOrMerge(ClusterScratch, color, duration, enableArrow, compassSpawnPop: true);
            }
        }
    }
}
