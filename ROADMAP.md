# ROADMAP — PEAK Sense of Direction

> Client-sided PEAK mod: always-visible, edge-of-screen player labels (with
> distance, status icons, and character-color matching) plus a matching
> off-screen indicator for the game's existing ping system (made bigger,
> louder from a distance, and richer — color ripple, distance label,
> anti-spam, ghost/item support) plus a ghost free-cam mode so dead players
> can actually help the living instead of going idle. **Fully open source,
> MIT licensed.** No paid/monetized component of any kind.

**Status:** Phase 1 (scaffold), Phase 2 (screen-space indicator framework),
Phase 3 (Mechanic 1 player labels), Phase 4 (campfire indicator bonus),
Phase 5 (Mechanic 2 better pings), and Phase 5b (native item-ping, replacing
the confirmed-broken `memiczny-PingItems` dependency) complete — see
"Handoff notes" at the bottom of this file. **Next up: Phase 6 (Mechanic 3,
ghost free-cam)** — `RESEARCH.md` Q10 has the exact game classes/fields to
hook. Full technical findings (with file:line references into the
decompiled game/mods) are in `RESEARCH.md` (gitignored, local-only). This
file has been updated with the corrected understanding from that research
(a few original assumptions below turned out to be imprecise — noted
inline as "research correction"), but stays high-level; deep technical
detail lives in `RESEARCH.md`.

**Last updated:** 2026-07-08 (session 3, post-Phase 5b).

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
3. ~~`memiczny-PingItems-1.6.2.zip` — item pings must receive the same
   visual/audio treatment as Mechanic 2's ground pings.~~ **Superseded:**
   investigation during Phase 5 found this mod itself doesn't work against
   the current PEAK build (confirmed by installing it alone, no other
   mods - pinging fails entirely; see "Handoff notes" bottom-of-file
   writeup), so "coexist with it" isn't a meaningful target anymore -
   replaced by Phase 5b, reimplementing item-ping detection/highlighting
   natively instead of depending on it.
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
   all toggles + the scale multiplier setting. (Originally planned to
   include the `PingItems` compatibility patch here too, since it's the
   same visual pipeline - moved to its own Phase 5b once investigation
   showed `PingItems` itself is broken against the current game version,
   independent of anything Sense of Direction does - see Phase 5b below.)
5b. **Phase 5b — native item-ping (PingItems replacement).** See "Handoff
   notes" at the bottom of this file for the full investigation write-up.
   Short version: `memiczny-PingItems` (the `.compatibility` mod meant to be
   interoperated with) turned out to be genuinely broken against the
   current PEAK build — unmaintained for ~10 months, reflecting a private
   `PointPinger` field (`coolDownLeft`) that no longer exists after a game
   update renamed/restructured that state, which silently breaks its whole
   ping pipeline (confirmed via in-game testing: installing it alone, with
   no other mods, already fully prevents pinging - not a Sense of Direction
   interaction bug). Decision: reimplement item-ping detection + on-screen
   highlighting **natively** in Sense of Direction rather than depending on
   or patching around a broken third-party mod - this both fixes the
   "doesn't work at all" bug for good (no dependency on an unmaintained mod)
   and is a straightforward superset of the original ROADMAP compatibility
   ask (Mechanic 2 already owns the whole `ReceivePoint_Rpc` pipeline as of
   Phase 5, so adding "detect nearby items/luggage and highlight them" is
   additive, not a redesign). **Reimplement the effect, not the code** -
   `memiczny-PingItems` ships with no LICENSE file at all in its package
   (RESEARCH.md's license-summary table), so it's all-rights-reserved by
   default same as `GhostPing` was; same treatment applies here as it did
   there (Q9): read its decompiled source at
   `scratch/decomp/out/memiczny-PingItems/**/*.cs` for the *approach* (its
   `PingItems.Services.PingService`/`PingItems.Factories.OptimizedPingableFactory`/
   `PingItems.Core.Highlighting.PingHighlighter` architecture is a fully
   worked reference for "find nearby Item/Luggage/MirageLuggage colliders
   near a ping point, spawn a highlight overlay per one, color-match to the
   pinging player"), but write our own implementation from scratch, in our
   own architecture (extending `PointPingerPatches.ReceivePointRpcPrefix`,
   which already owns ping spawning as of Phase 5, plus Phase 2's
   `IndicatorAnchor`/`IndicatorManager` for the highlight's own screen
   presence if it should be edge-indicated same as the ping itself).
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
as expected, off-screen labels clamp+arrow correctly).

Phase 4 (campfire indicator) is also done, built on top of Phase 2's
indicator framework: `CampfireIndicator/CampfireIndicatorController.cs`
re-resolves `MapHandler.CurrentCampfire` every frame (no Harmony hook on
`GoToSegment` needed — this is simpler and self-correcting) and registers/
re-registers an `IndicatorAnchor` for it, so the indicator automatically
points at whichever campfire is next as the player advances segments. Two
new `Campfire` config settings: `enable-campfire-indicator` (master switch)
and `show-distance`. (Originally shipped **default off**; flipped to
**default on** during the config rework - it's the most direct answer to the
question the mod is named after, so it belongs in the out-of-the-box
experience.)

**In-game playtest (2026-07-08) turned up three fixes, all applied:**
- Distance text wasn't using the native game font — `CampfireWidget.Refresh`
  now applies `Labels.NativeAssets.Font`/`OutlineMaterial` exactly like
  `PlayerLabel.Refresh` does.
- Default changed from on to off (see above).
- The icon was a plain orange square, and an off-screen arrow was showing —
  backwards from the intended design (the arrow was meant to be reserved for
  Mechanic 2's ping indicator, same as `PlayerLabel` already does for
  itself). Fixed: `CampfireWidget` no longer has an arrow at all (just
  clamps to the edge like a player label), and the icon now uses the game's
  real HUD campfire sprite (`StaminaBar.campfire`, discovered the same way
  `peak-checkpoint-save`'s `SavePicker` F7-menu title icon does it) via a new
  `NativeAssets.CampfireIconSprite` lookup, rather than a placeholder square.

**Second round of fixes (still same 2026-07-08 session), after playtest
confirmed the above worked:**
- `Debug` config section now bound last in `PluginConfig.cs` (was between
  `General` and `Player-Labels`) so it's the last tab in ModConfig-style
  settings UIs, not sandwiched ahead of user-facing settings.
- Campfire icon now has a black outline matching the host crown badge's
  look. The crown gets its outline for free (baked into that native sprite's
  own art); the HUD campfire sprite (`StaminaBar.campfire`) has none, so
  `CampfireIndicator/CampfireWidget.cs` fakes one the classic UI way: eight
  copies of the same sprite tinted solid black, offset by 1px in every
  direction and drawn behind the real icon - the sprite's own alpha shape
  does the rest, giving a stroke that follows its silhouette (Unity's
  built-in `UI.Outline` component was considered and rejected: for a
  `Simple`-mode `Image` it duplicates the quad's four corner vertices, not
  the sprite shape, so it would've just drawn an offset rectangle instead of
  a real outline).

Still not yet re-verified in-game after this round of fixes.

Phase 5 (Mechanic 2, better pings) is also done — see `CODEBASE.md`'s
`Pings/` section for the file breakdown. Built as a single Harmony patch set
on `PointPinger`/`PointPing` per `RESEARCH.md` Q6-Q9's finding that scaling,
ripple, the visibility-cutoff fix, anti-spam, and ghost-pinging all revolve
around the same patch surface: `ReceivePoint_Rpc` is fully replaced (prefix
returns false, reimplementing vanilla's own spawn logic) rather than run
alongside vanilla, since the harsh "don't even spawn the ping past ~40-50m"
early-exit lives inside that method body with no way to skip past it from a
plain prefix/postfix pair. All six roadmap sub-features (scale multiplier,
ripple, audio-falloff reduction, off-screen indicator + distance label,
anti-spam cooldown, ghost pinging) are independently toggleable under a new
`Pings` config section, plus `remove-visibility-cutoff` (on by default,
since it's the prerequisite fix the other settings build on).

**In-game playtest (2026-07-08) turned up six fixes, all applied:**
- The always-visible on-screen "Dot" marker (a plain colored square) was
  removed from `PingWidget` entirely — the real 3D ping is already visible
  on-screen, so a 2D UI square drawn on top of it just obstructed the view.
  The off-screen arrow itself was already correctly gated to only show once
  a ping actually leaves the screen (`IndicatorManager`'s existing
  `state.IsOffScreen` check) — the dot was the actual culprit being
  mistaken for an always-on arrow.
- `PingRipple` was a flat 2D ring laid on the hit-normal plane, which
  degenerates to a near-invisible sliver when viewed close to edge-on
  (common on steep terrain) — rewritten as an actual expanding translucent
  sphere (`GameObject.CreatePrimitive(PrimitiveType.Sphere)`, unlit
  alpha-blended material), which reads correctly as "3D" from any angle.
  It now also tracks the spawned `PointPing`'s own `transform.localScale`
  every frame (rather than a fixed world-unit max radius) so the ripple
  stays sized consistently relative to the ping marker itself, same
  reasoning as the scale fix below.
- Anti-spam was incorrectly throttling the local player's own pings too
  (since `ReceivePoint_Rpc` fires on every client including the sender,
  per RESEARCH.md Q6) — `PointPingerPatches.ShouldAcceptPing` now always
  accepts when the pinging character is `Character.localCharacter`.
  Redesigned from a single flat cooldown into a gradual, self-decaying
  ramp for other players: occasional pings are never delayed; only pinging
  faster than `ping-anti-spam-rapid-interval-seconds` repeatedly ramps the
  required gap up (`ping-anti-spam-cooldown-step-seconds` per rapid ping,
  capped at `ping-anti-spam-max-cooldown-seconds`), and a quiet period of
  `ping-anti-spam-reset-seconds` clears it back to normal.
- Ping scale: the original fix only multiplied vanilla's *already-clamped*
  `PointPing.Go()` output by our multiplier, so the underlying
  `minMaxScale` (0.2-3.0) clamp - the actual reason a ping's apparent
  on-screen size used to shrink past a certain distance - was still in
  effect underneath. `GoPostfix` now recomputes the same frustum-relative
  formula uncapped and overwrites `transform.localScale` outright, so a
  ping now keeps a constant apparent screen size at any distance,
  scaled by the configured multiplier.
- Audio boost wasn't actually reducing perceived falloff past ~40m, because
  boosting `SFX_Instance.settings.range` only pushes back
  `SFX_Player.PlaySFX`'s hard "don't even start playing" distance check -
  it doesn't touch the pooled `AudioSource`'s own rolloff curve, which is
  whatever vanilla ships and falls off far more steeply. New
  `PingAudioTuner` (polls `SFX_Player.instance.sources`, public fields, no
  reflection needed) forces `AudioRolloffMode.Linear` plus a configurable
  `minDistance`/`maxDistance` (new `ping-audio-min-distance-meters`
  setting, default 10m per the maintainer's "sounds normal up close"
  calibration anchor) onto whichever pooled source is currently playing a
  ping clip.
- Ping labels used to vanish the instant their ping was destroyed - new
  `PingWidgetFadeOut` fades the widget out over 0.35s instead (appearing
  stays instant, matching the maintainer's ask that only disappearing
  should animate).

**Second round of in-game feedback (still 2026-07-08), two more fixes:**
- Default scale multiplier was too aggressive (3x made pings absurdly
  large - "pinging the whole island") - default changed to 1x (matches
  vanilla's own uncapped size, i.e. purely "not shrinking at distance" with
  no extra embiggening), and the config range tightened from 1-8x to a
  saner 0.5-3x.
- Audio boost's `AudioRolloffMode.Linear` barely seemed to quiet down for
  most of its range and then vanished abruptly near maxDistance (~240m at
  default 600m range) - this is a known Linear-rolloff characteristic:
  Linear is linear in raw amplitude, but human hearing perceives loudness
  roughly logarithmically, so a linear amplitude falloff *sounds* like "loud
  the whole time, then a cliff" rather than a smooth fade. Switched
  `PingAudioTuner` to `AudioRolloffMode.Logarithmic`, which tracks perceived
  loudness much more naturally. Also added a distance-driven
  `AudioLowPassFilter` (cutoff frequency lowered the further away, 22kHz at
  `ping-audio-min-distance-meters` down to 700Hz at
  `ping-audio-range-meters`) per the maintainer's own "muffle it to sound
  further away" suggestion - real distant sounds lose high frequencies, not
  just volume, so this reads as "far away" far more convincingly than
  volume/rolloff curve tuning alone. The filter is added/removed on the
  pooled `AudioSource` on demand (tracked via a two-frame active-set diff in
  `PingAudioTuner`) since that AudioSource is shared by every sound in the
  game, not just pings - it must not stay muffled for whatever plays through
  it next once the ping itself stops.

**Third round (still 2026-07-08):** close-range volume was a tad too loud
with audio boost on - new `ping-audio-volume-multiplier` setting (default
0.85) trims the ping sound's own base volume down when boost is enabled.
Applied in `PingAwakePostfix` relative to the real vanilla volume (cached
per-`SFX_Instance` the first time it's seen, same pattern as the existing
`.settings.range` handling), not a guessed constant, so toggling boost off
mid-session correctly restores the untouched original.

**Still not resolvable from static analysis alone, flagged for further
live tuning:** exact low-pass cutoff range (700Hz-22kHz) and anti-spam
threshold balance are first-pass numbers, not vanilla-derived - adjust to
taste.

**Known limitation, not a bug:** ghost pings (RESEARCH.md Q9) are a
client-side visual/behavior change - other players only see the
enhanced/bypassed ghost ping if *they* also have Sense of Direction
installed, since the pinging client is the one whose Harmony patch decides
whether `canPing` bypasses the dead-check at all. Not currently a fixable
gap for a client-sided mod (would need the ping's origin client to already
be running the mod), noted here rather than as an open bug.

**`memiczny-PingItems` investigation (2026-07-08, same session) - root
cause found, plan changed:** the original Phase 5 plan (see RESEARCH.md
Q8) was to build a soft-dependency compatibility shim so Sense of
Direction's ping enhancements would also apply to PingItems' own item
highlights. Before building that, the maintainer tested pinging with both
mods installed and found **pinging didn't work at all** - not "item
highlights are missing," but no ping of any kind, even a plain vanilla-style
terrain ping. Investigation:
- Static read of `PingItems.Patches.PointPingerPatch`
  (`scratch/decomp/out/memiczny-PingItems/PingItems.Patches/PointPingerPatch.cs`)
  found it reflects a private `PointPinger` field called `coolDownLeft` that
  does not exist in the current decompile (current `PointPinger` uses
  `_timeLastPinged` + a computed `inCooldown` property instead - see Q6).
  That alone should be harmless (the reflection failure is wrapped in
  PingItems' own try/catch, falling back to `return true` = let vanilla's
  `Update()` run), and PingItems only *postfixes* `ReceivePoint_Rpc` (never
  prefixes it), so on paper it shouldn't conflict with Sense of Direction's
  own full-replacement prefix there either. Every code path in both mods'
  ping-adjacent methods turned out to be defensively try-caught, so static
  analysis alone couldn't produce a single smoking-gun exception.
- Added a try/catch safety net around
  `PointPingerPatches.ReceivePointRpcPrefix` regardless (falls back to
  `return true`/vanilla on any exception instead of ever silently eating a
  ping) - a legitimate robustness fix on its own, shipped, but it turned out
  not to be the actual cause.
- **The maintainer then isolated it empirically: pinging fails with
  `PingItems` installed *alone*, no other mods, offline/solo.** So this was
  never a Sense of Direction compatibility bug at all - `PingItems` 1.6.2
  has been unmaintained for roughly 10 months and is simply broken against
  the current PEAK build on its own. Confirmed by uninstalling it entirely:
  pinging works fine.
- **Decision (see Phase 5b in "Proposed implementation order" above):**
  rather than trying to patch around or coexist with a mod that doesn't
  work on its own, reimplement its actual feature (ping-triggered item/
  luggage highlighting) natively in Sense of Direction. This fully
  resolves the compatibility ask (a broken dependency can't be
  "compatible" with anything) and is a fairly natural extension of Phase
  5's existing `ReceivePoint_Rpc` ownership.

**Phase 5b (2026-07-08, session 3) — done.** Native item-ping reimplementation,
built as `ItemPings/` (see `CODEBASE.md` for the file breakdown) and hooked
into `Pings/PointPingerPatches.ReceivePointRpcPrefixImpl` behind a new
`Item-Pings/enable-item-pings` config switch. Read
`scratch/decomp/out/memiczny-PingItems/**/*.cs` for the *approach* only (no
LICENSE in that package - reimplemented, not copied, same treatment as
`GhostPing`/Q9) - its `PingItems.Services/PingService.cs`,
`PingItems.Factories/(Optimized)PingableFactory.cs`, and
`PingItems.Core.Highlighting/PingHighlighter.cs` were the reference
architecture, but the actual implementation is independent and considerably
smaller (no spatial grid/pooling - this runs once per ping, not per-frame;
no animal/celestial/spore-shroom adapters; no `MirageLuggage`, since it has
no display name of its own). One concrete finding worth recording: the
reference mod's own detection code reads `Item.ALL_ITEMS`, which **does not
exist** in the current decompile - the real static list is
`Item.ALL_ACTIVE_ITEMS` - a second, independent confirmation (alongside the
already-known `coolDownLeft` field) that a game update restructured internal
state PingItems depends on, reinforcing that it's genuinely broken rather
than something Sense of Direction could stay soft-compatible with. Detection
radii (separate item vs. luggage, luggage larger by default) and grouping
(flat group-by-display-name, not the reference mod's iterative cluster
search - unnecessary here since everything found is already within one
ping's detection radius of the same point) are both configurable. Credited
in `packaging/README.github-extra.md`'s new Credits section, per the
maintainer's ask, since the mod's ping-highlight *feature* is inspired by
`memiczny-PingItems` even though no code was reused. **Not yet playtested
in-game** - next session (or before shipping) should verify: items/luggage
actually highlight when pinged near, names/distances render in the native
font, grouping produces a sane "Nx Item" label, and a highlight fades early
when its item is picked up / its luggage is opened mid-display.

**Phase 5b playtest fixes (2026-07-08, still session 3):** the maintainer
confirmed the feature works, but flagged a substantial list of problems from
real in-game use:

- **Redundant distance label:** the generic ping's own distance sub-line and
  an item highlight's own distance line were both showing, stacked on top of
  each other. Fixed: `ItemPingSpawner.SpawnFor` now returns how many targets
  it highlighted, and `PointPingerPatches` suppresses the generic ping's own
  distance label whenever it did.
- **Duplicate stacking:** re-pinging the same item repeatedly stacked a new
  overlapping label each time rather than refreshing the existing one.
  Fixed: `ItemPingSpawner` now keeps a target-GameObject → highlight
  registry; a re-ping of an already-highlighted item calls
  `ItemPingHighlight.Refresh` (resets the timer, merges in any newly-found
  targets) instead of spawning a duplicate.
- **Name text clipping** (e.g. "Big Luggage" rendering with its leading "B"
  cut off): root-caused to `Indicators/ScreenSpaceTracker.Compute` only
  clamping points that are actually off-screen - a point technically
  on-screen but right at the pixel edge could still get a wide label
  clipped by the physical viewport border. Fixed generally (helps player
  labels/pings too, not just item pings): on-screen positions are now
  clamped to the same edge-margin-inset bounds as the off-screen case.
  Also widened the name text's own box (220px → 320px) and set explicit
  `TextOverflowModes.Overflow` as cheap additional insurance.
- **Grouping seeming "too strict":** largely explained by the detection
  rework below - once ping points reliably land on the actual item instead
  of nearby terrain, genuinely group-able clusters (multiple same-type items
  within one ping's detection radius) come up far more often. Grouping logic
  itself (flat group-by-display-name among one ping's detected targets) is
  unchanged.
- **The big one - pings phasing through items instead of landing on them**
  (a coconut up a tree, hard-to-hit small items like an energy drink/conch/
  flying disk, several items sitting side by side but only 1-2 detected):
  root cause confirmed via decompile - vanilla's own `PointPinger.
  TryGetPingHit` raycasts *only* against the `TerrainMap` layer
  (RESEARCH.md Q6), so the ping's landing point is wherever the ground/
  foliage is, never the item itself, even when the item visually blocks the
  crosshair - proximity search from that point then has to get lucky that
  the actual item fell within its radius. Fixed with what turned out to be
  the same technique `memiczny-PingItems` used before it broke (confirmed by
  reading its `PointPingerPatch.UpdateTranspiler`, an IL transpiler swapping
  the same layer-mask constant): a new Harmony prefix on the private
  `TryGetPingHit` (`PointPingerPatches.TryGetPingHitPrefix`) widens the
  raycast to `HelperFunctions.AllPhysicalExceptCharacter` (adds the
  `Default` layer items/luggage sit on) and casts it as a `SphereCast`
  rather than a plain ray (new `item-ping-hitbox-radius-meters` setting,
  default 0.35m) for forgiveness - aiming *near* an item, not pixel-perfect
  on its collider, now still lands the ping directly on it. New
  `enable-item-ping-hit-assist` master toggle (on by default; off restores
  vanilla's exact terrain-only raycast).
- **Jellyfish/giant urchins requested too:** `SlipperyJellyfish` is a real,
  simple class in the decompile, so it's now detected the same way as
  items/luggage (scene-wide `FindObjectsByType` each ping, since it has no
  static registry like `Item`/`Luggage` do). **Giant urchins are still not
  supported** - no dedicated component/class for them was found in the
  decompile (only a generic `Hazard_Urchins` settings-enum entry with no
  MonoBehaviour to key off), so this needs more research before it can be
  added; flagged to the maintainer as a known gap rather than silently
  dropped.

**Not yet re-verified in-game after this round of fixes** - next session (or
before shipping) should re-test all of the above, plus specifically: multiple
same-type items sitting side by side now getting grouped correctly, and the
hitbox assist not causing pings to land on unwanted `Default`-layer props
(non-item scenery) in a way that feels wrong.

**Phase 5b follow-up fix (2026-07-08, still session 3):** the physics-based
hitbox assist above turned out not to be enough - the maintainer reported
that a specific class of items remained unpingable *until picked up at least
once* (coconuts on a tree, berries on a bush, conches, flying disks, freshly-
spawned items from opened luggage), correctly guessing it was physics-
related since those exact items also can't be pushed until first picked up.
Confirmed via decompile: `Item.SetState`/`SetColliders` disable an item's own
collider (`SetColliders(enabled: false, isTrigger: true)`) while it's still
attached to its spawn point, only enabling it (`SetColliders(enabled: true,
isTrigger: false)`) once it transitions to `ItemState.Ground`/`Held` -
i.e. after first being picked up. No `Physics` raycast or spherecast,
however forgiving the radius, can ever hit a disabled collider - so the
hitbox-assist raycast patch could never help these items no matter how it
was tuned; a fundamentally different, physics-independent detection path was
needed. Added: `ItemPingDetector.FindNear` now also takes an approximate aim
ray (pinging character's head through the ping's landed point) and counts an
item/luggage/jellyfish as found if it's close enough to that ray - pure
vector math (closest-point-on-ray distance), never touching `Physics`, so it
works identically whether the target's collider is enabled or not. New
`enable-item-ping-ray-assist` (on by default) and
`item-ping-ray-assist-radius-meters` (default 0.6m) settings. The earlier
raycast-based `item-ping-hit-assist`/`item-ping-hitbox-radius-meters` are
kept as-is (still useful for landing the ping's own visible marker on an
already-active item's actual surface, not just detecting it) - the two
mechanisms are complementary, not a replacement for one another.
**Not yet re-verified in-game.**

**Phase 5b second follow-up fix (2026-07-08, still session 3):** the ray-
assist fix above only partially helped. The maintainer reported two more
symptoms in a fresh run: coconuts/berries on trees/bushes were *still*
completely unpingable even with ray-assist on, and starter items (Compass,
Flare, Binoculars, Lantern) were pingable near spawn but stopped being
pingable after walking ~50m away and coming back. Root cause, found by
tracing `Item.ALL_ACTIVE_ITEMS` more carefully in the decompile: it is
**not** a master list of every item, despite the name suggesting it - it's
`ItemOptimizationManager`'s own "recently relevant" cache. An item is only
ever added to it via `Item.WasActive()`, which `Item.OnEnable`/`Start` only
call when the item's own `Rigidbody.isKinematic` is false - so an item still
attached to its spawn point (a tree, a bush - matching the same kinematic
state the ray-assist fix's doc comment already described for colliders) is
**never added to the list at all**, regardless of any detection-radius or
ray-forgiveness tuning; and separately, `ItemOptimizationManager.Update`
actively **removes** any item from the list after 30 seconds without a
fresh `WasActive()` call - explaining the starter items going stale simply
from the player being far away for half a minute, unrelated to distance at
the moment of pinging itself. Both are independent bugs in the *previous*
detection approach (iterating `Item.ALL_ACTIVE_ITEMS`), not something the
ray-math/hitbox work from the prior fix round could ever have caught, since
the affected items were silently absent from the list being iterated in the
first place. Fixed: `ItemPingDetector.FindNear` now finds `Item`s via a
scene-wide `UnityEngine.Object.FindObjectsByType<Item>()` query each ping,
the same pattern already used for jellyfish, instead of reading
`ALL_ACTIVE_ITEMS` - sidesteps the optimization cache entirely, so kinematic
(still-attached) and long-untouched items are both found correctly.
`Luggage.ALL_LUGGAGE` is unaffected by this issue (it's a plain "still
closed" list with no time-based expiry) and is left as-is.
**Not yet re-verified in-game** - this is the third fix round for Phase 5b;
next session should do a thorough playtest pass covering every scenario
reported across all three rounds before considering this feature closed out.

**Phase 5b fourth fix round (2026-07-08, still session 3):** after the
scene-wide-query fix above, the maintainer found: (1) the generic ping's own
white distance label was *still* appearing alongside an item's distance
label despite the earlier suppression fix, (2) several more things still
unpingable - giant urchins, the campfire, spiders, spore bombs, beetles -
and asked whether some objects need to be manually special-cased (they do),
offering to help identify the remainder, and (3) a ripple jitter/sudden-grow
bug when re-pinging the same spot at close range.

- **Distance label still duplicating:** root cause was actually a step
  removed from where the earlier fix looked - `PingWidgetLink.Update` was
  re-reading `Plugin.Instance.Cfg.ShowPingDistanceLabel.Value` straight from
  config every single frame, which silently overwrote the suppression
  `PointPingerPatches` had computed and passed in at spawn time (the very
  next frame after spawning). Fixed by caching that decided value in a
  field and using it in `Update` instead of re-reading config.
- **More unpingable things:** confirmed via decompile that `Mob` (Beetle's
  base class) covers most creatures generically - detecting the base type
  picks up every Mob-derived species without a hardcoded list. `Spider` and
  `Capybara` don't inherit `Mob`, so they're detected explicitly alongside
  it. All three are hardcoded/derived display names (no name field on any of
  these classes, unlike Item/Luggage) gated by a new `enable-creature-pings`
  toggle (separate from item/luggage pinging). **Campfire, giant urchins,
  and spore bombs are still not supported** - campfire already has its own
  always-visible edge indicator (Phase 4), so pinging it doing nothing extra
  is an intentional non-issue rather than a gap; giant urchins/spore bombs
  still have no traceable class in the decompile. Added a debug-only helper,
  `ItemPingDetector.LogNearbyUnmatched` (runs when `enable-debug-logging` is
  on), that logs the name of every nearby collider not already a recognized
  type - the maintainer can ping near an urchin/spore bomb with debug
  logging on and read its real GameObject name straight from the log,
  no further decompile digging needed, then it can be added the same way
  jellyfish/Mob/Spider/Capybara were.
- **Ripple jitter on re-pinging the same spot:** `Pings/PingRipple` sizes
  itself relative to its source `PointPing`'s `transform.localScale` every
  frame, falling back to a hardcoded `1f` once that transform is gone.
  Re-pinging the same spot makes `PointPingerPatches` immediately
  `DestroyImmediate` the *previous* ping marker (only one tracked per
  `PointPinger` at a time) - the ripple, being free-standing rather than
  destroyed alongside it, was left reading a destroyed transform mid-fade
  and snapping to that `1f` fallback constant, which (especially up close,
  where the real distance-relative scale is well under 1) showed up as a
  sudden size jump/jitter right as the new ping took over. Fixed by freezing
  at the last observed scale instead of a hardcoded fallback.

**Not yet re-verified in-game** - please playtest this round specifically:
distance label no longer duplicating, beetles/spiders/capybaras now
highlighting when pinged, and no more ripple jitter on a same-spot re-ping.
If you can identify a giant urchin or spore bomb's logged GameObject name
(debug logging on, ping near one, check the log), pass it along and they can
be added directly without needing you to dig through decompiled sources.

**Phase 5b fifth fix round (2026-07-08, still session 3):** the maintainer
reported that most remaining gaps were fixed, but pinged some previously-
unpingable things and checked the debug log as suggested (Zombie, Spore
Bombs, Explosive Spore Bombs), and separately noticed spiders still have no
ping hitbox at all (the visible ping marker phases straight through one).

- **Zombie:** the debug dump excludes anything with a `Character` in its
  parent chain (to avoid re-logging players), which meant the game's own
  "zombie" mechanic (a dead player/NPC revived as hostile,
  `Character.isZombie`) never showed up in the log even though it isn't
  currently pingable - it's driven by a distinct `MushroomZombie` component,
  confirmed via decompile, so it's now detected the same way as
  Mob/Spider/Capybara (display name "Zombie").
- **Spore Bombs / Explosive Spore Bombs:** confirmed via the maintainer's own
  debug log that neither has a dedicated component at all (same situation
  the reference mod hit for its own "SporeShroom" - plain GameObject name
  matching, no class to key off). Log showed `Forest_SporeFungus` and
  `Jungle_SporeMushroomExplo` as the real names near where these were
  pinged; both are now detected via substring name-matching (so other
  biomes' differently-prefixed variants, if any, still match) against a
  bounded `Physics.OverlapSphere` around the ping point - "Spore Bomb" and
  "Explosive Spore Bomb" respectively.
- **Giant urchins still unidentified** - didn't show up distinctly in this
  round's log; `ItemPingDetector.LogNearbyUnmatched` (debug logging on) is
  still the way to find one.
- **Spider hitbox:** root cause found - Unity's `Physics` raycast/spherecast
  queries ignore trigger colliders by default regardless of layer mask or
  sphere radius, and a creature's actual hittable volume (e.g. Spider's own
  player-catch collider) is very plausibly a trigger, not a solid collider -
  so no amount of widening `TryGetPingHitPrefix`'s layer mask or sphere
  radius could ever have landed the ping marker on one. Fixed by passing
  `QueryTriggerInteraction.Collide` explicitly to both the `SphereCast` and
  `Raycast` calls there.

**Not yet re-verified in-game.**

**Phase 5b sixth fix round (2026-07-08, still session 3):** the maintainer
did a full biome-by-biome sweep, pinging every wiki-listed entity that
wasn't yet pingable and recording them in order in a new
`not-yet-pingable-entities.md` (gitignored, local-only checklist, not
shipped), then handed over both that list and the resulting `LogOutput.log`
(with the renderer-scan-less debug dump from the previous round) to cross-
reference. Confirmed matches, all now supported:
- **Antlion** - a real decompiled class (`Antlion : MonoBehaviour`), missed
  in the previous round's search pass.
- **Pickaxe** and **(Rusty) Piton** - turned out to be the *same*
  component, `ClimbHandle` (a climbable handhold anchor), distinguished only
  by its own `isPickaxe` flag - one fix covered both entries on the
  maintainer's list at once.
- **Poison Spore Bomb** - a plain (non-explosive) `Jungle_SporeMushroom`
  variant, sibling to the already-supported `Jungle_SporeMushroomExplo`.
- **Icicle** (`ShakyIcicleIce`), **Snow Pile** (`Snow Mount`), **Tumbleweed**
  (`tumbleweed(Clone)`, note the lowercase), **Cactus** (`Cactus base`) - all
  confirmed via the log's collider-name dump, added as more
  `ItemPingDetector.NamedHazards` entries.

**Still unresolved - didn't show up in this round's (Collider-only) log
dump:** Giant Urchin, Poison Ivy, Monstera, Geyser, Flash Bulb. Likely
explanation: these may have no physical `Collider` at all (decorative
foliage in particular is often a pure visual mesh), so a Collider-based
`Physics.OverlapSphere` sweep - all `LogNearbyUnmatched` did until now -
could never have revealed them regardless of how close the maintainer
pinged. Fixed the tool, not (yet) the gap: `LogNearbyUnmatched` now also
scans `Renderer`s scene-wide with a bounds-distance check, so a future sweep
near these five specifically should reveal their real names even without a
collider. Needs another maintainer pass (debug logging on, ping directly at
each of these five) before they can be added.

**Zombie still not pingable - flagged as a genuine open question, not
resolved this round.** The maintainer's log shows only generic "Roots,"
terrain clutter near where this was pinged (in the Tropics/root-cave area,
not literally a biome called "Roots" - see `not-yet-pingable-entities.md`),
no distinct creature collider name - but `MushroomZombie` detection was
already added the round before this one (RESEARCH.md/this file, fifth fix
round) and should, on paper, already cover it via `FindObjectsByType<
MushroomZombie>()` regardless of collider state, the same way Mob/Spider/
Capybara/Antlion do. Since that didn't visibly work, there's a real
open question here that a log dump alone couldn't resolve - possibilities
not yet ruled out: the specific "Zombie" the maintainer encountered might
not actually be a `MushroomZombie` instance (a different, not-yet-found
class for a themed variant), the GameObject might not be
`activeInHierarchy` at the moment of pinging (e.g. still in a "Sleeping"
state per `MushroomZombie.State`), or the build in use at the time might
have predated the `MushroomZombie` fix (needs confirming the game was fully
restarted after that deploy). **Next step:** have the maintainer retest
specifically against a `MushroomZombie` with `enable-debug-logging` on,
standing close when pinging it, and report back either (a) whether a
"Zombie" highlight appears at all (even mispositioned) or (b) what shows up
in the log's unmatched dump right at that moment - that will disambiguate
which of the above it actually is.

**Phase 5b seventh fix round (2026-07-08, still session 3):** a second
biome-by-biome maintainer sweep, cross-referenced against the same
`not-yet-pingable-entities.md` + `LogOutput.log` process as the previous
round.

- **Confirmed and fixed via the log's collider/renderer names:** Poison Ivy
  (`Jungle_PoisonIvy`), Monstera (`Monstera`/`Monstera (N)`), Geyser
  (literally named `Geyser`), Flash Bulb (`FlashPlant`) - all added to
  `ItemPingDetector.NamedHazards`.
- **Campfire:** added on request "for completeness of the item picker" even
  though it already has its own always-visible edge indicator (Phase 4) -
  pinging near the *current* segment's campfire (`MapHandler.CurrentCampfire`)
  now also highlights it like any other pingable, for consistency.
- **Cactus regression found and fixed:** the maintainer clarified they meant
  the small pickup-able cactus, not the big decorative structure - the
  previous round's `"Cactus base" -> "Cactus"` hazard mapping was actively
  wrong, matching the big `StickyCactus` structure's ground collider instead.
  Confirmed via decompile that the small pickup is a `CactusBall`
  `ItemComponent` on a regular `Item` - already covered by the existing Item
  loop with no dedicated fix needed. Removed the incorrect hazard mapping.
- **Stutter root-caused and fixed:** the maintainer reported a consistent
  stutter while spam-pinging (specifically noticed near a zombie, but not
  actually zombie-specific). The log showed why: debug logging had been left
  on throughout this whole investigation, and the previous round's
  `LogNearbyUnmatched` runs a full-scene `FindObjectsByType<Renderer>()`
  sweep on *every single ping* whenever debug logging is on - fine
  occasionally, a real stutter during rapid spam-pinging. Throttled to once
  every 3 seconds (`RendererScanCooldownSeconds`), and filtered the log's own
  noise (`SoD.`-prefixed ping ripples/widgets, the local player's own "Hand"
  view-model) which was cluttering every dump regardless of what was
  actually pinged.
- **Zombie still not resolved, but now instrumented for the next attempt:**
  no exceptions logged anywhere and the debug dump shows only generic
  terrain near where it was pinged, despite `MushroomZombie` detection
  (added two rounds ago) that should, on paper, already cover it. One
  concrete lead found this round: `MushroomZombie` has its own
  `distanceToEnable` field in the decompile, suggesting it may stay
  `SetActive(false)` until a player is close, which would make the default
  `FindObjectsByType<MushroomZombie>()` (excludes inactive objects) miss it
  even though it's visibly on-screen (if e.g. a separate always-active
  visual proxy is what's actually rendered). The Zombie loop now uses
  `FindObjectsInactive.Include` for its *search* (still gates the actual
  highlight on `activeInHierarchy`, so this alone doesn't change behavior
  for a genuinely-inactive zombie) plus a new diagnostic log line (fires
  whenever debug logging is on and at least one `MushroomZombie` exists
  anywhere in the scene): reports the total count found and the nearest
  one's distance from the ping point and its `activeInHierarchy` state.
  **Next step:** maintainer retests against a zombie with debug logging on
  and reports what that new log line says - `0 found` means it's not
  actually a `MushroomZombie` instance at all (a different, unidentified
  class); `found, activeInHierarchy=false` confirms the distance-gated
  deactivation theory (fixable by switching the actual match condition, not
  just the search, to ignore `activeInHierarchy`); `found, active=true, but
  far` means the ray-assist/detection-radius tuning needs adjusting instead.

**Not yet re-verified in-game.**

**Phase 5b eighth fix round (2026-07-08, still session 3) - Zombie resolved.**
The maintainer restarted fresh, started a run, and pinged the same zombie 5
times with the new diagnostic logging from the previous round on. The result
was unambiguous: `found 1 MushroomZombie(s) in scene; nearest is 44.6m-55.9m
from ping point, activeInHierarchy=True` across all 5 pings - a real,
active `MushroomZombie` exists, but it's consistently 44-56m away, meaning
it's an unrelated zombie elsewhere in the level, not the one actually being
aimed at. So `MushroomZombie` was never the right component for what the
maintainer meant. Root cause: `Character.isZombie` is a plain field
directly on the base `Character` class itself (confirmed via decompile - set
`true` both for a revived-dead-player and, separately, for pre-placed
hostile NPCs sharing the same `Character` type), completely distinct from
the `MushroomZombie` component. It was invisible to every previous debug
dump because `LogNearbyUnmatched` deliberately excludes anything with a
`Character` parent (to avoid re-logging players) - so a `Character`-based
zombie would never show up as "unmatched" regardless of whether it was
supported. Fixed: a new loop iterates `Character.AllCharacters` (the game's
own registry - a real, non-expiring list unlike `Item.ALL_ACTIVE_ITEMS`,
added in `Character.Awake`/removed in `OnDestroy`) filtering on
`character.isZombie`, labeled "Zombie". The old `MushroomZombie` loop is
kept alongside it (harmless, may cover a different in-game case) but is no
longer the primary fix. **Not yet re-verified in-game.**

**Phase 5b ninth round (2026-07-08, still session 3) - Zombie, take three.**
The `Character.isZombie` fix from the previous round also didn't work in a
fresh test (3 pings, nothing highlighted). The maintainer clarified an
important detail missed before: the target is a **naturally-spawned**
Roots-biome zombie (visibly mushroom-covered), not the "revived dead
player" mechanic - and pointed out the mushroom visual makes `MushroomZombie`
sound like the right component after all, contradicting the previous
round's read of its own diagnostic (a real `MushroomZombie` sitting 44-56m
from the ping point, in what was a *different level layout/run* - PEAK
regenerates each run, so that data point doesn't necessarily carry over).
Rather than pick one mechanism blind again, both are now kept active
simultaneously and diagnosed together: a combined log line (fires whenever
either finds anything at all, scene-wide) reports counts and nearest
distance for `MushroomZombie` instances and for `Character.isZombie`
instances separately in one line. **Next step:** maintainer retests against
the same kind of naturally-spawned zombie with debug logging on and reports
what that combined line says - this should finally disambiguate which (if
either) mechanism is the real one, or reveal that neither's `FindObjectsByType`/
`AllCharacters` result is showing up at all near the ping (pointing at a
totally different, still-unidentified class instead).

**Phase 5b tenth round (2026-07-08, still session 3) - Zombie resolved for
real.** After ~20 minutes hunting a rare naturally-spawned zombie, the
combined diagnostic gave an unambiguous answer: a real `MushroomZombie`
1.4-2.1m from the ping point (right at/just past the old 2m item-sized
default radius) and `0 zombie Character(s)` - confirming `MushroomZombie`
was the correct component all along (matching the maintainer's own read:
visibly mushroom-covered), just missed by a too-tight detection radius, not
a wrong-class problem. Fixed: all creature-type matches (`Mob`, `Spider`,
`Capybara`, `MushroomZombie`, `Antlion`) now use the larger `luggageRadiusSq`
(3.5m default) instead of the tighter `itemRadiusSq` (2m default) - creatures
move (unlike static loot) and are typically physically bigger than most
items, so they warrant the same "bigger target" forgiveness Luggage already
gets. The now-resolved `Character.isZombie` investigation code (both the
zombie-mechanism diagnostic and the "list all nearby Characters" diagnostic)
was removed - dead code once the mystery was solved, not worth carrying
forward. **Not yet re-verified in-game** that the widened radius alone is
now sufficient (should be, given the observed 1.4-2.1m near-misses are well
within the new 3.5m radius) - flag if a rare zombie sighting still doesn't
highlight.

**Phase 5b eleventh round (2026-07-08, still session 3) - Giant Urchin
resolved.** Name-based identification hit a wall for this one: its hitbox
GameObject is plainly named "Collider", parented directly under the level's
generic "Map" root, with no distinctive name anywhere in its own hierarchy -
confirmed by the maintainer spam-pinging (and even noclipping inside) one
directly, always producing the same generic name. `LogNearbyUnmatched` was
extended twice to chase this down: first to include `transform.root.name`
(still just "Map"), then to list attached component *types*, which revealed
a `CollisionModifier` (shared with `Antlion`, not distinctive alone) whose
parent carries `DisableBasedOnRunSettings` - a component with a public
`disableIfSettingDisabled` field naming the exact `RunSettings.SETTINGTYPE`
it's gated on. Reading that field directly (rather than guessing from a
name) gave a conclusive, unambiguous answer: `Hazard_Urchins`. Fixed:
`ItemPingDetector` now finds every `CollisionModifier` in the scene whose
parent's `DisableBasedOnRunSettings.disableIfSettingDisabled ==
RunSettings.SETTINGTYPE.Hazard_Urchins`, labeled "Giant Urchin" -
identification by component/field value instead of name-matching, a more
robust technique than `NamedHazards` for exactly this "no distinctive name
anywhere" case. `LogNearbyUnmatched`'s exclusion filter was updated to
recognize the same combination, so it stops appearing as log noise going
forward. **Not yet re-verified in-game.**

**Follow-up (still session 3):** the maintainer confirmed it works, but only
when noclipped inside the urchin to ping its "core" - not viable in normal
play. Root cause: the `CollisionModifier`'s own `transform.position` (used
as the match center) is apparently well inside the visible shell, not on its
surface, so the default 2m item radius only ever reached it from point-blank
range. Widened to double the luggage radius (7m default) specifically for
this detection, deliberately more generous than the plain creature radius
since this is a structural collider-root-to-surface offset, not just
"bigger/moving target" forgiveness. **Confirmed working** by the maintainer.

**Zombie, round two (still session 3):** re-tested against the same
naturally-spawned zombie (now confirmed `MushroomZombie`) after the urchin
fix - 15 pings, still no highlight, and the ping's own 3D marker visibly
phases through the zombie's body rather than landing on it. Root cause:
`MushroomZombie` uses `CharacterMovementZombie : CharacterMovement`, the
same movement controller real players use, meaning its collider is almost
certainly on the "Character" physics layer - deliberately excluded from
`TryGetPingHitPrefix`'s widened raycast (`AllPhysicalExceptCharacter`) to
avoid pings snapping onto teammates blocking the view, so the marker
continues past it to whatever's behind, same as vanilla always did. That's
a separate, accepted tradeoff (not fixed - re-including the Character layer
there would reintroduce the "pings stick to teammates" problem this file
already avoided on purpose). What *should* still work regardless of where
the marker visually lands is the ray-math-based highlight detection
(deliberately independent of `Physics`) - except the reconstructed aim ray
(head-to-point, not the true camera ray, since the RPC only carries the
final point/normal) diverges more than usual when the point itself landed
implausibly far past the zombie, and the existing ray-hitbox tolerance
(0.6m default) wasn't forgiving enough to close that gap. Fixed: creature
matching (`Mob`/`Spider`/`Capybara`/`MushroomZombie`/`Antlion`) now uses a
dedicated `MatchesCreature` check with a *doubled* ray-hitbox radius on top
of the already-widened luggage-sized point radius - both point-radius and
ray-alignment tolerance are now more forgiving specifically for
creatures. **Not yet re-verified in-game.**

**Correction (still session 3):** the maintainer pointed out `Capybara`
shouldn't have been swept into the same widened-radius treatment - unlike
the other creatures here, capybaras are static decoration (they don't move
or do anything), and fruit items are commonly placed right next to them, so
widening their catch radius makes it too easy to end up highlighting the
capybara when a nearby fruit was the thing actually meant to be pinged.
Reverted `Capybara` specifically back to the plain item radius/ray tolerance
(`Matches`, not `MatchesCreature`) - `Mob`/`Spider`/`MushroomZombie`/`Antlion`
(all of which genuinely move) keep the wider treatment.

**Zombie, round three (still session 3) - likely root cause found.** The
maintainer reported something new and much more telling: only the *first*
ping of many ever produced a highlight, and that highlight then stayed
fixed at the original ping location even as the (visibly moving) zombie
walked away - it never tracked the zombie at all, and every later ping (even
at point-blank range against its whole body while it was staggered on the
ground) failed to register anything. That pattern - a highlight that
doesn't track a moving target - points at `MushroomZombie`'s own root
`transform.position` simply not reflecting where its visible body actually
is (root-motion-less animation, or a fixed logic-anchor root with the real
movement happening on a child rig) - the exact same class of bug
`Item.Center()` already guards against elsewhere in the native game code
(it deliberately reads `mainRenderer.bounds.center`, not
`transform.position`, for this reason). Applied the same fix: a new
`ItemPingDetector.GetLiveCenter` helper prefers a child `Renderer`'s live
bounds center over the root transform, used for both matching and the
highlight's live position delegate. **Not yet re-verified in-game** - this
is a reasoned diagnosis from symptom pattern-matching against a known
codebase precedent, not confirmed via a fresh log/diagnostic pass the way
earlier zombie fixes were, so it may need another round if the guess is
wrong.

**Debug tooling added (still session 3):** hunting rare natural zombie
spawns for every test round was unsustainable, so two temporary dev/QA-only
additions (both off by default, `Debug` config section, not real shipped
features - remove once Phase 5b wraps): `ItemPings/ZombieDebugEsp.cs`
(`enable-zombie-debug-esp`) shows an always-visible through-walls label for
every `MushroomZombie` in the scene, refreshed once a second; its
`spawn-debug-zombie-key` (default F9) spawns a real, independent
`MushroomZombie_Player` a few meters ahead on demand. **Important
correction:** the first version of the spawn key called the game's own
`Character.Zombify()` console command directly - this turned out to
immediately kill/end the run for the maintainer, since `Zombify()` RPCs
`RPCA_Zombify` on the *local player's own* Character, and the spawned
zombie's follow-up `RPC_Arise` call finalizes that by calling
`spawnedFromCharacter.FinishZombifying()` - not a side-effect-free spawn at
all. Fixed by calling `PhotonNetwork.Instantiate("MushroomZombie_Player",
...)` directly and never firing that follow-up RPC - the spawned zombie
keeps its default `isNPCZombie = true` and is never linked to any player,
so it's indistinguishable from (and arguably a *better* test subject than)
a naturally-spawned Roots-biome zombie, with zero effect on the local
player.

**Update (config rework):** `spawn-debug-zombie-key` and its spawn code are
**removed** - Phase 5b is done and the key isn't needed any more. The ESP
(`enable-zombie-debug-esp`) is kept. The `Zombify()` finding above is left
on record because it's a live trap for anyone who reaches for that console
command again, not because the code still exists.

**Two real bugs found via this tooling, both fixed:**
1. **`MapHandler.CurrentCampfire` NullReferenceException outside a run.**
   The Campfire-detection loop added a few rounds back (`ItemPingDetector`,
   "requested for completeness") called `MapHandler.CurrentCampfire`
   directly with no guard - its getter throws a `NullReferenceException`
   when called outside an actual run (e.g. at the Airport, confirmed via the
   maintainer's own log after testing the debug spawner there).
   `ReceivePointRpcPrefix`'s outer try/catch silently swallowed this and
   fell back to vanilla ping behavior on *every single ping*, meaning **all**
   item-ping detection (not just the campfire check) was silently disabled
   the entire time the maintainer was testing at the Airport - a total,
   silent failure that had nothing to do with radius/matching logic despite
   looking exactly like one. `CampfireIndicatorController` already knew to
   guard on `MapHandler.ExistsAndInitialized` first; the new loop didn't.
   Fixed by adding the same guard.
2. **`GetLiveCenter` picking the wrong renderer.** Confirmed via the debug
   ESP against a freshly-spawned, reachable test zombie: `GetComponentInChildren
   <Renderer>()` is unordered across sibling children and returned
   `VFX_Kick` - an idle kick-attack particle effect that had never played,
   so its bounds sat at the Unity default `center (0,0,0), size zero` -
   ahead of any actual body mesh. This is exactly the "core somewhere in the
   void, correlates with distance but isn't the visible body" symptom the
   maintainer described. Fixed: `GetLiveCenter` now prefers a
   `SkinnedMeshRenderer` (the actual animated body mesh) first, and
   otherwise skips `ParticleSystemRenderer`/`TrailRenderer`/`LineRenderer`
   entirely before falling back to any other renderer type.

Both of these were plausibly present (in different forms) during *every*
prior zombie test this session, not just the debug-spawner ones - the
Airport NPE in particular could explain multiple "nothing happened" reports
if any testing occurred outside an active run.

**Confirmed fixed by the maintainer** - pinging a zombie now correctly
highlights it, tracking its actual body center live as it moves. This closes
out the Giant Urchin/Zombie investigation and, with it, Phase 5b as a whole:
every entity on the maintainer's `not-yet-pingable-entities.md` checklist
(gitignored, local-only - not shipped) has now been resolved except the
still-unidentified Giant Urchin *neighbors* that never came up again
(nothing else outstanding was reported).

**Debug tooling disposition:** maintainer opted to **keep** `ZombieDebugEsp`
+ the F9 debug-spawn key for now (both off by default, `Debug` config
section) rather than remove them immediately - useful if a similar
hard-to-reproduce issue comes up with another creature/hazard later. Revisit
before an actual public release; per its own doc comment this was never
meant to be a shipped feature.

Next up: **Phase 6, Mechanic 3 (ghost free-cam)** - `RESEARCH.md` Q10 has
the exact game classes/fields to hook for that.
