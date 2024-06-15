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
using System.Linq;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class LayersListView : ScrolledWindow
{
	private readonly ListView view;
	private readonly Gio.ListStore model;
	private readonly Gtk.SingleSelection selection_model;
	private readonly Gtk.SignalListItemFactory factory;
	private Document? active_document;

	public LayersListView ()
	{
		CanFocus = false;
		SetSizeRequest (200, 200);
		SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

		model = Gio.ListStore.New (LayersListViewItem.GetGType ());

		selection_model = Gtk.SingleSelection.New (model);
		selection_model.OnSelectionChanged ((o, args) => HandleSelectionChanged (o, args));

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
		view.OnActivate += HandleRowActivated;

		SetChild (view);

		PintaCore.Workspace.ActiveDocumentChanged += HandleActiveDocumentChanged;
	}

	private void HandleSelectionChanged (object? sender, GtkExtensions.SelectionChangedSignalArgs e)
	{
		ArgumentNullException.ThrowIfNull (active_document);

		int model_idx = (int) selection_model.Selected;
		int doc_idx = active_document.Layers.Count () - 1 - model_idx;

		if (active_document.Layers.CurrentUserLayerIndex != doc_idx)
			active_document.Layers.SetCurrentUserLayer (doc_idx);
	}

	private void HandleRowActivated (ListView sender, ListView.ActivateSignalArgs args)
	{
		// Open the layer properties dialog
		PintaCore.Actions.Layers.Properties.Activate ();
	}

	private void HandleActiveDocumentChanged (object? sender, EventArgs e)
	{
		var doc = PintaCore.Workspace.HasOpenDocuments ? PintaCore.Workspace.ActiveDocument : null;

		if (active_document == doc)
			return;

		if (active_document != null) {
			active_document.History.HistoryItemAdded -= HandleHistoryChanged;
			active_document.History.ActionUndone -= HandleHistoryChanged;
			active_document.History.ActionRedone -= HandleHistoryChanged;
			active_document.Layers.LayerAdded -= HandleLayerAdded;
			active_document.Layers.LayerRemoved -= HandleLayerRemoved;
			active_document.Layers.SelectedLayerChanged -= HandleSelectedLayerChanged;
			active_document.Layers.LayerPropertyChanged -= HandleLayerPropertyChanged;
		}

		// Clear out old items and rebuild.
		model.RemoveMultiple (0, model.GetNItems ());

		if (doc is not null) {
			foreach (var layer in doc.Layers.UserLayers.Reverse ()) {
				model.Append (new LayersListViewItem (doc, layer));
			}

			doc.History.HistoryItemAdded += HandleHistoryChanged;
			doc.History.ActionUndone += HandleHistoryChanged;
			doc.History.ActionRedone += HandleHistoryChanged;
			doc.Layers.LayerAdded += HandleLayerAdded;
			doc.Layers.LayerRemoved += HandleLayerRemoved;
			doc.Layers.SelectedLayerChanged += HandleSelectedLayerChanged;
			doc.Layers.LayerPropertyChanged += HandleLayerPropertyChanged;
		}

		active_document = doc;
	}

	private void HandleHistoryChanged (object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull (active_document);

		// Recreate all the widgets.
		// This update should ideally be done by changing gobject properties instead, but we don't have the ability to add custom properties yet
		uint selected_idx = selection_model.Selected;
		for (uint i = 0; i < model.GetNItems (); ++i) {
			int layer_idx = active_document.Layers.Count () - 1 - (int) i;
			model.Remove (i);
			model.Insert (i, new LayersListViewItem (active_document, active_document.Layers[layer_idx]));
		}

  		// Restore the selection.
		selection_model.Selected = selected_idx;
	}

	private void HandleLayerAdded (object? sender, IndexEventArgs e)
	{
		ArgumentNullException.ThrowIfNull (active_document);

		int index = active_document.Layers.Count () - 1 - e.Index;
		model.Insert ((uint) index, new LayersListViewItem (active_document, active_document.Layers[e.Index]));
	}

	private void HandleLayerRemoved (object? sender, IndexEventArgs e)
	{
		ArgumentNullException.ThrowIfNull (active_document);

		// Note: don't need to subtract 1 because the layer has already been removed from the document.
		model.Remove ((uint) (active_document.Layers.Count () - e.Index));
	}

	private void HandleSelectedLayerChanged (object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull (active_document);

		int index = active_document.Layers.Count () - 1 - active_document.Layers.CurrentUserLayerIndex;
		selection_model.SelectItem ((uint) index, unselectRest: true);
	}

	private void HandleLayerPropertyChanged (object? sender, EventArgs e)
	{
		// Treat the same as an undo event, and update the widgets.
		HandleHistoryChanged (sender, e);
	}
}
