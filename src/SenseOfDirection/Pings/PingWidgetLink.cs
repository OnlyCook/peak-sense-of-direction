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

        public static void Attach(GameObject pingInstance, Color color, bool enableArrow, bool showDistance)
        {
            var link = pingInstance.AddComponent<PingWidgetLink>();
            link._worldPosition = pingInstance.transform.position;
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
            float distanceMeters = Vector3.Distance(Character.localCharacter.Head, _worldPosition) * CharacterStats.unitsToMeters;
            _widget.Refresh(distanceMeters, Plugin.Instance.Cfg.ShowPingDistanceLabel.Value);
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
