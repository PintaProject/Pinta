using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

// used for the right hand side sliders
// uses a label, scale, and entry
// then hides the scale and draws over it
// with a drawingarea
public sealed class ColorPickerSlider : Gtk.Box
{
	public sealed class OnChangeValueArgs (string senderName, double value) : EventArgs
	{
		public string SenderName { get; } = senderName;
		public double Value { get; } = value;
	}

	public readonly record struct Settings (
		int Max,
		string Text, // required
		double InitialValue,
		Gtk.Window TopWindow, // required
		int SliderPaddingWidth,
		int SliderPaddingHeight,
		int SliderWidth,
		int MaxWidthChars);

	private readonly Settings settings;
	private readonly Gtk.Window top_window;

	private readonly Gtk.Scale slider_control;
	private readonly Gtk.Entry input_field;
	private readonly Gtk.Overlay slider_overlay;
	private readonly Gtk.DrawingArea cursor_area;

	public Gtk.DrawingArea Gradient { get; }

	public event EventHandler<OnChangeValueArgs>? OnValueChange;

	public ColorPickerSlider (Settings settings)
	{
		Gtk.Label label = new () { WidthRequest = 50 };
		label.SetLabel (settings.Text);

		Gtk.Entry inputField = new () {
			MaxWidthChars = settings.MaxWidthChars,
			WidthRequest = 50,
			Hexpand = false,
		};
		inputField.SetText (Convert.ToInt32 (settings.InitialValue).ToString ());
		inputField.OnChanged ((o, e) => {

			// see SetValue about suppression
			if (suppress_event)
				return;

			string t = inputField.GetText ();
			bool success = double.TryParse (t, CultureInfo.InvariantCulture, out double parsed);

			if (parsed > settings.Max) {
				parsed = settings.Max;
				inputField.SetText (Convert.ToInt32 (parsed).ToString ());
			}

			if (!success)
				return;

			OnChangeValueArgs e2 = new (
				senderName: label.GetLabel (),
				value: parsed);

			OnValueChange?.Invoke (this, e2);
		});

		Gtk.Scale sliderControl = new () {
			WidthRequest = settings.SliderWidth,
			Opacity = 0,
		};
		sliderControl.SetOrientation (Gtk.Orientation.Horizontal);
		sliderControl.SetAdjustment (Gtk.Adjustment.New (0, 0, settings.Max + 1, 1, 1, 1));
		sliderControl.SetValue (settings.InitialValue);
		sliderControl.OnChangeValue += (sender, args) => {

			OnChangeValueArgs e = new (
				senderName: label.GetLabel (),
				value: args.Value);

			inputField.SetText (e.Value.ToString (CultureInfo.InvariantCulture));
			OnValueChange?.Invoke (this, e);
			return false;
		};

		Gtk.DrawingArea cursorArea = new ();
		cursorArea.SetSizeRequest (settings.SliderWidth, this.GetHeight ());
		cursorArea.SetDrawFunc ((area, context, width, height) => {

			int outlineWidth = 2;

			double currentPosition = sliderControl.GetValue () / settings.Max * (width - 2 * settings.SliderPaddingWidth) + settings.SliderPaddingWidth;

			ReadOnlySpan<PointD> cursorPoly = [
				new (currentPosition, height / 2),
				new (currentPosition + 4, 3 * height / 4),
				new (currentPosition + 4, height - outlineWidth / 2),
				new (currentPosition - 4, height - outlineWidth / 2),
				new (currentPosition - 4, 3 * height / 4),
				new (currentPosition, height / 2),
			];

			context.LineWidth = outlineWidth;

			context.DrawPolygonal (
				cursorPoly,
				new Color (0, 0, 0),
				LineCap.Butt);

			context.FillPolygonal (
				cursorPoly,
				new Color (1, 1, 1));
		});

		Gtk.DrawingArea gradient = new ();
		gradient.SetSizeRequest (settings.SliderWidth, this.GetHeight ());

		Gtk.Overlay sliderOverlay = new () {
			WidthRequest = settings.SliderWidth,
			HeightRequest = this.GetHeight (),
		};
		sliderOverlay.AddOverlay (gradient);
		sliderOverlay.AddOverlay (cursorArea);
		sliderOverlay.AddOverlay (sliderControl);

		// --- Initialization (Gtk.Box)

		Append (label);
		Append (sliderOverlay);
		Append (inputField);

		// --- References to keep

		top_window = settings.TopWindow;

		cursor_area = cursorArea;
		slider_control = sliderControl;
		slider_overlay = sliderOverlay;
		input_field = inputField;

		Gradient = gradient;

		this.settings = settings;
	}

	public void SetSliderWidth (int sliderWidth)
	{
		slider_control.WidthRequest = sliderWidth;
		Gradient.SetSizeRequest (sliderWidth, this.GetHeight ());
		cursor_area.SetSizeRequest (sliderWidth, this.GetHeight ());
		slider_overlay.WidthRequest = sliderWidth;
	}

	private bool suppress_event = false;
	public void SetValue (double val)
	{
		slider_control.SetValue (val);
		// Make sure we do not set the text if we are editing it right now
		// This is the only reason top_window is passed in as an arg, and despite my best efforts I cannot find a way
		// to get that info from GTK programmatically.
		if (top_window.GetFocus ()?.Parent != input_field) {
			// hackjob
			// prevents OnValueChange from firing when we change the value internally
			// because OnValueChange eventually calls SetValue so it causes a stack overflow
			suppress_event = true;
			input_field.SetText (Convert.ToInt32 (val).ToString ());
		}
		Gradient.QueueDraw ();
		cursor_area.QueueDraw ();
		suppress_event = false;
	}

	public void DrawGradient (Context context, int width, int height, Color[] colors)
	{
		context.Antialias = Antialias.None;

		int draw_w = width - settings.SliderPaddingWidth * 2;
		int draw_h = height - settings.SliderPaddingHeight * 2;

		int x1 = settings.SliderPaddingWidth + draw_w;
		int y1 = settings.SliderPaddingHeight + draw_h;

		int bsize = draw_h / 2;

		// Draw transparency background
		context.FillRectangle (
			new RectangleD (settings.SliderPaddingWidth, settings.SliderPaddingHeight, draw_w, draw_h),
			new Color (1, 1, 1));

		for (int x = settings.SliderPaddingWidth; x < x1; x += bsize * 2) {

			int bwidth = bsize;

			if (x + bsize > x1)
				bwidth = x1 - x;

			context.FillRectangle (
				new RectangleD (x, settings.SliderPaddingHeight, bwidth, bsize),
				new Color (.8, .8, .8));
		}

		for (int x = settings.SliderPaddingWidth + bsize; x < x1; x += bsize * 2) {

			int bwidth = bsize;

			if (x + bsize > x1)
				bwidth = x1 - x;

			context.FillRectangle (
				new RectangleD (x, settings.SliderPaddingHeight + draw_h / 2, bwidth, bsize),
				new Color (.8, .8, .8));
		}

		LinearGradient pat = new (
			x0: settings.SliderPaddingWidth,
			y0: settings.SliderPaddingHeight,
			x1: x1,
			y1: y1);

		for (int i = 0; i < colors.Length; i++)
			pat.AddColorStop (i / (double) (colors.Length - 1), colors[i]);

		context.Rectangle (
			settings.SliderPaddingWidth,
			settings.SliderPaddingHeight,
			draw_w,
			draw_h);

		context.SetSource (pat);
		context.Fill ();
	}

}

public sealed class ColorPickerDialog : Gtk.Dialog
{
	private readonly Gtk.Box top_box;
	private readonly Gtk.Box swatch_box;

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
	private readonly Gtk.CheckButton picker_surface_option_draw_value;

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

	private readonly ImmutableArray<Color> original_colors;
	public Color[] Colors { get; private set; }

	private Color CurrentColor {
		get => Colors[color_index];
		set => Colors[color_index] = value;
	}

	private int spacing = 6;
	private int margins = 12;
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

		int pickerSurfaceDrawSize = (picker_surface_radius + picker_surface_padding) * 2;

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
	/// <param name="palette">Palette service.</param>
	/// <param name="adjustable">Palette of adjustable </param>
	/// <param name="currentColorIndex"></param>
	/// <param name="livePalette">Determines modality of the dialog and live palette behaviour. If true, dialog will not block rest of app and will update
	/// the current palette as the color is changed.</param>
	/// <param name="windowTitle">Title of the dialog.</param>
	public ColorPickerDialog (
		ChromeManager chrome,
		PaletteManager palette,
		Color[] adjustable,
		int currentColorIndex,
		bool livePalette,
		string windowTitle)
	{
		bool showWatches = !livePalette;

		ImmutableArray<Color> originalColors = [.. adjustable];

		original_colors = originalColors;
		Colors = [.. originalColors];
		color_index = currentColorIndex;

		// titlebar of color picker; mainly just contains the reset color button
		#region Titlebar

		Gtk.Button reset_button = new () { Label = Translations.GetString ("Reset Color") };
		reset_button.OnClicked += OnResetButtonClicked;

		Gtk.Button shrinkButton = new ();
		shrinkButton.OnClicked += OnShrinkButtonClicked;
		shrinkButton.SetIconName (
			small_mode
			? Resources.StandardIcons.WindowMaximize
			: Resources.StandardIcons.WindowMinimize);

		Gtk.Button okButton = new () { Label = Translations.GetString ("OK") };
		okButton.OnClicked += OnOkButtonClicked;
		okButton.AddCssClass (AdwaitaStyles.SuggestedAction);

		Gtk.Button cancelButton = new () { Label = Translations.GetString ("Cancel") };
		cancelButton.OnClicked += OnCancelButtonClicked;

		Gtk.HeaderBar titleBar = new ();
		titleBar.PackStart (reset_button);
		titleBar.PackStart (shrinkButton);
		titleBar.PackEnd (okButton);
		titleBar.PackEnd (cancelButton);
		titleBar.SetShowTitleButtons (false);

		this.SetTitlebar (titleBar);

		#endregion

		// Active palette contains the primary/secondary colors on the left of the color picker
		#region Color Display

		Gtk.Box colorDisplayBox = new () { Spacing = spacing };
		colorDisplayBox.SetOrientation (Gtk.Orientation.Vertical);

		Gtk.ListBox colorDisplayList = new ();

		if (Colors.Length > 1) {

			// technically this label would be wrong if you have >2 colors but there is no situation in which there are >2 colors in the palette
			string label = Translations.GetString ("Click to switch between primary and secondary color.");
			string shortcut_label = Translations.GetString ("Shortcut key");

			Gtk.Button colorDisplaySwap = new ();
			colorDisplaySwap.TooltipText = $"{label} {shortcut_label}: {"X"}";
			colorDisplaySwap.SetIconName (Resources.StandardIcons.EditSwap);
			colorDisplaySwap.OnClicked += (sender, args) => CycleColors ();
			colorDisplayBox.Append (colorDisplaySwap);
		}

		color_displays = new Gtk.DrawingArea[originalColors.Length];

		for (int i = 0; i < originalColors.Length; i++) {

			// This, unlike `i`, has a fixed value
			// which is what should be captured by the lambda
			int idx = i;

			Gtk.DrawingArea display = new ();
			display.SetSizeRequest (palette_display_size, palette_display_size);
			display.SetDrawFunc ((area, context, width, height) => DrawPaletteDisplay (context, Colors[idx]));

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

		colorDisplayBox.Append (colorDisplayList);

		#endregion

		// Picker surface; either is Hue & Sat (Color circle) or Sat & Val (Square)
		// Also contains picker surface switcher + options
		#region Picker Surface

		int pickerSurfaceDrawSize = (picker_surface_radius + picker_surface_padding) * 2;

		// Show Value toggle for hue sat picker surface

		Gtk.CheckButton pickerSurfaceOptionDrawValue = new () {
			Active = true,
			Label = Translations.GetString ("Show Value"),
		};
		pickerSurfaceOptionDrawValue.OnToggled += (o, e) => UpdateView ();

		picker_surface_option_draw_value = pickerSurfaceOptionDrawValue;


		// When Gir.Core supports it, this should probably be replaced with a toggle group.
		Gtk.ToggleButton pickerSurfaceHueSat = Gtk.ToggleButton.NewWithLabel (Translations.GetString ("Hue & Sat"));

		if (picker_surface_type == ColorSurfaceType.HueAndSat)
			pickerSurfaceHueSat.Toggle ();

		pickerSurfaceHueSat.OnToggled += (_, _) => {
			picker_surface_type = ColorSurfaceType.HueAndSat;
			pickerSurfaceOptionDrawValue.SetVisible (true);
			UpdateView ();
		};

		Gtk.ToggleButton pickerSurfaceSatVal = Gtk.ToggleButton.NewWithLabel (Translations.GetString ("Sat & Value"));

		if (picker_surface_type == ColorSurfaceType.SatAndVal)
			pickerSurfaceSatVal.Toggle ();

		pickerSurfaceSatVal.OnToggled += (_, _) => {
			picker_surface_type = ColorSurfaceType.SatAndVal;
			pickerSurfaceOptionDrawValue.SetVisible (false);
			UpdateView ();
		};

		pickerSurfaceHueSat.SetGroup (pickerSurfaceSatVal);

		Gtk.Box pickerSurfaceSelectorBox = new Gtk.Box {
			Spacing = spacing,
			WidthRequest = pickerSurfaceDrawSize,
			Homogeneous = true,
			Halign = Gtk.Align.Center,
		};
		pickerSurfaceSelectorBox.Append (pickerSurfaceHueSat);
		pickerSurfaceSelectorBox.Append (pickerSurfaceSatVal);

		Gtk.DrawingArea pickerSurface = new ();
		pickerSurface.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);
		pickerSurface.SetDrawFunc ((area, context, width, height) => DrawColorSurface (context));

		// Cursor handles the square in the picker surface displaying where your selected color is
		Gtk.DrawingArea pickerSurfaceCursor = new ();
		pickerSurfaceCursor.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);
		pickerSurfaceCursor.SetDrawFunc ((area, context, width, height) => {
			context.Antialias = Antialias.None;
			PointD loc = HsvToPickerLocation (CurrentColor.ToHsv (), picker_surface_radius);
			loc = new PointD (loc.X + picker_surface_radius + picker_surface_padding, loc.Y + picker_surface_radius + picker_surface_padding);

			context.FillRectangle (new RectangleD (loc.X - 5, loc.Y - 5, 10, 10), CurrentColor);
			context.DrawRectangle (new RectangleD (loc.X - 5, loc.Y - 5, 10, 10), new Color (0, 0, 0), 4);
			context.DrawRectangle (new RectangleD (loc.X - 5, loc.Y - 5, 10, 10), new Color (1, 1, 1), 1);
		});

		// Overlays the cursor on top of the surface
		Gtk.Overlay pickerSurfaceOverlay = new ();
		pickerSurfaceOverlay.AddOverlay (pickerSurface);
		pickerSurfaceOverlay.AddOverlay (pickerSurfaceCursor);
		pickerSurfaceOverlay.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);

		Gtk.Box pickerSurfaceBox = new Gtk.Box {
			Spacing = spacing,
			WidthRequest = pickerSurfaceDrawSize,
		};
		pickerSurfaceBox.SetOrientation (Gtk.Orientation.Vertical);
		pickerSurfaceBox.Append (pickerSurfaceSelectorBox);
		pickerSurfaceBox.Append (pickerSurfaceOverlay);
		pickerSurfaceBox.Append (pickerSurfaceOptionDrawValue);

		#endregion

		// Handles the ColorPickerSliders + Hex entry.

		#region SliderAndHex

		Gtk.Entry hexEntry = new () { Text_ = CurrentColor.ToHex (), MaxWidthChars = 10 };
		hexEntry.OnChanged ((o, e) => {
			if ((GetFocus ()?.Parent) != hexEntry) return;
			CurrentColor = Color.FromHex (hexEntry.GetText ()) ?? CurrentColor;
			UpdateView ();
		});

		Gtk.Box hexBox = new () { Spacing = spacing };
		hexBox.Append (new Gtk.Label { Label_ = Translations.GetString ("Hex"), WidthRequest = 50 });
		hexBox.Append (hexEntry);

		ColorPickerSlider.Settings cpsArgs = new () {
			Text = string.Empty,
			TopWindow = this,
			SliderPaddingHeight = CPS_PADDING_HEIGHT,
			SliderPaddingWidth = CPS_PADDING_WIDTH,
			SliderWidth = cps_width,
			MaxWidthChars = 3,
		};

		hue_cps = new ColorPickerSlider (cpsArgs with { Max = 360, Text = Translations.GetString ("Hue"), InitialValue = CurrentColor.ToHsv ().Hue, });
		hue_cps.Gradient.SetDrawFunc ((_, c, w, h) => hue_cps.DrawGradient (c, w, h, [
			CurrentColor.CopyHsv (hue: 0),
			CurrentColor.CopyHsv (hue: 60),
			CurrentColor.CopyHsv (hue: 120),
			CurrentColor.CopyHsv (hue: 180),
			CurrentColor.CopyHsv (hue: 240),
			CurrentColor.CopyHsv (hue: 300),
			CurrentColor.CopyHsv (hue: 360)
		]));
		hue_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.CopyHsv (hue: args.Value);
			UpdateView ();
		};

		sat_cps = new ColorPickerSlider (cpsArgs with { Max = 100, Text = Translations.GetString ("Sat"), InitialValue = CurrentColor.ToHsv ().Sat * 100.0, });
		sat_cps.Gradient.SetDrawFunc ((_, c, w, h) => sat_cps.DrawGradient (c, w, h, [
			CurrentColor.CopyHsv (sat: 0), CurrentColor.CopyHsv (sat: 1) ]));
		sat_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.CopyHsv (sat: args.Value / 100.0);
			UpdateView ();
		};

		val_cps = new ColorPickerSlider (cpsArgs with { Max = 100, Text = Translations.GetString ("Value"), InitialValue = CurrentColor.ToHsv ().Val * 100.0 });
		val_cps.Gradient.SetDrawFunc ((_, c, w, h) => val_cps.DrawGradient (c, w, h, [
			CurrentColor.CopyHsv (value: 0), CurrentColor.CopyHsv (value: 1) ]));
		val_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.CopyHsv (value: args.Value / 100.0);
			UpdateView ();
		};

		r_cps = new ColorPickerSlider (cpsArgs with { Max = 255, Text = Translations.GetString ("Red"), InitialValue = CurrentColor.R * 255.0, });
		r_cps.Gradient.SetDrawFunc ((_, c, w, h) => r_cps.DrawGradient (c, w, h, [
			CurrentColor with { R = 0 }, CurrentColor with { R = 1 } ]));
		r_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { R = args.Value / 255.0 };
			UpdateView ();
		};

		g_cps = new ColorPickerSlider (cpsArgs with { Max = 255, Text = Translations.GetString ("Green"), InitialValue = CurrentColor.G * 255.0, });
		g_cps.Gradient.SetDrawFunc ((_, c, w, h) => g_cps.DrawGradient (c, w, h, [
			CurrentColor with { G = 0 }, CurrentColor with { G = 1 } ]));
		g_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { G = args.Value / 255.0 };
			UpdateView ();
		};

		b_cps = new ColorPickerSlider (cpsArgs with { Max = 255, Text = Translations.GetString ("Blue"), InitialValue = CurrentColor.B * 255.0 });
		b_cps.Gradient.SetDrawFunc ((_, c, w, h) => b_cps.DrawGradient (c, w, h, [
			CurrentColor with { B = 0 }, CurrentColor with { B = 1 } ]));
		b_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { B = args.Value / 255.0 };
			UpdateView ();
		};

		a_cps = new ColorPickerSlider (cpsArgs with { Max = 255, Text = Translations.GetString ("Alpha"), InitialValue = CurrentColor.A * 255.0 });
		a_cps.Gradient.SetDrawFunc ((_, c, w, h) => a_cps.DrawGradient (c, w, h, [
			CurrentColor with { A = 0 }, CurrentColor with { A = 1 } ]));
		a_cps.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { A = args.Value / 255.0 };
			UpdateView ();
		};

		Gtk.Box slidersBox = new () { Spacing = spacing };
		slidersBox.SetOrientation (Gtk.Orientation.Vertical);
		slidersBox.Append (hexBox);
		slidersBox.Append (hue_cps);
		slidersBox.Append (sat_cps);
		slidersBox.Append (val_cps);
		slidersBox.Append (new Gtk.Separator ());
		slidersBox.Append (r_cps);
		slidersBox.Append (g_cps);
		slidersBox.Append (b_cps);
		slidersBox.Append (new Gtk.Separator ());
		slidersBox.Append (a_cps);

		#endregion

		#region Swatch

		// 90% taken from SatusBarColorPaletteWidget
		// todo: merge both

		Gtk.DrawingArea swatchRecent = new () {
			WidthRequest = 500,
			HeightRequest = PaletteWidget.SWATCH_SIZE * PaletteWidget.PALETTE_ROWS,
		};
		swatchRecent.SetDrawFunc ((area, g, width, height) => {

			var recent = palette.RecentlyUsedColors;
			int recent_cols = palette.MaxRecentlyUsedColor / PaletteWidget.PALETTE_ROWS;

			RectangleD recent_palette_rect = new (
				0,
				0,
				PaletteWidget.SWATCH_SIZE * recent_cols,
				PaletteWidget.SWATCH_SIZE * PaletteWidget.PALETTE_ROWS);

			for (int i = 0; i < recent.Count; i++)
				g.FillRectangle (PaletteWidget.GetSwatchBounds (palette, i, recent_palette_rect, true), recent.ElementAt (i));
		});

		Gtk.DrawingArea swatchPalette = new () {
			WidthRequest = 500,
			HeightRequest = PaletteWidget.SWATCH_SIZE * PaletteWidget.PALETTE_ROWS,
		};
		swatchPalette.SetDrawFunc ((area, g, width, height) => {

			RectangleD palette_rect = new (
				0,
				0,
				width - PaletteWidget.PALETTE_MARGIN,
				PaletteWidget.SWATCH_SIZE * PaletteWidget.PALETTE_ROWS);

			Palette currentPalette = palette.CurrentPalette;

			for (int i = 0; i < currentPalette.Count; i++)
				g.FillRectangle (PaletteWidget.GetSwatchBounds (palette, i, palette_rect), currentPalette[i]);
		});

		Gtk.Box swatchBox = new () { Spacing = spacing };
		swatchBox.SetOrientation (Gtk.Orientation.Vertical);
		swatchBox.Append (swatchRecent);
		swatchBox.Append (swatchPalette);
		swatchBox.SetVisible (showWatches);

		#endregion

		#region Mouse Handler

		Gtk.GestureClick click_gesture = Gtk.GestureClick.New ();
		click_gesture.SetButton (0); // Listen for all mouse buttons.
		click_gesture.OnPressed += (_, e) => {

			PointD absPos = new (e.X, e.Y);

			if (pickerSurface.IsMouseInDrawingArea (this, absPos, out PointD rel1)) {

				mouse_on_picker_surface = true;
				SetColorFromPickerSurface (new PointD (e.X, e.Y));

				return;
			}

			if (swatchBox.Visible && swatchRecent.IsMouseInDrawingArea (this, absPos, out PointD rel2)) {

				int recent_index = PaletteWidget.GetSwatchAtLocation (palette, rel2, new RectangleD (), true);

				if (recent_index < 0)
					return;

				CurrentColor = palette.RecentlyUsedColors.ElementAt (recent_index);

				UpdateView ();

				return;

			}

			if (swatchBox.Visible && swatchPalette.IsMouseInDrawingArea (this, absPos, out PointD rel3)) {

				int index = PaletteWidget.GetSwatchAtLocation (palette, rel3, new RectangleD ());

				if (index < 0)
					return;

				CurrentColor = palette.CurrentPalette[index];
				UpdateView ();
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

		// Top part of the color picker.
		// Includes palette, color surface, sliders/hex
		// Basically, the not-swatches
		Gtk.Box topBox = new () { Spacing = spacing };
		topBox.Append (colorDisplayBox);
		topBox.Append (pickerSurfaceBox);
		topBox.Append (slidersBox);

		mainVbox.Append (topBox);
		if (!small_mode)
			mainVbox.Append (swatchBox);

		Title = Translations.GetString (windowTitle);
		TransientFor = chrome.MainWindow;
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
		this.OnNotify += (_, args) => {

			if (args.Pspec.GetName () != "is-active" || !livePalette)
				return;

			if (IsActive) {
				this.SetOpacity (1f);
				return;
			}

			this.SetOpacity (0.85f);

			if (palette.PrimaryColor != Colors[0])
				palette.PrimaryColor = Colors[0];

			if (palette.SecondaryColor != Colors[1])
				palette.SecondaryColor = Colors[1];
		};

		if (livePalette) {

			void PrimaryChangeHandler (object? sender, EventArgs _)
			{
				Colors[0] = ((PaletteManager) sender!).PrimaryColor;
				UpdateView ();
			}

			void SecondaryChangeHandler (object? sender, EventArgs _)
			{
				Colors[1] = ((PaletteManager) sender!).SecondaryColor;
				UpdateView ();
			}

			palette.PrimaryColorChanged += PrimaryChangeHandler;
			palette.SecondaryColorChanged += SecondaryChangeHandler;

			this.OnResponse += (_, _) => {
				palette.PrimaryColorChanged -= PrimaryChangeHandler;
				palette.SecondaryColorChanged -= SecondaryChangeHandler;
			};
		}

		this.SetDefaultResponse (Gtk.ResponseType.Cancel);

		// --- References to keep

		color_display_box = colorDisplayBox;
		hex_entry = hexEntry;
		picker_surface = pickerSurface;
		picker_surface_box = pickerSurfaceBox;
		picker_surface_cursor = pickerSurfaceCursor;
		picker_surface_overlay = pickerSurfaceOverlay;
		picker_surface_selector_box = pickerSurfaceSelectorBox;
		show_swatches = showWatches;
		sliders_box = slidersBox;
		swatch_box = swatchBox;
		top_box = topBox;
	}

	private void OnResetButtonClicked (Gtk.Button button, EventArgs args)
	{
		Colors = [.. original_colors];
		UpdateView ();
	}

	private void OnShrinkButtonClicked (Gtk.Button button, EventArgs args)
	{
		//var contentArea = this.GetContentAreaBox ();
		//contentArea.RemoveAll ();
		SetSmallMode (!small_mode);
		button.SetIconName (
			small_mode
			? Resources.StandardIcons.WindowMaximize
			: Resources.StandardIcons.WindowMinimize);
	}

	private void OnOkButtonClicked (Gtk.Button button, EventArgs args)
	{
		this.Response ((int) Gtk.ResponseType.Ok);
		this.Close ();
	}

	private void OnCancelButtonClicked (Gtk.Button button, EventArgs args)
	{
		this.Response ((int) Gtk.ResponseType.Ok); // TODO: Is this the right response?
		this.Close ();
	}

	private void CycleColors ()
	{
		Color swap = Colors[0];

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
		foreach (var display in color_displays)
			display.QueueDraw ();
	}

	private void DrawPaletteDisplay (Context g, Color c)
	{
		int xy = palette_display_border_thickness;
		int wh = palette_display_size - palette_display_border_thickness * 2;

		g.Antialias = Antialias.None;

		// make checker pattern
		if (c.A != 1) {

			g.FillRectangle (
				new RectangleD (xy, xy, wh, wh),
				new Color (1, 1, 1));

			g.FillRectangle (
				new RectangleD (xy, xy, wh / 2, wh / 2),
				new Color (.8, .8, .8));

			g.FillRectangle (
				new RectangleD (xy + wh / 2, xy + wh / 2, wh / 2, wh / 2),
				new Color (.8, .8, .8));
		}

		g.FillRectangle (
			new RectangleD (xy, xy, wh, wh),
			c);

		g.DrawRectangle (
			new RectangleD (xy, xy, wh, wh),
			new Color (0, 0, 0), palette_display_border_thickness);
	}

	private void DrawColorSurface (Context g)
	{
		int draw_width = picker_surface_radius * 2;
		int draw_height = picker_surface_radius * 2;

		ImageSurface img = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			draw_width,
			draw_height);

		Span<ColorBgra> data = img.GetPixelData ();

		if (picker_surface_type == ColorSurfaceType.HueAndSat) {

			int rad = picker_surface_radius;

			PointI center = new (rad, rad);

			for (int y = 0; y < draw_height; y++) {
				for (int x = 0; x < draw_width; x++) {

					PointI pxl = new (x, y);
					PointI vec = pxl - center;

					if (vec.Magnitude () > rad) continue;

					double h = (MathF.Atan2 (vec.Y, -vec.X) + MathF.PI) / (2f * MathF.PI) * 360f;
					double s = Math.Min (vec.Magnitude () / rad, 1);
					double v = picker_surface_option_draw_value.Active ? CurrentColor.ToHsv ().Val : 1;

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
		switch (picker_surface_type) {
			case ColorSurfaceType.HueAndSat: {
					double rad = hsv.Hue * (Math.PI / 180.0);
					int mult = radius;
					double mag = hsv.Sat * mult;
					double x = Math.Cos (rad) * mag;
					double y = Math.Sin (rad) * mag;
					return new (x, -y);
				}

			case ColorSurfaceType.SatAndVal: {
					int size = radius * 2;
					double x = hsv.Val * (size - 1);
					double y = size - hsv.Sat * (size - 1);
					return new (x - radius, y - radius);
				}
			default:
				return PointD.Zero;
		}
	}

	void SetColorFromPickerSurface (PointD point)
	{
		picker_surface.TranslateCoordinates (
			this,
			picker_surface_padding,
			picker_surface_padding,
			out double x,
			out double y);

		PointI cursor = new (
			X: (int) (point.X - x),
			Y: (int) (point.Y - y));

		if (picker_surface_type == ColorSurfaceType.HueAndSat) {

			PointI centre = new (
				picker_surface_radius,
				picker_surface_radius);

			PointI vecCursor = cursor - centre;

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
