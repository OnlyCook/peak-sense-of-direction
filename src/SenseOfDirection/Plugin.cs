using BepInEx;
using HarmonyLib;
using SenseOfDirection.CampfireIndicator;
using SenseOfDirection.Indicators;
using SenseOfDirection.ItemPings;
using SenseOfDirection.Labels;
using SenseOfDirection.Pings;
using UnityEngine;

namespace SenseOfDirection
{
    /// <summary>
    /// Sense of Direction: client-sided PEAK mod. Always-visible, edge-of-screen
    /// player labels (distance, status icons, character-color matching), a
    /// matching off-screen indicator for the ping system (bigger, louder from a
    /// distance, richer), and a ghost free-cam mode. See ROADMAP.md for the full
    /// feature spec and phased implementation plan.
    ///
    /// Phase 5 (this state): Mechanic 1 (player labels) plus the campfire
    /// indicator bonus, and Mechanic 2 (better pings) are wired up on top of
    /// the Phase 2 indicator framework. Mechanic 3 still unimplemented.
    /// </summary>
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }

        internal PluginConfig Cfg { get; private set; }

        /// <summary>Exposes the protected BepInEx `Logger` to other classes (e.g. ItemPingDetector's debug-only unmatched-object dump).</summary>
        internal BepInEx.Logging.ManualLogSource Log => Logger;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Cfg = new PluginConfig(Config);
            _harmony = new Harmony(PluginInfo.Guid);

            PlayerLabelPatches.Apply(_harmony, Logger);
            VanillaLabelSuppressionPatch.Apply(_harmony, Logger);
            PointPingerPatches.Apply(_harmony, Logger);

            // Always instantiated - internally no-ops per-frame when
            // EnableCampfireIndicator is off, same pattern as
            // PlayerLabelController's own EnablePlayerLabels check.
            _ = CampfireIndicatorController.Instance;

            // Same no-op-when-disabled pattern - internally checks EnablePingAudioBoost.
            _ = PingAudioTuner.Instance;

            // Temporary dev/QA aid (see ZombieDebugEsp's own doc comment) -
            // same no-op-when-disabled pattern, internally checks EnableZombieDebugEsp.
            _ = ZombieDebugEsp.Instance;

            if (Cfg.EnableIndicatorTestHarness.Value)
            {
                var go = new GameObject("SenseOfDirection.IndicatorTestHarness");
                DontDestroyOnLoad(go);
                go.AddComponent<IndicatorTestHarness>();
            }

            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded.");
        }
    }
}
