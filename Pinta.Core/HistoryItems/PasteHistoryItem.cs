// 
// PasteHistoryItem.cs
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
using Gtk;

namespace Pinta.Core
{
	public class PasteHistoryItem : BaseHistoryItem
	{
		private Gdk.Pixbuf paste_image;
		private DocumentSelection old_selection;

		public override bool CausesDirty { get { return true; } }

		public PasteHistoryItem (Gdk.Pixbuf pasteImage, DocumentSelection oldSelection)
		{
			Text = Catalog.GetString ("Paste");
			Icon = Stock.Paste;

			paste_image = pasteImage;
			old_selection = oldSelection;
		}

		public override void Redo ()
		{
			Document doc = PintaCore.Workspace.ActiveDocument;

			// Copy the paste to the temp layer
			doc.CreateSelectionLayer ();
			doc.ShowSelectionLayer = true;

			using (Cairo.Context g = new Cairo.Context (doc.SelectionLayer.Surface)) {
				g.DrawPixbuf (paste_image, new Cairo.Point (0, 0));
			}

			Swap ();

			PintaCore.Workspace.Invalidate ();
			PintaCore.Tools.SetCurrentTool (Catalog.GetString ("Move Selected Pixels"));
		}

		public override void Undo ()
		{
			Swap ();

			PintaCore.Layers.DestroySelectionLayer ();
			PintaCore.Workspace.Invalidate ();
		}

		public override void Dispose ()
		{
			if (paste_image != null)
				(paste_image as IDisposable).Dispose ();

			if (old_selection != null)
				old_selection.Dispose ();
		}

		private void Swap ()
		{
			// Swap the selection paths, and whether the
			// selection path should be visible
			Document doc = PintaCore.Workspace.ActiveDocument;

			DocumentSelection swap_selection = doc.Selection;
			doc.Selection = old_selection;
			old_selection = swap_selection;
		}
	}
}
