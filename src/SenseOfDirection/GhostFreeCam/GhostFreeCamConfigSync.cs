using Photon.Pun;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace SenseOfDirection.GhostFreeCam
{
    /// <summary>
    /// Phase 6: the one place in this mod where a setting isn't purely local
    /// - <c>EnableGhostFreeCam</c>/<c>GhostFreeCamMaxDistanceMeters</c>/
    /// <c>GhostFreeCamUnlimitedRange</c> are host-controlled (see
    /// <c>PluginConfig.EnableGhostFreeCam</c>'s doc comment for why), which
    /// means every client needs to agree on the same effective values without
    /// this mod having any existing networking of its own to lean on.
    /// Implemented via Photon room custom properties rather than a bespoke
    /// RPC: only the master client ever writes them (<see cref="Tick"/>,
    /// polled once a frame from <c>GhostFreeCamPatches</c>' own
    /// <c>MainCameraMovement.LateUpdate</c> postfix - cheap enough not to
    /// need its own timer, and means publishing reacts to config changes,
    /// room joins, and host migration within a single frame with no extra
    /// event wiring), every other client just reads whatever's currently
    /// published (<see cref="TryGetEffective"/>). A missing property (host
    /// doesn't have this mod, or hasn't published yet) reads as fully
    /// disabled rather than falling back to the local client's own value -
    /// the whole point is that a client alone can't turn this on for itself.
    /// </summary>
    internal static class GhostFreeCamConfigSync
    {
        private const string EnabledKey = "SoD.GFC.Enabled";
        private const string MaxDistanceKey = "SoD.GFC.MaxDistance";
        private const string UnlimitedKey = "SoD.GFC.Unlimited";

        private static bool _publishedOnce;
        private static object _lastPublishedRoom;
        private static bool _lastEnabled;
        private static float _lastMaxDistance;
        private static bool _lastUnlimited;

        /// <summary>
        /// Master-client-only: (re)publishes this client's own config values
        /// to the room whenever they've changed, a new room was joined, or
        /// this client just became master (host migration) - all three cases
        /// collapse to the same "does the room's current state already match
        /// what we'd publish" check.
        /// </summary>
        internal static void Tick()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            {
                _publishedOnce = false;
                return;
            }

            PluginConfig cfg = Plugin.Instance.Cfg;
            bool enabled = cfg.EnableGhostFreeCam.Value;
            float maxDistance = cfg.GhostFreeCamMaxDistanceMeters.Value;
            bool unlimited = cfg.GhostFreeCamUnlimitedRange.Value;

            bool roomChanged = !ReferenceEquals(_lastPublishedRoom, PhotonNetwork.CurrentRoom);
            bool valuesChanged = !_publishedOnce
                || enabled != _lastEnabled
                || maxDistance != _lastMaxDistance
                || unlimited != _lastUnlimited;

            if (!roomChanged && !valuesChanged)
            {
                return;
            }

            var props = new Hashtable
            {
                [EnabledKey] = enabled,
                [MaxDistanceKey] = maxDistance,
                [UnlimitedKey] = unlimited,
            };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);

            _publishedOnce = true;
            _lastPublishedRoom = PhotonNetwork.CurrentRoom;
            _lastEnabled = enabled;
            _lastMaxDistance = maxDistance;
            _lastUnlimited = unlimited;
        }

        /// <summary>
        /// The master client always trusts its own local config directly
        /// (no round-trip through room properties, so there's no first-frame
        /// race against <see cref="Tick"/>'s own publish). Every other
        /// client reads whatever's currently published, treating a missing/
        /// malformed property as fully disabled.
        /// </summary>
        internal static bool TryGetEffective(out float maxDistanceMeters, out bool unlimited)
        {
            maxDistanceMeters = 0f;
            unlimited = false;

            if (!PhotonNetwork.InRoom)
            {
                return false;
            }

            PluginConfig cfg = Plugin.Instance.Cfg;
            if (PhotonNetwork.IsMasterClient)
            {
                if (!cfg.EnableGhostFreeCam.Value)
                {
                    return false;
                }
                maxDistanceMeters = cfg.GhostFreeCamMaxDistanceMeters.Value;
                unlimited = cfg.GhostFreeCamUnlimitedRange.Value;
                return true;
            }

            Hashtable props = PhotonNetwork.CurrentRoom.CustomProperties;
            if (props == null
                || !props.TryGetValue(EnabledKey, out object enabledObj)
                || !(enabledObj is bool enabledVal)
                || !enabledVal)
            {
                return false;
            }

            unlimited = props.TryGetValue(UnlimitedKey, out object unlimitedObj) && unlimitedObj is bool u && u;
            maxDistanceMeters = props.TryGetValue(MaxDistanceKey, out object maxDistObj) && maxDistObj is float f ? f : 0f;
            return true;
        }
    }
}
