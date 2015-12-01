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

// Some functions are from Paint.NET:

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
/////////////////////////////////////////////////////////////////////////////////

using System;
using Cairo;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Pinta.Core
{
	public static class CairoExtensions
	{
		// Most of these functions return an affected area
		// This can be ignored if you don't need it

		#region context
		public static Rectangle DrawRectangle (this Context g, Rectangle r, Color color, int lineWidth)
		{
			// Put it on a pixel line
			if (lineWidth == 1)
				r = new Rectangle (r.X + 0.5, r.Y + 0.5, r.Width-1, r.Height-1);

			g.Save ();

			g.MoveTo (r.X, r.Y);
			g.LineTo (r.X + r.Width, r.Y);
			g.LineTo (r.X + r.Width, r.Y + r.Height);
			g.LineTo (r.X, r.Y + r.Height);
			g.LineTo (r.X, r.Y);

			g.SetSourceColor (color);
			g.LineWidth = lineWidth;
			g.LineCap = LineCap.Square;

			Rectangle dirty = g.FixedStrokeExtents ();
			g.Stroke ();

			g.Restore ();

			return dirty;
		}

		public static Rectangle DrawFullRectangle(this Context g, Rectangle r, Color color, int lineWidth)
		{
			// Put it on a pixel line
			if (lineWidth == 1)
				r = new Rectangle(r.X + 0.5, r.Y + 0.5, r.Width - 1, r.Height - 1);

			g.Save();

			g.MoveTo(r.X - (double)lineWidth / 2d, r.Y);
			g.LineTo(r.X + r.Width, r.Y);
			g.LineTo(r.X + r.Width, r.Y + r.Height);
			g.LineTo(r.X, r.Y + r.Height);
			g.LineTo(r.X, r.Y);

			g.SetSourceColor (color);
			g.LineWidth = lineWidth;

			Rectangle dirty = g.FixedStrokeExtents();
			g.Stroke();

			g.Restore();

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

			g.SetSourceColor (color);

			Rectangle dirty = g.FixedStrokeExtents ();

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

			g.SetSource (pattern);

			Rectangle dirty = g.FixedStrokeExtents ();
			g.Fill ();

			g.Restore ();

			return dirty;
		}

        public static Rectangle FillRectangle (this Context g, Rectangle r, Pattern pattern, PointD patternOffset)
        {
            g.Save ();

            g.MoveTo (r.X, r.Y);
            g.LineTo (r.X + r.Width, r.Y);
            g.LineTo (r.X + r.Width, r.Y + r.Height);
            g.LineTo (r.X, r.Y + r.Height);
            g.LineTo (r.X, r.Y);

            pattern.Matrix.Translate (-patternOffset.X, -patternOffset.Y);
            g.SetSource (pattern);

            Rectangle dirty = g.FixedStrokeExtents ();
            g.Fill ();

            g.Restore ();

            return dirty;
        }

		public static Rectangle DrawPolygonal (this Context g, PointD[] points, Color color)
		{
			g.Save ();
			g.MoveTo (points[0]);
			foreach (var point in points) {
				g.LineTo (point.X, point.Y);
			}

			g.SetSourceColor (color);

			Rectangle dirty = g.FixedStrokeExtents ();
			g.Stroke ();

			g.Restore ();

			return dirty;
		}

		public static Rectangle FillPolygonal (this Context g, PointD[] points, Color color)
		{
			g.Save ();

			g.MoveTo (points[0]);
			foreach (var point in points)
				g.LineTo (point);

			g.SetSourceColor (color);

			Rectangle dirty = g.FixedStrokeExtents ();
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

			g.SetSourceColor (fill);
			g.FillPreserve ();

			g.SetSourceColor (stroke);
			g.LineWidth = lineWidth;
			g.LineCap = LineCap.Square;

			Rectangle dirty = g.FixedStrokeExtents ();

			g.Stroke ();
			g.Restore ();

			return dirty;
		}

		public static Rectangle FillStrokedFullRectangle(this Context g, Rectangle r, Color fill, Color stroke, int lineWidth)
		{
			double x = r.X;
			double y = r.Y;

			g.Save();

			// Put it on a pixel line
			if (lineWidth == 1)
			{
				x += 0.5;
				y += 0.5;
			}

			g.MoveTo(x - (double)lineWidth / 2d, y);
			g.LineTo(x + r.Width, y);
			g.LineTo(x + r.Width, y + r.Height);
			g.LineTo(x, y + r.Height);
			g.LineTo(x, y);

			g.SetSourceColor (fill);
			g.FillPreserve();

			g.SetSourceColor (stroke);
			g.LineWidth = lineWidth;

			Rectangle dirty = g.FixedStrokeExtents();

			g.Stroke();
			g.Restore();

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

			g.SetSourceColor (color);
			g.LineWidth = lineWidth;

			Rectangle dirty = g.FixedStrokeExtents ();

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

			g.SetSourceColor (color);

			Rectangle dirty = g.FixedStrokeExtents ();

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

			g.SetSourceColor (fill);
			g.FillPreserve ();

			g.SetSourceColor (stroke);
			g.LineWidth = lineWidth;

			Rectangle dirty = g.FixedStrokeExtents ();

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

			g.SetSourceColor (fill);
			g.FillPreserve ();

			g.SetSourceColor (stroke);
			g.LineWidth = lineWidth;

			Rectangle dirty = g.FixedStrokeExtents ();

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

			g.SetSourceColor (fill);

			Rectangle dirty = g.FixedStrokeExtents ();

			g.Fill ();
			g.Restore ();

			return dirty;
		}

		public static void FillRegion (this Context g, Gdk.Region region, Color color)
		{
			g.Save ();

			g.SetSourceColor (color);

			foreach (Gdk.Rectangle r in region.GetRectangles ()) {
				g.MoveTo (r.X, r.Y);
				g.LineTo (r.X + r.Width, r.Y);
				g.LineTo (r.X + r.Width, r.Y + r.Height);
				g.LineTo (r.X, r.Y + r.Height);
				g.LineTo (r.X, r.Y);

				g.SetSourceColor (color);

				g.FixedStrokeExtents ();
				g.Fill ();
			}

			g.Restore ();
		}

		public static Rectangle DrawRoundedRectangle (this Context g, Rectangle r, double radius, Color stroke, int lineWidth)
		{
			g.Save ();

			Path p = g.CreateRoundedRectanglePath (r, radius);

			g.AppendPath (p);

			g.SetSourceColor (stroke);
			g.LineWidth = lineWidth;

			Rectangle dirty = g.FixedStrokeExtents ();

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

		public static void QuadraticCurveTo (this Context g, double x1, double y1, double x2, double y2)
		{
			var c_x = g.CurrentPoint.X;
			var c_y = g.CurrentPoint.Y;
			var cp1x = c_x + 2.0 / 3.0 * (x1 - c_x);
			var cp1y = c_y + 2.0 / 3.0 * (y1 - c_y);
			var cp2x = cp1x + (x2 - c_x) / 3.0;
			var cp2y = cp1y + (y2 - c_y) / 3.0;
			g.CurveTo (cp1x, cp1y, cp2x, cp2y, x2, y2);
		}

		public static Rectangle DrawLine (this Context g, PointD p1, PointD p2, Color color, int lineWidth)
		{
			// Put it on a pixel line
			if (lineWidth == 1){
				p1 = new PointD (p1.X + 0.5, p1.Y + 0.5);
				p2 = new PointD (p2.X + 0.5, p2.Y + 0.5);
			}

			g.Save ();

			g.MoveTo (p1.X, p1.Y);
			g.LineTo (p2.X, p2.Y);

			g.SetSourceColor (color);
			g.LineWidth = lineWidth;
			g.LineCap = LineCap.Square;

			Rectangle dirty = g.FixedStrokeExtents ();
			g.Stroke ();

			g.Restore ();

			return dirty;
		}

		/// <summary>
		/// Computes a bounding box in user coordinates covering the
		/// area that would be affected by a call to Context.Stroke()
		/// using the current stroke parameters.
		/// 
		/// The rectangle returned by Cairo.Context.StrokeExtents()
		/// incorrectly specifies the X and Y coordinates of the
		/// bottom-right corner of the Rectangle in the width and
		/// height members. This method corrects the rectangle to
		/// contain the width and height in the width and height members.
		/// 
		/// This can be removed once we port to GTK3.
		/// </summary>
		/// <returns>
		/// The rectangle describing the area that would be
		/// affected.
		/// </returns>
		public static Rectangle FixedStrokeExtents (this Context g)
		{
			double x1, y1, x2, y2;
			cairo_stroke_extents (g.Handle, out x1, out y1, out x2, out y2);
			return new Rectangle (x1, y1, x2 - x1, y2 - y1);
		}

		/// <summary>
		/// The Pattern property is now deprecated in favour of the SetSource (pattern) method,
		/// but that method doesn't exist in older versions of Mono.Cairo. This extension method
		/// provides an implementation of that functionality.
		/// 
		/// This can be removed once we port to GTK3.
		/// </summary>
		public static void SetSource (this Context g, Pattern source)
		{
#pragma warning disable 612
			cairo_set_source (g.Handle, source.Pointer);
#pragma warning restore 612
		}

		private const string CairoLib = "libcairo-2.dll";

		[DllImport (CairoLib, CallingConvention=CallingConvention.Cdecl)]
		private static extern void cairo_stroke_extents (IntPtr cr, out double x1, out double y1, out double x2, out double y2);

		[DllImport (CairoLib, CallingConvention=CallingConvention.Cdecl)]
		private static extern void cairo_set_source (IntPtr cr, IntPtr pattern);

		private static Pango.Style CairoToPangoSlant (FontSlant slant)
		{
			switch (slant) {
				case FontSlant.Italic:
					return Pango.Style.Italic;
				case FontSlant.Oblique:
					return Pango.Style.Oblique;
				default:
					return Pango.Style.Normal;
			}
		}

		private static Pango.Weight CairoToPangoWeight (FontWeight weight)
		{
			return (weight == FontWeight.Bold) ? Pango.Weight.Bold : Pango.Weight.Normal;
		}

		public static Rectangle DrawText (this Context g, PointD p, string family, FontSlant slant, FontWeight weight, double size, Color color, string text, bool antiAliasing)
		{
			g.Save ();

			g.MoveTo (p.X, p.Y);
			g.SetSourceColor (color);
			g.Antialias = antiAliasing ? Antialias.Subpixel : Antialias.None;

			Pango.Layout layout = Pango.CairoHelper.CreateLayout (g);
			Pango.FontDescription fd = new Pango.FontDescription ();
			fd.Family = family;
			fd.Style = CairoToPangoSlant (slant);
			fd.Weight = CairoToPangoWeight (weight);
			fd.AbsoluteSize = size * Pango.Scale.PangoScale;
			layout.FontDescription = fd;
			layout.SetText (text);
			Pango.CairoHelper.ShowLayoutLine (g, layout.Lines[0]);

			Pango.Rectangle unused = Pango.Rectangle.Zero;
			Pango.Rectangle te = Pango.Rectangle.Zero;
			layout.GetExtents (out unused, out te);

			(layout as IDisposable).Dispose ();

			g.Restore ();

			return new Rectangle (te.X, te.Y, te.Width, te.Height);
		}

		public static void DrawPixbuf (this Context g, Gdk.Pixbuf pixbuf, Point dest)
		{
			g.Save ();

			Gdk.CairoHelper.SetSourcePixbuf (g, pixbuf, dest.X, dest.Y);
			g.Paint ();
			g.Restore ();
		}

		public static void DrawLinearGradient (this Context g, Surface oldsurface, GradientColorMode mode, Color c1, Color c2, PointD p1, PointD p2)
		{
			g.Save ();

			Gradient gradient = new Cairo.LinearGradient (p1.X, p1.Y, p2.X, p2.Y);

			if (mode == GradientColorMode.Color) {
				gradient.AddColorStop (0, c1);
				gradient.AddColorStop (1, c2);
				g.SetSource (gradient);
				g.Paint ();
			} else if (mode == GradientColorMode.Transparency) {
				gradient.AddColorStop (0, new Color (0, 0, 0, 1));
				gradient.AddColorStop (1, new Color (0, 0, 0, 0));
				g.SetSource (new SurfacePattern (oldsurface));
				g.Mask (gradient);
			}

			g.Restore ();
		}

		public static void DrawLinearReflectedGradient (this Context g, Surface oldsurface, GradientColorMode mode, Color c1, Color c2, PointD p1, PointD p2)
		{
			g.Save ();

			Gradient gradient = new Cairo.LinearGradient (p1.X, p1.Y, p2.X, p2.Y);

			if (mode == GradientColorMode.Color) {
				gradient.AddColorStop (0, c1);
				gradient.AddColorStop (0.5, c2);
				gradient.AddColorStop (1, c1);
				g.SetSource (gradient);
				g.Paint ();
			} else if (mode == GradientColorMode.Transparency) {
				gradient.AddColorStop (0, new Color (0, 0, 0, 1));
				gradient.AddColorStop (0.5, new Color (0, 0, 0, 0));
				gradient.AddColorStop (1, new Color (0, 0, 0, 1));
				g.SetSource (new SurfacePattern (oldsurface));
				g.Mask (gradient);
			}

			g.Restore ();
		}

		public static void DrawRadialGradient (this Context g, Surface oldsurface, GradientColorMode mode, Color c1, Color c2, PointD p1, PointD p2, double r1, double r2)
		{
			g.Save ();

			Gradient gradient = new Cairo.RadialGradient (p1.X, p1.Y, r1, p2.X, p2.Y, r2);

			if (mode == GradientColorMode.Color) {
				gradient.AddColorStop (0, c1);
				gradient.AddColorStop (1, c2);
				g.SetSource (gradient);
				g.Paint ();
			} else if (mode == GradientColorMode.Transparency) {
				gradient.AddColorStop (0, new Color (0, 0, 0, 1));
				gradient.AddColorStop (1, new Color (0, 0, 0, 0));
				g.SetSource (new SurfacePattern (oldsurface));
				g.Mask (gradient);
			}

			g.Restore ();
		}

		// The Color property is deprecated, so use this extension method until SetSourceColor is officially available everywhere.
		public static void SetSourceColor (this Context g, Color c)
		{
			g.SetSourceRGBA (c.R, c.G, c.B, c.A);
		}
		#endregion

		public static double Distance (this PointD s, PointD e)
		{
			return Magnitude (new PointD (s.X - e.X, s.Y - e.Y));
		}

		public static double Magnitude (this PointD p)
		{
			return Math.Sqrt (p.X * p.X + p.Y * p.Y);
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
			return new Gdk.Rectangle ((int)Math.Floor (r.X), (int)Math.Floor (r.Y),
			                          (int)Math.Ceiling (r.Width), (int)Math.Ceiling (r.Height));
		}

		public static bool ContainsPoint (this Cairo.Rectangle r, double x, double y)
		{
			if (x < r.X || x >= r.X + r.Width)
				return false;

			if (y < r.Y || y >= r.Y + r.Height)
				return false;

			return true;
		}

		public static bool ContainsPoint (this Cairo.Rectangle r, Cairo.PointD point)
		{
			return ContainsPoint (r, point.X, point.Y);
		}

		public unsafe static Gdk.Pixbuf ToPixbuf (this Cairo.ImageSurface surfSource)
		{
			using (Cairo.ImageSurface surf = surfSource.Clone ()) {
				surf.Flush ();

				ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;
				int len = surf.Data.Length / 4;

				for (int i = 0; i < len; i++) {
					if (dstPtr->A != 0)
						*dstPtr = (ColorBgra.FromBgra ((byte)(dstPtr->R * 255 / dstPtr->A), (byte)(dstPtr->G * 255 / dstPtr->A), (byte)(dstPtr->B * 255 / dstPtr->A), dstPtr->A));
					dstPtr++;
				}

				Gdk.Pixbuf pb = new Gdk.Pixbuf (surf.Data, true, 8, surf.Width, surf.Height, surf.Stride);
				return pb;
			}
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

		public unsafe static void SetPixel (this Cairo.ImageSurface surf, ColorBgra* surfDataPtr, int surfWidth, int x, int y, Color color)
		{
			ColorBgra* dstPtr = surfDataPtr;

			dstPtr += (x) + (y * surfWidth);

			dstPtr->R = (byte)(color.R * 255);
			dstPtr->G = (byte)(color.G * 255);
			dstPtr->B = (byte)(color.B * 255);
			dstPtr->A = (byte)(color.A * 255);
		}

		public unsafe static ColorBgra GetColorBgraUnchecked (this Cairo.ImageSurface surf, int x, int y)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;

			dstPtr += (x) + (y * surf.Width);

			return *dstPtr;
		}

		public unsafe static void SetColorBgra (this Cairo.ImageSurface surf, ColorBgra color, int x, int y)
		{
			if (x > surf.Width || y > surf.Height)
				throw new ArgumentOutOfRangeException ("Invalid canvas coordinates");

			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;

			dstPtr += (x) + (y * surf.Width);

			*dstPtr = color;
		}

		/// <summary>
		/// For performance reasons, this method does not check that the x and y coordinates are
		/// within the bounds of the image.
		/// </summary>
		public unsafe static void SetColorBgraUnchecked (this Cairo.ImageSurface surf, ColorBgra* surfDataPtr, int surfWidth, ColorBgra color, int x, int y)
		{
			ColorBgra* dstPtr = surfDataPtr;

			dstPtr += (x) + (y * surfWidth);

			*dstPtr = color;
		}

		public unsafe static ColorBgra GetColorBgraUnchecked (this Cairo.ImageSurface surf, ColorBgra* surfDataPtr, int surfWidth, int x, int y)
		{
			ColorBgra* dstPtr = surfDataPtr;

			dstPtr += (x) + (y * surfWidth);

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

		public static Gdk.Color ToGdkColor (this ColorBgra color)
		{
			Gdk.Color c = new Gdk.Color (color.R, color.G, color.B);

			return c;
		}

		public static string ToString2 (this Cairo.Color c)
		{
			return string.Format ("R: {0} G: {1} B: {2} A: {3}", c.R, c.G, c.B, c.A);
		}

		public static string ToString2 (this Cairo.PointD c)
		{
			return string.Format ("{0}, {1}", c.X, c.Y);
		}

		public static uint ToUint (this Cairo.Color c)
		{
			return Pinta.Core.ColorBgra.BgraToUInt32 ((int)(c.B * 255), (int)(c.R * 255), (int)(c.G * 255), (int)(c.A * 255));
		}

		public static Gdk.Size ToSize (this Cairo.Point point)
		{
			return new Gdk.Size (point.X, point.Y);
		}

		public static ImageSurface Clone (this ImageSurface surf)
		{
			if (PintaCore.Workspace.HasOpenDocuments)
				PintaCore.Workspace.ActiveDocument.SignalSurfaceCloned ();

			ImageSurface newsurf = new ImageSurface (surf.Format, surf.Width, surf.Height);

			using (Context g = new Context (newsurf)) {
				g.SetSource (surf);
				g.Paint ();
			}

			return newsurf;
		}

		public static unsafe bool ContainsTranslucent (this ImageSurface surf)
		{
			bool ret = false;

			ColorBgra* ptr = (ColorBgra*)surf.DataPtr;
			int width = surf.Width;

			for (int x = 0; x < width; x++)
				for (int y = 0; y < surf.Height; y++) {
					int a = (int)surf.GetColorBgraUnchecked (ptr, width, x, y).A;

					if (a > 0 && a < 255) {
						Console.WriteLine (surf.GetColorBgraUnchecked (ptr, width, x, y).ToString ());
						ret = true;
					}
				}

			return ret;
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

		public static void Clear (this ImageSurface surface)
		{
			using (Context g = new Context (surface)) {
				g.Operator = Operator.Clear;
				g.Paint ();
			}
		}

        public static void Clear (this Context g, Rectangle roi)
        {
            g.Save ();
            g.Rectangle (roi.X, roi.Y, roi.Width, roi.Height);
            g.Clip ();
            g.Operator = Operator.Clear;
            g.Paint ();
            g.Restore ();
        }

        [DllImport(CairoLib, CallingConvention = CallingConvention.Cdecl)]
        private static extern void cairo_path_extents(IntPtr cr, out double x1, out double y1, out double x2, out double y2);

        public static Gdk.Rectangle GetBounds(this Path path)
        {
            Rectangle rect;

            using (Context g = new Context(PintaCore.Layers.CurrentLayer.Surface))
            {
                g.AppendPath(path);

                double x1, y1, x2, y2;
                cairo_path_extents(g.Handle, out x1, out y1, out x2, out y2);
                rect = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            }

            int x = (int)Math.Round(rect.X);
            int y = (int)Math.Round(rect.Y);
            int w = (int)Math.Round(rect.Width);
            int h = (int)Math.Round(rect.Height);

            return new Gdk.Rectangle(x, y, w, h);
        }

		public static Color Clone(this Color color)
		{
			return new Color(color.R, color.G, color.B, color.A);
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

		/// <summary>
		/// Creates a copy of the rectangle but with the side edges
		/// moved out from the center by dx, and the top and bottom
		/// edges moved out from the center by dy.  Edges are moved
		/// inwards when dx or dy are negative.
		/// </summary>
		/// <param name="rect">The source rectangle.</param>
		/// <param name="dx">Distance to move sides from center.</param>
		/// <param name="dy">Distance to move top and bottom from center.</param>
		/// <returns>The inflated (or deflated) rectangle.</returns>
		public static Rectangle Inflate (this Rectangle rect, int dx, int dy)
		{
			return new Rectangle(rect.X - dx, rect.Y - dy, rect.Width + 2 * dx, rect.Height + 2 * dy);
		}

		/// <summary>
		/// Determines if the requested pixel coordinate is within bounds.
		/// </summary>
		/// <param name="surf">The image surface to check against.</param>
		/// <param name="x">The x coordinate.</param>
		/// <param name="y">The y coordinate.</param>
		/// <returns>true if (x,y) is in bounds, false if it's not.</returns>
		public static bool IsVisible (this ImageSurface surf, int x, int y)
		{
			return x >= 0 && x < surf.Width && y >= 0 && y < surf.Height;
		}


		public static unsafe ColorBgra* GetPointAddressUnchecked (this ImageSurface surf, int x, int y)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;

			dstPtr += (x) + (y * surf.Width);

			return dstPtr;
		}

		public static unsafe ColorBgra* GetPointAddressUnchecked (this ImageSurface surf, ColorBgra* surfDataPtr, int surfWidth, int x, int y)
		{
			ColorBgra* dstPtr = surfDataPtr;

			dstPtr += (x) + (y * surfWidth);

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
		public static unsafe ColorBgra GetPointUnchecked (this ImageSurface surf, ColorBgra* surfDataPtr, int surfWidth, int x, int y)
		{
			ColorBgra* dstPtr = surfDataPtr;

			dstPtr += (x) + (y * surfWidth);

			return *dstPtr;
		}

		public static unsafe ColorBgra* GetRowAddressUnchecked (this ImageSurface surf, int y)
		{
			ColorBgra* dstPtr = (ColorBgra*)surf.DataPtr;

			dstPtr += y * surf.Width;

			return dstPtr;
		}

		public static unsafe ColorBgra* GetRowAddressUnchecked (this ImageSurface surf, ColorBgra* surfDataPtr, int surfWidth, int y)
		{
			ColorBgra* dstPtr = surfDataPtr;

			dstPtr += y * surfWidth;

			return dstPtr;
		}

		public static unsafe ColorBgra* GetPointAddress (this ImageSurface surf, int x, int y)
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


		/// <summary>
		/// There was a bug in gdk-sharp where this returns incorrect values.
		/// We will probably have to use this for a long time until every distro
		/// has an updated gdk.
		/// </summary>
		public static bool ContainsCorrect (this Gdk.Rectangle r, int x, int y)
		{
			return ((((x >= r.Left) && (x <= r.GetRight ())) && (y >= r.Top)) && (y <= r.GetBottom ()));
		}

		/// <summary>
		/// There was a bug in gdk-sharp where this returns incorrect values.
		/// We will probably have to use this for a long time until every distro
		/// has an updated gdk.
		/// </summary>
		public static bool ContainsCorrect (this Gdk.Rectangle r, Gdk.Point pt)
		{
			return r.ContainsCorrect (pt.X, pt.Y);
		}

		public static unsafe ColorBgra GetBilinearSample (this ImageSurface src, float x, float y)
		{
			return GetBilinearSample (src, (ColorBgra*)src.DataPtr, src.Width, src.Height, x, y);
		}

		public static unsafe ColorBgra GetBilinearSample (this ImageSurface src, ColorBgra* srcDataPtr, int srcWidth, int srcHeight, float x, float y)
		{
			if (!Utility.IsNumber (x) || !Utility.IsNumber (y))
				return ColorBgra.Transparent;

			float u = x;
			float v = y;

			if (u >= 0 && v >= 0 && u < srcWidth && v < srcHeight) {
				unchecked {
					int iu = (int)Math.Floor (u);
					uint sxfrac = (uint)(256 * (u - (float)iu));
					uint sxfracinv = 256 - sxfrac;

					int iv = (int)Math.Floor (v);
					uint syfrac = (uint)(256 * (v - (float)iv));
					uint syfracinv = 256 - syfrac;

					uint wul = (uint)(sxfracinv * syfracinv);
					uint wur = (uint)(sxfrac * syfracinv);
					uint wll = (uint)(sxfracinv * syfrac);
					uint wlr = (uint)(sxfrac * syfrac);

					int sx = iu;
					int sy = iv;
					int sleft = sx;
					int sright;

					if (sleft == (srcWidth - 1))
						sright = sleft;
					else
						sright = sleft + 1;

					int stop = sy;
					int sbottom;

					if (stop == (srcHeight - 1))
						sbottom = stop;
					else
						sbottom = stop + 1;

					ColorBgra* cul = src.GetPointAddressUnchecked (srcDataPtr, srcWidth, sleft, stop);
					ColorBgra* cur = cul + (sright - sleft);
					ColorBgra* cll = src.GetPointAddressUnchecked (srcDataPtr, srcWidth, sleft, sbottom);
					ColorBgra* clr = cll + (sright - sleft);

					ColorBgra c = ColorBgra.BlendColors4W16IP (*cul, wul, *cur, wur, *cll, wll, *clr, wlr);
					return c;
				}
			} else {
				return ColorBgra.FromUInt32 (0);
			}
		}

		public static unsafe ColorBgra GetBilinearSampleClamped (this ImageSurface src, float x, float y)
		{
			return GetBilinearSampleClamped (src, (ColorBgra*)src.DataPtr, src.Width, src.Height, x, y);
		}

		public static unsafe ColorBgra GetBilinearSampleClamped (this ImageSurface src, ColorBgra* srcDataPtr, int srcWidth, int srcHeight, float x, float y)
		{
			if (!Utility.IsNumber (x) || !Utility.IsNumber (y))
				return ColorBgra.Transparent;

			float u = x;
			float v = y;

			if (u < 0)
				u = 0;
			else if (u > srcWidth - 1)
				u = srcWidth - 1;

			if (v < 0)
				v = 0;
			else if (v > srcHeight - 1)
				v = srcHeight - 1;

			unchecked {
				int iu = (int)Math.Floor (u);
				uint sxfrac = (uint)(256 * (u - (float)iu));
				uint sxfracinv = 256 - sxfrac;

				int iv = (int)Math.Floor (v);
				uint syfrac = (uint)(256 * (v - (float)iv));
				uint syfracinv = 256 - syfrac;

				uint wul = (uint)(sxfracinv * syfracinv);
				uint wur = (uint)(sxfrac * syfracinv);
				uint wll = (uint)(sxfracinv * syfrac);
				uint wlr = (uint)(sxfrac * syfrac);

				int sx = iu;
				int sy = iv;
				int sleft = sx;
				int sright;

				if (sleft == (srcWidth - 1))
					sright = sleft;
				else
					sright = sleft + 1;

				int stop = sy;
				int sbottom;

				if (stop == (srcHeight - 1))
					sbottom = stop;
				else
					sbottom = stop + 1;

				ColorBgra* cul = src.GetPointAddressUnchecked (srcDataPtr, srcWidth, sleft, stop);
				ColorBgra* cur = cul + (sright - sleft);
				ColorBgra* cll = src.GetPointAddressUnchecked (srcDataPtr, srcWidth, sleft, sbottom);
				ColorBgra* clr = cll + (sright - sleft);

				ColorBgra c = ColorBgra.BlendColors4W16IP (*cul, wul, *cur, wur, *cll, wll, *clr, wlr);
				return c;
			}
		}

		public static unsafe ColorBgra GetBilinearSampleWrapped (this ImageSurface src, float x, float y)
		{
			return GetBilinearSampleWrapped (src, (ColorBgra*)src.DataPtr, src.Width, src.Height, x, y);
		}

		public static unsafe ColorBgra GetBilinearSampleWrapped (this ImageSurface src, ColorBgra* srcDataPtr, int srcWidth, int srcHeight, float x, float y)
		{
			if (!Utility.IsNumber (x) || !Utility.IsNumber (y))
				return ColorBgra.Transparent;

			float u = x;
			float v = y;

			unchecked {
				int iu = (int)Math.Floor (u);
				uint sxfrac = (uint)(256 * (u - (float)iu));
				uint sxfracinv = 256 - sxfrac;

				int iv = (int)Math.Floor (v);
				uint syfrac = (uint)(256 * (v - (float)iv));
				uint syfracinv = 256 - syfrac;

				uint wul = (uint)(sxfracinv * syfracinv);
				uint wur = (uint)(sxfrac * syfracinv);
				uint wll = (uint)(sxfracinv * syfrac);
				uint wlr = (uint)(sxfrac * syfrac);

				int sx = iu;
				if (sx < 0)
					sx = (srcWidth - 1) + ((sx + 1) % srcWidth);
				else if (sx > (srcWidth - 1))
					sx = sx % srcWidth;

				int sy = iv;
				if (sy < 0)
					sy = (srcHeight - 1) + ((sy + 1) % srcHeight);
				else if (sy > (srcHeight - 1))
					sy = sy % srcHeight;

				int sleft = sx;
				int sright;

				if (sleft == (srcWidth - 1))
					sright = 0;
				else
					sright = sleft + 1;

				int stop = sy;
				int sbottom;

				if (stop == (srcHeight - 1))
					sbottom = 0;
				else
					sbottom = stop + 1;

				ColorBgra cul = src.GetPointUnchecked (srcDataPtr, srcWidth, sleft, stop);
				ColorBgra cur = src.GetPointUnchecked (srcDataPtr, srcWidth, sright, stop);
				ColorBgra cll = src.GetPointUnchecked (srcDataPtr, srcWidth, sleft, sbottom);
				ColorBgra clr = src.GetPointUnchecked (srcDataPtr, srcWidth, sright, sbottom);

				ColorBgra c = ColorBgra.BlendColors4W16IP (cul, wul, cur, wur, cll, wll, clr, wlr);

				return c;
			}
		}

		public static void TranslatePointsInPlace (this Point[] Points, int dx, int dy)
		{
			for (int i = 0; i < Points.Length; ++i) {
				Points[i].X += dx;
				Points[i].Y += dy;
			}
		}

		private struct Edge
		{
			public int miny;   // int
			public int maxy;   // int
			public int x;      // fixed point: 24.8
			public int dxdy;   // fixed point: 24.8

			public Edge (int miny, int maxy, int x, int dxdy)
			{
				this.miny = miny;
				this.maxy = maxy;
				this.x = x;
				this.dxdy = dxdy;
			}
		}

		public static Scanline[] GetScans (this Point[] points)
		{
			int ymax = 0;

			// Build edge table
			Edge[] edgeTable = new Edge[points.Length];
			int edgeCount = 0;

			for (int i = 0; i < points.Length; ++i) {
				Point top = points[i];
				Point bottom = points[(i + 1) % points.Length];
				int dy;

				if (top.Y > bottom.Y) {
					Point temp = top;
					top = bottom;
					bottom = temp;
				}

				dy = bottom.Y - top.Y;

				if (dy != 0) {
					edgeTable[edgeCount] = new Edge (top.Y, bottom.Y, top.X << 8, (((bottom.X - top.X) << 8) / dy));
					ymax = Math.Max (ymax, bottom.Y);
					++edgeCount;
				}
			}

			// Sort edge table by miny
			for (int i = 0; i < edgeCount - 1; ++i) {
				int min = i;

				for (int j = i + 1; j < edgeCount; ++j)
					if (edgeTable[j].miny < edgeTable[min].miny)
						min = j;

				if (min != i) {
					Edge temp = edgeTable[min];
					edgeTable[min] = edgeTable[i];
					edgeTable[i] = temp;
				}
			}

			// Compute how many scanlines we will be emitting
			int scanCount = 0;
			int activeLow = 0;
			int activeHigh = 0;
			int yscan1 = edgeTable[0].miny;

			// we assume that edgeTable[0].miny == yscan
			while (activeHigh < edgeCount - 1 &&
			       edgeTable[activeHigh + 1].miny == yscan1) {
				++activeHigh;
			}

			while (yscan1 <= ymax) {
				// Find new edges where yscan == miny
				while (activeHigh < edgeCount - 1 &&
				       edgeTable[activeHigh + 1].miny == yscan1) {
					++activeHigh;
				}

				int count = 0;
				for (int i = activeLow; i <= activeHigh; ++i) {
					if (edgeTable[i].maxy > yscan1) {
						++count;
					}
				}

				scanCount += count / 2;
				++yscan1;

				// Remove edges where yscan == maxy
				while (activeLow < edgeCount - 1 &&
				       edgeTable[activeLow].maxy <= yscan1) {
					++activeLow;
				}

				if (activeLow > activeHigh)
					activeHigh = activeLow;
			}

			// Allocate scanlines that we'll return
			Scanline[] scans = new Scanline[scanCount];

			// Active Edge Table (AET): it is indices into the Edge Table (ET)
			int[] active = new int[edgeCount];
			int activeCount = 0;
			int yscan2 = edgeTable[0].miny;
			int scansIndex = 0;

			// Repeat until both the ET and AET are empty
			while (yscan2 <= ymax) {
				// Move any edges from the ET to the AET where yscan == miny
				for (int i = 0; i < edgeCount; ++i) {
					if (edgeTable[i].miny == yscan2) {
						active[activeCount] = i;
						++activeCount;
					}
				}

				// Sort the AET on x
				for (int i = 0; i < activeCount - 1; ++i) {
					int min = i;

					for (int j = i + 1; j < activeCount; ++j)
						if (edgeTable[active[j]].x < edgeTable[active[min]].x)
							min = j;

					if (min != i) {
						int temp = active[min];
						active[min] = active[i];
						active[i] = temp;
					}
				}

				// For each pair of entries in the AET, fill in pixels between their info
				for (int i = 0; i < activeCount; i += 2) {
					Edge el = edgeTable[active[i]];
					Edge er = edgeTable[active[i + 1]];
					int startx = (el.x + 0xff) >> 8; // ceil(x)
					int endx = er.x >> 8;      // floor(x)

					scans[scansIndex] = new Scanline (startx, yscan2, endx - startx);
					++scansIndex;
				}

				++yscan2;

				// Remove from the AET any edge where yscan == maxy
				int k = 0;
				while (k < activeCount && activeCount > 0) {
					if (edgeTable[active[k]].maxy == yscan2) {
						// remove by shifting everything down one
						for (int j = k + 1; j < activeCount; ++j)
							active[j - 1] = active[j];

						--activeCount;
					} else {
						++k;
					}
				}

				// Update x for each entry in AET
				for (int i = 0; i < activeCount; ++i)
					edgeTable[active[i]].x += edgeTable[active[i]].dxdy;
			}

			return scans;
		}

		public static Path CreatePolygonPath (this Context g, Point[][] polygonSet)
		{
			g.Save ();
			Point p;

			for (int i = 0; i < polygonSet.Length; i++) {
				if (polygonSet[i].Length == 0)
					continue;

				p = polygonSet[i][0];
				g.MoveTo (p.X, p.Y);

				for (int j = 1; j < polygonSet[i].Length; j++) {
					p = polygonSet[i][j];
					g.LineTo (p.X, p.Y);
				}

				g.ClosePath ();
			}

			Path path = g.CopyPath ();

			g.Restore ();

			return path;
		}

		public static Gdk.Point ToGdkPoint (this PointD point)
		{
			return new Gdk.Point ((int)point.X, (int)point.Y);
		}

		public static bool IsEmpty (this PointD point)
		{
			return point.X == 0 && point.Y == 0;
		}

		public static void TransformPoint(this Matrix matrix, ref PointD point)
		{
			double x = point.X;
			double y = point.Y;

			matrix.TransformPoint(ref x, ref y);

			point.X = x;
			point.Y = y;
		}

		public static void InitMatrix(this Matrix matrix, Matrix source)
		{
			matrix.X0 = source.X0;
			matrix.Xx = source.Xx;
			matrix.Xy = source.Xy;

			matrix.Y0 = source.Y0;
			matrix.Yx = source.Yx;
			matrix.Yy = source.Yy;
		}

		public static void InitRectToRect(this Matrix matrix, Rectangle src, Rectangle dst)
		{
			matrix.InitIdentity();

			matrix.Xx = dst.Width / src.Width;
			matrix.Yy = dst.Height / src.Height;
			matrix.X0 = dst.X - (matrix.Xx * src.X);
			matrix.Y0 = dst.Y - (matrix.Yy * src.Y);
		}

		public static Rectangle FromLTRB(double left, double top, double right, double bottom)
		{
			return new Rectangle(Math.Min(left, right), Math.Min(top, bottom),
			                     Math.Abs(right - left), Math.Abs(bottom - top));
		}

		public static PointD GetCenter(this Cairo.Rectangle rect)
		{
			return new PointD(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
		}

		/// <summary>
		/// Computes and returns the Union (largest possible combination) of two Rectangles.
		/// The two given Rectangles do not need to intersect.
		/// 
		/// Another way to understand this function is that it computes and returns the
		/// smallest possible Rectangle that encompasses both given Rectangles.
		/// 
		/// This function works as is intuitively expected with neither, either, or both given Rectangles being null.
		/// </summary>
		/// <param name="r1">The first given Rectangle.</param>
		/// <param name="r2">The second given Rectangle.</param>
		/// <returns></returns>
		public static Rectangle? UnionRectangles(this Rectangle? r1, Rectangle? r2)
		{
			if (r1 == null)
			{
				//r2 is the only given Rectangle that could still have a value, and if it's null, return that anyways.
				return r2;
			}
			else if (r2 == null)
			{
				//Only r1 has a value.
				return r1;
			}
			else
			{
				//Both r1 and r2 have values.

				//Calculate the left-most and top-most values.
				double minX = Math.Min(r1.Value.X, r2.Value.X);
				double minY = Math.Min(r1.Value.Y, r2.Value.Y);

				//Calculate the right-most and bottom-most values and subtract the left-most and top-most values from them to get the width and height.
				return new Rectangle(minX, minY,
					Math.Max(r1.Value.X + r1.Value.Width, r2.Value.X + r2.Value.Width) - minX,
					Math.Max(r1.Value.Y + r1.Value.Height, r2.Value.Y + r2.Value.Height) - minY);
			}
		}

        public static Pattern ToTiledPattern (this Surface surface)
        {
            var pattern = new Cairo.SurfacePattern (surface);
            pattern.Extend = Extend.Repeat;

            return pattern;
        }

        public static void BlendSurface (this Context g, Surface src, BlendMode mode = BlendMode.Normal, double opacity = 1.0)
        {
            g.Save ();
            g.SetBlendMode (mode);
            g.SetSourceSurface (src, 0, 0);
            g.PaintWithAlpha (opacity);
            g.Restore ();
        }

        public static void BlendSurface (this Context g, Surface src, Rectangle roi, BlendMode mode = BlendMode.Normal, double opacity = 1.0)
        {
            g.Save ();
            g.Rectangle (roi);
            g.Clip ();
            g.SetBlendMode (mode);
            g.SetSourceSurface (src, 0, 0);
            g.PaintWithAlpha (opacity);
            g.Restore ();
        }

        public static void BlendSurface (this Context g, Surface src, Point offset, BlendMode mode = BlendMode.Normal, double opacity = 1.0)
        {
            g.Save ();
            g.Translate (offset.X, offset.Y);
            g.SetBlendMode (mode);
            g.SetSourceSurface (src, 0, 0);
            g.PaintWithAlpha (opacity);
            g.Restore ();
        }

        public static void SetBlendMode (this Context g, BlendMode mode)
        {
            switch (mode) {
                case BlendMode.Normal:
                    g.Operator = Operator.Over;
                    break;
                case BlendMode.Multiply:
                    g.Operator = (Operator)ExtendedOperators.Multiply;
                    break;
                case BlendMode.ColorBurn:
                    g.Operator = (Operator)ExtendedOperators.ColorBurn;
                    break;
                case BlendMode.ColorDodge:
                    g.Operator = (Operator)ExtendedOperators.ColorDodge;
                    break;
                case BlendMode.HardLight:
                    g.Operator = (Operator)ExtendedOperators.HardLight;
                    break;
                case BlendMode.SoftLight:
                    g.Operator = (Operator)ExtendedOperators.SoftLight;
                    break;
                case BlendMode.Overlay:
                    g.Operator = (Operator)ExtendedOperators.Overlay;
                    break;
                case BlendMode.Difference:
                    g.Operator = (Operator)ExtendedOperators.Difference;
                    break;
                case BlendMode.Color:
                    g.Operator = (Operator)ExtendedOperators.HslColor;
                    break;
                case BlendMode.Luminosity:
                    g.Operator = (Operator)ExtendedOperators.HslLuminosity;
                    break;
                case BlendMode.Hue:
                    g.Operator = (Operator)ExtendedOperators.HslHue;
                    break;
                case BlendMode.Saturation:
                    g.Operator = (Operator)ExtendedOperators.HslSaturation;
                    break;
                case BlendMode.Lighten:
                    g.Operator = (Operator)ExtendedOperators.Lighten;
                    break;
                case BlendMode.Darken:
                    g.Operator = (Operator)ExtendedOperators.Darken;
                    break;
                case BlendMode.Screen:
                    g.Operator = (Operator)ExtendedOperators.Screen;
                    break;
                case BlendMode.Xor:
                    g.Operator = Operator.Xor;
                    break;
            }
        }

        public enum ExtendedOperators
        {
            Clear = 0,

            Source = 1,
            SourceOver = 2,
            SourceIn = 3,
            SourceOut = 4,
            SourceAtop = 5,

            Destination = 6,
            DestinationOver = 7,
            DestinationIn = 8,
            DestinationOut = 9,
            DestinationAtop = 10,

            Xor = 11,
            Add = 12,
            Saturate = 13,

            Multiply = 14,
            Screen = 15,
            Overlay = 16,
            Darken = 17,
            Lighten = 18,
            ColorDodge = 19,
            ColorBurn = 20,
            HardLight = 21,
            SoftLight = 22,
            Difference = 23,
            Exclusion = 24,
            HslHue = 25,
            HslSaturation = 26,
            HslColor = 27,
            HslLuminosity = 28
        }
	}
}
