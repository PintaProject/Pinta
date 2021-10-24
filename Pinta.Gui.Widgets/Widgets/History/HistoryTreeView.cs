// 
// HistoryTreeView.cs
//  
// Author:
//       Anirudh Sanjeev <anirudh@anirudhsanjeev.org>
//	Joe Hillenbrand <joehillen@gmail.com>
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
using System.Diagnostics.CodeAnalysis;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class HistoryTreeView : ScrolledWindow
	{
		private TreeView tree;
		private Document? active_document;

		public HistoryTreeView ()
		{
			Build ();

			PintaCore.Workspace.ActiveDocumentChanged += Workspace_ActiveDocumentChanged;
		}

		[MemberNotNull (nameof (tree))]
		private void Build ()
		{
			CanFocus = false;
			SetSizeRequest (200, 200);

			SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

			tree = new TreeView {
				CanFocus = false,

				HeadersVisible = false,
				EnableGridLines = TreeViewGridLines.None,
				EnableTreeLines = false
			};

			tree.Selection.Mode = SelectionMode.Single;
			tree.Selection.SelectFunction = HistoryItemSelected;

			var icon_column = new TreeViewColumn ();
			var icon_cell = new CellRendererPixbuf ();
			icon_column.PackStart (icon_cell, true);

			var text_column = new TreeViewColumn ();
			var text_cell = new CellRendererText ();
			text_column.PackStart (text_cell, true);

			text_column.SetCellDataFunc (text_cell, new TreeCellDataFunc (HistoryRenderText));
			icon_column.SetCellDataFunc (icon_cell, new TreeCellDataFunc (HistoryRenderIcon));

			tree.AppendColumn (icon_column);
			tree.AppendColumn (text_column);

			Add (tree);

			ShowAll ();
		}

		private void Workspace_ActiveDocumentChanged (object? sender, EventArgs e)
		{
			var doc = PintaCore.Workspace.HasOpenDocuments ? PintaCore.Workspace.ActiveDocument : null;

			if (active_document == doc)
				return;

			if (active_document is not null) {
				active_document.History.HistoryItemAdded -= new EventHandler<HistoryItemAddedEventArgs> (OnHistoryItemsChanged);
				active_document.History.ActionUndone -= new EventHandler (OnHistoryItemsChanged);
				active_document.History.ActionRedone -= new EventHandler (OnHistoryItemsChanged);
			}

			tree.Model = doc?.History.ListStore;

			if (doc is not null) {
				doc.History.HistoryItemAdded += new EventHandler<HistoryItemAddedEventArgs> (OnHistoryItemsChanged);
				doc.History.ActionUndone += new EventHandler (OnHistoryItemsChanged);
				doc.History.ActionRedone += new EventHandler (OnHistoryItemsChanged);
			}

			active_document = doc;

			OnHistoryItemsChanged (this, EventArgs.Empty);
		}

		public bool HistoryItemSelected (TreeSelection selection, ITreeModel model, TreePath path, bool path_currently_selected)
		{
			if (active_document is null)
				return true;

			var current = path.Indices[0];

			if (!path_currently_selected) {
				while (active_document.History.Pointer < current)
					active_document.History.Redo ();

				while (active_document.History.Pointer > current)
					active_document.History.Undo ();
			}

			return true;
		}

		private void HistoryRenderText (TreeViewColumn column, CellRenderer cell, ITreeModel model, TreeIter iter)
		{
			var item = (BaseHistoryItem) model.GetValue (iter, 0);

			if (item.State == HistoryItemState.Undo) {
				((CellRendererText) cell).Style = Pango.Style.Normal;
				((CellRendererText) cell).Foreground = "black";
				((CellRendererText) cell).Text = item.Text;
			} else if (item.State == HistoryItemState.Redo) {
				((CellRendererText) cell).Style = Pango.Style.Oblique;
				((CellRendererText) cell).Foreground = "gray";
				((CellRendererText) cell).Text = item.Text;
			}
		}

		private void HistoryRenderIcon (TreeViewColumn column, CellRenderer cell, ITreeModel model, TreeIter iter)
		{
			var item = (BaseHistoryItem) model.GetValue (iter, 0);
			var pixbuf_cell = (CellRendererPixbuf) cell;

			if (pixbuf_cell.Pixbuf != null)
				pixbuf_cell.Pixbuf.Dispose ();

			if (item.Icon != null)
				pixbuf_cell.Pixbuf = PintaCore.Resources.GetIcon (item.Icon);
		}

		private void OnHistoryItemsChanged (object? o, EventArgs args)
		{
			if (active_document is null)
				return;

			if (tree.Model != null && active_document.History.Current != null) {
				tree.Selection.SelectIter (active_document.History.Current.Id);
				tree.ScrollToCell (tree.Model.GetPath (active_document.History.Current.Id), tree.Columns[1], true, (float) 0.9, 0);
			}
		}
	}
}
