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
  Dead/unconscious icons are plain colored squares for now (no ripped asset
  mandated for those in `ROADMAP.md`, unlike the crown) — swap for real art
  in a later polish pass if wanted. Crown icon position hugs the actual
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
- **Not done this phase, explicitly deferred (RESEARCH.md Q8 calls this a
  "sub-task, not a blocker"):** `memiczny-PingItems` item-ping compatibility
  (a soft-dependency shim registering its `PingHighlighter` instances into
  the same indicator system) and live-tuning the audio rolloff shape against
  an actual running `AudioSource`.

### Not yet built

- Phase 6: Mechanic 3 (ghost free-cam) — likely a small patch/reflection
  helper toggling `MainCameraMovement.isGodCam` (see `RESEARCH.md` Q10).

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
