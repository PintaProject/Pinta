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
using GObject;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

// GObject subclass for use with Gio.ListStore
[Subclass<GObject.Object>]
public sealed partial class LayersListViewItem
{
	private CanvasRenderer? canvas_renderer;

	// NRT - GObject requires a parameterless constructor, and these don't have simple defaults
	private readonly Document? document;
	private readonly UserLayer? user_layer;

	public LayersListViewItem (
		Document doc,
		UserLayer userLayer
	)
		: this ()
	{
		document = doc;
		user_layer = userLayer;
	}

	public string Label => user_layer?.Name ?? string.Empty;
	public bool Visible => !user_layer?.Hidden ?? false;

	public ImageSurface BuildThumbnail (
		int widthRequest,
		int heightRequest)
	{
		if (document is null || user_layer is null)
			throw new InvalidOperationException ($"{nameof (LayersListViewItem)} is not initialized");

		// If this is not the currently selected layer, just directly use the layer's surface.
		if (user_layer != document.Layers.CurrentUserLayer || !document.Layers.ShowSelectionLayer)
			return user_layer.Surface;

		// If it is, then we may need to draw the
		// selection layer over it, like when dragging a selection.
		ImageSurface surface = CairoExtensions.CreateImageSurface (Format.Argb32, widthRequest, heightRequest);

		var layers = new Layer[]
		{
			user_layer,
			document.Layers.SelectionLayer,
		};

		canvas_renderer ??= new CanvasRenderer (enableLivePreview: false, enableBackgroundPattern: true);
		canvas_renderer.Initialize (document.ImageSize, new Size (widthRequest, heightRequest));
		canvas_renderer.Render (layers, surface, PointI.Zero);

		return surface;
	}

	public void HandleVisibilityToggled (bool visible)
	{
		if (document is null || user_layer is null)
			throw new InvalidOperationException ($"{nameof (LayersListViewItem)} is not initialized");

		if (Visible == visible)
			return;

		Document doc = PintaCore.Workspace.ActiveDocument;

		LayerProperties initial = new (user_layer.Name, visible, user_layer.Opacity, user_layer.BlendMode);
		LayerProperties updated = new (user_layer.Name, !visible, user_layer.Opacity, user_layer.BlendMode);

		UpdateLayerPropertiesHistoryItem historyItem = new (
			Resources.Icons.LayerProperties,
			visible ? Translations.GetString ("Layer Shown") : Translations.GetString ("Layer Hidden"),
			doc.Layers.IndexOf (user_layer),
			initial,
			updated);

		historyItem.Redo ();

		doc.History.PushNewItem (historyItem);
	}
}

public sealed class LayersListViewItemWidget : Gtk.Box
{
	private static readonly Pattern transparent_pattern = CairoExtensions.CreateTransparentBackgroundPattern (8);

	private LayersListViewItem? item;
	private ImageSurface? thumbnail_surface;

	private readonly Gtk.DrawingArea item_thumbnail;
	private readonly Gtk.Label item_label;
	private readonly Gtk.CheckButton visible_button;

	public LayersListViewItemWidget ()
	{
		Gtk.DrawingArea itemThumbnail = Gtk.DrawingArea.New ();
		itemThumbnail.SetDrawFunc ((area, context, width, height) => DrawThumbnail (context, width, height));
		itemThumbnail.WidthRequest = 60;
		itemThumbnail.HeightRequest = 40;

		Gtk.Label itemLabel = Gtk.Label.New (string.Empty);
		itemLabel.Halign = Gtk.Align.Start;
		itemLabel.Hexpand = true;
		itemLabel.Ellipsize = Pango.EllipsizeMode.End;

		Gtk.CheckButton visibleButton = Gtk.CheckButton.New ();
		visibleButton.Halign = Gtk.Align.End;
		visibleButton.Hexpand = false;
		visibleButton.OnToggled += (_, _) => item?.HandleVisibilityToggled (visibleButton.Active);

		// --- Initialization (Gtk.Widget)

		this.SetAllMargins (2);

		// --- Initialization (Gtk.Box)

		Spacing = 6;

		SetOrientation (Gtk.Orientation.Horizontal);

		Append (itemThumbnail);
		Append (itemLabel);
		Append (visibleButton);

		// --- References to keep

		item_thumbnail = itemThumbnail;
		item_label = itemLabel;
		visible_button = visibleButton;
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
		Context g,
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
		g.SetSourceColor (new Color (0.5, 0.5, 0.5));
		g.Rectangle (offset.X + 0.5, offset.Y + 0.5, draw_width, draw_height);
		g.LineWidth = 1;

		g.Stroke ();

		g.Dispose ();
	}
}
