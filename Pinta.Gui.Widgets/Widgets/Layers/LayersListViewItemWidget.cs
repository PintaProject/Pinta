// 
// HistoryTreeView.cs
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
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
	// GObject subclass for use with Gio.ListStore
	public class LayersListViewItem : GObject.Object
	{
		CanvasRenderer? canvas_renderer;

		private readonly Document doc;
		private readonly UserLayer layer;

		public LayersListViewItem (Document doc, UserLayer layer) : base (true, Array.Empty<GObject.ConstructArgument> ())
		{
			this.doc = doc;
			this.layer = layer;
		}

		public string Label => layer.Name;
		public bool Visible => !layer.Hidden;

		public Cairo.ImageSurface BuildThumbnail (int width_request, int height_request)
		{
			// If this is the currently selected layer, we may need to draw the
			// selection layer over it, like when dragging a selection.
			if (layer == doc.Layers.CurrentUserLayer && doc.Layers.ShowSelectionLayer) {
				var surface = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, width_request, height_request);

				canvas_renderer ??= new CanvasRenderer (false, false);
				canvas_renderer.Initialize (doc.ImageSize, new Size (width_request, height_request));

				var layers = new List<Layer> { layer, doc.Layers.SelectionLayer };
				canvas_renderer.Render (layers, surface, PointI.Zero);

				return surface;
			}

			// Otherwise just directly use the layer's surface.
			return layer.Surface;
		}

		public void HandleVisibilityToggled (bool visible)
		{
			if (Visible == visible)
				return;

			var doc = PintaCore.Workspace.ActiveDocument;

			var initial = new LayerProperties (layer.Name, visible, layer.Opacity, layer.BlendMode);
			var updated = new LayerProperties (layer.Name, !visible, layer.Opacity, layer.BlendMode);

			var historyItem = new UpdateLayerPropertiesHistoryItem (
				Resources.Icons.LayerProperties,
				visible ? Translations.GetString ("Layer Shown") : Translations.GetString ("Layer Hidden"),
				doc.Layers.IndexOf (layer),
				initial,
				updated);
			historyItem.Redo ();

			doc.History.PushNewItem (historyItem);
		}
	}

	public class LayersListViewItemWidget : Box
	{
		private static readonly Cairo.Pattern transparent_pattern = CairoExtensions.CreateTransparentBackgroundPattern (8);

		private LayersListViewItem? item;
		private Cairo.ImageSurface? thumbnail_surface;

		private readonly Gtk.DrawingArea thumbnail;
		private readonly Gtk.Label label;
		private readonly Gtk.CheckButton visible_button;

		public LayersListViewItemWidget ()
		{
			Spacing = 6;
			this.SetAllMargins (2);
			SetOrientation (Orientation.Horizontal);

			thumbnail = Gtk.DrawingArea.New ();
			thumbnail.SetDrawFunc ((area, context, width, height) => DrawThumbnail (context, width, height));
			thumbnail.WidthRequest = 60;
			thumbnail.HeightRequest = 40;
			Append (thumbnail);

			label = Gtk.Label.New (string.Empty);
			label.Halign = Align.Start;
			label.Hexpand = true;
			label.Ellipsize = Pango.EllipsizeMode.End;
			Append (label);

			visible_button = CheckButton.New ();
			visible_button.Halign = Align.End;
			visible_button.Hexpand = false;
			Append (visible_button);

			visible_button.OnToggled += (_, _) => {
				item?.HandleVisibilityToggled (visible_button.Active);
			};
		}

		// Set the widget's contents to the provided layer.
		public void Update (LayersListViewItem item)
		{
			this.item = item;

			label.SetText (item.Label);
			visible_button.SetActive (item.Visible);

			thumbnail_surface = null;
			thumbnail.QueueDraw ();
		}

		private void DrawThumbnail (Cairo.Context g, int width, int height)
		{
			ArgumentNullException.ThrowIfNull (item);

			thumbnail_surface ??= item.BuildThumbnail (width, height);

			double scale;
			var draw_width = width;
			var draw_height = height;

			// The image is more constrained by height than width
			if ((double) width / (double) thumbnail_surface.Width >= (double) height / (double) thumbnail_surface.Height) {
				scale = (double) height / (double) (thumbnail_surface.Height);
				draw_width = (int) (thumbnail_surface.Width * height / thumbnail_surface.Height);
			} else {
				scale = (double) width / (double) (thumbnail_surface.Width);
				draw_height = (int) (thumbnail_surface.Height * width / thumbnail_surface.Width);
			}

			var offset_x = (int) ((width - draw_width) / 2f);
			var offset_y = (int) ((height - draw_height) / 2f);

			g.Save ();
			g.Rectangle (offset_x, offset_y, draw_width, draw_height);
			g.Clip ();

			g.SetSource (transparent_pattern);
			g.Paint ();

			g.Scale (scale, scale);
			g.SetSourceSurface (thumbnail_surface, (int) (offset_x / scale), (int) (offset_y / scale));
			g.Paint ();

			g.Restore ();

			// TODO: scale this box correctly to match layer aspect ratio
			g.SetSourceColor (new Cairo.Color (0.5, 0.5, 0.5));
			g.Rectangle (offset_x + 0.5, offset_y + 0.5, draw_width, draw_height);
			g.LineWidth = 1;
			g.Stroke ();
		}
	}
}
