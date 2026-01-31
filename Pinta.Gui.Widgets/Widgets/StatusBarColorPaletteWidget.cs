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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Adw;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class StatusBarColorPaletteWidget : Gtk.DrawingArea
{
	private static bool color_picker_active = false;

	private readonly RectangleD primary_rect = new (4, 3, 24, 24);
	private readonly RectangleD secondary_rect = new (17, 16, 24, 24);
	private readonly RectangleD swap_rect = new (27, 2, 15, 15);
	private readonly RectangleD reset_rect = new (2, 27, 15, 15);

	private readonly IChromeService chrome;
	private readonly IPaletteService palette;
	private readonly ISystemService system;

	private RectangleD palette_rect;
	private RectangleD recent_palette_rect;

	public StatusBarColorPaletteWidget (IChromeService chrome, IPaletteService palette, ISystemService system)
	{
		this.chrome = chrome;
		this.palette = palette;
		this.system = system;

		HasTooltip = true;
		OnQueryTooltip += HandleQueryTooltip;

		palette.PrimaryColorChanged += new EventHandler (Palette_ColorChanged);
		palette.SecondaryColorChanged += new EventHandler (Palette_ColorChanged);
		palette.RecentColorsChanged += new EventHandler (Palette_ColorChanged);
		palette.CurrentPalette.PaletteChanged += new EventHandler (Palette_ColorChanged);

		HeightRequest = PaletteWidget.WIDGET_HEIGHT;

		OnResize += (_, e) => HandleSizeAllocated (e);
		SetDrawFunc ((area, context, width, height) => Draw (context));

		// Handle mouse clicks.
		Gtk.GestureClick click_gesture = Gtk.GestureClick.New ();
		click_gesture.SetButton (0); // Listen for all mouse buttons.
		click_gesture.OnReleased += (_, e) => {
			HandleClick (new PointD (e.X, e.Y), click_gesture.GetCurrentButton (), click_gesture.GetCurrentEventState ());
			click_gesture.SetState (Gtk.EventSequenceState.Claimed);
		};
		AddController (click_gesture);
	}

	private async void HandleClick (PointD point, uint button, Gdk.ModifierType state)
	{
		var element = GetElementAtPoint (point);

		switch (element) {

			case WidgetElement.PrimaryColor:
			case WidgetElement.SecondaryColor:

				if (color_picker_active)
					break;

				color_picker_active = true;

				try {
					bool primarySelected = element switch {
						WidgetElement.PrimaryColor => true,
						WidgetElement.SecondaryColor => false,
						_ => throw new UnreachableException ()
					};

					PaletteColors? choices = await RunColorPicker (primarySelected);

					if (choices is null)
						break;

					if (palette.PrimaryColor != choices.Primary)
						palette.PrimaryColor = choices.Primary;

					if (palette.SecondaryColor != choices.Secondary)
						palette.SecondaryColor = choices.Secondary;
				} finally {
					color_picker_active = false;
				}

				break;

			case WidgetElement.SwapColors:

				Color temp = palette.PrimaryColor;

				// Swapping should not trigger adding colors to recently used palette
				palette.SetColor (true, palette.SecondaryColor, false);
				palette.SetColor (false, temp, false);

				break;

			case WidgetElement.ResetColors:

				palette.PrimaryColor = new Color (0, 0, 0);
				palette.SecondaryColor = new Color (1, 1, 1);

				break;

			case WidgetElement.Palette:

				int index = PaletteWidget.GetSwatchAtLocation (palette, point, palette_rect);

				if (index < 0)
					break;

				bool isCtrlPressed = state.IsControlPressed ();
				if (button == GtkExtensions.MOUSE_RIGHT_BUTTON) {
					palette.SecondaryColor = palette.CurrentPalette.Colors[index];
				} else if (button == GtkExtensions.MOUSE_LEFT_BUTTON && !isCtrlPressed) {
					palette.PrimaryColor = palette.CurrentPalette.Colors[index];
				} else if (button == GtkExtensions.MOUSE_MIDDLE_BUTTON ||
					   (button == GtkExtensions.MOUSE_LEFT_BUTTON && isCtrlPressed)) {
					SingleColor pick = new (palette.CurrentPalette.Colors[index]);
					var colors = await GetUserChosenColor (
						pick,
						Translations.GetString ("Choose Palette Color"));

					if (colors != null)
						palette.CurrentPalette.SetColor (index, colors.Color);
				}

				break;

			case WidgetElement.RecentColorsPalette:

				int recent_index = PaletteWidget.GetSwatchAtLocation (palette, point, recent_palette_rect, true);

				if (recent_index < 0)
					break;

				Color recentColor = palette.RecentlyUsedColors.ElementAt (recent_index);

				if (button == GtkExtensions.MOUSE_RIGHT_BUTTON) {
					palette.SetColor (false, recentColor, false);
				} else if (button == GtkExtensions.MOUSE_LEFT_BUTTON) {
					palette.SetColor (true, recentColor, false);
				}

				break;
		}
	}

	private void Draw (Context g)
	{
		const int TILE_SIZE = 16;
		using Pattern checkeredPattern =
			CairoExtensions.CreateTransparentBackgroundPattern (TILE_SIZE);

		// Draw Secondary color swatch

		if (palette.SecondaryColor.A < 1)
			g.FillRectangle (secondary_rect, checkeredPattern);

		g.FillRectangle (secondary_rect, palette.SecondaryColor);
		g.DrawRectangle (new RectangleD (secondary_rect.X + 1, secondary_rect.Y + 1, secondary_rect.Width - 2, secondary_rect.Height - 2), new Color (1, 1, 1), 1);
		g.DrawRectangle (secondary_rect, new Color (0, 0, 0), 1);

		// Draw Primary color swatch

		if (palette.PrimaryColor.A < 1)
			g.FillRectangle (primary_rect, checkeredPattern);

		g.FillRectangle (primary_rect, palette.PrimaryColor);
		g.DrawRectangle (new RectangleD (primary_rect.X + 1, primary_rect.Y + 1, primary_rect.Width - 2, primary_rect.Height - 2), new Color (1, 1, 1), 1);
		g.DrawRectangle (primary_rect, new Color (0, 0, 0), 1);

		// Draw the swap icon.
		GetStyleContext ().GetColor (out Gdk.RGBA fg_color);
		Cairo.Color cairo_fg_color = fg_color.ToCairoColor ();
		DrawSwapIcon (g, cairo_fg_color);

		// Draw the reset icon.
		double square_size = 0.6 * reset_rect.Width;
		g.DrawRectangle (new RectangleD (reset_rect.Location (), square_size, square_size), cairo_fg_color, 1);
		g.FillRectangle (new RectangleD (reset_rect.Right - square_size, reset_rect.Bottom - square_size, square_size, square_size), cairo_fg_color);

		// Draw recently used color swatches
		var recent = palette.RecentlyUsedColors;

		for (int i = 0; i < recent.Count; i++) {

			RectangleD swatchBounds = PaletteWidget.GetSwatchBounds (palette, i, recent_palette_rect, true);
			Color recentColor = recent.ElementAt (i);

			if (recentColor.A < 1) // Only draw checkered pattern if there is transparency
				g.FillRectangle (swatchBounds, checkeredPattern);

			g.FillRectangle (swatchBounds, recentColor);
		}

		// Draw color swatches
		var currentPalette = palette.CurrentPalette;

		for (int i = 0; i < currentPalette.Colors.Count; i++) {

			RectangleD swatchBounds = PaletteWidget.GetSwatchBounds (palette, i, palette_rect);
			Color paletteColor = currentPalette.Colors[i];

			if (paletteColor.A < 1) // Only draw checkered pattern if there is transparency
				g.FillRectangle (swatchBounds, checkeredPattern);

			g.FillRectangle (swatchBounds, paletteColor);
		}

		g.Dispose ();
	}

	private void DrawSwapIcon (Context g, Color color)
	{
		const double ARROW_SIZE = 4;

		g.Save ();
		g.LineWidth = 1.5;
		g.SetSourceColor (color);

		const double RADIUS = 11;
		const double OFFSET = 1;

		PointD p1 = new (
			X: swap_rect.Left + RADIUS,
			Y: swap_rect.Bottom - OFFSET);

		g.MoveTo (p1.X, p1.Y);

		g.CurveTo (
			p1.X,
			p1.Y - RADIUS - 2,
			p1.X,
			p1.Y - RADIUS + OFFSET,
			swap_rect.Left + OFFSET,
			swap_rect.Bottom - RADIUS);

		g.MoveTo (p1.X - ARROW_SIZE, p1.Y - ARROW_SIZE);

		g.LineTo (p1.X, p1.Y);
		g.LineTo (p1.X + ARROW_SIZE, p1.Y - ARROW_SIZE);

		PointD p2 = new (
			X: swap_rect.Left + OFFSET,
			Y: swap_rect.Bottom - RADIUS);

		g.MoveTo (p2.X + ARROW_SIZE, p2.Y - ARROW_SIZE);

		g.LineTo (p2.X, p2.Y);
		g.LineTo (p2.X + ARROW_SIZE, p2.Y + ARROW_SIZE);

		g.Stroke ();

		g.Restore ();
	}

	private void HandleSizeAllocated (Gtk.DrawingArea.ResizeSignalArgs e)
	{
		int width = e.Width;

		// Store the bounds allocated for our palette
		int recent_cols = palette.MaxRecentlyUsedColor / PaletteWidget.PALETTE_ROWS;

		recent_palette_rect = new RectangleD (50, 2, PaletteWidget.SWATCH_SIZE * recent_cols, PaletteWidget.SWATCH_SIZE * PaletteWidget.PALETTE_ROWS);
		palette_rect = new RectangleD (
			recent_palette_rect.Right + PaletteWidget.PALETTE_MARGIN,
			2,
			width - recent_palette_rect.Right - PaletteWidget.PALETTE_MARGIN,
			PaletteWidget.SWATCH_SIZE * PaletteWidget.PALETTE_ROWS);
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
				if (PaletteWidget.GetSwatchAtLocation (palette, point, palette_rect) >= 0) {
					// Translators: {0} is 'Ctrl', or a platform-specific key such as 'Command' on macOS.
					text = Translations.GetString ("Left click to set primary color. Right click to set secondary color. Middle click or press {0} and left click to choose palette color.",
						system.CtrlLabel ());
				}

				break;
			case WidgetElement.RecentColorsPalette:
				if (PaletteWidget.GetSwatchAtLocation (palette, point, recent_palette_rect, true) >= 0)
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

	private async Task<PaletteColors?> RunColorPicker (bool primarySelected)
	{
		using ColorPickerDialog colorPicker = new (
			chrome.MainWindow,
			palette,
			new PaletteColors (palette.PrimaryColor, palette.SecondaryColor),
			primarySelected,
			true,
			Translations.GetString ("Color Picker"));

		Gtk.ResponseType response = await colorPicker.RunAsync ();

		if (response != Gtk.ResponseType.Ok)
			return null;

		return (PaletteColors) colorPicker.Colors;
	}


	private async Task<SingleColor?> GetUserChosenColor (
		SingleColor colors,
		string title)
	{
		using ColorPickerDialog dialog = new (
			chrome.MainWindow,
			palette,
			colors,
			primarySelected: true,
			false,
			title);

		try {
			Gtk.ResponseType response = await dialog.RunAsync ();

			if (response != Gtk.ResponseType.Ok)
				return null;

			return (SingleColor) dialog.Colors;

		} finally {
			dialog.Destroy ();
		}
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
