using SenseOfDirection.Common;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
using UnityEngine;

namespace SenseOfDirection.Pings
{
    /// <summary>
    /// Ties a <see cref="PingWidget"/>'s lifetime to its ping GameObject:
    /// attached directly onto the spawned <c>PointPing</c> instance so the
    /// widget starts fading out (see <see cref="PingWidgetFadeOut"/>) the
    /// instant the ping itself is destroyed (whether that's the usual 2s
    /// auto-destroy or anti-spam/visibility-cutoff short-circuiting it),
    /// with no separate timer to keep in sync.
    /// </summary>
    public class PingWidgetLink : MonoBehaviour
    {
        private PingWidget _widget;
        private Vector3 _worldPosition;
        private bool _showDistance;

        public static void Attach(GameObject pingInstance, Color color, bool enableArrow, bool showDistance, bool itemPingHandled)
        {
            var link = pingInstance.AddComponent<PingWidgetLink>();
            link._worldPosition = pingInstance.transform.position;
            link._showDistance = showDistance;
            // Same reasoning as the CompassKind suppression below: an item
            // ping already spawns its own off-screen arrow for this same
            // ping (ItemPingWidget, via ItemPingHighlight), so showing the
            // generic ping's arrow too just stacks two overlapping arrows
            // pointing at the same spot with different lifetimes.
            link._widget = PingWidget.Create(() => link._worldPosition, color, enableArrow && !itemPingHandled);
            link._widget.Refresh(0f, showDistance);

            // An item ping already got its own (more useful, named) compass
            // marker for this same ping - showing the generic ping's own ring
            // marker right on top of it would just be visual clutter, same
            // reasoning as suppressing its distance label above.
            link._widget.Anchor.CompassKind = itemPingHandled ? CompassMarkerKind.None : CompassMarkerKind.Ping;
            link._widget.Anchor.GetDisplayMode = () => Plugin.Instance.Cfg.PingsCompassDisplayMode.Value;
            link._widget.Anchor.GetCompassColor = () => color;

            IndicatorManager.Instance.RegisterAnchor(link._widget.Anchor);
        }

        private void Update()
        {
            if (Character.localCharacter == null)
            {
                return;
            }
            float distanceMeters = Vector3.Distance(CharacterPositions.LocalViewpoint(), _worldPosition) * CharacterStats.unitsToMeters;
            // Uses the showDistance decided at Attach time, not a fresh read
            // of ShowPingDistanceLabel here - PointPingerPatches computes
            // that value once by also factoring in whether an item ping is
            // showing its own distance for this same ping (redundant
            // otherwise); re-reading the raw config value every frame here
            // silently overwrote that suppression the very next frame.
            _widget.Refresh(distanceMeters, _showDistance);
        }

        private void OnDestroy()
        {
            if (_widget != null)
            {
                PingWidgetFadeOut.Begin(_widget.CanvasGroup, _widget.Anchor);
            }
        }
    }
}
