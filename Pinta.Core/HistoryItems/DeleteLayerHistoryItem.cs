// 
// DeleteLayerHistoryItem.cs
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

namespace Pinta.Core
{
	public class DeleteLayerHistoryItem : BaseHistoryItem
	{
		private int layer_index;
		private UserLayer? layer;

		public DeleteLayerHistoryItem(string icon, string text, UserLayer layer, int layerIndex) : base(icon, text)
		{
			layer_index = layerIndex;
			this.layer = layer;
		}

		public override void Undo ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			doc.Layers.Insert (layer!, layer_index); // NRT - layer is set by constructor

			// Make new layer the current layer
			doc.Layers.SetCurrentUserLayer (layer!);

			layer = null;
		}

		public override void Redo ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			// Store the layer for "undo"
			layer = doc.Layers[layer_index];
			
			doc.Layers.DeleteLayer (layer_index, false);
		}

		public override void Dispose ()
		{
			if (layer != null)
				(layer.Surface as IDisposable).Dispose ();
		}
	}
}
