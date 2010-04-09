// 
// PencilTool.cs
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
using Cairo;
using Gtk;

namespace Pinta.Core
{
	public class PencilTool : BaseTool
	{		
		private Point last_point = point_empty;
		
		private ImageSurface undo_surface;
		private bool surface_modified;

		public PencilTool ()
		{
		}

		#region Properties
		public override string Name { get { return "Pencil"; } }
		public override string Icon { get { return "Tools.Pencil.png"; } }
		public override string StatusBarText { get { return "Left click to draw freeform, one-pixel wide lines with the primary color, right click to use the secondary color"; } }
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.P; } }
		#endregion

		#region Mouse Handlers
		protected override void OnMouseDown (Gtk.DrawingArea canvas, Gtk.ButtonPressEventArgs args, Cairo.PointD point)
		{
			surface_modified = false;
			undo_surface = PintaCore.Layers.CurrentLayer.Surface.Clone ();
		}

		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			Color tool_color;
			
			if ((args.Event.State & Gdk.ModifierType.Button1Mask) == Gdk.ModifierType.Button1Mask)
				tool_color = PintaCore.Palette.PrimaryColor;
			else if ((args.Event.State & Gdk.ModifierType.Button3Mask) == Gdk.ModifierType.Button3Mask)
				tool_color = PintaCore.Palette.SecondaryColor;
			else {
				last_point = point_empty;
				return;
			}
				
			DrawingArea drawingarea1 = (DrawingArea)o;
			
			int x = (int)point.X;
			int y = (int)point.Y;
			
			if (last_point.Equals (point_empty)) {
				last_point = new Point (x, y);
				return;
			}
			
			if (PintaCore.Workspace.PointInCanvas (point))
				surface_modified = true;

			ImageSurface surf = PintaCore.Layers.CurrentLayer.Surface;
			
			using (Context g = new Context (surf)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();
				
				g.Antialias = Antialias.None;
				
				g.MoveTo (last_point.X, last_point.Y);
				g.LineTo (x, y);

				g.Color = tool_color;
				g.LineWidth = 1;
				g.LineCap = LineCap.Square;
				
				g.Stroke ();
			}
			
			Gdk.Rectangle r = GetRectangleFromPoints (last_point, new Point (x, y));

			PintaCore.Workspace.Invalidate (r);
			
			last_point = new Point (x, y);
		}
		
		protected override void OnMouseUp (Gtk.DrawingArea canvas, Gtk.ButtonReleaseEventArgs args, Cairo.PointD point)
		{
			if (surface_modified)
				PintaCore.History.PushNewItem (new SimpleHistoryItem (Icon, Name, undo_surface, PintaCore.Layers.CurrentLayerIndex));
			else if (undo_surface != null)
				(undo_surface as IDisposable).Dispose ();

			surface_modified = false;
		}
		#endregion

		#region Private Methods
		private Gdk.Rectangle GetRectangleFromPoints (Point a, Point b)
		{
			int x = Math.Min (a.X, b.X) - 2 - 2;
			int y = Math.Min (a.Y, b.Y) - 2 - 2;
			int w = Math.Max (a.X, b.X) - x + (2 * 2) + 4;
			int h = Math.Max (a.Y, b.Y) - y + (2 * 2) + 4;
			
			return new Gdk.Rectangle (x, y, w, h);
		}
		#endregion
	}
}
