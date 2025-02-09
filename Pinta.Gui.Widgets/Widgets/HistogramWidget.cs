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

using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class HistogramWidget : Gtk.DrawingArea
{
	private readonly bool[] selected;

	public HistogramWidget ()
	{
		Histogram = new HistogramRgb ();
		selected = [true, true, true];

		SetDrawFunc ((_, context, _, _) => Draw (context));
	}

	public bool FlipHorizontal { get; set; }

	public bool FlipVertical { get; set; }

	public HistogramRgb Histogram { get; private set; }

	public void ResetHistogram ()
	{
		Histogram = new HistogramRgb ();
		Histogram.HistogramChanged += (_, _) => QueueDraw ();
	}

	public void SetSelected (int channel, bool val)
	{
		selected[channel] = val;
		QueueDraw ();
	}

	private static PointD CheckedPoint (RectangleD rect, PointD point)
	{
		if (point.X < rect.X)
			point = point with { X = rect.X };
		else if (point.X > rect.X + rect.Width)
			point = point with { X = rect.X + rect.Width };

		if (point.Y < rect.Y)
			point = point with { Y = rect.Y };
		else if (point.Y > rect.Y + rect.Height)
			point = point with { Y = rect.Y + rect.Height };

		return point;
	}

	private void DrawChannel (
		Context g,
		ColorBgra color,
		int channel,
		long max,
		float mean)
	{
		RectangleD rect = new (0, 0, GetAllocatedWidth (), GetAllocatedHeight ());

		int l = (int) rect.X;
		int t = (int) rect.Y;
		int r = (int) (rect.X + rect.Width);
		int b = (int) (rect.Y + rect.Height);

		int entryCount = Histogram.Entries;
		var hist = Histogram.HistogramValues[channel];

		++max;

		if (FlipHorizontal)
			Utility.Swap (ref l, ref r);

		if (!FlipVertical)
			Utility.Swap (ref t, ref b);

		var points = new PointD[entryCount + 2];

		points[entryCount] = new PointD (
			X: Mathematics.Lerp<double> (l, r, -1),
			Y: Mathematics.Lerp<double> (t, b, 20));
		points[entryCount + 1] = new PointD (
			X: Mathematics.Lerp<double> (l, r, -1),
			Y: Mathematics.Lerp<double> (b, t, 20));

		for (int i = 0; i < entryCount; i += entryCount - 1) {

			points[i] = new PointD (
				X: Mathematics.Lerp<double> (l, r, hist[i] / (float) max),
				Y: Mathematics.Lerp<double> (t, b, i / (float) entryCount));

			points[i] = CheckedPoint (rect, points[i]);
		}

		long sum3 = hist[0] + hist[1];

		for (int i = 1; i < entryCount - 1; ++i) {

			sum3 += hist[i + 1];

			points[i] = new PointD (
				X: Mathematics.Lerp<double> (l, r, sum3 / (float) (max * 3.1f)),
				Y: Mathematics.Lerp<double> (t, b, i / (float) entryCount));

			points[i] = CheckedPoint (rect, points[i]);
			sum3 -= hist[i - 1];
		}

		byte intensity = selected[channel] ? (byte) 96 : (byte) 32;
		ColorBgra pen_color = ColorBgra.Blend (ColorBgra.Black, color, intensity);
		ColorBgra brush_color = color.NewAlpha (intensity);

		g.LineWidth = 1;

		g.Rectangle (rect);
		g.Clip ();
		g.DrawPolygonal (points, pen_color.ToCairoColor (), LineCap.Square);
		g.FillPolygonal (points, brush_color.ToCairoColor ());
	}

	private void Draw (Context g)
	{
		long max = Histogram.GetMax ();
		var mean = Histogram.GetMean ();

		int channelCount = Histogram.Channels;

		for (int i = 0; i < channelCount; ++i)
			DrawChannel (g, Histogram.GetVisualColor (i), i, max, mean[i]);

		g.Dispose ();
	}
}
