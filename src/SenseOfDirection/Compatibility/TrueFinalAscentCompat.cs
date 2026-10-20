using System.Reflection;
using UnityEngine;

namespace SenseOfDirection.Compatibility
{
    /// <summary>
    /// True Final Ascent (com.CptPICHU.peak.truefinalascent) runs every biome
    /// in one climb, but it doesn't add segments for the extra ones: it chains
    /// them *inside* existing vanilla segments as "stages"
    /// (<c>FinalAscentSegments.MergeSegment</c>/<c>BuildKilnChain</c>):
    ///
    /// - segment 1: Tropics -> Roots
    /// - segment 2: Alpine -> Mesa
    /// - segment 4: The Kiln -> Gloom -> The Citadel -> summit
    ///
    /// Lighting a stage's campfire has its <c>Campfire.Light_Rpc</c> prefix
    /// clear <c>updateSegment</c> and swap the stage in place instead, so
    /// <c>MapHandler.currentSegment</c> never moves. Vanilla's
    /// <c>CurrentCampfire</c> keeps returning the segment's own (already lit)
    /// fire for the whole second stage, and The Kiln - where vanilla has no
    /// campfire and <see cref="Common.MapTargets.IsPastLastCampfire"/> goes
    /// straight to the summit - now holds two more campfires
    /// (<c>FA_KilnSummitCampfire</c>, then Gloom's).
    ///
    /// So while it's running, its own notion of the current stage is the
    /// source of truth: <c>FinalAscentSegments.CurrentStage.Campfire</c> is
    /// the campfire root to head for, and a stage with none and no
    /// <c>Next</c> (The Citadel) is the final climb to the summit. All of it
    /// is internal, so it's read by reflection; anything that fails to resolve
    /// (a TFA update renaming things) makes <see cref="TryGetStage"/> report
    /// "not governing" and everything falls back to the vanilla logic.
    /// </summary>
    internal static class TrueFinalAscentCompat
    {
        private const string TrueFinalAscentGuid = "com.CptPICHU.peak.truefinalascent";

        private static bool _resolved;
        private static System.Func<bool> _isActive;
        private static System.Func<object> _currentStage;
        private static FieldInfo _stageCampfireField;
        private static FieldInfo _stageNextField;

        /// <summary>
        /// The stage <see cref="_stageCampfire"/> was resolved for, so the
        /// child-component search runs once per stage change, not per frame.
        /// </summary>
        private static object _cachedStage;
        private static Campfire _stageCampfire;
        private static bool _stageIsLast;

        /// <summary>
        /// Whether True Final Ascent is driving the current run; if so,
        /// <paramref name="campfire"/> is the current stage's campfire (null
        /// when the stage has none) and <paramref name="isFinalClimb"/> says the
        /// stage is the last one, i.e. the summit is what's left.
        /// </summary>
        internal static bool TryGetStage(out Campfire campfire, out bool isFinalClimb)
        {
            campfire = null;
            isFinalClimb = false;

            if (!Resolve())
            {
                return false;
            }

            try
            {
                if (!_isActive())
                {
                    return false;
                }

                object stage = _currentStage();
                if (stage == null)
                {
                    return false;
                }

                if (!ReferenceEquals(stage, _cachedStage) || (_stageCampfire == null && !_stageIsLast))
                {
                    _cachedStage = stage;
                    _stageCampfire = FindCampfire(_stageCampfireField.GetValue(stage) as GameObject);
                    _stageIsLast = _stageCampfire == null && _stageNextField.GetValue(stage) == null;
                }

                campfire = _stageCampfire;
                isFinalClimb = _stageIsLast;
                return true;
            }
            catch (System.Exception)
            {
                // Silent: polled every frame, and "fall back to vanilla" is
                // the right answer for any state this can't read.
                return false;
            }
        }

        /// <summary>
        /// Same lookup TFA itself uses on a stage's campfire root
        /// (<c>GetComponentInChildren&lt;Campfire&gt;(true)</c>), preferring an
        /// unlit one should a root ever hold more than one.
        /// </summary>
        private static Campfire FindCampfire(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            Campfire first = null;
            foreach (Campfire candidate in root.GetComponentsInChildren<Campfire>(includeInactive: true))
            {
                if (candidate == null)
                {
                    continue;
                }
                if (candidate.state == Campfire.FireState.Off)
                {
                    return candidate;
                }
                first ??= candidate;
            }
            return first;
        }

        /// <summary>
        /// One-shot: only ever called once a map is up (every caller is behind
        /// <c>MapHandler.ExistsAndInitialized</c>), long after the chainloader
        /// has loaded every plugin, so a miss here really means "not installed".
        /// </summary>
        private static bool Resolve()
        {
            if (_resolved)
            {
                return _isActive != null;
            }
            _resolved = true;

            try
            {
                if (!BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue(TrueFinalAscentGuid, out var info) || info.Instance == null)
                {
                    return false;
                }

                Assembly assembly = info.Instance.GetType().Assembly;
                const BindingFlags StaticAny = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                const BindingFlags InstanceAny = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

                MethodInfo active = assembly.GetType("TrueFinalAscent.FinalAscent")?.GetProperty("Active", StaticAny)?.GetGetMethod(true);
                MethodInfo currentStage = assembly.GetType("TrueFinalAscent.FinalAscentSegments")?.GetProperty("CurrentStage", StaticAny)?.GetGetMethod(true);
                System.Type stageType = assembly.GetType("TrueFinalAscent.FinalAscentStage");
                FieldInfo campfireField = stageType?.GetField("Campfire", InstanceAny);
                FieldInfo nextField = stageType?.GetField("Next", InstanceAny);

                if (active == null || currentStage == null || campfireField == null || nextField == null)
                {
                    Plugin.Instance.Log.LogWarning(
                        "TrueFinalAscentCompat: True Final Ascent is installed but its stage API didn't resolve - " +
                        "the campfire indicator falls back to vanilla segments and will miss its extra biomes' campfires.");
                    return false;
                }

                _isActive = (System.Func<bool>)System.Delegate.CreateDelegate(typeof(System.Func<bool>), active);
                _currentStage = (System.Func<object>)System.Delegate.CreateDelegate(typeof(System.Func<object>), currentStage);
                _stageCampfireField = campfireField;
                _stageNextField = nextField;
                Plugin.Instance.Log.LogInfo("TrueFinalAscentCompat: True Final Ascent detected - campfire indicator follows its stages.");
                return true;
            }
            catch (System.Exception e)
            {
                _isActive = null;
                Plugin.Instance.Log.LogWarning($"TrueFinalAscentCompat: failed to hook True Final Ascent's stage API ({e.GetType().Name}: {e.Message}).");
                return false;
            }
        }
    }
}
