# Tile Marker

A small standalone tool: press a key, get a grid overlay over the current location, click a tile
(or right-click-drag an area) to mark it, then repeat to unmark. Built so other mods can let
players pick tiles visually instead of hand-editing coordinates in a config file.

Tile Marker doesn't know or care what the tiles are *for* — that's entirely up to the mod that
registers a category and reads the result back.

## Integrating your mod

1. Get the API in `GameLaunched`:

```csharp
private ITileMarkerApi tileMarkerApi;

private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
{
    tileMarkerApi = Helper.ModRegistry.GetApi<ITileMarkerApi>("NatrollEXE.TileMarker");
    tileMarkerApi?.RegisterCategory(ModManifest.UniqueID, "VisionIgnored", "Vision-ignored tiles");
}
```

2. (Optional) Give your own mod a keybind/GMCM button that jumps straight into the editor for your
   category, instead of making the player go through Tile Marker's picker:

```csharp
tileMarkerApi?.OpenEditor(ModManifest.UniqueID, "VisionIgnored");
```

3. Read back what the player marked, whenever you need it (e.g. once at `SaveLoaded`, or live via
   the `TileMarksChanged` event):

```csharp
IReadOnlyList<string> ranges = tileMarkerApi.GetMarkedTileRanges(
    ModManifest.UniqueID, "VisionIgnored", location.NameOrUniqueName);
```

The ranges come back in the exact same `"x,y"` / `"x1-x2,y1-y2"` string format Lots of Kisses
already uses for `VisionIgnoredTiles` — reuse the same parser, no format conversion needed. Or skip
parsing entirely with the direct check:

```csharp
bool ignored = tileMarkerApi.IsTileMarked(ModManifest.UniqueID, "VisionIgnored", location, x, y);
```

Tile Marker is an optional soft dependency — always null-check the API, since players may not have
it installed. Everything still works by hand-editing config.json as before; this just gives players
who *do* install it an easier way to produce the same data.

## Player controls (while the editor is open)

- **Left-click** a tile: mark it if it was empty, unmark it if it was already marked.
- **Right-click-drag** starting on an empty tile: paints continuously along the cursor path.
- **Right-click-drag** starting on a marked tile: erases continuously along the cursor path.
- **Shift + right-click-drag**: adds or removes the whole rectangle between the starting and ending tiles.
- **Esc**: cancels the current drag preview if one is in progress; otherwise closes and saves the editor.
- Pressing the configured key again while the editor is open also closes and saves it.

## Building

Requirements:

- [.NET SDK](https://dotnet.microsoft.com/download) (targets `net6.0`)
- Stardew Valley installed locally

The project uses [Pathoschild.Stardew.ModBuildConfig](https://www.nuget.org/packages/Pathoschild.Stardew.ModBuildConfig), which auto-detects your Stardew Valley install path on Windows, Linux, and macOS. If it can't find it automatically, set `<GamePath>` in `TileMarker.csproj`.

```sh
dotnet build
```

On a successful build, the mod is automatically copied into your `Mods` folder (via the build config package) as well as into `bin/Debug/net6.0/`.

## Notes

- Marked tiles are saved per save file (they depend on that save's installed map mods), not globally.
- Closing the editor writes an immediate per-save copy, so selections survive exiting the game before
  the next overnight save. An embedded save-data copy is also updated as a transferable backup when
  Stardew saves the day.
- In multiplayer, tile editing and saving are currently host-only. Farmhands can keep playing normally,
  but the host must create or change the shared selections.
- Re-opening the editor on a location shows whatever was already marked there, so a map-changing
  mod update doesn't leave stale marks invisible — you can see and adjust them against the new layout.
