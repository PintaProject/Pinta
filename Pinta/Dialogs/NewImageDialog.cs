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
		/// <summary>
		/// Configures and builds a NewImageDialog object.
		/// </summary>
		/// <param name="imgWidth">Initial value of the width entry.</param>
		/// <param name="imgHeight">Initial value of the height entry.</param>
		/// <param name="isClipboardSize">Indicates if there is an image on the clipboard (and the size parameters represent the clipboard image size).</param>
		private bool allow_background_color;
		private bool has_clipboard;
		private bool suppress_events;

		private Gdk.Size clipboard_size;

		private List<Gdk.Size> preset_sizes;
		private PreviewArea preview;

		private ComboBoxText preset_combo;
		private Entry width_entry;
		private Entry height_entry;

		private RadioButton portrait_radio;
		private RadioButton landscape_radio;

		private RadioButton white_bg_radio;
		private RadioButton secondary_bg_radio;
		private RadioButton trans_bg_radio;

		public NewImageDialog (int initialWidth, int initialHeight, BackgroundType initial_bg_type, bool isClipboardSize)
		    : base (string.Empty, PintaCore.Chrome.MainWindow, DialogFlags.Modal, Core.GtkExtensions.DialogButtonsCancelOk ())
		{
			Title = Translations.GetString ("New Image");
			WindowPosition = Gtk.WindowPosition.CenterOnParent;

			// We don't show the background color option if it's the same as "White"
			allow_background_color = PintaCore.Palette.SecondaryColor.ToColorBgra () != ColorBgra.White;

			BorderWidth = 4;
			ContentArea.Spacing = 4;

			Resizable = false;
			DefaultResponse = ResponseType.Ok;

			IconName = Resources.StandardIcons.DocumentNew;

			has_clipboard = isClipboardSize;
			clipboard_size = new Gdk.Size (initialWidth, initialHeight);

			InitializePresets ();
			BuildDialog ();

			if (initial_bg_type == BackgroundType.SecondaryColor && allow_background_color)
				secondary_bg_radio.Active = true;
			else if (initial_bg_type == BackgroundType.Transparent)
				trans_bg_radio.Active = true;
			else
				white_bg_radio.Active = true;

			width_entry.Text = initialWidth.ToString ();
			height_entry.Text = initialHeight.ToString ();

			width_entry.GrabFocus ();
			width_entry.SelectRegion (0, width_entry.Text.Length);

			WireUpEvents ();

			UpdateOrientation ();
			UpdatePresetSelection ();
			preview.Update (NewImageSize, NewImageBackground);
		}

		public int NewImageWidth { get { return int.Parse (width_entry.Text); } }
		public int NewImageHeight { get { return int.Parse (height_entry.Text); } }
		public Gdk.Size NewImageSize { get { return new Gdk.Size (NewImageWidth, NewImageHeight); } }

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

				if (!int.TryParse (width_entry.Text, out width))
					return false;
				if (!int.TryParse (height_entry.Text, out height))
					return false;

				return width > 0 && height > 0;
			}
		}

		private Gdk.Size SelectedPresetSize {
			get {
				if (preset_combo.ActiveText == Translations.GetString ("Clipboard") || preset_combo.ActiveText == Translations.GetString ("Custom"))
					return Gdk.Size.Empty;

				var text_parts = preset_combo.ActiveText.Split (' ');
				var width = int.Parse (text_parts[0]);
				var height = int.Parse (text_parts[2]);

				return new Gdk.Size (width, height);
			}
		}

		[MemberNotNull (nameof (preset_sizes))]
		private void InitializePresets ()
		{
			// Some arbitrary presets
			preset_sizes = new List<Gdk.Size> ();

			preset_sizes.Add (new Gdk.Size (640, 480));
			preset_sizes.Add (new Gdk.Size (800, 600));
			preset_sizes.Add (new Gdk.Size (1024, 768));
			preset_sizes.Add (new Gdk.Size (1600, 1200));
		}

		[MemberNotNull (nameof (preset_combo), nameof (portrait_radio), nameof (landscape_radio), nameof (white_bg_radio), nameof (secondary_bg_radio), nameof (trans_bg_radio), nameof (width_entry), nameof (height_entry), nameof (preview))]
		private void BuildDialog ()
		{
			// Layout table for preset, width, and height
			var layout_grid = new Grid ();

			layout_grid.RowSpacing = 5;
			layout_grid.ColumnSpacing = 6;

			// Preset Combo
			var size_label = new Label (Translations.GetString ("Preset:")) { Xalign = 1f, Yalign = .5f };

			var preset_entries = new List<string> ();

			if (has_clipboard)
				preset_entries.Add (Translations.GetString ("Clipboard"));

			preset_entries.Add (Translations.GetString ("Custom"));
			preset_entries.AddRange (preset_sizes.Select (p => string.Format ("{0} x {1}", p.Width, p.Height)));

			preset_combo = new ComboBoxText ();
			foreach (string entry in preset_entries)
				preset_combo.AppendText (entry);

			preset_combo.Active = 0;

			layout_grid.Attach (size_label, 0, 0, 1, 1);
			layout_grid.Attach (preset_combo, 1, 0, 1, 1);

			// Width Entry
			var width_label = new Label (Translations.GetString ("Width:")) { Xalign = 1f, Yalign = .5f };

			width_entry = new Entry ();
			width_entry.WidthRequest = 50;
			width_entry.ActivatesDefault = true;

			var width_units = new Label (Translations.GetString ("pixels"));

			var width_hbox = new HBox ();
			width_hbox.PackStart (width_entry, false, false, 0);
			width_hbox.PackStart (width_units, false, false, 5);

			layout_grid.Attach (width_label, 0, 1, 1, 1);
			layout_grid.Attach (width_hbox, 1, 1, 1, 1);

			// Height Entry
			var height_label = new Label (Translations.GetString ("Height:")) { Xalign = 1f, Yalign = .5f };

			height_entry = new Entry ();
			height_entry.WidthRequest = 50;
			height_entry.ActivatesDefault = true;

			var height_units = new Label (Translations.GetString ("pixels"));

			var height_hbox = new HBox ();
			height_hbox.PackStart (height_entry, false, false, 0);
			height_hbox.PackStart (height_units, false, false, 5);

			layout_grid.Attach (height_label, 0, 2, 1, 1);
			layout_grid.Attach (height_hbox, 1, 2, 1, 1);

			// Orientation Radio options
			var orientation_label = new Label (Translations.GetString ("Orientation:")) { Xalign = 0f, Yalign = .5f };

			portrait_radio = new RadioButton (Translations.GetString ("Portrait"));
			var portrait_image = new Image (PintaCore.Resources.GetIcon (Stock.OrientationPortrait, 16));

			var portrait_hbox = new HBox ();

			portrait_hbox.PackStart (portrait_image, false, false, 7);
			portrait_hbox.PackStart (portrait_radio, false, false, 0);

			landscape_radio = new RadioButton (portrait_radio, Translations.GetString ("Landscape"));
			var landscape_image = new Image (PintaCore.Resources.GetIcon (Stock.OrientationLandscape, 16));

			var landscape_hbox = new HBox ();

			landscape_hbox.PackStart (landscape_image, false, false, 7);
			landscape_hbox.PackStart (landscape_radio, false, false, 0);

			// Orientation VBox
			var orientation_vbox = new VBox ();
			orientation_vbox.PackStart (orientation_label, false, false, 4);
			orientation_vbox.PackStart (portrait_hbox, false, false, 0);
			orientation_vbox.PackStart (landscape_hbox, false, false, 0);

			// Background Color options
			var background_label = new Label (Translations.GetString ("Background:")) { Xalign = 0f, Yalign = .5f };

			white_bg_radio = new RadioButton (Translations.GetString ("White"));
			var image_white = new Image (GdkExtensions.CreateColorSwatch (16, new Gdk.Color (255, 255, 255)));

			var hbox_white = new HBox ();

			hbox_white.PackStart (image_white, false, false, 7);
			hbox_white.PackStart (white_bg_radio, false, false, 0);

			secondary_bg_radio = new RadioButton (white_bg_radio, Translations.GetString ("Background Color"));
			var image_bg = new Image (GdkExtensions.CreateColorSwatch (16, PintaCore.Palette.SecondaryColor.ToGdkColor ()));

			var hbox_bg = new HBox ();

			hbox_bg.PackStart (image_bg, false, false, 7);
			hbox_bg.PackStart (secondary_bg_radio, false, false, 0);

			trans_bg_radio = new RadioButton (secondary_bg_radio, Translations.GetString ("Transparent"));
			var image_trans = new Image (GdkExtensions.CreateTransparentColorSwatch (true));

			var hbox_trans = new HBox ();

			hbox_trans.PackStart (image_trans, false, false, 7);
			hbox_trans.PackStart (trans_bg_radio, false, false, 0);

			// Background VBox
			var background_vbox = new VBox ();
			background_vbox.PackStart (background_label, false, false, 4);
			background_vbox.PackStart (hbox_white, false, false, 0);

			if (allow_background_color)
				background_vbox.PackStart (hbox_bg, false, false, 0);

			background_vbox.PackStart (hbox_trans, false, false, 0);

			// Put all the options together
			var options_vbox = new VBox ();
			options_vbox.Spacing = 10;

			options_vbox.PackStart (layout_grid, false, false, 3);
			options_vbox.PackStart (orientation_vbox, false, false, 0);
			options_vbox.PackStart (background_vbox, false, false, 4);

			// Layout the preview + the options
			preview = new PreviewArea ();

			var preview_label = new Label (Translations.GetString ("Preview"));

			var preview_vbox = new VBox ();
			preview_vbox.PackStart (preview_label, false, false, 0);
			preview_vbox.PackStart (preview, true, true, 0);


			var main_hbox = new HBox (false, 10);

			main_hbox.PackStart (options_vbox, false, false, 0);
			main_hbox.PackStart (preview_vbox, true, true, 0);

			ContentArea.Add (main_hbox);

			ShowAll ();
		}

		private void WireUpEvents ()
		{
			// Handle preset combo changes
			preset_combo.Changed += (o, e) => {
				var new_size = IsValidSize ? NewImageSize : Gdk.Size.Empty;

				if (has_clipboard && preset_combo.ActiveText == Translations.GetString ("Clipboard"))
					new_size = clipboard_size;
				else if (preset_combo.ActiveText == Translations.GetString ("Custom"))
					return;
				else
					new_size = SelectedPresetSize;

				suppress_events = true;
				width_entry.Text = new_size.Width.ToString ();
				height_entry.Text = new_size.Height.ToString ();
				suppress_events = false;

				UpdateOkButton ();
				if (!IsValidSize)
					return;

				UpdateOrientation ();
				preview.Update (NewImageSize);
			};

			// Handle width/height entry changes
			width_entry.Changed += (o, e) => {
				if (suppress_events)
					return;

				UpdateOkButton ();

				if (!IsValidSize)
					return;

				if (NewImageSize != SelectedPresetSize)
					preset_combo.Active = has_clipboard ? 1 : 0;

				UpdateOrientation ();
				UpdatePresetSelection ();
				preview.Update (NewImageSize);
			};

			height_entry.Changed += (o, e) => {
				if (suppress_events)
					return;

				UpdateOkButton ();

				if (!IsValidSize)
					return;

				if (NewImageSize != SelectedPresetSize)
					preset_combo.Active = has_clipboard ? 1 : 0;

				UpdateOrientation ();
				UpdatePresetSelection ();
				preview.Update (NewImageSize);
			};

			// Handle orientation changes
			portrait_radio.Toggled += (o, e) => {
				if (portrait_radio.Active && IsValidSize && NewImageWidth > NewImageHeight) {
					var temp = NewImageWidth;
					width_entry.Text = height_entry.Text;
					height_entry.Text = temp.ToString ();
					preview.Update (NewImageSize);
				}
			};

			landscape_radio.Toggled += (o, e) => {
				if (landscape_radio.Active && IsValidSize && NewImageWidth < NewImageHeight) {
					var temp = NewImageWidth;
					width_entry.Text = height_entry.Text;
					height_entry.Text = temp.ToString ();
					preview.Update (NewImageSize);
				}
			};

			// Handle background color changes
			white_bg_radio.Toggled += (o, e) => { if (white_bg_radio.Active) preview.Update (new Cairo.Color (1, 1, 1)); };
			secondary_bg_radio.Toggled += (o, e) => { if (secondary_bg_radio.Active) preview.Update (PintaCore.Palette.SecondaryColor); };
			trans_bg_radio.Toggled += (o, e) => { if (trans_bg_radio.Active) preview.Update (new Cairo.Color (1, 1, 1, 0)); };
		}

		private void UpdateOrientation ()
		{
			if (NewImageWidth < NewImageHeight && !portrait_radio.Active)
				portrait_radio.Activate ();
			else if (NewImageWidth > NewImageHeight && !landscape_radio.Active)
				landscape_radio.Activate ();

			for (var i = 1; i < preset_combo.GetItemCount (); i++) {
				var text = preset_combo.GetValueAt<string> (i);

				if (text == Translations.GetString ("Clipboard") || text == Translations.GetString ("Custom"))
					continue;

				var text_parts = text.Split ('x');
				var width = int.Parse (text_parts[0].Trim ());
				var height = int.Parse (text_parts[1].Trim ());

				var new_size = new Gdk.Size (NewImageWidth < NewImageHeight ? Math.Min (width, height) : Math.Max (width, height), NewImageWidth < NewImageHeight ? Math.Max (width, height) : Math.Min (width, height));
				var new_text = string.Format ("{0} x {1}", new_size.Width, new_size.Height);

				preset_combo.SetValueAt (i, new_text);
			}
		}

		private void UpdateOkButton ()
		{
			var button = GetWidgetForResponse ((int) ResponseType.Ok);
			button.Sensitive = IsValidSize;
		}

		private void UpdatePresetSelection ()
		{
			if (!IsValidSize)
				return;

			var text = string.Format ("{0} x {1}", NewImageWidth, NewImageHeight);
			var index = preset_combo.FindValue (text);

			if (index >= 0 && preset_combo.Active != index)
				preset_combo.Active = index;
		}

		private class PreviewArea : DrawingArea
		{
			private Gdk.Size size;
			private Cairo.Color color;

			private int max_size = 250;

			public PreviewArea ()
			{
				WidthRequest = 300;
			}

			public void Update (Gdk.Size size)
			{
				this.size = size;

				this.QueueDraw ();
			}

			public void Update (Cairo.Color color)
			{
				this.color = color;

				this.QueueDraw ();
			}

			public void Update (Gdk.Size size, Cairo.Color color)
			{
				this.size = size;
				this.color = color;

				this.QueueDraw ();
			}

			protected override bool OnDrawn (Context cr)
			{
				base.OnDrawn (cr);

				if (size == Gdk.Size.Empty)
					return true;

				var preview_size = Gdk.Size.Empty;
				var widget_size = Window.GetBounds ();

				// Figure out the dimensions of the preview to draw
				if (size.Width <= max_size && size.Height <= max_size)
					preview_size = size;
				else if (size.Width > size.Height)
					preview_size = new Gdk.Size (max_size, (int) (max_size / ((float) size.Width / (float) size.Height)));
				else
					preview_size = new Gdk.Size ((int) (max_size / ((float) size.Height / (float) size.Width)), max_size);

				var r = new Cairo.Rectangle ((widget_size.Width - preview_size.Width) / 2, (widget_size.Height - preview_size.Height) / 2, preview_size.Width, preview_size.Height);

				if (color.A == 0) {
					// Fill with transparent checkerboard pattern
					using (var pattern = CairoExtensions.CreateTransparentBackgroundPattern (16))
						cr.FillRectangle (r, pattern);
				} else {
					// Fill with selected color
					cr.FillRectangle (r, color);
				}

				// Draw our canvas drop shadow
				cr.DrawRectangle (new Cairo.Rectangle (r.X - 1, r.Y - 1, r.Width + 2, r.Height + 2), new Cairo.Color (.5, .5, .5), 1);
				cr.DrawRectangle (new Cairo.Rectangle (r.X - 2, r.Y - 2, r.Width + 4, r.Height + 4), new Cairo.Color (.8, .8, .8), 1);
				cr.DrawRectangle (new Cairo.Rectangle (r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6), new Cairo.Color (.9, .9, .9), 1);

				return true;
			}
		}
	}
}

