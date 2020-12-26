// 
// MovePixelsHistoryItem.cs
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
	public class MovePixelsHistoryItem : BaseHistoryItem
	{
		// There's 2 types of move pixel operations to handle
		// - The first move "lifts" the selection up into a temporary layer
		//   and then moves it to the new spot
		// - Subsequent moves only move the selection
		//   around the temporary layer
		private Document doc;
		private DocumentSelection? old_selection;
		private readonly Matrix old_transform = new Matrix();
		private ImageSurface? old_surface;
		private int layer_index;
		private bool lifted;		// Whether this item has lift
		private bool is_lifted;		// Track state of undo/redo lift

		public MovePixelsHistoryItem (string icon, string text, Document document) : base (icon, text)
		{
			doc = document;
		}

		public override void Undo ()
		{
			Swap ();
		}

		public override void Redo ()
		{
			Swap ();
		}

		public override void Dispose ()
		{
            old_selection?.Dispose ();

			if (old_surface != null)
				(old_surface as IDisposable).Dispose ();
		}

		private void Swap ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			DocumentSelection swap_selection = doc.Selection;
			doc.Selection = old_selection!; // NRT - Set in TakeSnapshot
			old_selection = swap_selection;

			Matrix swap_transform = new Matrix();
			swap_transform.InitMatrix(doc.Layers.SelectionLayer.Transform);
			doc.Layers.SelectionLayer.Transform.InitMatrix(old_transform);
			old_transform.InitMatrix(swap_transform);

			if (lifted) {
				// Grab the original surface
				ImageSurface surf = doc.Layers[layer_index].Surface;

				// Undo to the "old" surface
				doc.Layers[layer_index].Surface = old_surface!; // NRT - Set in TakeSnapshot

				// Store the original surface for Redo
				old_surface = surf;

				is_lifted = !is_lifted;
				doc.Layers.ShowSelectionLayer = is_lifted;
			}

			PintaCore.Workspace.Invalidate ();
		}
		
		public void TakeSnapshot (bool lift)
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			lifted = lift;
			is_lifted = true;

			if (lift) {
				layer_index = doc.Layers.CurrentUserLayerIndex;
				old_surface = doc.Layers.CurrentUserLayer.Surface.Clone ();
			}
				
			old_selection = doc.Selection.Clone ();
			old_transform.InitMatrix(doc.Layers.SelectionLayer.Transform);
		}
	}
}
