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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class HistoryTreeView : ScrolledWindow
	{
		private TreeView tree;
		
		public HistoryTreeView ()
		{
			CanFocus = false;
			SetSizeRequest (200, 200);

			SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

			tree = new TreeView ();
			tree.CanFocus = false;

			tree.HeadersVisible = false;
			tree.EnableGridLines = TreeViewGridLines.None;
			tree.EnableTreeLines = false;
			tree.Selection.Mode = SelectionMode.Single;
			tree.Selection.SelectFunction = HistoryItemSelected;

			Gtk.TreeViewColumn icon_column = new Gtk.TreeViewColumn ();
			Gtk.CellRendererPixbuf icon_cell = new Gtk.CellRendererPixbuf ();
			icon_column.PackStart (icon_cell, true);

			Gtk.TreeViewColumn text_column = new Gtk.TreeViewColumn ();
			Gtk.CellRendererText text_cell = new Gtk.CellRendererText ();
			text_column.PackStart (text_cell, true);

			text_column.SetCellDataFunc (text_cell, new Gtk.TreeCellDataFunc (HistoryRenderText));
			icon_column.SetCellDataFunc (icon_cell, new Gtk.TreeCellDataFunc (HistoryRenderIcon));

			tree.AppendColumn (icon_column);
			tree.AppendColumn (text_column);

			PintaCore.Workspace.ActiveDocumentChanged += Workspace_ActiveDocumentChanged;
			
			PintaCore.History.HistoryItemAdded += new EventHandler<HistoryItemAddedEventArgs> (OnHistoryItemsChanged);
			PintaCore.History.ActionUndone += new EventHandler (OnHistoryItemsChanged);
			PintaCore.History.ActionRedone += new EventHandler (OnHistoryItemsChanged);

			Add (tree);
			ShowAll ();
		}

		private void Workspace_ActiveDocumentChanged (object sender, EventArgs e)
		{
			if (PintaCore.Workspace.HasOpenDocuments)
				tree.Model = PintaCore.Workspace.ActiveWorkspace.History.ListStore;
			else
				tree.Model = null;
				
			OnHistoryItemsChanged (this, EventArgs.Empty);
		}

		#region History
		public bool HistoryItemSelected (TreeSelection selection, TreeModel model, TreePath path, bool path_currently_selected)
		{
			int current = path.Indices[0];
			if (!path_currently_selected) {
				while (PintaCore.History.Pointer < current) {
					PintaCore.History.Redo ();
				}
				while (PintaCore.History.Pointer > current) {
					PintaCore.History.Undo ();
				}
			}
			return true;
		}

		private void HistoryRenderText (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			BaseHistoryItem item = (BaseHistoryItem)model.GetValue (iter, 0);
			if (item.State == HistoryItemState.Undo) {
				(cell as Gtk.CellRendererText).Style = Pango.Style.Normal;
				(cell as Gtk.CellRendererText).Foreground = "black";
				(cell as Gtk.CellRendererText).Text = item.Text;
			} else if (item.State == HistoryItemState.Redo) {
				(cell as Gtk.CellRendererText).Style = Pango.Style.Oblique;
				(cell as Gtk.CellRendererText).Foreground = "gray";
				(cell as Gtk.CellRendererText).Text = item.Text;
			}
		}

		private void HistoryRenderIcon (Gtk.TreeViewColumn column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			BaseHistoryItem item = (BaseHistoryItem)model.GetValue (iter, 0);
			var pixbuf_cell = cell as Gtk.CellRendererPixbuf;
			if (pixbuf_cell.Pixbuf != null)
				pixbuf_cell.Pixbuf.Dispose ();
			pixbuf_cell.Pixbuf = PintaCore.Resources.GetIcon (item.Icon);
		}

		private void OnHistoryItemsChanged (object o, EventArgs args)
		{
			if (tree.Model != null && PintaCore.History.Current != null) {
				tree.Selection.SelectIter (PintaCore.History.Current.Id);
				tree.ScrollToCell (tree.Model.GetPath (PintaCore.History.Current.Id), tree.Columns[1], true, (float)0.9, 0);
			}
		}
		#endregion
	}
}
