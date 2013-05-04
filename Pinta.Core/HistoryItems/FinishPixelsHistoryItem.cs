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

namespace Pinta.Core
{
	public class FinishPixelsHistoryItem : BaseHistoryItem
	{
		private ImageSurface old_selection_layer;
		private ImageSurface old_surface;
		private readonly Matrix old_transform = new Matrix();

		public override bool CausesDirty { get { return false; } }
		
		public FinishPixelsHistoryItem ()
		{
			Text = Catalog.GetString ("Finish Pixels");
			Icon = "Tools.Move.png";
		}

		public override void Undo ()
		{
			PintaCore.Layers.ShowSelectionLayer = true;

			Matrix swap_transfrom = new Matrix();
			swap_transfrom.InitMatrix(PintaCore.Layers.SelectionLayer.Transform);
			ImageSurface swap_surf = PintaCore.Layers.CurrentLayer.Surface;
			ImageSurface swap_sel = PintaCore.Layers.SelectionLayer.Surface;

			PintaCore.Layers.SelectionLayer.Surface = old_selection_layer;
			PintaCore.Layers.SelectionLayer.Transform.InitMatrix(old_transform);
			PintaCore.Layers.CurrentLayer.Surface = old_surface;

			old_transform.InitMatrix(swap_transfrom);
			old_surface = swap_surf;
			old_selection_layer = swap_sel;

			PintaCore.Workspace.Invalidate ();
			PintaCore.Tools.SetCurrentTool (Catalog.GetString ("Move Selected Pixels"));
		}

		public override void Redo ()
		{
			Matrix swap_transfrom = new Matrix();
			swap_transfrom.InitMatrix(PintaCore.Layers.SelectionLayer.Transform);
			ImageSurface swap_surf = PintaCore.Layers.CurrentLayer.Surface.Clone ();
			ImageSurface swap_sel = PintaCore.Layers.SelectionLayer.Surface;

			PintaCore.Layers.CurrentLayer.Surface = old_surface;
			PintaCore.Layers.SelectionLayer.Surface = old_selection_layer;
			PintaCore.Layers.SelectionLayer.Transform.InitMatrix(old_transform);

			old_surface = swap_surf;
			old_selection_layer = swap_sel;
			old_transform.InitMatrix(swap_transfrom);

			PintaCore.Layers.DestroySelectionLayer ();
			PintaCore.Workspace.Invalidate ();
		}

		public override void Dispose ()
		{
			if (old_surface != null)
				(old_surface as IDisposable).Dispose ();
			if (old_selection_layer != null)
				(old_selection_layer as IDisposable).Dispose ();
		}

		public void TakeSnapshot ()
		{
			old_selection_layer = PintaCore.Layers.SelectionLayer.Surface.Clone ();
			old_surface = PintaCore.Layers.CurrentLayer.Surface.Clone ();
			old_transform.InitMatrix(PintaCore.Layers.SelectionLayer.Transform);
		}
	}
}
