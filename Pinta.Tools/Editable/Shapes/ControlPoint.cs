// 
// ControlPoint.cs
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

namespace Pinta.Tools
{
	public class ControlPoint
	{
		//Note: not using get/set because this is used in time-critical code that is sped up without it.
		public PointD Position;
		public double Tension;

		/// <summary>
		/// A wrapper class for a PointD with its own tension value.
		/// </summary>
		/// <param name="passedPosition">The position of the PointD on the Canvas.</param>
		/// <param name="passedTension">The tension of the ControlPoint on the shape.</param>
		public ControlPoint(PointD passedPosition, double passedTension)
		{
			Position = passedPosition;
			Tension = passedTension;
		}

		public ControlPoint Clone()
		{
			return new ControlPoint(Position, Tension);
		}
	}
}
