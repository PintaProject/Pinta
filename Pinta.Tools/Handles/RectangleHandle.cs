using System;
using System.Collections.Generic;
using System.Linq;
using Pinta.Core;

namespace Pinta.Tools;

enum HandlePoint
{
	UpperLeft = 0,
	LowerLeft = 1,
	UpperRight = 2,
	LowerRight = 3,
	Left = 4,
	Up = 5,
	Right = 6,
	Down = 7,
}

/// <summary>
/// A handle for specifying a rectangular region.
/// </summary>
public class RectangleHandle : IToolHandle
{
	private readonly IWorkspaceService workspace;

	private PointD start_pt;
	private PointD end_pt;
	private Size image_size;
	private readonly Dictionary<HandlePoint, MoveHandle> handles;
	private MoveHandle? active_handle;
	private PointD? drag_start_pos;

	public RectangleHandle (IWorkspaceService workspace)
	{
		this.workspace = workspace;

		handles = new Dictionary<HandlePoint, MoveHandle>
		{
			{ HandlePoint.UpperLeft, new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeNW) } },
			{ HandlePoint.LowerLeft, new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeSW) } },
			{ HandlePoint.UpperRight, new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeNE) } },
			{ HandlePoint.LowerRight, new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeSE) } },
			{ HandlePoint.Left, new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeW) } },
			{ HandlePoint.Up, new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeN) } },
			{ HandlePoint.Right, new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeE) } },
			{ HandlePoint.Down, new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeS) } },
		};

		foreach (var handle in handles.Values)
			handle.Active = true;
	}

	#region IToolHandle Implementation
	public bool Active { get; set; }

	public void Draw (Gtk.Snapshot snapshot)
	{
		foreach (MoveHandle handle in handles.Values)
			handle.Draw (snapshot);
	}
	#endregion

	/// <summary>
	/// If enabled, dragging the end point before the start point
	/// flips the points to produce a valid rectangle, rather than
	/// clamping to an empty rectangle.
	/// </summary>
	public bool InvertIfNegative { get; init; }

	/// <summary>
	/// Whether the user is currently dragging a corner of the rectangle.
	/// </summary>
	public bool IsDragging => drag_start_pos is not null;

	/// <summary>
	/// The rectangle selected by the user.
	/// </summary>
	public RectangleD Rectangle {
		get => RectangleD.FromPoints (start_pt, end_pt, InvertIfNegative);
		set {
			start_pt = value.Location ();
			end_pt = value.EndLocation ();
			UpdateHandlePositions ();
		}
	}

	/// <summary>
	/// Begins a drag operation if the mouse position is on top of a handle.
	/// Mouse movements are clamped to fall within the specified image size.
	/// </summary>
	public bool BeginDrag (in PointD canvasPos, in Size imageSize)
	{
		if (IsDragging)
			return false;

		image_size = imageSize;

		PointD viewPos = workspace.CanvasPointToView (canvasPos);
		UpdateHandleUnderPoint (viewPos);

		if (active_handle is null)
			return false;

		drag_start_pos = viewPos;
		return true;
	}

	/// <summary>
	/// Updates the rectangle as the mouse is moved.
	/// </summary>
	/// <returns>The region to redraw with InvalidateWindowRect()</returns>
	public RectangleI UpdateDrag (PointD canvasPos, bool shiftPressed)
	{
		if (!IsDragging || active_handle is null)
			throw new InvalidOperationException ("Drag operation has not been started!");

		// Clamp mouse position to the image size.
		canvasPos = new PointD (
			Math.Round (Math.Clamp (canvasPos.X, 0, image_size.Width)),
			Math.Round (Math.Clamp (canvasPos.Y, 0, image_size.Height)));

		RectangleI dirty = ComputeInvalidateRect ();

		HandlePoint activeHandlePoint = handles.First (kvp => kvp.Value == active_handle).Key;
		MoveActiveHandle (activeHandlePoint, canvasPos.X, canvasPos.Y, shiftPressed);
		UpdateHandlePositions ();

		dirty = dirty.Union (ComputeInvalidateRect ());
		return dirty;
	}

	/// <summary>
	/// If a drag operation is active, returns whether the mouse has actually moved.
	/// This can be used to distinguish a "click" from a "click and drag".
	/// </summary>
	public bool HasDragged (PointD canvasPos)
	{
		if (drag_start_pos is null)
			throw new InvalidOperationException ("Drag operation has not been started!");

		PointD viewPos = workspace.CanvasPointToView (canvasPos);
		return drag_start_pos.Value.DistanceSquared (viewPos) > 1;
	}

	/// <summary>
	/// Finishes a drag operation.
	/// </summary>
	public void EndDrag ()
	{
		if (drag_start_pos is null)
			throw new InvalidOperationException ("Drag operation has not been started!");

		image_size = Size.Empty;
		active_handle = null;
		drag_start_pos = null;

		// If the rectangle was inverted, fix inverted start/end points.
		RectangleD rect = Rectangle;
		start_pt = rect.Location ();
		end_pt = rect.EndLocation ();
	}

	/// <summary>
	/// The cursor to display, if the cursor is over a corner of the rectangle.
	/// </summary>
	public Gdk.Cursor? GetCursorAtPoint (PointD viewPos)
		=> handles.Values.FirstOrDefault (c => c.ContainsPoint (viewPos))?.Cursor;

	private void UpdateHandlePositions ()
	{
		PointD center = Utility.Lerp (start_pt, end_pt, 0.5f);

		handles[HandlePoint.UpperLeft].CanvasPosition = start_pt;
		handles[HandlePoint.LowerLeft].CanvasPosition = new PointD (start_pt.X, end_pt.Y);
		handles[HandlePoint.UpperRight].CanvasPosition = new PointD (end_pt.X, start_pt.Y);
		handles[HandlePoint.LowerRight].CanvasPosition = end_pt;
		handles[HandlePoint.Left].CanvasPosition = new PointD (start_pt.X, center.Y);
		handles[HandlePoint.Up].CanvasPosition = new PointD (center.X, start_pt.Y);
		handles[HandlePoint.Right].CanvasPosition = new PointD (end_pt.X, center.Y);
		handles[HandlePoint.Down].CanvasPosition = new PointD (center.X, end_pt.Y);
	}

	private void UpdateHandleUnderPoint (PointD viewPos)
	{
		active_handle = handles.Values.FirstOrDefault (c => c.ContainsPoint (viewPos));

		// If the rectangle is empty (e.g. starting a new drag), all the handles are
		// at the same position so pick the bottom right corner.
		RectangleD rect = Rectangle;
		if (active_handle is not null && rect is { Width: 0.0, Height: 0.0 })
			active_handle = handles[HandlePoint.LowerRight];
	}

	private bool IsHigherThanWide ()
	{
		return end_pt.X - start_pt.X <= end_pt.Y - start_pt.Y;
	}

	private void ExpandUniformlyX ()
	{
		double x_average = (start_pt.X + end_pt.X) / 2;
		double y_distance = (end_pt.Y - start_pt.Y) / 2;

		start_pt = start_pt with { X = x_average - y_distance };
		end_pt = end_pt with { X = x_average + y_distance };
	}

	private void ExpandUniformlyY ()
	{
		double y_average = (start_pt.Y + end_pt.Y) / 2;
		double x_distance = (end_pt.X - start_pt.X) / 2;

		start_pt = start_pt with { Y = y_average - x_distance };
		end_pt = end_pt with { Y = y_average + x_distance };
	}

	private void MoveActiveHandle (HandlePoint handle, double x, double y, bool shiftPressed)
	{
		// Update the rectangle's size depending on which handle was dragged.

		switch (handle) {
			case HandlePoint.UpperLeft:
				start_pt = new (x, y);

				if (shiftPressed) {
					if (IsHigherThanWide ()) {
						start_pt = start_pt with { X = end_pt.X - end_pt.Y + start_pt.Y };
					} else {
						start_pt = start_pt with { Y = end_pt.Y - end_pt.X + start_pt.X };
					}
				}
				return;

			case HandlePoint.LowerLeft:
				start_pt = start_pt with { X = x };
				end_pt = end_pt with { Y = y };

				if (shiftPressed) {
					if (IsHigherThanWide ())
						start_pt = start_pt with { X = end_pt.X - end_pt.Y + start_pt.Y };
					else
						end_pt = end_pt with { Y = start_pt.Y + end_pt.X - start_pt.X };
				}
				return;

			case HandlePoint.UpperRight:
				end_pt = end_pt with { X = x };
				start_pt = start_pt with { Y = y };

				if (shiftPressed) {
					if (IsHigherThanWide ())
						end_pt = end_pt with { X = start_pt.X + end_pt.Y - start_pt.Y };
					else
						start_pt = start_pt with { Y = end_pt.Y - end_pt.X + start_pt.X };
				}
				return;

			case HandlePoint.LowerRight:
				end_pt = new (x, y);

				if (shiftPressed) {
					if (IsHigherThanWide ())
						end_pt = end_pt with { X = start_pt.X + end_pt.Y - start_pt.Y };
					else
						end_pt = end_pt with { Y = start_pt.Y + end_pt.X - start_pt.X };
				}
				return;

			case HandlePoint.Left:
				start_pt = start_pt with { X = x };

				if (shiftPressed) {
					ExpandUniformlyY ();
				}
				return;

			case HandlePoint.Up:
				start_pt = start_pt with { Y = y };

				if (shiftPressed) {
					ExpandUniformlyX ();
				}
				return;

			case HandlePoint.Right:
				end_pt = end_pt with { X = x };

				if (shiftPressed) {
					ExpandUniformlyY ();
				}
				return;

			case HandlePoint.Down:
				end_pt = end_pt with { Y = y };

				if (shiftPressed) {
					ExpandUniformlyX ();
				}
				return;

			default:
				throw new ArgumentOutOfRangeException (nameof (handle));
		}
	}

	/// <summary>
	/// Bounding rectangle to use with InvalidateWindowRect() when triggering a redraw.
	/// </summary>
	private RectangleI ComputeInvalidateRect ()
		=> MoveHandle.UnionInvalidateRects (handles.Values);
}
