/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Krzysztof Marecki <marecki.krzysztof@gmail.com>         //
/////////////////////////////////////////////////////////////////////////////////

// Additional code:
// 
// HistogramWidget.cs
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
using Cairo;

using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem(true)]
	public class HistogramWidget : FilledAreaBin
	{
		private bool[] selected;

		[Category("Custom Properties")]
		public bool FlipHorizontal { get; set; }
		
		[Category("Custom Properties")]
		public bool FlipVertical { get; set; }
		
		public HistogramRgb Histogram { get; private set; }
		
		
		public HistogramWidget ()
		{
			Histogram = new HistogramRgb ();
			selected = new bool[] {true, true, true};
			
			ExposeEvent += HandleExposeEvent;
		}
		
		public void ResetHistogram ()
		{
			Histogram = new HistogramRgb();
		}
		
		public void SetSelected(int channel, bool val)
        {
            selected[channel] = val;
        }
		
		private void CheckPoint (Rectangle rect, PointD point)
		{	
			if (point.X < rect.X) {
				point.X = rect.X;
			} else if (point.X > rect.X + rect.Width) {
				point.X = rect.X + rect.Width;
			}
			
			if (point.Y < rect.Y) {
				point.Y = rect.Y;
			} else if (point.Y > rect.Y + rect.Height) {
				point.Y = rect.Y + rect.Height;
			}
		}	
			
		private void DrawChannel(Context g, ColorBgra color, int channel, long max, float mean)
        {
			Rectangle rect = Allocation.ToCairoRectangle ();
			Histogram histogram = Histogram;
			
            int l = (int)rect.X;
            int t = (int)rect.Y;
			int r = (int)(rect.X + rect.Width);
            int b = (int)(rect.Y + rect.Height);
			
            int entries = histogram.Entries;
            long[] hist = histogram.HistogramValues [channel];
			
            ++max;

            if (FlipHorizontal) {
                Utility.Swap(ref l, ref r);
            }

            if (!FlipVertical) {
                Utility.Swap(ref t, ref b);
            }

            PointD[] points = new PointD[entries + 2];

            points[entries] = new PointD (Utility.Lerp (l, r, -1), Utility.Lerp (t, b, 20));
            points[entries + 1] = new PointD (Utility.Lerp (l, r, -1), Utility.Lerp (b, t, 20));
			
            for (int i = 0; i < entries; i += entries - 1) {
                points[i] = new PointD (
                    Utility.Lerp (l, r, (float)hist[i] / (float)max),
                    Utility.Lerp (t, b, (float)i / (float)entries));
				
				CheckPoint (rect, points [i]);
            }

            long sum3 = hist[0] + hist[1];
            
            for (int i = 1; i < entries - 1; ++i) {
                sum3 += hist[i + 1];

                points[i] = new PointD(
                    Utility.Lerp(l, r, (float)(sum3) / (float)(max * 3.1f)),
                    Utility.Lerp(t, b, (float)i / (float)entries));
			
				CheckPoint (rect, points [i]);
                sum3 -= hist[i - 1];
            }

            byte intensity = selected[channel] ? (byte)96 : (byte)32;
            ColorBgra pen_color = ColorBgra.Blend (ColorBgra.Black, color, intensity);
            ColorBgra brush_color = color;
           	brush_color.A = intensity;
			
			g.LineWidth = 1;
			
			g.Rectangle (rect);
			g.Clip ();
			g.DrawPolygonal (points, pen_color.ToCairoColor ());
			g.FillPolygonal (points, brush_color.ToCairoColor ());
        }

        private void DrawHistogram(Context g)
        {
			Histogram histogram = Histogram;
            long max = histogram.GetMax ();
            float[] mean = histogram.GetMean ();

            int channels = histogram.Channels;

            for (int i = 0; i < channels; ++i) {
                DrawChannel(g, histogram.GetVisualColor(i), i, max, mean[i]);
            }
        }

		private void HandleExposeEvent (object o, Gtk.ExposeEventArgs args)
		{
			using (Context g = Gdk.CairoHelper.Create (this.GdkWindow)) 
				DrawHistogram (g);
		}
	}
}
