using System;
using System.Globalization;
using System.Linq;
using Cairo;
using Gdk;
using GLib;
using Gtk;
using Pinta.Core;
using Pinta.Core.Extensions;
using Color = Cairo.Color;
using Context = Cairo.Context;
using HeaderBar = Adw.HeaderBar;
using String = System.String;

namespace Pinta.Gui.Widgets.Widgets;



// used for the right hand side sliders
// uses a label, scale, and entry
// then hides the scale and draws over it
// with a drawingarea
public class ColorPickerSlider : Gtk.Box
{
	private readonly Gtk.Window top_window;

	public Gtk.Label label = new Gtk.Label ();
	public Gtk.Scale slider = new Gtk.Scale ();
	public Gtk.Entry input = new Gtk.Entry ();
	public Gtk.Overlay slider_overlay = new Overlay ();
	public Gtk.DrawingArea gradient = new Gtk.DrawingArea ();
	public Gtk.DrawingArea cursor = new Gtk.DrawingArea ();

	public int maxVal;

	public class OnChangeValArgs : EventArgs
	{
		public string sender_name = "";
		public double value;
	}


	private bool entryBeingEdited = false;
	public ColorPickerSlider (int upper, String text, double val, Gtk.Window topWindow, int sliderPadding, int sliderWidth, int maxWidthChars = 3)
	{
		maxVal = upper;
		top_window = topWindow;
		label.SetLabel (text);
		label.WidthRequest = 50;
		slider.SetOrientation (Orientation.Horizontal);
		slider.SetAdjustment (Adjustment.New (0, 0, maxVal + 1, 1, 1, 1));

		slider.WidthRequest = sliderWidth;
		slider.SetValue (val);
		slider.Opacity = 0;

		gradient.SetSizeRequest (sliderWidth, this.GetHeight ());
		cursor.SetSizeRequest (sliderWidth, this.GetHeight ());

		cursor.SetDrawFunc ((area, context, width, height) => {
			int outlineWidth = 2;

			var prog = slider.GetValue () / maxVal * (width - 2 * sliderPadding);

			ReadOnlySpan<PointD> cursorPoly = stackalloc PointD[] {
					new PointD (prog + sliderPadding, height / 2),
					new PointD (prog + sliderPadding + 4, 3 * height / 4),
					new PointD (prog + sliderPadding + 4, height - outlineWidth / 2),
					new PointD (prog + sliderPadding - 4, height - outlineWidth / 2),
					new PointD (prog + sliderPadding - 4, 3 * height / 4),
					new PointD (prog + sliderPadding, height / 2)
				};

			context.LineWidth = outlineWidth;
			context.DrawPolygonal (cursorPoly, new Color (0, 0, 0), LineCap.Butt);
			context.FillPolygonal (cursorPoly, new Color (1, 1, 1));
		});


		slider_overlay.WidthRequest = sliderWidth;
		slider_overlay.HeightRequest = this.GetHeight ();


		slider_overlay.AddOverlay (gradient);
		slider_overlay.AddOverlay (cursor);
		slider_overlay.AddOverlay (slider);

		input.MaxWidthChars = maxWidthChars;
		input.WidthRequest = 50;
		input.Hexpand = false;
		input.SetText (Convert.ToInt32 (val).ToString ());
		this.Append (label);
		this.Append (slider_overlay);
		this.Append (input);

		//slider.Opacity = 0;

		slider.OnChangeValue += (sender, args) => {
			var e = new OnChangeValArgs ();


			e.sender_name = label.GetLabel ();
			e.value = args.Value;
			input.SetText (e.value.ToString (CultureInfo.InvariantCulture));
			OnValueChange?.Invoke (this, e);
			return false;
		};

		input.OnChanged ((o, e) => {
			if (suppressEvent > 0) {
				suppressEvent--;
				return;
			}
			var t = o.GetText ();
			double val;
			var success = double.TryParse (t, CultureInfo.InvariantCulture, out val);

			if (val > maxVal) {
				val = maxVal;
				input.SetText (Convert.ToInt32 (val).ToString ());
			}


			if (success) {
				var e2 = new OnChangeValArgs ();
				e2.sender_name = label.GetLabel ();
				e2.value = val;
				OnValueChange?.Invoke (this, e2);
			}
		});
	}

	public void SetSliderWidth (int sliderWidth)
	{
		slider.WidthRequest = sliderWidth;
		gradient.SetSizeRequest (sliderWidth, this.GetHeight ());
		cursor.SetSizeRequest (sliderWidth, this.GetHeight ());
		slider_overlay.WidthRequest = sliderWidth;
	}

	public event EventHandler<OnChangeValArgs> OnValueChange;

	private int suppressEvent = 0;
	public void SetValue (double val)
	{
		slider.SetValue (val);
		// Make sure we do not set the text if we are editing it right now
		if (top_window.GetFocus ()?.Parent != input) {
			// hackjob
			// prevents OnValueChange from firing when we change the value internally
			// because OnValueChange eventually calls SetValue so it causes a stack overflow
			suppressEvent = 2;
			input.SetText (Convert.ToInt32 (val).ToString ());
		}
		gradient.QueueDraw ();
		cursor.QueueDraw ();
	}

	public static void DrawGradient (Context context, int width, int height, int padwidth, int padheight, Color[] colors)
	{
		context.Antialias = Antialias.None;
		var draw_w = width - padwidth * 2;
		var draw_h = height - padheight * 2;
		var x1 = padwidth + draw_w;
		var y1 = padheight + draw_h;

		var bsize = draw_h / 2;

		// Draw transparency background
		context.FillRectangle (new RectangleD (padwidth, padheight, draw_w, draw_h), new Color (1, 1, 1));
		for (int x = padwidth; x < x1; x += bsize * 2) {
			var bwidth = bsize;
			if (x + bsize > x1)
				bwidth = x1 - x;
			context.FillRectangle (new RectangleD (x, padheight, bwidth, bsize), new Color (.8, .8, .8));
		}
		for (int x = padwidth + bsize; x < x1; x += bsize * 2) {
			var bwidth = bsize;
			if (x + bsize > x1)
				bwidth = x1 - x;
			context.FillRectangle (new RectangleD (x, padheight + draw_h / 2, bwidth, bsize), new Color (.8, .8, .8));
		}

		var pat = new LinearGradient (padwidth, padheight, x1, y1);

		for (int i = 0; i < colors.Length; i++)
			pat.AddColorStop (i / (double) (colors.Length - 1), colors[i]);

		context.Rectangle (padwidth, padheight, draw_w, draw_h);
		context.SetSource (pat);
		context.Fill ();
	}

}

public class CheckboxOption : Gtk.Box
{
	public bool state = false;
	public readonly Gtk.CheckButton button;
	public readonly Gtk.Label label;
	public CheckboxOption (int spacing, bool active, string text)
	{
		this.Spacing = spacing;
		button = new Gtk.CheckButton ();
		state = active;
		button.Active = state;
		this.Append (button);

		label = new Gtk.Label { Label_ = text };
		this.Append (label);
	}

	public void Toggle ()
	{
		state = !state;
		button.Active = state;
	}
}


public sealed class ColorPickerDialog : Gtk.Dialog
{

	private Gtk.Box top_box;
	private Gtk.Box swatch_box;

	private Gtk.HeaderBar title_bar;

	private Gtk.Box color_display_box;

	// palette
	private int palette_display_size = 50;
	private int palette_display_border_thickness = 3;
	private Gtk.DrawingArea[] color_displays;
	//private readonly Gtk.DrawingArea palette_display_primary;
	//private readonly Gtk.DrawingArea palette_display_secondary;

	// color surface
	private Gtk.Box picker_surface_selector_box;
	private Gtk.Box picker_surface_box;
	private Gtk.Overlay picker_surface_overlay;
	private int picker_surface_radius = 200 / 2;
	private int picker_surface_padding = 10;
	private Gtk.DrawingArea picker_surface;
	private Gtk.DrawingArea picker_surface_cursor;
	enum ColorSurfaceType
	{
		HueAndSat,
		SatAndVal
	}
	private ColorSurfaceType picker_surface_type = ColorSurfaceType.HueAndSat;
	// color surface options
	private bool mouse_on_picker_surface = false;
	private CheckboxOption picker_surface_option_draw_value;

	// swatches
	private Gtk.DrawingArea swatch_recent;
	private Gtk.DrawingArea swatch_palette;

	// hex + sliders
	private Entry hex_entry;
	private CheckboxOption hex_entry_add_alpha;

	private int cps_padding_height = 10;
	private int cps_padding_width = 14;
	private int cps_width = 200;
	private Gtk.Box sliders_box;
	private ColorPickerSlider hue_cps;
	private ColorPickerSlider sat_cps;
	private ColorPickerSlider val_cps;

	private ColorPickerSlider r_cps;
	private ColorPickerSlider g_cps;
	private ColorPickerSlider b_cps;

	private ColorPickerSlider a_cps;


	// common state
	public int color_index = 0;
	public Color[] colors;
	public readonly Color[] original_colors;

	public Color CurrentColor {
		get => colors[color_index];
		set => colors[color_index] = value;
	}

	private int spacing = 6;
	private int margins = 12;
	private ChromeManager chrome_manager;
	private string window_title;
	private bool small_mode = false;
	private bool show_swatches = false;

	public void SetSmallMode (bool isSmallMode)
	{
		small_mode = isSmallMode;
		if (isSmallMode) {
			spacing = 2;
			margins = 4;
			palette_display_size = 40;
			picker_surface_radius = 75;
			cps_width = 150;
			swatch_box.Visible = false;
		} else {
			spacing = 6;
			margins = 12;
			palette_display_size = 50;
			picker_surface_radius = 100;
			cps_width = 200;
			if(show_swatches)
				swatch_box.Visible = true;
		}

		top_box.Spacing = spacing;
		color_display_box.Spacing = spacing;

		foreach (var display in color_displays)
			display.SetSizeRequest (palette_display_size, palette_display_size);
		var pickerSurfaceDrawSize = (picker_surface_radius + picker_surface_padding) * 2;

		picker_surface_box.WidthRequest = pickerSurfaceDrawSize;
		picker_surface_box.Spacing = spacing;
		picker_surface_selector_box.WidthRequest = pickerSurfaceDrawSize;
		picker_surface_selector_box.Spacing = spacing;
		picker_surface.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);
		picker_surface_cursor.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);
		picker_surface_overlay.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);
		if (small_mode)
			picker_surface_selector_box.SetOrientation (Orientation.Vertical);
		else
			picker_surface_selector_box.SetOrientation (Orientation.Horizontal);

		hue_cps.SetSliderWidth (cps_width);
		sat_cps.SetSliderWidth (cps_width);
		val_cps.SetSliderWidth (cps_width);
		r_cps.SetSliderWidth (cps_width);
		g_cps.SetSliderWidth (cps_width);
		b_cps.SetSliderWidth (cps_width);
		a_cps.SetSliderWidth (cps_width);

		sliders_box.Spacing = spacing;

		DefaultWidth = 1;
		DefaultHeight = 1;
	}

	public void Setup ()
	{
		// Top part of the color picker.
		// Includes palette, color surface, sliders/hex
		// Basically, the not-swatches
		top_box = new Gtk.Box { Spacing = spacing };


		// titlebar of color picker; mainly just contains the reset color button
		#region Titlebar

			title_bar = new Gtk.HeaderBar ();
			var reset_button = new Button ();
			reset_button.Label = Translations.GetString ("Reset Color");
			reset_button.OnClicked += (button, args) => {
				colors = (Color[]) original_colors.Clone ();
				UpdateView ();
			};
			title_bar.PackStart (reset_button);

			var shrinkButton = new Gtk.Button ();
			if(small_mode)
				shrinkButton.SetIconName (Resources.StandardIcons.WindowMaximize);
			else
				shrinkButton.SetIconName (Resources.StandardIcons.WindowMinimize);
			shrinkButton.OnClicked += (sender, args) => {
				var contentArea = this.GetContentAreaBox ();
				//contentArea.RemoveAll ();
				SetSmallMode (!small_mode);
				if(small_mode)
					shrinkButton.SetIconName (Resources.StandardIcons.WindowMaximize);
				else
					shrinkButton.SetIconName (Resources.StandardIcons.WindowMinimize);
			};

			title_bar.PackStart (shrinkButton);

			Gtk.Button ok_button = new Button { Label = Translations.GetString ("OK") };
			ok_button.OnClicked += (sender, args) => { this.Response ((int)Gtk.ResponseType.Ok); this.Close (); };
			ok_button.AddCssClass (AdwaitaStyles.SuggestedAction);

			Gtk.Button cancel_button = new Button { Label = Translations.GetString ("Cancel") };
			cancel_button.OnClicked += (sender, args) => { this.Response ((int)Gtk.ResponseType.Ok); this.Close (); };

			title_bar.PackEnd (ok_button);
			title_bar.PackEnd (cancel_button);

			title_bar.SetShowTitleButtons (false);
			this.SetTitlebar (title_bar);

		#endregion


		// Active palette contains the primary/secondary colors on the left of the color picker
		#region Color Display

		color_display_box = new Gtk.Box { Spacing = spacing };
		color_display_box.SetOrientation (Orientation.Vertical);

		var colorDisplayList = new Gtk.ListBox ();

		if (colors.Length > 1) {
			var colorDisplaySwap = new Gtk.Button ();
			colorDisplaySwap.SetIconName (Resources.Icons.LayerMoveUp);
			colorDisplaySwap.OnClicked += (sender, args) => {
				var swap = colors[0];
				for (int i = 0; i < colors.Length - 1; i++)
					colors[i] = colors[i + 1];
				colors[^1] = swap;
				UpdateView ();
			};

			color_display_box.Append (colorDisplaySwap);
		}

		color_displays = new DrawingArea[original_colors.Length];
		for (int i = 0; i < original_colors.Length; i++) {
			var display = new Gtk.DrawingArea ();
			display.SetSizeRequest (palette_display_size, palette_display_size);
			var pos = i;
			display.SetDrawFunc ((area, context, width, height) => DrawPaletteDisplay (context, colors[pos]));
			colorDisplayList.Append (display);
			color_displays[i] = display;
		}

		// Set initial selected row
		colorDisplayList.SetSelectionMode (SelectionMode.Single);
		colorDisplayList.SelectRow (colorDisplayList.GetRowAtIndex (color_index));

		// Handle on select; index 0 -> primary; index 1 -> secondary
		colorDisplayList.OnRowSelected += ((sender, args) => {
			color_index = args.Row?.GetIndex () ?? 0;
			UpdateView ();
		});

		color_display_box.Append (colorDisplayList);

		#endregion


		// Picker surface; either is Hue & Sat (Color circle) or Sat & Val (Square)
		// Also contains picker surface switcher + options
		#region Picker Surface

		var pickerSurfaceDrawSize = (picker_surface_radius + picker_surface_padding) * 2;

		picker_surface_box = new Gtk.Box { Spacing = spacing, WidthRequest = pickerSurfaceDrawSize };
		picker_surface_box.SetOrientation (Orientation.Vertical);

		picker_surface_selector_box = new Gtk.Box { Spacing = spacing, WidthRequest = pickerSurfaceDrawSize };
		picker_surface_selector_box.Homogeneous = true;
		picker_surface_selector_box.Halign = Align.Center;

		// When Gir.Core supports it, this should probably be replaced with a toggle group.
		var pickerSurfaceHueSat = Gtk.ToggleButton.NewWithLabel (Translations.GetString ("Hue & Sat"));
		if (picker_surface_type == ColorSurfaceType.HueAndSat)
			pickerSurfaceHueSat.Toggle ();
		pickerSurfaceHueSat.OnToggled += (sender, args) => {
			picker_surface_type = ColorSurfaceType.HueAndSat;
			picker_surface_option_draw_value.SetVisible (true);
			UpdateView ();
		};

		var pickerSurfaceSatVal = Gtk.ToggleButton.NewWithLabel (Translations.GetString ("Sat & Value"));
		if (picker_surface_type == ColorSurfaceType.SatAndVal)
			pickerSurfaceSatVal.Toggle ();
		pickerSurfaceSatVal.OnToggled += (sender, args) => {
			picker_surface_type = ColorSurfaceType.SatAndVal;
			picker_surface_option_draw_value.SetVisible (false);
			UpdateView ();
		};


		pickerSurfaceHueSat.SetGroup (pickerSurfaceSatVal);
		picker_surface_selector_box.Append (pickerSurfaceHueSat);
		picker_surface_selector_box.Append (pickerSurfaceSatVal);

		picker_surface_box.Append (picker_surface_selector_box);

		picker_surface = new Gtk.DrawingArea ();
		picker_surface.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);
		picker_surface.SetDrawFunc ((area, context, width, height) => DrawColorSurface (context));

		// Cursor handles the square in the picker surface displaying where your selected color is
		picker_surface_cursor = new Gtk.DrawingArea ();
		picker_surface_cursor.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);
		picker_surface_cursor.SetDrawFunc ((area, context, width, height) => {
			context.Antialias = Antialias.None;
			var loc = HsvToPickerLocation (CurrentColor.GetHsv (), picker_surface_radius);
			loc = new PointD (loc.X + picker_surface_radius + picker_surface_padding, loc.Y + picker_surface_radius + picker_surface_padding);

			context.FillRectangle (new RectangleD (loc.X - 5, loc.Y - 5, 10, 10), CurrentColor);
			context.DrawRectangle (new RectangleD (loc.X - 5, loc.Y - 5, 10, 10), new Color (0, 0, 0), 4);
			context.DrawRectangle (new RectangleD (loc.X - 5, loc.Y - 5, 10, 10), new Color (1, 1, 1), 1);
		});

		// Overlays the cursor on top of the surface
		picker_surface_overlay = new Gtk.Overlay ();
		picker_surface_overlay.AddOverlay (picker_surface);
		picker_surface_overlay.AddOverlay (picker_surface_cursor);
		picker_surface_overlay.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);

		picker_surface_box.Append (picker_surface_overlay);

		// Show Value toggle for hue sat picker surface
		picker_surface_option_draw_value = new CheckboxOption (spacing, true, Translations.GetString ("Show Value"));
		picker_surface_option_draw_value.button.OnToggled += (o, e) => {
			picker_surface_option_draw_value.Toggle ();
			UpdateView ();
		};
		picker_surface_box.Append (picker_surface_option_draw_value);

		#endregion


		// Handles the ColorPickerSliders + Hex entry.
		#region SliderAndHex

		sliders_box = new Gtk.Box { Spacing = spacing };
		sliders_box.SetOrientation (Orientation.Vertical);

		var hexBox = new Gtk.Box { Spacing = spacing };

		hex_entry_add_alpha = new CheckboxOption (spacing, true, Translations.GetString ("Add Alpha"));
		hex_entry_add_alpha.button.OnToggled += (sender, args) => {
			hex_entry_add_alpha.Toggle ();
			UpdateView ();
		};

		hexBox.Append (new Label { Label_ = Translations.GetString ("Hex"), WidthRequest = 50 });
		hex_entry = new Entry { Text_ = CurrentColor.ToHex (hex_entry_add_alpha.state), MaxWidthChars = 10 };
		hex_entry.OnChanged ((o, e) => {
			if (GetFocus ()?.Parent == hex_entry) {
				CurrentColor = ColorExtensions.FromHex (hex_entry.GetText ()) ?? CurrentColor;
				UpdateView ();
			}
		});

		hexBox.Append (hex_entry);

		hexBox.Append (hex_entry_add_alpha);


		sliders_box.Append (hexBox);

		hue_cps = new ColorPickerSlider (360, Translations.GetString ("Hue"), CurrentColor.GetHsv ().h, this, cps_padding_width, cps_width);
		hue_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.SetHsv (hue: args.value);
			UpdateView ();
		};
		hue_cps.gradient.SetDrawFunc ((area, context, width, height) =>
			ColorPickerSlider.DrawGradient (context, width, height, cps_padding_width, cps_padding_height, new Color[] {
				CurrentColor.SetHsv (hue: 0),
				CurrentColor.SetHsv (hue: 60),
				CurrentColor.SetHsv (hue: 120),
				CurrentColor.SetHsv (hue: 180),
				CurrentColor.SetHsv (hue: 240),
				CurrentColor.SetHsv (hue: 300),
				CurrentColor.SetHsv (hue: 360)
			}));
		sliders_box.Append (hue_cps);

		sat_cps = new ColorPickerSlider (100, Translations.GetString ("Sat"), CurrentColor.GetHsv ().s * 100.0, this, cps_padding_width, cps_width);
		sat_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.SetHsv (sat: args.value / 100.0);
			UpdateView ();
		};
		sat_cps.gradient.SetDrawFunc ((area, context, width, height) =>
			ColorPickerSlider.DrawGradient (context, width, height, cps_padding_width, cps_padding_height, new Color[] {
				CurrentColor.SetHsv (sat: 0),
				CurrentColor.SetHsv (sat: 1)
			}));
		sliders_box.Append (sat_cps);


		val_cps = new ColorPickerSlider (100, Translations.GetString ("Value"), CurrentColor.GetHsv ().v * 100.0, this, cps_padding_width, cps_width);
		val_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.SetHsv (value: args.value / 100.0);
			UpdateView ();
		};
		val_cps.gradient.SetDrawFunc ((area, context, width, height) =>
			ColorPickerSlider.DrawGradient (context, width, height, cps_padding_width, cps_padding_height, new Color[] {
				CurrentColor.SetHsv (value: 0),
				CurrentColor.SetHsv (value: 1)
			}));
		sliders_box.Append (val_cps);

		sliders_box.Append (new Gtk.Separator ());

		r_cps = new ColorPickerSlider (255, Translations.GetString ("Red"), CurrentColor.R * 255.0, this, cps_padding_width, cps_width);
		r_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.SetRgba (r: args.value / 255.0);
			UpdateView ();
		};
		r_cps.gradient.SetDrawFunc ((area, context, width, height) =>
			ColorPickerSlider.DrawGradient (context, width, height, cps_padding_width, cps_padding_height,
				new Color[] { CurrentColor.SetRgba (r: 0), CurrentColor.SetRgba (r: 1) }));

		sliders_box.Append (r_cps);
		g_cps = new ColorPickerSlider (255, Translations.GetString ("Green"), CurrentColor.G * 255.0, this, cps_padding_width, cps_width);
		g_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.SetRgba (g: args.value / 255.0);
			UpdateView ();
		};
		g_cps.gradient.SetDrawFunc ((area, context, width, height) =>
			ColorPickerSlider.DrawGradient (context, width, height, cps_padding_width, cps_padding_height,
				new Color[] { CurrentColor.SetRgba (g: 0), CurrentColor.SetRgba (g: 1) }));
		sliders_box.Append (g_cps);
		b_cps = new ColorPickerSlider (255, Translations.GetString ("Blue"), CurrentColor.B * 255.0, this, cps_padding_width, cps_width);
		b_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.SetRgba (b: args.value / 255.0);
			UpdateView ();
		};
		b_cps.gradient.SetDrawFunc ((area, context, width, height) =>
			ColorPickerSlider.DrawGradient (context, width, height, cps_padding_width, cps_padding_height,
				new Color[] { CurrentColor.SetRgba (b: 0), CurrentColor.SetRgba (b: 1) }));
		sliders_box.Append (b_cps);
		sliders_box.Append (new Gtk.Separator ());
		a_cps = new ColorPickerSlider (255, Translations.GetString ("Alpha"), CurrentColor.A * 255.0, this, cps_padding_width, cps_width);
		a_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.SetRgba (a: args.value / 255.0);
			UpdateView ();
		};
		a_cps.gradient.SetDrawFunc ((area, context, width, height) =>
			ColorPickerSlider.DrawGradient (context, width, height, cps_padding_width, cps_padding_height,
				new Color[] { CurrentColor.SetRgba (a: 0), CurrentColor.SetRgba (a: 1) }));
		sliders_box.Append (a_cps);

		#endregion


		#region Swatch
		swatch_box = new Gtk.Box { Spacing = spacing };
		swatch_box.SetOrientation (Orientation.Vertical);


		// 90% taken from SatusBarColorPaletteWidget

		swatch_recent = new DrawingArea ();
		swatch_recent.WidthRequest = 500;
		swatch_recent.HeightRequest = StatusBarColorPaletteWidget.SWATCH_SIZE * StatusBarColorPaletteWidget.PALETTE_ROWS;

		swatch_recent.SetDrawFunc ((area, g, width, height) => {
			var recent = PintaCore.Palette.RecentlyUsedColors;
			var recent_cols = PintaCore.Palette.MaxRecentlyUsedColor / StatusBarColorPaletteWidget.PALETTE_ROWS;
			var recent_palette_rect = new RectangleD (0, 0, StatusBarColorPaletteWidget.SWATCH_SIZE * recent_cols,
				StatusBarColorPaletteWidget.SWATCH_SIZE * StatusBarColorPaletteWidget.PALETTE_ROWS);

			for (var i = 0; i < recent.Count (); i++)
				g.FillRectangle (StatusBarColorPaletteWidget.GetSwatchBounds (i, recent_palette_rect, true), recent.ElementAt (i));
		});

		swatch_box.Append (swatch_recent);


		swatch_palette = new DrawingArea ();

		swatch_palette.WidthRequest = 500;
		swatch_palette.HeightRequest = StatusBarColorPaletteWidget.SWATCH_SIZE * StatusBarColorPaletteWidget.PALETTE_ROWS;

		swatch_palette.SetDrawFunc ((area, g, width, height) => {
			var palette_rect = new RectangleD (0, 0,
				width - StatusBarColorPaletteWidget.PALETTE_MARGIN,
				StatusBarColorPaletteWidget.SWATCH_SIZE * StatusBarColorPaletteWidget.PALETTE_ROWS);

			var palette = PintaCore.Palette.CurrentPalette;
			for (var i = 0; i < palette.Count; i++)
				g.FillRectangle (StatusBarColorPaletteWidget.GetSwatchBounds (i, palette_rect), palette[i]);
		});
		swatch_box.Append (swatch_palette);

		if (!show_swatches)
			swatch_box.SetVisible (false);

		#endregion


		#region Mouse Handler

		var click_gesture = Gtk.GestureClick.New ();
		click_gesture.SetButton (0); // Listen for all mouse buttons.
		click_gesture.OnPressed += (_, e) => {
			PointD absPos = new PointD (e.X, e.Y);
			PointD relPos;

			if (picker_surface.IsMouseInDrawingArea (this, absPos, out relPos)) {
				mouse_on_picker_surface = true;
				SetColorFromPickerSurface (new PointD (e.X, e.Y));
			} else

			if (swatch_box.Visible && swatch_recent.IsMouseInDrawingArea (this, absPos, out relPos)) {
				var recent_index = StatusBarColorPaletteWidget.GetSwatchAtLocation (relPos, new RectangleD (), true);

				if (recent_index >= 0) {
					CurrentColor = PintaCore.Palette.RecentlyUsedColors.ElementAt (recent_index);
					UpdateView ();
				}
			} else

			if (swatch_box.Visible && swatch_palette.IsMouseInDrawingArea (this, absPos, out relPos)) {
				var index = StatusBarColorPaletteWidget.GetSwatchAtLocation (relPos, new RectangleD ());

				if (index >= 0) {
					CurrentColor = PintaCore.Palette.CurrentPalette[index];
					UpdateView ();
				}
			}
		};
		click_gesture.OnReleased += (_, e) => {
			mouse_on_picker_surface = false;
		};
		AddController (click_gesture);

		var motion_controller = Gtk.EventControllerMotion.New ();
		motion_controller.OnMotion += (_, args) => {
			if (mouse_on_picker_surface)
				SetColorFromPickerSurface (new PointD (args.X, args.Y));
		};
		AddController (motion_controller);

		#endregion

		Gtk.Box mainVbox = new () { Spacing = spacing };
		mainVbox.SetOrientation (Gtk.Orientation.Vertical);

		top_box.Append (color_display_box);
		top_box.Append (picker_surface_box);
		top_box.Append (sliders_box);


		mainVbox.Append (top_box);
		if(!small_mode)
			mainVbox.Append (swatch_box);

		Title = Translations.GetString (window_title);
		TransientFor = chrome_manager.MainWindow;
		Modal = false;
		IconName = Resources.Icons.ImageResizeCanvas;
		DefaultWidth = 1;
		DefaultHeight = 1;

		var contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (margins);
		contentArea.Append (mainVbox);
	}

	public ColorPickerDialog (ChromeManager chrome, Color[] palette, int currentColorIndex, bool continuous = false, string title = "Color Picker", bool showSwatches = false)
	{
		original_colors = palette;
		colors = (Color[]) palette.Clone ();
		color_index = currentColorIndex;
		chrome_manager = chrome;
		window_title = title;
		show_swatches = showSwatches;
		if (continuous)
			Modal = false;
		else
			Modal = true;
		Setup ();

		// Handles on active / off active
		// When user clicks off the color picker, we assign the color picker values to the palette
		// we only do this on off active because otherwise the recent color palette would be spammed
		// every time the color changes
		this.OnNotify += (sender, args) => {
			if (args.Pspec.GetName () == "is-active" && continuous) {
				if (!IsActive) {
					if (PintaCore.Palette.PrimaryColor != colors[0])
						PintaCore.Palette.PrimaryColor = colors[0];
					if (PintaCore.Palette.SecondaryColor != colors[1])
						PintaCore.Palette.SecondaryColor = colors[1];
				}
			}
		};

		this.SetDefaultResponse (Gtk.ResponseType.Cancel);
	}

	public void UpdateView ()
	{
		// Redraw picker surfaces
		picker_surface_cursor.QueueDraw ();
		picker_surface.QueueDraw ();

		// Redraw cps
		var hsv = CurrentColor.GetHsv ();

		hue_cps.SetValue (hsv.h);
		sat_cps.SetValue (hsv.s * 100.0);
		val_cps.SetValue (hsv.v * 100.0);

		r_cps.SetValue (CurrentColor.R * 255.0);
		g_cps.SetValue (CurrentColor.G * 255.0);
		b_cps.SetValue (CurrentColor.B * 255.0);
		a_cps.SetValue (CurrentColor.A * 255.0);


		// Update hex
		if (GetFocus ()?.Parent != hex_entry)
			hex_entry.SetText (CurrentColor.ToHex (hex_entry_add_alpha.state));

		// Redraw palette displays
		foreach (var display in color_displays) {
			display.QueueDraw ();
		}
	}



	private void DrawPaletteDisplay (Context g, Color c)
	{
		int xy = palette_display_border_thickness;
		int wh = palette_display_size - palette_display_border_thickness * 2;
		g.Antialias = Antialias.None;

		// make checker pattern
		if (c.A != 1) {
			g.FillRectangle (new RectangleD (xy, xy, wh, wh), new Color (1, 1, 1));
			g.FillRectangle (new RectangleD (xy, xy, wh / 2, wh / 2), new Color (.8, .8, .8));
			g.FillRectangle (new RectangleD (xy + wh / 2, xy + wh / 2, wh / 2, wh / 2), new Color (.8, .8, .8));
		}

		g.FillRectangle (new RectangleD (xy, xy, wh, wh), c);
		g.DrawRectangle (new RectangleD (xy, xy, wh, wh), new Color (0, 0, 0), palette_display_border_thickness);
	}

	private void DrawColorSurface (Context g)
	{
		int rad = picker_surface_radius;
		int x0 = picker_surface_padding;
		int y0 = picker_surface_padding;
		int draw_width = picker_surface_radius * 2;
		int draw_height = picker_surface_radius * 2;
		int x1 = x0 + picker_surface_padding;
		int y1 = y0 + picker_surface_padding;
		PointI center = new PointI (rad, rad);

		if (picker_surface_type == ColorSurfaceType.HueAndSat) {
			int stride = draw_width * 4;

			Span<byte> data = stackalloc byte[draw_height * stride];

			for (int y = 0; y < draw_height; y++) {
				for (int x = 0; x < draw_width; x++) {
					PointI pxl = new PointI (x, y);
					PointI vec = pxl - center;
					if (vec.Magnitude () <= rad) {
						var h = (MathF.Atan2 (vec.Y, -vec.X) + MathF.PI) / (2f * MathF.PI) * 360f;

						var s = Math.Min (vec.Magnitude () / rad, 1);

						double v = 1;
						if (picker_surface_option_draw_value.state)
							v = CurrentColor.GetHsv ().v;

						var c = ColorExtensions.FromHsv (h, s, v);

						double a = 1;
						var d = rad - vec.Magnitude ();
						if (d < 1) {
							a = d;
						}

						data[(y * stride) + (x * 4) + 0] = (byte) (c.R * 255);
						data[(y * stride) + (x * 4) + 1] = (byte) (c.G * 255);
						data[(y * stride) + (x * 4) + 2] = (byte) (c.B * 255);
						data[(y * stride) + (x * 4) + 3] = (byte) (a * 255);
					} else {
						data[(y * stride) + (x * 4) + 0] = (byte) (0);
						data[(y * stride) + (x * 4) + 1] = (byte) (0);
						data[(y * stride) + (x * 4) + 2] = (byte) (0);
						data[(y * stride) + (x * 4) + 3] = (byte) (0);
					}
				}
			}

			var img = MemoryTexture.New (draw_width, draw_height, MemoryFormat.R8g8b8a8, Bytes.New (data), (UIntPtr) stride).ToSurface ();
			g.SetSourceSurface (img, picker_surface_padding, picker_surface_padding);
			g.Paint ();
		} else if (picker_surface_type == ColorSurfaceType.SatAndVal) {
			int stride = draw_width * 3;

			Span<byte> data = stackalloc byte[draw_height * stride];

			for (int y = 0; y < draw_height; y++) {
				double s = 1.0 - (double) y / (draw_height - 1);
				for (int x = 0; x < draw_width; x++) {
					double v = (double) x / (draw_width - 1);
					var c = ColorExtensions.FromHsv (CurrentColor.GetHsv ().h, s, v);
					data[(y * stride) + (x * 3) + 0] = (byte) (c.R * 255);
					data[(y * stride) + (x * 3) + 1] = (byte) (c.G * 255);
					data[(y * stride) + (x * 3) + 2] = (byte) (c.B * 255);
				}
			}

			var img = MemoryTexture.New (draw_width, draw_height, MemoryFormat.R8g8b8, Bytes.New (data), (UIntPtr) stride).ToSurface ();
			g.SetSourceSurface (img, picker_surface_padding, picker_surface_padding);
			g.Paint ();
		}
	}

	// Takes in HSV values as tuple (h,s,v) and returns the position of that color in the picker surface.
	private PointD HsvToPickerLocation (ColorExtensions.Hsv hsv, int radius)
	{
		if (picker_surface_type == ColorSurfaceType.HueAndSat) {
			var rad = hsv.h * (Math.PI / 180.0);
			var mult = radius;
			var mag = hsv.s * mult;
			var x = Math.Cos (rad) * mag;
			var y = Math.Sin (rad) * mag;
			return new PointD (x, -y);
		} else if (picker_surface_type == ColorSurfaceType.SatAndVal) {
			int size = radius * 2;
			var x = hsv.v * (size - 1);
			var y = size - hsv.s * (size - 1);
			return new PointD (x - radius, y - radius);
		}

		return new PointD (0, 0);
	}

	void SetColorFromPickerSurface (PointD point)
	{
		picker_surface.TranslateCoordinates (this, picker_surface_padding, picker_surface_padding, out var x, out var y);
		PointI centre = new PointI (picker_surface_radius, picker_surface_radius);
		PointI cursor = new PointI ((int) (point.X - x), (int) (point.Y - y));

		PointI vecCursor = cursor - centre;

		if (picker_surface_type == ColorSurfaceType.HueAndSat) {
			var hue = (Math.Atan2 (vecCursor.Y, -vecCursor.X) + Math.PI) / (2f * Math.PI) * 360f;

			var sat = Math.Min (vecCursor.Magnitude () / picker_surface_radius, 1);

			CurrentColor = CurrentColor.SetHsv (hue: hue, sat: sat);
		} else if (picker_surface_type == ColorSurfaceType.SatAndVal) {
			int size = picker_surface_radius * 2;
			if (cursor.X > size - 1)
				cursor = cursor with { X = size - 1 };
			if (cursor.X < 0)
				cursor = cursor with { X = 0 };
			if (cursor.Y > size - 1)
				cursor = cursor with { Y = size - 1 };
			if (cursor.Y < 0)
				cursor = cursor with { Y = 0 };
			double s = 1f - (double) cursor.Y / (size - 1);
			double v = (double) cursor.X / (size - 1);
			CurrentColor = CurrentColor.SetHsv (sat: s, value: v);
		}
		UpdateView ();
	}
}
