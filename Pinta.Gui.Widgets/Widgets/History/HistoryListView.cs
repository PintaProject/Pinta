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
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class HistoryListView : Gtk.ScrolledWindow
{
	private readonly Gio.ListStore model;
	private readonly Gtk.SingleSelection selection_model;

	private Document? active_document;

	public HistoryListView ()
	{
		Gio.ListStore listModel = Gio.ListStore.New (HistoryListViewItem.GetGType ());

		Gtk.SingleSelection selectionModel = Gtk.SingleSelection.New (listModel);
		selectionModel.OnSelectionChanged += HandleSelectionChanged;

		Gtk.SignalListItemFactory signalFactory = Gtk.SignalListItemFactory.New ();
		signalFactory.OnSetup += (factory, args) => {
			var item = (Gtk.ListItem) args.Object;
			item.SetChild (new HistoryItemWidget ());
		};
		signalFactory.OnBind += (factory, args) => {
			var list_item = (Gtk.ListItem) args.Object;
			var model_item = (HistoryListViewItem) list_item.GetItem ()!;
			var widget = (HistoryItemWidget) list_item.GetChild ()!;
			widget.Update (model_item);
		};

		Gtk.ListView selectionView = Gtk.ListView.New (selectionModel, signalFactory);
		selectionView.CanFocus = false;

		// --- Initialization (Gtk.Widget)

		CanFocus = false;
		SetSizeRequest (200, 200);

		// --- Initialization (Gtk.ScrolledWindow)

		SetPolicy (Gtk.PolicyType.Automatic, Gtk.PolicyType.Automatic);
		SetChild (selectionView);

		// --- References to keep

		model = listModel;
		selection_model = selectionModel;

		// --- Event handlers for the application
		// TODO: Move handlers out of this constructor

		PintaCore.Workspace.ActiveDocumentChanged += OnActiveDocumentChanged;
	}

	private void HandleSelectionChanged (Gtk.SelectionModel sender, EventArgs e)
	{
		ArgumentNullException.ThrowIfNull (active_document);

		int index = (int) selection_model.Selected;

		while (active_document.History.Pointer < index)
			active_document.History.Redo ();

		while (active_document.History.Pointer > index)
			active_document.History.Undo ();
	}

	private void OnActiveDocumentChanged (object? sender, EventArgs e)
	{
		var doc =
			PintaCore.Workspace.HasOpenDocuments
			? PintaCore.Workspace.ActiveDocument
			: null;

		if (active_document == doc)
			return;

		if (active_document is not null) {
			active_document.History.HistoryItemAdded -= OnHistoryItemAdded;
			active_document.History.ActionUndone -= OnUndoOrRedo;
			active_document.History.ActionRedone -= OnUndoOrRedo;
		}

		// Clear out old items and rebuild.
		model.RemoveMultiple (0, model.GetNItems ());

		active_document = doc;

		if (doc is null)
			return;

		foreach (BaseHistoryItem item in doc.History.Items)
			model.Append (new HistoryListViewItem (item));

		// Move selection to the document's current history item.
		if (model.NItems > 0)
			selection_model.SetSelected ((uint) doc.History.Pointer);

		doc.History.HistoryItemAdded += OnHistoryItemAdded;
		doc.History.ActionUndone += OnUndoOrRedo;
		doc.History.ActionRedone += OnUndoOrRedo;
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
