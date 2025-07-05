using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ColorPickerDialog : Gtk.Dialog
{
	private readonly Gtk.Box top_box;
	private readonly Gtk.Box swatch_box;
	private readonly Gtk.Box color_display_box;

	private readonly Gtk.DrawingArea swatch_recent;
	private readonly Gtk.DrawingArea swatch_palette;

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
	private readonly PaletteManager palette;
	private const int CPS_PADDING_HEIGHT = 10;
	private const int CPS_PADDING_WIDTH = 14;
	private int slider_width = 200;
	private readonly Gtk.Box sliders_box;
	private readonly ColorPickerSlider hue_slider;
	private readonly ColorPickerSlider saturation_slider;
	private readonly ColorPickerSlider value_slider;

	private readonly ColorPickerSlider red_slider;
	private readonly ColorPickerSlider green_slider;
	private readonly ColorPickerSlider blue_slider;

	private readonly ColorPickerSlider alpha_slider;

	// common state
	private bool primary_selected; // TODO: Get rid of this

	private readonly ColorPick original_colors;
	public ColorPick Colors { get; private set; }

	private Color CurrentColor {
		get => GetTargeted ();
		set => SetTargeted (value);
	}

	private Color GetTargeted ()
	{
		return Colors switch {
			SingleColor singleColor => primary_selected ? singleColor.Color : throw new InvalidOperationException (),
			PaletteColors paletteColors => primary_selected ? paletteColors.Primary : paletteColors.Secondary,
			_ => throw new UnreachableException (),
		};
	}

	private void SetTargeted (Color color)
	{
		Colors = Colors switch {
			SingleColor singleColor =>
				primary_selected
				? singleColor with { Color = color }
				: throw new InvalidOperationException (),
			PaletteColors paletteColors =>
				primary_selected
				? paletteColors with { Primary = color }
				: paletteColors with { Secondary = color },
			_ => throw new UnreachableException (),
		};
	}

	private int spacing = 6;
	private int margins = 12;
	private bool small_mode = false;
	private readonly bool show_swatches = false;

	private void SetSmallMode (bool isSmallMode)
	{
		// incredibly silly workaround
		// but if this is not done, it seems Wayland will assume the window will never be transparent, and thus opacity will break
		SetOpacity (0.995f);
		small_mode = isSmallMode;
		if (isSmallMode) {
			spacing = 2;
			margins = 6;
			palette_display_size = 40;
			picker_surface_radius = 75;
			slider_width = 150;
			swatch_box.Visible = false;
		} else {
			spacing = 6;
			margins = 12;
			palette_display_size = 50;
			picker_surface_radius = 100;
			slider_width = 200;
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

		hue_slider.SetSliderWidth (slider_width);
		saturation_slider.SetSliderWidth (slider_width);
		value_slider.SetSliderWidth (slider_width);

		red_slider.SetSliderWidth (slider_width);
		green_slider.SetSliderWidth (slider_width);
		blue_slider.SetSliderWidth (slider_width);
		alpha_slider.SetSliderWidth (slider_width);

		sliders_box.Spacing = spacing;

		this.GetContentAreaBox ().SetAllMargins (margins);

		DefaultWidth = 1;
		DefaultHeight = 1;
	}

	private bool IsPrimary (int colorIndex) // TODO: Get rid of this
		=> colorIndex == 0;

	/// <param name="chrome">Current Chrome Manager.</param>
	/// <param name="palette">Palette service.</param>
	/// <param name="adjustable">Palette of adjustable </param>
	/// <param name="primarySelected"></param>
	/// <param name="livePalette">Determines modality of the dialog and live palette behaviour. If true, dialog will not block rest of app and will update
	/// the current palette as the color is changed.</param>
	/// <param name="windowTitle">Title of the dialog.</param>
	internal ColorPickerDialog (
		ChromeManager chrome,
		PaletteManager palette,
		ColorPick adjustable,
		bool primarySelected, // TODO: Get rid of this
		bool livePalette,
		string windowTitle)
	{
		primary_selected = true;

		bool showWatches = !livePalette;

		Gtk.Button resetButton = new () { Label = Translations.GetString ("Reset Color") };
		resetButton.OnClicked += OnResetButtonClicked;

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
		titleBar.PackStart (resetButton);
		titleBar.PackStart (shrinkButton);
		titleBar.PackEnd (okButton);
		titleBar.PackEnd (cancelButton);
		titleBar.SetShowTitleButtons (false);

		SetTitlebar (titleBar);

		// Active palette contains the primary/secondary colors on the left of the color picker
		#region Color Display

		Gtk.DrawingArea[] colorDisplays = CreateColorDisplays (adjustable);

		Gtk.ListBox colorDisplayList = new ();
		foreach (var colorDisplay in colorDisplays)
			colorDisplayList.Append (colorDisplay);
		// Set initial selected row
		colorDisplayList.SetSelectionMode (Gtk.SelectionMode.Single);
		colorDisplayList.SelectRow (colorDisplayList.GetRowAtIndex (primarySelected ? 0 : 1));

		// Handle on select; index 0 -> primary; index 1 -> secondary
		colorDisplayList.OnRowSelected += ((sender, args) => {
			int colorIndex = args.Row?.GetIndex () ?? 0;
			primary_selected = IsPrimary (colorIndex);
			UpdateView ();
		});

		Gtk.Box colorDisplayBox = new () { Spacing = spacing };
		colorDisplayBox.SetOrientation (Gtk.Orientation.Vertical);
		if (adjustable is PaletteColors paletteColors) {
			string label = Translations.GetString ("Click to switch between primary and secondary color.");
			string shortcut_label = Translations.GetString ("Shortcut key");
			Gtk.Button colorDisplaySwap = new () { TooltipText = $"{label} {shortcut_label}: {"X"}" };
			colorDisplaySwap.SetIconName (Resources.StandardIcons.EditSwap);
			colorDisplaySwap.OnClicked += (sender, args) => CycleColors ();
			colorDisplayBox.Append (colorDisplaySwap);
		}
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

		Colors = adjustable;

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

		Gtk.Entry hexEntry = new () {
			Text_ = CurrentColor.ToHex (),
			MaxWidthChars = 10,
		};
		hexEntry.OnChanged (HexEntry_OnChanged);

		Gtk.Label hexLabel = new () {
			Label_ = Translations.GetString ("Hex"),
			WidthRequest = 50,
		};

		Gtk.Box hexBox = new () { Spacing = spacing };
		hexBox.Append (hexLabel);
		hexBox.Append (hexEntry);

		ColorPickerSlider.Settings cpsArgs = new () {
			Text = string.Empty,
			TopWindow = this,
			SliderPaddingHeight = CPS_PADDING_HEIGHT,
			SliderPaddingWidth = CPS_PADDING_WIDTH,
			SliderWidth = slider_width,
			MaxWidthChars = 3,
		};

		ColorPickerSlider hueSlider = new (
			settings: cpsArgs with {
				Max = 360,
				Text = Translations.GetString ("Hue"),
				InitialValue = CurrentColor.ToHsv ().Hue,
			}
		);
		hueSlider.Gradient.SetDrawFunc (
			(_, c, w, h) => hueSlider.DrawGradient (
				c,
				w,
				h,
				ColorGradient.Create (
					startColor: CurrentColor.CopyHsv (hue: 0),
					endColor: CurrentColor.CopyHsv (hue: 360),
					startPosition: 0,
					endPosition: 360,
					new Dictionary<double, Color> {
						[60] = CurrentColor.CopyHsv (hue: 60),
						[120] = CurrentColor.CopyHsv (hue: 120),
						[180] = CurrentColor.CopyHsv (hue: 180),
						[240] = CurrentColor.CopyHsv (hue: 240),
						[300] = CurrentColor.CopyHsv (hue: 300),
					}
				)
			)
		);
		hueSlider.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.CopyHsv (hue: args.Value);
			UpdateView ();
		};

		ColorPickerSlider saturationSlider = new (
			settings: cpsArgs with {
				Max = 100,
				Text = Translations.GetString ("Sat"),
				InitialValue = CurrentColor.ToHsv ().Sat * 100.0,
			}
		);
		saturationSlider.Gradient.SetDrawFunc (
			(_, c, w, h) => saturationSlider.DrawGradient (
				c,
				w,
				h,
				ColorGradient.Create (
					CurrentColor.CopyHsv (sat: 0),
					CurrentColor.CopyHsv (sat: 1))
			)
		);
		saturationSlider.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.CopyHsv (sat: args.Value / 100.0);
			UpdateView ();
		};

		ColorPickerSlider valueSlider = new (
			settings: cpsArgs with {
				Max = 100,
				Text = Translations.GetString ("Value"),
				InitialValue = CurrentColor.ToHsv ().Val * 100.0,
			}
		);
		valueSlider.Gradient.SetDrawFunc (
			(_, c, w, h) => valueSlider.DrawGradient (
				c,
				w,
				h,
				ColorGradient.Create (
					CurrentColor.CopyHsv (value: 0),
					CurrentColor.CopyHsv (value: 1))
			)
		);
		valueSlider.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor.CopyHsv (value: args.Value / 100.0);
			UpdateView ();
		};

		ColorPickerSlider redSlider = new (
			settings: cpsArgs with {
				Max = 255,
				Text = Translations.GetString ("Red"),
				InitialValue = CurrentColor.R * 255.0,
			}
		);
		redSlider.Gradient.SetDrawFunc (
			(_, c, w, h) => redSlider.DrawGradient (
				c,
				w,
				h,
				ColorGradient.Create (
					CurrentColor with { R = 0 },
					CurrentColor with { R = 1 })
			)
		);
		redSlider.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { R = args.Value / 255.0 };
			UpdateView ();
		};

		ColorPickerSlider greenSlider = new (
			settings: cpsArgs with {
				Max = 255,
				Text = Translations.GetString ("Green"),
				InitialValue = CurrentColor.G * 255.0,
			}
		);
		greenSlider.Gradient.SetDrawFunc (
			(_, c, w, h) => greenSlider.DrawGradient (
				c,
				w,
				h,
				ColorGradient.Create (
					CurrentColor with { G = 0 },
					CurrentColor with { G = 1 })
			)
		);
		greenSlider.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { G = args.Value / 255.0 };
			UpdateView ();
		};

		ColorPickerSlider blueSlider = new (
			settings: cpsArgs with {
				Max = 255,
				Text = Translations.GetString ("Blue"),
				InitialValue = CurrentColor.B * 255.0,
			}
		);
		blueSlider.Gradient.SetDrawFunc (
			(_, c, w, h) => blueSlider.DrawGradient (
				c,
				w,
				h,
				ColorGradient.Create (
					CurrentColor with { B = 0 },
					CurrentColor with { B = 1 })
			)
		);
		blueSlider.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { B = args.Value / 255.0 };
			UpdateView ();
		};

		ColorPickerSlider alphaSlider = new (
			settings: cpsArgs with {
				Max = 255,
				Text = Translations.GetString ("Alpha"),
				InitialValue = CurrentColor.A * 255.0,
			}
		);
		alphaSlider.Gradient.SetDrawFunc (
			(_, c, w, h) => alphaSlider.DrawGradient (
				c,
				w,
				h,
				ColorGradient.Create (
					CurrentColor with { A = 0 },
					CurrentColor with { A = 1 })
			)
		);
		alphaSlider.OnValueChange += (sender, args) => {
			CurrentColor = CurrentColor with { A = args.Value / 255.0 };
			UpdateView ();
		};

		Gtk.Box slidersBox = new () { Spacing = spacing };
		slidersBox.SetOrientation (Gtk.Orientation.Vertical);
		slidersBox.Append (hexBox);
		slidersBox.Append (hueSlider);
		slidersBox.Append (saturationSlider);
		slidersBox.Append (valueSlider);
		slidersBox.Append (new Gtk.Separator ());
		slidersBox.Append (redSlider);
		slidersBox.Append (greenSlider);
		slidersBox.Append (blueSlider);
		slidersBox.Append (new Gtk.Separator ());
		slidersBox.Append (alphaSlider);

		// 90% taken from SatusBarColorPaletteWidget
		// todo: merge both

		Gtk.DrawingArea swatchRecent = new () {
			WidthRequest = 500,
			HeightRequest = PaletteWidget.SWATCH_SIZE * PaletteWidget.PALETTE_ROWS,
		};
		swatchRecent.SetDrawFunc (SwatchRecentDraw);

		Gtk.DrawingArea swatchPalette = new () {
			WidthRequest = 500,
			HeightRequest = PaletteWidget.SWATCH_SIZE * PaletteWidget.PALETTE_ROWS,
		};
		swatchPalette.SetDrawFunc (SwatchPaletteDraw);

		Gtk.Box swatchBox = new () { Spacing = spacing };
		swatchBox.SetOrientation (Gtk.Orientation.Vertical);
		swatchBox.Append (swatchRecent);
		swatchBox.Append (swatchPalette);
		swatchBox.SetVisible (showWatches);

		Gtk.GestureClick click_gesture = Gtk.GestureClick.New ();
		click_gesture.SetButton (0); // Listen for all mouse buttons.
		click_gesture.OnPressed += ClickGesture_OnPressed;
		click_gesture.OnReleased += ClickGesture_OnReleased;

		Gtk.EventControllerKey keyboard_gesture = Gtk.EventControllerKey.New ();
		keyboard_gesture.OnKeyPressed += KeyboardGesture_OnKeyPressed;

		Gtk.EventControllerMotion motion_controller = Gtk.EventControllerMotion.New ();
		motion_controller.OnMotion += MotionController_OnMotion;

		// Top part of the color picker.
		// Includes palette, color surface, sliders/hex
		// Basically, the not-swatches
		Gtk.Box topBox = new () { Spacing = spacing };
		topBox.Append (colorDisplayBox);
		topBox.Append (pickerSurfaceBox);
		topBox.Append (slidersBox);

		Gtk.Box mainVbox = new () { Spacing = spacing };
		mainVbox.SetOrientation (Gtk.Orientation.Vertical);
		mainVbox.Append (topBox);
		if (!small_mode)
			mainVbox.Append (swatchBox);

		Gtk.Box contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (margins);
		contentArea.Append (mainVbox);

		if (livePalette) {

			palette.PrimaryColorChanged += PrimaryChangeHandler;
			palette.SecondaryColorChanged += SecondaryChangeHandler;
			IsActivePropertyDefinition.Notify (this, ActiveWindowChangeHandler);
			this.OnResponse += ColorPickerDialog_OnResponse;
		}

		// --- Initialization (Gtk.Widget)

		// Mouse and keyboard handlers
		AddController (click_gesture);
		AddController (keyboard_gesture);
		AddController (motion_controller);

		// incredibly silly workaround
		// but if this is not done, it seems Wayland will assume the window will never be transparent, and thus opacity will break
		SetOpacity (0.995f);

		// --- Initialization (Gtk.Window)

		Title = Translations.GetString (windowTitle);
		TransientFor = chrome.MainWindow;
		Modal = false;
		IconName = Resources.Icons.ImageResizeCanvas;
		DefaultWidth = 1;
		DefaultHeight = 1;

		// --- Initialization (Gtk.Dialog)

		this.SetDefaultResponse (Gtk.ResponseType.Cancel);

		// --- References to keep

		primary_selected = primarySelected;
		original_colors = adjustable;

		hue_slider = hueSlider;
		saturation_slider = saturationSlider;
		value_slider = valueSlider;

		red_slider = redSlider;
		green_slider = greenSlider;
		blue_slider = blueSlider;
		alpha_slider = alphaSlider;

		color_displays = colorDisplays;
		color_display_box = colorDisplayBox;
		hex_entry = hexEntry;
		this.palette = palette;
		picker_surface = pickerSurface;
		picker_surface_box = pickerSurfaceBox;
		picker_surface_cursor = pickerSurfaceCursor;
		picker_surface_overlay = pickerSurfaceOverlay;
		picker_surface_selector_box = pickerSurfaceSelectorBox;
		show_swatches = showWatches;
		sliders_box = slidersBox;
		swatch_box = swatchBox;
		swatch_recent = swatchRecent;
		swatch_palette = swatchPalette;
		top_box = topBox;
	}

	Gtk.DrawingArea[] CreateColorDisplays (ColorPick pick)
	{
		switch (pick) {

			case SingleColor singleColors:

				Gtk.DrawingArea singleColorDisplay = new ();
				singleColorDisplay.SetSizeRequest (palette_display_size, palette_display_size);
				singleColorDisplay.SetDrawFunc ((area, context, width, height) => DrawPaletteDisplay (context, ((SingleColor) Colors).Color));

				return [singleColorDisplay];

			case PaletteColors paletteColors:

				Gtk.DrawingArea primaryColorDisplay = new ();
				primaryColorDisplay.SetSizeRequest (palette_display_size, palette_display_size);
				primaryColorDisplay.SetDrawFunc ((area, context, width, height) => DrawPaletteDisplay (context, ((PaletteColors) Colors).Primary));

				Gtk.DrawingArea secondaryColorDisplay = new ();
				secondaryColorDisplay.SetSizeRequest (palette_display_size, palette_display_size);
				secondaryColorDisplay.SetDrawFunc ((area, context, width, height) => DrawPaletteDisplay (context, ((PaletteColors) Colors).Secondary));

				return [primaryColorDisplay, secondaryColorDisplay];
			default:
				throw new UnreachableException ();
		}
	}

	void SwatchRecentDraw (Gtk.DrawingArea area, Context g, int width, int height)
	{
		var recent = palette.RecentlyUsedColors;
		int recent_cols = palette.MaxRecentlyUsedColor / PaletteWidget.PALETTE_ROWS;

		RectangleD recent_palette_rect = new (
			0,
			0,
			PaletteWidget.SWATCH_SIZE * recent_cols,
			PaletteWidget.SWATCH_SIZE * PaletteWidget.PALETTE_ROWS);

		for (int i = 0; i < recent.Count; i++)
			g.FillRectangle (PaletteWidget.GetSwatchBounds (palette, i, recent_palette_rect, true), recent.ElementAt (i));
	}

	void SwatchPaletteDraw (Gtk.DrawingArea area, Context g, int width, int height)
	{
		RectangleD paletteRect = new (
			0,
			0,
			width - PaletteWidget.PALETTE_MARGIN,
			PaletteWidget.SWATCH_SIZE * PaletteWidget.PALETTE_ROWS);

		Palette currentPalette = palette.CurrentPalette;

		for (int i = 0; i < currentPalette.Colors.Count; i++)
			g.FillRectangle (PaletteWidget.GetSwatchBounds (palette, i, paletteRect), currentPalette.Colors[i]);
	}

	void ColorPickerDialog_OnResponse (Gtk.Dialog _, ResponseSignalArgs args)
	{
		// Remove event handlers on exit (in particular, we don't want to handle the
		// 'is-active' property changing as the dialog is being closed (bug #1390)).

		Gtk.ResponseType response = (Gtk.ResponseType) args.ResponseId;

		// Don't attempt to remove the signals again when the dialog is deleted, which
		// triggers Gtk.ResponseType.DeleteEvent.

		if (response != Gtk.ResponseType.Cancel && response != Gtk.ResponseType.Ok)
			return;

		palette.PrimaryColorChanged -= PrimaryChangeHandler;
		palette.SecondaryColorChanged -= SecondaryChangeHandler;
		IsActivePropertyDefinition.Unnotify (this, ActiveWindowChangeHandler);
	}

	void ActiveWindowChangeHandler (object? _, NotifySignalArgs __)
	{
		if (Colors is not PaletteColors paletteColors)
			return;

		// Handles on active / off active
		// When user clicks off the color picker, we assign the color picker values to the palette
		// we only do this on off active because otherwise the recent color palette would be spammed
		// every time the color changes

		if (IsActive) {
			SetOpacity (1.0f);
			return;
		}

		SetOpacity (0.85f);

		if (palette.PrimaryColor != paletteColors.Primary)
			palette.PrimaryColor = paletteColors.Primary;

		if (palette.SecondaryColor != paletteColors.Secondary)
			palette.SecondaryColor = paletteColors.Secondary;
	}

	void PrimaryChangeHandler (object? sender, EventArgs _)
	{
		Color newPrimary = ((PaletteManager) sender!).PrimaryColor;
		Colors = Colors switch {
			SingleColor singleColor => singleColor with { Color = newPrimary },
			PaletteColors paletteColors => paletteColors with { Primary = newPrimary },
			_ => throw new UnreachableException (),
		};
		UpdateView ();
	}

	void SecondaryChangeHandler (object? sender, EventArgs _)
	{
		if (Colors is not PaletteColors paletteColors) return;
		Color newSecondary = ((PaletteManager) sender!).SecondaryColor;
		Colors = paletteColors with { Secondary = newSecondary };
		UpdateView ();
	}

	private void ClickGesture_OnPressed (
		Gtk.GestureClick _,
		Gtk.GestureClick.PressedSignalArgs e)
	{
		PointD absPos = new (e.X, e.Y);
		if (picker_surface.IsMouseInDrawingArea (this, absPos, out PointD _)) {

			mouse_on_picker_surface = true;
			SetColorFromPickerSurface (new PointD (e.X, e.Y));

			return;
		}

		if (swatch_box.Visible && swatch_recent.IsMouseInDrawingArea (this, absPos, out PointD rel2)) {

			int recent_index = PaletteWidget.GetSwatchAtLocation (palette, rel2, new RectangleD (), true);

			if (recent_index < 0)
				return;

			CurrentColor = palette.RecentlyUsedColors.ElementAt (recent_index);

			UpdateView ();

			return;

		}

		if (swatch_box.Visible && swatch_palette.IsMouseInDrawingArea (this, absPos, out PointD rel3)) {

			int index = PaletteWidget.GetSwatchAtLocation (palette, rel3, new RectangleD ());

			if (index < 0)
				return;

			CurrentColor = palette.CurrentPalette.Colors[index];
			UpdateView ();
		}
	}

	private void ClickGesture_OnReleased (
		Gtk.GestureClick _,
		Gtk.GestureClick.ReleasedSignalArgs e)
	{
		mouse_on_picker_surface = false;
	}

	private bool KeyboardGesture_OnKeyPressed (
		Gtk.EventControllerKey _,
		Gtk.EventControllerKey.KeyPressedSignalArgs e)
	{
		if (e.GetKey ().Value == Gdk.Constants.KEY_x)
			CycleColors ();
		return true;
	}

	private void MotionController_OnMotion (
		Gtk.EventControllerMotion _,
		Gtk.EventControllerMotion.MotionSignalArgs args)
	{
		if (!mouse_on_picker_surface) return;
		SetColorFromPickerSurface (new PointD (args.X, args.Y));
	}

	private void HexEntry_OnChanged (Gtk.Entry sender, EventArgs _)
	{
		if ((GetFocus ()?.Parent) != sender) return;
		CurrentColor = Color.FromHex (sender.GetText ()) ?? CurrentColor;
		UpdateView ();
	}

	private void OnResetButtonClicked (Gtk.Button button, EventArgs args)
	{
		Colors = original_colors;
		UpdateView ();
	}

	private void OnShrinkButtonClicked (Gtk.Button button, EventArgs args)
	{
		SetSmallMode (!small_mode);
		button.SetIconName (
			small_mode
			? Resources.StandardIcons.WindowMaximize
			: Resources.StandardIcons.WindowMinimize);
	}

	private void OnOkButtonClicked (Gtk.Button button, EventArgs args)
	{
		Response ((int) Gtk.ResponseType.Ok);
		Close ();
	}

	private void OnCancelButtonClicked (Gtk.Button button, EventArgs args)
	{
		Response ((int) Gtk.ResponseType.Cancel);
		Close ();
	}

	private static ColorPick CycledColors (ColorPick colorPick)
	{
		return colorPick switch {
			SingleColor singleColor => singleColor,
			PaletteColors paletteColors => paletteColors.Swapped (),
			_ => throw new UnreachableException (),
		};
	}

	private void CycleColors ()
	{
		Colors = CycledColors (Colors);
		UpdateView ();
	}


	private void UpdateView ()
	{
		// Redraw picker surfaces
		picker_surface_cursor.QueueDraw ();
		picker_surface.QueueDraw ();

		// Redraw cps
		HsvColor hsv = CurrentColor.ToHsv ();

		hue_slider.SetValue (hsv.Hue);
		saturation_slider.SetValue (hsv.Sat * 100.0);
		value_slider.SetValue (hsv.Val * 100.0);

		red_slider.SetValue (CurrentColor.R * 255.0);
		green_slider.SetValue (CurrentColor.G * 255.0);
		blue_slider.SetValue (CurrentColor.B * 255.0);
		alpha_slider.SetValue (CurrentColor.A * 255.0);


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
		int radius = picker_surface_radius;
		int diameter = 2 * radius;
		Size drawSize = new (diameter, diameter);

		using ImageSurface surface = CairoExtensions.CreateImageSurface (
			Format.Argb32,
			drawSize.Width,
			drawSize.Height);

		Span<ColorBgra> data = surface.GetPixelData ();

		switch (picker_surface_type) {

			case ColorSurfaceType.HueAndSat:

				PointI center = new (radius, radius);

				for (int y = 0; y < drawSize.Height; y++) {
					for (int x = 0; x < drawSize.Width; x++) {

						PointI pixel = new (x, y);
						PointI vector = pixel - center;

						if (vector.Magnitude () > radius) continue;

						double h = (MathF.Atan2 (vector.Y, -vector.X) + MathF.PI) / (2f * MathF.PI) * 360f;
						double s = Math.Min (vector.Magnitude () / radius, 1);
						double v = picker_surface_option_draw_value.Active ? CurrentColor.ToHsv ().Val : 1;

						double d = radius - vector.Magnitude ();
						double a = d < 1 ? d : 1;

						Color c = Color.FromHsv (h, s, v, a);

						data[drawSize.Width * y + x] = c.ToColorBgra ();
					}
				}

				break;


			case ColorSurfaceType.SatAndVal:

				for (int y = 0; y < drawSize.Height; y++) {
					double s = 1.0 - (double) y / (drawSize.Height - 1);
					for (int x = 0; x < drawSize.Width; x++) {
						double v = (double) x / (drawSize.Width - 1);
						Color c = Color.FromHsv (CurrentColor.ToHsv ().Hue, s, v);
						data[drawSize.Width * y + x] = c.ToColorBgra ();
					}
				}

				break;

			default:
				throw new InvalidOperationException ($"{nameof (picker_surface_type)} cannot have a value of {picker_surface_type}");
		}

		surface.MarkDirty ();
		g.SetSourceSurface (surface, picker_surface_padding, picker_surface_padding);
		g.Paint ();
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
				throw new InvalidOperationException ($"{nameof (picker_surface_type)} cannot have a value of {picker_surface_type}");
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
