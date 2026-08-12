using System;
using System.Collections.Generic;
using UnityEngine;

namespace SenseOfDirection.Common
{
    /// <summary>
    /// The mod's blanket "never take the game down with us" guard.
    ///
    /// This mod is client-sided and entirely cosmetic/informational: there is
    /// no failure inside it that is worth propagating into the game. Every
    /// entry point the *game* (or Unity, or Photon) calls into us - Harmony
    /// patch bodies, MonoBehaviour Update/LateUpdate, Photon event callbacks,
    /// startup wiring - therefore routes through here, so that a bug of ours,
    /// a game update that moved a field, or another mod leaving shared state
    /// in a shape we didn't expect degrades into "one feature stops drawing"
    /// rather than into a broken Character, a dead HUD, or a stalled network
    /// callback.
    ///
    /// Why this matters more than usual for Harmony patch bodies specifically:
    /// an exception thrown inside a prefix/postfix does not stay inside it.
    /// Harmony lets it propagate out of the *patched vanilla method*, so a
    /// throw in, say, our <c>Character.Awake</c> postfix aborts the rest of
    /// vanilla's own <c>Awake</c> call chain and leaves a half-constructed
    /// Character behind - which is how a purely cosmetic label bug turns into
    /// "my friends can't hear me" or "the game fell over on spawn". Wrapping
    /// the body converts that into a logged no-op.
    ///
    /// Two behaviours worth knowing about:
    ///
    /// - <b>Logging is rate-limited per context.</b> A failure that repeats
    ///   every frame would otherwise write tens of thousands of stack traces
    ///   into <c>LogOutput.log</c> and cost more framerate than the feature
    ///   was worth. The first failure of a context always logs in full; after
    ///   that, at most one per <see cref="LogIntervalSeconds"/>.
    /// - <b>Optional auto-disable.</b> Pass a <c>failureLimit</c> and a
    ///   context that fails that many times *consecutively* is switched off
    ///   for the rest of the session (any single success resets the count).
    ///   Used for the per-frame drivers, where "permanently broken" is far
    ///   more likely than "unlucky frame" and retrying forever just burns CPU
    ///   throwing. Left off (the default) everywhere a retry is cheap and
    ///   plausibly recoverable.
    /// </summary>
    internal static class Safe
    {
        private const float LogIntervalSeconds = 5f;

        /// <summary>
        /// A named guard, held in a <c>static readonly</c> field by whatever
        /// uses it, for the paths that run every frame.
        ///
        /// The <see cref="Run"/> helper below is more
        /// pleasant to read but takes a delegate, and a lambda that captures
        /// anything allocates a closure *per call*. In an Update/LateUpdate -
        /// or worse, in a per-anchor loop inside one - that's a few hundred
        /// short-lived objects a second handed to the GC purely as the cost of
        /// error handling, which is a bad trade in a game's render path. This
        /// form has no delegate and no allocation at all: the caller writes
        /// the try/catch itself and just reports through here.
        ///
        /// <code>
        /// private static readonly Safe.Context Ctx = new Safe.Context("Foo.Update", failureLimit: 300);
        ///
        /// private void Update()
        /// {
        ///     if (Ctx.Disabled) return;
        ///     try { UpdateImpl(); Ctx.Succeeded(); }
        ///     catch (Exception e) { Ctx.Failed(e); }
        /// }
        /// </code>
        /// </summary>
        internal class Context
        {
            private readonly string _name;
            private readonly int _failureLimit;
            private readonly State _state = new State();

            /// <param name="failureLimit">
            /// Consecutive failures before this context switches itself off
            /// for the session; 0 means "retry forever".
            /// </param>
            internal Context(string name, int failureLimit = 0)
            {
                _name = name;
                _failureLimit = failureLimit;
            }

            internal bool Disabled => _state.Disabled;

            /// <summary>Call after the guarded body completed - resets the consecutive-failure count.</summary>
            internal void Succeeded() => _state.ConsecutiveFailures = 0;

            /// <summary>Call from the <c>catch</c>; logs (rate-limited) and applies the auto-disable rule.</summary>
            internal void Failed(Exception e)
            {
                _state.ConsecutiveFailures++;
                bool disabling = _failureLimit > 0 && _state.ConsecutiveFailures >= _failureLimit;
                if (disabling)
                {
                    _state.Disabled = true;
                }
                Report(_state, _name, e, disabling);
            }
        }

        private class State
        {
            internal int ConsecutiveFailures;
            internal float LastLogTime;
            internal bool Disabled;
            internal bool HasLogged;
        }

        private static readonly Dictionary<string, State> States = new Dictionary<string, State>();

        private static State GetState(string context)
        {
            if (!States.TryGetValue(context, out State state))
            {
                state = new State();
                States[context] = state;
            }
            return state;
        }

        /// <summary>
        /// Runs <paramref name="body"/>, swallowing and logging anything it
        /// throws. Returns <see langword="true"/> if it completed.
        ///
        /// <paramref name="failureLimit"/> of 0 (the default) means "keep
        /// retrying forever"; any positive value switches the context off
        /// permanently after that many consecutive failures.
        ///
        /// For <em>cold</em> paths - startup, scene load, opening a menu, a
        /// character spawning. A capturing lambda allocates per call, so
        /// anything that runs every frame should use a
        /// <see cref="Context"/> field and its own try/catch instead.
        /// </summary>
        internal static bool Run(string context, Action body, int failureLimit = 0)
        {
            State state = GetState(context);
            if (state.Disabled)
            {
                return false;
            }

            try
            {
                body();
                state.ConsecutiveFailures = 0;
                return true;
            }
            catch (Exception e)
            {
                state.ConsecutiveFailures++;
                bool disabling = failureLimit > 0 && state.ConsecutiveFailures >= failureLimit;
                if (disabling)
                {
                    state.Disabled = true;
                }
                Report(state, context, e, disabling);
                return false;
            }
        }

        private static void Report(State state, string context, Exception e, bool disabling)
        {
            // Time.unscaledTime is only legal on the main thread. Everything
            // routed through here is main-thread (Unity callbacks, Harmony
            // patch bodies, Photon's main-thread dispatch), but a guard is
            // cheaper than the alternative failure mode: a throw *inside* the
            // error handler would escape this method and defeat the whole
            // point of it.
            float now;
            try
            {
                now = Time.unscaledTime;
            }
            catch
            {
                now = state.LastLogTime;
            }

            bool shouldLog = !state.HasLogged || disabling || now - state.LastLogTime >= LogIntervalSeconds;
            if (!shouldLog)
            {
                return;
            }

            state.HasLogged = true;
            state.LastLogTime = now;

            string suffix = disabling
                ? $" - failed {state.ConsecutiveFailures}x in a row, disabling it for the rest of this session"
                : state.ConsecutiveFailures > 1
                    ? $" (failure #{state.ConsecutiveFailures}, further identical errors suppressed for {LogIntervalSeconds:0}s)"
                    : string.Empty;

            try
            {
                Plugin.Instance?.Log?.LogError($"SenseOfDirection: {context} failed{suffix}: {e}");
            }
            catch
            {
                // No logger yet (or it threw): there is nowhere left to report
                // to, and throwing from the guard is strictly worse than
                // losing one log line.
            }
        }
    }
}
