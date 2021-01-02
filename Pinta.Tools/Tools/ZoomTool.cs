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

namespace Pinta.Tools
{
	public class ZoomTool : BaseTool
	{
		private Gdk.Cursor cursorZoomIn;
		private Gdk.Cursor cursorZoomOut;
		private Gdk.Cursor cursorZoom;
		private Gdk.Cursor cursorZoomPan;

		private MouseButton mouseDown;
		private bool is_drawing;
		protected PointD shape_origin;
		private Rectangle last_dirty;
		private static readonly int tolerance = 10;

		public ZoomTool (IServiceManager services) : base (services)
		{
			mouseDown = 0;

			cursorZoomIn = new Gdk.Cursor (Gdk.Display.Default, Gtk.IconTheme.Default.LoadIcon (Pinta.Resources.Icons.ViewZoomIn, 16), 0, 0);
			cursorZoomOut = new Gdk.Cursor (Gdk.Display.Default, Gtk.IconTheme.Default.LoadIcon (Pinta.Resources.Icons.ViewZoomOut, 16), 0, 0);
			cursorZoom = new Gdk.Cursor (Gdk.Display.Default, Gtk.IconTheme.Default.LoadIcon (Pinta.Resources.Icons.ToolZoom, 16), 0, 0);
			cursorZoomPan = new Gdk.Cursor (Gdk.Display.Default, Gtk.IconTheme.Default.LoadIcon (Pinta.Resources.Icons.ToolPan, 16), 0, 0);
		}

		public override string Name => Translations.GetString ("Zoom");
		public override string Icon => Pinta.Resources.Icons.ToolZoom;
		public override string StatusBarText => Translations.GetString ("Left click to zoom in. Right click to zoom out. Click and drag to zoom in selection.");
		public override Gdk.Cursor DefaultCursor => cursorZoom;
		public override Gdk.Key ShortcutKey => Gdk.Key.Z;
		public override int Priority => 9;

		protected override void OnMouseDown (Document document, ToolMouseEventArgs e)
		{
			// If we are already tracking, ignore any additional mouse down events
			if (mouseDown != MouseButton.None)
				return;

			shape_origin = e.PointDouble;

			switch (e.MouseButton) {
				case MouseButton.Left:
					SetCursor (cursorZoomIn);
					break;

				case MouseButton.Middle:
					SetCursor (cursorZoomPan);
					break;

				case MouseButton.Right:
					SetCursor (cursorZoomOut);
					break;
			}

			mouseDown = e.MouseButton;
		}

		protected override void OnMouseMove (Document document, ToolMouseEventArgs e)
		{
			if (mouseDown == MouseButton.Left) {
				if (Math.Abs (shape_origin.X - e.PointDouble.X) > tolerance || Math.Abs (shape_origin.Y - e.PointDouble.Y) > tolerance)  // if they've moved the mouse more than 10 pixels since they clicked
					is_drawing = true;

				//still draw rectangle after we have draw it one time...
				UpdateRectangle (document, e.PointDouble);
			} else if (mouseDown == MouseButton.Middle) {
				document.Workspace.ScrollCanvas ((int) ((shape_origin.X - e.PointDouble.X) * document.Workspace.Scale), (int) ((shape_origin.Y - e.PointDouble.Y) * document.Workspace.Scale));
			}
		}

		protected override void OnMouseUp (Document document, ToolMouseEventArgs e)
		{
			var x = e.PointDouble.X;
			var y = e.PointDouble.Y;
			document.Layers.ToolLayer.Hidden = true;

			if (mouseDown == MouseButton.Left || mouseDown == MouseButton.Right) {
				if (e.MouseButton == MouseButton.Left) {
					if (Math.Abs (shape_origin.X - x) <= tolerance && Math.Abs (shape_origin.Y - y) <= tolerance) {
						document.Workspace.ZoomIn ();
						document.Workspace.RecenterView (x, y);
					} else {
						document.Workspace.ZoomToRectangle (CairoExtensions.PointsToRectangle (shape_origin, e.PointDouble));
					}
				} else {
					document.Workspace.ZoomOut ();
					document.Workspace.RecenterView (x, y);
				}
			}

			mouseDown = MouseButton.None;

			is_drawing = false;

			SetCursor (cursorZoom);//restore regular cursor
		}

		private void UpdateRectangle (Document document, PointD point)
		{
			if (!is_drawing)
				return;

			var r = CairoExtensions.PointsToRectangle (shape_origin, point);

			document.Layers.ToolLayer.Clear ();
			document.Layers.ToolLayer.Hidden = false;

			using (var g = new Context (document.Layers.ToolLayer.Surface)) {
				g.Antialias = Antialias.Subpixel;

				var dirty = g.FillStrokedRectangle (r, new Color (0, 0.4, 0.8, 0.1), new Color (0, 0, 0.9), 1);
				dirty = dirty.Clamp ();

				document.Workspace.Invalidate (last_dirty.ToGdkRectangle ());
				document.Workspace.Invalidate (dirty.ToGdkRectangle ());

				last_dirty = dirty;
			}
		}
	}
}
