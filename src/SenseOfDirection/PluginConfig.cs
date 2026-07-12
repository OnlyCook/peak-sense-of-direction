using BepInEx.Configuration;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
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
        public readonly ConfigEntry<IndicatorDisplayMode> PlayerLabelsCompassDisplayMode;

        public readonly ConfigEntry<bool> EnableCampfireIndicator;
        public readonly ConfigEntry<bool> ShowCampfireDistance;
        public readonly ConfigEntry<IndicatorDisplayMode> CampfireCompassDisplayMode;

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
        public readonly ConfigEntry<int> PingAntiSpamFreeSpamCount;
        public readonly ConfigEntry<float> PingAntiSpamSlowModeIntervalSeconds;
        public readonly ConfigEntry<int> PingAntiSpamMaxQueueLength;
        public readonly ConfigEntry<float> PingAntiSpamResetSeconds;
        public readonly ConfigEntry<bool> EnableGhostPing;
        public readonly ConfigEntry<IndicatorDisplayMode> PingsCompassDisplayMode;

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
        public readonly ConfigEntry<IndicatorDisplayMode> ItemPingsCompassDisplayMode;

        public readonly ConfigEntry<bool> EnableCompass;
        public readonly ConfigEntry<float> CompassWidthPixels;
        public readonly ConfigEntry<float> CompassHeightPixels;
        public readonly ConfigEntry<float> CompassVerticalOffsetPixels;
        public readonly ConfigEntry<float> CompassHorizontalOffsetPixels;
        public readonly ConfigEntry<float> CompassFovDegrees;
        public readonly ConfigEntry<float> CompassIconSizePixels;
        public readonly ConfigEntry<float> CompassElevationThresholdMeters;
        public readonly ConfigEntry<bool> CompassShowDegreeNumbers;
        public readonly ConfigEntry<bool> CompassShowNames;
        public readonly ConfigEntry<bool> CompassShowDistances;
        public readonly ConfigEntry<bool> CompassRequiresHoldingItem;
        public readonly ConfigEntry<SenseOfDirection.Compass.CompassLineColor> CompassLineColor;
        public readonly ConfigEntry<float> CompassLineThicknessMultiplier;

        public readonly ConfigEntry<KeyCode> GhostFreeCamToggleKey;
        public readonly ConfigEntry<bool> EnableGhostFreeCam;
        public readonly ConfigEntry<float> GhostFreeCamMaxDistanceMeters;
        public readonly ConfigEntry<bool> GhostFreeCamUnlimitedRange;
        public readonly ConfigEntry<float> GhostFreeCamMoveSpeedMetersPerSecond;
        public readonly ConfigEntry<float> GhostFreeCamSprintMultiplier;
        public readonly ConfigEntry<bool> HideAllGhosts;

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

            PlayerLabelsCompassDisplayMode = config.Bind(
                "Player-Labels", "compass-display-mode", IndicatorDisplayMode.OffScreenOnly,
                "OffScreenOnly (default, current behavior): players only ever show as " +
                "the edge-of-screen label above. CompassOnly: players only show as a " +
                "marker on the Compass tape (see the Compass section) instead. Both: " +
                "show both at once.");

            EnableCampfireIndicator = config.Bind(
                "Campfire", "enable-campfire-indicator", false,
                "Show an always-on edge-of-screen indicator pointing at the current " +
                "segment's campfire (the one you're trying to reach next), so you " +
                "always know which way to go. Off by default.");

            ShowCampfireDistance = config.Bind(
                "Campfire", "show-campfire-distance", true,
                "Show the distance sub-line under the campfire indicator.");

            CampfireCompassDisplayMode = config.Bind(
                "Campfire", "compass-display-mode", IndicatorDisplayMode.OffScreenOnly,
                "Same OffScreenOnly/CompassOnly/Both choice as Player-Labels/compass-" +
                "display-mode, applied to the campfire indicator instead.");

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
                "than vanilla now that pings are bigger/louder. A short burst of pings " +
                "always goes through instantly; only once someone keeps spamming past that " +
                "does \"slow mode\" kick in, queueing further pings to arrive at a throttled " +
                "rate instead (never silently dropped, unless the queue itself is full - see " +
                "ping-anti-spam-max-queue-length). Never applies to your own pings - only " +
                "ones you receive from other players.");

            PingAntiSpamFreeSpamCount = config.Bind(
                "Pings", "ping-anti-spam-free-spam-count", 3,
                new ConfigDescription(
                    "How many pings in a row from the same player always show up instantly " +
                    "before slow mode kicks in.",
                    new AcceptableValueRange<int>(1, 20)));

            PingAntiSpamSlowModeIntervalSeconds = config.Bind(
                "Pings", "ping-anti-spam-slow-mode-interval-seconds", 0.5f,
                new ConfigDescription(
                    "Once slow mode kicks in, queued pings from that player are spaced at " +
                    "least this far apart before they're actually shown to you.",
                    new AcceptableValueRange<float>(0.1f, 5f)));

            PingAntiSpamMaxQueueLength = config.Bind(
                "Pings", "ping-anti-spam-max-queue-length", 2,
                new ConfigDescription(
                    "How many of a spamming player's pings can be queued up waiting to " +
                    "show at once while in slow mode. Any further ping while the queue's " +
                    "already full is dropped entirely rather than queued.",
                    new AcceptableValueRange<int>(1, 10)));

            PingAntiSpamResetSeconds = config.Bind(
                "Pings", "ping-anti-spam-reset-seconds", 2f,
                new ConfigDescription(
                    "How long a player has to go without pinging (with their queue fully " +
                    "drained) before slow mode fully resets to normal.",
                    new AcceptableValueRange<float>(1f, 30f)));

            EnableGhostPing = config.Bind(
                "Pings", "enable-ghost-ping", true,
                "Let dead players keep pinging as ghosts (vanilla blocks pinging once " +
                "dead), colored using their own character color same as when alive.");

            PingsCompassDisplayMode = config.Bind(
                "Pings", "compass-display-mode", IndicatorDisplayMode.OffScreenOnly,
                "Same OffScreenOnly/CompassOnly/Both choice as Player-Labels/compass-" +
                "display-mode, applied to pings instead.");

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

            ItemPingsCompassDisplayMode = config.Bind(
                "Item-Pings", "compass-display-mode", IndicatorDisplayMode.OffScreenOnly,
                "Same OffScreenOnly/CompassOnly/Both choice as Player-Labels/compass-" +
                "display-mode, applied to item/luggage/creature ping highlights " +
                "instead.");

            EnableCompass = config.Bind(
                "Compass", "enable-compass", true,
                "Master switch for the top-of-screen compass tape (Phase 7). Off " +
                "hides it entirely regardless of any of the per-mechanic compass-" +
                "display-mode settings above.");

            CompassWidthPixels = config.Bind(
                "Compass", "compass-width-pixels", 640f,
                new ConfigDescription(
                    "Width of the compass tape, in pixels at the 1920-wide reference " +
                    "resolution (scales with actual resolution same as everything " +
                    "else). Wider shows more of the horizon at once.",
                    new AcceptableValueRange<float>(300f, 1400f)));

            CompassHeightPixels = config.Bind(
                "Compass", "compass-height-pixels", 40f,
                new ConfigDescription(
                    "Extra vertical gap between the tick row and the marker " +
                    "baseline below it, on top of a small fixed minimum - the " +
                    "default keeps everything tight together. Raise this for more " +
                    "breathing room (e.g. after turning on marker names).",
                    new AcceptableValueRange<float>(40f, 200f)));

            CompassVerticalOffsetPixels = config.Bind(
                "Compass", "compass-vertical-offset-pixels", 14f,
                new ConfigDescription(
                    "Gap between the top of the screen and the compass tape.",
                    new AcceptableValueRange<float>(0f, 300f)));

            CompassHorizontalOffsetPixels = config.Bind(
                "Compass", "compass-horizontal-offset-pixels", 0f,
                new ConfigDescription(
                    "Horizontal offset from top-center. 0 keeps it centered; " +
                    "positive shifts right, negative shifts left (e.g. to dodge " +
                    "another HUD mod's own top-of-screen element).",
                    new AcceptableValueRange<float>(-800f, 800f)));

            CompassFovDegrees = config.Bind(
                "Compass", "compass-fov-degrees", 150f,
                new ConfigDescription(
                    "How much of the horizon (in degrees) is visible on the tape at " +
                    "once before a heading/marker slides off the edge. Lower feels " +
                    "closer to your actual view frustum; higher gives more lead time " +
                    "for things approaching from the side.",
                    new AcceptableValueRange<float>(60f, 180f)));

            CompassIconSizePixels = config.Bind(
                "Compass", "compass-icon-size-pixels", 26f,
                new ConfigDescription(
                    "Size of each marker's icon on the compass.",
                    new AcceptableValueRange<float>(12f, 64f)));

            CompassElevationThresholdMeters = config.Bind(
                "Compass", "compass-elevation-threshold-meters", 3f,
                new ConfigDescription(
                    "A marker only gets an up/down elevation arrow once its target " +
                    "is at least this many meters above/below you - avoids a " +
                    "flickering arrow for things that are roughly level with you.",
                    new AcceptableValueRange<float>(0.5f, 30f)));

            CompassShowDegreeNumbers = config.Bind(
                "Compass", "compass-show-degree-numbers", false,
                "Show a numeric heading (e.g. \"105\") at every non-cardinal tick " +
                "instead of leaving it as a plain unlabeled line. N/E/S/W are always " +
                "lettered either way.");

            CompassShowNames = config.Bind(
                "Compass", "compass-show-names", false,
                "Show a name label above each compass marker that has one (players, " +
                "item/creature pings, the campfire) - off by default to keep the " +
                "tape simple; distances still show independently of this setting.");

            CompassShowDistances = config.Bind(
                "Compass", "compass-show-distances", true,
                "Show a distance sub-label under each compass marker.");

            CompassRequiresHoldingItem = config.Bind(
                "Compass", "compass-requires-holding-item", false,
                "Only show the compass tape while the local player is actually " +
                "holding an in-game Compass item, instead of it always being " +
                "visible. Off by default.");

            CompassLineColor = config.Bind(
                "Compass", "compass-line-color", SenseOfDirection.Compass.CompassLineColor.White,
                "Base color of the compass tape's heading ticks/labels and baseline " +
                "stripe (true north keeps its own dark red accent regardless). " +
                "White is the default.");

            CompassLineThicknessMultiplier = config.Bind(
                "Compass", "compass-line-thickness-multiplier", 1f,
                new ConfigDescription(
                    "Scales the thickness of the compass tape's tick lines (both " +
                    "cardinal and minor) and its baseline stripe. 1 keeps the " +
                    "current/default thickness; higher values make the lines bolder.",
                    new AcceptableValueRange<float>(0.5f, 3f)));

            GhostFreeCamToggleKey = config.Bind(
                "Ghost-Free-Cam", "toggle-key", KeyCode.V,
                "Key that toggles free-fly camera mode on/off while you're dead and " +
                "spectating. Purely local - each player binds their own key. Only " +
                "does anything while enable-ghost-free-cam ends up effectively on " +
                "(see that setting's description for how that's decided).");

            EnableGhostFreeCam = config.Bind(
                "Ghost-Free-Cam", "enable-ghost-free-cam", true,
                "Lets dead players fly a free camera around instead of being stuck in " +
                "vanilla's third-person spectate view. Unlike every other setting in " +
                "this mod, this one and the two below it are host-controlled, not " +
                "purely local: only the room host's own value for these three " +
                "settings ever takes effect for every player, mirroring enable-ghost-" +
                "ping's requirement that both sides have this mod installed. Reason: " +
                "letting each client fly however far they like, unlimited, would be " +
                "an unfair (and effectively ESP-like) advantage other players in the " +
                "same run never agreed to. If the host doesn't have this mod " +
                "installed, ghost free-cam simply doesn't work for anyone, same as " +
                "ghost pinging. Your own value here still matters if/when you end up " +
                "being the host.");

            GhostFreeCamMaxDistanceMeters = config.Bind(
                "Ghost-Free-Cam", "max-distance-meters", 50f,
                new ConfigDescription(
                    "Host-controlled, see enable-ghost-free-cam. How far a ghost's " +
                    "free camera can scout from whichever living player they're " +
                    "currently spectating before being pulled back, like a chain of " +
                    "this length. Ignored entirely when unlimited-range is on.",
                    new AcceptableValueRange<float>(10f, 500f)));

            GhostFreeCamUnlimitedRange = config.Bind(
                "Ghost-Free-Cam", "unlimited-range", false,
                "Host-controlled, see enable-ghost-free-cam. Removes max-distance-" +
                "meters' leash entirely, letting ghosts free-cam anywhere on the " +
                "map. Off by default - the leash is what keeps this mechanic from " +
                "being overpowered.");

            GhostFreeCamMoveSpeedMetersPerSecond = config.Bind(
                "Ghost-Free-Cam", "move-speed-meters-per-second", 15f,
                new ConfigDescription(
                    "Purely local. How fast the free camera flies. PEAK's own built-" +
                    "in dev free-camera controller (reused for the first pass of this " +
                    "feature) turned out to feel unusably slow in practice, so this " +
                    "mod drives its own movement directly in real-world meters/second " +
                    "instead of relying on that controller's tuning.",
                    new AcceptableValueRange<float>(1f, 100f)));

            GhostFreeCamSprintMultiplier = config.Bind(
                "Ghost-Free-Cam", "sprint-multiplier", 3f,
                new ConfigDescription(
                    "Purely local. Speed multiplier while holding Left Shift.",
                    new AcceptableValueRange<float>(1f, 10f)));

            HideAllGhosts = config.Bind(
                "Ghost-Free-Cam", "hide-all-ghosts", false,
                "Purely local (unlike the three host-controlled settings above) - " +
                "hides every dead player's ghost body from your own view entirely. " +
                "Doesn't affect anyone else, and doesn't affect your own ability to " +
                "spectate/free-cam while dead yourself.");

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
