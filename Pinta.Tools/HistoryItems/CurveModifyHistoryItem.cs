// 
// CurveModifyHistoryItem.cs
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
using Pinta.Core;
using Cairo;

namespace Pinta.Tools
{
	public class CurveModifyHistoryItem : BaseHistoryItem
	{
		private CurveEngineCollection cEngines;

		private int selectedPointIndex, selectedPointCurveIndex;

		/// <summary>
		/// A history item for when curves are modified.
		/// </summary>
		/// <param name="icon">The history item's icon.</param>
		/// <param name="text">The history item's title.</param>
		public CurveModifyHistoryItem(string icon, string text) : base(icon, text)
		{
			cEngines = Pinta.Tools.LineCurveTool.CEngines.PartialClone();
			selectedPointIndex = Pinta.Tools.LineCurveTool.SelectedPointIndex;
			selectedPointCurveIndex = Pinta.Tools.LineCurveTool.SelectedPointCurveIndex;
		}

		public override void Undo()
		{
			Swap();
		}

		public override void Redo()
		{
			Swap();
		}

		private void Swap()
		{
			//Store the old curve data temporarily.
			CurveEngineCollection oldCEngine = cEngines;

			//Swap half of the data.
			cEngines = Pinta.Tools.LineCurveTool.CEngines;

			//Swap the other half.
			Pinta.Tools.LineCurveTool.CEngines = oldCEngine;


			//Swap the selected point data.
			int temp = selectedPointIndex;
			selectedPointIndex = Pinta.Tools.LineCurveTool.SelectedPointIndex;
			Pinta.Tools.LineCurveTool.SelectedPointIndex = temp;

			//Swap the selected curve data.
			temp = selectedPointCurveIndex;
			selectedPointCurveIndex = Pinta.Tools.LineCurveTool.SelectedPointCurveIndex;
			Pinta.Tools.LineCurveTool.SelectedPointCurveIndex = temp;
		}
	}
}
