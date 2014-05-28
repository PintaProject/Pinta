// 
// UserLayer.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Pinta.Tools
{
    public class EditEngine
    {
        public static readonly Color HoverColor =
            new Color(ToolControl.FillColor.R / 2d, ToolControl.FillColor.G / 2d, ToolControl.FillColor.B / 2d, ToolControl.FillColor.A * 2d / 3d);

        public const double CurveClickStartingRange = 10d;
        public const double CurveClickThicknessFactor = 1d;
        public const double DefaultEndPointTension = 0d;
        public const double DefaultMidPointTension = 1d / 3d;


        public int SelectedPointIndex = -1;
        public int SelectedPointCurveIndex = 0;

        /// <summary>
        /// The selected ControlPoint.
        /// </summary>
        public ControlPoint SelectedPoint
        {
            get
            {
                CurveEngine selEngine = SelectedCurveEngine;

                if (selEngine != null)
                {
                    return selEngine.ControlPoints[SelectedPointIndex];
                }
                else
                {
                    return null;
                }
            }

            set
            {
                CurveEngine selEngine = SelectedCurveEngine;

                if (selEngine != null)
                {
                    selEngine.ControlPoints[SelectedPointIndex] = value;
                }
            }
        }

        /// <summary>
        /// The active curve's CurveEngine.
        /// </summary>
        public CurveEngine ActiveCurveEngine
        {
            get
            {
                if (CEngines.Count > SelectedPointCurveIndex)
                {
                    return CEngines[SelectedPointCurveIndex];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// The selected curve's CurveEngine. This can be null.
        /// </summary>
        public CurveEngine SelectedCurveEngine
        {
            get
            {
                if (SelectedPointIndex > -1)
                {
                    return ActiveCurveEngine;
                }
                else
                {
                    return null;
                }
            }
        }

        public PointD HoverPoint = new PointD(-1d, -1d);
        public int HoveredPointAsControlPoint = -1;

        public bool ChangingTension = false;
        public PointD LastMousePosition = new PointD(0d, 0d);


        //Helps to keep track of the first modification on a curve after the mouse is clicked, to prevent unnecessary history items.
        public bool ClickedWithoutModifying = false;


        //Stores the editable curve data.
        public CurveEngineCollection CEngines = new CurveEngineCollection(false);
    }
}
