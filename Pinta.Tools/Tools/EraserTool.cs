// 
// EraserTool.cs
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
using Pinta.Core;
using Mono.Unix;

namespace Pinta.Tools
{
	public class EraserTool : BaseBrushTool
	{		
		private Point last_point = point_empty;

		public EraserTool ()
		{
		}

		#region Properties
		public override string Name { get { return Catalog.GetString ("Eraser"); } }
		public override string Icon { get { return "Tools.Eraser.png"; } }
		public override string StatusBarText { get { return Catalog.GetString ("Left click to erase to transparent, right click to erase to secondary color. "); } }
		public override Gdk.Key ShortcutKey { get { return Gdk.Key.E; } }
		public override int Priority { get { return 27; } }
		#endregion

		#region Mouse Handlers
		protected override void OnMouseMove (object o, Gtk.MotionNotifyEventArgs args, Cairo.PointD point)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			if (mouse_button <= 0) {
				last_point = point_empty;
				return;
			}

			DrawingArea drawingarea1 = (DrawingArea)o;
			
			int x = (int)point.X;
			int y = (int)point.Y;
			
			if (last_point.Equals (point_empty))
				last_point = new Point (x, y);

			if (doc.Workspace.PointInCanvas (point))
				surface_modified = true;

			ImageSurface surf = doc.CurrentLayer.Surface;
			
			using (Context g = new Context (surf)) {
				g.AppendPath (doc.SelectionPath);
				g.FillRule = FillRule.EvenOdd;
				g.Clip ();

				g.Antialias = UseAntialiasing ? Antialias.Subpixel : Antialias.None;

				// Adding 0.5 forces cairo into the correct square:
				// See https://bugs.launchpad.net/bugs/672232
				g.MoveTo (last_point.X + 0.5, last_point.Y + 0.5);
				g.LineTo (x + 0.5, y + 0.5);

				// Right-click is erase to background color, left-click is transparent
				if (mouse_button == 3)
					g.Color = PintaCore.Palette.SecondaryColor;
				else
					g.Operator = Operator.Clear;

				g.LineWidth = BrushWidth;
				g.LineJoin = LineJoin.Round;
				g.LineCap = LineCap.Round;
				
				g.Stroke ();
			}
			
			Gdk.Rectangle r = GetRectangleFromPoints (last_point, new Point (x, y));

			doc.Workspace.Invalidate (r);
			
			last_point = new Point (x, y);
		}
		#endregion
	}
}
