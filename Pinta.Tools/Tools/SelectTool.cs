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

	private readonly MoveHandle[] handles = new MoveHandle[8];
	private int? active_handle;

	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_S);
	protected override bool ShowAntialiasingButton => false;
	public override IEnumerable<MoveHandle> Handles => handles;

	public SelectTool (IServiceProvider services) : base (services)
	{
		tools = services.GetService<IToolService> ();
		workspace = services.GetService<IWorkspaceService> ();

		handles[0] = new MoveHandle { Cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.ResizeNW) };
		handles[1] = new MoveHandle { Cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.ResizeSW) };
		handles[2] = new MoveHandle { Cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.ResizeNE) };
		handles[3] = new MoveHandle { Cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.ResizeSE) };
		handles[4] = new MoveHandle { Cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.ResizeW) };
		handles[5] = new MoveHandle { Cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.ResizeN) };
		handles[6] = new MoveHandle { Cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.ResizeE) };
		handles[7] = new MoveHandle { Cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.ResizeS) };

		workspace.SelectionChanged += AfterSelectionChange;
	}

	protected abstract void DrawShape (Document document, RectangleD r, Layer l);

	protected override void OnBuildToolBar (Gtk.Box tb)
	{
		base.OnBuildToolBar (tb);
		workspace.SelectionHandler.BuildToolbar (tb, Settings);
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		// Ignore extra button clicks while drawing
		if (is_drawing)
			return;

		hist = new SelectionHistoryItem (workspace, Icon, Name);
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
			active_handle = 3;
		}

		// Do a full redraw for modes that can wipe existing selections outside the rectangle being drawn.
		if (combine_mode == CombineMode.Replace || combine_mode == CombineMode.Intersect) {
			Size size = document.ImageSize;
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

		// Should always be true, set in OnMouseDown
		if (active_handle.HasValue) {

			PointD p = new (
				X: Math.Round (Math.Clamp (e.PointDouble.X, 0, document.ImageSize.Width)),
				Y: Math.Round (Math.Clamp (e.PointDouble.Y, 0, document.ImageSize.Height)));

			OnHandleMoved (
				active_handle.Value,
				p.X,
				p.Y,
				e.IsShiftPressed);
		}

		RectangleI dirty = ReDraw (document);

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
		const int TOLERANCE = 0;

		if (Math.Abs (reset_origin.X - e.WindowPoint.X) <= TOLERANCE && Math.Abs (reset_origin.Y - e.WindowPoint.Y) <= TOLERANCE) {
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
			RectangleI dirty = ReDraw (document);

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
		if (document is null) return;

		LoadFromDocument (document);
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		workspace.SelectionHandler.OnSaveSettings (settings);
	}

	private void OnHandleMoved (int handle, double x, double y, bool shift_pressed)
	{
		switch (handle) {

			case 0:
				shape_origin = new (x, y);

				if (!shift_pressed) return;

				shape_origin =
					(shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
					? (shape_origin with { X = shape_end.X - shape_end.Y + shape_origin.Y })
					: (shape_origin with { Y = shape_end.Y - shape_end.X + shape_origin.X });

				return;

			case 1:
				shape_origin = shape_origin with { X = x };
				shape_end = shape_end with { Y = y };

				if (!shift_pressed) return;

				if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
					shape_origin = shape_origin with { X = shape_end.X - shape_end.Y + shape_origin.Y };
				else
					shape_end = shape_end with { Y = shape_origin.Y + shape_end.X - shape_origin.X };

				return;

			case 2:
				shape_end = shape_end with { X = x };
				shape_origin = shape_origin with { Y = y };

				if (!shift_pressed) return;

				if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
					shape_end = shape_end with { X = shape_origin.X + shape_end.Y - shape_origin.Y };
				else
					shape_origin = shape_origin with { Y = shape_end.Y - shape_end.X + shape_origin.X };

				return;

			case 3:
				shape_end = new (x, y);

				if (!shift_pressed)
					return;

				if (shape_end.X - shape_origin.X <= shape_end.Y - shape_origin.Y)
					shape_end = shape_end with { X = shape_origin.X + shape_end.Y - shape_origin.Y };
				else
					shape_end = shape_end with { Y = shape_origin.Y + shape_end.X - shape_origin.X };

				return;

			case 4:
				shape_origin = shape_origin with { X = x };

				if (!shift_pressed) return;

				double d4 = shape_end.X - shape_origin.X;
				shape_origin = shape_origin with { Y = (shape_origin.Y + shape_end.Y - d4) / 2 };
				shape_end = shape_end with { Y = (shape_origin.Y + shape_end.Y + d4) / 2 };

				return;

			case 5:
				shape_origin = shape_origin with { Y = y };

				if (!shift_pressed) return;

				double d5 = shape_end.Y - shape_origin.Y;
				shape_origin = shape_origin with { X = (shape_origin.X + shape_end.X - d5) / 2 };
				shape_end = shape_end with { X = (shape_origin.X + shape_end.X + d5) / 2 };

				return;

			case 6:
				shape_end = shape_end with { X = x };

				if (!shift_pressed) return;

				double d6 = shape_end.X - shape_origin.X;
				shape_origin = shape_origin with { Y = (shape_origin.Y + shape_end.Y - d6) / 2 };
				shape_end = shape_end with { Y = (shape_origin.Y + shape_end.Y + d6) / 2 };

				return;

			case 7:
				shape_end = shape_end with { Y = y };

				if (!shift_pressed) return;

				double d7 = shape_end.Y - shape_origin.Y;
				shape_origin = shape_origin with { X = (shape_origin.X + shape_end.X - d7) / 2 };
				shape_end = shape_end with { X = (shape_origin.X + shape_end.X + d7) / 2 };

				return;

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

			return MoveHandle.UnionInvalidateRects (handles);
		}

		var dirty = ComputeHandleBounds ();
		handles[0].CanvasPosition = new PointD (shape_origin.X, shape_origin.Y);
		handles[1].CanvasPosition = new PointD (shape_origin.X, shape_end.Y);
		handles[2].CanvasPosition = new PointD (shape_end.X, shape_origin.Y);
		handles[3].CanvasPosition = new PointD (shape_end.X, shape_end.Y);
		handles[4].CanvasPosition = new PointD (shape_origin.X, (shape_origin.Y + shape_end.Y) / 2);
		handles[5].CanvasPosition = new PointD ((shape_origin.X + shape_end.X) / 2, shape_origin.Y);
		handles[6].CanvasPosition = new PointD (shape_end.X, (shape_origin.Y + shape_end.Y) / 2);
		handles[7].CanvasPosition = new PointD ((shape_origin.X + shape_end.X) / 2, shape_end.Y);
		dirty = dirty.Union (ComputeHandleBounds ());

		// Repaint at the old and new handle positions.
		PintaCore.Workspace.InvalidateWindowRect (dirty);
	}

	private RectangleI ReDraw (Document document)
	{
		document.Selection.Visible = true;
		ShowHandles (true);

		RectangleD rect = CairoExtensions.PointsToRectangle (shape_origin, shape_end);

		DrawShape (document, rect, document.Layers.SelectionLayer);

		// Figure out a bounding box for everything that was drawn, and add a bit of padding.
		RectangleI dirty = rect.ToInt ();
		dirty = dirty.Inflated (2, 2);
		return dirty;
	}

	private void ShowHandles (bool visible)
	{
		foreach (var handle in handles)
			handle.Active = visible;
	}

	private MoveHandle? FindHandleUnderPoint (PointD window_point)
	{
		return handles.FirstOrDefault (c => c.Active && c.ContainsPoint (window_point));
	}

	private int? FindHandleIndexUnderPoint (PointD window_point)
	{
		var handle = FindHandleUnderPoint (window_point);
		if (handle is not null)
			return Array.IndexOf (handles, handle);
		else
			return null;
	}

	private void UpdateCursor (Document document, PointD window_point)
	{
		var active_handle = FindHandleUnderPoint (window_point);
		if (active_handle is not null) {
			SetCursor (active_handle.Cursor);
			return;
		}

		if (CurrentCursor != DefaultCursor)
			SetCursor (DefaultCursor);
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

		if (tools.CurrentTool != this) return;

		UpdateHandlePositions ();
		document.Workspace.Invalidate ();
	}
}
