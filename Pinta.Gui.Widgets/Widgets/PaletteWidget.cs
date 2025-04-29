using Pinta.Core;

namespace Pinta.Gui.Widgets;

internal static class PaletteWidget
{
	internal const int PALETTE_ROWS = 2;
	internal const int SWATCH_SIZE = 19;
	internal const int WIDGET_HEIGHT = 42;
	internal const int PALETTE_MARGIN = 10;

	public static int GetSwatchAtLocation (
		PaletteManager palette,
		PointD point,
		RectangleD palette_bounds,
		bool recentColorPalette = false)
	{
		int max =
			recentColorPalette
			? palette.RecentlyUsedColors.Count
			: palette.CurrentPalette.Count;

		// This could be more efficient, but is good enough for now
		for (int i = 0; i < max; i++)
			if (GetSwatchBounds (palette, i, palette_bounds, recentColorPalette).ContainsPoint (point))
				return i;

		return -1;
	}

	public static RectangleD GetSwatchBounds (
		PaletteManager palette,
		int index,
		RectangleD palette_bounds,
		bool recentColorPalette = false)
	{
		// Normal swatches are laid out like this:
		// 0 | 2 | 4 | 6
		// 1 | 3 | 5 | 7
		// Recent swatches are laid out like this (it's less visually jarring as they change):
		// 0 | 1 | 2 | 3
		// 4 | 5 | 6 | 7

		// First we need to figure out what row and column the color is
		int recent_cols = palette.MaxRecentlyUsedColor / PALETTE_ROWS;
		int row = recentColorPalette ? index / recent_cols : index % PALETTE_ROWS;
		int col = recentColorPalette ? index % recent_cols : index / PALETTE_ROWS;

		// Now we need to construct the bounds of that row/column
		double x = palette_bounds.X + (col * SWATCH_SIZE);
		double y = palette_bounds.Y + (row * SWATCH_SIZE);

		return new (x, y, SWATCH_SIZE, SWATCH_SIZE);
	}
}
