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
using Cairo;

namespace Pinta.Core;

// These operations mutate the surface
partial class CairoExtensions
{
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
			lineWidth == 1
			? new (r.X + 0.5, r.Y + 0.5, r.Width - 1, r.Height - 1) // Put it on a pixel line
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

	public static void Clear (this ImageSurface surface)
	{
		using Context g = new (surface) { Operator = Operator.Clear };
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

	public static void Rectangle (
		this Context g,
		RectangleD r)
	{
		g.Rectangle (r.X, r.Y, r.Width, r.Height);
	}

	public static void MarkDirty (this ImageSurface surface, in RectangleI rect)
	{
		surface.MarkDirty (
			rect.X,
			rect.Y,
			rect.Width,
			rect.Height);
	}
}
