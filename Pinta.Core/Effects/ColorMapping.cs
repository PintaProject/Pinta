using System;
using System.Collections.Generic;
using System.Linq;

namespace Pinta.Core;

public static class ColorMapping
{
	private static ColorBgra DefaultStartColor () => ColorBgra.Black;
	private static ColorBgra DefaultEndColor () => ColorBgra.White;
	private static double DefaultMinimumValue () => 0;
	private static double DefaultMaximumValue () => 255;
	private static IEnumerable<KeyValuePair<double, ColorBgra>> EmptyStops () => Enumerable.Empty<KeyValuePair<double, ColorBgra>> ();

	public static ColorGradient Gradient ()
	{
		return new ColorGradient (
			DefaultStartColor (),
			DefaultEndColor (),
			DefaultMinimumValue (),
			DefaultMaximumValue (),
			EmptyStops ()
		);
	}

	public static ColorGradient Gradient (ColorBgra start, ColorBgra end)
	{
		return new ColorGradient (
			start,
			end,
			DefaultMinimumValue (),
			DefaultMaximumValue (),
			EmptyStops ()
		);
	}

	public static ColorGradient Gradient (ColorBgra start, ColorBgra end, double minimum, double maximum)
	{
		return new ColorGradient (
			start,
			end,
			minimum,
			maximum,
			EmptyStops ()
		);
	}

	public static ColorGradient Gradient (ColorBgra start, ColorBgra end, double minimum, double maximum, IEnumerable<KeyValuePair<double, ColorBgra>> stops)
	{
		return new ColorGradient (
			start,
			end,
			minimum,
			maximum,
			stops
		);
	}

	public static IRangeColorMapping Range (double minimum, double maximum, Func<double, ColorBgra> mappingFunction)
	{
		return new SimpleRangeMapping (minimum, maximum, mappingFunction);
	}

	private sealed class SimpleRangeMapping : IRangeColorMapping
	{
		private readonly Func<double, ColorBgra> mapping_function;
		internal SimpleRangeMapping (double minimum, double maximum, Func<double, ColorBgra> mappingFunction)
		{
			if (minimum >= maximum) throw new ArgumentException ($"{nameof (minimum)} has to be lower than {nameof (maximum)}");
			MinimumPosition = minimum;
			MaximumPosition = maximum;
			mapping_function = mappingFunction;
		}
		public double MinimumPosition { get; }
		public double MaximumPosition { get; }
		public ColorBgra GetColor (double position) => mapping_function (position);
		public bool IsMapped (double position) => position >= MinimumPosition && position <= MaximumPosition;
	}
}
