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
    public class RectangleEditEngine: BaseEditEngine
    {
        public RectangleEditEngine(BaseTool passedOwner): base(passedOwner)
        {

        }

        protected override void createShape(bool ctrlKey, bool clickedOnControlPoint, ShapeEngine activeEngine, PointD prevSelPoint)
        {
			addRectanglePoints(ctrlKey, clickedOnControlPoint, activeEngine, prevSelPoint);


            //Set the new shape's DashPattern option to be the same as the previous shape's.
            activeEngine.DashPattern = dashPBox.comboBox.ComboBox.ActiveText;


            base.createShape(ctrlKey, clickedOnControlPoint, activeEngine, prevSelPoint);
        }

        protected override void movePoint(List<ControlPoint> controlPoints)
        {
			moveRectangularPoint(controlPoints);


			base.movePoint(controlPoints);
        }

		protected override void addShape()
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			SEngines.Add(new LineCurveSeriesEngine(doc.CurrentUserLayer, owner.UseAntialiasing, true));

			base.addShape();
		}
    }
}
