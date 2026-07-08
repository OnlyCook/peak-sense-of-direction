# ROADMAP — PEAK Sense of Direction

> Client-sided PEAK mod: always-visible, edge-of-screen player labels (with
> distance, status icons, and character-color matching) plus a matching
> off-screen indicator for the game's existing ping system (made bigger,
> louder from a distance, and richer — color ripple, distance label,
> anti-spam, ghost/item support) plus a ghost free-cam mode so dead players
> can actually help the living instead of going idle. **Fully open source,
> MIT licensed.** No paid/monetized component of any kind.

**Status:** Phase 1 (scaffold), Phase 2 (screen-space indicator framework),
and Phase 3 (Mechanic 1 player labels) complete — see "Handoff notes" at the
bottom of this file. Full technical findings (with file:line references
into the decompiled game/mods) are in `RESEARCH.md` (gitignored,
local-only). This file has been updated with the corrected understanding
from that research (a few original assumptions below turned out to be
imprecise — noted inline as "research correction"), but stays high-level;
deep technical detail lives in `RESEARCH.md`.

**Last updated:** 2026-07-08 (session 2, post-Phase 3).

## Design premise (why this mod, why client-sided)

PEAK's native player labels only appear within a short range and never show
distance (**research correction:** the actual vanilla gate, confirmed via
decompile, is `IsLookedAt` — an **8m flat cap combined with a shrinking
45°-max view-cone**, not a flat 15m radius; see `RESEARCH.md` Q1 for the
exact formula).
Existing "better player distance" mods (see reference zips below) fix the
distance-number part but still hide the label entirely once a player goes
off-screen — so you still have to physically turn around or scan the level to
relocate someone, which isn't viable mid-climb. The single biggest
differentiator for this mod: **player labels (and later, pings) stick to the
edge of the screen with a directional indicator when their subject is
off-screen or out of line-of-sight**, so you always know where every teammate
(alive, unconscious, or dead) currently is without turning the camera.

The mod is designed to be **client-side-only**: only the player running it
gets the benefit, no lobby-wide install requirement, no server authority
needed for the core label/ping-visual features since all the underlying data
(other players' transforms, health/ragdoll state, ping RPCs) is already
replicated to every client by the base game. The one mechanic that is an
open research question for pure client-side feasibility is ghost free-cam
(Mechanic 3) — see its section below.

---

## Reference material in this repo (gitignored, local-only research inputs)

- `BepInEx-BepInExPack_PEAK-5.4.2403.zip` — same BepInEx pack version used by
  `peak-checkpoint-save`, for parity.
- `AiAeT-BetterPlayerDistance-0.1.15.zip` — existing mod that adds distance
  numbers to player labels. Study for: how it hooks the native label
  component, how it reads player transforms/distance, and confirm its
  15m-visibility-gate workaround approach (if any) so we don't repeat a
  worse version of what already exists — this mod must clearly improve on it
  (edge-of-screen persistence is the improvement).
- `GinjeesPacks-BiggerPing-1.0.2.zip` — existing mod that scales up ping
  visuals. Study for: how it hooks the ping prefab/visual, what "bigger"
  actually touches (mesh scale? billboard scale? both?).
- `LucydDemon-BetterPingDistance-1.1.0.zip` — existing mod for ping distance
  scaling/labeling. Study for: any distance-label-on-ping precedent and
  audio distance-falloff handling, if it touches audio at all.
- `boxofbiscuits97-GhostPing-0.2.0.zip` — existing mod that lets
  ghosts/spectators ping. Originally scoped as "rip this into the mod";
  **research finding: its packaged `LICENSE` is an unedited GitHub
  placeholder ("TODO: choose a license") — no license was actually chosen,
  so it's all-rights-reserved by default and not safe to copy from.** Not a
  problem in practice: the mod's entire logic is two tiny Harmony
  transpilers (~35 lines, see `RESEARCH.md` Q9) removing a couple of IL
  instructions to skip a death/consciousness check and a line-of-sight
  penalty. Reimplementing the same *effect* from scratch (as a Harmony
  prefix, not an index-based IL-removal transpiler, since the latter is
  fragile against game updates) as part of Mechanic 2's own ping patch is
  barely more work than the "ripping" plan would have been, and avoids the
  licensing question entirely. Same treatment applies to
  `AiAeT-BetterPlayerDistance`, `GinjeesPacks-BiggerPing`, and
  `LucydDemon-BetterPingDistance` below — **none of the four root reference
  mods have a license that permits copying their source** (two have no
  LICENSE file at all, one has GPLv3 which is copyleft-incompatible with
  shipping Sense of Direction as MIT, one has the same unfilled
  placeholder) — all four are behavioral/pattern references only, never a
  source of copy-pasted code. See `RESEARCH.md`'s license-summary table for
  the full breakdown per mod.
- `.compatibility/CakeDevs-MoreColourOptions-1.0.0.zip` — adds extra
  character colors. Player-label foreground-color-matching (Mechanic 1)
  must resolve whatever color this mod assigns a character, not just the
  base game's fixed palette.
- `.compatibility/figgies-SmoreSkinColors-1.2.2.zip` — same compatibility
  concern as above, different mod, different color source (skin vs.
  character color) — need to determine during research which color source
  the native player label already keys off of, and whether these mods patch
  that same source (in which case we get compatibility for free by reading
  the same field) or provide color through a different path we need our own
  hook for.
- `.compatibility/memiczny-PingItems-1.6.2.zip` — lets players ping
  interactable items, not just world position. Sense of Direction's ping
  overhaul (bigger, off-screen-indicated, distance-labeled, anti-spam) must
  visually apply to *item* pings from this mod too, not just the vanilla
  ground/position ping — needs its own compatibility patch since it's a
  separate mod's RPC/prefab, not something we can assume is unified with the
  base game's ping call sites.
- Full decompiled `Assembly-CSharp` for the current PEAK build already exists
  from the sibling `peak-checkpoint-save` project
  (`~/Projects/GitHub/peak-checkpoint-save/scratch/decomp/allcs/Assembly-CSharp.decompiled.cs`)
  — reuse rather than re-decompiling, since it's confirmed to work flawlessly
  against the current game version per that mod's own notes.
- `~/Projects/GitHub/peak-checkpoint-save/` as a whole — reference project
  structure (`src/<PluginName>/*.cs`, `packaging/` layout, `manifest.json`,
  `README.md`/`CHANGELOG.md` conventions, `docs/RESEARCH.md`/`docs/INSTALL.md`/
  `docs/TESTING.md` split, `packaging/build-release.sh` +
  `packaging/gen-readme.sh` packaging flow) — copy conventions, not code
  (that mod does save/load orchestration, unrelated domain).

---

## Feature breakdown (as specified by maintainer)

### Mechanic 1 — Always-visible, edge-of-screen player labels

**Problem being solved:** native player labels vanish past 15m and never
show distance; even mods that add distance still hide the label entirely
off-screen, so finding a teammate who wandered off (or fell, or died) still
requires manually scanning/turning around, which isn't always possible
(e.g., mid-climb).

**Full behavior:**
- Show a label for every *other* player in the lobby (never your own).
  Labels are not gated by the native 15m range — configurable min/max range
  instead (see settings below).
- Label = player name (foreground color = that player's character color,
  see color-matching note below) with the game's native font, black outline,
  and an optional distance sub-line below in a separate configurable font
  size.
- **Host gets a crown icon** on their label (rip the crown icon asset from
  the game's own UI — decompile should reveal where it's drawn/stored,
  likely the pause menu or lobby player list).
- **Unconscious players** get a distinct icon on their label.
- **Dead players** get a different, distinct icon, and — this is the part
  other mods don't do — their label **persists** (does not disappear) so
  teammates can find the body to revive/loot. Caveat: only while their body's
  segment is currently the active one. **Research correction:** PEAK does
  not actually load/unload separate Unity scenes per biome/segment — the
  whole run lives in one loaded scene, and "the current segment" is really
  just whichever segment's root GameObject is `SetActive(true)`
  (`MapHandler.GoToSegment`, triggered by lighting a campfire, deactivates
  the old segment's root and activates the new one — see `RESEARCH.md` Q4).
  So the check is simply: is the dead player's body still under the
  currently-active segment root (`activeInHierarchy`, or a cached
  segment-index comparison against `MapHandler.Instance.currentSegment`)? A
  dead player's label must disappear once their segment deactivates, since
  there'd be nothing sensible to point to anymore.
- **Edge-of-screen sticking + off-screen/out-of-FOV indicator**: this is the
  headline feature. When a labeled player is outside the camera's view
  frustum (off-screen) or otherwise out of line-of-sight, their label clamps
  to the corresponding edge of the screen with a directional indicator
  (arrow or similar) showing which way to look. No known existing PEAK mod
  does this for player labels — main selling point.
- **Settings:**
  - Name label font size.
  - Distance label font size.
  - Display state mode: `Toggle` / `Always On` / `Hold`.
  - Rebindable key for whichever mode is active (default `G`). Toggle mode:
    press to show, press again to hide. Hold mode: press-and-hold behavior
    (see next two settings).
  - Hold-mode shown duration (how long labels stay visible after the key is
    released, or however "hold" ultimately behaves once researched against
    PEAK's input APIs).
  - Hold-mode minimum time: pressing the key once guarantees labels stay up
    for at least this many seconds even if the key is released immediately.
  - Min/max distance gate: label only shows if `min < distance < max`.
    Defaults: `min = 15m` (matches native gate, so default behavior isn't
    "more spammy" than vanilla unless the user opts in), `max = 1000m`.
  - Toggle: use player's actual character color for the label foreground,
    vs. a plain white foreground for everyone (accessibility/preference
    option). Must resolve color correctly even when the other client is
    using a color-adding mod (MoreColourOptions / SmoreSkinColors) —
    resolved by reading whatever shared field/component the game (and by
    extension those mods, if they patch the same field) uses to drive the
    *native* player label/character color, not a fixed enum we maintain
    ourselves.

**Extra/bonus, filed under Mechanic 1 per maintainer note:** a campfire
icon/indicator so the player can always see the direction of the (typically
next, or current-segment) campfire — same edge-of-screen indicator mechanism
as player labels, applied to a fixed world point rather than a moving
player. Not part of the original ask; a "since we're building the
indicator system anyway" addition.

### Mechanic 2 — Better pings

**Problems being solved:** native pings don't scale with distance, so a ping
30m+ away is nearly invisible, worse if the pinged ground matches the
pinging player's color (e.g., green player pinging grass). Ping sound also
doesn't carry — it's already faint past ~20m.

**Full behavior:**
- Pings **scale up with distance** (relative/non-linear scaling so far pings
  are deliberately, noticeably larger — configurable multiplier).
- Add a **3D ripple effect** in the pinging player's character color,
  radiating from the ping location, to make it readable against
  similarly-colored terrain.
- **Drastically reduce the ping sound's distance falloff** — should be
  clearly audible from anywhere on the map, while sounding normal/unchanged
  at close range (~10m, per maintainer's calibration anchor).
- Reuse the **same off-screen/edge-of-screen indicator system** from
  Mechanic 1 so an active ping that's off-screen still shows a directional
  cue.
- **Distance label** above the ping showing how far away it is.
- **Anti-spam setting**: more visual/audio prominence on pings makes ping
  spam more disruptive than in vanilla, so add a cooldown/rate-limit,
  configurable.
- Every individual enhancement (scaling, ripple, sound falloff reduction,
  off-screen indicator, distance label) must be independently toggleable in
  config, plus the ping-size scaling multiplier itself is user-configurable.
- **Ghosts can ping too**, using the ghost's own body color for the ripple/
  ping color instead of a living character color. (Ties into Mechanic 3 —
  see GhostPing reference mod above, meant to be folded directly into this
  mod rather than kept as a separate dependency.)
- **Compatibility with item pinging**: make this mod's ping enhancements
  apply to `.compatibility/memiczny-PingItems-1.6.2.zip`'s item pings too,
  not just the base game's world-position ping.

### Mechanic 3 — Ghost free-cam

**Problem being solved:** dead players are stuck in a third-person camera
locked to whichever living player they're spectating, which combined with
pings barely being visible/audible historically made death feel like being
benched rather than still able to contribute.

**Full behavior:**
- A bindable key toggles between normal spectate-camera (as today) and a
  free-flying camera the dead player can pilot anywhere on the map.
- Same key toggles back to spectate mode.
- Combined with Mechanic 2's ghost pinging (color-matched to the ghost's own
  body), a dead player becomes genuinely useful: fly ahead, scout, ping
  hazards/loot/the way forward for the still-living players.
- **Open research question, called out explicitly by maintainer:** whether
  free camera movement (not just ping RPCs) can be made to work with full
  fidelity purely client-side, given the mod's stated preference to stay
  client-only. Maintainer has explicitly said this priority is soft — if
  full client-only free-cam turns out to need any host/other-client
  cooperation (e.g. because the spectate camera's authority or the ghost's
  visible position to others lives server-side/is host-authoritative),
  that's an acceptable, documented tradeoff rather than a blocker. Attempt
  client-only first; document exactly what breaks (if anything) for other
  players if only the spectating client has the mod.
  **Research finding, very favorable:** the local camera (`MainCameraMovement`)
  is already 100% client-local (every client computes their own camera
  transform independently), and PEAK ships an already-fully-built,
  currently-dormant free-camera controller (`GodCam` — orbiting, WASD-style
  flight, scroll FOV) that `MainCameraMovement.LateUpdate` already checks
  for first via an `isGodCam` flag, just with no code path left in the
  shipped build that ever sets it true. What other clients actually see
  (the networked `PlayerGhost` body) is driven by separate synced fields
  (`CharacterData.lookDirection`/`spectateZoom`), decoupled from the local
  camera transform — so flipping `isGodCam` on/off locally (via reflection)
  to reuse this existing controller looks very likely to give free-cam with
  no other-client cooperation needed at all, worst case just a
  ghost-body-stops-rotating visual for other clients while free-camming.
  See `RESEARCH.md` Q10 — still needs in-game confirmation this pans out
  exactly as the decompile suggests, but this de-risks Mechanic 3
  considerably versus writing a free-cam controller from scratch.

---

## Compatibility targets (must explicitly verify, not just assume)

1. `CakeDevs-MoreColourOptions-1.0.0.zip` — extra character colors must be
   correctly reflected in Mechanic 1's label foreground color.
2. `figgies-SmoreSkinColors-1.2.2.zip` — same, different color axis
   (skin color). **Resolved by research (`RESEARCH.md` Q5):** both this and
   MoreColourOptions patch `PassportManager.Awake` to append new entries
   directly onto the same `Customization.skins` array that
   `CharacterCustomization.PlayerColor` reads from — same axis, same array,
   full compatibility for free just by reading `PlayerColor` as normal.
3. `memiczny-PingItems-1.6.2.zip` — item pings must receive the same visual/
   audio treatment as Mechanic 2's ground pings.
4. Multiplayer/host-vs-client parity: since this is client-sided, verify
   nothing about the implementation accidentally requires the *other*
   players to have the mod for the local player's own features to work
   (aside from the documented Mechanic 3 open question above).

---

## Proposed implementation order

Chosen so that later phases can reuse infrastructure built in earlier ones,
and so the hardest open question (Mechanic 3 client-sidedness) is tackled
once the rest of the mod already gives value on its own — i.e. the mod is
useful and shippable even if Mechanic 3 ends up needing compromises.

1. **Phase 1 — Project scaffold.** BepInEx plugin skeleton mirroring
   `peak-checkpoint-save`'s layout (`src/<Plugin>/`, `PluginInfo.cs`,
   `Plugin.cs`, `.csproj`, `Directory.Build.props`), config plumbing
   (BepInEx config + PEAKLib.ModConfig entries if that's the pattern the
   other mod follows), `packaging/manifest.json`, icon, README/CHANGELOG
   skeletons, `packaging/build-release.sh`. No gameplay code yet — get a
   loadable, versioned, empty plugin building and deploying to a local
   profile first.
2. **Phase 2 — Screen-space indicator framework.** The shared foundation:
   given a world position (or a `Transform` to track live) and the local
   player's camera, compute on-screen position when in view, or a
   clamped-to-edge position + direction arrow when not. This is pure
   camera/math work with no gameplay hookup yet, built and tested against
   dummy points before wiring in real players. Both Mechanic 1 (player
   labels + campfire icon) and Mechanic 2 (ping indicator) consume this.
3. **Phase 3 — Mechanic 1: player labels.** Native label/name lookup, host
   crown, unconscious/dead icons and the dead-label persist-until-scene-
   unload behavior, distance calculation + min/max gate, character-color
   resolution (with the two color-mod compatibility targets), all the
   listed settings (font sizes, display-state mode + rebind + hold timings),
   wired onto the Phase 2 indicator framework for the off-screen case.
4. **Phase 4 — Mechanic 1 bonus: campfire indicator.** Small addition once
   Phase 2/3 exist — point the same indicator system at the relevant
   campfire's world position.
5. **Phase 5 — Mechanic 2: better pings.** Distance-relative scale, color
   ripple (ripped/adapted from `GinjeesPacks-BiggerPing` /
   `LucydDemon-BetterPingDistance` as reference, or built fresh if their
   approach doesn't fit), audio falloff rework, off-screen indicator reuse
   (Phase 2), distance label, anti-spam cooldown, ghost-ping color source,
   all toggles + the scale multiplier setting. Do the `PingItems`
   compatibility patch as part of this phase since it's the same visual
   pipeline, not a separate system.
6. **Phase 6 — Mechanic 3: ghost free-cam.** Tackled last and independently
   because it's the one part of the mod whose client-sided feasibility was
   the biggest open question — now considerably de-risked by research (see
   `RESEARCH.md` Q10): try reflecting PEAK's own dormant `MainCameraMovement.isGodCam`
   flag to true first, reusing its already-built `GodCam` controller
   wholesale, before writing any bespoke free-fly camera code; only fall
   back to a from-scratch controller if that turns out to have a hidden
   gotcha once tested in-game. Ghost pinging itself (bypass the
   dead/consciousness gate and the line-of-sight visibility penalty for
   pinging ghosts, recolor using the ghost's own body color) is
   reimplemented as part of Mechanic 2's own `PointPinger` patch (Phase 5),
   not a separate system — see the reference-mods note above on why
   `GhostPing` is reimplemented rather than copied. Document any discovered
   multiplayer-fidelity caveats plainly in the README.
7. **Phase 7 — Compatibility pass + polish.** Explicit manual verification
   against all three `.compatibility` mods and the four root reference mods
   (make sure Sense of Direction doesn't conflict with e.g. BiggerPing or
   BetterPlayerDistance if a user runs both, even though this mod is meant
   to supersede their functionality — decide during research whether to
   detect-and-warn on overlapping mods or just let them visually stack).
8. **Phase 8 — Packaging & release.** Manifest finalization, README/CHANGELOG
   per `peak-checkpoint-save` conventions, MIT LICENSE file, first
   Thunderstore-shaped release zip via `build-release.sh` equivalent.

---

## Research status

All questions originally listed here were resolved in the session-1
research pass — see `RESEARCH.md` for the full technical write-up (one
section per question, with `ClassName.MethodName`/file:line references into
the decompiled sources, plus a "Summary of concrete API surface" section at
the bottom listing every class/field/method Sense of Direction will
actually touch). Highlights already folded into this file above: the
8m/45°-cone native label gate (not 15m), segment-activation instead of real
scene load/unload, full color-mod compatibility via `PlayerColor`, the
vanilla ping's harsh distance-based visibility cutoff (not just "faint"),
and the dormant `GodCam` free-camera controller PEAK already ships.

A few items in `RESEARCH.md` are flagged as **not resolvable from static
decompilation alone** and need confirming once implementation/in-game
testing starts (not blockers for starting Phase 1-2): the exact ping audio
rolloff curve/mode (only `SFX_Settings.range = 150` is known statically),
the actual crown/host-star sprite asset (Unity asset refs aren't visible in
IL — `PlayerName.hostStar` is the field name to grab it from live), and
whether reflecting `MainCameraMovement.isGodCam = true` works cleanly
end-to-end with no hidden gotchas.

---

## Handoff notes for the next session

Phase 1 (scaffold), Phase 2 (screen-space indicator framework), and Phase 3
(Mechanic 1 player labels) are all done.

Phase 2 lives in `src/SenseOfDirection/Indicators/` — see `CODEBASE.md` for
the file breakdown (`ScreenSpaceTracker` for the pure math,
`IndicatorAnchor`/`IndicatorManager` for the generic registration/update
loop, `IndicatorTestHarness` for dummy-point in-game verification, gated
behind the `enable-indicator-test-harness` debug config flag, off by
default). The math is an independent derivation (center-relative direction,
scaled out to a rectangle edge inset by a margin), not a port of
`PingItems.Core.EdgeTracker` — that class was read as reference only per
`RESEARCH.md` Q8 (no license permits reuse). **Verified in-game** by the
maintainer: dummy points clamp to the right edge and (after fixing an
inverted arrow-rotation sign — `angle + 90`, not `angle - 90`, see git
history) the arrow points the correct way as the camera turns.

Phase 3 lives in `src/SenseOfDirection/Labels/` — see `CODEBASE.md` for the
file breakdown. Implements: native name/color lookup
(`character.characterName` / `character.refs.customization.PlayerColor`),
host crown (real sprite, discovered at runtime off a live
`PlayerName.hostStar`), dead/unconscious icons (plain colored squares for
now — no ripped asset mandated for those, only the crown; swap for real art
later if wanted), the dead-label persist-until-segment-deactivates behavior
(implemented as a simple `character.gameObject.activeInHierarchy` check,
per `RESEARCH.md` Q4's simpler recommended approach — no segment-index
caching needed), distance calc (converted to real meters via
`CharacterStats.unitsToMeters`, since raw Unity units aren't 1:1 meters) +
min/max gate, and all the listed settings (font sizes, display-mode +
rebind + hold timings) under the `Player-Labels` config section.

**Not yet verified in-game:** Phase 3 hasn't been playtested yet — the
maintainer needs to confirm in a real PEAK session that labels appear
correctly for other players (font/outline material discovery succeeds,
crown/dead/unconscious icons look right, Toggle/AlwaysOn/Hold modes behave
as expected, off-screen labels clamp+arrow correctly) before Phase 4 builds
on top of this.

Next step is **Phase 4: campfire indicator** (small addition — same
`IndicatorAnchor` mechanism, pointed at a fixed world point instead of a
`Character`), then **Phase 5: Mechanic 2 (better pings)**.
`RESEARCH.md` Q6-Q9 have the exact game classes/fields to hook for that.
