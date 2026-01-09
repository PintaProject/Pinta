using System;
using System.Collections.Generic;
using System.Globalization;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

// used in `ColorPickerDialog` for the right hand side sliders
// uses a label, scale, and entry
// then hides the scale and draws over it
// with a drawingarea
public sealed class ColorPickerSlider : Gtk.Box
{
	public enum Component
	{
		Hue,
		Saturation,
		Value,
		Red,
		Green,
		Blue,
		Alpha
	}

	private const int PADDING_WIDTH = 14;
	private const int PADDING_HEIGHT = 10;

	private readonly Component component;
	private readonly Gtk.Scale slider_control;
	private readonly Gtk.Entry input_field;
	private readonly Gtk.Overlay slider_overlay;
	private readonly Gtk.DrawingArea cursor_area;
	private Color color;

	public Gtk.DrawingArea Gradient { get; }

	public event EventHandler? OnColorChanged;

	public ColorPickerSlider (Component component, Color initialColor, int initialWidth)
	{
		Gtk.Scale sliderControl = new () {
			WidthRequest = initialWidth,
			Opacity = 0,
		};
		sliderControl.SetOrientation (Gtk.Orientation.Horizontal);
		sliderControl.SetAdjustment (Gtk.Adjustment.New (0, 0, GetMaxValue (component) + 1, 1, 1, 1));
		sliderControl.SetValue (ExtractValue (initialColor, component));
		sliderControl.OnChangeValue += OnSliderControlChangeValue;

		Gtk.DrawingArea cursorArea = new ();
		cursorArea.SetSizeRequest (initialWidth, this.GetHeight ());
		cursorArea.SetDrawFunc (CursorAreaDrawingFunction);

		Gtk.DrawingArea gradient = new ();
		gradient.SetSizeRequest (initialWidth, this.GetHeight ());
		gradient.SetDrawFunc ((area, context, width, height) => {
			DrawGradient (context, width, height, CreateGradient (color, component));
		});

		Gtk.Label sliderLabel = new () { WidthRequest = 50 };
		sliderLabel.SetLabel (GetLabelText (component));

		Gtk.Overlay sliderOverlay = new () {
			WidthRequest = initialWidth,
			HeightRequest = this.GetHeight (),
		};
		sliderOverlay.AddOverlay (gradient);
		sliderOverlay.AddOverlay (cursorArea);
		sliderOverlay.AddOverlay (sliderControl);

		Gtk.Entry inputField = new () {
			MaxWidthChars = 3,
			WidthRequest = 50,
			Hexpand = false,
		};
		inputField.SetText (Convert.ToInt32 (ExtractValue (initialColor, component)).ToString ());
		inputField.OnChanged += OnInputFieldChanged;

		// --- Initialization (Gtk.Box)

		Append (sliderLabel);
		Append (sliderOverlay);
		Append (inputField);

		// --- References to keep

		color = initialColor;
		cursor_area = cursorArea;
		slider_control = sliderControl;
		slider_overlay = sliderOverlay;
		input_field = inputField;

		Gradient = gradient;

		this.component = component;
	}

	private void CursorAreaDrawingFunction (
		Gtk.DrawingArea area,
		Context context,
		int width,
		int height)
	{
		const int OUTLINE_WIDTH = 2;

		double currentPosition = slider_control.GetValue () / GetMaxValue (component) * (width - 2 * PADDING_WIDTH) + PADDING_WIDTH;

		ReadOnlySpan<PointD> cursorPoly = [
			new (currentPosition, height / 2),
			new (currentPosition + 4, 3 * height / 4),
			new (currentPosition + 4, height - OUTLINE_WIDTH / 2),
			new (currentPosition - 4, height - OUTLINE_WIDTH / 2),
			new (currentPosition - 4, 3 * height / 4),
			new (currentPosition, height / 2),
		];

		context.LineWidth = OUTLINE_WIDTH;

		context.DrawPolygonal (
			cursorPoly,
			new Color (0, 0, 0),
			LineCap.Butt);

		context.FillPolygonal (
			cursorPoly,
			new Color (1, 1, 1));
	}

	private bool OnSliderControlChangeValue (
		Gtk.Range sender,
		Gtk.Range.ChangeValueSignalArgs args)
	{
		// The provided value is from the scroll action, so we need to clamp to the range!
		double clampedValue = Math.Clamp (args.Value, 0, GetMaxValue (component));

		input_field.SetText (clampedValue.ToString (CultureInfo.InvariantCulture));

		color = UpdateValue (color, component, clampedValue);
		OnColorChanged?.Invoke (this, new ());

		return false;
	}

	private void OnInputFieldChanged (Gtk.Editable inputField, EventArgs e)
	{
		string text = inputField.GetText ();

		bool success = double.TryParse (
			text,
			CultureInfo.InvariantCulture,
			out double parsed);

		double maxValue = GetMaxValue (component);
		if (parsed > maxValue) {
			parsed = maxValue;
			inputField.SetText (Convert.ToInt32 (parsed).ToString ());
		}

		if (!success)
			return;

		color = UpdateValue (color, component, parsed);
		OnColorChanged?.Invoke (this, new ());
	}

	public void SetSliderWidth (int sliderWidth)
	{
		slider_control.WidthRequest = sliderWidth;
		Gradient.SetSizeRequest (sliderWidth, this.GetHeight ());
		cursor_area.SetSizeRequest (sliderWidth, this.GetHeight ());
		slider_overlay.WidthRequest = sliderWidth;
	}

	public Color Color {
		get => color;
		set {
			color = value;
			double componentValue = ExtractValue (value, component);
			slider_control.SetValue (componentValue);

			if (!input_field.IsEditingText ()) {
				// Ensure we don't get an infinite loop of "value changed" events
				string newText = Convert.ToInt32 (componentValue).ToString ();
				if (newText != input_field.GetText ())
					input_field.SetText (newText);
			}

			Gradient.QueueDraw ();
			cursor_area.QueueDraw ();
		}
	}

	private void DrawGradient (Context context, int width, int height, ColorGradient<Color> colors)
	{
		context.Antialias = Antialias.None;

		Size drawSize = new (
			Width: width - PADDING_WIDTH * 2,
			Height: height - PADDING_HEIGHT * 2);

		PointI p = new (
			X: PADDING_WIDTH + drawSize.Width,
			Y: PADDING_HEIGHT + drawSize.Height);

		int bsize = drawSize.Height / 2;

		// Draw transparency background
		context.FillRectangle (
			new RectangleD (PADDING_WIDTH, PADDING_HEIGHT, drawSize.Width, drawSize.Height),
			new Color (1, 1, 1));

		for (int x = PADDING_WIDTH; x < p.X; x += bsize * 2) {

			int bwidth =
				(x + bsize > p.X)
				? (p.X - x)
				: bsize;

			context.FillRectangle (
				new RectangleD (x, PADDING_HEIGHT, bwidth, bsize),
				new Color (.8, .8, .8));
		}

		for (int x = PADDING_WIDTH + bsize; x < p.X; x += bsize * 2) {

			int bwidth =
				(x + bsize > p.X)
				? (p.X - x)
				: bsize;

			context.FillRectangle (
				new RectangleD (x, PADDING_HEIGHT + drawSize.Height / 2, bwidth, bsize),
				new Color (.8, .8, .8));
		}

		LinearGradient pat = new (
			x0: PADDING_WIDTH,
			y0: PADDING_HEIGHT,
			x1: p.X,
			y1: p.Y);

		var normalized = colors.Resized (NumberRange.Create<double> (0, 1));

		pat.AddColorStop (normalized.Range.Lower, normalized.StartColor);

		for (int i = 0; i < normalized.StopsCount; i++)
			pat.AddColorStop (normalized.Positions[i], normalized.Colors[i]);

		pat.AddColorStop (normalized.Range.Upper, normalized.EndColor);

		context.Rectangle (
			PADDING_WIDTH,
			PADDING_HEIGHT,
			drawSize.Width,
			drawSize.Height);

		context.SetSource (pat);
		context.Fill ();
	}

	private static double ExtractValue (Color color, Component component) => component switch {
		Component.Hue => color.ToHsv ().Hue,
		Component.Saturation => color.ToHsv ().Sat * 100.0,
		Component.Value => color.ToHsv ().Val * 100.0,
		Component.Red => color.R * 255.0,
		Component.Green => color.G * 255.0,
		Component.Blue => color.B * 255.0,
		Component.Alpha => color.A * 255.0,
		_ => throw new ArgumentOutOfRangeException (nameof (component), component, null)
	};

	private static Color UpdateValue (Color color, Component component, double val) => component switch {
		Component.Hue => color.CopyHsv (hue: val),
		Component.Saturation => color.CopyHsv (sat: val / 100.0),
		Component.Value => color.CopyHsv (value: val / 100.0),
		Component.Red => color with { R = val / 255.0 },
		Component.Green => color with { G = val / 255.0 },
		Component.Blue => color with { B = val / 255.0 },
		Component.Alpha => color with { A = val / 255.0 },
		_ => throw new ArgumentOutOfRangeException (nameof (component), component, null)
	};

	private static double GetMaxValue (Component component) => component switch {
		Component.Hue => 360,
		Component.Saturation or Component.Value => 100,
		Component.Red or Component.Green or Component.Blue or Component.Alpha => 255,
		_ => throw new ArgumentOutOfRangeException ()
	};

	private static ColorGradient<Color> CreateGradient (Color color, Component component)
	{
		return component switch {
			Component.Hue => ColorGradient.Create (
				startColor: color.CopyHsv (hue: 0),
				endColor: color.CopyHsv (hue: 360),
				range: NumberRange.Create<double> (0, 360),
				new Dictionary<double, Color> {
					[60] = color.CopyHsv (hue: 60),
					[120] = color.CopyHsv (hue: 120),
					[180] = color.CopyHsv (hue: 180),
					[240] = color.CopyHsv (hue: 240),
					[300] = color.CopyHsv (hue: 300),
				}
			),
			Component.Saturation => ColorGradient.Create (
				color.CopyHsv (sat: 0),
				color.CopyHsv (sat: 1)
			),
			Component.Value => ColorGradient.Create (
				color.CopyHsv (value: 0),
				color.CopyHsv (value: 1)
			),
			Component.Red => ColorGradient.Create (
				color with { R = 0 },
				color with { R = 1 }
			),
			Component.Green => ColorGradient.Create (
				color with { G = 0 },
				color with { G = 1 }
			),
			Component.Blue => ColorGradient.Create (
				color with { B = 0 },
				color with { B = 1 }
			),
			Component.Alpha => ColorGradient.Create (
				color with { A = 0 },
				color with { A = 1 }
			),
			_ => throw new ArgumentOutOfRangeException (nameof (component), component, null)
		};
	}

	private static string GetLabelText (Component component) => component switch {
		Component.Hue => Translations.GetString ("Hue"),
		// Translators: this is an abbreviation for "Saturation"
		Component.Saturation => Translations.GetString ("Sat"),
		Component.Value => Translations.GetString ("Value"),
		Component.Red => Translations.GetString ("Red"),
		Component.Green => Translations.GetString ("Green"),
		Component.Blue => Translations.GetString ("Blue"),
		Component.Alpha => Translations.GetString ("Alpha"),
		_ => throw new ArgumentOutOfRangeException (nameof (component), component, null)
	};
}
