// 
// AddLayerHistoryItem.cs
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
	public class AddLayerHistoryItem : BaseHistoryItem
	{
		private int layer_index;
		private UserLayer layer;

		public AddLayerHistoryItem (string icon, string text, int newLayerIndex) : base (icon, text)
		{
			layer_index = newLayerIndex;
		}

		public override void Undo ()
		{
			// Store the layer for "redo"
			layer = PintaCore.Layers[layer_index];
			
			PintaCore.Layers.DeleteLayer (layer_index, false);
		}

		public override void Redo ()
		{
			PintaCore.Layers.Insert (layer, layer_index);

			// Make new layer the current layer
			PintaCore.Layers.SetCurrentLayer (layer);

			layer = null;
		}

		public override void Dispose ()
		{
			if (layer != null)
				(layer.Surface as IDisposable).Dispose ();
		}
	}
}
