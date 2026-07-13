using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;

namespace TileMarker.Core
{
    /// <summary>Bare-bones list menu shown when more than one mod has registered a category to mark.</summary>
    internal class CategoryPickerMenu : IClickableMenu
    {
        private const int VisibleRowCount = 4;
        private const int RowHeight = 48;

        private readonly List<(string OwnerModId, string Category, string DisplayName)> options;
        private readonly Action<string, string> onPicked;
        private readonly string title;
        private readonly List<ClickableComponent> rows = new();
        private int scrollOffset;

        public CategoryPickerMenu(List<(string OwnerModId, string Category, string DisplayName)> options, Action<string, string> onPicked, string title)
            : base(Game1.uiViewport.Width / 2 - 200, Game1.uiViewport.Height / 2 - 150, 400, 300, showUpperRightCloseButton: true)
        {
            this.options = options;
            this.onPicked = onPicked;
            this.title = title;

            RebuildRows();
        }

        private void RebuildRows()
        {
            rows.Clear();
            int y = yPositionOnScreen + 60;
            int end = Math.Min(options.Count, scrollOffset + VisibleRowCount);

            for (int optionIndex = scrollOffset; optionIndex < end; optionIndex++)
            {
                rows.Add(new ClickableComponent(
                    new Rectangle(xPositionOnScreen + 32, y, width - 64, RowHeight - 4),
                    optionIndex.ToString()
                ));
                y += RowHeight;
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            foreach (ClickableComponent row in rows)
            {
                if (row.bounds.Contains(x, y)
                    && int.TryParse(row.name, out int optionIndex)
                    && optionIndex >= 0
                    && optionIndex < options.Count)
                {
                    var picked = options[optionIndex];
                    Game1.playSound("smallSelect");
                    Game1.activeClickableMenu = null;
                    onPicked?.Invoke(picked.OwnerModId, picked.Category);
                    return;
                }
            }
        }

        public override void receiveScrollWheelAction(int direction)
        {
            int oldOffset = scrollOffset;
            int maxOffset = Math.Max(0, options.Count - VisibleRowCount);
            scrollOffset = direction > 0
                ? Math.Max(0, scrollOffset - 1)
                : Math.Min(maxOffset, scrollOffset + 1);

            if (scrollOffset != oldOffset)
            {
                RebuildRows();
                Game1.playSound("shiny4");
            }
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.4f);
            Game1.drawDialogueBox(xPositionOnScreen, yPositionOnScreen, width, height, false, true);

            SpriteText.drawString(b, title, xPositionOnScreen + 32, yPositionOnScreen + 24);

            foreach (ClickableComponent row in rows)
            {
                if (!int.TryParse(row.name, out int optionIndex)
                    || optionIndex < 0
                    || optionIndex >= options.Count)
                    continue;

                Rectangle r = row.bounds;
                bool hovered = r.Contains(Game1.getMouseX(), Game1.getMouseY());
                if (hovered)
                    b.Draw(Game1.fadeToBlackRect, r, Color.White * 0.15f);

                b.DrawString(Game1.smallFont, options[optionIndex].DisplayName, new Vector2(r.X + 4, r.Y + 2), Color.White);
                b.DrawString(Game1.smallFont, options[optionIndex].OwnerModId, new Vector2(r.X + 4, r.Y + 23), Color.Gray);
            }

            if (options.Count > VisibleRowCount)
            {
                int first = scrollOffset + 1;
                int last = Math.Min(options.Count, scrollOffset + VisibleRowCount);
                string pageText = $"{first}-{last} / {options.Count}";
                Vector2 size = Game1.smallFont.MeasureString(pageText);
                b.DrawString(
                    Game1.smallFont,
                    pageText,
                    new Vector2(xPositionOnScreen + width - 32 - size.X, yPositionOnScreen + height - 36),
                    Color.Gray
                );
            }

            drawMouse(b);
        }
    }
}
