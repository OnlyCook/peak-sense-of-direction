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

### Not yet built

- Phase 4: campfire indicator — same `IndicatorAnchor` mechanism as player
  labels, pointed at a fixed world point instead of a moving `Character`.
- Phase 5: Mechanic 2 (better pings) — Harmony patches on
  `PointPinger.Update`/`ReceivePoint_Rpc` (see `RESEARCH.md` Q6-Q9), likely
  `PingPatch.cs`.
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
