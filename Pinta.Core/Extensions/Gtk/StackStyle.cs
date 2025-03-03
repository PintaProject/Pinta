using System;

namespace Pinta.Core;

public sealed class StackStyle
{
	public static StackStyle Horizontal { get; } = new (Gtk.Orientation.Horizontal);
	public static StackStyle Vertical { get; } = new (Gtk.Orientation.Vertical);

	// --- Mandatory
	public Gtk.Orientation Orientation { get; }

	// --- Optional
	public int? Spacing { get; }

	public StackStyle (
		Gtk.Orientation orientation,
		int? spacing = null)
	{
		if (spacing.HasValue && spacing.Value < 0) throw new ArgumentOutOfRangeException (nameof (spacing));
		Orientation = orientation;
		Spacing = spacing;
	}
}
