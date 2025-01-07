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
using Pinta.Tools.Handles;

namespace Pinta.Tools;

public abstract class SelectTool : BaseTool
{
	private readonly IToolService tools;
	private readonly IWorkspaceService workspace;

	private RectangleI last_dirty;
	private SelectionHistoryItem? hist;
	private CombineMode combine_mode;

	private readonly RectangleHandle handle = new () { InvertIfNegative = true };
	public override IEnumerable<IToolHandle> Handles => Enumerable.Repeat (handle, 1);

	public override Gdk.Key ShortcutKey => Gdk.Key.S;
	protected override bool ShowAntialiasingButton => false;

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
		if (handle.IsDragging)
			return;

		hist = new SelectionHistoryItem (Icon, Name);
		hist.TakeSnapshot ();

		if (!handle.BeginDrag (e.PointDouble, document.ImageSize)) {
			// Start drawing a new rectangle.
			combine_mode = PintaCore.Workspace.SelectionHandler.DetermineCombineMode (e);

			var x = Math.Round (Math.Clamp (e.PointDouble.X, 0, document.ImageSize.Width));
			var y = Math.Round (Math.Clamp (e.PointDouble.Y, 0, document.ImageSize.Height));
			handle.Rectangle = new (x, y, 0.0, 0.0);

			document.PreviousSelection = document.Selection.Clone ();
			document.Selection.SelectionPolygons.Clear ();

			if (!handle.BeginDrag (new PointD (x, y), document.ImageSize))
				throw new Exception ("Should be able to start dragging a new rectangle");
		}

		// Do a full redraw for modes that can wipe existing selections outside the rectangle being drawn.
		if (combine_mode == CombineMode.Replace || combine_mode == CombineMode.Intersect) {
			var size = document.ImageSize;
			last_dirty = new RectangleI (0, 0, size.Width, size.Height);
		}

	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (!handle.IsDragging) {
			UpdateCursor (e.WindowPoint);
			return;
		}

		var dirty = handle.UpdateDrag (e.PointDouble, e.IsShiftPressed ? ConstrainType.Square : ConstrainType.None);
		PintaCore.Workspace.InvalidateWindowRect (dirty);

		dirty = ReDraw (document);

		if (document.Selection != null) {
			SelectionModeHandler.PerformSelectionMode (document, combine_mode, document.Selection.SelectionPolygons);
			document.Workspace.Invalidate (dirty.Union (last_dirty));
		}

		last_dirty = dirty;
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		if (handle.HasDragged (e.PointDouble)) {
			var dirty = ReDraw (document);

			if (document.Selection != null) {
				SelectionModeHandler.PerformSelectionMode (document, combine_mode, document.Selection.SelectionPolygons);

				document.Selection.Origin = handle.Rectangle.Location ();
				document.Selection.End = handle.Rectangle.EndLocation ();
				document.Workspace.Invalidate (last_dirty.Union (dirty));
				last_dirty = dirty;
			}
			if (hist != null) {
				document.History.PushNewItem (hist);
				hist = null;
			}

			handle.EndDrag ();
		} else {
			// Mark as being done interactive drawing before invoking the deselect action.
			// This will allow AfterSelectionChanged() to clear the selection.
			handle.EndDrag ();

			if (hist != null) {
				// Roll back any changes made to the selection, e.g. in OnMouseDown().
				hist.Undo ();
				hist = null;
			}

			PintaCore.Actions.Edit.Deselect.Activate ();
		}

		// Update the mouse cursor.
		UpdateCursor (e.WindowPoint);
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

	private RectangleI ReDraw (Document document)
	{
		document.Selection.Visible = true;
		ShowHandles (true);

		RectangleD rect = handle.Rectangle;

		DrawShape (document, rect, document.Layers.SelectionLayer);

		// Figure out a bounding box for everything that was drawn, and add a bit of padding.
		var dirty = rect.ToInt ();
		dirty = dirty.Inflated (2, 2);
		return dirty;
	}

	private void ShowHandles (bool visible)
	{
		handle.Active = visible;
	}

	private void UpdateCursor (in PointD view_pos)
	{
		string? cursor_name = null;

		if (handle.Active)
			cursor_name = handle.GetCursorAtPoint (view_pos);

		if (cursor_name is not null) {
			SetCursor (Cursor.NewFromName (cursor_name, null));
		} else {
			SetCursor (DefaultCursor);
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
		if (handle.IsDragging || !workspace.HasOpenDocuments)
			return;

		// TODO: Try to remove this ActiveDocument call
		LoadFromDocument (workspace.ActiveDocument);
	}

	/// <summary>
	/// Initialize from the document's selection.
	/// </summary>
	private void LoadFromDocument (Document document)
	{
		handle.Rectangle = document.Selection.SelectionPath.GetBounds ().ToDouble ();
		ShowHandles (document.Selection.Visible && tools.CurrentTool == this);
	}
}
