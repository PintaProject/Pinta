// 
// OrganizedPointCollection.cs
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
	public class OrganizedPointCollection
	{
		//Must be an integer.
		public const double SectionSize = 15;

		//Don't change this; it's automatically calculated.
        public static readonly int BorderingSectionRange = (int)Math.Ceiling(BaseEditEngine.ShapeClickStartingRange / SectionSize);

		private Dictionary<int, Dictionary<int, List<OrganizedPoint>>> collection;

		/// <summary>
		/// A collection of points that is organized using spatial hashing to optimize and speed up nearest point detection.
		/// </summary>
		public OrganizedPointCollection()
		{
			collection = new Dictionary<int, Dictionary<int, List<OrganizedPoint>>>();
		}

		/// <summary>
		/// Clone the collection of organized points.
		/// </summary>
		/// <returns>A clone of the organized points.</returns>
		public OrganizedPointCollection Clone()
		{
			OrganizedPointCollection clonedOPC = new OrganizedPointCollection();

			foreach (KeyValuePair<int, Dictionary<int, List<OrganizedPoint>>> xSection in collection)
			{
				//This must be created each time to ensure that it is fresh for each loop iteration.
				Dictionary<int, List<OrganizedPoint>> tempSection = new Dictionary<int, List<OrganizedPoint>>();

				foreach (KeyValuePair<int, List<OrganizedPoint>> section in xSection.Value)
				{
					tempSection.Add(section.Key,
						section.Value.Select(i => new OrganizedPoint(new PointD(i.Position.X, i.Position.Y), i.Index)).ToList());
				}

				clonedOPC.collection.Add(xSection.Key, tempSection);
			}

			return clonedOPC;
		}

		
		/// <summary>
		/// Store the given OrganizedPoint in an organized (spatially hashed) manner.
		/// </summary>
		/// <param name="op">The OrganizedPoint to store.</param>
		public void StoreAndOrganizePoint(OrganizedPoint op)
		{
			int sX = (int)((op.Position.X - op.Position.X % SectionSize) / SectionSize);
			int sY = (int)((op.Position.Y - op.Position.Y % SectionSize) / SectionSize);

			Dictionary<int, List<OrganizedPoint>> xSection;
			List<OrganizedPoint> ySection;

			//Ensure that the xSection for this particular point exists.
			if (!collection.TryGetValue(sX, out xSection))
			{
				//This particular X section does not exist yet; create it.
				xSection = new Dictionary<int, List<OrganizedPoint>>();
				collection.Add(sX, xSection);
			}

			//Ensure that the ySection (which is contained within the respective xSection) for this particular point exists.
			if (!xSection.TryGetValue(sY, out ySection))
			{
				//This particular Y section does not exist yet; create it.
				ySection = new List<OrganizedPoint>();
				xSection.Add(sY, ySection);
			}

			//Now that both the corresponding xSection and ySection for this particular point exist, add the point to the list.
			ySection.Add(op);
		}

		/// <summary>
		/// Clear the collection of organized points.
		/// </summary>
		public void ClearCollection()
		{
			collection.Clear();
		}


		/// <summary>
		/// Efficiently calculate the closest point (to currentPoint) on the shapes.
		/// </summary>
        /// <param name="SEL">The List of ShapeEngines to search through.</param>
		/// <param name="currentPoint">The point to calculate the closest point to.</param>
		/// <param name="closestShapeIndex">The index of the shape with the closest point.</param>
		/// <param name="closestPointIndex">The index of the closest point (in the closest shape).</param>
		/// <param name="closestPoint">The position of the closest point.</param>
		/// <param name="closestDistance">The closest point's distance away from currentPoint.</param>
		public static void FindClosestPoint(
            List<ShapeEngine> SEL, PointD currentPoint,
			out int closestShapeIndex, out int closestPointIndex, out PointD closestPoint, out double closestDistance)
		{
			closestShapeIndex = 0;
			closestPointIndex = 0;
			closestPoint = new PointD(0d, 0d);
			closestDistance = double.MaxValue;

			double currentDistance = double.MaxValue;

			for (int n = 0; n < SEL.Count; ++n)
			{
				Dictionary<int, Dictionary<int, List<OrganizedPoint>>> oP = SEL[n].OrganizedPoints.collection;

				//Calculate the current_point's corresponding *center* section.
				int sX = (int)((currentPoint.X - currentPoint.X % SectionSize) / SectionSize);
				int sY = (int)((currentPoint.Y - currentPoint.Y % SectionSize) / SectionSize);

				int xMin = sX - BorderingSectionRange;
				int xMax = sX + BorderingSectionRange;
				int yMin = sY - BorderingSectionRange;
				int yMax = sY + BorderingSectionRange;

				//Since the mouse and/or shape points can be close to the edge of a section,
				//the points in the surrounding sections must also be checked.
				for (int x = xMin; x <= xMax; ++x)
				{
					//This must be created each time to ensure that it is fresh for each loop iteration.
					Dictionary<int, List<OrganizedPoint>> xSection;

					//If the xSection doesn't exist, move on.
					if (oP.TryGetValue(x, out xSection))
					{
						//Since the mouse and/or shape points can be close to the edge of a section,
						//the points in the surrounding sections must also be checked.
						for (int y = yMin; y <= yMax; ++y)
						{
							List<OrganizedPoint> ySection;

							//If the ySection doesn't exist, move on.
							if (xSection.TryGetValue(y, out ySection))
							{
								foreach (OrganizedPoint p in ySection)
								{
									currentDistance = p.Position.Distance(currentPoint);

									if (currentDistance < closestDistance)
									{
										closestDistance = currentDistance;

										closestPointIndex = p.Index;
										closestShapeIndex = n;

										closestPoint = p.Position;
									}
								}
							}
						} //for each organized row
					}
				} //for each organized column
			} //for each ShapeEngine List
		} //FindClosestPoint
	}

	public class OrganizedPoint
	{
		//Note: not using get/set because this is used in time-critical code that is sped up without it.
		public PointD Position;
		public int Index;

		/// <summary>
		/// A wrapper class for a PointD that knows its index within the generated points of the shape that it's in.
		/// </summary>
		/// <param name="passedPosition">The position of the PointD on the Canvas.</param>
		/// <param name="passedIndex">The index within the generated points of the shape that it's in.</param>
		public OrganizedPoint(PointD passedPosition, int passedIndex)
		{
			Position = passedPosition;
			Index = passedIndex;
		}
	}
}
