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

        public readonly ConfigEntry<float> NameFontSize;
        public readonly ConfigEntry<float> DistanceFontSize;
        public readonly ConfigEntry<LabelDisplayMode> DisplayMode;
        public readonly ConfigEntry<float> HoldShownDuration;
        public readonly ConfigEntry<float> MaxDistanceMeters;
        public readonly ConfigEntry<bool> ShowDistanceLabel;
        public readonly ConfigEntry<bool> ShowStatusBadges;
        public readonly ConfigEntry<bool> UseCharacterColor;
        public readonly ConfigEntry<bool> ReplaceVanillaLabels;

        public readonly ConfigEntry<bool> EnableCampfireIndicator;
        public readonly ConfigEntry<bool> ShowCampfireDistance;

        public readonly ConfigEntry<bool> EnablePingScaling;
        public readonly ConfigEntry<float> PingScaleMultiplier;
        public readonly ConfigEntry<bool> EnablePingRipple;
        public readonly ConfigEntry<bool> EnablePingAudioBoost;
        public readonly ConfigEntry<float> PingAudioRangeMeters;
        public readonly ConfigEntry<float> PingAudioMinDistanceMeters;
        public readonly ConfigEntry<float> PingAudioVolumeMultiplier;
        public readonly ConfigEntry<bool> RemoveVisibilityCutoff;
        public readonly ConfigEntry<bool> EnablePingOffScreenIndicator;
        public readonly ConfigEntry<bool> ShowPingDistanceLabel;
        public readonly ConfigEntry<bool> EnablePingAntiSpam;
        public readonly ConfigEntry<float> PingAntiSpamRapidIntervalSeconds;
        public readonly ConfigEntry<float> PingAntiSpamCooldownStepSeconds;
        public readonly ConfigEntry<float> PingAntiSpamMaxCooldownSeconds;
        public readonly ConfigEntry<float> PingAntiSpamResetSeconds;
        public readonly ConfigEntry<bool> EnableGhostPing;

        public readonly ConfigEntry<bool> EnableDebugLogging;
        public readonly ConfigEntry<bool> EnableIndicatorTestHarness;

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

            EnableCampfireIndicator = config.Bind(
                "Campfire", "enable-campfire-indicator", false,
                "Show an always-on edge-of-screen indicator pointing at the current " +
                "segment's campfire (the one you're trying to reach next), so you " +
                "always know which way to go. Off by default.");

            ShowCampfireDistance = config.Bind(
                "Campfire", "show-campfire-distance", true,
                "Show the distance sub-line under the campfire indicator.");

            EnablePingScaling = config.Bind(
                "Pings", "enable-ping-scaling", true,
                "Scale ping visuals up the further away they are, well past vanilla's " +
                "own hard-capped scale, so far pings stay easy to spot.");

            PingScaleMultiplier = config.Bind(
                "Pings", "ping-scale-multiplier", 1f,
                new ConfigDescription(
                    "Extra multiplier applied on top of vanilla's own (uncapped, see " +
                    "remove-visibility-cutoff) ping scale. 1x is vanilla's own uncapped " +
                    "size; higher makes every ping bigger regardless of distance.",
                    new AcceptableValueRange<float>(0.5f, 3f)));

            EnablePingRipple = config.Bind(
                "Pings", "enable-ping-ripple", true,
                "Show an expanding ring in the pinging player's own character color " +
                "at the ping location, so it reads against similarly-colored terrain.");

            EnablePingAudioBoost = config.Bind(
                "Pings", "enable-ping-audio-boost", true,
                "Drastically reduce the ping sound's distance falloff so it's audible " +
                "from much further away, while sounding unchanged up close.");

            PingAudioRangeMeters = config.Bind(
                "Pings", "ping-audio-range-meters", 600f,
                new ConfigDescription(
                    "Ping sound's max audible range when audio boost is on (vanilla default is 150).",
                    new AcceptableValueRange<float>(150f, 2000f)));

            PingAudioMinDistanceMeters = config.Bind(
                "Pings", "ping-audio-min-distance-meters", 10f,
                new ConfigDescription(
                    "Distance under which the ping sound plays at full, unfalling-off volume " +
                    "before starting to fade out toward ping-audio-range-meters.",
                    new AcceptableValueRange<float>(1f, 50f)));

            PingAudioVolumeMultiplier = config.Bind(
                "Pings", "ping-audio-volume-multiplier", 0.85f,
                new ConfigDescription(
                    "Multiplier on the ping sound's own base (close-range) volume when " +
                    "audio boost is on - the far-range audibility boost also makes it " +
                    "slightly too loud up close, so this trims that back down.",
                    new AcceptableValueRange<float>(0.3f, 1.5f)));

            RemoveVisibilityCutoff = config.Bind(
                "Pings", "remove-visibility-cutoff", true,
                "Vanilla silently refuses to even spawn a ping's visual once its " +
                "pinging player is more than ~40-50m from you. On by default so far " +
                "pings still show up at all - most of the other Pings settings only " +
                "matter once this is on.");

            EnablePingOffScreenIndicator = config.Bind(
                "Pings", "enable-ping-offscreen-indicator", true,
                "Show an edge-of-screen arrow pointing toward an active ping when it's " +
                "off-screen, same mechanism as the player-label/campfire indicators.");

            ShowPingDistanceLabel = config.Bind(
                "Pings", "show-ping-distance-label", true,
                "Show a distance sub-line under the ping indicator.");

            EnablePingAntiSpam = config.Bind(
                "Pings", "enable-ping-anti-spam", true,
                "Rate-limit how often *other* players' pings actually render/play once " +
                "they're pinging rapidly, so spamming the ping key isn't more disruptive " +
                "than vanilla now that pings are bigger/louder. Never applies to your own " +
                "pings - only ones you receive from other players.");

            PingAntiSpamRapidIntervalSeconds = config.Bind(
                "Pings", "ping-anti-spam-rapid-interval-seconds", 1.5f,
                new ConfigDescription(
                    "Pings from the same player arriving faster than this count as " +
                    "\"rapid\" and gradually ramp up their required cooldown. Pinging " +
                    "slower than this never ramps anything up.",
                    new AcceptableValueRange<float>(0.1f, 10f)));

            PingAntiSpamCooldownStepSeconds = config.Bind(
                "Pings", "ping-anti-spam-cooldown-step-seconds", 1f,
                new ConfigDescription(
                    "Extra required cooldown added per rapid ping from the same player - " +
                    "the more they spam in a row, the longer they have to wait between " +
                    "pings actually showing up for you.",
                    new AcceptableValueRange<float>(0.1f, 5f)));

            PingAntiSpamMaxCooldownSeconds = config.Bind(
                "Pings", "ping-anti-spam-max-cooldown-seconds", 8f,
                new ConfigDescription(
                    "Cap on how long the ramped-up cooldown from ping-anti-spam-cooldown-" +
                    "step-seconds can grow to.",
                    new AcceptableValueRange<float>(1f, 30f)));

            PingAntiSpamResetSeconds = config.Bind(
                "Pings", "ping-anti-spam-reset-seconds", 6f,
                new ConfigDescription(
                    "How long a player has to go without pinging before their ramped-up " +
                    "cooldown fully resets to normal.",
                    new AcceptableValueRange<float>(1f, 30f)));

            EnableGhostPing = config.Bind(
                "Pings", "enable-ghost-ping", true,
                "Let dead players keep pinging as ghosts (vanilla blocks pinging once " +
                "dead), colored using their own character color same as when alive.");

            // Bound last so Debug is the last tab/section in ModConfig-style
            // settings UIs (section order follows bind order) - dev/QA
            // settings belong at the end, not ahead of anything user-facing.
            EnableDebugLogging = config.Bind(
                "Debug", "enable-debug-logging", false,
                "Log extra diagnostic detail to the BepInEx console/log file.");

            EnableIndicatorTestHarness = config.Bind(
                "Debug", "enable-indicator-test-harness", false,
                "Spawn a handful of fixed dummy world points around the camera to " +
                "visually verify the edge-of-screen indicator framework. Dev/QA " +
                "tool only - leave off for normal play.");
        }
    }
}
