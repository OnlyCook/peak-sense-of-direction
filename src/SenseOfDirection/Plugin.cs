using BepInEx;
using HarmonyLib;

namespace SenseOfDirection
{
    /// <summary>
    /// Sense of Direction: client-sided PEAK mod. Always-visible, edge-of-screen
    /// player labels (distance, status icons, character-color matching), a
    /// matching off-screen indicator for the ping system (bigger, louder from a
    /// distance, richer), and a ghost free-cam mode. See ROADMAP.md for the full
    /// feature spec and phased implementation plan.
    ///
    /// Phase 1 (this state): empty scaffold, no gameplay code yet - just a
    /// loadable, versioned plugin with config plumbing in place.
    /// </summary>
    [BepInPlugin(PluginInfo.Guid, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        internal static Plugin Instance { get; private set; }

        private PluginConfig _cfg;
        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            _cfg = new PluginConfig(Config);
            _harmony = new Harmony(PluginInfo.Guid);

            Logger.LogInfo($"{PluginInfo.Name} {PluginInfo.Version} loaded.");
        }
    }
}
