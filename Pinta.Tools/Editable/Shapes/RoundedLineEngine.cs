// 
// RoundedLineEngine.cs
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
	public class RoundedLineEngine: ShapeEngine
	{
		public double Radius;

		/// <summary>
		/// Create a new RoundedLineEngine.
		/// </summary>
		/// <param name="parentLayer">The parent UserLayer for the re-editable DrawingLayer.</param>
		/// <param name="passedAA">Whether or not antialiasing is enabled.</param>
		public RoundedLineEngine(UserLayer parentLayer, double passedRadius, bool passedAA)
			: base(parentLayer, BaseEditEngine.ShapeTypes.RoundedLineSeries, passedAA, true)
		{
			Radius = passedRadius;
		}

		public override ShapeEngine PartialClone()
		{
			RoundedLineEngine clonedCE = new RoundedLineEngine(parentLayer, Radius, AntiAliasing);

			clonedCE.ControlPoints = ControlPoints.Select(i => i.Clone()).ToList();

			//Don't clone the GeneratedPoints or OrganizedPoints, as they will be calculated.

			clonedCE.DashPattern = DashPattern;

			return clonedCE;
		}

		/// <summary>
		/// Generate each point in a rounded line shape that passes through the control points, and store the result in GeneratedPoints.
		/// </summary>
		public override void GeneratePoints()
		{
			List<PointD> generatedPoints = new List<PointD>();


			for (int currentIndex = 0; currentIndex < ControlPoints.Count; ++currentIndex)
			{
				//Determine the positions of the current, next, and double next ControlPoints.

				int nextIndex = currentIndex + 1;

				if (nextIndex >= ControlPoints.Count)
				{
					nextIndex = 0;
				}

				int doubleNextIndex = nextIndex + 1;

				if (doubleNextIndex >= ControlPoints.Count)
				{
					doubleNextIndex = 0;
				}

				PointD currentPosition = ControlPoints[currentIndex].Position;
				PointD nextPosition = ControlPoints[nextIndex].Position;
				PointD doubleNextPosition = ControlPoints[doubleNextIndex].Position;


				//Calculate the distance between the current and next point and the next and double next point.
				double currentDistance = currentPosition.Distance(nextPosition);
				double nextDistance = nextPosition.Distance(doubleNextPosition);


				//The radius value used can change between ControlPoints depending on their proximity to each other.
				double currentRadius = Radius;

				//Reduce the radius according to the distance between adjacent ControlPoints if necessary.
				if (currentRadius > currentDistance / 2d || currentRadius > nextDistance / 2d)
				{
					currentRadius = Math.Min(currentDistance / 2d, nextDistance / 2d);
				}


				//Calculate the current offset ratio, which is the ratio of the radius to the distance between the current and next points.

				double currentOffsetRatio;

				//Prevent a divide by 0 error.
				if (currentDistance <= 0d)
				{
					currentOffsetRatio = 0d;
				}
				else
				{
					currentOffsetRatio = currentRadius / currentDistance;

					if (currentOffsetRatio > 1d)
					{
						currentOffsetRatio = 1d;
					}
				}


				//Calculate the next offset ratio, which is the ratio of the radius to the distance between the next and double next points.

				double nextOffsetRatio;

				//Prevent a divide by 0 error.
				if (nextDistance <= 0d)
				{
					nextOffsetRatio = 0d;
				}
				else
				{
					nextOffsetRatio = currentRadius / nextDistance;

					if (nextOffsetRatio > 1d)
					{
						nextOffsetRatio = 1d;
					}
				}


				//Calculate the start and end points of the actual, straight line.

				PointD startPoint = new PointD(currentPosition.X + (nextPosition.X - currentPosition.X) * currentOffsetRatio,
					currentPosition.Y + (nextPosition.Y - currentPosition.Y) * currentOffsetRatio);

				PointD endPoint = new PointD(nextPosition.X - (nextPosition.X - currentPosition.X) * currentOffsetRatio,
					nextPosition.Y - (nextPosition.Y - currentPosition.Y) * currentOffsetRatio);


				//Calculate the end point of the rounded corner.
				PointD nextEndPoint = new PointD(nextPosition.X + (doubleNextPosition.X - nextPosition.X) * nextOffsetRatio,
					nextPosition.Y + (doubleNextPosition.Y - nextPosition.Y) * nextOffsetRatio);


				//Add the line.
				generatedPoints.AddRange(GenerateQuadraticBezierCurvePoints(startPoint, endPoint, endPoint));

				//Add the rounded corner.
				generatedPoints.AddRange(GenerateQuadraticBezierCurvePoints(endPoint, nextPosition, nextEndPoint));
			}


			GeneratedPoints = generatedPoints.ToArray();
		}

		/// <summary>
		/// Generate each point in a quadratic Bezier curve given the end points and control point.
		/// </summary>
		/// <param name="p0">The first end point that the curve passes through.</param>
		/// <param name="p1">The control point that the curve does not necessarily pass through.</param>
		/// <param name="p2">The second end point that the curve passes through.</param>
		/// <returns>The List of generated points.</returns>
		protected static List<PointD> GenerateQuadraticBezierCurvePoints(PointD p0, PointD p1, PointD p2)
		{
			List<PointD> resultList = new List<PointD>();


			//Note: this must be low enough for mouse clicks to be properly considered on/off the curve at any given point.
			double tInterval = .025d;

			double oneMinusT;
			double oneMinusTSquared;

			double tSquared;

			double oneMinusTTimesTTimesTwo;

			//t will go from 0d to 1d at the interval of tInterval.
			for (double t = 0d; t < 1d + tInterval; t += tInterval)
			{
				//There are 2 "layers" in a quadratic Bezier curve's calculation. These "layers"
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

				tSquared = t * t;

				oneMinusTTimesTTimesTwo = oneMinusT * t * 2d;

				//Resulting Point = (1 - t) ^ 2 * p0 + 2 * (1 - t) * t * p1 + t ^ 2 * p2
				//This is done for both the X and Y given a value t going from 0 to 1 at a very small interval
				//and given 3 points p0, p1, and p2, where p0 and p2 are end points and p1 is a control point.

				resultList.Add(new PointD(
					oneMinusTSquared * p0.X + oneMinusTTimesTTimesTwo * p1.X + tSquared * p2.X,
					oneMinusTSquared * p0.Y + oneMinusTTimesTTimesTwo * p1.Y + tSquared * p2.Y));
			}


			return resultList;
		}
	}
}
