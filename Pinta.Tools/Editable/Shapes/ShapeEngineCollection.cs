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

		/// <summary>
		/// Create a new ShapeEngine.
		/// </summary>
		/// <param name="passedParentLayer">The parent UserLayer for the re-editable DrawingLayer.</param>
		/// <param name="passedAA">Whether or not antialiasing is enabled.</param>
		public ShapeEngine(UserLayer passedParentLayer, bool passedAA)
		{
			parentLayer = passedParentLayer;
			DrawingLayer = new ReEditableLayer(parentLayer);

			AntiAliasing = passedAA;
		}

		/// <summary>
		/// Clone all of the necessary data in the ShapeEngine.
		/// </summary>
		/// <returns>The partially cloned shape data.</returns>
		public abstract ShapeEngine PartialClone();
		//Overrides should implement at least:
		/*{
			ShapeEngine clonedCE = new ShapeEngine(AntiAliasing);

			clonedCE.ControlPoints = ControlPoints.Select(i => i.Clone()).ToList();

			//Don't clone the GeneratedPoints or OrganizedPoints, as they will be calculated.

			clonedCE.DashPattern = DashPattern;

			return clonedCE;
		}*/

        /// <summary>
        /// Generate the points that make up the entirety of the shape being drawn.
        /// </summary>
        public abstract void GeneratePoints();


		protected UserLayer parentLayer;
	}
}
