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

		SurfaceDiff curves_surface_diff;
		ImageSurface curvesSurface;

		SurfaceDiff user_surface_diff;
		ImageSurface userSurface;

		CurveEngine cEngine;

		/// <summary>
		/// A history item for when curves are created, modified, and/or finalized.
		/// </summary>
		/// <param name="icon">The history item's icon.</param>
		/// <param name="text">The history item's title.</param>
		/// <param name="passedCurvesSurface">The stored CurvesLayer surface.</param>
		/// <param name="passedUserSurface">The stored UserLayer surface.</param>
		/// <param name="passedCurveEngine">The curve engine being used.</param>
		/// <param name="passedUserLayer">The UserLayer being modified.</param>
		public CurvesHistoryItem(string icon, string text, ImageSurface passedCurvesSurface,
		                       ImageSurface passedUserSurface, CurveEngine passedCurveEngine,
		                       UserLayer passedUserLayer) : base(icon, text)
		{
			userLayer = passedUserLayer;


			curves_surface_diff = SurfaceDiff.Create(passedCurvesSurface, userLayer.TextLayer.Layer.Surface, true);
			
			if (curves_surface_diff == null)
			{
				curvesSurface = passedCurvesSurface;
			}
			else
			{
				(passedCurvesSurface as IDisposable).Dispose();
			}


			user_surface_diff = SurfaceDiff.Create(passedUserSurface, userLayer.Surface, true);

			if (user_surface_diff == null)
			{
				userSurface = passedUserSurface;
			}
			else
			{
				(passedUserSurface as IDisposable).Dispose();
			}


			cEngine = passedCurveEngine;
		}

		public CurvesHistoryItem(string icon, string text) : base(icon, text)
		{
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

			if (curves_surface_diff != null)
			{
				curves_surface_diff.ApplyAndSwap(surf);
				PintaCore.Workspace.Invalidate(curves_surface_diff.GetBounds());
			}
			else
			{
				// Undo to the "old" surface
				userLayer.TextLayer.Layer.Surface = curvesSurface;

				// Store the original surface for Redo
				curvesSurface = surf;
			}



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



			//Store the old text data temporarily.
			CurveEngine oldCEngine = cEngine;

			//Swap half of the data.
			cEngine = Pinta.Tools.LineCurveTool.cEngine;

			//Swap the other half.
			Pinta.Tools.LineCurveTool.cEngine = oldCEngine;
		}

		public override void Dispose()
		{
			// Free up native surface
			if (curvesSurface != null)
				(curvesSurface as IDisposable).Dispose();

			// Free up native surface
			if (userSurface != null)
				(userSurface as IDisposable).Dispose();
		}
	}
}
