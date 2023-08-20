// 
// SelectTool.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Gdk;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

public abstract class SelectTool : BaseTool
{
	private readonly IToolService tools;
	private readonly IWorkspaceService workspace;

	private bool is_drawing = false;
	private PointD shape_origin;
	private PointD reset_origin;
	private PointD shape_end;
	private RectangleI last_dirty;
	private SelectionHistoryItem? hist;
	private CombineMode combine_mode;

	private readonly MoveHandleBundle handles = new ();
	private HandleType? active_handle;
	private string? active_cursor_name;

	public override Gdk.Key ShortcutKey => Gdk.Key.S;
	protected override bool ShowAntialiasingButton => false;
	public override IEnumerable<MoveHandle> Handles => handles.Enumerate ();

	private enum HandleType
	{
		ResizeNW = 0,
		ResizeSW = 1,
		ResizeNE = 2,
		ResizeSE = 3,
		ResizeW = 4,
		ResizeN = 5,
		ResizeE = 6,
		ResizeS = 7,
	}

	private sealed class MoveHandleBundle
	{
		internal MoveHandle ResizeNW { get; } = new MoveHandle { CursorName = Pinta.Resources.StandardCursors.ResizeNW };
		internal MoveHandle ResizeSW { get; } = new MoveHandle { CursorName = Pinta.Resources.StandardCursors.ResizeSW };
		internal MoveHandle ResizeNE { get; } = new MoveHandle { CursorName = Pinta.Resources.StandardCursors.ResizeNE };
		internal MoveHandle ResizeSE { get; } = new MoveHandle { CursorName = Pinta.Resources.StandardCursors.ResizeSE };
		internal MoveHandle ResizeW { get; } = new MoveHandle { CursorName = Pinta.Resources.StandardCursors.ResizeW };
		internal MoveHandle ResizeN { get; } = new MoveHandle { CursorName = Pinta.Resources.StandardCursors.ResizeN };
		internal MoveHandle ResizeE { get; } = new MoveHandle { CursorName = Pinta.Resources.StandardCursors.ResizeE };
		internal MoveHandle ResizeS { get; } = new MoveHandle { CursorName = Pinta.Resources.StandardCursors.ResizeS };

		internal IEnumerable<MoveHandle> Enumerate ()
		{
			yield return ResizeNW;
			yield return ResizeSW;
			yield return ResizeNE;
			yield return ResizeSE;
			yield return ResizeW;
			yield return ResizeN;
			yield return ResizeE;
			yield return ResizeS;
		}

		/// <returns>
		/// The type of handle represented by the argument
		/// </returns>
		internal HandleType DetermineType (MoveHandle handle)
		{
			if (ReferenceEquals (handle, ResizeNW)) return HandleType.ResizeNW;
			if (ReferenceEquals (handle, ResizeSW)) return HandleType.ResizeSW;
			if (ReferenceEquals (handle, ResizeNE)) return HandleType.ResizeNE;
			if (ReferenceEquals (handle, ResizeSE)) return HandleType.ResizeSE;
			if (ReferenceEquals (handle, ResizeW)) return HandleType.ResizeW;
			if (ReferenceEquals (handle, ResizeN)) return HandleType.ResizeN;
			if (ReferenceEquals (handle, ResizeE)) return HandleType.ResizeE;
			if (ReferenceEquals (handle, ResizeS)) return HandleType.ResizeS;
			throw new ArgumentException ("Handle not found in bundle", nameof (handle));
		}
	}

	public SelectTool (IServiceManager services) : base (services)
	{
		tools = services.GetService<IToolService> ();
		workspace = services.GetService<IWorkspaceService> ();
		workspace.SelectionChanged += AfterSelectionChange;
	}

	protected abstract void DrawShape (Document document, RectangleD r, Layer l);

	protected override void OnBuildToolBar (Box tb)
	{
		base.OnBuildToolBar (tb);

		workspace.SelectionHandler.BuildToolbar (tb, Settings);
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		// Ignore extra button clicks while drawing
		if (is_drawing)
			return;

		hist = new SelectionHistoryItem (Icon, Name);
		hist.TakeSnapshot ();

		reset_origin = e.WindowPoint;
		active_handle = FindHandleIndexUnderPoint (e.WindowPoint);

		if (!active_handle.HasValue) {
			combine_mode = PintaCore.Workspace.SelectionHandler.DetermineCombineMode (e);

			var x = Math.Round (Math.Clamp (e.PointDouble.X, 0, document.ImageSize.Width));
			var y = Math.Round (Math.Clamp (e.PointDouble.Y, 0, document.ImageSize.Height));
			shape_origin = new PointD (x, y);

			document.PreviousSelection = document.Selection.Clone ();
			document.Selection.SelectionPolygons.Clear ();

			// The bottom right corner should be selected.
			active_handle = HandleType.ResizeSE;
		}

		// Do a full redraw for modes that can wipe existing selections outside the rectangle being drawn.
		if (combine_mode == CombineMode.Replace || combine_mode == CombineMode.Intersect) {
			var size = document.ImageSize;
			last_dirty = new RectangleI (0, 0, size.Width, size.Height);
		}

		is_drawing = true;
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (!is_drawing) {
			UpdateCursor (document, e.WindowPoint);
			return;
		}

		var x = Math.Round (Math.Clamp (e.PointDouble.X, 0, document.ImageSize.Width));
		var y = Math.Round (Math.Clamp (e.PointDouble.Y, 0, document.ImageSize.Height));

		// Should always be true, set in OnMouseDown
		if (active_handle.HasValue)
			OnHandleMoved (active_handle.Value, x, y, e.IsShiftPressed);

		var dirty = ReDraw (document);

		UpdateHandlePositions ();

		if (document.Selection != null) {
			SelectionModeHandler.PerformSelectionMode (document, combine_mode, document.Selection.SelectionPolygons);
			document.Workspace.Invalidate (dirty.Union (last_dirty));
		}

		last_dirty = dirty;
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		// If the user didn't move the mouse, they want to deselect
		var tolerance = 0;

		if (Math.Abs (reset_origin.X - e.WindowPoint.X) <= tolerance && Math.Abs (reset_origin.Y - e.WindowPoint.Y) <= tolerance) {
			// Mark as being done interactive drawing before invoking the deselect action.
			// This will allow AfterSelectionChanged() to clear the selection.
			is_drawing = false;

			if (hist != null) {
				// Roll back any changes made to the selection, e.g. in OnMouseDown().
				hist.Undo ();

				hist = null;
			}

			PintaCore.Actions.Edit.Deselect.Activate ();

		} else {
			var dirty = ReDraw (document);

			if (document.Selection != null) {
				SelectionModeHandler.PerformSelectionMode (document, combine_mode, document.Selection.SelectionPolygons);

				document.Selection.Origin = shape_origin;
				document.Selection.End = shape_end;
				document.Workspace.Invalidate (last_dirty.Union (dirty));
				last_dirty = dirty;
			}
			if (hist != null) {
				document.History.PushNewItem (hist);
				hist = null;
			}
		}

		is_drawing = false;
		active_handle = null;

		// Update the mouse cursor.
		UpdateCursor (document, e.WindowPoint);
	}

	protected override void OnActivated (Document? document)
	{
		base.OnActivated (document);

		// When entering the tool, update the selection handles from the
		// document's current selection.
		if (document is not null) {
			LoadFromDocument (document);
		}
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		workspace.SelectionHandler.OnSaveSettings (settings);
	}

	private void OnHandleMoved (HandleType handle, double x, double y, bool shift_pressed)
	{
		switch (handle) {
			case HandleType.ResizeNW:
				shape_origin.X = x;
				shape_origin.Y = y;
				if (shift_pressed) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_origin.X = shape_end.X - shape_end.Y + shape_origin.Y;
					else
						shape_origin.Y = shape_end.Y - shape_end.X + shape_origin.X;
				}
				break;
			case HandleType.ResizeSW:
				shape_origin.X = x;
				shape_end.Y = y;
				if (shift_pressed) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_origin.X = shape_end.X - shape_end.Y + shape_origin.Y;
					else
						shape_end.Y = shape_origin.Y + shape_end.X - shape_origin.X;
				}
				break;
			case HandleType.ResizeNE:
				shape_end.X = x;
				shape_origin.Y = y;
				if (shift_pressed) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_end.X = shape_origin.X + shape_end.Y - shape_origin.Y;
					else
						shape_origin.Y = shape_end.Y - shape_end.X + shape_origin.X;
				}
				break;
			case HandleType.ResizeSE:
				shape_end.X = x;
				shape_end.Y = y;
				if (shift_pressed) {
					if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
						shape_end.X = shape_origin.X + shape_end.Y - shape_origin.Y;
					else
						shape_end.Y = shape_origin.Y + shape_end.X - shape_origin.X;
				}
				break;
			case HandleType.ResizeW:
				shape_origin.X = x;
				if (shift_pressed) {
					var d = shape_end.X - shape_origin.X;
					shape_origin.Y = (shape_origin.Y + shape_end.Y - d) / 2;
					shape_end.Y = (shape_origin.Y + shape_end.Y + d) / 2;
				}
				break;
			case HandleType.ResizeN:
				shape_origin.Y = y;
				if (shift_pressed) {
					var d = shape_end.Y - shape_origin.Y;
					shape_origin.X = (shape_origin.X + shape_end.X - d) / 2;
					shape_end.X = (shape_origin.X + shape_end.X + d) / 2;
				}
				break;
			case HandleType.ResizeE:
				shape_end.X = x;
				if (shift_pressed) {
					var d = shape_end.X - shape_origin.X;
					shape_origin.Y = (shape_origin.Y + shape_end.Y - d) / 2;
					shape_end.Y = (shape_origin.Y + shape_end.Y + d) / 2;
				}
				break;
			case HandleType.ResizeS:
				shape_end.Y = y;
				if (shift_pressed) {
					var d = shape_end.Y - shape_origin.Y;
					shape_origin.X = (shape_origin.X + shape_end.X - d) / 2;
					shape_end.X = (shape_origin.X + shape_end.X + d) / 2;
				}
				break;
			default:
				throw new ArgumentOutOfRangeException (nameof (handle));
		}
	}

	private void UpdateHandlePositions ()
	{
		RectangleI ComputeHandleBounds ()
		{
			// When loading a new document, we might get a selection change event
			// before there is a canvas size / scale.
			if (PintaCore.Workspace.CanvasSize.IsEmpty)
				return RectangleI.Zero;

			return MoveHandle.UnionInvalidateRects (handles.Enumerate ());
		}

		var dirty = ComputeHandleBounds ();
		handles.ResizeNW.CanvasPosition = new PointD (shape_origin.X, shape_origin.Y);
		handles.ResizeSW.CanvasPosition = new PointD (shape_origin.X, shape_end.Y);
		handles.ResizeNE.CanvasPosition = new PointD (shape_end.X, shape_origin.Y);
		handles.ResizeSE.CanvasPosition = new PointD (shape_end.X, shape_end.Y);
		handles.ResizeW.CanvasPosition = new PointD (shape_origin.X, (shape_origin.Y + shape_end.Y) / 2);
		handles.ResizeN.CanvasPosition = new PointD ((shape_origin.X + shape_end.X) / 2, shape_origin.Y);
		handles.ResizeE.CanvasPosition = new PointD (shape_end.X, (shape_origin.Y + shape_end.Y) / 2);
		handles.ResizeS.CanvasPosition = new PointD ((shape_origin.X + shape_end.X) / 2, shape_end.Y);
		dirty = dirty.Union (ComputeHandleBounds ());

		// Repaint at the old and new handle positions.
		PintaCore.Workspace.InvalidateWindowRect (dirty);
	}

	private RectangleI ReDraw (Document document)
	{
		document.Selection.Visible = true;
		ShowHandles (true);

		var rect = CairoExtensions.PointsToRectangle (shape_origin, shape_end);

		DrawShape (document, rect, document.Layers.SelectionLayer);

		// Figure out a bounding box for everything that was drawn, and add a bit of padding.
		var dirty = rect.ToInt ();
		dirty.Inflate (2, 2);
		return dirty;
	}

	private void ShowHandles (bool visible)
	{
		foreach (var handle in handles.Enumerate ())
			handle.Active = visible;
	}

	private MoveHandle? FindHandleUnderPoint (PointD window_point)
	{
		return handles.Enumerate ().FirstOrDefault (c => c.Active && c.ContainsPoint (window_point));
	}

	private HandleType? FindHandleIndexUnderPoint (PointD window_point)
	{
		var handle = FindHandleUnderPoint (window_point);
		if (handle is null) return null;
		return handles.DetermineType (handle);
	}

	private void UpdateCursor (Document document, PointD window_point)
	{
		var active_handle = FindHandleUnderPoint (window_point);
		if (active_handle is not null) {
			SetCursor (Cursor.NewFromName (active_handle.CursorName, null));
			active_cursor_name = active_handle.CursorName;
			return;
		}

		if (active_cursor_name != null) {
			SetCursor (DefaultCursor);
			active_cursor_name = null;
		}
	}

	protected override void OnAfterUndo (Document document)
	{
		base.OnAfterUndo (document);
		LoadFromDocument (document);
	}

	protected override void OnAfterRedo (Document document)
	{
		base.OnAfterRedo (document);
		LoadFromDocument (document);
	}

	private void AfterSelectionChange (object? sender, EventArgs event_args)
	{
		if (is_drawing || !workspace.HasOpenDocuments)
			return;

		// TODO: Try to remove this ActiveDocument call
		LoadFromDocument (workspace.ActiveDocument);
	}

	/// <summary>
	/// Initialize from the document's selection.
	/// </summary>
	private void LoadFromDocument (Document document)
	{
		var selection = document.Selection;
		shape_origin = selection.Origin;
		shape_end = selection.End;
		ShowHandles (document.Selection.Visible);

		if (tools.CurrentTool == this) {
			UpdateHandlePositions ();
			document.Workspace.Invalidate ();
		}
	}
}
