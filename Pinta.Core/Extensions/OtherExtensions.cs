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
		public static bool In<T> (this T enumeration, params T[] values)
		{
			if (enumeration is null)
				return false;

			foreach (var en in values)
				if (enumeration.Equals (en))
					return true;

			return false;
		}

		public static PointI[][] CreatePolygonSet (this BitMask stencil, RectangleD bounds, int translateX, int translateY)
		{
			var polygons = new List<PointI[]> ();

			if (!stencil.IsEmpty) {
				PointI start = bounds.Location ().ToInt ();
				var pts = new List<PointI> ();
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

						if (start.X >= bounds.Right) {
							++start.Y;
							start.X = (int) bounds.X;

							if (start.Y >= bounds.Bottom) {
								break;
							}
						}
					}

					if (!startFound)
						break;

					pts.Clear ();

					PointI last = new (start.X, start.Y + 1);
					PointI curr = new (start.X, start.Y);
					PointI next = curr;
					PointI left = new ();
					PointI right = new ();

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

					PointI[] points = pts.ToArray ();
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
