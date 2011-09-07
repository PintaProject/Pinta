// 
// OpenImagesListWidget.cs
//  
// Author:
//       Cameron White <cameronwhite91@gmail.com>
// 
// Copyright (c) 2011 2011
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
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class OpenImagesListWidget : ScrolledWindow
	{
		private TreeView tree;
		private ListStore store;

		public OpenImagesListWidget ()
		{
			CanFocus = false;
			SetSizeRequest (200, 200);
			SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

			tree = new TreeView ();
			tree.HeadersVisible = false;
			tree.EnableGridLines = TreeViewGridLines.None;
			tree.Selection.Mode = SelectionMode.Single;
			tree.Selection.SelectFunction = HandleDocumentSelected;

			TreeViewColumn file_name_column = new TreeViewColumn ();
			CellRendererText file_name_cell = new CellRendererText ();
			file_name_column.PackStart (file_name_cell, true);
			file_name_column.AddAttribute (file_name_cell, "text", 0);

			tree.AppendColumn(file_name_column);

			store = new ListStore (typeof (string));
			tree.Model = store;

			Add (tree);
			ShowAll ();

			PintaCore.Workspace.DocumentOpened += HandleDocumentOpenedOrClosed;
			PintaCore.Workspace.DocumentClosed += HandleDocumentOpenedOrClosed;
			PintaCore.Workspace.DocumentCreated += HandleDocumentOpenedOrClosed;
			PintaCore.Workspace.ActiveDocumentChanged += HandleActiveDocumentChanged;
		}

		/// <summary>
		/// If the active document is changed elsewhere, update the selected document in this widget
		/// </summary>
		private void HandleActiveDocumentChanged (object sender, EventArgs e)
		{
			if (PintaCore.Workspace.HasOpenDocuments)
			{
				int doc_index = PintaCore.Workspace.ActiveDocumentIndex;

				if (doc_index != -1)
				{
					var path = new TreePath (new int[] { doc_index });
					tree.Selection.SelectPath (path);
				}
			}
		}

		/// <summary>
		/// Rebuilds the list of documents after a document is opened or closed
		/// </summary>
		private void HandleDocumentOpenedOrClosed (object sender, DocumentEventArgs e)
		{
			store.Clear ();

			foreach (Document doc in PintaCore.Workspace.OpenDocuments)
			{
				store.AppendValues (doc.Filename);
			}
		}

		/// <summary>
		/// Sets the active document as selected by the user
		/// </summary>
		private bool HandleDocumentSelected (TreeSelection selection, TreeModel model, TreePath path, bool path_currently_selected)
		{
			int index = path.Indices[0];

			if (!path_currently_selected && index != PintaCore.Workspace.ActiveDocumentIndex)
			{
				PintaCore.Workspace.SetActiveDocument (index);
			}

			return true;
		}
	}
}

