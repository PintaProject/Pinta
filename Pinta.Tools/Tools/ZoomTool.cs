//
// ZoomTool.cs
//
// Author:
//       Olivier Dufour <olivier.duff@gmail.com>
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
using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class ZoomTool : BaseTool
{
	private readonly Gdk.Cursor cursor_zoom_in;
	private readonly Gdk.Cursor cursor_zoom_out;
	private readonly Gdk.Cursor cursor_zoom;
	private readonly Gdk.Cursor cursor_zoom_pan;

	private MouseButton mouse_down;
	private bool is_drawing;
	private PointD shape_origin;
	private RectangleD last_dirty;
	private static readonly int tolerance_squared = 100;

	public ZoomTool (IServiceProvider services) : base (services)
	{
		mouse_down = MouseButton.None;

		cursor_zoom_in = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.ZoomIn);
		cursor_zoom_out = GdkExtensions.CursorFromName (Pinta.Resources.StandardCursors.ZoomOut);
		cursor_zoom = Gdk.Cursor.NewFromTexture (Resources.GetIcon (Pinta.Resources.Icons.ToolZoom), 0, 0, null);
		cursor_zoom_pan = Gdk.Cursor.NewFromTexture (Resources.GetIcon (Pinta.Resources.Icons.ToolPan), 0, 0, null);
	}

	public override string Name => Translations.GetString ("Zoom");
	public override string Icon => Pinta.Resources.Icons.ToolZoom;
	public override string StatusBarText => Translations.GetString (
		"Left click to zoom in." +
		"\nRight click to zoom out." +
		"\nClick and drag to zoom in selection.");
	public override Gdk.Cursor DefaultCursor => cursor_zoom;
	public override Gdk.Key ShortcutKey => new (Gdk.Constants.KEY_Z);
	public override int Priority => 9;

	protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
	{
		// If we are already tracking, ignore any additional mouse down events
		if (mouse_down != MouseButton.None)
			return;

		shape_origin = e.PointDouble;

		switch (e.MouseButton) {
			case MouseButton.Left:
				SetCursor (cursor_zoom_in);
				break;

			case MouseButton.Middle:
				SetCursor (cursor_zoom_pan);
				break;

			case MouseButton.Right:
				SetCursor (cursor_zoom_out);
				break;
		}

		mouse_down = e.MouseButton;
	}

	protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
	{
		switch (mouse_down) {
			case MouseButton.Left:
				OnMouseMove_LeftPressed (document, e);
				break;
			case MouseButton.Middle:
				OnMouseMove_MiddlePressed (document, e);
				break;
		}
	}

	private void OnMouseMove_MiddlePressed (Document document, ToolMouseEventArgs e)
	{
		PointI delta =
			(shape_origin - e.PointDouble)
			.Scaled (document.Workspace.Scale)
			.ToInt ();

		document.Workspace.ScrollCanvas (delta);
	}

	private void OnMouseMove_LeftPressed (Document document, ToolMouseEventArgs e)
	{
		var shape_origin_window = document.Workspace.CanvasPointToView (shape_origin);
		if (shape_origin_window.DistanceSquared (e.WindowPoint) > tolerance_squared) // if they've moved the mouse more than 10 pixels since they clicked
			is_drawing = true;

		//still draw rectangle after we have draw it one time...
		UpdateRectangle (document, e.PointDouble);
	}

	protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
	{
		document.Layers.ToolLayer.Hidden = true;

		if (mouse_down == MouseButton.Left || mouse_down == MouseButton.Right) {
			if (e.MouseButton == MouseButton.Left) {
				PointD shapeOriginWindow = document.Workspace.CanvasPointToView (shape_origin);
				if (shapeOriginWindow.DistanceSquared (e.WindowPoint) <= tolerance_squared) {
					document.Workspace.ZoomInAroundCanvasPoint (e.PointDouble);
				} else {
					document.Workspace.ZoomToCanvasRectangle (RectangleD.FromPoints (shape_origin, e.PointDouble));
				}
			} else {
				document.Workspace.ZoomOutAroundCanvasPoint (e.PointDouble);
			}
		}

		mouse_down = MouseButton.None;

		is_drawing = false;

		SetCursor (cursor_zoom);//restore regular cursor
	}

	private void UpdateRectangle (Document document, PointD point)
	{
		if (!is_drawing)
			return;

		RectangleD r = RectangleD.FromPoints (shape_origin.Rounded (), point.Rounded ());

		document.Layers.ToolLayer.Clear ();
		document.Layers.ToolLayer.Hidden = false;

		using Context g = new (document.Layers.ToolLayer.Surface);

		RectangleD dirty = g.FillRectangle (r, new Color (0.7, 0.8, 0.9, 0.4));

		document.Workspace.Invalidate (last_dirty.ToInt ());
		document.Workspace.Invalidate (dirty.ToInt ());

		last_dirty = dirty;
	}
}
