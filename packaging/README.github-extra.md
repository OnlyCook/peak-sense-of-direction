## Requirements

- [BepInExPack PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) `5.4.2403`

## For players

- You can install the mod through r2modman as `Sense_of_Direction`
- Or on Thunderstore as `Sense of Direction` ([Website](https://thunderstore.io/c/peak/p/OnlyCook/Sense_of_Direction/))

## For developers

- [`ROADMAP.md`](ROADMAP.md): full feature spec, phased plan, status, handoff notes.

Build:
```bash
cd src/SenseOfDirection
dotnet build -c Release                          # -> bin/Release/SenseOfDirection.dll
dotnet build -c Release -p:DeployToProfile=true  # also copy into the r2modman profile
```

## Credits

- Item/luggage ping highlighting was inspired by
  [memiczny's PingItems](https://thunderstore.io/c/peak/p/memiczny/PingItems/)
  mod, which (as of this writing) no longer works against the current game
  version. Sense of Direction's version is an independent reimplementation
  (PingItems ships no LICENSE file, so its source isn't reused), built
  natively into this mod's own ping pipeline rather than depended on.
