using SenseOfDirection.Common;
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

        public static void Attach(GameObject pingInstance, Color color, bool enableArrow, bool showDistance)
        {
            var link = pingInstance.AddComponent<PingWidgetLink>();
            link._worldPosition = pingInstance.transform.position;
            link._showDistance = showDistance;
            link._widget = PingWidget.Create(() => link._worldPosition, color, enableArrow);
            link._widget.Refresh(0f, showDistance);
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
