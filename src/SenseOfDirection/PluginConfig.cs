using BepInEx.Configuration;
using SenseOfDirection.Compass;
using SenseOfDirection.Indicators;
using SenseOfDirection.ItemPings;
using SenseOfDirection.Labels;
using UnityEngine;

namespace SenseOfDirection
{
    /// <summary>
    /// All user-facing configuration for Sense of Direction.
    ///
    /// Two conventions hold throughout, both of them driven by how
    /// PEAKLib.ModConfig renders this in-game (one tab per section, every key
    /// shown uppercase next to its section):
    ///
    /// <list type="bullet">
    /// <item>Section order follows <em>bind</em> order, so the binds below run
    /// global-first, then per-mechanic, then dev/QA last.</item>
    /// <item>A key never repeats its own section's name. <c>Compass/width-pixels</c>,
    /// not <c>Compass/compass-width-pixels</c>. The one deliberate exception is
    /// each section's master switch, which stays fully descriptive
    /// (<c>enable-item-pings</c>, not a bare <c>enable</c>) - a lone "ENABLE"
    /// in the settings menu reads identically across every tab.</item>
    /// </list>
    /// </summary>
    public class PluginConfig
    {
        public readonly ConfigEntry<IndicatorPlacement> PlayerLabelPlacement;
        public readonly ConfigEntry<IndicatorPlacement> CampfirePlacement;
        public readonly ConfigEntry<IndicatorPlacement> PingPlacement;
        public readonly ConfigEntry<IndicatorPlacement> ItemPingPlacement;
        public readonly ConfigEntry<IndicatorPlacement> PirateCompassLuggagePlacement;
        public readonly ConfigEntry<bool> EnableLabelOverlapAvoidance;
        public readonly ConfigEntry<KeyCode> PreviewMenuKey;

        public readonly ConfigEntry<float> OnScreenNameFontScale;
        public readonly ConfigEntry<float> OnScreenDistanceFontScale;
        public readonly ConfigEntry<float> OffScreenNameFontScale;
        public readonly ConfigEntry<float> OffScreenDistanceFontScale;
        public readonly ConfigEntry<float> CompassNameFontScale;
        public readonly ConfigEntry<float> CompassDistanceFontScale;

        public readonly ConfigEntry<bool> EnablePlayerLabels;
        public readonly ConfigEntry<KeyCode> PlayerLabelToggleKey;
        public readonly ConfigEntry<LabelDisplayMode> PlayerLabelDisplayMode;
        public readonly ConfigEntry<float> HoldShownDuration;
        public readonly ConfigEntry<float> PlayerLabelMaxDistanceMeters;
        public readonly ConfigEntry<float> PlayerLabelNameFontSize;
        public readonly ConfigEntry<float> PlayerLabelDistanceFontSize;
        public readonly ConfigEntry<bool> ShowPlayerLabelDistance;
        public readonly ConfigEntry<bool> ShowStatusBadges;
        public readonly ConfigEntry<bool> UseCharacterColor;
        public readonly ConfigEntry<bool> ReplaceVanillaLabels;
        public readonly ConfigEntry<bool> ShowPlayerSkeleton;
        public readonly ConfigEntry<float> PlayerSkeletonLineThickness;
        public readonly ConfigEntry<bool> PlayerSkeletonUseCharacterColor;
        public readonly ConfigEntry<bool> ShowPlayerSkeletonJoints;

        public readonly ConfigEntry<bool> EnableCampfireIndicator;
        public readonly ConfigEntry<bool> ShowCampfireDistance;
        public readonly ConfigEntry<bool> HideCampfireName;

        public readonly ConfigEntry<bool> RemoveVisibilityCutoff;
        public readonly ConfigEntry<bool> EnablePingScaling;
        public readonly ConfigEntry<float> PingScaleMultiplier;
        public readonly ConfigEntry<bool> EnablePingRipple;
        public readonly ConfigEntry<bool> EnablePingOffScreenIndicator;
        public readonly ConfigEntry<bool> ShowPingDistanceLabel;
        public readonly ConfigEntry<bool> EnableGhostPing;

        public readonly ConfigEntry<bool> EnablePingAudioBoost;
        public readonly ConfigEntry<float> PingAudioRangeMeters;
        public readonly ConfigEntry<float> PingAudioMinDistanceMeters;
        public readonly ConfigEntry<float> PingAudioVolumeMultiplier;

        public readonly ConfigEntry<bool> EnablePingAntiSpam;
        public readonly ConfigEntry<int> PingAntiSpamFreeSpamCount;
        public readonly ConfigEntry<float> PingAntiSpamSlowModeIntervalSeconds;
        public readonly ConfigEntry<int> PingAntiSpamMaxQueueLength;
        public readonly ConfigEntry<float> PingAntiSpamResetSeconds;

        public readonly ConfigEntry<bool> EnableItemPings;
        public readonly ConfigEntry<float> ItemPingDurationSeconds;
        public readonly ConfigEntry<bool> EnableItemPingGrouping;
        public readonly ConfigEntry<bool> EnableCreaturePings;
        public readonly ConfigEntry<bool> UseNativeItemPingIcons;
        public readonly ConfigEntry<ItemPings.ItemPingNameMode> ItemPingNameMode;
        public readonly ConfigEntry<bool> ShowItemPingDistance;
        public readonly ConfigEntry<bool> EnableItemPingOffScreenIndicator;

        public readonly ConfigEntry<float> ItemPingDetectionRadiusMeters;
        public readonly ConfigEntry<float> ItemPingCrossKindRadiusMeters;
        public readonly ConfigEntry<float> LuggagePingDetectionRadiusMeters;
        public readonly ConfigEntry<bool> EnableItemPingHitAssist;
        public readonly ConfigEntry<float> ItemPingHitboxRadiusMeters;
        public readonly ConfigEntry<bool> EnableItemPingRayAssist;
        public readonly ConfigEntry<float> ItemPingRayAssistRadiusMeters;

        public readonly ConfigEntry<bool> EnableCompass;
        public readonly ConfigEntry<float> CompassWidthPixels;
        public readonly ConfigEntry<float> CompassMarkerGapPixels;
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
        public readonly ConfigEntry<bool> CompassClampIconsToEdge;
        public readonly ConfigEntry<bool> CompassColorPlayerLabels;

        public readonly ConfigEntry<bool> EnablePirateCompassLuggageIndicator;
        public readonly ConfigEntry<bool> ShowPirateCompassLuggageName;
        public readonly ConfigEntry<bool> ShowPirateCompassLuggageDistance;
        public readonly ConfigEntry<bool> EnablePirateCompassLuggageOffScreenIndicator;

        public readonly ConfigEntry<bool> EnableLuggagePing;
        public readonly ConfigEntry<KeyCode> LuggagePingKey;
        public readonly ConfigEntry<float> LuggagePingRadiusMeters;
        public readonly ConfigEntry<float> LuggagePingDurationSeconds;
        public readonly ConfigEntry<float> LuggagePingCooldownSeconds;

        public readonly ConfigEntry<bool> EnableGhostFreeCam;
        public readonly ConfigEntry<float> GhostFreeCamMaxDistanceMeters;
        public readonly ConfigEntry<bool> GhostFreeCamUnlimitedRange;
        public readonly ConfigEntry<KeyCode> GhostFreeCamToggleKey;
        public readonly ConfigEntry<float> GhostFreeCamMoveSpeedMetersPerSecond;
        public readonly ConfigEntry<float> GhostFreeCamSprintMultiplier;
        public readonly ConfigEntry<bool> GhostFreeCamShowCrosshair;
        public readonly ConfigEntry<bool> GhostFreeCamShowKeyHint;
        public readonly ConfigEntry<bool> HideAllGhosts;

        public readonly ConfigEntry<bool> EnableDebugLogging;
        public readonly ConfigEntry<bool> EnableIndicatorTestHarness;
        public readonly ConfigEntry<bool> EnableZombieDebugEsp;
        public readonly ConfigEntry<bool> EnableGhostFreeCamKeyHintPreview;

        public PluginConfig(ConfigFile config)
        {
            // ---- General: what shows up, and where. The four *-placement
            // settings are the ones people actually reach for ("put everything
            // on the compass"), so they lead - and they belong together rather
            // than one per mechanic tab, because they aren't a property of any
            // mechanic: they route a tracked thing between the two shared
            // rendering surfaces (screen edge vs. compass tape). Split across
            // four tabs, the single intent "show all of it on the compass" cost
            // four trips through the settings menu.
            //
            // Note the deliberate distinction from Player-Labels/display-mode:
            // placement answers *where* a thing is drawn, display-mode answers
            // *when* player labels are shown (Toggle/AlwaysOn/Hold). Two
            // different questions, so two clearly different words - see
            // Indicators.IndicatorPlacement.
            PlayerLabelPlacement = config.Bind(
                "General", "player-label-placement", IndicatorPlacement.Both,
                "Where player labels are drawn. Both: as the edge-of-screen " +
                "label and as a marker on the compass tape at once. OffScreenOnly: only " +
                "the edge-of-screen label. CompassOnly: only the compass marker.");

            CampfirePlacement = config.Bind(
                "General", "campfire-placement", IndicatorPlacement.OffScreenOnly,
                "Where the campfire indicator is drawn, using the same OffScreenOnly/" +
                "CompassOnly/Both choice as player-label-placement.");

            PingPlacement = config.Bind(
                "General", "ping-placement", IndicatorPlacement.OffScreenOnly,
                "Where pings are drawn, using the same OffScreenOnly/CompassOnly/Both " +
                "choice as player-label-placement.");

            ItemPingPlacement = config.Bind(
                "General", "item-ping-placement", IndicatorPlacement.OffScreenOnly,
                "Where item/luggage/creature ping highlights are drawn, using the same " +
                "OffScreenOnly/CompassOnly/Both choice as player-label-placement.");

            PirateCompassLuggagePlacement = config.Bind(
                "General", "pirate-compass-luggage-placement", IndicatorPlacement.Both,
                "Where the Pirate's Compass luggage indicator is drawn, using the same " +
                "OffScreenOnly/CompassOnly/Both choice as player-label-placement.");

            EnableLabelOverlapAvoidance = config.Bind(
                "General", "enable-label-overlap-avoidance", true,
                "Nudges overlapping player/ping/item-ping/campfire labels (and compass " +
                "markers) apart so they stay readable when several land on top of each " +
                "other, instead of stacking illegibly. Off restores every label/marker " +
                "to its exact tracked position with no nudging at all.");

            PreviewMenuKey = config.Bind(
                "General", "preview-menu-key", KeyCode.F8,
                "Key that opens the in-game settings menu: every visual setting in " +
                "this mod, laid out over a live preview of what it actually does " +
                "(player labels, pings, item pings, the campfire indicator and the " +
                "compass, all drawn on a real screenshot and updating as you change " +
                "them). Changes there are written straight to this config file. Set " +
                "to None to disable the key entirely.");

            // ---- Fonts: three areas, each split into name vs. distance text.
            // Multipliers rather than absolute sizes on purpose: each widget's
            // own size is tuned relative to its neighbours (a player's name is
            // deliberately bigger than an item ping's, a compass marker's
            // smaller than either), and one flat pixel size per area would
            // flatten that hierarchy. 1 = exactly the sizes the mod ships with.
            //
            // On-screen vs. off-screen is a state the *same* label passes
            // through - it isn't two different widgets - so a label crossing
            // that boundary eases between the two scales along with the
            // position transition (IndicatorManager.TransitionState) rather
            // than snapping.
            OnScreenNameFontScale = config.Bind(
                "Fonts", "on-screen-name-scale", 1f,
                new ConfigDescription(
                    "Scales every name label drawn on a thing you can actually see " +
                    "(player labels, item/creature pings). 1 keeps the shipped sizes.",
                    new AcceptableValueRange<float>(0.5f, 2f)));

            OnScreenDistanceFontScale = config.Bind(
                "Fonts", "on-screen-distance-scale", 1f,
                new ConfigDescription(
                    "Same, for the distance sub-line under an on-screen label.",
                    new AcceptableValueRange<float>(0.5f, 2f)));

            OffScreenNameFontScale = config.Bind(
                "Fonts", "off-screen-name-scale", 1f,
                new ConfigDescription(
                    "Scales every name label on a thing that's currently off-screen, i.e. " +
                    "clamped to the edge with an arrow. Set this below on-screen-name-scale " +
                    "to keep a crowded screen edge quieter without shrinking the labels on " +
                    "things you're actually looking at.",
                    new AcceptableValueRange<float>(0.5f, 2f)));

            OffScreenDistanceFontScale = config.Bind(
                "Fonts", "off-screen-distance-scale", 1f,
                new ConfigDescription(
                    "Same, for the distance sub-line under an off-screen (edge-clamped) label.",
                    new AcceptableValueRange<float>(0.5f, 2f)));

            CompassNameFontScale = config.Bind(
                "Fonts", "compass-name-scale", 1f,
                new ConfigDescription(
                    "Scales the name label above each compass-tape marker (only shown at " +
                    "all when Compass/show-names is on).",
                    new AcceptableValueRange<float>(0.5f, 2f)));

            CompassDistanceFontScale = config.Bind(
                "Fonts", "compass-distance-scale", 1f,
                new ConfigDescription(
                    "Scales the distance sub-label under each compass-tape marker.",
                    new AcceptableValueRange<float>(0.5f, 2f)));

            // ---- Player labels.
            EnablePlayerLabels = config.Bind(
                "Player-Labels", "enable-player-labels", true,
                "Master switch for Sense of Direction's player labels. Off hides them " +
                "entirely (vanilla's own name labels are unaffected either way).");

            PlayerLabelToggleKey = config.Bind(
                "Player-Labels", "toggle-key", KeyCode.G,
                "Key that shows/hides player labels, per display-mode below. Only a " +
                "single key can be bound here, not a combination like Ctrl+G.");

            PlayerLabelDisplayMode = config.Bind(
                "Player-Labels", "display-mode", LabelDisplayMode.Toggle,
                "Toggle: press toggle-key to show/hide labels. AlwaysOn: labels are " +
                "always visible (toggle-key does nothing). Hold: labels show while " +
                "toggle-key is held down.");

            HoldShownDuration = config.Bind(
                "Player-Labels", "hold-shown-duration", 1.5f,
                new ConfigDescription(
                    "Hold mode only: how many seconds labels stay visible after the key is " +
                    "released (also covers a quick tap).",
                    new AcceptableValueRange<float>(0f, 10f)));

            PlayerLabelMaxDistanceMeters = config.Bind(
                "Player-Labels", "max-distance-meters", 1000f,
                new ConfigDescription(
                    "A player's label stops showing beyond this distance. Lower it if " +
                    "you'd rather only track teammates who are actually nearby, or raise " +
                    "it to cover longer sightlines.",
                    new AcceptableValueRange<float>(50f, 2000f)));

            PlayerLabelNameFontSize = config.Bind(
                "Player-Labels", "name-font-size", 28f,
                new ConfigDescription(
                    "Base font size of each player's name label, before the Fonts section's " +
                    "on-screen/off-screen name scale is applied on top.",
                    new AcceptableValueRange<float>(10f, 60f)));

            PlayerLabelDistanceFontSize = config.Bind(
                "Player-Labels", "distance-font-size", 18f,
                new ConfigDescription(
                    "Base font size of the distance sub-line under each name label, before " +
                    "the Fonts section's distance scale is applied on top.",
                    new AcceptableValueRange<float>(8f, 40f)));

            ShowPlayerLabelDistance = config.Bind(
                "Player-Labels", "show-distance", true,
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
                "Sense of Direction's labels are the only ones ever shown. Normally the " +
                "two systems hand off to each other instead.");

            ShowPlayerSkeleton = config.Bind(
                "Player-Labels", "show-skeleton", false,
                "Draw each player's skeleton over the world, visible through walls and " +
                "terrain. Shows and hides together with the labels themselves, so the " +
                "display-mode and toggle-key above apply to it too. Off by default: it " +
                "gives away a lot more than a name label does.");

            PlayerSkeletonLineThickness = config.Bind(
                "Player-Labels", "skeleton-line-thickness", 2f,
                new ConfigDescription(
                    "How thick the skeleton's bones are drawn. Thickness stays the same " +
                    "on screen at any distance, so a far-off player is still a readable " +
                    "stick figure rather than a hairline.",
                    new AcceptableValueRange<float>(1f, 8f)));

            PlayerSkeletonUseCharacterColor = config.Bind(
                "Player-Labels", "skeleton-use-character-color", true,
                "Color each skeleton with that player's own character color. Off draws " +
                "them all in the vanilla name-label color instead. Separate from " +
                "use-character-color above, so the skeletons and the name labels can be " +
                "colored differently.");

            ShowPlayerSkeletonJoints = config.Bind(
                "Player-Labels", "skeleton-show-joints", true,
                "Draw a dot at each joint of the skeleton, on top of the bones.");

            // ---- Campfire.
            EnableCampfireIndicator = config.Bind(
                "Campfire", "enable-campfire-indicator", true,
                "Show an always-on edge-of-screen indicator pointing at the current " +
                "segment's campfire (the one you're trying to reach next), so you " +
                "always know which way to go. Turn it off if you'd rather find your own way " +
                "up and keep the rest of the mod.");

            ShowCampfireDistance = config.Bind(
                "Campfire", "show-distance", true,
                "Show the distance sub-line under the campfire indicator.");

            HideCampfireName = config.Bind(
                "Campfire", "hide-name", true,
                "Never show the campfire's name label (\"Campfire\") on the compass. " +
                "On by default since the icon alone already makes it obvious which " +
                "marker is the campfire.");

            // ---- Pings. remove-visibility-cutoff is bound first because it's
            // the foundation the rest of this section sits on: with it off,
            // vanilla never spawns a distant ping's visual at all, so there's
            // nothing for the scaling/ripple/indicator settings to act on.
            RemoveVisibilityCutoff = config.Bind(
                "Pings", "remove-visibility-cutoff", true,
                "Vanilla silently refuses to even spawn a ping's visual once its " +
                "pinging player is more than ~45m from you; turning this on makes " +
                "far pings still show up at all. Most of the other Pings settings only " +
                "matter once this is on.");

            EnablePingScaling = config.Bind(
                "Pings", "enable-scaling", true,
                "Scale ping visuals up the further away they are, well past vanilla's " +
                "own hard-capped scale, so far pings stay easy to spot.");

            PingScaleMultiplier = config.Bind(
                "Pings", "scale-multiplier", 1f,
                new ConfigDescription(
                    "Extra multiplier applied on top of vanilla's own (uncapped, see " +
                    "remove-visibility-cutoff) ping scale. 1x is vanilla's own uncapped " +
                    "size; higher makes every ping bigger regardless of distance.",
                    new AcceptableValueRange<float>(0.5f, 3f)));

            EnablePingRipple = config.Bind(
                "Pings", "enable-ripple", true,
                "Show an expanding ring in the pinging player's own character color " +
                "at the ping location, so it reads against similarly-colored terrain. " +
                "Press your ping key to preview this here.");

            EnablePingOffScreenIndicator = config.Bind(
                "Pings", "enable-offscreen-indicator", true,
                "Show an edge-of-screen arrow pointing toward an active ping when it's " +
                "off-screen, same mechanism as the player-label/campfire indicators. " +
                "Only the arrow; the distance line below is show-distance's own call. " +
                "Does nothing while General/ping-placement is CompassOnly, which hides " +
                "the whole edge-of-screen widget anyway.");

            ShowPingDistanceLabel = config.Bind(
                "Pings", "show-distance", true,
                "Show a distance sub-line under the ping indicator.");

            EnableGhostPing = config.Bind(
                "Pings", "enable-ghost-ping", true,
                "Let dead or unconscious players keep pinging (vanilla blocks pinging " +
                "once passed out, well before actual death), colored using their own " +
                "character color same as when alive. Requires both sides to have this " +
                "mod installed.");

            // ---- Ping audio: its own section rather than four more keys in
            // Pings, since all four are inert unless the boost is on.
            EnablePingAudioBoost = config.Bind(
                "Ping-Audio", "enable-audio-boost", true,
                "Drastically reduce the ping sound's distance falloff so it's audible " +
                "from much further away, while sounding unchanged up close. The rest of " +
                "this section does nothing while this is off.");

            PingAudioRangeMeters = config.Bind(
                "Ping-Audio", "range-meters", 600f,
                new ConfigDescription(
                    "Ping sound's max audible range (vanilla is 150).",
                    new AcceptableValueRange<float>(150f, 2000f)));

            PingAudioMinDistanceMeters = config.Bind(
                "Ping-Audio", "min-distance-meters", 10f,
                new ConfigDescription(
                    "Distance under which the ping sound plays at full volume before it " +
                    "starts falling off toward range-meters.",
                    new AcceptableValueRange<float>(1f, 50f)));

            PingAudioVolumeMultiplier = config.Bind(
                "Ping-Audio", "volume-multiplier", 0.85f,
                new ConfigDescription(
                    "Multiplier on the ping sound's own base (close-range) volume. The " +
                    "far-range audibility boost also makes it slightly too loud up close, " +
                    "so this trims that back down.",
                    new AcceptableValueRange<float>(0.3f, 1.5f)));

            // ---- Ping anti-spam: likewise its own section - five knobs that
            // are all inert unless the throttle is on, and all describe one
            // mechanism.
            EnablePingAntiSpam = config.Bind(
                "Ping-Anti-Spam", "enable-anti-spam", true,
                "Rate-limits how often *other* players' pings actually render/play once " +
                "they're pinging rapidly, so spamming the ping key isn't more disruptive " +
                "than vanilla now that pings are bigger/louder. A short burst always goes " +
                "through instantly; only once someone keeps spamming does \"slow mode\" " +
                "kick in, queueing further pings to arrive at a throttled rate instead " +
                "(never silently dropped, unless the queue itself is full; see " +
                "max-queue-length). Never applies to your own pings.");

            PingAntiSpamFreeSpamCount = config.Bind(
                "Ping-Anti-Spam", "free-spam-count", 3,
                new ConfigDescription(
                    "How many pings in a row from the same player always show up instantly " +
                    "before slow mode kicks in.",
                    new AcceptableValueRange<int>(1, 20)));

            PingAntiSpamSlowModeIntervalSeconds = config.Bind(
                "Ping-Anti-Spam", "slow-mode-interval-seconds", 0.5f,
                new ConfigDescription(
                    "Once slow mode kicks in, queued pings from that player are spaced at " +
                    "least this far apart before they're actually shown to you.",
                    new AcceptableValueRange<float>(0.1f, 5f)));

            PingAntiSpamMaxQueueLength = config.Bind(
                "Ping-Anti-Spam", "max-queue-length", 2,
                new ConfigDescription(
                    "How many of a spamming player's pings can be queued up waiting to " +
                    "show at once while in slow mode. Any further ping while the queue's " +
                    "already full is dropped entirely rather than queued.",
                    new AcceptableValueRange<int>(1, 10)));

            PingAntiSpamResetSeconds = config.Bind(
                "Ping-Anti-Spam", "reset-seconds", 2f,
                new ConfigDescription(
                    "How long a player has to go without pinging (with their queue fully " +
                    "drained) before slow mode fully resets to normal.",
                    new AcceptableValueRange<float>(1f, 30f)));

            // ---- Item pings: what a ping highlights, and how that highlight looks.
            EnableItemPings = config.Bind(
                "Item-Pings", "enable-item-pings", true,
                "Highlight nearby items/luggage when you ping near them, with a name " +
                "and distance label, as a native replacement for the (broken/unmaintained) " +
                "PingItems mod by memiczny.");

            ItemPingDurationSeconds = config.Bind(
                "Item-Pings", "duration-seconds", 6f,
                new ConfigDescription(
                    "How long an item/luggage highlight stays visible before fading " +
                    "out (ends early regardless if the item is picked up or the " +
                    "luggage is opened).",
                    new AcceptableValueRange<float>(2f, 20f)));

            EnableItemPingGrouping = config.Bind(
                "Item-Pings", "enable-grouping", true,
                "Group multiple nearby items of the same kind into a single " +
                "highlight showing a count (e.g. \"3x Coconut\") instead of one " +
                "highlight per item.");

            EnableCreaturePings = config.Bind(
                "Item-Pings", "enable-creature-pings", true,
                "Also highlight creatures (beetles, spiders, zombies, ...) when pinged, " +
                "same as items/luggage. OFF leaves creature pings behaving like vanilla " +
                "(so it won't work for them).");

            UseNativeItemPingIcons = config.Bind(
                "Item-Pings", "use-native-icons", true,
                "Show the item's own in-game icon (the art its inventory slot uses, " +
                "e.g. an actual bandage for a pinged bandage) as the highlight's " +
                "crosshair and its compass marker, instead of the mod's generic " +
                "item-ping icon. Only items (and the campfire) have an icon in the " +
                "game at all; luggage, creatures and hazards keep the generic icon " +
                "either way. Works with custom modded items as well.");

            ItemPingNameMode = config.Bind(
                "Item-Pings", "name-mode", ItemPings.ItemPingNameMode.Always,
                "Always: every highlight shows what it is. HideWhenIconShown: " +
                "anything already showing its own in-game icon (see use-native-icons) " +
                "drops its name, since the icon says what it is (luggage, creatures and " +
                "hazards have no icon, so they keep theirs). Never: no names at all. A " +
                "grouped ping keeps its count regardless (a hidden name still shows \"3x\").");

            ShowItemPingDistance = config.Bind(
                "Item-Pings", "show-distance", true,
                "Show a distance sub-line under the item/luggage highlight.");

            EnableItemPingOffScreenIndicator = config.Bind(
                "Item-Pings", "enable-offscreen-indicator", true,
                "Show an edge-of-screen arrow pointing toward a highlighted item/" +
                "luggage when it's off-screen, same mechanism as the ping indicator. " +
                "Does nothing while General/item-ping-placement is CompassOnly, which " +
                "hides the whole edge-of-screen widget anyway.");

            // ---- Item-ping detection: the "what did that ping actually hit"
            // tuning, split out from the Item-Pings tab above (which is about
            // what the resulting highlight *looks* like). Mostly leave alone.
            ItemPingDetectionRadiusMeters = config.Bind(
                "Item-Ping-Detection", "item-radius-meters", 2f,
                new ConfigDescription(
                    "How close a ping needs to land to an item for it to get highlighted.",
                    new AcceptableValueRange<float>(0.5f, 10f)));

            ItemPingCrossKindRadiusMeters = config.Bind(
                "Item-Ping-Detection", "cross-kind-radius-meters", 0.75f,
                new ConfigDescription(
                    "How close a *different* kind of item has to be to the item you " +
                    "actually pinged before it also gets highlighted. Items of the same " +
                    "kind still group together across the full item-radius-meters (that's " +
                    "what makes a \"2x COCONUT\" grouping), but a different item only " +
                    "counts if it was pretty much directly aimed at too, so pinging one " +
                    "item in a luggage doesn't drag in an unrelated one sitting next to it.",
                    new AcceptableValueRange<float>(0f, 10f)));

            LuggagePingDetectionRadiusMeters = config.Bind(
                "Item-Ping-Detection", "luggage-radius-meters", 3.5f,
                new ConfigDescription(
                    "Same as item-radius-meters, but for luggage, which is a bigger " +
                    "target.",
                    new AcceptableValueRange<float>(0.5f, 15f)));

            EnableItemPingHitAssist = config.Bind(
                "Item-Ping-Detection", "enable-hit-assist", true,
                "Widen the ping's own aim raycast so it can land directly on an " +
                "item/luggage's own collider (not just terrain/ground), instead of " +
                "phasing through to whatever's behind it. Fixes hard-to-ping items " +
                "like a coconut up a tree or a small dropped item. Off falls back to " +
                "vanilla's own terrain-only ping raycast.");

            ItemPingHitboxRadiusMeters = config.Bind(
                "Item-Ping-Detection", "hitbox-radius-meters", 0.35f,
                new ConfigDescription(
                    "Only used when enable-hit-assist is on. Treats the ping raycast as a " +
                    "sphere of this radius instead of an infinitely-thin line, so aiming " +
                    "near (not pixel-perfect on) an item's collider still hits it. 0 " +
                    "disables the sphere, falling back to a plain (still item-widened) " +
                    "raycast.",
                    new AcceptableValueRange<float>(0f, 1.5f)));

            EnableItemPingRayAssist = config.Bind(
                "Item-Ping-Detection", "enable-ray-assist", true,
                "Also count an item/luggage as pinged if it's close enough to your " +
                "aim line, independent of physics entirely. Needed for items that " +
                "aren't pushable/hittable until first picked up (an unpicked coconut " +
                "on a tree, berries on a bush, something freshly spawned from opened " +
                "luggage); their collider is disabled until then, so no physics " +
                "raycast (not even enable-hit-assist's) can ever land on them.");

            ItemPingRayAssistRadiusMeters = config.Bind(
                "Item-Ping-Detection", "ray-assist-radius-meters", 0.6f,
                new ConfigDescription(
                    "Only used when enable-ray-assist is on. How far off your exact aim " +
                    "line an item/luggage can be and still count as pinged.",
                    new AcceptableValueRange<float>(0f, 2f)));

            // ---- Compass.
            EnableCompass = config.Bind(
                "Compass", "enable-compass", true,
                "Master switch for the top-of-screen compass tape. Off hides it " +
                "entirely regardless of any individual mechanic's placement setting.");

            CompassWidthPixels = config.Bind(
                "Compass", "width-pixels", 640f,
                new ConfigDescription(
                    "Width of the compass tape, in pixels at the 1920-wide reference " +
                    "resolution (scales with actual resolution same as everything " +
                    "else). Wider shows more of the horizon at once.",
                    new AcceptableValueRange<float>(300f, 1400f)));

            CompassMarkerGapPixels = config.Bind(
                "Compass", "marker-gap-pixels", 40f,
                new ConfigDescription(
                    "Vertical gap between the tick row and the marker baseline below it, " +
                    "on top of a small fixed minimum. Raise this for more breathing room " +
                    "(e.g. after turning on show-names).",
                    new AcceptableValueRange<float>(40f, 200f)));

            CompassVerticalOffsetPixels = config.Bind(
                "Compass", "vertical-offset-pixels", 28f,
                new ConfigDescription(
                    "Gap between the top of the screen and the compass tape.",
                    new AcceptableValueRange<float>(0f, 300f)));

            CompassHorizontalOffsetPixels = config.Bind(
                "Compass", "horizontal-offset-pixels", 0f,
                new ConfigDescription(
                    "Horizontal offset from top-center. 0 keeps it centered; " +
                    "positive shifts right, negative shifts left (e.g. to dodge " +
                    "another HUD mod's own top-of-screen element).",
                    new AcceptableValueRange<float>(-800f, 800f)));

            CompassFovDegrees = config.Bind(
                "Compass", "fov-degrees", 150f,
                new ConfigDescription(
                    "How much of the horizon (in degrees) is visible on the tape at " +
                    "once before a heading/marker slides off the edge. Lower feels " +
                    "closer to your actual field of view; higher gives more lead time " +
                    "for things approaching from the side.",
                    new AcceptableValueRange<float>(60f, 180f)));

            CompassIconSizePixels = config.Bind(
                "Compass", "icon-size-pixels", 26f,
                new ConfigDescription(
                    "Size of each marker's icon on the compass.",
                    new AcceptableValueRange<float>(12f, 64f)));

            CompassElevationThresholdMeters = config.Bind(
                "Compass", "elevation-threshold-meters", 3f,
                new ConfigDescription(
                    "A marker only gets an up/down elevation arrow once its target " +
                    "is at least this many meters above/below you, which avoids a " +
                    "flickering arrow for things that are roughly level with you.",
                    new AcceptableValueRange<float>(0.5f, 30f)));

            CompassShowDegreeNumbers = config.Bind(
                "Compass", "show-degree-numbers", false,
                "Show a numeric heading (e.g. \"105\") at every non-cardinal tick " +
                "instead of leaving it as a plain unlabeled line. N/E/S/W are always " +
                "lettered either way.");

            CompassShowNames = config.Bind(
                "Compass", "show-names", false,
                "Show a name label above each compass marker that has one (players, " +
                "item/creature pings, the campfire). Distances still show independently " +
                "of this setting.");

            CompassShowDistances = config.Bind(
                "Compass", "show-distances", true,
                "Show a distance sub-label under each compass marker.");

            CompassRequiresHoldingItem = config.Bind(
                "Compass", "requires-holding-item", false,
                "Only show the compass tape while the local player is actually " +
                "holding an in-game Compass item, instead of it always being " +
                "visible.");

            CompassLineColor = config.Bind(
                "Compass", "line-color", SenseOfDirection.Compass.CompassLineColor.White,
                "Base color of the compass tape's heading ticks/labels and baseline " +
                "stripe (true north keeps its own dark red accent regardless).");

            CompassLineThicknessMultiplier = config.Bind(
                "Compass", "line-thickness-multiplier", 1f,
                new ConfigDescription(
                    "Scales the thickness of the compass tape's tick lines (both " +
                    "cardinal and minor) and its baseline stripe. 1 keeps the " +
                    "shipped thickness; higher values make the lines bolder.",
                    new AcceptableValueRange<float>(0.5f, 3f)));

            CompassClampIconsToEdge = config.Bind(
                "Compass", "clamp-icons-to-edge", false,
                "Markers that would otherwise not be visible (outside the compass " +
                "FOV window) are instead clamped to the nearest left/right edge of " +
                "the tape and shown dimmed, like a mini radar, instead of not " +
                "appearing at all.");

            CompassColorPlayerLabels = config.Bind(
                "Compass", "color-player-labels", false,
                "Tint a player's name/distance labels on the compass in their own " +
                "character color instead of plain white, matching how ping/item " +
                "ping labels are already colored.");

            // ---- Pirate's Compass: the in-game Pirate's Compass item already
            // makes requires-holding-item show the compass tape while it's held
            // (any CompassPointer-bearing item does), but the tape itself has no
            // way to represent what that specific compass actually points at -
            // the nearest unopened luggage. This adds a real indicator for that.
            EnablePirateCompassLuggageIndicator = config.Bind(
                "Pirate-Compass", "enable-pirate-compass-luggage-indicator", true,
                "While holding a Pirate's Compass, show an indicator pointing at the " +
                "nearest unopened luggage - the same target its own in-game needle " +
                "points at, made legible as a real edge-of-screen/compass marker " +
                "with a distance label instead of only a wobbling 3D needle.");

            ShowPirateCompassLuggageName = config.Bind(
                "Pirate-Compass", "show-luggage-name", true,
                "Show a name label (\"LUGGAGE\") above the Pirate's Compass indicator.");

            ShowPirateCompassLuggageDistance = config.Bind(
                "Pirate-Compass", "show-luggage-distance", true,
                "Show a distance sub-label under the Pirate's Compass indicator.");

            EnablePirateCompassLuggageOffScreenIndicator = config.Bind(
                "Pirate-Compass", "enable-off-screen-indicator", true,
                "Show an off-screen arrow pointing toward the nearest unopened luggage " +
                "while it isn't in view. Off shows the indicator only once it's " +
                "actually on screen.");

            // ---- Luggage Ping: inspired by the "Compass UI" mod's own suitcase-
            // ping key, for players coming from that mod. Press the key to
            // highlight every unopened luggage within radius-meters, using the
            // same item-ping highlight (name/distance label, off-screen arrow,
            // compass marker) item-ping-placement already routes - purely local,
            // never sent to other players, unlike a real ping.
            EnableLuggagePing = config.Bind(
                "Luggage-Ping", "enable-luggage-ping", true,
                "Master switch: press LUGGAGE PING KEY below to highlight every " +
                "unopened luggage within the set radius (100m by default) of you, " +
                "visible only to yourself. Comes with a cooldown by default (15s) " +
                "which is able to be changed through the mod's config, the same " +
                "goes for the radius.");

            LuggagePingKey = config.Bind(
                "Luggage-Ping", "key", KeyCode.T,
                "Key that triggers a luggage ping (see above) if enabled and not on a cooldown.");

            LuggagePingRadiusMeters = config.Bind(
                "Luggage-Ping", "radius-meters", 100f,
                new ConfigDescription(
                    "How far around you luggage gets highlighted. Capped well below " +
                    "the level's own size so this can't turn into a full map-wide " +
                    "luggage ESP.",
                    new AcceptableValueRange<float>(10f, 250f)));

            LuggagePingDurationSeconds = config.Bind(
                "Luggage-Ping", "duration-seconds", 6f,
                new ConfigDescription(
                    "How long each highlighted luggage stays visible before fading " +
                    "out (ends early regardless if it's opened first).",
                    new AcceptableValueRange<float>(2f, 20f)));

            LuggagePingCooldownSeconds = config.Bind(
                "Luggage-Ping", "cooldown-seconds", 15f,
                new ConfigDescription(
                    "How long you have to wait between luggage pings. Trying to " +
                    "ping again while this is still running shows a brief on-screen " +
                    "reminder instead of doing nothing silently. 0 disables the " +
                    "cooldown entirely.",
                    new AcceptableValueRange<float>(0f, 120f)));

            // ---- Ghost free-cam. The three host-controlled settings are bound
            // first, ahead of the purely-local ones, so the section reads
            // top-down as "what the room decides" then "what you decide".
            EnableGhostFreeCam = config.Bind(
                "Ghost-Free-Cam", "enable-ghost-free-cam", true,
                "Lets dead players fly a free camera instead of being stuck in " +
                "vanilla's third-person spectate view. Unlike every other setting in " +
                "this mod, this one and the two below it are host-controlled: only " +
                "the room host's own value for these three settings ever takes " +
                "effect for every player, the same way enable-ghost-ping requires " +
                "both sides to have this mod installed. That's because letting each " +
                "client fly unlimited distances would be an unfair, effectively " +
                "ESP-like advantage other players in the same run never agreed to. " +
                "If the host doesn't have this mod installed, ghost free-cam simply " +
                "doesn't work for anyone, same as ghost pinging. Your own value here " +
                "still matters if you end up being the host.");

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
                "map. Although note that the leash is what keeps this mechanic from " +
                "being overpowered.");

            GhostFreeCamToggleKey = config.Bind(
                "Ghost-Free-Cam", "toggle-key", KeyCode.B,
                "Purely local. Key that toggles free-fly camera mode on/off while " +
                "you're dead and spectating; each player binds their own. Only does " +
                "anything while enable-ghost-free-cam ends up effectively on (see that " +
                "setting for how that's decided).");

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

            GhostFreeCamShowCrosshair = config.Bind(
                "Ghost-Free-Cam", "show-crosshair", true,
                "Purely local. Shows a small reticle at the center of the screen " +
                "while free-cam is engaged, so you have something to aim regular " +
                "pings at (spectate mode otherwise has no crosshair at all).");

            GhostFreeCamShowKeyHint = config.Bind(
                "Ghost-Free-Cam", "show-key-hint", true,
                "Purely local. Shows a key badge + short label near vanilla's own " +
                "\"you are a ghost\" panel reminding you which key toggles free-cam " +
                "(and whether it'll engage or disengage), since vanilla's UI never " +
                "mentions this mod's keybind at all.");

            HideAllGhosts = config.Bind(
                "Ghost-Free-Cam", "hide-all-ghosts", false,
                "Purely local. Hides every dead player's ghost body from your own " +
                "view entirely. Doesn't affect anyone else, and doesn't affect your " +
                "own ability to spectate/free-cam while dead yourself.");

            // ---- Debug, bound last so it's the final tab: dev/QA settings
            // belong behind everything user-facing, not ahead of it.
            EnableDebugLogging = config.Bind(
                "Debug", "enable-debug-logging", false,
                "Log extra diagnostic detail to the BepInEx console/log file.");

            EnableIndicatorTestHarness = config.Bind(
                "Debug", "enable-indicator-test-harness", false,
                "Spawn a handful of fixed dummy world points around the camera to " +
                "visually verify the edge-of-screen indicator framework. Dev/QA " +
                "tool only; leave off for normal play.");

            EnableZombieDebugEsp = config.Bind(
                "Debug", "enable-zombie-debug-esp", false,
                "Dev/QA aid: always-visible edge-of-screen label for every " +
                "naturally-spawned zombie in the level, through walls, to speed up " +
                "testing zombie-ping detection without hunting a whole level for a " +
                "rare spawn. Not a real feature; leave off for normal play.");

            EnableGhostFreeCamKeyHintPreview = config.Bind(
                "Debug", "enable-ghost-free-cam-key-hint-preview", false,
                "Dev/QA aid: always shows the ghost free-cam key hint badge/label " +
                "(toggle-key still flips it between its 'go into'/'leave' text), " +
                "even while alive, to check its look without dying first. Not a " +
                "real feature; leave off for normal play.");
        }
    }
}
