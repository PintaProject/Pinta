// 
// ShapeEngineCollection.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2013 Andrew Davis, GSoC 2013 & 2014
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
	public class ShapeEngineCollection: List<ShapeEngine>
	{
		/// <summary>
		/// A partially cloneable ShapeEngine collection.
		/// </summary>
		public ShapeEngineCollection()
		{
		}

		/// <summary>
		/// A partially cloneable ShapeEngine collection. This constructor creates a partial clone of an existing ShapeEngineCollection.
		/// </summary>
		/// <param name="passedCEC">An existing ShapeEngineCollection to partially clone.</param>
		public ShapeEngineCollection(ShapeEngineCollection passedCEC)
		{
			for (int n = 0; n < passedCEC.Count; ++n)
			{
				Add(passedCEC[n].PartialClone());
			}
		}
        
		/// <summary>
		/// Clone the necessary data in each of the ShapeEngines in the collection.
		/// </summary>
		/// <returns>The partially cloned ShapeEngineCollection.</returns>
		public ShapeEngineCollection PartialClone()
		{
			return new ShapeEngineCollection(this);
		}


		/// <summary>
		/// Calculate the closest ControlPoint to currentPoint.
		/// </summary>
		/// <param name="currentPoint">The point to calculate the closest ControlPoint to.</param>
		/// <param name="closestCPShapeIndex">The index of the shape with the closest ControlPoint.</param>
		/// <param name="closestCPIndex">The index of the closest ControlPoint.</param>
		/// <param name="closestControlPoint">The closest ControlPoint to currentPoint.</param>
		/// <param name="closestCPDistance">The closest ControlPoint's distance from currentPoint.</param>
		public void FindClosestControlPoint(PointD currentPoint,
			out int closestCPShapeIndex, out int closestCPIndex, out ControlPoint closestControlPoint, out double closestCPDistance)
		{
			closestCPShapeIndex = 0;
			closestCPIndex = 0;
			closestControlPoint = null;
			closestCPDistance = double.MaxValue;

			double currentDistance;

			for (int shapeIndex = 0; shapeIndex < Count; ++shapeIndex)
			{
				List<ControlPoint> controlPoints = this[shapeIndex].ControlPoints;

				for (int cPIndex = 0; cPIndex < controlPoints.Count; ++cPIndex)
				{
					currentDistance = controlPoints[cPIndex].Position.Distance(currentPoint);

					if (currentDistance < closestCPDistance)
					{
						closestCPShapeIndex = shapeIndex;
						closestCPIndex = cPIndex;
						closestControlPoint = controlPoints[cPIndex];
						closestCPDistance = currentDistance;
					}
				}
			}
		}
	}

	public abstract class ShapeEngine
	{
		//A collection of the original ControlPoints that the shape is based on and that the user interacts with.
		public List<ControlPoint> ControlPoints = new List<ControlPoint>();

		//A collection of calculated PointD's that make up the entirety of the shape being drawn.
		public PointD[] GeneratedPoints = new PointD[0];

		//An organized collection of the GeneratedPoints's points for optimized nearest point detection.
		public OrganizedPointCollection OrganizedPoints = new OrganizedPointCollection();

		public ReEditableLayer DrawingLayer;

		public bool AntiAliasing;
		public string DashPattern = "-";
		public bool Closed;

		public Color OutlineColor, FillColor;

		public BaseEditEngine.ShapeTypes ShapeType;

		/// <summary>
		/// Create a new ShapeEngine.
		/// </summary>
		/// <param name="passedParentLayer">The parent UserLayer for the ReEditable DrawingLayer.</param>
		/// <param name="passedDrawingLayer">An existing ReEditableLayer to reuse. This is for cloning only. If not cloning, pass in null.</param>
		/// <param name="passedShapeType">The type of shape to create.</param>
		/// <param name="passedAA">Whether or not antialiasing is enabled.</param>
		/// <param name="passedClosed">Whether or not the shape is closed (first and last points are connected).</param>
		/// <param name="passedOutlineColor">The outline color for the shape.</param>
		/// <param name="passedFillColor">The fill color for the shape.</param>
		public ShapeEngine(UserLayer passedParentLayer, ReEditableLayer passedDrawingLayer,
			BaseEditEngine.ShapeTypes passedShapeType, bool passedAA, bool passedClosed, Color passedOutlineColor, Color passedFillColor)
		{
			parentLayer = passedParentLayer;

			if (passedDrawingLayer == null)
			{
				DrawingLayer = new ReEditableLayer(parentLayer);
			}
			else
			{
				DrawingLayer = passedDrawingLayer;
			}

			ShapeType = passedShapeType;
			AntiAliasing = passedAA;
			Closed = passedClosed;
			OutlineColor = passedOutlineColor;
			FillColor = passedFillColor;
		}

		/// <summary>
		/// Create a ShapeEngine clone with all of the common and added extra data (that depends on the ShapeEngine child type).
		/// </summary>
		/// <returns>The partially cloned shape data.</returns>
		public ShapeEngine PartialClone()
		{
			//The actual type of ShapeEngine child created for the clone must be done in an overridden method.
			ShapeEngine clonedCE = cloneSpecific();

			clonedCE.ControlPoints = ControlPoints.Select(i => i.Clone()).ToList();

			//Don't clone the GeneratedPoints or OrganizedPoints, as they will be calculated.

			clonedCE.DashPattern = DashPattern;

			return clonedCE;
		}

		/// <summary>
		/// Create a ShapeEngine clone with added extra data (besides the common data) to the clone that depends on the ShapeEngine child type.
		/// Note: do not add the common data when overriding this method! That is done in PartialClone.
		/// </summary>
		/// <returns></returns>
		protected abstract ShapeEngine cloneSpecific();

		/// <summary>
		/// Converts the ShapeEngine instance into a new instance of a different ShapeEngine (child) type, copying the common data.
		/// </summary>
		/// <param name="newShapeType">The new ShapeEngine type to create.</param>
		/// <param name="shapeIndex">The index to insert the ShapeEngine clone into SEngines at.
		/// This ensures that the clone is as transparent as possible.</param>
		/// <returns>A new ShapeEngine instance of the specified type with the common data copied over.</returns>
		public ShapeEngine GenericClone(BaseEditEngine.ShapeTypes newShapeType, int shapeIndex)
		{
			//Remove the old ShapeEngine instance.
			BaseEditEngine.SEngines.Remove(this);

			ShapeEngine clonedEngine;

			switch (newShapeType)
			{
				case BaseEditEngine.ShapeTypes.ClosedLineCurveSeries:
					clonedEngine = new LineCurveSeriesEngine(parentLayer, DrawingLayer, newShapeType, AntiAliasing, true, OutlineColor, FillColor);
					break;
				case BaseEditEngine.ShapeTypes.Ellipse:
					clonedEngine = new EllipseEngine(parentLayer, DrawingLayer, AntiAliasing, OutlineColor, FillColor);
					break;
				case BaseEditEngine.ShapeTypes.RoundedLineSeries:
					clonedEngine = new RoundedLineEngine(parentLayer, DrawingLayer, RoundedLineEditEngine.DefaultRadius,
						AntiAliasing, OutlineColor, FillColor);
					break;
				default:
					//Defaults to OpenLineCurveSeries.
					clonedEngine = new LineCurveSeriesEngine(parentLayer, DrawingLayer, newShapeType, AntiAliasing, false, OutlineColor, FillColor);

					break;
			}

			clonedEngine.ControlPoints = ControlPoints.Select(i => i.Clone()).ToList();

			//Don't clone the GeneratedPoints or OrganizedPoints, as they will be calculated.
			
			clonedEngine.DashPattern = DashPattern;

			//Add the new ShapeEngine instance at the specified index to ensure as transparent of a cloning as possible.
			BaseEditEngine.SEngines.Insert(shapeIndex, clonedEngine);

			return clonedEngine;
		}

        /// <summary>
        /// Generate the points that make up the entirety of the shape being drawn.
        /// </summary>
        public abstract void GeneratePoints();


		protected UserLayer parentLayer;
	}
}
