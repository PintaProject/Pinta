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
	private readonly Gtk.Label slider_label;
	private readonly Gtk.Scale slider_control;
	private readonly Gtk.Entry input_field;
	private readonly Gtk.Overlay slider_overlay;
	private readonly Gtk.DrawingArea cursor_area;

	public Gtk.DrawingArea Gradient { get; }

	public event EventHandler<OnChangeValueArgs>? OnValueChange;

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
			MaxWidthChars = settings.MaxWidthChars,
			WidthRequest = 50,
			Hexpand = false,
		};
		inputField.SetText (Convert.ToInt32 (settings.InitialValue).ToString ());
		inputField.OnChanged (OnInputFieldChanged);

		// --- Initialization (Gtk.Box)

		Append (sliderLabel);
		Append (sliderOverlay);
		Append (inputField);

		// --- References to keep

		top_window = settings.TopWindow;

		slider_label = sliderLabel;
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

		double currentPosition = slider_control.GetValue () / settings.Max * (width - 2 * settings.SliderPaddingWidth) + settings.SliderPaddingWidth;

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
		OnChangeValueArgs e = new (
			senderName: slider_label.GetLabel (),
			value: args.Value);

		input_field.SetText (e.Value.ToString (CultureInfo.InvariantCulture));

		OnValueChange?.Invoke (this, e);

		return false;
	}

	private void OnInputFieldChanged (Gtk.Entry inputField, EventArgs e)
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

		OnChangeValueArgs e2 = new (
			senderName: slider_label.GetLabel (),
			value: parsed);

		OnValueChange?.Invoke (this, e2);
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

		Size drawSize = new (
			Width: width - settings.SliderPaddingWidth * 2,
			Height: height - settings.SliderPaddingHeight * 2);

		PointI p = new (
			X: settings.SliderPaddingWidth + drawSize.Width,
			Y: settings.SliderPaddingHeight + drawSize.Height);

		int bsize = drawSize.Height / 2;

		// Draw transparency background
		context.FillRectangle (
			new RectangleD (settings.SliderPaddingWidth, settings.SliderPaddingHeight, drawSize.Width, drawSize.Height),
			new Color (1, 1, 1));

		for (int x = settings.SliderPaddingWidth; x < p.X; x += bsize * 2) {

			int bwidth =
				(x + bsize > p.X)
				? (p.X - x)
				: bsize;

			context.FillRectangle (
				new RectangleD (x, settings.SliderPaddingHeight, bwidth, bsize),
				new Color (.8, .8, .8));
		}

		for (int x = settings.SliderPaddingWidth + bsize; x < p.X; x += bsize * 2) {

			int bwidth =
				(x + bsize > p.X)
				? (p.X - x)
				: bsize;

			context.FillRectangle (
				new RectangleD (x, settings.SliderPaddingHeight + drawSize.Height / 2, bwidth, bsize),
				new Color (.8, .8, .8));
		}

		LinearGradient pat = new (
			x0: settings.SliderPaddingWidth,
			y0: settings.SliderPaddingHeight,
			x1: p.X,
			y1: p.Y);

		for (int i = 0; i < colors.Length; i++)
			pat.AddColorStop (i / (double) (colors.Length - 1), colors[i]);

		context.Rectangle (
			settings.SliderPaddingWidth,
			settings.SliderPaddingHeight,
			drawSize.Width,
			drawSize.Height);

		context.SetSource (pat);
		context.Fill ();
	}

}
