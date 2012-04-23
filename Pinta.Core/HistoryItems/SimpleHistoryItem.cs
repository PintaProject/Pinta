﻿//
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
using System.IO;

namespace Pinta.Core
{
	public class SimpleHistoryItem : BaseHistoryItem
	{
		ImageSurface old_surface;
		int layer_index;

		public SimpleHistoryItem (string icon, string text, ImageSurface oldSurface, int layerIndex) : base (icon, text)
		{
			old_surface = oldSurface;
			layer_index = layerIndex;
		}

		public SimpleHistoryItem (string icon, string text) : base (icon, text)
		{
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
			// Grab the original surface
			ImageSurface surf = PintaCore.Layers[layer_index].Surface;

			// Undo to the "old" surface
			PintaCore.Layers[layer_index].Surface = old_surface;

			// Store the original surface for Redo
			old_surface = surf;

			PintaCore.Workspace.Invalidate ();
		}

		public override void Dispose ()
		{
			// Free up native surface
			(old_surface as IDisposable).Dispose ();
		}

		public void TakeSnapshotOfLayer (int layerIndex)
		{
			layer_index = layerIndex;
			old_surface = PintaCore.Layers[layerIndex].Surface.Clone ();
		}

		public void TakeSnapshotOfLayer (Layer layer)
		{
			layer_index = PintaCore.Layers.IndexOf (layer);
			old_surface = layer.Surface.Clone ();
		}

		public override void LoadInternal (BinaryReader reader)
		{
			base.LoadInternal (reader);
			int len = reader.ReadInt32 ();
			int width = reader.ReadInt32 ();
			int height = reader.ReadInt32 ();
			int stride = reader.ReadInt32 ();
			Format eformat = (Format)reader.ReadInt32 ();
			byte[] datas = reader.ReadBytes (len);
			old_surface = new ImageSurface (datas, eformat, width, height, stride);
			layer_index = reader.ReadInt32 ();
		}
		
		public override void Save (BinaryWriter writer)
		{
			base.Save (writer);
			writer.Write (old_surface.Data.Length);
			writer.Write (old_surface.Width);
			writer.Write (old_surface.Height);
			writer.Write (old_surface.Stride);
			writer.Write ((int)old_surface.Format);
			writer.Write (old_surface.Data);
			writer.Write (layer_index);
		}
	}
}
