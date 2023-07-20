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
			if (stencil.IsEmpty)
				return Array.Empty<PointI[]> ();

			var polygons = new List<PointI[]> ();

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

						if (start.Y >= bounds.Bottom)
							break;
					}
				}

				if (!startFound)
					break;

				pts.Clear ();

				PointI last = new (start.X, start.Y + 1);
				PointI curr = new (start.X, start.Y);
				
				// trace island outline
				while (true) {

					// Calculation zone

					int currLastDiffX = curr.X - last.X;
					int currLastDiffY = curr.Y - last.Y;
					int currXDecreased = curr.X - 1;
					int currYDecreased = curr.Y - 1;

					int leftX = currXDecreased + ((currLastDiffX + currLastDiffY + 2) / 2);
					int leftY = currYDecreased + ((currLastDiffY - currLastDiffX + 2) / 2);

					int rightX = currXDecreased + ((currLastDiffX - currLastDiffY + 2) / 2);
					int rightY = currYDecreased + ((currLastDiffY + currLastDiffX + 2) / 2);

					PointI left = new (leftX, leftY);
					PointI right = new (rightX, rightY);

					int nextXAddition;
					int nextYAddition;
					if (bounds.ContainsPoint (leftX, leftY) && stencil[left]) {
						// go left
						nextXAddition = currLastDiffY;
						nextYAddition = -currLastDiffX;
					} else if (bounds.ContainsPoint (rightX, rightY) && stencil[right]) {
						// go straight
						nextXAddition = currLastDiffX;
						nextYAddition = currLastDiffY;
					} else {
						// turn right
						nextXAddition = -currLastDiffY;
						nextYAddition = currLastDiffX;
					}

					var nextX = curr.X + nextXAddition;
					var nextY = curr.Y + nextYAddition;
					PointI next = new (nextX, nextY);

					// Mutation zone

					if (Math.Sign (nextX - curr.X) != Math.Sign (currLastDiffX) ||
					    Math.Sign (nextY - curr.Y) != Math.Sign (currLastDiffY)) {
						pts.Add (curr);
						++count;
					}

					last = curr;
					curr = next;

					// Continue?

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

			return polygons.ToArray ();
		}
	}
}
