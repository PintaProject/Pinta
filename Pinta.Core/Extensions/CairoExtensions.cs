// 
// CairoExtensions.cs
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

namespace Pinta.Core
{
	public static class CairoExtensions
	{
		// Most of these functions return an affected area
		// This can be ignored if you don't need it
		public static Rectangle DrawRectangle (this Context g, Rectangle r, Color color, int lineWidth)
		{
			// Put it on a pixel line
			if (lineWidth == 1)
				r = new Rectangle (r.X - 0.5, r.Y - 0.5, r.Width, r.Height);
			
			g.Save ();
			
			g.MoveTo (r.X, r.Y);
			g.LineTo (r.X + r.Width, r.Y);
			g.LineTo (r.X + r.Width, r.Y + r.Height);
			g.LineTo (r.X, r.Y + r.Height);
			g.LineTo (r.X, r.Y);
			
			g.Color = color;
			g.LineWidth = lineWidth;
			g.LineCap = LineCap.Square;
			
			Rectangle dirty = g.StrokeExtents ();
			g.Stroke ();
			
			g.Restore ();
			
			return dirty;
		}
		
		public static Path CreateRectanglePath (this Context g, Rectangle r)
		{
			g.Save ();
			
			g.MoveTo (r.X, r.Y);
			g.LineTo (r.X + r.Width, r.Y);
			g.LineTo (r.X + r.Width, r.Y + r.Height);
			g.LineTo (r.X, r.Y + r.Height);
			g.LineTo (r.X, r.Y);
			
			Path path = g.CopyPath ();
			g.Restore ();
			
			return path;
		}

		public static void FillRectangle (this Context g, Rectangle r, Color color)
		{
			g.Save ();
			
			g.MoveTo (r.X, r.Y);
			g.LineTo (r.X + r.Width, r.Y);
			g.LineTo (r.X + r.Width, r.Y + r.Height);
			g.LineTo (r.X, r.Y + r.Height);
			g.LineTo (r.X, r.Y);
			
			g.Color = color;
			g.Fill ();
			
			g.Restore ();
		}

		public static void FillRectangle (this Context g, Rectangle r, Pattern pattern)
		{
			g.Save ();
			
			g.MoveTo (r.X, r.Y);
			g.LineTo (r.X + r.Width, r.Y);
			g.LineTo (r.X + r.Width, r.Y + r.Height);
			g.LineTo (r.X, r.Y + r.Height);
			g.LineTo (r.X, r.Y);
			
			g.Pattern = pattern;
			g.Fill ();
			
			g.Restore ();
		}
		
		public static Rectangle FillStrokedRectangle (this Context g, Rectangle r, Color fill, Color stroke, int lineWidth)
		{
			double x = r.X;
			double y = r.Y;
			
			g.Save ();

			// Put it on a pixel line
			if (lineWidth == 1) {
				x += 0.5;
				y += 0.5;
			}
			
			g.MoveTo (x, y);
			g.LineTo (x + r.Width, y);
			g.LineTo (x + r.Width, y + r.Height);
			g.LineTo (x, y + r.Height);
			g.LineTo (x, y);
			
			g.Color = fill;
			g.FillPreserve ();
			
			g.Color = stroke;
			g.LineWidth = lineWidth;
			g.LineCap = LineCap.Square;
			
			g.StrokePreserve ();
			
			Rectangle dirty = g.StrokeExtents ();
			
			g.Restore ();
			
			return dirty;
		}

		public static void DrawEllipse (this Context g, Rectangle r, Color color, int lineWidth)
		{
			double rx = r.Width / 2;
			double ry = r.Height / 2;
			double cx = r.X + rx;
			double cy = r.Y + ry;
			double c1 = 0.552285;
			
			g.Save ();
			
			g.MoveTo (cx + rx, cy);
			
			g.CurveTo (cx + rx, cy - c1 * ry, cx + c1 * rx, cy - ry, cx, cy - ry);
			g.CurveTo (cx - c1 * rx, cy - ry, cx - rx, cy - c1 * ry, cx - rx, cy);
			g.CurveTo (cx - rx, cy + c1 * ry, cx - c1 * rx, cy + ry, cx, cy + ry);
			g.CurveTo (cx + c1 * rx, cy + ry, cx + rx, cy + c1 * ry, cx + rx, cy);
			
			g.ClosePath ();
			
			g.Color = color;
			g.LineWidth = lineWidth;
			
			g.Stroke ();
			
			g.Restore ();
		}

		public static void FillEllipse (this Context g, Rectangle r, Color color)
		{
			double rx = r.Width / 2;
			double ry = r.Height / 2;
			double cx = r.X + rx;
			double cy = r.Y + ry;
			double c1 = 0.552285;
			
			g.Save ();
			
			g.MoveTo (cx + rx, cy);
			
			g.CurveTo (cx + rx, cy - c1 * ry, cx + c1 * rx, cy - ry, cx, cy - ry);
			g.CurveTo (cx - c1 * rx, cy - ry, cx - rx, cy - c1 * ry, cx - rx, cy);
			g.CurveTo (cx - rx, cy + c1 * ry, cx - c1 * rx, cy + ry, cx, cy + ry);
			g.CurveTo (cx + c1 * rx, cy + ry, cx + rx, cy + c1 * ry, cx + rx, cy);
			
			g.ClosePath ();
			
			g.Color = color;
			g.Fill ();
			
			g.Restore ();
		}

		public static Path CreateEllipsePath (this Context g, Rectangle r)
		{
			double rx = r.Width / 2;
			double ry = r.Height / 2;
			double cx = r.X + rx;
			double cy = r.Y + ry;
			double c1 = 0.552285;
			
			g.Save ();
			
			g.MoveTo (cx + rx, cy);
			
			g.CurveTo (cx + rx, cy - c1 * ry, cx + c1 * rx, cy - ry, cx, cy - ry);
			g.CurveTo (cx - c1 * rx, cy - ry, cx - rx, cy - c1 * ry, cx - rx, cy);
			g.CurveTo (cx - rx, cy + c1 * ry, cx - c1 * rx, cy + ry, cx, cy + ry);
			g.CurveTo (cx + c1 * rx, cy + ry, cx + rx, cy + c1 * ry, cx + rx, cy);
			
			g.ClosePath ();

			Path path = g.CopyPath ();
			
			g.Restore ();
			
			return path;
		}
		
		public static Rectangle FillStrokedEllipse (this Context g, Rectangle r, Color fill, Color stroke, int lineWidth)
		{
			double rx = r.Width / 2;
			double ry = r.Height / 2;
			double cx = r.X + rx;
			double cy = r.Y + ry;
			double c1 = 0.552285;
			
			g.Save ();
			
			g.MoveTo (cx + rx, cy);
			
			g.CurveTo (cx + rx, cy - c1 * ry, cx + c1 * rx, cy - ry, cx, cy - ry);
			g.CurveTo (cx - c1 * rx, cy - ry, cx - rx, cy - c1 * ry, cx - rx, cy);
			g.CurveTo (cx - rx, cy + c1 * ry, cx - c1 * rx, cy + ry, cx, cy + ry);
			g.CurveTo (cx + c1 * rx, cy + ry, cx + rx, cy + c1 * ry, cx + rx, cy);
			
			g.ClosePath ();
			
			g.Color = fill;
			g.FillPreserve ();
			
			g.Color = stroke;
			g.LineWidth = lineWidth;
			
			g.StrokePreserve ();
			
			Rectangle dirty = g.StrokeExtents ();
			g.Restore ();
			
			return dirty;
		}

		public static Rectangle FillStrokedRoundedRectangle (this Context g, Rectangle r, double radius, Color fill, Color stroke, int lineWidth)
		{
			g.Save ();
			
			if ((radius > r.Height / 2) || (radius > r.Width / 2))
				radius = Math.Min (r.Height / 2, r.Width / 2);
			
			g.MoveTo (r.X, r.Y + radius);
			g.Arc (r.X + radius, r.Y + radius, radius, Math.PI, -Math.PI / 2);
			g.LineTo (r.X + r.Width - radius, r.Y);
			g.Arc (r.X + r.Width - radius, r.Y + radius, radius, -Math.PI / 2, 0);
			g.LineTo (r.X + r.Width, r.Y + r.Height - radius);
			g.Arc (r.X + r.Width - radius, r.Y + r.Height - radius, radius, 0, Math.PI / 2);
			g.LineTo (r.X + radius, r.Y + r.Height);
			g.Arc (r.X + radius, r.Y + r.Height - radius, radius, Math.PI / 2, Math.PI);
			g.ClosePath ();
			
			g.Color = fill;
			g.FillPreserve ();
			
			g.Color = stroke;
			g.LineWidth = lineWidth;
			
			g.StrokePreserve ();
			
			Rectangle dirty = g.StrokeExtents ();
			g.Restore ();
			
			return dirty;
		}

		public static void DrawPixbuf (this Context g, Gdk.Pixbuf pixbuf, Point dest)
		{
			g.Save ();

			Gdk.CairoHelper.SetSourcePixbuf (g, pixbuf, dest.X, dest.Y);
			g.Paint ();
			g.Restore ();
		}

		public static Cairo.Rectangle ToCairoRectangle (this Gdk.Rectangle r)
		{
			return new Cairo.Rectangle (r.X, r.Y, r.Width, r.Height);
		}

		public static Cairo.Point Location (this Cairo.Rectangle r)
		{
			return new Cairo.Point ((int)r.X, (int)r.Y);
		}

		public static Cairo.Rectangle Clamp (this Cairo.Rectangle r)
		{
			double x = r.X;
			double y = r.Y;
			double w = r.Width;
			double h = r.Height;
			
			if (x < 0) {
				w -= x;
				x = 0;
			}
			
			if (y < 0) {
				h -= y;
				y = 0;
			}
			
			return new Cairo.Rectangle (x, y, w, h);
		}

		public static Gdk.Rectangle ToGdkRectangle (this Cairo.Rectangle r)
		{
			return new Gdk.Rectangle ((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
		}

		public static bool ContainsPoint (this Cairo.Rectangle r, double x, double y)
		{
			if (x < r.X || x > r.X + r.Width)
				return false;
			
			if (y < r.Y || y > r.Y + r.Height)
				return false;
			
			return true;
		}
	}
}
