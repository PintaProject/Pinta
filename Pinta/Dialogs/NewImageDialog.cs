//
// NewImageDialog.cs
//
// Author:
//       Jonathan Pobst <monkey@jpobst.com>
//
// Copyright (c) 2015 Jonathan Pobst
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
using Pinta.Core;

namespace Pinta;

public sealed class NewImageDialog : Gtk.Dialog
{
	private readonly bool has_clipboard;
	private bool suppress_events;
	private readonly PaletteManager palette;
	private readonly Size clipboard_size;

	private readonly PreviewArea preview_box;

	private readonly Gtk.StringList preset_dropdown_model;
	private readonly Gtk.DropDown preset_dropdown;
	private readonly Gtk.Entry width_entry;
	private readonly Gtk.Entry height_entry;

	private readonly Gtk.CheckButton portrait_radio;
	private readonly Gtk.CheckButton landscape_radio;

	private readonly Gtk.CheckButton white_background_radio;
	private readonly Gtk.CheckButton secondary_background_radio;
	private readonly Gtk.CheckButton transparent_background_radio;

	/// <summary>
	/// Configures and builds a NewImageDialog object.
	/// </summary>
	/// <param name="imgWidth">Initial value of the width entry.</param>
	/// <param name="imgHeight">Initial value of the height entry.</param>
	/// <param name="isClipboardSize">Indicates if there is an image on the clipboard (and the size parameters represent the clipboard image size).</param>
	public NewImageDialog (
		ChromeManager chrome,
		PaletteManager palette,
		Size initialSize,
		BackgroundType initialBackgroundType,
		bool isClipboardSize)
	{
		// --- Control creation

		// We don't show the background color option if it's the same as "White"
		bool allowBackgroundColor = palette.SecondaryColor.ToColorBgra () != ColorBgra.White;

		bool hasClipboard = isClipboardSize;

		Gtk.Label sizeLabel = CreateSizeLabel ();

		Gtk.StringList presetDropdownModel = Gtk.StringList.New ([.. GeneratePresetEntries (hasClipboard)]);
		Gtk.DropDown presetDropdown = Gtk.DropDown.New (presetDropdownModel, expression: null);

		Gtk.Label widthLabel = CreateWidthLabel ();
		Gtk.Entry widthEntry = CreateLengthEntry ();
		Gtk.Label widthUnitsLabel = CreateUnitsLabel ();
		Gtk.Box widthHbox = GtkExtensions.BoxHorizontal ([
			widthEntry,
			widthUnitsLabel]);

		Gtk.Label heightLabel = CreateHeightLabel ();
		Gtk.Entry heightEntry = CreateLengthEntry ();
		Gtk.Label heightUnitsLabel = CreateUnitsLabel ();
		Gtk.Box heightHbox = GtkExtensions.BoxHorizontal ([
			heightEntry,
			heightUnitsLabel]);

		// Orientation Radio options
		Gtk.Label orientationLabel = CreateOrientationLabel ();

		Gtk.CheckButton portraitRadio = CreatePortraitRadio ();
		Gtk.Image portraitImage = CreateOrientationIcon (Resources.Icons.OrientationPortrait);
		Gtk.Box portraitHbox = GtkExtensions.BoxHorizontal ([
			portraitImage,
			portraitRadio]);

		Gtk.CheckButton landscapeRadio = CreateLandscapeRadio (portraitRadio);
		Gtk.Image landscapeImage = CreateOrientationIcon (Resources.Icons.OrientationLandscape);

		Gtk.Box landscapeHbox = GtkExtensions.BoxHorizontal ([
			landscapeImage,
			landscapeRadio]);

		Gtk.Box orientationVbox = GtkExtensions.BoxVertical ([
			orientationLabel,
			portraitHbox,
			landscapeHbox]);

		// Background Color options
		Gtk.Label backgroundLabel = CreateBackgroundLabel ();

		Gtk.CheckButton whiteBackgroundRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("White"));

		Gtk.Image imageWhite = Gtk.Image.NewFromPixbuf (CairoExtensions.CreateColorSwatch (16, new Cairo.Color (1, 1, 1)).ToPixbuf ());
		imageWhite.MarginEnd = 7;

		Gtk.Box hboxWhite = GtkExtensions.BoxHorizontal ([
			imageWhite,
			whiteBackgroundRadio]);

		Gtk.CheckButton secondaryBackgroundRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Background Color"));
		secondaryBackgroundRadio.SetGroup (whiteBackgroundRadio);

		Gtk.Image imageBackground = Gtk.Image.NewFromPixbuf (CairoExtensions.CreateColorSwatch (16, palette.SecondaryColor).ToPixbuf ());
		imageBackground.MarginEnd = 7;

		Gtk.Box hboxBackground = GtkExtensions.BoxHorizontal ([
			imageBackground,
			secondaryBackgroundRadio]);

		Gtk.CheckButton transparentBackgroundRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Transparent"));
		transparentBackgroundRadio.SetGroup (secondaryBackgroundRadio);

		Gtk.Image imageTransparent = Gtk.Image.NewFromPixbuf (CairoExtensions.CreateTransparentColorSwatch (16, true).ToPixbuf ());
		imageTransparent.MarginEnd = 7;

		Gtk.Box hboxTransparent = GtkExtensions.BoxHorizontal ([
			imageTransparent,
			transparentBackgroundRadio]);

		IEnumerable<Gtk.Widget> GenerateBackgroundBoxItems ()
		{
			yield return backgroundLabel;
			yield return hboxWhite;

			if (allowBackgroundColor)
				yield return hboxBackground;

			yield return hboxTransparent;
		}

		var backgroundBoxItems = GenerateBackgroundBoxItems ().ToArray ();
		Gtk.Box backgroundVbox = GtkExtensions.BoxVertical (backgroundBoxItems);
		backgroundVbox.MarginTop = 4;

		// Layout table for preset, width, and height
		Gtk.Grid layoutGrid = Gtk.Grid.New ();
		layoutGrid.RowSpacing = 5;
		layoutGrid.ColumnSpacing = 6;
		layoutGrid.MarginBottom = 3;
		layoutGrid.Attach (sizeLabel, 0, 0, 1, 1);
		layoutGrid.Attach (presetDropdown, 1, 0, 1, 1);
		layoutGrid.Attach (widthLabel, 0, 1, 1, 1);
		layoutGrid.Attach (widthHbox, 1, 1, 1, 1);
		layoutGrid.Attach (heightLabel, 0, 2, 1, 1);
		layoutGrid.Attach (heightHbox, 1, 2, 1, 1);

		// Put all the options together
		BoxStyle spacedVertical = new (
			orientation: Gtk.Orientation.Vertical,
			spacing: 10);
		Gtk.Box optionsVbox = GtkExtensions.Box (
			spacedVertical,
			[
				layoutGrid,
				orientationVbox,
				backgroundVbox,
			]
		);

		// Layout the preview + the options

		Gtk.Label previewLabel = Gtk.Label.New (Translations.GetString ("Preview"));

		PreviewArea previewBox = new ();

		Gtk.Box previewVbox = GtkExtensions.BoxVertical ([
			previewLabel,
			previewBox]);

		BoxStyle spacedHorizontal = new (
			orientation: Gtk.Orientation.Horizontal,
			spacing: 10);
		Gtk.Box mainHbox = GtkExtensions.Box (
			spacedHorizontal,
			[
				optionsVbox,
				previewVbox,
			]
		);

		Gtk.Box contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (8);
		contentArea.Append (mainHbox);

		// --- Sub-component post-initialization

		if (initialBackgroundType == BackgroundType.SecondaryColor && allowBackgroundColor)
			secondaryBackgroundRadio.Active = true;

		if (initialBackgroundType == BackgroundType.Transparent)
			transparentBackgroundRadio.Active = true;

		if (initialBackgroundType == BackgroundType.White)
			whiteBackgroundRadio.Active = true;

		heightEntry.Buffer!.Text = initialSize.Height.ToString ();

		widthEntry.Buffer!.Text = initialSize.Width.ToString ();
		widthEntry.GrabFocus ();
		widthEntry.SelectRegion (0, (int) widthEntry.TextLength);

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString ("New Image");
		TransientFor = chrome.MainWindow;
		Modal = true;
		Resizable = false;
		IconName = Resources.StandardIcons.DocumentNew;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- References to keep

		has_clipboard = hasClipboard;
		this.palette = palette;
		clipboard_size = initialSize;
		preset_dropdown_model = presetDropdownModel;
		preset_dropdown = presetDropdown;
		width_entry = widthEntry;
		height_entry = heightEntry;
		portrait_radio = portraitRadio;
		landscape_radio = landscapeRadio;
		white_background_radio = whiteBackgroundRadio;
		secondary_background_radio = secondaryBackgroundRadio;
		transparent_background_radio = transparentBackgroundRadio;
		preview_box = previewBox;

		// --- TODO: Refactor this post-initialization

		WireUpEvents ();

		UpdateOrientation ();

		UpdatePresetSelection ();

		previewBox.Update (NewImageSize, NewImageBackground);
	}

	private static Gtk.Label CreateUnitsLabel ()
	{
		Gtk.Label label = Gtk.Label.New (Translations.GetString ("pixels"));
		label.MarginStart = 5;
		return label;
	}

	private static Gtk.Image CreateOrientationIcon (string iconName)
	{
		Gtk.Image image = Gtk.Image.New ();
		image.IconName = iconName;
		image.PixelSize = 16;
		image.MarginEnd = 7;
		return image;
	}

	private static Gtk.Entry CreateLengthEntry ()
	{
		Gtk.Entry entry = Gtk.Entry.New ();
		entry.WidthRequest = 50;
		entry.ActivatesDefault = true;
		return entry;
	}

	private static Gtk.CheckButton CreatePortraitRadio ()
		=> Gtk.CheckButton.NewWithLabel (Translations.GetString ("Portrait"));

	private static Gtk.CheckButton CreateLandscapeRadio (Gtk.CheckButton portraitRadio)
	{
		Gtk.CheckButton result = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Landscape"));
		result.SetGroup (portraitRadio);
		return result;
	}

	private static Gtk.Label CreateBackgroundLabel ()
	{
		Gtk.Label result = Gtk.Label.New (Translations.GetString ("Background:"));
		result.Xalign = 0f;
		result.Yalign = .5f;
		result.MarginBottom = 4;
		return result;
	}

	private static Gtk.Label CreateOrientationLabel ()
	{
		Gtk.Label result = Gtk.Label.New (Translations.GetString ("Orientation:"));
		result.Xalign = 0f;
		result.Yalign = .5f;
		result.MarginBottom = 4;
		return result;
	}

	private static Gtk.Label CreateWidthLabel ()
	{
		Gtk.Label result = Gtk.Label.New (Translations.GetString ("Width:"));
		result.Xalign = 1f;
		result.Yalign = .5f;
		return result;
	}

	private static Gtk.Label CreateHeightLabel ()
	{
		Gtk.Label result = Gtk.Label.New (Translations.GetString ("Height:"));
		result.Xalign = 1f;
		result.Yalign = .5f;
		return result;
	}

	private static Gtk.Label CreateSizeLabel ()
	{
		Gtk.Label result = Gtk.Label.New (Translations.GetString ("Preset:"));
		result.Xalign = 1f;
		result.Yalign = .5f;
		return result;
	}

	// Some arbitrary presets
	private static readonly IReadOnlyList<Size> preset_sizes = [
		new (640, 480),
		new (800, 600),
		new (1024, 768),
		new (1600, 1200),
	];

	private static IEnumerable<string> GeneratePresetEntries (bool hasClipboard)
	{
		if (hasClipboard)
			yield return Translations.GetString ("Clipboard");

		yield return Translations.GetString ("Custom");

		foreach (Size p in preset_sizes)
			yield return $"{p.Width} x {p.Height}";
	}

	public int NewImageWidth
		=> int.Parse (width_entry.Buffer!.Text!);

	public int NewImageHeight
		=> int.Parse (height_entry.Buffer!.Text!);

	public Size NewImageSize
		=> new (NewImageWidth, NewImageHeight);

	public BackgroundType NewImageBackgroundType {
		get {
			if (white_background_radio.Active)
				return BackgroundType.White;
			else if (transparent_background_radio.Active)
				return BackgroundType.Transparent;
			else
				return BackgroundType.SecondaryColor;
		}
	}

	public Cairo.Color NewImageBackground =>
		NewImageBackgroundType switch {
			BackgroundType.White => new Cairo.Color (1, 1, 1),
			BackgroundType.Transparent => new Cairo.Color (1, 1, 1, 0),
			_ => palette.SecondaryColor,
		};


	private bool IsValidSize {
		get {

			if (!int.TryParse (width_entry.Buffer!.Text!, out int width))
				return false;

			if (!int.TryParse (height_entry.Buffer!.Text!, out int height))
				return false;

			return width > 0 && height > 0;
		}
	}

	private Size GetSelectedPresetSize ()
	{
		string text = preset_dropdown_model.GetString (preset_dropdown.Selected)!;
		return ExtractSelectedPresetSize (text);
	}

	private static Size ExtractSelectedPresetSize (string text)
	{
		if (text == Translations.GetString ("Clipboard") || text == Translations.GetString ("Custom"))
			return Size.Empty;

		string[] textParts = text.Split (' ');

		return new (
			int.Parse (textParts[0]),
			int.Parse (textParts[2]));
	}

	private void WireUpEvents ()
	{
		// Handle preset changes
		Gtk.DropDown.SelectedPropertyDefinition.Notify (
			preset_dropdown,
			(_, _) => {
				Size new_size = IsValidSize ? NewImageSize : Size.Empty;

				string? preset_text = preset_dropdown_model.GetString (preset_dropdown.Selected);
				if (has_clipboard && preset_text == Translations.GetString ("Clipboard"))
					new_size = clipboard_size;
				else if (preset_text == Translations.GetString ("Custom"))
					return;
				else
					new_size = GetSelectedPresetSize ();

				suppress_events = true;
				width_entry.Buffer!.Text = new_size.Width.ToString ();
				height_entry.Buffer!.Text = new_size.Height.ToString ();
				suppress_events = false;

				UpdateOkButton ();
				if (!IsValidSize)
					return;

				UpdateOrientation ();
				preview_box.Update (NewImageSize);
			}
		);

		// Handle width/height entry changes
		width_entry.OnChanged += (o, e) => {
			if (suppress_events)
				return;

			UpdateOkButton ();

			if (!IsValidSize)
				return;

			if (NewImageSize != GetSelectedPresetSize ())
				preset_dropdown.Selected = has_clipboard ? 1u : 0;

			UpdateOrientation ();
			UpdatePresetSelection ();
			preview_box.Update (NewImageSize);
		};

		height_entry.OnChanged += (o, e) => {
			if (suppress_events)
				return;

			UpdateOkButton ();

			if (!IsValidSize)
				return;

			if (NewImageSize != GetSelectedPresetSize ())
				preset_dropdown.Selected = has_clipboard ? 1u : 0;

			UpdateOrientation ();
			UpdatePresetSelection ();
			preview_box.Update (NewImageSize);
		};

		// Handle orientation changes
		portrait_radio.OnToggled += (o, e) => {

			if (!portrait_radio.Active || !IsValidSize || NewImageWidth <= NewImageHeight)
				return;

			int temp = NewImageWidth;
			width_entry.Buffer!.Text = height_entry.Buffer!.Text;
			height_entry.Buffer!.Text = temp.ToString ();
			preview_box.Update (NewImageSize);
		};

		landscape_radio.OnToggled += (o, e) => {

			if (!landscape_radio.Active || !IsValidSize || NewImageWidth >= NewImageHeight)
				return;

			int temp = NewImageWidth;
			width_entry.Buffer!.Text = height_entry.Buffer!.Text;
			height_entry.Buffer!.Text = temp.ToString ();
			preview_box.Update (NewImageSize);
		};

		// Handle background color changes
		white_background_radio.OnToggled += (o, e) => { if (white_background_radio.Active) preview_box.Update (new Cairo.Color (1, 1, 1)); };
		secondary_background_radio.OnToggled += (o, e) => { if (secondary_background_radio.Active) preview_box.Update (palette.SecondaryColor); };
		transparent_background_radio.OnToggled += (o, e) => { if (transparent_background_radio.Active) preview_box.Update (new Cairo.Color (1, 1, 1, 0)); };
	}

	private void UpdateOrientation ()
	{
		if (NewImageWidth < NewImageHeight && !portrait_radio.Active)
			portrait_radio.Activate ();
		else if (NewImageWidth > NewImageHeight && !landscape_radio.Active)
			landscape_radio.Activate ();

		for (uint i = 1, n = preset_dropdown_model.GetNItems (); i < n; i++) {

			string text = preset_dropdown_model.GetString (i)!;

			if (text == Translations.GetString ("Clipboard") || text == Translations.GetString ("Custom"))
				continue;

			string[] textParts = text.Split ('x');

			int width = int.Parse (textParts[0].Trim ());
			int height = int.Parse (textParts[1].Trim ());

			Size newSize = new (
				NewImageWidth < NewImageHeight ? Math.Min (width, height) : Math.Max (width, height),
				NewImageWidth < NewImageHeight ? Math.Max (width, height) : Math.Min (width, height));

			string newText = $"{newSize.Width} x {newSize.Height}";

			if (newText != text)
				preset_dropdown_model.Splice (i, 1, [newText]);
		}
	}

	private void UpdateOkButton ()
	{
		var button = GetWidgetForResponse ((int) Gtk.ResponseType.Ok)!;
		button.Sensitive = IsValidSize;
	}

	private void UpdatePresetSelection ()
	{
		if (!IsValidSize)
			return;

		string text = $"{NewImageWidth} x {NewImageHeight}";

		if (preset_dropdown_model.FindString (text, out uint index) && preset_dropdown.Selected != index)
			preset_dropdown.Selected = index;
	}

	private sealed class PreviewArea : Gtk.Box
	{
		private Gtk.Picture picture;
		private Size size;
		private Cairo.Color color;

		private const int MAX_SIZE = 250;

		private static readonly Gdk.Texture transparent_pattern_texture =
			CairoExtensions.CreateTransparentBackgroundSurface (16).ToTexture ();

		public PreviewArea ()
		{
			WidthRequest = 300;
			Vexpand = true;
			Valign = Gtk.Align.Fill;
			Hexpand = true;
			Halign = Gtk.Align.Fill;

			// Center the paintable in an expanding box so that CSS can be used to draw
			// the drop shadow around only the canvas area.
			picture = Gtk.Picture.New ();
			picture.Name = "new-image-preview";
			picture.ContentFit = Gtk.ContentFit.ScaleDown;
			picture.Hexpand = true;
			picture.Vexpand = true;
			picture.Halign = Gtk.Align.Center;
			picture.Valign = Gtk.Align.Center;

			Append (picture);
		}

		public void Update (Size newSize)
			=> Update (newSize, color);

		public void Update (Cairo.Color newColor)
			=> Update (size, newColor);

		public void Update (Size newSize, Cairo.Color newColor)
		{
			size = newSize;
			color = newColor;

			Size previewSize = GetPreviewSizeForDraw (size);

			Graphene.Rect bounds = Graphene.Rect.Alloc ();
			bounds.Init (0, 0, previewSize.Width, previewSize.Height);

			Gtk.Snapshot snapshot = Gtk.Snapshot.New ();

			if (color.A == 0) {
				// Fill with transparent checkerboard pattern
				snapshot.AppendRepeatingTexture (transparent_pattern_texture, bounds);
			} else {
				// Fill with selected color
				snapshot.AppendColor (color.ToGdkRGBA (), bounds);
			}

			Gdk.Paintable? paintable = snapshot.ToPaintable (size: null);
			if (paintable is not null)
				picture.Paintable = paintable;
		}

		/// <summary>
		/// Figure out the dimensions of the preview to draw
		/// </summary>
		private static Size GetPreviewSizeForDraw (Size size)
		{
			if (size.Width <= MAX_SIZE && size.Height <= MAX_SIZE)
				return size;
			else if (size.Width > size.Height)
				return new (MAX_SIZE, (int) (MAX_SIZE / (size.Width / (float) size.Height)));
			else
				return new ((int) (MAX_SIZE / (size.Height / (float) size.Width)), MAX_SIZE);
		}
	}
}

