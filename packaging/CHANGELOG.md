## 1.0.3

- Fixed camera getting stuck/frozen on becoming unconscious when [PEAKSleepTalk](https://thunderstore.io/c/peak/p/Lokno/PEAKSleepTalk) is installed (or similar mods) and not being able to spectate other players anymore. This also fixes the ghost free-cam not working when said mod is installed.

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
