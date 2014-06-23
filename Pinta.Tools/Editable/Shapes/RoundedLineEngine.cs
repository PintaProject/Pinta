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
		protected RoundedLineEditEngine editEngine;

		/// <summary>
		/// Create a new RoundedLineEngine.
		/// </summary>
		/// <param name="passedEditEngine">The owner EditEngine.</param>
		/// <param name="parentLayer">The parent UserLayer for the re-editable DrawingLayer.</param>
		/// <param name="passedAA">Whether or not antialiasing is enabled.</param>
		public RoundedLineEngine(RoundedLineEditEngine passedEditEngine, UserLayer parentLayer, bool passedAA): base(parentLayer, passedAA, true)
		{
			editEngine = passedEditEngine;
		}

		public override ShapeEngine PartialClone()
		{
			RoundedLineEngine clonedCE = new RoundedLineEngine(editEngine, parentLayer, AntiAliasing);

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


			double radius = editEngine.Radius;

			//TO DO: implement the equivalent for this with a calculated bounding box.
			/*if ((radius > r.Height / 2) || (radius > r.Width / 2))
			{
				radius = Math.Min(r.Height / 2, r.Width / 2);
			}*/

			for (int i = 0; i < ControlPoints.Count; ++i)
			{
				//Calculate each line.

				int nextPoint = i + 1;

				if (nextPoint >= ControlPoints.Count)
				{
					nextPoint = 0;
				}

				PointD currentPosition = ControlPoints[i].Position;
				PointD nextPosition = ControlPoints[nextPoint].Position;

				double distance = Math.Sqrt(Math.Pow(currentPosition.X - nextPosition.X, 2d) + Math.Pow(currentPosition.Y - nextPosition.Y, 2d));

				double offsetRatio;

				if (distance <= 0d)
				{
					offsetRatio = 0d;
				}
				else
				{
					offsetRatio = radius / distance;

					if (offsetRatio > 1d)
					{
						offsetRatio = 1d;
					}
				}

				PointD startPoint = new PointD(currentPosition.X + (nextPosition.X - currentPosition.X) * offsetRatio,
					currentPosition.Y + (nextPosition.Y - currentPosition.Y) * offsetRatio);

				PointD endPoint = new PointD(nextPosition.X - (nextPosition.X - currentPosition.X) * offsetRatio,
					nextPosition.Y - (nextPosition.Y - currentPosition.Y) * offsetRatio);

				//Add each line.
				generatedPoints.AddRange(GenerateQuadraticBezierCurvePoints(startPoint, endPoint, endPoint));





				//Calculate each rounded corner.

				int nextNextPoint = nextPoint + 1;

				if (nextNextPoint >= ControlPoints.Count)
				{
					nextNextPoint = 0;
				}

				PointD nextNextPosition = ControlPoints[nextNextPoint].Position;

				distance = Math.Sqrt(Math.Pow(nextPosition.X - nextNextPosition.X, 2d) + Math.Pow(nextPosition.Y - nextNextPosition.Y, 2d));

				if (distance <= 0d)
				{
					offsetRatio = 0d;
				}
				else
				{
					offsetRatio = radius / distance;

					if (offsetRatio > 1d)
					{
						offsetRatio = 1d;
					}
				}

				PointD nextEndPoint = new PointD(nextPosition.X + (nextNextPosition.X - nextPosition.X) * offsetRatio,
					nextPosition.Y + (nextNextPosition.Y - nextPosition.Y) * offsetRatio);

				//Add each rounded corner.
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
