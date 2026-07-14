using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using TileMarker.Core;

namespace TileMarker.Api
{
    public sealed class TileMarkerApi : ITileMarkerApi
    {
        private readonly MarkedTileStore store;
        private readonly Action<string, string> openEditor;
        private readonly IMonitor monitor;

        public event EventHandler<TileMarksChangedEventArgs> TileMarksChanged
        {
            add => store.TileMarksChanged += value;
            remove => store.TileMarksChanged -= value;
        }

        internal TileMarkerApi(MarkedTileStore store, Action<string, string> openEditor, IMonitor monitor)
        {
            this.store = store;
            this.openEditor = openEditor;
            this.monitor = monitor;
        }

        public void RegisterCategory(string ownerModId, string category, string displayName)
        {
            if (!store.Register(ownerModId, category, displayName))
            {
                monitor.Log(
                    "[Tile Marker API] Ignored a category registration with an empty owner mod ID or category ID.",
                    LogLevel.Warn
                );
            }
        }

        public void OpenEditor(string ownerModId, string category)
        {
            if (!store.IsRegistered(ownerModId, category))
            {
                monitor.Log(
                    $"[Tile Marker API] Refused to open an unregistered category: {ownerModId ?? "<null>"}/{category ?? "<null>"}.",
                    LogLevel.Warn
                );
                return;
            }

            openEditor?.Invoke(ownerModId, category);
        }

        public IReadOnlyList<string> GetMarkedTileRanges(string ownerModId, string category, string locationName)
            => store.GetRanges(ownerModId, category, locationName);

        public bool IsTileMarked(string ownerModId, string category, GameLocation location, int x, int y)
        {
            if (location == null)
                return false;

            return store.IsTileMarked(
                ownerModId,
                category,
                location.NameOrUniqueName,
                new Point(x, y)
            );
        }
    }
}
