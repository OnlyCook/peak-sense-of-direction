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
    /// </summary>
    public class PingWidgetFadeOut : MonoBehaviour
    {
        private const float FadeDurationSeconds = 0.35f;

        private CanvasGroup _canvasGroup;
        private IndicatorAnchor _anchor;
        private float _elapsed;

        public static void Begin(CanvasGroup canvasGroup, IndicatorAnchor anchor)
        {
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
