using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

[GObject.Subclass<Gtk.Button>]
internal sealed partial class PintaColorButton
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

	private readonly Gtk.DrawingArea color_drawing_area = Gtk.DrawingArea.New ();

	partial void Initialize ()
	{
		color_drawing_area.Hexpand = true;
		color_drawing_area.Vexpand = true;
		color_drawing_area.SetDrawFunc (OnDraw);
		SetChild (color_drawing_area);
	}

	public static new PintaColorButton New () => NewWithProperties ([]);

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
