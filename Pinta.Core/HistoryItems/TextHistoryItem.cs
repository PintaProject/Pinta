// 
// TextHistoryItem.cs
//  
// Author:
//       Andrew Davis <andrew.3.1415@gmail.com>
// 
// Copyright (c) 2012 Andrew Davis, GSoC 2012
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

namespace Pinta.Core
{
	public class TextHistoryItem : BaseHistoryItem
	{
		UserLayer userLayer;

		SurfaceDiff text_surface_diff;
		ImageSurface textSurface;

		SurfaceDiff user_surface_diff;
		ImageSurface userSurface;

		TextEngine tEngine;
		Gdk.Rectangle textBounds;

		/// <summary>
		/// A history item for when text is created, edited, and/or finalized.
		/// </summary>
		/// <param name="icon">The history item's icon.</param>
		/// <param name="text">The history item's title.</param>
		/// <param name="passedTextSurface">The stored TextLayer surface.</param>
		/// <param name="passedUserSurface">The stored UserLayer surface.</param>
		/// <param name="passedTextEngine">The text engine being used.</param>
		/// <param name="passedUserLayer">The UserLayer being modified.</param>
		public TextHistoryItem(string icon, string text, ImageSurface passedTextSurface,
		                       ImageSurface passedUserSurface, TextEngine passedTextEngine,
		                       UserLayer passedUserLayer) : base(icon, text)
		{
			userLayer = passedUserLayer;


			text_surface_diff = SurfaceDiff.Create(passedTextSurface, userLayer.TextLayer.Surface, true);
			
			if (text_surface_diff == null)
			{
				textSurface = passedTextSurface;
			}
			else
			{
				(passedTextSurface as IDisposable).Dispose();
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


			tEngine = passedTextEngine;

			textBounds = new Gdk.Rectangle(userLayer.textBounds.X, userLayer.textBounds.Y, userLayer.textBounds.Width, userLayer.textBounds.Height);
		}

		public TextHistoryItem(string icon, string text) : base(icon, text)
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
			ImageSurface surf = userLayer.TextLayer.Surface;

			if (text_surface_diff != null)
			{
				text_surface_diff.ApplyAndSwap(surf);
				PintaCore.Workspace.Invalidate(text_surface_diff.GetBounds());
			}
			else
			{
				// Undo to the "old" surface
				userLayer.TextLayer.Surface = textSurface;

				// Store the original surface for Redo
				textSurface = surf;
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
			TextEngine oldTEngine = tEngine;
			Gdk.Rectangle oldTextBounds = textBounds;

			//Swap half of the data.
			tEngine = userLayer.tEngine;
			textBounds = userLayer.textBounds;

			//Swap the other half.
			userLayer.tEngine = oldTEngine;
			userLayer.textBounds = oldTextBounds;
		}

		public override void Dispose()
		{
			// Free up native surface
			if (textSurface != null)
				(textSurface as IDisposable).Dispose();

			// Free up native surface
			if (userSurface != null)
				(userSurface as IDisposable).Dispose();
		}
	}
}
