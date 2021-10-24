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
	public class LayersListWidget : ScrolledWindow
	{
		private TreeView tree;
		private TreeStore store;
		private Document? active_document;
		private bool updating_model;

		// For the active layer, we also draw the selection layer on top of it,
		// so we can't directly use that layer's surface.
		private Cairo.ImageSurface? active_layer_surface;
		private readonly CanvasRenderer canvas_renderer = new CanvasRenderer (false);

		private const int store_index_thumbnail = 0;
		private const int store_index_name = 1;
		private const int store_index_visibility = 2;
		private const int store_index_layer = 3;

		private const int thumbnail_width = 60;
		private const int thumbnail_height = 40;
		private const int thumbnail_column_width = 70;

		private const int name_column_min_width = 100;
		private const int name_column_max_width = 300;

		private const int visibility_column_width = 30;

		public LayersListWidget ()
		{
			Build ();

			PintaCore.Workspace.ActiveDocumentChanged += Workspace_ActiveDocumentChanged;

			tree.CursorChanged += HandleLayerSelected;
		}

		private UserLayer? GetSelectedLayerInTreeView () => tree.GetSelectedValueAt<UserLayer> (store_index_layer);

		private void SelectLayerInTreeView (int layerIndex) => tree.SetSelectedRows (layerIndex);

		[MemberNotNull (nameof (tree), nameof (store))]
		private void Build ()
		{
			CanFocus = false;
			SetSizeRequest (200, 200);

			SetPolicy (PolicyType.Automatic, PolicyType.Automatic);

			// Create tree
			tree = new TreeView {
				HeadersVisible = false,
				FixedHeightMode = true,
				Reorderable = false,
				EnableGridLines = TreeViewGridLines.None,
				EnableTreeLines = false,
				ShowExpanders = false,
				CanFocus = false
			};

			// Create Thumbnail column
			var crs = new CellRendererSurface (thumbnail_width, thumbnail_height);

			var col = new TreeViewColumn ("Thumbnail", crs, "surface", store_index_thumbnail) {
				Sizing = TreeViewColumnSizing.Fixed,
				FixedWidth = thumbnail_column_width
			};

			tree.AppendColumn (col);

			// Create text column
			var textCell = new CellRendererText {
				Ellipsize = Pango.EllipsizeMode.End
			};

			col = new TreeViewColumn ("Name", textCell, "text", store_index_name) {
				Sizing = TreeViewColumnSizing.Fixed,
				Expand = true,
				MinWidth = name_column_min_width,
				MaxWidth = name_column_max_width
			};

			tree.AppendColumn (col);

			// Create visible checkbox toggle
			var crt = new CellRendererToggle {
				Activatable = true
			};

			crt.Toggled += LayerVisibilityToggled;

			col = new TreeViewColumn ("Visible", crt, "active", store_index_visibility) {
				Sizing = TreeViewColumnSizing.Fixed,
				FixedWidth = visibility_column_width
			};

			tree.AppendColumn (col);

			store = new TreeStore (typeof (Cairo.ImageSurface), typeof (string), typeof (bool), typeof (Layer));

			tree.Model = store;
			tree.RowActivated += HandleRowActivated;

			Add (tree);

			ShowAll ();
		}

		private void HandleLayerSelected (object? o, EventArgs e)
		{
			// Prevent triggered when closing the app which causes a crash
			if (updating_model || !PintaCore.Workspace.HasOpenDocuments)
				return;

			updating_model = true;

			var doc = PintaCore.Workspace.ActiveDocument;
			var layer = GetSelectedLayerInTreeView ();

			if (doc.Layers.CurrentUserLayer != layer && layer != null)
				doc.Layers.SetCurrentUserLayer (layer);

			updating_model = false;
		}

		private void LayerVisibilityToggled (object? o, ToggledArgs args)
		{
			if (updating_model)
				return;

			updating_model = true;

			var visible = tree.GetValueAt<object> (args.Path, store_index_visibility);
			var layer = tree.GetValueAt<UserLayer> (args.Path, store_index_layer);

			if (visible is bool b && layer is not null)
				SetLayerVisibility (layer, !b);

			updating_model = false;
		}

		private void HandleHistoryItemAdded (object? sender, EventArgs e)
		{
			// TODO: Handle this more efficiently.
			Reset ();
		}

		private void HandleSelectedLayerChanged (object? sender, EventArgs e)
		{
			// TODO: Handle this more efficiently.
			Reset ();
		}

		void HandlePintaCoreLayersLayerPropertyChanged (object? sender, PropertyChangedEventArgs e)
		{
			// TODO: Handle this more efficiently.
			Reset ();
		}

		private void HandleLayerAddedOrRemoved (object? sender, EventArgs e)
		{
			// TODO: Handle this more efficiently.
			Reset ();

			// TODO: this should be handled elsewhere
			PintaCore.Workspace.Invalidate ();
		}

		private void HandleRowActivated (object? o, RowActivatedArgs args)
		{
			// The double click to activate will have already selected the layer.
			PintaCore.Actions.Layers.Properties.Activate ();
		}

		private void Workspace_ActiveDocumentChanged (object? sender, EventArgs e)
		{
			var doc = PintaCore.Workspace.HasOpenDocuments ? PintaCore.Workspace.ActiveDocument : null;

			if (active_document == doc)
				return;

			if (active_document != null) {
				active_document.History.HistoryItemAdded -= HandleHistoryItemAdded;
				active_document.History.ActionUndone -= HandleHistoryItemAdded;
				active_document.History.ActionRedone -= HandleHistoryItemAdded;
				active_document.Layers.LayerAdded -= HandleLayerAddedOrRemoved;
				active_document.Layers.LayerRemoved -= HandleLayerAddedOrRemoved;
				active_document.Layers.SelectedLayerChanged -= HandleSelectedLayerChanged;
				active_document.Layers.LayerPropertyChanged -= HandlePintaCoreLayersLayerPropertyChanged;
			}

			if (doc is not null) {
				doc.History.HistoryItemAdded += HandleHistoryItemAdded;
				doc.History.ActionUndone += HandleHistoryItemAdded;
				doc.History.ActionRedone += HandleHistoryItemAdded;
				doc.Layers.LayerAdded += HandleLayerAddedOrRemoved;
				doc.Layers.LayerRemoved += HandleLayerAddedOrRemoved;
				doc.Layers.SelectedLayerChanged += HandleSelectedLayerChanged;
				doc.Layers.LayerPropertyChanged += HandlePintaCoreLayersLayerPropertyChanged;
			}

			active_document = doc;

			Reset ();
		}

		private void Reset ()
		{
			store.Clear ();

			if (active_layer_surface != null) {
				(active_layer_surface as IDisposable).Dispose ();
				active_layer_surface = null;
			}

			if (!PintaCore.Workspace.HasOpenDocuments)
				return;

			var doc = PintaCore.Workspace.ActiveDocument;

			foreach (var layer in doc.Layers.UserLayers.Reverse ()) {
				var surf = layer.Surface;

				// If this is the currently selected layer, we may need to draw the
				// selection layer over it, like when dragging a selection.
				if (layer == doc.Layers.CurrentUserLayer && doc.Layers.ShowSelectionLayer) {
					active_layer_surface = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, thumbnail_width, thumbnail_height);
					canvas_renderer.Initialize (doc.ImageSize, new Gdk.Size (thumbnail_width, thumbnail_height));

					var layers = new List<Layer> { layer, doc.Layers.SelectionLayer };
					canvas_renderer.Render (layers, active_layer_surface, Gdk.Point.Zero);

					surf = active_layer_surface;
				}

				store.AppendValues (surf, layer.Name, !layer.Hidden, layer);
			}

			SelectLayerInTreeView (doc.Layers.Count () - doc.Layers.CurrentUserLayerIndex - 1);
		}

		private void SetLayerVisibility (UserLayer layer, bool visibility)
		{
			var doc = PintaCore.Workspace.ActiveDocument;

			var initial = new LayerProperties (layer.Name, visibility, layer.Opacity, layer.BlendMode);
			var updated = new LayerProperties (layer.Name, !visibility, layer.Opacity, layer.BlendMode);

			var historyItem = new UpdateLayerPropertiesHistoryItem (
				Resources.Icons.LayerProperties,
				(visibility) ? Translations.GetString ("Layer Shown") : Translations.GetString ("Layer Hidden"),
				doc.Layers.IndexOf (layer),
				initial,
				updated);
			historyItem.Redo ();

			doc.History.PushNewItem (historyItem);
		}
	}
}
