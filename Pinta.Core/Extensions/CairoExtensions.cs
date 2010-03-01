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

		public static Rectangle FillRectangle (this Context g, Rectangle r, Color color)
		{
			g.Save ();
			
			g.MoveTo (r.X, r.Y);
			g.LineTo (r.X + r.Width, r.Y);
			g.LineTo (r.X + r.Width, r.Y + r.Height);
			g.LineTo (r.X, r.Y + r.Height);
			g.LineTo (r.X, r.Y);
			
			g.Color = color;
			
			Rectangle dirty = g.StrokeExtents ();

			g.Fill ();
			g.Restore ();

			return dirty;
		}

		public static Rectangle FillRectangle (this Context g, Rectangle r, Pattern pattern)
		{
			g.Save ();
			
			g.MoveTo (r.X, r.Y);
			g.LineTo (r.X + r.Width, r.Y);
			g.LineTo (r.X + r.Width, r.Y + r.Height);
			g.LineTo (r.X, r.Y + r.Height);
			g.LineTo (r.X, r.Y);
			
			g.Pattern = pattern;

			Rectangle dirty = g.StrokeExtents ();
			g.Fill ();

			g.Restore ();

			return dirty;
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
			
			Rectangle dirty = g.StrokeExtents ();
			
			g.Stroke ();
			g.Restore ();
			
			return dirty;
		}

		public static Rectangle DrawEllipse (this Context g, Rectangle r, Color color, int lineWidth)
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
			
			Rectangle dirty = g.StrokeExtents ();

			g.Stroke ();
			g.Restore ();

			return dirty;
		}

		public static Rectangle FillEllipse (this Context g, Rectangle r, Color color)
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
			
			Rectangle dirty = g.StrokeExtents ();
			
			g.Fill ();
			g.Restore ();
			
			return dirty;
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
			
			Rectangle dirty = g.StrokeExtents ();
			
			g.Stroke ();
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

			g.Restore ();
			
			g.Color = fill;
			g.FillPreserve ();
			
			g.Color = stroke;
			g.LineWidth = lineWidth;
			
			Rectangle dirty = g.StrokeExtents ();
			
			g.Stroke ();
			g.Restore ();
			
			return dirty;
		}

		public static Rectangle FillRoundedRectangle (this Context g, Rectangle r, double radius, Color fill)
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
			
			g.Restore ();

			g.Color = fill;
			
			Rectangle dirty = g.StrokeExtents ();

			g.Fill ();
			g.Restore ();

			return dirty;
		}
		
		public static Rectangle DrawRoundedRectangle (this Context g, Rectangle r, double radius, Color stroke, int lineWidth)
		{
			g.Save ();
			
			Path p = g.CreateRoundedRectanglePath (r, radius);
			
			g.AppendPath (p);
			
			g.Color = stroke;
			g.LineWidth = lineWidth;
			
			Rectangle dirty = g.StrokeExtents ();

			g.Stroke ();
			g.Restore ();

			(p as IDisposable).Dispose ();
			
			return dirty;
		}
		
		public static Path CreateRoundedRectanglePath (this Context g, Rectangle r, double radius)
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
		
			Path p = g.CopyPath ();
			g.Restore ();
			
			return p;
		}

		public static Rectangle DrawLine (this Context g, PointD p1, PointD p2, Color color, int lineWidth)
		{
			// Put it on a pixel line
			if (lineWidth == 1)
				p1 = new PointD (p1.X - 0.5, p1.Y - 0.5);

			g.Save ();

			g.MoveTo (p1.X, p1.Y);
			g.LineTo (p2.X, p2.Y);

			g.Color = color;
			g.LineWidth = lineWidth;
			g.LineCap = LineCap.Square;

			Rectangle dirty = g.StrokeExtents ();
			g.Stroke ();

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
		
		public unsafe static Gdk.Pixbuf ToPixbuf (this Cairo.ImageSurface surf)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;
			int len = surf.Data.Length / 4;

			for (int i = 0; i < len; i++) {
				if (dstPtr->A != 0)
					*dstPtr = (ColorBgra.FromBgra (dstPtr->R, dstPtr->G, dstPtr->B, dstPtr->A));
				dstPtr++;
			}

			Gdk.Pixbuf pb = new Gdk.Pixbuf (surf.Data, true, 8, surf.Width, surf.Height, surf.Stride);
			return pb;
		}
		
		public unsafe static Color GetPixel (this Cairo.ImageSurface surf, int x, int y)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;
			
			dstPtr += (x) + (y * surf.Width);

			return new Color (dstPtr->R / 255f, dstPtr->G / 255f, dstPtr->B / 255f, dstPtr->A / 255f);
		}
		
		public unsafe static void SetPixel (this Cairo.ImageSurface surf, int x, int y, Color color)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;

			dstPtr += (x) + (y * surf.Width);

			dstPtr->R = (byte)(color.R * 255);
			dstPtr->G = (byte)(color.G * 255);
			dstPtr->B = (byte)(color.B * 255);
			dstPtr->A = (byte)(color.A * 255);
		}

		public unsafe static ColorBgra GetColorBgra (this Cairo.ImageSurface surf, int x, int y)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;

			dstPtr += (x) + (y * surf.Width);

			return *dstPtr;
		}

		public static ColorBgra ToColorBgra (this Cairo.Color color)
		{
			ColorBgra c = new ColorBgra ();

			c.R = (byte)(color.R * 255);
			c.G = (byte)(color.G * 255);
			c.B = (byte)(color.B * 255);
			c.A = (byte)(color.A * 255);

			return c;
		}

		public static Cairo.Color ToCairoColor (this ColorBgra color)
		{
			Cairo.Color c = new Cairo.Color ();

			c.R = color.R / 255d;
			c.G = color.G / 255d;
			c.B = color.B / 255d;
			c.A = color.A / 255d;

			return c;
		}

		public static string ToString2 (this Cairo.Color c)
		{
			return string.Format ("R: {0} G: {1} B: {2} A: {3}", c.R, c.G, c.B, c.A);
		}

		public static ImageSurface Clone (this ImageSurface surf)
		{
			ImageSurface newsurf = new ImageSurface (surf.Format, surf.Width, surf.Height);

			using (Context g = new Context (newsurf)) {
				g.SetSource (surf);
				g.Paint ();
			}

			return newsurf;
		}

		public static Path Clone (this Path path)
		{
			Path newpath;
			
			using (Context g = new Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.AppendPath (path);
				newpath = g.CopyPath ();
			}

			return newpath;
		}
		
		public static Rectangle GetBounds (this Path path)
		{
			Rectangle rect;

			using (Context g = new Context (PintaCore.Layers.CurrentLayer.Surface)) {
				g.AppendPath (PintaCore.Layers.SelectionPath);
				
				// We don't want the bounding box to include a stroke width 
				// of 1, but setting it to 0 returns an empty rectangle.  Set
				// it to a sufficiently small width and rounding takes care of it
				g.LineWidth = .001;
				rect = g.StrokeExtents ();
			}

			return new Rectangle (rect.X, rect.Y, rect.Width - rect.X, rect.Height - rect.Y);
		}
		
		public static Gdk.Color ToGdkColor (this Cairo.Color color)
		{
			Gdk.Color c = new Gdk.Color ();
			c.Blue = (ushort)(color.B * ushort.MaxValue);
			c.Red = (ushort)(color.R * ushort.MaxValue);
			c.Green = (ushort)(color.G * ushort.MaxValue);
			
			return c;
		}
		
		public static ushort GdkColorAlpha (this Cairo.Color color)
		{
			return (ushort)(color.A * ushort.MaxValue);
		}

		public static double GetBottom (this Rectangle rect)
		{
			return rect.Y + rect.Height;
		}

		public static double GetRight (this Rectangle rect)
		{
			return rect.X + rect.Width;
		}
		
		public static unsafe ColorBgra* GetPointAddressUnchecked (this ImageSurface surf, int x, int y)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;

			dstPtr += (x) + (y * surf.Width);

			return dstPtr;
		}

		public static unsafe ColorBgra* GetPointAddressUnchecked (this ImageSurface surf, ColorBgra* srcDataPtr, int srcWidth, int x, int y)
		{
			ColorBgra* dstPtr = srcDataPtr;

			dstPtr += (x) + (y * srcWidth);

			return dstPtr;
		}

		public static unsafe ColorBgra GetPointUnchecked (this ImageSurface surf, int x, int y)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;

			dstPtr += (x) + (y * surf.Width);

			return *dstPtr;
		}

		// This isn't really an extension method, since it doesn't use
		// the passed in argument, but it's nice to have the same calling
		// convention as the uncached version.  If you can use this one
		// over the other, it is much faster in tight loops (like effects).
		public static unsafe ColorBgra GetPointUnchecked (this ImageSurface surf, ColorBgra* srcDataPtr, int srcWidth, int x, int y)
		{
			ColorBgra* dstPtr = srcDataPtr;

			dstPtr += (x) + (y * srcWidth);

			return *dstPtr;
		}

		public static unsafe ColorBgra* GetRowAddressUnchecked (this ImageSurface surf, int y)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;

			dstPtr += y * surf.Width;

			return dstPtr;
		}

		public static unsafe ColorBgra* GetRowAddressUnchecked (this ImageSurface surf, ColorBgra* srcDataPtr, int srcWidth, int y)
		{
			ColorBgra* dstPtr = srcDataPtr;

			dstPtr += y * srcWidth;

			return dstPtr;
		}

		public static unsafe ColorBgra *GetPointAddress (this ImageSurface surf, int x, int y)
		{
			if (x < 0 || x >= surf.Width)
				throw new ArgumentOutOfRangeException ("x", "Out of bounds: x=" + x.ToString ());

			return surf.GetPointAddressUnchecked (x, y);
		}

		public static unsafe ColorBgra* GetPointAddress (this ImageSurface surf, Gdk.Point point)
		{
			return surf.GetPointAddress (point.X, point.Y);
		}

		public static Gdk.Rectangle GetBounds (this ImageSurface surf)
		{
			return new Gdk.Rectangle (0, 0, surf.Width, surf.Height);
		}

		public static Gdk.Size GetSize (this ImageSurface surf)
		{
			return new Gdk.Size (surf.Width, surf.Height);
		}
	}
}
