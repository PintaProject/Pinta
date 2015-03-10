// 
// CloneStampTool.cs
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
using Pinta.Core;
using Mono.Unix;
using Gdk;

namespace Pinta.Tools
{
	public class CloneStampTool : BaseBrushTool
	{
		private bool painting;
		private Point origin = new Point (int.MinValue, int.MinValue);
		private Point offset = new Point (int.MinValue, int.MinValue);
		private Point last_point = new Point (int.MinValue, int.MinValue);

		public override string Name {
			get { return Catalog.GetString ("Clone Stamp"); }
		}
		public override string Icon {
			get { return "Tools.CloneStamp.png"; }
		}
		public override string StatusBarText { get { return Catalog.GetString ("Ctrl-left click to set origin, left click to paint."); } }

		public override Gdk.Cursor DefaultCursor {
			get {
				int iconOffsetX, iconOffsetY;
				var icon = CreateIconWithShape ("Cursor.CloneStamp.png",
				                                CursorShape.Ellipse, BrushWidth, 16, 26,
				                                out iconOffsetX, out iconOffsetY);
                return new Gdk.Cursor (Gdk.Display.Default, icon, iconOffsetX, iconOffsetY);
			}
		}
		public override bool CursorChangesOnZoom { get { return true; } }

		public override Gdk.Key ShortcutKey { get { return Gdk.Key.L; } }
		public override int Priority { get { return 33; } }
		protected override bool ShowAntialiasingButton { get { return true; } }

		protected override void OnBuildToolBar(Gtk.Toolbar tb)
		{
			base.OnBuildToolBar(tb);

			// Change the cursor when the BrushWidth is changed.
			brush_width.ComboBox.Changed += (sender, e) => SetCursor (DefaultCursor);
		}

		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			// We only do stuff with the left mouse button
			if (args.Event.Button != 1)
				return;
				
			// Ctrl click is set origin, regular click is begin drawing
			if (!args.Event.IsControlPressed ()) {
				if (origin.IsNotSet ())
					return;
					
				painting = true;
				
				if (offset.IsNotSet ())
					offset = new Point ((int)point.X - origin.X, (int)point.Y - origin.Y);

				doc.ToolLayer.Clear ();
				doc.ToolLayer.Hidden = false;

				surface_modified = false;
				undo_surface = doc.CurrentUserLayer.Surface.Clone ();
			} else {
				origin = point.ToGdkPoint ();
			}
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			if (!painting || offset.IsNotSet ())
				return;
				
			int x = (int)point.X;
			int y = (int)point.Y;

			if (last_point.IsNotSet ()) {
				last_point = new Point (x, y);
				return;
			}

			using (var g = doc.CreateClippedToolContext ()) {
				g.Antialias = UseAntialiasing ? Cairo.Antialias.Subpixel : Cairo.Antialias.None;

				g.MoveTo (last_point.X, last_point.Y);
				g.LineTo (x, y);

				g.SetSource (doc.CurrentUserLayer.Surface, offset.X, offset.Y);
				g.LineWidth = BrushWidth;
				g.LineCap = Cairo.LineCap.Round;

				g.Stroke ();
			}

			var dirty_rect = GetRectangleFromPoints (last_point, new Point (x, y));
			
			last_point = new Point (x, y);
			surface_modified = true;
			doc.Workspace.Invalidate (dirty_rect);
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			painting = false;

			using (Cairo.Context g = new Cairo.Context (doc.CurrentUserLayer.Surface)) {
				g.SetSource (doc.ToolLayer.Surface);
				g.Paint ();
			}
			
			base.OnMouseUp (canvas, args, point);
			
			offset = new Point (int.MinValue, int.MinValue);
			last_point = new Point (int.MinValue, int.MinValue);

			doc.ToolLayer.Clear ();
			doc.ToolLayer.Hidden = true;
			doc.Workspace.Invalidate ();
		}

		protected override void OnKeyDown (Gtk.DrawingArea canvas, Gtk.KeyPressEventArgs args)
		{
			base.OnKeyDown(canvas, args);
			//note that this WONT work if user presses control key and THEN selects the tool!
			if (args.Event.Key == Key.Control_L || args.Event.Key == Key.Control_R) {
				Gdk.Pixbuf icon = PintaCore.Resources.GetIcon ("Cursor.CloneStampSetSource.png");
				Gdk.Cursor setSourceCursor = new Gdk.Cursor (Gdk.Display.Default, icon, 16, 26);
				SetCursor(setSourceCursor);
			}
		}

		protected override void OnKeyUp (Gtk.DrawingArea canvas, Gtk.KeyReleaseEventArgs args)
		{
			base.OnKeyUp(canvas, args);
			if (args.Event.Key == Key.Control_L || args.Event.Key == Key.Control_R)
				SetCursor(DefaultCursor);
		}

		protected override void OnDeactivated(BaseTool newTool)
		{
			origin = new Point (int.MinValue, int.MinValue);
		}
	}
}
