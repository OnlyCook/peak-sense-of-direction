using BepInEx.Configuration;
using SenseOfDirection.Labels;
using UnityEngine;

namespace SenseOfDirection
{
    /// <summary>
    /// All user-facing configuration for Sense of Direction. Mechanic 1/2/3
    /// settings from ROADMAP.md are added to this class as their respective
    /// phases are implemented.
    /// </summary>
    public class PluginConfig
    {
        public readonly ConfigEntry<KeyCode> UiToggleKey;
        public readonly ConfigEntry<bool> EnablePlayerLabels;

        public readonly ConfigEntry<bool> EnableDebugLogging;
        public readonly ConfigEntry<bool> EnableIndicatorTestHarness;

        public readonly ConfigEntry<float> NameFontSize;
        public readonly ConfigEntry<float> DistanceFontSize;
        public readonly ConfigEntry<LabelDisplayMode> DisplayMode;
        public readonly ConfigEntry<float> HoldShownDuration;
        public readonly ConfigEntry<float> MaxDistanceMeters;
        public readonly ConfigEntry<bool> ShowDistanceLabel;
        public readonly ConfigEntry<bool> ShowStatusBadges;
        public readonly ConfigEntry<bool> UseCharacterColor;
        public readonly ConfigEntry<bool> ReplaceVanillaLabels;

        public PluginConfig(ConfigFile config)
        {
            // Bound first so this is the first tab/section in ModConfig-style
            // settings UIs (section order follows bind order).
            UiToggleKey = config.Bind(
                "General", "toggle-key", KeyCode.G,
                "Key that controls Sense of Direction's own UI elements as a whole - " +
                "player labels for now, pings later - per whichever Display-Mode is " +
                "set under Player-Labels. Plain KeyCode (not a modifier combo), since " +
                "that's the only form PEAKLib.ModConfig's settings menu can render as " +
                "a rebindable key.");

            EnablePlayerLabels = config.Bind(
                "General", "enable-player-labels", true,
                "Master switch for Sense of Direction's player labels. Off hides them " +
                "entirely (vanilla's own name labels are unaffected either way).");

            EnableDebugLogging = config.Bind(
                "Debug", "enable-debug-logging", false,
                "Log extra diagnostic detail to the BepInEx console/log file.");

            EnableIndicatorTestHarness = config.Bind(
                "Debug", "enable-indicator-test-harness", false,
                "Spawn a handful of fixed dummy world points around the camera to " +
                "visually verify the edge-of-screen indicator framework. Dev/QA " +
                "tool only - leave off for normal play.");

            NameFontSize = config.Bind(
                "Player-Labels", "name-font-size", 28f,
                new ConfigDescription(
                    "Font size of each player's name label.",
                    new AcceptableValueRange<float>(10f, 60f)));

            DistanceFontSize = config.Bind(
                "Player-Labels", "distance-font-size", 18f,
                new ConfigDescription(
                    "Font size of the distance sub-line shown under each name label.",
                    new AcceptableValueRange<float>(8f, 40f)));

            DisplayMode = config.Bind(
                "Player-Labels", "display-mode", LabelDisplayMode.Toggle,
                "Toggle: press the key to show/hide labels. AlwaysOn: labels are " +
                "always visible. Hold: labels show while the key is held down. " +
                "Key is General/toggle-key above.");

            HoldShownDuration = config.Bind(
                "Player-Labels", "hold-shown-duration", 1.5f,
                new ConfigDescription(
                    "Hold mode only: how many seconds labels stay visible after the key is " +
                    "released (also covers a quick tap, since this timer is set on press, " +
                    "not on release).",
                    new AcceptableValueRange<float>(0f, 10f)));

            MaxDistanceMeters = config.Bind(
                "Player-Labels", "max-distance-meters", 1000f,
                "A player's label stops showing beyond this distance.");

            ShowDistanceLabel = config.Bind(
                "Player-Labels", "show-distance-label", true,
                "Show the distance sub-line under each name label.");

            ShowStatusBadges = config.Bind(
                "Player-Labels", "show-status-badges", true,
                "Show the host crown / unconscious / dead badges on each label.");

            UseCharacterColor = config.Bind(
                "Player-Labels", "use-character-color", true,
                "Color each label's name with that player's own character color " +
                "instead of the vanilla name-label color.");

            ReplaceVanillaLabels = config.Bind(
                "Player-Labels", "replace-vanilla-labels", false,
                "Hide the game's own close-range player name labels entirely, so " +
                "Sense of Direction's labels are the only ones ever shown. Off by " +
                "default - normally the two systems hand off to each other instead.");
        }
    }
}
