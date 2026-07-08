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

        public readonly ConfigEntry<bool> EnableItemPings;
        public readonly ConfigEntry<float> ItemPingDetectionRadiusMeters;
        public readonly ConfigEntry<float> LuggagePingDetectionRadiusMeters;
        public readonly ConfigEntry<float> ItemPingDurationSeconds;
        public readonly ConfigEntry<bool> ShowItemPingName;
        public readonly ConfigEntry<bool> ShowItemPingDistance;
        public readonly ConfigEntry<bool> EnableItemPingOffScreenIndicator;
        public readonly ConfigEntry<bool> EnableItemPingGrouping;
        public readonly ConfigEntry<bool> EnableItemPingHitAssist;
        public readonly ConfigEntry<float> ItemPingHitboxRadiusMeters;
        public readonly ConfigEntry<bool> EnableItemPingRayAssist;
        public readonly ConfigEntry<float> ItemPingRayAssistRadiusMeters;
        public readonly ConfigEntry<bool> EnableCreaturePings;

        public readonly ConfigEntry<bool> EnableDebugLogging;
        public readonly ConfigEntry<bool> EnableIndicatorTestHarness;
        public readonly ConfigEntry<bool> EnableZombieDebugEsp;
        public readonly ConfigEntry<KeyCode> SpawnDebugZombieKey;

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

            EnableItemPings = config.Bind(
                "Item-Pings", "enable-item-pings", true,
                "Highlight nearby items/luggage when you ping near them, with a name " +
                "and distance label - a native replacement for the (now confirmed " +
                "broken against the current game version) memiczny-PingItems mod.");

            ItemPingDetectionRadiusMeters = config.Bind(
                "Item-Pings", "item-ping-detection-radius-meters", 2f,
                new ConfigDescription(
                    "How close a ping needs to land to an item for it to get " +
                    "highlighted.",
                    new AcceptableValueRange<float>(0.5f, 10f)));

            LuggagePingDetectionRadiusMeters = config.Bind(
                "Item-Pings", "luggage-ping-detection-radius-meters", 3.5f,
                new ConfigDescription(
                    "Same as item-ping-detection-radius-meters, but for luggage - " +
                    "larger by default since luggage is a bigger target.",
                    new AcceptableValueRange<float>(0.5f, 15f)));

            ItemPingDurationSeconds = config.Bind(
                "Item-Pings", "item-ping-duration-seconds", 6f,
                new ConfigDescription(
                    "How long an item/luggage highlight stays visible before fading " +
                    "out (ends early regardless if the item is picked up or the " +
                    "luggage is opened).",
                    new AcceptableValueRange<float>(2f, 20f)));

            ShowItemPingName = config.Bind(
                "Item-Pings", "show-item-ping-name", true,
                "Show the item/luggage's name above its highlight.");

            ShowItemPingDistance = config.Bind(
                "Item-Pings", "show-item-ping-distance", true,
                "Show a distance sub-line under the item/luggage highlight.");

            EnableItemPingOffScreenIndicator = config.Bind(
                "Item-Pings", "enable-item-ping-offscreen-indicator", true,
                "Show an edge-of-screen arrow pointing toward a highlighted item/" +
                "luggage when it's off-screen, same mechanism as the ping indicator.");

            EnableItemPingGrouping = config.Bind(
                "Item-Pings", "enable-item-ping-grouping", true,
                "Group multiple nearby items of the same kind into a single " +
                "highlight showing a count (e.g. \"3x Coconut\") instead of one " +
                "highlight per item.");

            EnableItemPingHitAssist = config.Bind(
                "Item-Pings", "enable-item-ping-hit-assist", true,
                "Widen the ping's own aim raycast so it can land directly on an " +
                "item/luggage's own collider (not just terrain/ground), instead of " +
                "phasing through to whatever's behind it - fixes hard-to-ping items " +
                "like a coconut up a tree or a small dropped item. Off falls back to " +
                "vanilla's own terrain-only ping raycast.");

            ItemPingHitboxRadiusMeters = config.Bind(
                "Item-Pings", "item-ping-hitbox-radius-meters", 0.35f,
                new ConfigDescription(
                    "Only used when enable-item-ping-hit-assist is on. Treats the " +
                    "ping raycast as a sphere of this radius instead of an " +
                    "infinitely-thin line, so aiming near (not pixel-perfect on) an " +
                    "item's collider still hits it. 0 disables the sphere and uses a " +
                    "plain raycast (still widened to hit items, just no forgiveness).",
                    new AcceptableValueRange<float>(0f, 1.5f)));

            EnableItemPingRayAssist = config.Bind(
                "Item-Pings", "enable-item-ping-ray-assist", true,
                "Also count an item/luggage as pinged if it's close enough to your " +
                "aim line, independent of physics entirely. Needed for items that " +
                "aren't pushable/hittable until first picked up (an unpicked coconut " +
                "on a tree, berries on a bush, something freshly spawned from opened " +
                "luggage) - their collider is disabled until then, so no physics " +
                "raycast (not even item-ping-hit-assist's) can ever land on them.");

            ItemPingRayAssistRadiusMeters = config.Bind(
                "Item-Pings", "item-ping-ray-assist-radius-meters", 0.6f,
                new ConfigDescription(
                    "Only used when enable-item-ping-ray-assist is on. How far off " +
                    "your exact aim line an item/luggage can be and still count as " +
                    "pinged.",
                    new AcceptableValueRange<float>(0f, 2f)));

            EnableCreaturePings = config.Bind(
                "Item-Pings", "enable-creature-pings", true,
                "Also highlight creatures (beetles, spiders, capybaras, and most " +
                "other mobs) when pinged, same as items/luggage. Off leaves creature " +
                "pings behaving like vanilla (item/luggage highlighting is " +
                "unaffected).");

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

            EnableZombieDebugEsp = config.Bind(
                "Debug", "enable-zombie-debug-esp", false,
                "Temporary dev/QA aid: always-visible edge-of-screen label for every " +
                "naturally-spawned zombie in the level, through walls, to speed up " +
                "testing zombie-ping detection without hunting a whole level for a " +
                "rare spawn. Not a real feature - leave off for normal play.");

            SpawnDebugZombieKey = config.Bind(
                "Debug", "spawn-debug-zombie-key", KeyCode.F9,
                "Temporary dev/QA aid: press to spawn a real, independent " +
                "MushroomZombie a few meters in front of you for testing, without " +
                "waiting on a rare natural spawn and without any effect on your own " +
                "character. Only checked while enable-zombie-debug-esp is on. Not a " +
                "real feature.");
        }
    }
}
