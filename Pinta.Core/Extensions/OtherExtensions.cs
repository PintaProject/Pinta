/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See license-pdn.txt for full licensing and attribution details.             //
//                                                                             //
// Ported to Pinta by: Olivier Dufour <olivier.duff@gmail.com>                 //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using Cairo;

namespace Pinta.Core
{
	public static class OtherExtensions
	{
		public unsafe static Point[][] CreatePolygonSet (this IBitVector2D stencil, Rectangle bounds, int translateX, int translateY)
		{
			List<Point[]> polygons = new List<Point[]> ();

			if (!stencil.IsEmpty) {
				Point start = bounds.Location ();
				List<Point> pts = new List<Point> ();
				int count = 0;

				// find all islands
				while (true) {
					bool startFound = false;

					while (true) {
						if (stencil[start]) {
							startFound = true;
							break;
						}

						++start.X;

						if (start.X >= bounds.GetRight ()) {
							++start.Y;
							start.X = (int)bounds.X;

							if (start.Y >= bounds.GetBottom ()) {
								break;
							}
						}
					}

					if (!startFound)
						break;

					pts.Clear ();
					
					Point last = new Point (start.X, start.Y + 1);
					Point curr = new Point (start.X, start.Y);
					Point next = curr;
					Point left = new Point ();
					Point right = new Point ();

					// trace island outline
					while (true) {
						left.X = ((curr.X - last.X) + (curr.Y - last.Y) + 2) / 2 + curr.X - 1;
						left.Y = ((curr.Y - last.Y) - (curr.X - last.X) + 2) / 2 + curr.Y - 1;

						right.X = ((curr.X - last.X) - (curr.Y - last.Y) + 2) / 2 + curr.X - 1;
						right.Y = ((curr.Y - last.Y) + (curr.X - last.X) + 2) / 2 + curr.Y - 1;

						if (bounds.ContainsPoint (left.X, left.Y) && stencil[left]) {
							// go left
							next.X += curr.Y - last.Y;
							next.Y -= curr.X - last.X;
						} else if (bounds.ContainsPoint (right.X, right.Y) && stencil[right]) {
							// go straight
							next.X += curr.X - last.X;
							next.Y += curr.Y - last.Y;
						} else {
							// turn right
							next.X -= curr.Y - last.Y;
							next.Y += curr.X - last.X;
						}

						if (Math.Sign (next.X - curr.X) != Math.Sign (curr.X - last.X) ||
						    Math.Sign (next.Y - curr.Y) != Math.Sign (curr.Y - last.Y)) {
							pts.Add (curr);
							++count;
						}

						last = curr;
						curr = next;

						if (next.X == start.X && next.Y == start.Y)
							break;
					}

					Point[] points = pts.ToArray ();
					Scanline[] scans = points.GetScans ();

					foreach (Scanline scan in scans)
						stencil.Invert (scan);

					points.TranslatePointsInPlace (translateX, translateY);
					polygons.Add (points);
				}
			}

			return polygons.ToArray ();
		}
	}
}
