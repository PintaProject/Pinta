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
using System.Linq;
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
	public UserLayer? UserLayer { get; }

	public LayersListViewItem (
		Document doc,
		UserLayer userLayer
	)
		: this ()
	{
		document = doc;
		UserLayer = userLayer;
	}

	public string Label => UserLayer?.Name ?? string.Empty;
	public bool Visible => !UserLayer?.Hidden ?? false;

	public ImageSurface BuildThumbnail (
		int widthRequest,
		int heightRequest)
	{
		if (document is null || UserLayer is null)
			throw new InvalidOperationException ($"{nameof (LayersListViewItem)} is not initialized");

		ImageSurface surface = CairoExtensions.CreateImageSurface (Format.Argb32, widthRequest, heightRequest);

		List<Layer> layers = UserLayer.GetLayersToPaint ().ToList ();
		// For the current layer, show the selection layer too (e.g. when moving the selection's contents).
		if (UserLayer == document.Layers.CurrentUserLayer && document.Layers.ShowSelectionLayer)
			layers.Add (document.Layers.SelectionLayer);

		// Directly use the layer's surface if there isn't any blending required.
		if (layers.Count == 1)
			return layers[0].Surface;

		canvas_renderer ??= new CanvasRenderer (
			PintaCore.LivePreview,
			PintaCore.Workspace,
			enableLivePreview: false,
			enableBackgroundPattern: true);
		canvas_renderer.Initialize (document.ImageSize, new Size (widthRequest, heightRequest));
		canvas_renderer.Render (layers, surface, PointI.Zero);

		return surface;
	}

	public void HandleVisibilityToggled (bool visible)
	{
		if (document is null || UserLayer is null)
			throw new InvalidOperationException ($"{nameof (LayersListViewItem)} is not initialized");

		if (Visible == visible)
			return;

		Document doc = PintaCore.Workspace.ActiveDocument;

		LayerProperties initial = new (UserLayer.Name, visible, UserLayer.Opacity, UserLayer.BlendMode);
		LayerProperties updated = new (UserLayer.Name, !visible, UserLayer.Opacity, UserLayer.BlendMode);

		UpdateLayerPropertiesHistoryItem historyItem = new (
			Resources.Icons.LayerProperties,
			visible ? Translations.GetString ("Layer Shown") : Translations.GetString ("Layer Hidden"),
			doc.Layers.IndexOf (UserLayer),
			initial,
			updated);

		historyItem.Redo ();

		doc.History.PushNewItem (historyItem);
	}

	public event EventHandler? LayerModified;

	/// <summary>
	/// Signal that the layer has been modified.
	/// In the future this should be replaced by GObject properties and bindings.
	/// </summary>
	public void NotifyLayerModified ()
	{
		LayerModified?.Invoke (this, EventArgs.Empty);
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

		Gtk.GestureClick menuGesture = Gtk.GestureClick.New ();
		menuGesture.SetButton (Gdk.Constants.BUTTON_SECONDARY);
		menuGesture.OnPressed += MenuGesture_OnPressed;

		// --- Initialization (Gtk.Widget)

		this.SetAllMargins (2);
		this.AddController (menuGesture);

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

	private void MenuGesture_OnPressed (
		Gtk.GestureClick _,
		Gtk.GestureClick.PressedSignalArgs args)
	{
		if (item is null || item.UserLayer is null || !PintaCore.Workspace.HasOpenDocuments)
			return;

		Document doc = PintaCore.Workspace.ActiveDocument;
		// Ensure this is the current layer before opening the menu, since the menu actions
		// apply to the current layer.
		if (doc.Layers.CurrentUserLayer != item.UserLayer)
			doc.Layers.SetCurrentUserLayer (item.UserLayer);

		LayerActions actions = PintaCore.Actions.Layers;

		Gio.Menu operationsSection = Gio.Menu.New ();
		operationsSection.AppendItem (actions.DeleteLayer.CreateMenuItem ());
		operationsSection.AppendItem (actions.DuplicateLayer.CreateMenuItem ());
		operationsSection.AppendItem (actions.MergeLayerDown.CreateMenuItem ());
		operationsSection.AppendItem (actions.MoveLayerUp.CreateMenuItem ());
		operationsSection.AppendItem (actions.MoveLayerDown.CreateMenuItem ());

		Gio.Menu flipSection = Gio.Menu.New ();
		flipSection.AppendItem (actions.FlipHorizontal.CreateMenuItem ());
		flipSection.AppendItem (actions.FlipVertical.CreateMenuItem ());
		flipSection.AppendItem (actions.RotateZoom.CreateMenuItem ());

		Gio.Menu propertiesSection = Gio.Menu.New ();
		propertiesSection.AppendItem (actions.Properties.CreateMenuItem ());

		Gio.Menu menu = Gio.Menu.New ();
		menu.AppendSection (null, operationsSection);
		menu.AppendSection (null, flipSection);
		menu.AppendSection (null, propertiesSection);

		Gtk.PopoverMenu popover = Gtk.PopoverMenu.NewFromModel (menu);
		popover.SetParent (this);
		popover.Popup ();
	}

	/// <summary>
	/// Bind the widget to a different LayersListViewItem.
	/// </summary>
	public void SetItem (LayersListViewItem newItem)
	{
		if (item != null)
			item.LayerModified -= OnLayerModified;

		item = newItem;
		item.LayerModified += OnLayerModified;
		UpdateFromLayer ();
	}

	/// <summary>
	/// Event handler for modifications to the item's layer.
	/// </summary>
	private void OnLayerModified (object? sender, EventArgs e)
	{
		UpdateFromLayer ();
	}

	/// <summary>
	/// Update the widget to reflect the current state of the item's layer.
	/// </summary>
	private void UpdateFromLayer ()
	{
		if (item is null)
			throw new InvalidOperationException ($"{nameof (item)} is null");

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
