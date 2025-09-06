//
// LassoSelectTool.cs
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
using Cairo;
using ClipperLib;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class LassoSelectTool : BaseTool
{
	private readonly IWorkspaceService workspace;

	private bool is_dragging = false;
	private CombineMode combine_mode;
	private SelectionHistoryItem? hist;

	private Path? path;
	private readonly List<IntPoint> lasso_polygon = [];

	private Separator? mode_sep;
	private Label? lasso_mode_label;
	private ToolBarDropDownButton? lasso_mode_buttom;

	public LassoSelectTool (IServiceProvider services) : base (services)
	{
		workspace = services.GetService<IWorkspaceService> ();
	}

	public override string Name => Translations.GetString ("Lasso Select");
	public override string Icon => Pinta.Resources.Icons.ToolSelectLasso;
	public override string StatusBarText => Translations.GetString ("On Freeform mode, click and drag to draw the outline for a selection area." +
									"\n\nOn Polygon mode, click and drag to add a new point to the selection." +
									"\nPress Enter to finish the selection" +
									"\nPress Backspace to delete the last point");
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_S);
	public override Gdk.Cursor DefaultCursor => Gdk.Cursor.NewFromTexture (Resources.GetIcon ("Cursor.LassoSelect.png"), 9, 18, null);
	public override int Priority => 17;
	public override bool IsSelectionTool => true;

	private bool IsPolygonMode => LassoModeButtom.SelectedItem.GetTagOrDefault (false);
	private bool IsFreeformMode => !IsPolygonMode;

	protected override void OnBuildToolBar (Gtk.Box tb)
	{
		base.OnBuildToolBar (tb);
		workspace.SelectionHandler.BuildToolbar (tb, Settings);

		tb.Append (Separator);
		tb.Append (LassoModeLabel);
		tb.Append (LassoModeButtom);
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		if (is_dragging)
			return;

		is_dragging = true;

		if (lasso_polygon.Count == 0) {
			hist = new SelectionHistoryItem (workspace, Icon, Name);
			hist.TakeSnapshot ();

			combine_mode = workspace.SelectionHandler.DetermineCombineMode (e);
			path = null;

			document.PreviousSelection = document.Selection.Clone ();
		}

		if (IsPolygonMode) {
			PointD p = document.ClampToImageSize (e.PointDouble);

			lasso_polygon.Add (new IntPoint ((long) p.X, (long) p.Y));

			ApplySelection (document);
		}

	}

	private void ApplySelection (Document document)
	{
		document.Selection.SelectionPolygons.Clear ();
		document.Selection.SelectionPolygons.Add ([.. lasso_polygon]);

		SelectionModeHandler.PerformSelectionMode (
			document,
			combine_mode,
			document.Selection.SelectionPolygons);

		document.Workspace.Invalidate ();
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (!is_dragging)
			return;

		PointD p = document.ClampToImageSize (e.PointDouble);

		if (IsFreeformMode) {
			lasso_polygon.Add (new IntPoint ((long) p.X, (long) p.Y));

			ApplySelection (document);
			return;
		}

		if (lasso_polygon.Count == 0)
			return;

		lasso_polygon[lasso_polygon.Count - 1] = new IntPoint (p.X, p.Y);

		ApplySelection (document);
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		if (IsFreeformMode) {
			ApplySelection (document);

			FinalizeShape (document);
		}
		is_dragging = false;
	}

	private void FinalizeShape (Document document)
	{
		if (hist != null) {
			if (lasso_polygon.Count > 1)
				document.History.PushNewItem (hist);
			hist = null;
		}
		lasso_polygon.Clear ();
	}

	protected override void OnDeactivated (Document? document, BaseTool? newTool)
	{
		if (document != null)
			FinalizeShape (document);
	}

	protected override bool OnKeyDown (Document document, ToolKeyEventArgs e)
	{
		if (IsPolygonMode) {
			switch (e.Key.Value) {
				case Gdk.Constants.KEY_Return:
					FinalizeShape (document);
					return true;
				case Gdk.Constants.KEY_BackSpace:
					Backtrack (document);
					return true;
			}
		}

		return base.OnKeyDown (document, e);
	}

	private void Backtrack (Document document)
	{
		if (lasso_polygon.Count == 0) {
			return;
		}

		lasso_polygon.RemoveAt (lasso_polygon.Count - 1);

		if (lasso_polygon.Count == 0) {
			hist?.Undo ();
			return;
		}

		ApplySelection (document);
	}

	private Separator Separator => mode_sep ??= GtkExtensions.CreateToolBarSeparator ();
	private Label LassoModeLabel => lasso_mode_label ??= Label.New (string.Format (" {0}: ", Translations.GetString ("Lasso Mode")));

	private ToolBarDropDownButton LassoModeButtom {
		get {
			if (lasso_mode_buttom is null) {
				lasso_mode_buttom = new ToolBarDropDownButton ();

				lasso_mode_buttom.AddItem (Translations.GetString ("Freeform"), Pinta.Resources.Icons.ToolFreeformShape, false);
				lasso_mode_buttom.AddItem (Translations.GetString ("Polygon"), Pinta.Resources.Icons.LassoPolygon, true);

			}

			return lasso_mode_buttom;
		}
	}
}
