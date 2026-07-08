# Sense of Direction

**Always know where everyone is — even off-screen.**

> **Early WIP (v0.1.0).** This is an empty project scaffold only — no gameplay
> features are implemented yet. See [`ROADMAP.md`](ROADMAP.md) for the full
> planned feature set and phased implementation order.

PEAK's native player labels only show up within a short range and a narrow
view-cone, and never show distance. Existing mods that add distance numbers
still hide the label entirely once a player goes off-screen, so finding a
teammate who wandered off (or fell, or died) still means physically turning
around or scanning the level — not always possible mid-climb.

Sense of Direction is a **fully client-sided** mod (only you need it
installed) that will add:

- **Edge-of-screen player labels** with distance, a host crown, and
  unconscious/dead status icons — labels stick to the screen edge with a
  directional indicator instead of disappearing when a player is off-screen
  or out of your view.
- **Better pings**: distance-relative scaling, a color ripple effect,
  drastically reduced audio distance falloff, the same off-screen indicator,
  a distance label, and an anti-spam cooldown.
- **Ghost free-cam**: fly around freely as a dead player instead of being
  stuck in a fixed spectate camera.

Fully open source, MIT licensed.
