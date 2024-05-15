using System;
using System.Diagnostics;
using System.Linq;
using Cairo;
using Pinta.Core;
using Pinta.Resources;

namespace Pinta.Tools.Handles;

/// <summary>
/// A handle for specifying a rectangular region.
/// </summary>
public class RectangleHandle : IToolHandle
{
	private PointD start_pt = PointD.Zero;
	private PointD end_pt = PointD.Zero;
	private Size image_size = Size.Empty;
	private readonly MoveHandle[] handles = new MoveHandle[8];
	private MoveHandle? active_handle = null;
	private PointD? drag_start_pos = null;
	private double aspect_ratio = 1;

	public RectangleHandle ()
	{
		handles[0] = new MoveHandle { CursorName = StandardCursors.ResizeNW };
		handles[1] = new MoveHandle { CursorName = StandardCursors.ResizeSW };
		handles[2] = new MoveHandle { CursorName = StandardCursors.ResizeNE };
		handles[3] = new MoveHandle { CursorName = StandardCursors.ResizeSE };
		handles[4] = new MoveHandle { CursorName = StandardCursors.ResizeW };
		handles[5] = new MoveHandle { CursorName = StandardCursors.ResizeN };
		handles[6] = new MoveHandle { CursorName = StandardCursors.ResizeE };
		handles[7] = new MoveHandle { CursorName = StandardCursors.ResizeS };

		foreach (var handle in handles)
			handle.Active = true;
	}

	private enum HandleIndex
	{
		TopLeft = 0,
		BottomLeft = 1,
		TopRight = 2,
		BottomRight = 3,
		MiddleLeft = 4,
		TopMiddle = 5,
		MiddleRight = 6,
		BottomMiddle = 7
	}

	#region IToolHandle Implementation
	public bool Active { get; set; }

	public void Draw (Context cr)
	{
		foreach (MoveHandle handle in handles) {
			handle.Draw (cr);
		}
	}
	#endregion

	/// <summary>
	/// If enabled, dragging the end point before the start point
	/// flips the points to produce a valid rectangle, rather than
	/// producing an empty rectangle.
	/// </summary>
	public bool InvertIfNegative { get; set; }

	/// <summary>
	/// Bounding rectangle to use with InvalidateWindowRect() when triggering a redraw.
	/// </summary>
	public RectangleI InvalidateRect => MoveHandle.UnionInvalidateRects (handles);

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
	public bool BeginDrag (in PointD canvas_pos, in Size image_size)
	{
		if (IsDragging)
			return false;

		aspect_ratio = Rectangle.Width / Rectangle.Height;
		this.image_size = image_size;

		PointD view_pos = PintaCore.Workspace.CanvasPointToView (canvas_pos);
		UpdateHandleUnderPoint (view_pos);

		if (active_handle is null)
			return false;

		drag_start_pos = view_pos;
		return true;
	}

	/// <summary>
	/// Updates the rectangle as the mouse is moved.
	/// </summary>
	/// <param name="constrain">Constrain the rectangle to be a square</param>
	/// <returns>The region to redraw with InvalidateWindowRect()</returns>
	public RectangleI UpdateDrag (PointD canvas_pos, ConstrainType constrain = ConstrainType.None)
	{
		if (!IsDragging)
			throw new InvalidOperationException ("Drag operation has not been started!");

		// Clamp mouse position to the image size.
		canvas_pos = new PointD (
			Math.Round (Math.Clamp (canvas_pos.X, 0, image_size.Width)),
			Math.Round (Math.Clamp (canvas_pos.Y, 0, image_size.Height)));

		var dirty = InvalidateRect;

		MoveActiveHandle (canvas_pos.X, canvas_pos.Y, constrain);
		UpdateHandlePositions ();

		dirty = dirty.Union (InvalidateRect);
		return dirty;
	}

	/// <summary>
	/// If a drag operation is active, returns whether the mouse has actually moved.
	/// This can be used to distinguish a "click" from a "click and drag".
	/// </summary>
	public bool HasDragged (PointD canvas_pos)
	{
		if (drag_start_pos is null)
			throw new InvalidOperationException ("Drag operation has not been started!");

		PointD view_pos = PintaCore.Workspace.CanvasPointToView (canvas_pos);
		return drag_start_pos.Value.Distance (view_pos) > 1;
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
		var rect = Rectangle;
		start_pt = rect.Location ();
		end_pt = rect.EndLocation ();
	}

	/// <summary>
	/// Name of the cursor to display, if the cursor is over a corner of the rectangle.
	/// </summary>
	public string? GetCursorAtPoint (PointD view_pos)
		=> handles.FirstOrDefault (c => c.ContainsPoint (view_pos))?.CursorName;

	private void UpdateHandlePositions ()
	{
		var rect = Rectangle;
		var center = rect.GetCenter ();
		handles[0].CanvasPosition = new PointD (rect.Left, rect.Top);
		handles[1].CanvasPosition = new PointD (rect.Left, rect.Bottom);
		handles[2].CanvasPosition = new PointD (rect.Right, rect.Top);
		handles[3].CanvasPosition = new PointD (rect.Right, rect.Bottom);
		handles[4].CanvasPosition = new PointD (rect.Left, center.Y);
		handles[5].CanvasPosition = new PointD (center.X, rect.Top);
		handles[6].CanvasPosition = new PointD (rect.Right, center.Y);
		handles[7].CanvasPosition = new PointD (center.X, rect.Bottom);
	}

	private void UpdateHandleUnderPoint (PointD view_pos)
	{
		active_handle = handles.FirstOrDefault (c => c.ContainsPoint (view_pos));

		// If the rectangle is empty (e.g. starting a new drag), all the handles are
		// at the same position so pick the bottom right corner.
		var rect = Rectangle;
		if (active_handle is not null && rect.Width == 0.0 && rect.Height == 0.0)
			active_handle = handles[3];
	}

	private void MoveActiveHandle (double x, double y, ConstrainType constrain = ConstrainType.None)
	{
		// TODO - the constrain option should use the aspect ratio present when first dragging rather than the current aspect ratio
		// Update the rectangle's size depending on which handle was dragged.
		Console.WriteLine((HandleIndex)Array.IndexOf (handles, active_handle));
		Console.WriteLine(x + " " + y);
		switch ((HandleIndex)Array.IndexOf (handles, active_handle)) {
			case HandleIndex.TopLeft:
				switch (constrain) {
					case ConstrainType.None:
						start_pt = new (x, y);
						break;
					case ConstrainType.Square:
						/*if (end_pt.X - x > end_pt.Y - y)
							start_pt = new (end_pt.X - end_pt.Y + start_pt.Y, y);
						else
							start_pt = new (x, end_pt.Y - end_pt.X + start_pt.X);*/
						if (x - end_pt.X >= 0 && y - end_pt.Y >= 0) {
							if (x - end_pt.X > y - end_pt.Y)
								start_pt = new (end_pt.X + y - end_pt.Y, y);
							else
								start_pt = new (x, end_pt.Y + x - end_pt.X);
						} else if (x - end_pt.X <= 0 && y - end_pt.Y <= 0) {
							if (x - end_pt.X < y - end_pt.Y)
								start_pt = new (end_pt.X + y - end_pt.Y, y);
							else
								start_pt = new (x, end_pt.Y + x - end_pt.X);
						} else if (x - end_pt.X <= 0 && y - end_pt.Y >= 0) {
							if (x - end_pt.X < end_pt.Y - y)
								start_pt = new (end_pt.X + end_pt.Y - y, y);
							else
								start_pt = new (x, end_pt.Y + end_pt.X - x);
						} else if (x - end_pt.X >= 0 && y - end_pt.Y <= 0) {
							if (end_pt.X - x < y - end_pt.Y)
								start_pt = new (end_pt.X + end_pt.Y - y, y);
							else
								start_pt = new (x, end_pt.Y + end_pt.X - x);
						}
						break;
					case ConstrainType.AspectRatio:
						if (x - end_pt.X >= 0 && y - end_pt.Y >= 0) {
							if (x - end_pt.X > aspect_ratio * (y - end_pt.Y))
								start_pt = new (end_pt.X + (y - end_pt.Y) * aspect_ratio, y);
							else
								start_pt = new (x, end_pt.Y + (x - end_pt.X) / aspect_ratio);
						} else if (x - end_pt.X <= 0 && y - end_pt.Y <= 0) {
							if (x - end_pt.X < aspect_ratio * (y - end_pt.Y))
								start_pt = new (end_pt.X + (y - end_pt.Y) * aspect_ratio, y);
							else
								start_pt = new (x, end_pt.Y + (x - end_pt.X) / aspect_ratio);
						} else if (x - end_pt.X <= 0 && y - end_pt.Y >= 0) {
							if (x - end_pt.X < aspect_ratio * (end_pt.Y - y))
								start_pt = new (end_pt.X + (end_pt.Y - y) * aspect_ratio, y);
							else
								start_pt = new (x, end_pt.Y + (end_pt.X - x) / aspect_ratio);
						} else if (x - end_pt.X >= 0 && y - end_pt.Y <= 0) {
							if (end_pt.X - x < aspect_ratio * (y - end_pt.Y))
								start_pt = new (end_pt.X + (end_pt.Y - y) * aspect_ratio, y);
							else
								start_pt = new (x, end_pt.Y + (end_pt.X - x) / aspect_ratio);
						}
						break;
				}
				break;
			case HandleIndex.BottomLeft:
				start_pt = start_pt with { X = x };
				end_pt = end_pt with { Y = y };
				switch (constrain) {
					case ConstrainType.Square:
						/*if (end_pt.X - x > y - start_pt.Y)
							start_pt = start_pt with { X = end_pt.X - y + start_pt.Y };
						else
							end_pt = end_pt with { Y = start_pt.Y + end_pt.X - x };*/
						if (x - end_pt.X >= 0 && y - start_pt.Y >= 0) {
							if (x - end_pt.X > y - start_pt.Y)
								start_pt = start_pt with { X = end_pt.X + y - start_pt.Y };
							else
								end_pt = end_pt with { Y = start_pt.Y + x - end_pt.X };
						} else if (x - end_pt.X <= 0 && y - start_pt.Y <= 0) {
							if (x - end_pt.X < y - start_pt.Y)
								start_pt = start_pt with { X = end_pt.X + y - start_pt.Y };
							else
								end_pt = end_pt with { Y = start_pt.Y + x - end_pt.X };
						} else if (x - end_pt.X <= 0 && y - start_pt.Y >= 0) {
							if (x - end_pt.X < start_pt.Y - y)
								start_pt = start_pt with { X = end_pt.X + start_pt.Y - y };
							else
								end_pt = end_pt with { Y = start_pt.Y + end_pt.X - x };
						} else if (x - end_pt.X >= 0 && y - start_pt.Y <= 0) {
							if (end_pt.X - x < y - start_pt.Y)
								start_pt = start_pt with { X = end_pt.X + start_pt.Y - y };
							else
								end_pt = end_pt with { Y = start_pt.Y + end_pt.X - x };
						}
						break;
					case ConstrainType.AspectRatio:
						if (x - end_pt.X >= 0 && y - start_pt.Y >= 0) {
							if (x - end_pt.X > aspect_ratio * (y - start_pt.Y))
								start_pt = start_pt with { X = end_pt.X + (y - start_pt.Y) * aspect_ratio };
							else
								end_pt = end_pt with { Y = start_pt.Y + (x - end_pt.X) / aspect_ratio };
						} else if (x - end_pt.X <= 0 && y - start_pt.Y <= 0) {
							if (x - end_pt.X < aspect_ratio * (y - start_pt.Y))
								start_pt = start_pt with { X = end_pt.X + (y - start_pt.Y) * aspect_ratio };
							else
								end_pt = end_pt with { Y = start_pt.Y + (x - end_pt.X) / aspect_ratio };
						} else if (x - end_pt.X <= 0 && y - start_pt.Y >= 0) {
							if (x - end_pt.X < aspect_ratio * (start_pt.Y - y))
								start_pt = start_pt with { X = end_pt.X + (start_pt.Y - y) * aspect_ratio };
							else
								end_pt = end_pt with { Y = start_pt.Y + (end_pt.X - x) / aspect_ratio };
						} else if (x - end_pt.X >= 0 && y - start_pt.Y <= 0) {
							if (end_pt.X - x < aspect_ratio * (y - start_pt.Y))
								start_pt = start_pt with { X = end_pt.X + (start_pt.Y - y) * aspect_ratio };
							else
								end_pt = end_pt with { Y = start_pt.Y + (end_pt.X - x) / aspect_ratio };
						}
						break;
				}
				break;
			case HandleIndex.TopRight:
				start_pt = start_pt with { Y = y };
				end_pt = end_pt with { X = x };
				switch (constrain) {
					case ConstrainType.Square:
						/*if (end_pt.X - start_pt.X > end_pt.Y - start_pt.Y)
							end_pt = end_pt with { X = start_pt.X + end_pt.Y - start_pt.Y };
						else
							start_pt = start_pt with { Y = end_pt.Y - end_pt.X + start_pt.X };*/
						if (x - start_pt.X >= 0 && y - end_pt.Y >= 0) {
							if (x - start_pt.X > y - end_pt.Y)
								end_pt = end_pt with { X = start_pt.X + y - end_pt.Y };
							else
								start_pt = start_pt with { Y = end_pt.Y + x - start_pt.X };
						} else if (x - start_pt.X <= 0 && y - end_pt.Y <= 0) {
							if (x - start_pt.X < y - end_pt.Y)
								end_pt = end_pt with { X = start_pt.X + y - end_pt.Y };
							else
								start_pt = start_pt with { Y = end_pt.Y + x - start_pt.X };
						} else if (x - start_pt.X <= 0 && y - end_pt.Y >= 0) {
							if (x - start_pt.X < end_pt.Y - y)
								end_pt = end_pt with { X = start_pt.X + end_pt.Y - y };
							else
								start_pt = start_pt with { Y = end_pt.Y + start_pt.X - x };
						} else if (x - start_pt.X >= 0 && y - end_pt.Y <= 0) {
							if (start_pt.X - x < y - end_pt.Y)
								end_pt = end_pt with { X = start_pt.X + end_pt.Y - y };
							else
								start_pt = start_pt with { Y = end_pt.Y + start_pt.X - x };
						}
						break;
					case ConstrainType.AspectRatio:
						if (x - start_pt.X >= 0 && y - end_pt.Y >= 0) {
							if (x - start_pt.X > aspect_ratio * (y - end_pt.Y))
								end_pt = end_pt with { X = start_pt.X + (y - end_pt.Y) * aspect_ratio };
							else
								start_pt = start_pt with { Y = end_pt.Y + (x - start_pt.X) / aspect_ratio };
						} else if (x - start_pt.X <= 0 && y - end_pt.Y <= 0) {
							if (x - start_pt.X < aspect_ratio * (y - end_pt.Y))
								end_pt = end_pt with { X = start_pt.X + (y - end_pt.Y) * aspect_ratio };
							else
								start_pt = start_pt with { Y = end_pt.Y + (x - start_pt.X) / aspect_ratio };
						} else if (x - start_pt.X <= 0 && y - end_pt.Y >= 0) {
							if (x - start_pt.X < aspect_ratio * (end_pt.Y - y))
								end_pt = end_pt with { X = start_pt.X + (end_pt.Y - y) * aspect_ratio };
							else
								start_pt = start_pt with { Y = end_pt.Y + (start_pt.X - x) / aspect_ratio };
						} else if (x - start_pt.X >= 0 && y - end_pt.Y <= 0) {
							if (start_pt.X - x < aspect_ratio * (y - end_pt.Y))
								end_pt = end_pt with { X = start_pt.X + (end_pt.Y - y) * aspect_ratio };
							else
								start_pt = start_pt with { Y = end_pt.Y + (start_pt.X - x) / aspect_ratio };
						}
						break;
				}
				break;
			case HandleIndex.BottomRight:
				Console.WriteLine (start_pt);
				switch (constrain) {
					case ConstrainType.None:
						end_pt = new (x, y);
						break;
					case ConstrainType.Square:
						if (x - start_pt.X > 0 && y - start_pt.Y > 0) {
							if (x - start_pt.X > y - start_pt.Y)
								end_pt = new (start_pt.X + y - start_pt.Y, y);
							else
								end_pt = new (x, start_pt.Y + x - start_pt.X);
						} else if (x - start_pt.X < 0 && y - start_pt.Y < 0) {
							if (x - start_pt.X < y - start_pt.Y)
								end_pt = new (start_pt.X + y - start_pt.Y, y);
							else
								end_pt = new (x, start_pt.Y + x - start_pt.X);
						} else if (x - start_pt.X < 0 && y - start_pt.Y > 0) {
							if (x - start_pt.X < start_pt.Y - y)
								end_pt = new (start_pt.X + start_pt.Y - y, y);
							else
								end_pt = new (x, start_pt.Y + start_pt.X - x);
						} else if (x - start_pt.X > 0 && y - start_pt.Y < 0) {
							if (start_pt.X - x < y - start_pt.Y)
								end_pt = new (start_pt.X + start_pt.Y - y, y);
							else
								end_pt = new (x, start_pt.Y + start_pt.X - x);
						}

						break;
					case ConstrainType.AspectRatio:
						if (x - start_pt.X > 0 && y - start_pt.Y > 0) {
							Console.WriteLine("Cursor is to the bottom right of the start point");
							if (x - start_pt.X > aspect_ratio * (y - start_pt.Y))
								end_pt = new(start_pt.X + (y - start_pt.Y) * aspect_ratio, y);
							else
								end_pt = new(x, start_pt.Y + (x - start_pt.X) / aspect_ratio);
						} else if (x - start_pt.X < 0 && y - start_pt.Y < 0) {
							Console.WriteLine("Cursor is to the top left of the start point");
							if (x - start_pt.X < aspect_ratio * (y - start_pt.Y))
								end_pt = new(start_pt.X + (y - start_pt.Y) * aspect_ratio, y);
							else
								end_pt = new(x, start_pt.Y + (x - start_pt.X) / aspect_ratio);
						} else if (x - start_pt.X < 0 && y - start_pt.Y > 0) {
							Console.WriteLine("Cursor is to the bottom left of the start point");
							if (x - start_pt.X < aspect_ratio * (start_pt.Y - y))
								end_pt = new(start_pt.X + (start_pt.Y - y) * aspect_ratio, y);
							else
								end_pt = new(x, start_pt.Y + (start_pt.X - x) / aspect_ratio);
						} else if (x - start_pt.X > 0 && y - start_pt.Y < 0) {
							Console.WriteLine("Cursor is to the top right of the start point");
							if (start_pt.X - x < aspect_ratio * (y - start_pt.Y))
								end_pt = new(start_pt.X + (start_pt.Y - y) * aspect_ratio, y);
							else
								end_pt = new(x, start_pt.Y + (start_pt.X - x) / aspect_ratio);
						}

						break;
				}
				break;
			case HandleIndex.MiddleLeft:
				start_pt = start_pt with { X = x };
				switch (constrain) {
					case ConstrainType.Square:
						var d = end_pt.X - start_pt.X;
						var startY = start_pt.Y;
						start_pt = start_pt with { Y = (startY + end_pt.Y - d) / 2 };
						end_pt = end_pt with { Y = (startY + end_pt.Y + d) / 2 };
						break;
					case ConstrainType.AspectRatio:
						var d2 = end_pt.X - start_pt.X;
						var startY2 = start_pt.Y;
						start_pt = start_pt with { Y = (startY2 + end_pt.Y - d2 / aspect_ratio) / 2 };
						end_pt = end_pt with { Y = (startY2 + end_pt.Y + d2 / aspect_ratio) / 2 };
						break;
				}
				break;
			case HandleIndex.TopMiddle:
				start_pt = start_pt with { Y = y };
				switch (constrain) {
					case ConstrainType.Square:
						var d = end_pt.Y - start_pt.Y;
						var startX = start_pt.X;
						start_pt = start_pt with { X = (startX + end_pt.X - d) / 2 };
						end_pt = end_pt with { X = (startX + end_pt.X + d) / 2 };
						break;
					case ConstrainType.AspectRatio:
						var d2 = end_pt.Y - start_pt.Y;
						var startX2 = start_pt.X;
						start_pt = start_pt with { X = (startX2 + end_pt.X - d2 * aspect_ratio) / 2 };
						end_pt = end_pt with { X = (startX2 + end_pt.X + d2 * aspect_ratio) / 2 };
						break;
				}
				break;
			case HandleIndex.MiddleRight:
				end_pt = end_pt with { X = x };
				switch (constrain) {
					case ConstrainType.Square:
						var d = end_pt.X - start_pt.X;
						var startY = start_pt.Y;
						start_pt = start_pt with { Y = (startY + end_pt.Y - d) / 2 };
						end_pt = end_pt with { Y = (startY + end_pt.Y + d) / 2 };
						break;
					case ConstrainType.AspectRatio:
						var d2 = end_pt.X - start_pt.X;
						var startY2 = start_pt.Y;
						start_pt = start_pt with { Y = (startY2 + end_pt.Y - d2 / aspect_ratio) / 2 };
						end_pt = end_pt with { Y = (startY2 + end_pt.Y + d2 / aspect_ratio) / 2 };
						break;
				}
				break;
			case HandleIndex.BottomMiddle:
				end_pt = end_pt with { Y = y };
				switch (constrain) {
					case ConstrainType.Square:
						var d = end_pt.Y - start_pt.Y;
						var startX = start_pt.X;
						start_pt = start_pt with { X = (startX + end_pt.X - d) / 2 };
						end_pt = end_pt with { X = (startX + end_pt.X + d) / 2 };
						break;
					case ConstrainType.AspectRatio:
						var d2 = end_pt.Y - start_pt.Y;
						var startX2 = start_pt.X;
						start_pt = start_pt with { X = (startX2 + end_pt.X - d2 * aspect_ratio) / 2 };
						end_pt = end_pt with { X = (startX2 + end_pt.X + d2 * aspect_ratio) / 2 };
						break;
				}
				break;

			default:
				throw new ArgumentOutOfRangeException (nameof (active_handle));
		}

	}
}

public enum ConstrainType
{
	None,
	Square,
	AspectRatio
}
