//
// ArrowWindow.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;
using Gdk;

namespace Pinta.Docking.DockToolbars
{
	internal class ArrowWindow: Gtk.Window
	{
		const int LineWidth = 3;
		const int LineLength = 9;
		const int PointerWidth = 13;
		const int PointerLength = 6;
		
		Direction direction;
		Point[] arrow;
		int width, height;
		
		Gdk.GC redgc;
		
		// Where does the arrow point to
		public new enum Direction {
			Up, Down, Left, Right
		}
		
		public ArrowWindow (DockToolbarFrame frame, Direction dir): base (Gtk.WindowType.Popup)
		{
			SkipTaskbarHint = true;
			Decorated = false;
			TransientFor = frame.TopWindow;

			direction = dir;
			arrow = CreateArrow ();
			if (direction == Direction.Up || direction == Direction.Down) {
				 width = PointerWidth;
				 height = LineLength + PointerLength + 1;
			} else {
				 height = PointerWidth;
				 width = LineLength + PointerLength + 1;
			}
			
			// Create the mask for the arrow
			
			Gdk.Color black, white;
			black = new Gdk.Color (0, 0, 0);
			black.Pixel = 1;
			white = new Gdk.Color (255, 255, 255);
			white.Pixel = 0;
			
			Gdk.Pixmap pm = new Pixmap (this.GdkWindow, width, height, 1);
			Gdk.GC gc = new Gdk.GC (pm);
			gc.Background = white;
			gc.Foreground = white;
			pm.DrawRectangle (gc, true, 0, 0, width, height);
			
			gc.Foreground = black;
			pm.DrawPolygon (gc, false, arrow);
			pm.DrawPolygon (gc, true, arrow);
			
			this.ShapeCombineMask (pm, 0, 0);
			
			Realize ();
			
			redgc = new Gdk.GC (GdkWindow);
	   		redgc.RgbFgColor = new Gdk.Color (255, 0, 0);
			
			Resize (width, height);
		}
		
		public int Width {
			get { return width; }
		}
		
		public int Height {
			get { return height; }
		}
		
		Point[] CreateArrow ()
		{
			Point[] ps = new Point [8];
			ps [0] = GetPoint (0, (PointerWidth/2) - (LineWidth/2));
			ps [1] = GetPoint (LineLength, (PointerWidth/2) - (LineWidth/2));
			ps [2] = GetPoint (LineLength, 0);
			ps [3] = GetPoint (PointerLength + LineLength, (PointerWidth/2));
			ps [4] = GetPoint (LineLength, PointerWidth - 1);
			ps [5] = GetPoint (LineLength, (PointerWidth/2) + (LineWidth/2));
			ps [6] = GetPoint (0, (PointerWidth/2) + (LineWidth/2));
			ps [7] = ps [0];
			return ps;
		}
		
		Point GetPoint (int x, int y)
		{
			switch (direction) {
				case Direction.Right: return new Point (x, y);
				case Direction.Left: return new Point ((PointerLength + LineLength) - x, y);
				case Direction.Down: return new Point (y, x);
				default: return new Point (y, (PointerLength + LineLength) - x);
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			GdkWindow.DrawPolygon (redgc, false, arrow);
			GdkWindow.DrawPolygon (redgc, true, arrow);
			return true;
		}
	}
}
