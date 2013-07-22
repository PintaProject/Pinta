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
		UserLayer userLayer;

		SurfaceDiff user_surface_diff;
		ImageSurface userSurface;

		CurveEngineCollection cEngines;

		int selectedPointIndex, selectedPointCurveIndex;

		/// <summary>
		/// A history item for when curves are finalized.
		/// </summary>
		/// <param name="icon">The history item's icon.</param>
		/// <param name="text">The history item's title.</param>
		/// <param name="passedUserSurface">The stored UserLayer surface.</param>
		/// <param name="passedCurveEngines">The curve engines being used.</param>
		/// <param name="passedUserLayer">The UserLayer being modified.</param>
		public CurvesHistoryItem(string icon, string text, ImageSurface passedUserSurface, CurveEngineCollection passedCurveEngines,
		                       UserLayer passedUserLayer) : base(icon, text)
		{
			userLayer = passedUserLayer;


			user_surface_diff = SurfaceDiff.Create(passedUserSurface, userLayer.Surface, true);

			if (user_surface_diff == null)
			{
				userSurface = passedUserSurface;
			}
			else
			{
				(passedUserSurface as IDisposable).Dispose();
			}


			cEngines = passedCurveEngines;


			selectedPointIndex = Pinta.Tools.LineCurveTool.selectedPointIndex;
			selectedPointCurveIndex = Pinta.Tools.LineCurveTool.selectedPointCurveIndex;
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

			if (user_surface_diff != null)
			{
				user_surface_diff.ApplyAndSwap(surf);
				PintaCore.Workspace.Invalidate(user_surface_diff.GetBounds());
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
			cEngines = Pinta.Tools.LineCurveTool.cEngines;

			//Swap the other half.
			Pinta.Tools.LineCurveTool.cEngines = oldCEngine;


			//Swap the selected point data.
			int temp = selectedPointIndex;
			selectedPointIndex = Pinta.Tools.LineCurveTool.selectedPointIndex;
			Pinta.Tools.LineCurveTool.selectedPointIndex = temp;

			//Swap the selected curve data.
			temp = selectedPointCurveIndex;
			selectedPointCurveIndex = Pinta.Tools.LineCurveTool.selectedPointCurveIndex;
			Pinta.Tools.LineCurveTool.selectedPointCurveIndex = temp;
		}

		public override void Dispose()
		{
			// Free up native surface
			if (userSurface != null)
				(userSurface as IDisposable).Dispose();
		}
	}
}
