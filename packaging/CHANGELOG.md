## 1.0.5

- Added a new **Compass-Items** config section. Its first two settings (both config-only, both **on** by default) let the **host** place an extra regular **Compass** on the ground next to the backpack at every campfire — vanilla only ever gives out one compass per run, which leaves the rest of a co-op group without one. **campfire-compass-only-when-needed** limits that to co-op runs where the host's *Compass/display-mode* isn't `AlwaysOn` (on `AlwaysOn` nobody needs to hold a compass anyway); turn it off to get one at every campfire unconditionally.
- The same **Compass-Items** section's other 4 settings (config-only, all off by default) let the **host** give opened Luggage an extra chance to also contain a regular **Compass** or a **Pirate's Compass**. Only rolled when a luggage opens with a free item slot left, and the compass is only ever *added* into that free slot — nothing the game (or another mod) put in a luggage is ever replaced, and the game's own loot odds are left completely untouched. Explorer's Luggage is unaffected (both its slots are always full). Host-only: luggage contents are spawned by the host, so only the host's settings apply.

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
