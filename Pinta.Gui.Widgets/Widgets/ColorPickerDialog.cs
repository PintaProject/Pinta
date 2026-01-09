using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public sealed class ColorPickerDialog : Gtk.Dialog
{
	enum ColorSurfaceType
	{
		HueAndSat,
		SatAndVal,
	}

	const bool DEFAULT_SMALL_MODE = false;
	const ColorSurfaceType DEFAULT_PICKER_SURFACE_TYPE = ColorSurfaceType.HueAndSat;

	const int DEFAULT_MARGINS = BIG_MARGINS;
	const int DEFAULT_PALETTE_DISPLAY_SIZE = BIG_PALETTE_DISPLAY_SIZE;
	const int DEFAULT_PICKER_SURFACE_RADIUS = BIG_PICKER_SURFACE_RADIUS;
	const int DEFAULT_SLIDER_WIDTH = BIG_SLIDER_WIDTH;
	const int DEFAULT_SPACING = BIG_SPACING;

	const int BIG_MARGINS = 12;
	const int BIG_PALETTE_DISPLAY_SIZE = 50;
	const int BIG_PICKER_SURFACE_RADIUS = 200 / 2;
	const int BIG_SLIDER_WIDTH = 200;
	const int BIG_SPACING = 6;

	const int SMALL_MARGINS = 6;
	const int SMALL_PALETTE_DISPLAY_SIZE = 40;
	const int SMALL_PICKER_SURFACE_RADIUS = 75;
	const int SMALL_SLIDER_WIDTH = 150;
	const int SMALL_SPACING = 2;

	private readonly Gtk.Box top_box;
	private readonly Gtk.Box swatch_box;
	private readonly Gtk.Box color_display_box;
	private readonly Gtk.DrawingArea swatch_recent;
	private readonly Gtk.DrawingArea swatch_palette;

	// palette
	const int PALETTE_DISPLAY_BORDER_THICKNESS = 3;
	private int palette_display_size = DEFAULT_PALETTE_DISPLAY_SIZE;
	private readonly ImmutableArray<Gtk.DrawingArea> color_displays;

	// color surface
	const int PICKER_SURFACE_PADDING = 10;
	private int picker_surface_radius = DEFAULT_PICKER_SURFACE_RADIUS;
	private readonly Gtk.Box picker_surface_selector_box;
	private readonly Gtk.Box picker_surface_box;
	private readonly Gtk.Overlay picker_surface_overlay;
	private readonly Gtk.DrawingArea picker_surface;
	private readonly Gtk.DrawingArea picker_surface_cursor;

	// color surface options
	private ColorSurfaceType picker_surface_type = DEFAULT_PICKER_SURFACE_TYPE;
	private bool mouse_on_picker_surface = false;
	private readonly Gtk.CheckButton picker_surface_option_draw_value;

	// hex + sliders
	private readonly Gtk.Entry hex_entry;
	private readonly IPaletteService palette;
	private int slider_width = DEFAULT_SLIDER_WIDTH;
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
		get => ExtractTargetedColor (Colors, primary_selected);
		set => SetTargeted (value);
	}

	private static Color ExtractTargetedColor (ColorPick colors, bool primarySelected)
	{
		return colors switch {
			SingleColor singleColor => primarySelected ? singleColor.Color : throw new InvalidOperationException (),
			PaletteColors paletteColors => primarySelected ? paletteColors.Primary : paletteColors.Secondary,
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

	private int spacing = DEFAULT_SPACING;
	private int margins = DEFAULT_MARGINS;
	private bool small_mode = DEFAULT_SMALL_MODE;
	private readonly bool show_swatches = false;

	private void SetSmallMode (bool isSmallMode)
	{
		// incredibly silly workaround
		// but if this is not done, it seems Wayland will assume the window will never be transparent, and thus opacity will break
		SetOpacity (0.995f);
		small_mode = isSmallMode;
		if (isSmallMode) {
			spacing = SMALL_SPACING;
			margins = SMALL_MARGINS;
			palette_display_size = SMALL_PALETTE_DISPLAY_SIZE;
			picker_surface_radius = SMALL_PICKER_SURFACE_RADIUS;
			slider_width = SMALL_SLIDER_WIDTH;
			swatch_box.Visible = false;
		} else {
			spacing = BIG_SPACING;
			margins = BIG_MARGINS;
			palette_display_size = BIG_PALETTE_DISPLAY_SIZE;
			picker_surface_radius = BIG_PICKER_SURFACE_RADIUS;
			slider_width = BIG_SLIDER_WIDTH;
			if (show_swatches)
				swatch_box.Visible = true;
		}

		top_box.Spacing = spacing;
		color_display_box.Spacing = spacing;

		foreach (var display in color_displays)
			display.SetSizeRequest (palette_display_size, palette_display_size);

		int pickerSurfaceDrawSize = (picker_surface_radius + PICKER_SURFACE_PADDING) * 2;

		picker_surface_box.WidthRequest = pickerSurfaceDrawSize;
		picker_surface_box.Spacing = spacing;
		picker_surface_selector_box.WidthRequest = pickerSurfaceDrawSize;
		picker_surface_selector_box.Spacing = spacing;
		picker_surface.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);
		picker_surface_cursor.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);
		picker_surface_overlay.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);
		picker_surface_selector_box.SetOrientation (small_mode ? Gtk.Orientation.Vertical : Gtk.Orientation.Horizontal);

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

	private static bool IsPrimary (int colorIndex) // TODO: Get rid of this
		=> colorIndex == 0;

	/// <param name="parentWindow">The dialog's parent window.</param>
	/// <param name="palette">Palette service.</param>
	/// <param name="adjustable">Palette of adjustable </param>
	/// <param name="primarySelected"></param>
	/// <param name="livePalette">Determines modality of the dialog and live palette behaviour. If true, dialog will not block rest of app and will update
	/// the current palette as the color is changed.</param>
	/// <param name="windowTitle">Title of the dialog.</param>
	internal ColorPickerDialog (
		Gtk.Window? parentWindow,
		IPaletteService palette,
		ColorPick adjustable,
		bool primarySelected, // TODO: Get rid of this
		bool livePalette,
		string windowTitle)
	{
		bool showWatches = !livePalette;

		Gtk.Button resetButton = new () { Label = Translations.GetString ("Reset Color") };
		resetButton.OnClicked += OnResetButtonClicked;

		Gtk.Button shrinkButton = new ();
		shrinkButton.OnClicked += OnShrinkButtonClicked;
		shrinkButton.SetIconName (
			DEFAULT_SMALL_MODE
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

		// Active palette contains the primary/secondary colors on the left of the color picker
		#region Color Display

		ImmutableArray<Gtk.DrawingArea> colorDisplays = CreateColorDisplays (adjustable);

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

		int pickerSurfaceDrawSize = (DEFAULT_PICKER_SURFACE_RADIUS + PICKER_SURFACE_PADDING) * 2;

		// Show Value toggle for hue sat picker surface

		Gtk.CheckButton pickerSurfaceOptionDrawValue = new () {
			Active = true,
			Label = Translations.GetString ("Show Value"),
		};
		pickerSurfaceOptionDrawValue.SetVisible (DEFAULT_PICKER_SURFACE_TYPE == ColorSurfaceType.HueAndSat);
		pickerSurfaceOptionDrawValue.OnToggled += (o, e) => UpdateView ();

		Gtk.ToggleButton pickerSurfaceSatVal = Gtk.ToggleButton.NewWithLabel (Translations.GetString ("Sat & Value"));
		pickerSurfaceSatVal.Active = DEFAULT_PICKER_SURFACE_TYPE == ColorSurfaceType.SatAndVal;
		pickerSurfaceSatVal.OnToggled += (_, _) => {
			picker_surface_type = ColorSurfaceType.SatAndVal;
			pickerSurfaceOptionDrawValue.SetVisible (false);
			UpdateView ();
		};

		// When Gir.Core supports it, this should probably be replaced with a toggle group.
		Gtk.ToggleButton pickerSurfaceHueSat = Gtk.ToggleButton.NewWithLabel (Translations.GetString ("Hue & Sat"));
		pickerSurfaceHueSat.Active = DEFAULT_PICKER_SURFACE_TYPE == ColorSurfaceType.HueAndSat;
		pickerSurfaceHueSat.OnToggled += (_, _) => {
			picker_surface_type = ColorSurfaceType.HueAndSat;
			pickerSurfaceOptionDrawValue.SetVisible (true);
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

			PointD locBase = HsvToPickerLocation (CurrentColor.ToHsv (), picker_surface_radius);
			PointD loc = new (locBase.X + picker_surface_radius + PICKER_SURFACE_PADDING, locBase.Y + picker_surface_radius + PICKER_SURFACE_PADDING);

			context.Antialias = Antialias.None;

			context.FillRectangle (
				new RectangleD (loc.X - 5, loc.Y - 5, 10, 10),
				CurrentColor);

			context.DrawRectangle (
				new RectangleD (loc.X - 5, loc.Y - 5, 10, 10),
				new Color (0, 0, 0),
				4);

			context.DrawRectangle (
				new RectangleD (loc.X - 5, loc.Y - 5, 10, 10),
				new Color (1, 1, 1),
				1);
		});

		// Overlays the cursor on top of the surface
		Gtk.Overlay pickerSurfaceOverlay = new ();
		pickerSurfaceOverlay.AddOverlay (pickerSurface);
		pickerSurfaceOverlay.AddOverlay (pickerSurfaceCursor);
		pickerSurfaceOverlay.SetSizeRequest (pickerSurfaceDrawSize, pickerSurfaceDrawSize);

		Gtk.Box pickerSurfaceBox = new () {
			Spacing = spacing,
			WidthRequest = pickerSurfaceDrawSize,
		};
		pickerSurfaceBox.SetOrientation (Gtk.Orientation.Vertical);
		pickerSurfaceBox.Append (pickerSurfaceSelectorBox);
		pickerSurfaceBox.Append (pickerSurfaceOverlay);
		pickerSurfaceBox.Append (pickerSurfaceOptionDrawValue);

		#endregion

		// Handles the ColorPickerSliders + Hex entry.

		Color initialColor = ExtractTargetedColor (adjustable, primarySelected);

		Gtk.Entry hexEntry = new () {
			Text_ = initialColor.ToHex (),
			MaxWidthChars = 10,
		};
		hexEntry.OnChanged += HexEntry_OnChanged;

		Gtk.Label hexLabel = new () {
			Label_ = Translations.GetString ("Hex"),
			WidthRequest = 50,
		};

		Gtk.Box hexBox = new () { Spacing = spacing };
		hexBox.Append (hexLabel);
		hexBox.Append (hexEntry);

		ColorPickerSlider hueSlider = new (
			ColorPickerSlider.Component.Hue,
			initialColor,
			slider_width
		);
		hueSlider.OnColorChanged += (_, _) => {
			CurrentColor = hueSlider.Color;
			UpdateView ();
		};

		ColorPickerSlider saturationSlider = new (
			ColorPickerSlider.Component.Saturation,
			initialColor,
			slider_width
		);
		saturationSlider.OnColorChanged += (_, _) => {
			CurrentColor = saturationSlider.Color;
			UpdateView ();
		};

		ColorPickerSlider valueSlider = new (
			ColorPickerSlider.Component.Value,
			initialColor,
			slider_width
		);
		valueSlider.OnColorChanged += (_, _) => {
			CurrentColor = valueSlider.Color;
			UpdateView ();
		};

		ColorPickerSlider redSlider = new (
			ColorPickerSlider.Component.Red,
			initialColor,
			slider_width
		);
		redSlider.OnColorChanged += (_, _) => {
			CurrentColor = redSlider.Color;
			UpdateView ();
		};

		ColorPickerSlider greenSlider = new (
			ColorPickerSlider.Component.Green,
			initialColor,
			slider_width
		);
		greenSlider.OnColorChanged += (_, _) => {
			CurrentColor = greenSlider.Color;
			UpdateView ();
		};

		ColorPickerSlider blueSlider = new (
			ColorPickerSlider.Component.Blue,
			initialColor,
			slider_width
		);
		blueSlider.OnColorChanged += (_, _) => {
			CurrentColor = blueSlider.Color;
			UpdateView ();
		};

		ColorPickerSlider alphaSlider = new (
			ColorPickerSlider.Component.Alpha,
			initialColor,
			slider_width
		);
		alphaSlider.OnColorChanged += (_, _) => {
			CurrentColor = alphaSlider.Color;
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

		Gtk.GestureDrag dragGesture = Gtk.GestureDrag.New ();
		dragGesture.SetButton (0); // Listen for all mouse buttons.
		dragGesture.OnDragBegin += DragGesture_OnDragBegin;
		dragGesture.OnDragUpdate += DragGesture_OnDragUpdate;
		dragGesture.OnDragEnd += DragGesture_OnDragEnd;

		Gtk.EventControllerKey keyboardGesture = Gtk.EventControllerKey.New ();
		keyboardGesture.OnKeyPressed += KeyboardGesture_OnKeyPressed;

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
		if (!DEFAULT_SMALL_MODE)
			mainVbox.Append (swatchBox);

		Gtk.Box contentArea = this.GetContentAreaBox ();
		contentArea.SetAllMargins (DEFAULT_MARGINS);
		contentArea.Append (mainVbox);

		if (livePalette) {

			palette.PrimaryColorChanged += PrimaryChangeHandler;
			palette.SecondaryColorChanged += SecondaryChangeHandler;
			IsActivePropertyDefinition.Notify (this, ActiveWindowChangeHandler);
			OnResponse += ColorPickerDialog_OnResponse;
		}

		// --- Initialization (Gtk.Widget)

		// Mouse and keyboard handlers
		AddController (dragGesture);
		AddController (keyboardGesture);

		// incredibly silly workaround
		// but if this is not done, it seems Wayland will assume the window will never be transparent, and thus opacity will break
		SetOpacity (0.995f);

		// --- Initialization (Gtk.Window)

		SetTitlebar (titleBar);

		Title = Translations.GetString (windowTitle);
		TransientFor = parentWindow;
		Modal = false;
		IconName = Resources.Icons.ImageResizeCanvas;
		DefaultWidth = 1;
		DefaultHeight = 1;

		// --- Initialization (Gtk.Dialog)

		this.SetDefaultResponse (Gtk.ResponseType.Cancel);

		// --- References to keep

		Colors = adjustable;

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
		picker_surface_option_draw_value = pickerSurfaceOptionDrawValue;
		show_swatches = showWatches;
		sliders_box = slidersBox;
		swatch_box = swatchBox;
		swatch_recent = swatchRecent;
		swatch_palette = swatchPalette;
		top_box = topBox;
	}

	ImmutableArray<Gtk.DrawingArea> CreateColorDisplays (ColorPick pick)
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

	private void DragGesture_OnDragBegin (
		Gtk.GestureDrag gesture,
		Gtk.GestureDrag.DragBeginSignalArgs e)
	{
		gesture.GetStartPoint (out double startX, out double startY);
		PointD absPos = new (startX, startY);

		if (picker_surface.IsMouseInDrawingArea (this, absPos, out PointD _)) {
			mouse_on_picker_surface = true;
			SetColorFromPickerSurface (absPos);
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

	private void DragGesture_OnDragUpdate (
		Gtk.GestureDrag gesture,
		Gtk.GestureDrag.DragUpdateSignalArgs e)
	{
		if (!mouse_on_picker_surface)
			return;

		gesture.GetStartPoint (out double startX, out double startY);
		PointD absPos = new (startX + e.OffsetX, startY + e.OffsetY);
		SetColorFromPickerSurface (absPos);
	}

	private void DragGesture_OnDragEnd (
		Gtk.GestureDrag gesture,
		Gtk.GestureDrag.DragEndSignalArgs e)
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

	private void HexEntry_OnChanged (Gtk.Editable sender, EventArgs _)
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

		// Update sliders with current color
		Color current = CurrentColor;
		hue_slider.Color = current;
		saturation_slider.Color = current;
		value_slider.Color = current;
		red_slider.Color = current;
		green_slider.Color = current;
		blue_slider.Color = current;
		alpha_slider.Color = current;


		// Update hex
		if (GetFocus ()?.Parent != hex_entry)
			hex_entry.SetText (current.ToHex ());

		// Redraw palette displays
		foreach (var display in color_displays)
			display.QueueDraw ();
	}

	private void DrawPaletteDisplay (Context g, Color c)
	{
		int xy = PALETTE_DISPLAY_BORDER_THICKNESS;
		int wh = palette_display_size - PALETTE_DISPLAY_BORDER_THICKNESS * 2;

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
			new Color (0, 0, 0), PALETTE_DISPLAY_BORDER_THICKNESS);
	}

	private void DrawColorSurface (Context g)
	{
		int radius = picker_surface_radius;
		int radiusSquared = radius * radius;
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

						int magnitudeSquared = vector.MagnitudeSquared ();

						if (magnitudeSquared > radiusSquared) continue;

						double magnitude = Math.Sqrt (magnitudeSquared);

						double h = (MathF.Atan2 (vector.Y, -vector.X) + MathF.PI) / (2f * MathF.PI) * 360f;
						double s = Math.Min (magnitude / radius, 1);
						double v = picker_surface_option_draw_value.Active ? CurrentColor.ToHsv ().Val : 1;

						double d = radius - magnitude;
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
		g.SetSourceSurface (surface, PICKER_SURFACE_PADDING, PICKER_SURFACE_PADDING);
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
			PICKER_SURFACE_PADDING,
			PICKER_SURFACE_PADDING,
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
