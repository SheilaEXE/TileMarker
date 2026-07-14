using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace TileMarker.Config
{
    public class ModConfig
    {
        public KeybindList OpenEditorKey { get; set; } = KeybindList.Parse("OemPipe");

        // How opaque the add/remove preview squares are (0-100).
        public int OverlayOpacityPercent { get; set; } = 45;

        // Lets touchscreens and left-button-only environments paint by dragging.
        public bool EnableLeftClickBrush { get; set; } = false;
    }
}
