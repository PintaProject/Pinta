using System;
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
	public sealed class ValueChangedEventArgs (double value) : EventArgs
	{
		public double Value { get; } = value;
	}

	public readonly record struct Settings (
		int Max,
		string Text, // required
		double InitialValue,
		int SliderWidth);

	private const int PADDING_WIDTH = 14;
	private const int PADDING_HEIGHT = 10;

	private readonly Settings settings;
	private readonly Gtk.Scale slider_control;
	private readonly Gtk.Entry input_field;
	private readonly Gtk.Overlay slider_overlay;
	private readonly Gtk.DrawingArea cursor_area;

	public Gtk.DrawingArea Gradient { get; }

	public event EventHandler<ValueChangedEventArgs>? OnValueChange;

	public ColorPickerSlider (Settings settings)
	{
		Gtk.Scale sliderControl = new () {
			WidthRequest = settings.SliderWidth,
			Opacity = 0,
		};
		sliderControl.SetOrientation (Gtk.Orientation.Horizontal);
		sliderControl.SetAdjustment (Gtk.Adjustment.New (0, 0, settings.Max + 1, 1, 1, 1));
		sliderControl.SetValue (settings.InitialValue);
		sliderControl.OnChangeValue += OnSliderControlChangeValue;

		Gtk.DrawingArea cursorArea = new ();
		cursorArea.SetSizeRequest (settings.SliderWidth, this.GetHeight ());
		cursorArea.SetDrawFunc (CursorAreaDrawingFunction);

		Gtk.DrawingArea gradient = new ();
		gradient.SetSizeRequest (settings.SliderWidth, this.GetHeight ());

		Gtk.Label sliderLabel = new () { WidthRequest = 50 };
		sliderLabel.SetLabel (settings.Text);

		Gtk.Overlay sliderOverlay = new () {
			WidthRequest = settings.SliderWidth,
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
		inputField.SetText (Convert.ToInt32 (settings.InitialValue).ToString ());
		inputField.OnChanged += OnInputFieldChanged;

		// --- Initialization (Gtk.Box)

		Append (sliderLabel);
		Append (sliderOverlay);
		Append (inputField);

		// --- References to keep

		cursor_area = cursorArea;
		slider_control = sliderControl;
		slider_overlay = sliderOverlay;
		input_field = inputField;

		Gradient = gradient;

		this.settings = settings;
	}

	private void CursorAreaDrawingFunction (
		Gtk.DrawingArea area,
		Context context,
		int width,
		int height)
	{
		const int OUTLINE_WIDTH = 2;

		double currentPosition = slider_control.GetValue () / settings.Max * (width - 2 * PADDING_WIDTH) + PADDING_WIDTH;

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
		double clampedValue = Math.Clamp (args.Value, 0, settings.Max);

		input_field.SetText (clampedValue.ToString (CultureInfo.InvariantCulture));

		OnValueChange?.Invoke (this, new ValueChangedEventArgs (clampedValue));

		return false;
	}

	private void OnInputFieldChanged (Gtk.Editable inputField, EventArgs e)
	{

		// see SetValue about suppression
		if (suppress_event)
			return;

		string text = inputField.GetText ();

		bool success = double.TryParse (
			text,
			CultureInfo.InvariantCulture,
			out double parsed);

		if (parsed > settings.Max) {
			parsed = settings.Max;
			inputField.SetText (Convert.ToInt32 (parsed).ToString ());
		}

		if (!success)
			return;

		OnValueChange?.Invoke (this, new ValueChangedEventArgs (parsed));
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
		if (!input_field.IsEditingText ()) {
			// prevents OnValueChange from firing when we change the value internally
			// because OnValueChange eventually calls SetValue so it causes a stack overflow
			suppress_event = true;
			input_field.SetText (Convert.ToInt32 (val).ToString ());
		}
		Gradient.QueueDraw ();
		cursor_area.QueueDraw ();
		suppress_event = false;
	}

	public void DrawGradient (Context context, int width, int height, ColorGradient<Color> colors)
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

}
