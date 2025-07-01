//
// DocumentSelection.cs
//
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
//
// Copyright (c) 2012 Andrew Davis, GSoC 2012
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
using ClipperLib;

namespace Pinta.Core;

public sealed class DocumentSelection
{
	internal DocumentSelection ()
	{
	}

	private Path? selection_path;

	public List<List<IntPoint>> SelectionPolygons { get; set; } = [];
	public Clipper SelectionClipper { get; } = new ();

	public PointD Origin { get; set; }
	public PointD End { get; set; }

	private bool visible = true;
	public bool Visible {
		get => visible;
		set {
			visible = value;

			// Notify any listeners.
			SelectionModified?.Invoke (this, EventArgs.Empty);
		}
	}

	public Path SelectionPath {
		get {
			if (selection_path == null) {
				using Context g = CairoExtensions.CreatePathContext ();
				selection_path = g.CreatePolygonPath (ConvertToPolygonSet (SelectionPolygons));
			}

			return selection_path;
		}
	}

	public event EventHandler? SelectionModified;

	/// <summary>
	/// Indicate that the selection has changed.
	/// </summary>
	public void MarkDirty ()
	{
		selection_path = null;

		// Notify any listeners.
		SelectionModified?.Invoke (this, EventArgs.Empty);
	}

	public void Clip (Context g)
	{
		g.AppendPath (SelectionPath);
		g.FillRule = FillRule.EvenOdd;
		g.Clip ();
	}

	/// <summary>
	/// Makes a copy of the Selection.
	/// </summary>
	public DocumentSelection Clone ()
	{
		return new () {
			SelectionPolygons = [.. SelectionPolygons],
			Origin = new PointD (Origin.X, Origin.Y),
			End = new PointD (End.X, End.Y),
			visible = visible,
		};
	}

	/// <summary>
	/// Convert a Pinta Polygon set (Point[][]) to a Clipper Polygon collection (List[List[IntPoint]]).
	/// </summary>
	/// <param name="pintaPolygonSet">A Pinta Polygon set.</param>
	/// <returns>A Clipper Polygon collection.</returns>
	public static List<List<IntPoint>> ConvertToPolygons (IReadOnlyList<IReadOnlyList<PointI>> pintaPolygonSet)
	{
		List<List<IntPoint>> newPolygons = new (pintaPolygonSet.Count);
		foreach (var pA in pintaPolygonSet) {
			List<IntPoint> newPolygon = new (pA.Count);
			foreach (PointI p in pA) {
				newPolygon.Add (new IntPoint (p.X, p.Y));
			}
			newPolygons.Add (newPolygon);
		}
		return newPolygons;
	}

	/// <summary>
	/// Convert a Clipper Polygon collection (List[List[IntPoint]]) to a Pinta Polygon set (Point[][]).
	/// </summary>
	/// <param name="clipperPolygons">A Clipper Polygon collection.</param>
	/// <returns>A Pinta Polygon set.</returns>
	private static IReadOnlyList<IReadOnlyList<PointI>> ConvertToPolygonSet (IReadOnlyList<IReadOnlyList<IntPoint>> clipperPolygons)
	{
		var resultingPolygonSet = new PointI[clipperPolygons.Count][];

		int polygonNumber = 0;

		foreach (var ipL in clipperPolygons) {
			resultingPolygonSet[polygonNumber] = new PointI[ipL.Count];

			int pointNumber = 0;

			foreach (var ip in ipL) {
				resultingPolygonSet[polygonNumber][pointNumber] = new PointI ((int) ip.X, (int) ip.Y);

				++pointNumber;
			}

			++polygonNumber;
		}

		return resultingPolygonSet;
	}

	/// <summary>
	/// Return a transformed copy of the selection.
	/// </summary>
	public DocumentSelection Transform (Matrix transform)
	{
		var newPolygons = new List<List<IntPoint>> ();

		foreach (var ipL in SelectionPolygons) {
			var newPolygon = new List<IntPoint> ();

			foreach (IntPoint ip in ipL) {
				double x = ip.X;
				double y = ip.Y;
				transform.TransformPoint (ref x, ref y);
				newPolygon.Add (new IntPoint ((long) x, (long) y));
			}

			newPolygons.Add (newPolygon);
		}

		var origin = Origin;
		var end = End;
		transform.TransformPoint (ref origin);
		transform.TransformPoint (ref end);

		return new () {
			SelectionPolygons = newPolygons,
			Origin = origin,
			End = end,
			visible = visible,
		};
	}

	/// <summary>
	/// Create an elliptical Selection from a bounding Rectangle.
	/// </summary>
	/// <param name="r">The bounding Rectangle surrounding the ellipse.</param>
	public void CreateEllipseSelection (RectangleD r)
	{
		//These values were calculated in the static CreateEllipsePath method
		//in Pinta.Core.CairoExtensions, so they were used here as well.
		double rx = r.Width / 2; //1/2 of the bounding Rectangle Width.
		double ry = r.Height / 2; //1/2 of the bounding Rectangle Height.
		double cx = r.X + rx; //The middle of the bounding Rectangle, horizontally speaking.
		double cy = r.Y + ry; //The middle of the bounding Rectangle, vertically speaking.
		double c1 = 0.552285; //A constant factor used to give the least approximation error.

		//Clear the Selection Polygons collection to start from a clean slate.
		SelectionPolygons.Clear ();

		//Calculate an appropriate interval at which to increment t based on
		//the bounding Rectangle's Width and Height properties. The increment
		//for t determines how many intermediate Points to calculate for the
		//ellipse. For each curve, t will go from tInterval to 1. The lower
		//the value of tInterval, the higher number of intermediate Points
		//that will be calculated and stored into the Polygon collection.
		double tInterval = 1d / (r.Width + r.Height);

		//Create a new Polygon to store the upcoming ellipse.
		List<IntPoint> newPolygon = new List<IntPoint> ((int) (4d / tInterval)) {
			//These values were also calculated in the CreateEllipsePath method. This is where
			//the ellipse's 4 curves (and all of the Points on each curve) are determined.
			//Note: each curve is consecutive to the previous one, but they *do not* overlap,
			//other than the first/last Point (which is how it is supposed to work).

			//The starting Point.
			new IntPoint ((long) (cx + rx), (long) cy)
		};

		//Curve 1.
		newPolygon.AddRange (CalculateCurvePoints (tInterval,
			cx + rx, cy,
			cx + rx, cy - c1 * ry,
			cx + c1 * rx, cy - ry,
			cx, cy - ry));

		//Curve 2.
		newPolygon.AddRange (CalculateCurvePoints (tInterval,
			cx, cy - ry,
			cx - c1 * rx, cy - ry,
			cx - rx, cy - c1 * ry,
			cx - rx, cy));

		//Curve 3.
		newPolygon.AddRange (CalculateCurvePoints (tInterval,
			cx - rx, cy,
			cx - rx, cy + c1 * ry,
			cx - c1 * rx, cy + ry,
			cx, cy + ry));

		//Curve 4.
		newPolygon.AddRange (CalculateCurvePoints (tInterval,
			cx, cy + ry,
			cx + c1 * rx, cy + ry,
			cx + rx, cy + c1 * ry,
			cx + rx, cy));

		//Add the newly calculated elliptical Polygon.
		SelectionPolygons.Add (newPolygon);
		MarkDirty ();
	}

	/// <summary>
	/// Calculate each intermediate Point in the specified curve, returning Math.Round(1d / tInterval - 1d) number of Points.
	/// </summary>
	/// <param name="tInterval">The increment value for t (should be between 0-1).</param>
	/// <param name="x0">Starting point X (not included in the returned Point(s)).</param>
	/// <param name="y0">Starting point Y (not included in the returned Point(s)).</param>
	/// <param name="x1">Control point 1 X.</param>
	/// <param name="y1">Control point 1 Y.</param>
	/// <param name="x2">Control point 2 X.</param>
	/// <param name="y2">Control point 2 Y.</param>
	/// <param name="x3">Ending point X (included in the returned Point(s)).</param>
	/// <param name="y3">Ending point Y (included in the returned Point(s)).</param>
	/// <returns>Iterator for points of partial polygon</returns>
	private static IEnumerable<IntPoint> CalculateCurvePoints (
		double tInterval,
		double x0,
		double y0,
		double x1,
		double y1,
		double x2,
		double y2,
		double x3,
		double y3)
	{
		//t will go from tInterval to 1d at the interval of tInterval. t starts
		//at tInterval instead of 0d because the first Point in the curve is
		//skipped. This is needed because multiple curves will be placed
		//sequentially after each other and we don't want to have the same
		//Point be added to the Polygon twice.
		for (double t = tInterval; t < 1d; t += tInterval) {
			//There are 3 "layers" in a cubic Bezier curve's calculation. These "layers"
			//must be calculated for each intermediate Point (for each value of t from
			//tInterval to 1d). The Points in each "layer" store [the distance between
			//two consecutive Points from the previous "layer" multiplied by the value
			//of t (which is between 0d-1d)] plus [the position of the first Point of
			//the two consecutive Points from the previous "layer"]. This must be
			//calculated for the X and Y of every consecutive Point in every layer
			//until the last Point possible is reached, which is the Point on the curve.

			//Note: the code below is an optimized version of the commented explanation above.

			double oneMinusT = 1d - t;
			double oneMinusTSquared = oneMinusT * oneMinusT;
			double oneMinusTCubed = oneMinusTSquared * oneMinusT;

			double tSquared = t * t;
			double tCubed = tSquared * t;

			double oneMinusTSquaredTimesTTimesThree = oneMinusTSquared * t * 3d;
			double oneMinusTTimesTSquaredTimesThree = oneMinusT * tSquared * 3d;

			yield return new (
				x: (long) (oneMinusTCubed * x0 + oneMinusTSquaredTimesTTimesThree * x1 + oneMinusTTimesTSquaredTimesThree * x2 + tCubed * x3),
				y: (long) (oneMinusTCubed * y0 + oneMinusTSquaredTimesTTimesThree * y1 + oneMinusTTimesTSquaredTimesThree * y2 + tCubed * y3)
			);
		}
	}

	/// <summary>
	/// Create a rectangular Selection from a Rectangle.
	/// </summary>
	/// <param name="r">The Rectangle.</param>
	public void CreateRectangleSelection (RectangleD r)
	{
		SelectionPolygons.Clear ();
		SelectionPolygons.Add (CreateRectanglePolygon (r));

		Origin = new PointD (r.X, r.Y);
		End = new PointD (r.Right, r.Bottom);

		MarkDirty ();
	}

	/// <summary>
	/// Inverts the selection.
	/// </summary>
	/// <param name='imageSize'>
	/// The size of the document.
	/// </param>
	public void Invert (Size imageSize)
	{
		List<List<IntPoint>> resultingPolygons = [];

		var documentPolygon = CreateRectanglePolygon (new RectangleD (0, 0, imageSize.Width, imageSize.Height));

		// Create a rectangle that is the size of the entire image,
		// and subtract all of the polygons in the current selection from it.
		SelectionClipper.AddPath (documentPolygon, PolyType.ptSubject, true);
		SelectionClipper.AddPaths (SelectionPolygons, PolyType.ptClip, true);
		SelectionClipper.Execute (ClipType.ctDifference, resultingPolygons);

		SelectionClipper.Clear ();

		SelectionPolygons = resultingPolygons;
		MarkDirty ();
	}

	/// <summary>
	/// Expands or contracts the selection by a specified amount.
	/// </summary>
	/// <param name='delta'>
	/// The amount to expand the selection by. A positive value will expand the selection, and a negative value will contract the selection.
	/// </param>
	public void Offset (double delta)
	{
		// Remove any self-intersections from the selection polygons.
		List<List<IntPoint>> simplePolygons = [];

		SelectionClipper.AddPaths (SelectionPolygons, PolyType.ptSubject, true);
		SelectionClipper.Execute (ClipType.ctUnion, simplePolygons, PolyFillType.pftNonZero, PolyFillType.pftNonZero);
		SelectionClipper.Clear ();

		// Expand or contract the selection by the specified amount.
		List<List<IntPoint>> offsetPolygons = [];

		ClipperOffset clipperOffset = new ();
		clipperOffset.AddPaths (simplePolygons, JoinType.jtMiter, EndType.etClosedPolygon);
		clipperOffset.Execute (ref offsetPolygons, delta);

		SelectionPolygons = offsetPolygons;
		MarkDirty ();
	}

	private static List<IntPoint> CreateRectanglePolygon (RectangleD r)
	{
		// The 4 corners of the Rectangle.
		int corner1X = (int) Math.Round (r.X);
		int corner1Y = (int) Math.Round (r.Y);
		int corner2X = (int) Math.Round (r.X + r.Width);
		int corner2Y = (int) Math.Round (r.Y + r.Height);

		// Store each of the 4 corners of the Rectangle in the Polygon, and then store
		// the first corner again. It is important to note that the order of the
		// corners being added (clockwise) and the first/last Point being the same
		// should be kept this way; otherwise, problems could result.
		List<IntPoint> newPolygon = [
			new (corner1X, corner1Y),
			new (corner2X, corner1Y),
			new (corner2X, corner2Y),
			new (corner1X, corner2Y),
			new (corner1X, corner1Y)
		];

		return newPolygon;
	}

	/// <summary>
	/// Resets the selection.
	/// </summary>
	public void Clear ()
	{
		SelectionPolygons.Clear ();
		Origin = new PointD (0, 0);
		End = new PointD (0, 0);
		MarkDirty ();
	}

	/// <summary>
	/// Returns a rectangle that encloses the entire selection.
	/// </summary>
	public RectangleD GetBounds ()
	{
		double minX = double.MaxValue;
		double minY = double.MaxValue;
		double maxX = double.MinValue;
		double maxY = double.MinValue;

		// Calculate the minimum rectangular bounds that surround the current selection.
		foreach (List<IntPoint> li in SelectionPolygons) {
			foreach (IntPoint ip in li) {
				minX = Math.Min (minX, ip.X);
				minY = Math.Min (minY, ip.Y);
				maxX = Math.Max (maxX, ip.X);
				maxY = Math.Max (maxY, ip.Y);
			}
		}

		// Invalid (empty) rectangle - avoid overflow from maxX - minX
		if (minX > maxX || minY > maxY)
			return new ();

		return new (
			minX,
			minY,
			maxX - minX,
			maxY - minY);
	}
}
