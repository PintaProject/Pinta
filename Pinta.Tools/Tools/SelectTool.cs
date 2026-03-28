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
using System.Collections.Immutable;
using System.Linq;
using Pinta.Core;

namespace Pinta.Tools;

public abstract class SelectTool : BaseTool
{
	private readonly IToolService tools;
	private readonly IWorkspaceService workspace;

	private SelectionHistoryItem? hist = default;
	private CombineMode combine_mode = default;

	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_S);
	public override bool IsSelectionTool => true;
	protected override bool ShowAntialiasingButton => false;
	private readonly RectangleHandle handle;
	public override IEnumerable<IToolHandle> Handles => [handle];

	public SelectTool (IServiceProvider services) : base (services)
	{
		tools = services.GetService<IToolService> ();
		workspace = services.GetService<IWorkspaceService> ();

		handle = new (workspace) { InvertIfNegative = true };

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
		if (handle.IsDragging)
			return;

		hist = new SelectionHistoryItem (workspace, Icon, Name);
		hist.TakeSnapshot ();

		if (!handle.BeginDrag (e.PointDouble, document.ImageSize)) {
			// Start drawing a new rectangle.
			combine_mode = PintaCore.Workspace.SelectionHandler.DetermineCombineMode (e);

			double x = Math.Round (Math.Clamp (e.PointDouble.X, 0, document.ImageSize.Width));
			double y = Math.Round (Math.Clamp (e.PointDouble.Y, 0, document.ImageSize.Height));
			handle.Rectangle = new (x, y, 0.0, 0.0);

			document.PreviousSelection = document.Selection.Clone ();
			document.Selection.SelectionPolygons.Clear ();

			if (!handle.BeginDrag (new PointD (x, y), document.ImageSize))
				throw new InvalidOperationException ("Should be able to start drawing a new rectangle!");
		}
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (!handle.IsDragging) {
			UpdateCursor (e.WindowPoint);
			return;
		}

		handle.UpdateDrag (e.PointDouble, e.IsShiftPressed);

		ReDraw (document);

		SelectionModeHandler.PerformSelectionMode (document, combine_mode, document.Selection.SelectionPolygons);
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		if (handle.HasDragged (e.PointDouble)) {
			ReDraw (document);

			SelectionModeHandler.PerformSelectionMode (document, combine_mode, document.Selection.SelectionPolygons);

			document.Selection.HandleBounds = handle.Rectangle;

			if (hist != null) {
				document.History.PushNewItem (hist);
				hist = null;
			}

			handle.EndDrag ();
		} else {
			// If the user didn't move the mouse, they want to deselect

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
		if (document is null) return;

		LoadFromDocument (document);
	}

	protected override void OnSaveSettings (ISettingsService settings)
	{
		base.OnSaveSettings (settings);

		workspace.SelectionHandler.OnSaveSettings (settings);
	}

	private void ReDraw (Document document)
	{
		document.Selection.Visible = true;

		ShowHandles (true);

		RectangleD rect = handle.Rectangle;
		DrawShape (document, rect, document.Layers.SelectionLayer);
	}

	private void ShowHandles (bool visible)
	{
		handle.Active = visible;
	}

	private void UpdateCursor (PointD viewPos)
	{
		Gdk.Cursor? cursor = handle.Active ?
			handle.GetCursorAtPoint (viewPos) :
			null;

		SetCursor (cursor ?? DefaultCursor);
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
		DocumentSelection selection = document.Selection;
		handle.Rectangle = selection.HandleBounds;
		ShowHandles (document.Selection.Visible && tools.CurrentTool == this);
	}
}
