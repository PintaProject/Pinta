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

using System;
using System.IO;

namespace Pinta.Core
{
	public class UpdateLayerPropertiesHistoryItem : BaseHistoryItem
	{
		int layer_index;
		LayerProperties initial_properties;
		LayerProperties updated_properties;

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
			var layer = PintaCore.Layers[layer_index];
			layer.Opacity = initial_properties.Opacity;
			layer.Hidden = initial_properties.Hidden;
			layer.Name = initial_properties.Name;
		}

		public override void Redo ()
		{
			var layer = PintaCore.Layers[layer_index];
			layer.Opacity = updated_properties.Opacity;
			layer.Hidden = updated_properties.Hidden;
			layer.Name = updated_properties.Name;
		}

		public override void Dispose ()
		{
		}

		public override void LoadInternal (BinaryReader reader)
		{
			base.LoadInternal (reader);
			layer_index = reader.ReadInt32 ();
			initial_properties = new LayerProperties (reader.ReadString (), reader.ReadBoolean (), reader.ReadDouble ());
			updated_properties = new LayerProperties (reader.ReadString (), reader.ReadBoolean (), reader.ReadDouble ());
		}

		public override void Save (BinaryWriter writer)
		{
			base.Save (writer);
			writer.Write (layer_index);
			writer.Write (initial_properties.Name);
			writer.Write (initial_properties.Hidden);
			writer.Write (initial_properties.Opacity);
			writer.Write (updated_properties.Name);
			writer.Write (updated_properties.Hidden);
			writer.Write (updated_properties.Opacity);
		}
	}
}
