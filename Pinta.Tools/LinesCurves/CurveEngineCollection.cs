// 
// CurveEngineCollection.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2013 Andrew Davis, GSoC 2013
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
	public class CurveEngineCollection
	{
		//A List of CurveEngines.
		public List<CurveEngine> CEL = new List<CurveEngine>();

		/// <summary>
		/// A partially cloneable CurveEngine collection.
		/// </summary>
		/// <param name="passedAA">Whether or not antialiasing is enabled.</param>
		public CurveEngineCollection(bool passedAA)
		{
			CEL.Add(new CurveEngine(passedAA));
		}

		/// <summary>
		/// A partially cloneable CurveEngine collection. This constructor creates a partial clone of an existing CurveEngineCollection.
		/// </summary>
		/// <param name="passedCEC">An existing CurveEngineCollection to partially clone.</param>
		public CurveEngineCollection(CurveEngineCollection passedCEC)
		{
			for (int n = 0; n < passedCEC.CEL.Count; ++n)
			{
				CEL.Add(passedCEC.CEL[n].PartialClone());
			}
		}

		/// <summary>
		/// Clone the necessary data in each of the CurveEngines in the collection.
		/// </summary>
		/// <returns>The partially cloned CurveEngineCollection.</returns>
		public CurveEngineCollection PartialClone()
		{
			return new CurveEngineCollection(this);
		}
	}

	public class CurveEngine
	{
		//A collection of the original ControlPoints that the curve is based on and that the user interacts with.
		public List<ControlPoint> ControlPoints = new List<ControlPoint>();

		//A collection of calculated PointD's that make up the entirety of the curve being drawn.
		public PointD[] GeneratedPoints = new PointD[0];

		//An organized collection of the GeneratedPoints's points for optimized nearest point detection.
		public OrganizedPointCollection OrganizedPoints = new OrganizedPointCollection();

		public Arrow Arrow1 = new Arrow(), Arrow2 = new Arrow();

		public bool AntiAliasing;

		public string DashPattern = "-";

		/// <summary>
		/// Create a new CurveEngine.
		/// </summary>
		/// <param name="passedAA">Whether or not antialiasing is enabled.</param>
		public CurveEngine(bool passedAA)
		{
			AntiAliasing = passedAA;
		}

		/// <summary>
		/// Clone all of the necessary data in the CurveEngine.
		/// </summary>
		/// <returns>The partially cloned curve data.</returns>
		public CurveEngine PartialClone()
		{
			CurveEngine clonedCE = new CurveEngine(AntiAliasing);

			clonedCE.ControlPoints = ControlPoints.Select(i => i.Clone()).ToList();

			//Don't clone the GeneratedPoints or OrganizedPoints, as they will be calculated.

			clonedCE.Arrow1 = Arrow1.Clone();
			clonedCE.Arrow2 = Arrow2.Clone();

			clonedCE.DashPattern = DashPattern;

			return clonedCE;
		}


		/// <summary>
		/// Generate each point in a cardinal spline polynomial curve that passes through
		/// the given control points and store the result in GeneratedPoints.
		/// </summary>
		/// <param name="curveNum">The number of the curve to generate the points for.</param>
		public void GenerateCardinalSplinePolynomialCurvePoints(int curveNum)
		{
			List<ControlPoint> controlPoints = LineCurveTool.cEngines.CEL[curveNum].ControlPoints;


			List<PointD> generatedPoints = new List<PointD>();

			//Note: it's important that there be many generated points even if there are only 2 given points and it's just a line.
			//This is because the generated points are used in the check that determines if the mouse clicks on the line/curve.
			if (controlPoints.Count < 2)
			{
				foreach (ControlPoint cP in controlPoints)
				{
					generatedPoints.Add(cP.Position);
				}
			}
			else
			{
				//Generate tangents for each of the smaller cubic Bezier curves that make up each segment of the resulting curve.

				//The tension calculated for each point is a gradient between the previous
				//control point's tension and the following control point's tension.

				//Stores all of the tangent values.
				List<PointD> bezierTangents = new List<PointD>();

				//Calculate the first tangent.
				bezierTangents.Add(new PointD(
					controlPoints[0].Tension * (controlPoints[1].Position.X - controlPoints[0].Position.X),
					controlPoints[0].Tension * (controlPoints[1].Position.Y - controlPoints[0].Position.Y)));

				int pointCount = controlPoints.Count - 1;
				double pointCountDouble = (double)pointCount;
				double tensionForPoint;

				//Calculate all of the middle tangents.
				for (int i = 1; i < pointCount; ++i)
				{
					tensionForPoint = controlPoints[i].Tension * (double)i / pointCountDouble;

					bezierTangents.Add(new PointD(
						tensionForPoint *
							(controlPoints[i + 1].Position.X - controlPoints[i - 1].Position.X),
						tensionForPoint *
							(controlPoints[i + 1].Position.Y - controlPoints[i - 1].Position.Y)));
				}

				//Calculate the last tangent.
				bezierTangents.Add(new PointD(
					controlPoints[pointCount].Tension *
						(controlPoints[pointCount].Position.X - controlPoints[pointCount - 1].Position.X),
					controlPoints[pointCount].Tension *
						(controlPoints[pointCount].Position.Y - controlPoints[pointCount - 1].Position.Y)));



				//For optimization.
				int iMinusOne;

				//Generate the resulting curve's points with consecutive cubic Bezier curves that
				//use the given points as end points and the calculated tangents as control points.
				for (int i = 1; i < controlPoints.Count; ++i)
				{
					iMinusOne = i - 1;

					GenerateCubicBezierCurvePoints(
						generatedPoints,
						controlPoints[iMinusOne].Position,
						new PointD(
							controlPoints[iMinusOne].Position.X + bezierTangents[iMinusOne].X,
							controlPoints[iMinusOne].Position.Y + bezierTangents[iMinusOne].Y),
						new PointD(
							controlPoints[i].Position.X - bezierTangents[i].X,
							controlPoints[i].Position.Y - bezierTangents[i].Y),
						controlPoints[i].Position);
				}
			}

			GeneratedPoints = generatedPoints.ToArray();
		}

		/// <summary>
		/// Generate each point in a cubic Bezier curve given the end points and control points.
		/// </summary>
		/// <param name="resultList">The resulting List of PointD's to add the generated points to.</param>
		/// <param name="p0">The first end point that the curve passes through.</param>
		/// <param name="p1">The first control point that the curve does not necessarily pass through.</param>
		/// <param name="p2">The second control point that the curve does not necessarily pass through.</param>
		/// <param name="p3">The second end point that the curve passes through.</param>
		/// <returns></returns>
		private static void GenerateCubicBezierCurvePoints(List<PointD> resultList, PointD p0, PointD p1, PointD p2, PointD p3)
		{
			//Note: this must be low enough for mouse clicks to be properly considered on/off the line/curve at any given point.
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

				resultList.Add(new PointD(
					oneMinusTCubed * p0.X + oneMinusTSquaredTimesTTimesThree * p1.X + oneMinusTTimesTSquaredTimesThree * p2.X + tCubed * p3.X,
					oneMinusTCubed * p0.Y + oneMinusTSquaredTimesTTimesThree * p1.Y + oneMinusTTimesTSquaredTimesThree * p2.Y + tCubed * p3.Y));
			}
		}
	}
}
