using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace SenseOfDirection.GhostFreeCam
{
    /// <summary>
    /// Vanilla's own <c>PlayerGhost.Update</c> (decompiled) positions the
    /// ghost body other clients see purely from <c>m_target.Center</c> (the
    /// spectated player) offset by <c>CharacterData.spectateZoom</c> (a
    /// networked scroll-zoom scalar clamped to 1-5 units, ~a couple meters)
    /// - there is no existing networked field that lets a ghost's visible
    /// position stray any further than that from whoever it's spectating,
    /// so a free-camming ghost scouting tens of meters away has no way to
    /// actually show up there for other clients without new networking of
    /// some kind.
    ///
    /// Implemented as a raw Photon custom event (<c>PhotonNetwork.RaiseEvent</c>)
    /// rather than a <c>[PunRPC]</c> on <c>PlayerGhost</c> itself, since
    /// Harmony can't add a new RPC-callable method to a vanilla class - a
    /// custom event needs no PhotonView/RPC table at all, just a shared
    /// event code both sides agree on. Sent unreliable and throttled
    /// (<see cref="SendIntervalSeconds"/>), same idea as vanilla's own
    /// spectate ghost/character position sync (a few snapshots a second,
    /// smoothed out on the receiving end) rather than a full-rate stream -
    /// <see cref="TryGetPose"/> keeps the last two received snapshots per
    /// sender and linearly interpolates between them based on how far
    /// through the gap between their timestamps "now" is, exactly like a
    /// standard networked-position interpolation buffer (as opposed to a
    /// damped follow/lerp-toward-latest, which would visibly lag or
    /// overshoot depending on framerate/ping) - about one send interval of
    /// latency in exchange for perfectly smooth, framerate-independent
    /// movement between the sparse RPCs. A pose is treated as stale after
    /// <see cref="StaleAfterSeconds"/> with no update, so a receiving
    /// client automatically falls back to vanilla's own tightly-anchored
    /// formula the moment the sender disengages free-cam (or disconnects)
    /// - no explicit "stopped free-camming" message needed.
    /// </summary>
    internal static class GhostFreeCamPoseSync
    {
        // Must be < 200 - PhotonNetwork.RaiseEvent silently refuses codes
        // 200 and up (reserved for PUN's own internal events).
        private const byte EventCode = 199;

        private const float SendIntervalSeconds = 0.1f;
        private const float StaleAfterSeconds = 0.5f;

        private class PoseSample
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public float ReceivedAtRealtime;
        }

        /// <summary>The two most recent samples for one sender - <see cref="TryGetPose"/> interpolates between them rather than snapping to <see cref="Latest"/> directly.</summary>
        private class PoseBuffer
        {
            public PoseSample Previous;
            public PoseSample Latest;
        }

        private static readonly Dictionary<int, PoseBuffer> BuffersByActorNumber = new Dictionary<int, PoseBuffer>();
        private static readonly RaiseEventOptions SendToOthersOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };

        private static float _lastSendTime = float.NegativeInfinity;
        private static bool _callbackRegistered;
        private static EventCallback _eventCallback;

        private class EventCallback : IOnEventCallback
        {
            /// <summary>
            /// Registered globally via <c>PhotonNetwork.AddCallbackTarget</c>,
            /// which means this runs for <em>every</em> Photon event the
            /// client receives, not just ours - so it sits directly in the
            /// game's own network dispatch path. An exception escaping here
            /// doesn't just lose one ghost pose: it aborts the dispatch of
            /// that event, and anything else Photon was going to hand to
            /// callbacks after us never gets delivered. That is a plausible
            /// route from "a cosmetic mod threw" to "networked features
            /// (voice included) stop working for this player", so the whole
            /// body is guarded and the failure mode is simply "this pose
            /// sample is dropped".
            /// </summary>
            void IOnEventCallback.OnEvent(EventData photonEvent)
            {
                // Cheapest possible reject first, outside the guard: this is
                // called for every event on the wire and the overwhelming
                // majority are not ours.
                if (photonEvent.Code != EventCode)
                {
                    return;
                }

                if (ReceiveGuard.Disabled)
                {
                    return;
                }
                try
                {
                    HandlePoseEvent(photonEvent);
                    ReceiveGuard.Succeeded();
                }
                catch (System.Exception e)
                {
                    ReceiveGuard.Failed(e);
                }
            }

            private static readonly Common.Safe.Context ReceiveGuard =
                new Common.Safe.Context("GhostFreeCamPoseSync.OnEvent (receiving a ghost pose)", failureLimit: 300);

            private static void HandlePoseEvent(EventData photonEvent)
            {
                if (!(photonEvent.CustomData is object[] payload) || payload.Length < 2)
                {
                    return;
                }
                if (!(payload[0] is Vector3 position) || !(payload[1] is Quaternion rotation))
                {
                    return;
                }

                var sample = new PoseSample
                {
                    Position = position,
                    Rotation = rotation,
                    ReceivedAtRealtime = Time.unscaledTime,
                };

                if (BuffersByActorNumber.TryGetValue(photonEvent.Sender, out PoseBuffer buffer))
                {
                    buffer.Previous = buffer.Latest;
                    buffer.Latest = sample;
                }
                else
                {
                    // First sample ever for this sender - nothing to
                    // interpolate from yet, so seed both ends with it
                    // (TryGetPose's span works out to 0, t=1, no jump).
                    BuffersByActorNumber[photonEvent.Sender] = new PoseBuffer { Previous = sample, Latest = sample };
                }
            }
        }

        internal static void EnsureRegistered()
        {
            if (_callbackRegistered)
            {
                return;
            }

            // Only latch _callbackRegistered on success, so a failure here
            // (Photon not initialised yet, say) is retried next frame rather
            // than leaving pose sync permanently unregistered.
            bool ok = Common.Safe.Run(
                "GhostFreeCamPoseSync.EnsureRegistered",
                () =>
                {
                    _eventCallback = new EventCallback();
                    PhotonNetwork.AddCallbackTarget(_eventCallback);
                },
                failureLimit: 60);

            _callbackRegistered = ok;
        }

        internal static void SendPose(Vector3 position, Quaternion rotation)
        {
            // Guarded because it writes to the wire: a throw out of RaiseEvent
            // would propagate up through our LateUpdate hook into vanilla's
            // MainCameraMovement.LateUpdate. Ghost pose sync is purely
            // cosmetic (it's what lets others see where a free-cam ghost is),
            // so dropping a send is always the right trade.
            if (SendGuard.Disabled)
            {
                return;
            }
            try
            {
                SendPoseImpl(position, rotation);
                SendGuard.Succeeded();
            }
            catch (System.Exception e)
            {
                SendGuard.Failed(e);
            }
        }

        private static readonly Common.Safe.Context SendGuard =
            new Common.Safe.Context("GhostFreeCamPoseSync.SendPose", failureLimit: 300);

        private static void SendPoseImpl(Vector3 position, Quaternion rotation)
        {
            if (!PhotonNetwork.InRoom)
            {
                return;
            }
            float now = Time.unscaledTime;
            if (now - _lastSendTime < SendIntervalSeconds)
            {
                return;
            }
            _lastSendTime = now;

            object[] payload = { position, rotation };
            PhotonNetwork.RaiseEvent(EventCode, payload, SendToOthersOptions, SendOptions.SendUnreliable);
        }

        internal static bool TryGetPose(int actorNumber, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = default;
            if (!BuffersByActorNumber.TryGetValue(actorNumber, out PoseBuffer buffer))
            {
                return false;
            }
            if (Time.unscaledTime - buffer.Latest.ReceivedAtRealtime > StaleAfterSeconds)
            {
                return false;
            }

            // Rendered one send interval in the past, not at raw "now" -
            // otherwise t would already be ~1 (i.e. sitting right on
            // Latest, no visible interpolation happening at all) for
            // almost this entire gap, since "now" is normally past
            // Latest's own receive time by the time this is called. This
            // is the standard fixed-delay interpolation-buffer technique:
            // always render slightly behind so there are always two real
            // samples on either side of the render time to blend between.
            float renderTime = Time.unscaledTime - SendIntervalSeconds;
            float span = buffer.Latest.ReceivedAtRealtime - buffer.Previous.ReceivedAtRealtime;
            float t = span > 0f ? Mathf.Clamp01((renderTime - buffer.Previous.ReceivedAtRealtime) / span) : 1f;
            position = Vector3.Lerp(buffer.Previous.Position, buffer.Latest.Position, t);
            rotation = Quaternion.Slerp(buffer.Previous.Rotation, buffer.Latest.Rotation, t);
            return true;
        }
    }
}
