# Ability Toggles

Ability Toggles is a BepInEx mod for Hollow Knight: Silksong.

Current state: Silk Skills, Swift Step, Cling Grip, Faydown Cloak, Drifter's Cloak, Silk Soar, Needolin, Clawline, Needle Strike, Sylphsong, Beastling Call, and Elegy of the Deep.

Silk Skills are virtualized through their `ToolItem` unlock checks so they appear in the crest UI without setting their progression flags.
The mod currently patches `HeroController.CanDash()` so Swift Step can be used without setting `PlayerData.hasDash`.
It also patches the walljump checks inside `HeroController` so Cling Grip can be used without setting `PlayerData.hasWalljump`.
Faydown Cloak is patched through the normal double-jump permission checks without setting `PlayerData.hasDoubleJump`.
Drifter's Cloak is patched through the normal float permission checks without setting `PlayerData.hasBrolly`.
When the mapped Quick Map action starts Drifter's Cloak, that Quick Map press is consumed until that action is released so landing does not immediately open the map.
Silk Soar is patched through the normal super-jump permission check without setting `PlayerData.hasSuperJump`.
Needolin is patched through the normal Needolin permission check without setting `PlayerData.hasNeedolin`.
Clawline is patched through the normal harpoon-dash permission check without setting `PlayerData.hasHarpoonDash`.
Needle Strike is patched through the normal nail-charge permission check without setting `PlayerData.hasChargeSlash`.
Sylphsong regenerates silk over time while resting on a bench without setting progression flags.
Beastling Call is patched through the normal teleport permission checks without setting `PlayerData.UnlockedFastTravelTeleport`.
Elegy of the Deep is patched through the normal Needolin memory powerup check without setting `PlayerData.hasNeedolinMemoryPowerup`.

## Config

Settings appear under `Ability Toggles` in Configuration Manager.
Global toggles apply to every save, and each save slot also has its own values through Save Scoped Config.
An ability is enabled if either its global toggle or active-save toggle is enabled.

Global ability toggles and save ability toggles are disabled by default. The save-scoped extra cloak controls option is enabled by default; its existing config key is preserved for upgrades.

Available sections:

- `Silk Skills`: Silkspear, Thread Storm, Cross Stitch, Sharpdart, Rune Rage, Pale Nails.
- `Ancestral Arts`: Swift Step, Cling Grip, Needolin, Clawline, Silk Soar, Sylphsong.
- `Needolin Memories`: Beastling Call, Elegy of the Deep.
- `Other Abilities`: Needle Strike, Drifter's Cloak, Faydown Cloak.
- `Additional Options`: enable extra Drifter's Cloak controls.
- `Controls`: select a logical game action (Quick Map by default), an optional keyboard key, and an optional mapped controller button. `None` disables an additional binding. The normal Jump input still works.
- `Controls and Status`: live information showing normal acquisition, global overrides, and save overrides. Beastling Call still needs the game's fast-travel unlock; Elegy needs Needolin; Sylphsong works while resting at a bench.

The logical action follows game rebinding, so Quick Map does not assume a physical LB button. Quick Map presses used for floating are consumed until released. Diagnostic detection logging is disabled by default and bounded when enabled.

Requires BepInEx 5 and Save Scoped Config **0.4.0 or newer**. Configuration Manager is recommended for the controls and status panel. Startup checks report unsupported game APIs and roll back this mod's patches if patching fails; other mods can continue loading.



## Build and package

Requires the .NET 8 SDK and a local Silksong installation with BepInEx 5. Run from this repository:

```sh
python3 build.py --game-dir "/path/to/Hollow Knight Silksong"
python3 build.py --game-dir "/path/to/Hollow Knight Silksong" --package
```

`SILKSONG_GAME_DIR` can supply the game directory. Use `--dotnet /path/to/dotnet` for a local SDK. Use `--save-scoped-dll /path/to/SaveScopedConfig.dll` when building against a newer helper than the installed copy.
Builds go to `bin/Release/netstandard2.1`. Packaging writes `dist/AbilityToggles-<version>.zip` with the plugin, README, and SHA-256 checksum; it includes no game assemblies. `--check` validates version metadata without building. Identical inputs produce identical package contents and timestamps. Release metadata excludes Git revision suffixes so checked-in binaries do not identify an earlier source commit.

Builds do not deploy by default. Install the DLL under `BepInEx/plugins/AbilityToggles/` after closing the game. On Windows, `build.ps1 -GameDir "C:\path\to\game"` also builds locally; explicit `-Target Plugin` deploys a plugin and `-Target Dev` deploys for ScriptEngine. Do not install both copies.
