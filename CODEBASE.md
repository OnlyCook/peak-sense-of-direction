# CODEBASE.md — where things live

Brief map only — read `ROADMAP.md`/`RESEARCH.md` for the "why"/"how", this
file is just "where."

## `src/SenseOfDirection/` (the plugin, namespace `SenseOfDirection`)

- `PluginInfo.cs` — GUID/Name/Version constants.
- `Plugin.cs` — `BaseUnityPlugin` entry point (`Awake`), owns the `Harmony`
  instance and `PluginConfig`. New systems get wired up (instantiated /
  `Harmony.Apply`'d) here as phases land.
- `PluginConfig.cs` — every user-facing `ConfigEntry<T>`, grouped by section
  (`General`, `Debug`, `Player-Labels` so far; `General` holds cross-mechanic
  stuff like the shared UI toggle key, so it's bound first — section order
  in PEAKLib.ModConfig-style menus follows bind order). Mechanic 2/3
  settings get their own section as those phases land. Rebindable keys must
  be plain `ConfigEntry<KeyCode>`, not `KeyboardShortcut` — PEAKLib.ModConfig
  only renders a keybind widget for the handful of concrete types it knows
  (`bool`/`float`/`double`/`int`/`string`/`KeyCode`/enum); anything else
  falls through to its "unhandled setting type" case and never gets shown at
  all. See `~/Projects/GitHub/peak-checkpoint-save`'s own `PluginConfig.cs`
  for the same convention (that mod's the one PEAKLib.ModConfig is actually
  verified against).
- `SenseOfDirection.csproj` / `Directory.Build.props` — build config; game/
  BepInEx assembly paths, the `DeployToProfile` MSBuild target (see
  `CLAUDE.md` for the deploy command).

Mechanic 1 (player labels) is wired up as of Phase 3; Mechanic 2/3 still
unimplemented. As phases land, expect roughly (per `ROADMAP.md`'s phase
order):

### `Indicators/` (Phase 2, done)

- `ScreenSpaceTracker.cs` — pure static math, no MonoBehaviour/gameplay
  dependency: given a `Camera`, canvas size, and world position, returns
  on-screen canvas position, or a clamped-to-edge position + arrow angle when
  off-screen/behind-camera. Independent rect/ray-intersection derivation (not
  ported from `PingItems.Core.EdgeTracker`, which has no reusable license —
  see `RESEARCH.md` Q8).
- `IndicatorAnchor.cs` — a single tracked thing: world-position getter +
  active-check + the UI `RectTransform` widget (and optional off-screen arrow
  child) representing it. Generic; Mechanic 1/2/campfire each build their own
  widget hierarchy and register an anchor.
- `IndicatorManager.cs` — singleton owning the one full-screen overlay
  `Canvas`; `LateUpdate` drives every registered anchor's widget position via
  `ScreenSpaceTracker`. `Register/UnregisterAnchor` is the API later phases
  use.
- `IndicatorTestHarness.cs` — dev/QA only, gated by
  `PluginConfig.EnableIndicatorTestHarness` (off by default): spawns fixed
  dummy world points around the camera so the framework above can be verified
  visually in-game before Mechanic 1/2 wire in real players/pings.

### `Labels/` (Phase 3, done — player labels; campfire indicator is Phase 4, not yet built)

- `LabelDisplayMode.cs` — `Toggle`/`AlwaysOn`/`Hold` enum for
  `PluginConfig.DisplayMode`.
- `NativeAssets.cs` — lazily discovers PEAK's own native TMP font/outline
  material (scans `Resources.FindObjectsOfTypeAll<TextMeshProUGUI>()` for one
  using `DarumaDropOne-Regular SDF Outline`, per `RESEARCH.md` Q7) and the
  host/crown sprite (off a live `PlayerName.hostStar`, per Q2). Retried each
  frame until found — safe to call repeatedly, e.g. before the local player
  has spawned.
- `PlayerLabel.cs` — one player's widget: name + distance sub-line + host/
  dead/unconscious icons, built under `IndicatorManager`'s canvas and
  registered as an `IndicatorAnchor` (no off-screen arrow — that's reserved
  for pings per maintainer direction, labels just clamp quietly to the
  edge). Badges are stacked vertically (crown above the name, status badge
  below the distance line), not offset left/right — a horizontal offset can
  get pushed past the real screen edge and clipped when the label itself is
  already edge-clamped near that side. Has its own `CanvasGroup` for a
  vanilla-style fade (matches `UIPlayerNames.UpdateName`'s own
  `Time.deltaTime * 5f` fade rate); font size *and* the native font/material
  are all re-applied every `Refresh` call (not baked in once at creation) so
  config changes take effect without a restart and a label created before
  `NativeAssets` finishes discovering the native font still picks it up.
  Dead/unconscious icons use bundled badge art (`Common/IconAssets.cs`,
  PNGs under `Icons/`) — fixed tan color, not tinted per-player, unlike the
  compass/off-screen player face icons. Crown icon position hugs the actual
  rendered name width via `TMP_Text.GetPreferredValues` rather than a fixed
  offset.
- `PlayerLabelController.cs` — singleton owning one `PlayerLabel` per non-
  local, non-bot `Character`; drives the Toggle/AlwaysOn/Hold key logic and
  the per-frame content refresh (distance-meters conversion via
  `CharacterStats.unitsToMeters`, dead-label persistence via
  `character.gameObject.activeInHierarchy` per `RESEARCH.md` Q4, host/dead/
  unconscious icon state, name color from
  `character.refs.customization.PlayerColor` or `NativeAssets.DefaultTextColor`).
  Label anchor position is the same `IsLookedAt.playerNamePos` transform
  vanilla's own name label uses (falls back to `character.Head` if not
  found), and label fade target is computed by reimplementing
  `IsLookedAt.Update`'s own distance/view-cone formula (`RESEARCH.md` Q1),
  reading the thresholds straight off each character's own live `IsLookedAt`
  instance (not a hardcoded/duplicated copy, since the decompiled C# field
  defaults aren't guaranteed to match whatever's actually serialized onto
  the live prefab) — our label fades in exactly as vanilla's own fades out,
  or always shows if `replace-vanilla-labels` is on.
- `PlayerLabelPatches.cs` — Harmony patches (`AccessTools`-based, since
  `Character.Awake`/`OnDestroy` are private) registering/unregistering a
  label as characters spawn/despawn; confirmed as the right lifecycle hook by
  `RESEARCH.md` Q11.
- `VanillaLabelSuppressionPatch.cs` — backs `replace-vanilla-labels` (off by
  default): prefixes `UIPlayerNames.UpdateName` to force every native label
  slot inactive when the setting is on.

### `CampfireIndicator/` (Phase 4, done)

- `CampfireWidget.cs` — the on-screen widget: the game's own HUD campfire
  icon (`Labels.NativeAssets.CampfireIconSprite`, pulled from
  `StaminaBar.campfire` the same way `peak-checkpoint-save`'s `SavePicker`
  F7-menu title icon does) plus an optional distance sub-line in the native
  font/outline material. No off-screen arrow — reserved for Mechanic 2's
  ping indicator, same reservation `PlayerLabel` already follows; this
  widget just clamps quietly to the edge.
- `CampfireIndicatorController.cs` — singleton `MonoBehaviour` that
  re-resolves `MapHandler.CurrentCampfire` every `Update` (cheap, no Harmony
  hook on `GoToSegment` needed) and re-registers the `IndicatorAnchor`
  whenever it changes, so the indicator automatically follows segment
  advancement. Always instantiated from `Plugin.Awake`; no-ops per-frame when
  `Campfire/enable-campfire-indicator` is off (default off), same pattern as
  `PlayerLabelController`'s own master-switch check.
- `Labels/NativeAssets.cs` extended (not a new file) with
  `CampfireIconSprite` discovery alongside the existing font/host-star
  lookups — same lazy-retry-until-found approach.

### `Pings/` (Phase 5, done; playtest fixes applied same session)

- `PointPingerPatches.cs` — the whole mechanic's single Harmony patch set
  (RESEARCH.md Q6-Q9): a prefix on `PointPinger.ReceivePoint_Rpc` that fully
  replaces vanilla's own body (returns false) rather than running alongside
  it, since the harsh distance-based visibility early-exit vanilla uses lives
  inside that method with no prefix-only way to skip past it — reimplements
  the same spawn logic (raycast-independent here, point/normal already come
  in as RPC args) with our own anti-spam gate, optional visibility-cutoff
  bypass (`remove-visibility-cutoff`, on by default), ripple spawn, and
  indicator-widget attach. A prefix on the private `canPing` property getter
  lets dead players keep pinging as ghosts (`enable-ghost-ping`) by reading
  the also-private `inCooldown` property via `Traverse` instead of an
  index-based IL transpiler (Q9's fragility warning) — note this only takes
  effect for players who themselves have the mod, since the pinging client
  decides whether its own `canPing` bypasses the dead-check at all. A
  postfix on `PointPing.Go()` overwrites vanilla's own hard-clamped
  (`minMaxScale`, 0.2-3.0) `transform.localScale` with an uncapped recompute
  of the same frustum-relative formula times our multiplier, every frame -
  the clamp itself was why a ping's apparent screen size used to shrink past
  a certain distance; multiplying the already-clamped value (an earlier
  version of this patch) didn't fix that. A postfix on `PointPing.Awake()`
  overrides the shared `pingSound` SFX_Instance asset's `settings.range`
  (vanilla default 150) and feeds `PingClips` (a static `HashSet<AudioClip>`)
  for `PingAudioTuner` to consume. Anti-spam (`ShouldAcceptPing`) is a
  per-`Character` gradual/self-decaying ramp, skipped entirely for
  `Character.localCharacter` (only ever throttles *incoming* pings from
  other players, never the local player's own).
- `PingAudioTuner.cs` — a small always-running `MonoBehaviour` singleton
  (same registration pattern as `CampfireIndicatorController`) that polls
  `SFX_Player.instance.sources` (public fields, no reflection needed) every
  frame and forces `AudioRolloffMode.Linear` plus a configurable
  min/max distance onto whichever pooled `AudioSource` is currently playing
  a clip from `PointPingerPatches.PingClips`. Needed because boosting
  `SFX_Instance.settings.range` alone only pushes back
  `SFX_Player.PlaySFX`'s "don't even start playing" distance gate, not the
  actual rolloff curve baked onto the pooled source templates (not visible
  in the decompiled IL - RESEARCH.md Q6).
- `PingRipple.cs` — the "3D ripple" effect: an expanding translucent sphere
  (`GameObject.CreatePrimitive(PrimitiveType.Sphere)`, unlit alpha-blended
  material, color = pinging player's `PlayerColor`) rather than a flat 2D
  ring (the original version, which went near-invisible viewed edge-on).
  Free-standing (not ping-parented) but tracks the spawned `PointPing`'s own
  `transform.localScale` every frame so its size stays consistent relative
  to the ping marker itself at any distance, same reasoning as the Go()
  scale fix above. Grows + fades over ~1s then self-destroys.
- `PingWidget.cs` / `PingWidgetLink.cs` / `PingWidgetFadeOut.cs` — the
  ping's screen-space indicator: an optional distance sub-line plus the one
  widget type in the mod that actually uses `IndicatorAnchor.ArrowWidget`
  (labels and the campfire indicator both deliberately don't - see their own
  doc comments). No on-screen marker/dot (an earlier version had one - it
  just obstructed the already-visible 3D ping and was mistaken for an
  always-on arrow). `PingWidgetLink` is a `MonoBehaviour` attached directly
  onto the spawned `PointPing` GameObject so the widget's lifetime is tied
  1:1 to the ping's own; `OnDestroy` starts `PingWidgetFadeOut` (a 0.35s
  alpha fade via `CanvasGroup`, appearing stays instant) rather than
  snapping the widget away immediately.
- Ghost color: no separate ghost-body color source needed - `PlayerGhost`'s
  own `CustomizeGhost` reads the same `Customization.skins[skinIndex].color`
  array `CharacterCustomization.PlayerColor` does (confirmed in the
  decompile), so the existing `PlayerColor` read works unchanged whether the
  pinging character is alive or a ghost.
- **Not done this phase:** live-tuning the audio rolloff shape against an
  actual running `AudioSource`. (Item-ping compatibility, originally listed
  here as deferred, was superseded - see `ItemPings/` below.)

### `ItemPings/` (Phase 5b, done — native item/luggage ping highlighting)

Native replacement for the `.compatibility/memiczny-PingItems-1.6.2.zip`
dependency, which turned out to be broken against the current game build on
its own (see ROADMAP.md's Phase 5b writeup) - reimplements the *effect*
(highlight nearby items/luggage when a ping lands near them) from scratch
per RESEARCH.md Q8's reimplement-don't-copy rule (that mod ships no LICENSE).
Hooked into `Pings/PointPingerPatches.ReceivePointRpcPrefixImpl` (one call,
gated on `Item-Pings/enable-item-pings`), so item highlighting piggybacks on
the same anti-spam/ghost-ping gating the ping itself already went through.

- `ItemPingDetector.cs` — pure detection: given a ping point and separate
  item/luggage radii (meters, converted to world units via
  `CharacterStats.unitsToMeters`), finds nearby `Item`s, `Luggage` (off
  `Luggage.ALL_LUGGAGE`, which already excludes opened luggage on its own),
  and `SlipperyJellyfish`. Items and jellyfish are both found via a
  scene-wide `FindObjectsByType` each ping (cheap - runs once per ping, not
  per-frame) rather than any cached list: the reference mod reads
  `Item.ALL_ITEMS`, which doesn't exist at all in the current decompile, and
  its apparent replacement `Item.ALL_ACTIVE_ITEMS` turned out to be the
  wrong list too on closer inspection - it's a "recently relevant"
  optimization cache (`ItemOptimizationManager`), not a master list: an item
  is only added via `WasActive()`, which `Item.OnEnable`/`Start` only call
  when the item's `Rigidbody` isn't kinematic (so an item still attached to
  its spawn point - a tree, a bush - is never added at all), and
  `ItemOptimizationManager.Update` expires *any* item from the list after 30
  seconds without a fresh `WasActive()` call (so an untouched item goes
  stale simply from the player being elsewhere for half a minute). Both bugs
  independently caused real "can't ping this" reports; a direct scene query
  sidesteps both. Deliberately narrower than the reference mod otherwise: no
  spatial grid/pooling, no celestial/spore-shroom/capybara adapters, no
  `MirageLuggage` (no display name of its own), and no giant-urchin support
  yet (no dedicated class found in the decompile to key off, unlike
  jellyfish - needs more research).
- `ItemPingSpawner.cs` — orchestration: converts config radii, calls the
  detector, groups results by display name when `enable-item-ping-grouping`
  is on (a flat group-by-name, not the reference mod's iterative cluster
  search - everything found is already within one ping's detection radius of
  the same point, so nothing fancier is needed). Keeps a
  `Dictionary<GameObject, ItemPingHighlight>` registry so re-pinging an
  already-highlighted item/luggage merges into its existing highlight
  (`ItemPingHighlight.Refresh` resets the timer and folds in any newly-
  detected targets) instead of stacking a duplicate label on top of it -
  registry entries are dropped as soon as a highlight starts fading
  (`OnFadeStart`), not only once fully destroyed, so a re-ping mid-fade is
  free to start a fresh highlight rather than trying to revive a dying one.
  Returns how many targets were highlighted so the caller can suppress its
  own generic ping distance label when it's redundant with an item's.
- `ItemPingDetector.LogNearbyUnmatched` — debug-only (only runs with
  `enable-debug-logging` on): logs every collider name near a ping that
  isn't already a recognized type, so still-unsupported pingables (giant
  urchins, spore bombs - no distinct class found for either in the decompile)
  can be identified by their actual in-scene GameObject name without another
  decompile pass, then added the same way jellyfish/Mob/Spider/Capybara were.
- `ItemPingWidget.cs` — the on-screen widget: name label above an optional
  distance sub-line plus an off-screen arrow, same construction pattern as
  `Pings/PingWidget.cs` (arrow makes sense here, unlike player labels/the
  campfire indicator, since this points at a specific pinged object).
- `ItemPingHighlight.cs` — drives one highlight's lifetime on its own
  throwaway GameObject (not attached to the item/luggage's own GameObject,
  so it survives that being deactivated/destroyed - that's exactly the
  "no longer valid" signal it watches for): live group-center tracking,
  `item-ping-duration-seconds` countdown (reset by `Refresh` on a merge),
  early fade-out the instant every target in its group stops being valid.
  Reuses `Pings/PingWidgetFadeOut` unchanged (it only needs a
  `CanvasGroup`/`IndicatorAnchor` pair).

**Ray-based item-hitbox assist (physics-independent):**
`ItemPingDetector.FindNear` also accepts an approximate aim ray
(pinging character's head → the ping's landed point, reconstructed the same
way the ping marker's own rotation already is) and counts an item/luggage/
jellyfish as found if it's close enough to that ray, *regardless* of
distance to the landed point or of `Physics` entirely. This is the fix for
items that stay unpingable even with the raycast-based hitbox assist below:
per the decompile (`Item.SetState`/`SetColliders`), an item still attached to
its spawn point (an unpicked coconut on a tree, berries on a bush, something
freshly spawned from opened luggage) has its own collider *disabled* until
first picked up - the same reason it can't be pushed either - so no
`Physics` raycast/spherecast, however forgiving, can ever land on one.
Gated by `enable-item-ping-ray-assist` (`item-ping-ray-assist-radius-meters`
for the forgiveness radius).

Its `Physics.SphereCast`/`Raycast` calls also now pass `QueryTriggerInteraction.
Collide` explicitly - several creature hitboxes (e.g. Spider's own catch
volume) are trigger colliders, which Unity's physics queries ignore by
default regardless of layer mask/sphere radius, so the ping was passing
straight through them no matter how the raycast itself was tuned.

**Ping-raycast hitbox assist (physics-based):** `Pings/PointPingerPatches.TryGetPingHitPrefix`
prefixes the *private* `PointPinger.TryGetPingHit` (the method that computes
where a ping actually lands, RESEARCH.md Q6) to widen vanilla's `TerrainMap`-
only raycast to also hit the `Default` layer items/luggage sit on
(`HelperFunctions.AllPhysicalExceptCharacter`), optionally as a `SphereCast`
(`item-ping-hitbox-radius-meters`, default 0.35m) rather than a plain ray for
forgiveness - this is the actual fix for pings phasing through items instead
of landing on them (a coconut up a tree, a small dropped item), since the
ping's landing point itself is now often the item's own surface rather than
whatever terrain/foliage was behind it. Same underlying technique
`memiczny-PingItems` used via an IL transpiler (its `PointPingerPatch.
UpdateTranspiler`) before it broke - reimplemented as a plain Harmony prefix
instead, consistent with this file's existing preference for prefixes over
index-based IL transpilers (RESEARCH.md Q9).

**`Indicators/ScreenSpaceTracker`** also picked up a fix from this feature's
playtest: on-screen (not just off-screen) canvas positions are now clamped to
the same edge-margin-inset bounds, so a wide label (e.g. an item's name)
centered on a point right at the physical screen edge doesn't get its first
character clipped by the viewport border.

**Creature pings:** `Mob` (base class for most creatures - confirmed via
decompile that `Beetle : Mob`) is detected generically, so every Mob-derived
species is picked up without a per-species list; `Spider` and `Capybara`
don't inherit `Mob` so are detected explicitly. None of these carry a
display-name field like `Item`/`Luggage` do, so `Mob` falls back to its
GameObject name (`(Clone)` suffix stripped) - approximate, but the best
available without a hardcoded species→name table. Gated by
`enable-creature-pings` (separate from `enable-item-pings`, so creature
highlighting can be turned off independently).

**Zombie:** `MushroomZombie` was the right component all along (matching a
naturally-spawned Roots-biome zombie, not the "revived dead player"
mechanic) - a multi-round debug-logging investigation initially pointed
elsewhere (a `Character.isZombie` field also exists on the base `Character`
class, and an earlier live test found the only `MushroomZombie` in that
run's scene sitting a consistent 44-56m from the ping point, suggesting a
wrong-component problem), but a later diagnostic pass conclusively found a
real `MushroomZombie` only 1.4-2.1m from the ping - right at/just past the
old item-sized (2m) detection radius - while `Character.isZombie` was 0
every time. So the actual bug was detection radius, not component choice.
Matched against `luggageRadiusSq` (3.5m default) like every other creature
type below, not the tighter item radius - see the reasoning there.

**Name-matched hazards:** several pingables have no dedicated component in
the decompile at all - same situation the reference mod hit for its own
"SporeShroom", which it matched by plain GameObject name rather than a
class. `ItemPingDetector.NamedHazards` is an ordered substring→display-name
table, identified via `LogNearbyUnmatched` against real maintainer
playthroughs (a full biome-by-biome sweep, cross-referenced against a
wiki-sourced checklist the maintainer kept in `not-yet-pingable-entities.md`,
gitignored/local-only, not shipped): `Forest_SporeFungus` ("Spore Bomb"),
`Jungle_SporeMushroomExplo` ("Explosive Spore Bomb"), a plain
`Jungle_SporeMushroom` variant ("Poison Spore Bomb"), `ShakyIcicleIce`
("Icicle"), `Snow Mount` ("Snow Pile"), `tumbleweed(Clone)` ("Tumbleweed"),
`Cactus base` ("Cactus"). Matched by substring, not exact equality, and
checked most-specific-first (`SporeMushroomExplo` before the shorter
`SporeMushroom`, which would otherwise also match it), so other biomes'
differently-prefixed prefab variants likely still match. Found via a bounded
`OverlapSphere` (not a full scene query) since, unlike tree-attached items,
these sit right on the ground where you'd ping directly at them - no
ray-assist reach needed.

`Antlion` (a real decompiled class) and `ClimbHandle` (the same component
for both Pickaxe and (Rusty) Piton, distinguished by its own `isPickaxe`
flag - hardcoded "Pickaxe"/"Piton" labels rather than its own `GetName()`,
since that returns the less clean `"PITONPROMPT"` localization key for the
piton case) are both detected directly, same pattern as Spider/Capybara/
MushroomZombie.

`LogNearbyUnmatched` now also scans `Renderer`s (bounds-distance check), not
just `Collider`s via `OverlapSphere` - some still-requested pingables (giant
urchins, decorative foliage like Poison Ivy/Monstera) may have no physical
collider at all, so a Collider-only sweep could never have revealed them.
Poison Ivy (`PoisonIvy`), Monstera, Geyser, and Flash Bulb (`FlashPlant`)
were found in a follow-up sweep and added to `NamedHazards`.

**Giant Urchin** couldn't be identified by name at all - its hitbox
GameObject is plainly "Collider" under a generic "Map" root, with nothing
distinctive anywhere in its own hierarchy. Identified instead by component:
it carries a `CollisionModifier` (shared with `Antlion`, not distinctive
alone) whose parent has `DisableBasedOnRunSettings` with a public
`disableIfSettingDisabled` field naming the exact `RunSettings.SETTINGTYPE`
it's gated on - reading that field directly gave a conclusive
`Hazard_Urchins`. Detected via a dedicated loop (not `NamedHazards`, since
there's no name to match) checking for that exact
`CollisionModifier`+`DisableBasedOnRunSettings(Hazard_Urchins)` combination.
The renderer half of `LogNearbyUnmatched`'s sweep is throttled to once every
`RendererScanCooldownSeconds` (3s) - running a full-scene
`FindObjectsByType<Renderer>()` on *every* ping while debug logging is on
was a real, reported stutter during rapid spam-pinging, not just a
theoretical cost. The log also filters out this mod's own `SoD.`-prefixed
spawned objects and the local player's own first-person "Hand" model, which
were cluttering every dump as noise regardless of what was actually pinged.

`MapHandler.CurrentCampfire` is also detected (hardcoded "Campfire"), added
purely for pinning completeness even though it already has its own
always-visible edge indicator from Phase 4.

**Known-wrong mapping removed:** an earlier `"Cactus base" -> "Cactus"`
entry incorrectly matched the big decorative `StickyCactus` structure's
ground collider - the maintainer actually meant the small pickup-able
cactus, which is a `CactusBall` `ItemComponent` on a regular `Item` (already
covered by the Item loop, no dedicated fix needed) confirmed via decompile.

**Two bugs fixed outside `ItemPings/` itself, found via this feature's
playtest:**
- `Pings/PingWidgetLink.Update` was re-reading `ShowPingDistanceLabel`
  directly from config every frame, silently overwriting the one-time
  `showDistance` value `PointPingerPatches` computes at spawn (which factors
  in whether an item ping is already showing its own distance) - the
  generic ping's white distance label was reappearing the very next frame
  after being suppressed. Fixed by caching the decided value in a field
  instead of re-reading config in `Update`.
- `Pings/PingRipple` tracked its source `PointPing`'s `transform.localScale`
  every frame for its own relative sizing, falling back to a hardcoded `1f`
  once that transform was gone. Re-pinging the same spot makes
  `PointPingerPatches` immediately `DestroyImmediate` the *previous* ping
  marker (only one tracked per `PointPinger` at a time) - since the ripple
  is free-standing, not destroyed with it, it was left reading a destroyed
  transform mid-fade and snapping to that `1f` fallback, which (especially
  up close, where the real distance-relative scale is well under 1) showed
  up as a sudden jitter/growth spike. Fixed by freezing at the last observed
  scale instead of falling back to a constant.

### `GhostFreeCam/` (Phase 6, done — Mechanic 3, ghost free-cam)

- `GhostFreeCamPatches.cs` — bespoke WASD/Space/Ctrl/Shift-sprint flight
  controller with mouse look reusing `CharacterInput.lookInput` (kept live
  while dead) scaled by the player's own real Mouse/Controller Sensitivity
  and Invert X/Y settings, matching vanilla's `CharacterMovement.CameraLook()`
  formula exactly. Not a reuse of PEAK's own dormant `GodCam` controller as
  RESEARCH.md Q10 originally proposed — in-game testing found it unusably
  slow/unresponsive (legacy `Input.GetAxis` mouse axes nothing else in the
  shipped game drives camera look through), so this mechanic ended up
  bespoke rather than wholesale-reused, per that research doc's own
  documented fallback plan.
- `GhostFreeCamPoseSync.cs` / `GhostFreeCamConfigSync.cs` — keeps the ghost's
  networked body/lean state and the host-controlled subset of config
  (`enable-ghost-free-cam`, leash distance, unlimited-range) in sync across
  clients.

### `Compass/` (Phase 7, done — top-of-screen compass tape)

Ad hoc addition (not in the original `ROADMAP.md` phase list) requested
after Phase 6 landed: a native-HUD-styled compass strip at the top of the
screen showing every registered `Indicators.IndicatorAnchor` that opts in,
as an alternative (or supplement) to the edge-of-screen widgets Phases 2-5b
already built. Read Coomzy-Compass_UI-1.0.1 (`scratch/decomp/`) as an
architectural reference only — same license situation as every other
reference zip (no LICENSE file, all-rights-reserved by default, see
RESEARCH.md's license table) — nothing here is copied from it.

- `Indicators/IndicatorDisplayMode.cs` (new) / `IndicatorAnchor.cs` (extended) —
  each mechanic's anchor now optionally carries a `CompassKind` plus
  color/label/dead/unconscious getters and a per-type `OffScreenOnly` /
  `CompassOnly` / `Both` display-mode delegate, so `CompassManager` can
  render its own marker for an anchor without any mechanic registering
  twice. `IndicatorManager` gates the *original* off-screen widget/arrow on
  this same mode (hidden entirely in `CompassOnly`) and exposes its anchor
  list read-only for the compass to consume.
- `CompassManager.cs` — singleton owning the tape's own `Canvas` (top-center
  anchored, sized/positioned every frame from config so width/height/offset
  changes apply live), heading ticks, and a `Dictionary<IndicatorAnchor,
  CompassMarkerWidget>` synced each frame against `IndicatorManager.Anchors`
  (create on first sight, destroy once the anchor's gone). Bearing math is
  plain `Mathf.DeltaAngle` yaw subtraction mapped *linearly* onto the tape
  width (deliberately not Coomzy's own acos/dot-product curve - keeps
  degree-number ticks trivially aligned with markers), with markers/ticks
  fading out over the last quarter of the visible half-FOV rather than
  popping at the exact cutoff. Visual style went through two revisions
  after playtest feedback: a first pass built a bordered blue-ish panel
  background (styled after `peak-checkpoint-save`'s own F1/F7 panel), which
  was then dropped entirely in favor of matching Coomzy-Compass_UI's more
  minimal look instead - no background box, no "current heading" pointer
  (forward is always the tape's own center by construction), just ticks/
  markers floating over the world resting on one continuous baseline line.
  `compass-requires-holding-item` (off by default) gates the whole tape on
  `Character.localCharacter.data.currentItem` having a `CompassPointer`
  child component - PEAK has no dedicated "Compass" item class, it's a
  data-driven `Item` like any other, identified this way instead.
- `CompassIcons.cs` — placeholder shapes generated procedurally once and
  cached; only the elevation-arrow triangle and the tape's horizontal
  fade-line baseline are left here now (the campfire marker reuses
  `Labels.NativeAssets.CampfireIconSprite`, same as its off-screen
  counterpart; the player face/ping ring/item-ping diamond markers all moved
  to real bundled art via `Common/IconAssets.cs`, below).
- `Common/IconAssets.cs` (new, though the `Common/` folder itself predates
  this) — loads the mod's bundled icon PNGs (embedded resources under
  `Icons/`, see the `.csproj`'s `EmbeddedResource` glob) into cached
  `Sprite`s via `Assembly.GetManifestResourceStream` + `Texture2D.LoadImage`.
  The player face/ping/item-ping icons are drawn white-fill + black-outline
  on transparent specifically so `Image.color` tinting reproduces the
  anchor's color exactly (white × tint = tint) while leaving the outline
  untouched (black × tint = black); the two player-label status badges are
  pre-colored (fixed tan) and never tinted.
- `CompassMarkerWidget.cs` — one marker: a `CompassMarkerKind`-dependent icon
  tinted to the anchor's own color (player character color / ping color;
  campfire keeps its real sprite's own colors) — for the player marker the
  icon sprite itself swaps between normal/unconscious/dead face art as state
  changes, rather than a separate smiley overlay — plus a dead/unconscious
  status badge (`IconAssets.DeadBadge`/`UnconsciousBadge`, same art
  `Labels.PlayerLabel` uses, untinted), an elevation arrow (only shown once
  `compass-elevation-threshold-meters` is exceeded - plain `↑`/`↓` glyphs on
  TMP's own default font rather than a drawn triangle, deliberately *not*
  the game's stylized display font since a general-purpose font is far more
  likely to have those Unicode glyphs baked into its atlas), and optional
  name/distance sub-text.
- `CompassTick.cs` — the 24 fixed heading marks (every 15°, N/E/S/W always
  lettered and taller/thicker than the plain degree ticks between them,
  others blank unless `compass-show-degree-numbers` is on), created once
  and repositioned/refaded every frame as the camera turns. True north gets
  `CompassTheme.NorthAccent` (dark red) instead of plain white, on both its
  tick line and its "N" label - the one splash of color on an otherwise
  monochrome tape, common real-world-compass convention.
- Wired into `Labels/PlayerLabelController.cs` (compass visibility follows
  the same toggle-key/Hold/max-distance gate as the off-screen label, via
  `_labelsVisible`), `CampfireIndicator/CampfireIndicatorController.cs`,
  `Pings/PingWidgetLink.cs` (suppresses its own `CompassKind` entirely when
  the same ping already got an item-ping marker - showing both is
  redundant, same reasoning `PointPingerPatches` already applied to the
  generic ping's distance label), and `ItemPings/ItemPingHighlight.cs`
  (exposes its live group display name through a small cached field for
  the compass label, since `Update()` only computed it as a local before).
  Always instantiated from `Plugin.Awake` — no-ops (whole UI hidden) when
  `Compass/enable-compass` is off, same pattern as every other always-on
  controller in this mod.

### Not yet built

Nothing — every phase in `ROADMAP.md` plus the ad hoc Phase 7 compass
addition above is implemented. Remaining `ROADMAP.md` phases (7→8 in that
doc's own numbering: compatibility pass, packaging/release) are process/QA
work, not new code.

Update this section's file list as those land — keep it to one line per
file, don't restate what's already in `ROADMAP.md`/`RESEARCH.md`.

## `packaging/`

Thunderstore package sources (manifest, icon, README halves, changelog) +
`build-release.sh` (assembles `dist/SenseOfDirection-<version>.zip`) +
`gen-readme.sh` (regenerates root `README.md`). See `CLAUDE.md`'s markdown
index for what each file is.

## `scratch/decomp/` (gitignored)

Decompiled reference material from the Phase-1-preceding research pass —
raw unzipped reference-mod packages + ilspycmd output, and pointers to the
sibling `peak-checkpoint-save` repo's full game decompile. Layout and exact
paths are documented in `RESEARCH.md`'s "Decompiled output locations"
section — don't re-decompile anything without checking there first.

## `dist/` (gitignored)

Build output only (`build-release.sh` writes versioned release zips here).
