// 
// PaintBrushTool.cs
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
	public class PaintBrushTool : BaseBrushTool
	{
		
		private Point last_point = point_empty;
		
		public PaintBrushTool ()
		{
		}

		#region Properties
		public override string Name { get { return "Paintbrush"; } }
		public override string Icon { get { return "Tools.Paintbrush.png"; } }
		public override string StatusBarText { get { return "Left click to draw with primary color, right click to draw with secondary color"; } }
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.B; } }
		#endregion

		#region Mouse Handlers
		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			Color tool_color;
			
			if (mouse_button == 1)
				tool_color = PintaCore.Palette.PrimaryColor;
			else if (mouse_button == 3)
				tool_color = PintaCore.Palette.SecondaryColor;
			else {
				last_point = point_empty;
				return;
			}
				
			DrawingArea drawingarea1 = (DrawingArea)o;
			
			int x = (int)point.X;
			int y = (int)point.Y;
			
			if (last_point.Equals (point_empty))
				last_point = new Point (x, y);
			
			if (PintaCore.Workspace.PointInCanvas (point))
				surface_modified = true;

			ImageSurface surf = PintaCore.Layers.CurrentLayer.Surface;
			
			using (Context g = new Context (surf)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.Antialias = Antialias.Subpixel;
				
				g.MoveTo (last_point.X, last_point.Y);
				g.LineTo (x, y);

				g.Color = tool_color;
				g.LineWidth = BrushWidth;
				g.LineJoin = LineJoin.Round;
				g.LineCap = LineCap.Round;
				
				g.Stroke ();
			}
			
			Gdk.Rectangle r = GetRectangleFromPoints (last_point, new Point (x, y));

			PintaCore.Workspace.Invalidate (r);
			
			last_point = new Point (x, y);
		}
		#endregion
	}
}
