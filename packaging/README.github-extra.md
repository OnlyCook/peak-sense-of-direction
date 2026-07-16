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
