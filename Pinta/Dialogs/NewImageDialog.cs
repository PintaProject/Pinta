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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta
{
	public class NewImageDialog : Dialog
	{
		private readonly bool allow_background_color;
		private readonly bool has_clipboard;
		private bool suppress_events;

		private Size clipboard_size;

		private List<Size> preset_sizes;
		private PreviewArea preview;

		private Gtk.StringList preset_dropdown_model;
		private DropDown preset_dropdown;
		private Entry width_entry;
		private Entry height_entry;

		private CheckButton portrait_radio;
		private CheckButton landscape_radio;

		private CheckButton white_bg_radio;
		private CheckButton secondary_bg_radio;
		private CheckButton trans_bg_radio;

		/// <summary>
		/// Configures and builds a NewImageDialog object.
		/// </summary>
		/// <param name="imgWidth">Initial value of the width entry.</param>
		/// <param name="imgHeight">Initial value of the height entry.</param>
		/// <param name="isClipboardSize">Indicates if there is an image on the clipboard (and the size parameters represent the clipboard image size).</param>
		public NewImageDialog (int initialWidth, int initialHeight, BackgroundType initial_bg_type, bool isClipboardSize)
		{
			Title = Translations.GetString ("New Image");
			TransientFor = PintaCore.Chrome.MainWindow;
			Modal = true;
			this.AddCancelOkButtons ();
			this.SetDefaultResponse (ResponseType.Ok);

			// We don't show the background color option if it's the same as "White"
			allow_background_color = PintaCore.Palette.SecondaryColor.ToColorBgra () != ColorBgra.White;

			var content_area = this.GetContentAreaBox ();
			content_area.SetAllMargins (8);

			Resizable = false;

			IconName = Resources.StandardIcons.DocumentNew;

			has_clipboard = isClipboardSize;
			clipboard_size = new Size (initialWidth, initialHeight);

			InitializePresets ();
			BuildDialog ();

			if (initial_bg_type == BackgroundType.SecondaryColor && allow_background_color)
				secondary_bg_radio.Active = true;
			else if (initial_bg_type == BackgroundType.Transparent)
				trans_bg_radio.Active = true;
			else
				white_bg_radio.Active = true;

			width_entry.Buffer!.Text = initialWidth.ToString ();
			height_entry.Buffer!.Text = initialHeight.ToString ();

			width_entry.GrabFocus ();
			width_entry.SelectRegion (0, (int) width_entry.TextLength);

			WireUpEvents ();

			UpdateOrientation ();
			UpdatePresetSelection ();
			preview.Update (NewImageSize, NewImageBackground);
		}

		public int NewImageWidth => int.Parse (width_entry.Buffer!.Text!);
		public int NewImageHeight => int.Parse (height_entry.Buffer!.Text!);
		public Size NewImageSize => new Size (NewImageWidth, NewImageHeight);

		public enum BackgroundType
		{
			White,
			Transparent,
			SecondaryColor
		}

		public BackgroundType NewImageBackgroundType {
			get {
				if (white_bg_radio.Active)
					return BackgroundType.White;
				else if (trans_bg_radio.Active)
					return BackgroundType.Transparent;
				else
					return BackgroundType.SecondaryColor;
			}
		}

		public Cairo.Color NewImageBackground {
			get {
				switch (NewImageBackgroundType) {
					case BackgroundType.White:
						return new Cairo.Color (1, 1, 1);
					case BackgroundType.Transparent:
						return new Cairo.Color (1, 1, 1, 0);
					case BackgroundType.SecondaryColor:
					default:
						return PintaCore.Palette.SecondaryColor;
				}
			}
		}


		private bool IsValidSize {
			get {
				int width = 0;
				int height = 0;

				if (!int.TryParse (width_entry.Buffer!.Text!, out width))
					return false;
				if (!int.TryParse (height_entry.Buffer!.Text!, out height))
					return false;

				return width > 0 && height > 0;
			}
		}

		private Size SelectedPresetSize {
			get {
				string text = preset_dropdown_model.GetString (preset_dropdown.Selected)!;
				if (text == Translations.GetString ("Clipboard") || text == Translations.GetString ("Custom"))
					return Size.Empty;

				var text_parts = text.Split (' ');
				var width = int.Parse (text_parts[0]);
				var height = int.Parse (text_parts[2]);

				return new Size (width, height);
			}
		}

		[MemberNotNull (nameof (preset_sizes))]
		private void InitializePresets ()
		{
			// Some arbitrary presets
			preset_sizes = new List<Size> ();

			preset_sizes.Add (new Size (640, 480));
			preset_sizes.Add (new Size (800, 600));
			preset_sizes.Add (new Size (1024, 768));
			preset_sizes.Add (new Size (1600, 1200));
		}

		[MemberNotNull (nameof (preset_dropdown_model), nameof (preset_dropdown), nameof (portrait_radio), nameof (landscape_radio), nameof (white_bg_radio), nameof (secondary_bg_radio), nameof (trans_bg_radio), nameof (width_entry), nameof (height_entry), nameof (preview))]
		private void BuildDialog ()
		{
			// Layout table for preset, width, and height
			var layout_grid = new Grid ();

			layout_grid.RowSpacing = 5;
			layout_grid.ColumnSpacing = 6;

			// Preset Combo
			var size_label = Label.New (Translations.GetString ("Preset:"));
			size_label.Xalign = 1f;
			size_label.Yalign = .5f;

			var preset_entries = new List<string> ();

			if (has_clipboard)
				preset_entries.Add (Translations.GetString ("Clipboard"));

			preset_entries.Add (Translations.GetString ("Custom"));
			preset_entries.AddRange (preset_sizes.Select (p => $"{p.Width} x {p.Height}"));

			preset_dropdown_model = StringList.New (preset_entries.ToArray ());
			preset_dropdown = DropDown.New (preset_dropdown_model, expression: null);

			layout_grid.Attach (size_label, 0, 0, 1, 1);
			layout_grid.Attach (preset_dropdown, 1, 0, 1, 1);

			// Width Entry
			var width_label = Label.New (Translations.GetString ("Width:"));
			width_label.Xalign = 1f;
			width_label.Yalign = .5f;

			width_entry = new Entry ();
			width_entry.WidthRequest = 50;
			width_entry.ActivatesDefault = true;

			var width_units = Label.New (Translations.GetString ("pixels"));
			width_units.MarginStart = 5;

			var width_hbox = Box.New (Orientation.Horizontal, 0);
			width_hbox.Append (width_entry);
			width_hbox.Append (width_units);

			layout_grid.Attach (width_label, 0, 1, 1, 1);
			layout_grid.Attach (width_hbox, 1, 1, 1, 1);

			// Height Entry
			var height_label = Label.New (Translations.GetString ("Height:"));
			height_label.Xalign = 1f;
			height_label.Yalign = .5f;

			height_entry = new Entry ();
			height_entry.WidthRequest = 50;
			height_entry.ActivatesDefault = true;

			var height_units = Label.New (Translations.GetString ("pixels"));
			height_units.MarginStart = 5;

			var height_hbox = Box.New (Orientation.Horizontal, 0);
			height_hbox.Append (height_entry);
			height_hbox.Append (height_units);

			layout_grid.Attach (height_label, 0, 2, 1, 1);
			layout_grid.Attach (height_hbox, 1, 2, 1, 1);

			// Orientation Radio options
			var orientation_label = Label.New (Translations.GetString ("Orientation:"));
			orientation_label.Xalign = 0f;
			orientation_label.Yalign = .5f;

			portrait_radio = CheckButton.NewWithLabel (Translations.GetString ("Portrait"));
			var portrait_image = new Image () {
				IconName = Resources.Icons.OrientationPortrait,
				PixelSize = 16
			};

			var portrait_hbox = Box.New (Orientation.Horizontal, 0);
			portrait_image.MarginEnd = 7;

			portrait_hbox.Append (portrait_image);
			portrait_hbox.Append (portrait_radio);

			landscape_radio = CheckButton.NewWithLabel (Translations.GetString ("Landscape"));
			landscape_radio.SetGroup (portrait_radio);
			var landscape_image = new Image () {
				IconName = Resources.Icons.OrientationLandscape,
				PixelSize = 16
			};

			var landscape_hbox = Box.New (Orientation.Horizontal, 0);
			landscape_image.MarginEnd = 7;

			landscape_hbox.Append (landscape_image);
			landscape_hbox.Append (landscape_radio);

			// Orientation VBox
			var orientation_vbox = Box.New (Orientation.Vertical, 0);
			orientation_label.MarginBottom = 4;
			orientation_vbox.Append (orientation_label);
			orientation_vbox.Append (portrait_hbox);
			orientation_vbox.Append (landscape_hbox);

			// Background Color options
			var background_label = Label.New (Translations.GetString ("Background:"));
			background_label.Xalign = 0f;
			background_label.Yalign = .5f;
			background_label.MarginBottom = 4;

			white_bg_radio = CheckButton.NewWithLabel (Translations.GetString ("White"));
			var image_white = Image.NewFromPixbuf (CairoExtensions.CreateColorSwatch (16, new Cairo.Color (1, 1, 1)).ToPixbuf ());

			var hbox_white = Box.New (Orientation.Horizontal, 0);
			image_white.MarginEnd = 7;

			hbox_white.Append (image_white);
			hbox_white.Append (white_bg_radio);

			secondary_bg_radio = CheckButton.NewWithLabel (Translations.GetString ("Background Color"));
			secondary_bg_radio.SetGroup (white_bg_radio);
			var image_bg = Image.NewFromPixbuf (CairoExtensions.CreateColorSwatch (16, PintaCore.Palette.SecondaryColor).ToPixbuf ());

			var hbox_bg = Box.New (Orientation.Horizontal, 0);
			image_bg.MarginEnd = 7;

			hbox_bg.Append (image_bg);
			hbox_bg.Append (secondary_bg_radio);

			trans_bg_radio = CheckButton.NewWithLabel (Translations.GetString ("Transparent"));
			trans_bg_radio.SetGroup (secondary_bg_radio);
			var image_trans = Image.NewFromPixbuf (CairoExtensions.CreateTransparentColorSwatch (16, true).ToPixbuf ());
			image_trans.MarginEnd = 7;

			var hbox_trans = Box.New (Orientation.Horizontal, 0);

			hbox_trans.Append (image_trans);
			hbox_trans.Append (trans_bg_radio);

			// Background VBox
			var background_vbox = Box.New (Orientation.Vertical, 0);
			background_vbox.Append (background_label);
			background_vbox.Append (hbox_white);

			if (allow_background_color)
				background_vbox.Append (hbox_bg);

			background_vbox.Append (hbox_trans);

			// Put all the options together
			var options_vbox = Box.New (Orientation.Vertical, 0);
			options_vbox.Spacing = 10;

			layout_grid.MarginBottom = 3;
			background_vbox.MarginTop = 4;
			options_vbox.Append (layout_grid);
			options_vbox.Append (orientation_vbox);
			options_vbox.Append (background_vbox);

			// Layout the preview + the options
			preview = new PreviewArea ();
			preview.Vexpand = true;
			preview.Valign = Align.Fill;

			var preview_label = Label.New (Translations.GetString ("Preview"));

			var preview_vbox = Box.New (Orientation.Vertical, 0);
			preview.Hexpand = true;
			preview.Halign = Align.Fill;

			preview_vbox.Append (preview_label);
			preview_vbox.Append (preview);

			var main_hbox = Box.New (Orientation.Horizontal, 10);
			main_hbox.Append (options_vbox);
			main_hbox.Append (preview_vbox);

			var content_area = this.GetContentAreaBox ();
			content_area.Append (main_hbox);
		}

		private void WireUpEvents ()
		{
			// Handle preset changes
			// Gtk.DropDown doesn't have a proper signal for this, so listen to changes in the 'selected' property.
			preset_dropdown.OnNotify += (o, args) => {
				if (args.Pspec.GetName () != "selected")
					return;

				var new_size = IsValidSize ? NewImageSize : Size.Empty;

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
				preview.Update (NewImageSize);
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
				preview.Update (NewImageSize);
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
				preview.Update (NewImageSize);
			});

			// Handle orientation changes
			portrait_radio.OnToggled += (o, e) => {
				if (portrait_radio.Active && IsValidSize && NewImageWidth > NewImageHeight) {
					var temp = NewImageWidth;
					width_entry.Buffer!.Text = height_entry.Buffer!.Text;
					height_entry.Buffer!.Text = temp.ToString ();
					preview.Update (NewImageSize);
				}
			};

			landscape_radio.OnToggled += (o, e) => {
				if (landscape_radio.Active && IsValidSize && NewImageWidth < NewImageHeight) {
					var temp = NewImageWidth;
					width_entry.Buffer!.Text = height_entry.Buffer!.Text;
					height_entry.Buffer!.Text = temp.ToString ();
					preview.Update (NewImageSize);
				}
			};

			// Handle background color changes
			white_bg_radio.OnToggled += (o, e) => { if (white_bg_radio.Active) preview.Update (new Cairo.Color (1, 1, 1)); };
			secondary_bg_radio.OnToggled += (o, e) => { if (secondary_bg_radio.Active) preview.Update (PintaCore.Palette.SecondaryColor); };
			trans_bg_radio.OnToggled += (o, e) => { if (trans_bg_radio.Active) preview.Update (new Cairo.Color (1, 1, 1, 0)); };
		}

		private void UpdateOrientation ()
		{
			if (NewImageWidth < NewImageHeight && !portrait_radio.Active)
				portrait_radio.Activate ();
			else if (NewImageWidth > NewImageHeight && !landscape_radio.Active)
				landscape_radio.Activate ();

			for (uint i = 1, n = preset_dropdown_model.GetNItems (); i < n; i++) {
				var text = preset_dropdown_model.GetString (i)!;

				if (text == Translations.GetString ("Clipboard") || text == Translations.GetString ("Custom"))
					continue;

				var text_parts = text.Split ('x');
				var width = int.Parse (text_parts[0].Trim ());
				var height = int.Parse (text_parts[1].Trim ());

				var new_size = new Size (NewImageWidth < NewImageHeight ? Math.Min (width, height) : Math.Max (width, height), NewImageWidth < NewImageHeight ? Math.Max (width, height) : Math.Min (width, height));
				var new_text = $"{new_size.Width} x {new_size.Height}";

				if (new_text != text)
					preset_dropdown_model.Splice (i, 1, new[] { new_text });
			}
		}

		private void UpdateOkButton ()
		{
			var button = GetWidgetForResponse ((int) ResponseType.Ok)!;
			button.Sensitive = IsValidSize;
		}

		private void UpdatePresetSelection ()
		{
			if (!IsValidSize)
				return;

			var text = $"{NewImageWidth} x {NewImageHeight}";
			if (preset_dropdown_model.FindString (text, out uint index) && preset_dropdown.Selected != index) {
				preset_dropdown.Selected = index;
			}
		}

		private class PreviewArea : DrawingArea
		{
			private Size size;
			private Cairo.Color color;

			private readonly int max_size = 250;

			public PreviewArea ()
			{
				WidthRequest = 300;

				SetDrawFunc ((area, context, width, height) => Draw (context, width, height));
			}

			public void Update (Size size)
			{
				this.size = size;

				this.QueueDraw ();
			}

			public void Update (Cairo.Color color)
			{
				this.color = color;

				this.QueueDraw ();
			}

			public void Update (Size size, Cairo.Color color)
			{
				this.size = size;
				this.color = color;

				this.QueueDraw ();
			}

			private void Draw (Context cr, int widget_width, int widget_height)
			{
				var preview_size = Size.Empty;

				// Figure out the dimensions of the preview to draw
				if (size.Width <= max_size && size.Height <= max_size)
					preview_size = size;
				else if (size.Width > size.Height)
					preview_size = new Size (max_size, (int) (max_size / ((float) size.Width / (float) size.Height)));
				else
					preview_size = new Size ((int) (max_size / ((float) size.Height / (float) size.Width)), max_size);

				var r = new RectangleD ((widget_width - preview_size.Width) / 2, (widget_height - preview_size.Height) / 2, preview_size.Width, preview_size.Height);

				if (color.A == 0) {
					// Fill with transparent checkerboard pattern
					var pattern = CairoExtensions.CreateTransparentBackgroundPattern (16);
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
		}
	}
}

