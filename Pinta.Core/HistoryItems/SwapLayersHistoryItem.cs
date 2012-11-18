// 
// SwapLayersHistoryItem.cs
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
	// These are actions that can be undone by simply repeating
	// the action: invert colors, rotate 180 degrees, etc
	public class SwapLayersHistoryItem : BaseHistoryItem
	{
		private int layer_index_1;
		private int layer_index_2;

		public SwapLayersHistoryItem (string icon, string text, int layer1, int layer2) : base (icon, text)
		{
			layer_index_1 = layer1;
			layer_index_2 = layer2;
		}
		
		public override void Undo ()
		{
			Swap ();
		}

		public override void Redo ()
		{
			Swap ();
		}

		private void Swap ()
		{
			int selected = PintaCore.Layers.CurrentLayerIndex;
			
			int l1 = Math.Min (layer_index_1, layer_index_2);
			int l2 = Math.Max (layer_index_1, layer_index_2);

			UserLayer layer1 = PintaCore.Layers[l1];
			UserLayer layer2 = PintaCore.Layers[l2];

			PintaCore.Layers.DeleteLayer (l1, false);
			PintaCore.Layers.DeleteLayer (l2 - 1, false);

			PintaCore.Layers.Insert (layer2, l1);
			PintaCore.Layers.Insert (layer1, l2);
			
			PintaCore.Layers.SetCurrentLayer (selected);
		}
	}
}
