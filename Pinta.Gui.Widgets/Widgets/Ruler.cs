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
using Cairo;
using Gtk;
using Pinta.Core;

namespace Pinta.Gui.Widgets
{
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
	public class Ruler : DrawingArea
	{
		private double _position = 0;
		private MetricType _metric = MetricType.Pixels;

		/// <summary>
		/// Whether the ruler is horizontal or vertical.
		/// </summary>
		public Orientation Orientation { get; }

		/// <summary>
		/// Metric type used for the ruler.
		/// </summary>
		public MetricType Metric {
			get => _metric;
			set {
				_metric = value;
				QueueDraw ();
			}
		}

		/// <summary>
		/// The position of the mark along the ruler.
		/// </summary>
		public double Position {
			get => _position;
			set {
				_position = value;
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

			SetDrawFunc ((area, context, width, height) => Draw (context, width, height));

			// Determine the size request, based on the font size.
			int font_size = GetFontSize (GetPangoContext ().GetFontDescription (), ScaleFactor);
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

		private void Draw (Context cr, int width, int height)
		{
			GetStyleContext ().GetColor (out var color);
			cr.SetSourceColor (color);

			cr.LineWidth = 1.0;

			// Draw bottom line of the ruler.
			switch (Orientation) {
				case Orientation.Horizontal:
					cr.Rectangle (0, height - 1, width, 1);
					break;
				case Orientation.Vertical:
					cr.Rectangle (width - 1, 0, 1, height);
					// Swap so that width is the longer dimension (horizontal).
					Utility.Swap (ref width, ref height);
					break;
			}
			cr.Fill ();

			double[]? ruler_scale;
			int[]? subdivide;
			double pixels_per_unit = 1.0;

			switch (Metric) {
				case MetricType.Pixels:
					ruler_scale = new double[] { 1, 2, 5, 10, 25, 50, 100, 250, 500, 1000 };
					subdivide = new int[] { 1, 5, 10, 50, 100 };
					pixels_per_unit = 1.0;
					break;
				case MetricType.Inches:
					ruler_scale = new double[] { 1, 2, 4, 8, 16, 32, 64, 128, 256, 512 };
					subdivide = new int[] { 1, 2, 4, 8, 16 };
					pixels_per_unit = 72;
					break;
				case MetricType.Centimeters:
				default:
					ruler_scale = new double[] { 1, 2, 5, 10, 25, 50, 100, 250, 500, 1000 };
					subdivide = new int[] { 1, 5, 10, 50, 100 };
					pixels_per_unit = 28.35;
					break;
			}

			// Find our scaled range.
			double scaled_upper = Upper / pixels_per_unit;
			double scaled_lower = Lower / pixels_per_unit;
			double max_size = scaled_upper - scaled_lower;

			// There must be enough space between the large ticks for the text labels.
			var font = GetPangoContext ().GetFontDescription ();
			int font_size = GetFontSize (font, ScaleFactor);
			var max_digits = ((int) -Math.Abs (max_size)).ToString ().Length;
			int min_separation = max_digits * font_size * 2;

			double increment = width / max_size;

			// Figure out how to display the ticks.
			int scale_index;
			for (scale_index = 0; scale_index < ruler_scale.Length - 1; ++scale_index) {
				if (ruler_scale[scale_index] * increment > min_separation)
					break;
			}

			int divide_index;
			for (divide_index = 0; divide_index < subdivide.Length - 1; ++divide_index) {
				if (ruler_scale[scale_index] * increment < 5 * subdivide[divide_index + 1])
					break;
			}

			double pixels_per_tick = increment * ruler_scale[scale_index] / subdivide[divide_index];
			double units_per_tick = pixels_per_tick / increment;
			double ticks_per_unit = 1.0 / units_per_tick;

			int start = (int) Math.Floor (scaled_lower * ticks_per_unit);
			int end = (int) Math.Ceiling (scaled_upper * ticks_per_unit);

			for (int i = start; i <= end; ++i) {
				// Position of tick (add 0.5 to center tick on pixel).
				double position = Math.Floor (i * pixels_per_tick - scaled_lower * increment) + 0.5;

				// Height of tick
				int tick_height = height;
				for (int j = divide_index; j > 0; --j) {
					if (i % subdivide[j] == 0) break;
					tick_height = tick_height / 2 + 1;
				}

				// Draw text for major ticks.
				if (i % subdivide[divide_index] == 0) {
					int label_value = (int) Math.Round (i * units_per_tick);
					string label = label_value.ToString ();

					var layout = CreatePangoLayout (label);
					layout.SetFontDescription (font);

					switch (Orientation) {
						case Orientation.Horizontal:
							cr.MoveTo (position + 2, 0);
							PangoCairo.Functions.ShowLayout (cr, layout);
							break;
						case Orientation.Vertical:
							cr.Save ();
							cr.MoveTo (font_size * 1.5, position + font_size / 2);
							cr.Rotate (0.5 * Math.PI);
							PangoCairo.Functions.ShowLayout (cr, layout);
							cr.Restore ();
							break;
					}
				}

				// Draw ticks
				switch (Orientation) {
					case Orientation.Horizontal:
						cr.MoveTo (position, height - tick_height);
						cr.LineTo (position, height);
						break;
					case Orientation.Vertical:
						cr.MoveTo (height - tick_height, position);
						cr.LineTo (height, position);
						break;
				}

				cr.Stroke ();
			}

			// Draw marker
			var marker_position = (Position - Lower) * (width / (Upper - Lower));

			switch (Orientation) {
				case Orientation.Horizontal:
					cr.MoveTo (marker_position, 0);
					cr.LineTo (marker_position, height);
					break;
				case Orientation.Vertical:
					cr.MoveTo (0, marker_position);
					cr.LineTo (height, marker_position);
					break;
			}

			cr.Stroke ();

			// TODO-GTK3 - cache the ticks
		}

		private static int GetFontSize (Pango.FontDescription font, int scale_factor)
		{
			int font_size = font.GetSize ();
			font_size = PangoExtensions.UnitsToPixels (font_size);

			// Convert from points to device units.
			if (!font.GetSizeIsAbsolute ()) {
				font_size = (int) ((scale_factor * font_size) / 72.0);
			}

			return font_size;
		}
	}
}
