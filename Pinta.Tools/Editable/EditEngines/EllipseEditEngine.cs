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

		protected override ShapeEngine createShape(bool ctrlKey, bool clickedOnControlPoint, PointD prevSelPoint)
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			ShapeEngine newEngine = new EllipseEngine(doc.CurrentUserLayer, null, owner.UseAntialiasing,
				BaseEditEngine.OutlineColor, BaseEditEngine.FillColor);

			addRectanglePoints(ctrlKey, clickedOnControlPoint, newEngine, prevSelPoint);

			//Set the new shape's DashPattern option to be the same as the previous shape's.
			newEngine.DashPattern = dashPBox.comboBox.ComboBox.ActiveText;

			return newEngine;
		}

		protected override void movePoint(List<ControlPoint> controlPoints)
		{
			moveRectangularPoint(controlPoints);

			base.movePoint(controlPoints);
		}
    }
}
