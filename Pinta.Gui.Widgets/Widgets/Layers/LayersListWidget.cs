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
using System.Linq;
using Gtk;
using Mono.Unix;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	[System.ComponentModel.ToolboxItem (true)]
	public class LayersListWidget : ScrolledWindow
	{
		private TreeView tree;
		private TreeStore store;

		// For the active layer, we also draw the selection layer on top of it,
		// so we can't directly use that layer's surface.
		private Cairo.ImageSurface active_layer_surface;
		private CanvasRenderer canvas_renderer = new CanvasRenderer (false);
				
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
			CanFocus = false;
			SetSizeRequest (200, 200);
			
			SetPolicy (PolicyType.Automatic, PolicyType.Automatic);
			
			tree = new TreeView ();
			
			tree.HeadersVisible = false;
			tree.FixedHeightMode = true;
			tree.Reorderable = false;
			tree.EnableGridLines = TreeViewGridLines.None;
			tree.EnableTreeLines = false;
			tree.ShowExpanders = false;
			tree.CanFocus = false;
			
			var crs = new CellRendererSurface (thumbnail_width, thumbnail_height);
			var col = new TreeViewColumn ("Thumbnail", crs, "surface", store_index_thumbnail);
			col.Sizing = TreeViewColumnSizing.Fixed;
			col.FixedWidth = thumbnail_column_width;
			tree.AppendColumn (col);

			var textCell = new CellRendererText ();
			textCell.Ellipsize = Pango.EllipsizeMode.End;
			col = new TreeViewColumn ("Name", textCell, "text", store_index_name);
			col.Sizing = TreeViewColumnSizing.Fixed;			
			col.Expand = true;
			col.MinWidth = name_column_min_width;
			col.MaxWidth = name_column_max_width;
			tree.AppendColumn (col);
			
			var crt = new CellRendererToggle ();
			crt.Activatable = true;
			crt.Toggled += LayerVisibilityToggled;
			
			col = new TreeViewColumn ("Visible", crt, "active", store_index_visibility);
			col.Sizing = TreeViewColumnSizing.Fixed;
			col.FixedWidth = visibility_column_width;
			tree.AppendColumn (col);
			
			store = new TreeStore (typeof (Cairo.ImageSurface), typeof (string), typeof (bool), typeof (Layer));
			
			tree.Model = store;
			tree.RowActivated += HandleRowActivated;
			
			Add (tree);
			
			PintaCore.Layers.LayerAdded += HandleLayerAddedOrRemoved;
			PintaCore.Layers.LayerRemoved += HandleLayerAddedOrRemoved;
			PintaCore.Layers.SelectedLayerChanged += HandleSelectedLayerChanged;
			PintaCore.Layers.LayerPropertyChanged += HandlePintaCoreLayersLayerPropertyChanged;
			
			PintaCore.History.HistoryItemAdded += HandleHistoryItemAdded;
			PintaCore.History.ActionRedone += HandleHistoryItemAdded;
			PintaCore.History.ActionUndone += HandleHistoryItemAdded;			
			
			tree.CursorChanged += HandleLayerSelected;


			ShowAll ();
		}

		private UserLayer GetSelectedLayerInTreeView()
		{
			UserLayer layer = null;
			TreeIter iter;
			
			var paths = tree.Selection.GetSelectedRows ();
				
			if (paths != null && paths.Length > 0 && store.GetIter (out iter, paths[0])) {
				layer = store.GetValue(iter, store_index_layer) as UserLayer;
			}
			
			return layer;
		}
		
		private void SelectLayerInTreeView (int layerIndex)
		{									
			var path = new TreePath (new int[] { layerIndex });
			tree.Selection.SelectPath (path);
		}
		
		private void HandleLayerSelected (object o, EventArgs e)
		{			
			var layer = GetSelectedLayerInTreeView ();			
			if (PintaCore.Layers.CurrentLayer != layer)
				PintaCore.Layers.SetCurrentLayer (GetSelectedLayerInTreeView ());
		}
		
		private void LayerVisibilityToggled (object o, ToggledArgs args)
		{
			TreeIter iter;		
			if (store.GetIter (out iter, new TreePath (args.Path))) {
				bool b = (bool) store.GetValue (iter, store_index_visibility);				
				store.SetValue(iter, store_index_visibility, !b);

				var layer = (UserLayer)store.GetValue(iter, store_index_layer);
				SetLayerVisibility (layer, !b);
			}
		}
		
		private void HandleHistoryItemAdded (object sender, EventArgs e)
		{	
			// TODO: Handle this more efficiently.
			Reset ();
		}
		
		private void HandleSelectedLayerChanged (object sender, EventArgs e)
		{
			// TODO: Handle this more efficiently.
			Reset ();
		}		

		void HandlePintaCoreLayersLayerPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			// TODO: Handle this more efficiently.
			Reset ();
		}		
		
		private void HandleLayerAddedOrRemoved(object sender, EventArgs e)
		{
			// TODO: Handle this more efficiently.
			Reset ();
			
			// TODO: this should be handled elsewhere
			PintaCore.Workspace.Invalidate ();
		}
		
		private void HandleRowActivated(object o, RowActivatedArgs args)
		{
			// The double click to activate will have already selected the layer.
			PintaCore.Actions.Layers.Properties.Activate ();
		}
		
		public void Reset ()
		{
			store.Clear ();

			if (active_layer_surface != null) {
				(active_layer_surface as IDisposable).Dispose ();
				active_layer_surface = null;
			}

			if (!PintaCore.Workspace.HasOpenDocuments)
				return;

			var doc = PintaCore.Workspace.ActiveDocument;
				
			foreach (var layer in (doc.UserLayers as IEnumerable<Layer>).Reverse ()) {
				var surf = layer.Surface;

				// Draw the selection layer on top of the active layer.
				if (layer == doc.CurrentUserLayer && doc.ShowSelectionLayer) {
					active_layer_surface = new Cairo.ImageSurface (Cairo.Format.Argb32, thumbnail_width,
					                                               thumbnail_height);
					canvas_renderer.Initialize (doc.ImageSize,
					                            new Gdk.Size (thumbnail_width, thumbnail_height));

					var layers = new List<Layer> { layer, doc.SelectionLayer };
					canvas_renderer.Render (layers, active_layer_surface, Gdk.Point.Zero);

					surf = active_layer_surface;
				}

				store.AppendValues (surf, layer.Name, !layer.Hidden, layer);
			}
						
			SelectLayerInTreeView (PintaCore.Layers.Count - PintaCore.Layers.CurrentLayerIndex - 1);
		}

		private void SetLayerVisibility(UserLayer layer, bool visibility)
		{
			if (layer != null)
				layer.Hidden = !visibility;
			
			var initial = new LayerProperties(layer.Name, visibility, layer.Opacity, layer.BlendMode);
			var updated = new LayerProperties(layer.Name, !visibility, layer.Opacity, layer.BlendMode);

			var historyItem = new UpdateLayerPropertiesHistoryItem (
				"Menu.Layers.LayerProperties.png",
				(visibility) ? Catalog.GetString ("Layer Shown") : Catalog.GetString ("Layer Hidden"),
				PintaCore.Layers.IndexOf (layer),
				initial,
				updated);
			
			PintaCore.History.PushNewItem (historyItem);
			
			//TODO Call this automatically when the layer visibility changes.
			PintaCore.Workspace.Invalidate ();
		}
	}
}
