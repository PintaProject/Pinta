// 
// FinishPixelsHistoryItem.cs
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
using Mono.Unix;
using System.IO;
using System.Runtime.InteropServices;

namespace Pinta.Core
{
	public class FinishPixelsHistoryItem : BaseHistoryItem
	{
		private ImageSurface old_selection_layer;
		private PointD old_offset;
		private ImageSurface old_surface;

		public override bool CausesDirty { get { return false; } }
		
		public FinishPixelsHistoryItem ()
		{
			Text = Catalog.GetString ("Finish Pixels");
			Icon = "Tools.Move.png";
		}

		public override void Undo ()
		{
			PintaCore.Layers.ShowSelectionLayer = true;

			PointD swap_offset = PintaCore.Layers.SelectionLayer.Offset;
			ImageSurface swap_surf = PintaCore.Layers.CurrentLayer.Surface;
			ImageSurface swap_sel = PintaCore.Layers.SelectionLayer.Surface;

			PintaCore.Layers.SelectionLayer.Surface = old_selection_layer;
			PintaCore.Layers.SelectionLayer.Offset = old_offset;
			PintaCore.Layers.CurrentLayer.Surface = old_surface;

			old_offset = swap_offset;
			old_surface = swap_surf;
			old_selection_layer = swap_sel;

			PintaCore.Workspace.Invalidate ();
			PintaCore.Tools.SetCurrentTool (Catalog.GetString ("Move Selected Pixels"));
		}

		public override void Redo ()
		{
			PointD swap_offset = PintaCore.Layers.SelectionLayer.Offset;
			ImageSurface swap_surf = PintaCore.Layers.CurrentLayer.Surface.Clone ();
			ImageSurface swap_sel = PintaCore.Layers.SelectionLayer.Surface;

			PintaCore.Layers.CurrentLayer.Surface = old_surface;
			PintaCore.Layers.SelectionLayer.Surface = old_selection_layer;
			PintaCore.Layers.SelectionLayer.Offset = old_offset;

			old_surface = swap_surf;
			old_selection_layer = swap_sel;
			old_offset = swap_offset;

			PintaCore.Layers.DestroySelectionLayer ();
			PintaCore.Workspace.Invalidate ();
		}

		public override void Dispose ()
		{
			if (old_surface != null)
				(old_surface as IDisposable).Dispose ();
		}

		public void TakeSnapshot ()
		{
			old_selection_layer = PintaCore.Layers.SelectionLayer.Surface.Clone ();
			old_offset = PintaCore.Layers.SelectionLayer.Offset;
			old_surface = PintaCore.Layers.CurrentLayer.Surface.Clone ();
		}

		public override void LoadInternal (BinaryReader reader)
		{
			base.LoadInternal (reader);

			int len = reader.ReadInt32 ();
			int width = reader.ReadInt32 ();
			int height = reader.ReadInt32 ();
			int stride = reader.ReadInt32 ();
			byte[] datas = reader.ReadBytes (len);
			IntPtr ptr = Marshal.AllocHGlobal (datas.Length * sizeof(byte));
			Marshal.Copy (datas, 0, ptr, len);
			old_selection_layer = new ImageSurface (ptr, Format.Argb32, width, height, stride);

			old_offset = new PointD (reader.ReadInt32 (), reader.ReadInt32 ());

			len = reader.ReadInt32 ();
			width = reader.ReadInt32 ();
			height = reader.ReadInt32 ();
			stride = reader.ReadInt32 ();
			datas = reader.ReadBytes (len);
			IntPtr ptr2 = Marshal.AllocHGlobal (datas.Length * sizeof(byte));
			Marshal.Copy (datas, 0, ptr2, len);
			old_surface = new ImageSurface (ptr2, Format.Argb32, width, height, stride);
		}

		public override void Save (BinaryWriter writer)
		{
			base.Save (writer);
			
			writer.Write (old_selection_layer.Data.Length);
			writer.Write (old_selection_layer.Width);
			writer.Write (old_selection_layer.Height);
			writer.Write (old_selection_layer.Stride);
			writer.Write (old_selection_layer.Data, 0, old_selection_layer.Data.Length);

			writer.Write(old_offset.X);
			writer.Write(old_offset.Y);

			writer.Write (old_surface.Data.Length);
			writer.Write (old_surface.Width);
			writer.Write (old_surface.Height);
			writer.Write (old_surface.Stride);
			writer.Write (old_surface.Data, 0, old_surface.Data.Length);
		}
	}
}
