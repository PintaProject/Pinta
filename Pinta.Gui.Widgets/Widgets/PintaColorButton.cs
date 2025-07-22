using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

internal sealed class PintaColorButton : Gtk.Button
{
	private Color display_color = Color.Black;
	public Color DisplayColor {
		get => display_color;
		set {
			if (display_color == value) return;
			display_color = value;
			color_drawing_area.QueueDraw ();
		}
	}

	private readonly Gtk.DrawingArea color_drawing_area;
	internal PintaColorButton ()
	{
		Gtk.DrawingArea colorDrawingArea = new () {
			Hexpand = true,
			Vexpand = true,
		};
		colorDrawingArea.SetDrawFunc (OnDraw);
		SetChild (colorDrawingArea);
		color_drawing_area = colorDrawingArea;
	}

	private void OnDraw (
		Gtk.DrawingArea drawingArea,
		Context cr,
		int width,
		int height)
	{
		const int TILE_SIZE = 16;

		using Pattern checkeredPattern =
			CairoExtensions.CreateTransparentBackgroundPattern (TILE_SIZE);

		cr.SetSource (checkeredPattern);
		cr.Paint ();

		cr.SetSourceColor (display_color);
		cr.Rectangle (0, 0, width, height);
		cr.Fill ();
	}
}
