// 
// ShapesModifyHistoryItem.cs
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
	public class ShapesModifyHistoryItem : BaseHistoryItem
	{
        private BaseEditEngine ee;

		private ShapeEngineCollection sEngines;

		private int selectedPointIndex, selectedShapeIndex;

		/// <summary>
		/// A history item for when shapes are modified.
		/// </summary>
        /// <param name="passedEE">The EditEngine being used.</param>
		/// <param name="icon">The history item's icon.</param>
		/// <param name="text">The history item's title.</param>
		public ShapesModifyHistoryItem(BaseEditEngine passedEE, string icon, string text) : base(icon, text)
		{
            ee = passedEE;

			sEngines = BaseEditEngine.SEngines.PartialClone();
            selectedPointIndex = ee.SelectedPointIndex;
            selectedShapeIndex = ee.SelectedShapeIndex;
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
            Swap (ref sEngines, ref BaseEditEngine.SEngines);

			//Ensure that all of the shapes that should no longer be drawn have their ReEditableLayer removed from the drawing loop.
			foreach (ShapeEngine se in sEngines)
			{
				//Determine if it is currently in the drawing loop and should no longer be. Note: a DrawingLayer could be both removed and then
				//later added in the same swap operation, but this is faster than looping through each ShapeEngine in BaseEditEngine.SEngines.
				if (se.DrawingLayer.InTheLoop && !BaseEditEngine.SEngines.Contains(se))
				{
					se.DrawingLayer.TryRemoveLayer();
				}
			}


			//Ensure that all of the shapes that should now be drawn have their ReEditableLayer in the drawing loop.
			foreach (ShapeEngine se in BaseEditEngine.SEngines)
			{
				//Determine if it is currently out of the drawing loop; if not, it should be.
				if (!se.DrawingLayer.InTheLoop)
				{
					se.DrawingLayer.TryAddLayer();
				}
			}

            Swap (ref selectedPointIndex, ref ee.SelectedPointIndex);
            Swap (ref selectedShapeIndex, ref ee.SelectedShapeIndex);

			//Determine if the currently active tool matches the shape's corresponding tool, and if not, switch to it.
			BaseEditEngine.ActivateCorrespondingTool(selectedShapeIndex, true);
		}
	}
}
