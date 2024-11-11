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
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

// GObject subclass for use with Gio.ListStore
public sealed class LayersListViewItem : GObject.Object
{
	private CanvasRenderer? canvas_renderer;

	private readonly Document doc;
	private readonly UserLayer layer;

	public LayersListViewItem (
		Document doc,
		UserLayer layer
	)
		: base (true, Array.Empty<GObject.ConstructArgument> ())
	{
		this.doc = doc;
		this.layer = layer;
	}

	public string Label => layer.Name;
	public bool Visible => !layer.Hidden;

	public Cairo.ImageSurface BuildThumbnail (
		int widthRequest,
		int heightRequest)
	{
		// If this is not the currently selected layer, just directly use the layer's surface.
		if (layer != doc.Layers.CurrentUserLayer || !doc.Layers.ShowSelectionLayer)
			return layer.Surface;

		// It it is, then we may need to draw the
		// selection layer over it, like when dragging a selection.
		ImageSurface surface = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, widthRequest, heightRequest);

		var layers = new Layer[]
		{
			layer,
			doc.Layers.SelectionLayer,
		};

		canvas_renderer ??= new CanvasRenderer (null, false);
		canvas_renderer.Initialize (doc.ImageSize, new Size (widthRequest, heightRequest));
		canvas_renderer.Render (layers, surface, PointI.Zero);

		return surface;
	}

	public void HandleVisibilityToggled (bool visible)
	{
		if (Visible == visible)
			return;

		Document doc = PintaCore.Workspace.ActiveDocument;

		LayerProperties initial = new (layer.Name, visible, layer.Opacity, layer.BlendMode);
		LayerProperties updated = new (layer.Name, !visible, layer.Opacity, layer.BlendMode);

		UpdateLayerPropertiesHistoryItem historyItem = new (
			Resources.Icons.LayerProperties,
			visible ? Translations.GetString ("Layer Shown") : Translations.GetString ("Layer Hidden"),
			doc.Layers.IndexOf (layer),
			initial,
			updated);

		historyItem.Redo ();

		doc.History.PushNewItem (historyItem);
	}
}

public sealed class LayersListViewItemWidget : Gtk.Box
{
	private static readonly Cairo.Pattern transparent_pattern = CairoExtensions.CreateTransparentBackgroundPattern (8);

	private LayersListViewItem? item;
	private Cairo.ImageSurface? thumbnail_surface;

	private readonly Gtk.DrawingArea item_thumbnail;
	private readonly Gtk.Label item_label;
	private readonly Gtk.CheckButton visible_button;

	public LayersListViewItemWidget ()
	{
		Spacing = 6;

		this.SetAllMargins (2);

		SetOrientation (Gtk.Orientation.Horizontal);

		item_thumbnail = CreateItemThumbnail ();
		item_label = CreateItemLabel ();
		visible_button = CreateVisibleButton ();

		Append (item_thumbnail);
		Append (item_label);
		Append (visible_button);
	}

	private Gtk.CheckButton CreateVisibleButton ()
	{
		Gtk.CheckButton result = Gtk.CheckButton.New ();
		result.Halign = Gtk.Align.End;
		result.Hexpand = false;
		result.OnToggled += (_, _) => item?.HandleVisibilityToggled (visible_button.Active);
		return result;
	}

	private static Gtk.Label CreateItemLabel ()
	{
		Gtk.Label result = Gtk.Label.New (string.Empty);
		result.Halign = Gtk.Align.Start;
		result.Hexpand = true;
		result.Ellipsize = Pango.EllipsizeMode.End;
		return result;
	}

	private Gtk.DrawingArea CreateItemThumbnail ()
	{
		Gtk.DrawingArea result = Gtk.DrawingArea.New ();
		result.SetDrawFunc ((area, context, width, height) => DrawThumbnail (context, width, height));
		result.WidthRequest = 60;
		result.HeightRequest = 40;
		return result;
	}

	// Set the widget's contents to the provided layer.
	public void Update (LayersListViewItem item)
	{
		this.item = item;

		item_label.SetText (item.Label);
		visible_button.SetActive (item.Visible);

		thumbnail_surface = null;
		item_thumbnail.QueueDraw ();
	}

	private void DrawThumbnail (
		Cairo.Context g,
		int width,
		int height)
	{
		if (item is null)
			throw new InvalidOperationException ($"{nameof (item)} is null");

		thumbnail_surface ??= item.BuildThumbnail (width, height);

		double scale;
		int draw_width;
		int draw_height;

		// The image is more constrained by height than width
		if (width / (double) thumbnail_surface.Width >= height / (double) thumbnail_surface.Height) {
			scale = height / (double) (thumbnail_surface.Height);
			draw_width = thumbnail_surface.Width * height / thumbnail_surface.Height;
			draw_height = height;
		} else {
			scale = width / (double) (thumbnail_surface.Width);
			draw_width = width;
			draw_height = thumbnail_surface.Height * width / thumbnail_surface.Width;
		}

		PointI offset = new (
			X: (int) ((width - draw_width) / 2f),
			Y: (int) ((height - draw_height) / 2f)
		);

		g.Save ();

		g.Rectangle (offset.X, offset.Y, draw_width, draw_height);
		g.Clip ();

		g.SetSource (transparent_pattern);
		g.Paint ();

		g.Scale (scale, scale);
		g.SetSourceSurface (thumbnail_surface, (int) (offset.X / scale), (int) (offset.Y / scale));
		g.Paint ();

		g.Restore ();

		// TODO: scale this box correctly to match layer aspect ratio
		g.SetSourceColor (new Cairo.Color (0.5, 0.5, 0.5));
		g.Rectangle (offset.X + 0.5, offset.Y + 0.5, draw_width, draw_height);
		g.LineWidth = 1;

		g.Stroke ();

		g.Dispose ();
	}
}
