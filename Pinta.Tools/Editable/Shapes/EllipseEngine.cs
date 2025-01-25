//
// EllipseEngine.cs
//
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
//
// Copyright (c) 2014 Andrew Davis, GSoC 2014
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

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Cairo;
using Pinta.Core;

namespace Pinta.Tools;

public sealed class EllipseEngine : ShapeEngine
{
	/// <summary>
	/// Create a new EllipseEngine.
	/// </summary>
	/// <param name="parentLayer">The parent UserLayer for the re-editable DrawingLayer.</param>
	/// <param name="drawingLayer">An existing ReEditableLayer to reuse. This is for cloning only. If not cloning, pass in null.</param>
	/// <param name="antialiasing">Whether or not antialiasing is enabled.</param>
	/// <param name="outlineColor">The outline color for the shape.</param>
	/// <param name="fillColor">The fill color for the shape.</param>
	/// <param name="brushWidth">The width of the outline of the shape.</param>
	/// <param name="lineCap">Defines the edge of the line drawn.</param>
	public EllipseEngine (
		UserLayer parentLayer,
		ReEditableLayer? drawingLayer,
		bool antialiasing,
		Color outlineColor,
		Color fillColor,
		int brushWidth,
		LineCap lineCap)
	: base (
		parentLayer,
		drawingLayer,
		BaseEditEngine.ShapeTypes.Ellipse,
		antialiasing,
		true,
		outlineColor,
		fillColor,
		brushWidth,
		lineCap)
	{ }

	private EllipseEngine (EllipseEngine src)
		: base (src)
	{
	}

	public override ShapeEngine Clone ()
	{
		return new EllipseEngine (this);
	}

	private static bool IsPerfectRectangle (
		PointD cp0,
		PointD cp1,
		PointD cp2,
		PointD cp3)
	{
		if (cp0.X == cp1.X) {
			if (cp0.Y == cp3.Y && cp1.Y == cp2.Y && cp2.X == cp3.X) {
				return true;
			}
		} else if (cp0.Y == cp1.Y) {
			if (cp0.X == cp3.X && cp1.X == cp2.X && cp2.Y == cp3.Y) {
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Generate each point in an elliptic shape and store the result in GeneratedPoints.
	/// <param name="brush_width">The width of the brush that will be used to draw the shape.</param>
	/// </summary>
	public override void GeneratePoints (int brush_width)
	{
		var points = CreatePoints ().ToImmutableArray ();
		var fallbackPoints = CreateFallbackPoints (points);
		GeneratedPoints = [.. points, .. fallbackPoints];
	}

	private IEnumerable<GeneratedPoint> CreateFallbackPoints (ImmutableArray<GeneratedPoint> points)
	{
		//Make sure there are now generated points; otherwise, one of the ellipse conditions was not met.
		if (points.Length != 0)
			yield break;

		//Something went wrong. Just copy the control points. Note: it's important that there be many generated points even if
		//everything is just a linear connection of control points. This is because the generated points are used in the check
		//that determines if the mouse clicks on the shape.

		//Go through each control point.
		for (int currentNum = 0; currentNum < ControlPoints.Count; ++currentNum) {
			//Determine the next control point.

			int nextNum = currentNum + 1;

			if (nextNum >= ControlPoints.Count)
				nextNum = 0;

			PointD currentPoint = ControlPoints[currentNum].Position;
			PointD nextPoint = ControlPoints[nextNum].Position;

			//Lerp from the current point to the next point.
			for (float lerpPos = 0.0f; lerpPos < 1.0f; lerpPos += 0.01f)
				yield return new GeneratedPoint (
					Utility.Lerp (currentPoint, nextPoint, lerpPos),
					currentNum);
		}
	}

	private IEnumerable<GeneratedPoint> CreatePoints ()
	{
		//An ellipse requires exactly 4 control points in order to draw anything.
		if (ControlPoints.Count != 4)
			yield break;

		//This is mostly for time efficiency/optimization, but it can also help readability.
		PointD
			cp0 = ControlPoints[0].Position,
			cp1 = ControlPoints[1].Position,
			cp2 = ControlPoints[2].Position,
			cp3 = ControlPoints[3].Position;

		//An ellipse also requires that all 4 control points compose a perfect rectangle parallel/perpendicular to the window.
		//So, confirm that it is indeed a perfect rectangle.
		bool perfectRectangle = IsPerfectRectangle (cp0, cp1, cp2, cp3);

		if (!perfectRectangle)
			yield break;

		//It is expected that the 4 control points always form a perfect rectangle parallel/perpendicular to the window.
		//However, we must first determine which control point is at the top left and which is at the bottom right.
		//It is also expected that the 4 control points are adjacent to each other by index and position, e.g.: 0, 1, 2, 3.

		PointD topLeft = cp0;
		PointD bottomRight = cp0;

		//Compare the second point with the first.
		if (cp1.X < topLeft.X || cp1.Y < topLeft.Y) {
			//The second point is either more left or more up than the first.

			topLeft = cp1;

			//Compare the third point with the second.
			if (cp2.X < topLeft.X || cp2.Y < topLeft.Y) {
				//The third point is either more left or more up than the second.

				topLeft = cp2;

				//The first point remains the bottom right.
			} else {
				//The third point is neither more left nor more up than the second.

				//The second point remains the top left.

				bottomRight = cp3;
			}
		} else {
			//The second point is neither more left nor more up than the first.

			PointD secondPoint = cp1;

			//Compare the third point with the second.
			if (cp2.X < secondPoint.X || cp2.Y < secondPoint.Y) {
				//The third point is either more left or more up than the second.

				topLeft = cp3;
				bottomRight = cp1;
			} else {
				//The third point is neither more left nor more up than the second.

				//The first point remains the top left.

				bottomRight = cp2;
			}
		}

		//Now we can calculate the width and height.
		double width = bottomRight.X - topLeft.X;
		double height = bottomRight.Y - topLeft.Y;

		//Some elliptic math code taken from Cairo Extensions, and some from DocumentSelection code written for GSoC 2013.

		//Calculate an appropriate interval at which to increment t based on
		//the bounding rectangle's width and height properties. The increment
		//for t determines how many intermediate Points to calculate for the
		//ellipse. For each curve, t will go from tInterval to 1. The lower
		//the value of tInterval, the higher number of intermediate Points
		//that will be calculated and stored into the Polygon collection.
		double tInterval = .02d;

		double r_x = width / 2d; //1/2 of the bounding Rectangle Width.
		double r_y = height / 2d; //1/2 of the bounding Rectangle Height.

		//The middle of the bounding Rectangle...
		PointD c = new (
			X: topLeft.X + r_x, // ...Horizontally speaking
			Y: topLeft.Y + r_y); // ...Vertically speaking

		const double c_1 = 0.5522847498307933984022516322796d; //tan(pi / 8d) * 4d / 3d ~= 0.5522847498307933984022516322796d

		// Save first quadrant to later close the ellipse
		var first_quadrant = CalculateCurvePoints (
			tInterval,
			c.X + r_x, c.Y,
			c.X + r_x, c.Y - c_1 * r_y,
			c.X + c_1 * r_x, c.Y - r_y,
			c.X, c.Y - r_y,
			3);

		foreach (var p in first_quadrant)
			yield return p;

		foreach (
			var p in
			CalculateCurvePoints (
				tInterval,
				c.X, c.Y - r_y,
				c.X - c_1 * r_x, c.Y - r_y,
				c.X - r_x, c.Y - c_1 * r_y,
				c.X - r_x, c.Y,
				0
			)
		) yield return p;

		foreach (
			var p in
			CalculateCurvePoints (
				tInterval,
				c.X - r_x, c.Y,
				c.X - r_x, c.Y + c_1 * r_y,
				c.X - c_1 * r_x, c.Y + r_y,
				c.X, c.Y + r_y,
				1
			)
		) yield return p;

		foreach (
			var p in
			CalculateCurvePoints (
				tInterval,
				c.X, c.Y + r_y,
				c.X + c_1 * r_x, c.Y + r_y,
				c.X + r_x, c.Y + c_1 * r_y,
				c.X + r_x, c.Y,
				2
			)
		) yield return p;

		// Close the curve.
		// Do not close the curve if no dash pattern used, or else dash pattern wraps past the end of the ellipse
		if (!CairoExtensions.IsValidDashPattern (DashPattern)) {
			yield return first_quadrant.Take (2).Last ();
			// Closes the curve in more extreme, near-flat circle (width >>> height) cases.
			yield return first_quadrant.Take (3).Last ();
		}
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
	/// <param name="cPIndex">The index of the previous ControlPoint to the generated points.</param>
	/// <returns></returns>
	private static IEnumerable<GeneratedPoint> CalculateCurvePoints (
		double tInterval,
		double x0, double y0,
		double x1, double y1,
		double x2, double y2,
		double x3, double y3,
		int cPIndex)
	{
		//Generates points of partial Polygon containing the calculated Points in the curve.
		for (double t = 0; t < 1d; t += tInterval) {
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
				new PointD (
					X: oneMinusTCubed * x0 + oneMinusTSquaredTimesTTimesThree * x1 + oneMinusTTimesTSquaredTimesThree * x2 + tCubed * x3,
					Y: oneMinusTCubed * y0 + oneMinusTSquaredTimesTTimesThree * y1 + oneMinusTTimesTSquaredTimesThree * y2 + tCubed * y3),
				cPIndex
			);
		}
	}
}
