// 
// LayersListWidget.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//       Greg Lowe <greg@vis.net.nz>
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
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	public class LayersListView : ScrolledWindow
	{
		private ListView view;
		private Gio.ListStore model;
		private Gtk.SingleSelection selection_model;
		private Gtk.SignalListItemFactory factory;
		private Document? active_document;

		public LayersListView ()
		{
			CanFocus = false;
			SetSizeRequest (200, 200);
			SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

			model = Gio.ListStore.New (LayersListViewItem.GetGType ());

			selection_model = Gtk.SingleSelection.New (model);
#if false // TODO-GTK4 - the selection-changed signal isn't generated currently (https://github.com/gircore/gir.core/issues/831)
			selection_model.OnSelectionChanged += OnSelectionChanged;
#endif

			factory = Gtk.SignalListItemFactory.New ();
			factory.OnSetup += (factory, args) => {
				var item = (Gtk.ListItem) args.Object;
				item.SetChild (new LayersListViewItemWidget ());
			};
			factory.OnBind += (factory, args) => {
				var list_item = (Gtk.ListItem) args.Object;
				var model_item = (LayersListViewItem) list_item.GetItem ()!;
				var widget = (LayersListViewItemWidget) list_item.GetChild ()!;
				widget.Update (model_item);
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

			if (active_document != null) {
				active_document.History.HistoryItemAdded -= OnHistoryChanged;
				active_document.History.ActionUndone -= OnHistoryChanged;
				active_document.History.ActionRedone -= OnHistoryChanged;
				active_document.Layers.LayerAdded -= OnLayerAddedOrRemoved;
				active_document.Layers.LayerRemoved -= OnLayerAddedOrRemoved;
				active_document.Layers.SelectedLayerChanged -= OnSelectedLayerChanged;
				active_document.Layers.LayerPropertyChanged -= OnLayerPropertyChanged;
			}

			// Clear out old items and rebuild.
			model.RemoveMultiple (0, model.GetNItems ());

			if (doc is not null) {
				foreach (var layer in doc.Layers.UserLayers.Reverse ()) {
					model.Append (new LayersListViewItem (layer));
				}

				doc.History.HistoryItemAdded += OnHistoryChanged;
				doc.History.ActionUndone += OnHistoryChanged;
				doc.History.ActionRedone += OnHistoryChanged;
				doc.Layers.LayerAdded += OnLayerAddedOrRemoved;
				doc.Layers.LayerRemoved += OnLayerAddedOrRemoved;
				doc.Layers.SelectedLayerChanged += OnSelectedLayerChanged;
				doc.Layers.LayerPropertyChanged += OnLayerPropertyChanged;
			}

			active_document = doc;
		}

		private void OnHistoryChanged (object? sender, EventArgs e)
		{
			// TODO-GTK4 - implement this.
		}

		private void OnLayerAddedOrRemoved (object? sender, EventArgs e)
		{
			// TODO-GTK4 - implement this.
		}

		private void OnSelectedLayerChanged (object? sender, EventArgs e)
		{
			// TODO-GTK4 - implement this.
		}

		private void OnLayerPropertyChanged (object? sender, EventArgs e)
		{
			// TODO-GTK4 - implement this.
		}
	}
}
