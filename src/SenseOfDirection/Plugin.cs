using BepInEx;
using HarmonyLib;
using SenseOfDirection.CampfireIndicator;
using SenseOfDirection.Common;
using SenseOfDirection.Compass;
using SenseOfDirection.GhostFreeCam;
using SenseOfDirection.Indicators;
using SenseOfDirection.ItemPings;
using SenseOfDirection.Labels;
using SenseOfDirection.LuggagePing;
using SenseOfDirection.Pings;
using SenseOfDirection.PirateCompass;
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

        /// <summary>
        /// Every step of startup is wired up independently through
        /// <see cref="Common.Safe"/> rather than run as one straight-line
        /// block. Previously any single failure - one moved game field, one
        /// mod that got to a patch target first - would throw out of
        /// <c>Awake</c> and abandon every step after it, leaving the mod in a
        /// silent half-initialised state that's far harder to diagnose (and
        /// far more likely to misbehave) than a cleanly missing feature.
        /// Now each system either comes up or is skipped with a log line, and
        /// the rest still load.
        ///
        /// Note the ordering guarantee this deliberately preserves: the config
        /// and the Harmony instance are built first and *un*guarded. If those
        /// two can't be created there is no mod to speak of, and silently
        /// continuing past them would only produce a cascade of confusing
        /// downstream failures.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            Cfg = new PluginConfig(Config);
            _harmony = new Harmony(PluginInfo.Guid);

            // Harmony patch sets. Each Apply already catches internally, so
            // these guards are belt-and-braces against a failure in the
            // AccessTools/typeof resolution that happens before its own try.
            Wire("PlayerLabelPatches", () => PlayerLabelPatches.Apply(_harmony, Logger));
            Wire("VanillaLabelSuppressionPatch", () => VanillaLabelSuppressionPatch.Apply(_harmony, Logger));
            Wire("PointPingerPatches", () => PointPingerPatches.Apply(_harmony, Logger));
            Wire("GhostFreeCamPatches", () => GhostFreeCamPatches.Apply(_harmony, Logger));
            Wire("PauseSuppressPatch", () => Ui.PauseSuppressPatch.Apply(_harmony, Logger));
            Wire("LuggageCompassSpawner", () => CompassItems.LuggageCompassSpawner.Apply(_harmony, Logger));
            Wire("PingableRegistryPatches", () => PingableRegistryPatches.Apply(_harmony, Logger));
            Wire("PirateCompassNeedlePatch", () => PirateCompassNeedlePatch.Apply(_harmony, Logger));

            // Watches for known-broken patches other mods leave on vanilla
            // methods this mod depends on (currently PEAKSleepTalk's), and
            // removes them. Deliberately independent of every patch set above:
            // it has to keep working even when one of them didn't apply.
            Wire("SleepTalkCompat", () => Compatibility.SleepTalkCompat.Initialize(_harmony, Logger));

            // Always instantiated - internally no-ops per-frame when
            // EnableCampfireIndicator is off, same pattern as
            // PlayerLabelController's own EnablePlayerLabels check.
            Wire("CampfireIndicatorController", () => _ = CampfireIndicatorController.Instance);

            // Same no-op-when-disabled pattern - internally checks EnableScoutStatueIndicator.
            Wire("ScoutStatueIndicatorController", () => _ = ScoutStatueIndicator.ScoutStatueIndicatorController.Instance);

            // Same no-op-when-disabled pattern - internally checks EnableBelltowerIndicator.
            Wire("BelltowerIndicatorController", () => _ = BelltowerIndicator.BelltowerIndicatorController.Instance);

            // Same no-op-when-disabled pattern - internally checks EnablePingAudioBoost.
            Wire("PingAudioTuner", () => _ = PingAudioTuner.Instance);

            // Phase 7: same no-op-when-disabled pattern - internally checks EnableCompass.
            Wire("CompassManager", () => _ = CompassManager.Instance);

            // Same no-op-when-disabled pattern - internally checks EnablePirateCompassLuggageIndicator.
            Wire("PirateCompassLuggageIndicatorController", () => _ = PirateCompassLuggageIndicatorController.Instance);

            // Same no-op-when-disabled pattern - internally checks EnableLuggagePing.
            Wire("LuggagePingController", () => _ = LuggagePingController.Instance);

            // Same no-op-when-disabled pattern - internally checks
            // EnableCompassAtCampfires (and does nothing off the host).
            Wire("CampfireCompassSpawner", () => _ = CompassItems.CampfireCompassSpawner.Instance);

            // Always instantiated - clears every player label on any scene
            // load (main menu, lobby, a run) so a label whose Character never
            // fired OnDestroy can't stay stuck on screen forever. See its own
            // doc comment.
            Wire("SceneResetCoordinator", () => _ = Common.SceneResetCoordinator.Instance);

            // Keeps the "what's pingable in this level" sweep (and every icon/
            // widget/mesh a ping needs) off the ping path itself, so pinging
            // never has to stop and build something first.
            Wire("PingableRegistry", () => _ = PingableRegistry.Instance);
            Wire("PingPrewarm", () => _ = PingPrewarm.Instance);

            // The in-game settings/preview menu (General/preview-menu-key, F8).
            // Always instantiated: it builds nothing until actually opened, and
            // it's what watches for that key in the first place.
            Wire("PreviewMenu", () => _ = Ui.PreviewMenu.Instance);

            // Temporary dev/QA aid (see ZombieDebugEsp's own doc comment) -
            // same no-op-when-disabled pattern, internally checks EnableZombieDebugEsp.
            Wire("ZombieDebugEsp", () => _ = ZombieDebugEsp.Instance);

            Wire("IndicatorTestHarness", () =>
            {
                if (!Cfg.EnableIndicatorTestHarness.Value)
                {
                    return;
                }
                var go = new GameObject("SenseOfDirection.IndicatorTestHarness");
                DontDestroyOnLoad(go);
                go.AddComponent<IndicatorTestHarness>();
            });

            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded.");
        }

        /// <summary>
        /// Brings one system up, logging and skipping it if it fails rather
        /// than taking the rest of <see cref="Awake"/> down with it.
        /// </summary>
        private void Wire(string system, System.Action setup)
        {
            if (!Common.Safe.Run($"startup: {system}", setup))
            {
                Logger.LogWarning($"{system} failed to initialise - that feature is disabled, the rest of the mod is unaffected.");
            }
        }
    }
}
