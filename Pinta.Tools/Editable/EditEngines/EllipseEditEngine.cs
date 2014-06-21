// 
// EllipseEditEngine.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2014 Andrew Davis, GSoC 2014
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
using Cairo;
using Pinta.Core;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Unix;

namespace Pinta.Tools
{
    public class EllipseEditEngine: BaseEditEngine
    {
		public EllipseEditEngine(BaseTool passedOwner): base(passedOwner)
        {

        }

		protected override void CreateShape(bool ctrlKey, bool clickedOnControlPoint, ShapeEngine actEngine, PointD prevSelPoint)
		{
			PointD startingPoint;

			//Create the initial points of the shape. The second point will follow the mouse around until released.
			if (ctrlKey && clickedOnControlPoint)
			{
				startingPoint = prevSelPoint;

				ClickedWithoutModifying = false;
			}
			else
			{
				startingPoint = shapeOrigin;
			}


			actEngine.ControlPoints.Add(new ControlPoint(new PointD(startingPoint.X, startingPoint.Y), 0.0));
			actEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X, startingPoint.Y + .01d), 0.0));
			actEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y + .01d), 0.0));
			actEngine.ControlPoints.Add(
				new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y), 0.0));


			SelectedPointIndex = 2;
			SelectedShapeIndex = SEngines.Count - 1;


			//Set the new shape's DashPattern option to be the same as the previous shape's.
			actEngine.DashPattern = dashPBox.comboBox.ComboBox.ActiveText;


			base.CreateShape(ctrlKey, clickedOnControlPoint, actEngine, prevSelPoint);
		}

		protected override void MovePoint(List<ControlPoint> controlPoints)
		{
			//NOTE: doubleNext and doublePrevious may not be the same if there are not 4 control points!

			//Figure out the indeces of the surrounding points.

			int doublePreviousIndex = SelectedPointIndex - 2;
			int previousPointIndex = SelectedPointIndex - 1;
			int nextPointIndex = SelectedPointIndex + 1;
			int doubleNextIndex = SelectedPointIndex + 2;

			if (previousPointIndex < 0)
			{
				previousPointIndex = controlPoints.Count - 1;
				doublePreviousIndex = controlPoints.Count - 2;

				if (doublePreviousIndex < 0)
				{
					doublePreviousIndex = 0;
				}
			}
			else if (doublePreviousIndex < 0)
			{
				doublePreviousIndex = controlPoints.Count - 1;
			}

			if (nextPointIndex >= controlPoints.Count)
			{
				nextPointIndex = 0;
				doubleNextIndex = 1;

				if (doubleNextIndex >= controlPoints.Count)
				{
					doubleNextIndex = 0;
				}
			}
			else if (doubleNextIndex >= controlPoints.Count)
			{
				doubleNextIndex = 0;
			}


			//Update the control points' positions.


			//Update the selected control point's position.
			controlPoints.ElementAt(SelectedPointIndex).Position = new PointD(currentPoint.X, currentPoint.Y);


			//Determine the positions of each of the surrounding points.
			PointD doublePreviousPoint = controlPoints.ElementAt(doublePreviousIndex).Position;
			PointD previousPoint = controlPoints.ElementAt(previousPointIndex).Position;
			PointD nextPoint = controlPoints.ElementAt(nextPointIndex).Position;
			PointD doubleNextPoint = controlPoints.ElementAt(doubleNextIndex).Position;

			//Ensure that only one direction is moved in at a time, if either.
			bool moveVertical = (previousPoint.X == doublePreviousPoint.X);
			bool moveHorizontal = moveVertical ? false : (previousPoint.Y == doublePreviousPoint.Y);

			//Update the previous control point's position.
			controlPoints.ElementAt(previousPointIndex).Position = new PointD(
				moveHorizontal ? currentPoint.X : previousPoint.X, moveVertical ? currentPoint.Y : previousPoint.Y);

			//Ensure that only one direction is moved in at a time, if either.
			moveVertical = (nextPoint.X == doubleNextPoint.X);
			moveHorizontal = moveVertical ? false : (nextPoint.Y == doubleNextPoint.Y);

			//Update the next control point's position.
			controlPoints.ElementAt(nextPointIndex).Position = new PointD(
				moveHorizontal ? currentPoint.X : nextPoint.X, moveVertical ? currentPoint.Y : nextPoint.Y);
		}

		protected override void AddShape()
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			SEngines.Add(new EllipseEngine(doc.CurrentUserLayer, owner.UseAntialiasing));

			base.AddShape();
		}
    }
}
