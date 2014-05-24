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
using System.Linq;
using System.Collections.Generic;
using Cairo;
using ClipperLibrary;

namespace Pinta.Core
{
	public class DocumentSelection
	{
		private Path selection_path;

		public List<List<IntPoint>> SelectionPolygons = new List<List<IntPoint>>();
		public Clipper SelectionClipper = new Clipper();

		public Path SelectionPath
		{
			get { return selection_path; }
			set
			{
				if (selection_path == value)
					return;

				DisposeSelection ();

				selection_path = value;
			}
		}

		public void Clip (Context g)
		{
			g.AppendPath (selection_path);
			g.FillRule = FillRule.EvenOdd;
			g.Clip ();
		}

		public void Draw (Cairo.Context g, double scale, bool fillSelection)
		{
			g.Save ();
			g.Translate (0.5, 0.5);
			g.Scale (scale, scale);
			
			g.AppendPath (selection_path);
			
			if (fillSelection)
			{
				g.SetSourceColor (new Cairo.Color (0.7, 0.8, 0.9, 0.2));
				g.FillRule = Cairo.FillRule.EvenOdd;
				g.FillPreserve ();
			}
			
			g.LineWidth = 1 / scale;
			
			// Draw a white line first so it shows up on dark backgrounds
			g.SetSourceColor (new Cairo.Color (1, 1, 1));
			g.StrokePreserve ();
			
			// Draw a black dashed line over the white line
			g.SetDash (new double[] { 2 / scale, 4 / scale }, 0);
			g.SetSourceColor (new Cairo.Color (0, 0, 0));
			
			g.Stroke ();
			g.Restore ();
		}

		/// <summary>
		/// Make a complete copy of the Selection.
		/// </summary>
		/// <returns>A copy of this Selection (as a DocumentSelection object).</returns>
		public DocumentSelection Clone()
		{
			DocumentSelection clonedSelection = new DocumentSelection();

			clonedSelection.selection_path = selection_path.Clone();
			clonedSelection.SelectionPolygons = SelectionPolygons.ToList();
			clonedSelection.SelectionClipper = new Clipper();

			return clonedSelection;
		}

		/// <summary>
		/// Convert a Pinta Polygon set (Point[][]) to a Clipper Polygon collection (List[List[IntPoint]]).
		/// </summary>
		/// <param name="pintaPolygonSet">A Pinta Polygon set.</param>
		/// <returns>A Clipper Polygon collection.</returns>
		public static List<List<IntPoint>> ConvertToPolygons(Point[][] pintaPolygonSet)
		{
			List<List<IntPoint>> newPolygons = new List<List<IntPoint>>();

			foreach (Point[] pA in pintaPolygonSet)
			{
				List<IntPoint> newPolygon = new List<IntPoint>();

				foreach (Point p in pA)
				{
					newPolygon.Add(new IntPoint((long)p.X, (long)p.Y));
				}

				newPolygons.Add(newPolygon);
			}

			return newPolygons;
		}

		/// <summary>
		/// Convert a Clipper Polygon collection (List[List[IntPoint]]) to a Pinta Polygon set (Point[][]).
		/// </summary>
		/// <param name="clipperPolygons">A Clipper Polygon collection.</param>
		/// <returns>A Pinta Polygon set.</returns>
		public static Point[][] ConvertToPolygonSet(List<List<IntPoint>> clipperPolygons)
		{
			Point[][] resultingPolygonSet = new Point[clipperPolygons.Count][];

			int polygonNumber = 0;

			foreach (List<IntPoint> ipL in clipperPolygons)
			{
				resultingPolygonSet[polygonNumber] = new Point[ipL.Count];

				int pointNumber = 0;

				foreach (IntPoint ip in ipL)
				{
					resultingPolygonSet[polygonNumber][pointNumber] = new Point((int)ip.X, (int)ip.Y);

					++pointNumber;
				}

				++polygonNumber;
			}

			return resultingPolygonSet;
		}

		public static List<List<IntPoint>> Transform (List<List<IntPoint>> selection, Matrix transform)
		{
			List<List<IntPoint>> newPolygons = new List<List<IntPoint>> ();
			
			foreach (List<IntPoint> ipL in selection) {
				List<IntPoint> newPolygon = new List<IntPoint> ();
				
				foreach (IntPoint ip in ipL) {
					double x = ip.X;
					double y = ip.Y;
					transform.TransformPoint (ref x, ref y);
					newPolygon.Add (new IntPoint ((long)x, (long)y));
				}
				
				newPolygons.Add (newPolygon);
			}

			return newPolygons;
		}

		/// <summary>
		/// Create an elliptical Selection from a bounding Rectangle.
		/// </summary>
		/// <param name="selectionSurface">The selection surface to use for calculating the elliptical Path.</param>
		/// <param name="r">The bounding Rectangle surrounding the ellipse.</param>
		public void CreateEllipseSelection(Surface selectionSurface, Rectangle r)
		{
			using (Context g = new Context(selectionSurface))
			{
				SelectionPath = g.CreateEllipsePath(r);
			}


			//These values were calculated in the static CreateEllipsePath method
			//in Pinta.Core.CairoExtensions, so they were used here as well.
			double rx = r.Width / 2; //1/2 of the bounding Rectangle Width.
			double ry = r.Height / 2; //1/2 of the bounding Rectangle Height.
			double cx = r.X + rx; //The middle of the bounding Rectangle, horizontally speaking.
			double cy = r.Y + ry; //The middle of the bounding Rectangle, vertically speaking.
			double c1 = 0.552285; //A constant factor used to give the least approximation error.

			//Clear the Selection Polygons collection to start from a clean slate.
			SelectionPolygons.Clear();

			//Calculate an appropriate interval at which to increment t based on
			//the bounding Rectangle's Width and Height properties. The increment
			//for t determines how many intermediate Points to calculate for the
			//ellipse. For each curve, t will go from tInterval to 1. The lower
			//the value of tInterval, the higher number of intermediate Points
			//that will be calculated and stored into the Polygon collection.
			double tInterval = 1d / (r.Width + r.Height);

			//Create a new Polygon to store the upcoming ellipse.
			List<IntPoint> newPolygon = new List<IntPoint>();

			//These values were also calculated in the CreateEllipsePath method. This is where
			//the ellipse's 4 curves (and all of the Points on each curve) are determined.
			//Note: each curve is consecutive to the previous one, but they *do not* overlap,
			//other than the first/last Point (which is how it is supposed to work).

			//The starting Point.
			newPolygon.Add(new IntPoint((long)(cx + rx), (long)cy));

			//Curve 1.
			newPolygon.AddRange(CalculateCurvePoints(tInterval,
				cx + rx, cy,
				cx + rx, cy - c1 * ry,
				cx + c1 * rx, cy - ry,
				cx, cy - ry));

			//Curve 2.
			newPolygon.AddRange(CalculateCurvePoints(tInterval,
				cx, cy - ry,
				cx - c1 * rx, cy - ry,
				cx - rx, cy - c1 * ry,
				cx - rx, cy));

			//Curve 3.
			newPolygon.AddRange(CalculateCurvePoints(tInterval,
				cx - rx, cy,
				cx - rx, cy + c1 * ry,
				cx - c1 * rx, cy + ry,
				cx, cy + ry));

			//Curve 4.
			newPolygon.AddRange(CalculateCurvePoints(tInterval,
				cx, cy + ry,
				cx + c1 * rx, cy + ry,
				cx + rx, cy + c1 * ry,
				cx + rx, cy));

			//Add the newly calculated elliptical Polygon.
			SelectionPolygons.Add(newPolygon);
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
		List<IntPoint> CalculateCurvePoints(double tInterval, double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3)
		{
			//For benchmarking purposes.
			/*tInterval = .00001;
			x0 = 50;
			y0 = 50;
			x1 = 150;
			y1 = 155;
			x2 = 200;
			y2 = 208;
			x3 = 506;
			y3 = 632;

			long startTime = DateTime.Now.Ticks;*/



			//Create a new partial Polygon to store the calculated Points.
			List<IntPoint> calculatedPoints = new List<IntPoint>();


			//Old calculation code.
			/*//This is for optimization, since these values will be used repetitively.
			//These 6 values (3 for X and Y each) store the distance between each
			//of the 4 Points of the curve, consecutively.
			double[] firstLayerDistancesX = new double[3];
			double[] firstLayerDistancesY = new double[3];
			firstLayerDistancesX[0] = x1 - x0;
			firstLayerDistancesX[1] = x2 - x1;
			firstLayerDistancesX[2] = x3 - x2;
			firstLayerDistancesY[0] = y1 - y0;
			firstLayerDistancesY[1] = y2 - y1;
			firstLayerDistancesY[2] = y3 - y2;

			//This is also for optimization.
			double[,] intermediatePointsX = new double[2, 3];
			double[,] intermediatePointsY = new double[2, 3];*/



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



				//Old calculation code.
				/*//These Points are in the "first layer".
				intermediatePointsX[0, 0] = firstLayerDistancesX[0] * t + x0;
				intermediatePointsX[0, 1] = firstLayerDistancesX[1] * t + x1;
				intermediatePointsX[0, 2] = firstLayerDistancesX[2] * t + x2;
				intermediatePointsY[0, 0] = firstLayerDistancesY[0] * t + y0;
				intermediatePointsY[0, 1] = firstLayerDistancesY[1] * t + y1;
				intermediatePointsY[0, 2] = firstLayerDistancesY[2] * t + y2;

				//These Points are in the "second layer".
				intermediatePointsX[1, 0] = (intermediatePointsX[0, 1] - intermediatePointsX[0, 0]) * t + intermediatePointsX[0, 0];
				intermediatePointsX[1, 1] = (intermediatePointsX[0, 2] - intermediatePointsX[0, 1]) * t + intermediatePointsX[0, 1];
				intermediatePointsY[1, 0] = (intermediatePointsY[0, 1] - intermediatePointsY[0, 0]) * t + intermediatePointsY[0, 0];
				intermediatePointsY[1, 1] = (intermediatePointsY[0, 2] - intermediatePointsY[0, 1]) * t + intermediatePointsY[0, 1];

				//The "third layer" (the resulting Point that is on the curve) is stored immediately after calculation.
				calculatedPoints.Add(new IntPoint(
					(long)((intermediatePointsX[1, 1] - intermediatePointsX[1, 0]) * t + intermediatePointsX[1, 0]),
					(long)((intermediatePointsY[1, 1] - intermediatePointsY[1, 0]) * t + intermediatePointsY[1, 0])));*/



				oneMinusT = 1d - t;
				oneMinusTSquared = oneMinusT * oneMinusT;
				oneMinusTCubed = oneMinusTSquared * oneMinusT;

				tSquared = t * t;
				tCubed = tSquared * t;

				oneMinusTSquaredTimesTTimesThree = oneMinusTSquared * t * 3d;
				oneMinusTTimesTSquaredTimesThree = oneMinusT * tSquared * 3d;

				calculatedPoints.Add(new IntPoint(
					(long)(oneMinusTCubed * x0 + oneMinusTSquaredTimesTTimesThree * x1 + oneMinusTTimesTSquaredTimesThree * x2 + tCubed * x3),
					(long)(oneMinusTCubed * y0 + oneMinusTSquaredTimesTTimesThree * y1 + oneMinusTTimesTSquaredTimesThree * y2 + tCubed * y3)));
			}



			//For benchmarking purposes.
			//long timeTaken = DateTime.Now.Ticks - startTime;



			//Return the partial Polygon containing the calculated Points in the curve.
			return calculatedPoints;
		}

		/// <summary>
		/// Create a rectangular Selection from a Rectangle.
		/// </summary>
		/// <param name="selectionSurface">The selection surface to use for calculating the rectangular Path.</param>
		/// <param name="r">The Rectangle.</param>
		public void CreateRectangleSelection(Surface selectionSurface, Rectangle r)
		{
			using (Context g = new Context(selectionSurface))
			{
				SelectionPath = g.CreateRectanglePath(r);
			}

			//Clear the Selection Polygons collection to start from a clean slate.
			SelectionPolygons.Clear();

			SelectionPolygons.Add (CreateRectanglePolygon (r));
		}

		/// <summary>
		/// Inverts the selection.
		/// </summary>
		/// <param name="surface">
		/// Surface for the selection path.
		/// </param>
		/// <param name='imageSize'>
		/// The size of the document.
		/// </param>
		public void Invert (Surface surface, Gdk.Size imageSize)
		{
			List<List<IntPoint>> resultingPolygons = new List<List<IntPoint>> ();

			var documentPolygon = CreateRectanglePolygon (new Rectangle (0, 0, imageSize.Width, imageSize.Height));

			// Create a rectangle that is the size of the entire image,
			// and subtract all of the polygons in the current selection from it.
			SelectionClipper.AddPolygon (documentPolygon, PolyType.ptSubject);
			SelectionClipper.AddPolygons (SelectionPolygons, PolyType.ptClip);
			SelectionClipper.Execute (ClipType.ctDifference, resultingPolygons);
			
			SelectionClipper.Clear ();

			SelectionPolygons = resultingPolygons;
			using (Context g = new Context (surface)) {
				SelectionPath = g.CreatePolygonPath (ConvertToPolygonSet (resultingPolygons));
			}
		}

		/// <summary>
		/// Disposes of the old Selection without any intention of reusing it.
		/// </summary>
		public void DisposeSelection()
		{
			if (selection_path != null)
			{
				(selection_path as IDisposable).Dispose();
			}
		}

		/// <summary>
		/// Disposes of the old Selection, but allows for reusability.
		/// </summary>
		public void DisposeSelectionPreserve()
		{
			Path old = SelectionPath;

			SelectionPath = null;

			if (old != null)
			{
				(old as IDisposable).Dispose();
			}
		}

		/// <summary>
		/// Reset (clear) the Selection.
		/// </summary>
		/// <param name="selectionSurface"></param>
		/// <param name="imageSize"></param>
		public void ResetSelection(Surface selectionSurface, Gdk.Size imageSize)
		{
			using (Cairo.Context g = new Cairo.Context(selectionSurface))
			{
				SelectionPath = g.CreateRectanglePath(new Rectangle(0, 0, imageSize.Width, imageSize.Height));
			}

			SelectionPolygons.Clear();
		}

		private List<IntPoint> CreateRectanglePolygon (Rectangle r)
		{
			// The 4 corners of the Rectangle.
			int corner1X = (int)Math.Round(r.X);
			int corner1Y = (int)Math.Round(r.Y);
			int corner2X = (int)Math.Round(r.X + r.Width);
			int corner2Y = (int)Math.Round(r.Y + r.Height);
			
			// Store each of the 4 corners of the Rectangle in the Polygon, and then store
			// the first corner again. It is important to note that the order of the
			// corners being added (clockwise) and the first/last Point being the same
			// should be kept this way; otherwise, problems could result.
			List<IntPoint> newPolygon = new List<IntPoint>() {
				new IntPoint(corner1X, corner1Y),
				new IntPoint(corner2X, corner1Y),
				new IntPoint(corner2X, corner2Y),
				new IntPoint(corner1X, corner2Y),
				new IntPoint(corner1X, corner1Y)
			};

			return newPolygon;
		}
	}
}
