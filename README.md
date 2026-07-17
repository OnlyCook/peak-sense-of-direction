<!-- GENERATED FILE — do not edit by hand.
     Source: packaging/README.md + packaging/README.github-extra.md
     Regenerate with: bash packaging/gen-readme.sh -->

**The only mod for directions you'll ever need, highly customizable and very user friendly to set up!**

This mod keeps every teammate on your screen even when they're out of view, makes pings actually visible and useful (you can even ping items/objects), adds a powerful compass, and much more.

<img width="1080" height="270" alt="player-labels-item-pings" src="https://github.com/OnlyCook/peak-sense-of-direction/blob/main/packaging/player-labels-item-pings.png?raw=true" />

Client-sided, no other player needs it installed for this to work. 

Fully localized in all 14 languages the game ships with: English, Français, Italiano, Deutsch, Español (España), 日本語, 한국어, Português (Brasil), Русский, 简体中文, Español (Latinoamérica), Українська, Polski, Türkçe.

---

## Features (all optional, all configurable)

- **Player labels**: name, distance, host/dead/unconscious badges, character-color matched text, edge-of-screen clamping so a label never actually disappears when they are out of view (default toggle key: **`G`**)
- **Better pings**: scale with distance so far pings aren't invisible, a 3D ripple so it reads against matching terrain, and much less audio falloff so you can hear them from far away
- **Item pings**: ping an item, piece of luggage, hazard, or creature and get a name/icon + distance highlight on it, with aim-assist so it's easier to ping them (also works with modded items)
- **Compass**: a top-of-screen tape showing players, pings, the campfire, and item pings all at once, as an alternative or supplement to the edge-of-screen labels
- **Campfire indicator**: always know which way the next campfire is
- **Ghost free-cam**: dead/unconscious players get a free-flying camera instead of being locked to a teammate's view, so they can scout ahead and still ping hazards/loot for alive players
- **Label overlap prevention** (on by default): nudges labels apart so they don't stack into an unreadable pile
- **Pirate's Compass support**: holding one shows you the nearest unopened luggage visually (you can also set that you must hold any compass item for the compass ui to appear)

<img width="808" height="152" alt="compass" src="https://github.com/OnlyCook/peak-sense-of-direction/blob/main/packaging/compass.png?raw=true" />

## Quick setup panel

> Open with `F8` at any time.

Every visual setting laid out over a live preview of what it actually does, fully translated with explanations, updating in real time as you change it. No dependencies needed. Most (but not all) settings are editable here, if something seems missing check the mod's config directly, ghost free-cam options in particular are there only.

<img width="1920" height="1080" alt="quick-setup" src="https://github.com/OnlyCook/peak-sense-of-direction/blob/main/packaging/quick-setup.png?raw=true" />

## Ghost free-cam

<img width="800" height="449" alt="ghost-free-cam" src="https://github.com/OnlyCook/peak-sense-of-direction/blob/main/packaging/ghost-free-cam.gif?raw=true" />

Press `V` while dead to fly around freely instead of being stuck spectating a teammate. Combine it with ghost pinging (works automatically) to scout ahead and ping dangers, loot or better paths for whoever's still alive. How limited dead players are, decides the host (50m within the spectating player by default).

## Notes

- Only the ghost pings and ghost free-cam features need the host (and ideally everyone) to have the mod installed for the full effect, host's config decides whether they're enabled at all. Everything else is entirely client-sided.
- Translations were done by AI, so if something is off in your language you are free to contact me (see below).

## Feedback & bug reports

Found a bug or have a suggestion? Please **[fill out this form](https://forms.gle/4Vi7kp2c42A9FfSu5)** or send me an email at `theactualcooker@gmail.com`.

## Configuration

Config file: `BepInEx/config/OnlyCook.SenseOfDirection.cfg`.

<details>

<summary><b>View config information</b></summary>

If you have [PEAKLib.ModConfig](https://thunderstore.io/c/peak/p/PEAKModding/ModConfig/) installed, every setting below is also editable in the game's settings under **Mod Settings → Sense of Direction**, no need to touch the config file by hand. Easiest way to get an overview and tweak things visually, is the **F8** quick setup panel described above though (but it doesn't contain everything).

- **General**: where each mechanic's indicator is drawn (off-screen label / compass / both), label overlap avoidance, the quick setup panel's key (`F8`).
- **Fonts**: separate size multipliers for on-screen, off-screen, and compass text.
- **Player-Labels**: master switch, toggle key (default `G`), display mode (Toggle/AlwaysOn/Hold) and its timings, min/max distance, font sizes, distance/badges/character-color toggles, whether to replace vanilla's own labels, and the through-walls skeleton ESP (off by default).
- **Campfire**: master switch and whether to show distance.
- **Pings**: distance scaling and its multiplier, the color ripple, off-screen indicator, distance label, and ghost pinging.
- **Ping-Audio**: audio boost toggle, range, minimum distance, and volume multiplier.
- **Ping-Anti-Spam**: how many pings are free before slow-mode kicks in, the slow-mode interval, queue length, and reset timing.
- **Item-Pings**: master switch, highlight duration, grouping, creature pings, native icons, name mode, distance, and off-screen indicator, plus separate detection radii and hit/ray assist for landing pings on hard-to-hit items.
- **Compass**: master switch, width/offset/FOV, icon size, elevation threshold, degree numbers, names/distances, line color and thickness, and whether it requires holding an in-game compass item.
- **Pirate-Compass**: luggage indicator toggle, name/distance display, off-screen indicator.
- **Luggage-Ping**: master switch, key (default `T`), radius, duration, cooldown (optional).
- **Ghost-Free-Cam**: master switch, leash distance / unlimited range, toggle key (default **V**), move speed and sprint multiplier, crosshair, key hint, and hiding every ghost from your own view.
- **Debug**: verbose logging, plus a couple of QA-only toggles. Please keep logging on when reporting issues.

</details>

## Credits

- Item pinging is inspired by [PingItems](https://thunderstore.io/c/peak/p/memiczny/PingItems/) by memiczny (reimplemented from scratch and revived, that mod is broken against the current game build).
- The compass is inspired by [Compass UI](https://thunderstore.io/c/peak/p/Coomzy/Compass_UI/) by Coomzy (also redone from the ground up as its broken).
- Ghost pinging idea from [GhostPing](https://thunderstore.io/c/peak/p/boxofbiscuits97/GhostPing/) by boxofbiscuits97 (also gave me the idea to add the ghost free-cam mechanic).

## Requirements

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) `5.4.2403`

## For players

- You can install the mod through r2modman as `Sense_of_Direction`
- On [Thunderstore](https://thunderstore.io/c/peak/p/OnlyCook/Sense_of_Direction/),
- Or on [Nexus Mods](https://www.nexusmods.com/peak/mods/192)

## For developers

- [`ROADMAP.md`](ROADMAP.md): full feature spec, phased plan, status, handoff notes.

Build:
```bash
cd src/SenseOfDirection
dotnet build -c Release                          # -> bin/Release/SenseOfDirection.dll
dotnet build -c Release -p:DeployToProfile=true  # also copy into the r2modman profile
```
