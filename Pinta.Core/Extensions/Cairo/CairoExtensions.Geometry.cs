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
using System.Collections.Immutable;
using Cairo;

namespace Pinta.Core;

// These are not intended to mutate the image surface
partial class CairoExtensions
{
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

	public static bool ContainsPoint (this Region region, int x, int y)
		=> RegionContainsPoint (region.Handle, x, y);

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
		using Context g = new (doc.Layers.CurrentUserLayer.Surface);
		g.AppendPath (path);
		return g.PathExtents ().ToInt ();
	}

	public static RectangleI GetBounds (this ImageSurface surf)
		=> new (0, 0, surf.Width, surf.Height);

	public static Size GetSize (this ImageSurface surf)
		=> new (surf.Width, surf.Height);

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

	private static void GetRectangle (this Region region, int i, out CairoRectangleInt rect)
		=> RegionGetRectangle (region.Handle, i, out rect);

	private static int GetNumRectangles (this Region region)
		=> RegionNumRectangles (region.Handle);
}
