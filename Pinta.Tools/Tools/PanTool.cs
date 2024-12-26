// 
// PanTool.cs
//  
// Author:
//       Olivier Dufour
// 
// Copyright (c) 2010 Olivier Dufour
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
using Pinta.Core;

namespace Pinta.Tools;

public sealed class PanTool : BaseTool
{
	private bool active;
	private PointD last_point;
	private readonly Gdk.Cursor grabbing_cursor = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.Grabbing);

	public PanTool (IServiceProvider services) : base (services) { }

	public override string Name => Translations.GetString ("Pan");
	public override string Icon => Pinta.Resources.Icons.ToolPan;
	public override string StatusBarText => Translations.GetString ("Click and drag to navigate image.");
	public override Gdk.Cursor DefaultCursor => GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.Grab);
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_H);
	public override int Priority => 11;

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		// If we are already panning, ignore any additional mouse down events
		if (active)
			return;

		// Don't scroll if the whole canvas fits (no scrollbars)
		if (!document.Workspace.ImageViewFitsInWindow)
			active = true;

		last_point = new PointD (e.RootPoint.X, e.RootPoint.Y);
		SetCursor (grabbing_cursor);
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		if (!active)
			return;

		PointI delta = (last_point - e.RootPoint).ToInt ();

		document.Workspace.ScrollCanvas (delta);

		last_point = new PointD (e.RootPoint.X, e.RootPoint.Y);
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		active = false;
		SetCursor (DefaultCursor);
	}
}
