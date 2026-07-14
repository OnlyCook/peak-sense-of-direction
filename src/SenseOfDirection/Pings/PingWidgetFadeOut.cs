using System.Collections.Generic;
using SenseOfDirection.Indicators;
using UnityEngine;

namespace SenseOfDirection.Pings
{
    /// <summary>
    /// Fades a <see cref="PingWidget"/> (or an <c>ItemPingWidget</c>) out over a
    /// short duration instead of snapping it away the instant its ping is
    /// destroyed - appearing stays instant (no fade-in, matching maintainer
    /// feedback that only disappearing should animate). The anchor stays
    /// registered for the duration (so the arrow keeps tracking the camera
    /// correctly while fading) and is only unregistered - which hands the widget
    /// back to its pool - once the fade completes.
    ///
    /// That final unregister is also the *only* thing that ever takes a finished
    /// ping off the compass, which is why this is one standalone always-running
    /// driver rather than a component sitting on each widget, which is what it
    /// used to be. <see cref="IndicatorManager"/> deactivates a widget's GameObject
    /// outright when its placement is <c>CompassOnly</c> - there is no
    /// edge-of-screen indicator to show in that mode - and an inactive GameObject's
    /// <c>Update</c> never runs. So the fade never advanced, never finished, and
    /// never unregistered the anchor: the ping's compass marker stayed on the
    /// compass for the rest of the run, and only in that one placement mode. A
    /// driver that is always active cannot be switched off by the thing it is
    /// trying to clean up.
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

        private class Fade
        {
            internal CanvasGroup CanvasGroup;
            internal IndicatorAnchor Anchor;
            internal float Elapsed;
        }

        private static PingWidgetFadeOut _driver;

        private readonly List<Fade> _fades = new List<Fade>();

        private static PingWidgetFadeOut Driver
        {
            get
            {
                if (_driver == null)
                {
                    var go = new GameObject("SenseOfDirection.PingWidgetFadeOut");
                    DontDestroyOnLoad(go);
                    _driver = go.AddComponent<PingWidgetFadeOut>();
                }
                return _driver;
            }
        }

        public static void Begin(CanvasGroup canvasGroup, IndicatorAnchor anchor)
        {
            anchor.OverlapSize = Vector2.zero;
            Driver.Add(canvasGroup, anchor);
        }

        /// <summary>
        /// Re-arms the fade already running for this widget rather than starting a
        /// second one: ping widgets are pooled, so one CanvasGroup fades out once
        /// per ping it is ever rented for, and two fades racing over a single widget
        /// would have the loser unregister an anchor the winner had already handed
        /// back to the pool.
        /// </summary>
        private void Add(CanvasGroup canvasGroup, IndicatorAnchor anchor)
        {
            foreach (Fade existing in _fades)
            {
                if (existing.CanvasGroup == canvasGroup)
                {
                    existing.Anchor = anchor;
                    existing.Elapsed = 0f;
                    return;
                }
            }

            _fades.Add(new Fade { CanvasGroup = canvasGroup, Anchor = anchor, Elapsed = 0f });
        }

        private void Update()
        {
            for (int i = _fades.Count - 1; i >= 0; i--)
            {
                Fade fade = _fades[i];
                fade.Elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(fade.Elapsed / FadeDurationSeconds);

                // Null if its widget was torn down some other way (a scene change,
                // say). The anchor still has to come off the compass, so the fade
                // runs to its end regardless and only the alpha is skipped.
                if (fade.CanvasGroup != null)
                {
                    fade.CanvasGroup.alpha = 1f - t;
                }

                if (t < 1f)
                {
                    continue;
                }

                // Dropped before the unregister, not after: that hands a pooled
                // widget straight back to its pool, where it can be rented again in
                // this very frame - and a fade still holding it would then be fading
                // out a widget that now belongs to somebody else's ping.
                _fades.RemoveAt(i);
                IndicatorManager.Instance.UnregisterAnchor(fade.Anchor);
            }
        }
    }
}
