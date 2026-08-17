using UnityEngine;

namespace SenseOfDirection.Common
{
    /// <summary>
    /// Shared "this fixed indicator's own name/distance labels flash the
    /// pinging player's color to show it was just pinged" state, used by
    /// <see cref="CampfireIndicator.CampfireIndicatorController"/> and
    /// <see cref="ScoutStatueIndicator.ScoutStatueIndicatorController"/>.
    ///
    /// Both of those already show an always-on indicator for something
    /// <see cref="ItemPings.ItemPingDetector"/> would otherwise also detect
    /// and highlight on its own (the current campfire, an unclaimed scout
    /// amulet) - so <see cref="ItemPings.ItemPingSpawner"/> intercepts a ping
    /// that lands on either one, skips spawning the redundant
    /// <see cref="ItemPings.ItemPingHighlight"/>, and triggers this instead:
    /// the existing indicator's own labels flash the pinging player's color
    /// rather than a second, overlapping indicator appearing next to it.
    /// </summary>
    internal sealed class PingFlashState
    {
        /// <summary>How long the flash holds at full color before starting to fade.</summary>
        private const float HoldSeconds = 1.5f;

        /// <summary>How long the fade back to the indicator's normal color takes, once it starts.</summary>
        private const float FadeSeconds = 1f;

        private float _startTime = float.NegativeInfinity;
        private Color _color = Color.white;

        internal void Trigger(Color color)
        {
            _color = color;
            _startTime = Time.unscaledTime;
        }

        /// <summary>
        /// Whether the flash is still in its window (hold or fade) - drives
        /// the color returned by <see cref="Evaluate"/>, and is also what a
        /// caller should gate a normally-hidden label's *visibility* on (not
        /// just its color) - see that method's own doc comment on why.
        /// </summary>
        internal bool Active => Time.unscaledTime - _startTime <= HoldSeconds + FadeSeconds;

        /// <summary>
        /// The color to draw with right now: the flash color while holding,
        /// then either eased towards <paramref name="restColor"/> (while
        /// fading) or faded to fully transparent (see below), depending on
        /// what <paramref name="restColor"/> itself is - and
        /// <paramref name="restColor"/> outright once the window is over.
        ///
        /// Two distinct cases, both driven by <paramref name="restColor"/>'s
        /// own alpha:
        /// <list type="bullet">
        /// <item><b>Opaque rest color</b> (a label that's normally shown, just
        /// un-tinted once the flash is over) - RGB (and alpha, though it never
        /// actually moves since both ends are opaque) blend smoothly from the
        /// flash color to <paramref name="restColor"/> across the fade.</item>
        /// <item><b>Transparent rest color</b> (a label that's normally
        /// hidden, and should only ever be forced visible by a flash) - RGB
        /// stays pinned to the flash color for the *entire* window instead of
        /// blending toward white, and only alpha eases down to 0. Blending
        /// RGB too here would visibly desaturate the label towards white
        /// *while it's still fully opaque and on screen* before it vanishes -
        /// reads as "changed color, sat there, then popped away" rather than
        /// the plain fade-out every other transient element in this mod uses
        /// (<c>PingWidgetFadeOut</c>/<c>ItemPingHighlight</c>'s own
        /// <c>CanvasGroup</c> alpha fades). Only once alpha has eased all the
        /// way to 0 - i.e. the label is already invisible - does the color
        /// actually become white (the same frame the window ends and this
        /// method starts returning <paramref name="restColor"/> outright), so
        /// nothing shows a color change the player could actually see.</item>
        /// </list>
        /// Since <see cref="Active"/> reads the same elapsed time as this
        /// method, alpha is already exactly 0 by the last frame a caller still
        /// has the label active in the hidden case, so deactivating it the
        /// frame after is invisible - no pop.
        /// </summary>
        internal Color Evaluate(Color restColor)
        {
            float elapsed = Time.unscaledTime - _startTime;
            if (elapsed < 0f || elapsed > HoldSeconds + FadeSeconds)
            {
                return restColor;
            }
            if (elapsed <= HoldSeconds)
            {
                return _color;
            }
            float t = Mathf.InverseLerp(HoldSeconds, HoldSeconds + FadeSeconds, elapsed);
            if (restColor.a <= 0f)
            {
                Color faded = _color;
                faded.a = Mathf.Lerp(_color.a, 0f, t);
                return faded;
            }
            return Color.Lerp(_color, restColor, t);
        }
    }
}
