// 
// CurvesDialog.cs
//  
// Author:
//      Krzysztof Marecki <marecki.krzysztof@gmail.com>
// 
// Copyright (c) 2010 Krzysztof Marecki
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
using System.Collections.Generic;
using Cairo;

using Pinta.Core;

namespace Pinta
{

	public partial class CurvesDialog : Gtk.Dialog
	{
		private const int drawing_width = 256;
		private const int drawing_height = 256;
		
		private List<Point> points;
		
		public SortedList<int, int>[] ControlPoints {
			get { return new SortedList<int, int>[] { }; }
		}
		
		public ColorTransferMode Mode {
			get { 
				return (comboMap.Active == 0) ? 
						ColorTransferMode.Rgb : 
						ColorTransferMode.Luminosity; }
		}
				
		public CurvesDialog ()
		{
			this.Build ();
		
			points = new List<Point> ();
			points.Add (new Point (0, drawing_height));
			points.Add (new Point (drawing_width, 0));
				
			drawing.DoubleBuffered = true;
			
			drawing.ExposeEvent += HandleDrawingExposeEvent;
			drawing.MotionNotifyEvent += HandleDrawingMotionNotifyEvent;
			drawing.LeaveNotifyEvent += HandleDrawingLeaveNotifyEvent;
		}

		private void HandleDrawingLeaveNotifyEvent (object o, Gtk.LeaveNotifyEventArgs args)
		{
			//to invalidate whole drawing area
			drawing.GdkWindow.InvalidateRect 
				(new Gdk.Rectangle (0, 0, drawing_width, drawing_height), true);	
		}

		private void HandleDrawingMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{	
			//to invalidate whole drawing area
			drawing.GdkWindow.InvalidateRect 
				(new Gdk.Rectangle (0, 0, drawing_width, drawing_height), true);	
		}

		private void HandleDrawingExposeEvent (object o, Gtk.ExposeEventArgs args)
		{	
			using (Context context = Gdk.CairoHelper.Create(drawing.GdkWindow)) {
				
				context.Rectangle (0, 0, drawing_width, drawing_height);
				context.LineWidth = 1;
				context.Stroke ();
				
				int x, y;
				Gdk.ModifierType mask;
				drawing.GdkWindow.GetPointer(out x, out y, out mask); 
				
				if (x >= 0 && x < drawing_width && y >= 0 && y < drawing_height) {
					
					context.LineWidth = 0.5;
					context.MoveTo (x, 0);
					context.LineTo (x, drawing_height);
					context.MoveTo (0, y);
					context.LineTo (drawing_width, y);
					context.Stroke();
					
					this.labelPoint.Text = string.Format ("({0}, {1})", x, y);
				} else
					this.labelPoint.Text = string.Empty;
				
				context.SetDash (new double[] {4, 4}, 2);
				context.LineWidth = 1;
				
				for (int i = 1; i < 4; i++) {
					context.MoveTo (i * drawing_width / 4, 0);
					context.LineTo (i * drawing_width / 4, drawing_height);
					context.MoveTo (0, i * drawing_height / 4);
					context.LineTo (drawing_width, i * drawing_height / 4);
					context.Stroke();
				}
				
				
			}
		}
	}
}
