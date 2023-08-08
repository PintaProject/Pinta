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

using Cairo;

namespace Pinta.Core
{
	public class FinishPixelsHistoryItem : BaseHistoryItem
	{
		private ImageSurface? old_selection_layer;
		private ImageSurface? old_surface;
		private Matrix old_transform = CairoExtensions.CreateIdentityMatrix ();

		public override bool CausesDirty => false;

		public FinishPixelsHistoryItem ()
		{
			Text = Translations.GetString ("Finish Pixels");
			Icon = Resources.Icons.ToolMove; ;
		}

		public override void Undo ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			doc.Layers.ShowSelectionLayer = true;

			Matrix swap_transform = doc.Layers.SelectionLayer.Transform;
			ImageSurface swap_surf = doc.Layers.CurrentUserLayer.Surface;
			ImageSurface swap_sel = doc.Layers.SelectionLayer.Surface;

			doc.Layers.SelectionLayer.Surface = old_selection_layer!; // NRT - Set in TakeSnapshot
			doc.Layers.SelectionLayer.Transform = old_transform;
			doc.Layers.CurrentUserLayer.Surface = old_surface!;

			old_transform = swap_transform;
			old_surface = swap_surf;
			old_selection_layer = swap_sel;

			PintaCore.Workspace.Invalidate ();
			PintaCore.Tools.SetCurrentTool ("MoveSelectedTool");
		}

		public override void Redo ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			Matrix swap_transfrom = doc.Layers.SelectionLayer.Transform;
			ImageSurface swap_surf = doc.Layers.CurrentUserLayer.Surface.Clone ();
			ImageSurface swap_sel = doc.Layers.SelectionLayer.Surface;

			doc.Layers.CurrentUserLayer.Surface = old_surface!; // NRT - Set in TakeSnapshot
			doc.Layers.SelectionLayer.Surface = old_selection_layer!;
			doc.Layers.SelectionLayer.Transform = old_transform;

			old_surface = swap_surf;
			old_selection_layer = swap_sel;
			old_transform = swap_transfrom;

			doc.Layers.DestroySelectionLayer ();
			PintaCore.Workspace.Invalidate ();
		}

		public void TakeSnapshot ()
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			old_selection_layer = doc.Layers.SelectionLayer.Surface.Clone ();
			old_surface = doc.Layers.CurrentUserLayer.Surface.Clone ();
			old_transform = doc.Layers.SelectionLayer.Transform.Clone ();
		}
	}
}
