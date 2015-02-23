// 
// LineCurveSeriesEngine.cs
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
	public class LineCurveSeriesEngine: ShapeEngine
	{
        public Arrow Arrow1 = new Arrow(), Arrow2 = new Arrow();

		/// <summary>
		/// Create a new LineCurveSeriesEngine.
		/// </summary>
		/// <param name="parent_layer">The parent UserLayer for the re-editable DrawingLayer.</param>
		/// <param name="drawing_layer">An existing ReEditableLayer to reuse. This is for cloning only. If not cloning, pass in null.</param>
		/// <param name="shape_type">The owner EditEngine.</param>
		/// <param name="antialiasing">Whether or not antialiasing is enabled.</param>
		/// <param name="closed">Whether or not the shape is closed (first and last points are connected).</param>
		/// <param name="outline_color">The outline color for the shape.</param>
		/// <param name="fill_color">The fill color for the shape.</param>
		/// <param name="brush_width">The width of the outline of the shape.</param>
		public LineCurveSeriesEngine(UserLayer parentLayer, ReEditableLayer passedDrawingLayer, BaseEditEngine.ShapeTypes passedShapeType,
			bool passedAA, bool passedClosed, Color passedOutlineColor, Color passedFillColor, int passedBrushWidth) : base(parentLayer,
			passedDrawingLayer, passedShapeType, passedAA, passedClosed, passedOutlineColor, passedFillColor, passedBrushWidth)
		{
			
		}

        private LineCurveSeriesEngine (LineCurveSeriesEngine src)
            : base (src)
        {
            Arrow1 = src.Arrow1.Clone ();
            Arrow2 = src.Arrow2.Clone ();
        }

        public override ShapeEngine Clone ()
		{
            return new LineCurveSeriesEngine (this);
		}
		
		/// <summary>
		/// Generate each point in an line/curve series (cardinal spline polynomial curve) shape that passes through the control points,
		/// and store the result in GeneratedPoints.
		/// <param name="brush_width">The width of the brush that will be used to draw the shape.</param>
		/// </summary>
        public override void GeneratePoints (int brush_width)
        {
			List<GeneratedPoint> generatedPoints = new List<GeneratedPoint>();

			if (ControlPoints.Count < 2)
			{
				generatedPoints.Add(new GeneratedPoint(ControlPoints[0].Position, 0));
			}
			else
			{
				//Generate tangents for each of the smaller cubic Bezier curves that make up each segment of the resulting curve.

				//The tension calculated for each point is a gradient between the previous
				//control point's tension and the following control point's tension.

				//Stores all of the tangent values.
				List<PointD> bezierTangents = new List<PointD>();

				int pointCount = ControlPoints.Count - 1;
				double pointCountDouble = (double)pointCount;
				double tensionForPoint;

				//Calculate the first tangent.
				if (Closed)
				{
					bezierTangents.Add(new PointD(
						ControlPoints[0].Tension * (ControlPoints[1].Position.X - ControlPoints[pointCount].Position.X),
						ControlPoints[0].Tension * (ControlPoints[1].Position.Y - ControlPoints[pointCount].Position.Y)));
				}
				else
				{
					bezierTangents.Add(new PointD(
						ControlPoints[0].Tension * (ControlPoints[1].Position.X - ControlPoints[0].Position.X),
						ControlPoints[0].Tension * (ControlPoints[1].Position.Y - ControlPoints[0].Position.Y)));
				}

				//Calculate all of the middle tangents.
				for (int i = 1; i < pointCount; ++i)
				{
					tensionForPoint = ControlPoints[i].Tension * (double)i / pointCountDouble;

					bezierTangents.Add(new PointD(
						tensionForPoint *
							(ControlPoints[i + 1].Position.X - ControlPoints[i - 1].Position.X),
						tensionForPoint *
							(ControlPoints[i + 1].Position.Y - ControlPoints[i - 1].Position.Y)));
				}

				//Calculate the last tangent.
				if (Closed)
				{
					bezierTangents.Add(new PointD(
						ControlPoints[pointCount].Tension *
							(ControlPoints[0].Position.X - ControlPoints[pointCount - 1].Position.X),
						ControlPoints[pointCount].Tension *
							(ControlPoints[0].Position.Y - ControlPoints[pointCount - 1].Position.Y)));
				}
				else
				{
					bezierTangents.Add(new PointD(
						ControlPoints[pointCount].Tension *
							(ControlPoints[pointCount].Position.X - ControlPoints[pointCount - 1].Position.X),
						ControlPoints[pointCount].Tension *
							(ControlPoints[pointCount].Position.Y - ControlPoints[pointCount - 1].Position.Y)));
				}


				int iMinusOne;

				//Generate the resulting curve's points with consecutive cubic Bezier curves that
				//use the given points as end points and the calculated tangents as control points.
				for (int i = 1; i < ControlPoints.Count; ++i)
				{
					iMinusOne = i - 1;

					generatedPoints.AddRange(GenerateCubicBezierCurvePoints(
						ControlPoints[iMinusOne].Position,
						new PointD(
							ControlPoints[iMinusOne].Position.X + bezierTangents[iMinusOne].X,
							ControlPoints[iMinusOne].Position.Y + bezierTangents[iMinusOne].Y),
						new PointD(
							ControlPoints[i].Position.X - bezierTangents[i].X,
							ControlPoints[i].Position.Y - bezierTangents[i].Y),
						ControlPoints[i].Position,
						i));
				}

				if (Closed)
				{
					// Close the shape.

					iMinusOne = ControlPoints.Count - 1;

					generatedPoints.AddRange(GenerateCubicBezierCurvePoints(
							ControlPoints[iMinusOne].Position,
							new PointD(
								ControlPoints[iMinusOne].Position.X + bezierTangents[iMinusOne].X,
								ControlPoints[iMinusOne].Position.Y + bezierTangents[iMinusOne].Y),
							new PointD(
								ControlPoints[0].Position.X - bezierTangents[0].X,
								ControlPoints[0].Position.Y - bezierTangents[0].Y),
							ControlPoints[0].Position,
							0));
				}
			}

			GeneratedPoints = generatedPoints.ToArray();
        }

		/// <summary>
		/// Generate each point in a cubic Bezier curve given the end points and control points.
		/// </summary>
		/// <param name="p0">The first end point that the curve passes through.</param>
		/// <param name="p1">The first control point that the curve does not necessarily pass through.</param>
		/// <param name="p2">The second control point that the curve does not necessarily pass through.</param>
		/// <param name="p3">The second end point that the curve passes through.</param>
		/// <param name="cPIndex">The index of the previous ControlPoint to the generated points.</param>
		/// <returns>The List of generated points.</returns>
		protected static List<GeneratedPoint> GenerateCubicBezierCurvePoints(PointD p0, PointD p1, PointD p2, PointD p3, int cPIndex)
		{
			List<GeneratedPoint> resultList = new List<GeneratedPoint>();


			//Note: this must be low enough for mouse clicks to be properly considered on/off the curve at any given point.
			double tInterval = .025d;

			double oneMinusT;
			double oneMinusTSquared;
			double oneMinusTCubed;

			double tSquared;
			double tCubed;

			double oneMinusTSquaredTimesTTimesThree;
			double oneMinusTTimesTSquaredTimesThree;

			//t will go from 0d to 1d at the interval of tInterval.
			for (double t = 0d; t < 1d + tInterval; t += tInterval)
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

				//Resulting Point = (1 - t) ^ 3 * p0 + 3 * (1 - t) ^ 2 * t * p1 + 3 * (1 - t) * t ^ 2 * p2 + t ^ 3 * p3
				//This is done for both the X and Y given a value t going from 0d to 1d at a very small interval
				//and given 4 points p0, p1, p2, and p3, where p0 and p3 are end points and p1 and p2 are control points.

				resultList.Add(new GeneratedPoint(new PointD(
					oneMinusTCubed * p0.X + oneMinusTSquaredTimesTTimesThree * p1.X + oneMinusTTimesTSquaredTimesThree * p2.X + tCubed * p3.X,
					oneMinusTCubed * p0.Y + oneMinusTSquaredTimesTTimesThree * p1.Y + oneMinusTTimesTSquaredTimesThree * p2.Y + tCubed * p3.Y),
					cPIndex));
			}


			return resultList;
		}
	}
}
