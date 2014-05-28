// 
// CurvesHistoryItem.cs
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
	public class CurvesHistoryItem : BaseHistoryItem
	{
        private EditEngine ee;

		private UserLayer userLayer;

		private SurfaceDiff userSurfaceDiff;
		private ImageSurface userSurface;

		private CurveEngineCollection cEngines;

		private int selectedPointIndex, selectedPointCurveIndex;

		/// <summary>
		/// A history item for when curves are finalized.
		/// </summary>
        /// <param name="passedEE">The EditEngine being used.</param>
		/// <param name="icon">The history item's icon.</param>
		/// <param name="text">The history item's title.</param>
		/// <param name="passedUserSurface">The stored UserLayer surface.</param>
		/// <param name="passedUserLayer">The UserLayer being modified.</param>
		/// <param name="passedSelectedPointIndex">The selected point's index.</param>
		/// <param name="passedSelectedPointCurveIndex">The selected point's curve index.</param>
        public CurvesHistoryItem(EditEngine passedEE, string icon, string text, ImageSurface passedUserSurface, UserLayer passedUserLayer,
			int passedSelectedPointIndex, int passedSelectedPointCurveIndex) : base(icon, text)
		{
            ee = passedEE;

			userLayer = passedUserLayer;


			userSurfaceDiff = SurfaceDiff.Create(passedUserSurface, userLayer.Surface, true);

			if (userSurfaceDiff == null)
			{
				userSurface = passedUserSurface;
			}
			else
			{
				(passedUserSurface as IDisposable).Dispose();
			}


			cEngines = ee.CEngines.PartialClone();
			selectedPointIndex = passedSelectedPointIndex;
			selectedPointCurveIndex = passedSelectedPointCurveIndex;
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
			// Grab the original surface
			ImageSurface surf = PintaCore.Workspace.ActiveDocument.ToolLayer.Surface;



			// Grab the original surface
			surf = userLayer.Surface;

			if (userSurfaceDiff != null)
			{
				userSurfaceDiff.ApplyAndSwap(surf);
				PintaCore.Workspace.Invalidate(userSurfaceDiff.GetBounds());
			}
			else
			{
				// Undo to the "old" surface
				userLayer.Surface = userSurface;

				// Store the original surface for Redo
				userSurface = surf;
			}



			//Redraw everything since surfaces were swapped.
			PintaCore.Workspace.Invalidate();



			//Store the old curve data temporarily.
			CurveEngineCollection oldCEngine = cEngines;

			//Swap half of the data.
            cEngines = ee.CEngines;

			//Swap the other half.
            ee.CEngines = oldCEngine;


			//Swap the selected point data.
			int temp = selectedPointIndex;
            selectedPointIndex = ee.SelectedPointIndex;
            ee.SelectedPointIndex = temp;

			//Swap the selected curve data.
			temp = selectedPointCurveIndex;
            selectedPointCurveIndex = ee.SelectedPointCurveIndex;
            ee.SelectedPointCurveIndex = temp;
		}

		public override void Dispose()
		{
			// Free up native surface
			if (userSurface != null)
				(userSurface as IDisposable).Dispose();
		}
	}
}
