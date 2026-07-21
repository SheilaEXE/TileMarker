# Tile Marker

Tile Marker lets players select map tiles visually inside Stardew Valley.

Your mod decides what those tiles mean. They can be no-spawn areas, special interaction zones,
tiles that don't block vision, or anything else your mod needs. Tile Marker only provides the
editor and stores the selections.

The basic integration is:

> Register a category → open the editor → check the selected tiles.

## Quick start for mod authors

### 1. Copy the API contract into your project

Copy [`Api/ITileMarkerApi.cs`](Api/ITileMarkerApi.cs) into your mod's source code. You may change
its namespace to match your project.

You don't need to reference `TileMarker.dll`. The copied file only tells SMAPI which methods your
mod wants to use.

### 2. Add Tile Marker to your `manifest.json`

If Tile Marker is an optional convenience for your players, add this entry to your existing
`Dependencies` list:

```json
"Dependencies": [
  {
    "UniqueID": "NatrollEXE.TileMarker",
    "IsRequired": false
  }
]
```

Keep `IsRequired` as `false` if your mod can still work without the visual editor. Change it to
`true` only if your mod cannot work without Tile Marker.

### 3. Get the API and register a category

Register your category once in `GameLaunched`. A category is one independent group of marked
tiles owned by your mod.

```csharp
using StardewModdingAPI;
using StardewModdingAPI.Events;
using TileMarker.Api;

namespace ExampleMod
{
    public class ModEntry : Mod
    {
        private const string TileCategory = "NoSpawnArea";
        private ITileMarkerApi tileMarkerApi;

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            tileMarkerApi = Helper.ModRegistry.GetApi<ITileMarkerApi>("NatrollEXE.TileMarker");

            if (tileMarkerApi == null)
            {
                Monitor.Log("Tile Marker isn't installed; the visual tile editor will be unavailable.", LogLevel.Trace);
                return;
            }

            tileMarkerApi.RegisterCategory(
                ModManifest.UniqueID,
                TileCategory,
                "Areas where spawning is disabled"
            );
        }
    }
}
```

The three registration values are:

- `ModManifest.UniqueID`: identifies your mod as the owner.
- `TileCategory`: a stable internal ID chosen by you. Don't translate or rename it after release.
- The final text: the player-facing category name shown by Tile Marker. This can be translated.

You may register more than one category if your mod needs separate tile groups.

### Optional: let compatible categories merge

Two mods can keep independent selections while allowing players to use their combined tiles.
Register both categories with the same stable shared-group ID:

```csharp
tileMarkerApi.RegisterCategoryWithSharedGroup(
    ModManifest.UniqueID,
    TileCategory,
    "Transparent vision obstacles",
    "ExampleAuthor.SharedVisionIgnored"
);
```

When **Merge compatible categories** is enabled in Tile Marker's GMCM, API lookups for one
category include every registered category in the same shared group. The saved selections are
never copied or overwritten. Disabling the option immediately restores each category's individual
results. Categories registered normally with `RegisterCategory` are never merged.

### 4. Open the editor

This can be called from your own keybind, GMCM button, command, or menu:

```csharp
tileMarkerApi?.OpenEditor(ModManifest.UniqueID, TileCategory);
```

The category must be registered before you open it. The editor always opens on the player's
current location.

Players can also use Tile Marker's own keybind and choose a registered category from its picker.

### 5. Check whether a tile was marked

For most mods, this is the easiest way to use the result:

```csharp
bool isMarked = tileMarkerApi?.IsTileMarked(
    ModManifest.UniqueID,
    TileCategory,
    location,
    tileX,
    tileY
) == true;
```

`IsTileMarked()` returns `false` when the tile isn't marked. It also returns `false` safely when
the location is unavailable.

That's the complete basic integration. The sections below are only needed for more advanced uses.

## Getting all marked ranges

If you need the complete selection for a location, ask for its saved ranges:

```csharp
IReadOnlyList<string> ranges = tileMarkerApi?.GetMarkedTileRanges(
    ModManifest.UniqueID,
    TileCategory,
    location.NameOrUniqueName
) ?? Array.Empty<string>();
```

The result uses compact coordinate strings:

- `"5,8"` means one tile.
- `"5-9,8"` means a horizontal range.

Selections are returned row by row. For example, a rectangle covering three rows is returned as
three horizontal ranges. This keeps the saved coordinates easy to read and process.

The list is empty when that location has no marked tiles. If you only need to test individual
tiles, prefer `IsTileMarked()` so you don't need to parse these strings yourself.

## Reacting immediately when selections change

Subscribe to `TileMarksChanged` if your mod caches the selected tiles or needs to refresh something
as soon as the player closes the editor:

```csharp
// Add this immediately after RegisterCategory() in the GameLaunched example above:
tileMarkerApi.TileMarksChanged += OnTileMarksChanged;

private void OnTileMarksChanged(object sender, TileMarksChangedEventArgs e)
{
    if (e.OwnerModId != ModManifest.UniqueID || e.Category != TileCategory)
        return;

    Monitor.Log($"Tile selection changed in {e.LocationName}.", LogLevel.Trace);
    // Refresh your cached data here, if your mod uses a cache.
}
```

Always filter the event by owner and category, since other mods may also use Tile Marker.

## API reference

| Member | What it does |
| --- | --- |
| `RegisterCategory(ownerModId, category, displayName)` | Registers one tile group and makes it available in the category picker. |
| `RegisterCategoryWithSharedGroup(ownerModId, category, displayName, sharedGroup)` | Registers an independent category that can be read together with compatible categories when merging is enabled. |
| `OpenEditor(ownerModId, category)` | Opens a registered category on the player's current location. |
| `IsTileMarked(ownerModId, category, location, x, y)` | Checks one tile without requiring any range parsing. |
| `GetMarkedTileRanges(ownerModId, category, locationName)` | Returns every marked coordinate range for one location. |
| `TileMarksChanged` | Raised after a changed selection is saved. |

## Optional integration and fallback behavior

If Tile Marker is optional, always null-check `tileMarkerApi`. Your mod should keep its normal
behavior when the API isn't available.

For example, a mod may continue accepting manually written coordinates in its own `config.json`,
while Tile Marker gives players an easier visual way to create or update the same data.

## Player controls

These controls apply only while the tile editor is open:

- **Left-click**: mark or unmark one tile.
- **Right-click and drag from an empty tile**: paint continuously.
- **Right-click and drag from a marked tile**: erase continuously.
- **Shift + right-click and drag**: mark or unmark a rectangle.
- **Esc during a drag**: cancel that drag.
- **Esc while not dragging**: close and save.
- Pressing the configured editor key again also closes and saves.

### Touchscreens and Cinderbox

Enable **Paint by dragging with left click** in Generic Mod Config Menu, or set this in
`config.json`:

```json
"EnableLeftClickBrush": true
```

While this option is enabled, tapping still changes one tile. Holding and dragging with a finger
paints or erases continuously. The option is disabled by default, so desktop mouse controls don't
change unless the player enables it.

## How selections are saved

- Selections are stored separately for each save file, location, mod, and category.
- Closing the editor saves changes immediately, so they survive quitting before the next in-game day.
- Stardew's save data also receives a backup copy during a normal game save.
- Only the host can edit selections in multiplayer.
- Reopening the editor shows the selections already stored for the current map.

## Building Tile Marker

Requirements:

- [.NET SDK](https://dotnet.microsoft.com/download), targeting `net6.0`.
- Stardew Valley installed locally.

The project uses
[`Pathoschild.Stardew.ModBuildConfig`](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig)
to detect the game path and package the mod automatically.

```sh
dotnet build
```

After a successful build, the mod is copied to the game's `Mods` folder and packaged under
`bin/Debug/net6.0/`. If the game path can't be detected, set `<GamePath>` in `TileMarker.csproj`.
