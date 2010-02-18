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
		//drawing area width and height
		private const int size = 256;
		//control point radius
		private const int radius = 8;
		
		private List<Point> points;
		private int channels;
		//last added control point x;
		private int last_cpx;
		
		public SortedList<int, int>[] ControlPoints { get; private set; }
		
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
			points.Add (new Point (0, size));
			points.Add (new Point (size, 0));
				
			drawing.DoubleBuffered = true;
			
			comboMap.Changed += HandleComboMapChanged;
			buttonReset.Clicked += HandleButtonResetClicked;
			drawing.ExposeEvent += HandleDrawingExposeEvent;
			drawing.MotionNotifyEvent += HandleDrawingMotionNotifyEvent;
			drawing.LeaveNotifyEvent += HandleDrawingLeaveNotifyEvent;
			drawing.ButtonPressEvent += HandleDrawingButtonPressEvent;
			
			ResetControlPoints();
		}

		void HandleButtonResetClicked (object sender, EventArgs e)
		{
			ResetControlPoints();
		}

		private void ResetControlPoints()
		{
			channels = (Mode == ColorTransferMode.Luminosity) ? 1 : 3;
			ControlPoints = new SortedList<int, int>[channels];
			
			for (int i = 0; i < channels; i++) {
				SortedList<int, int> list = new SortedList<int, int>();
				
				list.Add (0, 0);
				list.Add (size - 1, size - 1);
				ControlPoints [i] = list;
			}
			
			InvalidateDrawing ();
		}
		
		private void HandleComboMapChanged (object sender, EventArgs e)
		{
			ResetControlPoints ();
		}
		
		private void InvalidateDrawing()
		{
			//to invalidate whole drawing area
			drawing.GdkWindow.InvalidateRect 
				(new Gdk.Rectangle (0, 0, size - 1, size - 1), true);		
		}
		
		private void HandleDrawingLeaveNotifyEvent (object o, Gtk.LeaveNotifyEventArgs args)
		{
			InvalidateDrawing ();
		}
		
		private void AddControlPoint (int x, int y)
		{
			SortedList<int, int> controlPoints = ControlPoints [0];
			
			if (!controlPoints.ContainsKey (x))
				controlPoints.Add (x, size - 1 - y);
			
			last_cpx = x;
		}
		
		private void HandleDrawingMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		{	
			int x, y;
			Gdk.ModifierType mask;
			drawing.GdkWindow.GetPointer(out x, out y, out mask); 
			
			if (args.Event.State == Gdk.ModifierType.Button1Mask) {
				SortedList<int, int> controlPoints = ControlPoints [0];
				
				if (controlPoints.ContainsKey (last_cpx))
					controlPoints.Remove (last_cpx);
						
				AddControlPoint (x, y);
			}
			
			InvalidateDrawing ();	
		}
		
		private void HandleDrawingButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			int x, y;
			Gdk.ModifierType mask;
			drawing.GdkWindow.GetPointer(out x, out y, out mask); 
			SortedList<int, int> controlPoints = ControlPoints [0];
			
			if (args.Event.Button == 1) {
				AddControlPoint (x, y);
			} 
			
			if (args.Event.Button == 3) {
				for (int i = 0; i < controlPoints.Count; i++) {
					int cpx = controlPoints.Keys [i];
					int cpy = size - 1 - (int)controlPoints.Values [i];
					
					if (CheckControlPointProximity (cpx, cpy, x, y)) {
						controlPoints.RemoveAt (i);
						break;
					}
				}
			}
			
			InvalidateDrawing();
		}

		private void DrawBorder (Context context)
		{
			context.Rectangle (0, 0, size - 1, size - 1);
			context.LineWidth = 1;
			context.Stroke ();
		}
		
		private void DrawPointerCross (Context context)
		{
			int x, y;
			Gdk.ModifierType mask;
			drawing.GdkWindow.GetPointer(out x, out y, out mask); 
			
			if (x >= 0 && x < size && y >= 0 && y < size) {
				context.LineWidth = 0.5;
				context.MoveTo (x, 0);
				context.LineTo (x, size);
				context.MoveTo (0, y);
				context.LineTo (size , y);
				context.Stroke ();
				
				this.labelPoint.Text = string.Format ("({0}, {1})", x, y);
			} else
				this.labelPoint.Text = string.Empty;
		}
		
		private void DrawGrid (Context context)
		{
			context.Color = new Color (0.05, 0.05, 0.05);
			context.SetDash (new double[] {4, 4}, 2);
			context.LineWidth = 1;
			
			for (int i = 1; i < 4; i++) {
				context.MoveTo (i * size / 4, 0);
				context.LineTo (i * size / 4, size);
				context.MoveTo (0, i * size / 4);
				context.LineTo (size, i * size / 4);
			}
			
			context.MoveTo (0, size - 1);
			context.LineTo (size - 1, 0);
			context.Stroke();
			
			context.SetDash (new double[] {}, 0);
		}
		
		//cpx, cpyx - control point's x and y coordinates
		private bool CheckControlPointProximity(int cpx, int cpy, int x, int y)
		{
			return (Math.Sqrt (Math.Pow (cpx - x, 2) + Math.Pow (cpy - y, 2)) < radius);
		}
			
		private void DrawControlPoints (Context context)
		{
			int x, y;
			Gdk.ModifierType mask;
			drawing.GdkWindow.GetPointer(out x, out y, out mask); 
			
			for (int i = 0; i < ControlPoints[0].Count; i++) {
				int cpx = ControlPoints[0].Keys[i];
				int cpy = size - 1 - (int)ControlPoints[0].Values[i];
				
				Rectangle rect;
				
				if (CheckControlPointProximity (cpx, cpy, x, y)) {
					rect = new Rectangle (cpx - (radius + 2) / 2, cpy - (radius + 2) / 2, radius + 2, radius + 2);
					context.DrawEllipse (rect, new Color (0.2, 0.2, 0.2), 2);
					rect = new Rectangle (cpx - radius / 2, cpy - radius / 2, radius, radius);
					context.FillEllipse (rect, new Color (0.9, 0.9, 0.9));
				} else {
					rect = new Rectangle (cpx - radius / 2, cpy - radius / 2, radius, radius);
					context.DrawEllipse (rect, new Color (0.3, 0.3, 0.3), 2);
				
					rect = new Rectangle (cpx - (radius - 2) / 2, cpy - (radius - 2) / 2, radius - 2, radius -2);
					context.FillEllipse (rect, new Color (0.4, 0.4, 0.4));
				}
			}
			
			context.Stroke();
		}
		
		private void DrawSpline (Context context)
		{
			int points = ControlPoints[0].Count;
			SplineInterpolator interpolator = new SplineInterpolator();
			IList<int> xa = ControlPoints[0].Keys;
			IList<int> ya = ControlPoints[0].Values;
			PointD[] line = new PointD[size];
			
			for (int i = 0; i < points; i++) {
				interpolator.Add (xa [i], ya [i]);
			}
			
			for (int i = 0; i < line.Length; i++) {
                line[i].X = (float)i;
                line[i].Y = (float)(Utility.Clamp(size - 1 - interpolator.Interpolate(i), 0, size - 1));
				
				//hack to draw line inside drawing area when y is 0
				line[i].Y = (line[i].Y != 0) ? line[i].Y : 1;
            }
			
			context.LineWidth = 2;
			context.LineJoin = LineJoin.Round;
			
			context.MoveTo (line [0]);		
			for (int i = 1; i < line.Length; i++)
				context.LineTo (line [i]);
	
			context.Color = new Color(0.4, 0.4, 0.4);
			context.Stroke();
		}
		
		private void HandleDrawingExposeEvent (object o, Gtk.ExposeEventArgs args)
		{	
			using (Context context = Gdk.CairoHelper.Create(drawing.GdkWindow)) {
				
				DrawBorder (context);
				DrawPointerCross (context);
				DrawSpline (context);
				DrawGrid (context);
				DrawControlPoints (context);
			}
		}
	}
}
