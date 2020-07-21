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
                Add (passedCEC[n].Clone ());
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

		//A collection of calculated GeneratedPoints that make up the entirety of the shape being drawn.
		public GeneratedPoint[] GeneratedPoints = new GeneratedPoint[0];

		//An organized collection of the GeneratedPoints's points for optimized nearest point detection.
		public OrganizedPointCollection OrganizedPoints = new OrganizedPointCollection();

        private UserLayer parent_layer;
		public ReEditableLayer DrawingLayer;

		public bool AntiAliasing;
		public string DashPattern = "-";
		public bool Closed;

		public Color OutlineColor, FillColor;

		public int BrushWidth;

		public BaseEditEngine.ShapeTypes ShapeType;

		/// <summary>
		/// Create a new ShapeEngine.
		/// </summary>
		/// <param name="parent_layer">The parent UserLayer for the ReEditable DrawingLayer.</param>
		/// <param name="drawing_layer">An existing ReEditableLayer to reuse. This is for cloning only. If not cloning, pass in null.</param>
		/// <param name="shape_type">The type of shape to create.</param>
		/// <param name="antialiasing">Whether or not antialiasing is enabled.</param>
		/// <param name="closed">Whether or not the shape is closed (first and last points are connected).</param>
		/// <param name="outline_color">The outline color for the shape.</param>
		/// <param name="fill_color">The fill color for the shape.</param>
		/// <param name="brush_width">The width of the outline of the shape.</param>
        public ShapeEngine (UserLayer parent_layer, ReEditableLayer drawing_layer,
                            BaseEditEngine.ShapeTypes shape_type, bool antialiasing,
                            bool closed, Color outline_color, Color fill_color,
                            int brush_width)
		{
            this.parent_layer = parent_layer;

			if (drawing_layer == null)
                DrawingLayer = new ReEditableLayer (parent_layer);
			else
				DrawingLayer = drawing_layer;

			ShapeType = shape_type;
			AntiAliasing = antialiasing;
			Closed = closed;
			OutlineColor = outline_color.Clone();
			FillColor = fill_color.Clone();
			BrushWidth = brush_width;
		}

        protected ShapeEngine (ShapeEngine src)
        {
            DrawingLayer = src.DrawingLayer;
            ShapeType = src.ShapeType;
            AntiAliasing = src.AntiAliasing;
            Closed = src.Closed;
            OutlineColor = src.OutlineColor.Clone ();
            FillColor = src.FillColor.Clone ();
            BrushWidth = src.BrushWidth;

			// Don't clone the GeneratedPoints or OrganizedPoints, as they will be calculated.
            ControlPoints = src.ControlPoints.Select (i => i.Clone ()).ToList ();
            DashPattern = src.DashPattern;
        }

        public abstract ShapeEngine Clone ();

		/// <summary>
		/// Converts the ShapeEngine instance into a new instance of a different
        /// ShapeEngine (child) type, copying the common data.
		/// </summary>
		/// <param name="newShapeType">The new ShapeEngine type to create.</param>
		/// <param name="shapeIndex">The index to insert the ShapeEngine clone into SEngines at.
		/// This ensures that the clone is as transparent as possible.</param>
		/// <returns>A new ShapeEngine instance of the specified type with the common data copied over.</returns>
        public ShapeEngine Convert (BaseEditEngine.ShapeTypes newShapeType, int shapeIndex)
		{
            //Remove the old ShapeEngine instance.
            BaseEditEngine.SEngines.Remove (this);

            ShapeEngine clone;

            switch (newShapeType)
            {
                case BaseEditEngine.ShapeTypes.ClosedLineCurveSeries:
                    clone = new LineCurveSeriesEngine (parent_layer, DrawingLayer, newShapeType, AntiAliasing, true,
                        OutlineColor, FillColor, BrushWidth);

                    break;
                case BaseEditEngine.ShapeTypes.Ellipse:
                    clone = new EllipseEngine (parent_layer, DrawingLayer, AntiAliasing, OutlineColor, FillColor, BrushWidth);

                    break;
                case BaseEditEngine.ShapeTypes.RoundedLineSeries:
                    clone = new RoundedLineEngine (parent_layer, DrawingLayer, RoundedLineEditEngine.DefaultRadius,
                        AntiAliasing, OutlineColor, FillColor, BrushWidth);

                    break;
                default:
                    //Defaults to OpenLineCurveSeries.
                    clone = new LineCurveSeriesEngine (parent_layer, DrawingLayer, newShapeType, AntiAliasing, false,
                        OutlineColor, FillColor, BrushWidth);

                    break;
            }

            // Don't clone the GeneratedPoints or OrganizedPoints, as they will be calculated.
            clone.ControlPoints = ControlPoints.Select (i => i.Clone ()).ToList ();
            clone.DashPattern = DashPattern;

            // Add the new ShapeEngine instance at the specified index to
            // ensure as transparent of a cloning as possible.
            BaseEditEngine.SEngines.Insert (shapeIndex, clone);

            return clone;
        }

        /// <summary>
        /// Generate the points that make up the entirety of the shape being drawn.
		/// <param name="brush_width">The width of the brush that will be used to draw the shape.</param>
        /// </summary>
        public abstract void GeneratePoints (int brush_width);

        public PointD[] GetActualPoints ()
		{
            int n = GeneratedPoints.Length;
            PointD[] points = new PointD[n];

			for (int i = 0; i < n; ++i)
				points[i] = GeneratedPoints[i].Position;

			return points;
		}
	}
}
