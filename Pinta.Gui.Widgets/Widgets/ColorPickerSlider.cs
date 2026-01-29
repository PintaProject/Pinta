using System;
using System.Collections.Generic;
using System.Globalization;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

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
	private Color color;

	private readonly Gtk.Entry input_field;
	private readonly Gtk.DrawingArea gradient_slider;

	public event EventHandler? OnColorChanged;

	public ColorPickerSlider (Component component, Color initialColor, int initialWidth)
	{
		Gtk.DrawingArea gradientSlider = new ();
		gradientSlider.SetSizeRequest (initialWidth, this.GetHeight ());
		gradientSlider.SetDrawFunc ((_, context, width, height) => {
			DrawGradient (context, width, height, CreateGradient (color, component));
		});

		Gtk.Label sliderLabel = new () { WidthRequest = 50 };
		sliderLabel.SetLabel (GetLabelText (component));

		Gtk.Entry inputField = new () {
			MaxWidthChars = 3,
			WidthRequest = 50,
			Hexpand = false,
		};
		inputField.SetText (Convert.ToInt32 (ExtractValue (initialColor, component)).ToString ());
		inputField.OnChanged += OnInputFieldChanged;

		Gtk.GestureDrag dragGesture = Gtk.GestureDrag.New ();
		dragGesture.SetButton (GtkExtensions.MOUSE_LEFT_BUTTON);
		dragGesture.OnDragBegin += (_, _) => dragGesture.SetState (Gtk.EventSequenceState.Claimed);
		dragGesture.OnDragUpdate += OnDragUpdate;
		dragGesture.OnDragEnd += OnDragEnd;
		gradientSlider.AddController (dragGesture);

		Gtk.EventControllerScroll scrollController = Gtk.EventControllerScroll.New (Gtk.EventControllerScrollFlags.Vertical);
		scrollController.OnScroll += HandleScrollEvent;
		gradientSlider.AddController (scrollController);

		// --- Initialization (Gtk.Box)

		Append (sliderLabel);
		Append (gradientSlider);
		Append (inputField);

		// --- References to keep

		color = initialColor;
		input_field = inputField;
		gradient_slider = gradientSlider;

		this.component = component;
	}

	private void OnDragUpdate (Gtk.GestureDrag sender, Gtk.GestureDrag.DragUpdateSignalArgs args)
	{
		sender.GetStartPoint (out double startX, out _);
		UpdateColorFromDrag (startX + args.OffsetX);
	}

	private void OnDragEnd (Gtk.GestureDrag sender, Gtk.GestureDrag.DragEndSignalArgs args)
	{
		sender.GetStartPoint (out double startX, out _);
		UpdateColorFromDrag (startX + args.OffsetX);
	}

	private void UpdateColorFromDrag (double x)
	{
		x -= PADDING_WIDTH;
		double maxX = gradient_slider.GetWidth () - 2 * PADDING_WIDTH;
		double value = (x / maxX) * GetMaxValue (component);

		UpdateColorValue (value);
	}

	private void UpdateColorValue (double value)
	{
		double clampedValue = Math.Clamp (Math.Round (value), 0, GetMaxValue (component));
		input_field.SetText (clampedValue.ToString (CultureInfo.InvariantCulture));

		color = UpdateValue (color, component, clampedValue);
		gradient_slider.QueueDraw ();
		OnColorChanged?.Invoke (this, EventArgs.Empty);
	}

	private bool HandleScrollEvent (
		Gtk.EventControllerScroll controller,
		Gtk.EventControllerScroll.ScrollSignalArgs args)
	{
		double value = ExtractValue (color, component);
		UpdateColorValue (value - args.Dy);
		return true;
	}

	private void DrawCursor (Context context, int width, int height)
	{
		const int OUTLINE_WIDTH = 2;

		double value = ExtractValue (color, component);
		double currentPosition = value / GetMaxValue (component) * (width - 2 * PADDING_WIDTH) + PADDING_WIDTH;

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
		gradient_slider.WidthRequest = sliderWidth;
	}

	public Color Color {
		get => color;
		set {
			color = value;
			double componentValue = ExtractValue (value, component);

			if (!input_field.IsEditingText ()) {
				// Ensure we don't get an infinite loop of "value changed" events
				string newText = Convert.ToInt32 (componentValue).ToString ();
				if (newText != input_field.GetText ())
					input_field.SetText (newText);
			}

			gradient_slider.QueueDraw ();
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

		DrawCursor (context, width, height);
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
