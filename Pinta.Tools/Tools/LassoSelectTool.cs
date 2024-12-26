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
using System.Linq;
using Cairo;
using ClipperLib;
using Gtk;
using Pinta.Core;

namespace Pinta.Tools;

public class LassoSelectTool : BaseTool
{
	private readonly IWorkspaceService workspace;

	private bool is_drawing = false;
	private CombineMode combine_mode;
	private SelectionHistoryItem? hist;

	private Path? path;
	private readonly List<IntPoint> lasso_polygon = new ();

	public LassoSelectTool (IServiceProvider services) : base (services)
	{
		workspace = services.GetService<IWorkspaceService> ();
	}

	public override string Name => Translations.GetString ("Lasso Select");
	public override string Icon => Pinta.Resources.Icons.ToolSelectLasso;
	public override string StatusBarText => Translations.GetString ("Click and drag to draw the outline for a selection area.");
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_S);
	public override Gdk.Cursor DefaultCursor => Gdk.Cursor.NewFromTexture (Resources.GetIcon ("Cursor.LassoSelect.png"), 9, 18, null);
	public override int Priority => 17;

	protected override void OnBuildToolBar (Box tb)
	{
		base.OnBuildToolBar (tb);

		workspace.SelectionHandler.BuildToolbar (tb, Settings);
	}

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		if (is_drawing)
			return;

		hist = new SelectionHistoryItem (Icon, Name);
		hist.TakeSnapshot ();

		combine_mode = workspace.SelectionHandler.DetermineCombineMode (e);
		path = null;
		is_drawing = true;

		document.PreviousSelection = document.Selection.Clone ();
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (!is_drawing)
			return;

		var p = document.ClampToImageSize (e.PointDouble);

		document.Selection.Visible = true;

		var surf = document.Layers.SelectionLayer.Surface;

		using Context g = new (surf) {
			Antialias = Antialias.Subpixel
		};

		if (path != null) {
			g.AppendPath (path);
		} else {
			g.MoveTo (p.X, p.Y);
		}

		g.LineTo (p.X, p.Y);
		lasso_polygon.Add (new IntPoint ((long) p.X, (long) p.Y));

		path = g.CopyPath ();

		g.FillRule = FillRule.EvenOdd;
		g.ClosePath ();

		document.Selection.SelectionPolygons.Clear ();
		document.Selection.SelectionPolygons.Add (lasso_polygon.ToList ());

		SelectionModeHandler.PerformSelectionMode (document, combine_mode, document.Selection.SelectionPolygons);

		document.Workspace.Invalidate ();
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		var surf = document.Layers.SelectionLayer.Surface;

		using Context g = new (surf);
		if (path != null) {
			g.AppendPath (path);
			path = null;
		}

		g.FillRule = FillRule.EvenOdd;
		g.ClosePath ();

		document.Selection.SelectionPolygons.Clear ();
		document.Selection.SelectionPolygons.Add (lasso_polygon.ToList ());
		SelectionModeHandler.PerformSelectionMode (document, combine_mode, document.Selection.SelectionPolygons);
		document.Workspace.Invalidate ();

		if (hist != null) {
			document.History.PushNewItem (hist);
			hist = null;
		}

		lasso_polygon.Clear ();
		is_drawing = false;
	}
}
