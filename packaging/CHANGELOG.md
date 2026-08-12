## 1.1.1

- A lot of things that couldn't be pinged before now can be: Scout amulets still in their statue, the Belltower, the Flytrap, the floating Ghost Ball thingy, and every new trap in The Citadel.
- Added new portal indicator (with its own icon) for the new Nadir *biome*, instead of still trying to point toward the Peak biome's flag.
- Pings now ignore all items/objects stuck to the player (arrows, throns, ticks, cacti) to still be able to ping even if they are obstructing your vision.
- Your own worn backpack isn't accidentally pingable anymore. Other players' worn backpacks are more difficult to ping (to not accidentally ping it and all its items).
- Added Traditional Chinese translations.
- Every missing creature, hazard and trap the mod names itself when pinging it is now translated into all 15 languages (instead of being hard-coded in English).
- Fixed a recurring stutter the mod caused every 5 seconds while rescanning the entire biome for new pingable entries. This is optimized greatly now.

## 1.1.0

- **PEAK 2.0.a compatibility update**. The recent major update broke lot's of stuff: like the compass not appearing at all or pinging not working (and breaking the vanilla ping with it).
- Note: some other new things still don't work though (for example the player character has new colliders which break pinging in some cases or the new hazards not being pingable), but I'll fix this later after taking a short nap.

## 1.0.6

- Added a new **luggage-indicator-display-mode** setting under *Pirate-Compass* (also in the Quick Setup panel) which controls when the Pirate's Compass' luggage indicator shows (needing to actively hold it is the default still).
- Fixed major issue where the mod tried to find native game assets to reuse (font, icons) in the main menu where they weren't loaded into memory yet causing massive performance issues.
- Fixed magnifying glass border having thinner corners than edges.

## 1.0.5

- The campfire indicator now points at the **Peak** once the last campfire on the map is lit with its own icon instead of trying to indicate the next campfire.
- Added a new host-only **Compass-Items** config section (only available in the mod's config) which contains 6 new settings:
    - **enable-compass-at-campfires** (enabled by default): spawns an extra compass next to any world spawned backpack.
    - **campfire-compass-only-when-needed** (also enabled by default): restricts the previous setting by only spawning the extra compass when in co-op and the host has compass' display-mode not set to *AlwaysOn*.
    - The other 4 settings (config-only, all off by default) let the **host** give opened Luggage an extra chance to also contain a regular **Compass** or a **Pirate's Compass**. Only rolled when a luggage opens with a free item slot left, and the compass is only ever *added* into that free slot. Nothing the game (or another mod) put in a luggage is ever replaced, and the game's own loot odds are left completely untouched.
- Added a new **pirate-display-mode** option under *Compass* to control compass tape visibility separately for the regular Compass item and Pirate's Compass.
- Fixed the game's own `MapHandler.CurrentCampfire` throwing once the last campfire was lit, which silently broke every ping from that point on.

## 1.0.4

- Ghost free-cam keybinds now aren't hard-coded anymore. Instead the user-defined in-game navigation bindings are used (movement binds, sprint, crouch, jump). Also adds 2 new settings (config-only) where you can rebind the secondary ascend/descend keys (by default 'E' / 'Q' respectively). Thanks to **Cat-As$-Trophy** (fire name) for pointing this out!
- Added 2 new settings to the Quick Setup panel: **badge-size-pixels** (under Player-Labels): controls the badge icon size below player labels, and **indicator-icon-size-multiplier** (under Misc): controls the on-/off-screen indicator's icon size (pings, item pings, campfire).
- Added a new **anti-overlap-animation-speed-multiplier** setting (under Misc) to slow down and delay the interpolating of the overlap avoidance mechanic (so that labels don't move around so much, if you want it to be calmer).
- Extended *require-holding-item* to be **display-mode** (under Compass) which includes more conditions when the compass tape at the top should be shown: AlwaysOn (default), MainInventory, Carried, or HoldingItem.
- Added a new **color-player-labels** setting (config-only) to show compass player labels (and it's distance label) in the player's color (off by default though, because if player labels are white they are easier to distinguish as being player specific labels when you ask me). 
- Actually added a proper outline to the vertical height diff arrow in the compass. Also vertically centered it fr this time.
- Added a subtle pop animation in the compass to newly pinged luggage through the luggage area pinging mechanic.
- Renamed the `General` tab in the Quick Setup panel to `Misc`.
- Fixed *campfire* and *ping* in Quick Setup panel's preview wrongly showing the vertical diff arrow.

## 1.0.3

- Fixed camera getting stuck/frozen on becoming unconscious when [PEAKSleepTalk](https://thunderstore.io/c/peak/p/Lokno/PEAKSleepTalk) is installed (or similar mods) and not being able to spectate other players anymore. This also fixes the ghost free-cam not working when said mod is installed.
- Now you can ping while unconscious.
- 'E' / 'Q' to ascend/descend respectively now do not work while unconscious (to stop unintended behavior as the 'E' key speeds up the dying process).

## 1.0.2

- Default ghost free-cam keybind is now `B` (was *V* before but I didn't realize that voice chat was bound to it, mb).
- Reworked the ghost free-cam toggle label to match the game's scheme even more and fixed a misplacement issue.
- Added 1 new setting to the Quick Setup panel: **hide-name** (Campfire): hides the name label of the campfire on the compass (enabled by default).
- Ported 2 settings from the mod's config to the Quick Setup panel: **enable-luggage-ping**, and **luggage-ping-key** (both under Item-Pings).
- Fixed compass and on-/off-screen indicators permanently as well as statically staying when switching scenes before the indicators fade.
- Fixed the Quick Setup preview not showing the campfire name label.
- Fixed aim-assist still being active for lit campfires thus blocking potential item pings.
- Simplified font of the key badge in the footer of the Quick Setup panel.

## 1.0.1

- Added client-sided luggage pinging within a designated radius relative to the player (enabled by default; default key: `T`). Has an optional cooldown mechanic to balance it out a little (although able to be fully disabled). Thanks to **KrsnaCallisto** for suggesting this!
- Optimized initialization of item and regular pings to never stutter initially even more.
- Fixed an issue that sometimes wouldn't allow items in just opened luggage to be item pinged before waiting for the periodic item list update to happen.
- Added widescreen support for the Quick Setup panel.
- Minor icon sizing and position adjustments.

## 1.0.0

Initial release.
