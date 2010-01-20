// 
// LayersListWidget.cs
//  
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
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

namespace Pinta
{
	// TODO: This needs to be completely redone properly
	// ie: more than 3 layers, scrolling, etc.
	// is probably supposed to be a treeview?
	[System.ComponentModel.ToolboxItem (true)]
	public class LayersListWidget : Gtk.DrawingArea
	{
		private int thumb_width = 56;
		private int thumb_height = 42;
		
		private Cairo.Rectangle layer1_bounds = new Cairo.Rectangle (0, 2, 175, 47);
		private Cairo.Rectangle layer2_bounds = new Cairo.Rectangle (0, 49, 175, 47);
		private Cairo.Rectangle layer3_bounds = new Cairo.Rectangle (0, 96, 175, 47);

		private Cairo.Rectangle layer1_toggle = new Cairo.Rectangle (155, 8, 16, 16);
		private Cairo.Rectangle layer2_toggle = new Cairo.Rectangle (155, 55, 16, 16);
		private Cairo.Rectangle layer3_toggle = new Cairo.Rectangle (155, 102, 16, 16);
		
		private Gdk.Pixbuf layer_visible;
		private Gdk.Pixbuf layer_invisible;
		
		private Cairo.Surface transparent;
		
		public LayersListWidget ()
		{
			this.AddEvents ((int)Gdk.EventMask.ButtonPressMask);
			
			// Insert initialization code here.
			layer_visible = PintaCore.Resources.GetIcon ("LayersWidget.Visible.png");
			layer_invisible = PintaCore.Resources.GetIcon ("LayersWidget.Hidden.png");
			
			transparent = new Cairo.ImageSurface (Cairo.Format.ARGB32, thumb_width, thumb_height);
			Cairo.Color gray = new Cairo.Color (.75, .75, .75);
			
			// Create checkerboard background	
			int grid_width = 4;
			
			using (Cairo.Context g = new Cairo.Context (transparent)) {
				g.Color = new Cairo.Color (1, 1, 1);
				g.Paint ();
				
				for (int y = 0; y < thumb_height; y += grid_width)		
					for (int x = 0; x < thumb_width; x += grid_width) {
						if ((x / grid_width % 2) + (y / grid_width % 2) == 1)
							g.FillRectangle (new Cairo.Rectangle (x, y, grid_width, grid_width), gray);
				}
			}	
			
			PintaCore.Layers.LayerAdded += HandlePintaCoreLayersLayerAddedOrRemoved;
			PintaCore.Layers.LayerRemoved += HandlePintaCoreLayersLayerAddedOrRemoved;
			PintaCore.Layers.SelectedLayerChanged += HandlePintaCoreLayersLayerAddedOrRemoved;
			PintaCore.History.HistoryItemAdded += HandlePintaCoreHistoryHistoryItemAdded;
			PintaCore.History.ActionRedone += HandlePintaCoreHistoryHistoryItemAdded;
			PintaCore.History.ActionUndone += HandlePintaCoreHistoryHistoryItemAdded;
		}

		private void HandlePintaCoreHistoryHistoryItemAdded (object sender, EventArgs e)
		{
			this.GdkWindow.Invalidate ();
		}

		private void HandlePintaCoreLayersLayerAddedOrRemoved (object sender, EventArgs e)
		{
			this.GdkWindow.Invalidate ();

			// TODO: this should be handled elsewhere
			PintaCore.Workspace.Invalidate ();
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			Layer toggled = GetClickedLayerVisibleToggle (ev.X, ev.Y);
			Layer clicked = GetClickedLayer (ev.X, ev.Y);
			
			if (toggled != null)
				toggled.Hidden = !toggled.Hidden;
			else if (clicked != null)
				PintaCore.Layers.SetCurrentLayer (clicked);
			
			GdkWindow.Invalidate ();
			PintaCore.Workspace.Invalidate ();
			
			// Insert button press handling code here.
			return base.OnButtonPressEvent (ev);
		}

		private Layer GetClickedLayer (double x, double y)
		{
			int count = PintaCore.Layers.Count;
			
			if (count == 1) {
				if (layer1_bounds.ContainsPoint (x, y))
					return PintaCore.Layers[0];
			} else if (count == 2) {
				if (layer1_bounds.ContainsPoint (x, y))
					return PintaCore.Layers[1]; 
				else if (layer2_bounds.ContainsPoint (x, y))
					return PintaCore.Layers[0];
			} else {
				if (layer1_bounds.ContainsPoint (x, y))
					return PintaCore.Layers[2]; 
				else if (layer2_bounds.ContainsPoint (x, y))
					return PintaCore.Layers[1]; 
				else if (layer3_bounds.ContainsPoint (x, y))
					return PintaCore.Layers[0];
			}
			
			return null;
		}

		private Layer GetClickedLayerVisibleToggle (double x, double y)
		{
			int count = PintaCore.Layers.Count;
			
			if (count == 1) {
				if (layer1_toggle.ContainsPoint (x, y))
					return PintaCore.Layers[0];
			} else if (count == 2) {
				if (layer1_toggle.ContainsPoint (x, y))
					return PintaCore.Layers[1]; 
				else if (layer2_toggle.ContainsPoint (x, y))
					return PintaCore.Layers[0];
			} else {
				if (layer1_toggle.ContainsPoint (x, y))
					return PintaCore.Layers[2]; 
				else if (layer2_toggle.ContainsPoint (x, y))
					return PintaCore.Layers[1]; 
				else if (layer3_toggle.ContainsPoint (x, y))
					return PintaCore.Layers[0];
			}
			
			return null;
		}		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);
			
			
			int y = 6;
			
			foreach (var item in PintaCore.Layers.Reverse ()) {
				DrawLayer (item, 5, y, item.Name, item == PintaCore.Layers.CurrentLayer);
				y += 47;
			}
			
			// Insert drawing code here.
			return true;
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			// Insert layout code here.
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			// Calculate desired size here.
			requisition.Height = 145;
			requisition.Width = 175;
		}
		
		private void DrawLayer (Layer l, int x, int y, string name, bool selected)
		{
			using (Cairo.Context g = Gdk.CairoHelper.Create (GdkWindow)) {
			
			if (selected) {
				g.FillRectangle (new Cairo.Rectangle (0, y - 3, 300, 47), new Cairo.Color (0, 0, 1));

			}
			
			g.Save ();
			g.Translate (x, y);
			g.SetSource (transparent);
			g.Paint ();

			g.Scale (56 / PintaCore.Workspace.ImageSize.X, 42 / PintaCore.Workspace.ImageSize.Y);
			
			g.SetSource (l.Surface);
			g.Paint ();
			g.Restore ();
			
			g.DrawRectangle (new Cairo.Rectangle (x, y, 56, 42), new Cairo.Color (0, 0, 0), 1);
			g.MoveTo (x + 70, y + 26);
			g.TextPath (name);
			g.Fill ();
			
			if (!l.Hidden)
				Gdk.CairoHelper.SetSourcePixbuf (g, layer_visible, 155, y + 3);
			else
				Gdk.CairoHelper.SetSourcePixbuf (g, layer_invisible, 155, y + 3);
			
			g.Paint ();
			
			}
			
		}
	}
}
