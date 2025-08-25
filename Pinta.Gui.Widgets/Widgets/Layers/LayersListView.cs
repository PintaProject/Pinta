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
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class LayersListView : Gtk.ScrolledWindow
{
	private readonly Gio.ListStore list_model;
	private readonly Gtk.SingleSelection selection_model;
	private Document? active_document;
	private bool changing_selection = false;

	public LayersListView ()
	{
		// --- Control creaton

		Gio.ListStore listModel = Gio.ListStore.New (LayersListViewItem.GetGType ());

		Gtk.SingleSelection selectionModel = Gtk.SingleSelection.New (listModel);
		selectionModel.OnSelectionChanged += HandleSelectionChanged;

		Gtk.SignalListItemFactory factory = Gtk.SignalListItemFactory.New ();
		factory.OnSetup += HandleFactorySetup;
		factory.OnBind += HandleFactoryBind;

		Gtk.ListView view = Gtk.ListView.New (selectionModel, factory);
		view.CanFocus = false;
		view.OnActivate += HandleRowActivated;

		// --- Initialization (Gtk.Widget)

		CanFocus = false;
		SetSizeRequest (200, 200);

		// --- Initialization (Gtk.ScrolledWindow)

		SetPolicy (Gtk.PolicyType.Automatic, Gtk.PolicyType.Automatic);
		SetChild (view);

		// --- References to keep

		list_model = listModel;
		selection_model = selectionModel;

		// --- Other initialization (TODO: remove references to PintaCore)

		PintaCore.Workspace.ActiveDocumentChanged += HandleActiveDocumentChanged;
	}

	private static void HandleFactorySetup (
		Gtk.SignalListItemFactory factory,
		Gtk.SignalListItemFactory.SetupSignalArgs args)
	{
		var item = (Gtk.ListItem) args.Object;
		item.SetChild (new LayersListViewItemWidget ());
	}

	private static void HandleFactoryBind (
		Gtk.SignalListItemFactory factory,
		Gtk.SignalListItemFactory.BindSignalArgs args)
	{
		var list_item = (Gtk.ListItem) args.Object;
		var model_item = (LayersListViewItem) list_item.GetItem ()!;
		var widget = (LayersListViewItemWidget) list_item.GetChild ()!;
		widget.Update (model_item);
	}

	private void HandleSelectionChanged (
		Gtk.SelectionModel sender,
		EventArgs e)
	{
		ArgumentNullException.ThrowIfNull (active_document);

		// If changing the current layer causes a history item to be added, ensure we
		// don't end up in an infinite loop when HandleHistoryChanged updates the
		// selection (see bug #1463)
		if (changing_selection)
			return;

		try {
			changing_selection = true;

			int model_idx = (int) selection_model.Selected;
			int doc_idx = active_document.Layers.Count () - 1 - model_idx;

			if (active_document.Layers.CurrentUserLayerIndex != doc_idx) {
				active_document.Layers.SetCurrentUserLayer (doc_idx);
			}
		} finally {
			changing_selection = false;
		}
	}

	private void HandleRowActivated (
		Gtk.ListView sender,
		Gtk.ListView.ActivateSignalArgs args)
	{
		// Open the layer properties dialog
		PintaCore.Actions.Layers.Properties.Activate ();
	}

	private void HandleActiveDocumentChanged (object? sender, EventArgs e)
	{
		Document? doc =
			PintaCore.Workspace.HasOpenDocuments
			? PintaCore.Workspace.ActiveDocument
			: null;

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
		list_model.RemoveMultiple (0, list_model.GetNItems ());

		active_document = doc;
		if (doc is null)
			return;

		foreach (var layer in doc.Layers.UserLayers.Reverse ())
			list_model.Append (new LayersListViewItem (doc, layer));

		// Update our selection to match the document's active layer.
		int currentModelIndex = doc.Layers.Count () - 1 - doc.Layers.CurrentUserLayerIndex;
		selection_model.SelectItem ((uint) currentModelIndex, unselectRest: true);

		doc.History.HistoryItemAdded += HandleHistoryChanged;
		doc.History.ActionUndone += HandleHistoryChanged;
		doc.History.ActionRedone += HandleHistoryChanged;
		doc.Layers.LayerAdded += HandleLayerAdded;
		doc.Layers.LayerRemoved += HandleLayerRemoved;
		doc.Layers.SelectedLayerChanged += HandleSelectedLayerChanged;
		doc.Layers.LayerPropertyChanged += HandleLayerPropertyChanged;
	}

	private void HandleHistoryChanged (object? sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull (active_document);

		// Recreate all the widgets.
		// This update should ideally be done by changing gobject properties instead, but we don't have the ability to add custom properties yet
		uint selected_idx = selection_model.Selected;
		for (uint i = 0; i < list_model.GetNItems (); ++i) {
			int layer_idx = active_document.Layers.Count () - 1 - (int) i;
			list_model.Remove (i);
			list_model.Insert (i, new LayersListViewItem (active_document, active_document.Layers[layer_idx]));
		}

		// Restore the selection.
		selection_model.Selected = selected_idx;
	}

	private void HandleLayerAdded (object? sender, IndexEventArgs e)
	{
		ArgumentNullException.ThrowIfNull (active_document);

		int index = active_document.Layers.Count () - 1 - e.Index;
		list_model.Insert ((uint) index, new LayersListViewItem (active_document, active_document.Layers[e.Index]));
	}

	private void HandleLayerRemoved (object? sender, IndexEventArgs e)
	{
		ArgumentNullException.ThrowIfNull (active_document);

		// Note: don't need to subtract 1 because the layer has already been removed from the document.
		list_model.Remove ((uint) (active_document.Layers.Count () - e.Index));
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
