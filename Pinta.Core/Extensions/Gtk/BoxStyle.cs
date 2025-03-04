using System;

namespace Pinta.Core;

public sealed class BoxStyle
{
	public static BoxStyle Horizontal { get; } = new (Gtk.Orientation.Horizontal);
	public static BoxStyle Vertical { get; } = new (Gtk.Orientation.Vertical);

	// --- Mandatory
	public Gtk.Orientation Orientation { get; }

	// --- Optional
	public int? Spacing { get; }
	public string? CssClass { get; }

	public BoxStyle (
		Gtk.Orientation orientation,
		int? spacing = null,
		string? cssClass = null)
	{
		if (spacing.HasValue && spacing.Value < 0) throw new ArgumentOutOfRangeException (nameof (spacing));
		Orientation = orientation;
		Spacing = spacing;
		CssClass = cssClass;
	}
}
