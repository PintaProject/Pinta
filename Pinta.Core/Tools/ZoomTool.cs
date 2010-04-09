// 
// ZoomTool.cs
//  
// Author:
//       dufoli <${AuthorEmail}>
// 
// Copyright (c) 2010 dufoli
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

namespace Pinta.Core
{
	public class ZoomTool : BaseTool
	{
		private Gdk.Cursor cursorZoomIn;
		private Gdk.Cursor cursorZoomOut;
		private Gdk.Cursor cursorZoom;
		private Gdk.Cursor cursorZoomPan;

		private uint mouseDown;
		private bool is_drawing;
		protected PointD shape_origin;
		private Rectangle last_dirty;
		private static readonly int tolerance = 10;

		public override string Name {
			get { return "Zoom"; }
		}
		public override string Icon {
			get { return "Tools.Zoom.png"; }
		}
		public override string StatusBarText {
			get { return "Click left to zoom in. Click right to zoom out. Click and drag to zoom in selection."; }
		}
		public override Gdk.Cursor DefaultCursor {
			get { return cursorZoom; }
		}
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.Z; } }

		public ZoomTool ()
		{
			this.mouseDown = 0;

			cursorZoomIn = new Gdk.Cursor (PintaCore.Chrome.DrawingArea.Display, PintaCore.Resources.GetIcon ("Menu.View.ZoomIn.png"), 0, 0);
			cursorZoomOut = new Gdk.Cursor (PintaCore.Chrome.DrawingArea.Display, PintaCore.Resources.GetIcon ("Menu.View.ZoomOut.png"), 0, 0);
			cursorZoom = new Gdk.Cursor (PintaCore.Chrome.DrawingArea.Display, PintaCore.Resources.GetIcon ("Tools.Zoom.png"), 0, 0);
			cursorZoomPan = new Gdk.Cursor (PintaCore.Chrome.DrawingArea.Display, PintaCore.Resources.GetIcon ("Tools.Pan.png"), 0, 0);
		}

		protected void UpdateRectangle (PointD point)
		{
			if (!is_drawing)
				return;

			Rectangle r = PointsToRectangle (shape_origin, point);
			Rectangle dirty;

			PintaCore.Layers.ToolLayer.Clear ();
			PintaCore.Layers.ToolLayer.Hidden = false;

			using (Context g = new Context (PintaCore.Layers.ToolLayer.Surface)) {
				g.Antialias = Antialias.Subpixel;

				dirty = g.FillStrokedRectangle (r, new Color (0, 0.4, 0.8, 0.1), new Color (0, 0, 0.9), 1);
				dirty = dirty.Clamp ();

				PintaCore.Workspace.Invalidate (last_dirty.ToGdkRectangle ());
				PintaCore.Workspace.Invalidate (dirty.ToGdkRectangle ());
				
				last_dirty = dirty;
			}
		}

		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			shape_origin = point;

			switch (args.Event.Button) {
				case 1://left
					SetCursor (cursorZoomIn);
					break;

				case 2://midle
					SetCursor (cursorZoomPan);
					break;

				case 3://right
					SetCursor (cursorZoomOut);
					break;
			}

			mouseDown = args.Event.Button;
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			base.OnMouseMove (o, args, point);

			if (mouseDown == 1) {
				if (Math.Abs (shape_origin.X - point.X) > tolerance || Math.Abs (shape_origin.Y - point.Y) > tolerance)  // if they've moved the mouse more than 10 pixels since they clicked
					is_drawing = true;
					
				//still draw rectangle after we have draw it one time...
				UpdateRectangle (point);
			} else if (mouseDown == 2) {
				PintaCore.Workspace.ScrollCanvas ((int)((shape_origin.X - point.X) * PintaCore.Workspace.Scale), (int)((shape_origin.Y - point.Y) * PintaCore.Workspace.Scale));
			}
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			double x = point.X;
			double y = point.Y;
			PintaCore.Layers.ToolLayer.Hidden = true;
			
			if (mouseDown == 1 || mouseDown == 3) {	//left or right
				if (args.Event.Button == 1) {	//left
					if (Math.Abs (shape_origin.X - x) <= tolerance && Math.Abs (shape_origin.Y - y) <= tolerance) {
						PintaCore.Workspace.ZoomIn ();
						PintaCore.Workspace.RecenterView (x, y);
					} else {
						PintaCore.Workspace.ZoomToRectangle (PointsToRectangle (shape_origin, point));
					}
				} else {
					PintaCore.Workspace.ZoomOut ();
					PintaCore.Workspace.RecenterView (x, y);
				}
			}

			mouseDown = 0;
			
			is_drawing = false;
			SetCursor (cursorZoom);//restore regular cursor
		}

		Rectangle PointsToRectangle (Cairo.PointD p1, Cairo.PointD p2)
		{
			double x, y, w, h;

			if (p1.Y <= p2.Y) {
				y = p1.Y;
				h = p2.Y - y;
			} else {
				y = p2.Y;
				h = p1.Y - y;
			}

			if (p1.X <= p2.X) {
				x = p1.X;
				w = p2.X - x;
			} else {
				x = p2.X;
				w = p1.X - x;
			}

			return new Rectangle (x, y, w, h);
		}
	}
}
