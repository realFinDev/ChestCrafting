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

You only install this on **your own PC**. Your friends and the server don't need it, and it works on any server, including unmodded ones. If you play on someone else's server, ask the owner first whether they're okay with crafting mods.

### The easy way: r2modman (recommended)

r2modman is a free mod manager. It keeps mods separate from your normal game, so nothing can break your vanilla Valheim.

1. **Install r2modman.** Download it from [thunderstore.io/package/ebkr/r2modman](https://thunderstore.io/package/ebkr/r2modman/) and run the installer.
2. **Set it up for Valheim.** Open r2modman, pick **Valheim**, then **Select profile → Default → Select profile**.
3. **Install BepInEx** (the mod loader). Go to **Online** (or **Get mods**), search for `BepInExPack_Valheim`, click it and press **Download**.
4. **Download ChestCrafting.** Open this repository's [Releases page](../../releases), and under the newest version's **Assets**, click `ChestCrafting.dll` to download it.
5. **Put the file in the plugins folder.** In r2modman, go to **Settings → Browse profile folder**. A folder opens: go into `BepInEx`, then `plugins`, and move `ChestCrafting.dll` there.
6. **Start the game with mods.** Click **Start modded** at the top left. Always start Valheim this way when you want mods; the normal Steam button starts it without mods.

### Without a mod manager

1. **Find your Valheim folder.** In Steam, right-click **Valheim → Manage → Browse local files**.
2. **Install BepInEx.** Download `BepInExPack_Valheim` from [Thunderstore](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) (**Manual Download**), unzip it, open the `BepInExPack_Valheim` folder inside, and copy **everything in it** into your Valheim folder (next to `valheim.exe`).
3. **Start Valheim once** from Steam, go to the main menu, then quit. This creates the `BepInEx/plugins` folder.
4. **Put `ChestCrafting.dll`** (from the [Releases page](../../releases)) into `Valheim/BepInEx/plugins/`.
5. Start Valheim from Steam as usual.

On Linux or Steam Deck, use r2modman instead. Manual BepInEx needs extra launch options there.

### Check that it works

Stand at a workbench with a chest nearby and open the crafting menu. The ingredients now show two numbers, like **`12/5`**: what you have in your bag and nearby chests, and what the recipe needs. Click **Craft** and the items come out of the chest.

If you only see one number, the mod isn't loaded:

- Did you start with **Start modded** (r2modman)?
- Is `ChestCrafting.dll` **directly** in `BepInEx/plugins`, not in a subfolder or still inside a zip?
- Is another "craft from containers" mod active? Disable it. Two of these at once cause problems.
- Still stuck? Open an [issue](../../issues) and attach `BepInEx/LogOutput.log`.

### Uninstall

Delete `ChestCrafting.dll` from `BepInEx/plugins`. Your world and characters aren't changed by removing it.

## Config

The settings file appears after you've started the game once with the mod. In r2modman: **Config editor → realfin.ChestCrafting → Edit config**. Without r2modman, open `BepInEx/config/realfin.ChestCrafting.cfg` in Notepad. Restart the game after changing it.

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
