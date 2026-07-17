using System.Collections.Generic;
using SenseOfDirection.Common;
using SenseOfDirection.ItemPings;
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

        /// <summary><see cref="Time.time"/> of the last successful ping, or negative infinity before the first one - so the very first press of a session is never itself blocked by the cooldown.</summary>
        private float _lastPingTime = float.NegativeInfinity;

        private void Update()
        {
            PluginConfig cfg = Plugin.Instance.Cfg;
            Character local = Character.localCharacter;

            if (!cfg.EnableLuggagePing.Value || local == null)
            {
                _keyWasDown = false;
                return;
            }

            // Deliberately not Input.GetKeyDown - see PlayerLabelController.
            // ComputeLabelsVisible's own doc comment for why: Unity's legacy
            // Input Manager can silently miss a key-down edge when another key
            // (e.g. a WASD movement key) is already held that same frame.
            KeyCode key = cfg.LuggagePingKey.Value;
            bool keyDownNow = key != KeyCode.None && Input.GetKey(key);
            if (keyDownNow && !_keyWasDown)
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
            _keyWasDown = keyDownNow;
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
                ItemPingSpawner.SpawnOrMerge(ClusterScratch, color, duration, enableArrow);
            }
        }
    }
}
