using UnityEngine;

namespace SenseOfDirection.Indicators
{
    /// <summary>
    /// Shared pacing for the anti-overlap mechanism's own repositioning motion
    /// (<see cref="Indicators.IndicatorManager"/>'s on-screen offset and
    /// <see cref="Compass.CompassManager"/>'s tape offset) - see
    /// <c>PluginConfig.AntiOverlapAnimationSpeedMultiplier</c>.
    ///
    /// At the default multiplier (1) this is a plain passthrough to
    /// <see cref="Vector2.MoveTowards"/> at the caller's own base speed -
    /// identical to the un-configurable behaviour before this setting existed.
    /// Below 1 it does two things: slows the interpolation itself, and holds
    /// the target still for a short delay (longer the lower the multiplier)
    /// before committing to a freshly-changed target, so a label doesn't
    /// visibly start sliding the instant an overlap is created or resolved -
    /// for players who find this mod's labels moving around distracting.
    /// Never cuts how often the motion updates (still driven every frame,
    /// still fully interpolated) - only how far it's allowed to travel per
    /// second and how promptly it reacts.
    /// </summary>
    public static class OverlapAnimationPacing
    {
        /// <summary>Longest a target change is ever held back, reached only at the lowest allowed multiplier.</summary>
        private const float MaxDelaySeconds = 0.5f;

        /// <summary>Floor on the speed multiplier - even "as slow as possible" still has to finish resolving an overlap in finite time.</summary>
        private const float MinMultiplier = 0.2f;

        /// <summary>Below this, a frame-to-frame change in the raw target is treated as noise (the label already effectively arrived) rather than a fresh change worth re-delaying for.</summary>
        private const float TargetChangeEpsilonPixels = 0.5f;

        public static float Multiplier =>
            Mathf.Clamp(Plugin.Instance.Cfg.AntiOverlapAnimationSpeedMultiplier.Value, MinMultiplier, 1f);

        /// <summary>Per-anchor state <see cref="Advance"/> needs between calls - owned and stored (e.g. in a <c>Dictionary&lt;IndicatorAnchor, State&gt;</c>) by whichever manager is driving a given anchor's motion.</summary>
        public sealed class State
        {
            public Vector2 HeldTarget;
            public float HeldElapsedSeconds;
            public bool Primed;
        }

        /// <summary>
        /// Advances <paramref name="current"/> one frame towards
        /// <paramref name="rawTarget"/>, through this class's delay/speed
        /// pacing, at up to <paramref name="baseSpeedPerSecond"/> (scaled by
        /// <see cref="Multiplier"/>).
        /// </summary>
        public static Vector2 Advance(Vector2 current, Vector2 rawTarget, float baseSpeedPerSecond, State state)
        {
            float multiplier = Multiplier;

            if (!state.Primed)
            {
                state.HeldTarget = rawTarget;
                state.Primed = true;
            }
            else if (Vector2.Distance(state.HeldTarget, rawTarget) > TargetChangeEpsilonPixels)
            {
                state.HeldElapsedSeconds += Time.deltaTime;
                float delaySeconds = (1f - multiplier) * MaxDelaySeconds;
                if (state.HeldElapsedSeconds >= delaySeconds)
                {
                    state.HeldTarget = rawTarget;
                    state.HeldElapsedSeconds = 0f;
                }
            }
            else
            {
                state.HeldTarget = rawTarget;
                state.HeldElapsedSeconds = 0f;
            }

            return Vector2.MoveTowards(current, state.HeldTarget, Time.deltaTime * baseSpeedPerSecond * multiplier);
        }
    }
}
