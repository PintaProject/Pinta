// 
// RectangleEditEngine.cs
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
    class RectangleEditEngine: BaseEditEngine
    {
        public RectangleEditEngine(BaseTool passedOwner): base(passedOwner)
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
                new ControlPoint(new PointD(startingPoint.X, startingPoint.Y + .01d), DefaultEndPointTension));
            actEngine.ControlPoints.Add(
                new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y + .01d), DefaultEndPointTension));
            actEngine.ControlPoints.Add(
                new ControlPoint(new PointD(startingPoint.X + .01d, startingPoint.Y), DefaultEndPointTension));


            SelectedPointIndex = 2;
            SelectedShapeIndex = SEngines.Count - 1;
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


            //Keep the tension values consistent.

            double movingPointTension = controlPoints[SelectedPointIndex].Tension;
            double previousPointTension = controlPoints[previousPointIndex].Tension;
            double nextPointTension = controlPoints[nextPointIndex].Tension;


            //Update the control points' positions.

            controlPoints.RemoveAt(SelectedPointIndex);
            controlPoints.Insert(SelectedPointIndex, new ControlPoint(new PointD(currentPoint.X, currentPoint.Y), movingPointTension));

            PointD doublePreviousPoint = controlPoints.ElementAt(doublePreviousIndex).Position;
            PointD previousPoint = controlPoints.ElementAt(previousPointIndex).Position;
            PointD nextPoint = controlPoints.ElementAt(nextPointIndex).Position;
            PointD doubleNextPoint = controlPoints.ElementAt(doubleNextIndex).Position;

            controlPoints.RemoveAt(previousPointIndex);
            controlPoints.Insert(previousPointIndex,
                new ControlPoint(new PointD(previousPoint.X == doublePreviousPoint.X ? previousPoint.X : currentPoint.X,
                    previousPoint.Y == doublePreviousPoint.Y ? previousPoint.Y : currentPoint.Y), previousPointTension));

            controlPoints.RemoveAt(nextPointIndex);
            controlPoints.Insert(nextPointIndex,
                new ControlPoint(new PointD(nextPoint.X == doubleNextPoint.X ? nextPoint.X : currentPoint.X,
                    nextPoint.Y == doubleNextPoint.Y ? nextPoint.Y : currentPoint.Y), nextPointTension));
        }
    }
}
