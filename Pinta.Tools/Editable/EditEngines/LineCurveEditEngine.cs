// 
// LineCurveEditEngine.cs
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
    public class LineCurveEditEngine: ArrowedEditEngine
	{
        public LineCurveEditEngine(BaseTool passedOwner): base(passedOwner)
        {

        }

        protected override void CreateShape(bool ctrlKey, bool clickedOnControlPoint, ShapeEngine actEngine, PointD prevSelPoint)
        {
            PointD startingPoint;

            //Then create the initial points of the shape. The second point will follow the mouse around until released.
            if (ctrlKey && clickedOnControlPoint)
            {
                startingPoint = prevSelPoint;

                ClickedWithoutModifying = false;
            }
            else
            {
                startingPoint = shapeOrigin;
            }

            actEngine.ControlPoints.Add(new ControlPoint(new PointD(startingPoint.X, startingPoint.Y), DefaultEndPointTension));
            actEngine.ControlPoints.Add(
                new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y + .01d), DefaultEndPointTension));


            SelectedPointIndex = 1;
            SelectedShapeIndex = SEngines.Count - 1;


            //Set the new shape's DashPattern to be the same as the previous shape's.
            actEngine.DashPattern = dashPBox.comboBox.ComboBox.ActiveText;


            base.CreateShape(ctrlKey, clickedOnControlPoint, actEngine, prevSelPoint);
        }

        protected override void MovePoint(List<ControlPoint> controlPoints)
        {
            //Update the control point's position.
			controlPoints.ElementAt(SelectedPointIndex).Position = new PointD(currentPoint.X, currentPoint.Y);
        }

		protected override void AddShape()
		{
			SEngines.Add(new LineCurveSeriesEngine(owner.UseAntialiasing));
		}
    }
}
