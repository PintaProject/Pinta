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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Cairo;

// TODO-GTK4 (bindings, unsubmitted) - should this be added to gir.core?
namespace Cairo
{
	public readonly record struct Color (
		double R,
		double G,
		double B,
		double A)
	{
		public Color (double r, double g, double b)
			: this (r, g, b, 1.0)
		{ }
	}
}

namespace Pinta.Core
{
	public static partial class CairoExtensions
	{
		private const string CairoLibraryName = "cairo-graphics";

		static CairoExtensions ()
		{
			NativeImportResolver.RegisterLibrary (CairoLibraryName,
				windowsLibraryName: "libcairo-2.dll",
				linuxLibraryName: "libcairo.so.2",
				osxLibraryName: "libcairo.2.dylib"
			);
		}

		// Most of these functions return an affected area
		// This can be ignored if you don't need it

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD DrawRectangle (
			this Context g,
			RectangleD r,
			Color color,
			int lineWidth)
		{
			RectangleD e = // Effective rectangle
				(lineWidth == 1)
				? new RectangleD (r.X + 0.5, r.Y + 0.5, r.Width - 1, r.Height - 1) // Put it on a pixel line
				: r;

			g.Save ();

			g.MoveTo (e.X, e.Y);
			g.LineTo (e.X + e.Width, e.Y);
			g.LineTo (e.X + e.Width, e.Y + e.Height);
			g.LineTo (e.X, e.Y + e.Height);
			g.LineTo (e.X, e.Y);

			g.SetSourceColor (color);
			g.LineWidth = lineWidth;
			g.LineCap = LineCap.Square;

			RectangleD dirty = g.StrokeExtents ();
			g.Stroke ();

			g.Restore ();

			return dirty;
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static Path CreateRectanglePath (
			this Context g,
			RectangleD r)
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

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD StrokeExtents (this Context g)
		{
			g.StrokeExtents (
				out double x1,
				out double y1,
				out double x2,
				out double y2);

			return new (
				x1,
				y1,
				x2 - x1,
				y2 - y1);
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD FillRectangle (
			this Context g,
			RectangleD r,
			Color color)
		{
			g.Save ();

			g.MoveTo (r.X, r.Y);
			g.LineTo (r.X + r.Width, r.Y);
			g.LineTo (r.X + r.Width, r.Y + r.Height);
			g.LineTo (r.X, r.Y + r.Height);
			g.LineTo (r.X, r.Y);

			g.SetSourceColor (color);

			RectangleD dirty = g.StrokeExtents ();

			g.Fill ();

			g.Restore ();

			return dirty;
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD FillRectangle (
			this Context g,
			RectangleD r,
			Pattern pattern)
		{
			g.Save ();

			g.MoveTo (r.X, r.Y);
			g.LineTo (r.X + r.Width, r.Y);
			g.LineTo (r.X + r.Width, r.Y + r.Height);
			g.LineTo (r.X, r.Y + r.Height);
			g.LineTo (r.X, r.Y);

			g.SetSource (pattern);

			RectangleD dirty = g.StrokeExtents ();

			g.Fill ();

			g.Restore ();

			return dirty;
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD FillRectangle (
			this Context g,
			RectangleD r,
			Pattern pattern,
			PointD patternOffset)
		{
			g.Save ();

			g.MoveTo (r.X, r.Y);
			g.LineTo (r.X + r.Width, r.Y);
			g.LineTo (r.X + r.Width, r.Y + r.Height);
			g.LineTo (r.X, r.Y + r.Height);
			g.LineTo (r.X, r.Y);

			Matrix xform = CreateIdentityMatrix ();
			pattern.GetMatrix (xform);
			xform.Translate (-patternOffset.X, -patternOffset.Y);
			pattern.SetMatrix (xform);

			g.SetSource (pattern);

			RectangleD dirty = g.StrokeExtents ();

			g.Fill ();

			g.Restore ();

			return dirty;
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD DrawPolygonal (
			this Context g,
			ReadOnlySpan<PointD> points,
			Color color,
			LineCap lineCap)
		{
			g.Save ();
			g.MoveTo (points[0].X, points[0].Y);

			foreach (var point in points)
				g.LineTo (point.X, point.Y);

			g.SetSourceColor (color);
			g.LineCap = lineCap;

			RectangleD dirty = g.StrokeExtents ();
			g.Stroke ();

			g.Restore ();

			return dirty;
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD FillPolygonal (
			this Context g,
			ReadOnlySpan<PointD> points,
			Color color)
		{
			g.Save ();

			g.MoveTo (points[0].X, points[0].Y);

			foreach (var point in points)
				g.LineTo (point.X, point.Y);

			g.SetSourceColor (color);

			RectangleD dirty = g.StrokeExtents ();
			g.Fill ();

			g.Restore ();

			return dirty;
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD DrawEllipse (
			this Context g,
			RectangleD r,
			Color color,
			int lineWidth)
		{
			double rx = r.Width / 2;
			double ry = r.Height / 2;
			double cx = r.X + rx;
			double cy = r.Y + ry;

			const double c1 = 0.552285;

			g.Save ();

			g.MoveTo (cx + rx, cy);

			g.CurveTo (
				cx + rx,
				cy - c1 * ry,
				cx + c1 * rx,
				cy - ry,
				cx,
				cy - ry);

			g.CurveTo (
				cx - c1 * rx,
				cy - ry,
				cx - rx,
				cy - c1 * ry,
				cx - rx,
				cy);

			g.CurveTo (
				cx - rx,
				cy + c1 * ry,
				cx - c1 * rx,
				cy + ry,
				cx,
				cy + ry);

			g.CurveTo (
				cx + c1 * rx,
				cy + ry,
				cx + rx,
				cy + c1 * ry,
				cx + rx,
				cy);

			g.ClosePath ();

			g.SetSourceColor (color);
			g.LineWidth = lineWidth;

			RectangleD dirty = g.StrokeExtents ();

			g.Stroke ();
			g.Restore ();

			return dirty;
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD FillEllipse (
			this Context g,
			RectangleD r,
			Color color)
		{
			double rx = r.Width / 2;
			double ry = r.Height / 2;
			double cx = r.X + rx;
			double cy = r.Y + ry;

			const double c1 = 0.552285;

			g.Save ();

			g.MoveTo (cx + rx, cy);

			g.CurveTo (
				cx + rx,
				cy - c1 * ry,
				cx + c1 * rx,
				cy - ry,
				cx,
				cy - ry);

			g.CurveTo (
				cx - c1 * rx,
				cy - ry,
				cx - rx,
				cy - c1 * ry,
				cx - rx,
				cy);

			g.CurveTo (
				cx - rx,
				cy + c1 * ry,
				cx - c1 * rx,
				cy + ry,
				cx,
				cy + ry);

			g.CurveTo (
				cx + c1 * rx,
				cy + ry,
				cx + rx,
				cy + c1 * ry,
				cx + rx,
				cy);

			g.ClosePath ();

			g.SetSourceColor (color);

			RectangleD dirty = g.StrokeExtents ();

			g.Fill ();
			g.Restore ();

			return dirty;
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD FillStrokedEllipse (
			this Context g,
			RectangleD r,
			Color fill,
			Color stroke,
			int lineWidth)
		{
			double rx = r.Width / 2;
			double ry = r.Height / 2;
			double cx = r.X + rx;
			double cy = r.Y + ry;

			const double c1 = 0.552285;

			g.Save ();

			g.MoveTo (cx + rx, cy);

			g.CurveTo (
				cx + rx,
				cy - c1 * ry,
				cx + c1 * rx,
				cy - ry,
				cx,
				cy - ry);

			g.CurveTo (
				cx - c1 * rx,
				cy - ry,
				cx - rx,
				cy - c1 * ry,
				cx - rx,
				cy);

			g.CurveTo (
				cx - rx,
				cy + c1 * ry,
				cx - c1 * rx,
				cy + ry,
				cx,
				cy + ry);

			g.CurveTo (
				cx + c1 * rx,
				cy + ry,
				cx + rx,
				cy + c1 * ry,
				cx + rx,
				cy);

			g.ClosePath ();

			g.SetSourceColor (fill);
			g.FillPreserve ();

			g.SetSourceColor (stroke);
			g.LineWidth = lineWidth;

			RectangleD dirty = g.StrokeExtents ();

			g.Stroke ();
			g.Restore ();

			return dirty;
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD FillRoundedRectangle (
			this Context g,
			RectangleD r,
			double radius,
			Color fill)
		{
			g.Save ();

			if ((radius > r.Height / 2) || (radius > r.Width / 2))
				radius = Math.Min (r.Height / 2, r.Width / 2);

			g.MoveTo (r.X, r.Y + radius);

			g.Arc (
				r.X + radius,
				r.Y + radius,
				radius,
				Math.PI,
				-Math.PI / 2);

			g.LineTo (r.X + r.Width - radius, r.Y);

			g.Arc (
				r.X + r.Width - radius,
				r.Y + radius,
				radius,
				-Math.PI / 2,
				0);

			g.LineTo (r.X + r.Width, r.Y + r.Height - radius);

			g.Arc (
				r.X + r.Width - radius,
				r.Y + r.Height - radius,
				radius,
				0,
				Math.PI / 2);

			g.LineTo (r.X + radius, r.Y + r.Height);

			g.Arc (
				r.X + radius,
				r.Y + r.Height - radius,
				radius,
				Math.PI / 2,
				Math.PI);

			g.ClosePath ();

			g.SetSourceColor (fill);

			RectangleD dirty = g.StrokeExtents ();

			g.Fill ();
			g.Restore ();

			return dirty;
		}

		public static void QuadraticCurveTo (
			this Context g,
			double x1,
			double y1,
			double x2,
			double y2)
		{
			g.GetCurrentPoint (
				out double c_x,
				out double c_y);

			double cp1x = c_x + 2.0 / 3.0 * (x1 - c_x);
			double cp1y = c_y + 2.0 / 3.0 * (y1 - c_y);
			double cp2x = cp1x + (x2 - c_x) / 3.0;
			double cp2y = cp1y + (y2 - c_y) / 3.0;

			g.CurveTo (cp1x, cp1y, cp2x, cp2y, x2, y2);
		}

		/// <returns>Bounding rectangle of changed area</returns>
		public static RectangleD DrawLine (
			this Context g,
			PointD p1,
			PointD p2,
			Color color,
			int lineWidth)
		{
			// Put it on a pixel line
			if (lineWidth == 1) {
				p1 = new PointD (p1.X + 0.5, p1.Y + 0.5);
				p2 = new PointD (p2.X + 0.5, p2.Y + 0.5);
			}

			g.Save ();

			g.MoveTo (p1.X, p1.Y);
			g.LineTo (p2.X, p2.Y);

			g.SetSourceColor (color);
			g.LineWidth = lineWidth;
			g.LineCap = LineCap.Square;

			RectangleD dirty = g.StrokeExtents ();
			g.Stroke ();

			g.Restore ();

			return dirty;
		}

		public static void DrawPixbuf (
			this Context g,
			GdkPixbuf.Pixbuf pixbuf,
			double pixbuf_x,
			double pixbuf_y)
		{
			g.Save ();
			Gdk.Functions.CairoSetSourcePixbuf (g, pixbuf, pixbuf_x, pixbuf_y);
			g.Paint ();
			g.Restore ();
		}

		public static void DrawPixbuf (
			this Context g,
			GdkPixbuf.Pixbuf pixbuf,
			PointD pixbuf_pos)
		{
			g.DrawPixbuf (pixbuf, pixbuf_pos.X, pixbuf_pos.Y);
		}

		public static GdkPixbuf.Pixbuf ToPixbuf (this ImageSurface surfSource)
			=> Gdk.Functions.PixbufGetFromSurface (
				surfSource,
				0,
				0,
				surfSource.Width,
				surfSource.Height
			)!;

		public static ColorBgra ToColorBgra (this Cairo.Color color)
			=> ColorBgra.FromBgra (
				b: (byte) (color.B * 255),
				g: (byte) (color.G * 255),
				r: (byte) (color.R * 255),
				a: (byte) (color.A * 255));

		public static Cairo.Color ToCairoColor (this ColorBgra color)
			=> new (
				R: color.R / 255d,
				G: color.G / 255d,
				B: color.B / 255d,
				A: color.A / 255d);

		public static ImageSurface Clone (this ImageSurface surf)
		{
			if (PintaCore.Workspace.HasOpenDocuments)
				PintaCore.Workspace.ActiveDocument.SignalSurfaceCloned ();

			ImageSurface newsurf = CreateImageSurface (
				surf.Format,
				surf.Width,
				surf.Height);

			Context g = new (newsurf);
			g.SetSourceSurface (surf, 0, 0);
			g.Paint ();

			return newsurf;
		}

		public static Path Clone (this Path path)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			Context g = new (doc.Layers.CurrentUserLayer.Surface);
			g.AppendPath (path);
			return g.CopyPath ();
		}

		public static void Clear (this ImageSurface surface)
		{
			Context g = new (surface) { Operator = Operator.Clear };
			g.Paint ();
		}

		public static void Clear (this Context g, RectangleD roi)
		{
			g.Save ();
			g.Rectangle (roi.X, roi.Y, roi.Width, roi.Height);
			g.Clip ();
			g.Operator = Operator.Clear;
			g.Paint ();
			g.Restore ();
		}

		public static void MarkDirty (this ImageSurface surface, in RectangleI rect)
		{
			surface.MarkDirty (
				rect.X,
				rect.Y,
				rect.Width,
				rect.Height);
		}

		public static RectangleD PathExtents (this Context context)
		{
			context.PathExtents (
				out double x1,
				out double y1,
				out double x2,
				out double y2);

			return new (
				x1,
				y1,
				x2 - x1,
				y2 - y1);
		}

		public static RectangleI GetBounds (this Path path)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;
			Context g = new (doc.Layers.CurrentUserLayer.Surface);
			g.AppendPath (path);
			return g.PathExtents ().ToInt ();
		}

		// This isn't really an extension method, since it doesn't use
		// the passed in argument, but it's nice to have the same calling
		// convention as the uncached version.  If you can use this one
		// over the other, it is much faster in tight loops (like effects).
		public static ref readonly ColorBgra GetColorBgra (
			this ImageSurface surf,
			ReadOnlySpan<ColorBgra> data,
			int width,
			PointI position)
		{
			return ref data[width * position.Y + position.X];
		}

		/// <summary>
		/// Prefer using the variant which takes the surface data and width, for improved performance
		/// if there are repeated calls in a loop.
		/// </summary>
		public static ref readonly ColorBgra GetColorBgra (
			this ImageSurface surf,
			PointI position)
		{
			return ref surf.GetColorBgra (surf.GetReadOnlyPixelData (), surf.Width, position);
		}

		public static RectangleI GetBounds (this ImageSurface surf)
			=> new (0, 0, surf.Width, surf.Height);

		public static Size GetSize (this ImageSurface surf)
			=> new (surf.Width, surf.Height);

		public static ColorBgra GetBilinearSample (this ImageSurface src, float x, float y)
			=> GetBilinearSample (
				src,
				src.GetReadOnlyPixelData (),
				src.Width,
				src.Height,
				x,
				y);

		public static ColorBgra GetBilinearSample (
			this ImageSurface src,
			ReadOnlySpan<ColorBgra> src_data,
			int srcWidth,
			int srcHeight,
			float x,
			float y)
		{
			if (!Utility.IsNumber (x) || !Utility.IsNumber (y))
				return ColorBgra.Transparent;

			float u = x;
			float v = y;

			if (u < 0 || v < 0 || u >= srcWidth || v >= srcHeight)
				return ColorBgra.FromUInt32 (0);

			unchecked {
				int iu = (int) Math.Floor (u);
				uint sxfrac = (uint) (256 * (u - iu));
				uint sxfracinv = 256 - sxfrac;

				int iv = (int) Math.Floor (v);
				uint syfrac = (uint) (256 * (v - iv));
				uint syfracinv = 256 - syfrac;

				uint wul = sxfracinv * syfracinv;
				uint wur = sxfrac * syfracinv;
				uint wll = sxfracinv * syfrac;
				uint wlr = sxfrac * syfrac;

				int sx = iu;
				int sy = iv;
				int sleft = sx;
				int sright = (sleft == (srcWidth - 1)) ? sleft : sleft + 1;

				int stop = sy;
				int sbottom = (stop == (srcHeight - 1)) ? stop : stop + 1;

				ColorBgra cul = src.GetColorBgra (src_data, srcWidth, new (sleft, stop));
				ColorBgra cur = src.GetColorBgra (src_data, srcWidth, new (sright, stop));
				ColorBgra cll = src.GetColorBgra (src_data, srcWidth, new (sleft, sbottom));
				ColorBgra clr = src.GetColorBgra (src_data, srcWidth, new (sright, sbottom));

				return ColorBgra.BlendColors4W16IP (cul, wul, cur, wur, cll, wll, clr, wlr);
			}
		}

		public static ColorBgra GetBilinearSampleClamped (
			this ImageSurface src,
			float x,
			float y
		)
			=> GetBilinearSampleClamped (
				src,
				src.GetReadOnlyPixelData (),
				src.Width,
				src.Height,
				x,
				y);

		public static ColorBgra GetBilinearSampleClamped (
			this ImageSurface src,
			ReadOnlySpan<ColorBgra> src_data,
			int srcWidth,
			int srcHeight,
			float x,
			float y)
		{
			if (!Utility.IsNumber (x) || !Utility.IsNumber (y))
				return ColorBgra.Transparent;

			float u = Math.Clamp (x, 0, srcWidth - 1);
			float v = Math.Clamp (y, 0, srcHeight - 1);

			unchecked {
				int iu = (int) Math.Floor (u);
				uint sxfrac = (uint) (256 * (u - iu));
				uint sxfracinv = 256 - sxfrac;

				int iv = (int) Math.Floor (v);
				uint syfrac = (uint) (256 * (v - iv));
				uint syfracinv = 256 - syfrac;

				uint wul = sxfracinv * syfracinv;
				uint wur = sxfrac * syfracinv;
				uint wll = sxfracinv * syfrac;
				uint wlr = sxfrac * syfrac;

				int sx = iu;
				int sy = iv;
				int sleft = sx;
				int sright = (sleft == (srcWidth - 1)) ? sleft : sleft + 1;

				int stop = sy;
				int sbottom = (stop == (srcHeight - 1)) ? stop : stop + 1;

				ColorBgra cul = src.GetColorBgra (src_data, srcWidth, new (sleft, stop));
				ColorBgra cur = src.GetColorBgra (src_data, srcWidth, new (sright, stop));
				ColorBgra cll = src.GetColorBgra (src_data, srcWidth, new (sleft, sbottom));
				ColorBgra clr = src.GetColorBgra (src_data, srcWidth, new (sright, sbottom));

				return ColorBgra.BlendColors4W16IP (cul, wul, cur, wur, cll, wll, clr, wlr);
			}
		}

		public static ColorBgra GetBilinearSampleWrapped (
			this ImageSurface src,
			float x,
			float y
		)
			=> GetBilinearSampleWrapped (
				src,
				src.GetReadOnlyPixelData (),
				src.Width,
				src.Height,
				x,
				y);

		public static ColorBgra GetBilinearSampleWrapped (
			this ImageSurface src,
			ReadOnlySpan<ColorBgra> src_data,
			int srcWidth,
			int srcHeight,
			float x,
			float y)
		{
			if (!Utility.IsNumber (x) || !Utility.IsNumber (y))
				return ColorBgra.Transparent;

			float u = x;
			float v = y;

			unchecked {
				int iu = (int) Math.Floor (u);
				uint sxfrac = (uint) (256 * (u - iu));
				uint sxfracinv = 256 - sxfrac;

				int iv = (int) Math.Floor (v);
				uint syfrac = (uint) (256 * (v - iv));
				uint syfracinv = 256 - syfrac;

				uint wul = sxfracinv * syfracinv;
				uint wur = sxfrac * syfracinv;
				uint wll = sxfracinv * syfrac;
				uint wlr = sxfrac * syfrac;

				int sx = iu;
				if (sx < 0)
					sx = srcWidth - 1 + ((sx + 1) % srcWidth);
				else if (sx > (srcWidth - 1))
					sx %= srcWidth;

				int sy = iv;
				if (sy < 0)
					sy = srcHeight - 1 + ((sy + 1) % srcHeight);
				else if (sy > (srcHeight - 1))
					sy %= srcHeight;

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

				ColorBgra cul = src.GetColorBgra (src_data, srcWidth, new (sleft, stop));
				ColorBgra cur = src.GetColorBgra (src_data, srcWidth, new (sright, stop));
				ColorBgra cll = src.GetColorBgra (src_data, srcWidth, new (sleft, sbottom));
				ColorBgra clr = src.GetColorBgra (src_data, srcWidth, new (sright, sbottom));

				return ColorBgra.BlendColors4W16IP (cul, wul, cur, wur, cll, wll, clr, wlr);
			}
		}

		public static void TranslatePointsInPlace (this Span<PointI> points, PointI delta)
		{
			for (int i = 0; i < points.Length; ++i)
				points[i] += delta;
		}

		private struct Edge
		{
			public int miny { get; }   // int
			public int maxy { get; }   // int
			public int x { get; set; } // fixed point: 24.8
			public int dxdy { get; }   // fixed point: 24.8

			public Edge (int miny, int maxy, int x, int dxdy)
			{
				this.miny = miny;
				this.maxy = maxy;
				this.x = x;
				this.dxdy = dxdy;
			}
		}

		public static ImmutableArray<Scanline> GetScans (this ReadOnlySpan<PointI> points)
		{
			int ymax = 0;

			// Build edge table
			Edge[] edgeTable = new Edge[points.Length];
			int edgeCount = 0;

			for (int i = 0; i < points.Length; ++i) {

				PointI top = points[i];
				PointI bottom = points[(i + 1) % points.Length];

				if (top.Y > bottom.Y)
					(bottom, top) = (top, bottom);

				int dy = bottom.Y - top.Y;

				if (dy != 0) {
					edgeTable[edgeCount] = new Edge (top.Y, bottom.Y, top.X << 8, ((bottom.X - top.X) << 8) / dy);
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

				if (min != i)
					(edgeTable[i], edgeTable[min]) = (edgeTable[min], edgeTable[i]);
			}

			// Compute how many scanlines we will be emitting
			int scanCount = 0;
			int activeLow = 0;
			int activeHigh = 0;
			int yscan1 = edgeTable[0].miny;

			// we assume that edgeTable[0].miny == yscan
			while (activeHigh < edgeCount - 1 && edgeTable[activeHigh + 1].miny == yscan1)
				++activeHigh;

			while (yscan1 <= ymax) {

				// Find new edges where yscan == miny
				while (activeHigh < edgeCount - 1 && edgeTable[activeHigh + 1].miny == yscan1)
					++activeHigh;

				int count = 0;
				for (int i = activeLow; i <= activeHigh; ++i)
					if (edgeTable[i].maxy > yscan1)
						++count;

				scanCount += count / 2;
				++yscan1;

				// Remove edges where yscan == maxy
				while (activeLow < edgeCount - 1 && edgeTable[activeLow].maxy <= yscan1)
					++activeLow;

				if (activeLow > activeHigh)
					activeHigh = activeLow;
			}

			// Allocate scanlines that we'll return
			var scans = ImmutableArray.CreateBuilder<Scanline> (scanCount);
			scans.Count = scanCount;

			// Active Edge Table (AET): it is indices into the Edge Table (ET)
			int[] active = new int[edgeCount];
			int activeCount = 0;
			int yscan2 = edgeTable[0].miny;
			int scansIndex = 0;

			// Repeat until both the ET and AET are empty
			while (yscan2 <= ymax) {
				// Move any edges from the ET to the AET where yscan == miny
				for (int i = 0; i < edgeCount; ++i) {

					if (edgeTable[i].miny != yscan2)
						continue;

					active[activeCount] = i;
					++activeCount;
				}

				// Sort the AET on x
				for (int i = 0; i < activeCount - 1; ++i) {

					int min = i;

					for (int j = i + 1; j < activeCount; ++j)
						if (edgeTable[active[j]].x < edgeTable[active[min]].x)
							min = j;

					if (min != i)
						(active[i], active[min]) = (active[min], active[i]);
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

			return scans.MoveToImmutable ();
		}

		public static Path CreatePolygonPath (
			this Context g,
			IReadOnlyList<IReadOnlyList<PointI>> polygonSet)
		{
			g.Save ();
			PointI p;

			for (int i = 0; i < polygonSet.Count; i++) {

				if (polygonSet[i].Count == 0)
					continue;

				p = polygonSet[i][0];
				g.MoveTo (p.X, p.Y);

				for (int j = 1; j < polygonSet[i].Count; j++) {
					p = polygonSet[i][j];
					g.LineTo (p.X, p.Y);
				}

				g.ClosePath ();
			}

			Path path = g.CopyPath ();

			g.Restore ();

			return path;
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
		public static RectangleD? UnionRectangles (
			this RectangleD? r1,
			RectangleD? r2)
		{
			if (!r1.HasValue) //r2 is the only given Rectangle that could still have a value, and if it's null, return that anyways.
				return r2;

			if (!r2.HasValue) //Only r1 has a value.
				return r1;

			// If execution reaches this point, then both r1 and r2 have values.

			//Calculate the left-most and top-most values.

			PointD min = new (
				X: Math.Min (r1.Value.X, r2.Value.X),
				Y: Math.Min (r1.Value.Y, r2.Value.Y));

			//Calculate the right-most and bottom-most values and subtract the left-most and top-most values from them to get the width and height.
			return new (
				min.X,
				min.Y,
				Math.Max (r1.Value.X + r1.Value.Width, r2.Value.X + r2.Value.Width) - min.X,
				Math.Max (r1.Value.Y + r1.Value.Height, r2.Value.Y + r2.Value.Height) - min.Y);
		}

		public static Pattern CreateTransparentBackgroundPattern (int size)
			=>
				CreateTransparentBackgroundSurface (size)
				.ToTiledPattern ();

		public static ImageSurface CreateTransparentBackgroundSurface (int size)
		{
			ImageSurface surface = CreateImageSurface (Format.Argb32, size, size);

			// Draw the checkerboard
			Context g = new (surface);

			// Fill white
			g.FillRectangle (new RectangleD (0, 0, size, size), new Color (1, 1, 1));

			Color color = new (0.78, 0.78, 0.78);
			int half_size = size / 2;

			// Draw gray squares
			g.FillRectangle (new RectangleD (0, 0, half_size, half_size), color);
			g.FillRectangle (new RectangleD (half_size, half_size, half_size, half_size), color);

			return surface;
		}

		public static Pattern ToTiledPattern (this Surface surface)
			=> new SurfacePattern (surface) { Extend = Extend.Repeat };

		public static void Rectangle (
			this Context g,
			RectangleD r)
		{
			g.Rectangle (r.X, r.Y, r.Width, r.Height);
		}

		public static void BlendSurface (
			this Context g,
			Surface src,
			BlendMode mode = BlendMode.Normal,
			double opacity = 1.0)
		{
			g.Save ();

			g.SetBlendMode (mode);
			g.SetSourceSurface (src, 0, 0);
			g.PaintWithAlpha (opacity);

			g.Restore ();
		}

		public static void BlendSurface (
			this Context g,
			Surface src,
			RectangleD roi,
			BlendMode mode = BlendMode.Normal,
			double opacity = 1.0)
		{
			g.Save ();

			g.Rectangle (roi);
			g.Clip ();
			g.SetBlendMode (mode);
			g.SetSourceSurface (src, 0, 0);
			g.PaintWithAlpha (opacity);

			g.Restore ();
		}

		public static void BlendSurface (
			this Context g,
			Surface src,
			PointD offset,
			BlendMode mode = BlendMode.Normal,
			double opacity = 1.0)
		{
			g.Save ();

			g.Translate (offset.X, offset.Y);
			g.SetBlendMode (mode);
			g.SetSourceSurface (src, 0, 0);
			g.PaintWithAlpha (opacity);

			g.Restore ();
		}

		public static void SetBlendMode (
			this Context g,
			BlendMode mode)
		{
			g.Operator = GetBlendModeOperator (mode);
		}

		private static Operator GetBlendModeOperator (BlendMode mode)
			=> mode switch {
				BlendMode.Normal => Operator.Over,
				BlendMode.Multiply => (Operator) ExtendedOperators.Multiply,
				BlendMode.ColorBurn => (Operator) ExtendedOperators.ColorBurn,
				BlendMode.ColorDodge => (Operator) ExtendedOperators.ColorDodge,
				BlendMode.HardLight => (Operator) ExtendedOperators.HardLight,
				BlendMode.SoftLight => (Operator) ExtendedOperators.SoftLight,
				BlendMode.Overlay => (Operator) ExtendedOperators.Overlay,
				BlendMode.Difference => (Operator) ExtendedOperators.Difference,
				BlendMode.Color => (Operator) ExtendedOperators.HslColor,
				BlendMode.Luminosity => (Operator) ExtendedOperators.HslLuminosity,
				BlendMode.Hue => (Operator) ExtendedOperators.HslHue,
				BlendMode.Saturation => (Operator) ExtendedOperators.HslSaturation,
				BlendMode.Lighten => (Operator) ExtendedOperators.Lighten,
				BlendMode.Darken => (Operator) ExtendedOperators.Darken,
				BlendMode.Screen => (Operator) ExtendedOperators.Screen,
				BlendMode.Xor => Operator.Xor,
				_ => throw new ArgumentOutOfRangeException (nameof (mode)),
			};

		public static void SetSourceSurface (
			this Context g,
			Surface surface,
			ResamplingMode resamplingMode)
		{
			SurfacePattern src_pattern = new (surface) {
				Filter = resamplingMode.ToCairoFilter (),
			};

			g.SetSource (src_pattern);
		}

		public static RectangleI GetRectangleFromPoints (
			PointI a,
			PointI b,
			int inflate)
		{
			int x1 = Math.Min (a.X, b.X);
			int y1 = Math.Min (a.Y, b.Y);
			int x2 = Math.Max (a.X, b.X);
			int y2 = Math.Max (a.Y, b.Y);

			return new RectangleI (x1, y1, x2 - x1, y2 - y1).Inflated (inflate, inflate);
		}

		/// <summary>
		/// Create a rectangle with a positive width / height from the provided points.
		/// </summary>
		public static RectangleD PointsToRectangle (
			PointD p1,
			PointD p2)
		{
			double y1 = Math.Min (p1.Y, p2.Y);
			double y2 = Math.Max (p1.Y, p2.Y);
			double x1 = Math.Min (p1.X, p2.X);
			double x2 = Math.Max (p1.X, p2.X);

			return new (
				x1,
				y1,
				x2 - x1,
				y2 - y1);
		}

		// TODO-GTK4 (bindings) - remove once gir.core has improved bindings for Cairo.Region (https://github.com/gircore/gir.core/pull/621)
		[StructLayout (LayoutKind.Sequential)]
		private struct CairoRectangleInt
		{
			public int X;
			public int Y;
			public int Width;
			public int Height;

			public readonly RectangleI ToRectangleI () => new (X, Y, Width, Height);
		}

		[DllImport (CairoLibraryName, EntryPoint = "cairo_region_create_rectangle")]
		private static extern Cairo.Internal.RegionOwnedHandle RegionCreateRectangle (ref CairoRectangleInt rect);

		[LibraryImport (CairoLibraryName, EntryPoint = "cairo_region_contains_point")]
		[return: MarshalAs (UnmanagedType.Bool)]
		private static partial bool RegionContainsPoint (Cairo.Internal.RegionHandle handle, int x, int y);

		[LibraryImport (CairoLibraryName, EntryPoint = "cairo_region_xor")]
		private static partial Status RegionXor (Cairo.Internal.RegionHandle handle, Cairo.Internal.RegionHandle other);

		[LibraryImport (CairoLibraryName, EntryPoint = "cairo_region_num_rectangles")]
		private static partial int RegionNumRectangles (Cairo.Internal.RegionHandle handle);

		[LibraryImport (CairoLibraryName, EntryPoint = "cairo_region_get_rectangle")]
		private static partial int RegionGetRectangle (Cairo.Internal.RegionHandle handle, int i, out CairoRectangleInt rect);

		public static Region CreateRegion (in RectangleI rect)
		{
			CairoRectangleInt cairo_rect = new () {
				X = rect.X,
				Y = rect.Y,
				Width = rect.Width,
				Height = rect.Height,
			};

			return new (RegionCreateRectangle (ref cairo_rect));
		}

		public static bool ContainsPoint (this Cairo.Region region, int x, int y)
			=> RegionContainsPoint (region.Handle, x, y);

		private static Status Xor (this Cairo.Region region, Cairo.Region other)
			=> RegionXor (region.Handle, other.Handle);

		private static void GetRectangle (this Cairo.Region region, int i, out CairoRectangleInt rect)
			=> RegionGetRectangle (region.Handle, i, out rect);

		private static int GetNumRectangles (this Cairo.Region region)
			=> RegionNumRectangles (region.Handle);

		// Ported from PDN.
		public static void FillStencilFromPoint (
			ImageSurface surface,
			BitMask stencil,
			PointI start,
			int tolerance,
			out RectangleD boundingBox,
			Cairo.Region limitRegion,
			bool limitToSelection)
		{
			ReadOnlySpan<ColorBgra> surf_data = surface.GetReadOnlyPixelData ();
			int surf_width = surface.Width;
			ColorBgra cmp = surface.GetColorBgra (surf_data, surf_width, start);
			int top = int.MaxValue;
			int bottom = int.MinValue;
			int left = int.MaxValue;
			int right = int.MinValue;
			RectangleI[] scans;

			stencil.Clear (false);

			if (limitToSelection) {
				var excluded = CreateRegion (new RectangleI (0, 0, stencil.Width, stencil.Height));
				excluded.Xor (limitRegion);
				scans = new RectangleI[excluded.GetNumRectangles ()];
				for (int i = 0, n = scans.Length; i < n; ++i) {
					excluded.GetRectangle (i, out var cairo_rect);
					scans[i] = cairo_rect.ToRectangleI ();
				}
			} else {
				scans = Array.Empty<RectangleI> ();
			}

			foreach (var rect in scans)
				stencil.Set (rect, true);

			Queue<PointI> queue = new (16);
			queue.Enqueue (start);

			while (queue.Count > 0) {
				PointI pt = queue.Dequeue ();

				ReadOnlySpan<ColorBgra> row = surf_data.Slice (pt.Y * surf_width, surf_width);
				int localLeft = pt.X - 1;
				int localRight = pt.X;

				while (localLeft >= 0 &&
					   !stencil.Get (localLeft, pt.Y) &&
					   ColorBgra.ColorsWithinTolerance (cmp, row[localLeft], tolerance)) {
					stencil.Set (localLeft, pt.Y, true);
					--localLeft;
				}

				int surfaceWidth = surface.Width;
				while (
					localRight < surfaceWidth
					&& !stencil.Get (localRight, pt.Y)
					&& ColorBgra.ColorsWithinTolerance (cmp, row[localRight], tolerance)) {
					stencil.Set (localRight, pt.Y, true);
					++localRight;
				}

				++localLeft;
				--localRight;

				void CheckRow (ReadOnlySpan<ColorBgra> surf_data, int row)
				{
					int sleft = localLeft;
					int sright = localLeft;
					ReadOnlySpan<ColorBgra> other_row = surf_data.Slice (row * surf_width, surf_width);

					for (int sx = localLeft; sx <= localRight; ++sx) {
						if (!stencil.Get (sx, row) &&
							ColorBgra.ColorsWithinTolerance (cmp, other_row[sx], tolerance)) {
							++sright;
						} else {
							if (sright - sleft > 0)
								queue.Enqueue (new PointI (sleft, row));

							++sright;
							sleft = sright;
						}
					}

					if (sright - sleft > 0)
						queue.Enqueue (new PointI (sleft, row));
				}

				if (pt.Y > 0)
					CheckRow (surf_data, pt.Y - 1);

				if (pt.Y < surface.Height - 1)
					CheckRow (surf_data, pt.Y + 1);

				if (localLeft < left)
					left = localLeft;

				if (localRight > right)
					right = localRight;

				if (pt.Y < top)
					top = pt.Y;

				if (pt.Y > bottom)
					bottom = pt.Y;
			}

			foreach (var rect in scans)
				stencil.Set (rect, false);

			boundingBox = new RectangleD (left, top, right - left + 1, bottom - top + 1);
		}

		// Ported from PDN
		public static void FillStencilByColor (
			ImageSurface surface,
			BitMask stencil,
			ColorBgra cmp,
			int tolerance,
			out RectangleD boundingBox,
			Cairo.Region limitRegion,
			bool limitToSelection)
		{
			int surf_width = surface.Width;

			int top = int.MaxValue;
			int bottom = int.MinValue;
			int left = int.MaxValue;
			int right = int.MinValue;
			RectangleI[] scans;

			stencil.Clear (false);

			if (limitToSelection) {
				var excluded = CreateRegion (new RectangleI (0, 0, stencil.Width, stencil.Height));
				excluded.Xor (limitRegion);
				scans = new RectangleI[excluded.GetNumRectangles ()];
				for (int i = 0, n = scans.Length; i < n; ++i) {
					excluded.GetRectangle (i, out var cairo_rect);
					scans[i] = cairo_rect.ToRectangleI ();
				}
			} else {
				scans = Array.Empty<RectangleI> ();
			}

			foreach (var rect in scans)
				stencil.Set (rect, true);

			Parallel.For (0, surface.Height, y => {

				bool foundPixelInRow = false;

				ReadOnlySpan<ColorBgra> row = surface.GetReadOnlyPixelData ().Slice (y * surf_width, surf_width);

				for (int x = 0; x < surf_width; ++x) {

					if (!ColorBgra.ColorsWithinTolerance (cmp, row[x], tolerance))
						continue;

					stencil.Set (x, y, true);

					if (x < left)
						left = x;

					if (x > right)
						right = x;

					foundPixelInRow = true;
				}

				if (foundPixelInRow) {

					if (y < top)
						top = y;

					if (y >= bottom)
						bottom = y;
				}
			});

			foreach (var rect in scans)
				stencil.Set (rect, false);

			boundingBox = new RectangleD (left, top, right - left + 1, bottom - top + 1);
		}

		/// <summary>
		/// Wrapper method to create an ImageSurface and handle allocation failures.
		/// </summary>
		public static ImageSurface CreateImageSurface (
			Cairo.Format format,
			int width,
			int height)
		{
			ImageSurface surf = new (format, width, height);

			if (surf == null || surf.Status == Cairo.Status.NoMemory)
				throw new OutOfMemoryException ("Unable to allocate memory for image");

			return surf;
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
			HslLuminosity = 28,
		}

		/// <summary>
		/// Returns the validity of the dash pattern.
		/// </summary>
		/// <param name="dash_pattern">The dash pattern string.</param>
		/// <returns>Returns false if dash pattern invalid or would draw a normal line, returns true if draws a dash pattern.</returns>
		public static bool IsValidDashPattern (string dash_pattern)
		{
			// dashpattern "-" and "" produce different results at high brush size (see #733), so we default "-" to "" (a normal line.)
			return dash_pattern.Contains ('-') && dash_pattern != "-";
		}

		/// <summary>
		/// Given a string pattern consisting of dashes and spaces, creates the Cairo dash pattern.
		/// Any other characters are treated as a space.
		/// See https://www.cairographics.org/manual/cairo-cairo-t.html#cairo-set-dash
		/// </summary>
		/// <param name="dash_pattern">The dash pattern string.</param>
		/// <param name="brush_width">The width of the brush.</param>
		/// <param name="line_cap">The line cap style being used.</param>
		/// <param name="dash_list">The Cairo dash pattern.</param>
		/// <param name="offset">The offset into the dash pattern to begin drawing from.</param>
		/// <returns>Returns false if dash pattern invalid or would draw a normal line, returns true if draws a dash pattern. See <see cref="IsValidDashPattern"/></returns>
		public static bool CreateDashPattern (
			string dash_pattern,
			double brush_width,
			LineCap line_cap,
			out double[] dash_list,
			out double offset)
		{
			// An empty cairo pattern, a pattern with no dashes, or a pattern with a single dash just draws a normal line.
			// Cairo draws a normal line when the dash list is empty.
			if (!IsValidDashPattern (dash_pattern)) {
				dash_list = Array.Empty<double> ();
				offset = 0.0;
				return false;
			}

			List<double> dashes = new ();

			// Count the number of consecutive dashes / spaces.
			// e.g. "---  - " produces { 3.0, 2.0, 1.0, 1.0 }
			{
				var is_dash = dash_pattern.Select (c => c == '-').ToArray ();
				int count = 0;

				for (int i = 0; i < dash_pattern.Length; ++i, ++count) {

					if (i <= 0 || is_dash[i] == is_dash[i - 1])
						continue;

					dashes.Add (count);
					count = 0;
				}

				dashes.Add (count);
			}

			// The cairo pattern must have an even number of dash and space sequences to loop,
			// so add a zero length space if the string pattern ended with a dash.
			if (dash_pattern.EndsWith ('-'))
				dashes.Add (0.0);

			// The cairo pattern starts with a dash, so if the string pattern
			// started with a space we need to add a zero-width dash.
			// However, we can't draw a zero-width dash if the line cap is square, so instead
			// we need to shift the space to the end of the pattern and then use the offset parameter
			// to start drawing from there.
			double? offset_from_end = null;
			if (!dash_pattern.StartsWith ('-')) {
				offset_from_end = dashes[0];
				dashes.RemoveAt (0);

				// From above, the dash pattern must already have a space at the end so
				// we can just increase its size.
				// The list is non-empty since patterns containing only a space result in an early exit.
				dashes[^1] += offset_from_end.Value;
			}

			// Each dash / space is the size of the brush width.
			// Line caps add some complexity - dashes extend visually by 0.5 * brush_width on
			// either side (e.g. a dash of size 0 still draws a square), so we need to
			// adjust the sizes accordingly for this padding.
			// Cairo seems to sometimes ignore zero sized dashes though (e.g. for a dash pattern
			// such as "- -" => { 0, 30, 0, 15 }, with brush width 15), so we always draw at least a length of 1.
			double dash_size_offset = line_cap == LineCap.Butt ? 0.0 : -brush_width;
			double space_size_offset = line_cap == LineCap.Butt ? 0.0 : brush_width;
			dash_list = dashes.Select ((dash, i) => {
				if ((i % 2) == 0)
					return Math.Max (dash * brush_width + dash_size_offset, 1.0);
				else
					return dash * brush_width + space_size_offset;
			}
			).ToArray ();

			offset = 0;
			if (offset_from_end.HasValue)
				offset = dash_list.Sum () - (offset_from_end.Value * brush_width + 0.5 * space_size_offset);
			return true;
		}

		/// <summary>
		/// Sets the dash pattern from a string
		/// (see <see cref="CreateDashPattern"/>).
		/// </summary>
		/// <returns>Returns false if dash pattern invalid or would draw a normal line, returns true if draws a dash pattern.</returns>
		public static bool SetDashFromString (
			this Context context,
			string dash_pattern,
			double brush_width,
			LineCap line_cap = LineCap.Butt)
		{
			bool isValidDashPattern = CreateDashPattern (
				dash_pattern,
				brush_width,
				line_cap,
				out var dashes,
				out var offset);

			context.SetDash (
				dashes,
				offset);
			return isValidDashPattern;
		}

		/// <summary>
		/// Access the image surface's data as a read-only span of ColorBgra pixels.
		/// </summary>
		public static ReadOnlySpan<ColorBgra> GetReadOnlyPixelData (this ImageSurface surface)
			=> surface.GetPixelData ();

		/// <summary>
		/// Access the image surface's data as a span of ColorBgra pixels.
		/// </summary>
		public static Span<ColorBgra> GetPixelData (this ImageSurface surface)
			=> MemoryMarshal.Cast<byte, ColorBgra> (surface.GetData ());

		public static void SetSourceColor (
			this Context context,
			Color color
		)
			=> context.SetSourceRgba (
				color.R,
				color.G,
				color.B,
				color.A);

		public static void AddColorStop (
			this Gradient gradient,
			double offset,
			Color color
		)
			=> gradient.AddColorStopRgba (
				offset,
				color.R,
				color.G,
				color.B,
				color.A);

		public static Matrix CreateIdentityMatrix ()
		{
			Matrix matrix = new ();
			matrix.InitIdentity ();
			return matrix;
		}

		public static Matrix CreateMatrix (
			double xx,
			double xy,
			double yx,
			double yy,
			double x0,
			double y0)
		{
			Matrix matrix = new ();
			matrix.Init (xx, xy, yx, yy, x0, y0);
			return matrix;
		}

		// TODO-GTK4 (bindings) - requires improvements to struct generation (https://github.com/gircore/gir.core/issues/622)
		// This needs to have a proper copy operator in gir.core, or access to the 6 float fields.
		// Should also audit all usages of Cairo.Matrix which changed from a struct to a class with gir.core
		public static void InitMatrix (
			this Matrix m,
			Matrix other)
		{
			m.InitIdentity ();
			m.Multiply (other);
		}

		// TODO-GTK4 (bindings) - requires improvements to struct generation (https://github.com/gircore/gir.core/issues/622)
		// This needs to have a proper copy operator in gir.core, or access to the 6 float fields.
		public static Matrix Clone (this Matrix m)
		{
			Matrix result = CreateIdentityMatrix ();
			result.Multiply (m);
			return result;
		}

		public static void TransformPoint (
			this Matrix m,
			ref PointD p)
		{
			double newX = p.X;
			double newY = p.Y;
			m.TransformPoint (ref newX, ref newY);
			p = new PointD (newX, newY);
		}

		/// <summary>
		/// Port of gdk_cairo_get_clip_rectangle from GTK3
		/// </summary>
		public static bool GetClipRectangle (
			Context context,
			out RectangleI rect)
		{
			context.ClipExtents (
				out double x1,
				out double y1,
				out double x2,
				out double y2);

			bool clip_exists = x1 < x2 && y1 < y2;

			rect = new RectangleD (x1, y1, x2 - x1, y2 - y1).ToInt ();

			return clip_exists;
		}

		public static ImageSurface CreateColorSwatch (
			int size,
			Color color)
		{
			ImageSurface surf = CreateImageSurface (Cairo.Format.Argb32, size, size);
			Context g = new (surf);

			g.FillRectangle (new RectangleD (0, 0, size, size), color);
			g.DrawRectangle (new RectangleD (0, 0, size, size), new Color (0, 0, 0), 1);

			return surf;
		}

		public static ImageSurface CreateTransparentColorSwatch (int size, bool drawBorder)
		{
			ImageSurface surface = CreateTransparentBackgroundSurface (size);
			Context g = new (surface);

			if (drawBorder)
				g.DrawRectangle (new RectangleD (0, 0, size, size), new Color (0, 0, 0), 1);

			return surface;
		}
	}
}
