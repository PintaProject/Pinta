using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Tools;

/// <summary>
/// A handle that the user can click and move, e.g. for resizing a selection.
/// </summary>
public sealed class MoveHandle : IToolHandle
{
	private static readonly Gdk.RGBA fill_color = new () { Red = 0, Green = 0, Blue = 1, Alpha = 1 };
	private static readonly Gdk.RGBA selection_fill_color = new () { Red = 1, Green = 0.5f, Blue = 0, Alpha = 1 };
	private static readonly Gdk.RGBA stroke_color = new () { Red = 1, Green = 1, Blue = 1, Alpha = 0.7f };
	private static readonly Gdk.Cursor default_cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.Default);

	private const double RADIUS = 4.5;

	private readonly IWorkspaceService workspace;

	public MoveHandle (IWorkspaceService workspace)
	{
		this.workspace = workspace;
	}

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
		const int TOLERANCE = 5;

		RectangleD bounds = ComputeWindowRect ().Inflated (TOLERANCE, TOLERANCE);
		return bounds.ContainsPoint (window_point);
	}

	/// <summary>
	/// Draw the handle, at a constant window space size (i.e. not depending on the image zoom or resolution)
	/// </summary>
	public void Draw (Gtk.Snapshot snapshot)
	{
		Gsk.PathBuilder pathBuilder = Gsk.PathBuilder.New ();
		PointD windowPt = workspace.CanvasPointToView (CanvasPosition);
		pathBuilder.AddCircle (windowPt.ToGraphenePoint (), (float) RADIUS);
		Gsk.Path path = pathBuilder.ToPath ();

		Gdk.RGBA fillColor = Selected ? selection_fill_color : fill_color;
		snapshot.AppendFill (path, Gsk.FillRule.EvenOdd, fillColor);

		Gsk.Stroke stroke = Gsk.Stroke.New (lineWidth: 1.0f);
		snapshot.AppendStroke (path, stroke, stroke_color);
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
		const double DIAMETER = 2 * RADIUS;

		PointD windowPt = workspace.CanvasPointToView (CanvasPosition);
		return new RectangleD (windowPt.X - RADIUS, windowPt.Y - RADIUS, DIAMETER, DIAMETER);
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

