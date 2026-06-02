# Ability Toggles

Ability Toggles is a BepInEx mod for Hollow Knight: Silksong.

Current state: Silk Skills, Swift Step, Cling Grip, Faydown Cloak, Drifter's Cloak, Silk Soar, Needolin, Clawline, Needle Strike, Sylphsong, Beastling Call, and Elegy of the Deep.

Silk Skills are virtualized through their `ToolItem` unlock checks so they appear in the crest UI without setting their progression flags.
The mod currently patches `HeroController.CanDash()` so Swift Step can be used without setting `PlayerData.hasDash`.
It also patches the walljump checks inside `HeroController` so Cling Grip can be used without setting `PlayerData.hasWalljump`.
Faydown Cloak is patched through the normal double-jump permission checks without setting `PlayerData.hasDoubleJump`.
Drifter's Cloak is patched through the normal float permission checks without setting `PlayerData.hasBrolly`.
When LB / Quick Map starts Drifter's Cloak, that Quick Map press is consumed until LB is released so landing does not immediately open the map.
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

Global ability toggles and save ability toggles are disabled by default. The save-scoped `Bind Drifter's Cloak to LB/L1` option is enabled by default.

Available sections:

- `Silk Skills`: Silkspear, Thread Storm, Cross Stitch, Sharpdart, Rune Rage, Pale Nails.
- `Ancestral Arts`: Swift Step, Cling Grip, Needolin, Clawline, Silk Soar, Sylphsong.
- `Needolin Memories`: Beastling Call, Elegy of the Deep.
- `Other Abilities`: Needle Strike, Drifter's Cloak, Faydown Cloak.
- `Additional Options`: Bind Drifter's Cloak to LB/L1.

## Build

From the game directory:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\ModDev\AbilityToggles\build.ps1
```

The default build target is development mode:

```text
BepInEx/scripts/AbilityToggles.dll
```

For a normal BepInEx plugin install:

```powershell
powershell.exe -ExecutionPolicy Bypass -File .\ModDev\AbilityToggles\build.ps1 -Target Plugin
```
