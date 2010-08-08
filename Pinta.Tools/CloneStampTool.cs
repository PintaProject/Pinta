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
	[System.ComponentModel.Composition.Export (typeof (BaseTool))]
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
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.L; } }
		public override int Priority { get { return 33; } }

		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			// We only do stuff with the left mouse button
			if (args.Event.Button != 1)
				return;
				
			// Ctrl click is set origin, regular click is begin drawing
			if ((args.Event.State & Gdk.ModifierType.ControlMask) == 0) {
				if (origin.IsNotSet ())
					return;
					
				painting = true;
				
				if (offset.IsNotSet ())
					offset = new Point ((int)point.X - origin.X, (int)point.Y - origin.Y);

				PintaCore.Layers.ToolLayer.Clear ();
				PintaCore.Layers.ToolLayer.Hidden = false;

				surface_modified = false;
				undo_surface = PintaCore.Layers.CurrentLayer.Surface.Clone ();
			} else {
				origin = point.ToGdkPoint ();
			}
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			if (!painting || offset.IsNotSet ())
				return;
				
			int x = (int)point.X;
			int y = (int)point.Y;

			if (last_point.IsNotSet ()) {
				last_point = new Point (x, y);
				return;
			}

			using (Cairo.Context g = new Cairo.Context (PintaCore.Layers.ToolLayer.Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = Cairo.FillRule.EvenOdd;
				g.Clip ();

				g.Antialias = Cairo.Antialias.Subpixel;

				g.MoveTo (last_point.X, last_point.Y);
				g.LineTo (x, y);

				g.SetSource (PintaCore.Workspace.ActiveDocument.CurrentLayer.Surface, offset.X, offset.Y);
				g.LineWidth = BrushWidth;
				g.LineCap = Cairo.LineCap.Round;

				g.Stroke ();
			}

			var dirty_rect = GetRectangleFromPoints (last_point, new Point (x, y));
			
			last_point = new Point (x, y);
			surface_modified = true;
			PintaCore.Workspace.Invalidate (dirty_rect);
		}

		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			painting = false;

			using (Cairo.Context g = new Cairo.Context (PintaCore.Workspace.ActiveDocument.CurrentLayer.Surface)) {
				g.SetSource (PintaCore.Layers.ToolLayer.Surface);
				g.Paint ();
			}
			
			base.OnMouseUp (canvas, args, point);
			
			offset = new Point (int.MinValue, int.MinValue);
			last_point = new Point (int.MinValue, int.MinValue);
			
			PintaCore.Layers.ToolLayer.Clear ();
			PintaCore.Layers.ToolLayer.Hidden = true;
			PintaCore.Workspace.Invalidate ();
		}

		protected override void OnDeactivated ()
		{
			origin = new Point (int.MinValue, int.MinValue);
		}
	}
}
