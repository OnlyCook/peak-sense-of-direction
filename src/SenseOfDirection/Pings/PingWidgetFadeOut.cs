using SenseOfDirection.Indicators;
using UnityEngine;

namespace SenseOfDirection.Pings
{
    /// <summary>
    /// Fades a <see cref="PingWidget"/> out over a short duration instead of
    /// snapping it away the instant its ping is destroyed - appearing stays
    /// instant (no fade-in, matching maintainer feedback that only
    /// disappearing should animate). Added onto the widget's own root
    /// GameObject by <see cref="PingWidgetLink"/>'s <c>OnDestroy</c>; the
    /// anchor stays registered for the duration (so the arrow keeps tracking
    /// the camera correctly while fading) and is only unregistered - which
    /// destroys the widget - once the fade completes.
    ///
    /// <see cref="IndicatorAnchor.OverlapSize"/> is zeroed out the instant the
    /// fade begins, opting this anchor out of label-overlap-avoidance for the
    /// rest of its life: a fading-out ping is about to disappear entirely, so
    /// it has no reading left to protect, and letting it keep contributing
    /// its last-known (now frozen, since nothing calls <c>Refresh</c> on it
    /// anymore) box was pushing a brand new, actually-visible ping's label
    /// away for the whole ~0.35s fade whenever the new ping landed within
    /// range of a just-replaced old one (e.g. re-pinging near a previous
    /// spot) - a real bug, not overlap resolution working as intended, since
    /// the "conflicting" label was already on its way out.
    /// </summary>
    public class PingWidgetFadeOut : MonoBehaviour
    {
        private const float FadeDurationSeconds = 0.35f;

        private CanvasGroup _canvasGroup;
        private IndicatorAnchor _anchor;
        private float _elapsed;

        public static void Begin(CanvasGroup canvasGroup, IndicatorAnchor anchor)
        {
            anchor.OverlapSize = Vector2.zero;
            var runner = canvasGroup.gameObject.AddComponent<PingWidgetFadeOut>();
            runner._canvasGroup = canvasGroup;
            runner._anchor = anchor;
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / FadeDurationSeconds);
            _canvasGroup.alpha = 1f - t;

            if (t >= 1f)
            {
                IndicatorManager.Instance.UnregisterAnchor(_anchor);
            }
        }
    }
}
