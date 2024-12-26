using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Tools;

/// <summary>
/// A handle that the user can click and move, e.g. for resizing a selection.
/// </summary>
public sealed class MoveHandle : IToolHandle
{
	private static readonly Cairo.Color fill_color = new (0, 0, 1, 1);
	private static readonly Cairo.Color selection_fill_color = new (1, 0.5, 0, 1);
	private static readonly Cairo.Color stroke_color = new (1, 1, 1, 0.7);
	private static readonly Gdk.Cursor default_cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.Default);

	public PointD CanvasPosition { get; set; }

	/// <summary>
	/// Inactive handles are not drawn.
	/// </summary>
	public bool Active { get; set; } = false;

	/// <summary>
	/// A handle that is selected by the user for interaction is drawn in a different color.
	/// </summary>
	public bool Selected { get; set; } = false;

	public Gdk.Cursor Cursor { get; init; } = default_cursor;

	/// <summary>
	/// Tests whether the window point is inside the handle's area.
	/// The area to grab a handle is a bit larger than the rendered area for easier selection.
	/// </summary>
	public bool ContainsPoint (PointD window_point)
	{
		const int tolerance = 5;

		var bounds = ComputeWindowRect ().Inflated (tolerance, tolerance);
		return bounds.ContainsPoint (window_point);
	}

	/// <summary>
	/// Draw the handle, at a constant window space size (i.e. not depending on the image zoom or resolution)
	/// </summary>
	public void Draw (Cairo.Context cr)
	{
		cr.FillStrokedEllipse (ComputeWindowRect (), Selected ? selection_fill_color : fill_color, stroke_color, 1);
	}

	/// <summary>
	/// Bounding rectangle to use with InvalidateWindowRect() when triggering a redraw.
	/// </summary>
	public RectangleI InvalidateRect => ComputeWindowRect ().Inflated (2, 2).ToInt ();

	/// <summary>
	/// Bounding rectangle of the handle (in window space).
	/// </summary>
	private RectangleD ComputeWindowRect ()
	{
		const double radius = 4.5;
		const double diameter = 2 * radius;

		var window_pt = PintaCore.Workspace.CanvasPointToView (CanvasPosition);
		return new RectangleD (window_pt.X - radius, window_pt.Y - radius, diameter, diameter);
	}

	/// <summary>
	/// Returns the union of the invalidate rectangles for a collection of handles.
	/// </summary>
	public static RectangleI UnionInvalidateRects (IEnumerable<MoveHandle> handles) =>
		handles
		.Select (c => c.InvalidateRect)
		.DefaultIfEmpty (RectangleI.Zero)
		.Aggregate ((accum, r) => accum.Union (r));
}

