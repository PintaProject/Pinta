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

using Cairo;
using Gtk;
using Pinta.Core;
using System;

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
		public Orientation Orientation { get; private set; }

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

		private Requisition GetSizeRequest ()
		{
			var border = StyleContext.GetBorder (StateFlags);
			var font = ObsoleteExtensions.GetStyleContextFont (StyleContext, StateFlags);
			int font_size = GetFontSize (font);

			int size = 2 + font_size * 2;

			int width = border.Left + border.Right;
			int height = border.Top + border.Bottom;

			switch (Orientation) {
				case Orientation.Horizontal:
					width += 1;
					height += size;
					break;
				case Orientation.Vertical:
					width += size;
					height += 1;
					break;
			}

			return new Requisition () { Width = width, Height = height };
		}

		protected override void OnGetPreferredHeight (out int minimum_height, out int natural_height)
		{
			minimum_height = natural_height = GetSizeRequest ().Height;
		}

		protected override void OnGetPreferredWidth (out int minimum_width, out int natural_width)
		{
			minimum_width = natural_width = GetSizeRequest ().Width;
		}

		protected override bool OnDrawn (Context cr)
		{
			var awidth = AllocatedWidth;
			var aheight = AllocatedHeight;
			StyleContext.RenderBackground (cr, 0, 0, awidth, aheight);

			cr.LineWidth = 1.0;
			Gdk.CairoHelper.SetSourceRgba (cr, StyleContext.GetColor (StateFlags));

			// Determine the ruler's size.
			var border = StyleContext.GetBorder (StateFlags);
			int rwidth = awidth - (border.Left + border.Right);
			int rheight = aheight - (border.Top + border.Bottom);

			// Draw bottom line of the ruler.
			switch (Orientation) {
				case Orientation.Horizontal:
					cr.Rectangle (0, aheight - border.Bottom - 1, awidth, 1);
					break;
				case Orientation.Vertical:
					cr.Rectangle (awidth - border.Left - 1, 0, 1, aheight);
					// Swap so that width is the longer dimension (horizontal).
					Utility.Swap (ref awidth, ref aheight);
					Utility.Swap (ref rwidth, ref rheight);
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
			var font = ObsoleteExtensions.GetStyleContextFont (StyleContext, StateFlags);
			int font_size = GetFontSize (font);
			var max_digits = ((int) -Math.Abs (max_size)).ToString ().Length;
			int min_separation = max_digits * font_size * 2;

			double increment = awidth / max_size;

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
				int height = rheight;
				for (int j = divide_index; j > 0; --j) {
					if (i % subdivide[j] == 0) break;
					height = height / 2 + 1;
				}

				// Draw text for major ticks.
				if (i % subdivide[divide_index] == 0) {
					int label_value = (int) Math.Round (i * units_per_tick);
					string label = label_value.ToString ();

					var layout = CreatePangoLayout (label);
					layout.FontDescription = font;

					switch (Orientation) {
						case Orientation.Horizontal:
							cr.MoveTo (position + 2, border.Top);
							Pango.CairoHelper.ShowLayout (cr, layout);
							break;
						case Orientation.Vertical:
							PangoContext.BaseGravity = Pango.Gravity.East;
							PangoContext.GravityHint = Pango.GravityHint.Strong;
							cr.Save ();
							cr.MoveTo (border.Left + font_size, position + font_size / 2);
							cr.Rotate (0.5 * Math.PI);
							Pango.CairoHelper.ShowLayout (cr, layout);
							cr.Restore ();
							break;
					}
				}

				// Draw ticks
				switch (Orientation) {
					case Orientation.Horizontal:
						cr.MoveTo (position, rheight + border.Top - height);
						cr.LineTo (position, rheight + border.Top);
						break;
					case Orientation.Vertical:
						cr.MoveTo (rheight + border.Left - height, position);
						cr.LineTo (rheight + border.Left, position);
						break;
				}

				cr.Stroke ();
			}

			// TODO-GTK3 - cache the ticks, and update the marker's position as the mouse moves.

			return base.OnDrawn (cr);
		}

		private static int GetFontSize (Pango.FontDescription font)
		{
			int font_size = font.Size;
			if (!font.SizeIsAbsolute)
				font_size = (int) (font_size / Pango.Scale.PangoScale);

			return font_size;
		}
	}
}
