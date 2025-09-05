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
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Cairo;
using Pinta.Core;

namespace Pinta.Gui.Widgets;

public enum MetricType
{
	Pixels,
	Inches,
	Centimeters,
}

/// <summary>
/// Replacement for Gtk.Ruler, which was removed in GTK3.
/// Based on the original GTK2 widget and Inkscape's ruler widget.
/// </summary>
public sealed class Ruler : Gtk.DrawingArea
{
	private double position = 0;
	private MetricType metric = MetricType.Pixels;

	private Surface? cached_surface = null;
	private Size? last_known_size = null;

	private double? selection_start = null;
	private double? selection_end = null;

	public void SetSelectionRange (double? start, double? end)
	{
		if (selection_start == start && selection_end == end)
			return;

		selection_start = start;
		selection_end = end;

		QueueDraw ();
	}

	/// <summary>
	/// Whether the ruler is horizontal or vertical.
	/// </summary>
	public Gtk.Orientation Orientation { get; }

	/// <summary>
	/// Metric type used for the ruler.
	/// </summary>
	public MetricType Metric {
		get => metric;
		set {
			metric = value;
			QueueFullRedraw ();
		}
	}

	/// <summary>
	/// The position of the mark along the ruler.
	/// </summary>
	public double Position {
		get => position;
		set {
			position = value;
			QueueFullRedraw ();
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

	public Ruler (Gtk.Orientation orientation)
	{
		Orientation = orientation;

		SetDrawFunc ((area, context, width, height) => Draw (context, new Size (width, height)));

		// Determine the size request, based on the font size.
		int font_size = GetFontSize (GetPangoContext ().GetFontDescription ()!, ScaleFactor);
		int size = 2 + font_size * 2;

		int width = 0;
		int height = 0;
		switch (Orientation) {
			case Gtk.Orientation.Horizontal:
				height = size;
				break;
			case Gtk.Orientation.Vertical:
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

		QueueFullRedraw ();
	}

	// Invalidates cache _and_ queues redraw. Like a full refresh
	private void QueueFullRedraw ()
	{
		InvalidateCache ();
		QueueDraw ();
	}

	private void InvalidateCache ()
	{
		cached_surface?.Dispose ();
		cached_surface = null;
	}

	private static readonly ImmutableArray<double> pixels_ruler_scale = [1, 2, 5, 10, 25, 50, 100, 250, 500, 1000];
	private static readonly ImmutableArray<int> pixels_subdivide = [1, 5, 10, 50, 100];

	private static readonly ImmutableArray<double> inches_ruler_scale = [1, 2, 4, 8, 16, 32, 64, 128, 256, 512];
	private static readonly ImmutableArray<int> inches_subdivide = [1, 2, 4, 8, 16];

	private static readonly ImmutableArray<double> centimeters_ruler_scale = [1, 2, 5, 10, 25, 50, 100, 250, 500, 1000];
	private static readonly ImmutableArray<int> centimeters_subdivide = [1, 5, 10, 50, 100];

	private readonly record struct RulerDrawSettings (
		ImmutableArray<int> SubDivide,
		double ScaledUpper,
		double ScaledLower,
		Pango.FontDescription Font,
		int FontSize,
		double Increment,
		int DivideIndex,
		double PixelsPerTick,
		double UnitsPerTick,
		int Start,
		int End,
		double MarkerPosition,
		RectangleD RulerOuterLine,
		Size EffectiveSize,
		Color Color,
		Gtk.Orientation Orientation);
	private RulerDrawSettings CreateSettings (Size preliminarySize)
	{
		GetStyleContext ().GetColor (out Color color);

		RectangleD rulerOuterLine = Orientation switch {

			Gtk.Orientation.Vertical => new (
				X: preliminarySize.Width - 1,
				Y: 0,
				Width: 1,
				Height: preliminarySize.Height),

			Gtk.Orientation.Horizontal => new (
				X: 0,
				Y: preliminarySize.Height - 1,
				Width: preliminarySize.Width,
				Height: 1),

			_ => throw new UnreachableException (),
		};

		Size effectiveSize = Orientation switch {
			Gtk.Orientation.Vertical => new (preliminarySize.Height, preliminarySize.Width),// Swap so that width is the longer dimension (horizontal).
			Gtk.Orientation.Horizontal => preliminarySize,
			_ => throw new UnreachableException (),
		};

		ImmutableArray<double> rulerScale = Metric switch {
			MetricType.Pixels => pixels_ruler_scale,
			MetricType.Inches => inches_ruler_scale,
			MetricType.Centimeters => centimeters_ruler_scale,
			_ => throw new UnreachableException (),
		};

		ImmutableArray<int> subdivide = Metric switch {
			MetricType.Pixels => pixels_subdivide,
			MetricType.Inches => inches_subdivide,
			MetricType.Centimeters => centimeters_subdivide,
			_ => throw new UnreachableException (),
		};

		double pixels_per_unit = Metric switch {
			MetricType.Pixels => 1.0,
			MetricType.Inches => 72,
			MetricType.Centimeters => 28.35,
			_ => throw new UnreachableException (),
		};

		// Find our scaled range.
		double scaledUpper = Upper / pixels_per_unit;
		double scaledLower = Lower / pixels_per_unit;
		double maxSize = scaledUpper - scaledLower;

		// There must be enough space between the large ticks for the text labels.
		Pango.FontDescription font = GetPangoContext ().GetFontDescription ()!;
		int fontSize = GetFontSize (font, ScaleFactor);
		int maxDigits = ((int) -Math.Abs (maxSize)).ToString ().Length;
		int minSeparation = maxDigits * fontSize * 2;

		double increment = effectiveSize.Width / maxSize;

		// Figure out how to display the ticks.
		int scaleIndex;
		for (scaleIndex = 0; scaleIndex < rulerScale.Length - 1; ++scaleIndex) {
			if (rulerScale[scaleIndex] * increment > minSeparation)
				break;
		}

		int divideIndex;
		for (divideIndex = 0; divideIndex < subdivide.Length - 1; ++divideIndex) {
			if (rulerScale[scaleIndex] * increment < 5 * subdivide[divideIndex + 1])
				break;
		}

		double pixelsPerTick = increment * rulerScale[scaleIndex] / subdivide[divideIndex];
		double unitsPerTick = pixelsPerTick / increment;
		double ticksPerUnit = 1.0 / unitsPerTick;

		return new (
			SubDivide: subdivide,
			ScaledUpper: scaledUpper,
			ScaledLower: scaledLower,
			Font: font,
			FontSize: fontSize,
			Increment: increment,
			DivideIndex: divideIndex,
			PixelsPerTick: pixelsPerTick,
			UnitsPerTick: unitsPerTick,
			Start: (int) Math.Floor (scaledLower * ticksPerUnit),
			End: (int) Math.Ceiling (scaledUpper * ticksPerUnit),
			MarkerPosition: GetPositionOnRuler (Position, effectiveSize.Width),
			RulerOuterLine: rulerOuterLine,
			EffectiveSize: effectiveSize,
			Color: color,
			Orientation: Orientation);
	}

	private void Draw (Context cr, Size preliminarySize)
	{
		if (last_known_size != preliminarySize) {
			InvalidateCache ();
			last_known_size = new Size (preliminarySize.Width, preliminarySize.Height);
		}

		RulerDrawSettings settings = CreateSettings (preliminarySize);

		cached_surface ??= CreateBaseRuler (settings, preliminarySize);

		// Draw the selection projection if a selection exists
		if (selection_start.HasValue && selection_end.HasValue) {

			// Convert selection coordinates to ruler widget coordinates
			double p1 = GetPositionOnRuler (selection_start.Value, settings.EffectiveSize.Width);
			double p2 = GetPositionOnRuler (selection_end.Value, settings.EffectiveSize.Width);

			cr.SetSourceRgba (0.5, 0.7, 1.0, 0.5); // Semi-transparent blue

			switch (Orientation) {
				case Gtk.Orientation.Horizontal:
					cr.Rectangle (p1, 0, p2 - p1, settings.EffectiveSize.Height);
					break;
				default:
					cr.Rectangle (0, p1, settings.EffectiveSize.Height, p2 - p1);
					break;
			}

			cr.Fill ();
		}

		cr.SetSourceSurface (cached_surface, 0, 0);
		cr.Paint ();

		cr.SetSourceColor (settings.Color);
		cr.LineWidth = 1.0;

		// Draw marker
		switch (settings.Orientation) {
			case Gtk.Orientation.Horizontal:
				cr.MoveTo (settings.MarkerPosition, 0);
				cr.LineTo (settings.MarkerPosition, settings.EffectiveSize.Height);
				break;
			case Gtk.Orientation.Vertical:
				cr.MoveTo (0, settings.MarkerPosition);
				cr.LineTo (settings.EffectiveSize.Height, settings.MarkerPosition);
				break;
		}

		cr.Stroke ();
	}

	private ImageSurface CreateBaseRuler (in RulerDrawSettings settings, Size preliminarySize)
	{
		ImageSurface result = new (
			Format.Argb32,
			preliminarySize.Width,
			preliminarySize.Height);

		using Context drawingContext = new (result);

		drawingContext.SetSourceColor (settings.Color);
		drawingContext.LineWidth = 1.0;
		drawingContext.Rectangle (settings.RulerOuterLine);
		drawingContext.Fill ();

		for (int i = settings.Start; i <= settings.End; ++i) {

			// Position of tick (add 0.5 to center tick on pixel).
			double tickPosition = Math.Floor (i * settings.PixelsPerTick - settings.ScaledLower * settings.Increment) + 0.5;

			// Height of tick
			int tickHeight = settings.EffectiveSize.Height;

			for (int j = settings.DivideIndex; j > 0; --j) {
				if (i % settings.SubDivide[j] == 0) break;
				tickHeight = tickHeight / 2 + 1;
			}

			// Draw text for major ticks.
			if (i % settings.SubDivide[settings.DivideIndex] == 0) {

				string label = ((int) Math.Round (i * settings.UnitsPerTick)).ToString ();
				var layout = CreatePangoLayout (label);
				layout.SetFontDescription (settings.Font);

				if (settings.Orientation == Gtk.Orientation.Horizontal) {
					drawingContext.MoveTo (tickPosition + 2, 0);
					PangoCairo.Functions.ShowLayout (drawingContext, layout);
				} else {
					drawingContext.Save ();
					drawingContext.MoveTo (settings.FontSize * 1.5, tickPosition + settings.FontSize / 2);
					drawingContext.Rotate (0.5 * Math.PI);
					PangoCairo.Functions.ShowLayout (drawingContext, layout);
					drawingContext.Restore ();
				}
			}

			// Draw ticks
			if (settings.Orientation == Gtk.Orientation.Horizontal) {
				drawingContext.MoveTo (tickPosition, settings.EffectiveSize.Height - tickHeight);
				drawingContext.LineTo (tickPosition, settings.EffectiveSize.Height);
			} else {
				drawingContext.MoveTo (settings.EffectiveSize.Height - tickHeight, tickPosition);
				drawingContext.LineTo (settings.EffectiveSize.Height, tickPosition);
			}
			drawingContext.Stroke ();
		}

		return result;
	}

	[MethodImpl (MethodImplOptions.AggressiveInlining)]
	private double GetPositionOnRuler (double position, double width)
	{
		double range = Upper - Lower;
		double scaledWidth = width / range;
		double positionFromLower = position - Lower;
		return positionFromLower * scaledWidth;
	}

	private static int GetFontSize (Pango.FontDescription font, int scaleFactor)
	{
		int fontSize = PangoExtensions.UnitsToPixels (font.GetSize ());
		if (font.GetSizeIsAbsolute ())
			return fontSize;
		else
			return (int) (scaleFactor * fontSize / 72.0);
	}
}
