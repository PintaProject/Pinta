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

		// Draw transparency background

		const int SQUARE_SIZE = 8;
		const int TILE_SIZE = SQUARE_SIZE * 2;

		ImageSurface tileSurface = new (Format.Rgb24, TILE_SIZE, TILE_SIZE);

		using (Context sc = new (tileSurface)) {

			Color colorLight = Color.White;
			Color colorDark = new (0.8, 0.8, 0.8);

			// Top-left and bottom-right squares
			sc.SetSourceColor (colorLight);
			sc.Rectangle (0, 0, SQUARE_SIZE, SQUARE_SIZE);
			sc.Rectangle (SQUARE_SIZE, SQUARE_SIZE, SQUARE_SIZE, SQUARE_SIZE);
			sc.Fill ();

			// Top-right and bottom-left squares
			sc.SetSourceColor (colorDark);
			sc.Rectangle (SQUARE_SIZE, 0, SQUARE_SIZE, SQUARE_SIZE);
			sc.Rectangle (0, SQUARE_SIZE, SQUARE_SIZE, SQUARE_SIZE);
			sc.Fill ();
		}

		SurfacePattern checkeredPattern = new (tileSurface) {
			Extend = Extend.Repeat
		};

		cr.SetSource (checkeredPattern);
		cr.Paint ();

		cr.SetSourceRgba (
			display_color.R,
			display_color.G,
			display_color.B,
			display_color.A);
		cr.Rectangle (0, 0, width, height);
		cr.Fill ();
	}
}
