// 
// Arrow.cs
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
using Pinta.Core;

namespace Pinta.Tools
{
	public class Arrow
	{
		public bool Show = false;
		public double ArrowSize = 10d, AngleOffset = 15d, LengthOffset = 10d;

		private const double radiansToDegrees = Math.PI / 180d;
		private const double invRadiansToDegrees = 180d / Math.PI;
		
		/// <summary>
		/// Returns a clone of the Arrow.
		/// </summary>
		/// <returns>A clone of the Arrow.</returns>
		public Arrow Clone()
		{
			Arrow clonedA = new Arrow();

			clonedA.Show = Show;
			clonedA.ArrowSize = ArrowSize;
			clonedA.AngleOffset = AngleOffset;
			clonedA.LengthOffset = LengthOffset;

			return clonedA;
		}

		/// <summary>
		/// Draws the arrow.
		/// </summary>
		/// <param name="g">The drawing context.</param>
		/// <param name="endPoint">The end point of a shape.</param>
		/// <param name="almostEndPoint">The point right before the end point.</param>
		public Rectangle? Draw(Context g, Color outlineColor, PointD endPoint, PointD almostEndPoint)
		{
			//First, calculate the ending angle.
			double endingAngle = Math.Atan(Math.Abs(endPoint.Y - almostEndPoint.Y) / Math.Abs(endPoint.X - almostEndPoint.X)) * invRadiansToDegrees;

			//This is necessary to have a properly calculated ending angle.
			if (endPoint.Y - almostEndPoint.Y > 0)
			{
				if (endPoint.X - almostEndPoint.X > 0)
				{
					endingAngle = 180d - endingAngle;
				}
			}
			else
			{
				if (endPoint.X - almostEndPoint.X > 0)
				{
					endingAngle += 180d;
				}
				else
				{
					endingAngle = 360d - endingAngle;
				}
			}

			//Calculate the points of the arrow.
			PointD[] arrowPoints =
							{
								endPoint,
								new PointD(
									endPoint.X + Math.Cos((endingAngle + 270 + AngleOffset) * radiansToDegrees) * ArrowSize,
									endPoint.Y + Math.Sin((endingAngle + 270 + AngleOffset) * radiansToDegrees) * ArrowSize * -1d),
								new PointD(
									endPoint.X + Math.Cos((endingAngle + 180) * radiansToDegrees) * (ArrowSize + LengthOffset),
									endPoint.Y + Math.Sin((endingAngle + 180) * radiansToDegrees) * (ArrowSize + LengthOffset) * -1d),
								new PointD(
									endPoint.X + Math.Cos((endingAngle + 90 - AngleOffset) * radiansToDegrees) * ArrowSize,
									endPoint.Y + Math.Sin((endingAngle + 90 - AngleOffset) * radiansToDegrees) * ArrowSize * -1d)
							};

			//Draw the arrow.
			g.FillPolygonal(arrowPoints, outlineColor);


            //Calculate the minimum bounding rectangle for the arrowhead and return it so
            //that it can be unioned with the existing invalidation rectangle.

			double minX = Math.Min(Math.Min(arrowPoints[1].X, arrowPoints[2].X), arrowPoints[3].X);
			double minY = Math.Min(Math.Min(arrowPoints[1].Y, arrowPoints[2].Y), arrowPoints[3].Y);

			return new Rectangle(minX, minY,
				Math.Max(Math.Max(arrowPoints[1].X, arrowPoints[2].X), arrowPoints[3].X) - minX,
				Math.Max(Math.Max(arrowPoints[1].Y, arrowPoints[2].Y), arrowPoints[3].Y) - minY);
		}
	}
}
