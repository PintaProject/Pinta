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
using Cairo;
using Pinta.Core;

namespace Pinta;

public sealed class NewImageDialog : Gtk.Dialog
{
	private readonly bool has_clipboard;
	private bool suppress_events;

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
		Size initialSize,
		BackgroundType initialBackgroundType,
		bool isClipboardSize)
	{
		// --- Control creation

		// We don't show the background color option if it's the same as "White"
		bool allowBackgroundColor = PintaCore.Palette.SecondaryColor.ToColorBgra () != ColorBgra.White;

		bool hasClipboard = isClipboardSize;

		Gtk.Label sizeLabel = CreateSizeLabel ();

		// Preset Combo
		Gtk.StringList presetDropdownModel = Gtk.StringList.New (GeneratePresetEntries (hasClipboard).ToArray ());

		Gtk.DropDown presetDropdown = Gtk.DropDown.New (presetDropdownModel, expression: null);

		Gtk.Label widthLabel = CreateWidthLabel ();
		Gtk.Entry widthEntry = CreateLengthEntry ();
		Gtk.Label widthUnitsLabel = CreateUnitsLabel ();
		Gtk.Box widthHbox = CreateHorizontalBox (widthEntry, widthUnitsLabel);

		Gtk.Label heightLabel = CreateHeightLabel ();
		Gtk.Entry heightEntry = CreateLengthEntry ();
		Gtk.Label heightUnitsLabel = CreateUnitsLabel ();
		Gtk.Box heightHbox = CreateHorizontalBox (heightEntry, heightUnitsLabel);

		// Orientation Radio options
		Gtk.Label orientationLabel = CreateOrientationLabel ();

		Gtk.CheckButton portraitRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Portrait"));

		Gtk.Image portraitImage = new () {
			IconName = Resources.Icons.OrientationPortrait,
			PixelSize = 16,
			MarginEnd = 7,
		};

		Gtk.Box portraitHbox = CreateHorizontalBox (portraitImage, portraitRadio);

		Gtk.CheckButton landscapeRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Landscape"));
		landscapeRadio.SetGroup (portraitRadio);

		Gtk.Image landscapeImage = new () {
			IconName = Resources.Icons.OrientationLandscape,
			PixelSize = 16,
			MarginEnd = 7,
		};

		Gtk.Box landscapeHbox = CreateHorizontalBox (landscapeImage, landscapeRadio);

		Gtk.Box orientationVbox = CreateVerticalBox (orientationLabel, portraitHbox, landscapeHbox);

		// Background Color options
		Gtk.Label backgroundLabel = CreateBackgroundLabel ();

		Gtk.CheckButton whiteBackgroundRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("White"));

		Gtk.Image imageWhite = Gtk.Image.NewFromPixbuf (CairoExtensions.CreateColorSwatch (16, new Cairo.Color (1, 1, 1)).ToPixbuf ());
		imageWhite.MarginEnd = 7;

		Gtk.Box hboxWhite = CreateHorizontalBox (imageWhite, whiteBackgroundRadio);

		Gtk.CheckButton secondaryBackgroundRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Background Color"));
		secondaryBackgroundRadio.SetGroup (whiteBackgroundRadio);

		Gtk.Image imageBackground = Gtk.Image.NewFromPixbuf (CairoExtensions.CreateColorSwatch (16, PintaCore.Palette.SecondaryColor).ToPixbuf ());
		imageBackground.MarginEnd = 7;

		Gtk.Box hboxBackground = CreateHorizontalBox (imageBackground, secondaryBackgroundRadio);

		Gtk.CheckButton transparentBackgroundRadio = Gtk.CheckButton.NewWithLabel (Translations.GetString ("Transparent"));
		transparentBackgroundRadio.SetGroup (secondaryBackgroundRadio);

		Gtk.Image imageTransparent = Gtk.Image.NewFromPixbuf (CairoExtensions.CreateTransparentColorSwatch (16, true).ToPixbuf ());
		imageTransparent.MarginEnd = 7;

		Gtk.Box hboxTransparent = CreateHorizontalBox (imageTransparent, transparentBackgroundRadio);

		Gtk.Box backgroundVbox = CreateVerticalBox (backgroundLabel, hboxWhite);

		if (allowBackgroundColor)
			backgroundVbox.Append (hboxBackground);

		backgroundVbox.Append (hboxTransparent);
		backgroundVbox.MarginTop = 4;

		// Layout table for preset, width, and height
		Gtk.Grid layoutGrid = new () {
			RowSpacing = 5,
			ColumnSpacing = 6,
			MarginBottom = 3,
		};
		layoutGrid.Attach (sizeLabel, 0, 0, 1, 1);
		layoutGrid.Attach (presetDropdown, 1, 0, 1, 1);
		layoutGrid.Attach (widthLabel, 0, 1, 1, 1);
		layoutGrid.Attach (widthHbox, 1, 1, 1, 1);
		layoutGrid.Attach (heightLabel, 0, 2, 1, 1);
		layoutGrid.Attach (heightHbox, 1, 2, 1, 1);

		// Put all the options together
		Gtk.Box optionsVbox = Gtk.Box.New (Gtk.Orientation.Vertical, 0);
		optionsVbox.Spacing = 10;
		optionsVbox.Append (layoutGrid);
		optionsVbox.Append (orientationVbox);
		optionsVbox.Append (backgroundVbox);

		// Layout the preview + the options

		Gtk.Label previewLabel = Gtk.Label.New (Translations.GetString ("Preview"));

		PreviewArea previewBox = new () {
			Vexpand = true,
			Valign = Gtk.Align.Fill,
			Hexpand = true,
			Halign = Gtk.Align.Fill,
		};

		Gtk.Box previewVbox = Gtk.Box.New (Gtk.Orientation.Vertical, 0);
		previewVbox.Append (previewLabel);
		previewVbox.Append (previewBox);

		Gtk.Box mainHbox = Gtk.Box.New (Gtk.Orientation.Horizontal, 10);
		mainHbox.Append (optionsVbox);
		mainHbox.Append (previewVbox);

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
		TransientFor = PintaCore.Chrome.MainWindow;
		Modal = true;
		Resizable = false;
		IconName = Resources.StandardIcons.DocumentNew;

		// --- Initialization (Gtk.Dialog)

		this.AddCancelOkButtons ();
		this.SetDefaultResponse (Gtk.ResponseType.Ok);

		// --- References to keep

		has_clipboard = hasClipboard;
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

	private static Gtk.Box CreateHorizontalBox (Gtk.Widget w1, Gtk.Widget w2)
	{
		Gtk.Box hbox = Gtk.Box.New (Gtk.Orientation.Horizontal, 0);
		hbox.Append (w1);
		hbox.Append (w2);
		return hbox;
	}

	private static Gtk.Box CreateVerticalBox (Gtk.Widget w1, Gtk.Widget w2)
	{
		Gtk.Box vbox = Gtk.Box.New (Gtk.Orientation.Vertical, 0);
		vbox.Append (w1);
		vbox.Append (w2);
		return vbox;
	}

	private static Gtk.Box CreateVerticalBox (params Gtk.Widget[] children)
	{
		Gtk.Box vbox = Gtk.Box.New (Gtk.Orientation.Vertical, 0);

		foreach (var child in children)
			vbox.Append (child);

		return vbox;
	}

	private static Gtk.Label CreateUnitsLabel ()
	{
		Gtk.Label label = Gtk.Label.New (Translations.GetString ("pixels"));
		label.MarginStart = 5;
		return label;
	}

	private static Gtk.Entry CreateLengthEntry ()
		=> new () {
			WidthRequest = 50,
			ActivatesDefault = true,
		};

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
	private static readonly IReadOnlyList<Size> preset_sizes = new Size[] {
		new (640, 480),
		new (800, 600),
		new (1024, 768),
		new (1600, 1200),
	};

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

	public enum BackgroundType
	{
		White,
		Transparent,
		SecondaryColor,
	}

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
			_ => PintaCore.Palette.SecondaryColor,
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

	private Size SelectedPresetSize {
		get {
			string text = preset_dropdown_model.GetString (preset_dropdown.Selected)!;
			if (text == Translations.GetString ("Clipboard") || text == Translations.GetString ("Custom"))
				return Size.Empty;

			var textParts = text.Split (' ');
			int width = int.Parse (textParts[0]);
			int height = int.Parse (textParts[2]);

			return new (width, height);
		}
	}

	private void WireUpEvents ()
	{
		// Handle preset changes
		// Gtk.DropDown doesn't have a proper signal for this, so listen to changes in the 'selected' property.
		preset_dropdown.OnNotify += (o, args) => {
			if (args.Pspec.GetName () != "selected")
				return;

			Size new_size = IsValidSize ? NewImageSize : Size.Empty;

			string? preset_text = preset_dropdown_model.GetString (preset_dropdown.Selected);
			if (has_clipboard && preset_text == Translations.GetString ("Clipboard"))
				new_size = clipboard_size;
			else if (preset_text == Translations.GetString ("Custom"))
				return;
			else
				new_size = SelectedPresetSize;

			suppress_events = true;
			width_entry.Buffer!.Text = new_size.Width.ToString ();
			height_entry.Buffer!.Text = new_size.Height.ToString ();
			suppress_events = false;

			UpdateOkButton ();
			if (!IsValidSize)
				return;

			UpdateOrientation ();
			preview_box.Update (NewImageSize);
		};
		// Handle width/height entry changes
		width_entry.OnChanged ((o, e) => {
			if (suppress_events)
				return;

			UpdateOkButton ();

			if (!IsValidSize)
				return;

			if (NewImageSize != SelectedPresetSize)
				preset_dropdown.Selected = has_clipboard ? 1u : 0;

			UpdateOrientation ();
			UpdatePresetSelection ();
			preview_box.Update (NewImageSize);
		});

		height_entry.OnChanged ((o, e) => {
			if (suppress_events)
				return;

			UpdateOkButton ();

			if (!IsValidSize)
				return;

			if (NewImageSize != SelectedPresetSize)
				preset_dropdown.Selected = has_clipboard ? 1u : 0;

			UpdateOrientation ();
			UpdatePresetSelection ();
			preview_box.Update (NewImageSize);
		});

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
		secondary_background_radio.OnToggled += (o, e) => { if (secondary_background_radio.Active) preview_box.Update (PintaCore.Palette.SecondaryColor); };
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

			var textParts = text.Split ('x');
			int width = int.Parse (textParts[0].Trim ());
			int height = int.Parse (textParts[1].Trim ());

			Size newSize = new (NewImageWidth < NewImageHeight ? Math.Min (width, height) : Math.Max (width, height), NewImageWidth < NewImageHeight ? Math.Max (width, height) : Math.Min (width, height));
			string newText = $"{newSize.Width} x {newSize.Height}";

			if (newText != text)
				preset_dropdown_model.Splice (i, 1, new[] { newText });
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

	private sealed class PreviewArea : Gtk.DrawingArea
	{
		private Size size;
		private Cairo.Color color;

		private const int MAX_SIZE = 250;

		public PreviewArea ()
		{
			WidthRequest = 300;

			SetDrawFunc ((area, context, width, height) => Draw (context, width, height));
		}

		public void Update (Size size)
		{
			this.size = size;

			QueueDraw ();
		}

		public void Update (Cairo.Color color)
		{
			this.color = color;

			QueueDraw ();
		}

		public void Update (Size size, Cairo.Color color)
		{
			this.size = size;
			this.color = color;

			QueueDraw ();
		}

		private void Draw (Context cr, int widget_width, int widget_height)
		{
			Size preview_size = GetPreviewSizeForDraw ();

			RectangleD r = new (
				(widget_width - preview_size.Width) / 2,
				(widget_height - preview_size.Height) / 2,
				preview_size.Width,
				preview_size.Height);

			if (color.A == 0) {
				// Fill with transparent checkerboard pattern
				Pattern pattern = CairoExtensions.CreateTransparentBackgroundPattern (16);
				cr.FillRectangle (r, pattern);
			} else {
				// Fill with selected color
				cr.FillRectangle (r, color);
			}

			// Draw our canvas drop shadow
			cr.DrawRectangle (new RectangleD (r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2), new Cairo.Color (.5, .5, .5), 1);
			cr.DrawRectangle (new RectangleD (r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), new Cairo.Color (.8, .8, .8), 1);
			cr.DrawRectangle (new RectangleD (r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), new Cairo.Color (.9, .9, .9), 1);
		}

		private Size GetPreviewSizeForDraw () // Figure out the dimensions of the preview to draw
		{
			if (size.Width <= MAX_SIZE && size.Height <= MAX_SIZE)
				return size;
			else if (size.Width > size.Height)
				return new Size (MAX_SIZE, (int) (MAX_SIZE / (size.Width / (float) size.Height)));
			else
				return new Size ((int) (MAX_SIZE / (size.Height / (float) size.Width)), MAX_SIZE);
		}
	}
}

