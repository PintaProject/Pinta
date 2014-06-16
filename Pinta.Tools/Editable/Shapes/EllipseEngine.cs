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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cairo;

namespace Pinta.Tools
{
	public class EllipseEngine: ShapeEngine
	{
		/// <summary>
		/// Create a new EllipseEngine.
		/// </summary>
		/// <param name="passedAA">Whether or not antialiasing is enabled.</param>
		public EllipseEngine(bool passedAA): base(passedAA)
		{
			
		}

		public override ShapeEngine PartialClone()
		{
			EllipseEngine clonedCE = new EllipseEngine(AntiAliasing);

			clonedCE.ControlPoints = ControlPoints.Select(i => i.Clone()).ToList();

			//Don't clone the GeneratedPoints or OrganizedPoints, as they will be calculated.

			clonedCE.DashPattern = DashPattern;

			return clonedCE;   
		}

		/// <summary>
		/// Generate each point in an elliptic shape that passes through the control points and store the result in GeneratedPoints.
		/// </summary>
		public override void GeneratePoints()
		{
			List<PointD> generatedPoints = new List<PointD>();


			//An ellipse requires exactly 4 control points in order to draw anything.
			if (ControlPoints.Count == 4)
			{
				//It is expected that the 4 control points always form a rectangle parallel/perpendicular to the window.
				//However, we must first determine which control point is at the top left and which is at the bottom right.
				//It is also expected that the 4 control points are adjacent to each other by index and position, e.g.: 0, 1, 2, 3.
				
				PointD topLeft = ControlPoints[0].Position;
				PointD bottomRight = ControlPoints[0].Position;

				//Compare the second point with the first.
				if (ControlPoints[1].Position.X < topLeft.X || ControlPoints[1].Position.Y < topLeft.Y)
				{
					//The second point is either more left or more up than the first.
					
					topLeft = ControlPoints[1].Position;

					//Compare the third point with the second.
					if (ControlPoints[2].Position.X < topLeft.X || ControlPoints[2].Position.Y < topLeft.Y)
					{
						//The third point is either more left or more up than the second.

						topLeft = ControlPoints[2].Position;

						//The first point remains the bottom right.
					}
					else
					{
						//The third point is neither more left nor more up than the second.

						//The second point remains the top left.

						bottomRight = ControlPoints[3].Position;
					}
				}
				else
				{
					//The second point is neither more left nor more up than the first.

					PointD secondPoint = ControlPoints[1].Position;

					//Compare the third point with the second.
					if (ControlPoints[2].Position.X < secondPoint.X || ControlPoints[2].Position.Y < secondPoint.Y)
					{
						//The third point is either more left or more up than the second.

						topLeft = ControlPoints[3].Position;
						bottomRight = ControlPoints[1].Position;
					}
					else
					{
						//The third point is neither more left nor more up than the second.

						//The first point remains the top left.

						bottomRight = ControlPoints[2].Position;
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
				double tInterval = 1d / (width + height);

				double rx = width / 2; //1/2 of the bounding Rectangle Width.
				double ry = height / 2; //1/2 of the bounding Rectangle Height.
				double cx = topLeft.X + rx; //The middle of the bounding Rectangle, horizontally speaking.
				double cy = topLeft.Y + ry; //The middle of the bounding Rectangle, vertically speaking.
				double c1 = 0.5522847498307933984022516322796d; //tan(pi / 8d) * 4d / 3d = 0.5522847498307933984022516322796d

				generatedPoints.Add(new PointD(cx + rx, cy));

				generatedPoints.AddRange(calculateCurvePoints(tInterval,
					cx + rx, cy,
					cx + rx, cy - c1 * ry,
					cx + c1 * rx, cy - ry,
					cx, cy - ry));

				generatedPoints.AddRange(calculateCurvePoints(tInterval,
					cx, cy - ry,
					cx - c1 * rx, cy - ry,
					cx - rx, cy - c1 * ry,
					cx - rx, cy));

				generatedPoints.AddRange(calculateCurvePoints(tInterval,
					cx - rx, cy,
					cx - rx, cy + c1 * ry,
					cx - c1 * rx, cy + ry,
					cx, cy + ry));

				generatedPoints.AddRange(calculateCurvePoints(tInterval,
					cx, cy + ry,
					cx + c1 * rx, cy + ry,
					cx + rx, cy + c1 * ry,
					cx + rx, cy));
			}
			else
			{
				//Something went wrong. Just copy the control points.
				foreach (ControlPoint cp in ControlPoints)
				{
					generatedPoints.Add(cp.Position);
				}
			}


			GeneratedPoints = generatedPoints.ToArray();
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
		/// <returns></returns>
		protected List<PointD> calculateCurvePoints(double tInterval, double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
		{
			//Create a new partial Polygon to store the calculated Points.
			List<PointD> calculatedPoints = new List<PointD>((int)(1d / tInterval));

			double oneMinusT;
			double oneMinusTSquared;
			double oneMinusTCubed;

			double tSquared;
			double tCubed;

			double oneMinusTSquaredTimesTTimesThree;
			double oneMinusTTimesTSquaredTimesThree;

			//t will go from tInterval to 1d at the interval of tInterval. t starts
			//at tInterval instead of 0d because the first Point in the curve is
			//skipped. This is needed because multiple curves will be placed
			//sequentially after each other and we don't want to have the same
			//Point be added to the Polygon twice.
			for (double t = tInterval; t < 1d; t += tInterval)
			{
				//There are 3 "layers" in a cubic Bezier curve's calculation. These "layers"
				//must be calculated for each intermediate Point (for each value of t from
				//tInterval to 1d). The Points in each "layer" store [the distance between
				//two consecutive Points from the previous "layer" multipled by the value
				//of t (which is between 0d-1d)] plus [the position of the first Point of
				//the two consecutive Points from the previous "layer"]. This must be
				//calculated for the X and Y of every consecutive Point in every layer
				//until the last Point possible is reached, which is the Point on the curve.

				//Note: the code below is an optimized version of the commented explanation above.

				oneMinusT = 1d - t;
				oneMinusTSquared = oneMinusT * oneMinusT;
				oneMinusTCubed = oneMinusTSquared * oneMinusT;

				tSquared = t * t;
				tCubed = tSquared * t;

				oneMinusTSquaredTimesTTimesThree = oneMinusTSquared * t * 3d;
				oneMinusTTimesTSquaredTimesThree = oneMinusT * tSquared * 3d;

				calculatedPoints.Add(new PointD(
					(oneMinusTCubed * x0 + oneMinusTSquaredTimesTTimesThree * x1 + oneMinusTTimesTSquaredTimesThree * x2 + tCubed * x3),
					(oneMinusTCubed * y0 + oneMinusTSquaredTimesTTimesThree * y1 + oneMinusTTimesTSquaredTimesThree * y2 + tCubed * y3)));
			}

			//Return the partial Polygon containing the calculated Points in the curve.
			return calculatedPoints;
		}
	}
}
