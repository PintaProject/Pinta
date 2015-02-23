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
using Pinta.Core;

namespace Pinta.Tools
{
    public class EllipseEngine : ShapeEngine
	{
		/// <summary>
		/// Create a new EllipseEngine.
		/// </summary>
		/// <param name="parent_layer">The parent UserLayer for the re-editable DrawingLayer.</param>
		/// <param name="drawing_layer">An existing ReEditableLayer to reuse. This is for cloning only. If not cloning, pass in null.</param>
		/// <param name="antialiasing">Whether or not antialiasing is enabled.</param>
		/// <param name="outline_color">The outline color for the shape.</param>
		/// <param name="fill_color">The fill color for the shape.</param>
		/// <param name="brush_width">The width of the outline of the shape.</param>
        public EllipseEngine (UserLayer parent_layer, ReEditableLayer drawing_layer,
                              bool antialiasing, Color outline_color, Color fill_color,
                              int brush_width)
            : base (parent_layer, drawing_layer, BaseEditEngine.ShapeTypes.Ellipse,
                    antialiasing, true, outline_color, fill_color, brush_width)
		{
			
		}

        private EllipseEngine (EllipseEngine src)
            : base (src)
        {
        }

        public override ShapeEngine Clone ()
		{
            return new EllipseEngine (this);
		}

		/// <summary>
		/// Generate each point in an elliptic shape and store the result in GeneratedPoints.
		/// <param name="brush_width">The width of the brush that will be used to draw the shape.</param>
		/// </summary>
        public override void GeneratePoints (int brush_width)
		{
			List<GeneratedPoint> points = new List<GeneratedPoint>();

			//An ellipse requires exactly 4 control points in order to draw anything.
			if (ControlPoints.Count == 4)
			{
				//This is mostly for time efficiency/optimization, but it can also help readability.
				PointD cp0 = ControlPoints[0].Position, cp1 = ControlPoints[1].Position,
					cp2 = ControlPoints[2].Position, cp3 = ControlPoints[3].Position;

				//An ellipse also requires that all 4 control points compose a perfect rectangle parallel/perpendicular to the window.
				//So, confirm that it is indeed a perfect rectangle.

				bool perfectRectangle = false;

				if (cp0.X == cp1.X)
				{
					if (cp0.Y == cp3.Y && cp1.Y == cp2.Y && cp2.X == cp3.X)
					{
						perfectRectangle = true;
					}
				}
				else if (cp0.Y == cp1.Y)
				{
					if (cp0.X == cp3.X && cp1.X == cp2.X && cp2.Y == cp3.Y)
					{
						perfectRectangle = true;
					}
				}

				if (perfectRectangle)
				{
					//It is expected that the 4 control points always form a perfect rectangle parallel/perpendicular to the window.
					//However, we must first determine which control point is at the top left and which is at the bottom right.
					//It is also expected that the 4 control points are adjacent to each other by index and position, e.g.: 0, 1, 2, 3.

					PointD topLeft = cp0;
					PointD bottomRight = cp0;

					//Compare the second point with the first.
					if (cp1.X < topLeft.X || cp1.Y < topLeft.Y)
					{
						//The second point is either more left or more up than the first.

						topLeft = cp1;

						//Compare the third point with the second.
						if (cp2.X < topLeft.X || cp2.Y < topLeft.Y)
						{
							//The third point is either more left or more up than the second.

							topLeft = cp2;

							//The first point remains the bottom right.
						}
						else
						{
							//The third point is neither more left nor more up than the second.

							//The second point remains the top left.

							bottomRight = cp3;
						}
					}
					else
					{
						//The second point is neither more left nor more up than the first.

						PointD secondPoint = cp1;

						//Compare the third point with the second.
						if (cp2.X < secondPoint.X || cp2.Y < secondPoint.Y)
						{
							//The third point is either more left or more up than the second.

							topLeft = cp3;
							bottomRight = cp1;
						}
						else
						{
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

					double rx = width / 2d; //1/2 of the bounding Rectangle Width.
					double ry = height / 2d; //1/2 of the bounding Rectangle Height.
					double cx = topLeft.X + rx; //The middle of the bounding Rectangle, horizontally speaking.
					double cy = topLeft.Y + ry; //The middle of the bounding Rectangle, vertically speaking.
					double c1 = 0.5522847498307933984022516322796d; //tan(pi / 8d) * 4d / 3d ~= 0.5522847498307933984022516322796d

					points.AddRange(calculateCurvePoints(tInterval,
						cx + rx, cy,
						cx + rx, cy - c1 * ry,
						cx + c1 * rx, cy - ry,
						cx, cy - ry,
						3));

					points.AddRange(calculateCurvePoints(tInterval,
						cx, cy - ry,
						cx - c1 * rx, cy - ry,
						cx - rx, cy - c1 * ry,
						cx - rx, cy,
						0));

					points.AddRange(calculateCurvePoints(tInterval,
						cx - rx, cy,
						cx - rx, cy + c1 * ry,
						cx - c1 * rx, cy + ry,
						cx, cy + ry,
						1));

					points.AddRange(calculateCurvePoints(tInterval,
						cx, cy + ry,
						cx + c1 * rx, cy + ry,
						cx + rx, cy + c1 * ry,
						cx + rx, cy,
						2));

                    // Close the curve.
					points.Add(new GeneratedPoint(new PointD(cx + rx, cy), 3));
				}
			}
			
			//Make sure there are now generated points; otherwise, one of the ellipse conditions was not met.
			if (points.Count == 0)
			{
				//Something went wrong. Just copy the control points. Note: it's important that there be many generated points even if
				//everything is just a linear connection of control points. This is because the generated points are used in the check
				//that determines if the mouse clicks on the shape.

				int nextNum;

				PointD currentPoint, nextPoint;

				//Go through each control point.
				for (int currentNum = 0; currentNum < ControlPoints.Count; ++currentNum)
				{
					//Determine the next control point.

					nextNum = currentNum + 1;

					if (nextNum >= ControlPoints.Count)
					{
						nextNum = 0;
					}

					currentPoint = ControlPoints[currentNum].Position;
					nextPoint = ControlPoints[nextNum].Position;

					//Lerp from the current point to the next point.
					for (float lerpPos = 0.0f; lerpPos < 1.0f; lerpPos += 0.01f)
					{
						points.Add(new GeneratedPoint(Utility.Lerp(currentPoint, nextPoint, lerpPos), currentNum));
					}
				}
			}

			GeneratedPoints = points.ToArray();
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
		protected List<GeneratedPoint> calculateCurvePoints(
			double tInterval,
			double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3,
			int cPIndex)
		{
			List<GeneratedPoint> calculatedPoints = new List<GeneratedPoint>((int)(1d / tInterval));

			double oneMinusT;
			double oneMinusTSquared;
			double oneMinusTCubed;

			double tSquared;
			double tCubed;

			double oneMinusTSquaredTimesTTimesThree;
			double oneMinusTTimesTSquaredTimesThree;

			for (double t = 0; t < 1d; t += tInterval)
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

				calculatedPoints.Add(new GeneratedPoint(new PointD(
					(oneMinusTCubed * x0 + oneMinusTSquaredTimesTTimesThree * x1 + oneMinusTTimesTSquaredTimesThree * x2 + tCubed * x3),
					(oneMinusTCubed * y0 + oneMinusTSquaredTimesTTimesThree * y1 + oneMinusTTimesTSquaredTimesThree * y2 + tCubed * y3)),
					cPIndex));
			}

			//Return the partial Polygon containing the calculated Points in the curve.
			return calculatedPoints;
		}
	}
}
