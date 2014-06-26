// 
// ShapeModifyHistoryItem.cs
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
using Pinta.Core;
using Cairo;

namespace Pinta.Tools
{
	public class ShapeModifyHistoryItem : BaseHistoryItem
	{
        private BaseEditEngine ee;

		private ShapeEngineCollection sEngines;

		private int selectedPointIndex, selectedPointShapeIndex;

		/// <summary>
		/// A history item for when shapes are modified.
		/// </summary>
        /// <param name="passedEE">The EditEngine being used.</param>
		/// <param name="icon">The history item's icon.</param>
		/// <param name="text">The history item's title.</param>
		public ShapeModifyHistoryItem(BaseEditEngine passedEE, string icon, string text) : base(icon, text)
		{
            ee = passedEE;

			sEngines = BaseEditEngine.SEngines.PartialClone();
            selectedPointIndex = ee.SelectedPointIndex;
            selectedPointShapeIndex = ee.SelectedShapeIndex;
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
			//Store the old shape data temporarily.
			ShapeEngineCollection oldSEngine = sEngines;

			//Swap half of the data.
			sEngines = BaseEditEngine.SEngines;

			//Swap the other half.
			BaseEditEngine.SEngines = oldSEngine;


			//Swap the selected point data.
			int temp = selectedPointIndex;
            selectedPointIndex = ee.SelectedPointIndex;
            ee.SelectedPointIndex = temp;

			//Swap the selected shape data.
			temp = selectedPointShapeIndex;
            selectedPointShapeIndex = ee.SelectedShapeIndex;
            ee.SelectedShapeIndex = temp;
		}
	}
}
