using System;
using System.Globalization;
using System.Linq;
using Cairo;
using Pinta.Core;
using Color = Cairo.Color;
using Context = Cairo.Context;

namespace Pinta.Gui.Widgets;

// used for the right hand side sliders
// uses a label, scale, and entry
// then hides the scale and draws over it
// with a drawingarea
public sealed class ColorPickerSlider : Gtk.Box
{
	public sealed class OnChangeValArgs : EventArgs
	{
		public string sender_name = "";
		public double value;
	}

	public struct ColorPickerSliderArgs ()
	{
		public int Upper { get; set; }
		public required string Text { get; set; }
		public double Val { get; set; }
		public required Gtk.Window TopWindow { get; set; }
		public int SliderPaddingWidth { get; set; }
		public int SliderPaddingHeight { get; set; }
		public int SliderWidth { get; set; }
		public int MaxWidthChars { get; set; } = 3;
	}

	private readonly ColorPickerSliderArgs args;
	private readonly Gtk.Window top_window;

	private readonly Gtk.Label label = new ();
	private readonly Gtk.Scale slider = new ();
	private readonly Gtk.Entry input = new ();
	private readonly Gtk.Overlay slider_overlay = new ();
	private readonly Gtk.DrawingArea cursor = new ();

	public Gtk.DrawingArea Gradient { get; private set; } = new ();

	public event EventHandler<OnChangeValArgs>? OnValueChange;

	private readonly int max_val;

	public ColorPickerSlider (ColorPickerSliderArgs args)
	{
		this.args = args;
		top_window = args.TopWindow;
		max_val = args.Upper;

		label.SetLabel (args.Text);
		label.WidthRequest = 50;
		slider.SetOrientation (Gtk.Orientation.Horizontal);
		slider.SetAdjustment (Gtk.Adjustment.New (0, 0, max_val + 1, 1, 1, 1));

		slider.WidthRequest = args.SliderWidth;
		slider.SetValue (args.Val);
		slider.Opacity = 0;
		Gradient.SetSizeRequest (args.SliderWidth, this.GetHeight ());
		cursor.SetSizeRequest (args.SliderWidth, this.GetHeight ());

		cursor.SetDrawFunc ((area, context, width, height) => {
			int outlineWidth = 2;

			double curpos = slider.GetValue () / max_val * (width - 2 * args.SliderPaddingWidth) + args.SliderPaddingWidth;

			ReadOnlySpan<PointD> cursorPoly = stackalloc PointD[] {
				new PointD (curpos, height / 2),
				new PointD (curpos + 4, 3 * height / 4),
				new PointD (curpos + 4, height - outlineWidth / 2),
				new PointD (curpos - 4, height - outlineWidth / 2),
				new PointD (curpos - 4, 3 * height / 4),
				new PointD (curpos, height / 2)
			};

			context.LineWidth = outlineWidth;
			context.DrawPolygonal (cursorPoly, new Color (0, 0, 0), LineCap.Butt);
			context.FillPolygonal (cursorPoly, new Color (1, 1, 1));
		});

		slider_overlay.WidthRequest = args.SliderWidth;
		slider_overlay.HeightRequest = this.GetHeight ();


		slider_overlay.AddOverlay (Gradient);
		slider_overlay.AddOverlay (cursor);
		slider_overlay.AddOverlay (slider);

		input.MaxWidthChars = args.MaxWidthChars;
		input.WidthRequest = 50;
		input.Hexpand = false;
		input.SetText (Convert.ToInt32 (args.Val).ToString ());
		this.Append (label);
		this.Append (slider_overlay);
		this.Append (input);

		slider.OnChangeValue += (sender, args) => {

			OnChangeValArgs e = new ();
			e.sender_name = label.GetLabel ();
			e.value = args.Value;

			input.SetText (e.value.ToString (CultureInfo.InvariantCulture));
			OnValueChange?.Invoke (this, e);
			return false;
		};

		input.OnChanged ((o, e) => {
			// see SetValue about suppression
			if (suppress_event)
				return;

			string t = o.GetText ();
			double val;
			bool success = double.TryParse (t, CultureInfo.InvariantCulture, out val);

			if (val > max_val) {
				val = max_val;
				input.SetText (Convert.ToInt32 (val).ToString ());
			}


			if (success) {
				OnChangeValArgs e2 = new ();
				e2.sender_name = label.GetLabel ();
				e2.value = val;
				OnValueChange?.Invoke (this, e2);
			}
		});
	}

	public void SetSliderWidth (int sliderWidth)
	{
		slider.WidthRequest = sliderWidth;
		Gradient.SetSizeRequest (sliderWidth, this.GetHeight ());
		cursor.SetSizeRequest (sliderWidth, this.GetHeight ());
		slider_overlay.WidthRequest = sliderWidth;
	}

	private bool suppress_event = false;
	public void SetValue (double val)
	{
		slider.SetValue (val);
		// Make sure we do not set the text if we are editing it right now
		// This is the only reason top_window is passed in as an arg, and despite my best efforts I cannot find a way
		// to get that info from GTK programmatically.
		if (top_window.GetFocus ()?.Parent != input) {
			// hackjob
			// prevents OnValueChange from firing when we change the value internally
			// because OnValueChange eventually calls SetValue so it causes a stack overflow
			suppress_event = true;
			input.SetText (Convert.ToInt32 (val).ToString ());
		}
		Gradient.QueueDraw ();
		cursor.QueueDraw ();
		suppress_event = false;
	}

	public void DrawGradient (Context context, int width, int height, Color[] colors)
	{
		context.Antialias = Antialias.None;

		int draw_w = width - args.SliderPaddingWidth * 2;
		int draw_h = height - args.SliderPaddingHeight * 2;

		int x1 = args.SliderPaddingWidth + draw_w;
		int y1 = args.SliderPaddingHeight + draw_h;

		int bsize = draw_h / 2;

		// Draw transparency background
		context.FillRectangle (new RectangleD (args.SliderPaddingWidth, args.SliderPaddingHeight, draw_w, draw_h), new Color (1, 1, 1));

		for (int x = args.SliderPaddingWidth; x < x1; x += bsize * 2) {
			int bwidth = bsize;

			if (x + bsize > x1)
				bwidth = x1 - x;

			context.FillRectangle (new RectangleD (x, args.SliderPaddingHeight, bwidth, bsize), new Color (.8, .8, .8));
		}
		for (int x = args.SliderPaddingWidth + bsize; x < x1; x += bsize * 2) {
			int bwidth = bsize;

			if (x + bsize > x1)
				bwidth = x1 - x;

			context.FillRectangle (new RectangleD (x, args.SliderPaddingHeight + draw_h / 2, bwidth, bsize), new Color (.8, .8, .8));
		}

		LinearGradient pat = new (
			x0: args.SliderPaddingWidth,
			y0: args.SliderPaddingHeight,
			x1: x1,
			y1: y1);

		for (int i = 0; i < colors.Length; i++)
			pat.AddColorStop (i / (double) (colors.Length - 1), colors[i]);

		context.Rectangle (args.SliderPaddingWidth, args.SliderPaddingHeight, draw_w, draw_h);
		context.SetSource (pat);
		context.Fill ();
	}

}

public sealed class CheckboxOption : Gtk.Box
{
	public bool State { get; private set; }
	public Gtk.CheckButton Button { get; }
	public Gtk.Label Label { get; }
	public CheckboxOption (int spacing, bool active, string text)
	{
		this.Spacing = spacing;
		Button = new Gtk.CheckButton ();
		State = active;
		Button.Active = State;
		this.Append (Button);

		Label = new Gtk.Label { Label_ = text };
		this.Append (Label);
	}

	public void Toggle ()
	{
		State = !State;
		Button.Active = State;
	}
}


public sealed class ColorPickerDialog : Gtk.Dialog
{

	private readonly Gtk.Box top_box;
	private readonly Gtk.Box swatch_box;

	private readonly Gtk.HeaderBar title_bar;

	private readonly Gtk.Box color_display_box;

	// palette
	private int palette_display_size = 50;
	private readonly int palette_display_border_thickness = 3;
	private readonly Gtk.DrawingArea[] color_displays;

	// color surface
	private int picker_surface_radius = 200 / 2;
	private readonly int picker_surface_padding = 10;
	private readonly Gtk.Box picker_surface_selector_box;
	private readonly Gtk.Box picker_surface_box;
	private readonly Gtk.Overlay picker_surface_overlay;
	private readonly Gtk.DrawingArea picker_surface;
	private readonly Gtk.DrawingArea picker_surface_cursor;

	enum ColorSurfaceType
	{
		HueAndSat,
		SatAndVal,
	}

	private ColorSurfaceType picker_surface_type = ColorSurfaceType.HueAndSat;
	// color surface options
	private bool mouse_on_picker_surface = false;
	private readonly CheckboxOption picker_surface_option_draw_value;

	// swatches
	private readonly Gtk.DrawingArea swatch_recent;
	private readonly Gtk.DrawingArea swatch_palette;

	// hex + sliders
	private readonly Gtk.Entry hex_entry;

	private const int CPS_PADDING_HEIGHT = 10;
	private const int CPS_PADDING_WIDTH = 14;
	private int cps_width = 200;
	private readonly Gtk.Box sliders_box;
	private readonly ColorPickerSlider hue_cps;
	private readonly ColorPickerSlider sat_cps;
	private readonly ColorPickerSlider val_cps;

	private readonly ColorPickerSlider r_cps;
	private readonly ColorPickerSlider g_cps;
	private readonly ColorPickerSlider b_cps;

	private readonly ColorPickerSlider a_cps;

	// common state
	private int color_index = 0;
	public Color[] Colors { get; private set; }
	private readonly Color[] original_colors;

	private Color CurrentColor {
		get => Colors[color_index];
		set => Colors[color_index] = value;
	}

	private int spacing = 6;
	private int margins = 12;
	private readonly ChromeManager chrome_manager;
	private readonly string window_title;
	private bool small_mode = false;
	private readonly bool show_swatches = false;

	private void SetSmallMode (bool isSmallMode)
	{
		// incredibly silly workaround
		// but if this is not done, it seems Wayland will assume the window will never be transparent, and thus opacity will break
		this.SetOpacity (0.995f);
		small_mode = isSmallMode;
		if (isSmallMode) {
			spacing = 2;
			margins = 6;
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
			if (show_swatches)
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
			picker_surface_selector_box.SetOrientation (Gtk.Orientation.Vertical);
		else
			picker_surface_selector_box.SetOrientation (Gtk.Orientation.Horizontal);

		hue_cps.SetSliderWidth (cps_width);
		sat_cps.SetSliderWidth (cps_width);
		val_cps.SetSliderWidth (cps_width);
		r_cps.SetSliderWidth (cps_width);
		g_cps.SetSliderWidth (cps_width);
		b_cps.SetSliderWidth (cps_width);
		a_cps.SetSliderWidth (cps_width);

		sliders_box.Spacing = spacing;

		this.GetContentAreaBox ().SetAllMargins (margins);

		DefaultWidth = 1;
		DefaultHeight = 1;
	}

	/// <param name="chrome">Current Chrome Manager.</param>
	/// <param name="palette">Palette of adjustable </param>
	/// <param name="currentColorIndex"></param>
	/// <param name="livePalette">Determines modality of the dialog and live palette behaviour. If true, dialog will not block rest of app and will update
	/// the current palette as the color is changed.</param>
	/// <param name="title">Title of the dialog.</param>
	public ColorPickerDialog (ChromeManager chrome, Color[] palette, int currentColorIndex, bool livePalette, string title)
	{
		original_colors = palette;
		Colors = (Color[]) palette.Clone ();
		color_index = currentColorIndex;
		chrome_manager = chrome;
		window_title = title;
		show_swatches = !livePalette;


		// Top part of the color picker.
		// Includes palette, color surface, sliders/hex
		// Basically, the not-swatches
		top_box = new Gtk.Box { Spacing = spacing };


		// titlebar of color picker; mainly just contains the reset color button
		#region Titlebar

		title_bar = new Gtk.HeaderBar ();
		Gtk.Button reset_button = new ();
		reset_button.Label = Translations.GetString ("Reset Color");
		reset_button.OnClicked += (button, args) => {
			Colors = (Color[]) original_colors.Clone ();
			UpdateView ();
		};
		title_bar.PackStart (reset_button);

		Gtk.Button shrinkButton = new ();
		if (small_mode)
			shrinkButton.SetIconName (Resources.StandardIcons.WindowMaximize);
		else
			shrinkButton.SetIconName (Resources.StandardIcons.WindowMinimize);
		shrinkButton.OnClicked += (sender, args) => {
			var contentArea = this.GetContentAreaBox ();
			//contentArea.RemoveAll ();
			SetSmallMode (!small_mode);
			if (small_mode)
				shrinkButton.SetIconName (Resources.StandardIcons.WindowMaximize);
			else
				shrinkButton.SetIconName (Resources.StandardIcons.WindowMinimize);
		};

		title_bar.PackStart (shrinkButton);

		Gtk.Button ok_button = new () { Label = Translations.GetString ("OK") };
		ok_button.OnClicked += (sender, args) => { this.Response ((int) Gtk.ResponseType.Ok); this.Close (); };
		ok_button.AddCssClass (AdwaitaStyles.SuggestedAction);

		Gtk.Button cancel_button = new () { Label = Translations.GetString ("Cancel") };
		cancel_button.OnClicked += (sender, args) => { this.Response ((int) Gtk.ResponseType.Ok); this.Close (); };

		title_bar.PackEnd (ok_button);
		title_bar.PackEnd (cancel_button);

		title_bar.SetShowTitleButtons (false);
		this.SetTitlebar (title_bar);

		#endregion


		// Active palette contains the primary/secondary colors on the left of the color picker
		#region Color Display

		color_display_box = new Gtk.Box { Spacing = spacing };
		color_display_box.SetOrientation (Gtk.Orientation.Vertical);

		Gtk.ListBox colorDisplayList = new ();

		if (Colors.Length > 1) {
			Gtk.Button colorDisplaySwap = new ();
			// technically this label would be wrong if you have >2 colors but there is no situation in which there are >2 colors in the palette
			string label = Translations.GetString ("Click to switch between primary and secondary color.");
			string shortcut_label = Translations.GetString ("Shortcut key");
			colorDisplaySwap.TooltipText = $"{label} {shortcut_label}: {"X"}";
			colorDisplaySwap.SetIconName (Resources.StandardIcons.EditSwap);
			colorDisplaySwap.OnClicked += (sender, args) => {
				CycleColors ();
			};

			color_display_box.Append (colorDisplaySwap);
		}

		color_displays = new Gtk.DrawingArea[original_colors.Length];
		for (int i = 0; i < original_colors.Length; i++) {
			Gtk.DrawingArea display = new ();
			display.SetSizeRequest (palette_display_size, palette_display_size);
			int pos = i;
			display.SetDrawFunc ((area, context, width, height) => DrawPaletteDisplay (context, Colors[pos]));
			colorDisplayList.Append (display);
			color_displays[i] = display;
		}

		// Set initial selected row
		colorDisplayList.SetSelectionMode (Gtk.SelectionMode.Single);
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

		int pickerSurfaceDrawSize = (picker_surface_radius + picker_surface_padding) * 2;

		picker_surface_box = new Gtk.Box { Spacing = spacing, WidthRequest = pickerSurfaceDrawSize };
		picker_surface_box.SetOrientation (Gtk.Orientation.Vertical);

		picker_surface_selector_box = new Gtk.Box { Spacing = spacing, WidthRequest = pickerSurfaceDrawSize };
		picker_surface_selector_box.Homogeneous = true;
		picker_surface_selector_box.Halign = Gtk.Align.Center;

		// Show Value toggle for hue sat picker surface
		picker_surface_option_draw_value = new CheckboxOption (spacing, true, Translations.GetString ("Show Value"));
		picker_surface_option_draw_value.Button.OnToggled += (o, e) => {
			picker_surface_option_draw_value.Toggle ();
			UpdateView ();
		};

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
			var loc = HsvToPickerLocation (CurrentColor.ToHsv (), picker_surface_radius);
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

		picker_surface_box.Append (picker_surface_option_draw_value);

		#endregion


		// Handles the ColorPickerSliders + Hex entry.
		#region SliderAndHex

		sliders_box = new Gtk.Box { Spacing = spacing };
		sliders_box.SetOrientation (Gtk.Orientation.Vertical);

		var hexBox = new Gtk.Box { Spacing = spacing };

		hexBox.Append (new Gtk.Label { Label_ = Translations.GetString ("Hex"), WidthRequest = 50 });
		hex_entry = new Gtk.Entry { Text_ = CurrentColor.ToHex (), MaxWidthChars = 10 };
		hex_entry.OnChanged ((o, e) => {
			if (GetFocus ()?.Parent == hex_entry) {
				CurrentColor = Color.FromHex (hex_entry.GetText ()) ?? CurrentColor;
				UpdateView ();
			}
		});

		hexBox.Append (hex_entry);


		sliders_box.Append (hexBox);


		ColorPickerSlider.ColorPickerSliderArgs cpsArgs = new () {
			Text = string.Empty,
			TopWindow = this,
			SliderPaddingHeight = CPS_PADDING_HEIGHT,
			SliderPaddingWidth = CPS_PADDING_WIDTH,
			SliderWidth = cps_width,
		};


		hue_cps = new ColorPickerSlider (cpsArgs with { Upper = 360, Text = Translations.GetString ("Hue"), Val = CurrentColor.ToHsv ().Hue, });
		hue_cps.Gradient.SetDrawFunc ((_, c, w, h) => hue_cps.DrawGradient (c, w, h, new Color[] {
			CurrentColor.CopyHsv (hue: 0),
			CurrentColor.CopyHsv (hue: 60),
			CurrentColor.CopyHsv (hue: 120),
			CurrentColor.CopyHsv (hue: 180),
			CurrentColor.CopyHsv (hue: 240),
			CurrentColor.CopyHsv (hue: 300),
			CurrentColor.CopyHsv (hue: 360)
		}));
		hue_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.CopyHsv (hue: args.value);
			UpdateView ();
		};
		sliders_box.Append (hue_cps);


		sat_cps = new ColorPickerSlider (cpsArgs with { Upper = 100, Text = Translations.GetString ("Sat"), Val = CurrentColor.ToHsv ().Sat * 100.0, });
		sat_cps.Gradient.SetDrawFunc ((_, c, w, h) => sat_cps.DrawGradient (c, w, h, new Color[] {
			CurrentColor.CopyHsv (sat: 0), CurrentColor.CopyHsv (sat: 1) }));
		sat_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.CopyHsv (sat: args.value / 100.0);
			UpdateView ();
		};
		sliders_box.Append (sat_cps);


		val_cps = new ColorPickerSlider (cpsArgs with { Upper = 100, Text = Translations.GetString ("Value"), Val = CurrentColor.ToHsv ().Val * 100.0 });
		val_cps.Gradient.SetDrawFunc ((_, c, w, h) => val_cps.DrawGradient (c, w, h, new Color[] {
			CurrentColor.CopyHsv (value: 0), CurrentColor.CopyHsv (value: 1) }));
		val_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.CopyHsv (value: args.value / 100.0);
			UpdateView ();
		};
		sliders_box.Append (val_cps);


		sliders_box.Append (new Gtk.Separator ());


		r_cps = new ColorPickerSlider (cpsArgs with { Upper = 255, Text = Translations.GetString ("Red"), Val = CurrentColor.R * 255.0, });
		r_cps.Gradient.SetDrawFunc ((_, c, w, h) => r_cps.DrawGradient (c, w, h, new Color[] {
			CurrentColor with { R = 0 }, CurrentColor with { R = 1 } }));
		r_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { R = args.value / 255.0 };
			UpdateView ();
		};
		sliders_box.Append (r_cps);


		g_cps = new ColorPickerSlider (cpsArgs with { Upper = 255, Text = Translations.GetString ("Green"), Val = CurrentColor.G * 255.0, });
		g_cps.Gradient.SetDrawFunc ((_, c, w, h) => g_cps.DrawGradient (c, w, h, new Color[] {
			CurrentColor with { G = 0 }, CurrentColor with { G = 1 } }));
		g_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { G = args.value / 255.0 };
			UpdateView ();
		};
		sliders_box.Append (g_cps);


		b_cps = new ColorPickerSlider (cpsArgs with { Upper = 255, Text = Translations.GetString ("Blue"), Val = CurrentColor.B * 255.0 });
		b_cps.Gradient.SetDrawFunc ((_, c, w, h) => b_cps.DrawGradient (c, w, h, new Color[] {
			CurrentColor with { B = 0 }, CurrentColor with { B = 1 } }));
		b_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { B = args.value / 255.0 };
			UpdateView ();
		};
		sliders_box.Append (b_cps);


		sliders_box.Append (new Gtk.Separator ());
		a_cps = new ColorPickerSlider (cpsArgs with { Upper = 255, Text = Translations.GetString ("Alpha"), Val = CurrentColor.A * 255.0 });
		a_cps.Gradient.SetDrawFunc ((_, c, w, h) => a_cps.DrawGradient (c, w, h, new Color[] {
			CurrentColor with { A = 0 }, CurrentColor with { A = 1 } }));
		a_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { A = args.value / 255.0 };
			UpdateView ();
		};
		sliders_box.Append (a_cps);
		#endregion


		#region Swatch
		swatch_box = new Gtk.Box { Spacing = spacing };
		swatch_box.SetOrientation (Gtk.Orientation.Vertical);


		// 90% taken from SatusBarColorPaletteWidget
		// todo: merge both

		swatch_recent = new Gtk.DrawingArea ();
		swatch_recent.WidthRequest = 500;
		swatch_recent.HeightRequest = StatusBarColorPaletteWidget.SWATCH_SIZE * StatusBarColorPaletteWidget.PALETTE_ROWS;

		swatch_recent.SetDrawFunc ((area, g, width, height) => {

			var recent = PintaCore.Palette.RecentlyUsedColors;
			int recent_cols = PintaCore.Palette.MaxRecentlyUsedColor / StatusBarColorPaletteWidget.PALETTE_ROWS;

			RectangleD recent_palette_rect = new (
				0,
				0,
				StatusBarColorPaletteWidget.SWATCH_SIZE * recent_cols,
				StatusBarColorPaletteWidget.SWATCH_SIZE * StatusBarColorPaletteWidget.PALETTE_ROWS);

			for (int i = 0; i < recent.Count; i++)
				g.FillRectangle (StatusBarColorPaletteWidget.GetSwatchBounds (i, recent_palette_rect, true), recent.ElementAt (i));
		});

		swatch_box.Append (swatch_recent);

		swatch_palette = new Gtk.DrawingArea ();

		swatch_palette.WidthRequest = 500;
		swatch_palette.HeightRequest = StatusBarColorPaletteWidget.SWATCH_SIZE * StatusBarColorPaletteWidget.PALETTE_ROWS;

		swatch_palette.SetDrawFunc ((area, g, width, height) => {

			RectangleD palette_rect = new (
				0,
				0,
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

			PointD absPos = new (e.X, e.Y);
			PointD relPos;

			if (picker_surface.IsMouseInDrawingArea (this, absPos, out relPos)) {
				mouse_on_picker_surface = true;
				SetColorFromPickerSurface (new PointD (e.X, e.Y));
			} else if (swatch_box.Visible && swatch_recent.IsMouseInDrawingArea (this, absPos, out relPos)) {
				var recent_index = StatusBarColorPaletteWidget.GetSwatchAtLocation (relPos, new RectangleD (), true);

				if (recent_index >= 0) {
					CurrentColor = PintaCore.Palette.RecentlyUsedColors.ElementAt (recent_index);
					UpdateView ();
				}
			} else if (swatch_box.Visible && swatch_palette.IsMouseInDrawingArea (this, absPos, out relPos)) {
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

		var keyboard_gesture = Gtk.EventControllerKey.New ();
		keyboard_gesture.OnKeyPressed += (_, e) => {
			if (e.GetKey ().Value == Gdk.Constants.KEY_x)
				CycleColors ();
			return true;
		};
		AddController (keyboard_gesture);

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
		if (!small_mode)
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


		// incredibly silly workaround
		// but if this is not done, it seems Wayland will assume the window will never be transparent, and thus opacity will break
		this.SetOpacity (0.995f);

		// Handles on active / off active
		// When user clicks off the color picker, we assign the color picker values to the palette
		// we only do this on off active because otherwise the recent color palette would be spammed
		// every time the color changes
		this.OnNotify += (sender, args) => {
			if (args.Pspec.GetName () == "is-active" && livePalette) {
				if (!IsActive) {
					this.SetOpacity (0.85f);
					if (PintaCore.Palette.PrimaryColor != Colors[0])
						PintaCore.Palette.PrimaryColor = Colors[0];
					if (PintaCore.Palette.SecondaryColor != Colors[1])
						PintaCore.Palette.SecondaryColor = Colors[1];
				} else {
					this.SetOpacity (1f);
				}
			}
		};

		if (livePalette) {
			EventHandler PrimaryChangeHandler = (sender, args) => {
				Colors[0] = ((PaletteManager) sender!).PrimaryColor;

				UpdateView ();
			};
			EventHandler SecondaryChangeHandler = (sender, args) => {
				Colors[1] = ((PaletteManager) sender!).SecondaryColor;
				UpdateView ();
			};

			PintaCore.Palette.PrimaryColorChanged += PrimaryChangeHandler;
			PintaCore.Palette.SecondaryColorChanged += SecondaryChangeHandler;

			this.OnResponse += (sender, args) => {
				PintaCore.Palette.PrimaryColorChanged -= PrimaryChangeHandler;
				PintaCore.Palette.SecondaryColorChanged -= SecondaryChangeHandler;
			};
		}


		this.SetDefaultResponse (Gtk.ResponseType.Cancel);
	}

	private void CycleColors ()
	{
		var swap = Colors[0];
		for (int i = 0; i < Colors.Length - 1; i++)
			Colors[i] = Colors[i + 1];
		Colors[^1] = swap;
		UpdateView ();
	}


	private void UpdateView ()
	{
		// Redraw picker surfaces
		picker_surface_cursor.QueueDraw ();
		picker_surface.QueueDraw ();

		// Redraw cps
		var hsv = CurrentColor.ToHsv ();

		hue_cps.SetValue (hsv.Hue);
		sat_cps.SetValue (hsv.Sat * 100.0);
		val_cps.SetValue (hsv.Val * 100.0);

		r_cps.SetValue (CurrentColor.R * 255.0);
		g_cps.SetValue (CurrentColor.G * 255.0);
		b_cps.SetValue (CurrentColor.B * 255.0);
		a_cps.SetValue (CurrentColor.A * 255.0);


		// Update hex
		if (GetFocus ()?.Parent != hex_entry)
			hex_entry.SetText (CurrentColor.ToHex ());

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

			ImageSurface img = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, draw_width, draw_height);
			Span<ColorBgra> data = img.GetPixelData ();

			for (int y = 0; y < draw_height; y++) {
				for (int x = 0; x < draw_width; x++) {

					PointI pxl = new PointI (x, y);
					PointI vec = pxl - center;

					if (vec.Magnitude () > rad) continue;

					double h = (MathF.Atan2 (vec.Y, -vec.X) + MathF.PI) / (2f * MathF.PI) * 360f;
					double s = Math.Min (vec.Magnitude () / rad, 1);
					double v = picker_surface_option_draw_value.State ? CurrentColor.ToHsv ().Val : 1;

					double d = rad - vec.Magnitude ();
					double a = d < 1 ? d : 1;

					Color c = Color.FromHsv (h, s, v, a);

					data[draw_width * y + x] = c.ToColorBgra ();
				}
			}

			img.MarkDirty ();
			g.SetSourceSurface (img, picker_surface_padding, picker_surface_padding);
			g.Paint ();
		} else if (picker_surface_type == ColorSurfaceType.SatAndVal) {

			ImageSurface img = CairoExtensions.CreateImageSurface (Cairo.Format.Argb32, draw_width, draw_height);
			Span<ColorBgra> data = img.GetPixelData ();

			for (int y = 0; y < draw_height; y++) {
				double s = 1.0 - (double) y / (draw_height - 1);
				for (int x = 0; x < draw_width; x++) {
					double v = (double) x / (draw_width - 1);
					Color c = Color.FromHsv (CurrentColor.ToHsv ().Hue, s, v);
					data[draw_width * y + x] = c.ToColorBgra ();
				}
			}

			img.MarkDirty ();
			g.SetSourceSurface (img, picker_surface_padding, picker_surface_padding);
			g.Paint ();
		}
	}

	// Takes in HSV values as tuple (h,s,v) and returns the position of that color in the picker surface.
	private PointD HsvToPickerLocation (HsvColor hsv, int radius)
	{
		if (picker_surface_type == ColorSurfaceType.HueAndSat) {
			double rad = hsv.Hue * (Math.PI / 180.0);
			int mult = radius;
			double mag = hsv.Sat * mult;
			double x = Math.Cos (rad) * mag;
			double y = Math.Sin (rad) * mag;
			return new PointD (x, -y);
		} else if (picker_surface_type == ColorSurfaceType.SatAndVal) {
			int size = radius * 2;
			double x = hsv.Val * (size - 1);
			double y = size - hsv.Sat * (size - 1);
			return new PointD (x - radius, y - radius);
		}

		return PointD.Zero;
	}

	void SetColorFromPickerSurface (PointD point)
	{
		picker_surface.TranslateCoordinates (
			this,
			picker_surface_padding,
			picker_surface_padding,
			out double x,
			out double y);

		PointI centre = new (picker_surface_radius, picker_surface_radius);
		PointI cursor = new ((int) (point.X - x), (int) (point.Y - y));

		PointI vecCursor = cursor - centre;

		if (picker_surface_type == ColorSurfaceType.HueAndSat) {
			double hue = (Math.Atan2 (vecCursor.Y, -vecCursor.X) + Math.PI) / (2f * Math.PI) * 360f;
			double sat = Math.Min (vecCursor.Magnitude () / picker_surface_radius, 1);
			CurrentColor = CurrentColor.CopyHsv (hue: hue, sat: sat);
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
			CurrentColor = CurrentColor.CopyHsv (sat: s, value: v);
		}
		UpdateView ();
	}
}
