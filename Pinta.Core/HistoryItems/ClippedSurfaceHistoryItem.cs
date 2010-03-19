// 
// SimpleHistoryItem.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
// 
// Copyright (c) 2010 Jonathan Pobst
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
	public class ClippedSurfaceHistoryItem : BaseHistoryItem
	{
		IrregularSurface old_surface;
		int layer_index;

		public ClippedSurfaceHistoryItem (string icon, string text, IrregularSurface oldSurface, int layerIndex) : base (icon, text)
		{
			old_surface = (IrregularSurface)oldSurface.Clone();
			layer_index = layerIndex;
		}

		public ClippedSurfaceHistoryItem (string icon, string text) : base (icon, text)
		{
		}

		public override void Undo ()
		{
			// Grab the original surface
			IrregularSurface new_surf = new IrregularSurface(PintaCore.Layers[layer_index].Surface, old_surface.Region);
			
			// Undo to the "old" surface
			old_surface.Draw(PintaCore.Layers[layer_index].Surface);
			
			// Store the original surface for Redo
			old_surface = new_surf;
			
			PintaCore.Workspace.Invalidate (old_surface.Region.Clipbox);
		}

		public override void Redo ()
		{
			// Grab the original surface
			IrregularSurface new_surf = new IrregularSurface(PintaCore.Layers[layer_index].Surface, old_surface.Region);
			
			// Undo to the "old" surface
			old_surface.Draw(PintaCore.Layers[layer_index].Surface);
			
			// Store the original surface for Redo
			old_surface = new_surf;
			
			PintaCore.Workspace.Invalidate (old_surface.Region.Clipbox);
		}

		public override void Dispose ()
		{
			// Free up native surface
			(old_surface as IDisposable).Dispose ();
		}

	}
}