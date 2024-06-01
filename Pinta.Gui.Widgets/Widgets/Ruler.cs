//
// Ruler.cs
//
// Author:
//       Cameron White <cameronwhite91@gmail.com>
//
// Copyright (c) 2020 Cameron White
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
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public enum MetricType
{
	Pixels,
	Inches,
	Centimeters
}

/// <summary>
/// Replacement for Gtk.Ruler, which was removed in GTK3.
/// Based on the original GTK2 widget and Inkscape's ruler widget.
/// </summary>
public sealed class Ruler : DrawingArea
{
	private double position = 0;
	private MetricType metric = MetricType.Pixels;

	/// <summary>
	/// Whether the ruler is horizontal or vertical.
	/// </summary>
	public Orientation Orientation { get; }

	/// <summary>
	/// Metric type used for the ruler.
	/// </summary>
	public MetricType Metric {
		get => metric;
		set {
			metric = value;
			QueueDraw ();
		}
	}

	/// <summary>
	/// The position of the mark along the ruler.
	/// </summary>
	public double Position {
		get => position;
		set {
			position = value;
			QueueDraw ();
		}
	}

	/// <summary>
	/// Lower limit of the ruler in pixels.
	/// </summary>
	public double Lower { get; private set; } = 0;

	/// <summary>
	/// Upper limit of the ruler in pixels.
	/// </summary>
	public double Upper { get; private set; } = 1;

	public Ruler (Orientation orientation)
	{
		Orientation = orientation;

		SetDrawFunc ((area, context, width, height) => Draw (context, new Size (width, height)));

		// Determine the size request, based on the font size.
		int font_size = GetFontSize (GetPangoContext ().GetFontDescription ()!, ScaleFactor);
		int size = 2 + font_size * 2;

		int width = 0;
		int height = 0;
		switch (Orientation) {
			case Orientation.Horizontal:
				height = size;
				break;
			case Orientation.Vertical:
				width = size;
				break;
		}

		WidthRequest = width;
		HeightRequest = height;
	}

	/// <summary>
	/// Update the ruler's range.
	/// </summary>
	public void SetRange (double lower, double upper)
	{
		if (lower > upper)
			throw new ArgumentOutOfRangeException (nameof (lower), "Invalid range");

		Lower = lower;
		Upper = upper;

		QueueDraw ();
	}

	private static readonly IReadOnlyList<double> pixels_ruler_scale = new double[] { 1, 2, 5, 10, 25, 50, 100, 250, 500, 1000 };
	private static readonly IReadOnlyList<int> pixels_subdivide = new int[] { 1, 5, 10, 50, 100 };

	private static readonly IReadOnlyList<double> inches_ruler_scale = new double[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
	private static readonly IReadOnlyList<int> inches_subdivide = new int[] { 1, 2, 4, 8, 16 };

	private static readonly IReadOnlyList<double> centimeters_ruler_scale = new double[] { 1, 2, 5, 10, 25, 50, 100, 250, 500, 1000 };
	private static readonly IReadOnlyList<int> centimeters_subdivide = new int[] { 1, 5, 10, 50, 100 };

	private sealed record RulerDrawSettings (
		IReadOnlyList<int> subdivide,
		double scaled_upper,
		double scaled_lower,
		Pango.FontDescription font,
		int font_size,
		double increment,
		int divide_index,
		double pixels_per_tick,
		double units_per_tick,
		int start,
		int end,
		double marker_position,
		RectangleD rect,
		Size effectiveSize,
		Color color,
		Orientation orientation);
	private RulerDrawSettings CreateSettings (Size preliminarySize)
	{
		GetStyleContext ().GetColor (out Color color);

		RectangleD rect;
		Size effectiveSize;

		// Bottom line of the ruler.
		switch (Orientation) {

			case Orientation.Vertical:

				rect = new (
					X: preliminarySize.Width - 1,
					Y: 0,
					Width: 1,
					Height: preliminarySize.Height);

				// Swap so that width is the longer dimension (horizontal).
				effectiveSize = new (preliminarySize.Height, preliminarySize.Width);

				break;

			case Orientation.Horizontal:
			default:
				rect = new (
					X: 0,
					Y: preliminarySize.Height - 1,
					Width: preliminarySize.Width,
					Height: 1);

				effectiveSize = preliminarySize;

				break;
		}

		IReadOnlyList<double> ruler_scale;
		IReadOnlyList<int> subdivide;
		double pixels_per_unit;

		switch (Metric) {
			case MetricType.Pixels:
				ruler_scale = pixels_ruler_scale;
				subdivide = pixels_subdivide;
				pixels_per_unit = 1.0;
				break;
			case MetricType.Inches:
				ruler_scale = inches_ruler_scale;
				subdivide = inches_subdivide;
				pixels_per_unit = 72;
				break;
			case MetricType.Centimeters:
			default:
				ruler_scale = centimeters_ruler_scale;
				subdivide = centimeters_subdivide;
				pixels_per_unit = 28.35;
				break;
		}

		// Find our scaled range.
		double scaled_upper = Upper / pixels_per_unit;
		double scaled_lower = Lower / pixels_per_unit;
		double max_size = scaled_upper - scaled_lower;

		// There must be enough space between the large ticks for the text labels.
		Pango.FontDescription font = GetPangoContext ().GetFontDescription ()!;
		int font_size = GetFontSize (font, ScaleFactor);
		int max_digits = ((int) -Math.Abs (max_size)).ToString ().Length;
		int min_separation = max_digits * font_size * 2;

		double increment = effectiveSize.Width / max_size;

		// Figure out how to display the ticks.
		int scale_index;
		for (scale_index = 0; scale_index < ruler_scale.Count - 1; ++scale_index) {
			if (ruler_scale[scale_index] * increment > min_separation)
				break;
		}

		int divide_index;
		for (divide_index = 0; divide_index < subdivide.Count - 1; ++divide_index) {
			if (ruler_scale[scale_index] * increment < 5 * subdivide[divide_index + 1])
				break;
		}

		double pixels_per_tick = increment * ruler_scale[scale_index] / subdivide[divide_index];
		double units_per_tick = pixels_per_tick / increment;
		double ticks_per_unit = 1.0 / units_per_tick;

		return new (
			subdivide: subdivide,
			scaled_upper: scaled_upper,
			scaled_lower: scaled_lower,
			font: font,
			font_size: font_size,
			increment: increment,
			divide_index: divide_index,
			pixels_per_tick: pixels_per_tick,
			units_per_tick: units_per_tick,
			start: (int) Math.Floor (scaled_lower * ticks_per_unit),
			end: (int) Math.Ceiling (scaled_upper * ticks_per_unit),
			marker_position: (Position - Lower) * (effectiveSize.Width / (Upper - Lower)),
			rect: rect,
			effectiveSize: effectiveSize,
			color: color,
			orientation: Orientation);
	}

	private void Draw (
		Context cr,
		Size preliminarySize)
	{
		RulerDrawSettings settings = CreateSettings (preliminarySize);

		cr.SetSourceColor (settings.color);
		cr.LineWidth = 1.0;
		cr.Rectangle (settings.rect);
		cr.Fill ();

		for (int i = settings.start; i <= settings.end; ++i) {

			// Position of tick (add 0.5 to center tick on pixel).
			double position = Math.Floor (i * settings.pixels_per_tick - settings.scaled_lower * settings.increment) + 0.5;

			// Height of tick
			int tick_height = settings.effectiveSize.Height;
			for (int j = settings.divide_index; j > 0; --j) {
				if (i % settings.subdivide[j] == 0) break;
				tick_height = tick_height / 2 + 1;
			}

			// Draw text for major ticks.
			if (i % settings.subdivide[settings.divide_index] == 0) {
				int label_value = (int) Math.Round (i * settings.units_per_tick);
				string label = label_value.ToString ();

				var layout = CreatePangoLayout (label);
				layout.SetFontDescription (settings.font);

				switch (settings.orientation) {
					case Orientation.Horizontal:
						cr.MoveTo (position + 2, 0);
						PangoCairo.Functions.ShowLayout (cr, layout);
						break;
					case Orientation.Vertical:
						cr.Save ();
						cr.MoveTo (settings.font_size * 1.5, position + settings.font_size / 2);
						cr.Rotate (0.5 * Math.PI);
						PangoCairo.Functions.ShowLayout (cr, layout);
						cr.Restore ();
						break;
				}
			}

			// Draw ticks
			switch (settings.orientation) {
				case Orientation.Horizontal:
					cr.MoveTo (position, settings.effectiveSize.Height - tick_height);
					cr.LineTo (position, settings.effectiveSize.Height);
					break;
				case Orientation.Vertical:
					cr.MoveTo (settings.effectiveSize.Height - tick_height, position);
					cr.LineTo (settings.effectiveSize.Height, position);
					break;
			}

			cr.Stroke ();
		}

		// Draw marker
		switch (settings.orientation) {
			case Orientation.Horizontal:
				cr.MoveTo (settings.marker_position, 0);
				cr.LineTo (settings.marker_position, settings.effectiveSize.Height);
				break;
			case Orientation.Vertical:
				cr.MoveTo (0, settings.marker_position);
				cr.LineTo (settings.effectiveSize.Height, settings.marker_position);
				break;
		}

		cr.Stroke ();

		// TODO-GTK3 - cache the ticks
	}

	private static int GetFontSize (
		Pango.FontDescription font,
		int scale_factor)
	{
		int font_size = font.GetSize ();
		font_size = PangoExtensions.UnitsToPixels (font_size);

		// Convert from points to device units.
		if (!font.GetSizeIsAbsolute ())
			font_size = (int) (scale_factor * font_size / 72.0);

		return font_size;
	}
}
