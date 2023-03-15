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
	// GObject subclass for use with Gio.ListStore
	public class HistoryListViewItem : GObject.Object
	{
		public HistoryListViewItem (BaseHistoryItem item) : base (true, Array.Empty<GObject.ConstructArgument> ())
		{
			Label = item.Text!;
		}

		public string Label { get; set; }
	}

	public class HistoryListView : ScrolledWindow
	{
		private ListView view;
		private Gio.ListStore model;
		private Gtk.SingleSelection selection_model;
		private Gtk.SignalListItemFactory factory;
		private Document? active_document;

		public HistoryListView ()
		{
			CanFocus = false;
			SetSizeRequest (200, 200);
			SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

			model = Gio.ListStore.New (HistoryListViewItem.GetGType ());

			selection_model = Gtk.SingleSelection.New (model);
#if false // TODO-GTK4 - the selection-changed signal isn't generated currently (https://github.com/gircore/gir.core/issues/831)
			selection_model.OnSelectionChanged += OnSelectionChanged;
#endif

			// TODO-GTK4
			// - improve spacing
			// - show the history item's icons
			// - display undone items as faded / italic
			factory = Gtk.SignalListItemFactory.New ();
			factory.OnSetup += (factory, args) => {
				var label = new Gtk.Label ();
				var item = (Gtk.ListItem) args.Object;
				item.SetChild (label);
			};
			factory.OnBind += (factory, args) => {
				var list_item = (Gtk.ListItem) args.Object;
				var model_item = (HistoryListViewItem) list_item.GetItem ()!;
				var label = (Gtk.Label) list_item.GetChild ()!;
				label.SetText (model_item.Label);
			};

			view = ListView.New (selection_model, factory);
			view.CanFocus = false;

			SetChild (view);

			PintaCore.Workspace.ActiveDocumentChanged += OnActiveDocumentChanged;
		}

		private void OnActiveDocumentChanged (object? sender, EventArgs e)
		{
			var doc = PintaCore.Workspace.HasOpenDocuments ? PintaCore.Workspace.ActiveDocument : null;
			if (active_document == doc)
				return;

			if (active_document is not null) {
				active_document.History.HistoryItemAdded -= OnHistoryItemAdded;
				active_document.History.ActionUndone -= OnUndoOrRedo;
				active_document.History.ActionRedone -= OnUndoOrRedo;
			}

			// Clear out old items and rebuild.
			model.RemoveMultiple (0, model.GetNItems ());

			if (doc is not null) {
				foreach (BaseHistoryItem item in doc.History.Items) {
					model.Append (new HistoryListViewItem (item));
				}

				doc.History.HistoryItemAdded += OnHistoryItemAdded;
				doc.History.ActionUndone += OnUndoOrRedo;
				doc.History.ActionRedone += OnUndoOrRedo;
			}

			active_document = doc;
		}

		private void OnHistoryItemAdded (object? sender, HistoryItemAddedEventArgs args)
		{
			ArgumentNullException.ThrowIfNull (active_document);

			uint idx = (uint) active_document.History.Pointer;

			// Remove any stale (previously undone) items before adding the new item.
			model.RemoveMultiple (idx, model.GetNItems () - idx);

			model.Append (new HistoryListViewItem (args.Item));
			selection_model.SetSelected (idx);
		}

		private void OnUndoOrRedo (object? sender, EventArgs args)
		{
			ArgumentNullException.ThrowIfNull (active_document);

			// Update the selected history item.
			uint selected_idx = (uint) active_document.History.Pointer;
			selection_model.SetSelected (selected_idx);
		}
	}
}
