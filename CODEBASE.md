# CODEBASE.md — where things live

Brief map only — read `ROADMAP.md`/`RESEARCH.md` for the "why"/"how", this
file is just "where."

## `src/SenseOfDirection/` (the plugin, namespace `SenseOfDirection`)

- `PluginInfo.cs` — GUID/Name/Version constants.
- `Plugin.cs` — `BaseUnityPlugin` entry point (`Awake`), owns the `Harmony`
  instance and `PluginConfig`. New systems get wired up (instantiated /
  `Harmony.Apply`'d) here as phases land.
- `PluginConfig.cs` — every user-facing `ConfigEntry<T>`, grouped by section.
  Currently just `Debug` (empty scaffold otherwise — Mechanic 1/2/3 settings
  from `ROADMAP.md` get added here as their phase is implemented).
- `SenseOfDirection.csproj` / `Directory.Build.props` — build config; game/
  BepInEx assembly paths, the `DeployToProfile` MSBuild target (see
  `CLAUDE.md` for the deploy command).

No gameplay code exists yet as of Phase 1 (empty scaffold only). As phases
land, expect roughly (per `ROADMAP.md`'s phase order):
- Phase 2: a screen-space edge-of-screen indicator framework (shared by
  Mechanics 1 and 2) — likely its own file, e.g. `EdgeIndicator.cs` /
  `ScreenSpaceTracker.cs`.
- Phase 3-4: Mechanic 1 (player labels + campfire indicator) — a Harmony
  patch on `Character.Awake`/`OnDestroy` for label lifecycle (see
  `RESEARCH.md` Q11), likely `PlayerLabelManager.cs` + a per-label class.
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
