using System;
using System.Collections.Immutable;
using System.Linq;
using Pinta.Core;

namespace Pinta.Tools;

/// <summary>
/// A handle for specifying a rectangular region.
/// </summary>
public class RectangleHandle : IToolHandle
{
	private readonly IWorkspaceService workspace;

	private PointD start_pt;
	private PointD end_pt;
	private Size image_size;
	private readonly ImmutableArray<MoveHandle> handles;
	private MoveHandle? active_handle;
	private PointD? drag_start_pos;

	public RectangleHandle (IWorkspaceService workspace)
	{
		this.workspace = workspace;

		handles = [
			new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeNW) },
			new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeSW) },
			new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeNE) },
			new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeSE) },
			new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeW) },
			new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeN) },
			new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeE) },
			new (workspace) { Cursor = GdkExtensions.CursorFromName (Resources.StandardCursors.ResizeS) },
		];

		foreach (var handle in handles)
			handle.Active = true;
	}

	#region IToolHandle Implementation
	public bool Active { get; set; }

	public void Draw (Gtk.Snapshot snapshot)
	{
		foreach (MoveHandle handle in handles)
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

		int activeHandleIndex = handles.IndexOf (active_handle);
		MoveActiveHandle (activeHandleIndex, canvasPos.X, canvasPos.Y, shiftPressed);
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
		return drag_start_pos.Value.Distance (viewPos) > 1;
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
		=> handles.FirstOrDefault (c => c.ContainsPoint (viewPos))?.Cursor;

	private void UpdateHandlePositions ()
	{
		RectangleD rect = Rectangle;
		PointD center = rect.GetCenter ();
		handles[0].CanvasPosition = new PointD (rect.Left, rect.Top);
		handles[1].CanvasPosition = new PointD (rect.Left, rect.Bottom);
		handles[2].CanvasPosition = new PointD (rect.Right, rect.Top);
		handles[3].CanvasPosition = new PointD (rect.Right, rect.Bottom);
		handles[4].CanvasPosition = new PointD (rect.Left, center.Y);
		handles[5].CanvasPosition = new PointD (center.X, rect.Top);
		handles[6].CanvasPosition = new PointD (rect.Right, center.Y);
		handles[7].CanvasPosition = new PointD (center.X, rect.Bottom);
	}

	private void UpdateHandleUnderPoint (PointD viewPos)
	{
		active_handle = handles.FirstOrDefault (c => c.ContainsPoint (viewPos));

		// If the rectangle is empty (e.g. starting a new drag), all the handles are
		// at the same position so pick the bottom right corner.
		RectangleD rect = Rectangle;
		if (active_handle is not null && rect is { Width: 0.0, Height: 0.0 })
			active_handle = handles[3];
	}

	private void MoveActiveHandle (int handle, double x, double y, bool shiftPressed)
	{
		// Update the rectangle's size depending on which handle was dragged.
		switch (handle) {
			case 0:
				start_pt = new (x, y);

				if (!shiftPressed) return;

				start_pt =
					(end_pt.X - start_pt.X <= end_pt.Y - start_pt.Y)
					? (start_pt with { X = end_pt.X - end_pt.Y + start_pt.Y })
					: (start_pt with { Y = end_pt.Y - end_pt.X + start_pt.X });

				return;

			case 1:
				start_pt = start_pt with { X = x };
				end_pt = end_pt with { Y = y };

				if (!shiftPressed) return;

				if (end_pt.X - start_pt.X <= end_pt.Y - start_pt.Y)
					start_pt = start_pt with { X = end_pt.X - end_pt.Y + start_pt.Y };
				else
					end_pt = end_pt with { Y = start_pt.Y + end_pt.X - start_pt.X };

				return;

			case 2:
				end_pt = end_pt with { X = x };
				start_pt = start_pt with { Y = y };

				if (!shiftPressed) return;

				if (end_pt.X - start_pt.X <= end_pt.Y - start_pt.Y)
					end_pt = end_pt with { X = start_pt.X + end_pt.Y - start_pt.Y };
				else
					start_pt = start_pt with { Y = end_pt.Y - end_pt.X + start_pt.X };

				return;

			case 3:
				end_pt = new (x, y);

				if (!shiftPressed)
					return;

				if (end_pt.X - start_pt.X <= end_pt.Y - start_pt.Y)
					end_pt = end_pt with { X = start_pt.X + end_pt.Y - start_pt.Y };
				else
					end_pt = end_pt with { Y = start_pt.Y + end_pt.X - start_pt.X };

				return;

			case 4:
				start_pt = start_pt with { X = x };

				if (!shiftPressed) return;

				double d4 = end_pt.X - start_pt.X;
				start_pt = start_pt with { Y = (start_pt.Y + end_pt.Y - d4) / 2 };
				end_pt = end_pt with { Y = (start_pt.Y + end_pt.Y + d4) / 2 };

				return;

			case 5:
				start_pt = start_pt with { Y = y };

				if (!shiftPressed) return;

				double d5 = end_pt.Y - start_pt.Y;
				start_pt = start_pt with { X = (start_pt.X + end_pt.X - d5) / 2 };
				end_pt = end_pt with { X = (start_pt.X + end_pt.X + d5) / 2 };

				return;

			case 6:
				end_pt = end_pt with { X = x };

				if (!shiftPressed) return;

				double d6 = end_pt.X - start_pt.X;
				start_pt = start_pt with { Y = (start_pt.Y + end_pt.Y - d6) / 2 };
				end_pt = end_pt with { Y = (start_pt.Y + end_pt.Y + d6) / 2 };

				return;

			case 7:
				end_pt = end_pt with { Y = y };

				if (!shiftPressed) return;

				double d7 = end_pt.Y - start_pt.Y;
				start_pt = start_pt with { X = (start_pt.X + end_pt.X - d7) / 2 };
				end_pt = end_pt with { X = (start_pt.X + end_pt.X + d7) / 2 };

				return;

			default:
				throw new ArgumentOutOfRangeException (nameof (handle));
		}
	}

	/// <summary>
	/// Bounding rectangle to use with InvalidateWindowRect() when triggering a redraw.
	/// </summary>
	private RectangleI ComputeInvalidateRect ()
		=> MoveHandle.UnionInvalidateRects (handles);
}
