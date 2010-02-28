// 
// ColorGradientWidget.cs
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
using System.ComponentModel;
using System.Linq;
using Cairo;

using Pinta.Core;

namespace Pinta
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ColorGradientWidget : Gtk.Bin
	{
		//gradient horizontal padding
		private const double xpad = 0.1;
		//gradient vertical padding
		private const double ypad = 0.03;
		
		private double[] vals;
		
		private Rectangle GradientRectangle {
			get {
					Rectangle rect = Allocation.ToCairoRectangle ();
					double x = rect.X + xpad * rect.Width;
					double y = rect.Y + ypad * rect.Height;
					double width = (1 - 2 * xpad) * rect.Width;
					double height = (1 - 2 * ypad) * rect.Height;
					
					return new Rectangle (x, y, width, height);
			}
		}
			
		[Category("Custom Properties")]
		public int Count {
			get { return vals.Length; }
			set {
				if (value < 2 || value > 3) {
                    throw new ArgumentOutOfRangeException("value", value, "Count must be 2 or 3");
                }
			
				vals = new double[value];
				double step = 256 / (value - 1);
				
				for (int i = 0; i < value ; i++) {
					vals [i] = i * step - 1;
				}
			}
		}
		
		public Color MaxColor { get; set; }

		public ColorGradientWidget ()
		{
			this.Build ();
			
//			how to enable receiving MotionNotifyEvent ?
//			MotionNotifyEvent += HandleMotionNotifyEvent;
			
			ExposeEvent += HandleExposeEvent;
		}

		public int GetValue (int i)
		{
			return (int) vals [i];
		}
		
		public void SetValue (int i, int val)
		{
			if (vals [i] != val) { 
				vals [i] = val;
				GdkWindow.Invalidate ();
				OnValueChanged (i);
			}
		}
		
		private double GetYValue (double val)
		{
			Rectangle rect = GradientRectangle;
			Rectangle all = Allocation.ToCairoRectangle();
			
			return all.Y + ypad * all.Height + rect.Height * (255 - val) / 255; 
		}
		
		private int GetValueFromY (double yval)
		{
			Rectangle rect = GradientRectangle;
			Rectangle all = Allocation.ToCairoRectangle ();
			
			yval -= all.Y + ypad * all.Height;
			return ((int)(255 * (rect.Height - yval) / rect.Height));
		}
		
		private int FindValueIndex(int y)
		{
			var yvals = (from val in vals select GetYValue (val)).ToArray();
			
			for (int i=0; i < Count - 1; i++) {
				double y1 = yvals [i];
				double y2 = yvals [i + 1];
				double h = (y1 - y2) / 2;
				
				if (y1 - y <= h) return i;
				if (y - y2 <= h) return i + 1;
			}
			
			return -1;
		}
		
//		instead of
//		private void HandleMotionNotifyEvent (object o, Gtk.MotionNotifyEventArgs args)
		public void MotionNotify ()
		{
			int px, py;
			Gdk.ModifierType mask;
			GdkWindow.GetPointer (out px, out py, out mask); 
			
			if (mask == Gdk.ModifierType.Button1Mask) {
			
				int i = FindValueIndex (py);
				if (i != -1)
				{
					Rectangle rect = GradientRectangle;
					double y = GetValueFromY (py);
					
					if (rect.ContainsPoint (rect.X, py)) {
						vals[i] = y;
						OnValueChanged (i);
					}
				}
			}
			
			GdkWindow.Invalidate ();
		}
		
		private void DrawGradient (Context g)
		{
			Rectangle rect = GradientRectangle;
				
			Gradient pat = new LinearGradient(rect.X, rect.Y, rect.X, 
			                                  rect.Y + rect.Height);
			pat.AddColorStop (0, MaxColor);
			pat.AddColorStop (1, new Cairo.Color (0, 0, 0));
			
			g.Rectangle (rect);
			g.Pattern = pat;
			g.Fill();
		}
		
		private void DrawTriangles (Context g)
		{
			int px, py;
			Gdk.ModifierType mask;
			GdkWindow.GetPointer (out px, out py, out mask); 
			
			Rectangle rect = GradientRectangle;
			Rectangle all = Allocation.ToCairoRectangle();
			
			int index = FindValueIndex (py);
			
			for (int i = 0; i < Count; i++) {
				
				double val = vals [i];
				double y = GetYValue (val);
				Color color = (index == i && all.ContainsPoint (px, py)) ?
								new Color (0.1, 0.1, 0.9) : new Color (0.1, 0.1, 0.1);
				
				//left triangle
				PointD[] points = new PointD[] { new PointD (rect.X, y),
												 new PointD (rect.X - xpad * rect.Width, y + ypad * rect.Height),
												 new PointD (rect.X - xpad * rect.Width, y - ypad * rect.Height) };
				g.FillPolygonal (points, color);
				
				double x = rect.X + rect.Width;
				//right triangle
				PointD[] points2 = new PointD[] { new PointD (x , y),
												 new PointD (x + xpad * rect.Width, y + ypad * rect.Height),
												 new PointD (x + xpad * rect.Width, y - ypad * rect.Height) };
				g.FillPolygonal (points2, color);
			}
		}
		
		private void HandleExposeEvent (object o, Gtk.ExposeEventArgs args)
		{
			using (Context g = Gdk.CairoHelper.Create (this.GdkWindow)) {
				
				DrawGradient (g);
				DrawTriangles (g);
			}
		}
		
		#region Protected Methods
		bool running;
		protected void OnValueChanged(int index) 
		{
			if (running)
				return;
			
			running=true;
            if (ValueChanged != null) {
                ValueChanged(this, new IndexEventArgs (index));
            }
			running=false;
        }
		#endregion
		
		#region Public Events
		public event IndexEventHandler ValueChanged;
		#endregion
	}
}
