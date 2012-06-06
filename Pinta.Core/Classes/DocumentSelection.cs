// 
// DocumentSelection.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2010 Andrew Davis
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

				selection_path = value;
			}
		}

		/// <summary>
		/// Make a complete copy of the Selection.
		/// </summary>
		/// <returns>A copy of this Selection (as a DocumentSelection object).</returns>
		public DocumentSelection Clone()
		{
			return (DocumentSelection)MemberwiseClone();
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

		public void CreateEllipseSelection(Surface selectionSurface, Rectangle r)
		{
			using (Context g = new Context(selectionSurface))
			{
				SelectionPath = g.CreateEllipsePath(r);
			}


			SelectionPolygons.Clear();

			//Make this an ellipse, not a rectangle!
			int corner1X = (int)Math.Round(r.X);
			int corner1Y = (int)Math.Round(r.Y);
			int corner2X = (int)Math.Round(r.X + r.Width);
			int corner2Y = (int)Math.Round(r.Y + r.Height);

			List<IntPoint> newPolygon = new List<IntPoint>();
			newPolygon.Add(new IntPoint(corner1X, corner1Y));
			newPolygon.Add(new IntPoint(corner2X, corner1Y));
			newPolygon.Add(new IntPoint(corner2X, corner2Y));
			newPolygon.Add(new IntPoint(corner1X, corner2Y));
			newPolygon.Add(new IntPoint(corner1X, corner1Y));
			SelectionPolygons.Add(newPolygon);
		}

		public void CreateRectangleSelection(Surface selectionSurface, Rectangle r)
		{
			using (Context g = new Context(selectionSurface))
			{
				SelectionPath = g.CreateRectanglePath(r);
			}


			SelectionPolygons.Clear();

			int corner1X = (int)Math.Round(r.X);
			int corner1Y = (int)Math.Round(r.Y);
			int corner2X = (int)Math.Round(r.X + r.Width);
			int corner2Y = (int)Math.Round(r.Y + r.Height);

			List<IntPoint> newPolygon = new List<IntPoint>();
			newPolygon.Add(new IntPoint(corner1X, corner1Y));
			newPolygon.Add(new IntPoint(corner2X, corner1Y));
			newPolygon.Add(new IntPoint(corner2X, corner2Y));
			newPolygon.Add(new IntPoint(corner1X, corner2Y));
			newPolygon.Add(new IntPoint(corner1X, corner1Y));
			SelectionPolygons.Add(newPolygon);
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
	}
}
