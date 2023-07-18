// 
// UpdateLayerPropertiesHistoryItem.cs
//  
// Author:
//       Greg Lowe <greg@vis.net.nz>
// 
// Copyright (c) 2010 Greg Lowe
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

namespace Pinta.Core
{
	public class UpdateLayerPropertiesHistoryItem : BaseHistoryItem
	{
		readonly int layer_index;
		readonly LayerProperties initial_properties;
		readonly LayerProperties updated_properties;

		public UpdateLayerPropertiesHistoryItem (
				 string icon,
				 string text,
				 int layerIndex,
				 LayerProperties initialProperties,
				 LayerProperties updatedProperties)
			: base (icon, text)
		{
			layer_index = layerIndex;
			initial_properties = initialProperties;
			updated_properties = updatedProperties;
		}

		public override void Undo ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			var layer = doc.Layers[layer_index];
			layer.Opacity = initial_properties.Opacity;
			layer.Hidden = initial_properties.Hidden;
			layer.Name = initial_properties.Name;
			layer.BlendMode = initial_properties.BlendMode;

			UpdateSelectionLayer (doc, layer);
		}

		public override void Redo ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			var layer = doc.Layers[layer_index];
			layer.Opacity = updated_properties.Opacity;
			layer.Hidden = updated_properties.Hidden;
			layer.Name = updated_properties.Name;
			layer.BlendMode = updated_properties.BlendMode;

			UpdateSelectionLayer (doc, layer);
		}

		private void UpdateSelectionLayer (Document doc, Layer layer)
		{
			// Keep the selection layer's visibility in sync with the current layer.
			if (doc.Layers.CurrentUserLayer == layer)
				doc.Layers.SelectionLayer.Hidden = layer.Hidden;
		}
	}
}
