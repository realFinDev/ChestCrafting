# ChestCrafting

A small **client-side** [BepInEx](https://github.com/BepInEx/BepInEx) mod for Valheim: crafting stations and building use ingredients from **nearby chests**, without duplicating items.

Only the player who wants it installs it. The server and other players don't need it.

Why it exists and how it works: [Why my Valheim 'craft from chests' mod crafted 70 muckshakes from one click](https://realfin.dev/blog/valheim-craft-from-containers-duplication-bug/).

## Features

- Works at **every crafting station** (workbench, forge, cauldron, stonecutter, artisan table, black forge, galdr table, food preparation table, mead ketill) and when crafting from the inventory.
- **Building** with the hammer, hoe and cultivator (can be turned off).
- Takes ingredients from **your inventory first**, then from the **nearest chests**.
- Requirements show **available/needed**, e.g. `47/3`.
- Keeps vanilla behaviour: one click = one craft, Shift = ×5 multi-craft, item upgrades.

### Safe in multiplayer

- Takes **ownership** of a chest before changing it (like opening it does), so changes are saved and not overwritten.
- Skips chests **another player has open**, other players' **private chests**, chests in **wards you can't access**, and **gravestones**.
- Uses the game's public inventory methods only, no reflection on private game methods.

### Not covered

- Smelters, kilns, blast furnaces, fermenters, cooking stations, fuel. They don't use the crafting window.
- Recipes where *any one* of several ingredients will do only use your inventory.

## Install

1. Install BepInEx for Valheim (e.g. with [r2modman](https://thunderstore.io/package/ebkr/r2modman/)).
2. Download `ChestCrafting.dll` from the [Releases](../../releases) page.
3. Put it in `BepInEx/plugins/` (in r2modman: *Settings → Browse profile folder*).
4. Don't run it together with another "craft from containers" mod.

## Config

`BepInEx/config/realfin.ChestCrafting.cfg` (created after the first start; r2modman's *Config editor* shows it):

| Setting | Default | |
|---|---|---|
| `Enabled` | `true` | Turn the mod on/off |
| `Range` | `20` | Max distance to a chest in meters (1–50) |
| `ForBuilding` | `true` | Also use chest items when building |
| `ShowAvailable` | `true` | Show `available/needed` |

## Compatibility

Built and tested with Valheim **l-1.0.20** (network version 41) and BepInExPack 5.4.2351. Not tested together with other mods that change crafting.

## Build

Needs the .NET SDK (6 or newer).

1. Create a `lib/` folder and copy into it from your Valheim install (`valheim_Data/Managed/`): `assembly_valheim.dll`, `assembly_utils.dll`, `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`, `Unity.TextMeshPro.dll`, and from `BepInEx/core/`: `BepInEx.dll`, `0Harmony.dll`.
2. `dotnet build -c Release -o out`
3. Use `out/ChestCrafting.dll`.

The game's assemblies are not included in this repository.

## License

MIT
