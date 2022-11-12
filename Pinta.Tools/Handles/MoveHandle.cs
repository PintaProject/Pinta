using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools
{
	/// <summary>
	/// A handle that the user can click and move, e.g. for resizing a selection.
	/// </summary>
	public class MoveHandle : IToolHandle
	{
		public static readonly Cairo.Color FillColor = new (0, 0, 1, 0.9);
		public static readonly Cairo.Color StrokeColor = new (1, 1, 1, 0.7);

		public Cairo.PointD CanvasPosition { get; set; }
		public bool Active { get; set; } = false;
		public Gdk.CursorType Cursor { get; init; }

		/// <summary>
		/// Tests whether the window point is inside the handle's area.
		/// The area to grab a handle is a bit larger than the rendered area for easier selection.
		/// </summary>
		public bool ContainsPoint (Cairo.PointD window_point)
		{
			const int tolerance = 5;

			var bounds = ComputeWindowRect ().Inflate (tolerance, tolerance);
			return bounds.ContainsPoint (window_point);
		}

		/// <summary>
		/// Draw the handle, at a constant window space size (i.e. not depending on the image zoom or resolution)
		/// </summary>
		public void Draw (Cairo.Context cr)
		{
			cr.FillStrokedEllipse (ComputeWindowRect (), MoveHandle.FillColor, MoveHandle.StrokeColor, 1);
		}

		/// <summary>
		/// Bounding rectangle to use with InvalidateWindowRect() when triggering a redraw.
		/// </summary>
		public Gdk.Rectangle InvalidateRect => ComputeWindowRect ().Inflate (2, 2).ToGdkRectangle ();

		/// <summary>
		/// Bounding rectangle of the handle (in window space).
		/// </summary>
		private Cairo.Rectangle ComputeWindowRect ()
		{
			const double radius = 4.5;
			const double diameter = 2 * radius;

			var window_pt = PintaCore.Workspace.CanvasPointToWindow (CanvasPosition);
			return new Cairo.Rectangle (window_pt.X - radius, window_pt.Y - radius, diameter, diameter);
		}

		/// <summary>
		/// Returns the union of the invalidate rectangles for a collection of handles.
		/// </summary>
		public static Gdk.Rectangle UnionInvalidateRects (IEnumerable<MoveHandle> handles) =>
			handles.Select (c => c.InvalidateRect).DefaultIfEmpty (Gdk.Rectangle.Zero).Aggregate ((accum, r) => accum.Union (r));
	}
}

