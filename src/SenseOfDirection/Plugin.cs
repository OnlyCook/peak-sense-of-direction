using BepInEx;
using HarmonyLib;
using SenseOfDirection.Indicators;
using SenseOfDirection.Labels;
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
    /// Phase 3 (this state): Mechanic 1 (player labels) is wired up on top of
    /// the Phase 2 indicator framework. Mechanic 2/3 still unimplemented.
    /// </summary>
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }

        internal PluginConfig Cfg { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Cfg = new PluginConfig(Config);
            _harmony = new Harmony(PluginInfo.Guid);

            PlayerLabelPatches.Apply(_harmony, Logger);
            VanillaLabelSuppressionPatch.Apply(_harmony, Logger);

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
