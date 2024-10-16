// 
// StatusBarColorPaletteWidget.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2020 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class StatusBarColorPaletteWidget : Gtk.DrawingArea
{
	private readonly PaletteManager palette;
	private readonly ChromeManager chrome;

	private readonly RectangleD primary_rect = new (4, 3, 24, 24);
	private readonly RectangleD secondary_rect = new (17, 16, 24, 24);
	private readonly RectangleD swap_rect = new (27, 2, 15, 15);
	private readonly RectangleD reset_rect = new (2, 27, 15, 15);

	private RectangleD palette_rect;
	private RectangleD recent_palette_rect;

	const int PALETTE_ROWS = 2;
	const int SWATCH_SIZE = 19;
	const int WIDGET_HEIGHT = 42;
	const int PALETTE_MARGIN = 10;

	public StatusBarColorPaletteWidget (
		ChromeManager chrome,
		PaletteManager palette)
	{
		this.chrome = chrome;
		this.palette = palette;

		HasTooltip = true;
		OnQueryTooltip += HandleQueryTooltip;

		palette.PrimaryColorChanged += new EventHandler (Palette_ColorChanged);
		palette.SecondaryColorChanged += new EventHandler (Palette_ColorChanged);
		palette.RecentColorsChanged += new EventHandler (Palette_ColorChanged);
		palette.CurrentPalette.PaletteChanged += new EventHandler (Palette_ColorChanged);

		HeightRequest = WIDGET_HEIGHT;

		OnResize += (_, e) => HandleSizeAllocated (e);
		SetDrawFunc ((area, context, width, height) => Draw (context));

		// Handle mouse clicks.
		Gtk.GestureClick click_gesture = Gtk.GestureClick.New ();
		click_gesture.SetButton (0); // Listen for all mouse buttons.
		click_gesture.OnReleased += (_, e) => {
			HandleClick (new PointD (e.X, e.Y), click_gesture.GetCurrentButton ());
			click_gesture.SetState (Gtk.EventSequenceState.Claimed);
		};
		AddController (click_gesture);
	}

	private void HandleClick (PointD point, uint button)
	{
		switch (GetElementAtPoint (point)) {
			case WidgetElement.PrimaryColor:
				palette.PrimaryColor = GetUserChosenColor (palette.PrimaryColor, Translations.GetString ("Choose Primary Color"));
				break;
			case WidgetElement.SecondaryColor:
				palette.SecondaryColor = GetUserChosenColor (palette.SecondaryColor, Translations.GetString ("Choose Secondary Color"));
				break;
			case WidgetElement.SwapColors:
				var temp = palette.PrimaryColor;

				// Swapping should not trigger adding colors to recently used palette
				palette.SetColor (true, palette.SecondaryColor, false);
				palette.SetColor (false, temp, false);

				break;
			case WidgetElement.ResetColors:
				palette.PrimaryColor = new Color (0, 0, 0);
				palette.SecondaryColor = new Color (1, 1, 1);

				break;
			case WidgetElement.Palette:

				var index = GetSwatchAtLocation (point);

				if (index < 0)
					break;

				if (button == GtkExtensions.MouseRightButton)
					palette.SecondaryColor = palette.CurrentPalette[index];
				else if (button == GtkExtensions.MouseLeftButton)
					palette.PrimaryColor = palette.CurrentPalette[index];
				else
					palette.CurrentPalette[index] = GetUserChosenColor (palette.CurrentPalette[index], Translations.GetString ("Choose Palette Color"));

				break;
			case WidgetElement.RecentColorsPalette:

				int recent_index = GetSwatchAtLocation (point, true);

				if (recent_index < 0)
					break;

				if (button == GtkExtensions.MouseRightButton)
					palette.SetColor (false, palette.RecentlyUsedColors.ElementAt (recent_index), false);
				else if (button == GtkExtensions.MouseLeftButton)
					palette.SetColor (true, palette.RecentlyUsedColors.ElementAt (recent_index), false);

				break;
		}
	}

	private void Draw (Context g)
	{
		// Draw Secondary color swatch
		g.FillRectangle (secondary_rect, palette.SecondaryColor);
		g.DrawRectangle (new RectangleD (secondary_rect.X + 1, secondary_rect.Y + 1, secondary_rect.Width - 2, secondary_rect.Height - 2), new Color (1, 1, 1), 1);
		g.DrawRectangle (secondary_rect, new Color (0, 0, 0), 1);

		// Draw Primary color swatch
		g.FillRectangle (primary_rect, palette.PrimaryColor);
		g.DrawRectangle (new RectangleD (primary_rect.X + 1, primary_rect.Y + 1, primary_rect.Width - 2, primary_rect.Height - 2), new Color (1, 1, 1), 1);
		g.DrawRectangle (primary_rect, new Color (0, 0, 0), 1);

		// Draw the swap icon.
		GetStyleContext ().GetColor (out var fg_color);
		DrawSwapIcon (g, fg_color);

		// Draw the reset icon.
		double square_size = 0.6 * reset_rect.Width;
		g.DrawRectangle (new RectangleD (reset_rect.Location (), square_size, square_size), fg_color, 1);
		g.FillRectangle (new RectangleD (reset_rect.Right - square_size, reset_rect.Bottom - square_size, square_size, square_size), fg_color);

		// Draw recently used color swatches
		var recent = palette.RecentlyUsedColors;

		for (int i = 0; i < recent.Count; i++)
			g.FillRectangle (GetSwatchBounds (i, true), recent.ElementAt (i));

		// Draw color swatches
		var currentPalette = palette.CurrentPalette;

		for (int i = 0; i < currentPalette.Count; i++)
			g.FillRectangle (GetSwatchBounds (i), currentPalette[i]);
	}

	private void DrawSwapIcon (Context g, Color color)
	{
		const double arrow_size = 4;

		g.Save ();
		g.LineWidth = 1.5;
		g.SetSourceColor (color);

		const double radius = 11;
		const double offset = 1;

		double x = swap_rect.Left + radius;
		double y = swap_rect.Bottom - offset;

		g.MoveTo (x, y);
		g.CurveTo (x, y - radius - 2, x, y - radius + offset, swap_rect.Left + offset, swap_rect.Bottom - radius);

		g.MoveTo (x - arrow_size, y - arrow_size);
		g.LineTo (x, y);
		g.LineTo (x + arrow_size, y - arrow_size);

		x = swap_rect.Left + offset;
		y = swap_rect.Bottom - radius;
		g.MoveTo (x + arrow_size, y - arrow_size);
		g.LineTo (x, y);
		g.LineTo (x + arrow_size, y + arrow_size);

		g.Stroke ();
		g.Restore ();
	}

	private void HandleSizeAllocated (Gtk.DrawingArea.ResizeSignalArgs e)
	{
		int width = e.Width;

		// Store the bounds allocated for our palette
		int recent_cols = palette.MaxRecentlyUsedColor / PALETTE_ROWS;

		recent_palette_rect = new RectangleD (
			50,
			2,
			SWATCH_SIZE * recent_cols,
			SWATCH_SIZE * PALETTE_ROWS);

		palette_rect = new RectangleD (
			recent_palette_rect.Right + PALETTE_MARGIN,
			2,
			width - recent_palette_rect.Right - PALETTE_MARGIN,
			SWATCH_SIZE * PALETTE_ROWS);
	}

	private RectangleD GetSwatchBounds (
		int index,
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
		RectangleD palette_bounds = recentColorPalette ? recent_palette_rect : palette_rect;
		double x = palette_bounds.X + (col * SWATCH_SIZE);
		double y = palette_bounds.Y + (row * SWATCH_SIZE);

		return new (x, y, SWATCH_SIZE, SWATCH_SIZE);
	}

	private int GetSwatchAtLocation (PointD point, bool recentColorPalette = false)
	{
		int max =
			recentColorPalette
			? palette.RecentlyUsedColors.Count
			: palette.CurrentPalette.Count;

		// This could be more efficient, but is good enough for now
		for (int i = 0; i < max; i++)
			if (GetSwatchBounds (i, recentColorPalette).ContainsPoint (point))
				return i;

		return -1;
	}

	/// <summary>
	/// Provide a custom tooltip based on the cursor location.
	/// </summary>
	private bool HandleQueryTooltip (object o, Gtk.Widget.QueryTooltipSignalArgs args)
	{
		string? text = null;
		PointD point = new (args.X, args.Y);

		switch (GetElementAtPoint (point)) {
			case WidgetElement.Palette:
				if (GetSwatchAtLocation (point) >= 0)
					text = Translations.GetString ("Left click to set primary color. Right click to set secondary color. Middle click to choose palette color.");
				break;
			case WidgetElement.RecentColorsPalette:
				if (GetSwatchAtLocation (point, true) >= 0)
					text = Translations.GetString ("Left click to set primary color. Right click to set secondary color.");
				break;
			case WidgetElement.PrimaryColor:
				text = Translations.GetString ("Click to select primary color.");
				break;
			case WidgetElement.SecondaryColor:
				text = Translations.GetString ("Click to select secondary color.");
				break;
			case WidgetElement.SwapColors:
				string label = Translations.GetString ("Click to switch between primary and secondary color.");
				string shortcut_label = Translations.GetString ("Shortcut key");
				text = $"{label} {shortcut_label}: {"X"}";
				break;
			case WidgetElement.ResetColors:
				text = Translations.GetString ("Click to reset primary and secondary color.");
				break;
		}

		args.Tooltip.SetText (text);
		return text != null;
	}

	private void Palette_ColorChanged (object? sender, EventArgs e)
	{
		// Color change events may be received while the widget is minimized,
		// so we only call Invalidate() if the widget is shown.
		if (GetRealized ())
			QueueDraw ();
	}

	private Color GetUserChosenColor (Color initialColor, string title)
	{
		var ccd = Gtk.ColorChooserDialog.New (title, chrome.MainWindow);
		ccd.UseAlpha = true;
		ccd.SetColor (initialColor);

		Color result = initialColor;

		Gtk.ResponseType response = ccd.RunBlocking ();
		if (response == Gtk.ResponseType.Ok)
			ccd.GetColor (out result);

		ccd.Destroy ();

		return result;
	}

	private WidgetElement GetElementAtPoint (PointD point)
	{
		if (palette_rect.ContainsPoint (point))
			return WidgetElement.Palette;
		if (recent_palette_rect.ContainsPoint (point))
			return WidgetElement.RecentColorsPalette;
		if (primary_rect.ContainsPoint (point))
			return WidgetElement.PrimaryColor;
		if (secondary_rect.ContainsPoint (point))
			return WidgetElement.SecondaryColor;
		if (swap_rect.ContainsPoint (point))
			return WidgetElement.SwapColors;
		if (reset_rect.ContainsPoint (point))
			return WidgetElement.ResetColors;

		return WidgetElement.Nothing;
	}

	private enum WidgetElement
	{
		Nothing,
		Palette,
		RecentColorsPalette,
		PrimaryColor,
		SecondaryColor,
		SwapColors,
		ResetColors,
	}
}
