using BepInEx.Configuration;

namespace SenseOfDirection
{
    /// <summary>
    /// All user-facing configuration for Sense of Direction.
    /// Empty scaffold for now (Phase 1) - Mechanic 1/2/3 settings from ROADMAP.md
    /// are added to this class as their respective phases are implemented.
    /// </summary>
    public class PluginConfig
    {
        public readonly ConfigEntry<bool> EnableDebugLogging;

        public PluginConfig(ConfigFile config)
        {
            EnableDebugLogging = config.Bind(
                "Debug", "enable-debug-logging", false,
                "Log extra diagnostic detail to the BepInEx console/log file.");
        }
    }
}
